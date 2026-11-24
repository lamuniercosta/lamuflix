# DEV-561 — Patron Rulings (CONCLUSIONS)

## Q1 — ADR 0049 boundary amending ADR-0011 decision 2
- **Verdict**: Approved — author ADR 0049 (`docs/adr/0049-*.md`, status `Accepted`) as a narrow amendment to `docs/adr/ADR-0011.md` decision 2 permitting one separate, labelled, read-only `Not in your library yet` discovery row on `For You`, while keeping main `For You` and `Related` recommendations strictly library-only with unchanged eligibility.
- **Basis**:
  - **Ticket scope & boundary** (`DEV-561` Goal; Wanted 1, 6, 7, 8; Done when 1, 5, 7; Out of scope 1, 3, 4; Needs 2): mandates an `Accepted` ADR amending `ADR-0011` decision 2 before code merges so one separate read-only `For You` row shows out-of-library movies with library-seed reasons and no playback/request/purchase controls, while library `For You` and `Related` stay library-only.
  - **Preserved ADR-0011 & ADR 0048 invariants** (`docs/adr/ADR-0011.md:13-15,19`; `docs/adr/0048-tmdb-crowd-similarity.md:11,17-19`): candidates come exclusively from locally persisted first-page TMDb recommendation rows already fetched for library movies (no discover/trending/similar lists, zero request-time provider HTTP calls, no viewing/taste data leaving the machine, explainable reasons required, and peer-history collaborative filtering still rejected).
  - **ADR numbering** (`DEV-561` Wanted 1; `recon-DEV-561:34-36`): `0048` (`docs/adr/0048-tmdb-crowd-similarity.md`) is the highest ADR on `origin/main` and open PR branches, so `0049` is the next free ADR number.

## Q2 — Charter §2.3 persistence change on DEV-539 recommendation links
- **Question**: What persistence change should we authorize on the `DEV-539` links (`MovieRecommendationRecord` / `movie_recommendations`) for storing recommendation `title`, `release_year`, and `poster_path` through the existing recommendation ingest/backfill without a second store or provider route?
- **Verdict**: Approved (Charter §2.3 item 3) — extend `MovieRecommendationRecord` and `movie_recommendations` in place via a compatible additive EF migration adding nullable `title`, `release_year`, and `poster_path` columns, reusing the existing TMDb recommendation ingest and backfill flow with no second store or new provider endpoint.
- **Rationale & Implications**:
  - **Ticket & current-schema rationale** (`recon-DEV-561:16-18,33,63,78-79`; `MovieRecommendationRecord.cs:5-12`; `20261122171551_AddCrowdRecommendations.cs:20-35`): `DEV-539` currently stores only `(source_movie_id, recommended_tmdb_id)`, and `DEV-561` Wanted 3 plus `Where it lands` explicitly authorize persisting `title`, `year`, and `poster_path` from the already-fetched first-page `movie/{id}/recommendations` response (`TmdbMetadataProvider.Signals.cs:38-89`) on that link table.
  - **Compatibility & discovery eligibility** (`docs/adr/0048-tmdb-crowd-similarity.md:11,17`): keeping all three added columns nullable preserves the `(source_movie_id, recommended_tmdb_id)` composite key, cascade behavior, and existing ID-only rows for `DEV-539` binary Crowd links; discovery excludes any row lacking a non-blank `title`, while `release_year` and `poster_path` remain optional (`null` allowed), with Phase B pickup confirming exact column facets before migration generation.
  - **Ingest & backfill refresh implication** (`TmdbMetadataProvider.Signals.cs:50-89`; `EfMovieRepository.cs:152-169`; `BackfillTmdbRecommendationsCommandHandler.cs:56-60,117-126`; `recon-DEV-561:37,51,56`): normal TMDb enrichment writes full recommendation metadata, and reusing `--backfill-tmdb-recommendations` must also refresh matched movies whose existing `movie_recommendations` rows still lack `title` (rather than skipping solely on `TmdbId is not null`) so an already-backfilled home library can hydrate legacy ID-only rows offline of the read-only HTTP route.

## Q3 — Candidate IMDb identity resolution vs zero-extra-TMDb-call constraint
- **Question**: Can existing TMDb ingest/backfill resolve and persist a nullable candidate IMDb ID on `MovieRecommendationRecord` (suppressing unresolved candidates when an IMDb-only library movie makes exclusion uncertain), or does that require a Charter §2.3(a) owner escalation?
- **Verdict**: `blocked: structural — TMDb's movie/{id}/recommendations response carries id, title, release_date, and poster_path but no imdb_id, so DEV-561 cannot satisfy both "It needs no extra TMDb call" (Goal; Problem bullet 4; Out of scope bullet 3) and IMDb-ID candidate exclusion for library movies without a TMDb ID (Wanted 3; Done when 2) under zero read-time HTTP calls (Done when 5). Should DEV-561: (Option A — Recommended) keep zero extra TMDb calls and exclude library movies by TMDb ID plus normalized (Title, ReleaseYear) fallback when a library movie lacks a TMDb ID (amending Wanted 3 / Done when 2), or (Option B) allow deduplicated TMDb external_ids lookups during ingest/backfill to persist nullable recommended_imdb_id on movie_recommendations for exact IMDb-ID exclusion without suppressing candidates whose IMDb ID is unavailable (relaxing "It needs no extra TMDb call")?`
- **Rationale & Implications**:
  - **Irreducible ticket contradiction (`Charter §2.3(a)`)**: `DEV-561` Goal and Problem bullet 4 state `"It needs no extra TMDb call"` because `movie/{id}/recommendations` already supplies `title`, `release_date`, and `poster_path` (`recon-DEV-561:16-17,33,63`), whereas `recon-DEV-561` identity addendum lines 4–8 confirms recommendation items carry no `imdb_id` (`TmdbMetadataProvider.Mapping.cs:373-386`) and no local TMDb-to-IMDb candidate map exists to satisfy `Wanted 3` and `Done when 2 & 5` (`recon-DEV-561:33,48,51`).
  - **Why candidate-identity calls and candidate suppression alter ticket scope**: adding per-candidate TMDb lookups during ingest/backfill introduces up to 20 extra TMDb requests per source movie (thousands across the home library) contrary to `"It needs no extra TMDb call"` and `"Only the lists DEV-539 already fetches"` (`recon-DEV-561:63`), while suppressing unresolved candidates whenever any OMDb-enriched library movie has `ImdbId != null` and `TmdbId == null` (`Movie.cs:59-63`) could blank or severely truncate the discovery row (`recon-DEV-561:35,56`).
  - **Gate 1 & spec-PR implication**: carry this Option A vs Option B owner checkbox on the `DEV-561` spec PR and keep Gate 1 closed until the owner resolves it; neither extra TMDb candidate-identity HTTP calls nor row suppression on IMDb-only library records is authorized without owner selection.

---

## Q3 owner ruling

- **Owner answer** — Approved Patron Option A. PR #492 records the checked item "Owner ruling on DEV-561 Q3 Patron Option A." The Conductor confirmed the ruling, and the DEV-561 task note records Rigger’s read-back at `68b19f8a`.
- **Rationale** — The existing first-page TMDb recommendation response supplies the candidate TMDb ID and title/release-date data. It carries no candidate IMDb ID, and recon found no local mapping to supply one. Option A preserves the ticket’s zero-extra-TMDb-call requirement. [PR #492](https://github.com/lamuniercosta/lamuflix/pull/492) · `recon-DEV-561:4–8`.
- **Scope amendment** — Wanted 3 and Done when 2 now require library exclusion by TMDb ID, with a normalized `(Title, ReleaseYear)` fallback for library movies lacking a TMDb ID. The discovery path uses the existing recommendation fetch and makes zero request-time provider calls. Candidate exclusion uses those two local identity checks even when the library contains IMDb-only movies.
- **Gate 1** — This approval resolves the Q3 owner checkbox. Gate 1 remains pending the spec PR merge.

## Q4 — Public read contract (`GET /api/recommendations/not-in-library`)
- **Verdict**: Approved (Charter §2.3 item 4) — expose `GET /api/recommendations/not-in-library` as a separate read-only local query on `RecommendationEndpoints` under the existing `/api` route-group authorization convention, returning HTTP `200` with an array of cards (`tmdbId`, `title`, nullable `releaseYear`, nullable `posterPath`, `reason`), returning `[]` when `Tmdb:Enabled` is `false`, `Tmdb:ApiKey` is blank, or no candidates qualify, flowing C# DTO → OpenAPI → generated TypeScript while the frontend derives the TMDb movie URL from `tmdbId`.
- **Basis**:
  - **Route, isolation & local-only execution** (`recon-DEV-561:28,41,43,57,71-72`; `RecommendationEndpoints.cs:28-45`; `docs/adr/0049-for-you-outside-library-discovery.md:7-11`): adopts the ticket's default route (`GET /api/recommendations/not-in-library`) on the existing `/api` `RecommendationEndpoints` group with zero request-time provider HTTP calls while keeping `GET /api/recommendations/for-you` and `ForYouRecommendationsPageDto` unchanged.
  - **Card DTO shape & empty-list conditions** (`recon-DEV-561:24,27-28,30-33,42,61`; `specs/DEV-561/CONCLUSIONS.md` Q2 & Q3): exposes only card fields persisted on `MovieRecommendationRecord` (`tmdbId`, non-blank `title`, nullable `releaseYear`, nullable `posterPath`) plus library-seed `reason`, lets the web client derive the outbound TMDb movie page URL from `tmdbId` (ticket default; no candidate IMDb ID per Q3 Option A), and returns HTTP `200` `[]` when `Tmdb:Enabled` is `false`, `ApiKey` is blank, or no candidates qualify.
  - **Contract chain** (`specs/PRODUCT.md:12,46`; `recon-DEV-561:73-74`; `web/src/features/for-you/forYouApi.ts:1-7`): the C# response DTO regenerates `web/src/api/openapi.json` and `web/src/api/types.ts` so `web/src/features/for-you/` consumes generated schema types with no hand-rolled duplicates.


**Q4 exchange**

- **Keel** — Approve the public read contract? I recommend GET /api/recommendations/not-in-library, a separate local read query using the existing recommendation routes authorization convention. Return HTTP 200 with an array of cards containing tmdbId, title, nullable releaseYear, nullable posterPath, and reason. Return [] for disabled TMDb, blank API key, or no eligible candidates. Generate OpenAPI and TypeScript from the C# DTO. The frontend can derive the TMDb movie URL from tmdbId. This keeps the existing For You contract independent and exposes only card data. Basis: recon-DEV-561 lines 24-44, 56-61 and 69-74, and ADR 0049.
- **Patron** — DEV-561 Q4 approved at 48345282. The full ruling is above.

---

## Q5 — Seed eligibility, score, and ordering

- **Keel** — Approve the deterministic seed and ranking algorithm for the new row? I recommend one weight per distinct library movie: exclude Disliked first, then use Liked 2, else Watched 1, with Watchlist alone ineligible. Bind the two weights in RecommendationOptions, use the two-seed minimum and 20-card maximum from Wanted 5, collapse duplicate source/candidate pairs, group by recommended TMDb ID, then sort by total weight descending and TMDb ID ascending. A movie both liked and watched counts once at Liked weight. This preserves the ticket quality bar and keeps results reproducible. Basis: recon-DEV-561 lines 25–27, 39–40, 58–60, and 80–81.
- **Patron** — Approved. Reject Disliked first. Count each eligible liked or watched library movie once, with Liked taking precedence. Bind defaults Liked 2 and Watched 1 in RecommendationOptions. Deduplicate source/candidate pairs before grouping by candidate TMDb ID. Require two distinct seeds before ranking and capping at 20. Order by summed weight descending, then TMDb ID ascending. Record the tie-break as [assumed].
- **Basis** — Wanted 4–5, Done when 3–4, and Open points establish eligibility, quality bar, weights and limit. Existing seed precedence and stable tie handling provide the precedent. See recon-DEV-561:25–26,39–40,58–59,80–83 and Q2.

---

## Q6 — Candidate identity and metadata

- **Keel** — Approve the exact candidate identity and metadata selection rule under owner-approved Option A? Exclude any candidate whose TMDb ID appears in the library. For library movies without a TMDb ID, compare both known release years for equality and titles after Unicode NFC, trimming, collapsing internal whitespace, and ordinal case-insensitive comparison. Preserve punctuation and accents. A missing year cannot establish the fallback match, so an otherwise eligible candidate may remain with a null year. Count only links with a nonblank title under Q2. For conflicting metadata on eligible links for the same candidate ID, take the entire title/year/poster tuple from the highest-weight contributing seed, then smallest library movie ID. This gives the same local identity check and displayed card on every read, with a documented limitation for incomplete legacy metadata. Basis: Q2, owner Q3, recon-DEV-561:4–8,24,38,80–84.
- **Patron** — Approved. Match TMDb ID across the library. Apply the normalized title and equal known-year match only to library movies without a TMDb ID. A missing year cannot establish a match. Count only links with a nonblank title. Resolve metadata conflicts by choosing one atomic tuple from the highest-weight contributor, using lowest source MovieId for a tie.
- **Basis** — Q2 allows null year and preserves legacy ID-only Crowd links. Owner Q3 requires local two-tier exclusion. Requiring both years avoids excluding distinct same-title films on weak evidence. Taking the whole tuple avoids mixing provider snapshots, and strongest-seed ordering follows existing precedent. See recon-DEV-561:4–8,24,38,81,83–84.

---

## Q7 — Discovery card reason

- **Keel** — Approve the card reason contract? I recommend the two highest-weight contributing library seeds, breaking seed ties by ascending library MovieId, and the ticket example “Recommended for {seed title} and {seed title}.” Reuse the concise, display-safe reason style with a discovery-specific formatter if necessary. Recon found no RecommendationReasonBuilder.cs, and SimilarityScorer.BuildEvidence represents similarities instead of direct links. Keep the two actual contributing seed movies when their display titles coincide. This bounds copy while still naming two contributors. Basis: Wanted 6, ADR 0049, Q5, recon-DEV-561:26–27,71,80–85.
- **Patron** — Approved. Use a focused discovery formatter under Features/Recommendations, reusing existing display-safe title sanitization. Select two contributors by seed weight descending and library MovieId ascending. Preserve both titles if the selected distinct seeds share a display title. Log the two-title cap and tie-break as [assumed].
- **Basis** — Wanted 5–6, ADR 0049 and Q5 require two distinct contributing seeds and a reason grounded in their library titles. The ticket's example and existing reason handling establish copy and sanitization style. See recon-DEV-561:25–27,71,81–82,85.

---

## Q8 — Product positioning and discovery terminology

- **Keel, initial question** — Approve Outside-library recommendation as the CONTEXT.md term? Define it as a movie outside the library surfaced in the dedicated For You row from TMDb recommendations contributed by eligible library seeds, with an _Avoid_: wanted movie line. Keep the row heading Not in your library yet. Update the Recommendation entry and PRODUCT.md Positioning to describe the separate read-only row and preserve library-only For You and Related.
- **Patron, initial answer** — Needs recon on the PRODUCT.md path, the cited Positioning text, and whether the ticket's prerequisites introduce the missing document or section.
- **Keel, resumed question** — Wisp found only specs/PRODUCT.md, which has no Positioning section, and neither prerequisite introduces one. Approve treating the ticket's bare path and line number as stale references, adding a focused Positioning subsection to that blueprint in the delivery scope, and updating the root CONTEXT.md glossary as proposed? Should Rigger record the corrected path and section reference?
- **Patron** — Approved under Charter §2.3 items 2 and 6. Add the focused Positioning subsection to the existing specs/PRODUCT.md. Update the root CONTEXT.md Recommendation entry and add Outside-library recommendation with _Avoid_: wanted movie. Keep the ticket's row heading. Log the glossary naming as [assumed]. Patron dispatched Rigger to record the corrected file and section in a DEV-561 comment.
- **Basis** — Wanted 2, Done when 11, ADR 0049 and the restored PRODUCT provenance in recon-DEV-561:5–21. The existing blueprint is the document that fulfills the ticket's positioning requirement.

---

## Q9 — Web behavior and attribution

- **Keel** — Approve one independent section beneath the For You library grid, visible only after a successful nonempty discovery response? Keep the library experience usable during discovery loading, empty results, and errors. Use visually distinct read-only cards with title, reason, optional year and poster treatment, and an accessible, safe new-tab TMDb link. Omit Play, library details, and Not interested. Place the required, day-verified TMDb credit beside the section and list the feature in the README, leaving README provider credit with DEV-559.
- **Patron** — Approved. Hide the section while discovery loads, on an empty response, or on discovery fetch failure. Keep the library grid usable. Use title, reason, optional year, image or poster fallback, and a derived TMDb movie link with target new tab and rel noopener noreferrer. Omit library actions and Not interested. Verify the official TMDb terms on the day and display the required credit beside the visible section. Add the feature to README.md while leaving README provider attribution with DEV-559. Log loading/error suppression as [assumed].
- **Basis** — Wanted 6, 8–9, Done when 6–9 and 11, Open points, Where it lands, ADR 0049 and Q4. The ticket specifies a separate discovery surface, empty-state hiding and a distinct read-only card. See the restored ticket text in recon-DEV-561:27–34,42–47,60–61,73–75 before the PRODUCT addendum.

---

## Q10 — Acceptance evidence and home-library counts

- **Keel** — Approve deterministic provider, real EF, ranking and identity, HTTP, parity, web and local E2E tests, plus a separate home-library run? Hydrate legacy rows first. Count unique titled out-of-library candidates with at least one eligible seed before the cutoff, then those meeting two seeds before the card cap. Publish those counts and the ranked top 10 or all if fewer, with the owner's actual good-pick/not verdict for each. Keep raw viewing and seed data local and retain the cutoff if the run is sparse.
- **Patron** — Approved. Use real PostgreSQL/EF with Testcontainers for migration and backfill, a fail-on-any-request HTTP handler, both unconfigured TMDb cases, unchanged exact For You order, Related library-only parity, web coverage, and local E2E visibility. Use the two agreed denominators and obtain actual human-owner verdicts after hydration. A short or empty run is valid evidence when accurately reported. Any change to the two-seed bar requires an owner ruling.
- **Basis** — Wanted 3–9, Done when 2–10, Open points, ADR 0049, Q2–Q9 and the AGENTS.md real-storage rule. The ticket requires both automated coverage and a separate home-library receipt. See the previously restored ticket text in recon-DEV-561:24–46.

---

## Q11 — Implementation sequence and data path

- **Keel** — Approve the ordered implementation and boundary? After Phase B prerequisite and seam verification, add the compatible schema, provider mapping, enrichment and atomic per-source backfill that retains prior links on failed fetches. Implement the separate local query with set-based projections. Preserve existing For You configuration, bind discovery weights in RecommendationOptions and name the two-seed and 20-card rules. Generate C# DTO, OpenAPI and TypeScript before web, then finish scoped docs, local E2E, home acceptance and gates. Use the existing dependencies and layers and keep LocalPlay, secrets and unrelated schema out.
- **Patron** — Approved under Charter §2.3 items 1–6. Start Phase B by confirming DEV-539 Done, ADR 0049 Accepted and the spec merge, with fresh recon for column facets and implementation/test seams. Use additive EF storage and the existing first-page fetch; hydrate older links through the backfill and preserve stored links when a fetch fails. Implement set-based local discovery and preserve For You and Related behavior. Finish generated types before wiring web and retain the scoped docs, tests and receipt sequence.
- **Basis** — Wanted 1, 3–7, Open points, Where it lands, the verbatim Needs section in the PRODUCT addendum at recon-DEV-561:13–21, ADR 0049, task-pipeline Phase 3, and Q1–Q10. The ordering ensures the local query can depend on hydrated links and web uses the published API schema.

---

## Q12 — Review closing bar, scope, and round cap

- **Keel** — Approve a closing bar of zero open confirmed actionable in-scope findings at any severity across Risk, Standards, Spec and ship review? Require the Phase 3 and rebased-head gates and the delivery PR's acceptance evidence including the owner's home-library verdicts. Cap the loop at two rounds and two fix commits per round. Route any third-round findings or remaining blockers to the owner on the PR without clearance. Freeze Q1–Q11 and leave unrelated work outside this round.
- **Patron** — Approved with the Charter §2.3 follow-up filter. Close all confirmed actionable in-scope findings, including Low. Meet the required gates and provide the acceptance receipts before awaiting-merge. Apply both caps and escalate remaining blockers on the PR. Outside-scope Critical or High observations, broken behavior, or owner-requested follow-ups can go through Rigger after checking existing open tickets. Record other outside-scope observations as “noted, no ticket.”
- **Basis** — Task-pipeline size-L review, Phase 3 gate rules, Phase 5 merge bar and caps, Charter §2.3, ADR 0049, DEV-561 Done when and Out of scope, and Q10's acceptance evidence.
