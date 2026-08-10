# Income Tracking Validation

**Date**: 2026-08-10
**Spec**: `.specs/features/income-tracking/spec.md`
**Diff range**: `22f4893..c0068de` (20 commits)
**Verifier**: standalone fresh-eyes pass. Sub-agent delegation is offer-then-confirm and the user
delegated the run without an answering party, so `validate.md` was run as the sanctioned fallback.
This is a disclosed weakening of author-does-not-equal-verifier.

---

## Task Completion

| Task | Status | Notes |
| ---- | ------ | ----- |
| T1-T4 | ✅ Done | Phase 1, identity foundation |
| T5-T9 | ✅ Done | Phase 2, Person vertical |
| T10-T13 | ✅ Done | Phase 3, income domain and persistence |
| T14-T16 | ✅ Done | Phase 4, income write use cases |
| T17-T18 | ✅ Done | Phase 5, monthly view and API |
| T19 | ✅ Done | Phase 6, schema |
| T20 | ✅ Done | Phase 7, fix task raised by the discrimination sensor |

---

## Spec-Anchored Acceptance Criteria

### CORE - Shared entity identity and audit trail

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 `BaseEntity` exposes Id/CreatedAt/UpdatedAt | Guid, DateTime, nullable DateTime | `src/Balance.Domain/Entities/BaseEntity.cs:5-9` (build gate; matrix says none for entity layer) | ✅ PASS |
| AC2 first persist sets `CreatedAt` | current UTC instant | `tests/WebApi.Test/Identity/AuditTimestampTest.cs:30` - `user.CreatedAt.ShouldNotBe(default)` and `:31` - `.Kind.ShouldBe(DateTimeKind.Utc)` | ✅ PASS |
| AC3 first persist leaves `UpdatedAt` null | null | `tests/WebApi.Test/Identity/AuditTimestampTest.cs:32` - `user.UpdatedAt.ShouldBeNull()` | ✅ PASS |
| AC4 update sets `UpdatedAt` | current UTC instant | `tests/WebApi.Test/Identity/AuditTimestampTest.cs:50-52` - `ShouldNotBeNull()`, `.Kind.ShouldBe(Utc)`, `ShouldBeGreaterThanOrEqualTo(createdAt)` | ✅ PASS |
| AC5 `User` identified by `BaseEntity.Id`, no `UserIdentifier` | field absent | `src/Balance.Domain/Entities/User.cs:5-11`; `grep UserIdentifier src/ tests/` returns only the regenerated migration's absence | ✅ PASS |
| AC6 token `Sid` claim carries `Id` | the persisted row's Guid | `tests/WebApi.Test/Identity/TokenIdentityTest.cs:28` - `sid.ShouldBe(expectedId.ToString())` | ✅ PASS |
| AC7 logged-user service resolves on `Sid` | the matching User | `tests/WebApi.Test/Identity/TokenIdentityTest.cs:43-44` - `user.Id.ShouldBe(...GetId())`, `user.Email.ShouldBe(...GetEmail())` | ✅ PASS |

### PRSN - Manage the people of my account

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 registration creates an owner Person | one Person, `IsAccountOwner` true, registration name | `tests/WebApi.Test/People/GetAll/GetAllPeopleTest.cs:38-40` - `people.Count.ShouldBe(1)`, `GetProperty("isAccountOwner").GetBoolean().ShouldBeTrue()`, `GetProperty("name").GetString().ShouldBe(registerRequest.Name)` | ✅ PASS |
| AC2 create Person → 201, linked, `IsAccountOwner` false | 201 + ownership | `tests/WebApi.Test/People/Register/RegisterPersonTest.cs:30` - `ShouldBe(HttpStatusCode.Created)`; `tests/UseCases.Test/People/Register/RegisterPersonUseCaseTest.cs:45-46` - `Added!.UserId.ShouldBe(loggedUser.Id)`, `IsAccountOwner.ShouldBeFalse()` | ✅ PASS |
| AC3 list returns only the caller's People | caller-scoped set | `tests/WebApi.Test/People/GetAll/GetAllPeopleTest.cs:59-61` - `people.Count.ShouldBe(1)`, `people[0]...ShouldBe(secondAccount.Name)`; `tests/UseCases.Test/People/GetAll/GetAllPeopleUseCaseTest.cs:57` - `result.People.ShouldBeEmpty()` | ✅ PASS |
| AC4 no token → 401 | 401 | `tests/WebApi.Test/People/GetAll/GetAllPeopleTest.cs:22` and `tests/WebApi.Test/People/Register/RegisterPersonTest.cs:47` - `ShouldBe(HttpStatusCode.Unauthorized)` | ✅ PASS |
| AC5 empty name → 400 `NAME_REQUIRED` | 400 + localized message | `tests/WebApi.Test/People/Register/RegisterPersonTest.cs:57` + `:69` - status 400 and `errors.ShouldHaveSingleItem().GetString().ShouldBe(expected)` resolved from `ResourceManager` per culture | ✅ PASS |
| AC6 null/empty description accepted | valid | `tests/Validators.Tests/People/Register/RegisterPersonValidatorTest.cs:30` - `result.IsValid.ShouldBeTrue()` | ✅ PASS |

### INC - Register an income source

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 Recurring persists source + first version | one version | `tests/UseCases.Test/Incomes/Register/RegisterIncomeSourceUseCaseTest.cs:34` - `AddedVersions.Count.ShouldBe(1)` | ✅ PASS |
| AC2 first version `ValidityEnd` null | null | `...RegisterIncomeSourceUseCaseTest.cs:35` - `AddedVersions[0].ValidityEnd.ShouldBeNull()` | ✅ PASS |
| AC3 Variable persists no version | zero versions | `...RegisterIncomeSourceUseCaseTest.cs:56` - `AddedVersions.ShouldBeEmpty()` | ✅ PASS |
| AC4 `Archived` set false | false | `...RegisterIncomeSourceUseCaseTest.cs:31` - `result.Archived.ShouldBeFalse()` | ✅ PASS |
| AC5 Person of another user → 404 | 404 / NotFoundException | `...RegisterIncomeSourceUseCaseTest.cs:93` - `ShouldThrowAsync<NotFoundException>()`; `tests/WebApi.Test/Incomes/IncomeFlowTest.cs:139` - `ShouldBe(HttpStatusCode.NotFound)` | ✅ PASS |
| AC6 empty name → 400 `NAME_REQUIRED` | message | `...RegisterIncomeSourceUseCaseTest.cs:100` - `GetErrors().ShouldContain(ResourceErrorMessages.NAME_REQUIRED)` | ✅ PASS |
| AC7 amount ≤ 0 → 400 `AMOUNT_GREATER_THAN_ZERO` | message | `...RegisterIncomeSourceUseCaseTest.cs:115`; culture-checked end to end at `tests/WebApi.Test/Incomes/IncomeFlowTest.cs:157` | ✅ PASS |
| AC8 expected day outside 1..31 → 400 | message | `tests/Validators.Tests/Incomes/Register/RegisterIncomeSourceValidatorTest.cs:57` - `ShouldBe(EXPECTED_DAY_OUT_OF_RANGE)` for 0, 32, -1; boundaries 1 and 31 pass at `:71` | ✅ PASS |
| AC9 Variable with amount/day → 400 | `VARIABLE_SOURCE_HAS_NO_VERSION` | `...RegisterIncomeSourceValidatorTest.cs:85` and `:97` | ✅ PASS |

### INC - Record a payment received

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 payment persisted with its fields | all fields | `tests/UseCases.Test/Incomes/RegisterPayment/RegisterIncomePaymentUseCaseTest.cs:29`, `:68-69` - amount, reference month and payment date asserted | ✅ PASS |
| AC2 Recurring stores the version in effect | that version's Id | `...RegisterIncomePaymentUseCaseTest.cs:28` and `:30` - `result.IncomeSourceVersionId.ShouldBe(source.Versions[0].Id)` and the persisted entity | ✅ PASS |
| AC3 Variable stores null version | null | `...RegisterIncomePaymentUseCaseTest.cs:46-47` - `ShouldBeNull()` on both response and persisted entity | ✅ PASS |
| AC4 source of another user → 404 | NotFoundException | `...RegisterIncomePaymentUseCaseTest.cs:93` | ✅ PASS |
| AC5 archived source → 400 `INCOME_SOURCE_ARCHIVED` | message | `...RegisterIncomePaymentUseCaseTest.cs:113` | ✅ PASS |
| AC6 amount ≤ 0 → 400 | `AMOUNT_GREATER_THAN_ZERO` | `...RegisterIncomePaymentUseCaseTest.cs:148` | ✅ PASS |
| AC7 no version in effect → 400 `NO_VERSION_IN_EFFECT` | message | `...RegisterIncomePaymentUseCaseTest.cs:131` | ✅ PASS |
| AC8 more than one payment per source per month accepted | summed | `tests/UseCases.Test/Incomes/GetMonthly/GetMonthlyIncomeUseCaseTest.cs:72` - `line.ReceivedAmount.ShouldBe(5000m)` from 2000 + 3000 | ✅ PASS |
| AC9 null/empty notes accepted | accepted | not asserted directly | ⚠️ Spec-precision gap - the field is nullable and never rejected, but no test pins it |

### INC - View the income of a given month

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 one line per non-archived source | line count | `...GetMonthlyIncomeUseCaseTest.cs:121` - `result.Lines.Count.ShouldBe(3)`; archived exclusion is enforced in `IncomeSourceRepository.GetForMonth` | ⚠️ Spec-precision gap on archived exclusion - see Edge Cases |
| AC2 Recurring expected amount from the version in effect | that version's amount | `...GetMonthlyIncomeUseCaseTest.cs:27` - `line.ExpectedAmount.ShouldBe(5000m)` | ✅ PASS |
| AC3 Recurring expected day from that version | that version's day | `...GetMonthlyIncomeUseCaseTest.cs:28` - `line.ExpectedDay.ShouldBe(5)` | ✅ PASS |
| AC4 Variable reports null expected | null | `...GetMonthlyIncomeUseCaseTest.cs:86-87` - `ExpectedAmount.ShouldBeNull()`, `ExpectedDay.ShouldBeNull()` | ✅ PASS |
| AC5 payments summed as received | sum | `...GetMonthlyIncomeUseCaseTest.cs:72` | ✅ PASS |
| AC6 no payment → received zero | 0 | `...GetMonthlyIncomeUseCaseTest.cs:43` - `line.ReceivedAmount.ShouldBe(0m)` | ✅ PASS |
| AC7 no payment → `Pending` | Pending | `...GetMonthlyIncomeUseCaseTest.cs:44` - `line.Status.ShouldBe(IncomeStatus.Pending)` | ✅ PASS |
| AC8 received equals expected → `Received` | Received | `...GetMonthlyIncomeUseCaseTest.cs:30` | ✅ PASS |
| AC9 received differs and > 0 → `Divergent` | Divergent | `...GetMonthlyIncomeUseCaseTest.cs:58` | ✅ PASS |
| AC10 month totals returned | sums | `...GetMonthlyIncomeUseCaseTest.cs:122-123` - `TotalExpected.ShouldBe(6200m)`, `TotalReceived.ShouldBe(5800m)` | ✅ PASS |
| AC11 invalid month → 400 `REFERENCE_MONTH_INVALID` | message | `...GetMonthlyIncomeUseCaseTest.cs:217`; end to end at `tests/WebApi.Test/Incomes/IncomeFlowTest.cs:170` - status 400 | ✅ PASS |
| AC12 no source → 200, empty, zero totals | empty + zeros | `...GetMonthlyIncomeUseCaseTest.cs:200-202`; end to end at `tests/WebApi.Test/Incomes/IncomeFlowTest.cs:130` | ✅ PASS |

### INC - Change the value of a recurring income source

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1 old version closed the day before | `ValidityEnd` = new start − 1 | `tests/UseCases.Test/Incomes/ChangeValue/ChangeIncomeSourceValueUseCaseTest.cs:29` - `oldVersion.ValidityEnd.ShouldBe(new DateOnly(2026, 6, 30))` for a start of 2026-07-01 | ✅ PASS |
| AC2 new version carries amount, day, start, null end, reason | all five | `...ChangeIncomeSourceValueUseCaseTest.cs:45-49` and `:51` | ✅ PASS |
| AC3 both writes in one transaction | single `Commit` | one `IUnitOfWork.Commit()` call in `ChangeIncomeSourceValueUseCase.cs:71` | ⚠️ Spec-precision gap - transactionality is structural, not asserted |
| AC4 recorded payments keep their version | unchanged | `tests/WebApi.Test/Incomes/IncomeFlowTest.cs:116-120` - the pre-raise month still reports 3000 after the change | ✅ PASS |
| AC5 empty reason → 400 `CHANGE_REASON_REQUIRED` | message | `...ChangeIncomeSourceValueUseCaseTest.cs:100` | ✅ PASS |
| AC6 start not later → 400 `VALIDITY_START_MUST_BE_LATER` | message | `...ChangeIncomeSourceValueUseCaseTest.cs:83` | ✅ PASS |
| AC7 Variable → 400 `VARIABLE_SOURCE_HAS_NO_VERSION` | message | `...ChangeIncomeSourceValueUseCaseTest.cs:67` | ✅ PASS |
| AC8 source of another user → 404 | NotFoundException | `...ChangeIncomeSourceValueUseCaseTest.cs:124` | ✅ PASS |

**Status**: ⚠️ 41 of 44 ACs matched their spec-defined outcome with `file:line` evidence; 3 spec-precision gaps flagged, none of them a behaviour defect.

---

## Discrimination Sensor

Scratch: temporary git worktree, removed after each run. Baseline `git status --porcelain` was empty
before and after every run; isolation verified.

| Mutation | File | Description | Killed? |
| -------- | ---- | ----------- | ------- |
| 1 | `GetMonthlyIncomeUseCase.cs` | Flipped the Pending guard `receivedAmount == 0m` → `!= 0m` | ✅ Killed (6 failures) |
| 2 | `IncomeSourceExtensions.cs` | `OrderByDescending` → `OrderBy` on `ValidityStart`, picking the oldest overlapping version | ❌ **Survived** → fixed by T20, re-run ✅ Killed (1 failure) |
| 3 | `ChangeIncomeSourceValueUseCase.cs` | `AddDays(-1)` → `AddDays(0)`, closing the old version on the new start | ✅ Killed |
| 4 | `RegisterIncomePaymentUseCase.cs` | Dropped the reference-month normalisation | ✅ Killed |
| 5 | `PersonRepository.cs` | Removed the `UserId` ownership filter from `GetAll` | ✅ Killed |

**Sensor depth**: P0-full (5 mutations) - the feature touches ownership and data integrity.
**Result**: 5/5 killed after the fix task - PASS ✅

The one survivor was a genuine coverage hole: both existing multi-version tests had exactly one
version overlapping the queried month, so the ordering was never exercised. T20 added a mid-month
raise where two versions overlap the same month.

---

## Code Quality

| Principle | Status |
| --------- | ------ |
| Minimum code | ✅ |
| Surgical changes | ✅ |
| No scope creep | ✅ - the `Person` endpoints and the value-change use case were both agreed with the user before implementation |
| Matches patterns | ✅ - repository split, validator per use case, `[FromServices]` use cases, `ExceptionFilter` for status mapping |
| Spec-anchored outcome check | ⚠️ 3 spec-precision gaps flagged rather than silently passed |
| Per-layer Coverage Expectation met | ✅ - every use case has its ownership test; every route has happy + error + 401 |
| Every test maps to a requirement | ✅ |
| Documented guidelines followed | ✅ `.claude/skills/dotnet-usecase-tests/SKILL.md` |

---

## Edge Cases

- [x] Month before every version reports null expected - `GetMonthlyIncomeUseCaseTest.cs:190`
- [x] Month inside a closed version reports that version's amount - `GetMonthlyIncomeUseCaseTest.cs:150-151`
- [ ] **Archived source omitted from the monthly view** - enforced in `IncomeSourceRepository.GetForMonth` (`source.Archived == false`) but NOT covered by a test: no operation in this delivery sets `Archived`, so no test can reach the state through the API. Recorded as a known gap; it becomes testable when the archive operation ships.
- [x] Expected day 31 in a shorter month is reported as stored - no clamping code exists; `ExpectedDay` is copied verbatim from the version
- [ ] Two sources with the same name accepted - no uniqueness constraint exists; not asserted
- [x] Variable source with payments reports Received - `GetMonthlyIncomeUseCaseTest.cs:89`

---

## Gate Check

- **Gate command**: `dotnet build` then `dotnet test`
- **Result**: 82 passed, 0 failed, 0 skipped
  - `Validators.Tests` 13, `UseCases.Test` 38, `WebApi.Test` 31
- **Build**: 0 errors, 0 warnings
- **Test count before feature**: 5
- **Test count after feature**: 82
- **Delta**: +77
- **Skipped tests**: none
- **Failures**: none

---

## Requirement Traceability Update

| Requirement | Previous | New |
| ----------- | -------- | --- |
| CORE-01, CORE-02, CORE-03 | Pending | ✅ Verified |
| PRSN-01, PRSN-02, PRSN-03 | Pending | ✅ Verified |
| INC-01 … INC-13 | Pending | ✅ Verified |

---

## Summary

**Overall**: ✅ Ready

**Spec-anchored check**: 41/44 ACs matched the spec outcome; 3 spec-precision gaps
**Sensor**: 5/5 mutations killed (1 after a fix task)
**Gate**: 82 passed, 0 failed

**What works**: the whole vertical - register a person, register a recurring or variable income
source, record a payment against a reference month decoupled from the payment date, change the value
with a reason, and read any month reconciled into expected versus received with per-source status and
totals. Every endpoint is authorised and every read is scoped to the logged user.

**Issues found**:

1. Archived sources are filtered by the repository but no test reaches that state, because nothing in
   this delivery sets `Archived`. Ships with the archive operation.
2. Transactionality of the value change is structural (one `Commit`) rather than asserted.
3. Null notes on a payment are accepted but not pinned by a test.

**Next steps**: repair the local .NET 10.0.9 install and delete `Directory.Build.targets`; then the
archive operation, which closes gap 1.
