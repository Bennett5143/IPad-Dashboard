using Dashboard.Web.Components.Pages;

namespace Dashboard.Tests.Components.Pages;

public class HeatmapPayloadBuilderTests
{
    private static readonly DateTimeOffset Start = new(2026, 6, 1, 6, 0, 0, TimeSpan.Zero);

    private static Run MakeRun(int points, bool withStreams = false, int? streamLength = null)
    {
        var track = Enumerable.Range(0, points)
            .Select(i => new GeoPoint(53.55 + (i * 0.001), 9.99 + (i * 0.001)))
            .ToList();

        StravaStreams? streams = null;
        if (withStreams)
        {
            var len = streamLength ?? points;
            streams = new StravaStreams(
                track,
                Enumerable.Range(0, len).Select(i => i * 10).ToList(),
                Enumerable.Range(0, len).Select(i => (double)i).ToList(),
                Enumerable.Range(0, len).Select(i => 120 + i).ToList());
        }

        return new Run(1, "Lauf", "Run", Start, 5000, TimeSpan.FromMinutes(30), track, streams);
    }

    [Fact]
    public void Build_AddsPreformattedRunInfo_FromFullMetrics()
    {
        var run = new Run(
            42, "Morgenlauf", "Run", Start, 5000, TimeSpan.FromMinutes(30),
            Enumerable.Range(0, 3).Select(i => new GeoPoint(53.55 + i * 0.001, 9.99)).ToList(),
            AverageHeartRate: 148);

        var info = Assert.Single(HeatmapPayloadBuilder.Build([run])).Info;

        Assert.Equal("Morgenlauf", info.Name);
        Assert.Equal("01.06.2026", info.Date);
        Assert.Equal("5,0 km", info.Distance);
        Assert.Equal("6:00 /km", info.Pace);          // 30 min / 5 km
        Assert.Equal("Ø 148 bpm", info.HeartRate);
    }

    [Fact]
    public void Build_RunInfo_FallsBackWithoutNameOrHeartRate()
    {
        var run = new Run(
            7, "  ", "Run", Start, 4000, TimeSpan.FromMinutes(22),
            Enumerable.Range(0, 3).Select(i => new GeoPoint(53.55 + i * 0.001, 9.99)).ToList());

        var info = Assert.Single(HeatmapPayloadBuilder.Build([run])).Info;

        Assert.Equal("Lauf", info.Name);              // leerer Name → Fallback
        Assert.Equal("5:30 /km", info.Pace);          // 22 min / 4 km
        Assert.Null(info.HeartRate);
    }

    [Fact]
    public void Build_SkipsRunsWithFewerThanTwoPoints()
    {
        Assert.Empty(HeatmapPayloadBuilder.Build([MakeRun(1)]));
    }

    [Fact]
    public void Build_KeepsAllPoints_WhenUnderLimit()
    {
        var payload = Assert.Single(HeatmapPayloadBuilder.Build([MakeRun(3)]));

        Assert.Equal(3, payload.Pts.Length);
        Assert.Equal(53.55, payload.Pts[0][0]);
        Assert.Equal(9.99, payload.Pts[0][1]);
        Assert.Null(payload.T);
        Assert.Null(payload.Alt);
        Assert.Null(payload.Hr);
    }

    [Fact]
    public void Build_Downsamples_AndAlwaysKeepsLastPoint()
    {
        // 10 Punkte auf max. 4 → stride 3 → Indizes 0,3,6,9 (Endpunkt enthalten)
        var payload = Assert.Single(HeatmapPayloadBuilder.Build([MakeRun(10)], maxPoints: 4));

        Assert.Equal(4, payload.Pts.Length);
        Assert.Equal(53.55 + 0.009, payload.Pts[^1][0], 10);
    }

    [Fact]
    public void Build_DownsamplesStreamsWithSameIndices()
    {
        var payload = Assert.Single(HeatmapPayloadBuilder.Build([MakeRun(10, withStreams: true)], maxPoints: 4));

        Assert.Equal(new[] { 0, 30, 60, 90 }, payload.T);
        Assert.Equal(new[] { 0d, 3d, 6d, 9d }, payload.Alt);
        Assert.Equal(new[] { 120, 123, 126, 129 }, payload.Hr);
    }

    [Fact]
    public void Build_DropsMisalignedStreams()
    {
        // Stream-Länge ≠ Track-Länge → Streams nicht nutzbar
        var payload = Assert.Single(HeatmapPayloadBuilder.Build([MakeRun(10, withStreams: true, streamLength: 7)]));

        Assert.Null(payload.T);
        Assert.Null(payload.Alt);
        Assert.Null(payload.Hr);
    }
}
