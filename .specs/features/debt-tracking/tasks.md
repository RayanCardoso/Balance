# Debt Tracking Tasks

> **For agentic workers:** REQUIRED SUB-SKILL: use `superpowers:subagent-driven-development`
> (recommended) or `superpowers:executing-plans` to implement this plan task by task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Spec**: `.specs/features/debt-tracking/spec.md`
**Design**: `.specs/features/debt-tracking/design.md`
**Status**: Awaiting approval

**Goal:** Record what the household owes - to a person, a bank or anyone else - with a monthly
schedule, payments whose method is chosen at payment time, a derived outstanding balance, and the
month's obligation subtracted from the dashboard's balance.

**Architecture:** A vertical slice over the existing Clean Architecture layers. `Debt` →
`DebtInstallment` (expectation) → `DebtPayment` (fact) mirrors the proven `RecurringExpense` anatomy;
`Creditor` is an account-level catalogue like `Category`. The dashboard composes a third use case
interface and nothing else in income or expenses changes behaviour.

**Tech Stack:** .NET 10, EF Core + PostgreSQL, FluentValidation, xUnit + Shouldly + Moq + Bogus.

34 tasks across 10 phases. Every task ends in one atomic commit.

---

## Global Constraints

- **No income file is edited.** AD-006. Nothing in this feature references an income type.
- **No expense file changes behaviour.** `RegisterInstallmentPlanUseCase` is the single existing production file whose body changes, and only to call the extracted calculator. Every existing test must stay green **unedited** - editing one is the signal the refactor was not faithful.
- **`ExpenseStatus` and `ExpenseType` gain no member.** Overdue is a boolean on the line.
- **Money is `decimal` at `(18,2)`.** Months are `DateOnly` normalised to the first of the month.
- **`Restrict` on every relationship.** Nothing cascades away silently.
- **Ownership is filtered in the repository, never in a controller** (AD-003), and a not-owned id answers 404, never 403 (AD-004).
- **Every `Include` of a child collection carries an explicit `OrderBy`.** See the risk table in `design.md`.
- **Tests come first in every task.** Write the failing test, run it, watch it fail for the right reason, then implement. The `Tests` field of each task names the exact cases.

---

## Gate Check Commands

| Gate | Command |
| ---- | ------- |
| `build` | `dotnet build Balance.sln --nologo` — zero errors, zero warnings |
| `test` | `dotnet test Balance.sln --nologo` — every test green, including all 349 pre-existing ones |
| `commit` | `"C:\Program Files\LibreOffice\program\python.exe" .claude/skills/tlc-spec-driven/scripts/check_commit.py --message "<msg>"` |
| `migration` | `dotnet ef migrations list --project src/Balance.Infrastructure --startup-project src/Balance.Api` |

`test` subsumes `build`, so a task whose gate is `test` does not repeat `build`.

The Python that runs the skill validators is LibreOffice's bundled CPython 3.10.19. The `python` and
`python3` names on PATH are Microsoft Store stubs and do not work.

PostgreSQL is mapped to host port **5434**, not 5432 - this machine runs local `postgresql-x64-17`
and `postgresql-x64-18` services that answer `localhost` first. Only the migration gate needs a live
database; every other gate runs on the EF in-memory provider.

---

## Test Coverage Matrix

Which layer proves what. A task's `Tests` field must agree with this table.

| Layer | Project | What it proves | Requirements |
| ----- | ------- | -------------- | ------------ |
| Pure domain rules | `tests/UseCases.Test` (no fixture) | `InstallmentAmountCalculator`, `DebtScheduleBuilder` and `DebtExtensions` in isolation, with awkward totals, short months and boundary due days | DSCH-01, DSCH-02, DBAL-01 |
| Use case | `tests/UseCases.Test` | Business rules with mocked repositories: transactions, schedule generation, reference-month derivation, status and overdue resolution, ownership 404s | CRED-02, DEBT-01..03, DSCH-01..03, DPAY-01..03, DVEW-01..03, DBAL-01..03, DDSH-01 |
| Validator | `tests/Validators.Tests` | Field-level rules and their message keys, in both cultures | CRED-03, DEBT-03, DPAY-04 |
| Endpoint | `tests/WebApi.Test` | Status codes, JSON shape, `[Authorize]`, cross-account isolation, end-to-end month reconciliation | every requirement |
| Schema shape | none — asserted by the migration gate | Precision, indexes and delete behaviour. The EF in-memory provider ignores unique indexes and precision, so these are read off the generated migration, and every rule they back is *also* enforced in a use case and unit-tested there | DPAY-01 |
| Entities, enums, contracts | none — compile-only | Data shapes carry no behaviour; the build gate is the whole check | — |

---

## Execution Plan

| Phase | Theme | Tasks | Batch |
| ----- | ----- | ----- | ----- |
| 1 | Schema foundation | T1–T4 | 1 |
| 2 | Domain rules and messages | T5–T8 | 1 |
| 3 | Repositories | T9–T10 | 2 |
| 4 | Creditor slice | T11–T15 | 2 |
| 5 | Debt registration | T16–T19 | 3 |
| 6 | Debt payments | T20–T23 | 4 |
| 7 | Debt reads | T24–T28 | 5 |
| 8 | Monthly debt view | T29–T32 | 6 |
| 9 | Dashboard | T33 | 6 |
| 10 | Migration | T34 | 7 — run inline, not delegated |

Phase 10 stays with the orchestrator: it needs Docker and a live database rather than a worker's
sandbox.

---

## Task Breakdown

### Phase 1: Schema foundation

```
T1 -> T2
T1 -> T3
T3 -> T4
```

#### T1: Add the domain debt enums

**Where**: `src/Balance.Domain/Enums/CreditorType.cs`, `src/Balance.Domain/Enums/DebtMode.cs`
**What**: `CreditorType { Person = 0, Institution = 1, Other = 2 }` and
`DebtMode { Scheduled = 0, OpenEnded = 1 }`, one file each, matching the existing `ExpenseType` style.
Do **not** add a member to `ExpenseStatus` or `ExpenseType`.
**Depends on**: none
**Requirement**: CRED-01, DEBT-01
**Tests**: compile-only per the coverage matrix; the values are asserted wherever they are consumed
**Gate**: `build`
**Commit**: `feat: add creditor type and debt mode enums`

#### T2: Mirror the debt enums in Communication

**Where**: `src/Balance.Communication/Enums/CreditorType.cs`, `src/Balance.Communication/Enums/DebtMode.cs`
**What**: The same two enums with identical member order, so the existing `(CommunicationX)domainX`
cast convention stays valid.
**Depends on**: T1
**Requirement**: CRED-01, DEBT-01
**Tests**: compile-only; the casts are exercised by every endpoint test from T15 on
**Gate**: `build`
**Commit**: `feat: mirror the debt enums in the communication layer`

#### T3: Add the four debt entities

**Where**: `src/Balance.Domain/Entities/Creditor.cs`, `Debt.cs`, `DebtInstallment.cs`, `DebtPayment.cs`
**What**: Exactly the four classes in the design's Data Models section, all `: BaseEntity`, including
the nullable `DueDay`, `InstallmentCount`, `EndMonth` on `Debt`, the nullable `DebtInstallmentId`,
`Type` and `AccountId` on `DebtPayment`, and the two distinct references `CreditorId` (who is owed)
and `PersonId` (who owes). No `OutstandingBalance` or `Settled` property - both are derived in T7.
**Depends on**: T1
**Requirement**: CRED-01, DEBT-01, DSCH-01, DPAY-01
**Tests**: compile-only; persistence is asserted by T15, T19 and T23
**Gate**: `build`
**Commit**: `feat: add the creditor, debt, installment and payment entities`

#### T4: Configure the four entities in BalanceDbContext

**Where**: `src/Balance.Infrastructure/DataAccess/BalanceDbContext.cs`
**What**: Four `DbSet` properties and one configuration block per entity, appended after the
recurring-expense block. `HasPrecision(18, 2)` on `PrincipalAmount`, `TotalAmount`, `ExpectedAmount`
and `AmountPaid`; `Restrict` on every relationship; indexes on `Creditor.UserId`, `Debt.CreditorId`,
`Debt.PersonId`, `(DebtInstallment.DebtId, ReferenceMonth)` and `(DebtPayment.DebtId, ReferenceMonth)`;
a **unique** index on `DebtPayment.DebtInstallmentId`. The income and expense blocks are not touched.
**Depends on**: T3
**Requirement**: DEBT-01, DPAY-01
**Tests**: schema-shape layer per the coverage matrix — verified by reading the migration in T34; the
uniqueness rule it backs is separately enforced and unit-tested in T21
**Gate**: `test` — all 349 existing tests must stay green through the context change
**Commit**: `feat: configure the debt entities in the database context`

---

### Phase 2: Domain rules and messages

```
T5 -> (nothing in this phase)
T6, T7, T8 have no intra-phase dependency
```

#### T5: Extract the installment rounding rule and reuse it

**Where**: create `src/Balance.Domain/Extensions/InstallmentAmountCalculator.cs`; modify
`src/Balance.Application/UseCases/Expenses/RegisterInstallmentPlan/RegisterInstallmentPlanUseCase.cs`;
create `tests/UseCases.Test/Domain/InstallmentAmountCalculatorTest.cs`
**What**: `public static IReadOnlyList<decimal> Split(decimal total, int count)` carrying the rule
verbatim from `RegisterInstallmentPlanUseCase.BuildInstallments` - parts 1..N-1 are
`Math.Round(total / count, 2, MidpointRounding.AwayFromZero)`, part N is the residual. Then rewrite
`BuildInstallments` to call it and delete the inline arithmetic.
**Depends on**: none
**Requirement**: DSCH-01
**Tests**: `Split` returns amounts summing exactly to the total for `100.00 / 3`, `1500.00 / 10`,
`1000.00 / 7`, `0.05 / 3` and `999.99 / 4`; the last element carries the residual; `count = 1` returns
the total unchanged. **Plus**: every pre-existing installment-plan test passes with no edit - that is
the proof the extraction was faithful, and editing one to accommodate it fails this task.
**Gate**: `test`
**Commit**: `refactor: share the installment rounding rule between plans and debts`

#### T6: Add the debt schedule builder

**Where**: create `src/Balance.Domain/Extensions/DebtScheduleBuilder.cs`;
create `tests/UseCases.Test/Domain/DebtScheduleBuilderTest.cs`
**What**: `FirstCompetenceMonth(DateOnly startDate, int dueDay)` returning the month of `startDate`
normalised to its first day when `startDate.Day <= dueDay`, and the following month otherwise;
`DueDateIn(DateOnly competenceMonth, int dueDay)` returning that month's day `dueDay`, clamped to the
month's length. `CompetenceMonthResolver` is **not** called and **not** modified - it answers a
card-invoice question and this is a due-day question.
**Depends on**: none
**Requirement**: DSCH-02
**Tests**: start 2026-03-20 with due day 10 → first month 2026-04-01; start 2026-03-05 with due day 10
→ 2026-03-01; start 2026-03-10 with due day 10 → 2026-03-01 (the boundary is inclusive);
`DueDateIn(2026-02-01, 31)` → 2026-02-28; `DueDateIn(2024-02-01, 31)` → 2024-02-29;
`DueDateIn(2026-04-01, 31)` → 2026-04-30; `DueDateIn(2026-03-01, 10)` → 2026-03-10
**Gate**: `test`
**Commit**: `feat: derive a debt schedule from its start date and due day`

#### T7: Add the derived balance and settled rules

**Where**: create `src/Balance.Domain/Extensions/DebtExtensions.cs`;
create `tests/UseCases.Test/Domain/DebtExtensionsTest.cs`
**What**: `OutstandingBalance(this Debt debt)` returning `TotalAmount` minus the sum of
`debt.Payments.Select(p => p.AmountPaid)`, and `IsSettled(this Debt debt)` returning
`OutstandingBalance() <= 0`. No persisted field is added.
**Depends on**: T3
**Requirement**: DBAL-01
**Tests**: a debt of 1500 with no payments reports 1500 and is not settled; with payments of 150 and
150 reports 1200; with payments summing exactly to the total reports 0 and **is** settled; with an
overpayment reports a negative balance and is settled; a debt whose `Payments` collection is empty
does not throw
**Gate**: `test`
**Commit**: `feat: derive a debt outstanding balance from its payments`

#### T8: Add the eight new error-message keys

**Where**: `src/Balance.Exception/ResourceErrorMessages.resx`,
`src/Balance.Exception/ResourceErrorMessages.pt-BR.resx`,
`src/Balance.Exception/ResourceErrorMessages.cs`
**What**: `CREDITOR_NOT_FOUND`, `DEBT_NOT_FOUND`, `DEBT_INSTALLMENT_NOT_FOUND`,
`DEBT_PAYMENT_NOT_FOUND`, `DEBT_ARCHIVED`, `TOTAL_LESS_THAN_PRINCIPAL`, `SCHEDULE_REQUIRED`,
`SCHEDULE_NOT_ALLOWED`, added to both `.resx` files and the accessor. Existing keys are reused, not
duplicated - in particular `ACCOUNT_REQUIRED_FOR_CREDIT`, `PAYMENT_ALREADY_RECORDED`,
`AMOUNT_GREATER_THAN_ZERO`, `NAME_REQUIRED`, `DAY_OUT_OF_RANGE`, `INSTALLMENT_COUNT_INVALID` and
`REFERENCE_MONTH_INVALID` already exist.
**Depends on**: none
**Requirement**: CRED-03, DEBT-03, DPAY-04
**Tests**: compile-only here; each key's wording in both cultures is asserted by the validator tests
in T12, T17 and T21
**Gate**: `build`
**Commit**: `feat: add the debt error message keys`

---

### Phase 3: Repositories

```
T9, T10 have no intra-phase dependency
```

#### T9: Add the creditor repositories

**Where**: create `src/Balance.Domain/Repositories/Creditors/ICreditorReadOnlyRepository.cs`,
`ICreditorWriteOnlyRepository.cs`, `ICreditorUpdateOnlyRepository.cs`; create
`src/Balance.Infrastructure/DataAccess/Repositories/CreditorRepository.cs`; modify
`src/Balance.Infrastructure/DependencyInjectionExtension.cs`
**What**: `GetAll(User user, bool includeArchived)` filtering on `UserId` and excluding archived
unless asked, ordered by `Name`; `GetById(User, Guid)` returning null when not owned; `Add(Creditor)`;
a tracked `GetById` on the update-only interface. One class implements all three, registered scoped,
following `CategoryRepository`.
**Depends on**: T3
**Requirement**: CRED-02
**Tests**: use-case layer covers behaviour from T13; the DI registration is proven by every endpoint
test from T15 resolving the controller
**Gate**: `test`
**Commit**: `feat: add the creditor repositories`

#### T10: Add the debt repositories

**Where**: create `src/Balance.Domain/Repositories/Debts/IDebtReadOnlyRepository.cs`,
`IDebtWriteOnlyRepository.cs`, `IDebtUpdateOnlyRepository.cs`,
`IDebtInstallmentWriteOnlyRepository.cs`, `IDebtPaymentRepository.cs`; create
`src/Balance.Infrastructure/DataAccess/Repositories/DebtRepository.cs`,
`DebtPaymentRepository.cs`; modify
`src/Balance.Infrastructure/DependencyInjectionExtension.cs`
**What**: The eight methods listed in the design's repository table. Every read filters on
`Debt.Person.UserId`. Every `Include(d => d.Installments)` carries
`.OrderBy(installment => installment.Number)` and every `Include(d => d.Payments)` carries
`.OrderBy(payment => payment.PaymentDate)`; `GetForMonth` filters `Installments` to the requested
`ReferenceMonth` inside the `Include` and excludes archived debts. `GetById` includes `Creditor`,
`Category`, `Installments` and `Payments` - `OutstandingBalance` silently reports the full total if
`Payments` is missing.
**Depends on**: T3
**Requirement**: DEBT-02, DPAY-02, DVEW-01, DBAL-01, DBAL-02
**Tests**: use-case layer covers behaviour from T18 on; the ordering guarantee is asserted at the
endpoint layer in T19 with a fixture whose installments are inserted deliberately out of order
**Gate**: `test`
**Commit**: `feat: add the debt repositories`

---

### Phase 4: Creditor slice

```
T11 -> T12 -> T15
T11 -> T13 -> T15
T11 -> T14 -> T15
```

#### T11: Add the creditor contracts

**Where**: create `src/Balance.Communication/Requests/RequestRegisterCreditorJson.cs`;
`src/Balance.Communication/Responses/ResponseCreditorJson.cs`, `ResponseCreditorsJson.cs`,
`ResponseCreditorSummaryJson.cs`
**What**: The four DTOs in the design's Requests and Responses section. `ResponseCreditorSummaryJson`
carries `Creditor`, `UnsettledDebtCount`, `TotalOwed`, `TotalPaid`, `OutstandingBalance`.
**Depends on**: T2
**Requirement**: CRED-01, DBAL-03
**Tests**: compile-only; shapes are asserted by T15
**Gate**: `build`
**Commit**: `feat: add the creditor request and response contracts`

#### T12: Register a creditor

**Where**: create `src/Balance.Application/UseCases/Creditors/Register/IRegisterCreditorUseCase.cs`,
`RegisterCreditorUseCase.cs`, `RegisterCreditorValidator.cs`; create
`tests/UseCases.Test/Creditors/Register/RegisterCreditorUseCaseTest.cs`,
`tests/Validators.Tests/Creditors/RegisterCreditorValidatorTest.cs`; modify
`src/Balance.Application/DependencyInjectionExtension.cs`;
create `tests/CommonTestUtilities/Entities/CreditorBuilder.cs`,
`tests/CommonTestUtilities/Requests/RequestRegisterCreditorJsonBuilder.cs`
**What**: Validate, persist against `loggedUser.Id`, `Commit()`, return `ResponseCreditorJson`.
Contact and notes are optional.
**Depends on**: T8, T9, T11
**Requirement**: CRED-01, CRED-03
**Tests**: use case persists with the logged user's id and returns the creditor; a null contact and a
null notes are accepted; two creditors of the same user may share a name. Validator: an empty name
yields `NAME_REQUIRED`, asserted in both the invariant and the `pt-BR` culture
**Gate**: `test`
**Commit**: `feat: register a creditor`

#### T13: List creditors

**Where**: create `src/Balance.Application/UseCases/Creditors/GetAll/IGetAllCreditorsUseCase.cs`,
`GetAllCreditorsUseCase.cs`; create `tests/UseCases.Test/Creditors/GetAll/GetAllCreditorsUseCaseTest.cs`
**What**: Return the logged user's creditors, excluding archived ones unless `includeArchived` is
true.
**Depends on**: T9, T11
**Requirement**: CRED-02
**Tests**: returns only the logged user's creditors; excludes an archived creditor by default;
includes it when the flag is set; an empty catalogue returns an empty list, not null
**Gate**: `test`
**Commit**: `feat: list the creditors of the logged user`

#### T14: Archive a creditor

**Where**: create `src/Balance.Application/UseCases/Creditors/Archive/IArchiveCreditorUseCase.cs`,
`ArchiveCreditorUseCase.cs`; create `tests/UseCases.Test/Creditors/Archive/ArchiveCreditorUseCaseTest.cs`
**What**: `Execute(Guid id, bool archived)` sets or clears `Archived` and commits; a creditor not
owned by the logged user throws `NotFoundException` with `CREDITOR_NOT_FOUND`.
**Depends on**: T8, T9
**Requirement**: CRED-02
**Tests**: sets `Archived` and commits; clears it when called with false; a foreign id throws
`NotFoundException` carrying `CREDITOR_NOT_FOUND`
**Gate**: `test`
**Commit**: `feat: archive and unarchive a creditor`

#### T15: Expose the creditor endpoints

**Where**: create `src/Balance.Api/Controllers/CreditorController.cs`; create
`tests/WebApi.Test/Creditors/RegisterCreditorTest.cs`, `GetAllCreditorsTest.cs`, `ArchiveCreditorTest.cs`
**What**: `POST api/creditor` → 201, `GET api/creditor` → 200 with `[FromQuery] bool includeArchived = false`,
`PUT api/creditor/{id:guid}/archive` → 204 with `[FromQuery] bool archived = true`. Every response
type documented with `ProducesResponseType`, matching `RecurringExpenseController`.
**Depends on**: T12, T13, T14
**Requirement**: CRED-01, CRED-02, CRED-03
**Tests**: 201 with the persisted shape; 400 with `NAME_REQUIRED` in both cultures on an empty name;
401 on every route with no bearer token; a second account's creditor is absent from the list and its
id answers 404 on archive
**Gate**: `test`
**Commit**: `feat: expose the creditor endpoints`

---

### Phase 5: Debt registration

```
T16 -> T17 -> T18 -> T19
```

#### T16: Add the debt contracts

**Where**: create `src/Balance.Communication/Requests/RequestRegisterDebtJson.cs`;
`src/Balance.Communication/Responses/ResponseDebtJson.cs`, `ResponseDebtsJson.cs`,
`ResponseDebtInstallmentJson.cs`
**What**: The DTOs in the design's Requests and Responses section. `ResponseDebtJson` carries
`OutstandingBalance` and `IsSettled` as computed values and the `Installments` collection.
`RequestRegisterDebtJson` carries nullable `InstallmentCount` and `DueDay`.
**Depends on**: T2
**Requirement**: DEBT-01, DSCH-01, DSCH-03
**Tests**: compile-only; shapes are asserted by T19
**Gate**: `build`
**Commit**: `feat: add the debt request and response contracts`

#### T17: Validate a debt registration

**Where**: create
`src/Balance.Application/UseCases/Debts/Register/RegisterDebtValidator.cs`; create
`tests/Validators.Tests/Debts/RegisterDebtValidatorTest.cs`; create
`tests/CommonTestUtilities/Requests/RequestRegisterDebtJsonBuilder.cs`
**What**: Name not empty → `NAME_REQUIRED`; `PrincipalAmount > 0` and `TotalAmount > 0` →
`AMOUNT_GREATER_THAN_ZERO`; `TotalAmount >= PrincipalAmount` → `TOTAL_LESS_THAN_PRINCIPAL`;
when `Mode == Scheduled`, `InstallmentCount` and `DueDay` both present → `SCHEDULE_REQUIRED`, with
`InstallmentCount >= 1` → `INSTALLMENT_COUNT_INVALID` and `DueDay` in 1..31 → `DAY_OUT_OF_RANGE`;
when `Mode == OpenEnded`, either field present → `SCHEDULE_NOT_ALLOWED`.
**Depends on**: T8, T16
**Requirement**: DEBT-03, DSCH-03
**Tests**: one case per rule above, each asserting the exact message key, run in both the invariant
and the `pt-BR` culture; a valid scheduled request and a valid open-ended request both pass;
`TotalAmount == PrincipalAmount` passes (the boundary is inclusive)
**Gate**: `test`
**Commit**: `feat: validate a debt registration`

#### T18: Register a debt and generate its schedule

**Where**: create `src/Balance.Application/UseCases/Debts/Register/IRegisterDebtUseCase.cs`,
`RegisterDebtUseCase.cs`; create `tests/UseCases.Test/Debts/Register/RegisterDebtUseCaseTest.cs`;
create `tests/CommonTestUtilities/Entities/DebtBuilder.cs`; modify `src/Balance.Application/DependencyInjectionExtension.cs`
**What**: Validate; resolve creditor, person and category through their read repositories, each
throwing `NotFoundException` with the key naming **that** entity; for `Scheduled`, take the first
competence month from `DebtScheduleBuilder.FirstCompetenceMonth`, split the total with
`InstallmentAmountCalculator.Split`, build N installments advancing one month each with
`DebtScheduleBuilder.DueDateIn` for the due date, and set `EndMonth` to installment N's month; for
`OpenEnded`, create no installments and leave `DueDay`, `InstallmentCount` and `EndMonth` null. One
`Commit()` for the debt and its installments together. No income record is created.
**Depends on**: T5, T6, T10, T17
**Requirement**: DEBT-01, DEBT-02, DEBT-03, DSCH-01, DSCH-02, DSCH-03
**Tests**: 1500.00 over 10 from 2026-03-20 with due day 10 produces 10 installments of 150.00 whose
first `ReferenceMonth` is 2026-04-01 and whose last is 2027-01-01, with `EndMonth` equal to the last;
1000.00 over 3 produces 333.33 / 333.33 / 333.34 summing exactly to the total; the same start date
with due day 25 puts installment 1 in 2026-03-01; a due day of 31 gives the February installment a
due date of the 28th; `OpenEnded` persists zero installments and null schedule fields; a foreign
creditor throws `NotFoundException` with `CREDITOR_NOT_FOUND`, a foreign person with
`PERSON_NOT_FOUND`, a foreign category with `CATEGORY_NOT_FOUND`; `Commit` is called exactly once
**Gate**: `test`
**Commit**: `feat: register a debt and generate its installment schedule`

#### T19: Expose the debt registration endpoint

**Where**: create `src/Balance.Api/Controllers/DebtController.cs`; create
`tests/WebApi.Test/Debts/RegisterDebtTest.cs`
**What**: `POST api/debt` → 201, documenting 400, 404 and 401. The controller holds nothing but the
use-case call.
**Depends on**: T18
**Requirement**: DEBT-01, DEBT-02, DSCH-01, DSCH-03
**Tests**: 201 carrying ten installments in ascending `Number` order **from a fixture whose
installments were inserted out of order**, which is what proves the repository's `OrderBy` rather
than the provider's luck; 400 with `TOTAL_LESS_THAN_PRINCIPAL`; 400 with `SCHEDULE_NOT_ALLOWED` for
an open-ended request carrying a due day; 404 for another account's creditor; 401 with no token
**Gate**: `test`
**Commit**: `feat: expose the debt registration endpoint`

---

### Phase 6: Debt payments

```
T20 -> T21 -> T23
T20 -> T22 -> T23
```

#### T20: Add the debt payment contracts

**Where**: create `src/Balance.Communication/Requests/RequestRegisterDebtPaymentJson.cs`,
`RequestUpdateDebtPaymentJson.cs`; `src/Balance.Communication/Responses/ResponseDebtPaymentJson.cs`
**What**: As in the design. `RequestRegisterDebtPaymentJson` carries `DebtId`, optional
`DebtInstallmentId`, `PaymentDate`, `AmountPaid`, optional `Type`, optional `AccountId`, optional
`Notes` - and **no reference month**, which is derived. The update request carries no `DebtId` and no
installment: neither may be moved.
**Depends on**: T2
**Requirement**: DPAY-01, DPAY-03
**Tests**: compile-only; shapes are asserted by T23
**Gate**: `build`
**Commit**: `feat: add the debt payment contracts`

#### T21: Record a debt payment

**Where**: create
`src/Balance.Application/UseCases/Debts/RegisterPayment/IRegisterDebtPaymentUseCase.cs`,
`RegisterDebtPaymentUseCase.cs`, `RegisterDebtPaymentValidator.cs`; create
`tests/UseCases.Test/Debts/RegisterPayment/RegisterDebtPaymentUseCaseTest.cs`,
`tests/Validators.Tests/Debts/RegisterDebtPaymentValidatorTest.cs`; create
`tests/CommonTestUtilities/Requests/RequestRegisterDebtPaymentJsonBuilder.cs`; modify `src/Balance.Application/DependencyInjectionExtension.cs`
**What**: Resolve the debt (404 `DEBT_NOT_FOUND` when not owned); reject an archived debt with
`DEBT_ARCHIVED`; when an installment id is supplied, confirm it belongs to that debt (404
`DEBT_INSTALLMENT_NOT_FOUND` otherwise), probe `GetByInstallment` and reject a second payment with
`PAYMENT_ALREADY_RECORDED`, and copy the installment's `ReferenceMonth`; when none is supplied,
derive the reference month from `PaymentDate.FirstDayOfMonth()`. Resolve the account against the
logged user when supplied. Validator: `AmountPaid > 0` → `AMOUNT_GREATER_THAN_ZERO`, and
`Type == Credit` with a null `AccountId` → `ACCOUNT_REQUIRED_FOR_CREDIT`.
**Depends on**: T8, T10, T20
**Requirement**: DPAY-01, DPAY-02, DPAY-04
**Tests**: a scheduled payment takes its reference month from the installment and ignores any month
in the request; an open-ended payment leaves `DebtInstallmentId` null and derives the month from the
payment date; a null type and a null account are accepted; an account belonging to a different person
of the same user is accepted; a second payment on the same installment throws with
`PAYMENT_ALREADY_RECORDED`; an archived debt throws with `DEBT_ARCHIVED`; an installment of another
debt throws with `DEBT_INSTALLMENT_NOT_FOUND`; a foreign account throws with `ACCOUNT_NOT_FOUND`.
Validator: zero and negative amounts yield `AMOUNT_GREATER_THAN_ZERO`; `Credit` with no account
yields `ACCOUNT_REQUIRED_FOR_CREDIT`; `Debit` and `Pix` with no account pass — all in both cultures
**Gate**: `test`
**Commit**: `feat: record a payment against a debt`

#### T22: Correct a recorded debt payment

**Where**: create
`src/Balance.Application/UseCases/Debts/UpdatePayment/IUpdateDebtPaymentUseCase.cs`,
`UpdateDebtPaymentUseCase.cs`; create
`tests/UseCases.Test/Debts/UpdatePayment/UpdateDebtPaymentUseCaseTest.cs`
**What**: `Execute(Guid id, RequestUpdateDebtPaymentJson request)` overwrites `AmountPaid`,
`PaymentDate`, `Type`, `AccountId` and `Notes` and commits. `ReferenceMonth`, `DebtId` and
`DebtInstallmentId` are never written. A payment not owned by the logged user throws
`NotFoundException` with `DEBT_PAYMENT_NOT_FOUND`. The same credit-without-account rule applies.
**Depends on**: T8, T10, T20
**Requirement**: DPAY-03, DPAY-04
**Tests**: the amount, date, type, account and notes are overwritten; the reference month and the
installment id are unchanged after the call, asserted explicitly rather than implied; clearing the
type to null is persisted as null; a foreign payment throws with `DEBT_PAYMENT_NOT_FOUND`; correcting
to `Credit` with no account throws with `ACCOUNT_REQUIRED_FOR_CREDIT`
**Gate**: `test`
**Commit**: `feat: correct a recorded debt payment`

#### T23: Expose the debt payment endpoints

**Where**: modify `src/Balance.Api/Controllers/DebtController.cs`; create
`tests/WebApi.Test/Debts/RegisterDebtPaymentTest.cs`, `UpdateDebtPaymentTest.cs`
**What**: `POST api/debt/payment` → 201 and `PUT api/debt/payment/{id:guid}` → 200, both documenting
400, 404 and 401.
**Depends on**: T21, T22
**Requirement**: DPAY-01, DPAY-02, DPAY-03, DPAY-04
**Tests**: paying installment 1 by pix with no account returns 201 and the debt's
`OutstandingBalance` drops by exactly that amount on a follow-up read; paying by credit with no
account returns 400 with `ACCOUNT_REQUIRED_FOR_CREDIT`; a second payment on the same installment
returns 400 with `PAYMENT_ALREADY_RECORDED`; another account's payment id returns 404 on update; 401
with no token
**Gate**: `test`
**Commit**: `feat: expose the debt payment endpoints`

---

### Phase 7: Debt reads

```
T24, T25, T26, T27 have no intra-phase dependency
T24, T25, T26, T27 -> T28
```

#### T24: Read one debt

**Where**: create `src/Balance.Application/UseCases/Debts/GetById/IGetDebtByIdUseCase.cs`,
`GetDebtByIdUseCase.cs`; create `tests/UseCases.Test/Debts/GetById/GetDebtByIdUseCaseTest.cs`
**What**: Return `ResponseDebtJson` with creditor name and type, category name, the full schedule,
the payments, and `OutstandingBalance` / `IsSettled` from `DebtExtensions`. 404 `DEBT_NOT_FOUND` when
not owned.
**Depends on**: T7, T10, T16
**Requirement**: DBAL-01
**Tests**: a debt with two payments reports the reduced balance and is not settled; a fully paid debt
reports zero and is settled; the installments come back ordered by `Number` and the payments by
`PaymentDate`; the creditor name and type are carried through; a foreign id throws with
`DEBT_NOT_FOUND`
**Gate**: `test`
**Commit**: `feat: read one debt with its schedule and balance`

#### T25: List debts

**Where**: create `src/Balance.Application/UseCases/Debts/GetAll/IGetAllDebtsUseCase.cs`,
`GetAllDebtsUseCase.cs`; create `tests/UseCases.Test/Debts/GetAll/GetAllDebtsUseCaseTest.cs`
**What**: `Execute(Guid? creditorId, Guid? personId, bool includeInactive)`. Archived and settled
debts are excluded unless `includeInactive` is true - settled is evaluated with `IsSettled()` after
the read, since it is derived and cannot be filtered in SQL.
**Depends on**: T7, T10, T16
**Requirement**: DBAL-02
**Tests**: returns only the logged user's debts; filters by creditor; filters by person; excludes an
archived debt by default and includes it with the flag; excludes a settled debt by default and
includes it with the flag; an unsettled and an archived debt of the same creditor are told apart
**Gate**: `test`
**Commit**: `feat: list debts with creditor and person filters`

#### T26: Archive a debt

**Where**: create `src/Balance.Application/UseCases/Debts/Archive/IArchiveDebtUseCase.cs`,
`ArchiveDebtUseCase.cs`; create `tests/UseCases.Test/Debts/Archive/ArchiveDebtUseCaseTest.cs`
**What**: `Execute(Guid id, bool archived)` sets or clears `Archived` and commits; 404
`DEBT_NOT_FOUND` when not owned.
**Depends on**: T10
**Requirement**: DBAL-02
**Tests**: sets `Archived` and commits; clears it when called with false; a foreign id throws with
`DEBT_NOT_FOUND`; the debt's payments are untouched by the call
**Gate**: `test`
**Commit**: `feat: archive and unarchive a debt`

#### T27: Summarise what is owed to one creditor

**Where**: create `src/Balance.Application/UseCases/Creditors/GetSummary/IGetCreditorSummaryUseCase.cs`,
`GetCreditorSummaryUseCase.cs`; create
`tests/UseCases.Test/Creditors/GetSummary/GetCreditorSummaryUseCaseTest.cs`
**What**: Resolve the creditor (404 `CREDITOR_NOT_FOUND`), read its debts through
`IDebtReadOnlyRepository.GetByCreditor`, and return the count of unsettled debts, the sum of their
`TotalAmount`, the sum of their payments and the resulting outstanding balance. Settled and archived
debts are excluded from all four figures.
**Depends on**: T7, T9, T10, T11
**Requirement**: DBAL-03
**Tests**: two debts against one creditor, one settled, produce a count of 1 and an outstanding
balance equal to the unsettled debt's remainder alone; a creditor with no debts returns zeroes rather
than null; an archived debt is excluded; a foreign creditor throws with `CREDITOR_NOT_FOUND`
**Gate**: `test`
**Commit**: `feat: summarise the debts owed to one creditor`

#### T28: Expose the debt and creditor read endpoints

**Where**: modify `src/Balance.Api/Controllers/DebtController.cs`,
`src/Balance.Api/Controllers/CreditorController.cs`; create
`tests/WebApi.Test/Debts/GetDebtByIdTest.cs`, `GetAllDebtsTest.cs`, `ArchiveDebtTest.cs`,
`tests/WebApi.Test/Creditors/GetCreditorSummaryTest.cs`
**What**: `GET api/debt/{id:guid}` → 200, `GET api/debt` → 200 with `creditorId`, `personId` and
`includeInactive` from the query, `PUT api/debt/{id:guid}/archive` → 204,
`GET api/creditor/{id:guid}/summary` → 200.
**Depends on**: T24, T25, T26, T27
**Requirement**: DBAL-01, DBAL-02, DBAL-03
**Tests**: the detail route returns the computed balance and settled flag; the list route honours
each filter and the inactive flag; archiving removes the debt from the default list; the summary
route matches the sum of the unsettled debts' remainders; another account's ids answer 404 on all
four; 401 with no token
**Gate**: `test`
**Commit**: `feat: expose the debt and creditor read endpoints`

---

### Phase 8: Monthly debt view

```
T29 -> T30 -> T31 -> T32
```

#### T29: Add the monthly debt contracts

**Where**: create `src/Balance.Communication/Responses/ResponseMonthlyDebtLineJson.cs`,
`ResponseMonthlyDebtJson.cs`
**What**: As in the design, including the nullable `InstallmentNumber`, `InstallmentCount`, `DueDate`
and `ExpectedAmount` that an open-ended line leaves empty, the `Status` of the existing
`Balance.Communication.Enums.ExpenseStatus` type, and the `IsOverdue` boolean.
`ResponseMonthlyDebtJson` carries `CompetenceMonth`, `Lines`, `TotalExpected`, `TotalPaid`,
`TotalCommitted`.
**Depends on**: T2
**Requirement**: DVEW-01, DVEW-02, DVEW-03
**Tests**: compile-only; shapes are asserted by T32
**Gate**: `build`
**Commit**: `feat: add the monthly debt response contracts`

#### T30: Build a monthly debt line

**Where**: create `src/Balance.Application/UseCases/Debts/GetMonthly/GetMonthlyDebtUseCase.cs` with
its private line builders and `ResolveStatus`; create
`tests/UseCases.Test/Debts/GetMonthly/GetMonthlyDebtLineTest.cs`
**What**: The scheduled builder maps an installment plus its payment; the open-ended builder maps a
payment alone. `ResolveStatus(expected, actual)` reproduces the three-branch rule from
`GetMonthlyExpenseUseCase` - null actual is `Pending`, null expected with an actual is `Paid`,
otherwise equal is `Paid` and different is `Divergent`. `IsOverdue` is computed from a `today`
**parameter**, never from `DateTime.UtcNow` read inside the builder.
**Depends on**: T10, T29
**Requirement**: DVEW-01, DVEW-02
**Tests**: an installment with no payment is `Pending` with a null `AmountPaid`; a payment equal to
the expected amount is `Paid`; a different amount is `Divergent`; a `Pending` line whose `DueDate` is
before the supplied `today` has `IsOverdue` true; the same line with a payment has it false; a
`Pending` line whose due date is exactly `today` has it false; an open-ended line carries a null
`ExpectedAmount`, a null `InstallmentNumber`, `Paid` and `IsOverdue` false; the creditor name, type
and category name are carried through
**Gate**: `test`
**Commit**: `feat: build the monthly line of a debt installment`

#### T31: Assemble the month and its totals

**Where**: modify `src/Balance.Application/UseCases/Debts/GetMonthly/GetMonthlyDebtUseCase.cs`; create
`src/Balance.Application/UseCases/Debts/GetMonthly/IGetMonthlyDebtUseCase.cs`; create
`tests/UseCases.Test/Debts/GetMonthly/GetMonthlyDebtUseCaseTest.cs`; modify `src/Balance.Application/DependencyInjectionExtension.cs`
**What**: `Execute(int year, int month)` validates the month exactly as `GetMonthlyExpenseUseCase`
does (400 `REFERENCE_MONTH_INVALID`), resolves `today` **once** as
`DateOnly.FromDateTime(DateTime.UtcNow)`, reads through `GetForMonth`, builds the lines, and sums:
`TotalExpected` over scheduled lines only, `TotalPaid` over every recorded payment, and
`TotalCommitted` as paid-when-present-else-expected per line.
**Depends on**: T30
**Requirement**: DVEW-01, DVEW-03
**Tests**: month 13 and year 0 throw `ErrorOnValidationException` with `REFERENCE_MONTH_INVALID`; a
month with no lines returns empty lines and three zeroed totals; a month with one unpaid installment
of 150 reports `TotalExpected` 150, `TotalPaid` 0 and `TotalCommitted` 150; once paid at 140 it
reports 150 / 140 / 140; an open-ended payment of 100 in the month adds 100 to `TotalPaid` and
`TotalCommitted` and nothing to `TotalExpected`; an archived debt contributes no line and no total
**Gate**: `test`
**Commit**: `feat: report the debt obligations and totals of a month`

#### T32: Expose the monthly debt endpoint

**Where**: modify `src/Balance.Api/Controllers/DebtController.cs`; create
`tests/WebApi.Test/Debts/GetMonthlyDebtTest.cs`
**What**: `GET api/debt/{year:int}/{month:int}` → 200, documenting 400 and 401.
**Depends on**: T31
**Requirement**: DVEW-01, DVEW-02, DVEW-03
**Tests**: with a ten-installment debt paid only in month 1, month 1 reads `Paid` and month 2 reads
`Pending` with `TotalCommitted` equal to the expected amount; an invalid month returns 400 with
`REFERENCE_MONTH_INVALID`; an empty month returns 200 with zeroed totals; 401 with no token
**Gate**: `test`
**Commit**: `feat: expose the monthly debt endpoint`

---

### Phase 9: Dashboard

#### T33: Compose the debts into the dashboard

**Where**: modify
`src/Balance.Application/UseCases/Dashboard/GetMonthly/GetMonthlyDashboardUseCase.cs`,
`src/Balance.Communication/Responses/ResponseMonthlyDashboardJson.cs`; modify
`tests/UseCases.Test/Dashboard/GetMonthlyDashboardUseCaseTest.cs`; modify
`tests/WebApi.Test/Dashboard/GetMonthlyDashboardTest.cs`
**What**: One more constructor dependency, `IGetMonthlyDebtUseCase`, invoked through its interface;
one more response property, `Debts`; and `Balance` becomes
`income.TotalReceived - expenses.TotalCommitted - debts.TotalCommitted`. No debt, expense or income
type is read directly inside the use case - it stays composition only, per AD-006.
`ResponseMonthlyExpenseJson` is **not** modified.
**Depends on**: T31
**Requirement**: DDSH-01, DDSH-02
**Tests**: income 5000, committed expenses 2000 and a debt installment of 150 give a balance of 2850;
a month with no debts gives the same balance as before the feature, which is what proves nothing
regressed; the use case calls `IGetMonthlyDebtUseCase.Execute` exactly once with the requested year
and month; the endpoint returns the debt block alongside income and expenses; 401 with no token.
Existing dashboard tests are **updated, not replaced** - only the new expectation is added
**Gate**: `test`
**Commit**: `feat: subtract the month's debts from the dashboard balance`

---

### Phase 10: Migration

#### T34: Generate and verify the AddDebtTracking migration

**Where**: `src/Balance.Infrastructure/Migrations/`
**What**: `dotnet ef migrations add AddDebtTracking --project src/Balance.Infrastructure --startup-project src/Balance.Api`.
Then read the generated file and confirm: four `CreateTable` calls and no `AlterColumn` or `DropColumn`
against an existing table; `numeric(18,2)` on `PrincipalAmount`, `TotalAmount`, `ExpectedAmount` and
`AmountPaid`; `ReferentialAction.Restrict` on every foreign key; the indexes listed in the design; and
`unique: true` on the `DebtPayment.DebtInstallmentId` index.
**Depends on**: T4, T33
**Requirement**: DEBT-01, DPAY-01
**Tests**: schema-shape layer per the coverage matrix — the migration file is the artefact under
review. Run the `migration` gate against the live database to confirm it applies cleanly on top of
`AddExpenseTracking`
**Gate**: `migration`, then `test`
**Commit**: `feat: add the debt tracking migration`

---

## Self-Review

**Spec coverage.** Every requirement in the spec's traceability table maps to at least one task:
CRED-01 → T1, T3, T11, T12, T15; CRED-02 → T9, T13, T14, T15; CRED-03 → T8, T12, T15;
DEBT-01 → T1, T3, T4, T16, T18, T19, T34; DEBT-02 → T10, T18, T19; DEBT-03 → T8, T17, T18;
DSCH-01 → T5, T18, T19; DSCH-02 → T6, T18; DSCH-03 → T16, T17, T18, T19;
DPAY-01 → T3, T4, T20, T21, T23, T34; DPAY-02 → T10, T21, T23; DPAY-03 → T20, T22, T23;
DPAY-04 → T8, T21, T22, T23; DVEW-01 → T10, T29, T30, T31, T32; DVEW-02 → T29, T30, T32;
DVEW-03 → T29, T31, T32; DBAL-01 → T7, T10, T24, T28; DBAL-02 → T10, T25, T26, T28;
DBAL-03 → T11, T27, T28; DDSH-01 → T33; DDSH-02 → T33. No gaps.

**Type consistency.** `InstallmentAmountCalculator.Split` (T5) is called by name in T18.
`DebtScheduleBuilder.FirstCompetenceMonth` and `DueDateIn` (T6) are called by name in T18.
`DebtExtensions.OutstandingBalance` and `IsSettled` (T7) are called by name in T24, T25 and T27.
`IGetMonthlyDebtUseCase.Execute(int, int)` (T31) is the exact signature T33 injects.
`IDebtPaymentRepository.GetByInstallment` (T10) is the probe named in T21.

**Placeholder scan.** No task defers work to a later one, and every `Tests` field names concrete
cases with concrete values rather than "test the above".

**One open question carried from the spec.** The month's debt block lives on the dashboard response
rather than inside `ResponseMonthlyExpenseJson`. T33 implements the dashboard placement. If that is
reversed, T29 through T33 are the only tasks affected.
