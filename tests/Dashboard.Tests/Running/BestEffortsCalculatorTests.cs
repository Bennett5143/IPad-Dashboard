namespace Dashboard.Tests.Running;

public class BestEffortsCalculatorTests
{
    // Gerader Lauf nach Osten; Schritt ~stepMeters (großzügig bemessen, damit die Haversine-
    // Distanz die runden Zieldistanzen sicher überschreitet), Zeit je Punkt aus secondsPerPoint.
    private static Run RunWith(int points, double stepMeters, int[] secondsPerPoint, bool withStreams = true)
    {
        var lngStep = stepMeters / (111_320 * Math.Cos(53.55 * Math.PI / 180));
        var track = Enumerable.Range(0, points)
            .Select(i => new GeoPoint(53.55, 9.99 + i * lngStep))
            .ToList();
        StravaStreams? streams = withStreams
            ? new StravaStreams(track, secondsPerPoint, null, null)
            : null;

        var movingSeconds = secondsPerPoint.Length == 0 ? 0 : secondsPerPoint[^1];
        return new Run(1, "Lauf", "Run", default, points * stepMeters,
            TimeSpan.FromSeconds(movingSeconds), track, streams);
    }

    private static int[] ConstantTimes(int points, int secondsPerStep) =>
        Enumerable.Range(0, points).Select(i => i * secondsPerStep).ToArray();

    [Fact]
    public void Compute_ReturnsNull_WithoutStreams()
    {
        var efforts = BestEffortsCalculator.Compute(RunWith(50, 105, [], withStreams: false));

        Assert.All(efforts, e => Assert.Null(e.FastestTime));
        Assert.Equal([1000, 5000, 10000], efforts.Select(e => e.DistanceMeters));
    }

    [Fact]
    public void Compute_NullForTargetLongerThanRun()
    {
        // 13 Punkte × 105 m ≈ 1,26 km → 1 km vorhanden, 5 km nicht.
        var efforts = BestEffortsCalculator.Compute(RunWith(13, 105, ConstantTimes(13, 10)));

        Assert.NotNull(efforts.Single(e => e.DistanceMeters == 1000).FastestTime);
        Assert.Null(efforts.Single(e => e.DistanceMeters == 5000).FastestTime);
    }

    [Fact]
    public void Compute_FindsFastestKilometreWindow()
    {
        // 30 Punkte × 105 m. Erste 10 Abschnitte langsam (10 s), der Rest schnell (5 s).
        var times = new int[30];
        for (var i = 1; i < 30; i++)
        {
            times[i] = times[i - 1] + (i <= 10 ? 10 : 5);
        }

        var km = BestEffortsCalculator.Compute(RunWith(30, 105, times))
            .Single(e => e.DistanceMeters == 1000).FastestTime;

        // Schnellster 1-km-Abschnitt liegt ganz im schnellen Teil: ~10 Abschnitte × 5 s ≈ 50 s.
        Assert.NotNull(km);
        Assert.InRange(km!.Value.TotalSeconds, 48, 60);
    }

    [Fact]
    public void Compute_CustomTargets()
    {
        // 30 Punkte × 105 m ≈ 3,1 km, konstant 6 s je Abschnitt.
        var effort = Assert.Single(BestEffortsCalculator.Compute(RunWith(30, 105, ConstantTimes(30, 6)), [2000]));

        Assert.Equal(2000, effort.DistanceMeters);
        Assert.NotNull(effort.FastestTime);
        Assert.InRange(effort.FastestTime!.Value.TotalSeconds, 112, 126); // ~2 km / (105 m / 6 s)
    }
}
