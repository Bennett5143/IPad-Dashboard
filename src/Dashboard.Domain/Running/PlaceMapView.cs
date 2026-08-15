namespace Dashboard.Domain.Running;

/// <summary>
/// Rechnet aus, welchen Kartenausschnitt ein Ort braucht — und damit, welche Kacheln vorgeladen
/// werden müssen.
/// <para>
/// Die Heatmap zeigt einen Ort in fester Ansicht: sie rahmt dessen Ausdehnung und lässt sich weder
/// zoomen noch verschieben. Genau deshalb ist die Kachelmenge endlich und vorher bekannt — bei
/// einer frei beweglichen Karte wäre sie es nicht, und genau daran scheiterte die alte Ansicht auf
/// dem offline iPad.
/// </para>
/// Reine, testbare Geometrie (Web-Mercator, 256-px-Kacheln).
/// </summary>
public static class PlaceMapView
{
    /// <summary>Kachelgröße der OSM-Kacheln in Pixeln.</summary>
    private const int TileSize = 256;

    /// <summary>
    /// Größte Zoomstufe, bei der die Ausdehnung noch vollständig in den Kartenbereich passt —
    /// dieselbe Rechnung, die auch Leaflets <c>fitBounds</c> anstellt.
    /// </summary>
    public static int FitZoom(GeoBounds bounds, int viewportWidthPx, int viewportHeightPx, int maxZoom = 19)
    {
        if (viewportWidthPx <= 0 || viewportHeightPx <= 0)
        {
            return 0;
        }

        var lonSpan = Math.Max(bounds.MaxLon - bounds.MinLon, 1e-9) / 360.0;
        var latSpan = Math.Max(MercatorY(bounds.MinLat) - MercatorY(bounds.MaxLat), 1e-9);

        var zoomForWidth = Math.Log2(viewportWidthPx / (TileSize * lonSpan));
        var zoomForHeight = Math.Log2(viewportHeightPx / (TileSize * latSpan));

        var zoom = (int)Math.Floor(Math.Min(zoomForWidth, zoomForHeight));
        return Math.Clamp(zoom, 0, maxZoom);
    }

    /// <summary>Kachel-Bereich einer Ausdehnung auf einer Zoomstufe (beide Grenzen inklusive).</summary>
    public static (int XMin, int XMax, int YMin, int YMax) TileRange(GeoBounds bounds, int zoom)
    {
        var max = (1 << zoom) - 1;

        return (
            Math.Clamp(TileX(bounds.MinLon, zoom), 0, max),
            Math.Clamp(TileX(bounds.MaxLon, zoom), 0, max),
            Math.Clamp(TileY(bounds.MaxLat, zoom), 0, max), // Nord = kleinere y
            Math.Clamp(TileY(bounds.MinLat, zoom), 0, max));
    }

    /// <summary>Wie viele Kacheln eine Ausdehnung über die Zoomstufen umfasst.</summary>
    public static long TileCount(GeoBounds bounds, int minZoom, int maxZoom)
    {
        long total = 0;
        for (var zoom = minZoom; zoom <= maxZoom; zoom++)
        {
            var (xMin, xMax, yMin, yMax) = TileRange(bounds, zoom);
            total += (long)(xMax - xMin + 1) * (yMax - yMin + 1);
        }

        return total;
    }

    private static int TileX(double longitude, int zoom) =>
        (int)Math.Floor((longitude + 180.0) / 360.0 * (1 << zoom));

    private static int TileY(double latitude, int zoom) =>
        (int)Math.Floor(MercatorY(latitude) * (1 << zoom));

    /// <summary>Web-Mercator-Y, normiert auf 0 (Nordpol) bis 1 (Südpol).</summary>
    private static double MercatorY(double latitude)
    {
        var clamped = Math.Clamp(latitude, -85.05112878, 85.05112878) * Math.PI / 180.0;
        return (1 - (Math.Log(Math.Tan(clamped) + (1 / Math.Cos(clamped))) / Math.PI)) / 2;
    }
}
