---
tags:
  - dev/doc
---

# Deployment

How to run the dashboard as containers on a small always-on host (any amd64 or
arm64 box — a Raspberry Pi 4 works). Development setup stays as described in
the [README](../README.md): `docker compose up -d db` + `dotnet run` on the host.

## One-time setup on the host

```bash
git clone https://github.com/Bennett5143/IPad-Dashboard.git
cd IPad-Dashboard

# 1. Postgres credentials (compose reads .env automatically)
cp .env.Example .env            # then set a real POSTGRES_PASSWORD
chmod 600 .env

# 2. API keys / OAuth client secrets (optional — tiles degrade gracefully)
cat > dashboard.secrets.env <<'EOF'
Weather__ApiKey=...
Football__ApiKey=...
Strava__ClientId=...
Strava__ClientSecret=...
Whoop__ClientId=...
Whoop__ClientSecret=...
EOF
chmod 600 dashboard.secrets.env

# 3. Private-but-not-secret config (location, transit stops, tracked clubs)
cp src/Dashboard.Web/appsettings.Local.json.example src/Dashboard.Web/appsettings.Local.json
# fill it in; the file is mounted read-only into the container

# 4. Build and start
docker compose up -d --build
```

The app listens on port **5235** (mapped from the container's 8080). Pending EF
migrations are applied automatically at startup (`Seeding:ApplyMigrations=true`
is set in the compose file); quotes are seeded on first run.

## Notes for production hosts

- **Pin `AllowedHosts`** in `appsettings.Local.json` (e.g.
  `"AllowedHosts": "myhost.local;localhost"`) so foreign `Host` headers are
  rejected (DNS-rebinding hardening). The default is `*`.
- **Plain HTTP** is the intended LAN transport; there is no TLS termination in
  the stack. Do not port-forward the app to the internet as-is — it has no
  authentication by design (kiosk).
- **OAuth redirect URIs** (`Strava:RedirectUri`, `Whoop:RedirectUri`) default to
  `localhost` dev URLs. To run the connect flows against a hosted instance,
  override them in `appsettings.Local.json` and register the same URIs in the
  Strava/WHOOP developer portals (WHOOP requires `https`). Existing tokens keep
  working — the redirect URI only matters for (re-)connecting.
- **Map tile warm-up**: `GET /tiles/warm` is disabled outside Development.
  Enable it temporarily for the initial cache fill:
  `Tiles__WarmupEnabled=true docker compose up -d app`, warm, then revert.
- **State lives in named volumes**: `dashboard-db-data` (Postgres),
  `dashboard-tile-cache`, `dashboard-crest-cache`, `dashboard-logs`. Rebuilding
  or recreating containers keeps all of them.
- **Health probes**: `GET /health/live` and `GET /health/ready` (checks the DB).

## Updating

```bash
./deploy.sh            # latest origin/main
./deploy.sh <ref>      # a specific tag/commit (also manual rollback)
```

The script builds the new image before recreating the container (downtime is
seconds), waits for `/health/ready`, and automatically rolls back to the
previously deployed commit if the new version does not come up healthy.
Volumes are untouched either way; migrations only ever roll forward — for a
destructive migration, take a DB dump first (see below).

## Moving an existing database in

To carry data (OAuth tokens, habit history, synced runs) over from another
instance, dump and restore before first start — or into a stopped app:

```bash
# on the source machine
docker compose exec db pg_dump -U dashboard -Fc dashboard > dashboard.dump

# on the target host
docker compose stop app
docker compose exec -T db pg_restore -U dashboard -d dashboard --clean --if-exists < dashboard.dump
docker compose start app
```

Only one instance should use the Strava/WHOOP tokens afterwards — both
providers rotate refresh tokens, so two instances refreshing the same token
lock each other out.
