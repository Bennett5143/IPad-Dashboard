namespace Dashboard.Web.Components;

/// <summary>
/// Decides which paper theme is active: the day theme between sunrise and sunset,
/// the night theme otherwise. Sunset-driven when the weather slice has sun times,
/// with a fixed local-hour fallback (day 06:00–21:00) until it does. Switching only
/// swaps token values, so it never shifts layout (PRD §7).
/// </summary>
public static class ThemeResolver
{
    public const string Day = "eink";
    public const string Night = "night";

    public static string Resolve(
        DateTimeOffset nowUtc,
        DateTimeOffset? sunriseUtc,
        DateTimeOffset? sunsetUtc,
        TimeZoneInfo timeZone)
    {
        if (sunriseUtc is { } sunrise && sunsetUtc is { } sunset && sunset > sunrise)
        {
            return nowUtc >= sunrise && nowUtc < sunset ? Day : Night;
        }

        var localHour = TimeZoneInfo.ConvertTime(nowUtc, timeZone).Hour;
        return localHour is >= 6 and < 21 ? Day : Night;
    }
}
