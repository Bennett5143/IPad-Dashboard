namespace Dashboard.Domain.Running;

/// <summary>Berechnet aggregierte Kennzahlen über eine Menge von Läufen. Rein, ohne IO.</summary>
public static class RunStatsCalculator
{
    public static RunStats Calculate(IEnumerable<Run> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);

        var count = 0;
        var totalMeters = 0d;
        var totalMovingTime = TimeSpan.Zero;

        foreach (var run in runs)
        {
            count++;
            totalMeters += run.DistanceMeters;
            totalMovingTime += run.MovingTime;
        }

        var totalKm = totalMeters / 1000d;
        var averagePace = totalKm > 0 ? totalMovingTime.TotalMinutes / totalKm : (double?)null;

        return new RunStats(count, totalKm, totalMovingTime, averagePace);
    }
}
