using Dashboard.Domain.Whoop;

namespace Dashboard.Tests.Whoop;

public class WhoopHabitMapperTests
{
    private static WhoopWorkout Workout(
        string sport, double highShare = 0, double? distanceM = null, int minutes = 30)
    {
        var start = new DateTimeOffset(2026, 6, 11, 6, 0, 0, TimeSpan.Zero);
        return new WhoopWorkout("id-1", sport, start, start.AddMinutes(minutes), distanceM, highShare);
    }

    [Theory]
    [InlineData("running", 0.0, HabitKind.Zone2Run)]
    [InlineData("running", 0.30, HabitKind.Vo2MaxIntervals)]
    [InlineData("jumping rope", 0.0, HabitKind.JumpRope)]
    [InlineData("jump_rope", 0.0, HabitKind.JumpRope)]
    [InlineData("weightlifting", 0.0, HabitKind.Strength)]
    [InlineData("functional fitness", 0.0, HabitKind.Strength)]
    [InlineData("yoga", 0.0, HabitKind.Stretching)]
    public void MapKind_MapsKnownSports(string sport, double share, HabitKind expected)
    {
        Assert.Equal(expected, WhoopHabitMapper.MapKind(Workout(sport, share)));
    }

    [Theory]
    [InlineData("cycling")]
    [InlineData("walking")]
    [InlineData("")]
    public void MapKind_ReturnsNull_ForUntrackedSports(string sport)
    {
        Assert.Null(WhoopHabitMapper.MapKind(Workout(sport)));
    }

    [Fact]
    public void MapKind_UsesIntensityThresholdForRuns()
    {
        Assert.Equal(HabitKind.Zone2Run, WhoopHabitMapper.MapKind(Workout("running", 0.14)));
        Assert.Equal(HabitKind.Vo2MaxIntervals, WhoopHabitMapper.MapKind(Workout("running", 0.15)));
    }

    [Fact]
    public void BuildRunningDetails_ComputesDurationAndPace()
    {
        // 30 min über 5 km → 6,00 min/km
        var details = WhoopHabitMapper.BuildRunningDetails(Workout("running", distanceM: 5000, minutes: 30));

        Assert.NotNull(details);
        Assert.Equal(30, details!.DurationMinutes);
        Assert.Equal(6.00m, details.PaceMinPerKm);
    }

    [Fact]
    public void BuildRunningDetails_ReturnsNull_WithoutValidDistance()
    {
        Assert.Null(WhoopHabitMapper.BuildRunningDetails(Workout("running", distanceM: null, minutes: 30)));
        Assert.Null(WhoopHabitMapper.BuildRunningDetails(Workout("running", distanceM: 0, minutes: 30)));
    }
}
