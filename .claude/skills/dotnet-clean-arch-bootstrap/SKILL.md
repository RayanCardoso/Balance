---
name: dotnet-clean-arch-bootstrap
description: Use when starting a brand-new .NET REST API from scratch and the solution does not exist yet - creating the layered project skeleton, solution file, DI wiring, exception handling, Swagger and test projects for a Clean Architecture / DDD backend on .NET 10 with PostgreSQL.
---

# Bootstrap a .NET Clean Architecture API

## Overview

Creates a complete, compiling .NET 10 solution laid out in six source layers and four test
projects, ready to receive feature modules.

**Core principle: the skeleton comes from the `dotnet` CLI, the code comes from templates.**
The CLI owns project creation, references and package resolution — it never guesses a version
number. The templates own the C# — they are copied, not invented.

## When to Use

- "Create a new API", "start a new project", "set up a new backend"
- An empty directory, or a repo with no `src/`
- The stack is .NET + REST + EF Core + PostgreSQL

**When NOT to use:**
- The solution already exists → use `dotnet-new-crud-module` to add features
- Adding auth to an existing solution → use `dotnet-auth-jwt-module`
- The user wants a non-layered / minimal-API service — this skill imposes a heavy structure

## Target Stack

| Concern | Choice |
| --- | --- |
| Framework | .NET 10 (`net10.0`) |
| Database | PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Docs | Swashbuckle (`AddSwaggerGen`) — the template's `Microsoft.AspNetCore.OpenApi` is removed |
| Mapping | AutoMapper |
| Validation | FluentValidation |
| Auth | JWT + BCrypt (added by `dotnet-auth-jwt-module`) |
| Tests | xUnit + Shouldly + Moq + Bogus |

## Layer Rules

These are structural, not stylistic. `dotnet-arch-guard` enforces them.

```
Domain          -> (nothing)
Exception       -> (nothing)
Communication   -> (nothing)
Infrastructure  -> Domain
Application     -> Domain, Communication, Exception
Api             -> Application, Infrastructure, Communication, Exception
```

`Application` must **not** reference `Infrastructure`. Use cases depend on the repository
interfaces in `Domain`; `Api` is the only project that wires implementations.

## Procedure

### Step 1 — Get the project name

Ask the user for the solution name if it was not given. It must be PascalCase, letters and
digits only (`Billing`, `CashFlow`, `OrderApi`). This becomes the namespace root, the
assembly prefix, and the `__PROJECT_NAME__` substitution.

### Step 2 — Run the scaffolder

```bash
pwsh -File .claude/skills/dotnet-clean-arch-bootstrap/scripts/scaffold.ps1 -Name <ProjectName> -Path <target-dir>
```

It creates the projects, wires the references, adds packages, and stops. It writes no C#.
It refuses to run if `src/` already exists.

Add `-SolutionFormat slnx` only if the user asked for the new XML solution format
(needs Visual Studio 17.13+). The default is classic `.sln`.

### Step 3 — Write the source files

Read `references/base-files.md` and write **every** file in it, replacing `__PROJECT_NAME__`
with the project name in both paths and file contents.

Do not skip files that look optional. `appsettings.Test.json` and the `public partial class
Program { }` line are what make the integration test project boot — omitting them produces a
solution that compiles and then fails at test time.

### Step 4 — Verify it compiles

```bash
dotnet build
```

Fix any error before continuing. Do not report success without a clean build — see
`superpowers:verification-before-completion`.

### Step 5 — Verify the tests run

```bash
dotnet test
```

Zero tests is the expected result at this point. What matters is that all four test projects
build and the runner starts.

### Step 6 — Add authentication

The base has no `User` entity, no login and no `[Authorize]`. Invoke `dotnet-auth-jwt-module`
to add them. Skip only if the user explicitly wants an anonymous API.

### Step 7 — Report

Tell the user, concretely:
- the build and test results actually observed
- that `docker compose up -d` starts PostgreSQL
- that no migration exists yet — the first one is created by the first feature module
- that `SigningKey` in `appsettings.Development.json` is a placeholder to replace

## Quick Reference

| File | Why it exists |
| --- | --- |
| `Exception/ResourceErrorMessages.cs` | Hand-written `ResourceManager` wrapper — the VS `.Designer.cs` is absent on a CLI build |
| `Infrastructure/Extensions/ConfigurationExtensions.cs` | `IsTestEnvironment()` gates the Npgsql registration so tests can swap providers |
| `Api/Filters/ExceptionFilter.cs` | Maps the exception hierarchy to status codes; controllers never try/catch |
| `Api/Middleware/CultureMiddleware.cs` | Reads `Accept-Language` so `.resx` messages localize per request |
| `Api/appsettings.Test.json` | `InMemoryTest: true` — without it `WebApi.Test` tries to reach a real database |
| `WebApi.Test/CustomWebApplicationFactory.cs` | Boots the real API against the in-memory provider |

## Common Mistakes

| Mistake | Consequence |
| --- | --- |
| Pinning package versions in the script | Versions rot and stop resolving; let NuGet pick for `net10.0` |
| Letting `Application` reference `Infrastructure` | Use cases start touching `DbContext` and the layering collapses |
| Dropping `public partial class Program { }` | `WebApplicationFactory<Program>` cannot find an entry point |
| Keeping `Microsoft.AspNetCore.OpenApi` alongside Swashbuckle | Two documentation pipelines describing the same API |
| Generating a `.Designer.cs` for the `.resx` by hand | Drifts from the `.resx` silently; use the `ResourceManager` wrapper |
| Reporting success without running `dotnet build` | The most common failure mode of this skill |

## Related Skills

- `dotnet-auth-jwt-module` — the expected next step
- `dotnet-new-crud-module` — the first feature module
- `dotnet-arch-guard` — verifies the layer rules above still hold
