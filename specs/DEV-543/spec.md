# Feature Specification: For you nearest-seed ranking

**Feature Branch**: `feature/543-spec`
**Created**: 2026-11-19
**Status**: gate1: provisional
**Input**: Ticket DEV-543 (parent DEV-490; size L); grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri note `DEV-543`; the authoritative DEV-543 YouTrack export supplied by the Conductor in disposable `specs/DEV-543/ticket-text.txt`; historical intake pointers `recon-DEV-543` / `recon-DEV-543-2` (see `DRAFTING_RECEIPT.md` for pickup evidence); `specs/PRODUCT.md`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Quill does not invent PLAN FROZEN or a structural checkbox. The earlier ADR numbering collision (ticket/early grill named 0045; DEV-545 took 0045) is resolved: Conductor claimed ADR 0047 and Keel drafted Proposed `docs/adr/0047-for-you-nearest-seeds.md` for the **delivery** PR (`brief.md` Decision record). Gate 1 does not land the ADR separately. No open drafting `needs decision` for Gate 1.

## Clarifications

### Session 2026-11-19

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical inventable ambiguity for drafting? → A: Frozen scope (nearest-seed For you ranking, strong-match eligibility, best-seed reasons, Related profile removal, one-library-load and bounded-cache work, legacy cleanup after caller checks, evidence, ADR/glossary), Related decision (Q2), recency (Q3), shared strong-match policy (Q4), seed precedence and empty results (Q5), best-seed selection and ID tie-break (Q6), closing bar (Q7), and two-round review cap (Q8) are frozen in `brief.md` / `CONCLUSIONS.md`. Presentation `[assumed]` tie-breaker in `ASSUMPTIONS.md` is Patron-ratified. Incomplete caller inventory is an implementation precondition (`brief.md` Files and implementation boundaries), not an inventable behaviour choice. ADR path resolved to Proposed `docs/adr/0047-for-you-nearest-seeds.md` (Conductor claim; Keel draft preserved for delivery with T017–T019; historical ticket/grill 0045 naming preserved as provenance only). Historical recon notes are superseded or recovery-limited; see `DRAFTING_RECEIPT.md`. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## What the User Will See *(mandatory)*

- **Where**: Home For you rail (and any surface that shows For you recommendation cards with reasons)
- **What the user sees or does**: Unwatched movies appear only when they have a strong match to at least one of the owner's seeds (liked, watched without review, or watchlist). Ranking prefers movies near those seeds. Each card's reason list starts with a seed line naming the best strong seed (`Because you liked {title}`, `Because you watched {title}`, or `Because {title} is on your watchlist`), followed by the DEV-518 evidence for that same pair. Superseded generic lines ("Matches your viewing history", "Matches your {genre} interest", "No shared features identified") no longer appear on For you.
- **What it needs to show up**: Default Compose stack; enriched library with owner likes/watches/watchlist/dislikes; DEV-518 similarity and DEV-544 library feature load and strong-match options available on the running build

- **Where**: Related rail for a selected movie
- **What the user sees or does**: Related results use DEV-518 similarity to the selected movie (with existing watched penalty and disliked exclusion). Profile-style "Matches your…" reasons and score influence from unrelated reviews are gone.
- **What it needs to show up**: Same Compose stack; at least one selected movie with Related candidates

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a library with liked/watched/watchlist seeds and unwatched candidates that have strong matches, **When** the user opens Home and inspects the For you rail, **Then** cards show seed-first reason lines naming a liked/watched/watchlist title and do not show the three superseded generic reason templates
- **SMOKE-002**: **Given** a selected movie with Related results, **When** the user opens Related, **Then** no result shows a "Matches your" profile reason

## Scope

**In scope** (`brief.md` Frozen scope; Q1–Q8; ticket Wanted 1–13 and Done when as frozen in brief/CONCLUSIONS from historical `recon-DEV-543-2` — note superseded/recovery-limited; see `DRAFTING_RECEIPT.md`):

1. Nearest-seed For you ranking using DEV-518 pair similarity over positive seeds (liked / watched-without-review / watchlist) with configured weights, top-three aggregation, disliked penalty, and playback recency scaling.
2. Strong-match eligibility via the single shared DEV-544 options set (default bar 0.225; Collection, Director or Themes at value 1).
3. Best strong-seed selection for card reasons; seed line first in the existing `reasons` list; OpenAPI/DTO shape unchanged.
4. Related rail: remove taste-profile score contribution and profile reasons; keep DEV-518 similarity, `AlreadyWatchedPenalty`, and disliked exclusion.
5. One DEV-544 library feature load per For you request; shared seed/candidate feature mapping (full cast, keywords, collection); no paged catalog walk; no per-movie `GetAsync`; no second profile read on cache miss; cache must not grow with library size × embedding size (follow DEV-510 shape).
6. Shared recency decay helper from existing TasteProfileCalculator decay; configurable floor default 0.25.
7. Options for seed weights, rank multipliers, disliked weight, and recency floor; reuse DEV-544 strong-match options; remove ticket-named dead profile members after caller checks (keep `AlreadyWatchedPenalty`).
8. Unit/integration coverage named in brief Acceptance; unchanged OpenAPI snapshot; DEV-545 before/after quality and warm runtime evidence; nearest-seed ADR + ADR 0034 supersession note + Taste Profile glossary rewrite in the same PR.
9. Size L review closing bar (all severities including Low; Sentry/Ledger/Compass dispositions; two fix/review rounds).

**Out of scope**:

- Card layout and response field changes beyond the named reason/seed carriage (DEV-542).
- Per-seed/per-collection caps, paging, exclusions (DEV-541).
- Changing DEV-518 similarity itself or the Related candidate pool (DEV-544 owns the pool; this ticket only removes the Related profile part).
- Crowd similarity (DEV-539) and plot experiment (DEV-540).
- New NuGet/npm dependency, new project/layer, database schema, public API/OpenAPI/TypeScript shape, `Features:LocalPlay`, secrets, or `Process.Start`.
- Re-opening ADR numbering: Proposed 0047 is claimed and drafted for delivery; historical 0045 naming is provenance only; Gate 1 does not land the ADR separately.
- Claiming PLAN FROZEN, Gate 1 merge, implementation PASS, or runtime acceptance from this draft.

**Precedence**: Ticket Scope & Technical Design and acceptance criteria (historical `recon-DEV-543-2`, now superseded/recovery-limited; frozen text in brief/CONCLUSIONS) are already approved authority (`PRODUCT.md` §3). Brief Q1–Q8 and `CONCLUSIONS.md` govern plan boundaries. Only Keel declares PLAN FROZEN after independent reread. Keel owns ADRs.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only)*

**Seed**
A library movie that carries an owner signal used for nearest-seed scoring: liked, watched without review, watchlist (positive), or disliked (negative only).
_Avoid_: treating disliked as a positive seed; treating a movie with multiple signals as multiple positive kinds

**Strong match**
A candidate–seed pair whose raw DEV-518 similarity is at or above the shared bar, or that has a shared strong reason (Collection, Director or Themes at value 1), evaluated before seed weight, recency, aggregation, and disliked penalty.
_Avoid_: requiring weighted/recency-adjusted score for admission

**Best seed**
Among strong positive pairs for a candidate, the seed with the highest recency-adjusted weighted similarity (effective seed weight × DEV-518 similarity); ascending movie ID breaks ties.
_Avoid_: naming a weak-evidence seed on the card

**Nearest-seed score**
Signed aggregate: sum of top three positive products (similarity × effective weight) with rank multipliers 1 / 0.5 / 0.25, minus disliked weight × max similarity to any disliked seed.
_Avoid_: equal-weight profile centroid / signed blend formula from ADR 0034

## User Scenarios & Testing *(mandatory)*

### User Story 1 - For you nearest-seed ranking and strong-match eligibility (Priority: P1)

The owner sees For you results ranked by closeness to their nearest seeds, and only candidates with at least one strong positive match appear (no padding).

**Why this priority**: Core product change; without ranking and eligibility the rail still uses the superseded centroid baseline.

**Independent Test**: Handler unit tests cover seed precedence, watchlist-only ranking, self-exclusion, disliked penalty, recency floor, top-three aggregation, strong-match boundary (including director-only), weak-aggregate rejection, and deterministic ID ties; page may return fewer than requested.

**Acceptance Scenarios**:

1. **Given** positive seeds with configured weights (liked 1.0, watched-without-review 0.6, watchlist 0.4) and unwatched non-disliked candidates, **When** For you runs, **Then** each candidate is scored with DEV-518 similarity against every positive seed except itself, using consistent full cast/keywords/collection mapping on both sides, and ranked by the signed nearest-seed aggregate.
2. **Given** a movie that is both watched and liked, **When** seeds are classified, **Then** it counts once as liked (liked > watched-without-review > watchlist); disliked is negative only.
3. **Given** watchlist as the only positive signal, **When** For you runs, **Then** ordinary nearest-seed ranking runs (no special empty fallback); **Given** no positive seeds or no strong positive pairs after self-exclusion, **Then** the list is empty with no padding.
4. **Given** a candidate whose only strong match is a shared director with one seed, **When** For you runs, **Then** the candidate qualifies and that seed is eligible to be named; **Given** a high aggregate of only weak pairs, **Then** the candidate is excluded.
5. **Given** otherwise equal candidates where one is closer to a disliked seed, **When** results are ranked, **Then** the closer-to-disliked candidate ranks lower; disliked term and raw strong-match check are unscaled by recency.
6. **Given** a positive seed with playback events, **When** scoring runs, **Then** its weight is multiplied by existing exponential decay with configurable floor 0.25; seeds without playback (including watchlist) stay unscaled.

---

### User Story 2 - Best-seed reasons on For you (Priority: P1)

Each For you card explains itself with the best strong seed and that pair's DEV-518 evidence, using the existing reasons list shape.

**Why this priority**: Ticket Done when forbids superseded generic reasons and requires named seed pairs in the top 10.

**Independent Test**: Handler tests assert reason order (seed line then DEV-518 evidence for the same pair), correct templates by signal kind, exclusion of the three superseded templates, and that the named seed is the best strong pair even if that pair is outside the top-three score contributions.

**Acceptance Scenarios**:

1. **Given** a qualifying candidate with one or more strong positive pairs, **When** the result is built, **Then** the card keeps the selected seed's movie ID, title and signal kind with its DEV-518 evidence; `reasons` starts with the matching seed template, then that pair's evidence lines.
2. **Given** equally scored strong seeds or equally ranked candidates after existing ordering keys, **When** selection runs, **Then** ascending movie ID breaks the tie (`ASSUMPTIONS.md`).
3. **Given** For you results, **When** reasons are inspected, **Then** none of "Matches your viewing history", "Matches your {genre} interest", or "No shared features identified" appear.

---

### User Story 3 - Related without taste-profile contribution (Priority: P1)

Related scores and reasons no longer use the taste profile; unrelated reviews do not change Related scores.

**Why this priority**: Removing For you profile machinery would leave Related on a dead path unless this story lands with it.

**Independent Test**: Related handler tests prove unchanged score after an unrelated review is added; no "Matches your" reasons; disliked exclusion and `AlreadyWatchedPenalty` still apply.

**Acceptance Scenarios**:

1. **Given** Related candidates for a selected movie, **When** scoring runs, **Then** the score is DEV-518 similarity to the selected movie (plus existing watched penalty / disliked exclusion) with no profile blend contribution.
2. **Given** an unrelated movie receives a new review, **When** Related is rescored for the same selection, **Then** result scores are unchanged.
3. **Given** Related results, **When** reasons are inspected, **Then** no result carries a "Matches your" profile reason.

---

### User Story 4 - One library load, shared options, recency helper, and cache bounds (Priority: P2)

For you loads library features once, shares options with DEV-544 strong-match, extracts shared decay, and keeps cache growth bounded.

**Why this priority**: Correct ranking depends on this data path; ticket speed and Wanted 8/10 require it.

**Independent Test**: Handler/persistence tests verify one DEV-544 library feature read, shared seed/candidate features and watchlist state, no per-movie `GetAsync`, no second profile read on cache miss, and that cached data does not grow with library size × embedding size.

**Acceptance Scenarios**:

1. **Given** a For you request, **When** the handler runs, **Then** seeds and candidates come from one DEV-544 library feature load; paged catalog walk and per-eligible `GetAsync` are gone.
2. **Given** a cache miss on the profile/source path still used for playback/recency inputs, **When** For you runs, **Then** there is no redundant second profile-inputs read.
3. **Given** recommendation options, **When** strong-match and seed scoring configure, **Then** both surfaces share the DEV-544 strong-match option set; seed weights, rank multipliers, disliked weight, and recency floor live in recommendation options; `AlreadyWatchedPenalty` remains.

---

### User Story 5 - Legacy cleanup, decision record, glossary, and acceptance evidence (Priority: P2)

After both rails stop using profile scoring, ticket-named dead members are removed only after caller checks; decision docs and DEV-545 evidence land in the same PR.

**Why this priority**: Prevents leaving dead profile code and closes Done when / Wanted 12–13.

**Independent Test**: Caller inventory recorded before each removal; OpenAPI snapshot unchanged; unit+integration suites green; DEV-545 snapshot on 2026-11-16 home library meets top-10 seed diversity and named pairs; before/after hit rate does not decline; warm For you under 500 ms with 450 movies and 150 liked.

**Acceptance Scenarios**:

1. **Given** For you and Related no longer call ticket-named legacy profile members (Wanted 11 list; keep `AlreadyWatchedPenalty`), **When** caller checks find no remaining `src` callers, **Then** those members are removed; any remaining caller stops removal for that member until disposition is recorded.
2. **Given** the same **delivery** PR, **When** docs land, **Then** Proposed ADR 0047 (`docs/adr/0047-for-you-nearest-seeds.md`) is in that delivery diff with ADR 0034 supersession of the signed formula, CONTEXT Taste Profile rewrite, and the documented reason table (not landed separately at Gate 1).
3. **Given** the DEV-545 snapshot on the 2026-11-16 home library, **When** For you top 10 is inspected, **Then** at least four distinct best seeds appear, and the pairs The Good, the Bad and the Ugly → A Fistful of Dollars, Hercules → Aladdin, and Pirate Radio → About Time are in the top 10; prohibited reason strings are absent.
4. **Given** before/after DEV-545 leave-one-out hit rate and MRR plus warm runtime measurement, **When** the PR is prepared, **Then** hit rate has not declined and warm For you is under 500 ms for 450 movies / 150 liked, with fixture and run evidence attached.

### Edge Cases

- No positive seeds → empty For you list (no watchlist-only special fallback beyond ordinary ranking when watchlist seeds exist).
- Positive seeds exist but no candidate has a strong positive pair after self-exclusion → empty list, never padded.
- Candidate equals a positive seed (watchlisted unwatched) → that seed is excluded from that candidate's positive comparisons (self-exclusion).
- Fewer than three positive products → sum the available products with the leading rank multipliers.
- Best strong seed outside the top-three score contributors → still named on the card.
- Recency at floor → liked seed still carries 0.25 × its weight.
- Incomplete recon caller inventory → cleanup tasks remain conditional on fresh caller checks before plan freeze / removal.
- DEV-518 / DEV-544 / DEV-510 not merged → implementation does not start (brief Task order step 1).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: For you MUST rank unwatched, not-disliked candidates by nearest-seed signed aggregate using DEV-518 similarity and configured positive seed weights (defaults liked 1.0, watched-without-review 0.6, watchlist 0.4), top-three rank multipliers (defaults 1, 0.5, 0.25), and disliked weight (default 1.0) × max similarity to any disliked seed (`brief.md` Scoring; Q5).
- **FR-002**: System MUST resolve each movie to a single strongest seed kind before recency (disliked negative only; liked > watched-without-review > watchlist) (`brief.md` Q5).
- **FR-003**: System MUST show a candidate only when at least one positive pair is a strong match under the shared DEV-544 options (default bar 0.225; Collection, Director or Themes at value 1), evaluated on raw pair similarity/breakdown before weight, recency, aggregation, and disliked penalty (`brief.md` Q4).
- **FR-004**: System MUST select the card best seed from strong positive pairs by recency-adjusted weighted similarity and break remaining ties by ascending movie ID; candidate ordering uses the same final ID tie-break after existing keys (`brief.md` Q6; `ASSUMPTIONS.md`).
- **FR-005**: For you reasons MUST place the seed template first in the existing `reasons` list, then DEV-518 evidence for that same pair, using the three templates in `brief.md`; MUST NOT emit the three superseded generic templates; OpenAPI and generated client types MUST keep the existing shape (`brief.md` Scoring item 5).
- **FR-006**: Positive seeds with playback MUST apply existing exponential decay with configurable multiplier floor default 0.25 via a shared helper; positives without playback and the disliked/strong-match raw checks MUST remain unscaled (`brief.md` Q3).
- **FR-007**: For you MUST load library features once per request via the DEV-544 library feature load, derive seeds and candidates from that load with consistent signal mapping, and MUST NOT use paged catalog walk, per-movie `GetAsync`, or a second profile-inputs read on cache miss; cached data MUST NOT grow with library size × embedding size (`brief.md` Scoring item 1; Wanted 8/10).
- **FR-008**: Related MUST score with DEV-518 similarity to the selected movie plus existing `AlreadyWatchedPenalty` and disliked exclusion, MUST NOT apply taste-profile contribution or profile reasons, and MUST keep scores stable when an unrelated review is added (`brief.md` Q2).
- **FR-009**: After both rails stop using them, and only after recorded caller checks, the ticket-named legacy profile members in Wanted 11 MUST be removed except `AlreadyWatchedPenalty`; DEV-518 owns year/runtime/rating weight option removals (`brief.md` Q2; Wanted 11).
- **FR-010**: The same **delivery** PR MUST include Proposed ADR 0047 at `docs/adr/0047-for-you-nearest-seeds.md` (Keel-drafted; Conductor-claimed after the historical 0045 collision with DEV-545; preserved for delivery rather than landed separately at Gate 1), mark the signed-formula part of ADR 0034 superseded, rewrite the Taste Profile entry in `CONTEXT.md`, and document the reason table in one place mirrored in docs (`brief.md` Decision record; Wanted 12).
- **FR-011**: Verification MUST include the unit/integration suites, unchanged `OpenApiContractTests` snapshot, applicable repo gates with honest status vocabulary, DEV-545 before/after quality (hit rate not lower) and warm runtime under 500 ms on the named fixture, the 2026-11-16 top-10 diversity/named-pair checks, and Compose UI execution of SMOKE-001 and SMOKE-002 with recorded build identity, fixture, and both rail-change outcomes in `EVIDENCE.md` (no `/web` source edits) (`brief.md` Acceptance and verification; Q7).
- **FR-012**: Review closing (T023) MUST depend on T021/T022/T024 plus Q7, and MUST fix and verify all accepted in-scope findings including Low across Sentry, Ledger and Compass within two fix/review rounds, with frozen scope ending “anything else is a follow-up issue, not a finding in this round” (`brief.md` Q7–Q8).

### Key Entities

- **Positive seed**: Liked, watched-without-review, or watchlist movie used for nearest-seed comparison (self excluded per candidate).
- **Negative seed**: Disliked movie contributing only the max-similarity penalty term.
- **Qualifying candidate**: Unwatched, not disliked, with ≥1 strong positive pair.
- **Nearest-seed result**: Ranked candidate with best strong seed identity/signal and ordered reasons.
- **Shared strong-match policy**: DEV-544 bar + strong-reason list reused by For you and Related surfaces.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On the 2026-11-16 home library snapshot, For you top 10 includes at least four distinct best seeds and the three named seed→movie pairs from Done when.
- **SC-002**: No For you result in handler tests or the DEV-545 snapshot carries the three superseded generic reason templates; Related tests show no "Matches your" reasons.
- **SC-003**: Adding a review to an unrelated movie does not change Related scores in handler tests.
- **SC-004**: A watchlisted movie with unchanged features changes For you rank versus its pre-watchlist rank in unit tests.
- **SC-005**: Candidates without a strong-match seed are omitted even with a high weak-seed aggregate; director-only strong matches qualify and name that seed.
- **SC-006**: For you performs one DEV-544 library feature load per request with no per-movie `GetAsync` and no second profile read on cache miss.
- **SC-007**: DEV-545 leave-one-out hit rate after the change is not lower than before; warm For you responds under 500 ms with 450 movies and 150 liked movies.
- **SC-008**: `dotnet test` for unit and integration projects passes and the OpenAPI contract snapshot is unchanged.
- **SC-009**: Proposed ADR 0047, ADR 0034 supersession note, CONTEXT Taste Profile rewrite, and reason table docs land together in the same **delivery** PR (not Gate 1 alone).
- **SC-010**: SMOKE-001 and SMOKE-002 both execute against the described Compose UI on a recorded build identity and fixture with both rail changes present, and record pass/fail outcomes in `EVIDENCE.md` without `/web` source edits.

## Assumptions

- Patron `[assumed]` ascending movie ID final tie-breaker in `specs/DEV-543/ASSUMPTIONS.md` is binding.
- DEV-518, DEV-544 and DEV-510 merges are prerequisites before Build implementation (brief Task order step 1); this worktree may not yet contain their landed contracts — Phase B pickup recon pins exact symbols and verifies typed options binding/`ValidateOnStart` wiring.
- Historical recon caller inventory was incomplete; current `recon-DEV-543` / `recon-DEV-543-2` notes are superseded or recovery-limited (`DRAFTING_RECEIPT.md`) — cleanup remains conditional on fresh caller checks; Quill does not invent a complete inventory here. Phase A may challenge that conditional plan; Phase B pickup checks the already-planned cleanup guard before deletions.
- Nearest-seed ADR path is Proposed `docs/adr/0047-for-you-nearest-seeds.md` (Conductor claim; Keel draft preserved for delivery). Historical ticket/grill 0045 naming is provenance only.
- Quill does not author ADRs; T017 verifies Proposed ADR 0047 lands in the delivery diff with T018/T019.
