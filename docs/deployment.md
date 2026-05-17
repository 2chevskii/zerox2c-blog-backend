# Deployment

This repository deploys the 0x2c.dev backend API. It does not deploy MySQL and it does not deploy the public routing/static server. Those are separate infrastructure concerns.

Related repositories:

- `../blog-frontend`: builds and publishes the public reader SPA static files.
- `../blog-admin-frontend`: builds and publishes the admin SPA static files.
- `../blog-backend`: builds and deploys the API Docker container.

## Branches And Environments

The same workflow handles CI and deployment:

| Git ref | GitHub Environment | Public URL | Deploys |
| --- | --- | --- | --- |
| Pull request to `develop` or `master` | none | none | No |
| Push to `develop` | `development` | `https://dev.0x2c.dev` | Yes |
| Push to `master` | `production` | `https://0x2c.dev` | Yes |
| Manual `workflow_dispatch` on another ref | `preview` context | none | No |

Production approval is configured in GitHub repository settings, not in YAML. Configure required reviewers for the `production` environment in all three repositories. Development normally has no required reviewers.

Public deployments run with `ASPNETCORE_ENVIRONMENT=Production` for both `develop` and `master`. The branch selects deployment target and secrets; it does not select ASP.NET Core's `Development` environment.

## External Infrastructure

The backend deployment assumes these already exist:

- A Linux host reachable by SSH.
- Docker Engine with the Docker Compose plugin on that host.
- A separately deployed MySQL service reachable from the API container.
- An external routing server that terminates HTTPS and proxies `/api/` to the API loopback port.

The routing server must preserve the `/api` prefix when proxying to the API. It should forward `Host`, `X-Forwarded-Host`, `X-Forwarded-For`, and `X-Forwarded-Proto`; the API uses ASP.NET Core forwarded-header middleware so Steam callback URLs use the public HTTPS host.

Default API loopback ports:

| Environment | API |
| --- | ---: |
| production | `127.0.0.1:5103` |
| development | `127.0.0.1:5203` |

Default deployment directories:

```text
/opt/0x2c-blog/production/api
/opt/0x2c-blog/development/api
```

## GitHub Secrets

Define these in both the `development` and `production` GitHub Environments for this repository.

Required deployment secrets:

- `VPS_HOST`: SSH hostname or IP address.
- `VPS_SSH_USER`: SSH user.
- `VPS_SSH_PRIVATE_KEY`: private key for `VPS_SSH_USER`.
- `MYSQL_CONNECTION_STRING`: full API connection string for the separately deployed MySQL service.
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_SIGNING_KEY`: at least 32 UTF-8 bytes.

Optional deployment secrets:

- `VPS_SSH_PORT`: SSH port. Defaults to `22`.
- `DEPLOY_ROOT`: root deployment directory. Defaults to `/opt/0x2c-blog`.
- `APP_HOST`: host binding for the API container port. Defaults to `127.0.0.1`.
- `APP_PORT`: host port for the API. Defaults to `5103` for production and `5203` for development.
- `JWT_ACCESS_TOKEN_LIFETIME_MINUTES`: defaults to `60`.
- `SUPERADMIN_USE_DEFAULT_PASSWORD`: defaults to `false`. Keep this `false` for public deployments unless deliberately bootstrapping a temporary environment.
- `OPENAPI_ENABLED`: defaults to `false`.
- `DEVELOPER_EXCEPTION_PAGE_ENABLED`: defaults to `false`.

Workflow-provided variables used by `scripts/deploy.sh`:

- `DEPLOY_ENVIRONMENT`: `development` or `production`, from `scripts/resolve-deployment-context.sh`.
- `IMAGE`: GHCR image reference with the environment-specific commit tag.
- `DEFAULT_APP_PORT`: `5103` for production or `5203` for development.

Example `MYSQL_CONNECTION_STRING` shape:

```text
Server=mysql-host;Port=3306;Database=blog;User=blog_api;Password=...;AllowPublicKeyRetrieval=True;SslMode=None;
```

## Scripts

- `scripts/resolve-deployment-context.sh`: maps the GitHub event/ref to deployment outputs such as environment name, image tag, environment URL, and default API port.
- `scripts/smoke-docker-image.sh`: starts a temporary CI-only MySQL container, starts the API image, and checks `GET /api/health`.
- `scripts/deploy.sh`: creates `api.env` and `compose.yaml`, uploads them over SSH, runs `docker compose pull && docker compose up -d --remove-orphans`, then checks the API health endpoint through the host loopback binding.

The CI smoke-test MySQL container is temporary and local to the GitHub runner. It is not part of deployment.

## Workflow Process

1. Checkout and resolve deployment context.
   The workflow runs `scripts/resolve-deployment-context.sh`. Pull requests build but set `deploy=false`. Pushes to `develop` and `master` set `deploy=true`.

2. Restore and build the API.
   The workflow runs:

   ```bash
   dotnet restore ZeroX2C.Blog.slnx
   dotnet build ZeroX2C.Blog.slnx --configuration Release --no-restore
   ```

3. Build and smoke-test the Docker image.
   The workflow builds `zerox2c-blog-api:test`, runs `scripts/smoke-docker-image.sh`, and verifies `/api/health`.

4. Push the image to GHCR on deployable branches.
   Deployable pushes publish:

   ```text
   ghcr.io/{owner}/{repo}:sha-{commit}
   ghcr.io/{owner}/{repo}:{environment}-{commit}
   ```

5. Deploy over SSH.
   The deploy job runs in the selected GitHub Environment. For production, GitHub waits for required reviewers before exposing production environment secrets. `scripts/deploy.sh` uploads the generated Compose payload to:

   ```text
   {DEPLOY_ROOT}/{environment}/api
   ```

6. Start or update the API container.
   The host runs `docker compose pull` and `docker compose up -d --remove-orphans`. The API container receives production environment variables through `api.env`.

7. Verify deployment.
   The script checks:

   ```text
   http://{APP_HOST}:{APP_PORT}/api/health
   ```

## Runtime Files On The VPS

The backend deployment directory contains:

```text
compose.yaml
api.env
```

The generated Compose file contains only the `api` service. It does not create MySQL, volumes, routing rules, certificates, or static frontend services.

## External Routing Shape

Production routing should behave like:

```text
/api/*    -> proxy to http://127.0.0.1:5103/api/*
/admin/   -> serve /opt/0x2c-blog/production/admin/www with SPA fallback
/         -> serve /opt/0x2c-blog/production/reader/www with SPA fallback
```

Development uses:

```text
/api/*    -> proxy to http://127.0.0.1:5203/api/*
/admin/   -> serve /opt/0x2c-blog/development/admin/www with SPA fallback
/         -> serve /opt/0x2c-blog/development/reader/www with SPA fallback
```

## Operational Notes

- Startup applies EF Core migrations automatically. Database rollback is not handled by these workflows.
- Rollback for the API means redeploying a previous GHCR image tag. Database migrations remain forward-only unless handled manually.
- `docker compose up -d --remove-orphans` removes old services from this API deploy directory if they were previously managed by this repository.
- The post-deploy public checks are `https://{host}/api/health`, `https://{host}/`, and `https://{host}/admin/`.
