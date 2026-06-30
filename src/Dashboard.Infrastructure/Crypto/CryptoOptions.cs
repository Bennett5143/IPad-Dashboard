namespace Dashboard.Infrastructure.Crypto;

/// <summary>
/// Konfiguration des Krypto-Slices. Beide Quellen sind schlüssellos und frei
/// (CoinGecko-Markt + alternative.me-Stimmung); es gibt daher kein Secret.
/// </summary>
public sealed class CryptoOptions
{
    public const string SectionName = "Crypto";

    public string MarketBaseUrl { get; init; } = "https://api.coingecko.com/";
    public string SentimentBaseUrl { get; init; } = "https://api.alternative.me/";

    /// <summary>Fiat-Währung der Kurse (CoinGecko-Code, klein).</summary>
    public string VsCurrency { get; init; } = "eur";

    /// <summary>
    /// CoinGecko-Slugs der Watchlist. Reihenfolge egal – die Anzeige sortiert nach
    /// Marktkapitalisierung. In <c>appsettings.Local.json</c> überschreibbar.
    /// </summary>
    public IReadOnlyList<string> CoinIds { get; init; } =
        ["bitcoin", "ethereum", "solana"];

    /// <summary>Leit-Münze der Summary-Kachel (CoinGecko-Slug).</summary>
    public string SummaryCoinId { get; init; } = "bitcoin";

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(5);
}
