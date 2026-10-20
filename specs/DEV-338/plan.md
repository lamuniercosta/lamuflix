# Implementation Plan: DEV-338 SimilarityScorer pure function

**Branch**: `feature/338-spec` | **Date**: 2026-10-20 | **Spec**: `specs/DEV-338/spec.md`

**Input**: Feature specification from `specs/DEV-338/spec.md`, settled grill brief `specs/DEV-338/brief.md`, facts from note `recon-DEV-338`, Patron rulings in `specs/DEV-338/CONCLUSIONS.md` Q1–Q9, taste copy in `specs/DEV-338/ASSUMPTIONS.md`.

**Note**: Planning artifact only (Phase A). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. Phase B remains gated on Epic 7 (`specs/PRODUCT.md:17`) plus merged DEV-336 and DEV-337 implementations and landed-signature recon. No stubs.

## Summary

Deliver a pure static `SimilarityScorer.Score` that returns a clamped [0, 1] similarity score and immutable caller-safe `Reason` chips for two `MovieFeatures` values under existing `SimilarityWeights` and landed `GenreWeights`. Seven terms, weighted-mean normalization, Q1–Q9 boundaries, focused unit and FsCheck property tests. Nothing else ships.

## Technical Context

**Language/Version**: C# / .NET 10. Core and unit tests only. No web change.

**Primary Dependencies**: None added. BCL, existing `System.Collections.Immutable`, existing `SimilarityWeights`. Tests use existing xUnit v3, Shouldly, and FsCheck. Inputs `MovieFeatures` and `GenreWeights` are predecessor deliverables (DEV-336 / DEV-337), not implemented here.

**Storage**: N/A. No database, migration, or persistence edit.

**Testing**: xUnit v3, Shouldly, and FsCheck in `LamuFlix.UnitTests`. Property-test gate active, no opt-out. No Testcontainers, HTTP fixture, mocks, or second assertion library. No Gherkin/Reqnroll.

**Target Platform**: Existing Core library. No new host.

**Project Type**: Existing ports-and-adapters solution. New types in the constitution-pre-named `Features/Recommendations` folder. Not a new project or layer. Pure static function; no DI.

**Performance Goals**: No numeric latency target in the brief. None invented.

**Constraints**: TreatWarningsAsErrors. No new NuGet/npm dependency. No new project, top-level folder, or architectural layer. No interface/service/DI/cache. No public API route/DTO/status change. No schema/migration. No options/configuration edit. No `Features:LocalPlay`, secrets, or `Process.Start`. No predecessor stubs. No root `CONTEXT.md` edit and no ADR. Thresholds stay in `harness.yml`. Decay rates are `const double` in the scorer (mutation-safe).

**Scale/Scope**: Exactly the approved file set in brief "Approach and approved file set". No deletion or unrelated rewrite.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-338: pure `SimilarityScorer`, seven-term score, caller-safe Reasons (`recon-DEV-338` §10.2–10.3). Nothing added or dropped. No owner checkbox (brief Q8–Q9).
- Core organisation: types live in `LamuFlix.Core.Features.Recommendations` (constitution I; recon §3, §7). Recommendations is constitution-pre-named, not a new architectural layer (care list item 2; brief Q6). Feature folders MUST NOT reference each other; scorer consumes the Recommendations `MovieFeatures` input (approved predecessor path under `Features/Recommendations`, recon-DEV-338:32; brief.md:19,26,30 — landed contract without relocation) plus Domain `GenreWeights` and existing Options `SimilarityWeights` only. Do not invent landed namespaces or change predecessor files.
- Core must not reference EF/Npgsql/HTTP/process APIs (constitution I). Pure static scoring only.
- No MediatR, generic repository, UnitOfWork, or speculative service abstraction (PRODUCT.md §2; constitution II; brief Q6).
- No endpoint, status code, OpenAPI, or generated TypeScript change (constitution API rules; PRODUCT.md care list item 4; recon §7 item 4; task note `ui: (none)`).
- No schema or migration (care list item 3; recon §7 item 3).
- No secret, `Features:LocalPlay`, or `Process.Start` (care list item 5; recon §7 items 5–6).
- Principle VII (Configuration Isolation and Deterministic Time): existing options unchanged; no configuration binding change; no timestamps or randomness introduced by this pure function; LocalPlay and process execution excluded (constitution VII; recon-DEV-338-2:16, :27-30; recon-DEV-338 §7 items 5–6).
- Vocabulary: existing root `CONTEXT.md` Recommendation/Reason/genre entries suffice; Similarity score / Reason scoping notes stay in `brief.md` / `spec.md` (brief Q8; constitution VIII). No ADR for this reversible pure function.
- Tests: xUnit v3, Shouldly, FsCheck (constitution IX; brief Q7). Gates from configured scripts; never lower thresholds (brief Gate expectations). Q9 one-way Reason implication supersedes Q7 iff wording.
- Coding conventions: sealed records, immutable list of Reasons, `nameof` on guards, no explanatory comments beyond AAA test headers (constitution Coding Conventions).

Post-design re-check: the design below adds no package, project, port, route, schema, glossary edit, or file outside the approved set. The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-338/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes contracts, files, ordering, math, and proof requirements. There is no new HTTP public contract. `AGENTS.md` is not updated: it is outside the feature directory and outside the named file set.

### Source Code (repository root)

```text
src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs
src/LamuFlix.Core/Features/Recommendations/SimilarityResult.cs
src/LamuFlix.Core/Features/Recommendations/Reason.cs

tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerPropertyTests.cs
tests/LamuFlix.UnitTests/Features/Recommendations/ReasonTests.cs   # optional
```

**Structure Decision**: Existing `LamuFlix.Core` and `LamuFlix.UnitTests` only. Recon records Recommendations production folder absent at base HEAD; it is created by these files (and/or by merged predecessors). No other path is in scope. If the contract cannot be implemented inside this set, stop for a decision. Do not widen the set. Do not implement or stub `MovieFeatures` / `GenreWeights` here.

## Design Overview

1. **Phase B pickup (before production edits).** Wisp reconfirms Epic 7 plus merged DEV-336 and DEV-337 on main and records landed `MovieFeatures` / `GenreWeights` signatures (including how genre sets are passed into GenreWeights similarity). Gauge owns baseline gates. Keel owns spec-drift analysis. Builder does not search for missing contracts or substitute predecessors. Missing dependencies block build, not this Phase A plan.

2. **Result and Reason records.** `public sealed record Reason(string Text)` and `public sealed record SimilarityResult(double Score, ImmutableList<Reason> Reasons)` beside `public static class SimilarityScorer` in `LamuFlix.Core.Features.Recommendations`.

3. **Guards.** `ArgumentNullException` for null `a` / `b` / `weights` / `genreWeights`. `ArgumentOutOfRangeException` naming `weights` for any negative or nonfinite weight among the seven. Finite `[0.0, double.MaxValue]` remains valid.

4. **Eligibility and overlap (Q2).** Set terms eligible only when both sets nonempty; scalars when both non-null. Directors/Writers/Cast: unweighted Jaccard under OrdinalIgnoreCase. Genres: delegate to landed GenreWeights contract (no second formula). Disjoint nonempty sets: `s = 0`, weight retained. No eligible terms → 0 / empty Reasons outside Q1.

5. **Proximity and rating (Q3).** `const double` year k = 0.1 / year, runtime k = 0.01 / minute. Cast scalars to double before absolute difference. `Math.Exp(-k * distance)`. Rating: `1 - |rA - rB| / 10`. Apply existing `RatingBoost` as that term's weight only.

6. **Stable normalization (Q3, Q5).** Over terms with data: divide eligible weights by `maxW` (handle max zero before dividing), accumulate scaled numerator/denominator, clamp to [0, 1]. If every eligible positive-weight term has `s_i == 1.0`, return exactly `1.0`. Q1 zero-denominator identity vs mismatch as ruled.

7. **Seven-field identity (Q5).** Compare Directors, Writers, Cast, Genres via null-safe OrdinalIgnoreCase set equality; ReleaseYear, Runtime, ImdbRating exact scalar equality. Ignore MetaScore, FullPlot, IDs, reference equality. Both fully empty profiles → 0 / empty Reasons.

8. **Reasons (Q4, Q9).** One Reason per eligible `w > 0` and `s > 0`, fixed order matching SimilarityWeights property order. Shared-token spelling and sort as ruled. Sanitize display only. Labels and fallbacks from `ASSUMPTIONS.md`. One-way `Score > 0 => Reasons nonempty`. Per-term emission independent of normalized underflow. Q1 `Identical feature profile` is the sole synthetic identity Reason.

9. **Proof order (brief task ordering / Q7).** (1) Dependency pickup. (2) Records, guards, set overlap. (3) Proximity, normalization, identity boundaries. (4) Deterministic Reasons. (5) Focused unit examples and tagged FsCheck properties alongside; all relevant tests green before handoff. (6) Whole-diff refactor (Cog) then Gauge size:M gates.

10. **Gates (Phase B).** Roslyn, complexity (refactor threshold from harness), JetBrains changed-diff inspection without `-All`, property tests, vulnerable packages, format verification, `dotnet test`, mutation on changed Core via `scripts/run-mutation.ps1` without `--since` in worktrees, web gate reporting actual applicability. Native exits only; `SKIPPED (scope-empty)`, configured `SKIP`, and mutation `NOT APPLICABLE` are never PASS. Exit 1 or Could not run blocks. No separate architect/Gherkin stage.

## Loop discipline

- **Closing bar:** In-scope Critical/High block. Medium/Low are recorded nonblocking unless they demonstrate a failed explicit AC (then classify by actual impact). No gate failure is waived by review severity. Size:M requires Sentry, Ledger, and Compass; a missing axis blocks adjudication.
- **Round cap:** Two plan-challenge rounds; two build review/remediation rounds, at most two fix commits per round. Unresolved above-bar findings at cap block; no third round, lowered threshold, or silent scope growth. Findings and summary must be posted on the PR.
- **Frozen scope:** Ticket pure seven-term scorer; immutable `SimilarityResult` and `Reason`; deterministic caller-safe explanations; Q1–Q9 math/input boundaries; focused unit/property tests; approved file set.
- **Exclusions:** Recommendation orchestration, candidates, storage, caching, endpoints, UI chips, configuration edits, predecessor implementations, tuning studies, dependencies, schema, unrelated cleanup; anything else is a follow-up issue, not a finding in this round. Apply charter follow-up policy for excluded concerns.

## Complexity Tracking

No constitution violation requires tracking. Patron recorded no constitution departure and no §2.3 escalation (brief Q1–Q9).
