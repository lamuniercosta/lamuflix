# Persisted OMDb admission per transport attempt

Status: Proposed. Requires owner approval of this ADR and the accompanying constitution MINOR amendment 1.6.0 to 1.7.0 at the DEV-426 Gate 1 spec-PR merge. No implementation or constitution approval is claimed.

Ticket: DEV-426. Authority: `specs/DEV-426/brief.md`, Q1–Q6 and post-grill C1/C2 correction; `specs/DEV-426/CONCLUSIONS.md`, including Patron C1/C2 ruling. Evidence: `recon-DEV-426`, sections 1–9.2. Amendment proposal: `specs/DEV-426/constitution-amendment-v1.7.0.md`.

## Context

The ticket requires a persisted daily OMDb request budget, `IOmdbRequestGate`, `OmdbQuotaOptions`, the `omdb_daily_usage` counter table, provider wiring and concurrent/restart/rollover tests. The budget must survive process recreation and arbitrate across hosts sharing a database. Automatic retries happen below `FetchAsync`, so counting logical lookups would undercount outbound attempts.

Constitution 1.6.0 Principle I defines a closed Core port list and requires an ADR for a new port. Principles VII and VIII need explicit coverage for the new options record and vendor-named Core types. Patron ruled to amend these clauses while preserving the ticket-named types, superseding the Q6 no-ADR ruling. Governance requires the proposal, ADR, version bump, Sync Impact Report, dependent-template review and owner approval. The 1.4.0/ADR-0024, 1.5.0/ADR-0029 and 1.6.0/ADR-0030 changes provide the cited precedents.

## Decision

1. Add Core port `IOmdbRequestGate` with `Task<bool> TryAcquireAsync(CancellationToken)`. A returned true authorises one attempt through one committed reservation; false denotes exhaustion without increment. Add Core record `OmdbQuotaOptions`, section `Omdb:Quota`, with positive `DailyLimit` defaulting to 1000, validated at startup through ServiceDefaults. Consume a startup `IOptions` snapshot; changes take effect on restart.
2. Implement the scoped Infrastructure gate through one parameterised PostgreSQL conditional insert-or-increment. Sample TimeProvider UTC date and limit once. Persist `usage_date` as date primary key and `request_count` as nonnegative integer in `omdb_daily_usage`; generate the EF migration and snapshot. Share one counter per database, retain past dates and never refund reservations. Lowered limits deny further admission without rewriting history; instances must use consistent limits.
3. Register an OMDb-only `DelegatingHandler` after `AddStandardResilienceHandler` on the shipped typed-client chain. Every retry acquires separately. Construct it only with `IServiceScopeFactory`, resolve the scoped gate in a fresh async scope per attempt and dispose that scope before transport. Rejections above this handler spend no reservation.
4. Map distinct sealed Infrastructure-internal exhaustion and persistence-failure signals in `FetchAsync` to existing `RateLimited` and `ProviderUnavailable`, respectively, with distinct structured logs. Preserve the original persistence exception. Signals neither trigger retry predicates nor carry API keys or URIs. Stop further OMDb variants, preserve chained-provider fallback and propagate caller cancellation unwrapped.
5. Propose constitution 1.7.0: add `IOmdbRequestGate` to Principle I, `OmdbQuotaOptions` to Principle VII, and both ticket-named types to Principle VIII vendor-name exceptions. Retain all unrelated substantive rules. Adapt the architecture whitelist as a directly affected test guard. Owner approval of this ADR and amendment is an unchecked Gate 1 decision until the spec PR is approved and merged by the owner. Rejection becomes `blocked: structural — ticket-named port/types vs constitution`.

## Consequences

- Real PostgreSQL/Testcontainers tests must establish atomic concurrency, first-row races, recreation, rollover, configuration changes and cancellation/failure behavior. Shipped composition plus WireMock must establish per-attempt retry counts and failure/fallback behavior; unit tests cover options, mapping and scope lifetimes. Required fixture adaptations accompany the step introducing their dependency.
- Each outbound attempt costs one database round trip. A crash, timeout or cancellation can leave a spent reservation without a completed request; conservative loss is accepted. Separate keys sharing a database share its allowance, and mismatched host limits are an operator error without reconciliation.
- No new package, project, layer, public API, UI, secret handling, LocalPlay change, cleanup job, retry-policy redesign or DEV-333 bulk-scan implementation is introduced. Ticket scope and the existing review/gate requirements remain unchanged.
- This ADR remains Proposed until owner approval; the amendment is not a waiver. Planning analysis and plan challenge do not close Gate 1 or authorise implementation.

## Plan challenge clarification (Patron PC1/PC2)

`specs/DEV-426/CONCLUSIONS.md` PC1/PC2 governs the support-type names and remaining semantics. New Infrastructure support types have functional names (`EfMetadataRequestGate`, `DailyRequestUsageRecord`, `DailyRequestUsageConfiguration`, `MetadataQuotaHandler`, `MetadataQuotaExhaustedException`, `MetadataQuotaPersistenceException`), one file per type; ticket Core names and table remain unchanged. Local admission signals leave upstream provider health state unchanged. Interruption after partial fallback progress discards that partial result and returns the mapped failure. Admission receives the ambient SendAsync token; timeout/cancellation preserves the conservative committed-reservation rule and does not change resilience policy. Structured signal logs exclude keys, URIs and raw exception serialization; the original persistence exception remains in memory as the inner exception.

After verified owner Gate 1 approval, Keel records the checkbox answer and spec-PR merge receipt in CONCLUSIONS.md. Phase B verifies the merged constitution without any edit; date-only differences are recorded there, not repaired. Keel changes only this ADR's status to Accepted in the implementation PR after that approval and before code merge. This delivery path supersedes any earlier implication of a same-PR status change or a Phase B constitution-date edit. Gate 1 remains open until the owner approves.
