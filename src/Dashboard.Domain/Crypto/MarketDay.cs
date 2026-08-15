namespace Dashboard.Domain.Crypto;

/// <summary>Ein Wert im Tages-Badge des Wochenkalenders: Symbol plus Tagesveränderung in Prozent.</summary>
public sealed record DailyChange(string Symbol, decimal ChangePercent);

/// <summary>
/// Die Kursveränderungen eines Kalendertags (Berlin). Bewusst eine <em>Liste</em> von Werten je Tag:
/// heute trägt sie nur Bitcoin, ein Index daneben soll später ohne Formatänderung dazukommen.
/// </summary>
public sealed record MarketDay(DateOnly Date, IReadOnlyList<DailyChange> Changes);
