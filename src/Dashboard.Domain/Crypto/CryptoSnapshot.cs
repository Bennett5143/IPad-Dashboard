using Dashboard.Domain.Common;

namespace Dashboard.Domain.Crypto;

/// <summary>
/// UI-fertige Sicht auf die Krypto-Watchlist: Markt-Quotes (nach Marktkapitalisierung
/// sortiert) plus optionale Marktstimmung. Wird vom Background-Service erzeugt und in
/// <see cref="CryptoState"/> gehalten.
/// </summary>
public sealed record CryptoSnapshot(
    IReadOnlyList<CoinQuote> Coins,
    MarketSentiment? Sentiment,
    string SummaryCoinId,
    DateTimeOffset RetrievedAtUtc) : ISnapshot
{
    /// <summary>
    /// Leit-Münze für die Summary-Kachel (per <see cref="SummaryCoinId"/>); fällt auf die
    /// erste – also kapitalstärkste – Münze zurück, falls die konfigurierte Id fehlt.
    /// </summary>
    public CoinQuote? Summary =>
        Coins.FirstOrDefault(c => string.Equals(c.Id, SummaryCoinId, StringComparison.OrdinalIgnoreCase))
        ?? (Coins.Count > 0 ? Coins[0] : null);
}
