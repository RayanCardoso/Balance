---
name: dotnet-new-crud-module
description: Use when adding a new entity or resource to an existing .NET Clean Architecture API - generating the full vertical slice across Domain, Communication, Application, Infrastructure and Api for something like Invoice, Product, Category or Order, with repositories, use cases, controller, migration and tests.
---

# Generate a CRUD feature module

## Overview

Creates one complete vertical slice — entity, repositories, DTOs, five use cases, controller,
migration and tests — in the layout established by `dotnet-clean-arch-bootstrap`.

**Core principle: a module is a vertical slice, not a layer.** Everything for `Invoice` is
generated together, top to bottom, so the module is usable the moment it compiles.

## When to Use

- "Add a Product module", "create CRUD for Invoice", "I need a new resource"
- The solution already exists in this layout

**When NOT to use:**
- No solution yet → `dotnet-clean-arch-bootstrap`
- One operation on an entity that already exists → `dotnet-new-usecase`
- A read-only or reference table that needs no endpoints — just add the entity and `DbSet`

## Before Generating: Four Questions

Ask these up front. Each one changes what gets written, and guessing wrong means regenerating
thirty files.

1. **Entity name and properties**, with types and nullability.
2. **Is it user-owned?** Does every row belong to one user, the way an expense does? If yes,
   the entity gets `UserId`/`User` and *every* query filters by `ILoggedUser`. If no (a shared
   catalogue, say), it does not — and the ownership tests do not apply.
3. **Which operations?** Full CRUD is the default; drop the ones that make no sense. An
   audit-log entity has no update or delete.
4. **Role restrictions?** Does any operation need `[Authorize(Roles = ...)]`?

If the user says "just do the usual", assume: user-owned, full CRUD, no role restrictions —
and say so in your report so they can correct you.

## What Gets Generated

For entity `<E>` (singular, PascalCase) with plural `<Es>`:

**Domain**
```
Entities/<E>.cs
Repositories/<Es>/I<Es>ReadOnlyRepository.cs      GetAll, GetById
Repositories/<Es>/I<Es>WriteOnlyRepository.cs     Add, Delete
Repositories/<Es>/I<Es>UpdateOnlyRepository.cs    GetById, Update
```

The three-interface split is deliberate: a use case declares its intent through the interface
it injects. A use case that takes `IReadOnly` provably cannot write. Do not collapse them into
one `IRepository`.

**Communication**
```
Requests/RequestRegister<E>Json.cs      create payload
Requests/Request<E>Json.cs              update payload
Responses/ResponseRegistered<E>Json.cs  create result
Responses/Response<E>Json.cs            full detail
Responses/ResponseShort<E>Json.cs       list item
Responses/Response<Es>Json.cs           list wrapper: List<ResponseShort<E>Json>
```

**Application** — five use cases, each a folder with interface, implementation, and a validator
where there is input to validate:
```
UseCases/<Es>/Register/    Register<E>UseCase    + validator
UseCases/<Es>/GetAll/      GetAll<E>UseCase
UseCases/<Es>/GetById/     Get<E>ByIdUseCase
UseCases/<Es>/Update/      Update<E>UseCase      + validator
UseCases/<Es>/Delete/      Delete<E>UseCase
```

**Infrastructure**
```
DataAccess/Repositories/<Es>/<Es>Repository.cs    implements all three interfaces
```
plus `DbSet<<E>>` on the context and three `AddScoped` lines.

**Api**
```
Controllers/<Es>Controller.cs
```

**Tests**
```
tests/CommonTestUtilities/Entities/<E>Builder.cs
tests/CommonTestUtilities/Requests/RequestRegister<E>JsonBuilder.cs
tests/CommonTestUtilities/Requests/Request<E>JsonBuilder.cs
tests/CommonTestUtilities/Repositories/<Es>ReadOnlyRepositoryBuilder.cs
tests/CommonTestUtilities/Repositories/<Es>WriteOnlyRepositoryBuilder.cs
tests/CommonTestUtilities/Repositories/<Es>UpdateOnlyRepositoryBuilder.cs
tests/UseCases.Test/<Es>/<Operation>/...
tests/Validators.Tests/<Es>/...
tests/WebApi.Test/<Es>/<Operation>/...
```

## Procedure

1. **Ask the four questions.**
2. **Read an existing module** in the solution and match it. If one exists, it outranks the
   templates here.
3. **Write the files** using `references/crud-templates.md`, substituting `__PROJECT_NAME__`,
   `<E>` and `<Es>`.
4. **Register everything.** Three repository lines in `Infrastructure/DependencyInjectionExtension.cs`,
   five use case lines in `Application/DependencyInjectionExtension.cs`, mappings in `AutoMapping`,
   the `DbSet` on the context.
5. **Add validation messages** to both `.resx` files and `ResourceErrorMessages.cs`.
6. **Create the migration:**
   ```bash
   dotnet ef migrations add Add<E> --project src/<Name>.Infrastructure --startup-project src/<Name>.Api
   ```
7. **Extend the integration test factory** if the module is user-owned — seed one row per
   seeded user, exposed through an identity manager, the way `dotnet-auth-jwt-module` does.
8. **Verify:**
   ```bash
   dotnet build
   dotnet test
   ```
9. **Report** the observed build and test output, the endpoints created, and any assumption
   you made about the four questions.

## Ownership Rule

For a user-owned entity this is not optional:

```csharp
// Correct - scoped to the caller
await _dbContext.Invoices
    .AsNoTracking()
    .FirstOrDefaultAsync(invoice => invoice.Id == id && invoice.UserId == user.Id);

// Wrong - any authenticated user reads any row
await _dbContext.Invoices.FirstOrDefaultAsync(invoice => invoice.Id == id);
```

When the filtered query finds nothing, throw `NotFoundException` — never a distinct "forbidden"
message. Telling the caller a row exists but is not theirs confirms the id, which is exactly
what an enumeration attack needs.

Both `GetById` overloads exist for a reason: the `IReadOnly` one uses `AsNoTracking()`, the
`IUpdateOnly` one does not, because EF must track an entity it is about to update. They are
implemented as explicit interface implementations on the same repository class.

## Common Mistakes

| Mistake | Consequence |
| --- | --- |
| Collapsing the three repository interfaces into one | Use cases lose their read/write intent; a read-only use case gains `Delete` |
| `AsNoTracking()` on the update path | `Update` silently persists nothing |
| Forgetting one of the eight DI registrations | Runtime `InvalidOperationException` on first call, not a build error |
| Missing `AutoMapping` entries | AutoMapper throws at first map, not at startup |
| Returning the entity instead of a response DTO | Leaks `UserId` and every navigation property |
| Skipping the migration | Builds and tests pass; the real database has no table |
| Querying without the ownership filter | Cross-user data leak |
| Generating before asking the four questions | Thirty files that need regenerating |

## Related Skills

- `dotnet-new-usecase` — one more operation on this module later
- `dotnet-usecase-tests` — the test files
- `dotnet-arch-guard` — verifies registrations and ownership filters
