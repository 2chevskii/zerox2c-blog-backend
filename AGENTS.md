# Agent Instructions

Agents should always follow this file. It is the operational guide for making productive follow-up changes in this repository.

## Project Snapshot

This is the backend for the 0x2c.dev personal website. It is an ASP.NET Core modular monolith with EF Core/PostgreSQL persistence, custom user authentication, admin content-management APIs, public blog-post read APIs, startup bootstrap, and audited entity writes.

The current application project is `src/ZeroX2C.Blog.API/ZeroX2C.Blog.API.csproj`. The solution entry point is `ZeroX2C.Blog.slnx`.

## Related Repositories

This repository is part of the local 0x2c.dev blog workspace:

- `../blog-backend` - ASP.NET Core backend API and persistence.
- `../blog-frontend` - public Vue frontend for readers.
- `../blog-admin-frontend` - Vue admin frontend for content management.

Agents may inspect and modify any of these three sibling repositories when a task requires coordinated backend, public frontend, or admin frontend changes. Keep commits focused per repository and do not mix unrelated work.

Read these docs before making broad changes:

- `README.md` for setup and current API surface.
- `docs/architecture/functional-requirements.md` for product behavior.
- `docs/architecture/blueprint.md` for architecture and extension rules.
- `docs/architecture/data-schema.md` for persistence shape.
- `docs/architecture/roadmap.md` for current gaps and likely next work.

## Architecture Rules

- Keep the codebase as a modular monolith unless the user explicitly asks for project splitting.
- Place feature/domain code under `Modules/<Feature>/`.
- Prefer feature-first organization over technical-layer organization for application code.
- Inside a module:
  - Put HTTP endpoints in `Controllers/`.
  - Put request/response DTOs in `Contracts/`.
  - Put admin-only use cases in `Admin/`.
  - Put authentication-specific code in `Auth/`.
  - Keep feature-owned entities/services directly under the module folder when a deeper folder does not reduce scanning cost.
- Place reusable application infrastructure under `CrossCutting/`.
- Place persistence infrastructure under `Persistence/`.
- Place generic shared domain primitives under `Modules/Shared/`.
- Place small framework/configuration helpers under `Utility/`.
- Do not create catch-all root folders such as `Services`, `Models`, or `Helpers` for feature-owned code.
- Source files must not contain more than one root-level type. Nested types are allowed.

## Controller And Service Rules

- Controllers must not contain business logic.
- Controllers are HTTP adapters only: route binding, authorization attributes, request/response mapping, status-code mapping, and delegation to application services.
- Business rules, validation that depends on application state, persistence decisions, and mutations must live in application/domain services or lower layers.
- Preserve the existing operation-result pattern for application services. Services return explicit status values; controllers map those statuses to HTTP responses.
- Keep request DTO validation attributes at the API boundary, but do not rely on attributes for stateful validation such as uniqueness, ownership, publication state, or role rules.

## Persistence Rules

- Database entities derived from `EntityBase` are audited by `EntityAuditSaveChangesInterceptor`.
- Saving audited entities requires an authenticated `IAuthenticationContext`.
- Use `IAuthenticationContextManager.AsSystem()` only for code-owned technical work such as startup bootstrap.
- Generated database entity IDs must use `Guid.CreateVersion7()` so IDs remain sortable by creation time.
- Technical users are the only exception to generated GUID v7 IDs.
- Do not manually create, edit, or delete EF Core migration files. Use `dotnet ef` commands.
- Do not use `EnsureCreated` for application behavior. Startup applies migrations through `Database.MigrateAsync()`.
- Preserve soft-delete behavior for `EntityBase` entities: `Remove` is intercepted and converted to an audited soft delete.

## Technical Users

Known technical users live in `Modules/Users/KnownUsers.cs`.

- System:
  - Id: `00000000-0000-0000-0000-000000000001`
  - Username: `system`
  - Purpose: code-only actor for internal execution contexts.
- Superadmin:
  - Id: `00000000-0000-0000-0000-000000000002`
  - Username: `superadmin`
  - Purpose: fixed administrative account created during bootstrap.

Rules:

- Do not add password, external-login, token, or controller login paths for the system user.
- Do not replace fixed technical-user IDs with generated IDs.
- Do not let normal registration or admin user operations reassign known technical usernames or emails to ordinary users.
- Superadmin bootstrap must use `KnownUsers.SuperAdmin`.
- `SuperAdmin:UseDefaultPassword` controls initial startup password behavior for a missing superadmin. If it is enabled, use the default password from `KnownUsers`; otherwise generate a random password and log it on startup.
- If changing role-management behavior, preserve the invariant that exactly the known system and known superadmin users can hold `UserRole.SuperAdmin`.
- Admin user operations must not modify known technical users, except changing the known superadmin password.

## Authentication And Authorization

- Authentication uses custom application entities, not ASP.NET Core Identity.
- JWT bearer auth is configured in `Program.cs`.
- The authentication context is populated by `AuthenticationContextMiddleware` after `UseAuthentication()` and before `UseAuthorization()`.
- Current authorization policy names live in `AuthorizationPolicyNames`.
- Admin content endpoints require the `Admin` policy.
- Role mutation requires the `SuperAdmin` policy.
- Blocked users can currently authenticate; write guards should use authorization policies or service-level checks depending on the feature.
- Steam login is implemented. Google and GitHub are planned but not implemented.

## Posts And Tags

- Public post queries return only non-deleted posts with `PostStatus.Published` and a non-null `PublishedAt`.
- Admin post queries include drafts and published posts but exclude soft-deleted posts.
- Post slugs are stored on posts. If an admin create/update request omits a slug, generate one from the title and append numeric suffixes to avoid collisions. If a slug is explicitly provided, duplicate or invalid slugs must fail validation.
- Tags do not have separate slugs. Tag `Name` is the stable kebab-case identifier and must be unique, lowercase kebab-case, and at most 20 characters.
- Slugs use lowercase ASCII letters, digits, and hyphen-separated segments.
- Post/tag mutations belong in `Modules/Posts/Admin/`, not controllers.
- Keep tag assignment logic in the post application service unless it becomes shared by multiple modules.

## Configuration And Startup

- `Program.cs` is the composition root.
- Required config:
  - `ConnectionStrings:PostgreSql`
  - `Jwt:Issuer`
  - `Jwt:Audience`
  - `Jwt:SigningKey` with at least 32 UTF-8 bytes
  - `Jwt:AccessTokenLifetimeMinutes`
  - `SuperAdmin:UseDefaultPassword`
- Startup order matters:
  - Register services.
  - Configure authentication and authorization.
  - Build middleware pipeline.
  - Apply migrations.
  - Run `ApplicationBootstrapper`.
  - Run the app.
- Do not leave API processes, frontend dev servers, preview servers, watchers, or other long-running development processes running after local verification. Stop anything you started before finishing unless the user explicitly asks to keep it running.

## Workflow Rules

- Preserve existing behavior unless the user explicitly asks for a behavioral change.
- Prefer clear, maintainable code over clever abstractions.
- Use the repo's existing patterns before introducing new abstractions.
- Commits must follow Conventional Commits.
- Do not put unrelated changes into the same commit; split them into multiple focused commits.
- Do not create projects, solutions, or other .NET metadata files manually; use `dotnet new` and related .NET CLI commands.
- Before changing generated files or migrations, confirm the correct CLI workflow.
- If the user asks a question or asks for thoughts, answer with analysis and do not modify files unless explicitly requested.
