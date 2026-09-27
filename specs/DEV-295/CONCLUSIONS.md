# DEV-295 CONCLUSIONS

Every grill question, Patron's ruling, and its cited basis. Taste assumptions live in
`ASSUMPTIONS.md`; the frozen scope and plan decisions live in `brief.md`.

- **Ticket.** DEV-295, "Implement EnrichmentFailureCategory taxonomy and root-cause exception
  classifier". In Progress, size M, parent DEV-282, type feature, no `ui:` tag.
- **Worktree.** `F:/Dev/LamuFlix.worktrees/feature-295-spec`. Branch `feature/295-spec`.
- **Base.** `origin/main` at `baa1524` ("DEV-294 - Add Phase A specification artifacts and ADR 0014").
- **Feature pin.** `specs/DEV-295`.
- **Evidence.** Task note `DEV-295`; `recon-DEV-295` lines 1-47; ticket text fetched for DEV-295
  and DEV-294; `specs/PRODUCT.md`; `.specify/memory/constitution.md` v1.1.0; `CONTEXT.md`;
  `docs/adr/`; `src/LamuFlix.Worker/Services/EnrichmentJobProcessor.cs` and
  `OmdbMetadataProvider.cs`; `tests/LamuFlix.Test/EnrichmentTests.cs` and `WorkerTests.cs`;
  `Directory.Packages.props`; `harness.yml`.
- **Grill cap.** 12 questions, all answered. Q2 was folded into Q1. Patron's full ruling text is
  the source of record: `C:/Users/lamun/AppData/Local/Temp/DEV-295-Patron-rulings.txt`.

---

## Q1 - One canonical `EnrichmentFailureCategory` (taxonomy shape and ownership)

**Question.** DEV-294's merged `spec.md:38` defines `LamuFlix.Core.Domain.EnrichmentFailureCategory`
as a plain `enum` (`Unknown=0, ProviderUnavailable=1, RateLimited=2, InvalidResponse=3`).
DEV-294's ticket text names the type for `Movie.MarkFailed` and `Movie.LastFailureCategory` but
does not fix its shape. Should DEV-295's richer taxonomy be canonical and supersede it?

**Alternatives.** (i) Shape: `Ardalis.SmartEnum` package vs a sealed-record closed set.
(ii) Location: canonical type under `Core.Domain` vs a second type under `Core.Enrichment`
(`recon-DEV-295:14`, candidate only).

**Keel recommendation.** DEV-295's sealed-record closed set is canonical; it lives in
`Core.Domain`; no `Ardalis.SmartEnum` package; no second type; correcting DEV-294's spec is not a
ticket-text change, so no owner checkbox.

**Patron ruling: CHANGE - ACCEPT canonicalisation.**
- One canonical `EnrichmentFailureCategory`: a SmartEnum-style closed **sealed-record set** with
  `ProviderUnavailable`, `RateLimited`, `InvalidResponse`, `Unknown` and the properties `Code`,
  `SafeDescription`, `IsRetryable`.
- It stays in `LamuFlix.Core.Domain` so DEV-294's `Movie` can consume it. No `Ardalis.SmartEnum`
  package and no duplicate type under `Core.Enrichment`.
- DEV-294's `spec.md:38` and file ownership are reconciled **before either Phase B implementation**;
  DEV-294's owner amends its spec/ADR, and Rigger records the clarification on DEV-294.

**Basis (Patron).** DEV-295 ticket *Scope & Technical Design* 1 requires the shape and the three
properties. `constitution.md:157-159` (Principle IV: failures are classified into
`EnrichmentFailureCategory` (`ProviderUnavailable`, `RateLimited`, `InvalidResponse`, `Unknown`),
each with `IsRetryable` and a caller-safe message) and `constitution.md:304` (Technology Stack,
Category types: "SmartEnum-style closed sets (`Ardalis.SmartEnum` or sealed records); plain `enum`
only for `EnrichmentStatus`, `MovieSort`, `SortDirection`") override DEV-294 `spec.md:38`.
DEV-294's ticket text only names the type, so correcting its spec does not change what DEV-294
delivers. `PRODUCT.md` section 5.

**Owner checkbox: none.** §2.3 care-item ruling: Patron authorises a narrowly scoped correction
to an otherwise unnamed spec (`specs/DEV-294/spec.md:38`) by its owner. Both tickets' deliverables
are preserved, so it is neither a ticket-text change nor a plan add/drop/reorder.

---

## Q2 - Taxonomy shape and dependency

**Question.** Ticket Scope 1 says "Create SmartEnum or sealed record set".

**Keel recommendation / Patron ruling: folded into Q1 - ACCEPT.** Sealed-record closed set
(private constructor, static instances, value equality, `sealed`), so no `Ardalis.SmartEnum`
package is added. §2.3 care-list 1 avoided.

---

## Q3 - Locations and namespaces

**Question.** Recon candidate put both types in `src/LamuFlix.Core/Enrichment/`. Where do they live?

**Keel recommendation.** Taxonomy in `Core.Domain` (consumed by `Movie`); classifier in
`Core.Features.Enrichment` (the sanctioned feature folder).

**Patron ruling: ACCEPT.**
- `EnrichmentFailureCategory` -> `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs`,
  namespace `LamuFlix.Core.Domain`.
- `EnrichmentFailureClassifier` -> `src/LamuFlix.Core/Features/Enrichment/EnrichmentFailureClassifier.cs`,
  namespace `LamuFlix.Core.Features.Enrichment`.

**Basis (Patron).** The taxonomy is consumed by DEV-294's `Movie` per its ticket. Feature placement
of the classifier follows the constitution's feature structure (Principle I) and the
`ArchitectureTests` dependency rule (`ArchitectureTests.cs:69-96`, deps limited to own namespace +
Ports + Domain + Pipeline + `System`/`Microsoft.Extensions.Logging`). No new layer is introduced.

**Notes.** `src/LamuFlix.Core/Enrichment/` from `recon-DEV-295:14` is **not** used; it was a
candidate path only. `Core/Features/` exists with a `.gitkeep`.

---

## Q4 - Classifier API and root-cause semantics

**Question.** Signature, interface, null handling, and precedence.

**Keel recommendation.** `public static EnrichmentFailureCategory Classify(Exception exception)`;
no interface; `ArgumentNullException` on null; never returns null; chain scan outermost-to-innermost,
first recognized signal wins, else `Unknown`.

**Patron ruling: ACCEPT with precision.**
- `public static EnrichmentFailureCategory Classify(Exception exception)`.
- `ArgumentNullException` for a null argument; `Unknown` for otherwise unrecognized errors.
- Traverse `Exception`/`InnerException` outermost to innermost; the first recognized signal wins,
  with cycle protection if needed. This interprets ticket Scope 2's "single classification service
  mapping root-cause exceptions" while retaining a meaningful outer HTTP status.
- No interface and no state (no services abstraction; `PRODUCT.md` section 2, care-list 2).

**Basis (Patron).** Ticket Scope 2 ("Single classification service mapping root-cause
exceptions ... to `EnrichmentFailureCategory`"); `OmdbMetadataProvider.GetAsync` /
`EnsureSuccessStatusCode` for the outer-status signal; `PRODUCT.md` section 2 (no speculative
abstractions).

**Notes.** Caller cancellation is the explicit exception to category totality (Q6/Q9).

---

## Q5 - Mapping table

**Question.** Exact exception/status -> category rows.

**Keel recommendation.** 429 -> `RateLimited`; 5xx/408, `TimeoutException`, timeout-shaped
`TaskCanceledException`, `SocketException`/`IOException` -> `ProviderUnavailable`; `JsonException`
/malformed payload, 401/403/other non-429 4xx -> `InvalidResponse`; else `Unknown`.

**Patron ruling: CHANGE - ACCEPT the listed rows and add one.**
- `HttpRequestException.StatusCode == 429` -> `RateLimited`.
- `HttpRequestException.StatusCode` 5xx or 408, `TimeoutException`, timeout-shaped
  `TaskCanceledException`, `SocketException`, `IOException` -> `ProviderUnavailable`.
- **`HttpRequestException` with a null `StatusCode` -> `ProviderUnavailable`** (ordinary transport
  failure; the addition).
- `JsonException`/malformed payload, `StatusCode` 401, 403, and any other non-429 4xx ->
  `InvalidResponse`.
- Everything else -> `Unknown`.
- If an HTTP status is present, use it before an inner transport signal.

**Basis (Patron).** Ticket Scope 1 names 429, transient 5xx/network drop, malformed payload/401;
the remaining rows are classifier elaboration from `OmdbMetadataProvider.GetAsync` /
`EnsureSuccessStatusCode` and constitution IV.

---

## Q6 - Cancellation

**Question.** How does the classifier treat `OperationCanceledException`?

**Keel recommendation.** Rethrow a caller-cancelled `OperationCanceledException`; map only a
timeout-shaped cancellation to `ProviderUnavailable`.

**Patron ruling: ACCEPT.**
- A caller-cancelled `OperationCanceledException` whose `CancellationToken` is cancelled propagates
  (is not classified).
- Timeout-shaped cancellation maps to `ProviderUnavailable`.
- A bare `OperationCanceledException` with no cancelled token and no timeout signal -> `Unknown`.
- `Classify` returns a category for every non-null, non-caller-cancelled exception. Caller
  cancellation is the explicit exception to totality.

**Basis (Patron).** Constitution *Enrichment Reliability Rules* (graceful shutdown requeues
cancellation); misclassifying it as a failure would violate that rule.

---

## Q7 - `Code` and `SafeDescription`

**Question.** Values and wording.

**Patron ruling: ACCEPT.**
- `Code` is a stable lowercase snake_case contract value: `provider_unavailable`, `rate_limited`,
  `invalid_response`, `unknown`.
- `SafeDescription` is provider-agnostic, caller-safe, with no raw exception text, host, URL, or
  internal detail.
- Exact copy is **taste**; logged `[assumed]` in `ASSUMPTIONS.md`.

**Basis (Patron).** DEV-295 ticket Scope 1-2 and constitution IV's raw-text prohibition;
constitution VIII (functionality-based, English, no vendor names in Core).

---

## Q8 - Scope boundary

**Question.** Does DEV-295 wire the classifier, emit telemetry, or touch the pipeline?

**Patron ruling: ACCEPT with boundary.**
- DEV-295 ships the taxonomy, the classifier, `docs/adr/ADR-0006.md`, and unit/property tests.
- No Worker/provider/EF/schema edit, no OTel emitter, and no new telemetry dependency.
- Existing `EnrichmentJobProcessor.cs:67-69` logs the raw exception; ADR-0006 states that raw
  details go only to logs/traces and never to database columns or API responses.
- The DEV-295 ticket does not require new telemetry wiring and its AC lists only classifier,
  safe descriptions, and the ADR. Downstream integration must wire classification/retry and trace
  emission where appropriate; **do not claim new spans are implemented in DEV-295**.
- If Keel believes "must go to OpenTelemetry spans" obligates emission in this ticket, flag the
  exact ticket-text conflict before Gate 1.

**Basis (Patron).** Ticket AC and Scope 2; `PRODUCT.md` section 2; constitution IV
(classification separated from exception type; raw text never persisted).

**Keel's resolution of the flagged condition (recorded before Gate 1).** No genuine ticket-text
conflict. Scope 2's "must go to OpenTelemetry spans and logs ONLY" is a **placement prohibition**:
it forbids raw details reaching database columns or API responses and permits them in logs/traces.
It is satisfied in DEV-295 by construction: `SafeDescription` carries no raw detail, and the
classifier neither persists nor returns anything. The AC names no emission deliverable. Actual
span/log emission and retry wiring are downstream integration (follow-up 1), never claimed as
implemented here. This is recorded, not escalated.

---

## Q9 - Property tests and the Core test reference (§2.3 care-list 1)

**Question.** Does DEV-295 add FsCheck, or record a property-test opt-out?

**Keel recommendation.** (a) Add FsCheck + the Core `ProjectReference` + focused properties.

**Patron ruling: ACCEPT (a), no owner checkbox.**
- Add FsCheck centrally (`Directory.Packages.props`) and a `ProjectReference` from
  `tests/LamuFlix.Test/LamuFlix.Test.csproj` to `src/LamuFlix.Core/LamuFlix.Core.csproj`.
- Focused properties: closed set, non-blank safe fields, fixed retry semantics, and classifier
  outcomes across generated unrecognized exceptions.
- Do **not** state `Classify` is non-throwing for null or caller cancellation (Q4/Q6); test those
  separately.
- This is a deliberate §2.3 dependency ruling: `harness.yml:31-35` permits a property-test opt-out
  only for a ticket with no domain invariants, and the category/classifier have invariants.
- DEV-294's prior owner YES authorises its own FsCheck use but is not needed as owner approval
  here. `PRODUCT.md` section 5: Patron decides new dependencies, so this is a ruling, not a
  checkbox.

**Basis (Patron).** `harness.yml:31-35`; constitution IX (property tests, FsCheck); `PRODUCT.md`
section 5.

**Notes.** `Directory.Packages.props` currently has no FsCheck package; pin `FsCheck.Xunit.v3` `3.4.0` at
Phase B and record it. DEV-294 is not implemented yet, so DEV-295 must not rely on DEV-294 to supply
the package or the reference.

**F5 update (Patron ruling, plan challenge, 2026-09-26). CHANGE - use `FsCheck.Xunit.v3`.**
The property-test dependency is `FsCheck.Xunit.v3` **3.4.0** (with its transitive `FsCheck`), not
plain `FsCheck` or `FsCheck.Xunit`. **Basis.** Constitution IX tooling table (`constitution.md:257-265, 303`)
mandates the FsCheck xUnit integration and xUnit v3; `FsCheck.Xunit` 3.4.0 depends on xUnit **v2**,
whereas `FsCheck.Xunit.v3` 3.4.0 depends on `xunit.v3.extensibility.core >=4 <5`, matching the pinned
`xunit.v3` 4.0.1 (`Directory.Packages.props:20`). This is a version-specific realization of the mandated
tool, so it is **not a constitution departure and carries no owner checkbox**. Property tests use
`[Property]` plus `[Trait("Category", "Property")]`; Phase B must prove the native property gate
(`run-property-tests.ps1`) runs green. Sources: nuget.org/packages/FsCheck.Xunit.v3/,
nuget.org/packages/FsCheck.Xunit/.

---

## Q10 - Tests

**Question.** Paths, frameworks, coverage shape.

**Patron ruling: ACCEPT adjusted for Q5/Q6.**
- xUnit v3 and Shouldly from the existing CPM; no Testcontainers and no mocks (pure logic).
- Category tests under `tests/LamuFlix.Test/Domain/`.
- Classifier tests under `tests/LamuFlix.Test/Features/Enrichment/`.
- An exhaustive four-category Theory plus one case per mapping row, plus the null case and the
  cancellation cases.
- Assert category identity, `Code`, `IsRetryable`, and the safe description; test that raw exception
  messages/URLs never appear in the description.

**Basis (Patron).** Ticket AC; constitution IX testing rules; existing CPM
(`Directory.Packages.props:20,23`).

---

## Q11 - ADR-0006 and CONTEXT.md

**Question.** Exact path, title, status; and whether `CONTEXT.md` changes.

**Patron ruling: ACCEPT.**
- Path `docs/adr/ADR-0006.md` exactly as the ticket writes it.
- Title "0006. Failure categories separated from exception types; raw errors never leave logs".
- Status: `Accepted` at Phase B creation (the DEV-295 spec PR has already merged by then; never `Proposed`).
- Ticket DEV-295 (parent DEV-282).
- No `CONTEXT.md` edit: its **Failure Category** term (`CONTEXT.md:31`) already covers this; no new
  domain term is introduced. This avoids a §2.3 care-list 6 checkpoint.
- Do not rename ADR 0013/0014 (§2.3 care-list 6). Record the numbering-convention inconsistency as
  a follow-up candidate.

**Basis (Patron).** Ticket Scope 3; constitution *Documentation Rules* (ADR convention); §2.3
care-list 6 (unnamed files).

---

## Q12 - Closing bar, gates, and ordering

**Question.** Severity bar, round cap, gate set, Stryker, Phase B ordering.

**Patron ruling: ACCEPT, with hard gates made explicit (CHANGE only there).**
- M review path, scope, severity/follow-up treatment, and the two-round cap: accepted.
- Spec PR limited to `specs/DEV-295`; no `/web` changes.
- **Run `dotnet stryker` pre-delivery-PR and require the `harness.yml` mutation threshold of 80 for
  Core**; constitution IX and `AGENTS.md` pre-PR verification apply regardless of ticket size.
- All named gates need native exits; exit 2 scope-empty is blocking; opt-out only where valid.
- Phase B sequence: **DEV-295's canonical `Core.Domain` category lands before DEV-294's
  implementation consumes it**; coordinate the DEV-294 amendment and avoid competing file
  ownership. DEV-294's spec PR is already merged, but its Phase B must not implement the
  conflicting enum.
- No seat merges; the user merges PRs.

**Basis (Patron).** `harness.yml` (mutation threshold 80); constitution IX (mutation on
`LamuFlix.Core`); `AGENTS.md` pre-PR verification; cross-ticket coordination from Q1.

---

## Rulings summary

| Q | Topic | Ruling |
|---|---|---|
| Q1 | Canonical taxonomy, shape, ownership | ACCEPT canonicalisation (CHANGE): sealed-record set in `Core.Domain`; no Ardalis; no duplicate type; DEV-294 spec corrected; no owner checkbox |
| Q2 | Shape/dependency | Folded into Q1 |
| Q3 | Locations/namespaces | ACCEPT |
| Q4 | Classifier API/root-cause | ACCEPT with precision |
| Q5 | Mapping table | ACCEPT + null-status -> `ProviderUnavailable` |
| Q6 | Cancellation | ACCEPT |
| Q7 | `Code`/`SafeDescription` | ACCEPT; copy is taste `[assumed]` |
| Q8 | Scope boundary | ACCEPT with boundary; no ticket-text conflict (recorded) |
| Q9 | FsCheck.Xunit.v3 + Core reference | ACCEPT (a); F5: `FsCheck.Xunit.v3` 3.4.0 (xUnit v3); Patron ruling, no checkbox |
| Q10 | Tests | ACCEPT adjusted for Q5/Q6 |
| Q11 | ADR-0006 / CONTEXT.md | ACCEPT |
| Q12 | Closing/gates/order | ACCEPT + Stryker required; DEV-295 lands taxonomy first |

**Owner checkboxes: none.** No question changed the ticket text or departed from the constitution.
The DEV-294 spec correction (Q1) and the FsCheck.Xunit.v3 dependency (Q9, F5) are Patron rulings recorded here.
