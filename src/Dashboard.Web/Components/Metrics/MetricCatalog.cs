namespace Dashboard.Web.Components.Metrics;

/// <summary>
/// Zentraler Erklärungs-Katalog (FA-10.08/10.09): deutsche UI-Texte je <see cref="MetricId"/>.
/// Bewusst hier als reine, testbare Daten statt verstreut über die Razor-Seiten – ein Test
/// stellt sicher, dass jede <see cref="MetricId"/> einen vollständigen Eintrag besitzt.
/// </summary>
public static class MetricCatalog
{
    private static readonly IReadOnlyDictionary<MetricId, MetricExplanation> Entries =
        new Dictionary<MetricId, MetricExplanation>
        {
            // ---- /whoop – Metrik-Karten (30-Tage-Trends) ----
            [MetricId.WhoopRecovery] = new(
                "Recovery",
                "WHOOP-Erholungswert des Tages (0–100 %) im Verlauf der letzten 30 Tage. Fasst HRV, Ruhepuls, Schlaf und Vortagesbelastung zu einer Zahl zusammen.",
                "Höher = besser erholt. Grün ab ~67 %, Gelb ~34–66 %, Rot darunter. Achte auf den Trend über mehrere Tage, nicht auf einen einzelnen Wert.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "Recovery in %"),

            [MetricId.WhoopHrv] = new(
                "HRV (Herzratenvariabilität)",
                "Streuung der Abstände zwischen zwei Herzschlägen (RMSSD in ms), im Tiefschlaf gemessen – letzte 30 Tage.",
                "Höher ist meist besser (entspanntes vegetatives Nervensystem). Der „normale\" Bereich ist sehr individuell – vergleiche mit deinem eigenen Verlauf, nicht mit anderen.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "HRV in ms"),

            [MetricId.WhoopRestingHeartRate] = new(
                "Ruhepuls",
                "Ruheherzfrequenz im Schlaf (Schläge/min), letzte 30 Tage.",
                "Niedriger = in der Regel besser erholt bzw. fitter. Ein über mehrere Tage erhöhter Ruhepuls kann auf Stress, eine beginnende Erkrankung oder Übertraining hindeuten.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "Schläge pro Minute"),

            [MetricId.WhoopSleep] = new(
                "Schlaf",
                "Tatsächliche Schlafdauer pro Nacht (Stunden), letzte 30 Tage.",
                "Vergleiche mit deinem Schlafbedarf. Einzelne kurze Nächte sind normal; dauerhaft zu wenig drückt Recovery und HRV.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "Schlaf in Stunden"),

            [MetricId.WhoopStrain] = new(
                "Tages-Strain",
                "WHOOP-Tagesbelastung (Strain, Skala 0–21) – die kardiovaskuläre Gesamtbelastung des Tages, letzte 30 Tage.",
                "Hoher Strain bei gleichzeitig niedriger Recovery über mehrere Tage ist ein Warnsignal.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "Strain (0–21)",
                Method: "Die Strain-Skala ist logarithmisch: von 18 auf 19 steckt deutlich mehr Belastung als von 8 auf 9."),

            [MetricId.WhoopRespiratoryRate] = new(
                "Atemfrequenz",
                "Atemzüge pro Minute im Schlaf, letzte 30 Tage.",
                "Normalerweise sehr stabil. Ein deutlicher Anstieg (≥ ~1 Atemzug/min über deinem Schnitt) geht laut WHOOP oft einer Erkrankung voraus.",
                XAxis: "Zeit (letzte 30 Tage)",
                YAxis: "Atemzüge pro Minute"),

            // ---- /whoop – Analytics-Sektionen ----
            [MetricId.TrainingLoad] = new(
                "Trainingslast (Form-Indikator)",
                "Verhältnis aus akuter (~7 Tage) zu chronischer (~28 Tage) Belastung – das ACWR, berechnet aus dem täglichen WHOOP-Strain. Die Linie zeigt den Verlauf der letzten ~90 Tage.",
                "Um 1,0 = die Belastung passt zur Gewöhnung. Deutlich über 1,3–1,5 = schnelle Laststeigerung (laut Modell erhöhtes Risiko), unter 0,8 = Formverlust bzw. Tapering. Bewusst als Heuristik gedacht, keine Verletzungs-Vorhersage.",
                XAxis: "Zeit (letzte ~90 Tage)",
                YAxis: "ACWR-Verhältnis (akut ÷ chronisch)",
                Method: "EWMA-geglättetes ACWR (7d:28d) aus dem Tages-Strain; Zonenschwellen 0,8 / 1,3 / 1,5. Tage ohne getragenes Band zählen als Last 0."),

            [MetricId.AerobicFitness] = new(
                "Aerobe Fitness",
                "Monats-Durchschnitt der Herzschläge pro Kilometer beim Laufen, letzte 12 Monate. Beantwortet: Laufe ich bei gleicher Herzfrequenz schneller als vor ein paar Monaten?",
                "Niedriger = effizienter und fitter (weniger Herzschläge für dieselbe Strecke). Ein fallender Verlauf bedeutet steigende aerobe Fitness.",
                XAxis: "Zeit (letzte 12 Monate)",
                YAxis: "Herzschläge pro km (niedriger = besser)",
                Method: "Herzschläge/km = Ø-HF × Laufzeit (min) ÷ Distanz (km). Läufe unter 2 km ausgeklammert; Monats-Ø ab 2 Läufen; Trend = jüngster Monat vs. ~3 Monate zuvor."),

            [MetricId.RecoveryDrivers] = new(
                "Recovery-Treiber",
                "Wie stark hängt deine Recovery am Folgetag mit Schlafdauer, Einschlafzeit und der Vortagesbelastung zusammen? Korrelation über die letzten 12 Monate, dazu Streudiagramme.",
                "Werte nahe +1 oder −1 = starker Zusammenhang, nahe 0 = keiner. Korrelation ist kein Beweis für Ursache. Zeilen mit wenigen Datenpunkten (n) sind ausgegraut.",
                Method: "Pearson-Korrelationskoeffizient r, mindestens 10 Wertepaare je Treiber."),

            [MetricId.SleepNight] = new(
                "Letzte Nacht (Schlafphasen)",
                "Zusammensetzung der jüngsten Nacht aus Leicht-, Tief- und REM-Schlaf (plus Wachzeit, falls geliefert) als anteiliger Balken.",
                "Die Balkenbreite ist der Zeitanteil der jeweiligen Phase. Tief- und REM-Schlaf sind die erholsamsten Phasen. Tippe ein Segment für Details.",
                XAxis: "Zeitanteil der Nacht (0–100 %)"),

            [MetricId.TimeOfDay] = new(
                "Tageszeit-Effektivität",
                "Zu welcher Tageszeit holst du das meiste aus deinen Trainings? Je Trainingsart ein eigenes Effektivitätsmaß, aufgeschlüsselt nach sechs Zeitfenstern (letzte 12 Monate).",
                "Das hervorgehobene Fenster ist dein bestes. Eine belastbare Aussage gibt es erst ab 5 Trainings je Fenster – achte auf das n hinter jeder Zeile.",
                XAxis: "Effektivität (Balkenlänge)",
                YAxis: "Tageszeit-Fenster",
                Method: "Laufen: Herzschläge/km (niedriger = besser). Kraft/Seilspringen: kJ/min (höher = besser). Bestzeit-Verdict erst ab n ≥ 5."),

            [MetricId.TimeOfDayMatrix] = new(
                "Trainings-Häufigkeit",
                "Wann trainierst du? Anzahl Trainings je Tageszeit-Fenster (Zeilen) und Wochentag (Spalten) über alle Trainingsarten, letzte 12 Monate.",
                "Intensiver gefärbte Zellen = mehr Trainings in diesem Slot. Zeigt Gewohnheiten und Lücken – nicht die Effektivität (die steht in den Karten darüber).",
                XAxis: "Wochentag (Mo–So)",
                YAxis: "Tageszeit-Fenster"),

            [MetricId.SleepBedtime] = new(
                "Einschlafzeit → Recovery",
                "Durchschnittliche Recovery am Folgetag, gruppiert danach, wann du eingeschlafen bist (letzte 12 Monate).",
                "Längster Balken = das Einschlaf-Fenster mit der besten Folgetags-Recovery. Verrät, ob früheres Zubettgehen sich bei dir auszahlt. n beachten.",
                XAxis: "Ø Recovery am Folgetag (%)",
                YAxis: "Einschlaf-Fenster",
                Method: "Einschlafzeit über einen 18-Uhr-Anker gemittelt (Mitternachts-Umbruch). Fenster: vor 22:30 / 22:30–23:30 / 23:30–00:30 / nach 00:30."),

            [MetricId.SleepDuration] = new(
                "Schlafdauer → Recovery",
                "Durchschnittliche Recovery am Folgetag, gruppiert nach Schlafdauer (letzte 12 Monate).",
                "Zeigt, ab wie vielen Stunden Schlaf deine Recovery spürbar besser wird. Längster Balken = beste Folgetags-Recovery. n beachten.",
                XAxis: "Ø Recovery am Folgetag (%)",
                YAxis: "Schlafdauer-Bucket",
                Method: "Dauer-Buckets: unter 6 / 6–7 / 7–8 / über 8 Stunden."),

            [MetricId.WhoopRuns] = new(
                "Läufe nach Recovery",
                "Deine Läufe im Zeitraum, jeweils mit dem Recovery-Farbpunkt des betreffenden Tages.",
                "Grün/Gelb/Rot zeigt, mit welcher Erholung du in den Lauf gegangen bist. Hilft zu erkennen, ob du an roten Tagen kürzertrittst.",
                Method: "Workouts vom Typ Laufen, den WHOOP-Tageswerten nach Datum zugeordnet."),

            // ---- /runs ----
            [MetricId.RunYearReview] = new(
                "Jahresrückblick",
                "Lauf-Bilanz des gewählten Jahres: Anzahl, Gesamt-Kilometer, Höhenmeter, Zeit und Eddington-Zahl – dazu Kilometer pro Monat als Balken und die Jahres-Rekorde.",
                "Die Monatsbalken zeigen die km-Verteilung übers Jahr. Über die Jahres-Buttons wechselst du das Jahr.",
                XAxis: "Monat",
                YAxis: "Kilometer pro Monat",
                Method: "Eddington-Zahl E: an E Tagen mindestens je E km gelaufen – ein Maß für konstante Distanz, das mit steigendem E immer schwerer wächst."),

            [MetricId.RouteClusters] = new(
                "Standard-Runden",
                "Wiederkehrende Strecken: ähnliche GPS-Tracks werden automatisch zu „Runden\" zusammengefasst – mit Anzahl, Ø-Distanz, Ø-Pace und Bestzeit je Runde.",
                "Zeigt deine Hausstrecken und wie schnell du sie im Schnitt bzw. bestenfalls läufst. Eine Runde erkennt das System richtungsunabhängig.",
                Method: "Clustering über die Hausdorff-Distanz auf vereinfachten, metrisch projizierten Tracks (Schwelle ~150 m, Distanz ±15 %)."),

            [MetricId.RunList] = new(
                "Lauf-Liste",
                "Deine Läufe im gewählten Zeitraum mit Datum, Name, Distanz, Pace, Ø-Herzfrequenz und Höhenmetern.",
                "Tippe eine Zeile, um das Detail mit Verlaufsprofilen und Bestzeiten zu öffnen. Den Zeitraum (4 Wochen / 12 Monate / alle) stellst du über die Buttons ein.",
                Method: "Werte aus dem Strava-Sync; Pace = Zeit ÷ Distanz."),

            // ---- /runs/{id} ----
            [MetricId.RunPaceProfile] = new(
                "Pace-Profil",
                "Verlauf der Pace über die Laufstrecke, aus den gespeicherten GPS-Streams.",
                "Bewusst invertiert dargestellt: oben = schneller, unten = langsamer. So erkennst du Tempowechsel, Anstiege (langsamer) und den Endspurt. Der Bereich unter der Kurve nennt die schnellste und langsamste Pace.",
                XAxis: "Distanz (Lauf-Verlauf)",
                YAxis: "Pace in min/km (oben = schneller)"),

            [MetricId.RunElevationProfile] = new(
                "Höhen-Profil",
                "Höhenverlauf über die Laufstrecke in Metern.",
                "Anstiege und Gefälle entlang der Strecke. Der Bereich darunter nennt tiefsten und höchsten Punkt.",
                XAxis: "Distanz (Lauf-Verlauf)",
                YAxis: "Höhe in Metern"),

            [MetricId.RunHeartRateProfile] = new(
                "Herzfrequenz-Profil",
                "Herzfrequenz-Verlauf über die Laufstrecke (Schläge/min).",
                "Steigt typischerweise mit Anstrengung und an Steigungen und „driftet\" bei langen Läufen nach oben. Der Bereich darunter nennt Minimum und Maximum.",
                XAxis: "Distanz (Lauf-Verlauf)",
                YAxis: "Herzfrequenz in bpm"),

            [MetricId.RunBestEfforts] = new(
                "Bestzeiten in diesem Lauf",
                "Die schnellsten zusammenhängenden Abschnitte dieses Laufs (z. B. beste 1 km, 5 km, 10 km).",
                "Es ist die schnellste Teilstrecke innerhalb genau dieses Laufs – nicht deine Allzeit-Bestzeit.",
                Method: "Schnellstes Zeitfenster je Zieldistanz, per gleitendem Fenster über die Zeit-/GPS-Streams."),

            // ---- /habits ----
            [MetricId.HabitHeatmap] = new(
                "Jahres-Heatmap",
                "Kalender der erledigten Habits: jede Zelle ist ein Tag, dunkler = mehr an dem Tag erledigt (bzw. erledigt im gewählten Habit-Filter).",
                "Wie die GitHub-Beitragsgrafik – zeigt Konstanz übers Jahr und Lücken auf einen Blick. Über die Tabs nach einzelnem Habit filtern.",
                XAxis: "Kalenderwochen",
                YAxis: "Wochentag (Mo–So)"),

            [MetricId.HabitWeeklyBars] = new(
                "Letzte 12 Wochen",
                "Anzahl erledigter Habit-Einträge je Woche über die letzten 12 Wochen.",
                "Höherer Balken = mehr Einträge in der Woche. Gut, um den aktuellen Trend gegen das Jahresbild zu sehen.",
                XAxis: "Woche (Startdatum)",
                YAxis: "Einträge pro Woche"),

            [MetricId.HabitStreaks] = new(
                "Serien (Streaks)",
                "Aktuelle und längste Serie. Mit Habit-Filter zählt die Wochen-Serie (Wochen in Folge mit ≥ 1 Eintrag); ohne Filter die Tages-Serie über alle Habits zusammen.",
                "Wochen-Serien sind für 3×/Woche-Habits sinnvoller als Tages-Serien. Die laufende Woche bzw. der heutige Tag haben Karenz, damit die Serie nicht zu jedem Wochen-/Tagesbeginn abreißt.",
                Method: "Längste historische Serie wird mitgeführt; Mo-basierte Wochen in Berlin-Zeit."),

            // ---- /status ----
            [MetricId.StatusSources] = new(
                "Datenquellen",
                "Ampel je Datenquelle (Wetter, Fußball, HVV, WHOOP …): grün = frisch, gelb = veraltet, plus Zeitpunkt des letzten erfolgreichen Abrufs.",
                "„Veraltet\" heißt: länger kein erfolgreicher Abruf als für diese Quelle üblich. Nachts ist z. B. der HVV-Monitor naturgemäß ruhiger. Ein dauerhaft gelber Punkt deutet auf ein Problem hin (Token/Netz).",
                Method: "Status direkt aus den laufenden Slice-States gelesen (on-demand, kein eigener Poll)."),

            [MetricId.StatusStrava] = new(
                "Strava-Status",
                "Verbindungs- und Sync-Zustand von Strava: letzter Sync, letzter Fehler, Anzahl Läufe, geladene Streams und Stand des einmaligen Metrik-Re-Syncs.",
                "„Streams x/y geladen\" ist der Fortschritt des Detaildaten-Backfills (für Verlaufsprofile und Bestzeiten). Backfills laufen gedrosselt über mehrere Zyklen – x wächst langsam.",
                Method: "Aus dem Strava-Sync-State und der Lauf-Tabelle."),

            [MetricId.StatusWhoop] = new(
                "WHOOP-Status",
                "Verbindung sowie Historien-Tiefe der gespeicherten WHOOP-Tageswerte und Workouts.",
                "Die Tiefe (z. B. „seit 320 Tagen\") zeigt, wie weit der historische Backfill schon zurückreicht – die Tageszeit- und Schlaf-Analysen brauchen Monate an Historie, um aussagekräftig zu sein.",
                Method: "Aus dem ältesten gespeicherten Tageswert bzw. Workout."),

            [MetricId.StatusSystem] = new(
                "System (Host)",
                "Hardware-Werte des Raspberry Pi: CPU-Temperatur, RAM-Auslastung und Load-Average (nur unter Linux verfügbar).",
                "CPU-Temperatur dauerhaft über ~80 °C → Kühlung prüfen. Der Load-Average entspricht grob der Zahl wartender Prozesse; dauerhaft über der Kern-Anzahl heißt überlastet.",
                Method: "Aus /sys und /proc gelesen; unter macOS/Windows ausgeblendet."),
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
