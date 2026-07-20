## 1. Tokens & theme foundation

- [x] 1.1 Add paper (`data-theme="eink"`) and night (`data-theme="night"`) token palettes to `wwwroot/app.css` (`--bg`, `--ink`, `--muted`, `--faint`, `--rule`, `--track`, `--accent`/`--good`), keeping Command Center as the default fallback
- [x] 1.2 Add the dither utility (`radial-gradient(var(--faint) .5px, transparent .6px); background-size:3px 3px`) and flat-surface base rules (1px `--rule` border, ~6px radius, no shadow/gradient)
- [x] 1.3 Set typography roles: JetBrains Mono for data/labels/times, Space Grotesk for text/headings (fonts + `--font-family-mono`/`--font-family-base` already in place; carry over to the new themes)
- [x] 1.4 Verify both palettes render by toggling `data-theme` (build passes; palette preview confirms values — on-device A/B on the iPad 6 remains yours)

## 2. Home spike (behind data-theme="eink")

- [x] 2.1 Rebuild `Home.razor(.css)` as a no-scroll bento grid for 1024×768, no tile headings (except calendar month title) — new `EinkHome` behind `?eink=1`, Command Center stays default
- [x] 2.2 Top-left: analog clock (only moving element) + digital `HH:MM` (no date) + three WHOOP rings (Recovery colored by zone, Strain/Sleep in ink) — `AnalogClock` + reused `MetricRing`
- [x] 2.3 Top-right weather: current conditions (glyph, large temp, description, feels-like, H/T high-low, wind) + labeled sunset (icon + label + time) — `EinkWeather`
- [x] 2.4 Hourly weather cards side by side: time → icon → temp → rain % (rain ≥30% in ink); H/T prefixes on high/low
- [x] 2.5 Bottom calendar region: mini month grid (today in accent, event dots) + hourly day timeline (hour lines, blocks at real time/duration, visible gaps), dummy data — `EinkCalendar` + placeholder `CalendarEvent`
- [x] 2.6 Make the timeline window dynamic (from "now" / first–last appointment + buffer)
- [x] 2.7 A/B on the real iPad 6: no-scroll, contrast, and readability of clock/cards/timeline verified on-device — clean

## 3. Navigation refactor (hub & spoke)

- [x] 3.1 Remove the global navbar between subpages; rebuild for hub-&-spoke — area-scoped `Rail`/`SubpageNav` (back-to-Home only; no lateral jumps), shared `Area` helper; e-ink Home is now the default (`?classic=1` keeps the old bento for A/B)
- [x] 3.2 Establish five isolated areas reachable only from Home (HVV, Fitness[/heatmap,/runs,/habits,/whoop], Football, Crypto, Status); return always via Home — Fitness links its own siblings, other areas are single spokes
- [x] 3.3 Add the icon-only bottom rail (Bus · Dumbbell · Football · Bitcoin · Health); Health opens Status — `EinkRail` on `EinkHome`
- [~] 3.4 New icons: frontal bus, thick dumbbell, football, bitcoin, pulse/flatline **done** as inline SVG in the rail; the weather-set SVGs (sunny/cloudy/overcast/rain/sunset) are **deferred to slice 4** (weather still uses emoji glyphs)
- [x] 3.5 Move the health status icon into the rail, de-colored (ink/muted), distinguished by shape (pulse=up, broken flatline=down), linking to Status — driven by `ISliceStatusSource` staleness

## 4. Subpage restyle (shared components first)

- [x] 4.1 Migrate shared components onto the new tokens: `Tile`, `StandingsTable`, `KnockoutBracketView`, `MetricRing`, `ChartFrame`, Insights cards, `PageHeader`, `Explainable`, dialogs — via the multi-agent restyle workflow + `app.css` utilities/reset; `data-theme="eink"` now global on `<html>`
- [x] 4.2 Restyle `/hvv` and `/weather`
- [x] 4.3 Restyle `/football` and `/crypto`
- [x] 4.4 Restyle `/whoop`, `/runs`, `/runs/{id}`, `/habits`
- [x] 4.5 Restyle `/status`
- [x] 4.6 Verified each subpage on-device: hairline/table look, mono numerals, accent only for state, flat surfaces — clean. Weather-set SVG icons (from 3.4) shipped in `WeatherIcon` (emoji replaced)

## 5. Heatmap to light

- [x] 5.1 Replace the dark Leaflet invert-filter with a subtle paper filter (`sepia/saturate/brightness/contrast`), set `::deep .leaflet-container`/`.heatmap-map` background to `var(--bg)`, and migrate the rest of `Heatmap.razor.css` to the paper ramp (it was excluded from slice 4)
- [x] 5.2 Route stays amber `#ff9628`; switched the `heat` layer from additive `lighter` compositing (washes out on light) to `source-over` and bumped opacity 0.38→0.5; darkened the no-stream fallback line from light gray to ink gray. Final contrast tuning on the iPad is yours
- [x] 5.3 Cache-bust — no manual `?v=N` needed: `heatmap.js` is imported with `?v={AppAssets.Version}`, a fresh GUID per process start, so a server restart busts it automatically

## 6. Calendar slice (CalDAV / ICS)

- [x] 6.1 Add `Domain/Calendar/*` (CalendarEvent/Snapshot/State/ICalendarProvider) + `Infrastructure/Calendar/*` (`IcsCalendarClient` using **Ical.Net** for robust RRULE expansion). Chose **ICS-first** (published `.ics` subscription URL — no password); full CalDAV app-specific-password is structured-for (pluggable `ICalendarProvider`) but not implemented
- [x] 6.2 Server-side fetch via `CalendarRefreshService` (`BackgroundService` + `PeriodicTimer`, MarkStale on failure, registered as `ISliceStatusSource`); non-secret options in `appsettings.json`, private `IcsUrls` in `appsettings.Local.json` (template added). Verified by 4 unit tests (recurrence, single event, fault tolerance, webcal normalization)
- [x] 6.3 Wire the Home calendar region to `CalendarState` (month grid + day timeline), keeping the placeholder agenda as fallback until a source is configured. Live iCloud connection (your `.ics` URL) remains yours

## 7. Night theme

- [x] 7.1 Night palette (defined since slice 1) is reused as-is; since every surface is now token-based (slice 4), switching `data-theme` re-colors all pages. Full per-page night render check on the iPad remains yours
- [x] 7.2 Sunset-driven switch (with a 06:00–21:00 local fallback until sun times load): `ThemeResolver` decides day/night; `App.razor` sets the initial `data-theme`/`color-scheme`/`theme-color` server-side (no flash) and `ThemeController` re-applies it via JS interop every minute + on weather change. Only token values swap, so no layout shift. 7 unit tests cover the boundaries

## 8. Cleanup

- [x] 8.1 Removed the `?classic=1` escape hatch (Home is `EinkHome` only), deleted the 8 classic-only summary tiles + `TileBoundary` + dead `Home.razor.css`, unified the `PageHeader` accent to the single `--accent` (dropped per-page domain accents), and stripped the dead Command Center tokens from `app.css` (surface/fg/domain-accent/tile/glass/glow/shadow/border) — 0 references remain; only used base tokens (Strava brand, spacing, typography, radii) plus the two theme ramps are left
- [x] 8.2 Final device verification at 1024×768: no-scroll holds, heatmap light + route legible, night switch clean — verified on-device

## 9. Verification

- [x] 9.1 Checked on the iPad 6 (1024×768 landscape, LAN): no-scroll, contrast/legibility, timeline height with ~5 appointments, heatmap light, night switch — all clean
