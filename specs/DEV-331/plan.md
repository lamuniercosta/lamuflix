# Implementation Plan: TMDb Metadata Provider Adapter

**Branch**: `feature/331-spec` | **Date**: 2026-10-26 | **Spec**: `specs/DEV-331/spec.md`
**Input**: Frozen `brief.md` (Q1–Q8, Q3a, Q9) + PC1–PC7 addenda; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; `recon-DEV-331`; `recon-DEV-331-2`; `specs/PRODUCT.md`; `.specify/memory/constitution.md`
**Repair**: Plan-challenge repair round 2 FINAL of max 2 (PC1–PC6 applied in round 1; PC7 fixture-copy scope in this round).

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md` or `contracts/` — every technical choice is ruled in the brief, and the public contract chain is untouched (no DTO/route/OpenAPI/TypeScript change). Precedent: DEV-303.

## Summary

Deliver `TmdbMetadataProvider : IMetadataProvider` beside the existing OMDb adapter: concrete typed HttpClient (BaseAddress trailing-slash normalization at TMDb registration only), independent standard resilience from shared `MetadataProviderResilienceOptions`, ticket-named `TmdbOptions` with private nested `AllowedBaseUrlAttribute`, isolated passive `metadata-provider-tmdb` health (`IOptions<TmdbOptions>` on the check), core-field mapping from official TMDb v3 search/find/details with strict non-throwing poster rules, redacted telemetry tagged `provider=tmdb` via own `ActivitySource` on `TelemetryConstants.ActivitySourceName`, and a recorded-JSON + WireMock proof matrix in IntegrationTests (mapping via `FindAsync` over real typed client — no stub handler / InternalsVisibleTo). Production enrichment continues to resolve unkeyed OMDb. Gate 1 stays closed on the Q7 constitution owner checkbox; amendment/ADR are coordinated via Conductor, not landed here. Owner-checkbox landing (T026) precedes any Phase B implementation.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).
**Primary Dependencies**: Existing pins only — `Microsoft.Extensions.Http`, `Microsoft.Extensions.Http.Resilience`, `WireMock.Net`. **No new packages** (Q6; §2.3 item 1).
**Storage**: N/A — read-only external provider; no schema/migration.
**Testing**: xUnit v3, Shouldly; options/passive-health unit tests; recorded sanitized JSON + WireMock.Net on loopback over real typed client + standard resilience handlers in `LamuFlix.IntegrationTests`. No mocked HTTP transport; no stub `HttpMessageHandler`; no `InternalsVisibleTo`; no live TMDb. TDD optional; fixture-first and all required tests before verification (Ledger L2).
**Target Platform**: .NET server (ASP.NET Core Minimal API host).
**Project Type**: library adapter + composition into existing Api host via `AddMetadataProvider`.
**Performance Goals**: one lookup path (find or search→details); wall clock bounded by existing total timeout options; isolated breaker stops calls to a dead TMDb pipeline without affecting OMDb.
**Constraints**: no new explanatory comments; preserve unrelated existing comments in edited files; `CancellationToken` end to end; `TimeProvider` for time; complexity ≤ 15 then refactor gate per `harness.yml`; never hardcode thresholds (use `OmdbOptions.MatchThreshold`); secrets never committed or logged; BannedSymbols via existing build/analyzers.
**Scale/Scope**: personal library; second adapter only; no new project/folder/layer; fallback chain is DEV-423.

## Constitution Check

Gates checked against `.specify/memory/constitution.md`. **One deliberate departure** is open as the Q7 owner checkbox; Gate 1 remains closed until the owner answers and the amendment/ADR land. All other principles PASS under the brief and PC1–PC6.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Ports in Core; adapters in Infrastructure; Core has no HTTP client | `TmdbMetadataProvider` implements existing `IMetadataProvider`; no new port (Q2; constitution :148–152). HttpClient only in Infrastructure. |
| VI (Observability) | `TelemetryConstants` owns names; health check per external dependency | Own `ActivitySource` reusing `TelemetryConstants.ActivitySourceName` (no new source/meter **name**); reuse enrichment-duration histogram; add `TmdbProviderName = "tmdb"`; passive `metadata-provider-tmdb` untagged outside ready (Q4, Q6; PC6 / Compass F6). |
| VII (Configuration Isolation) | Validated options; `ValidateOnStart`; no hardcoded secrets | `TmdbOptions` bound via existing `BindAndValidate`; private nested `AllowedBaseUrlAttribute` https-or-loopback; ApiKey from secrets/env only, not Required at startup so missing-key health can report (Q4; PC6). |
| VIII (Ubiquitous Language) | Vendor names limited; Core stays functional where possible | Adapter class and ticket-named `TmdbOptions` / check id `metadata-provider-tmdb` are the ruled exceptions; health/test/helper types stay vendor-neutral `SecondaryMetadataProvider*` (PC5 / Ledger M3–M4; Principle VIII; `recon-DEV-331:45` DEV-303 precedent bans vendor-named tests/helpers, not Core-only). **Owner checkbox required** for Core `TmdbOptions` and stack-row dual-adapter permission. |
| IX (Test Pyramid) | WireMock for HTTP; no mocked infrastructure; Theory+MemberData | Recorded JSON + WireMock.Net over real handlers; mapping via `FindAsync` in IntegrationTests; no stub handler / InternalsVisibleTo; no live network; fresh DI per test (Q6; PC4). |
| Tech Stack | Metadata provider behind `IMetadataProvider`, typed HttpClient, Http.Resilience | Satisfied for OMDb today; **TMDb beside OMDb requires constitution amendment + ADR (Q7)** — see Complexity Tracking. |
| Gates | Roslyn, complexity, InspectCode, format, vulnerable packages, mutation | Phase B only after Gate 1 + T026; tasks.md verification phase. |

### Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Constitution Technology Stack Constraints metadata-provider row (:357) names only OMDb; Principle VIII / VII language around vendor naming and listed Core options | Ticket explicitly requires `TmdbMetadataProvider : IMetadataProvider` beside OMDb and ticket-named `TmdbOptions` in Core (`recon-DEV-331:137–165`; PRODUCT.md:23–24) | Silent stack-row ignore, renaming Core options away from the ticket, or a new port — all contradict ticket text or constitution port rules; Patron escalated as Q7 owner checkbox |

Status until owner checkbox: **FAIL closed (Gate 1 blocked)** — drafting proceeds; Phase B implementation is not authorized until T026 confirms amendment/ADR landed.

## Approach

- **Registration (Q2; PC1)**: Keep `AddHttpClient<IMetadataProvider, OmdbMetadataProvider>` untouched. Add `AddHttpClient<TmdbMetadataProvider>` with BaseAddress from `IOptions<TmdbOptions>.BaseUrl` **normalized to a trailing slash at TMDb registration only** (assert outgoing path against slashless loopback base in tests). Independent `.AddStandardResilienceHandler()` configured from `IOptions<MetadataProviderResilienceOptions>` (same configurator as OMDb). `TryAddSingleton<SecondaryMetadataProviderHealthState>()`. Register `SecondaryMetadataProviderHealthCheck` as `"metadata-provider-tmdb"` with **empty tags**. No selector, composite or new port. `AddMetadataProvider` does not call `AddLamuFlixOptions` (T023 owns inspection — PC6 / Compass F4).
- **Activation (Q2, Q4)**: For either Enabled value, production enrichment still resolves OMDb; RabbitMQ `HasRegistration<IMetadataProvider>` stays true. `Enabled=false` or Enabled+blank key → `Failed(ProviderUnavailable)` with zero HTTP. Health check registers in either mode.
- **Lookup (Q3, Q3a)**: Manual IMDb id → find endpoint, `movie_results` only. Else clean title → search page 1 → `TitleMatcher` vs `OmdbOptions.MatchThreshold` → tie-break score desc, id asc → details. No hardcoded 0.8.
- **Mapping (Q3, Q5; PC2)**: Core fields only; usable `poster_path` = exact one leading `/`, no scheme/host/backslash/controls/`.`/`..` segments; non-throwing `Uri.TryCreate` else absent PosterUrl; never `/configuration`; never map `vote_average` to ImdbRating; credits/ratings left empty. Adversarial fixtures required.
- **Classification (Q5; PC3)**: Exact table in brief/spec; 401 → InvalidResponse + wasUnauthorized; details/find 404 → NotFound; **search 404 → InvalidResponse**; when catching `OperationCanceledException`, if `ct.IsCancellationRequested` rethrow unrecorded (coincident cancel included), else non-caller timeout/breaker → `Failed(ProviderUnavailable)`. No OMDb edits.
- **Resilience (Q5)**: Shared options section `Omdb:Resilience`; isolated pipeline/breaker; Retry-After support without claiming TMDb sends it. Shared tuning and reactive 429 exhaustion are accepted residuals (Sentry F7/F8).
- **Telemetry (Q6; PC6 / Compass F6)**: Private static `ActivitySource` using `TelemetryConstants.ActivitySourceName` (no new name); existing histogram; `TmdbProviderName`; source-generated nested Log; `TimeProvider`.
- **Health (Q4; PC5–PC6)**: Isolated `SecondaryMetadataProvider*` types; check takes `IOptions<TmdbOptions>` for blank-key Unhealthy (initial + disabled); atomic immutable last-write-wins snapshot (Sentry F5). Small duplication vs OMDb justified by blank-key/`IOptions` divergence and isolation — no shared extraction, no `OmdbOptions`/`MetadataProviderProbe` rewrite (PC5; Principle VIII; `recon-DEV-331:45`).
- **Options (Q4; PC6 / Compass F1)**: `TmdbOptions` three fields; private nested `AllowedBaseUrlAttribute` inside `TmdbOptions.cs` only; ServiceDefaults bind line; appsettings non-secret defaults only.
- **Secrecy (Q5; Sentry F3)**: Default HttpClient factory logging stays enabled during sweep; capture adapter + `Microsoft.Extensions.Http` + resilience categories on retries/errors, structured state, exceptions, LamuFlix/`System.Net.Http` activities, and health descriptions.
- **Constitution/ADR**: Proposed amendment+ADR coordinated through Conductor after/with owner checkbox; Quill does not assign ADR numbers or edit constitution in Phase A drafting. T026 precedes implementation.

## Project Structure

```text
specs/DEV-331/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md

src/LamuFlix.Core/
  Options/TmdbOptions.cs                                 (new; owner-checkbox gated; private nested AllowedBaseUrlAttribute)
  Pipeline/TelemetryConstants.cs                         (edit: TmdbProviderName)
src/LamuFlix.Infrastructure/
  Adapters/TmdbMetadataProvider.cs                       (new)
  Adapters/TmdbMetadataProvider.Mapping.cs               (new)
  Adapters/TmdbMetadataProvider.Log.cs                   (new)
  Adapters/SecondaryMetadataProviderHealthState.cs       (new)
  Adapters/SecondaryMetadataProviderHealthCheck.cs       (new; IOptions<TmdbOptions>)
  Adapters/MetadataProviderServiceCollectionExtensions.cs (edit; trailing-slash BaseAddress normalize)
src/LamuFlix.ServiceDefaults/
  Extensions.cs                                          (edit: BindAndValidate<TmdbOptions>)
src/LamuFlix.Api/
  appsettings.json                                       (edit: Tmdb Enabled/BaseUrl)

tests/LamuFlix.UnitTests/
  Options/SecondaryMetadataProviderOptionsTests.cs       (new; vendor-neutral options tests for ticket-named TmdbOptions)
  Adapters/SecondaryMetadataProviderHealthCheckTests.cs  (new; initial/disabled blank-key Unhealthy)
  HealthCheckRegistrationTests.cs                        (edit: assert secondary untagged)
  Options/OptionsStartupValidationTests.cs               (edit: Tmdb bind path)

tests/LamuFlix.IntegrationTests/
  LamuFlix.IntegrationTests.csproj                       (future edit ONLY: Fixtures/SecondaryMetadata/**/*.json CopyToOutputDirectory=PreserveNewest; preserve relative dirs; no package/project-ref/other-fixture/settings changes — PC7)
  Fixtures/SecondaryMetadata/                            (new recorded sanitized JSON; same-project only)
  SecondaryMetadataProviderProbe.cs                      (new; dedicated; no OMDb probe rewrite)
  SecondaryMetadataProviderMappingTests.cs               (new; FindAsync + WireMock + colocated fixtures; read via AppContext.BaseDirectory)
  SecondaryMetadataProviderLookupTests.cs                (new)
  SecondaryMetadataProviderResilienceTests.cs            (new)
  SecondaryMetadataProviderTelemetryTests.cs             (new; dedicated)
  SecondaryMetadataProviderCompositionTests.cs           (new)
  MetadataProviderCompositionTests.cs                    (edit: OMDb unchanged under both Enabled)
  HealthEndpointTests.cs                                 (edit: secondary outside ready)
```

**Layout decisions (brief Q6 + PC4–PC6)**:

- Health types in two files (one-type-per-file; matches OMDb health split). Small duplication of health state/check and `AllowedBaseUrlAttribute` is accepted: TMDb blank-key rule needs `IOptions<TmdbOptions>` and an isolated snapshot; sharing would edit `OmdbOptions.cs` / `MetadataProviderProbe.cs` or invent a premature abstraction (PC5; Principle VIII; `recon-DEV-331:45`; §2.3 item 6; Q8).
- Test/helper types use `SecondaryMetadataProvider*` including `SecondaryMetadataProviderOptionsTests`, `SecondaryMetadataProviderProbe`, `SecondaryMetadataProviderTelemetryTests` — vendor-neutral helpers; ticket-named production `TmdbOptions` unchanged.
- Recorded JSON under `tests/LamuFlix.IntegrationTests/Fixtures/SecondaryMetadata/` only (Compass F5 / PC4). Loading strategy (PC7): future edit of `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` solely to copy `Fixtures/SecondaryMetadata/**/*.json` with `CopyToOutputDirectory=PreserveNewest` (preserving relative output directories); tests read fixtures only via `Path.Combine(AppContext.BaseDirectory, "Fixtures", "SecondaryMetadata", …)`. No package/project-reference/other-fixture/settings changes; **no** cross-project UnitTests reference and **no** new test project.
- Mapping coverage is IntegrationTests-only via `FindAsync` + real typed client + WireMock — not UnitTests private-mapper invocation, not stub `HttpMessageHandler`, not `InternalsVisibleTo` (PC4 / Ledger M1). Unit tests remain for options and passive health only.
- Integration fixture `SecondaryMetadataProviderProbe.cs` flat beside `MetadataProviderProbe.cs`; dedicated probe only — do not rewrite OMDb probe or extract shared abstraction (PC5 / Ledger M2).
- No InternalsVisibleTo; exercise via `FindAsync` / `HealthCheckService`.

## Test Strategy

Configure short resilience timing through `Omdb:Resilience` options like `MetadataProviderProbe` — no production timing seams (Q6). TDD optional; write fixtures and required tests before verification (Ledger L2).

| Surface | Coverage |
|---|---|
| Mapping (IntegrationTests FindAsync + WireMock) | Core fields; null/empty/absent optionals; malformed/wrong-shaped; zero/invalid runtime/id; vote_average excluded; no credits; adversarial poster_path (scheme/host/backslash/controls/dot segments) |
| Matching | Threshold from OmdbOptions; deterministic ties; year query; no matches; manual-id precedence; no title fallback |
| WireMock lookup | Every Q5 status/result row; search 404 → InvalidResponse; disabled/blank-key zero HTTP; slashless-loopback BaseUrl → outgoing `/3/` path |
| WireMock resilience | Exhausted retry; 429; synthetic Retry-After; attempt/total timeout; isolated breaker; cancellation during request/delay; coincident cancel when `ct.IsCancellationRequested` propagates unrecorded |
| Composition/health | Concrete resolve; unkeyed OMDb both Enabled values; untagged secondary; readiness unchanged; Healthy/Degraded/Unhealthy/recovery incl. initial/disabled blank-key; OMDb health regression; T023 inspection: no `AddLamuFlixOptions` from `AddMetadataProvider` |
| Telemetry/secrecy | Dedicated `SecondaryMetadataProviderTelemetryTests.cs`; provider=tmdb; ErrorType on Failed; default factory logging enabled; key absent from adapter + `Microsoft.Extensions.Http` + resilience categories, structured state, exceptions, LamuFlix/`System.Net.Http` activities, health descriptions |
| Options unit | BaseUrl validation edges; ApiKey not required; defaults; private nested validator behaviour |

No Gherkin unless opted in. Property tests only if harness requires for changed Core — follow existing opt-in/out reporting (SKIP ≠ PASS).

## Gates (Phase B — after Gate 1 and T026)

1. Explicit changed-C# scope: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness, `./scripts/run-jetbrains-inspectcode.ps1`.
2. Affected tests, then full `dotnet test`.
3. Property / vulnerable-packages / format per harness; distinguish PASS, failure, `SKIPPED (scope-empty)`, opt-out SKIP, mutation N/A, Could not run. BannedSymbols covered by build/analyzers.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` from worktree).
5. Size M review: Sentry, Ledger, Compass; closing bar Critical/High/Medium; two rounds max (Q8).

Phase A (this draft) runs none of the build/test gates.

## Risks

- **Gate 1 blocked**: implementation before owner checkbox/amendment/ADR (T026) is out of policy.
- **Shared MatchThreshold / Omdb:Resilience**: tuning couples adapters — accepted residual (Q3a, Q5; Sentry F7).
- **Reactive rate-limit only**: no proactive throttle — accepted residual (Sentry F8).
- **First-page matching**: may miss obscure titles — accepted (Q3).
- **Missing-key Unhealthy while disabled**: intentional for AC; does not affect readiness (Q4).
- **Complexity hotspots**: classification + mapping — split private helpers; refactor gate ≤ harness threshold.
- **Secrecy via query string**: redaction must cover factory/resilience categories on retries/errors/telemetry and health descriptions, not only happy-path adapter logs (Q5; Sentry F3).
- **BaseUrl slash**: operator slashless config silently truncates relative paths unless registration normalizes (PC1).

## Ordering

See `tasks.md`: T026 owner landing first → fixtures/options/startup → health → mapper/lookup → DI/resilience → probe → mapping/lookup/resilience/telemetry/composition → named existing-file edits → verification/review. Same-file edits are sequenced, never `[P]`. Preserve unrelated existing comments; no new explanatory comments.
