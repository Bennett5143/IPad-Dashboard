using Dashboard.Domain.Enums;
using Dashboard.Domain.Habits;
using Dashboard.Domain.Time;

namespace Dashboard.Domain.Whoop;

/// <summary>
/// Übernimmt die heutigen WHOOP-Workouts in den Habit-Tracker: hakt passende Habits ab und füllt
/// Lauf-Details (Dauer/Pace) aus WHOOP. Idempotent über <see cref="IWhoopProcessedWorkoutStore"/> –
/// jedes Workout wird genau einmal verarbeitet, manuelle Eingaben werden nicht überschrieben.
/// </summary>
public sealed class WhoopHabitSync
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private readonly IWhoopProvider _whoop;
    private readonly HabitTrackingService _habits;
    private readonly IWhoopProcessedWorkoutStore _processed;
    private readonly IClock _clock;

    public WhoopHabitSync(
        IWhoopProvider whoop,
        HabitTrackingService habits,
        IWhoopProcessedWorkoutStore processed,
        IClock clock)
    {
        _whoop = whoop;
        _habits = habits;
        _processed = processed;
        _clock = clock;
    }

    /// <returns>Anzahl neu übernommener Habits.</returns>
    public async Task<int> ApplyTodayAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var berlinNow = TimeZoneInfo.ConvertTime(now, BerlinTz);
        var today = DateOnly.FromDateTime(berlinNow.DateTime);
        var fromUtc = new DateTimeOffset(berlinNow.Date, berlinNow.Offset).ToUniversalTime();

        var workouts = await _whoop.GetWorkoutsAsync(fromUtc, now, ct);
        if (workouts.Count == 0)
        {
            return 0;
        }

        var processed = await _processed.GetProcessedAsync(workouts.Select(w => w.Id).ToList(), ct);
        var summaries = await _habits.GetSummaryForAsync(today, ct);
        var runDetailsPresent = summaries
            .Where(s => s.TodaysRunning is not null)
            .Select(s => s.Kind)
            .ToHashSet();

        var applied = 0;
        foreach (var workout in workouts)
        {
            if (processed.Contains(workout.Id))
            {
                continue;
            }

            var kind = WhoopHabitMapper.MapKind(workout);
            if (kind is { } k)
            {
                await _habits.CompleteAsync(today, k, ct);

                if (k is HabitKind.Zone2Run or HabitKind.Vo2MaxIntervals && !runDetailsPresent.Contains(k))
                {
                    var details = WhoopHabitMapper.BuildRunningDetails(workout);
                    if (details is not null)
                    {
                        await _habits.SaveRunningAsync(today, k, details.DurationMinutes, details.PaceMinPerKm, ct);
                        runDetailsPresent.Add(k);
                    }
                }

                applied++;
            }

            await _processed.MarkProcessedAsync(workout.Id, now, ct);
        }

        return applied;
    }
}
