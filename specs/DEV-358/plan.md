# Implementation Plan: TMDb Keywords, Full Cast, and Collection Signals in MovieFeatures

**Branch**: `feature/358-spec` | **Date**: 2026-11-01 | **Spec**: `specs/DEV-358/spec.md`
**Input**: Controlling `brief.md` (Q1–Q12 + Plan Challenge Round 1 P1–P12); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-358`, `recon-DEV-358`, `recon-DEV-358-2`, `adjudication-DEV-358-plan`; `specs/PRODUCT.md`; `.specify/memory/constitution.md`
**Status**: Draft — OWNER DECISION PENDING (Q6). Plan Challenge Round 1 repairs R1–R10 applied. Gate 1 closed. Not PLAN FROZEN; no build pickup or gate/metric PASS claimed.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`, plus `CONTEXT.md` glossary entries (C2/R3). **I1 formal folding authorization** (Patron Analyze Round 1 repair rulings in `CONCLUSIONS.md` / `brief.md`): fold `research.md`, `data-model.md`, `quickstart.md`, and `contracts/tmdb-signals.md` into `plan.md` and `spec.md` per DEV-331/DEV-424 house precedent; those four standalone paths are omitted from the Q11 manifest (replaced by this plan's Assumed external contracts / Data model / Internal signal contract / Quickstart sections and by `DRAFTING_RECEIPT.md`). Assumed TMDb signal shapes and evaluation plumbing live in `ASSUMPTIONS.md` and the folded sections below. Tracked acceptance receipt path is `specs/DEV-358/comparison.md` (R6; future Phase B output).

## Summary

When TMDb is enabled, request keyword and credits signals through the landed `TmdbMetadataProvider` in one logical invocation (identity → details/collection → keywords/credits), merge them onto OMDb Found results without overwriting base metadata (overall Found/`MarkEnriched` survives typed supplemental failure), persist keywords/`movie_keywords`/`movie_tmdb_cast` atomically under tracked-load and deterministic locks with per-signal availability semantics that supersede DEV-424 clear-on-OMDb-success for unavailable/disabled collection, expose defensive `MovieFeatures` keyword/collection terms with configurable weights, guarantee seed franchise contiguous top-10 grouping via pure `SeedCollectionGrouper` (embedding-less eligible locals allowed), and prove Hit Rate@10 non-regression with paired DEV-356 offline evaluation receipts written to tracked `specs/DEV-358/comparison.md`. Incremental enrichment only — no automatic backfill. No new dependency, project, port, layer, public API, or frontend change.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).
**Primary Dependencies**: Existing pins only — HttpClient + resilience on landed TMDb adapter, EF Core, evaluation harness under `tests/LamuFlix.Evaluation*`. **No new NuGet/npm packages** (Q4; §2.3 item 1).
**Storage**: PostgreSQL via existing EF Core. New ticket-authorized tables: `keywords`, `movie_keywords`, `movie_tmdb_cast` (+ single EF-scaffolded `AddTmdbRecommendationSignals` migration family). Collection membership on recommended Q6 branch: reuse landed `collections` + nullable `movies.collection_id` (DEV-424). Join-table transition not planned until owner selects it and brief/plan are amended.
**Testing**: xUnit v3, Shouldly, NSubstitute, AutoFixture, Theory/MemberData, FsCheck for independent invariants, WireMock offline HTTP in IntegrationTests only for TMDb signal HTTP cases, Testcontainers PostgreSQL for translation/serialization/transactions/concurrency. Never mock the data-access driver. `TimeProvider` + injected seeded `Random`. No live TMDb in CI/replay.
**Target Platform**: .NET ASP.NET Core personal library backend; evaluation CLI via `scripts/run-evaluation.ps1` → `EvaluationEntry`.
**Project Type**: Ports and adapters with feature-organised Core — no new project or layer (Q4; constitution Principle I).
**Performance Goals**: Indexed same-collection candidate union (no whole-catalog scan); match existing recommendation latency class; no new SLA invented; large-cast correctness smoke only (no cardinality/performance gate).
**Constraints**: No explanatory comments; `CancellationToken` end to end; complexity ≤ harness thresholds; never hardcode/lower gate thresholds; no public DTO/OpenAPI/TS edits; no generic `MmrSelector` redesign; Q6 checkbox unresolved → Gate 1 closed; Phase B revalidates upstream contracts at build HEAD (Q1); README/CHANGELOG excluded.
**Scale/Scope**: Personal library; Size M; exact Q11 file/test/task/gate boundary as amended by R3/R5/R6/R7.

## Constitution Check

Gates checked against `.specify/memory/constitution.md`. **No constitution departure claimed.** Q6 is a ticket-design owner checkbox under role 2.3(a), not a constitution amendment. Care-item-3 schema additions (`keywords`/`movie_keywords`/`movie_tmdb_cast`) are deliberate Patron approvals within ticket-authorized tables (Q8). `movie_tmdb_cast` and `TmdbSignalReplayTests` names retained as adapter-source/ablation provenance outside provider-neutral Core (R10).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Ports in Core; adapters in Infrastructure; feature-organised Core | Extend landed `TmdbMetadataProvider` / `ChainedMetadataProvider` and existing candidate-source contract; immutable domain data only — **no new Core port or layer** (Q4, Q8, Q11). |
| II (Explicit Handlers) | Sealed handlers; existing decorator chain | Extend `GetRecommendationsQueryHandler` and existing enrichment/save path; no new MediatR abstraction (Q8–Q9). |
| III (Filters) | Library filters via `MovieQuery` | Untouched; no browse/public filter change (out of scope). |
| IV (Enrichment) | Explicit state machine; atomic claim before provider; Core decisions / thin consumer; no raw exception storage | Preserve `Pending`/`Enriched`/`NotFound`/`Failed` transitions via intent methods; atomic `TryClaimForEnrichmentAsync` before any provider call; Core handlers own Found/`MarkEnriched`/`MarkNotFound`/`MarkFailed` decisions; thin consumer ack/nack/republish only; cancellation propagates; raw exception text never persisted/logged as signal success. After primary OMDb Found, supplemental NotFound or typed Failed (`RateLimited`/`ProviderUnavailable`/`InvalidResponse`) keeps overall Found/`MarkEnriched` with Unavailable affected signals and existing adapter retry/health accounting (R4; Enrichment Reliability Rules). Per-signal Available/KnownEmpty/Unavailable/Disabled write table applies inside successful enrichment saves (R1). |
| V (Validated Inputs) | ProblemDetails / validated inputs | No new public HTTP inputs; evaluation CLI strict Disabled\|Enabled parsing via `EvaluationEntry` (Q10; R7). |
| VII (Configuration Isolation) | Validated options; no hardcoded secrets | Reuse `TmdbOptions` / existing secret config; no new credential surface; zero raw `api_key` URI logging (Q4, Q7; R5). |
| VIII (Ubiquitous Language) | Feature glossary; functionality-based Core names; new terms in `CONTEXT.md` same PR | Core domain types/members use functionality-based names `MovieKeyword`, `MovieSignalSnapshot`, `FullCast` (R3 amends C1 `ExtendedCast`; constitution :402-407). Nested availability enum in `MovieSignalSnapshot.cs`. Six domain terms added to `CONTEXT.md` in package 2 (C2/R3). Vendor `Tmdb*` restricted to Infrastructure adapters and `TmdbOptions`. No new ADR. |
| IX (Test Pyramid) | Testcontainers for EF; no mocked driver; HTTP suites in IntegrationTests | FR-014 / Q11 / R5: WireMock TMDb signal cases live in `IntegrationTests/MetadataProviderTmdbSignalTests.cs`; pure chain unit tests remain without duplicate HTTP suite. |
| API and Contract Rules | C# DTO → OpenAPI → generated TS | **No public API shape change** this ticket (Q4, Q11). |
| Gates | Roslyn, complexity, InspectCode, format, vulnerable packages, mutation | Phase B/implement stages only; see Gates below. |

### Complexity Tracking

Empty by design — no constitution violation to justify.

## Approach

Follow brief Q11 eight-step task order with **strict sequential package ordering (I2)**: later packages MUST NOT start before their predecessor package checkpoint completes; parallelism is permitted only among independent tasks within the same active package. Planned path uses recommended Q6 FK branch conditionally.

1. **Build-entry gate (Q1, Q6, Q11 step 1)**: At Phase B, bounded recon revalidates DEV-331/DEV-411/DEV-356 contracts at build HEAD. Resolve Q6 owner checkbox before coding. Spec drafting may precede the owner answer; build entry may not. **Checkpoint 1** before package 2.
2. **Domain + MovieFeatures + CONTEXT.md glossary (Q7–Q8, Q11 step 2; C1/C2/R3)**: Add six canonical `CONTEXT.md` glossary entries; add immutable `MovieKeyword` / `MovieSignalSnapshot` with nested `Unavailable`/`KnownEmpty`/`Available`; `FullCast` as `ImmutableHashSet<string>`; keyword snapshots as `ImmutableArray<MovieKeyword>`; feature keywords as `ImmutableHashSet<int>`; extend `MovieMetadata` with optional Keywords/`FullCast` snapshots and sole collection identity (`CollectionId`/`CollectionName`) plus snapshot collection availability only — no `MovieCastEntry`; extend `MovieFeatures`/`MovieFeaturesMapper`/`SimilarityWeights` (Keyword=1.0, Collection=2.0 defaults [assumed]); defensive equality/hash/dirty-tracking/invariant tests. **Checkpoint 2** before package 3.
3. **Adapter + chain supplementation (Q7–Q8, Q11 step 3; after package 2; R4/R5)**: Add `TmdbMetadataProvider.Signals.cs` (+ mapping/log edits) for one logical invocation: resolve identity (IMDb or title/year policy) → details (collection) → keywords/credits over existing client; extend `ChainedMetadataProvider` for enabled-only same-movie supplemental merge after OMDb Found with typed outcome matrix (NotFound/Failed keep Found/`MarkEnriched`); preserve primary/fallback matrix when no primary Found; never duplicate TMDb fallback Found lookup; offline WireMock cases in `IntegrationTests/MetadataProviderTmdbSignalTests.cs` covering independent parse/mixed states/large-cast smoke/zero `api_key` URI logging; pure chain unit tests without duplicate HTTP suite. **Checkpoint 3** before package 4.
4. **Atomic persistence + migration (Q8, Q11 step 4; after package 3; R1/R2)**: `KeywordRecord`/`KeywordConfiguration`; `movie_keywords` + `movie_tmdb_cast`; EF-scaffold single `AddTmdbRecommendationSignals` migration family (never hand-edit generated Designer/snapshot); extend `EfMovieRepository` with tracked loading of keyword joins, full-cast joins, and related identities before replacement; deterministic locks for keyword IDs in a distinct stable namespace and both base/`FullCast` actor natural keys using existing upsert discipline; per-signal Available/KnownEmpty/Unavailable/Disabled write table; supersede DEV-424 clear-on-OMDb-success for unavailable/disabled collection while preserving KnownEmpty clears; Testcontainers roundtrip/reload-save/no-wipe/concurrent same-keyword/mixed-state/rollback plus amended collection clear-on-OMDb proof. Collection FK reuse only on owner-approved branch. **Checkpoint 4** before package 5.
5. **Rehydration, scoring, seed grouping (Q8–Q9, Q11 step 5; after package 4; R9)**: Extend catalog/candidate/profile rehydration; `SimilarityScorer` keyword/collection terms; extend `IRecommendationCandidateSource` / `EfRecommendationCandidateSource` indexed same-collection union allowing otherwise eligible locals without persisted embeddings (rehydrate base + signals; existing missing-plot fallback; no embedding writer/global-profile change); pure Core `SeedCollectionGrouper` (no fourth injected port) + handler wiring; unit/property/API tests for no-op/order/capacity/eligibility/leading-block taste cost; leave generic `MmrSelector` and public DTOs untouched; document incremental enrichment / no automatic backfill. **Checkpoint 5** before package 6.
6. **Paired evaluation plumbing (Q10–Q11 step 6; after package 5; R7/R8)**: Extend existing `tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs` (strict child mode → pure `EvaluationReplayOptions.Default`); extend fixture loader/catalog movie/options/replay; add `baseline.tmdb.json` + `baseline.tmdb.sources.json` with explicit keyword=1/collection=2 weights retaining original ranking fields; `-TmdbSignals` on `scripts/run-evaluation.ps1` with child `LAMUFLIX_EVALUATION_TMDB_SIGNALS`; tests for disabled baseline, independent exercised coverage, leakage, CLI isolation. **Checkpoint 6** before package 7.
7. **Measurement receipt (Q10, Q11 step 7; after package 6; R6)**: Execute both pinned publish commands; write ignored `artifacts/evaluation/DEV-358/{baseline.json,tmdb.json}` (never force-add) and tracked `specs/DEV-358/comparison.md` with exact commands, SHA-256 hashes, seed/mode/cohort/denominator/hit counts/HR@10/MRR/deltas/lambda/weights/independent coverage and scrubbed provenance; state that profile replay does not prove seed grouping (T023/SC-003 remain mandatory); missing provenance/coverage or HitCount regression → measurement incomplete / Package 8 locked. Receipt is Phase B output — not created with invented metrics now. **Checkpoint 7** before package 8.
8. **Gates & reviews (Q11 step 8, Q12; after package 7)**: Build-stage only — harness build/`dotnet test`, Roslyn, complexity, InspectCode, property tests, vulnerable packages, format, pre-PR `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since`; three-axis reviews with two-round caps. Spec workspace runs none of these.

### Assumed external contracts (research folded)

| Decision | Rationale | Alternatives rejected |
|---|---|---|
| Keywords/credits endpoints + DTO shapes per ASSUMPTIONS.md | External schemas not in tree; Conductor authorized Patron [assumed] (Q7) | Further external recon stop (superseded); inventing SDK |
| One logical invocation: identity → details → keywords/credits | Collection from details; no duplicate fallback Found lookup (R4; recon-DEV-358-2:62-82) | Separate orphaned keyword/credits calls; duplicate identity lookup |
| Supplemental merge after OMDb Found; typed failure keeps Found | Chain has no field merge; signals otherwise missed on OMDb success (Q8; R4) | Overwriting base Actors; failing overall enrichment on supplemental NotFound |
| `keywords`/`movie_keywords`/`movie_tmdb_cast` | Ticket DEV-411 pattern + Care-item-3 (Q8; R10 provenance name) | Second actor table; collection join-table without owner answer |
| Recommended collection FK reuse + availability-gated clear supersession | Landed DEV-424 path; R1 supersedes clear-on-OMDb for unavailable/disabled only | Silent omit of ticket join table; nullable-ID clear; unapproved dual source of truth |
| Contiguous leading franchise block via pure `SeedCollectionGrouper` | Weight alone cannot guarantee AC (Q9); embedding-less eligible union (R9) | Generic MMR redesign; public grouping field; profile-replay seed; fourth port |
| Paired `-TmdbSignals` offline evaluation + tracked comparison.md | Ticket HR@10/MRR measurement; exact HitCount bar (Q10; R6–R8) | Fabricated associations; rounded HR pass; live TMDb in CI; force-adding ignored JSON |
| Incremental enrichment only | Accepted Compass F4 limitation (R9) | Automatic backfill/sweep follow-up ticket |

### Data model (folded; I1-authorized; R1–R3)

- **MovieKeyword**: `(int Id, string Name)` — positive ID, trimmed nonblank name; snapshots `ImmutableArray<MovieKeyword>`.
- **MovieSignalSnapshot**: nested availability enum `Unavailable`/`KnownEmpty`/`Available` in the same file; optional Keywords snapshot; optional `FullCast` (`ImmutableHashSet<string>` normalized names); **collection availability only** — no collection identity fields; no `MovieCastEntry`.
- **MovieMetadata**: sole collection identity/validation `CollectionId`/`CollectionName`; optional Keywords/`FullCast` signal carriers.
- **KeywordRecord**: `Id` (external keyword int), `Name`; join `movie_keywords(movie_id, keyword_id)`.
- **movie_tmdb_cast**: movie ↔ existing actor identity (no second actor table; Care-item-3 table name retained as provenance).
- **Collection**: reuse `CollectionRecord` + `MovieRecord.CollectionId` on recommended Q6 branch.
- **Per-signal save table**: Available → replace with valid values; KnownEmpty → clear; Unavailable/Disabled → no signal write; explicit clear-all → clear all; nullable `CollectionId` alone never clears; unavailable/disabled collection supersedes DEV-424 clear-on-OMDb-success while KnownEmpty no-collection clears remain.
- **Load/lock**: tracked-load keyword joins, full-cast joins, and related identities before replacement; lock keyword IDs in a distinct stable namespace and both base/`FullCast` actor natural keys using existing upsert discipline; no accidental writes to Unavailable associations; atomic rollback on failure.
- **MovieFeatures**: `ImmutableHashSet<int> Keywords` (defensive), `int? CollectionId`, enabled effective Cast = base ∪ `FullCast`.
- **SimilarityWeights**: add `Keyword` / `Collection` finite nonnegative doubles.
- **Evaluation catalog**: optional keyword IDs, `FullCast`, CollectionId without losing base cast/features; enriched fixture carries keyword=1/collection=2 explicitly.

### Internal signal contract (folded; not a public API)

Offline assumed TMDb shapes, one-logical-invocation sequence, typed supplemental outcome matrix, and evaluation mode transport — see `ASSUMPTIONS.md` and FR-001/FR-003/FR-010. No OpenAPI/HTTP DTO change. Seed grouping communicated only by adjacency in the existing flat recommendation array. Profile-replay comparison receipt does not prove seed grouping.

## Project Structure

```text
CONTEXT.md
specs/DEV-358/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md
  comparison.md                                 # Phase B tracked acceptance receipt (R6)

src/LamuFlix.Core/Domain/{MovieMetadata.cs,MovieKeyword.cs,MovieSignalSnapshot.cs}
src/LamuFlix.Core/Features/Recommendations/{MovieFeatures.cs,MovieFeaturesMapper.cs,SimilarityScorer.cs,GetRecommendationsQueryHandler.cs,SeedCollectionGrouper.cs}
src/LamuFlix.Core/Options/SimilarityWeights.cs
src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs
src/LamuFlix.Infrastructure/Adapters/{TmdbMetadataProvider.cs,TmdbMetadataProvider.Mapping.cs,TmdbMetadataProvider.Log.cs,TmdbMetadataProvider.Signals.cs,ChainedMetadataProvider.cs,MetadataProviderServiceCollectionExtensions.cs}
src/LamuFlix.Infrastructure/Persistence/{LamuFlixDbContext.cs,EfMovieRepository.cs,EfMovieCatalog.cs,EfRecommendationCandidateSource.cs,EfRecommendationProfileSource.cs}
src/LamuFlix.Infrastructure/Persistence/Records/{MovieRecord.cs,KeywordRecord.cs}
src/LamuFlix.Infrastructure/Persistence/Configurations/{MovieConfiguration.cs,KeywordConfiguration.cs}
src/LamuFlix.Infrastructure/Persistence/Migrations/{<EF-generated timestamp>_AddTmdbRecommendationSignals.cs,.Designer.cs,LamuFlixDbContextModelSnapshot.cs}

tests/LamuFlix.UnitTests/Domain/MovieMetadataSignalTests.cs
tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs   # pure chain; no WireMock HTTP suite
tests/LamuFlix.IntegrationTests/MetadataProviderTmdbSignalTests.cs  # embedded offline WireMock (R5)
tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs                  # existing; R7 extend (not prospective)
tests/... (remaining exact Q11 unit/integration/evaluation files)
scripts/run-evaluation.ps1
tests/LamuFlix.Evaluation/Fixtures/{baseline.tmdb.json,baseline.tmdb.sources.json}
artifacts/evaluation/DEV-358/{baseline.json,tmdb.json}              # ignored raw JSON; never force-add
```

**Removed from planned edit set (R5)**: `tests/LamuFlix.UnitTests/Adapters/TmdbMetadataProviderSignalTests.cs`.

**Structure Decision**: Extend existing Core Recommendations feature and Infrastructure adapters/persistence — no new project folder or architectural layer. Exact paths are the amended Q11 planned edit set (C1/C2/R3/R5/R6/R7); EF timestamp is tooling-assigned only. Any filename discrepancy discovered later must be recorded as a bounded path correction in `brief.md` before use (Q11) — Quill verified listed tracked paths exist at HEAD `c2d78146`, including existing `tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs` (R7 extend target, not a new file); reserve prospective status for `comparison.md` and genuinely new targets only, not existence claims.

## Test Strategy

Per Q11 / FR-014 / I3 / R2/R5/R8:

- Unit: domain snapshots/availability/equality/dirty-tracking, mapper, scorer symmetry/bounds/zero/absent, `SeedCollectionGrouper` order/ties/capacity/no-op, pure chain matrix (no duplicate HTTP suite).
- Integration: WireMock TMDb signals in `MetadataProviderTmdbSignalTests` (independent parse, mixed known-empty/unavailable, large-cast correctness smoke, zero credential URI logging); Testcontainers persistence replace/clear/preserve/reload-save/concurrent same-keyword/mixed-state/rollback/migration + amended DEV-424 clear-on-OMDb proof; candidate union including embedding-less eligible; API seed recommendation grouping.
- Evaluation: `EvaluationEntry` transport; fixture loader options with explicit positive weights; Disabled baseline reproduction; independently nonzero keyword and collection coverage; leakage; CLI/env isolation; `TmdbSignalReplayTests` (retained provenance name); paired publish → tracked comparison.md (ordinary `dotnet test` does not replace it).
- Property: FsCheck for independent invariants (equality/hash, score symmetry/bounds).
- Libraries: Shouldly, NSubstitute, AutoFixture, Theory/MemberData (R5).
- Never mock EF driver; no explanatory comments; no threshold edits.
- Tests may be written alongside production code or red-first (`$implement`; TDD optional). `SC-005` requires automated coverage plus applicable gate evidence across all `FR-014` paths — **no** mandatory failing-then-passing execution receipt (I3).

## Gates (Phase B only)

From harness.yml / AGENTS.md — Spec grill runs none:

- Harness build and `dotnet test`
- `./scripts/run-roslyn-analyzers.ps1`
- `./scripts/run-cyclomatic-complexity.ps1` (refactor threshold later)
- `./scripts/run-jetbrains-inspectcode.ps1`
- `./scripts/run-property-tests.ps1`
- `./scripts/run-vulnerable-packages.ps1`
- Harness-selected format check
- Pre-PR: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since`)
- Exact-head reviews + hosted checks

Exit-2 SKIPPED(scope-empty), configured SKIP, and mutation N/A stay distinct from PASS. Unrunnable applicable gate = Could not run (blocks). No web gate or acceptance-framework generation without applicable frontend diff/opt-in.

## Loop discipline (Q12)

- Closing bar: zero unresolved in-scope Critical/High; no unfulfilled ticket AC; no failed/unrunnable applicable mandatory gate. Medium/Low get explicit dispositions.
- All three axes (Sentry, Ledger, Compass) required for this M ticket.
- Hard cap: **2** plan-challenge review/fix rounds; separately **2** total build internal code-review/ship-review rounds. No third receipt-repair round. Remaining blockers at cap stay blockers.
- Follow-ups via Rigger per charter; otherwise noted, no ticket (including README residual and incremental-enrichment limitation).

## Risks

| Risk | Mitigation |
|---|---|
| Q6 unresolved at Gate 1 | Checkbox carried verbatim; Gate 1 closed; FK branch conditional only |
| Assumed TMDb schemas diverge from live API | Fail closed as Unavailable; WireMock fixtures encode assumed shapes; incompatibility surfaces as failure evidence (Q7) |
| DEV-424 clear-on-OMDb wipes unavailable collection | R1 supersession: Unavailable/Disabled collection is no-op; KnownEmpty still clears; amend collection proof |
| Untracked joins wiped / concurrent keyword races | R2 tracked-load before replace; deterministic locks; reload-save and concurrent same-keyword tests; atomic rollback |
| Empty provenance / HR regression / cast-only coverage | BLOCKED/FAIL; Package 8 locked; independently nonzero keyword+collection coverage required (Q10; R6/R8) |
| Vector shortlist omits franchise members | Indexed candidate union before cutoff, including embedding-less eligible locals (Q9; R9) |
| Uncapped full cast at personal-library scale | Accepted residual; large-cast correctness smoke only — no cardinality/performance gate (R5) |
| Previously enriched titles lack signals | Accepted incremental limitation; no automatic backfill (R9) |
| Upstream drift at build | Q1 bounded recon at Phase B HEAD |

## Quickstart (folded)

Phase B only, after owner checkbox + dependency revalidation:

```powershell
# Paired evaluation (raw JSON ignored; tracked receipt is specs/DEV-358/comparison.md)
pwsh -NoProfile -File ./scripts/run-evaluation.ps1 -FixturePath ./tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.json -OutputPath ./artifacts/evaluation/DEV-358/baseline.json -Seed 356 -TmdbSignals Disabled
pwsh -NoProfile -File ./scripts/run-evaluation.ps1 -FixturePath ./tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.json -OutputPath ./artifacts/evaluation/DEV-358/tmdb.json -Seed 356 -TmdbSignals Enabled
```

Then write tracked `specs/DEV-358/comparison.md` per Q10/R6 (commands, SHA-256 hashes, metrics, weights, independent coverage, scrubbed provenance; state that profile replay does not prove seed grouping) and run harness gates listed above. Do not invent metrics during Spec drafting.
