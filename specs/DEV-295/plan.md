# DEV-295 Implementation Plan: Enrichment Failure Category Taxonomy and Classifier

**Status:** Phase A, frozen at plan challenge  
**Base:** `origin/main` at `baa1524`  
**Scope:** Confined to `specs/DEV-295` during Phase A; Phase B will implement.

---

## Technical Context

### Stack & Architecture

- **Language & Runtime:** C# 14 on .NET 10 (no legacy frameworks).
- **Core layer:** Pure, dependency-free BCL usage in `LamuFlix.Core`.
- **Testing:** xUnit v3 with Shouldly assertions; FsCheck.Xunit.v3 3.4.0 for property tests (new addition to CPM).
- **Code structure:**
  - `LamuFlix.Core/Domain/` → domain types (`EnrichmentFailureCategory`).
  - `LamuFlix.Core/Features/Enrichment/` → feature logic (`EnrichmentFailureClassifier`).
  - Dependency graph: `Core.Features.Enrichment` depends only on `Core.Domain`, `Core.Pipeline`, `Core.Ports`, and `System`.

### Key Constraints

1. **No external SmartEnum package** — sealed record with static instances (Q1, Q2).
2. **No DI or interface** — static method, pure logic, no state (Q3, Q4).
3. **Cyclomatic complexity:** ≤15 for implementation, ≤6 for refactor (brief.md:131).
4. **Architecture boundary:** `ArchitectureTests.cs:69-96` enforces strict feature-layer dependencies.
5. **Mutation testing:** Stryker 80% on `LamuFlix.Core` (constitutional requirement, Q12).
6. **Caller cancellation is exceptional:** Does not return a category; propagates (Q6).

---

## Design Decisions (Frozen by Patron, CONCLUSIONS.md)

### 1. Taxonomy Design (Q1, Q2)

**Decision:** Sealed-record closed set with four instances.

```csharp
public sealed record EnrichmentFailureCategory
{
    public required string Code { get; init; }
    public required string SafeDescription { get; init; }
    public required bool IsRetryable { get; init; }

    private EnrichmentFailureCategory() { }

    public static readonly EnrichmentFailureCategory ProviderUnavailable = 
        new() { Code = "provider_unavailable", SafeDescription = "...", IsRetryable = true };
    
    // ... RateLimited, InvalidResponse, Unknown
}
```

**Rationale:** Sealed record provides value semantics, immutability, and static instances with stable value equality. No SmartEnum package overhead. Closed-set invariant is enforced by T015 property test (exhaustive static instance enumeration), not by compile-time switch exhaustiveness.

**Alternatives considered:** Plain `enum` (rejected: no properties; `Ardalis.SmartEnum` (rejected: external dependency; Q1 ruling).

### 2. Classifier Architecture (Q3, Q4, Q5)

**Decision:** Pure, static `Classify(Exception exception)` method in `EnrichmentFailureClassifier`.

```csharp
namespace LamuFlix.Core.Features.Enrichment;

public static class EnrichmentFailureClassifier
{
    public static EnrichmentFailureCategory Classify(Exception exception)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        
        // Traverse outermost → innermost
        // First recognized signal wins
        // Return EnrichmentFailureCategory or propagate OperationCanceledException
    }
}
```

**Rationale:** 
- Pure logic, no state, no DI overhead.
- Outermost-to-innermost traversal captures HTTP status before inner transport exceptions.
- Caller cancellation propagates (not classified) to preserve graceful-shutdown semantics (constitution IV).

**Mapping:** Implemented per spec.md mapping table; HTTP 429 → `RateLimited`, 5xx and 408 → `ProviderUnavailable` (408 separated from 5xx), unmapped status (1xx, 3xx, ≥600) → `Unknown` (no fallthrough), null status → `ProviderUnavailable`, timeouts, network errors → `ProviderUnavailable`, auth/4xx (except 429, 408, 401, 403) → `InvalidResponse`, unrecognized → `Unknown`.

### 3. Cycle Protection & Precedence (Q4, Q5)

**Decision:** Traversal tracks visited exceptions to prevent infinite loops on cyclic `InnerException` chains (defensive).

**Precedence rule:** 
- If `HttpRequestException` with a status is encountered, use its status even if inner exceptions suggest a different category (e.g., inner `TimeoutException`). This ensures the outer HTTP signal is trusted over lower-level signals.
- A present `HttpRequestException.StatusCode` (including null) is a recognized signal and does not fall through to inner exceptions. A null `StatusCode` → `ProviderUnavailable` (transport failure). An unmapped status code (1xx, 3xx, ≥600) → `Unknown` (signal present but unrecognized; no fallthrough).

### 4. Caller Cancellation Handling (Q6)

**Decision:** Three cases:

1. **Caller-cancelled (propagate):** `OperationCanceledException` with a cancelled `CancellationToken` → rethrow (not classified).
2. **Timeout-shaped (classify):** `OperationCanceledException` with timeout correlation (inner exception or timeout flag) → `ProviderUnavailable`.
3. **Bare (unknown):** `OperationCanceledException` with no cancelled token and no timeout signal → `Unknown`.

**Rationale:** Caller cancellation must not be misclassified as a failure (constitution Enrichment Reliability Rules). Timeout-shaped cancellation is a provider unavailability signal.

### 5. Constitution Compliance Check

The /speckit Constitution Check verifies this work respects the repo's governance principles:

1. **Principle I** (Core isolation + feature folders): `EnrichmentFailureCategory` lives in `LamuFlix.Core.Domain` (isolated, feature-aware). `EnrichmentFailureClassifier` in `LamuFlix.Core.Features.Enrichment` (strict namespace boundary).

2. **Principle IV** (category + IsRetryable + raw-text ban): The sealed record includes `IsRetryable` boolean; `SafeDescription` excludes all raw exception text, URLs, and system details per spec requirement.

3. **Principle VIII** (functionality-based English names, no vendor names in Core): All names (`EnrichmentFailureCategory`, `EnrichmentFailureClassifier`, `SafeDescription`, `IsRetryable`) are domain-driven English, vendor-neutral, and free of library names.

4. **Principle IX** (xUnit v3 + Shouldly + FsCheck.Xunit.v3 + Stryker 80 on Core): Tests use xUnit v3 with Shouldly assertions; FsCheck.Xunit.v3 3.4.0 added to CPM with [Property] + [Trait("Category","Property")] attributes; Stryker mutation testing at 80% threshold on `LamuFlix.Core`.

**Static-analysis gates** (all exit 0):
- Roslyn analyzers (CA/IDE warnings)
- Cyclomatic complexity (≤15 main, ≤6 refactor)
- InspectCode (ReSharper/Rider)
- `dotnet format --verify-no-changes`

### 6. Test Strategy (Q9, Q10)

**Unit Tests:**
- Category tests: exhaustive Theory over the four instances, asserting properties and frozen `SafeDescription`.
- Classifier tests: one row per mapping table + null case + cancellation cases + AggregateException chain + cyclic InnerException chain.
- Framework: xUnit v3, Shouldly, no mocks (pure logic).

**Property Tests (FsCheck.Xunit.v3):**
- Closed set invariant: exactly four categories (deterministic enumeration of static instances, not sampling).
- Safe-field properties: non-blank, no raw exception text.
- `IsRetryable` matches frozen rules.
- `Classify` always returns a category for generated unrecognized exceptions (except caller cancellation).

**Coverage:** Stryker 80% on `LamuFlix.Core`; mapping table and closed-set invariants are the mutation surface.

---

## Implementation Strategy

### Phase Structure

Setup: verify the worktree and branch (`feature/295-spec`); Phase A spec drafting only.

**Phase 1: Core Taxonomy**
- Implement `EnrichmentFailureCategory.cs` as a sealed record with four static instances.
- Verify sealed record semantics (value equality, immutability, static instances).
- Test: Basic property assertion (code, description, retry flag).

**Phase 2: Classifier Logic**
- Implement `EnrichmentFailureClassifier.Classify(Exception)` with:
  - Null check and ArgumentNullException.
  - Outermost-to-innermost traversal with cycle protection.
  - Mapping logic for HTTP status codes, timeout exceptions, network errors, JSON errors.
  - Cancellation handling (propagate caller cancellation, classify timeout-shaped).
  - Early returns and guard clauses to keep complexity ≤15.

**Phase 3: Unit Tests**
- Category tests: Theory asserting all four instances.
- Classifier tests: one case per mapping row + edge cases.
- Mutation surface verification.

**Phase 4: ADR & Documentation**
- Write `docs/adr/ADR-0006.md` (status: Accepted from Phase B start).
- Document the principle: categories separated from exception types; raw errors never leave logs.

**Phase 5: Integration & Wiring, then Property Tests**
- Add FsCheck.Xunit.v3 3.4.0 to `Directory.Packages.props`.
- Add FsCheck.Xunit.v3 + Core ProjectReference to `tests/LamuFlix.Test/LamuFlix.Test.csproj`.
- Then FsCheck.Xunit.v3 generators for exceptions and edge cases.
- Closed-set and safe-field invariants.

**Phase 6: Gates & Validation**
- Run all ten gates in order.
- Stryker last (pre-delivery-PR, 80% threshold on Core).
- Fix any failures and re-run gates.

### Key Dependencies & Ordering

1. **Taxonomy must land before classifier:** The classifier depends on the category type.
2. **Classifier logic before tests:** Tests verify the mapping.
3. **Unit tests before property tests:** Property tests depend on the logic being correct.
4. **Gates last:** Build, test, and static analysis after all code is written.
5. **ADR before wiring; wiring before property tests:** six-phase order (`brief.md:209-212`).

### Parallel Opportunities

- **Unit test cases can be written in parallel** once the classifier skeleton is in place.
- **Property test setup and mapping table tests can run concurrently.**
- **ADR can be drafted in parallel with classifier implementation** (independent artifact).

### Edge Cases & Defensive Measures

1. **Cyclic `InnerException` chains:** Traversal tracks visited exceptions using reference-based tracking.
2. **Null `StatusCode`:** Treated as `ProviderUnavailable` (transport failure without status).
3. **Nested `AggregateException`:** Outermost-to-innermost flattening with HTTP status precedence; all InnerExceptions iterated in order.
4. **Timeout-shaped `TaskCanceledException` predicate and order:** 
   - Inner `TimeoutException` (or similar timeout signal) → `ProviderUnavailable`.
   - Else, `CancellationToken` is cancelled → propagate (caller cancellation).
   - Else, bare `OperationCanceledException` → `Unknown`.
   - **Limitation (follow-up 1):** Detection of caller-initiated CancellationToken depends on the exception's token property; linked CTS chains (CancelAfter) require explicit check by caller (Phase B caller must call `token.IsCancellationRequested` before `Classify` if linked-CTS propagation is needed; ADR-0006 records this as a downstream enhancement).

---

## Architecture Compliance

### ArchitectureTests Constraints

**From brief.md:134–136:**

- `LamuFlix.Core` stays free of EF Core, Npgsql, RabbitMQ, Infrastructure.
- `Core/Features/Enrichment/` depends only on its own namespace, `Ports`, `Domain`, `Pipeline`, `System`.

**Compliance:**
- No `using Infrastructure;` or EF Core.
- `EnrichmentFailureClassifier` imports `System.*` only; no logging namespace.

### Mutation Testing Surface

**Critical invariants for Stryker:**

1. **Closed-set completeness:** All four instances are distinct and exhaustive.
2. **Mapping table:** Every HTTP status code, exception type, and signal is tested.
3. **Retry flag:** Each category's `IsRetryable` is correct (mutable, will be killed by Stryker).
4. **Safe descriptions:** No raw exception text (asserted, Stryker will try to inject it).
5. **Cancellation propagation:** Caller cancellation must rethrow (Stryker will try to swallow it).

---

## Out of Scope (Frozen Boundary, Q8)

- No edit to `EnrichmentJobProcessor`, `OmdbMetadataProvider`, `MovieService`, `Data.Models`, or schema/migrations.
- No new OTel emitter code or telemetry dependency.
- No new project, layer, or package dependency (sealed record is BCL; FsCheck.Xunit.v3 is testing-only).
- No `CONTEXT.md` edit (term "Failure Category" already exists at line 31).
- No rename of existing ADRs.
- No consumption by DEV-294 Phase B; DEV-295 Phase B delivers the sealed-record type first (see DEV-294 Coordination).
- No retry-wiring integration (pipeline adoption is Phase B follow-up 2).
- No OTel span or log emission (ADR records principle; emission is Phase B follow-up 3).

---

## Assumptions

1. **FsCheck.Xunit.v3 3.4.0:** Phase B will pin FsCheck.Xunit.v3 3.4.0 in `Directory.Packages.props`. Property tests use [Property] + [Trait("Category","Property")] attributes; Phase B must verify run-property-tests.ps1 gate works with native property discovery.
2. **DEV-294 coordination:** DEV-294's Phase B will consume the canonical `Core.Domain.EnrichmentFailureCategory` (not implement a conflicting enum). DEV-294's spec will be amended before Phase B (Q1 ruling).
3. **No OTel emission:** ADR records the principle; actual span/log emission is downstream (Phase B follow-up 3).
4. **Caller cancellation context:** The caller (e.g., `EnrichmentJobProcessor`) holds the `CancellationToken`; the classifier detects it via the exception's `CancellationToken` property. Linked CTS (CancelAfter) propagation requires explicit check by caller (follow-up 1).

---

## Summary

DEV-295 Phase A produces a frozen specification of a pure, closed failure-category taxonomy and a static exception classifier. Phase B will implement the taxonomy and classifier, write comprehensive unit and property tests, ensure architectural compliance, and validate via ten gates with Stryker mutation testing at 80% on `LamuFlix.Core`. The design respects caller cancellation semantics (propagates, not classifies), prioritizes HTTP status signals over lower-level exceptions, and maintains pure, dependency-free logic in `LamuFlix.Core`.

**Next action:** Keel analyzes the spec, plan, and tasks; after Patron approval and owner checkboxes (none in this case), Phase B implementation begins.
