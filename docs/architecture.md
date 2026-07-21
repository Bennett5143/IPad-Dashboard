---
tags:
  - dev/doc
---

# Architecture

How the app is layered and the patterns every feature follows. What each module
does: [feature-modules.md](feature-modules.md) · why things are the way they
are: [history.md](history.md).

## Layers

`Web → Infrastructure → Domain`, enforced by project references:

```
src/
├── Dashboard.Web             # Blazor Server: UI, Program.cs (DI wiring, OAuth endpoints)
├── Dashboard.Domain          # entities, value objects, pure per-feature logic, ports (interfaces)
└── Dashboard.Infrastructure  # DbContext + migrations, API clients, background services, stores
tests/
└── Dashboard.Tests           # xUnit: domain logic + clients via stub HTTP handler + FakeClock
```

The Domain is provider- and DB-free (no EF, no HTTP, no NetTopologySuite) and
defines the ports (`IWeatherProvider`, `IRunRepository`, …) that Infrastructure
implements. The Web project's `GlobalUsings` only import `Dashboard.Domain.*`,
so UI code sees domain types only; concrete infrastructure appears solely in
`Program.cs`.

## The vertical slice pattern (external API → UI)

Every data-pulling feature (weather, football, transit, WHOOP, crypto,
calendar) is built the same way:

1. **Domain**: records + port + a singleton `…State : ObservableState<TSnapshot>`.
   The state holds the latest snapshot in memory, can `MarkStale()`, and raises
   `Changed` so Blazor re-renders — this is why it exists instead of
   `IMemoryCache`.
2. **Infrastructure**: a typed `HttpClient` client mapping DTOs → domain, plus a
   `BackgroundService` polling on a `PeriodicTimer`. On failure it calls
   `MarkStale()`; the last snapshot stays visible ("graceful degradation" —
   a dead API never crashes a tile).
3. **Web**: components subscribe to `Changed` and format via pure `*Formatter`
   classes.
4. **Tests**: domain logic directly; clients through a `StubHttpMessageHandler`
   and `FakeClock` (no network).

Strava and WHOOP add OAuth2 (server-side token stores, CSRF-protected
callbacks) and persist to the DB instead of / in addition to in-memory state.

## Analytics pattern

All insights (training load, sleep analysis, route clustering, streaks, …)
split into a **pure domain calculator** (simple values in, result records out,
fully unit-tested) and a thin **web builder** that formats for display. Pages
read from the DB, filled by the background services — a page reload never costs
API budget. Every heuristic reports its sample size and stays silent below a
minimum (honesty over impressiveness).

## UI

- **Navigation is hub-and-spoke**: `Home.razor` (`/`, `KioskLayout`) links out
  to detail pages (`/weather`, `/hvv`, `/football`, `/crypto`, `/whoop`,
  `/heatmap`, `/runs`, `/habits`, `/status`) which use `DetailLayout` with a
  shared `Rail`.
- **Home is an e-ink-style bento** (no-scroll, tuned for 1024×768): analog
  clock, WHOOP rings, weather with hourly cards, month grid + day agenda from
  the calendar feed, and a rail with status glyph.
- **Paper theme**: two palettes (`data-theme="eink"` day / `"night"`) over one
  token set in `wwwroot/app.css` — the single source for all colors/sizes.
  A tiny plain-JS snippet in `App.razor` flips the theme on a 20:00–08:00
  wall-clock schedule (`ThemeResolver` picks the server-side initial value).
  Color is information, never decoration: one accent plus state colors on data
  points.
- Page logic lives in **testable builders** (`HeatmapPayloadBuilder`,
  `WhoopInsightsBuilder`, …), not in Razor markup. Charts are inline SVG
  (`Sparkline`, `Scatter`); non-obvious metrics open an explanation popup
  (`Explainable` + `MetricCatalog`).
- Each tile sits in an `ErrorBoundary` (`TileBoundary`); a `ReconnectModal`
  covers SignalR drops for unattended displays.

## Operations

- **Database**: PostgreSQL 16 + PostGIS (run tracks as
  `geometry(LineString, 4326)`), via `docker-compose`. The image is
  multi-arch (`imresamu/postgis`) so it runs on x64 and ARM64 alike.
- **Migrations**: ordered chain in `Infrastructure/Migrations`, applied
  manually with `dotnet ef database update` (no auto-migrate on start).
  `dotnet-ef` is a local tool — `dotnet tool restore` from `src/`.
- **Configuration tiers**: app-wide defaults in `appsettings.json` · secrets in
  user secrets / env vars · private-but-not-secret values (locations, stops,
  clubs, watchlist) in gitignored `appsettings.Local.json`, loaded explicitly
  in `Program.cs` regardless of environment.
- **Logging & health**: Serilog (console + rolling JSON files + in-memory ring
  buffer surfaced on `/status`) · `GET /health/live` and `/health/ready`.
- **Asset proxies**: the display client needs no internet. Map tiles go through
  `/tiles/{z}/{x}/{y}.png` and club crests/flags through `/crests?u=<url>`
  (host allowlist against SSRF), both with on-disk caches.
