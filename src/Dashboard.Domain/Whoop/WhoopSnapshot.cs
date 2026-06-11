namespace Dashboard.Domain.Whoop;

/// <summary>UI-fertige Sicht auf den aktuellen WHOOP-Tagesstatus. Wird in <see cref="WhoopState"/> gehalten.</summary>
public sealed record WhoopSnapshot(
    WhoopRecovery? Recovery,
    WhoopSleepSummary? Sleep,
    double? DayStrain,
    DateTimeOffset RetrievedAtUtc);

/// <summary>Recovery des letzten physiologischen Zyklus.</summary>
public sealed record WhoopRecovery(int ScorePercent, double HrvMillis, int RestingHeartRate)
{
    /// <summary>WHOOP-Ampel: grün ab 67 %, gelb 34–66 %, rot darunter.</summary>
    public WhoopRecoveryLevel Level => ScorePercent switch
    {
        >= 67 => WhoopRecoveryLevel.High,
        >= 34 => WhoopRecoveryLevel.Medium,
        _ => WhoopRecoveryLevel.Low
    };
}

/// <summary>Zusammenfassung des letzten Hauptschlafs (keine Naps).</summary>
public sealed record WhoopSleepSummary(int PerformancePercent, TimeSpan Asleep);

public enum WhoopRecoveryLevel
{
    Low,
    Medium,
    High
}
