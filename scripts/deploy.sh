#!/usr/bin/env bash
set -euo pipefail

require_env() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "Missing ${name} environment secret" >&2
    exit 1
  fi
}

require_env VPS_HOST
require_env VPS_SSH_USER
require_env VPS_SSH_PRIVATE_KEY
require_env MYSQL_DATABASE
require_env MYSQL_USER
require_env MYSQL_PASSWORD
require_env MYSQL_ROOT_PASSWORD
require_env JWT_ISSUER
require_env JWT_AUDIENCE
require_env JWT_SIGNING_KEY
require_env DEPLOY_ENVIRONMENT
require_env IMAGE
require_env DEFAULT_APP_PORT

VPS_SSH_PORT="${VPS_SSH_PORT:-22}"
DEPLOY_ROOT="${DEPLOY_ROOT:-/opt/0x2c-blog}"
APP_HOST="${APP_HOST:-127.0.0.1}"
APP_PORT="${APP_PORT:-${DEFAULT_APP_PORT}}"
JWT_ACCESS_TOKEN_LIFETIME_MINUTES="${JWT_ACCESS_TOKEN_LIFETIME_MINUTES:-60}"
SUPERADMIN_USE_DEFAULT_PASSWORD="${SUPERADMIN_USE_DEFAULT_PASSWORD:-false}"
OPENAPI_ENABLED="${OPENAPI_ENABLED:-false}"
DEVELOPER_EXCEPTION_PAGE_ENABLED="${DEVELOPER_EXCEPTION_PAGE_ENABLED:-false}"

ssh_dir="$(mktemp -d)"
payload_dir="$(mktemp -d)"
cleanup() {
  rm -rf "${ssh_dir}" "${payload_dir}"
}
trap cleanup EXIT

key_path="${ssh_dir}/deploy_key"
known_hosts_path="${ssh_dir}/known_hosts"

printf '%s\n' "${VPS_SSH_PRIVATE_KEY}" > "${key_path}"
chmod 600 "${key_path}"
ssh-keyscan -p "${VPS_SSH_PORT}" "${VPS_HOST}" > "${known_hosts_path}"

cat > "${payload_dir}/mysql.env" <<MYSQL_ENV
MYSQL_ROOT_PASSWORD=${MYSQL_ROOT_PASSWORD}
MYSQL_DATABASE=${MYSQL_DATABASE}
MYSQL_USER=${MYSQL_USER}
MYSQL_PASSWORD=${MYSQL_PASSWORD}
MYSQL_ENV

cat > "${payload_dir}/api.env" <<API_ENV
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__MySql=Server=mysql;Port=3306;Database=${MYSQL_DATABASE};User=${MYSQL_USER};Password=${MYSQL_PASSWORD};AllowPublicKeyRetrieval=True;SslMode=None;
Jwt__Issuer=${JWT_ISSUER}
Jwt__Audience=${JWT_AUDIENCE}
Jwt__SigningKey=${JWT_SIGNING_KEY}
Jwt__AccessTokenLifetimeMinutes=${JWT_ACCESS_TOKEN_LIFETIME_MINUTES}
SuperAdmin__UseDefaultPassword=${SUPERADMIN_USE_DEFAULT_PASSWORD}
OpenApi__Enabled=${OPENAPI_ENABLED}
Diagnostics__DeveloperExceptionPageEnabled=${DEVELOPER_EXCEPTION_PAGE_ENABLED}
API_ENV

cat > "${payload_dir}/compose.yaml" <<COMPOSE
services:
  mysql:
    image: mysql:8.4
    restart: unless-stopped
    env_file:
      - mysql.env
    command:
      - --character-set-server=utf8mb4
      - --collation-server=utf8mb4_0900_ai_ci
    volumes:
      - mysql-data:/var/lib/mysql
    healthcheck:
      test:
        [
          "CMD-SHELL",
          "mysqladmin ping -h 127.0.0.1 -uroot -p\$\${MYSQL_ROOT_PASSWORD} --silent",
        ]
      interval: 10s
      timeout: 5s
      retries: 10

  api:
    image: ${IMAGE}
    restart: unless-stopped
    depends_on:
      mysql:
        condition: service_healthy
    env_file:
      - api.env
    ports:
      - "${APP_HOST}:${APP_PORT}:8080"

volumes:
  mysql-data:
COMPOSE

target="${VPS_SSH_USER}@${VPS_HOST}"
deploy_dir="${DEPLOY_ROOT}/${DEPLOY_ENVIRONMENT}/api"
ssh_options=(
  -i "${key_path}"
  -p "${VPS_SSH_PORT}"
  -o "UserKnownHostsFile=${known_hosts_path}"
)

ssh "${ssh_options[@]}" "${target}" "mkdir -p '${deploy_dir}'"
scp \
  -i "${key_path}" \
  -P "${VPS_SSH_PORT}" \
  -o "UserKnownHostsFile=${known_hosts_path}" \
  "${payload_dir}/compose.yaml" \
  "${payload_dir}/api.env" \
  "${payload_dir}/mysql.env" \
  "${target}:${deploy_dir}/"

ssh "${ssh_options[@]}" "${target}" "cd '${deploy_dir}' && docker compose pull && docker compose up -d --remove-orphans"

for attempt in {1..30}; do
  if ssh "${ssh_options[@]}" "${target}" "curl --fail --silent --show-error 'http://${APP_HOST}:${APP_PORT}/api/health'"; then
    exit 0
  fi

  sleep 2
done

ssh "${ssh_options[@]}" "${target}" "cd '${deploy_dir}' && docker compose logs api --tail=120"
exit 1
