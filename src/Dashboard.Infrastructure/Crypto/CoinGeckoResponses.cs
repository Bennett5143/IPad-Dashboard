using System.Text.Json.Serialization;

namespace Dashboard.Infrastructure.Crypto;

/// <summary>Ein Eintrag des CoinGecko-<c>coins/markets</c>-Endpunkts (nur genutzte Felder).</summary>
internal sealed record CoinGeckoMarket(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("image")] string? Image,
    [property: JsonPropertyName("current_price")] decimal? CurrentPrice,
    [property: JsonPropertyName("price_change_percentage_24h")] double? PriceChangePercentage24h,
    [property: JsonPropertyName("sparkline_in_7d")] CoinGeckoSparkline? Sparkline);

internal sealed record CoinGeckoSparkline(
    [property: JsonPropertyName("price")] IReadOnlyList<double>? Price);

/// <summary>
/// Antwort von <c>coins/{id}/market_chart</c>: Paare aus Zeitstempel (Millisekunden seit Epoch)
/// und Kurs. Nur <c>prices</c> wird gelesen — Marktkapitalisierung und Volumen interessieren hier nicht.
/// </summary>
internal sealed record CoinGeckoMarketChart(
    [property: JsonPropertyName("prices")] IReadOnlyList<decimal[]> Prices);
