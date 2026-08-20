namespace Dashboard.Web.Components.Metrics;

/// <summary>
/// Zentraler Erklärungs-Katalog (FA-10.08/10.09): sachliche deutsche UI-Texte je
/// <see cref="MetricId"/>. Bewusst hier als reine, testbare Daten statt verstreut über die
/// Razor-Seiten – ein Test stellt sicher, dass jede <see cref="MetricId"/> einen vollständigen
/// Eintrag besitzt.
/// </summary>
public static class MetricCatalog
{
    private static readonly IReadOnlyDictionary<MetricId, MetricExplanation> Entries =
        new Dictionary<MetricId, MetricExplanation>
        {
            // ---- /whoop – Metrik-Karten (30-Tage-Trends) ----
            [MetricId.WhoopRecovery] = new(
                "Recovery",
                "Der tägliche WHOOP-Erholungswert (0–100 %) der letzten 30 Tage.",
                "Von WHOOP aus HRV, Ruhepuls, Schlaf und Vortagesbelastung berechnet.",
                "Höhere Werte stehen für bessere Erholung; aussagekräftig ist der Trend über mehrere Tage. Grün ab ~67 %, Gelb ~34–66 %, Rot darunter.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "Recovery in %"),

            [MetricId.WhoopHrv] = new(
                "HRV (Herzratenvariabilität)",
                "Die Herzratenvariabilität (RMSSD in Millisekunden) der letzten 30 Tage.",
                "Im Tiefschlaf gemessene Streuung der Abstände zwischen zwei Herzschlägen.",
                "Höhere Werte deuten meist auf bessere Erholung hin. Der normale Bereich ist stark individuell – der eigene Verlauf zählt, nicht ein absoluter Zielwert.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "HRV in ms"),

            [MetricId.WhoopRestingHeartRate] = new(
                "Ruhepuls",
                "Der Ruhepuls im Schlaf (Schläge pro Minute) der letzten 30 Tage.",
                "Niedrigste Herzfrequenz während der Schlafphasen, von WHOOP gemessen.",
                "Ein niedrigerer Ruhepuls spricht in der Regel für bessere Erholung oder Fitness; mehrere erhöhte Tage können auf Stress, Krankheit oder Übertraining hindeuten.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "Schläge pro Minute"),

            [MetricId.WhoopSleep] = new(
                "Schlaf",
                "Die Schlafdauer pro Nacht (Stunden) der letzten 30 Tage.",
                "Tatsächliche Schlafzeit ohne Wachphasen, von WHOOP erfasst.",
                "Zeigt, ob der Schlafbedarf regelmäßig gedeckt wird; dauerhaft kurze Nächte drücken Recovery und HRV.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "Schlaf in Stunden"),

            [MetricId.WhoopStrain] = new(
                "Tages-Strain",
                "Die tägliche Gesamtbelastung (WHOOP-Strain, Skala 0–21) der letzten 30 Tage.",
                "Kardiovaskuläre Belastung über den Tag; die Skala ist logarithmisch (von 18 auf 19 steckt mehr Belastung als von 8 auf 9).",
                "Hoher Strain bei gleichzeitig niedriger Recovery über mehrere Tage ist ein Warnsignal für Überlastung.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "Strain (0–21)"),

            [MetricId.WhoopRespiratoryRate] = new(
                "Atemfrequenz",
                "Die Atemfrequenz im Schlaf (Atemzüge pro Minute) der letzten 30 Tage.",
                "Während des Schlafs gemessen; normalerweise sehr stabil.",
                "Ein deutlicher Anstieg (etwa ab einem Atemzug pro Minute über dem Schnitt) geht laut WHOOP häufig einer Erkrankung voraus.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "Atemzüge pro Minute"),

            // ---- /whoop – Analytics-Sektionen ----
            [MetricId.TrainingLoad] = new(
                "Trainingslast (Form-Indikator)",
                "Das Verhältnis aus akuter (~7 Tage) zu chronischer (~28 Tage) Belastung im Verlauf der letzten ~90 Tage.",
                "EWMA-geglättetes ACWR aus dem täglichen WHOOP-Strain; Zonenschwellen 0,8 / 1,3 / 1,5. Tage ohne getragenes Band zählen als Last 0.",
                "Werte um 1,0 bedeuten, die Belastung passt zur Gewöhnung; über ~1,3–1,5 steigt die Last schnell (laut Modell erhöhtes Risiko), unter 0,8 sinkt die Form. Eine Heuristik, keine Verletzungsvorhersage.",
                XAxis: "Zeit (letzte ~90 Tage)",
                YAxis: "ACWR-Verhältnis (akut ÷ chronisch)"),

            [MetricId.AerobicFitness] = new(
                "Aerobe Fitness",
                "Der Monats-Durchschnitt der Herzschläge pro Kilometer beim Laufen über die letzten 12 Monate.",
                "Herzschläge/km = Ø-Herzfrequenz × Laufzeit (min) ÷ Distanz (km). Läufe unter 2 km bleiben außen vor; Monatswert ab zwei Läufen.",
                "Weniger Herzschläge pro Kilometer bedeuten höhere aerobe Effizienz. Ein fallender Verlauf zeigt steigende Fitness bei gleicher Herzfrequenz – etwas, das Strava (Free) und WHOOP nicht ausweisen.",
                XAxis: "Zeit (letzte 12 Monate)",
                YAxis: "Herzschläge pro km (weniger = besser)"),

            [MetricId.RecoveryDrivers] = new(
                "Recovery-Treiber",
                "Der Zusammenhang zwischen der Recovery am Folgetag und Schlafdauer, Einschlafzeit sowie Vortagesbelastung – mit Streudiagrammen, über die letzten 12 Monate.",
                "Pearson-Korrelationskoeffizient r über mindestens zehn Wertepaare je Treiber.",
                "Werte nahe +1 oder −1 zeigen einen starken, nahe 0 keinen Zusammenhang. Korrelation ist kein Kausalbeweis; Zeilen mit wenigen Datenpunkten (n) sind ausgegraut."),

            [MetricId.TimeOfDayMatrix] = new(
                "Trainings-Häufigkeit",
                "Die Anzahl Trainings je Tageszeit-Fenster (Zeilen) und Wochentag (Spalten) über alle Arten, letzte 12 Monate.",
                "Auszählung der Workouts aus der WHOOP-Historie; intensivere Färbung bedeutet mehr Trainings.",
                "Macht Gewohnheiten und Lücken im Wochenrhythmus sichtbar – unabhängig von der Effektivität.",
                XAxis: "Wochentag (Mo–So)",
                YAxis: "Tageszeit-Fenster"),

            [MetricId.SleepBedtime] = new(
                "Einschlafzeit → Recovery",
                "Die durchschnittliche Recovery am Folgetag, gruppiert nach Einschlafzeit, über die letzten 12 Monate.",
                "Einschlafzeit über einen 18-Uhr-Anker gemittelt; Fenster: vor 22:30 / 22:30–23:30 / 23:30–00:30 / nach 00:30.",
                "Zeigt, welches Einschlaf-Fenster mit der besten Folgetags-Recovery einhergeht; der längste Balken markiert das beste Fenster (n beachten).",
                XAxis: "Ø Recovery am Folgetag (%)",
                YAxis: "Einschlaf-Fenster"),

            [MetricId.SleepDuration] = new(
                "Schlafdauer → Recovery",
                "Die durchschnittliche Recovery am Folgetag, gruppiert nach Schlafdauer, über die letzten 12 Monate.",
                "Dauer-Buckets: unter 6 / 6–7 / 7–8 / über 8 Stunden.",
                "Zeigt, ab welcher Schlafdauer die Recovery spürbar besser ausfällt (n beachten).",
                XAxis: "Ø Recovery am Folgetag (%)",
                YAxis: "Schlafdauer-Bucket"),

            // ---- /runs ----
            [MetricId.RunYearReview] = new(
                "Jahresrückblick",
                "Die Lauf-Bilanz des gewählten Jahres: Kennzahlen, Kilometer pro Monat und Rekorde.",
                "Aus allen synchronisierten Läufen des Jahres. Eddington-Zahl E: an E Tagen jeweils mindestens E km gelaufen.",
                "Die Monatsbalken zeigen die Verteilung übers Jahr; die Eddington-Zahl misst konstante Distanz und wächst mit steigendem E immer schwerer.",
                XAxis: "Monat",
                YAxis: "Kilometer pro Monat"),

            // ---- /habits ----
            [MetricId.HabitHeatmap] = new(
                "Jahres-Heatmap",
                "Ein Kalender der erledigten Habits: je Zelle ein Tag, dunkler bedeutet an dem Tag mehr erledigt.",
                "Aus den Habit-Einträgen; über die Tabs nach einzelnem Habit filterbar.",
                "Zeigt Konstanz übers Jahr und Lücken auf einen Blick – wie die GitHub-Beitragsgrafik.",
                XAxis: "Kalenderwochen",
                YAxis: "Wochentag (Mo–So)"),

        };

    /// <summary>
    /// Erklärung zu einer Metrik. Wirft <see cref="KeyNotFoundException"/>, wenn keine hinterlegt
    /// ist – per <c>MetricCatalogTests</c> ausgeschlossen, sodass die Seiten gefahrlos zugreifen.
    /// </summary>
    public static MetricExplanation Get(MetricId id) =>
        Entries.TryGetValue(id, out var explanation)
            ? explanation
            : throw new KeyNotFoundException($"Keine Erklärung für Metrik '{id}' im MetricCatalog hinterlegt.");

    /// <summary>Alle hinterlegten IDs – für Vollständigkeits-Tests.</summary>
    public static IReadOnlyCollection<MetricId> Ids => Entries.Keys.ToArray();
}
