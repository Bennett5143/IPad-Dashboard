## ADDED Requirements

### Requirement: All subpages conform to the e-ink style

Every subpage SHALL be restyled to the paper/e-ink look: `/hvv`, `/weather`, `/football`, `/crypto`, `/status`, `/whoop`, `/runs`, `/runs/{id}`, `/habits`, and `/heatmap`. Each SHALL use paper tokens, a hairline/table look, mono numerals, accent only for state, flat surfaces, and dither where a fill is needed.

#### Scenario: A restyled subpage

- **WHEN** any listed subpage renders in the e-ink theme
- **THEN** it uses ramp tokens with hairline rules, mono numerals, flat surfaces, and reserves accent color for state only

### Requirement: Shared components use the new tokens

The shared components `Tile`, `StandingsTable`, `KnockoutBracketView`, `MetricRing`, `Sparkline`, `ChartFrame`, and the Insights cards SHALL be migrated onto the new tokens so every consuming page inherits the style.

#### Scenario: Shared component migration

- **WHEN** a shared component renders after migration
- **THEN** it draws with ramp/accent tokens and flat surfaces, with no leftover Command Center or glass styling

### Requirement: Run heatmap is light

The run heatmap SHALL be light/white to match the paper background. The dark Leaflet filter (`invert hue-rotate brightness contrast` on `::deep .leaflet-tile-pane`) SHALL be removed (leaving light OSM tiles) or replaced with a subtle paper filter (slight desaturation/warmth). The Leaflet container background SHALL be `var(--bg)` instead of `#16181d`. The route SHALL remain amber `#ff9628`, with contrast on the light background verified and stroke width/opacity adjusted if needed. The `?v=N` cache-bust in `Heatmap.razor` SHALL be incremented for the JS/CSS change.

#### Scenario: Light map with amber route

- **WHEN** the heatmap renders in the e-ink theme
- **THEN** the map tiles are light, the container background is `var(--bg)`, and the route is amber and legible against the light background

#### Scenario: Cache bust bumped

- **WHEN** the heatmap JS/CSS is changed
- **THEN** the `?v=N` query in `Heatmap.razor` is incremented so clients load the new assets
