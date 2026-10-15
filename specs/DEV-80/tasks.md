# Tasks: DEV-80 Folder Import Dialog and Smart Polling

**Input**: Design documents from `specs/DEV-80/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), brief.md (Q1-Q7, R1-R4, A1-A6), recon-DEV-80 and recon-DEV-80-3

**Tests**: Generated status contract and projection tests (DTO, `EfMovieCatalog`, OpenAPI baseline, Verify snapshot, `OpenApiContractTests`); import 202 + Location, 422/400, and network-error behavior; polling starts with Pending and stops with none; retry shown only for NotFound/Failed with 202/404/409 paths; MSW handlers typed through `openapi-msw` using generated paths. Tests are required by the brief Q7 matrix and are included below.

**Organization**: The backend contract/projection and generated-client/provider foundations pass tests independently; the first combined US1/US2 frontend story handoff completes after T017 with `App.test.tsx` and `ImportDialog.test.tsx` passing, followed by the polling and retry handoffs with dependencies and typed MSW fixtures. Order follows the brief (brief.md:57-60 task ordering; Q7 order brief.md:147): T005 regenerates the OpenAPI baseline plus Verify snapshot, T006/T007 prove the generated contract and mapping on those regenerated artifacts, T008 generates and commits types.ts from the regenerated baseline, then T009/T010/T010A establish the typed client, provider, and MSW handlers; sequential order T011 -> T012 -> T013 -> T014 -> T015 -> T016 -> T013A -> T017 with no parallel markers and no handoff with knowingly failing deferred tests; list/dialog/poll/retry follow in that order, then tests and gates.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel only when files and dependencies are disjoint; no task below carries [P] because each pair in the same phase shares a file or builds on its predecessor
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Phase A draft and analyze context only; no code setup runs here

- [ ] T001 Confirm `specs/DEV-80/brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `spec.md`, `plan.md` are present as Phase A decision inputs. Implementation prerequisites before any code task: Gate 1 merged, Conductor and Rigger-provisioned Phase B worktree on branch `feature/DEV-80`, completed Wisp pickup drift check, and Keel re-analysis. No delivery worktree receipt is claimed here; Anvil never performs recon. `Palette design.md` is awaited before the UI plan challenge per the brief. Leave T001 unchecked until Quill corrections are read back and Keel confirms consistency.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Status contract that every later slice builds on (brief ordering step 1)

**CRITICAL**: No user story work can begin until this phase is complete

- [X] T002 [Foundation] Carry status in the summary port record `src/LamuFlix.Core/Ports/MovieSummary.cs` (positional record; update the known constructor call in `tests/LamuFlix.UnitTests/Features/Library/BrowseMoviesQueryHandlerTests.cs:29`; constructor inventory bounded to recon-DEV-80:10.2/10.5 roots, not exhaustive)
- [X] T003 [Foundation] Project the existing persisted status in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` (no schema change or migration; column already exists)
- [X] T004 [Foundation] Add `enrichmentStatus` to `MovieSummaryResponse` in `src/LamuFlix.Api/Endpoints/LibraryResponses.cs`, serialized by name (`Pending|Enriched|NotFound|Failed`), and expose it in the browse response mapping in `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs` (no other DTO field, route, or details change). Add the approved `src/LamuFlix.Api/EnrichmentStatusSchemaTransformer.cs` (`IOpenApiSchemaTransformer`, scoped to `EnrichmentStatus` only, emitting `type: string` with `enum [Pending, Enriched, NotFound, Failed]`) and the approved `src/LamuFlix.Api/BrowseMoviesOperationTransformer.cs` (`IOpenApiOperationTransformer`, applied to `BrowseMovies` only, setting `sort`/`direction`/`page`/`pageSize` names, `Required = true` each, `sort` string enum `Title|Year|Rating|Runtime`, `direction` string enum `Ascending|Descending`, `page` integer minimum 1, `pageSize` integer 1..100; other ten parameters, binding, validation, route, runtime, and UI unchanged), both registered in the existing `AddOpenApi` options in `src/LamuFlix.Api/Program.cs` via `AddSchemaTransformer` / `AddOperationTransformer` (brief R2/R4, recon-DEV-80:10.1, recon-DEV-80-3:22-27).
- [X] T005 [Foundation] Regenerate the contract baseline `web/src/api/openapi.json` and the Verify snapshot `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together via the opt-in test `OpenApiContractTests.OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` (recon-DEV-80:10.4): set `$env:LAMUFLIX_REGENERATE_OPENAPI = 'true'`, run `dotnet test tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj --filter FullyQualifiedName~OpenApiContractTests.OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether`, then remove the switch. Retain the independent drift check and the generated TypeScript step; no manual snapshot acceptance and no generated-file edits.
- [X] T006 [Foundation] Update `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` expectations for the generated contract (owns schema/baseline/snapshot proof for both; no new named operation; `LibraryEndpointsTests.cs:538-550` exact route set unchanged): `enrichmentStatus` schema is `type: string` with `enum` exactly `[Pending, Enriched, NotFound, Failed]`; the `BrowseMovies` operation carries parameters `sort`/`direction`/`page`/`pageSize`, each required, with `sort` enum `Title|Year|Rating|Runtime`, `direction` enum `Ascending|Descending`, `page` integer minimum 1, `pageSize` integer 1..100; operation set and unrelated parameters unchanged (runs after the T005 baseline/Verify regeneration on those artifacts, before the T008 `types.ts` generation and T009/T010/T010A typed client, provider, and MSW use)
- [X] T007 [Foundation] Prove the browse status mapping across three named paths (runs after T006 on the shared contract files; not parallel with T006): (a) `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` — update the `RecordingMovieCatalog` constructor (`:688`) and the `ShouldCarryExactly(item, id, title)` exact-shape assertion (`:363-365`, helper `:610-611`) to `id/title/enrichmentStatus`; DTO/HTTP mapping proof only, since `RecordingMovieCatalog` (`:582-584,674-689`) never runs `EfMovieCatalog`; (b) `tests/LamuFlix.IntegrationTests/EfMovieCatalogBrowseTests.cs` — real Postgres/Testcontainers projection proof (`new EfMovieCatalog(context)` at `:252`, `:293`; extend the `:233-272` and `:274-310` browse tests to assert the projected status, not titles only); (c) `tests/LamuFlix.IntegrationTests/ApiEndToEndBrowseTests.cs` — end-to-end item exact-shape proof over HTTP against real Postgres (update `ShouldCarryExactly(item, id, title)` at `:72`, helper `:210-211`, to `id/title/enrichmentStatus`)

**Checkpoint**: Foundation ready - status flows from the persisted column through port, projection, and response into the committed baseline and snapshot with passing contract proof; story slices build on this

---

## Phase 3: Generated Client and Provider (brief ordering step 2)

**Purpose**: Typed client and sole cache layer every web slice builds on

- [ ] T008 [Foundation] Add approved npm names to `web/package.json` with a `generate:api` script writing `web/src/api/types.ts` from `web/src/api/openapi.json` (`@tanstack/react-query` and `openapi-fetch` runtime; `openapi-typescript` and `openapi-msw` dev; no version pinned in this task; Zod is not added), then commit generated `web/src/api/types.ts` and the `web/package-lock.json` result (never hand-edit `types.ts`; runs after the T005 regeneration with T006/T007 contract proof, before T009/T010/T010A typed client, provider, and MSW use)
- [ ] T009 [Foundation] Create typed `openapi-fetch` client in `web/src/api/client.ts` from the generated paths (no hand-rolled DTO types)
- [ ] T010 [Foundation] Wrap `App` with `QueryClientProvider` in `web/src/main.tsx` as the single client state/cache layer (no other state library, no router)
- [ ] T010A [Foundation] Create the generated-path browse handlers in `web/src/test/handlers.ts` from the generated `types.ts` paths via `openapi-msw` and register them in `web/src/test/server.ts`, preserving the existing MSW harness contract (`onUnhandledRequest: error`, reset-after-each, close-after-all per recon-DEV-80:59); handlers consume the corrected generated query types (recon-DEV-80-3:29-33) and assert the A6 standing wire query `sort=Title&direction=Ascending&page=1&pageSize=20` (brief R3) with no handwritten DTOs, casts, or stale PascalCase workaround; the historical optional-PascalCase baseline defect is superseded by the corrected contract. Every US1-US4 test builds on these handlers

**Checkpoint**: Generated client committed and provider wired; behavior slices can now build queries and mutations on them

---

## Phase 4: User Story 1 - Browse the minimum library list (Priority: P1)

**Goal**: Library screen renders the browse first page with title, status badge, and eligible retry action per row, plus the empty-library copy

**Independent Test**: Render against a browse response and verify rows, badges, retry visibility, and the empty copy

- [ ] T011 [US1] Create `web/src/library/libraryQueries.ts` with the browse query over the typed `openapi-fetch` client (no hand-rolled DTO duplicates): send the A6 standing first-page wire query `sort=Title&direction=Ascending&page=1&pageSize=20` through the corrected generated query types (brief R3/R4, recon-DEV-80-3:29-33; server requires lowercase keys, `page=1`, case-sensitive values, `pageSize` 1..100; omission returns 422 per recon-DEV-80:10.3); no handwritten DTOs, casts, or stale PascalCase workaround; no sort UI or paging controls (builds on the T010A handlers; not parallel with T010A)
- [ ] T012 [US1] Create `web/src/library/LibraryView.tsx` rendering one row per item (title, status badge text per ASSUMPTIONS.md A3, `Retry` only for NotFound/Failed), server browse order kept, empty copy per A4 (builds on T011; not parallel with T011)
- [ ] T013 [US1] Rewrite `web/src/App.tsx` as the library screen host with the `Import folder` button and rewrite `web/src/App.test.tsx` to cover the library-host browse behavior against the T010A browse handlers (existing `web/src/test/` harness contract intact; builds on T011-T012; dialog integration and dialog-dependent assertions deferred to T013A): assert the `Import folder` button presence, rows with badges and the empty copy, row-specific `Retry` accessible names (`Retry <title>`), and the list polite live region announcing status changes without moving focus

**Checkpoint**: User Story 1 list host renders rows, badges, retry visibility, and the empty copy; dialog integration and dialog-dependent assertions are deferred to T013A, so Phase 4 is not an independent Anvil story handoff

---

## Phase 5: User Story 2 - Import a movie folder (Priority: P1)

**Goal**: Import dialog submits the folder path and on 202 closes and prepends the Pending cached row; 422/400/network errors show inline

**Independent Test**: Submit valid, invalid, malformed, and failing requests and verify dialog close plus prepend, or the matching inline error with the dialog kept open

- [ ] T014 [US2] Create `web/src/library/ImportDialog.tsx` with dialog title, input label, and Import/Cancel actions per ASSUMPTIONS.md A2
- [ ] T015 [US2] Add the import mutation to `web/src/library/libraryQueries.ts`: on 202 close the dialog and prepend `{ id, title, enrichmentStatus: 'Pending' }` to the cached browse query (`id` from `Location`, `title` from the last folder-path segment per A1); on 422 show the `folderPath` problem inline; on 400 or network failure show a generic inline error
- [ ] T016 [US2] Extend `web/src/test/handlers.ts` with `openapi-msw` handlers from generated paths covering import 202 + Location, 422, 400, and network failure (same file as T021; not parallel with T021)
- [ ] T013A [US1/US2] Wire and test the actual dialog in `web/src/App.tsx` with `web/src/library/ImportDialog.tsx` and cover all dialog-dependent `App.test.tsx` assertions against the T010A browse handlers plus T016 import handlers (depends on T013 and T014-T016; runs after T016 and before T017; not parallel with T016): assert opening, dialog title/label/actions per US2-AC1, initial `Folder path` focus, Tab/Shift+Tab containment, Escape/Cancel, focus return to `Import folder`, and successful import-202 closure
- [ ] T017 [US2] Add `web/src/library/ImportDialog.test.tsx` covering import 202 plus Location, 422/400 errors, and network-error behavior with observable assertions (builds on T014-T016 and T013A; not parallel with T013A): assert the US2-AC1 open-then-show-labels flow (`Import folder` opens the dialog showing title `Import a movie folder`, label `Folder path`, actions `Import`/`Cancel`); Enter submits the form; inline 422/generic errors are exposed via `aria-describedby` + `role="alert"`; Escape/Cancel closes with focus returned to `Import folder`, including the 202 close

**Checkpoint**: First combined US1/US2 frontend story handoff: T014-T016, T013A, and T017 complete with `App.test.tsx` and `ImportDialog.test.tsx` passing

---

## Phase 6: User Story 3 - Poll while any Pending is in view (Priority: P1)

**Goal**: Browse query refetches every 2 s only while a Pending item is present, with background refetch off

**Independent Test**: Seed cached browse data with and without Pending and verify the interval arms and disarms

- [ ] T018 [US3] Set the browse query `refetchInterval` function in `web/src/library/libraryQueries.ts` to return `2000` while any current browse item has `enrichmentStatus === 'Pending'`, else `false`, keeping background refetch off
- [ ] T019 [US3] Add `web/src/library/libraryQueries.polling.test.tsx` proving polling starts with a Pending row and stops when none remain, with observable background-refetch-off coverage (spec.md:58, FR-005) and 2-second reconciliation of the Pending row to its server state (builds on T018; not parallel with T018): assert reconciled status announces through the polite live region and focus is not stolen on reconcile

**Checkpoint**: User Story 3 polls conditionally with proof on both arms

---

## Phase 7: User Story 4 - Retry enrichment for NotFound/Failed rows (Priority: P1)

**Goal**: Retry appears only on NotFound/Failed rows; 202 returns the row to Pending and re-arms polling; 404/409 shows an inline row error with invalidation

**Independent Test**: Render each status, trigger retries, and verify visibility plus all three outcomes

- [ ] T020 [US4] Add the retry mutation to `web/src/library/libraryQueries.ts`: POST `/api/movies/{id}/enrichment`; on 202 set that row to `Pending` in cache; on 404/409 show an inline row error and invalidate the browse query
- [ ] T021 [US4] Extend `web/src/test/handlers.ts` with `openapi-msw` retry handlers covering 202, 404, and 409 (same file as T016; not parallel with T016)
- [ ] T022 [US4] Add `web/src/library/LibraryView.retry.test.tsx` proving retry appears only for NotFound/Failed rows plus the 202/404/409 paths with observable assertions (builds on T020-T021; not parallel with T021): each row `Retry` carries the row-specific accessible name (`Retry <title>`); 404/409 inline row errors are exposed via the described alert pattern. No skeleton, list-level manual retry, or poll-error behavior is added (out of scope per the round 1 adjudication).

**Checkpoint**: User Story 4 retries with re-armed polling or inline row error, all covered

---

## Phase 8: Gates

**Purpose**: Stage-appropriate verification; no code changes except gate-driven fixes via Cog

- [ ] T023 Run `scripts/run-web-gates.ps1` (lint, TypeScript check, Vitest, build) and the backend gates applicable to the touched C# files at their pipeline stages, with pre-PR mutation requirements; PASS, SKIPPED (scope-empty), configured SKIP, mutation N/A, and Could not run stay distinct and thresholds are never lowered
