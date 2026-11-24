# Feature Specification: Outside-library discovery on For You

**Feature Branch**: `feature/561-spec`
**Created**: 2026-11-24
**Status**: PLAN FROZEN, ready for owner Gate 1
**Input**: Ticket DEV-561 (size L); grill-closed `brief.md` (Q1–Q12); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-561`, `recon-DEV-561`; `specs/PRODUCT.md`; Accepted ADR `docs/adr/0049-for-you-outside-library-discovery.md`.

## Owner question — Gate 1

None remaining from ticket contradiction or constitution departure. Q3 Option A owner checkbox on PR #492 is resolved (`CONCLUSIONS.md` Q3 owner ruling; `brief.md` Q3). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR.

## Clarifications

### Session 2026-11-24

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope (ADR 0049 boundary, additive link metadata, Option A library exclusion, local read contract, seed/score/reason rules, web section and credit, docs/glossary, acceptance evidence, closing bar) is settled in `brief.md` Q1–Q12 and `CONCLUSIONS.md`. Ticket Wanted / Done when / Out of scope / Open-point defaults are already approved authority (`specs/PRODUCT.md` §3), with Wanted 3 and Done when 2 amended by the Q3 Option A owner ruling. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## What the User Will See *(mandatory)*

- **Where**: For You page (`web/src/features/for-you/`), below the existing library recommendation grid
- **What the user sees or does**: A separate section headed **Not in your library yet** appears only after a successful nonempty discovery response. Cards are visually distinct from library cards, show title, reason, optional year, poster or fallback, and an accessible outbound TMDb movie link (new tab, `rel="noopener noreferrer"`). No Play control, no library details navigation, no Not interested control. Day-verified official TMDb attribution sits beside the visible section. While discovery loads, fails, or returns empty, the section stays hidden and the library grid remains usable.
- **What it needs to show up**: TMDb configured (`Enabled` true and nonblank `ApiKey`); Accepted ADR 0049 and Gate 1 merge before coding; DEV-539 Done with recommendation links present; enrichment and/or `--backfill-tmdb-recommendations` has hydrated titled recommendation rows for liked/watched library seeds; at least one outside-library candidate with two distinct eligible seeds.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** TMDb configured, hydrated titled recommendation links, and at least one outside-library candidate with two eligible seeds, **When** the owner opens For You, **Then** the page shows **Not in your library yet** with that candidate’s title, a two-seed reason, and a TMDb outbound link, and the library grid above still loads.
- **SMOKE-002**: **Given** TMDb unconfigured (`Enabled` false or blank `ApiKey`) or no eligible discovery candidates, **When** the owner opens For You, **Then** the discovery section is absent and the library For You grid still works.
- **SMOKE-003**: **Given** a visible discovery card, **When** the owner inspects the card, **Then** there is no Play control and no library-details navigation; the TMDb link opens in a new tab with `rel="noopener noreferrer"`; and the required TMDb credit is visible beside the section.

Smoke execution for this ticket includes web unit/integration coverage plus a local seeded E2E that opens For You and sees the section (`brief.md` Q9–Q10). Compose-stack smoke is not invented beyond that brief obligation.

## Scope

**In scope** (`brief.md` frozen scope and Q1–Q11; ticket Wanted 1–9 and Done when as amended by Q3 Option A):

1. **ADR 0049** Accepted amending ADR-0011 decision 2 for one separate, labelled, read-only **Not in your library yet** For You row; main For You and Related stay library-only. Spec review includes the Accepted ADR; coding waits on merged Gate 1 and confirmed DEV-539 Done.
2. **Persistence**: additive nullable `title`, `release_year`, and `poster_path` on existing `movie_recommendations` / `MovieRecommendationRecord`; preserve composite key, cascade, and ID-only Crowd consumers; EF migration; existing enrichment and `--backfill-tmdb-recommendations` write/hydrate full first-page metadata for matched source movies (including legacy ID-only rows).
3. **Local discovery query**: set-based ranking over persisted titled links; disliked sources rejected; Liked weight (default 2) else Watched (default 1); Watchlist-alone ineligible; weights in `RecommendationOptions`; from eligible titled deduplicated contributors choose one canonical atomic metadata tuple per TMDb ID (highest-weight seed; tie: lowest library MovieId); then Option A library exclusion — TMDb ID against all library movies, then normalized title (punctuation and accents preserved) + equal known year applied to that canonical tuple for library movies without TMDb ID — before counting or returning; two distinct contributing seeds minimum; cap 20; sort by summed weight descending then TMDb ID ascending; reason “Recommended for {seed title} and {seed title}”.
4. **Public read contract**: `GET /api/recommendations/not-in-library` on `RecommendationEndpoints` under existing `/api` authorization; HTTP 200 array of `tmdbId`, `title`, nullable `releaseYear`, nullable `posterPath`, `reason`; `[]` when TMDb disabled, blank ApiKey, or no eligible candidates; existing For You contract unchanged; C# DTO → OpenAPI → generated TypeScript.
5. **Web**: independent section under the library grid; hide on loading/empty/error; distinct read-only cards; TMDb credit beside visible section; feature listed in README; README provider attribution left to DEV-559.
6. **Docs**: focused Positioning subsection on `specs/PRODUCT.md`; update root `CONTEXT.md` Recommendation entry; define **Outside-library recommendation** with `_Avoid_: wanted movie`.
7. **Evidence**: provider, real EF/Testcontainers, ranking/identity, API zero-HTTP and config-off, For You parity, Related library-only, web, local E2E, and home-library pre-cutoff / pre-cap counts plus owner verdicts on ranked top 10 (or all if fewer).

**Out of scope**:

- Requests, downloads, purchases, Wanted lists, alerts, Not interested.
- New provider lists or calls (discover/trending/similar); request-time provider HTTP; peer-history collaborative filtering; outside-movie overview embeddings.
- Showing outside movies on Related; changing main For You eligibility.
- Candidate IMDb lookup or blanket suppression for IMDb-only library records (rejected by Q3 Option A).
- New NuGet/npm dependency, project, top-level folder, or architectural layer.
- `Features:LocalPlay`, secrets, or `Process.Start`.

**Precedence**: Ticket Wanted, Done when, Out of scope and Open points are the approved scope and acceptance authority (`specs/PRODUCT.md` §3), as amended by the recorded Q3 Option A owner ruling for Wanted 3 / Done when 2. Brief Q1–Q12 and `CONCLUSIONS.md` govern plan boundaries. Keel recorded the Phase 2 plan challenge dispositions in [brief.md](brief.md#phase-2-step-6-adjudication).

**Dependencies**: DEV-539 Done (stores recommended TMDb ids including outside-library targets). ADR 0049 Accepted before coding. Spec PR Gate 1 merged before coding.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only)*

**Outside-library recommendation**
A movie outside the library surfaced in the dedicated read-only For You row from TMDb recommendations contributed by eligible library seeds.
_Avoid_: wanted movie

**Not in your library yet**
The labelled For You section heading for outside-library recommendations. Read-only discovery surface; not the main library For You grid.
_Avoid_: treating this row as a library recommendation; Play / library-details affordances

**Discovery seed**
A library movie that contributes once to a candidate’s score when Liked (weight default 2) or else Watched (weight default 1). Disliked is rejected first. Watchlist alone is ineligible.
_Avoid_: counting Watchlist-only movies; double-counting Liked+Watched

**Discovery card**
A read-only result with `tmdbId`, non-blank `title`, optional year and poster, and a two-seed reason. The web derives the TMDb movie URL from `tmdbId`.
_Avoid_: Play; library details; Not interested; IMDb link invent

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Persist titled recommendation metadata (Priority: P1)

Enrichment and the existing backfill store title, release year, and poster path with each recommended TMDb id on the DEV-539 link table so discovery can rank offline.

**Why this priority**: Without titled hydrated links, the local discovery query has nothing eligible to score.

**Independent Test**: Provider fixture proves first-page mapping of id/title/year/poster including optional fields and id dedupe; real PostgreSQL/EF proves additive migration, key/cascade/Crowd ID-only preservation, ingest and legacy hydration, and retain-prior-links on failed fetch.

**Acceptance Scenarios**:

1. **Given** a successful first-page recommendations fetch during enrichment, **When** links are written, **Then** each row carries recommended TMDb id plus title, and may carry nullable release year and poster path from that response.
2. **Given** legacy ID-only recommendation rows for a matched source movie that already has a TMDb id, **When** `--backfill-tmdb-recommendations` runs, **Then** those rows are refreshed with metadata rather than skipped solely because a TMDb id already exists.
3. **Given** a provider fetch or replacement write failure for a source, **When** enrichment or backfill finishes that attempt, **Then** previously stored links for that source remain.
4. **Given** the additive migration, **When** it applies on a database that already holds ID-only `movie_recommendations`, **Then** composite key, cascade, and existing Crowd binary consumers still work.

---

### User Story 2 - Rank outside-library candidates locally (Priority: P1) 🎯 MVP

A local set-based query scores titled out-of-library candidates from liked/watched library seeds with deterministic weights, two-seed quality bar, Option A identity exclusion, metadata selection, and two-seed reasons — with zero request-time provider HTTP.

**Why this priority**: This is the discovery product behaviour ADR 0049 authorizes.

**Independent Test**: Domain and property tests cover exclusion, blank-title exclusion, seed eligibility, weighting, dedupe, two-seed bar, canonical metadata choice before title/year fallback, conflicting-tuple isolation, punctuation/accent preservation, same-title/different-year, stable sort, cap 20, and reason formatting; ranking uses only local data.

**Acceptance Scenarios**:

1. **Given** eligible titled deduplicated contributors for a recommended TMDb id, **When** discovery ranks, **Then** it selects one canonical atomic title/year/poster tuple from the highest-weight contributing seed (lowest library MovieId on ties) before any library-exclusion or return decision.
2. **Given** a candidate whose TMDb id matches any library movie, **When** discovery ranks, **Then** that candidate is excluded (TMDb-ID check covers the whole library).
3. **Given** a library movie without a TMDb id whose normalized title and known release year equal the candidate’s canonical tuple, **When** discovery ranks, **Then** that candidate is excluded; a missing year cannot establish the fallback match; punctuation and accents are preserved under normalization.
4. **Given** conflicting metadata tuples from multiple seeds for one TMDb id, **When** discovery ranks, **Then** an ineligible or losing seed cannot determine fallback identity or the displayed metadata.
5. **Given** a candidate with only one eligible contributing seed, **When** discovery ranks, **Then** it is omitted; with two distinct eligible seeds it may appear.
6. **Given** a disliked library movie that recommends a candidate, **When** discovery ranks, **Then** that movie never contributes; a movie both liked and watched contributes once at Liked weight.
7. **Given** eligible candidates after the same Option A exclusion, **When** results are ordered, **Then** they sort by summed weight descending then TMDb ID ascending and return at most 20 cards.
8. **Given** an eligible card, **When** the reason is built, **Then** it uses the two highest-weight contributing seeds (MovieId ascending on ties) as “Recommended for {seed title} and {seed title}”, keeping both contributors even when display titles coincide.

---

### User Story 3 - Expose the discovery read route (Priority: P1)

`GET /api/recommendations/not-in-library` returns the ranked discovery cards under the existing recommendation authorization convention without changing the For You API contract.

**Why this priority**: Web composition depends on the published C# → OpenAPI → TypeScript contract.

**Independent Test**: API integration asserts JSON shape, authorization convention, empty conditions, zero outside HTTP via a fail-on-any-request handler, and unchanged For You movie ids/order; Related stays library-only.

**Acceptance Scenarios**:

1. **Given** eligible local candidates and TMDb configured, **When** the route is called, **Then** HTTP 200 returns an array of `tmdbId`, `title`, nullable `releaseYear`, nullable `posterPath`, and `reason`.
2. **Given** `Tmdb:Enabled` false, or Enabled with blank `ApiKey`, or no eligible candidates, **When** the route is called, **Then** HTTP 200 returns `[]`.
3. **Given** any discovery request, **When** the handler runs, **Then** no outside HTTP call is made.
4. **Given** this ticket’s changes, **When** For You is requested, **Then** the library movie ids and order match the pre-change contract.

---

### User Story 4 - Show the For You discovery section (Priority: P1)

For You shows an independent **Not in your library yet** section with distinct read-only cards and TMDb credit, without blocking the library grid.

**Why this priority**: Delivers the owner-visible row Wanted 8–9 require.

**Independent Test**: Web tests cover visible/empty/loading/error behaviour, independence of the library grid, nullable metadata, credit, read-only cards, and new-tab link; local seeded E2E opens For You and sees the section (SMOKE-001–003).

**Acceptance Scenarios**:

1. **Given** a successful nonempty discovery response, **When** For You renders, **Then** the section appears below the library grid with distinct cards showing title, reason, optional year, and poster or fallback.
2. **Given** discovery loading, empty `[]`, or fetch failure, **When** For You renders, **Then** the discovery section is hidden and the library grid remains usable.
3. **Given** a visible discovery card, **When** inspected, **Then** there is no Play, no library details link, and no Not interested control; the TMDb link uses a new tab and `rel="noopener noreferrer"`.
4. **Given** a visible section, **When** the owner views it, **Then** day-verified official TMDb attribution is displayed beside the section.

---

### User Story 5 - Align product docs and ship acceptance evidence (Priority: P2)

Product positioning, glossary, README feature list, and home-library acceptance receipts match ADR 0049 and Done when.

**Why this priority**: Closes Done when 10–11 and the brief acceptance evidence bar without inventing new product behaviour.

**Independent Test**: Docs land in the introducing delivery PR; after hydration, report unique titled out-of-library candidates with ≥1 eligible seed and those with ≥2 before the cap; attach ranked top 10 (or all if fewer) with actual owner good-pick/not verdicts; sparse results reported as observed.

**Acceptance Scenarios**:

1. **Given** delivery docs, **When** reviewed, **Then** `specs/PRODUCT.md` has a focused Positioning subsection, root `CONTEXT.md` Recommendation entry is updated, **Outside-library recommendation** is defined with `_Avoid_: wanted movie`, and README lists the feature while leaving README provider attribution to DEV-559.
2. **Given** a hydrated home library, **When** production ranking runs for acceptance, **Then** the delivery PR publishes the ≥1-seed and ≥2-seed denominators computed under the same Option A exclusion as the live query, plus the owner’s verdicts on the ranked top 10 or all if fewer.
3. **Given** sparse or empty home-library results, **When** evidence is attached, **Then** the short or empty run is reported accurately without relaxing the two-seed bar.

---

### Edge Cases

- TMDb disabled or blank ApiKey → API `[]`; web section hidden.
- No eligible candidates / only one seed → API `[]`; section hidden.
- Blank-title recommendation links → not counted toward discovery.
- Missing candidate or library year → title/year fallback cannot match; candidate may remain visible if otherwise eligible.
- Same display title on two distinct seed movies → reason still names both contributors.
- Duplicate source/candidate pairs → collapsed before grouping by recommended TMDb ID.
- Conflicting metadata tuples for one TMDb id → canonical highest-weight (lowest MovieId) tuple wins; ineligible or losing seeds cannot set fallback identity or displayed metadata.
- Watchlist-only library movie → never seeds.
- Disliked source → never seeds even if also watched/liked historically under reject-disliked-first.
- Provider fetch failure → prior links retained; discovery uses whatever titled rows remain.
- Related rail → remains library-only; no outside movies.
- Main For You → movie set and order unchanged.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST keep Accepted ADR 0049 in `docs/adr/` amending ADR-0011 decision 2 for one separate read-only outside-library For You row while main For You and Related stay library-only (`brief.md` Q1; ticket Wanted 1 / Done when 1).
- **FR-002**: System MUST add nullable `title`, `release_year`, and `poster_path` to `MovieRecommendationRecord` / `movie_recommendations` via a compatible additive EF migration, preserving composite key, cascade, and existing ID-only Crowd behaviour (`brief.md` Q2; `CONCLUSIONS.md` Q2).
- **FR-003**: Normal TMDb enrichment MUST persist first-page recommendation id, title, and optional year/poster; `--backfill-tmdb-recommendations` MUST hydrate matched source movies whose existing rows lack titles; each successful source refresh MUST publish a complete link set atomically, and a failed fetch or write MUST retain prior links (`brief.md` Q2, Q11).
- **FR-004**: Discovery MUST count only links with a title that is neither null, empty, nor whitespace-only and MUST make zero request-time provider HTTP calls (`brief.md` Q1, Q6; ADR 0049).
- **FR-005**: After the canonical metadata tuple is chosen (FR-008), library exclusion MUST (1) match candidate TMDb ID against all library movies and (2) for library movies without a TMDb ID apply Unicode NFC + trim + collapsed-whitespace + ordinal case-insensitive title comparison to that canonical tuple while explicitly preserving punctuation and accents, requiring both release years known and equal; a missing year MUST NOT establish that fallback; candidate IMDb lookup and IMDb-only blanket suppression are out of scope (`brief.md` Q3, Q6; Q3 Option A).
- **FR-006**: Seed eligibility MUST reject Disliked first; count each eligible Liked or Watched library movie once with Liked precedence; treat Watchlist alone as ineligible; bind integer Liked default 2 and Watched default 1 in `RecommendationOptions` and reject values unless Liked > Watched > 0 (ticket Wanted 5 and `brief.md` Q5 and adjudication).
- **FR-007**: Ranking MUST deduplicate source/candidate pairs, group by recommended TMDb ID, require two distinct contributing sources to count a candidate as passing the quality bar or return it as a card, sort by summed weight descending then TMDb ID ascending, and cap at 20 (`brief.md` Q5; `ASSUMPTIONS.md`).
- **FR-008**: From eligible titled deduplicated contributors, conflicting metadata for one candidate MUST select one canonical atomic title/year/poster tuple from the highest-weight contributing seed, breaking ties by lowest library MovieId, before title/year fallback exclusion or return; an ineligible or losing seed MUST NOT determine fallback identity or displayed metadata (`brief.md` Q6).
- **FR-009**: Card reason MUST name the two highest-weight contributing seeds ordered by weight descending and MovieId ascending as “Recommended for {seed title} and {seed title}”, as display-safe plain text, keeping both titles when they coincide (`brief.md` Q7; `ASSUMPTIONS.md`).
- **FR-010**: System MUST expose `GET /api/recommendations/not-in-library` on `RecommendationEndpoints` under the existing `/api` authorization convention, returning HTTP 200 with the card array or `[]` for disabled TMDb, blank ApiKey, or no eligible candidates, without changing `GET /api/recommendations/for-you` (`brief.md` Q4).
- **FR-011**: Contract chain MUST flow C# DTO in Core Recommendations → `web/src/api/openapi.json` → generated `web/src/api/types.ts`; web MUST consume generated types (`brief.md` Q4; `specs/PRODUCT.md` contract chain).
- **FR-012**: For You MUST add an independently fetched **Not in your library yet** section below the library grid, visible only after a successful nonempty response; hide during loading, empty, and error; keep the library grid usable (`brief.md` Q9; `ASSUMPTIONS.md`).
- **FR-013**: Discovery cards MUST be visually distinct and read-only (title, reason, optional year, poster or fallback, outbound TMDb link with new tab and `rel="noopener noreferrer"`); MUST omit Play, library details, and Not interested (`brief.md` Q9).
- **FR-014**: System MUST display day-verified official TMDb attribution beside the visible discovery section; README MUST list the feature and MUST leave README provider attribution to DEV-559 (`brief.md` Q9).
- **FR-015**: Delivery MUST add a focused Positioning subsection to `specs/PRODUCT.md`, update the root `CONTEXT.md` Recommendation entry, and define **Outside-library recommendation** with `_Avoid_: wanted movie` (`brief.md` Q8).
- **FR-016**: Automated tests MUST cover provider mapping, real EF migration/backfill, local ranking/identity, API zero-HTTP and config-off empties, For You parity, Related library-only, web behaviours, and local E2E visibility (`brief.md` Q10).
- **FR-017**: Delivery PR MUST attach hydrated home-library unique ≥1-seed and ≥2-seed pre-cap counts computed under the same Option A exclusion as the live query, plus ranked top 10 (or all if fewer) with actual owner good-pick/not verdicts; sparse results reported as observed; two-seed bar unchanged without owner ruling (`brief.md` Q10).
- **FR-018**: Delivery revision MUST satisfy Size L Phase 3 and rebased-head gates with honest exits, close all confirmed actionable in-scope findings including Low within two review rounds and two fix commits per round, and leave merge to the owner (`brief.md` Q12).

### Key Entities *(include if feature involves data)*

- **Recommendation link metadata**: Existing `(source_movie_id, recommended_tmdb_id)` row extended with nullable title, release year, and poster path.
- **Outside-library recommendation**: Ranked discovery card derived only from titled links whose candidate is outside the library under Option A identity rules.
- **Discovery seed contribution**: One weighted contribution from an eligible liked/watched library movie to a candidate TMDb id.
- **Discovery reason**: Display-safe two-seed explanation grounded in library seed titles.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Provider/integration coverage proves first-page recommendation mapping supplies candidate id, title, and optional year/poster without increasing the production fetch request count beyond the existing first-page path.
- **SC-002**: Additive migration applies on a database that already holds ID-only recommendation rows; Crowd ID-only consumers and cascade behaviour remain intact.
- **SC-003**: Backfill hydrates titled metadata for matched sources that previously lacked titles; prior links survive failed fetches and writes, and a concurrent reader sees the complete committed link set.
- **SC-004**: Domain tests prove TMDb-id exclusion across the library, title/year fallback only against the canonical tuple for library movies without TMDb id, punctuation/accent preservation, conflicting-tuple isolation, blank-title exclusion, disliked/watchlist rules, two-seed bar, Liked precedence, weighted sort, TMDb-id tie-break, cap 20, metadata selection before exclusion, and reason formatting.
- **SC-005**: API tests prove zero outside HTTP, both config-off empty cases, unauthenticated HTTP 401, JSON card shape, unchanged For You ids/order, and Related library-only.
- **SC-006**: Web and local E2E prove section visibility rules, read-only cards, new-tab TMDb link, visible credit, safe poster fallback, plain-text title and reason rendering, and independent library-grid usability.
- **SC-007**: Delivery PR publishes home-library ≥1-seed and ≥2-seed counts and owner verdicts on the ranked top 10 or all if fewer.
- **SC-008**: `specs/PRODUCT.md`, root `CONTEXT.md`, and README feature list match ADR 0049’s discovery exception; glossary includes **Outside-library recommendation**.
- **SC-009**: Size L verification records actual gate statuses with honest exit-2 dispositions; failure or `Could not run` blocks; review findings have written dispositions per the closing bar.

## Assumptions

- Taste/ordering assumptions logged in `ASSUMPTIONS.md` (TMDb ID ascending tie-break; reason seed ordering; glossary term; hide on loading/error) are Patron-approved `[assumed]` rulings, not Quill invents.
- DEV-539 Done and ADR 0049 Accepted are prerequisites confirmed at Phase B pickup before coding (`brief.md` Q11).
- Exact EF column facets and implementation/test seams are verified by pickup recon; this draft does not invent unstated schema facets beyond nullable title, release year, and poster path.
- Option A can miss library records without a TMDb ID when either year is unknown or titles differ after normalization. The delivery receipt documents this limitation.
- Day-verified TMDb attribution text is confirmed against official terms on the shipping day (`brief.md` Q9); this draft does not invent the exact credit wording beyond “official TMDb attribution.”
