# Implementation Plan: DEV-348 Postgres pgvector recommendation candidate source

**Branch**: `feature/348-spec` | **Date**: 2026-10-21 | **Spec**: [spec.md](spec.md)

**Input**: [spec.md](spec.md), settled grill brief [brief.md](brief.md), facts from note `recon-DEV-348`, Patron rulings in [CONCLUSIONS.md](CONCLUSIONS.md), taste receipt [ASSUMPTIONS.md](ASSUMPTIONS.md), `specs/PRODUCT.md`.

**Status**: Draft planning artifact only (Phase A). Not frozen; Gate 1 not claimed. No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1, and until Phase B prerequisites in brief Q1/Q4 are met.

## Summary

Replace the uncapped enriched-movie candidate adapter with a PostgreSQL pgvector cosine-distance (`<=>`) query over stored `movie_features` vectors. Return at most `CandidatePoolSize` (default 50) nearest neighbours as `RecommendationCandidate(Movie, CosineSimilarity)`, empty without throw when the selected vector is missing/unusable, and append handler-only `Reason("Similar story")` when cosine similarity is strictly above a calibrated `RecommendationOptions.SimilarStoryThreshold`. Preserve weighted metadata scoring and final handler order; defer reranking to DEV-354. Consume landed DEV-413 mapping unchanged; no schema, dependency, HTTP, or web work in this ticket.

**Note on the Spec Kit phases**: research, the data model, contracts, and the quickstart are folded into this plan. No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` is produced. Contracts, file set, retrieval design, handler behavior, calibration checkpoint, tests, and gates are already ruled in brief Q1–Q11; this ticket adds no HTTP route and authors no persistence schema. `.specify/extensions.yml` is absent, so no extension hook ran. There is no agent-context script under `.specify/`; `AGENTS.md` / `CLAUDE.md` SPECKIT markers are outside the Phase A artifact set named by Conductor and are not edited.

## Technical Context

**Language/Version**: C# on .NET 10 (solution projects under `src/` / `tests/`).

**Primary Dependencies**: None added. Reuse pinned `Pgvector` / `Pgvector.EntityFrameworkCore` already on HEAD (`Directory.Packages.props`; `recon-DEV-348:22`) and existing EF Core Npgsql `UseVector()` wiring (`recon-DEV-348:16,22`).

**Storage**: Consume landed DEV-413 `movie_features` mapping unchanged. No migration, index, or schema authored by DEV-348. Concrete entity/column identifiers are bound only from Phase B recon (brief Q5/Q8; `recon-DEV-348:21`).

**Testing**: xUnit, NSubstitute, FsCheck, PostgreSQL+pgvector Testcontainers. Adapter proofs in `EfRecommendationCandidateSourceTests`; handler unit/property in `GetRecommendationsQueryHandlerTests` / `GetRecommendationsQueryHandlerPropertyTests`; options in `RecommendationOptionsTests`. Deterministic 384-d vectors via `EmbeddingDimensions.Value`. No live Ollama. No Gherkin/Reqnroll. No property-test opt-out. No container-availability waiver.

**Target Platform**: Existing Api / Infrastructure persistence adapters (`recon-DEV-348:15-17`).

**Project Type**: Existing .NET solution using ports and adapters with feature-organised Core. No new project.

**Performance Goals**: At most two sequential read-only async reader commands on populated pools; one when selected vector unavailable. Server-side order and cap before hydration. No N+1 (brief Q6).

**Constraints**: No new NuGet/npm dependency; no new port/project/layer; no schema/migration/index; no HTTP/OpenAPI/web; no LocalPlay/secrets/`Process.Start`; no request-time embedding; no DEV-354 reranking; no fabricated Phase A numeric threshold; no sentinel/fake `CosineSimilarity`. Thresholds stay in `harness.yml`.

**Scale/Scope**: Patron-approved nine-file envelope (five production, four test) in brief Q8. Delivery scope: stored-vector server-bounded candidate retrieval with similarity, validated calibrated threshold, handler-only story reason, forced caller/test migration. Scope statements describe that delivery boundary and do not preclude findings about failures to meet in-scope requirements.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Principle I (boundaries): no new project, folder, layer, or Core port. Existing `IRecommendationCandidateSource` changes in place; architecture whitelist unchanged (brief Q2; constitution ports whitelist; `recon-DEV-348:29`).
- Principle II (handlers): `GetRecommendationsQueryHandler` remains the sole consumer; story suffix is handler-only; scorer/result/reason/output records unchanged (brief Q3).
- Technology Stack: pgvector operator and EF translation already licensed by pinned packages and `UseVector()` (`recon-DEV-348:22`); no new package.
- Principle IX (tests): real PostgreSQL+pgvector for translation/serialization; never mock the driver (brief Q7; AGENTS Non-negotiables).
- API and Contract Rules: no public route/DTO/OpenAPI/TS change (brief Frozen scope; ticket ui: none).
- Documentation rules: no ADR authored by Quill; no `CONTEXT.md` / `README.md` / `PRODUCT.md` edit in this ask (brief Q8/Q11).
- Static-analysis gates: Roslyn, complexity, InspectCode, mutation, property tests, vulnerable packages, format, `dotnet test`, web gates — harness thresholds authoritative, never copied into this plan as numeric literals and never lowered (brief Gate expectations).
- Charter §2.3 / PRODUCT §5: care-list items 1–6 ruled; zero owner escalations (brief Q11).

Post-design re-check: the same checks hold. This plan adds no package, project, new port name, schema authored here, HTTP contract, CONTEXT.md edit, ADR, or file outside the approved brief envelope. Side design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) are not produced — the brief already freezes contracts, files, ordering, and proofs.

## Scope and prerequisite checks

Binding product, care-list, and sequencing constraints. Constraints remain in force.

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches canvas note DEV-348 Tracker summary / ticket Scope & Technical Design and Acceptance Criteria (brief Frozen scope). Nothing is added or dropped. No owner checkbox.
- No schema or migration authored by this ticket (care list item 3; brief Q5; consume DEV-413 only).
- Public API shape unchanged (care list item 4; ticket ui: none).
- Epic / sequencing: PRODUCT defers Recommendations / Ollama / pgvector execution until Epic 7 completes (`specs/PRODUCT.md:17`). Phase B build also requires landed DEV-413 schema and landed DEV-342 golden implementation, plus drift/consistency analysis and the measured-default Patron checkpoint (brief Q1/Q4).
- Phase A contains no numeric `SimilarStoryThreshold` default (brief Q4). Quill does not invent DEV-413 identifiers or DEV-342 paths (brief Q8).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-348/
├── brief.md, CONCLUSIONS.md, ASSUMPTIONS.md   (Keel / Patron)
├── spec.md                                     (/speckit-specify)
├── plan.md                                     (/speckit-plan)
├── tasks.md                                    (/speckit-tasks)
├── checklists/requirements.md
└── DRAFTING_RECEIPT.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. `AGENTS.md` / `CLAUDE.md` are not updated.

### Source Code (repository root)

```text
src/LamuFlix.Core/Features/Recommendations/RecommendationCandidate.cs   # new — sealed record
src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs               # in-place return contract
src/LamuFlix.Core/Options/RecommendationOptions.cs                      # SimilarStoryThreshold + validation
src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs  # unwrap + story suffix
src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs    # two-read vector query

tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs
tests/LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs
```

**Structure Decision**: Match existing Recommendations feature folder, Core Ports, and Infrastructure Persistence adapter layout (`recon-DEV-348:14-19`). DI stays `AddScoped<IRecommendationCandidateSource, EfRecommendationCandidateSource>()` with direct `IOptions<RecommendationOptions>` injection into the adapter (brief Q6; `recon-DEV-348:16`). No production file expansion beyond the brief envelope without a new Patron file-scope ruling.

## Design (from settled brief)

1. **Core contract (Q2)**: `RecommendationCandidate(Movie, CosineSimilarity)`; port returns that list in place.
2. **Options (Q4)**: `SimilarStoryThreshold` finite in `[-1, 1]`; calibrated default only after Phase B measurement + Patron ratification; no Phase A placeholder.
3. **Retrieval (Q5–Q6)**: selected-vector read then one server-bounded candidate query; eligibility, order, cap, hydration, cancellation/fault rules as brief; consume DEV-413 identifiers from recon.
4. **Handler (Q3)**: unwrap movie for scorer; preserve weighted score/order/Take; append one strict-threshold "Similar story" suffix after feature reasons.
5. **Atomic Stage B (Q10)**: record + port + calibrated option + real adapter + forced substitutes/tests in one honest green build phase — no sentinel similarities or stubs. Story reason is a separate Stage C.
6. **Tests and gates (Q7/Q9)**: real DB adapter proofs; handler unit/property; options startup; closing bar Critical/High/Medium; three axes; two-round caps; Phase 3 step 5 gate verdicts.

## Complexity Tracking

> No constitution violations requiring justification. Existing port changes in place; adapter stays in Infrastructure Persistence; no new architectural layer.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phase boundaries

- **Phase A (this ask)**: Spec Kit docs under `specs/DEV-348` only (`spec.md`, `plan.md`, `tasks.md`, checklist, drafting receipt). No freeze, publish, gates, or implementation.
- **Phase B**: After Gate 1 and workspace handoff, follow `tasks.md` stages A–D (brief Q10): prerequisites/calibration → atomic contract+adapter → story reason → refactor/gates/review/ship. Fresh Phase B drift/schema/golden recon remains a later pipeline step.
