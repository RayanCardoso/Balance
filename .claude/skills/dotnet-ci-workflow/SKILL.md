---
name: dotnet-ci-workflow
description: Use when a .NET solution has no automated build or test pipeline and needs one - adding a GitHub Actions workflow that restores, builds, runs xUnit tests against a PostgreSQL service container, and enforces architecture rules on push and pull request.
---

# Add a GitHub Actions pipeline

## Overview

One workflow that builds, tests against a real PostgreSQL container, and runs the architecture
check on every push and pull request.

**Core principle: CI runs what a reviewer cannot.** Its job is the checks a human skips —
a clean build from scratch, the full test suite, and the structural rules.

## When to Use

- "Add CI", "set up GitHub Actions", "run tests automatically"
- A .NET solution with no `.github/workflows`

**When NOT to use:**
- A workflow already exists → edit it rather than adding a second
- The repo is not on GitHub — the concepts port, this YAML does not

## Procedure

### Step 1 — Check what exists

Look for `.github/workflows/*.yml`. If a workflow is already there, read it and extend it
instead of adding a competing one.

### Step 2 — Write the workflow

Copy `assets/dotnet-ci.yml` to `.github/workflows/ci.yml`, replacing `__PROJECT_NAME__` with
the solution name.

Two parts matter and are easy to get wrong:

- **The health check on the `postgres` service.** Without `--health-cmd pg_isready`, the job
  starts the test step while the database is still booting, and the failure looks like a random
  connection error roughly one run in five.
- **The `env` block on the test step.** Configuration comes through double-underscore
  environment variables (`Settings__Jwt__SigningKey`), which is how .NET maps them onto nested
  configuration keys. Integration tests that use the in-memory provider still need
  `SigningKey`, because token generation is registered at startup regardless.

### Step 3 — Confirm the SDK version

`dotnet-version: '10.0.x'` must match the solution's `TargetFramework`. If the repo pins an SDK
through `global.json`, the workflow must not contradict it.

### Step 4 — Verify

Do not claim the pipeline works because the YAML is written. Either:

```bash
gh workflow run ci.yml
gh run watch
```

or push the branch and report the observed conclusion. A workflow that has never run is an
untested change.

## Secrets

Never put a real signing key or connection string in the workflow file — it is committed and
public in a public repo. The values in the template are throwaway values scoped to the CI job.
For anything real, use `${{ secrets.NAME }}` and tell the user which repository secrets to
create; do not create them yourself.

## Common Mistakes

| Mistake | Consequence |
| --- | --- |
| No health check on the postgres service | Intermittent connection failures that look like flaky tests |
| Single underscore in `Settings_Jwt_SigningKey` | Silently ignored; token generation throws at runtime |
| `dotnet test` without `--no-build` after building | Rebuilds everything, doubling job time |
| A real secret committed in the YAML | Permanently leaked; rotation is the only fix |
| SDK version not matching `TargetFramework` | Restore fails with an unhelpful NuGet error |
| Reporting success from an unrun workflow | The most common failure of this skill |

## Related Skills

- `dotnet-arch-guard` — the check the last step runs
- `dotnet-clean-arch-bootstrap` — the solution layout this workflow assumes
