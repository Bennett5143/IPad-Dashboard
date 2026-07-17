namespace Dashboard.Domain.Calendar;

/// <summary>Fetches and materializes upcoming appointments from the configured calendar source.</summary>
public interface ICalendarProvider
{
    Task<CalendarSnapshot> GetCalendarAsync(CancellationToken ct = default);
}
