# Feature Specification: Chain Metadata Providers

**Feature Branch**: `feature/423-spec`
**Created**: 2026-10-26
**Status**: gate1: provisional (Patron, 2026-10-26; awaiting owner spec PR review/merge; Phase B STOP preserved on DEV-331 owner Q7 resolution and implementation landing T000/T001). See PLAN_CHALLENGE_ADJUDICATION.md final receipt.
**Input**: Ticket DEV-423 (parent DEV-100, Size M; depends DEV-331); frozen `brief.md` (Q1–Q7 + PC1–PC6 appendix); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-423`, `recon-DEV-423`, `recon-DEV-423-2`; `specs/PRODUCT.md`

## Scope

**In scope** (ticket Scope & Technical Design items 1–5 and AC 1–3; brief Q1–Q5):

- `ChainedMetadataProvider : IMetadataProvider` in `src/LamuFlix.Infrastructure/Adapters/`, constructed with an explicitly ordered `IReadOnlyList<IMetadataProvider>` (no `IServiceProvider`, no unkeyed `IEnumerable<IMetadataProvider>` enumeration).
- Documented fallback: advance on `NotFound`, `Failed(RateLimited)`, or `Failed(ProviderUnavailable)`; stop on other `Failed` categories; first `Found` wins; exhaustion returns first remembered eligible failure else `NotFound`.
- `AddMetadataProvider` replaces the OMDb port-typed `AddHttpClient<IMetadataProvider, OmdbMetadataProvider>` with concrete `AddHttpClient<OmdbMetadataProvider>`; retains the landed DEV-331 TMDb typed client, BaseUrl trailing-slash normalization, independent resilience, and secondary health registrations verbatim; adds keyed transient `IMetadataProvider` forwards named `omdb` / `tmdb` (from `TelemetryConstants`); sole unkeyed `IMetadataProvider` is a transient factory-built chain (`[omdb]` when `TmdbOptions.Enabled=false`, `[omdb, tmdb]` when true) resolving each entry exclusively via `GetRequiredKeyedService<IMetadataProvider>` (PC4).
- Disabled TMDb composition omits TMDb from the chain list (zero secondary resolve/invoke/HTTP); existing enrichment tests pass unchanged.
- Table-driven unit tests with fake providers covering the full primary/secondary result matrix; integration composition/observability/resilience coverage with WireMock — never live vendors.
- Bounded create/edit file set named in Q5 / FR-008 (PC2/PC5 path and assertion amendments).

**Out of scope** (ticket; brief Q1, Q4, Q5):

- Building or modifying the TMDb adapter, `TmdbOptions`, secondary health types, TMDb telemetry constant, or TMDb resilience — those are DEV-331 deliverables.
- Merging fields across providers; any new Core port, result/category field, options record, secret surface, public API/DTO/OpenAPI/TypeScript, schema/migration, project, layer, or NuGet/npm package.
- `Features:LocalPlay` / `Process.Start`; constitution or ADR edits in this ticket (upstream DEV-331 owner Q7 remains the constitution authority).
- README, C4, or `CONTEXT.md` changes; new domain glossary term; Gherkin unless separately opted in.
- Aggregate chain-level health verdict, chain-level span/histogram, or chain-level resilience/timeout/retry/breaker.

**Precedence**: Ticket Scope & Technical Design is already approved authority. Brief Q1–Q7 rulings plus Phase 2 PC1–PC6 amendments govern composition, fallback, observability, files, tests, gates, and loop discipline. Phase A freezes against the specified DEV-331 contract; Phase B requires owner Q7 resolution, amendment/ADR, DEV-331 implementation landing, and a mandatory pickup drift check — no duplicate DEV-423 constitution checkbox and no silent clearance of the upstream departure.

**Upstream prerequisite (not a DEV-423 owner checkbox)**: Phase B implementation is blocked until DEV-331’s owner constitution resolution and implementation have landed, followed by pickup drift/analyze against main. Drafting claims no implementation readiness.

## User Scenarios & Testing

### User Story 1 - Documented Fallback and First Found Wins (Priority: P1)

As the enrichment pipeline, I need a single `IMetadataProvider` that asks OMDb first and TMDb second under a documented fallback rule, so lookups recover from primary miss or transient unavailability without hiding bad primary data behind a second vendor.

**Why this priority**: ticket items 1–2 and AC 1–2 — the core deliverable.

**Independent Test**: unit Theory/MemberData with two fake providers covering Found / NotFound / five Failed categories as primary and secondary; assert call counts, returned result, and no secondary call after Found or terminal failure.

**Acceptance Scenarios**:

1. **Given** primary `Found` **When** the chain runs **Then** that result is returned and the secondary provider is not called (ticket AC; Q3).
2. **Given** primary `NotFound` and secondary `Found` **When** the chain runs **Then** secondary is called and its `Found` is returned (ticket AC; Q3).
3. **Given** primary `Failed(RateLimited)` or `Failed(ProviderUnavailable)` **When** the chain runs **Then** secondary is called (ticket AC; Q3).
4. **Given** primary `Failed(InvalidResponse)`, `Failed(Unknown)`, or any other non-eligible `Failed` including defensive `DuplicateImdbId` **When** the chain runs **Then** that failure is returned immediately and secondary is not called (ticket item 2; Q3).
5. **Given** both providers exhausted with at least one eligible failure remembered **When** the chain finishes **Then** the first eligible `Failed` in provider order is returned (Q3).
6. **Given** both providers exhausted with only `NotFound` outcomes **When** the chain finishes **Then** `NotFound` is returned (Q3).
7. **Given** primary eligible failure then secondary terminal failure **When** the chain runs **Then** the secondary terminal failure is returned (later stop overrides remembered eligible failure) (Q3).

### User Story 2 - Keyed Individual Providers and Sole Unkeyed Chain (Priority: P1)

As a maintainer, I need individual providers registered under DI keys `omdb` and `tmdb`, with the enrichment handlers resolving a single unkeyed chain, so composition is explicit, non-recursive, and RabbitMQ consumer activation still sees an `IMetadataProvider` descriptor.

**Why this priority**: ticket item 3; load-bearing registration order and consumer guard (Q2).

**Independent Test**: composition tests resolve keyed providers and sole unkeyed `ChainedMetadataProvider`; assert RabbitMQ activation guard still sees unkeyed port registration; `AddMetadataProvider` remains before `AddLamuFlixRabbitMq`.

**Acceptance Scenarios**:

1. **Given** `AddMetadataProvider` **When** resolving keyed `"omdb"` / `"tmdb"` **Then** each forwards to its transient concrete typed client (`OmdbMetadataProvider` / `TmdbMetadataProvider`) (Q2).
2. **Given** `AddMetadataProvider` **When** resolving unkeyed `IMetadataProvider` **Then** the instance is `ChainedMetadataProvider` (sole unkeyed registration) (Q2).
3. **Given** registration **Then** concrete typed clients stay transient; the chain is transient; typed clients are never captured by a singleton (Q2).
4. **Given** `Program.cs` order **Then** `AddMetadataProvider` still precedes `AddLamuFlixRabbitMq` and `HasRegistration<IMetadataProvider>` remains true (Q2).
5. **Given** the chain type **Then** it accepts only `IReadOnlyList<IMetadataProvider>`; constructor rejects null with `ArgumentNullException` and empty with `ArgumentException`, snapshots via `ToArray()` so later caller-list mutation cannot change order, and never captures an unguarded mutable list through a raw primary-constructor parameter (Q2, Q5; PC3).

### User Story 3 - Disabled TMDb Behaves as Today (Priority: P1)

As an operator with TMDb disabled (the DEV-331 default), I need the chain to contain OMDb alone so enrichment behaviour and existing tests remain unchanged until TMDb is deliberately enabled.

**Why this priority**: ticket item 4 and AC 3.

**Independent Test**: composition with `TmdbOptions.Enabled=false`; assert chain list is `[omdb]` only, zero TMDb resolve/invoke/HTTP; existing enrichment regression suite passes unchanged.

**Acceptance Scenarios**:

1. **Given** `TmdbOptions.Enabled=false` **When** the factory builds the chain **Then** the ordered list is OMDb only and TMDb is neither resolved nor invoked (ticket item 4; Q2).
2. **Given** disabled composition **When** existing enrichment tests run **Then** they pass unchanged (ticket AC 3; Q5).
3. **Given** disabled composition **When** a lookup completes **Then** the returned OMDb result is identical to pre-chain single-provider behaviour for that OMDb outcome (Q3).
4. **Given** disabled composition **Then** concrete TMDb registration and its DEV-331 health check remain available as specified upstream; they are simply omitted from the chain list (Q2).

### User Story 4 - Transparent Observability, Health, and Resilience (Priority: P2)

As an operator, I need each adapter to keep its own spans, duration measurements, health snapshots, and circuit breakers, with the chain adding none of those, so primary failures stay visible and one provider’s outage cannot poison the other through nested policies.

**Why this priority**: Q4; preserves DEV-331/OMDb contracts without chain-level aggregation.

**Independent Test**: integration cases with enabled fallback assert two `Metadata.Lookup` spans and two enrichment-duration samples (`provider=omdb` then `provider=tmdb`); disabled mode leaves existing one-provider telemetry tests unchanged; health check names/tags/readiness exclusion unchanged; independent breakers verified.

**Acceptance Scenarios**:

1. **Given** an invoked adapter **Then** that adapter alone opens `Metadata.Lookup` and records one enrichment-duration measurement with its provider tag; the chain opens no Activity and records no histogram (Q4).
2. **Given** enabled fallback that invokes both adapters **Then** two spans and two duration measurements are emitted (`omdb` then `tmdb`) (Q4).
3. **Given** disabled TMDb **Then** only OMDb telemetry fires and existing single-provider telemetry tests remain unchanged (Q4; Q5).
4. **Given** primary failure then secondary success **Then** primary health snapshot is not overwritten by secondary success; check ids `metadata-provider` and `metadata-provider-tmdb`, descriptions, untagged status, and `/health/ready` exclusion stay as specified (Q4).
5. **Given** both typed clients **Then** each retains independent `AddStandardResilienceHandler` from shared `Omdb:Resilience` with isolated breaker state; the chain adds no resilience, timeout budget, retry, or breaker (Q4).

### User Story 5 - Lookup Pass-Through and Exception Propagation (Priority: P2)

As the enrichment handler, I need the chain to forward the same lookup (including `ManualImdbId`) and cancellation token unchanged, and to propagate caller cancellation and unexpected exceptions without catch/reclassify or exception-based fallback, so retry policy and cancellation semantics stay adapter- and handler-owned.

**Why this priority**: Q3 pass-through rules; protects consumer retryability.

**Independent Test**: fake providers assert identical lookup/token identity; cancellation and unexpected exception cases prove no catch/reclassify and no fallback on thrown exceptions.

**Acceptance Scenarios**:

1. **Given** any configured provider call **Then** the original `MetadataLookup` instance (including `ManualImdbId`) and `CancellationToken` are forwarded unchanged (Q3).
2. **Given** caller cancellation **Then** `OperationCanceledException` propagates; the chain neither catches nor reclassifies it (Q3).
3. **Given** an unexpected exception from a provider **Then** it propagates; the chain does not fall back on thrown exceptions (Q3).
4. **Given** primary `Failed(ProviderUnavailable)` then secondary `NotFound` **Then** exhaustion returns the eligible primary failure so enrichment retry policy remains retryable rather than marking the movie permanently NotFound (Q3).

### Edge Cases

- One-provider disabled chain returns the same OMDb result shape as today’s single registration (Q3).
- Both eligible failures preserve OMDb precedence (first remembered eligible failure) (Q3).
- Later terminal failure after a remembered eligible failure wins immediately (Q3; PC1). **Accepted cost**: secondary `InvalidResponse` / `DuplicateImdbId` after a remembered eligible primary failure can route the enrichment handler through non-retryable `Decide` and immediately dead-letter rather than retry — bad data must remain visible (Sentry M1 / PC1).
- `Failed(Unknown)` is terminal for chain advancement (ticket item 2 / Q3) yet remains `IsRetryable=true` downstream in `EnrichmentRetryPolicy` — accepted cost; no algorithm change (PC1 / Sentry L1).
- End-to-end latency may consume both provider resilience budgets when fallback runs — accepted cost; no elapsed-time ceiling test required (Q4; Sentry L2).
- RateLimited fan-out doubles vendor load during partial outage by ticket design; chain bulkhead/cap is out of scope — noted, no ticket (Q6; Sentry L3).
- No aggregate health verdict; primary 401 (`InvalidResponse`) stops fallback and keeps primary Unhealthy advice visible (Q3, Q4).
- Adapter-specific manual-id/search behaviour remains OMDb/DEV-331 responsibility; unit ManualImdbId identity plus real typed-client fallback suffice — no added WireMock manual-id case (Q3; Sentry L6).
- Factory-owned ordered list needs no duplicate-entry guard (PC4 / Sentry L4).
- Any newly discovered file requirement or broader composition-assertion drift needs an explicit cited decision amendment before edits (Q5; PC5).

## Requirements

### Functional Requirements

- **FR-001**: Create `public sealed class ChainedMetadataProvider` in `src/LamuFlix.Infrastructure/Adapters/ChainedMetadataProvider.cs` implementing Core `IMetadataProvider.FindAsync`. Constructor takes `IReadOnlyList<IMetadataProvider>` only; reject `null` with `ArgumentNullException` and empty with `ArgumentException`; snapshot the list via `ToArray()` after the guards so later caller-list mutation cannot change chain order. Do not prescribe a raw-capture primary constructor incompatible with those guards. No mutable `static` initialisation in the chain type. No `IServiceProvider` or unkeyed `IEnumerable<IMetadataProvider>` enumeration inside the chain (Q2, Q5; PC3).
- **FR-002**: Fallback algorithm (Q3; PC1; PC3): `Found` → return immediately; `NotFound` → continue; eligibility for remembered-failure advancement is exact record equality to `EnrichmentFailureCategory.RateLimited` or `EnrichmentFailureCategory.ProviderUnavailable` only — every other current category and any future category is terminal and returns immediately (including `InvalidResponse`, `Unknown`, defensive `DuplicateImdbId`); providers exhausted → return first remembered eligible failure if present, otherwise `NotFound`. Never merge fields. No catch-all that silently treats a future category as eligible.
- **FR-003**: Forward the original `MetadataLookup` (including `ManualImdbId`) and `CancellationToken` unchanged to each invoked provider. Perform no `try`/`catch`, internal retry, or fallback on thrown exceptions; caller cancellation and unexpected exceptions propagate (Q3).
- **FR-004**: `AddMetadataProvider` authors exactly (a) replacement of OMDb’s port-typed `AddHttpClient<IMetadataProvider, OmdbMetadataProvider>` with concrete `AddHttpClient<OmdbMetadataProvider>`, (b) keyed transient `IMetadataProvider` forwards using `TelemetryConstants.OmdbProviderName` (`"omdb"`) and `TelemetryConstants.TmdbProviderName` (`"tmdb"`), and (c) sole unkeyed transient factory building `ChainedMetadataProvider` with `[omdb]` when `TmdbOptions.Enabled=false` and `[omdb, tmdb]` when true, resolving each list entry exclusively via `GetRequiredKeyedService<IMetadataProvider>` (no unkeyed recursion; no duplicate-entry guard). Retain the landed DEV-331 `AddHttpClient<TmdbMetadataProvider>` registration, its BaseUrl trailing-slash normalization lambda, independent resilience, and secondary health registrations verbatim — do not re-author or duplicate them. Preserve the private shared `Configure` helper bound to `MetadataProviderResilienceOptions` (`"Omdb:Resilience"`) and unrelated existing comments. Disabled composition must not resolve or invoke TMDb. Preserve `AddMetadataProvider` before `AddLamuFlixRabbitMq` and the unkeyed port descriptor for RabbitMQ activation. Never capture typed clients in a singleton (Q2; PC4).
- **FR-005**: Chain is telemetry-, health-, and resilience-transparent: opens no `Activity`, records no `EnrichmentDuration` or health snapshot, adds no `HttpClient`, resilience handler, timeout budget, retry, or circuit breaker. Each adapter retains independent standard resilience via the shared private `Configure` helper and `MetadataProviderResilienceOptions` (`"Omdb:Resilience"`), isolated breaker/health, and per-attempt `Metadata.Lookup` + enrichment-duration with its provider tag. One invoked adapter → one span/measurement; both invoked → two. Existing disabled-mode single-provider telemetry tests remain unchanged. Health check names `metadata-provider` / `metadata-provider-tmdb`, descriptions, untagged status, and readiness exclusion unchanged; no aggregate health verdict (Q4; PC4).
- **FR-006**: Unit tests in `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs`, namespace `LamuFlix.UnitTests.Adapters`, class `MetadataProviderChainTests` (vendor-neutral name; PC2): Theory/MemberData full matrix currently 7×7 (Found, NotFound, and Failed categories derived from `EnrichmentFailureCategory` so a new category expands coverage rather than silently using a hand-copied list); secondary call suppression after Found or terminal failures; exhaustion precedence; lookup/token identity including ManualImdbId; exception/cancellation propagation; null → `ArgumentNullException` and empty → `ArgumentException` constructor rejection; caller-list-mutation after construction does not change order; one-provider equivalence (ticket item 5; Q5; PC3).
- **FR-007**: Integration tests in `tests/LamuFlix.IntegrationTests/MetadataProviderChainCompositionTests.cs` reuse xUnit/Shouldly/WireMock and shipped composition: keyed `"omdb"`/`"tmdb"` resolution under both `TmdbOptions.Enabled` modes (separate from disabled-chain construction); sole-unkeyed resolution; disabled zero secondary resolve/invoke/HTTP; real typed-client fallback and first-Found short circuit; per-provider spans/duration; health isolation; independent breakers; RabbitMQ activation and readiness exclusion. Local fake HTTP only — never live vendors. Existing enrichment regressions pass unchanged with TMDb disabled (Q5; PC6).
- **FR-008**: Edit only `src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs` (OMDb concrete typed client replacement, keyed forwards, sole unkeyed factory; TMDb client/normalization/resilience/secondary health retained verbatim) and, in `tests/LamuFlix.IntegrationTests/MetadataProviderCompositionTests.cs` plus conditionally `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderCompositionTests.cs`, only type or behaviour assertions that pin unkeyed `IMetadataProvider` to bare `OmdbMetadataProvider` (retarget those to the chain); preserve all unrelated health/telemetry/resilience assertions. Broader drift requires a cited ruling before edits. Create only the three files in FR-001/FR-006/FR-007 (unit path under `Adapters/` per PC2). Document fallback in this DEV-423 packet. Touch no Core/options/config/secret/API/schema/telemetry-constant/health-type/README/C4/`CONTEXT.md` files (Q5; PC4; PC5).
- **FR-009**: Phase B ordering: (1) prove upstream owner resolution, amendment/ADR, and DEV-331 implementation landed; (2) mandatory main pickup drift check and read-only speckit-analyze, amend decisions/tasks only if required; (3) chain unit tests and implementation; (4) DI wiring and narrow composition assertion changes; (5) integration and observability/resilience regressions; (6) required verification and three-axis review. Optional Gherkin not selected (Q1, Q5).
- **FR-010**: Phase B verification: scoped Roslyn/analyzer, cyclomatic-complexity (then refactor threshold from harness), InspectCode; appropriate unit/integration suites plus unchanged enrichment regressions; property/security/vulnerability/mutation and other harness pre-PR checks without hardcoding or lowering thresholds. Mutation via `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from a worktree. Exit 0 = Pass; exit 1 = Failure; exit 2 retains SKIPPED (scope-empty) / SKIP / mutation NOT APPLICABLE as reported; Could not run is never Pass. Closing bar: Critical/High/Medium in-scope block code-review/ship-review; Low non-blocking unless concrete higher severity. Size M requires Sentry, Ledger, and Compass. Plan-challenge/fix and remediation caps: two rounds each; stop blocked at cap (Q5, Q6).

### Key Entities

- **`ChainedMetadataProvider`**: Infrastructure decorator over ordered `IMetadataProvider` instances; vendor-neutral; owns fallback algorithm only.
- **`IMetadataProvider` / `MetadataLookup` / `MetadataLookupResult` / `EnrichmentFailureCategory`**: existing Core port and result hierarchy — unchanged.
- **Keyed provider registrations**: DI keys `"omdb"` and `"tmdb"` forwarding to concrete typed clients.
- **`TmdbOptions.Enabled`**: upstream DEV-331 option; when false, chain list is OMDb-only (consumed, not defined here).

## Success Criteria

- **SC-001**: OMDb `NotFound` leads to a TMDb call; OMDb `Found` makes no TMDb call (ticket AC).
- **SC-002**: OMDb `RateLimited` falls back to TMDb; OMDb `InvalidResponse` does not (ticket AC).
- **SC-003**: With TMDb disabled, existing enrichment tests pass unchanged (ticket AC).
- **SC-004**: Unit matrix proves exhaustion returns first eligible failure when mixed with misses, and one-provider disabled equivalence holds (Q3, Q5).
- **SC-005**: Composition proves keyed `omdb`/`tmdb`, sole unkeyed `ChainedMetadataProvider`, preserved RabbitMQ activation, and readiness exclusion unchanged (Q2, Q4).
- **SC-006**: Enabled fallback emits two provider-tagged spans/duration samples; disabled mode leaves existing one-provider telemetry tests untouched; primary health is not overwritten by secondary success (Q4).
- **SC-007**: Analyzer, complexity, InspectCode, format, and required test gates pass on the change set; skipped/unrunnable gates are never reported as PASS (Q5, Q6).

## Assumptions

- No taste decisions; provider order and `Enabled=false` default come from the ticket and DEV-331 contract (`ASSUMPTIONS.md`).
- DEV-331 owner Q7 constitution resolution and implementation landing are an unresolved external prerequisite, not an assumption and not silently cleared (`ASSUMPTIONS.md`; Q1).
- WireMock and fakes stand in for live vendors; no live OMDb/TMDb network in tests (Q5).
- Documenting the fallback rule in this packet satisfies the ticket’s “documented fallback” without README/C4/`CONTEXT` edits (Q5).

## Clarifications

All grill categories were resolved by Patron Q1–Q7 before drafting; Phase 2 PC1–PC6 amendments supersede conflicting Q2/Q5 path/assertion summaries only. `/speckit-clarify` scan: Functional Scope, Domain/Data, Interaction, NFR (observability/resilience/security), Integration/Dependencies, and Out-of-scope — **Clear**. No `[NEEDS CLARIFICATION]` markers remain. Remaining open item is the external DEV-331 Phase B prerequisite (Q1), not a drafting ambiguity.

## Traceability

| Source | Encoded as |
|---|---|
| Ticket items 1–5, AC 1–3 | US1–US3; FR-001–FR-008; SC-001–SC-003 |
| Q1 phase/upstream | Scope precedence; FR-009; Assumptions; T000/T001 |
| Q2 DI/port | US2–US3; FR-001, FR-004 |
| Q3 fallback/pass-through | US1, US5; FR-002, FR-003; Edge Cases (PC1) |
| Q4 observability | US4; FR-005; SC-006 |
| Q5 files/tests/gates (+ PC2/PC5 path/assertion amendments) | FR-006–FR-010; SC-004–SC-007 |
| Q6 loop discipline | FR-010 closing bar / round caps |
| Q7 brief freeze | Phase A brief frozen for drafting only |
| PC1 later-terminal / Unknown cost | US1-7; FR-002; Edge Cases; plan Risks |
| PC2 unit path/namespace | FR-006; plan Project Structure; T002/T004 |
| PC3 eligibility/guards/snapshot/matrix | FR-001, FR-002, FR-006; plan Approach; T002/T003 |
| PC4 OMDb authored / TMDb retained / GetRequiredKeyedService | Scope; FR-004, FR-005, FR-008; plan Approach; T001/T005 |
| PC5 bare-OMDb assertion narrow edit | FR-008; plan file set; T007/T008 |
| PC6 keyed both modes / Scope FR-008 / T002 no `[P]` | FR-007; Scope; T002/T009/T010 |
