using System.Net.Http.Json;

using Dashboard.Domain.Crypto;

using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Crypto;

/// <summary>
/// <see cref="ICryptoMarketProvider"/> auf Basis des freien CoinGecko-Endpunkts
/// <c>api/v3/coins/markets</c> (kein API-Key). Holt Kurs, 24-h-Änderung und die
/// 7-Tage-Sparkline in einem Aufruf; CoinGecko liefert bereits nach Marktkapitalisierung sortiert.
/// </summary>
public sealed class CoinGeckoClient : ICryptoMarketProvider
{
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
