# DEV-310 grill conclusions

Phase A; feature/310-spec at 8fd9eb217f06ae995266055056f96c7daa5f87b3. Facts: recon-DEV-310 including sections 11 and 12. Patron decides; Keel records. Only ticket-scope changes or necessary constitution departures become owner checkboxes. No Speckit drafting before the closed brief.

## Q1 - Contract authority with an absent architecture-plan.md

Keel question: The ticket explicitly fixes seven routes, DTOs and statuses (recon 11.1.2); its first AC refers to architecture-plan.md section 6, but recon 11.1.4 proves the file absent. I recommend retaining both ACs verbatim, using the explicit ticket route table and constitution API rules as the concrete contract, and recording the absent reference as traceability context rather than reconstructing or changing delivery scope. No new architecture-plan file or ticket rewrite. Does this resolve authority without an owner checkbox, or do you judge a ticket change necessary? Give a cited ruling, rationale and implications.

Patron reply: approved as recommended, no owner checkbox, no ticket change; ACs retained verbatim, ticket route table plus constitution lines 462-466 is the contract, absent architecture-plan.md recorded as traceability context, noted no ticket.

Full cited ruling follows:
# DEV-310 grill Q1 — Patron ruling (architecture-plan.md absent)

**Verdict: Approved as recommended. No owner checkbox, no ticket change.** Retain both ACs verbatim; the ticket's own route table plus the constitution's quoted architecture-plan §6 substance is the concrete contract; the absent file is recorded as traceability context only.

## Basis

- Ticket text is decided. The Scope & Technical Design explicitly names all seven routes, DTOs, and statuses (recon 11.1.2), and PRODUCT.md §3 (lines 22-24) makes a ticket's Scope & Technical Design authoritative — an owner decision already made. The deliverable is fully determined without architecture-plan.md.
- The referenced content survives. The constitution's API and Contract Rules (lines 462-466) quote the §6 endpoint-table substance verbatim — 202 with Location for import and enrichment, 204 for watchlist and play, 403 when LocalPlay is off — identical to the ticket table, and the constitution prevails over any other document (preamble, lines 511-514). AC1 is therefore verifiable against the constitution; AC2 against the ticket table.
- Every alternative escalates for nothing. Rewriting or dropping AC1, or reconstructing architecture-plan.md, would change ticket text (a §2.3a owner checkbox) or create an artifact the ticket does not name. Keeping the ACs verbatim changes nothing the ticket delivers.

## Implications

- spec.md spells out the seven routes with exact statuses and headers from the ticket table; any route, DTO field, or status beyond it needs a fresh §2.3 item-4 ruling.
- CONCLUSIONS.md records: architecture-plan.md absent from the worktree (recon 11.1.4: recursive filter and glob both empty); authority resolved via the ticket table plus constitution lines 462-466. The missing document is a documentation gap, not Critical/High broken behaviour: noted, no ticket.
- No new architecture-plan file, no ticket rewrite; Gate 1 carries no owner checkbox for this question.


---

## Q2 - Facet architecture and PRODUCT care-list items 1-3

Keel question: Extend existing IMovieCatalog with two facet reads; sealed query records and handlers in Core Features/Library; read-only projections in its existing adapter over existing genre/actor/director tables. No new port/project/layer/package/migration/ArchitectureTests whitelist relaxation. Ticket DTOs remain GenreDto(int Id,string Name) and PersonDto(int Id,string Name). Cost: two related read methods enlarge catalog. Approve this architecture and explicit no-dependency/no-schema-change rulings, or specify a different justified decision.

Patron reply: approved as recommended; extend IMovieCatalog with two facet reads, sealed queries and handlers in Features/Library, read-only projections in existing adapter; section 5 items 1-3 ruled no-change with cited basis.

Full cited ruling:
# DEV-310 grill Q2 — Patron ruling (facet architecture; PRODUCT §5 items 1-3)

**Verdict: Approved as recommended.** Extend the existing IMovieCatalog with the two facet read operations; sealed facet query records and sealed handlers in the existing Core Features/Library slice; read-only projections in the existing catalog adapter over the existing genre/actor/director tables. Rulings under PRODUCT.md §5: item 1 dependencies — no change; item 2 architecture — no new project, folder, or layer; item 3 schema — no change. The cost (two related read methods enlarging the catalog) is accepted.

## Basis

- Port: IMovieCatalog is already on the constitution Principle I port whitelist (recon 11.3, lines 115-118). Adding read operations to a whitelisted port is not adding a port, so no ADR is required. Facet lists are catalog reads, cohesive with the existing GetDetailsAsync on the same port (recon 3.4).
- §5 item 1 (dependencies) and item 3 (schema): recon §2.1 shows the Directory.Packages.props pins cover everything facet reads need, and recon §2.3 shows the movie_genres / movie_actors / movie_directors skip-navigation tables already exist (MovieConfiguration.cs:140-160, Initial migration). No package, no migration.
- §5 item 2 (architecture): queries and handlers land in the existing Features/Library slice as sealed records and sealed handlers per constitution Principle II (recon 11.3, lines 131-136). No repository, mediator, or services wrapper is introduced; recon 3.5 confirms no facet implementation exists today, so this is additive, not a rewrite.

## Constraints before API details

- The two new IMovieCatalog operations are read-only projections over the existing genre/actor/director tables; no write path, no migration, no new port (a new port would require an ADR and a fresh ruling).
- Decorator order stays Tracing -> Logging -> Validation -> handler (constitution II, recon 11.3 lines 131-136).
- API DTOs are limited to the ticket scalar fields — GenreDto(int Id, string Name) and PersonDto(int Id, string Name) (recon 11.1.2). Any field, route, or status beyond the ticket table needs a fresh §5 item-4 ruling.


---

## Q3 - Facet public contract

Keel question: GET /api/genres returns 200 arrays of distinct stored GenreDto {id,name}; people requires one role value actor or director with 422 ProblemDetails for missing, blank, duplicate or unsupported role, separate role tables, distinct by Id, all stored rows, empty [], no pagination/search/extra fields/movie-filter; Name then Id sorting tagged [assumed]. Ticket names actor only: if director adds scope choose actor-only or raise an owner checkbox. Rule role domain, duplicate policy, membership, status and scalar shape.

Patron reply: approved with actor-only role per ticket; director would add scope and is noted, no ticket; no owner checkbox. Remaining recommendation approved.

Full cited ruling:
# DEV-310 grill Q3 — Patron ruling (facet public contract; PRODUCT §5 item 4)

**Verdict: Approved as recommended, with the role domain fixed at actor-only.** GET /api/genres returns 200 with a JSON array of distinct stored GenreDto {id, name}; GET /api/people requires exactly one role query value — actor — returning 200 with a JSON array of PersonDto {id, name}; missing, blank, duplicate, or unsupported role (director included) returns 422 ProblemDetails; empty result returns 200 with []; distinct by entity Id; all stored rows, no movie-filter parameter; sort by Name then Id tagged [assumed] taste. No owner checkbox.

## Basis

- Role domain: the ticket names GET /api/people?role=actor and nothing else (recon 11.1.2), and ticket text is authoritative — anything not named is not approved (PRODUCT.md §3, lines 22-24). Supporting director would add delivery scope the ticket does not name, which is a §2.3a owner checkbox; it is not justified to hold Gate 1 for an unnamed behavior when the parameterized route keeps the extension cheap later. Director support: noted, no ticket (not Critical/High broken behaviour).
- Statuses and error shape: 200 is the success status for these GETs under ticket AC2 (exact status codes, recon 11.1.3); 422 for role-validation failures is the constitution Principle V mapping ValidationException -> 422 (recon 11.3, lines 205-211), and all error paths produce ProblemDetails per the PR quality gates (recon 11.3, line 63).
- Shape and membership: the ticket fixes the scalar DTOs GenreDto(int Id, string Name) and PersonDto(int Id, string Name) (recon 11.1.2); distinct list means distinct stored entities, so dedup by Id and never union roles — overlapping ids across actor/director tables must never merge (recon 3.5: separate tables). No pagination, search, extra fields, or movie-filter: any of those is a public API shape the ticket does not specify and needs a fresh §5 item-4 ruling.

## Implications

- spec.md fixes the facet contract exactly as above; the role parameter is validated as a single required value with domain {actor}, and 422 responses carry ProblemDetails from the single IExceptionHandler path.
- ASSUMPTIONS.md logs the sort order (Name, then Id) tagged [assumed].
- CONCLUSIONS.md records: role domain actor-only per ticket text; director support noted, no ticket; no owner checkbox raised for this question.


---

## Q4 - Command endpoint input, success and errors

Keel question: Import accepts {folderPath}, validates LibraryPath guard, dispatches ImportMovieFolderCommand and returns empty 202 with Location /api/movies/{newId}. Enrichment dispatches RequestEnrichmentCommand only and returns empty 202 with Location /api/movies/{id}. Watchlist add/remove return empty 204 and preserve repeated-transition 409. Integer ids use MovieId.TryCreate; nonpositive 422, malformed transport binding/body framework 400 ProblemDetails, domain validation 422. Existing single exception handler adds 409 and 403, retains 404/422/500, no exception-text leakage. Approve public contract; playback execution next.

Patron reply: approved as recommended, no departure or owner checkbox; exactly two missing exception arms, 409 and 403; no worker-command endpoint.

Full cited ruling:
# DEV-310 grill Q4 — Patron ruling (command endpoint input/success/error contract; PRODUCT §5 item 4)

**Verdict: Approved as recommended. No scope or constitution departure; no owner checkbox.** Import takes ImportMovieRequest {folderPath} validated through the LibraryPath guard and returns empty 202 with Location /api/movies/{newId}; enrichment dispatches RequestEnrichmentCommand only and returns empty 202 with Location /api/movies/{id}; watchlist POST/DELETE return empty 204 with repeated add/remove surfacing InvalidTransitionException as 409; ids parse as int then MovieId.TryCreate with nonpositive = 422, malformed binding/body = framework 400 ProblemDetails, domain validation = 422; the single IExceptionHandler gains exactly the InvalidTransitionException = 409 and FeatureDisabledException = 403 arms, keeping NotFound = 404, Validation = 422, unhandled = 500, with no exception text leaked. The 409-instead-of-silent-success cost on repeated watchlist operations is accepted.

## Basis

- Routes, statuses, and headers are ticket-decided (recon 11.1.2) and ticket text is authoritative (PRODUCT.md §3, lines 22-24); the Location header on the enrichment 202 comes from the constitution API and Contract Rules (recon 11.3, lines 462-466: 202 with Location for import and enrichment requests), which the ticket AC1 defers to as settled in Q1.
- Command dispatch matches the existing registrations and prior rulings: RequestEnrichmentCommand is the registered manual-request handler (recon 3.2, HandlerRegistration.cs:28) while ProcessEnrichmentCommand is worker-side and DEV-308 spec FR-005 forbids a duplicate row; watchlist handlers and their repeat-transition throws already exist (recon 3.3, Movie.cs:101-119), and 409 is the constitution Principle V mapping for InvalidTransitionException (recon 11.3, lines 205-211) — preserving domain behavior, not inventing idempotency.
- Input handling follows settled precedent and guards: DEV-309 passes an int id through MovieId.TryCreate (recon 3.3, spec.md:73-80) and LibraryPath already enforces non-blank and no traversal (recon 3.1, LibraryPath.cs:32-33); framework 400 covers malformed transport binding, which sits outside the domain-exception table, so the Principle V table is neither contradicted nor extended, and line 213's no-leak rule holds.

## Implications

- The IExceptionHandler edit adds exactly two arms (409, 403) to reach the constitution V table; recon noted no 403 mapping exists today, so this is a forced edit toward the mandated mapping, not a new shape.
- spec.md fixes the 400/404/409/422/500 split per endpoint as above; any additional request field, response field, or status needs a fresh §5 item-4 ruling.
- CONCLUSIONS.md records the accepted cost: repeated watchlist add/remove conflicts with 409 rather than silently succeeding.


---

## Q5 - Playback execution boundary

Keel question: Dispatch existing decorated PlayMovieCommand; no execution-adapter/config/flag changes. Valid movie flag off 403, enabled successful launch 204, preserve missing-movie 404 precedence and nonpositive 422. Preserve current DEV-394 configured-executable-first, unmapped-format fail-closed, ArgumentList/no concatenated Arguments; generic 500 on launch/config failures, no text leaks. No OS fallback or new shell/process path. Tests use real flag registration with recording starter or safe substitute, never real processes. UseShellExecute=true remains baseline. Approve item-5 boundary or identify necessary constitution departure.

Patron reply: approved as recommended, no constitution departure or owner checkbox; existing precedence and DEV-394 fail-closed baseline preserved; recording substitute tests only.

Full cited ruling:
# DEV-310 grill Q5 — Patron ruling (LocalPlay execution; PRODUCT §5 item 5)

**Verdict: Approved as recommended. No constitution departure; no owner checkbox.** The play endpoint dispatches the existing decorated PlayMovieCommand with no execution-adapter, configuration, or flag changes; flag off with a valid movie returns 403 ProblemDetails via DisabledMediaPlayerLauncher; flag on with successful launch returns empty 204; a missing movie returns 404 even when the flag is off (existing lookup precedence preserved); nonpositive id returns 422. The DEV-394 fail-closed baseline at the current HEAD stands: configured executable resolved first, unmapped format throws before any process starts, ArgumentList only, no OS-association fallback, no new shell or process path; unhandled launch/config failures return generic 500 ProblemDetails with no raw text. Tests use the real flag-based registration with a recording IProcessStarter or safe launcher substitute and never launch a real process; the disabled-host test proves 403 with zero starter calls. UseShellExecute=true remains the DEV-394 baseline and is out of this ticket's scope.

## Basis

- The play contract is ticket-decided: 204, or 403 when LocalPlay is disabled (recon 11.1.2). 403 is exactly the constitution Principle V FeatureDisabledException mapping and Principle VII's DisabledMediaPlayerLauncher design (recon 11.3, lines 205-211 and 253-257); 404-before-launch is the existing handler behavior, since PlayMovieCommandHandler looks up the movie via IMovieCatalog before touching the launcher (recon 3.4).
- The execution boundary is untouched and conformant: recon 12.3 (current HEAD, superseding older launcher citations) proves DEV-394 resolves the configured executable first, throws on unmapped formats without starting any process, and uses ArgumentList with no concatenated Arguments — precisely what Principle VII mandates (never a shell or concatenated command line). Adding an OS-association fallback or any new process path would be a new item-5 surface and is rejected.
- Failure and test shape follow the constitution: unhandled launch/config failures fall under the unhandled -> 500 arm with ProblemDetails and no exception text (recon 11.3, line 213), and the test approach reuses the established flag-selection pattern (ApiHostFactory.cs:26 forces LocalPlay=false; ApiHostCompositionTests.cs:187-199 asserts the disabled launcher resolves, recon 3.4) with a recording substitute rather than a real process.

## Implications

- spec.md fixes the play endpoint precedence exactly: 422 nonpositive id -> 404 missing movie (flag-independent) -> 403 flag off -> 204 launched -> 500 generic for any other launch/config failure.
- No edits to ProcessMediaPlayerLauncher, flag configuration, or launcher registration are authorized; any such change needs a fresh §5 item-5 ruling.
- CONCLUSIONS.md records this item-5 ruling with recon 12.3 cited as the current-HEAD launcher evidence.


---

## Q6 - OpenAPI ownership and constitution departure

Keel question: Prior DEV-309 Q11 defers document/snapshot/drift to DEV-20; committed artifacts are absent; constitution requires endpoint OpenAPI update and drift proof. Recommend complete TypedResults/DTO metadata in DEV-310 and DEV-20 generation ownership, but this needs an explicit owner checkbox if Patron judges the constitution departure necessary. Do not waive by precedent or claim absent drift passed. Give exact checkbox/Gate 1 condition.

Patron reply: blocked: structural - owner checkbox required; deferral accepted as necessary constitution departure under section 2.3b. Gate 1 stays closed until owner answers.

Full cited ruling:
# DEV-310 grill Q6 — Patron ruling (contract-chain ownership; constitution departure)

**Verdict: blocked: structural — owner checkbox required.** I accept Keel's recommendation (DEV-310 ships complete TypedResults/DTO metadata and leaves document generation, snapshot, and client work to DEV-20), but the deferral departs from the constitution's per-endpoint PR gate, and I judge the departure necessary — so it escalates under §2.3b. Gate 1 stays closed until the owner answers.

## Exact owner checkbox for the DEV-310 spec PR

- [ ] Approve DEV-310's committed OpenAPI document, Verify snapshot, and drift-check deferral to DEV-20 for the seven new endpoints (import, enrichment, watchlist add/remove, play, genres, people)?

**Gate 1 condition:** Gate 1 remains closed until this box is answered. An owner rejection must be reconciled — DEV-310 would then need the document work brought into scope through a ticket change — never assumed away or silently waived.

## Basis

- The constitution requires new endpoints to update the committed OpenAPI document, snapshot-tested with Verify, drift check green (recon 11.3, line 63 PR gates and lines 64-65 API rules; constitution 470-472), and both artifacts are absent from the worktree (recon 11.4, lines 104-105). A DEV-310 PR with seven new endpoints cannot meet that gate today — the departure is real, not formal.
- The infrastructure is owned elsewhere: DEV-307 spec.md FR-034 names DEV-20 as document/contract infrastructure owner (cited in DEV-309 Q11, specs/DEV-309/CONCLUSIONS.md:68-72). Building generation/snapshot/drift work inside DEV-310 would silently add DEV-20's delivery scope — itself a §2.3a escalation — so deferral is the only path that does not commandeer another ticket.
- Precedent fixes the form, not the answer: DEV-309 Q11 ruled this same deferral blocked: structural as a DEV-309-specific constitution-departure checkbox, explicitly not a reuse of any earlier owner approval (CONCLUSIONS.md:70). DEV-310 therefore gets its own checkbox; citing DEV-309 does not waive DEV-310's gate.

## What DEV-310 still guarantees (no departure)

- Contract chain intact: C# DTO -> OpenAPI -> generated TypeScript; no hand-written web types, no OpenAPI/Verify/snapshot package, no web-folder or generation wiring in DEV-310.
- Every endpoint carries complete TypedResults and DTO metadata so DEV-20's generation produces a faithful document covering all seven routes; when DEV-20 lands, the drift check covers DEV-310's endpoints retroactively.
- No claim is made that artifact drift passed; the gate is recorded as deferred-pending-owner, not green.

## YouTrack recording (Rigger)

One ownership comment on existing DEV-20 naming the seven DEV-310 routes, their DTO/endpoint metadata inputs, and DEV-310's pending deferral checkbox. No summary/AC rewrite, no new ticket. Done only on Rigger's verified reply; it does not close the owner checkbox.


---

## Q7 - File envelope and ordering

Keel question: Add per-feature endpoint classes and ticket DTOs; fill ImportEndpoints; additive ApiEndpoints and facet HandlerRegistration wiring; exactly 409/403 exception arms; extend IMovieCatalog and its current Infrastructure implementation; sealed GetGenresQuery/GetPeopleQuery and handlers in Features/Library; new matching host/handler/persistence tests with bounded test-only ApiHostFactory changes. Preserve DEV-309 endpoints/responses, command handlers, Program.cs, whitelist, migrations, playback adapters/config, web/dependencies and unrelated scripts. No deletion/wholesale rewrite/new layer. Order facet contract/reads, commands/errors, group wiring, host proof, refactor/gates. Approve care-list item 6 with cited basis. Catalog adapter identified by interface ownership, not guessed path.

Patron reply: approved envelope and ordering, no owner checkbox; cited forced additive edits; scope creep rejected.

Full cited ruling:
# DEV-310 grill Q7 — Patron ruling (file-scope envelope and ordering; PRODUCT §5 item 6)

**Verdict: Approved as recommended.** The additive envelope — new EnrichmentEndpoints, WatchlistEndpoints, PlaybackEndpoints, FacetEndpoints plus ImportMovieRequest/GenreDto/PersonDto under Api/Endpoints; filled ImportEndpoints; additive wiring in ApiEndpoints and HandlerRegistration; exactly the 409/403 arms in ValidationExceptionHandler; IMovieCatalog and its existing Infrastructure implementation extended for facet reads; sealed GetGenresQuery/GetPeopleQuery and handlers in Core/Features/Library; matching new test files with bounded ApiHostFactory adjustments only if required — is the file scope for DEV-310. The ordering facet contract/reads -> command endpoints/error mapping -> feature-group wiring -> host proof -> refactor/gates is approved. No owner checkbox.

## Basis (forced existing-file edits, cited)

- ImportEndpoints.cs is the ticket-named import route stub, empty today (recon 3.1, ImportEndpoints.cs:7); filling it is the ticket's own deliverable. ApiEndpoints group wiring and HandlerRegistration facet rows are additive and forced by the constitution: MapXxxEndpoints grouping (recon 11.3, lines 462-466) and explicit decorated handler registration with no reflection scanning (lines 131-136).
- ValidationExceptionHandler gains exactly the 409 and 403 arms — a forced edit toward the constitution Principle V table (recon 11.3, lines 205-211) already ruled in Q4; IMovieCatalog and its existing adapter are extended per the Q2 ruling, identified by IMovieCatalog ownership rather than an uncited filename.
- Everything preserved as listed — DEV-309 LibraryEndpoints/LibraryResponses, existing command handlers, Program.cs, architecture whitelist, migrations, playback adapters/config (Q5), web and dependencies (Q6), unrelated scripts — satisfies §5 item 6: no deletion or wholesale rewrite of any tracked file the ticket does not name.

## Scope-creep rejections (boundaries of this envelope)

- No OpenAPI generation, snapshot, or web work (Q6 deferral, pending owner checkbox); no pagination, search, movie-filter, or director role (Q3); no playback adapter, flag, or config edits (Q5); no new top-level folder, project, layer, or package (Q2).
- ApiHostFactory changes are test-only and bounded to what the test doubles/real Postgres wiring strictly requires; anything beyond needs a fresh ruling.
- Any file outside this envelope is needs-recon or a fresh §5 item-6 ruling, never a silent edit.


---

## Q8 - Test and gate strategy

Keel question: WebApplicationFactory proof for all seven routes, full success/status/header/body/error matrix; real Postgres for facet translation/distinct Id/sorting/empty tables; real Postgres/RabbitMQ for persistence/publication assertions; MockFileSystem scanner; NSubstitute Core ports/safe process seam only. Focused new facet handler tests; preserve existing command/worker invariants; no duplicate retry/DLQ work. Phase A docs-only, no fabricated gates; Phase B complete pipeline including changed Core/Infrastructure mutation, API exclusions only script-emitted. Explicit property opt-out because HTTP adapters/read projections introduce no new domain invariant, preserve property suite. Honest SKIPPED/SKIP/N-A/Could not run and owner-pending drift. Approve.

Patron reply: approved including explicit property opt-out; constitution IX matched; no gate threshold lowering or fabricated PASS.

Full cited ruling:
# DEV-310 grill Q8 — Patron ruling (test and gate strategy; constitution IX; property-tests opt-out)

**Verdict: Approved as recommended, including the explicit property-tests opt-out.** WebApplicationFactory proof for all seven routes covering the full status/header/body matrix; real Postgres Testcontainers for facet query translation, distinct-id, sorting, and empty-table behavior; real Postgres and RabbitMQ where import/manual-enrichment persistence or publication is asserted; MockFileSystem for the scanner; NSubstitute only for Core ports or the safe process seam; focused unit tests for the new facet handlers; existing command/worker tests retained with no duplicate retry/DLQ work. Phase A is documentation only — no fabricated gate PASS. Phase B runs the full pipeline with mutation for changed Core/Infrastructure and API exclusion only when the script itself emits it. No owner checkbox.

## Basis

- Constitution Principle IX (recon 11.3, line 62): Testcontainers (Postgres, RabbitMQ), WebApplicationFactory, WireMock.Net, Verify; NSubstitute with Moq forbidden; infrastructure never mocked, EF Core InMemory forbidden, MockFileSystem for the scanner. The plan matches point for point, and the recording process seam with no real process launch is already ruled in Q5.
- The test matrix is the settled contract made executable: exact statuses, Location headers, empty command bodies, scalar facet arrays, and ProblemDetails come from ticket AC2 (recon 11.1.3) and the constitution V table (recon 11.3, lines 205-211) as ruled in Q3/Q4 — including repeat-watchlist 409, disabled-play 403, enabled recording launch 204, and fail-closed generic 500 with no sensitive text.
- Property-tests opt-out is honest scope, not threshold-lowering: this ticket adds HTTP adapters and read projections over existing domain behavior (Q2/Q4 rulings) and introduces no new domain invariant, so propertyTests: opt-out is recorded with that reason and the existing property suite is retained untouched. The mutation gate still applies to changed Core/Infrastructure; NOT APPLICABLE is valid only when run-mutation.ps1 itself reports the configured exclusion.

## Implications

- brief.md/spec.md record the test matrix, the propertyTests: opt-out with its reason, and every gate disposition by name: SKIPPED (scope-empty), SKIP (configured opt-out), NOT APPLICABLE (script-emitted), Could not run (blocking). None is ever folded into a green verdict.
- OpenAPI drift stays deferred-pending-owner per Q6 — never claimed green.
- Phase B gate failures route per the pipeline failure table; no threshold is lowered to go green.


---

## Q9 - Loop discipline

Keel question: No unresolved Critical/High; Medium fixed or explicitly adjudicated with cited evidence and necessary follow-up ID, no silent demotion; Low noted/no ticket. Gates, missing mandatory axes, owner checkbox or unacknowledged handoff block regardless. Freeze exactly seven routes and Q2-Q8 scope, owner-pending OpenAPI deferral, no worker/director/frontend/schema/dependency/layer/launcher expansion; anything else is a follow-up issue, not a finding in this round. Two plan/remediation rounds then blocked for above-bar findings, no lowered bar. Confirm and clarify Medium.

Patron reply: confirmed; Medium not-a-defect needs cited evidence; real-but-deferred needs YouTrack follow-up ID before READY. Two rounds then blocked; Q6 checkbox remains blocking.

Full cited ruling:
# DEV-310 grill Q9 — Patron ruling (loop terms: closing bar, frozen scope, round cap)

**Verdict: Confirmed as recommended.** The closing bar, frozen scope, and two-round cap are the loop terms for DEV-310's code-review/ship-review and plan challenge. No owner checkbox.

## Basis

- The pipeline's Loop Discipline requires exactly these three elements for readback-judged stages — a closing bar, a frozen countable scope ending with the follow-up line, and a round cap defaulting to two (agent-pipeline.mdc, Loop Discipline 1-3). Keel's terms match that standing rule; cited once here for the DEV-310 chain.
- The bar is consistent with the pipeline's deferral semantics: a Critical or High finding deferred to a follow-up still keeps the stage at NEEDS FIXES — deferral stops further fix commits, it does not make the diff READY — and amendments to bar or scope after a loop opens are a new issue.
- The frozen scope is the ticket plus the Q2-Q8 rulings and nothing more: seven ticket routes, actor-only facet reads (Q2/Q3), the Q3-Q5 contracts, the Q7 file envelope, the Q8 proof matrix; Q6 document/snapshot/drift is deferred-pending-owner and outside the finding scope; no worker redesign, director support, frontend, schema, dependency, new layer, or launcher change. Anything else is a follow-up issue, not a finding in this round.

## Medium disposition policy (clarified)

A Medium closes only one of two ways:
1. A fix commit inside the loop; or
2. Explicit adjudication in the review artifact with cited evidence — either not-a-defect (evidence quoted) or real-but-deferred, in which case a YouTrack follow-up ID (via Rigger) is recorded before the stage can exit READY.

A Medium is never silently demoted to Low, and an ordinary Low is recorded as noted/no ticket without busywork, per the standing follow-up rule (only Critical/High, broken behaviour, or owner-requested findings get tickets).

## Blocks regardless of severity

Any gate failure, a missing mandatory Risk/Standards/Spec axis report, the unfulfilled Q6 owner checkbox, or an unacknowledged handoff blocks the stage independent of finding severity. Past the two-round cap, unresolved above-bar findings go blocked with cited evidence — the bar is never lowered and the loop never runs indefinitely.


---

## Q10 - Shared understanding and closure

Keel question: Read CONCLUSIONS.md Q1-Q9, assembled brief.md and ASSUMPTIONS.md; confirm faithful scope/contracts/architecture/security/files/tests/gates/order/loop terms and exact owner checkbox, shared understanding and grill closure, or identify a correction. Closure authorizes only Quill Phase A drafting; no implementation or owner approval. Report pending DEV-20 ownership-comment receipt.

Patron reply: Shared understanding confirmed; grill closed; no correction. Artifacts read and faithful. Quill Phase A drafting only. Gate 1 stays closed on Q6. DEV-20 ownership comment not sent successfully and remains pending; property opt-out must be recorded in task note.

Full cited closure:
# DEV-310 grill Q10 — Patron closure confirmation

**Verdict: Shared understanding confirmed; the grill is closed.** I read specs/DEV-310/CONCLUSIONS.md (all nine exchanges with full cited rulings appended), brief.md (read back, final-confirmation-pending marker accurate), and ASSUMPTIONS.md (Q3 Name-then-Id sorting tagged [assumed]). The brief faithfully captures the seven exact route/DTO/status/header/input/failure contracts, actor-only catalog reads, no dependency/schema/layer/port change, DEV-394 execution preservation, the forced file envelope, tests/gates with the property opt-out, ordering, frozen scope/closing bar/two-round cap, and the exact Q6 owner checkbox. No correction.

## What closure authorizes

- Quill Phase A drafting only (spec.md, plan.md, tasks.md) against this brief and CONCLUSIONS.md. No implementation, no Speckit launch by Keel, no checkbox closure.
- Gate 1 remains CLOSED until the owner answers the Q6 checkbox on the spec PR: Approve DEV-310's committed OpenAPI document, Verify snapshot, and drift-check deferral to DEV-20 for the seven new endpoints (import, enrichment, watchlist add/remove, play, genres, people)?
- Settled vocabulary stands as-is: existing canonical terms (constitution VIII), ticket-decided GenreDto/PersonDto; no new domain term, glossary, or ADR change is introduced.

## Rigger dispatch status (reported as requested)

- DEV-20 ownership comment (Q6): PENDING — dispatch attempted three times; each send was interrupted before completion, so Rigger has not received it. Comment file ready at $env:TEMP\patron-dev310-dev20-comment.md. A verified recording does not close the owner checkbox.
- One additional recording follows from brief.md line 81: the propertyTests: opt-out line with its reason must be recorded on the DEV-310 task note for pipeline acceptance of a no-tests-tagged exit 2. Also pending the same channel.
- I will dispatch both once the channel is free, or report to Bernstein if Rigger proves unreachable.

---

## Phase B §2.3 #6 — ApiHostCompositionTests.cs route-set update

**Verdict: Approved.** Update tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs:131-147 route assertion to the seven ticket routes and ManifestContracts lines 34-72 to add the two facet contracts. No owner checkbox.

- Not ticket-named and outside the frozen envelope: recon-DEV-310 §14.3 names only the seven endpoints, §15.4 records this test file as NOT named by the ticket, ticket ACs are the two route-conformance lines (§14.1); spec/plan/tasks/brief contain no ApiHostCompositionTests reference, and Q7 (CONCLUSIONS:192) requires a fresh ruling for any file outside the envelope.
- Forced consequential edit: the exact two-route ShouldBe would fail the dotnet test gate (task-pipeline Phase 3) once the seven ticket-decided routes land; Q8/brief proof matrix requires seven-route host proof, implemented by T024 after T023 wiring.
- No escalation: the edit implements the ticket's seven routes (no ticket-text change) and departs from no constitution clause; under the role rule the §2.3 #6 item is Patron-decided. It adds no deliverable and reorders no frozen work — it sits inside Q8/T024 proof within the brief:92 frozen scope — so no task-chain §4 owner checkbox.

---

## Phase B §2.3 #6 — RecordingMovieCatalog facet-member update (T001 I1)

**Verdict: Approved.** Add both T002 facet members to RecordingMovieCatalog in tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs:662-686 only, minimal recording/stub parity with the new port members; no other test-logic change. No owner checkbox.

- Not ticket-named and outside the frozen envelope: recon-DEV-310 §16.9 records zero matches for LibraryEndpointsTests, RecordingMovieCatalog, and the EfMovieCatalog filename across spec/plan/tasks/brief/CONCLUSIONS/ASSUMPTIONS; T002 (tasks.md:31) names only IMovieCatalog.cs and T005 (tasks.md:34) names only the existing Infrastructure adapter; Q7 (CONCLUSIONS:192) requires a fresh ruling for any off-envelope file.
- Forced consequential edit: IMovieCatalog.cs:10,12 has no default members, so C# requires every concrete implementation to add both T002 members; T005 covers EfMovieCatalog only, leaving RecordingMovieCatalog (LibraryEndpointsTests.cs:662-686, registered at :570-572) non-compiling; NSubstitute proxies need no source edit (recon §16.7). Quill binds this compatibility work to Phase 2; empty default interface methods stay prohibited per Keel I1.
- No escalation: the edit implements the ticket-decided Q2 facet reads (brief.md:38,58; spec.md:118; plan.md:74) with no ticket-text change and no constitution departure; under the role rule the §2.3 #6 item is Patron-decided. It adds no deliverable and reorders no frozen work — test-only fixture parity inside the brief:92 frozen scope — so no task-chain §4 owner checkbox.


---

## Phase B T001 I1/I2 technical resolution and Quill correction list (Keel, 2026-10-06)

Identity: F:/Dev/LamuFlix.worktrees/DEV-310; branch feature/DEV-310; HEAD 0badbad05f17959d004401f8664603fee499268a; clean before this decision append. The reported 5fc45a14c461f81e1ca53ed74c46b3209555477b is superseded by the Patron parity-ruling commit recorded at DEV-310:265-269. Facts used only from the requested artifacts and recon-DEV-310 sections 14-16; no independent source recon.

### I1 HIGH - Technical decision settled; traceability correction required

Basis: DEV-310:261-269; recon section 16.2-16.8; Patron approval above at CONCLUSIONS.md:290-296. T002 adds two required members with no default interface implementations. Both concrete implementations must implement the same signatures: EfMovieCatalog under T005, and RecordingMovieCatalog under expanded T002. Bind the latter to Phase 2, within tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs:662-686 only. Use minimal recording/stub parity, with cancellation handling consistent with the existing double and typed empty facet results for this existing library-only fixture. Preserve Queries, DetailsIds, Details, BrowseAsync, GetDetailsAsync, registration and all existing test assertions. Do not implement EF queries in the double, change unrelated test logic, or add default interface bodies. The exact new member signatures are not pinned by recon section 16.6; the double implements the signatures actually introduced by T002, not a separate contract invented here.

T002 port/double parity and T005 adapter parity must be complete together before the Phase 2 passing handoff. An intermediate interface edit is not a passing checkpoint. Retain T008 focused handler tests and T009 real Postgres facet proof; stub parity is compilation compatibility, not proof of production facet behavior. NSubstitute proxies require no source edit. No new task ID, reordered phase, dependency, schema, public API, owner checkbox or deliverable is needed.

### I2 MEDIUM - Technical decision settled; traceability correction required

Basis: CONCLUSIONS.md:280-286; DEV-310:242-254; recon sections 14.4 and 15.4. Bind the approved ApiHostCompositionTests.cs edit to T024 in Phase 8 after T023 group wiring and explicit facet registrations. The business-route assertion at the current lines 131-147 must preserve both existing library routes (/api/movies and /api/movies/{id:int}) and add the seven ticket routes, for nine business routes total. Retain the existing liveness assertion and existing route/HTTP-method conventions; role=actor is a query contract, not an additional route. ManifestContracts at current lines 34-72 retains all seven existing handler rows and adds GetGenresQuery and GetPeopleQuery, for nine handler contracts. Do not replace the existing two-route set with only the seven new routes, drop manifest rows, relax equality to a subset assertion, or remove existing composition checks. No source/launcher/registration change beyond the already approved T023 work is authorized by this test ruling.

### Exact traceability corrections for Quill (one numbered fix list)

1. brief.md:54-62: extend the existing-test envelope with tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs, restricted to RecordingMovieCatalog member parity (current lines 662-686; Patron CONCLUSIONS.md:290-296), and tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs, restricted to additive business-route and ManifestContracts updates (current lines 131-147 and 34-72; Patron CONCLUSIONS.md:280-286). At brief.md:68 explicitly include port/concrete-implementation parity in foundation work before its passing checkpoint; at :71 explicitly include additive nine-business-route/nine-handler-contract composition proof after wiring. Cite recon sections 14-16 and these two Patron rulings. Preserve the frozen seven-route deliverable and all other envelope exclusions; distinguish this Phase B addendum from historical Phase A identity/owner-pending wording.
2. plan.md:9 and :29: carry both approved existing-test edits into the foundation-then-host-proof approach and envelope. At :80-88 name the evidenced adapter src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs (recon sections 15.1 and 16.3), LibraryEndpointsTests.cs restricted to RecordingMovieCatalog parity in Phase 2, and ApiHostCompositionTests.cs restricted to route/manifest additions in Phase 8. At :91 replace the stale unnamed/guessed-adapter implication with the evidenced path and citations; no new adapter or layer. Record both-concrete-implementation completion before the Phase 2 handoff, then preservation of the two existing routes/seven manifest rows plus seven routes/two facet rows after T023. Cite both Patron rulings and the Keel technical decisions here.
3. tasks.md:31: expand T002 to add both required port members and matching minimal RecordingMovieCatalog members in tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs:662-686, preserving existing logic and prohibiting default interface bodies. At :34 give T005 the evidenced path src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs and require both production facet methods. At :40 and dependencies :154-157 specify that T002 port/double parity and T005 adapter parity complete before the passing foundation checkpoint; keep T008/T009 behavior proof unchanged. At :127 expand T024 to name tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs, retain both existing library routes and seven existing manifest rows, add seven ticket routes and the two facet query rows, and preserve liveness/other checks after T023. T023 stays wiring/registration only; no new task IDs or phase reorder. At T001 :21 and Notes :177 record Phase B identity/recon/ruling provenance as a dated receipt, preserving historical Phase A context; leave T001 unchecked until Quill corrections are read back and Keel confirms consistency.

Disposition: I1 and I2 decisions are resolved by the existing Patron approvals and the technical constraints above; their artifact corrections are still pending. T001 remains unchecked and Phase 2 remains on hold until Quill applies this list and Keel verifies traceability. T1 LOW remains noted: the prerequisite script rejected feature/DEV-310 under its numeric-branch convention; no branch rename or pin workaround. No implementation, tests, gates, commits, PR/tracker changes, Quill-owned artifact edits or new owner question in this ask. Recon baseline PASS receipts are not implementation gate results.

---

## Phase B T001 verified completion (Keel, 2026-10-07)

Identity verified: F:/Dev/LamuFlix.worktrees/DEV-310; feature/DEV-310; HEAD 0badbad05f17959d004401f8664603fee499268a. Existing four modified artifacts preserved.

Quill corrections 1-3 are consistent with DEV-310:279-291: brief.md:64,70,73; plan.md:9,29,81,85,91; tasks.md:31,34,40,127,154. I1 parity is bounded to T002 RecordingMovieCatalog and T005 EfMovieCatalog before the Phase 2 passing checkpoint, with T008/T009 behavior proof retained. I2 T024 preserves two existing routes and seven existing handler rows, adds seven routes and two facet rows after T023, and retains liveness and exact composition checks. No remaining correction or new ruling.

T001 satisfied and checked in tasks.md:21. All five decision inputs are present; ASSUMPTIONS.md is 208 bytes and spec.md is 10713 bytes, matching Wisp DEV-310:306. Gate 1 PR #103 MERGED at 2026-10-07T00:44:43Z with Q6 ticked is verified from DEV-310:218-222. Phase B provisioning and recorded In Progress tracker state are at DEV-310:225-227. Completed Wisp pickup drift is at DEV-310:232,304-309 and recon-DEV-310:227,266 (six spec files, 813 insertions). Keel correction readback is recorded at DEV-310:300-302 and confirmed in this resumption.

This completion supersedes earlier T001 unchecked/Phase 2 hold dispositions; historical Phase A owner-pending wording remains history, not a current owner block. Task is Phase B with T001 complete, T002-T026 unchecked, and Phase 2 eligible for Conductor handoff to Anvil. Tracker In Progress is the recorded Rigger receipt, not a new live tracker query. T1 LOW remains noted without branch rename or workaround. No code, tests, implementation gates, commit, PR or tracker mutation. Next: Conductor hands off the foundational phase with the approved parity constraints and passing checkpoint.
---

## Phase 3 §2.3 #6 — Incremental exact-set route/name assertions (LibraryEndpointsTests.cs:538, ApiHostCompositionTests.cs:131-147)

**Verdict: Approved — incremental additive updates per endpoint phase, completed at T024.** No owner checkbox.

- LibraryEndpointsTests.cs:538 (off-envelope; brief.md:64 limits this file to :662-686): each phase 3-7 that maps a ticket route extends the exact `endpoints.Keys.ShouldBe([...], ignoreOrder: true)` set with exactly the endpoint name(s) that phase maps (Phase 3: `ImportMovie`). Keep the exact-set ShouldBe (no subset/Contains relaxation), keep the two existing ShouldDescribe lines (:539-540) unchanged, add no new ShouldDescribe or other test logic. Forced edit: tasks.md:9 requires every story phase to pass with no new failing host tests, and the existing exact set fails the dotnet test gate the moment a named route is mapped.
- ApiHostCompositionTests.cs:131-147: the approved edit (CONCLUSIONS Phase B ApiHostCompositionTests ruling, :280-286) may land incrementally in phases 3-7, each adding only the route(s) that phase maps to the exact business-route set; ManifestContracts (:34-72) rows are added only when a phase actually makes them fail. T024 (tasks.md:127) remains the completion and proof point for the final nine-route/nine-contract form and all its constraints (no subset relaxation, no dropped rows, liveness and conventions preserved). Same forced basis (tasks.md:9).
- No escalation: additive assertions tracking the ticket's own seven routes; no ticket-text change, no constitution departure, no new task ID, phase reorder, or deliverable. brief.md:64 envelope is read as extended to LibraryEndpointsTests.cs:538 by this ruling; this standing ruling covers phases 4-7 without re-asking.

---

## Phase 8 §2.3 #6 — BadHttpRequestException 400 arm in ValidationExceptionHandler.cs

**Verdict: Approved — add exactly one `BadHttpRequestException` arm returning 400 ProblemDetails.** No owner checkbox; supersedes the exactly-two-arms wording of Q4 (CONCLUSIONS:92) for this one arm only.

- Forced edit: spec.md:25 (US1 AC3), spec.md:100 (edge cases) and FR-008 spec.md:117 require malformed body/binding to return 400 ProblemDetails *through the single exception handler*; Anvil's Phase 8 host proof shows the real host throws `Microsoft.AspNetCore.Http.BadHttpRequestException`, which falls to `_ => false` and yields generic 500. The spec AC forces the edit, so §2.3 #6 is satisfied. Q4 assumed framework binding produced 400 unaided (CONCLUSIONS:98); that premise was wrong at the host, and the contract (400) is unchanged — so this is not a ticket-text change or a constitution departure (Principle V table not extended with a domain exception; transport 400 sits outside it, as Q4 already held).
- Shape: one arm `BadHttpRequestException => WriteAsync(httpContext, StatusCodes.Status400BadRequest, CreateBadRequestProblem())`, a static `CreateBadRequestProblem()` matching the existing helpers (Status 400, Title `Bad Request`, Type `https://tools.ietf.org/html/rfc9110#section-15.5.1`). No `Detail`, no `exception.Message`, no errors extension (no-leak rule, Q4). Fixed 400, not `exception.StatusCode` passthrough. No other new arms, no middleware/option changes, no `Program.cs` edits.
- Proof: a host-level integration test (malformed JSON body on POST `/api/movies/import` and a non-int route id) asserting 400 ProblemDetails with no exception text; existing 404/409/403/422/500 arms and their tests unchanged.
