namespace Dashboard.Domain.Running;

/// <summary>Monats-Aggregat der Lauf-Effizienz; <see cref="AvgBeatsPerKm"/> fehlt bei zu dünner Stichprobe.</summary>
public sealed record MonthlyEfficiency(int Year, int Month, int SampleCount, double? AvgBeatsPerKm);

/// <summary>
/// Aerobe Fitness-Kurve (FA-10.05): Herzschläge pro km über Monate — werde ich bei gleicher
/// Herzfrequenz schneller? Gleiches Maß wie die Tageszeit-Auswertung (niedriger = effizienter),
/// als **Heuristik** über alle Läufe hinweg (FA-10.02). Reine, testbare Logik.
/// </summary>
public static class AerobicEfficiencyCalculator
{
    /// <summary>Monate mit weniger Läufen liefern keinen Durchschnitt (FA-10.02).</summary>
    public const int MinRunsPerMonth = 2;

    /// <summary>Kürzere Läufe verzerren das Maß (Anlauf-/Mess-Rauschen) und bleiben außen vor.</summary>
    public const double MinDistanceKm = 2;

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>Herzschläge pro km (Ø-HF × Minuten ÷ km); <c>null</c> ohne HF/Distanz/Dauer.</summary>
    public static double? BeatsPerKm(Run run)
    {
        var km = run.DistanceMeters / 1000.0;
        var minutes = run.MovingTime.TotalMinutes;
        if (run.AverageHeartRate is not > 0 || km < MinDistanceKm || minutes <= 0)
        {
            return null;
        }

        return run.AverageHeartRate.Value * minutes / km;
    }

    /// <summary>
    /// Lückenlose Monatsreihe (Berlin) vom ersten bis zum letzten messbaren Lauf;
    /// Monate unter <see cref="MinRunsPerMonth"/> messbaren Läufen bleiben ohne Wert.
    /// </summary>
    public static IReadOnlyList<MonthlyEfficiency> Monthly(IEnumerable<Run> runs)
    {
        var measured = runs
            .Select(r => (Run: r, Measure: BeatsPerKm(r)))
            .Where(x => x.Measure is not null)
            .Select(x =>
            {
                var local = TimeZoneInfo.ConvertTime(x.Run.StartUtc, BerlinTz);
                return (local.Year, local.Month, Measure: x.Measure!.Value);
            })
            .ToList();
        if (measured.Count == 0)
        {
            return [];
        }

        var byMonth = measured
            .GroupBy(x => (x.Year, x.Month))
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Avg: g.Average(x => x.Measure)));

        var first = byMonth.Keys.Min();
        var last = byMonth.Keys.Max();

        var months = new List<MonthlyEfficiency>();
        for (var cursor = new DateOnly(first.Year, first.Month, 1);
             cursor <= new DateOnly(last.Year, last.Month, 1);
             cursor = cursor.AddMonths(1))
        {
            var agg = byMonth.GetValueOrDefault((cursor.Year, cursor.Month));
            months.Add(new MonthlyEfficiency(
                cursor.Year, cursor.Month, agg.Count,
                agg.Count >= MinRunsPerMonth ? agg.Avg : null));
        }

        return months;
    }

    /// <summary>
    /// Veränderung des jüngsten belastbaren Monats gegenüber dem Monat ~3 davor, in Prozent
    /// (negativ = effizienter geworden); <c>null</c> ohne Vergleichsmonat.
    /// </summary>
    public static double? TrendPercent(IReadOnlyList<MonthlyEfficiency> months)
    {
        var latest = months.LastOrDefault(m => m.AvgBeatsPerKm is not null);
        if (latest is null)
        {
            return null;
        }

        var latestIndex = months.ToList().IndexOf(latest);
        var reference = months
            .Take(Math.Max(0, latestIndex - 2))
            .LastOrDefault(m => m.AvgBeatsPerKm is not null);
        if (reference is null)
        {
            return null;
        }

        return (latest.AvgBeatsPerKm!.Value - reference.AvgBeatsPerKm!.Value)
            / reference.AvgBeatsPerKm.Value * 100;
    }
}
