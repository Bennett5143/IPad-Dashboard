namespace Dashboard.Tests.Football;

public class TournamentTests
{
    private static readonly Tournament Wc = new(
        "WC", "WM 2026",
        new DateTimeOffset(2026, 6, 11, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 19, 23, 59, 59, TimeSpan.Zero));

    [Theory]
    [InlineData("2026-06-29T12:00:00Z", true)]   // mitten im Turnier
    [InlineData("2026-06-11T00:00:00Z", true)]   // exakt Start
    [InlineData("2026-07-19T23:59:59Z", true)]   // exakt Ende
    [InlineData("2026-06-10T23:59:59Z", false)]  // einen Moment davor
    [InlineData("2026-07-20T00:00:01Z", false)]  // einen Moment danach
    public void IsActive_RespectsWindow(string nowIso, bool expected)
    {
        var now = DateTimeOffset.Parse(nowIso, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected, Wc.IsActive(now));
    }
}
