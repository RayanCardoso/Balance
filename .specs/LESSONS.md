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

## Quarantined (failed when applied - ignore)

A confirmed lesson that recurred alongside failure. Kept for the maintainer to review.

_none_
