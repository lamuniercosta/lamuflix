# Research: DEV-330 Enrichment status SignalR push

**Date**: 2026-10-26

**Sources**: `brief.md` Q1–Q9 + PC1/PC2, `CONCLUSIONS.md` Q1–Q9 + PC1/PC2, `recon-DEV-330`, `recon-DEV-330-2`, ADR-0022, Proposed ADR-0029 (Keel PC1 update), `specs/PRODUCT.md`, constitution Principles I / API and Contract Rules / IX / Technology Stack. Grill closed; plan-challenge clarifications applied; no Technical Context NEEDS CLARIFICATION remained for Quill to invent.

## D1 — Post-commit notifier at EfMovieRepository

**Decision**: Detect enrichment-status transitions at `EfMovieRepository` using persisted-before vs committed-after status; require `IEnrichmentStatusNotifier` on the constructor (explicit fixture doubles; no optional/no-op production fallback); after enclosing commit, sequentially await `NotifyAsync(..., CancellationToken.None)` once per transitioned movie; catch/log per-movie exceptions and continue remaining movies; clear failed-attempt transitions before recovery.

**Rationale**: Ticket AC1/AC2; single movie-status choke point (`EfMovieRepository.SaveChangesAsync`); Patron Q2/Q5; PC1 supersedes Q5 optional-fallback permission. Covers all repository callers without registering unused handlers.

**Alternatives considered**: Handler-scattered notifies (rejected — duplicates and misses choke-point semantics); EF interceptor (rejected — Q5); durable notification outbox (rejected — Q3/ADR-0022 ephemeral push); optional constructor fallback (rejected — PC1 / Ledger M1 silent production no-op footgun); speculative internal transition-tracker collaborator before measured complexity (rejected for now — PC2 / Ledger M2 Follow-up noted, no ticket).

## D2 — SignalR hub and Api adapter

**Decision**: `EnrichmentHub` at `/hubs/enrichment` under flat Api root; `SignalREnrichmentStatusNotifier` maps Core port args to `EnrichmentStatusChangedDto` and broadcasts via `IHubContext<EnrichmentHub>.Clients.All`; SignalR JSON name converters configured independently of HTTP JSON; shared-framework server (no server NuGet); adapter logs attempt start/failure with movie id and committed status; sequential one-shot awaits with no application-level timeout/fan-out bound.

**Rationale**: Ticket Scope; ADR-0022; Patron Q4/Q5/Q6; PC1 R3/R4; PC2 retains Api root (Ledger L2).

**Alternatives considered**: Raw WebSocket framing (rejected); groups/subscriptions (rejected — single-user broadcast); npm client in this ticket (belongs to DEV-422); `Hubs/` folder (taste — not adopted); application-level send timeout/fan-out (rejected — PC1 retains unbounded sequential ephemeral attempt).

## D3 — OpenAPI components-only DTO exposure

**Decision**: New `EnrichmentHubSchemaTransformer` (`IOpenApiDocumentTransformer`) inserts unreferenced `EnrichmentStatusChangedDto` into `components.schemas` without a REST path; regenerate baseline/snapshot/TypeScript via existing `LAMUFLIX_REGENERATE_OPENAPI` + `npm run generate:api` workflow. Keep existing `EnrichmentStatusSchemaTransformer` for discovered HTTP-endpoint types.

**Rationale**: Contract chain; Patron Q4; PC2 / Ledger L1. recon-DEV-330-2:30-33: schema transformers fire only for `JsonTypeInfo.Type` already present because an endpoint references them; a hub-only DTO has no REST endpoint, so document-level insertion is required. `ExpectedOperations` exact match must stay unchanged; required fields and four-name string enum proofs retained; schemas remain subset-checked.

**Alternatives considered**: Extending `EnrichmentStatusSchemaTransformer` alone (rejected — never runs for an undiscussed hub DTO); new REST endpoint solely for schema discovery (rejected — expands ExpectedOperations); hand-edited `types.ts` (forbidden).

## D4 — Test-only SignalR client package

**Decision**: `Microsoft.AspNetCore.SignalR.Client` 10.0.14 in integration tests via `Directory.Packages.props`; official `HubConnection` against test host.

**Rationale**: Patron Q6; alignment with Microsoft.AspNetCore.* 10.0.14 train (recheck at Build pickup).

**Alternatives considered**: Hand-rolled SignalR framing in tests (rejected).

## D5 — CORS reuse

**Decision**: Retain existing `DevSpa` policy; apply to hub/negotiate; prove OPTIONS preflight, rejected origin, negotiation/delivery.

**Rationale**: Ticket AC3; DEV-308 precedent; Patron Q7.

**Alternatives considered**: AllowCredentials / second origin (rejected).

## D6 — Constitution v1.5.0 + ADR-0029

**Decision**: Owner Gate 1 checkbox for MINOR amendment and ADR-0029; Phase A carries proposed amendment text in `constitution-amendment-v1.5.0.md`; Phase B applies after user approval with whitelist update.

**Rationale**: Patron Q1; constitution closed port list and amendment procedure.

**Alternatives considered**: Silent port addition without amendment (forbidden).

## D7 — Post-commit cancellation, isolation, crash honesty (PC1)

**Decision**: Pass `CancellationToken.None` at every post-commit `NotifyAsync` call site; isolate at both repository dispatch and adapter send; no durable crash-window detection between commit and send; T036 records crash loss / no guaranteed receipt / DEV-422 reconnect-refetch.

**Rationale**: findings-DEV-330-Sentry R1–R4; Patron PC1; ADR-0022 / ADR-0029.

**Alternatives considered**: Passing the caller token into post-commit send (rejected — cancels immediately); durable outbox for crash window (rejected — Q3 ephemeral).

## D8 — Frozen file set and task hygiene (PC2)

**Decision**: Retain planned Api root file paths; keep stable task IDs (including T012) and explicit phase dependencies; T006/T007 blocked until T001 records Gate 1; incremental static gates use exact touched-file `-Files` lists and record them.

**Rationale**: Patron PC2; findings-DEV-330-Ledger L2/L3; findings-DEV-330-Compass L1–L3.

**Alternatives considered**: Renumbering T012 into Phase 4 sequence (rejected — cosmetic only; dependencies already correct); adopting `Hubs/` folder (rejected — no taste change).
