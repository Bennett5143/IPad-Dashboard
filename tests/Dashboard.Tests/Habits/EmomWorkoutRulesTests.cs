namespace Dashboard.Tests.Habits;

public class EmomWorkoutRulesTests
{
    private static EmomSegment Seg(int from, int to, int push = 5, int pull = 10)
        => new() { FromMinute = from, ToMinute = to, PushupsPerMinute = push, PullupsPerMinute = pull };

    [Fact]
    public void Valid_WhenContiguousFromOne()
        => Assert.Null(EmomWorkoutRules.ValidateSegments(new[] { Seg(1, 8), Seg(9, 20) }));

    [Fact]
    public void Invalid_WhenEmpty()
        => Assert.NotNull(EmomWorkoutRules.ValidateSegments(Array.Empty<EmomSegment>()));

    [Fact]
    public void Invalid_WhenNotStartingAtOne()
        => Assert.NotNull(EmomWorkoutRules.ValidateSegments(new[] { Seg(2, 8) }));

    [Fact]
    public void Invalid_WhenGapBetweenSegments()
        => Assert.NotNull(EmomWorkoutRules.ValidateSegments(new[] { Seg(1, 8), Seg(10, 20) }));

    [Fact]
    public void Invalid_WhenOverlapping()
        => Assert.NotNull(EmomWorkoutRules.ValidateSegments(new[] { Seg(1, 8), Seg(8, 20) }));

    [Fact]
    public void Invalid_WhenNegativeReps()
        => Assert.NotNull(EmomWorkoutRules.ValidateSegments(new[] { Seg(1, 8, push: -1) }));
}
