# Tasks: Close DEV-289 leftovers

**Input:** [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [quickstart.md](quickstart.md) (canonical verification procedure)  
**Branch:** `feature/360-dev-360-spec` (spec only); Phase B uses its own task worktree from merged `main`.

## Phase A: Analysis, plan challenge, freeze, and Gate 1

- [x] T001 Run `/speckit-analyze` read-only on this ticket's Phase A artifacts. Persist the report byte-for-byte as `analyze.md` and record its SHA256. It must show Critical 0 and High 0. Sentry, Ledger, and Compass then challenge the plan, and Keel adjudicates (`task-DEV-360:93`).
- [x] T001a After every adjudication edit, rerun the read-only analysis, persist `analyze.md` again, and record the new SHA256. The plan is frozen only on a persisted report with Critical 0 and High 0 that is newer than every artifact edit. The challenge is capped at two rounds. If Round 2 still leaves a blocking finding, stop and report to Patron; there is no third round.
- [x] T001b Post the challenge findings and Keel's adjudication summary as comments on the spec PR. Reply to each fixed finding with its fix and resolve the thread. The review round is not finished until it is on the PR. Depends on T001a.
- [x] T002 **[OWNER]** Owner answers D1–D3 recorded 2026-09-23 (`task-DEV-360:81-84`); see [spec.md](spec.md) "Gate 1 and authorization envelope".
- [x] T003 Rigger recorded Patron's Q1 clarification on DEV-360 with verified read-back (`chain:7`).
- [x] T004 Rigger recorded the D2 deferral as comments on DEV-280 and DEV-360, with verified read-back (`task-DEV-360:90`). Rigger also applied Patron's H1 AC4 correction (exactly one pending `Status` AddColumn, no migration) and the Compass F2 comment recording D1–D3 and the AC3 deferral, both read back and verified (`task-DEV-360:91,93`).

## Phase B1: Pickup

- [x] T005 Re-check drift against Phase B `main`: DEV-280/290 overlap, the EF Core patch in `Directory.Packages.props`, and cited line numbers. Run the Roslyn, cyclomatic-complexity (threshold 15 and `-Threshold 6`), and InspectCode gates with pwsh 7 on Phase B `main` over the gate-baseline set listed in `research.md`. Record the numeric exits in the PR "Verification evidence" section and in `research.md` (Patron G1). Re-check the out-of-set model consumers listed in `research.md` for FR-004 nullability impact. If drift shows a new pre-existing method over 6 in a touched file, record it as a PR baseline exception and notify Patron. A pre-existing method over 15 stops DEV-360 and is reported to Patron (Patron G2). Update this task list only if drift is recorded. Phase B addenda to `research.md` (the T005 baseline) and `tasks.md` (drift) are evidence records on the task branch; they do not reopen the frozen plan or Gate 1. If DEV-280 or DEV-290 has already landed, route the overlap comment through Patron and Rigger (spec Edge Cases; `task-DEV-360:18,24,38`).

## Phase B2: Design-time EF tooling (owner decision D3; precondition for US1 and US3)

- [x] T006 Run `dotnet add LamuFlix.Data package Microsoft.EntityFrameworkCore.Design --version 9.0.<p>` (constitution:374-375) so that `Directory.Packages.props` gets `<PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.<p>" />`, where `<p>` is the `Microsoft.EntityFrameworkCore` patch (currently 9.0.0). Then set the `LamuFlix.Data/LamuFlix.Data.csproj` reference metadata: `PrivateAssets` `all`, `IncludeAssets` `runtime; build; native; contentfiles; analyzers; buildtransitive`, and `Publish` `true`. Add `dotnet-ef` `9.0.<p>` with `rollForward: false` to `.config/dotnet-tools.json`. No other package or tool.
- [x] T007 Run [quickstart.md](quickstart.md) §1 and record every native exit and version, including the pinned tool's target framework. Any failure blocks T010 and T014. Fix it within T006; if the pinned 9.0.x tool or its version does not work, stop and report to Patron. D3 is not reopened and there is no fallback to the global `dotnet-ef` 10.0.5 (Patron SF-3). Then, before T013, run quickstart §5 as a baseline and record the full list of pending operations (Patron SF-1).

## Phase B3: User Story 1 — explicit configuration failures (P1)

- [x] T008 [US1] Remove the hardcoded fallback in `LamuFlix.Data/LamuFlixContext.cs`: throw `InvalidOperationException` naming `LAMUFLIX_CONNECTION` when it is unset and options are not configured. Keep the configured-options behavior.
- [x] T009 [US1] In `LamuFlix.Test/UnitTest1.cs`, remove the literal fallback and scope the `LAMUFLIX_TEST_CONNECTION` guard to the DB-dependent path. Resolve the context lazily on first DB access, not eagerly in `[TestInitialize]` (`UnitTest1.cs:32-44`). The guard fires then, before any file-system access, so the three live `AssistirFilme_*` in-memory tests stay runnable (Patron SF-5). Edit only the DB setup/guard path.
- [x] T010 [US1] Record [quickstart.md](quickstart.md) §2 and §3, then scan `LamuFlix.Data` and `LamuFlix.Test` for connection-string, user id, and password literals. Depends on T007–T009.

## Phase B4: User Story 2 — observable missing deletes (P1)

- [x] T011 [US2] In `LamuFlix.Data/Repositories/GenericRepository.cs`, throw `KeyNotFoundException` naming the entity type and id when `Find` returns null (message wording per `ASSUMPTIONS.md`).
- [x] T012 [US2] Confirm that no in-repo `.Delete(` caller relies on the silent return, and record [quickstart.md](quickstart.md) §4. Add no test (D2); the PR cites T004's DEV-280 comment.

## Phase B5: User Story 3 — model and gates agree (P2)

- [x] T013 [US3] Compare CLR properties of the entities mapped in `LamuFlix.Data/Migrations/LamuFlixContextModelSnapshot.cs` with `LamuFlix.Data/Models/*.cs`. Annotate nullable columns and navigations per Patron Q2. `Player.cs` is included unless pickup proves its conditional skip. `Temp.cs` and `MovieEnrichmentMessage.cs` are unmapped and excluded (spec Edge Cases). Do not edit migrations.
- [x] T013a [US3] Apply only behavior-preserving nullability fixes to consumers that T013 breaks under `TreatWarningsAsErrors`, limited to `LamuFlix.Web/Services/FilmesServices.cs` and `LamuFlix.Test/UnitTest1.cs`. Unmapped `Temp.cs` and `MovieEnrichmentMessage.cs` are never edited. Preserve returned values and public shape, and use minimal `!` or guard edits. Leave the `ProcessStarter` member unchanged (D1). Rename only identifiers this task edits or whose signatures it changes (Patron H2/M2, `task-DEV-360:89`); the file-wide Portuguese vocabulary is a DEV-290/Epic 1 follow-up. If a consumer outside these two files needs an edit (for example in `LamuFlix.Work`), stop and report to Patron (Patron SF-4). Depends on T013.
- [ ] T014 [US3] **STOPPED 2026-09-23:** probe `Up` still has `AlterColumn` `Id` IdentityColumn on actor, collection, director, genre, movie, and player, plus `AddColumn` `Status`. SC-003 is not met. Probe discarded. See `research.md` Phase B pickup evidence. Run [quickstart.md](quickstart.md) §5. Record the pending-changes result and a probe `Up` containing exactly one operation, `AddColumn` `Status`. Discard the probe so that `LamuFlix.Data/Migrations` has no diff (Patron H1). If the probe shows any operation other than `AddColumn` `Status`, stop and report to Patron with the T007 baseline; add no migration (Patron SF-1). Depends on T007, T013, and T013a.
- [x] T015 [US3] Set `harness.yml` `warningsAsErrors: true` and verify it matches `Directory.Build.props` `TreatWarningsAsErrors=true`.
- [ ] T016 [US3] Have Ledger review the `specs/DEV-289/tasks.md` file set as it is on Phase B `main`, using the Standards brief (`.claude/skills/code-review/standards-brief.md`), smell baseline (`.claude/skills/code-review/smell-baseline.md`), and vendor rules. Record concrete `file:line` findings.
- [ ] T017 [US3] Before editing a DEV-289-set `.cs` file outside the gate-baseline set, run the three gates on it as it is on `main` (complexity at 15 and at `-Threshold 6`, with pwsh 7). Record the exits as a T005 addendum in `research.md`, so that T018 can apply the G1/G2 ladder. Fix only Q3-local annotation/analyzer findings within that set and budget. Route other findings to Patron for follow-up, then quote the ticket ids on the PR. Schedule no new chain work. Closing bar (Patron Q4, `CONCLUSIONS.md`): every in-scope Critical or High finding with a concrete failure scenario is fixed and verified before T018. If one cannot be fixed within scope, stop and report to Patron; routing it alone does not close it.

## Phase B6: Verification and review handoff

- [ ] T018 Run the pipeline gates in `task-pipeline:65-75`: Roslyn analyzers, cyclomatic complexity at 15 and the refactor gate `run-cyclomatic-complexity.ps1 -Threshold 6` over the whole changed `.cs` files (Patron G2; constitution:338), InspectCode, property tests, vulnerable packages (including the T006 package), `dotnet format --verify-no-changes`, `dotnet test`, web gates, and gitleaks. Record each numeric exit. This ticket adds no domain invariant, so record `propertyTests: opt-out — no new domain invariant in DEV-360` in its task note before accepting property-test exit 2; without that note, exit 2 blocks. The opt-out covers only the property-test gate; `dotnet test` and every other gate stay required (Patron SF-6). Web gates cover the `web/` frontend, not the C# `LamuFlix.Web` project; with no `web/` diff, web-gate exit 2 is `SKIPPED`, not PASS (Patron SF-7). Any other skipped or unrunnable gate blocks. Run the gates with pwsh 7. Gate findings in touched files follow Patron's G1 ladder:
  - (a) introduced by the DEV-360 diff: fix it;
  - (b) pre-existing and Q3-local: fix it under T017;
  - (c) pre-existing and not Q3-local: a targeted, justified Roslyn/InspectCode suppression only (constitution:345-347), recorded with `file:line`, a one-line justification, and a recording reference. Never refactor beyond the ticket.

  CA1502 complexity is never suppressed (constitution:344-345). A DEV-360-created method over 6 is fixed. A pre-existing method over 6 is recorded as a PR baseline exception with its numeric exit (Patron G2; DEV-361 precedent). A pre-existing method over 15 in a touched file stops DEV-360 and is reported to Patron; the Phase A baseline has none. The pre-existing InspectCode warning at `MovieEnrichmentMessage.cs:1` is outside the boundary and is tracked by DEV-366; cite DEV-366 in the PR evidence, and do not fix, suppress, or file it.
- [ ] T019 Check `git diff origin/main...HEAD` against the plan's file impact boundary, including its allowance for evidence-only edits to `specs/DEV-360/research.md` and `tasks.md`. Confirm the `ProcessStarter` member is unchanged (D1), `FilmesServices.cs` has only T013a nullability fixes and T017 Q3-local fixes (each recorded by `file:line`), there is no migration diff, and there is no test project or package (D2).

## Dependencies and execution order

T001, T001a, and T001b complete in Phase A before Gate 1 (at most two challenge rounds); T002–T004 are done. After the user merges that PR, T005 is the Phase B pickup check. T006–T007 precede every EF design-time run (T010 §2 and T014); T007's §5 baseline precedes T013. T008–T009 precede T010. T011 precedes T012. T013 precedes T013a, and both precede T014. T016 precedes T017, and T017's closing bar precedes T018. T018–T019 follow all applicable work. No production task runs in the spec sprint.

## Implementation strategy

Land the tooling gate first, then the two P1 behaviors, then model/gate alignment and the Standards review. Do not reduce scope based on predicted later tickets. Any other new package, schema, public API, or unnamed file rewrite is a structural owner question.
