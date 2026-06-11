using System.Globalization;

namespace Dashboard.Web.Components.Tiles;

/// <summary>Reine Darstellungs-Helfer für die WHOOP-Kachel.</summary>
public static class WhoopFormatter
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static string RecoveryScore(WhoopRecovery? recovery) =>
        recovery is null ? "–" : $"{recovery.ScorePercent}%";

    public static string RecoveryCss(WhoopRecoveryLevel? level) => level switch
    {
        WhoopRecoveryLevel.High => "recovery-high",
        WhoopRecoveryLevel.Medium => "recovery-medium",
        WhoopRecoveryLevel.Low => "recovery-low",
        _ => string.Empty
    };

    public static string Hrv(WhoopRecovery? recovery) =>
        recovery is null ? "–" : $"{recovery.HrvMillis.ToString("0", German)} ms";

    public static string RestingHr(WhoopRecovery? recovery) =>
        recovery is null ? "–" : $"{recovery.RestingHeartRate} bpm";

    public static string Sleep(WhoopSleepSummary? sleep) =>
        sleep is null ? "–" : $"{(int)sleep.Asleep.TotalHours}:{sleep.Asleep.Minutes:00} h · {sleep.PerformancePercent}%";

    public static string Strain(double? dayStrain) =>
        dayStrain is null ? "–" : dayStrain.Value.ToString("0.0", German);

    public static string UpdatedAt(DateTimeOffset retrievedAtUtc) =>
        TimeZoneInfo.ConvertTime(retrievedAtUtc, BerlinTz)
            .ToString("HH:mm", CultureInfo.InvariantCulture);
}
