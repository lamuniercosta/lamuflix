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
