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

Five fixed sport habits, toggleable per day with weekly/yearly counters,
backfill, optional run details (owned entity) and EMOM gym tracking.
`Domain/Habits/HabitTrackingService` + `HabitsTile`; `CompleteAsync` is the
hook WHOOP auto-fill uses. `/habits` adds a GitHub-style year heatmap and
streaks (`HabitsHeatmapBuilder`, `HabitStreakCalculator`).

## Weather — OpenWeatherMap

Current conditions, tomorrow, hourly outlook, rain probability, sun/wind
extras. `Domain/Weather` (snapshot factory aggregates day/hour buckets),
`Infrastructure/Weather/OpenWeatherMapClient`, home block `EinkWeather` +
detail page `/weather`.

## Football — football-data.org

Tracked clubs (results, fixtures, standings), top-5 league tables, Champions
League league phase + knockout bracket derived purely from fixtures
(`KnockoutBracketBuilder`), and tournament windows (EM/WM). One API call per
competition per refresh with an inter-call delay for the free tier. `/football`
with reusable `StandingsTable`/`KnockoutBracketView`; crests come through the
offline `/crests` proxy. A `IFabrizioAlertSource` seam awaits the X/social
slice.

## Transit — HVV departures

Next departures per configured station (line, direction, real-time delay,
mode icon) from the unofficial geofox endpoint; conservative polling
(≥ 60 s/station). Semantics worth knowing: `delay` is nullable — `null` means
"no live data", not "on time" — and `timeOffset` counts from the server time
in the response. `Domain/Hvv` (`DepartureSelector`), `HvvDepartureClient`,
page `/hvv`. Stations are private config (`appsettings.Local.json`).

## Run heatmap & runs — Strava

OAuth2 sync of runs into PostGIS (incremental, rate-limit aware, stream
backfill). `/heatmap`: Leaflet with heat/pace/elevation/direction/heart-rate
layers, clickable routes, cluster filter. `/runs`: list + per-run SVG
profiles, year in review (`RunReviewCalculator`), best efforts, and route
clustering into "standard loops" in pure C# (`RouteClusterer`).
`Infrastructure/Strava` owns tokens, sync and stores.

## WHOOP — recovery, auto-fill, insights

OAuth2 (refresh rotates both tokens; https redirect required). Recovery rings
on the home, habit auto-fill (idempotent via processed-workout table,
`sport_name` → habit mapping, zone-2 vs. VO2max via HR zones), and the
`/whoop` insights hub reading persisted daily metrics/workouts: trends,
time-of-day effectiveness, sleep analysis, training load (ACWR), aerobic
fitness curve, recovery drivers. Windowed backfill fills history
(`WhoopBackfillPlanner`).

## Crypto — CoinGecko + alternative.me

Market watchlist (price, 24 h change, 7-day sparkline) and Fear & Greed
sentiment; both keyless. Market data is mandatory (failure → stale), sentiment
is best-effort (keeps last value). `Domain/Crypto`, `CoinGeckoClient` +
`FearGreedClient`, page `/crypto`.

## Calendar — ICS feed

Server-side fetch of one or more ICS URLs (Ical.Net), powering the home month
grid + day agenda. `Domain/Calendar` (`CalendarState`),
`Infrastructure/Calendar/IcsCalendarClient` + `CalendarRefreshService`,
component `EinkCalendar`. No persistence — pure live state.

## Status & observability

`/status` shows per-slice freshness (`ISliceStatusSource` implemented by
`ObservableState<T>`), OAuth token health, backfill progress, DB health, host
metrics (Linux only) and recent warnings/errors from the Serilog ring buffer.
The rail carries a de-colored status glyph (shape, not color, signals
up/down).
