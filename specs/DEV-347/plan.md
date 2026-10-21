# Implementation Plan: DEV-347 FeaturesRequested message, topology, and publish after enrichment succeeds

**Branch**: `feature/347-spec` | **Date**: 2026-10-21 | **Spec**: [spec.md](spec.md)

**Input**: [spec.md](spec.md), settled grill brief [brief.md](brief.md), facts from note `recon-DEV-347`, Patron rulings in [CONCLUSIONS.md](CONCLUSIONS.md), taste receipt [ASSUMPTIONS.md](ASSUMPTIONS.md), `specs/PRODUCT.md`.

**Status**: Draft planning artifact only (Phase A). Not frozen; Gate 1 not claimed. No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1.

## Summary

Define `FeaturesRequested`, declare the three `features.*` quorum queues on the existing `lamuflix.enrichment` exchange, extend the existing enrichment queue port/publisher, and publish `FeaturesRequested(movie.Id, Attempt: 1)` from Core after a successful enrichment save when the persisted plot (`MovieMetadata.Synopsis`) is non-whitespace. Preserve enrichment topology and `EnrichmentRouting.Decide` semantics. Keep the enrichment consumer thin with a single post-handler ack path and the Q9 loud-failure settlement contract. Consuming features messages, embeddings, and `movie_features` schema remain out of scope.

**Note on the Spec Kit phases**: research, the data model, contracts, and the quickstart are folded into this plan. No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` is produced. Message shape, topology, publish owner, eligibility, telemetry, and failure/settlement are already ruled in brief Q1–Q9; this ticket adds no HTTP route and no persistence entity. `.specify/extensions.yml` is absent, so no extension hook ran. There is no agent-context script under `.specify/`; `AGENTS.md` is outside the Phase A artifact set and is not edited.

## Technical Context

**Language/Version**: C# on .NET 10 (solution projects under `src/` / `tests/`).

**Primary Dependencies**: None added. Reuse pinned `RabbitMQ.Client` (`Directory.Packages.props:10`, `recon-DEV-347:95`) and `System.Text.Json` web defaults already used by `RabbitMqEnrichmentQueuePublisher` (`recon-DEV-347:49`).

**Storage**: Existing PostgreSQL movie persistence only. No schema or migration. `movie_features` belongs to DEV-413 (brief care-list; `recon-DEV-347:97`).

**Testing**: xUnit v3, Shouldly, NSubstitute for Core ports; RabbitMQ/Postgres Testcontainers fixtures for transport/persistence. Unit coverage in `ProcessEnrichmentCommandHandlerTests`, routing/registration tests where behavior changes; integration coverage in `RabbitMqTopologyTests`, `RabbitMqPublisherTests`, `EnrichmentConsumerTests`. No Gherkin/Reqnroll. No property-test opt-out granted by this brief.

**Target Platform**: Existing Api-hosted enrichment consumer and Infrastructure RabbitMQ adapters (`recon-DEV-347:50,69`). Legacy `src/LamuFlix.Worker` is not a target.

**Project Type**: Existing .NET solution using ports and adapters with feature-organised Core. No new project.

**Performance Goals**: None in the brief. None added.

**Constraints**: No new NuGet/npm dependency; no new port interface/project/layer; no HTTP/OpenAPI/web change; no schema/migration; no LocalPlay/secrets/`Process.Start`; no silent publish failure; no transactional outbox; no features consumer/sweeper in this ticket. Thresholds stay in `harness.yml`. Preserve ticket AC wording "exactly one" as execution emission cardinality (brief Q9).

**Scale/Scope**: Patron-approved production file envelope plus corresponding tests listed below. Delivery scope (brief): message, three features queues/bindings/arguments, routing support, existing publisher extension, and post-save enrichment-success publish. Scope statements describe that delivery boundary and do not preclude findings about failures to meet in-scope requirements.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Principle I (boundaries): no new project, folder, layer, or Core port interface. `FeaturesRequested` is a message record; publishing reuses `IEnrichmentQueue` (brief Q2; constitution ports whitelist; `recon-DEV-347:87,96`).
- Enrichment Reliability Rules: consumer stays thin; decisions live in Core; silent publish failure forbidden; `FeaturesRequested` reuses claim/retry/DLQ/category/sweeper machinery without forking a second pipeline (constitution cited in brief Q2, Q6, Q8, Q9; `recon-DEV-347:24,102`).
- Technology Stack: quorum queues; topology in one `RabbitMqTopology` class; publisher confirms and mandatory routing retained (brief Q3, Q8; `recon-DEV-347:39-49`).
- Principle VI (telemetry): reuse existing producer activity/`TraceContextCarrier.Inject` pattern (brief Q8).
- Principle IX (tests): Core eligibility unit-tested without RabbitMQ; real broker topology/publisher/consumer proofs use Testcontainers (`recon-DEV-347:75-85`).
- API and Contract Rules: no public route/DTO/OpenAPI/TS change (brief care-list; `recon-DEV-347:98`).
- Documentation rules: no ADR authored by Quill; no `CONTEXT.md` / `README.md` / `PRODUCT.md` edit in this ask.
- Static-analysis gates: Roslyn, complexity (implementation and refactor thresholds resolved from `harness.yml` at execution via the supported script invocations), InspectCode, mutation, property tests, vulnerable packages, format, `dotnet test` — harness thresholds authoritative, never copied into this plan as numeric literals and never lowered (brief Gate expectations; Plan challenge editorial decisions).
- Charter §2.3 / PRODUCT §5: care-list items 1–6 ruled in CONCLUSIONS; zero owner escalations (brief Closing bar).

Post-design re-check: the same checks hold. This plan adds no package, project, new port name, schema, HTTP contract, CONTEXT.md edit, ADR, or file outside the approved brief envelope. Side design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) are not produced — the brief already freezes contracts, files, ordering, and proofs.

## Scope and prerequisite checks

Binding product, care-list, and sequencing constraints. Constraints remain in force.

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches canvas note DEV-347 Ticket Scope & Technical Design items 1–4 and Acceptance Criteria bullets 1–3 (brief section Ticket scope and acceptance criteria — verbatim). Ticket wording `FullPlot` maps to `MovieMetadata.Synopsis` with whitespace-empty treatment per Q5 and ASSUMPTIONS; AC “exactly one” is execution emission cardinality per Q9 and `spec.md:64-67,111-114`. Nothing is added or dropped. No owner checkbox.
- No schema or migration (care list item 3; brief; `recon-DEV-347:97`).
- Public API shape unchanged (care list item 4; `recon-DEV-347:98`).
- Epic / sequencing: PRODUCT defers Recommendations execution until Epic 7 completes (`specs/PRODUCT.md:17`). This ticket defines the features request seam only; it does not start Phase B Recommendations/embedding work and does not authorize an exception to that sequencing (brief Frozen scope).
- Legacy Worker wording in PRODUCT (`specs/PRODUCT.md:10`) is not a decision to move this feature into `src/LamuFlix.Worker` (brief Frozen scope; ticket item 4).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-347/
├── brief.md, CONCLUSIONS.md, ASSUMPTIONS.md   (Keel / Patron)
├── spec.md                                     (/speckit-specify)
├── plan.md                                     (/speckit-plan)
├── tasks.md                                    (/speckit-tasks)
├── checklists/requirements.md
└── DRAFTING_RECEIPT.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. `AGENTS.md` is not updated.

### Source Code (repository root)

```text
src/LamuFlix.Core/Ports/FeaturesRequested.cs                    # new — sealed record message
src/LamuFlix.Core/Ports/IEnrichmentQueue.cs                     # extend EnqueueAsync(FeaturesRequested)
src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs  # post-save publish owner
# ProcessEnrichmentOutcome.cs remains in approved envelope; retain existing claim outcome semantics (brief)

src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs        # declare features.* queues
src/LamuFlix.Infrastructure/RabbitMq/EnrichmentRouting.cs       # features route helpers; Decide unchanged
src/LamuFlix.Infrastructure/RabbitMq/RabbitMqEnrichmentQueuePublisher.cs  # FeaturesRequested publish
src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs      # preserve Q9 settlement; no second publish

tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs
tests/LamuFlix.UnitTests/RabbitMq/EnrichmentRoutingTests.cs     # where changed behavior needs coverage
tests/LamuFlix.UnitTests/RabbitMq/RabbitMqServiceCollectionExtensionsTests.cs  # where needed
tests/LamuFlix.IntegrationTests/RabbitMqTopologyTests.cs
tests/LamuFlix.IntegrationTests/RabbitMqPublisherTests.cs
tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs
tests/LamuFlix.Tests.Common/RabbitMqFixture.cs                  # only if reset/cleanup enumerates new queues
```

**Structure Decision**: Match existing Core Ports + Enrichment feature folder and Infrastructure RabbitMQ adapter layout (`recon-DEV-347:33-57`). Registration stays in existing `RabbitMqServiceCollectionExtensions` (`recon-DEV-347:50`); no registration rewrite or production file expansion beyond the brief envelope without a new Patron file-scope ruling.

## Design (from settled brief)

1. **Message and port (Q1–Q2)**: Add `FeaturesRequested` sealed record. Extend `IEnrichmentQueue` only; do not add a port or change the whitelist.
2. **Topology (Q3)**: One `RabbitMqTopology` owner declares `features.requested` / `features.retry` / `features.dead-letter` on `lamuflix.enrichment` with Q3 arguments; enrichment queues untouched.
3. **Routing (Q4)**: Add features routing support in `EnrichmentRouting` without changing `Decide(ProcessEnrichmentOutcome)`.
4. **Eligibility and publish owner (Q5–Q6)**: Core `CompleteAsync` owns eligibility (`Enriched` + non-whitespace `Synopsis`) and awaits enqueue after successful save with `Attempt: 1`. Consumer does not publish features.
5. **Telemetry and confirms (Q8)**: Reuse publisher confirms, mandatory routing, web-default JSON, trace injection, existing producer activity tags.
6. **Failure and settlement (Q9)**: Loud publish/confirm failures; stopping cancellation requeues enrichment; other failures dead-letter enrichment without success ack; refused-claim redelivery never republishes features; no outbox/rollback/in-handler retry; eventual recovery is DEV-413/DEV-414.
7. **Scope boundary (Q7 + care-list)**: No features consumer, embeddings, schema, Worker target, dependency, HTTP/OpenAPI, LocalPlay/secrets/`Process.Start`.
8. **Tests and gates**: Brief Test strategy and Gate expectations; architecture tests must still pass.

## Complexity Tracking

> No constitution violations requiring justification. Message + topology + publisher extension reuse existing enrichment machinery; Core owns the eligibility decision; no new architectural layer.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phase boundaries

- **Phase A (this ask)**: Spec Kit docs under `specs/DEV-347` only (`spec.md`, `plan.md`, `tasks.md`, checklist, drafting receipt). No freeze, publish, gates, or implementation.
- **Phase B**: After Gate 1 and workspace handoff, Anvil implements per `tasks.md` one phase per ask; Cog refactor; Gauge gates. Fresh Phase B drift recon remains a later pipeline step (`task-pipeline`; brief Task ordering).
