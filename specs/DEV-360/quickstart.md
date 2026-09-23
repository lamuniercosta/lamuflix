# DEV-360 verification scenarios

This file is the canonical verification procedure. `spec.md`, `plan.md`, and `tasks.md` reference its numbered sections rather than restating them. Every recorded result goes in the PR body under "Verification evidence" (`ASSUMPTIONS.md`).

## 1. EF design-time tooling gate (precondition for §2 and §5)

Run from the repo root after T006 lands the owner-authorized tooling (`spec.md` Gate 1 decision D3). Record every native exit; any non-zero exit or mismatch blocks §2 and §5.

1. `dotnet tool restore` → exit 0.
2. `dotnet --list-runtimes` lists the `Microsoft.NETCore.App` runtime the pinned tool targets. A runtime of the same major version as the pinned `dotnet-ef` 9.0.x package's target framework is required, and T007 records that framework; the tool keeps `rollForward: false`. If the runtime is missing, block to Patron; do not enable roll-forward. Then `dotnet tool run dotnet-ef --version` → reports `9.0.<p>`, where `<p>` equals the `Microsoft.EntityFrameworkCore` patch in `Directory.Packages.props` (currently `9.0.0`). The global `dotnet-ef` 10.0.5 is not used; every EF command below runs as `dotnet tool run dotnet-ef`.
3. `dotnet restore LamuFlix.Data` → exit 0. This supplies `project.assets.json`; without it EF fails with NETSDK1004 (analysis U1, observed).
4. `dotnet list LamuFlix.Data package` → lists `Microsoft.EntityFrameworkCore.Design` at the same `9.0.<p>`.
5. `dotnet list LamuFlix.Web package --include-transitive` and `dotnet list LamuFlix.Work package --include-transitive` → neither lists `Microsoft.EntityFrameworkCore.Design` (proves `PrivateAssets=all`: design-time only, not a runtime dependency of the hosts).
6. `dotnet build LamuFlix.Web` and `dotnet build LamuFlix.Work` → exit 0, and no `Microsoft.EntityFrameworkCore.Design.dll` exists under `LamuFlix.Web/bin` or `LamuFlix.Work/bin`. `Publish=true` copies it only to `LamuFlix.Data`'s own output. If it reaches a host output, block to Patron.

## 2. Context missing-variable outcome (FR-002, SC-001)

Unset `LAMUFLIX_CONNECTION` in the process. Run `dotnet tool run dotnet-ef migrations has-pending-model-changes --project LamuFlix.Data --startup-project LamuFlix.Data --context LamuFlixContext`. Record the non-zero native exit and the `InvalidOperationException` text naming `LAMUFLIX_CONNECTION`. A tooling error (NETSDK1004, "doesn't reference Microsoft.EntityFrameworkCore.Design", assembly-load failure) is not this evidence; fix §1 and rerun.

## 3. Test-setup missing-variable outcome (FR-002)

Inspect T009's guard in `UnitTest1.cs`: it runs only on the DB-dependent path and fires on first context access, before any file-system access. Unset `LAMUFLIX_TEST_CONNECTION`; temporarily lift `[Ignore]` from the English-named `TestMethod1`; run only that test locally; record MSTest Inconclusive naming the variable; restore `[Ignore]` and verify the diff has no `[Ignore]` or test-method change beyond T009's setup/guard edit. Run the three existing `AssistirFilme_*` tests, which do not require a MySQL server; they execute and pass. Their pre-existing EF Core InMemory use is legacy, not an authorization for new InMemory tests. A committed `dotnet test` result is not claimed as proof of §2 or §3.

## 4. Delete not-found behavior (FR-003)

Read back `GenericRepository.Delete(object id)`: when `Find(id)` is null it throws `KeyNotFoundException` naming the entity type and id. Per owner decision D2, DEV-360 adds no test for this path; the required test is deferred to DEV-280. Record the deferral on the PR. Add no test project, test package, MSTest test, or EF Core InMemory test.

## 5. Pending-model check (FR-004, SC-003; AC4 as corrected by Patron H1)

The model maps `Movie.Status` (`Movie.cs:21`; `LamuFlixContext.cs:54`), but no migration or snapshot contains it. So after DEV-360 exactly one pending operation is expected: the pre-existing `AddColumn` for `Status` on the movie table. No migration is committed.

1. Set `LAMUFLIX_CONNECTION` to a process-only, non-secret dummy MySQL design-time value supplied outside the repo. Commit no connection-string value.
2. Rerun the §2 command. Record its native exit and output. Expect the "changes have been made to the model" pending-changes result, not a tooling error or the §2 missing-variable exception. The §2 failure is FR-002 evidence, never model-check evidence.
3. List the pending operations with a throwaway probe: `dotnet tool run dotnet-ef migrations add Dev360PendingProbe --project LamuFlix.Data --startup-project LamuFlix.Data --context LamuFlixContext`. Record the exit (0). The generated `Up` must contain exactly one operation, `AddColumn` named `Status` on the movie table, and nothing else. That column is non-nullable and keeps its `Pending` default (`LamuFlixContext.cs:54`). Any other operation (for example an `AlterColumn` left by nullability drift) fails SC-003.
4. Discard the probe: delete exactly the two generated `*_Dev360PendingProbe*.cs` files, then `git restore LamuFlix.Data/Migrations/LamuFlixContextModelSnapshot.cs`. Do not use `migrations remove`, which needs a live database. `git status --short LamuFlix.Data/Migrations` must be empty.

## 6. Configuration, review, and gates (FR-005–FR-008)

Read `harness.yml` and `Directory.Build.props`; both warning settings are true. Record Ledger's Standards findings, local fixes, and follow-up ticket ids. Run gitleaks and the pipeline gates with numeric exits. Property-test exit 2 requires the task note's `propertyTests: opt-out` reason; with no `/web` diff, web-gate exit 2 is reported as `SKIPPED`, not PASS.
