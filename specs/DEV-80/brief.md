# DEV-80 - Grill brief

## Status and authority

Phase A grill closed at Q7 of a maximum of 12 questions. Patron approved Q1-Q6 at commit 80174e4 and Q7 at 50d6317. No blocked: structural item arose. This brief authorizes Quill drafting; it does not establish plan freeze, Gate 1, implementation authorization, or gate clearance.

Worktree: F:/Dev/LamuFlix.worktrees/feature-080-spec. Branch: feature/080-spec. Decision HEAD: 50d6317c0e32dcca7188639811f95435a606d181. The committed intake pin selects specs/DEV-80.

Facts come exclusively from shared recon-DEV-80, recon-DEV-80-2 and recon-DEV-80-3. Ticket authority is DEV-80 note:7-8, reproduced in recon-DEV-80:11-16. The original Q1-Q7 decisions and A1-A5 taste answers are reproduced below; subsequent Patron rulings R1-R4 and A6 are recorded in the decision supplement. Missing facts require needs recon to Bernstein; unresolved decisions return to Patron and are recorded here before artifact changes.

## Frozen scope and approach

Deliver the folder import modal, minimum first-page library list, Pending status display, conditional 2-second TanStack Query polling, and NotFound/Failed retry behavior. Include the one required browse status field and the complete C# DTO -> OpenAPI -> generated TypeScript contract chain. TanStack Query is the sole client state/cache layer. HTTP uses openapi-fetch typed by generated paths; MSW handlers use openapi-msw typed by those paths. Zod remains deferred.

The host displays title, status badge and eligible retry action. Import 202 closes the dialog and prepends a Pending cached row using the Location id and the last folder-path segment as title. Polling reconciles the row with the server. Import 422 shows the folderPath problem inline; 400/network failure shows a generic inline error. Poll while any current browse item is Pending; stop otherwise, with background polling false. Retry 202 changes the cached row to Pending; 404/409 shows an inline row error and invalidates browse data.

MovieSummaryResponse gains enrichmentStatus with string values Pending, Enriched, NotFound and Failed. Carry persisted status through MovieSummary and EfMovieCatalog. Details DTO, other enrichment fields, new routes, schema changes, posters, filters, sorting UI, paging controls, details view, routing and other state libraries are outside the frozen scope. A new CI types-regeneration diff step is outside Q2 scope. Preserve the existing MSW harness contract.

Frozen product scope is Q1-Q6 as approved by Q7; R1-R4 below settle forced implementation and metadata corrections within that scope. Anything else is a follow-up issue, not a finding in this round.

## Loop discipline

- Closing bar: in-scope Critical, High and Medium findings block closure.
- Review round cap: two.
- Size M adjudication requires Sentry, Ledger and Compass axis reports under the seat rules.
- Out-of-scope observations use the Q7 follow-up policy: a ticket only for Critical/High or broken behavior; otherwise one recorded line. Rigger owns tracker writes after Patron rules.

## Files and responsibilities

Keel owns this brief and decisions. Quill drafts spec.md, plan.md and tasks.md from it. UI plan challenge awaits Palette design.md under the seat rules; no design approval is implied here.

Approved delivery touch set, from recon-DEV-80:40-44,51,54-61 and Q1-Q5:

- src/LamuFlix.Core/Ports/MovieSummary.cs: carry status in the summary port record.
- src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs: project the existing persisted status.
- src/LamuFlix.Api/Endpoints/LibraryResponses.cs and the browse response mapping in src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs: expose the approved summary status only.
- web/src/api/openapi.json and tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json: regenerate the contract baseline and Verify snapshot through their established workflow.
- tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs and affected existing projection/browse tests: prove the generated status contract and mapping. No new named operation is added.
- web/package.json and web/package-lock.json: approved dependencies and generate:api script.
- web/src/api/types.ts: committed generated output from openapi-typescript; never edit it by hand.
- web/src/main.tsx: QueryClientProvider around App.
- web/src/App.tsx and web/src/App.test.tsx: replace readiness host/assertion with the minimal library host and behavior coverage.
- Additive /web client, query, dialog/list and test modules serving this scope; Quill specifies their concrete paths in plan/tasks without introducing another architectural layer.

R2/R4 extend this delivery touch set with src/LamuFlix.Api/EnrichmentStatusSchemaTransformer.cs, src/LamuFlix.Api/BrowseMoviesOperationTransformer.cs and their registration in src/LamuFlix.Api/Program.cs. Existing tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs owns both status schema and browse parameter metadata proof. No new test file is required for the metadata correction.

Exact package pins have not been ruled in Q1-Q7. Do not invent compatibility evidence or treat approval of a dependency name as approval of a specific version; obtain recon if a version-specific decision is needed.

## Test strategy and gate expectations

Q7 approves generated status contract and projection tests covering DTO, EfMovieCatalog, OpenAPI baseline, Verify snapshot and OpenApiContractTests. Cover import 202 plus Location, 422/400 errors and Q6 network-error behavior; polling starts with Pending and stops with none; retry appears only for NotFound/Failed and covers 202/404/409. Test HTTP handlers are typed through openapi-msw using generated paths. Tests must verify observable behavior and use existing libraries/conventions.

Run the existing web gate scripts/run-web-gates.ps1 and backend gates applicable to the touched C# files at their pipeline stages. The existing web gate runs lint, TypeScript checking, Vitest and build (recon-DEV-80:61). Backend verification follows repository-configured analyzers, tests and pre-PR mutation requirements; never lower thresholds. PASS, SKIPPED (scope-empty), configured SKIP, mutation N/A and Could not run remain distinct. No gates or tests are run by this grill handoff.

## Task ordering

1. Carry status through the backend summary/projection and response contract.
2. Regenerate OpenAPI/Verify artifacts, then generate and commit TypeScript types; establish typed client and QueryClientProvider.
3. Implement the minimum list, import dialog, cache update, conditional polling and retry flows with the approved taste copy.
4. Complete the approved contract/projection and frontend behavior coverage, then run stage-appropriate web/backend gates.

This ordering is the Q7 ruling. Frontend implementation follows web-implement; backend work follows its own authorized pipeline stage.

## Complete approved grill answers

The following is the persisted Patron decision record, reproduced without changing its rulings. Q7 contains the accepted Keel proposal; Q1-Q6 contain the questions, answers and cited rationale available in the source artifact.

# DEV-80 - Patron conclusions

Grill (Phase 2 step 1). Budget 12 questions; 6 used. Facts from recon-DEV-80; ticket scope from note DEV-80:7-8.
Unattended-owner run: no user question in chat; no `blocked: structural` item arises.

## Q1/12 - New npm dependency: `@tanstack/react-query` (section 2.3.1, 2.3.2)

**Ruling:** Approved as a runtime dependency and the single client state/cache layer; `QueryClientProvider` wraps `App` in `web/src/main.tsx`. No other state library (no Redux/Zustand/context store), no router.

- Ticket text names it (DEV-80:7 'TanStack Query smart polling'; :8 'refetchInterval 2s') - ticket text counts as decided.
- Constitution:335 names TanStack Query as the web stack; DEV-20/brief.md:121 deferred it to 'later API-client work', which this is (recon-DEV-80:65,68,78-79).
- Editing `main.tsx` is forced by this AC, so it is in scope (section 2.3.6).

## Q2/12 - Generated client: `openapi-typescript` (dev) + `openapi-fetch` (runtime)

**Ruling:** Approved. Add `openapi-typescript` as a devDependency with an npm script `generate:api` that writes `web/src/api/types.ts` from `web/src/api/openapi.json`; commit the generated file. All HTTP calls go through an `openapi-fetch` client typed by those `paths`. No hand-rolled DTO types.

- Role hard rule (contract chain) and constitution:470-472 require web code to consume generated types, never hand-written ones; recon-DEV-80:58,87 show `types.ts` does not exist, so the ticket cannot be delivered without a generator.
- Constitution:335 names exactly `openapi-typescript` + `openapi-fetch`; not a swap of anything in PRODUCT.md (PRODUCT.md:11 names React/TS/Vite only).
- A CI step that diffs regenerated `types.ts` against the committed one is not added here (ci.yml not forced by an AC or gate): noted, no ticket.

## Q3/12 - Zod and MSW handler generation

**Ruling:** Zod is NOT added (no runtime schema validation is required by the AC; the folder-path check is server-side, ImportEndpoints.cs:33-39). Test handlers use `openapi-msw` (devDependency) so MSW handlers are derived from the generated `paths` types, not hand-typed.

- Constitution:472 'The TypeScript client and MSW handlers are generated from that document, never hand-written'; `openapi-msw` binds handler paths, params and bodies to the `openapi-typescript` output, which meets that rule with one test-only dependency.
- Zod stays deferred as in DEV-20/brief.md:121 - constitution:335 lists the stack, it does not require every member in every ticket; adding an unused dependency fails YAGNI.

## Q4/12 - Absent enrichment status in API responses (section 2.3.4)

**Ruling:** Approved public API shape addition: `MovieSummaryResponse` gains `enrichmentStatus` (the `EnrichmentStatus` enum, serialized by name as a string: `Pending|Enriched|NotFound|Failed`). Nothing else: no `EnrichedAt`/`EnrichmentAttempts`/`LastFailureCategory`, no change to `MovieDetailsResponse`, no new route, no schema/migration.

- The ticket cannot be met without it: 'prepend Pending row', 'refetchInterval 2s only while any Pending in view', and 'retry on NotFound/Failed' (DEV-80:8) all need the row's status, and no response carries it (recon-DEV-80:40-41,48). This delivers the ticket as written; it neither adds to nor drops from it, so it is a Patron ruling, not an escalation.
- Constitution:468 already prescribes the exposure (`EnrichmentStatus`, enums by name, numeric values never cross the API); this ticket exposes the one field it needs. The DEV-318 rejection (recon-DEV-80:50) was for a test-only ticket with no consumer; here the AC is the consumer.
- Forced chain, in scope: `MovieSummary` port record + `EfMovieCatalog` projection (column already exists, MovieRecord.cs:19), `LibraryResponses.cs`, `web/src/api/openapi.json`, the Verify snapshot, and `OpenApiContractTests` expectations (recon-DEV-80:51). No new named route, so `LibraryEndpointsTests.cs:538-550` stays unchanged.

## Q5/12 - Replacing the tracked readiness `App` (section 2.3.6)

**Ruling:** In scope - forced by the AC. `web/src/App.tsx` becomes the library screen (minimal movie list + 'Import folder' button + dialog); `web/src/App.test.tsx` readiness assertion is rewritten to cover that screen. `web/src/test/msw.test.ts`/`server.ts` keep their contract.

- 'prepend Pending row' and 'Pending in view' (DEV-80:8) require a rendered list; App is the only mount point (recon-DEV-80:54-55,88). A forced edit is in scope: cite the AC and move on.
- The list is the minimum host for the AC: title + status badge + retry action per row, browse first page. Posters, filters, sorting UI, paging controls, details view and routing are out of scope.

## Q6/12 - Behaviour on 202, polling and retry

**Ruling:**
- Import: on 202, close the dialog, take the id from the `Location` header (CORS already exposes it, Program.cs:31-33) and prepend `{ id, title, enrichmentStatus: 'Pending' }` to the cached browse query; the next poll reconciles it with the server row. On 422, keep the dialog open and show the `folderPath` problem message inline; on 400 or network failure, show a generic error inline.
- Polling: `refetchInterval` is a function returning `2000` while any item in the current browse data has `enrichmentStatus === 'Pending'`, else `false`. Default `refetchIntervalInBackground` (false).
- Retry: shown only for `NotFound` and `Failed` rows; POST `/api/movies/{id}/enrichment`; on 202 set that row to `Pending` in cache, which re-arms polling. 404/409 show an inline row error and invalidate the browse query.

- DEV-80:8 verbatim; existing endpoint contracts: import 202+Location (recon-DEV-80:23), enrichment 202/404/409/422 with Pending-guard (recon-DEV-80:34-37).
- Optimistic title source is a taste call - see ASSUMPTIONS.md A1.

## Q7/12 - Brief closure terms (Keel)

**Ruling:** Approved as Keel proposed. Blocking bar: in-scope Critical/High/Medium findings block; two review rounds; scope frozen at Q1-Q6, and anything outside it becomes a follow-up issue, not a finding in this round. Test strategy and ordering are accepted unchanged:
- Tests: generated status contract and projection (DTO, `EfMovieCatalog`, OpenAPI baseline/Verify snapshot, `OpenApiContractTests`); import 202 + Location and 422/400 errors; 2s polling starts with a Pending row and stops when none remain; retry shown only for NotFound/Failed, plus the 202/404/409 paths; MSW handlers typed through `openapi-msw`; existing web gates (`run-web-gates.ps1`) plus the backend gates for the touched `.cs` files.
- Order: status contract, then generated client and provider, then list/dialog/poll/retry, then tests and gates.

- Each test item traces to a Q1-Q6 ruling or DEV-80:8. Ordering follows the contract chain (C# DTO, then OpenAPI, then `types.ts`, then web).
- The frozen scope enforces Q5's out-of-scope list. Follow-up handling follows the role rule: a ticket only for Critical/High or broken behaviour; anything else gets one line here.
- Basis is Keel's recommendation (taste/process). It changes no ticket text and does not clear Gate 1.

## Approved taste answers

# DEV-80 - Assumptions (taste, Patron)

- A1 [assumed] Optimistic Pending row title is the last segment of the submitted folder path; the 2s poll replaces it with the server title.
- A2 [assumed] Labels: button 'Import folder'; dialog title 'Import a movie folder'; input label 'Folder path'; submit 'Import'; cancel 'Cancel'; row action 'Retry'.
- A3 [assumed] Status badge text is the enum name as received (`Pending`, `Enriched`, `NotFound` shown as 'Not found', `Failed`).
- A4 [assumed] Empty library copy: 'No movies yet. Import a folder to get started.'
- A5 [assumed] List order is the server browse order; the client does not re-sort after a poll.

## Decision supplement - plan challenge round 2 (2026-10-12)

State reconciliation: initial verification found HEAD 59da43828faa1e54ba9f588a75e4b5d1ea69a531, the direct child of 8a5565d containing Patron R2/R3. Patron subsequently recorded R4 at bae726b88af2907627ca331f87eba39d788529c7. The six untracked draft artifacts were preserved; these are expected decision advances, not unexplained drift. Original grill Decision HEAD above remains historical.

- A6 [assumed] Standing first-page request is sort=Title&direction=Ascending&page=1&pageSize=20 (Patron R3; recon-DEV-80:123-125). Preserve server order; no sort UI or paging controls.
- Selected metadata-only mechanism: BrowseMoviesOperationTransformer.cs implements IOpenApiOperationTransformer and is registered using AddOperationTransformer in Program.cs AddOpenApi options. Apply only to BrowseMovies; rename the four existing ordering/pagination parameters to sort/direction/page/pageSize, mark each required, use the exact string enums and integer bounds in R4. Leave the other ten parameters unchanged. Recon-DEV-80-3:22-27 supports the operation-transformer surface; :16-20 does not establish parameter-level requiredness for the attribute alternative, which is rejected under R4.
- Existing OpenApiContractTests.cs must prove the status string schema and all four generated parameter names, parameter-level required flags, enum sets and integer bounds. Keep the existing operation set and unrelated parameters unchanged. Use recon-DEV-80:128-136 regeneration workflow for baseline and Verify snapshot, then generate types.ts. Client and MSW handlers consume the corrected generated query types and send/assert A6 values without handwritten DTOs or unsafe casts (recon-DEV-80-3:29-33). No binding, validation, route, accepted-value, DTO or UI change arises from the metadata correction.
- Quill retains the R1 design scope trim; only a newly added or changed UI behaviour returns to Palette. The two-round cap and closing bar remain unchanged. This supplement clears ND-01/ND-02 decisions; it does not clear artifact consistency, freeze the plan or open Gate 1.

### Complete subsequent Patron rulings
# DEV-80 Conclusions (Patron)

## R1 - design.md ownership after plan-challenge round 1 (2026-10-12)
Ruling: Quill retains the round 1 scope trim of design.md; Palette does not resume ownership for this round. Keel verifies design.md together with plan.md and tasks.md.
- The edits only remove claims (list-level retry, skeleton, poll-error UI, committed contrast/target). Keel's adjudication forced each removal: Compass F1 is out of frozen Q1-Q6 scope (brief.md:19), and fix item 4 says to align design claims with what tasks verify, with contrast/target not a delivery requirement (DEV-80 note :90, :96).
- No new design decision was made, so this is not a §2.3 care item and does not need a Palette re-draft. A gate forced the edit, so it stays in scope.
- If Keel's verification finds that design.md added or changed UI behaviour, as opposed to deleting it, that part goes back to Palette.

## R2 - ND-01 schema transformer file and registration (2026-10-12)
Ruling: Approved. Add a new file `src/LamuFlix.Api/EnrichmentStatusSchemaTransformer.cs` (`IOpenApiSchemaTransformer`), scoped to `EnrichmentStatus` only, that emits `type: string`, `enum [Pending, Enriched, NotFound, Failed]`. Register it with `AddSchemaTransformer` in the existing `AddOpenApi` options in `src/LamuFlix.Api/Program.cs`.
- The Q-ruling at brief.md:96-100 already approved the string-by-name wire contract. Recon-DEV-80:105-109 names a schema transformer as the supported mechanism, and Sentry F1 (High, accepted) forces the edit. That makes the extra file and the Program.cs edit a forced edit, in scope.
- No new dependency, project or layer (Microsoft.AspNetCore.OpenApi is already referenced). Runtime is unchanged and nothing goes to the user.
- Extend the brief.md:34-38 touch set with these two paths. Nothing else is added.

## R3 - ND-02 first-page constants and browse-query OpenAPI metadata (2026-10-12)
Ruling: Constants approved as taste (A6 [assumed]). Correcting the metadata is approved in principle. Its mechanism and touched files stay pending recon.
- Constants: `sort=Title&direction=Ascending&page=1&pageSize=20`. This is a taste choice inside A5 and brief.md:17 (no sort or paging UI). recon-DEV-80:123-125 proves it valid.
- Metadata: the committed OpenAPI shows optional PascalCase properties, but the server requires lowercase keys (recon-DEV-80:122-126). Correcting the document so it states what the server already enforces is not a new public API shape (§2.3 item 4). The contract chain (brief.md:81-91) and AC require it so generated client and openapi-msw handlers type the real wire keys. The route, accepted values, validation, DTOs and UI must not change.
- The concrete mechanism (attribute, parameter binding or transformer) and the files it touches are not in recon. Recon must establish them, and Keel brings them back to me before drafting. If the fix would change runtime binding or validation, it becomes a new ruling.

## R4 - ND-02 browse-query metadata mechanism and files (2026-10-12)
Ruling: Approved as Keel proposed. Add a new file `src/LamuFlix.Api/BrowseMoviesOperationTransformer.cs` (`IOpenApiOperationTransformer`), applied to the `BrowseMovies` operation only. Register it with `AddOperationTransformer` in the existing `AddOpenApi` options in `src/LamuFlix.Api/Program.cs`, beside the R2 schema transformer. It sets `sort`/`direction`/`page`/`pageSize` names, `Required = true`, string enum `Title|Year|Rating|Runtime` and `Ascending|Descending`, integer `page` minimum 1, and integer `pageSize` 1..100.
- This closes R3:19. recon-DEV-80-3:22-27 establishes the operation transformer as the only surface that carries casing, requiredness and wire types together, and it is metadata-only. Mechanism A (`[FromQuery]`/`[Required]` on `BrowseMoviesRequest.cs`) is rejected because it touches binding and its parameter-level `required` is unproven (recon-DEV-80-3:16-20).
- Microsoft.AspNetCore.OpenApi 10.0.12 is already referenced (recon-DEV-80-3:26), so there is no new dependency, project or layer. It is not a new API shape: the document now states what the server already enforces (LibraryEndpoints.cs:21-34, MovieQueryValidator.cs:10-13; R3:18). Request types, binding, validation, the route, the other 10 parameters and the UI stay unchanged.
- Proof goes in the existing `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` and adds no new test file. Extend the brief.md touch set with these paths. Any runtime change reopens this ruling.

