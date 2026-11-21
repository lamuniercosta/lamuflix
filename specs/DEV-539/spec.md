# Feature Specification: TMDb Crowd similarity criterion

**Feature Branch**: `feature/539-spec`
**Created**: 2026-11-21
**Status**: gate1: provisional
**Input**: Ticket DEV-539 (size L); grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; Maestri notes `recon-DEV-539`, `dev-539`; `specs/PRODUCT.md`; `specs/DEV-539/ticket-text.txt`; Accepted ADR `docs/adr/0048-tmdb-crowd-similarity.md`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Q8 / frozen scope; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Quill does not invent PLAN FROZEN or a structural checkbox.

## Clarifications

### Session 2026-11-21

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope (Crowd link model, TMDb id + first-page recommendations persistence, one-off Api operator backfill, library feature-load resolution, scoring/reasons/activation, DEV-545 evidence gate, file boundaries, Size L review bar) is settled in `brief.md` Q1–Q8 and `CONCLUSIONS.md`. Ticket Wanted / AC / Out of scope / Open-point defaults are already approved authority (`PRODUCT.md` §3). No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## What the User Will See *(mandatory)*

- **Where**: For you rail and Related rail reason lines (existing reason presentation; no frontend file change in this ticket)
- **What the user sees or does**: When Crowd is active and TMDb is configured, a linked pair may surface the Crowd reason — For you: “Often liked together with {seed title}”; Related: “Often liked together” — via the existing reason channel. When Crowd is inactive or TMDb is unconfigured, Crowd reasons do not appear and recommendation scores match the pre-change DEV-545 baseline for those states.
- **What it needs to show up**: TMDb configured (`Enabled` true and nonblank `ApiKey`); Crowd activation on; library movies with persisted TMDb ids and recommendation links (via enrichment and/or the one-off backfill); DEV-543 For you and DEV-544 library feature-load seams available for scoring integration.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** Crowd active, TMDb configured, two linked library movies, and a fixture where Crowd wins under the existing reason-selection rules, **When** For you returns a recommendation that includes a Crowd contribution for that pair, **Then** the reason list includes “Often liked together with {seed title}” (exact ticket copy with the seed title substituted). Conditioned on existing selection rules; no separate reason path.
- **SMOKE-002**: **Given** Crowd active, TMDb configured, a linked Related pair, and a fixture where Crowd wins under the existing reason-selection rules, **When** Related returns that movie, **Then** the reason list includes “Often liked together”. Conditioned on existing selection rules; no separate reason path.
- **SMOKE-003**: **Given** TMDb unconfigured (`Enabled` false or blank `ApiKey`) or Crowd explicitly inactive, **When** For you and Related are scored, **Then** no Crowd reason appears and scores match the pre-change DEV-545 snapshot for that state.

Smoke execution for this ticket is automated unit/integration coverage of reason emission and score parity (no frontend file change; never Compose UI smoke; never `web/e2e`).

## Scope

**In scope** (`brief.md` frozen scope and Q1–Q8; ticket Wanted 1–7, AC 1–10, Open-point defaults):

1. **ADR 0048** Accepted amending ADR-0011 for external catalogue item-to-item similarity; library-only candidates and prohibition on local peer-history collaborative filtering stand. Spec review includes the Accepted ADR; coding waits on merged Gate 1.
2. **Persistence**: nullable `movies.tmdb_id`; pair table of unique `(source library movie id, recommended TMDb id)` including out-of-library targets; EF mapping and migration applying on a database that already holds the home library.
3. **Enrichment**: map and persist TMDb id on successful TMDb enrichment; fetch first page only of `movie/{id}/recommendations` through the existing resilient typed client; replace rows on successful fetch (including empty page); non-fatal failure preserves rows when TMDb id unchanged and clears stale rows when it changes; no second rate limiter; no keyword/cast/trailer enrichment changes.
4. **One-off backfill**: explicit operator mode on existing `LamuFlix.Api` after configuration/DI and before the web host; gated by Enabled + nonblank ApiKey; one logical IMDb find per eligible movie and one first-page recommendations fetch only after a match; persist TMDb id and link rows atomically together per movie (failure leaves `tmdb_id` null with no partial link write); skip missing IMDb ids and stored TMDb ids; leave unmatched and failed movies retry-eligible; report IMDb-eligible, matched, unmatched, skipped, and failures as distinct counts without exposing the key; no new package, project, layer, public endpoint, or `Process.Start`.
5. **Link resolution**: at the DEV-544 library feature-load seam (`EfRecommendationCandidateSource`); either direction yields one binary link; no self-links; no reciprocal bonus; no remote calls during scoring; late library additions link without re-enriching the existing movie.
6. **Crowd scoring**: binary criterion after Themes for For you and Related; default weight 2; value 1 linked / 0 otherwise; denominator includes Crowd weight when active even for unlinked pairs; omit Crowd when inactive including when TMDb unconfigured; exact score parity with pre-change DEV-545 when unconfigured; independent Crowd activation under `RecommendationOptions`.
7. **Activation and threshold**: checked-in Crowd default off unless same-library leave-one-out top-10 hit rate with Crowd enabled is ≥ baseline; attach before/after hit rate and MRR; keep existing strong-match bar when inactive; decide Crowd-active bar from DEV-545 coverage table and judged pairs.
8. **Evidence**: WireMock recommendations path/first-page tests; migration and backfill integration coverage; late-add link test; criteria table / weight / denominator / reason pins; score-parity tests; home-library migration + backfill output + DEV-545 numbers on the delivery PR.

**Out of scope**:

- Showing movies that are not in the library.
- TMDb “similar” lists, TMDb user ratings, any second external source.
- Changes to keyword, cast, or trailer enrichment.
- Local peer-history collaborative filtering.
- Public HTTP route, DTO field set, status-code, or frontend file changes.
- New NuGet/npm dependency, project, top-level folder, architectural layer, second rate limiter, or `Process.Start`.
- Secrets written into code, ticket, or backfill output.
- Changes to `Features:LocalPlay`.
- Claiming PLAN FROZEN, Gate 1 merge, implementation PASS, or runtime acceptance from this draft.

**Precedence**: Ticket Scope & Technical Design and acceptance criteria are already approved authority (`PRODUCT.md` §3). Brief Q1–Q8 and `CONCLUSIONS.md` govern plan boundaries. Only Keel declares PLAN FROZEN after independent reread / analyze.

**Dependencies**: DEV-543 (For you nearest-seeds consumer), DEV-544 (library feature-load seam), DEV-545 (before/after harness). ADR 0048 Accepted before coding. Spec PR Gate 1 merged before coding.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only)*

**Crowd link**
A binary association between two distinct library movies that exists when either movie’s stored first-page recommendations include the other’s TMDb id. Value is 1 when linked, 0 otherwise; one-way equals two-way; no self-links; no extra weight for reciprocation.
_Avoid_: remote lookups during scoring; padding scores for reciprocation; linking to non-library titles in returned candidates

**Recommended TMDb id**
A TMDb catalogue id stored on a source library movie’s recommendation rows. May refer to a movie outside the library and is retained so a later library addition resolves without re-enriching the source.
_Avoid_: discarding out-of-library ids; treating a recommended id alone as a recommendation result

**Crowd criterion**
Binary item-to-item similarity criterion placed after Themes for For you and Related, default weight 2, present in the criteria table and denominator only when TMDb is configured and Crowd is active.
_Avoid_: including Crowd when TMDb is disabled or its key is blank; changing other criteria weights as part of this ticket except the Crowd-active strong-match bar decision from DEV-545 evidence

**Crowd activation**
Independent control under `RecommendationOptions` that can leave Crowd scoring off while TMDb enrichment remains available. Checked-in default stays off unless leave-one-out top-10 hit rate does not fall versus the same-library baseline.
_Avoid_: tying Crowd scoring solely to `Tmdb:Enabled`; enabling by default without DEV-545 evidence

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Persist TMDb identity and recommendation links (Priority: P1)

During TMDb enrichment, each successfully enriched movie stores its TMDb id and the first page of recommended TMDb ids so Crowd can resolve later without further remote calls at scoring time.

**Why this priority**: Without persisted identities and recommendation rows, Crowd cannot resolve library links or seed the home library for quality measurement.

**Independent Test**: WireMock through the real typed `HttpClient` proves `movie/{id}/recommendations` path and first-page-only reads; integration coverage proves persisted `tmdb_id` and link rows, empty-page replacement, and failure rules for unchanged vs changed source ids.

**Acceptance Scenarios**:

1. **Given** TMDb enrichment succeeds for a movie, **When** mapping completes, **Then** the movie’s nullable TMDb id is set from the response id already present in the TMDb details payload.
2. **Given** a successful first-page recommendations fetch (including an empty page), **When** enrichment completes, **Then** that source’s recommendation rows are replaced with the returned set.
3. **Given** a non-fatal recommendations fetch failure and an unchanged TMDb id, **When** enrichment completes, **Then** prior recommendation rows are preserved.
4. **Given** a non-fatal recommendations fetch failure after the source TMDb id changed, **When** enrichment completes, **Then** stale rows for the previous identity are cleared.
5. **Given** WireMock serves multiple recommendation pages, **When** enrichment runs through the real typed client, **Then** only the first page is requested/read and ids are persisted.

---

### User Story 2 - One-off backfill for existing IMDb-identified movies (Priority: P1)

An operator runs an explicit Api executable mode once after deploy to assign TMDb ids and first-page recommendation links to eligible legacy movies without starting the web host as a long-running enrichment job.

**Why this priority**: Home-library Crowd evaluation and DEV-545 before/after measurement require legacy rows to hold the same data new enrichments write.

**Independent Test**: Integration coverage of configuration refusal, missing IMDb ids, stored ids, unmatched finds, matched atomic writes, failed recommendation fetches, repeat invocation, and truthful counts without key exposure; migration applies on existing movie data.

**Acceptance Scenarios**:

1. **Given** TMDb unconfigured, **When** the operator mode is invoked, **Then** it refuses the run without writing rows or exposing secrets.
2. **Given** TMDb configured and movies with IMDb ids but no `tmdb_id`, **When** backfill runs, **Then** each eligible movie gets exactly one logical IMDb find; a recommendations fetch runs only after a successful match; and on match the TMDb id and link rows are persisted together atomically.
3. **Given** movies already holding `tmdb_id` or lacking IMDb ids, **When** backfill runs, **Then** those movies are skipped and counted accordingly (no find, no recommendations fetch).
4. **Given** an unmatched IMDb find, **When** backfill finishes that movie attempt, **Then** `tmdb_id` stays null, no recommendation rows are written, the movie stays retry-eligible, and the attempt is counted as unmatched (not as a failure).
5. **Given** a matched find whose recommendations fetch fails, **When** backfill finishes that movie attempt, **Then** `tmdb_id` stays null with no partial link write, the movie stays retry-eligible, and the attempt is counted as a failure (distinct from unmatched).
6. **Given** a second identical invocation, **When** backfill runs again, **Then** already-stored ids are skipped and counts remain truthful.
7. **Given** a completed home-library run, **When** delivery evidence is prepared, **Then** matched, unmatched, skipped, and failure counts from the actual run are attached to the PR as distinct figures without exposing the ApiKey.

---

### User Story 3 - Resolve Crowd links at library feature load (Priority: P1) 🎯 MVP

When library features load, stored recommendation pairs resolve into symmetric binary Crowd links among library movies only, including links created when a recommended movie is added later.

**Why this priority**: Scoring consumers need in-memory link presence without remote calls; late additions must activate stored out-of-library targets.

**Independent Test**: Integration proof that a movie added after enrichment links to an existing source without re-enriching that source; pins for symmetry, binary contribution, no self-links, and library-only returned movies.

**Acceptance Scenarios**:

1. **Given** two distinct library movies where either stores the other’s TMDb id in recommendations, **When** library features load, **Then** the Crowd link value between them is 1.
2. **Given** only one direction of recommendation, **When** links resolve, **Then** the link value is still 1 (one-way equals two-way).
3. **Given** a movie whose stored recommendations include only out-of-library TMDb ids, **When** features load, **Then** those ids remain stored and no non-library movie appears in recommendation results.
4. **Given** a later library addition whose TMDb id was already stored on an existing movie’s recommendation rows, **When** features load, **Then** the link activates without re-enriching the existing movie.
5. **Given** any recommendation result set after Crowd integration, **When** candidates are returned, **Then** every movie is a library movie.

---

### User Story 4 - Crowd scoring, reasons, and configuration parity (Priority: P1)

For you and Related include binary Crowd after Themes when active, with default weight 2, ticket reason copy, and exact pre-change score parity when Crowd is omitted.

**Why this priority**: Delivers the user-visible similarity signal and preserves unconfigured behaviour.

**Independent Test**: Pin criteria order, default weight, contribution, denominator, and reason text; DEV-545 baselines prove For you and Related score parity separately for disabled TMDb and enabled TMDb with blank key; exercise Crowd active and explicitly inactive with TMDb configured.

**Acceptance Scenarios**:

1. **Given** Crowd active and TMDb configured, **When** scores are computed, **Then** Crowd appears immediately after Themes with default weight 2, value 1 when linked else 0, and its weight is included in the denominator for every pair.
2. **Given** Crowd active, TMDb configured, a linked pair, and a fixture where Crowd wins under the existing reason-selection rules (same winning-Crowd predicate as SMOKE-001/SMOKE-002), **When** reasons are selected, **Then** For you uses “Often liked together with {seed title}” and Related uses “Often liked together”.
3. **Given** `Tmdb:Enabled` false, **When** For you and Related are scored, **Then** Crowd is absent from the criteria table and scores match the pre-change DEV-545 snapshot.
4. **Given** `Tmdb:Enabled` true and blank `ApiKey`, **When** For you and Related are scored, **Then** the same absent-Crowd parity holds.
5. **Given** TMDb configured and Crowd explicitly inactive, **When** scores are computed, **Then** Crowd is omitted while enrichment remains available, and the inactive strong-match bar is preserved.

---

### User Story 5 - DEV-545 evidence and default activation (Priority: P2)

Before enabling Crowd by default, same-library leave-one-out top-10 hit rate and MRR are measured before and after; the strong-match bar is confirmed or retuned from coverage and judged pairs.

**Why this priority**: Ticket ships enabled by default only when quality does not fall; evidence belongs on the delivery PR.

**Independent Test**: Attach before/after hit rate and MRR, coverage table, judged pairs, backfill counts, and the resulting threshold/default decision; a lower hit rate keeps the checked-in default off.

**Acceptance Scenarios**:

1. **Given** the same library and evaluation inputs, **When** DEV-545 runs before and after Crowd, **Then** leave-one-out top-10 hit rate and MRR are published on the PR.
2. **Given** enabled hit rate ≥ baseline, **When** defaults are finalized, **Then** Crowd may ship enabled by default per the evidence-backed decision recorded on the PR.
3. **Given** enabled hit rate lower than baseline, **When** defaults are finalized, **Then** the checked-in Crowd default remains off.
4. **Given** Crowd-active coverage table and judged pairs, **When** the strong-match bar is reviewed, **Then** the inactive bar is kept for inactive states, and either the shared bar holds or a Crowd-active bar applies when Crowd is active.

---

### Edge Cases

- TMDb unconfigured → Crowd absent; score parity with pre-change snapshot.
- Blank ApiKey with Enabled true → same as unconfigured for Crowd presence.
- Empty first recommendations page → successful replacement with zero rows.
- Recommendations fetch failure, TMDb id unchanged → preserve prior rows (normal enrichment).
- Recommendations fetch failure, TMDb id changed → clear stale rows (normal enrichment).
- Backfill partial failure → do not commit TMDb id without its link set; movie stays retry-eligible.
- Self-recommendation id → no self-link.
- Reciprocal recommendations → still value 1 (no bonus).
- Out-of-library recommended ids → stored; never returned as candidates.
- Late library addition matching a stored recommended id → link without re-enrichment.
- Crowd inactive with TMDb configured → enrichment continues; Crowd omitted from scoring.
- Strong-match bar when Crowd inactive → existing bar unchanged.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST keep Accepted ADR 0048 in `docs/adr/` amending ADR-0011 for external catalogue item-to-item similarity while preserving library-only candidates and rejecting local peer-history collaborative filtering (`brief.md` Q1; ticket AC 1).
- **FR-002**: System MUST add nullable `movies.tmdb_id` and a unique pair table of source library movie id + recommended TMDb id (including out-of-library targets), with EF mapping and a migration that applies on a database already holding the home library (`brief.md` Q2–Q3; ticket AC 2).
- **FR-003**: On successful TMDb enrichment, System MUST carry the mapped TMDb id through `MovieMetadata` / `Movie.MarkEnriched` without requiring a `ProcessEnrichmentCommandHandler` behaviour change beyond existing metadata persistence (`brief.md` Implementation order 2; Q6).
- **FR-004**: When TMDb is configured, System MUST fetch only the first page of `movie/{id}/recommendations` through the existing resilient typed `HttpClient`, reuse shared retry/circuit behaviour, and MUST NOT add a second rate limiter (`brief.md` Q3, Q6, Q7; ticket Wanted 3).
- **FR-005**: A successful recommendations fetch (including empty) MUST replace that source’s recommendation rows; a non-fatal failure MUST preserve rows when TMDb id is unchanged and clear stale rows when TMDb id changed; backfill MUST keep stricter per-movie atomicity (`brief.md` Q3, Q6).
- **FR-006**: System MUST provide an explicit operator mode on the existing `LamuFlix.Api` executable after configuration/DI setup and before the normal web host starts, reusing configured TMDb client and dependencies, with no new package, project, layer, public endpoint, or `Process.Start` (`brief.md` Q7).
- **FR-007**: Backfill MUST gate on Enabled + nonblank ApiKey; skip missing IMDb ids and stored TMDb ids; perform one logical IMDb find per eligible movie and one first-page recommendations fetch only after a match; persist TMDb id and link rows together on match; on unmatched find or failed recommendations fetch leave `tmdb_id` null with no partial link write so retry remains eligible; report IMDb eligibility, matched, unmatched, skipped, and failures as distinct counts without exposing ApiKey (`brief.md` Q3; ticket AC 4).
- **FR-008**: System MUST resolve Crowd links in memory at the DEV-544 library feature-load seam without remote calls during scoring; either direction yields one binary link; no self-links; no reciprocal bonus (`brief.md` Q2; ticket AC 5, 9).
- **FR-009**: A movie added later whose TMDb id was stored on an existing movie’s recommendation rows MUST create a link without re-enriching the existing movie (`brief.md` Q2; ticket AC 5).
- **FR-010**: Every recommendation result MUST be a library movie; TMDb-only titles MUST NOT appear (`brief.md` frozen scope; ticket AC 9).
- **FR-011**: When Crowd is active and TMDb configured, System MUST place binary Crowd immediately after Themes for For you and Related with default weight 2, value 1/0, denominator contribution even for unlinked pairs, and ticket reason strings (`brief.md` Q4; ticket Wanted 5, AC 8).
- **FR-012**: When TMDb is unconfigured or Crowd is inactive, System MUST omit Crowd from the criteria table and denominator; For you and Related scores MUST match pre-change DEV-545 snapshots for disabled TMDb and for Enabled+blank key; inactive strong-match bar MUST be preserved (`brief.md` Q4–Q5; ticket AC 6–7).
- **FR-013**: System MUST expose Crowd activation under `RecommendationOptions` independently of TMDb enrichment so enrichment can remain on while Crowd scoring is off (`brief.md` Q5).
- **FR-014**: Checked-in Crowd default MUST remain off unless same-library leave-one-out top-10 hit rate with Crowd enabled is ≥ baseline; before/after hit rate and MRR MUST be attached to the delivery PR (`brief.md` Q5; ticket AC 10).
- **FR-015**: System MUST use DEV-545 coverage table and judged pairs with Crowd active to decide whether the existing strong-match bar holds or a Crowd-active bar applies when Crowd is active (`brief.md` Q5; ticket Open points).
- **FR-016**: Automated tests MUST cover WireMock recommendations path/first-page behaviour through the real typed client; migration against existing data; backfill refusal/skip/match/fail/repeat counts; late-add link without re-enrichment; criteria/weight/denominator/reason pins; score parity; library-only invariant (`brief.md` Test and acceptance strategy).
- **FR-017**: Delivery revision MUST demonstrate home-library migration application, attach actual backfill counts and DEV-545 before/after evidence, satisfy Size L gates and review bar (two rounds, two remediation commits per round; unresolved Critical/High/Medium and accepted Low block), and leave merge to the user (`brief.md` Closing bar).

### Key Entities *(include if feature involves data)*

- **Movie TMDb identity**: Nullable catalogue id stored on a library movie after enrichment or backfill.
- **Recommendation pair**: Unique `(source library movie id, recommended TMDb id)` row owned by the source movie’s recommendation list.
- **Crowd link**: Derived symmetric binary relationship between two distinct library movies resolved at feature load.
- **Crowd scoring term**: Criteria-table entry after Themes with configured weight and ticket reason copy when active.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: WireMock integration through the real typed client proves the recommendations request uses path `movie/{id}/recommendations` and reads only the first page even when more pages exist; persisted TMDb id and recommended ids match that page.
- **SC-002**: Migration adds nullable `movies.tmdb_id` and the pair table and applies successfully on a database that already holds the home library.
- **SC-003**: Backfill output from the actual home-library run reports matched, unmatched, skipped, and failure counts on the PR without exposing ApiKey; repeat run remains safe.
- **SC-004**: A movie added after enrichment creates a Crowd link to an existing movie without re-enriching that existing movie.
- **SC-005**: With `Tmdb:Enabled` false, and separately with Enabled true + blank ApiKey, For you and Related scores match the pre-change DEV-545 snapshot and Crowd is absent from the criteria table.
- **SC-006**: With Crowd active, criteria order places Crowd after Themes, default weight is 2, linked contribution is 1, denominator includes the weight, and reason strings match the ticket copy for For you and Related.
- **SC-007**: Every returned recommendation movie is a library movie (zero TMDb-only titles in results).
- **SC-008**: Delivery PR attaches DEV-545 before/after leave-one-out top-10 hit rate and MRR; Crowd ships enabled by default only if hit rate is not lower; threshold/default decision is recorded from coverage table and judged pairs.
- **SC-009**: Size L verification on the delivery revision records actual gate statuses (Roslyn, complexity, InspectCode, mutation, property tests, vulnerable packages, format, tests; web `SKIPPED (scope-empty)` when empty); failure or `Could not run` blocks; review findings have written dispositions per the closing bar.

## Assumptions

- DEV-543, DEV-544, and DEV-545 land (or are otherwise available) before the work that consumes each (feature-load resolution, scoring integration, and DEV-545 evidence respectively) per `brief.md` sequencing; absence blocks only the consuming phase and is reported to Bernstein, not a silent invent. A pending DEV-545 harness is not an unconditional prerequisite for schema or enrichment.
- Accepted ADR 0048 already present in this worktree is the architectural record for Gate 1 review; coding still waits on merged spec Gate 1.
- Existing reason-selection machinery surfaces Crowd reason strings without frontend file changes.
- Taste ordering “Crowd after Themes” is logged in `ASSUMPTIONS.md` as `[assumed]` and confirmed by Patron Q4.
