namespace Dashboard.Web.Components;

/// <summary>
/// Picks the active paper theme by local clock: the day theme from 08:00 to 20:00,
/// the night theme from 20:00 to 08:00. Switching only swaps token values, so it
/// never shifts layout. App.razor uses this for the initial server-side
/// value; a tiny inline script keeps a running kiosk in sync (see App.razor).
/// </summary>
public static class ThemeResolver
{
    public const string Day = "eink";
    public const string Night = "night";

    /// <summary>Day hour range is [DayStartHour, NightStartHour); night otherwise.</summary>
    public const int DayStartHour = 8;
    public const int NightStartHour = 20;

    public static string Resolve(DateTimeOffset nowUtc, TimeZoneInfo timeZone)
    {
        var hour = TimeZoneInfo.ConvertTime(nowUtc, timeZone).Hour;
        return hour is >= DayStartHour and < NightStartHour ? Day : Night;
    }
}
