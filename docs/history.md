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
