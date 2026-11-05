# Feature Specification: Explicit movie reviews and compact feedback icon row

**Feature Branch**: `feature/461-spec`

**Created**: 2026-11-05

**Status**: gate1: provisional

**Input**: Ticket DEV-461 (Size L, parent DEV-288; UI Tag empty at intake); frozen `specs/DEV-461/brief.md` (grill closed Q1–Q10 plus Phase 2 step 6 challenge addendum); Patron rulings in `CONCLUSIONS.md` (incl. commit `9e3fdc1663d53b4fedcdf88c201a5fdcd56d4c71`); taste receipt `ASSUMPTIONS.md`; ADR `docs/adr/0034-explicit-movie-reviews.md`; Maestri notes `DEV-461`, `recon-DEV-461`, `recon-DEV-461-2`, `recon-DEV-461-3`, `adjudication-DEV-461-Keel`; `specs/PRODUCT.md`. No decisions beyond the brief and its rulings. Quill invents nothing.

## Domain vocabulary *(mandatory)*

Settled in brief Q8–Q9 for delivery-PR glossary update of root `CONTEXT.md` (Constitution VIII). Spec artifacts use these meanings now; the glossary file itself is updated in the delivery PR, not claimed already changed:

- **Review**: An explicit opinion about a movie, with state `None`, `Liked`, or `Disliked`, independent of watching and watchlist intent. Distinguish from external metadata Rating and from engineering code review (ASSUMPTIONS glossary disambiguation).
- **Feedback**: Explicit Watched and Review actions about a movie.
- **Taste Profile**: An aggregate of viewing history and explicit review preferences.

_Avoid_: treating Not interested as a second independently persisted truth; inventing review timestamps; conflating Liked with Watched or Watchlist.

## Scope

**In scope** (brief Problem and frozen delivery; Q1–Q9; ADR 0034):

1. Canonical per-movie `Review` (`None` / `Liked` / `Disliked`), independent of Watched and Watchlist, with active-thumb clear and opposite-thumb direct switch.
2. Persist Review on the existing Movie aggregate/record as the approved Ardalis.SmartEnum (CLR `None`/`Liked`/`Disliked`, wire/Name `none`/`liked`/`disliked`, EF Value converter, exact `TryFromName`); same-migration backfill of `IsNotInterested == true` → `Disliked`, false → `None`, then drop the legacy mapping/column (lossy Down: Disliked→true, None|Liked→false); legacy not-interested routes and `isNotInterested` DTO flags as projections of `Review == Disliked` only (legacy mark sets Disliked; legacy unmark clears only Disliked, preserving Liked; repeat/invalid keep 409).
3. Idempotent `PUT /api/movies/{id}/review` with raw-string body field `{ "review": "none" | "liked" | "disliked" }` → 204 / 400 / 404 / 422 per the step 6 boundary matrix; responses expose `Review.Name` strings (never numbers); required `review` on summary, details, and recommended-movie DTOs; preserve existing fields and watched/watchlist/not-interested routes.
4. Contract chain C# DTOs → OpenAPI → generated TypeScript/MSW via existing tooling; matching Verify baseline; never hand-edit generated artifacts or invent duplicate web contracts.
5. Shared four-icon row (Watched, Thumbs up, Thumbs down, Watchlist) on every existing `MovieCard` caller and Movie Details via existing `MovieFeedbackActions`; move bookmark into that row without duplication; leave list-row placement unchanged; local SVG thumbs; exact Q9 accessible names/tooltips; optimistic/rollback/pending/retry/shared-state preserved.
6. Recommendation effects in both For You and Related: separate liked/disliked Taste Profile aggregates (plot, genres, cast, director), affinities `L`/`D`, signed profile contribution inside existing blend (preserving selected-movie similarity, blend composition, MMR, and deterministic ordering except the changed profile contribution), eligibility, incumbent post-commit cache invalidation/freshness, configuration/validation — per Q4–Q5 and step 6 addendum.
7. Truthful personal reason chip `Because you liked {X}` per Q6; keep Reason `kind`/`text` shape.
8. Glossary/ADR delivery obligations and the agreed regression/contract/acceptance evidence matrix (brief Test strategy; Q7).

**Out of scope** (brief frozen scope; §2.3):

- New NuGet/npm dependency (including any second enum package), project, top-level folder, new Feedback feature folder, or architectural layer / state library / Feedback-to-Recommendations Core port.
- Recommendation-engine replacement; list-row redesign; LocalPlay / secrets / `Process.Start`.
- Repurposing the unused not-interested blend penalty as an additional dislike penalty.
- Invented review timestamps or review recency; clamping signed profile contribution back to `[0,1]`.
- Retry queue, log-and-swallow, new invalidation port, or distributed/cross-process freshness guarantee for Taste Profile cache.
- New public concurrency controls (ETag / 409 / 412) for review assignment — last completed assignment prevails.
- Unrelated cleanup, gate/threshold edits, or handwritten OpenAPI/TS/MSW edits.
- Anything outside Q1–Q9 plus the step 6 addendum — follow-up issue, not a finding in this round.

**Precedence**: Ticket Scope/Recommendations/Acceptance (recon-DEV-461) plus Patron Q1–Q10 and Phase 2 step 6 addendum in `brief.md` / `CONCLUSIONS.md` are authoritative. Phase A produces documents only. Palette owns `specs/DEV-461/design.md` visual polish (Quill does not author it; Quill links disposition only). ADR 0034 is Keel-authored and remains **Proposed** until Gate 1 acceptance, then an explicit owner/Keel lifecycle update to Accepted.

**Owner checkbox**: none currently required (brief Q10; step 6). A later ticket contradiction or constitution departure keeps Gate 1 closed until the owner decides.

**Gate 1 / Phase B**: Explicit user Gate 1 approval and user-owned spec PR merge land first; then Phase B `T000` pickup drift runs against landed main/contracts. PLAN FROZEN is not Gate 1 approval. No seat merges.

## What the User Will See *(mandatory)*

- **Where**: Library grid / LibraryTile / For You / Related movie cards (`MovieCard`); Movie Details feedback row; recommendation reason chips on recommendation surfaces.
- **What the user sees or does**:
  - Bulky Watched / Not interested text buttons are replaced by a compact row of four icons: Watched, Thumbs up, Thumbs down, Watchlist (bookmark relocated into the row; no duplicate bookmark).
  - Thumbs are mutually exclusive; activating the pressed thumb clears the review; activating the other switches; Watched and Watchlist stay independent.
  - Review state persists across reload and matches between cards and Movie Details.
  - After liking or disliking, the next For You / Related results reflect the change (similar titles rise or fall / exclusion), with truthful reason chips when evidence exists.
- **What it needs to show up**: Default Compose stack; library with enriched movies and embeddings available for recommendation proof; no new secrets or LocalPlay.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a library movie card on the home surface, **When** the operator views the card actions, **Then** four icons appear (Watched, Thumbs up, Thumbs down, Watchlist) and the bulky text Watched / Not interested buttons do not.
- **SMOKE-002**: **Given** a movie with no review, **When** the operator likes it on a card and opens Movie Details (or reloads), **Then** the liked thumb is active on Details and the review survives reload.
- **SMOKE-003**: **Given** a liked movie with similar candidates available, **When** the operator requests For You after the like settles, **Then** similar candidates rank higher than before the like (relative ordering under controlled conditions), without fabricated positive reasons when evidence is absent.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Canonical Review persistence, migration, and API (Priority: P1)

A movie carries one Review SmartEnum (`None` / `Liked` / `Disliked`; wire Names `none` / `liked` / `disliked`) independent of Watched and Watchlist. Existing not-interested marks backfill to Disliked then the legacy column/mapping is dropped in the same migration. Clients set review via raw-string `PUT /api/movies/{id}/review`; legacy not-interested routes remain as projections. Summary/details/recommended DTOs expose required `review` Name strings and keep `isNotInterested` as the Disliked projection. Contract artifacts regenerate through tooling.

**Why this priority**: Ticket requires persistence, Details reflection, and OpenAPI baseline update; Q1–Q2 freeze the domain and public shape (brief; recon-DEV-461 Scope/Acceptance).

**Independent Test**: Domain/handler tests for all transitions; Testcontainers migration/reload/projection; HTTP status matrix for canonical and legacy routes; OpenAPI equality + generated types + Verify baseline (brief Q7).

**Acceptance Scenarios**:

1. **Given** a movie with `Review = None`, **When** the client PUTs `liked`, **Then** response is 204 and reload shows `review: liked` with `isNotInterested: false`.
2. **Given** `Review = Liked`, **When** the client PUTs `disliked`, **Then** state becomes Disliked directly (no intermediate None required) and `isNotInterested` projects true.
3. **Given** `Review = Disliked`, **When** the client PUTs `disliked` again, **Then** response is 204 (idempotent) without 409.
4. **Given** `Review = Liked`, **When** legacy DELETE not-interested runs, **Then** Liked is preserved (unmark clears only Disliked).
5. **Given** historical `IsNotInterested = true`, **When** the same-migration backfill then drop runs, **Then** Review is Disliked and the legacy column/mapping is gone; false migrates to None; Down maps Disliked→true and None|Liked→false (lossy for Liked).
6. **Given** malformed/unbindable JSON/body or route syntax, **When** PUT review is called, **Then** status is 400; **Given** missing/null/whitespace/unknown/wrong-case `review` or `id <= 0`, **Then** 422 (missing/null reach semantic validation, not binding 400); **Given** unknown movie, **Then** 404; **Given** valid repeated assignment, **Then** 204.
7. **Given** the contract pipeline, **When** artifacts are regenerated, **Then** OpenAPI, TypeScript, MSW, and Verify baseline match without hand edits; response `review` values are Name strings.

---

### User Story 2 - Reviews change For You and Related (Priority: P1)

Liking raises similar candidates; disliking lowers them and excludes the movie where eligibility requires. Separate liked/disliked profile aggregates feed bounded affinities `L` and `D`; profile contribution is `baselineSimilarity + LikedReviewWeight*L - DislikedReviewWeight*D` inside existing `BlendProfileWeight`, unclamped, while preserving selected-movie similarity, blend composition, MMR, and deterministic ordering except for that changed profile contribution. Disliked movies are omitted from positive baseline inputs. Canonical and legacy review writes use the incumbent `EfMovieRepository` post-commit generation-bump-then-tag-remove seam: completed invalidation is required before 204; a post-commit invalidation failure returns 500 with the DB write retained. Client mutations provisionally roll back optimistic state and reconcile via authoritative refresh plus idempotent retry; successful mutations invalidate/refetch library, details, For You, and Related.

**Why this priority**: Ticket acceptance requires a test that liking moves similar movies up and disliking moves them down or out; UI-only or storage-only reviews fail the ticket (brief Problem; Q4–Q5).

**Independent Test**: Controlled candidate-pool handler tests for both For You and Related before/after Like, Dislike, Clear, and opposite switches (MMR controlled); per-channel plot/genres/cast/director tests; warm-cache mutate-next-request integration tests; FakeEmbeddingGenerator only (brief Q7).

**Acceptance Scenarios**:

1. **Given** a controlled candidate pool designed for demonstrable ranking/exclusion, **When** a source movie is Liked, **Then** similar candidates rank higher in both handlers relative to the pre-like baseline (MMR controlled); arbitrary reviews need not universally perturb ordering.
2. **Given** the same pool, **When** that source is Disliked, **Then** similar candidates rank lower and the disliked movie is excluded from For You (and Related excludes Disliked).
3. **Given** Liked alone, **When** eligibility is evaluated, **Then** Liked does not exclude the movie; None removes only the review effect.
4. **Given** a warm cached Taste Profile, **When** review changes via canonical or legacy route and invalidation completes, **Then** the next recommendation request reflects the new state (204 path); clear/switch removes the previous signal from the controlled pool; failed tag-remove then read plus in-flight generation rejection remain covered; post-commit invalidation failure yields 500 with committed write retained.
5. **Given** invalid review weight configuration, **When** the host starts, **Then** options validation fails closed per Q4 rules.

---

### User Story 3 - Truthful liked-review reason chips (Priority: P2)

At most one personal chip `Because you liked {X}` appears when a currently Liked source has positive evidence in an enabled review channel, liked and profile weights are enabled, and net profile contribution after dislikes is positive. Selection is deterministic (greatest positive review affinity, ties by movie id); titles use existing sanitation. Viewing-history/genre chips stay grounded in baseline evidence only. No fabricated positive reason for negative-only profiles or disabled channels. Reason DTO shape unchanged.

**Why this priority**: Ticket asks dynamic chips to still make sense and contemplates `Because you liked X` (brief Q6).

**Independent Test**: Unit/integration reason provenance tests — baseline vs review evidence, positive liked-source, ties, sanitation, disabled-channel and negative-only cases (brief Q7).

**Acceptance Scenarios**:

1. **Given** positive liked-source evidence and enabled weights with positive net contribution, **When** reasons are built, **Then** at most one `Because you liked {X}` chip appears with sanitized title.
2. **Given** equal affinity among liked sources, **When** selecting `{X}`, **Then** the lower movie id wins.
3. **Given** liked-only or disliked-only inputs without baseline evidence, **When** viewing-history/genre chips are considered, **Then** they are not inferred from review-only inputs.
4. **Given** negative-only profile or disabled review channels, **When** reasons are built, **Then** no fabricated positive liked chip is emitted.

---

### User Story 4 - Compact accessible four-icon feedback row (Priority: P1)

Every existing MovieCard surface and Movie Details shows one shared four-icon row (Watched, Thumbs up, Thumbs down, Watchlist). Bookmark moves into the row; list-row placement unchanged. Exact Q9 accessible names and tooltips; `aria-pressed`; hover and keyboard-focus tooltips; visible active/focus; mouse/keyboard isolation from card navigation. Optimistic updates, rollback, per-movie pending, error/retry, and shared cross-surface state preserved; refresh retry never replays a successful mutation.

**Why this priority**: Ticket acceptance requires four icons on home cards, Details reflection, and MSW component coverage (brief Q3, Q9; recon component map).

**Independent Test**: Vitest/RTL with typed MSW handlers — every icon, exact names, tooltips, keyboard/focus/active, navigation isolation, mutual exclusion, shared card/details state, pending guard, rollback/retry, one relocated bookmark (brief Q7).

**Acceptance Scenarios**:

1. **Given** Library grid, LibraryTile, For You, and Related cards, **When** rendered, **Then** each shows the four-icon row and no bulky Watched / Not interested text buttons.
2. **Given** Movie Details, **When** rendered, **Then** the same shared row appears and review state matches cards after mutation.
3. **Given** inactive thumbs, **When** the user activates Like then Dislike, **Then** only Disliked remains pressed; activating the active thumb clears to None.
4. **Given** Q9 copy, **When** each icon is inspected, **Then** accessible name and tooltip equal `{action}, {title}` per the ASSUMPTIONS superseding table.
5. **Given** a failed mutation (including ambiguous post-commit 500), **When** the UI settles, **Then** optimistic review rolls back provisionally without claiming DB rollback; authoritative refresh and idempotent retry reconcile; refresh-only retry does not replay a successful mutation.
6. **Given** the former poster/details bookmark position, **When** the row is shown, **Then** Watchlist appears only in the row (no duplicate).

### Edge Cases

- Watched and Watchlist never mutate Review; Review never mutates Watched or Watchlist (Q1, Q5).
- Legacy mark not-interested sets Disliked; legacy unmark clears only Disliked; Liked preserved; invalid/repeat legacy transitions keep 409 (Q2).
- New movies have no explicit review until reviewed (`None`) (Q1–Q2).
- Missing metadata/embeddings invent no review signal; no usable channel ⇒ affinity 0 (Q4–Q5).
- Clear/switch removes previous review signal from profile and reasons (Q5–Q6).
- Dead `notInterested` blend term is not repurposed as an extra dislike penalty (Q5).
- Live Ollama is not used in automated tests — FakeEmbeddingGenerator only (Q7).
- Palette visual polish may refine CSS before plan challenge without changing Q9 copy or interaction contracts (Q3, Q8).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST persist exactly one canonical Review per movie as the approved Ardalis.SmartEnum with CLR identifiers `None`/`Liked`/`Disliked` and literal lowercase Names `none`/`liked`/`disliked`, independent of Watched and Watchlist; persist by Value via EF converter; parse with exact `TryFromName`; no new package or project-owned C# enum (Q1–Q2; step 6).
- **FR-002**: Selecting the active thumb MUST clear to `None`; selecting the opposite thumb MUST switch directly (Q1).
- **FR-003**: Same migration MUST backfill historical `IsNotInterested` true→Disliked and false→None, then drop the legacy mapping/column so Review is the sole persisted opinion truth; Down MUST recreate the boolean as Disliked→true and None|Liked→false (explicitly lossy for Liked); public legacy routes/DTO fields remain projections only (Q2; step 6).
- **FR-004**: Legacy `/not-interested` routes and `isNotInterested` fields MUST project `Review == Disliked` only, with Q2 mark/unmark/409 semantics.
- **FR-005**: System MUST expose idempotent `PUT /api/movies/{id}/review` binding `review` as a raw string (so unknown names reach Infrastructure FluentValidation via existing ValidationDecorator, not an incoming enum converter 400). Status matrix: malformed/unbindable JSON/body or route syntax → 400; missing/null/whitespace/unknown/wrong-case review or `id <= 0` → 422; unknown movie → 404; valid including repeated assignment → 204. Do not reject missing review as binding 400 ahead of semantic 422. Central ProblemDetails/traceId mapping; no exception-text leakage (Q2; step 6).
- **FR-006**: Summary, details, and recommended-movie DTOs MUST include required `review` as `Review.Name` strings (never numbers) while preserving existing fields (Q2; step 6).
- **FR-007**: Contract regeneration MUST follow C# → OpenAPI → generated TypeScript/MSW with matching Verify baseline; no hand edits or handwritten duplicate web contracts (Q2, Q7).
- **FR-008**: Taste Profile MUST keep recency-weighted watched/playback baseline omitting Disliked from positive inputs, plus separate liked and disliked aggregates (plot, genres, cast, director) that include reviewed-but-unplayed movies, equal per-reviewed-movie weight, no invented review timestamps; profile source MUST load Review state and required metadata/embeddings while preserving baseline separation (Q4–Q5; brief:57).
- **FR-009**: System MUST compute bounded `[0,1]` affinities `L` and `D` and profile contribution `baselineSimilarity + LikedReviewWeight*L - DislikedReviewWeight*D` inside `BlendProfileWeight` without clamping the signed result to `[0,1]`, and MUST preserve selected-movie similarity, existing blend composition, MMR, and deterministic ordering except for that changed profile contribution (Q4; brief:69).
- **FR-010**: Defaults MUST be `ReviewPlotWeight = 1.0`, `LikedReviewWeight = 1.0`, `DislikedReviewWeight = 1.0` (ASSUMPTIONS); startup validation per Q4.
- **FR-011**: For You MUST exclude Watched or Disliked; Related MUST exclude Disliked; Liked alone excludes neither; watchlist behavior unchanged (Q5).
- **FR-012**: Canonical and legacy review writes MUST use the existing Infrastructure repository post-commit generation-bump-then-tag-remove invalidation seam (no Feedback-to-Recommendations Core reference or new port). Completed invalidation is required before 204. Post-commit invalidation failure MUST propagate 500 with the committed DB write retained. Database atomicity does not imply cache atomicity. Clients MUST invalidate/refetch library, details, For You, and Related after successful mutation; provisional UI rollback MUST reconcile via authoritative refresh and idempotent retry without asserting DB rollback (Q5; step 6).
- **FR-013**: Reason builder MUST emit at most one truthful `Because you liked {X}` chip under Q6 gates; keep Reason `kind`/`text` shape (Q6; ASSUMPTIONS).
- **FR-014**: UI MUST render one shared four-icon row on all existing MovieCard callers and Movie Details, relocate Watchlist into that row, leave MovieListRow unchanged, and use local SVG thumbs with no new icon dependency; only the thumbs pair is mutually exclusive — Review `None` leaves Watched/Watchlist pressed state independent (Q3, Q9; step 6).
- **FR-015**: Each icon MUST implement Q9 accessible names/tooltips (exact casing; no sentence-case transform), `aria-pressed`, hover+focus tooltips, visible active/focus, and navigation isolation; preserve optimistic/provisional-rollback/pending/retry/shared-state contracts (Q3, Q9).
- **FR-016**: Delivery MUST update root `CONTEXT.md` Feedback/Taste Profile definitions and add Review per brief glossary; ADR 0034 records the durable tradeoff and remains Proposed until Gate 1 acceptance then explicit Accepted lifecycle update (Q8; step 6).
- **FR-017**: Required proofs MUST match the brief Test strategy table (domain/legacy including lossy Down, ranking both handlers including channel-isolated plot/genre/cast/director proofs on controlled pools, real-source Testcontainers regression for unplayed Liked/Disliked profile inputs, properties, post-commit cache invalidation including failed-tag-remove/read and in-flight generation rejection, 500-with-committed-write, reasons, Vitest/MSW icons, contract sync, architecture/notification regressions) with FakeEmbeddingGenerator and unmodified harness gates (Q7–Q8; step 6).

### Key Entities

- **Review**: Per-movie opinion SmartEnum — CLR `None` | `Liked` | `Disliked`; Name `none` | `liked` | `disliked`.
- **Feedback**: Watched mark plus Review.
- **Taste Profile**: Baseline viewing-history aggregate plus separate liked and disliked review aggregates (plot embedding + genre/cast/director preferences).
- **Reason**: Existing `kind`/`text` explanation attached to a recommendation; may include one personal liked-review chip when evidence supports it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Home/library recommendation cards show exactly four feedback icons and zero bulky Watched / Not interested text buttons (ticket Acceptance).
- **SC-002**: After setting a review on a card, Movie Details and a full reload show the same review state (ticket Acceptance).
- **SC-003**: Controlled For You (and Related) candidate-pool comparisons prove liking raises similar candidates and disliking lowers or excludes them; clear/switch removes prior effects; no universal rank-change claim for arbitrary reviews (ticket Acceptance; Q7; Sentry M2).
- **SC-004**: Component tests cover every icon, thumbs mutual exclusion (Watched/Watchlist independent), and exact title-bearing accessible names (ticket Acceptance; Q9).
- **SC-005**: After completed invalidation (204 path), the next recommendation request from a controlled warm-cache pool reflects the mutation including clear/switch removal; failed-tag-remove/read and in-flight generation rejection remain proven; post-commit invalidation failure yields 500 with committed write retained — not claimed as successful freshness (Q5, Q7; step 6).
- **SC-006**: Contract tooling proves OpenAPI/TypeScript/MSW/Verify stay synchronized after the new route and `review` Name-string fields (Q2, Q7).
- **SC-007**: No fabricated positive liked reason appears for negative-only or disabled-channel profiles (Q6).

## Assumptions

Copied from `ASSUMPTIONS.md` (taste and defaults; Q9 supersedes earlier label alternatives; step 6 appends wrapping/glossary):

- Icon row order left-to-right: Watched, Thumbs up, Thumbs down, Watchlist.
- Exact accessible names and tooltips use `{action}, {title}` per Q9 superseding table (`Mark Watched` / `Unmark Watched`; `Like` / `Clear liked review`; `Dislike` / `Clear disliked review`; `Add to Watchlist` / `Remove from Watchlist`); exact casing is content, not a transform.
- Tooltips on both pointer hover and keyboard focus.
- Narrow-viewport `flex-wrap` allowed only as overflow safety; Q9 order and target sizes preserved.
- Default weights `ReviewPlotWeight = LikedReviewWeight = DislikedReviewWeight = 1.0` (configurable, validated; not empirically tuned).
- Personal chip copy `Because you liked {X}` with movie-id tie-break and existing title sanitation.
- Existing watchlist component path is `WatchlistToggleButton` in `web/src/library/watchlist.tsx`; card/watchlist styling in `web/src/library/library.css` (Q9 factual clarification — not new files).
- Palette owns `design.md` contrast evidence (≥4.5:1 shipped-theme ratios) and visual CSS; Quill does not claim measurements.
- Phase A drafting claims no gate execution results.

## Retained Low follow-ups (not blockers; not in frozen delivery)

- No numeric load bound invented for reviewed-but-unplayed profile inputs (Sentry L2).
- No new public concurrency/ETag behavior for review writes (Sentry L3) — last completed assignment prevails.
- Palette contrast measurements and design wording remain Palette-owned (Sentry L4; Compass F4); Quill links disposition only.
- Shared-function rewrite deferred to existing Phase B refactor gates if duplication appears (Ledger F8).
