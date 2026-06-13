# iPad-Dashboard

[![CI](https://github.com/Bennett5143/IPad-Dashboard/actions/workflows/ci.yml/badge.svg)](https://github.com/Bennett5143/IPad-Dashboard/actions/workflows/ci.yml)

Selbstgehostetes Dashboard für ein iPad 6th Gen, das auf einem
Raspberry Pi 4 läuft. Das Kiosk-Dashboard (`/`) zeigt: Uhrzeit, Wetter,
Habit-Tracker, Fußball-Ergebnisse, HVV-Abfahrten, WHOOP-Recovery und das
Zitat des Tages.

Auf Unterseiten ist es zu einem **persönlichen Fitness-Daten-Hub** ausgebaut,
der WHOOP- und Strava-Daten bündelt und Auswertungen baut, die keine einzelne
App zeigt: Lauf-Heatmap (`/heatmap`), Lauf-Liste + Detailprofile (`/runs`),
WHOOP-Insights mit Tageszeit-/Schlaf-/Trainingslast-Analysen (`/whoop`),
Habit-Verlauf (`/habits`) und eine Status-/Observability-Seite (`/status`).

## Tech-Stack

- C# / .NET 10 mit Blazor Server
- Entity Framework Core 10 (Npgsql Provider) + NetTopologySuite
- PostgreSQL 16 mit **PostGIS** (Lauf-Tracks als `geometry(LineString,4326)`)
- Serilog (Konsole + JSON-Dateien)
- Leaflet (clientseitig) für die Heatmap, Inline-SVG für Charts
- Docker / docker-compose
- Hosting auf Raspberry Pi 4 (ARM64)

## Projektstruktur

```
src/
├── Dashboard.Web              # Blazor Server, Entrypoint, UI
│   ├── Components/
│   │   ├── Layout/            # KioskLayout (/), MainLayout (Unterseiten) + SubpageNav
│   │   ├── Pages/             # Home (/), Heatmap (/heatmap), Runs (/runs) + RunDetail,
│   │   │                      #   Habits (/habits), WhoopInsights (/whoop), Status (/status),
│   │   │                      #   Admin/Quotes — je mit testbaren *Builder-Klassen
│   │   ├── Tile.razor         # Container-Komponente mit Header/Body-Slot
│   │   ├── TileBoundary.razor # ErrorBoundary-Wrapper pro Tile
│   │   ├── Sparkline / Scatter# Inline-SVG-Charts
│   │   └── Tiles/             # Konkrete Tile-Implementierungen + Formatter
│   └── wwwroot/               # app.css (Design-Tokens), js/heatmap.js
├── Dashboard.Domain           # reine Logik: Entities/ValueObjects/Enums, Ports (Interfaces),
│                              #   Slice-Rechner (Whoop-/Running-/Habits-Analytics), Common, Status
└── Dashboard.Infrastructure   # DbContext, Migrationen, Seeder, API-Clients, BackgroundServices, Stores
tests/
└── Dashboard.Tests            # xUnit (Domänenlogik + Clients via Stub-HttpHandler + FakeClock)
```

Cross-cutting MSBuild-Properties werden zentral via `Directory.Build.props`
verwaltet, NuGet-Versionen via `Directory.Packages.props` (Central Package
Management). Beide liegen im Repo-Root.

## UI-Architektur

Das Dashboard nutzt zwei Layouts: `KioskLayout` für die Dashboard-Page (`/`)
– Vollbild ohne Navigation, optimiert für den Kiosk-Modus – und `MainLayout`
für alle Unterseiten (`/heatmap`, `/runs`, `/habits`, `/whoop`, `/status`,
`/admin/*`), die sich eine gemeinsame `SubpageNav` (Quer-Navigation) teilen
und scrollen dürfen.

Optik: dunkles „Liquid Glass HUD" (No-Scroll-Layout für das iPad). Die
Hauptansicht ist bewusst auf 1024×768 fixiert; die Kacheln schrumpfen/clippen
statt zu scrollen.

Jede Kachel auf dem Dashboard ist mit einer `TileBoundary` umschlossen. Eine
crashende Tile (z.B. weil eine externe API nicht erreichbar ist) zeigt damit
nur lokal eine „Daten nicht verfügbar"-Meldung, ohne den Rest des Dashboards
in Mitleidenschaft zu ziehen. Die optische Hülle (Card mit Rahmen, Schatten,
Header) stellt die `Tile`-Komponente bereit; der konkrete Inhalt steckt in
Tiles unter `Components/Tiles/`. Komposition statt Vererbung.

Globale Design-Tokens (Farben, Spacing-Scale, Typografie) liegen als CSS
Custom Properties in `wwwroot/app.css`. Komponenten-spezifisches Styling
nutzt Blazor's CSS Isolation via `*.razor.css`-Dateien.

## Status

- ✅ **Phasen 0–3:** Repo, Dev-Umgebung, CI/Coverage/Branch-Protection/Dependabot, Dashboard-Skelett
- ✅ **Phase 4 – Features:** Uhrzeit & Datum, Zitat des Tages, Habit-Tracker (inkl. Backfill/EMOM),
  Wetter (OpenWeatherMap), Fußball (football-data.org), HVV-Abfahrtsmonitor
- ✅ **Phase 7 – Lauf-Heatmap:** Strava-OAuth-Sync, PostGIS, Leaflet, Layer (Pace/Höhe/Richtung/HF)
- ✅ **Phase 8 – WHOOP:** Recovery-Kachel, Habit-Auto-Fill, Insights-Seite
- ✅ **Redesign:** dunkles „Liquid Glass HUD", No-Scroll-iPad-Layout, generisches `ObservableState<T>`
- ✅ **Fitness-Daten-Hub (Phasen 9–14):**
  - Daten-Fundament: WHOOP-Historie/-Workouts + Strava-Metriken persistiert (Backfills)
  - Analytics auf `/whoop`: Tageszeit-Effektivität, Schlafenszeiten, Trainingslast (ACWR),
    aerobe Fitness-Kurve, Recovery-Treiber
  - Lauf-Vertiefung: klickbare Heatmap, `/runs` Liste + Detailprofile, Year in Review,
    Routen-Erkennung, Best Efforts
  - Habits: `/habits`-Jahres-Heatmap + Streaks · Observability: `/status` + Log-Ringpuffer
  - Quick-Wins: Liga-Tabelle & Wochenkalender auf Tap, Wetter-Extras, Quer-Navigation
- ⬜ **Offen:** Container/Pi-Deployment (Phase 5) & Kiosk-Hardening (Phase 6); Kachel-Redesign (Phase 14.1)

> Anforderungen, Phasenplan und detaillierte Feature-Roadmap werden außerhalb des Repos
> in einem Obsidian-Vault gepflegt.

## Setup

### Voraussetzungen

- .NET 10 SDK
- Docker / Docker Compose
- EF Core CLI (einmalig global installieren):
```bash
  dotnet tool install --global dotnet-ef
```

### Lokale Inbetriebnahme

1. **Postgres-Container starten:**
```bash
   docker compose up -d db
```

2. **Connection-String per User Secrets setzen** (nicht in `appsettings.json`!):
```bash
   cd src/Dashboard.Web
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
     "Host=localhost;Port=5432;Database=dashboard;Username=...;Password=..."
```

3. **Datenbank migrieren** (manuell, wird nicht beim Start automatisch ausgeführt):
```bash
   dotnet ef database update
```

4. **App starten:**
```bash
   dotnet run
```

Beim ersten Start seedet die App die initialen Zitate (idempotent –
spätere Starts überspringen das Seeding automatisch, wenn bereits
Einträge vorhanden sind).

### Health-Checks

- `GET /health/live` – Liveness (antwortet die App auf HTTP?)
- `GET /health/ready` – Readiness (inkl. DB-Erreichbarkeit)

### Logging

Serilog schreibt parallel auf zwei Sinks:

- **Konsole** – menschenlesbares Format für die Entwicklung
- **Datei** – tägliche, JSON-formatierte Rotation in
  `src/Dashboard.Web/logs/dashboard-YYYYMMDD.log` (14 Tage Retention)

## Konfiguration

### Seeding

In `appsettings.json` steuerbar:

```json
"Seeding": {
  "Enabled": true
}
```

### Wetter (OpenWeatherMap)

Standort, Sprache und Aktualisierungsintervall stehen in `appsettings.json`
(Sektion `Weather`). Der API-Key ist ein Geheimnis und gehört **nicht** ins
Repo – lokal via User Secrets:

```bash
cd src/Dashboard.Web
dotnet user-secrets set "Weather:ApiKey" "<dein-openweathermap-key>"
```

Ohne Key startet die App normal; die Wetter-Kacheln bleiben im freundlichen
„Daten gerade nicht verfügbar"-Zustand (Graceful Degradation). Ein
`WeatherRefreshService` (`BackgroundService`) pollt im konfigurierten Intervall
(Standard 15 min) die Endpunkte `data/2.5/weather` und `data/2.5/forecast`,
legt das Ergebnis prozessweit in `WeatherState` ab und pusht Aktualisierungen
ohne Reload an die Kacheln. Die stündliche Vorschau nutzt das 3-Stunden-Raster
des kostenlosen Forecast-Endpoints.

### Fußball (football-data.org)

Vereine und Intervall stehen in `appsettings.json` (Sektion `Football`); je
Verein eine football-data.org-`TeamId` plus Liga-`CompetitionCode` (z. B. `PD`
für La Liga, `BL1` für die Bundesliga). Der API-Key (Header `X-Auth-Token`)
gehört in User Secrets:

```bash
cd src/Dashboard.Web
dotnet user-secrets set "Football:ApiKey" "<dein-football-data-token>"
```

Ein `FootballRefreshService` (`BackgroundService`) holt pro Verein die
Saison-Spielliste und die Ligatabelle (Default-Intervall 30 min, schont das
Free-Tier-Limit von 10 Requests/min), löst Ergebnisse/Spiele in die
Vereins-Perspektive auf (Gegner, Heim/Auswärts, eigene Tore) und pusht sie via
`FootballState` an die Kachel. Ohne Key/Vereine bleibt die Kachel im
„nicht verfügbar"-Zustand.

### HVV-Abfahrtsmonitor

Haltestellen gehören **nicht** ins versionierte `appsettings.json` (sie verraten
die ungefähre Wohngegend), sondern in die **gitignored `appsettings.Local.json`**
(Sektion `Hvv`) – kein API-Key nötig. Vorlage kopieren und anpassen:

```bash
cd src/Dashboard.Web
cp appsettings.Local.json.example appsettings.Local.json
# danach Stationen eintragen
```

`appsettings.Local.json` wird **umgebungsunabhängig** geladen (auch in Production /
auf dem Raspberry Pi) und überschreibt `appsettings.json`. Je Haltestelle eine
`MasterId` und optionale `Lines`-Filter (`Line` = Liniennname, `Direction` =
Teilstring des Richtungstexts, case-insensitiv; leer = alle Abfahrten). Beispiel:

```json
{ "Name": "Beispielhaltestelle", "MasterId": "Master:00000", "City": "Hamburg",
  "Lines": [ { "Line": "42", "Direction": "Zielrichtung" } ] }
```

Die `MasterId` und die Richtungstexte lassen sich über den unauthentifizierten
`geofox/checkName`- bzw. `geofox/departureList`-Endpoint ermitteln (alternativ
der Abfahrts-Generator auf <https://abfahrten.hvv.de>). Die drei Kacheln auf
dem Dashboard binden über `StationIndex` (0–2) an die konfigurierten Haltestellen.

Ein `HvvRefreshService` (`BackgroundService`) pollt den inoffiziellen
`geofox/departureList`-Endpoint konservativ (mind. 60 s pro Haltestelle,
FA-6.04) und legt das Ergebnis in `HvvState` ab. Gefiltert wird **client-seitig**
nach Linie + Richtungstext (der serverseitige Filter bräuchte fragile
next-stop-IDs). `delay` wird als nullable `TimeSpan` modelliert (`null` = keine
Echtzeitdaten ≠ pünktlich), `timeOffset` relativ zur gelieferten Server-Zeit
gerechnet. Bei Ausfall bleibt der letzte Stand erhalten bzw. die Kachel zeigt
freundlich „nicht verfügbar" (FA-6.05).

> Hinweis: Der Endpoint ist inoffiziell (rechtliche Grauzone, private Nutzung,
> ein Gerät, ≤ 1 Req/min). Details in der Recherche-Notiz.

### Strava / Lauf-Heatmap (Phase 7)

GPS-Tracks der Läufe werden via offizieller Strava-API (OAuth2) in die lokale
DB synchronisiert und als PostGIS-`geometry(LineString,4326)` gespeichert; die
Heatmap unter `/heatmap` rendert sie clientseitig mit Leaflet.

**Voraussetzung DB:** PostGIS — `docker-compose.yml` nutzt dafür das
`imresamu/postgis`-Image (Multi-Arch-Fork mit amd64 **und** arm64) statt
`postgres:alpine`. Das offizielle `postgis/postgis`-Image hat kein
arm64-Manifest und läuft daher weder auf Apple Silicon noch auf dem
Raspberry Pi 4. Die Migration legt die Extension an.

Eine eigene Strava-API-App anlegen (<https://www.strava.com/settings/api>),
als „Authorization Callback Domain" `localhost` eintragen und Client-ID/Secret
als User Secrets setzen:

```bash
cd src/Dashboard.Web
dotnet user-secrets set "Strava:ClientId" "<client-id>"
dotnet user-secrets set "Strava:ClientSecret" "<client-secret>"
```

Verbinden über `/strava/connect` (oder den Link in der Habit-Kachel, FA-8.04).
Ein `StravaSyncService` (`BackgroundService`) lädt beim Erst-Sync die Historie
und danach inkrementell neue Läufe (nur `Run`/`TrailRun`), respektiert das
Rate-Limit (pausiert bei HTTP 429 statt abzubrechen) und merkt sich den letzten
erfolgreichen Sync. Tokens werden serverseitig in der DB gehalten. Ohne
Verbindung zeigt `/heatmap` einen „Mit Strava verbinden"-Hinweis.

> Hinweis: Die Heatmap lädt Leaflet und OSM-Kartenkacheln über das Internet
> (CDN/Tile-Server) – im reinen Offline-Kiosk müsste man diese vendoren.

### WHOOP (Phase 8)

Recovery/Schlaf/Strain via offizieller WHOOP-API (v2, OAuth2, kostenlos). Client-ID/Secret
als User Secrets:

```bash
cd src/Dashboard.Web
dotnet user-secrets set "Whoop:ClientId" "<client-id>"
dotnet user-secrets set "Whoop:ClientSecret" "<client-secret>"
```

Verbinden über `/whoop/connect` (Redirect-URI muss `https` sein → `https`-Profil). Ein
`WhoopRefreshService` pollt periodisch, hakt passende Habits automatisch ab und
persistiert Tageswerte + Workouts (Tabellen `WhoopDailyMetrics`/`WhoopWorkouts`) für die
Auswertungen auf `/whoop`. `appsettings.json` → Sektion `Whoop` (u. a. `BackfillDays`,
Default 365). Ohne Token bleibt die Kachel im „nicht verbunden"-Zustand.

> Die WHOOP-Developer-API liefert **kein GPS** (Heatmap bleibt Strava-Sache) und **keine
> Schritte**; Gewicht nur als aktueller App-Wert ohne Historie.

### Secrets-Management

Sensible Daten (Connection-Strings, API-Keys) gehören **nicht**
in `appsettings.json` und damit nicht ins Repo:

- **Lokal:** `dotnet user-secrets`
- **Container:** `.env`-Datei (in `.gitignore`)
- **CI/CD:** GitHub Secrets

Daten, die zwar kein Geheimnis, aber **privat** sind (z. B. die konkreten
HVV-Haltestellen in Wohnortnähe oder der Wetter-Standort), gehören in die
gitignored **`appsettings.Local.json`** (Vorlage: `appsettings.Local.json.example`).
Sie wird umgebungsunabhängig geladen und überschreibt `appsettings.json`.

## CI / Quality Gates

Jeder Push und jeder Pull Request gegen `main` durchläuft:

- `dotnet restore` / `build` / `test` (xUnit)
- Code Coverage via `coverlet.collector`
- Coverage-Report via ReportGenerator (Markdown-Summary in der
  Workflow-Übersicht, HTML-Report als Artifact)
- `dotnet format --verify-no-changes` (EditorConfig-Compliance)
- CodeQL Static Analysis

**Branch Protection** auf `main`:

- Direkte Pushes nicht möglich
- Merges nur via Pull Request mit grüner CI
- Squash-Merge als einzige Merge-Strategie
- Branch muss vor Merge auf dem aktuellen `main`-Stand sein

## Dependencies

[Dependabot](.github/dependabot.yml) prüft wöchentlich (montags) auf
Updates für:

- NuGet-Pakete (gruppiert nach EF Core, Test-Stack, Serilog)
- GitHub Actions

Patch- und Minor-Updates werden via
[`dependabot-auto-merge.yml`](.github/workflows/dependabot-auto-merge.yml)
automatisch gemergt, sobald die CI grün ist. Major-Bumps bleiben offen
und brauchen manuelle Sichtung der Release Notes.

## Branching & Commits

- `main` ist immer in einem deploybaren Zustand
- Feature-Branches: `<type>/phase-<X.Y>-<kurz-beschreibung>`
- Commits folgen [Conventional Commits](https://www.conventionalcommits.org/)
  mit optionaler FA-Referenz:
```
  <type>(<scope>): <kurze Beschreibung> [<FA-Referenz>]

  <optionaler Body mit mehr Details, max 72 Zeichen pro Zeile>
```
- Merges in `main` nur via Squash-PR

## Lizenz

[MIT](LICENSE)