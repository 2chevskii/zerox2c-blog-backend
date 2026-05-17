#!/usr/bin/env bash
set -euo pipefail

repo="${GITHUB_REPOSITORY,,}"
{
  echo "image=ghcr.io/${repo}"

  if [[ "${GITHUB_EVENT_NAME}" == "pull_request" ]]; then
    echo "deploy=false"
    echo "environment=preview"
    echo "environment_url="
    echo "image_tag=pr-${GITHUB_SHA}"
    echo "default_app_port=0"
  elif [[ "${GITHUB_REF_NAME}" == "master" ]]; then
    echo "deploy=true"
    echo "environment=production"
    echo "environment_url=https://0x2c.dev"
    echo "image_tag=production-${GITHUB_SHA}"
    echo "default_app_port=5103"
  elif [[ "${GITHUB_REF_NAME}" == "develop" ]]; then
    echo "deploy=true"
    echo "environment=development"
    echo "environment_url=https://dev.0x2c.dev"
    echo "image_tag=development-${GITHUB_SHA}"
    echo "default_app_port=5203"
  else
    echo "deploy=false"
    echo "environment=preview"
    echo "environment_url="
    echo "image_tag=sha-${GITHUB_SHA}"
    echo "default_app_port=0"
  fi
} >> "${GITHUB_OUTPUT}"
