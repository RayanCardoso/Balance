# .NET Clean Architecture skills

Eight skills that scaffold and maintain a layered .NET 10 REST API, derived from the structure
of this repository (CashFlow) and retargeted to .NET 10 + PostgreSQL + Shouldly.

## Order of use

```
New project
   dotnet-clean-arch-bootstrap     solution, layers, DI, Swagger, tests
   dotnet-auth-jwt-module          User, login, JWT, BCrypt, ILoggedUser
   dotnet-ci-workflow              GitHub Actions (once)

Every feature
   dotnet-new-crud-module          new entity, full vertical slice
   dotnet-new-usecase              one more operation on an existing module
   dotnet-usecase-tests            tests in the house style

Before committing
   dotnet-arch-guard               layer rules, DI registrations, ownership filters

When needed
   dotnet-report-export            Excel / PDF download endpoints
```

## Skills

| Skill | Does |
| --- | --- |
| `dotnet-clean-arch-bootstrap` | Creates a compiling 10-project solution from a name. Script builds the skeleton via the `dotnet` CLI; templates supply the C#. |
| `dotnet-auth-jwt-module` | Adds the authentication vertical and the Swagger Authorize button. |
| `dotnet-new-crud-module` | Entity, three repository interfaces, DTOs, five use cases, controller, migration, test builders. |
| `dotnet-new-usecase` | One operation: interface, implementation, validator, DI line, controller action, test. |
| `dotnet-usecase-tests` | xUnit + Shouldly + Moq + Bogus, including the cross-user ownership test. |
| `dotnet-arch-guard` | Script for mechanical rules, checklist for the security-relevant ones. |
| `dotnet-report-export` | Excel/PDF use cases returning `byte[]`, with the font-resolver trap called out. |
| `dotnet-ci-workflow` | Build, test against a PostgreSQL service container, run the architecture check. |

## Stack

.NET 10 · PostgreSQL + EF Core · JWT + BCrypt · AutoMapper · FluentValidation · Swashbuckle
· xUnit + Shouldly + Moq + Bogus

## Scope note

These live in `C:\CashFlow\.claude\skills`, so they only load inside this repository. To use
them when creating projects elsewhere, copy the directory to the personal skills location:

```bash
cp -r C:/CashFlow/.claude/skills/dotnet-* ~/.claude/skills/
```
