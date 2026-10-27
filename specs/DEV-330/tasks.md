# Tasks: DEV-330 Enrichment status SignalR push

**Input**: Design documents from `specs/DEV-330/` — spec.md, plan.md, brief.md (Q1–Q9), CONCLUSIONS.md, research.md, data-model.md, contracts/enrichment-hub.md, quickstart.md, constitution-amendment-v1.5.0.md, Proposed ADR-0029.

**Prerequisites**: Owner Gate 1 checkbox answered; Phase B pickup preflight in plan.md completed. Phase A writes documentation only.

**Tests**: Required by ticket (unit + integration) and brief Q8 matrix.

**Organization**: Phases follow brief Q8 order: constitution/port/CPM → persistence → hub/adapter → OpenAPI/TS → gates. User stories map as US1 persistence+emission, US2 hub/CORS delivery, US3 contract chain.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 / US3 maps to spec.md user stories
- Include exact file paths in descriptions

## Path Conventions

Repository root `src/`, `tests/`, `web/` as in plan.md Project Structure.

---

## Phase 1: Setup — Phase B pickup receipt (T001–T005)

**Purpose**: Record Gate 1 answer and drift before any production edit.

- [x] T001 Record owner Gate 1 answer for the constitution v1.5.0 + ADR-0029 checkbox from `spec.md`; stop if unchecked. T006/T007 MUST NOT be marked done until this recorded answer exists (Ledger L3 / PC2)
- [x] T002 [P] Recheck `origin/main` drift for named dependencies DEV-421, DEV-310, DEV-314, DEV-320 and for `Directory.Packages.props` Microsoft.AspNetCore.* train vs SignalR.Client 10.0.14; report drift to Keel/Patron rather than inventing a version
- [x] T003 [P] Confirm constitution baseline version and ADR-0029 path collision status on main; stop for reconciliation if needed
- [x] T004 [P] Verify `.specify/feature.json` still pins `specs/DEV-330` and worktree branch is the Phase B delivery branch
- [x] T005 Take test-count baseline (`dotnet test` summary lines) for later comparison

**Anvil Phase B pickup receipt — 2026-10-27**: Checked at `F:/Dev/LamuFlix.worktrees/DEV-330`, branch `feature/DEV-330`, HEAD `b3ba938141a10444b38efa406d72fcab248c2091`, `git status --porcelain` clean before any edit; `git fetch origin` updated nothing, `origin/main` = HEAD = `b3ba9381` (0 ahead / 0 behind).

- T001: Owner Gate 1 answer for "Constitution v1.5.0 MINOR amendment + ADR-0029" is **APPROVED**. Evidence: PR #264 merged 2026-10-27T02:02:19Z with the structural owner checkbox ticked (Conductor-verified tick, recorded in note `DEV-330:10-13`). `specs/DEV-330/spec.md:15` keeps the packet checkbox unchecked and `spec.md:7` keeps `gate1: provisional` per the Phase 1 note "owner checkbox preserved unchecked until user answers"; the recorded answer lives in this receipt, not by editing the spec packet. T006/T007 may proceed.
- T002: Zero drift since `recon-DEV-330`. Named prerequisites are all in `origin/main` history: DEV-421 = `52e44366`, DEV-310 = `1f014a1e`, DEV-314 = `5aa26a8d`, DEV-320 = `d64127d7`. `Directory.Packages.props` Microsoft.AspNetCore.* train is `Microsoft.AspNetCore.OpenApi 10.0.14` (:41) and `Microsoft.AspNetCore.Mvc.Testing 10.0.14` (:58), consistent with the `Microsoft.Extensions.*` 10.0.14 set (:23-30); no `SignalR` token in any `.props`/`.csproj`/`.cs`, so the planned `Microsoft.AspNetCore.SignalR.Client` 10.0.14 (T010) aligns with today's train. No version invented, no drift ruling needed.
- T003: `.specify/memory/constitution.md:567` = `Version: 1.4.0 | Ratified: 2026-09-21 | Last Amended: 2026-10-22`, as plan preflight #4 expects; the closed port list (:148-152) still omits `IEnrichmentStatusNotifier`. `docs/adr/0029-enrichment-status-post-commit-notifier.md` exists on main with `Status: Proposed` and is the only file numbered 0029 in `docs/adr/` — no path collision, so T007 is a status bump only. No reconciliation stop.
- T004: **premise absent, recorded without inventing a pin file.** `find . -name feature.json` (excluding `web/node_modules`) → 0 results; `.specify/init-options.json` and `.specify/integration.json` carry tooling settings only, no feature pin; `check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` fails with `ERROR: Not on a feature branch. Current branch: feature/DEV-330`, because `.specify/scripts/powershell/common.ps1:287-322` resolves the feature directory only via `SPECIFY_FEATURE_DIRECTORY`, `.specify/feature.json`, or a numeric branch prefix (`001-…`/`1234-…`/`20260319-…`), none of which match this repo's `DEV-###` scheme (repo-wide condition, not introduced by DEV-330). The task's intent is verified directly: `specs/DEV-330/` holds `spec.md`, `plan.md`, `tasks.md`; the worktree branch is the Phase B delivery branch `feature/DEV-330`. Whether to add `.specify/feature.json` (or rely on `SPECIFY_FEATURE_DIRECTORY`) is an open premise for Conductor/Keel; no substitute file created, nothing blocking Phase 1.
- T005: Test-count baseline at `b3ba9381`, full `dotnet test` from the worktree root, exit 0, all four projects green: `LamuFlix.ArchitectureTests` 15 passed / 0 failed; `LamuFlix.Evaluation` 82 passed / 0 failed; `LamuFlix.UnitTests` 1824 passed / 0 failed; `LamuFlix.IntegrationTests` 920 passed / 2 skipped / 0 failed of 922 (`OllamaEmbeddingIntegrationTests.GenerateAsync_SamplePlot_Returns384DimensionalEmbedding` and `OpenApiContractTests.OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` skipped by design). Totals: 2841 passed, 2 skipped, 0 failed of 2843. Skips are not counted as PASS for later comparison.

**Pickup disposition**: T001–T005 complete; no production edit made in this phase. Cleared to proceed to Phase 2 (T006–T010) under the existing phase order; T004 premise and the `spec.md` checkbox/spec-packet state are reported to Conductor rather than resolved here.

**Checkpoint**: Gate 1 answered; drift recorded; safe to amend constitution and add the port.

---

## Phase 2: Foundational — Constitution, whitelist, Core port, CPM (T006–T010)

**Purpose**: Blocking prerequisites before story implementation (brief Q8 step 1)

**⚠️ CRITICAL**: No US1–US3 production behavior until this phase completes. T006/T007 blocked until T001 records the owner Gate 1 answer.

- [x] T006 Apply approved Constitution v1.5.0 text from `specs/DEV-330/constitution-amendment-v1.5.0.md` into `.specify/memory/constitution.md` (Sync Impact Report, Principle I port list, API/Contract Rules enrichment wording, Known Technical Debt polling item, Version/Last Amended); review `.specify/templates/*.md` and record no-edit or follow-up — only after T001 Gate 1 evidence
- [x] T007 [P] Publish ADR-0029 as Accepted at `docs/adr/0029-enrichment-status-post-commit-notifier.md` (Keel-authored content; status bump only if still Proposed) in the same delivery PR constraints as the amendment — only after T001 Gate 1 evidence
- [x] T008 [P] Add `|IEnrichmentStatusNotifier` to `PortWhitelistPattern` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` preserving existing tokens
- [x] T009 [P] Create `src/LamuFlix.Core/Ports/IEnrichmentStatusNotifier.cs` with `Task NotifyAsync(MovieId movieId, EnrichmentStatus status, CancellationToken cancellationToken)`
- [x] T010 Add central pin `Microsoft.AspNetCore.SignalR.Client` 10.0.14 in `Directory.Packages.props` and PackageReference in `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` only (no server NuGet, no npm)

**Anvil Phase 2 receipt — 2026-10-27**: Applied at `F:/Dev/LamuFlix.worktrees/DEV-330`, branch `feature/DEV-330`, base `32912979`.

- T006: `.specify/memory/constitution.md` amended exactly per `constitution-amendment-v1.5.0.md`: Sync Impact Report entry prepended (1.4.0 → 1.5.0 MINOR, 2026-10-27), Principle I closed port list now reads `… IPlaybackHistory, IMovieFeaturesRepository, IEnrichmentStatusNotifier, and the BCL/M.E.AI abstractions …` with `Adding a port requires an ADR.` retained, the API/Contract Rules polling sentence replaced with the ADR-0022 SignalR-push wording, the Known Technical Debt `Polling … (ADR-0008)` bullet deleted, footer now `Version: 1.5.0 | Ratified: 2026-09-21 | Last Amended: 2026-10-27`. No unrelated rule touched; the whitelist-only ports `IRecommendationProfileSource` and `IEnrichmentOutbox` were left in place per the amendment constraint not to silently drop whitelist tokens. Templates: `.specify/templates/{checklist,constitution,plan,spec,tasks}-template.md` contain no port-enumeration or enrichment-delivery references (grep for `IMovieFeaturesRepository`/port) → no edits required; Sync Impact Report records that. `constitution-amendment-v1.5.0.md` is left untouched as Phase A history.
- T007: `docs/adr/0029-enrichment-status-post-commit-notifier.md` status line is now `Status: Accepted. Constitution v1.5.0 (MINOR) adds the … port (owner Gate 1 approved per PR #264).` The closing paragraph that still claimed Gate 1 was closed was updated to record the approval; no decision content was altered (Keel-authored body preserved).
- T008: `PortWhitelistPattern` gained `|IEnrichmentStatusNotifier` as the final alternative; all 11 pre-existing tokens, anchors, and the `ports.Count >= 6` anti-vacuity floor are unchanged.
- T009: `src/LamuFlix.Core/Ports/IEnrichmentStatusNotifier.cs` — `Task NotifyAsync(MovieId movieId, EnrichmentStatus status, CancellationToken cancellationToken)`, namespace `LamuFlix.Core.Ports`, no default argument, no SignalR/Api dependency. Core references unchanged.
- T010: `Directory.Packages.props` (test group, after `Microsoft.AspNetCore.Mvc.Testing`) and `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` pin `Microsoft.AspNetCore.SignalR.Client` `10.0.14`; `dotnet list package` confirms 10.0.14 resolved. No server-side SignalR NuGet added (shared framework), no npm package, no other csproj touched.
- Verification: `dotnet build LamuFlix.sln` exit 0, 0 warnings / 0 errors; `dotnet test LamuFlix.ArchitectureTests` 15 passed / 0 failed (whitelist now accepts the new port); `dotnet format --verify-no-changes` exit 0. Changed-file set: the six files above only.

**Checkpoint**: Constitution, ADR, whitelist, port, and test-client dependency in place.

---

## Phase 3: User Story 1 — Committed enrichment status notifications (Priority: P1) 🎯 MVP

**Goal**: Exactly one post-commit notification per actual status transition; none on failed/no-op paths (AC1, AC2). Persistence proofs use a port test double; adapter and live Api notifier DI wait for US2 (brief Q8 steps 2→3).

**Independent Test**: `EnrichmentStatusNotificationPersistenceTests` green against an `IEnrichmentStatusNotifier` test double with real DB/Testcontainers for persistence semantics (no adapter unit or real SignalR DI required yet).

### Tests for User Story 1

- [ ] T011 [P] [US1] Create failing persistence tests in `tests/LamuFlix.IntegrationTests/EnrichmentStatusNotificationPersistenceTests.cs` covering insert-Pending with assigned id, actual transitions (re-request, manual match, consumer completion paths available in host), Pending no-op, unrelated save, failed save, failed enclosing transaction commit or rollback after successful intermediate SaveChanges (emits zero), duplicate-IMDb recovery (Failed only after success), repeated saves without duplicate notifications, multiple in-memory status mutations emit only for persisted-before versus final committed-after, multi-movie save once each, pre-commit cancellation (no event), post-commit cancellation still attempts send via `CancellationToken.None`, throwing-notifier multi-movie case (committed save succeeds and every remaining transitioned movie still receives an attempt), and direct non-handler `IMovieRepository` caller emission proof (Compass L2 / PC1)

### Implementation for User Story 1

- [ ] T013 [US1] Extend `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` to detect persisted-before vs committed-after enrichment status, retain insert ids, clear failed-attempt candidates, require `IEnrichmentStatusNotifier` on the constructor, and after enclosing commit sequentially await `NotifyAsync(..., CancellationToken.None)` per movie with catch/log isolation that continues remaining movies — no speculative transition-tracker helper unless measured gates later force a bounded ruling (PC1/PC2)
- [ ] T014 [US1] Wire required repository constructor/DI/composition and strictly necessary existing fixtures with explicit `IEnrichmentStatusNotifier` doubles (no optional/no-op production fallback); do not require the live Api SignalR adapter yet
- [ ] T015 [US1] Drive T011 green against the port test double without mocking the data-access driver for transition/transaction proofs
- [ ] T016 [US1] Run incremental static-analysis gates with exact touched-file `-Files` lists recorded in the task receipt (`run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1`) then `dotnet format --verify-no-changes`

**Checkpoint**: AC1/AC2 persistence matrix proven against a port test double (brief Q8 step 2). Adapter unit tests and real Api notifier DI are deferred to US2 after T019–T022 (brief Q8 step 3).

---

## Phase 4: User Story 2 — Hub, adapter, CORS delivery (Priority: P1)

**Goal**: Hub at `/hubs/enrichment` broadcasts framed events under existing DevSpa CORS (AC3 + live delivery); real notifier DI and adapter unit proofs complete here (brief Q8 step 3).

**Independent Test**: Adapter unit tests + `EnrichmentHubTests` + hub negotiate cases in `ApiCorsTests` green using official HubConnection against test host with real Api notifier DI.

### Tests for User Story 2

- [ ] T012 [P] [US2] Create failing unit tests in `tests/LamuFlix.UnitTests/Enrichment/SignalREnrichmentStatusNotifierTests.cs` for DTO mapping/broadcast invocation and send-exception isolation (committed outcome unchanged) — green only after T019–T022 (stable ID retained; Phase 4 dependencies authoritative — Compass L3 / PC2)
- [ ] T017 [P] [US2] Create failing `tests/LamuFlix.IntegrationTests/EnrichmentHubTests.cs` for negotiation, live `EnrichmentStatusChanged` framing, camelCase `id` int, all four status names
- [ ] T018 [P] [US2] Extend `tests/LamuFlix.IntegrationTests/ApiCorsTests.cs` to prove retained DevSpa facets on allowed OPTIONS `/hubs/enrichment/negotiate` (origin `http://localhost:5173`, any method/header, no credentials) and exposed `Location` on the appropriate actual CORS response (not a preflight-only header assertion); rejected origin lacks allow-origin; do not invent authorization status codes (Compass L1 / PC2)

### Implementation for User Story 2

- [ ] T019 [P] [US2] Create `src/LamuFlix.Api/EnrichmentStatusChangedDto.cs` positional record `(int Id, EnrichmentStatus EnrichmentStatus)` per Api conventions (Api root path retained)
- [ ] T020 [P] [US2] Create `src/LamuFlix.Api/EnrichmentHub.cs` (server-to-client surface; no client invoke methods/groups; Api root path retained)
- [ ] T021 [US2] Create `src/LamuFlix.Api/SignalREnrichmentStatusNotifier.cs` mapping to DTO, `IHubContext<EnrichmentHub>` / `Clients.All`, independent SignalR JSON name converters, attempt-start/failure logs with movie id and committed status, isolate send failures, sequential awaited one-shot with no application-level timeout/fan-out bound (PC1 R3/R4)
- [ ] T022 [US2] Edit `src/LamuFlix.Api/Program.cs` to `AddSignalR` (+ JSON protocol), register real `IEnrichmentStatusNotifier` → `SignalREnrichmentStatusNotifier`, `MapHub<EnrichmentHub>("/hubs/enrichment")` behind existing CORS middleware order; add named live-host integration test proving real `SignalREnrichmentStatusNotifier` resolution; fixtures use explicit doubles only — no optional production fallback (PC1 / Ledger M1)
- [ ] T023 [US2] Drive T012/T017/T018 green with real notifier DI and HubConnection against test host
- [ ] T024 [US2] Run incremental static-analysis gates with exact touched-file `-Files` lists recorded in the task receipt, then format verify

**Checkpoint**: Connected client receives framed events; CORS preflight for negotiate succeeds for 5173; adapter unit + real DI green.

---

## Phase 5: User Story 3 — OpenAPI components + generated TypeScript (Priority: P2)

**Goal**: C# DTO in `components.schemas`; regenerate baseline/snapshot/types; REST operations unchanged.

**Independent Test**: OpenAPI contract assertions + `npm run check:api` green.

### Tests for User Story 3

- [ ] T025 [US3] Extend `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` for required schema fields, four-name string enum, and unchanged exact `ExpectedOperations`

### Implementation for User Story 3

- [ ] T026 [US3] Create `src/LamuFlix.Api/EnrichmentHubSchemaTransformer.cs` (`IOpenApiDocumentTransformer`) inserting unreferenced C#-derived `EnrichmentStatusChangedDto` into `components.schemas` without a REST path — distinct from existing discovered-type `EnrichmentStatusSchemaTransformer` (recon-DEV-330-2:30-33; Ledger L1 / PC2)
- [ ] T027 [US3] Register the document transformer in `src/LamuFlix.Api/Program.cs` OpenAPI setup alongside existing transformers/normalizer without replacing the discovered-type schema transformer
- [ ] T028 [US3] Regenerate `web/src/api/openapi.json` + Verify snapshot via `quickstart.md` (`LAMUFLIX_REGENERATE_OPENAPI=true`); never hand-edit
- [ ] T029 [US3] Run `npm run generate:api` then `npm run check:api` under `web/` for `types.ts` / mocks; confirm no new hub MSW path handler is required
- [ ] T030 [US3] Drive T025 (required fields, four-name string enum, unchanged exact `ExpectedOperations`) and web drift checks green; run incremental gates with exact touched-file `-Files` lists recorded

**Checkpoint**: DEV-422 can consume generated components type; contract drift clean.

---

## Phase 6: Polish & cross-cutting gates (T031–T036)

**Purpose**: Full applicable verification and frozen-diff discipline (brief Q8 step 5)

- [ ] T031 Run close-out static-analysis on all changed `.cs` with exact `-Files` list recorded (Roslyn, complexity implementation + refactor ceiling, InspectCode) and `dotnet format --verify-no-changes`; if measured complexity requires an internal helper, stop for a bounded file-list ruling (Ledger M2 Follow-up) rather than silently expanding scope
- [ ] T032 Run full `dotnet test`; architecture tests; property-tests script; vulnerable-packages script; record exit codes (SKIPPED/SKIP/N/A/Could not run never reported as PASS)
- [ ] T033 Run mutation gate without `--since` from the worktree; surviving mutant → missing test, never lower threshold
- [ ] T034 Run web gates / generated-contract checks for regenerated artifacts
- [ ] T035 Frozen-list diff check against plan.md file set (Api root paths retained; no speculative helper file); report any extras rather than silently expanding scope
- [ ] T036 Hand PR-body evidence to Quill/Conductor: Gate 1 checkbox state, gate exit codes, limits (process crash between commit and send can lose the event with no durable crash-window detection; no guaranteed receipt; reconnect/refetch is DEV-422; no DEV-422 client; no backplane); do not merge

**Checkpoint**: Ready for review loop under Q9 closing bar; user owns merge.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Gate 1 + drift — blocks everything
- **Foundational (Phase 2)**: Depends on Setup — blocks US1–US3
- **US1 (Phase 3)**: Depends on Foundational — MVP persistence emission against port test double (brief Q8 step 2)
- **US2 (Phase 4)**: Depends on Foundational and on US1 repository/port wiring; sequential after US1 MVP — DTO/hub/adapter/Program (T019–T022) before adapter unit green (T012) and real notifier DI proofs (brief Q8 step 3)
- **US3 (Phase 5)**: Depends on DTO + OpenAPI registration from US2
- **Polish (Phase 6)**: Depends on US1–US3

### User Story Dependencies

- **US1**: Port + repository; persistence proofs inject a port test double (no adapter/SignalR types required)
- **US2**: Depends on T019–T022 (DTO/hub/adapter/Program + real DI) before T012/T017/T018 go green; CORS tests need MapHub
- **US3**: Depends on DTO + document transformer registration

### Parallel Opportunities

- T002–T004 parallel in Setup
- T007–T010 parallel within Foundational after T006 amendment text is applied (whitelist/port/CPM independent files)
- T011 alone in US1 tests; T012/T017/T018 parallel as failing tests in US2; T019/T020 parallel; T012 green only after T021–T022

---

## Parallel Example: User Story 2

```text
Task: Create EnrichmentStatusChangedDto.cs
Task: Create EnrichmentHub.cs
Task: Extend ApiCorsTests negotiate cases
Task: Create EnrichmentHubTests.cs
Task: Create SignalREnrichmentStatusNotifierTests.cs (failing until adapter exists)
```

Then serialize Program.cs + real notifier registration (shared file); drive T012/T017/T018 green.

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1–2 foundation
2. Phase 3 repository persistence emission proven against a port test double (brief Q8 step 2)
3. Stop and validate AC1/AC2 persistence matrix (including failed enclosing commit/rollback after intermediate SaveChanges, repeated saves without duplicates, and multi in-memory mutations → persisted-before vs final committed-after only)
4. Do not require adapter unit tests or real Api notifier DI for this checkpoint

### Incremental Delivery

1. Add US2 hub/adapter/DI (T019–T022), then adapter unit + hub/CORS live delivery (brief Q8 step 3)
2. Add US3 generated contract
3. Phase 6 gates + review

### Notes

- Do not register unused enrichment handlers
- Do not add server SignalR NuGet or npm `@microsoft/signalr` here
- Do not hand-edit generated OpenAPI/TS artifacts
- Do not claim Gate 1, plan freeze, or merge in task completion notes
- PC1/PC2: required notifier ctor + `CancellationToken.None` + per-movie repository isolation; sequential awaits with no app-level timeout/fan-out; T012 ID and phase dependencies unchanged; owner checkbox preserved unchecked until user answers
