# Income Tracking Specification

## Problem Statement

The Balance API can authenticate a user and nothing else. There is no way to record where money comes
from, how much of it arrived, or when. The owner needs to answer three questions that a single "amount"
column cannot: what do I earn today, what did I earn before and why did it change, and what has actually
landed this month versus what is still pending.

## Goals

- [ ] Record every income source of the account, split between Recurring and Variable, and keep an append-only history of its value changes with the reason for each change.
- [ ] Record each payment received against an income source, with the reference month decoupled from the payment date.
- [ ] Return, for any reference month, the expected and received amounts per source plus month totals, so pending income is visible.
- [ ] Give every persisted entity in the solution a single identity and audit shape (`Guid Id`, `CreatedAt`, `UpdatedAt`).

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
| ------- | ------ |
| Expenses, budgets, balance between income and spending | This delivery covers income only; the user asked for income logic |
| Archive / unarchive endpoint for an income source | The `Archived` field exists and is honored by queries, but no operation sets it; deferred to its own feature |
| Update and delete for Person | Only create and list are needed for income tracking to work end to end |
| Update, delete or correction of a recorded payment | Not requested; the history is append-only in this delivery |
| Multi-currency, exchange rates | Not requested; a single implicit currency is assumed |
| Reports, exports (Excel / PDF), charts | Separate concern, covered by a different skill in this repo |
| Recurring-payment automation (auto-creating expected payments) | The monthly view projects expectation on read; nothing is written automatically |
| Rate limiting on the new endpoints | No rate limiting exists anywhere in the solution; out of this feature's boundary |

---

## Assumptions & Open Questions

Every ambiguity is resolved or recorded here - nothing is left silently unclear.

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --------------------- | -------------- | --------- | ---------- |
| `User` migrates to `BaseEntity` | PK becomes `Guid Id`; `UserIdentifier` is removed and the JWT `Sid` claim carries `Id` | The repo has exactly one entity and five auth tests today, so this is the cheapest this migration will ever be; the alternative is two permanent identity styles | y |
| Ownership of income data | `Person` carries `UserId`; ownership cascades `IncomePayment` to `IncomeSource` to `Person` to `User` | The user logs in as an account and is himself a Person, alongside other People he registers (e.g. spouse) | y |
| The account owner's own `Person` | Created automatically inside `RegisterUserUseCase`, flagged `IsAccountOwner = true` | Removes the window where an account exists but cannot own income | y |
| Person management surface | `PersonController` with create and list only | The minimum for income tracking to work end to end for a second person | y |
| Meaning of `ReceitaBaseHistorico` on the payment | FK `IncomeSourceVersionId` on `IncomePayment`, nullable | The user confirmed that fields named after other tables denote a relationship; freezing the version keeps history stable if a validity date is later corrected | y |
| Registration use cases | Two: `RegisterIncomeSource` (source plus first version) and `RegisterIncomePayment` | They are distinct events with distinct validation | y |
| Monthly view semantics | Expected plus received, reconciled per source, keyed by `ReferenceMonth` | Without projection the view cannot answer what is still pending, which is the purpose of storing `ExpectedDay` | y |
| Value change | `ChangeIncomeSourceValue` closes the current version automatically and opens the new one in one transaction | Guarantees a timeline with no gaps and no overlaps; `ChangeReason` would otherwise never be filled | y |
| Versions for Variable sources | Only Recurring sources have `IncomeSourceVersion` rows; Variable sources have none | A Variable source has no expected amount or expected day by definition, which is what makes it variable | n |
| Multiple payments per source per reference month | Allowed and summed into the received amount | Split payments and variable income arrive in parts; a uniqueness rule would reject legitimate data | n |
| Month keying | The monthly view filters on `ReferenceMonth`, never on `PaymentDate` | A salary paid on 03 September for August must count as August | n |
| Money representation | `decimal` mapped to `numeric(18,2)` | Standard for money in PostgreSQL; avoids binary floating point error | n |
| `ReferenceMonth` storage | A `DateOnly` normalized to the first day of the month | Comparable and indexable without string parsing | n |
| Timestamps | `CreatedAt` and `UpdatedAt` in UTC; `UpdatedAt` null until the first update | Matches the existing codebase, which has no local-time handling | n |
| Concurrency on value change | Last write wins, guarded by an overlap check at write time; no locking | Single-account application; pessimistic locking is unjustified complexity here | n |
| Observability | N/A because the solution has no logging, metrics or tracing beyond the ASP.NET Core default | Adding an observability stack is outside this feature's boundary | n |
| External-dependency failure | N/A because the feature calls no external service; PostgreSQL failures surface through the existing `ExceptionFilter` as 500 | No new outbound dependency is introduced | n |

**Open questions:** none - all resolved or logged above.

---

## User Stories

### P1: Shared entity identity and audit trail ⭐ MVP

**User Story**: As the developer of Balance, I want every persisted entity to share one identity and audit shape so that no entity in the system needs a bespoke key or timestamp handling.

**Why P1**: Every other story in this spec persists an entity. The shape has to exist first, and unifying `User` costs least while the solution has a single entity.

**Acceptance Criteria**:
1. The system SHALL provide a `BaseEntity` type exposing `Id` of type `Guid`, `CreatedAt` of type `DateTime` and `UpdatedAt` of type nullable `DateTime`.
2. WHEN an entity is persisted for the first time THEN the system SHALL set `CreatedAt` to the current UTC instant.
3. WHEN an entity is persisted for the first time THEN the system SHALL leave `UpdatedAt` null.
4. WHEN an already persisted entity is modified and saved THEN the system SHALL set `UpdatedAt` to the current UTC instant.
5. The system SHALL identify a `User` by the `BaseEntity` `Id`, with no separate `UserIdentifier` field.
6. WHEN the system issues an access token THEN it SHALL place the user's `Id` in the `Sid` claim.
7. WHEN a request carries a valid token THEN the logged-user service SHALL resolve the `User` whose `Id` equals the `Sid` claim.

**Independent Test**: Register a user, decode the returned token and confirm the `Sid` claim is a `Guid` that matches the persisted row's primary key, with `CreatedAt` set and `UpdatedAt` null.

---

### P1: Manage the people of my account ⭐ MVP

**User Story**: As an account owner, I want myself and the other people in my household to exist as People so that each income source can be attributed to whoever earns it.

**Why P1**: An income source cannot be created without a Person to attribute it to.

**Acceptance Criteria**:
1. WHEN a user completes registration THEN the system SHALL create a `Person` linked to that user, with the registration name and `IsAccountOwner` set to true.
2. WHEN an authenticated user creates a `Person` THEN the system SHALL persist it linked to that user with `IsAccountOwner` set to false and respond 201.
3. WHEN an authenticated user lists People THEN the system SHALL return only the People linked to that user.
4. IF a request to a Person endpoint carries no valid bearer token THEN the system SHALL respond 401.
5. IF a `Person` is submitted with an empty name THEN the system SHALL respond 400 carrying the `NAME_REQUIRED` message.
6. The system SHALL accept a null or empty description on a `Person`.

**Independent Test**: Register a user, list People and see exactly one owner Person; create a second Person and see both, while a second account sees only its own.

---

### P1: Register an income source ⭐ MVP

**User Story**: As an account owner, I want to register where my money comes from, either as a recurring source with an expected amount and day or as a variable one, so that payments have something to attach to.

**Why P1**: This is the root of the whole model; nothing else can be recorded first.

**Acceptance Criteria**:
1. WHEN an authenticated user registers an income source of type Recurring THEN the system SHALL persist the source and its first version in a single transaction.
2. WHEN an income source of type Recurring is registered THEN the system SHALL set the first version's `ValidityEnd` to null.
3. WHEN an authenticated user registers an income source of type Variable THEN the system SHALL persist the source with no version row.
4. WHEN an income source is registered THEN the system SHALL set `Archived` to false.
5. IF the referenced `Person` does not belong to the logged user THEN the system SHALL respond 404.
6. IF the name of the income source is empty THEN the system SHALL respond 400 carrying the `NAME_REQUIRED` message.
7. IF the amount of a Recurring income source is less than or equal to zero THEN the system SHALL respond 400 carrying the `AMOUNT_GREATER_THAN_ZERO` message.
8. IF the expected day of a Recurring income source is outside the range 1 to 31 THEN the system SHALL respond 400 carrying the `EXPECTED_DAY_OUT_OF_RANGE` message.
9. IF an amount or expected day is supplied for a Variable income source THEN the system SHALL respond 400 carrying the `VARIABLE_SOURCE_HAS_NO_VERSION` message.

**Independent Test**: Register a Recurring source and read back one source plus one open version; register a Variable source and read back one source with zero versions.

---

### P1: Record a payment received ⭐ MVP

**User Story**: As an account owner, I want to record that money actually arrived, for a reference month that may differ from the payment date, so that late or early payments land in the right month.

**Why P1**: Without payments the monthly view has nothing real to reconcile against.

**Acceptance Criteria**:
1. WHEN an authenticated user records a payment THEN the system SHALL persist the payment date, the reference month, the amount received, the notes and the income source.
2. WHILE the income source is of type Recurring the system SHALL store on the payment the identifier of the version in effect at the reference month.
3. WHILE the income source is of type Variable the system SHALL store a null version identifier on the payment.
4. IF the referenced income source does not belong to the logged user THEN the system SHALL respond 404.
5. IF the income source is archived THEN the system SHALL respond 400 carrying the `INCOME_SOURCE_ARCHIVED` message.
6. IF the amount received is less than or equal to zero THEN the system SHALL respond 400 carrying the `AMOUNT_GREATER_THAN_ZERO` message.
7. IF no version of a Recurring income source is in effect at the reference month THEN the system SHALL respond 400 carrying the `NO_VERSION_IN_EFFECT` message.
8. The system SHALL accept more than one payment for the same income source and reference month.
9. The system SHALL accept a null or empty notes value.

**Independent Test**: Record a payment dated 03 September with reference month August and confirm it is returned by the August view and absent from the September view.

---

### P1: View the income of a given month ⭐ MVP

**User Story**: As an account owner, I want one screen per month showing what I expected and what actually arrived, so that I can see at a glance what is still pending.

**Why P1**: This is the payoff of the whole model and the second use case the user asked for by name.

**Acceptance Criteria**:
1. WHEN an authenticated user requests the income of a reference month THEN the system SHALL return one line for each non-archived income source belonging to that user.
2. WHILE an income source is of type Recurring the system SHALL report as the expected amount the amount of the version in effect at that reference month.
3. WHILE an income source is of type Recurring the system SHALL report the expected day of the version in effect at that reference month.
4. WHILE an income source is of type Variable the system SHALL report a null expected amount.
5. WHEN payments exist for an income source at that reference month THEN the system SHALL report the sum of their amounts as the received amount.
6. WHEN no payment exists for an income source at that reference month THEN the system SHALL report a received amount of zero.
7. WHEN an income source has no payment at that reference month THEN the system SHALL report its status as `Pending`.
8. WHEN the received amount of an income source equals its expected amount THEN the system SHALL report its status as `Received`.
9. WHEN the received amount of an income source differs from a non-null expected amount and is greater than zero THEN the system SHALL report its status as `Divergent`.
10. The system SHALL return the sum of the expected amounts and the sum of the received amounts for the month.
11. IF the requested reference month is not a valid year and month THEN the system SHALL respond 400 carrying the `REFERENCE_MONTH_INVALID` message.
12. WHEN a user has no income source THEN the system SHALL respond 200 with an empty line collection and zeroed totals.

**Independent Test**: With one Recurring source paid and one unpaid, request the month and see one `Received` line, one `Pending` line and totals that match the sum of the two lines.

---

### P2: Change the value of a recurring income source

**User Story**: As an account owner, I want to register that a source now pays a different amount and why, so that I keep the history of when I earned less and the reason it changed.

**Why P2**: The account is usable without it, but this is what turns the version table into an actual history rather than a single frozen row.

**Acceptance Criteria**:
1. WHEN an authenticated user changes the value of a Recurring income source THEN the system SHALL set the `ValidityEnd` of the version in effect to the day before the new `ValidityStart`.
2. WHEN an authenticated user changes the value of a Recurring income source THEN the system SHALL create a new version carrying the new amount, the new expected day, the new `ValidityStart`, a null `ValidityEnd` and the supplied change reason.
3. WHEN a value change is applied THEN the system SHALL perform the closing of the old version and the creation of the new one in a single transaction.
4. The system SHALL leave the version identifier of previously recorded payments unchanged.
5. IF the change reason is empty THEN the system SHALL respond 400 carrying the `CHANGE_REASON_REQUIRED` message.
6. IF the new `ValidityStart` is not later than the `ValidityStart` of the version in effect THEN the system SHALL respond 400 carrying the `VALIDITY_START_MUST_BE_LATER` message.
7. IF the income source is of type Variable THEN the system SHALL respond 400 carrying the `VARIABLE_SOURCE_HAS_NO_VERSION` message.
8. IF the referenced income source does not belong to the logged user THEN the system SHALL respond 404.

**Independent Test**: Register a source at one amount, record a payment, change the value with a reason, then confirm the old month still reports the old expected amount and the new month reports the new one.

---

## Edge Cases

- IF a reference month falls before the `ValidityStart` of every version of a Recurring source THEN the system SHALL report a null expected amount for that source.
- IF a reference month falls inside a closed version's validity range THEN the system SHALL report that closed version's amount rather than the current one.
- WHEN an income source is archived THEN the system SHALL omit it from the monthly view while keeping its recorded payments in the database.
- IF the expected day is 31 and the reference month is shorter THEN the system SHALL report the day as stored without clamping it to the month length.
- IF two income sources of the same user carry the same name THEN the system SHALL accept both.
- WHEN a Variable source has payments in a month THEN the system SHALL report its status as `Received` regardless of any expected amount.

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| -------------- | ----- | ----- | ------ |
| CORE-01 | P1: Shared entity identity and audit trail | Design | Pending |
| CORE-02 | P1: Shared entity identity and audit trail | Design | Pending |
| CORE-03 | P1: Shared entity identity and audit trail | Design | Pending |
| PRSN-01 | P1: Manage the people of my account | Design | Pending |
| PRSN-02 | P1: Manage the people of my account | Design | Pending |
| PRSN-03 | P1: Manage the people of my account | Design | Pending |
| INC-01 | P1: Register an income source | Design | Pending |
| INC-02 | P1: Register an income source | Design | Pending |
| INC-03 | P1: Register an income source | Design | Pending |
| INC-04 | P1: Record a payment received | Design | Pending |
| INC-05 | P1: Record a payment received | Design | Pending |
| INC-06 | P1: Record a payment received | Design | Pending |
| INC-07 | P1: View the income of a given month | Design | Pending |
| INC-08 | P1: View the income of a given month | Design | Pending |
| INC-09 | P1: View the income of a given month | Design | Pending |
| INC-10 | P1: View the income of a given month | Design | Pending |
| INC-11 | P2: Change the value of a recurring income source | Design | Pending |
| INC-12 | P2: Change the value of a recurring income source | Design | Pending |
| INC-13 | P2: Change the value of a recurring income source | Design | Pending |

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 19 total, 0 mapped to tasks, 19 unmapped ⚠️ (expected before the Tasks phase)

### Requirement coverage map

| Requirement | Covers |
| ----------- | ------ |
| CORE-01 | `BaseEntity` shape and its `CreatedAt` / `UpdatedAt` lifecycle |
| CORE-02 | `User` migrated onto `BaseEntity`, `UserIdentifier` removed |
| CORE-03 | Token `Sid` claim and logged-user resolution on the new `Id` |
| PRSN-01 | Owner Person created during user registration |
| PRSN-02 | Person creation endpoint, ownership and validation |
| PRSN-03 | Person listing scoped to the logged user |
| INC-01 | Recurring source registered with its first open version, transactionally |
| INC-02 | Variable source registered without a version |
| INC-03 | Registration validation and Person ownership check |
| INC-04 | Payment persisted with reference month decoupled from payment date |
| INC-05 | Version in effect frozen onto the payment |
| INC-06 | Payment validation, archived and ownership checks |
| INC-07 | One line per non-archived source with expected values resolved per month |
| INC-08 | Received amount as the sum of the month's payments |
| INC-09 | Per-source status: Received, Pending, Divergent |
| INC-10 | Month totals and empty-state response |
| INC-11 | Current version closed at the day before the new start |
| INC-12 | New version created with the change reason, transactionally |
| INC-13 | Value-change validation and immutability of recorded payments |

---

## Success Criteria

- [ ] `dotnet build` reports zero errors and zero warnings, and `dotnet test` is green including the five pre-existing auth tests, adapted to the new `User` identity.
- [ ] A single account can register two People, attach a Recurring source to each, record payments and read a month back with correct per-source status and totals.
- [ ] A second account requesting the same month receives none of the first account's sources.
- [ ] After one value change, the month before the change still reports the old expected amount and the month after reports the new one, with the change reason readable on the new version.
- [ ] Every endpoint added by this feature answers 401 without a bearer token.
