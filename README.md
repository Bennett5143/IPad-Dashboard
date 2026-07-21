# iPad-Dashboard

[![CI](https://github.com/Bennett5143/IPad-Dashboard/actions/workflows/ci.yml/badge.svg)](https://github.com/Bennett5143/IPad-Dashboard/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-informational.svg)](LICENSE)

A self-hosted personal dashboard that pulls health, sports, market, weather and
transit data into a single, glanceable home screen — each tile taps through to a
deeper detail page.

It's a standard ASP.NET Core web app: run it on any machine and open it in any
browser. The name comes from the author's own setup, but nothing ties it to that
hardware.

## Who it's for

Anyone who wants one screen that answers *"what do I need to know right now?"* —
built for self-hosters comfortable with .NET and a Postgres container. It doubles
as a reference implementation of a cleanly layered Blazor Server app (vertical
slices, background polling, graceful degradation, offline-friendly asset proxying).

## Features

- **Home** — a calm, e-ink-style paper bento: clock, recovery rings, weather
  and a calendar agenda, with automatic day/night themes.
- **Fitness hub** — aggregates WHOOP and Strava into views no single app offers:
  a run heatmap, run list with detail profiles, recovery/sleep/training-load
  insights, and a habit history.
- **Football** — top-5 league tables, Champions League (league phase + knockout
  bracket), EM/WM tournaments and your tracked clubs.
- **Crypto** — a market watchlist with 7-day sparklines plus Fear & Greed sentiment.
- **Weather & transit** — current conditions with an hourly outlook, and live
  departures.
- **Status page** — health and observability for the running instance.

Every tile degrades gracefully: if an upstream API is down, that tile shows a
friendly "unavailable" state while the rest of the dashboard keeps working.

## Tech stack

- C# / .NET 10 · Blazor Server
- EF Core 10 (Npgsql) · PostgreSQL 16 + PostGIS (run tracks as geometry)
- Serilog · Leaflet (self-hosted) · Docker Compose

## Quick start

```bash
# 1. Start Postgres (PostGIS)
docker compose up -d db

# 2. Point the app at it (kept out of the repo via user secrets)
cd src/Dashboard.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=dashboard;Username=...;Password=..."

# 3. Apply migrations, then run
dotnet ef database update      # needs: dotnet tool install --global dotnet-ef
dotnet run
```

Then open the printed URL. The app runs fine with no API keys configured — the
data tiles simply stay in their "unavailable" state until you add credentials.

Health probes: `GET /health/live` (liveness) and `GET /health/ready` (incl. DB).

## Configuration

App-wide settings (URLs, refresh intervals, defaults) live in `appsettings.json`.
Secrets go in user secrets locally (or environment variables / a `.env` file in a
container). Private-but-not-secret values — your weather location, transit stops,
tracked clubs — go in a gitignored `appsettings.Local.json` (copy the provided
`appsettings.Local.json.example`).

| Integration | Source | Credentials |
| --- | --- | --- |
| Weather | OpenWeatherMap | API key |
| Football | football-data.org | API key (free tier) |
| Crypto | CoinGecko + alternative.me | none |
| Transit | HVV (Hamburg) | none |
| WHOOP | WHOOP API v2 | OAuth |
| Strava | Strava API | OAuth |

Each integration is optional and self-contained. Background services poll on their
own interval and push updates to the UI over SignalR without a page reload.

## Documentation

- [Architecture](docs/architecture.md) — layers, the vertical-slice pattern, UI and operations
- [Feature modules](docs/feature-modules.md) — what each module does and its key files
- [History](docs/history.md) — guiding decisions and a compact changelog

## License

[MIT](LICENSE)
