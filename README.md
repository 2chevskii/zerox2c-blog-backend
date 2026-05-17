# 0x2c.dev Blog Backend

Backend API for the 0x2c.dev personal website. The application is a modular ASP.NET Core backend with custom authentication, admin content-management endpoints, public blog-post queries, EF Core persistence, and MySQL as the primary database.

This repository is no longer a generic .NET template. Treat it as an application codebase.

## Current Stack

- .NET 10 target framework.
- ASP.NET Core controllers.
- EF Core 9.x with `Pomelo.EntityFrameworkCore.MySql`.
- MySQL 8.4 for local development through Docker Compose.
- Markdig for backend Markdown parsing/rendering.
- HtmlSanitizer for backend-rendered HTML sanitization.
- JWT bearer authentication.
- Scalar/OpenAPI for local API exploration.
- Central package management through `Directory.Packages.props`.

## Repository Layout

```text
src/ZeroX2C.Blog.API/
  CrossCutting/
    Api/                  Route constraints and API infrastructure.
    Bootstrap/            Startup bootstrap pipeline.
  Modules/
    Assets/               MySQL-backed image upload and retrieval.
    Posts/                Blog posts, tags, public queries, admin use cases.
    Shared/               Generic shared domain primitives.
    Users/                Users, authentication, authorization, admin user operations.
  Persistence/
    Auditing/             EF Core save interceptor and audit handlers.
    Migrations/           EF Core migrations.
    BlogDbContext.cs      EF Core model configuration.
  Utility/
    Configuration/        Small configuration helpers.
  Program.cs              Composition root, middleware, auth, migrations, bootstrap.
```

## Implemented Product Surface

Public endpoints:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/steam`
- `GET /api/auth/steam/callback`
- `GET /api/me`
- `GET /api/posts`
- `GET /api/posts/{id:guid}`
- `GET /api/posts/{slug}`
- `GET /api/images/{id:guid}`

`GET /api/posts` accepts `offset`, `limit`, `search`, `tags`, `from`, and `to`.
The `tags` value is a comma-separated list of tag names. `from` and `to` are
inclusive published-date filters in `yyyy-MM-dd` format.

Admin endpoints:

- `POST /api/admin/images`
- `POST /api/admin/markdown/render`
- `GET /api/admin/users`
- `PUT /api/admin/users/{id}/role`
- `PUT /api/admin/users/{id}/password`
- `POST /api/admin/users/{id}/block`
- `POST /api/admin/users/{id}/unblock`
- `GET /api/admin/posts`
- `GET /api/admin/posts/{id}`
- `POST /api/admin/posts`
- `PUT /api/admin/posts/{id}`
- `POST /api/admin/posts/{id}/publish`
- `POST /api/admin/posts/{id}/unpublish`
- `DELETE /api/admin/posts/{id}`
- `GET /api/admin/posts/{id}/markdown/images`
- `POST /api/admin/posts/{id}/markdown/images`
- `GET /api/admin/tags`
- `GET /api/admin/tags/{id}`
- `POST /api/admin/tags`
- `PUT /api/admin/tags/{id}`
- `DELETE /api/admin/tags/{id}`

Planned but not currently implemented: pages, comments, Google/GitHub external login, sitemap, health checks, and integration tests.

## Local Development

Prerequisites:

- .NET SDK compatible with `global.json`.
- Docker Desktop or another Docker Compose compatible runtime.
- MySQL port `3306` available, or override the connection string.

Start local MySQL:

```powershell
docker compose up -d mysql
```

Build:

```powershell
dotnet build ZeroX2C.Blog.slnx
```

Run the API:

```powershell
dotnet run --project src/ZeroX2C.Blog.API/ZeroX2C.Blog.API.csproj
```

On startup the API applies EF Core migrations and then runs application bootstrap handlers. Do not leave locally started API processes running after verification work.

OpenAPI and Scalar are mapped by `Program.cs`; use the local application URL from launch output and navigate to the Scalar API reference path.

## Configuration

Current configuration sections:

- `ConnectionStrings:MySql`: MySQL connection string.
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SigningKey`: must be at least 32 UTF-8 bytes.
- `Jwt:AccessTokenLifetimeMinutes`
- `SuperAdmin:UseDefaultPassword`

The checked-in `appsettings.json` is a local development baseline. Use user secrets, environment variables, or deployment-specific configuration for real secrets.

## Technical Users

Technical users are defined in `Modules/Users/KnownUsers.cs`.

- System: id `00000000-0000-0000-0000-000000000001`, username `system`, role `SuperAdmin`.
- Superadmin: id `00000000-0000-0000-0000-000000000002`, username `superadmin`, role `SuperAdmin`.

The system user is only for code-driven execution contexts and must not get password, token, external login, or controller login paths.

The superadmin password is initialized at startup:

- If the known superadmin does not exist and `SuperAdmin:UseDefaultPassword` is `true`, bootstrap uses the default password defined in `KnownUsers`.
- If the known superadmin does not exist and `UseDefaultPassword` is `false`, bootstrap generates a random password and logs it on startup.
- Once the known superadmin exists, its password is not reset by bootstrap and can be changed through the admin API.

## Persistence And Auditing

All entities derived from `EntityBase` are audited through `EntityAuditSaveChangesInterceptor`. Saves require an authenticated `IAuthenticationContext`; bootstrap uses the system context through `IAuthenticationContextManager.AsSystem()`.

Generated IDs should use `Guid.CreateVersion7()` for sortable GUIDs. The known technical-user IDs are the only intentional exception.

EF Core migrations must be created with `dotnet ef`; do not hand-edit migration files.

## Documentation

Project docs live under `docs/architecture/`.

- `functional-requirements.md`: product behavior and rules.
- `blueprint.md`: implementation architecture and extension guidance.
- `data-schema.md`: current persistence model.
- `roadmap.md`: completed work, current gaps, and likely next phases.

Agent-specific instructions are in `AGENTS.md`; read it before modifying code.
