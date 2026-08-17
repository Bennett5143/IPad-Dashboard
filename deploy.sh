#!/usr/bin/env bash
# Health-gated, pull-based deploy for the containerized stack (see docs/deployment.md).
#
#   ./deploy.sh                  # deploy the latest main image (port 5235)
#   ./deploy.sh dev              # deploy the latest dev image as the second
#                                # instance (port 5236, own database)
#   ./deploy.sh main sha-<sha>   # deploy an explicit image revision — also how
#                                # you roll back by hand
#
# Pulls the CI-built multi-arch image from GHCR (see .github/workflows/docker.yml);
# nothing is built on this host anymore. `docker compose build app` remains a
# manual fallback if the registry or CI is unavailable.
#
# Before recreating, the currently running image's revision (git sha, stamped as
# an OCI label by CI) is recorded; if the new version does not come up healthy,
# that revision is redeployed via its immutable sha-<fullsha> tag.
set -euo pipefail
cd "$(dirname "$0")"

TARGET="${1:-main}"
REF="${2:-}"

# Docker needs sudo on hosts where the caller is not in the docker group; keep
# git running as the calling user either way. `env` carries APP_TAG across sudo.
SUDO=()
docker info >/dev/null 2>&1 || SUDO=(sudo)

case "$TARGET" in
    main)
        # No -p: the main stack keeps the default (directory-based) project
        # name it has always had, so its containers and volumes stay the same.
        COMPOSE_ARGS=(-f docker-compose.yml)
        APP_CONTAINER="dashboard-app"
        HEALTH_URL="${HEALTH_URL:-http://localhost:5235/health/ready}"
        git checkout -q main
        git pull -q --ff-only
        ;;
    dev)
        # Separate compose project: own containers and own volumes (compose
        # prefixes them per project) — dev never touches the main database.
        COMPOSE_ARGS=(-p dashboard-dev -f docker-compose.yml -f docker-compose.dev.yml)
        APP_CONTAINER="dashboard-app-dev"
        HEALTH_URL="${HEALTH_URL:-http://localhost:5236/health/ready}"
        git fetch -q origin
        git checkout -q --detach origin/dev
        ;;
    *)
        echo "usage: ./deploy.sh [main|dev] [image-tag]" >&2
        exit 2
        ;;
esac

APP_TAG="${REF:-$TARGET}"

dc() {
    "${SUDO[@]}" env APP_TAG="$APP_TAG" docker compose "${COMPOSE_ARGS[@]}" "$@"
}

healthy() {
    curl -fsS --max-time 5 "$HEALTH_URL" 2>/dev/null | grep -q '"status": "Healthy"'
}

# Rollback handle: the revision label of the image that is running right now.
previous="$("${SUDO[@]}" docker inspect \
    --format '{{ index .Config.Labels "org.opencontainers.image.revision" }}' \
    "$APP_CONTAINER" 2>/dev/null || true)"
if [ -n "$previous" ]; then
    echo "$previous" > ".last-deploy-${TARGET}"
fi

dc pull app
dc up -d

for _ in $(seq 1 12); do
    sleep 5
    if healthy; then
        echo "deploy ok: ${TARGET} @ ${APP_TAG}"
        exit 0
    fi
done

if [ -n "$previous" ]; then
    echo "health check failed — rolling back to sha-${previous}" >&2
    APP_TAG="sha-${previous}"
    dc pull app
    dc up -d
else
    echo "health check failed and no previous image is recorded (first deploy?)." >&2
    echo "manual rollback: ./deploy.sh ${TARGET} sha-<fullsha>" >&2
fi
exit 1
