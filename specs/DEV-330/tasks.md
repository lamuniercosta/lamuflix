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

- [x] T011 [P] [US1] Create failing persistence tests in `tests/LamuFlix.IntegrationTests/EnrichmentStatusNotificationPersistenceTests.cs` covering insert-Pending with assigned id, actual transitions (re-request, manual match, consumer completion paths available in host), Pending no-op, unrelated save, failed save, failed enclosing transaction commit or rollback after successful intermediate SaveChanges (emits zero), duplicate-IMDb recovery (Failed only after success), repeated saves without duplicate notifications, multiple in-memory status mutations emit only for persisted-before versus final committed-after, multi-movie save once each, pre-commit cancellation (no event), post-commit cancellation still attempts send via `CancellationToken.None`, throwing-notifier multi-movie case (committed save succeeds and every remaining transitioned movie still receives an attempt), and direct non-handler `IMovieRepository` caller emission proof (Compass L2 / PC1)

### Implementation for User Story 1

- [x] T013 [US1] Extend `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` to detect persisted-before vs committed-after enrichment status, retain insert ids, clear failed-attempt candidates, require `IEnrichmentStatusNotifier` on the constructor, and after enclosing commit sequentially await `NotifyAsync(..., CancellationToken.None)` per movie with catch/log isolation that continues remaining movies — no speculative transition-tracker helper unless measured gates later force a bounded ruling (PC1/PC2)
- [x] T014 [US1] Wire required repository constructor/DI/composition and strictly necessary existing fixtures with explicit `IEnrichmentStatusNotifier` doubles (no optional/no-op production fallback); do not require the live Api SignalR adapter yet
- [x] T015 [US1] Drive T011 green against the port test double without mocking the data-access driver for transition/transaction proofs
- [x] T016 [US1] Run incremental static-analysis gates with exact touched-file `-Files` lists recorded in the task receipt (`run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1`) then `dotnet format --verify-no-changes`

**Anvil Phase 3 receipt — 2026-10-27**: Applied at `F:/Dev/LamuFlix.worktrees/DEV-330`, branch `feature/DEV-330`, base `cb49d266`.

- T011/T015: new `tests/LamuFlix.IntegrationTests/EnrichmentStatusNotificationPersistenceTests.cs` — 13 Testcontainers-backed facts driving a direct `IMovieRepository` caller (no MediatR handler, no data-access mocking): insert-Pending with assigned id and `CancellationToken.None`; six sequential real transitions (insert Pending, MarkEnriched, re-request, MarkNotFound, MarkFailed, MatchManually) one event each with the committed status; Pending no-op plus repeated saves (no duplicates); unrelated watchlist update (no event); two in-memory mutations before one save (only final NotFound emitted); multi-movie save (one per transitioned movie, unchanged movie silent); duplicate-IMDb failed save (zero events, `DuplicateImdbIdException` propagated, row still Pending on reload) then successful recovery emitting exactly one Failed; ambient transaction rolled back after a successful intermediate `SaveChangesAsync` (zero, verified on the follow-up save); ambient transaction committed after the intermediate save (event only after the enclosing commit); pre-commit cancellation (zero, row still Pending in a sibling context); throwing notifier (committed save intact, both movies attempted); caller token cancelled during dispatch (both sends still attempted with `CancellationToken.None`); direct non-handler repository caller (one Enriched event). Helper double: `tests/LamuFlix.IntegrationTests/RecordingEnrichmentStatusNotifier.cs` (records movie id, status and token; injectable per-attempt behavior).
- T013: `EfMovieRepository` ctor is now `(db, timeProvider, options, IEnrichmentStatusNotifier notifier, ILogger<EfMovieRepository> logger, GenreWeightsCache? cache = null, HybridCachedRecommendationProfileSource? tasteProfileCache = null)` — both new dependencies are required, no optional/no-op fallback (`ILogger` carries the ticket's catch/log requirement; `Microsoft.Extensions.Logging.Abstractions` is already an Infrastructure dependency used by sibling adapters, no new package). Transitions are captured from the existing per-movie `Baseline.Status` plus a new `TrackedMovie.IsNew` flag (insert ids are the already-assigned `MovieId`, written by `ToRecord`), so failed saves simply drop the method-local candidate list; `ResetAfterDuplicateImdbId` clearing the identity map also discards them. Post-commit dispatch awaits `NotifyAsync(..., CancellationToken.None)` sequentially, catches and logs each movie's exception and continues with the rest. **Design note (documented, not an escalation):** the ambient-transaction branch (`HasMetadataWrites()` + `Database.CurrentTransaction != null`) has no in-repo commit site (recon §4.3), so candidates from that branch are held on the scoped repository instance until a later save observes that the ambient transaction is gone and re-reads the committed row (`AsNoTracking` status probe); rolled-back or never-committed writes are dropped instead of notified. This is the only state kept between calls, it is per-scoped-instance and never static, and it is the minimal shape that satisfies FR-005/AC1 for a committed outer transaction and the T011 rollback case; no separate transition-tracker helper was added and measured complexity stays under the threshold.
- T014: fixtures adapted with explicit doubles — `RecordingEnrichmentStatusNotifier` (+ `NullLogger<EfMovieRepository>.Instance`) at the 14 direct construction sites (`EfMovieRepositoryTests`, `EfMovieRepositorySurvivorTests`, `EfMovieRepositoryClaimConcurrencyTests`, `EfMovieRepositoryDeleteTests`, `EfMovieRepositoryPlotTests`, `EfMovieCatalogFacetsTests`, `EfRecommendationCandidateSourceTests`, `EnrichmentOutboxPersistenceTests`, `FeedbackTasteProfileInvalidationTests`, `GenreWeightsIntegrationTests`, `ImportMovieFolderCommandHandlerPersistenceTests`, `MissingMovieFeaturesRecoveryTests`, `OutboxRelayTests`, `StrandedMovieSweeperTests`, `ApiEndToEndLibraryTests`) and explicit registrations in the composed/DI harnesses (`ApiHostFactory` and `ApiEndToEndFactory`/`HealthEndpointTests` via `TryAddSingleton` so a real Api registration will win later, `PersistenceCompositionTests`, `TasteProfileInvalidationHarness`, `FeaturesConsumerPersistenceTests`, `MovieCatalogRegistrationTests`, `PersistenceServiceCollectionExtensionsTests`, `TasteProfileCacheRegistrationTests`, `EfMovieRepositoryConstructorTests`). `PersistenceServiceCollectionExtensions` keeps `AddScoped<IMovieRepository, EfMovieRepository>()`; the live Api notifier registration stays with US2/T022. No production no-op fallback exists.
- T016 (touched-file `-Files` list, 28 files — all Phase 3 `.cs` files above the base `cb49d266`): `run-roslyn-analyzers.ps1` exit 0 (0 CA/IDE findings), `run-cyclomatic-complexity.ps1` exit 0 (0 methods > 15), `run-jetbrains-inspectcode.ps1` exit 0 (0 WARNING+; two pre-existing redundant `using LamuFlix.UnitTests;` directives removed from touched files and one deliberate `AccessToDisposedClosure` suppression added in the new test), `dotnet format LamuFlix.sln --verify-no-changes` exit 0.
- Verification: `dotnet build LamuFlix.sln` 0 warnings / 0 errors; full `dotnet test LamuFlix.sln` all green — ArchitectureTests 15, Evaluation 82, UnitTests 1824, IntegrationTests 933 passed / 2 skipped of 935. Totals 2854 passed, 2 skipped, 0 failed (Phase 1 baseline 2841/2/0 plus the 13 new facts).

**Checkpoint**: AC1/AC2 persistence matrix proven against a port test double (brief Q8 step 2). Adapter unit tests and real Api notifier DI are deferred to US2 after T019–T022 (brief Q8 step 3).

---

## Phase 4: User Story 2 — Hub, adapter, CORS delivery (Priority: P1)

**Goal**: Hub at `/hubs/enrichment` broadcasts framed events under existing DevSpa CORS (AC3 + live delivery); real notifier DI and adapter unit proofs complete here (brief Q8 step 3).

**Independent Test**: Adapter unit tests + `EnrichmentHubTests` + hub negotiate cases in `ApiCorsTests` green using official HubConnection against test host with real Api notifier DI.

### Tests for User Story 2

- [x] T012 [P] [US2] Create failing unit tests in `tests/LamuFlix.UnitTests/Enrichment/SignalREnrichmentStatusNotifierTests.cs` for DTO mapping/broadcast invocation and send-exception isolation (committed outcome unchanged) — green only after T019–T022 (stable ID retained; Phase 4 dependencies authoritative — Compass L3 / PC2)
- [x] T017 [P] [US2] Create failing `tests/LamuFlix.IntegrationTests/EnrichmentHubTests.cs` for negotiation, live `EnrichmentStatusChanged` framing, camelCase `id` int, all four status names
- [x] T018 [P] [US2] Extend `tests/LamuFlix.IntegrationTests/ApiCorsTests.cs` to prove retained DevSpa facets on allowed OPTIONS `/hubs/enrichment/negotiate` (origin `http://localhost:5173`, any method/header, no credentials) and exposed `Location` on the appropriate actual CORS response (not a preflight-only header assertion); rejected origin lacks allow-origin; do not invent authorization status codes (Compass L1 / PC2)

### Implementation for User Story 2

- [x] T019 [P] [US2] Create `src/LamuFlix.Api/EnrichmentStatusChangedDto.cs` positional record `(int Id, EnrichmentStatus EnrichmentStatus)` per Api conventions (Api root path retained)
- [x] T020 [P] [US2] Create `src/LamuFlix.Api/EnrichmentHub.cs` (server-to-client surface; no client invoke methods/groups; Api root path retained)
- [x] T021 [US2] Create `src/LamuFlix.Api/SignalREnrichmentStatusNotifier.cs` mapping to DTO, `IHubContext<EnrichmentHub>` / `Clients.All`, independent SignalR JSON name converters, attempt-start/failure logs with movie id and committed status, isolate send failures, sequential awaited one-shot with no application-level timeout/fan-out bound (PC1 R3/R4)
- [x] T022 [US2] Edit `src/LamuFlix.Api/Program.cs` to `AddSignalR` (+ JSON protocol), register real `IEnrichmentStatusNotifier` → `SignalREnrichmentStatusNotifier`, `MapHub<EnrichmentHub>("/hubs/enrichment")` behind existing CORS middleware order; add named live-host integration test proving real `SignalREnrichmentStatusNotifier` resolution; fixtures use explicit doubles only — no optional production fallback (PC1 / Ledger M1)
- [x] T023 [US2] Drive T012/T017/T018 green with real notifier DI and HubConnection against test host
- [x] T024 [US2] Run incremental static-analysis gates with exact touched-file `-Files` lists recorded in the task receipt, then format verify

**Anvil Phase 4 receipt — 2026-10-27**: Applied at `F:/Dev/LamuFlix.worktrees/DEV-330`, branch `feature/DEV-330`, base `65e55c34`.

- T012: new `tests/LamuFlix.UnitTests/Enrichment/SignalREnrichmentStatusNotifierTests.cs` — 5 facts against a substituted `IHubContext<EnrichmentHub>`/`IHubClients`/`IClientProxy`: all four statuses broadcast the `EnrichmentStatusChanged` event name once to `Clients.All` with `EnrichmentStatusChangedDto(Id, EnrichmentStatus)` mapped from `MovieId.Value` (parameterised over Pending/Enriched/NotFound/Failed), the caller token is forwarded to `SendCoreAsync`, and a throwing send is isolated so `NotifyAsync` completes without rethrowing while still making exactly one attempt.
- T017: new `tests/LamuFlix.IntegrationTests/EnrichmentHubTests.cs` — 3 facts against `ApiHostFactory`: HTTP `POST /hubs/enrichment/negotiate?negotiateVersion=1` returns a connection token and transports; `LiveHost_ResolvesRealSignalRNotifier` proves `IEnrichmentStatusNotifier` resolves to `SignalREnrichmentStatusNotifier` in the composed Api host (the Phase 3 `TryAddSingleton` double no longer wins once the real registration exists); an official `HubConnection` (LongPolling over `factory.Server.CreateHandler()`) receives four live `EnrichmentStatusChanged` frames from the real notifier and asserts camelCase `id` (JSON number, expected int) plus `enrichmentStatus` string for all four names, and that `Id` never appears.
- T018: `ApiCorsTests` extended from 3 to 6 facts: allowed-origin `OPTIONS /hubs/enrichment/negotiate` retains origin, echoes requested `POST` method and requested headers, and sends no `Access-Control-Allow-Credentials`; an actual allowed-origin `POST` negotiate response carries allow-origin plus exposed `Location` (not a preflight-only claim); a non-5173 origin POST negotiate carries no allow-origin.
- T019: `src/LamuFlix.Api/EnrichmentStatusChangedDto.cs` — `public sealed record EnrichmentStatusChangedDto(int Id, EnrichmentStatus EnrichmentStatus)` at the flat Api root; `MovieId` unwrapped at the Api boundary. Public because the Api project has no `InternalsVisibleTo` and the adapter unit test asserts the mapped DTO directly (`GlobalExceptionHandler` is the existing public Api-root precedent).
- T020: `src/LamuFlix.Api/EnrichmentHub.cs` — `public sealed class EnrichmentHub : Hub;` at the Api root, server-to-client surface only, no invoke methods/groups/subscriptions.
- T021: `src/LamuFlix.Api/SignalREnrichmentStatusNotifier.cs` — maps `MovieId.Value`/`EnrichmentStatus` to the DTO, `IHubContext<EnrichmentHub>.Clients.All.SendAsync("EnrichmentStatusChanged", dto, cancellationToken)`, attempt-start Information log and failure Error log with movie id + committed status name, `catch (Exception)` isolation matching the repository dispatch pattern. One awaited send; no timeout, retry, fan-out bound, or replay.
- T022: `src/LamuFlix.Api/Program.cs` — `AddSignalR().AddJsonProtocol(...)` applying `HttpJsonConfiguration.Apply` to SignalR `PayloadSerializerOptions` (a separate options instance from the HTTP JSON configuration, so camelCase + four string status names are configured independently), `AddSingleton<IEnrichmentStatusNotifier, SignalREnrichmentStatusNotifier>()`, and `app.MapHub<EnrichmentHub>("/hubs/enrichment")` after `UseCors`/`MapApiEndpoints`. No optional/no-op fallback. Fixtures unchanged: `ApiHostFactory`/`ApiEndToEndFactory` keep their explicit `TryAddSingleton` doubles, which now lose to the real registration by design, and no test asserted on those doubles.
- T023: T012/T017/T018 green against real notifier DI and an official `HubConnection` on the test host; focused runs 5/5 unit and 9/9 integration (3 hub + 6 CORS).
- T024: touched-file `-Files` list (7 files above the base `65e55c34`) — `run-roslyn-analyzers.ps1` exit 0 (0 CA/IDE findings), `run-cyclomatic-complexity.ps1` exit 0 (0 methods > 15), `run-jetbrains-inspectcode.ps1` exit 0 after removing one redundant `using Shouldly;` (already a global using in the unit project) and replacing one redundant `Returns<Task>` type argument in the new unit test; `dotnet format LamuFlix.sln --verify-no-changes` exit 0. One initial roslyn run failed at MSB3030 because it raced a parallel complexity build in the same output folders; the serial rerun passed — no code change was involved.
- Verification: `dotnet build LamuFlix.sln` 0 warnings / 0 errors; full `dotnet test LamuFlix.sln` green — ArchitectureTests 15, Evaluation 82, UnitTests 1829, IntegrationTests 939 passed / 2 skipped of 941. Totals 2865 passed, 2 skipped, 0 failed (Phase 3 2854/2/0 plus 5 adapter facts and 6 hub/CORS facts).

**Checkpoint**: Connected client receives framed events; CORS preflight for negotiate succeeds for 5173; adapter unit + real DI green.

---

## Phase 5: User Story 3 — OpenAPI components + generated TypeScript (Priority: P2)

**Goal**: C# DTO in `components.schemas`; regenerate baseline/snapshot/types; REST operations unchanged.

**Independent Test**: OpenAPI contract assertions + `npm run check:api` green.

### Tests for User Story 3

- [x] T025 [US3] Extend `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` for required schema fields, four-name string enum, and unchanged exact `ExpectedOperations`

### Implementation for User Story 3

- [x] T026 [US3] Create `src/LamuFlix.Api/EnrichmentHubSchemaTransformer.cs` (`IOpenApiDocumentTransformer`) inserting unreferenced C#-derived `EnrichmentStatusChangedDto` into `components.schemas` without a REST path — distinct from existing discovered-type `EnrichmentStatusSchemaTransformer` (recon-DEV-330-2:30-33; Ledger L1 / PC2)
- [x] T027 [US3] Register the document transformer in `src/LamuFlix.Api/Program.cs` OpenAPI setup alongside existing transformers/normalizer without replacing the discovered-type schema transformer
- [x] T028 [US3] Regenerate `web/src/api/openapi.json` + Verify snapshot via `quickstart.md` (`LAMUFLIX_REGENERATE_OPENAPI=true`); never hand-edit
- [x] T029 [US3] Run `npm run generate:api` then `npm run check:api` under `web/` for `types.ts` / mocks; confirm no new hub MSW path handler is required
- [x] T030 [US3] Drive T025 (required fields, four-name string enum, unchanged exact `ExpectedOperations`) and web drift checks green; run incremental gates with exact touched-file `-Files` lists recorded

**Checkpoint**: DEV-422 can consume generated components type; contract drift clean.

---

## Phase 6: Polish & cross-cutting gates (T031–T036)

**Purpose**: Full applicable verification and frozen-diff discipline (brief Q8 step 5)

- [x] T031 Run close-out static-analysis on all changed `.cs` with exact `-Files` list recorded (Roslyn, complexity implementation + refactor ceiling, InspectCode) and `dotnet format --verify-no-changes`; if measured complexity requires an internal helper, stop for a bounded file-list ruling (Ledger M2 Follow-up) rather than silently expanding scope
- [x] T032 Run full `dotnet test`; architecture tests; property-tests script; vulnerable-packages script; record exit codes (SKIPPED/SKIP/N/A/Could not run never reported as PASS)
- [ ] T033 Run mutation gate without `--since` from the worktree; surviving mutant → missing test, never lower threshold
- [x] T034 Run web gates / generated-contract checks for regenerated artifacts
- [x] T035 Frozen-list diff check against plan.md file set (Api root paths retained; no speculative helper file); report any extras rather than silently expanding scope
- [x] T036 Hand PR-body evidence to Quill/Conductor: Gate 1 checkbox state, gate exit codes, limits (process crash between commit and send can lose the event with no durable crash-window detection; no guaranteed receipt; reconnect/refetch is DEV-422; no DEV-422 client; no backplane); do not merge


**Anvil Phase 6 receipt — 2026-10-27**: Applied at `F:/Dev/LamuFlix.worktrees/DEV-330`, branch `feature/DEV-330`, base `8a5b36ac`. Working tree clean before any edit. T031–T032 and T034–T035 are green; **T033 (mutation) is FAILED and is recorded as FAILED, never as PASS** (see below), so the Phase 6 checkpoint is not met.

- T031: close-out static analysis on the exact 39 changed `.cs` files from `git diff --name-only origin/main...HEAD -- '*.cs'` (recorded verbatim in the list below). `run-roslyn-analyzers.ps1 -Files <39>` exit 0 (no CA/IDE warnings); `run-cyclomatic-complexity.ps1 -Files <39> -Threshold 15` exit 0 (0 methods > 15); `run-cyclomatic-complexity.ps1 -Files <39> -Threshold 6` exit 0 (0 methods > 6, refactor ceiling); `run-jetbrains-inspectcode.ps1 -Files <39>` exit 0 (no WARNING+); `dotnet format LamuFlix.sln --verify-no-changes` exit 0. No internal helper was forced by measured complexity, so the Ledger M2 bounded-file-list stop did not trigger and no scope was added.
- T031 exact `-Files` list (39): `src/LamuFlix.Api/EnrichmentHub.cs`, `src/LamuFlix.Api/EnrichmentHubSchemaTransformer.cs`, `src/LamuFlix.Api/EnrichmentStatusChangedDto.cs`, `src/LamuFlix.Api/Program.cs`, `src/LamuFlix.Api/SignalREnrichmentStatusNotifier.cs`, `src/LamuFlix.Core/Ports/IEnrichmentStatusNotifier.cs`, `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs`, `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`, `tests/LamuFlix.IntegrationTests/ApiCorsTests.cs`, `tests/LamuFlix.IntegrationTests/ApiEndToEndFactory.cs`, `tests/LamuFlix.IntegrationTests/ApiEndToEndLibraryTests.cs`, `tests/LamuFlix.IntegrationTests/ApiHostFactory.cs`, `tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs`, `tests/LamuFlix.IntegrationTests/EfMovieRepositoryClaimConcurrencyTests.cs`, `tests/LamuFlix.IntegrationTests/EfMovieRepositoryDeleteTests.cs`, `tests/LamuFlix.IntegrationTests/EfMovieRepositoryPlotTests.cs`, `tests/LamuFlix.IntegrationTests/EfMovieRepositorySurvivorTests.cs`, `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs`, `tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs`, `tests/LamuFlix.IntegrationTests/EnrichmentHubTests.cs`, `tests/LamuFlix.IntegrationTests/EnrichmentOutboxPersistenceTests.cs`, `tests/LamuFlix.IntegrationTests/EnrichmentStatusNotificationPersistenceTests.cs`, `tests/LamuFlix.IntegrationTests/FeaturesConsumerPersistenceTests.cs`, `tests/LamuFlix.IntegrationTests/FeedbackTasteProfileInvalidationTests.cs`, `tests/LamuFlix.IntegrationTests/GenreWeightsIntegrationTests.cs`, `tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs`, `tests/LamuFlix.IntegrationTests/ImportMovieFolderCommandHandlerPersistenceTests.cs`, `tests/LamuFlix.IntegrationTests/MissingMovieFeaturesRecoveryTests.cs`, `tests/LamuFlix.IntegrationTests/MovieCatalogRegistrationTests.cs`, `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`, `tests/LamuFlix.IntegrationTests/OutboxRelayTests.cs`, `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs`, `tests/LamuFlix.IntegrationTests/RecordingEnrichmentStatusNotifier.cs`, `tests/LamuFlix.IntegrationTests/StrandedMovieSweeperTests.cs`, `tests/LamuFlix.IntegrationTests/TasteProfileInvalidationHarness.cs`, `tests/LamuFlix.UnitTests/Enrichment/SignalREnrichmentStatusNotifierTests.cs`, `tests/LamuFlix.UnitTests/Persistence/EfMovieRepositoryConstructorTests.cs`, `tests/LamuFlix.UnitTests/Persistence/PersistenceServiceCollectionExtensionsTests.cs`, `tests/LamuFlix.UnitTests/Persistence/TasteProfileCacheRegistrationTests.cs`.
- T032: `dotnet test LamuFlix.sln` run 1 exit **1** — `LamuFlix.IntegrationTests.OllamaEmbeddingResilienceTests.GenerateAsync_ExhaustedRequestTimeout_MapsToProviderUnavailable` failed (`probe.RequestCount` 5 vs 4), a timing-sensitive test unrelated to DEV-330. Isolated rerun of that single fact exit 0 (1/1 passed). Full `dotnet test LamuFlix.sln` rerun exit **0**: ArchitectureTests 15 passed / 0 failed, Evaluation 82 / 0, UnitTests 1829 / 0, IntegrationTests 940 passed / 2 skipped / 0 failed of 942 (the two by-design skips are `OllamaEmbeddingIntegrationTests.GenerateAsync_SamplePlot_Returns384DimensionalEmbedding` and `OpenApiContractTests.OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether`). Totals 2866 passed / 2 skipped / 0 failed; the first-run failure and its isolated green rerun are both recorded rather than hidden. `run-property-tests.ps1` exit 0 (100 tests across 4 projects; UnitTests 97, IntegrationTests 3). `run-vulnerable-packages.ps1` exit 0 (no vulnerable packages, transitive included). No gate in T032 was SKIPPED or N/A.
- T033: `scripts/run-mutation.ps1` (no `--since`, from the worktree) exit **1 — FAILED**. The gate mutated changed production C# only: `LamuFlix.Api` and `LamuFlix.Infrastructure` are `NOT APPLICABLE` (listed in `harness.yml` `gates.mutation.exclusions` per Constitution IX), so only `LamuFlix.Core.csproj` ran; its only changed production file is the `IEnrichmentStatusNotifier` interface. Stryker created 2044 mutants, 1905 removed by the mutate filter and 139 CompileError, leaving **0 tested mutants**; the script fails on `Total number of tested mutants across all invocations is 0`. No mutant survived, so no missing-test remedy applies, and no threshold, glob, project scope, or flag was changed to make it pass (standing ruling in DEV-294/DEV-296: 0 tested = FAIL, no retry, no scope/flag change without a new Patron ruling). This is a tooling-inconclusive gate result that needs a Keel/Patron ruling; it is not a PASS and is not a code defect this phase can fix.
- T034: `run-web-gates.ps1 -BaseRef origin/main` exit 0 (ESLint, `tsc --noEmit`, Vitest, Vite build); `npm run check:api` under `web/` exit 0 (generated `openapi.json`/`types.ts` drift clean).
- T035: frozen-list check against plan.md Project Structure. The 46-file `origin/main...HEAD` diff contains only plan-listed paths, the plan-sanctioned "strictly necessary repository composition/constructor fixture adaptations", the regenerated artifacts (`web/src/api/openapi.json`, `web/src/api/types.ts`, Verify snapshot), `specs/DEV-330/tasks.md` receipts, and the two Gate 1 documents (`.specify/memory/constitution.md`, `docs/adr/0029-…md`). Extra to the explicit plan list, reported rather than silently absorbed: `tests/LamuFlix.IntegrationTests/RecordingEnrichmentStatusNotifier.cs` (new explicit `IEnrichmentStatusNotifier` test double required by T014's no-no-op-fallback rule). No speculative production helper file; Api paths remain at the flat Api root (no `Hubs/` folder); no file outside the plan's allowed set.
- T036: evidence handed to Conductor (Quill not connected) in the report: Gate 1 checkbox state = approved via PR #264 (recorded in the Phase 1 receipt; `specs/DEV-330/spec.md` packet checkbox intentionally left unchecked per the Phase 1 note), gate exit codes as above, and limits — a process crash between commit and send can lose the event with no durable crash-window detection; no guaranteed receipt; reconnect/refetch is DEV-422; no DEV-422 client; no backplane. Not merged; user owns merge.

**Phase 6 disposition**: T031, T032, T034, T035, T036 complete and green. T033 left **unchecked** because its gate verdict is FAILED (0 tested; tooling-inconclusive on an interface-only Core diff). Phase 6 checkpoint is not claimed as met; outcome reported to Conductor for a Keel/Patron ruling.

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
