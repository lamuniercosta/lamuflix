# DEV-310 Phase A brief

Status: GRILL CLOSED. Patron confirmed shared understanding at Q10 with no correction after reading all artifacts. Quill Phase A drafting is now permitted. Gate 1 remains CLOSED for the Q6 owner checkbox; no implementation is authorized.

Worktree: F:/Dev/LamuFlix.worktrees/feature-310-spec
Branch: feature/310-spec
Evidence HEAD: 8fd9eb217f06ae995266055056f96c7daa5f87b3
Size: M. Phase: A (spec path). Delivery branch belongs to Phase B.
Facts: recon-DEV-310 sections 1-12; section 12 supersedes old ProcessMediaPlayerLauncher citations with DEV-394 fail-closed evidence. User instruction closes the .junie/mcp/mcp.json concern. No independent recon or production edits by Keel.

## Authority and accepted ticket scope

PRODUCT section 3 makes Scope & Technical Design authoritative. Patron deliberately rules every care-list item under PRODUCT section 5; only a change contradicting/changing ticket scope or a necessary constitution departure becomes an owner checkbox. Older charter/pipeline escalation wording does not override this rule. Full exchanges, cited bases, rationale, costs and implications are append-only in CONCLUSIONS.md Q1-Q10.

Retain ticket acceptance criteria verbatim:

- All endpoints conform to the route table in section 6 of architecture-plan.md.
- Endpoints return exact HTTP status codes and headers.

architecture-plan.md is absent (recon 11.1.4); ticket route table plus constitution 462-466 supplies the concrete contract. Record absent reference as traceability context only; no reconstruction, ticket rewrite or follow-up (Q1).

| Route | Request / dispatch | Success |
|---|---|---|
| POST /api/movies/import | ImportMovieRequest(string FolderPath); ImportMovieFolderCommand | empty 202, Location: /api/movies/{newId} |
| POST /api/movies/{id}/enrichment | RequestEnrichmentCommand only | empty 202, Location: /api/movies/{id} |
| POST /api/movies/{id}/watchlist | AddToWatchlistCommand | empty 204 |
| DELETE /api/movies/{id}/watchlist | RemoveFromWatchlistCommand | empty 204 |
| POST /api/movies/{id}/play | PlayMovieCommand | empty 204 on launch; 403 ProblemDetails when existing movie and LocalPlay disabled |
| GET /api/genres | GetGenresQuery | 200 array of GenreDto(int Id, string Name) |
| GET /api/people?role=actor | GetPeopleQuery; required single actor role | 200 array of PersonDto(int Id, string Name) |

JSON request uses {folderPath}; facet items use {id,name}; no envelope or extra fields. All seven routes use TypedResults and complete response/error metadata, per-feature MapXxxEndpoints grouping and existing decorated handler registration. No worker-side ProcessEnrichmentCommand endpoint, duplicate worker registration, RecordEnrichmentFailureCommand endpoint or stranded-sweeper endpoint.

## Confirmed grill answers and approach

Q1: Contract authority and verbatim ACs as above; no owner checkbox.

Q2: Extend existing IMovieCatalog with two read-only facet operations, sealed query records/handlers in existing Core/Features/Library, read-only projections in its existing Infrastructure adapter. No new port/project/top-level folder/layer/package/schema/migration, no architecture whitelist relaxation, no ADR needed. Accepted cost: two cohesive read methods enlarge the catalog. Tracing -> Logging -> Validation -> handler remains unchanged.

Q3: Facets return all stored entities distinct by Id, preserve distinct entities sharing a Name, return [] when empty; no pagination/search/movie-filter/extra fields or union of actor/director identities. People requires exactly one role value actor; missing/blank/duplicate/unsupported role (including director) returns 422 ProblemDetails through the single exception handler. Director support would add scope and is noted, no ticket. Name then Id sorting is [assumed] taste in ASSUMPTIONS.md. No owner checkbox.

Q4: Validate required/nonblank/no-traversal folder input with the LibraryPath guard before constructing the import command; movie ids bind as int then MovieId.TryCreate. Nonpositive ids and domain validation: 422. Malformed transport binding/body: framework 400 ProblemDetails. NotFound: 404. InvalidTransition: 409, including repeated watchlist add/remove; accepted cost is conflicts rather than invented idempotency. Unhandled: generic 500. Existing single ValidationExceptionHandler adds exactly InvalidTransitionException=409 and FeatureDisabledException=403, preserves existing 404/422 and generic 500. No exception text leaks. No owner checkbox.

Q5: Playback preserves existing lookup-before-launch precedence: nonpositive id 422; missing movie 404 regardless of flag; existing movie with flag off 403 via DisabledMediaPlayerLauncher; enabled successful launch 204; other launch/config failures generic 500 without raw text. Preserve DEV-394 configured-executable-first fail-closed unmapped-format behavior, no process start on unmapped format, ArgumentList and no concatenated Arguments, no OS-association fallback or new process path. No launcher/config/flag/registration edits; UseShellExecute=true remains baseline outside ticket scope. Tests use real flag registration with recording IProcessStarter or safe launcher substitute; never launch a real process. No owner checkbox.

Q6: blocked: structural - necessary constitution departure. DEV-310 supplies complete C# DTO/TypedResults metadata; committed OpenAPI document generation, Verify snapshot/drift and generated client work remain DEV-20. Contract flow remains C# DTO -> OpenAPI -> generated TypeScript; no handwritten web duplicates. No web/generation/package changes and no claim of artifact drift PASS. Full checkbox and condition below. Patron requires one Rigger-recorded ownership comment on existing DEV-20 naming these routes, metadata inputs and pending checkbox. Recording receipt is pending; a verified comment cannot close owner approval.

Q7: Bounded file envelope and ordering below; care-list item 6 approved with cited forced additive edits. No owner checkbox.

Q8: Test and gate strategy below confirmed; explicit property opt-out approved; no owner checkbox.

Q9: Loop discipline below confirmed; no owner checkbox.

## Files touched and ownership envelope (Phase B)

New files in src/LamuFlix.Api/Endpoints/: EnrichmentEndpoints, WatchlistEndpoints, PlaybackEndpoints, FacetEndpoints, plus ImportMovieRequest, GenreDto and PersonDto files. Fill existing ImportEndpoints.cs. Add feature-group wiring to ApiEndpoints.cs and facet decorated registrations to HandlerRegistration.cs. Add exactly 409/403 exception arms to ExceptionHandling/ValidationExceptionHandler.cs.

Extend src/LamuFlix.Core/Ports/IMovieCatalog.cs and its current Infrastructure implementation for facet reads, identified by existing interface ownership rather than a guessed filename. New GetGenresQuery/GetPeopleQuery sealed records and sealed handlers under Core/Features/Library; use existing catalog projection patterns. Do not invent an uncited adapter path; if task-level exact-path evidence is required, request it from Wisp.

New matching endpoint host tests in tests/LamuFlix.IntegrationTests/, facet handler tests in existing UnitTests/Features/Library, and real persistence facet tests in existing IntegrationTests. ApiHostFactory changes are test-only, bounded to required safe doubles/real Postgres wiring.

Preserve DEV-309 LibraryEndpoints/LibraryResponses, existing command handlers, Program.cs, architecture whitelist, migrations, playback adapters/config, web/dependencies and unrelated scripts. No deletion or wholesale rewriting. Anything outside this envelope needs recon or a fresh Patron ruling.

Phase B addendum (2026-10-06; recon sections 14-16; Patron CONCLUSIONS.md:280-286 and :290-296; Keel DEV-310:273-293 I1/I2): existing-test envelope extends to tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs restricted to RecordingMovieCatalog member parity (current lines 662-686) and tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs restricted to additive business-route and ManifestContracts updates (current lines 131-147 and 34-72). Frozen seven-route deliverable and all other envelope exclusions unchanged. This addendum supplements, and does not rewrite, the Phase A identity/owner-pending wording above.

Keel writes only prescribed spec decision artifacts. Quill owns spec.md, plan.md and tasks.md after closure. No production implementation is authorized in Phase A.

## Task ordering

1. Freeze scalar facet contract, extend existing catalog reads, implement facet queries/handlers/projections and focused tests; port/concrete-implementation parity (T002 port plus RecordingMovieCatalog parity with T005 adapter) completes together before the Phase 2 passing handoff.
2. Add command endpoint request validation/dispatch and exactly two error-mapping arms, preserving established handler semantics.
3. Wire feature groups and explicit decorated registrations; complete endpoint response/error metadata.
4. Prove all seven routes at the real host, and infrastructure-dependent behavior with real database/broker tests and safe process seams, including additive nine-business-route/nine-handler-contract composition proof in ApiHostCompositionTests.cs after T023 wiring.
5. Refactor the complete delivery diff then run full Phase B gates; no worker redesign or OpenAPI infrastructure implementation.

Quill splits implementation tasks into independently verifiable phases/checkpoints using this ordering. Phase B starts only after Gate 1 approval and performs pickup drift check against main; facts come from Wisp.

## Test strategy and gate expectations

WebApplicationFactory proof for all seven routes: ordinary success, exact statuses/Location/empty command bodies/scalar arrays, invalid folder/id/role, not found, repeated watchlist 409, disabled play 403 with no process call, enabled recording launch 204, generic fail-closed 500 without sensitive text. Preserve current DEV-394 unit proofs, no real process.

Real Postgres Testcontainers for facet translation, distinct Id/same-name preservation, ordering and empty tables. Real Postgres/RabbitMQ whenever import/manual-enrichment persistence/publication behavior is asserted. MockFileSystem for scanner. NSubstitute only for Core ports or safe process seam; no driver mocks, EF InMemory or new mocking/assertion library. Focused facet handler unit tests; retain existing command/worker tests, no duplicate retry/DLQ feature work.

propertyTests: opt-out - HTTP adapters and read projections introduce no new domain invariant; existing property suite retained. This line must also be recorded in the task note for pipeline acceptance of a no-tests-tagged exit 2.

Phase A: documentation consistency/analyze proof, not fabricated production gate PASS. Phase B: pipeline Roslyn, implement/refactor complexity (harness values), InspectCode on changed diff (never -All), vulnerability, format, full tests/property suite, mutation script for changed Core/Infrastructure. Mutation threshold remains harness-defined; API exclusions only when run-mutation.ps1 itself emits configured NOT APPLICABLE. No Stryker since from worktrees. No threshold lowering.

Report SKIPPED (scope-empty), SKIP (configured opt-out), NOT APPLICABLE (script-emitted) separately; none is PASS or counted as checked code. Could not run and gate failures block. No web changes; web gate uses actual harness/script disposition. OpenAPI drift is deferred-pending-owner, never green.

## Loop discipline

Closing bar for code-review/ship-review and plan challenge: no unresolved Critical/High. Critical/High deferral retains NEEDS FIXES. Medium closes by fix or explicit evidence-based not-a-defect adjudication; a real-but-deferred Medium needs a Rigger-recorded YouTrack follow-up ID before READY. No silent demotion. Ordinary Low is noted/no ticket. Gate failures, missing any mandatory Risk/Standards/Spec axis, unfulfilled Q6 owner checkbox and unacknowledged handoff block regardless of severity.

Frozen scope: exactly seven ticket routes, actor-only catalog facet reads, Q3-Q5 contracts, Q7 file envelope and Q8 proof; Q6 document/snapshot/drift deferred-pending-owner; no worker redesign, director support, frontend, schema/dependency/new layer or launcher changes; anything else is a follow-up issue, not a finding in this round.

Round cap: two plan-challenge/remediation rounds. Unresolved above-bar findings after cap are blocked with cited evidence; no lowered bar or indefinite loop. A scope/bar amendment after opening a loop is a new issue.

## Owner-only structural condition

- [ ] Approve DEV-310's committed OpenAPI document, Verify snapshot, and drift-check deferral to DEV-20 for the seven new endpoints (import, enrichment, watchlist add/remove, play, genres, people)?

Gate 1 remains CLOSED until owner answers this checkbox on the spec PR. Patron approval, earlier DEV-309 approval, verified tracker comment, clean analyze or clean review cannot close it. Owner rejection requires reconciliation of ticket/scope before bringing document work into DEV-310. Carry exact checkbox into the spec PR; do not ask owner in chat.

## Vocabulary and ADR disposition

Use existing canonical Movie, Library, Import, Enrichment, Watchlist and Playback terms (constitution VIII in recon 11.3). Genre/Person DTO names are ticket-decided. No new domain vocabulary, port or architectural trade-off requiring ADR is introduced by these rulings; no shared glossary/ADR rewrite is authorized. Existing vocabulary and decisions are settled and ready to feed Quill Phase A Speckit drafting. Closure confirmed in CONCLUSIONS.md Q10; drafting must preserve Q6 owner condition.


## Q10 closure receipt

Patron read all decision artifacts and confirmed shared understanding with no correction. Grill closed; Quill may draft spec.md, plan.md and tasks.md from this brief. No implementation or owner-checkbox closure. DEV-20 ownership comment remains pending Patron/Rigger dispatch; no verified tracker receipt exists. Property-test opt-out is recorded in the DEV-310 canvas task note. Full exchange and closure: CONCLUSIONS.md Q10.

