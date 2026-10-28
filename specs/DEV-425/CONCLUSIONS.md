# DEV-425 grill conclusions

Append-only decision exchanges. Evidence: notes DEV-425 and recon-DEV-425; worktree F:/Dev/LamuFlix.worktrees/feature-425-spec, branch feature/425-spec, base f698aeceb6b6af50a397d076b83ed178566641b0. Patron decides under charter section 2.3. Grill cap: 12 questions.

## Q1 — Facet contract and vocabulary

Keel: Do you rule that the ticket-authorized director facet is GET /api/directors with no role parameter, returning DirectorDto[] with id, name, movieCount and excluding directors with zero linked movies? I recommend this dedicated route and DTO, reusing the existing facet/query/handler/port pattern, because the route itself identifies the role and GenreDto supplies the count precedent; cost is a new ticket-scoped DTO and handler, with no dependency/schema/layer change. Cite exact ticket, PRODUCT or constitution basis and settle canonical Director, Director facet and Director filter terms.

Patron response:
[from Patron] DEV-425 Q1 ruling: Approved — GET /api/directors with no role parameter, returning DirectorDto[] (id, name, movieCount), excluding directors with zero linked movies.

Basis (max 3):
- Ticket-decided, not a section 2.3 escalation: DEV-425 note Scope states Facet directors endpoint in FacetEndpoints mirroring /people (id, name, movie count; directors with at least one movie), and recon-DEV-425 section 6 item 4 confirms the /api/directors route with (id, name, movie count) is literally in the ticket Scope; recon section 1 confirms the shared /api group makes /api/directors the pattern-consistent route.
- Precedent: recon section 1 — GenreDto is (Id, Name, MovieCount) and supplies the count shape; recon section 2 — GetGenresAsync projects Movies.Count, and the at-least-one-movie filter is new ticket-specified behaviour versus /people. The role=actor requirement on /people exists only because one route serves a role-selected people facet; a dedicated directors route carries the role in the route itself, so a role parameter would be redundant and is not authorized by the ticket.
- No care-item change: recon sections 6 and 1-2 — no new dependency, layer, or schema (directors and movie_directors exist since Initial); DirectorDto + GetDirectors query/handler/port reuse the existing facet pattern, ticket-scoped.

Canonical terms settled:
- Director: a DirectorRecord person credited in the director role, linked to movies via movie_directors; distinct from the actor/person facet (PersonDto on /people).
- Director facet: the GET /api/directors facet list — DirectorDto items (id, name, movieCount) for Directors with at least one linked movie.
- Director filter: the singular DirectorId/directorId single-select browse filter over that Director identity (detailed behaviour for later questions; multi-director is ticket out-of-scope).

---

## Q2 — Query parsing and page reset (pending recon, not decided)

Keel: Do you approve directorId parsing consistently across API, C# MovieQueryString and web URL state: absent or empty means no filter; one valid int selects that director; duplicate keys, malformed or overflowing ints are rejected as 422 by the API and invalid by the C# parser; unknown integer identities yield an empty matching list, with no extra existence/positive-id validation? I recommend existing ReadNumber/Seen-set conventions and matching web normalization to current single-value filter behavior, preserving other filters and resetting page on director change; cost is updating equality/hash and serialization tests as well as endpoint mapping. Basis recon sections 3-4. If web malformed URL normalization needs a fact not in recon, do not invent it: state the decision needed explicitly. Cite basis. Facts come from recon-DEV-425. If you need one that is not there, reply needs recon: <question> and stop there.

Patron: DEV-425 Q2 needs recon: How does web filterQuery/fromParams and FilterPanel currently normalize a malformed single-value integer URL key such as runtimeMin, and reset page on a single-value filter change - dropped to absent, preserved as unknown, or another behaviour - and does browseParams forward the malformed value? Recon sections 3-4 do not state this, so I do not rule Q2 until that fact is supplied.

Disposition: pending recon. Q1 is settled; Q2 has no ruling. No brief or grill closure yet. Resume Q2 after recon is appended to recon-DEV-425. Questions used: 2 of 12.

### Q2 continuation after Wisp recon

Keel: Wisp reports live filterQuery preserves raw scalar strings, params.get first-wins duplicates; consumers are lenient; FilterPanel commits reset page=1; browseParams forwards malformed values to API. The strict libraryUrl parser is not live. Revised Q2 recommendation: API and C# MovieQueryString accept absent/empty as null, valid int32 as DirectorId, reject duplicate/malformed/overflowing values (API 422, C# parse false), unknown/zero/negative integers return no matches without existence validation. Web directorId follows live scalar architecture: retain raw scalar string, first occurrence wins, serialize nonempty value and forward to API for authoritative int validation; empty clears/omits; no broad parser cleanup or wiring dead libraryUrl. Each director selection/replacement/clear resets page=1 and preserves other filters. Equality/hash include DirectorId. Do you approve this precise split, with cited basis? The initial question and recon deferral are already recorded; this is continuation Q2, not a new question.

Patron response:
[from Patron] DEV-425 Q2 ruling: Approved — the precise split as recommended: strict API and C# MovieQueryString parsing, lenient live-web raw scalar handling, first occurrence wins on the web, empty clears/omits, each director selection/replacement/clear resets page to 1 and preserves other filters, and Equality/hash include DirectorId.

Basis (max 3):
- Initial recon section 3: API single-value keys use ReadNumber with MarkSeenOnce — duplicate is 422, empty means null, non-int is 422; MovieQueryString singular directorId follows the Seen-set duplicate-rejection pattern; MovieQuery Equals/GetHashCode must extend for the new singular DirectorId. Overflow and malformed values therefore fail int32 parsing as API 422 and C# parse false.
- Q2 recon sections 1-4 and Summary: the live web path (filterQuery.fromParams to FilterPanel to buildBrowseParams to useBrowseMoviesQuery) preserves raw scalar strings, params.get makes the first duplicate win, malformed values are forwarded to the API for authoritative validation, FilterPanel commit resets page to 1 on every committed change, and the strict libraryUrl parser is dead code (test-only import) — so no broad parser cleanup and no wiring of libraryUrl.
- Initial recon section 3: MovieQueryValidator does not validate ActorIds/GenreIds existence and ints are accepted as-is, with DirectorId needing no validator rule beyond parse-time 422; an unknown, zero, or negative int32 therefore flows to the movie_directors match and returns no matches, with no existence or positive-id validation.

---

## Q3 — Hosts, composition, and missing selected identity

Keel: Should the Director filter be available in both Library and Watchlist uses of the existing FilterPanel, with director predicate AND-composed with all active browse/watchlist predicates? I recommend both, because the ticket names Filter Panel DEV-322 without a route restriction and shared URL filtering should behave consistently; fetch directors independently in each host needing them, keep full-library facet counts independent of active filters/watchlist, and preserve a selected directorId when absent from returned facets (show its id fallback and allow clearing rather than silently broadening results). Cost is wiring the watchlist host as well as browse, while using the same existing movie browse API. Confirm whether this interprets existing ticket delivery or contradicts it; cite basis.

Patron response:
[from Patron] DEV-425 Q3 ruling: Approved both — the Director filter is available in Library and Watchlist uses of the existing FilterPanel, AND-composed with all active browse/watchlist predicates; this interprets existing ticket delivery and does not contradict it.

Basis (max 3):
- Ticket delivery, not a scope change: DEV-425 ticket Scope (intake note line 28, quoted in initial recon sections 4 and 6) names a searchable single-select combobox in Filter Panel DEV-322 with URL directorId and sets no route restriction; initial recon section 4 records the watchlist variant passing genres only and leaves browse-only versus both as a spec decision, so supplying the same filter there completes the named panel, not new delivery.
- Composition: initial recon section 2 — BrowseAsync is a fluent AND chain including WhereInWatchlist, and WhereDirector slots after WhereActors on the same existing movie browse API, so the director predicate composes with watchlist and all active filters with no new API, layer, or schema.
- Facet and selection semantics: Q1 settled the Director facet as full-library DirectorDto counts (id, name, movieCount, at least one linked movie), independent of active filters or watchlist; Q2 settled that an unmatched directorId yields no matches rather than broadening. Preserving a selected directorId absent from returned facets, showing its id fallback, and allowing clearing follows that no-silent-broadening rule and costs only wiring the watchlist host plus an independent directors fetch in each host.

---

## Q4 — Facet counts, ordering, search and taste

Keel: Do you approve facet and search semantics: counts are distinct linked library movies per director, independent of active query/watchlist; directors with same name remain separate identities; facet order Name then Id using database ordering as existing facets; combobox uses client-side case-insensitive substring search over names with deterministic name/id ordering and at most 8 suggestions, matching cast precedent? I recommend label Director, placeholder Search directors, empty message No directors found, and count badges in suggestions; retain selection separately from search text so typing never clears the active predicate. Cost is explicit disambiguation by count/id for shared names and taste defaults for Palette to present accessibly. Log wording/order/search-limit choices as assumed. Cite ticket count and existing facet/cast evidence; no endpoint search or pagination is added.

Patron response:
[from Patron] DEV-425 Q4 ruling: Approved — distinct linked-movie counts independent of active query/watchlist, same-name directors remain separate identities, facet order Name then Id via database ordering, and client-side case-insensitive substring combobox search with deterministic name/id ordering capped at 8 suggestions, count badges, and selection retained separately from search text so typing never clears the active predicate. No endpoint search or pagination is added.

Basis (max 3):
- Ticket and facet evidence: DEV-425 ticket Scope requires (id, name, movie count; directors with at least one movie); initial recon section 2 shows GetGenresAsync projecting linked Movies.Count and GetPeopleAsync ordering Name then Id, and section 5 cites the shared-name facet test precedent (GetPeople_ActorRole_ReturnsScalarArrayPreservingSharedNames and SharedMovie_CountsOncePerGenre) — separate ids, not name merging, and counts from the full linked library per Q1/Q3.
- Cast precedent: initial recon section 4 — the DEV-322 cast combobox typeahead filters the sorted facet list with CAST_SUGGESTION_LIMIT = 8 and excludes already-selected ids, and genres render a movieCount badge; mirroring that pattern gives the director combobox its search, ordering, limit, and badges with no new endpoint behaviour.
- Taste choices logged as assumed: label Director, placeholder Search directors, empty message No directors found, Name-then-Id order, and the 8-suggestion limit are accepted as recommended and must be logged tagged [assumed] in specs/DEV-425/ASSUMPTIONS.md; Palette presents them accessibly within those settled words.

---

## Q5 — Combobox interaction, accessibility and local states

Keel: Do you approve this interaction/state contract for the existing dependency-free single-select combobox: selecting replaces the sole director; explicit Clear Director removes only directorId and Clear all includes it; typing changes only suggestions until selection; keyboard supports Arrow navigation, Enter select, Escape close, visible focus, associated label and combobox/listbox/option semantics with selected/expanded state; loading and director-fetch failure are local to this control, other filters stay usable, selection is retained/clearable during loading/error, and failure offers retry using existing query refetch? I recommend disabled suggestions while loading/error, status messages Loading directors and Could not load directors, Retry button and Director <id> missing-selection fallback; cost is keyboard/state tests without a new npm package. Palette supplies responsive placement and visual details before plan challenge. Cite basis, log copy as assumed.

Patron response:
[from Patron] DEV-425 Q5 ruling: Approved — the dependency-free single-select interaction/state contract as recommended: selecting replaces the sole director, Clear Director removes only directorId, Clear all includes it, typing changes only suggestions until selection, full keyboard/ARIA combobox semantics, and loading/fetch failure local to this control with selection retained and clearable and retry via existing query refetch.

Basis (max 3):
- Existing panel precedent: initial recon section 4 — the DEV-322 cast combobox keeps an independent error state (castFailed renders an alert and keeps the rest usable), FilterPanel tests cover independent genre/cast error states, and handleClear resets the filter set; extending that locality to the director control, with Clear Director scoped to directorId only, follows the same architecture with no new npm package.
- Settled state semantics: Q2 ruled every committed filter change resets page to 1 and preserves other filters, Q3 ruled a selected directorId absent from returned facets is preserved with an id fallback and remains clearable rather than silently broadening, and Q4 ruled typing never clears the active predicate — the loading/error retention, Director <id> fallback, and suggestion disabling are the direct application of those rulings.
- Taste choices logged as assumed: Clear Director, Loading directors, Could not load directors, and Retry copy are accepted as recommended and must be logged tagged [assumed] in specs/DEV-425/ASSUMPTIONS.md; Palette supplies responsive placement and visual details before plan challenge within the settled keyboard, focus, label, and combobox/listbox/option semantics.

---

## Q6 — Approach, file boundary, ordering, tests and gate expectations

Keel: Do you approve this implementation boundary and order: first Core DirectorId equality/hash and MovieQueryString; existing IMovieCatalog DirectorFacet/GetDirectorsAsync plus GetDirectorsQuery/handler; then EF WhereDirector using navigation Any and director facet projection over existing directors/movie_directors, with real PostgreSQL tests; then API DirectorDto/FacetEndpoints/HandlerRegistration and BrowseMoviesRequest/LibraryEndpoints mapping with endpoint tests; then generate OpenAPI followed by generated TS and mocks/typecheck; finally filterQuery/browseParams/FilterPanel and both LibraryView hosts plus web tests? No package, project, layer, migration, schema/metadata-ingest edit, LocalPlay, secrets, Process.Start, tracked-file deletion or broad rewrite; only narrow edits forced by this API/filter delivery, contract gates and IMovieCatalog compilation (test fakes). API 200 array including empty [], browse invalid scalar 422; reuse current fault handling. Test plan covers distinct facet counts/orphans/shared names, null/match/unmatched/AND composition without duplicate movies, query equality/hash/roundtrip/strict parsing, API shape/status, generated-contract drift, URL reload/first-wins/raw forwarding/page reset, selection/replace/clear/clear-all, keyboard and independent loading/error/retry on both hosts. Include seeded property roundtrips for new query invariant. Backend/web gates run later by pipeline, not in Phase A; no threshold changes, scope-empty skip never PASS. I recommend this order to enforce DTO to OpenAPI to generated types, cost is targeted updates across existing layers and their tests. Cite basis; Quill must not fill missing decisions.

Patron response:
[from Patron] DEV-425 Q6 ruling: Approved — the implementation boundary and order as recommended, from Core DirectorId/MovieQueryString through port, EF, API, generated contract, and web hosts, with the stated test plan including seeded property roundtrips; Quill must not fill missing decisions — any gap returns to Patron.

Basis (max 3):
- Contract chain and recon order: charter hard rule flows C# DTO to OpenAPI (web/src/api/openapi.json) to generated TypeScript (web/src/api/types.ts) with no hand-rolled web duplicates, and initial recon sections 1-4 map exactly the named chain — MovieQuery/MovieQueryString, IMovieCatalog DirectorFacet/GetDirectorsAsync with GetDirectorsQuery/handler, EfMovieCatalog WhereDirector via the existing movie_directors navigation, FacetEndpoints/HandlerRegistration/BrowseMoviesRequest/LibraryEndpoints, then openapi regeneration, mocks/typecheck, filterQuery/browseParams/FilterPanel and both LibraryView hosts.
- Boundary exclusions match recon section 6: no new dependency, project, layer, or migration (directors and movie_directors exist since Initial), no schema/metadata-ingest, LocalPlay, secrets, or Process.Start contact, and no tracked-file deletion or broad rewrite; only narrow edits forced by this delivery, contract gates, and IMovieCatalog test-fake compilation.
- Status and gate semantics: initial recon sections 1 and 3 settle facet 200 array including empty [] and browse invalid-scalar 422 under current fault handling; gates run later by the pipeline, not in Phase A, with no threshold changes and a scope-empty skip never reported as PASS.

---

## Q7 — Loop discipline and frozen scope

Keel: Do you approve loop terms: Critical, High and Medium findings block code-review/ship-review closure; Low findings are recorded nonblocking; two review rounds maximum and at most two fix commits per round, with any third-round unresolved above-bar finding escalated on the PR rather than relabelled or waived? Frozen scope is only Q1-Q6 Director facet and single-select Director filter across Library/Watchlist, necessary existing-chain API/URL/contract/test edits and approved accessibility/local states; multi-director selection, genre/cast behavior changes, parser-wide cleanup, metadata ingest/schema/packages/layers and unrelated fixes are excluded; anything else is a follow-up issue, not a finding in this round. Follow-up recording follows charter section 2.3 Patron/Rigger policy, with minor observations noted no ticket. I recommend these defaults because they bound remediation without hiding correctness gaps; cost is unresolved above-bar issues remaining blocked at the cap. M requires all Sentry/Ledger/Compass axes; Palette design before plan challenge; Gate 1 remains user-owned spec merge, and this grill closes no later gate. Cite standing basis and rule any departure explicitly.

Patron response:
[from Patron] DEV-425 Q7 ruling: Approved — Critical, High, and Medium findings block code-review/ship-review closure, Low findings are recorded nonblocking, at most two review rounds and two fix commits per round, and any third-round unresolved above-bar finding is escalated on the PR rather than relabelled or waived; frozen scope is Q1-Q6 only. No departure.

Basis (max 3):
- Standing loop basis: this closing bar, frozen scope, and round cap are cited once for this chain and not re-ruled per ticket; escalating an unresolved above-bar finding on the PR instead of waiving it follows the harness rule that a gate threshold is never lowered to make a gate pass.
- Frozen scope: Q1-Q6 settle the Director facet and single-select Director filter across Library/Watchlist plus necessary existing-chain API/URL/contract/test edits and approved accessibility/local states; multi-director selection, genre/cast behaviour changes, parser-wide cleanup, metadata ingest/schema/packages/layers, and unrelated fixes are excluded.
- Follow-up recording per charter section 2.3 Patron/Rigger policy: an out-of-scope Critical or High finding, broken behaviour, or user-requested item goes to Rigger for a follow-up ticket (one per finding, existing open ticket folded first); anything else is noted, no ticket in CONCLUSIONS.md. M tier keeps all Sentry/Ledger/Compass axes, Palette design precedes plan challenge, Gate 1 remains the user-owned spec merge, and this grill closes no later gate.

---

## Q8 — Shared understanding and grill closure

Keel: Please read specs/DEV-425/brief.md, CONTEXT.md, CONCLUSIONS.md and ASSUMPTIONS.md in F:/Dev/LamuFlix.worktrees/feature-425-spec on feature/425-spec. Do you confirm shared understanding and close the grill on Q1-Q7 as faithfully transcribed in the brief, with no unresolved structural checkbox, so these decisions and canonical glossary are ready for Quill speckit-specify/plan/tasks and Palette design? I recommend close: all delivery, parsing, hosts, states, file boundary, tests, gate expectations and loop terms are settled; cost is that any later genuine gap must return as needs decision rather than Quill inventing it. The brief explicitly flags that current recon note replaced initial intake text with Q2 evidence, and cites preserved intake facts in our full exchanges rather than claiming new recon. The brief also follows live task-pipeline M versus L clarify/checklist scope over the stale intake next-line. No production/test gate or plan challenge has run and Gate 1 remains user-owned. Reply approved or give one concrete correction and cited basis; do not edit the four files concurrently. Budget now 8 of 12 questions.

Patron response:
[from Patron] DEV-425 Q8 ruling: Approved — shared understanding confirmed and the grill is closed on Q1-Q7 as faithfully transcribed in brief.md, CONTEXT.md, CONCLUSIONS.md, and ASSUMPTIONS.md, with no unresolved structural checkbox; ready for Quill speckit-specify/plan/tasks and Palette design.

Basis (max 3):
- Fidelity check: I read all four files in F:/Dev/LamuFlix.worktrees/feature-425-spec on feature/425-spec — brief.md Settled answers Q1-Q7 match the CONCLUSIONS.md exchanges, CONTEXT.md carries the canonical Director/Director facet/Director filter glossary from Q1, and ASSUMPTIONS.md carries the Q4/Q5 taste entries tagged [assumed].
- No structural escalation: brief.md Gate expectations and handoff correctly records that no production/test gate or plan challenge has run, Gate 1 remains the user-owned spec merge, the recon-note replacement and preserved intake facts are flagged rather than claimed as new recon, and the live task-pipeline M-versus-L clarify/checklist scope supersedes the stale intake line.
- Closure cost accepted as recommended: any later genuine gap returns as needs decision through Keel to Patron rather than Quill inventing it; this closure authorizes no Build, closes no later gate, and implies no merge approval.

Disposition: grill closed after eight questions (Q2 recon continuation is the same question), within the twelve-question budget. All Q1-Q7 decisions are in brief.md; Q8 confirms faithful transcription and shared understanding. No unresolved structural owner checkbox. Quill and Palette may proceed with their later Phase A work; Gate 1, plan challenge and Build remain separate and uncleared.

---

## Plan challenge decision PC1 — Phase 2 step 6

Keel request: explicitly name the property generator, handler and browse-parameter test targets; clarify combined Phase 1 red semantics and dependency order; remove unapproved wrap behavior; fix closed-state ARIA references; keep the design tree conceptual in FilterPanel.tsx. Evidence: all three findings-DEV-425 axis notes; brief Q2/Q5/Q6/Q7. Patron supplied the following complete ruling without edits:

[from Patron] DEV-425 Phase 2 step 6 ruling: APPROVED as recommended, with the Compass 3 refinement below. Test-only clarification of Q6 frozen scope; no delivery change; no implementation authorized by this ruling; no owner escalation.

1. Q6 frozen-scope clarification (Ledger M1/M2, Compass 1/5) — allowed test files, and only these additions:
   - tests/LamuFlix.UnitTests/Library/MovieQueryFixture.cs and tests/LamuFlix.UnitTests/Library/MovieQueryGeneratorTests.cs: allowed, so the FsCheck generator actually emits non-null DirectorId and the seeded property roundtrips are non-vacuous.
   - tests/LamuFlix.UnitTests/Library/MovieQuerySurvivorTests.cs and MovieQueryStringSurvivorTests.cs: allowed only if needed to exercise non-null DirectorId in the survivor paths; not a blanket authorization.
   - New tests/LamuFlix.UnitTests/Features/Library/GetDirectorsQueryHandlerTests.cs: allowed, following the existing GetGenres/GetPeople handler-test convention.
   - tests/LamuFlix.Test/AddHandlerTests.cs: allowed only if required to assert the existing handler registration; otherwise untouched.
   - New web/src/library/browseParams.test.ts: allowed as the concrete T041 target, replacing the vague browseParams tests wording.
   Basis: brief Q6 step 6 (matching tests and test fakes forced by the added IMovieCatalog member) plus Q6 test plan (seeded property roundtrips; handler coverage following existing facet handlers; web URL/browse-parameter tests); brief Q7 frozen scope expressly includes necessary existing-chain test edits; ticket Scope (DEV-425 note lines 28-30) authorizes MovieQuery.DirectorId/directorId and the combobox. AGENTS.md non-negotiable: a surviving mutant is a missing test — a generator that never emits DirectorId would leave the roundtrip proof vacuous. No section 2.3 care item is triggered: no new dependency, project, layer, schema, or API shape beyond the ticket; no LocalPlay, secrets, or Process.Start; the named test files are now explicitly in scope, so no unnamed tracked-file rewrite.

2. Phase 1 order and red/green (Ledger M3) — approved:
   - Keep the single combined Core+Persistence Phase 1 (analyze I1 closure stands; no placeholder or default-throwing port implementation).
   - Compile-failure red is acceptable while the new required symbols/port member are being introduced; behavioral red is required after compile closure; one green checkpoint closes the phase.
   - State the actual execution order explicitly as T003, T005, T004, T006 (dependency order), not numeric T001-T006.
   Basis: brief Q6 binding sequence (Core, then Persistence, in one executable phase per analyze fix I1/I2: T005 depends on T003, T004 depends on T005) and Q6 prohibition on placeholder implementations; plan/tasks text clarification only.

3. Compass 2 (wrap-at-ends) — approved: remove the Palette wrap-at-ends claim from design.md; Arrow-key navigation remains exactly as settled in brief Q5, with no wrap behaviour added and no ASSUMPTIONS.md entry.
   Basis: brief Q5 settles Arrow navigation, Enter, Escape only; wrap is unsettled interactive behaviour and may not enter through design text.

4. Compass 3 (ARIA) — approved as refined: retain aria-expanded=false when closed; render aria-controls only when the mounted listbox exists; render aria-activedescendant only when an active descendant is present. T043 asserts exactly this.
   Basis: brief Q5 requires combobox/listbox/option semantics including expanded state; aria-expanded=false is the valid closed-state expression of that settled contract, while controls/activedescendant must reference elements that exist.

5. Ledger L1 (component tree) — approved: the design.md component tree stays conceptual within FilterPanel.tsx; no extracted subcomponent files or new abstraction are mandated or authorized.
   Basis: brief Q6 step 5/Q7 frozen scope allows FilterPanel.tsx edits only in the web host files, with no new layer or files; section 2.3 care items 2/6 are not triggered by in-file structure.

6. All other Ledger/Compass/Sentry Low and Informational items remain recorded and non-blocking per brief Q7; they authorize no scope expansion. The 7 Medium findings are closed by items 1-2 above once Quill folds them into plan.md/tasks.md/design.md text and Keel read-back confirms. Gate 1 provisional marking remains a separate later step after that read-back; this ruling does not mark it.

Disposition: proceed with documentation fixes only on this ruling. No blocked: structural question; no constitution departure identified.

Keel application: Patron item 6 is not a closure receipt. All seven Medium entries remain open until direct read-back, including Sentry R1 and R2, which require the separate API empty/duplicate parsing and raw web forwarding pins. No above-bar finding is waived. Low items are recorded individually in PLAN_CHALLENGE.md. No ticket write requested.
