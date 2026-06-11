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
