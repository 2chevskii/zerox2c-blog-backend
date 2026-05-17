---
name: commit
description: Commit worktree changes when the user asks to commit, save progress, commit current repository changes, or commit changes across the 0x2c.dev blog repositories. Use this skill for Git commit workflows in blog-frontend, blog-backend, and blog-admin-frontend, including splitting commits by related work and using Conventional Commits.
---

# Commit workflow

When the user asks to commit changes, do not ask for confirmation unless the requested scope is ambiguous or a destructive action would be required. Commit the requested worktree changes directly.

## Scope rules

- "Commit changes in current repository" means operate only in the current working directory's Git repository.
- "Commit changes in this repository" means operate only in the current working directory's Git repository.
- "Commit changes in all three repositories" means operate in all three sibling repositories of the 0x2c.dev blog workspace:
  - `../blog-frontend`
  - `../blog-backend`
  - `../blog-admin-frontend`
- "Commit changes in all repos" or "commit everything" means all three sibling repositories when the current workspace is one of those three repositories.

## Commit rules

- Inspect `git status --short` before staging in each target repository.
- Include all current changes in the requested scope unless the user explicitly says to commit only a subset.
- Split commits by related work when multiple unrelated feature areas are present.
- Keep commits repository-local; never create one commit that spans multiple repositories.
- Use Conventional Commits, for example `feat: ...`, `fix: ...`, `docs: ...`, `refactor: ...`, `chore: ...`.
- Prefer concise commit subjects that describe the related work, not the file list.
- Use non-interactive Git commands only.
- Do not amend, rebase, reset, stash, or discard changes unless the user explicitly asks.
- If a repository has no changes, report that no commit was needed for that repository.

## Practical workflow

1. Determine target repository or repositories from the user's wording.
2. Run `git status --short` in each target repository.
3. Group changes by related work using file paths and change intent.
4. Stage each group with explicit paths.
5. Commit each group with a Conventional Commit message.
6. Run `git status --short` after committing and report commit hashes plus any remaining uncommitted files.