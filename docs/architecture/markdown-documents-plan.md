# Markdown Documents Migration Plan

Last updated: 2026-05-16

This plan covers moving Markdown parsing, rendering, preview, and image reference resolution to the backend, while keeping CodeMirror as the editor surface in the admin frontend.

## Research Summary

Current state:

- Post content is stored as `Post.Body` on the `Posts` table.
- Admin create/update DTOs send `body` as a plain string.
- Public post details return raw `body`.
- The admin frontend renders previews with `markdown-it` in `PostEditorView.vue`.
- The public frontend renders articles with `markdown-it` and sanitizes with `DOMPurify`.
- Existing images are stored as PostgreSQL blobs in the `Images` table and streamed from `GET /api/images/{id}`.
- Embedded images currently only use `ImagePurpose.Embedded`; there is no post-owned embedded image table or local Markdown path.

NuGet/package research:

- `dotnet package search markdown` and exact NuGet search identify `Markdig` as the best Markdown parser/renderer candidate for .NET. It has high downloads, is maintained by `xoofx`, is CommonMark-compliant, exposes an AST, and supports direct HTML rendering.
- Context7 Markdig docs confirm:
  - `MarkdownPipelineBuilder` builds reusable immutable pipelines.
  - `Markdown.Parse` returns an AST.
  - `LinkInline` image nodes can be inspected and their `Url` changed before rendering.
  - `.DisableHtml()` prevents raw HTML passthrough, but generated HTML should still be sanitized.
- NuGet search identifies `HtmlSanitizer` as the most established sanitizer package for the generated HTML.
- Context7 CodeMirror docs confirm:
  - CM6 uses `autocompletion({ override: [...] })` for custom completions.
  - `CompletionContext.matchBefore` is the intended way to detect completion ranges.
  - `EditorView.domEventHandlers` is the intended way to handle paste/drop events from an extension.

## Target Design

### Backend owns Markdown documents

Replace direct `Post.Body` storage with explicit Markdown document entities:

- `PostMarkdownDraft`
  - One active draft document per post.
  - Stored in a separate `PostMarkdownDrafts` table.
  - Used by admin editing and preview.
- `PostMarkdownDocument`
  - One published/rendered document per post.
  - Stored in a `PostMarkdownDocuments` table.
  - Used by public post details.
- `MarkdownDocumentContent`
  - Value object owned by both entities.
  - Stores raw Markdown source, backend-rendered sanitized HTML, extracted plain text, and reading minutes.

Publish behavior:

- Create/update saves the current draft document.
- Publish copies the current draft document into the published document table and updates publish metadata.
- Editing a published post updates only the draft until the post is published again.
- Public queries continue to return only non-deleted published posts with `PublishedAt != null`, but they read from `PostMarkdownDocument`.

### Backend owns Markdown rendering

Add a Markdown service:

- Build one reusable Markdig pipeline with advanced useful extensions and `.DisableHtml()`.
- Parse Markdown into an AST.
- Rewrite local image paths before rendering:
  - Supported local path shape: `images/{imageId}` or `./images/{imageId}`.
  - Only post-attached embedded images are resolved.
  - Resolved image URLs become `/api/images/{imageId}`.
- Render HTML with Markdig.
- Sanitize rendered HTML.
- Extract plain text and reading minutes.

Add an admin preview endpoint so the editor preview uses backend HTML instead of a frontend Markdown renderer.

### Post-owned Markdown images

Add `PostMarkdownImage`:

- Links a post to an uploaded embedded image.
- Stores a stable local editor path such as `images/{imageId}`.
- Lets the backend validate and resolve Markdown image references.

Add admin endpoints:

- `GET /api/admin/posts/{postId}/markdown/images`
- `POST /api/admin/posts/{postId}/markdown/images`
- `POST /api/admin/markdown/render`

The image upload endpoint returns the public image URL and the local Markdown path.

### API contract direction

Admin responses expose both:

- `bodyMarkdown`: raw draft Markdown for editing.
- `bodyHtml`: backend-rendered sanitized draft preview.
- `readingMinutes`.

Public post details expose:

- `bodyHtml`: backend-rendered sanitized published document.
- `readingMinutes`.

The public frontend should not render Markdown locally.

### Frontend changes

Admin frontend:

- Remove `markdown-it` preview rendering from `PostEditorView.vue`.
- Call the backend render endpoint with debounce for preview HTML.
- Upload pasted/dropped images through the post Markdown image endpoint.
- Insert Markdown image syntax with the returned local path: `![alt](images/{imageId})`.
- Add CodeMirror completions for attached image local paths using `@codemirror/autocomplete`.
- Keep CodeMirror as the editor; editing remains a plain text Markdown authoring UI.

Public frontend:

- Stop importing and using `markdown-it`.
- Render `bodyHtml` returned by the backend.
- Use backend `readingMinutes`.

## Implementation Order

1. Add backend document/image entities, EF configuration, Markdown services, DTOs, and controllers.
2. Update post services/query services to use draft/published document entities.
3. Generate an EF migration with `dotnet ef`.
4. Update admin frontend DTOs/API wrappers/editor.
5. Update public frontend DTOs/rendering.
6. Build all three projects.

## Delegation Strategy

The work can be parallelized after the backend contracts are sketched:

- Explorer 1: inspect backend post persistence and API contracts.
- Explorer 2: inspect admin frontend editor/API usage.
- Worker 1: backend entities, EF mapping, Markdown rendering service, and migrations.
- Worker 2: admin frontend editor integration once endpoint shapes are known.
- Worker 3: public frontend response/rendering update once public DTOs are known.

Use medium reasoning effort for focused implementation tasks and high only for contract-heavy or migration-heavy review. Do not use extra-high reasoning effort for subagents.
