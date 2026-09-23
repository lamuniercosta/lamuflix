# Tasks: Restore five InspectCode warnings

**Input:** [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [quickstart.md](quickstart.md)  
**Branch:** `feature/361-dev-361-spec` (spec only); Phase B uses its task worktree from merged `main`.

## Phase A: Plan challenge before freeze and spec PR `[PHASE A — BLOCKS ALL IMPLEMENTATION]`

- [x] T013 **[PHASE A — BLOCKS ALL IMPLEMENTATION]** [Process] Hand `plan.md` to Sentry, Ledger, and Compass for plan challenge; Keel adjudicates findings and freezes the plan before the spec PR and Gate 1. Frozen 2026-09-23 by Keel: Round 1 Compass 0, Ledger 0, Sentry F1 rejected as freeze blocker (size:M, no mutation gate; `QueryStringBuilder` coverage is a Patron follow-up candidate), F2 resolved (no baseline exception), F3 [assumed].

## Phase 1: Pickup and owner gate

- [ ] T001 [Process] Re-run `/speckit-analyze` against Phase B `origin/main`; compare the five keys and frozen three-file set for drift. On unchanged `origin/main` source, run the gates **sequentially** with `-BaseRef origin/main`: `./scripts/run-jetbrains-inspectcode.ps1 -BaseRef origin/main -Files $files -MinSeverity WARNING`, `./scripts/run-roslyn-analyzers.ps1 -BaseRef origin/main -Files $files`, `./scripts/run-cyclomatic-complexity.ps1 -BaseRef origin/main -Files $files` at the configured Phase 3 threshold 15, then a separate `./scripts/run-cyclomatic-complexity.ps1 -BaseRef origin/main -Files $files -Threshold 6` refactor baseline. Here `$files=@('LamuFlix.Web/Models/Filmes/FilmesViewModel.cs','LamuFlix.Web/TagHelpers/Extensions.cs','LamuFlix.Test/UnitTest1.cs')`. List every method over 6 in those three files with path, line, complexity, and numeric exit; explicitly include `UnitTest1.DynamicQuery` if the analyzer emits it. The Phase A unchanged-source threshold-6 probe returned exit **0** and listed **none**, despite visible branches at `UnitTest1.cs:387-446`; preserve that measured discrepancy rather than infer a failure. Record WARNING+ IDs, paths, lines, and numeric exits. Run read-only `./scripts/run-jetbrains-inspectcode.ps1 -All -MinSeverity SUGGESTION` before restoring the five keys to inventory target IDs solution-wide. Fix any in-scope WARNING+ hit within the three frozen files for AC2 and notify Patron (none in the current pre-restoration baseline); only out-of-scope hits go to the separate InspectCode follow-up.
- [ ] T002 **[OWNER — Gate 1]** Obtain the user's answer to the B1 reversal checkbox in `spec.md`. Until answered, keep `resharper_nullable_warning_suppression_is_used_highlighting` at `suggestion`.
- [ ] T003 [Process] Have Rigger record Patron's DEV-361 ruling and frozen-scope recon comment, the separate size:M DEV-281 InspectCode follow-up covering `Details.cshtml:60` and all 39 solution-wide WARNING+ baseline hits including `Razor.AssemblyNotResolved` [error], and a separate Epic-1 Principle VIII language-migration issue for `FilmesFilterViewModel` and `AssistirFilme_*` (`UnitTest1.cs:479,498,541`) under Patron's DEV-361 analyze C1 ruling in `CONCLUSIONS.md`. Verify each YouTrack read-back before claiming `(verified)`. Check T003 off only after Rigger reports the real YouTrack IDs and verified read-backs for both follow-ups; no ID is assumed here. Neither follow-up is a DEV-361 prerequisite, neither is scheduled into the chain, and neither is an owner checkbox.

## Phase 2: User Story 1 — warnings expose real defects (P1)

**Independent test:** the five values are warning and InspectCode on the frozen three paths exits 0.

- [ ] T004 [US1] Change exactly the five ticket-named inspection keys in `.editorconfig` from `suggestion` to `warning`; leave B1 key unchanged absent user ruling. Rewrite the stale rationale comments at `.editorconfig:69,72,74` so they no longer describe the restored keys as out of scope (wording per `ASSUMPTIONS.md`). After restoration, run the read-only `./scripts/run-jetbrains-inspectcode.ps1 -All -MinSeverity WARNING` inventory with numeric exit and path/line/ID list; route only out-of-scope hits to DEV-281.
- [ ] T005 [US1] In `LamuFlix.Web/TagHelpers/Extensions.cs`, replace default `KeyValuePair` equality at lines 43 and 56 with `StringComparison.OrdinalIgnoreCase` for keys and ordinal comparison for values under the existing match guard; do not change pagination null-value behavior. `SingleOrDefault` already throws on multiple case-insensitive matches, making the chosen key comparer equivalent for reachable guarded matches.
- [ ] T006 [US1] In `LamuFlix.Test/UnitTest1.cs`, resolve five condition diagnostic hits across four `if` sites at lines 61, 72, 101, and 112 (two IDs on line 112). Simplify the four dead null branches in `TestMethod1` and `TestMethod2`, quoting the test names in the change receipt.
- [ ] T007 [US1] Inspect the two unused-setter and one collection target findings in `FilmesFilterViewModel` at `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs:14,16,20`; search for in-product assignments and verify the `[FromQuery]` MVC model binder writer before any fix or suppression.

## Phase 3: User Story 2 — external writers remain supported (P2)

**Independent test:** bindable/deserializable properties remain present, and each remaining suppression is local with a reason.

- [ ] T008 [US2] For proven external-writer false positives in `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs`, add only a type/member scoped InspectCode suppression bracket with a one-line binder/deserializer reason. Do not use a file-scoped `.editorconfig` section or `[SuppressMessage]`.
- [ ] T009 [US2] Fix any genuine target defect minimally within `FilmesViewModel.cs`; do not delete a binder-visible property or introduce a package.
- [ ] T010 [US2] Run `./scripts/run-jetbrains-inspectcode.ps1 -BaseRef origin/main -Files 'LamuFlix.Web/Models/Filmes/FilmesViewModel.cs','LamuFlix.Web/TagHelpers/Extensions.cs','LamuFlix.Test/UnitTest1.cs'` at default WARNING. Resolve every WARNING+ finding in these three files for AC2, notify Patron, and record exit 0. Reconcile the post-restoration `-All -MinSeverity WARNING` inventory from T004; only out-of-scope hits go to the separate InspectCode follow-up.

## Phase 4: Verification and review handoff

- [ ] T011 Check the diff contains only `.editorconfig` plus the three frozen source files, with the B1 key unchanged unless the owner directed otherwise; confirm no new package reference.
- [ ] T012 Run gates sequentially with `$files` as in T001: `./scripts/run-roslyn-analyzers.ps1 -BaseRef origin/main -Files $files`, `./scripts/run-cyclomatic-complexity.ps1 -BaseRef origin/main -Files $files` at configured threshold 15, `./scripts/run-cyclomatic-complexity.ps1 -BaseRef origin/main -Files $files -Threshold 6` at the refactor gate for DEV-361-changed methods, `./scripts/run-jetbrains-inspectcode.ps1 -BaseRef origin/main -Files $files -MinSeverity WARNING`, `dotnet format --verify-no-changes`, and `dotnet test`. Record numeric exits and diagnostic evidence; exit 2 is not a pass. The solution-wide `-All -Threshold 6` diagnostic on unchanged `origin/main` (T013 F2 rerun) exited **1** listing only `LamuFlix.Web/Extensions/EntityExtensions.cs:13 DynamicQuery` (12), `ViewModelExtensions.cs:8 HasQuery` (8) and `ViewModelExtensions.cs:36 HasPropertyValue` (7), none in the frozen paths; CA1502 does not emit `UnitTest1.DynamicQuery`. No baseline exception is granted: if the three-file threshold-6 run exits non-zero in Phase B, stop and report `blocked` to Patron with the emitted method, complexity and numeric exit. Do not refactor unchanged methods, suppress CA1502, or alter thresholds in DEV-361. List wider behavior/package findings for Patron follow-up without adding them to the chain.

## Dependencies and execution order

T013 is Phase A and blocks all implementation: its plan challenge and freeze happen before T001 and before all Phase B work, and precede the spec PR and Gate 1. Task IDs are labels, not execution order. T001–T003 precede Phase B build work. T004 enables target findings, then T005–T009 may be handled by their distinct files. T010–T012 follow local fixes.

## Implementation strategy

Keep the B1 owner question isolated from the five authorized restorations. Use the measured three-file scope; no package, public API, schema, or extra file is implied by a warning.
