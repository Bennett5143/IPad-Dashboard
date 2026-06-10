namespace Dashboard.Tests.Running;

public class RunTypesAndTokenTests
{
    [Theory]
    [InlineData("Run", true)]
    [InlineData("TrailRun", true)]
    [InlineData("Ride", false)]
    [InlineData("Walk", false)]
    [InlineData(null, false)]
    public void RunTypes_AllowsOnlyRunAndTrailRun(string? type, bool expected)
    {
        Assert.Equal(expected, RunTypes.IsRun(type));
    }

    [Fact]
    public void NeedsRefresh_True_WhenExpiringWithinOneHour()
    {
        var now = new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);
        var token = new StravaTokenSet("a", "r", now.AddMinutes(30));

        Assert.True(token.NeedsRefresh(now));
    }

    [Fact]
    public void NeedsRefresh_False_WhenValidForMoreThanOneHour()
    {
        var now = new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);
        var token = new StravaTokenSet("a", "r", now.AddHours(5));

        Assert.False(token.NeedsRefresh(now));
    }
}
