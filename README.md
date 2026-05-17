# iPad-Dashboard

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
├── Dashboard.Domain           # Entities, Value Objects, Enums
├── Dashboard.Infrastructure   # DbContext, Seeder, externe API-Clients
└── Dashboard.Tests            # xUnit
```

NuGet-Versionen werden zentral über `Directory.Packages.props` im
Repo-Root verwaltet (Central Package Management).

## Status

🚧 In Entwicklung – Phase 1 (Lokale Dev-Umgebung) abgeschlossen.

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

### Secrets-Management

Sensible Daten (Connection-Strings, später API-Keys) gehören **nicht**
in `appsettings.json` und damit nicht ins Repo:

- **Lokal:** `dotnet user-secrets`
- **Container:** `.env`-Datei (in `.gitignore`)
- **CI/CD:** GitHub Secrets

## Branching

- `main` ist immer in einem deploybaren Zustand.
- Features laufen in Branches `<type>/phase-<X.Y>-<kurz-beschreibung>`.
- Commits laufen mit FA-Referenz:
```
  <type>(<scope>): <kurze Beschreibung> [<FA-Referenz>]

  <optionaler Body mit mehr Details, max 72 Zeichen pro Zeile>
```
- Merges in `main` nur via Pull Request.
