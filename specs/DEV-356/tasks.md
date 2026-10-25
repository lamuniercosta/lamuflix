# Tasks: DEV-356 Offline Evaluation Harness (Hit Rate@10 and MRR)

**Input**: Design documents from `specs/DEV-356/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (plan records why). Code Phases 1–5 also require the Phase B entry checklist below, including the **Q2 conjunctive STOP**. Missing Phase B evidence blocks T001.

**Tests**: Included. Brief Q8 / FR-008 require them. Author the behavior test before the production edit it locks. When the types under test do not yet exist, add only the minimum compilable contracts needed so those tests compile; do not treat missing-type compile failures as behavioral red tests. When types already exist and the locked behavior is not already implemented, demonstrate behavioral failure before the behavior edit when possible. Drive each phase's tests green before that phase's commit. Use existing xUnit v3, Shouldly, and FsCheck conventions. Real DEV-416 only after Q2 STOP clears. Testcontainers if real persistence/translation is exercised; never mock drivers. No EF InMemory. No new test NuGet dependency. No property-test opt-out. Single Anvil ownership for Phases 1–5.

**Organization**: Phases follow brief Q8 task order. Anvil implements Phases 1–5, one phase per ask, and stops after Phase 5. Phase 6 is Cog, then Gauge. Do not start Phase 1 until the Phase B entry checklist is satisfied. Plan challenge/freeze and Gate 1 remain separate and are not cleared by this task list.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 deterministic replay/metrics, US2 leakage isolation, US3 command/summary publication.
- Exact file paths are in each description.

## Phase B entry (before Code Phase 1)

**Purpose**: Schedule Build workspace entry checks from brief Q2/Q8/Q10 and PRODUCT §§1–3,5. Do not run gates, tests, or analyzers from this Spec workspace. Missing evidence blocks T001.

**Independent Test**: Every checklist item below has recorded evidence in the task note (or named owner handoff). Any missing item stops code.

- [ ] TB001 Confirm the spec PR has merged and Gate 1 is established (Rigger / YouTrack `Ready to build` handoff). Checklist mark must reference the recorded receipt.
- [ ] TB002 **Q2 conjunctive STOP**: Obtain Phase B drift recon that verifies the merged DEV-416 implementation AND its exact invocation/input/output contract (Wisp → `recon-DEV-356` refresh or successor). Do not invent a replacement ranker, callable seam, or missing DEV-416 details. Reconcile fixture catalog/ranking inputs and call mapping against that evidence (Keel plan reconciliation). Also record concrete deterministic serializer options, UTC parsing, ordinal stable-ID ordering, seed/`TimeProvider` mapping, and an evidence-backed intentional evaluation invocation mode for the approved executable before any code (Sentry F2 clarification; Compass F5). All conditions required before T001/T003.
- [ ] TB003 Full-ticket reconciliation: cite Wisp recon for fuller Scope & Technical Design reconciliation or an explicit no-fuller-text receipt (absent at intake per recon-DEV-356:15; Sentry F7 clarification). When fuller text exists, reconcile against brief/spec/plan/tasks without contradiction before Build. Record the Wisp-cited reconciliation receipt or Wisp-cited "no fuller text found" handoff in `DEV-356`. No unsupported self-certification.
- [ ] TB004 Obtain Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef` mode) recorded in `DEV-356`.
- [ ] TB005 Keel checks or amends drift against the brief/spec/plan before any Code Phase 1 edit. Missing Keel drift check blocks T001/T003.

**Checkpoint**: Phase B entry evidence is complete, including Q2 STOP. Only then may Anvil start T003/T001.

**Keel reconciliation, 2026-10-25:** TB002/TB003/TB005 receipts and verdicts are recorded in note DEV-356. The concrete mapping for T004/T007/T009/T012/T014/T015 is now `brief.md` section "Phase B reconciliation" and the corresponding plan addendum, under Patron ruling `8fedb011` and Wisp receipts `recon-DEV-356:67-127`, `recon-DEV-356-2:1-81`. Mode A includes an exact dedicated entry filter, child-scoped environment parameter/marker transport, no publication from ordinary suite execution, and command failure on zero selected entry tests. Q2 clearance alone does not satisfy TB004; obtain Gauge baseline evidence before T003/T001. Checklist boxes above remain receipt pointers to be reconciled by the Conductor, not implicit permission to start code.

---

## Phase 1: Evaluation project, fixture validation, and pure metrics (US1 foundational)

**Purpose**: After ALL Phase B entry checks, scaffold `tests/LamuFlix.Evaluation` and register in `LamuFlix.sln`, then validate fixtures and implement pure HR@10/MRR computation with unit coverage. Blocks replay/integration until green. Exact DEV-416 project references follow TB002 evidence—add only necessary references, never a blanket UnitTests copy, and no new NuGet packages. A missing-project compile error is not a behavioral red test (Compass F3).

**Independent Test**: Fixture validation rejects duplicate IDs/malformed/nonfinite/unresolved references and accepts empty event arrays; pure metric tests cover ranks 1/10/11 or absent, repeated plays, short/empty rankings, zero observations, and invalid duplicate ranking IDs.

**Checkpoint**: Project builds; fixture validation and pure metrics proven without requiring live DEV-416 call success beyond what TB002 already reconciled for compile-time references.

### Scaffolding for Phase 1 (before or alongside compilable test authoring)

- [X] T003 [US1] After TB001–TB005 (all entry checks), create `tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj` as net10.0 xUnit v3 executable using only existing CPM packages as needed and only necessary project references required by the reconciled DEV-416 contract. Register the project in `LamuFlix.sln`. No new NuGet dependencies. No blanket UnitTests reference copy. Scaffold before or alongside T001/T002 so those tests can compile; behavior still follows at T004/T005 (Compass F3).

### Tests for Phase 1

- [X] T001 [P] [US1] After or alongside T003, author fixture-validation tests in `tests/LamuFlix.Evaluation/` covering required declarations (`fixtureId`, immutable catalog/ranking inputs, event rows with unique stable event ID, user/movie identities, UTC timestamp), rejection of duplicate event IDs / malformed / nonfinite / unresolved movie-catalog references, preservation of valid rows without deduplication, and acceptance of empty event arrays. Minimum compilable contracts when types are absent; behavioral failure before T004 when possible.
- [X] T002 [P] [US1] After or alongside T003, author pure metric unit tests in `tests/LamuFlix.Evaluation/` for HR@10 and MRR truncated at 10: ranks 1, 10, 11 or absent; repeated plays; short/empty rankings; zero observations (`observationCount` and both metrics zero); duplicate movie IDs in ranking output fail rather than silent repair. Minimum compilable contracts when absent; behavioral failure before T005 when possible.

### Implementation for Phase 1

- [X] T004 [US1] After T001 (and T003), implement fixture load/validation types under `tests/LamuFlix.Evaluation/` per FR-003 / Q7 (exact JSON property mapping for DEV-416 predecessor inputs follows TB002 reconciliation; UTC parsing per TB002).
- [X] T005 [US1] After T002 (and T003), implement pure metric computation under `tests/LamuFlix.Evaluation/` per FR-005 / Q5.
- [X] T006 [US1] Drive T001–T002 green.

**Checkpoint**: Phase 1 tests green. Commit `DEV-356 - evaluation project fixture validation and metrics`.

---

## Phase 2: Strict as-of replay and leakage proofs (US1–US2)

**Purpose**: Implement chronological replay with strict as-of history and prove per-observation temporal isolation using the real TB002-reconciled DEV-416 invocation at T009/T010. No temporary compile seam, stub, or fake ranking path is authorized (Sentry F1). Phase 3 adds the independently expected nonempty baseline evidence; it is not the first replacement of a fake ranking path.

**Independent Test**: Leakage tests compare every earlier observation ranking/score under appended/shuffled future events, target exclusion, equal-time events, cross-user future history, and future-derived profiles; full-fixture permutation yields identical in-memory metrics/model results (no published-summary requirement in this phase; Compass F1).

**Checkpoint**: As-of boundary and leakage AC paths are evidenced at the observation level with in-memory permutation equality (aggregate-only equality insufficient; publication not required).

### Tests for Phase 2

- [X] T007 [P] [US1] Author strict as-of replay/history tests in `tests/LamuFlix.Evaluation/`: timestamp then ordinal stable event-ID traversal per TB002; history uses only timestamp < target across applicable users; equal-time rows cannot teach one another; score before target visibility to later observations; fixed seed/`TimeProvider` inputs per TB002.
- [X] T008 [P] [US2] Author leakage tests in `tests/LamuFlix.Evaluation/` comparing each earlier observation ranking and score when future events are appended or shuffled; cover target exclusion, equal-time events, cross-user future history, future-derived profiles, and full-fixture permutation invariance via in-memory metrics/model equality. Aggregate-only assertions are insufficient. Do not require published summary bytes here (Phase 4 owns published-byte permutation proof).

### Implementation for Phase 2

- [X] T009 [US1/US2] After T007–T008, implement strict as-of replay orchestration under `tests/LamuFlix.Evaluation/` per FR-004 / Q4, exercising the real Phase B–reconciled DEV-416 invocation surface from TB002 (no invented replacement ranker, stub, temporary compile seam, or fake ranking path; Sentry F1).
- [X] T010 [US1/US2] Drive T007–T008 green against that real TB002-reconciled invocation.

**Checkpoint**: Phase 2 tests green. Commit `DEV-356 - strict as-of replay and leakage proofs`.

---

## Phase 3: Real DEV-416 fixed-fixture integration (US1)

**Purpose**: On the already-real TB002-reconciled ranking path from Phase 2, add the nonempty baseline fixture and prove independently expected HR@10/MRR against that real ranker. Does not require summary serialization, publication, or published-byte comparison (those land in Phase 4). Does not introduce or replace a fake ranking path (Sentry F1; Compass F2).

**Independent Test**: Nonempty baseline fixture through real DEV-416 matches independently worked expected HR@10/MRR; duplicate ranking IDs fail. Empty-input coverage alone does not satisfy this phase.

**Checkpoint**: Ticket deterministic HR@10/MRR AC against real ranking is evidenced via independently expected metrics. Q8 repeated-command and permuted-fixture published-byte proofs are not claimed here.

### Tests for Phase 3

- [X] T011 [P] [US1] Author real DEV-416 fixed-fixture integration tests in `tests/LamuFlix.Evaluation/` using nonempty `tests/LamuFlix.Evaluation/Fixtures/baseline.json` with independently worked expected HR@10/MRR; prove duplicate ranking IDs fail. Do not require summary serialization, publication, or published-byte identity (Phase 4). Use Testcontainers only if real persistence/translation is exercised; never mock driver behavior.

### Implementation for Phase 3

- [X] T012 [US1] After T011 and TB002, add/complete nonempty `tests/LamuFlix.Evaluation/Fixtures/baseline.json` (fixtureId, immutable catalog/ranking inputs, event rows) and wire real DEV-416 final-ranking integration per reconciled contract. Record independently derived expected HR@10/MRR with the fixture or adjacent expected-results artifact under `tests/LamuFlix.Evaluation/`.
- [X] T013 [US1] Drive T011 green on real DEV-416.

**Checkpoint**: Phase 3 tests green (independently expected real-ranker metrics). Commit `DEV-356 - real DEV-416 baseline evaluation`.

---

## Phase 4: Command and summary publication (US3)

**Purpose**: Add `scripts/run-evaluation.ps1` and deterministic summary serialization/publication using the TB002-recorded intentional evaluation invocation mode, then prove (a) Q8 repeated successful identical-fixture command runs with byte-identical published summaries and (b) published-byte equality for equivalent original/permuted fixtures on the same nonempty Phase 3 real-ranker baseline, after serializer/publication exist (Compass F1/F2). Command intentionally drives evaluation; incidental `dotnet test` side effects are not the publication path (Compass F5).

**Independent Test**: Defaults and explicit overrides; exit 0 only after complete publication; nonzero without truncated/partial success on invalid/missing fixture, ranking failure, ordinary write failure, native child nonzero exit, or terminating/nonterminating script error; empty event array yields zero metrics; JSON field order and numeric rules match FR-006; two successive successful command runs on the Phase 3 nonempty real-ranker fixture (same seed/paths) produce byte-identical published summary files; equivalent original/permuted fixtures of that baseline produce byte-identical published summaries.

**Checkpoint**: Single-command AC, Q8 repeated-command published-byte proof, and permuted-fixture published-byte proof are evidenced after serialization and publication exist.

### Tests for Phase 4

- [X] T014 [P] [US3] Author command/summary-publication tests covering: repo-root-relative defaults (`tests/LamuFlix.Evaluation/Fixtures/baseline.json`, `artifacts/evaluation/metrics-summary.json`, seed 356); `-FixturePath` / `-OutputPath` / `-Seed` / `-Help`; exit 0 only after complete validation/replay/publication; nonzero without truncated/partial success on invalid/missing fixture, ranking failure, ordinary write failure (no truncated output presented as success), native child nonzero exit, and terminating/nonterminating script-error propagation (Sentry F3/F4; do not prescribe catch syntax or write mechanism; no concurrency/crash-recovery scope); empty event array → zero `observationCount`/`hitRateAt10`/`mrr`; exact ordered UTF-8 JSON fields `schemaVersion`, `fixtureId`, `seed`, `cutoff`, `observationCount`, `hitCount`, `reciprocalRankSum`, `hitRateAt10`, `mrr` with finite unrounded invariant-culture numbers and no wall-clock/path/timing metadata; two successive successful command runs on the same nonempty Phase 3 real-ranker fixture/seed/paths with byte comparison of the published summary files; and explicit published-byte comparison for equivalent original/permuted fixtures of that same baseline (Compass F1). Schedule these published-byte proofs only after serialization/publication exist via T015/T016.

### Implementation for Phase 4

- [X] T015 [US3] After T014, implement deterministic summary serialization under `tests/LamuFlix.Evaluation/` per FR-006 / Q6 using TB002-recorded serializer options, and add `scripts/run-evaluation.ps1` per FR-002 / Q7 implementing the TB002-recorded intentional evaluation invocation mode (never publish incidentally through ordinary test execution; Compass F5). Before first generated output, verify ignore coverage for `artifacts/evaluation` and add the narrowly authorized `.gitignore` entry only if needed so generated summaries stay untracked (Sentry F9; preserve Q9 conditional permission). Ordinary write-failure integrity and native child nonzero exit plus terminating/nonterminating script-error propagation must yield nonzero without truncated success; do not add concurrency/crash-recovery scope or prescribe script syntax/mechanism beyond the ruled contracts (Sentry F3/F4).
- [X] T016 [US3] Drive T014 green, including the repeated successful identical-fixture command runs, the equivalent original/permuted-fixture published-byte comparison, and ordinary write-failure / child-exit / script-error propagation proofs on the Phase 3 nonempty real-ranker fixture.

**Checkpoint**: Phase 4 tests green (command/publication plus Q8 repeated-command and permuted-fixture published-byte proofs). Commit `DEV-356 - run-evaluation command and metrics summary`.

---

## Phase 5: Property invariants and UnitTests reference (US1–US2)

**Purpose**: Add FsCheck `[0,1]` bounds and ordering/future-prefix invariants in the existing UnitTests Recommendations property-test location; add evaluation project reference to `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj` only if required. No property-test opt-out.

**Independent Test**: Property tests pass under `./scripts/run-property-tests.ps1` conventions; invariants hold for metric bounds and ordering/future-prefix rules from Q8.

**Checkpoint**: Q8 property obligations evidenced. Ready for Cog refactor.

### Tests for Phase 5

- [X] T017 [P] [US1/US2] Author FsCheck property tests under `tests/LamuFlix.UnitTests/Features/Recommendations/` for metric `[0,1]` bounds and ordering/future-prefix invariance per Q8. No property-test opt-out.

### Implementation for Phase 5

- [X] T018 [US1/US2] After T017, add only the required project reference from `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj` to the evaluation project if needed for the property tests; expose only what those invariants require. Drive T017 green.

**Checkpoint**: Phase 5 tests green. Commit `DEV-356 - evaluation property invariants`.

---

## Phase 6: Refactor and verification (Cog, then Gauge)

**Purpose**: Not an Anvil phase. After Phases 1–5 are committed, Cog refactors the whole change. Gauge then runs the configured gates. Review loops follow brief Q1/Q9 closing bar and two-round cap.

**Checkpoint**: Refactor complexity is at or under the refactor threshold. Every mandatory gate has its script verdict. Scope-empty skip is not recorded as pass.

- [ ] T019 Refactor the completed change across the file set in `specs/DEV-356/plan.md` until cyclomatic complexity passes at the configured refactor threshold. Read `gates.complexity.refactor` from `harness.yml` and pass that configured value to the supported `-Threshold` parameter of `./scripts/run-cyclomatic-complexity.ps1`. Do not encode a task-owned numeric literal as configuration. Extract helpers, early returns, and guard clauses. Do not suppress to pass. Do not edit harness/config/generated files or weaken thresholds.
- [ ] T020 Run the configured gates and record each script verdict: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` with no arguments (normal changed-diff gate; does not replace T019 stricter refactor evidence), `./scripts/run-jetbrains-inspectcode.ps1` with no arguments, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, worktree-safe `./scripts/run-mutation.ps1` without `--since` for changed eligible production projects, and web gates where applicable. Test-only changes do not establish production mutation eligibility. Unchanged `/web` is scope-empty `SKIPPED`, not PASS. No threshold edits. Preserve Q9 verdict handling: skipped/opt-out/N/A/could-not-run never PASS.

---

## Excluded (never scheduled)

UI / `design.md`, clarify/checklist, ADR, CONTEXT.md or ASSUMPTIONS.md rewrite, new schema/migration, new NuGet/npm dependency, new production port/layer, public HTTP/OpenAPI/web contracts, invented DEV-416 replacement ranker or callable seam, temporary stub/fake ranking path, shared-library workaround, ArchitectureTests exemption, automatic STOP/timeout enforcement script, new Epic 7 preflight blocker or owner checkbox, concurrency/crash-recovery write scope beyond ordinary write-failure integrity, live DB/export integration, DEV-417 comparison implementation, production ranking rewrite, LocalPlay/secrets/`Process.Start`, lowering gate thresholds, mutation `--since`, property-test opt-out, and unrelated refactors. A discovered need outside this list stops for a decision. It is not absorbed into a phase.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase B entry**: Depends on Spec workspace handoff. TB001–TB005 must have recorded evidence before T003/T001. TB002 (Q2 STOP) is conjunctive and blocking.
- **Phase 1**: Depends on Phase B entry. Scaffold T003 before or alongside T001/T002; author tests; implement T004/T005 after their tests; drive T006 green before phase commit.
- **Phase 2**: Depends on Phase 1. As-of replay/leakage against real TB002-reconciled invocation (no stub/seam). Author T007/T008 first; T009; drive T010 green. In-memory permutation proof only.
- **Phase 3**: Depends on Phase 2 and TB002 real-contract evidence. Independently expected nonempty real-ranker baseline metrics only; does not depend on Phase 4 serialization/publication; does not replace a fake ranking path. Author T011; T012; drive T013 green.
- **Phase 4**: Depends on Phase 3 (same nonempty real-ranker fixture and successful evaluation path). Owns serialization/publication, Q8 repeated identical-fixture command + published-byte proof, and explicit equivalent original/permuted-fixture published-byte proof after T015 exists. Author T014; T015; drive T016 green.
- **Phase 5**: Depends on Phases 1–2 metric/replay semantics. Author T017; T018; drive green.
- **Phase 6**: Depends on Phases 1–5 being committed. Cog owns T019. Gauge owns T020.

### User Story Dependencies

- **US1 (P1)**: Project/metrics in Phase 1; replay in Phase 2 (real invocation); real ranking baseline metrics in Phase 3; properties in Phase 5. Published-byte identity on the baseline is US3/Phase 4 evidence.
- **US2 (P1)**: Leakage and in-memory permutation proofs in Phase 2; property future-prefix invariants in Phase 5. Published-byte permutation equality is US3/Phase 4 evidence.
- **US3 (P1)**: Command/summary publication, Q8 repeated-command published-byte proof, and permuted-fixture published-byte proof in Phase 4 (needs successful nonempty real-ranker path from Phase 3; proofs run only after serialization/publication exist).

### Within Each User Story

- After all entry checks, scaffold the evaluation project (T003) before or alongside compilable test authoring. Author the behavior test before the production edit it locks. When types are absent, add minimum compilable contracts first; demonstrate behavioral failure before the behavior edit when possible. Missing-project or missing-type compile failures are not behavioral red tests.
- Phase tests green before that phase's commit.
- Anvil one production phase per ask.

### Parallel Opportunities

- TB001–TB005 are intake evidence gates; they complete before any Code Phase 1 task.
- T003 scaffolds before or alongside T001/T002; T001 and T002 test authoring may run in parallel with each other once scaffolding exists or concurrently with T003 when files differ.
- T007 and T008 test authoring in parallel when files differ.
- Single Anvil ownership for Phases 1–5; parallel markers never split two writers onto the same shared harness file.

## Parallel Example: Phase 2 tests

```text
Task: "T007 Author strict as-of replay/history tests"
Task: "T008 Author per-observation leakage tests"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Finish Phase B entry including Q2 STOP (serializer/UTC/ordinal-ID/seed/TimeProvider/invocation-mode records and Wisp-cited TB003 receipt).
2. Finish Phase 1 scaffolding then fixtures/metrics.
3. Finish Phase 2 as-of replay against real TB002-reconciled invocation (in-memory leakage/permutation).
4. Finish Phase 3 nonempty real-ranker baseline (independently expected metrics only).
5. Stop there only to validate US1 real-ranker metrics. Frozen scope still requires US2–US3 (including Phase 4 repeated-command and permuted-fixture published-byte proofs) before review.

### Incremental Delivery

US2 lands with Phase 2 leakage and in-memory permutation proofs (and Phase 5 properties). US3 lands with Phase 4 command/publication plus Q8 repeated successful identical-fixture command runs and equivalent original/permuted-fixture published-byte comparison on the Phase 3 nonempty fixture. Phase 6 does not add behavior.

### Parallel Team Strategy

One implementer is the expected path, in phase order. Do not split two writers onto the same evaluation harness, fixture, or shared test file.

## Notes

- [P] tasks use different files and do not depend on an unfinished task or shared test/minimal-contract file still being written by another unfinished task.
- Story labels map to `spec.md`.
- Commit after each phase, not after every task, using `DEV-356 - {phase subject}`.
- Gate 1 comes from the merged spec PR. Phase B entry (TB001–TB005), including Q2 STOP, must also be evidenced before T001. This task list does not authorize implementation before that.
- Plan challenge/freeze remains a separate later claim and is not cleared here.
- New unanticipated files or decisions return to Keel; recon gaps return to Wisp; do not invent requirements.
- Spec workspace does not run gates, tests, or analyzers for this ask.
- Preserve `.specify/feature.json` pin to `specs/DEV-356` if present. Wisp found it absent in this Build worktree (`recon-DEV-356:17,64,127`); do not create it or make absence a new entry blocker (brief Phase B reconciliation).
