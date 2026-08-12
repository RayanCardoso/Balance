# Expense Tracking Context

**Gathered:** 2026-08-12
**Spec:** `.specs/features/expense-tracking/spec.md`
**Status:** Ready for design

---

## Feature Boundary

Expenses, plus the two shared tables they need. New: `Category`, `Account`, `Expense`,
`InstallmentPlan`, `RecurringExpense`, `RecurringExpenseVersion`, `RecurringExpensePayment`, the
`ExpenseType` and `ExpensePriority` and `ExpenseStatus` enums, their repositories, controllers, use
cases, one migration and tests. Plus a dashboard endpoint that composes the existing monthly income
view with the new monthly expense view.

**The income feature is not modified.** `IncomeSource`, `IncomeSourceVersion`, `IncomePayment` and
their four use cases are read and reused, never edited. The dashboard calls
`IGetMonthlyIncomeUseCase` as it stands.

---

## Implementation Decisions

### Naming

The user's Portuguese table names are examples. The implementation uses English throughout, matching
the income feature:

| User's name | Implementation |
| ----------- | -------------- |
| `Categoria` | `Category` |
| `Prioridade` | `Priority` (`Essential`, `Important`, `Superfluous`) |
| `Conta` | `Account` |
| `Instituicao` | `Institution` |
| `DiaFechamento` / `DiaVencimento` | `ClosingDay` / `DueDay` |
| `Limite` | `Limit` |
| `TipoDespesa` | `ExpenseType` (`Credit`, `Debit`, `Pix`) |
| `Despesa` | `Expense` |
| `MesCompetencia` | `CompetenceMonth` |
| `Parcela` | `InstallmentNumber` |
| `Parcelamento` | `InstallmentPlan` |
| `Parcelas` | `InstallmentCount` |
| `DataInicial` / `DataFinal` | `StartDate` / `EndDate` |
| `DespesaRecorrente` | `RecurringExpense` |
| `EhEstimativa` | `IsEstimate` |
| `Arquivado` | `Archived` |
| `DespesaRecorrenteVersao` | `RecurringExpenseVersion` |
| `RazaoMudanca` | `ChangeReason` |
| `ValidadeInicial` / `ValidadeFinal` | `ValidityStart` / `ValidityEnd` |
| `DespesaRecorrentePagamento` | `RecurringExpensePayment` |
| `DataReferencia` | `ReferenceMonth` |
| `DataPagamento` | `PaymentDate` |
| `ValorPago` | `AmountPaid` |
| `Observacoes` | `Notes` |

### Ownership — user's decision

Split, because the two shared tables have different sharing semantics:

- **`Category` is account-level.** It carries `UserId` directly. In the user's words: *"a categoria
  faz parte de UserId pois pode ser usado por todos os person derivados dele"*. One `Mercado`
  category serves the whole household.
- **`Account` is person-level.** It carries `PersonId`. In the user's words: *"cada pessoa tem as
  próprias contas, pois pode ter alguns cartões e o usuário outros"*.

`Account` being person-level is the documented exception AD-002 allows for (*"unless they are
genuinely account-level rather than person-level"*) read in reverse: the card belongs to a person, the
category belongs to the account.

Ownership cascades:

- `Category` → `User`
- `Account` → `Person` → `User`
- `Expense` → `Person` → `User`
- `InstallmentPlan` → `Person` → `User`
- `RecurringExpense` → `Person` → `User`
- `RecurringExpenseVersion` → `RecurringExpense` → `Person` → `User`
- `RecurringExpensePayment` → `RecurringExpense` → `Person` → `User`

### Cross-person accounts — user's decision

An `Expense` attributed to person A **may** be paid on an account belonging to person B, as long as
both resolve to the same `User`. `Person` answers *who spent it*, `Account` answers *what paid for
it*. This makes "my wife bought it on my card" representable. Validation checks only that both belong
to the logged user; a foreign account answers 404, not 400.

### Competence month for credit — user's decision

`CompetenceMonth` is derived, with an explicit override:

- `Credit` on an account with a `ClosingDay`: a purchase on or before the closing day lands in the
  month of `Date`; after it, in the following month. This is the whole reason `ClosingDay` exists.
- `Debit`, `Pix`, or `Credit` on an account with no `ClosingDay`: the month of `Date`.
- If the request supplies `CompetenceMonth` explicitly, that value wins, normalised to day 1.

### Installment plans — user's decision

Registering an `InstallmentPlan` generates the N `Expense` rows in one transaction:

- `InstallmentNumber` runs 1..N.
- Each installment is `round(Total / N, 2)`, except the last, which is `Total − sum(previous)`. The N
  amounts therefore sum to the total exactly, with no lost or invented cent.
- The first installment's `CompetenceMonth` comes from the credit rule above applied to `StartDate`;
  each subsequent one advances a month.
- Every generated expense carries `Type = Credit` and `Date = StartDate` — the purchase happened once;
  what advances is the invoice it lands on.
- `EndDate` is computed as the competence month of installment N, not accepted from the request, so
  the two dates can never contradict the installment count.
- The plan carries its own `PersonId`, which the generated expenses inherit. The user's sketch omits
  `Pessoa` on `Parcelamento`, but without it the generated rows have no owner.

### Recurring expenses

- `RecurringExpenseVersion` is the estimate timeline, identical in shape to `IncomeSourceVersion`:
  greatest `ValidityStart` not after the month's last day, with `ValidityEnd` null or not before the
  month's first day. Changing the base value closes the current version at the day before the new
  `ValidityStart` and opens the new one, in one transaction.
- `RecurringExpensePayment` is the real value for one month. `(RecurringExpenseId, ReferenceMonth)` is
  unique — exactly one payment row per expense per month, unlike income, which allows several.
- The payment can be **updated** after the fact, which is the refinement the user described
  (*"esse valor também pode ser alterado para ser refinado"*).
- `IsEstimate` marks whether the version amount is a guess (Luz) or a known fixed value (Netflix). It
  is reported on the monthly line so the frontend can mark a figure as provisional. It never blocks
  anything.
- The payment freezes `RecurringExpenseVersionId`, the same way an income payment freezes its version,
  so correcting a validity date later cannot rewrite recorded history.

### Approved additions

All four requested additions were approved by the user:

1. **`Category` and `Account` endpoints** (create + list). Without them nothing can be registered and
   no `Expense` can exist.
2. **Monthly expense view + combined dashboard.** `GET /api/expense/{year}/{month}` reconciles the
   month's variable expenses and recurring expenses; `GET /api/dashboard/{year}/{month}` composes it
   with the existing monthly income view. This is what feeds the frontend page.
3. **Archive / unarchive for `RecurringExpense`.** The user's *"podem ser excluídas"*, resolved as a
   soft archive so the payment history survives. Shipping the operation alongside the field is the
   direct application of lesson **L-002** — income shipped `Archived` with nothing to set it.
4. **`AccountId` on `RecurringExpensePayment`**, nullable, recording which account actually paid that
   month when it differs from the recurring expense's default.

### Agent's Discretion

- `ClosingDay`, `DueDay` and `Limit` on `Account` are nullable: they describe a credit card, and
  "Inter Débito" has none of them.
- Money as `decimal` mapped to `numeric(18,2)`; `CompetenceMonth` and `ReferenceMonth` as `DateOnly`
  normalised to the first day of the month. Both mirror the income feature.
- Repository read/write split, validator style, error-message keys and their `pt-BR` translations
  follow the conventions already in the solution.
- `Limit` is informational. Nothing validates a total against it.

---

## Environment Constraints

| Constraint | Status |
| ---------- | ------ |
| .NET | 10.0.103, `dotnet build Balance.sln` exits 0. The `Directory.Build.targets` pin recorded in the previous handoff is gone; the runtime is repaired. |
| Docker / PostgreSQL | Daemon down at spec time. The user will start Docker Desktop; seeding and the frontend page depend on it. |
| Node | System Node is v12.6.0, too old for Vite. NVM holds v20.19.4 and v18.20.4. `nvm use` needs admin, so Node is invoked directly from `%APPDATA%\nvm\v20.19.4\node.exe` — verified working with npm 10.8.2. |
| Python | The Python 3.13 install recorded in the previous handoff is gone. The skill validators run on LibreOffice's bundled CPython at `C:\Program Files\LibreOffice\program\python.exe` (3.10.19). The `python` / `python3` names on PATH are Microsoft Store stubs and do not work. |

---

## Deferred Ideas

- Update and delete for `Category` and `Account` — create and list only in this delivery.
- Archive for `Account`; only `RecurringExpense` gets the operation.
- Update, delete or correction of a single `Expense`, and cancelling an `InstallmentPlan` partway.
- Validating a month's credit total against `Account.Limit`.
- Budgets, spending targets, and alerts on `DueDay`.
- Reports and exports over the monthly expense view.
- Archive / unarchive for `IncomeSource`, still carried forward from income-tracking.
