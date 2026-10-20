# Implementation Plan: DEV-342 SimilarityScorer property and golden tests

**Branch**: `feature/342-spec` | **Date**: 2026-10-20 | **Spec**: `specs/DEV-342/spec.md`

**Input**: Feature specification from `specs/DEV-342/spec.md`, settled grill brief `specs/DEV-342/brief.md`, facts from notes `recon-DEV-342` and `recon-DEV-342-2`, Patron rulings in `specs/DEV-342/CONCLUSIONS.md` Q1–Q8.

**Note**: Planning artifact only (Phase A). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. Phase B remains gated on Epic 7 (`specs/PRODUCT.md:17`) plus merged DEV-337 and DEV-338 production implementations verified by pickup drift recon. No stubs. Spec-workspace must not run implementation gates (brief Q1/Q8).

## Summary

Deliver FsCheck property coverage and an independently audited golden ranking fixture that verify the mathematical correctness of the real merged `SimilarityScorer`. Four 1000-iteration invariant properties plus a typed 30-movie / three-seed top-3 suite. Nothing else ships.

## Technical Context

**Language/Version**: C# / .NET 10. Unit tests only. No production or web change.

**Primary Dependencies**: None added. Tests use existing xUnit v3, Shouldly, FsCheck, and FsCheck.Xunit.v3 already referenced by `LamuFlix.UnitTests` (`recon-DEV-342:48-50`). Consumes landed `MovieFeatures`, `SimilarityScorer`, `GenreWeights`, and existing `SimilarityWeights` from predecessors; none are implemented here.

**Storage**: N/A. Typed in-memory golden data only; no disk I/O, migration, or persistence edit.

**Testing**: xUnit v3, Shouldly, and FsCheck in `LamuFlix.UnitTests`. Property-test gate active, no opt-out. No Testcontainers, HTTP fixture, mocks, second assertion library, or Gherkin/Reqnroll.

**Target Platform**: Existing unit-test project. No new host.

**Project Type**: Existing ports-and-adapters solution. New files only under the existing `tests/LamuFlix.UnitTests/Features/Recommendations/` folder plus `specs/DEV-342/golden-calculations.md`. Not a new project or layer.

**Performance Goals**: No numeric latency target in the brief. None invented. Acceptance is 1000 iterations per property plus golden top-3 match.

**Constraints**: TreatWarningsAsErrors. No new NuGet/npm dependency. No new project, top-level folder, or architectural layer. No production edits. No public API route/DTO/status change. No schema/migration. No options/configuration edit. No `Features:LocalPlay`, secrets, or `Process.Start`. No predecessor stubs or dependency rewrites. No root `CONTEXT.md` edit, ADR, or `ASSUMPTIONS.md`. Thresholds stay in `harness.yml`. Mutation on a test-only `src/`-empty diff must remain truthful `SKIPPED (scope-empty)`.

**Scale/Scope**: Exactly the approved file set in brief Q4. No deletion or unrelated rewrite of unnamed tracked files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-342: generators, four scorer properties, 30-movie golden top-3 for three seeds (`recon-DEV-342:94-108`). Selected-movie exclusion stays DEV-340. Nothing added or dropped. No owner checkbox (brief Q8).
- Care list items 1–6: no new packages; no new project/layer; no schema; no public API; no LocalPlay/secrets/Process.Start; no rewrite of unnamed tracked files (`recon-DEV-342:56-63`; brief Q4).
- No MediatR, generic repository, UnitOfWork, or speculative service abstraction (PRODUCT.md §2).
- Vocabulary: existing root `CONTEXT.md` entries suffice; no glossary edit; no ADR; no `ASSUMPTIONS.md` (brief Q8; constitution VIII).
- Tests: xUnit v3, Shouldly, FsCheck (constitution IX; brief Q2/Q5). Gates from configured scripts; never lower thresholds (brief Q8). Mutation scope-empty is never PASS.
- Coding conventions: no explanatory comments in C# beyond AAA test headers; oracle arithmetic lives in `golden-calculations.md` (brief Q4; AGENTS.md Non-negotiables).
- Phase B sequencing: Epic 7 deferral and no speculative stubs (PRODUCT.md §2; brief Q1).

Post-design re-check: the design below adds no package, project, port, route, schema, glossary edit, production file, or file outside the approved set. The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-342/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── golden-calculations.md   # Phase B only; not authored in Phase A
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, `contracts/`, and `ASSUMPTIONS.md` are not produced. The brief already freezes contracts, files, ordering, assertion semantics, and gate expectations. There is no new HTTP public contract. `AGENTS.md` is not updated: it is outside the feature directory and outside the named file set.

### Source Code (repository root)

```text
tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesArbitraries.cs
tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerInvariantPropertyTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenFixture.cs
tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenTests.cs
```

No production paths. Preserve landed DEV-337/DEV-338 tests, including DEV-338's planned `SimilarityScorerTests.cs` and `SimilarityScorerPropertyTests.cs`.

**Structure Decision**: Existing `LamuFlix.UnitTests` Recommendations folder only, plus Phase B `golden-calculations.md`. If the contract cannot be implemented inside this set, stop for a decision. Do not widen the set. Do not implement or stub `SimilarityScorer` / `GenreWeights` here.

## Design Overview

1. **Phase B pickup (before test writes).** In the Build workspace, verify Epic 7 plus merged DEV-337 and DEV-338 on main. Record VO bounds, constructors, scorer/IDF call signatures, string-genre/integer-ID bridge (including null/blank handling), and predecessor-test overlap. Resolve contradictions before writes; re-run cross-artifact analysis and amend drifted tasks through the prescribed process. Do not invent bridges or alter dependencies.

2. **Local arbitraries (Q2/Q6).** `MovieFeaturesArbitraries.cs` supplies constructive branches for empty/single/partial/identical/disjoint/overlapping profiles from a shared token vocabulary; case variants and preserved null/blank tokens; self-similarity-only nonempty seven-field profiles through generation and shrinking; finite nonnegative weights with all-zero / single-positive / mixed / MaxValue families; real pure GenreWeights contexts for populated and empty/unknown defaults. Retain branch-coverage diagnostics from the Q6 exchange.

3. **Invariant properties (Q2/Q5).** `SimilarityScorerInvariantPropertyTests.cs` hosts exactly four properties with MaxTest = 1000, Category = Property, distinct Replay seeds, preserved shrinking: exact self-similarity 1.0; symmetry within 1e-12; finite inclusive [0,1] boundedness; one-way Reasons implication. Featureless inputs remain in the non-self-similarity properties. Leave unrelated MaxTest = 100 tests untouched.

4. **Independent golden oracle (Q3/Q8).** After prerequisites merge, curate typed 30-movie data and three seeds in `SimilarityScorerGoldenFixture.cs` with explicitly pinned weights (Director = 3.0, Writers = 2.0, Cast = 2.0, Genres = 3.0, YearProximity = 1.0, RuntimeProximity = 0.5, RatingBoost = 0.5). Independently derive and document ordered top-3 identities in `golden-calculations.md` with strict score separation. Then implement `SimilarityScorerGoldenTests.cs` against the real scorer and full-library IDF. Never snapshot scorer output into expected rankings; never call DEV-340 handler.

5. **Proof order (brief dependency-ordered plan / Q8).** (1) Prerequisite verification and pickup drift recon. (2) Generators/shrinkers and four properties. (3) Fixture, independent calculations, golden assertions. (4) Focused suite, property gate, full `dotnet test`, refactor, Q8 harness gates with exact-head receipts, mandatory three-axis review under Q7 limits. Route real dependency failures with replay/counterexample; do not rewrite production, predecessor tests, or assertions to obtain green results.

6. **Gates (Phase B only).** Roslyn; cyclomatic complexity with configured refactor threshold <= 6; JetBrains InspectCode; property tests; vulnerable packages; `dotnet format --verify-no-changes`; `dotnet test` — all must exit 0 (PASS). For test-only zero-`src/` diffs, `scripts/run-mutation.ps1` must report exit-2 `SKIPPED (scope-empty)`, nonblocking and never PASS. Web gates N/A. Native exits only; never opt out, touch production to force mutation, or change thresholds. A gate that cannot run has not passed.

## Loop discipline

- **Closing bar:** Verified in-scope Critical/High findings block. Medium/Low are recorded nonblocking with disposition unless they demonstrate a failed explicit acceptance criterion; classify actual impact and do not waive a failed AC (brief Q7 amended ruling).
- **Axes:** All three M-ticket axes are mandatory — Sentry, Ledger, Compass. A missing report blocks adjudication.
- **Round cap:** Plan challenge and Phase 4 review/remediation are each capped at two rounds, with at most two fix commits per remediation round. Remaining blockers at the cap produce a blocked report with exact unresolved findings; no third round or weakened threshold.
- **Frozen scope:** Local generators/shrinkers; four 1000-case scorer properties; typed 30-movie / three-seed golden top-3; `golden-calculations.md`; Q1–Q8 planning/pickup evidence.
- **Exclusions:** Production edits; predecessor-test rewrites; query-handler exclusion tests; new packages/projects/layers; database/API/playback changes; unrelated cleanup; anything else is a follow-up issue, not a finding in this round. Apply charter follow-up policy (`noted, no ticket` unless Critical/High, broken behavior, or user request). Rigger alone performs tracker writes.

## Complexity Tracking

No constitution violation requires tracking. Patron recorded no constitution departure and no §2.3 escalation (brief Q1–Q8).
