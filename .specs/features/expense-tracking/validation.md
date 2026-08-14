# Expense Tracking Validation

**Date**: 2026-08-12
**Spec**: `.specs/features/expense-tracking/spec.md`
**Diff range**: `main..HEAD` (`feature/expense-tracking`, 54 commits, HEAD `b42f6f3`)
**Verifier**: independent sub-agent (author ≠ verifier) — coverage re-derived from `spec.md`, evidence-or-zero
**Iteration**: 2 of a maximum 3

## Validation: expense-tracking - PASS ✅

**Verdict**: ✅ **PASS** — 348/348 tests green, 72/72 acceptance criteria traced to a `file:line` + an
assertion that matches the spec-defined outcome, and 11 of 12 behaviour-level mutations killed. The
single survivor is a behaviour the spec itself declares untested by design; it is flagged below rather
than passed silently.

---

## What changed since iteration 1

Iteration 1 returned FAIL on `b6a1cac`: 11 mutations, 9 killed, 2 survived, one acceptance-criterion
clause with zero evidence. Commit `b42f6f3` is the sole fix commit. Each of its three claims was
re-checked from scratch — the fix author's assertion that both mutants now die was **not** taken on
trust; both were re-injected in a fresh scratch worktree.

| Iteration-1 gap | Severity | Claimed fix | Independently verified? |
| --------------- | -------- | ----------- | ----------------------- |
| VIEW AC3's due-day clause had no assertion at any layer; `DueDay = 0` survived both suites | Major | Assertions at the use case and endpoint layers, plus the due-day-31 edge case | ✅ **Closed.** M1 (`DueDay = 0`) now kills 2 use-case tests + 1 endpoint test. See the sensor table for the named killers. |
| Status branch for a payment with no version in effect: unspecified and untested; `Paid → Divergent` survived | Minor | Pinned as a row in `spec.md`'s Assumptions table **and** covered by a test | ✅ **Closed.** M2 now kills `A_Payment_With_No_Version_In_Effect_Is_Paid_With_A_Null_Expected`. Adequacy of the spec placement judged below. |
| Same-name categories/accounts edge case had no test | Minor | Two endpoint tests | ✅ **Closed.** M11 (listings collapse same-named rows) kills exactly those two tests. |

**Is the Assumptions-table placement adequate for the status branch?** Yes. The Assumptions table is
`spec.md`'s own documented mechanism for resolving an ambiguity, the new row states a *precise*
outcome (`Paid`, with a null expected amount) rather than a hand-wave, it carries the rationale for
rejecting `Divergent`, and a test now pins it. That converts a ⚠️ spec-precision gap into a specified,
asserted outcome. Making it a VIEW acceptance criterion would have been slightly stronger — an AC
would appear in the traceability table and inherit a requirement ID — but the branch is unreachable
through the public API (`RegisterRecurringExpensePaymentUseCase` rejects such a month with
`NO_VERSION_IN_EFFECT`), so promoting defensive code to a user-facing criterion would misdescribe it.
The chosen placement is the better of the two. **Cosmetic note only:** the row is not reflected in the
Requirement coverage map, so the behaviour is discoverable from the Assumptions table but not from
VIEW-03's row.

**One thing the fix did not address, and did not claim to:** the installment midpoint rounding mode.
Iteration 1 recorded it as an accepted spec-precision decision but never probed it. This run did — see
M12.

---

## Task Completion

All 49 tasks in `tasks.md` are marked ✅ Complete; no task is blocked or partial. The five recorded
deviations were re-read this iteration and each remains justified — the judgements from iteration 1
were re-derived, not copied.

| Task | Status | Deviation | Verdict |
| ---- | ------ | --------- | ------- |
| T12, T27 | ✅ Done | Culture coverage for validation messages deferred from the use-case layer to the endpoint layer | **Justified.** Matches the pre-existing Person/Income convention; both cultures assert at the endpoint layer (`RecurringExpenseEndpointsTest.cs:163,178,193,208,224`; `RegisterExpenseTest.cs:128,149`). |
| T18, T28, T35 | ✅ Done | `SPEC_DEVIATION`: per-entity `*_NOT_FOUND` keys added beyond the design's error table | **Justified.** The design named only `PERSON_NOT_FOUND`, which would answer a foreign *category* with "Person not found". The 404 status AD-004 pins is unchanged; only the body names the right entity. Each key is asserted: `RegisterExpenseUseCaseTest.cs:186,200`, `RegisterRecurringExpensePaymentUseCaseTest.cs:219`, `UpdateRecurringExpensePaymentUseCaseTest.cs:149`. |
| T26 | ✅ Done | `ResponseRecurringExpenseJson` carries the version **history**, not just the current version | **Justified.** Without it RECR-03 AC1 is unobservable end to end; it is what lets `RecurringExpenseEndpointsTest.cs:81` assert the closed version at its exact end date. |
| T39 | ✅ Done | Catalogue name/priority fields removed from the **recurring** line; "archived omitted" test moved to T41 | **Justified.** `RecurringExpenseRepository.GetForMonth` does not load catalogue navigations, so those fields would ship always-blank. The archived-omission test belongs at the endpoint layer because the `Archived == false` filter lives in the repository — covered at `GetMonthlyExpenseTest.cs:93-121`. |
| T47 | ✅ Done | `SPEC_DEVIATION`: docker-compose host port 5432 → 5434 | **Justified.** Environment-local collision; one reversible line, container port unchanged. |

---

## Constraint checks

### AD-006 — income code is read-only

Verified by diff evidence this iteration, not carried over.

| Check | Evidence | Result |
| ----- | -------- | ------ |
| No income-named file in the diff | `git diff --name-only main..HEAD \| grep -Ei 'income'` → **no matches** (exit 1) | ✅ |
| Nothing deleted or renamed | `git diff --name-status --diff-filter=DR main..HEAD` → **empty** | ✅ |
| Dashboard composes rather than reimplements | `GetMonthlyDashboardUseCase.cs:14,28` injects and invokes `IGetMonthlyIncomeUseCase`; no income entity or repository is referenced anywhere in the class | ✅ |
| Composition asserted, not assumed | `GetMonthlyDashboardUseCaseTest.cs:26` — `result.Income.ShouldBeSameAs(income)` (reference identity, so a reshaped or refiltered income half fails); `:83` — `incomeUseCase.Verify(u => u.Execute(2026, 8), Times.Once)` | ✅ |
| Income half byte-identical end to end | `GetMonthlyDashboardTest.cs:52` — `dashboard.GetProperty("income").GetRawText().ShouldBe(income.GetRawText())` against `GET /api/income/2026/8` | ✅ |
| The composition is *load-bearing*, not decorative | Sensor **M8**: making the dashboard ask the income use case for January instead of the requested month kills 1 use-case + 3 endpoint tests | ✅ |

### "The 82 pre-existing tests stay green **and unedited**"

Checked against the diff, not only against a green suite.

| Check | Evidence | Result |
| ----- | -------- | ------ |
| Pre-existing files modified (M/D/R) | 14 files; **not one is a test file** | ✅ |
| Pre-existing `*Test.cs` edited | none — every `*Test.cs` in the diff is an addition (`--diff-filter=M` list contains no test) | ✅ |
| Test count | 82 → 348 (**+266**), 0 removed, 0 skipped | ✅ |

Modified pre-existing files: `.specs/LESSONS.md`, `.specs/STATE.md`, `.specs/lessons.json`,
`docker-compose.yml`, `Program.cs`, `appsettings.Development.json`, both
`DependencyInjectionExtension.cs`, the `ResourceErrorMessages` trio, `BalanceDbContext.cs`,
`BalanceDbContextModelSnapshot.cs`, and `tests/CommonTestUtilities/Repositories/UnitOfWorkBuilder.cs`.

`UnitOfWorkBuilder.cs` is the only file under `tests/` that was edited. It is a shared *utility*, not a
test. Re-read this iteration: the change is **purely additive** — the existing `static Build()` used by
the 82 income tests is byte-identical; a new instance-based `BuildCounting()` was added alongside it
for the commit-count assertions.

---

## Spec-Anchored Acceptance Criteria

72 acceptance criteria across 9 stories. VIEW AC3 is a conjunction and is scored as two clauses, giving
73 clause-level rows. Every row is traced to a `file:line` and the assertion expression, and each
asserted value was checked against the outcome `spec.md` pins — not merely that an assertion exists.

Line numbers in `GetMonthlyExpenseUseCaseTest.cs`, `GetMonthlyExpenseTest.cs` and
`CategoryEndpointsTest.cs` shifted in `b42f6f3`; those citations are **re-derived against HEAD**, not
carried over from iteration 1. All cited lines were mechanically dumped and confirmed to contain the
claimed assertion.

### P1: Keep a catalogue of categories and accounts (SHAR-01..03)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 create category → persisted with name/description/priority, 201 | 201 + the three fields | `WebApi.Test/Categories/CategoryEndpointsTest.cs:27-34` — `StatusCode.ShouldBe(Created)`, `name`/`description`/`priority` each `ShouldBe(request.*)` | ✅ PASS |
| AC1 linked to that user | `UserId` = logged user | `UseCases.Test/Categories/Register/RegisterCategoryUseCaseTest.cs:45` — `writeRepository.Added!.UserId.ShouldBe(loggedUser.Id)` | ✅ PASS |
| AC2 list categories → only that user's | second account sees none | `CategoryEndpointsTest.cs:65-68` — `ShouldContain(...name)`, `secondList...ShouldBeEmpty()` | ✅ PASS |
| AC3 create account → persisted to the person, 201 | 201 + `personId` | `CategoryEndpointsTest.cs:79-89` — `Created`, `personId.ShouldBe(personId)`, plus institution/closingDay/dueDay/limit | ✅ PASS |
| AC4 list accounts → only accounts whose person is that user's | second account sees none | `CategoryEndpointsTest.cs:120-123` — `secondList...ShouldBeEmpty()` | ✅ PASS |
| AC5 foreign person on an account → 404 | 404 | `CategoryEndpointsTest.cs:136` — `ShouldBe(HttpStatusCode.NotFound)`; key at `RegisterAccountUseCaseTest.cs:77` — `ShouldContain(PERSON_NOT_FOUND)` | ✅ PASS |
| AC6 empty name → 400 `NAME_REQUIRED` | that exact key | `RegisterCategoryUseCaseTest.cs:61`, `RegisterAccountUseCaseTest.cs:93` — `ShouldBe/ShouldContain(NAME_REQUIRED)`; validators `RegisterCategoryValidatorTest.cs:38`, `RegisterAccountValidatorTest.cs:37` | ⚠️ PASS with note — the key is pinned at two layers, but no endpoint test asserts the **400 status** on the catalogue routes specifically. The `ErrorOnValidationException` → 400 mapping is proved on a sibling route at `RegisterExpenseTest.cs:128`. Carried unchanged from iteration 1; acceptable. |
| AC7 closing/due day outside 1..31 → 400 `DAY_OUT_OF_RANGE` | that key, both bounds, both days | `RegisterAccountUseCaseTest.cs:97-98,111` and `:115-116,129` — `[InlineData(0)] [InlineData(32)]` for each day, `ShouldContain(DAY_OUT_OF_RANGE)`; valid bounds 1 and 31 accepted at `RegisterAccountValidatorTest.cs:71-73` | ✅ PASS |
| AC8 accept null closing day, due day, limit | all three null | `RegisterAccountUseCaseTest.cs:48-50` — `ClosingDay`/`DueDay`/`Limit` `.ShouldBeNull()` | ✅ PASS |
| AC9 no bearer token → 401 | 401 on all four routes | `CategoryEndpointsTest.cs:42, 50, 97, 105` — `ShouldBe(HttpStatusCode.Unauthorized)` ×4 | ✅ PASS |

### P1: Register an expense (EXPN-01..03)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 persist name/person/type/amount/category/account/date, 201 | all seven + 201 | `RegisterExpenseUseCaseTest.cs:26-41` — seven `added.*.ShouldBe(...)` on the persisted entity **and** seven on the response (`:28` type, `:30` category, `:32` date); endpoint `RegisterExpenseTest.cs:35-46` | ✅ PASS |
| AC2 credit, day ≤ closing day → month of the date | first day of that month | `CompetenceMonthResolverTest.cs:22` (day 20 of 20 → `2026-08-01`), `:14` (day 15); use case `RegisterExpenseUseCaseTest.cs:68`; endpoint `RegisterExpenseTest.cs:62` `"2026-08-01"` | ✅ PASS |
| AC3 credit, day > closing day → following month | first day of the next month | `CompetenceMonthResolverTest.cs:30` (21 > 20 → `2026-09-01`), `:38` (Dec → `2027-01-01`); `RegisterExpenseUseCaseTest.cs:54-55`; endpoint `RegisterExpenseTest.cs:47` `"2026-09-01"` | ✅ PASS |
| AC4 debit/pix → month of the date | closing day ignored | `CompetenceMonthResolverTest.cs:52-56` — `Debit_And_Pix_Ignore_The_Closing_Day`, `ShouldBe(2026-08-01)`; `RegisterExpenseUseCaseTest.cs:87-96` | ✅ PASS |
| AC5 credit with no closing day → month of the date | no roll | `CompetenceMonthResolverTest.cs:46`; `RegisterExpenseUseCaseTest.cs:109` | ✅ PASS |
| AC6 explicit competence month overrides, normalised to day 1 | `2026-11-17` → `2026-11-01`, beating a derived `2026-09-01` | `RegisterExpenseUseCaseTest.cs:124-125` — `ShouldBe(new DateOnly(2026, 11, 1))` on both the result and the persisted entity | ✅ PASS |
| AC7 accept an account of a different person of the same user | 201, both ids kept | `RegisterExpenseUseCaseTest.cs:152,158` — `PersonId` = spender, `AccountId` = card holder's; endpoint `RegisterExpenseTest.cs:80-81` | ✅ PASS |
| AC8 foreign person/category/account → 404 | 404, each naming its entity | `RegisterExpenseUseCaseTest.cs:172, 186, 200` — `PERSON_NOT_FOUND` / `CATEGORY_NOT_FOUND` / `ACCOUNT_NOT_FOUND`; endpoint `RegisterExpenseTest.cs:94, 107` | ✅ PASS |
| AC9 empty name → 400 `NAME_REQUIRED` | that key, both cultures | `RegisterExpenseUseCaseTest.cs:214`; endpoint `RegisterExpenseTest.cs:149` | ✅ PASS |
| AC10 amount ≤ 0 → 400 `AMOUNT_GREATER_THAN_ZERO` | that key | `RegisterExpenseUseCaseTest.cs:230` `[0][-1]`; endpoint `RegisterExpenseTest.cs:128` | ✅ PASS |

### P1: Register an installment purchase (INST-01..03)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 plan + installments in one transaction, 201 | exactly one `Commit()` | `RegisterInstallmentPlanUseCaseTest.cs:168,170` — plan added, `UnitOfWork.Commits.ShouldBe(1)`; 201 at `RegisterInstallmentPlanTest.cs:38` | ✅ PASS |
| AC2 exactly N expenses numbered 1..N, each referencing the plan | `[1,2,3,4,5]` + plan id | `RegisterInstallmentPlanUseCaseTest.cs:28,31,33` — `ShouldBe([1,2,3,4,5])`, `added.Count.ShouldBe(5)`, `ShouldAllBe(... InstallmentPlanId == result.Id)` | ✅ PASS |
| AC3 sum of installments = total exactly | equality, no lost cent | `RegisterInstallmentPlanUseCaseTest.cs:68` — `Sum(...).ShouldBe(total)` over 7 awkward totals incl. `0.05/3`, `99.99/7`, `0.02/12`; endpoint `RegisterInstallmentPlanTest.cs:56` | ✅ PASS |
| AC4 installment 1 by the credit rule, then +1 month each | Sep, Oct, Nov from `2026-08-21` @ closing 20 | `RegisterInstallmentPlanUseCaseTest.cs:101-102`; year boundary `:118-121`; endpoint `RegisterInstallmentPlanTest.cs:62` | ✅ PASS |
| AC5 every generated expense `Credit`, dated the start date | both, on all N | `RegisterInstallmentPlanUseCaseTest.cs:136-137` — `ShouldAllBe(Type == Credit)`, `ShouldAllBe(Date == 2026-08-21)` | ✅ PASS |
| AC6 plan end date = competence month of the last installment | `2027-02-01` | `RegisterInstallmentPlanUseCaseTest.cs:154,156` — on the result and on the persisted plan; endpoint `RegisterInstallmentPlanTest.cs:46` `"2026-11-01"` | ✅ PASS |
| AC7 count < 2 → 400 `INSTALLMENT_COUNT_INVALID` | that key | `RegisterInstallmentPlanUseCaseTest.cs:187` `[1][0][-1]`; endpoint `RegisterInstallmentPlanTest.cs:93` | ✅ PASS |
| AC8 total ≤ 0 → 400 `AMOUNT_GREATER_THAN_ZERO` | that key | `RegisterInstallmentPlanUseCaseTest.cs:203`; endpoint `RegisterInstallmentPlanTest.cs:115` | ✅ PASS |
| AC9 foreign person/category/account → 404 | 404 | `RegisterInstallmentPlanUseCaseTest.cs:217, 231, 245`; endpoint `RegisterInstallmentPlanTest.cs:129` | ✅ PASS |
| Independent Test: 100.00 over 3 → 33.33 / 33.33 / 33.34 | exact triple | `RegisterInstallmentPlanUseCaseTest.cs:46-47` — `ShouldBe([33.33m, 33.33m, 33.34m])`; endpoint `RegisterInstallmentPlanTest.cs:53` | ✅ PASS |

### P1: Register a recurring expense (RECR-01, RECR-02)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 expense + first version in one transaction, 201 | one `Commit()` | `RegisterRecurringExpenseUseCaseTest.cs:84` — `Commits.ShouldBe(1)`; fields `:30,40`; 201 at `RecurringExpenseEndpointsTest.cs:40` | ✅ PASS |
| AC2 first version's validity end is null | null | `RegisterRecurringExpenseUseCaseTest.cs:55-56` — on the persisted version **and** the response; endpoint `RecurringExpenseEndpointsTest.cs:57` — `validityEnd.ValueKind.ShouldBe(JsonValueKind.Null)` | ✅ PASS |
| AC3 `Archived` false, supplied `IsEstimate` stored | false + both true/false | `RegisterRecurringExpenseUseCaseTest.cs:71,73,74` — `[InlineData(true)][InlineData(false)]`, `Archived.ShouldBeFalse()`, `IsEstimate.ShouldBe(isEstimate)` | ✅ PASS |
| AC4 foreign person/category/account → 404 | 404 | `RegisterRecurringExpenseUseCaseTest.cs:98, 112, 126`; endpoint `RecurringExpenseEndpointsTest.cs:158` | ✅ PASS |
| AC5 empty name → 400 `NAME_REQUIRED` | that key | `RegisterRecurringExpenseUseCaseTest.cs:140`; endpoint both cultures `RecurringExpenseEndpointsTest.cs:163` | ✅ PASS |
| AC6 base amount ≤ 0 → 400 `AMOUNT_GREATER_THAN_ZERO` | that key | `RegisterRecurringExpenseUseCaseTest.cs:156`; endpoint `RecurringExpenseEndpointsTest.cs:178` | ✅ PASS |
| AC7 due day outside 1..31 → 400 `DAY_OUT_OF_RANGE` | that key, both bounds | `RegisterRecurringExpenseUseCaseTest.cs:172` `[0][32]`; bounds 1/31 accepted `RegisterRecurringExpenseValidatorTest.cs:66-68`; endpoint `RecurringExpenseEndpointsTest.cs:193` | ✅ PASS |

### P1: Record what a recurring expense actually cost (RPAY-01..03)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 persist month, date, amount, notes, paying account, 201 | all five | `RegisterRecurringExpensePaymentUseCaseTest.cs:31-44` — five on the persisted entity (`:33` payment date) **and** five on the response (`:38`, `:44` paying account); endpoint `RecurringExpensePaymentTest.cs:44-51` | ✅ PASS |
| AC2 store the id of the version in effect at the reference month | the *closed* version's id for a past month | `RegisterRecurringExpensePaymentUseCaseTest.cs:83,85` — `ShouldBe(oldVersion.Id)` on the response and on the persisted entity; endpoint `RecurringExpensePaymentTest.cs:82-83` — `frozen.ShouldBe(oldVersionId)` **and** `ShouldNotBe(newVersionId)` | ✅ PASS |
| AC3 update overwrites amount, date, notes, paying account, 200 | all four + 200 | `UpdateRecurringExpensePaymentUseCaseTest.cs:30,34,37`; endpoint `RecurringExpensePaymentTest.cs:133,136` | ✅ PASS |
| AC4 update leaves reference month and version id unchanged | unchanged even when the month now resolves to a newer version | `UpdateRecurringExpensePaymentUseCaseTest.cs:87,89` — `RecurringExpenseVersionId.ShouldBe(oldVersion.Id)`, `ReferenceMonth.ShouldBe(2026-08-01)`; endpoint `RecurringExpensePaymentTest.cs:138,141` | ✅ PASS |
| AC5 duplicate month → 400 `PAYMENT_ALREADY_RECORDED` | that key, nothing written | `RegisterRecurringExpensePaymentUseCaseTest.cs:141,143,144` — key asserted, `Added.ShouldBeNull()`, `Commits.ShouldBe(0)`; endpoint `RecurringExpensePaymentTest.cs:146-160` | ✅ PASS |
| AC6 foreign recurring expense or payment → 404 | 404, each naming its entity | `RegisterRecurringExpensePaymentUseCaseTest.cs:219` (`RECURRING_EXPENSE_NOT_FOUND`), `UpdateRecurringExpensePaymentUseCaseTest.cs:149` (`RECURRING_EXPENSE_PAYMENT_NOT_FOUND`); endpoint `RecurringExpensePaymentTest.cs:236, 251` | ✅ PASS |
| AC7 archived expense → 400 `RECURRING_EXPENSE_ARCHIVED` | that key | `RegisterRecurringExpensePaymentUseCaseTest.cs:156`; endpoint `RecurringExpensePaymentTest.cs:164-180` (archived through the real archive route) | ✅ PASS |
| AC8 amount ≤ 0 → 400 `AMOUNT_GREATER_THAN_ZERO` | that key, both paths | `RegisterRecurringExpensePaymentUseCaseTest.cs:191`, `UpdateRecurringExpensePaymentUseCaseTest.cs:122`; endpoint `RecurringExpensePaymentTest.cs:197, 212` | ✅ PASS |
| AC9 no version in effect → 400 `NO_VERSION_IN_EFFECT` | that key | `RegisterRecurringExpensePaymentUseCaseTest.cs:171`; endpoint `RecurringExpensePaymentTest.cs:182` | ✅ PASS |
| AC10 accept null/empty notes and a null paying account | both null | `RegisterRecurringExpensePaymentUseCaseTest.cs:110,113`; `UpdateRecurringExpensePaymentUseCaseTest.cs:103,106`; endpoint `RecurringExpensePaymentTest.cs:102-103` | ✅ PASS |
| Independent Test: 180 → correct to 172.40; Aug reports 172.40, Sep still 150 | both months | Correction: `UpdateRecurringExpensePaymentUseCaseTest.cs:34`, endpoint `RecurringExpensePaymentTest.cs:133`. **Note (carried):** the "September still reports 150.00" half is proved by the version-resolution tests (`GetMonthlyExpenseUseCaseTest.cs:156-179`) rather than by one end-to-end two-month fixture. | ✅ PASS |

### P1: View the expenses of a given month (VIEW-01..04)

All citations in this section re-derived against HEAD.

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 one variable line per matching expense | one line, right amount | `GetMonthlyExpenseTest.cs:50` — `variable.ShouldHaveSingleItem()...ShouldBe(80.00m)`; cross-user isolation `:199-201` | ✅ PASS |
| AC2 one recurring line per non-archived recurring expense | 2 lines; archived omitted | `GetMonthlyExpenseTest.cs:51` — `recurring.Count.ShouldBe(2)`; archived omission `:108` — `ShouldBeEmpty()` | ✅ PASS |
| AC3a version in effect → report that version's amount as expected | 150.00 from the closed version, not 180.00 from the open one | `GetMonthlyExpenseUseCaseTest.cs:178` — `ExpectedAmount.ShouldBe(150m)`; endpoint `GetMonthlyExpenseTest.cs:54` | ✅ PASS |
| **AC3b … and the recurring expense's due day** | the stored `DueDay`, per line | `GetMonthlyExpenseUseCaseTest.cs:91-92` — `First(line => line.Name == "Aluguel").DueDay.ShouldBe(10)` and `"Netflix" … ShouldBe(22)`; endpoint `GetMonthlyExpenseTest.cs:165,168` — `GetProperty("dueDay").GetInt32().ShouldBe(10)` / `(22)` | ✅ **PASS — gap closed** (was ❌ in iteration 1) |
| AC4 no version in effect → null expected | null | `GetMonthlyExpenseUseCaseTest.cs:152` — `ExpectedAmount.ShouldBeNull()` | ✅ PASS |
| AC5 payment exists → actual = amount paid; `Paid` when actual == expected | 150/150 → `Paid` | `GetMonthlyExpenseUseCaseTest.cs:32-34`; endpoint `GetMonthlyExpenseTest.cs:83-84` — `status == (int)ExpenseStatus.Paid` | ✅ PASS |
| AC6 payment differs from a non-null expected → `Divergent` | 180 vs 150 → `Divergent` | `GetMonthlyExpenseUseCaseTest.cs:51-53`; endpoint `GetMonthlyExpenseTest.cs:56` | ✅ PASS |
| AC7 no payment → null actual, `Pending` | both | `GetMonthlyExpenseUseCaseTest.cs:70-72`; endpoint `GetMonthlyExpenseTest.cs:60-61` | ✅ PASS |
| AC8 report the `IsEstimate` flag | true and false | `GetMonthlyExpenseUseCaseTest.cs:193-194`; endpoint `GetMonthlyExpenseTest.cs:181-185` | ✅ PASS |
| AC9 four totals; committed = variable + (actual ?? expected) per line | 80 + 180 + 45 = 305 | `GetMonthlyExpenseUseCaseTest.cs:260-265` — all four pinned; endpoint `GetMonthlyExpenseTest.cs:63-68` | ✅ PASS |
| AC10 installment line reports number and plan count | 3 of 10 / 1 of 3 | `GetMonthlyExpenseUseCaseTest.cs:222-224`; endpoint `GetMonthlyExpenseTest.cs:142-143`; negative case (one-off carries no markers) `:236-240` | ✅ PASS |
| AC11 invalid month → 400 `REFERENCE_MONTH_INVALID` | that key | `GetMonthlyExpenseUseCaseTest.cs:298-299` `[0][13]`; endpoint both cultures `GetMonthlyExpenseTest.cs:234` | ✅ PASS |
| AC12 nothing recorded → 200, empty collections, zeroed totals | 200 + 4 zeros | `GetMonthlyExpenseUseCaseTest.cs:277-283`; endpoint `GetMonthlyExpenseTest.cs:211-216` (200 asserted at `:251`) | ✅ PASS |
| Assumptions row: payment present + expected null → `Paid` | `Paid`, null expected, non-null actual | `GetMonthlyExpenseUseCaseTest.cs:134-136` — `ExpectedAmount.ShouldBeNull()`, `ActualAmount.ShouldBe(90m)`, `Status.ShouldBe(ExpenseStatus.Paid)` | ✅ **PASS — now specified and pinned** (was ⚠️ in iteration 1) |

### P2: Change the base value of a recurring expense (RECR-03, RECR-04)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 close the version in effect at the day before the new start | new start `2026-09-01` → end `2026-08-31` | `ChangeRecurringExpenseValueUseCaseTest.cs:23` — `ValidityEnd.ShouldBe(new DateOnly(2026, 8, 31))`; gap/overlap-free at `:63`; endpoint `RecurringExpenseEndpointsTest.cs:81` `"2026-08-31"` | ✅ PASS |
| AC2 create a new version with amount, start, null end, reason | all four | `ChangeRecurringExpenseValueUseCaseTest.cs:39,43,46`; endpoint `RecurringExpenseEndpointsTest.cs:83,86` | ✅ PASS |
| AC3 closing + creation in one transaction | one `Commit()` | `ChangeRecurringExpenseValueUseCaseTest.cs:73` — `Commits.ShouldBe(1)` | ✅ PASS |
| AC4 previously recorded payments keep their version id | id, month and amount all unchanged | `ChangeRecurringExpenseValueUseCaseTest.cs:97,99` — `RecurringExpenseVersionId.ShouldBe(frozenVersionId)`, `AmountPaid.ShouldBe(152.40m)` | ✅ PASS |
| AC5 empty reason → 400 `CHANGE_REASON_REQUIRED` | that key | `ChangeRecurringExpenseValueUseCaseTest.cs:113`; endpoint both cultures `RecurringExpenseEndpointsTest.cs:208` | ✅ PASS |
| AC6 new start not later than the current → 400 `VALIDITY_START_MUST_BE_LATER` | that key, equal **and** earlier | `ChangeRecurringExpenseValueUseCaseTest.cs:125` (equal) and `:137` (earlier); rejected change leaves the version open and commits nothing `:150,152`; endpoint `RecurringExpenseEndpointsTest.cs:224` | ✅ PASS |
| AC7 foreign recurring expense → 404 | 404, nothing mutated | `ChangeRecurringExpenseValueUseCaseTest.cs:176,179` — key + `AddedVersions.ShouldBeEmpty()`; endpoint `RecurringExpenseEndpointsTest.cs:130` | ✅ PASS |

### P2: Archive a recurring expense (RECR-05)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 archive → flag true, 204 | true + 204 | `ArchiveRecurringExpenseUseCaseTest.cs:24-25` — `Archived.ShouldBeTrue()`, `Commits.ShouldBe(1)`; endpoint `RecurringExpenseEndpointsTest.cs:98` `NoContent` | ✅ PASS |
| AC2 unarchive → flag false, 204 | false + 204 | `ArchiveRecurringExpenseUseCaseTest.cs:38-39`; endpoint `RecurringExpenseEndpointsTest.cs:114` | ✅ PASS |
| AC3 archived → omitted from the month, payments kept in the database | line and totals gone; unarchive restores 150.00 | `GetMonthlyExpenseTest.cs:108-110` then `:120` — `actualAmount.ShouldBe(150.00m)` after unarchiving; proved at the only layer where the repository's `Archived == false` filter runs | ✅ PASS |
| AC4 foreign recurring expense → 404 | 404, flag untouched | `ArchiveRecurringExpenseUseCaseTest.cs:82,84,85` — key, `Archived.ShouldBeFalse()`, `Commits.ShouldBe(0)`; endpoint `RecurringExpenseEndpointsTest.cs:144` | ✅ PASS |

### P2: See income and expenses for the same month (DASH-01, DASH-02)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 return the existing income view and the expense view for the same month | both halves byte-identical to the individual endpoints | `GetMonthlyDashboardTest.cs:52-53` — `GetRawText().ShouldBe(...)` for both halves; unit `GetMonthlyDashboardUseCaseTest.cs:26-27` `ShouldBeSameAs` | ✅ PASS |
| AC2 balance = total income received − total committed expense | 5000 − 305 = 4695 | `GetMonthlyDashboardTest.cs:75` — `ShouldBe(4695.00m)`; negative case `:89` — `ShouldBe(-300.00m)`; unit discriminators `GetMonthlyDashboardUseCaseTest.cs:40, 53, 67` (expected-vs-received and committed-vs-paid each pinned separately) | ✅ PASS |
| AC3 no bearer token → 401 | 401 | `GetMonthlyDashboardTest.cs:144` — `ShouldBe(HttpStatusCode.Unauthorized)` | ✅ PASS |
| AC4 income half produced by invoking the existing use case, income unmodified | invoked once, for the requested month, object passed through unchanged | `GetMonthlyDashboardUseCaseTest.cs:83` — `Verify(Execute(2026, 8), Times.Once)`; `:26` — `ShouldBeSameAs(income)`; plus the AD-006 diff evidence and sensor M8 | ✅ PASS |

**Status**: ✅ **All 73 clause-level rows covered** — 72/72 acceptance criteria matched the spec-defined
outcome, plus the newly pinned Assumptions row. 0 gaps. 1 ⚠️ note carried on SHAR AC6 (status code
asserted on a sibling route rather than the catalogue routes).

---

## Payload / conjunction rule

Checked on every payload-bearing criterion: assertions must target field **values / state**, not that a
call occurred.

| Criterion | Check | Result |
| --------- | ----- | ------ |
| EXPN AC1 | Seven persisted fields **and** seven response fields asserted by value (`RegisterExpenseUseCaseTest.cs:26-41`), not `Verify(Add(It.IsAny<Expense>()))` | ✅ |
| RPAY AC1 | Five persisted + five response fields by value (`RegisterRecurringExpensePaymentUseCaseTest.cs:31-44`) | ✅ |
| RPAY AC3 | Update asserts the four overwritten values, and sensor **M9** proves the paying-account write is load-bearing (dropping it kills 2 use-case + 1 endpoint test) | ✅ |
| INST AC1 | `Commit()` counted at exactly 1 via `UnitOfWorkBuilder.Commits`, not `Verify(..., Times.Once)` on a bare mock | ✅ |
| DASH AC1 | Reference identity (`ShouldBeSameAs`) at the unit layer and raw JSON equality end to end — the strongest available form | ✅ |
| DASH AC4 | Not merely "the use case was called": `Verify(Execute(2026, 8))` pins the *arguments*, and M8 confirms a wrong month is caught | ✅ |
| RECR-03 AC1 | The closed version's `ValidityEnd` asserted as the literal `2026-08-31`, not "is not null" | ✅ |
| **VIEW AC3 (conjunction)** | Both clauses now asserted separately — expected amount **and** due day, at two layers. This was the one violation in iteration 1 | ✅ **fixed** |
| Rejection paths | Every 400/404 test also asserts nothing was added and `Commits == 0` — conjunction, not just the exception | ✅ |

No violations remain.

---

## Discrimination Sensor

**Isolation**: two temporary `git worktree`s on detached `HEAD` (`b42f6f3`) under the session
scratchpad — `sensor2` (M1–M11) and `sensor3` (M12). Mutations were applied to those copies only,
tests run there, and the copy reverted with `git checkout -- .` after each mutation. Both worktrees were
removed with `git worktree remove --force` followed by `git worktree prune`. **No `git stash` was
used at any point.**

- Pre-sensor baseline: `git status --porcelain` on the real tree → **empty**.
- Post-sensor: `git status --porcelain` → **empty**; `git worktree list` shows only
  `C:/estudos/projetos/Balance/backend`; `HEAD` still `b42f6f3` on `feature/expense-tracking`.
- **Isolation verified** — the real working tree matches the pre-sensor baseline exactly.

**Depth**: P0-full (money arithmetic + data integrity). **12 behaviour-level mutations**, of which 2 are
re-injections of iteration 1's survivors, 2 are deliberate probes of the *new* tests, and 8 are a fresh
selection across the highest-risk logic.

| # | File:line | Mutation | Killed? |
| - | --------- | -------- | ------- |
| **M1** | `GetMonthlyExpenseUseCase.cs:90` | `DueDay = expense.DueDay` → `DueDay = 0` — *iteration-1 survivor, re-injected* | ✅ **Killed** — 2 use-case + 1 endpoint. Killers named: `A_Recurring_Line_Reports_The_Due_Day_Of_Its_Expense`, `A_Due_Day_Of_31_Is_Not_Clamped_To_A_Shorter_Month` |
| **M2** | `GetMonthlyExpenseUseCase.cs:118-120` | `expectedAmount is null` branch returns `Divergent` instead of `Paid` — *iteration-1 survivor, re-injected* | ✅ **Killed** — 1 use-case (`A_Payment_With_No_Version_In_Effect_Is_Paid_With_A_Null_Expected`). Endpoint suite unaffected, as expected: the branch is unreachable through the public API |
| **M3** | `GetMonthlyExpenseUseCase.cs:90` | **Probe of the new edge-case test**: `DueDay = Math.Min(expense.DueDay, DateTime.DaysInMonth(competenceMonth.Year, competenceMonth.Month))` — a genuine clamping implementation | ✅ **Killed** — sole killer is `A_Due_Day_Of_31_Is_Not_Clamped_To_A_Shorter_Month`. The test **does** discriminate; it is not a duplicate of M1's coverage |
| **M4** | `CompetenceMonthResolver.cs:15-17` | Drop the `type == ExpenseType.Credit` guard, so debit/pix roll on the closing day too | ✅ Killed (4 use-case) |
| **M5** | `RegisterInstallmentPlanUseCase.cs:135` | `AddMonths(number - 1)` → `AddMonths(number)` — off-by-one on the competence-month advance | ✅ Killed (3 use-case + 2 endpoint) |
| **M6** | `RecurringExpenseExtensions.cs:26` | Drop the closed-version clause, so a closed version is never in effect | ✅ Killed (4 use-case + 1 endpoint) |
| **M7** | `GetMonthlyExpenseUseCase.cs:53` | `TotalCommitted` drops the `totalVariable` operand | ✅ Killed (1 use-case + 3 endpoint) |
| **M8** | `GetMonthlyDashboardUseCase.cs:29` | Dashboard asks the income use case for month `1` instead of the requested month | ✅ Killed (1 use-case + 3 endpoint) |
| **M9** | `UpdateRecurringExpensePaymentUseCase.cs:47` | Remove the required side effect `payment.AccountId = request.AccountId` | ✅ Killed (2 use-case + 1 endpoint) |
| **M10** | `RegisterRecurringExpensePaymentUseCase.cs:61` | Freeze the *newest* version instead of the one in effect at the reference month | ✅ Killed (2 use-case + 3 endpoint) |
| **M11** | `GetAllCategoriesUseCase.cs:23` + `GetAllAccountsUseCase.cs` | **Probe of the new same-name tests**: catalogue listings `DistinctBy(name)`, collapsing same-named rows | ✅ **Killed** — 2 endpoint, exactly `Two_Categories_Of_The_Same_User_May_Share_A_Name` and `Two_Accounts_Of_The_Same_Person_May_Share_A_Name` |
| **M12** | `RegisterInstallmentPlanUseCase.cs:122` | `MidpointRounding.AwayFromZero` → `MidpointRounding.ToEven` on installments 1..N-1 | ❌ **SURVIVED** — see below |

**Sensor depth**: P0-full
**Result**: **11/12 killed** - PASS ✅ (the single survivor is spec-acknowledged and non-blocking; see below)

Every mutation on the logic the spec makes load-bearing died, several with a single targeted killer:
competence-month derivation, installment competence advance, version-in-effect resolution across closed
ranges, the committed total, the dashboard's month argument and balance operands, the frozen version id
on both the register and the update path, and the required side effects on a payment correction.

**On M12 (the one survivor).** This is **not** a regression and **not** a newly discovered defect in the
implementation — the code does exactly what `spec.md` says. The Assumptions table pins
`MidpointRounding.AwayFromZero` and, in the same row, states: *"It affects at most one cent on a
non-final installment, and the final installment is the residual, so the sum is exact under either mode.
No test depends on the choice."* This run **empirically confirms that claim is accurate**: none of the
seven totals in the suite (`100/3`, `10/3`, `0.05/3`, `1000/7`, `99.99/7`, `1234.56/11`, `0.02/12`)
produces an exact half-cent midpoint, and `The_Remainder_Lands_Entirely_On_The_Last_Installment`
(`RegisterInstallmentPlanUseCaseTest.cs:84`) recomputes `each` with the same rounding mode the
implementation uses, so it mirrors the implementation rather than pinning the mode independently.

A distinguishing input does exist (e.g. total `0.05` over `2` installments: `0.025` → `0.03`
away-from-zero vs `0.02` to-even), so the mutant is *not* equivalent — it is simply outside the input
set the suite exercises. AC3 ("the sum equals the total exactly") holds under either mode, which is why
no acceptance criterion fails. Classified as a **⚠️ spec-precision note, accepted**: the spec
deliberately declines to require a test here, the money invariant the user actually cares about is
proved, and iteration 1 already recorded lesson **L-008** on the underlying pattern. It does not block
the verdict. A one-line strengthening is suggested (not required) under Fix Plans.

---

## Code Quality

| Principle | Status | Note |
| --------- | ------ | ---- |
| Minimum code | ✅ | No abstraction added for single-use code; `CompetenceMonthResolver` and `VersionInEffect` are pure static functions with two and one caller |
| Surgical changes | ✅ | 14 pre-existing files modified, each for a stated reason; no income file touched |
| No scope creep | ✅ | Everything in the diff maps to a spec goal or an approved addition in the Assumptions table; nothing from the Out of Scope table appears. `b42f6f3` adds tests and one spec row only — no production code changed to make a test pass |
| Matches patterns | ✅ | Repository read/write split, `AbstractValidator<TRequest>`, `ProducesResponseType`, `(CommunicationX)domainX` casts, Bogus/Moq/Shouldly builders — all mirror the income slice |
| Didn't "improve" unrelated code | ✅ | `UnitOfWorkBuilder` change is purely additive; the existing `static Build()` is byte-identical |
| Spec-anchored outcome check | ✅ | 72/72 assertions match the spec-defined outcome; the previously unspecified status branch is now specified and asserted |
| Per-layer Coverage Expectation | ✅ | Domain rules 1:1 with ACs; every route in scope has happy + edge + error + 401 paths |
| Every test maps to a spec requirement | ✅ | No unclaimed tests. The three tests added by `b42f6f3` map to VIEW AC3b, a spec edge case, a spec Assumptions row, and a spec edge case respectively — each carries an XML doc comment naming its clause |
| Documented guidelines followed | ✅ | `.claude/skills/tlc-spec-driven/references/coding-principles.md`; the repo's `dotnet-arch-guard` conventions (layer boundaries, DI registration, `ProducesResponseType`, user-scoped queries) hold across all five new controllers |
| Would a senior engineer approve? | ✅ | Yes |

---

## Edge Cases

| Edge case from spec.md | Evidence | Result |
| ---------------------- | -------- | ------ |
| Month before every version → null expected | `GetMonthlyExpenseUseCaseTest.cs:152`; `RecurringExpenseExtensionsTest.cs:16` | ✅ |
| Month inside a closed version's range → that closed version's amount | `RecurringExpenseExtensionsTest.cs:26` (150 not 180); `GetMonthlyExpenseUseCaseTest.cs:178`. Sensor M6 confirms discrimination | ✅ |
| Credit dated exactly on the closing day → month of the date | `CompetenceMonthResolverTest.cs:22`; endpoint `RegisterExpenseTest.cs:62` | ✅ |
| Total not dividing evenly → whole remainder on the last installment | `RegisterInstallmentPlanUseCaseTest.cs:86-87` — first N−1 all equal `each`, last equals `total − each×(N−1)` | ✅ |
| Due day 31 in a shorter month → reported as stored, not clamped | `GetMonthlyExpenseUseCaseTest.cs:111` — `ShouldHaveSingleItem().DueDay.ShouldBe(31)` for February 2026. Sensor M3 proves it discriminates against a real clamping implementation | ✅ **now covered** (was ❌) |
| Two categories or two accounts of the same user with the same name → both accepted | `CategoryEndpointsTest.cs:155-156,163-164` and `:179-180,187-188` — both `201`, both listed, distinct ids. Sensor M11 proves discrimination | ✅ **now covered** (was ❌) |
| Archived after a payment → payment retrievable, line omitted | `GetMonthlyExpenseTest.cs:93-121` — archived, totals zeroed, unarchived, 150.00 restored | ✅ |

**7/7 covered** (was 5/7).

---

## Gate Check

- **Build gate**: `dotnet build Balance.sln --nologo` → **0 errors, 0 warnings** ✅
- **Test gate**: `dotnet test Balance.sln --nologo` → **348 passed, 0 failed, 0 skipped** ✅
  - Validators.Tests **59** · UseCases.Test **173** · WebApi.Test **116**
- **Test count before feature**: 82 · **after**: 348 · **delta**: **+266** · **skipped**: none
- **Failures**: none
- **Migration**: `20260812233254_AddExpenseTracking` — additive; `InitialCreate` untouched

> **Environment note.** The `MSB3021`/`MSB3027` copy-lock that forced iteration 1 onto
> `--artifacts-path` did **not** recur — the dev API was stopped, so both gates ran in place against the
> normal `bin/` output. The figures above are from unmodified in-place runs.

---

## Fix Plans

No blocking fixes. One optional strengthening.

### Optional — pin the installment midpoint rounding mode with a midpoint input (Cosmetic)

- **Root cause**: `RegisterInstallmentPlanUseCase.cs:122` specifies `MidpointRounding.AwayFromZero`, and
  `spec.md` names it, but no test input lands on an exact half-cent, and
  `The_Remainder_Lands_Entirely_On_The_Last_Installment` recomputes the expected share with the same
  mode as the implementation. Sensor M12 (`AwayFromZero → ToEven`) survives all 348 tests.
- **Suggested fix**: add one `[InlineData(0.05, 2)]` row to
  `RegisterInstallmentPlanUseCaseTest.The_Remainder_Lands_Entirely_On_The_Last_Installment` and assert
  the literal pair `[0.03m, 0.02m]` rather than a recomputed `each`.
- **Priority**: **Cosmetic.** The spec explicitly records that no test depends on this choice, the money
  invariant (AC3, exact sum) holds under either mode, and no acceptance criterion is affected. Deferring
  with a recorded decision is a legitimate outcome.

---

## Requirement Traceability Update

| Requirement | Previous Status | New Status |
| ----------- | --------------- | ---------- |
| SHAR-01 | ✅ Verified | ✅ Verified |
| SHAR-02 | ✅ Verified | ✅ Verified |
| SHAR-03 | ✅ Verified | ✅ Verified |
| EXPN-01 | ✅ Verified | ✅ Verified |
| EXPN-02 | ✅ Verified | ✅ Verified |
| EXPN-03 | ✅ Verified | ✅ Verified |
| INST-01 | ✅ Verified | ✅ Verified |
| INST-02 | ✅ Verified | ✅ Verified |
| INST-03 | ✅ Verified | ✅ Verified |
| RECR-01 | ✅ Verified | ✅ Verified |
| RECR-02 | ✅ Verified | ✅ Verified |
| RECR-03 | ✅ Verified | ✅ Verified |
| RECR-04 | ✅ Verified | ✅ Verified |
| RECR-05 | ✅ Verified | ✅ Verified |
| RPAY-01 | ✅ Verified | ✅ Verified |
| RPAY-02 | ✅ Verified | ✅ Verified |
| RPAY-03 | ✅ Verified | ✅ Verified |
| VIEW-01 | ✅ Verified | ✅ Verified |
| **VIEW-02** | ❌ Needs Fix | ✅ **Verified** — VIEW AC3's due-day clause now asserted at two layers; M1 and M3 both killed |
| **VIEW-03** | ⚠️ Verified with a spec-precision gap | ✅ **Verified** — the null-expected branch is specified in `spec.md` and pinned by a test; M2 killed |
| VIEW-04 | ✅ Verified | ✅ Verified |
| DASH-01 | ✅ Verified | ✅ Verified |
| DASH-02 | ✅ Verified | ✅ Verified |

**23 / 23 verified.**

---

## Summary

**Overall**: ✅ **Ready.**

**Spec-anchored check**: 72/72 ACs matched the spec-defined outcome · 0 gaps · 0 open spec-precision gaps
**Sensor**: 11/12 mutations killed (P0-full depth; the survivor is spec-acknowledged and non-blocking)
**Gate**: 348 passed, 0 failed, 0 skipped · build 0 errors, 0 warnings
**Constraints**: AD-006 upheld by diff evidence and by sensor M8 · 82 pre-existing tests green and unedited
**Isolation**: real working tree byte-identical to the pre-sensor baseline; no stray worktrees

**What works**: The money arithmetic is genuinely well tested. The installment residual is proved
against seven awkward totals rather than one happy path; the competence-month boundary is asserted at
the resolver, the use case and the endpoint; the version freeze is asserted against a *past* month after
a value change, so an implementation freezing the currently-open version fails on its own; the dashboard
compares raw serialised JSON against the income endpoint, which is the strongest available proof of
AD-006. Every rejection path asserts that nothing was written and nothing committed.

**What iteration 1's fix genuinely closed**: all three gaps, verified by re-injection rather than by the
author's word. The due-day clause is now asserted at two layers and survives a *clamping* implementation,
not just a zeroing one — the two new tests are not redundant with each other. The status branch moved
from unspecified to specified-and-pinned, in the right place in the spec. The same-name edge case is
covered by tests that a listing-level de-duplication actually kills.

**Issues found**: one non-blocking spec-precision note (M12, installment midpoint mode) — already
covered by lesson L-008 and explicitly declared untested by the spec itself. Optional fix recorded above.

**Next steps**: The feature is ready to close. No fix→re-verify iteration is required; the loop ends at
iteration 2 of 3.
