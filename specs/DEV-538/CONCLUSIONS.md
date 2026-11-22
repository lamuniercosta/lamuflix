# DEV-538 grill conclusions

---

## Grill opener and ratified ticket defaults

**Keel**

> [from Keel] /grill-with-docs DEV-538

**Patron**

> [from Patron] DEV-538 grill ready: ticket defaults ratified for client grouping, caps (4 seed rows, 6 cards/row in one file), watchlist row (1c), and hidden-when-empty series row (1a) (specs/DEV-538/ticket-text.txt:52,54,79-83); concur with needs recon on DEV-541/DEV-542 contract fields for collectionId, watched-collection signal, and empty-state seed presence before ruling on DTO/contract changes.

- Group the ranked For You results on the client. The ticket keeps the existing single request and sets client grouping as the default (`ticket-text.txt:54,79`).
- Define the four-seed-row and six-card-per-row limits together in one file, as required by the Done when criteria (`ticket-text.txt:52,80-81`).
- Use “Close to your watchlist” for watchlist-seeded results under rule 1c. The individually titled seed rows are for liked and watched seeds (`ticket-text.txt:29-30,82`).
- Include the series row and hide it when it has no movies. This preserves the series behavior for libraries with eligible entries (`ticket-text.txt:28,83`).
- The DTO and empty-state decisions await the authoritative contract evidence for candidate collection ID, watched membership in that collection, and seed presence in an empty ranked response (`recon-DEV-538:17-18,26-27,39`).

---

## Q1. Series eligibility in the For You response

**Keel**

> [from Keel] DEV-538 grill Q1. What disposition should the spec record for Wanted rule 1a? Recon lines 51-53 confirm the DEV-541/542 response lacks candidate collection identity and the watched-collection signal. Wanted line 28 requires the series row, while Done when line 54 restricts API changes to DEV-541/542. I recommend retaining rule 1a and raising one owner checkbox to approve an additive For You response amendment on the same GET, with the series fact expressed as a nullable started-collection identifier per item. A non-null value would identify an eligible unwatched candidate in a collection with a watched member and support one-per-collection grouping. The cost is an explicit change to the ticket API boundary, with Gate 1 closed pending owner approval. Please rule on the disposition and provide its cited basis and proposed checkbox wording. Reply to Keel.

**Patron**

> [from Patron] DEV-538 Q1 ruling: blocked: structural — retain rule 1a in spec with additive nullable startedCollectionId (int?) on RecommendedMovieDto and carry owner checkbox on spec PR (amending Done when line 54); full ruling and checkbox wording at C:\Users\lamun\AppData\Local\Temp\patron-DEV-538-q1.md

blocked: structural — Retain Wanted rule 1a (Next in a series you started) by adding a nullable startedCollectionId (int?) per item on RecommendedMovieDto (non-null only when the candidate belongs to a collection that has at least one watched movie in the library), or drop rule 1a from DEV-538 to keep Done when line 54 unchanged?

- **Ticket contradiction requiring owner checkbox (`specs/DEV-538/ticket-text.txt:19,28,54,74,83,88`; `recon-DEV-538:51-53`; `specs/PRODUCT.md:36-39`)**: `Wanted` line 28 and `Open points` line 83 require building row 1a (`Next in a series you started`, hidden while empty) and `Where it lands` line 74 anticipates editing `RecommendedMovieDto.cs` if the spec adds collection identity, but `recon-DEV-538:51-53` confirms neither `DEV-541` nor `DEV-542` exposes collection identity or watched-collection membership, making rule 1a impossible without amending `Done when` line 54 (`No change to the API except what DEV-542 and DEV-541 add`).
- **Spec disposition (`specs/PRODUCT.md:34-39,46`; `specs/DEV-538/ticket-text.txt:28,74,83`)**: Retain rule 1a in the spec using one additive nullable `startedCollectionId` (`int?`) field per item on `RecommendedMovieDto` on the existing `GET /api/recommendations/for-you` response (non-null only when the candidate belongs to a collection with at least one watched movie), supporting client-side series filtering, earliest-release-year ordering, and one-per-collection deduplication in a single request.
- **Proposed owner checkbox wording (`specs/PRODUCT.md:36-39`)**: `- [ ] Approve adding nullable startedCollectionId (int?) to RecommendedMovieDto on GET /api/recommendations/for-you to satisfy Wanted rule 1a (Next in a series you started, ticket lines 28, 74, 83), amending Done when line 54 (or instruct dropping rule 1a instead).`


---

## Q2. Seed presence and the consolidated owner checkbox

**Keel**

> [from Keel] DEV-538 grill Q2. What disposition should the spec record for Wanted rule 6? Recon lines 51 and 54 show that no seeds and seeds with no strong match produce the same empty response. I recommend retaining both empty states and proposing a required boolean hasSeeds on the existing For You page envelope, true when the request has any eligible liked, watched, or watchlist seed before candidate filtering, caps, paging, or swap exclusions. This keeps one request and makes empty-state selection deterministic. Please rule on the signal and whether the owner should approve both Q1 and Q2 additions under one amendment to Done when line 54, with explicit owner checkbox wording. Reply to Keel.

**Patron**

> [from Patron] DEV-538 Q2 ruling: blocked: structural — retain both rule 6 empty states with required boolean hasSeeds on the For You page envelope, and combine Q1 (startedCollectionId) and Q2 (hasSeeds) into one consolidated owner checkbox amending Done when line 54; full ruling and checkbox wording at C:\Users\lamun\AppData\Local\Temp\patron-DEV-538-q2.md

blocked: structural — Retain both Wanted rule 6 empty states (6a no seeds vs. 6b no strong matches) by adding a required boolean hasSeeds to the GET /api/recommendations/for-you page envelope, and consolidate Q1 (startedCollectionId) and Q2 (hasSeeds) into a single owner checkbox amending Done when line 54.

- **Empty-state contract gap (`specs/DEV-538/ticket-text.txt:20,36-39,49,54`; `recon-DEV-538:51,54`; `specs/PRODUCT.md:36-39`)**: `Wanted` rule 6 and `Done when` line 49 require two distinct empty states (6a: no liked, watched, or watchlist seeds; 6b: seeds exist, but no result clears the DEV-543 strong-match bar) within a single For You request (`Done when` line 54), but `recon-DEV-538:51,54` confirms `DEV-541`/`DEV-542` return `{ items: [], hasMore: false }` for both cases with no seed-presence signal when `items` is empty.
- **Spec disposition (`specs/PRODUCT.md:34-39,46`; `specs/DEV-538/ticket-text.txt:36-39,54`)**: Retain both empty states in the spec and add a required boolean `hasSeeds` on the existing `GET /api/recommendations/for-you` response envelope (`{ items, hasMore, hasSeeds }`), set to `true` whenever at least one eligible liked, watched, or watchlist seed exists before candidate filtering, strong-match filtering, caps, paging, or swap exclusions, and `false` otherwise.
- **Consolidated owner checkbox wording (Q1 + Q2 amendment to `Done when` line 54, `specs/PRODUCT.md:36-39`)**: `- [ ] Approve amending Done when line 54 to permit two additive fields on GET /api/recommendations/for-you while keeping a single request: (1) nullable startedCollectionId (int?) on each RecommendedMovieDto item (non-null when the candidate belongs to a collection with a watched movie) for Wanted rule 1a, and (2) required boolean hasSeeds on the page envelope (true when any liked, watched, or watchlist seed exists before candidate/strong-match filtering, caps, paging, or exclusions) for Wanted rule 6a vs. 6b.`


---

## Q3. Deterministic row assignment

**Keel**

> [from Keel] DEV-538 grill Q3. What is the deterministic row assignment policy for Wanted rules 1-4? I recommend assigning at most one candidate per started collection to the series row first, choosing earliest known release year, then API rank for ties or unknown years. Sort series cards by that same order. Route remaining candidates by their single best seed: watchlist to Close to your watchlist; liked/watched by seed movie ID plus its reported signal. Select seed groups by descending size, ties by first API rank. Preserve API order within other groups and within More for you after deduplication. Any singleton row, unselected seed group, or capped themed-row overflow goes to More for you without rerouting a folded series card. This resolves precedence and retains every eligible movie while respecting the four/six limits. Please rule on the policy against ticket lines 25-35 and 79-83. Reply to Keel.

**Patron**

> [from Patron] DEV-538 Q3 ruling: Ratified deterministic row assignment, tie-break, singleton fold, and 4-row/6-card overflow policy for Wanted rules 1-4 (log tie-break/overflow details as [assumed] in ASSUMPTIONS.md); full ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-538-q3.md

Ratified: Adopt Keel's deterministic client-side row assignment, tie-break, folding, and overflow policy for Wanted rules 1–4 (`specs/DEV-538/ticket-text.txt:25-35,79-83`; log tie-break/overflow sub-rules tagged `[assumed]` in `specs/DEV-538/ASSUMPTIONS.md` per `specs/PRODUCT.md:28-30`).

- **Precedence and series row (rule 1a, `specs/DEV-538/ticket-text.txt:27-28,32-33,83`)**: Evaluate rule 1a first across candidates with non-null `startedCollectionId`, picking at most one candidate per collection by earliest known release year (unknown years sort after known years, ties broken by API rank) and ordering the row by that same comparator; if the resulting series row has `< 2` movies, fold it directly into `More for you` without rerouting through seed/watchlist rows.
- **Seed and watchlist rows (rules 1b–1c, `specs/DEV-538/ticket-text.txt:29-30,32-33,80-82`)**: Route remaining candidates by their single best seed (`watchlist` -> `Close to your watchlist`; `liked`/`watched` -> grouped by seed movie ID + reported signal for `Because you liked {title}` / `Because you watched {title}`), rank eligible `>= 2`-movie seed groups by descending candidate count (ties by earliest first-appearance API rank) up to 4 seed rows, and preserve API rank order within each themed row up to the 6-card cap.
- **Folding and More for you (rules 1d, 2, 3, 5, `specs/DEV-538/ticket-text.txt:31-33,35,81`)**: Route every folded singleton row (`< 2` movies from 1a, 1b, or 1c), unselected seed group beyond the 4-row cap, and per-row card overflow beyond the 6-card cap in themed rows (1a–1c) into `More for you`, deduplicated by movie ID in original API rank order (`More for you` itself is uncapped by the 6-card themed-row cap because rule 5 paging/swap applies to `More for you` only).


---

## Q4. Paging, swap and empty initial results

**Keel**

> [from Keel] DEV-538 grill Q4. How should the grouped view apply Wanted rule 5? I recommend deriving themed rows only from the initial ranked response and keeping them stable through DEV-517 Load more and Swap. Use the established DEV-517 request semantics for those controls on More for you, deduplicate against every themed and More card already shown, and keep the existing page state if a follow-up returns no items. Show the rule 6 empty states only after a successful empty initial response. This keeps the cap on themed rows, allows More for you to grow, and prevents a More action from changing another row. Please rule against ticket lines 31-39 and 54 and recon lines 23 and 45. Reply to Keel.

**Patron**

> [from Patron] DEV-538 Q4 ruling: Ratified freezing themed rows (1a-1c) from the initial response, scoping DEV-517 Load more and Swap with cross-row deduplication to More for you only, and showing rule 6 empty states only on an empty initial response; full ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-538-q4.md

Ratified: Derive and freeze themed rows (1a–1c) from the initial For You response, scope DEV-517 Load more and Swap exclusively to More for you with cross-row deduplication, and render rule 6 empty states only on an empty initial response (`specs/DEV-538/ticket-text.txt:31-39,48,54`; `recon-DEV-538:23,45`; log follow-up stability details tagged `[assumed]` in `specs/DEV-538/ASSUMPTIONS.md` per `specs/PRODUCT.md:28-30`).

- **Frozen themed rows on initial load (`specs/DEV-538/ticket-text.txt:27-35,48,54`; `recon-DEV-538:23,45`)**: Compute themed rows (1a series, 1b seed rows, 1c watchlist) and the initial `More for you` bucket (1d) once from the single initial `GET /api/recommendations/for-you` response and keep themed rows immutable across subsequent `More for you` paging or swap actions so seed/series/watchlist rows never page or reshuffle.
- **More for you Load more and Swap (`specs/DEV-538/ticket-text.txt:31-32,35,48`; `recon-DEV-538:23,45`)**: Render `DEV-517` Load more and Swap controls on `More for you` only, using established `DEV-517` request semantics while excluding/deduplicating against every movie ID already displayed across themed rows and `More for you` (preserving rule 2 uniqueness) and preserving the current rendered state if a follow-up request yields no additional items.
- **Empty-state trigger (`specs/DEV-538/ticket-text.txt:30-32,36-39,49`)**: Render the rule 6 empty states (6a when `hasSeeds === false`, 6b when `hasSeeds === true`) only when the initial For You response succeeds with zero items.


---

## Q5. More for you with controls and zero cards

**Keel**

> [from Keel] DEV-538 grill Q5. How should More for you behave when every initial item went into a themed row but hasMore is true? I recommend a labelled More for you section with its h2 and Load more in that case. Mount the carousel only when the section gains cards, and offer Swap only when its existing DEV-517 prerequisites hold. Hide the section when it has neither cards nor an available control. This keeps rule 5 reachable without adding a request on initial load and preserves the empty-row check for rows with no content or action. Please rule against Wanted rules 3, 5, and 7 and ticket lines 44, 48, 54. Reply to Keel.

**Patron**

> [from Patron] DEV-538 Q5 ruling: Ratified rendering labelled More for you section (h2 + Load more) when initial More cards = 0 and hasMore is true, mounting MovieCarousel when cards >= 1, offering Swap only when DEV-517 prerequisites hold, and hiding More for you when it has neither cards nor available controls; full ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-538-q5.md

Ratified: Render the labelled More for you region with its h2 and Load more control when initial More for you has 0 cards but hasMore is true, mounting MovieCarousel once More for you has at least 1 card and omitting More for you entirely when it has neither cards nor an available control (`specs/DEV-538/ticket-text.txt:33,35,40,44,48,50,54`; log tagged `[assumed]` in `specs/DEV-538/ASSUMPTIONS.md` per `specs/PRODUCT.md:28-30`).

- **Zero-card More for you with `hasMore: true` (`specs/DEV-538/ticket-text.txt:35,40,48,50,54`)**: When all initial items land in themed rows (1a–1c) and `hasMore` is `true`, render the labelled `More for you` region and its `h2` heading with the `Load more` control so rule 5 paging remains reachable without firing a second initial request (`Done when` line 54), and mount `MovieCarousel` once the row has at least one card.
- **One-or-more cards and Swap prerequisites (`specs/DEV-538/ticket-text.txt:33,35,40,48`)**: Because `More for you` is the fold target for rule 3 (`< 2` movies), it renders its `MovieCarousel` whenever it has `>= 1` card, and displays `Swap` only when existing `DEV-517` prerequisites hold (e.g., replaceable cards exist in `More for you`).
- **Skipping empty More for you (`specs/DEV-538/ticket-text.txt:44`)**: When `More for you` has 0 cards and no available control (`hasMore === false`), skip rendering the `More for you` section entirely so empty-row tests under `Done when` line 44 pass cleanly.


---

## Q6. Empty-state wording and library link

**Keel**

> [from Keel] DEV-538 grill Q6. What copy should the two rule 6 empty states use? I recommend retaining the exact current message for 6a, No recommendations yet. Mark movies as watched or liked to get some. For 6b I recommend No recommendations closely match the movies you have liked, watched, or added to your watchlist yet. Give both a Browse the library link to the existing library destination. This preserves the ticket wording for 6a and makes the reason for 6b clear. Please rule against ticket lines 20 and 36-39. Reply to Keel.

**Patron**

> [from Patron] DEV-538 Q6 ruling: Ratified 6a copy (No recommendations yet. Mark movies as watched or liked to get some.), 6b copy (No recommendations closely match the movies you have liked, watched, or added to your watchlist yet.), and Browse the library link for both (log 6b copy and link label as [assumed] in ASSUMPTIONS.md); full ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-538-q6.md

Ratified: Adopt Keel's rule 6 empty-state copy for 6a and 6b and the shared Browse the library link (`specs/DEV-538/ticket-text.txt:20,36-39,49`; log the 6b message and link label tagged `[assumed]` in `specs/DEV-538/ASSUMPTIONS.md` per `specs/PRODUCT.md:28-30`).

- **Case 6a copy (no seeds, `specs/DEV-538/ticket-text.txt:20,37,39`)**: When `hasSeeds === false`, render the exact existing copy: `No recommendations yet. Mark movies as watched or liked to get some.`
- **Case 6b copy (seeds exist, no strong match, `specs/DEV-538/ticket-text.txt:38-39`; `specs/PRODUCT.md:28-30`)**: When `hasSeeds === true` and initial `items` is empty, render: `No recommendations closely match the movies you have liked, watched, or added to your watchlist yet.` (record tagged `[assumed]` in `specs/DEV-538/ASSUMPTIONS.md`).
- **Library link (`specs/DEV-538/ticket-text.txt:36,49`; `specs/PRODUCT.md:28-30`)**: Include a `Browse the library` link to the existing library destination in both 6a and 6b empty states (record link label tagged `[assumed]` in `specs/DEV-538/ASSUMPTIONS.md`).


---

## Q7. Contract-first implementation and verification

**Keel**

> [from Keel] DEV-538 grill Q7. What implementation and verification sequence should Quill plan for the owner-conditional scope? I recommend confirming prerequisite contracts at pickup, then sourcing watched collection membership and seed presence through the existing recommendations query without changing eligibility or ranking, exposing the two approved values through the existing For You response, and moving C# DTO -> OpenAPI -> generated TypeScript before web work. Keep Related serialization and default MovieCard behavior unchanged. In the existing For You feature, isolate pure grouping and shared caps, render labelled h2 regions via the existing MovieCarousel and reuse DEV-517 controls. Then run backend contract/query tests and harness gates for changed C#, web component/regression tests and web gates, and desktop/phone before-and-after captures. Scope files to the existing recommendation path and generated contracts, For You page/CSS/API/grouping/tests, and shared MovieCard/tests. This preserves one initial GET and keeps data contracts ahead of consuming UI. Please rule on the sequence and clarify how the shared RecommendedMovieDto must preserve the Related JSON under the For You-only amendment. Basis: ticket lines 25-54 and 63-75, recon lines 21-34 and 51-54, Q1-Q6. Reply to Keel.

**Patron**

> [from Patron] DEV-538 Q7 ruling: Ratified contract-first implementation and verification sequence; keep Related JSON unchanged by populating StartedCollectionId only on For You and omitting null via JsonIgnoreCondition.WhenWritingNull on shared RecommendedMovieDto; full ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-538-q7.md

Ratified: Adopt Keel's contract-first implementation, file-scope, and verification sequence for the owner-conditional scope, keeping `/movies/{id}/recommendations` Related JSON unchanged by omitting null `startedCollectionId` on `RecommendedMovieDto` (`specs/DEV-538/ticket-text.txt:25-54,63-75`; `recon-DEV-538:21-34,51-54`).

- **Prerequisite check & backend contract chain first (`specs/DEV-538/ticket-text.txt:19,54,74-75,87-89`; `recon-DEV-538:26-27,51-54`)**: Confirm landed `DEV-541`/`DEV-542`/`DEV-543`/`DEV-517` contracts at pickup, compute `StartedCollectionId` and `HasSeeds` in `GetForYouRecommendationsQueryHandler` without changing eligibility or scoring (`ticket-text.txt:25,58`), expose `HasSeeds` on `ForYouRecommendationsPageDto` and nullable `StartedCollectionId` on `RecommendedMovieDto`, and regenerate `C# DTO -> web/src/api/openapi.json -> web/src/api/types.ts` before web edits.
- **Preserving Related JSON on shared `RecommendedMovieDto` (`src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs:30-43,66-100,181-199`; `recon-DEV-538:26,52`)**: Populate `StartedCollectionId` only in the For You projection (`null` for `/movies/{id:int}/recommendations`) and decorate it with `[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` (matching `DEV-542`'s optional-property serialization on `RecommendedMovieDto`) so Related responses omit `startedCollectionId` and keep their JSON payload unchanged while OpenAPI/TypeScript exposes `startedCollectionId?: number | null`.
- **Web isolation & verification sequence (`specs/DEV-538/ticket-text.txt:27-53,67-73`; `recon-DEV-538:21-25,30-34`)**: Keep `MovieCard` seed-line display enabled by default while adding the hide-seed-line prop for row cards, isolate pure grouping and single-file caps (`4` seed rows, `6` cards/row) alongside `ForYouPage.tsx`/`ForYouPage.css`/`forYouApi.ts` reusing `MovieCarousel` and `DEV-517` controls, then run backend unit/integration tests and C# harness gates, web component/regression tests and web gates, and attach desktop/phone before-and-after screenshots to the PR.


---

## Q8. Frozen scope, closing bar, and round cap

**Keel**

> [from Keel] DEV-538 grill Q8. Can we close the grill on these review terms? Freeze scope to the Q1-Q7 owner-conditional For You grouping, the two response fields and contract chain, the MovieCard seed-line opt-out, DEV-517 More for you integration, and corresponding tests and screenshots. Keep scoring, ranking, candidate-pool semantics, schema, dependencies, navigation, home page design, and per-themed-row paging outside the freeze; anything else is a follow-up issue, not a finding in this round. I recommend a two-round review cap and two fix commits per round per task-pipeline lines 28-29. Close only when required harness and web gates and ticket acceptance evidence pass, all three Size L axes report on the reviewed head, every High or Medium and any accepted in-scope Low finding is closed with evidence, and every remaining Low has an explicit disposition. Escalate unresolved in-scope findings after the cap. The consolidated Q2 owner checkbox remains open and gates implementation; it does not prevent writing the spec brief. Please confirm or amend and close the grill. Reply to Keel.

**Patron**

> [from Patron] DEV-538 Q8 confirmed: Ratified frozen scope (Q1-Q7 owner-conditional For You grouping, startedCollectionId and hasSeeds contract chain, MovieCard seed-line opt-out, DEV-517 More for you integration, tests, and screenshots; scoring/ranking/schema/dependencies/nav/home/per-row paging out of scope), standing Size L closing bar (2 review rounds, 2 fix commits/round per task-pipeline:18-29), and consolidated Q2 owner checkbox gating Gate 1 while spec brief proceeds; grill closed.


---

## Gate 1 provisional ruling (Phase 2 step 7)

`gate1: provisional` — Plan challenge adjudication (`specs/DEV-538/PLAN_ADJUDICATION.md`, `PLAN FROZEN`, 19 findings: 0 High, 9 Medium, 10 Low — 17 accepted with repairs in `brief.md`, `spec.md`, `plan.md`, `tasks.md`, and `docs/adr/0047-for-you-grouping.md`; 2 Low recorded), `checklists/requirements.md` User-Visible Outcome items ticked, and 1 structural owner checkbox confirmed in `specs/DEV-538/spec.md:15` (consolidated Q1+Q2 amendment to Done when line 54 for `startedCollectionId` and `hasSeeds`). Gate 1 remains closed until the user checks the owner checkbox and merges the Spec Kit PR.

---

## Gate 1 Spec Kit PR merge receipt (T000)

**Gate 1 verified.** [Spec Kit PR #459](https://github.com/lamuniercosta/lamuflix/pull/459) shows the owner checkbox checked for the Done when line 54 amendment, approving additive `startedCollectionId` and `hasSeeds` fields on the single For You GET request. GitHub records that `lamuniercosta` merged the PR into `main` on 2026-11-21 at 17:15:32 UTC as `d9be8d2f34b861072e721156f0397b8270c028f7`.

The DEV-538 task note records Phase A base `1bbc8a637b97ec2efdf0eaa6f2be6da8abbbc089`. Git confirms that base is an ancestor of the merge commit and that the merge commit is present on the Build branch `feature/DEV-538`. T000 is complete. T001 pickup and runtime acceptance remain pending.
