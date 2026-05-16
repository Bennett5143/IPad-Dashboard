# Projekt-Roadmap: iPad-Kiosk-Dashboard

> Ablaufplan und Checkliste für die Umsetzung des Projekts.
> Ergänzt den `projektplan-ipad-kiosk.md` (Tech-Stack) und die `anforderungen-moscow.md` (Features) um eine **zeitliche/strukturelle Reihenfolge**.
> Stand: Mai 2026

---

## Leitprinzipien

- **Lokal entwickeln, remote deployen:** Entwicklung erfolgt auf dem Mac gegen ein lokales `docker-compose`-Setup. Der Raspberry Pi 4 ist **Deployment-Ziel**, nicht Entwicklungsumgebung.
- **CI früh aufsetzen:** Test- und Build-Pipeline steht *vor* dem ersten Feature, nicht danach.
- **Iterativ pro Feature:** Jedes Feature wird vollständig (Domain → Infrastructure → UI → Tests) abgeschlossen, bevor das nächste startet.
- **Lernfokus vor Produktivität:** Bewusst überengineeren, um Berührungspunkte mit modernen Backend-/DevOps-Themen zu schaffen.

---

## Wichtige Vorab-Themen

### ARM64-Architektur
Der Raspberry Pi 4 läuft auf **ARM64**. Apple Silicon Macs ebenfalls – lokal ist das also kompatibel. Aber: GitHub-Actions-Runner sind standardmäßig **x86_64/AMD64**. Für Container-Builds in der CI deshalb zwingend `docker buildx` mit `--platform linux/arm64` (oder Multi-Arch-Build) verwenden, sonst startet das Image auf dem Pi nicht oder läuft langsam unter Emulation.

**Konsequenz für Free-Plan-Minuten:** Multi-Arch-ARM64-Builds via QEMU-Emulation sind ein Vielfaches langsamer als native x86_64-Builds (10–20 min statt 2–3 min). Image-Builds deshalb **nicht** an jeden Push hängen, sondern nur an `main`-Pushes oder Git-Tags (siehe Phase 5). Damit kommt man im privaten Repo mit den 2000 Free-Minuten/Monat komfortabel hin.

### Externe APIs – frühzeitig recherchieren
Vor der Implementierung der API-getriebenen Features klären:

- **Wetter:** OpenWeatherMap (kostenloser Tier reicht für Kiosk-Use-Case)
- **Fußball:** football-data.org oder api-football – prüfen, ob HSV (2. Bundesliga) und Real Madrid (La Liga) abgedeckt sind und welche Rate Limits gelten
- **HVV (Abfahrtsmonitor):** inoffizieller Endpoint `www.hvv.de/geofox/departureList` – **Recherche abgeschlossen**, Doku in `docs/hvv-api-notes.md` (Endpoint-Details, Body-Schema, semantische Fallen, rechtliche Einordnung). Polling konservativ: max. 1 Request/min pro Haltestelle.
- **Strava (Heatmap, Phase 7):** offizielle Strava-API v3, OAuth2-basiert. Rate-Limits aktuell 100 Req/15 min und 1000 Req/Tag pro App. Für inkrementelle Daily-Syncs unkritisch. Recherche und Doku in `docs/strava-api-notes.md` erfolgen erst zu Beginn von Phase 7.

Auswirkung auf die Architektur: Alle drei Kern-APIs (Wetter, Fußball, HVV) brauchen einen **Background Service** (`IHostedService`) plus **Caching** (`IMemoryCache`), damit nicht bei jedem Render ein API-Call ausgelöst wird. Bei der HVV-API zusätzlich besonders relevant: **Graceful Degradation**, weil ein inoffizieller Endpoint jederzeit ohne Vorwarnung wegbrechen kann. Strava in Phase 7 nutzt dasselbe Background-Service-Pattern, schreibt das Ergebnis aber persistent in die DB (PostGIS) statt in den Memory-Cache.

### Secrets-Management auf drei Ebenen
| Ebene | Mechanismus | Zweck |
|-------|-------------|-------|
| Lokal (Mac) | `dotnet user-secrets` | API-Keys, DB-Passwörter beim Entwickeln |
| Container (lokal & Pi) | `.env`-Datei, in `.gitignore` | Wird von `docker-compose` eingelesen |
| CI/CD | GitHub Secrets | Für Image-Build und Deployment |

API-Keys oder Passwörter dürfen **nie** committet werden.

---

## Phase 0 – Repo-Foundation

- [ ] GitHub-Repo anlegen – **privat** (2.000 Free-Actions-Minuten/Monat reichen, solange Image-Builds nicht an jeden Push gehängt werden; siehe ARM64-Hinweis oben). Bei Bedarf später auf öffentlich umschaltbar.
- [ ] `.gitignore` mit Vorlage „VisualStudio" + Ergänzungen für `.env`, `appsettings.Development.json`
- [ ] `.editorconfig` einrichten (einheitliche Formatierung)
- [ ] `README.md` mit kurzem Projektüberblick, Tech-Stack, Setup-Anleitung
- [ ] `LICENSE` setzen (optional, z.B. MIT)
- [ ] GitHub Issues / Project Board einrichten – auch solo nützlich, übt Ticket-getriebene Arbeit
- [ ] Branching-Konvention festlegen: `main` + Feature-Branches, Merges nur via PR (auch alleine – übt PR-Reviews mit sich selbst)

---

## Phase 1 – Lokale Dev-Umgebung

- [ ] Solution mit Projekt-Struktur anlegen, z.B.:
  - `Dashboard.Web` (Blazor Server)
  - `Dashboard.Domain` (Entitäten, Value Objects, Enums)
  - `Dashboard.Infrastructure` (DbContext, Repositories, externe API-Clients)
  - `Dashboard.Tests` (xUnit + bUnit + Testcontainers)
- [ ] Klassen aus dem Klassendiagramm übertragen: `HabitEntry`, `HabitKind` (Enum), `RunningDetails` (Value Object), `Quote`
- [ ] EF Core einbinden (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- [ ] `DbContext` konfigurieren, Connection-String aus User Secrets lesen
- [ ] `docker-compose.yml` mit Postgres-Service **lokal** auf dem Mac (Named Volume für Persistenz)
- [ ] Erste EF-Migration erstellen + ausführen
- [ ] Seed-Mechanismus für die 365 Zitate vorbereiten (idempotent: neu einspielbar ohne Duplikate)
- [ ] Logging-Setup mit Serilog (Konsole + Datei)
- [ ] Health-Check-Endpoint (`/health`) – wird später für Container-Health relevant

---

## Phase 2 – CI-Pipeline (jetzt, nicht später!)

- [ ] Workflow-Datei `.github/workflows/ci.yml` anlegen
- [ ] Steps: `dotnet restore` → `dotnet build` → `dotnet test` → `dotnet format --verify-no-changes`
- [ ] Trigger: Push auf jeden Branch + Pull Requests gegen `main`
- [ ] Branch-Protection auf `main`: Merges nur bei grüner Pipeline
- [ ] Optional: Code-Coverage-Reporting (Codecov oder als Workflow-Artifact)

**Begründung der Reihenfolge:** Wenn die Pipeline erst nach dem ersten Feature kommt, ist die Hürde, „doch noch" Tests zu schreiben, in der Praxis hoch. Andersherum zwingt eine grüne Pipeline ab Tag eins zu sauberer Arbeit.

---

## Phase 3 – Dashboard-Skelett

- [ ] Layout-Komponente mit Tile-Grid (am Wireframe orientiert)
- [ ] `Tile`-Basiskomponente (Container) – einzelne Kacheln erben davon oder nutzen sie als Wrapper
- [ ] Globales Error-Handling (Error-Boundary in Blazor, ggf. zusätzliche Middleware)
- [ ] Platzhalter-Komponenten für jede Kachel (Uhr, Wetter, Habits, Fußball, Zitat) – noch ohne Logik
- [ ] Routing/Navigation, falls nötig (z.B. Admin-Bereich vorbereiten – auch wenn Inhalt später kommt)

---

## Phase 4 – Features (iterativ, jedes mit Tests)

Jedes Feature endet mit: funktionsfähige Kachel + Unit-Tests + ggf. Integration-Tests + grüne Pipeline + Merge in `main`.

### 4.1 Uhrzeit & Datum
- Einstieg ohne DB, ohne API
- Lernfokus: Timer-Pattern in Blazor Server, SignalR-Updates, Time-Zone-Handling
- **Achtung Zeitzone:** Server läuft im Container (UTC?), Anzeige soll lokale Zeit zeigen → bewusst entscheiden

### 4.2 Zitate
- Erster echter DB-Zugriff
- Lernfrage: Repository-Pattern oder EF direkt im Service? Pro/Contra abwägen
- Deterministische Tagesauswahl: z.B. `quoteId = (dayOfYear % count) + 1` – dokumentieren, warum kein Random
- Seed der 365 Einträge per Migration oder eigener Seeder

### 4.3 Habit-Tracker
- Komplexester DB-Teil (Schreiben, Aggregation: Wochen-/Jahres-Counts)
- Lernfokus: EF-Aggregationen, optimistic concurrency, ggf. Caching
- UI: Touch-optimiertes Markieren, rückwirkende Eingabe (siehe FA-3.06)
- **Wichtig für Phase 7:** Im Habit-Bereich einen visuellen Anker (Icon/Button) vorsehen, der später die Heatmap-Route öffnet (FA-3.09). Aktuell führt der Anker nur auf eine Placeholder-Seite – die echte Implementierung folgt in Phase 7.

### 4.4 Wetter
- Erste externe API (z.B. OpenWeatherMap)
- Lernfokus:
  - `IHttpClientFactory` (richtiges HttpClient-Handling)
  - `IHostedService` für Background-Refresh
  - `IMemoryCache` für gecachte Antworten
  - Resilience mit **Polly** (Retry, Circuit Breaker)

### 4.5 Fußball
- Zweite externe API, gleiches Muster wie Wetter
- Zwei Datenquellen kombinieren (Real Madrid + HSV)
- Lernfokus: Wiederverwendung der Patterns aus 4.4, ggf. eigenes Abstraktionslevel

### 4.6 HVV-Abfahrtsmonitor
- Dritte externe API, dieselben Patterns wie 4.4/4.5 anwenden
- **Vorab:** API-Recherche bereits abgeschlossen, siehe `docs/hvv-api-notes.md` – dort Endpoint, Body-Schema, Sample-Responses und rechtliche Einordnung
- Lernfokus:
  - **DTO-Mapping als Anti-Corruption-Layer:** Wire-Format der externen API (z.B. `timeOffset` als Minuten-Integer) bleibt in der Infrastructure-Schicht, Domain arbeitet mit `DateTimeOffset`
  - **Nullable-Semantik bewusst nutzen:** `delay: null` ≠ `delay: 0` (siehe `hvv-api-notes.md`)
  - **Graceful Degradation:** Endpoint kann jederzeit wegbrechen – Polly-Circuit-Breaker zeigen lassen, was sie können
  - **Server-Zeit vs. Client-Zeit:** `timeOffset` relativ zur Response-`time` rechnen, nicht zu `DateTime.Now`
  - **Zeitzone:** HVV liefert CET/CEST, Container läuft i.d.R. UTC – `TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")` explizit einsetzen
- Sample-JSON aus der Recherche (Erfolgs- und Fehlerfall, mit/ohne Echtzeitdaten) in `Dashboard.Tests/TestData/` ablegen → Mapping-Tests laufen offline und deterministisch
- README-Hinweis aufnehmen: inoffizieller Endpoint, für ernsthafte Projekte offiziellen Geofox-Zugang beantragen (Vorlage in `hvv-api-notes.md`)

---

## Phase 5 – Containerisierung & Deployment

- [ ] `Dockerfile` mit Multi-Stage-Build (SDK-Stage zum Bauen, Runtime-Stage als finales Image)
- [ ] Production-`docker-compose.yml` für den Pi (eigene Datei, getrennt von Dev-Compose)
- [ ] Workflow `.github/workflows/docker-publish.yml`:
  - Trigger: Push auf `main` oder Git-Tags
  - Multi-Platform-Build mit `docker buildx`, Target `linux/arm64`
  - Push zu **ghcr.io** (GitHub Container Registry – gratis, gut integriert)
- [ ] Reverse Proxy auf dem Pi: **Caddy** (einfachste Option, automatisches HTTPS, minimale Config)
- [ ] Postgres auf dem Pi mit Named Volume aufsetzen (Daten persistent halten)
- [ ] Erstes manuelles Deployment: per SSH `docker compose pull && docker compose up -d`
- [ ] Optional später: SSH-Deploy-Action oder **Watchtower** für Auto-Updates

---

## Phase 6 – Kiosk-Hardening

- [ ] iPad: Web-App zum Homescreen hinzufügen
- [ ] PWA-Manifest hinterlegen für echten Vollbildmodus (`display: standalone`)
- [ ] Guided Access auf dem iPad aktivieren (sperrt das Tab/die App)
- [ ] SignalR-Reconnect-Verhalten testen: WLAN kurz aus, was passiert?
- [ ] Postgres-Backup einrichten: `pg_dump` als Cron-Job auf dem Pi
- [ ] **Dependabot** oder **Renovate** aktivieren – automatische Dependency-PRs, sehr lehrreich
- [ ] **Secret-Scanning** in den Repo-Settings aktivieren – warnt automatisch, falls API-Keys oder Tokens versehentlich committet werden
- [ ] Monitoring/Logging: zumindest Container-Logs zentral ansehbar (`docker compose logs`)

---

## Phase 7 – Lauf-Heatmap (optionale Erweiterung)

> Voraussetzung: Phasen 0–6 abgeschlossen, Dashboard läuft stabil auf dem Pi. Diese Phase ist bewusst *nach* dem Kiosk-Hardening eingeplant – es ist ein Erweiterungsfeature, kein MVP-Bestandteil.

**Ziel:** Visualisierung aller eigenen Lauf-Strecken aus Strava als geografische Heatmap auf einer separaten Route. Aufruf per Tap im Habit-Bereich des Dashboards (Anker bereits in Phase 4.3 platziert).

**Lernfokus dieser Phase:**
- OAuth2-Flow in C# (Authorization Code Flow, Refresh-Token-Lifecycle, sicherer Token-Storage)
- **PostGIS** als Geo-Extension für PostgreSQL – Geo-Datentypen, räumliche Indizes, einfache Spatial-Queries
- Client-Server-Aufteilung beim Karten-Rendering: GeoJSON vom Backend, Visualisierung im Browser mit Leaflet
- Inkrementelle Datensynchronisation mit "Since"-Pattern (nur Activities seit letztem Sync laden)

### 7.1 API-Recherche & Doku
- [ ] `docs/strava-api-notes.md` analog zu `hvv-api-notes.md` anlegen
- [ ] Strava-Developer-Account anlegen, App registrieren (`http://localhost`-Callback für lokale Tests, später auf Pi-Domain umstellen)
- [ ] Endpoints dokumentieren: `/oauth/token` (Authorization & Refresh), `/athlete/activities` (Liste), `/activities/{id}/streams` (GPS-Daten)
- [ ] Sample-Responses für Activity-Liste und Activity-Streams ablegen (Mapping-Tests offline)
- [ ] Rate-Limits klären und im Sync-Service hart berücksichtigen

### 7.2 PostGIS-Integration
- [ ] Postgres-Image im `docker-compose.yml` von `postgres:16-alpine` auf `postgis/postgis:16-3.4-alpine` (oder vergleichbar) umstellen – sowohl lokal als auch im Pi-Compose
- [ ] EF-Migration anlegen, die `CREATE EXTENSION IF NOT EXISTS postgis;` ausführt
- [ ] NuGet-Paket `NetTopologySuite` + `Npgsql.NetTopologySuite` einbinden
- [ ] DbContext konfigurieren: `opts.UseNpgsql(cs, o => o.UseNetTopologySuite())`
- [ ] Lernfrage: Welche Geo-Typen nutzen? Vorschlag: `LineString` für die GPS-Spur, `Point` für Start/End-Punkte, SRID 4326 (WGS84)

### 7.3 Domain-Modell & Persistenz
- [ ] Neue Entitäten in `Dashboard.Domain`:
  - `StravaActivity` (Id, StravaActivityId, StartedAt, DistanceMeters, MovingTimeSeconds, AverageHeartRate?, Track als `LineString`)
  - `StravaSyncState` (LastSyncedAt, LastActivityId, LastError?, LastErrorAt?) – Single-Row-Tabelle für Sync-Status
- [ ] EF-Migrationen schreiben
- [ ] Räumlicher Index auf `Track` (`CREATE INDEX ... USING GIST (track)`) – wird für spätere Filterungen relevant

### 7.4 OAuth-Setup
- [ ] Admin-Route `/heatmap/connect` mit Strava-Connect-Button (führt zur Strava-Authorize-URL)
- [ ] Callback-Endpoint, der den Authorization-Code gegen Access- und Refresh-Token tauscht
- [ ] Token-Storage in eigener Tabelle (verschlüsselt oder zumindest nicht im Klartext im Log)
- [ ] Refresh-Token-Logik: vor jedem API-Call prüfen, ob Access-Token noch gültig ist, sonst refreshen

### 7.5 Sync-Background-Service
- [ ] `StravaSyncService : BackgroundService` mit konfigurierbarem Intervall (z.B. alle 4 Stunden – Activities entstehen ja nicht im Minutentakt)
- [ ] Inkrementelle Logik: `/athlete/activities?after={timestamp}` mit Pagination
- [ ] Pro neuer Activity: Streams-Endpoint abrufen (`?keys=latlng,heartrate,velocity_smooth`) und LineString konstruieren
- [ ] Polly drumherum: Retry mit Exponential Backoff, Respekt vor `X-RateLimit-Usage`-Headern
- [ ] Sync-State nach jedem Lauf updaten – auch im Fehlerfall (Last-Error festhalten)

### 7.6 Heatmap-Route & UI
- [ ] Neue Blazor-Page `/heatmap` mit Voll-Viewport-Karte
- [ ] Tap im Habit-Bereich → Navigation auf `/heatmap`, Zurück-Button auf Dashboard
- [ ] Leaflet via JS-Interop oder existierender Blazor-Wrapper einbinden (Lernfrage: welcher Weg gibt mehr Kontrolle?)
- [ ] Backend-Endpoint `GET /api/heatmap/tracks?from=...&to=...` liefert GeoJSON-FeatureCollection
- [ ] Leaflet.heat-Plugin für die Frequenz-Layer; Activities als Polylines als zweite Layer (umschaltbar)
- [ ] Zeitraum-Filter im UI (letzte 4 Wochen / 12 Monate / alles)

### 7.7 Tests
- [ ] Mapping-Tests Strava-Stream-Response → `LineString` (mit gespeicherten Sample-JSONs, offline)
- [ ] Sync-Service-Tests gegen ein Fake-`HttpMessageHandler` (kein echtes Strava)
- [ ] Integration-Tests mit Testcontainers + PostGIS-Image: einmal eine Activity reinpacken, Heatmap-Endpoint queryen, GeoJSON validieren

### 7.8 Dokumentation & Pflege
- [ ] README-Abschnitt: wie macht man das einmalige OAuth-Setup als Eigentümer?
- [ ] Rate-Limit-Verhalten und Sync-Intervall ehrlich dokumentieren
- [ ] Hinweis: Strava-API-Keys gehören in GitHub Secrets bzw. `.env`, nie ins Repo

---

## GitHub Actions – Übersicht

Alle Actions liegen unter `.github/workflows/*.yml`. Empfohlene Aufteilung:

| Datei | Trigger | Inhalt | Phase |
|-------|---------|--------|-------|
| `ci.yml` | Jeder Push, jeder PR | Restore, Build, Test, Format-Check | 2 |
| `docker-publish.yml` | Push auf `main` oder Tag | Multi-Arch-Image-Build, Push zu ghcr.io | 5 |
| `deploy.yml` (optional) | Nach erfolgreichem Image-Push | SSH zum Pi, `docker compose pull && up -d` | 5+ |

**Warum Trennung?** Image-Builds dauern länger als reine .NET-Builds. Diese sollen *nicht* bei jedem Feature-Branch-Push laufen. Bei einem privaten Repo mit Free-Plan-Minuten (2000 min/Monat) ist diese Trennung sogar zwingend, damit ARM64-Emulationsbuilds nicht das Budget aufessen.

---

## Häufig vergessene Themen (Checkliste)

- [ ] **Time-Zone-Handling in Postgres:** `timestamptz` vs. `timestamp` bewusst wählen. Empfehlung: `timestamptz` als Default, alle Zeiten in UTC speichern, Anzeige in lokaler Zeit.
- [ ] **`DateOnly` / `TimeOnly`** wo passend einsetzen (siehe Klassendiagramm: `HabitEntry.Date` ist `DateOnly` – gut!). EF Core 8+ unterstützt das nativ.
- [ ] **Validation:** FluentValidation oder DataAnnotations – früh entscheiden und durchziehen.
- [ ] **Background Services** als sauberes Pattern für externe API-Polls (`BackgroundService`-Basisklasse).
- [ ] **Nullable-Semantik in DTOs/Domain:** `null` und `0` (oder leerer String) sind nicht dasselbe. Wenn das Fehlen eines Wertes bedeutungstragend ist (z.B. „keine Echtzeitdaten verfügbar" vs. „pünktlich"), nullable Typen verwenden und niemals reflexartig `?? 0` schreiben.
- [ ] **Idempotenter Seed** für die 365 Zitate: Mehrfachausführung darf keine Duplikate erzeugen.
- [ ] **Health-Check-Endpoint** für Container-Orchestrierung und Monitoring.
- [ ] **Strukturiertes Logging** von Anfang an (Serilog mit JSON-Output für spätere Auswertung).
- [ ] **Geo-Datentypen & SRID (für Phase 7):** Bei PostGIS immer explizit eine SRID setzen (4326 für „rohe" GPS-Koordinaten, 3857 wenn auf Map-Tiles gerechnet wird). Mixing zwischen SRIDs führt zu kryptischen Fehlern bei Spatial-Queries.

---

## Nächste mögliche Vertiefungen

Themen, die im Verlauf des Projekts eigene Ausarbeitungen verdienen:

1. **Solution-Struktur** – Domain/Infrastructure/Web-Aufteilung im Detail (Clean Architecture light)
2. **Background-Service-Pattern** für die externen APIs (Wetter, Fußball, HVV, Strava)
3. **Caching-Strategien** (Memory-Cache vs. später ggf. Redis)
4. **Resilience mit Polly** – Retry, Circuit Breaker, Timeout
5. **EF Core Aggregationen** für die Habit-Statistiken (Wochen-/Jahres-Counts)
6. **DTO-Mapping & Anti-Corruption-Layer** – wie verhindere ich, dass externe API-Eigenheiten in die Domain durchsickern (Beispiel HVV: `timeOffset`-Minuten → `DateTimeOffset`)
7. **OAuth2 in C#** – Authorization Code Flow, Token-Refresh-Lifecycle, sichere Token-Persistenz (relevant für Phase 7 / Strava)
8. **PostGIS & NetTopologySuite** – Geo-Typen in EF Core, räumliche Indizes (GIST), Spatial-Queries (für Phase 7)
9. **Karten-Rendering im Browser** – Leaflet via JS-Interop in Blazor, GeoJSON-Pipeline vom Backend