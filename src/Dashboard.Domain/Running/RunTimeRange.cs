namespace Dashboard.Domain.Running;

/// <summary>Zeitraum-Filter der Heatmap (FA-8.10).</summary>
public enum RunTimeRange
{
    FourWeeks,
    TwelveMonths,
    All
}

public static class RunTimeRangeExtensions
{
    /// <summary>Frühester einzubeziehender Zeitpunkt, oder <c>null</c> für „alle".</summary>
    public static DateTimeOffset? CutoffUtc(this RunTimeRange range, DateTimeOffset nowUtc) => range switch
    {
        RunTimeRange.FourWeeks => nowUtc.AddDays(-28),
        RunTimeRange.TwelveMonths => nowUtc.AddMonths(-12),
        _ => null
    };
}
