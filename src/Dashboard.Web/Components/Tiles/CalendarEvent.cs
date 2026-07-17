namespace Dashboard.Web.Components.Tiles;

/// <summary>
/// A single calendar appointment shown on the home page. This is a placeholder
/// shape for the e-ink home spike (slice 2); the calendar data-source slice
/// (Infrastructure/Calendar, CalDAV/ICS) replaces the dummy provider with real
/// events while keeping this UI-facing model.
/// </summary>
public sealed record CalendarEvent(string Title, DateTime Start, DateTime End)
{
    public TimeSpan Duration => End - Start;
}
