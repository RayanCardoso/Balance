---
name: dotnet-arch-guard
description: Use when reviewing changes to a layered .NET API before committing or merging - checking that Clean Architecture layer boundaries hold, that new use cases and repositories are actually registered in dependency injection, that endpoints document their responses, and that user-owned queries filter by the logged user.
---

# Check architecture rules in a .NET solution

## Overview

Two passes: a script for what is mechanically provable, and a review for what needs judgment.

**Core principle: the compiler does not enforce architecture.** A missing DI registration, an
unscoped query and a leaked entity all build cleanly and fail in production. This skill is the
check that would otherwise never happen.

## When to Use

- Before committing or opening a PR on a layered .NET API
- Right after `dotnet-new-crud-module` or `dotnet-new-usecase`
- "Review this", "did I break the architecture", "check the layers"

**When NOT to use:**
- Hunting a specific bug → `superpowers:systematic-debugging`
- General code quality unrelated to layering → `/code-review`

## Pass 1 — The script

```bash
pwsh -File .claude/skills/dotnet-arch-guard/scripts/check-architecture.ps1 -Root .
```

Exit code 0 means clean, 1 means violations. It checks six things:

| Rule | Detects |
| --- | --- |
| `layering` | A project referencing a layer it must not; `Application` importing `Infrastructure`; a controller touching `DbContext`, `IQueryable` or `SaveChanges` |
| `di-registration` | A `*UseCase` class absent from `AddUseCases`; a repository interface absent from `AddRepositories` |
| `swagger` | A controller with fewer `ProducesResponseType` attributes than HTTP actions |

Report every finding. Do not silence one because it looks harmless — a use case missing from DI
throws on the first request that reaches it, which is usually in front of a user.

## Pass 2 — What the script cannot see

Read the changed files and check these by hand. Each is a real defect class that no static rule
catches reliably.

### Ownership filtering

For every query against a user-owned entity, confirm the predicate includes the logged user:

```csharp
// Correct
.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id)

// Vulnerable - any authenticated caller reads any row
.FirstOrDefaultAsync(x => x.Id == id)
```

Also confirm the *use case* obtained the user from `ILoggedUser.Get()` rather than from
anything in the request payload. A user id accepted from the request body is the caller
choosing whose data to read.

### Entities crossing the API boundary

A controller action or use case returning a `Domain.Entities` type instead of a `Response*Json`
DTO leaks `UserId`, navigation properties, and — for `User` — the password hash. Check what
`Execute` returns, not just what the controller does with it.

### Missing `NotFoundException` after an ownership-filtered lookup

If the filtered query returns `null` and the code proceeds, it will throw a
`NullReferenceException` and surface as a 500 through `ExceptionFilter`. It must be an explicit
`NotFoundException`.

### Distinguishable failure messages

"Not found" and "not yours" must produce the same response. Different messages confirm which
ids exist.

### Tracking on the update path

An update use case whose repository query uses `AsNoTracking()` will commit nothing, silently.
Confirm the `IUpdateOnly` overload does *not* use it, and that the mapper writes onto the
tracked instance (`_mapper.Map(request, entity)`, not `_mapper.Map<Entity>(request)`).

### Untranslated messages

A validation message written as a literal instead of a `ResourceErrorMessages` property will
not localize, and `CultureMiddleware` will appear broken.

## Reporting

Group findings by severity and be concrete about consequence:

1. **Security** — ownership gaps, leaked entities, enumeration oracles
2. **Correctness** — missing DI registrations, silent no-op updates, missing migrations
3. **Consistency** — missing `ProducesResponseType`, hardcoded messages, layering drift

For each finding give the file, the line, and what breaks. If nothing is wrong, say so plainly
rather than inventing minor observations.

## Common Mistakes

| Mistake | Consequence |
| --- | --- |
| Running the script and stopping there | Every security-relevant rule is in pass 2 |
| Treating a DI finding as cosmetic | It is a guaranteed runtime failure on that endpoint |
| Fixing violations without being asked | This skill reports; fixing is a separate decision |
| Judging a file in isolation | Layering is a property of the reference graph, not of one file |

## Related Skills

- `dotnet-new-crud-module` — generates code this skill checks
- `superpowers:requesting-code-review` — broader review beyond architecture
