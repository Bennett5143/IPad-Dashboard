## ADDED Requirements

### Requirement: No global navbar between subpages

The system SHALL remove the global navbar that allowed jumping directly between subpages (e.g. Football → run heatmap). The existing global navigation (`KioskLayout`/NavMenu) SHALL be rebuilt to reflect the hub-&-spoke model.

#### Scenario: No cross-subpage navigation

- **WHEN** the user is on any subpage
- **THEN** there is no global navbar offering a direct jump to a different subpage area

### Requirement: Five isolated hub-and-spoke areas

The system SHALL expose five isolated areas, each reachable only from Home, with return always via Home: HVV, Fitness (containing `/heatmap`, `/runs`, `/habits`, `/whoop`), Football, Crypto, and Status.

#### Scenario: Enter and return

- **WHEN** the user opens an area from Home
- **THEN** they can navigate within that area and return to Home, but cannot hop laterally into another area

#### Scenario: Fitness groups its subpages

- **WHEN** the user is in the Fitness area
- **THEN** `/heatmap`, `/runs`, `/habits`, and `/whoop` are reachable within that area

### Requirement: Icon-only rail

Home SHALL show a bottom rail of icons only (no text): Bus, Dumbbell, Football, Bitcoin, and Health. The four data icons open their areas; the Status area hangs off the Health icon. New icons SHALL be provided: a frontal bus, a thick dumbbell, football, bitcoin, pulse/flatline, and a weather set (sunny/cloudy/overcast/rain/sunset).

#### Scenario: Rail opens areas

- **WHEN** the user taps a rail icon
- **THEN** the corresponding area opens (Health icon opens Status), with no text labels shown on the rail
