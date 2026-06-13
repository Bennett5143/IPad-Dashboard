namespace Dashboard.Tests.Running;

public class RouteClustererTests
{
    private static List<GeoPoint> Line(int n, double latOffset = 0) =>
        Enumerable.Range(0, n).Select(i => new GeoPoint(53.55 + latOffset, 9.99 + i * 0.0005)).ToList();

    [Fact]
    public void FindBestCluster_MatchesSimilarRoute()
    {
        var reps = new List<RouteClusterRepresentative>
        {
            new(1, 5000, Line(20)),
        };

        // Nahezu identische Runde, kompatible Distanz.
        var match = RouteClusterer.FindBestCluster(5050, Line(20, latOffset: 0.0002), reps);

        Assert.Equal(1, match);
    }

    [Fact]
    public void FindBestCluster_NoMatch_ForDifferentShape()
    {
        var reps = new List<RouteClusterRepresentative> { new(1, 5000, Line(20)) };

        // Gleiche Distanz, aber ~330 m parallel versetzt → über der Schwelle.
        var match = RouteClusterer.FindBestCluster(5000, Line(20, latOffset: 0.003), reps);

        Assert.Null(match);
    }

    [Fact]
    public void FindBestCluster_SkipsDistanceIncompatibleClusters()
    {
        var reps = new List<RouteClusterRepresentative> { new(1, 5000, Line(20)) };

        // Identische Form, aber Distanz 20 % länger → Vorfilter verwirft.
        Assert.Null(RouteClusterer.FindBestCluster(6000, Line(20), reps));
    }

    [Fact]
    public void FindBestCluster_PicksClosestAmongCandidates()
    {
        var reps = new List<RouteClusterRepresentative>
        {
            new(1, 5000, Line(20, latOffset: 0.0008)), // ~90 m weg
            new(2, 5000, Line(20, latOffset: 0.0001)), // ~11 m weg → näher
        };

        Assert.Equal(2, RouteClusterer.FindBestCluster(5000, Line(20), reps));
    }

    [Fact]
    public void FindBestCluster_NullForShortTrack()
    {
        Assert.Null(RouteClusterer.FindBestCluster(5000, Line(1), [new(1, 5000, Line(20))]));
    }
}
