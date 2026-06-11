using Dashboard.Domain.Whoop;

namespace Dashboard.Tests.Whoop;

public class WhoopRecoveryTests
{
    [Theory]
    [InlineData(100, WhoopRecoveryLevel.High)]
    [InlineData(67, WhoopRecoveryLevel.High)]
    [InlineData(66, WhoopRecoveryLevel.Medium)]
    [InlineData(34, WhoopRecoveryLevel.Medium)]
    [InlineData(33, WhoopRecoveryLevel.Low)]
    [InlineData(0, WhoopRecoveryLevel.Low)]
    public void Level_FollowsWhoopColorThresholds(int score, WhoopRecoveryLevel expected)
    {
        Assert.Equal(expected, new WhoopRecovery(score, 60, 50).Level);
    }
}
