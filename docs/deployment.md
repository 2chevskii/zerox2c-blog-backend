# Deployment

The blog is deployed from three independent public repositories:

- `blog-frontend`: builds and publishes reader SPA static files.
- `blog-admin-frontend`: builds and publishes admin SPA static files.
- `blog-backend`: deploys the API Docker container.

Branch mapping:

- `develop` deploys to the `development` GitHub Environment and `https://dev.0x2c.dev`.
- `master` deploys to the `production` GitHub Environment and `https://0x2c.dev`.

Configure required reviewers on the `production` environment in each GitHub repository. The workflow files reference the environments, but reviewer protection is a GitHub repository setting.

## GitHub Environment Secrets

All three repositories use these environment secrets:

- `VPS_HOST`: SSH hostname or IP.
- `VPS_SSH_USER`: SSH user.
- `VPS_SSH_PRIVATE_KEY`: private key for the SSH user.
- `VPS_SSH_PORT`: optional, defaults to `22`.
- `DEPLOY_ROOT`: optional, defaults to `/opt/0x2c-blog`.

Backend-only deployment secrets:

- `APP_HOST`: optional, defaults to `127.0.0.1`.
- `APP_PORT`: optional. If omitted, workflows use the standard API port for the environment.

Backend-only application secrets:

- `MYSQL_CONNECTION_STRING`: connection string for the separately deployed MySQL service.
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_SIGNING_KEY`
- `JWT_ACCESS_TOKEN_LIFETIME_MINUTES`: optional, defaults to `60`.
- `SUPERADMIN_USE_DEFAULT_PASSWORD`: optional, defaults to `false`.
- `OPENAPI_ENABLED`: optional, defaults to `false`.
- `DEVELOPER_EXCEPTION_PAGE_ENABLED`: optional, defaults to `false`.

Default API loopback ports:

| Environment | API |
| --- | ---: |
| production | 5103 |
| development | 5203 |

## VPS Layout

Each repository deploys only its own files or service under:

```text
/opt/0x2c-blog/{environment}/{app}
```

Current app names are:

- `reader`
- `admin`
- `api`

Frontend deployments publish static files under:

```text
/opt/0x2c-blog/{environment}/reader/www
/opt/0x2c-blog/{environment}/admin/www
```

The backend deploy writes a one-service Docker Compose file for the API. It does not create or manage MySQL; database provisioning, storage, backups, and lifecycle are handled by a separate deployment.

## External Routing

An external routing server owns HTTPS, static file serving, SPA fallback behavior, and API reverse proxying. Keep `/api/` unstripped when proxying to the backend API. Serve the admin SPA under `/admin/`.

Production routing shape:

```text
/api/*    -> proxy to http://127.0.0.1:5103/api/*
/admin/   -> serve /opt/0x2c-blog/production/admin/www with SPA fallback
/         -> serve /opt/0x2c-blog/production/reader/www with SPA fallback
```

Development uses the same shape with `dev.0x2c.dev`, `/opt/0x2c-blog/development/...`, and API port `5203`.

## Post-Deploy Checks

- `https://{host}/`
- `https://{host}/admin/`
- `https://{host}/api/health`
- Refresh a reader SPA route.
- Refresh an admin SPA route.
- Confirm the Steam login return URL is `https://{host}/api/auth/steam/callback`.
