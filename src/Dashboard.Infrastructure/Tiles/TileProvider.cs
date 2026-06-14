using System.Net;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Tiles;

/// <summary>
/// Karten-Kachel-Proxy mit Platten-Cache: liefert eine Kachel aus dem lokalen Cache oder lädt
/// sie einmalig vom Upstream-Anbieter und legt sie ab. Dadurch bekommt das bewusst offline
/// gehaltene Kiosk-iPad eine echte Karte – es spricht nur mit dem LAN-Server, der das Internet hat.
/// </summary>
public sealed class TileProvider
{
    private const int MaxAttempts = 3;

    // Begrenzt die GLEICHZEITIGEN Upstream-Abrufe über alle Anfragen hinweg (TileProvider ist
    // transient, daher statisch). Beim ersten Laden fragt der Browser Dutzende Kacheln auf einmal
    // an – ohne Drossel lehnt der Anbieter (CARTO) einen Teil ab → schwarze Lücken in der Karte.
    private static readonly SemaphoreSlim UpstreamGate = new(6);

    private readonly HttpClient _http;
    private readonly TileOptions _options;
    private readonly ILogger<TileProvider> _logger;
    private readonly string _cacheRoot;

    public TileProvider(
        HttpClient http,
        IOptions<TileOptions> options,
        IHostEnvironment environment,
        ILogger<TileProvider> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        var baseDir = Path.IsPathRooted(_options.CacheDirectory)
            ? _options.CacheDirectory
            : Path.Combine(environment.ContentRootPath, _options.CacheDirectory);

        // Pro Anbieter (URL-Vorlage) ein eigenes Unterverzeichnis: So mischt ein Anbieter-Wechsel
        // (z. B. CARTO → OSM) nicht alte mit neuen Kacheln, und ein Zurückwechseln nutzt den alten
        // Cache weiter – ohne manuelles Löschen.
        var providerKey = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(_options.UrlTemplate)))[..8];
        _cacheRoot = Path.Combine(baseDir, providerKey);
    }

    /// <summary>Plausibilitätsprüfung der Slippy-Map-Koordinaten (Schutz gegen Unsinns-Anfragen).</summary>
    public bool IsValid(int z, int x, int y)
    {
        if (z < 0 || z > _options.MaxZoom || x < 0 || y < 0)
        {
            return false;
        }

        var count = 1L << z; // Kacheln je Achse auf Zoomstufe z
        return x < count && y < count;
    }

    /// <summary>
    /// Kachel-PNG aus dem Cache oder vom Upstream; <c>null</c> bei ungültigen Koordinaten oder
    /// Upstream-Fehler (Leaflet zeigt dann nur diese eine Kachel nicht – die Karte bleibt nutzbar).
    /// </summary>
    public async Task<byte[]?> GetTileAsync(int z, int x, int y, CancellationToken ct)
    {
        if (!IsValid(z, x, y))
        {
            return null;
        }

        var path = Path.Combine(_cacheRoot, z.ToString(), x.ToString(), $"{y}.png");
        if (File.Exists(path))
        {
            return await File.ReadAllBytesAsync(path, ct);
        }

        var url = _options.UrlTemplate
            .Replace("{z}", z.ToString())
            .Replace("{x}", x.ToString())
            .Replace("{y}", y.ToString());

        await UpstreamGate.WaitAsync(ct);
        try
        {
            // Inzwischen von einer parallelen Anfrage gecacht?
            if (File.Exists(path))
            {
                return await File.ReadAllBytesAsync(path, ct);
            }

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    using var response = await _http.GetAsync(url, ct);
                    if (response.IsSuccessStatusCode)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                        await WriteCacheAsync(path, bytes, ct);
                        return bytes;
                    }

                    // 404 o. ä. ändert sich nicht – nur bei Drosselung (429) oder Serverfehler (5xx)
                    // lohnt ein erneuter Versuch.
                    if (response.StatusCode != HttpStatusCode.TooManyRequests && (int)response.StatusCode < 500)
                    {
                        _logger.LogDebug("Kachel {Z}/{X}/{Y}: Upstream antwortete {Status}", z, x, y, (int)response.StatusCode);
                        return null;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    if (ct.IsCancellationRequested || attempt == MaxAttempts)
                    {
                        _logger.LogDebug(ex, "Kachel {Z}/{X}/{Y}: Abruf fehlgeschlagen", z, x, y);
                        return null;
                    }
                }

                await Task.Delay(150 * attempt, ct); // kleiner Backoff vor dem nächsten Versuch
            }

            return null;
        }
        finally
        {
            UpstreamGate.Release();
        }
    }

    /// <summary>Liegt die Kachel bereits im Platten-Cache?</summary>
    public bool IsCached(int z, int x, int y) =>
        File.Exists(Path.Combine(_cacheRoot, z.ToString(), x.ToString(), $"{y}.png"));

    /// <summary>
    /// Lädt alle Kacheln einer Bounding-Box über die Zoomstufen einmalig in den Cache – gedrosselt
    /// (eine nach der anderen mit kleiner Pause), um den Anbieter nicht zu überlasten. Bereits
    /// gecachte Kacheln werden übersprungen. Gibt die Zahl NEU geladener Kacheln zurück.
    /// </summary>
    public async Task<int> WarmAsync(
        double minLat, double minLng, double maxLat, double maxLng, int minZoom, int maxZoom, CancellationToken ct)
    {
        var queued = CountTiles(minLat, minLng, maxLat, maxLng, minZoom, maxZoom);
        _logger.LogInformation("Tile-Warmup gestartet: ~{Count} Kacheln (Zoom {Min}–{Max}).", queued, minZoom, maxZoom);

        var checkedCount = 0;
        var fetched = 0;
        for (var z = minZoom; z <= maxZoom; z++)
        {
            var max = (1 << z) - 1;
            var xMin = Math.Clamp(LonToTileX(minLng, z), 0, max);
            var xMax = Math.Clamp(LonToTileX(maxLng, z), 0, max);
            var yMin = Math.Clamp(LatToTileY(maxLat, z), 0, max); // Nord = kleinere y
            var yMax = Math.Clamp(LatToTileY(minLat, z), 0, max);

            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    ct.ThrowIfCancellationRequested();
                    checkedCount++;
                    if (IsCached(z, x, y))
                    {
                        continue;
                    }

                    await GetTileAsync(z, x, y, ct);
                    fetched++;
                    await Task.Delay(120, ct); // höflich zum Anbieter – kein Massen-Burst

                    if (fetched % 250 == 0)
                    {
                        _logger.LogInformation(
                            "Tile-Warmup: {Fetched} neu geladen ({Checked}/{Queued} geprüft).", fetched, checkedCount, queued);
                    }
                }
            }
        }

        _logger.LogInformation("Tile-Warmup fertig: {Checked} Kacheln geprüft, {Fetched} neu geladen.", checkedCount, fetched);
        return fetched;
    }

    /// <summary>Zahl der Kacheln einer Bounding-Box über die Zoomstufen (für die Mengen-Schranke).</summary>
    public static long CountTiles(double minLat, double minLng, double maxLat, double maxLng, int minZoom, int maxZoom)
    {
        long total = 0;
        for (var z = minZoom; z <= maxZoom; z++)
        {
            var max = (1 << z) - 1;
            var xMin = Math.Clamp(LonToTileX(minLng, z), 0, max);
            var xMax = Math.Clamp(LonToTileX(maxLng, z), 0, max);
            var yMin = Math.Clamp(LatToTileY(maxLat, z), 0, max);
            var yMax = Math.Clamp(LatToTileY(minLat, z), 0, max);
            total += (long)(xMax - xMin + 1) * (yMax - yMin + 1);
        }

        return total;
    }

    private static int LonToTileX(double lon, int z) =>
        (int)Math.Floor((lon + 180.0) / 360.0 * (1 << z));

    private static int LatToTileY(double lat, int z)
    {
        var rad = lat * Math.PI / 180.0;
        var y = (1.0 - Math.Log(Math.Tan(rad) + 1.0 / Math.Cos(rad)) / Math.PI) / 2.0 * (1 << z);
        return (int)Math.Floor(y);
    }

    private static async Task WriteCacheAsync(string path, byte[] bytes, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Über eine temporäre Datei + Move schreiben, damit parallele Anfragen keine halb
        // geschriebene Datei lesen.
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temp, bytes, ct);
        File.Move(temp, path, overwrite: true);
    }
}
