# Feature Specification: Recommendation cards show seed and evidence

**Feature Branch**: `feature/542-spec`
**Created**: 2026-11-19
**Status**: gate1: provisional
**Input**: Ticket DEV-542 (feature; size M; parent DEV-490); grill-closed private `brief.md` (Q1–Q5); `specs/DEV-542/CONCLUSIONS.md`; Maestri notes `DEV-542`, `recon-DEV-542` (authoritative ticket text at lines 44–69; Wanted item 2 and Done when at 71–97; HEAD `1ac83297c1ba8ffc936987e8b198ce8dc612b01a`); `specs/PRODUCT.md`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure. Frozen scope, seed wording, shared optional `seed`, server-owned evidence text, Phase B dependency checkpoint, and the all-severity closing bar with a two-round cap are closed in `brief.md` Q1–Q5 and `CONCLUSIONS.md`. Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Quill does not invent PLAN FROZEN or a structural checkbox.

## Clarifications

### Session 2026-11-19

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Seed wording (Q1), shared DTO seed optionality (Q2), server-formatted evidence text with full `matchedNames` (Q3), Phase B DEV-543/DEV-518 checkpoint (Q4), and review closing bar/cap (Q5) are frozen in `brief.md` / `CONCLUSIONS.md`. Ticket Scope & Technical Design and Done when at `recon-DEV-542:44–69,79–97` are already approved authority (`PRODUCT.md` §3). No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

- **Where**: For You page recommendation cards (`ForYouPage` / shared `MovieCard`)
- **What the user sees or does**: Each card’s first line names the seed with the matching signal template — “Because you liked {title}”, “Because you watched {title}”, or “Because {title} is on your watchlist” — with the title linked to `/movies/{movieId}` and accessible name “Open {title}”. Under that line, at most two evidence lines appear strongest first as a semantic list (desktop shows up to two; small widths default to one). Empty evidence shows no evidence lines. The flat legacy reason chips are gone.
- **What it needs to show up**: Default Compose stack; For You results with a nearest seed from DEV-543 and ordered evidence from DEV-518 (empty evidence is valid). Home-library reviewer check uses Hercules seeded from Aladdin as in Done when.

- **Where**: Movie details Related rail cards (`RelatedMovies` / shared `MovieCard`)
- **What the user sees or does**: Each Related card shows up to two evidence lines strongest first as a semantic list, without a seed line (the details-page movie is the seed). Empty evidence shows no evidence lines. Legacy reason chips are gone.
- **What it needs to show up**: Default Compose stack; a movie details page whose Related response carries ordered evidence (empty list valid).

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001** (For You presentation — `web/e2e/for-you-review.e2e.ts` seed-title assertion plus home-library reviewer check): **Given** the home library with For You available, **When** the reviewer opens For You, **Then** the Hercules card shows “Because you liked Aladdin” first and a “Same director” evidence line beneath it; the e2e check reads the seed title rather than legacy reasons.
- **SMOKE-002** (Related presentation — rewritten Related component coverage): **Given** Related recommendations with evidence items, **When** the Related rail cards render, **Then** up to two evidence lines appear without a seed line, and empty evidence invents no explanations.

## Scope

**In scope** (`brief.md` Frozen scope / Plan decisions / Test and evidence plan / Task ordering; Q1–Q5; ticket Scope, Technical design, Done when at `recon-DEV-542:49–69,79–97`):

1. **Shared recommendation response shape**: Keep one `RecommendedMovieDto` on the existing For You and Related routes. Add optional `seed` (`movieId`, `title`, `signal` of `liked` | `watched` | `watchlist`) and ordered `evidence` items (`criterion`, server-built `text`, `matchedNames`). Remove `reasons` and `RecommendedMovieReasonDto`. For You always sends a populated `seed`; Related omits the `seed` key entirely.
2. **Server evidence text**: Backend builds display-ready `text`, including first-three-names-plus-`+N` when needed, while `matchedNames` keeps the full ordered list. Web renders `text` as delivered.
3. **Legacy reason retirement**: Replace the prior `RecommendationReasonBuilder` / reasons path with DEV-518 evidence; remove “Matches your viewing history” outside a test that asserts its removal; retire unused `ToReasonDto` / `Kind` mapping.
4. **Card rendering**: Update shared `MovieCard` and styles, `ForYouPage`, and `RelatedMovies`. For You announces seed first, then evidence as `ul`/`li` capped at two (desktop) / one (small widths). Related shows evidence only. Seed and recommendation links remain independently operable (including keyboard). Accessible name for the seed link is “Open {title}”. Executable verification covers seed-before-evidence reading order, independent keyboard/link operation, and the small-width evidence cap via a viewport-capable check that includes Related’s shared rules.
5. **Contract chain and tests**: Regenerate OpenAPI drift snapshot, `web/src/api/openapi.json`, `web/src/api/types.ts`, and `web/src/mocks/handlers.ts` together via `LAMUFLIX_REGENERATE_OPENAPI` then `npm run generate:api`. Update API/integration, component, Related, and For You e2e tests named in Done when. Capture before/after desktop For You and one Related rail screenshot pair for the delivery PR. Run applicable size:M gates including static analysis on changed C# with no explanatory comments.
6. **Phase B dependency checkpoint**: Before production edits, verify DEV-543 supplies a real seed for every emitted For You item and DEV-518 supplies strongest-first evidence for both endpoints (empty lists allowed). Stop for bounded recon and a Patron ruling if either contract is missing or insufficient. Preserve recommendation identity, score, ranking, and selection.

**Out of scope**:

- DEV-538 For You grouping by seed.
- DEV-517 load more / swap.
- Any change to scoring, ranking, or which movies are recommended.
- New NuGet/npm dependency, project, top-level folder, architectural layer, database schema, `Features:LocalPlay`, secrets, or `Process.Start` (`PRODUCT.md` §5; brief Frozen scope).
- Retaining `reasons` alongside `seed`/`evidence`.
- Claiming PLAN FROZEN, Gate 1 merge, Build PASS, or runtime acceptance from this draft.
- Inventing upstream DEV-543/DEV-518 delivery proof during Phase A.

**Precedence**: Ticket Scope & Technical Design and Done when (`recon-DEV-542:44–69,79–97`) are already approved authority (`PRODUCT.md` §3). Brief Q1–Q5 and `CONCLUSIONS.md` govern plan boundaries. Only Keel declares PLAN FROZEN after independent analyze/reread. Gate 1 is a separate process. Sequence DEV-542 ahead of DEV-541 because both regenerate the same OpenAPI files; DEV-542 also blocks DEV-538.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Seed**
The existing movie whose liked, watched, or watchlist signal caused a For You recommendation; carried as `movieId`, `title`, and `signal`. Related uses the details-page movie as seed and does not send a seed object.
_Avoid_: inventing a fourth signal; sending `seed: null` on Related instead of omitting the key

**Evidence item**
One ordered criterion match with server-built display `text` and the complete ordered `matchedNames` list; strongest first; empty list valid.
_Avoid_: client-side re-formatting of names into `text`; inventing evidence when the list is empty

**Seed line**
The For You first-line copy from Wanted item 2 for the three signals, with the title linked to `/movies/{movieId}` and accessible name “Open {title}”.
_Avoid_: collapsing all signals to “Because you liked …”

Consumed vocabulary (already in use; not redefined): For You, Related rail, `RecommendedMovieDto`, enrichment pending empty Related list, contract chain C# DTO → OpenAPI → TypeScript.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Recommendation response carries seed and evidence (Priority: P1) 🎯 MVP

API consumers of For You and Related receive ordered evidence; For You also receives a populated seed; legacy reasons are gone from the shared DTO.

**Why this priority**: Without the response shape, neither surface can show seed/evidence, and Done when requires regenerated types with `seed`/`evidence` and no `reasons`.

**Independent Test**: Integration and end-to-end API tests assert For You populated seed, Related omission of the seed key, ordered evidence with full matched names and display text, empty evidence, absence of `reasons`, preserved movie identities/scores/order under the same inputs, and the five-name formatter case at the backend boundary. `npm run check:api` passes after joint regeneration.

**Acceptance Scenarios**:

1. **Given** For You recommendations with DEV-543 seeds and DEV-518 evidence, **When** `GET /recommendations/for-you` returns, **Then** every item includes populated `seed` (`movieId`, `title`, one of `liked`/`watched`/`watchlist`) and ordered `evidence`, and no `reasons` key.
2. **Given** Related recommendations for a details movie, **When** `GET /movies/{id}/recommendations` returns, **Then** each item includes ordered `evidence`, the `seed` key is omitted, and no `reasons` key.
3. **Given** an evidence item with five matched names, **When** the response is built, **Then** `text` shows the first three names plus “+2” and `matchedNames` retains all five in order.
4. **Given** empty evidence, **When** either endpoint returns successfully, **Then** `evidence` is an empty array and no explanatory lines are invented.
5. **Given** the regenerated web API artifacts, **When** `npm run check:api` runs in `web/`, **Then** it passes and `RecommendedMovieDto` in generated types exposes `seed` and `evidence` with no `reasons` key.

---

### User Story 2 - For You cards show seed then evidence (Priority: P1)

A viewer on For You reads why each card was recommended: the seed line first, then up to two strongest evidence lines.

**Why this priority**: This is the ticket’s primary user-visible outcome and the home-library Hercules acceptance check.

**Independent Test**: Component tests cover zero/one/three evidence items (three show two on desktop), small-width one-line default via a viewport-capable check, all three seed signals, seed link accessible name and target, seed-before-evidence reading order, independent keyboard/link operation of seed vs recommendation links, and a five-name item showing three names plus “+2”. Full web/e2e reads the seed title after both card consumers compile against the generated contract. Home-library reviewer sees Hercules with “Because you liked Aladdin” and a “Same director” line.

**Acceptance Scenarios**:

1. **Given** a For You item with seed signal `liked` and title Aladdin, **When** the card renders, **Then** the first line is “Because you liked Aladdin”, the title links to `/movies/{movieId}`, and the accessible name is “Open Aladdin”.
2. **Given** seeds with `watched` or `watchlist`, **When** those cards render, **Then** the first lines are “Because you watched {title}” and “Because {title} is on your watchlist” respectively.
3. **Given** three evidence items at desktop width, **When** the card renders, **Then** exactly two evidence lines appear strongest first as a `ul`/`li` list under the seed line.
4. **Given** three evidence items at small width, **When** the card renders, **Then** the default visible evidence count is one.
5. **Given** zero evidence items, **When** the card renders, **Then** no evidence lines appear and no invented explanations are shown.

---

### User Story 3 - Related cards show evidence without a seed line (Priority: P2)

A viewer on a movie details Related rail sees up to two evidence lines and no seed line.

**Why this priority**: Completes the dual-surface presentation required by Scope; depends on the same shared card/evidence rendering as US2 but omits seed UI.

**Independent Test**: The four named Related tests at `RelatedMovies.test.tsx` lines 125, 148, 548, and 580 are rewritten for the new shape and pass; Related cards never render a seed line; Related shares the viewport-capable small-width evidence-cap check.

**Acceptance Scenarios**:

1. **Given** Related items with evidence, **When** the rail cards render, **Then** up to two evidence lines appear strongest first and no seed line is shown.
2. **Given** empty evidence, **When** a Related card renders, **Then** no evidence lines and no invented explanations appear.
3. **Given** the rewritten Related tests named in Done when, **When** the suite runs, **Then** they pass against seed-omitted responses and evidence rendering.

---

### Edge Cases

- Related response omits `seed` entirely rather than sending null.
- Empty evidence is valid on both surfaces; UI invents nothing.
- More than two evidence items: UI shows at most two (desktop) or one (small widths); API may still return the fuller ordered list.
- Five matched names: server formats three + “+2”; full list remains in `matchedNames`.
- Pending enrichment Related path continues to return an empty recommendation list under existing status behaviour.
- `rg "Matches your viewing history" src web/src` finds no match outside a test asserting removal.
- Missing/insufficient DEV-543 or DEV-518 contract at Phase B pickup stops work for bounded recon and Patron ruling; do not invent provenance or change ranking.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST return ordered `evidence` on both existing recommendation routes and MUST NOT return `reasons` or `RecommendedMovieReasonDto`.
- **FR-002**: Every For You recommendation MUST include a populated `seed` with `movieId`, `title`, and `signal` in {`liked`, `watched`, `watchlist`}.
- **FR-003**: Related recommendations MUST omit the `seed` JSON key; the details-page movie remains the implicit seed.
- **FR-004**: Evidence `text` MUST be built server-side; with more than three matched names, `text` MUST show the first three plus `+N`, while `matchedNames` retains the full ordered list.
- **FR-005**: Web MUST render evidence `text` as delivered and MUST NOT re-format matched names into display text for valid responses.
- **FR-006**: For You cards MUST show the Wanted item 2 seed templates first, with title linked to `/movies/{movieId}` and accessible name “Open {title}”, then a semantic evidence list capped at two (desktop) / one (small widths). Executable checks MUST verify seed-before-evidence reading order and that the seed link and recommendation link remain independently operable by keyboard and pointer.
- **FR-007**: Related cards MUST show the same evidence list rules without a seed line. The small-width one-line evidence default MUST be verified with a viewport-capable check that covers Related’s shared evidence rules as well as For You.
- **FR-008**: Zero evidence MUST yield no evidence lines and MUST NOT invent explanations.
- **FR-009**: Legacy “Matches your viewing history” copy MUST be removed from `src` and `web/src` except a test asserting its removal; the prior reasons-builder path MUST be replaced by DEV-518 evidence.
- **FR-010**: Contract artifacts MUST follow C# DTO → OpenAPI → generated TypeScript/MSW regeneration together; web MUST consume generated types only.
- **FR-011**: Integration, component, Related, and For You e2e tests named in Done when MUST be updated for seed/evidence; before/after screenshots and the home-library Hercules check are required delivery evidence.
- **FR-012**: Phase B MUST verify DEV-543 and DEV-518 delivery inputs before production edits and MUST preserve recommendation identity, score, ranking, and selection.
- **FR-013**: Existing recommendation routes and documented status behaviour MUST be preserved; no new dependency, layer, schema, LocalPlay, secrets, or `Process.Start` change.

### Key Entities

- **Recommended movie response**: Shared card payload with movie summary fields, score, optional seed, and ordered evidence; no reasons.
- **Seed**: Causing movie identity, title, and signal for For You.
- **Evidence item**: Criterion key, server display text, and full ordered matched names.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A home-library reviewer opening For You can read the Hercules card’s seed line “Because you liked Aladdin” first and a “Same director” evidence line beneath it without consulting legacy reason chips.
- **SC-002**: Regenerated generated API types expose `seed` and `evidence` on `RecommendedMovieDto` and contain no `reasons` key; `npm run check:api` passes.
- **SC-003**: Component coverage demonstrates zero, one, and three evidence items (three display two on desktop; small width defaults to one via a viewport-capable check), all three seed signals, five-name “+2” display, seed-link accessible name “Open {title}”, seed-before-evidence reading order, and independent keyboard/link operation of seed vs recommendation links.
- **SC-004**: Related rail coverage for the four named tests passes on evidence-only cards with no seed line and shares the small-width evidence-cap verification.
- **SC-005**: Integration tests assert For You seed presence, Related seed-key omission, ordered evidence, and absence of `reasons`; regenerated OpenAPI/generated types mark `seed` optional and `evidence` required with no `reasons`; For You e2e reads the seed title after both web consumers compile against that contract.
- **SC-006**: `rg "Matches your viewing history" src web/src` matches only a removal-asserting test.
- **SC-007**: Before/after desktop For You screenshots and one Related rail card-set screenshot pair are attached to the delivery PR; applicable size:M gates report honest pass/fail labels for changed work.

## Assumptions

- DEV-543 and DEV-518 will supply the seed and evidence contracts described by the ticket by Phase B pickup; Phase A plans against those contracts, and Phase B verifies them before production edits (`brief.md` Q4).
- Existing recommendation routes, status codes, pending-enrichment empty Related behaviour, and card navigation to `/movies/{id}` remain the correct surfaces (`recon-DEV-542`; Wanted item 2).
- Ruled responsive outcome stands: desktop shows up to two evidence lines; small widths default to one. Exact reusable style/breakpoint anchor is not cited in bounded `recon-DEV-542`; Phase B pickup recon locates the shared cut (or records the concrete style target) before presentation edits, without inventing a new breakpoint number in this draft.
- No taste `[assumed]` copy beyond Wanted item 2 and ticket examples; Patron Q1–Q5 closed the inventable presentation choices with ticket citations.
