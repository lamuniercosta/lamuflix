# Implementation Plan: DEV-353 Recency-weighted TasteProfile

**Branch**: `feature/353-spec` | **Date**: 2026-10-22 | **Spec**: `specs/DEV-353/spec.md`

**Input**: Feature specification from `specs/DEV-353/spec.md`, settled grill brief `specs/DEV-353/brief.md`, facts from notes `recon-DEV-353` and `recon-DEV-353-2`, Patron rulings in `specs/DEV-353/CONCLUSIONS.md`, `specs/DEV-353/ASSUMPTIONS.md`, feature glossary `specs/DEV-353/CONTEXT.md`.

**Note**: Planning artifact only (Phase A). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. Phase B remains subject to Epic 7 sequencing (`specs/PRODUCT.md:17`) and a fresh drift recon.

## Summary

Deliver a pure Core `TasteProfileCalculator` that computes a recency-weighted **TasteProfile** (independent **genre preferences** and **plot centroid**) from materialized **watch signals**, plus `RecommendationOptions.RecencyDecayLambda` and focused unit/property tests. Caching, history loading, DEV-413 repository publication, DI/handler/API/UI wiring stay out. Nothing else ships.

## Technical Context

**Language/Version**: C# / .NET 10. Core domain/options/recommendations feature folder and named unit/property tests. No web change.

**Primary Dependencies**: None added. Existing BCL (`TimeProvider`, `System.Collections.Immutable`, `Math`). Existing `GenreWeights` and `EmbeddingDimensions.Value` (384). Tests use existing xUnit v3 and Shouldly under `tests/LamuFlix.UnitTests`. Property-test framework, category trait, and seed conventions are verified at Phase B T001 against the live project and property-test gate; stop for recon or a cited ruling if any are missing. No new package.

**Storage**: N/A. No schema, migration, or persistence in this ticket. Caller materializes values; DEV-413/351/352 landing is out of scope.

**Testing**: xUnit v3, Shouldly, Theory/MemberData hand oracles, and property tests under the existing framework/category/seed conventions verified at T001. Test-local BCL `TimeProvider` subclass counting `GetUtcNow`. No Testcontainers, no EF InMemory, no driver mocking, no new package, no property-test opt-out, no Gherkin/Reqnroll.

**Target Platform**: Existing LamuFlix.Core library consumers. No new host or port.

**Project Type**: Existing ports-and-adapters solution. Domain records for shared contracts; Recommendations feature folder for the pure calculator; ticket-forced edit to existing `RecommendationOptions`. Not a new project or layer.

**Performance Goals**: No numeric latency target in the brief. Numerical stability via per-channel `age - a_min` rescaling (brief Q6).

**Constraints**: TreatWarningsAsErrors. No new NuGet/npm dependency. No new project, top-level folder, or architectural layer. No new port or ADR. No public API route/DTO/status change. No schema/migration. No DI/handler/config-file edits. No `Features:LocalPlay`, secrets, or `Process.Start`. No DEV-415 cache. Thresholds stay in `harness.yml`. File envelope frozen in brief Exact file envelope.

**Scale/Scope**: Exactly the eight production/test files named in brief Exact file envelope, plus Spec Kit docs under `specs/DEV-353/`. Feature-local glossary already in `CONTEXT.md`. No root `CONTEXT.md` edit. No deletion or unrelated rewrite.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-353 note:28-38: pure Core recency-weighted genre/embedding average, lambda in RecommendationOptions, injected clock, cold-start empty profile; caching split to DEV-415. Nothing is added or dropped. No owner checkbox (brief Q1–Q10).
- Core organisation: shared Domain types (`TasteProfileMovie`, `TasteProfile`), Recommendations feature folder for `TasteProfileCalculator`, options edit on existing `RecommendationOptions`. Feature folders MUST NOT reference each other; no PlaybackEvent feature-folder dependency (brief Q3; constitution I; recon-DEV-353:125).
- Port closed set: no new port; materialized inputs; DEV-413 draft repository/snapshot not implemented or published here (brief Q3, Q8; recon-DEV-353-2:9-19).
- Core must not reference EF Core/Npgsql (constitution I). Calculator is synchronous in-memory math only.
- No MediatR, generic repository, or UnitOfWork (PRODUCT.md speculative abstractions; constitution II).
- No endpoint, status code, OpenAPI, or generated TypeScript change (PRODUCT.md care list item 4).
- No schema or migration (care list item 3; brief Q8).
- No secret, `Features:LocalPlay`, or `Process.Start` (care list item 5).
- Options and time: `RecencyDecayLambda` validated; `TimeProvider` injected; no `DateTime.Now` (constitution VII; recon-DEV-353:127).
- Vocabulary: four terms in feature-local `CONTEXT.md` only; no root glossary edit (brief Q10; constitution VIII).
- Tests: xUnit v3, Shouldly, and property tests under conventions verified at T001 under existing `tests/LamuFlix.UnitTests` (constitution IX; recon-DEV-353:174-184; brief Q9). Gates from configured scripts; never lower thresholds (brief Gate expectations).
- Immutable collections for record collections (constitution.md:428; recon-DEV-353:130).

Post-design re-check: the design below adds no package, project, new port, route, schema, or file outside the approved set. The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-353/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── CONTEXT.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes contracts, files, ordering, and proof requirements. There is no new HTTP public contract. `AGENTS.md` is not updated: it is outside the feature directory and outside the named file set. No `design.md` (ui: none).

### Source Code (repository root)

```text
src/LamuFlix.Core/Domain/TasteProfileMovie.cs
src/LamuFlix.Core/Domain/TasteProfile.cs
src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs
src/LamuFlix.Core/Options/RecommendationOptions.cs

tests/LamuFlix.UnitTests/Domain/TasteProfileTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/TasteProfileCalculatorTests.cs
tests/LamuFlix.UnitTests/Options/RecommendationOptionsRecencyTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/TasteProfileProperties.cs
```

**Structure Decision**: Existing Core Domain, Options, and Recommendations feature folder only, plus the four named test files under `tests/LamuFlix.UnitTests`. If the contract cannot be implemented inside this set, stop for a decision. Do not widen the set.

## Design Overview

1. **Recency option (brief Q1).** Add `RecencyDecayLambda` to `RecommendationOptions` with default `Math.Log(2.0) / 30.0`. Validate finite and strictly `> 0.0` in the existing `IValidatableObject` path without changing `MmrLambda`, `Take`, `CandidatePoolSize`, or `Weights` semantics. No appsettings edit in this ticket.

2. **Domain contracts (brief Q3).** `TasteProfileMovie` and `TasteProfile` as sealed records in Core/Domain. `IsEmpty` is a computed getter. Default/empty `PlotEmbedding` means absent; provided vectors must be 384 finite floats when nonempty.

3. **Calculator boundary (brief Q3, Q8).** `TasteProfileCalculator` takes `TimeProvider` and `RecommendationOptions`, exposes synchronous `Calculate`. Caller materializes movies and supplies `GenreWeights`. No repository, cache, DI registration, handler, or route.

4. **Watch signals (brief Q2).** Expand each movie into dated launch signals plus at most one undated mark signal when launches are absent and `IsWatched` is true. Preserve duplicate launch timestamps. Do not invent timestamps.

5. **Time and weights (brief Q6).** Sample `GetUtcNow` once. Age with fractional `TotalDays` and future clamp. Per channel, rescale with `exp(-lambda * (age - a_min))` before accumulation.

6. **Genre channel (brief Q4).** Canonicalize genres; weighted average of `GenreWeights.GetWeight` over genre-bearing signals; OrdinalIgnoreCase dictionary; omit exact zeros; no L1 or SimilarityWeights.Genres scalar.

7. **Plot channel (brief Q5).** Coordinate-wise weighted mean in double, cast to float; independent denominator; empty when no contributors or all-zero mean; zero vectors participate.

8. **Validation (brief Q7).** Constructor and Calculate preconditions as ruled; validate all rows before accumulation; never mutate inputs.

9. **Proof order (brief task ordering / Q9).** (1) Phase B read-only drift/scheduling/property-discovery recon. (2) Domain contracts + RecencyDecayLambda with tests. (3) Calculator watch/genre/plot implementation with independent unit oracles. (4) Validation, stability, determinism, and property coverage. (5) Refactor and applicable live gates/review.

10. **Gates (Phase B).** Run configured scripts from the Build workspace after Gate 1. Report native exits accurately; scope-empty SKIPPED / harness SKIP / mutation N/A are never PASS. Property opt-out is not accepted. Mutation via `scripts/run-mutation.ps1` without `--since` in worktrees. Surviving mutants require tests. No separate architect/Gherkin stage unless acceptance features exist (they do not).

## Domain vocabulary (feature-local)

Copied from `specs/DEV-353/CONTEXT.md` / `spec.md` so plan and spec stay aligned:

| Term | Definition | Avoid |
| --- | --- | --- |
| TasteProfile | Personal preference signal from watched movies with independent genre and plot components; empty means no personal signal | GenreWeights, recommendation score |
| Watch signal | Dated playback launch, or undated manual watched mark when that movie has no launch; repeats are separate | completed viewing, playback progress |
| Genre preference | Personal genre preference combining recency with library rarity | GenreWeights, genre scoring coefficient |
| Plot centroid | Recency-weighted average of available plot embeddings; empty means no directional plot preference | plot similarity score, generated embedding |

## Loop discipline

- **Closing bar:** Critical, High, and Medium findings within frozen scope block. Low is recorded nonblocking. Missing required axis, failed/unrunnable required gate, or actual unresolved ticket/constitution departure blocks independently of severity. Unnecessary abstractions are follow-ups, never fix commits (brief Q10).
- **Round cap:** Two for plan correction/challenge and two for formal review/remediation at their respective stages. Remaining above-bar findings after the cap stop and report to Bernstein. No third round or automatic waiver.
- **Frozen scope:** Pure materialized-input TasteProfile computation with Q1–Q9 exact contracts/semantics, `RecommendationOptions.RecencyDecayLambda` and its validation, and the prescribed unit/property tests; anything else is a follow-up issue, not a finding in this round.
- **Exclusions:** DEV-351/352 schema or readers, DEV-413 port/repository/ADR publication, DEV-415 cache, DI/handler/API/UI/config edits, IsNotInterested/negative taste, recommendation routes/ranking/MMR, new port/package/project/layer/schema, root glossary edit, LocalPlay/secrets/Process.Start, and unrelated cleanup.

## Complexity Tracking

No constitution violation requires tracking. Patron recorded no constitution departure and no §2.3 escalation (brief Q1–Q10).
