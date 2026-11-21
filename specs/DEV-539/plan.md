# Implementation Plan: TMDb Crowd similarity criterion

**Branch** — `feature/539-spec` | **Date** — 2026-11-21 | **Worktree** — `F:/Dev/LamuFlix.worktrees/feature-539-spec`

**Status** — PLAN FROZEN. See [plan adjudication](PLAN_ADJUDICATION.md) and [Patron's ruling](CONCLUSIONS.md#plan-challenge-adjudication-ledger-h1h2m1m2m4l1-compass-f1-sentry-m2). Gate 1, implementation and runtime acceptance remain pending.

**Authority** — DEV-539 ticket (Wanted, AC, Out of scope, Open points, Where it lands), `brief.md` Q1–Q8, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `spec.md`, Maestri `recon-DEV-539`, `specs/PRODUCT.md`, Accepted `docs/adr/0048-tmdb-crowd-similarity.md`, `.specify/memory/constitution.md`.

## Summary

Add Crowd, a binary item-to-item similarity criterion sourced from the first page of TMDb movie recommendations, for For you and Related. Persist TMDb ids and recommendation pairs, seed legacy IMDb-identified movies through an explicit Api operator mode, resolve links at the DEV-544 library feature-load seam, and use DEV-545 evidence to set default activation and any Crowd-active strong-match bar.

## Technical context

- **Stack** — .NET 10 (`LamuFlix.Core`, `LamuFlix.Infrastructure`, `LamuFlix.Api`); EF Core / PostgreSQL; existing TMDb typed `HttpClient` and shared resilience handler; xUnit + WireMock + Testcontainers for tests. No frontend file changes; no public route/DTO/status-code changes.
- **Scale** — At the historical 449-movie library size, one logical find per eligible movie and one first-page fetch per match total at most 898 logical requests before shared retries. Normal enrichment adds one first-page fetch per newly enriched movie when TMDb is configured.
- **Boundaries** — ADR 0048 Accepted amending ADR-0011. DEV-543 owns For you consumer integration readiness. DEV-544 owns the library feature-load seam. DEV-545 owns the measurement harness. No second rate limiter, no new package/project/layer, no `Process.Start`, no secret exposure.

## Constitution check

Gates checked against `.specify\memory\constitution.md` (worktree current). Constitution compliance (line 962) requires each constitution-gate evaluation to cite the principle number.

| Principle | Gate | Plan response |
|---|---|---|
| I — Ports and adapters | Named ports; Core isolation | Reuse existing TMDb adapter and `IRecommendationCandidateSource` / EF adapter. No new port. |
| II — Explicit handlers | Handlers + AddHandler | Api startup validates and dispatches the Core operator command via `HandlerRegistration.cs`. Enrichment retains its existing `MarkEnriched` handler. |
| III — Typed query model | MovieQuery | N/A. Browse filtering is outside scope. |
| IV — Domain transitions | Movie mutators | Carry TMDb id and recommendation outcome through `MovieMetadata` / `Movie` via existing enriched transition. |
| V — Validated inputs and errors | ProblemDetails / options | Gate backfill and Crowd presence on existing TMDb configuration predicate; validate Crowd weight/activation through `RecommendationOptions` pipeline. |
| VI — Observability | Safe diagnostics | Backfill counts and DEV-545 before/after metrics are sanitized delivery evidence; never log ApiKey. |
| VII — Configuration | Options; LocalPlay; Process.Start | Reuse `TmdbOptions` and extend `RecommendationOptions` / `SimilarityWeights` for Crowd weight and activation. No LocalPlay, secrets, or `Process.Start`. |
| VIII — Ubiquitous language | Constitution VIII | T014a aligns `CONTEXT.md` and `docs/recommendations.md` in the introducing delivery PR under the brief's authorized boundary. |
| IX — Test pyramid | Meaningful layers | WireMock typed-client integration, real-Postgres (Testcontainers) persistence/resolution/atomic retries, unit pins for criteria/reasons/parity, DEV-545 runtime evidence. No web e2e invent. |

### API and contract rules

No public HTTP route, DTO field set, or status-code change. OpenAPI document at `web/src/api/openapi.json` and generated TypeScript remain untouched; no OpenAPI/TS regeneration. Contract chain stays C# DTO → OpenAPI → generated types with zero drift expected for this ticket (constitution Development Workflow → API and Contract Rules).

### Static-analysis gates

Size L delivery revision must pass the constitution Static-Analysis Gates (Roslyn, CA1502 including refactor `-Threshold 6`, InspectCode) plus mutation, property tests, vulnerable packages, format, and tests with honest exits. Web scope-empty → `SKIPPED (scope-empty)`. Failure or `Could not run` blocks. Never fold non-pass into PASS; never lower thresholds.

## Approach

1. **Gate 1 and pickup.** The owner merges the spec PR containing Accepted ADR 0048 before coding. T001 verifies named seams and existing operator port fit and records the DEV-545 loader/scorer path. Readiness attaches only to T012/T015/T017. Missing required facts go to recon. A new port requires a cited ruling and ADR under constitution I. If evaluation needs alignment, identify exact files and obtain a cited ruling before editing.
2. **Schema and domain.** Add nullable, non-unique `movies.tmdb_id` and unique pairs `(source_movie_id, recommended_tmdb_id)`. Cascade source-owned rows and index `recommended_tmdb_id`. Retain incoming catalogue IDs. Extend `MovieMetadata` and `Movie` so `MarkEnriched` carries identity and fetch outcome via the existing handler. Prove migration on a populated database.
3. **TMDb adapter.** Reuse the configured typed client. Validate the first page and any echoed source ID if supplied by the response contract. Handle invalid pages and transport failures under FR-005 and Q6. Record safe outcomes and redact credential query values at the typed-client logging boundary. Omit full TMDb URIs, raw exception text and payloads from logs, output and evidence.
4. **Operator backfill.** Api startup parses `--backfill-tmdb-recommendations`, refuses malformed operator input, verifies configuration and applied migration, and dispatches a Core command through `AddHandler` before hosting. The handler processes one movie at a time with cancellation, reusing existing ports and the shared bounded retry/circuit pipeline including 429 handling. Leave failed rows retry-eligible. On breaker rejection or cancellation, exit nonzero with sanitized partial counts. Preserve committed rows and skip them on rerun. Capture actual home-library counts at T019 before enabled measurement, after confirming backup and rollback readiness.
5. **Feature-load resolution (consumes DEV-544).** Batch-load pairs at the library seam and pass an immutable link lookup with the existing snapshot. Resolve recommended TMDb IDs to every matching library row across distinct catalogue identities. Exclude self-links. Either direction gives a single binary link. Keep the scorer free of database and network calls.
6. **Scoring and glossary (consumes DEV-543).** Crowd is after Themes, has default weight 2, and contributes to the denominator only in the active configured state. Validate effective sums and use a separate activation Boolean. Wire and test optional `CrowdActiveStrongMatchThreshold` in `[0, 1]` before measurement. Use it only when active and set. Thread the selected seed title through existing For you reasons and explicitly map the Related reason. Preserve strong-reason eligibility and default tier selection unless Q4 or landed DEV-543 requires a change. Apply T014a in the introducing delivery PR.
7. **Evidence and defaults (consumes DEV-545).** T015a pins exact scores from the pre-Crowd scorer separately from T017. After T019, use the same library and inputs for DEV-545 before/after. Verify the candidate harness exercises the production feature/scoring path. Attach sanitized hit rate, MRR, coverage table, judged pairs and backfill counts. T018 tunes the existing active option if justified, verifies the final scored configuration and enables Crowd by default only if hit rate holds. T020–T022 govern delivery gates, rebase, ship review, PR checks and the merge bar.

## Authorized project files

| Area | Files and scope |
|---|---|
| ADR | `docs/adr/0048-tmdb-crowd-similarity.md` (Accepted; review artifact — already present) |
| Glossary and guidance | `CONTEXT.md` and `docs/recommendations.md` (T014a, scope in the brief). |
| Domain | `src/LamuFlix.Core/Domain/Movie.cs`, `src/LamuFlix.Core/Domain/MovieMetadata.cs` |
| Options | `src/LamuFlix.Core/Options/RecommendationOptions.cs`, `src/LamuFlix.Core/Options/SimilarityWeights.cs` (Crowd weight + activation; conditional Crowd-active strong-match bar); `TmdbOptions.cs` expected unchanged |
| TMDb adapter | `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.Mapping.cs`, `TmdbMetadataProvider.cs`, `TmdbMetadataProvider.Signals.cs`; existing typed-client logging boundary if needed for redaction only. Preserve auth, configuration and resilience wiring. |
| Persistence | `src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs`, new migration under `Migrations/`, `EfRecommendationCandidateSource.cs` |
| Operator entry and handler | `src/LamuFlix.Api/Program.cs`, Core backfill command/handler/five-count result in the existing enrichment feature, and existing `HandlerRegistration.cs` (`AddHandler`). |
| Enrichment handler | `ProcessEnrichmentCommandHandler.cs` — no behaviour change expected |
| Feature carrier and scoring | Under `src/LamuFlix.Core/Features/Recommendations/`: `MovieFeatures.cs`, `MovieFeaturesMapper.cs`, `MovieFeaturesSnapshot.cs`, `SimilarityScorer.cs`, `RecommendationReasonBuilder.cs`, `RecommendationScoring.cs`, `GetForYouRecommendationsQueryHandler.cs`, and `GetRecommendationsQueryHandler.cs`. At T001 verify the landed paths and whether `src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs` already carries the snapshot. |
| Tests and evaluation | `tests/LamuFlix.IntegrationTests`, `tests/LamuFlix.UnitTests` and existing `LamuFlix.Tests.Common` fixtures. T001 checks `LamuFlix.Evaluation` and `Evaluation.IntegrationTests` for production path reuse. |

## Project structure

### Documentation (this feature)

```text
specs/DEV-539/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # this file
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/           # none — no public API change
├── checklists/
├── tasks.md
└── DRAFTING_RECEIPT.md
```

### Source Code (authorized)

See the authorized file table above.

**Structure Decision**: Extend existing vertical-slice / ports-and-adapters layout. No new project, top-level folder, or architectural layer.

## Complexity Tracking

> No constitution violations requiring justification. Schema change is ticket-authorized (`PRODUCT.md` §5 / ticket AC 2).

## Phase 0 research

See [research.md](research.md). All drafting unknowns are resolved from brief/CONCLUSIONS; no NEEDS CLARIFICATION remains for Gate 1 drafting.

## Phase 1 design

See [data-model.md](data-model.md) and [quickstart.md](quickstart.md) for the drafting references. Use the frozen plan and T009 for the operator flag and preflight. The challenge decisions govern duplicate resolution, pair lifecycle and baseline provenance.
