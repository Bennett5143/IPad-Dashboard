using System.Net.Http.Json;

using Dashboard.Domain.Crypto;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Crypto;

/// <summary>
/// Kursverläufe für die Coin-Detailseite, auf Anforderung geholt und zwischengespeichert.
/// <para>
/// CoinGecko drosselt den freien Zugang hart. Elf Coins mal fünf Zeiträume dauerhaft im
/// Hintergrund mitzuführen, wäre für eine Seite, die selten offen ist, verschwendet — deshalb
/// wird erst geholt, wenn jemand hinsieht, und das Ergebnis so lange behalten, wie es für den
/// Zeitraum sinnvoll ist: Stunde und Tag kurz, das Jahr lang.
/// </para>
/// </summary>
public sealed class CoinHistoryClient : ICoinHistoryProvider
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly CryptoOptions _options;
    private readonly ILogger<CoinHistoryClient> _logger;

    public CoinHistoryClient(
        HttpClient http,
        IMemoryCache cache,
        IOptions<CryptoOptions> options,
        ILogger<CoinHistoryClient> logger)
    {
        _http = http;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CoinHistory> GetHistoryAsync(
        string coinId, CoinRange range, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(coinId))
        {
            return CoinHistory.Empty(coinId, range);
        }

        var key = $"coin-history:{coinId}:{range}";
        if (_cache.TryGetValue<CoinHistory>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var history = await FetchAsync(coinId, range, ct);
        if (history.HasPoints)
        {
            _cache.Set(key, history, Lifetime(range));
        }

        return history;
    }

    private async Task<CoinHistory> FetchAsync(string coinId, CoinRange range, CancellationToken ct)
    {
        var vs = Uri.EscapeDataString(_options.VsCurrency);
        var uri = $"api/v3/coins/{Uri.EscapeDataString(coinId)}/market_chart" +
                  $"?vs_currency={vs}&days={Days(range)}";

        try
        {
            var chart = await _http.GetFromJsonAsync<CoinGeckoMarketChart>(uri, ct);
            if (chart is null)
            {
                return CoinHistory.Empty(coinId, range);
            }

            var points = chart.Prices
                .Where(static point => point.Length >= 2)
                .Select(static point => new CoinHistoryPoint(
                    DateTimeOffset.FromUnixTimeMilliseconds((long)point[0]), point[1]))
                .ToList();

            return new CoinHistory(coinId, range, Trim(points, range));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Ein Verlauf ist nie kritisch: die Seite sagt „gerade nicht verfügbar", die
            // Kursliste bleibt unberührt, und der nächste Aufruf versucht es erneut.
            _logger.LogWarning(ex, "Krypto: Verlauf für {Coin} ({Range}) nicht abrufbar.", coinId, range);
            return CoinHistory.Empty(coinId, range);
        }
    }

    /// <summary>
    /// Die Stunde hat kein eigenes Fenster bei CoinGecko — sie wird aus dem Tagesverlauf
    /// (Fünf-Minuten-Punkte) herausgeschnitten.
    /// </summary>
    private static IReadOnlyList<CoinHistoryPoint> Trim(
        IReadOnlyList<CoinHistoryPoint> points, CoinRange range)
    {
        if (range != CoinRange.Hour || points.Count == 0)
        {
            return points;
        }

        var cutoff = points[^1].At.AddHours(-1);
        var lastHour = points.Where(point => point.At >= cutoff).ToList();

        // Zwei Punkte sind das Minimum für eine Linie; sonst lieber den vollen Tag zeigen.
        return lastHour.Count >= 2 ? lastHour : points;
    }

    private static int Days(CoinRange range) => range switch
    {
        CoinRange.Hour => 1,
        CoinRange.Day => 1,
        CoinRange.Week => 7,
        CoinRange.Month => 30,
        _ => 365,
    };

    private static TimeSpan Lifetime(CoinRange range) => range switch
    {
        CoinRange.Hour => TimeSpan.FromMinutes(5),
        CoinRange.Day => TimeSpan.FromMinutes(5),
        CoinRange.Week => TimeSpan.FromMinutes(30),
        CoinRange.Month => TimeSpan.FromHours(6),
        _ => TimeSpan.FromHours(24),
    };
}
