namespace Dashboard.Domain.Whoop;

/// <summary>
/// Merkt sich, welche WHOOP-Workouts bereits in Habits übernommen wurden – damit ein vom Nutzer
/// wieder abgehaktes Habit nicht beim nächsten Poll erneut gesetzt wird (jedes Workout genau einmal).
/// </summary>
public interface IWhoopProcessedWorkoutStore
{
    Task<IReadOnlySet<string>> GetProcessedAsync(
        IReadOnlyCollection<string> workoutIds, CancellationToken ct = default);

    Task MarkProcessedAsync(string workoutId, DateTimeOffset whenUtc, CancellationToken ct = default);
}
