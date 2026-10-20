# DEV-340 — GetRecommendationsQuery and Phase A candidate provider

## Status and authority

Grill closed at Q6/12 on 2026-10-20: six ACCEPT rulings, zero open owner checkboxes, zero ADRs, no glossary edit. This is the decision brief for Quill, not a spec, plan freeze, Gate 1 receipt, implementation authorization, or gate result.

- Worktree: F:/Dev/LamuFlix.worktrees/feature-340-spec
- Branch: feature/340-spec
- Pinned HEAD: 87b28a934a56d1a1822a2d7133e07f3d53a03e6b
- Ticket: DEV-340, parent DEV-288, size:M, ui:none.
- Ticket source: DEV-340 note lines 25-34, copied verbatim into recon-DEV-340-2:129-138.
- Current contract facts: recon-DEV-340-3 and recon-DEV-340-3-2, with provenance and path:line evidence.
- Decision receipts: append-only [CONCLUSIONS.md](CONCLUSIONS.md), Q1-Q6, including complete questions, answers, cited rulings, rationale and implications.
- Taste receipt: [ASSUMPTIONS.md](ASSUMPTIONS.md), Q4.

Earlier recon requests recorded in CONCLUSIONS.md are historical and answered by recon-DEV-340-2:116-151 and both Q3 recon parts. No fact search beyond the named evidence was used to close the grill.

## Ticket scope, preserved verbatim

1. Query: GetRecommendationsQuery(MovieId MovieId, int Take = 10).
2. Load candidate movies via IRecommendationCandidateSource (Phase A: all enriched movies in library except selected).
3. Score candidates using SimilarityScorer.
4. Rank by score descending and take top N.
5. Map to RecommendedMovie(MovieSummary Summary, double Score, ImmutableList<Reason> Reasons).

Acceptance: Returns top-N ranked recommendations with attached reason chips.

The ticket is authoritative under specs/PRODUCT.md section 3. No delivery addition, subtraction, or constitution departure was approved.

## Every grill answer and its plan implications

### Q1 ACCEPT — Exact delivery boundary

Deliver the handler and real Phase A candidate port/persistence adapter, with no HTTP route or web UI. Do not invent a substitute scorer or competing result contract. Missing prerequisite ownership/contracts required recon before selecting orchestration and files.

Basis: DEV-340:14 and later verbatim DEV-340:27-34; PRODUCT authority/no-speculative-abstractions rules; constitution closed port set. Q1's request for ticket/sibling evidence is now answered. Its prohibition on duplicate RecommendedMovie contracts does not exclude the ticket-owned type confirmed in Q2.

### Q2 ACCEPT — Ownership and prerequisite sequencing

DEV-340 creates:

- sealed record GetRecommendationsQuery(MovieId MovieId, int Take = 10).
- sealed record RecommendedMovie(MovieSummary Summary, double Score, ImmutableList<Reason> Reasons).
- GetRecommendationsQueryHandler and its validator.
- IRecommendationCandidateSource and its Phase A EF adapter.

Consume the frozen sibling implementations without duplicating, stubbing, or replacing them:

| Owner | Contract consumed |
| --- | --- |
| DEV-336 | MovieFeatures; public static MovieFeaturesMapper.From(Movie movie), sole mapping entry point |
| DEV-337 | GenreWeights; scoped GenreWeightsCalculator.GetAsync(CancellationToken); shared GenreWeightsCache |
| DEV-338 | SimilarityScorer.Score(MovieFeatures a, MovieFeatures b, SimilarityWeights weights, GenreWeights genreWeights) returning SimilarityResult(double Score, ImmutableList<Reason> Reasons); Reason |

At the pinned HEAD these are spec-only, with zero implementation matches in recon. Phase A may specify against the frozen contracts. Phase B must wait for Epic 7 completion and DEV-336/337/338 implementations landed on main, then perform drift recon and verify compatibility. Do not manufacture placeholder copies to make this ticket compile early.

Basis: recon-DEV-340-2:34-52,101-106,129-150; recon-DEV-340-3:106-123; recon-DEV-340-3-2:3-12; PRODUCT sequencing.

### Q3 ACCEPT — Selected lookup and candidate-source boundary

Core/Ports/IRecommendationCandidateSource.cs exposes:

    Task<IReadOnlyList<Movie>> GetCandidatesAsync(MovieId selectedMovieId, CancellationToken ct);

Use existing Core domain Movie rather than introducing a candidate DTO or another port. The existing whitelist already permits this interface.

The handler loads the selected movie with IMovieRepository.GetAsync(query.MovieId, ct). Null throws the existing parameterless NotFoundException. Use MovieFeaturesMapper.From(Movie) directly for selected and candidate movies.

Scoped EfRecommendationCandidateSource in Infrastructure/Persistence:

- Queries db.Movies.AsNoTracking(), filtering Status == EnrichmentStatus.Enriched and Id != selectedMovieId.
- Loads Actors, Directors, Genres and Writers together with scalar metadata and flags.
- Executes exactly one SQL command and materializes the pool once; no pagination, pool cap, N+1 repository calls, or per-candidate database lookup.
- Privately maps records to domain movies using existing Movie.Rehydrate semantics, preserving metadata, collections and flags.
- Does not expose/call private EfMovieRepository.ToDomain or rewrite EfMovieRepository.

The full pool has a deliberate memory cost in Phase A. Vector/MMR retrieval belongs elsewhere.

Basis: recon-DEV-340-3:20-35,43-47,55-86,110-118; recon-DEV-340-3-2:14-18,28; Patron Q3 cited ruling.

### Q4 ACCEPT — Orchestration, validation, ranking and mapping

Handler contract:

    IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>

Use a FluentValidation query validator with Take > 0, no invented maximum. The query default is exactly 10. Use query.Take directly; RecommendationOptions.Take does not override it.

Cancellation and lookup:

- A pre-cancelled token stops work.
- Pass the caller token to selected lookup, candidate source and genre calculator; check cancellation during scoring.
- Missing selected movie throws NotFoundException, following existing handler behavior.
- An existing selected movie is accepted in any enrichment state; the frozen mapper handles absent metadata.
- Propagate cancellation and dependency errors; do not turn failures into successful empty results.

Computation:

1. Load the selected movie, then the complete candidate pool.
2. Empty pool returns ImmutableList<RecommendedMovie>.Empty before calling GenreWeightsCalculator.
3. Otherwise call GenreWeightsCalculator.GetAsync once and read IOptions<RecommendationOptions>.Value.Weights once.
4. Map selected features once, then map/score each candidate exactly once using the frozen scorer.
5. Rank by Score descending, then candidate.Id.Value ascending for ties.
6. Return min(query.Take, candidate count). Keep zero-score candidates; no positive-score cutoff.
7. Pass each SimilarityResult.Score and Reasons unchanged into RecommendedMovie.

The tie-breaker is a Patron taste ruling tagged [assumed] in ASSUMPTIONS.md. It makes membership and order deterministic under candidate permutations without changing score ordering.

Construct MovieSummary with exactly these existing fields:

| Field | Candidate source |
| --- | --- |
| Id | candidate.Id |
| Title | candidate.Title |
| PosterUrl | candidate.Metadata?.PosterUrl |
| ReleaseYear | candidate.Metadata?.ReleaseYear?.Value |
| Runtime | candidate.Metadata?.Runtime?.Minutes |
| ImdbRating | candidate.Metadata?.ImdbRating?.Value |
| EnrichmentStatus | candidate.Status |
| IsInWatchlist | candidate.IsInWatchlist |
| IsWatched | candidate.IsWatched |
| IsNotInterested | candidate.IsNotInterested |

No ParsedReleaseYear fallback, reason rewriting, new thresholds, watched/not-interested exclusion, or candidate cap. Do not retest/reimplement the sibling scoring formula.

Basis: DEV-340:27-34; recon-DEV-340-3:43-47,58-71,85,94,110-118; recon-DEV-340-3-2:7-8,16; Patron Q4 cited ruling.

### Q5 ACCEPT — Files, test strategy, gates and task order

Approved new production files:

- src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQuery.cs
- src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryValidator.cs
- src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs
- src/LamuFlix.Core/Features/Recommendations/RecommendedMovie.cs
- src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs
- src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs

Use the existing LamuFlix.Core.Features.Recommendations namespace for the feature files, Core.Ports for the port and the existing persistence namespace for the adapter. Queries/results are sealed records; handler and adapter are sealed classes. No new project, architectural layer, service wrapper, mapper abstraction or package.

Approved additions to existing files:

- src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs: scoped IRecommendationCandidateSource -> EfRecommendationCandidateSource registration in AddLamuFlixPersistence.
- src/LamuFlix.Api/HandlerRegistration.cs: handler and validator registration using the existing AddHandler pipeline and validator pattern.
- tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs: handler manifest/composition coverage; leave the route manifest unchanged.

Consume DEV-337's calculator/cache registrations as landed; do not create a second cache or alter that sibling implementation.

Approved new test files:

- tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryValidatorTests.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs
- tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs
- tests/LamuFlix.UnitTests/Persistence/EfRecommendationCandidateSourceSurvivorTests.cs

Test strategy:

- Match existing xUnit, Shouldly, NSubstitute, AutoFixture and FsCheck conventions.
- Use actual MovieFeaturesMapper, SimilarityScorer, GenreWeightsCalculator and GenreWeightsCache. Substitute only existing I/O ports: IMovieRepository, IRecommendationCandidateSource and IMovieCatalog; supply calculator genre counts through the catalog.
- Cover Take > 0 validation, zero/negative Take, default 10 versus options.Take, missing selected, existing unenriched selected, empty/fewer/exact/more-than-N candidates, ties and shuffled input, zero-score retention, all ten summary fields, exact Score/Reasons passthrough, cancellation and errors.
- FsCheck covers total ordering (Score descending then Id.Value ascending), count bounds and permutation invariance with real collaborators and valid candidate-source pools excluding selected by construction; selected-id exclusion is proved in both real-Postgres adapter files, not as a handler property. It does not recreate scorer internals.
- Real PostgreSQL Testcontainers via PostgresFixture/PostgresCollection proves filtering across every enrichment state, selected-id exclusion, all four collections/scalars/flags rehydration, no tracking, single-query/no-N+1 execution, no cap and cancellation. Never mock the EF driver for translation/serialization behavior.
- Composition proves handler/validator/adapter resolution through established DI and the new handler manifest contract.

Phase B task order:

1. Wisp prerequisite/drift recon confirms Epic 7 sequencing, landed DEV-336/337/338 and compatibility of this brief's consumed signatures.
2. Contracts and validator.
3. Candidate adapter and real-Postgres integration plus UnitTests survivor tests.
4. Handler and focused unit/property tests.
5. DI additions and host composition/manifest checks.
6. Cog refactor on the complete implementation diff.
7. Gauge Phase 3 step 5 verification on the resulting head.

Gauge expectations: Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable-package scan, dotnet format --verify-no-changes, full dotnet test, and scripts/run-mutation.ps1 without since on changed production projects, using harness.yml thresholds. Never lower thresholds. Web gate is expected SKIPPED (scope-empty). The script's actual verdict controls: scope-empty SKIPPED, configured SKIP and mutation N/A are non-blocking but never PASS; failure/Could not run blocks; property-test exit 2 without recorded valid opt-out blocks. Property tests are planned here, not opted out.

No implementation, tests, gates or commits run in this Phase A grill.

Deliberate charter section 2.3 care-list rulings: no dependency additions/swaps (1); no new projects/layers or whitelist edit (2); no schema/migrations (3); no routes/OpenAPI/web shape (4); no local playback, secrets or Process.Start (5); only the named additions/forced manifest edit, no unrelated deletion or rewrite (6). No owner checkbox is needed.

Basis: recon-DEV-340-3:91-105; recon-DEV-340-3-2:7-18; earlier recon cited by Patron Q5; charter and task-pipeline gate rules.

### Q6 ACCEPT — Shared understanding and loop discipline

Closing bar for later code-review/ship-review:

- Zero unresolved in-scope Critical/High findings.
- Zero ticket acceptance/contract violations; they cannot be demoted to bypass acceptance.
- Every applicable required gate retains independent blocking force.
- Medium/Low findings are explicitly adjudicated within scope, not silently treated as blockers or used to justify unrelated fixes.
- Out-of-scope recording follows charter line 38: Patron/Rigger follow-up only for Critical/High, broken behavior or user-requested work; otherwise record noted, no ticket in CONCLUSIONS.md. Fold into an existing ticket before filing where applicable.

Frozen scope: ticket-owned query/result/handler/validator; full enriched-except-selected Phase A candidate port/EF adapter; frozen sibling consumption; rank/take and unmodified reasons; named DI/manifest additions and focused tests. Exclude HTTP endpoints, OpenAPI/generated clients/web, MMR, embeddings/vector caps, new eligibility filters/thresholds, sibling implementation changes, schema/migrations, dependency/project/layer additions, unrelated rewrites, and playback/security changes; anything else is a follow-up issue, not a finding in this round.

Round cap: two rounds each for analyze/fix, plan challenge, and later code review/remediation. Report unresolved above-bar or process blockers at the cap; do not run a third round or waive them.

No new glossary term: CONTEXT.md already defines Recommendation and Reason. No ADR: the port is already in the constitution and architecture whitelist; this is an M ticket without a new architectural decision requiring an ADR.

Basis: Patron Q6 cited ruling; charter sections 2.3 and halt conditions; PRODUCT quality, sequencing and authority.

## Handoff boundary

Keel stops after this brief. Quill next drafts spec.md, plan.md and tasks.md from this brief and named recon, reporting needs decision for any gap. Subsequent steps are read-only speckit-analyze, bounded fixes, plan challenge/freeze, Patron provisional Gate 1 and a specs-only PR through Rigger. Grill closure does not establish any of those later states, and Phase B remains gated on the prerequisites above.

## PD1 — Patron accepted Ledger F5 scope extension (2026-10-20)

Authority: DEV-340 task note:17 records Patron ACCEPT; findings-DEV-340-Ledger Addendum 1:55-79 supplies the cited basis. This updates Q5 from four to five new test files, retaining tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs and adding tests/LamuFlix.UnitTests/Persistence/EfRecommendationCandidateSourceSurvivorTests.cs using real PostgreSQL through PostgresFixture. No ticket change or owner checkbox; all other scope and gate terms remain.

Basis: recon-DEV-340:99,116-118,124-144 confirms Infrastructure mutation selects only UnitTests and excludes IntegrationTests by name; Infrastructure is not excluded. Recon-DEV-340-3:104 names existing real-Postgres survivor exemplars. The new file must carry mutation-killing assertions for all four enrichment states, selected-id exclusion, all four collections/scalars/flags, null metadata, repository mapping parity, and command count. Keep the IntegrationTests proof. Quill must align spec US1/FR-013, plan file list/testing and tasks before this round can close. Never alter mutation configuration or thresholds to compensate. This is a Phase A decision record, not a plan-freeze or Gate 1 receipt.

## PD2 — Patron accepted proof allocation and measurement (2026-10-20)

Authority: Patron ruling in .maestri-temp/pd2-ruling.txt; complete exchange appended to CONCLUSIONS.md. Ledger F2 / Sentry M3: the adapter owns selected-id exclusion. Both Postgres test files prove it; handler properties use valid pools and assert total order, count and permutation only. Do not add a defensive handler filter. Ledger F1: one query means exactly one SQL command. Count commands using DbCommandInterceptor scoped only to candidate load, excluding setup/parity repository calls; use a fresh query DbContext and assert ChangeTracker.Entries() is empty afterward. Retain Q3 full-pool AsNoTracking + Includes + private Rehydrate; accepted memory/cartesian cost does not authorize a projection rewrite. Named recon establishes an exemplar, not a performance guarantee (recon-DEV-340-3:32-34,78-86,101-104). No ticket change, owner checkbox or constitution departure.

Round-1 drafting clarifications of existing Q3-Q5 obligations: thread ct into EF execution; handler checks at entry, after each awaited dependency and per scored candidate. Prove pre-cancelled no-I/O and cancellation after scoring has begun without mocking frozen collaborators or sleep/timing races. Rehydrate parity covers metadata null iff MetadataTitle is null, all collections (including empty), scalars, flags/status/enrichment fields against repository reads on separate contexts; keep the private null-Id guard, without inventing an impossible persisted null primary key. Handler null-metadata summaries retain null metadata-derived scalars. Use real Options.Create, fresh per-test cache and catalog genre counts. DI proof names ManifestContracts, handler/validator and scoped adapter resolution with calculator/options dependencies; drift recon re-reads shared registration body. A duplication gate finding stops for decision; no existing repository rewrite, new mapper or threshold workaround is authorized. These clarify current obligations rather than add delivery scope.
