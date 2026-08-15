namespace Dashboard.Domain.Crypto;

/// <summary>Die Zeiträume der Coin-Detailseite.</summary>
public enum CoinRange
{
    Hour,
    Day,
    Week,
    Month,
    Year
}

/// <summary>
/// Ein Kursverlauf für einen Zeitraum: die Punkte in zeitlicher Reihenfolge plus die Eckwerte,
/// die die Achsen beschriften. <paramref name="Points"/> ist leer, wenn die Quelle nichts lieferte.
/// </summary>
public sealed record CoinHistory(
    string CoinId,
    CoinRange Range,
    IReadOnlyList<CoinHistoryPoint> Points)
{
    public static CoinHistory Empty(string coinId, CoinRange range) => new(coinId, range, []);

    public bool HasPoints => Points.Count > 0;

    public decimal Minimum => Points.Min(point => point.Price);
    public decimal Maximum => Points.Max(point => point.Price);
    public DateTimeOffset From => Points[0].At;
    public DateTimeOffset To => Points[^1].At;
}

/// <summary>Ein Kurspunkt: Zeitstempel und Preis in der konfigurierten Währung.</summary>
public sealed record CoinHistoryPoint(DateTimeOffset At, decimal Price);

/// <summary>
/// Liefert Kursverläufe je Coin und Zeitraum. Getrennt von der Watchlist, weil hier
/// <em>auf Anforderung</em> geholt wird: elf Coins mal fünf Zeiträume im Hintergrund würden die
/// kostenlose Quelle dauerhaft belasten, obwohl fast nie jemand hinsieht.
/// </summary>
public interface ICoinHistoryProvider
{
    Task<CoinHistory> GetHistoryAsync(string coinId, CoinRange range, CancellationToken ct = default);
}
