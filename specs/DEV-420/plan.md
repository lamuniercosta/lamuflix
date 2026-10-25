# Implementation Plan: DEV-420 Relay outbox rows to RabbitMQ with publisher confirms and remove the direct enqueue

**Branch**: `feature/420-spec` | **Date**: 2026-10-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/DEV-420/spec.md`, closed grill brief `specs/DEV-420/brief.md` (Q1–Q12; plan-challenge Q7 logging amendment), facts from notes `DEV-420` and `recon-DEV-420`, Patron rulings in `specs/DEV-420/CONCLUSIONS.md`, product authority in `specs/PRODUCT.md`, design authority `docs/adr/0027-transactional-outbox-enrichment.md` and Draft `docs/adr/0028-confirmed-enrichment-outbox-relay.md` in this worktree.

**Status**: Draft planning artifact only (Phase 2 step 3 drafts; Round 1 plan-challenge correction). Brief grill closed; Gate 1 not claimed. No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. **Phase B STOP** until pickup verifies landed DEV-329 schema/Stage/payload/save contracts and DEV-419 ADR against current main and resolves drift — do not invent substitute prerequisite contracts (brief Q2, Q10). Draft ADR-0028 exists and remains Draft; Quill does not author or accept ADRs (historical Phase 2 step 3 drafts predated it). ADR acceptance, plan freeze, and Gate 1 stay separate later steps. No `design.md` (no ui tag).

## Phase B entry preflight (executable before Code Phase 1)

Schedule these Build-workspace checks before production edits. Do not run gates, tests, or analyzers from Spec as substitutes for Phase B. Missing evidence blocks code.

| Check | Owner / evidence | Blocks code if missing |
|---|---|---|
| Spec PR merged and Gate 1 established | Rigger / YouTrack Ready-to-build handoff | Yes |
| Phase B pickup verifies landed DEV-329 (`outbox_messages` schema, Stage/adapter/payload/save contracts) and DEV-419 (ADR-0027) against current main; reconcile drift vs Q9 24-file boundary (including `TelemetryConstants.cs` per post-grill C1) and brief Q2–Q8; STOP on missing landing or mismatch rather than inventing substitutes | Wisp → recon refresh; Keel clearance | Yes |
| Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef`) when Build intake requires them | Gauge → recorded in `DEV-420` | Yes (when required by Build intake) |
| Keel drift check or amend against brief/spec before code | Keel handoff in `DEV-420` | Yes |

## Summary

Deliver a single Infrastructure `OutboxRelayHostedService` registered from the Api host that batch-polls durable unprocessed `outbox_messages`, publishes through the existing confirmed mandatory RabbitMQ path, marks ProcessedAt only after confirmation, backs off on failure, deletes expired processed rows on an independent schedule, and exposes Ready-tagged readiness plus six backlog/relay instruments whose names are centralized in existing `TelemetryConstants.cs` and consumed by `OutboxRelayMetrics`. Cut over only `ImportMovieFolderCommandHandler` and `RequestEnrichmentCommandHandler` off direct enqueue while preserving Stage → shared save atomicity. Prove with real PostgreSQL and RabbitMQ plus deterministic TimeProvider tests inside the exact Q9 24-file boundary (post-grill C1 / CONCLUSIONS.md:218-221). Concurrent multi-instance relays remain out of scope.

## Domain vocabulary

| Term | Definition source | Notes |
|------|-------------------|-------|
| staged | brief Q12 | In-memory tracking before shared save |
| durable | brief Q12 | Committed in Postgres with the movie write |
| unprocessed | brief Q12 | ProcessedAt IS NULL |
| confirmed | brief Q12 | Broker acceptance via confirmed mandatory path |
| processed | brief Q12 | Marker durably committed after confirm |
| expired processed | brief Q12 | ProcessedAt strictly older than retention cutoff |

CONTEXT.md is not edited. Draft ADR-0028 already records settled relay decisions and must preserve ADR-0027 guarantees and this vocabulary; it remains Draft until separately accepted (brief Closure).

## Technical Context

**Language/Version**: C# / .NET 10 backend. No web change.

**Primary Dependencies**: None added. Reuse existing EF Core, Npgsql, RabbitMQ publisher/`IEnrichmentQueue`, OpenTelemetry metrics/health packages, xUnit/Shouldly/NSubstitute/Testcontainers already in the solution. No new NuGet/npm dependency (brief Q3, Q7–Q9).

**Storage**: Existing PostgreSQL `outbox_messages` (DEV-329) only. No migration, index, or column change (brief Q2, Q6, Q9).

**Testing**: xUnit; Shouldly; NSubstitute; real Postgres and RabbitMQ Testcontainers for query/serialization/broker proofs and cutover both-or-neither rollback; deterministic TimeProvider (reuse available support; no new package) including no-overlapping-passes and failure-log allowlist sentinel/rowless proofs. Never mock driver translation/serialization. No Gherkin/Reqnroll. All gate/test execution belongs to Phase B / Build workspace.

**Target Platform**: Existing `LamuFlix.Api` host. No public HTTP/OpenAPI shape change (readiness reuses existing `/health/ready` mapping).

**Project Type**: Existing ports-and-adapters solution. New relay types under existing `src/LamuFlix.Infrastructure/Enrichment/`; Api registration, two Core handler cutovers, and six Q7 metric-name `const` members in existing `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`; tests in existing unit/integration projects (brief Q9; post-grill C1).

**Performance Goals**: Operational defaults only (BatchSize 100, PollInterval 1s, etc.). Not harness gate thresholds (brief Q5).

**Constraints**: TreatWarningsAsErrors. Ticket scope authoritative. No LocalPlay/secrets/`Process.Start`. No public HTTP/OpenAPI/web. No new dependency/project/layer/schema. No publisher/Stage/DbContext/migration edits. Mutation never uses `--since`. Thresholds never lowered. Scope-empty/opt-out/N/A/Could not run never PASS. Phase B STOP on unverified DEV-329/DEV-419 landing/drift.

**Scale/Scope**: Exact Q9 24-file production/test delivery set (below), including `TelemetryConstants.cs` as file 24 solely for the six Q7 constants (Patron post-grill C1 / CONCLUSIONS.md:218-221). Unanticipated files return to Keel for a new cited Patron ruling. Frozen scope (brief Q11–Q12): unchanged DEV-329 contracts; one Api-host Infrastructure relay; bounded ordered confirmed mandatory publication and per-row markers; failure backoff and shutdown; processed-only retention; readiness/backlog metrics with centralized names; two-handler direct-enqueue cutover; Q8 proofs and exact Q9 boundary; anything else is a follow-up issue, not a finding in this round.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Every gate in this Constitution Check cites a numbered Core Principle (I–IX). Constraints that live only under unnumbered constitution sections are listed in **Unnumbered constitution authority checks** below, not here. Scope and Phase B prerequisites remain under **Scope and prerequisite checks**.*

- **Principle I** (ports and adapters, feature-organised Core; `.specify/memory/constitution.md`): Relay is Infrastructure-internal on existing `LamuFlixDbContext` and `IEnrichmentQueue`; no new Core relay port, generic persistence abstraction, or project/layer (brief Q3; charter §2.3 items 1–2).
- **Principle II** (explicit handlers; no MediatR): Handler cutover only removes direct enqueue from two named handlers; Stage wiring retained; no MediatR (brief Q2).
- **Principle VI** (observability): Ready-tagged `outbox-relay` on existing readiness; six low-cardinality instruments with names defined as `const` members in existing `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` and consumed by Infrastructure `OutboxRelayMetrics` (no local telemetry-name literals); withhold invalid/failed backlog gauges; successful empty sample yields count/age zero; respective pass/cleanup fault clearing; structured failure logs allow only outbox row id when row-scoped, exception type name, and stable internal error category — never exception objects, raw Message/ToString()/stack/inner-exception text, payload-derived values, or sensitive connection/configuration details (brief Q7; post-grill C1; plan-challenge Q7 / CONCLUSIONS.md:233-236; Draft ADR-0028; constitution centralized naming).
- **Principle VII** (configuration isolation and deterministic time): `OutboxRelayOptions` under `OutboxRelay`; all timestamps/delays/timeouts via injected `TimeProvider`; no secrets/LocalPlay/`Process.Start` (brief Q5–Q7).
- **Principle VIII** (ubiquitous language): Feature-local vocabulary only; no root CONTEXT.md edit; Draft ADR-0028 preserves terms while remaining Draft (brief Q12).
- **Principle IX** (test pyramid): Unit + real Postgres/RabbitMQ Testcontainers; existing assertion/mocking libraries; never mock Infrastructure drivers for translation/serialization (brief Q8).

### Unnumbered constitution authority checks

Binding constraints from unnumbered constitution sections. **Numbered principle: none** for these items; do not invent I–IX mappings.

- **Technology Stack Constraints**: Existing .NET 10 / EF Core / Npgsql / RabbitMQ / OpenTelemetry stack; no new package (brief Q3, Q7–Q9).
- **Coding Conventions — Comments**: No explanatory comments beyond AAA test headers, empty-block justifications, and scoped warning suppressions.
- **Pull Request Quality Gates**: Configured scripts and harness thresholds only; never lower thresholds or invent a threshold value (brief Q10 / `AGENTS.md`).

Post-design re-check: the numbered-principle Constitution Check gates above still hold, including Principle VI centralized metric naming via the post-grill C1 `TelemetryConstants.cs` authorization. This plan adds the ruled relay/options/state/metrics/health composition (constants before `OutboxRelayMetrics` consumer), retention, two-handler cutover, and Q9 test adaptations with no public HTTP contract, no schema migration, no new dependency/project/layer, and no harness edit. Unnumbered constitution authority checks and scope/Phase B constraints remain in force under their sections below; they are not waived by this re-check. Side design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) are not produced by Quill — the brief already records/settles the proposed contracts, files, ordering, and proofs. No `design.md` (no ui tag).

## Scope and prerequisite checks

Binding product, care-list, and Phase B prerequisite constraints. **Numbered principle: none applicable** for each item; do not invent I/II/IX citations. Constraints remain in force.

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). DEV-420:27-30 fixes relay with confirms, backoff, two-handler enqueue removal, retention, health/backlog metrics, Postgres+RabbitMQ integration; excludes concurrent multi-instance relays. Nothing added or dropped beyond the brief. No owner checkbox (brief Q12).
- Schema care list (§2.3 #3): consume DEV-329 `outbox_messages` unchanged; no migration here (brief Q2, Q6).
- Public API shape (§2.3 #4): unchanged; no OpenAPI/web regeneration (brief Q7, Q9).
- Epic / sequencing: **Phase B STOP** until landed DEV-329 and DEV-419 are verified at pickup; approved design packet alone is insufficient (brief Q2, Q10).
- File scope (§2.3 #6): exact Q9 24-file set including existing `TelemetryConstants.cs` solely for the six Q7 metric-name constants (Patron post-grill C1 / CONCLUSIONS.md:218-221); narrow host/test rewrites authorized because ticket-required registration/cutover/verification force them; additional files need a new cited ruling. Constants are scheduled before the `OutboxRelayMetrics` consumer (brief Q10 step 5).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-420/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md
├── tasks.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes approach, files, failure/retention/health policy, and proof order. There is no new public HTTP contract. No `design.md` (no ui tag). Draft ADR-0028 was authored by Keel in Phase 2 step 5 and remains Draft; Quill does not author or accept ADRs (historical Phase 2 step 3 drafts predated it). No `ASSUMPTIONS.md` (no taste rulings).

### Source Code (repository root) — exact Q9 boundary

```text
# New production
src/LamuFlix.Infrastructure/Enrichment/OutboxRelayHostedService.cs
src/LamuFlix.Infrastructure/Enrichment/OutboxRelayOptions.cs
src/LamuFlix.Infrastructure/Enrichment/OutboxRelayState.cs
src/LamuFlix.Infrastructure/Enrichment/OutboxRelayMetrics.cs
src/LamuFlix.Infrastructure/Enrichment/OutboxRelayHealthCheck.cs
src/LamuFlix.Infrastructure/Enrichment/OutboxRelayServiceCollectionExtensions.cs

# Existing production (bounded edits)
src/LamuFlix.Api/Program.cs
src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs
src/LamuFlix.Core/Features/Enrichment/RequestEnrichmentCommandHandler.cs
src/LamuFlix.Core/Pipeline/TelemetryConstants.cs

# New tests
tests/LamuFlix.UnitTests/Enrichment/OutboxRelayHostedServiceTests.cs
tests/LamuFlix.UnitTests/Enrichment/OutboxRelayHealthCheckTests.cs
tests/LamuFlix.UnitTests/Enrichment/OutboxRelayOptionsTests.cs
tests/LamuFlix.IntegrationTests/OutboxRelayTests.cs
tests/LamuFlix.IntegrationTests/OutboxRetentionTests.cs

# Existing tests/factories (bounded adjustments)
tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs
tests/LamuFlix.UnitTests/Features/Enrichment/RequestEnrichmentCommandHandlerTests.cs
tests/LamuFlix.IntegrationTests/ImportEndpointTests.cs
tests/LamuFlix.IntegrationTests/EnrichmentEndpointTests.cs
tests/LamuFlix.IntegrationTests/ImportMovieFolderCommandHandlerPersistenceTests.cs
tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs
tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs
tests/LamuFlix.IntegrationTests/ApiHostFactory.cs
tests/LamuFlix.IntegrationTests/ApiEndToEndFactory.cs
```

**Q9 count**: 6 new + 4 existing production and 5 new + 9 existing test files = 24. `TelemetryConstants.cs` is Q9 file 24 solely for the six Q7 `lamuflix.outbox.*` `const` members consumed by `OutboxRelayMetrics` (Patron post-grill C1 / CONCLUSIONS.md:218-221; brief.md:137-143,174-176).

**Outside delivery (do not edit)**: publisher/queue port, Stage adapter, DbContext/entity/configuration, migrations, movie repository, other enqueue callers, API contracts, `web/`, `Directory.Packages.props`, harness thresholds, secrets, LocalPlay. No further `TelemetryConstants.cs` changes beyond the six approved constants.

**Structure Decision**: Match brief Q9 exact paths. Schedule the six centralized constants before the `OutboxRelayMetrics` consumer. Real relay tests must run the worker; factory isolation for unrelated tests cannot substitute for delivery proof.

## Design (from settled brief)

1. **Authority (Q1–Q2)**: DEV-329 contracts and ADR-0027 are prerequisites; Draft ADR-0028 elaborates relay delivery; cut over only the two named handlers; preserve Stage/shared-save both-or-neither atomicity and uncertain-save distinction; Phase B verifies landing.
2. **Relay lifecycle (Q3)**: Fresh-scope bounded ordered reads; sequential confirmed mandatory publish; per-row post-confirm markers; non-overlapping passes; at-least-once; no Core relay port/schema.
3. **Failure (Q4)**: Abort-first; preserve prefix; capped backoff; poison head-of-line; clean shutdown.
4. **Options (Q5)**: Validated `OutboxRelayOptions`; immediate poll; deterministic delays; linked publish timeout.
5. **Retention (Q6)**: Same worker, independent schedule, processed-only bounded deletes.
6. **Health/metrics/logging (Q7, Q12; post-grill C1; plan-challenge Q7)**: Cached Ready check; six instruments with names centralized in `TelemetryConstants.cs` and consumed by `OutboxRelayMetrics` (Principle VI); gauge withholding on invalid samples; empty-sample zeros; independent pass/cleanup fault clearing; strict failure-log allowlist (row id when row-scoped, exception type name, stable category only); constants before consumer.
7. **Tests (Q8)**: Real DB/broker + fault injection + deterministic unit/composition proofs, including no-overlap, logging sentinel/rowless, recovery, and cutover both-or-neither proofs.
8. **Files (Q9)**: Exact 24-file set above (including `TelemetryConstants.cs` as file 24).
9. **Ordering (Q10)**: Pickup STOP → harness/options → relay+failure → retention → TelemetryConstants six constants then health/metrics/host → cutover → Phase B gates.
10. **Loop discipline (Q11–Q12)**: Critical/High/Medium block; two+two round caps; three axes required; frozen scope sentence governs findings.

## Complexity Tracking

> No constitution violations requiring justification. Infrastructure-internal worker; no new architectural layer; schema consumed unchanged from DEV-329.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phase boundaries

- **Phase A (this ask)**: Spec Kit docs under `specs/DEV-420` only (`spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md` with L clarify session in `spec.md`). Historical note: Phase 2 step 3 Quill drafts predated Draft ADR-0028; Keel authored Draft ADR-0028 in step 5. This Round 1 correction references that Draft without accepting it, freezing the plan, claiming Gate 1, publishing, running gates, or implementing. Challenge verification, brief/plan freeze, ADR acceptance, and Gate 1 remain distinct later steps.
- **Phase B**: After Gate 1 and verified landed DEV-329 + DEV-419 + drift clearance (STOP otherwise). Then Build implements per `tasks.md`; Cog refactor; final post-refactor configured build/format and full `dotnet test` on the changed HEAD before Gauge gates per brief Q10. Build asks do not author or accept the ADR unless Conductor separately assigns ADR work to Keel.

## Phase A freeze receipt — 2026-10-25 (Keel)

**Plan: FROZEN for Phase A planning.** Read-only speckit-analyze is CLEAN and the consolidated plan-challenge Round 1 is CLOSED/CLEAN (PLAN_CHALLENGE_ADJUDICATION.md:72-104). Five accepted Medium findings are closed; Ledger F2 remains rejected; six Low dispositions are preserved; no Round 2 or open decision is required. See brief.md, Phase A freeze receipt, for the analysis coverage, exact input SHA256 manifest, scope and limitations.

Target: F:/Dev/LamuFlix.worktrees/feature-420-spec, feature/420-spec, HEAD/base 8063eed0d5cc4b5bb3a8eb0c68b8990acf7a8cd3. The eight reviewed artifacts remain untracked working bytes, not HEAD-pinned content. All preceding text is preserved as historical stage content; this append supersedes only its pending-freeze status. No design, Q9 24-file boundary, Q7 policy, task completion mark or round-cap change is made.

No unmet Phase A freeze criterion. ADR-0028 remains Draft; Gate 1 and gate1: provisional remain unset. Phase B entry TB000-TB003 still requires its own evidence. No implementation, tests/gates, commit, PR or tracker action is performed. Phase 2 step 7 remains the later Conductor/Patron/Rigger action; user merge remains separate.
