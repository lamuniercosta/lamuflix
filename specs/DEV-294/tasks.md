# DEV-294 Tasks — Work Breakdown and Progress Tracking

**Status:** Phase A (owner checkboxes answered YES 2026-09-26; CONCLUSIONS.md:165-168). Gate 1 opens when the user merges the spec PR.  
**Ticket:** DEV-294, size L  
**Sequence:** Phase A (specs only) → Phase B (implementation, post-Gate 1)

---

## Owner Checkboxes (answered YES 2026-09-26)

Owner checkboxes answered YES 2026-09-26 (CONCLUSIONS.md:165-168). Gate 1 opens when the user merges the spec PR.

- [x] **Q10(b), §2.3 #1: Add FsCheck NuGet dependency**  
  Should FsCheck be added to `Directory.Packages.props` to enable property tests for the state machine and value objects?  
  **YES** → authorize FsCheck and property tests as frozen Phase B scope.  
  **NO** → team re-decides property-tests gate satisfaction.  
  *Citation: `.cursor/rules/refactor-gate.mdc`, `harness.yml:31-35`, grill conclusion lines 112-122.*

- [x] **Q12, §2.3 #6: Extend CONTEXT.md glossary**  
  Should `CONTEXT.md` be extended to add "Movie Aggregate" and "Enrichment Attempt" terms, and update existing "Enrichment Status" and "Watchlist" entries?  
  **YES** → authorize glossary updates in Phase B.  
  **NO** → no glossary change. Consequence (D5): constitution VIII requires new domain terms in CONTEXT.md in the same PR, so a NO leaves the delivery PR in conflict with constitution VIII.  
  *Citation: Conductor steer (CONCLUSIONS.md:152-162), grill conclusion lines 136-142.*

---

## Phase A Tasks (Specifications Only)

**Ownership:** Quill (Writer)  
**Deadline:** Before spec PR merge  
**Blocking:** Owner checkboxes answered YES 2026-09-26 (CONCLUSIONS.md:165-168). Gate 1 opens when the user merges the spec PR.

### Task A1: Spec Document (spec.md)

- [x] Define purpose and scope.
- [x] List acceptance criteria (100% branch coverage on state machine; all value-object boundary tests; architecture tests green; all gates pass).
- [x] Document deliverables: 12 domain type files (~250 lines), test project reference, test suite (~350 lines).
- [x] Record constraints (Core isolation, time handling, value-object invariants, complexity limits, naming rules, state machine semantics).
- [x] Enumerate out-of-scope items and follow-ups.
- [x] Owner checkboxes carried, answered YES (CONCLUSIONS.md:165-168).

**Output:** `specs/DEV-294/spec.md`

### Task A2: Plan Document (plan.md)

- [x] Define implementation sequence (Step 1–6: value objects → exceptions → metadata → aggregate → csproj → tests).
- [x] Estimate lines per file (9 value objects + enums, 2 metadata/exceptions, 1 aggregate, tests).
- [x] Document Phase B gate sequence (10 gates: Roslyn, complexity 15/6, InspectCode, vulnerable-packages, format, build, test, property tests, Stryker).
- [x] Record review strategy (2 review rounds, /ship-review runs, severity bar, follow-ups).
- [x] Timeline estimate (~8–12 hours end-to-end).
- [x] Add Constitution Check section citing principles VII, VIII, IX and PR gates.

**Output:** `specs/DEV-294/plan.md`

### Task A3: Task Breakdown (tasks.md — this file)

- [x] List Phase A tasks (specs only).
- [x] Owner checkboxes carried, answered YES.
- [x] Outline Phase B work items (per plan.md steps).
- [x] Record acceptance criteria for each phase.
- [x] Document dependencies (Gate 1 opens at the user's merge of the spec PR, Phase B enters after spec PR merge).

**Output:** `specs/DEV-294/tasks.md`

### Task A4: Adjudication & Freeze

- [ ] Keel runs `/speckit-analyze` (read-only), reports fix list.
- [ ] Plan challenge: team discusses findings.
- [ ] Freeze: all spec changes locked, branch ready for spec PR.
- [ ] Spec PR carries both owner checkboxes.

---

## Phase B Tasks (Implementation, Post-Gate 1)

**Ownership:** Implementer (post-Gate 1 merge)  
**Dependency:** Spec PR merged by user (Gate 1)  
**Deadline:** After Phase B entry, ~8–12 hours elapsed time

### Commit 1: Value Objects and Enums

Create 7 sealed records and 1 enum in `src/LamuFlix.Core/Domain/` (consuming the DEV-295 EnrichmentFailureCategory sealed record already in Core):

- [ ] `EnrichmentStatus.cs` (5 lines): Enum `Pending=0, Enriched=1, NotFound=2, Failed=3`
- [ ] `EnrichmentFailureCategory.cs`: Sealed-record set from DEV-295 already in Core (`ProviderUnavailable`, `RateLimited`, `InvalidResponse`, `Unknown`) with properties `Code`, `SafeDescription`, `IsRetryable`
- [ ] `MovieId.cs` (15 lines): Sealed record `Value` (int) > 0; includes `TryCreate(int value, [NotNullWhen(true)] out MovieId? result)` factory
- [ ] `ImdbId.cs` (20 lines): Sealed record `Value` (string) matches `^tt\d{7,8}$`; includes `TryCreate` factory
- [ ] `ImdbRating.cs` (20 lines): Sealed record `Value` (decimal) 0.0–10.0 inclusive, value == decimal.Round(value, 1) (numeric, not scale; D8); includes `TryCreate` factory
- [ ] `Runtime.cs` (10 lines): Sealed record `Minutes` (int) > 0; includes `TryCreate` factory
- [ ] `ReleaseYear.cs` (20 lines): Sealed record `Value` (int), explicit constructor (not a positional record) `(int value, DateTimeOffset now)` validates `1888 ≤ value ≤ now.Year + 5`; `now` is a parameter only, never a property or field (D12); includes `TryCreate(int value, DateTimeOffset now, [NotNullWhen(true)] out ReleaseYear? result)` factory
- [ ] `LibraryPath.cs` (15 lines): Sealed record `Value` (string) non-blank, no `..` substring; includes `TryCreate` factory
- [ ] `MediaFormat.cs` (15 lines): Sealed record `Extension` (string) non-blank, trimmed, one leading `.` stripped, lowercased; includes `TryCreate` factory

**Subtotal:** ~125 lines  
**Exit criteria:** `dotnet build` succeeds, no CA/IDE warnings.

### Commit 2: Exceptions and Metadata

- [ ] `InvalidTransitionException.cs` (20 lines): Public sealed class, inherits `InvalidOperationException`, carries action name and state, standard constructors. Domain pattern per ADR 0014. HTTP 409 mapping deferred to follow-up #2.
- [ ] `MovieMetadata.cs` (8 lines): Sealed record, `Title` (non-blank), `Synopsis?`, `ReleaseYear?`, `Runtime?`, `ImdbRating?`, `ImdbId?`. No `TryCreate` factory (validate via constructor only).

**Subtotal:** ~28 lines  
**Exit criteria:** `dotnet build` succeeds.

### Commit 3: Movie Aggregate

- [ ] `Movie.cs` (100 lines): Sealed class aggregate root
  - [ ] Static factory: `Create(MovieId id, string title, LibraryPath path, MediaFormat format)`
  - [ ] Properties: `Id`, `Title`, `Path`, `Format`, `IsInWatchlist`, `Metadata?`, `Status`, `EnrichedAt?`, `EnrichmentAttempts`, `LastFailureCategory?`, `LastAttemptAt?` (all private setters)
  - [ ] Transition methods:
    - [ ] `MarkEnriched(MovieMetadata metadata, DateTimeOffset now)`: Guard Pending, increment attempts, set EnrichedAt/Metadata/LastAttemptAt, clear LastFailureCategory, Status → Enriched
    - [ ] `MarkNotFound(DateTimeOffset now)`: Guard Pending, increment attempts, set LastAttemptAt, Status → NotFound
    - [ ] `MarkFailed(EnrichmentFailureCategory category, DateTimeOffset now)`: Guard Pending, increment attempts, set LastFailureCategory/LastAttemptAt, Status → Failed
    - [ ] `RequestEnrichment()`: Guard not Pending, Status → Pending, keep attempts and history
    - [ ] `AddToWatchlist()`: Guard not already in, IsInWatchlist → true
    - [ ] `RemoveFromWatchlist()`: Guard already in, IsInWatchlist → false

**Subtotal:** ~100 lines  
**Exit criteria:** `dotnet build` succeeds, cyclomatic complexity ≤15 (implement) and ≤6 (refactor).

### Commit 4: Test Project Reference

- [ ] `tests/LamuFlix.Test/LamuFlix.Test.csproj`: Add one `<ProjectReference>` to `src/LamuFlix.Core/LamuFlix.Core.csproj`

**Subtotal:** 1 line  
**Exit criteria:** Project loads in IDE.

### Commit 5: Test Suite

Create exhaustive tests in `tests/LamuFlix.Test/Domain/`:

- [ ] `MovieTests.cs` (~250 lines):
  - [ ] State machine theory: 4 enrichment states × 4 enrichment actions = 16 cases
    - For each legal transition: assert result state, all tracking fields per transition table, no throw
    - For each illegal transition: assert throw `InvalidTransitionException`, no mutations
  - [ ] Watchlist theory: 2 states × 2 actions = 4 cases
    - For each legal action: assert new state, no throw
    - For each illegal action: assert throw, no mutations
  - [ ] Factory initialization tests
  - [ ] History preservation test (`RequestEnrichment` keeps EnrichedAt/Metadata/LastFailureCategory)

- [ ] `ValueObjectTests.cs` (or per-type files, ~100 lines):
  - [ ] `MovieId`: Valid and invalid TryCreate tests (> 0 vs ≤ 0), throwing constructor tests
  - [ ] `ImdbId`: Valid (`tt1234567`, `tt12345678`) and invalid (`tt123456`, `tt123456789`) TryCreate tests, throwing constructor tests
  - [ ] `ImdbRating`: Valid (0.0, 10.0, 5.5, 5.50m, 10.00m) and invalid (>10, <0, 5.55m, 10.05m) TryCreate tests (D8), throwing constructor tests
  - [ ] `Runtime`: Valid and invalid TryCreate tests (> 0 vs ≤ 0), throwing constructor tests
  - [ ] `ReleaseYear`: Valid and invalid TryCreate tests (boundary 1888, now.Year + 5 vs before/after), throwing constructor tests
  - [ ] `LibraryPath`: Valid and invalid TryCreate tests (non-blank, no `..` vs rejection of `..`, `a/../b`, `a..b`), throwing constructor tests
  - [ ] `MediaFormat`: Valid and invalid TryCreate tests (normalization: trim → strip exactly one leading `.` → lowercase → reject blank; `"."` and `" . "` invalid, `"..mkv"` becomes `".mkv"`), throwing constructor tests
  - [ ] `with`-bypass guarantee: Reflection test asserts every value-object property has `SetMethod == null`
  - [ ] Additional missing tests: Movie.Create blank/whitespace title throws; MovieMetadata blank title throws; ReleaseYear equality across different `now` values

**Subtotal:** ~350 lines  
**Exit criteria:** `dotnet test` all pass, ArchitectureTests green.

### Commit 5b: Property Tests (FsCheck, D9)

- [ ] Add FsCheck to `Directory.Packages.props` with a stable version compatible with net10.0
- [ ] Add FsCheck to `tests/LamuFlix.Test/LamuFlix.Test.csproj` reference
- [ ] Write property tests in `tests/LamuFlix.Test/Domain/PropertyTests.cs`:
  - [ ] Property tests for all value-object invariants (MovieId > 0, ImdbId regex, ImdbRating per D8 predicate, Runtime > 0, ReleaseYear bounds, LibraryPath non-blank/no-.., MediaFormat normalization)
  - [ ] Property tests for the state transition table (all state, all action combinations)
  - [ ] Each property test tagged `[Trait("Category", "Property")]` and using `[Fact]` with FsCheck

**Exit criteria:** Property tests pass; `run-property-tests` exits 0.

### Commit 6: CONTEXT.md Glossary

- [ ] `CONTEXT.md`: Extend existing entries and add new ones:
  - [ ] Extend **Enrichment Status** (`CONTEXT.md:27-29`): add the four enum values (Pending, Enriched, NotFound, Failed)
  - [ ] Extend **Watchlist** (`CONTEXT.md:38`): clarify independence from enrichment status
  - [ ] Add **Movie Aggregate**: domain model root for enrichment workflows
  - [ ] Add **Enrichment Attempt**: monotonic counter for enrichment calls

**Subtotal:** 1 edit  
**Exit criteria:** `dotnet build` succeeds, no errors.

### Phase B: Gate Sequence (Sequential)

Run gates in order after all code is committed:

- [ ] **Gate 1**: `./scripts/run-roslyn-analyzers.ps1` → exit 0
- [ ] **Gate 2**: `./scripts/run-cyclomatic-complexity.ps1` (threshold 15) → exit 0
- [ ] **Gate 3**: `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` (refactor) → exit 0
- [ ] **Gate 4**: `./scripts/run-jetbrains-inspectcode.ps1` → exit 0
- [ ] **Gate 5**: `./scripts/run-vulnerable-packages.ps1` → exit 0
- [ ] **Gate 6**: `dotnet format --verify-no-changes` → exit 0
- [ ] **Gate 7**: `dotnet build` → exit 0, zero warnings
- [ ] **Gate 8**: `dotnet test` → exit 0
- [ ] **Gate 9**: `./scripts/run-property-tests.ps1 -Project tests/LamuFlix.Test/LamuFlix.Test.csproj` → exit 0; exit 2 SKIPPED is a failure
- [ ] **Gate 10**: `dotnet stryker` → mutation score ≥ 80%, exit 0. **Stop condition**: If report mutates files outside `src/LamuFlix.Core/Domain/`, misses Core Domain, or cannot run, stop and report to Keel.

**Exit criteria:** All 10 gates pass sequentially.

### Phase B: Review and Merge

- [ ] Open delivery PR with format `DEV-294 - {subject}`
- [ ] Run `/ship-review` (size L ticket requires Sentry, Ledger, Compass)
- [ ] Address Critical/High findings, re-verify
- [ ] File Medium/Low findings as follow-ups (precedent: DEV-291)
- [ ] Merge PR (owner performs manual merge)
- [ ] Record follow-ups (Rigger):
  - Persistence and adoption (parent DEV-282, size L, estimate 5)
  - HTTP 409 mapping (Medium): PR gate: new exception types are mapped in the single IExceptionHandler. Create IExceptionHandler and wire `InvalidTransitionException` to HTTP 409 at API boundary. No IExceptionHandler exists in `src/` at recon HEAD (recon-DEV-294 §10a, Q8); creating one is outside frozen scope.

---

## Acceptance Criteria by Phase

### Phase A (Gate 1)
- [x] spec.md, plan.md, tasks.md complete
- [x] Owner checkboxes carried, answered YES
- [ ] Keel `/speckit-analyze` completed, fixes applied
- [ ] Spec PR opened and merged with both checkboxes answered YES

### Phase B (Implementation)
- [ ] All 12 domain types created (7 value objects with TryCreate factories, 2 enums, MovieMetadata, InvalidTransitionException, Movie aggregate)
- [ ] Test project reference added
- [ ] 350+ lines of test code, 20 test cases + missing-test additions
- [ ] All 10 gates pass sequentially
- [ ] Mutation score ≥ 80% (Stryker) on src/LamuFlix.Core/Domain only
- [ ] `/ship-review` completed
- [ ] Follow-ups filed
- [ ] PR merged, main checkout clean

---

## Checkbox Summary

| Item | Type | Status | Blocking |
|---|---|---|---|
| Q10(b) FsCheck | Owner | ☑ YES 2026-09-26 | No |
| Q12 CONTEXT.md | Owner | ☑ YES 2026-09-26 | No |
| Phase A specs | Task | ✅ In Progress | No |
| Phase B implementation | Task | ⏳ Awaiting spec PR merge | Yes |

**Total checkboxes:** 2 owner (answered), 0 task (non-blocking)

---

## Dependencies & Milestones

```
Phase A Specs (complete)
  ↓
Spec PR Opened (both checkboxes answered YES)
  ↓
User Merges Spec PR → Gate 1 Opens
  ↓ (Phase B entry)
Phase B Implementation (6 commits + 10 gates)
  ↓
/ship-review + findings
  ↓
PR Merged + Follow-ups Filed → Delivery Complete
```

---

## Notes

- **No taste assumptions**: All decisions are ticket-named or Patron-ruled with citation.
- **No ASSUMPTIONS.md**: Written only if taste assumptions are made (none here).
- **Worktree**: All Phase B work happens in `F:\Dev\LamuFlix.worktrees\feature-DEV-294-...`. Main checkout stays clean.
- **Commit format**: `DEV-294 - {subject}` followed by Co-Authored-By attribution.
- **Time estimate**: Phase A (this spec) ~1–2 hours; Phase B (implementation + gates + review) ~8–12 hours.
