namespace Dashboard.Infrastructure.Calendar;

/// <summary>
/// Configuration of the calendar source. Non-secret defaults (interval, timezone, window)
/// live in <c>appsettings.json</c> (section <see cref="SectionName"/>); the published
/// <see cref="IcsUrls"/> are private-but-not-secret and belong in <c>appsettings.Local.json</c>
/// (git-ignored). A published iCloud calendar link (or any https/webcal .ics subscription URL)
/// needs no password; a full CalDAV account password would instead go into User-Secrets.
/// </summary>
public sealed class CalendarOptions
{
    public const string SectionName = "Calendar";

    /// <summary>Published .ics subscription URLs (iCloud "Public Calendar" links, or any https/webcal ICS).</summary>
    public string[] IcsUrls { get; init; } = Array.Empty<string>();

    /// <summary>IANA timezone used to resolve all-day and floating events. Default: Europe/Berlin.</summary>
    public string TimeZone { get; init; } = "Europe/Berlin";

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>How many days ahead to materialize; recurrences are expanded within [today-1, today+WindowDays].</summary>
    public int WindowDays { get; init; } = 45;

    public int HttpTimeoutSeconds { get; init; } = 20;
}
