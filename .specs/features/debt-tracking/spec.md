# Debt Tracking Specification

## Problem Statement

Balance records what the household earns and what it spends. It cannot record what the household
*owes*. Money borrowed does not behave like either half of the existing model: an installment plan is
a purchase that already happened on a known card, and a recurring expense repeats forever with no
principal and no end. A debt is neither. It has a counterparty, a principal that was handed over
once, a total that may exceed that principal, an end that is either known or deliberately open, and a
monthly obligation whose payment method is decided at the moment of paying, not at the moment of
borrowing.

The owner needs to answer three questions the current system cannot: how much do I still owe, to
whom, and what does that cost me this month. The counterparty matters as much as the amount - "1500
from my father, repaid in ten" and "an 1800 loan from the bank over twenty-four months" are the same
shape with different consequences, and the owner must be able to pull up either side by the party
responsible for it.

---

## Goals

- [ ] Keep a catalogue of creditors - a person, an institution or anything else money is owed to - shared across the whole household.
- [ ] Register a debt with a known schedule and have the system produce one installment per month, summing to the total exactly.
- [ ] Register a debt with no schedule, whose balance is reduced by ad-hoc payments.
- [ ] Record a payment against a debt, choosing the payment method and the account at that moment rather than at registration.
- [ ] Report, for any competence month, the debt installments due, what was expected, what was paid and whether the line is pending, paid, divergent or overdue.
- [ ] Report the outstanding balance of a debt, and the aggregate owed to one creditor, without storing a balance field.
- [ ] Include the month's debt obligation in the dashboard's committed total, so the balance of a month is not overstated.

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
| ------- | ------- |
| Any change to income entities, repositories or use cases | AD-006 stands; the dashboard composes `IGetMonthlyIncomeUseCase` and edits nothing |
| Any change to `Expense`, `InstallmentPlan` or `RecurringExpense` behaviour | A debt payment is its own record; nothing in the expense module learns about debts |
| Treating a credit-card invoice as a debt | The invoice is already derived from `Expense` + `Account.ClosingDay`; modelling it again would double every card purchase in the month's totals. Explicitly rejected by the user |
| Registering the borrowed money as income | Explicitly rejected by the user: the principal is recorded on the debt as history and never enters a month's received total |
| Interest rates, amortisation tables, price/SAC schedules | The user supplies the total to be repaid; the system does not compute interest. `PrincipalAmount` and `TotalAmount` together already expose the cost |
| Money other people owe the user (receivables) | The user described only debts owed outward |
| Renegotiating, refinancing or rescheduling a debt partway | Not requested; a schedule is generated whole, as an installment plan is |
| Partial payment of a single scheduled installment | One installment carries at most one payment, mirroring `RecurringExpensePayment`. A short payment is recorded as `Divergent`, not as two rows |
| Deleting a debt or a creditor | Archiving covers the requested need and keeps history retrievable |
| Alerts, notifications or reminders on a due day | Separate concern, not requested; the monthly view exposes `IsOverdue` for a client to act on |
| Reports and exports (Excel / PDF) | Separate concern, covered by a different skill in this repo |
| Multi-currency | Not requested; a single implicit currency is assumed, matching income and expenses |

---

## Assumptions & Open Questions

Every ambiguity is resolved or recorded here - nothing is left silently unclear.

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --------------------- | -------------- | --------- | ---------- |
| A creditor is a first-class entity | New `Creditor` entity, not a string on the debt and not a flag on `Person` | The user requires locating what is responsible for a debt. A free string cannot aggregate; reusing `Person` would make "Banco Itau" a household member that can own an `Account` | y |
| Ownership of `Creditor` | Account-level: `Creditor` carries `UserId`, per AD-005 | "My father" is the same creditor for every member of the household, exactly as a `Category` is | y |
| Ownership of `Debt` | Person-level: `Debt` carries `PersonId`, per AD-005 | The debt is one household member's obligation, and the monthly view already groups by person | y |
| Two roles on one debt | `Debt.PersonId` is who in the household pays; `Debt.CreditorId` is who is owed | These are different axes, and collapsing them loses the question the feature exists to answer | y |
| Debt shape | `DebtMode` enum: `Scheduled` = 0, `OpenEnded` = 1 | The user asked for both a ten-installment loan and an informal debt with no term | y |
| Payment method on the debt | `Debt` carries no `AccountId` and no `ExpenseType`; both live on `DebtPayment` and are optional | The user stated the same debt may be paid by debit, card or pix. A nullable `AccountId` matches the decision already shipped on `Expense` | y |
| Borrowed money | `PrincipalAmount` and `StartDate` are stored as history and never enter any income total | The user rejected generating an `IncomePayment` | y |
| Cost of the debt | `TotalAmount` is supplied by the user and must be greater than or equal to `PrincipalAmount` | Equal for a family loan, greater for a bank loan. The difference is the cost, exposed without modelling a rate | y |
| Outstanding balance | Computed as `TotalAmount - sum(AmountPaid)`, never persisted | A stored balance drifts from its payments the first time a payment is corrected | y |
| Settled state | Derived: a debt is settled when its outstanding balance is less than or equal to zero. No `Settled` flag | Same reason as the balance. Lesson L-002 records the cost of shipping a flag with nothing to set it | y |
| Installment amounts | Installments 1..N-1 are `Math.Round(TotalAmount / N, 2, MidpointRounding.AwayFromZero)`; installment N is the residual | Identical to `InstallmentPlan`, and the N amounts must sum to the total exactly | y |
| Rounding helper | The rule is extracted from `RegisterInstallmentPlanUseCase` into a shared `Domain.Extensions.InstallmentAmountCalculator`, and both callers use it | Two copies of a money-splitting rule is exactly the kind of duplication that diverges by one cent. `InstallmentPlan` behaviour is unchanged and its tests must stay green untouched | n |
| First competence month of a schedule | The month of `StartDate` when `StartDate.Day <= DueDay`, otherwise the following month | `CompetenceMonthResolver` encodes a credit-card invoice rule and must NOT be reused here: a debt has no closing day, it has a due day. Borrowing on the 20th with payment on the 10th first falls due the next month | n |
| Due date within a month | `DueDay` clamped to the last day of the target month | A `DueDay` of 31 must still produce a real date in February. This deliberately differs from `RecurringExpense.DueDay`, which is reported as stored because nothing there builds a date from it | n |
| `Debt.EndMonth` | Computed as the competence month of installment N, not accepted from the request | Accepting both a count and an end invites a contradiction the system would have to arbitrate. Mirrors `InstallmentPlan.EndDate` | n |
| Payments per scheduled installment | At most one, enforced by a unique index on `DebtPayment.DebtInstallmentId` | Mirrors the unique index on `(RecurringExpenseId, ReferenceMonth)`; a short payment is `Divergent`, not a second row | y |
| Reference month of a payment | Copied from the installment for a `Scheduled` debt; derived from `PaymentDate` normalised to the first of its month for an `OpenEnded` one | The request cannot be allowed to contradict the schedule it is paying | n |
| Credit payment without an account | Rejected with the existing `ACCOUNT_REQUIRED_FOR_CREDIT` message | The rule already shipped for `Expense` on this branch; a debt payment that diverged from it would be a surprise | n |
| Overdue | Reported as a boolean `IsOverdue` on the monthly line, computed from an unpaid installment whose due date is past. `ExpenseStatus` is NOT extended | Adding a fourth value to a shared enum changes the meaning of income and expense responses that never asked for it | n |
| "Today" for the overdue comparison | `DateOnly.FromDateTime(DateTime.UtcNow)`, taken once per request | The solution stamps every audit field from `DateTime.UtcNow` and holds no timezone concept. Pinning it here stops `IsOverdue` from depending on server locale, and keeps the rule testable by controlling the installment's due date rather than the clock | n |
| Line status | `Pending` / `Paid` / `Divergent`, resolved by the same rules as `GetMonthlyExpenseUseCase.ResolveStatus` | The reader already knows these three words; a fourth vocabulary would be gratuitous | y |
| Where the month's debts appear | A `GetMonthlyDebtUseCase` produces `ResponseMonthlyDebtJson`, and `GetMonthlyDashboardUseCase` composes it alongside income and expenses. `ResponseMonthlyExpenseJson` is NOT modified | The user chose "own block, counted in the committed total". Putting the block inside the expense response would force `GetMonthlyExpenseUseCase` to inject debt repositories, making the expense module depend on debts; composition at the dashboard is the pattern AD-006 already established. **Deviates from the wording the user approved - flagged for confirmation** | n |
| Dashboard balance | `Balance = income.TotalReceived - expenses.TotalCommitted - debts.TotalCommitted` | A month whose debt obligation is invisible reports surplus that is already spoken for | n |
| Committed amount of a debt line | The amount paid once a payment exists, the expected amount while it does not | Identical to `Committed()` for recurring expenses | y |
| An `OpenEnded` debt in the monthly view | Contributes only the payments recorded in that month; it has no expectation and never adds to the expected total | There is no schedule to expect anything from | n |
| Money representation | `decimal` mapped to `numeric(18,2)` | Matches income and expenses | y |
| Month storage | `DateOnly` normalised to the first day of the month | Matches income and expenses | y |
| Delete behaviour on relationships | `Restrict` everywhere | Matches income and expenses; nothing cascades away silently | y |
| Archived debt in the monthly view | Omitted from the month; its installments and payments remain in the database | Mirrors `RecurringExpense` archiving | n |
| Observability | N/A because the solution has no logging, metrics or tracing beyond the ASP.NET Core default | Outside this feature's boundary | y |
| External-dependency failure | N/A because the feature calls no external service; PostgreSQL failures surface through the existing `ExceptionFilter` as 500 | No new outbound dependency is introduced | y |

**Open questions:** one - whether the month's debt block belongs on the dashboard response (as
specified here) or inside `ResponseMonthlyExpenseJson` (as originally worded to the user). Resolved
in favour of the dashboard; awaiting confirmation.

---

## User Stories

### P1: Keep a catalogue of creditors ⭐ MVP

**User Story**: As an account owner, I want the people and institutions I owe money to to exist as
records so that every debt points at a real counterparty I can look up later.

**Why P1**: No debt can be registered before its creditor exists, and locating a debt by who is
responsible for it is the reason the feature exists.

**Acceptance Criteria**:
1. WHEN an authenticated user creates a creditor THEN the system SHALL persist it linked to that user with the supplied name, type, contact and notes, and respond 201.
2. WHEN an authenticated user lists creditors THEN the system SHALL return only the creditors linked to that user.
3. The system SHALL accept a creditor whose contact and notes are null.
4. IF a creditor is submitted with an empty name THEN the system SHALL respond 400 carrying the `NAME_REQUIRED` message.
5. WHEN an authenticated user archives a creditor THEN the system SHALL set `Archived` and omit it from the default listing while keeping its debts retrievable.
6. WHEN creditors are listed with the archived flag set THEN the system SHALL include archived creditors, mirroring the debt listing.
7. IF two creditors of the same user carry the same name THEN the system SHALL accept both.
8. IF a request to a creditor endpoint carries no valid bearer token THEN the system SHALL respond 401.

**Independent Test**: Create a creditor of type `Person` and one of type `Institution`, list both
back, and confirm a second account sees neither.

---

### P1: Register a debt with a schedule ⭐ MVP

**User Story**: As an account owner, I want to record that I borrowed a sum to be repaid over a
number of months so that the system produces the monthly obligations instead of me typing ten of
them.

**Why P1**: This is the core record of the feature and the case the user described first.

**Acceptance Criteria**:
1. WHEN an authenticated user registers a debt with `Mode = Scheduled` THEN the system SHALL persist the debt and its N installments in a single transaction and respond 201.
2. WHEN a debt of N installments is registered THEN the system SHALL create exactly N installments numbered 1 to N, each referencing the debt.
3. WHEN a debt of N installments is registered THEN the system SHALL make the sum of the installment expected amounts equal `TotalAmount` exactly.
4. WHEN a debt of N installments is registered THEN the system SHALL give installment 1 the competence month of `StartDate` when the day of `StartDate` is not after `DueDay`, and the following month otherwise, with each later installment advancing exactly one month.
5. WHEN a debt of N installments is registered THEN the system SHALL set `EndMonth` to the competence month of installment N.
6. WHEN an installment's competence month is shorter than `DueDay` THEN the system SHALL set that installment's due date to the last day of the month.
7. The system SHALL persist `PrincipalAmount` and `TotalAmount` separately and SHALL NOT create any income record from either.
8. IF the referenced creditor, person or category does not belong to the logged user THEN the system SHALL respond 404.
9. IF the name of the debt is empty THEN the system SHALL respond 400 carrying the `NAME_REQUIRED` message.
10. IF `PrincipalAmount` or `TotalAmount` is less than or equal to zero THEN the system SHALL respond 400 carrying the `AMOUNT_GREATER_THAN_ZERO` message.
11. IF `TotalAmount` is less than `PrincipalAmount` THEN the system SHALL respond 400 carrying the `TOTAL_LESS_THAN_PRINCIPAL` message.
12. IF `InstallmentCount` is less than 1 THEN the system SHALL respond 400 carrying the `INSTALLMENT_COUNT_INVALID` message.
13. IF `DueDay` falls outside the range 1 to 31 THEN the system SHALL respond 400 carrying the `DAY_OUT_OF_RANGE` message.
14. IF `Mode` is `Scheduled` and `InstallmentCount` or `DueDay` is absent THEN the system SHALL respond 400 carrying the `SCHEDULE_REQUIRED` message.

**Independent Test**: Register 1500.00 over 10 installments starting on the 20th with a due day of
10, and read back ten installments of 150.00 whose first competence month is the month after the
start date.

---

### P1: Register a debt with no schedule ⭐ MVP

**User Story**: As an account owner, I want to record an informal debt I will pay off whenever I can,
so that it still counts against what I owe even though it has no term.

**Why P1**: The user named this as one of the two shapes a debt takes.

**Acceptance Criteria**:
1. WHEN an authenticated user registers a debt with `Mode = OpenEnded` THEN the system SHALL persist the debt with no installments and respond 201.
2. WHEN a debt with `Mode = OpenEnded` is registered THEN the system SHALL leave `InstallmentCount`, `DueDay` and `EndMonth` null.
3. IF `Mode` is `OpenEnded` and `InstallmentCount` or `DueDay` is supplied THEN the system SHALL respond 400 carrying the `SCHEDULE_NOT_ALLOWED` message.
4. The system SHALL apply the same name, amount and ownership validation as a scheduled debt.

**Independent Test**: Register an open-ended debt of 300.00, read it back with zero installments and
an outstanding balance of 300.00.

---

### P1: Record a payment against a debt ⭐ MVP

**User Story**: As an account owner, I want to record that I paid an installment - saying at that
moment whether it went out on debit, card or pix - so that the debt's balance reflects what I
actually did.

**Why P1**: Without payments a debt never shrinks and the monthly view has nothing to compare
against.

**Acceptance Criteria**:
1. WHEN an authenticated user records a payment against a scheduled installment THEN the system SHALL persist it with the amount, the payment date, the optional type, the optional account and the notes, and respond 201.
2. WHEN a payment is recorded against a scheduled installment THEN the system SHALL set its reference month to that installment's competence month, ignoring any month supplied by the request.
3. WHEN a payment is recorded against an open-ended debt THEN the system SHALL set its reference month to the first day of the month of its payment date and SHALL leave `DebtInstallmentId` null.
4. WHEN an authenticated user corrects a recorded payment THEN the system SHALL overwrite its amount, date, type, account and notes without moving its reference month or its installment.
5. The system SHALL accept a payment whose account belongs to a different person of the same user.
6. The system SHALL accept a payment with a null type and a null account.
7. IF the payment type is `Credit` and no account is supplied THEN the system SHALL respond 400 carrying the `ACCOUNT_REQUIRED_FOR_CREDIT` message.
8. IF a payment already exists for the referenced installment THEN the system SHALL respond 400 carrying the `PAYMENT_ALREADY_RECORDED` message.
9. IF the amount paid is less than or equal to zero THEN the system SHALL respond 400 carrying the `AMOUNT_GREATER_THAN_ZERO` message.
10. IF the referenced debt, installment, payment or account does not belong to the logged user THEN the system SHALL respond 404.
11. IF the referenced debt is archived THEN the system SHALL respond 400 carrying the `DEBT_ARCHIVED` message.
12. IF the referenced installment does not belong to the referenced debt THEN the system SHALL respond 404.

**Independent Test**: Pay installment 1 of a ten-installment debt by pix, read the debt back with an
outstanding balance reduced by exactly that amount, and confirm a second payment on the same
installment is rejected.

---

### P1: View the debt obligations of a given month ⭐ MVP

**User Story**: As an account owner, I want to see which debt installments fall in a month, what each
was expected to cost and what it actually cost, so that I know how much of the month is already
spoken for.

**Why P1**: This is what makes a debt visible day to day rather than a record filed once.

**Acceptance Criteria**:
1. WHEN an authenticated user requests the debts of a competence month THEN the system SHALL return one line per scheduled installment whose competence month equals it, carrying the debt name, the creditor name, the creditor type, the person, the category, the installment number, the installment count, the due date, the expected amount, the amount paid, the payment type and the payment account name.
2. WHEN a line has no payment THEN the system SHALL report its status as `Pending` and a null amount paid.
3. WHEN a line has a payment equal to its expected amount THEN the system SHALL report its status as `Paid`.
4. WHEN a line has a payment different from its expected amount THEN the system SHALL report its status as `Divergent`.
5. WHEN a line is `Pending` and its due date is before the current date THEN the system SHALL report `IsOverdue` as true, and false in every other case.
6. WHEN an open-ended debt has a payment in the requested month THEN the system SHALL return one line for that payment with a null expected amount, a null installment number and the status `Paid`.
7. WHEN an open-ended debt has no payment in the requested month THEN the system SHALL omit it from the response.
8. WHEN a debt is archived THEN the system SHALL omit its lines from the month while keeping its installments and payments in the database.
9. WHEN the month is requested THEN the system SHALL return `TotalExpected`, `TotalPaid` and `TotalCommitted`, where the committed amount of a line is the amount paid when one exists and the expected amount while it does not.
10. IF the requested month is outside the range 1 to 12, or the year outside 1 to 9999, THEN the system SHALL respond 400 carrying the `REFERENCE_MONTH_INVALID` message.
11. WHEN a month holds no debt lines THEN the system SHALL respond 200 with empty lines and zeroed totals.

**Independent Test**: With a ten-installment debt paid only in month 1, read month 1 as `Paid` and
month 2 as `Pending`, and confirm the committed total of month 2 equals the expected amount.

---

### P1: See what is still owed, and to whom ⭐ MVP

**User Story**: As an account owner, I want to open a creditor and see every debt I have with them
and the total still outstanding, so that I can answer "how much do I still owe my father" in one
place.

**Why P1**: This is the capability the user named explicitly - locating the party responsible for a
debt.

**Acceptance Criteria**:
1. WHEN an authenticated user requests a debt THEN the system SHALL return it with its creditor, its person, its category, its full schedule, its payments, its outstanding balance and whether it is settled.
2. WHEN a debt is returned THEN the system SHALL compute its outstanding balance as `TotalAmount` minus the sum of its payments, and SHALL report it as settled when that balance is less than or equal to zero.
3. WHEN an authenticated user lists debts THEN the system SHALL return only debts owned through that user's people, and SHALL support filtering by creditor and by person.
4. WHEN debts are listed without an explicit flag THEN the system SHALL omit archived and settled debts, and SHALL include them when the flag is set.
5. WHEN an authenticated user requests a creditor's summary THEN the system SHALL return the creditor, the count of its unsettled debts, the sum of their total amounts, the sum of what has been paid against them and the resulting outstanding balance.
6. WHEN an authenticated user archives a debt THEN the system SHALL set `Archived` and omit it from the default listing and from every month.
7. IF the referenced debt or creditor does not belong to the logged user THEN the system SHALL respond 404.

**Independent Test**: With two debts against the same creditor, one settled, request that creditor's
summary and confirm the outstanding balance equals only the unsettled debt's remainder.

---

### P2: See the month's debts alongside income and expenses

**User Story**: As an account owner, I want the dashboard to subtract what I owe this month, so that
the balance it shows is money I can actually spend.

**Why P2**: The monthly debt view is usable on its own; this makes the existing dashboard honest.

**Acceptance Criteria**:
1. WHEN an authenticated user requests the dashboard for a month THEN the system SHALL return the income, the expenses and the debts of that month in one response.
2. WHEN the dashboard is composed THEN the system SHALL obtain the debt half by invoking `IGetMonthlyDebtUseCase` and SHALL NOT read, reimplement or modify any debt, expense or income type inside the dashboard use case.
3. WHEN the dashboard is composed THEN the system SHALL compute its balance as the received income minus the committed expenses minus the committed debts.
4. The system SHALL leave `ResponseMonthlyExpenseJson` and every expense use case unchanged.
5. IF the dashboard request carries no valid bearer token THEN the system SHALL respond 401.

**Independent Test**: With income 5000, committed expenses 2000 and a debt installment of 150 in the
same month, read a dashboard balance of 2850.

---

## Edge Cases

- IF a scheduled debt's total does not divide evenly by its installment count THEN the system SHALL place the entire remainder on the last installment.
- IF `DueDay` is 31 and an installment's month is shorter THEN the system SHALL set that installment's due date to the last day of the month rather than failing.
- IF a debt starts on a day equal to its due day THEN the system SHALL place installment 1 in the month of the start date, not the following one.
- IF a payment exceeds the outstanding balance THEN the system SHALL accept it, report the balance as zero or negative, and report the debt as settled.
- IF a payment is corrected to a smaller amount after a debt was settled THEN the system SHALL report the debt as unsettled again, because the state is derived and not stored.
- IF an open-ended debt receives two payments in the same month THEN the system SHALL accept both and return two lines for that month.
- IF a creditor is archived while it still has unsettled debts THEN the system SHALL keep those debts listable and payable; archiving hides the creditor from the picker only.
- IF a debt's category or creditor is shared with an expense or another debt THEN the system SHALL accept it; nothing is exclusive.
- WHEN a debt is archived after payments were recorded THEN the system SHALL keep those payments retrievable through the database while omitting the lines from every month.

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| -------------- | ----- | ----- | ------ |
| CRED-01 | P1: Keep a catalogue of creditors | Schema foundation | Pending |
| CRED-02 | P1: Keep a catalogue of creditors | Creditor slice | Pending |
| CRED-03 | P1: Keep a catalogue of creditors | Domain rules and messages | Pending |
| DEBT-01 | P1: Register a debt with a schedule | Schema foundation | Pending |
| DEBT-02 | P1: Register a debt with a schedule | Debt registration | Pending |
| DEBT-03 | P1: Register a debt with a schedule | Domain rules and messages | Pending |
| DSCH-01 | P1: Register a debt with a schedule | Schedule generation | Pending |
| DSCH-02 | P1: Register a debt with a schedule | Schedule generation | Pending |
| DSCH-03 | P1: Register a debt with no schedule | Debt registration | Pending |
| DPAY-01 | P1: Record a payment against a debt | Schema foundation | Pending |
| DPAY-02 | P1: Record a payment against a debt | Debt payments | Pending |
| DPAY-03 | P1: Record a payment against a debt | Debt payments | Pending |
| DPAY-04 | P1: Record a payment against a debt | Domain rules and messages | Pending |
| DVEW-01 | P1: View the debt obligations of a given month | Monthly debt view | Pending |
| DVEW-02 | P1: View the debt obligations of a given month | Monthly debt view | Pending |
| DVEW-03 | P1: View the debt obligations of a given month | Monthly debt view | Pending |
| DBAL-01 | P1: See what is still owed, and to whom | Debt reads | Pending |
| DBAL-02 | P1: See what is still owed, and to whom | Debt reads | Pending |
| DBAL-03 | P1: See what is still owed, and to whom | Creditor slice | Pending |
| DDSH-01 | P2: See the month's debts alongside income and expenses | Dashboard | Pending |
| DDSH-02 | P2: See the month's debts alongside income and expenses | Dashboard | Pending |

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 21 total, 0 mapped to tasks, 21 unmapped. Mapping happens in the Tasks phase.

### Requirement coverage map

| Requirement | Covers |
| ----------- | ------ |
| CRED-01 | `Creditor` entity carrying `UserId`, its `CreditorType`, nullable contact and notes |
| CRED-02 | Creditor creation, listing scoped to the logged user, archiving |
| CRED-03 | Creditor validation: empty name, duplicate names accepted, 401 |
| DEBT-01 | `Debt` entity, its two owning references, `PrincipalAmount` versus `TotalAmount`, no income record |
| DEBT-02 | Debt registration transactionally with its schedule, response shape |
| DEBT-03 | Debt validation: names, amounts, total below principal, ownership 404s |
| DSCH-01 | `DebtInstallment` generation: count, numbering, amounts summing exactly, `EndMonth` |
| DSCH-02 | First competence month from `StartDate` versus `DueDay`, monthly advance, due-day clamping |
| DSCH-03 | `OpenEnded` mode: no installments, schedule fields rejected and left null |
| DPAY-01 | `DebtPayment` entity, nullable installment, nullable type and account, unique per installment |
| DPAY-02 | Payment registration and reference-month derivation for both modes |
| DPAY-03 | Payment correction without moving month or installment |
| DPAY-04 | Payment validation: credit without account, duplicate, amount, archived debt, foreign installment |
| DVEW-01 | Monthly lines for scheduled installments with creditor, person, category and payment detail |
| DVEW-02 | Per-line status `Pending` / `Paid` / `Divergent` and the `IsOverdue` flag |
| DVEW-03 | Open-ended lines, archived omission, month totals, invalid month, empty state |
| DBAL-01 | Debt detail with schedule, payments, computed outstanding balance and settled state |
| DBAL-02 | Debt listing with creditor and person filters, archived and settled exclusion, archiving |
| DBAL-03 | Creditor summary: unsettled debt count, totals owed, paid and outstanding |
| DDSH-01 | Dashboard composing `IGetMonthlyDebtUseCase` without touching income or expense code |
| DDSH-02 | Dashboard balance net of committed debts, and its 401 without a token |

---

## Success Criteria

- [ ] `dotnet build` reports zero errors and zero warnings, and `dotnet test` is green including every pre-existing test, none of which is edited.
- [ ] A debt of 1000.00 over 3 installments produces expected amounts of 333.33, 333.33 and 333.34 in three consecutive competence months.
- [ ] A debt started on the 20th with a due day of 10 places its first installment in the following month; one started on the 10th places it in the month of the start date.
- [ ] A debt with a due day of 31 whose installment falls in February carries a due date of the 28th or 29th.
- [ ] Paying an installment by pix with no account is accepted; paying it by credit with no account is rejected with `ACCOUNT_REQUIRED_FOR_CREDIT`.
- [ ] An open-ended debt of 300.00 reduced by two payments of 100.00 reports an outstanding balance of 100.00 and is not settled; a third payment of 100.00 settles it.
- [ ] A creditor's summary aggregates only its unsettled debts and matches the sum of those debts' outstanding balances.
- [ ] With income 5000, committed expenses 2000 and a debt installment of 150, the dashboard reports a balance of 2850, and `ResponseMonthlyExpenseJson` is unchanged from before the feature.
