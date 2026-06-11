namespace Dashboard.Domain.Whoop;

/// <summary>
/// Zusammengeführte WHOOP-Tageswerte (ein Punkt pro Kalendertag, Berlin) für Verlaufs-Auswertungen.
/// Einzelne Felder können fehlen, wenn WHOOP für den Tag (noch) keinen Wert hat.
/// </summary>
public sealed record WhoopDailyMetric(
    DateOnly Date,
    int? RecoveryScore,
    double? HrvMillis,
    int? RestingHeartRate,
    double? SleepHours,
    int? SleepPerformance,
    double? DayStrain)
{
    public WhoopRecoveryLevel? RecoveryLevel =>
        RecoveryScore is { } score ? WhoopRecovery.LevelFor(score) : null;
}
