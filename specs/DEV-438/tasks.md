# Tasks: Rescan replaced movie files and resolve primary video at play time

**Input**: `specs/DEV-438/` spec.md, plan.md, frozen brief.md (Q1–Q8 + Phase 2 adjudication addendum), CONCLUSIONS.md (incl. Phase 2 rulings 1–8), ASSUMPTIONS.md, recon-DEV-438, recon-DEV-438-2, note DEV-438, specs/PRODUCT.md
**Status**: PLAN FROZEN by Keel on 2026-11-04. Phase B pickup recorded on 2026-11-05 at `933e8ca827d18afafd659a2fdecd4f2c21953ce7`: spec PR #324 merged at `338df33b78bcb41629d8bdcdecf5aefab41a4410`; T000/T001 complete. Current authority: `CONCLUSIONS.md` Phase B pickup receipt and current `recon-DEV-438` (127 lines). T002–T028 remain unchecked; no production verification result is claimed.
**Gate 1**: No owner structural checkbox (brief Q8 / Phase 2 addendum; PRODUCT.md §5). Ordinary owner Gate 1 / spec merge precedes Build pickup.
**Tests**: Required (ticket ACs; brief Q7). Prefer new-behavior regressions red before production edits where feasible (consistent with this Tests language — no mandatory uniform TDD invented). No real Process.Start in tests — fake process starter only. No EF driver mocking / EF InMemory. Parallel only on disjoint files after prerequisites.
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`.
**Conventions**: `Method_Condition_Expected`; existing xUnit v3 / Shouldly / NSubstitute / MockFileSystem / AutoFixture / Faker.Net / Theory/MemberData / Testcontainers only; no new packages/layers/ports/schema/UI; never hand-edit generated OpenAPI/TS/MSW; never lower harness thresholds; preserve unrelated `harness.yml` modification and do not stage it with this task; TimeProvider / FakeTimeProvider for time; `const` exclusion tokens / non-mutable static initialization (AGENTS Stryker); no explanatory comments outside allowed exemptions.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs**. Canonical pipeline remains Gate 1 → Build/refactor/gates → review → delivery under Conductor (brief Q6 step 6; Q7–Q8). Path authority: refreshed `recon-DEV-438` + `recon-DEV-438-2`.

---

## Local phase dependency / execution order

```text
Phase 0  T000 (Keel Gate 1 record) — blocks T001+
Phase 1  T001 (pickup recon + reanalysis) — blocks T002+
Phase 2  T002 → T006   (US1 shared foundation)
Phase 3  T007 → T011   (US2 per-movie rescan)
Phase 4  T012 → T015   (US3 bulk rescan)
Phase 5  T016 → T019   (US4 play-time resolution)
Phase 6  T020 → T022   (US5 contracts + glossary)
Phase 7  T023 → T028   (verification, review, delivery)
```

---

## Phase 0: Gate 1 record (blocks Phase 1)

**Purpose**: Confirm user Gate 1 merge before any Build work.
**Checkpoint**: Verified merge receipt in CONCLUSIONS.md; or stop.

- [X] T000 **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-438 in `specs/DEV-438/CONCLUSIONS.md`. Receipt only — does not create a new ruling. T001–T028 MUST NOT proceed until this receipt exists (brief Q8; FR-015). Receipt completed on 2026-11-05; see CONCLUSIONS.md Phase B pickup / T000.

---

## Phase 1: Pickup — fresh recon and drift reanalysis (blocks implementation)

**Purpose**: Confirm frozen brief/spec/plan/tasks still match landed main and the approved file/contract set.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift stops for a cited decision — no invention.

- [X] T001 After T000: obtain fresh Build pickup recon against picked HEAD / landed main for the approved surfaces cited in `recon-DEV-438` / `recon-DEV-438-2` (`IMediaLibraryScanner`, `DirectoryMediaLibraryScanner`, `SupportedVideoExtensions`, `Movie`, `IMediaPlayerLauncher`, launcher adapters, `PlayMovieCommandHandler`, `IMovieRepository` / `EfMovieRepository` Baseline/Apply/PresentIds, `HandlerRegistration`, `ApiEndpoints` / `PlaybackEndpoints` / `ImportEndpoints`, named unit/integration tests, OpenAPI/Verify/TS/MSW generation workflow, `CONTEXT.md`). Run cross-artifact reanalysis of brief/spec/plan/tasks. Amend tasks only where authorized by demonstrated drift. **STOP** on unresolved drift or missing facts for a cited decision — do not invent substitutes. Preserve unrelated `harness.yml` dirt; do not stage it. No production feature edits in this task (brief Q6–Q7; FR-012; Phase 2 ruling 7). Completed at HEAD `933e8ca8` against landed main using current `recon-DEV-438` §§1–7; F1–F10 dispositions and coverage in CONCLUSIONS.md. Missing historical Phase A notes are not substituted for current evidence; STOP still applies to any new missing fact.

---

## Phase 2: User Story 1 — Shared selector, port extensions, Format mutation (P1)

**Goal**: One shared top-level token-filtered selector; Scan + ResolvePrimaryVideo; `Movie.RefreshFormat`; import 404/409 alignment.
**Independent Test**: MockFileSystem selector matrix + unreadable-candidate fault + import 404/409 regression + RefreshFormat field preservation.

- [X] T002 [P] [US1] Add failing unit coverage in `tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs` (and import regression in `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs` as forced) using Theory/MemberData with AutoFixture/Faker.Net for extension/case/largest/ordinal-tie/token-boundary (`movie-sample`, `Movie.Sample` exclude; `sampler`, `extraspecial` keep)/subfolder-ignored/only-excluded/unsupported-only/empty/nested-only cases matching FR-001, plus an **explicit selector unit fault case** proving unexpected candidate length/access/IO propagates (no silent skip of an unreadable supported sibling); add deterministic selection under permuted listing property coverage in the approved existing suite; document/test import's change from unmapped generic 500 to NotFoundException/404 and InvalidTransitionException/409 including only-excluded (Q2; Q7; Phase 2 rulings 1, 6; SC-002; SC-004)
- [X] T003 [US1] Extend `src/LamuFlix.Core/Ports/IMediaLibraryScanner.cs` with `ResolvePrimaryVideo(LibraryPath)` returning sealed `PrimaryVideo(LibraryPath Path, MediaFormat Format)` in new file `src/LamuFlix.Core/Ports/PrimaryVideo.cs` (existing ports area); no new port interface (Q3; FR-002; Phase 2 ruling 7)
- [X] T004 [US1] Implement the shared selector inside `src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs` alongside existing `SupportedVideoExtensions` membership (do not fork the extension list; exclusion tokens as `const`; no mutable static initializer); reuse it from `Scan` and `ResolvePrimaryVideo`; translate only expected missing-folder / no-eligible-video (including only-excluded) outcomes at this adapter boundary to existing domain exception types (`NotFoundException` / `InvalidTransitionException`) — do not globally remap arbitrary `InvalidOperationException`; unexpected candidate IO propagates (make T002 fault case green here) (Q2–Q4; FR-001–FR-002; FR-008; Phase 2 rulings 1, 6)
- [X] T005 [US1] Add `Movie.RefreshFormat(MediaFormat)` in `src/LamuFlix.Core/Domain/Movie.cs` that changes only Format; add/adjust domain unit coverage proving id/path/title/year/metadata/enrichment/user flags are preserved — domain test-first where feasible, consistent with the Tasks Tests language (no mandatory uniform TDD invented) (Q3; FR-003; SC-001; Phase 2 L5/L6)
- [X] T006 [US1] Make T002 suites green against the shared selector/import behavior; narrowly update `src/LamuFlix.Api/Endpoints/ImportEndpoints.cs` OpenAPI problem metadata and `tests/LamuFlix.IntegrationTests/ImportEndpointTests.cs` for import 404/409 (incl. only-excluded); keep unrelated scanner assertions intact except intentional token/error taxonomy alignment; no duplicate-import fix; recursive discovery missing-root untouched (Q6 step 1; FR-008; FR-012–FR-013; Phase 2 ruling 6)

---

## Phase 3: User Story 2 — Per-movie Format-only rescan (P1)

**Goal**: `POST /api/movies/{id}/rescan` with Format-only persistence, mapped failures, and cancellation wiring.
**Independent Test**: Unit handler matrix incl. cancellation; Postgres avi→mkv + concurrent enrichment + no-UPDATE unchanged; endpoint contract + RequestAborted.

- [ ] T007 [P] [US2] Add failing unit coverage for per-movie rescan changed/unchanged/unknown/missing-folder/no-eligible-video plus pre-save cancellation (no save) and in-flight save cancellation (propagates, no success/rollback promise) under `tests/LamuFlix.UnitTests/Features/Library/` (exact new test file at Build inside that folder) (Q4; Q7; FR-004; FR-008; FR-013; Phase 2 ruling 8)
- [ ] T008 [US2] Implement sealed `RescanMovieCommand`, `RescanMovieResult`, validator, handler, and `internal sealed class RescanMovieOperation` in `src/LamuFlix.Core/Features/Library/` (`RescanMovieOperation` takes only `IMovieRepository` + `IMediaLibraryScanner`); handler loads the movie, resolves primary video, compares old/new Format, calls `RefreshFormat` only when changed, saves only on change, never inserts or mutates non-Format state, never re-parses title/year or enqueues enrichment; cooperative cancellation checks before synchronous resolve and before save; both later bulk handler and this handler use the same operation via decorators at entry — no handler-calls-handler (Q3–Q4; FR-003–FR-004; FR-011; Phase 2 rulings 4, 8)
- [ ] T009 [US2] Add `src/LamuFlix.Api/Endpoints/RescanEndpoints.cs` with `MapRescanEndpoints` plus `RescanMovieResponse` in the existing endpoint area for bodyless `POST /api/movies/{id}/rescan` → 200 `{ movieId, changed, oldFormat, newFormat }` (lowercase extensions without dot; existing `MovieId` wire representation); wire through `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs` and register via `src/LamuFlix.Api/HandlerRegistration.cs` `AddHandler`/decorators; bind `HttpContext.RequestAborted` through endpoint → decorators → handler → repository; declare OpenAPI success/problem metadata; rescan remains ungated by LocalPlay (Q4; Q6; FR-004; FR-011; Phase 2 ruling 8)
- [ ] T010 [US2] Extend `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` existing `Baseline`/`From`/`Apply` (and baseline refresh/rebuild) paths for Format changed-property diffing (`record.Format` only when baseline differs; never full-graph `Update`; format-only keeps metadata-write detection false). Add real-Postgres Testcontainers coverage proving: (a) avi→mkv updates exactly one existing row with id/title/year/metadata/enrichment/user flags preserved and no duplicate insert; (b) a concurrent enrichment-owned column write between rescan load and save survives while changed Format persists; (c) unchanged rescan issues no UPDATE. Project `tests/LamuFlix.IntegrationTests/`; exact new test file at Build. Never mock EF query translation or substitute EF InMemory (Q5; Q7; FR-003; FR-013; SC-001; Phase 2 ruling 5)
- [ ] T011 [US2] Add endpoint/host tests for per-movie rescan success and 404/409/422/400 failures with safe ProblemDetails + traceId and no filesystem paths/raw exception text; ordinary success/reachability coverage (no routing workaround vs bulk route — different segment counts); make T007 green (Q4; Q7; FR-008–FR-009; FR-013; SC-004; Phase 2 Compass F3)

---

## Phase 4: User Story 3 — Synchronous snapshot bulk rescan (P1)

**Goal**: `POST /api/movies/rescan` snapshot-and-sequential aggregate with partial-progress semantics.
**Independent Test**: Unit bulk matrix incl. unexpected IO/cancellation not counted failed; Postgres mixed/rerun; endpoint aggregate + RequestAborted.

- [ ] T012 [P] [US3] Add failing unit coverage for bulk empty/mixed/rerun/deleted-snapshot/unexpected-fault/cancellation and no-write-when-unchanged under `tests/LamuFlix.UnitTests/Features/Library/`, explicitly asserting unexpected IO and cancellation are **not** counted failed (Q5; Q7; FR-005–FR-007; FR-013; SC-005; Phase 2 rulings 1, 8)
- [ ] T013 [US3] Extend `src/LamuFlix.Core/Ports/IMovieRepository.cs` and `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` with an ascending existing scalar `MovieId` snapshot projection (`AsNoTracking`; no metadata/entity preload; no new scope/factory port). Each unique snapshotted ID gets one current first `GetAsync` in the fresh request scope; Q5 already settles ownership; accepted bounded tracking; unexpected save abort prevents reuse after failure (Q5; FR-006; Phase 2 ruling 5; recon-DEV-438 §3)
- [ ] T014 [US3] Implement sealed `RescanLibraryCommand`, `RescanLibraryResult`, validator, and handler in `src/LamuFlix.Core/Features/Library/`: snapshot IDs once, process sequentially ascending via shared `RescanMovieOperation`, load current row, resolve disk, `RefreshFormat` + save only when changed, continue on expected load/resolve failures only (`catch` when `NotFoundException` or `InvalidTransitionException` around load/resolve; saves outside the catch), abort on any unexpected with prior commits kept, propagate cancellation without success summary and without counting failed; expose aggregate counts + ascending `failedMovieIds` ([assumed] Q4); IDs inserted after snapshot wait for a later request; decorator registration at entry; no handler-calls-handler / cross-feature reference (Q5; FR-005–FR-007; FR-011; Phase 2 rulings 4–5, 8)
- [ ] T015 [US3] Add bodyless `POST /api/movies/rescan` to `src/LamuFlix.Api/Endpoints/RescanEndpoints.cs` returning 200 `{ changed, unchanged, failed, failedMovieIds }` via `RescanLibraryResponse`; register handler through `AddHandler`/decorators; bind RequestAborted; add endpoint + Postgres mixed/idempotent-rerun coverage; ordinary success/reachability test (3-segment route; no routing workaround); make T012 green (Q4–Q5; Q7; FR-005; FR-013; SC-005; Phase 2 rulings 5, 8)

---

## Phase 5: User Story 4 — Gated play-time file resolution (P1)

**Goal**: lookup → EnsureEnabled → resolve → launch resolved file/extension → history after success; launch-failure and cancellation regressions.
**Independent Test**: Unit order/guards/launch-failure/cancellation; corrected PlaybackEndpointTests; mandatory real-disk host case with unique temp dir + fake starter.

- [ ] T016 [P] [US4] Add/adjust failing unit coverage in `tests/LamuFlix.UnitTests/Features/Playback/PlayMovieCommandHandlerTests.cs` and launcher tests (`tests/LamuFlix.UnitTests/Playback/ProcessMediaPlayerLauncherTests.cs`, `tests/LamuFlix.UnitTests/Playback/DisabledMediaPlayerLauncherTests.cs`) for order lookup→guard→resolve→launch→history, stale Format ignored, zero disk probe for disabled known movie, zero launch/history on earlier failures, disabled Launch still throws, **explicit launch-failure regression** (thrown Launch → unexpected failure path, zero history, zero Format write, no retries/new taxonomy), pre-launch cancellation (no launch/history), and preserve existing `HandleAsync_AppendCancellation_…` semantics (Q3; Q7; FR-010; FR-013; SC-003–SC-004; Phase 2 rulings 2, 8; recon-DEV-438-2 §6). Pickup F2: preserve the current 9-argument `MovieDetails` fixtures and their enrichment-status argument; do not restore the Phase A arity.
- [ ] T017 [US4] Extend `src/LamuFlix.Core/Ports/IMediaPlayerLauncher.cs` with `EnsureEnabled()`; update `src/LamuFlix.Infrastructure/Playback/ProcessMediaPlayerLauncher.cs` (enabled no-op) and `src/LamuFlix.Infrastructure/Playback/DisabledMediaPlayerLauncher.cs` (throws `FeatureDisabledException`); Launch retains its existing gate; keep `ProcessStartInfo.ArgumentList` single-file argument and existing Process.Start mechanism — no shell concatenation or new execution path; wiring via `src/LamuFlix.Infrastructure/Playback/PlaybackServiceCollectionExtensions.cs` as needed (Q3; FR-010–FR-012)
- [ ] T018 [US4] Amend `src/LamuFlix.Core/Features/Playback/PlayMovieCommandHandler.cs` to retain existing `IMovieCatalog`, `IMediaPlayerLauncher`, `IPlaybackHistory`, and `TimeProvider`, and add existing-port dependency `IMediaLibraryScanner` (four ports plus clock in the approved final composition; pickup F1) (Patron-assessed; no composite port): lookup catalog identity (unknown → 404), call `EnsureEnabled` (known disabled → 403 before disk), cooperative cancellation check then resolve primary file via scanner, cooperative check then `Launch` with resolved path + actual extension, and record history only after successful launch using existing clock/append behavior; launcher-reported failure remains unexpected generic 500 with zero history and zero Format write — do not promise the launcher always detects a disappeared media argument; do not persist Format from play; update `src/LamuFlix.Api/Endpoints/PlaybackEndpoints.cs` problem metadata if required for 409 while keeping success 204; bind RequestAborted through endpoint → decorators → handler → catalog/history (Q3–Q4; FR-008; FR-010–FR-011; SC-003; Phase 2 rulings 2–3, 8)
- [ ] T019 [US4] Rewrite `tests/LamuFlix.IntegrationTests/PlaybackEndpointTests.cs` to use folder-valued catalog paths and resolved-file results; add mandatory real-disk import → swap → rescan → play host coverage following the `tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs` / `ApiEndToEndPlaybackTests.cs` pattern with a **unique temporary directory**, guaranteed cleanup, fake process starter, and **no real Process.Start**; make T016 green; preserve success/repeated-play/history completion semantics; include in full gate runs (Q7; FR-013; SC-003; Phase 2 L4). Pickup F2: preserve `PlaybackEndpointTests.KnownDetails()` enrichment status and the current `ApiEndToEndImportTests` OMDb `Type: movie` fixture plus exact-shape `enrichmentStatus` assertion while adding DEV-438 coverage.

---

## Phase 6: User Story 5 — Generated contracts and CONTEXT.md (P2)

**Goal**: Contract chain + glossary alignment; no frontend feature/UI.
**Independent Test**: OpenAPI/TS/MSW drift green; glossary terms present without implementation detail.

- [ ] T020 [US5] Regenerate contracts via supported workflow from recon-DEV-438-2 §6: set `LAMUFLIX_REGENERATE_OPENAPI=true`, run OpenAPI regeneration test (writes `web/src/api/openapi.json` baseline + Verify snapshot `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together), then from `web/` run `npm run generate:api` producing `web/src/api/types.ts` and `web/src/mocks/handlers.ts`; verify with `npm run check:api`. Include forced snapshots/mocks in the explicit project footprint. **Never hand-edit** generated artifacts (Q6; FR-012; SC-006; Phase 2 item 13). Pickup F2: regenerate from current C# DTOs, preserving landed `enrichmentStatus` in OpenAPI, TypeScript, and the Verify snapshot; `handlers.ts` has no measured pickup drift and remains tooling-owned.
- [ ] T021 [US5] After recon of existing `CONTEXT.md` entries, align **Primary video** and **Rescan** glossary definitions with `specs/DEV-438/spec.md` Domain vocabulary — free of implementation detail — stating explicitly that Rescan updates an existing Movie's Format whereas Import creates a Movie (Q2; Q6; Principle VIII; Phase 2 ruling 7; [assumed] ASSUMPTIONS.md)
- [ ] T022 Confirm no frontend feature/UI/admin action work landed; web changes limited to generated contracts / forced generation-aligned snapshots/mocks (Q1; FR-012)

---

## Phase 7: Verification, review, delivery (Q6 step 6; Q7–Q8)

**Purpose**: Full delivery gates, Q8 closing bar, exact-head workflow under Conductor.
**Independent Test**: Honest gate exits; review bar met; unrelated harness.yml still unstaged.

- [ ] T023 Confirm frozen scope only: shared selector; Format-only per-movie + snapshot bulk rescan APIs; mapped failures (incl. import 404/409); gated play resolution/history order; narrow port/wiring/generated-contract/glossary edits; approved tests. No UI, duplicate-import 409, schema, codecs, worker, deleted-title flags, new port/layer/package/ADR, play-time persistence, legacy sample-only migration, or broad rewrites. Preserve unrelated `harness.yml` modification and do not stage it (Q8; FR-011–FR-012; FR-015)
- [ ] T024 Run standalone `dotnet build`, then full unit/integration/architecture suites (`dotnet test` as configured) including the mandatory real-disk case (FR-013–FR-014; Q7; Phase 2 Compass F5)
- [ ] T025 Run `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml, `./scripts/run-jetbrains-inspectcode.ps1` on changed `.cs`; `dotnet format --verify-no-changes`; `./scripts/run-property-tests.ps1`; `./scripts/run-vulnerable-packages.ps1` (FR-014; Q7)
- [ ] T026 Run configured mutation (`pwsh -NoProfile -File ./scripts/run-mutation.ps1`) and OpenAPI/TS/MSW drift (`npm run check:api` / web gates) plus applicable web checks. Report **actual** exits only: PASS/FAIL/SKIPPED (scope-empty)/SKIP (opt-out)/Could not run — never relabel as PASS. **N/A is mutation-only** (FR-014; Q7; SC-006)
- [ ] T027 Size review under Q8 closing bar (Critical/High/Medium block; Low explicit disposition; two rounds total: initial plus one remediation/recheck; unresolved blockers return to Bernstein); review against fixed base/head (FR-015; Q8)
- [ ] T028 Exact-head delivery workflow under Conductor (rebase/ship/PR as routed); user merges; no autonomous merge (brief Q6 step 6; Q8)

---


## Phase B pickup instructions — 2026-11-05

The receipt in `CONCLUSIONS.md` supersedes historical Phase A pending-Gate-1, branch/path, and baseline-preservation statements for this pickup only. Work exclusively in `F:/Dev/LamuFlix.worktrees/DEV-438`, branch `feature/DEV-438`, starting at `933e8ca827d18afafd659a2fdecd4f2c21953ce7` (landed main in current recon). Current `recon-DEV-438` is the pickup path authority; the historical 203-line recon, part 2, and adjudication notes are unavailable, but the frozen artifacts retain their rulings and current recon re-establishes the required footprint. Do not invent an unavailable fact.

F1 corrects the historical word `retains` in spec FR-011 / US4, plan Constitution Check / Approach, and CONCLUSIONS Phase 2 ruling 3: the handler currently has three ports plus TimeProvider; T018 adds the existing scanner port. Patron already approved the resulting four-port composition; no new interface, ADR, composite wrapper, allowlist edit, or constitution departure follows. This is a factual correction, not a new design ruling.

F7: there is no `harness.yml` dirt at pickup. Earlier preserve/exclude instructions are conditional if unrelated dirt exists; never create it. F3–F5 and F8–F10 are accounted for by existing tasks, not additional scope. Implementation starts with T002–T006; delivery gates and later review remain pending.

## Dependencies

- T000 before T001–T028.
- T001 before T002–T028.
- Phase 2 (T002–T006) before Phase 3–5 (selector/port/domain foundation).
- T003 before T004 (port before adapter).
- T005 may proceed after domain tests are named (test-first where feasible); handler work in Phase 3 depends on T005.
- Phase 3 (T007–T011) before Phase 4 bulk endpoint completeness is required for shared RescanEndpoints file edits — T009 creates the endpoint file; T015 extends it (serialize same-file edits).
- Phase 4 (T012–T015) after T008/T005/T010 foundation (`RescanMovieOperation` + Format Baseline/Apply); T013 before T014.
- Phase 5 (T016–T019) after ResolvePrimaryVideo (T003–T004) and EnsureEnabled port work; T017 before T018.
- Phase 6 after rescan + play + import endpoint shapes exist (T006/T009/T015/T018).
- Phase 7 after implementation phases green.
- Parallel only on disjoint files after prerequisites (Phase 2 L6).

## Sequencing examples

```text
T000 Gate 1 receipt → T001 pickup recon (STOP on missing facts)
T002 red selector/import/fault/404-409 → T003 PrimaryVideo port → T004 scanner selector → T005 RefreshFormat → T006 green + ImportEndpoints
T007 red per-movie + cancellation → T008 RescanMovieOperation/handler → T009 MapRescanEndpoints/DI/RequestAborted → T010 Baseline Format + Postgres concurrent → T011 endpoint green
T012 red bulk (unexpected/cancellation not failed) → T013 scalar snapshot → T014 bulk handler → T015 bulk endpoint/Postgres green
T016 red play + launch-failure/cancellation → T017 EnsureEnabled adapters → T018 PlayMovieCommandHandler → T019 endpoint/real-disk unique temp green
T020 LAMUFLIX_REGENERATE_OPENAPI + generate:api + check:api → T021 CONTEXT.md → T022 no-UI confirmation
T023–T028 standalone build / full gates / review / delivery
```

## Parallel opportunities

- After T001: T002 selector tests and T005 domain RefreshFormat tests may proceed in parallel (different files) once T003/T004 sequencing for production selector is respected for green-up.
- T007 and T012 test authoring may prepare in parallel after US1 foundation, but production endpoint file `RescanEndpoints.cs` edits (T009/T015) serialize.
- T016 play unit tests may prepare after ResolvePrimaryVideo exists; launcher adapter edits (T017) are independent of rescan handlers once the port surface is agreed.
- Never mark `[P]` across shared files or incomplete prerequisites.

## Implementation strategy

### MVP

Gate 1 → pickup → US1 shared selector + RefreshFormat + import 404/409 → US2 per-movie rescan with Postgres + concurrent enrichment proof. That alone unblocks explicit Format refresh for swapped files.

### Incremental delivery

1. Shared selector (import behavior includes token exclusions + 404/409).
2. Per-movie rescan API + Format Baseline/Apply + persistence proof.
3. Bulk snapshot rescan for the swap cohort.
4. Play-time resolved file + LocalPlay guard ordering + launch-failure regression.
5. Generated contracts (incl. MSW) + glossary + full gates/review.

### Notes

- Quill Phase A / this repair round does not execute T000–T028 and does not mark code tasks complete.
- Keel recorded PLAN FROZEN in plan.md after independent repair verification; Gate 1 and implementation clearance remain pending.
- Exact new test filenames under existing test projects are chosen at Build without expanding frozen scope (brief Q6).
- Proposed new paths (`PrimaryVideo.cs`, `RescanMovieOperation`, rescan commands/results/responses) are distinct from verified existing recon paths.
