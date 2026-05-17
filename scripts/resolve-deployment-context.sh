#!/usr/bin/env bash
set -euo pipefail

log() {
  echo "[deployment-context] $*" >&2
}

repo="${GITHUB_REPOSITORY,,}"
image="ghcr.io/${repo}"

if [[ "${GITHUB_EVENT_NAME}" == "pull_request" ]]; then
  deploy="false"
  environment="preview"
  environment_url=""
  image_tag="pr-${GITHUB_SHA}"
  default_app_port="0"
elif [[ "${GITHUB_REF_NAME}" == "master" ]]; then
  deploy="true"
  environment="production"
  environment_url="https://0x2c.dev"
  image_tag="production-${GITHUB_SHA}"
  default_app_port="5103"
elif [[ "${GITHUB_REF_NAME}" == "develop" ]]; then
  deploy="true"
  environment="development"
  environment_url="https://dev.0x2c.dev"
  image_tag="development-${GITHUB_SHA}"
  default_app_port="5203"
else
  deploy="false"
  environment="preview"
  environment_url=""
  image_tag="sha-${GITHUB_SHA}"
  default_app_port="0"
fi

{
  echo "image=${image}"
  echo "deploy=${deploy}"
  echo "environment=${environment}"
  echo "environment_url=${environment_url}"
  echo "image_tag=${image_tag}"
  echo "default_app_port=${default_app_port}"
} >> "${GITHUB_OUTPUT}"

log "event=${GITHUB_EVENT_NAME} ref=${GITHUB_REF_NAME} sha=${GITHUB_SHA}"
log "deploy=${deploy} environment=${environment} environment_url=${environment_url:-<none>}"
log "image=${image}:${image_tag} default_app_port=${default_app_port}"
