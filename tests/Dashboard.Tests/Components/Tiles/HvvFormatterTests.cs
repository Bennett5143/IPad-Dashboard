namespace Dashboard.Tests.Components.Tiles;

public class HvvFormatterTests
{
    private static readonly DateTimeOffset Planned =
        new(2026, 6, 10, 14, 6, 0, TimeSpan.FromHours(2));

    private static Departure WithDelay(TimeSpan? delay) =>
        new("189", "S Blankenese", TransportMode.Bus, "Bus", Planned, delay);

    [Fact]
    public void Time_ShowsExpectedTimeInBerlin()
    {
        // +2 min Verspätung → erwartet 14:08
        Assert.Equal("14:08", HvvFormatter.Time(WithDelay(TimeSpan.FromMinutes(2))));
    }

    [Fact]
    public void DelayBadge_ShowsPositiveMinutes()
    {
        Assert.Equal("+2", HvvFormatter.DelayBadge(WithDelay(TimeSpan.FromSeconds(120))));
    }

    [Fact]
    public void DelayBadge_ShowsNegativeMinutes_WhenEarly()
    {
        Assert.Equal("-1", HvvFormatter.DelayBadge(WithDelay(TimeSpan.FromSeconds(-60))));
    }

    [Fact]
    public void DelayBadge_IsNull_WhenPunctualOrNoLiveData()
    {
        Assert.Null(HvvFormatter.DelayBadge(WithDelay(TimeSpan.Zero)));
        Assert.Null(HvvFormatter.DelayBadge(WithDelay(null)));
    }

    [Theory]
    [InlineData(TransportMode.Bus, "🚌")]
    [InlineData(TransportMode.SBahn, "🚆")]
    [InlineData(TransportMode.UBahn, "🚇")]
    [InlineData(TransportMode.Ferry, "⛴️")]
    [InlineData(TransportMode.Other, "🚍")]
    public void ModeEmoji_MapsMode(TransportMode mode, string expected)
    {
        Assert.Equal(expected, HvvFormatter.ModeEmoji(mode));
    }
}
