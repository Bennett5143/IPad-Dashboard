## Why

The current "Command Center" look (dark, five domain colors, since PR #107) feels "partly wrong" for a wall-mounted iPad info panel. Inspired by TRMNL e-ink terminals, we want a calm, paper-like information sheet: high contrast, a strict grid, typography-first, generous whitespace, static, no color noise. Color should carry information, never decoration. This is a UI/layout/navigation refactor inside the existing Blazor Server project — **not a rewrite**.

## What Changes

- **Design tokens & themes:** Swap the `:root` palette in `app.css` to a paper day theme (`data-theme="eink"`) plus a night theme (`data-theme="night"`) over the same tokens; add a 1-bit dither utility; flatten surfaces (remove tile gradients, shadows, glows). **BREAKING:** the Command Center theme and `--glass-*`/`--glow-*` shims are removed once migration is done (kept as fallback until then).
- **Home page:** Rebuild as a no-scroll bento (1024×768): analog clock + digital time + 3 WHOOP rings (top-left), weather with hourly cards and labeled sunset (top-right), full-width calendar (mini month grid + hourly day timeline) at the bottom.
- **Navigation:** Remove the global navbar between subpages; adopt hub-&-spoke with five isolated areas reachable only from Home; add an icon-only rail (Bus · Dumbbell · Football · Bitcoin · Health) and move the Status/health icon into the rail, de-colored by shape (pulse vs. flatline).
- **Subpages:** Re-theme every subpage (`/hvv`, `/weather`, `/football`, `/crypto`, `/status`, `/whoop`, `/runs`, `/runs/{id}`, `/habits`, `/heatmap`) and the shared components (`Tile`, `StandingsTable`, `KnockoutBracketView`, `MetricRing`, `Sparkline`, `ChartFrame`) onto the new tokens. Make the run heatmap **light/white** (remove the dark Leaflet filter, paper background, keep the amber route).
- **Calendar data source:** Add Apple Calendar (iCloud CalDAV app-specific password **or** published `.ics` URL) fetched and cached server-side; Home shows the finished agenda + month grid. Secrets in `appsettings.Local.json`.

## Capabilities

### New Capabilities
- `eink-theme-system`: Paper (day) and night color-token palettes behind `data-theme`, the dither texture utility, flat-surface rules, typography roles (JetBrains Mono for data, Space Grotesk for text), and the day/night switching behavior.
- `home-bento-layout`: The no-scroll bento Home page — clock + WHOOP rings, weather block with hourly cards and labeled sunset, and the combined calendar (month grid + day timeline) region.
- `hub-spoke-navigation`: The hub-&-spoke navigation model — five isolated areas reachable only from Home, the icon-only bottom rail, and the Status area hanging off the health icon.
- `eink-subpage-restyle`: Requirements for every subpage and shared component to conform to the e-ink style, including the light-map run heatmap with the amber route.
- `calendar-integration`: Server-side Apple Calendar data source (CalDAV or ICS) with caching, feeding the Home agenda and month grid.

### Modified Capabilities
<!-- No existing specs in openspec/specs/; everything is introduced as new capabilities. -->

## Impact

- **CSS/theme:** `src/Dashboard.Web/wwwroot/app.css` (tokens, dither utility, both themes), `Components/Tile.razor(.css)`.
- **Layout/nav:** `Home.razor(.css)`, `KioskLayout.razor(.css)`, NavMenu.
- **Weather/calendar components:** hourly cards, month grid, day timeline (partly new).
- **Heatmap:** `Heatmap.razor.css`, `wwwroot/js/heatmap.js` (light map style, `?v=N` cache-bust bump).
- **Shared components:** `StandingsTable`, `KnockoutBracketView`, `MetricRing`, `Sparkline`, `ChartFrame`, Insights cards.
- **New infrastructure:** `Infrastructure/Calendar/*` (CalDAV/ICS client + cache), options in `appsettings.Local.json`.
- **Constraints:** Blazor Server stays; static UI (single moving element is the analog clock hands) to suit the weak iPad-6 GPU; verify no-scroll at 1024×768 landscape on the real device.
