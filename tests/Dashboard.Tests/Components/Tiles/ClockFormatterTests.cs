namespace Dashboard.Tests.Components.Tiles;

public class ClockFormatterTests
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    [Theory]
    [InlineData("2026-01-19T13:23:45+00:00", "14:23:45", "2026-01-19T14:23:45+01:00")] // Winter: UTC+1
    [InlineData("2026-07-19T12:23:45+00:00", "14:23:45", "2026-07-19T14:23:45+02:00")] // Sommer: UTC+2
    public void Format_ConvertsUtcToBerlinLocalTime(string utcInput, string expectedTime, string expectedIso)
    {
        var utc = DateTimeOffset.Parse(utcInput);

        var result = ClockFormatter.Format(utc, BerlinTz);

        Assert.Equal(expectedTime, result.Time);
        Assert.Equal(expectedIso, result.Iso);
    }

    [Fact]
    public void Format_RendersGermanWeekdayAbbreviation()
    {
        // 19. Mai 2026 ist ein Dienstag
        var utc = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);

        var result = ClockFormatter.Format(utc, BerlinTz);

        Assert.Equal("Di, 19.05.2026", result.Date);
    }

    [Fact]
    public void Format_HandlesDayBoundaryCrossing()
    {
        // 18. Mai 23:00 UTC = 19. Mai 01:00 Berlin (CEST)
        var utc = new DateTimeOffset(2026, 5, 18, 23, 0, 0, TimeSpan.Zero);

        var result = ClockFormatter.Format(utc, BerlinTz);

        Assert.Equal("Di, 19.05.2026", result.Date);
        Assert.Equal("01:00:00", result.Time);
    }

    [Theory]
    [InlineData("2026-03-29T00:30:00+00:00", "01:30:00")] // vor dem Wechsel: CET (+01:00)
    [InlineData("2026-03-29T01:30:00+00:00", "03:30:00")] // nach dem Wechsel: CEST (+02:00)
    public void Format_HandlesDaylightSavingTransition(string utcInput, string expectedTime)
    {
        // Am 29.03.2026 springt die Uhr in Deutschland von 02:00 CET auf 03:00 CEST.
        // → Lokal existiert die Stunde 02:00–02:59 schlicht nicht.
        var utc = DateTimeOffset.Parse(utcInput);

        var result = ClockFormatter.Format(utc, BerlinTz);

        Assert.Equal(expectedTime, result.Time);
    }
}
