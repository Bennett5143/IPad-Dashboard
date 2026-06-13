namespace Dashboard.Domain.Running;

/// <summary>
/// Vergleicht zwei GPS-Tracks auf „dieselbe Runde" (FA-8.17): projizieren → vereinfachen →
/// Hausdorff-Distanz in Metern. Reine, testbare Logik.
/// </summary>
public static class RouteSimilarity
{
    /// <summary>Vereinfachungstoleranz vor dem Vergleich (drosselt die Hausdorff-Kosten ohne Formverlust).</summary>
    public const double SimplifyToleranceMeters = 12;

    /// <summary>Hausdorff-Distanz beider Tracks in Metern; <c>+∞</c>, wenn einer zu kurz ist.</summary>
    public static double HausdorffMeters(IReadOnlyList<GeoPoint> a, IReadOnlyList<GeoPoint> b)
    {
        if (a.Count < 2 || b.Count < 2)
        {
            return double.PositiveInfinity;
        }

        var origin = a[0]; // gemeinsamer Ursprung für beide Projektionen
        var pa = RouteGeometry.Simplify(RouteGeometry.ProjectAll(a, origin), SimplifyToleranceMeters);
        var pb = RouteGeometry.Simplify(RouteGeometry.ProjectAll(b, origin), SimplifyToleranceMeters);
        return RouteGeometry.PolylineHausdorff(pa, pb);
    }
}

/// <summary>Regeln, wann zwei Läufe als dieselbe Runde gelten (FA-8.17).</summary>
public static class RouteMatchRules
{
    /// <summary>Erlaubte relative Abweichung der Distanz für einen Vorfilter (±15 %).</summary>
    public const double DistanceTolerance = 0.15;

    /// <summary>Default-Schwelle der Hausdorff-Distanz (Meter); mit echten Daten zu kalibrieren.</summary>
    public const double DefaultThresholdMeters = 150;

    /// <summary>Schneller Vorfilter über die Gesamtdistanz, bevor die Geometrie verglichen wird.</summary>
    public static bool DistancesCompatible(double aMeters, double bMeters) =>
        aMeters > 0 && bMeters > 0
        && Math.Abs(aMeters - bMeters) <= DistanceTolerance * Math.Max(aMeters, bMeters);
}
