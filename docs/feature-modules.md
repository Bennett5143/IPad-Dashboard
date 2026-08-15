---
tags:
  - dev/doc
---

# Feature modules

Per feature: what it does and where the key files live. The shared patterns
(vertical slice, analytics, theming) are in
[architecture.md](architecture.md).

## Clock & daily quote

Real-time clock (`Europe/Berlin`) via `IClock`/`SystemClock` — the testable
time source used everywhere. Daily quote maps the date deterministically onto
a pool of 365 DB-seeded entries (`Domain/Quotes`, idempotent `DbSeeder`).

## Habit tracker

Three sport habits — strength, zone-2 run, VO2max intervals — toggleable per
day with weekly/yearly counters, backfill, optional run details (owned entity)
and EMOM gym tracking. `HabitCatalog` is the one list every view and counter
reads; jump rope and stretching left the tracker but stay in `HabitKind`,
because the value is persisted as a string and old rows must remain readable.
`Domain/Habits/HabitTrackingService` + `HabitsTile`; `CompleteAsync` is the
hook WHOOP auto-fill uses. `/habits` adds a GitHub-style year heatmap and
streaks (`HabitsHeatmapBuilder`, `HabitStreakCalculator`).

## Weather — OpenWeatherMap

Current conditions, tomorrow, hourly outlook, rain probability, sun/wind
extras. `Domain/Weather` (snapshot factory aggregates day/hour buckets),
`Infrastructure/Weather/OpenWeatherMapClient`, home block `EinkWeather` +
detail page `/weather`.

## Football — football-data.org

Six tracked clubs (results, fixtures, standings), top-5 league tables,
Champions League league phase + knockout bracket derived purely from fixtures
(`KnockoutBracketBuilder`), and tournament windows (EM/WM). One API call per
competition per refresh — twelve in total, spaced ten seconds apart, which is
what the free tier tolerates (eight was measured to hit a 429). `/football`
shows one full-width table; form and next fixture of every club live in the
week calendar on the home page instead. Crests come through the offline
`/crests` proxy. A `IFabrizioAlertSource` seam awaits the X/social slice.

## Transit — HVV departures

Next departures per configured station (line, direction, real-time delay,
mode icon) from the unofficial geofox endpoint; conservative polling
(≥ 60 s/station). Semantics worth knowing: `delay` is nullable — `null` means
"no live data", not "on time" — and `timeOffset` counts from the server time
in the response. `Domain/Hvv` (`DepartureSelector`), `HvvDepartureClient`,
page `/hvv`. Stations are private config (`appsettings.Local.json`).

## Run heatmap & runs — Strava

OAuth2 sync of runs into PostGIS (incremental, rate-limit aware, stream
backfill). `/runs`: list + per-run SVG profiles, year in review
(`RunReviewCalculator`), best efforts, and the **places** — runs grouped by the
proximity of their start point (`RunPlaceMatcher`, threshold 2 km), named in
the UI. Route *shape* deliberately plays no part: the question is where I ran,
not which route I took.

`/heatmap` shows one place at a time in a view that cannot be dragged or
zoomed, framed on that place's extent. That is what makes the tile set finite:
`PlaceTileWarmupService` preloads exactly those rectangles (`PlaceMapView`
computes the fitting zoom plus one reserve level), so the offline iPad never
meets a grey tile. Layers (heat/pace/elevation/direction/heart rate) and the
tap-a-route popup are unchanged. `Infrastructure/Strava` owns tokens, sync and
stores.

## WHOOP — recovery, auto-fill, insights

OAuth2 (refresh rotates both tokens; https redirect required). Recovery rings
on the home, habit auto-fill (idempotent via processed-workout table,
`sport_name` → habit mapping, zone-2 vs. VO2max via HR zones), and the
`/whoop` insights hub reading persisted daily metrics/workouts: trends,
time-of-day effectiveness, sleep analysis, training load (ACWR), aerobic
fitness curve, recovery drivers. Windowed backfill fills history
(`WhoopBackfillPlanner`).

## Crypto — CoinGecko + alternative.me

Market watchlist of eleven coins (price, 24 h change, 7-day sparkline), Fear &
Greed sentiment, and a daily change series for the week calendar's badge; all
keyless. Market data is mandatory (failure → stale), sentiment and the daily
series are best-effort (they keep their last value).

`/crypto` lists every configured coin and expands one row at a time into a
large seven-day chart; `/crypto/{coinId}` adds hour/day/week/month/year.
Those histories are fetched **on demand** and cached per coin and range
(`CoinHistoryClient`) — eleven coins times five ranges in the background would
pull permanently on a free, hard-throttled source. `Domain/Crypto`,
`CoinGeckoClient` + `FearGreedClient`.

## Week calendar — football and price development

The full-width region at the bottom of the home page: seven day columns Mon–Sun
(Berlin), each with that day's fixtures of the tracked clubs and a badge
carrying Bitcoin's change for that day. A Champions League matchday is
condensed into a single entry — the clubs involved do not appear separately.
Future days carry no badge.

`FootballWeekBuilder` builds the week from the football snapshot,
`EinkWeek` renders it, and the daily series comes from the crypto snapshot
(`CoinGeckoClient` as `ICryptoHistoryProvider`, best-effort like the market
sentiment). There is no appointment integration: the former ICS/Apple calendar
slice was removed with this region.

## Research pages — football news & market report

Two pages that display rows written by a **separate tool**, not by this app:
football news (`/football/news`) and a market report (`/crypto/market`).

The rows live in a Postgres schema named `research`, in the same database this
app uses. That schema has exactly one writer, and it is not this application:
another program creates it, versions it with its own migrations, and fills it.
Here it is read and rendered, nothing else.

Two consequences visible in the code:

- `ResearchDbContext` (`Infrastructure/Research`) is a second, read-only
  context: `NoTracking`, `SaveChanges` throws, and every entity is mapped with
  `ExcludeFromMigrations()`. `DashboardDbContext` — the one that owns the
  migration history — is explicitly kept from scanning those configurations, so
  a generated migration can never create, alter or drop those tables.
- A missing schema is an empty page, not an error: the repository catches the
  Postgres "undefined schema/table" states and returns nothing, because the
  writing tool may simply never have run against a given database.

Both pages show the grades the writer recorded (`confirmed | reported | rumour`
for news, `evidenced | plausible | unclear` for market explanations) unchanged,
and never add a forecast or a recommendation of their own.
`Domain/Research`, `Infrastructure/Research`, pages `FootballNews.razor` and
`Market.razor`; guarded by `tests/Dashboard.Tests/Research`.

## Status & observability

`/status` shows per-slice freshness (`ISliceStatusSource` implemented by
`ObservableState<T>`), OAuth token health, backfill progress, DB health, host
metrics (Linux only) and recent warnings/errors from the Serilog ring buffer.
The rail carries a de-colored status glyph (shape, not color, signals
up/down).
