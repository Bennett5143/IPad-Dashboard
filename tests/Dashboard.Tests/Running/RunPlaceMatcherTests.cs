namespace Dashboard.Tests.Running;

/// <summary>
/// Zuordnung eines Laufs zu einem Ort. Der Kern der Änderung gegenüber dem alten
/// Routen-Vergleich: entscheidend ist der Startpunkt, nicht der Verlauf der Strecke.
/// </summary>
public class RunPlaceMatcherTests
{
    // Hamburg-Harburg als Bezugspunkt; 0,01° Breite ≈ 1,1 km.
    private static GeoPoint At(double latOffset = 0, double lonOffset = 0) =>
        new(53.4600 + latOffset, 9.9800 + lonOffset);

    private static RunPlaceCandidate Place(int id, double latOffset = 0, double lonOffset = 0) =>
        new(id, At(latOffset, lonOffset));

    [Fact]
    public void FindPlace_SameStartPoint_MatchesRegardlessOfTheRoute()
    {
        // Zwei Läufe von derselben Haustür: der Verlauf spielt keine Rolle, er geht gar nicht ein.
        var places = new[] { Place(1) };

        Assert.Equal(1, RunPlaceMatcher.FindPlace(At(0.0004), places));
        Assert.Equal(1, RunPlaceMatcher.FindPlace(At(-0.0004, 0.0006), places));
    }

    [Fact]
    public void FindPlace_StartFarAway_OpensANewPlace()
    {
        // ~5,5 km entfernt, deutlich über der Schwelle von 2 km.
        Assert.Null(RunPlaceMatcher.FindPlace(At(0.05), [Place(1)]));
    }

    [Fact]
    public void FindPlace_PicksTheNearestPlace()
    {
        var places = new[] { Place(1, 0.010), Place(2, 0.002), Place(3, -0.015) };

        Assert.Equal(2, RunPlaceMatcher.FindPlace(At(), places));
    }

    [Fact]
    public void FindPlace_WithoutPlaces_ReturnsNull()
    {
        Assert.Null(RunPlaceMatcher.FindPlace(At(), []));
    }

    [Fact]
    public void FindPlace_ThresholdIsConfigurable()
    {
        var far = At(0.05); // ~5,5 km

        Assert.Null(RunPlaceMatcher.FindPlace(far, [Place(1)]));
        Assert.Equal(1, RunPlaceMatcher.FindPlace(far, [Place(1)], thresholdMeters: 10_000));
    }

    [Fact]
    public void DistanceMeters_MatchesTheKnownSpanOfADegree()
    {
        // 0,01° Breite sind rund 1,11 km — grob genug für die Schwelle, genau genug als Prüfung.
        var distance = RunPlaceMatcher.DistanceMeters(At(), At(0.01));

        Assert.InRange(distance, 1_100, 1_120);
    }

    [Fact]
    public void MoveCentre_AveragesTheStartPoints()
    {
        var centre = At();

        // Zweiter Lauf 0,01° nördlich → Mittelpunkt wandert die halbe Strecke.
        var afterSecond = RunPlaceMatcher.MoveCentre(centre, runCount: 1, At(0.01));
        Assert.Equal(centre.Latitude + 0.005, afterSecond.Latitude, 6);

        // Beim zehnten Lauf zieht ein einzelner Startpunkt kaum noch.
        var afterTenth = RunPlaceMatcher.MoveCentre(centre, runCount: 9, At(0.01));
        Assert.Equal(centre.Latitude + 0.001, afterTenth.Latitude, 6);
    }

    [Fact]
    public void MoveCentre_FirstRun_TakesTheStartPoint()
    {
        var start = At(0.02);

        Assert.Equal(start, RunPlaceMatcher.MoveCentre(At(), runCount: 0, start));
    }

    [Fact]
    public void Bounds_GrowAroundEveryPointOfTheTrack()
    {
        var bounds = GeoBounds.Around(At()).ExtendAll([At(0.01, 0.02), At(-0.03, -0.01)]);

        Assert.Equal(53.4300, bounds.MinLat, 4);
        Assert.Equal(9.9700, bounds.MinLon, 4);
        Assert.Equal(53.4700, bounds.MaxLat, 4);
        Assert.Equal(10.0000, bounds.MaxLon, 4);
        Assert.Equal(53.4500, bounds.Centre.Latitude, 4);
    }
}
