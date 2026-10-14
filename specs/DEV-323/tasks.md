# Tasks: DEV-323 Movie Details view (metadata, feature-gated Play)

**Input**: `specs/DEV-323/spec.md`, `specs/DEV-323/plan.md`, `research.md`, `data-model.md`, `contracts/`.

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ present. Phase A only: confirm frozen inputs and prepare the spec PR with the original owner checkbox still open (Gate 1 closed until answered). Phase B build starts only after the spec PR is merged AND the owner checkbox is answered; owner no requires re-plan. DEV-320 codegen tooling plus TanStack Query/Zod integration must be delivered in the build base before any typed web task; if absent, stop and report needs recon. Exact pins and delivered client/handler paths are Phase B recon, never invented here.

**Tests**: Included; the ticket names RTL/MSW plus backend xUnit explicitly.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

## Phase 1: Setup — Phase A spec-PR preparation only (no build)

**Purpose**: Confirm frozen inputs and prepare the spec PR with the open owner checkbox. Nothing here authorizes Phase B execution.

- [ ] T001 Confirm `brief.md`, `CONCLUSIONS.md` owner checkbox (open, unanswered), `ASSUMPTIONS.md`, `spec.md`, `plan.md` present; record HEAD for the spec PR.
- [ ] T002 [P] Verify `.specify/feature.json` still pins `specs/DEV-323` (preserve; do not retarget).

---

## Phase 2: Foundational — Phase B build (requires merged spec PR + answered owner checkbox)

**Purpose**: Backend additive slice plus regenerated contract outputs that all stories depend on. Do not start until the spec PR is merged and the owner checkbox is answered; owner no requires re-plan.

**CRITICAL**: No user story work can begin until this phase is complete. Tasks run strictly in order (no parallel markers): Core, then EF, then API/tests, then OpenAPI, then generated outputs, then web.

- [X] T010 Extend Core `MovieMetadata` additively per data-model.md in `src/LamuFlix.Core/Domain/MovieMetadata.cs` (keep existing fields and behavior).
- [X] T011 Extend `EfMovieCatalog.GetDetailsAsync` projection over stored fields/joins in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` (no schema/migration); preserve missing/partial behavior.
- [X] T012 Extend `MovieMetadataResponse` and mapping in `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` (nullable scalars, people arrays; outer response, route, codes unchanged).
- [X] T013 [US1] Extend backend proofs in order: `EfMovieCatalogDetailsTests.cs`, then `LibraryEndpointsTests.cs`, then `ApiEndToEndLibraryTests.cs` (new fields, null scalars, empty collections, exact frozen shape).
- [X] T014 Regenerate `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` through the existing `OpenApiContractTests` regeneration facility only; prove drift equality (no hand edits; source modification of `OpenApiContractTests.cs` is not preauthorized).
- [X] T015 Generate `web/src/api/types.ts` with delivered DEV-320 tooling only (never hand-edit; no substitute types). BLOCKED if tooling absent: report needs recon and stop. Any Core constructor/test caller adjustments beyond the named files are deferred to Wisp recon; do not invent paths.

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - View complete movie metadata (Priority: P1)

**Goal**: Loaded details view renders all present metadata with conditional badges and placeholder poster.

**Independent Test**: Generated MSW handlers serving full/partial/null GET fixtures with generated contract types and Zod validation; assert per spec US1 scenarios.

- [X] T020 [US1] RTL tests for full render in `web/src/features/movie-details/MovieDetailsView.test.tsx` (title, poster alt, badges, directors, cast).
- [X] T021 [US1] RTL tests for partial/null metadata, placeholder poster, title/format fallback in `MovieDetailsView.test.tsx` (after T020; same file, no parallel work).
- [X] T022 [US1] Implement typed feature API module `web/src/features/movie-details/movieDetailsApi.ts` consuming the delivered DEV-320 openapi-fetch client and generated types through TanStack Query with Zod validation (relative `/api/...` URLs; no duplicate DTOs, competing client, or invented pins/paths).
- [X] T023 [US1] Implement `web/src/features/movie-details/MovieDetailsView.tsx` loaded/metadata states plus `MovieDetailsView.css` (conditional fields/badges, placeholder, notices; copy from ASSUMPTIONS.md).

**Checkpoint**: User Story 1 fully functional and testable independently.

---

## Phase 4: User Story 2 - Feature-gated Play (Priority: P2)

**Goal**: Play button with 204/403/404/error handling and the accessible 403 dialog.

**Independent Test**: Generated MSW handlers with 204/403/404/500 Play responses; assert inline texts, modal open/close/focus, in-flight guard.

- [X] T030 [US2] RTL tests for 204 success, 404 removal, generic error, and in-flight double-press guard in `MovieDetailsView.test.tsx` (after T021; same file, no parallel work).
- [X] T031 [US2] RTL tests for 403 dialog (title/body, focus to Close, Escape/Close dismissal, focus return, focus kept inside) in `MovieDetailsView.test.tsx` (after T030; same file, no parallel work).
- [X] T032 [US2] Implement Play control plus hand-built accessible dialog in `MovieDetailsView.tsx` (always rendered, enabled except in flight; `role="dialog"`, `aria-modal`; no `<dialog>` showModal).

**Checkpoint**: User Stories 1 AND 2 both work independently.

---

## Phase 5: User Story 3 - Entry mount and load states (Priority: P3)

**Goal**: Q5 fallback entry through `App.tsx` with loading/404/error states and unchanged readiness elsewhere.

**Independent Test**: Explicit location set/restore; assert id prop on the details path and readiness elsewhere.

- [ ] T040 [US3] RTL tests for loading, GET 404, and GET error states in `MovieDetailsView.test.tsx` (after T031; same file, no parallel work).
- [ ] T041 [US3] App entry tests in `web/src/App.test.tsx` (initial `/movies/<id>` mounts view with id; other paths keep readiness).
- [ ] T042 [US3] Q5 fallback mount in `web/src/App.tsx` (initial-pathname parse only; no router, no `main.tsx` rewiring beyond existing mount).

**Checkpoint**: All user stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns (implementation checks; final gates are Gauge-owned after Cog refactor)

**Purpose**: Implementation TDD/contract checks plus review readiness. Final gate verdicts belong to Gauge after Cog refactor.

- [ ] T050 Regenerate-and-prove pass: OpenAPI/snapshot drift equality plus web compile/typecheck against generated `types.ts` (TDD/contract check; keep).
- [ ] T051 Implementation analyzer self-check on changed `.cs` files plus `dotnet format --verify-no-changes` (final Roslyn/complexity/InspectCode gates are Gauge-owned after Cog refactor).
- [ ] T052 Implementation-focused tests/TDD checks (meaningful backend/web checks for this slice, T050 contract/typecheck proof kept); report honest verdicts, never SKIPPED/SKIP/NOT APPLICABLE as PASS. Unaccepted property-test exit-2 remains blocking. Full gate battery and mutation exercise are not implementation-owned; final gate verdicts are Gauge-owned after Cog refactor per plan.md:88.
- [ ] T053 Confirm scope exclusions hold (no backdrop/tagline/writers rendering; no new NuGet/competing codegen dependency, schema, playback-backend, flag-endpoint, secrets, legacy MVC, or layer change); verify the approved scope and the recorded owner answer on the merged spec PR (Gate 1). Do not require the checkbox to remain open; owner no requires re-plan before Gate 1.

---

## Dependencies & Execution Order

- **Setup (Phase 1)**: Phase A spec-PR preparation; starts immediately; authorizes no build.
- **Foundational (Phase 2)**: Phase B; depends on merged spec PR + answered owner checkbox (owner no requires re-plan), then Setup. BLOCKS all user stories. Strict order: T010 Core, then T011 EF, then T012 API, then T013 backend proofs, then T014 OpenAPI/snapshot, then T015 generated types. T015 (delivered DEV-320 tooling + stack integration) gates every typed web task.
- **User Stories (Phases 3-5)**: Depend on Foundational; then proceed in story order (P1 to P2 to P3). Same-file test tasks never run concurrently.
- **Polish (Phase 6)**: Depends on all desired user stories being complete; implementation checks only, final gates Gauge-owned.

### Within Each User Story

- Tests MUST be written and FAIL before implementation.
- API module before view; view before entry mount.
- Story complete before moving to next priority.
- Same-file tasks run strictly in order: T020, then T021, then T030, then T031, then T040 in `MovieDetailsView.test.tsx`.

### Parallel Opportunities

- No parallel markers in Phase 2 or in `MovieDetailsView.test.tsx` work. The only parallel-safe task is T002 (different file, no dependency).

