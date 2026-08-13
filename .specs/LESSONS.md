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

## Quarantined (failed when applied - ignore)

A confirmed lesson that recurred alongside failure. Kept for the maintainer to review.

_none_
