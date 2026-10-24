# Implementation Plan: DEV-414 FeaturesRequested consumer and movie_features persistence

**Branch**: `feature/414-spec` | **Date**: 2026-10-23 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/DEV-414/spec.md`, settled grill brief `specs/DEV-414/brief.md` (Q1–Q9 including Q4 continuation), facts from notes `recon-DEV-414` and `recon-DEV-414-2` (corrected publisher evidence at recon-DEV-414-2:90-213 governs), Patron rulings in `specs/DEV-414/CONCLUSIONS.md`, product authority in `specs/PRODUCT.md`.

**Note**: Planning artifact only (Phase 2 step 3.2). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1, and until Phase B entry evidence is complete. No `design.md` (no ui tag). Proposed ADR `docs/adr/0026-separate-feature-generation-and-recovery.md` (status: proposed) was drafted by Keel under Conductor Phase 2 step 5 and accompanies this spec PR; Quill does not invent ADR body text or claim ADR acceptance, plan-challenge clearance, or Gate 1.

## Phase B entry preflight (executable before Code Phase 1)

Schedule these Build-workspace checks before T001. Do not run gates, tests, or analyzers from Spec. Missing evidence blocks code.

| Check | Owner / evidence | Blocks code if missing |
|---|---|---|
| Recorded Epic 7 (SPA) completion receipt (`specs/PRODUCT.md` §2: Epic 9 / recommendations-pgvector deferred until Epic 7 completes). Brief Q9 does not waive PRODUCT sequencing. | Named handoff / receipt recorded in `DEV-414`. Requires the receipt record only; does not assert current Epic 7 status. | Yes |
| Spec PR merged and Gate 1 established | Rigger / YouTrack `Ready to build` handoff in `DEV-414` | Yes |
| Phase B drift recon vs `origin/main` for the authorized file set (Wisp → `recon-DEV-414` refresh) | Wisp | Yes |
| Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef`) | Gauge → recorded in `DEV-414` | Yes |
| Keel drift check or amend against brief/spec before code | Keel handoff in `DEV-414` | Yes |

## Summary

Deliver a `features.requested` BackgroundService consumer that loads plot text, generates one embedding via the existing `IEmbeddingGenerator<string, Embedding<float>>`, and upserts `movie_features` through the existing DEV-413 repository with MovieId last-write-wins semantics. Bound retry/DLQ to the existing features topology and EnrichmentOptions.MaxAttempts / RabbitMqOptions.RetryDelay within corrected publication limits (configured confirms/mandatory/awaited publish; no asserted broker-ack/nack/return or exactly-once guarantee). Extend StrandedMovieSweeper with an independent missing-features recovery pass that enqueues FeaturesRequested(MovieId, 1) via a new anti-join seam, with deliberate per-sweep Attempt 1 reset after prior DLQ. Register only when feature prerequisites exist; expose features-consumer readiness separately from metadata liveness; preserve tracing. Additive root CONTEXT.md vocabulary for the three brief Q9 terms lands in the same spec PR.

## Technical Context

**Language/Version**: C# / .NET 10 backend. No web change.

**Primary Dependencies**: Existing RabbitMQ.Client 7.2.2 (Directory.Packages.props:10; recon-DEV-414-2:C), Microsoft.Extensions.AI embedding abstractions, OllamaSharp embedding adapter (DEV-412), EF Core / Npgsql / pgvector movie_features storage (DEV-413), Testcontainers PostgreSQL/RabbitMQ. No new NuGet/npm dependency.

**Storage**: Existing `movie_features` table and upsert. No schema or migration. New database-side anti-join query only (brief Q5–Q6).

**Testing**: xUnit; existing assertion libraries; FakeEmbeddingGenerator; deterministic TimeProvider; NSubstitute for Core ports only; real Testcontainers for query translation, serialization, upsert, and broker behavior. No driver mocks, no Infrastructure mocks, no external Ollama download. All gate/test execution belongs to Phase B / Build workspace.

**Target Platform**: Existing `LamuFlix.Api` host with RabbitMQ enrichment topology. No public HTTP surface change.

**Project Type**: Existing ports-and-adapters solution. New types sit in existing Core/Features/Enrichment and Infrastructure/RabbitMq. No new project or layer.

**Performance Goals**: None in the brief. None added. Accepted costs include repeat inference on redelivery and indefinite SweepInterval re-enqueue of persistently failing movies (brief Q3, Q5).

**Constraints**: TreatWarningsAsErrors. Ticket scope authoritative. No LocalPlay/secrets/Process.Start. No public HTTP/OpenAPI/web. Do not assert unverified RabbitMQ.Client 7.x broker-ack/nack/return await semantics. Mutation never uses `--since`. Thresholds never lowered. PRODUCT Epic 9 sequencing not waived.

**Scale/Scope**: Authorized new and existing files in brief Q6–Q8. Unanticipated files return to Keel.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). Ticket DEV-414 Scope lines 20–24 and AC line 17 match brief frozen scope. Owner structural checkboxes: none.
- Dependency care list (§2.3 #1): no new package (recon-DEV-414 §2.3 item 1; Q4 continuation / Q6).
- No new project/folder/layer (§2.3 #2): consumer/handler beside EnrichmentConsumer and Features/Enrichment (recon-DEV-414 §2.3 item 2; Q6).
- Database schema (§2.3 #3): no migration; existing movie_features upsert/schema unchanged; new query seam only (Q5–Q6).
- No public API shape change (§2.3 #4): features-consumer readiness on existing readiness endpoint; no new route/status/DTO (Q7).
- No LocalPlay/secrets/`Process.Start` (§2.3 #5): excluded (Q6, Q9).
- File scope (§2.3 #6): non-ticket-named existing-file edits deliberately authorized by Patron citing forcing AC (Q6–Q8); additive CONTEXT.md authorized (Q9).
- Principle I: Core behind existing ports; IEmbeddingGenerator already named; no MediatR (constitution I–II; recon-DEV-414 section 6).
- Principle IV: enrichment claim/MarkFailed state machine remains enrichment-only; feature lifecycle does not MarkFailed (Q2, Q4; recon-DEV-414 section 6).
- Principle VI: producer → consumer → embedding/upsert tracing; structured MovieId logs; existing /health/live and /health/ready (Q7).
- Principle VII: TimeProvider for ComputedAt and delays; no secrets in source (Q3, Q7).
- Principle VIII: additive CONTEXT.md terms in same PR (Q9).
- Principle IX: integration proof for retry-queue, DLQ, sweeper, single-trace e2e; NSubstitute only; never mock Infrastructure (Q8; recon-DEV-414 section 6).
- Static-analysis gates: configured scripts; no threshold waiver; scope-empty SKIPPED never PASS (Q1).

Post-design re-check: the design below adds the ruled consumer/handler/recovery/activation seams with no public HTTP contract, no schema migration, no new dependency, and no harness edit. Proposed ADR 0026 accompanies this spec PR under Conductor Phase 2 step 5; acceptance and challenge clearance are not claimed. The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-414/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md
├── tasks.md
├── checklists/requirements.md
└── DRAFTING_RECEIPT.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes approach, files, classification, publication limits, recovery, activation, and proof order. There is no new public HTTP contract. No `design.md` (no ui tag). Proposed ADR `docs/adr/0026-separate-feature-generation-and-recovery.md` (status: proposed) accompanies this spec PR (Keel, Conductor Phase 2 step 5); acceptance is not claimed.

### Source Code (repository root)

```text
# New Core
src/LamuFlix.Core/Features/Enrichment/ProcessFeaturesCommand.cs
src/LamuFlix.Core/Features/Enrichment/ProcessFeaturesCommandHandler.cs
src/LamuFlix.Core/Features/Enrichment/ProcessFeaturesOutcome.cs
src/LamuFlix.Core/Features/Enrichment/SweepMissingMovieFeaturesCommand.cs
src/LamuFlix.Core/Features/Enrichment/SweepMissingMovieFeaturesCommandHandler.cs

# Extended ports + adapters
src/LamuFlix.Core/Ports/IMovieRepository.cs                    # + GetPlotAsync
src/LamuFlix.Core/Ports/IMovieFeaturesRepository.cs            # + FindMovieIdsMissingFeaturesAsync
src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieFeaturesRepository.cs

# New / extended RabbitMq + composition
src/LamuFlix.Infrastructure/RabbitMq/FeaturesConsumer.cs
src/LamuFlix.Infrastructure/RabbitMq/FeaturesRouting.cs
src/LamuFlix.Infrastructure/RabbitMq/RabbitMqEnrichmentQueuePublisher.cs  # typed retry/DLQ overload
src/LamuFlix.Infrastructure/RabbitMq/RabbitMqServiceCollectionExtensions.cs
src/LamuFlix.Infrastructure/Enrichment/StrandedMovieSweeper.cs
src/LamuFlix.Api/Program.cs                                    # composition as needed

# Vocabulary (same Spec PR)
CONTEXT.md                                                     # additive Q9 terms only

# Tests (illustrative; exact files follow existing test layout in Build)
tests/LamuFlix.UnitTests/...
tests/LamuFlix.IntegrationTests/...
```

No `/web` edit. No `harness.yml` or gate-script edit. No migration file.

**Structure Decision**: Mirror EnrichmentConsumer / EnrichmentRouting / ProcessEnrichmentCommandHandler beside existing enrichment feature folder and RabbitMq infrastructure (recon-DEV-414). Port extensions stay on existing interfaces. Sweeper gains a second independently guarded pass without becoming a claiming enrichment consumer (Q2, Q5).

## Design Overview

1. **Ports (Q6; Patron F1).** Add `GetPlotAsync` collapsing missing movie and null plot to null via no-tracking scalar projection that returns the stored plot value unchanged (no normalization in that projection). Add `FindMovieIdsMissingFeaturesAsync` as Movies ⟕ movie_features anti-join whose nonblank-plot eligibility matches the handler's canonical .NET `string.IsNullOrWhiteSpace` / `Char.IsWhiteSpace` Unicode semantics (empty, spaces-only, tab/newline-only, Unicode-whitespace-only excluded; any non-whitespace character included); no N+1; no driver mocks; no SQL TRIM/btrim equivalence claim. If exact parity cannot be expressed in PostgreSQL, return needs decision to Patron.

2. **Handler (Q2–Q4; Patron F1).** ProcessFeaturesCommandHandler loads plot; null/missing or `string.IsNullOrWhiteSpace` → successful no-op outcome; nonblank → generate one vector, validate, upsert with TimeProvider ComputedAt; never touches EnrichmentStatus/EnrichmentAttempts. Classification: EmbeddingGenerationException.Category first, else EnrichmentFailureClassifier; whitelist four codes; invalid payload/Attempt&lt;1/invalid vector → permanent invalid_response.

3. **Consumer and routing (Q4, Q6–Q7; Patron F3).** FeaturesConsumer + FeaturesRouting mirror EnrichmentConsumer disposition (retry vs application-published DLQ, sanitized headers on application-published messages only, ack-after-publish, shutdown nack/requeue with no Attempt increment). Application sanitized category/attempt/exhaustion headers constrain only retry/DLQ messages the application itself publishes; broker-generated dead-letter entries are outside that header/accounting contract. Retain existing topology. Publisher overload carries FeaturesRequested retry/DLQ through existing PublishCoreAsync options. Do not claim unverified broker confirm await semantics or infinite requested-queue residence; live-broker tests are authoritative, including repeated shutdown-requeue coverage that records actual queue residence, broker headers/body, and application Attempt. Any required topology change returns to Patron.

4. **Recovery (Q5).** SweepMissingMovieFeaturesCommandHandler uses the anti-join and enqueues Attempt 1. StrandedMovieSweeper runs enrichment and feature passes independently on SweepInterval; feature pass only when feature composition is active.

5. **Activation and observability (Q7; Patron F7).** Feature-specific IsFeaturesConsumerActive-style predicate (four prerequisites, not IMetadataProvider). Concrete feature liveness + features-consumer readiness on existing ready endpoint. Trace and structured logs carry MovieId, Attempt, safe category, and no-input outcomes without raw exception text; feature handler/consumer/recovery logs exclude plot text and embedding vector contents.

6. **Vocabulary (Q9).** Additive CONTEXT.md entries in this Spec PR; retain all existing definitions verbatim.

7. **Proof order (Q8).** Build pickup drift/baseline → ports/EF/query tests (including real-PostgreSQL handler/anti-join whitespace parity corpus) → handler unit tests (same corpus; safe log content excludes plot/vector) → consumer/routing/publisher/liveness live RabbitMQ tests (including application-published sanitized headers and repeated shutdown-requeue residence/header/Attempt recording) → independent recovery integration → active/inactive composition + E2E. Anvil one production phase per ask; Cog refactor once; Gauge gates; three-axis review. Q1 verdict handling exact.

## Loop discipline

- **Closing bar:** Critical and High block. Medium and Low require explicit disposition: fix in scope, follow-up ticket if charter criteria apply, or noted no ticket. Follow-ups only for out-of-scope Critical/High, broken behavior, or user request.
- **Round cap:** two review rounds; at most two remediation commits per round. Third-round findings go to user review with the PR.
- **Frozen scope:** features.requested consumer; plot embedding via existing generator with MovieId upsert; bounded per-chain retry/DLQ within corrected publication limits; independent missing-features recovery with deliberate per-sweep Attempt 1 reset; minimal existing-port/EF/publisher/composition extensions; feature liveness/trace; required tests/gates ordering; additive CONTEXT vocabulary. Anything else is a follow-up issue, not a finding in this round.
- **Gate verdicts:** exit 1, Could not run, unaccepted property-test exit 2 block. Preserve scope-empty SKIPPED, mutation NOT APPLICABLE, harness-disabled SKIP, recorded property-test opt-out verbatim; never PASS.

## Complexity Tracking

> No constitution violations requiring justification. File placements and CONTEXT.md additive edit are Patron-ruled under ticket scope / item 6 without owner checkboxes. Package set unchanged. Proposed ADR `docs/adr/0026-separate-feature-generation-and-recovery.md` accompanies this spec PR (Keel, Conductor Phase 2 step 5; status: proposed); acceptance and challenge clearance are not claimed.
