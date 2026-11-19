# Implementation Plan: Ordered six-criterion similarity scorer

**Branch**: `feature/518-spec` | **Date**: 2026-11-19 | **Spec**: `specs/DEV-518/spec.md`

**Input** — [Spec](spec.md), [brief](brief.md), [rulings](CONCLUSIONS.md), [plan adjudication](PLAN_ADJUDICATION.md), [Product](../PRODUCT.md), and the repository constitution.

**Status** — PLAN FROZEN after all three challenge reports. Gate 1 awaits owner merge of the spec PR. Implementation gates and runtime acceptance are pending.

**Artifacts** — Spec, brief, plan, tasks, checklists, evidence runbook, [plan adjudication](PLAN_ADJUDICATION.md), and [ADR 0046](../../docs/adr/0046-shared-recommendation-similarity.md). The design uses the existing schema and HTTP contract.

## Summary

Replace the current multi-term similarity scorer with one ordered six-criterion function (Collection, Director, Themes, Plot, Cast, Genre) that returns score plus per-criterion evidence and contributions under a fixed-weight denominator (15 at defaults), per the normative scoring contract in `spec.md` (authoritative copy of `brief.md` Scoring contract). Add GenreWeights-style library keyword-count caching for Themes rarity. Wire Related, MMR redundancy, and SeedCollectionGrouper to the shared scorer; remove year/runtime/rating/writers and SimilarStoryThreshold. Pin tests/docs, expose six criteria in DEV-545 compare mode, and capture before/after Related (five DEV-545 titles) plus For You snapshots as PR evidence.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Core`, `LamuFlix.Infrastructure`) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing stack only — EF catalog/repository, existing recommendation handlers, DEV-545 evaluation/compare surfaces. No new NuGet/npm dependency (`brief.md` Q6–Q7; `PRODUCT.md` §5).

**Storage**: Existing PostgreSQL movies / movie_keywords population via `IMovieCatalog` / `EfMovieCatalog`. No schema or migration change (Q7).

**Testing**: Existing xUnit suites named in brief/recon; Postgres Testcontainers for catalog keyword counts and invalidation; evaluation fixtures/compare mode for DEV-545 evidence. No new test library. Catalog/repository/cache tests, consumer tests, and DEV-545 compare paths are authorized test surfaces resolved by T001 path recon — they are not blocked merely because their test files sit outside the production source list below.

**Target Platform**: Local API recommendation path; home Postgres/Compose for Related/For You snapshots and Fistful acceptance.

**Project Type**: Existing ports-and-adapters .NET API with feature-organised Core handlers; internal scoring/options change — no `/web` or public API edits.

**Performance Goals** — One immutable snapshot per recommendation operation. Cache lifetime is driven by successful writes and version-token invalidation. Review the representative PostgreSQL grouping plan against existing indexes at implementation.

**Constraints** — Q6–Q9 govern scope and the capped, three-axis review. The denominator is the finite positive sum of all six configured weights. Done-when and merge clearance require verified publication and observed acceptance. The spec PR must merge at Gate 1 before Build.

**Scale/Scope**: Size L; targeted edits to scorer, options, feature mapping, keyword-count infra, three consumers, named tests/docs, and DEV-545 compare/snapshot evidence.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current) as **plan-compliance intent only** — these rows are not executed implementation gate results. The approved scope carries no structural owner checkbox.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **Compliant (plan)** — extend existing `IMovieCatalog` with a keyword-count read; EF adapter in `EfMovieCatalog`; no new port layer (`brief.md` Q7). |
| II (Explicit Handlers) | Handlers + AddHandler | **Compliant (plan)** — existing `GetRecommendationsQueryHandler` / MMR / `SeedCollectionGrouper` consumers only; no MediatR; no new handler type. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **Compliant (plan)** — no Movie mutation; cache invalidation hooks alongside existing genre-weight invalidation after successful commits (and for keyword-membership mutations even if genre invalidation is absent at that site). |
| V (Validated inputs / errors) | ProblemDetails | **Compliant (plan)** — no public API/error-contract change. |
| VI (Observability) | Safe diagnostics | **Compliant (plan)** — treat metadata names as literal data in reasons and compare output and as structured fields when logging. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **Compliant (plan)** — use the existing typed validation and ValidateOnStart chain for the scoring invariants below. Remove SimilarStoryThreshold. |
| VIII (Ubiquitous Language) | CONTEXT.md | **Compliant (plan)** — align CONTEXT.md with the pinned table. [ADR 0046](../../docs/adr/0046-shared-recommendation-similarity.md) records the design. |
| IX (Test Pyramid) | Meaningful layers | **Compliant (plan)** — unit/property for scorer edges; Testcontainers for catalog counts/invalidation; DEV-545/home evidence for runtime acceptance unit tests cannot prove. |
| Tech stack / API docs | OpenAPI chain | **Compliant (plan)** — no public DTO/route/status change; no OpenAPI/TS generation edit. |
| Governance / Static-Analysis Gates | Honest exits | Phase B executable checklist (T019 / `EVIDENCE.md`): Roslyn, cyclomatic complexity (including applicable refactor-gate threshold), InspectCode, property tests (when applicable), vulnerable packages / security checks (when applicable), architecture tests, `dotnet format --verify-no-changes`, `dotnet test`, mutation when production C# under `src/` changes, web SKIPPED (scope-empty) if `/web` untouched; never fold non-pass into PASS; never lower thresholds. Closing bar and 2-round cap per Q8–Q9 (**conjunctive**: every actionable in-scope severity closed **and** cited dispositions for disputed findings; Q6 scope). |

### Complexity Tracking

The keyword-count cache follows the existing GenreWeightsCache pattern under ADR 0046.

## Approach

- The spec PR includes ADR 0046 and the adjudication. Gate 1 requires the owner merge receipt in CONCLUSIONS.md before Build.
- T001 inventories the landed source and records drift without editing artifacts. T001a applies authorized tasks-only amendments when needed.
- **Task order (`brief.md` Files and implementation order / Q5–Q7)**:
  1. Fix five Related seed IDs (DEV-545 titles) and capture DEV-545 Related + For You baselines before scoring edits (no unruled pending-baseline bypass).
  2. Extend MovieFeatures/MovieFeaturesMapper; add catalog keyword-count read, cache/calculator, repository invalidation (including keyword-membership mutations), DI; test counts/invalidation with real persistence; separately prove consumer one-snapshot-per-operation reuse.
  3. Author SimilarityWeights/RecommendationOptions (including SimilarStoryThreshold removal) and ordered SimilarityScorer/SimilarityResult evidence per normative table first; do not require solution build or green tests until step 4 migrates consumers/fixtures.
  4. Wire GetRecommendationsQueryHandler, MmrSelector, SeedCollectionGrouper; remove Related SimilarStoryThreshold caller path; update fixtures; then require buildable solution + green tests.
  5. Update named tests (including create SimilarityCriteriaTableTests if absent), docs/recommendations.md, CONTEXT.md vocabulary; non-overlapping DEV-545 compare edits then deferred/overlapping compare + evaluation fixtures; six-criteria output proof after both; run legacy absence scan after consumer and evaluation migration.
  6. Run static gates + format + architecture/property/security/refactor checks as applicable; stage after/baseline comparisons; verify Fistful observed acceptance; delivery PR may open without attachments; merge clearance needs published attachments and observed acceptance. Runtime pending does not clear Done-when / merge clearance.
- Q1 and Q6 preserve For You profile relevance and Related taste blending outside the weight-default change and leave candidate, UI, schema, and API scope unchanged.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-518/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md              # This file
├── tasks.md
├── EVIDENCE.md          # Runbook / evidence slots (pending until filled)
├── PLAN_ADJUDICATION.md
└── checklists/
    ├── requirements.md
    └── harness.md
```

ADR 0046 records the keyword snapshot and scoring decisions. The schema and HTTP contracts stay as specified.

### Source Code (authorized boundary under Q6–Q7 and the plan challenge ruling)

Production / docs surfaces:

```text
src/LamuFlix.Core/
├── Ports/IMovieCatalog.cs                         # keyword-count read beside GetGenreCountsAsync
├── Domain/GenreWeightsCache.cs                    # precedent only; mirror pattern for keyword cache
├── Domain/<KeywordWeightsCache>.cs                # singleton cache (GenreWeightsCache pattern; exact type name at implement)
├── Options/SimilarityWeights.cs
├── Options/RecommendationOptions.cs
└── Features/Recommendations/
    ├── MovieFeatures.cs
    ├── MovieFeaturesMapper.cs
    ├── SimilarityScorer.cs
    ├── SimilarityResult.cs
    ├── GenreWeightsCalculator.cs                  # precedent for scoped calculator
    ├── <KeywordWeightsCalculator>.cs              # scoped calculator
    ├── GetRecommendationsQueryHandler.cs
    ├── GetForYouRecommendationsQueryHandler.cs    # snapshot plumbing only (Patron ruling in CONCLUSIONS.md)
    ├── MmrSelector.cs
    └── SeedCollectionGrouper.cs

src/LamuFlix.Infrastructure/Persistence/
├── EfMovieCatalog.cs                              # keyword-count query over movies + movie_keywords
├── EfMovieRepository.cs                           # invalidate keyword cache with GenreWeightsCache
├── PersistenceServiceCollectionExtensions.cs      # singleton cache registration
└── MovieCatalogServiceCollectionExtensions.cs     # scoped calculator registration

docs/recommendations.md                            # create; pin criteria table
CONTEXT.md                                         # vocabulary alignment only
```

Authorized test / evidence surfaces (resolved by T001 path recon — not stopped by the production source list):

```text
tests/ (named / as applicable — create/extend homes from T001):
├── LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs
├── LamuFlix.UnitTests/Features/Recommendations/SimilarityCriteriaTableTests.cs  # create if absent
├── LamuFlix.UnitTests/Options/SimilarityWeightsTests.cs
├── LamuFlix.UnitTests/Options/SimilarityWeightsSurvivorTests.cs
├── LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs
├── catalog/repository/cache Testcontainers suites for keyword counts + invalidation (homes from T001)
├── consumer tests covering Related / MMR / SeedCollectionGrouper wiring + one-snapshot-per-operation reuse (homes from T001)
├── LamuFlix.Evaluation/Fixtures/EvaluationFixtureLoader.cs
├── LamuFlix.Evaluation/Fixtures/EvaluationFixtureLoaderTests.cs
├── LamuFlix.Evaluation/Replay/TmdbSignalReplayTests.cs
├── DEV-545 compare/snapshot entry points for six-criteria output and baseline/after capture (paths from T001)
└── related scorer property/golden/threshold suites already under Features/Recommendations/
```

**Structure Decision**: Stay inside existing Core ports/domain/features/options and Infrastructure persistence adapters. Keyword cache mirrors GenreWeightsCache without a new project or layer. Public Api/Endpoints and `/web` remain out of bounds. Any tracked **production** file outside this authorized set needs a cited Patron ruling before edit. Authorized test/compare surfaces listed above (and exact homes T001 records) are in scope for implementation tests even when they sit outside the production tree. Exact undeclared helper type names are implementer choices within the GenreWeights precedent — do not invent alternate architectures. Route any required out-of-bound caller edit through Patron per the brief.

## Scoring validation and keyword snapshots

- Validate the scoring options at startup. The six non-negative weights must sum to a finite positive denominator. ThemesCeiling must be finite and positive. Plot bounds must be finite within [-1,1] and floor < ceiling. Display thresholds must be finite within [0,1], and MinimumKeywordMovies must be at least 1. Explicit scorer calls reject invalid scoring inputs. The stoplist uses OrdinalIgnoreCase with trimmed distinct entries and discards blanks and nulls. Explicit null means empty and an omitted setting retains the default four names.
- Obtain N and distinct n_k from one PostgreSQL statement or a transaction with repeatable-read or stronger snapshot semantics. This guarantees 0 ≤ n_k ≤ N even with concurrent writes. Empty libraries produce zero Themes. Keep up to three counted shared names by descending rarity, including zero-weight names when they qualify. Zero Themes emits no reason.
- The singleton cache serves one shared catalog within one serving process. Use a scoped calculator and version token. T001 enumerates production commit paths affecting N or keyword membership or eligibility, including bypasses. T004 tests and T006 wires invalidation at each applicable successful commit, even without genre invalidation. Failed writes preserve the cached value. A failed or cancelled load leaves the cache empty and a subsequent operation can retry. Check cancellation during stale-version retries and prevent stale repopulation. Resolve any authority or path gaps under Q6–Q7 before implementation.
- The Related and For You handlers resolve one immutable snapshot at their operation boundary and explicitly pass it through collection grouping or MMR, respectively, into every scorer call on that operation. The pure scorer accepts the snapshot as an argument. In-flight operations retain their snapshot through invalidation and subsequent operations reload. For You handler changes are limited to snapshot plumbing under Patron's ruling in CONCLUSIONS.md (d01e60f0).
- Study selection/order changes visible in the existing T020 before/after captures for MMR and collection behavior. Keep tuning fixed and use T014 selection coverage alongside the receipts.

## Test Strategy

| Family | Required evidence |
|---|---|
| Table pin | Normative scoring contract rows: order, six defaults, Collection/Director/Cast/Genre value rules, exact reason texts, Themes formula/defaults, graded thresholds 0.3/0.5/0.5, configured denominator, contribution sum = Score |
| Scorer edges | Missing keywords, genre-only ceiling, OrdinalIgnoreCase stoplist normalization, minimum frequency, rarity evidence including ubiquitous counted names, empty/zero norms, Themes ceiling and half ceiling, Plot boundaries, reason order, zero threshold, equality and below-threshold contribution. Validate invalid scoring options as listed below. |
| Legacy absence | T018 scans recommendation code, options, and tests for YearProximity, RuntimeProximity, RatingBoost, Writers, SimilarStoryThreshold, Similar release years, Similar runtime, Similar rating, Shared writers, and Identical feature profile. Inspect Keyword declarations/references in SimilarityWeights, Recommendations:Weights sections, member access, and weight initializers and fixtures. MovieFeatures.Keywords, MovieKeyword, keyword counts and stoplist configuration remain valid. |
| Keyword infra (lifecycle) | Testcontainers proves empty/distinct/consistent counts during a concurrent commit, each inventoried success-only invalidation site, failed writes, retry after load failure, stale in-flight rejection, and cancellation. |
| Keyword infra (consumer) | Related and For You operations pass the same snapshot identity to all of their pairs, including collection grouping/MMR as applicable. Inject invalidation between pairs and verify refresh on the next operation. |
| Consumers | Related/MMR/SeedCollectionGrouper use shared score; Related reasons from ordered evidence; fixtures supply new feature fields/weights |
| DEV-545 / home | Six-criteria/contributions output proof after compare + evaluation migration; five-title Related before/after staged before merge clearance; For You before/after; Fistful → Good/Bad/Ugly with Same director + Shared themes (observed acceptance); published attachments required for merge clearance, not for opening the PR |
| Gates | Roslyn, complexity (incl. applicable refactor), InspectCode, format, architecture tests, applicable property/security checks, tests, applicable mutation; honest labels; never fold N/A or non-pass into PASS |

## Pickup evidence

T001 resolves exact test and evaluation paths and inventories write coverage. T002 records seed IDs and before-snapshot evidence.

## Post-design Constitution Check

The adjudicated design meets Principles I–IX within the existing architecture. Implementation checks begin after Gate 1.
