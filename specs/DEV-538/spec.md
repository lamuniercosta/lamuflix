# Feature Specification: For You page grouped by reason

**Feature Branch**: `feature/538-spec`

**Created**: 2026-11-21

**Status**: gate1: provisional — Gate 1 closed pending owner structural checkbox (below) and user merge of the Spec Kit PR

**Input**: Ticket DEV-538 (parent DEV-490; size L); grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-538`, `recon-DEV-538`; `specs/PRODUCT.md`. Constitution cited only in the plan. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

**blocked: structural** — The ticket requires the series row and two empty states (`ticket-text.txt:28,36-39,49`) while limiting API changes to DEV-541 and DEV-542 (`:54`). Prerequisite contracts provide neither candidate collection identity nor watched membership in that collection, and an empty page carries no seed-presence signal (`recon-DEV-538:49-54`). This consolidated Q2 wording supersedes the Q1-only checkbox. Gate 1 stays closed pending the owner's decision on the amendment and the Spec Kit PR merge.

- [ ] Approve amending Done when line 54 to permit two additive fields on GET /api/recommendations/for-you while keeping a single request: (1) nullable startedCollectionId (int?) on each RecommendedMovieDto item (non-null when the candidate belongs to a collection with a watched movie) for Wanted rule 1a, and (2) required boolean hasSeeds on the page envelope (true when any liked, watched, or watchlist seed exists before candidate/strong-match filtering, caps, paging, or exclusions) for Wanted rule 6a vs. 6b.

## Clarifications

### Session 2026-11-21

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Opening defaults, series signal (`startedCollectionId`), seed presence (`hasSeeds`), deterministic row assignment, paging/Swap/empty triggers, reachable More for you controls, empty-state copy, contract-first delivery sequence, and frozen review terms are settled in `brief.md` Settled decisions Q1–Q8 and `CONCLUSIONS.md`. Ticket Scope & Technical Design / Done when remain approved authority (`PRODUCT.md` §3) except the owner-pending Done when line 54 amendment carried above. Presentation taste items are logged in `ASSUMPTIONS.md`. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

- **Where**: For You page (`/for-you`)
- **What the user sees or does**: Instead of one flat grid, the page shows short labelled rows in fixed order: “Next in a series you started” (when it has at least two movies), up to four “Because you liked {title}” / “Because you watched {title}” rows, “Close to your watchlist” (when eligible), and “More for you” for the rest and for folded overflow. Each populated row is a named region with an `h2` and a horizontal carousel (arrow keys and Previous/Next). Cards inside rows keep evidence lines but do not repeat the seed “Because you …” line. Load more and Swap appear only on More for you. When the first successful load returns no movies, one of two empty messages appears with a “Browse the library” link.
- **What it needs to show up**: Default Compose stack; landed DEV-541/542/543/517 contracts; a library with liked/watched/watchlist seeds and strong-match candidates. Series row needs at least one collection with a watched member and an eligible unwatched candidate in the ranked page.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a library whose For You response includes multiple strong-match items with distinct seeds, **When** the viewer opens `/for-you`, **Then** movies appear under reason headings (not one undifferentiated grid), each visible row has a real subheading, and cards in those rows do not repeat the seed line under the heading.
- **SMOKE-002**: **Given** a successful For You load with zero items and no seeds, **When** the page settles, **Then** the viewer sees “No recommendations yet. Mark movies as watched or liked to get some.” and a “Browse the library” link.
- **SMOKE-003**: **Given** a successful For You load with zero items while seeds exist, **When** the page settles, **Then** the viewer sees “No recommendations closely match the movies you have liked, watched, or added to your watchlist yet.” and a “Browse the library” link.

## Scope

**In scope** (`brief.md` Settled decisions / Plan and file scope / Frozen scope; Q1–Q8; ticket Wanted / Done when / Where it lands):

1. **Additive For You response fields (owner-conditional)**: On the existing single `GET /api/recommendations/for-you`, add nullable `startedCollectionId` (`int?`) on each `RecommendedMovieDto` item (non-null only when the candidate belongs to a collection with at least one watched movie) and required `hasSeeds` on the page envelope `{ items, hasMore, hasSeeds }` (true when any liked, watched, or watchlist seed exists before candidate/strong-match filtering, caps, paging, or exclusions). Populate `StartedCollectionId` only in the For You projection; omit null on the wire so Related JSON stays unchanged.
2. **Client-side grouping**: Group the initial ranked response on the client. Caps: maximum four liked/watched seed rows and six cards per themed row, defined together in one file. Deterministic assignment per brief Q3 (series first; one per started collection; earliest known release year then API rank; best-seed uniqueness; watchlist row; More for you last; singleton/overflow fold).
3. **Row presentation**: Reuse existing `MovieCarousel` per populated row; labelled region + real `h2`; hide seed line via a prop on shared `MovieCard` (default unchanged for Related and other callers); retain DEV-542 evidence. Keep loading and error/Retry. Route stays `/for-you`.
4. **More for you + DEV-517**: Themed rows frozen from the initial GET. Load more and Swap apply only to More for you with cross-row deduplication. Zero-card More for you with `hasMore` still shows labelled region + Load more; mount carousel once a card exists; omit section when neither cards nor controls.
5. **Empty states**: Rule 6a vs 6b from `hasSeeds` after a successful initial empty GET; copy and library link per Q6.
6. **Contract chain and verification**: C# DTO → OpenAPI → generated TypeScript before web work. Backend unit/integration coverage for the two fields; web component coverage for Done when behaviours; harness and web gates; desktop and phone before-and-after screenshots on the delivery PR. Size L three-axis review under the written closing bar.

**Out of scope**:

- Scoring, ranking, or candidate-pool semantics (DEV-543 and upstream).
- Home page redesign; new routes; per-themed-row API paging.
- New NuGet/npm dependency; new project/top-level folder/architectural layer; database schema change; `Features:LocalPlay`, secrets hardcoding, or `Process.Start`.
- Claiming PLAN FROZEN, Gate 1 merge, Build PASS, or runtime acceptance from this draft.
- Anything else is a follow-up issue, not a finding in this round.

**Precedence**: Ticket Scope & Technical Design and Done when are approved authority (`PRODUCT.md` §3) except the owner-pending Done when line 54 amendment. Brief Q1–Q8 and `CONCLUSIONS.md` govern plan boundaries. Quill invents no decisions. Only Keel declares PLAN FROZEN after independent reread.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Started collection ID**
Optional per-item identifier of a collection that already has a watched movie; non-null marks a series-eligible candidate for client grouping.
_Avoid_: exposing a generic collection ID for every collection member; requiring a watched flag separate from this identifier

**Seed presence (`hasSeeds`)**
Required page-envelope boolean that is true when any liked, watched, or watchlist seed exists before candidate selection, strong-match filtering, caps, paging, or swap exclusions.
_Avoid_: inferring seed presence from a non-empty `items` page; computing after exclusions that empty the page

**Themed row**
Series, liked/watched seed, or watchlist row derived once from the initial For You response and kept fixed through More for you actions.
_Avoid_: paging or reshuffling themed rows on Load more / Swap

**Fold into More for you**
Move a singleton themed row (`< 2` movies), an unselected seed group beyond the four-row cap, or cards beyond the six-card themed-row cap into More for you without placing the movie in a second themed row.
_Avoid_: dropping overflow movies; re-routing a folded series card through seed/watchlist rows

Consumed vocabulary (already in use; not redefined): For You, More for you, strong-match, seed, evidence, `RecommendedMovieDto`, Load more, Swap, contract chain C# DTO → OpenAPI → TypeScript.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - For You response carries series and seed-presence signals (Priority: P1) 🎯 MVP

Consumers of the single For You GET receive enough series and seed-presence facts to build Wanted rule 1a and distinguish empty states 6a vs 6b, without a second request and without changing Related JSON.

**Why this priority**: Without these fields, series grouping and dual empty states cannot be implemented under the single-request constraint.

**Independent Test**: Backend unit and HTTP/integration coverage prove watched vs unwatched collection membership, no collection, populated collection ID, omitted null `startedCollectionId` on the wire, true/false `hasSeeds` with an empty page (mapped from Core), seed presence unaffected by paging/exclusions, required `hasSeeds`, and Related JSON free of `startedCollectionId` when null. OpenAPI and generated-type checks assert For You schema/type shape only (required boolean `hasSeeds`, optional nullable item `startedCollectionId`) — not Related omission.

**Acceptance Scenarios**:

1. **Given** a candidate in a collection that has at least one watched movie, **When** For You succeeds, **Then** that item’s `startedCollectionId` is the collection’s identity on the wire; **Given** no watched member or no collection, **When** For You succeeds, **Then** that item **omits** `startedCollectionId` on the wire (`JsonIgnoreCondition.WhenWritingNull` — null is never serialized).
2. **Given** at least one liked, watched, or watchlist seed before filtering, **When** For You returns zero items, **Then** `hasSeeds` is true; **Given** no such seeds, **When** For You returns zero items, **Then** `hasSeeds` is false. Endpoint mapping MUST copy `HasSeeds` from the Core For You result onto `ForYouRecommendationsPageDto`.
3. **Given** exclusions or paging that yield an empty page while seeds still exist upstream of filtering, **When** For You succeeds, **Then** `hasSeeds` remains true.
4. **Given** a Related recommendations request, **When** the response is serialized, **Then** `startedCollectionId` does not appear and the prior Related JSON shape is preserved aside from fields already owned by DEV-542.

---

### User Story 2 - Client groups ranked results into ordered reason rows (Priority: P1)

The For You page turns one ranked page into short reason rows with deterministic assignment, uniqueness, caps, and folding.

**Why this priority**: This is the core user-visible fix for the flat-list problem in the ticket Goal.

**Independent Test**: Component tests with fixed fixtures show series-priority row order, empty-row omission, release-year/tie cases, best-seed uniqueness, seed-group count/rank ties, liked vs watched signal split, retained API order in watchlist and More including folded overflow, singleton folding, overflow into More for you, and four/six caps defined in one file; one initial For You GET.

**Acceptance Scenarios**:

1. **Given** a fixed ranked page with series, liked/watched, and watchlist candidates, **When** the page groups results, **Then** rows appear in Wanted order 1a → 1b → 1c → 1d with missing empty themed rows omitted, and series assignment has priority over seed/watchlist placement for any candidate selected into the series row (`brief.md` Q3).
2. **Given** multiple candidates sharing a started collection ID, **When** the series row is built, **Then** at most one candidate per collection is selected by earliest known release year (unknown years last) then API rank, and the series row uses that same order.
3. **Given** a movie with more than one seed signal that was not selected for the series row, **When** rows are assigned, **Then** it appears once in the row of its single best seed (watchlist → Close to your watchlist; liked/watched → titled seed group keyed by seed movie ID plus reported signal).
4. **Given** a themed row with fewer than two movies, or seed groups beyond four, or cards beyond six in a themed row, **When** grouping completes, **Then** those movies appear in More for you (uncapped by the themed six-card limit) and not in a second themed row, retaining their relative API order among More for you cards including folded overflow.
5. **Given** liked/watched seed groups of size ≥ 2 after series selection, **When** selecting seed rows, **Then** groups are formed by seed movie ID plus reported signal (liked vs watched labels), at most four groups are chosen by descending candidate count then first API appearance (count/rank ties), labelled “Because you liked {title}” or “Because you watched {title}”, and API order is preserved within each seed row, the watchlist row, and More for you.

---

### User Story 3 - Row chrome: headings, carousel, hidden seed line (Priority: P1)

Each populated row is accessible and scannable; cards do not repeat the row’s reason as a seed line.

**Why this priority**: Done when requires region/`h2` semantics, carousel keyboard behaviour, and hidden seed lines with retained evidence.

**Independent Test**: Component tests assert named regions, `h2` headings, carousel arrow-key/Previous/Next behaviour, hidden seed lines with retained evidence on For You row cards, and unchanged default seed-line behaviour for other `MovieCard` callers.

**Acceptance Scenarios**:

1. **Given** at least one populated themed or More for you row, **When** the page renders, **Then** each such row is a labelled region whose heading is an `h2` one level below the page heading.
2. **Given** a populated row, **When** the viewer uses arrow keys or Previous/Next, **Then** focus moves through that row’s carousel the same way as on the details Related rail.
3. **Given** For You row cards, **When** they render, **Then** no card shows a “Because you …” seed line while evidence lines from DEV-542 remain; **Given** Related (or other default callers), **When** they render, **Then** seed-line behaviour is unchanged.

---

### User Story 4 - Distinct empty states (Priority: P1)

An empty initial For You load explains either missing seeds or no strong matches.

**Why this priority**: Ticket Wanted rule 6 and Done when require two distinct empty states with library links.

**Independent Test**: Two component tests show the two messages and the “Browse the library” link; rule 6 applies only after a successful initial GET with zero items.

**Acceptance Scenarios**:

1. **Given** a successful initial For You response with zero items and `hasSeeds === false`, **When** the page settles, **Then** it shows “No recommendations yet. Mark movies as watched or liked to get some.” and “Browse the library”.
2. **Given** a successful initial For You response with zero items and `hasSeeds === true`, **When** the page settles, **Then** it shows “No recommendations closely match the movies you have liked, watched, or added to your watchlist yet.” and “Browse the library”.
3. **Given** a non-empty initial render, **When** a later More for you follow-up returns no items, **Then** rule 6 empty states are not shown and existing cards remain.

---

### User Story 5 - More for you Load more and Swap with frozen themed rows (Priority: P2)

Paging and Swap continue to work on More for you only without reshuffling themed rows.

**Why this priority**: Preserves DEV-517 behaviour under the new layout; depends on stable themed rows from US2.

**Independent Test**: Component tests cover Load more / Swap only on More for you, zero initial More cards with `hasMore` including the region `h2`, Swap offered only when existing DEV-517 prerequisites hold, cross-row deduplication, stable themed rows, and empty Load more and empty Swap follow-ups that preserve rendered cards.

**Acceptance Scenarios**:

1. **Given** themed rows computed from the initial GET, **When** Load more or Swap runs, **Then** themed rows stay fixed and only More for you changes.
2. **Given** More for you starts with zero cards and `hasMore` is true, **When** the page renders, **Then** the labelled More for you region with its `h2` and Load more appear; the carousel mounts once a card exists.
3. **Given** follow-up results that overlap movie IDs already shown in any row, **When** they are applied, **Then** duplicates are excluded; an empty Load more follow-up and an empty Swap follow-up each leave the current cards in place without triggering rule 6 empty states.
4. **Given** More for you has neither cards nor an available control, **When** the page renders, **Then** the More for you section is omitted.
5. **Given** the existing DEV-517 Swap prerequisites, **When** those prerequisites hold on More for you, **Then** Swap is offered; **Given** those prerequisites do not hold, **When** the page renders, **Then** Swap is omitted while Load more still follows `hasMore` / control availability.

---

### Edge Cases

- Series row empty → build then hide (omit from DOM); do not drop rule 1a from scope.
- Singleton series row → fold entirely into More for you.
- All initial items land in themed rows with `hasMore: true` → More for you shows Load more without cards until loaded.
- Caps constants must live in exactly one file (Done when).
- Loading and error/Retry behaviour retained; empty states are not shown while loading or on error.
- One initial For You GET for grouping; no second request solely to discover seeds or series eligibility.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Successful For You GET MUST return `{ items, hasMore, hasSeeds }` with required boolean `hasSeeds` true iff any liked, watched, or watchlist seed exists before candidate/strong-match filtering, caps, paging, or exclusions; endpoint mapping MUST copy `HasSeeds` from the Core For You result onto the page DTO (`brief.md` Q2, Q7).
- **FR-002**: Each For You item MUST carry optional nullable `startedCollectionId` non-null only when the candidate belongs to a collection with at least one watched movie; a null value MUST be omitted on the wire (`WhenWritingNull`); Related MUST omit the wire field when null (`brief.md` Q1, Q7).
- **FR-003**: Client MUST group the initial ranked `items` into Wanted rows 1a–1d per brief Q3: series assignment has priority; remaining liked/watched groups use seed movie ID plus reported signal; watchlist and More for you (including folded overflow) preserve API rank; uniqueness, four seed-row and six card themed-row caps defined in one file (`brief.md` Q3; opening defaults).
- **FR-004**: Themed rows MUST be computed once from the initial GET and MUST remain fixed across More for you Load more and Swap (`brief.md` Q4).
- **FR-005**: Load more and Swap MUST apply only to More for you, using established DEV-517 request semantics (including Swap eligibility prerequisites) and excluding/deduplicating against every rendered movie ID; zero-card More for you with `hasMore` MUST show the labelled region `h2` and Load more (`brief.md` Q4–Q5).
- **FR-006**: Rule 6 empty states MUST render only after a successful initial GET with zero items, selecting 6a vs 6b from `hasSeeds`, with the Q6 copy and “Browse the library” link (`brief.md` Q6).
- **FR-007**: Each populated row MUST be a labelled region with a real `h2` and MUST reuse `MovieCarousel` keyboard/Previous/Next behaviour (`brief.md` Plan; ticket Wanted 7).
- **FR-008**: For You row cards MUST hide the seed line via a `MovieCard` prop while retaining evidence; default seed-line behaviour for other callers MUST remain unchanged (`brief.md` Plan).
- **FR-009**: System MUST keep a single initial For You request for grouping; contract chain MUST regenerate OpenAPI and TypeScript from C# DTOs before web consumption of the new fields (`brief.md` Q7).
- **FR-010**: Verification MUST cover Done when component behaviours, backend field semantics, required harness/web gates, and desktop/phone before-and-after screenshots, under the Size L closing bar (`brief.md` Frozen scope; Q8).

### Key Entities

- **For You page envelope**: `{ items, hasMore, hasSeeds }` over the existing ranked page.
- **Started collection signal**: Per-item optional series eligibility identity for client grouping.
- **Themed row / More for you bucket**: Client-side presentation partitions of the ranked page.
- **Seed-line opt-out**: Shared card presentation switch used by For You rows only.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A reviewer can open For You and identify why each visible short row exists from its heading alone, without reading repeated seed lines on every card.
- **SC-002**: Component fixtures prove Wanted row order, uniqueness, folding, caps, hidden seed lines, region/`h2` semantics, carousel keyboard movement, both empty states with library links, and More for you-only controls.
- **SC-003**: Empty initial loads with and without seeds show the two distinct messages; a non-empty page never swaps to rule 6 because of an empty follow-up.
- **SC-004**: HTTP/integration proves Related recommendation JSON remains free of `startedCollectionId` when null; OpenAPI and generated web types prove For You schema/type shape (required `hasSeeds`, optional nullable `startedCollectionId`) for consumption.
- **SC-005**: Desktop and phone before-and-after screenshots are attached to the delivery PR; required harness and web gates report honest verdicts (no threshold lowering; no folding SKIPPED/Could not run into PASS).
- **SC-006**: Size L review closes only when Sentry, Ledger, and Compass report on the reviewed head; High/Medium and accepted in-scope Low findings are closed with evidence; remaining Lows have explicit disposition; at most two review rounds and two fix commits per round.

## Assumptions

- Presentation assumptions logged in `ASSUMPTIONS.md` (caps; series/seed tie-breaks and overflow; frozen themed rows; zero-card More for you behaviour; 6b copy and library link label).
- DEV-541 paging/exclusions, DEV-542 seed/evidence, DEV-543 strong-match selection, and DEV-517 Load more/Swap contracts are prerequisites confirmed at Phase B pickup before production edits.
- No new CONTEXT.md glossary terms are required; vocabulary above is spec-local.
- Owner approval of the structural checkbox is required before Gate 1 can pass; drafts proceed with the behaviours included.
