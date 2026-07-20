using Dashboard.Domain.Calendar;
using Dashboard.Domain.Time;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Calendar;

/// <summary>
/// Fetches one or more published .ics subscription URLs and materializes the upcoming
/// appointments (recurrences expanded via Ical.Net) into the domain snapshot. A single
/// unreachable source is tolerated; only a total failure throws so the refresher can mark
/// the slice stale.
/// </summary>
public sealed class IcsCalendarClient : ICalendarProvider
{
    private readonly HttpClient _http;
    private readonly IClock _clock;
    private readonly CalendarOptions _options;
    private readonly ILogger<IcsCalendarClient> _logger;
    private readonly TimeZoneInfo _tz;

    public IcsCalendarClient(
        HttpClient http,
        IClock clock,
        IOptions<CalendarOptions> options,
        ILogger<IcsCalendarClient> logger)
    {
        _http = http;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
        _tz = ResolveTimeZone(_options.TimeZone);
    }

    public async Task<CalendarSnapshot> GetCalendarAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var localToday = TimeZoneInfo.ConvertTime(now, _tz).Date;
        var from = localToday.AddDays(-1);
        var to = localToday.AddDays(_options.WindowDays);

        var events = new List<CalendarEvent>();
        var anySucceeded = false;
        Exception? lastError = null;

        foreach (var url in _options.IcsUrls)
        {
            if (string.IsNullOrWhiteSpace(url)) continue;
            ct.ThrowIfCancellationRequested();
            try
            {
                var text = await _http.GetStringAsync(Normalize(url), ct);
                events.AddRange(Expand(text, from, to));
                anySucceeded = true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Kalender: ICS-Quelle nicht ladbar ({Host}).", HostOf(url));
            }
        }

        if (!anySucceeded && lastError is not null)
        {
            throw new InvalidOperationException("Keine Kalender-Quelle erreichbar.", lastError);
        }

        var ordered = events.OrderBy(e => e.Start).ToList();
        return new CalendarSnapshot(ordered, now);
    }

    private IEnumerable<CalendarEvent> Expand(string ics, DateTime from, DateTime to)
    {
        var calendar = Ical.Net.Calendar.Load(ics);
        if (calendar is null) yield break;

        // Ical.Net v5 GetOccurrences takes a single (inclusive) start and returns a
        // start-time-ordered, potentially infinite sequence; bound the upper end ourselves.
        var windowStart = new Ical.Net.DataTypes.CalDateTime(from, _options.TimeZone);
        var windowEnd = new Ical.Net.DataTypes.CalDateTime(to, _options.TimeZone);

        foreach (var occ in calendar.GetOccurrences(windowStart).TakeWhile(o => o.Period.StartTime < windowEnd))
        {
            if (occ.Source is not Ical.Net.CalendarComponents.CalendarEvent src) continue;

            var title = string.IsNullOrWhiteSpace(src.Summary) ? "(ohne Titel)" : src.Summary.Trim();

            if (src.IsAllDay)
            {
                var day = occ.Period.StartTime.Value.Date;
                var start = new DateTimeOffset(day, _tz.GetUtcOffset(day));
                yield return new CalendarEvent(title, start, start.AddDays(1), AllDay: true);
            }
            else
            {
                var start = ToOffset(occ.Period.StartTime);
                var end = occ.Period.EndTime is { } e ? ToOffset(e) : start.AddHours(1);
                yield return new CalendarEvent(title, start, end);
            }
        }
    }

    private static DateTimeOffset ToOffset(Ical.Net.DataTypes.CalDateTime value)
        => new(DateTime.SpecifyKind(value.AsUtc, DateTimeKind.Utc));

    private static string Normalize(string url)
        => url.StartsWith("webcal://", StringComparison.OrdinalIgnoreCase)
            ? "https://" + url["webcal://".Length..]
            : url;

    // The ICS path/token is semi-secret; only log the host.
    private static string HostOf(string url)
        => Uri.TryCreate(Normalize(url), UriKind.Absolute, out var u) ? u.Host : "ungültige URL";

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}
