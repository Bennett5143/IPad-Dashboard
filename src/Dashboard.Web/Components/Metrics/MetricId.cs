namespace Dashboard.Web.Components.Metrics;

/// <summary>
/// Stabile Kennung jeder erklärbaren Metrik/Visualisierung (FA-10.08). Jeder Wert hat genau
/// einen Eintrag im <see cref="MetricCatalog"/> – per Test abgesichert. Neue Auswertung →
/// Wert hier ergänzen und Erklärung im Katalog hinterlegen.
/// </summary>
public enum MetricId
{
    // /whoop – Metrik-Karten (30-Tage-Trends)
    WhoopRecovery,
    WhoopHrv,
    WhoopRestingHeartRate,
    WhoopSleep,
    WhoopStrain,
    WhoopRespiratoryRate,

    // /whoop – Analytics-Sektionen
    TrainingLoad,
    AerobicFitness,
    RecoveryDrivers,
    SleepNight,
    TimeOfDay,
    TimeOfDayMatrix,
    SleepBedtime,
    SleepDuration,
    WhoopRuns,

    // /runs
    RunYearReview,
    RouteClusters,
    RunList,

    // /runs/{id}
    RunPaceProfile,
    RunElevationProfile,
    RunHeartRateProfile,
    RunBestEfforts,

    // /habits
    HabitHeatmap,
    HabitWeeklyBars,
    HabitStreaks,

    // /status
    StatusSources,
    StatusStrava,
    StatusWhoop,
    StatusSystem,
}
