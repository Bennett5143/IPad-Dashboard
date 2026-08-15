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
    DateTimeOffset RetrievedAtUtc,
    IReadOnlyList<MarketDay>? DailyChanges = null) : ISnapshot
{
    /// <summary>Die Tagesveränderungen für das Badge im Wochenkalender; leer, wenn keine vorliegen.</summary>
    public IReadOnlyList<MarketDay> Days => DailyChanges ?? [];

    /// <summary>Die Veränderungen eines Kalendertags; leer, wenn für den Tag nichts vorliegt.</summary>
    public IReadOnlyList<DailyChange> ChangesOn(DateOnly date) =>
        Days.FirstOrDefault(day => day.Date == date)?.Changes ?? [];

    /// <summary>
    /// Leit-Münze für die Summary-Kachel (per <see cref="SummaryCoinId"/>); fällt auf die
    /// erste – also kapitalstärkste – Münze zurück, falls die konfigurierte Id fehlt.
    /// </summary>
    public CoinQuote? Summary =>
        Coins.FirstOrDefault(c => string.Equals(c.Id, SummaryCoinId, StringComparison.OrdinalIgnoreCase))
        ?? (Coins.Count > 0 ? Coins[0] : null);
}
