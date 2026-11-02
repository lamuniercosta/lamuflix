# Tasks: TMDb Keywords, Full Cast, and Collection Signals in MovieFeatures

**Input**: `specs/DEV-358/` spec.md, plan.md, controlling brief.md (Q1–Q12 + Analyze Round 1 + Plan Challenge Round 1 P1–P12 / R1–R10), CONCLUSIONS.md, ASSUMPTIONS.md, notes DEV-358 / recon-DEV-358 / recon-DEV-358-2 / analyze-DEV-358 / adjudication-DEV-358-plan
**Status**: Draft — OWNER DECISION PENDING (Q6). Gate 1 closed. Not PLAN FROZEN. Spec Kit Repair Rounds 1–2 applied; Plan Challenge Round 1 repairs R1–R10 applied.
**Tests**: Required (Q11 / FR-014 / I3 / R5). Write tests alongside or red-first per `$implement` (TDD optional); no mandatory failing-then-passing receipt. Never mock the EF data-access driver — use Testcontainers. Use Shouldly, NSubstitute, AutoFixture, Theory/MemberData, `TimeProvider` and injected seeded `Random`; only `// arrange` / `// act` / `// assert`; no explanatory comments; no gate-threshold edits.
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps) **and only within the same active package**. Same-file edits are never `[P]`.
**Owner gate**: T001 must resolve the Q6 checkbox before any implementation task. Spec drafting may precede that answer; build entry may not (Q1, Q11).
**Package ordering (I2)**: Strict sequential eight-package order — later packages MUST NOT start before the prior package checkpoint; parallelism only inside a ready package.

---

## Package 1 / Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm worktree, feature pin, dependency revalidation, and owner checkbox. No new packages or projects.
**Checkpoint 1**: Owner checkbox resolved + upstream contracts revalidated at build HEAD → unlocks Package 2 only.

- [X] T001 Confirm worktree `F:/Dev/LamuFlix.worktrees/feature-358-spec` on `feature/358-spec`, `.specify/feature.json` → `specs/DEV-358`; run bounded recon revalidating DEV-331/DEV-411/DEV-356 contracts at build HEAD; resolve Q6 owner checkbox (FK reuse vs collection join-table transition) before coding; introduce no new NuGet/npm dependency or project (Q1, Q4, Q6, Q11 step 1; FR-013)

---

## Package 2 / Phase 2: Foundational — CONTEXT.md glossary, domain signals, MovieFeatures

**Purpose**: Canonical glossary + immutable signal data + defensive feature bag/defaults (C1/C2/R3).
**Depends on**: Checkpoint 1 (T001).
**Checkpoint 2**: Glossary present; domain/feature unit + property + equality/dirty-tracking tests green without adapter HTTP or DB → unlocks Package 3 only.

- [X] T002 [P] Add/align six canonical glossary entries to `CONTEXT.md` (`Keyword`, `Full cast`, `Base cast`, `Franchise collection`, `Unavailable signal`, `Known empty`) with term / definition / `_Avoid_:` lines per brief Local vocabulary and Constitution Principle VIII; keep FullCast wording consistent with R3 (C2/R3; recon-DEV-358-2:174-192; FR-012 manifest)
- [X] T003 [P] Create `src/LamuFlix.Core/Domain/MovieKeyword.cs` and `src/LamuFlix.Core/Domain/MovieSignalSnapshot.cs` as immutable domain data: nested availability enum `Unavailable`/`KnownEmpty`/`Available` in `MovieSignalSnapshot.cs` (no extra file); keyword snapshots as `ImmutableArray<MovieKeyword>`; `FullCast` as `ImmutableHashSet<string>` normalized names; collection availability only on the snapshot — no collection identity fields and no `MovieCastEntry` (Q8, Q11; FR-004; R3)
- [X] T004 Extend `src/LamuFlix.Core/Domain/MovieMetadata.cs` to carry optional Keywords/`FullCast` snapshots; sole collection identity/validation remains `CollectionId`/`CollectionName`; no new port/layer (Q8; FR-004; R3; after T003)
- [X] T005 [P] Extend `src/LamuFlix.Core/Features/Recommendations/MovieFeatures.cs` with defensive `ImmutableHashSet<int> Keywords` and nullable positive `CollectionId`; preserve equality/hash and no-signal behaviour (Q8; FR-007; R3)
- [X] T006 Extend `src/LamuFlix.Core/Features/Recommendations/MovieFeaturesMapper.cs` so enabled effective Cast is base Actors ∪ `FullCast` and disabled maps base-cast-only without keyword/collection terms (Q8; FR-007; R3; after T004–T005)
- [X] T007 [P] Add configurable finite nonnegative `Keyword` (default 1.0) and `Collection` (default 2.0) to `src/LamuFlix.Core/Options/SimilarityWeights.cs` (Q8; FR-008; ASSUMPTIONS.md)
- [X] T008 [P] Add/extend `tests/LamuFlix.UnitTests/Domain/MovieMetadataSignalTests.cs` and `tests/LamuFlix.UnitTests/Features/Recommendations/{MovieFeaturesTests.cs,MovieFeaturesPropertyTests.cs,MovieFeaturesArbitraries.cs,MovieFeaturesMapperTests.cs}` for defensive snapshots, availability semantics, equality/hash/dirty-tracking, enabled/disabled cast, and invariant coverage using Shouldly/NSubstitute/AutoFixture/Theory/MemberData as applicable (Q11; FR-014; R3/R5)

**Checkpoint**: Package 2 complete — Package 3 (adapter) may proceed; Packages 4–8 must wait.

---

## Package 3 / Phase 3: User Story 1 — Ingest and supplement TMDb signals (Priority: P1) 🎯 MVP signal path

**Goal**: Keywords/credits fetch + enabled same-movie supplemental merge with typed outcome matrix.
**Depends on**: Checkpoint 2 (Package 2 complete). Do not start before Package 2 checkpoint.
**Independent Test**: WireMock signal tests in IntegrationTests + pure chain matrix in MetadataProviderChainTests / composition tests (no duplicate UnitTests HTTP suite).
**Checkpoint 3**: US1 independently testable offline → unlocks Package 4 only.

- [ ] T009 [P] [US1] Add `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.Signals.cs` and extend `TmdbMetadataProvider.cs` / `TmdbMetadataProvider.Mapping.cs` / `TmdbMetadataProvider.Log.cs` so one logical invocation resolves IMDb identity or existing title/year policy, fetches details for collection, then keywords/credits via existing client/base URL/`api_key`/resilience/cancellation; implement Q7 identity/dedup/full-cast/fail-closed rules; never duplicate a TMDb fallback Found lookup; zero raw `api_key` URI logging (FR-001, FR-002; R4/R5)
- [ ] T010 [US1] Extend `src/LamuFlix.Infrastructure/Adapters/ChainedMetadataProvider.cs` and `MetadataProviderServiceCollectionExtensions.cs` for enabled-only same-movie supplemental merge after OMDb Found; after primary Found, supplemental NotFound or typed Failed (`RateLimited`/`ProviderUnavailable`/`InvalidResponse`) keeps overall Found/`MarkEnriched` with Unavailable affected signals and existing adapter retry/health accounting; preserve primary/fallback matrix when no primary Found; preserve OMDb base metadata; KnownEmpty vs Unavailable semantics; cancellation/unexpected exceptions propagate; no raw exceptions stored/logged as success (FR-003; Q8; R4; after T009)
- [ ] T011 [P] [US1] Extend `tests/LamuFlix.IntegrationTests/MetadataProviderTmdbSignalTests.cs` with embedded offline WireMock payloads covering full/uncapped cast, representative large-cast correctness smoke (no cutoff/cardinality/performance gate), identity mismatch, empty/unavailable/malformed, valid-keywords/malformed-credits and inverse, independent collection and mixed known-empty/unavailable cases, independent partial signal failures, and zero credential-bearing URI logging — do **not** create `UnitTests/Adapters/TmdbMetadataProviderSignalTests.cs` (FR-002, FR-014; R5)
- [ ] T012 [US1] Extend `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs` and `tests/LamuFlix.IntegrationTests/{MetadataProviderTmdbSignalTests.cs,MetadataProviderChainCompositionTests.cs}` for supplemental merge request sequence, every listed outcome, base-metadata preservation, no duplicate fallback lookup, disabled zero-request parity, cancellation/unexpected exception propagation; retain pure chain unit tests without a duplicate HTTP suite (FR-003, FR-014; R4/R5; after T010)

**Checkpoint**: Package 3 complete — Package 4 (persistence) may proceed.

---

## Package 4 / Phase 4: User Story 2a — Atomic persistence and migration (Priority: P1)

**Goal**: `keywords`/`movie_keywords`/`movie_tmdb_cast` (+ Q6-conditional collection FK reuse) with tracked-load/lock/atomicity and per-signal availability proofs.
**Depends on**: Checkpoint 3 (Package 3 complete). Do not start before Package 3 checkpoint; do not overlap Package 3 tasks.
**Independent Test**: Testcontainers repository/migration/composition proofs including reload-save, concurrent same-keyword, mixed-state, amended DEV-424 clear-on-OMDb proof.
**Checkpoint 4**: Persistence/migration roundtrip green → unlocks Package 5 only.

- [ ] T013 [P] [US2] Create `src/LamuFlix.Infrastructure/Persistence/Records/KeywordRecord.cs` and `Configurations/KeywordConfiguration.cs`; extend `MovieRecord.cs` / `MovieConfiguration.cs` / `LamuFlixDbContext.cs` for `movie_keywords` and `movie_tmdb_cast` reusing existing actor identities (no second actor table); retain approved `movie_tmdb_cast` provenance name outside provider-neutral Core (FR-005; Q8 Care-item-3; R2/R10)
- [ ] T014 [US2] Scaffold EF migration `*_AddTmdbRecommendationSignals.cs` + Designer + update `LamuFlixDbContextModelSnapshot.cs` via EF tooling only — never hand-edit generated output (FR-012; Q11; after T013)
- [ ] T015 [US2] Extend `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` to tracked-load keyword joins, full-cast joins, and related identities before replacement; apply deterministic locks for keyword IDs in a distinct stable namespace and both base/`FullCast` actor natural keys using existing upsert discipline; per signal Available replaces, KnownEmpty clears, Unavailable/Disabled is no-op; explicit clear-all clears all; nullable `CollectionId` alone never clears; supersede DEV-424 clear-on-OMDb-success for unavailable/disabled collection while preserving KnownEmpty clears; on recommended Q6 FK branch reuse DEV-424 collection persistence without duplicate ingestion; atomic rollback on failure (FR-005, FR-006; R1/R2; after T014)
- [ ] T016 [P] [US2] Add/extend `tests/LamuFlix.IntegrationTests/{EfMovieRepositoryTmdbSignalTests.cs,EfMovieRepositoryCollectionTests.cs,MigrationTests.cs,PersistenceCompositionTests.cs}` and `tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs` for roundtrip, replacement, clear, unavailable-preserve, disabled-preserve, reload-then-save/no wipe, concurrent same-keyword insert, mixed independent signal states, equality/dirty-tracking, rollback, and amended clear-on-OMDb collection proof for unavailable/disabled supersession (FR-005, FR-014; R1/R2)

**Checkpoint**: Package 4 complete — Package 5 (rehydration/scoring/grouping) may proceed.

---

## Package 5 / Phase 5: User Story 2b + User Story 3 — Rehydration, scoring, seed franchise grouping (Priority: P1)

**Goal**: Catalog/candidate/profile rehydration, keyword/collection scoring terms, indexed same-collection union (embedding-less eligible) + contiguous leading block via pure grouper.
**Depends on**: Checkpoint 4 (Package 4 complete). Do not start before Package 4 checkpoint (no early start after T005/T007 alone).
**Independent Test**: Scorer unit/property; SeedCollectionGrouper unit/property + handler/API integration (production proof separate from profile replay).
**Checkpoint 5**: Features rehydrate/score and US3 seed path proven → unlocks Package 6 only.

- [ ] T017 [US2] Extend `EfMovieCatalog.cs`, `EfRecommendationCandidateSource.cs`, and `EfRecommendationProfileSource.cs` so rehydration preserves signal snapshots/availability without public DTO fields; document incremental enrichment / no automatic backfill for previously enriched titles (FR-007, FR-015; Q8; R9)
- [ ] T018 [US2] Extend `SimilarityScorer.cs` plus `tests/LamuFlix.UnitTests/Features/Recommendations/{SimilarityScorerTests.cs,SimilarityScorerPropertyTests.cs,SimilarityScorerInvariantPropertyTests.cs}` for keyword set-overlap and collection 1/0/absent terms with zero/absent/bounds/symmetry and same-name/different-ID cases (FR-008; after T007/T005)
- [ ] T019 [P] [US3] Create `src/LamuFlix.Core/Features/Recommendations/SeedCollectionGrouper.cs` implementing contiguous leading block up to `min(10, capacity)`, similarity-desc then stable MovieId, remainder MMR order with selected IDs removed, exact no-op when disabled/null/no matches, and accepted leading-block displacement taste cost; keep the grouper pure with no fourth injected port (FR-009; Q9; R9; ASSUMPTIONS.md)
- [ ] T020 [US3] Extend `src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs` and `EfRecommendationCandidateSource.cs` to union indexed eligible local same-collection members before cutoff — including otherwise eligible members without persisted embeddings, rehydrating base and persisted signal data and using existing missing-plot fallback with no embedding writer/global-profile change; exclude seed/duplicates/unavailable/external-only; no whole-catalog scan (FR-009; R9; after T019)
- [ ] T021 [US3] Wire pure `SeedCollectionGrouper` into `GetRecommendationsQueryHandler.cs` for seed path only; do not modify generic `MmrSelector` or global/profile ranking; no public grouping field; no fourth injected port (FR-009; Q9; R9)
- [ ] T022 [P] [US3] Add `tests/LamuFlix.UnitTests/Features/Recommendations/{SeedCollectionGrouperTests.cs,SeedCollectionGrouperPropertyTests.cs}` and extend `GetRecommendationsQueryHandlerTests.cs` / `GetRecommendationsQueryHandlerPropertyTests.cs` for no-op, deterministic order/ties, capacity, more-than-ten, eligibility/dedup (FR-009, FR-014)
- [ ] T023 [US3] Extend `tests/LamuFlix.IntegrationTests/{EfRecommendationCandidateSourceTests.cs,EfRecommendationProfileSourceTests.cs,ApiEndToEndRecommendationTests.cs}` proving production seed-handler/API contiguous top-10 franchise grouping including embedding-less eligible locals; do not claim profile-replay or comparison.md as seed proof (FR-011, FR-014; R6/R9; after T020–T021)

**Checkpoint**: Package 5 complete — Package 6 (evaluation plumbing) may proceed.

---

## Package 6 / Phase 6: User Story 4a — Paired evaluation plumbing (Priority: P1)

**Goal**: Provenance-backed fixture + `EvaluationEntry` mode transport (without publishing the acceptance receipt yet).
**Depends on**: Checkpoint 5 (Package 5 complete). Do not start before Package 5 checkpoint.
**Independent Test**: evaluation unit/integration tests for options/CLI/fixture isolation and independent keyword/collection coverage.
**Checkpoint 6**: Disabled baseline + CLI isolation tests green → unlocks Package 7 only.

- [ ] T024 [P] [US4] Extend existing `tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs` and `tests/LamuFlix.Evaluation/Fixtures/{EvaluationFixtureLoader.cs,EvaluationCatalogMovie.cs}` and `Replay/{EvaluationReplayOptions.cs,EvaluationReplay.cs,ReplayFixtureFactory.cs}` for optional keyword IDs/`FullCast`/CollectionId; Entry reads child-only `LAMUFLIX_EVALUATION_TMDB_SIGNALS` strictly (unset=Disabled; reject invalid), passes parsed mode to pure `EvaluationReplayOptions.Default`; explicit options are environment-independent; no `EvaluationRunner` type (FR-010; Q10–Q11; R7/R8)
- [ ] T025 [US4] Create provenance-backed `tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.json` and scrubbed `baseline.tmdb.sources.json` retaining exact DEV-356 IDs/events/labels/base features and original ranking fields while explicitly adding `ranking.similarityWeights.keyword=1` and `collection=2`; leave `baseline.json` / `baseline.expected.json` untouched; no secrets in provenance; old fixtures missing added fields use record defaults (FR-010; Q10; R8)
- [ ] T026 [US4] Extend `scripts/run-evaluation.ps1` with `-TmdbSignals Disabled|Enabled` and child-only env transport into `EvaluationEntry` (FR-010; ASSUMPTIONS.md; R7)
- [ ] T027 [P] [US4] Add/extend `tests/LamuFlix.Evaluation/Replay/TmdbSignalReplayTests.cs` (retain approved provenance class name outside Core), `tests/LamuFlix.UnitTests/Features/Recommendations/EvaluationReplayPropertyTests.cs`, and `tests/LamuFlix.Evaluation.IntegrationTests/Entry/EvaluationCommandTests.cs` for disabled baseline reproduction with identical positive weights, Enabled independently nonzero exercised keyword and collection terms with nonzero weights (empty/no-op/cast-only insufficient), leakage, explicit options/environment isolation, and strict CLI values (FR-010, FR-014; R7/R8/R10)

**Checkpoint**: Package 6 complete — Package 7 (measurement) may proceed.

---

## Package 7 / Phase 7: User Story 4b — Paired evaluation measurement receipt (Priority: P1)

**Goal**: Execute both pinned publish commands and record Q10 comparison evidence.
**Depends on**: Checkpoint 6 (Package 6 complete). Do not start before Package 6 checkpoint.
**Checkpoint 7**: tracked `specs/DEV-358/comparison.md` satisfies Q10/R6 and T028 is complete → unlocks Package 8 only. BLOCKED, FAIL, or any incomplete measurement leaves Checkpoint 7 unsatisfied and Package 8 locked. Do not invent metrics during Spec drafting.

- [ ] T028 [US4] Execute both pinned publish commands; write ignored `artifacts/evaluation/DEV-358/{baseline.json,tmdb.json}` (never force-add) and tracked `specs/DEV-358/comparison.md` proving identical nonzero denominator, Disabled baseline reproduction, Enabled independently nonzero keyword and collection coverage with positive weights, and Enabled HitCount ≥ Disabled HitCount; receipt includes exact commands, raw/fixture/source SHA-256, seed/mode/cohort/denominator/hit counts/HR@10/MRR/deltas/lambda/weights/independent coverage and scrubbed provenance, and states that profile replay does not prove seed grouping; BLOCKED/FAIL leaves this task incomplete (FR-010; SC-002; Q10; R6/R8; Q11 step 7)

**Checkpoint**: Package 7 complete — Package 8 (gates/reviews) may proceed.

---

## Package 8 / Phase 8: Polish & verification (Build stage)

**Purpose**: Harness gates and review loop — Spec workspace does not claim these passes.
**Depends on**: Checkpoint 7 (Package 7 complete). Do not start before Package 7 checkpoint.

- [ ] T029 Confirm edits stay inside the amended Q11 planned exact edit set (includes `CONTEXT.md`, `EvaluationEntry.cs`, tracked `specs/DEV-358/comparison.md`; excludes removed UnitTests signal HTTP file and README/CHANGELOG); no public API/OpenAPI/TS, no generic MmrSelector edit, no baseline/golden weakening, no LocalPlay/secrets/Process.Start, no threshold edits, no unapproved collection join-table (FR-012; SC-006; C2; R5–R7/R10)
- [ ] T030 Run harness build and `dotnet test` on affected projects then full suite as required (FR-014; SC-005; I3)
- [ ] T031 Run `./scripts/run-roslyn-analyzers.ps1` on changed `.cs` files (Q11)
- [ ] T032 Run `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from `harness.yml` (Q11)
- [ ] T033 Run `./scripts/run-jetbrains-inspectcode.ps1` on changed files (Q11)
- [ ] T034 Run `./scripts/run-property-tests.ps1` (Q11)
- [ ] T035 Run `./scripts/run-vulnerable-packages.ps1` (Q11)
- [ ] T036 Run harness-selected format check / `dotnet format --verify-no-changes` (Q11)
- [ ] T037 Pre-PR: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since`; preserve every configured score/kill-share/file-span threshold; investigate survivors (Q11)
- [ ] T038 Complete exact-head reviews under Q12 closing bar (all three axes; max 2 build internal rounds total); disposition every finding; do not demote blockers at cap

---

## Dependencies & Execution Order

### Strict eight-package order (I2; brief Q11; Analyze Round 1)

| Package | Phase | Contents | May start after |
| --- | --- | --- | --- |
| 1 | Phase 1 | T001 owner/dependency gate | — |
| 2 | Phase 2 | T002–T008 glossary + Core domain/`MovieFeatures` | Checkpoint 1 |
| 3 | Phase 3 | T009–T012 adapter/chain US1 | Checkpoint 2 |
| 4 | Phase 4 | T013–T016 persistence/migration | Checkpoint 3 |
| 5 | Phase 5 | T017–T023 rehydration/scoring/grouping | Checkpoint 4 |
| 6 | Phase 6 | T024–T027 evaluation plumbing | Checkpoint 5 |
| 7 | Phase 7 | T028 measurement receipt | Checkpoint 6 |
| 8 | Phase 8 | T029–T038 gates/reviews | Checkpoint 7 |

Later packages MUST NOT start before the prior package checkpoint. Parallelism is permitted only among independent `[P]` tasks inside the same active package. Explicitly forbidden overlaps from Analyze I2: starting Package 4 during Package 3; starting Package 5 after only T005/T007; starting Package 6 before Package 5 checkpoint.

### User Story mapping

- **US1**: Package 3 (after Package 2).
- **US2**: Packages 4–5 (persistence then rehydration/scoring).
- **US3**: Package 5 (after Package 4).
- **US4**: Packages 6–7 (plumbing then measurement).

### Parallel Opportunities (within a ready package only)

- Package 2: T002/T003/T005/T007/T008 after T001
- Package 3: T009/T011 once Package 2 checkpoint is green
- Package 4: T013/T016 once Package 3 checkpoint is green (not overlapping Package 3)
- Package 5: T019/T022 once Package 4 checkpoint is green
- Package 6: T024/T027 once Package 5 checkpoint is green (respect T025 fixture authorship constraints)

---

## Parallel Example: Package 2 foundational

```text
Task: "Add CONTEXT.md glossary entries (Keyword/Full cast/Base cast/Franchise collection/Unavailable signal/Known empty)"
Task: "Create MovieKeyword.cs and MovieSignalSnapshot.cs with nested availability + FullCast ImmutableHashSet"
Task: "Extend MovieFeatures.cs Keywords/CollectionId"
Task: "Add SimilarityWeights.Keyword/Collection defaults"
Task: "Add MovieMetadataSignalTests + MovieFeatures* equality/dirty-tracking tests"
```

---

## Implementation Strategy

### MVP First (Packages 1–4)

1. Package 1 owner/dependency gate
2. Package 2 glossary + domain/features
3. Package 3 supplemental ingest
4. Package 4 persistence
5. **STOP and VALIDATE** offline signal + DB proofs

### Incremental Delivery

1. Package 5 rehydration/scoring + seed grouping → API proof
2. Package 6 evaluation plumbing
3. Package 7 paired evaluation → tracked comparison.md receipt
4. Package 8 gates/reviews

### Notes

- Q6 join-table selection requires brief/plan/task boundary amendment before continuing Package 4 collection work — do not implement both membership models.
- Ordinary `dotnet test` does not replace T028 publish-script receipt.
- Quill does not run Package 8 gates during Spec drafting.
- Core names are functionality-based (`MovieKeyword`, `MovieSignalSnapshot`, `FullCast`); vendor `Tmdb*` stays on Infrastructure adapters/`TmdbOptions` only (C1 amended by R3).
- Retained provenance names `movie_tmdb_cast` and `TmdbSignalReplayTests` are intentional outside provider-neutral Core (R10).
- README/CHANGELOG remain excluded; narrative-currency residual noted, no ticket (R10).
- Plan Challenge Round 1 repairs do not claim PLAN FROZEN, Gate 1 clearance, or gate/metric PASS.
