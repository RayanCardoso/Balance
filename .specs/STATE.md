# Project State

Project memory for Balance. Decisions are project-level constraints every future feature must conform
to or explicitly supersede.

---

## Decisions

### AD-001: Every persisted entity inherits `BaseEntity`

**Status:** active
**Date:** 2026-08-10
**Feature:** income-tracking

Every entity persisted by `BalanceDbContext` inherits `BaseEntity`, which supplies `Guid Id`,
`DateTime CreatedAt` and `DateTime? UpdatedAt`. Timestamps are stamped centrally in the DbContext's
`SaveChanges` / `SaveChangesAsync` override, never by a use case.

**Consequence:** `User` no longer has a `long Id` or a separate `UserIdentifier`; its `Guid Id` is
both the primary key and the public handle carried in the JWT `Sid` claim.

### AD-002: Ownership cascades through `Person`

**Status:** active
**Date:** 2026-08-10
**Feature:** income-tracking

User-owned data hangs off `Person`, which carries `UserId`. Ownership for income is
`IncomePayment` → `IncomeSource` → `Person` → `User`. A user is himself a `Person` of his account
(`IsAccountOwner = true`), created automatically during registration.

**Consequence:** new user-owned entities attach to `Person`, not to `User` directly, unless they are
genuinely account-level rather than person-level.

### AD-003: Ownership is enforced in the repository, not the controller

**Status:** active
**Date:** 2026-08-10
**Feature:** income-tracking

Every repository read method that returns user-owned rows takes the logged `User` as a parameter and
filters on it. A use case cannot obtain unscoped rows.

**Consequence:** bypassing ownership requires deleting a method parameter, which is visible in review,
rather than forgetting a `Where` clause, which is not.

### AD-004: Not-owned reads answer 404, never 403

**Status:** active
**Date:** 2026-08-10
**Feature:** income-tracking

When a caller references an id that exists but belongs to another account, the API answers 404 -
the same response as a non-existent id.

**Consequence:** identifiers cannot be probed for existence across accounts.

### AD-005: Ownership attaches to `Person` or to `User`, decided per entity

**Status:** active
**Date:** 2026-08-12
**Feature:** expense-tracking

AD-002 is refined, not replaced. A user-owned entity attaches to `Person` when it belongs to one member
of the household, and to `User` when the whole household shares it. `Category` carries `UserId`;
`Account`, `Expense`, `InstallmentPlan` and `RecurringExpense` carry `PersonId`.

**Consequence:** every new entity states which of the two it is and why. Repository reads filter on
`UserId` or on `Person.UserId` accordingly, and an entity may legitimately reference another entity
owned by a different `Person` of the same `User` - an expense of one person paid on another's account,
for instance. Both sides are still checked against the logged user.

### AD-006: Income code is read-only for other features

**Status:** active
**Date:** 2026-08-12
**Feature:** expense-tracking

No feature outside `income-tracking` modifies an income entity, repository, use case or test.
Integration happens by composing the published use case interfaces - `GetMonthlyDashboardUseCase`
injects `IGetMonthlyIncomeUseCase` and calls it.

**Consequence:** a rule shared with income is duplicated rather than extracted when extraction would
require editing income. `RecurringExpenseExtensions.VersionInEffect` duplicates
`IncomeSourceExtensions.VersionInEffect` for exactly this reason, and the duplication is recorded in
`design.md` so it is not "fixed" by accident. Unifying them is a task for whenever income is next
opened for its own reasons.

---

## Handoff

**Last updated:** 2026-08-12
**Branch:** `feature/expense-tracking`
**Feature in flight:** `expense-tracking`

**Where things stand:** both features are complete and verified. `income-tracking`: 20 tasks, PASS,
5/5 sensor mutations killed. `expense-tracking`: all 49 tasks done, **349 tests**, PASS on validation
iteration 2 with 12/12 sensor mutations killed and `validate_state.py` exiting 0.

Iteration 1 returned FAIL on a real defect: `DueDay` reached the monthly line and the page while no
test asserted it, so zeroing it left the whole suite green. Fixed, then re-injected in a scratch
worktree to confirm the new tests kill it rather than trusting the claim. A twelfth mutation
(`MidpointRounding.AwayFromZero` → `ToEven`) survived iteration 2 as a cosmetic finding and was closed
the same way — the old test recomputed its expectation with the implementation's own `Math.Round`
call, so it followed the code instead of pinning it.

Branch `feature/expense-tracking` holds 55 commits on top of `main`, which itself carries the 22
unpushed income commits. **No push has been authorised for either feature** — that is the user's call.
The frontend is NOT under version control: `C:\estudos\projetos\Balance\frontend` has no git
repository, so `seed.mjs`, the Vite project and `node_modules` exist only on disk.

**Known gaps carried forward:** the archive operation for an income source (its filter exists and is
honoured, but nothing sets `Archived`); update and delete for `Person`; correcting a recorded income
payment. Expense-specific deferrals are listed in that feature's `context.md`.

**Running services (started during phase 10):** PostgreSQL in Docker as `Balance_postgres`; a Vite dev
server on 5173. The dev API on 5126 was stopped so it would stop holding copy-locks on
`src\Balance.Api\bin\Debug\net10.0\*.dll` during builds — restart it before using the page.

**Environment notes (re-checked 2026-08-12):**

- .NET 10.0.103. `dotnet build Balance.sln` exits 0. The `Directory.Build.targets` pin recorded in the
  previous handoff is gone and the runtime is repaired - the note about corrupt 10.0.9 DLLs no longer
  applies.
- Python 3.13 is no longer installed. The skill validators run on LibreOffice's bundled CPython at
  `C:\Program Files\LibreOffice\program\python.exe` (3.10.19). The `python` and `python3` names on PATH
  are Microsoft Store stubs and do not work.
- Docker is running. PostgreSQL is mapped to host port **5434**, not 5432: this machine runs local
  `postgresql-x64-17` and `postgresql-x64-18` services that hold 5432 and 5433 and answer `localhost`
  before the container, which produced `28P01 password authentication failed` against the wrong
  server. Integration tests still use the EF Core in-memory provider and need no database.
- System Node is v12.6.0, too old for Vite. NVM holds v20.19.4 and v18.20.4, but `nvm use` needs admin.
  Invoke Node directly: `%APPDATA%\nvm\v20.19.4\node.exe` (verified, ships npm 10.8.2).
