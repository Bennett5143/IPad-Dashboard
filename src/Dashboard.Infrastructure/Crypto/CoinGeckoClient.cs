using System.Net.Http.Json;

using Dashboard.Domain.Crypto;

using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Crypto;

/// <summary>
/// <see cref="ICryptoMarketProvider"/> auf Basis des freien CoinGecko-Endpunkts
/// <c>api/v3/coins/markets</c> (kein API-Key). Holt Kurs, 24-h-Änderung und die
/// 7-Tage-Sparkline in einem Aufruf; CoinGecko liefert bereits nach Marktkapitalisierung sortiert.
/// Über <see cref="ICryptoHistoryProvider"/> kommt zusätzlich die Tagesreihe der Leit-Münze
/// (<c>api/v3/coins/{id}/market_chart</c>) für das Badge im Wochenkalender.
/// </summary>
public sealed class CoinGeckoClient : ICryptoMarketProvider, ICryptoHistoryProvider
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>
    /// Abgefragtes Fenster der Tagesreihe. Acht Tage decken die laufende Woche auch am Montag ab —
    /// der erste Tag dient nur als Vergleichswert für den zweiten.
    /// </summary>
    private const int HistoryDays = 8;

    private readonly HttpClient _http;
    private readonly CryptoOptions _options;

    public CoinGeckoClient(HttpClient http, IOptions<CryptoOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<CoinQuote>> GetMarketAsync(CancellationToken ct = default)
    {
        if (_options.CoinIds.Count == 0)
        {
            return [];
        }

        var ids = Uri.EscapeDataString(string.Join(',', _options.CoinIds));
        var vs = Uri.EscapeDataString(_options.VsCurrency);
        var uri = $"api/v3/coins/markets?vs_currency={vs}&ids={ids}" +
                  "&order=market_cap_desc&price_change_percentage=24h&sparkline=true";

        var markets = await _http.GetFromJsonAsync<IReadOnlyList<CoinGeckoMarket>>(uri, ct)
            ?? throw new InvalidOperationException("Leere Antwort vom CoinGecko-Markt-Endpoint.");

        return markets
            .Where(static m => m.CurrentPrice is not null)
            .Select(ToQuote)
            .ToList();
    }

    /// <summary>
    /// Tagesveränderungen der Leit-Münze. Bewusst <em>ohne</em> <c>interval=daily</c>: der Parameter
    /// ist kostenpflichtigen Plänen vorbehalten, und die automatische Stundenauflösung im
    /// 2-bis-90-Tage-Fenster ist ohnehin genauer — daraus lassen sich die Tagesgrenzen in Berliner
    /// Zeit ziehen statt in UTC. Je Kalendertag zählt der letzte Kurs des Tages; für heute ist das
    /// der aktuelle, also die Veränderung bis jetzt.
    /// </summary>
    public async Task<IReadOnlyList<MarketDay>> GetDailyChangesAsync(CancellationToken ct = default)
    {
        var id = _options.SummaryCoinId;
        if (string.IsNullOrWhiteSpace(id))
        {
            return [];
        }

        var vs = Uri.EscapeDataString(_options.VsCurrency);
        var uri = $"api/v3/coins/{Uri.EscapeDataString(id)}/market_chart?vs_currency={vs}&days={HistoryDays}";

        var chart = await _http.GetFromJsonAsync<CoinGeckoMarketChart>(uri, ct)
            ?? throw new InvalidOperationException("Leere Antwort vom CoinGecko-Verlaufs-Endpoint.");

        return ToDailyChanges(chart, SymbolOf(id));
    }

    /// <summary>
    /// Schlusskurs je Berliner Kalendertag bilden und daraus die Veränderung zum Vortag. Rein und
    /// ohne HTTP — der Teil, der schiefgehen kann, ist damit testbar.
    /// </summary>
    private static IReadOnlyList<MarketDay> ToDailyChanges(CoinGeckoMarketChart chart, string symbol)
    {
        var closes = chart.Prices
            .Where(static point => point.Length >= 2)
            .Select(static point => (
                Date: DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTime(
                        DateTimeOffset.FromUnixTimeMilliseconds((long)point[0]), BerlinTz).DateTime),
                Price: point[1]))
            .GroupBy(static point => point.Date)
            .OrderBy(static group => group.Key)
            .Select(static group => (group.Key, Close: group.Last().Price))
            .ToList();

        var days = new List<MarketDay>(Math.Max(0, closes.Count - 1));
        for (var i = 1; i < closes.Count; i++)
        {
            var previous = closes[i - 1].Close;
            if (previous <= 0)
            {
                continue;
            }

            var change = (closes[i].Close - previous) / previous * 100m;
            days.Add(new MarketDay(closes[i].Key, [new DailyChange(symbol, change)]));
        }

        return days;
    }

    // Für die geführten Münzen ist das Kürzel bekannt; alles andere trägt seinen Slug in Großbuchstaben.
    private static string SymbolOf(string coinId) => coinId.ToLowerInvariant() switch
    {
        "bitcoin" => "BTC",
        "ethereum" => "ETH",
        "solana" => "SOL",
        _ => coinId.ToUpperInvariant()
    };

    private static CoinQuote ToQuote(CoinGeckoMarket m) => new(
        m.Id,
        m.Symbol.ToUpperInvariant(),
        m.Name,
        m.CurrentPrice ?? 0m,
        m.PriceChangePercentage24h ?? 0d,
        m.MarketCap,
        m.Sparkline?.Price is { } prices
            ? prices.Select(static p => (double?)p).ToList()
            : []);
}
