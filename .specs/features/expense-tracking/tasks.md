# Expense Tracking Tasks

**Spec**: `.specs/features/expense-tracking/spec.md`
**Design**: `.specs/features/expense-tracking/design.md`
**Status**: Awaiting approval

49 tasks across 10 phases. Every task ends in one atomic commit. No task edits an income file.

---

## Gate Check Commands

| Gate | Command |
| ---- | ------- |
| `build` | `dotnet build Balance.sln --nologo` — zero errors, zero warnings |
| `test` | `dotnet test Balance.sln --nologo` — every test green, including the 82 pre-existing ones |
| `commit` | `"C:\Program Files\LibreOffice\program\python.exe" .claude/skills/tlc-spec-driven/scripts/check_commit.py --message "<msg>"` |
| `migration` | `dotnet ef migrations list --project src/Balance.Infrastructure --startup-project src/Balance.Api` |
| `page` | The page renders the seeded month with non-zero income and expense totals in a browser |

`test` subsumes `build`, so a task whose gate is `test` does not repeat `build`.

The Python that runs the skill validators is LibreOffice's bundled CPython 3.10.19. The `python` and
`python3` names on PATH are Microsoft Store stubs and do not work.

---

## Test Coverage Matrix

Which layer proves what. A task's `Tests` field must agree with this table.

| Layer | Project | What it proves | Requirements |
| ----- | ------- | -------------- | ------------ |
| Pure domain rules | `tests/UseCases.Test` (no fixture) | `CompetenceMonthResolver` and `VersionInEffect` in isolation, including overlapping-validity fixtures | EXPN-02, VIEW-02 |
| Use case | `tests/UseCases.Test` | Business rules with mocked repositories: transactions, installment arithmetic, status resolution, ownership 404s | EXPN-01..03, INST-01..03, RECR-01..05, RPAY-01..03, VIEW-01..04, DASH-01 |
| Validator | `tests/Validators.Tests` | Field-level rules and their message keys, in both cultures | SHAR-03, EXPN-03, INST-03, RECR-02, RPAY-03 |
| Endpoint | `tests/WebApi.Test` | Status codes, JSON shape, `[Authorize]`, cross-account isolation, end-to-end month reconciliation | every requirement |
| Schema shape | none — asserted by the migration gate | Precision, indexes and delete behaviour. The EF in-memory provider used by `WebApi.Test` ignores unique indexes and precision, so these are verified by reading the generated migration, and every rule they back is *also* enforced in a use case and unit-tested there | SHAR-02, RPAY-03 |
| Entities, enums, contracts | none — compile-only | Data shapes carry no behaviour; the build gate is the whole check and behaviour is asserted by the slice that consumes them | — |

---

## Execution Plan

| Phase | Theme | Tasks | Batch |
| ----- | ----- | ----- | ----- |
| 1 | Schema foundation | T1–T6 | 1 |
| 2 | Domain rules and messages | T7–T9 | 1 |
| 3 | Category and Account slice | T10–T15 | 2 |
| 4 | Expense slice | T16–T20 | 3 |
| 5 | Installment plans | T21–T24 | 3 |
| 6 | Recurring expenses | T25–T31 | 4 |
| 7 | Recurring payments | T32–T37 | 5 |
| 8 | Monthly expense view | T38–T41 | 6 |
| 9 | Dashboard | T42–T44 | 6 |
| 10 | Migration, seed and page | T45–T49 | 7 — run inline, not delegated |

Phase 10 stays with the orchestrator: it starts Docker, applies migrations and drives a browser, all of
which need the interactive environment rather than a worker's sandbox.

---

## Task Breakdown

### Phase 1: Schema foundation

```
T1 -> T3
T1 -> T4
T3 -> T4
T3 -> T5
T3 -> T6
T4 -> T6
T5 -> T6
```

T2 has no intra-phase dependency.

#### T1: Add the domain expense enums ✅

**Where**: `src/Balance.Domain/Enums/`
**What**: `ExpenseType { Credit = 0, Debit = 1, Pix = 2 }`, `ExpensePriority { Essential = 0, Important = 1, Superfluous = 2 }`, `ExpenseStatus { Pending = 0, Paid = 1, Divergent = 2 }`, one file each, matching the existing `IncomeType` style.
**Depends on**: none
**Requirement**: SHAR-01, EXPN-01, VIEW-03
**Tests**: compile-only per the coverage matrix — enums carry no behaviour; their values are asserted wherever they are consumed
**Gate**: `build`
**Status**: ✅ Complete — build clean, 0 errors 0 warnings

#### T2: Mirror the expense enums in Communication

**Where**: `src/Balance.Communication/Enums/`
**What**: The same three enums with identical member order, so the existing `(CommunicationX)domainX` cast convention stays valid.
**Depends on**: none
**Requirement**: SHAR-01, EXPN-01, VIEW-03
**Tests**: compile-only per the coverage matrix; the cast is exercised by every endpoint test in later phases
**Gate**: `build`

#### T3: Add the Category and Account entities

**Where**: `src/Balance.Domain/Entities/`
**What**: `Category : BaseEntity` with `Name`, `Description?`, `Priority`, `UserId`, `User`. `Account : BaseEntity` with `Name`, `Institution`, `ClosingDay?`, `DueDay?`, `Limit?`, `PersonId`, `Person`. The nullable card fields are the design's decision for debit accounts.
**Depends on**: T1
**Requirement**: SHAR-01, SHAR-02
**Tests**: compile-only per the coverage matrix; persistence is asserted by T15
**Gate**: `build`

#### T4: Add the Expense and InstallmentPlan entities

**Where**: `src/Balance.Domain/Entities/`
**What**: `Expense : BaseEntity` and `InstallmentPlan : BaseEntity` exactly as specified in the design's data model, including the nullable `InstallmentNumber` and `InstallmentPlanId` and the `Installments` collection.
**Depends on**: T1, T3
**Requirement**: EXPN-01, INST-01
**Tests**: compile-only per the coverage matrix; persistence is asserted by T20 and T24
**Gate**: `build`

#### T5: Add the recurring expense entities

**Where**: `src/Balance.Domain/Entities/`
**What**: `RecurringExpense`, `RecurringExpenseVersion` and `RecurringExpensePayment`, all `: BaseEntity`, as specified in the design's data model, including the nullable `AccountId` on the payment.
**Depends on**: T3
**Requirement**: RECR-01, RPAY-01
**Tests**: compile-only per the coverage matrix; persistence is asserted by T31 and T37
**Gate**: `build`

#### T6: Configure the seven new entities in BalanceDbContext

**Where**: `src/Balance.Infrastructure/DataAccess/BalanceDbContext.cs`
**What**: `DbSet` per entity; `HasPrecision(18, 2)` on every money column; `Restrict` on every relationship; the indexes listed in the design; a unique index on `RecurringExpensePayment (RecurringExpenseId, ReferenceMonth)`. The income configuration block is not touched.
**Depends on**: T3, T4, T5
**Requirement**: SHAR-02, RPAY-03
**Tests**: schema-shape layer per the coverage matrix — verified by reading the migration in T45; the uniqueness rule it backs is separately enforced and unit-tested in T34
**Gate**: `test` — the 82 existing tests must stay green through the context change

---

### Phase 2: Domain rules and messages

No intra-phase dependencies: T7, T8 and T9 depend only on Phase 1 and can be done in any order.

#### T7: Add the four new error-message keys

**Where**: `src/Balance.Exception/ResourceErrorMessages.resx`
**What**: `DAY_OUT_OF_RANGE`, `INSTALLMENT_COUNT_INVALID`, `PAYMENT_ALREADY_RECORDED`, `RECURRING_EXPENSE_ARCHIVED` added to the invariant `.resx`, the `pt-BR` `.resx` and the generated `ResourceErrorMessages` accessor. Existing keys are reused, not duplicated.
**Depends on**: none
**Requirement**: SHAR-03, INST-03, RPAY-03
**Tests**: validator layer per the coverage matrix — each key is asserted in both cultures by the validator tests of the phase that raises it
**Gate**: `build`

#### T8: Add CompetenceMonthResolver

**Where**: `src/Balance.Domain/Extensions/CompetenceMonthResolver.cs`
**What**: `static DateOnly Resolve(ExpenseType, int? closingDay, DateOnly date)` implementing EXPN-02: credit past the closing day rolls to the next month; everything else stays in the date's month. Always returns day 1.
**Depends on**: none
**Requirement**: EXPN-02
**Tests**: pure-domain layer — a fixture per branch, including a purchase exactly on the closing day (which must not roll), a December purchase rolling into January of the next year, and a credit account with a null closing day
**Gate**: `test`

#### T9: Add RecurringExpenseExtensions.VersionInEffect

**Where**: `src/Balance.Domain/Extensions/RecurringExpenseExtensions.cs`
**What**: The version-in-effect rule for a competence month, duplicated deliberately from income per the design. Reuses `DateOnly.FirstDayOfMonth()` without editing it.
**Depends on**: none
**Requirement**: VIEW-02
**Tests**: pure-domain layer — including a fixture where two versions genuinely overlap the same month, so the ordering is exercised rather than assumed. This is lesson **L-001** applied directly
**Gate**: `test`

---

### Phase 3: Category and Account slice

```
T10 -> T12
T11 -> T13
T12 -> T14
T13 -> T14
T14 -> T15
```

#### T10: Add the Category repository

**Where**: `src/Balance.Infrastructure/DataAccess/Repositories/Categories/CategoryRepository.cs`
**What**: `ICategoryReadOnlyRepository` (`GetAll(User)`, `GetById(User, Guid)`) and `ICategoryWriteOnlyRepository` (`Add`) in Domain, with one implementation. Both reads filter on `UserId` per AD-003.
**Depends on**: none
**Requirement**: SHAR-01
**Tests**: endpoint layer per the coverage matrix — repository filtering is proved by the cross-account isolation test in T15
**Gate**: `build`

#### T11: Add the Account repository

**Where**: `src/Balance.Infrastructure/DataAccess/Repositories/Accounts/AccountRepository.cs`
**What**: `IAccountReadOnlyRepository` (`GetAll(User)`, `GetById(User, Guid)`) and `IAccountWriteOnlyRepository` (`Add`). Both reads filter on `Person.UserId`.
**Depends on**: none
**Requirement**: SHAR-02
**Tests**: endpoint layer per the coverage matrix — filtering is proved by the cross-account isolation test in T15
**Gate**: `build`

#### T12: Add the Category contracts and use cases

**Where**: `src/Balance.Application/UseCases/Categories/`
**What**: `RequestRegisterCategoryJson`, `ResponseCategoryJson`, `RegisterCategoryUseCase` with its validator, and `GetAllCategoriesUseCase`. The register use case attaches `UserId` from `ILoggedUser`.
**Depends on**: T10
**Requirement**: SHAR-01, SHAR-03
**Tests**: use case + validator layers — a success path, a listing scoped to the logged user, and the `NAME_REQUIRED` rule in both cultures
**Gate**: `test`

#### T13: Add the Account contracts and use cases

**Where**: `src/Balance.Application/UseCases/Accounts/`
**What**: `RequestRegisterAccountJson`, `ResponseAccountJson`, `RegisterAccountUseCase` with its validator, and `GetAllAccountsUseCase`. The register use case resolves the referenced `Person` through `IPersonReadOnlyRepository` and 404s when it is not the caller's.
**Depends on**: T11
**Requirement**: SHAR-02, SHAR-03
**Tests**: use case + validator layers — a success path, a foreign person producing 404, `NAME_REQUIRED`, `DAY_OUT_OF_RANGE` for day 0 and day 32, and acceptance of a null closing day, due day and limit
**Gate**: `test`

#### T14: Add the Category and Account controllers

**Where**: `src/Balance.Api/Controllers/CategoryController.cs`
**What**: `CategoryController` and `AccountController`, both `[Authorize]`, with full `ProducesResponseType` documentation matching `IncomeController`, plus the four use case registrations in `Balance.Application`'s DI extension and the four repository registrations in `Balance.Infrastructure`'s.
**Depends on**: T12, T13
**Requirement**: SHAR-01, SHAR-02
**Tests**: endpoint layer — asserted by T15
**Gate**: `build`

#### T15: Add the catalogue endpoint tests

**Where**: `tests/WebApi.Test/Categories/CategoryEndpointsTest.cs`
**What**: Integration tests for both controllers: 201 on create, listing returns only the caller's rows, a second account sees none of them, a foreign `PersonId` on an account returns 404, and every route returns 401 without a token.
**Depends on**: T14
**Requirement**: SHAR-01, SHAR-02, SHAR-03
**Tests**: this task is the endpoint-layer coverage for Phase 3
**Gate**: `test`

---

### Phase 4: Expense slice

```
T16 -> T18
T17 -> T18
T18 -> T19
T19 -> T20
```

#### T16: Add the Expense repository

**Where**: `src/Balance.Infrastructure/DataAccess/Repositories/Expenses/ExpenseRepository.cs`
**What**: `IExpenseWriteOnlyRepository` (`Add`, `AddRange`) and `IExpenseReadOnlyRepository` (`GetForMonth(User, DateOnly)`), the read including category, account and plan and filtering on `Person.UserId` and `CompetenceMonth`.
**Depends on**: none
**Requirement**: EXPN-01, VIEW-01
**Tests**: endpoint layer per the coverage matrix — proved by T20 and T41
**Gate**: `build`

#### T17: Add the Expense contracts

**Where**: `src/Balance.Communication/Requests/RequestRegisterExpenseJson.cs`
**What**: `RequestRegisterExpenseJson` with `Name`, `PersonId`, `Type`, `Amount`, `CategoryId`, `AccountId`, `Date` and a nullable `CompetenceMonth` override, plus `ResponseExpenseJson`.
**Depends on**: none
**Requirement**: EXPN-01, EXPN-02
**Tests**: compile-only per the coverage matrix; the shape is asserted by T20
**Gate**: `build`

#### T18: Add RegisterExpenseUseCase

**Where**: `src/Balance.Application/UseCases/Expenses/Register/RegisterExpenseUseCase.cs`
**What**: Resolves person, category and account through their owning repositories, derives `CompetenceMonth` through `CompetenceMonthResolver` unless the request overrides it, persists and commits. Its validator covers `NAME_REQUIRED` and `AMOUNT_GREATER_THAN_ZERO`.
**Depends on**: T16, T17
**Requirement**: EXPN-01, EXPN-02, EXPN-03
**Tests**: use case + validator layers — credit after and on the closing day, debit ignoring the closing day, credit on an account with no closing day, an explicit override winning over the derived value, an account belonging to a *different person of the same user* succeeding, and a foreign person, category or account each producing 404
**Gate**: `test`

#### T19: Add the ExpenseController register route

**Where**: `src/Balance.Api/Controllers/ExpenseController.cs`
**What**: `[Authorize]` controller with `POST /api/expense`, documented responses, plus the use case and repository DI registrations.
**Depends on**: T18
**Requirement**: EXPN-01
**Tests**: endpoint layer — asserted by T20
**Gate**: `build`

#### T20: Add the expense endpoint tests

**Where**: `tests/WebApi.Test/Expenses/RegisterExpenseTest.cs`
**What**: 201 with the derived competence month for a credit expense past the closing day, 400 for a non-positive amount, 404 for a foreign category, 401 without a token, and a cross-person account accepted.
**Depends on**: T19
**Requirement**: EXPN-01, EXPN-02, EXPN-03
**Tests**: this task is the endpoint-layer coverage for Phase 4
**Gate**: `test`

---

### Phase 5: Installment plans

```
T21 -> T22
T22 -> T23
T23 -> T24
```

#### T21: Add the InstallmentPlan repository and contracts

**Where**: `src/Balance.Infrastructure/DataAccess/Repositories/Expenses/InstallmentPlanRepository.cs`
**What**: `IInstallmentPlanWriteOnlyRepository.Add(InstallmentPlan)`, plus `RequestRegisterInstallmentPlanJson` and `ResponseInstallmentPlanJson` carrying the generated installments.
**Depends on**: none
**Requirement**: INST-01
**Tests**: endpoint layer per the coverage matrix — proved by T24
**Gate**: `build`

#### T22: Add RegisterInstallmentPlanUseCase

**Where**: `src/Balance.Application/UseCases/Expenses/RegisterInstallmentPlan/RegisterInstallmentPlanUseCase.cs`
**What**: Generates N expenses in one `Commit()`: amounts `round(total / n, 2)` with the residual on the last, competence months advancing monthly from the resolved first month, `Type = Credit`, `Date = StartDate`, and the plan's `EndDate` set to the last installment's competence month. Validator covers `INSTALLMENT_COUNT_INVALID` and `AMOUNT_GREATER_THAN_ZERO`.
**Depends on**: T21
**Requirement**: INST-01, INST-02, INST-03
**Tests**: use case + validator layers — 100.00 over 3 giving 33.33/33.33/33.34, several awkward totals whose installments must sum exactly, N consecutive competence months crossing a year boundary, a count of 1 rejected, and a foreign category producing 404
**Gate**: `test`

#### T23: Add the installment-plan route

**Where**: `src/Balance.Api/Controllers/ExpenseController.cs`
**What**: `POST /api/expense/installment-plan` with documented responses, plus its DI registrations.
**Depends on**: T22
**Requirement**: INST-01
**Tests**: endpoint layer — asserted by T24
**Gate**: `build`

#### T24: Add the installment-plan endpoint tests

**Where**: `tests/WebApi.Test/Expenses/RegisterInstallmentPlanTest.cs`
**What**: 201 returning N installments whose amounts sum to the total and whose competence months are consecutive, 400 for a count below 2, and 401 without a token.
**Depends on**: T23
**Requirement**: INST-01, INST-02, INST-03
**Tests**: this task is the endpoint-layer coverage for Phase 5
**Gate**: `test`

---

### Phase 6: Recurring expenses

```
T25 -> T27
T26 -> T27
T25 -> T28
T26 -> T28
T25 -> T29
T27 -> T30
T28 -> T30
T29 -> T30
T30 -> T31
```

#### T25: Add the RecurringExpense repositories

**Where**: `src/Balance.Infrastructure/DataAccess/Repositories/RecurringExpenses/RecurringExpenseRepository.cs`
**What**: The read-only, write-only and update-only interfaces from the design, with one implementation. `GetForMonth` includes the versions and that month's payment; every read filters on `Person.UserId`.
**Depends on**: none
**Requirement**: RECR-01, VIEW-02
**Tests**: endpoint layer per the coverage matrix — proved by T31 and T41
**Gate**: `build`

#### T26: Add the recurring expense contracts

**Where**: `src/Balance.Communication/Requests/RequestRegisterRecurringExpenseJson.cs`
**What**: `RequestRegisterRecurringExpenseJson`, `RequestChangeRecurringExpenseValueJson` and `ResponseRecurringExpenseJson` with its version.
**Depends on**: none
**Requirement**: RECR-01, RECR-03
**Tests**: compile-only per the coverage matrix; shapes are asserted by T31
**Gate**: `build`

#### T27: Add RegisterRecurringExpenseUseCase

**Where**: `src/Balance.Application/UseCases/RecurringExpenses/Register/RegisterRecurringExpenseUseCase.cs`
**What**: Persists the recurring expense and its first open version in one `Commit()`, with `Archived = false` and the supplied `IsEstimate`. Validator covers `NAME_REQUIRED`, `AMOUNT_GREATER_THAN_ZERO` and `DAY_OUT_OF_RANGE`.
**Depends on**: T25, T26
**Requirement**: RECR-01, RECR-02
**Tests**: use case + validator layers — one version created with a null validity end, `Commit` called exactly once, a foreign person/category/account each producing 404, and each validation message in both cultures
**Gate**: `test`

#### T28: Add ChangeRecurringExpenseValueUseCase

**Where**: `src/Balance.Application/UseCases/RecurringExpenses/ChangeValue/ChangeRecurringExpenseValueUseCase.cs`
**What**: Closes the version in effect at the day before the new validity start and opens the new one in one `Commit()`. Validator covers `CHANGE_REASON_REQUIRED` and `VALIDITY_START_MUST_BE_LATER`.
**Depends on**: T25, T26
**Requirement**: RECR-03, RECR-04
**Tests**: use case + validator layers — the old version's end set to the day before, the new version open with its reason, both saved in one commit, an equal or earlier start rejected, and a foreign expense producing 404
**Gate**: `test`

#### T29: Add ArchiveRecurringExpenseUseCase

**Where**: `src/Balance.Application/UseCases/RecurringExpenses/Archive/ArchiveRecurringExpenseUseCase.cs`
**What**: One use case taking the target state, so archive and unarchive share the ownership check. Loads through the update-only repository, sets `Archived` and commits.
**Depends on**: T25
**Requirement**: RECR-05
**Tests**: use case layer — archiving sets the flag, unarchiving clears it, and a foreign expense produces 404 without touching the flag
**Gate**: `test`

#### T30: Add the RecurringExpenseController

**Where**: `src/Balance.Api/Controllers/RecurringExpenseController.cs`
**What**: `[Authorize]` controller with `POST /api/recurring-expense`, `PUT /api/recurring-expense/value` and `PUT /api/recurring-expense/{id:guid}/archive`, documented responses, plus DI registrations.
**Depends on**: T27, T28, T29
**Requirement**: RECR-01, RECR-03, RECR-05
**Tests**: endpoint layer — asserted by T31
**Gate**: `build`

#### T31: Add the recurring expense endpoint tests

**Where**: `tests/WebApi.Test/RecurringExpenses/RecurringExpenseEndpointsTest.cs`
**What**: 201 on register with one open version, 200 on a value change with the old version closed, 204 on archive and unarchive, 404 for a foreign expense, 400 for each validation rule, and 401 on every route without a token.
**Depends on**: T30
**Requirement**: RECR-01..05
**Tests**: this task is the endpoint-layer coverage for Phase 6
**Gate**: `test`

---

### Phase 7: Recurring payments

```
T32 -> T34
T33 -> T34
T32 -> T35
T33 -> T35
T34 -> T36
T35 -> T36
T36 -> T37
```

#### T32: Add the RecurringExpensePayment repository

**Where**: `src/Balance.Infrastructure/DataAccess/Repositories/RecurringExpenses/RecurringExpensePaymentRepository.cs`
**What**: `Add`, a tracked `GetById(User, Guid)` for the update path, and `GetByMonth(Guid, DateOnly)` backing the duplicate probe. Reads filter on `RecurringExpense.Person.UserId`.
**Depends on**: none
**Requirement**: RPAY-01, RPAY-02
**Tests**: endpoint layer per the coverage matrix — proved by T37
**Gate**: `build`

#### T33: Add the payment contracts

**Where**: `src/Balance.Communication/Requests/RequestRegisterRecurringExpensePaymentJson.cs`
**What**: `RequestRegisterRecurringExpensePaymentJson`, `RequestUpdateRecurringExpensePaymentJson` and `ResponseRecurringExpensePaymentJson`, all carrying the nullable paying `AccountId`.
**Depends on**: none
**Requirement**: RPAY-01, RPAY-02
**Tests**: compile-only per the coverage matrix; shapes are asserted by T37
**Gate**: `build`

#### T34: Add RegisterRecurringExpensePaymentUseCase

**Where**: `src/Balance.Application/UseCases/RecurringExpenses/RegisterPayment/RegisterRecurringExpensePaymentUseCase.cs`
**What**: Resolves the recurring expense, rejects an archived one, probes for an existing payment in that month, resolves and freezes the version in effect, persists and commits. Validator covers `AMOUNT_GREATER_THAN_ZERO`.
**Depends on**: T32, T33
**Requirement**: RPAY-01, RPAY-03
**Tests**: use case + validator layers — the frozen version id matching the version in effect, a second payment for the same month rejected with `PAYMENT_ALREADY_RECORDED`, an archived expense rejected, a month with no version in effect rejected, a null notes and null paying account accepted, and a foreign expense producing 404
**Gate**: `test`

#### T35: Add UpdateRecurringExpensePaymentUseCase

**Where**: `src/Balance.Application/UseCases/RecurringExpenses/UpdatePayment/UpdateRecurringExpensePaymentUseCase.cs`
**What**: Loads the payment tracked and scoped to the caller, overwrites amount, payment date, notes and paying account, and commits.
**Depends on**: T32, T33
**Requirement**: RPAY-02, RPAY-03
**Tests**: use case + validator layers — the amount overwritten while the reference month and frozen version id are unchanged, a non-positive amount rejected, and a foreign payment producing 404
**Gate**: `test`

#### T36: Add the payment routes

**Where**: `src/Balance.Api/Controllers/RecurringExpenseController.cs`
**What**: `POST /api/recurring-expense/payment` and `PUT /api/recurring-expense/payment/{id:guid}`, documented, plus DI registrations.
**Depends on**: T34, T35
**Requirement**: RPAY-01, RPAY-02
**Tests**: endpoint layer — asserted by T37
**Gate**: `build`

#### T37: Add the payment endpoint tests

**Where**: `tests/WebApi.Test/RecurringExpenses/RecurringExpensePaymentTest.cs`
**What**: 201 on record, 200 on correction, 400 on a duplicate month, 400 against an archived expense, 404 for a foreign payment, and 401 on both routes without a token.
**Depends on**: T36
**Requirement**: RPAY-01, RPAY-02, RPAY-03
**Tests**: this task is the endpoint-layer coverage for Phase 7
**Gate**: `test`

---

### Phase 8: Monthly expense view

```
T38 -> T39
T39 -> T40
T40 -> T41
```

#### T38: Add the monthly expense contracts

**Where**: `src/Balance.Communication/Responses/ResponseMonthlyExpenseJson.cs`
**What**: The response with a variable line collection, a recurring line collection and the four totals. Variable lines carry the installment number and count; recurring lines carry expected, actual, due day, `IsEstimate` and status.
**Depends on**: none
**Requirement**: VIEW-01, VIEW-04
**Tests**: compile-only per the coverage matrix; the shape is asserted by T41
**Gate**: `build`

#### T39: Add GetMonthlyExpenseUseCase

**Where**: `src/Balance.Application/UseCases/Expenses/GetMonthly/GetMonthlyExpenseUseCase.cs`
**What**: Loads the month's expenses and non-archived recurring expenses, resolves each version in effect, applies the four-step status rule and computes the totals including the committed total. Rejects an invalid year or month with `REFERENCE_MONTH_INVALID`.
**Depends on**: T38
**Requirement**: VIEW-01, VIEW-02, VIEW-03, VIEW-04
**Tests**: use case layer — a paid bill matching its estimate reporting `Paid`, a differing amount reporting `Divergent`, an unpaid bill reporting `Pending` with a null actual, a month predating every version reporting a null expected, an archived expense omitted, an installment line carrying its number and count, the committed total mixing actuals and estimates, an invalid month rejected, and an empty account returning zeroed totals
**Gate**: `test`

#### T40: Add the monthly expense route

**Where**: `src/Balance.Api/Controllers/ExpenseController.cs`
**What**: `GET /api/expense/{year:int}/{month:int}` with documented responses, plus its DI registration.
**Depends on**: T39
**Requirement**: VIEW-01
**Tests**: endpoint layer — asserted by T41
**Gate**: `build`

#### T41: Add the monthly expense endpoint tests

**Where**: `tests/WebApi.Test/Expenses/GetMonthlyExpenseTest.cs`
**What**: An end-to-end month built through the API — one paid bill, one unpaid bill, one installment expense — read back with the right statuses and totals; a second account seeing none of it; 400 for month 13; 401 without a token.
**Depends on**: T40
**Requirement**: VIEW-01..04
**Tests**: this task is the endpoint-layer coverage for Phase 8
**Gate**: `test`

---

### Phase 9: Dashboard

```
T42 -> T43
T43 -> T44
```

#### T42: Add GetMonthlyDashboardUseCase

**Where**: `src/Balance.Application/UseCases/Dashboard/GetMonthly/GetMonthlyDashboardUseCase.cs`
**What**: Injects `IGetMonthlyIncomeUseCase` and `IGetMonthlyExpenseUseCase`, invokes both for the month and returns `ResponseMonthlyDashboardJson` with both halves and the balance. No income type is edited.
**Depends on**: none
**Requirement**: DASH-01, DASH-02
**Tests**: use case layer — both halves returned unchanged from their mocked use cases, and the balance computed as total income received minus total committed expense
**Gate**: `test`

#### T43: Add the DashboardController

**Where**: `src/Balance.Api/Controllers/DashboardController.cs`
**What**: `[Authorize]` controller with `GET /api/dashboard/{year:int}/{month:int}`, documented, plus its DI registration.
**Depends on**: T42
**Requirement**: DASH-01
**Tests**: endpoint layer — asserted by T44
**Gate**: `build`

#### T44: Add the dashboard endpoint tests

**Where**: `tests/WebApi.Test/Dashboard/GetMonthlyDashboardTest.cs`
**What**: With income and expenses in one month, the dashboard's income half equals what `GET /api/income/{year}/{month}` returns and its expense half equals what `GET /api/expense/{year}/{month}` returns; the balance is their difference; 401 without a token.
**Depends on**: T43
**Requirement**: DASH-01, DASH-02
**Tests**: this task is the endpoint-layer coverage for Phase 9
**Gate**: `test`

---

### Phase 10: Migration, seed and page

```
T45 -> T47
T47 -> T48
T48 -> T49
T46 -> T49
```

#### T45: Generate the AddExpenseTracking migration

**Where**: `src/Balance.Infrastructure/Migrations/`
**What**: An additive migration on top of the committed `InitialCreate`. Read the generated file and confirm it creates seven tables, alters none, and carries the money precision, the indexes and the unique index from T6.
**Depends on**: none
**Requirement**: SHAR-02, RPAY-03
**Tests**: schema-shape layer per the coverage matrix — the generated SQL is the artifact under review
**Gate**: `migration`

#### T46: Add the CORS policy

**Where**: `src/Balance.Api/Program.cs`
**What**: A named policy allowing `http://localhost:5173` with the methods and headers the page needs, applied before authorization. No `AllowAnyOrigin`.
**Depends on**: none
**Requirement**: DASH-01
**Tests**: endpoint layer — the existing suite must stay green through the pipeline change; the policy itself is proved by the page loading in T49
**Gate**: `test`

#### T47: Start PostgreSQL and apply the migrations

**Where**: `docker-compose.yml`
**What**: Bring up the postgres service, start the API and confirm `DataBaseMigration` applies both migrations, leaving a database with every income and expense table present.
**Depends on**: T45
**Requirement**: SHAR-01, EXPN-01, RECR-01
**Tests**: schema-shape layer — the applied schema is inspected directly in the running database
**Gate**: `migration`

#### T48: Add the seeding script

**Where**: `frontend/scripts/seed.mjs`
**What**: A Node 20 script driving the public API: register a user, log in, create a second person, categories of all three priorities, a credit and a debit account, recurring and variable income with payments, recurring expenses with and without a payment, single expenses of all three types, and one installment plan. Idempotent enough to re-run against a fresh database.
**Depends on**: T47
**Requirement**: every requirement — the seed exercises each endpoint
**Tests**: endpoint layer — every call's status code is asserted by the script itself, which fails loudly on a non-2xx
**Gate**: `page` — the seeded month returns non-zero totals from `GET /api/dashboard`

#### T49: Build the frontend page

**Where**: `frontend/src/App.jsx`
**What**: A React + Vite page, created with the NVM Node 20 binary, that logs in with the seeded credentials and renders the dashboard month: fixed income, variable income, fixed (recurring) expenses, variable expenses with their installment markers, and the balance. Estimated figures are visibly marked as such.
**Depends on**: T46, T48
**Requirement**: DASH-01, VIEW-01, VIEW-04
**Tests**: endpoint layer — the page is the manual acceptance check; the data it renders is already asserted by T41 and T44
**Gate**: `page`

---

## Requirement → Task Map

| Requirement | Tasks |
| ----------- | ----- |
| SHAR-01 | T1, T3, T10, T12, T14, T15, T47 |
| SHAR-02 | T3, T6, T11, T13, T14, T15, T45 |
| SHAR-03 | T7, T12, T13, T15 |
| EXPN-01 | T1, T4, T16, T17, T18, T19, T20, T47 |
| EXPN-02 | T8, T17, T18, T20 |
| EXPN-03 | T18, T20 |
| INST-01 | T4, T21, T22, T23, T24 |
| INST-02 | T22, T24 |
| INST-03 | T7, T22, T24 |
| RECR-01 | T5, T25, T26, T27, T30, T31, T47 |
| RECR-02 | T27, T31 |
| RECR-03 | T26, T28, T30, T31 |
| RECR-04 | T28, T31 |
| RECR-05 | T29, T30, T31 |
| RPAY-01 | T5, T32, T33, T34, T36, T37 |
| RPAY-02 | T32, T33, T35, T36, T37 |
| RPAY-03 | T6, T7, T34, T35, T37, T45 |
| VIEW-01 | T16, T38, T39, T40, T41, T49 |
| VIEW-02 | T9, T25, T39, T41 |
| VIEW-03 | T1, T2, T39, T41 |
| VIEW-04 | T38, T39, T41, T49 |
| DASH-01 | T42, T43, T44, T46, T49 |
| DASH-02 | T42, T44 |

All 23 requirements are mapped. T48 exercises every endpoint end to end.
