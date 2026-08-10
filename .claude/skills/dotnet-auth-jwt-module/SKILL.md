---
name: dotnet-auth-jwt-module
description: Use when a .NET Clean Architecture API needs user accounts, sign-up, login or protected endpoints - adding JWT bearer tokens, BCrypt password hashing, a logged-user service, roles, and the Swagger Authorize button to a solution that currently has no authentication.
---

# Add JWT authentication to a .NET Clean Architecture API

## Overview

Adds the whole authentication vertical: a `User` entity, register and login use cases, JWT
issuing and validation, BCrypt hashing, and an `ILoggedUser` service that every other use case
consumes to scope data to the caller.

**Core principle: the token carries identity, the use case enforces ownership.**
`[Authorize]` only proves the caller is *someone*. Every query must additionally filter by
`ILoggedUser.Get()` — otherwise any authenticated user can read any other user's rows.

## When to Use

- Right after `dotnet-clean-arch-bootstrap`
- "Add login", "protect these endpoints", "add users", "add JWT"
- An existing solution in this layout where `Domain/Entities/User.cs` does not exist

**When NOT to use:**
- Authentication already exists → edit it directly
- The user wants an external identity provider (Entra ID, Auth0, Keycloak) — this skill issues
  its own tokens and does not federate

## Procedure

### Step 1 — Confirm the prerequisites

The solution must already have the layer layout, `ExceptionFilter`, `ResourceErrorMessages`
and `IUnitOfWork` from `dotnet-clean-arch-bootstrap`. If `Domain/Entities/User.cs` already
exists, stop and ask the user before overwriting anything.

Confirm these packages are present, and add whichever are missing:

```bash
dotnet add src/<Name>.Infrastructure package BCrypt.Net-Next
dotnet add src/<Name>.Infrastructure package System.IdentityModel.Tokens.Jwt
dotnet add src/<Name>.Api package Microsoft.AspNetCore.Authentication.JwtBearer
```

### Step 2 — Write the files

Read `references/auth-files.md` and apply it in order. It contains both new files and
targeted edits to existing ones (`Program.cs`, both `DependencyInjectionExtension.cs`, the
`DbContext`, `AutoMapping`). Apply the edits — do not rewrite those files from scratch or you
will drop whatever feature modules already registered themselves there.

`CommonTestUtilities` gains a reference to `Infrastructure` for `JwtTokenGeneratorBuilder`:

```bash
dotnet add tests/CommonTestUtilities reference src/<Name>.Infrastructure
```

### Step 3 — Add the messages

Seven keys go into **both** `.resx` files plus a property each in `ResourceErrorMessages.cs`.
The table is in `references/auth-files.md`. A missing key does not fail the build — it
silently returns the key name at runtime, so verify one localized message actually resolves.

### Step 4 — Migration

```bash
dotnet ef migrations add AddUser --project src/<Name>.Infrastructure --startup-project src/<Name>.Api
```

If this is the first migration in the solution, name it `InitialCreate` instead.

### Step 5 — Verify

```bash
dotnet build
dotnet test
```

Then confirm by hand, and report what you actually saw:
- `POST /api/user` returns 201 with a token
- `POST /api/login` with those credentials returns 200
- `POST /api/login` with a wrong password returns 401
- a protected endpoint without a token returns 401

## Security Rules

These are requirements, not preferences. Each one has bitten real APIs built on this layout.

| Rule | Why |
| --- | --- |
| Never return `User.Password`, even hashed | Response DTOs are the boundary; an entity returned directly leaks the hash |
| Login failures use one message for both branches | Different messages for "no such e-mail" and "wrong password" make the endpoint an account-enumeration oracle |
| The token carries `UserIdentifier` (Guid), never `Id` | A sequential id in a token leaks row counts and invites enumeration |
| Every user-owned query filters by `ILoggedUser.Get()` | `[Authorize]` authenticates; it does not authorize. Without the filter, any logged-in user reads everyone's data |
| `SigningKey` comes from configuration, never a literal | A committed key is a permanent forge-any-token capability |
| `SigningKey` is at least 32 bytes | HMAC-SHA256 throws below that, at runtime, on the first login |

## Common Mistakes

| Mistake | Consequence |
| --- | --- |
| `app.UseAuthorization()` before `app.UseAuthentication()` | Every request is anonymous; `[Authorize]` rejects valid tokens |
| Forgetting `AddHttpContextAccessor()` | `HttpContextTokenValue` gets a null `HttpContext` and throws on the first protected call |
| Naming the BCrypt wrapper `BCrypt` | Collides with the `BCrypt.Net` namespace and forces fully-qualified names everywhere |
| Rewriting `Program.cs` wholesale | Drops the `ExceptionFilter` and `CultureMiddleware` registrations |
| Trusting the e-mail uniqueness check alone | It is a read-then-write race; the unique index on `Email` is the real guarantee |
| Skipping the `Users` `DbSet` | `LoggedUser` fails to compile, and the migration silently omits the table |

## Related Skills

- `dotnet-clean-arch-bootstrap` — creates the base this skill extends
- `dotnet-new-crud-module` — generates modules that consume `ILoggedUser`
- `dotnet-arch-guard` — flags user-owned queries missing the ownership filter
