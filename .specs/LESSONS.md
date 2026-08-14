# LESSONS - auto-maintained by scripts/lessons.py

> Machine-owned. Do NOT hand-edit. Changes are overwritten on the next `lessons.py` write.
> Canonical state lives in `.specs/lessons.json`. Edit lessons only via the script.
> promote_threshold=2 distinct features · window_days=45 · quarantine_threshold=2

## Confirmed (load these at Specify/Design)

Corroborated across multiple features. Safe to apply as guidance.

_none_

## Candidates (under observation - do NOT load as guidance yet)

Seen once or not yet corroborated. Tracked, not trusted.

### L-001 - When a selector picks one row out of an overlapping set, add a fixture where two rows genuinely overlap; a single-match fixture never exercises the ordering.
- signal: `surviving_mutant` · recurrence: 1 feature(s) · scope: `domain/date-ranges` · harmful: 0
- features: income-tracking
- evidence: IncomeSourceExtensions.cs VersionInEffect / mutant 2 (domain/date-ranges)
- last seen: 2026-08-10T23:28:52Z

### L-002 - Do not specify behaviour for a state no shipped operation can produce; either include the operation that sets it or record the rule as untestable until it ships.
- signal: `spec_precision_gap` · recurrence: 1 feature(s) · scope: `spec/state-flags` · harmful: 0
- features: income-tracking
- evidence: spec.md Edge Cases / archived source (spec/state-flags)
- last seen: 2026-08-10T23:28:52Z

### L-003 - Assert one field per clause of a conjunctive acceptance criterion; a criterion naming two outputs needs two assertions.
- signal: `ac_gap` · recurrence: 1 feature(s) · scope: `read-model` · harmful: 0
- features: expense-tracking
- evidence: spec.md VIEW AC3 (due-day clause); validation.md Spec-Anchored AC table (read-model)
- last seen: 2026-08-13T00:29:46Z

### L-004 - Assert every pass-through field a projection response copies from its entity; totals and status assertions do not cover them.
- signal: `surviving_mutant` · recurrence: 1 feature(s) · scope: `read-model` · harmful: 0
- features: expense-tracking
- evidence: M11 - GetMonthlyExpenseUseCase.cs:90 DueDay=0 survived both suites (read-model)
- last seen: 2026-08-13T00:29:46Z

### L-005 - Give every branch of a status resolution rule its own fixture, including the null-input branch.
- signal: `surviving_mutant` · recurrence: 1 feature(s) · scope: `use-case` · harmful: 0
- features: expense-tracking
- evidence: M9 - GetMonthlyExpenseUseCase.cs:118 null-expected branch survived both suites (use-case)
- last seen: 2026-08-13T00:29:47Z

### L-006 - When the design adds a branch the spec does not define, pin its outcome in the spec before implementing it.
- signal: `spec_precision_gap` · recurrence: 1 feature(s) · scope: `spec` · harmful: 0
- features: expense-tracking
- evidence: design.md status rule step 2 vs spec.md VIEW AC5-AC6 (spec)
- last seen: 2026-08-13T00:29:47Z

### L-007 - Give every entity its own NOT_FOUND message key instead of reusing a neighbouring entity's.
- signal: `spec_deviation` · recurrence: 1 feature(s) · scope: `errors` · harmful: 0
- features: expense-tracking
- evidence: SPEC_DEVIATION in RegisterExpenseUseCase.cs, ChangeRecurringExpenseValueUseCase.cs, UpdateRecurringExpensePaymentUseCase.cs (T18, T28, T35) (errors)
- last seen: 2026-08-13T00:29:48Z

### L-008 - Pin the midpoint rounding mode in the spec whenever a total is split and rounded across rows.
- signal: `spec_precision_gap` · recurrence: 1 feature(s) · scope: `money` · harmful: 0
- features: expense-tracking
- evidence: spec.md Assumptions table - installment midpoint rounding; T22 (money)
- last seen: 2026-08-13T00:29:49Z

### L-009 - Pin a non-default container host port in docker-compose when local database services already hold the defaults.
- signal: `spec_deviation` · recurrence: 1 feature(s) · scope: `environment` · harmful: 0
- features: expense-tracking
- evidence: tasks.md T47 SPEC_DEVIATION; docker-compose.yml host port 5434 (environment)
- last seen: 2026-08-13T00:29:49Z

### L-010 - Pin a rounding mode with an exact-midpoint input and a literal expected value; recomputing the expectation with the implementation's own rounding call asserts nothing.
- signal: `surviving_mutant` · recurrence: 1 feature(s) · scope: `money` · harmful: 0
- features: expense-tracking
- evidence: validation.md sensor M12 - RegisterInstallmentPlanUseCase.cs:122 (AwayFromZero -> ToEven survived all 348 tests) (money)
- last seen: 2026-08-13T01:10:36Z

### L-011 - Pin the accepted input grammar in the spec for any value the UI also formats for display; a parser that rejects the formatter's own output rejects what the user just read.
- signal: `spec_precision_gap` · recurrence: 1 feature(s) · scope: `money` · harmful: 0
- features: balance-mobile-app
- evidence: validation.md SPG-1; src/shared/lib/money.ts:16 formatMoney vs money.ts:28 parseMoneyInput (money)
- last seen: 2026-08-14T15:02:34Z

### L-012 - Route a failed client-side parse to a visible outcome; mapping it to the field's absent value silently discards what the user typed.
- signal: `spec_precision_gap` · recurrence: 1 feature(s) · scope: `forms` · harmful: 0
- features: balance-mobile-app
- evidence: validation.md SPG-2; src/features/catalogue/ui/AccountsScreen.tsx:56 (forms)
- last seen: 2026-08-14T15:02:34Z

### L-013 - Assert order-dependent and constraint-dependent behaviour against the real database engine; an in-memory test provider guarantees neither ordering nor unique constraints.
- signal: `ac_gap` · recurrence: 1 feature(s) · scope: `persistence` · harmful: 0
- features: balance-mobile-app
- evidence: validation.md VFF-1; backend commit 3f44760; RecurringExpenseRepository.cs:29,36,44,56 (persistence)
- last seen: 2026-08-14T15:02:35Z

## Quarantined (failed when applied - ignore)

A confirmed lesson that recurred alongside failure. Kept for the maintainer to review.

_none_
