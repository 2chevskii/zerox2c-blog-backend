# Deployment

The blog is deployed from three independent public repositories:

- `blog-frontend`: reader SPA.
- `blog-admin-frontend`: admin SPA.
- `blog-backend`: API and environment-owned MySQL service.

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
- `APP_HOST`: optional, defaults to `127.0.0.1`.
- `APP_PORT`: optional. If omitted, workflows use the standard port for the app/environment.

Backend-only environment secrets:

- `MYSQL_DATABASE`
- `MYSQL_USER`
- `MYSQL_PASSWORD`
- `MYSQL_ROOT_PASSWORD`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_SIGNING_KEY`
- `JWT_ACCESS_TOKEN_LIFETIME_MINUTES`: optional, defaults to `60`.
- `SUPERADMIN_USE_DEFAULT_PASSWORD`: optional, defaults to `false`.
- `OPENAPI_ENABLED`: optional, defaults to `false`.
- `DEVELOPER_EXCEPTION_PAGE_ENABLED`: optional, defaults to `false`.

Default loopback ports:

| Environment | Reader | Admin | API |
| --- | ---: | ---: | ---: |
| production | 5101 | 5102 | 5103 |
| development | 5201 | 5202 | 5203 |

## VPS Layout

Each repository deploys only its own service under:

```text
/opt/0x2c-blog/{environment}/{app}
```

Current app names are:

- `reader`
- `admin`
- `api`

The backend deploy writes a Docker Compose file with `api` and `mysql` services and a private `mysql-data` volume per environment. Frontend deploys write a one-service Compose file for their static Nginx container.

## Host Nginx

Host-level Nginx owns HTTPS and routes to the loopback ports. Keep `/api/` unstripped when proxying to the backend. Strip `/admin/` when proxying to the admin frontend.

Production example:

```nginx
server {
    listen 80;
    server_name 0x2c.dev;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name 0x2c.dev;

    ssl_certificate /etc/letsencrypt/live/0x2c.dev/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/0x2c.dev/privkey.pem;

    location = /admin {
        return 308 /admin/;
    }

    location /admin/ {
        proxy_pass http://127.0.0.1:5102/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /api/ {
        proxy_pass http://127.0.0.1:5103;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location / {
        proxy_pass http://127.0.0.1:5101;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Development uses the same shape with `server_name dev.0x2c.dev` and ports `5201`, `5202`, and `5203`.

## Post-Deploy Checks

- `https://{host}/`
- `https://{host}/admin/`
- `https://{host}/api/health`
- Refresh a reader SPA route.
- Refresh an admin SPA route.
- Confirm the Steam login return URL is `https://{host}/api/auth/steam/callback`.
