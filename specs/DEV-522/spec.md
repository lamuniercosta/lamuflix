# Feature Specification: Stop duplicate movie imports

**Feature Branch**: `feature/522-spec`
**Created**: 2026-11-17
**Status**: gate1: provisional
**Input**: Ticket DEV-522 (bug; size M); grill-closed `brief.md` (Q1–Q7); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-522`, `recon-DEV-522` (ticket at line 24; governing rules 28–31; targeted shell evidence 41–75; HEAD `2c8f0504b3c2550c8fd655eb58457fa9658f3e25`); `specs/PRODUCT.md`.

## Owner question — Gate 1

No current ticket reduction or constitution departure is proposed: Patron ca667dc7 corrects Q4 to require investigation of the actual historical trigger as well as mechanism reproduction, stopping needs recon if historical evidence is unavailable. Mechanism-only evidence cannot close Done-when 3; any future proposal to accept it requires an owner checkbox on the PR and blocks Gate 1 until answered. Ordinary owner Gate 1 remains pending until the separate Gate 1 process. Quill does not invent PLAN FROZEN or a structural checkbox. Ticket-delivery changes or necessary constitution departures remain §2.3 owner escalations, never closed by a Patron answer alone (`brief.md` Mandatory investigation gate).

## Clarifications

### Session 2026-11-17

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope, identity policy, additive failure contract, investigation gate, file envelope, test/gate ordering, and review loop are frozen in brief Q1–Q7. Historical scan/rebuild/reimport trigger remains unproven (`recon-DEV-522` lines 74–75; brief Mandatory investigation gate). Exact production patches stay conditional on Q4 evidence. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Gaps Quill cannot close from brief/recon go to `DRAFTING_RECEIPT.md` as `needs decision:` / `needs recon:`.

## Scope

**In scope** (`brief.md` Frozen scope / Approach and behavior / Mandatory investigation gate / Test strategy; Q1–Q7; ticket Done when at `recon-DEV-522` line 24):

1. **Mandatory pre-implementation investigation (blocking)**: Reproduce repeat-import and late-IMDb paths against a real database; capture sanitized logs and row/constraint outcomes; distinguish demonstrated defect mechanism from the unproven historical scan/rebuild/reimport trigger; obtain Rigger verified YouTrack causal-evidence receipt under Patron ruling. Investigate available historical logs/database rows to classify the actual scan/rebuild/reimport trigger; mechanism reproduction alone cannot satisfy the root-cause AC. Historical evidence unavailable -> stop needs recon (Patron ca667dc7 supersedes original Q4). Inability to reproduce or missing required evidence stops with `needs recon:`; no invented cause or waiver (`brief.md` Q4).
2. **Import skip/reuse by known identity**: Identify an existing movie using library path/key and IMDb ID when already available to the operation. Skip and reuse; never silently merge/delete; never add an external metadata lookup at import to discover an ID (`brief.md` Approach 1–2; Q2).
3. **Race-safe one-row / no-second-enrichment-enqueue**: Preserve database uniqueness as concurrency backstop. Repeated or concurrent imports of the same known identity leave one persisted movie, return a deterministic skipped/already-present result through existing import semantics, and do not enqueue a second enrichment. Preserve existing failed-enrichment retry path; skipping a duplicate does not remove intended retry behavior (`brief.md` Approach 2; Q2/Q5).
4. **Late-IMDb collision on a distinct path**: When IMDb identity is discovered only during enrichment for a distinct path, settle to `Failed` with category `duplicate_imdb_id` and the existing canonical caller-safe message; no automatic deletion/merge; not a silent unenriched gap or falsely successful result (`brief.md` Approach 3; Q2). Change enrichment settlement only if reproduction proves existing late-failure settlement needs repair (`brief.md` Q5).
5. **Additive caller-safe failure projection**: Existing library summary/detail JSON responses gain nullable string fields `failureCategory` and `failureMessage`. For `Failed`, project the existing category code and its canonical caller-safe message when available. For all non-Failed states both fields are null. Never persist or expose raw exception text. Preserve status values and routes (`brief.md` Approach 4; Q3).
6. **Contract generation then UI**: Generate OpenAPI from C# DTOs and TypeScript from OpenAPI before web consumption. No handwritten generated-type duplicates or manual generated-file edits. Show the canonical failure message alongside `Failed` in existing MovieCard and the failed-enrichment section of BulkImportDialog. When details are null, retain the existing `Failed` label without inventing a cause (`brief.md` Approach 5–6; Q3/Q7; `ASSUMPTIONS.md`).
7. **Tests and gates**: Coverage and gate expectations in `brief.md` Test strategy / Task ordering / Gate expectations (Q6). Closing bar and 2×2 review caps per Q7. Size M requires all three review axes.

**Out of scope**:

- Automatic historical merge, cleanup, or deletion of existing duplicates.
- External metadata calls added at import to discover an IMDb ID.
- New NuGet/npm dependency, project, top-level folder, architectural layer, port, schema migration, index relaxation, or LocalPlay / secrets / `Process.Start` change (`brief.md` Frozen scope; Q5; `PRODUCT.md` §5).
- Inventing a public import-contract shape change beyond carrying existing import results; any such change needs a separate ruling (`brief.md` Q5).
- Inferring the historical incident cause from static code alone (`recon-DEV-522` lines 74–75).
- Claiming Gate 1, Build/runtime acceptance, or merge clearance from the plan freeze.
- New ADR (brief: no new port or constitutional departure commissioned).
- Broadening to another worktree; unrelated file rewrite/deletion.

**Precedence**: Ticket Problem / Wanted / Done when sections (`recon-DEV-522` line 24) are already approved authority (`PRODUCT.md` §3). Brief Q1–Q7 and `CONCLUSIONS.md` govern plan boundaries. Only Keel declares PLAN FROZEN after independent analyze/reread. Gate 1 is a separate process.

The following four phrases are spec-local explanatory labels, prohibited as new code/domain identifiers. The API fields express existing EnrichmentFailureCategory Code/SafeDescription; no new CONTEXT term is required (Patron ca667dc7).

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md or ADR expansion)*

**Known import identity**
Library path/key identity always considered at import, plus IMDb identity when that ID is already available to the import operation — used to skip/reuse an existing movie rather than create a second row.
_Avoid_: inventing an IMDb ID via external metadata at import; silent merge/delete

**Late IMDb discovery**
IMDb identity learned only during enrichment for a distinct library path that had no known IMDb identity at import time.
_Avoid_: treating this as a successful duplicate import; auto-deleting or merging rows

**Caller-safe failure projection**
Nullable `failureCategory` / `failureMessage` on existing library summary/detail responses: for `Failed`, the existing category code (including `duplicate_imdb_id`) and its canonical safe message when available; both null for non-Failed; never raw exception text.
_Avoid_: new status enum values; new routes; inventing fallback wording when details are null

**Defect-mechanism evidence**
Real-DB reproduction of repeat-import and late-IMDb paths with sanitized logs and row/constraint outcomes, recorded via Rigger in YouTrack; distinct from proof of the historical scan/rebuild/reimport trigger.
_Avoid_: labeling static code references as causal proof of the historical incident

Consumed vocabulary (already in use; not redefined): EnrichmentStatus (`Pending` / `Enriched` / `NotFound` / `Failed`), `EnrichmentFailureCategory` / `duplicate_imdb_id`, library path/key uniqueness, MovieCard, BulkImportDialog.

## What the User Will See *(mandatory)*

- **Where**: Library grid / movie card (`MovieCard`)
- **What the user sees or does**: When a movie’s enrichment status is `Failed` and a caller-safe failure reason is available, the card shows that canonical message alongside the existing Failed presentation. When failure details are null, the existing Failed label remains (no invented cause).
- **What it needs to show up**: Default Compose stack; at least one library movie settled to Failed with an available failure category/message (for example `duplicate_imdb_id`).

- **Where**: Bulk import dialog, failed-enrichment section (`BulkImportDialog`)
- **What the user sees or does**: Failed enrichments continue to appear in the existing failed section; when a caller-safe reason is available it is shown; null details keep the existing Failed label; existing retry control remains available where the product already exposes it.
- **What it needs to show up**: Default Compose stack; a bulk/library import that produces or surfaces a Failed enrichment with available reason, or an existing Failed row visible in that section.

### Smoke Test *(mandatory when something is visible)*

Acceptance IDs only — verified by Vitest + React Testing Library **component regressions** in `web/src/library/MovieCard.test.tsx` and `web/src/library/BulkImportDialog.test.tsx` (tasks T010/T012). They are **not** browser e2e, not executed browser smoke tests, and **must not** add `web/e2e/*.e2e.ts` files (`brief.md` Post-grill drafting clarification; `CONCLUSIONS.md` post-grill drafting ruling; Constitution Principle IX `constitution.md:707-734` and Technology Stack Constraints `constitution.md:760` impose no browser-e2e mandate). Real-DB and serialized API coverage stay in separate Testcontainers / `WebApplicationFactory` suites (T009 and earlier persistence tasks).

- **SMOKE-001** (MovieCard component regression, T010/T012): **Given** a library movie whose enrichment is Failed with available `duplicate_imdb_id` reason, **When** the card is rendered with that Failed state, **Then** it shows Failed together with the canonical caller-safe message (not raw exception text); when details are null, the existing Failed label remains without an invented cause.
- **SMOKE-002** (BulkImportDialog failed-section component regression, T010/T012): **Given** a Failed enrichment in the bulk-import failed section with available reason, **When** that failed-enrichment section is rendered, **Then** the canonical caller-safe message is shown beside Failed; when details are null, the existing Failed label remains without an invented cause.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Defect-mechanism evidence before fixes (Priority: P1)

Operators/implementers investigate historical log/database evidence for the actual trigger and separately reproduce repeat-import and late-IMDb paths against a real database, capture sanitized evidence, and obtain the Rigger YouTrack receipt before production edits.

**Why this priority**: Ticket Done-when requires cause evidence; brief Q4 makes investigation a hard gate before implementation pickup (`brief.md` Mandatory investigation gate; Task ordering step 3).

**Independent Test**: Rigger verified ticket receipt plus historical log/database evidence establishing the actual trigger and separate sanitized mechanism reproduction logs/row/constraint outcomes — without requiring import/UI code changes to be finished.

**Acceptance Scenarios**:

1. **Given** a real database and the current import/enrichment paths, **When** repeat-import and late-IMDb paths are reproduced, **Then** sanitized logs and row/constraint outcomes documenting the defect mechanism are captured.
2. **Given** that reproduction evidence, **When** Patron rules on the evidence text, **Then** Rigger alone records the cited causal evidence in YouTrack and supplies the verified receipt.
3. **Given** inability to reproduce a failing mechanism or unavailable required evidence, **When** investigation completes, **Then** work stops with `needs recon:` — no invented cause, waiver, or production fix selection.

---

### User Story 2 - Idempotent import of known identity (Priority: P1) 🎯 MVP

Importing the same movie again by existing library path/key, or by IMDb ID already known to the operation, reuses the existing movie: one persisted row, deterministic skipped/already-present result, no second enrichment enqueue; concurrent conflicts still leave one row.

**Why this priority**: Ticket Done-when “twice importing leaves one movie”; brief Approach 1–2 and Q2.

**Independent Test**: Single/bulk rerun and concurrency Testcontainers/unit coverage show one row, skipped/already-present semantics, and no duplicate enqueue — without requiring UI/contract work to be finished.

**Acceptance Scenarios**:

1. **Given** a movie already imported at a library path/key, **When** single-folder import runs again for that path/key, **Then** one persisted movie remains, the result is deterministic skipped/already-present under existing import semantics, and no second enrichment is enqueued.
2. **Given** a bulk/library import that includes a path already present, **When** the bulk operation runs, **Then** the existing movie is reused/skipped without creating a duplicate row or second enqueue; request-local skip alone does not replace durable identity checks (`recon-DEV-522` line 49).
3. **Given** an import operation that already knows an IMDb identity held by an existing movie, **When** import would otherwise create another row, **Then** the existing movie is reused and no second enrichment is enqueued.
4. **Given** concurrent imports of the same known path/key or known IMDb identity, **When** both complete, **Then** uniqueness/conflict translation leaves one persisted movie and no second enrichment enqueue (orchestration unit tests alone do not satisfy this).
5. **Given** existing failed-import/enrichment retry behavior, **When** duplicate skipping is introduced, **Then** intended retry semantics remain available independently of duplicate skipping.

---

### User Story 3 - Truthful late-IMDb Failed settlement (Priority: P1)

A distinct path whose IMDb identity collides only during enrichment settles to Failed with `duplicate_imdb_id` and the canonical safe message, not a silent gap or success; no automatic merge/delete.

**Why this priority**: Ticket Done-when “duplicate IMDb id no longer leaves a movie unenriched without a visible reason”; brief Approach 3; Q2 boundary explicitly approved.

**Independent Test**: Enrichment settlement coverage (extend existing suites only if reproduction proves repair is needed) shows Failed + `duplicate_imdb_id` + canonical message — without requiring the additive library DTO/UI to be finished.

**Acceptance Scenarios**:

1. **Given** a distinct library path imported without a known IMDb identity that later resolves to an IMDb ID already held by another movie, **When** enrichment processes that collision, **Then** the movie settles to Failed with category `duplicate_imdb_id` and the existing canonical caller-safe message.
2. **Given** that late collision, **When** settlement completes, **Then** no automatic deletion or merge of library rows occurs and the outcome is not reported as successful enrichment.

---

### User Story 4 - Visible caller-safe failure reason (Priority: P2)

Library summary/detail responses expose nullable `failureCategory` / `failureMessage`; generated clients consume them; MovieCard and BulkImportDialog show the canonical message beside Failed, with null fallback to the existing Failed label.

**Why this priority**: Closes the proven visibility gap (`recon-DEV-522` lines 62–66) after truthful Failed settlement exists; brief Approach 4–6; Q3.

**Independent Test**: Real API JSON assertions across Failed and non-Failed states (separate from UI); Vitest + React Testing Library component regressions on MovieCard and BulkImportDialog for Failed safe reason / null fallback / no raw exception text (SMOKE-001/002 via T010/T012 — never browser e2e) — after C# DTO → OpenAPI → TypeScript generation.

**Acceptance Scenarios**:

1. **Given** a Failed movie with an available failure category, **When** library summary and detail JSON are returned, **Then** `failureCategory` and `failureMessage` carry the category code and canonical caller-safe message (never raw exception text).
2. **Given** a movie in Pending, Enriched, or NotFound, **When** those responses are returned, **Then** both failure fields are null.
3. **Given** Failed with unavailable/null failure details, **When** summary/detail JSON is returned, **Then** both fields are null (nullable fallback).
4. **Given** regenerated OpenAPI and TypeScript, **When** MovieCard and the BulkImportDialog failed section are exercised as Vitest + RTL component regressions, **Then** the canonical message appears when available in both views; when null, the existing Failed label remains without invented cause wording (SMOKE-001/002; not browser e2e).

### Edge Cases

- Concurrent path/key or known-IMDb imports racing uniqueness constraints.
- Bulk import mixing new paths, already-imported paths, and retryable failures in one request.
- Late IMDb collision on a distinct path (Failed, not merge).
- Failed with null failure details → retain existing Failed label (`ASSUMPTIONS.md`).
- Raw provider/exception text must never appear in persisted failure fields or API/UI.
- Investigation cannot reproduce a failing mechanism → stop; do not invent cause or proceed to fix selection.
- Existing ports cannot support race-safe reuse → stop with needs recon/decision; do not add a port silently (`brief.md` Q5).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Before production fix selection/implementation, the team MUST investigate historical logs/database rows for the actual scan/rebuild/reimport trigger, stopping needs recon if that evidence is unavailable, and separately reproduce repeat-import and late-IMDb paths against a real database, capture sanitized logs and row/constraint outcomes, distinguish reproduction from independently evidenced historical trigger, and obtain a Rigger verified YouTrack causal-evidence receipt under Patron ruling (`brief.md` Q4).
- **FR-002**: If reproduction finds no failing mechanism or required evidence is unavailable, implementation MUST stop with `needs recon:`; MUST NOT invent a cause or waive the root-cause AC (`brief.md` Q4).
- **FR-003**: Import MUST identify an existing movie by library path/key and by IMDb ID when that ID is already available to the operation, then skip and reuse that movie (`brief.md` Q2).
- **FR-004**: Import MUST NOT silently merge or delete movies, and MUST NOT add an external metadata lookup at import to discover an IMDb ID (`brief.md` Approach 1; Frozen scope).
- **FR-005**: Repeated or concurrent imports of the same known identity MUST leave exactly one persisted movie, return a deterministic skipped/already-present result through existing import semantics, and MUST NOT enqueue a second enrichment (`brief.md` Q2).
- **FR-006**: Database uniqueness constraints on library path/key and IMDb identifiers MUST remain the concurrency backstop; MUST NOT relax or migrate those indexes away (`brief.md` Q5; `recon-DEV-522` lines 53–55).
- **FR-007**: Existing failed-import/enrichment retry semantics MUST remain available independently of duplicate skipping (`brief.md` Approach 2; Q6).
- **FR-008**: When IMDb identity for a distinct path is discovered only during enrichment and collides with an existing movie, the system MUST settle that movie to Failed with category `duplicate_imdb_id` and the existing canonical caller-safe message; MUST NOT auto-delete/merge or present a silent unenriched gap or false success (`brief.md` Q2).
- **FR-009**: Enrichment settlement code paths MAY change only if Q4 reproduction proves existing late-failure settlement needs repair; otherwise preserve existing settlement (`brief.md` Q5).
- **FR-010**: Existing library summary and detail responses MUST add nullable string fields `failureCategory` and `failureMessage`; for Failed, project existing category code and canonical caller-safe message when available; for all non-Failed states both MUST be null (`brief.md` Q3).
- **FR-011**: System MUST NOT persist or return raw exception text in failure fields, API responses, or UI (`brief.md` Q3; constitution enrichment failure rule at `recon-DEV-522` lines 30–31).
- **FR-012**: Status enum values and existing library/import routes MUST be preserved; OpenAPI MUST be generated from C# DTOs and TypeScript from OpenAPI before web consumption, with no handwritten generated-type duplicates or manual generated-file edits (`brief.md` Approach 4–5; Q3).
- **FR-013**: MovieCard and BulkImportDialog failed-enrichment UI MUST show the canonical failure message alongside Failed when available; when details are null, retain the existing Failed label without inventing cause wording (`brief.md` Approach 6; Q7; `ASSUMPTIONS.md`).
- **FR-014**: Import endpoint/transport files MAY change only if needed to carry existing import results without inventing status codes/routes; any additional public import-contract shape change requires a separate Patron ruling (`brief.md` Q5).
- **FR-015**: If existing ports cannot support race-safe reuse, work MUST stop with needs recon/decision rather than adding a port silently (`brief.md` Q5; constitution closed-port rule at `recon-DEV-522` line 29).
- **FR-016**: Verification MUST include the brief-named unit, integration/Testcontainers, real API JSON (separate from UI), Vitest + React Testing Library component regressions for both views (T010/T012; never browser e2e / never `web/e2e` files), and identity/idempotency property tests, plus configured backend/web/mutation gates with exact exit-2 labels never folded into PASS; no gate threshold may be lowered (`brief.md` Q6 and Post-grill drafting clarification; Gate expectations; `constitution.md:707-734,760`).

### Key Entities

- **Movie (library row)**: Persisted library movie with library path/key identity, optional IMDb identity, and enrichment status/failure classification.
- **Import operation result**: Existing single/bulk import outcome semantics including imported, skipped/already-present, retried, and failure results (`recon-DEV-522` lines 49, 69–70).
- **Enrichment failure projection**: Caller-safe category code and message exposed only when status is Failed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A repeated import of the same movie by existing path/key (and by known IMDb identity when available to the operation) ends with one library movie and no duplicate enrichment work queued.
- **SC-002**: Concurrent imports of the same known identity leave one library movie (proven against a real database, not orchestration mocks alone).
- **SC-003**: A late IMDb collision on a distinct path shows Failed with a visible caller-safe `duplicate_imdb_id` reason rather than a silent unenriched gap or false success.
- **SC-004**: Library summary and detail responses expose nullable failure category/message correctly for Failed and null for non-Failed, without raw exception text.
- **SC-005**: On the library card and bulk-import failed section, users see the canonical Failed reason when available, and the existing Failed label when details are null.
- **SC-006**: Evidence of the actual historical duplicate trigger and separately demonstrated defect mechanism is present in the ticket via Rigger verified receipt. Unavailable historical evidence stops T001; mechanism-only evidence does not close this criterion.
- **SC-007**: Required gates report honest PASS / SKIPPED (scope-empty) / SKIP / N/A / Could not run / FAIL verdicts at the checked SHA; no non-pass folded into PASS.

## Assumptions

- Taste fallback when failure details are null retains the existing Failed label without invented cause wording (`ASSUMPTIONS.md`; Q7).
- “Existing import semantics” for skipped/already-present results means preserve current import result vocabulary rather than invent a new public import DTO in this ticket (`brief.md` Q2/Q5).
- Q4 investigation and Rigger receipt precede Build implementation work after Gate 1 / pickup (`brief.md` Task ordering).
- No `ui:` tag is recorded on the DEV-522 intake note used for this draft; Palette `design.md` is therefore not authored by Quill in this ask (pipeline applies Palette only on `ui:` tickets). If Keel requires a tag confirmation, that is a decision outside Quill drafting.


## Plan-adjudicated acceptance refinements

These refinements implement brief.md authoritative amendments and supersede draft interpretations above; see PLAN_ADJUDICATION.md for every disposition.

- US1/T001 independently establishes historical trigger evidence and mechanism reproduction, plus enqueue/save ordering, bulk commit visibility and concrete existing IMDb carriers including ManualImdbId participation. Missing historical evidence stops needs recon; no mechanism-only closure.
- US2/T002–T004: known-IMDb distinct-path reuse returns existing skipped/already-present semantics for the existing identity with no second row/enqueue/Failed settlement. T003 counts enqueue calls at most one under concurrent winner/loser constraint translation and covers two new same-known-IMDb paths within one bulk request, retaining processing of remaining items. Absent carrier needs explicit evidence-backed applicability disposition, never an invented input or silent scope reduction.
- Phase matrix: import-time known-IMDb conflict -> skip/reuse with no Failed; enrichment-time late conflict -> Failed/duplicate_imdb_id/canonical safe message, no merge/delete (T004 vs T007/T008).
- US2/US3 retries of duplicate_imdb_id must not add rows, loop enqueue indefinitely or regress to Pending; preserve existing non-retryable no-op or same Failed settlement without new UI.
- Path variants matching existing path/key semantics skip; variants still distinct under those rules follow late-discovery Failed behavior. No new normalization is authorized.
- SMOKE component tests are an explicit Patron-approved departure from browser e2e convention (CONCLUSIONS post-grill ruling 01c6d482; Constitution IX/stack), with existing e2e Failed-text compatibility checked in T014; no claim of executed browser smoke.
- Existing nullable LastFailureCategory flows through MovieSummary/MovieDetails/EfMovieCatalog (Patron ca667dc7) to Code/SafeDescription Failed-only response mapping. No new port/schema/layer.
