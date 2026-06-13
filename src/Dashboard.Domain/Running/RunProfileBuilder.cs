namespace Dashboard.Domain.Running;

/// <summary>
/// Heruntergerechnetes Verlaufsprofil eines Laufs (Pace/Höhe/HF je Streckenabschnitt) für die
/// SVG-Darstellung auf der Detailseite. Reihen sind index-gleich; einzelne fehlen, wenn der
/// zugehörige Stream nicht vorliegt.
/// </summary>
public sealed record RunProfile(
    IReadOnlyList<double?> Pace,
    IReadOnlyList<double?> Elevation,
    IReadOnlyList<double?> HeartRate)
{
    public bool HasPace => Pace.Any(v => v is not null);
    public bool HasElevation => Elevation.Any(v => v is not null);
    public bool HasHeartRate => HeartRate.Any(v => v is not null);
    public bool IsEmpty => Pace.Count == 0;
}

/// <summary>
/// Baut das Verlaufsprofil aus den Pro-Punkt-Streams (Downsampling serverseitig, damit das SVG
/// klein bleibt) — reine, testbare Geometrie. Pace entsteht aus Distanz/Zeit je Abschnitt
/// (Punkt-zu-Punkt ist zu verrauscht), Höhe/HF aus den Punktwerten gemittelt.
/// </summary>
public static class RunProfileBuilder
{
    public const int DefaultBuckets = 120;

    private static readonly RunProfile Empty = new([], [], []);

    public static RunProfile Build(Run run, int buckets = DefaultBuckets)
    {
        var streams = run.Streams;
        var track = streams?.Track;
        if (streams is null || track is null || track.Count < 2 || buckets < 1)
        {
            return Empty;
        }

        var n = track.Count;
        var times = streams.TimeOffsetsSeconds.Count == n ? streams.TimeOffsetsSeconds : null;
        var alt = streams.AltitudesMeters?.Count == n ? streams.AltitudesMeters : null;
        var hr = streams.HeartRates?.Count == n ? streams.HeartRates : null;

        var segments = n - 1;
        var bucketCount = Math.Min(buckets, segments);

        var pace = new double?[bucketCount];
        var elevation = new double?[bucketCount];
        var heartRate = new double?[bucketCount];

        for (var k = 0; k < bucketCount; k++)
        {
            var lo = (int)((long)k * segments / bucketCount);
            var hi = (int)((long)(k + 1) * segments / bucketCount); // exklusiv, Segment-Index
            if (hi <= lo)
            {
                hi = lo + 1;
            }

            double segDist = 0, segTime = 0;
            for (var s = lo; s < hi; s++)
            {
                segDist += GeoMath.HaversineMeters(track[s], track[s + 1]);
                if (times is not null)
                {
                    segTime += Math.Max(0, times[s + 1] - times[s]);
                }
            }

            pace[k] = times is not null && segDist > 0 && segTime > 0
                ? segTime / 60.0 / (segDist / 1000.0) // min/km
                : null;
            elevation[k] = alt is not null ? Average(alt, lo, hi) : null;
            heartRate[k] = hr is not null ? Average(hr.Select(v => (double)v).ToList(), lo, hi) : null;
        }

        return new RunProfile(pace, elevation, heartRate);
    }

    private static double Average(IReadOnlyList<double> values, int fromPoint, int toPointExclusiveSegment)
    {
        // Punkte des Segmentbereichs sind fromPoint .. toPointExclusiveSegment (inklusive Endpunkt).
        double sum = 0;
        var count = 0;
        for (var i = fromPoint; i <= toPointExclusiveSegment && i < values.Count; i++)
        {
            sum += values[i];
            count++;
        }

        return count == 0 ? 0 : sum / count;
    }
}
