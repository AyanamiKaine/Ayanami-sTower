## Purpose

This document is for automated coding agents working in this monorepo. It explains the repo shape, essential conventions, and the expected workflow to make safe, high‑quality changes that pass CI and fit existing practices.

## Monorepo overview

-   .NET/C# first. The root `AyanamisTower.sln` aggregates many apps, libs, and examples.
-   Central MSBuild props are enforced:
    -   `Directory.Build.props`: Nullable enabled, latest C# lang version, analyzers on, warnings as errors, XML docs required, unified bin/obj.
    -   `Directory.Packages.props`: Central Package Management (CPM). Add or update dependencies here, not in individual `.csproj` files.
-   Technologies present: ASP.NET Core, Avalonia, SDL/graphics, ECS, scripting, tests (xUnit and MSTest).

## Agent workflow (follow this checklist)

1. Understand the task

    - Extract explicit requirements into a short checklist in your response.
    - Identify the correct project(s) and files by searching the solution and folders; avoid creating duplicate implementations.

2. Find the right place

    - Prefer editing an existing project that already owns the domain (e.g., Web API code under `WebAPI/`, UI under `Avalonia/` or specific app folders, ECS under its library).
    - Don’t mix concerns (keep REST out of core libraries, UI out of services, etc.).

3. Design small, verifiable changes

    - Keep patches minimal and focused; avoid reformatting unrelated code.
    - Maintain public APIs unless the change requires it; if modified, update all usages and docs.

4. Tests first (or with the change)

    - Add unit tests for new features and bug fixes.
    - Prefer the test framework already used by that project (xUnit or MSTest are both present). Create or extend the matching `Tests/` project.
    - Include at least: happy path, one edge case, and a regression test if fixing a bug.

5. Dependencies

    - Use central package management: add versions in `Directory.Packages.props` (PackageVersion or GlobalPackageReference).
    - Reference packages in the local `.csproj` without versions.
    - Choose popular, actively maintained libraries; avoid niche or heavy dependencies without strong justification.

6. Quality gates (green before done)

    - Build the solution.
    - Fix all analyzer warnings (warnings are errors).
    - Run unit tests; ensure they pass.
    - For ASP.NET projects, add/verify minimal API wiring and Swagger docs when adding endpoints.

7. Documentation and samples

    - Update XML docs on public members (required by repo settings).
    - If behavior changes, update README or relevant docs in the affected project.
    - Add a minimal usage example or test demonstrating new API surface when helpful.

8. Commits/PR hygiene
    - One logical change per commit. Clear messages: what changed and why.
    - Include a brief summary of tests added/updated.
    - Avoid committing large binaries or secrets (respect `.gitignore`, use environment variables or development secrets for local values).

## Coding conventions

-   C# latest features allowed; keep style consistent with nearby code.
-   Nullable reference types enabled: annotate and handle nulls explicitly.
-   XML docs required for public APIs; include summaries and param/returns.
-   Prefer small, single‑purpose methods and clear names.
-   Avoid allocations or reflection in tight loops/hot paths; measure when optimizing.
-   Logging: if adding logging to .NET services, use `Microsoft.Extensions.Logging` via DI; keep logs structured and at appropriate levels.

## Testing guidance

-   Put tests near peers in `Tests/` folders or the project’s existing test project.
-   Arrange/Act/Assert; keep tests deterministic and isolated.
-   Use fakes/mocks where external dependencies exist; don’t hit network or disk unless the project already does so in integration tests.
-   If adding APIs/endpoints, cover:
    -   Success response shape
    -   Validation/authorization failures (if applicable)
    -   Boundary conditions (empty, large inputs)

## Web/API specifics

-   Minimal APIs or controllers should validate inputs and return typed results.
-   Keep core libraries free of transport concerns (no REST/HTTP types in domain libs). Use extension methods/adapters in web projects for mapping.
-   When serializing enums to clients, prefer string representation to improve readability and forward compatibility.
-   Document new endpoints in Swagger/XML docs; include example payloads when useful.

## UI specifics (Avalonia and others)

-   Keep UI state and logic separated; prefer MVVM patterns where present.
-   Don’t block the UI thread; use async APIs appropriately.
-   If adding components, include a minimal usage sample and wire into an existing demo page or app when feasible.

## Performance and safety

-   Treat warnings as errors: fix analyzer feedback rather than disabling it.
-   Avoid global behavioral changes (e.g., altering serializer defaults) unless scoped to the project needing it.
-   Respect cross‑platform support; avoid Windows‑only APIs in libraries unless clearly platform‑specific.

## Do/Don’t quick list

Do:

-   Add tests with new features and bug fixes.
-   Keep changes minimal, readable, and documented.
-   Use central package management and repository conventions.
-   Run build and tests before concluding a task.

Don’t:

-   Reformat large files unrelated to the change.
-   Introduce transport/protocol types into core libraries.
-   Bypass analyzers or suppress warnings without justification.
-   Commit secrets or large binaries.

## When in doubt

Prefer smallest viable change, follow existing patterns in the nearest project, and document assumptions briefly in your PR/commit description.
