# Conventions for agent sessions

Personal dashboard: C# / .NET 10, Blazor Server, EF Core + Postgres/PostGIS.
Read [docs/architecture.md](docs/architecture.md) before structural changes.
Per-feature file map: [docs/feature-modules.md](docs/feature-modules.md) ·
decisions and what was deliberately *not* built: [docs/history.md](docs/history.md).

## Display context (decided — do not "fix")

- The dashboard runs as a kiosk on an iPad in the LAN, with no internet
  access in the display context. LAN-only is a deliberate decision, not a
  gap — hence no auth, plain HTTP, and server-side asset proxies for map
  tiles and crests.
- Article/source links in reports and pages are therefore irrelevant: never
  optimize for link quality or use it as a success measure.

## Architecture invariants

- Layers `Web → Infrastructure → Domain`, enforced by project references;
  the Domain stays provider- and DB-free. Every data-pulling feature is the
  same vertical slice (state + typed client + background service + tile) —
  extend by copying an existing slice, not by inventing a new shape.
- External APIs never block or crash the render path: on failure call
  `MarkStale()`, the last snapshot stays visible, the tile degrades to
  "unavailable" while the rest keeps working.
- The `research` schema has one writer, and it is not this app: mapped only
  in the read-only `ResearchDbContext`, never in `DashboardDbContext` or its
  migrations; absent schema renders as an empty page, not an error. Guarded
  by `tests/Dashboard.Tests/Research`.
- Colors and sizes come only from the theme tokens in
  `src/Dashboard.Web/wwwroot/app.css`; color carries information, never
  decoration.

## Config & secrets

Three tiers — app-wide defaults in `appsettings.json` · secrets in user
secrets / env vars · private-but-not-secret values (location, stops, clubs)
in gitignored `appsettings.Local.json`. Never commit the latter two.

## Dev workflow

- Run locally: `docker compose up -d db`, then `dotnet run` in
  `src/Dashboard.Web`.
- Tests: `dotnet test` (xUnit; clients via `StubHttpMessageHandler` +
  `FakeClock` — no network in tests).
- Migrations: `dotnet ef database update`, applied manually (`dotnet-ef` is
  a local tool — `dotnet tool restore` from `src/`); migrations only roll
  forward.
- One squash PR per slice. Deployment to the Pi host:
  `./deploy.sh` — see [docs/deployment.md](docs/deployment.md).
