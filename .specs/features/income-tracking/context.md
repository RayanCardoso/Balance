# Income Tracking Context

**Gathered:** 2026-08-10
**Spec:** `.specs/features/income-tracking/spec.md`
**Status:** Ready for design

---

## Feature Boundary

Income only: `BaseEntity`, `Person`, `IncomeSource`, `IncomeSourceVersion`, `IncomePayment`, the
`IncomeType` enum, their repositories, an `IncomeController` and a `PersonController`, six use cases,
one migration and tests. The `User` entity is migrated onto `BaseEntity` as a prerequisite. Nothing
about expenses, budgets, reports or archiving operations.

---

## Implementation Decisions

### Naming

The user's Portuguese table names were examples. The implementation uses English throughout:

| User's name | Implementation |
| ----------- | -------------- |
| `Pessoa` | `Person` |
| `ReceitaBase` | `IncomeSource` |
| `ReceitaBaseVersao` | `IncomeSourceVersion` |
| `ReceitaHistoricoPagamento` | `IncomePayment` |
| `TipoReceita` | `IncomeType` (`Recurring`, `Variable`) |
| `RazãoMudança` | `ChangeReason` |
| `DiaEsperado` | `ExpectedDay` |
| `ValidadeInicial` / `ValidadeFinal` | `ValidityStart` / `ValidityEnd` |
| `MesReferência` | `ReferenceMonth` |
| `ValorRecebido` | `AmountReceived` |
| `Arquivado` | `Archived` |

### Identity and audit

- `BaseEntity` carries `Guid Id`, `DateTime CreatedAt`, `DateTime? UpdatedAt`, all UTC.
- `User` migrates onto it. Its `long Id` and `Guid UserIdentifier` collapse into the single `Guid Id`.
- The JWT `Sid` claim carries `Id`. `LoggedUser` resolves on `Id`.
- This lands as its own phase with its own commit, before any income work.

### Ownership

- `Person` carries `UserId`. Ownership cascades `IncomePayment` → `IncomeSource` → `Person` → `User`.
- The user is himself a `Person` of his account; other People (e.g. spouse) are registered under the
  same account and are equally eligible to own income sources.
- The owner `Person` is created automatically inside `RegisterUserUseCase`, flagged `IsAccountOwner = true`.
- Every read filters by `ILoggedUser.Get()`. Every new endpoint carries `[Authorize]` - these are the
  first authorized endpoints in the solution.

### Version history

- `IncomePayment.IncomeSourceVersionId` is a nullable FK freezing which version was in effect when the
  payment was recorded. Chosen over deriving the version from dates so that correcting a validity date
  later cannot silently rewrite past history.
- Only Recurring sources have versions. Variable sources have none, and their payments carry a null
  version reference.
- `ChangeIncomeSourceValue` closes the current version by setting `ValidityEnd` to the day before the
  new `ValidityStart`, then opens the new version, both in one transaction. The timeline therefore has
  no gaps and no overlaps by construction.
- Already recorded payments keep pointing at their original version.

### Use cases

Six in total:

1. `RegisterPerson`
2. `GetAllPeople`
3. `RegisterIncomeSource` - creates the source plus, for Recurring, its first open version
4. `RegisterIncomePayment` - resolves and freezes the version in effect
5. `GetMonthlyIncome` - the reconciled view
6. `ChangeIncomeSourceValue` - closes the old version, opens the new one

### Monthly view

- Keyed on `ReferenceMonth`, never on `PaymentDate`. A salary paid on 03 September for August counts
  as August.
- One line per non-archived source of the user, whether or not it was paid.
- Recurring lines carry the expected amount and expected day resolved from the version in effect for
  that month; Variable lines carry a null expected amount.
- Received amount is the sum of that month's payments, so split payments add up.
- Per-source status: `Received`, `Pending`, `Divergent`.
- The response carries month totals for expected and received.

### Agent's Discretion

- Money as `decimal` mapped to `numeric(18,2)`; `ReferenceMonth` as a `DateOnly` normalized to the
  first day of the month.
- Repository split (read-only / write-only interfaces), validator style, error-message keys and their
  `pt-BR` translations follow the conventions already in the solution.
- Placement of the status calculation (use case versus a domain method) is a design-phase call.

### Declined / Undiscussed Gray Areas → Assumptions

None declined - the user chose to discuss all six gray areas. The consequences that surfaced during
discussion but were not put to a vote are recorded as unconfirmed assumptions in the spec: versions
being Recurring-only, multiple payments per month being allowed, money and month representation,
UTC timestamps, last-write-wins concurrency, and observability and external-dependency failure being
marked N/A.

---

## Specific References

The user supplied a hand-drawn entity diagram (`.docs/modelo.excalidraw` and an image in the
conversation) showing `ReceitaHistoricoPagamento` → `ReceitaBase` ← `ReceitaBaseVersao`, with
`ReceitaBase` → `Enum TipoReceita`. The stated purpose, in the user's words: to know the income, the
history of when he earned less and the reason he now earns more, plus variable earnings and when he
was actually paid.

The user clarified that fields named after other tables denote a relationship, which resolved
`ReceitaBaseHistorico` into the version foreign key on the payment.

---

## Deferred Ideas

- Archive and unarchive operations for an income source. The `Archived` field is honored by every
  query but nothing sets it in this delivery.
- Update and delete for `Person`.
- Correcting or deleting a recorded payment.
- Expenses and the balance between income and spending.
- Reports and exports over the monthly view.
