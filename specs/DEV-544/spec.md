# Feature Specification: Whole-library Related strong matches

**Feature Branch**: `feature/544-spec`
**Created**: 2026-11-19
**Status**: gate1: provisional
**Input**: Ticket DEV-544 (parent DEV-490; size M); grill-closed `brief.md` (Q1–Q6); `CONCLUSIONS.md`; Maestri notes `DEV-544`, `recon-DEV-544`; `specs/PRODUCT.md`; `specs/DEV-544/ticket-text.txt`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Review loop / Settled grill answers; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Quill does not invent PLAN FROZEN or a structural checkbox.

## Clarifications

### Session 2026-11-19

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope (whole-library feature load, Related strong-match eligibility and Tier A/B ordering, option/`take` descriptions, empty-rail copy, required automated and runtime evidence), port/result contract (Q3), missing-embedding rules (Q2), embedding lifetime (Q4), option domains (Q5), and review loop (Q6) are frozen in `brief.md` / `CONCLUSIONS.md`. Presentation empty-rail copy is ticket Goal / Open points authority, not a new Patron taste invent. **Drafting clarification for Q1 and Q5** (brief): draft against the DEV-518 scorer contract; map Collection, Director, Themes directly to DEV-518 breakdown criteria; below-bar admission uses enabled criterion value 1 on the unblended/unpenalized breakdown; DEV-518 owns Themes normalization and its ceiling; Build pickup requires merged DEV-518 with six-criterion breakdown and normalized Themes (absent → dependency blocker to Bernstein); no Keyword alias, no DEV-544 ceiling option, no DEV-518 criteria/weight/reason changes. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## What the User Will See *(mandatory)*

- **Where**: Movie details Related rail (`RelatedMovies`)
- **What the user sees or does**: The rail lists only strong matches from the whole library (shorter lists are fine). Matches that clear the similarity bar appear before matches admitted only on a full Collection, Director, or Themes reason. When nothing qualifies, the empty success state reads “No close match in your library.”
- **What it needs to show up**: Default Compose / home stack with enriched library movies; Related recommendations endpoint; default `MinimumSimilarity` / `StrongReasonCriteria` (or configured equivalents). Home-library Goal first placements (Pirate Radio first for About Time; Hercules first for Aladdin) are verified with the DEV-545 snapshot script on the 2026-11-16 dataset (runtime acceptance), not only unit tests.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a successful Related response with zero movies, **When** the movie-details Related section renders, **Then** the page shows “No close match in your library.” and does not show a related carousel.
- **SMOKE-002**: **Given** a successful Related response with one or more strong matches, **When** the Related section renders, **Then** those movies appear in API order under the Related heading (no padding invented by the UI).

## Scope

**In scope** (`brief.md` Review loop frozen scope; Settled grill Q1–Q5; ticket Wanted 1–7, 9–10 and Done when; Out of scope / Needs for boundaries):

1. **Whole-library feature load** on existing `IRecommendationCandidateSource`: one method returns, for every enriched library movie, the DEV-518 feature sets plus summary fields, watched/review/watchlist state, and optional plot embedding; fixed database round trips that do not grow with library size; enriched filter retained; missing feature row allowed for candidates.
2. **Related scoring path**: Related exclusively uses the new load; scores every enriched library movie except the selected one with the DEV-518 function; removes plot top-50 cutoff and same-collection union; preserves disliked exclusion, taste-profile blend, `AlreadyWatchedPenalty`, MMR, and seed collection grouping.
3. **Strong matches only**: eligibility on raw pair similarity and the DEV-518 unblended/unpenalized breakdown before blend/penalty — at/above `MinimumSimilarity`, or below the bar with an enabled `StrongReasonCriteria` criterion (Collection, Director, or Themes mapped directly to the matching DEV-518 breakdown criteria) at value 1 (including Themes-only admission at value 1); just-below-ceiling Themes (< 1) without another enabled strong reason at 1 remains excluded; DEV-518 owns the normalized Themes value and its ceiling; never pad; `take` is a maximum; Tier A (above bar) strictly before Tier B (strong-reason-only) in the final rail.
4. **`CandidatePoolSize`**: keep name and default 50; document sole role as `take` cap; update option description, OpenAPI `take` description, and the two committed baseline description strings; both endpoints still reject `take` above the cap with 400.
5. **Empty-rail copy**: Related empty success text becomes “No close match in your library.”
6. **Embeddings**: load per request with features; no cache that scales with library size × embedding size; taste-profile cache stays profile-only; documentation/measurement review trigger at > 450 enriched movies (no runtime cap).
7. **Evidence**: unit/integration/property coverage named in the brief; DEV-545 before snapshot as an explicit prerequisite before replacing the Related path; DEV-545 after snapshot after automated verification; Goal first-placement checks; quality coverage table, score-band samples, and judged-pair attachments (ticket-named odd keyword pairs recorded as bad judgments; stoplist/`ThemesCeiling` routed to DEV-518; defaults frozen); warm < 500 ms on the ticket 450-movie warm benchmark; DEV-518 Plot evidence when supplied; selected-relative cosine verification if the legacy Similar story trigger survives; unchanged For-you leave-one-out stated on the PR.

**Out of scope**:

- For you migration/scoring and removal of per-movie `GetAsync` (DEV-543).
- Changes to DEV-518 criteria, weights, or reasons.
- Removing Related taste-profile scoring (DEV-543 decides).
- Paging, caps and exclusions beyond this ticket’s strong-match filter and existing disliked exclusion.
- Renaming `CandidatePoolSize`.
- New NuGet/npm dependency, project, top-level folder, architectural layer, database schema, public route/DTO/status-code shape, `Features:LocalPlay`, secrets, or `Process.Start`.
- Keyword alias for Themes; any DEV-544-owned ceiling option; any change to DEV-518 criteria, weights, or reasons (brief Drafting clarification for Q1 and Q5).
- Claiming PLAN FROZEN, Gate 1 merge, implementation PASS, or runtime acceptance from this draft.
- Implementing Build work before a merged DEV-518 baseline that exposes the six-criterion breakdown and normalized Themes (absent on pickup → dependency blocker to Bernstein).

**Precedence**: Ticket Scope & Technical Design and acceptance criteria are already approved authority (`PRODUCT.md` §3). Brief Q1–Q6 and `CONCLUSIONS.md` govern plan boundaries. Only Keel declares PLAN FROZEN after independent reread / analyze.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Whole-library feature load**
One port call that returns feature/state/embedding data for every enriched library movie with a fixed database round-trip count.
_Avoid_: per-movie `GetAsync`; plot-nearest pool sizing; same-collection second query

**Strong match**
A candidate whose raw DEV-518 pair similarity is ≥ `MinimumSimilarity`, or below that bar with at least one enabled `StrongReasonCriteria` criterion at value 1 on the unblended/unpenalized DEV-518 breakdown (Collection, Director, Themes mapped by those exact criterion names), evaluated before taste blend and watched penalty. DEV-518 owns Themes normalization and its ceiling.
_Avoid_: padding the rail to `take`; Keyword aliases; a DEV-544 ceiling option; treating Plot/Cast/Genre alone as sufficient; treating identity-fallback as a strong reason

**Tier A / Tier B**
Tier A = above-or-equal bar; Tier B = strong-reason-only below the bar. Final Related order keeps every Tier A result before every Tier B result through blend, MMR, and collection grouping.
_Avoid_: letting blend/diversity/grouping promote a Tier B movie ahead of any Tier A movie

**CandidatePoolSize (post-change)**
Configuration that caps request `take` on recommendation endpoints; it no longer sizes a Related candidate pool.
_Avoid_: using it to truncate the library feature load

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Whole-library feature load (Priority: P1)

Related (and later For you via DEV-543) obtains every enriched movie’s DEV-518 features, summary fields, state flags, and optional embedding in one fixed-cost load, without plot-pool truncation or a same-collection union query.

**Why this priority**: Without the uncapped load, director/theme matches outside the old plot top 50 never reach the scorer (About Time → Pirate Radio; Aladdin → Hercules).

**Independent Test**: Integration command counts for one Related request at 20 vs 200 movies are equal; rewritten former cap/`LIMIT` tests prove uncapped enriched loading with optional embeddings; selected movie with unusable vector still yields empty rail; enriched candidate without embedding remains loadable.

**Acceptance Scenarios**:

1. **Given** an enriched library, **When** the whole-library feature load runs, **Then** every enriched movie is returned with id, title, release year, people/genre/keyword sets, collection id, watched/review/watchlist state, and plot embedding when present, using a fixed number of database round trips.
2. **Given** the EF implementation of the retained `GetCandidatesAsync` path, **When** it loads candidates, **Then** it reflects the uncapped feature-and-state load (no `Take(CandidatePoolSize)` plot cutoff, no same-collection union) while keeping the enriched filter and selected-vector empty guard.
3. **Given** libraries of 20 and 200 movies, **When** one Related request is recorded with the existing interceptor, **Then** database command counts are equal.
4. **Given** an enriched candidate with a missing feature row or unusable embedding, **When** the load runs, **Then** the candidate remains present with Plot treated as 0 and other DEV-518 criteria evaluated normally.

---

### User Story 2 - Strong-match Related rail (Priority: P1) 🎯 MVP

On a movie’s Related rail, the user sees only strong matches from the whole library, ordered so above-bar matches precede strong-reason-only matches, with existing disliked exclusion, taste blend, watched penalty, MMR, and collection grouping preserved inside that contract.

**Why this priority**: Delivers the ticket Goal — short rails of close matches, including pairs the old pool hid.

**Independent Test**: Fake 60-movie source with lowest-plot director match appears; no-embedding director-only admission; bar-above-all + empty strong-reason list → HTTP 200 empty list; under-bar director-only after all above-bar; below-bar Themes-only at value 1 admitted; just-below-ceiling Themes (< 1) without director/collection excluded; changing configured `MinimumSimilarity` / `StrongReasonCriteria` changes eligibility; live Related handler selected-vector empty guard after whole-library switch with regression test; MMR prefix/tie tests stay green; DEV-545 snapshot verifies Goal first placements (Pirate Radio first for About Time; Hercules first for Aladdin) under defaults (runtime).

**Acceptance Scenarios**:

1. **Given** raw pair similarity ≥ `MinimumSimilarity` (default 0.225), **When** Related builds the rail, **Then** the movie is eligible (Tier A) before blend/penalty.
2. **Given** similarity below the bar and an enabled strong-reason criterion at value 1, **When** Related builds the rail, **Then** the movie is eligible as Tier B and appears after every Tier A result in the final rail, even if blend/MMR/grouping would otherwise promote it.
3. **Given** similarity below the bar with Themes as the only enabled strong reason at DEV-518 breakdown value 1, **When** Related builds the rail, **Then** the movie is admitted as Tier B.
4. **Given** similarity below the bar with DEV-518 Themes breakdown value just under 1 (just-below-ceiling) and no Collection or Director criterion at 1 (and no other enabled strong reason at 1), **When** Related builds the rail, **Then** the movie is excluded.
5. **Given** fewer strong matches than `take`, **When** Related returns, **Then** the list is shorter or empty and is never padded; empty success is HTTP 200.
6. **Given** a selected movie with no usable plot embedding on the live Related handler path after the whole-library method switch, **When** Related runs, **Then** the rail is empty (candidate-side rollout only; regression-covered).
7. **Given** disliked exclusion, taste-profile blend, `AlreadyWatchedPenalty`, MMR, and seed collection grouping, **When** Related runs, **Then** those stages remain, subject to the Tier A-before-Tier B contract; `MmrSelector` tie-order and cross-`take` prefix tests stay green.
8. **Given** a change to configured `MinimumSimilarity` or `StrongReasonCriteria` without a code change, **When** Related builds the rail, **Then** eligibility changes accordingly (automated assertion).

---

### User Story 3 - Take-cap documentation and empty-rail copy (Priority: P2)

Operators and API consumers see that `CandidatePoolSize` only caps `take`, OpenAPI `take` descriptions match that role, and the Related empty success copy matches the ticket Goal.

**Why this priority**: Prevents the old “pool size” reading from surviving in options/docs/UI after the load change.

**Independent Test**: Existing 400 cap tests on both endpoints stay green; OpenAPI drift limited to the two `take` description strings; RelatedMovies empty-success copy test expects “No close match in your library.”

**Acceptance Scenarios**:

1. **Given** `take` above `CandidatePoolSize`, **When** Related or For you is requested, **Then** the API still returns 400 (existing endpoint tests remain green).
2. **Given** the OpenAPI contract workflow, **When** descriptions are regenerated/verified, **Then** only the `take` description text changes among committed baseline strings (lines cited by the ticket), if at all.
3. **Given** a successful empty Related response, **When** the Related section renders, **Then** the user sees “No close match in your library.”

---

### Edge Cases

- Selected movie missing/unusable embedding → empty Related rail (preserved).
- Candidate missing feature row or unusable embedding → still eligible; Plot = 0; other criteria count.
- Empty `StrongReasonCriteria` with bar above every fixture pair score → HTTP 200 empty list.
- `MinimumSimilarity` at inclusive boundary equals the bar → Tier A.
- Below-bar Themes-only candidate at breakdown value 1 → admitted as Tier B.
- Below-bar candidate whose enabled Themes breakdown value is just under 1 (just-below-ceiling) and whose Collection/Director values are not 1 → excluded.
- Themes boundary tests drive the merged DEV-518 scorer and its configured ceiling (no DEV-544 ceiling knob).
- Changing `MinimumSimilarity` / `StrongReasonCriteria` via configuration changes eligibility without a code change (automated assertion required).
- Live Related handler retains the selected-vector empty guard after switching to the whole-library method (regression test required).
- Library larger than 450 enriched movies → no runtime cap/paging/cache in this ticket; measurement review trigger only.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose a whole-library feature-load method on existing `IRecommendationCandidateSource` returning DEV-518 `MovieFeatures` plus required summary fields, watched/review/watchlist state, and optional plot embedding (and selected-relative cosine where applicable), built via `MovieFeaturesMapper` with signals (`brief.md` Q3).
- **FR-002**: System MUST load enriched library features in a fixed number of database round trips that does not grow with library size, retaining request cancellation and the enriched status filter (`brief.md` Implementation approach 2; Wanted 1).
- **FR-003**: Related MUST use the whole-library load exclusively for library features, exclude the selected movie, remove plot top-50 cutoff and same-collection union, and keep an explicit selected-vector empty guard on the live Related handler path after switching to the whole-library method (plus the EF retained-path guard), with a regression test (`brief.md` Q2–Q3).
- **FR-004**: Related MUST score candidates with the DEV-518 similarity function and apply strong-match eligibility on raw pair similarity/breakdown before taste-profile blend or `AlreadyWatchedPenalty` (`brief.md` Q1).
- **FR-005**: System MUST treat a movie as a strong match when pair similarity ≥ `MinimumSimilarity` (default 0.225, finite ∈ [0,1]) or when below the bar with ≥ 1 enabled `StrongReasonCriteria` criterion at value 1 on the unblended/unpenalized DEV-518 breakdown, including Themes-only admission at value 1; just-below-ceiling Themes (< 1) without another enabled strong reason at 1 MUST be excluded; defaults Collection, Director, Themes mapped directly to those DEV-518 criterion names; empty list valid; reject null entries and unsupported criteria including Plot, Cast, Genre, and unknown names; automated tests MUST assert that changing the configured bar or strong-reason list changes eligibility without a code change (`brief.md` Q5; Drafting clarification).
- **FR-006**: System MUST NOT invent a Keyword alias or a DEV-544 ceiling option; Themes reaching value 1 is owned entirely by the merged DEV-518 scorer (normalization and configured ceiling). Themes boundary tests MUST use that scorer and ceiling. DEV-518 criteria, weights, and reasons MUST remain unchanged (`brief.md` Drafting clarification for Q1 and Q5).
- **FR-007**: Final Related ordering MUST keep every Tier A match before every Tier B match through blend, MMR, and seed collection grouping; `take` is a maximum and MUST NOT pad (`brief.md` Q1).
- **FR-008**: System MUST preserve disliked exclusion, Related taste-profile scoring, `AlreadyWatchedPenalty`, MMR, and `SeedCollectionGrouper` as after DEV-518, subject to FR-007 (`brief.md` Q1; Wanted 6).
- **FR-009**: Enriched candidates without usable embeddings MUST remain eligible with Plot = 0; selected movies without usable embeddings MUST keep an empty rail on the live Related handler path (`brief.md` Q2).
- **FR-010**: Embeddings MUST load per request with features; nothing that scales with library size × embedding size may be cached; taste-profile cache remains profile-only; > 450 enriched movies is a documentation/measurement review trigger only (`brief.md` Q4).
- **FR-011**: `CandidatePoolSize` MUST keep its name and default 50 and MUST be documented solely as the `take` cap; both endpoints MUST keep 400 rejection when `take` exceeds it; OpenAPI/`RecommendationOptions` descriptions and the two committed baseline strings MUST be updated; committed `web/src/api/openapi.json` and downstream generated outputs MUST be regenerated and verified via the API contract chain (`brief.md` Implementation approach 4).
- **FR-012**: Related empty-success UI copy MUST be “No close match in your library.”; scoped Vitest/RTL MUST assert empty copy and no carousel on empty success, and Related heading plus API order without padding on non-empty success (SMOKE-001/002) (`brief.md` Implementation approach 4; ticket Goal).
- **FR-013**: For you behaviour MUST remain unchanged in this ticket (`brief.md` frozen scope; Wanted 8).
- **FR-014**: DEV-518 reason presentation MUST be preserved; if DEV-518 supplies Plot evidence, use it; if the legacy “Similar story” trigger still exists, it MUST use the actual selected-relative cosine and that behaviour MUST be verified on the DEV-545 snapshot (`brief.md` Implementation approach 1).
- **FR-015**: DEV-545 before snapshot on the 2026-11-16 home-library fixture MUST be an explicit prerequisite before replacing the Related path; DEV-545 after snapshot, Goal first-placement checks, quality **coverage table**, **score-band samples**, **judged-pair** attachments (including the ticket-named odd keyword pairs recorded as bad judgments), and 450-movie warm timing MUST run only after automated verification; stoplist/`ThemesCeiling` follow-ups for those pairs route to DEV-518 while DEV-544 defaults stay frozen (`brief.md` Q5; Implementation approach 1 and 5).

### Key Entities *(include if feature involves data)*

- **Library feature row**: One enriched movie’s DEV-518 features, summary/display fields, state flags, and optional embedding used for Related scoring.
- **Strong-match decision**: Tier A/B eligibility derived from raw pair similarity, breakdown/Themes ceiling check, and configured options before blend/penalty.
- **Related rail result**: Ordered strong matches after blend, MMR, and collection grouping, truncated to caller `take` without padding.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On the 2026-11-16 home-library dataset with frozen default options (`MinimumSimilarity` 0.225; `StrongReasonCriteria` Collection, Director, Themes — no silent default change), About Time’s Related list places Pirate Radio first and Aladdin’s places Hercules first (Goal first-placement). DEV-545 PR evidence MUST include the before/after snapshot diff, the quality **coverage table**, **score-band samples**, and **judged-pair** attachments. Ticket-named odd keyword pairs MUST be recorded as bad judgments in those judged pairs: a How to Train Your Dragon film ↔ About Time (themes only, 0.200) and The Untouchables ↔ It Was Just an Accident (themes only, 0.253). Stoplist or `ThemesCeiling` remediation for those pairs routes through DEV-518’s mechanism (brief Q5); DEV-544 does not add a second rule or change frozen defaults.
- **SC-002**: Related warm response completes in under 500 ms on the ticket **450-movie** warm benchmark (DEV-545 measurement; Wanted 10).
- **SC-003**: Database command count for one Related request is identical at 20 and 200 movies (measured on the live Related path after the whole-library handler switch).
- **SC-004**: With bar above every fixture pair score and empty strong-reason list, Related returns HTTP 200 with an empty list.
- **SC-005**: A director-only under-bar match appears after every above-bar match; a below-bar Themes-only match at value 1 is admitted; a just-below-ceiling Themes (< 1) match without director/collection does not appear; changing the configured bar changes eligibility.
- **SC-006**: OpenAPI committed baseline drift is limited to `take` description text; committed `web/src/api/openapi.json` and downstream generated outputs are regenerated/verified via the contract chain; For-you leave-one-out numbers are unchanged and stated on the PR.
- **SC-007**: Empty Related success UI shows “No close match in your library.” and no carousel; non-empty success shows the Related heading and movies in API order without UI padding.
- **SC-008**: `dotnet test` passes for UnitTests, IntegrationTests, and `LamuFlix.ArchitectureTests` covering the brief-named suites; existing MMR and endpoint cap tests stay green.

## Assumptions

- DEV-518 is the scoring/reason baseline this ticket feeds; For you migration remains DEV-543; snapshot/quality/timing evidence uses DEV-545.
- Empty-rail copy “No close match in your library.” is ticket authority (Goal / Open points), not a new Patron `[assumed]` taste invent for this draft.
- Quill-named identifier `GetLibraryFeaturesAsync` on `IRecommendationCandidateSource` is a drafting label within the ticket-owned port file; Keel may rename in analyze without changing behaviour.
- Build pickup requires merged DEV-518 with six-criterion pair score/breakdown and normalized Themes; if absent on the pickup base, report a dependency blocker to Bernstein (brief Drafting clarification). Drafting itself proceeds against that contract now.
- No new public route, DTO field set, or HTTP status code is introduced; empty Related remains 200; invalid `take` remains 400.
