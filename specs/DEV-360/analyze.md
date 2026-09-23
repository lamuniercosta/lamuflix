# Specification Analysis Report: DEV-360

Read-only `/speckit-analyze` run on `feature/DEV-360` at `064f6c5`. This is the post-repair rerun required by Patron's amendment-conformance ruling. It covers the owner-option-A amendment (`0341cf9`) and its consistency repairs (`b424d7a`, `064f6c5`). I read spec.md, plan.md, tasks.md, research.md, quickstart.md, checklists/requirements.md, and the constitution (v1.0.0). This report replaces the earlier one (SHA256 `59f658bf…bc02`, taken at `0341cf9`). Settled inputs, not findings:
- owner decisions D1–D3 and the option A answer on the T014 block;
- Patron's rulings, including the amendment-conformance ruling;
- the settled Round 1/2 findings.

**Prerequisite run:**
- Plain `check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` exited 1, because the branch name `feature/DEV-360` fails the speckit branch-name pattern.
- Rerun with the script's own overrides (`SPECIFY_FEATURE=360-dev-360-spec`, `SPECIFY_FEATURE_DIRECTORY=specs/DEV-360`) exited 0. `FEATURE_DIR` = `specs/DEV-360`; `AVAILABLE_DOCS` = research.md, quickstart.md, tasks.md.

**Option A conformance.** Every artifact now states option A consistently:

| Artifact | What it says |
|---|---|
| SC-003 (spec.md:49) | DEV-360 may leave no pending operation beyond `AddColumn movie.Status`. The six `Id` IdentityColumn `AlterColumn` operations are baseline residuals. Any other operation fails. No migration or snapshot edit; DEV-19 owns the refresh. |
| US3 (spec.md:20) | Same rule. |
| Gate 1 owner entry (spec.md:64) | Records option A. |
| Status (spec.md:5) | Gate 1 passed, then reopened for option A; re-freeze pending. |
| quickstart §5 (:28-34) | Same pass rule for the probe. |
| plan.md:31 (§2.3 row) | Adds the option A answer. |
| plan.md:52 (B5) | Same pass rule. |
| plan.md:57 (boundary) and T019 (tasks.md:52) | Allow the amendment. |
| research.md:5 and :35 | Record the amended SC-003; the stop verdict is marked pre-amendment. |
| T014 (tasks.md:39) | Pre-amendment verdict marked; stays stopped and unchecked until re-freeze. |
| checklists/requirements.md:12 | Notes the amendment. |

No artifact still requires exactly one pending operation. T004 (tasks.md:13) mentions "exactly one" only as its historical record of the H1 tracker correction.

## Findings

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| T1 | Inconsistency (tracker vs spec) | MEDIUM | spec.md:49, spec.md:5; tasks.md:13 (T004) | SC-003 cites "AC4 as amended by owner option A", but the tracker AC4 has not yet been read back with that change; the last recorded AC4 change is still T004's H1 correction. Per Patron's ruling this does not block re-freeze. It must be recorded with a verified read-back before the ticket closes, and spec.md:5 says so. | Rigger records the option A AC4 on DEV-360 (and the DEV-19 handoff) with a read-back, then cite it the way T004 is cited. |
| B2 | Ambiguity (allowance wording) | LOW | plan.md:57; tasks.md:52 (T019) | The allowance names `0341cf9` and "its consistency follow-up commit" in the singular. There are now two follow-ups (`b424d7a`, `064f6c5`), plus the commit that will persist this report. Each subject names option A, and together they touch only the listed `specs/DEV-360` files, so T019's check still passes. The singular wording is the only gap. | Optional: say "follow-up commits whose subjects name option A". |
| I1 | Inconsistency (citation drift, carried over) | LOW | research.md:3; tasks.md:27; plan.md:23 | The same `UnitTest1.cs` setup is cited with different line ranges. Patron marks this optional. | As before. |
| I2 | Inconsistency (traceability, carried over) | LOW | spec.md:6; spec.md:55-69 | The Input line and Gate 1 envelope don't cite Q4, Round 1, G1 or G2. Patron marks this optional. | As before. |
| C1 | Coverage (carried over) | LOW | quickstart.md:13-14; SC-006 | The isolation check leaves out `LamuFlix.Test`. Patron marks this optional. | As before. |
| D1 | Dependency ordering (carried over) | LOW | tasks.md:56 | The dependencies section doesn't mention T015 or that T005 precedes T006. Patron marks this optional. | As before. |

**Severity counts:** CRITICAL 0 · HIGH 0 · MEDIUM 1 · LOW 5

**Resolved since the report at `0341cf9`:**
- B1 (diff boundary): the allowance is now in plan.md:57 and T019.
- P1 (§2.3 row): option A is now in plan.md:31.
- S1 (status line): spec.md:5 is updated.
- H1 (stale SC-003 verdict): tasks.md:39 and research.md:35 now read as the pre-amendment verdict.

## Coverage Summary

| Requirement | Has task? | Task IDs | Notes |
|---|---|---|---|
| FR-001 Remove both fallbacks | Yes | T008, T009, T010 | T010 runs the literal scan |
| FR-002 Explicit missing-variable outcomes | Yes | T008, T009, T010 | quickstart §2–§3 |
| FR-003 Delete throws `KeyNotFoundException` | Yes | T011, T012 | Test deferred to DEV-280 (D2) |
| FR-004 Snapshot-aligned nullability, no migration | Yes | T013, T013a, T014 | T014 reopened under option A |
| FR-005 `harness.yml` `warningsAsErrors` | Yes | T015 | |
| FR-006 Ledger Standards review within budget | Yes | T016, T017 | Q3/Q4 closing bar in T017 |
| FR-007 `ProcessStarter` unchanged | Yes | T013a, T019 | |
| FR-008 Pipeline gates with numeric exits | Yes | T005, T018 | |
| FR-009 Design package and pinned `dotnet-ef` | Yes | T006, T007 | |
| SC-001 Missing-variable and Delete evidence | Yes | T010, T012 | |
| SC-002 Three `AssistirFilme_*` tests pass | Yes | T009, T018 | |
| SC-003 No introduced pending operation beyond `Status`; six residuals allowed | Yes | T007, T014 | Option A; see T1 |
| SC-004 Warning settings; Ledger findings closed or linked | Yes | T015, T016, T017 | |
| SC-005 No credential literal; gitleaks green | Yes | T010, T018 | |
| SC-006 Tooling gate and host isolation | Yes | T007 | See C1 |

## Constitution Alignment

No conflicts. Option A adds no migration, schema change, dependency, or API change. Deferring the snapshot refresh to DEV-19 fits the constitution's single fresh `Initial` migration (constitution:275). The Principle II and VII rows still rest on Patron's C1/C2 ruling.

## Unmapped Tasks

- **T001–T004:** Phase A process, analysis and tracker records.
- **T005:** Phase B pickup and drift check.

These are workflow tasks, not orphans.

## Metrics

- **Total requirements:** 15 (9 FR + 6 SC)
- **Total tasks:** 22 (T001–T019, plus T001a, T001b and T013a)
- **Coverage:** 100% (15 of 15 requirements have at least one task)
- **Ambiguities:** 1 (B2)
- **Duplications:** 0
- **Critical issues:** 0

## Next Actions

- **Re-freeze bar met:** there are no CRITICAL or HIGH findings, and this report is newer than every artifact edit (`064f6c5`). Persisting it meets the re-freeze bar.
- **T1 is for Rigger:** record it with a verified read-back before the ticket closes. It does not block re-freeze.
- **B2 and the carried-over LOWs are optional.** Editing any artifact for them would require another analysis rerun under T001a.
