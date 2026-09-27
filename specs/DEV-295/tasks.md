# DEV-295 Implementation Tasks: Enrichment Failure Category Taxonomy and Classifier

**Status:** Phase A planning (frozen, awaiting Gate 1 approval)  
**Branch:** `feature/295-spec`  
**Base:** `origin/main` at `baa1524`

---

## Task Organization

Tasks are organized by phase, with clear dependencies. Each task includes file paths and success criteria. All tasks are independently testable within their phase.

**Phases:**
1. **Phase 1:** Taxonomy foundation
2. **Phase 2:** Classifier implementation  
3. **Phase 3:** Unit tests
4. **Phase 4:** ADR & documentation
5. **Phase 5:** Wiring & property tests
6. **Phase 6:** Gate verification & delivery

---

## Phase 1: Taxonomy Foundation

**Phase Goal:** Implement the closed `EnrichmentFailureCategory` sealed-record set with all four instances and properties.

**Independent Test Criteria:**
- All four instances are defined and publicly accessible.
- Each instance has correct `Code` (lowercase snake_case), `SafeDescription` (non-blank, no raw exception text), and `IsRetryable` values.
- Sealed record prevents instantiation outside the static instances.
- Value equality works correctly (two instances with same properties are equal).

### T001: Create EnrichmentFailureCategory sealed record

- [ ] **T001** Create `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs`
  - Sealed record with `Code` (string), `SafeDescription` (string), `IsRetryable` (bool) properties.
  - Private constructor (prevents public instantiation).
  - Four static readonly instances: `ProviderUnavailable`, `RateLimited`, `InvalidResponse`, `Unknown`.
  - Static instances initialized with frozen values from ASSUMPTIONS.md.
  - Compile and verify no warnings.

**Success Criteria:**
- File compiles with no CA, IDE, or Roslyn warnings.
- All four instances are accessible via `EnrichmentFailureCategory.ProviderUnavailable` etc.
- Static instances are distinct and immutable.

---

## Phase 2: Classifier Implementation

**Phase Goal:** Implement `EnrichmentFailureClassifier.Classify(Exception)` with exception traversal, mapping logic, and cancellation handling.

**Independent Test Criteria:**
- Classifier accepts any exception and returns a category (or propagates caller cancellation).
- Null input throws `ArgumentNullException`.
- Outermost-to-innermost traversal with cycle protection works correctly.
- HTTP status codes map correctly (429, 5xx, 408, null, others per spec).
- Timeout, network, and JSON exceptions map correctly.
- Caller cancellation propagates unclassified; timeout-shaped cancellation maps to `ProviderUnavailable`.
- Cyclomatic complexity ≤15.

### T002: Create EnrichmentFailureClassifier skeleton

- [ ] **T002** Create `src/LamuFlix.Core/Features/Enrichment/EnrichmentFailureClassifier.cs`
  - Public static class with `Classify(Exception exception)` method signature.
  - Argument null check (throw `ArgumentNullException`).
  - Method body: traversal logic stub (placeholder for Phase 2 implementation).
  - Compile and verify no warnings.

**Success Criteria:**
- File compiles with no warnings.
- Method signature matches specification.
- Namespace is `LamuFlix.Core.Features.Enrichment`.

### T003: Implement HTTP status mapping helper

- [ ] **T003** Extract HTTP status mapping logic in `EnrichmentFailureClassifier.cs`
  - Private static method `MapHttpStatusToCategory(int? statusCode) → EnrichmentFailureCategory?`.
  - Map 429 → `RateLimited`, 5xx and 408 → `ProviderUnavailable`, null status → `ProviderUnavailable`, 401/403 → `InvalidResponse`, other 4xx (excluding 408, 429, 401, 403) → `InvalidResponse`.
  - **Unmapped status precedence:** If status code is present but unmapped (1xx, 3xx, ≥600, or other), do NOT fall through to inner signals; return `Unknown` (signal present but unrecognized).
  - Early returns and guard clauses to keep complexity low.
  - Unit tested immediately (T008).

**Success Criteria:**
- Method correctly maps all status codes per spec.
- Tested in isolation (T008).

### T004: Implement exception traversal and signal detection

- [ ] **T004** Implement core traversal logic in `Classify` method
  - Flatten `AggregateException.InnerExceptions` by iterating all inner exceptions in order (outermost-to-innermost).
  - Track visited exceptions to prevent cycles (use reference-based `HashSet<Exception>` with ReferenceEqualityComparer to avoid int-hash collisions that can silently drop a node).
  - For each exception, detect signals: HTTP status code, timeout type, network type, JSON error type.
  - Store first HTTP status encountered (HTTP status precedence: an HTTP status present in any node beats inner transport signals).
  - Return category based on first recognized signal; if none, return `Unknown`.
  - Keep method ≤15 cyclomatic complexity (may need to extract helpers for status map and signal detection).
  - Note: Cyclic test needs a test double overriding the virtual Exception.InnerException, since InnerException has no setter.

**Success Criteria:**
- Traversal handles nested exceptions correctly.
- Cycle protection prevents infinite loops.
- First signal (especially HTTP status) wins precedence.
- Complexity ≤15 verified by running cyclomatic complexity gate.

### T005: Implement caller cancellation detection and propagation

- [ ] **T005** Add cancellation handling to `Classify` method
  - Detect `OperationCanceledException` with a cancelled `CancellationToken` → rethrow (propagate unclassified).
  - Detect timeout-shaped `TaskCanceledException` or `OperationCanceledException` with timeout inner exception → return `ProviderUnavailable`.
  - Detect bare `OperationCanceledException` with no cancelled token and no timeout signal → return `Unknown`.
  - Handle `OperationCanceledException` **before** returning `Unknown` for unrecognized types.

**Success Criteria:**
- Caller cancellation propagates (exception is rethrown).
- Timeout-shaped cancellation maps to `ProviderUnavailable`.
- Bare cancellation maps to `Unknown`.
- Tested in isolation (T010).

### T006: Implement full mapping table in Classify

- [ ] **T006** Add comprehensive exception-type detection and mapping to `Classify` method
  - Detect `JsonException` → `InvalidResponse`.
  - Detect `SocketException` → `ProviderUnavailable`.
  - Detect `IOException` → `ProviderUnavailable` (NOT a subtype relationship; HttpRequestException : Exception, not IOException).
  - Handle `HttpRequestException` (including null StatusCode → `ProviderUnavailable`) explicitly and separately from the IOException row.
  - Detect `TimeoutException` → `ProviderUnavailable`.
  - Detect `HttpRequestException` and extract status code (use T003 helper to map).
  - Ensure all mapping rows from spec.md mapping table are covered.
  - Default: any unrecognized exception → `Unknown`.

**Success Criteria:**
- All spec mapping table rows are implemented.
- Correct category returned for each exception type.
- Method complexity ≤15 verified by gate.
- Unit tests pass for all rows (T008–T011).

---

## Phase 3: Unit Tests

**Phase Goal:** Comprehensive unit test coverage of taxonomy and classifier with xUnit v3 and Shouldly.

**Independent Test Criteria:**
- All four category instances are correct (code, description, retry flag).
- All mapping table rows return correct categories.
- Edge cases (null input, cycles, cancellation) are handled correctly.
- No raw exception text appears in safe descriptions.

### T007: Create category property tests

- [ ] **T007** Create `tests/LamuFlix.Test/Domain/EnrichmentFailureCategoryTests.cs`
  - xUnit Theory that tests all four instances.
  - Assert each instance's `Code`, `SafeDescription`, and `IsRetryable` match frozen values.
  - Assert each `SafeDescription` is non-blank, contains no exception text, no URLs, no hosts.
  - Use Shouldly for assertions.
  - Run and verify no failures.

**Success Criteria:**
- Test class compiles and all tests pass.
- All four instances verified.
- Safe description content validation passes.

### T008: Create HTTP status mapping tests

- [ ] **T008** Create unit tests for HTTP status code mapping in `tests/LamuFlix.Test/Features/Enrichment/EnrichmentFailureClassifierTests.cs`
  - Test `Classify` with `HttpRequestException` for each status code row:
    - 429 → `RateLimited`.
    - 500, 502, 503, 504, 599 (boundary), 408 → `ProviderUnavailable`.
    - null status → `ProviderUnavailable`.
    - 401, 403 → `InvalidResponse`.
    - Other 4xx (400, 404, etc. excluding 408, 429, 401, 403) → `InvalidResponse`.
    - Unmapped status (e.g., 100, 200, 300, 600, 999) → `Unknown` (status present but unrecognized; do NOT fall through to inner signals).
  - Use Shouldly assertions.
  - Run and verify all tests pass.

**Success Criteria:**
- All status code mappings tested and passing.
- Exception creation and classifier call work correctly.

### T009: Create transport and timeout exception tests

- [ ] **T009** Add tests for transport exceptions to `EnrichmentFailureClassifierTests.cs`
  - Test `TimeoutException` → `ProviderUnavailable`.
  - Test timeout-shaped `TaskCanceledException` → `ProviderUnavailable`.
  - Test `SocketException` → `ProviderUnavailable`.
  - Test `IOException` → `ProviderUnavailable`.
  - Test `JsonException` → `InvalidResponse`.
  - Test unrecognized exception type → `Unknown`.
  - Run and verify all tests pass.

**Success Criteria:**
- All transport and timeout cases tested and passing.
- JSON error detection works.
- Unknown exception fallback works.

### T010: Create cancellation handling tests

- [ ] **T010** Add cancellation tests to `EnrichmentFailureClassifierTests.cs`
  - Test caller-cancelled `OperationCanceledException` (with cancelled token) → rethrow (use `Assert.Throws`).
  - Test timeout-shaped `TaskCanceledException` → `ProviderUnavailable`.
  - Test bare `OperationCanceledException` (no cancelled token, no timeout) → `Unknown`.
  - Run and verify all tests pass.

**Success Criteria:**
- Caller cancellation propagates correctly.
- Timeout-shaped cancellation maps correctly.
- Bare cancellation maps to `Unknown`.

### T011: Create edge case and integration tests

- [ ] **T011** Add edge case tests to `EnrichmentFailureClassifierTests.cs`
  - Test null input → `ArgumentNullException` thrown.
  - Test nested `AggregateException` with multiple inner exceptions (at least one with HTTP status) → walk all InnerExceptions in order; outer status wins (HTTP status present in any node beats inner transport signals).
  - Test cyclic `InnerException` chain (exception A → B → A) → no infinite loop, returns correct category.
  - Test `HttpRequestException` with status code 429 and inner `TimeoutException` → uses 429 (HTTP status precedence).
  - Run and verify all tests pass.

**Success Criteria:**
- Null input properly rejected.
- AggregateException flattened correctly with status precedence.
- Cyclic chains handled without hanging.
- HTTP status precedence verified.

---

## Phase 4: ADR & Documentation

**Phase Goal:** Document the architectural decision and integration principles.

**Independent Test Criteria:**
- ADR-0006 is created with correct title, status (Accepted at Phase B creation, never Proposed), and content.
- Principle is clearly stated: categories separated from exception types; raw errors never leave logs.
- Tracking and parent references are correct.

### T012: Create ADR-0006

- [ ] **T012** Create `docs/adr/ADR-0006.md`
  - Title: "0006. Failure categories separated from exception types; raw errors never leave logs".
  - Status: `Accepted` (decision is locked in for Phase B implementation).
  - Ticket: DEV-295 (parent: DEV-282).
  - Context: Metadata enrichment failures currently expose raw exception details; this risks leaking internal URLs, stack traces, and system details to callers.
  - Decision: Classify exceptions into categories; expose only safe descriptions to callers; raw details go only to logs/OpenTelemetry spans.
  - Consequences: Callers see consistent, safe messages; retry decisions based on category (e.g., `RateLimited` → exponential backoff); raw error context remains available for diagnostics in logs/traces.
  - **Limitations (follow-up 1):** Linked CTS (CancelAfter) propagation requires explicit check by caller. Follow-ups (follow-up 2, 3): pipeline retry wiring and OTel emission are downstream.
  - Follow the shape of existing ADRs (e.g., `docs/adr/0014-core-movie-aggregate-separate-from-ef-entity.md`).

**Success Criteria:**
- ADR-0006 exists and is valid Markdown.
- All required sections are present.
- Principle is clearly documented.

---

## Phase 5: Wiring & Property Tests

**Phase Goal:** Add FsCheck property tests, update project files, and verify test infrastructure.

**Independent Test Criteria:**
- Closed-set invariant verified (exactly four categories).
- Safe-field properties validated (non-blank, no exception text).
- `IsRetryable` matches frozen rules for all instances.
- `Classify` always returns a category for unrecognized exceptions (except caller cancellation).
- Property tests compile and run successfully with `dotnet test` and `./scripts/run-property-tests.ps1`.

### T013: Add FsCheck.Xunit.v3 to package management

- [ ] **T013** Edit `Directory.Packages.props`
  - Add FsCheck.Xunit.v3 PackageVersion entry (pin to 3.4.0, xUnit v3 compatible; transitive FsCheck).
  - Example: `<PackageVersion Include="FsCheck.Xunit.v3" Version="3.4.0" />`.
  - Verify CPM validates correctly (run `dotnet nuget locals http-cache -c` and `dotnet restore` if needed).

**Success Criteria:**
- FsCheck.Xunit.v3 3.4.0 PackageVersion added to CPM.
- No package resolution errors.

### T014: Add FsCheck.Xunit.v3 and Core ProjectReference to test project

- [ ] **T014** Edit `tests/LamuFlix.Test/LamuFlix.Test.csproj`
  - Add `<PackageReference Include="FsCheck.Xunit.v3" />` (uses CPM version 3.4.0 from T013).
  - Add `<ProjectReference Include="../../src/LamuFlix.Core/LamuFlix.Core.csproj" />` (if not already present from DEV-294).
  - Verify project reloads correctly; run `dotnet build tests/LamuFlix.Test`.

**Success Criteria:**
- Project file edits are syntactically valid.
- Project compiles without errors.
- FsCheck.Xunit.v3 and Core are accessible in test files.

### T015: Create closed-set property test

- [ ] **T015** Create `tests/LamuFlix.Test/Domain/EnrichmentFailureCategoryPropertyTests.cs` (or append to existing property tests file)
  - **Deterministic closed-set enumeration (not FsCheck sampling):** Test that directly enumerates the four static instances (`EnrichmentFailureCategory.ProviderUnavailable`, `RateLimited`, `InvalidResponse`, `Unknown`) and asserts all four are distinct and accessible.
  - Attribute: `[Property][Trait("Category", "Property")]`.
  - Assert exhaustively (not via shrinking or sampling): all four static instances are present, distinct, and no others exist.
  - Run with `./scripts/run-property-tests.ps1 -Project tests/LamuFlix.Test/LamuFlix.Test.csproj`.

**Success Criteria:**
- Property test compiles.
- Test runs without Shrink iterations (closed set is fixed).

### T016: Create safe-field property test

- [ ] **T016** Add property test to verify safe descriptions
  - FsCheck.Xunit.v3 property: For each of the four categories, assert `SafeDescription` is non-blank and matches frozen values.
  - Attribute: `[Property][Trait("Category", "Property")]`.
  - Run and verify passes.

**Success Criteria:**
- Safe descriptions are immutable and match frozen values.
- Non-blank assertion passes.

### T017: Create classifier robustness property test

- [ ] **T017** Add property test for classifier behavior across generated exceptions
  - FsCheck.Xunit.v3: Define an explicit Gen<Exception> that produces known mappings (429, 5xx, 408, null status, TimeoutException, TaskCanceledException, SocketException, IOException, JsonException, 401, 403, other 4xx, unrecognized exception type).
  - Feed generated exceptions to `Classify`.
  - Assert result is always a valid `EnrichmentFailureCategory` (one of the four instances) **or** an `OperationCanceledException` with cancelled token is rethrown. Unrecognized exceptions must map to a valid category (spec.md:122, brief.md:152-155).
  - Do **not** test that null or caller cancellation never throw (those are unit tests).
  - Attribute: `[Property][Trait("Category", "Property")]`.
  - Run and verify passes.

**Success Criteria:**
- Classifier handles arbitrary exceptions without crashing.
- All results are valid categories or propagated cancellations.

---

## DEV-294 Coordination & Delivery Ordering

**Critical note:** The canonical `Core.Domain.EnrichmentFailureCategory` must land before DEV-294 Phase B consumes it. The DEV-294 spec/ADR 0014 amendment is recorded as a Rigger comment on DEV-294, not in this PR (brief.md:206-219, 231-238). DEV-295 Phase B delivers the complete sealed-record type; DEV-294 Phase B follows and consumes it without duplicating the taxonomy.

---

## Phase 6: Gate Verification & Delivery

**Phase Goal:** Run all ten static analysis, compilation, and test gates; fix any failures; prepare delivery PR.

**Independent Test Criteria:**
- All ten gates exit 0.
- Stryker mutation threshold 80% on `LamuFlix.Core` achieved.
- No files outside the scope changed.
- Commits follow `DEV-295 - {subject}` format.

### T018: Run Roslyn analyzers gate

- [ ] **T018** Run `./scripts/run-roslyn-analyzers.ps1` from worktree root
  - Expected: Exit 0 (no CA, IDE, or warning-level violations on changed `.cs` files).
  - If exit 1: Review output, fix violations (no suppression without documented justification), and re-run.

**Success Criteria:**
- Exit code 0.
- All new code is clean.

### T019: Run cyclomatic complexity gates

- [ ] **T019** Run `./scripts/run-cyclomatic-complexity.ps1` (threshold 15)
  - Expected: Exit 0.
  - If exit 1: Review output, refactor (extract helpers, early returns, guard clauses), and re-run.
  - Record threshold used in results.

- [ ] **T019b** Run `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` (refactor threshold)
  - Expected: Exit 0 (tighter threshold for quality gates).
  - If exit 1: Refactor further and re-run.

**Success Criteria:**
- Both gates exit 0.
- Methods are well-factored.

### T020: Run InspectCode gate

- [ ] **T020** Run `./scripts/run-jetbrains-inspectcode.ps1` from worktree root
  - Expected: Exit 0 (no ReSharper/Rider inspection violations).
  - If exit 1: Review output, fix violations, and re-run.

**Success Criteria:**
- Exit code 0.
- Code quality meets InspectCode standards.

### T021: Run format check gate

- [ ] **T021** Run `dotnet format --verify-no-changes` from worktree root
  - Expected: Exit 0 (code is formatted correctly).
  - If exit 1: Run `dotnet format` to auto-format, verify changes, and re-run verify.

**Success Criteria:**
- Exit code 0.
- Code is formatted.

### T022: Run vulnerable packages gate

- [ ] **T022** Run `./scripts/run-vulnerable-packages.ps1` from worktree root
  - Expected: Exit 0 (no vulnerable packages added).
  - FsCheck.Xunit.v3 3.4.0 should not introduce vulnerabilities; verify CPM has no deprecated/vulnerable versions.

**Success Criteria:**
- Exit code 0.
- No vulnerable dependencies.

### T023: Run build gate

- [ ] **T023** Run `dotnet build` from worktree root
  - Expected: Exit 0 (all projects build without errors or warnings).
  - If exit 1: Review error output and fix (likely compilation or architectural violations).

**Success Criteria:**
- Exit code 0.
- Solution compiles cleanly.

### T024: Run unit and integration test gate

- [ ] **T024** Run `dotnet test` from worktree root
  - Expected: Exit 0 (all tests pass, including ArchitectureTests).
  - If exit 1: Review test output, fix failures, and re-run.
  - Should include: xUnit tests, Shouldly assertions, ArchitectureTests (verifies Core dependencies).

**Success Criteria:**
- Exit code 0.
- All tests pass.
- ArchitectureTests pass (verifies Core.Features.Enrichment dependencies are within bounds).

### T025: Run property test gate

- [ ] **T025** Run `./scripts/run-property-tests.ps1 -Project tests/LamuFlix.Test/LamuFlix.Test.csproj`
  - Expected: Exit 0 (FsCheck.Xunit.v3 properties pass; exit 2 = skipped/not applicable).
  - Property tests generate many random inputs and verify invariants (deterministic closed-set test, safe-field invariants, generator robustness).
  - If exit 1: Review failure, check property test assumptions, and fix.

**Success Criteria:**
- Exit code 0 (not 2; 2 is blocking since FsCheck.Xunit.v3 is added in scope).
- All property tests pass.

### T026: Run Stryker mutation test gate

- [ ] **T026** Run `dotnet stryker` from worktree root (or via mutation test script if exists)
  - Project: `src/LamuFlix.Core/LamuFlix.Core.csproj`.
  - Threshold: 80% (constitutional requirement, Q12).
  - Expected: Exit 0 (mutation score ≥80%).
  - If exit 1: Review mutant report (files affected, surviving mutants).
    - Surviving mutants should be in `EnrichmentFailureCategory.cs` or `EnrichmentFailureClassifier.cs`.
    - Write additional tests to kill surviving mutants (e.g., assert `IsRetryable` values, mapping precedence).
  - Rerun until threshold is met.

**Success Criteria:**
- Exit code 0.
- Mutation score ≥80% on Core.
- No files outside the scope mutated.

### T027: Prepare delivery PR

- [ ] **T027** After all gates pass, create commits and open PR
  - Commit message format: `DEV-295 - {subject}` (e.g., `DEV-295 - Implement EnrichmentFailureCategory taxonomy and classifier`).
  - Commit includes: all `.cs` files, `ADR-0006.md`, updated `Directory.Packages.props` and test `.csproj`.
  - PR diff limited to `specs/DEV-295` (this Phase A) or Phase B files (after Gate 1).
  - Do not merge; user merges PRs (constitutional rule).
  - Report PR URL and link to Keel for final review.

**Success Criteria:**
- PR is created with clean diff.
- All gates have been run and passed (evidence in PR comments or CI).
- Commit messages follow format.
- User merges PR (not automated).

---

## Dependency Graph & Execution Order

```
T001 (Taxonomy)
  ↓
T002 (Classifier skeleton)
  ├→ T003 (HTTP mapping helper)
  ├→ T004 (Traversal logic)
  ├→ T005 (Cancellation handling)
  └→ T006 (Full mapping)
  ↓
T007 (Category tests) [P] T008–T011 (Classifier tests)
  ↓
T012 (ADR-0006)
  ↓
T013 (Add FsCheck.Xunit.v3) → T014 (Add ProjectReference)
  ↓
T015–T017 (Property tests)
  ↓
T018–T026 (Gates in order)
  ↓
T027 (Delivery PR)
```

### Parallel Opportunities

- **After T006:** T008–T011 (all classifier unit test cases can be written in parallel).
- **After T014:** T015–T017 (all property tests can be written in parallel).
- **After T012:** Can be done concurrently with T013–T026 (ADR is independent documentation).

---

## MVP Scope & Phasing

**Minimum Viable Product (Phase 1–3):**
- Taxonomy sealed record (T001).
- Classifier with full mapping table (T002–T006).
- Unit tests for all mapping rows and edge cases (T007–T011).

**Complete Delivery (Phase 4–6):**
- ADR documentation (Phase 4: T012).
- Wiring and FsCheck.Xunit.v3 integration (Phase 5: T013–T014).
- Property tests (Phase 5: T015–T017).
- All ten gates passing with Stryker 80% (Phase 6: T018–T026).
- Delivery PR (T027).

**Suggested Approach:** Implement Phase 1–3 first, then Phase 4 ADR (T012), then Phase 5 wiring (T013–T014) and property tests (T015–T017), then Phase 6 gates (T018–T026) to validate correctness and achieve mutation coverage (80% on Core).

---

## Task Status Tracking

| Task | Phase | Status | Owner | Notes |
|------|-------|--------|-------|-------|
| T001 | 1 | Blocked (awaiting Gate 1) | - | Taxonomy sealed record |
| T002–T006 | 2 | Blocked (awaiting Gate 1) | - | Classifier with mapping |
| T007–T011 | 3 | Blocked (awaiting Gate 1) | - | Unit tests |
| T012 | 4 | Blocked (awaiting Gate 1) | - | ADR-0006 |
| T013–T017 | 5 | Blocked (awaiting Gate 1) | - | Wiring & property tests |
| T018–T026 | 6 | Blocked (awaiting Gate 1) | - | Gates & delivery |
| T027 | 6 | Blocked (awaiting Gate 1) | - | Delivery PR |

**Gate 1 Approval Required** before Phase B (implementation) begins. The user merges the spec PR; this unblocks Phase B.
