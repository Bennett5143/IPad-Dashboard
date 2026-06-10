namespace Dashboard.Tests.Running;

public class RunTimeRangeTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FourWeeks_CutsOff28DaysBack()
    {
        Assert.Equal(Now.AddDays(-28), RunTimeRange.FourWeeks.CutoffUtc(Now));
    }

    [Fact]
    public void TwelveMonths_CutsOff12MonthsBack()
    {
        Assert.Equal(Now.AddMonths(-12), RunTimeRange.TwelveMonths.CutoffUtc(Now));
    }

    [Fact]
    public void All_HasNoCutoff()
    {
        Assert.Null(RunTimeRange.All.CutoffUtc(Now));
    }
}
