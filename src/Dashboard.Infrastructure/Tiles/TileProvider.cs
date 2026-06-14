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
        _cacheRoot = Path.IsPathRooted(_options.CacheDirectory)
            ? _options.CacheDirectory
            : Path.Combine(environment.ContentRootPath, _options.CacheDirectory);
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

        try
        {
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kachel {Z}/{X}/{Y}: Upstream antwortete {Status}", z, x, y, (int)response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            await WriteCacheAsync(path, bytes, ct);
            return bytes;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Kachel {Z}/{X}/{Y}: Abruf fehlgeschlagen", z, x, y);
            return null;
        }
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
