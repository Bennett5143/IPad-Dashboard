using Dashboard.Domain.Whoop;

namespace Dashboard.Tests.Components.Tiles;

public class WhoopFormatterTests
{
    [Fact]
    public void RecoveryScore_ShowsPercent_OrDash()
    {
        Assert.Equal("72%", WhoopFormatter.RecoveryScore(new WhoopRecovery(72, 60, 48)));
        Assert.Equal("–", WhoopFormatter.RecoveryScore(null));
    }

    [Theory]
    [InlineData(WhoopRecoveryLevel.High, "recovery-high")]
    [InlineData(WhoopRecoveryLevel.Medium, "recovery-medium")]
    [InlineData(WhoopRecoveryLevel.Low, "recovery-low")]
    public void RecoveryCss_MapsLevel(WhoopRecoveryLevel level, string expected)
    {
        Assert.Equal(expected, WhoopFormatter.RecoveryCss(level));
    }

    [Fact]
    public void Sleep_FormatsHoursAndPerformance()
    {
        Assert.Equal("7:30 h · 88%", WhoopFormatter.Sleep(new WhoopSleepSummary(88, TimeSpan.FromHours(7.5))));
        Assert.Equal("–", WhoopFormatter.Sleep(null));
    }

    [Fact]
    public void Strain_RoundsToOneDecimal_OrDash()
    {
        Assert.Equal("12,7", WhoopFormatter.Strain(12.74));
        Assert.Equal("–", WhoopFormatter.Strain(null));
    }
}
