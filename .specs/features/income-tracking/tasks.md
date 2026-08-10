# Income Tracking Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute
flow and Critical Rules.** Do not search for skill files by filesystem path.

**Design**: `.specs/features/income-tracking/design.md`
**Status**: Approved (user delegated the run)

---

## Test Coverage Matrix

> Generated from codebase sampling and project guidelines - user delegated confirmation.
> Guidelines found: `.claude/skills/dotnet-usecase-tests/SKILL.md` (the repo's own testing standard:
> xUnit + Shouldly + Moq + Bogus, builders in `CommonTestUtilities`, ownership test mandatory,
> assert messages through `ResourceManager` and never a hardcoded string).
> Samples: `tests/WebApi.Test/Users/Register/RegisterUserTest.cs`, `tests/WebApi.Test/Login/DoLogin/DoLoginTest.cs`.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
| ---------- | ------------------ | -------------------- | ---------------- | ----------- |
| Use case (Application) | unit | All reachable branches; 1:1 to spec ACs; one representative validation case; every `NotFoundException`; the cross-user ownership case | `tests/UseCases.Test/**/*Test.cs` | `dotnet test tests/UseCases.Test` |
| Validator (FluentValidation) | unit | Every rule plus its boundary values | `tests/Validators.Tests/**/*Test.cs` | `dotnet test tests/Validators.Tests` |
| Controller / endpoint | integration | Every route added: happy path, each listed edge case, error paths, and 401 without a token | `tests/WebApi.Test/**/*Test.cs` | `dotnet test tests/WebApi.Test` |
| Infrastructure service (`LoggedUser`, DbContext behaviour) | integration | Key paths through the in-memory provider | `tests/WebApi.Test/**/*Test.cs` | `dotnet test tests/WebApi.Test` |
| Entity / enum / EF mapping / DI wiring | none | Build gate only | - | build gate only |

## Gate Check Commands

> Generated from codebase - user delegated confirmation. The solution has no linter or formatter
> configured, so the Build gate is compile + full suite.

| Gate Level | When to Use | Command |
| ---------- | ----------- | ------- |
| Quick | After tasks whose only tests are unit tests | `dotnet test tests/UseCases.Test; dotnet test tests/Validators.Tests` |
| Full | After tasks with integration tests | `dotnet test` |
| Build | After entity, mapping, enum or wiring-only tasks | `dotnet build` then `dotnet test` |

---

## Execution Plan

Phases are ordered and run sequentially - each phase completes before the next begins.

### Phase 1: Identity foundation and test conventions

```
T1 → T3 → T4
T2 → T3
```

### Phase 2: Person vertical

```
T5 → T6 → T7 → T9
T6 → T8 → T9
```

### Phase 3: Income domain and persistence

```
T10 → T11 → T12 → T13
```

### Phase 4: Income write use cases

```
T14 → T15 → T16
```

### Phase 5: Monthly view and API surface

```
T17 → T18
```

### Phase 6: Schema

```
T19
```

---

## Task Breakdown

### Phase 1: Identity foundation and test conventions

#### T1: Create BaseEntity ✅

**What**: The abstract root carrying `Guid Id`, `DateTime CreatedAt` and `DateTime? UpdatedAt`.
**Where**: `src/Balance.Domain/Entities/BaseEntity.cs`
**Depends on**: None
**Reuses**: nothing - new root
**Requirement**: CORE-01

**Done when**:

- [x] `Id` defaults to a new `Guid`, `CreatedAt` to `DateTime.UtcNow`, `UpdatedAt` to null
- [x] `dotnet build` reports 0 errors and 0 warnings

**Tests**: none
**Gate**: build
**Commit**: `feat(domain): add BaseEntity with id and audit timestamps`
**Status**: ✅ Complete - build clean, 0 errors 0 warnings

#### T2: Adopt the repo's test message convention ✅

**What**: Expose `ResourceErrorMessages.ResourceManager`, add the `CultureInlineDataTest` class data, and convert the two existing tests that assert hardcoded Portuguese strings.
**Where**: `tests/CommonTestUtilities/Culture/CultureInlineDataTest.cs`
**Depends on**: None
**Reuses**: `src/Balance.Exception/ResourceErrorMessages.cs`
**Requirement**: CORE-01

**Done when**:

- [x] `ResourceErrorMessages.ResourceManager` is public and the private field is gone
- [x] `CultureInlineDataTest` yields `en` and `pt-BR`
- [x] `RegisterUserTest` and `DoLoginTest` assert through `ResourceManager.GetString(key, culture)` as `[Theory]`
- [x] Gate check passes: `dotnet test`
- [x] Test count: 8 tests pass (5 existing, converted to 8 by the culture theories)

**Tests**: integration
**Gate**: full
**Commit**: `test: assert error messages through ResourceManager per repo convention`
**Status**: ✅ Complete - 8 passed, 0 failed

#### T3: Migrate User and its consumers to the Guid identity ✅

**What**: `User` inherits `BaseEntity`; `UserIdentifier` and `long Id` are removed, and every consumer moves to `Id`. One atomic change - splitting it leaves a tree that does not compile.
**Where**: `src/Balance.Domain/Entities/User.cs` plus its consumers `JwtTokenGenerator.cs`, `LoggedUser.cs`, `UserBuilder.cs`, `CustomWebApplicationFactory.cs`
**Depends on**: T1, T2
**Reuses**: existing JWT and logged-user wiring
**Requirement**: CORE-02, CORE-03

**Done when**:

- [x] `User` has no `UserIdentifier` and no `long Id`
- [x] The `Sid` claim carries `user.Id`
- [x] `LoggedUser` resolves on `Id`
- [x] A test proves the `Sid` claim of a login token equals the persisted user's `Id`
- [x] A test proves `LoggedUser.Get()` returns the user matching a token's `Sid`
- [x] Gate check passes: `dotnet test`
- [x] Test count: 10 tests pass (no silent deletions)

**Tests**: integration
**Gate**: full
**Commit**: `refactor(domain)!: move User onto BaseEntity Guid identity`
**Status**: ✅ Complete - 10 passed, 0 failed. Only the stale migration still names `UserIdentifier`; T19 regenerates it.

#### T4: Stamp audit timestamps centrally ✅

**What**: Override `SaveChanges` and `SaveChangesAsync` so `Added` entries get `CreatedAt` and `Modified` entries get `UpdatedAt`.
**Where**: `src/Balance.Infrastructure/DataAccess/BalanceDbContext.cs`
**Depends on**: T3
**Reuses**: the existing `BalanceDbContext`
**Requirement**: CORE-01

**Done when**:

- [x] A newly added entity has `CreatedAt` set and `UpdatedAt` null after save
- [x] A modified entity has `UpdatedAt` set after save
- [x] Gate check passes: `dotnet test`
- [x] Test count: 12 tests pass

**Tests**: integration
**Gate**: full
**Commit**: `feat(infrastructure): stamp CreatedAt and UpdatedAt on save`
**Status**: ✅ Complete - 12 passed, 0 failed. Phase 1 done.

---

### Phase 2: Person vertical

#### T5: Create the Person entity ✅

**What**: `Person` inheriting `BaseEntity`, with name, optional description, `UserId` and `IsAccountOwner`.
**Where**: `src/Balance.Domain/Entities/Person.cs`
**Depends on**: T1
**Reuses**: `BaseEntity`
**Requirement**: PRSN-01

**Done when**:

- [x] Navigation to `User` and the `UserId` foreign key are declared
- [x] `dotnet build` reports 0 errors and 0 warnings

**Tests**: none
**Gate**: build
**Commit**: `feat(domain): add Person entity owned by a user`
**Status**: ✅ Complete - build clean

#### T6: Persist Person ✅

**What**: `DbSet<Person>`, its mapping and index, and the read-only / write-only repository pair with ownership filtering.
**Where**: `src/Balance.Infrastructure/DataAccess/Repositories/People/PersonRepository.cs`
**Depends on**: T5
**Reuses**: `UserRepository` shape
**Requirement**: PRSN-03

**Done when**:

- [x] `GetAll(User)` and `GetById(User, Guid)` filter on `UserId`
- [x] Repositories are registered in `AddRepositories`
- [x] `dotnet build` reports 0 errors and 0 warnings

**Tests**: none
**Gate**: build
**Commit**: `feat(infrastructure): persist Person with ownership-scoped reads`
**Status**: ✅ Complete - build clean, 12 tests still green

#### T7: RegisterPerson use case ✅

**What**: The use case, its request and response contracts, its validator and its message keys.
**Where**: `src/Balance.Application/UseCases/People/Register/RegisterPersonUseCase.cs`
**Depends on**: T6
**Reuses**: `RegisterUserUseCase` and `RegisterUserValidator` shape
**Requirement**: PRSN-02

**Done when**:

- [x] A created Person is linked to the logged user with `IsAccountOwner` false
- [x] An empty name raises `ErrorOnValidationException` carrying `NAME_REQUIRED`
- [x] Gate check passes: `dotnet test tests/UseCases.Test; dotnet test tests/Validators.Tests`
- [x] Test count: 6 tests pass (planned 5; an ownership-linking assertion was added)

**Tests**: unit
**Gate**: quick
**Commit**: `feat(application): add RegisterPerson use case`
**Status**: ✅ Complete - UseCases.Test 3 passed, Validators.Tests 3 passed

#### T8: GetAllPeople use case ✅

**What**: The use case and its response contract, returning only the logged user's People.
**Where**: `src/Balance.Application/UseCases/People/GetAll/GetAllPeopleUseCase.cs`
**Depends on**: T6
**Reuses**: `ILoggedUser`, `IPersonReadOnlyRepository`
**Requirement**: PRSN-03

**Done when**:

- [x] Returns the People of the logged user
- [x] A second user's People are not returned (ownership test)
- [x] Gate check passes: `dotnet test tests/UseCases.Test; dotnet test tests/Validators.Tests`
- [x] Test count: 9 tests pass (6 UseCases.Test + 3 Validators.Tests)

**Tests**: unit
**Gate**: quick
**Commit**: `feat(application): add GetAllPeople use case`
**Status**: ✅ Complete - 9 unit tests green

#### T9: Expose People over HTTP and seed the owner ✅

**What**: `PersonController` with create and list, DI registration for both use cases, and the owner Person created inside `RegisterUserUseCase`.
**Where**: `src/Balance.Api/Controllers/PersonController.cs` plus `RegisterUserUseCase.cs` and both `DependencyInjectionExtension.cs`
**Depends on**: T7, T8
**Reuses**: `UserController` shape, `BalanceClassFixture`
**Requirement**: PRSN-01, PRSN-02, PRSN-03

**Done when**:

- [x] Both endpoints carry `[Authorize]` and answer 401 without a token
- [x] Registering a user creates exactly one Person flagged `IsAccountOwner`
- [x] Creating a Person answers 201; listing returns only the caller's People
- [x] Gate check passes: `dotnet test`
- [x] Test count: 28 tests pass (3 Validators + 6 UseCases + 19 WebApi)

**Tests**: integration
**Gate**: full
**Commit**: `feat(api): add PersonController and seed the owner person`
**Status**: ✅ Complete - Phase 2 done. 28 tests green.

---

### Phase 3: Income domain and persistence

#### T10: Create the income enums

**What**: `IncomeType` with Recurring and Variable, and `IncomeStatus` with Pending, Received and Divergent.
**Where**: `src/Balance.Domain/Enums/IncomeType.cs`
**Depends on**: None
**Reuses**: `Roles` enum-style class shape
**Requirement**: INC-01

**Done when**:

- [ ] Both enums declared with explicit integer values
- [ ] `dotnet build` reports 0 errors and 0 warnings

**Tests**: none
**Gate**: build
**Commit**: `feat(domain): add income type and status enums`

#### T11: Create the income entities

**What**: `IncomeSource`, `IncomeSourceVersion` and `IncomePayment`, all inheriting `BaseEntity`.
**Where**: `src/Balance.Domain/Entities/IncomeSource.cs` and its two siblings
**Depends on**: T10
**Reuses**: `BaseEntity`, `Person`
**Requirement**: INC-01, INC-04, INC-11

**Done when**:

- [ ] `IncomePayment.IncomeSourceVersionId` is nullable
- [ ] `IncomeSourceVersion.ValidityEnd` is nullable
- [ ] `dotnet build` reports 0 errors and 0 warnings

**Tests**: none
**Gate**: build
**Commit**: `feat(domain): add income source, version and payment entities`

#### T12: Map the income tables

**What**: The three `DbSet` properties, precision, delete behaviour and indexes.
**Where**: `src/Balance.Infrastructure/DataAccess/BalanceDbContext.cs`
**Depends on**: T11
**Reuses**: the existing `OnModelCreating`
**Requirement**: INC-01

**Done when**:

- [ ] Money columns declare precision 18 scale 2
- [ ] `(IncomeSourceId, ReferenceMonth)` is indexed
- [ ] Every relationship uses restrict delete behaviour
- [ ] `dotnet build` reports 0 errors and 0 warnings

**Tests**: none
**Gate**: build
**Commit**: `feat(infrastructure): map income tables with precision and indexes`

#### T13: Income repositories

**What**: The read-only and write-only repository pair for income, with ownership filtering and the month query.
**Where**: `src/Balance.Infrastructure/DataAccess/Repositories/Incomes/IncomeSourceRepository.cs`
**Depends on**: T12
**Reuses**: `PersonRepository` shape
**Requirement**: INC-03, INC-06, INC-07

**Done when**:

- [ ] `GetById(User, Guid)` includes versions and filters through `Person.UserId`
- [ ] `GetForMonth(User, DateOnly)` includes versions and that month's payments and excludes archived sources
- [ ] Repositories are registered in `AddRepositories`
- [ ] `dotnet build` reports 0 errors and 0 warnings

**Tests**: none
**Gate**: build
**Commit**: `feat(infrastructure): add income repositories with month query`

---

### Phase 4: Income write use cases

#### T14: RegisterIncomeSource use case

**What**: Creates the source and, for Recurring, its first open version, in one commit.
**Where**: `src/Balance.Application/UseCases/Incomes/Register/RegisterIncomeSourceUseCase.cs`
**Depends on**: T13
**Reuses**: `RegisterPersonUseCase` shape
**Requirement**: INC-01, INC-02, INC-03

**Done when**:

- [ ] A Recurring source persists one open version; a Variable source persists none
- [ ] A Person belonging to another user raises `NotFoundException` (ownership test)
- [ ] Amount not greater than zero and expected day outside 1 to 31 both raise validation errors
- [ ] Gate check passes: `dotnet test tests/UseCases.Test; dotnet test tests/Validators.Tests`
- [ ] Test count: 20 tests pass

**Tests**: unit
**Gate**: quick
**Commit**: `feat(application): add RegisterIncomeSource use case`

#### T15: RegisterIncomePayment use case

**What**: Records a payment and freezes the version in effect at the reference month.
**Where**: `src/Balance.Application/UseCases/Incomes/RegisterPayment/RegisterIncomePaymentUseCase.cs`
**Depends on**: T14
**Reuses**: the version-in-effect rule from the design
**Requirement**: INC-04, INC-05, INC-06

**Done when**:

- [ ] A Recurring payment stores the version in effect; a Variable payment stores null
- [ ] An archived source raises `INCOME_SOURCE_ARCHIVED`
- [ ] A source belonging to another user raises `NotFoundException` (ownership test)
- [ ] Gate check passes: `dotnet test tests/UseCases.Test; dotnet test tests/Validators.Tests`
- [ ] Test count: 28 tests pass

**Tests**: unit
**Gate**: quick
**Commit**: `feat(application): add RegisterIncomePayment use case`

#### T16: ChangeIncomeSourceValue use case

**What**: Closes the version in effect and opens a new one carrying the change reason.
**Where**: `src/Balance.Application/UseCases/Incomes/ChangeValue/ChangeIncomeSourceValueUseCase.cs`
**Depends on**: T15
**Reuses**: the version-in-effect rule
**Requirement**: INC-11, INC-12, INC-13

**Done when**:

- [ ] The old version's `ValidityEnd` is the day before the new `ValidityStart`
- [ ] A Variable source raises `VARIABLE_SOURCE_HAS_NO_VERSION`
- [ ] A start not later than the current version's start raises a validation error
- [ ] Gate check passes: `dotnet test tests/UseCases.Test; dotnet test tests/Validators.Tests`
- [ ] Test count: 36 tests pass

**Tests**: unit
**Gate**: quick
**Commit**: `feat(application): add ChangeIncomeSourceValue use case`

---

### Phase 5: Monthly view and API surface

#### T17: GetMonthlyIncome use case

**What**: Reconciles expected against received per source and returns the month totals.
**Where**: `src/Balance.Application/UseCases/Incomes/GetMonthly/GetMonthlyIncomeUseCase.cs`
**Depends on**: T16
**Reuses**: `GetForMonth`, the version-in-effect rule, the status resolution order
**Requirement**: INC-07, INC-08, INC-09, INC-10

**Done when**:

- [ ] Recurring reports expected amount and day from the version in effect; Variable reports null
- [ ] Status resolves Pending, Received and Divergent per the design's ordering
- [ ] Totals equal the sum of the lines; an empty account returns zeroed totals
- [ ] A month inside a closed version's range reports that closed version's amount
- [ ] Gate check passes: `dotnet test tests/UseCases.Test; dotnet test tests/Validators.Tests`
- [ ] Test count: 48 tests pass

**Tests**: unit
**Gate**: quick
**Commit**: `feat(application): add GetMonthlyIncome use case`

#### T18: Expose income over HTTP

**What**: `IncomeController` with the four routes and DI registration for all four use cases.
**Where**: `src/Balance.Api/Controllers/IncomeController.cs`
**Depends on**: T17
**Reuses**: `PersonController` shape
**Requirement**: INC-01, INC-04, INC-07, INC-11

**Done when**:

- [ ] All four routes carry `[Authorize]` and answer 401 without a token
- [ ] Registering, paying and reading a month works end to end for a seeded user
- [ ] A second account does not see the first account's sources
- [ ] Gate check passes: `dotnet test`
- [ ] Test count: 60 tests pass

**Tests**: integration
**Gate**: full
**Commit**: `feat(api): add IncomeController`

---

### Phase 6: Schema

#### T19: Regenerate the initial migration

**What**: Delete the stale `InitialCreate` and generate one covering all five tables.
**Where**: `src/Balance.Infrastructure/Migrations/`
**Depends on**: T18
**Reuses**: the existing EF Core tooling setup
**Requirement**: CORE-02, INC-01

**Done when**:

- [ ] Exactly one migration exists and it creates Users, People, IncomeSources, IncomeSourceVersions and IncomePayments
- [ ] `Users.Id` is `uuid` and `Users.Email` keeps its unique index
- [ ] `dotnet build` then `dotnet test` both pass
- [ ] Test count: 60 tests pass

**Tests**: none
**Gate**: build
**Commit**: `feat(infrastructure): regenerate initial migration for the income schema`

---

## Task Granularity Check

| Task | Scope | Status |
| ---- | ----- | ------ |
| T1, T5, T10, T11 | 1 entity or enum file group | ✅ Granular |
| T2 | 1 convention + its 2 conversions | ⚠️ Multi-file, cohesive |
| T3 | 1 identity change + its 4 forced consumers | ⚠️ Multi-file, one dependency chain that cannot compile split |
| T4, T12 | 1 file | ✅ Granular |
| T6, T13 | 1 repository pair + registration | ⚠️ Multi-file, cohesive |
| T7, T8, T14, T15, T16, T17 | 1 use case + contracts + validator + tests | ✅ Granular |
| T9, T18 | 1 controller + wiring | ⚠️ Multi-file, cohesive |
| T19 | 1 migration | ✅ Granular |

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
| ---- | ---------------------- | ------------- | ------ |
| T1 | None | none | ✅ Match |
| T2 | None | none | ✅ Match |
| T3 | T1, T2 | T1 → T3, T2 → T3 | ✅ Match |
| T4 | T3 | T3 → T4 | ✅ Match |
| T5 | T1 | cross-phase, backward | ✅ Match |
| T6 | T5 | T5 → T6 | ✅ Match |
| T7 | T6 | T6 → T7 | ✅ Match |
| T8 | T6 | T6 → T8 | ✅ Match |
| T9 | T7, T8 | T7 → T9, T8 → T9 | ✅ Match |
| T10 | None | none | ✅ Match |
| T11 | T10 | T10 → T11 | ✅ Match |
| T12 | T11 | T11 → T12 | ✅ Match |
| T13 | T12 | T12 → T13 | ✅ Match |
| T14 | T13 | cross-phase, backward | ✅ Match |
| T15 | T14 | T14 → T15 | ✅ Match |
| T16 | T15 | T15 → T16 | ✅ Match |
| T17 | T16 | cross-phase, backward | ✅ Match |
| T18 | T17 | T17 → T18 | ✅ Match |
| T19 | T18 | cross-phase, backward | ✅ Match |

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
| ---- | --------------------------- | --------------- | --------- | ------ |
| T1 | Entity | none | none | ✅ OK |
| T2 | Test infrastructure + integration tests | integration | integration | ✅ OK |
| T3 | Entity + infrastructure service | integration | integration | ✅ OK |
| T4 | DbContext behaviour | integration | integration | ✅ OK |
| T5 | Entity | none | none | ✅ OK |
| T6 | EF mapping + repository | none | none | ✅ OK |
| T7 | Use case + validator | unit | unit | ✅ OK |
| T8 | Use case | unit | unit | ✅ OK |
| T9 | Controller + use case wiring | integration | integration | ✅ OK |
| T10 | Enum | none | none | ✅ OK |
| T11 | Entity | none | none | ✅ OK |
| T12 | EF mapping | none | none | ✅ OK |
| T13 | Repository | none | none | ✅ OK |
| T14 | Use case + validator | unit | unit | ✅ OK |
| T15 | Use case + validator | unit | unit | ✅ OK |
| T16 | Use case + validator | unit | unit | ✅ OK |
| T17 | Use case | unit | unit | ✅ OK |
| T18 | Controller | integration | integration | ✅ OK |
| T19 | Migration | none | none | ✅ OK |

## Phase Execution Map

```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6
```

19 tasks. Executed inline in the main window: the user delegated the run without review and sub-agents
are offer-then-confirm, which no longer has an answering party. The Verifier therefore runs as the
standalone fresh-eyes pass described in `validate.md`.
