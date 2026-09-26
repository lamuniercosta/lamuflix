# DEV-294 Plan — Implementation Sequence and Gate Strategy

**Status:** Phase A, frozen at plan challenge  
**Ticket:** DEV-294, size L  
**Effort estimate:** ~8–12 hours  
**Entry criteria:** Spec PR merged by user (Gate 1)

---

## Implementation Sequence (Phase B, worktree-only)

Each commit follows the format: `DEV-294 - {subject}`

### Step 1: Value Objects and Enums (Commit 1)

7 sealed records with validating constructors and 2 enums, with no dependencies on each other.

1. **`EnrichmentStatus.cs`** — Enum (Pending=0, Enriched=1, NotFound=2, Failed=3). ~5 lines.
2. **`EnrichmentFailureCategory.cs`** — Enum (Unknown=0, ProviderUnavailable=1, RateLimited=2, InvalidResponse=3). ~5 lines.
3. **`MovieId.cs`** — Sealed record: `Value` (int) > 0; includes `TryCreate(int value, out MovieId? result)` factory with valid test (> 0) and invalid test (≤ 0). ~15 lines.
4. **`ImdbId.cs`** — Sealed record: `Value` (string) matches `^tt\d{7,8}$` via `[GeneratedRegex]`; includes `TryCreate` factory with valid test (regex match) and invalid test (wrong format). ~20 lines.
5. **`ImdbRating.cs`** — Sealed record: `Value` (decimal) 0.0–10.0 inclusive, value == decimal.Round(value, 1) (numeric, not scale; D8); includes `TryCreate` factory with valid test (0.0, 10.0, 5.5, 5.50, 10.00) and invalid test (> 10, < 0, 5.55, 10.05). ~20 lines.
6. **`Runtime.cs`** — Sealed record: `Minutes` (int) > 0; includes `TryCreate` factory with valid test (> 0) and invalid test (≤ 0). ~10 lines.
7. **`ReleaseYear.cs`** — Sealed record: `Value` (int); explicit constructor (not a positional record) `(int value, DateTimeOffset now)` validates `1888 ≤ value ≤ now.Year + 5`, equality on `Value` only; `now` is a parameter only, never a property or field (D12); includes `TryCreate(int, DateTimeOffset, out ReleaseYear?)` factory with valid test (1888, now.Year + 5) and invalid test (before/after bounds). ~20 lines.
8. **`LibraryPath.cs`** — Sealed record: `Value` (string) non-blank, rejects literal `..` substring; includes `TryCreate` factory with valid test (non-blank, no `..`) and invalid test (`..`, `a/../b`, `a..b`). ~15 lines.
9. **`MediaFormat.cs`** — Sealed record: `Extension` (string) non-blank; trimmed, exactly one leading `.` stripped, lowercased; includes `TryCreate` factory with valid test (normalization) and invalid test (blank, invalid normalization). ~15 lines.

**Total: ~125 lines, 9 files.**

### Step 2: Exceptions and Metadata (Commit 2)

1. **`InvalidTransitionException.cs`** — Public sealed class inheriting `InvalidOperationException`. Carries action name and current state as a string. Standard constructors. Domain pattern per ADR 0014. HTTP 409 mapping deferred to follow-up #2. ~20 lines.
2. **`MovieMetadata.cs`** — Sealed record: `Title` (non-blank), `Synopsis?`, `ReleaseYear?`, `Runtime?`, `ImdbRating?`, `ImdbId?`. Uses the value objects from Step 1. ~8 lines.

**Total: ~28 lines, 2 files.**

### Step 3: Movie Aggregate (Commit 3)

1. **`Movie.cs`** — Public sealed class, aggregate root.
   - **Constructor**: Static factory `static Movie Create(MovieId id, string title, LibraryPath path, MediaFormat format)` returns instance with `Status = Pending`, `EnrichmentAttempts = 0`, `IsInWatchlist = false`, all nullables null.
   - **Properties**: `Id` (MovieId), `Title` (string), `Path` (LibraryPath), `Format` (MediaFormat), `IsInWatchlist` (bool), `Metadata` (MovieMetadata?), `Status` (EnrichmentStatus), `EnrichedAt` (DateTimeOffset?), `EnrichmentAttempts` (int), `LastFailureCategory` (EnrichmentFailureCategory?), `LastAttemptAt` (DateTimeOffset?). All with private setters.
   - **Transition methods**:
     - `MarkEnriched(MovieMetadata metadata, DateTimeOffset now)`: Guard `Status == Pending`, else throw. Increment attempts, set `EnrichedAt = now`, `Metadata = metadata`, `LastAttemptAt = now`, clear `LastFailureCategory`, set `Status = Enriched`. ~6 lines.
     - `MarkNotFound(DateTimeOffset now)`: Guard `Status == Pending`, else throw. Increment attempts, set `LastAttemptAt = now`, set `Status = NotFound`. ~4 lines.
     - `MarkFailed(EnrichmentFailureCategory category, DateTimeOffset now)`: Guard `Status == Pending`, else throw. Increment attempts, set `LastFailureCategory = category`, set `LastAttemptAt = now`, set `Status = Failed`. ~5 lines.
     - `RequestEnrichment()`: Guard `Status != Pending`, else throw. Set `Status = Pending`. Keep attempts, `EnrichedAt`, `Metadata`, `LastFailureCategory` as history. ~4 lines.
     - `AddToWatchlist()`: Guard `!IsInWatchlist`, else throw. Set `IsInWatchlist = true`. ~3 lines.
     - `RemoveFromWatchlist()`: Guard `IsInWatchlist`, else throw. Set `IsInWatchlist = false`. ~3 lines.

   **Total: ~100 lines.**

**Total: ~100 lines, 1 file.**

### Step 4: Test Project Reference (Commit 4)

1. **`tests/LamuFlix.Test/LamuFlix.Test.csproj`** — Add one `<ProjectReference>`:
   ```xml
   <ProjectReference Include="..\..\src\LamuFlix.Core\LamuFlix.Core.csproj" />
   ```

**Total: 1 line (1 edit to existing file).**

### Step 5: Tests (Commit 5)

Create exhaustive test coverage in `tests/LamuFlix.Test/Domain/`:

1. **`MovieTests.cs`** — State machine exhaustive theory matrix.
   - Theory: 4 enrichment states (Pending, Enriched, NotFound, Failed) × 4 enrichment actions (MarkEnriched, MarkNotFound, MarkFailed, RequestEnrichment) = 16 cases.
     - Each case where action is legal: verify result state, verify all tracking fields per transition table, verify no throw.
     - Each case where action is illegal: verify throw `InvalidTransitionException`, verify no mutations.
   - Theory: 2 watchlist states (InWatchlist, NotInWatchlist) × 2 watchlist actions (Add, Remove) = 4 cases.
     - Each case where action is legal: verify new state, verify no throw.
     - Each case where action is illegal: verify throw, verify no mutations.
   - Additional tests: `Create` factory initialization, tracking fields initialization, `RequestEnrichment` preserves history, Movie.Create blank/whitespace title throws, MovieMetadata blank title throws.
   - **Total: ~250 lines, 40+ test cases.**

2. **`ValueObjectTests.cs`** (or per-type files):
   - `MovieId`: Valid (> 0) and invalid (≤ 0) TryCreate tests.
   - `ImdbId`: Valid (`tt1234567`, `tt12345678`) and invalid (`tt123456`, `tt123456789`) TryCreate tests.
   - `ImdbRating`: Valid (0.0, 10.0, 5.5, 5.50, 10.00) and invalid (> 10.0, < 0.0, 5.55, 10.05) TryCreate tests per D8 predicate.
   - `Runtime`: Valid (> 0) and invalid (≤ 0) TryCreate tests.
   - `ReleaseYear`: Valid (1888, current+5 based on supplied `now`) and invalid (before 1888, after current+5) TryCreate tests. Equality holds across different `now` values (D12).
   - `LibraryPath`: Valid (non-blank, no `..`) and invalid (`..`, `a/../b`, `a..b`) TryCreate tests.
   - `MediaFormat`: Valid (normalization: trim, strip leading `.`, lowercase) and invalid (blank, invalid normalization) TryCreate tests.
   - `with`-bypass guarantee: A reflection test asserts that every value-object property has `SetMethod == null`, confirming no `init` modifier allows `with` to mutate.
   - **Total: ~100 lines, 30+ assertions.**

**Total: ~350 lines, 1–2 files.**

### Step 5b: Property Tests (FsCheck, D9)

Add FsCheck to CPM (`Directory.Packages.props`) with a stable version compatible with net10.0. Add FsCheck reference to `tests/LamuFlix.Test/LamuFlix.Test.csproj`. Write property tests in `tests/LamuFlix.Test/Domain/PropertyTests.cs` using FsCheck to cover all value-object invariants and the state transition table. Each property is a plain `[Fact]` method tagged `[Trait("Category", "Property")]` so that FsCheck failures propagate to xUnit. Tests use the ImdbRating predicate from D8.

**Total:** ~100 lines.

### Step 6: Verify Build & Tests

```powershell
dotnet build
dotnet test --filter "FullyQualifiedName~LamuFlix.Test.Domain"
```

Confirm all tests pass, architecture tests remain green.

### Step 7: CONTEXT.md Glossary (Commit 6)

1. **`CONTEXT.md`** — Extend existing entries and add new ones:
   - Extend **Enrichment Status** (`CONTEXT.md:27-29`): add the four enum values (Pending, Enriched, NotFound, Failed).
   - Extend **Watchlist** (`CONTEXT.md:38`): clarify that it is independent of enrichment status and controlled by `AddToWatchlist()` / `RemoveFromWatchlist()`.
   - Add **Movie Aggregate**: describe as the domain model root for enrichment workflows, with invariants enforced via transition methods.
   - Add **Enrichment Attempt**: counter tracking the number of enrichment calls, monotonic and never reset.

**Total: 1 edit to existing file.**

---

## Constitution Check

This specification addresses the following constitution principles and PR gates:

- **Principle VII (Time)**: `DateTimeOffset now` is passed to all methods that need a timestamp; no clock is read (banned by BannedSymbols.txt).
- **Principle VIII (CONTEXT.md)**: New domain terms (Movie Aggregate, Enrichment Attempt) and updated terms (Enrichment Status, Watchlist) require CONTEXT.md edits in scope (Commit 6, CONCLUSIONS:168).
- **Principle IX (Testing)**: xUnit v3 and Shouldly are used (CPM 4.0.1, 4.3.0). Hand-written test builders for aggregate initialization are permitted (Known Technical Debt: LamuFlix.Test). Stryker mutation score ≥ 80% on Core.Domain.*.
- **Coding Conventions**: All types are sealed (`sealed class`, `sealed record`). No XML comments. Each value object has a `TryCreate` factory alongside its throwing constructor.
- **PR Gate: Constitution VIII**: New domain types (Movie, enums, exceptions, value objects) require CONTEXT.md entries in scope (Commit 6, CONCLUSIONS:168).
- **PR Gate: Sealed types**: All aggregate, exception, and value-object types are sealed. Enforced by ArchitectureTests.
- **PR Gate: Core isolation**: Core must not reference EF Core, Npgsql, RabbitMQ, or Infrastructure. Enforced by ArchitectureTests.cs:24-48.

### Recorded Deviation (PR Gate: IExceptionHandler Mapping)

The PR gate "new exception types are mapped in the single IExceptionHandler" cannot be satisfied in DEV-294: no `IExceptionHandler` implementation exists in `src/` at recon HEAD (recon-DEV-294 §10a, Q8 ruling). Creating one would add a new API-boundary component and edit unnamed API files, which is outside frozen scope. This gate line is recorded as **not met in DEV-294** and deferred to follow-up #2. The deviation is documented and does not block this spec.

---

## Phase B Gate Sequence

After implementation is complete and all code committed, run the following gates **sequentially** (each must exit 0 before proceeding):

### 1. Roslyn Analyzers
```powershell
./scripts/run-roslyn-analyzers.ps1
```
**Acceptance:** Exit 0 (no CA/IDE warnings on changed `.cs` files vs. main).

### 2. Cyclomatic Complexity (Implementation Threshold)
```powershell
./scripts/run-cyclomatic-complexity.ps1
```
**Acceptance:** Exit 0 (no methods exceed 15).

### 3. Cyclomatic Complexity (Refactor Threshold)
```powershell
./scripts/run-cyclomatic-complexity.ps1 -Threshold 6
```
**Acceptance:** Exit 0 (no methods exceed 6). *Refactor large methods using guard clauses, early returns, and helper extraction if needed.*

### 4. JetBrains InspectCode
```powershell
./scripts/run-jetbrains-inspectcode.ps1
```
**Acceptance:** Exit 0 (no ReSharper warnings on changed files).

### 5. Vulnerable Packages
```powershell
./scripts/run-vulnerable-packages.ps1
```
**Acceptance:** Exit 0 (no deprecated or vulnerable packages).

### 6. Format Check
```powershell
dotnet format --verify-no-changes
```
**Acceptance:** Exit 0 (no format violations).

### 7. Build
```powershell
dotnet build
```
**Acceptance:** Exit 0, zero warnings (TreatWarningsAsErrors=true).

### 8. Test Suite
```powershell
dotnet test
```
**Acceptance:** Exit 0, all tests pass (including ArchitectureTests; Core must not reference EF, Npgsql, RabbitMQ, Infrastructure).

### 9. Property Tests
```powershell
./scripts/run-property-tests.ps1 -Project tests/LamuFlix.Test/LamuFlix.Test.csproj
```
**Acceptance:** Exit 0; exit 2 SKIPPED is a failure.

### 10. Stryker Mutation Testing (Pre-PR)
```powershell
dotnet stryker
```
**Acceptance:** Exit 0, mutation score ≥ 80% (never lowered). **Stop condition**: If the Stryker report mutates files outside `src/LamuFlix.Core/Domain/`, or misses Core Domain entirely, or cannot run, stop and report to Keel.

---

## Review & Merge Strategy

### Review Rounds
- **Round 1**: `/ship-review` runs (size L ticket). Sentry, Ledger, and Compass axis reports required. At most 2 fix commits per round.
- **Round 2**: Addressed findings (if any) and re-verification. At most 2 fix commits per round.
- **Escalation**: A third review round escalates to the user (not standard).

### Severity Classification
- **Critical or High**: Must fix and re-verify before PR.
- **Medium or Low**: Filed as follow-up issues (precedent: DEV-291). Included in findings summary but do not block merge.

### Follow-ups (Rigger records; not in scope)
1. **Persistence & Adoption** (parent DEV-282, size L, estimate 5):
   - EF migration or mapping for tracking fields.
   - Rehydration factory for aggregate.
   - Wiring into `EnrichmentJobProcessor` and `MovieService`.

2. **HTTP 409 Mapping** (Medium):
   - PR gate: new exception types are mapped in the single IExceptionHandler. Create IExceptionHandler and wire `InvalidTransitionException` to HTTP 409 at API boundary. No IExceptionHandler exists in `src/` at recon HEAD (recon-DEV-294 §10a, Q8); creating one is outside frozen scope.

---

## Commit Message Format

All commits in Phase B follow:
```
DEV-294 - {subject}

{optional body}
```

Include Co-Authored-By attribution in the committing session's standard format. Example:
```
DEV-294 - Add Movie aggregate root with state machine and enrichment tracking

Implements a sealed class Movie with private setters and state-transition methods
for enrichment workflows. Includes five tracking fields (Status, EnrichedAt,
EnrichmentAttempts, LastFailureCategory, LastAttemptAt).
```

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Architecture test failure (Core references prohibited layer) | Verify no `using` statements for EF, Npgsql, RabbitMQ, Infrastructure. Architecture tests run before PR. |
| Cyclomatic complexity exceeds threshold | Design transition methods with guard clauses and early returns; extract large methods. Target complexity stays ≤6. |
| `with` operator bypasses value-object validation | Use sealed record with get-only `Value` property (no `init`). Reflection test verifies bypass is impossible. |
| Test matrix incomplete (missing edge case) | Enumerate all (state, action) pairs exhaustively. Theory matrix is 16 + 4 = 20 cases minimum; each with throw + no-throw variants. |
| Mutation score < 80% | Add tests that exercise boundary conditions and error paths. Stryker reports survive any mutation. |

---

## Success Criteria (Phase B readiness)

- ✅ All 12 domain types created and committed.
- ✅ Test project reference added.
- ✅ 350+ lines of test code, 40+ test cases covering state machine and value objects.
- ✅ All 10 gates pass sequentially.
- ✅ Mutation score ≥ 80%.
- ✅ Ship review completed (Sentry, Ledger, Compass).
- ✅ Follow-up issues filed (Rigger records).
- ✅ PR merged, main checkout remains clean.

---

## Timeline (Estimate)

| Phase | Duration | Notes |
|---|---|---|
| Steps 1–2 | ~1–2 hours | Enums, value objects, exceptions, metadata (straightforward validation). |
| Step 3 | ~1–2 hours | Movie aggregate (transition logic, tracking fields). |
| Step 4 | ~10 min | Add ProjectReference line. |
| Step 5 | ~3–4 hours | Exhaustive test matrix, value-object boundaries, edge cases. |
| Step 6 | ~20 min | Build and smoke test. |
| Gates | ~1–2 hours | Run all 10 gates sequentially. Complexity refactoring if needed. |
| Review & follow-ups | ~1–2 hours | Ship review, address findings, file follow-ups. |
| **Total** | **~8–12 hours** | Estimate for one developer end-to-end. |
