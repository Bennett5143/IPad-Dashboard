using Dashboard.Domain.Entities;
using Dashboard.Domain.Enums;
using Dashboard.Domain.Time;
using Dashboard.Domain.ValueObjects;

namespace Dashboard.Domain.Habits;

public sealed class HabitTrackingService
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private readonly IHabitEntryRepository _repository;
    private readonly IClock _clock;

    public HabitTrackingService(IHabitEntryRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public DateOnly GetCurrentLocalDate()
    {
        var local = TimeZoneInfo.ConvertTime(_clock.UtcNow, BerlinTz);
        return DateOnly.FromDateTime(local.DateTime);
    }

    public async Task<IReadOnlyList<HabitSummary>> GetSummaryForAsync(
        DateOnly date, CancellationToken ct = default)
    {
        var (weekStart, weekEnd) = HabitWeek.ContainingDate(date);
        var yearStart = new DateOnly(date.Year, 1, 1);
        var yearEnd = new DateOnly(date.Year, 12, 31);

        var completedToday = await _repository.GetCompletedKindsAsync(date, ct);
        var weekCounts = await _repository.CountByKindAsync(weekStart, weekEnd, ct);
        var yearCounts = await _repository.CountByKindAsync(yearStart, yearEnd, ct);

        var todaysEmom = await _repository.GetEmomAsync(date, ct);
        var running = await _repository.GetRunningForDateAsync(date, ct);

        return Enum.GetValues<HabitKind>()
            .Select(kind => new HabitSummary(
                Kind: kind,
                IsDoneToday: completedToday.Contains(kind),
                WeekCount: weekCounts.GetValueOrDefault(kind, 0),
                YearCount: yearCounts.GetValueOrDefault(kind, 0),
                TodaysEmom: kind == HabitKind.Strength ? todaysEmom : null,
                TodaysRunning: running.GetValueOrDefault(kind)))
            .ToList();
    }

    public async Task ToggleAsync(DateOnly date, HabitKind kind, CancellationToken ct = default)
    {
        var existing = await _repository.GetAsync(date, kind, ct);
        if (existing is null)
        {
            await _repository.AddAsync(new HabitEntry { Date = date, Kind = kind }, ct);
        }
        else
        {
            await _repository.RemoveAsync(existing, ct);
        }
    }

    public async Task SaveEmomAsync(
        DateOnly date, IReadOnlyList<EmomSegment> segments, CancellationToken ct = default)
    {
        var error = EmomWorkoutRules.ValidateSegments(segments);
        if (error is not null)
            throw new ArgumentException(error, nameof(segments));

        await _repository.UpsertEmomAsync(date, segments, ct);
    }

    public async Task SaveRunningAsync(
        DateOnly date, HabitKind kind, int durationMinutes, decimal paceMinPerKm,
        CancellationToken ct = default)
    {
        if (kind is not (HabitKind.Zone2Run or HabitKind.Vo2MaxIntervals))
            throw new ArgumentException("Lauf-Details gibt es nur für Lauf-Habits.", nameof(kind));

        var error = RunningDetailsRules.Validate(durationMinutes, paceMinPerKm);
        if (error is not null) throw new ArgumentException(error);

        await _repository.UpsertRunningAsync(
            date, kind, new RunningDetails(durationMinutes, paceMinPerKm), ct);
    }
}
