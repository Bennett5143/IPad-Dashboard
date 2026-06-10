# iPad-Dashboard

[![CI](https://github.com/Bennett5143/IPad-Dashboard/actions/workflows/ci.yml/badge.svg)](https://github.com/Bennett5143/IPad-Dashboard/actions/workflows/ci.yml)

Selbstgehostetes Dashboard für ein iPad 6th Gen, das auf einem
Raspberry Pi 4 läuft. Anzeigt: Uhrzeit, Wetter, Habit-Tracker,
Fußball-Ergebnisse, HVV-Abfahrten, Zitat des Tages.

## Tech-Stack

- C# / .NET 10 mit Blazor Server
- Entity Framework Core (Npgsql Provider)
- PostgreSQL 16
- Serilog (Konsole + JSON-Dateien)
- Docker / docker-compose
- Hosting auf Raspberry Pi 4 (ARM64)

## Projektstruktur

```
src/
├── Dashboard.Web              # Blazor Server, Entrypoint, UI
│   ├── Components/
│   │   ├── Layout/            # KioskLayout (Dashboard), MainLayout (Admin)
│   │   ├── Pages/             # Home.razor (/), Admin/Quotes.razor
│   │   ├── Tile.razor         # Container-Komponente mit Header/Body-Slot
│   │   ├── TileBoundary.razor # ErrorBoundary-Wrapper pro Tile
│   │   └── Tiles/             # Konkrete Tile-Implementierungen
│   └── wwwroot/app.css        # Design-Tokens (CSS Custom Properties)
├── Dashboard.Domain           # Entities, Value Objects, Enums
└── Dashboard.Infrastructure   # DbContext, Seeder, externe API-Clients
tests/
└── Dashboard.Tests            # xUnit
```

Cross-cutting MSBuild-Properties werden zentral via `Directory.Build.props`
verwaltet, NuGet-Versionen via `Directory.Packages.props` (Central Package
Management). Beide liegen im Repo-Root.

## UI-Architektur

Das Dashboard nutzt zwei Layouts: `KioskLayout` für die Dashboard-Page (`/`)
– Vollbild ohne Navigation, optimiert für den Kiosk-Modus – und `MainLayout`
für den Admin-Bereich (`/admin/*`) mit Sidebar und Top-Row.

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

🚧 In Entwicklung
- ✅ Phase 1: Lokale Dev-Umgebung (DB, EF Core, Docker, Seeding)
- ✅ Phase 2: CI-Pipeline, Code-Coverage, Branch Protection, Dependabot
- ✅ Phase 3: Dashboard-Skelett (Layouts, Tile-Komposition, Error-Isolation, Routing)
- 🚧 Phase 4: Features
  - ✅ Uhrzeit & Datum, Zitat des Tages, Wetter (OpenWeatherMap), Fußball (football-data.org)
  - ⬜ Habit-Tracker, HVV-Abfahrtsmonitor

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

### Secrets-Management

Sensible Daten (Connection-Strings, später API-Keys) gehören **nicht**
in `appsettings.json` und damit nicht ins Repo:

- **Lokal:** `dotnet user-secrets`
- **Container:** `.env`-Datei (in `.gitignore`)
- **CI/CD:** GitHub Secrets

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