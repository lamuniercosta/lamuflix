# DEV-294 Spec — Movie Aggregate Root, Domain State Machine, and Sealed Record Value Objects

**Status:** Phase A, owner checkboxes answered YES 2026-09-26 (CONCLUSIONS.md:165-168). Gate 1 opens when the user merges the spec PR.  
**Ticket:** DEV-294, size L, parent DEV-282  
**Scope:** `src/LamuFlix.Core/Domain/` + `tests/LamuFlix.Test/Domain/`  
**Branch:** `feature/DEV-294-implement-movie-aggregate-root-domain-state-machine-and-seal`  
**Base:** `origin/main`

---

## Purpose

Implement a domain-driven Movie aggregate root with a finite-state machine for enrichment workflows and sealed-record value objects. This establishes LamuFlix.Core as the authoritative domain model, separate from the EF `Data.Models.Movie` entity. The aggregate enforces invariants through its constructor and transition methods; value objects validate their constraints during construction.

---

## Acceptance Criteria (Gate 2 readiness)

1. **100% branch coverage on the state machine**: Exhaustive test theory matrix covering 20 cases (4 enrichment states × 4 enrichment actions = 16, plus 2 watchlist states × 2 watchlist actions = 4). Each case verifies either the legal result (including all tracking fields per the transition table) or the exception throw with no mutations. Evidenced by the test suite + Stryker (threshold 80%, never lowered).

2. **All value-object boundary tests pass**: Each sealed record validates both sides of every rule in the value-object spec below, including `MediaFormat` normalization, and `ReleaseYear` bounds relative to supplied `now`. The `with`-bypass guarantee (no `init` on `Value` properties) is evidenced by a reflection test asserting that every value-object property has `SetMethod == null`.

3. **Architecture tests remain green**: `LamuFlix.Core` must not reference EF Core, Npgsql, RabbitMQ, or Infrastructure. Verified by `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:24-48`.

4. Run the 10 gates in the Phase B Gate Sequence below, sequentially.

---

## Deliverables

### Domain Types (12 files, ~250 lines total)

All files are `src/LamuFlix.Core/Domain/<Type>.cs`, flat structure. Namespace: `LamuFlix.Core.Domain`.

| File | Type | Purpose | Lines |
|---|---|---|---|
| `EnrichmentStatus.cs` | `public enum` | `Pending=0, Enriched=1, NotFound=2, Failed=3` | 5 |
| `EnrichmentFailureCategory.cs` | `public enum` | `Unknown=0, ProviderUnavailable=1, RateLimited=2, InvalidResponse=3` | 5 |
| `InvalidTransitionException.cs` | `public sealed class` | Carries action name and current state; inherits `InvalidOperationException`; domain pattern per ADR 0014; mapped to HTTP 409 in follow-up #2 | 20 |
| `MovieMetadata.cs` | `public sealed record` | Value carrier for enriched metadata: `Title` (non-blank), `Synopsis?`, `ReleaseYear?`, `Runtime?`, `ImdbRating?`, `ImdbId?`. No `TryCreate` factory (validate via constructor only). | 8 |
| `MovieId.cs` | `public sealed record` | `Value` (int) > 0; includes `TryCreate` factory | 15 |
| `ImdbId.cs` | `public sealed record` | `Value` (string) matches `^tt\d{7,8}$` via `[GeneratedRegex]`; includes `TryCreate` factory | 20 |
| `ImdbRating.cs` | `public sealed record` | `Value` (decimal) 0.0–10.0 inclusive, value == decimal.Round(value, 1) (numeric, not scale; D8); includes `TryCreate` factory | 20 |
| `Runtime.cs` | `public sealed record` | `Minutes` (int) > 0; includes `TryCreate` factory | 10 |
| `ReleaseYear.cs` | `public sealed record` | `Value` (int); explicit constructor (not a positional record) `ReleaseYear(int value, DateTimeOffset now)` validates `1888 ≤ value ≤ now.Year + 5`, equality uses `Value` only, `now` is parameter only, never a property (D12); includes `TryCreate` factory | 20 |
| `LibraryPath.cs` | `public sealed record` | `Value` (string) non-blank, rejects literal `..` substring; includes `TryCreate` factory | 15 |
| `MediaFormat.cs` | `public sealed record` | `Extension` (string) non-blank; trimmed, exactly one leading `.` stripped, lowercased; includes `TryCreate` factory | 15 |
| `Movie.cs` | `public sealed class` | Aggregate root: `Id`, `Title`, `Path`, `Format`, `IsInWatchlist`, `Metadata?`, plus 5 tracking fields. Private setters. Static `Create(...)` factory. State-transition methods. | 100 |

**Subtotal:** 12 files, ~250 lines.

### Test Project Reference (1 edit)

- `tests/LamuFlix.Test/LamuFlix.Test.csproj`: Add one `<ProjectReference>` to `src/LamuFlix.Core/LamuFlix.Core.csproj`.

### Test Suite (1+ files, ~350+ lines)

- `tests/LamuFlix.Test/Domain/MovieTests.cs`: State machine exhaustive theory matrix (20 cases: 16 enrichment + 4 watchlist), tracking field assertions, mutation verification on throws.
- `tests/LamuFlix.Test/Domain/ValueObjectTests.cs`: Boundary tests for each value object (valid and invalid per `TryCreate`), `with`-bypass guard, `ReleaseYear` bounds checks.
- Possible per-type test files (e.g., `ImdbIdTests.cs`, `MediaFormatTests.cs`) — detail deferred to Phase B.

**Subtotal:** 350+ lines of test code.

### Acceptance Criteria Evidence

- **Branch coverage**: Test theory matrix + Stryker mutation score ≥ 80%.
- **No coverage collector**: Tests reference no code-coverage package (adding one is §2.3 #1, not authorized).

---

## Constraints & Rules

### Architectural
- **Core isolation**: Core must not reference EF Core, Npgsql, RabbitMQ, or Infrastructure. Verified by ArchitectureTests.
- **Time handling**: Never read a clock. All methods accepting date/time take `DateTimeOffset now` as a parameter. `DateTime.Now` / `UtcNow` are banned (BannedSymbols.txt / RS0030).
- **Value-object invariants**: Enforced during construction. Invalid input throws `ArgumentException` family. The `with` operator cannot bypass validation (no `init` on `Value` property).

### Complexity
- Keep each transition method small with guard clauses and early returns. Cyclomatic complexity must stay ≤15 (implement threshold) and ≤6 (refactor threshold).

### Naming & Duplication
- `Core.Domain.Movie` and `Data.Models.Movie` share a simple name. Test files needing both use qualified names or aliases. No production code changes to support naming.

### State Machine Semantics
- **Legal transitions (only from Pending)**:
  - `MarkEnriched(metadata, now)` → Enriched. Increments attempts, sets `EnrichedAt`, `Metadata`, clears `LastFailureCategory`.
  - `MarkNotFound(now)` → NotFound. Increments attempts, common fields only.
  - `MarkFailed(category, now)` → Failed. Increments attempts, sets `LastFailureCategory`.
- **Legal transitions (from Enriched, NotFound, Failed)**:
  - `RequestEnrichment()` → Pending. Keeps attempts (monotonic), `EnrichedAt`, `Metadata`, `LastFailureCategory` as history. Throws from Pending.
- **Watchlist** (independent of Status):
  - `AddToWatchlist()` throws when already in watchlist.
  - `RemoveFromWatchlist()` throws when absent.
  - Differs intentionally from idempotent `MovieService` methods (not wired here).

---

## Out of Scope (Frozen Boundary)

- No migration, no `LamuFlixContext` change, no rehydration factory.
- No edits to `Data.Models.Movie`, `MovieEnrichmentStatus`, `EnrichmentJobProcessor`, or `MovieService`.
- No new NuGet packages except FsCheck (D9).
- ADR 0014 ("Core Movie Aggregate Separate from EF Entity") is drafted by Keel in a separate Phase A step after this brief.

**Follow-ups filed by Rigger** (not in scope):
1. Persistence, rehydration, and adoption (parent DEV-282, size L).
2. HTTP 409 mapping at API boundary and IExceptionHandler wiring (PR gate: new exception types are mapped in the single IExceptionHandler). No IExceptionHandler implementation exists in `src/` at recon HEAD (recon-DEV-294 §10a, Q8); creating one would add a new API-boundary component outside frozen scope, deferred to this follow-up.

---

## Phase Overview

### Phase A (specs only, this brief)
1. ✅ Brief, CONCLUSIONS.md, task note.
2. Keel drafts ADR 0014 (separate step).
3. Quill drafts spec.md, plan.md, tasks.md.
4. Keel runs read-only `/speckit-analyze`, reports fix list.
5. Plan challenge and adjudication, freeze.
6. Spec PR carries the two owner checkboxes, answered YES.

### Phase B (implementation, after Gate 1 + user merge)
1. Verify worktree and branch.
2. Add value objects and enums, then exceptions, metadata, aggregate, csproj reference, tests.
3. Build and test immediately.
4. Run the 10 gates in the Phase B Gate Sequence below, sequentially.
5. Open delivery PR.

---

## Owner Checkboxes (answered YES 2026-09-26)

- [x] **Q10(b), §2.3 #1: Add FsCheck as a NuGet dependency** so property tests can cover the state machine and value objects?  
  Patron ruling: `blocked: structural`. FsCheck is a new NuGet dependency (§2.3 #1). The refactor gate's property-tests precondition (harness.yml:31-35) prohibits opt-outs for tickets with domain invariants, and this ticket has them. **YES** authorizes FsCheck and property tests (frozen Phase B scope). **NO** requires the team to re-decide gate satisfaction. **Owner answer: YES** (CONCLUSIONS.md:165-168).

- [x] **Q12, §2.3 #6: Extend CONTEXT.md glossary** (Enrichment Status, Watchlist, add Movie Aggregate and Enrichment Attempt)?  
  Conductor steer: The ticket does not name `CONTEXT.md`. The glossary extension is a §2.3 #6 checkpoint (editing a file the ticket does not name). **YES** authorizes the edits. **NO** → no glossary change. Consequence (D5): constitution VIII requires new domain terms in CONTEXT.md in the same PR, so a NO leaves the delivery PR in conflict with constitution VIII. **Owner answer: YES** (CONCLUSIONS.md:165-168).

Owner checkboxes answered YES 2026-09-26 (CONCLUSIONS.md:165-168). Gate 1 opens when the user merges the spec PR.

---

## Checkbox Summary

- **Owner checkboxes:** 2, answered YES (non-blocking)
- **Task checkboxes (not blocking):** 0 (Q1 is not a checkbox; it is a ruling.)

---

## Test Strategy & Gate Expectations

### Frameworks
- xUnit v3 (CPM 4.0.1)
- Shouldly (CPM 4.3.0)
- FsCheck (base package, D9; version pinned at Phase B)
- No Testcontainers, no Moq, no NSubstitute (not needed for pure domain logic).

### Coverage Approach
- **Branch coverage AC**: Exhaustive state-machine theory (4×4 + 2×2 = 20 cases), each asserting mutations or throw with no change.
- **Mutation testing**: Stryker with threshold 80% (never lowered). No coverage report attached (no coverage package authorized).
- **Property tests**: FsCheck, required (D9, D10); `run-property-tests` must exit 0.

### Phase B Gate Sequence (all must exit 0, see plan.md for details)
1. `./scripts/run-roslyn-analyzers.ps1` (CA/IDE warning+)
2. `./scripts/run-cyclomatic-complexity.ps1` (≤15 implement threshold)
3. `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` (≤6 refactor threshold)
4. `./scripts/run-jetbrains-inspectcode.ps1`
5. `./scripts/run-vulnerable-packages.ps1` (no deprecated packages)
6. `dotnet format --verify-no-changes`
7. `dotnet build`
8. `dotnet test` (including ArchitectureTests)
9. `./scripts/run-property-tests.ps1 -Project tests/LamuFlix.Test/LamuFlix.Test.csproj` (must exit 0; exit 2 SKIPPED is a failure)
10. `dotnet stryker` (≥80%)

**Exit codes:** 0 = Pass, 1 = Fail, 2 = SKIPPED (never counts as pass).

---

## Closing & Review

- **Severity bar**: Critical and High findings block. Medium and Low become follow-ups with severity intact (precedent: DEV-291).
- **Round cap**: 2 formal review rounds, max 2 fix commits per round. A 3rd round escalates to the user.
- **Ship review**: `/ship-review` runs (size L ticket). Requires Sentry, Ledger, and Compass axis reports.

---

## File Manifest (Phase B deliverables)

- `src/LamuFlix.Core/Domain/EnrichmentStatus.cs`
- `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs`
- `src/LamuFlix.Core/Domain/InvalidTransitionException.cs`
- `src/LamuFlix.Core/Domain/MovieMetadata.cs`
- `src/LamuFlix.Core/Domain/MovieId.cs`
- `src/LamuFlix.Core/Domain/ImdbId.cs`
- `src/LamuFlix.Core/Domain/ImdbRating.cs`
- `src/LamuFlix.Core/Domain/Runtime.cs`
- `src/LamuFlix.Core/Domain/ReleaseYear.cs`
- `src/LamuFlix.Core/Domain/LibraryPath.cs`
- `src/LamuFlix.Core/Domain/MediaFormat.cs`
- `src/LamuFlix.Core/Domain/Movie.cs`
- `tests/LamuFlix.Test/LamuFlix.Test.csproj` (add Core ProjectReference + FsCheck PackageReference)
- `tests/LamuFlix.Test/Domain/MovieTests.cs`
- `tests/LamuFlix.Test/Domain/ValueObjectTests.cs` (or per-type files)
- `tests/LamuFlix.Test/Domain/PropertyTests.cs` (FsCheck property tests)
- `Directory.Packages.props` (add FsCheck PackageVersion)
- `CONTEXT.md` (extend Enrichment Status, Watchlist; add Movie Aggregate, Enrichment Attempt)

**Total files:** 15 new files + 3 edits  
**Total lines (target):** ~250 (domain: 12 files) + 350+ (tests) + 100 (property tests) = 700+ lines
