namespace Dashboard.Web.Components.Pages;

/// <summary>
/// Ein Lauf als JS-Nutzlast für die Heatmap. Property-Namen werden vom JS-Interop zu
/// <c>pts/t/alt/hr</c> gecamelcased – so erwartet sie <c>heatmap.js</c>.
/// </summary>
public sealed record HeatmapRunPayload(double[][] Pts, int[]? T, double[]? Alt, int[]? Hr);

/// <summary>
/// Baut aus Läufen die (gedünnte) Heatmap-Nutzlast: pro Lauf Punkte + index-gleiche Streams.
/// Reine, testbare Logik – das Dünnen hält die Übertragung über die Blazor-Verbindung klein.
/// </summary>
public static class HeatmapPayloadBuilder
{
    /// <summary>Max. Punkte pro Lauf in der Nutzlast (Streams werden index-gleich mitgedünnt).</summary>
    public const int DefaultMaxPoints = 250;

    public static IReadOnlyList<HeatmapRunPayload> Build(
        IReadOnlyList<Run> runs, int maxPoints = DefaultMaxPoints) =>
        runs.Select(run => BuildRun(run, maxPoints)).OfType<HeatmapRunPayload>().ToList();

    private static HeatmapRunPayload? BuildRun(Run run, int maxPoints)
    {
        var n = run.Track.Count;
        if (n < 2)
        {
            return null;
        }

        var stride = Math.Max(1, (int)Math.Ceiling(n / (double)maxPoints));
        var idx = new List<int>();
        for (var i = 0; i < n; i += stride)
        {
            idx.Add(i);
        }
        if (idx[^1] != n - 1)
        {
            idx.Add(n - 1); // Endpunkt immer behalten, sonst fehlt das letzte Stück
        }

        var streams = run.Streams;
        var time = streams?.TimeOffsetsSeconds is { } t && t.Count == n ? idx.Select(i => t[i]).ToArray() : null;
        var altitude = streams?.AltitudesMeters is { } a && a.Count == n ? idx.Select(i => a[i]).ToArray() : null;
        var heartRate = streams?.HeartRates is { } h && h.Count == n ? idx.Select(i => h[i]).ToArray() : null;

        return new HeatmapRunPayload(
            idx.Select(i => new[] { run.Track[i].Latitude, run.Track[i].Longitude }).ToArray(),
            time, altitude, heartRate);
    }
}
