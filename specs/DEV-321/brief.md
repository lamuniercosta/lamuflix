# DEV-321 — Frozen Library browse brief

## Status and authority

Phase A, Phase 2 step 1: grill closed by Patron at **Q10/12**. This is the decision-complete input to Quill, not Gate 1 clearance or implementation authorization. Full exchanges and cited rulings are append-only in `CONCLUSIONS.md`; taste decisions are tagged `[assumed]` in `ASSUMPTIONS.md`. No Spec Kit, ADR, production edits or gates were performed in this ask.

- Spec worktree: `F:/Dev/LamuFlix.worktrees/feature-321-spec`, branch `feature/321-spec`.
- Authorized entry HEAD for Q9: `756c5f36ce980f20f0031254bdcd6eb9bc883863`; Patron Q10 conclusion HEAD: `a6fd0dd7dbdfcd1e9cce671a91d890168d3e03b2`.
- Preserve the existing `.specify/feature.json` change pinning `specs/DEV-321`. Do not work in the main checkout.
- Task: DEV-321, parent DEV-286, `size:L`, `ui:`. Phase B worktree has not been created.
- Facts: `recon-DEV-321`, especially sections 9–15. Prior recon claims that enrichment populates posters are corrected by section 11; do not carry them forward.
- Ticket authority: original full-body receipt `DEV-321:15-31`, originally scope lines 24–27 and AC 30–31. At this read-back, note additions moved the same body to `DEV-321:19-35`: overview 25, grid 28, URL synchronization 29, TanStack Query 30, tests 31, AC 34–35. These references identify the same ticket text, not a ticket amendment.
- Charter basis: `lamuflix-team-charter:9-21` (§2.3 Patron decisions and the two owner escalations), :24 (C# DTO → OpenAPI → generated TS), :17 (forced edits), :25 (worktree boundary), :29–31 (posted reviews, no merges, Rigger records tracker decisions). Pipeline basis: `task-pipeline:25-27`, :66–82, :97–118.

No owner structural checkbox is identified; no constitution departure is approved (Q10). TanStack Query itself is already authoritative ticket scope, originally DEV-321:26, currently :30; it is not a new-dependency owner checkbox.

## Loop discipline — Q10

**Closing bar:** Critical and High findings block `$code-review` and `$ship-review`. Medium and Low findings are recorded and handled under the charter follow-up rules. A failed or unrunnable applicable gate, missing Sentry/Ledger/Compass axis report, or API/OpenAPI/TS contract mismatch blocks independently of severity. An unnecessary abstraction is follow-up work, never a fix commit.

**Frozen scope:** the ticket's main responsive Library browse grid with poster, title, year, runtime, IMDb rating and enrichment status; all existing canonical filter/page URL synchronization with approved aliases/defaults; TanStack fetching, skeletons, error recovery and history restoration; the additive same-row browse contract and generated contract chain; and the bounded test and forced-edit envelope in Q8/Q9. anything else is a follow-up issue, not a finding in this round.

**Round caps:** two plan/draft correction rounds; two formal review/remediation rounds; at most two fix commits per remediation round. At the cap, unresolved blockers go to the user on the PR, without a waiver or third round. The grill used ten questions of twelve and is closed.

## Q1 — Additive browse contract

Patron APPROVED amended under §2.3 item 4. Widen `MovieSummary` → `MovieSummaryResponse` on existing `GET /api/movies`, then regenerate OpenAPI and TypeScript. Preserve `id`, `title`, `items`, `totalCount`, route, query parameter contract and status codes. No per-tile details requests, migration or new join.

| Wire member | Contract and source |
| --- | --- |
| `posterUrl` | Nullable string, stored `MovieRecord.PosterUrl` passed through unchanged |
| `releaseYear` | Nullable CLR `int`, calendar year, same name/type/units as details metadata |
| `runtime` | Nullable CLR `int`, minutes, same name/type/units as details metadata |
| `imdbRating` | Nullable CLR `decimal`, 0–10 with at most one decimal place, same semantics as details metadata |
| `enrichmentStatus` | Required existing `EnrichmentStatus`, wire string name exactly `Pending`, `Enriched`, `NotFound`, `Failed` |

The existing global status converter emits names; do not add or change global serialization. Require OpenAPI to describe the four string values and generated TS to carry that contract. Metadata schema nullability/number handling follows existing generated metadata schemas, not a hand-written TS simplification. Null scalar members stay present as JSON null.

Q1's factual conditions are resolved by recon:60–73: metadata names/CLR types/units, string converter, and all five added scalars already on the same movies row. If implementation needs a migration or new join, stop and re-ask Patron. Basis: ticket grid item, charter public-contract and contract-chain rules; `CONCLUSIONS.md` Q1.

## Q2 — URL vocabulary and history mechanism

Patron APPROVED native `URLSearchParams`, `history.pushState`, `history.replaceState` and `popstate`. Add no router, state or validation dependency. The small feature URL adapter is the sole owner of history and URL state. All normalized filter values and page participate in state restoration and the query key.

Canonical keys: `text`, `genreIds`, `actorIds`, `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown`, `yearMin`, `yearMax`, `statuses`, `inWatchlist`, `sort`, `direction`, `page`, `pageSize` (recon:33,80).

- Sort names: `Title`, `Year`, `Rating`, `Runtime`; direction names: `Ascending`, `Descending`.
- Read-only aliases: `genres` → `genreIds`; `Asc`/`Desc` → `Ascending`/`Descending`. `genreIds` wins when both genre keys occur. Never emit aliases; normalize with `replaceState`. No other aliases.
- Always emit `sort`, `direction`, `page`, `pageSize`. Honor a supplied URL pageSize; offer no size selector.
- User changes push a history entry and immediately update adapter state. Initialization, alias/default/malformed normalization replace the current entry. Back/forward restores all controls and query state without a push. Remove the listener on cleanup.
- Filter/sort edits reset page to 1; pagination changes page alone.
- Drop unknown keys. Unparsable values normalize to defaults; valid but out-of-range or cross-field-invalid values remain server validation's responsibility and can produce the error boundary's 422 state. Do not duplicate server validation in the SPA.

Map canonical lowercase keys to the existing PascalCase generated API query parameters through a typed adapter. Do not change the server query parameter contract. Basis: ticket URL item and bookmarked-URL AC, recon:32–35, charter §2.3 item 1 and contract chain.

## Q3 — Usable SPA defaults and codec details

Patron APPROVED SPA product defaults `sort=Title`, `direction=Ascending`, `page=1`, `pageSize=24`; text/watchlist/runtime/year absent and lists empty. This supersedes Q2's initial instruction to copy Core construction defaults: Core has null sort/direction and page 0/0, which cannot form a canonical browse URL (recon:82–117). Q3's validator condition is closed: page numbers are >=1, no upper bound; sizes are 1–100 inclusive, so 1/24 is valid (recon:119–124, Q4 receipt).

- Lists accumulate and emit repeated keys, one value per key; no comma serialization assumption.
- Empty runtime/year bound means unbounded. Read incomplete groups leniently, materializing missing bounds as empty and missing runtimeIncludeUnknown as false if any runtime key exists. Emit runtime's three keys or year's two keys as complete groups; otherwise omit the group.
- Malformed/repeated scalar falls to its default; a malformed/repeated range key drops the whole group. Enums are exact-case except the Q2 direction aliases. Read booleans case-insensitively, emit lowercase true/false.
- Encode keys/values and join pairs with ampersands; test reserved characters and parity with server codec fixtures. Text empty-string remains a distinct representable value; null text is absent.

Basis: recon:99–117 codec semantics, ticket bookmarked URL AC and Q3 cited Patron ruling. Defaults are taste, logged `[assumed]`.

## Q4 — Poster availability boundary

Patron APPROVED supplied-poster display, with a title-labelled placeholder at the same reserved aspect ratio for null or image-load error. Prove populated and null posters through seeded real-DB rows, and populated/null/broken images in RTL.

Poster storage/mapping is verified; runtime poster population in the shipped stack is not established (recon:75,159). Do not claim that live enrichment currently produces posters. Do not repair enrichment or the orphaned worker in DEV-321. Poster persistence is the separate Patron-authorized follow-up **DEV-408**, reported created/read back by Rigger in task note DEV-321:13; genre/actor picker improvement is `noted, no ticket`. Neither expands this ticket or waives its supplied-poster behavior. Basis: ticket grid requirement, Q1 nullable passthrough, charter follow-up rule, Q4/Q10.

## Q5 — App integration and DEV-320 timing

Patron APPROVED replacing the readiness `App.tsx` and its orphaned readiness test in the same delivery. This is forced by the ticket overview's main Library view. Install one QueryClientProvider in `main.tsx`; style the responsive view in `index.css`.

Only `@tanstack/react-query` is newly added by DEV-321, already ticket-named. Write the React error boundary without another package. DEV-320's generator/client dependencies are inherited.

Phase A may finish before DEV-320. **Phase B waits for the user's merge of DEV-320 #124 and Wisp current-head recon of generated `types.ts`, `client.ts` and abort-signal support.** The Conductor records this as a code prerequisite. No copying unmerged code, duplicate hand-written DTOs, hand-edited generated files or replacement client. If the merged client conflicts with this approach or lacks signal support, re-ask Patron. The contract chain is C# first → generated OpenAPI → generated types using the merged pipeline. Basis: recon:35,38–41 and charter §2.3/contract chain.

## Q6 — Fetching, error boundary and restoration ownership

Patron APPROVED one browse query keyed by complete normalized filter/page state, using the merged generated client with cancellation propagated. No per-tile fetches or prefetch.

- Any normalized-key change shows reserved-size skeletons, never previous-key placeholder data. Same-key background refetch retains current content and announces loading politely.
- The hand-written Library boundary handles render/query failures, resets on query-key change, and offers Retry for recoverable fetch errors. Retry resets/refetches the same canonical URL without a history entry. Automatic query retries are off. A server 422 is an invalid-filter error and is never retried; other failures, including 400, show recoverable fetch error without a retry loop. Choose the state by status, not by the field-key vocabulary.
- URL adapter owns initial bookmark read, push/replace/popstate and listener cleanup. App composes providers/boundary; LibraryView owns controls/grid/pagination.

Basis: ticket TanStack/skeleton/error-boundary and history items, recon:38, Q5 prerequisite, Q6.

Post-grill factual correction: Patron's ruling in `CONCLUSIONS.md:298-315` supersedes the earlier 400 invalid-filter wording in Q2/Q3/Q6. Constitution V and recon-DEV-321 section 16 establish 422 for malformed, out-of-range and cross-field-invalid browse filters. Existing endpoint status codes, server-owned validation and history behavior are preserved; no ticket change or constitution departure. Ruling committed at `8d5ab9bc554e4cee7df0cb37c26f3566f3de6310`, directly after the Q10 HEAD.

## Q7 — UI and control behavior

Patron APPROVED browse at the current SPA root. Responsive CSS poster grid reserves 2:3 media slots, shared by images, placeholders and skeletons. Each tile shows title, year, runtime as `<n> min`, IMDb badge with one decimal, and exact enrichment-status chip labels. Missing scalar metadata uses an em dash. No detail/play/watchlist mutation controls; LocalPlay remains untouched.

Every codec key is restorable in its corresponding control. Text submits on Enter/Search; sort/direction selects; status multiselect; watchlist tri-state; optional runtime/year bounds and include-unknown; genre/actor numeric-ID entry for repeated IDs. Apply filters together with page reset. No new facet endpoint or picker dependency.

Previous/Next pagination uses `ceil(totalCount/pageSize)`, current page and result count; disable movement past known bounds. Empty-library/filtered-empty states explain the state and offer clear filters. A valid page beyond available results stays in the URL and shows empty content with a return-to-first-page action, without silent rewrite. Label controls, support keyboard/focus-visible behavior and polite loading/status announcements. No numeric performance target is invented; Palette later verifies visual layout. Basis: ticket grid/URL/test items and both AC, Q7; taste in ASSUMPTIONS.

## Q8/Q9 — Exact file envelope and test strategy

Existing production targets:

- `src/LamuFlix.Core/Ports/MovieSummary.cs`: widen the port record.
- `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`: project the existing same-row grid scalars.
- `src/LamuFlix.Api/Endpoints/LibraryResponses.cs`: widen browse DTO and mapping.
- Conditional `src/LamuFlix.Api/Program.cs`: only an EnrichmentStatus-scoped OpenAPI schema transformer if regenerated output lacks a string schema with all four names. No global serializer changes. Runtime converter and schema generation are separate; recon:148–154 identifies the customization point and absence of an existing schema transformer.

Existing backend tests to extend:

| File | Required change/proof |
| --- | --- |
| `tests/LamuFlix.UnitTests/Features/Library/BrowseMoviesQueryHandlerTests.cs` | Compile-forced summary construction, preserve unchanged-query/returned-page proof |
| `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` | Compile-forced fake construction; widen exact frozen wire shape; populated/null values and status string assertions |
| `tests/LamuFlix.IntegrationTests/ApiEndToEndBrowseTests.cs` | Widen exact item assertions; seeded full-stack grid scalar/status proof, populated/null poster and metadata |
| `tests/LamuFlix.IntegrationTests/EfMovieCatalogBrowseTests.cs` | Real Postgres projection proof for all four statuses and populated/null grid scalars |
| `tests/LamuFlix.IntegrationTests/EfMovieCatalogPagingTests.cs` | Replace id/title-only SQL expectation with grid summary columns; retain no JOIN and empty ChangeTracker assertions |
| `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` | Generated browse schema/status/nullability proof and baseline/snapshot drift |

`ApiEndToEndLibraryTests.cs` is a read-only pattern, not an edit target. Existing fixtures/seeds may be reused; seed PosterUrl directly in the named tests. No fixture/library/package change is silently authorized. Recon:130–145 identifies all constructor sites: production browse plus the handler expectation and RecordingMovieCatalog fake. Other type usages do not justify edits. Compile-forced construction changes are permitted; unrelated unnamed files are not.

Regenerate `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together through existing `LAMUFLIX_REGENERATE_OPENAPI=true` opt-in (recon:137), then generated `web/src/api/types.ts` through DEV-320's merged tooling. Never edit generated files by hand. Assert exact status string schema/names before considering the conditional transformer unnecessary.

Existing web targets: `web/src/App.tsx`, `main.tsx`, `index.css`, `App.test.tsx`, `web/package.json` and its package-manager lockfile. Reuse `web/src/test/server.ts` and `setup.ts` for existing MSW/RTL setup; add feature fixtures/handlers in tests, preserving the synthetic harness test behavior. `web/src/api/client.ts` is consumed after DEV-320, not a new client implementation.

Approved new feature-local files under `web/src/library/`: `LibraryView.tsx`, `MovieGrid.tsx`, `MovieCard.tsx`, `LibraryFilters.tsx`, `Pagination.tsx`, `LibraryErrorBoundary.tsx`, `useBrowseMovies.ts`, `libraryUrl.ts`, `useLibraryUrl.ts`, plus focused adjacent tests (`.test.ts` for adapters/hooks or `.test.tsx` for rendered behavior). This is a feature folder, not a new top-level folder or generic architectural layer.

Backend tests use real Testcontainers Postgres for translation/projection/serialization behavior, existing assertion and mocking conventions, no new test package. Web Vitest/RTL tests cover grid/status/nullable/broken poster, responsive structure, skeletons and same-key refetch, 422 invalid-filter without retry, other-error retry/reset, status-based classification independent of field-key vocabulary, pagination first/last/empty/past-end, every URL key, aliases/precedence/defaults/groups/malformed/repeated values/encoding, bookmark/back-forward, cancellation/stale response protection and listener cleanup. Use table and generated cases for adapter round trips without a new property-testing dependency.

Q9 amendment: C# MovieQueryString is unchanged, so no new C# URL round-trip property tests. The C# property gate covers invariants the changed C# introduces; if none exist, record the property-test opt-out with that reason. Never infer opt-out from an exit code alone.

## Q9 — Gate expectations and task ordering

Thresholds come only from `harness.yml`; do not hardcode/lower thresholds or edit generated gate configuration. Gauge performs applicable diff-scoped gates:

- Roslyn analyzers, cyclomatic complexity, InspectCode.
- Mutation because production C# changes; `scripts/run-mutation.ps1`, no `since` in worktrees; no threshold waiver.
- Property tests under the Q9 amendment, vulnerable packages, format verification, `dotnet test`.
- `scripts/run-web-gates.ps1` for web and contract drift/generated output checks.

Exit-1 failure or Could-not-run applicable gate blocks. Scope-empty exit-2 is non-blocking `SKIPPED (scope-empty)`, never PASS; configured disabled gates are SKIP; mutation N/A only from the script's explicit exclusions; property opt-out must be recorded. No gate receipt is claimed by this brief.

Phase A order: this brief → Quill spec/plan/tasks (L clarification/checklist) → Keel read-only analysis against brief → Palette design before plan challenge → Keel L ADR in a later ask → all three plan axes/adjudication → provisional spec-PR handoff → user-merged spec PR (Gate 1). Completing the grill does not close Gate 1. Palette may refine presentation within frozen behavior; product/scope changes return to Patron.

Phase B order:

1. User-merged DEV-321 spec PR and DEV-320 #124 prerequisite; Wisp recon merged types/client/signal support and changed-file drift; Keel drift analysis against main; Gauge baseline.
2. Widen port/projection/DTO and forced constructors with real-DB and wire tests.
3. Regenerate OpenAPI/baseline/derived snapshot; add the narrowly scoped status schema transformer only if needed; regenerate generated TypeScript.
4. URL adapter and round-trip/history tests; normalized state-to-generated-parameter mapping.
5. Query/boundary/provider integration, skeleton/error/cancellation tests.
6. Responsive grid, controls, pagination and integrated bookmark/back-forward behavior; all web work through `web-implement` TDD.
7. Cog refactor; Gauge complete mixed backend/web gates; all three review axes and Keel adjudication under Q10; bounded remediation; L ship review/rebased gates; PR and exact-head merge bar; user merge only.

Anvil creates new M/L code; Cog alters existing code and refactors/fixes under Conductor assignment. Each implementation phase follows the pipeline commit/handoff checkpoint and must not silently proceed to the next ask's phase. No seat merges or enables auto-merge. End delivery at `awaiting-merge` until user-owned merge reconciliation.

## Decision receipt and handoff

All ten grill answers are incorporated above: Q1 contract; Q2 URL/history/dependencies; Q3 defaults and codec normalization; Q4 posters/follow-up; Q5 App and DEV-320; Q6 fetching/error/history owners; Q7 controls/UI; Q8 exact envelope/tests; Q9 gates/order and property amendment; Q10 closing bar/scope/caps. Historical needs-recon conditions for metadata, serialization, storage, usable defaults, validator bounds and exact backend file binding are resolved by recon sections 9–15 and the cited conclusions.

The intentional Phase B prerequisite is still the user merge of DEV-320 #124 plus generated-client/type/signal recon; it is not a hand-written-contract exception. The next authorized workflow is Quill drafting from this brief when the Conductor assigns it. Spec Kit, design, ADR, plan challenge, Gate 1 and implementation remain later stages.
