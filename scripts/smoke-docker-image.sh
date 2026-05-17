#!/usr/bin/env bash
set -euo pipefail

image="${1:-zerox2c-blog-api:test}"
network_name="zerox2c-blog-ci"
mysql_container="zerox2c-blog-mysql-ci"
api_container="zerox2c-blog-api-ci"

cleanup() {
  docker rm -f "${api_container}" "${mysql_container}" >/dev/null 2>&1 || true
  docker network rm "${network_name}" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker network create "${network_name}" >/dev/null
docker run \
  --detach \
  --name "${mysql_container}" \
  --network "${network_name}" \
  --network-alias mysql \
  --env MYSQL_ROOT_PASSWORD=rootpassword \
  --env MYSQL_DATABASE=blog_ci \
  --env MYSQL_USER=blog \
  --env MYSQL_PASSWORD=blogpassword \
  mysql:8.4 \
  --character-set-server=utf8mb4 \
  --collation-server=utf8mb4_0900_ai_ci \
  >/dev/null

for attempt in {1..60}; do
  if docker exec "${mysql_container}" mysqladmin ping -h 127.0.0.1 -uroot -prootpassword --silent >/dev/null 2>&1; then
    break
  fi

  if [[ "${attempt}" == "60" ]]; then
    docker logs "${mysql_container}"
    exit 1
  fi

  sleep 2
done

docker run \
  --detach \
  --name "${api_container}" \
  --network "${network_name}" \
  --publish 18080:8080 \
  --env ASPNETCORE_ENVIRONMENT=Production \
  --env ConnectionStrings__MySql='Server=mysql;Port=3306;Database=blog_ci;User=blog;Password=blogpassword;AllowPublicKeyRetrieval=True;SslMode=None;' \
  --env Jwt__Issuer=ZeroX2C.Blog.CI \
  --env Jwt__Audience=ZeroX2C.Blog.Api.CI \
  --env Jwt__SigningKey=ci-signing-key-with-at-least-thirty-two-bytes \
  --env Jwt__AccessTokenLifetimeMinutes=60 \
  --env SuperAdmin__UseDefaultPassword=true \
  "${image}" \
  >/dev/null

for attempt in {1..60}; do
  if curl --fail --silent --show-error http://127.0.0.1:18080/api/health >/dev/null 2>&1; then
    exit 0
  fi

  if [[ "${attempt}" == "60" ]]; then
    docker logs "${api_container}"
    exit 1
  fi

  sleep 2
done
