# DEV-301 `/speckit-analyze` receipt: frozen plan (D1–D16)

**Date**: 2026-09-30
**Run by**: Keel, after the plan freeze (brief.md, "Plan freeze")
**Inputs**: `spec.md`, `plan.md`, `tasks.md` in the working tree of `feature/301-spec` (HEAD `2c1d8cf` plus uncommitted drafts); `.specify/memory/constitution.md`
**Prerequisite check**: `check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` returned FEATURE_DIR `specs\DEV-301` and passed. No `.specify/extensions.yml`, so no hooks ran.
**Mode**: read-only. None of the three artifacts was edited.
**Prior run**: spec round 1 only. That run produced D1–D7. This is the first run over the frozen D1–D16 set.

## Verdict

**CLEAN for Gate 1.** 0 CRITICAL, 0 HIGH, 0 MEDIUM. There are 8 LOW findings. Each is wording or cross-reference drift. None of them changes what gets built or blocks implementation.

## Findings

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| I1 | Inconsistency | LOW | spec.md:104 vs plan.md:51, T008 | FR-004 shortens the SQL and leaves out the `AS "Value"` alias, which D13 says is required. plan.md and T008 have the exact call. | Optionally copy D13's call into FR-004. The implementer follows T008 → D13 either way. |
| I2 | Inconsistency | LOW | spec.md:134 vs spec.md:95, plan.md:57 | The Assumptions section lists follow-ups F1–F3 only. The proposed F4 (concurrency token) appears only under the known limitations. | None needed. F4 is still proposed and waiting on Patron. Add it once it is ruled. |
| I3 | Underspec | LOW | plan.md:52 vs brief D12 | The plan's cancellation list leaves out `AddAsync`, which D12 covers "if it awaits anything". | T008 already says the CT is forwarded, so no change is needed. |
| I4 | Inconsistency | LOW | tasks.md:9 | The phase header says T001–T003 can run in parallel with T004–T006, but only T001 and T004 are marked `[P]`. | Cosmetic. The header and the Dependencies section agree. |
| I5 | Inconsistency | LOW | plan.md:39, brief:41 vs tasks.md:34 | The plan and the brief say to add `FixedTimeProvider` "only if absent". T014A says "Add" with no condition. | Harmless. Ledger F3 establishes that IntegrationTests has no copy. |
| I6 | Inconsistency | LOW | spec.md:110 vs plan.md:42, T019, T026 | FR-010's no-edit list leaves out `LamuFlix.Tests.Common`, which the plan and tasks exclude (Compass F9). | Optionally add it to FR-010. T026 enforces it anyway. |
| C1 | Constitution | LOW | plan.md:19-26, plan.md:47 | The constitution's stack table names Mapster for object mapping. The plan uses hand-written private `ToDomain`/`Apply`. **This is not a violation.** Mapster is not referenced anywhere under `src/`, so there is no mapping library to depart from. Adding it would be a new dependency, which Q12/AC10 forbid. The diff-based `Apply` also cannot be expressed as a Mapster map. | Optionally add one line to the Constitution Check so reviewers do not raise it. |
| C2 | Constitution | LOW | T004, T014 | Principle IX prefers `Theory` + `MemberData` over repeated `Fact`s. The tasks do not name that form for the zero/negative lease cases (T004) or the three terminal statuses (T014). | This is enforced at implementation and review. No plan change. |

## Coverage

| Requirement | Has task? | Task IDs |
|---|---|---|
| FR-001 repository, 5 members | Yes | T005, T008, T010, T012, T015 |
| FR-002 atomic claim | Yes | T014, T015 |
| FR-003 lease guard | Yes | T004, T005 |
| FR-004 identity | Yes | T007, T008 |
| FR-005 identity map, baseline save, D9, D11 | Yes | T009–T012 |
| FR-006 claim sync (D2, D8) | Yes | T016, T017 |
| FR-007 `Rehydrate` | Yes | T001, T002, T023 |
| FR-008 concurrency and SQL capture | Yes | T019–T022 |
| FR-009 AC8 integration cases | Yes | T007, T009, T011, T014, T016 |
| FR-010 frozen scope, no packages | Yes | T026 |
| FR-011 gates, property, vulnerable packages, mutation | Yes | T003, T006, T013, T018, T023–T025 |
| SC-001 50/50 winners, D6 ×4 | Yes | T020, T022 |
| SC-002 one `UPDATE`, no locks | Yes | T021 |
| SC-003 AC8 on real Postgres | Yes | T011, T014 |
| SC-004 zero packages or out-of-scope files | Yes | T026 |
| SC-005 gates exit 0 or "Could not run" | Yes | T024, T025 |

Unmapped tasks: none.

## Decision trace (D1–D16 → artifacts)

D1 plan:47, T002 · D2 plan:49, T017 · D3 T016 · D4 plan:37-38 · D5 FR-004, T007, T025 · D6 SC-001, T022 · D7 T014A · D8 FR-006, T017 · D9 FR-005, T010, T011 · D10 FR-008, T020 · D11 FR-005, T007, T009, T012 · D12 plan:52, T008, T010, T017 · D13 plan:51, T008 · D14 plan:62, Phase 2 header, T020 · D15 FR-011, T023, T024 · D16 US4, plan:36, T004.

All 16 decisions appear in the artifacts. None is contradicted.

## Metrics

- Requirements: 16 (FR-001–FR-011, SC-001–SC-005)
- Tasks: 27 (T001–T026 plus T014A)
- Coverage: 100%
- Ambiguity: 0 · Duplication: 0 · Critical: 0 · High: 0 · Medium: 0 · Low: 8

## Next action

Nothing blocks Gate 1 or `/implement`. The LOW items do not need a fix round.
