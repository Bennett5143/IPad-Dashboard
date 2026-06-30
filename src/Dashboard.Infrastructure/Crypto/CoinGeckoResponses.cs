using System.Text.Json.Serialization;

namespace Dashboard.Infrastructure.Crypto;

/// <summary>Ein Eintrag des CoinGecko-<c>coins/markets</c>-Endpunkts (nur genutzte Felder).</summary>
internal sealed record CoinGeckoMarket(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("current_price")] decimal? CurrentPrice,
    [property: JsonPropertyName("price_change_percentage_24h")] double? PriceChangePercentage24h,
    [property: JsonPropertyName("market_cap")] decimal? MarketCap,
    [property: JsonPropertyName("sparkline_in_7d")] CoinGeckoSparkline? Sparkline);

internal sealed record CoinGeckoSparkline(
    [property: JsonPropertyName("price")] IReadOnlyList<double>? Price);
