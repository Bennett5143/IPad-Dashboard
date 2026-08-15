namespace Dashboard.Domain.Running;

/// <summary>
/// Ordnet einen Lauf einem Ort zu — über die Nähe seines <em>Startpunkts</em>, nicht über den
/// Verlauf der Strecke.
/// <para>
/// Das ersetzt den Vergleich ganzer Routen: der lieferte für denselben Ort mehrere „Runden",
/// sobald die Strecke variierte. Zwei unterschiedlich verlaufende Läufe von derselben Haustür
/// sind derselbe Ort — die Frage ist „wo", nicht „welche Strecke".
/// </para>
/// Reine, testbare Logik ohne Persistenz.
/// </summary>
public static class RunPlaceMatcher
{
    /// <summary>
    /// Bis zu dieser Entfernung zwischen Startpunkten gilt es als derselbe Ort. 2 km fassen eine
    /// Nachbarschaft zusammen, ohne zwei Städte zu verschmelzen; der echte Bestand entscheidet,
    /// ob es dabei bleibt, deshalb ist der Wert überschreibbar.
    /// </summary>
    public const double DefaultThresholdMeters = 2_000;

    private const double EarthRadius = 6_371_000;
    private const double Deg2Rad = Math.PI / 180;

    /// <summary>
    /// Id des nächstgelegenen Ortes innerhalb der Schwelle; <c>null</c>, wenn keiner nah genug
    /// liegt (→ neuer Ort).
    /// </summary>
    public static int? FindPlace(
        GeoPoint start,
        IReadOnlyList<RunPlaceCandidate> places,
        double thresholdMeters = DefaultThresholdMeters)
    {
        int? best = null;
        var bestDistance = thresholdMeters;

        foreach (var place in places)
        {
            var distance = DistanceMeters(start, place.Centre);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = place.Id;
            }
        }

        return best;
    }

    /// <summary>Neuer Mittelpunkt, wenn ein weiterer Startpunkt dazukommt (laufendes Mittel).</summary>
    public static GeoPoint MoveCentre(GeoPoint centre, int runCount, GeoPoint start)
    {
        if (runCount <= 0)
        {
            return start;
        }

        var weight = 1.0 / (runCount + 1);
        return new GeoPoint(
            centre.Latitude + ((start.Latitude - centre.Latitude) * weight),
            centre.Longitude + ((start.Longitude - centre.Longitude) * weight));
    }

    /// <summary>Entfernung zweier Punkte in Metern (Haversine).</summary>
    public static double DistanceMeters(GeoPoint a, GeoPoint b)
    {
        var lat1 = a.Latitude * Deg2Rad;
        var lat2 = b.Latitude * Deg2Rad;
        var deltaLat = (b.Latitude - a.Latitude) * Deg2Rad;
        var deltaLon = (b.Longitude - a.Longitude) * Deg2Rad;

        var h = (Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2))
            + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2));

        return 2 * EarthRadius * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }
}
