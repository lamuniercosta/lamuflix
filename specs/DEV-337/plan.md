# Implementation Plan: DEV-337 GenreWeights IDF calculator

**Branch**: `feature/337-spec` | **Date**: 2026-10-20 | **Spec**: `specs/DEV-337/spec.md`

**Input**: Feature specification from `specs/DEV-337/spec.md`, settled grill brief `specs/DEV-337/brief.md`, facts from notes `recon-DEV-337` and `recon-DEV-337-2`, Patron rulings in `specs/DEV-337/CONCLUSIONS.md`.

**Note**: Planning artifact only (Phase A). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1, and Phase B remains gated on Epic 7 (`specs/PRODUCT.md:17`).

## Summary

Deliver library-wide inverse-document-frequency **GenreWeights** with weighted Jaccard **genre similarity**, backed by a real single-command `IMovieCatalog` genre-counts snapshot, a process-wide generation-safe in-memory cache, repository post-persistence invalidation, and DI registration that keeps one shared cache. Publish the three approved glossary terms already drafted in root `CONTEXT.md`. Focused unit, property, and PostgreSQL integration tests lock the contracts. Nothing else ships.

## Technical Context

**Language/Version**: C# / .NET 10. Core, Infrastructure persistence, and named unit/integration tests. No web change.

**Primary Dependencies**: None added. BCL synchronization only for the cache. Core may use existing allowed references (constitution I). Tests use existing xUnit v3, Shouldly, NSubstitute, FsCheck, and Testcontainers PostgreSQL.

**Storage**: Existing PostgreSQL tables only. One grouped query over existing genre/movie join data. No migration or schema change. DEV-411 already persists genres (recon section 7).

**Testing**: xUnit v3, Shouldly, NSubstitute (unit collaborators), FsCheck (properties), real PostgreSQL Testcontainers for catalog query and write/invalidation proofs. No EF InMemory, no driver mocking, no second assertion/mocking library, no Gherkin/Reqnroll.

**Target Platform**: Existing LamuFlix.Api host. No new host or port.

**Project Type**: Existing ports-and-adapters solution. Extends existing `IMovieCatalog` port; Domain types for shared math/cache; Recommendations feature folder for the scoped calculator. Not a new project or layer.

**Performance Goals**: No numeric latency target in the brief. Conservative extra reloads on scalar saves are accepted (brief Q5).

**Constraints**: TreatWarningsAsErrors. No new NuGet/npm dependency. No new project, top-level folder, or architectural layer. No new cache port/interface, TTL, cache option, or background refresh. No new ADR (existing catalog port extended). No public API route/DTO/status change. No schema/migration. No command-handler edits. No `Features:LocalPlay`, secrets, or `Process.Start`. No MovieFeatures dependency. Thresholds stay in `harness.yml`.

**Scale/Scope**: Exactly the approved file set in brief "Approved file set", including root `CONTEXT.md` glossary publication. No deletion or unrelated rewrite.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-337: IDF GenreWeights, grouped counts on `IMovieCatalog`, cache invalidation on catalog changes, weighted Jaccard (recon-DEV-337-2 §9.1 verbatim ticket). Nothing is added or dropped. No owner checkbox (brief Q1, handoff).
- Core organisation: shared Domain types (`GenreWeights`, `GenreWeightsCache`), Ports (`GenreCounts`, `IMovieCatalog` member), Recommendations feature folder for the scoped calculator. Feature folders MUST NOT reference each other; shared concepts live in Domain/Ports (constitution I; recon-DEV-337-2 §9.3). Recommendations folder is constitution-pre-named, not a new architectural layer (care list item 2; brief Q4).
- Port closed set: extending `IMovieCatalog` does not add a new port; no ADR required (brief Q4; constitution I lines 136–140). Do not introduce `IRecommendationCandidateSource` or any other new port in this ticket.
- Core must not reference EF Core/Npgsql (constitution I). Grouped query stays in `EfMovieCatalog`. Cache uses BCL only.
- No MediatR, generic repository, or UnitOfWork (PRODUCT.md speculative abstractions; constitution II). No handler edits (brief Q5).
- No endpoint, status code, OpenAPI, or generated TypeScript change (constitution API rules; PRODUCT.md care list item 4; recon §6.4). Existing `GET /genres` stays unchanged.
- No schema or migration (care list item 3; brief Q2).
- No secret, `Features:LocalPlay`, or `Process.Start` (care list item 5; recon §6.5).
- Vocabulary: three approved terms published to `CONTEXT.md` in the same PR (constitution VIII / PR Quality Gates; brief Q3). Terms are **Genre weight**, **GenreWeights**, and **Genre similarity** with definitions fixed in `spec.md` Domain vocabulary.
- Tests: xUnit v3, Shouldly, NSubstitute, FsCheck, Testcontainers Postgres (constitution IX; recon-DEV-337-2 §9.5). Gates from configured scripts; never lower thresholds (brief Gate expectations).
- Lifetimes: singleton cache, scoped calculator and catalog (brief Q4–Q5; recon-DEV-337-2 §9.3 lifetime facts). Never capture scoped dependencies in the singleton.

Post-design re-check: the design below adds no package, project, new port, route, schema, or file outside the approved set. The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-337/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes contracts, files, ordering, and proof requirements. There is no new HTTP public contract. `AGENTS.md` is not updated: it is outside the feature directory and outside the named file set. No `ASSUMPTIONS.md` (brief: no taste decisions).

### Source Code (repository root)

```text
src/LamuFlix.Core/Ports/IMovieCatalog.cs
src/LamuFlix.Core/Ports/GenreCounts.cs
src/LamuFlix.Core/Domain/GenreWeights.cs
src/LamuFlix.Core/Domain/GenreWeightsCache.cs
src/LamuFlix.Core/Features/Recommendations/GenreWeightsCalculator.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs
src/LamuFlix.Infrastructure/Persistence/MovieCatalogServiceCollectionExtensions.cs
src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
CONTEXT.md

tests/LamuFlix.UnitTests/Domain/GenreWeightsTests.cs
tests/LamuFlix.UnitTests/Domain/GenreWeightsCacheTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/GenreWeightsCalculatorTests.cs
tests/LamuFlix.IntegrationTests/GenreWeightsIntegrationTests.cs
tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs
tests/LamuFlix.IntegrationTests/MovieCatalogRegistrationTests.cs
tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs
```

**Structure Decision**: Existing Core Domains/Ports/Recommendations and Infrastructure Persistence only, plus the named tests and root glossary. If the contract cannot be implemented inside this set, stop for a decision. Do not widen the set.

## Design Overview

1. **Counts contract.** `sealed record GenreCounts(long TotalMovies, IReadOnlyDictionary<int, long> MoviesWithGenre)` in Core/Ports. `IMovieCatalog.GetGenreCountsAsync(CancellationToken)`. Semantics per brief Q2 / spec FR-001–FR-002. Adapt `RecordingMovieCatalog` only as compiler-forced.

2. **Grouped query.** `EfMovieCatalog.GetGenreCountsAsync` issues one grouped SQL command over existing tables. Prove command count and snapshot coherence on real Postgres (empty library, zero-count genres, movies without genres, shared memberships, total not multiplied by joins). Do not change `GetGenresAsync` facet HTTP behavior.

3. **Immutable math.** `Core/Domain/GenreWeights.cs`: `Calculate(GenreCounts)`, `GetWeight(int)`, `Similarity(IEnumerable<int>, IEnumerable<int>)`. Formula, rejection rules, defaults, set semantics, rare/common controlled comparison, and immutability per brief Q3 / spec FR-004–FR-005. Do not apply `RecommendationOptions.Weights.Genres`.

4. **Cache.** `Core/Domain/GenreWeightsCache.cs`: concrete singleton, BCL synchronization, immutable weights, generation counter only. Surface: `Task<GenreWeights> GetOrLoadAsync(Func<CancellationToken, Task<GenreWeights>> load, CancellationToken ct)` and `void Invalidate()`. Lazy rebuild; serialize loads; atomic generation recheck with publication; discard stale on mid-load invalidation. `Invalidate` completes independently of any in-flight database load and never waits on the async load gate; brief synchronous publication synchronization is permitted (no lock-free mandate). Only the current gate holder invokes its own scoped loader; gate wait, load, and generation-mismatch retries use that caller token; holder cancellation releases the gate without cancelling independent waiters. Never publish incomplete state; never retain the loader or scoped catalog/DbContext after the call; never capture scoped services. No new cache interface, shared background task, package, or layer.

5. **Calculator.** `Core/Features/Recommendations/GenreWeightsCalculator.cs`: scoped, `Task<GenreWeights> GetAsync(CancellationToken)`, depends on scoped `IMovieCatalog` and singleton cache. Passes a caller-owned loader and token into `GetOrLoadAsync`; does not retain scoped catalog/DbContext beyond the call.

6. **Invalidation.** Inject cache into `EfMovieRepository`. Invalidate at successful completion of the public `SaveChangesAsync` (covering every internal path) and after `TryDeleteAsync` when `rows == 1`, before return. Failed/cancelled saves exit before invalidation. No pre-save or false-delete invalidation. No handler edits. Conservative invalidation on scalar saves accepted; warm-cache scalar-only saves with no genre change still force a reload on the next read even when computed weights are equal.

7. **DI.** `TryAddSingleton<GenreWeightsCache>()` in both `AddLamuFlixPersistence` and `AddMovieCatalog`; scoped calculator in `AddMovieCatalog`. Optional `GenreWeightsCache? cache = null` with `cache ?? new GenreWeightsCache()` for direct construction only; DI always supplies the shared singleton. No mutable static fallback. No production rename of the existing persistence extension.

8. **Glossary.** Keep the three approved terms in root `CONTEXT.md` with the Domain vocabulary definitions from `spec.md`.

9. **Proof order (brief Q6).** (1) Counts contract and adapter with Postgres instrumentation. (2) Pure GenreWeights unit/property tests. (3) Cache, calculator, and DI registration tests with TaskCompletionSource (no sleeps). (4) Post-persistence integration for warm-cache import/membership/delete and failure retention.

10. **Gates (Phase B).** Gauge runs configured scripts from the Build workspace after Epic 7 and Gate 1 prerequisites. Report native exits accurately; scope-empty SKIPPED is never PASS. Mutation via `scripts/run-mutation.ps1` without `--since` in worktrees. Surviving mutants require tests. No separate architect/Gherkin stage.

## Domain vocabulary (published)

Copied from root `CONTEXT.md` / `spec.md` so plan and spec stay aligned:

| Term | Definition | Avoid |
| --- | --- | --- |
| Genre weight | The library rarity multiplier for a genre; rarer genres carry more weight in genre similarity. | genre preference, genre popularity score |
| GenreWeights | An immutable table of genre weights for one library catalog snapshot. | user taste weights, recommendation option weights |
| Genre similarity | The overlap of two movies genre sets, weighted by library rarity. | unweighted genre overlap, overall recommendation score |

## Loop discipline

- **Closing bar:** Evidence-backed Critical, High, and Medium findings within frozen scope block. Low is recorded nonblocking. All three axes (Sentry Risk, Ledger Standards, Compass Spec) are mandatory for size:M; a missing axis blocks adjudication. Unnecessary abstractions and out-of-scope concerns are follow-ups, not fix commits. Apply charter follow-up recording: file only when criteria justify a ticket; otherwise `noted, no ticket`.
- **Round cap:** At most two review rounds and two fix commits per round. Unresolved blockers at the cap go to user review on the PR; no third round or waived gate. Findings and summary must be posted on the PR.
- **Frozen scope:** Real grouped library counts via the existing catalog; immutable IDF GenreWeights and weighted Jaccard; process-wide generation-safe lazy cache; repository post-persistence invalidation; DI and required test-double compatibility; approved tests and glossary. Anything else is a follow-up issue, not a finding in this round.
- **Exclusions:** Recommendation routes, ranking, MMR, candidate-source port, MovieFeatures integration, option tuning, UI/OpenAPI/generated clients, new port/package/project/layer/schema, direct-SQL or distributed invalidation, TTL/background refresh, handler/playback edits, and unrelated cleanup. No secrets, Features:LocalPlay, or Process.Start changes.

## Complexity Tracking

No constitution violation requires tracking. Patron recorded no constitution departure and no §2.3 escalation (brief Q1–Q7).
