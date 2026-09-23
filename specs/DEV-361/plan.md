# Implementation Plan: Restore five InspectCode warnings

**Branch:** `feature/361-dev-361-spec` | **Date:** 2026-09-23 | **Spec:** [spec.md](spec.md)

## Summary

Restore exactly five ticket-named `.editorconfig` severities to `warning`, resolve their measured ten hits in three files, and run InspectCode on those paths. Keep the separate `NullableWarningSuppressionIsUsed` B1 reversal at `suggestion` until the owner answers. This Phase A artifact authorizes no production edit.

## Technical Context

**Language/Version:** C# / .NET 10; `.editorconfig` InspectCode settings.  
**Primary Dependencies:** Existing JetBrains InspectCode tool; no new package or annotation dependency.  
**Storage:** N/A; no schema/migration.  
**Testing:** WARNING+ InspectCode on the frozen three paths; Roslyn, CA1502, format, and solution tests in Phase B.  
**Project Type:** Existing flat Web/Test solution.  
**Constraints:** Code fixes limited to three probed files; remaining suppressions per type/member with reason; no B1 reversal without owner answer.

## Constitution Check

| Principle | Application | Status |
|---|---|---|
| VIII — Ubiquitous Language in English (`constitution.md:217-232`); Known Technical Debt (`constitution.md:421-429`) | VIII requires Portuguese identifiers to be renamed on contact, while the transitional-debt clause bars **new** Portuguese identifiers in the current Web/Test layout. For DEV-361's suppression/minimal-edit contact, preserve existing identifiers and add none. Rigger records a separate Epic-1 language migration issue under T003 per Patron's DEV-361 analyze C1 ruling in `CONCLUSIONS.md`; it is not added to the chain. | Conflict reconciled by Patron's C1 ruling; follow-up outside ticket |
| IX — Test Pyramid (`constitution.md:237-264`); Known Technical Debt (`constitution.md:421-429`) | Simplify dead conditions in existing MSTest tests; add no MSTest tests. Do not skip AC2 based on future DEV-280. Mutation: Stryker break 80 applies to `LamuFlix.Core` (`constitution.md:246`) and `/architect` runs on L tickets only (`task-pipeline:66`); DEV-361 is size:M, so no mutation gate applies. `QueryStringBuilder` coverage stays a Patron follow-up candidate (new MSTest barred by `constitution.md:427-429`; an xUnit project is §2.3 structural). | Pass in plan |
| Development Workflow — Pull Request Quality Gates / Static-Analysis Gates (`constitution.md:301-349`) | Restore five real warning severities, retain MVC-bound properties, and use targeted justified ReSharper suppressions only for proven false positives. Run all required gates on modified C# files. | Pass in plan; Phase B gates pending |
| Charter §2.3 (outside constitution) | No new package; B1 checkbox remains open; no code outside frozen flagged paths. | Gate 1 blocked |

## Phase 0: Research

The SUGGESTION probe on ten candidate files exited 1 with 162 issues overall. Five target IDs account for ten hits at nine distinct locations in only three files: test line 112 carries two IDs. A later solution-wide probe found one additional target-ID WARNING hit in `Views/Filmes/Details.cshtml:60`, outside this ticket's frozen edit set; record it for Patron follow-up. [research.md](research.md) records the counts and baselines. These inventories are not passing gates. The Phase B pickup rechecks drift against `main` before edits.

## Phase 1: Design

No new data model or contract. [quickstart.md](quickstart.md) defines the gate and behavior checks. For MVC binder false positives in the three measured model properties, verify the external writer and use a member/type scoped InspectCode suppression with a one-line reason. For real cases, make minimal code fixes. Preserve `TagHelpers` null-value guard and simplify dead test branches. The existing B1 key stays unchanged.

## Implementation sequence

1. At Phase B pickup, compare `origin/main` with this frozen set and run `/speckit-analyze`. On unchanged source, collect three-file WARNING+ InspectCode, Roslyn, configured CA1502 threshold-15 and explicit refactor threshold-6 baselines, running gates sequentially with `-BaseRef origin/main`. Inventory target IDs across the solution at SUGGESTION before changing severities. Record the separate size:M DEV-281 InspectCode follow-up covering `Details.cshtml:60` and all 39 solution-wide WARNING+ baseline hits, including `Razor.AssemblyNotResolved` [error], without adding it to the chain.
2. Set the five ticket-named `.editorconfig` keys to `warning`; leave B1 key at `suggestion` pending user ruling.
3. Fix `TagHelpers/Extensions.cs:43,56` with ordinal-ignore-case key and ordinal value comparison under the existing guard. The guarded `SingleOrDefault` throws on multiple case-insensitive key matches, so reachable key identity is unchanged.
4. Simplify four condition sites in `UnitTest1.TestMethod1` and `TestMethod2` without changing their remaining predicate.
5. Inspect the three `FilmesViewModel.cs` target findings: fix genuine unused members; suppress only proven external binder/serializer writers at type/member scope with reasons.
6. Run `run-jetbrains-inspectcode.ps1 -BaseRef origin/main -Files 'LamuFlix.Web/Models/Filmes/FilmesViewModel.cs','LamuFlix.Web/TagHelpers/Extensions.cs','LamuFlix.Test/UnitTest1.cs'` at its default WARNING threshold. Fix every WARNING+ finding in those paths for AC2 and notify Patron; none existed in the pre-restoration baseline. Record exit 0. After restoration, run the separate read-only `-All -MinSeverity WARNING` inventory; route only out-of-scope hits to DEV-281.
7. Run gates sequentially: `./scripts/run-roslyn-analyzers.ps1 -BaseRef origin/main -Files <three paths>`, `./scripts/run-cyclomatic-complexity.ps1 -BaseRef origin/main -Files <three paths>` at configured threshold 15, the refactor `-Threshold 6` check for methods DEV-361 changed, and `./scripts/run-jetbrains-inspectcode.ps1 -BaseRef origin/main -Files <three paths>`, then `dotnet format --verify-no-changes` and `dotnet test`. Record numeric exits, findings, and diff review. The solution-wide `-All -Threshold 6` diagnostic on unchanged `origin/main` (T013 F2 rerun) exited **1** listing only `LamuFlix.Web/Extensions/EntityExtensions.cs:13 DynamicQuery` (12), `ViewModelExtensions.cs:8 HasQuery` (8) and `ViewModelExtensions.cs:36 HasPropertyValue` (7), none in the frozen paths; CA1502 does not emit `UnitTest1.DynamicQuery`. No baseline exception is granted: if the three-file threshold-6 run exits non-zero in Phase B, stop and report `blocked` to Patron with the emitted method, complexity and numeric exit. Do not refactor unchanged methods, suppress CA1502, or alter thresholds in DEV-361. List any behavior/package need for Patron follow-up without adding it to the chain.

## File impact boundary

`.editorconfig` and the three measured source paths only: `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs`, `LamuFlix.Web/TagHelpers/Extensions.cs`, `LamuFlix.Test/UnitTest1.cs`. No file-scoped `.editorconfig` suppression, new package, API change, or edit elsewhere. The B1 key on `.editorconfig:68` is held pending owner answer.

## Complexity Tracking

No constitutional violation or added abstraction is planned. Plan frozen at T013 (2026-09-23, Round 1 of 2). Gate 1 remains closed on the B1 owner checkbox in [spec.md](spec.md).
