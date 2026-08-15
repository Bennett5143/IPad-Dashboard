namespace Dashboard.Domain.Crypto;

/// <summary>
/// Liefert die Tagesveränderungen der Leit-Münze für das Badge im Wochenkalender. Getrennt von
/// <see cref="ICryptoMarketProvider"/>, weil die Watchlist Pflicht ist und diese Reihe nicht: fällt
/// sie aus, fehlt ein Badge — die Kurse bleiben davon unberührt.
/// </summary>
public interface ICryptoHistoryProvider
{
    /// <summary>
    /// Prozentuale Veränderung je Kalendertag (Berlin), aufsteigend sortiert, jüngster Tag zuletzt.
    /// Der jüngste Tag ist der laufende und trägt die Veränderung bis jetzt.
    /// </summary>
    Task<IReadOnlyList<MarketDay>> GetDailyChangesAsync(CancellationToken ct = default);
}
