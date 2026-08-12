# Expense Tracking Specification

## Problem Statement

Balance can record where money comes from and reconcile it month by month. It cannot record where money
goes. The owner needs to answer the other half of the same question: what did I commit to spending this
month, what did I actually spend, and which of those figures is still a guess.

Spending has three shapes the income model does not cover. A one-off purchase lands on a card whose
invoice may close before the month does, so the month it *belongs to* is not the month it *happened
in*. An installment purchase is one decision that produces twelve monthly charges. A recurring bill
like Luz has a value that is estimated until the bill arrives and then known — and the known value has
to overwrite the estimate for that month without destroying the estimate for the others.

---

## Goals

- [ ] Record a one-off expense on credit, debit or pix, attributed to a person, a category and an account, with the competence month derived from the account's closing day.
- [ ] Record an installment purchase once and have the system produce one expense per installment, summing to the total exactly.
- [ ] Record a recurring monthly bill with a base value that has an append-only history of changes and a reason for each change.
- [ ] Record what a recurring bill actually cost in a given month, overriding the estimate for that month only, and allow that figure to be corrected afterwards.
- [ ] Return, for any competence month, the variable expenses and the recurring expenses with expected versus actual amounts and month totals.
- [ ] Return income and expenses for the same month in one response, without modifying any income code.

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
| ------- | ------- |
| Any change to income entities, repositories or use cases | The user's standing constraint; the dashboard composes `IGetMonthlyIncomeUseCase` and edits nothing |
| Update and delete for `Category` and `Account` | Create and list are what expense registration needs; the rest is not requested |
| Archive for `Account` | Only `RecurringExpense` was approved for archiving |
| Update, delete or correction of a single `Expense` | Not requested; the correction path requested was for recurring payments only |
| Cancelling or shortening an `InstallmentPlan` partway | Not requested; a plan is generated whole |
| Validating a month's spending against `Account.Limit` | `Limit` is stored for display; no rule was requested against it |
| Budgets, spending targets, alerts on `DueDay` | Separate concern, not requested |
| Reports and exports (Excel / PDF), charts in the API | Separate concern, covered by a different skill in this repo |
| Automatic generation of recurring payment rows each month | The monthly view projects the expectation on read; nothing is written automatically, matching the income design |
| Multi-currency, exchange rates | Not requested; a single implicit currency is assumed |
| Rate limiting on the new endpoints | No rate limiting exists anywhere in the solution |
| Authentication on the frontend page | The page is a read-only demo against a seeded account; it uses one token |

---

## Assumptions & Open Questions

Every ambiguity is resolved or recorded here - nothing is left silently unclear.

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --------------------- | -------------- | --------- | ---------- |
| Ownership of `Category` | Account-level: `Category` carries `UserId` | The user stated a category is used by every Person derived from the account | y |
| Ownership of `Account` | Person-level: `Account` carries `PersonId` | The user stated each person has their own cards | y |
| Cross-person accounts | An expense of person A may be paid on an account of person B, provided both belong to the same user | `Person` records who spent, `Account` records what paid; "my wife bought it on my card" must be representable | y |
| Competence month for credit | Derived from `Account.ClosingDay`, overridable by an explicit request value | This is the only behaviour that gives `ClosingDay` a purpose | y |
| Installment generation | Registering a plan generates the N expenses in one transaction | The user chose generation over manual entry of each installment | y |
| `Category.Priority` | Enum with `Essential`, `Important`, `Superfluous` | The user chose named levels over an integer rank so the view can label the groups | y |
| Archive for `RecurringExpense` | Ship the archive and unarchive operations alongside the `Archived` field | Approved as an addition; lesson L-002 records that income shipped `Archived` with no operation to set it | y |
| `AccountId` on `RecurringExpensePayment` | Nullable, recording the account that actually paid that month | Approved as an addition | y |
| `Category` and `Account` endpoints | Create and list, approved as an addition | Nothing can be registered without them | y |
| Monthly expense view and dashboard | Approved as additions | They are what the requested frontend page consumes | y |
| Installment rounding | Installments 1..N-1 are `round(Total / N, 2)`; installment N is `Total - sum(previous)` | The N amounts must sum to the total exactly, with no lost or invented cent | n |
| Installment midpoint rounding | `MidpointRounding.AwayFromZero` on installments 1..N-1 | Recorded during T22 as a spec-precision gap: the spec says only "round to 2 decimals" and does not pin the midpoint mode. It affects at most one cent on a non-final installment, and the final installment is the residual, so the sum is exact under either mode. No test depends on the choice | n |
| Installment expense type and date | Every generated expense carries `Type = Credit` and `Date = StartDate` | The purchase happened once; what advances month to month is the invoice it lands on | n |
| `InstallmentPlan.EndDate` | Computed as the competence month of installment N, not accepted from the request | Accepting both a count and an end date invites a contradiction the system would have to arbitrate | n |
| `InstallmentPlan.PersonId` | Added to the plan; generated expenses inherit it | The user's sketch omits it, but without it the generated rows have no owner and no ownership cascade | n |
| Payments per recurring expense per month | Exactly one, enforced by a unique index on `(RecurringExpenseId, ReferenceMonth)` | The user's sketch states `DataReferencia` is unique with `DespesaRecorrente`; unlike income, a bill is paid once |  n |
| Nullability on `Account` | `ClosingDay`, `DueDay` and `Limit` are nullable | They describe a credit card; a debit account such as "Inter Débito" has none of them | n |
| `IsEstimate` behaviour | Reported on the monthly line; never blocks an operation | It marks a figure as provisional for the reader; it is not a validation rule | n |
| Version freeze on a recurring payment | The payment stores the identifier of the version in effect at its reference month | Mirrors `IncomePayment`; correcting a validity date later cannot rewrite recorded history | n |
| Money representation | `decimal` mapped to `numeric(18,2)` | Matches the income feature and avoids binary floating point error | n |
| `CompetenceMonth` and `ReferenceMonth` storage | `DateOnly` normalised to the first day of the month | Matches the income feature; comparable and indexable without string parsing | n |
| Delete behaviour on relationships | `Restrict` everywhere | Matches the income feature; nothing cascades away silently | n |
| Concurrency on a value change | Last write wins, guarded by an overlap check at write time | Matches the income feature; a household application does not justify locking | n |
| Observability | N/A because the solution has no logging, metrics or tracing beyond the ASP.NET Core default | Adding an observability stack is outside this feature's boundary | n |
| External-dependency failure | N/A because the feature calls no external service; PostgreSQL failures surface through the existing `ExceptionFilter` as 500 | No new outbound dependency is introduced | n |

**Open questions:** none - all resolved or logged above.

---

## User Stories

### P1: Keep a catalogue of categories and accounts ⭐ MVP

**User Story**: As an account owner, I want my spending categories and my payment accounts to exist as records so that every expense can be filed against one of each.

**Why P1**: No expense can be registered before a category and an account exist.

**Acceptance Criteria**:
1. WHEN an authenticated user creates a category THEN the system SHALL persist it linked to that user with the supplied name, description and priority, and respond 201.
2. WHEN an authenticated user lists categories THEN the system SHALL return only the categories linked to that user.
3. WHEN an authenticated user creates an account THEN the system SHALL persist it linked to the referenced person and respond 201.
4. WHEN an authenticated user lists accounts THEN the system SHALL return only the accounts whose person belongs to that user.
5. IF the referenced person of an account does not belong to the logged user THEN the system SHALL respond 404.
6. IF a category or an account is submitted with an empty name THEN the system SHALL respond 400 carrying the `NAME_REQUIRED` message.
7. IF the closing day or the due day of an account is supplied and falls outside the range 1 to 31 THEN the system SHALL respond 400 carrying the `DAY_OUT_OF_RANGE` message.
8. The system SHALL accept a null closing day, due day and limit on an account.
9. IF a request to a category or account endpoint carries no valid bearer token THEN the system SHALL respond 401.

**Independent Test**: Create a category and an account, list both back, and confirm a second account sees neither.

---

### P1: Register an expense ⭐ MVP

**User Story**: As an account owner, I want to record a purchase on credit, debit or pix so that the money leaving my accounts is captured against the month it really belongs to.

**Why P1**: This is the core record of the whole feature.

**Acceptance Criteria**:
1. WHEN an authenticated user registers an expense THEN the system SHALL persist the name, person, type, amount, category, account and date, and respond 201.
2. WHILE the expense type is `Credit` and the account carries a closing day, the system SHALL set the competence month to the month of the date when the day of the date is not after the closing day.
3. WHILE the expense type is `Credit` and the account carries a closing day, the system SHALL set the competence month to the month following the date when the day of the date is after the closing day.
4. WHILE the expense type is `Debit` or `Pix`, the system SHALL set the competence month to the month of the date.
5. WHILE the expense type is `Credit` and the account carries no closing day, the system SHALL set the competence month to the month of the date.
6. WHEN the request supplies a competence month explicitly THEN the system SHALL store that value normalised to the first day of its month, overriding the derived one.
7. The system SHALL accept an expense whose account belongs to a different person of the same user.
8. IF the referenced person, category or account does not belong to the logged user THEN the system SHALL respond 404.
9. IF the name of the expense is empty THEN the system SHALL respond 400 carrying the `NAME_REQUIRED` message.
10. IF the amount of the expense is less than or equal to zero THEN the system SHALL respond 400 carrying the `AMOUNT_GREATER_THAN_ZERO` message.

**Independent Test**: With an account closing on day 20, register a credit expense dated the 21st and read it back with a competence month one month later than its date.

---

### P1: Register an installment purchase ⭐ MVP

**User Story**: As an account owner, I want to record a purchase split into installments once, so that the system produces the monthly charges instead of me typing twelve of them.

**Why P1**: The user named installments as a core capability of the expense system.

**Acceptance Criteria**:
1. WHEN an authenticated user registers an installment plan THEN the system SHALL persist the plan and its installment expenses in a single transaction and respond 201.
2. WHEN an installment plan of N installments is registered THEN the system SHALL create exactly N expenses numbered 1 to N, each referencing the plan.
3. WHEN an installment plan is registered THEN the system SHALL make the sum of the installment amounts equal the plan's total amount exactly.
4. WHEN an installment plan is registered THEN the system SHALL set the competence month of installment 1 by the same rule used for a single credit expense, and advance the competence month by one month for each subsequent installment.
5. WHEN an installment plan is registered THEN the system SHALL set every generated expense's type to `Credit` and its date to the plan's start date.
6. WHEN an installment plan is registered THEN the system SHALL set the plan's end date to the competence month of the last installment.
7. IF the installment count is less than two THEN the system SHALL respond 400 carrying the `INSTALLMENT_COUNT_INVALID` message.
8. IF the total amount of the plan is less than or equal to zero THEN the system SHALL respond 400 carrying the `AMOUNT_GREATER_THAN_ZERO` message.
9. IF the referenced person, category or account does not belong to the logged user THEN the system SHALL respond 404.

**Independent Test**: Register a plan of 100.00 over 3 installments and read back three expenses of 33.33, 33.33 and 33.34, in three consecutive competence months.

---

### P1: Register a recurring expense ⭐ MVP

**User Story**: As an account owner, I want bills that arrive every month to exist as one record with a base value, so that I do not re-enter Netflix twelve times a year.

**Why P1**: Recurring bills are half of the feature the user described.

**Acceptance Criteria**:
1. WHEN an authenticated user registers a recurring expense THEN the system SHALL persist the recurring expense and its first version in a single transaction and respond 201.
2. WHEN a recurring expense is registered THEN the system SHALL set the first version's validity end to null.
3. WHEN a recurring expense is registered THEN the system SHALL set `Archived` to false and store the supplied `IsEstimate` flag.
4. IF the referenced person, category or account does not belong to the logged user THEN the system SHALL respond 404.
5. IF the name of the recurring expense is empty THEN the system SHALL respond 400 carrying the `NAME_REQUIRED` message.
6. IF the base amount is less than or equal to zero THEN the system SHALL respond 400 carrying the `AMOUNT_GREATER_THAN_ZERO` message.
7. IF the due day is outside the range 1 to 31 THEN the system SHALL respond 400 carrying the `DAY_OUT_OF_RANGE` message.

**Independent Test**: Register "Netflix" and read back one recurring expense with exactly one open version carrying its base amount.

---

### P1: Record what a recurring expense actually cost ⭐ MVP

**User Story**: As an account owner, I want to enter the real value of a bill for one month when it arrives, and correct it afterwards, so that the estimate stops being used for that month without touching any other month.

**Why P1**: This is the overwrite behaviour the user described as the point of the recurring model.

**Acceptance Criteria**:
1. WHEN an authenticated user records a payment for a recurring expense THEN the system SHALL persist the reference month, the payment date, the amount paid, the notes and the paying account, and respond 201.
2. WHEN a payment is recorded THEN the system SHALL store on it the identifier of the version in effect at its reference month.
3. WHEN an authenticated user updates a recorded payment THEN the system SHALL overwrite its amount paid, payment date, notes and paying account, and respond 200.
4. WHEN a payment is updated THEN the system SHALL leave its reference month and its version identifier unchanged.
5. IF a payment already exists for that recurring expense and reference month THEN the system SHALL respond 400 carrying the `PAYMENT_ALREADY_RECORDED` message.
6. IF the referenced recurring expense or payment does not belong to the logged user THEN the system SHALL respond 404.
7. IF the recurring expense is archived THEN the system SHALL respond 400 carrying the `RECURRING_EXPENSE_ARCHIVED` message.
8. IF the amount paid is less than or equal to zero THEN the system SHALL respond 400 carrying the `AMOUNT_GREATER_THAN_ZERO` message.
9. IF no version of the recurring expense is in effect at the reference month THEN the system SHALL respond 400 carrying the `NO_VERSION_IN_EFFECT` message.
10. The system SHALL accept a null or empty notes value and a null paying account.

**Independent Test**: Record Luz at 180.00 for August against a 150.00 estimate, then correct it to 172.40, and confirm August reports 172.40 while September still reports the 150.00 estimate.

---

### P1: View the expenses of a given month ⭐ MVP

**User Story**: As an account owner, I want one screen per month showing my variable spending and my recurring bills with what I expected against what I paid, so that I can see what is still outstanding.

**Why P1**: This is the payoff of the model and the source of the requested frontend page.

**Acceptance Criteria**:
1. WHEN an authenticated user requests the expenses of a competence month THEN the system SHALL return one variable line for each expense of that user whose competence month matches.
2. WHEN an authenticated user requests the expenses of a competence month THEN the system SHALL return one recurring line for each non-archived recurring expense of that user.
3. WHILE a recurring expense has a version in effect at that month, the system SHALL report that version's amount as the expected amount and the recurring expense's due day.
4. IF a recurring expense has no version in effect at that month THEN the system SHALL report a null expected amount for it.
5. WHEN a payment exists for a recurring expense at that month THEN the system SHALL report its amount paid as the actual amount and its status as `Paid` when the actual equals the expected.
6. WHEN a payment exists for a recurring expense at that month and its amount differs from a non-null expected amount THEN the system SHALL report its status as `Divergent`.
7. WHEN no payment exists for a recurring expense at that month THEN the system SHALL report a null actual amount and a status of `Pending`.
8. WHEN a recurring line is returned THEN the system SHALL report the `IsEstimate` flag of its recurring expense.
9. The system SHALL return the total of the variable lines, the total expected of the recurring lines, the total actually paid of the recurring lines, and the total committed for the month as the sum of the variable total and, per recurring line, its actual amount when present and its expected amount otherwise.
10. WHEN a variable line originates from an installment plan THEN the system SHALL report its installment number and its plan's installment count.
11. IF the requested competence month is not a valid year and month THEN the system SHALL respond 400 carrying the `REFERENCE_MONTH_INVALID` message.
12. WHEN a user has no expense and no recurring expense THEN the system SHALL respond 200 with empty line collections and zeroed totals.

**Independent Test**: With one paid recurring bill, one unpaid recurring bill and one installment expense in the month, request the month and see the three lines with the right statuses and totals that match their sum.

---

### P2: Change the base value of a recurring expense

**User Story**: As an account owner, I want to register that a bill now costs a different amount and why, so that past months keep the value they really had.

**Why P2**: The account works without it, but it is what turns the version table into a history rather than a frozen row.

**Acceptance Criteria**:
1. WHEN an authenticated user changes the base value of a recurring expense THEN the system SHALL set the validity end of the version in effect to the day before the new validity start.
2. WHEN an authenticated user changes the base value of a recurring expense THEN the system SHALL create a new version carrying the new amount, the new validity start, a null validity end and the supplied change reason.
3. WHEN a value change is applied THEN the system SHALL perform the closing of the old version and the creation of the new one in a single transaction.
4. The system SHALL leave the version identifier of previously recorded payments unchanged.
5. IF the change reason is empty THEN the system SHALL respond 400 carrying the `CHANGE_REASON_REQUIRED` message.
6. IF the new validity start is not later than the validity start of the version in effect THEN the system SHALL respond 400 carrying the `VALIDITY_START_MUST_BE_LATER` message.
7. IF the referenced recurring expense does not belong to the logged user THEN the system SHALL respond 404.

**Independent Test**: Register a bill at 150.00, change it to 180.00 from September, and confirm August still reports 150.00 while September reports 180.00 with the reason readable.

---

### P2: Archive a recurring expense

**User Story**: As an account owner, I want to retire a bill I no longer pay so that it stops appearing in my months, without losing the record of what I paid while I had it.

**Why P2**: The user asked for recurring expenses to be removable; archiving is the non-destructive reading of that.

**Acceptance Criteria**:
1. WHEN an authenticated user archives a recurring expense THEN the system SHALL set its `Archived` flag to true and respond 204.
2. WHEN an authenticated user unarchives a recurring expense THEN the system SHALL set its `Archived` flag to false and respond 204.
3. WHEN a recurring expense is archived THEN the system SHALL omit it from the monthly view while keeping its recorded payments in the database.
4. IF the referenced recurring expense does not belong to the logged user THEN the system SHALL respond 404.

**Independent Test**: Archive a bill that has a recorded payment, confirm it disappears from the month, unarchive it and confirm it returns with its payment intact.

---

### P2: See income and expenses for the same month

**User Story**: As an account owner, I want income and spending for one month in a single response, so that a page can show what came in against what went out without stitching two calls together.

**Why P2**: Both halves are readable separately; this is what makes the requested page one request instead of two.

**Acceptance Criteria**:
1. WHEN an authenticated user requests the dashboard of a month THEN the system SHALL return the existing monthly income view and the monthly expense view for that same month.
2. WHEN the dashboard is returned THEN the system SHALL report the month's balance as the total income received minus the total committed expense.
3. IF a request to the dashboard endpoint carries no valid bearer token THEN the system SHALL respond 401.
4. The system SHALL produce the income half by invoking the existing monthly income use case, leaving every income type unmodified.

**Independent Test**: With income and expenses recorded in the same month, request the dashboard and confirm both halves match what the two individual endpoints return, and that the balance is their difference.

---

## Edge Cases

- IF a competence month falls before the validity start of every version of a recurring expense THEN the system SHALL report a null expected amount for it.
- IF a competence month falls inside a closed version's validity range THEN the system SHALL report that closed version's amount rather than the current one.
- IF a credit expense is dated exactly on the account's closing day THEN the system SHALL place it in the month of the date, not the following one.
- IF the total of an installment plan does not divide evenly by its installment count THEN the system SHALL place the entire remainder on the last installment.
- IF a due day is 31 and the month is shorter THEN the system SHALL report the day as stored without clamping it to the month length.
- IF two categories or two accounts of the same user carry the same name THEN the system SHALL accept both.
- WHEN a recurring expense is archived after a payment was recorded THEN the system SHALL keep that payment retrievable through the database while omitting the line from the month.

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| -------------- | ----- | ----- | ------ |
| SHAR-01 | P1: Keep a catalogue of categories and accounts | Schema foundation | Implementing |
| SHAR-02 | P1: Keep a catalogue of categories and accounts | Schema foundation | Implementing |
| SHAR-03 | P1: Keep a catalogue of categories and accounts | Domain rules and messages | Implementing |
| EXPN-01 | P1: Register an expense | Schema foundation | Implementing |
| EXPN-02 | P1: Register an expense | Domain rules and messages | Implementing |
| EXPN-03 | P1: Register an expense | Expense slice | Implementing |
| INST-01 | P1: Register an installment purchase | Schema foundation | Implementing |
| INST-02 | P1: Register an installment purchase | Installment plans | Implementing |
| INST-03 | P1: Register an installment purchase | Domain rules and messages | Implementing |
| RECR-01 | P1: Register a recurring expense | Schema foundation | Implementing |
| RECR-02 | P1: Register a recurring expense | Recurring expenses | Implementing |
| RPAY-01 | P1: Record what a recurring expense actually cost | Schema foundation | Implementing |
| RPAY-02 | P1: Record what a recurring expense actually cost | Pending | Pending |
| RPAY-03 | P1: Record what a recurring expense actually cost | Schema foundation | Implementing |
| VIEW-01 | P1: View the expenses of a given month | Expense slice | Implementing |
| VIEW-02 | P1: View the expenses of a given month | Domain rules and messages | Implementing |
| VIEW-03 | P1: View the expenses of a given month | Schema foundation | Implementing |
| VIEW-04 | P1: View the expenses of a given month | Pending | Pending |
| RECR-03 | P2: Change the base value of a recurring expense | Recurring expenses | Implementing |
| RECR-04 | P2: Change the base value of a recurring expense | Pending | Pending |
| RECR-05 | P2: Archive a recurring expense | Pending | Pending |
| DASH-01 | P2: See income and expenses for the same month | Pending | Pending |
| DASH-02 | P2: See income and expenses for the same month | Pending | Pending |

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 23 total, 0 mapped to tasks, 23 unmapped. Mapping happens in the Tasks phase.

### Requirement coverage map

| Requirement | Covers |
| ----------- | ------ |
| SHAR-01 | `Category` entity, creation and listing scoped to the logged user |
| SHAR-02 | `Account` entity, creation and listing scoped through `Person`, nullable card fields |
| SHAR-03 | Catalogue validation: empty name, day out of range, foreign person 404 |
| EXPN-01 | `Expense` entity and its registration with person, category and account |
| EXPN-02 | Competence-month derivation from `ClosingDay`, and the explicit override |
| EXPN-03 | Expense validation, cross-person accounts and ownership 404s |
| INST-01 | `InstallmentPlan` entity and transactional generation of N expenses |
| INST-02 | Installment amounts summing to the total exactly, remainder on the last |
| INST-03 | Installment competence months advancing monthly, plan end date, validation |
| RECR-01 | `RecurringExpense` and `RecurringExpenseVersion`, registered transactionally |
| RECR-02 | Recurring registration validation and ownership checks |
| RPAY-01 | `RecurringExpensePayment` persisted with its version frozen |
| RPAY-02 | Updating a recorded payment without moving its month or version |
| RPAY-03 | Payment validation: duplicate month, archived, amount, no version in effect |
| VIEW-01 | Variable lines for the month, including installment number and count |
| VIEW-02 | Recurring lines with the expected amount resolved per month and `IsEstimate` |
| VIEW-03 | Actual amount and per-line status: `Paid`, `Pending`, `Divergent` |
| VIEW-04 | Month totals, committed total, invalid month and empty state |
| RECR-03 | Current version closed at the day before the new start |
| RECR-04 | New version created with its change reason, transactionally, history immutable |
| RECR-05 | Archive and unarchive, and their effect on the monthly view |
| DASH-01 | Dashboard composing the existing income use case with the expense view |
| DASH-02 | Dashboard balance and its 401 without a token |

---

## Success Criteria

- [ ] `dotnet build` reports zero errors and zero warnings, and `dotnet test` is green including all 82 pre-existing tests, none of which is edited.
- [ ] A credit expense dated after its account's closing day lands in the following competence month, and one dated on the closing day does not.
- [ ] A plan of 100.00 over 3 installments produces expenses of 33.33, 33.33 and 33.34 in three consecutive competence months.
- [ ] A recurring bill reports its estimate in a month with no payment and the paid amount in a month with one, and correcting the paid amount changes only that month.
- [ ] After one base-value change, the month before still reports the old amount and the month after reports the new one, with the reason readable.
- [ ] An archived recurring expense disappears from the monthly view while its payments remain in the database.
- [ ] The dashboard's income half is byte-identical to what `GET /api/income/{year}/{month}` returns for the same month.
- [ ] Every endpoint added by this feature answers 401 without a bearer token.
- [ ] PostgreSQL holds a seeded account with people, categories, accounts, income and expenses of every shape, and the frontend page renders it.
