namespace Dashboard.Domain.Whoop;

/// <summary>Form-Zonen des Akut-zu-Chronisch-Quotienten (ACWR).</summary>
public enum TrainingLoadZone
{
    Low,      // < 0,8 – Unterlast
    Balanced, // 0,8 – 1,3
    Elevated, // 1,3 – 1,5
    High      // ≥ 1,5
}

/// <summary>Ein Tagespunkt der Trainingslast-Reihe; <see cref="Ratio"/> fehlt in der Warmlauf-Phase.</summary>
public sealed record TrainingLoadPoint(DateOnly Date, double Acute, double Chronic, double? Ratio);

/// <summary>
/// Trainingslast als ACWR (FA-10.04 der MoSCoW-Spec): akute (7 Tage) zu chronischer
/// (28 Tage) Belastung aus dem WHOOP-Tages-Strain, beides als **EWMA** — robust gegen
/// einzelne Tageslücken (Band nicht getragen = Last 0). Bewusst als **Form-Indikator
/// (Heuristik)** geframt, nicht als Verletzungs-Vorhersage: ACWR ist sportwissenschaftlich
/// umstritten. Reine, testbare Logik.
/// </summary>
public static class TrainingLoadCalculator
{
    public const int AcuteDays = 7;
    public const int ChronicDays = 28;

    /// <summary>Unter so vielen Strain-Tagen im Akut-Fenster gilt die Aussage als eingeschränkt (FA-10.02).</summary>
    public const int MinAcuteSamples = 5;

    private const double AcuteLambda = 2.0 / (AcuteDays + 1);
    private const double ChronicLambda = 2.0 / (ChronicDays + 1);

    /// <summary>
    /// Tagesreihe vom ersten bis zum letzten Datum der Historie (Kalenderlücken zählen als
    /// Last 0). Der Quotient fehlt, bis die chronische EWMA eingeschwungen ist
    /// (<see cref="ChronicDays"/> Tage Warmlauf).
    /// </summary>
    public static IReadOnlyList<TrainingLoadPoint> Compute(IReadOnlyList<WhoopDailyMetric> metrics)
    {
        var strainByDate = metrics
            .Where(m => m.DayStrain is not null)
            .ToDictionary(m => m.Date, m => m.DayStrain!.Value);
        if (strainByDate.Count == 0)
        {
            return [];
        }

        var first = strainByDate.Keys.Min();
        var last = strainByDate.Keys.Max();

        var points = new List<TrainingLoadPoint>();
        double acute = 0, chronic = 0;
        var dayIndex = 0;
        for (var date = first; date <= last; date = date.AddDays(1), dayIndex++)
        {
            var load = strainByDate.GetValueOrDefault(date, 0);
            acute = AcuteLambda * load + (1 - AcuteLambda) * acute;
            chronic = ChronicLambda * load + (1 - ChronicLambda) * chronic;

            var ratio = dayIndex >= ChronicDays && chronic > 0.01
                ? acute / chronic
                : (double?)null;
            points.Add(new TrainingLoadPoint(date, acute, chronic, ratio));
        }

        return points;
    }

    public static TrainingLoadZone ZoneFor(double ratio) => ratio switch
    {
        < 0.8 => TrainingLoadZone.Low,
        < 1.3 => TrainingLoadZone.Balanced,
        < 1.5 => TrainingLoadZone.Elevated,
        _ => TrainingLoadZone.High
    };

    /// <summary>Wie viele der letzten <see cref="AcuteDays"/> Tage bis <paramref name="lastDate"/> Strain-Daten haben.</summary>
    public static int AcuteDaysWithData(IReadOnlyList<WhoopDailyMetric> metrics, DateOnly lastDate)
    {
        var floor = lastDate.AddDays(-(AcuteDays - 1));
        return metrics.Count(m => m.DayStrain is not null && m.Date >= floor && m.Date <= lastDate);
    }
}
