# Backend Architecture Blueprint

Last updated: 2026-05-16

This document describes the implemented architecture and the preferred way to extend it. It is written for future maintainers and agents who need to make changes without rediscovering the system shape from scratch.

## 1. Scope

The backend powers a personal website with:

- Public blog-post read APIs.
- Admin APIs for posts, tags, and users.
- Image/media upload and retrieval for blog content.
- Custom users, local credentials, Steam login, JWT authentication, and role-based authorization.
- EF Core persistence with audited entity writes.
- Startup migrations and technical-user bootstrap.

Planned additions:

- Informational pages.
- Comments.
- Google and GitHub external login.
- Sitemap and health endpoints.
- Test projects and integration tests.

Out of scope for the first production iteration:

- Multi-tenant publishing.
- Complex editorial workflow.
- Payments.
- Analytics pipelines.
- Non-image file storage.

## 2. Runtime And Persistence Stack

Current stack:

- Target framework: `net10.0`.
- ASP.NET Core controllers.
- EF Core: 9.x.
- MySQL provider: `Pomelo.EntityFrameworkCore.MySql` 9.x.
- Database: MySQL.
- Local database: Docker Compose with MySQL 8.4.
- API docs: OpenAPI and Scalar.
- Authentication: JWT bearer.

Important dependency decision:

- The app targets .NET 10, but EF Core is pinned to 9.x because the selected Pomelo MySQL provider is an EF Core 9 provider.
- Do not casually upgrade EF Core to 10 without checking MySQL provider compatibility and running migrations against a real MySQL instance.

## 3. Composition Root

`Program.cs` owns:

- DbContext registration.
- Strongly typed options registration.
- Application service registrations.
- Authentication and authorization configuration.
- Route options and slug constraint registration.
- Controller and JSON enum configuration.
- OpenAPI/Scalar registration.
- Middleware ordering.
- Migration and bootstrap execution.

Startup order:

1. Build service collection.
2. Configure JWT bearer authentication.
3. Configure authorization policies.
4. Build app.
5. Map OpenAPI and Scalar.
6. Use authentication.
7. Run `AuthenticationContextMiddleware`.
8. Use authorization.
9. Map controllers.
10. Apply EF Core migrations.
11. Run `ApplicationBootstrapper`.
12. Run the app.

The authentication context middleware must stay after `UseAuthentication()` and before `UseAuthorization()`.

## 4. Module Layout

Current module layout:

```text
Modules/
  Assets/
    Images/               MySQL-backed image entity, upload/read controllers, image services.
  Posts/
    Admin/                Admin post/tag use cases and operation results.
    Contracts/            Post DTOs.
    Contracts/Tags/       Tag DTOs.
    Controllers/          Public and admin post/tag controllers.
    Tags/                 Tag entity, post-tag join entity, tag name rules.
    Post.cs               Post entity.
    PostQueryService.cs   Public post query service.
  Shared/
    EntityBase.cs         Common audited entity fields.
  Users/
    Admin/                Admin user use cases and operation results.
    Auth/                 Auth services, JWT, context, bootstrap, providers.
    Contracts/            Auth/admin user DTOs.
    Controllers/          Auth and admin user controllers.
    KnownUsers.cs         Fixed technical users.
    User.cs               User entity.
```

Extension rules:

- Add new product areas as modules under `Modules/<Feature>/`.
- Keep controllers thin and delegate to application services.
- Keep EF entities in the owning module.
- Keep reusable infrastructure outside modules only when it is genuinely cross-cutting.
- Prefer direct, explicit code over generic frameworks until duplication is real.

## 5. API Surface

Implemented public API:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/steam`
- `GET /api/auth/steam/callback`
- `GET /api/me`
- `GET /api/posts`
- `GET /api/posts/{id:guid}`
- `GET /api/posts/{slug}`
- `GET /api/images/{id:guid}`

Implemented admin API:

- `POST /api/admin/images`
- `GET /api/admin/users`
- `PUT /api/admin/users/{id}/role`
- `POST /api/admin/users/{id}/block`
- `POST /api/admin/users/{id}/unblock`
- `GET /api/admin/posts`
- `GET /api/admin/posts/{id}`
- `POST /api/admin/posts`
- `PUT /api/admin/posts/{id}`
- `POST /api/admin/posts/{id}/publish`
- `POST /api/admin/posts/{id}/unpublish`
- `DELETE /api/admin/posts/{id}`
- `GET /api/admin/tags`
- `GET /api/admin/tags/{id}`
- `POST /api/admin/tags`
- `PUT /api/admin/tags/{id}`
- `DELETE /api/admin/tags/{id}`

Planned API:

- Google/GitHub auth challenge and callback endpoints.
- Public tags endpoint.
- Page public/admin endpoints.
- Comment public/admin endpoints.
- Sitemap endpoint.
- Health endpoints.

## 6. Authentication Context

Authentication data flows like this:

1. ASP.NET Core validates JWT and builds `ClaimsPrincipal`.
2. `AuthenticationContextMiddleware` converts the principal to `AuthenticationData`.
3. `AuthenticationContextManager` stores the data in `AsyncLocal`.
4. Services consume `IAuthenticationContext`.

Rules:

- Do not read `HttpContext.User` from application services.
- Inject `IAuthenticationContext` into services that need actor information.
- Use `MaybeUserId`/`MaybeRole` when anonymous access is expected.
- Use `UserId`/`Role` only when the calling path guarantees authentication.
- Use `IAuthenticationContextManager.AsSystem()` only for controlled code paths such as bootstrap.

## 7. Technical Users And Bootstrap

Known technical users:

- System: `00000000-0000-0000-0000-000000000001`, username `system`.
- Superadmin: `00000000-0000-0000-0000-000000000002`, username `superadmin`.

Bootstrap responsibilities:

- Ensure the system user exists.
- Ensure the superadmin user exists.
- Repair known technical user fields if they drift.
- Remove system login paths by keeping system password hash empty and clearing external logins.
- Fail startup if a non-technical user owns a known technical username or email.
- Fail startup if a user other than the known system and known superadmin users has `UserRole.SuperAdmin`.
- Set the superadmin password based on `SuperAdmin:UseDefaultPassword` only when creating the missing known superadmin.
- Do not reset an existing known superadmin password during bootstrap.

The system user exists because audited persistence requires an authenticated actor. Bootstrap enters a system authentication scope before writing technical users.

## 8. Persistence And Auditing

`BlogDbContext` configures the EF model centrally in `OnModelCreating`.

Current DbSets:

- `Users`
- `UserExternalLogins`
- `Posts`
- `Tags`
- `PostTags`
- `Images`

Auditing:

- `EntityBase` provides `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`, `IsDeleted`, `DeletedBy`, and `DeletedAt`.
- `EntityAuditSaveChangesInterceptor` applies audit behavior.
- Added entities get created metadata and reset update/delete metadata.
- Modified entities get update metadata.
- Deleted entities are converted to soft-deleted modified entities.
- Save operations throw when there is no authenticated context.

Persistence rules:

- Use `Guid.CreateVersion7()` for generated entity IDs.
- Technical-user IDs are fixed and are the only exception.
- Use EF Core migrations for schema changes.
- Do not manually edit migration files.
- Query services must explicitly filter soft-deleted rows.

## 9. Posts And Tags Design

Posts:

- `PostStatus` controls draft/published/archive-like state. Current public behavior uses only published posts.
- Public queries require `Status == Published`, `PublishedAt != null`, and `!IsDeleted`.
- Admin queries exclude deleted posts but include draft/published states.
- `PublishedBy` stores the authenticated actor id at publish time.
- Slugs are optional for posts.

Tags:

- Tags use `Name` as the stable kebab-case identifier.
- Tag names are unique, lowercase kebab-case, and at most 20 characters.
- Tags do not have a separate slug field.
- Deleting a tag soft-deletes active `PostTag` rows.

Slug rules:

- Lowercase ASCII letters, digits, and hyphen-separated segments.
- Post slug max length: 160.
- Tag name max length: 20.

Image rules:

- `Image` rows store blob bytes in MySQL for the current scope.
- Uploads are admin-only and use multipart form-data with `file` and `purpose`.
- Public retrieval is by image id so post bodies can embed `/api/images/{id}` URLs.
- Supported purposes are `Cover`, `Banner`, and `Embedded`.
- Post cover and banner image ids must refer to active images with matching purpose.
- Keep image storage behind the `Modules/Assets/Images` service/controller boundary so MySQL blobs can move to object storage later without changing post services or frontend contracts.

## 10. Security Notes

- Keep secrets outside committed config for non-local environments.
- JWT signing key must contain at least 32 UTF-8 bytes.
- The checked-in appsettings file is a development baseline.
- Admin write endpoints require the `Admin` policy.
- Role mutation requires the `SuperAdmin` policy.
- System user must not become externally authenticatable.
- Logging a generated superadmin password is intentional for bootstrap but should be treated as sensitive operational output.

## 11. Extension Guidance

When adding a new feature:

1. Identify the owning module or create a new module.
2. Add contracts under `Contracts/`.
3. Add application service interfaces and implementations in the module.
4. Keep controllers as adapters.
5. Add entities to the module and wire them in `BlogDbContext`.
6. Generate migrations through `dotnet ef`.
7. Update `README.md`, `AGENTS.md`, and architecture docs if behavior or extension rules change.

When adding an external provider:

- Keep provider-specific protocol code behind an interface.
- Normalize provider identity into `UserExternalLogin`.
- Do not duplicate provider accounts across users.
- Keep local-password and external-login flows distinct.

When adding comments or media:

- Reuse `EntityBase`.
- Decide the owning module boundary first.
- Enforce blocked-user write restrictions in services or authorization requirements.
- Keep storage decisions behind module-owned abstractions.
