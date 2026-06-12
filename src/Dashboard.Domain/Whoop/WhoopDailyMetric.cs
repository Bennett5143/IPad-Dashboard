namespace Dashboard.Domain.Whoop;

/// <summary>
/// Zusammengeführte WHOOP-Tageswerte (ein Punkt pro Kalendertag, Berlin) für Verlaufs-Auswertungen.
/// Einzelne Felder können fehlen, wenn WHOOP für den Tag (noch) keinen Wert hat.
/// </summary>
/// <remarks>
/// Die Schlafphasen-, Schlafzeiten- und Vitalfelder (ab FA-9.11) sind optional, damit bestehende
/// Aufrufer unverändert bleiben; sie definieren zugleich das Zielschema der späteren
/// Tages-Persistenz (FA-9.10). <see cref="SleepStartUtc"/>/<see cref="SleepEndUtc"/> beziehen sich
/// auf den Hauptschlaf, dessen Ende diesem Kalendertag zugeordnet ist.
/// </remarks>
public sealed record WhoopDailyMetric(
    DateOnly Date,
    int? RecoveryScore,
    double? HrvMillis,
    int? RestingHeartRate,
    double? SleepHours,
    int? SleepPerformance,
    double? DayStrain,
    double? LightSleepHours = null,
    double? DeepSleepHours = null,
    double? RemSleepHours = null,
    double? AwakeHours = null,
    DateTimeOffset? SleepStartUtc = null,
    DateTimeOffset? SleepEndUtc = null,
    double? RespiratoryRate = null,
    double? Spo2Percentage = null,
    double? SkinTempCelsius = null)
{
    public WhoopRecoveryLevel? RecoveryLevel =>
        RecoveryScore is { } score ? WhoopRecovery.LevelFor(score) : null;
}
