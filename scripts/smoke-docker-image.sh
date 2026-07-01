#!/usr/bin/env bash
set -euo pipefail

image="${1:-zerox2c-blog-api:test}"
network_name="zerox2c-blog-ci"
postgres_container="zerox2c-blog-postgres-ci"
api_container="zerox2c-blog-api-ci"

log() {
  echo "[smoke-test] $*"
}

cleanup() {
  log "Cleaning up temporary containers and network"
  docker rm -f "${api_container}" "${postgres_container}" >/dev/null 2>&1 || true
  docker network rm "${network_name}" >/dev/null 2>&1 || true
}
trap cleanup EXIT

log "Starting smoke test for image ${image}"
log "Creating Docker network ${network_name}"
docker network create "${network_name}" >/dev/null
log "Starting temporary PostgreSQL container ${postgres_container}"
docker run \
  --detach \
  --name "${postgres_container}" \
  --network "${network_name}" \
  --network-alias postgres \
  --env POSTGRES_DB=blog_ci \
  --env POSTGRES_USER=blog \
  --env POSTGRES_PASSWORD=blogpassword \
  postgres:17-alpine \
  >/dev/null

log "Waiting for PostgreSQL readiness"
for attempt in {1..60}; do
  if docker exec "${postgres_container}" pg_isready -h 127.0.0.1 -U blog -d blog_ci >/dev/null 2>&1; then
    log "PostgreSQL is ready after attempt ${attempt}"
    break
  fi

  if [[ "${attempt}" == "60" ]]; then
    log "PostgreSQL did not become ready; dumping logs"
    docker logs "${postgres_container}"
    exit 1
  fi

  sleep 2
done

log "Starting API container ${api_container}"
docker run \
  --detach \
  --name "${api_container}" \
  --network "${network_name}" \
  --publish 18080:8080 \
  --env ASPNETCORE_ENVIRONMENT=Production \
  --env ConnectionStrings__PostgreSql='Host=postgres;Port=5432;Database=blog_ci;Username=blog;Password=blogpassword;' \
  --env Jwt__Issuer=ZeroX2C.Blog.CI \
  --env Jwt__Audience=ZeroX2C.Blog.Api.CI \
  --env Jwt__SigningKey=ci-signing-key-with-at-least-thirty-two-bytes \
  --env Jwt__AccessTokenLifetimeMinutes=60 \
  --env SuperAdmin__UseDefaultPassword=true \
  "${image}" \
  >/dev/null

log "Waiting for API health endpoint"
for attempt in {1..60}; do
  if curl --fail --silent --show-error http://127.0.0.1:18080/api/health >/dev/null 2>&1; then
    log "API health endpoint passed after attempt ${attempt}"
    exit 0
  fi

  if [[ "${attempt}" == "60" ]]; then
    log "API health endpoint did not pass; dumping logs"
    docker logs "${api_container}"
    exit 1
  fi

  sleep 2
done
