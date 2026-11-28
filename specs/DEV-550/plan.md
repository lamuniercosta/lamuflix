# Implementation Plan: HTTP client outbound address guard and architecture rules

**Branch**: `feature/550-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-550-spec`) | **Date**: 2026-11-28 | **Spec**: `specs/DEV-550/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q6); `CONCLUSIONS.md`; Maestri notes `DEV-550`, `recon-DEV-550`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft — Quill drafting only. Gate 1, Build, and runtime provider receipts remain pending. Quill does not declare PLAN FROZEN.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` — grill Q1–Q6 freeze inventable unknowns; no schema or public API change. No `ASSUMPTIONS.md` — brief records no taste assumptions.

## Summary

Install one shared outbound connect-time address guard on every named and typed HTTP client through `ConfigureHttpClientDefaults` in Infrastructure, enforce mandatory and OpenSubtitles-stricter deny sets at dial time on direct HTTP/2-or-lower transports, prove registration and behaviour with dynamic DI discovery plus controlled seams, and add two NetArchTest rules (helper namespaces; Persistence-only EF with two exact type exceptions). Delivery closes with exact-head C# gates and separate redacted Compose and home-cluster provider receipts.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Infrastructure`, `LamuFlix.ArchitectureTests`, `LamuFlix.IntegrationTests`, `LamuFlix.UnitTests`) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing `Microsoft.Extensions.Http` / `SocketsHttpHandler` / `IHttpClientFactory` typed clients; existing NetArchTest ArchitectureTests; existing xUnit integration and UnitTests FsCheck property conventions. No new NuGet/npm dependency (`brief.md` File envelope; `PRODUCT.md` §5 item 1).

**Storage**: No database or migration change. Host `Program.cs` Migrate call and `OutboxRelayHostedService` remain in place as exact architecture exceptions only.

**Testing**: Ticket-required guard, request behaviour, and DI coverage under `tests/LamuFlix.IntegrationTests/`; the two FsCheck address-policy properties under `tests/LamuFlix.UnitTests/Adapters/` (constitution IX Test Pyramid; brief Q5 amendment / `CONCLUSIONS.md` analyze round 1 item 1; path and harness discovery confirmed at T001). Match each project's existing assertion and mocking conventions (IntegrationTests and UnitTests: Shouldly + NSubstitute per constitution IX Test Pyramid; ArchitectureTests: Shouldly wherever an equivalent exists, xUnit `Assert` only where none exists, per constitution IX Test Pyramid). ArchitectureTests rules and Valid/Violating fixtures (including partial-name-legal and exact EF-exception proofs); optional narrow opt-in on existing subtitle integration tests when they exercise a guarded local endpoint. Controlled DNS/connector seams are internal resolver/connector delegates defaulting to production DNS and socket connect, substituted only in test composition — no live provider requests for automated cases. Never mock the data-access driver for query-translation behaviour (N/A for this HTTP-guard path).

**Target Platform**: Local API / worker hosts that register the three Infrastructure HTTP client extensions; Compose and home-cluster delivery receipts for provider reachability.

**Project Type**: Existing ports-and-adapters .NET solution; Infrastructure adapters, ArchitectureTests, IntegrationTests, and UnitTests within the approved envelope.

**Performance Goals**: One resolution per connection attempt; no invented latency SLA.

**Constraints**: Charter §2.3 care-list items deliberately ruled (Q1–Q6). No public API change. No LocalPlay/secrets/`Process.Start`. Host composition files outside the edit envelope. Serialize with DEV-551 on `SubtitleProviderServiceCollectionExtensions.cs`. Honest gate labels; never lower thresholds; never merge. Size M requires Risk (Sentry), Standards (Ledger), Spec (Compass). Closing bar through Low; max two review rounds and two fix commits per round (`brief.md` Q6).

**Scale/Scope**: Size M; shared guard + address policy + proofs + two architecture rules + two-environment receipts. Starting providers: OMDb, TMDb, Ollama, OpenSubtitles (`recon-DEV-550` lines 34–37; `CONCLUSIONS.md` Q6 inventory clarification).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox (`PRODUCT.md` §5): no ticket contradiction; no constitution departure. No new ADR (`brief.md` Q6).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests | **PASS** — no new Core port or layer; guard lives in Infrastructure adapters; ArchitectureTests gain two enforcement rules inside the existing project (`brief.md` Q1, Q4). |
| II (Explicit Handlers) | Handlers + AddHandler for use cases | **PASS (N/A use-case path)** — no new application command/query; HTTP client registration and dial-time policy only. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — connect refusal is transport-level; no new public API error shape. Failure reasons omit secrets and query strings (Q2). |
| VI (Observability) | Safe diagnostics | **PASS** — logged connect-failure reasons must not carry secrets or query strings (Q2/Q3). |
| VII (Configuration / LocalPlay / Process.Start) | Options; no IConfiguration magic strings; LocalPlay gated | **PASS** — test override is internal options set only in test composition, with zero configuration binding or deployment key (Q5). Production subtitle acquisition and provider options unchanged. No LocalPlay, secrets hardcoding, or Process.Start. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — spec-local vocabulary only; no CONTEXT.md expansion claimed. |
| IX (Test Pyramid) | Meaningful layers; WebApplicationFactory; property tests in UnitTests | **PASS** — DI and behaviour proofs under IntegrationTests with production composition discovery; architecture fixtures under ArchitectureTests; two FsCheck address-policy properties in existing UnitTests (constitution IX Test Pyramid; brief Q5 amendment). Match each project's existing assertion and mocking conventions. |

### Stack / API / workflow checks (outside Constitution Check principle rows)

| Check | Plan response |
|---|---|
| Tech stack / API docs | **PASS** — no public route/DTO/OpenAPI/TypeScript change (`brief.md` File envelope; `PRODUCT.md` §5 item 4). |
| Static-analysis gates | Every changed `.cs` task completes only after Roslyn, cyclomatic complexity, and InspectCode pass on those files; ArchitectureTests green; full pre-merge matrix per brief Verification. Apply AGENTS no-explanatory-comment rule; Q3 places range citations in spec and PR body, not XML prose. |
| Web gates | Scope-empty on the approved file envelope — record as `SKIPPED (scope-empty)`, never PASS. |
| Mutation | Applicable production C# under `src/` expects mutation under existing harness policy with static-initialization discipline (const address literals; mutable or mutation-sensitive computation out of static initialization; expression-bodied static constructors if any); record PASSED / FAILED / SKIPPED (scope-empty) / N/A honestly. |

### Complexity Tracking

No constitution violations requiring justification. No new architectural layer or dependency.

## Approach

- **Phase A** — Quill drafts Spec Kit artifacts from the closed brief. Keel analyzes and may send fix lists. Plan challenge and Gate 1 follow the pipeline. Quill does not claim freeze or Gate 1.
- **Phase B pickup**: Recheck `SubtitleProviderServiceCollectionExtensions.cs` against DEV-551 (serialize; either order). Fresh read-only drift recon for the authorized envelope. Record pickup receipts for: participating host compositions (Api first from `recon-DEV-550` line 13, then any others found that invoke the three registration extensions); existing redirect handling and any explicit subtitle follow path; effective factory setup ordering (defaults vs per-client configure; verify applicable ordering against Microsoft Learn at pickup); UnitTests property-file path and harness discovery convention; production architecture assembly set and baseline EF dependency inventory outside Persistence, including how the installed rule reports generated/nested types. Retain the existing friendship check (`InternalsVisibleTo` for IntegrationTests / conditional UnitTests). Stop for cited Patron ruling before any outside-envelope edit. Stop `needs recon:` if a required path/fact is missing. Stop before dependent rule or registration work if required evidence is missing or an additional EF source dependency outside the two exact exceptions is found.
- **Task order (`brief.md` Task order / Q6)**:
  1. Gate 1, then shared-file recheck with DEV-551 at pickup (blocks Build on that file).
  2. Shared factory registration (`ConfigureHttpClientDefaults` via one per-service-collection idempotent internal extension called from the three registration extensions), configure-in-place `ConnectCallback` and subtitle `AllowAutoRedirect = false` without replacing the handler, `UseProxy = false`, HTTP/2 ceiling, OpenSubtitles `SocketsHttpHandler`, mutation-safe address policy table (mandatory + stricter sets as const literals with parse-on-read / getter computation, `MapToIPv4`), and default-false internal test options that also expose internal resolver/connector delegates defaulting to production DNS and socket connect (test-only substitution; no config binding) — before production registration and request proofs depend on them. One resolution per connection attempt; fall back among validated exact endpoints from that result under existing timeout/cancellation; fail closed on resolver errors/empty/(nullable) null; denied candidates never dial.
  3. Dynamic DI coverage that discovers registrations independently of the guard helper, requires nonempty coverage per confirmed composition, proves repeated installation applies defaults once with subtitle callback/redirect retained, and runs the same discovery/inspection against an added unguarded registration; per-client request behaviour with controlled resolver/connector seams (including followed-redirect, fallback, fail-closed, boundary/singleton examples, and redacted denial capture); address-policy property checks in UnitTests at the pickup-verified path; narrow subtitle regression opt-in only where a guarded local endpoint is required.
  4. Architecture rules and Valid/Violating fixtures driven by the T001 assembly/EF inventory and the same predicates for production and independent positive/negative fixture assertions (including partial-name-legal proofs, exact EF-exception matching for the two approved full type names, a Violating near-match / nested attribution case when generated-artifact attribution is required with exact origin evidence, and an independent method-body-only EF violation) as an independent stream that may proceed in parallel with the guard work — no guard-foundation or InternalsVisibleTo prerequisite (`brief.md` Task order step 3). Gate rule work on the T001 inventory disposition.
  5. After both streams converge: applicable C# gates on the exact head (including mutation static-initialization discipline); then **Rigger** (pipeline Operator) obtains the two deployment receipts from the delivery candidate while Size M review may proceed in parallel (receipts do not wait on review closure). If a review fix changes the delivery candidate, both receipts MUST cover that delivery candidate. After any review fixes, re-prove exact-head gate evidence before merge.
- **Do not**: add egress proxy/network policy; move the two EF exceptions; edit host `Program.cs`; add configuration keys/deployment edits; invent a fifth client; treat recon’s headline count of five as acceptance; add ADR/Gherkin; lower gate thresholds; touch LocalPlay/secrets/`Process.Start`; expand the file envelope without a cited Patron ruling.

### Quill-named new files (within approved Adapters / test directories)

| Path | Role |
|---|---|
| `src/LamuFlix.Infrastructure/Adapters/OutboundHttpClientDefaultsExtensions.cs` | Per-service-collection idempotent `ConfigureHttpClientDefaults` registration; transport constraints; configure-in-place connect callback wiring |
| `src/LamuFlix.Infrastructure/Adapters/OutboundConnectAddressPolicy.cs` | Mutation-safe single internal range table (const literals; parse/compute outside static init) + classification after `MapToIPv4` |
| `src/LamuFlix.Infrastructure/Adapters/OutboundHttpGuardOptions.cs` | Default-false internal test-only stricter-set relaxation; internal resolver/connector delegates defaulting to production DNS/socket (test-only substitution) |
| `tests/LamuFlix.IntegrationTests/OutboundHttpClientGuardRegistrationTests.cs` | Independent dynamic DI discovery + nonempty composition coverage + unguarded addition + repeat-install + quota/resilience/timeout retention |
| `tests/LamuFlix.IntegrationTests/OutboundHttpClientGuardBehaviorTests.cs` | Controlled resolver/connector request proofs (fallback, fail-closed, followed-redirect, boundary/singleton examples) + redacted denial capture |
| `tests/LamuFlix.UnitTests/Adapters/OutboundConnectAddressPolicyPropertyTests.cs` | Mandatory-deny / mapping FsCheck properties (constitution IX; path confirmed at T001) |
| `tests/LamuFlix.ArchitectureTests/Fixtures/Violating/HelperNamespaceFixture.cs` | Namespace rule negative fixture |
| `tests/LamuFlix.ArchitectureTests/Fixtures/Valid/NonHelperNamespaceFixture.cs` | Namespace rule Valid fixture proving ordinary partial names stay legal |
| `tests/LamuFlix.ArchitectureTests/Fixtures/Violating/EfOutsidePersistenceMethodBodyFixture.cs` | EF method-body-only violation |
| `tests/LamuFlix.ArchitectureTests/Fixtures/Valid/EfInsidePersistenceFixture.cs` | EF Valid fixture as needed by convention |
| `tests/LamuFlix.ArchitectureTests/Fixtures/Valid/EfExactExceptionMatchFixture.cs` | Fixture proving EF allow-list matching is exact for the allowed full type names (convention-adjustable name) |
| `tests/LamuFlix.ArchitectureTests/Fixtures/Violating/EfNearMatchOutsidePersistenceFixture.cs` | Violating near-match: EF dependency outside Persistence with a full type name distinct from both allowed types (convention-adjustable name) |

Exact fixture class names may be adjusted at Build to match existing Valid/Violating naming conventions, remaining inside those directories.

## Verification Approach

- **DI**: Build each confirmed participating production service composition (Api first; others from T001); discover named and typed clients independently of the guard-registration helper; require nonempty discovered coverage per composition; inspect final handler chains for shared `ConnectCallback` and OpenSubtitles `AllowAutoRedirect == false`; prove repeated installation applies defaults once while the effective subtitle handler retains callback and redirect; run the same discovery and inspection against an added unguarded factory registration that must be found and fail; detect unguarded new registration without a fixed list (`brief.md` Verification; Q3).
- **Request behaviour**: Test composition substitutes internal resolver/connector delegates; production uses system DNS and the socket connector with address policy applied before every connector call. One resolution per attempt; fallback among validated exact endpoints under existing timeout/cancellation; denied never dial; resolver exception/empty/(nullable) null fail closed. Mandatory denial before connect; subtitle private fail / public reach approved endpoint; configured providers retain private/loopback reach under mandatory rules; IPv4-mapped IPv6, mixed candidates, independent boundary/singleton examples (mapped where applicable), controlled followed-redirect refusal (plus any pickup-confirmed explicit subtitle follow path), scoped test flag, proxy and HTTP-version boundary; captured denial reason category omits URL, query, and secret-header values. Preserve current request-path and retention assertions (`brief.md` Verification; Q2–Q3).
- **Property**: Two FsCheck properties in `LamuFlix.UnitTests` at the pickup-verified Adapters path — mandatory-deny invariance under subtitle test override; equivalent classification after IPv4 mapping; existing harness thresholds and discovery convention (`brief.md` Verification; constitution IX Test Pyramid).
- **Architecture**: Use the T001 assembly/EF inventory and the same predicates for production and independent positive/negative fixture assertions. Both rules against the inventoried production assemblies; Valid fixtures pass (including partial-name-legal and exact EF-exception proofs); Violating fixtures fail (including a near-match / nested attribution case when generated-artifact attribution is required with exact origin evidence, and an independent method-body-only EF violation); exact exception matching by full type name for the two approved source types only (`LamuFlix.Infrastructure.Enrichment.OutboxRelayHostedService` and Api `Program`); fixture assertions independent of production pass (Q4; recon lines 15–16, 28). An ambiguous or additional EF source type stops before rule work.
- **Gates**: Roslyn; cyclomatic complexity; InspectCode; property tests; vulnerable packages; format; full `dotnet test`; mutation per harness including static-initialization discipline on the address-policy table; web scope-empty SKIPPED; honest labels (`brief.md` Verification / Q6). Per-task scoped static-analysis completion, T013 architecture checkpoint, and T015 exact-head sweep are all retained. Exact-head evidence must be preserved or re-proven after any review fixes.
- **Runtime**: After verification gates (T015), **Rigger** (pipeline Operator / operator evidence owner) obtains separate redacted Compose and home-cluster receipts recording environment, candidate revision, and reachability per OMDb, TMDb, Ollama, OpenSubtitles; credentials and sensitive URLs redacted; acceptance pending until both exist. Receipt collection may proceed while Size M review runs and does not wait on review closure (`brief.md` Task order step 4). If a review fix changes the delivery candidate, both receipts MUST cover that delivery candidate.
- **Review / delivery PR**: Through Low; Sentry, Ledger, Compass dispositions; two rounds / two fix commits per round (Q6). Delivery PR body publishes the approved address ranges and source citations (Q2/Q3).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-550/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md              # This file (also mirrored to private IMPL_PLAN)
├── tasks.md             # Also mirrored to private TASKS
├── DRAFTING_RECEIPT.md
└── checklists/
    └── requirements.md
```

No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` / `ASSUMPTIONS.md`.

### Source Code (authorized responsibility envelope — brief File envelope; ticket Where it lands; recon)

```text
src/LamuFlix.Infrastructure/Adapters/
├── MetadataProviderServiceCollectionExtensions.cs   # invoke shared defaults (existing)
├── OllamaEmbeddingServiceCollectionExtensions.cs    # invoke shared defaults (existing)
├── SubtitleProviderServiceCollectionExtensions.cs   # invoke shared defaults; SocketsHttpHandler + AllowAutoRedirect=false (existing; serialize with DEV-551)
├── OutboundHttpClientDefaultsExtensions.cs          # new (Quill-named)
├── OutboundConnectAddressPolicy.cs                  # new (Quill-named)
└── OutboundHttpGuardOptions.cs                      # new (Quill-named)

src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj  # InternalsVisibleTo IntegrationTests if needed; UnitTests only if properties need internal policy

tests/LamuFlix.ArchitectureTests/
├── ArchitectureTests.cs                             # two new rules (existing)
└── Fixtures/Valid/ + Fixtures/Violating/            # new fixtures (partial-name Valid; exact EF-exception; near-match Violating; method-body Violating)

tests/LamuFlix.IntegrationTests/
├── OutboundHttpClientGuardRegistrationTests.cs      # new (Quill-named) — DI + retention proofs
├── OutboundHttpClientGuardBehaviorTests.cs          # new (Quill-named) — request seams + redacted reason
├── OpenSubtitlesSubtitleProviderTests.cs            # opt-in only if guarded local endpoint required
├── SubtitleSearchDownloadEndpointTests.cs           # opt-in only if guarded local endpoint required
└── SubtitleProviderHealthRegistrationTests.cs       # opt-in only if guarded local endpoint required

tests/LamuFlix.UnitTests/Adapters/
└── OutboundConnectAddressPolicyPropertyTests.cs     # new (Quill-named) — FsCheck address-policy properties (path confirmed at T001)
```

**Structure Decision**: Stay inside existing Infrastructure Adapters, ArchitectureTests, IntegrationTests, and UnitTests projects. Host `Program.cs` and `OutboxRelayHostedService.cs` are architecture-rule exception subjects only — not edited.

## Phase 0 / Phase 1 Spec Kit design outputs

Grill Q1–Q6 already resolve inventable research unknowns. No separate `research.md`, `data-model.md`, `quickstart.md`, or `contracts/` artifacts. Range documentation lives in `spec.md` and the delivery PR body (Q3 superseding Q2 XML-doc wording).
