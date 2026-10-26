# DEV-422 — SignalR enrichment-status client brief

Date: 2026-10-26. Seat: Keel. Patron confirmed shared understanding in Q6.
Worktree: `F:/Dev/LamuFlix.worktrees/feature-422-spec`; branch: `feature/422-spec`.
Recon baseline: `52e443666b5e767edc93befc20eb55de2e77787b`.

## Status and owner gate

Grill complete; brief ready for Quill to draft spec, plan, and tasks. This is a conditional scope freeze, not Gate 1 clearance, implementation authorization, plan-challenge clearance, or a merge bar.

- [ ] **blocked: structural — replace ticket-mandated hand-typed hub payload types with the generated DEV-330 schema type?**

Carry that exact unanswered owner question into the spec and spec PR. DEV-422 note line 16 mandates hand-typed payload types, but DEV-330 FR-011 requires generated schema consumption. Patron Q2 recommends generated `EnrichmentStatusChangedDto`; only the owner can approve the ticket-text change. Gate 1 stays closed until answered. A Patron answer does not close this checkbox.

DEV-330's contract is frozen but unlanded on PR [#264](https://github.com/lamuniercosta/lamuflix/pull/264), commit `b565e932151027d8a681b8f00b70f155443f9be9`, branch `feature/330-spec`. Its own Gate 1 constitution checkbox remains open; ADR-0029 remains Proposed. No server implementation or generated hub schema exists at this recon baseline. Require upstream implementation and generated schema landing plus contract-drift verification before Phase B; do not treat PR objects as landed contracts.

## Evidence and recorded exchanges

Facts are from notes `DEV-422` and `recon-DEV-422`, including targeted contract evidence at lines 97–138. Full questions, recommendations, Patron answers, rationale, implications, and cited bases are preserved append-only in [CONCLUSIONS.md](CONCLUSIONS.md), Q1–Q6. This brief records every settled grill answer and the plan decisions; it introduces no independent repository measurements.

## Grill answers and frozen behavior

### Q1 — Scope and dependency

Implement the SPA enrichment refresh mechanism: pinned `@microsoft/signalr`, `useEnrichmentStatus`, one connection per SPA session, automatic reconnect and recovery, TanStack Query cache updates, removal of DEV-80 enrichment polling including optimistic import dependence, and Vitest connection-fake coverage. Preserve current Pending visuals and accessibility. Server hub implementation belongs to DEV-330. Adding the ticket-named npm dependency is approved; add no server NuGet package. ADR-0022 already accepts SignalR and supersedes ADR-0008 for this channel; its constitution-text cleanup stays out of scope. Q2 qualifies the original payload-type interpretation.

Basis: `DEV-422:14-18`; recon §§1, 6, 8; Patron Q1.

### Q2 — Contract and type provenance

Use `/hubs/enrichment`, event `EnrichmentStatusChanged`, exactly one positional argument with camelCase fields `{ id: number, enrichmentStatus: "Pending" | "Enriched" | "NotFound" | "Failed" }`. The server DTO unwraps its integer movie id; no timestamps, metadata, or error details are available. No client hub methods, groups, or subscriptions; single-instance broadcast audience.

Subject to the owner checkbox, consume generated `EnrichmentStatusChangedDto` from `web/src/api/types.ts` after upstream C# DTO → OpenAPI schema → generated TypeScript landing. Do not hand-write a duplicate or hand-edit generated files. Hub-only schema adds no MSW path handler. Test the connection with a fake, not MSW SignalR emulation.

Delivery follows committed actual status transitions, with sequential awaited post-commit sends and per-movie exception isolation. There is no replay, durable receipt, crash-window detection, or global event version. Lost events are an accepted upstream limitation; client recovery refetches authoritative HTTP state. Do not invent stronger cross-transaction ordering guarantees.

Basis: recon `102-126,129-134`; DEV-330 contract `enrichment-hub.md`, FR-001–FR-011; Patron Q2.

### Q3 — Placement and connection lifecycle

Place hook and narrow connection factory/owner seam in `web/src/library/useEnrichmentStatus.ts`. Mount once in the SPA shell under `main.tsx`'s root `QueryClientProvider`, surviving Library/Details route changes. No new top-level folder, state library, or architectural layer.

Register event/lifecycle handlers before starting; dispose handlers, stop the connection, and cancel timers/in-flight start ownership on cleanup. StrictMode mount/unmount/remount and late start completion must leave at most one active connection and no disposed-owner cache writes. Only one start/retry owner runs at a time.

Build `/hubs/enrichment` against `VITE_API_BASE_URL || window.location.origin`; configure `withCredentials: false`, with no added tokens/auth headers. This matches upstream DevSpa no-credentials policy. Use default `withAutomaticReconnect()` after connection. For failed initial starts or exhausted reconnect, use cancellable outer delays `1s, 2s, 4s, 8s, 16s`, then cap at `30s` until disposal. These timers retry the connection, never HTTP polling.

Basis: recon `44-45,58,80,102,105`; Patron Q3 and Q6. Retry policy is recorded `[assumed]` in ASSUMPTIONS.md.

### Q4 — Cache strategy and recovery

Validate payloads and ignore malformed events. For every valid event, patch only `enrichmentStatus` for matching existing items across root `['browseMovies']` variants with `setQueriesData`; never fabricate absent rows. Invalidate all browse variants for authoritative metadata, filter membership, and sorting. Handle concurrent HTTP/event races through invalidation/refetch, with no synthetic sequence numbers. Coalesce concurrent refresh work rather than create independent unbounded request loops. Do not touch recommendation caches.

Keep the separate details QueryClient. One-way root event/invalidation/recovery notifications reconcile mounted `['movieDetails', movieId]` through the nested client, without a second connection or converting the application to one client. Recovery must work when the id is absent from all browse pages and when a duplicate event or recovery carries the same status. Do not rely only on row status diffs or create a feedback cycle to root invalidation.

On every successful initial start and reconnect, invalidate root browse queries, including Pending movies, and reconcile mounted details even without a new event. This closes startup/disconnection loss windows through HTTP; it does not promise durable event delivery.

Basis: recon `39,44-50,110-126`; Patron Q4–Q5.

### Q5 — Polling removal and mutation races

Remove `browseRefetchInterval` and browse `refetchInterval`/`refetchIntervalInBackground` options. Preserve `pendingRow`, `markRowPending`, optimistic import placeholder semantics, and row-id reconciliation. Successful import/retry/request-enrichment/match mutations immediately invalidate `['browseMovies']`; subsequent refresh comes from event/start/reconnect invalidations. This must converge whether the event arrives before or after the mutation response, without restoring timers for HTTP requests.

Repurpose `libraryQueries.polling.test.tsx` and update `LibraryView.retry.test.tsx` in place; replace cadence assertions with no-polling and event/invalidation reconciliation coverage. Preserve `aria-live="polite"` and current status labels/visuals.

Basis: recon §§1–3, `20-25,84`; Patron Q5.

### Q6 — Dependency pin, file scope, and closure

Pin an exact published compatible `10.0.x` `@microsoft/signalr` version in package.json and lockfile; no caret/tilde. Verify the actual available patch at implementation pickup instead of inventing a version in Phase A. Dependency choice is settled; patch availability is a pickup fact.

Production file scope:

- `web/package.json`, `web/package-lock.json`.
- `web/src/library/useEnrichmentStatus.ts` (new hook and connection seam).
- `web/src/main.tsx`, `web/src/App.tsx`.
- `web/src/library/libraryQueries.ts`.
- `web/src/features/movie-details/movieDetailsQueries.ts`.
- `web/src/features/movie-details/MovieDetailsView.tsx`.
- Upstream generated `web/src/api/openapi.json` and `web/src/api/types.ts` pickup only through the contract generation chain.

Test scope: new `web/src/library/useEnrichmentStatus.test.tsx`; existing `web/src/library/libraryQueries.polling.test.tsx` and `web/src/library/LibraryView.retry.test.tsx`; focused details/App tests; `web/e2e/import.e2e.ts` only when required by the changed refresh behavior. Quill must name concrete focused test paths in the plan; additional behavioral decisions return as `needs decision:` rather than silently expanding scope. No file deletions or unrelated rewrites. No backend, DB schema, LocalPlay, secrets, Process.Start, new visual design, or new ADR work.

## Loop discipline

Closing bar for code-review/ship-review: High and Medium block; Low/Informational defer unless they demonstrate a concrete acceptance violation. Round cap: **two**. Owner checkbox resolution and upstream contract pickup remain independent prerequisites, not review dispositions.

Frozen scope: Q1–Q6 above, including the conditional generated-type choice and the enumerated production/test boundaries; anything else is a follow-up issue, not a finding in this round.

## Plan and task ordering

1. Carry the Q2 owner checkbox into Phase A artifacts and PR; before Phase B, verify its resolution, upstream DEV-330 Gate 1/implementation/generated-schema landing, and contract drift against main. Use the frozen PR only as planning evidence until landing.
2. Verify and pin the compatible published exact npm patch and lockfile.
3. Implement connection factory/owner seam, lifecycle hook, and root mounting with connection-fake tests.
4. Implement validated event cache updates, browse invalidation, details bridge, and start/reconnect reconciliation.
5. Remove enrichment polling; migrate optimistic import and retry/request/match reconciliation and repurpose tests.
6. Run web verification and report live-hub integration limitations explicitly.

Quill drafts spec.md, plan.md, and tasks.md from this brief; Keel analyzes those read-only against the brief before plan challenge. No production implementation is authorized by this document.

## Test strategy and gate expectations

Vitest + RTL uses a controllable connection fake, not a real network connection or MSW hub simulation. Cover:

- One active connection across routes and StrictMode; URL/no-credentials options; handler registration/disposal; failed starts; reconnect exhaustion/backoff; timer and late-start cleanup races.
- Validity checks, all four statuses, duplicates, absent rows, multiple filtered cache variants, metadata/filter/sort convergence, HTTP/event races, and no fabricated rows.
- Details id absent from browse caches, all transitions, same-status recovery, and one-way notification behavior without invalidation cycles.
- Event before mutation response and mutation before event, optimistic import/retry reconciliation, terminal metadata refresh, and no interval-driven browse requests.
- Existing Pending rendering and live-region behavior.

At implementation verification require `npm run lint` (including check:api, ESLint, Prettier), full Vitest suite, and production build from web/. Targeted import e2e uses the real landed hub when available; report unavailable integration as `Could not run`, never PASS. No backend mutation run is needed for a web-only diff; C# scope-empty gates, when executed, are `SKIPPED (scope-empty)`, never PASS. No gates were run during this grill/document phase.

Existing canonical vocabulary is retained: enrichment status, Pending, enrichment-status event, reconnect/refetch. No new domain term or glossary amendment was decided. Accepted ADR-0022 and the conditional upstream contract supply drafting context; do not treat Proposed ADR-0029 as Accepted.

## Phase 2 analyze decisions (2026-10-26)

Patron ratified these decisions in CONCLUSIONS.md, Phase 2 Analyze Rulings; they supplement Q1–Q6 without expanding delivery scope.

- propertyTests: opt-out — web-only SignalR connection lifecycle and TanStack Query cache orchestration; no new or changed domain invariants. Basis: task-pipeline Phase 3 step 5; brief Q1/Q6 and Test strategy. The full Vitest connection-fake lifecycle/cache/race matrix remains mandatory; add no property-testing dependency. Gauge records actual command/exit; an accepted opt-out is never PASS.
- Constitution governance remains authoritative. Accepted ADR-0022 records pending constitution amendment; it does not itself amend constitution polling text. DEV-330 carries the existing open constitution-departure owner gate and Proposed ADR-0029. DEV-422 Phase B additionally requires landed approved upstream constitution disposition/amendment alongside landed hub implementation/generated schema and contract-drift verification. No present constitutional-compliance clearance is claimed. Constitution editing stays outside DEV-422 scope; no duplicate owner checkbox is introduced. The DEV-422 payload-type owner checkbox remains open.

## Phase 2 plan-challenge decisions (2026-10-26)

Patron ratified the full exchange in CONCLUSIONS.md, Phase 2 Challenge Rulings. These decisions clarify Q1–Q6 within the existing file set; no new taste assumptions, owner checkbox, or implementation authorization.

1. **Freshness (Sentry M1)**: Coalescing must not drop a valid event arriving during an older HTTP refetch. Both browse and mounted details must complete a fresh authoritative pass after that event. T006/T007 prove R1 starts, E2 arrives, stale R1 completes, then fresh state converges. No synthetic server sequence numbers.
2. **Ownership and bridge (Sentry M2, Ledger M1/M2, Compass M1)**: Keep connection, timers, and callback-subscription registry per mounted root hook instance in `useEnrichmentStatus.ts`; no module-global mutable owner or registry. Inject the factory through a hook parameter/default seam; default factory helper stays private. Return a narrow subscription seam passed through existing `main.tsx` → `App.tsx` → movie-details props; events and start/reconnect recovery publish independently of browse-cache presence or status changes. Subscribe/unsubscribe in the existing movie-details files. No new context/provider/store/event-bus module, second connection, or reverse invalidation. T003 teardown asserts zero fake live connections and pending timers before restoring timers. Fake is hand-written, with no new package.
3. **Validation (Ledger M3, Sentry L4)**: Use existing Zod (`recon-DEV-422:63`), not another dependency or duplicate DTO. Require exactly one positional argument, non-null object, finite integer id, and four exact string statuses. T006 names null/missing/string/fractional/non-finite ids, unknown statuses, and zero/two-or-more arguments. Do not impose an uncontracted positive-only id rule.
4. **Pin and verification hygiene (Ledger M4)**: Use exact save (`--save-exact`) with no range in package.json or the lockfile root. Record post-pin `npm audit` results and disposition of introduced findings before clearance; do not blanket-fail unrelated historical advisories or alter thresholds. Record production-build output size delta against baseline, without new tooling or budget. Use browser runtime imports and `import type` for type-only symbols; confirm Node-only transports are absent from browser runtime output.
5. **Acceptance proofs (Compass L2/L3)**: T007 explicitly proves initial-start and reconnect reconciliation with no event, including absent-browse ids; T010 explicitly asserts `aria-live="polite"` remains green after polling removal.
6. **Failure behavior (Sentry L3)**: A failed authoritative refetch retains the event status patch; subsequent event/recovery refetch reconciles it. No rollback machinery.

Standing closing-bar dispositions: Ledger L5 same-reference/redundant-write optimization and L6 exported retry constants are deferred implementation detail, without weakening existing no-fabrication/multi-variant coverage. Sentry L1 extra debounce is deferred; L2 prolonged-outage staleness is accepted without polling or new UI; L5 minimal anonymous broadcast remains upstream-owned. Ledger L7 is clarified under item 2. All Sentry I1–I3 and Compass informational 4–6 are acknowledged without new scope: protocol/pin compatibility and broad browse-prefix invalidation remain pickup/implementation obligations already covered; HMR enhancement, task-label cleanup, and unrelated test rewrites are not added. These deferrals are `noted, no ticket` under Patron ruling.

One numbered Quill correction round incorporates these decisions into spec/plan/tasks. Keel verifies the returned documentation before plan freeze. The two-round cap, High/Medium closing bar, open payload-type owner checkbox, and all Phase B upstream prerequisites remain unchanged.

## Phase 2 closure and conditional plan freeze (2026-10-26)

Keel verified Quill correction round 1 against the six decisions above by reading the returned spec requirements, plan structure/design, task descriptions T002–T015, and numbered drafting receipt. All seven Medium findings are resolved in documentation; accepted Low acceptance clarifications are incorporated and remaining Low/Informational entries are disposed under the standing closing bar. No second correction round is needed. All 23 entries are recorded in PLAN_CHALLENGE_ADJUDICATION.md.

The plan is frozen for owner Gate 1 review with Q1–Q6 and the ratified Phase 2 supplements as its scope. This closure supersedes earlier draft-only/no-freeze status descriptions for the plan-challenge stage; historical drafting receipts remain historical. Ready for spec-PR preparation and owner review, not Gate 1 approval: the payload-type owner checkbox remains open. Phase B remains STOP until the recorded owner/upstream/landing/drift prerequisites are satisfied. No production implementation, gates, publication, merge bar, or merge is claimed.
