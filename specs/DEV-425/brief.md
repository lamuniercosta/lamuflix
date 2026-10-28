# DEV-425 — Director facet and Director filter brief

Status: grill closed by Patron at Q8, after eight questions out of twelve. Q1–Q7 are settled with no unresolved structural owner checkbox. This is a Phase A decision brief, not implementation or Gate 1 approval.

## Identity and evidence

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-425-spec`.
- Branch: `feature/425-spec`; intake base: `f698aeceb6b6af50a397d076b83ed178566641b0`.
- Ticket: DEV-425, parent DEV-100, `size:M`, `ui:`. Dependencies: DEV-411 director ingestion and DEV-322 Filter Panel.
- Ticket authority: note DEV-425 lines 26–30: director facet in FacetEndpoints from DirectorRecord with id, name and movie count, only directors with movies; MovieQuery.DirectorId and directorId via movie_directors; searchable single-select in the existing Filter Panel and URL. Multi-director selection and genre/cast multi-select changes are excluded.
- Facts: Wisp initial intake recon as cited in CONCLUSIONS.md Q1–Q7 and current note recon-DEV-425 for bounded Q2 URL evidence. The current recon note contains the Q2 pass in place of the initial intake text; the initial facts used here are preserved in the cited decision exchanges and this file boundary, not newly measured by Keel.
- Decisions: complete question/answer exchanges, cited basis and implications in CONCLUSIONS.md. Taste defaults are also tagged `[assumed]` in ASSUMPTIONS.md. Canonical vocabulary is in CONTEXT.md.

## Settled answers

### Q1 — Facet contract

`GET /api/directors`, with no required role parameter, returns HTTP 200 and a `DirectorDto[]` of `{ id, name, movieCount }`, including `[]` when empty. Include only directors linked to at least one library movie. Use existing query/handler/port architecture: DirectorFacet and GetDirectorsAsync, GetDirectorsQuery/handler, DirectorDto and endpoint registration. Director identity is separate from actor identity even when names match.

### Q2 — Query contract and URL behavior

Add `int? DirectorId` to MovieQuery and include it in equality and hashing. The URL/API key is singular `directorId`.

- API and C# MovieQueryString: absent or empty means null; one valid int32 selects; duplicate occurrences, malformed and overflowing integers fail (API 422, C# parse false). An unknown, zero or negative integer yields no matching movies; do not add existence or positive-id validation.
- Live web scalar handling: preserve raw nonempty directorId strings, first occurrence wins on duplicate URL keys, serialize/forward the scalar to the API for authoritative integer validation. Empty clears and omits it. Do not wire the unused strict libraryUrl parser or clean up other parsers.
- Selection, replacement and clearing reset page to 1 and preserve all other filter values. Include directorId in URL recognition/serialization and API browse parameters; valid URL reload and navigation restore it.

### Q3 — Hosts and composition

Provide the control in both Library and Watchlist FilterPanel hosts. AND-compose its movie_directors predicate with all active browse predicates, including watchlist, without duplicate movie rows. Fetch the director facet independently for each host needing it through the existing query architecture. Counts always describe the full library, independent of current filters and watchlist.

A selected identity absent from facet results remains selected and clearable, displayed with the `Director <id>` fallback. Do not silently remove its predicate or broaden results. This includes selection retention during loading or fetch errors.

### Q4 — Counts and search

Count distinct linked library movies per director. Preserve separate same-name identities. Order facets by Name then Id using existing database ordering. Search names client-side with case-insensitive substring matching, deterministic name/id ordering, and at most eight suggestions. Show movie-count badges; disambiguate shared names by identity/count. No endpoint search or pagination is added. Typing edits search text only; it does not alter the active director.

Taste defaults: label `Director`, placeholder `Search directors`, empty message `No directors found`. See ASSUMPTIONS.md.

### Q5 — Interaction, accessibility and states

Use the existing dependency-free combobox pattern. Selecting replaces the sole director. `Clear Director` clears only directorId; Clear all includes directorId. Support Arrow-key navigation, Enter selection, Escape close, visible focus, associated label, and combobox/listbox/option semantics including selected and expanded state.

Loading and failures are local to the director control; other filters remain usable. Disable suggestions while loading/error, keep selection clearable, and offer Retry through existing query refetch on failure. Copy: `Loading directors`, `Could not load directors`, `Retry`. Palette provides responsive placement and visual details within these semantics before plan challenge.

### Q6 — Approach, files, task order and test strategy

Use only narrow changes to the existing architecture. No dependency, project, architectural layer, migration, schema or ingestion change, LocalPlay, secrets, Process.Start, tracked-file deletion or broad rewrite is authorized.

The following sequence is binding; Quill may divide it into phases without reversing dependencies:

1. Core value/query contract: `src/LamuFlix.Core/Library/MovieQuery.cs`, `MovieQueryString.cs`, `src/LamuFlix.Core/Ports/IMovieCatalog.cs`; add `src/LamuFlix.Core/Features/Library/GetDirectorsQuery.cs` and `GetDirectorsQueryHandler.cs` following existing facet handlers.
2. Persistence: `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` and `MovieRecordQueryExtensions.cs`; use existing Directors navigation and `Any` for WhereDirector. DirectorRecord, MovieRecord and the directors/movie_directors tables already exist according to intake recon; do not alter schema or metadata ingestion.
3. API: `src/LamuFlix.Api/Endpoints/FacetEndpoints.cs`, new `DirectorDto.cs`, `BrowseMoviesRequest.cs`, `LibraryEndpoints.cs`, and `src/LamuFlix.Api/HandlerRegistration.cs`. Reuse existing validation/problem handling and cancellation conventions; success/invalid-input statuses are specified above.
4. Contract: generate `web/src/api/openapi.json` from C# then `web/src/api/types.ts`; update generated mocks through the established generation path and `web/src/api/client.typecheck.ts` coverage. Never hand-edit generated contract files. Consume generated DirectorDto and browse-parameter types; no duplicate web DTO or cast workaround for the new contract.
5. Web: `web/src/features/library/filterQuery.ts`, `FilterPanel.tsx`, `web/src/library/browseParams.ts`, `LibraryView.tsx`, and narrow query wiring in `libraryQueries.ts` if required by the existing browse/facet path. Wire both hosts, independent facet state and approved interactions. Use existing query/cache architecture, without a new state layer.
6. Matching tests and test fakes forced by the added IMovieCatalog member. Do not change genre/cast behavior as part of these edits.

Tests must prove behavior, including common paths:

- Real PostgreSQL/Testcontainers facet and browse tests: orphan exclusion, empty list, distinct counts for shared movies, same-name identities, deterministic order, null passthrough, matching and unmatched director, AND composition including watchlist, no duplicate movie rows. Do not mock query translation.
- Core tests: equality/hash difference for DirectorId, null/default behavior, parse/format roundtrips including seeded property roundtrips, empty/valid/duplicate/malformed/overflowing values, unknown identities without extra validator rules.
- API integration: facet field set/types and empty array; no required role; browse filter and 422 failures. Follow existing FacetEndpointsTests, EfMovieCatalogFacetsTests, EfMovieCatalogBrowseTests, LibraryEndpointsTests, MovieQueryTests and MovieQueryStringTests patterns. Update existing catalog test fakes only as compilation requires; add handler coverage following existing facet handlers.
- Web Vitest/RTL: raw scalar and first-wins behavior, empty omission and API forwarding, generated contract/typecheck, URL reload/navigation, page reset with other filters preserved, selection/replacement/clear/Clear all, same-name identities/counts, typing without predicate changes, keyboard/ARIA, missing selection, independent loading/error/retry and both Library/Watchlist hosts. Extend FilterPanel.test.tsx, filterQuery.test.ts and browseWatchlist.test.tsx following existing conventions.

### Q7 — Loop discipline

- Closing bar: Critical, High and Medium findings block code-review/ship-review closure. Low findings are recorded and nonblocking. Do not waive or relabel unresolved blockers to close a round.
- Round cap: two review rounds maximum; at most two fix commits per round. A third-round unresolved above-bar finding goes to user review on the PR and stays blocked. The cap is not permission for a third review round.
- All three Sentry, Ledger and Compass axis reports are required for M review/adjudication. A missing axis is blocked, never skipped.
- Frozen scope: only Q1–Q6 Director facet and single-select Director filter in Library/Watchlist, necessary existing-chain API/URL/generated-contract/test edits, and approved accessibility and local states. Exclude multi-director selection, genre/cast behavior changes, broad parser cleanup, ingestion/schema/packages/layers and unrelated fixes; anything else is a follow-up issue, not a finding in this round.
- Follow-up handling obeys charter section 2.3: Patron decides, Rigger records; minor observations are `noted, no ticket`. Ticket contradictions and necessary constitution departures alone become structural owner checkboxes; none has been identified in Q1–Q7.

## Gate expectations and handoff

This documentation-only Phase A ask runs no production/test gates and claims no implementation verification. Recorded clean baseline counts in DEV-425 are measurements at the intake base, not future gate results.

Later Build runs the configured Roslyn, complexity, InspectCode, mutation, property-test, vulnerable-package, format, full dotnet-test and web gates according to task-pipeline. Use configured thresholds; never lower them. Property roundtrips are in scope, with no preapproved property-test opt-out. Mutation uses run-mutation.ps1, never Stryker since from a worktree. Exit 1 and Could not run block; scope-empty exit-2 is nonblocking SKIPPED (scope-empty), never PASS; explicit mutation exclusions are N/A and disabled gates SKIP, never PASS. Generated API drift checking is required.

Quill drafts spec.md, plan.md and tasks.md from this brief and evidence; a missing decision returns through Keel to Patron. Current task-pipeline assigns clarify/checklist to L, not this M ticket; the older intake-note line saying M includes them is stale. Keel later analyzes read-only. Palette design.md must exist before plan challenge. Conductor requests all three challenge axes. No ADR is required for this M ticket without a new irreversible architecture decision.

### Q8 — Closure

Patron reviewed the four brief artifacts and confirmed shared understanding and faithful transcription of Q1–Q7, with no unresolved structural owner checkbox. The full closure exchange and cited basis are appended in CONCLUSIONS.md. Eight questions were used; Q2 resumed after bounded recon without consuming a new question.

The settled vocabulary and decisions are ready to feed speckit-specify and Palette design. Spec and plan must not contradict them. Gate 1 remains closed until the user merges the later spec PR. This brief does not freeze an unwritten plan, clear plan challenge, authorize Build or imply merge approval.

## Plan challenge clarification PC1 — Patron ruling, Phase 2 step 6

This append-only clarification completes Q6 test-file scope and executable ordering; Q1-Q7 delivery remains unchanged. Full cited Patron ruling is in CONCLUSIONS.md, Plan challenge decision PC1.

- Q6 matching tests explicitly include `tests/LamuFlix.UnitTests/Library/MovieQueryFixture.cs` and `MovieQueryGeneratorTests.cs` so generators emit non-null DirectorId. `MovieQuerySurvivorTests.cs` and `MovieQueryStringSurvivorTests.cs` in that directory are allowed only if needed for non-null DirectorId survivor paths.
- Add `tests/LamuFlix.UnitTests/Features/Library/GetDirectorsQueryHandlerTests.cs`. `tests/LamuFlix.Test/AddHandlerTests.cs` is allowed only if needed to assert existing registration; otherwise leave it untouched. `BrowseMoviesQueryHandlerTests.cs` has no additional work authorized merely by mention in Ledger M2.
- T041 concretely creates `web/src/library/browseParams.test.ts`, testing raw nonempty scalars including malformed `abc` and fractional `12.5`, with empty omission. T040 also proves malformed raw scalar serialization survives a commit cycle. Do not use numeric formatting fallbacks that turn a malformed directorId into empty.
- Combined Core+Persistence Phase 1 remains one Build ask. After T001/T002, Core dependency order is T003, T005, T004, T006; preserve IDs, not numeric execution order. Compile-failure red while introducing required symbols/port is allowed; after compile closure require behavioral red evidence before the one green checkpoint. No placeholder/default-throwing implementation.
- Q2 API pin: singular `string? DirectorId` on BrowseMoviesRequest; use the existing ReadNumber empty-means-null mode and reject duplicate values from raw IQueryCollection counts, before model-bound scalar information can hide them. Absent/empty is null; duplicate/malformed/overflowing is 422. C# parser follows Q2.
- Q5 Arrow navigation remains as settled; remove the unapproved wrap-at-ends design claim. No wrap behavior is added and no new taste assumption is recorded.
- Closed combobox retains `aria-expanded=false`; `aria-controls` exists only when its listbox is mounted; `aria-activedescendant` exists only when the active descendant exists. Tests query the input as combobox.
- Design component names are a conceptual tree inside FilterPanel.tsx; no extracted files or new abstraction are mandated or authorized.

No delivery change, new dependency/layer/schema/API shape, constitution departure, owner checkbox, implementation authorization, or Gate 1 approval follows. Plan freeze requires separate saved fix read-back in PLAN_CHALLENGE.md.
