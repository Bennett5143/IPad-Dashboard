namespace Dashboard.Tests.Running;

public class RunProfileBuilderTests
{
    // Lauf gen Osten: jeder Punkt 0,001° weiter (~66 m bei 53,55° Breite), 30 s je Abschnitt.
    private static Run RunWithStreams(int points, bool altitude = true, bool heartRate = true)
    {
        var track = Enumerable.Range(0, points)
            .Select(i => new GeoPoint(53.55, 9.99 + i * 0.001))
            .ToList();
        var times = Enumerable.Range(0, points).Select(i => i * 30).ToList();
        var alt = altitude ? Enumerable.Range(0, points).Select(i => (double)i).ToList() : null;
        var hr = heartRate ? Enumerable.Range(0, points).Select(i => 120 + i).ToList() : null;
        var streams = new StravaStreams(track, times, alt, hr);

        return new Run(1, "Lauf", "Run",
            new DateTimeOffset(2026, 6, 1, 6, 0, 0, TimeSpan.Zero),
            5000, TimeSpan.FromMinutes(30), track, streams);
    }

    [Fact]
    public void Build_ReturnsEmpty_WithoutStreams()
    {
        var run = new Run(1, "Lauf", "Run", default, 5000, TimeSpan.FromMinutes(30),
            [new GeoPoint(53.55, 9.99), new GeoPoint(53.56, 9.99)]);

        Assert.True(RunProfileBuilder.Build(run).IsEmpty);
    }

    [Fact]
    public void Build_DownsamplesToBucketCount()
    {
        var profile = RunProfileBuilder.Build(RunWithStreams(100), buckets: 10);

        Assert.Equal(10, profile.Pace.Count);
        Assert.Equal(10, profile.Elevation.Count);
        Assert.Equal(10, profile.HeartRate.Count);
        Assert.True(profile.HasPace);
        Assert.True(profile.HasElevation);
        Assert.True(profile.HasHeartRate);
    }

    [Fact]
    public void Build_LimitsBucketsToSegmentCount()
    {
        // 4 Punkte → 3 Segmente → höchstens 3 Buckets, auch bei größerem Wunsch.
        var profile = RunProfileBuilder.Build(RunWithStreams(4), buckets: 120);

        Assert.Equal(3, profile.Pace.Count);
    }

    [Fact]
    public void Build_ComputesPlausiblePace()
    {
        // ~66 m in 30 s ⇒ ~2,2 m/s ⇒ ~7,5 min/km. Toleranz großzügig (Haversine + Breite).
        var profile = RunProfileBuilder.Build(RunWithStreams(20), buckets: 5);

        Assert.All(profile.Pace, p => Assert.InRange(p!.Value, 6.5, 8.5));
    }

    [Fact]
    public void Build_OmitsMissingStreams()
    {
        var profile = RunProfileBuilder.Build(RunWithStreams(20, altitude: false, heartRate: false), buckets: 5);

        Assert.True(profile.HasPace);                 // Track + Zeit reichen
        Assert.False(profile.HasElevation);
        Assert.False(profile.HasHeartRate);
        Assert.All(profile.Elevation, e => Assert.Null(e));
    }

    [Fact]
    public void Build_ElevationRises_AlongTheRun()
    {
        var profile = RunProfileBuilder.Build(RunWithStreams(40), buckets: 8);

        var elevations = profile.Elevation.Select(e => e!.Value).ToList();
        Assert.True(elevations[^1] > elevations[0]); // Höhe steigt monoton mit dem Index
    }
}
