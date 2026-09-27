# DEV-295 Specification: Enrichment Failure Category Taxonomy and Classifier

**Status:** Specification (frozen scope, all questions answered by Patron)  
**Feature:** Implement EnrichmentFailureCategory taxonomy and root-cause exception classifier  
**Ticket:** DEV-295 (parent: DEV-282)  
**Base:** `origin/main` at `baa1524`

## User Value

Users need a consistent, caller-safe way to understand why metadata enrichment failed. Raw exception details expose internal implementation; failure categories provide meaningful, actionable feedback for retry decisions and user-facing messaging.

## Success Criteria

1. **Classifier accuracy**: Every known root-cause exception (HTTP status codes, timeouts, network failures, malformed responses) maps to the correct failure category in unit tests.
2. **Caller safety**: All `SafeDescription` values contain no raw exception text, stack traces, internal URLs, or system details.
3. **Consistency**: The taxonomy and classifier exist for uniform caller-safe use; pipeline adoption and retry wiring are downstream (out of scope, follow-up 1).
4. **Testability**: The classification logic is exhaustively tested (xUnit unit tests + FsCheck.Xunit.v3 property tests); mutation testing achieves 80% on LamuFlix.Core.

## Scope & Technical Design (Authoritative - Patron Ruled)

### 1. EnrichmentFailureCategory Taxonomy

A closed, SmartEnum-style sealed-record set in `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs`:

| Instance | Code | IsRetryable | SafeDescription |
|----------|------|-----------|---|
| `ProviderUnavailable` | `provider_unavailable` | `true` | "The metadata provider is temporarily unavailable." |
| `RateLimited` | `rate_limited` | `true` | "The metadata provider is temporarily rate limiting requests." |
| `InvalidResponse` | `invalid_response` | `false` | "The metadata provider returned an unusable response." |
| `Unknown` | `unknown` | `true` | "The enrichment failed for an unknown reason." |

**Design notes:**
- No `Ardalis.SmartEnum` package; implemented as a sealed record with private constructor and static instances.
- `Code` is a stable lowercase snake_case contract value (for persistence/API serialization).
- `SafeDescription` is provider-agnostic and caller-safe (taste copy ruled by Patron, in ASSUMPTIONS.md).
- `IsRetryable` guides retry policies in the enrichment pipeline.
- Comparison and `switch` over instances are stable.

### 2. EnrichmentFailureClassifier

A pure, stateless classifier in `src/LamuFlix.Core/Features/Enrichment/EnrichmentFailureClassifier.cs`:

**Signature:**
```csharp
public static EnrichmentFailureCategory Classify(Exception exception)
```

**Behavior:**
- Throws `ArgumentNullException` if the exception is null; never returns null.
- Traverses `Exception` and `InnerException` outermost-to-innermost with cycle protection.
- First recognized signal wins; HTTP status (if present) beats inner transport signals. A null `StatusCode` on `HttpRequestException` is itself a recognized signal → `ProviderUnavailable`; does not fall through.
- Maps root-cause exceptions to categories per the mapping table below.
- **Caller cancellation** (explicit exception): propagates unclassified (caller cancellation must not be misclassified as failure).

**Mapping table** (Patron-ruled, Q5):

| Signal | Category | Notes |
|--------|----------|-------|
| `HttpRequestException.StatusCode == 429` | `RateLimited` | HTTP Too Many Requests |
| `HttpRequestException.StatusCode` ∈ [500–599] | `ProviderUnavailable` | 5xx Server Error |
| `HttpRequestException.StatusCode == 408` | `ProviderUnavailable` | Request Timeout |
| `HttpRequestException.StatusCode == null` | `ProviderUnavailable` | Transport failure (no status) |
| `TimeoutException` | `ProviderUnavailable` | Timeout waiting for response |
| timeout-shaped `TaskCanceledException` | `ProviderUnavailable` | Cancellation due to timeout, not caller request |
| `SocketException` | `ProviderUnavailable` | Network-level error |
| `IOException` | `ProviderUnavailable` | Network I/O failure |
| `JsonException` (malformed payload) | `InvalidResponse` | Response parsing failed |
| `HttpRequestException.StatusCode` ∈ [401, 403] | `InvalidResponse` | Auth/permission error |
| `HttpRequestException.StatusCode` ∈ [400–499] ∖ {408, 429, 401, 403} | `InvalidResponse` | Other 4xx client error |
| `HttpRequestException.StatusCode` is present but unmapped (1xx, 3xx, ≥600, other) | `Unknown` | Unrecognized HTTP status; do **not** fall through to inner signals |
| Any other exception | `Unknown` | Unrecognized failure type |

**Signal Precedence:** A present `HttpRequestException.StatusCode` (even unmapped) is a recognized signal and beats all inner signals; unmapped codes → `Unknown` (no fallthrough).

**Cancellation handling** (Q6, Patron-ruled):
- A caller-cancelled `OperationCanceledException` whose `CancellationToken` is cancelled → **propagates** (re-thrown, not classified).
- A timeout-shaped `OperationCanceledException` (inner exception or timeout correlation) → `ProviderUnavailable`.
- A bare `OperationCanceledException` with no cancelled token and no timeout signal → `Unknown`.

### 3. Architecture & Integration

- **Location:** `EnrichmentFailureCategory` in `LamuFlix.Core.Domain` (consumed by DEV-294's `Movie` entity). `EnrichmentFailureClassifier` in `LamuFlix.Core.Features.Enrichment`.
- **Dependencies:** No new external packages (sealed record is BCL). No DI registration (static method, pure logic).
- **Scope boundary:** No edit to `EnrichmentJobProcessor`, `OmdbMetadataProvider`, Worker, schema, or database. No OTel emitter code (ADR records the principle; emission is downstream integration).

### 4. Documentation & ADR

**Path:** `docs/adr/ADR-0006.md`  
**Title:** "0006. Failure categories separated from exception types; raw errors never leave logs"  
**Status:** `Accepted` (phase B creation; spec PR has already merged at Gate 1)  
**Records:**
- Failure categories are distinct from exception types.
- `SafeDescription` carries no raw exception detail.
- Raw exception text, stack traces, and URLs go only to logs/OpenTelemetry spans, never to database columns or API responses.
- Classification and retry wiring are downstream integration.

## Acceptance Criteria

1. [ ] `EnrichmentFailureCategory` sealed-record set with all four instances and properties is implemented and compiles.
2. [ ] `EnrichmentFailureClassifier.Classify(Exception)` maps all known exceptions to correct categories in unit tests.
3. [ ] All `SafeDescription` values are confirmed non-blank and contain no raw exception text, URLs, hosts, or internal details.
4. [ ] `ADR-0006.md` is written, status Accepted, and documents the principle.
5. [ ] Unit tests (xUnit/Shouldly) cover all mapping rows, null handling, and cancellation cases.
6. [ ] Property tests (FsCheck.Xunit.v3) verify closed-set invariants, safe-field non-blankness, retry semantics, and classifier outcomes.
7. [ ] All ten gates pass: Roslyn, Cyclomatic (15), Cyclomatic (6), InspectCode, format, vulnerable-packages, build, test, property-test, and Stryker (80% on Core).

## Testing Strategy (Patron-Ruled, Q9–Q10)

**Frameworks:** xUnit v3, Shouldly, FsCheck.Xunit.v3 3.4.0 (new, added to CPM).

**Unit Tests** (`tests/LamuFlix.Test/Domain/EnrichmentFailureCategoryTests.cs`):
- Exhaustive Theory over all four categories asserting `Code`, `IsRetryable`, and frozen `SafeDescription`.
- Assertion that every `SafeDescription` is non-blank and contains no URL, host, or exception text.

**Classifier Tests** (`tests/LamuFlix.Test/Features/Enrichment/EnrichmentFailureClassifierTests.cs`):
- One case per mapping table row: 429, 5xx, 408, null status, `TimeoutException`, timeout-shaped `TaskCanceledException`, `SocketException`, `IOException`, `JsonException`, 401, 403, other 4xx, unrecognized exception.
- `ArgumentNullException` on null input.
- Caller-cancellation propagation case (exception re-thrown).
- Bare `OperationCanceledException` → `Unknown`.
- Nested/`AggregateException` chain case proving outermost-status precedence and cycle protection.

**Property Tests** (`tests/LamuFlix.Test/[Domain|Features]/EnrichmentPropertyTests.cs`, `[Property][Trait("Category", "Property")]`, FsCheck.Xunit.v3):
- Closed set is exactly the four categories.
- `Code` and `SafeDescription` are non-blank.
- `IsRetryable` matches ticket rules.
- `Classify` returns a category for generated unrecognized exceptions.
- Do **not** assert non-throwing for null or caller cancellation; those are unit-test cases.

**Coverage:** Stryker on `LamuFlix.Core` at 80% threshold. Mapping table and closed-set invariants are the mutation surface.

## Out of Scope (Frozen Boundary, Q8)

- No edit to `EnrichmentJobProcessor`, `OmdbMetadataProvider`, `MovieService`, `Data.Models`, or schema/migrations.
- No new OTel emitter code or telemetry dependency.
- No new project, layer, or package dependency (sealed record is BCL).
- No `CONTEXT.md` edit (term "Failure Category" already exists at line 31).
- No rename of existing ADRs.

## Assumptions & Taste (Patron-Ruled, Q7, ASSUMPTIONS.md)

Every assumption below is `[assumed]` (taste, non-decision):

- `SafeDescription` exact copy is as specified in ASSUMPTIONS.md (frozen).
- `Code` values are contract (stable, snake_case, decided).
- `IsRetryable` is ticket-decided (not taste).
- No UI copy, sort order, or empty-state strings are introduced.

## File Manifest

**New files:**
- `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs`
- `src/LamuFlix.Core/Features/Enrichment/EnrichmentFailureClassifier.cs`
- `docs/adr/ADR-0006.md`
- `tests/LamuFlix.Test/Domain/EnrichmentFailureCategoryTests.cs`
- `tests/LamuFlix.Test/Features/Enrichment/EnrichmentFailureClassifierTests.cs`
- `tests/LamuFlix.Test/[Domain|Features]/EnrichmentPropertyTests.cs` (property tests)

**Edited files:**
- `Directory.Packages.props` (add FsCheck.Xunit.v3 3.4.0 PackageVersion)
- `tests/LamuFlix.Test/LamuFlix.Test.csproj` (add FsCheck.Xunit.v3 + Core ProjectReference)

---

**Gate Expectations (10 gates, all exit 0):**

1. `./scripts/run-roslyn-analyzers.ps1`
2. `./scripts/run-cyclomatic-complexity.ps1`
3. `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`
4. `./scripts/run-jetbrains-inspectcode.ps1`
5. `dotnet format --verify-no-changes`
6. `./scripts/run-vulnerable-packages.ps1`
7. `dotnet build`
8. `dotnet test` (including ArchitectureTests)
9. `./scripts/run-property-tests.ps1 -Project tests/LamuFlix.Test/LamuFlix.Test.csproj`
10. `dotnet stryker` (80% on LamuFlix.Core, pre-delivery-PR)
