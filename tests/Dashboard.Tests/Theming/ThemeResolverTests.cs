using Dashboard.Web.Components;

namespace Dashboard.Tests.Theming;

public class ThemeResolverTests
{
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    // July = Berlin is UTC+2, so local hour = utcHour + 2.
    private static DateTimeOffset Utc(int hour, int minute = 0) =>
        new(2026, 7, 15, hour, minute, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(6, ThemeResolver.Day)]    // 08:00 local — first day hour
    [InlineData(10, ThemeResolver.Day)]   // 12:00 local
    [InlineData(17, ThemeResolver.Day)]   // 19:00 local
    public void Resolve_DaytimeHours_ReturnDay(int utcHour, string expected) =>
        Assert.Equal(expected, ThemeResolver.Resolve(Utc(utcHour), Berlin));

    [Theory]
    [InlineData(18, ThemeResolver.Night)] // 20:00 local — first night hour
    [InlineData(21, ThemeResolver.Night)] // 23:00 local
    [InlineData(0, ThemeResolver.Night)]  // 02:00 local
    public void Resolve_NighttimeHours_ReturnNight(int utcHour, string expected) =>
        Assert.Equal(expected, ThemeResolver.Resolve(Utc(utcHour), Berlin));

    [Fact]
    public void Resolve_Boundaries_AreInclusiveOfDayStartAndExclusiveOfNightStart()
    {
        // 07:59 local -> night, 08:00 local -> day
        Assert.Equal(ThemeResolver.Night, ThemeResolver.Resolve(Utc(5, 59), Berlin)); // 07:59 local
        Assert.Equal(ThemeResolver.Day, ThemeResolver.Resolve(Utc(6, 0), Berlin));    // 08:00 local
        // 19:59 local -> day, 20:00 local -> night
        Assert.Equal(ThemeResolver.Day, ThemeResolver.Resolve(Utc(17, 59), Berlin));  // 19:59 local
        Assert.Equal(ThemeResolver.Night, ThemeResolver.Resolve(Utc(18, 0), Berlin)); // 20:00 local
    }
}
