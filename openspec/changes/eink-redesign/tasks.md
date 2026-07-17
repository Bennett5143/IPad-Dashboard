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
- [ ] 2.7 A/B on the real iPad 6: verify no-scroll, contrast, and readability of clock/cards/timeline (build passes + 1024×768 mock verified; on-device A/B via `/?eink=1` remains yours — DB/Docker not available locally)

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
- [ ] 4.6 Verify each subpage: hairline/table look, mono numerals, accent only for state, flat surfaces (build green + independent grep = 0 Command-Center tokens/dark hex left; on-device visual check across pages remains yours). Also still open from 3.4: weather-set SVG icons (weather uses emoji)

## 5. Heatmap to light

- [ ] 5.1 Remove the dark Leaflet filter (or replace with a subtle paper filter) in `Heatmap.razor.css`/`wwwroot/js/heatmap.js`; set `::deep .leaflet-container { background: var(--bg) }`
- [ ] 5.2 Keep the route amber `#ff9628`; verify contrast on the light background, adjust stroke width/opacity if needed
- [ ] 5.3 Bump the `?v=N` cache-bust in `Heatmap.razor`

## 6. Calendar slice (CalDAV / ICS)

- [ ] 6.1 Add `Infrastructure/Calendar/*`: an events model + a client supporting a published `.ics` URL and iCloud CalDAV (app-specific password)
- [ ] 6.2 Add server-side fetch + cache (tiles/crests pattern); options/secrets in `appsettings.Local.json`
- [ ] 6.3 Wire the Home calendar region (month grid + day timeline) to the live source, replacing dummy data; keep a dummy-data fallback when unconfigured

## 7. Night theme

- [ ] 7.1 Finalize the night palette and verify every page renders correctly in it
- [ ] 7.2 Implement the evening switch trigger (manual/clock/sunset — at minimum a deterministic switch) that swaps root `data-theme` without layout shift

## 8. Cleanup

- [ ] 8.1 Remove the Command Center theme and `--glass-*`/`--glow-*`/`--shadow-*` shims plus dead CSS
- [ ] 8.2 Final device verification at 1024×768: no-scroll holds, heatmap light + route legible, night switch clean

## 9. Verification

- [ ] 9.1 `dotnet run --launch-profile https --project src/Dashboard.Web`; check on the iPad 6 (1024×768 landscape, LAN): no-scroll, contrast/legibility, timeline height with ~5 appointments, heatmap light, night switch
