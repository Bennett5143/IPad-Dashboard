using System.Globalization;

namespace Dashboard.Web.Components.Tiles;

public readonly record struct ClockDisplay(
    string Iso,
    string Date,
    string Time,
    string TimeHm,
    string Seconds,
    string LongDate,
    string Week);

public static class ClockFormatter
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public static ClockDisplay Format(DateTimeOffset utcNow, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(utcNow, timeZone);

        return new ClockDisplay(
            Iso: local.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
            Date: local.ToString("ddd, dd.MM.yyyy", German),
            Time: local.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            TimeHm: local.ToString("HH:mm", CultureInfo.InvariantCulture),
            Seconds: local.ToString("ss", CultureInfo.InvariantCulture),
            LongDate: local.ToString("dddd, d. MMMM", German),
            Week: $"KW {ISOWeek.GetWeekOfYear(local.DateTime)}"
        );
    }
}
