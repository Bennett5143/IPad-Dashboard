#!/usr/bin/env bash
# Health-gated deploy for the containerized stack (see docs/deployment.md).
#
#   ./deploy.sh              # deploy latest origin/main
#   ./deploy.sh <ref>        # deploy a tag/commit (also how you roll back by hand)
#
# Builds the new image BEFORE recreating the container (downtime = seconds),
# then waits for /health/ready; if the new version does not come up healthy,
# it rebuilds and restarts the previously deployed commit.
set -euo pipefail
cd "$(dirname "$0")"

HEALTH_URL="${HEALTH_URL:-http://localhost:5235/health/ready}"

# Docker needs sudo on hosts where the caller is not in the docker group;
# keep git running as the calling user either way.
DOCKER="docker"
docker info >/dev/null 2>&1 || DOCKER="sudo docker"

git rev-parse HEAD > .last-deploy

if [ $# -eq 0 ]; then
    git checkout -q main
    git pull -q --ff-only
else
    git fetch -q origin
    git checkout -q --detach "$1"
fi

deploy_current() {
    $DOCKER compose build app
    $DOCKER compose up -d
}

healthy() {
    curl -fsS --max-time 5 "$HEALTH_URL" 2>/dev/null | grep -q '"status": "Healthy"'
}

deploy_current
for _ in $(seq 1 12); do
    sleep 5
    if healthy; then
        echo "deploy ok: $(git log --oneline -1)"
        exit 0
    fi
done

previous="$(cat .last-deploy)"
echo "health check failed — rolling back to ${previous}" >&2
git checkout -q --detach "$previous"
deploy_current
exit 1
