# iPad-Dashboard

Selbstgehostetes Dashboard für ein iPad 6th Gen, das auf einem
Raspberry Pi 4 läuft. Anzeigt: Uhrzeit, Wetter, Habit-Tracker,
Fußball-Ergebnisse, HVV-Abfahrten, Zitat des Tages.

## Tech-Stack
- C# / .NET 10 mit Blazor Server
- PostgreSQL 16
- Docker / docker-compose
- Hosting auf Raspberry Pi 4 (ARM64)

## Status
🚧 In Entwicklung – Phase 0 (Repo-Foundation)

## Setup
_Folgt in Phase 1._

## Branching
- `main` ist immer in einem deploybaren Zustand.
- Features laufen in Branches `<type>/phase-<X.Y>-<kurz-beschreibung>`.
- Commits laufen mit FA-Referenz 
    `<type>(<scope>): <kurze Beschreibung> [<FA-Referenz>]
    <optionaler Body mit mehr Details, max 72 Zeichen pro Zeile>`.
- Merges in `main` nur via Pull Request.