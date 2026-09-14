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

# 4. First deploy (pulls the CI-built image from GHCR and starts the stack)
./deploy.sh
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
- **Kiosk full screen**: the app ships a web app manifest (`display: "standalone"`
  for the presentation mode, `scope: "/"` for the app's URL boundary). iOS reads
  the manifest when the home-screen icon is created, and an existing icon may keep
  what it read then. However, it does not always do so: a `scope` change deployed
  on 2026-08-22 took effect on the icon already on the iPad, without touching it.
  Safari 15.4 fetches the manifest during page load rather than only on "Add to
  Home Screen", which is the likely reason — this was not verified against a
  specific iPadOS version, so treat it as an observation, not a guarantee.
  So check the icon first, and only if subpages still leave standalone mode,
  remove it and add it again — that re-reads the manifest for certain.

## Updating

```bash
./deploy.sh                  # latest main image
./deploy.sh dev              # latest dev image (second instance, see below)
./deploy.sh main sha-<sha>   # an explicit image revision (also manual rollback)
```

Images are built by CI as multi-arch (amd64 + arm64) and published to
`ghcr.io/bennett5143/ipad-dashboard` on every push to `main`/`dev`, tagged with
the branch name and an immutable `sha-<fullsha>` tag. The script checks out the
deployed branch (so the compose files match), pulls the image, recreates the
container (downtime is seconds) and waits for `/health/ready`. If the new
version does not come up healthy, it automatically redeploys the previously
running image revision via its sha tag. Volumes are untouched either way;
migrations only ever roll forward — for a destructive migration, take a DB dump
first (see below).

Run the script from a clean `main` checkout (a `dev` deploy leaves the working
tree detached on `origin/dev`; the next `./deploy.sh` puts it back on `main`).

If the registry or CI is unavailable, building locally remains possible:
`docker compose build app && docker compose up -d`.

## SBOM & security scanning

Every CI build and published image describes and checks itself (the local
`docker compose build` fallback gets neither attestations nor the SBOM
artifact); all of it is advisory and never blocks a merge or deploy (LAN-only
kiosk — base-image CVEs are fixed by base-image updates, not by app code):

- **Image SBOM + provenance**: each pushed image carries per-platform SPDX SBOM
  and provenance attestations (covering the app and the Debian base-image
  packages). Inspect them straight from the registry:

  ```bash
  docker buildx imagetools inspect ghcr.io/bennett5143/ipad-dashboard:main \
    --format '{{ json .SBOM }}'
  ```

- **App SBOM**: every CI run uploads a CycloneDX JSON of the NuGet dependency
  graph (direct + transitive) as the `sbom` workflow artifact.
- **Vulnerability signals**: Grype scans the published `main` image (weekly and
  after each image push) and OpenSSF Scorecard assesses the repo posture —
  both report into GitHub → Security → Code scanning, next to CodeQL.
  `dotnet restore` additionally audits all NuGet packages against known
  advisories (`NuGetAuditMode=all`), summarized in each Build & Test run.

## Running the dev branch side by side

`./deploy.sh dev` starts a second, fully isolated instance of the `dev` branch
next to the main stack: own containers (`dashboard-app-dev`, `dashboard-db-dev`),
own named volumes (compose prefixes them per project) and its own database, on
port **5236** (DB on `127.0.0.1:5433`). Isolation is mandatory — migrations only
roll forward, so a newer dev schema must never touch the main database.

The dev instance starts against an empty database (migrations + seeding run at
startup). To rehearse migrations on real data, copy the main database in —
always main → dev, never the reverse:

```bash
docker compose exec db pg_dump -U dashboard -Fc dashboard \
  | docker compose -p dashboard-dev -f docker-compose.yml -f docker-compose.dev.yml \
      exec -T db pg_restore -U dashboard -d dashboard --clean --if-exists
```

A copied database includes the Strava/WHOOP OAuth tokens — leave them to the
main instance (see the token note below) and don't run the dev connect flows
against the live APIs unless main's tokens are deliberately handed over.

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

## Retiring the research schema (one-off, Sep 2026) — done

The tool that wrote the `research` schema moved off this host, and the pages that
read it are gone. The schema was therefore dropped by hand rather than by a
migration: this application never owned those tables, and after the removal it
does not know them — a generated migration could only have reached the four it
once mapped, leaving the other twelve orphaned.

Executed on 2026-09-14 against both databases. Kept here because it is the
template for any future foreign-schema retirement, and because the record of what
was dropped belongs with the instructions.

The commands carry `sudo` because this host keeps Docker root-only; drop it where
the caller is in the `docker` group, the same distinction `deploy.sh` makes for
itself. As everywhere else in this file, `dashboard` is the role and database name
this host uses — substitute your `POSTGRES_USER` / `POSTGRES_DB` from `.env` if
yours differ. Note that every Postgres client binary runs **inside the container**:
the host has none installed, `pg_restore` included.

Run only **after** the release that removes the research pages is deployed, so
nothing queries the schema while it disappears.

```bash
# 1. Back up, and prove the dump carries the rows — not just the schema. The &&
#    chain is the point: every later step runs only if this one produced a
#    restorable archive, because afterwards it is the only copy.
sudo docker compose exec -T db pg_dump -U dashboard -Fc dashboard > research-drop.dump \
  && sudo docker compose exec -T db pg_restore -l < research-drop.dump \
       | grep -c 'TABLE DATA research' \
  && sha256sum research-drop.dump \
  && echo "backup ok"

# 2. Record what is about to go — the app only ever mapped four of these tables.
sudo docker compose exec -T db psql -U dashboard -d dashboard -c '\dt research.*'

# 3. Drop it. Only after step 1 printed "backup ok".
sudo docker compose exec -T db psql -U dashboard -d dashboard -c 'DROP SCHEMA research CASCADE;'

# 4. Verify.
sudo docker compose exec -T db psql -U dashboard -d dashboard \
  -c "SELECT 1 FROM information_schema.schemata WHERE schema_name = 'research';"
```

Step 1 counts `TABLE DATA` entries rather than grepping for the schema line, and
the difference matters: a dump can carry the schema definition and none of the
rows. The count must equal the number of tables from step 2 — sixteen, when this
ran. Step 4 returning no row is the check that the drop worked. The application
needs no restart; it holds no connection to those tables any more.

The dev instance has its own database and carried the same sixteen tables with
zero rows — a structure-only copy. It was dropped the same way, without a backup,
by prefixing the compose call with `-p dashboard-dev -f docker-compose.yml -f
docker-compose.dev.yml`.

The production dump is kept at
`/mnt/ssd/apps/backups/dashboard-pre-research-drop-20260914.dump` (7.7 MB, sha256
`3f42b3fa743eae4b4bcca2dbbc72ded5490b6179fdc091037827acd72147b785`). It is a full
database dump, so it also restores the dashboard's own tables.
