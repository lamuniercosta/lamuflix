# Implementation Plan: Chain Metadata Providers

**Branch**: `feature/423-spec` | **Date**: 2026-10-26 | **Spec**: `specs/DEV-423/spec.md`
**Input**: Frozen `brief.md` (Q1–Q7 + PC1–PC6 appendix); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-423`, `recon-DEV-423`, `recon-DEV-423-2`; `specs/PRODUCT.md`; `.specify/memory/constitution.md`
**Status**: PLAN FROZEN for Gate 1 review by Keel (2026-10-26); Gate 1 not approved; Phase B blocked on DEV-331 T000/T001. See PLAN_CHALLENGE_ADJUDICATION.md final receipt.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief, and the public contract chain is untouched (no DTO/route/OpenAPI/TypeScript change). Precedent: DEV-331 / DEV-303.

## Summary

Deliver `ChainedMetadataProvider : IMetadataProvider` that asks OMDb then TMDb under the documented three-trigger fallback, registered as the sole unkeyed `IMetadataProvider` while individual adapters remain keyed (`omdb` / `tmdb`) transient forwards over concrete typed HttpClients. The chain is telemetry/health/resilience-transparent. Disabled `TmdbOptions.Enabled` yields an OMDb-only list so existing enrichment tests pass unchanged. Phase A drafts against the frozen DEV-331 contract only. Phase B is blocked until DEV-331 owner Q7 constitution resolution, amendment/ADR, and implementation land, then a mandatory pickup drift check — no duplicate DEV-423 constitution checkbox and no silent clearance.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).
**Primary Dependencies**: Existing pins only — `Microsoft.Extensions.DependencyInjection` keyed services, `Microsoft.Extensions.Http`, `Microsoft.Extensions.Http.Resilience`, WireMock.Net in tests. **No new packages** (Q4; §2.3 item 1).
**Storage**: N/A — no schema/migration.
**Testing**: xUnit v3, Shouldly; UnitTests Theory/MemberData fake-provider matrix; IntegrationTests WireMock + shipped composition. No live vendors. TDD optional; required tests before verification.
**Target Platform**: .NET server (ASP.NET Core Minimal API host).
**Project Type**: Infrastructure decorator + DI composition into existing Api host via `AddMetadataProvider`.
**Performance Goals**: first-Found short circuit; end-to-end latency may consume both provider resilience budgets on fallback (accepted, Q4).
**Constraints**: no new explanatory comments; preserve unrelated existing comments in edited files; `CancellationToken` end to end; complexity ≤ 15 then refactor gate per `harness.yml`; never hardcode/lower thresholds; no Core/options/config/secret/API/schema/CONTEXT/README/C4 edits; no constitution/ADR authored here.
**Scale/Scope**: personal library; composition only; TMDb adapter owned by DEV-331.

## Constitution Check

Gates checked against `.specify/memory/constitution.md`. DEV-423 introduces a vendor-neutral decorator over the existing port and does **not** open a new constitution departure. The dual-adapter / `TmdbOptions` departure remains DEV-331 Q7; this plan inherits that resolution as an explicit Phase B blocker (Q1).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Ports in Core; adapters in Infrastructure; no new port without ADR | `ChainedMetadataProvider` implements existing `IMetadataProvider`; no new port (Q2; constitution :148–152). |
| VI (Observability) | TelemetryConstants owns names; health per external dependency | Chain opens no Activity/histogram/health; adapters retain existing per-provider telemetry and untagged checks (Q4). No `TelemetryConstants` edit. |
| VII (Configuration Isolation) | Validated options; no hardcoded secrets | Consumes landed `TmdbOptions.Enabled` only; no new options/secrets (Q4, Q5). |
| VIII (Ubiquitous Language) | Vendor names limited; Core stays functional | Chain and test type names are vendor-neutral (`ChainedMetadataProvider`, `MetadataProviderChainTests`); DI keys reuse existing `TelemetryConstants` provider name strings (Q2, Q5). No new `CONTEXT.md` term (Q5). |
| IX (Test Pyramid) | WireMock for HTTP; Theory+MemberData; no mocked infrastructure drivers | Fake providers for algorithm unit matrix; WireMock + real typed clients for composition/fallback (Q5). |
| Tech Stack | Metadata provider behind `IMetadataProvider`, typed HttpClient, Http.Resilience | Satisfied by composing existing adapters; dual-vendor stack-row permission is DEV-331 Q7, not a DEV-423 amendment (Q1). |
| Gates | Roslyn, complexity, InspectCode, format, vulnerable packages, mutation | Phase B only after upstream landing + pickup drift; see tasks verification phase. |

### Complexity Tracking

No DEV-423 constitution violation to justify. Upstream dual-adapter permission is tracked on DEV-331 Q7 and remains a Phase B prerequisite, not a Complexity Tracking row for this ticket.

## Approach

- **Chain type (Q2, Q3, Q5; PC3)**: `public sealed class ChainedMetadataProvider` in Infrastructure/Adapters taking `IReadOnlyList<IMetadataProvider>`. Guard `null` with `ArgumentNullException` and empty with `ArgumentException`, then snapshot via `ToArray()` so later caller-list mutation cannot change order. Do not capture an unguarded mutable list through a raw primary-constructor parameter incompatible with those guards. Eligibility for remembered-failure advancement is exact record equality to `EnrichmentFailureCategory.RateLimited` or `EnrichmentFailureCategory.ProviderUnavailable`; every other/future category is terminal. No mutable `static` initialisation in the chain. Iterate in order applying FR-002. Forward lookup + token. No catch/reclassify.
- **Registration (Q2; PC4)**: Author exactly the OMDb replacement of port-typed `AddHttpClient<IMetadataProvider, OmdbMetadataProvider>` with concrete `AddHttpClient<OmdbMetadataProvider>`; retain landed DEV-331 `AddHttpClient<TmdbMetadataProvider>` (BaseUrl trailing-slash normalization, independent resilience, secondary health) verbatim — do not re-author or duplicate it. Preserve the private shared `Configure` helper bound to `MetadataProviderResilienceOptions` (`"Omdb:Resilience"`) and unrelated comments. Add `AddKeyedTransient<IMetadataProvider>("omdb", …)` and `"tmdb"` forwarding to concrete clients. Sole unkeyed `AddTransient<IMetadataProvider>(sp => new ChainedMetadataProvider(list))` with list `[omdb]` or `[omdb,tmdb]` from `IOptions<TmdbOptions>.Value.Enabled`, resolving each entry exclusively via `GetRequiredKeyedService<IMetadataProvider>` (no duplicate-entry guard). Do not enumerate unkeyed `IEnumerable<IMetadataProvider>`. Keep registration before RabbitMQ. T001 confirms which TMDb lines actually landed before T005.
- **Disabled short-circuit (Q2, ticket item 4)**: When `Enabled=false`, omit TMDb from the constructed list entirely — do not resolve/invoke a disabled TMDb that would return `Failed(ProviderUnavailable)` under DEV-331 FR-003.
- **Observability (Q4)**: Zero chain instrumentation. New enabled-fallback tests assert two spans/measurements; leave existing disabled-mode telemetry tests alone.
- **Health/resilience (Q4)**: No chain health recording; preserve both check registrations/tags/text; independent breakers remain adapter-owned.
- **Files (Q5; PC2; PC5)**: Create `ChainedMetadataProvider.cs`, `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs` (namespace `LamuFlix.UnitTests.Adapters`), and `MetadataProviderChainCompositionTests.cs`; edit extensions + only type/behaviour assertions pinning unkeyed `IMetadataProvider` to bare OMDb in the two named composition files (preserve unrelated health/telemetry/resilience assertions). Broader drift needs a cited ruling. Document fallback in this packet only.
- **Composition proof (PC6)**: Keyed `"omdb"`/`"tmdb"` resolution under both `TmdbOptions.Enabled` modes is tested separately from disabled-chain zero secondary resolve/invoke/HTTP.
- **Ordering (Q1, Q5)**: Upstream prove → pickup drift/analyze → unit chain → DI → integration/regression → gates/review. No Gherkin.

## Project Structure

```text
specs/DEV-423/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md

src/LamuFlix.Infrastructure/
  Adapters/ChainedMetadataProvider.cs                      (new)
  Adapters/MetadataProviderServiceCollectionExtensions.cs  (edit: concrete clients, keys, sole unkeyed factory)

tests/LamuFlix.UnitTests/
  Adapters/MetadataProviderChainTests.cs                   (new; namespace LamuFlix.UnitTests.Adapters; class MetadataProviderChainTests; PC2)

tests/LamuFlix.IntegrationTests/
  MetadataProviderChainCompositionTests.cs                 (new)
  MetadataProviderCompositionTests.cs                      (edit: type/behaviour assertions pinning unkeyed→bare OMDb only; PC5)
  SecondaryMetadataProviderCompositionTests.cs             (edit only if landed DEV-331 pins unkeyed→OMDb; those assertions only; PC5)
```

**Layout decisions (brief Q5 + PC2/PC5)**:

- Single chain source file — algorithm is small; keep complexity under refactor gate via early returns / private helpers if needed.
- Vendor-neutral test class names (`MetadataProviderChain*`) per constitution VIII / DEV-303 D1; unit file under `Adapters/` matching existing adapter unit layout; distinct from `MetadataProviderChainCompositionTests`.
- No Core, ServiceDefaults, Api appsettings, TelemetryConstants, health-type, README, C4, or CONTEXT edits.
- No research/data-model/contracts artifacts — contract chain untouched; decisions frozen in brief + PC amendments.

## Test Strategy

| Surface | Coverage |
|---|---|
| Unit matrix | Categories derived from `EnrichmentFailureCategory` (current full matrix 7×7; expands when categories are added); call counts; exhaustion precedence; ManualImdbId/token identity; cancel/exception propagation; null→`ArgumentNullException` / empty→`ArgumentException`; caller-list-mutation after construction; one-provider equivalence (PC3) |
| Composition | Keyed omdb/tmdb under both Enabled modes (T009), separate from disabled-chain zero secondary resolve/invoke/HTTP (T010); sole unkeyed ChainedMetadataProvider; RabbitMQ activation; readiness exclusion (PC6) |
| Fallback E2E | WireMock: NotFound→TMDb Found; Found short circuit; RateLimited fallback; InvalidResponse stop (no added ManualImdbId WireMock case — Sentry L6) |
| Observability | Enabled: two Metadata.Lookup + two duration tags; disabled: existing one-provider tests unchanged |
| Health/resilience | Isolation of snapshots; independent breakers; eligible primary failure then secondary Found preserving the primary degraded snapshot; primary 401 → InvalidResponse with zero secondary calls and primary Unhealthy advice visible (fallback rules unchanged) |
| Regression | Existing enrichment suite unchanged under Enabled=false |

No Gherkin. Property tests only if harness requires for changed surface — report SKIP/SKIPPED/N/A distinctly.

## Gates (Phase B — after upstream landing + pickup drift)

1. Explicit changed-C# scope: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness, `./scripts/run-jetbrains-inspectcode.ps1`.
2. Affected tests (chain unit/integration + enrichment regressions), then full `dotnet test`.
3. Property / vulnerable-packages / format per harness; distinguish PASS, failure, SKIPPED (scope-empty), SKIP, mutation N/A, Could not run.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` from worktree).
5. Size M review: Sentry, Ledger, Compass; closing bar Critical/High/Medium; two remediation rounds max (Q6).

Phase A (this draft) runs none of the build/test gates and claims no gate result.

## Risks

- **Upstream not landed**: implementing before DEV-331 owner resolution + Phase B code is out of policy (Q1).
- **Pickup drift**: landed DEV-331 may differ from frozen contract — mandatory drift/analyze before coding; T001 confirms landed OMDb/TMDb registration lines before T005 (Q1, Q5; PC4).
- **Registration recursion**: resolving unkeyed `IMetadataProvider` inside the factory without keys would recurse — mitigated by keyed forwards + `GetRequiredKeyedService` on the factory-owned list (Q2; PC4). No duplicate-entry guard required (Sentry L4).
- **Singleton capture**: registering the chain as singleton would capture transient HttpClients — mitigated by transient chain (Q2).
- **Later-terminal dead-letter trade-off (PC1 / Sentry M1)**: secondary terminal failure after a remembered eligible primary failure can immediately dead-letter rather than retry; accepted so bad data stays visible. `Unknown` stops chain advancement yet remains retryable downstream (Sentry L1).
- **Double resilience budget**: fallback may spend both timeouts — accepted; no elapsed-time ceiling test (Q4; Sentry L2).
- **RateLimited fan-out**: ticket-required load amplification during partial outage — noted, no ticket (Sentry L3).
- **Composition assertion churn**: only type/behaviour assertions pinning unkeyed→bare OMDb in the two named files may be retargeted; unrelated health/telemetry/resilience assertions preserved; broader drift needs a cited ruling (Q5; PC5).

## Ordering

See `tasks.md`: T000 upstream prove → T001 pickup drift → optional unit-test drafting → chain implementation (unit suite compiles with T003) → DI → composition assertion edits → integration → verification/review. Same-file edits are sequenced. T002 has no `[P]` marker (PC6). Preserve unrelated existing comments; no new explanatory comments.
