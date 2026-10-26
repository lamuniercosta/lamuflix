# Feature Specification: DEV-330 Enrichment status SignalR push

**Feature Branch**: `feature/330-spec`

**Created**: 2026-10-26

**Status**: gate1: provisional — Gate 1 closed (owner structural checkbox open)

**Input**: Closed grill brief `specs/DEV-330/brief.md` (Q1–Q9), append-only `CONCLUSIONS.md`, Proposed ADR `docs/adr/0029-enrichment-status-post-commit-notifier.md`, accepted ADR-0022, notes `recon-DEV-330` and `recon-DEV-330-2`, task note `DEV-330`, `specs/PRODUCT.md`. No new decisions taken here; gaps go to `DRAFTING_RECEIPT.md` as `needs decision:` lines for Keel.

Ticket: DEV-330 — Add EnrichmentHub in Api that broadcasts enrichment status changes over SignalR. Scope: hub at `/hubs/enrichment`; Core port `IEnrichmentStatusNotifier` plus Api implementation; CORS for `http://localhost:5173`; unit and integration tests. Acceptance: one `EnrichmentStatusChanged` per committed change; no message on failed save; CORS preflight succeeds. Out of scope: React hook (DEV-422), scale-out backplane. Design fixed by ADR-0022 (DEV-421).

## Owner question — Gate 1 stays closed

- [ ] **blocked: structural — Approve Constitution v1.5.0 MINOR amendment (add IEnrichmentStatusNotifier to the Principle I closed port list, update polling-only enrichment status wording per ADR-0022, and update the architecture port whitelist) plus ADR-0029 for the notifier port and post-commit semantics?**

Carried verbatim from `brief.md`. Patron framed it; only the user answers. Proposed amendment text: `constitution-amendment-v1.5.0.md`. ADR-0029 remains Proposed until this gate is satisfied. No noncompliant implementation is authorized in advance.

## Clarifications

### Session 2026-10-26

- Q: Are any critical ambiguities left after the closed grill? → A: None. Q1–Q9 in `brief.md` / `CONCLUSIONS.md` settle scope, event boundary, delivery/failure/cancellation, wire contract, architecture, test dependency, CORS, task order, and loop discipline. Clarify run recorded no further questions.
- Plan challenge Round 1 (PC1/PC2 in `brief.md` / `CONCLUSIONS.md`): required repository notifier dependency + `CancellationToken.None` + per-movie repository isolation; sequential awaited attempts with no application-level timeout/fan-out; adapter attempt-start/failure logs (movie id + status) without durable crash-window detection; document transformer retained vs discovered-type schema transformer; frozen Api root paths; Ledger M2 Follow-up (noted, no ticket). Owner checkbox unchanged.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Live enrichment status after a committed change (Priority: P1)

A connected client receives exactly one `EnrichmentStatusChanged` notification when a movie’s enrichment status actually transitions and that transition is successfully persisted — including a new movie inserted as Pending — and receives nothing when a save fails or the status did not change.

**Why this priority**: Ticket AC1 and AC2. Without correct post-commit emission, push is either noisy or silent on the paths that matter.

**Independent Test**: Persistence-focused integration tests with real database/Testcontainers and an explicit `IEnrichmentStatusNotifier` port double prove insert-Pending with assigned id; actual transitions; no-op/unrelated saves; failed save; failed enclosing transaction commit or rollback after successful intermediate SaveChanges (emits zero); duplicate-IMDb recovery (Failed only after successful recovery commit); repeated saves without duplicate notifications; multiple in-memory status mutations emit only for persisted-before versus final committed-after; multi-movie save once each; cancellation before vs after commit; a throwing notifier double that still leaves the committed save successful and continues attempts for every remaining transitioned movie; and emission from a direct non-handler caller of `IMovieRepository` (repository-boundary proof, independent of MediatR handlers). Adapter mapping/broadcast and send-failure isolation unit tests belong with hub/adapter delivery (US2), after the real Api notifier exists.

**Acceptance Scenarios**:

1. **Given** a movie whose enrichment status has not yet been persisted (new insert) or whose persisted status differs from the status about to commit, **When** the enclosing movie persistence transaction commits successfully, **Then** exactly one `EnrichmentStatusChanged` is broadcast for that movie with its assigned id and committed status name.
2. **Given** a save that fails before commit, a Pending-to-Pending no-op, an unrelated movie update, or a failed first save on the duplicate-IMDb path, **When** those outcomes occur, **Then** no `EnrichmentStatusChanged` is emitted for that attempt; a successful recovery transition to Failed emits exactly one event.
3. **Given** a successful commit followed by a send failure, a throwing notifier, or a caller cancellation after commit, **When** the repository post-commit dispatch runs with `CancellationToken.None` and per-movie isolation, **Then** the committed save outcome is unchanged, failures are logged and isolated per movie, remaining transitioned movies still receive attempts, and the send attempt is not silently skipped solely because the caller token canceled.

---

### User Story 2 - Hub reachable under existing SPA CORS (Priority: P1)

The Vite origin `http://localhost:5173` can negotiate and use the enrichment hub under the existing credential-free `DevSpa` CORS policy; other origins do not receive allow-origin permission.

**Why this priority**: Ticket AC3 and Scope CORS. A hub that browsers cannot negotiate is unusable for the SPA.

**Independent Test**: Adapter unit tests cover DTO mapping/broadcast and send-exception isolation after the real Api notifier exists. Integration tests cover allowed OPTIONS preflight on `/hubs/enrichment/negotiate`, rejected non-5173 origin (missing allow-origin), and successful negotiation plus live delivery via the official SignalR client against the test host with real notifier DI.

**Acceptance Scenarios**:

1. **Given** `Origin: http://localhost:5173`, **When** the browser sends OPTIONS to `/hubs/enrichment/negotiate`, **Then** the preflight succeeds under the existing DevSpa policy (retained origin, any method/header, no credentials), and exposed `Location` is verified on the appropriate actual CORS response for that policy — not asserted as a preflight-only header claim.
2. **Given** a non-5173 origin, **When** a hub CORS request is made, **Then** the response carries no `Access-Control-Allow-Origin` for that origin (browser policy absence, not a newly invented authorization response).
3. **Given** an allowed client, **When** it negotiates and a committed status change is broadcast, **Then** the client receives the framed event with camelCase `id` (int) and `enrichmentStatus` as one of the four string names.

---

### User Story 3 - Generated TypeScript contract for the hub payload (Priority: P2)

The C# hub DTO is exposed in OpenAPI `components.schemas` (no new REST operation), regenerating `web/src/api/openapi.json`, its Verify snapshot, and `web/src/api/types.ts` through existing commands so DEV-422 consumes a generated type.

**Why this priority**: Contract-chain rule (C# DTO → OpenAPI → generated TypeScript). Push without a typed payload leaves DEV-422 inventing shapes.

**Independent Test**: OpenAPI contract tests assert required fields, four-name string enum, and unchanged exact REST operations; `LAMUFLIX_REGENERATE_OPENAPI=true` then `npm run generate:api` / `check:api` prove generated artifacts; no hand-edits.

**Acceptance Scenarios**:

1. **Given** the C# `EnrichmentStatusChangedDto`, **When** the OpenAPI document is generated, **Then** `components.schemas` includes the DTO with required `id` and `enrichmentStatus` and the four string enum names, and `ExpectedOperations` remains unchanged.
2. **Given** the regenerated baseline, **When** `npm run generate:api` runs, **Then** `types.ts` exposes the components schema type and no new MSW path handler is required for the hub-only schema.
3. **Given** committed generated artifacts, **When** drift checks run, **Then** they pass without hand-edited generated files.

---

### Edge Cases

- Multiple in-memory mutations before one save: only persisted-before vs final committed-after status decides emission; intermediate states do not emit.
- One `SaveChanges` affecting several movies: each movie with an actual status transition emits exactly once; unchanged movies emit nothing.
- Intermediate `SaveChangesAsync` inside an uncommitted outer transaction is not a notification boundary; the port runs only after the actual enclosing persistence transaction commits.
- Failed enclosing transaction commit or rollback after a successful intermediate `SaveChanges` emits zero notifications for that attempt.
- Repeated saves of an already-committed status do not duplicate notifications.
- Failed-attempt transitions cleared before recovery so a ghost Pending event cannot leak after a failed first save.
- Unused handlers (`ApplyEnrichmentResult`, `RecordEnrichmentFailure`, `ClaimEnrichment`) stay unregistered; repository coverage still notifies any caller that commits through `IMovieRepository`, including a direct non-handler repository caller used in tests.
- No client-to-server hub methods, groups, or subscriptions; broadcast is to all connected clients on the single Api instance.
- No replay, durable notification outbox, backplane, timestamps, metadata, or error details in the payload.
- `IEnrichmentStatusNotifier` is a required `EfMovieRepository` constructor dependency; fixtures supply explicit doubles; no optional/no-op production constructor fallback. Live Api DI must resolve `SignalREnrichmentStatusNotifier`.
- Post-commit calls pass `CancellationToken.None`. Repository dispatch catches/logs per transitioned movie and continues remaining movies; adapter also isolates send failures and logs attempt start/failure with movie id and committed status.
- Sequential awaited one-shot attempts have no application-level timeout or fan-out bound; unbounded post-commit latency is accepted for this single-instance ticket.
- Process death between commit and send can lose the event with no durable crash-window detection; reconnect/refetch convergence belongs to DEV-422.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST map SignalR hub `EnrichmentHub` at `/hubs/enrichment` in the Api host (ticket Scope; ADR-0022).
- **FR-002**: System MUST expose server-to-client event `EnrichmentStatusChanged` with exactly one argument `EnrichmentStatusChangedDto(int Id, EnrichmentStatus EnrichmentStatus)`, serialized camelCase as `id` (int) and `enrichmentStatus` (`Pending` | `Enriched` | `NotFound` | `Failed` only), never numeric or a SmartEnum object (Q4).
- **FR-003**: System MUST unwrap `MovieId` to `int Id` at the Api boundary and MUST NOT place SignalR or Api DTO types in Core (Q4, Q5).
- **FR-004**: Core MUST define port `IEnrichmentStatusNotifier` with `Task NotifyAsync(MovieId movieId, EnrichmentStatus status, CancellationToken cancellationToken)` under `src/LamuFlix.Core/Ports/` (Q5; ticket Scope).
- **FR-005**: `EfMovieRepository` MUST detect persisted-before vs committed-after enrichment status, retain assigned insert ids, clear failed-attempt transitions before recovery, require `IEnrichmentStatusNotifier` in its constructor (explicit fixture doubles; no optional/no-op fallback), and invoke the port only after the actual enclosing persistence transaction commits — once per changed movie, sequentially awaited, passing `CancellationToken.None`, catching/logging per-movie notifier exceptions and continuing remaining movies (Q2, Q5; PC1).
- **FR-006**: System MUST emit nothing for Pending-to-Pending no-ops, unchanged status, unrelated movie updates, failed saves, and cancellation that aborts persistence (Q2, Q3; AC2).
- **FR-007**: Api MUST implement `SignalREnrichmentStatusNotifier` using `IHubContext<EnrichmentHub>` / `Clients.All`, configuring SignalR JSON name-converter semantics independently of HTTP JSON; MUST log attempt start and failure with movie id and committed status; MUST isolate send failures so they cannot fail an already committed save or trigger enrichment retry; MUST retain sequential awaited ephemeral attempts with no application-level timeout or fan-out bound (Q3, Q4, Q5; PC1).
- **FR-008**: After a successful commit, caller cancellation MUST NOT silently skip the send attempt; the post-commit call site MUST pass `CancellationToken.None` independently of the caller token (Q3; PC1).
- **FR-009**: Api DI MUST resolve the real `SignalREnrichmentStatusNotifier` in the live host (named integration proof); repository fixtures MUST supply explicit doubles; optional/no-op production constructor fallback is forbidden (Q5; PC1).
- **FR-010**: System MUST retain existing `DevSpa` CORS exactly (`http://localhost:5173`, any method/header, exposed `Location` on the appropriate actual CORS response, no credentials) for the hub and negotiation endpoint; MUST NOT add origins, authentication, authorization, or `AllowCredentials` (Q7; AC3; PC2).
- **FR-011**: System MUST expose the C# DTO via an OpenAPI document transformer into `components.schemas` without adding a REST operation, then regenerate `web/src/api/openapi.json`, Verify snapshot, and `types.ts` through existing workflows; generated artifacts MUST NOT be hand-edited (Q4).
- **FR-012**: Integration tests MAY add test-only `Microsoft.AspNetCore.SignalR.Client` 10.0.14 via `Directory.Packages.props` in the existing integration test project; MUST NOT add a server SignalR NuGet or npm package in this ticket (Q6).
- **FR-013**: Phase B MUST apply the owner-approved Constitution v1.5.0 amendment and architecture whitelist update in the same delivery as the port; until Gate 1 is answered, noncompliant implementation is unauthorized (Q1).
- **FR-014**: System MUST NOT add handler-specific notification calls, event bus, EF interceptor, notification outbox, replay, backplane, database schema changes, new projects/layers, React client work, LocalPlay/secrets/`Process.Start` changes, or unrelated file rewrites (Q5, Q8, Q9; frozen scope).

### Key Entities

- **Enrichment status transition**: Persisted-before vs committed-after `EnrichmentStatus` for a movie; insertion into Pending counts as a transition.
- **EnrichmentStatusChanged notification**: Ephemeral post-commit broadcast of movie id and committed status name; not durable state.
- **EnrichmentHub**: Server hub surface at `/hubs/enrichment`; server-to-client only.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For every verified committed actual status transition in the test matrix, observers record exactly one `EnrichmentStatusChanged` with correct id and status name (AC1).
- **SC-002**: Failed saves, no-op Pending saves, unrelated updates, and pre-commit cancellation produce zero notifications for those attempts (AC2).
- **SC-003**: Allowed-origin OPTIONS preflight on `/hubs/enrichment/negotiate` succeeds; disallowed origin lacks allow-origin permission; negotiation and live delivery succeed for an allowed client (AC3).
- **SC-004**: OpenAPI components schema for the DTO carries required fields and the four string names; exact REST operation set is unchanged; generated `types.ts` / drift checks pass without hand-edits.
- **SC-005**: A notifier send failure or throwing notifier after commit leaves the persisted save successful, does not retry enrichment, and does not skip attempts for remaining transitioned movies; post-commit caller cancellation still attempts send via `CancellationToken.None`.
- **SC-006**: Applicable Phase B analyzer, architecture, unit/integration, property, vulnerable-package, format, mutation, and web/generated-contract gates are run under harness configuration and reported honestly (SKIPPED/SKIP/N/A/Could not run never folded into PASS).

## Assumptions

- No taste rulings and no new glossary terms; no `ASSUMPTIONS.md` or `CONTEXT.md` edits (brief Q9).
- Single-user, single Api instance; reconnect/refetch belongs to DEV-422 under ADR-0022.
- Ticket dependencies DEV-421, DEV-310, DEV-314, DEV-320 are named by the ticket; this spec does not claim live prerequisite completion (brief Identity).
- SignalR Client package version 10.0.14 is Patron-selected to align with the Microsoft package train; Build pickup rechecks drift (Q6).
