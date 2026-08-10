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

---

## Handoff

**Last updated:** 2026-08-10
**Branch:** `main`
**Feature in flight:** `income-tracking`

**Where things stand:** `income-tracking` is complete and verified. All 20 tasks are done, 82 tests
pass, and `validation.md` records a PASS with 5/5 sensor mutations killed. 22 local commits sit ahead
of `origin/main` - `git push` was explicitly NOT authorised for this run, so pushing is the user's
next action.

**Known gaps carried forward:** the archive operation for an income source (its filter exists and is
honoured, but nothing sets `Archived`); update and delete for `Person`; correcting a recorded payment.

**Environment notes:**

- The local ASP.NET Core 10.0.9 install is corrupt (73 of 140 ref-pack DLLs and 22 of 143 shared-runtime
  DLLs are zero-filled). `Directory.Build.targets` pins build and run to the intact 10.0.8 and is
  gitignored. Delete it after repairing .NET.
- Python lives at `%LOCALAPPDATA%\Programs\Python\Python313\python.exe`. The `python3` name on PATH is
  the Microsoft Store stub and does not work - invoke the skill's validators with that full path.
- Docker is installed but its daemon is not running, so PostgreSQL cannot be started. Integration tests
  run on the EF Core in-memory provider through `CustomWebApplicationFactory`.
