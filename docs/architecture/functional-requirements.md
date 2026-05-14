# Functional Requirements

Last updated: 2026-05-14

## 1. Identity And Authentication

Users can register and sign in.

Local authentication:

- A user can register with username, email, and password.
- A user can log in with username and password.
- A user can log in with email and password.
- Username must be unique.
- Email must be unique.

External authentication:

- A user can authenticate through Google.
- A user can authenticate through Steam.
- A user can authenticate through GitHub.
- External logins are linked to a local user account.
- A provider account cannot be linked to multiple local users.

Implementation note:

- Authentication and authorization use custom application entities and services, not ASP.NET Core Identity.
- Google has first-party ASP.NET Core authentication support.
- GitHub and Steam should be treated as provider adapters behind the same external login flow; exact packages can be selected during implementation.

## 2. Roles And Authorization

Supported roles:

- `User`
- `Admin`
- `SuperAdmin`

Rules:

- Every registered user has normal user capabilities.
- `Admin` grants access to content moderation and content management endpoints.
- `SuperAdmin` grants all admin capabilities and application-level administrative control.
- Role is a single enum-like property on `User`, not a separate role table.
- `SuperAdmin` is still a user role value, not a separate user type.
- There must be exactly one superadmin user per application instance.

Superadmin bootstrap:

- Required roles are created during application bootstrap.
- The initial superadmin is created during application bootstrap after migrations are applied.
- Superadmin bootstrap must be idempotent.
- Bootstrap reads initial superadmin data from secure configuration.
- Application services must reject assigning `SuperAdmin` to a second user.
- Startup validation must fail if the database contains zero or more than one superadmin.

Important correction:

- Creating the superadmin directly inside EF migration code is not ideal. Migrations should describe schema and deterministic data changes; user bootstrap depends on secrets and environment-specific credentials. The safer design is: run migrations, then run an idempotent bootstrap step.

## 3. User Blocking

Admins can block and unblock users.

Rules:

- A blocked user cannot create comments.
- A blocked user cannot upload comment images.
- A blocked user cannot perform user-level write actions.
- Blocking does not automatically delete existing comments.
- Whether blocked users can still log in remains an explicit product decision.

Initial default:

- Blocked users may still log in, but write actions check `IsBlocked`.

## 4. Blog

Public users can:

- View published blog posts.
- View blog post details by slug.
- View post tags.
- View visible comments on published posts.

Admins can:

- Create, update, publish, unpublish, and archive posts.
- Assign tags.
- Set a post banner image.
- Embed internal images in the post body.

Rules:

- Public endpoints only expose published posts.
- Draft and archived posts are visible only through admin endpoints.

## 5. Informational Pages

Public users can:

- View published pages by slug.

Admins can:

- Create, update, publish, unpublish, and archive pages.

Rules:

- Public endpoints only expose published pages.
- Pages use the same basic publication conventions as blog posts.

## 6. Media

The backend stores media files as blobs in MySQL for v1.

Supported media:

- Images only.

Allowed usage:

- Blog post banners.
- Images inside blog post bodies.
- Images inside comments.

Rules:

- Media is for internal site content only.
- Non-image files are not supported in v1.
- The API must validate content type and size.
- The `Media` module must hide the physical storage decision from other modules so DB blobs can later be replaced with object storage.

## 7. Comments

Authenticated users can comment on blog posts.

Rules:

- Anonymous users cannot create comments.
- Blocked users cannot create comments.
- Comments belong to an author user.
- Comments belong to a blog post.
- Public endpoints return only visible comments.
- Admins can hide or restore comments.

Open decision:

- Decide whether v1 comments are flat or threaded.
