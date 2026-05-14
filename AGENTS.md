# Agent Instructions

Agents should always follow instructions listed here.

## Coding Rules

- Controllers must not contain business logic.
- Controllers are HTTP adapters only: route binding, authorization attributes, request/response mapping, status-code mapping, and delegation to application services.
- Business rules, validation that depends on application state, persistence decisions, and mutations must live in application/domain services or lower layers.
- Preserve existing behavior unless the user explicitly asks for a behavioral change.
- Prefer clear, maintainable code over clever abstractions.

## Workflow Rules

- Commits must follow Conventional Commits.
- Do not put unrelated changes into the same commit; split them into multiple focused commits.
- Do not create, edit, or delete EF Core migration files manually; use `dotnet ef` commands.
- Do not create projects, solutions, or other .NET metadata files manually; use `dotnet new` and related .NET CLI commands.
