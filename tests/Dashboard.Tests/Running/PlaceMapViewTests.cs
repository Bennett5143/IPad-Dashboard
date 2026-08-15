namespace Dashboard.Tests.Running;

/// <summary>
/// Der Kartenausschnitt eines Ortes und die Kacheln, die er braucht. Diese Rechnung trägt das
/// Vorladen: was sie nicht nennt, fehlt später auf dem offline iPad.
/// </summary>
public class PlaceMapViewTests
{
    // Rund um Hamburg-Harburg; ~1,1 km je 0,01° Breite.
    private static GeoBounds Bounds(double latSpan, double lonSpan) =>
        new(53.4500, 9.9500, 53.4500 + latSpan, 9.9500 + lonSpan);

    [Fact]
    public void FitZoom_SmallerArea_MeansCloserZoom()
    {
        var small = PlaceMapView.FitZoom(Bounds(0.01, 0.02), 940, 400);
        var large = PlaceMapView.FitZoom(Bounds(0.20, 0.40), 940, 400);

        Assert.True(small > large, $"kleiner Ort {small} sollte näher zoomen als großer {large}");
    }

    [Fact]
    public void FitZoom_TheBoundsFitIntoTheViewport()
    {
        var bounds = Bounds(0.026, 0.041); // die reale Ausdehnung des Heim-Ortes
        var zoom = PlaceMapView.FitZoom(bounds, 940, 400);

        // Bei der gewählten Stufe passt die Fläche in den Kartenbereich …
        var (xMin, xMax, yMin, yMax) = PlaceMapView.TileRange(bounds, zoom);
        Assert.True((xMax - xMin + 1) * 256 <= 940 + 256);
        Assert.True((yMax - yMin + 1) * 256 <= 400 + 256);

        // … eine Stufe näher nicht mehr.
        var (nxMin, nxMax, nyMin, nyMax) = PlaceMapView.TileRange(bounds, zoom + 2);
        Assert.True((nxMax - nxMin + 1) * 256 > 940 || (nyMax - nyMin + 1) * 256 > 400);
    }

    [Fact]
    public void FitZoom_StaysWithinTheProvidersRange()
    {
        // Ein einzelner Punkt (Ort mit einem sehr kurzen Lauf) darf nicht über Zoom 19 hinaus.
        var pinpoint = new GeoBounds(53.45, 9.95, 53.45, 9.95);

        Assert.InRange(PlaceMapView.FitZoom(pinpoint, 940, 400), 0, 19);
    }

    [Fact]
    public void FitZoom_WithoutAViewport_ReturnsTheWholeWorld()
    {
        Assert.Equal(0, PlaceMapView.FitZoom(Bounds(0.01, 0.01), 0, 0));
    }

    [Fact]
    public void TileRange_NorthIsTheSmallerY()
    {
        var (_, _, yMin, yMax) = PlaceMapView.TileRange(Bounds(0.5, 0.5), 12);

        Assert.True(yMin <= yMax);
    }

    [Fact]
    public void TileCount_CoversEveryZoomLevelOfTheRange()
    {
        var bounds = Bounds(0.05, 0.08);

        var single = PlaceMapView.TileCount(bounds, 13, 13);
        var withReserve = PlaceMapView.TileCount(bounds, 13, 14);

        Assert.True(withReserve > single);
        Assert.Equal(single + PlaceMapView.TileCount(bounds, 14, 14), withReserve);
    }

    /// <summary>
    /// Die Schranke im Warmup (4000 Kacheln je Ort) darf für einen echten Ort nie greifen —
    /// sonst bliebe seine Karte grau.
    /// </summary>
    [Fact]
    public void TileCount_ForARealPlace_StaysWellUnderTheWarmupLimit()
    {
        var bounds = Bounds(0.026, 0.041); // Heim-Ort aus dem echten Bestand
        var zoom = PlaceMapView.FitZoom(bounds, 940, 400);

        Assert.True(PlaceMapView.TileCount(bounds, zoom, zoom + 1) < 4_000);
    }
}
