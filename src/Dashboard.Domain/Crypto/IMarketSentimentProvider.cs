namespace Dashboard.Domain.Crypto;

/// <summary>
/// Liefert die Marktstimmung (Fear-&amp;-Greed). Bewusst getrennt vom Markt-Provider:
/// andere BaseUrl, und ein Ausfall ist best-effort und darf die Kurse nie blockieren.
/// </summary>
public interface IMarketSentimentProvider
{
    Task<MarketSentiment?> GetSentimentAsync(CancellationToken ct = default);
}
