namespace Dashboard.Domain.Running;

/// <summary>Kilometer eines Kalendermonats (1 = Januar) für den Monatsbalken.</summary>
public sealed record MonthlyDistance(int Month, double Km);

/// <summary>Rekorde eines Jahres; Felder fehlen, wenn keine passenden Läufe vorliegen.</summary>
public sealed record RunRecords(
    double LongestKm, double? FastestPaceMinPerKm, int? BiggestMonth, double BiggestMonthKm);

/// <summary>Jahresrückblick eines Lauf-Jahres (FA-8.16).</summary>
public sealed record RunYearReview(
    int Year,
    int RunCount,
    double TotalKm,
    double TotalElevationMeters,
    TimeSpan TotalTime,
    int EddingtonKm,
    IReadOnlyList<MonthlyDistance> Months,
    RunRecords Records);

/// <summary>
/// Jahres-/Monatsstatistik der Läufe (FA-8.16) — reine, testbare Aggregation. Alle Datums-
/// Einordnungen in Berliner Lokalzeit.
/// </summary>
public static class RunReviewCalculator
{
    /// <summary>Läufe unter dieser Distanz fließen nicht in den Pace-Rekord ein (Mess-Rauschen).</summary>
    public const double MinDistanceKmForPace = 1;

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>Jahre mit Läufen, absteigend (jüngstes zuerst) – für die Jahresauswahl.</summary>
    public static IReadOnlyList<int> AvailableYears(IEnumerable<Run> runs) =>
        runs.Select(BerlinYear).Distinct().OrderByDescending(y => y).ToList();

    public static RunYearReview Build(IReadOnlyList<Run> runs, int year)
    {
        var inYear = runs.Where(r => BerlinYear(r) == year).ToList();

        var monthKm = new double[12];
        double totalMeters = 0, totalElevation = 0;
        var totalTime = TimeSpan.Zero;
        foreach (var run in inYear)
        {
            monthKm[BerlinMonth(run) - 1] += run.DistanceMeters / 1000.0;
            totalMeters += run.DistanceMeters;
            totalElevation += run.ElevationGainMeters ?? 0;
            totalTime += run.MovingTime;
        }

        var months = Enumerable.Range(1, 12)
            .Select(m => new MonthlyDistance(m, monthKm[m - 1]))
            .ToList();

        var biggestMonthKm = monthKm.Max();
        var records = new RunRecords(
            LongestKm: inYear.Count == 0 ? 0 : inYear.Max(r => r.DistanceMeters) / 1000.0,
            FastestPaceMinPerKm: inYear
                .Where(r => r.DistanceMeters / 1000.0 >= MinDistanceKmForPace && r.MovingTime > TimeSpan.Zero)
                .Select(r => r.MovingTime.TotalMinutes / (r.DistanceMeters / 1000.0))
                .DefaultIfEmpty(double.NaN)
                .Min() is var pace && !double.IsNaN(pace) ? pace : null,
            BiggestMonth: biggestMonthKm > 0 ? Array.IndexOf(monthKm, biggestMonthKm) + 1 : null,
            BiggestMonthKm: biggestMonthKm);

        return new RunYearReview(
            year,
            inYear.Count,
            totalMeters / 1000.0,
            totalElevation,
            totalTime,
            Eddington(inYear.Select(r => r.DistanceMeters / 1000.0)),
            months,
            records);
    }

    /// <summary>
    /// Eddington-Zahl über die Einzelläufe: größtes E, für das mindestens E Läufe ≥ E km lang
    /// sind. (Klassisch tagesbezogen; hier pro Lauf — passend zur Aktivitäten-Granularität.)
    /// </summary>
    public static int Eddington(IEnumerable<double> distancesKm)
    {
        var sorted = distancesKm.Where(d => d > 0).OrderByDescending(d => d).ToList();
        var e = 0;
        for (var i = 0; i < sorted.Count; i++)
        {
            if (sorted[i] >= i + 1)
            {
                e = i + 1;
            }
            else
            {
                break; // absteigend sortiert → ab hier kann nichts mehr passen
            }
        }

        return e;
    }

    private static int BerlinYear(Run run) => TimeZoneInfo.ConvertTime(run.StartUtc, BerlinTz).Year;

    private static int BerlinMonth(Run run) => TimeZoneInfo.ConvertTime(run.StartUtc, BerlinTz).Month;
}
