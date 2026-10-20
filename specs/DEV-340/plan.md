# Implementation Plan: DEV-340 GetRecommendationsQuery and Phase A candidate provider

**Branch**: `feature/340-spec` | **Date**: 2026-10-20 | **Spec**: `specs/DEV-340/spec.md`

**Input**: Feature specification from `specs/DEV-340/spec.md`, settled grill brief `specs/DEV-340/brief.md`, facts from notes `recon-DEV-340`, `recon-DEV-340-2`, `recon-DEV-340-3`, `recon-DEV-340-3-2`, Patron rulings in `specs/DEV-340/CONCLUSIONS.md`, taste receipt `specs/DEV-340/ASSUMPTIONS.md`.

**Status**: Plan-challenge round 1 in progress (not frozen; Gate 1 not claimed). Planning artifact only (Phase A). No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1, and Phase B remains gated on Epic 7 plus landed DEV-336/337/338 (`specs/PRODUCT.md` sequencing; brief Q2/Q5).

## Summary

Deliver `GetRecommendationsQuery` / handler / validator, ticket-owned `RecommendedMovie`, constitution-whitelisted `IRecommendationCandidateSource` with Phase A EF adapter (all enriched library movies except selected, one untracked query), orchestration that consumes frozen DEV-336/337/338 scoring contracts, deterministic rank-and-take with unmodified reason chips, and focused unit/property/Postgres/composition tests. No HTTP route or web UI. Nothing else ships.

## Technical Context

**Language/Version**: C# / .NET 10. Core feature folder and Ports, Infrastructure Persistence, Api handler registration, and named unit/integration tests. No web change (`ui:none`).

**Primary Dependencies**: None added. Consume existing FluentValidation, Options (`RecommendationOptions` / `SimilarityWeights`), EF Core/Npgsql in Infrastructure only, and frozen sibling types once landed. Tests use existing xUnit, Shouldly, NSubstitute, AutoFixture, FsCheck, and Testcontainers PostgreSQL conventions.

**Storage**: Existing PostgreSQL `movies` and join tables only. Read-side Phase A candidate query. No migration or schema change.

**Testing**: Existing xUnit/Shouldly/NSubstitute (I/O ports only)/AutoFixture/FsCheck conventions; real PostgreSQL Testcontainers for both candidate-adapter proof files (IntegrationTests and UnitTests survivor per brief PD1). Composition via `ApiHostCompositionTests` / `ManifestContracts`. No EF InMemory, no driver mocking for translation/serialization, no second assertion/mocking library, no Gherkin/Reqnroll. Gate obligations: brief Q5 Gauge expectations and `AGENTS.md` verification commands / harness thresholds — do not restate unsupported analyzer specifics here.

**Target Platform**: Existing LamuFlix.Api host. No new host, endpoint, or OpenAPI shape.

**Project Type**: Existing ports-and-adapters / vertical-slice solution. Recommendations feature folder (constitution-pre-named), Ports for the candidate source, Persistence for the EF adapter. Not a new project or layer.

**Performance Goals**: No numeric latency target in the brief. Full enriched-except-selected pool has deliberate Phase A memory/cartesian cost (brief Q3/PD2); vector/MMR retrieval belongs elsewhere. No projection rewrite is mandated.

**Constraints**: No new NuGet/npm dependency. No new project, top-level folder, or architectural layer. No new ADR (port already in closed set / whitelist). No public API route/DTO/status change. No schema/migration. No `Features:LocalPlay`, secrets, or `Process.Start`. Do not stub sibling implementations. Thresholds stay in `harness.yml`. Property tests planned, not opted out. Follow established project conventions and mandatory gate scripts per brief Q5 / `AGENTS.md`.

**Scale/Scope**: Exactly the approved file set in brief Q5/PD1 (six new production files, five new test files including the UnitTests survivor, three bounded edits). No deletion or unrelated rewrite.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-340 verbatim Scope & Technical Design and Acceptance Criteria (brief; recon-DEV-340-2 §5). Nothing is added or dropped. No owner checkbox (brief Q5–Q6).
- Core organisation: Recommendations feature folder for query/result/handler/validator; Ports for `IRecommendationCandidateSource`; Infrastructure Persistence for EF adapter. Feature folders MUST NOT reference each other; shared concepts live in Domain/Ports/Pipeline (constitution I; recon-DEV-340:53). Recommendations folder is constitution-pre-named, not a new architectural layer (care list item 2; brief Q5).
- Port closed set: `IRecommendationCandidateSource` is already enumerated; implementing the source file does not add a new port name. No ADR; architecture whitelist already contains the name (recon-DEV-340:51-52; brief Q3/Q6).
- Core must not reference EF Core/Npgsql (constitution I). Candidate SQL stays in `EfRecommendationCandidateSource`.
- No MediatR, generic repository, or UnitOfWork (PRODUCT.md speculative abstractions; constitution II). Handler implements `IQueryHandler<,>` directly.
- No endpoint, status code, OpenAPI, or generated TypeScript change (constitution API rules; PRODUCT.md care list item 4; brief Q1/Q5). Route manifest stays unchanged.
- No schema or migration (care list item 3; brief Q5).
- No secret, `Features:LocalPlay`, or `Process.Start` (care list item 5; recon-DEV-340:79).
- Vocabulary: **Recommendation** and **Reason** already in root `CONTEXT.md`; no new glossary term (brief Q6).
- Tests: established xUnit/Shouldly/NSubstitute/AutoFixture/FsCheck/Testcontainers Postgres conventions (constitution IX; brief Q5/PD1). Gates from configured scripts; never lower thresholds (brief Q5 Gauge expectations; `AGENTS.md`).
- Lifetimes: scoped candidate source; handler/validator via existing `AddHandler` pipeline; consume DEV-337 shared cache/calculator registrations as landed — do not create a second cache (brief Q5). Drift recon re-reads the shared `PersistenceServiceCollectionExtensions` body after siblings land (brief PD2).

Post-design re-check: the design below adds no package, project, new port name, route, schema, or file outside the approved set (five test files per PD1). The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-340/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes contracts, files, ordering, and proof requirements. There is no new HTTP public contract. `AGENTS.md` is not updated: it is outside the feature directory and outside the named file set.

### Source Code (repository root)

```text
src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQuery.cs
src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryValidator.cs
src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs
src/LamuFlix.Core/Features/Recommendations/RecommendedMovie.cs
src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs
src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs
src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
src/LamuFlix.Api/HandlerRegistration.cs

tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryValidatorTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs
tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs
tests/LamuFlix.UnitTests/Persistence/EfRecommendationCandidateSourceSurvivorTests.cs
tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs
```

**Structure Decision**: Existing Core Features/Recommendations and Ports plus Infrastructure Persistence and Api handler registration only, plus the named tests (five new test files per brief PD1; IntegrationTests retained). Infrastructure mutation selects UnitTests only (recon-DEV-340 section 11); thresholds/config unchanged. If the contract cannot be implemented inside this set, stop for a decision. Do not widen the set.

## Design Overview

1. **Contracts.** `sealed record GetRecommendationsQuery(MovieId MovieId, int Take = 10)`; `sealed record RecommendedMovie(MovieSummary Summary, double Score, ImmutableList<Reason> Reasons)`; FluentValidation validator with `Take > 0` only (handler owns default-10 and options-independence proofs). Namespace `LamuFlix.Core.Features.Recommendations`.

2. **Candidate port.** `IRecommendationCandidateSource.GetCandidatesAsync(MovieId selectedMovieId, CancellationToken ct)` → `Task<IReadOnlyList<Movie>>`. Domain `Movie` only; no candidate DTO. Selected-id exclusion is the adapter contract (brief PD2); handler does not add a defensive filter.

3. **Phase A EF adapter.** `EfRecommendationCandidateSource`: `AsNoTracking` query, `Status == Enriched`, exclude selected id, include Actors/Directors/Genres/Writers + scalars/flags, exactly one SQL command and one materialization (thread `ct` into EF execution), private `Movie.Rehydrate` mapping. Prove on real Postgres in IntegrationTests and UnitTests survivor (brief PD1). Command-count proof: scoped `DbCommandInterceptor` on the candidate-load call only (exclude setup/parity repository commands); fresh query `DbContext` with empty `ChangeTracker.Entries()` afterward. Full-state parity vs repository reads on separate contexts (null `MetadataTitle` ⇒ null `Metadata`, empty/nonempty collections, scalars/flags/status/enrichment); retain private null-Id guard without a fictitious persisted-null-PK test. Retain full Includes and private Rehydrate; no projection rewrite (brief PD2). Do not touch private `EfMovieRepository.ToDomain`. Duplication gate finding stops for decision; no mapper/repository rewrite authorized.

4. **Handler orchestration.** Constructor dependencies: `IMovieRepository`, `IRecommendationCandidateSource`, `GenreWeightsCalculator`, `IOptions<RecommendationOptions>`. Selected via `IMovieRepository.GetAsync` → null throws `NotFoundException`. Empty pool short-circuits before genre calculator. Otherwise one `GenreWeightsCalculator.GetAsync`, one `IOptions<RecommendationOptions>.Value.Weights` read, map selected once, score each candidate once with frozen `SimilarityScorer`, rank Score desc then Id.Value asc, take `min(Take, count)`, passthrough Score/Reasons, build `MovieSummary` per brief Q4 field table (null-metadata summaries retain null metadata-derived scalars). Cancellation checkpoints: entry, after each awaited dependency, and per scored candidate. Errors and cancellation propagate; pre-cancelled token performs no dependency I/O.

5. **Sibling consumption.** DEV-336 mapper entry point `MovieFeaturesMapper.From(Movie)`; DEV-337 calculator/cache as registered; DEV-338 `SimilarityScorer.Score` / `SimilarityResult` / `Reason`. No stubs, no second cache, no formula reimplementation.

6. **DI / composition.** Scoped port→adapter in `AddLamuFlixPersistence`; handler+validator in `HandlerRegistration` via `AddHandler`. Composition proves handler, validator, and scoped adapter resolution via existing registrations and `ManifestContracts` contract extension, including scoped calculator and bound options dependencies; route manifest unchanged. Drift recon re-reads the shared `PersistenceServiceCollectionExtensions` body after siblings land (no stale line assumptions / second cache).

7. **Proof order (brief Q5/PD1 Phase B).** (1) Wisp prerequisite/drift recon confirms Epic 7 sequencing and landed DEV-336/337/338 compatibility (re-read shared registration body). (2) Contracts and validator. (3) Candidate adapter + real-Postgres IntegrationTests and UnitTests survivor. (4) Handler + unit/property tests (properties: total order / count / permutation only; valid pools). (5) DI + host composition/manifest. (6) Cog refactor on complete implementation diff. (7) Gauge Phase 3 step 5 verification.

8. **Gates (Phase B).** Brief Q5 Gauge expectations and `AGENTS.md` verification commands apply; do not duplicate full gate/loop prose here. Report native exits accurately; scope-empty SKIPPED is never PASS. Mutation via `scripts/run-mutation.ps1` without `--since` in worktrees; Infrastructure selects UnitTests only (recon-DEV-340 section 11); thresholds/config unchanged. Property-test exit 2 without recorded opt-out blocks. Web gate expected SKIPPED (scope-empty). Surviving mutants require tests. No separate architect/Gherkin stage for this ticket. Loop bar/cap/scope: brief Q6.

## Domain vocabulary (published)

Copied from root `CONTEXT.md` / `spec.md` so plan and spec stay aligned:

| Term | Definition | Avoid |
| --- | --- | --- |
| Recommendation | A suggested library movie from the details page. | search result, related-title guess without score/reasons |
| Reason | A caller-safe explanation attached to a recommendation. | internal identifiers, scorer debug strings rewritten here |

## Loop discipline

Authoritative closing bar, round cap, frozen scope, and exclusions are brief Q6 (and charter / `AGENTS.md` as cited there). Do not diverge from those obligations here; this section is a pointer only, not a second source of truth.

## Complexity Tracking

No constitution violation requires tracking. Patron recorded no constitution departure and no §2.3 escalation (brief Q1–Q6).
