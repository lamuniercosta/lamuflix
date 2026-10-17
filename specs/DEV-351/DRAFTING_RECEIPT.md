# DEV-351 Phase 2 step 3.2 Spec Kit Drafting Receipt

**Date**: 2026-10-17
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-351-spec` | **Branch**: `feature/351-spec` | **HEAD**: `1cdb13c487c1b257f20cc4d2466d31414190a814`

**Method**: `speckit-specify`, `speckit-plan`, and `speckit-tasks` skills read and followed by hand. No `.specify/extensions.yml`, so no before or after hooks. `$env:PYTHONUTF8='1'` set before `setup-plan.ps1 -Json` and `setup-tasks.ps1 -Json`. `setup-plan.ps1` resolved `FEATURE_SPEC`, `IMPL_PLAN`, `SPECS_DIR=specs/DEV-351`, and branch `feature/351-spec`. `setup-tasks.ps1` resolved `FEATURE_DIR` and the tasks template; `AVAILABLE_DOCS` was empty. No Spec Kit runner emitted these files. Content comes from `brief.md`, CONCLUSIONS.md Q1–Q8, `ASSUMPTIONS.md`, note recon-DEV-351, note DEV-351:15–28, the PRODUCT.md lines Patron cited, and `.specify/memory/constitution.md` for the plan gate. Quill added no product ruling.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, this receipt. No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` (`plan.md` states why). `AGENTS.md` was not edited: the ask names the spec docs, and Q7 does not name that file. `.specify/feature.json` already pointed at `specs/DEV-351` and was not changed. No ADR, README, CHANGELOG, tracker edit, commit, or gate run.

**Owner checkbox**: the step 3.2 statement that none was required is superseded by the round-1 correction below. Gate 1 stays closed.

**Traceability**: One user story, because the ticket has one acceptance criterion. Handler recording tests are T005 (Q7 step 1) and become green at T018 (Q7 step 4). The brief says writing tests earlier is not a requirement to run them. Focused files named in `tasks.md`: `PlaybackEventTests.cs`, `EfPlaybackHistoryTests.cs`, `PlaybackHistoryEndpointTests.cs`.

**needs decision**: none.

**needs recon**:

1. recon-DEV-351:38 names `ApiEndToEndPlaybackTests.cs` and does not describe whether that host seeds PostgreSQL or substitutes ports. T007 stays a dependency-and-schema edit and is not the acceptance proof. The seeded proof is T019.
2. C2 closes the port-whitelist gap. `IPlaybackHistory` is pre-whitelisted (constitution.md:136–140, ArchitectureTests.cs:28–29). No ADR is required. recon-DEV-351 still does not mention `LamuFlix.ArchitectureTests`, and Q7 still does not name that file, so no task edits it and this round does not amend it.

## Phase 2 step 3.3 round-1 correction

Applied the numbered list at the end of `brief.md`. This is the existing round-1 fix list, not a new analysis round. No production write, no constitution or ArchitectureTests edit, no scope expansion. Gate 1 remains closed.

1. C1 applied. Unchecked OD-1 is in `spec.md` (spec PR checkbox), `plan.md`, and `tasks.md`. The Domain path stays a recommendation. No silent relocation.
2. C2 applied. `IPlaybackHistory` is recorded as pre-whitelisted. No ADR requirement was added.
3. C3 applied. Incremental `AddPlaybackEvents` is recorded as approved. Generated migration and regression requirements stay.
4. I1 applied. `tasks.md` puts spec analysis, plan challenge adjudication, and owner Gate 1 before Q7 implementation and delivery.
5. I2 applied. T002 is not marked parallel. It depends on T001, including in the phase-dependency list, and it is absent from Parallel Opportunities.

**needs decision**: none added. C1 remains the existing unchecked owner decision.

No `pr-body-DEV-351` note existed. This round does not draft a merge-bar PR body. The spec PR checkbox is OD-1 in `spec.md`.

## Phase 2 step 6 correction

Applied the single numbered 10-item list in `brief.md` (the correction handoff) and note DEV-351 lines 247–256. Corrected drafts are returned for read-only verification. This is not a third fix round. The plan is not frozen. Implementation and delivery are not authorized. No production code was edited. No delivery gate was run. OD-1 stays unchecked.

1. `ASSUMPTIONS.md` and `spec.md` Assumptions keep zero `[assumed]` taste entries and now carry unresolved C1/OD-1 and resolved C2 and C3. OD-1 is not checked. Patron's Domain recommendation is not treated as owner approval.
2. Q4 event id is integer generated identity / `int Id` in spec FR-014, plan Design §3, and T008, T009, T014, and T016. `movie_id` converter and foreign key, and the other Q4 semantics, stay. Required `MovieId` and `PlayedAt` are pinned. `timestamptz` is the equivalent literal for `timestamp with time zone`.
3. T019 adds a bounded append-failure check through existing handling and keeps the seeded actual-adapter acceptance proof. `spec.md`, `plan.md`, and `checklists/requirements.md` no longer say a shaped 500 mapper is already evidenced. Missing response-shape facts go to recon. No new mapper, status, DTO, or baseline.
4. T017 and T019 state deterministic database round-trip comparison with fixed UTC instants aligned to supported precision. T005 keeps exact event and clock equality at the substituted port. No unverified precision number is imported into the spec.
5. T017 proves two-movie sibling movie and event survival with exact counts after one owner is deleted (SC-005).
6. Plan delivery verification and T021 include the three-dot `base...head` diff-envelope review for FR-013, FR-021, and FR-022. Complete planned coverage is not claimed until that review exists and is read back. T011 and T019 include a bounded check for unrelated tracked writes on the shared play context. No new context or service.
7. FR-016 and T016 state that Down discards playback history and preserves movies. Delete-between-resolve-and-append is recorded as an accepted FR-007 residual. Existing retry-is-new-launch wording still disposes the non-atomic launch residual. No cleanup or deduplication scope was added.
8. Plan acknowledges all four handler dependencies and the port-count heuristic, and adds no abstraction. Handler rationale, WatchHistory vocabulary, and unchanged 204 use settled Q1, Q2, Q3, Q5, and Q7 citations. Unsupported constitution.md:138, :460–461, and :488 assertions are removed. No root glossary edit.
9. T019 and plan §6 resolve the real adapter through production `AddLamuFlixPersistence` on the seeded host. Unit composition coverage stays beside that proof. T003's test path and namespace are conditional on OD-1.
10. Unchecked OD-1 stays in spec, plan, and tasks. Prerequisites stay before implementation and delivery. The Q7 envelope, the zero Critical/High/Medium closing bar, and the two-round cap stay. No third fix round, plan freeze, or delivery authorization.

**needs decision**: none added. C1/OD-1 remains the existing unchecked owner decision.
