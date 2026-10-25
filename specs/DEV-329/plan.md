# Implementation Plan: DEV-329 Stage EnrichmentRequested in transactional outbox with movie write

**Branch**: `feature/329-spec` | **Date**: 2026-10-25 | **Spec**: `specs/DEV-329/spec.md`

**Input**: Feature specification from `specs/DEV-329/spec.md`, OPEN grill brief `specs/DEV-329/brief.md` (Q1–Q13 including Q6a), facts from notes `DEV-329`, `recon-DEV-329` through `recon-DEV-329-5`, recorded Q7 Gauge baseline refresh (task note DEV-329:125-171), Patron rulings in `specs/DEV-329/CONCLUSIONS.md`, `[assumed]` receipt `specs/DEV-329/ASSUMPTIONS.md`, design authority ADR-0027 at cited revision `cb6c6a780d77f41b32d69689f237bdce43d2562d` (Q2).

**Status**: Draft planning artifact only (Phase A). Brief not frozen; Gate 1 not claimed. No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. **Phase B STOP** until pickup verifies landed DEV-419 authority and main drift — do not treat the unmerged cited branch object as landed (brief Q2, Q9).

## Phase B entry preflight (executable before Code Phase 1)

Schedule these Build-workspace checks before production edits. Do not run gates, tests, or analyzers from Spec as substitutes for Phase B. Missing evidence blocks code.

| Check | Owner / evidence | Blocks code if missing |
|---|---|---|
| Spec PR merged and Gate 1 established | Rigger / YouTrack Ready-to-build handoff | Yes |
| Phase B pickup verifies landed DEV-419 (ADR-0027 outbox authority) on main and recon drift vs authorized Q7 file set; stop on missing landing or mismatch rather than using cited branch tip alone (brief Q2, Q9) | Wisp → recon refresh; Keel clearance | Yes |
| Q7 Gauge baseline refresh already recorded for the 14 existing Q7 files; net-new files marked absent (task note DEV-329:125-171). Re-measure only if the approved file set changes again | Gauge → DEV-329 note | Yes (stale/missing baseline after file-set change) |
| Keel drift check or amend against brief/spec before code | Keel handoff in DEV-329 | Yes |

## Summary

Deliver typed EnrichmentRequested staging for ImportMovieFolder and RequestEnrichment before their shared EfMovieRepository path-A save: Core port `IEnrichmentOutbox.Stage`, scoped EF adapter sharing the movie DbContext, four-column `outbox_messages` with one EF-tool-generated AddEnrichmentOutbox migration and pending index, lossless Web-default bytea payload, retained post-save direct enqueue, and Q8 proofs for wire compatibility, atomicity, schema, DI, and architecture. No relay, no direct-publish removal, no other enqueue paths, no EfMovieRepository production edits, no public API/web changes.

## Domain vocabulary

| Term | Definition source | Notes |
|------|-------------------|-------|
| staged | brief Q12 | In-memory tracking by Stage before SaveChangesAsync |
| durable | brief Q12 | Committed in Postgres with the movie write |
| unprocessed | brief Q12 | ProcessedAt IS NULL; direct enqueue never marks processed |
| path A | recon-DEV-329-4:47-49; brief Q6 | No-metadata EfMovieRepository save path |
| EnrichmentRequested | recon-DEV-329:31; brief Q3–Q4 | Existing typed payload (MovieId, Attempt) |

CONTEXT.md is not edited. No new ADR is authored by this ticket (ADR-0027 from DEV-419 remains design authority once landed).

## Technical Context

**Language/Version**: C# / .NET 10 backend. No web change.

**Primary Dependencies**: None added. Reuse existing EF Core, Npgsql, System.Text.Json, xUnit/Shouldly/NSubstitute/Testcontainers already in the solution. No new NuGet/npm dependency (brief Q3, Q7; recon-DEV-329:73).

**Storage**: PostgreSQL table `outbox_messages` only (brief Q5). Same Postgres and same transaction as the movie write (ADR-0027:15; brief Q2).

**Testing**: xUnit v3; Shouldly; NSubstitute; real Postgres Testcontainers for adapter/atomicity/migration proofs. Never mock driver translation/serialization. No Gherkin/Reqnroll. Property tests via configured harness scripts in Phase B (brief Q8, Q10). All gate/test execution belongs to Phase B / Build workspace except the already-recorded Gauge baseline measurement.

**Target Platform**: Existing `LamuFlix.Api` host. No public HTTP/OpenAPI shape change.

**Project Type**: Existing ports-and-adapters solution. New port in `LamuFlix.Core.Ports`; adapter/record/configuration/migration under existing Infrastructure Persistence folders; tests in existing unit/integration/architecture projects (brief Q7).

**Performance Goals**: None in the brief. Pending index supports future relay polling without implying ordering or delivery guarantees (brief Q5).

**Constraints**: TreatWarningsAsErrors. Ticket scope authoritative. No LocalPlay/secrets/`Process.Start`. No public HTTP/OpenAPI/web. No new dependency/project/layer. No EfMovieRepository production edits. No publisher/consumer/relay edits. Never hand-edit EF-generated migration/Designer/snapshot. Mutation never uses `--since`. Thresholds never lowered. Scope-empty/opt-out/N/A/Could not run never PASS. Phase B STOP on unverified DEV-419 landing/drift.

**Scale/Scope**: Exact Q7 21-file production/test delivery set (below). Unanticipated files return to Keel for a new cited Patron ruling. Frozen scope sentence (brief Q12): DEV-329 delivers only typed EnrichmentRequested staging for ImportMovieFolder and RequestEnrichment before their shared path-A save; four-column outbox_messages with generated EF migration and pending index; scoped EF adapter and lossless Web-default payload; retained post-save direct enqueue; the Q7 exact files and Q8 tests proving wire compatibility, atomicity, schema, DI and architecture. Exclude relay/retention execution, direct-publish removal, other enqueue paths, metadata-save compatibility, new dependencies/layers, public API/web changes and other tables; metadata B/C is noted, no ticket with the pickup guard. anything else is a follow-up issue, not a finding in this round.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Every gate in this Constitution Check cites a numbered Core Principle (I–IX). Constraints that live only under unnumbered constitution sections are listed in **Unnumbered constitution authority checks** below, not here. Scope and Phase B prerequisites remain under **Scope and prerequisite checks**.*

- **Principle I** (ports and adapters, feature-organised Core; `.specify/memory/constitution.md:127-153`): Ticket-named `IEnrichmentOutbox` extends existing `LamuFlix.Core.Ports` whitelist explicitly (brief Q3; charter §2.3 item 2). Core Stage signature stays EF-free; Infrastructure owns the adapter. No new project/layer/generic outbox abstraction. ArchitectureTests whitelist update is required proof, not a threshold change (recon-DEV-329:68-69).
- **Principle II** (explicit handlers and decorators; `.specify/memory/constitution.md:161-169`): No MediatR. Existing `AddHandler` Scoped registrations remain; HandlerRegistration.cs unchanged (recon-DEV-329-5:23-36; brief Q7).
- **Principle VII** (configuration isolation and deterministic time; `.specify/memory/constitution.md:275-294`): `CreatedAt` from injected `TimeProvider.GetUtcNow()` at Stage; no `DateTime.Now`. No secrets, LocalPlay, or `Process.Start` (brief Q5, Q7).
- **Principle VIII** (ubiquitous language; `.specify/memory/constitution.md:296-311`): Feature-local terms staged/durable/unprocessed only; no root CONTEXT.md edit; no new ADR authored here (brief Q9, Q12).
- **Principle IX** (test pyramid; `.specify/memory/constitution.md:316-343`): Unit orchestration + real Postgres Testcontainers for serialization/atomicity/migration; existing assertion/mocking libraries only; never mock Infrastructure drivers for translation/serialization (brief Q8).

### Unnumbered constitution authority checks

Binding constraints from unnumbered constitution sections. **Numbered principle: none** for these items; do not invent I–IX mappings.

- **Technology Stack Constraints**: Existing .NET 10 / EF Core / Npgsql / System.Text.Json stack; no new package (brief Q3, Q7).
- **Coding Conventions — Comments**: No explanatory comments beyond AAA test headers, empty-block justifications, and scoped warning suppressions.
- **Pull Request Quality Gates**: Configured scripts and harness thresholds only; never lower thresholds or invent a threshold value (brief Q10 / `AGENTS.md`). Generated migrations/snapshot exclusions reported explicitly.

Post-design re-check: the numbered-principle Constitution Check gates above still hold. This plan adds the ruled port/adapter/schema/migration/handler wiring and Q7 test adaptations with no public HTTP contract, no new dependency/project/layer, no EfMovieRepository rewrite, and no harness edit. Unnumbered constitution authority checks and scope/schema/Phase B constraints remain in force under their sections below; they are not waived by this re-check. Side design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) are not produced by Quill — the brief already records/settles the proposed contracts, files, ordering, and proofs (OPEN / NOT FROZEN). No `design.md` (no ui tag).

## Scope and prerequisite checks

Binding product, care-list, and Phase B prerequisite constraints. **Numbered principle: none applicable** for each item; do not invent I/II/IX citations. Constraints remain in force.

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). DEV-329:27-29 fixes outbox_messages schema + EF migration; IEnrichmentOutbox.Stage; ImportMovieFolder and RequestEnrichment stage before SaveChangesAsync; retain direct enqueue; no relay; unit + integration tests. Nothing added or dropped beyond the brief. No owner checkbox (brief ticket-decided scope; Q7).
- Schema care list (§2.3 #3): only ticket-named `outbox_messages` (brief Q5).
- Public API shape (§2.3 #4): unchanged; no OpenAPI/web regeneration (brief Q7, Q10).
- Epic / sequencing: DEV-420 owns relay/retention/direct-publish removal. **Phase B STOP** until landed DEV-419 authority and main drift are verified at pickup; cited remote branch tip alone is insufficient (brief Q2, Q9). Q2 provenance caveats (no fetch; main integration not established at grill) remain preserved.
- File scope (§2.3 #6): exact Q7 set; supporting DI/test/architecture adaptations approved; additional files need a new cited ruling.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-329/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md
├── tasks.md
└── DRAFTING_RECEIPT.md
```

`research.md`, `data-model.md`, `quickstart.md`, `contracts/`, and `checklists/` are not produced. No `design.md` (no ui tag). No clarify/checklist or new ADR on this M path (brief Q9–Q10).

### Source Code (repository root) — exact Q7 boundary

```text
# New production
src/LamuFlix.Core/Ports/IEnrichmentOutbox.cs
src/LamuFlix.Infrastructure/Persistence/EfEnrichmentOutbox.cs
src/LamuFlix.Infrastructure/Persistence/Records/OutboxMessageRecord.cs
src/LamuFlix.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/<EF timestamp>_AddEnrichmentOutbox.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/<same EF timestamp>_AddEnrichmentOutbox.Designer.cs

# Existing production (bounded edits)
src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs
src/LamuFlix.Core/Features/Enrichment/RequestEnrichmentCommandHandler.cs
src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs
src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs

# Tests (bounded edits + one new suite)
tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs
tests/LamuFlix.UnitTests/Features/Enrichment/RequestEnrichmentCommandHandlerTests.cs
tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs
tests/LamuFlix.IntegrationTests/ImportMovieFolderCommandHandlerPersistenceTests.cs
tests/LamuFlix.IntegrationTests/EnrichmentEndpointTests.cs
tests/LamuFlix.IntegrationTests/ImportEndpointTests.cs
tests/LamuFlix.IntegrationTests/MigrationTests.cs
tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs
tests/LamuFlix.IntegrationTests/EnrichmentOutboxPersistenceTests.cs
tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs
```

**Outside delivery (do not edit)**: `HandlerRegistration.cs`, `EfMovieRepository.cs`, broker/consumer, other handlers, API contracts, `web/`, `Directory.Packages.props`.

**Structure Decision**: Match brief Q7 exact paths. EF timestamp is tooling-supplied at implementation. Latest prior migration at HEAD is `20261022101541_AddMovieFeatures` (recon-DEV-329 / task note); do not invent the new timestamp in Phase A.

## Design (from settled brief)

1. **Authority (Q1–Q2)**: DEV-419 ADR-0027 supplies same-Postgres/same-transaction staging, ProcessedAt after settled broker confirmation, at-least-once, one Api relay and retention as fixed outcomes. Schema/Stage/handler boundaries are delegated and ruled here. Phase B verifies landing.
2. **Port and UoW (Q3)**: `void Stage(EnrichmentRequested)`; scoped adapter; stage before save; retain enqueue after success; no FeaturesRequested.
3. **Payload (Q4)**: Adapter-local Web defaults → lossless bytea; byte parity + consumer round-trip + Attempt preservation; PublishRawAsync is future reuse only.
4. **Schema (Q5)**: Four columns + pending partial index; TimeProvider CreatedAt; null ProcessedAt; distinct repeated rows; DEV-420 owns mark/retention.
5. **Save/failure (Q6/Q6a)**: Path A only; both-or-neither; precise rollback assertions; dispose failed scopes; post-commit publish failure retains rows; B/C noted no ticket with pickup guard; no EfMovieRepository edits.
6. **Files (Q7)**: Exact 21-file set above; Gauge baseline refresh recorded; additional files need new ruling.
7. **Tests (Q8)**: Unit orchestration + EnrichmentOutboxPersistenceTests + Migration/Composition/Architecture proofs; existing libraries only.
8. **Ordering (Q9)**: Pickup STOP → foundation (port/schema/adapter/DI + early proofs) → coherent handler wiring/fixtures → full atomicity/migration/composition/architecture → verification/review.
9. **Gates (Q10)**: Phase-separated expectations; live harness thresholds; no web/OpenAPI gates.
10. **Loop discipline (Q11–Q13)**: Critical/High/Medium block; three axes required; two+two round cap [assumed]; Q12 scope sentence governs findings.

## Complexity Tracking

> No constitution violations requiring justification. Ticket-named port extends existing Ports whitelist under Patron Q3; schema is ticket-decided; no new architectural layer.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phase boundaries

- **Phase A (this ask)**: Spec Kit docs under `specs/DEV-329` only (`spec.md`, `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md`). No freeze, publish, gates, or implementation. Keel owns read-only speckit-analyze and brief alignment next. Challenge, brief/plan freeze, and Gate 1 remain distinct.
- **Phase B**: After Gate 1 and verified landed DEV-419 + drift clearance (STOP otherwise). Then Build implements per `tasks.md`; Cog refactor; final post-refactor configured build/format and full `dotnet test` (unit/integration/architecture) on the changed HEAD before Gauge gates per brief Q10 (`tasks.md` T017–T019).
