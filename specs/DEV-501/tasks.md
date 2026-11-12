# Tasks: Explicit subtitle search and download (DEV-501)

**Input**: [spec.md](./spec.md), [plan.md](./plan.md), [data-model.md](./data-model.md), [contracts/subtitle-search-download.md](./contracts/subtitle-search-download.md), [design.md](./design.md), [brief.md](./brief.md).

**Prerequisites**: plan.md, spec.md; research/data-model/contracts available. Work only in `F:/Dev/LamuFlix.worktrees/feature-501-spec` on `feature/501-spec`.

**Gate 1 prerequisite (explicit; not claimed)**: Owner Gate 1 approval of spec/plan/tasks is required before any implementation code, constitution application, or Phase B pickup execution. Plan freeze does not mark any implementation task complete or grant Gate 1.

**Challenge status**: **PLAN FROZEN** — round 1 closed by Keel after independent repair reread; receipt `PLAN_ADJUDICATION.md`. Gate 1 not approved; runtime OPEN.

**Tests**: Required by brief / FR-018 — include failing tests before or with each seam. Scorer/ranking property tests and the applicable property gate are mandatory (not optional).

**Global rules** (do not restate per task): `TimeProvider` only; injected seeded `Random` for tokens without crypto claims; no explanatory comments (AAA headers in tests only); Shouldly + NSubstitute + FsCheck where property tests apply; never hand-edit `web/src/api/openapi.json` / `types.ts`; never lower gate thresholds; no new NuGet/npm dependency, layer, schema, or LocalPlay/`Process.Start` change; qualified vocabulary; Q7 staging supersedes Q5 direct-create; retain `Subtitles:SaveEnabled`; preserve pre-existing dirty `harness.yml` edit — coordinate minimal prepare-only removal; verify current paths before delete/update because recon:113–130 inventories are incomplete; sequential shared edits then final generated contracts; Principle V: ValidationException→422 via centralized handler+traceId; malformed HTTP/domain/token outside ValidationException→400; no global remap.

## Format: `[ID] [P?] [Story?] Description`

### FR / SC coverage map (all must trace)

| ID | Tasks |
|---|---|
| FR-001 | T013–T015 |
| FR-002 | T018–T020 |
| FR-003 | T014–T015, T017–T020 (400/422/429/504/cancel; ProblemDetails reasons+traceId) |
| FR-004 | T008, T016 |
| FR-005 | T007, T028 |
| FR-006 | T033, T011, T014–T015, T021, T023 |
| FR-007 | T016, T021, T023–T024 |
| FR-008 | T010, T018–T020 |
| FR-009 | T010, T017–T020 |
| FR-010 | T010, T017–T020 |
| FR-011 | T014–T015, T017–T020, T024 |
| FR-012 | T021, T023–T024 (D5 active local-only; distinct 429 copy) |
| FR-013 | T021, T023–T024 |
| FR-014 | T025–T027 |
| FR-015 | T009, T013, T018 |
| FR-016 | T022, T027, T033 |
| FR-017 | T005 |
| FR-018 | T007–T012, T015–T017, T020–T021, T028 |
| FR-019 | T026, T028–T030 |
| FR-020 | T032 |
| FR-021 | T031 |
| FR-022 | all (constraint) |
| FR-023 | T009, T015, T017–T020 |
| SC-001 | T011–T015, T021–T024, T032 |
| SC-002 | T016–T020, T023–T024, T032 |
| SC-003 | T015, T017, T021, T024, T032 |
| SC-004 | T025–T027 |
| SC-005 | T010, T017–T020 |
| SC-006 | T008, T016–T020 |
| SC-007 | T009, T015, T021, T024 |
| SC-008 | T032 |

---

## Phase 1: Setup (after Gate 1; before code)

- [X] T001 Confirm owner Gate 1 approval is recorded for DEV-501 spec/plan/tasks before starting Phase B or implementation; if Gate 1 is not recorded, stop — plan freeze alone grants no Gate 1 or implementation clearance.
- [X] T002 [P] Confirm `.specify/feature.json` remains `specs/DEV-501` and worktree branch is `feature/501-spec`.
- [X] T003 After Gate 1, record Phase B dependency readiness for Player CC panel and Subs discovery against **current main**; **verify current existing paths** for subtitle endpoints/handlers/tests/store/provider cited in plan.md at pickup; amend later tasks only where authorized — do not rebuild prerequisite tickets; if required current-source facts are missing, stop as `needs recon`.
- [X] T004 After Gate 1, run an executable read-only `speckit-analyze` drift check of the approved DEV-501 artifacts against **current main**; write a drift analysis / authorized task-amendment receipt; invent no recon facts; do not start implementation code until this receipt exists.

---

## Phase 2: Foundational (blocking)

**Purpose**: Catalog, scoring, binder, store publication primitives, glossary, constitution maintenance.

**⚠️ CRITICAL**: No user-story endpoint/UI completion until foundational pieces needed by that story exist. **T033 is a foundational dependency** of language/search validation and UI — complete before T013 search validation and T023 UI consumption. Phase 2 starts only after T001–T004.

- [X] T005 After Gate 1, apply scoped Principle VII maintenance in `.specify/memory/constitution.md`: version **1.15.0 → 1.16.0**; replace prepare-time validation wording with search/download acquisition validation (note key-free catalog and valid empty preferences); accept ADR 0041 in the same maintenance step (cite in Sync Impact Report + update Last Amended); record that ADR 0041 supersedes **only** ADR 0038 prepare-route wording while ADR 0038 remains Proposed and provider/read-write separation is preserved; do **not** amend Principle I port list (no new binder port); do **not** amend Principle V (422 for ValidationException preserved); preserve Principle VI telemetry conventions when retiring prepare-only signals; retain typed options/secret isolation/startup non-failure/`SaveEnabled`; no owner structural checkbox (Q2/D4).
- [X] T006 Verify/add Principle VIII glossary terms in root `CONTEXT.md`: **Subtitle Candidate**, **Candidate ID**, **Local Subtitle Track**, **Subtitle Match Percent**, **Subtitle Download Availability** — additive only; do not rewrite existing Movie, Library, or exact-basename sidecar serving entries; verify current `CONTEXT.md` at pickup before edit.
- [X] T033 After shared `SubtitlesOptions`/validator serialization allows it, implement `SupportedSubtitleLanguages.cs`, `GetSubtitleLanguagesQuery.cs`, `GetSubtitleLanguagesQueryHandler.cs`, `GetSubtitleLanguagesResult.cs` under `src/LamuFlix.Core/Features/Subtitles/`; map `GET /api/subtitles/languages` in `SubtitleEndpoints.cs`; add `SubtitleLanguagesResponse.cs` / `SubtitleLanguageOptionDto.cs` under Api Endpoints. Prove ordered/deduplicated configured preferences, remaining ordinal English-name/code order, first preference or null default, bilingual labels, no provider call/key, private no-store. Canonical normalization: trim + `OrdinalIgnoreCase` match, emit catalog `Code`. Align typed preference validation so empty list is valid and explicit supported-language search works with otherwise valid provider settings; reuse catalog for search validation. Catalog unaffected by 422 unless a validator applies. Include catalog/preference/search/API/UI proofs in T011/T012/T015/T021; propagate DTOs through T027 then T022. **Blocks T013 and T023.** Serialize edits that touch `SubtitlesOptions`/validators/`SubtitleEndpoints.cs` with T008/T009/T010 — do not race shared files.
- [X] T007 [P] Implement concrete `SubtitleMatchScorer` in `src/LamuFlix.Core/Features/Subtitles/SubtitleMatchScorer.cs` (moviehash 100; distinct-token Jaccard; `MidpointRounding.AwayFromZero`; empty union 0; sort key helpers). Unit tests in `tests/LamuFlix.UnitTests/Features/Subtitles/SubtitleMatchScorerTests.cs`. Meaningful property tests in `tests/LamuFlix.UnitTests/Features/Subtitles/SubtitleMatchScorerPropertyTests.cs` covering bounds, hash override, deterministic ordering, overlap symmetry/empty behavior (`[Trait("Category","Property")]` per brief Final drafting and test directions). Verify current existing subtitle unit/property test locations at pickup (recon:128 incomplete) before placing files.
- [X] T008 [P] Implement concrete `SubtitleCandidateBinder` in `src/LamuFlix.Core/Features/Subtitles/SubtitleCandidateBinder.cs` as process-local DI **Singleton** (thread-safe; **no** `ISubtitleCandidateBinder` interface/port/layer/distributed store; no mutable static initialization). Options on `SubtitlesOptions`: `CandidateTokenTtl` default `00:05:00` (> zero), `CandidateStoreMaxEntries` default 1000 (>= 1). Mint opaque 32-hex/128-bit tokens under lock via injected seeded `Random` + `TimeProvider` without cryptographic-security claims. On insert: purge `expiresAtUtc <= now`, then if full evict earliest expiry with FIFO tie-break. Unit tests in `tests/LamuFlix.UnitTests/Features/Subtitles/SubtitleCandidateBinderTests.cs` for expiry, cap, FIFO eviction, restart emptiness, cross-movie rejection — no paid side effects; document process-local stale across replicas.
- [X] T009 Extend `SubtitleCandidate` / provider metadata mapping in `src/LamuFlix.Core/Ports/SubtitleCandidate.cs` and `src/LamuFlix.Infrastructure/Adapters/OpenSubtitlesSubtitleProvider.cs` for releaseName + quota/reset; convert relative durations/Retry-After with `timeProvider.GetUtcNow().Add(duration)`; never invent charge/refund; enforce streaming `MaxSubtitleDownloadBytes` default `2 * 1024 * 1024` while reading provider body. Extend `tests/LamuFlix.IntegrationTests/OpenSubtitlesSubtitleProviderTests.cs` (verify path at pickup) for metadata/quota/missing/timeout/failure/oversize.
- [X] T010 Extend `src/LamuFlix.Infrastructure/FileSystem/SubtitleSidecarStore.cs` and discovery: Subs-only BS Player naming; one leading-dot `.srt` staging file reused across next-free `(N)` non-replacing attempts; typed `MaxSubtitlePublicationAttempts` default 10_000; containment/symlink-reparse refusal **before directory creation**, **after creation/before staging**, and **before each publication attempt**; owned-staging cleanup on cancel/finally and on attempt exhaustion (409 `read_only_library`); explicit discovery exclusion tests. Extend `tests/LamuFlix.UnitTests/FileSystem/SubtitleSidecarStoreTests.cs` and `tests/LamuFlix.IntegrationTests/SubtitleSidecarStoreConcurrencyTests.cs` after verifying those paths at pickup.

**Checkpoint**: Catalog, scoring (+ property), binder, provider metadata, glossary path, and guarded store primitives independently testable.

---

## Phase 3: User Story 1 — Search by language with ranked candidates (P1) 🎯 MVP slice

### Tests

- [X] T011 [P] [US1] Unit tests for language validation/defaults/canonical normalization and empty-preferences-valid behavior beside Core subtitle tests (`SupportedSubtitleLanguagesTests` / query-layer tests).
- [X] T012 [P] [US1] Integration/API tests scaffold for `GET /api/subtitles/languages` and `GET /api/movies/{id}/subtitles/search` in `tests/LamuFlix.IntegrationTests/SubtitleSearchDownloadEndpointTests.cs`.

### Implementation

- [X] T013 [US1] Add `SearchSubtitlesQuery` + `SearchSubtitlesQueryValidator` + handler + result in `src/LamuFlix.Core/Features/Subtitles/` using provider search, `SubtitleMatchScorer`, `SubtitleCandidateBinder` issuance, quota/availability projection; register in `HandlerRegistration.cs`. FluentValidation failures remain `ValidationException` → 422 via centralized handler (no endpoint remap).
- [X] T014 [US1] Map `GET /movies/{id:int}/subtitles/search` and ensure languages route wired in `SubtitleEndpoints.cs` with DTO mappers; status mapping per FR-003; `Cache-Control: private, no-store` on success and mapped errors; read-only returns disabled downloadAvailability without blocking search when provider config valid; missing/invalid provider config → 409 `provider_not_configured`; empty preferences alone must not 409.
- [X] T015 [US1] Prove T012 green: catalog ordering/default; sorted candidates; always-present `quota` with null fields when unknown; downloadAvailability; ProblemDetails `reason` + `traceId`; **422** for decorator ValidationException; **400** for malformed binding/domain language prechecks outside ValidationException; 403/404/409; GET **429** `search_rate_limited` (not download-quota narrative); 502/504; private no-store on success and every mapped GET error; caller cancellation not reported as timeout. GET proofs alone do **not** satisfy POST coverage.

**Checkpoint**: Search-only MVP via API.

---

## Phase 4: User Story 2 — Explicit download into Subs and activate track (P1)

### Tests

- [X] T016 [P] [US2] Unit tests for download handler: POST precheck order (movie 404 → token 400 → read-only 409 → provider config 409 → containment 403) before paid call; unknown/expired/evicted/restarted/cross-movie → 400 `stale_candidate`; known remainingDownloads == 0 → 429 before paid call; unknown/positive last-known proceeds after prechecks; malformed SRT / oversize before staging → 502; no automatic retry; cancellation cleanup ≠ timeout.
- [X] T017 [P] [US2] API/handler/filesystem integration cases closing the POST matrix: invalid body/movie id malformed binding → 400; FluentValidation decorator failures → **422** `invalid_request` with `traceId`; unknown/expired/evicted/restarted/cross-movie tokens → 400 `stale_candidate` before paid call; missing movie → 404; read-only → 409 `read_only_library` **before** missing/invalid config 409 `provider_not_configured`; containment/symlink-reparse at create/pre-stage/pre-publication → 403; known-zero quota → 429 `download_quota_exhausted` with known metadata; provider failure/oversize/invalid SRT → 502; provider timeout (caller still active) → 504; caller cancellation cleans staging and is not 504; success and error acquisition responses carry private no-store; 201 `{track, quota}` persists in list/content; collision `(N)` staged reuse and concurrent no-overwrite; publication-attempt ceiling → 409 + staging cleanup.

### Implementation

- [X] T018 [US2] Add `DownloadSubtitleCommand.cs`, `DownloadSubtitleCommandValidator.cs`, `DownloadSubtitleCommandHandler.cs`, `DownloadSubtitleResult.cs`. Handler precheck/execution order MUST match T016/spec/brief D1: missing movie 404 → invalid/stale binding 400 → read-only policy 409 → missing/invalid provider configuration 409 → containment 403 → refuse known-zero quota 429 → paid provider download with streaming byte cap → SRT validate before staging → store publish with rechecks/attempt ceiling → return track + quota. Do not resolve the binder before the movie check. Propagate caller cancellation; ValidationException stays on decorator path to 422.
- [X] T019 [US2] Map `POST /movies/{id:int}/subtitles/download` accepting only `{candidateId}` returning 201 `{track, quota}` reusing `SubtitleTrackDto`; register handler; enforce private no-store on success and mapped error paths; no raw endpoint exception responses.
- [X] T020 [US2] Close T017 proofs green — do not treat GET T015 or provider fixtures as sufficient POST HTTP proof.

**Checkpoint**: Search + download API complete for writable library.

---

## Phase 5: User Story 3 — Quota, timeout, and read-only UI states (P2)

### Tests

- [ ] T021 [P] [US3] Vitest/RTL coverage beside `web/src/features/playback/` tests: language default/change (incl. empty preferences / null default); ordering; stale discard; Get serialization; read-only disabled Get; quota unavailable/exhausted copy; search-rate-limit vs download-quota copy; timeout/error + manual retry; session-safe activation; changed-language completion; **active check only on actual active local track** (D5 — no candidate-row active check).

### Implementation

- [ ] T022 [US3] **After T025–T027 complete** (prepare retirement + final backend contract regeneration/snapshot approval), run `cd web && npm run generate:api` so clients/mocks validate against the final regenerated `web/src/api/openapi.json` / `types.ts` — never hand-edit generated contracts. Snapshot/installer workflows remain gated pickup discoveries (recon:128 incomplete); do not invent commands.
- [ ] T023 [US3] web-implement Subtitles tab UI in `web/src/features/playback/` (`BrowserPlayer.tsx` / `SubtitleSelector.tsx` and tab section files) per `design.md` + D5: LanguageSearch from generated catalog types, QuotaLine, LocalTrackList (active check here only), CandidateList (Get only), SubtitleStatus; native select/radio/button a11y; no new state library; media-chrome retained.
- [ ] T024 [US3] Wire generated clients only; refuse Get client-side when `downloadAvailability.enabled` is false; discard stale searches; keep Off/local usable; map distinct 429 reasons to approved copy; SMOKE-001..003 to `web/e2e/` only if Compose-visible path available — otherwise document smoke deferral vs runtime OPEN receipts.

**Checkpoint**: UI states match design without stopping playback.

---

## Phase 6: User Story 4 — Retire prepare-only acquisition surface (P2)

- [ ] T025 [US4] **Before any deletion**, verify current-checkout existence and shared usage of D4 delete/update candidates and classify additional prepare-named sweep candidates such as `PrepareSubtitleCommand.cs` (recon:116/119/125/128 incomplete — do not treat older recon alone as complete). Remove prepare route mapping and handler wiring from `SubtitleEndpoints.cs` and `HandlerRegistration.cs` only after shared search/download registration edits are in place (sequential — no concurrent shared-file edits). Delete only D4-confirmed prepare-only artifacts after verification: `PrepareSubtitleCommandHandler.cs`, `SubtitlePrepareResponse.cs`, `PrepareSubtitleCommandHandlerTests.cs`, `SubtitlePrepareServeEndpointTests.cs`. Retain sidecar GET and local list/content and all D4 retain list items.
- [ ] T026 [US4] Assert prepare returns 404. Discover applicable installer and snapshot approval interfaces from the **current checkout** before invocation (recon:128 did not establish `scripts/install.ps1`). Remove the prepare-only `harness.yml` references established by recon:86 (`killFileSpanExempt` PrepareSubtitle entry and related comment), coordinating a minimal edit with the owner of the pre-existing dirty change without stashing/resetting/overwriting it. Regenerate affected harness outputs through the discovered installer; do not hand-edit generated gate files or add exemptions to pass gates. Explicitly verify repository-wide code/settings/generated contracts contain no **live** prepare-only references; classify historical documentation and negative 404 assertions separately. Confirm `Subtitles:SaveEnabled` still drives read-only downloadAvailability/POST 409.
- [ ] T027 [P] [US4] After prepare removal, regenerate final OpenAPI/web types and update OpenAPI contract tests/snapshots through the normal snapshot workflow so obsolete prepare symbols are gone and 422/catalog/search/download contracts are present — never hand-edit generated snapshots; sequence after shared endpoint edits (final contracts last).

**Checkpoint**: Prepare-only surface fully retired; final contracts regenerated.

---

## Phase 7: Polish & cross-cutting

- [ ] T028 [P] Run .NET verification on changed scope: `dotnet test`, `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`, **`./scripts/run-property-tests.ps1` (mandatory applicable — scorer/ranking property tests exist)**, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`; report each exit honestly (`SKIPPED`/`SKIP`/`N/A`/`Could not run` ≠ PASS).
- [ ] T029 [P] Run web gates: `./scripts/run-web-gates.ps1` (or project equivalents lint/test/check:api); report honestly.
- [ ] T030 Applicable mutation gate pre-PR via `./scripts/run-mutation.ps1` when required by architect stage — never lower threshold; `SKIPPED`/`N/A` ≠ PASS. Measure new-test file span; do not pre-approve exemptions.
- [ ] T031 Size L three-axis review (Sentry/Ledger/Compass) against closing bar; two-round cap; unnecessary abstractions → follow-ups only.
- [ ] T032 Runtime OPEN receipts after home-cluster prerequisite: configured-key pt-BR search; explicit download plays; BS Player sees Subs file; quota/timeout/read-only continuity using copied real movie folders; anything unexecuted = `Could not run`, never PASS.

---

## Dependencies & Execution Order

### Phase Dependencies / explicit execution order

- **Setup (Phase 1)**: after recorded Gate 1; T001–T004 before any code
- **Governance before feature code**: **T005 completes before** any Core/Api/Infrastructure implementation tasks
- **Foundational (Phase 2)**: serialize shared `SubtitlesOptions`/validator/`SubtitleEndpoints.cs` edits across T008/T009/T010/T033 first; then allow genuinely disjoint scorer bodies/tests (T007) and other non-shared work. T033 before T013/T023
- **US1 (Phase 3)**: after foundational scoring/binder/provider/catalog pieces needed by search
- **US2 (Phase 4)**: after T008–T010 + US1 binder issuance path
- **After US2, despite phase numbering**: **T025 → T026 → T027** (prepare retirement + final backend OpenAPI/contract regeneration/snapshot approval), **then T022** (generator/client validation) **→ T023 → T024** (UI)
- **Polish (Phase 7)**: after desired stories complete

### User Story Dependencies

- **US1**: foundational scoring/binder/provider metadata + catalog (T033)
- **US2**: US1 candidate issuance + foundational store
- **US4 then US3**: prepare retirement/final backend contracts (T025–T027) before generator/UI (T022–T024); design.md (Palette D5) for UI

### Parallel Opportunities

- Only after T005 and after serialized shared options/validator/endpoint edits: T007 unit/property bodies may run in parallel with other **genuinely disjoint** files
- Do **not** treat T008/T009/T010/T033 as mutually disjoint — they share `SubtitlesOptions`/validation and T033 touches `SubtitleEndpoints.cs`
- T006 may run beside foundational work once Gate 1 + Phase B receipts exist (glossary file only)
- T011 / T012 in parallel
- T016 / T017 in parallel
- T028 / T029 in parallel

### Parallel Example: after shared options/endpoints serialized

```text
Task: "SubtitleMatchScorer + unit/property tests (T007)"  # disjoint once options settled
# NOT in parallel with each other while editing shared options/endpoints:
# T008 binder options, T009 provider options/cap, T010 store options, T033 endpoints/catalog
```

---

## Implementation Strategy

### MVP First (US1 + US2 API)

1. Gate 1 → Phase 1 drift/dependency receipts → T005 → serialized shared options/endpoints → remaining foundational
2. US1 search + languages API
3. US2 download API + full POST matrix / filesystem proofs
4. T025–T027 prepare retirement + final backend contracts
5. T022–T024 generator/client validation + UI
6. Stop points validate via integration filters before UI and again after contracts

### Incremental Delivery

1. API search/download
2. Sequential prepare removal + final generated contracts + snapshot approval (T025–T027)
3. Generator/client validation then CC Subtitles UI states (T022–T024; D5)
4. Gates including mandatory property gate + Size L review
5. Runtime receipts when home cluster ready

### Notes

- Exact planned new filenames are pinned in plan.md; if a recon-cited path is missing at pickup, file `needs recon` rather than inventing code facts.
- Palette `design.md` remains authoritative for UI composition; D5 candidate active-check removal is Palette-owned.
- Quill invents no product decisions in these tasks.
- Plan freeze is recorded in `PLAN_ADJUDICATION.md`; task execution and owner Gate 1 approval are not claimed by this task list.
