# Balance

Finance Project with DDD using Spec Driven Development.

REST API in .NET 10 following Clean Architecture / DDD.

## Stack

EF Core + PostgreSQL · JWT + BCrypt · AutoMapper · FluentValidation · Swagger
· xUnit + Shouldly + Moq + Bogus

## Layers

| Project | Responsibility |
| --- | --- |
| `Balance.Api` | Controllers, filters, middleware, DI composition root |
| `Balance.Application` | Use cases, validators, AutoMapper profile |
| `Balance.Domain` | Entities, repository interfaces, domain services |
| `Balance.Infrastructure` | EF Core, repositories, JWT, cryptography |
| `Balance.Communication` | Request/response DTOs |
| `Balance.Exception` | Exception hierarchy and message catalogue |

## Getting started

    docker compose up -d
    dotnet ef migrations add InitialCreate --project src/Balance.Infrastructure --startup-project src/Balance.Api
    dotnet run --project src/Balance.Api

Swagger UI: `https://localhost:<port>/swagger`

## Tests

    dotnet test
