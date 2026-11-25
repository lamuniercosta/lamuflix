# Feature Specification: Movie details About section

**Feature Branch**: `feature/570-spec`

**Created**: 2026-11-25

**Status**: gate1: provisional

**Input**: Ticket DEV-570 (parent DEV-490; size M); grill-closed `brief.md` (Q1–Q5); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-570`, `recon-DEV-570`; `specs/PRODUCT.md`. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Authority and scope / Grill record / Closing bar; `PRODUCT.md` §5). Ordinary owner Gate 1 remains **pending** until the user merges the Spec Kit PR. Sequence: clean plan challenge → Keel freeze receipt (PLAN FROZEN) → Patron provisional / Spec PR → owner merge completes Gate 1. Build and Chromium acceptance remain later deliverables.

## Clarifications

### Session 2026-11-25

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Cast mapped/unmapped behaviour, missing-value wire shape (`writers`/`genres` present arrays; `collectionName` present nullable; outer `metadata` null when absent), About omit-when-no-eligible-rows, Director/Directors and Writer/Writers by count with fixed Cast/Genres/Collection labels, file envelope, verification order, and Size M closing bar are settled in `brief.md` Q1–Q5 and `CONCLUSIONS.md`. Taste item Writer/Writers is tagged `[assumed]` in `ASSUMPTIONS.md`. Conductor asked Quill to run specify/plan/tasks in one ask without inventing decisions. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

- **Where**: Movie details page (`MovieDetailsView`), below the hero and actions and above the trailer.
- **What the user sees or does**: An **About \<title\>** heading with a description list of eligible rows in order — Director(s), Cast, Writer(s), Genres, Collection — using grey labels and white comma-separated values. Empty rows are omitted. Mapped cast names select the actor and open the existing More with panel; unmapped names stay plain text. The hero cast section stays as it is. When the movie has no metadata, the existing “No metadata available yet” notice stays and About is not shown. When metadata exists but no About row has data, About is not shown.
- **What it needs to show up**: Default Compose stack with at least one library movie that has enriched metadata including the fields to display (and, for Collection, a collection name). Null-metadata and empty-row movies remain valid for empty-state checks.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a fully enriched library movie on the details page, **When** the viewer scrolls to About, **Then** the heading **About \<title\>** and every eligible row (Director(s), Cast, Writer(s), Genres, and Collection when present) show the stored values in that order.
- **SMOKE-002**: **Given** About Cast includes a mapped actor name, **When** the viewer activates that name, **Then** the existing More with panel opens for that actor; the hero cast section remains unchanged.
- **SMOKE-003**: **Given** a movie with no metadata, **When** the viewer opens details, **Then** About is absent and the existing no-metadata notice remains.

## Scope

**In scope** (`brief.md` Authority and scope / Implementation approach / Verification and task ordering; Q1–Q5; ticket Scope and Acceptance at `recon-DEV-570:49–68`):

1. **API projection**: Add stored `writers`, `genres`, and collection name to `MovieMetadataResponse` on existing `GET /api/movies/{id}`; when metadata exists, `writers` and `genres` are always-present string arrays (empty when missing) and `collectionName` is an always-present nullable string (null when absent); outer `metadata` stays null when the movie has no metadata; regenerate OpenAPI baseline, Verify snapshot, and generated TypeScript via sanctioned tooling; extend API integration and OpenAPI contract tests.
2. **About section**: Place **About \<title\>** below hero and actions and above trailer; `dl` labelled by the heading with `dt`/`dd` pairs; row order Director(s), Cast, Writer(s), Genres, Collection; omit empty rows; omit entire section when metadata is null or every row has no data; Director/Directors and Writer/Writers by displayed count; fixed labels Cast, Genres, Collection; existing grey/white tokens; comma separators; wrap at phone width.
3. **Cast interaction**: Mapped About cast names use the existing page selection path and More with panel; unmapped names stay plain text; preserve comma-separated stored order; keep hero cast behaviour.
4. **Verification**: Unit/component coverage for rows, singular/plural labels, and empty cases (including null-metadata); API integration coverage for new fields; windowed and phone-width Chromium screenshots of the same enriched movie via Test URLs plus identifiable SMOKE-001–002 / separate-movie SMOKE-003; applicable C# and web gates; Size M three-axis review under the closing bar (Keel adjudicates).

**Out of scope**:

- Maturity rating and mood tags.
- Palette `design.md` / any `ui:` tag change (ticket tags are `size:M` only; recon line 76).
- New NuGet/npm dependency; new project/top-level folder/architectural layer; database schema; public API beyond the three ticket fields on the existing route; secrets hardcoding; `Process.Start`; changes to `Features:LocalPlay` execution.
- Anything else is a follow-up issue, not a finding in this round.

**Precedence**: Ticket Scope / Acceptance (`recon-DEV-570:49–68`) are approved authority (`PRODUCT.md` §3). Brief Q1–Q5 and `CONCLUSIONS.md` govern plan boundaries. Quill invents no decisions. PLAN FROZEN authority is the Keel freeze receipt in `CONCLUSIONS.md` only.

**Dependencies**: Existing movie details page, stored `MovieMetadata` fields, existing More with panel, and the OpenAPI → `types.ts` contract chain. No new services.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**About section**
Heading **About \<movie title\>** plus a heading-labelled description list of eligible metadata rows on the movie details page, placed below hero and actions and above the trailer.
_Avoid_: replacing the hero cast section; showing About when metadata is null or every row is empty

**Eligible About row**
A row with at least one value to display (a directors, actors, writers, or genres list with at least one non-blank value, or a non-null collection name). Ignore empty and whitespace-only list members in About only. Preserve the original text and relative order of displayed members. Collection appears only when the movie has a collection name.
_Avoid_: rendering blank `dd` values; inventing placeholder copy for missing fields

**Count-based crew labels**
Director vs Directors and Writer vs Writers according to the number of names shown in that row; Cast, Genres, and Collection stay fixed.
_Avoid_: pluralising Cast/Genres/Collection; inventing other label variants

Consumed vocabulary (already in use; not redefined): movie details, More with panel, hero cast, no-metadata notice, Test URLs portal.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Project About fields on movie details API (Priority: P1) 🎯 MVP

A client of `GET /api/movies/{id}` receives writers, genres, and collection name with the stored values when metadata exists, empty arrays or null collection name when those values are missing, and null outer metadata when the movie has none.

**Why this priority**: The page cannot render the new rows until the contract projects the stored fields and generated types agree.

**Independent Test**: Endpoint JSON asserts populated values and order, empty writers/genres, explicit null `collectionName` where absent, and preserved null-metadata; `OpenApiContractTests` schema assertions require present `writers`/`genres` arrays and present nullable `collectionName` on `MovieMetadataResponse`; OpenAPI drift check and regenerated TypeScript agree on the field shape. Endpoint and schema assertions are authored before projection; regeneration uses the sanctioned path confirmed at T001; endpoint and OpenAPI tests pass before web.

**Acceptance Scenarios**:

1. **Given** a movie with stored writers, genres, and a collection name, **When** `GET /api/movies/{id}` returns metadata, **Then** the JSON includes those values (arrays present; `collectionName` set).
2. **Given** a movie with metadata but no writers (and no collection), **When** the endpoint returns metadata, **Then** `writers` is `[]`, `genres` reflects stored genres, and `collectionName` is `null`.
3. **Given** a movie with no metadata, **When** the endpoint returns details, **Then** outer `metadata` is `null` (unchanged).
4. **Given** the DTO change, **When** OpenAPI baseline, Verify snapshot, and `types.ts` are regenerated via the sanctioned path confirmed at T001, **Then** `OpenApiContractTests` schema assertions and generated types agree on present `writers`/`genres` arrays and present nullable `collectionName`.

---

### User Story 2 - Read About on the details page (Priority: P1)

A viewer opening a movie with eligible metadata sees About \<title\> with the ticket-ordered rows, correct singular/plural crew labels, omitted empty rows, and no About when nothing is eligible; null-metadata movies keep the existing notice.

**Why this priority**: This is the primary user-visible deliverable of the ticket.

**Independent Test**: Component/unit tests cover About semantics — full enrichment, missing-writers-only, optional collection, all-rows-empty, null-metadata, singular/plural Director/Directors and Writer/Writers, heading-labelled `dl`, stored order with comma separators, blank-member filtering and displayed counts. Chromium receipts (US4) verify existing grey/white styling and phone-width wrapping.

**Acceptance Scenarios**:

1. **Given** a fully enriched movie, **When** the details page renders, **Then** About \<title\> appears after actions, existing browser player and launch feedback, and before the trailer, with Director(s), Cast, Writer(s), Genres, and Collection (when present) in that order.
2. **Given** metadata missing writers only, **When** the page renders, **Then** the Writers row is hidden and other eligible rows remain.
3. **Given** metadata with no collection name, **When** the page renders, **Then** the Collection row is absent.
4. **Given** one director and one writer, **When** About renders those rows, **Then** labels are `Director` and `Writer`; with more than one, labels are `Directors` and `Writers`.
5. **Given** null metadata, **When** the page renders, **Then** About is absent and the existing no-metadata notice remains.
6. **Given** non-null metadata where every About row has no data, **When** the page renders, **Then** About is absent.

---

### User Story 3 - Select cast from About (Priority: P1)

A viewer can activate a mapped cast name in About to open the existing More with panel; unmapped names stay plain text; hero cast behaviour is unchanged.

**Why this priority**: Ticket Acceptance requires cast names to keep opening More with; Q1 freezes mapped vs unmapped behaviour.

**Independent Test**: Regression covers mapped About name → More with, unmapped name as plain text, and existing hero cast selection still opening More with.

**Acceptance Scenarios**:

1. **Given** About Cast includes a mapped actor name, **When** the viewer activates it, **Then** the existing More with panel opens for that actor.
2. **Given** About Cast includes an unmapped name, **When** the viewer views the row, **Then** that name is plain text (not a selection control).
3. **Given** the hero cast section, **When** the viewer uses it as today, **Then** existing selection behaviour is unchanged.

---

### User Story 4 - Chromium About receipt (Priority: P2)

Delivery captures windowed and phone-width Chromium screenshots of About on the **same** enriched real library movie via the Test URLs portal, plus identifiable SMOKE-001–003 observations (SMOKE-001–002 on a movie with required data and mapped cast; SMOKE-003 on a **separate** null-metadata movie). Null-metadata About absence remains covered by component tests (US2) as well.

**Why this priority**: Acceptance requires the visual receipt after API and page work together; it closes delivery evidence rather than defining interactive behaviour.

**Independent Test**: Test URLs portal records identifiable route/result for each smoke observation and for the screenshot pair; both screenshots show About on the same enriched movie; SMOKE-003 uses a separate null-metadata movie.

**Acceptance Scenarios**:

1. **Given** integrated API and UI on a real enriched library movie, **When** Chromium captures a windowed screenshot via Test URLs, **Then** About is visible with eligible rows.
2. **Given** the same enriched movie and route, **When** Chromium captures a phone-width screenshot, **Then** About remains usable with wrapping values.
3. **Given** a movie with required About data and mapped cast, **When** SMOKE-001–002 are observed via Test URLs, **Then** eligible About rows and mapped-cast → More with are confirmed with identifiable route/result.
4. **Given** a separate movie with null metadata, **When** SMOKE-003 is observed via Test URLs, **Then** About is absent and the existing no-metadata notice remains, with identifiable route/result.

---

### Edge Cases

- Metadata null — no About; keep existing no-metadata notice.
- Metadata present but every About row empty — no About.
- Missing writers only — hide Writers; keep other eligible rows.
- Missing collection name (`collectionName` null) — hide Collection.
- Single vs multiple directors/writers — Director/Directors and Writer/Writers by displayed count.
- Unmapped cast names — plain text; mapped names use existing selection path.
- A missing or malformed new key on non-null metadata follows the existing details error path. Valid empty arrays and explicit null `collectionName` remain accepted.
- When the people lookup is pending or failed, names without a match appear as plain text.
- Hero cast — unchanged; About does not replace it.
- LocalPlay controls on the page — untouched; About work does not change playback gating or execution.
- Generated contract files — regenerate via sanctioned tooling; never hand-edit `types.ts` or invent a second DTO shape.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `MovieMetadataResponse` on existing `GET /api/movies/{id}` MUST project stored `writers` and `genres` as always-present string arrays when metadata exists (empty when missing) and `collectionName` as an always-present nullable string (null when absent); outer `metadata` MUST remain null when the movie has no metadata (`brief.md` Q2; `recon-DEV-570:51,66–67`).
- **FR-002**: Delivery MUST regenerate `web/src/api/openapi.json`, `web/src/api/types.ts`, and the OpenAPI Verify snapshot through sanctioned tooling so contract tests and generated types agree; generated files MUST NOT be hand-edited (`brief.md` Implementation approach; `PRODUCT.md` §1 contract chain).
- **FR-003**: API integration tests in `LibraryEndpointsTests.cs` MUST assert populated field values/order, empty writers/genres, explicit null `collectionName` where absent, and preserved null-metadata behaviour; `OpenApiContractTests.cs` MUST assert the OpenAPI schema for present `writers`/`genres` string arrays and present nullable `collectionName` on `MovieMetadataResponse`, checking required membership and nullability separately. Author those endpoint and schema assertions before projection; regenerate baseline/snapshot/types via the sanctioned path confirmed at T001; pass endpoint and OpenAPI tests before web (`brief.md` Verification step 1).
- **FR-004**: The details page MUST show **About \<title\>** after the hero, actions, existing browser player and launch feedback, and before the trailer when at least one About row is eligible; MUST use a heading-labelled `dl` with `dt`/`dd` pairs; MUST list rows in order Director(s), Cast, Writer(s), Genres, Collection; MUST omit empty and whitespace-only list members from About display and eligibility; MUST omit empty rows; MUST omit the entire section when metadata is null or every row has no data; MUST keep the existing no-metadata notice for null metadata (`brief.md` Q3; `recon-DEV-570:52–60,64–65`).
- **FR-005**: Director/Directors and Writer/Writers labels MUST use Director/Directors and Writer/Writers by displayed count; Cast, Genres, and Collection MUST use those fixed labels (`brief.md` Q3; `ASSUMPTIONS.md`).
- **FR-006**: About MUST reuse existing grey label / white value tokens and comma separators for multiple values (`brief.md` Q3; `recon-DEV-570:59–60`). Phone-width wrapping MUST be verified by Chromium receipts (FR-010; US4), not by component-test layout expectations.
- **FR-007**: Mapped About cast names MUST open the existing More with panel through the existing page selection path; unmapped names MUST remain plain text; comma-separated stored order MUST be preserved; hero cast behaviour MUST remain unchanged (`brief.md` Q1; `recon-DEV-570:52–55,65`).
- **FR-008**: Implementation MUST stay inside the approved file envelope: `LibraryResponses.cs`, `LibraryEndpointsTests.cs`, `OpenApiContractTests.cs` and its generated snapshot, generated `openapi.json` / `types.ts`, `movieDetailsApi.ts`, `MovieDetailsView.tsx`, focused adjacent About, view and parser tests, one focused colocated `AboutSection`, and `MovieDetailsView.css` if needed for scoped About styling. Minimal `CastSection.tsx` sharing is conditional on real reuse. Patron authorizes additive required-field updates to existing test/mock fixtures only with T001 or compiler evidence and within the bounds in `brief.md` Q4 and the plan-challenge addendum. Generator-owned `handlers.ts` and `types.ts` change via regeneration. Other tracked file changes require a fresh Patron ruling. Pickup (T001) confirms that envelope; About implementation (T008) stays inside it.
- **FR-009**: Web unit/component tests MUST cover About semantics — row values and order, singular/plural labels, missing-writers-only, optional collection, all-rows-empty, null-metadata with existing notice, accessible naming of the description list, blank-member omissions and displayed counts, strict rejection of malformed or missing new keys through the existing details error path, mapped/unmapped About cast paths, and retained hero selection regression (`brief.md` Verification step 2; `recon-DEV-570:67`). Phone-width wrapping belongs to Chromium (FR-010), not these component tests.
- **FR-010**: Delivery MUST capture windowed and phone-width Chromium screenshots of About on the **same** enriched real library movie via the Test URLs portal, recording local test route and viewport dimensions. The same-movie rule applies only to that screenshot pair. Delivery MUST also record identifiable SMOKE-001–002 on a movie with the required data and mapped cast, and SMOKE-003 on a **separate** movie with null metadata, each with identifiable route/result (`brief.md` Verification step 3; `recon-DEV-570:68`). Existing grey/white styling and phone-width wrapping from FR-006 are verified on the screenshot pair. Null-metadata About absence remains covered by component tests (FR-009) as well.
- **FR-011**: After Build, Cog `/refactor` precedes Gauge Phase 3 step 5, then Gauge pre-pass and Sentry/Ledger/Compass review plus findings artifact, then Phase 5 rebase and gate rerun. Applicable gates MUST be run and recorded using the rules in `plan.md` Verification Approach, including the task-note condition for property-test exit 2. These gates and review are Conductor-owned downstream steps — not Anvil local phases — and do not assert hosted merge readiness.
- **FR-012**: Size M review MUST cover Risk (Sentry), Standards (Ledger), and Spec (Compass) axes. **Keel** adjudicates over all three axis reports and owns the findings artifact. Every verified in-scope finding through Low MUST close within two rounds and at most two fix commits per round, or be reported blocked; record dispositions in the findings artifact (`brief.md` Q5; Closing bar). Review is downstream of Build, not bundled into an Anvil phase.

### Key Entities

- **Movie metadata (details response)**: Existing details payload whose metadata object gains `writers`, `genres`, and `collectionName` when present.
- **About row**: Label/value pair for one eligible metadata facet on the details page.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A fully enriched movie shows every eligible About row with stored values in ticket order; missing writers hides only the Writers row.
- **SC-002**: Mapped About cast names open the existing More with panel; unmapped names stay plain text; hero cast behaviour is unchanged.
- **SC-003**: `GET /api/movies/{id}` returns `writers`, `genres`, and `collectionName` with the Q2 missing-value shape; `OpenApiContractTests` schema assertions and generated types agree on present arrays and present nullable `collectionName`.
- **SC-004**: Unit/component tests cover About semantics (rows, singular/plural labels, empty cases); API integration and OpenAPI schema assertions cover the new fields.
- **SC-005**: Windowed and phone-width Chromium screenshots of About on the same enriched real library movie are preserved; identifiable SMOKE-001–002 (enriched + mapped cast) and SMOKE-003 (separate null-metadata movie) observations with route/result are preserved; phone-width wrapping verified on the screenshot pair; null-metadata component coverage retained.
- **SC-006**: Size M review closes under the two-round / two-fix-commits-per-round cap with Keel adjudication over Sentry/Ledger/Compass and the findings artifact; every verified in-scope finding through Low is closed within the cap or reported blocked; applicable gates recorded without folding `SKIPPED` / `SKIP` / `N/A` / `Could not run` into PASS; hosted merge readiness is not asserted from Build.

## Assumptions

- Writer/Writers by displayed count (and fixed Cast, Genres, Collection labels) is the Patron `[assumed]` taste item in `specs/DEV-570/ASSUMPTIONS.md`, frozen by brief Q3.
- T001 verifies the pinned export invocation at pickup. T004 runs the export after projection, using `plan.md` Contract regeneration.
