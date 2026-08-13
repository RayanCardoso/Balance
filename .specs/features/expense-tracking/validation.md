# Expense Tracking Validation

**Date**: 2026-08-12
**Spec**: `.specs/features/expense-tracking/spec.md`
**Diff range**: `main..HEAD` (`feature/expense-tracking`, 53 commits, HEAD `b6a1cac`)
**Verifier**: independent sub-agent (author ≠ verifier) — coverage re-derived from `spec.md`, evidence-or-zero

**Verdict**: ❌ **FAIL** — 342/342 tests green and 8/8 of the mandated high-risk mutations killed, but
one acceptance-criterion clause (VIEW AC3, the due day) has **zero evidence at any layer** and its
mutant survives both suites. Two further gaps are recorded below.

---

## Task Completion

All 49 tasks in `tasks.md` are marked ✅ Complete. Five carry recorded deviations; each was judged, not
accepted on the author's word.

| Task | Status | Deviation | Verdict |
| ---- | ------ | --------- | ------- |
| T12, T27 | ✅ Done | Culture coverage for validation messages deferred from the use-case layer to the endpoint layer | **Justified.** Matches the pre-existing Person/Income convention; both cultures are asserted at the endpoint layer (`RecurringExpenseEndpointsTest.cs:163-236`, `RegisterExpenseTest.cs:112-149`). |
| T18, T28, T35 | ✅ Done | `SPEC_DEVIATION`: `CATEGORY_NOT_FOUND`, `ACCOUNT_NOT_FOUND`, `RECURRING_EXPENSE_NOT_FOUND`, `RECURRING_EXPENSE_PAYMENT_NOT_FOUND` added beyond the design's error table | **Justified.** The design named only `PERSON_NOT_FOUND`, which would have answered a foreign *category* with "Person not found". The 404 status AD-004 pins is unchanged; only the body names the right entity. Each key is asserted (`RegisterExpenseUseCaseTest.cs:186,200`; `ChangeRecurringExpenseValueUseCaseTest.cs:176`; `UpdateRecurringExpensePaymentUseCaseTest.cs:149`). |
| T26 | ✅ Done | `ResponseRecurringExpenseJson` carries the version **history** as a collection, not just the current version | **Justified.** Without it RECR-03 AC1 is unobservable at the endpoint layer. The collection is what makes `RecurringExpenseEndpointsTest.cs:77-86` able to assert the closed version at its exact end date. |
| T39 | ✅ Done | `CategoryName` / `CategoryPriority` / `AccountName` removed from the **recurring** line; "archived omitted" test moved to T41 | **Justified.** `RecurringExpenseRepository.GetForMonth` does not load catalogue navigations, so those fields would ship always-blank. Moving the archived-omission test to T41 is correct: the `Archived == false` filter lives in the repository, so a mocked-repository test would assert what the mock was told to return. It is genuinely covered at T41 (`GetMonthlyExpenseTest.cs:93-121`). |
| T47 | ✅ Done | `SPEC_DEVIATION`: docker-compose host port moved 5432 → 5434 | **Justified.** Environment-local collision with two installed PostgreSQL services; the container's own port is unchanged and the change is one line, reversible, confined to the repo. |

---

## Constraint checks

### AD-006 — income code is read-only

| Check | Evidence | Result |
| ----- | -------- | ------ |
| No income file modified | `git diff --name-only main..HEAD \| grep -Ei 'income\|Incomes/'` → **no matches** (exit 1) | ✅ |
| No file deleted or renamed | `git diff --name-status --diff-filter=DR main..HEAD` → **empty** | ✅ |
| Dashboard composes rather than reimplements | `GetMonthlyDashboardUseCase.cs:14,28` injects and invokes `IGetMonthlyIncomeUseCase`; no income entity or repository is referenced | ✅ |
| Composition asserted, not assumed | `GetMonthlyDashboardUseCaseTest.cs:26` `result.Income.ShouldBeSameAs(income)` — reference identity, so a reshaped or refiltered income half fails; `:83` `incomeUseCase.Verify(u => u.Execute(2026, 8), Times.Once)` | ✅ |
| Income half byte-identical end to end | `GetMonthlyDashboardTest.cs:52` `dashboard.GetProperty("income").GetRawText().ShouldBe(income.GetRawText())` — raw serialised JSON against `GET /api/income/2026/8` | ✅ |

### "All 82 pre-existing tests stay green **and unedited**"

Checked against the diff, not only against the suite being green.

| Check | Evidence | Result |
| ----- | -------- | ------ |
| Pre-existing files modified (M/D/R) | 12 files, listed below — **not one is a test file** | ✅ |
| Pre-existing `*Test.cs` edited | none appear in the M/D/R list; every test file in the diff is an **addition** | ✅ |
| Test count | 82 → 342 (**+260**), 0 removed, 0 skipped | ✅ |

Modified pre-existing files: `.specs/STATE.md`, `docker-compose.yml`, `Program.cs`,
`appsettings.Development.json`, both `DependencyInjectionExtension.cs`, the `ResourceErrorMessages`
trio, `BalanceDbContext.cs`, `BalanceDbContextModelSnapshot.cs`, and
`tests/CommonTestUtilities/Repositories/UnitOfWorkBuilder.cs`.

`UnitOfWorkBuilder.cs` is the only file under `tests/` that was edited. It is a shared *utility*, not a
test, and the change is **purely additive** — the existing `static Build()` used by the 82 income tests
is untouched; a new instance-based `BuildCounting()` was added alongside it for the commit-count
assertions. No pre-existing behaviour changed.

---

## Spec-Anchored Acceptance Criteria

72 criteria across 9 stories. Every one traced to `file:line` + the assertion expression, and checked
against the outcome the spec pins — not merely that an assertion exists.

### P1: Keep a catalogue of categories and accounts (SHAR-01..03)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 create category → persisted with name/description/priority, 201 | 201 + the three fields | `WebApi.Test/Categories/CategoryEndpointsTest.cs:27-34` — `StatusCode.ShouldBe(Created)`, `name/description/priority` each `ShouldBe(request.*)` | ✅ PASS |
| AC1 linked to that user | `UserId` = logged user | `UseCases.Test/Categories/Register/RegisterCategoryUseCaseTest.cs:45` — `writeRepository.Added!.UserId.ShouldBe(loggedUser.Id)` | ✅ PASS |
| AC2 list categories → only that user's | second account sees none | `CategoryEndpointsTest.cs:65-68` — first list `ShouldContain(...name)`, `secondList...ShouldBeEmpty()` | ✅ PASS |
| AC3 create account → persisted to the person, 201 | 201 + `personId` | `CategoryEndpointsTest.cs:79-89` — `Created`, `personId.ShouldBe(personId)`, plus institution/closingDay/dueDay/limit | ✅ PASS |
| AC4 list accounts → only accounts whose person is that user's | second account sees none | `CategoryEndpointsTest.cs:120-123` — `secondList...ShouldBeEmpty()` | ✅ PASS |
| AC5 foreign person on an account → 404 | 404 | `CategoryEndpointsTest.cs:136` — `ShouldBe(HttpStatusCode.NotFound)`; message asserted at `RegisterAccountUseCaseTest.cs:77` (`PERSON_NOT_FOUND`) | ✅ PASS |
| AC6 empty name → 400 `NAME_REQUIRED` | that exact key | `RegisterCategoryUseCaseTest.cs:61` and `RegisterAccountUseCaseTest.cs:93` — `ShouldBe(ResourceErrorMessages.NAME_REQUIRED)`; validators `RegisterCategoryValidatorTest.cs:38`, `RegisterAccountValidatorTest.cs:37` | ⚠️ PASS with note — the *key* is pinned at two layers, but no endpoint test asserts the **400 status** for the catalogue routes specifically. The `ErrorOnValidationException` → 400 mapping is proved by `RegisterExpenseTest.cs:142`. Acceptable. |
| AC7 closing/due day outside 1..31 → 400 `DAY_OUT_OF_RANGE` | that exact key, both bounds | `RegisterAccountUseCaseTest.cs:99-130` — `[InlineData(0)] [InlineData(32)]` for both days, `ShouldContain(DAY_OUT_OF_RANGE)`; valid bounds 1 and 31 accepted at `RegisterAccountValidatorTest.cs:73` | ✅ PASS |
| AC8 accept null closing day, due day, limit | all three null | `RegisterAccountUseCaseTest.cs:48-50` — `ClosingDay/DueDay/Limit .ShouldBeNull()` | ✅ PASS |
| AC9 no bearer token → 401 | 401 on all four routes | `CategoryEndpointsTest.cs:42, 50, 97, 105` — `ShouldBe(HttpStatusCode.Unauthorized)` ×4 | ✅ PASS |

### P1: Register an expense (EXPN-01..03)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 persist name/person/type/amount/category/account/date, 201 | all seven + 201 | `UseCases.Test/Expenses/Register/RegisterExpenseUseCaseTest.cs:26-41` — seven `added.*.ShouldBe(...)` on the persisted entity **and** seven on the response; endpoint `WebApi.Test/Expenses/RegisterExpenseTest.cs:35-46` | ✅ PASS |
| AC2 credit, day ≤ closing day → month of the date | first day of that month | `CompetenceMonthResolverTest.cs:20-22` (day 20 of 20 → `2026-08-01`), `:12-14` (day 15); use case `RegisterExpenseUseCaseTest.cs:68`; endpoint `RegisterExpenseTest.cs:62` `"2026-08-01"` | ✅ PASS |
| AC3 credit, day > closing day → following month | first day of the next month | `CompetenceMonthResolverTest.cs:28-30` (21 > 20 → `2026-09-01`), `:36-38` (Dec → `2027-01-01`); `RegisterExpenseUseCaseTest.cs:54-55`; endpoint `RegisterExpenseTest.cs:47` `"2026-09-01"` | ✅ PASS |
| AC4 debit/pix → month of the date | closing day ignored | `CompetenceMonthResolverTest.cs:52-56` `[Debit][Pix]`; `RegisterExpenseUseCaseTest.cs:87-96` | ✅ PASS |
| AC5 credit with no closing day → month of the date | no roll | `CompetenceMonthResolverTest.cs:44-46`; `RegisterExpenseUseCaseTest.cs:109` | ✅ PASS |
| AC6 explicit competence month overrides, normalised to day 1 | `2026-11-17` → `2026-11-01`, beating a derived `2026-09-01` | `RegisterExpenseUseCaseTest.cs:124-125` — `ShouldBe(new DateOnly(2026, 11, 1))` on both result and persisted entity | ✅ PASS |
| AC7 accept an account of a different person of the same user | 201, both ids kept | `RegisterExpenseUseCaseTest.cs:152-158` — `PersonId` = spender, `AccountId` = card holder's, `account.PersonId.ShouldNotBe(spender.Id)`; endpoint `RegisterExpenseTest.cs:80-81` | ✅ PASS |
| AC8 foreign person/category/account → 404 | 404, each naming its entity | `RegisterExpenseUseCaseTest.cs:172, 186, 200` — `PERSON_NOT_FOUND` / `CATEGORY_NOT_FOUND` / `ACCOUNT_NOT_FOUND`; endpoint `RegisterExpenseTest.cs:94, 107` `NotFound` | ✅ PASS |
| AC9 empty name → 400 `NAME_REQUIRED` | that key, both cultures | `RegisterExpenseUseCaseTest.cs:214`; endpoint both cultures `RegisterExpenseTest.cs:133-149` | ✅ PASS |
| AC10 amount ≤ 0 → 400 `AMOUNT_GREATER_THAN_ZERO` | that key | `RegisterExpenseUseCaseTest.cs:230` `[0][-1]`; endpoint both cultures `RegisterExpenseTest.cs:112-128` | ✅ PASS |

### P1: Register an installment purchase (INST-01..03)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 plan + installments in one transaction, 201 | exactly one `Commit()` | `RegisterInstallmentPlanUseCaseTest.cs:168-170` — plan added, 6 installments added, `UnitOfWork.Commits.ShouldBe(1)`; 201 at `RegisterInstallmentPlanTest.cs:38` | ✅ PASS |
| AC2 exactly N expenses numbered 1..N, each referencing the plan | `[1,2,3,4,5]` + plan id | `RegisterInstallmentPlanUseCaseTest.cs:26-33` — counts, `ShouldBe([1,2,3,4,5])`, `ShouldAllBe(... InstallmentPlanId == result.Id)` | ✅ PASS |
| AC3 sum of installments = total exactly | equality, no lost cent | `RegisterInstallmentPlanUseCaseTest.cs:68` — `Sum(...).ShouldBe(total)` over 7 awkward totals incl. `0.05/3`, `99.99/7`, `0.02/12`; endpoint `RegisterInstallmentPlanTest.cs:56` | ✅ PASS |
| AC4 installment 1 by the credit rule, then +1 month each | `2026-08-21` @ closing 20 → Sep, Oct, Nov | `RegisterInstallmentPlanUseCaseTest.cs:101-102`; year boundary `:115-121` (Nov, Dec, Jan-27, Feb-27); endpoint `RegisterInstallmentPlanTest.cs:62` | ✅ PASS |
| AC5 every generated expense `Credit`, dated the start date | both, on all N | `RegisterInstallmentPlanUseCaseTest.cs:136-137` — `ShouldAllBe(Type == Credit)`, `ShouldAllBe(Date == 2026-08-21)` | ✅ PASS |
| AC6 plan end date = competence month of the last installment | `2027-02-01` | `RegisterInstallmentPlanUseCaseTest.cs:154-156` — literal, tied to `Installments[^1].CompetenceMonth`, and on the persisted plan; endpoint `RegisterInstallmentPlanTest.cs:46` `"2026-11-01"` | ✅ PASS |
| AC7 count < 2 → 400 `INSTALLMENT_COUNT_INVALID` | that key | `RegisterInstallmentPlanUseCaseTest.cs:187` `[1][0][-1]`; endpoint both cultures `RegisterInstallmentPlanTest.cs:76-93` | ✅ PASS |
| AC8 total ≤ 0 → 400 `AMOUNT_GREATER_THAN_ZERO` | that key | `RegisterInstallmentPlanUseCaseTest.cs:203`; endpoint `RegisterInstallmentPlanTest.cs:98-115` | ✅ PASS |
| AC9 foreign person/category/account → 404 | 404 | `RegisterInstallmentPlanUseCaseTest.cs:217, 231, 245`; endpoint `RegisterInstallmentPlanTest.cs:129` | ✅ PASS |
| Independent Test: 100.00 over 3 → 33.33 / 33.33 / 33.34 | exact triple | `RegisterInstallmentPlanUseCaseTest.cs:46-47` `ShouldBe([33.33m, 33.33m, 33.34m])`; endpoint `RegisterInstallmentPlanTest.cs:53` | ✅ PASS |

### P1: Register a recurring expense (RECR-01, RECR-02)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 expense + first version in one transaction, 201 | one `Commit()` | `RegisterRecurringExpenseUseCaseTest.cs:84` — `Commits.ShouldBe(1)`; fields `:26-45`; 201 at `RecurringExpenseEndpointsTest.cs:40` | ✅ PASS |
| AC2 first version's validity end is null | null | `RegisterRecurringExpenseUseCaseTest.cs:55-56`; endpoint `RecurringExpenseEndpointsTest.cs:57` — `validityEnd.ValueKind.ShouldBe(JsonValueKind.Null)` on exactly one version | ✅ PASS |
| AC3 `Archived` false, supplied `IsEstimate` stored | false + both true/false | `RegisterRecurringExpenseUseCaseTest.cs:70-74` — `[InlineData(true)][InlineData(false)]`, `Archived.ShouldBeFalse()`, `IsEstimate.ShouldBe(isEstimate)` | ✅ PASS |
| AC4 foreign person/category/account → 404 | 404 | `RegisterRecurringExpenseUseCaseTest.cs:98, 112, 126`; endpoint `RecurringExpenseEndpointsTest.cs:158` | ✅ PASS |
| AC5 empty name → 400 `NAME_REQUIRED` | that key | `RegisterRecurringExpenseUseCaseTest.cs:140`; endpoint both cultures `RecurringExpenseEndpointsTest.cs:163` | ✅ PASS |
| AC6 base amount ≤ 0 → 400 `AMOUNT_GREATER_THAN_ZERO` | that key | `RegisterRecurringExpenseUseCaseTest.cs:156`; endpoint `RecurringExpenseEndpointsTest.cs:178` | ✅ PASS |
| AC7 due day outside 1..31 → 400 `DAY_OUT_OF_RANGE` | that key, both bounds | `RegisterRecurringExpenseUseCaseTest.cs:172` `[0][32]`; bounds 1/31 accepted `RegisterRecurringExpenseValidatorTest.cs:68`; endpoint `RecurringExpenseEndpointsTest.cs:193` | ✅ PASS |

### P1: Record what a recurring expense actually cost (RPAY-01..03)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 persist month, date, amount, notes, paying account, 201 | all five | `RegisterRecurringExpensePaymentUseCaseTest.cs:31-44` — five on the persisted entity **and** five on the response; endpoint `RecurringExpensePaymentTest.cs:44-51` incl. 201 | ✅ PASS |
| AC2 store the id of the version in effect at the reference month | the *closed* version's id for a past month | `RegisterRecurringExpensePaymentUseCaseTest.cs:83-85` — `ShouldBe(oldVersion.Id)` **and** `ShouldNotBe(newVersion.Id)`; endpoint `RecurringExpensePaymentTest.cs:82-83` | ✅ PASS |
| AC3 update overwrites amount, date, notes, paying account, 200 | all four + 200 | `UpdateRecurringExpensePaymentUseCaseTest.cs:28-37`; endpoint `RecurringExpensePaymentTest.cs:128-136` | ✅ PASS |
| AC4 update leaves reference month and version id unchanged | unchanged even when the month now resolves to a newer version | `UpdateRecurringExpensePaymentUseCaseTest.cs:87-89` — `ShouldBe(oldVersion.Id)`, `ShouldNotBe(newVersion.Id)` after the fixture reopens a version at `2026-08-01`; endpoint `RecurringExpensePaymentTest.cs:138-141` | ✅ PASS |
| AC5 duplicate month → 400 `PAYMENT_ALREADY_RECORDED` | that key, nothing written | `RegisterRecurringExpensePaymentUseCaseTest.cs:141-144` — key asserted, `Added.ShouldBeNull()`, `Commits.ShouldBe(0)`; endpoint `RecurringExpensePaymentTest.cs:146-160` | ✅ PASS |
| AC6 foreign recurring expense or payment → 404 | 404, each naming its entity | `RegisterRecurringExpensePaymentUseCaseTest.cs:219` (`RECURRING_EXPENSE_NOT_FOUND`), `UpdateRecurringExpensePaymentUseCaseTest.cs:149` (`RECURRING_EXPENSE_PAYMENT_NOT_FOUND`); endpoint `RecurringExpensePaymentTest.cs:236, 251` | ✅ PASS |
| AC7 archived expense → 400 `RECURRING_EXPENSE_ARCHIVED` | that key | `RegisterRecurringExpensePaymentUseCaseTest.cs:156`; endpoint `RecurringExpensePaymentTest.cs:164-180` (archived through the real archive route) | ✅ PASS |
| AC8 amount ≤ 0 → 400 `AMOUNT_GREATER_THAN_ZERO` | that key, both paths | `RegisterRecurringExpensePaymentUseCaseTest.cs:191`, `UpdateRecurringExpensePaymentUseCaseTest.cs:122`; endpoint `RecurringExpensePaymentTest.cs:197, 212` | ✅ PASS |
| AC9 no version in effect → 400 `NO_VERSION_IN_EFFECT` | that key | `RegisterRecurringExpensePaymentUseCaseTest.cs:171`; endpoint `RecurringExpensePaymentTest.cs:182` | ✅ PASS |
| AC10 accept null/empty notes and null paying account | both null | `RegisterRecurringExpensePaymentUseCaseTest.cs:110-114`; `UpdateRecurringExpensePaymentUseCaseTest.cs:103-107`; endpoint `RecurringExpensePaymentTest.cs:102-103` | ✅ PASS |
| Independent Test: 180 → correct to 172.40, Aug reports 172.40, Sep still 150 | both months | Correction: `UpdateRecurringExpensePaymentUseCaseTest.cs:28`, endpoint `RecurringExpensePaymentTest.cs:133`. **Note:** the "September still reports 150.00" half is proved by the version-resolution tests (`GetMonthlyExpenseUseCaseTest.cs:93-114`) rather than by one end-to-end two-month fixture. | ✅ PASS |

### P1: View the expenses of a given month (VIEW-01..04)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 one variable line per matching expense | one line, right amount | `GetMonthlyExpenseTest.cs:50` — `variable.ShouldHaveSingleItem()...amount.ShouldBe(80.00m)`; isolation `:168-181` | ✅ PASS |
| AC2 one recurring line per non-archived recurring expense | 2 lines; archived omitted | `GetMonthlyExpenseTest.cs:51` `recurring.Count.ShouldBe(2)`; archived omission `:108` `ShouldBeEmpty()` | ✅ PASS |
| AC3a version in effect → report that version's amount as expected | 150.00 from the closed version, not 180.00 from the open one | `GetMonthlyExpenseUseCaseTest.cs:114` — `ExpectedAmount.ShouldBe(150m)`; endpoint `GetMonthlyExpenseTest.cs:54` | ✅ PASS |
| **AC3b … and the recurring expense's due day** | the stored `DueDay` on the line | **no `file:line` — `DueDay` is never asserted on a monthly recurring line at any layer** (`grep -rn "dueDay\|DueDay"` over both monthly-view test files returns nothing). Mutation **M11** (`GetMonthlyExpenseUseCase.cs:90` → `DueDay = 0`) **SURVIVED both suites**. | ❌ **GAP** |
| AC4 no version in effect → null expected | null | `GetMonthlyExpenseUseCaseTest.cs:88` — `ExpectedAmount.ShouldBeNull()` | ✅ PASS |
| AC5 payment exists → actual = amount paid; `Paid` when actual == expected | 150/150 → `Paid` | `GetMonthlyExpenseUseCaseTest.cs:32-34`; endpoint `GetMonthlyExpenseTest.cs:83-84` `status == (int)ExpenseStatus.Paid` | ✅ PASS |
| AC6 payment differs from a non-null expected → `Divergent` | 180 vs 150 → `Divergent` | `GetMonthlyExpenseUseCaseTest.cs:51-53`; endpoint `GetMonthlyExpenseTest.cs:56` | ✅ PASS |
| AC7 no payment → null actual, `Pending` | both | `GetMonthlyExpenseUseCaseTest.cs:70-72`; endpoint `GetMonthlyExpenseTest.cs:60-61` | ✅ PASS |
| AC8 report the `IsEstimate` flag | true and false | `GetMonthlyExpenseUseCaseTest.cs:129-130`; endpoint `GetMonthlyExpenseTest.cs:160-164` | ✅ PASS |
| AC9 four totals, committed = variable + (actual ?? expected) per line | 80 + 180 + 45 = 305 | `GetMonthlyExpenseUseCaseTest.cs:196-201` — all four pinned; endpoint `GetMonthlyExpenseTest.cs:63-68` | ✅ PASS |
| AC10 installment line reports number and plan count | 3 of 10 / 1 of 3 | `GetMonthlyExpenseUseCaseTest.cs:158-160`; endpoint `GetMonthlyExpenseTest.cs:142-143`; negative case (one-off carries no markers) `:164-177` | ✅ PASS |
| AC11 invalid month → 400 `REFERENCE_MONTH_INVALID` | that key | `GetMonthlyExpenseUseCaseTest.cs:234-235` `[0][13]`; endpoint both cultures `GetMonthlyExpenseTest.cs:200-213` | ✅ PASS |
| AC12 nothing recorded → 200, empty collections, zeroed totals | 200 + 4 zeros | `GetMonthlyExpenseUseCaseTest.cs:213-219`; endpoint `GetMonthlyExpenseTest.cs:184-196` (200 asserted at `:230`) | ✅ PASS |
| (design rule) payment present + expected null → `Paid` | **spec does not define this outcome** | no test; mutation **M9** (`GetMonthlyExpenseUseCase.cs:118` → `Divergent`) **SURVIVED both suites** | ⚠️ **Spec-precision gap + surviving mutant** |

### P2: Change the base value of a recurring expense (RECR-03, RECR-04)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 close the version in effect at the day before the new start | new start `2026-09-01` → end `2026-08-31` | `ChangeRecurringExpenseValueUseCaseTest.cs:23` — `ValidityEnd.ShouldBe(new DateOnly(2026, 8, 31))`; gap/overlap-free at `:63`; endpoint `RecurringExpenseEndpointsTest.cs:81` `"2026-08-31"` | ✅ PASS |
| AC2 create a new version with amount, start, null end, reason | all four | `ChangeRecurringExpenseValueUseCaseTest.cs:37-46`; endpoint `RecurringExpenseEndpointsTest.cs:83-86` | ✅ PASS |
| AC3 closing + creation in one transaction | one `Commit()` | `ChangeRecurringExpenseValueUseCaseTest.cs:73` — `Commits.ShouldBe(1)` | ✅ PASS |
| AC4 previously recorded payments keep their version id | id, month and amount all unchanged | `ChangeRecurringExpenseValueUseCaseTest.cs:97-99` | ✅ PASS |
| AC5 empty reason → 400 `CHANGE_REASON_REQUIRED` | that key | `ChangeRecurringExpenseValueUseCaseTest.cs:113`; endpoint both cultures `RecurringExpenseEndpointsTest.cs:208` | ✅ PASS |
| AC6 new start not later than the current → 400 `VALIDITY_START_MUST_BE_LATER` | that key, equal **and** earlier | `ChangeRecurringExpenseValueUseCaseTest.cs:125` (equal) and `:137` (earlier); rejected change leaves the version open and commits nothing `:150-152`; endpoint `RecurringExpenseEndpointsTest.cs:224` | ✅ PASS |
| AC7 foreign recurring expense → 404 | 404, nothing mutated | `ChangeRecurringExpenseValueUseCaseTest.cs:176-179`; endpoint `RecurringExpenseEndpointsTest.cs:130` | ✅ PASS |

### P2: Archive a recurring expense (RECR-05)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 archive → flag true, 204 | true + 204 | `ArchiveRecurringExpenseUseCaseTest.cs:24-25`; endpoint `RecurringExpenseEndpointsTest.cs:98` `NoContent` | ✅ PASS |
| AC2 unarchive → flag false, 204 | false + 204 | `ArchiveRecurringExpenseUseCaseTest.cs:38-39`; endpoint `RecurringExpenseEndpointsTest.cs:114` | ✅ PASS |
| AC3 archived → omitted from the month, payments kept in the database | line and totals gone; unarchive restores 150.00 | `GetMonthlyExpenseTest.cs:108-110` then `:120` `actualAmount.ShouldBe(150.00m)` after unarchiving — proved at the only layer where the repository's `Archived == false` filter runs | ✅ PASS |
| AC4 foreign recurring expense → 404 | 404, flag untouched | `ArchiveRecurringExpenseUseCaseTest.cs:82-85` — key, `Archived.ShouldBeFalse()`, `Commits.ShouldBe(0)`; endpoint `RecurringExpenseEndpointsTest.cs:144` | ✅ PASS |

### P2: See income and expenses for the same month (DASH-01, DASH-02)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 return the existing income view and the expense view for the same month | both halves byte-identical to the individual endpoints | `GetMonthlyDashboardTest.cs:52-53` — `GetRawText().ShouldBe(...)` for both; unit `GetMonthlyDashboardUseCaseTest.cs:26-27` `ShouldBeSameAs` | ✅ PASS |
| AC2 balance = total income received − total committed expense | 5000 − 305 = 4695 | `GetMonthlyDashboardTest.cs:75` `ShouldBe(4695.00m)`; negative case `:89` `ShouldBe(-300.00m)`; unit discriminators `GetMonthlyDashboardUseCaseTest.cs:40, 53, 67` (expected-vs-received and committed-vs-paid each pinned separately) | ✅ PASS |
| AC3 no bearer token → 401 | 401 | `GetMonthlyDashboardTest.cs:144` — `ShouldBe(HttpStatusCode.Unauthorized)` | ✅ PASS |
| AC4 income half produced by invoking the existing use case, income unmodified | invoked once, response object passed through unchanged | `GetMonthlyDashboardUseCaseTest.cs:83` `Verify(Execute(2026, 8), Times.Once)`, `:26` `ShouldBeSameAs(income)`; plus the AD-006 diff evidence above | ✅ PASS |

**Status**: ❌ Gaps present — 70/72 ✅ PASS, 1 ❌ GAP (VIEW AC3b), 1 ⚠️ Spec-precision gap.

---

## Payload / conjunction rule

Checked on every payload-bearing criterion: assertions target field **values**, not "a call happened".

| Criterion | Check | Result |
| --------- | ----- | ------ |
| EXPN AC1 | Seven persisted fields **and** seven response fields asserted by value (`RegisterExpenseUseCaseTest.cs:26-41`), not `Verify(Add(It.IsAny<Expense>()))` | ✅ |
| RPAY AC1 | Five persisted + five response fields by value (`RegisterRecurringExpensePaymentUseCaseTest.cs:31-44`) | ✅ |
| INST AC1 | `Commit()` counted at exactly 1 via `UnitOfWorkBuilder.Commits`, not `Verify(..., Times.Once)` on a bare mock | ✅ |
| DASH AC1 | Reference identity (`ShouldBeSameAs`) at the unit layer and raw JSON equality end to end — the strongest available form | ✅ |
| RECR-03 AC1 | The closed version's `ValidityEnd` asserted as the literal `2026-08-31`, not "is not null" | ✅ |
| Rejection paths | Every 400/404 test also asserts nothing was added and `Commits == 0` — conjunction, not just the exception | ✅ |
| **VIEW AC3b** | `DueDay` is populated but **never asserted** — the one place the rule is violated | ❌ |

---

## Discrimination Sensor

**Isolation**: a temporary `git worktree` at `…/scratchpad/sensor` on detached `HEAD` (`b6a1cac`).
Mutations applied to that copy only, tests run there, file restored after each run, then
`git worktree remove --force` + `git worktree prune`. **No `git stash` was used.**

- Pre-sensor baseline: `git status --porcelain` on the real tree → **empty** (0 bytes captured to disk).
- Post-sensor: `git status --porcelain` → **empty**; `git worktree list` shows only the real tree.
- **Isolation verified** — the real working tree matches the baseline exactly.

**Depth**: P0-full (data integrity + money arithmetic). **11 behaviour-level mutations.**

| # | File:line | Mutation | Killed? |
| - | --------- | -------- | ------- |
| M1 | `Balance.Domain/Extensions/CompetenceMonthResolver.cs:17` | `date.Day > closingDay` → `>=` (a purchase *on* the closing day would roll) | ✅ Killed (2 failed) |
| M2 | `…/RegisterInstallmentPlanUseCase.cs:133` | `number == count ? residual : each` → `each` (last installment rounded, sum stops being exact) | ✅ Killed (11 failed) |
| M3 | `Balance.Domain/Extensions/RecurringExpenseExtensions.cs:27` | `OrderByDescending` → `OrderBy` | ✅ Killed (1 failed — the overlapping-versions fixture) |
| M4 | `…/GetMonthlyExpenseUseCase.cs:123-125` | `Paid`/`Divergent` branches swapped | ✅ Killed (2 failed) |
| M5 | `…/GetMonthlyExpenseUseCase.cs:105` | `ActualAmount ?? ExpectedAmount` → `ExpectedAmount ?? ActualAmount` (estimate beats the real bill) | ✅ Killed (1 failed) |
| M6 | `…/GetMonthlyDashboardUseCase.cs:35` | balance from `TotalRecurringPaid` instead of `TotalCommitted` | ✅ Killed (3 failed) |
| M7 | `…/GetMonthlyDashboardUseCase.cs:35` | balance from `TotalExpected` instead of `TotalReceived` | ✅ Killed (3 failed) |
| M8 | `…/UpdateRecurringExpensePaymentUseCase.cs:47` | update recomputes the frozen version id (`= Guid.NewGuid()`) | ✅ Killed (2 failed) |
| M9 | `…/GetMonthlyExpenseUseCase.cs:118` | payment with no version in effect → `Divergent` instead of `Paid` | ❌ **SURVIVED** (UseCases.Test **and** WebApi.Test) |
| M10 | `…/GetMonthlyExpenseUseCase.cs:52` | `TotalRecurringPaid` sums `ExpectedAmount` instead of `ActualAmount` | ✅ Killed (both suites) |
| M11 | `…/GetMonthlyExpenseUseCase.cs:90` | `DueDay = expense.DueDay` → `DueDay = 0` | ❌ **SURVIVED** (UseCases.Test **and** WebApi.Test) |

**Result**: **9/11 killed** — ❌ FAIL.

All eight mutations on the logic identified as highest-risk (competence month, installment residual,
version ordering, status resolution, committed total, balance composition, version freeze) were killed,
several by a single targeted test — the suite discriminates well on the arithmetic. The two survivors
are both on `GetMonthlyExpenseUseCase`'s recurring-line construction:

- **M11 is a real, reachable defect in coverage.** The monthly view could report every due day as `0`
  and all 342 tests stay green. The frontend page built in T49 renders due days, so this is shipped,
  user-visible behaviour with zero assertions behind it.
- **M9 is lower severity.** The branch it mutates appears **unreachable through the public API**:
  `RegisterRecurringExpensePaymentUseCase` rejects a month with no version in effect
  (`NO_VERSION_IN_EFFECT`), and `ChangeRecurringExpenseValueUseCase` always leaves the newest version
  open with no gap in the timeline — so a recorded payment can never later resolve to a null expected
  amount. It is defensive code whose outcome the spec never defines.

---

## Code Quality

| Principle | Status | Note |
| --------- | ------ | ---- |
| Minimum code | ✅ | No abstraction added for single-use code; `CompetenceMonthResolver` and `VersionInEffect` are pure static functions with two and one caller respectively |
| Surgical changes | ✅ | 12 pre-existing files modified, each for a stated reason; no income file touched |
| No scope creep | ✅ | Everything in the diff maps to a spec goal or an approved addition in the Assumptions table; nothing from the Out of Scope table appears |
| Matches patterns | ✅ | Repository read/write split, `AbstractValidator<TRequest>`, `ProducesResponseType`, `(CommunicationX)domainX` casts, Bogus/Moq/Shouldly builders — all mirror the income slice |
| Didn't "improve" unrelated code | ✅ | `UnitOfWorkBuilder` change is purely additive; the existing `static Build()` is byte-identical |
| Spec-anchored outcome check | ⚠️ | 70/72 assertions match the spec-defined outcome; VIEW AC3b has no assertion, and the status rule's null-expected branch has no spec-defined outcome to match |
| Per-layer Coverage Expectation | ⚠️ | Domain rules 1:1 with ACs; every route in scope has happy + edge + error + 401 paths. The one hole is a *field* on an existing route's payload, not a missing path |
| Every test maps to a spec requirement | ✅ | No unclaimed tests found; the extras beyond the ACs (`A_One_Off_Expense_Carries_No_Installment_Markers`, `Success_Day_Boundaries`, `A_Rejected_Change_Leaves_The_Version_In_Effect_Open`) are negative/boundary companions to listed criteria |
| Documented guidelines followed | ✅ | `.claude/skills/tlc-spec-driven/references/coding-principles.md`; the repo's `dotnet-arch-guard` conventions (layer boundaries, DI registration, `ProducesResponseType`, user-scoped queries) hold across all five new controllers |
| Would a senior engineer approve? | ⚠️ | Yes, with the due-day assertion added |

---

## Edge Cases

| Edge case from spec.md | Evidence | Result |
| ---------------------- | -------- | ------ |
| Month before every version → null expected | `GetMonthlyExpenseUseCaseTest.cs:88`; `RecurringExpenseExtensionsTest.cs:16` | ✅ |
| Month inside a closed version's range → that closed version's amount | `RecurringExpenseExtensionsTest.cs:26` (150 not 180); `GetMonthlyExpenseUseCaseTest.cs:114` | ✅ |
| Credit dated exactly on the closing day → month of the date | `CompetenceMonthResolverTest.cs:20-22`; endpoint `RegisterExpenseTest.cs:62` | ✅ |
| Total not dividing evenly → whole remainder on the last installment | `RegisterInstallmentPlanUseCaseTest.cs:86-87` — first N−1 all equal `each`, last equals `total − each×(N−1)` | ✅ |
| Due day 31 in a shorter month → reported as stored, not clamped | **no test.** The validator accepts 31 (`RegisterRecurringExpenseValidatorTest.cs:68`) but nothing asserts what the monthly view *reports* — same root cause as VIEW AC3b | ❌ **NOT covered** |
| Two categories or two accounts of the same user with the same name → both accepted | **no test** at any layer | ❌ **NOT covered** |
| Archived after a payment → payment retrievable, line omitted | `GetMonthlyExpenseTest.cs:93-121` — archived, totals zeroed, unarchived, 150.00 restored | ✅ |

5/7 covered.

---

## Gate Check

- **Build**: `dotnet build Balance.sln --nologo` → **0 errors, 0 warnings** ✅
- **Test**: `dotnet test Balance.sln --nologo` → **342 passed, 0 failed, 0 skipped** ✅
  - Validators.Tests 59 · UseCases.Test 170 · WebApi.Test 113
- **Test count before feature**: 82 · **after**: 342 · **delta**: +260 · **skipped**: none
- **Failures**: none
- **Migration**: `20260812233254_AddExpenseTracking` — additive, `InitialCreate` untouched

> **Environment note (does not affect the verdict).** The dev API on `http://localhost:5126` holds file
> locks on `src/Balance.Api/bin/Debug/net10.0/*.dll`, so an in-place build fails with `MSB3021`/`MSB3027`
> — a copy-lock, never a compile error. Both gates were therefore run with
> `--artifacts-path <scratch>`, which redirects all output away from the locked directory and leaves the
> running API alone. The 0-warning / 342-pass figures above are from those runs.

---

## Fix Plans

### Fix 1 — Assert the due day on the monthly recurring line (Major)

- **Root cause**: `GetMonthlyExpenseUseCase.cs:90` sets `DueDay = expense.DueDay`, but no test at any
  layer reads that field. Mutation M11 (`DueDay = 0`) survives all 342 tests.
- **Spec clause**: VIEW AC3 — "the system SHALL report that version's amount as the expected amount
  **and the recurring expense's due day**".
- **Fix task**: In `tests/UseCases.Test/Expenses/GetMonthly/GetMonthlyExpenseUseCaseTest.cs`, assert
  `line.DueDay.ShouldBe(<the value the fixture registered>)` on a recurring line, using a due day that
  is **not** `0` and not the builder default shared by every fixture. Add the same assertion end to end
  in `tests/WebApi.Test/Expenses/GetMonthlyExpenseTest.cs`, and cover the listed edge case with a bill
  whose due day is **31** read back in **February**, asserting `dueDay == 31` (no clamping).
- **Verify**: re-run mutation M11 (`DueDay = 0`) in a scratch worktree; it must now fail.
- **Done when**: M11 is killed and the due-day-31 edge case has a `file:line` citation.

### Fix 2 — Pin the status of a payment with no version in effect (Minor)

- **Root cause**: `GetMonthlyExpenseUseCase.cs:116-119` returns `Paid` when a payment exists but no
  version is in effect. `design.md` states this rule; `spec.md` does not. Mutation M9 survives.
- **Fix task**: Either (a) add the rule to `spec.md` as a VIEW criterion and add a unit test asserting
  `Status == Paid` with `ExpectedAmount == null` and a non-null `ActualAmount`; or (b) if the branch is
  agreed to be unreachable — as the analysis above indicates — replace it with an explicit
  unreachable-state comment so no test is owed for it.
- **Priority**: Minor — unreachable through the public API on current evidence.

### Fix 3 — Cover the duplicate-name edge case (Minor)

- **Root cause**: The spec's edge case "two categories or two accounts of the same user carrying the
  same name are both accepted" has no test. Nothing enforces or forbids it today; a future unique index
  would break it silently.
- **Fix task**: In `tests/WebApi.Test/Categories/CategoryEndpointsTest.cs`, register two categories with
  an identical name for one user and assert both are `201` and both appear in the listing; repeat for
  accounts.

---

## Requirement Traceability Update

| Requirement | Previous Status | New Status |
| ----------- | --------------- | ---------- |
| SHAR-01 | Implementing | ✅ Verified |
| SHAR-02 | Implementing | ✅ Verified |
| SHAR-03 | Implementing | ✅ Verified |
| EXPN-01 | Implementing | ✅ Verified |
| EXPN-02 | Implementing | ✅ Verified |
| EXPN-03 | Implementing | ✅ Verified |
| INST-01 | Implementing | ✅ Verified |
| INST-02 | Implementing | ✅ Verified |
| INST-03 | Implementing | ✅ Verified |
| RECR-01 | Implementing | ✅ Verified |
| RECR-02 | Implementing | ✅ Verified |
| RECR-03 | Implementing | ✅ Verified |
| RECR-04 | Implementing | ✅ Verified |
| RECR-05 | Implementing | ✅ Verified |
| RPAY-01 | Implementing | ✅ Verified |
| RPAY-02 | Implementing | ✅ Verified |
| RPAY-03 | Implementing | ✅ Verified |
| VIEW-01 | Implementing | ✅ Verified |
| **VIEW-02** | Implementing | ❌ **Needs Fix** — the due-day half of VIEW AC3 has no evidence (Fix 1) |
| VIEW-03 | Implementing | ⚠️ Verified with a spec-precision gap (Fix 2) |
| VIEW-04 | Implementing | ✅ Verified |
| DASH-01 | Implementing | ✅ Verified |
| DASH-02 | Implementing | ✅ Verified |

21 verified · 1 needs fix · 1 verified with a flagged gap.

---

## Summary

**Overall**: ⚠️ Issues — not ready to close, but close to it.

**Spec-anchored check**: 70/72 ACs matched the spec-defined outcome · 1 AC clause with zero evidence · 1 spec-precision gap
**Sensor**: 9/11 mutations killed (8/8 on the mandated high-risk logic; both survivors on the same method)
**Gate**: 342 passed, 0 failed, 0 skipped · build 0 errors, 0 warnings
**Constraints**: AD-006 upheld by diff evidence · 82 pre-existing tests green and unedited

**What works**: The money arithmetic is genuinely well tested. The installment residual is proved
against seven awkward totals rather than one happy path; the competence-month boundary is asserted at
the resolver, the use case and the endpoint; the version freeze is asserted against a *past* month
after a value change, so an implementation freezing the currently-open version fails on its own. The
dashboard compares raw serialised JSON against the income endpoint, which is the strongest available
proof of AD-006. Every rejection path asserts that nothing was written and nothing committed. Eight of
eight mutations on the highest-risk logic died.

**Issues found**:
1. VIEW AC3's due-day clause has no assertion anywhere; `DueDay = 0` survives all 342 tests (Fix 1).
2. The status rule's null-expected branch is neither specified nor tested (Fix 2).
3. Two spec edge cases — due day 31 unclamped, duplicate names accepted — have no evidence (Fix 1, Fix 3).

**Next steps**: Apply Fix 1 (blocking), then re-run mutation M11 and re-verify. Fixes 2 and 3 are
Minor and may be batched or deferred with a recorded decision.
