using Dashboard.Domain.Common;

namespace Dashboard.Domain.Calendar;

/// <summary>The upcoming appointments last fetched from the calendar source, sorted by start.</summary>
public sealed record CalendarSnapshot(
    IReadOnlyList<CalendarEvent> Events,
    DateTimeOffset RetrievedAtUtc) : ISnapshot;
