## ADDED Requirements

### Requirement: Paper and night color palettes behind a data-theme switch

The system SHALL define two color-token palettes over the same `:root` token names, selected by a `data-theme` attribute: a paper day theme (`data-theme="eink"`) and a night theme (`data-theme="night"`). Both palettes SHALL define `--bg`, `--ink`, `--muted`, `--faint`, `--rule`, `--track`, and `--accent`/`--good` (forest green). Switching SHALL be a hard swap (no cross-fade).

#### Scenario: Day theme active

- **WHEN** the root element has `data-theme="eink"`
- **THEN** the page background is the paper tone `#f5f2eb`, text uses ink `#23201a`, and the accent is forest green `#2f8256`

#### Scenario: Night theme active

- **WHEN** the root element has `data-theme="night"`
- **THEN** the page background is `#121210`, text uses `#ece6d8`, and the accent is forest green `#57c98a`

#### Scenario: Command Center remains available during migration

- **WHEN** neither `data-theme="eink"` nor `data-theme="night"` is set
- **THEN** the previous Command Center theme still renders, so the redesign can be A/B compared until migration completes

### Requirement: Color carries information only

The system SHALL restrict color to information, not decoration. The single primary accent (forest green) SHALL be used only for links and active selection. All other color SHALL appear only on data points: WHOOP recovery zone (green/yellow/red, slightly desaturated for paper), bull/bear sign, and the heatmap route amber `#ff9628`.

#### Scenario: Neutral chrome

- **WHEN** a surface, border, label, or heading is rendered
- **THEN** it uses only ramp tokens (bg/ink/muted/faint/rule/track), never a domain or accent color for decoration

#### Scenario: Data-bearing color outside the ramp

- **WHEN** a WHOOP recovery value, a bull/bear price move, or a heatmap route is rendered
- **THEN** its state color is applied to that data point only

### Requirement: Flat surfaces with hairlines and dither texture

The system SHALL render flat tiles: no tile gradients, no shadows, no glows. Surfaces SHALL use a 1px `--rule` hairline border and a small radius (~6px). Texture, where a fill is needed, SHALL come from a CSS dither utility using `radial-gradient(var(--faint) .5px, transparent .6px)` with `background-size: 3px 3px`. The legacy `--glass-*`, `--glow-*`, and `--shadow-*` shims SHALL be removed once migration is complete.

#### Scenario: Tile rendering

- **WHEN** a tile or card is rendered in the e-ink theme
- **THEN** it has a flat background, a 1px `--rule` border, ~6px radius, and no shadow or gradient

#### Scenario: Dither fill

- **WHEN** a surface needs a filled texture (e.g. ring track, event block)
- **THEN** it uses the dither utility rather than a solid decorative color

### Requirement: Typography roles

The system SHALL use JetBrains Mono for data, labels, and times (the primary "star" typeface) and Space Grotesk for prose and headings. Both fonts are already loaded in-app.

#### Scenario: Numeric data uses mono

- **WHEN** a time, metric, price, or label is rendered
- **THEN** it is set in JetBrains Mono

### Requirement: Day/night switching

The system SHALL support switching between the day and night themes. The switching trigger (manual, clock time, or sunset-driven) is configurable; at minimum a deterministic switch SHALL be available so the night theme can be validated on the device.

#### Scenario: Evening switch

- **WHEN** the configured night condition is met
- **THEN** the root `data-theme` becomes `night` and every page re-renders in the night palette without layout shift
