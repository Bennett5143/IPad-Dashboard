namespace Dashboard.Tests.Habits;

public class EmomWorkoutTests
{
    [Fact]
    public void Totals_SumAcrossSegmentsWithVaryingReps()
    {
        // Min 1–8: 10 Pull + 5 Push;  Min 9–20: 9 Pull + 5 Push
        var workout = new EmomWorkout
        {
            Segments =
            {
                new EmomSegment { FromMinute = 1, ToMinute = 8,  PullupsPerMinute = 10, PushupsPerMinute = 5 },
                new EmomSegment { FromMinute = 9, ToMinute = 20, PullupsPerMinute = 9,  PushupsPerMinute = 5 }
            }
        };

        Assert.Equal(20, workout.TotalMinutes);             // 8 + 12
        Assert.Equal(8 * 10 + 12 * 9, workout.TotalPullups); // 80 + 108 = 188
        Assert.Equal(20 * 5, workout.TotalPushups);          // 100
    }
}