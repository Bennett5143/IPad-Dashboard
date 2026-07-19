namespace Dashboard.Domain.Calendar;

/// <summary>
/// A single calendar appointment shown on the home page. Times are absolute
/// (<see cref="DateTimeOffset"/>); all-day events span local midnight to midnight.
/// </summary>
public sealed record CalendarEvent(string Title, DateTimeOffset Start, DateTimeOffset End, bool AllDay = false)
{
    public TimeSpan Duration => End - Start;
}
