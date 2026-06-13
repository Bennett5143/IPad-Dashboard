namespace Dashboard.Tests.Running;

public class RouteSimilarityTests
{
    // Erzeugt einen geraden Track von (53.55, 9.99) nach Osten, n Punkte, optionaler Nord-Versatz.
    private static List<GeoPoint> Line(int n, double latOffset = 0, double lngStep = 0.0005) =>
        Enumerable.Range(0, n).Select(i => new GeoPoint(53.55 + latOffset, 9.99 + i * lngStep)).ToList();

    [Fact]
    public void Hausdorff_IsNearZero_ForIdenticalTracks()
    {
        var track = Line(20);

        Assert.True(RouteSimilarity.HausdorffMeters(track, track) < 1);
    }

    [Fact]
    public void Hausdorff_IsDirectionAgnostic()
    {
        var track = Line(20);
        var reversed = Enumerable.Reverse(track).ToList();

        Assert.True(RouteSimilarity.HausdorffMeters(track, reversed) < 1);
    }

    [Fact]
    public void Hausdorff_ReflectsParallelOffset()
    {
        // ~0,001° Breitenversatz ≈ 111 m.
        var a = Line(20);
        var b = Line(20, latOffset: 0.001);

        var d = RouteSimilarity.HausdorffMeters(a, b);
        Assert.InRange(d, 90, 130);
    }

    [Fact]
    public void Hausdorff_InfiniteForDegenerateTrack()
    {
        Assert.True(double.IsPositiveInfinity(RouteSimilarity.HausdorffMeters(Line(1), Line(20))));
    }

    [Fact]
    public void DistancesCompatible_AppliesTolerance()
    {
        Assert.True(RouteMatchRules.DistancesCompatible(5000, 5500));   // +10 %
        Assert.False(RouteMatchRules.DistancesCompatible(5000, 6000));  // +20 %
        Assert.False(RouteMatchRules.DistancesCompatible(0, 5000));
    }

    [Fact]
    public void Simplify_DropsCollinearPoints()
    {
        var dense = Enumerable.Range(0, 50).Select(i => (X: (double)i, Y: 0.0)).ToList();

        var simplified = RouteGeometry.Simplify(dense, toleranceMeters: 1);

        Assert.Equal(2, simplified.Count); // Gerade → nur Start und Ende
    }
}
