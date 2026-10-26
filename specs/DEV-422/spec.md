# Feature Specification: DEV-422 SignalR enrichment-status client hook

**Feature Branch**: `feature/422-spec`

**Created**: 2026-10-26

**Status**: gate1: provisional — Gate 1 closed until owner checkbox answered

**Input**: Ticket DEV-422 (size M, parent DEV-100; tags size:M, no ui tag); frozen grill brief `specs/DEV-422/brief.md` (grill closed Q6); facts from note `recon-DEV-422`; Patron rulings in `specs/DEV-422/CONCLUSIONS.md`; taste receipt `specs/DEV-422/ASSUMPTIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## Owner gate *(mandatory — Gate 1 stays closed)*

- [ ] **blocked: structural — replace ticket-mandated hand-typed hub payload types with the generated DEV-330 schema type?**

Carry that exact unanswered owner question into the spec PR. `DEV-422:16` mandates hand-typed hub payload types; DEV-330 FR-011 requires generated schema consumption. Patron Q2 recommends generated `EnrichmentStatusChangedDto`; only the owner can approve the ticket-text change. A Patron answer does not close this checkbox (brief Status and owner gate; CONCLUSIONS Q2).

## Domain vocabulary *(mandatory — retained terms)*

Existing canonical vocabulary is retained. No new domain term or glossary amendment was decided (brief Test strategy). Do not edit `CONTEXT.md`.

- **enrichment status** / **Pending** / **Enriched** / **NotFound** / **Failed**: Existing enrichment status names (generated `EnrichmentStatus` at `web/src/api/types.ts:83` per recon).
- **enrichment-status event**: Server-to-client `EnrichmentStatusChanged` with one positional payload.
- **reconnect/refetch**: On successful initial start and reconnect, invalidate browse (including Pending) and reconcile mounted details via HTTP; no durable event replay (ADR-0022; brief Q4).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Live enrichment status without polling (Priority: P1)

A user browsing the library sees enrichment status on Pending rows update when the server publishes an enrichment-status event, without timed HTTP polling of the browse list. One SignalR connection for the SPA session delivers `EnrichmentStatusChanged`; valid events patch matching existing browse rows and trigger authoritative HTTP refresh; malformed events are ignored. Pending visuals and the list live region stay as they are today.

**Why this priority**: Ticket and brief require replacing DEV-80 enrichment polling with the SignalR client refresh mechanism while preserving visuals (brief Q1, Q5; `DEV-422:14-18`).

**Independent Test**: Vitest + RTL with a controllable connection fake proves event-driven browse cache patch/invalidation for all four statuses, ignore of malformed payloads, no fabricated rows, and no interval-driven browse requests (brief Test strategy).

**Acceptance Scenarios**:

1. **Given** the SPA has an active enrichment hub connection and a browse cache contains a movie row, **When** a valid `EnrichmentStatusChanged` event arrives for that id with any of Pending, Enriched, NotFound, or Failed, **Then** that row's `enrichmentStatus` is updated in place across matching root `['browseMovies']` variants and browse queries are invalidated for authoritative metadata, filter membership, and sort order.
2. **Given** a valid event for a movie id that is not present in any cached browse page, **When** the event is handled, **Then** no new browse row is fabricated.
3. **Given** a malformed or invalid payload, **When** the handler runs, **Then** the event is ignored and browse caches are not patched from that payload.
4. **Given** enrichment status is refreshing via the hub, **When** the user views Pending rows, **Then** current status labels and `aria-live="polite"` list behavior remain unchanged (no visual redesign); focused proofs assert `aria-live="polite"` remains present after polling removal.
5. **Given** an older authoritative browse refetch (`R1`) is in flight, **When** a valid enrichment-status event (`E2`) arrives before `R1` completes, **Then** coalescing does not swallow `E2`'s invalidation: after stale `R1` completes, browse converges to a fresh authoritative pass driven by `E2` (no synthetic server sequence numbers).

---

### User Story 2 - One session connection with reconnect recovery (Priority: P1)

A user navigating Library and Details keeps a single enrichment hub connection for the SPA session. The connection uses `/hubs/enrichment` against the API base URL with credentials disabled, automatic reconnect after a successful start, and cancellable bounded outer retry when initial start fails or automatic reconnect is exhausted. On every successful initial start and reconnect, browse queries (including Pending) are invalidated and mounted details are reconciled even without a new event, so disconnect windows converge through HTTP rather than event replay.

**Why this priority**: Ticket requires one connection per session, auto-reconnect, and pending invalidation on reconnect (brief Q3–Q4; ADR-0022).

**Independent Test**: Connection-fake Vitest coverage for one active connection across routes and StrictMode, URL/no-credentials options, handler registration/disposal, failed starts, reconnect exhaustion/backoff, timer and late-start cleanup races, and start/reconnect invalidation (brief Test strategy; ASSUMPTIONS.md retry schedule).

**Acceptance Scenarios**:

1. **Given** the SPA shell mounts under the root QueryClientProvider, **When** the user moves between Library and Details, **Then** at most one enrichment hub connection remains active for the session.
2. **Given** StrictMode mount/unmount/remount or a late start completing after disposal, **When** cleanup runs, **Then** disposed owners write no cache updates, timers and in-flight start ownership are cancelled, and at most one active connection remains.
3. **Given** a successful initial start or a reconnect, **When** the connection becomes ready, **Then** root browse queries including Pending are invalidated and mounted details are reconciled even if no event arrives.
4. **Given** initial `start()` fails or automatic reconnect is exhausted, **When** outer retry runs, **Then** cancellable delays follow `1s, 2s, 4s, 8s, 16s` then cap at `30s` until disposal, retrying the connection only (never HTTP browse polling) [assumed schedule in ASSUMPTIONS.md].

---

### User Story 3 - Details page stays current via one-way bridge (Priority: P2)

A user on a movie details page sees enrichment status converge when events, connect, or reconnect recovery fire, even if that movie id is absent from every cached browse page, and even when a duplicate or same-status notification arrives. The nested details QueryClient stays; a one-way notification from the root connection reconciles mounted `['movieDetails', movieId]` without a second hub connection and without a feedback cycle into root invalidation.

**Why this priority**: Nested details client exists today; brief freezes keeping it while bridging recovery (brief Q4–Q5; recon nested-client facts).

**Independent Test**: Focused details proofs for id absent from browse caches, all status transitions, same-status recovery, and one-way notification without invalidation cycles (brief Test strategy; concrete path in plan).

**Acceptance Scenarios**:

1. **Given** Details is open for a movie id not present in any `['browseMovies']` cache page, **When** a valid enrichment-status event for that id occurs, **Then** mounted `['movieDetails', movieId]` is reconciled through the nested client.
2. **Given** Details is open for a movie id absent from all browse caches, **When** successful initial start or reconnect recovery completes with no enrichment-status event, **Then** mounted `['movieDetails', movieId]` still reconciles through the nested client.
3. **Given** a duplicate event or recovery carrying the same status already shown, **When** the bridge runs, **Then** details still reconcile without depending only on row status diffs or browse-cache presence/status changes.
4. **Given** an older details refetch (`R1`) is in flight for the mounted id, **When** a valid event (`E2`) arrives before `R1` completes, **Then** coalescing does not swallow `E2`: after stale `R1` completes, mounted details converge to a fresh authoritative pass (no synthetic server sequence numbers).
5. **Given** the root connection handles events and recovery, **When** details invalidates, **Then** there is no second hub connection and no cycle that re-triggers root browse invalidation from the details bridge alone.

---

### User Story 4 - Import and retry converge without browse polling (Priority: P2)

A user who imports a folder or retries/request-enriches/matches a movie still sees optimistic Pending row behavior, but browse refresh no longer uses `browseRefetchInterval` or browse refetch-interval options. Successful import/retry/request-enrichment/match mutations immediately invalidate `['browseMovies']`; further refresh comes from events and start/reconnect invalidations, converging whether the event arrives before or after the mutation response.

**Why this priority**: Ticket depends on removing DEV-80 polling while preserving import/retry semantics (brief Q5; `DEV-422:17`).

**Independent Test**: Repurposed `libraryQueries.polling.test.tsx` and updated `LibraryView.retry.test.tsx` prove no interval-driven browse requests, event-before-mutation and mutation-before-event convergence, optimistic import/retry reconciliation, and terminal metadata refresh (brief Q5; Test strategy).

**Acceptance Scenarios**:

1. **Given** browse previously polled while Pending, **When** this feature ships, **Then** `browseRefetchInterval` and browse `refetchInterval` / `refetchIntervalInBackground` options are gone and no timed browse polling remains for enrichment status.
2. **Given** a successful import, retry, request-enrichment, or match mutation, **When** the mutation succeeds, **Then** `['browseMovies']` is invalidated immediately while `pendingRow`, `markRowPending`, and optimistic import placeholder / row-id reconciliation semantics remain.
3. **Given** an enrichment-status event may arrive before or after the mutation response, **When** both paths complete, **Then** browse state converges without restoring HTTP polling timers.

### Edge Cases

- Lost events are an accepted upstream limitation; client recovery is HTTP refetch on start/reconnect, not durable replay (brief Q2, Q4; ADR-0022).
- Payload has no title/poster/filter/error fields; every valid event still invalidates browse for authoritative metadata/membership/ordering (brief Q4).
- Coalesce concurrent refresh work; do not create independent unbounded request loops. A valid event arriving during an older in-flight refetch must still cause a fresh authoritative pass after that event; coalescing must not drop its invalidation (brief Q4; Phase 2 plan-challenge item 1).
- When an authoritative refetch fails, retain the in-place event status patch; converge on a subsequent event or start/reconnect recovery refetch. No rollback machinery (brief Phase 2 plan-challenge item 6).
- Do not touch recommendation caches (brief Q4).
- Hub is server-to-client only: no client hub methods, groups, or subscriptions (brief Q2).
- Subject to the owner checkbox: consume generated `EnrichmentStatusChangedDto` after DEV-330 lands; do not hand-write a duplicate or hand-edit generated files (brief Q2). Until the owner answers, Gate 1 stays closed.
- DEV-330 hub contract is frozen but unlanded (PR #264 / `b565e932`); Phase B requires landed approved upstream constitution disposition/amendment, hub implementation/generated-schema landing, and contract-drift verification; do not treat PR objects as landed contracts (brief Status; Q2; plan ordering step 1; Phase 2 Analyze Rulings).
- Constitution remains authoritative. Accepted ADR-0022 accepts SignalR transport and notes a pending constitution amendment; it does not supersede constitution polling text. Upstream DEV-330 constitution-departure owner checkbox and Proposed ADR-0029 remain unresolved. Constitution-text editing stays out of DEV-422 scope; no duplicate constitution checkbox on DEV-422; DEV-422 payload-type checkbox stays open (brief Phase 2 analyze decisions; CONCLUSIONS Phase 2 Analyze Rulings).
- No backend, DB schema, LocalPlay, secrets, Process.Start, new visual design, or new ADR work (brief Q6).
- Targeted import e2e uses the real landed hub when available; otherwise report `Could not run`, never PASS (brief Test strategy).
- Web-only diff: C# scope-empty gates, when executed, are `SKIPPED (scope-empty)`, never PASS (brief Test strategy).
- **propertyTests: opt-out — web-only SignalR connection lifecycle and TanStack Query cache orchestration; no new or changed domain invariants** (brief Phase 2 analyze decisions; CONCLUSIONS Phase 2 Analyze Rulings). Vitest matrix remains mandatory; opt-out is never PASS; no new property-testing library.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Implement the SPA enrichment refresh mechanism: pinned `@microsoft/signalr`, `useEnrichmentStatus`, one connection per SPA session, automatic reconnect and recovery, TanStack Query cache updates, removal of DEV-80 enrichment polling including optimistic import dependence on browse intervals, and Vitest connection-fake coverage. Preserve current Pending visuals and accessibility. Server hub implementation belongs to DEV-330. Adding the ticket-named npm dependency is approved; add no server NuGet package. Accepted ADR-0022 accepts SignalR transport and supersedes ADR-0008 for the push channel while noting a pending constitution amendment; constitution polling text remains authoritative until that amendment lands. Constitution-text editing stays out of DEV-422 scope (brief Q1; Phase 2 Analyze Rulings).
- **FR-002**: Carry owner checkbox `blocked: structural — replace ticket-mandated hand-typed hub payload types with the generated DEV-330 schema type?` into Phase A artifacts and the spec PR. Subject to that answer, consume generated `EnrichmentStatusChangedDto` from `web/src/api/types.ts` after upstream C# DTO → OpenAPI → generated TypeScript landing. Do not hand-write a duplicate or hand-edit generated files. Hub-only schema adds no MSW path handler. Test the connection with a fake, not MSW SignalR emulation (brief Q2).
- **FR-003**: Use hub route `/hubs/enrichment`, event `EnrichmentStatusChanged`, exactly one positional argument with camelCase fields `{ id: number, enrichmentStatus: "Pending" | "Enriched" | "NotFound" | "Failed" }`. No client hub methods, groups, or subscriptions; single-instance broadcast audience. Do not invent stronger cross-transaction ordering or durable delivery guarantees than upstream provides (brief Q2).
- **FR-004**: Place hook in `web/src/library/useEnrichmentStatus.ts`. Keep connection, timers, and callback-subscription registry state per mounted root hook instance — no module-global mutable owner or registry. Inject the connection factory through a hook parameter/default seam; the default factory helper stays private in that file. Return a narrow subscription seam from the root hook and pass it through existing `main.tsx` → `App.tsx` → movie-details props. Mount once under `main.tsx`'s root `QueryClientProvider`, surviving Library/Details route changes. No new context/provider/store/event-bus module, top-level folder, state library, or architectural layer (brief Q3; Phase 2 plan-challenge item 2).
- **FR-005**: Register event/lifecycle handlers before starting; dispose handlers, stop the connection, cancel timers/in-flight start ownership, and unsubscribe subscription consumers on cleanup. StrictMode and late-start races must leave at most one active connection and no disposed-owner cache writes. Only one start/retry owner runs at a time (brief Q3; Phase 2 plan-challenge item 2).
- **FR-006**: Build `/hubs/enrichment` against `VITE_API_BASE_URL || window.location.origin`; configure `withCredentials: false`, with no added tokens/auth headers. Use default `withAutomaticReconnect()` after connection. For failed initial starts or exhausted reconnect, use cancellable outer delays `1s, 2s, 4s, 8s, 16s`, then cap at `30s` until disposal — connection retries only, never HTTP polling [assumed] (brief Q3; ASSUMPTIONS.md).
- **FR-007**: Validate runtime hub invocations with existing Zod (`recon-DEV-422:63`): require exactly one positional argument, a non-null object, a finite integer `id` (consistent with upstream C# `int`; no uncontracted positive-only id rule), and one of the four exact status strings. Ignore malformed events — explicit matrix includes null/missing/string/fractional/non-finite `id`, unknown status, and zero or two-or-more arguments. Do not add another validation dependency or a duplicate DTO. For every valid event, patch only `enrichmentStatus` for matching existing items across root `['browseMovies']` variants with `setQueriesData`; never fabricate absent rows. Invalidate all browse variants for authoritative metadata, filter membership, and sorting. Handle concurrent HTTP/event races through invalidation/refetch. Coalesce concurrent refresh work, but a valid event arriving during an older in-flight refetch must still cause a fresh authoritative pass after that event — coalescing must not drop its invalidation. When an authoritative refetch fails, retain the in-place event status patch and converge on a subsequent event or start/reconnect recovery refetch; no rollback machinery. Do not touch recommendation caches (brief Q4; Phase 2 plan-challenge items 1, 3, 6).
- **FR-008**: Keep the separate details QueryClient. Subscribe/unsubscribe via the narrow root subscription seam in existing movie-details files; publish valid events and start/reconnect recovery independently of browse-cache presence or status changes. One-way notifications reconcile mounted `['movieDetails', movieId]` through the nested client, without a second connection, reverse invalidation into root, or converting the application to one client. Recovery must work when the id is absent from all browse pages, on successful initial start and reconnect with no event, and when a duplicate/same-status notification arrives. Do not rely only on row status diffs. A valid event during an older details refetch must still yield a fresh authoritative details pass after that event (brief Q4–Q5; Phase 2 plan-challenge items 1, 2, 5).
- **FR-009**: On every successful initial start and reconnect, invalidate root browse queries, including Pending movies, and reconcile mounted details even without a new event (brief Q4).
- **FR-010**: Remove `browseRefetchInterval` and browse `refetchInterval`/`refetchIntervalInBackground` options. Preserve `pendingRow`, `markRowPending`, optimistic import placeholder semantics, and row-id reconciliation. Successful import/retry/request-enrichment/match mutations immediately invalidate `['browseMovies']`; subsequent refresh comes from event/start/reconnect invalidations. Converge whether the event arrives before or after the mutation response, without restoring timers for HTTP browse polling (brief Q5).
- **FR-011**: Repurpose `web/src/library/libraryQueries.polling.test.tsx` and update `web/src/library/LibraryView.retry.test.tsx` in place; replace cadence assertions with no-polling and event/invalidation reconciliation coverage. Preserve `aria-live="polite"` and current status labels/visuals; proofs must concretely assert `aria-live="polite"` remains green after polling removal (brief Q5; Phase 2 plan-challenge item 5).
- **FR-012**: Pin an exact published compatible `10.0.x` `@microsoft/signalr` version with `--save-exact` in `web/package.json` and lockfile root; no caret/tilde ranges. Verify the actual available patch at implementation pickup — do not invent a version in Phase A. After pin, run existing `npm audit`, record DEV-422-introduced findings and disposition without blanket-failing unrelated historical advisories or altering thresholds. Use browser runtime imports and `import type` for type-only symbols; confirm Node-only transports are absent from browser runtime output. Record production-build output size delta against baseline without new tooling or budget (brief Q6; Phase 2 plan-challenge item 4).
- **FR-013**: Production file scope is exactly: `web/package.json`, `web/package-lock.json`; `web/src/library/useEnrichmentStatus.ts` (new); `web/src/main.tsx`, `web/src/App.tsx`; `web/src/library/libraryQueries.ts`; `web/src/features/movie-details/movieDetailsQueries.ts`; `web/src/features/movie-details/MovieDetailsView.tsx`; upstream generated `web/src/api/openapi.json` and `web/src/api/types.ts` pickup only through the contract generation chain. Test scope: new `web/src/library/useEnrichmentStatus.test.tsx`; existing `libraryQueries.polling.test.tsx` and `LibraryView.retry.test.tsx`; focused details/App tests at `web/src/features/movie-details/MovieDetailsView.test.tsx` and `web/src/App.test.tsx`; `web/e2e/import.e2e.ts` only when required by the changed refresh behavior. No file deletions or unrelated rewrites (brief Q6; concrete focused paths named in plan).
- **FR-014**: Vitest + RTL uses a hand-written controllable connection fake (no new package), not a real network connection or MSW hub simulation. Cover the matrix in brief Test strategy (connection lifecycle including teardown of zero fake live connections/pending timers before restoring fake timers; payload validity matrix; statuses/duplicates/absent rows/filter variants; R1-start/E2/stale-R1/fresh-convergence races for browse and details; details bridge including no-event start/reconnect for absent-browse ids; mutation races; Pending rendering and concrete `aria-live="polite"` assertion) (brief Test strategy; Phase 2 plan-challenge items 1–5).
- **FR-015**: At implementation verification require `npm run lint` (including check:api, ESLint, Prettier), full Vitest suite, and production build from `web/`. Record post-pin `npm audit` disposition for introduced findings and production-build size delta as in FR-012. Targeted import e2e uses the real landed hub when available; report unavailable integration as `Could not run`, never PASS. No backend mutation run is needed for a web-only diff; C# scope-empty gates, when executed, are `SKIPPED (scope-empty)`, never PASS. **propertyTests: opt-out — web-only SignalR connection lifecycle and TanStack Query cache orchestration; no new or changed domain invariants** — Gauge records actual command/exit; opt-out is never PASS; add no property-testing library (brief Test strategy; Phase 2 analyze decisions; CONCLUSIONS Phase 2 Analyze Rulings; Phase 2 plan-challenge item 4).
- **FR-016**: Loop discipline — Closing bar for code-review/ship-review: High and Medium block; Low/Informational defer unless they demonstrate a concrete acceptance violation. Round cap: **two**. Owner checkbox resolution and upstream contract pickup remain independent prerequisites, not review dispositions. Frozen scope: Q1–Q6 including the conditional generated-type choice and the enumerated production/test boundaries; anything else is a follow-up issue, not a finding in this round (brief Loop discipline).
- **FR-017**: Phase B prerequisites before production implementation: owner checkbox answered; landed approved upstream DEV-330 constitution disposition/amendment; hub implementation/generated-schema landing; contract-drift verification against main. Use the frozen DEV-330 PR only as planning evidence until landing. Do not invent a duplicate constitution owner checkbox; constitution editing stays out of scope (brief plan ordering step 1; Phase 2 Analyze Rulings).

### Key Entities

- **useEnrichmentStatus**: SPA-shell hook owning per-mounted-instance hub connection, timers, callback-subscription registry, and root cache updates for enrichment-status events.
- **Connection factory seam**: Hook-parameter/default seam in the same file; default factory helper stays private; builds/owns the hub connection for Vitest fakes.
- **Subscription seam**: Narrow subscribe/unsubscribe return from the root hook, passed through existing shell/App/details props; publishes valid events and start/reconnect recovery independently of browse-cache presence/status.
- **EnrichmentStatusChangedDto**: Upstream payload type `{ id, enrichmentStatus }` — generated consumption subject to owner checkbox; no duplicate hand-written DTO.
- **browseMovies cache**: Root TanStack Query keys under `['browseMovies']` patched and invalidated on events/recovery.
- **Details bridge**: One-way notification path from root recovery/events into nested `['movieDetails', movieId]` invalidation via the subscription seam; no second connection or reverse cycle.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users see enrichment status on library rows update from live events without timed browse polling while Pending.
- **SC-002**: After a disconnect or cold start, users' browse (including Pending) and open details converge again once the hub connection starts or reconnects, without requiring a full page reload solely for missed events.
- **SC-003**: Users keep current Pending labels and polite live-region behavior; no visual redesign of Pending rows.
- **SC-004**: Import and retry/request/match flows still show optimistic Pending behavior and converge with live events whether the event or the mutation response arrives first.
- **SC-005**: A single SPA session does not leave multiple concurrent enrichment hub connections active across ordinary Library/Details navigation and StrictMode remounts under test.
- **SC-006**: Implementation verification records honest lint/Vitest/build results; unavailable live-hub e2e is `Could not run` (never PASS); web-only C# scope-empty gates are `SKIPPED (scope-empty)` (never PASS).

## Assumptions

- Outer connection start / post-close retry schedule is Patron taste/ops assumption tagged `[assumed]` in `specs/DEV-422/ASSUMPTIONS.md` (brief Q3, Q6): default `withAutomaticReconnect()` delays unchanged; outer retry `1000ms, 2000ms, 4000ms, 8000ms, 16000ms`, then capped at `30000ms` until dispose.
- Exact published `10.0.x` `@microsoft/signalr` patch is a Phase B pickup fact, not invented in Phase A (brief Q6).
- Generated `EnrichmentStatusChangedDto` consumption is conditional on the owner checkbox; Gate 1 stays closed until answered (brief Status).
- DEV-330 hub implementation and generated OpenAPI/types are absent at recon baseline and remain Phase B STOP prerequisites alongside landed approved upstream constitution disposition/amendment (brief Status; recon §§4–5, 129–133; Phase 2 Analyze Rulings).
- No new CONTEXT.md glossary terms; Accepted ADR-0022 supplies transport context only (pending constitution amendment noted); Proposed ADR-0029 not treated as Accepted; constitution remains authoritative (brief Test strategy; Phase 2 Analyze Rulings).
- No ui-tag / no Palette `design.md` requirement: visual Pending-row changes are out of scope (`DEV-422:18`; no ui tag on ticket).
- Exclusions in brief Q6 (backend, schema, LocalPlay, secrets, Process.Start, new visual design, new ADR, file deletions) remain out of scope.
- propertyTests opt-out is recorded as above; Vitest matrix remains mandatory (brief Phase 2 analyze decisions).
