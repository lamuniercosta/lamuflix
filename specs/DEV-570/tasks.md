---
description: "Task list for DEV-570 movie details About section"
---

# Tasks: Movie details About section

**Input**: Design documents from `specs/DEV-570/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q5), `CONCLUSIONS.md`, `ASSUMPTIONS.md`; Maestri notes `DEV-570`, `recon-DEV-570`; `specs/PRODUCT.md`.

**Status**: Gate 1 / T000 is complete and T001 pickup reanalysis is **READY**. See the receipts in `CONCLUSIONS.md`. T002 is next.

**Tests**: Required (`brief.md` Verification; FR-001–FR-012; SC-001–SC-006; SMOKE-001–003). Follow implement / `web-implement` test-first (failing cases before implementation) in each story phase. Use existing `LibraryEndpointsTests`, `OpenApiContractTests` schema assertions + Verify snapshot, and `MovieDetailsView` / adjacent About semantic tests (including null-metadata). Chromium integrated verification uses Test URLs: same-movie screenshot pair; SMOKE-001–002 on enriched/mapped-cast movie; SMOKE-003 on a separate null-metadata movie. No new test framework/dependency. Property-test disposition follows `plan.md` Verification Approach.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/review work is never `[P]` and is **not** Anvil-dispatched.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief Q4 (FR-008; T001 confirms; T008 implements); STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no new dependencies; no LocalPlay execution edits; never hand-edit generated `types.ts` / OpenAPI baseline; never lower harness thresholds. Otherwise-unassigned **local Build** implementation tasks are owned by the generic Build lane (Anvil). Named seats own Spec PR, pickup, Cog refactor, Gauge gates, review, and ship per `task-pipeline` — those are downstream of local Build, not Anvil phases.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` / `web-implement` Build dispatch only (Phases 0–4). They are **not** canonical `task-pipeline` phases. Under task-pipeline Phase 3 step 2, Anvil receives **one local phase per ask, in order** — phases are sequential; do not claim cross-phase parallelism on shared files. Do **not** bundle Gauge or review into an Anvil phase.

**Spec-PR provenance**: After Keel records the plan-challenge freeze, Patron sets `gate1: provisional`; Bernstein coordinates; Rigger prepares/opens the Spec Kit PR; only the user merges (completes Gate 1); Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T001+
Phase 1   T001          (Wisp pickup + Keel reanalysis; FR-008 envelope + sanctioned OpenAPI path) — blocks production edits
Phase 2   T002 → T003 → T004 → T005   (US1 API + contract; assertions before projection; tests green before web)
Phase 3   T006 → T007 → T008 → T009   (US2 About semantics + US3 cast; FR-008 via T008) — after contract green
Phase 4   T010          (US4: same-movie screenshot pair; SMOKE-001–002 enriched; SMOKE-003 separate null-metadata)
—— local Build ends ——
Downstream (not Anvil): Cog /refactor → Gauge Phase 3 step 5 → Gauge pre-pass + Sentry/Ledger/Compass → Keel adjudicates + findings → Phase 5 rebase + gate rerun
```

Ordering per brief Verification and task ordering / Q1–Q5. No dependency cycle. Web phase consumes regenerated types; do not start Phase 3 until Phase 2 contract checkpoint passes. Do not assert hosted merge readiness from Build.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [X] T000 After Keel freeze receipt (PLAN FROZEN) and Patron `gate1: provisional`: **Bernstein** coordinates and **Rigger** prepares/opens the DEV-570 Spec Kit PR for ordinary owner Gate 1; only the user merges (completes Gate 1). After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-570 in `specs/DEV-570/CONCLUSIONS.md`. Receipt only. T001–T010 MUST NOT proceed without verified merge (`brief.md` Closing bar; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon (blocks production edits)

**Purpose**: Confirm authorized seams and sanctioned OpenAPI regeneration path against pickup HEAD.
**Checkpoint**: Written pickup receipt cites file/test seams and regeneration commands; Keel read-only reanalysis clear. T001 itself does not edit production files.

- [X] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree confirming the authorized FR-008 envelope paths (`src/LamuFlix.Api/Endpoints/LibraryResponses.cs`, `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs`, `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`, Verify snapshot path, `web/src/api/openapi.json`, `web/src/api/types.ts`, `web/src/features/movie-details/movieDetailsApi.ts`, `MovieDetailsView.tsx`, adjacent tests, colocated `AboutSection`, scoped `MovieDetailsView.css` if needed, and minimal `CastSection.tsx` sharing if warranted) and recording any demonstrated drift as a **separate follow-up** before production edits. Inventory all typed `MovieMetadataResponse` and `MovieDetailsResponse` consumers, including existing test/mock fixtures. Identify required-field updates, verify ownership of generated `handlers.ts` and `types.ts`, and verify and record the exact export command and npm scripts from `plan.md` Contract regeneration without running the export. Identify the existing LocalPlay launch test suites for T009. Document any out-of-envelope consumer for a fresh Patron ruling before edits. **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. **Strictly read-only reconnaissance and analysis** aside from collecting the receipt artifacts named by the Build lane.

**Checkpoint**: T002+ unlocks only when the Wisp receipt is recorded and Keel pickup analysis is clear (with any authorized drift follow-up amendments already recorded).

---

## Phase 2: User Story 1 - Project About fields on movie details API (Priority: P1) 🎯 MVP

**Goal**: Project `writers`, `genres`, and `collectionName` on existing `MovieMetadataResponse`; regenerate contract artifacts; green endpoint and OpenAPI tests before web.
**Independent Test**: Endpoint JSON assertions + `OpenApiContractTests` schema assertions for present writers/genres arrays and present nullable collectionName + regenerated types agreement (FR-001–FR-003; SC-003–SC-004).
**Order**: T002 → T003 → T004 → T005 after T001. Sequential — shared mapper/tests/generated artifacts. Assertions before projection; regeneration uses T001 path; endpoint and OpenAPI pass before web.

### Tests for User Story 1

- [X] T002 [US1] **Build lane**. After T001: Using implement test-first, **before projection**, author failing assertions in (a) `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` for populated `writers`/`genres`/`collectionName` values and order, empty `writers`/`genres`, explicit null `collectionName` where absent, and preserved null outer `metadata`; and (b) `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` schema assertions requiring `writers` and `genres` as required string arrays and `collectionName` both in the schema `required` list and nullable on `MovieMetadataResponse` (FR-001, FR-003; `brief.md` Verification step 1; Q2).

### Implementation for User Story 1

- [X] T003 [US1] **Build lane**. After T002 authored: Extend `MovieMetadataResponse` and `From` in `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` to project stored writers, genres, and collection name with the Q2 missing-value shape (present empty arrays; present nullable `collectionName`; outer null metadata unchanged) (FR-001; recon lines 16–19, 51). Preserve explicit null JSON for `collectionName` by leaving off `WhenWritingNull`. Make T002 endpoint assertions pass.
- [X] T004 [US1] **Build lane**. After T003: Run the pinned opt-in test in `plan.md` Contract regeneration to refresh the OpenAPI baseline and Verify snapshot, then run `npm run generate:api` from `web/`. Clear the opt-in variable even on failure. Verify required arrays, required-and-nullable `collectionName`, explicit null JSON, and no `WhenWritingNull`. Treat any generated `handlers.ts` change as generator output only (FR-002; SC-003).
- [X] T005 [US1] **Build lane**. After T004: Run `LibraryEndpointsTests` About-field cases and `OpenApiContractTests` (schema assertions + drift), and `npm run check:api` from `web/`; record pass evidence (or an explicit blocker distinct from a passed checkpoint). Do **not** start Phase 3 web until this checkpoint **passes**.

**Checkpoint**: Endpoint tests, OpenAPI schema and drift tests, and `npm run check:api` pass. Do not proceed to Phase 3 until this checkpoint **passes**.

---

## Phase 3: User Story 2 + User Story 3 - About display and cast selection (Priority: P1)

**Goal**: Align Zod parser; render About with ticket order/labels/empty rules; wire mapped/unmapped cast through existing More with; preserve hero cast and trailer placement.
**Independent Test**: Web unit/component **semantic** coverage for FR-004–FR-007 / FR-009 / SC-001–SC-002 / SC-004 (phone-width wrapping is Chromium / FR-006 → T010).
**Order**: T006 → T007 → T008 → T009 after Phase 2 checkpoint. Sequential — shared feature files.

### Tests for User Stories 2 and 3

- [X] T006 [US2] [US3] **Build lane**. After T005: Using `web-implement` test-first, extend `web/src/features/movie-details/MovieDetailsView.test.tsx` and focused adjacent About/parser tests with failing **semantic** cases for full row values/order, missing-writers-only, optional collection, all-rows-empty (About absent), null-metadata (existing notice, About absent), Director/Directors and Writer/Writers by count, heading-labelled `dl`/`dt`/`dd`, comma-separated stored order, blank-only rows and mixed blank/non-blank entries with displayed counts, mapped About name opens existing More with, unmapped name plain text, hero cast selection regression, and About placement after actions/player/launch feedback and before trailer (FR-004–FR-007, FR-009; Q1, Q3; Verification step 2). Cover malformed values and missing required keys among the new fields through the existing details error path. T010 verifies the grey/white tokens and phone-width wrapping.

### Implementation for User Stories 2 and 3

- [X] T007 [US2] **Build lane**. After T006 authored: Update `web/src/features/movie-details/movieDetailsApi.ts` so Zod metadata validation accepts generated `writers`, `genres`, and `collectionName` against `components` types before the view consumes them (FR-002; Q2). Keep the fields required, with nullable `collectionName`. Apply only the additive required-field updates to existing typed test/mock fixtures supported by T001 or compiler evidence under FR-008. Not `[P]` with T008 — parser must land before view wiring on the shared feature.
- [X] T008 [US2] [US3] **Build lane**. After T007: Implement About in `web/src/features/movie-details/MovieDetailsView.tsx` inside the FR-008 envelope confirmed at T001 using one focused colocated `AboutSection`, scoped styling in `MovieDetailsView.css` if needed, and a minimal `CastSection.tsx` change only for real selection-rendering reuse. Enforce FR-004–FR-007 placement, labels (Director/Directors and Writer/Writers), empty rules, and selection behaviour without touching LocalPlay execution. Make T006 pass.
- [X] T009 [US2] [US3] **Build lane**. After T008: Run the About/view and parser tests, `MovieDetailsView.bsPlayer.test.tsx`, and the existing LocalPlay launch tests identified at T001. Record pass evidence (or an explicit blocker distinct from a passed checkpoint).

**Checkpoint**: US2 and US3 independently testable via web semantic tests. Do not proceed to Phase 4 until this checkpoint **passes**.

---

## Phase 4: User Story 4 - Integrated Chromium verification (Priority: P2)

**Goal**: Same-movie windowed + phone-width screenshots of an enriched movie; identifiable SMOKE-001–002 on enriched/mapped-cast movie and SMOKE-003 on a separate null-metadata movie (closes local Build).
**Independent Test**: Screenshot pair on one enriched movie + SMOKE-001–002 + SMOKE-003 (separate null-metadata movie) each with identifiable route/result (FR-006 wrapping; FR-010; SC-005; SMOKE-001–003). Null-metadata component coverage retained from Phase 3.
**Order**: After Phase 3 checkpoint pass. Local Build ends after this checkpoint — no Anvil Gauge/review phase.

- [X] T010 [US4] **Build lane**. After T009: Via the Test URLs portal in Chromium: (1) on the **same** enriched real library movie, capture a windowed screenshot showing About and a phone-width screenshot showing About with wrapping values (FR-006). Inspect existing grey label and white value token reuse; record that movie’s local test route and viewport dimensions; (2) record identifiable SMOKE-001–002 on a movie with the required data and mapped cast (route/result for each); (3) record identifiable SMOKE-003 on a **separate** movie with null metadata (About absent; existing notice remains; route/result). Confirm the Test URLs seed exposes an enriched movie with mapped cast before attempting SMOKE-002. If none is available, record the seed blocker. Preserve both screenshots and smoke notes for review (FR-010; SC-005; Verification step 3). Do not treat unit/component null-metadata coverage as a substitute for SMOKE-003, and do not require SMOKE-003 on the screenshot movie. Record pass evidence or an explicit receipt blocker (blocker ≠ passed delivery checkpoint). Do **not** assert hosted merge readiness.

**Checkpoint**: Local Build integrated-verification checkpoint passes only with both same-movie screenshots and identifiable SMOKE-001–003 observations (with per-observation route/result) preserved. Downstream Cog/Gauge/review follow — not Anvil.

---

## Downstream pipeline (Conductor-owned; not Anvil local phases)

After Phase 4 Build checkpoint **pass**, Conductor sequences (do not dispatch as Anvil `## Phase N:`):

1. **Cog** `/refactor` over the whole build.
2. **Gauge** Phase 3 step 5 gates explicitly: Roslyn, cyclomatic complexity, InspectCode, scoped mutation for production C#, property tests under the standing rule, vulnerable packages, format, `dotnet test`, `scripts/run-web-gates.ps1` exit 0. Apply the scopes, gate labels and property-test task-note condition in `plan.md` Verification Approach (FR-011; brief Verification step 4).
3. **Gauge** pre-pass, then **Sentry / Ledger / Compass** Size M three-axis review. **Keel** adjudicates over all three axis reports and owns the findings artifact. Every verified in-scope finding through Low MUST close within two rounds and at most two fix commits per round, or be reported blocked; each accepted fix independently verified on the reviewed head (FR-012; Q5; SC-006).
4. **Phase 5** rebase and Gauge gate rerun on the rebased head (same skip/opt-out/failure rules, including the standing property-test exit-2 condition above). Hosted merge readiness is a later ship bar — not claimed from Build.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0**: Blocks all Build work
- **Phase 1**: Read-only pickup (FR-008 envelope + sanctioned OpenAPI path); blocks production edits until receipt + Keel reanalysis clear
- **Phase 2**: API/contract MVP (assertions before projection; endpoint+OpenAPI green before web); blocks Phase 3 until checkpoint **pass**
- **Phase 3**: About UI semantics + cast interaction after contract green; blocks Phase 4 until checkpoint **pass**
- **Phase 4**: Integrated Chromium verification (same-movie screenshot pair; SMOKE-001–002 enriched; SMOKE-003 separate null-metadata); closes local Build
- **Downstream**: Cog refactor → Gauge Phase 3 step 5 → Gauge pre-pass + Sentry/Ledger/Compass → Keel adjudicates + findings → Phase 5 rebase + gate rerun (not Anvil)

### User Story Dependencies

- **US1 (P1)**: After Phase 1 — no dependency on other stories
- **US2 (P1)**: After US1 contract checkpoint — consumes generated types; component tests cover semantics; Chromium verifies phone-width wrapping
- **US3 (P1)**: Same Phase 3 as US2 — shares About cast rendering; independently asserted by mapped/unmapped tests
- **US4 (P2)**: After US2/US3 checkpoint — same-movie screenshot pair + SMOKE-001–002 + separate-movie SMOKE-003 + FR-006 wrapping

### Parallel Opportunities

- No `[P]` tasks in this draft: mapper, generated artifacts, and movie-details feature files are shared sequential seams.
- Spec PR coordination (T000) is organizationally parallel to nothing in Build until merge receipt exists.

### Parallel Example

```text
# None claimed — Phase 2 and Phase 3 are strictly sequential on shared files.
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 0–1
2. Complete Phase 2 (API + contract; assertions before projection; endpoint+OpenAPI green)
3. **STOP and VALIDATE**: Contract green independently
4. Continue to Phase 3 for user-visible About

### Incremental Delivery

1. Spec PR merge → pickup (FR-008 / T001)
2. API/contract → demoable wire shape
3. About UI + cast → user-visible MVP (FR-008 / T008)
4. Integrated Chromium verification (same-movie screenshots + SMOKE-001–002 + separate SMOKE-003) → closes local Build
5. Downstream Cog → Gauge → Keel adjudication/findings → Phase 5 rebase/gates (not Anvil; no hosted-merge claim from Build)

### Notes

- `[Story]` labels map to spec.md user stories US1–US4
- Verify tests fail before implementing within each story phase
- Commit after each task or logical group under `DEV-570 - {subject}`
- Stop at any checkpoint to validate independently
- Avoid: vague tasks, out-of-envelope edits, hand-edited generated types, LocalPlay changes, bundling Gauge/review into Anvil phases, asserting hosted merge readiness from Build
