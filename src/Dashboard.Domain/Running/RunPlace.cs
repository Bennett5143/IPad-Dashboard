namespace Dashboard.Domain.Running;

/// <summary>
/// Rechteck um eine Menge von Punkten (Grad). Trägt den Kartenausschnitt eines Ortes und damit
/// auch die Kachelmenge, die dafür vorgeladen werden muss.
/// </summary>
public readonly record struct GeoBounds(double MinLat, double MinLon, double MaxLat, double MaxLon)
{
    public static GeoBounds Around(GeoPoint point) =>
        new(point.Latitude, point.Longitude, point.Latitude, point.Longitude);

    public GeoPoint Centre => new((MinLat + MaxLat) / 2, (MinLon + MaxLon) / 2);

    /// <summary>Erweitert das Rechteck, bis der Punkt darin liegt.</summary>
    public GeoBounds Extend(GeoPoint point) => new(
        Math.Min(MinLat, point.Latitude),
        Math.Min(MinLon, point.Longitude),
        Math.Max(MaxLat, point.Latitude),
        Math.Max(MaxLon, point.Longitude));

    /// <summary>Erweitert das Rechteck um alle Punkte einer Strecke.</summary>
    public GeoBounds ExtendAll(IEnumerable<GeoPoint> points)
    {
        var bounds = this;
        foreach (var point in points)
        {
            bounds = bounds.Extend(point);
        }

        return bounds;
    }
}

/// <summary>
/// Ein Ort, an dem gelaufen wird — nicht eine Strecke. „Wo war ich laufen?" ist die Frage, die
/// die Lauf-Übersicht und die Heatmap beantworten; wie die Runde an dem Tag verlief, ist eine
/// andere.
/// </summary>
/// <param name="Centre">Mittel der Startpunkte; wandert mit jedem zugeordneten Lauf.</param>
/// <param name="Bounds">Ausdehnung über alle Strecken des Ortes — der Kartenausschnitt.</param>
public sealed record RunPlace(int Id, string Name, GeoPoint Centre, GeoBounds Bounds, int RunCount);

/// <summary>Ein Ort als Kandidat für die Zuordnung: mehr als Mittelpunkt braucht der Vergleich nicht.</summary>
public sealed record RunPlaceCandidate(int Id, GeoPoint Centre);

/// <summary>Anzeige-Aggregat eines Ortes für die Lauf-Übersicht.</summary>
public sealed record RunPlaceSummary(
    int Id,
    string Name,
    int RunCount,
    double TotalDistanceKm,
    double? AveragePaceMinPerKm,
    DateTimeOffset? LastRunUtc);

/// <summary>Zuordnung eines Laufs zu seinem Ort (für das Detail-Badge).</summary>
public sealed record RunPlaceInfo(int Id, string Name);
