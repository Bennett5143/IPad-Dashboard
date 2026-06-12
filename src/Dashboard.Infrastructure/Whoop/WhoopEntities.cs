namespace Dashboard.Infrastructure.Whoop;

/// <summary>Single-Row-Entity (Id = 1) mit dem aktuellen WHOOP-OAuth-Token-Satz.</summary>
internal sealed class WhoopTokenEntity
{
    public int Id { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

/// <summary>Bereits in Habits übernommene WHOOP-Workouts (UUID), damit jedes nur einmal verarbeitet wird.</summary>
internal sealed class WhoopProcessedWorkoutEntity
{
    public string WorkoutId { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAtUtc { get; set; }
}

/// <summary>
/// Persistierte WHOOP-Tageswerte (FA-9.10), ein Datensatz pro Berliner Kalendertag.
/// Spalten spiegeln <see cref="Dashboard.Domain.Whoop.WhoopDailyMetric"/>; alles nullable,
/// weil WHOOP einzelne Werte erst später (oder nie) liefert.
/// </summary>
internal sealed class WhoopDailyMetricEntity
{
    public DateOnly Date { get; set; }
    public int? RecoveryScore { get; set; }
    public double? HrvMillis { get; set; }
    public int? RestingHeartRate { get; set; }
    public double? SleepHours { get; set; }
    public int? SleepPerformance { get; set; }
    public double? DayStrain { get; set; }
    public double? LightSleepHours { get; set; }
    public double? DeepSleepHours { get; set; }
    public double? RemSleepHours { get; set; }
    public double? AwakeHours { get; set; }
    public DateTimeOffset? SleepStartUtc { get; set; }
    public DateTimeOffset? SleepEndUtc { get; set; }
    public double? RespiratoryRate { get; set; }
    public double? Spo2Percentage { get; set; }
    public double? SkinTempCelsius { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
