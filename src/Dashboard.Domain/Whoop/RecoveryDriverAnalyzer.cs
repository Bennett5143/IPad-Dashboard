namespace Dashboard.Domain.Whoop;

/// <summary>Untersuchte Einflussfaktoren auf die Recovery.</summary>
public enum RecoveryFactor
{
    SleepDuration,
    Bedtime,
    PreviousDayStrain
}

/// <summary>Korrelation eines Faktors mit der Recovery; <see cref="PearsonR"/> fehlt bei zu dünner Stichprobe.</summary>
public sealed record RecoveryDriverStat(RecoveryFactor Factor, int SampleCount, double? PearsonR);

/// <summary>
/// Recovery-Treiber (FA-10.06): Wie hängen Schlafdauer, Einschlafzeit und der Strain des
/// Vortags mit der Recovery zusammen? Pearson-Korrelation als **Heuristik** — Zusammenhang,
/// keine Kausalität (FA-10.02). Reine, testbare Logik.
/// </summary>
public static class RecoveryDriverAnalyzer
{
    /// <summary>Unter so vielen Wertepaaren gibt es keinen Korrelationswert (FA-10.02).</summary>
    public const int MinSamples = 10;

    /// <summary>Wertepaare (Faktor, Recovery) für Scatter und Korrelation.</summary>
    public static IReadOnlyList<(double X, double Y)> Pairs(
        IReadOnlyList<WhoopDailyMetric> metrics, RecoveryFactor factor)
    {
        switch (factor)
        {
            case RecoveryFactor.SleepDuration:
                return metrics
                    .Where(m => m is { SleepHours: not null, RecoveryScore: not null })
                    .Select(m => (m.SleepHours!.Value, (double)m.RecoveryScore!.Value))
                    .ToList();

            case RecoveryFactor.Bedtime:
                return metrics
                    .Where(m => m is { SleepStartUtc: not null, RecoveryScore: not null })
                    .Select(m => (
                        SleepAnalyzer.MinutesSinceAnchor(m.SleepStartUtc!.Value),
                        (double)m.RecoveryScore!.Value))
                    .ToList();

            case RecoveryFactor.PreviousDayStrain:
                var strainByDate = metrics
                    .Where(m => m.DayStrain is not null)
                    .ToDictionary(m => m.Date, m => m.DayStrain!.Value);
                return metrics
                    .Where(m => m.RecoveryScore is not null)
                    .Select(m => (Strain: strainByDate.TryGetValue(m.Date.AddDays(-1), out var s)
                        ? s
                        : (double?)null, m.RecoveryScore))
                    .Where(x => x.Strain is not null)
                    .Select(x => (x.Strain!.Value, (double)x.RecoveryScore!.Value))
                    .ToList();

            default:
                return [];
        }
    }

    /// <summary>Alle Faktoren als Korrelations-Statistik (Reihenfolge wie das Enum).</summary>
    public static IReadOnlyList<RecoveryDriverStat> Analyze(IReadOnlyList<WhoopDailyMetric> metrics) =>
        Enum.GetValues<RecoveryFactor>()
            .Select(factor =>
            {
                var pairs = Pairs(metrics, factor);
                return new RecoveryDriverStat(factor, pairs.Count, Pearson(pairs));
            })
            .ToList();

    /// <summary>Pearson-r; <c>null</c> unter <see cref="MinSamples"/> Paaren oder ohne Varianz.</summary>
    public static double? Pearson(IReadOnlyList<(double X, double Y)> pairs)
    {
        if (pairs.Count < MinSamples)
        {
            return null;
        }

        var meanX = pairs.Average(p => p.X);
        var meanY = pairs.Average(p => p.Y);
        double cov = 0, varX = 0, varY = 0;
        foreach (var (x, y) in pairs)
        {
            cov += (x - meanX) * (y - meanY);
            varX += (x - meanX) * (x - meanX);
            varY += (y - meanY) * (y - meanY);
        }

        return varX < 1e-9 || varY < 1e-9 ? null : cov / Math.Sqrt(varX * varY);
    }
}
