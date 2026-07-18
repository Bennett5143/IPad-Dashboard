using Dashboard.Web.Components;

namespace Dashboard.Tests.Theming;

public class ThemeResolverTests
{
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private static DateTimeOffset Utc(int y, int mo, int d, int h, int mi) =>
        new(y, mo, d, h, mi, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_BetweenSunriseAndSunset_IsDay()
    {
        var theme = ThemeResolver.Resolve(
            Utc(2026, 7, 15, 12, 0), Utc(2026, 7, 15, 3, 0), Utc(2026, 7, 15, 19, 30), Berlin);

        Assert.Equal(ThemeResolver.Day, theme);
    }

    [Fact]
    public void Resolve_AfterSunset_IsNight()
    {
        var theme = ThemeResolver.Resolve(
            Utc(2026, 7, 15, 20, 0), Utc(2026, 7, 15, 3, 0), Utc(2026, 7, 15, 19, 30), Berlin);

        Assert.Equal(ThemeResolver.Night, theme);
    }

    [Fact]
    public void Resolve_BeforeSunrise_IsNight()
    {
        var theme = ThemeResolver.Resolve(
            Utc(2026, 7, 15, 2, 0), Utc(2026, 7, 15, 3, 0), Utc(2026, 7, 15, 19, 30), Berlin);

        Assert.Equal(ThemeResolver.Night, theme);
    }

    [Theory]
    [InlineData(10, ThemeResolver.Day)]   // 12:00 Berlin (UTC+2 in July)
    [InlineData(1, ThemeResolver.Night)]  // 03:00 Berlin
    [InlineData(20, ThemeResolver.Night)] // 22:00 Berlin
    public void Resolve_WithoutSunData_FallsBackToLocalHours(int utcHour, string expected)
    {
        var theme = ThemeResolver.Resolve(Utc(2026, 7, 15, utcHour, 0), null, null, Berlin);

        Assert.Equal(expected, theme);
    }

    [Fact]
    public void Resolve_WithoutSunData_DayStartsAtSixLocal()
    {
        // 04:00 UTC = 06:00 Berlin (July, UTC+2) -> first day hour.
        Assert.Equal(ThemeResolver.Day, ThemeResolver.Resolve(Utc(2026, 7, 15, 4, 0), null, null, Berlin));
        // 03:59 UTC = 05:59 Berlin -> still night.
        Assert.Equal(ThemeResolver.Night, ThemeResolver.Resolve(Utc(2026, 7, 15, 3, 59), null, null, Berlin));
    }
}
