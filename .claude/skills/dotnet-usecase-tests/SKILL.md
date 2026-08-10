---
name: dotnet-usecase-tests
description: Use when a use case, validator or endpoint in a .NET Clean Architecture API has no test coverage, or when covering existing untested application code - writing xUnit tests with Shouldly assertions, Moq repository mocks and Bogus data builders in the UseCases.Test, Validators.Tests and WebApi.Test projects.
---

# Write tests for a use case

## Overview

Produces tests in the established shape for this stack: xUnit + Shouldly + Moq + Bogus, with
mocks and fake data hidden behind builders in `CommonTestUtilities`.

**Core principle: the test names the behaviour, the builder hides the setup.**
A test body that spends ten lines configuring mocks is a builder that has not been written yet.

## When to Use

- Right after `dotnet-new-usecase` or `dotnet-new-crud-module`
- "Cover this use case", "add tests", "this has no tests"
- Before refactoring untested application code

**When NOT to use:**
- Testing `Domain` entities with no dependencies — plain xUnit needs no builders
- The use case does not exist yet — write it first, or use `superpowers:test-driven-development`
  to drive it from the tests

## Which Project

| Testing | Project | Style |
| --- | --- | --- |
| A use case in isolation | `UseCases.Test` | Mocked repositories, no HTTP, no database |
| A `FluentValidation` validator | `Validators.Tests` | Construct the validator directly |
| An endpoint end to end | `WebApi.Test` | `CustomWebApplicationFactory` + in-memory provider |

A use case with a validator gets tests in both of the first two. They cover different things:
the validator test proves the rule, the use case test proves the rule is *invoked*.

## The Shape

```csharp
public class RegisterExpenseUseCaseTest
{
    [Fact]
    public async Task Success()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterExpenseJsonBuilder.Build();
        var useCase = CreateUseCase(loggedUser);

        var result = await useCase.Execute(request);

        result.ShouldNotBeNull();
        result.Title.ShouldBe(request.Title);
    }

    [Fact]
    public async Task Error_Title_Empty()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterExpenseJsonBuilder.Build();
        request.Title = string.Empty;

        var useCase = CreateUseCase(loggedUser);

        var act = async () => await useCase.Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().Count.ShouldBe(1);
        exception.GetErrors().ShouldContain(ResourceErrorMessages.TITLE_REQUIRED);
    }

    private static RegisterExpenseUseCase CreateUseCase(User user)
    {
        var repository = ExpensesWriteOnlyRepositoryBuilder.Build();
        var mapper = MapperBuilder.Build();
        var unitOfWork = UnitOfWorkBuilder.Build();
        var loggedUser = LoggedUserBuilder.Build(user);

        return new RegisterExpenseUseCase(repository, unitOfWork, mapper, loggedUser);
    }
}
```

Three things carry the pattern:

- **`CreateUseCase` private factory.** One place to change when the constructor changes, instead of every `[Fact]`.
- **Builders, not inline mocks.** `UserBuilder`, `RequestXxxJsonBuilder`, `XxxRepositoryBuilder`.
- **Method names read as behaviour.** `Success`, `Error_Title_Empty`, `Error_Expense_Not_Found`.

## Builder Conventions

| Kind | Location | Shape |
| --- | --- | --- |
| Entity | `CommonTestUtilities/Entities/<Entity>Builder.cs` | `static Build()` with a Bogus `Faker<T>` |
| Request | `CommonTestUtilities/Requests/Request<X>JsonBuilder.cs` | `static Build()` with a Bogus `Faker<T>` |
| Read repository | `CommonTestUtilities/Repositories/<Entity>ReadOnlyRepositoryBuilder.cs` | Fluent instance methods returning `this`, then `Build()` |
| Write repository | same folder | `static Build()` — nothing to configure |

Read-side builders are fluent because a test needs to say *what the repository finds*.
Write-side builders are static because there is nothing to arrange.

## Coverage Target

For each use case, one test per branch that can be reached:

- the success path
- each validation rule that can fail (usually delegated to `Validators.Tests`, with **one**
  representative case kept in the use case test to prove validation runs)
- each `NotFoundException`
- each ownership failure: a second user must not reach the first user's entity

The ownership test is the one most often skipped and the one that catches real vulnerabilities.
Build two users, seed the entity against one, execute as the other, expect `NotFoundException`.

## Integration Tests

In `WebApi.Test`, inherit the fixture and use the seeded identities:

```csharp
public class RegisterExpenseTest : __PROJECT_NAME__ClassFixture
{
    private const string METHOD = "api/expenses";

    private readonly string _token;

    public RegisterExpenseTest(CustomWebApplicationFactory factory) : base(factory)
    {
        _token = factory.User_Team_Member.GetToken();
    }

    [Fact]
    public async Task Success()
    {
        var request = RequestRegisterExpenseJsonBuilder.Build();

        var response = await DoPost(METHOD, request, token: _token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Title_Empty(string culture)
    {
        var request = RequestRegisterExpenseJsonBuilder.Build();
        request.Title = string.Empty;

        var response = await DoPost(METHOD, request, token: _token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var errors = body.RootElement.GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            "TITLE_REQUIRED", new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }
}
```

The culture `[Theory]` is what proves `CultureMiddleware` and the `.resx` files are actually
wired — assert against the resource, never against a hardcoded string.

## Common Mistakes

| Mistake | Consequence |
| --- | --- |
| `Assert.Equal` instead of Shouldly | Two assertion dialects in one suite |
| Hardcoding an expected message string | The test breaks on translation instead of on behaviour |
| Inline `new Mock<IXxx>()` in every test | The same setup drifts across files; extract a builder |
| Sharing state between `[Fact]`s | xUnit runs classes in parallel; tests fail depending on order |
| Asserting only `ShouldNotBeNull()` | Passes even when the use case returns the wrong data |
| Omitting the cross-user ownership test | The exact gap that leaks other users' data |
| Reporting "tests written" without running them | Run `dotnet test` and quote the count |

## Related Skills

- `superpowers:test-driven-development` — when tests should come first
- `dotnet-new-usecase` — the code these tests cover
