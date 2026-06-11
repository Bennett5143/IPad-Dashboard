using System.Text.Json.Serialization;

namespace Dashboard.Infrastructure.Whoop;

/// <summary>OAuth-Token-Antwort von WHOOP (<c>expires_in</c> relativ in Sekunden).</summary>
internal sealed class WhoopTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; } = string.Empty;
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; init; } = string.Empty;
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
}

/// <summary>Generischer Listen-Wrapper der v2-Endpunkte (<c>records</c> + <c>next_token</c>).</summary>
internal sealed class WhoopCollection<T>
{
    [JsonPropertyName("records")] public IReadOnlyList<T> Records { get; init; } = [];
}

internal sealed class WhoopRecoveryRecord
{
    [JsonPropertyName("score_state")] public string? ScoreState { get; init; }
    [JsonPropertyName("score")] public WhoopRecoveryScore? Score { get; init; }
}

internal sealed class WhoopRecoveryScore
{
    [JsonPropertyName("recovery_score")] public double RecoveryScore { get; init; }
    [JsonPropertyName("resting_heart_rate")] public double RestingHeartRate { get; init; }
    [JsonPropertyName("hrv_rmssd_milli")] public double HrvRmssdMilli { get; init; }
}

internal sealed class WhoopSleepRecord
{
    [JsonPropertyName("nap")] public bool Nap { get; init; }
    [JsonPropertyName("score_state")] public string? ScoreState { get; init; }
    [JsonPropertyName("score")] public WhoopSleepScore? Score { get; init; }
}

internal sealed class WhoopSleepScore
{
    [JsonPropertyName("sleep_performance_percentage")] public double SleepPerformancePercentage { get; init; }
    [JsonPropertyName("stage_summary")] public WhoopSleepStageSummary? StageSummary { get; init; }
}

internal sealed class WhoopSleepStageSummary
{
    [JsonPropertyName("total_light_sleep_time_milli")] public long LightMilli { get; init; }
    [JsonPropertyName("total_slow_wave_sleep_time_milli")] public long SlowWaveMilli { get; init; }
    [JsonPropertyName("total_rem_sleep_time_milli")] public long RemMilli { get; init; }
}

internal sealed class WhoopCycleRecord
{
    [JsonPropertyName("score_state")] public string? ScoreState { get; init; }
    [JsonPropertyName("score")] public WhoopCycleScore? Score { get; init; }
}

internal sealed class WhoopCycleScore
{
    [JsonPropertyName("strain")] public double Strain { get; init; }
}

internal sealed class WhoopWorkoutRecord
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("sport_name")] public string? SportName { get; init; }
    [JsonPropertyName("start")] public DateTimeOffset Start { get; init; }
    [JsonPropertyName("end")] public DateTimeOffset End { get; init; }
    [JsonPropertyName("score_state")] public string? ScoreState { get; init; }
    [JsonPropertyName("score")] public WhoopWorkoutScore? Score { get; init; }
}

internal sealed class WhoopWorkoutScore
{
    [JsonPropertyName("distance_meter")] public double? DistanceMeter { get; init; }
    [JsonPropertyName("zone_durations")] public WhoopZoneDurations? ZoneDurations { get; init; }
}

internal sealed class WhoopZoneDurations
{
    [JsonPropertyName("zone_zero_milli")] public long ZoneZero { get; init; }
    [JsonPropertyName("zone_one_milli")] public long ZoneOne { get; init; }
    [JsonPropertyName("zone_two_milli")] public long ZoneTwo { get; init; }
    [JsonPropertyName("zone_three_milli")] public long ZoneThree { get; init; }
    [JsonPropertyName("zone_four_milli")] public long ZoneFour { get; init; }
    [JsonPropertyName("zone_five_milli")] public long ZoneFive { get; init; }

    private long Total => ZoneZero + ZoneOne + ZoneTwo + ZoneThree + ZoneFour + ZoneFive;

    /// <summary>Zeitanteil in den HF-Zonen 4+5 (0..1).</summary>
    public double HighIntensityShare => Total == 0 ? 0 : (double)(ZoneFour + ZoneFive) / Total;
}
