---
name: dotnet-new-usecase
description: Use when adding a single new operation to a feature module that already exists in a .NET Clean Architecture API - one action such as filter by period, archive, approve, export or search, rather than a whole new entity with full CRUD.
---

# Add one use case to an existing module

## Overview

The fine-grained counterpart to `dotnet-new-crud-module`. Adds exactly one operation to a
module that already exists, touching five files instead of thirty.

**Core principle: one use case, one `Execute`.** If an operation needs a second public method,
it is two use cases.

## When to Use

- "Add an endpoint that filters expenses by period"
- "I need an action to approve an invoice"
- The entity, repository and controller already exist

**When NOT to use:**
- The entity does not exist yet → `dotnet-new-crud-module`
- Only a validation rule is changing → edit the existing validator

## What Gets Created

For a use case `<Verb><Entity>` in module `<Entity>`:

| File | Notes |
| --- | --- |
| `Application/UseCases/<Entity>/<Verb>/I<Verb><Entity>UseCase.cs` | Interface — one `Execute` |
| `Application/UseCases/<Entity>/<Verb>/<Verb><Entity>UseCase.cs` | Implementation |
| `Application/UseCases/<Entity>/<Verb>/<Verb><Entity>Validator.cs` | Only if the operation takes a request body or user-supplied parameter |
| `Application/DependencyInjectionExtension.cs` | One line in `AddUseCases` |
| `Api/Controllers/<Entity>Controller.cs` | One action |
| `tests/UseCases.Test/<Entity>/<Verb>/<Verb><Entity>UseCaseTest.cs` | Success + each failure branch |

Repository interfaces in `Domain` and their implementation in `Infrastructure` are extended
only if the use case needs a query that does not exist yet.

## Procedure

### Step 1 — Read the sibling use case first

Open the nearest existing use case in the same module and match it: constructor injection
order, whether it takes `ILoggedUser`, how it validates, what it returns. Consistency inside a
module matters more than any rule in this file.

### Step 2 — Decide the contract

Answer before writing anything:

- What does `Execute` take, and what does it return? A new response shape means a new DTO in `Communication`.
- Is the data user-owned? If yes, the use case takes `ILoggedUser` and the repository query filters by it.
- Does it write? If yes it needs `IUnitOfWork.Commit()`. If it only reads, it must not take `IUnitOfWork`.
- Which repository interface does the query belong on — `ReadOnly`, `WriteOnly` or `UpdateOnly`?

### Step 3 — Write the files

Follow the module's existing shape. The canonical read-side skeleton:

```csharp
public class FilterExpensesUseCase : IFilterExpensesUseCase
{
    private readonly IExpensesReadOnlyRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILoggedUser _loggedUser;

    public FilterExpensesUseCase(
        IExpensesReadOnlyRepository repository,
        IMapper mapper,
        ILoggedUser loggedUser)
    {
        _repository = repository;
        _mapper = mapper;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseExpensesJson> Execute(RequestFilterJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var result = await _repository.FilterByPeriod(loggedUser, request.Start, request.End);

        return new ResponseExpensesJson
        {
            Expenses = _mapper.Map<List<ResponseShortExpenseJson>>(result)
        };
    }

    private static void Validate(RequestFilterJson request)
    {
        var result = new FilterExpensesValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
```

### Step 4 — Register and expose

Add the `AddScoped` line in `Application/DependencyInjectionExtension.cs`, then add the
controller action with `[FromServices]` for the use case and a `ProducesResponseType` for every
status code the action can actually return.

Forgetting the DI line compiles cleanly and fails at the first request with an
`InvalidOperationException` from the service provider. It is the most common defect here.

### Step 5 — Test and verify

Invoke `dotnet-usecase-tests` for the test file, then:

```bash
dotnet build
dotnet test
```

Report the observed result, not the intended one.

## Common Mistakes

| Mistake | Consequence |
| --- | --- |
| Skipping the DI registration | Runtime failure on the first call, not a build error |
| Injecting `IUnitOfWork` in a read-only use case | Implies a write that never happens; misleads the next reader |
| Querying without the `ILoggedUser` filter | Any authenticated user reads another user's rows |
| Adding a second public method to the use case | It is two use cases — split it |
| A new response shape reusing an unrelated DTO | Couples two endpoints; changing one silently breaks the other |
| Omitting `ProducesResponseType` for the error path | Swagger documents only the happy path |

## Related Skills

- `dotnet-new-crud-module` — when the entity itself is new
- `dotnet-usecase-tests` — the matching unit tests
