# DEV-295 Phase A grill questions (Keel to Patron)

Worktree `F:/Dev/LamuFlix.worktrees/feature-295-spec`; branch `feature/295-spec`; base
`origin/main` baa1524; feature pin `specs/DEV-295`.

Evidence: task note `DEV-295`, `recon-DEV-295` lines 1-47, DEV-295 and DEV-294 ticket text,
`specs/PRODUCT.md`, `.specify/memory/constitution.md` v1.1.0, `CONTEXT.md`, `docs/adr`, Worker
services, tests, `Directory.Packages.props`, `harness.yml`.

Q2 (shape) is folded into Q1. Rule each ACCEPT or CHANGE with basis; Keel logs each in
`specs/DEV-295/CONCLUSIONS.md`.

---

**Q1 (taxonomy canonicalisation; decide first).** DEV-294's merged `spec.md:38` defines
`LamuFlix.Core.Domain.EnrichmentFailureCategory` as a plain `enum`
(`Unknown=0, ProviderUnavailable=1, RateLimited=2, InvalidResponse=3`). DEV-294's ticket text names
the type for `Movie.MarkFailed` and `Movie.LastFailureCategory` but does not fix its shape.
Constitution basis: Technology Stack, Category types - "SmartEnum-style closed sets
(`Ardalis.SmartEnum` or sealed records); plain `enum` only for `EnrichmentStatus`, `MovieSort`,
`SortDirection`" (`constitution.md:304`), and Principle IV requires each category to carry
`IsRetryable` and a caller-safe message (`constitution.md:157-159`). DEV-295 ticket Scope 1
requires exactly that shape and those three properties. Alternatives: (i) shape - `Ardalis.SmartEnum`
package vs a sealed-record closed set; (ii) location - canonical type under `Core.Domain`
(DEV-294's location) vs a second type under `Core.Enrichment` (`recon-DEV-295:14`, candidate only).
Keel recommendation: DEV-295's sealed-record closed set is canonical and supersedes DEV-294's
plain-enum shape; it lives in `Core.Domain`; no `Ardalis.SmartEnum` package (care-list 1 avoided);
no second type. Because DEV-294's ticket text does not name the shape, this is a correction of a
stale, non-constitutional spec by its owner (Rigger records it on DEV-294), not a ticket-text
change and not a plan add/drop/reorder: no owner checkbox. Rule, and state explicitly whether you
judge it an owner checkbox under PRODUCT.md section 5 / task-pipeline section 2.2.

**Q2.** Folded into Q1 (shape and dependency).

**Q3 (locations).** Q1 fixes the taxonomy in `Core.Domain`. Confirm
`EnrichmentFailureCategory` -> `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs`, namespace
`LamuFlix.Core.Domain`. For `EnrichmentFailureClassifier`, choose (a)
`src/LamuFlix.Core/Features/Enrichment/`, namespace `LamuFlix.Core.Features.Enrichment`, where
ArchitectureTests enforces deps limited to own namespace + Ports + Domain + Pipeline +
System/M.E.Logging, or (b) `src/LamuFlix.Core/Domain/` beside the taxonomy as a pure domain
service. Keel recommends (a). Rule.

**Q4 (classifier API and root-cause semantics).** Keel recommendation:
`public static EnrichmentFailureCategory Classify(Exception exception)`; no interface (care-list 2,
PRODUCT.md section 2); `ArgumentNullException` on null; never returns null; scan the exception
chain outermost-to-innermost, first recognized signal wins, else `Unknown`. Rule the signature
(static vs instance) and the precedence.

**Q5 (mapping table).** From ticket Scope 1 descriptions and the provider code. `RateLimited` =
`HttpRequestException.StatusCode` 429. `ProviderUnavailable` = `StatusCode` 5xx or 408,
`TimeoutException`, `TaskCanceledException` with a `TimeoutException` inner,
`SocketException`/`IOException` (network drop). `InvalidResponse` = `JsonException` or malformed
payload, `StatusCode` 401 (ticket), 403, any other non-429 4xx. `Unknown` = everything else. Rule
the table, adding or removing rows, so it is testable and mutatable.

**Q6 (cancellation).** A caller-cancelled `OperationCanceledException` is not a metadata failure
and must not mark a movie `Failed` (graceful shutdown). Keel recommendation: a bare
`OperationCanceledException` whose token is cancelled is rethrown, not classified; only a
timeout-shaped cancellation maps to `ProviderUnavailable`. Rule (rethrow vs `Unknown`) and the
resulting totality statement for the AC.

**Q7 (Code and SafeDescription).** Ticket Scope 1 names `Code` and `SafeDescription`. Keel
recommendation: `Code` is a stable lowercase snake_case contract value (`provider_unavailable`,
`rate_limited`, `invalid_response`, `unknown`); `SafeDescription` is provider-agnostic caller-safe
copy with no exception text, host, URL, or internal detail (ticket Scope 2 and constitution VIII).
Keel logs `Code` as a ruling and `SafeDescription` wording as taste `[assumed]`. Rule the `Code`
format; assume the copy.

**Q8 (scope boundary).** Ticket AC lists only classifier unit tests, caller-safe descriptions, and
the ADR. Keel recommendation: DEV-295 ships only `EnrichmentFailureCategory`,
`EnrichmentFailureClassifier`, `docs/adr/ADR-0006.md`, and unit tests. No edit to
`EnrichmentJobProcessor`, `OmdbMetadataProvider`, `MovieService`, `Data.Models`, `LamuFlixContext`,
or the schema; no OTel emitter code and no new telemetry dependency; "raw details to OTel and logs
only" is an invariant documented in ADR-0006 and guaranteed by `SafeDescription` carrying no raw
detail, with emission and retry wiring deferred downstream (Principle IV). Rule, and say whether
raw-exception emission is in scope.

**Q9 (property tests and the Core test reference).** `harness.yml` lines 31-35 and the refactor
gate require property tests for domain logic and forbid an opt-out when invariants exist;
`Directory.Packages.props` has no FsCheck; DEV-294's owner already authorised FsCheck but that
Phase B has not run; `tests/LamuFlix.Test/LamuFlix.Test.csproj` does not reference `LamuFlix.Core`.
Options: (a) DEV-295 adds FsCheck (PackageVersion + PackageReference), a small property suite
(Code/SafeDescription non-blank, IsRetryable matches the ticket, Classify total and non-throwing),
and the Core ProjectReference; (b) DEV-295 records `propertyTests: opt-out` in the task note. Keel
recommends (a). Per PRODUCT.md section 5 a new dependency is a Patron ruling, not an owner
checkbox; rule, and say whether you judge it a checkbox given DEV-294's precedent.

**Q10 (tests).** Keel recommendation: xUnit v3 + Shouldly from the existing CPM, no
Testcontainers/NSubstitute/WireMock (pure); test files mirror namespaces:
`tests/LamuFlix.Test/Features/Enrichment/EnrichmentFailureClassifierTests.cs` and
`tests/LamuFlix.Test/Domain/EnrichmentFailureCategoryTests.cs`; an exhaustive Theory over the 4
categories plus one case per mapping row, each asserting category, Code, IsRetryable,
SafeDescription. Rule the paths and the coverage/evidence shape.

**Q11 (docs).** Ticket Scope 3 names `docs/adr/ADR-0006.md`. Keel recommendation: use that path
exactly, title "0006. Failure categories separated from exception types; raw errors never leave
logs", Status "Proposed (Gate 1 closed; becomes Accepted when the DEV-295 spec PR merges)" per the
DEV-294 ADR 0014 precedent, Ticket DEV-295 (parent DEV-282). Do not rename ADR 0013/0014
(care-list 6); record the numbering-convention inconsistency as a follow-up candidate.
`CONTEXT.md:31` already defines "Failure Category - a caller-safe taxonomy" and DEV-295 introduces
no new domain term, so no `CONTEXT.md` edit (avoids a care-list 6 checkpoint). Rule both.

**Q12 (closing, gates, ordering).** Blocking = verified in-scope Critical/High fixed before PR;
Medium/Low and out-of-scope become follow-ups with severity intact; 2 review rounds x at most 2 fix
commits; M path = pre-pass + Sentry/Ledger/Compass, Keel adjudicates; no `/web` changes. Gates:
run-roslyn-analyzers, run-cyclomatic-complexity (15, then refactor `-Threshold 6`),
run-jetbrains-inspectcode, `dotnet format --verify-no-changes`, run-vulnerable-packages,
`dotnet test`, `dotnet build`, run-property-tests if Q9(a). Rule whether DEV-295 must run
`dotnet stryker` pre-PR despite M (constitution IX puts the 80 threshold on `LamuFlix.Core`; the
change is two small pure Core files) and the Phase B ordering versus DEV-294 (which ticket lands
the canonical `Core.Domain` taxonomy first). Spec PR: `gate1: provisional`, diff `specs/DEV-295`
only; the DEV-294 correction from Q1 lands as a Rigger-recorded amendment/comment on DEV-294, not
in this PR.

---

Facts come from `recon-DEV-295`. If you need a recon fact that is not there, rule on the rest and
reply `needs recon: <question>`.
