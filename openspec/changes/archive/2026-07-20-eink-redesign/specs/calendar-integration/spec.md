## ADDED Requirements

### Requirement: Server-side Apple Calendar data source

The system SHALL fetch appointments from Apple Calendar server-side (the Pi has internet), via either iCloud CalDAV (app-specific password) or a published `.ics` subscription URL. The server SHALL fetch and cache events (same pattern as tiles/crests); the iPad SHALL only render the finished agenda and month grid.

#### Scenario: Server fetches and caches

- **WHEN** the calendar source is configured and refreshed
- **THEN** the server fetches events from CalDAV or the `.ics` URL and caches them, and the client renders the cached agenda without contacting the calendar itself

#### Scenario: Configurable source

- **WHEN** an administrator configures the calendar
- **THEN** either a CalDAV account (app-specific password) or a published `.ics` URL can be used as the source

### Requirement: Secrets stay out of version control

Calendar configuration and secrets SHALL live in `appsettings.Local.json` (not versioned), following the WHOOP/Strava token pattern.

#### Scenario: Secrets not committed

- **WHEN** the calendar credentials are set
- **THEN** they are read from `appsettings.Local.json` and are not present in any versioned file

### Requirement: Buildable with dummy data first

The calendar feature SHALL be buildable against dummy data before the live source is connected, so the Home calendar region can be developed and validated independently.

#### Scenario: Dummy-data mode

- **WHEN** no live calendar source is configured
- **THEN** the Home calendar region renders with dummy appointments so the layout can be verified
