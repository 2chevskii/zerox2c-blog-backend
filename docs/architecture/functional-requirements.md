# Functional Requirements

Last updated: 2026-05-17

This document describes intended product behavior and explicitly marks what is already implemented. Use it when deciding whether a code change is preserving behavior, completing planned behavior, or intentionally changing product scope.

## 1. Current Implementation Snapshot

Implemented:

- Local user registration with username, email, and password.
- Local login with username or email plus password.
- Steam OpenID login flow.
- JWT access tokens.
- Current-user endpoint.
- User roles: `User`, `Admin`, `SuperAdmin`.
- Admin user listing, role mutation, blocking, and unblocking.
- Known technical users: system and superadmin.
- Public post listing and post details by id or slug.
- Admin post CRUD-like management: list, get, create, update, publish, unpublish, delete.
- Admin tag management: list, get, create, update, delete.
- Admin image upload for cover, banner, and embedded post images.
- Public image retrieval by id.
- Soft delete and audit stamping for entities derived from `EntityBase`.
- Startup migrations and application bootstrap.

Planned but not implemented:

- Google and GitHub external login.
- Comments.
- Informational pages.
- Public tag listing endpoint.
- Sitemap endpoint.
- Health endpoints.
- Integration tests.

## 2. Identity And Authentication

Users can register and sign in.

Local authentication:

- A user can register with username, email, and password.
- A user can log in with username and password.
- A user can log in with email and password.
- Username must be unique.
- Email must be unique.
- Registered local users start with `UserRole.User`.
- Registered local users currently do not require email confirmation before login.

External authentication:

- Steam login is implemented.
- Google login is planned.
- GitHub login is planned.
- External logins are linked to local user records through `UserExternalLogin`.
- A provider account cannot be linked to multiple local users.
- Steam-created local users receive synthetic usernames and emails based on the Steam id.
- External-login users have an empty password hash and should authenticate through their external provider, not through local password login.

Current blocked-user behavior:

- Local blocked users can still log in.
- Existing Steam external-login users are rejected if blocked.
- Write actions should guard blocked users where the feature requires it.
- Whether all blocked users should be rejected at login remains a product decision.

Implementation constraints:

- Authentication and authorization use custom application entities and services, not ASP.NET Core Identity.
- JWT claims are created by `JwtTokenService`.
- Request/auth principal data is normalized into `AuthenticationData`.
- `AuthenticationContextMiddleware` populates the application authentication context for downstream services.

## 3. Roles And Authorization

Supported roles:

- `User`
- `Admin`
- `SuperAdmin`

Rules:

- Every registered user has normal user capabilities.
- `Admin` grants access to content moderation and content management endpoints.
- `SuperAdmin` grants role-management capability and should retain all admin capabilities.
- Role is a single enum-like property on `User`, not a separate role table.
- `SuperAdmin` is still a user role value, not a separate user type.
- Exactly the known system user and known superadmin user should hold the `SuperAdmin` role.
- Admin routes can require `Admin` or `SuperAdmin`.
- Role mutation routes must require `SuperAdmin`.

## 4. Technical Users And Bootstrap

Technical users are defined in code by `KnownUsers`.

System:

- Id: `00000000-0000-0000-0000-000000000001`
- Username: `system`
- Email: `system@internal.local`
- Purpose: code-only internal actor for audited work.
- Role: `SuperAdmin`.
- Login methods: none.
- Usage path: only through `IAuthenticationContextManager.AsSystem()`.

Superadmin:

- Id: `00000000-0000-0000-0000-000000000002`
- Username: `superadmin`
- Email: `superadmin@internal.local`
- Purpose: fixed administrative account.
- Role: `SuperAdmin`.

Bootstrap rules:

- Migrations run first; application bootstrap runs after migrations.
- Bootstrap creates or repairs the system user and superadmin user.
- Technical-user bootstrap must be idempotent.
- Bootstrap must fail if a non-technical user owns a known technical username or email.
- Bootstrap must fail if any user other than `KnownUsers.System` and `KnownUsers.SuperAdmin` has `UserRole.SuperAdmin`.
- The system user must have no password hash and no external logins.
- Superadmin bootstrap uses the fixed superadmin identity.
- `SuperAdmin:UseDefaultPassword` controls initial password behavior when the known superadmin is missing.
- If `UseDefaultPassword` is enabled, use the default password defined in `KnownUsers`.
- If `UseDefaultPassword` is disabled, generate a random password and log it during startup.
- Bootstrap must not reset the known superadmin password after the user already exists.

Important correction:

- Technical users must not be created directly inside EF migration code. Migrations describe schema and deterministic data changes. Technical-user setup belongs in idempotent bootstrap after migrations.

## 5. User Administration

Admins can:

- List users.
- Block users.
- Unblock users.

Superadmins can:

- Change a user's role.
- Change the known superadmin user's password.

Rules:

- A normal admin must not be able to assign or remove superadmin privileges.
- Application services must reject assigning `SuperAdmin` to ordinary users.
- Known technical users cannot be modified through admin user operations.
- The known superadmin password is the only known-user field that can be modified through admin user operations.
- Superadmin users cannot be blocked through admin user operations.
- Blocking records `IsBlocked`, `BlockedAt`, and `BlockedReason`.
- Unblocking clears those fields.

## 6. Blog Posts

Public users can:

- View published post list.
- Search published posts by loose full-text terms across title, subtitle, slug, published article text, and tag metadata.
- Filter published post lists by tag and published date range.
- View published post details by id.
- View published post details by slug.
- Viewing published post details increments the post view count.

Admins can:

- List non-deleted posts.
- Filter posts by status.
- Search posts by loose full-text terms across title, subtitle, slug, and draft article text.
- Create draft posts.
- Update post content and tag assignments.
- Publish posts.
- Unpublish posts.
- Soft-delete posts.

Rules:

- Public endpoints expose only non-deleted posts with `Published` status and non-null `PublishedAt`.
- Public post-list date filters use `from` and `to` query parameters as inclusive calendar dates.
- Admin endpoints expose drafts and published posts but exclude soft-deleted posts.
- New posts start as drafts.
- New posts start with zero likes, dislikes, comments, and views.
- Publishing sets `Status`, `PublishedBy`, and `PublishedAt`.
- Unpublishing returns the post to draft state and clears publish metadata.
- Post slugs are generated from the title when omitted on create or update.
- Generated post slugs must be unique; numeric suffixes are appended when needed.
- Explicitly provided post slugs must be unique and match the slug pattern, otherwise the request fails validation.
- Post tag assignments must refer to active, non-deleted tags.
- Cover image assignments must refer to active images uploaded with `Cover` purpose.
- Banner image assignments must refer to active images uploaded with `Banner` purpose.

## 7. Tags

Admins can:

- List non-deleted tags.
- Search tags by name or description.
- Create tags.
- Update tags.
- Soft-delete tags.

Rules:

- Tags require a unique name.
- Tag names must be lowercase kebab-case and no longer than 20 characters.
- Tags do not have a separate slug field.
- Deleting a tag soft-deletes active post-tag assignments for that tag.

Public tag endpoints are planned but not currently implemented.

## 8. Informational Pages

Planned behavior:

- Public users can view published pages by slug.
- Admins can create, update, publish, unpublish, archive, and delete pages.
- Pages should use the same publication conventions as blog posts where possible.

Current state:

- No page module or page endpoints are implemented.

## 9. Media

Implemented behavior:

- The backend stores image media for internal site content.
- Allowed post image purposes are `Cover`, `Banner`, and `Embedded`.
- Admins upload images through `POST /api/admin/images` as multipart form-data with `file` and `purpose`.
- Public clients retrieve stored images through `GET /api/images/{id}`.
- Images are stored as MySQL blobs with original file name, content type, size, purpose, and audit metadata.
- Upload validation rejects empty files, files larger than 5 MB, and non-image content types outside JPEG, PNG, WebP, and GIF.
- Post cover and banner references are validated against active image rows and expected image purpose.

Planned behavior:

- Allowed use cases include blog post banners, post body images, and comment images.
- Non-image file storage is out of scope for v1.
- Media storage should stay behind a module boundary so MySQL blob storage can later move to object storage.

Current state:

- Cover, banner, and embedded post images are implemented.
- Comment images are planned with comments.

## 10. Comments

Planned behavior:

- Authenticated users can comment on published blog posts.
- Anonymous users cannot comment.
- Blocked users cannot comment.
- Comments belong to an author user and a blog post.
- Public endpoints return only visible comments.
- Admins can hide or restore comments.

Current state:

- No comments module or comment endpoints are implemented.
- Comment threading vs flat comments remains undecided.
