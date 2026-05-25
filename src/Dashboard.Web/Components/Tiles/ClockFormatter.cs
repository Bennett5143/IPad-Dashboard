using System.Globalization;

namespace Dashboard.Web.Components.Tiles;

public readonly record struct ClockDisplay(string Iso, string Date, string Time);

public static class ClockFormatter
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public static ClockDisplay Format(DateTimeOffset utcNow, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(utcNow, timeZone);

        return new ClockDisplay(
            Iso:  local.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
            Date: local.ToString("ddd, dd.MM.yyyy", German),
            Time: local.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
        );
    }
}
