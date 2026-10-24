# Implementation Plan: DEV-416 Apply Maximal Marginal Relevance diversification to blended recommendations

**Branch**: `feature/416-spec` | **Date**: 2026-10-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/DEV-416/spec.md`, settled grill brief `specs/DEV-416/brief.md` (Q1–Q12; grill closed by Patron Q12), facts from note `recon-DEV-416`, Patron rulings in `specs/DEV-416/CONCLUSIONS.md`, product authority in `specs/PRODUCT.md` sections 1–3 and 5.

**Note**: Planning artifact only (Phase 2 Spec Kit drafting). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1, and until Phase B entry evidence is complete. No `design.md` (no ui tag). No clarify/checklist or ADR on this M path. Gate 1 is not claimed.

## Phase B entry preflight (executable before Code Phase 1)

Schedule these Build-workspace checks before T001. Do not run gates, tests, or analyzers from Spec. Missing evidence blocks code.

| Check | Owner / evidence | Blocks code if missing |
|---|---|---|
| Spec PR merged and Gate 1 established | Rigger / YouTrack `Ready to build` handoff in `DEV-416` | Yes |
| Phase B recon drift vs `origin/main` for the authorized file set; reconcile against frozen decisions; any new fact gap routes recon (brief task order item 1) | Wisp → `recon-DEV-416` refresh | Yes |
| Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef`) | Gauge → recorded in `DEV-416` | Yes |
| Keel drift check or amend against brief/spec before code | Keel handoff in `DEV-416` | Yes |

## Summary

Deliver deterministic Maximal Marginal Relevance diversification of the existing blended recommendation ranking: a pure static `MmrSelector` over the full eligible scored pool before Take, using `RecommendationOptions.MmrLambda` (default 0.7), DEV-338 `SimilarityScorer` pairwise redundancy with per-call feature projection and incremental running maxima, movie-ID tie-break, and mapping selected IDs back to original Movie, blended Score, and caller-safe Reasons. Prove with focused selector tests, bounded FsCheck plus independent full-rescan reference, the Q5 sequel-chain fixture, and handler diversification/payload coverage. No public API, schema, dependency, or web change.

## Technical Context

**Language/Version**: C# / .NET 10 backend. No web change.

**Primary Dependencies**: None new. Reuse existing Core recommendation types: `SimilarityScorer`, `MovieFeaturesMapper`, `BlendScorer`, `RecommendationOptions.MmrLambda`, handler scoring path (recon-DEV-416:9-17,73-84).

**Storage**: No schema or migration. No persistence change. Candidate uniqueness remains the existing MovieFeatures join structural guarantee (recon-DEV-416:108-112).

**Testing**: xUnit v3; Shouldly; NSubstitute where already used; FsCheck property conventions already present for recommendations. Real DEV-338 scorer in selector/property/sequel tests — no scorer mock. No new test dependency. No database fixture required for selector proofs. Handler tests remain unit-level with existing pool doubles. All gate/test execution belongs to Phase B / Build workspace.

**Target Platform**: Existing `LamuFlix.Api` host. No public HTTP surface change. Insertion point is `GetRecommendationsQueryHandler` ranking/truncation after `Score(` (recon-DEV-416:15,97-101).

**Project Type**: Existing ports-and-adapters layout with feature-organised Core. Edits stay in `src/LamuFlix.Core/Features/Recommendations/` and matching unit tests. No new project or layer.

**Performance Goals**: Algorithmic bound only: O(n*k) pairwise calls with auxiliary O(n) for n candidates and k = min(Take, n). Not a measured SLA (Q4).

**Constraints**: TreatWarningsAsErrors. Ticket scope authoritative. No LocalPlay/secrets/`Process.Start`. No public HTTP/OpenAPI/web. No schema/migration. No new dependency/port/ADR. Mutation never uses `--since`. Thresholds never lowered. Scope-empty/opt-out/N/A/could-not-run never PASS. PRODUCT Epic 9 sequencing not waived.

**Scale/Scope**: Authorized eight-file implementation inventory in brief Q7. Unanticipated files return to Keel/recon.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). Ticket DEV-416 Scope items 1–4 and AC match brief frozen scope. Owner structural checkboxes: none (Q12).
- Dependency care list (§2.3 #1; PRODUCT §5 #1): no new NuGet/npm dependency (Q3, Q7).
- No new project/folder/layer (§2.3 #2): pure static selector in existing Recommendations folder; no DI service (Q3; PRODUCT §2 non-goals).
- Database schema (§2.3 #3): no migration; no persistence edit (Q7).
- No public API shape change (§2.3 #4): RecommendedMovie and OpenAPI field shapes unchanged (Q2, Q7).
- No LocalPlay/secrets/`Process.Start` (§2.3 #5): excluded (Q7, Q12).
- File scope (§2.3 #6): bounded to Q7 inventory; no deletions or whole-file rewrites.
- Principle I: Core pure functions beside existing recommendation scorers; no new port/ADR (Q3, Q12).
- Principle VII: no new time/randomness; no secrets in source.
- Principle IX: unit and property coverage with real SimilarityScorer; no driver mocks required for this Core-only change (Q6, Q8).
- Static-analysis gates: configured scripts; no threshold waiver; mutation and property tests must execute and exit 0; scope-empty SKIPPED never PASS (Q8).

Post-design re-check: the design below adds the ruled selector types, narrow handler ranking replacement, and test files with no public HTTP contract, no schema migration, no new dependency/port/ADR, and no harness edit. The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-416/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md
├── tasks.md
└── DRAFTING_RECEIPT.md
```

`research.md`, `data-model.md`, `quickstart.md`, `contracts/`, and `checklists/` are not produced. The brief already freezes approach, files, algorithm, fixture, tests, and gate expectations. There is no new public HTTP contract. No `design.md` (no ui tag). No clarify/checklist or ADR on this M path (brief Q12; Conductor drafting ask).

### Source Code (repository root)

```text
# New Core
src/LamuFlix.Core/Features/Recommendations/MmrCandidate.cs
src/LamuFlix.Core/Features/Recommendations/MmrSelector.cs

# Narrow Core edit
src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs

# New unit tests
tests/LamuFlix.UnitTests/Features/Recommendations/MmrSelectorTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/MmrSelectorPropertyTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/MmrSelectorSequelChainTests.cs

# Existing handler tests (order/coverage updates only)
tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs
```

No `/web` edit. No `harness.yml` or gate-script edit. No migration file. No CONTEXT.md rewrite. Unchanged: `RecommendationOptions`, `MovieFeaturesMapper`, `SimilarityScorer`, `BlendScorer`.

**Structure Decision**: Keep MMR as a pure static Core selector beside existing scorers. Project features once per handler call and invoke Select over the full scored pool before Take, then map IDs back to original scored payloads. Fixture builders and the full-rescan reference stay test-local (Q3–Q7).

## Design Overview

1. **Selector contract (Q3, Q10, A1).** Add sealed `MmrCandidate` and static `MmrSelector.Select(...)`. Validate the full input before empty shortcuts. Implement the identical DEV-338 SimilarityWeights policy privately in `MmrSelector.cs`: reject each of the seven properties (Director, Writers, Cast, Genres, YearProximity, RuntimeProximity, RatingBoost) when < 0.0 or !double.IsFinite with `ArgumentOutOfRangeException`, before empty shortcuts; no scorer visibility edit, reflection, or dummy Score call; `SimilarityScorer` unchanged within the Q7 inventory. Treat inputs as read-only. Return ordered MovieIds only.

2. **MMR loop (Q2, Q4).** Relevance = original blended score. At each step maximize `lambda * relevance - (1 - lambda) * maxSim`. Keep per-unselected running maximum redundancy (initially 0), invocation-owned for that Select call. After selecting an item, if more picks remain, score each remaining candidate against the newly selected features and update each running maximum as `max(previous maximum, new pair similarity)` over all prior selections, never latest-only. Discard pairwise Reasons. Exact equal objectives tie by ascending MovieId.Value, with no epsilon or rounding. No static mutable/cross-request state and no full pairwise matrix; auxiliary state remains O(n). Retain earlier-selected-maximum and independent full-rescan reference proofs.

3. **Handler integration (Q2, Q6, Q11).** After existing `Score(` produces the eligible scored list, project features once, call Select with `recommendationOptions.MmrLambda` and `query.Take`, map IDs to original Movie/Score/Reasons in selector order, and stop. Do not post-sort by Score. Do not change RecommendedMovie/OpenAPI shapes.

4. **Proof order (Q5–Q6, Q9).** Validation/boundary tests → algorithm/ties/lambda/missing-feature/earlier-max tests → FsCheck/reference and sequel fixture (both before handler wiring; may proceed independently once contract is stable) → handler diversification/lambda/payload/non-monotonic order → refactor to complexity ≤6 → Gauge Q8 gates at exact head and again on rebased head.

## Loop discipline

- **Closing bar:** Critical, High, and Medium findings block closure. Low findings are recorded without blocking. Zero unresolved Critical/High/Medium within frozen scope; applicable gates earned. Scope-empty or opt-out is skipped, never pass; an unrunnable gate has not passed. Low and out-of-scope findings follow charter follow-up policy (Q1).
- **Round cap:** maximum two review rounds and two fix commits per round; third-round findings go to user review with the PR; a third fix commit means the finding or plan was wrong (Q1).
- **Frozen scope:** deterministic MMR diversification of the existing blended recommendation ranking using RecommendationOptions.MmrLambda (default 0.7), the DEV-338 SimilarityScorer, movie ID tie-break, min(Take, candidate count), FsCheck properties and the sequel-chain fixture tests; anything else is a follow-up issue, not a finding in this round (Q1).
- **Gate verdicts:** preserve scope-empty SKIPPED, opt-out SKIP, mutation N/A, and Could not run distinctions; never PASS. Mutation and property tests must execute and exit 0 (Q8).

## Complexity Tracking

> No constitution violations requiring justification. File placements are Patron-ruled under ticket scope / §2.3 item 6 without owner checkboxes. No new package. No ADR.
