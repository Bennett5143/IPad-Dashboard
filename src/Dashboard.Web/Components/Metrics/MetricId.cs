namespace Dashboard.Web.Components.Metrics;

/// <summary>
/// Stabile Kennung jeder erklärbaren Metrik/Visualisierung (FA-10.08). Jeder Wert hat genau
/// einen Eintrag im <see cref="MetricCatalog"/> – per Test abgesichert. Neue Auswertung →
/// Wert hier ergänzen und Erklärung im Katalog hinterlegen.
///
/// Bewusst NICHT erklärt (selbsterklärend, kein Popup): Lauf-Liste, „Läufe nach Recovery",
/// Wochen-Balken, /status-Fakten. Standard-Runden werden direkt antippbar (→ Heatmap) statt
/// per Popup erklärt.
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

    // /runs
    RunYearReview,

    // /runs/{id}
    RunPaceProfile,
    RunElevationProfile,
    RunHeartRateProfile,
    RunBestEfforts,

    // /habits
    HabitHeatmap,
    HabitStreaks,
}
