## ADDED Requirements

### Requirement: No-scroll bento home

The Home page SHALL be a bento grid that fits without scrolling at 1024×768 landscape. Tiles SHALL have no headings except the calendar month title.

#### Scenario: Fits the kiosk viewport

- **WHEN** Home is displayed at 1024×768 landscape on the iPad 6
- **THEN** all content fits with no vertical or horizontal scrolling

### Requirement: Clock and WHOOP rings (top-left)

The top-left column SHALL show a small analog clock, a digital time in `HH:MM` (e.g. `14:32`) with no date, and three WHOOP rings: Recovery colored by zone (green/yellow/red), Strain and Sleep rendered in ink. The analog clock hands SHALL be the only moving element on the page.

#### Scenario: Time display

- **WHEN** the top-left column renders
- **THEN** it shows the analog clock, the digital `HH:MM` time without a date, and three WHOOP rings

#### Scenario: Recovery is the only colored ring

- **WHEN** the WHOOP rings render
- **THEN** the Recovery ring is colored by its zone while Strain and Sleep are drawn in ink

### Requirement: Weather block (top-right)

The top-right region SHALL show current weather (glyph, large temperature, description, feels-like, high/low, wind) with a clearly labeled sunset (sunset icon + label + time). Below it, hourly cards SHALL sit side by side, each showing time → weather icon → temperature → rain %, with rain ≥30% rendered in ink. High/low temperatures SHALL be prefixed `H` and `T` (e.g. `H 21° / T 12°`).

#### Scenario: Current conditions with sunset

- **WHEN** the weather block renders
- **THEN** it shows current conditions and a clearly labeled sunset time with a sunset icon

#### Scenario: Hourly cards

- **WHEN** the hourly forecast renders
- **THEN** each card shows time, weather icon, temperature, and rain %, with rain ≥30% emphasized in ink

#### Scenario: High/low prefixes

- **WHEN** high and low temperatures are shown
- **THEN** they are prefixed with `H` and `T` respectively (e.g. `H 21° / T 12°`)

### Requirement: Calendar region (full width, bottom)

The bottom full-width region SHALL combine a mini month grid (today in the accent color, event dots on days with events) and an hourly day timeline (hour lines, appointments as blocks at real time/duration, gaps left visible as free time). The timeline window SHOULD be dynamic (from "now" or first–last appointment plus buffer) rather than a fixed 08–22 range.

#### Scenario: Month grid

- **WHEN** the calendar region renders
- **THEN** the current day is marked in the accent color and days with events show event dots

#### Scenario: Day timeline

- **WHEN** the day timeline renders
- **THEN** appointments appear as blocks positioned at their real start time and duration, with hour lines and visible gaps for free time

### Requirement: Health status moves to the rail

The Home page SHALL move the status/health icon into the rail and render it de-colored (ink/muted), distinguished by shape: `up` = pulse line, `down` = broken flatline. The icon SHALL link to the Status page.

#### Scenario: Status by shape

- **WHEN** the backend is up
- **THEN** the rail health icon shows a pulse line in ink; when down it shows a broken flatline, recognizable without color
