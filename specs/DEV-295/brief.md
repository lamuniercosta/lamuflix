# DEV-295 Phase A brief

**Status.** Grill closed at 12/12 questions (Q1-Q12; Q2 folded into Q1). Patron ruled every
question; rulings and citations are in `CONCLUSIONS.md`, taste copy in `ASSUMPTIONS.md`. The scope
Quill drafts from is frozen below. Owner checkboxes: **none**. If it is not in this brief, it is
not decided.

## Source and worktree

- **Ticket.** DEV-295, "Implement EnrichmentFailureCategory taxonomy and root-cause exception
  classifier". In Progress, size M, parent DEV-282, type feature, no `ui:` tag.
- **Worktree.** `F:/Dev/LamuFlix.worktrees/feature-295-spec`.
  - Branch: `feature/295-spec`.
  - Base: `origin/main` at `baa1524` ("DEV-294 - Add Phase A specification artifacts and ADR 0014").
  - Feature pin: `specs/DEV-295`.
- **Evidence.** Task note `DEV-295`; `recon-DEV-295` lines 1-47; DEV-295 and DEV-294 ticket text;
  `specs/PRODUCT.md`; `.specify/memory/constitution.md` v1.1.0; `CONTEXT.md`; `docs/adr`;
  `src/LamuFlix.Worker/Services/EnrichmentJobProcessor.cs` and `OmdbMetadataProvider.cs`;
  `tests/LamuFlix.Test/EnrichmentTests.cs` and `WorkerTests.cs`; `Directory.Packages.props`;
  `harness.yml`.
- **Ticket *Scope & Technical Design* (authoritative, cited and moved on).**
  1. `EnrichmentFailureCategory` - "Create SmartEnum or sealed record set": `ProviderUnavailable`
     (retryable; transient 5xx, network drop), `RateLimited` (retryable with delay; HTTP 429),
     `InvalidResponse` (not retryable; malformed payload, HTTP 401), `Unknown` (retryable up to
     max attempts); properties `string Code`, `string SafeDescription`, `bool IsRetryable`.
  2. `EnrichmentFailureClassifier` - a single classification service mapping root-cause exceptions
     (e.g. `HttpRequestException`, `TimeoutException`, status code checks) to
     `EnrichmentFailureCategory`. Raw exception details, stack traces, and internal URLs must go to
     OpenTelemetry spans and logs ONLY, never to database columns or API responses.
  3. `docs/adr/ADR-0006.md` - failure categories separated from exception types; raw errors never
     leave logs.
- **Ticket acceptance criteria.** Classifier categorizes known exceptions accurately in unit tests;
  `EnrichmentFailureCategory` exposes caller-safe descriptions; `docs/adr/ADR-0006.md` accepted.

## Frozen committed scope

Every decision below is a Patron ruling; the Q-number points at `CONCLUSIONS.md`.

### 1. `EnrichmentFailureCategory` (Q1, Q2, Q7)

- A single canonical **SmartEnum-style closed sealed-record set** in
  `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs`, namespace `LamuFlix.Core.Domain`.
- Private constructor, static instances, value equality, `sealed`. No `Ardalis.SmartEnum` package
  and **no second type** under `Core.Enrichment` (`recon-DEV-295:14` was a candidate path only).
- Instances and members:

  | Instance | `Code` | `IsRetryable` | `SafeDescription` (taste, `ASSUMPTIONS.md`) |
  |---|---|---|---|
  | `ProviderUnavailable` | `provider_unavailable` | `true` | "The metadata provider is temporarily unavailable." |
  | `RateLimited` | `rate_limited` | `true` | "The metadata provider is temporarily rate limiting requests." |
  | `InvalidResponse` | `invalid_response` | `false` | "The metadata provider returned an unusable response." |
  | `Unknown` | `unknown` | `true` | "The enrichment failed for an unknown reason." |

- `IsRetryable` is ticket-decided. `Code` values are a stable contract value (Q7). Exact
  `SafeDescription` copy is taste `[assumed]` (`ASSUMPTIONS.md`).

### 2. `EnrichmentFailureClassifier` (Q4, Q5, Q6)

- `src/LamuFlix.Core/Features/Enrichment/EnrichmentFailureClassifier.cs`, namespace
  `LamuFlix.Core.Features.Enrichment` (Q3) - the sanctioned Enrichment feature, depending only on
  `Domain` (and `System`) so `ArchitectureTests` stays green.
- `public static EnrichmentFailureCategory Classify(Exception exception)`. No interface, no state,
  no DI registration (Q4; `PRODUCT.md` section 2).
- `ArgumentNullException` for a null argument. Never returns null.
- Traverse `Exception`/`InnerException` outermost-to-innermost with **cycle protection**; first
  recognized signal wins; an HTTP status present beats an inner transport signal (Q4/Q5).
- Mapping table (Q5):

  | Signal | Category |
  |---|---|
  | `HttpRequestException.StatusCode == 429` | `RateLimited` |
  | `HttpRequestException.StatusCode` 5xx or 408 | `ProviderUnavailable` |
  | `HttpRequestException.StatusCode == null` | `ProviderUnavailable` |
  | `TimeoutException` | `ProviderUnavailable` |
  | timeout-shaped `TaskCanceledException` | `ProviderUnavailable` |
  | `SocketException`, `IOException` (network drop) | `ProviderUnavailable` |
  | `JsonException` / malformed payload | `InvalidResponse` |
  | `HttpRequestException.StatusCode` 401, 403, other 4xx except 408 and 429 | `InvalidResponse` |
  | anything else | `Unknown` |

- Cancellation (Q6): a caller-cancelled `OperationCanceledException` whose `CancellationToken` is
  cancelled **propagates** (not classified); a timeout-shaped cancellation is `ProviderUnavailable`;
  a bare `OperationCanceledException` with no cancelled token and no timeout signal is `Unknown`.
  `Classify` returns a category for every non-null, non-caller-cancelled exception; caller
  cancellation is the explicit exception to totality.

### 3. ADR (Q11)

- `docs/adr/ADR-0006.md`, exactly as the ticket writes it. Title "0006. Failure categories
  separated from exception types; raw errors never leave logs". Status `Accepted` at Phase B
  creation: the DEV-295 spec PR has already merged by then (Gate 1), so it is never `Proposed`.
  Ticket DEV-295 (parent DEV-282). It records: categories
  separated from exception types; `SafeDescription` carries no raw detail; raw details go only to
  logs/traces, never to database columns or API responses; classification and retry wiring are
  downstream. Follow the DEV-294 ADR 0014 shape (`docs/adr/0014-core-movie-aggregate-separate-from-ef-entity.md`).

### 4. Test project wiring (Q9, F5)

- `Directory.Packages.props`: add one `PackageVersion` for **FsCheck.Xunit.v3** `3.4.0` (transitive
  `FsCheck`; xUnit v3 compatible; F5 ruling). Record the version in the commit.
- `tests/LamuFlix.Test/LamuFlix.Test.csproj`: add one `PackageReference` for FsCheck.Xunit.v3 and one
  `ProjectReference` to `src/LamuFlix.Core/LamuFlix.Core.csproj`. Idempotent if DEV-294 adds the
  same reference first.

### File manifest (names are Quill's; paths are fixed)

- `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs` (new)
- `src/LamuFlix.Core/Features/Enrichment/EnrichmentFailureClassifier.cs` (new)
- `docs/adr/ADR-0006.md` (new)
- `tests/LamuFlix.Test/Domain/EnrichmentFailureCategoryTests.cs` (new)
- `tests/LamuFlix.Test/Features/Enrichment/EnrichmentFailureClassifierTests.cs` (new)
- `tests/LamuFlix.Test/.../PropertyTests.cs` (new; exact path under the two namespaces is Quill's)
- `Directory.Packages.props` (edit: FsCheck.Xunit.v3 `PackageVersion` 3.4.0)
- `tests/LamuFlix.Test/LamuFlix.Test.csproj` (edit: FsCheck.Xunit.v3 + Core `ProjectReference`)

### Out of scope (frozen boundary, Q8)

- No edit to `EnrichmentJobProcessor`, `OmdbMetadataProvider`, `MovieService`, `Data.Models`
  (`Movie`, `MovieEnrichmentStatus`), `LamuFlixContext`, or any migration/schema.
- No OTel emitter code, no new telemetry dependency, and no claim that new spans are implemented.
  Existing `EnrichmentJobProcessor.cs:67-69` already logs the raw exception.
- No dependency on `Ardalis.SmartEnum`; no new project, layer, or interface.
- No `CONTEXT.md` edit (the **Failure Category** term at `CONTEXT.md:31` covers this).
- No rename of `docs/adr/ADR-0013`/`0014`.
- **This boundary is frozen: anything else is a follow-up, not a finding in this round.**

## Approach

- Pure, dependency-free C# 14 on .NET 10 in `LamuFlix.Core` using only the BCL. The classifier is
  a `static` method over exceptions; it reads no clock, no config, no network.
- The taxonomy is a closed sealed-record set; comparison and `switch` over instances are stable.
- Keep both types and every mapping helper small: guard clauses and early returns keep cyclomatic
  complexity under 15 (implement) and 6 (refactor). A mapping lookup/`switch` on status codes may
  need one extracted private helper.
- `Core` stays free of EF Core, Npgsql, RabbitMQ, and Infrastructure (`ArchitectureTests.cs:24-48`).
  `Core/Features/Enrichment/` may depend only on its own namespace, `Ports`, `Domain`, `Pipeline`,
  `System`, and `Microsoft.Extensions.Logging*` (`ArchitectureTests.cs:69-96`).
- No XML doc/comment unless strictly necessary (constitution *Coding Conventions*).

## Test strategy (Q9, Q10, F5)

- **Frameworks.** xUnit v3 and Shouldly from the existing CPM (`Directory.Packages.props:20,23`).
  FsCheck.Xunit.v3 `3.4.0` for property tests (Q9, F5). No Testcontainers, NSubstitute, or WireMock - the logic is pure.
- **Category tests** (`tests/LamuFlix.Test/Domain/EnrichmentFailureCategoryTests.cs`): an exhaustive
  Theory over the four categories asserting `Code`, `IsRetryable`, and the frozen `SafeDescription`;
  a test that every `SafeDescription` is non-blank and contains no URL, host, or exception text.
- **Classifier tests** (`tests/LamuFlix.Test/Features/Enrichment/EnrichmentFailureClassifierTests.cs`):
  one case per mapping-table row (429, 5xx, 408, null status, `TimeoutException`, timeout-shaped
  `TaskCanceledException`, `SocketException`, `IOException`, `JsonException`, 401, 403, other 4xx,
  unrecognized), the `ArgumentNullException` case, the caller-cancellation propagation case, the
  bare-`OperationCanceledException` -> `Unknown` case, and a nested/`AggregateException` chain case
  proving outermost-status precedence and cycle protection.
- **Property tests** (`[Property]` + `[Trait("Category", "Property")]`, FsCheck.Xunit.v3 `3.4.0`, Q9/F5): the closed set is exactly the
  four categories; `Code`/`SafeDescription` are non-blank; `IsRetryable` matches the ticket; and
  `Classify` returns a category for generated unrecognized exceptions. Do **not** assert
  non-throwing for null or caller cancellation; test those separately (Q4/Q6/Q9).
- **Coverage/mutation evidence.** Stryker on `LamuFlix.Core` at the `harness.yml` threshold 80; the
  mapping table and closed-set invariants are the mutation surface.

## Gate expectations (Q9, Q12)

Run under pwsh 7, on the `main...HEAD` diff; every named gate must exit natively.

1. `./scripts/run-roslyn-analyzers.ps1` - exit 0.
2. `./scripts/run-cyclomatic-complexity.ps1` (15) - exit 0.
3. `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` (refactor) - exit 0.
4. `./scripts/run-jetbrains-inspectcode.ps1` - exit 0.
5. `dotnet format --verify-no-changes` - exit 0.
6. `./scripts/run-vulnerable-packages.ps1` - exit 0.
7. `dotnet build` - exit 0.
8. `dotnet test` (including `ArchitectureTests`) - exit 0.
9. `./scripts/run-property-tests.ps1 -Project tests/LamuFlix.Test/LamuFlix.Test.csproj` - exit 0;
   exit 2 is blocking (FsCheck.Xunit.v3 is added in scope, so no opt-out).
10. `dotnet stryker` - pre-delivery-PR, mutation threshold 80 on `LamuFlix.Core` (Q12; constitution
    IX). Stop and report if the report mutates files outside the two new Core files or cannot run.

Exit codes: 0 = Pass, 1 = Fail, 2 = SKIPPED (never a pass). A gate that could not run is
`Could not run`. A plain build cannot substitute for the analyzers. No `/web` changes and no web
gates.

## Closing bar and round cap (Q12)

- **Blocking.** Every verified in-scope Critical or High finding is fixed and re-verified before the
  delivery PR.
- **Follow-ups.** Medium and Low findings, and anything outside the frozen scope, become follow-ups
  with their source and severity intact (DEV-291/DEV-294 precedent).
- **Round cap.** Two formal review rounds, at most two fix commits per round; a third round
  escalates and does not publish.
- **Review path (M).** Gauge pre-pass + the three axes Sentry, Ledger, Compass, in parallel; Keel
  adjudicates and needs all three axis reports.
- **Merge.** No seat merges; the user merges every PR.

## Task ordering and Phase A next action

**Phase A, this worktree.**
1. This brief, `CONCLUSIONS.md`, `ASSUMPTIONS.md`. Done.
2. Quill drafts `spec.md`, `plan.md`, and `tasks.md` from this brief, `recon-DEV-295`, and
   `specs/PRODUCT.md`; reports the files written; a gap is `needs decision:` to Keel.
3. Keel runs read-only `/speckit-analyze`, checks `plan.md`/`tasks.md` against this brief, and sends
   Quill one numbered fix list per round.
4. Plan challenge, adjudication, freeze.
5. Patron sets `gate1: provisional`. Rigger commits `specs/DEV-295` on `feature/295-spec` and opens
   the spec PR (diff `specs/DEV-295` only). No owner checkboxes to carry.

**Phase B, after Gate 1 (user merges the spec PR).**
1. Verify worktree and branch; commit format `DEV-295 - {subject}`.
2. Land the canonical `Core.Domain` taxonomy **before** DEV-294's implementation consumes it (Q12).
   Do not re-declare or add an enum.
3. Build in this frozen six-phase order and mirror it in the `plan.md` and `tasks.md` phase maps:
   Phase 1 taxonomy, Phase 2 classifier, Phase 3 unit tests, Phase 4 `docs/adr/ADR-0006.md`,
   Phase 5 the FsCheck.Xunit.v3 csproj/CPM wiring then the property tests, Phase 6 gates. The ADR
   precedes the csproj/CPM wiring and the tests; the wiring precedes the property tests.
4. Build and `dotnet test` immediately; then run the ten gates in order, Stryker last.
5. Open the delivery PR. The main checkout stays clean.

## DEV-294 reconciliation (Q1)

- DEV-294's merged `specs/DEV-294/spec.md:38` plain enum is superseded by this ticket's canonical
  sealed-record set. DEV-294's owner amends its spec/ADR 0014 and file ownership; Rigger records the
  clarification on DEV-294. This is a Patron §2.3 care-item ruling, not a ticket-text change, so it
  rides a DEV-294 amendment/comment and **not** this spec PR.
- DEV-294's Phase B must consume `LamuFlix.Core.Domain.EnrichmentFailureCategory`; it must not
  implement the conflicting enum.

## Owner checkboxes

**None.** No ruling changed the DEV-295 ticket text (Scope & Technical Design or acceptance
criteria) and none departs from the constitution. The DEV-294 spec correction (Q1) preserves both
tickets' deliverables; the FsCheck.Xunit.v3 dependency (Q9, F5) is a `PRODUCT.md` section 5 Patron ruling.
Keel's Q8 condition - whether "must go to OpenTelemetry spans" obligates emission here - was
examined and judged a **non-conflict** (placement prohibition, not an emission deliverable; the AC
names none), so it is recorded, not escalated. If Gate 1's reviewer disagrees, the user can raise it
as a checkbox then.

## Follow-ups (Patron decides; Rigger records; filing is not a plan change)

1. Wire classification, category-driven retry, and trace emission into the enrichment pipeline
   (`EnrichmentJobProcessor`/Core handlers, `EnrichmentOptions` max attempts, `RateLimited` TTL
   retry queue). Parent DEV-282. Carries constitution IV's retry and raw-text invariants.
2. DEV-294 spec/ADR 0014 amendment and file-ownership reconciliation (Q1).
3. ADR numbering-convention inconsistency (`ADR-0001/0002/0010` vs `0013`/`0014` vs this ticket's
   `ADR-0006`) - recorded for Patron; do not rename existing ADRs here.
4. When a database column or API field for the failure category is introduced, make `Code` the
   persisted/serialized contract value (none exists today; no schema change here).
