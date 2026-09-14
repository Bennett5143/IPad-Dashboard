---
tags:
  - dev/doc
  - dev/changelog
---

# History & guiding decisions

What was built when, and the decisions that hold across features. Patterns in
detail: [architecture.md](architecture.md).

## Guiding decisions

- **L1 — Observable singleton state instead of `IMemoryCache`.** Blazor Server
  needs a change event to re-render; every polling slice derives from
  `ObservableState<TSnapshot>`.
- **L2 — One vertical slice per feature**: Domain (records + port + state) →
  Infrastructure (typed client + background service) → Web (tile + formatter)
  → tests.
- **L3 — Graceful degradation everywhere.** Failed fetch → `MarkStale()`, last
  data stays, tile shows "unavailable". External APIs never block the render
  path.
- **L4 — Analytics = pure domain calculator + thin web builder**, reading from
  the DB, not the live API.
- **L5 — Honest heuristics**: every insight reports sample size; below a
  minimum it says nothing.
- **L6 — Route clustering in pure C# instead of PostGIS SQL** (offline
  testable, keeps the domain DB-free); `ST_HausdorffDistance` remains a
  documented optimization path.
- **L7 — The display client is fully offline (LAN only).** No browser call
  ever leaves the LAN — not even `<img src>`; Leaflet is self-hosted, tiles
  and crests go through server-side proxies.
- **L8 — Config tiers**: secrets → user secrets; private-but-not-secret →
  gitignored `appsettings.Local.json`; app-wide → `appsettings.json`.
- **L9 — One theme token source** (`wwwroot/app.css`); colors/sizes are never
  hardcoded, so a re-theme is a token swap.
- **L10 — Color is information, never decoration** (e-ink redesign): one
  accent, state colors only on data points, structure from hairlines and
  typography.

- **L11 — Documentation cites the code it describes.** The architecture
  diagram's specification names the files and lines it is built on and records
  the commit they were read at; CI re-validates those citations against the
  current commit, so a moved or deleted file fails a check instead of leaving a
  diagram that claims evidence it no longer has. What no check can catch — the
  architecture changing while the cited files stay put — is stated in the docs
  rather than implied to be covered.

- **L12 — The SDK version is pinned in three places or in none.** Restores run
  with `--locked-mode`, but one entry in the lock files is not declared by this
  repository at all: the Web SDK injects
  `Microsoft.AspNetCore.App.Internal.Assets`, whose version comes from whichever
  SDK performs the restore. That couples three things that look independent —
  `ci.yml`'s `setup-dotnet` version, the Dockerfile's SDK base-image digest, and
  the four `packages.lock.json` files. Moving one alone breaks the restore with
  `NU1004`, which happened twice in one day: first when the runner picked up a
  newer SDK under a floating `10.0.x`, then when dependabot bumped the base-image
  digest on its own. Both pins now name what they are coupled to. A version bump
  means regenerating the lock files with the new SDK and moving both pins in the
  same change.

**Deliberately not built**: weather×run correlation (no historical weather
data), Apple Health (no cloud API; WHOOP doesn't pass HealthKit through),
news ticker, speculative HVV cancellation flag (unverifiable on the
unofficial endpoint).

## Changelog

One squash PR per slice.

- **Phases 0–3 · Foundation** — repo, local dev environment, CI (build, test,
  coverage, format gate, CodeQL, Dependabot), dashboard skeleton.
- **Phase 4 · Core tiles** — clock/date, daily quote (deterministic, 365 DB
  entries), habit tracker (#53), weather via OpenWeatherMap (#49), football
  via football-data.org (#50), HVV departures via the unofficial geofox
  endpoint (#51, #55, #56). First visual identity: dark "liquid glass" (#63),
  which introduced the generic `ObservableState<T>`.
- **Phase 7 · Run heatmap (Strava)** — OAuth2 + PostGIS + Leaflet (#52);
  pace/elevation/direction/heart-rate layers from per-point streams (#60–#62).
- **Phase 8 · WHOOP** — recovery tile (#57), idempotent habit auto-fill (#58),
  insights page `/whoop` (#59). The API has no GPS, so the heatmap stays
  Strava's job.
- **Phase 9 · Data foundation** — WHOOP daily metrics and workouts persisted
  with windowed backfill (#64–#66); Strava activity details + full re-sync
  (#67).
- **Phase 10 · Analytics engine** — time-of-day effectiveness (#69), sleep
  analysis (#70), training load ACWR (#71), aerobic fitness curve (#72),
  recovery drivers (#73) — all on `/whoop`.
- **Phase 11 · Runs** — clickable heatmap (#74), `/runs` list + detail with
  SVG profiles (#75), year in review incl. Eddington number (#76), route
  clustering into "standard loops" (#77, see L6), best efforts (#78).
- **Phase 12 · Habits analytics** — year heatmap + weekly bars (#79), streaks
  (#80) on `/habits`.
- **Phase 13 · Observability** — `/status` page + header indicator (#68),
  Serilog ring buffer (#81).
- **Phase 14 · Quick wins + explainable metrics** — league table modal (#82),
  weather extras (#84), cross-navigation (#85), week calendar (#86);
  `Explainable` popups + `MetricCatalog` + labeled axes (#88).
- **Phase 15 · Modular redesign & new sources** — summary home + detail pages
  `/weather`, `/hvv` (#93–#97, forest-green re-theme), football expansion:
  top-5 tables, Champions League bracket, tournaments, `/football`
  (#100, #101), crypto watchlist + `/crypto` (#102), crest proxy + table
  filter (#103), bracket ordering fix (#104), log-forging fix (#105), public
  README (#106). Still open from this phase: X/social client, Fabrizio alert,
  MCO feed, LLM insights.
- **e-ink redesign (July 2026)** — planned as OpenSpec change `eink-redesign`
  (archived in #125). A short-lived "command center" home (#107) was
  superseded by the calm paper direction: e-ink day/night token themes (#109),
  home spike (#110), hub-and-spoke navigation with the e-ink bento home as
  default (#111), all subpages restyled to paper (#112), light paper heatmap
  (#113), server-side ICS calendar feeding the home agenda (#114),
  sunset-driven theme switching (#115) simplified to a plain-JS 20:00–08:00
  schedule (#117), command-center cleanup (#116), CoinGecko User-Agent fix
  (#118), home polish — no dither, SVG weather icons (#119).
- **First hosted deployment (Aug 2026)** — multi-stage Dockerfile + compose
  `app` service with opt-in startup migrations (#127), LAN endpoint hardening:
  loopback-only DB port, gated `/tiles/warm`, sanitized `/health/ready` (#128),
  YAGNI cleanup of dead UI components and the speculative Fabrizio-alert port
  (#129), deployment guide (see [deployment.md](deployment.md)).
- **Reading a foreign schema (Aug 2026)** — football news and a market report
  produced by a separate tool were displayed from a `research` schema this app
  read but never wrote or migrated. Second read-only `DbContext`, migration
  exclusion enforced by tests, empty state when the schema is absent. Retired a
  month later, when the writing tool moved off this host — see *The research
  pages are retired* below.
- **Content over layout (Aug 2026)** — OpenSpec change `dashboard-refinements`:
  the pages kept their look and changed what stands on them. The home calendar
  became a week over football and price development, and the ICS/Apple calendar
  slice was removed with it (#143); departures gained exclusion filters and show
  six per station (#144); the football page dropped its club column and the news
  pages became a swipeable deck without links (#145, #146); crypto grew to
  eleven coins with expandable rows and a detail page per coin (#147); the
  weather trend got labelled axes and twelve hours (#148); the habit tracker
  narrowed to three habits (#149); and runs are now grouped by **place** rather
  than by route shape, with a heatmap that shows one place in a view that cannot
  move so its tiles can be preloaded (#150, #151).

  Three things the implementation corrected about the plan. The duplicated
  league picker came from the configuration binder appending code defaults to
  configured values (#142) — it cost no API calls, as the plan had claimed.
  Twelve football calls per refresh do not fit through eight-second spacing;
  ten seconds does. And dropping a habit took a WHOOP training category with it,
  because the analysis derived its category from the habit mapper — sport
  classification is now its own concern.

- **The architecture gets a picture (Sep 2026)** — no application code changed.
  `docs/archify/architecture.archify.json` describes the layering this repo
  already explained in prose, and the README shows it as a light/dark still
  (#222, #223, #224). The specification cites eight files and lines; the
  `Architecture diagram` workflow re-checks them on every change under `src/`
  (see L11). Exporting the stills is a browser action and is not automated —
  the workflow fails instead when the specification is newer than the images,
  which makes forgetting the re-export impossible without granting CI write
  access to the repository.

  One correction the diagram forced: a first draft labelled the edge from
  `Dashboard.Web` to `Dashboard.Infrastructure` "injected services", which
  contradicts this repo's own architecture notes — the Web project sees domain
  types and names implementations only in `Program.cs`. A second draft modelled
  `ObservableState<T>` and the domain ports as their own nodes; more accurate,
  harder to read, and dropped in favour of fixing three edge labels.

  Alongside it, the local agent tooling stays out of the repo (#220, #221): the
  rtk `PreToolUse` hook, graphify's output and its skill symlink are all
  per-machine decisions.

- **The research pages are retired (Sep 2026)** — OpenSpec change
  `retire-research-features`. The tool that wrote the `research` schema moved off
  this host, so `/football/news` and `/crypto/market` lost their only source. Both
  pages are gone, and nothing replaces them: research results are read in a notes
  vault outside this application — no second connection, no file import, no
  read-only archive of the old rows in the dashboard.

  Removed with them: the read-only `ResearchDbContext` and its repository, the
  `NewsDeck` paging view (a reference search found no consumer outside those two
  pages), the grade-badge styles, and the boundary tests that kept the migrating
  context out of the foreign schema — a guard whose subject no longer exists
  protects nothing and only makes the next reader look for a schema that is not
  there. `/crypto` lost its tab row entirely: one entry pointing at the page
  already open is a control that cannot do anything.

  The schema itself was dropped by hand (`DROP SCHEMA research CASCADE`) on
  2026-09-14, after a verified `pg_dump` — deliberately not an EF migration.
  Writing it as one would have required teaching `DashboardDbContext` about tables
  it had been kept ignorant of on purpose, and the numbers show why that mattered:
  the schema held **sixteen** tables and this application had ever mapped **four**.
  A migration would have dropped those four and orphaned twelve.

- **The build pipeline learns what it is pinned to (Sep 2026)** — two `NU1004`
  failures in one day, same error code, three different doors, all of them the
  coupling now recorded as L12.

  `setup-dotnet` asked for `10.0.x` while every job restored with
  `--locked-mode`; when the runner picked up SDK 10.0.401 the implicit
  `Microsoft.AspNetCore.App.Internal.Assets` moved to 10.0.12 against lock files
  recording 10.0.11 and four jobs went down (#229). Re-running the last green run
  on the unchanged `dev` reproduced it exactly, which is what separated a runner
  change from a code regression. Then dependabot bumped the Dockerfile's SDK
  base-image digest on its own and broke the containerized publish the same way
  (#243).

  A third variant came from the lock files themselves: dependabot regenerates the
  lock file of the project that *declares* a package, and this solution has four.
  `tests/Dashboard.Tests` references `Dashboard.Infrastructure`, so its lock file
  kept the old transitive set and locked restore refused it (#233, #234).
  `dotnet restore --force-evaluate` over the solution is the step a single-project
  regeneration misses.

  The reason none of this was caught before a deploy: `docker.yml` only ran on
  `push`, so the image build was never a check on the pull request that broke it —
  one promotion PR stood green on eleven checks with an image that did not build.
  Pull requests now build the image too, without logging in, pushing, or
  attesting, and on the runner's architecture only.
