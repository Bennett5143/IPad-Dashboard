using System.Net;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Crests;

/// <summary>Ein aus dem Cache/Upstream geliefertes Wappen samt MIME-Typ.</summary>
public sealed record CrestImage(byte[] Bytes, string ContentType);

/// <summary>
/// Wappen-/Flaggen-Proxy mit Platten-Cache – dasselbe Muster wie der Karten-Kachel-Proxy: Das
/// offline gehaltene Kiosk-iPad kann kein <c>&lt;img src="https://…"&gt;</c> ins Internet laden, also
/// holt der LAN-Server das Bild einmalig, legt es ab und liefert es fortan lokal aus. Die
/// Upstream-Host-Allowlist (<see cref="CrestOptions.AllowedHosts"/>) verhindert, dass der Endpoint
/// zum offenen Proxy wird.
/// </summary>
public sealed class CrestProvider
{
    private const int MaxAttempts = 3;

    // Begrenzt die GLEICHZEITIGEN Upstream-Abrufe (CrestProvider ist transient, daher statisch):
    // Beim ersten Rendern einer 20-Zeilen-Tabelle fragt der Browser 20 Wappen auf einmal an.
    private static readonly SemaphoreSlim UpstreamGate = new(6);

    private readonly HttpClient _http;
    private readonly ILogger<CrestProvider> _logger;
    private readonly HashSet<string> _allowedHosts;
    private readonly string _cacheRoot;

    public CrestProvider(
        HttpClient http,
        IOptions<CrestOptions> options,
        IHostEnvironment environment,
        ILogger<CrestProvider> logger)
    {
        _http = http;
        _logger = logger;
        _allowedHosts = new HashSet<string>(options.Value.AllowedHosts, StringComparer.OrdinalIgnoreCase);

        var dir = options.Value.CacheDirectory;
        _cacheRoot = Path.IsPathRooted(dir) ? dir : Path.Combine(environment.ContentRootPath, dir);
    }

    /// <summary>URL ist absolut, http(s) und ihr Host steht in der Allowlist.</summary>
    public bool IsAllowed(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && _allowedHosts.Contains(uri.Host);

    /// <summary>
    /// Wappen aus dem Cache oder vom Upstream; <c>null</c> bei nicht erlaubter URL oder
    /// Upstream-Fehler (dann rendert der Browser nur dieses eine Bild nicht – die Seite bleibt heil).
    /// </summary>
    public async Task<CrestImage?> GetCrestAsync(string url, CancellationToken ct)
    {
        if (!IsAllowed(url))
        {
            return null;
        }

        var contentType = ContentTypeFor(url);
        var path = CacheKey(url);
        if (File.Exists(path))
        {
            return new CrestImage(await File.ReadAllBytesAsync(path, ct), contentType);
        }

        await UpstreamGate.WaitAsync(ct);
        try
        {
            // Inzwischen von einer parallelen Anfrage gecacht?
            if (File.Exists(path))
            {
                return new CrestImage(await File.ReadAllBytesAsync(path, ct), contentType);
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
                        return new CrestImage(bytes, contentType);
                    }

                    // 404 o. ä. ändert sich nicht – nur bei Drosselung (429)/Serverfehler (5xx) neu versuchen.
                    if (response.StatusCode != HttpStatusCode.TooManyRequests && (int)response.StatusCode < 500)
                    {
                        _logger.LogDebug("Wappen {Url}: Upstream antwortete {Status}", ForLog(url), (int)response.StatusCode);
                        return null;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    if (ct.IsCancellationRequested || attempt == MaxAttempts)
                    {
                        _logger.LogDebug(ex, "Wappen {Url}: Abruf fehlgeschlagen", ForLog(url));
                        return null;
                    }
                }

                await Task.Delay(150 * attempt, ct);
            }

            return null;
        }
        finally
        {
            UpstreamGate.Release();
        }
    }

    // Zeilenumbrüche aus der user-gelieferten URL entfernen, bevor sie ins Log geht: sonst ließen
    // sich über \r\n gefälschte Log-Zeilen einschleusen (Log-Forging).
    private static string ForLog(string url) => url.Replace("\r", string.Empty).Replace("\n", string.Empty);

    // Hash der vollen URL als Dateiname; Zwei-Zeichen-Präfix als Unterordner, damit kein einzelnes
    // Verzeichnis mit Tausenden Dateien entsteht.
    private string CacheKey(string url)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        return Path.Combine(_cacheRoot, hash[..2], hash);
    }

    private static string ContentTypeFor(string url)
    {
        var ext = Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant();
        return ext switch
        {
            ".svg" => "image/svg+xml",
            ".gif" => "image/gif",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "image/png"
        };
    }

    private static async Task WriteCacheAsync(string path, byte[] bytes, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Über temporäre Datei + Move schreiben, damit parallele Anfragen keine halb geschriebene Datei lesen.
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temp, bytes, ct);
        File.Move(temp, path, overwrite: true);
    }
}
