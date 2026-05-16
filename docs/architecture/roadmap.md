# Backend Roadmap

Last updated: 2026-05-16

This roadmap tracks implementation progress at project scale. It is intentionally practical: use it to choose the next coherent change set, avoid duplicating completed work, and keep unrelated work out of the same commit.

## Status Legend

- Done: implemented enough for current scope.
- Partial: meaningful code exists, but important behavior or verification remains.
- Planned: documented but not implemented.

## Phase 0. Architecture Baseline

Status: Done.

Completed:

- Runtime target selected: .NET 10.
- Persistence stack selected: EF Core 9.x with Pomelo MySQL provider.
- Architecture docs created under `docs/architecture`.
- Modular monolith direction selected.
- Custom identity model selected instead of ASP.NET Core Identity.

Follow-up:

- Keep docs current when code changes behavior or module boundaries.

## Phase 1. Project Scaffold

Status: Done.

Completed:

- Solution and API project exist.
- Central package management exists.
- Docker Compose includes local MySQL.
- `Program.cs` configures controllers, OpenAPI/Scalar, authentication, authorization, EF Core, and bootstrap.
- `BlogDbContext` and migrations exist.
- Startup applies migrations.

Still needed:

- Health checks.
- Standardized error handling beyond controller-level status mapping.
- Request logging policy.

## Phase 2. Persistence And Auditing

Status: Partial.

Completed:

- `EntityBase` defines common audit and soft-delete fields.
- EF Core save interceptor applies created/updated/deleted audit stamps.
- Deletes are converted to soft deletes for `EntityBase` entities.
- `TimeProvider.System` is registered for audit timestamps.
- Save operations require an authenticated application context.

Still needed:

- Tests for audit behavior.
- Clear query conventions or helpers for consistently excluding soft-deleted rows.
- Decision on `DateTime` vs `DateTimeOffset` consistency.

## Phase 3. Identity, Auth, Roles, And Technical Users

Status: Partial.

Completed:

- `User` and `UserExternalLogin` entities exist.
- Local registration exists.
- Local login by username/email exists.
- Steam login exists.
- JWT token creation exists.
- Authentication context middleware exists.
- Roles exist: `User`, `Admin`, `SuperAdmin`.
- Admin and superadmin authorization policies exist.
- Technical users are defined in `KnownUsers`.
- Startup bootstrap creates/repairs system and superadmin users.
- Superadmin password behavior is controlled by `SuperAdmin:UseDefaultPassword`.
- Admin user list, block, unblock, and role mutation endpoints exist.

Still needed:

- Google external login.
- GitHub external login.
- Consistent decision on whether blocked users may log in.
- Rate limiting for auth-sensitive endpoints.
- Tests for auth, bootstrap, role rules, and blocked-user behavior.
- Tests for the invariant that only `KnownUsers.System` and `KnownUsers.SuperAdmin` can hold `SuperAdmin`.

## Phase 4. Posts And Tags

Status: Partial.

Completed:

- `Post`, `Tag`, and `PostTag` entities exist.
- Post statuses exist.
- Slug helpers exist for posts, and tag name rules exist for tags.
- Public published-post list and details endpoints exist.
- Admin post list/get/create/update/publish/unpublish/delete endpoints exist.
- Admin tag list/get/create/update/delete endpoints exist.
- Post/tag assignment is implemented.
- Public queries hide draft and soft-deleted posts.
- Admin queries hide soft-deleted posts.

Still needed:

- Public tag list/detail endpoints if frontend needs them.
- Archive behavior beyond the enum value.
- SEO fields if still required.
- Banner/cover image relationship enforcement after media is implemented.
- Tests for visibility, slug uniqueness, tag assignment, publish/unpublish, and soft delete.

## Phase 5. Media

Status: Planned.

Completed:

- Placeholder `Image` entity exists.

Still needed:

- Decide storage shape for v1: MySQL blob vs immediate object storage abstraction.
- Add image metadata fields.
- Add upload endpoint.
- Add retrieval endpoint.
- Validate content type and size.
- Add post banner/body-image integration.
- Add comment image integration after comments exist.

## Phase 6. Pages

Status: Planned.

Still needed:

- Add page entity.
- Add publication state and slug handling.
- Add public page-by-slug endpoint.
- Add admin page management endpoints.
- Decide whether pages reuse post DTO/service patterns or get a separate module shape.

## Phase 7. Comments

Status: Planned.

Still needed:

- Decide flat vs threaded comments.
- Add comment entity.
- Add public comments endpoint for posts.
- Add authenticated create-comment endpoint.
- Reject blocked users from comment creation.
- Add admin hide/restore endpoints.
- Add optional image attachment support after media exists.

## Phase 8. Production Readiness

Status: Planned.

Still needed:

- Add tests.
- Add CI build/test workflow.
- Add production configuration notes.
- Add deployment/migration workflow.
- Add MySQL backup and restore notes.
- Add structured logging conventions.
- Add health checks.
- Add observability guidance.

## Recommended Next Work

Near-term, highest leverage:

1. Add tests around authentication/bootstrap/auditing because those are cross-cutting and easy to regress.
2. Harden admin user role rules around known superadmin invariants.
3. Add public tag endpoints if the frontend needs tag navigation.
4. Implement health checks and basic production readiness docs.
5. Start the media module only after deciding whether v1 truly stores blobs in MySQL.

## Work-Splitting Guidance

Keep changes focused:

- Do not mix auth hardening with post/tag feature work.
- Do not mix schema migrations with unrelated refactors.
- Do not add a new module and production deployment work in the same commit.
- Update docs in the same change set when behavior or architecture changes.
