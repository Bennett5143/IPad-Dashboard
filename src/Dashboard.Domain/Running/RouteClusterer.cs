namespace Dashboard.Domain.Running;

/// <summary>Repräsentant eines Routen-Clusters für den Vergleich (Erst-Lauf des Clusters).</summary>
public sealed record RouteClusterRepresentative(
    int ClusterId, double DistanceMeters, IReadOnlyList<GeoPoint> Track);

/// <summary>
/// Greedy-inkrementelles Routen-Clustering (FA-8.17): Ein neuer Lauf wird nur gegen die
/// Cluster-Repräsentanten verglichen (nicht paarweise) und dem ähnlichsten zugeordnet –
/// oder eröffnet einen neuen Cluster. Reine, testbare Logik; die Persistenz und die
/// Reihenfolge (chronologisch, damit Repräsentanten deterministisch sind) liegt im Aufrufer.
/// </summary>
public static class RouteClusterer
{
    /// <summary>
    /// Cluster-Id des ähnlichsten Repräsentanten innerhalb der Schwelle; <c>null</c>, wenn keiner passt
    /// (→ neuer Cluster). Vorfilter über die Distanz, dann Hausdorff-Vergleich.
    /// </summary>
    public static int? FindBestCluster(
        double runDistanceMeters,
        IReadOnlyList<GeoPoint> runTrack,
        IReadOnlyList<RouteClusterRepresentative> representatives,
        double thresholdMeters = RouteMatchRules.DefaultThresholdMeters)
    {
        if (runTrack.Count < 2)
        {
            return null;
        }

        int? best = null;
        var bestDistance = thresholdMeters;
        foreach (var rep in representatives)
        {
            if (!RouteMatchRules.DistancesCompatible(runDistanceMeters, rep.DistanceMeters))
            {
                continue;
            }

            var hausdorff = RouteSimilarity.HausdorffMeters(runTrack, rep.Track);
            if (hausdorff <= bestDistance)
            {
                bestDistance = hausdorff;
                best = rep.ClusterId;
            }
        }

        return best;
    }
}
