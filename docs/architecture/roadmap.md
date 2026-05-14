# Backend Roadmap

Last updated: 2026-05-14

## Phase 0. Architecture Baseline

Goal: agree on the first implementation direction before generating application code.

Tasks:

- Confirm documentation format: `blueprint.md`, `roadmap.md`, `data-schema.md`.
- Confirm runtime target: `.NET 10` and ASP.NET Core.
- Confirm persistence stack: EF Core 9 + Pomelo on `net10.0`.
- Capture functional requirements for users, roles, media, and comments.
- Run a provider spike before final project scaffold.

Done when:

- Provider choice is written down in `blueprint.md`.
- Functional requirements v0 are accepted.
- Data model v0 is accepted.
- Initial API surface is accepted.

## Phase 1. Provider Spike

Goal: validate the chosen EF Core + MySQL stack before committing the codebase shape.

- Target framework: `net10.0`
- EF Core packages: `9.x`
- Provider: `Pomelo.EntityFrameworkCore.MySql 9.x`

Spike checklist:

- Create a minimal ASP.NET Core project.
- Add one `BlogPost` entity and one `Tag` entity.
- Generate an initial migration.
- Apply migration to MySQL in Docker.
- Insert and read sample data.
- Check generated schema for strings, timestamps, GUIDs or ULIDs, indexes, and many-to-many relations.
- Verify rollback or migration removal workflow.

Pass criteria:

- Pomelo works cleanly with EF Core 9 on `net10.0`.
- Migrations generate predictable MySQL schema.
- Basic CRUD works against real MySQL.
- No provider-level blocker appears for the expected content model.

Fallback rule:

- If Pomelo + EF Core 9 has a serious blocker, revisit the provider decision before scaffolding the full project.
- Avoid EF Core 8 unless EF Core 9 has a provider-level blocker that cannot be worked around cleanly.

## Phase 2. Project Scaffold

Goal: create the actual backend skeleton.

Tasks:

- Create solution and API project.
- Add Docker Compose with MySQL.
- Add app configuration and strongly typed options.
- Add EF Core DbContext and migration setup.
- Add health checks.
- Add basic error handling and request logging.

Done when:

- API starts locally.
- MySQL starts locally.
- First migration applies successfully.
- Health endpoint confirms API and database status.

## Phase 3. Identity And Roles

Goal: implement users, authentication, roles, and blocking.

Tasks:

- Add custom user and external-login tables.
- Store role as a `User` enum property.
- Add local registration and login with username/email/password.
- Add external login flow abstraction for Google, Steam, and GitHub.
- Add `User`, `Admin`, and `SuperAdmin` roles.
- Add idempotent bootstrap for roles and the initial superadmin.
- Add startup validation for exactly one superadmin.
- Add user blocking fields and write-action guards.

Done when:

- Users can register and log in locally.
- External provider flow shape is implemented or stubbed behind provider adapters.
- Admin routes can require `Admin` or `SuperAdmin`.
- Superadmin bootstrap is deterministic and does not live inside raw EF migration code.
- Blocked users cannot create comments or upload comment images.

## Phase 4. Content Core

Goal: implement reusable publishing primitives.

Tasks:

- Add content status model: draft, published, archived.
- Add slug handling.
- Add SEO fields.
- Add created/updated/published timestamps.
- Add common pagination response model.

Done when:

- Blog and Pages can reuse the same content conventions without inheritance-heavy design.

## Phase 5. Media

Goal: support internal image storage through MySQL blobs.

Tasks:

- Add media asset table with blob payload.
- Restrict uploads to images.
- Add content type and size validation.
- Add image retrieval endpoint.
- Keep blob persistence hidden behind the `Media` module boundary.

Done when:

- Blog posts can reference banner images.
- Blog post bodies and comments can reference internal images.
- Non-image uploads are rejected.

## Phase 6. Blog

Goal: publish and read blog posts.

Tasks:

- Add posts.
- Add tags.
- Add post-tag relation.
- Add banner image reference.
- Add internal image references in post body format.
- Add public post list and post details endpoints.
- Add admin create/update/publish/unpublish endpoints.

Done when:

- Public API only returns published posts.
- Admin API can manage drafts and published posts.
- Basic integration tests cover persistence and visibility rules.

## Phase 7. Pages

Goal: manage informational pages.

Tasks:

- Add pages with slug and content body.
- Add public page-by-slug endpoint.
- Add admin page management endpoints.

Done when:

- Frontend can render named pages from backend content.

## Phase 8. Comments

Goal: allow authenticated, non-blocked users to comment on posts.

Tasks:

- Add comments table.
- Add optional comment-image relation.
- Add public comments endpoint for posts.
- Add authenticated create-comment endpoint.
- Add authorization check that rejects blocked users.
- Add admin hide/restore endpoints.

Done when:

- Anonymous users cannot create comments.
- Blocked users cannot create comments.
- Public users see only visible comments.
- Admins can hide and restore comments.

## Phase 9. Admin

Goal: complete admin-facing moderation and management endpoints.

Tasks:

- Add user list endpoint.
- Add role management endpoint.
- Add block/unblock endpoints.
- Add moderation views for comments and media.
- Add rate limiting for auth-sensitive endpoints.

Done when:

- Public endpoints remain anonymous.
- Admin endpoints reject unauthenticated requests.
- Superadmin-only operations reject normal admins.

## Phase 10. Production Readiness

Goal: prepare deployment.

Tasks:

- Add production configuration notes.
- Add database migration command/workflow.
- Add backup notes for MySQL and media.
- Add structured logging configuration.
- Add basic CI build and test pipeline.

Done when:

- The service can be deployed and restored with documented steps.
