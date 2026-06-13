using System.Globalization;

namespace Dashboard.Web.Components.Pages;

/// <summary>Anzeige-fertige Eckdaten eines Laufs fürs Heatmap-Popup (server-seitig formatiert).</summary>
public sealed record HeatmapRunInfo(string Name, string Date, string Distance, string Pace, string? HeartRate);

/// <summary>
/// Ein Lauf als JS-Nutzlast für die Heatmap. Property-Namen werden vom JS-Interop zu
/// <c>pts/t/alt/hr/info</c> gecamelcased – so erwartet sie <c>heatmap.js</c>.
/// </summary>
public sealed record HeatmapRunPayload(double[][] Pts, int[]? T, double[]? Alt, int[]? Hr, HeatmapRunInfo Info);

/// <summary>
/// Baut aus Läufen die (gedünnte) Heatmap-Nutzlast: pro Lauf Punkte + index-gleiche Streams +
/// vorberechnete Popup-Infos. Reine, testbare Logik – das Dünnen hält die Übertragung über die
/// Blazor-Verbindung klein; die Popup-Werte stammen aus den vollen Lauf-Metriken, nicht aus den
/// gedünnten Streams.
/// </summary>
public static class HeatmapPayloadBuilder
{
    /// <summary>Max. Punkte pro Lauf in der Nutzlast (Streams werden index-gleich mitgedünnt).</summary>
    public const int DefaultMaxPoints = 250;

    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

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
            time, altitude, heartRate, BuildInfo(run));
    }

    private static HeatmapRunInfo BuildInfo(Run run)
    {
        var localDate = TimeZoneInfo.ConvertTime(run.StartUtc, BerlinTz);
        return new HeatmapRunInfo(
            string.IsNullOrWhiteSpace(run.Name) ? "Lauf" : run.Name,
            localDate.ToString("dd.MM.yyyy", German),
            $"{(run.DistanceMeters / 1000.0).ToString("0.0", German)} km",
            FormatPace(run),
            run.AverageHeartRate is { } hr ? $"Ø {hr} bpm" : null);
    }

    private static string FormatPace(Run run)
    {
        var km = run.DistanceMeters / 1000.0;
        if (km <= 0 || run.MovingTime <= TimeSpan.Zero)
        {
            return "–";
        }

        var pace = run.MovingTime.TotalMinutes / km;
        var minutes = (int)pace;
        var seconds = (int)Math.Round((pace - minutes) * 60, MidpointRounding.AwayFromZero);
        if (seconds == 60)
        {
            minutes++;
            seconds = 0;
        }

        return $"{minutes}:{seconds:00} /km";
    }
}
