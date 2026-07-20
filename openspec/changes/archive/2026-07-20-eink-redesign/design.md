## Context

The dashboard is a Blazor Server app rendering on a wall-mounted iPad 6 (1024×768 landscape, weak GPU, LAN-only, no local internet — the Raspberry Pi server has internet and holds state). The current "Command Center" theme (dark, five domain colors, PR #107) reads as noisy. The approved design (Moodboard Rev 5, TRMNL-inspired) wants a calm paper info sheet. This is a UI/layout/navigation refactor over the existing project — Blazor Server stays because it fits an offline iPad with a connected server perfectly. See `proposal.md` for motivation and the five capability specs for the concrete requirements.

## Goals / Non-Goals

**Goals:**
- One token swap in `app.css` drives both a paper day theme and a night theme via `data-theme`, mirroring the Waldgrün re-theme (slice 15.2) approach.
- Rebuild Home as a no-scroll bento and migrate every subpage + shared component to the new tokens.
- Replace global navigation with hub-&-spoke plus an icon rail.
- Add a server-side Apple Calendar data source feeding the Home calendar.
- Keep the UI static (only the analog clock hands move) to respect the iPad-6 GPU.

**Non-Goals:**
- No rewrite of the app or a framework change (Blazor Server stays).
- No new client-side rendering framework or SPA router.
- No change to existing data integrations (WHOOP, Strava, football, HVV, crypto) beyond restyling their surfaces.
- Final tuning of radius/dither intensity and the night-switch trigger are deferred (see Open Questions).

## Decisions

- **Token swap over per-component rewrite.** Redefine the same `:root` token names under `data-theme="eink"` and `data-theme="night"`, then point component CSS at tokens. *Why:* proven by the Waldgrün re-theme; keeps two themes in sync and lets us A/B against Command Center by leaving it as the default until migration completes. *Alternative:* per-component restyle — rejected as more work and drift-prone.
- **Themes are a hard swap, not a transition.** *Why:* e-ink discipline and the weak GPU favor static rendering; cross-fades are decorative motion we explicitly avoid.
- **Dither as a CSS utility, not raster images.** `radial-gradient(var(--faint) .5px, transparent .6px); background-size:3px 3px`. *Why:* scales, themes automatically via `--faint`, zero asset weight. *Alternative:* 1-bit PNG textures — rejected (per-theme assets, no token reuse).
- **Hub-&-spoke by rebuilding `KioskLayout`/NavMenu, not adding guards.** Remove the global navbar; each area is reached only from Home and returns to Home. *Why:* the spec forbids lateral jumps; simplest is to not render cross-area nav at all rather than police routes.
- **Calendar fetched and cached server-side, dummy-data first.** Mirror the tiles/crests cache pattern; build the Home calendar region against dummy appointments, then wire the live source. *Why:* decouples the layout slice from the integration slice and keeps the iPad rendering only finished data. *Decision CalDAV vs ICS:* support a published `.ics` URL as the simplest path and iCloud CalDAV (app-specific password) as the richer one; secrets live in `appsettings.Local.json` like WHOOP/Strava.
- **Slice-by-slice delivery, one branch → one squash PR.** Order: tokens → home spike (behind `data-theme="eink"`, verify on device) → nav refactor → subpages (shared components first) → heatmap light → calendar slice → night theme → cleanup. *Why:* each slice is independently verifiable on the real iPad, and the home spike de-risks the whole look before broad migration.
- **Heatmap: remove the dark Leaflet filter rather than invert twice.** Light OSM tiles stay light; container background becomes `var(--bg)`; route stays amber `#ff9628`; bump `?v=N`. *Why:* the current dark look is a CSS filter hack; dropping it is less code and lets real tile colors read on paper.

## Risks / Trade-offs

- **No-scroll breaks at 1024×768 when the calendar timeline grows** → keep the timeline window dynamic (from "now" / first–last appointment + buffer) and verify with ~5 appointments + hour lines on the device before merging the home slice.
- **Removing `--glass-*`/`--glow-*`/`--shadow-*` shims breaks pages not yet migrated** → keep Command Center as the default theme and remove shims only in the final cleanup slice, after every page is on tokens.
- **Amber route contrast is weaker on a light background than on dark** → verify contrast; adjust stroke width/opacity, not the hue (hue is data).
- **CalDAV auth / `.ics` availability may fail on the Pi** → dummy-data mode keeps Home rendering; the calendar region degrades to placeholder rather than blocking the page.
- **Stale cached heatmap assets after the light change** → the `?v=N` cache-bust bump forces clients to reload JS/CSS.

## Migration Plan

1. Land tokens behind `data-theme`, Command Center still default (no visual change yet).
2. Build the bento Home behind `data-theme="eink"`; verify no-scroll/contrast on the iPad 6 (A/B).
3. Refactor navigation to hub-&-spoke + icon rail + rail health status.
4. Migrate shared components, then each subpage/batch onto tokens.
5. Switch the heatmap to light and bump the cache-bust.
6. Add the calendar slice (dummy data → live CalDAV/ICS).
7. Finalize the night theme and its switch trigger.
8. Cleanup: remove Command Center + glass/glow/shadow tokens and dead CSS. **Rollback:** until step 8, reverting `data-theme` (or the default) restores Command Center; each slice is its own PR and revertible.

## Open Questions

- Timeline window: dynamic vs. fixed 08–22.
- Calendar: final yes/no (currently yes, dummy in the moodboard).
- Radius and dither intensity: final tuning values.
- Night switch trigger: manual vs. clock-time vs. sunset-driven.
