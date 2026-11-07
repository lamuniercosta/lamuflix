# Implementation Plan: Harden the local launcher

**Branch**: `feature/458-spec` | **Date**: 2026-11-07 | **Spec**: [spec.md](spec.md)

**Input**: Grill-closed `specs/DEV-458/brief.md` (Q1–Q9); `CONCLUSIONS.md` (including Q7–Q9); `ASSUMPTIONS.md`; Proposed ADR `docs/adr/0036-local-launcher-trust-boundary.md`; Maestri notes `DEV-458`, `recon-DEV-458`, `recon-DEV-458-2` section 7, `recon-DEV-458-2-2` section 8, `recon-DEV-458-2-3` section 9; `analysis-DEV-458-Keel`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft — Phase 2 Spec Kit drafting after plan-challenge adjudication round-1 remaining-blocker repair (brief/CONCLUSIONS Q9). **PLAN NOT FROZEN**. Gate 1 closed. Selected Q7 design recorded below is distinct from pending Phase B proof. Constitution Check VII remains **OPEN** awaiting structural owner decision (Q9 checkbox), not unspecified accessor recon. Known 403/404 payload feasibility resolved by Q9. No implementation clearance. No production gates run in this ask.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md` (Quill). No `research.md`, `data-model.md`, `quickstart.md`, or `contracts/` — behavioural and footprint choices are ruled in the brief/ADR; public HTTP route/statuses unchanged. ADR 0036 drafted by Keel — Quill does not edit ADR bodies; status remains **Proposed** until Gate 1 acceptance, then an explicit owner/Keel lifecycle update to Accepted.

## Summary

Harden local media launch behind a proven local HTTP boundary: fail-start when LocalPlay is enabled on any non-proven-loopback effective listener via a dedicated API `IHostedService.StartAsync` registered in `Api/Program.cs` (no ServiceDefaults change) — blocked for implementation until the Q9 owner checkbox answers I1/VII; refuse play requests that are not raw-loopback without the exact five provenance headers (existing 403 title `Forbidden`, no detail, before side effects); inside the existing Infrastructure launcher, check selected-file existence before player resolution and process start (parameterless `NotFoundException()` → 404 title `Not Found`, no detail / safe 422, zero start/history); pin nginx play-route deny (proxy 403) and retain Aspire web endpoint-proxy disabled, with a separate named LocalPlay-enabled loopback positive-control host sharing the same build/player/file fixtures; prove with runtime smoke. Preserve DEV-438 selection, UseShellExecute, and one ArgumentList file argument. No throttle, containment assertion, or argument-separator change. No new dependency, port, project, layer, schema, or secret. No handler/Core edit.

## Domain vocabulary

| Term | Definition | Notes |
|------|------------|-------|
| LocalPlay | `Features:LocalPlay` flag selecting enabled vs disabled launcher | Existing; constitution VII |
| Primary video | DEV-438 shared selector outcome (path + format) | Unchanged |
| Configured player | PlaybackOptions player matching format case/dot-insensitively | Existing |
| Playback history | Event recorded only after successful Launch return | Existing |

No glossary additions (brief Q4/Q5).

## Technical Context

**Language/Version**: C# / .NET 10 — ports and adapters with feature-organised core and explicit handlers (constitution Principles I–II); no new web UI.

**Primary Dependencies**: Existing pins only — including already-referenced System.IO.Abstractions (`IFileSystem`). **No new NuGet/npm packages** (Q3; Q4; PRODUCT §5 item 1).

**Storage**: N/A for this ticket — no schema or migration (Q4).

**Testing**: xUnit v3, Shouldly, NSubstitute; existing MockFileSystem / `IFileSystem` seams for launcher; API/host/options integration tests; actual effective-listener startup tests per Q7 matrices; nginx/Aspire runtime smoke with process-start observation. Gates from harness.yml at implementation time — do not freeze numeric thresholds here (brief Test strategy; Q5).

**Target Platform**: Existing `LamuFlix.Api` host; shipped `docker/nginx.conf` and Aspire AppHost; Compose keeps LocalPlay off.

**Project Type**: Extend existing Playback Infrastructure, API playback endpoint/startup composition (selected Q7 API-only hosting hook), and focused tests — no new project/layer; ServiceDefaults unchanged (Q4; Q7).

**Performance Goals**: None numeric in brief.

**Constraints**: TreatWarningsAsErrors; TimeProvider for time; Process launches via `ArgumentList` only; LocalPlay registration rule preserved; never hand-edit generated OpenAPI/TS; never lower gate thresholds; report PASS/failure/SKIPPED/SKIP/N/A/Could not run distinctly; no GlobalExceptionHandler edit without a new Patron ruling; constitution VII — no ad hoc key reads / new key / new options record; complexity ≤ harness then refactor gate.

**Scale/Scope**: Size M; frozen Q1–Q4 surfaces named in brief Planned file responsibilities as refined by Q7; anything else is a follow-up issue.

**Selected design (distinct from pending proof/freeze)**: Dedicated API `IHostedService.StartAsync` registered in `src/LamuFlix.Api/Program.cs`; synchronous validation or throw before server start; no ServiceDefaults change (brief Q7; CONCLUSIONS Q7; `recon-DEV-458-2:12-48`). Phase B actual-startup/shipped-path evidence, Keel plan challenge, PLAN FROZEN, and Gate 1 remain separately pending and are not claimed here.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` on this worktree. Brief/CONCLUSIONS Q9: structural owner checkbox is open; VII remains binding — never a waiver, never a silent fail-all, never magic-string/new-options/reflection/code-endpoint workarounds.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | No new ports/layers without ADR | **PASS** — reuse `IMediaPlayerLauncher`, `IProcessStarter`, `IFileSystem`; ADR 0036 records trust-boundary tradeoff (Keel). |
| II / V (Failures / API) | ProblemDetails; existing statuses | **PASS** — reuse existing 403/404/422/500 mappings and statuses; Q9 resolves payload feasibility: 403 title `Forbidden` / 404 title `Not Found`, both with absent detail; handler ignores messages; no custom detail/message/constructor; no handler/Core edit (Q1; Q3; Q4; Q8 forbidden-content; Q9; `recon-DEV-458-2-3` §9). |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay registration; ArgumentList | **OPEN — awaiting owner** — ticket-named LocalPlay/Process.Start; preserve ArgumentList + UseShellExecute; named available surfaces I0/I2/I3/I4/I6 stand; **I1 UNAVAILABLE** (no permitted Kestrel Url/absence reader; no enabled configuration certifiable under current constraints; silent fail-all rejected) per `recon-DEV-458-2-2` §8.4 / CONCLUSIONS Q9. I7 Phase B timing/parity proof remains mandatory (T005). Owner PR checkbox (exact brief Q9 text) must stay unchecked until the user answers VII departure (a)/(b) or Q2 rescope. No magic-string, new Kestrel options record, reflection, or code endpoint approved. |
| VIII (Ubiquitous Language) | CONTEXT.md + ADR | **PASS** — no new glossary terms; ADR 0036 Proposed until Gate 1 Accepted lifecycle. |
| IX (Test Pyramid) | Real evidence where required | **PASS** — pure unit parsing + API/host tests + real-Kestrel startup listener tests + shipped-path runtime smoke (not mocked proxy); Q9 exact-title/detail-absence/no-leak asserts in approved test zones (Q4; Q7; Q8; Q9). |
| API and Contract Rules | C# → OpenAPI → generated TS | **PASS** — no expected contract diff; refresh only via tooling if a gate requires it (Q4). |
| Static-Analysis / PR gates | Harness scripts; never lower thresholds | After owner answer + Gate 1 + Phase B pickup; honest dispositions (Q5; Q9). |
| Governance / ADR | Architectural choice recorded | **PASS** — ADR 0036 Proposed; Quill does not rewrite it (Keel owns ADR). |

### Complexity Tracking

No Complexity Tracking rows. Q9 opens the structural owner checkbox for I1/VII; Constitution Check stays OPEN awaiting owner; **PLAN NOT FROZEN**; Gate 1 closed.

## Approach

Follow brief Task ordering and Planned file responsibilities (Q4), refined by Q7, with Q6 side-effect precision:

1. **Selected hosting design (pre-implementation composition)**: Register dedicated API `IHostedService` type `src/LamuFlix.Api/LocalPlayListenerValidationHostedService.cs` from `src/LamuFlix.Api/Program.cs`; pure address/listener parsing helper `src/LamuFlix.Api/LocalPlayListenerAddressParser.cs` (directly unit-testable; no mutable static initialization). `StartAsync` completes synchronously or throws before server startup. No fire-and-forget, no `ApplicationStarted` validation, and options startup validation is not the primary hook. **No ServiceDefaults change** (brief Q7; CONCLUSIONS Q7; Learn hosted-services https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0#ihostedservice-interface and startup https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-10.0 as supplied by `recon-DEV-458-2:18-20`). Design selection ≠ Phase B proof ≠ PLAN FROZEN ≠ Gate 1.
2. **Effective-set observation and precedence (US1 / Q7 / Q8 / Q9)**: Observe **both** framework configuration inputs and `IServerAddressesFeature`, including `PreferHostingUrls`, via named available surfaces **I0** (`IOptions<FeatureOptions>`), **I2** (`IConfiguration[WebHostDefaults.ServerUrlsKey]`), **I3** (`WebHostDefaults.HttpPortsKey`/`HttpsPortsKey`), **I4** (`WebHostDefaults.PreferHostingUrlsKey` + feature `PreferHostingUrls`), **I6** (`IServer.Features.Get<IServerAddressesFeature>()`). **I1 UNAVAILABLE** — no permitted Kestrel-section Url/absence reader; no enabled configuration certifiable under current constraints; reject silent fail-all (`recon-DEV-458-2-2` §8.4; CONCLUSIONS Q9). **I7** empty-vs-unpopulated timing/parity remains Phase B T005 real-host proof. Precedence pointer (checked-in): brief Q7.2 and CONCLUSIONS Q7 §2 / supplement 6 — Kestrel-section endpoints override URL-family unless hosting URLs are preferred; URL family overrides `HTTP_PORTS`/`HTTPS_PORTS`; documented empty-config default is net10.0 `http://localhost:5000` (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0). Distinguish explicitly: (a) absent/not-yet-populated address feature — never infer “no inputs” from an empty feature alone; (b) independently proven merged configuration inputs via I0/I2/I3/I4/I6; (c) true empty-config default (default applies only when no endpoint is configured anywhere — uncertifiable for LocalPlay=true until owner answers I1); (d) present-but-unprovable/irreconcilable inputs fail closed rather than choosing the safe-looking surface. Preserve Q2 loopback grammar and Q4 mapped-address normalization; disabled LocalPlay adds no bind restriction. Validator implementation stays blocked until the Q9 owner checkbox is answered.
3. **Constitution VII compliance (Q7 supplement 6 / Q9)**: Obtain framework inputs only through the named available surfaces above. No ad hoc raw configuration-key reads, no new public configuration key, no new options record, no reflection, no code endpoints. Constitution Check stays **OPEN awaiting owner** (not unspecified recon). Exact unchecked owner checkbox (brief Q9) must be carried on the spec PR. No silent waiver; no silent fail-all.
4. **No code-defined Kestrel endpoints (Q7 / G2)**: Do not introduce `Listen`, `ListenAnyIP`, `ListenLocalhost`, `ListenUnixSocket`, `ListenNamedPipe`, `ListenHandle`, `UseSystemd`, or endpoint-defining `ConfigureKestrel` in `src/`. Enforce as an explicit review invariant with a `src/` source-search receipt at the reviewed head plus review — not as a runtime detection claim. Any required such endpoint **returns to Patron with fresh recon** before proceeding. URL-family, ports-shorthand, and Aspire/environment bindings remain in scope via I2/I3/I4/I6; Kestrel-section certification awaits owner.
5. **Request guard (US2 / Q8 / Q9)**: Early route-specific guard helper `src/LamuFlix.Api/Endpoints/PlayRequestBoundary.cs` used from `PlaybackEndpoints` — loopback peer + absence of exactly the five headers `Forwarded`, `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto`, `X-Real-IP` (no wider list); refuse via existing FeatureDisabledException → existing 403 ProblemDetails path **before** command handler, preserving title `Forbidden` and absent detail (handler ignores messages; no custom detail). Do not enable UseForwardedHeaders; IPv4-mapped normalization shared with startup checks via the Api parser helper. Payload feasibility resolved by Q9/`recon-DEV-458-2-3` §9 — no handler/Core edit.
6. **Launcher outcomes (US3 / Q8 / Q9)**: Inject existing `IFileSystem` into `ProcessMediaPlayerLauncher` via existing `PlaybackServiceCollectionExtensions` factory; existence → player resolve → Start; map missing file to existing **parameterless** `NotFoundException()` (fixed message `Not found.` never surfaces; wire title `Not Found`, no detail) and unmapped format to `ValidationException`; preserve ArgumentList/UseShellExecute (no `--` separator, no UseShellExecute change, no throttle/containment). Unconditional API registration chain is `Program.cs:36` → `HandlerRegistration.cs:32`; RabbitMQ registration is conditional and is not the API proof (`recon-DEV-458-2:50-59`). No custom NotFound message/constructor; no handler/Core edit.
7. **Shipped proxies (US2/US4 / Q8 pinned)**: In `docker/nginx.conf`, deny `POST /api/movies/{id}/play` with proxy 403 before `/api/` `proxy_pass`; retain Compose LocalPlay-disabled default but do not rely on it as the exclusion. For Aspire, retain disabled web `WithEndpointProxySupport(false)` and permit no endpoint proxy to forward play; **no AppHost edit** unless Phase B smoke shows play forwarding through the web proxy — then only minimal retaining/applying `WithEndpointProxySupport(false)` is in-ruling, else return to Patron. Mechanism documentation stays in `specs/DEV-458/` (no README/docker-compose edit). Positive control is a **separate named** LocalPlay-enabled loopback host using the **same build, configured player fixture, and selected-file fixture** as the negative smokes; expected 204 + exactly one process start; never presented as proof that excluded/disabled topologies can launch.
8. **Verification**: Update API/startup/history/composition tests per brief + Q7/Q8 matrices; run harness gates; Size M three-axis review; attach no-code-Listen source-search receipt; no GlobalExceptionHandler/Core/selector/handler/docker-compose/ServiceDefaults/web edits without a new Patron ruling.

**Phase labels**: Phase A = planning documents only (this draft). Gate 1 stays closed while the Q9 owner checkbox is unanswered; after user answer + explicit user approval + user-owned spec PR merge, Phase B = post-merge `T000` pickup drift against landed main/DEV-438, then implementation. **PLAN NOT FROZEN** ≠ Gate 1 approval; selected Q7 design does not invent PLAN FROZEN; no seat merges. Spec-PR body preparation carries the checkbox; actual PR publication is Conductor-owned and is not claimed posted here.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-458/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md
docs/adr/0036-local-launcher-trust-boundary.md   # Keel; Proposed until Gate 1 Accepted lifecycle; Quill does not rewrite
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced — brief/ADR freeze behaviour and footprint; public route/statuses unchanged.

### Source Code (repository root)

```text
# Launcher (US3)
src/LamuFlix.Infrastructure/Playback/ProcessMediaPlayerLauncher.cs
src/LamuFlix.Infrastructure/Playback/PlaybackServiceCollectionExtensions.cs

# Request / startup boundary (US1–US2) — Q7 selected API-only composition
src/LamuFlix.Api/Program.cs                                          # registration only
src/LamuFlix.Api/LocalPlayListenerValidationHostedService.cs         # IHostedService.StartAsync
src/LamuFlix.Api/LocalPlayListenerAddressParser.cs                   # pure parse/normalize; no mutable static init
src/LamuFlix.Api/Endpoints/PlaybackEndpoints.cs
src/LamuFlix.Api/Endpoints/PlayRequestBoundary.cs                    # peer + five-header guard helper
# src/LamuFlix.ServiceDefaults/Extensions.cs   # NO CHANGE for DEV-458 (Q7)

# Shipped proxies (US2/US4 / Q8 pinned)
docker/nginx.conf                                                    # play-route deny → proxy 403
src/LamuFlix.AppHost/Program.cs                                      # edit only if Q8 smoke gate trips

# Tests (existing projects; named files)
tests/LamuFlix.UnitTests/Playback/ProcessMediaPlayerLauncherTests.cs
tests/LamuFlix.UnitTests/Playback/PlaybackServiceCollectionExtensionsTests.cs
tests/LamuFlix.UnitTests/Options/LocalPlayListenerAddressParserTests.cs
tests/LamuFlix.IntegrationTests/PlaybackEndpointTests.cs
tests/LamuFlix.IntegrationTests/ApiEndToEndPlaybackTests.cs
tests/LamuFlix.IntegrationTests/PlaybackHistoryEndpointTests.cs
tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs
tests/LamuFlix.IntegrationTests/LocalPlayRealKestrelHostFixture.cs   # NEW real-Kestrel fixture (not TestServer)
tests/LamuFlix.IntegrationTests/LocalPlayListenerStartupTests.cs     # real-host matrices + observables
```

**Layout decisions**:

- No new project, top-level folder, or abstraction layer (Q4). Helpers are focused types inside the existing `LamuFlix.Api` project (Api root + `Endpoints/`), not a new architectural layer.
- Core `IMediaPlayerLauncher`, PlayMovieCommandHandler, DEV-438 selector, GlobalExceptionHandler, docker-compose, ServiceDefaults, and `web/` are out of approved footprint unless a new Patron ruling (Q4; Q7; Q8).
- Hosting hook and landing file are selected by Q7: dedicated API `IHostedService.StartAsync` registered from `Program.cs`; ServiceDefaults unchanged.
- **Existing fixture facts (recon; do not invent)**: `ApiHostFactory` / `ApiEndToEndFactory` are `WebApplicationFactory<Program>` + TestServer harnesses (`Microsoft.AspNetCore.TestHost`); they remove/retain hosted services and do **not** prove real Kestrel bind/refusal (`recon-DEV-458:40-49`, `:71`). No existing bind-address refusal or real-Kestrel LocalPlay fixture was found in recon. Phase B therefore adds `LocalPlayRealKestrelHostFixture` / `LocalPlayListenerStartupTests` in the existing IntegrationTests project rather than claiming TestServer evidence closes Q7.

## Test Strategy

| Family | Required evidence |
|---|---|
| Parser unit (Ledger L3) | `LocalPlayListenerAddressParserTests.cs`: pure accept/reject grammar including mapped-loopback normalize; wildcards; non-loopback literals; **hostname rejection** (arbitrary hostname rejected independent of DNS — brief Q2; Compass L3); unknown/unprovable forms. No mutable static initialization in production parser. Grammar-level hostname coverage is justified when runtime DNS-independent rejection is the settled rule; no new DNS seam without ruling. |
| Launcher unit | Existing file + mapped player success; missing file with **parameterless** `NotFoundException()`; unmapped format → ValidationException; both failures → 404 wins; case/dot-insensitive mapping; hostile filename single ArgumentList entry (no `--` separator); process-start failure; MockFileSystem arrangements; assert title `Not Found`, detail absent, no message/type/path/basename/player leak (Q8 forbidden-content; Q9) |
| API play | IPv4/IPv6/mapped loopback success; null/non-loopback peers; each of the **exact five** provenance headers case/empty/multiple → 403 with Q6 zero side effects **and** Q9 body asserts (title exactly `Forbidden`, detail absent, no disabled claim / peer address / header name / exception message / exception type); disabled case preserves Q6 order and same title/detail-absence pins; replace unmapped-format 500 with safe 422; keep folder-missing 404 (title `Not Found`, detail absent, no path leak), no-video 409, validation 422, success 204, history regressions |
| Startup (Q7 mandatory; real Kestrel) | Executable `LocalPlayRealKestrelHostFixture` + `LocalPlayListenerStartupTests`: real Kestrel (not TestServer), ephemeral ports, isolation, cleanup/dispose, actual host composition registration order. Observables: start-sequence recording; refusal before bind; no listening socket / no served request on fail; registration-order regression vs later hosted services; concurrency (no concurrent bind/serve first); PreferHostingUrls both ways; Kestrel-over-URL; HTTP_PORTS/HTTPS_PORTS + URL override; **net10.0** empty-config default `http://localhost:5000` (fail visibly if documented default drifts); absent/unpopulated feature vs proven merged inputs vs true empty default vs unprovable; mapped/mixed/on-off; hostname case via grammar-level rejection coverage. Parser/TestServer-only evidence cannot close Q7 (`recon-DEV-458-2` G1; Q7; Ledger M2; Compass L2). |
| Runtime smoke (Q8) | Actual nginx path: proxy 403 + zero starts; Aspire web-proxy attempt: no forwarded launch + zero starts; header-bearing or non-loopback direct-API: API 403 + zero starts; **separate named** positive-control LocalPlay-enabled loopback host (same build/player/file fixtures) → 204 + exactly one start — not proof excluded topologies can launch; observe Aspire injected-listener environment (G3) |
| Invariant receipt | `src/` source-search receipt at reviewed head for Listen-family / UseSystemd / endpoint-defining ConfigureKestrel symbols, plus review (Q7 G2) — covered by T032 |
| Gates | Roslyn, complexity (+ refactor threshold), InspectCode, full tests, property, vulnerable packages, format, mutation (no worktree `--since`), architecture, contract drift — honest SKIPPED/N/A/Could not run |

### Explicit return-to-Patron / recon conditions (no silent exemption)

Return rather than weaken acceptance when any of the following occurs (Q7 / Q8 / Q9):

1. **Already open (Q9)**: I1 Kestrel-section Url/absence reader is UNAVAILABLE under current VII — owner checkbox must be answered (VII departure (a)/(b) or Q2 rescope) before any validator implementation. No magic-string, new options record, reflection, code endpoint, or silent fail-all while waiting.
2. A code-defined Kestrel endpoint (`Listen` family / `UseSystemd` / endpoint-defining `ConfigureKestrel`) is required for this delivery.
3. Startup tests contradict the derived precedence or cannot reconcile configuration and the address feature at `IHostedService.StartAsync` in the net10.0 shipped API host — ask which surface authoritatively carries the effective listener set for that case. I7 timing/parity remains Phase B T005 mandatory proof.
4. Any future proposal to surface a custom 403/404 detail or message-carrying `NotFoundException` — return under §2.3(4)/(6) before any handler/Core edit. Current bodies are already sufficient (Q9).
5. Aspire smoke shows play forwarding through the web proxy and the required AppHost edit exceeds retaining/applying `WithEndpointProxySupport(false)`.

## Gates (after Gate 1 and implementation)

Gates run sequentially (shared worktree `obj`/`bin`).

1. Changed-C# scope: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (+ refactor threshold from harness.yml), `./scripts/run-jetbrains-inspectcode.ps1`.
2. Affected suites then full `dotnet test`.
3. Property / vulnerable-packages / `dotnet format --verify-no-changes` per harness; honest SKIPPED/SKIP/N/A/Could not run.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` for eligible changed production C# (no `--since` from worktrees).
5. Architecture + OpenAPI/contract drift checks by applicability; no hand-edited generated files.
6. Size M three-axis review (Sentry/Ledger/Compass) under brief closing bar; max two remediation rounds; unresolved blockers stop for explicit disposition.
7. Actual nginx/Aspire negative-launch smoke with positive local control; unrunnable smoke is outstanding, not PASS.
8. No-code-Listen source-search receipt at reviewed head + review.
9. Rebased gates before awaiting-merge; user merges.

Phase A runs none of these and claims no gate result.

## Risks

- **Phase B startup proof still outstanding**: Selected Q7 design does not replace mandated actual-startup/shipped-path evidence; contradictory evidence returns for recon (Q7; `recon-DEV-458-2` G1/G3). I7 timing/parity remains T005 mandatory.
- **Constitution VII I1 gap (OPEN — awaiting owner)**: I1 UNAVAILABLE; named I0/I2/I3/I4/I6 stand; silent fail-all rejected. Constitution Check VII stays OPEN until the exact Q9 owner checkbox is answered; no magic-string, new options record, reflection, or code endpoint (CONCLUSIONS Q9; constitution `:440-443`).
- **403/404 payload feasibility (RESOLVED by Q9)**: Existing titles `Forbidden`/`Not Found`, absent detail, parameterless NotFound, handler ignores messages; pin new exact-title/detail-absence/no-leak asserts in approved test zones; no handler/Core edit (`recon-DEV-458-2-3` §9).
- **Code-defined Listen endpoints**: Invisible to pre-start observers; mitigated by explicit no-Listen review invariant + source-search receipt; required endpoint returns to Patron (`recon-DEV-458-2` G2; Q7).
- **Stripping loopback proxy**: Request check cannot distinguish it from a local client; mitigated by Q8-pinned nginx deny + Aspire web proxy disabled + runtime smoke; absolute any-proxy claim and unlisted-header stripping proxies remain unsupported residuals (Q1; Q8.2–Q8.3).
- **Noted residuals (no ticket)**: launch flooding without throttle; preserved UseShellExecute semantics; dash-leading path/terminator and DEV-438 containment only if pursued later with recon (Q8.3).
- **IFileSystem registration order**: If playback factory cannot resolve existing registration, return to Patron — do not add a parallel seam (Q3; `recon-DEV-458-2:50-59`).
- **Check/start race**: Accepted as generic 500; do not invent atomic filesystem/process semantics (Q3).
- **Contract drift surprise**: Route already declares 403/404/422; unexpected generated diffs must be explained (Q4).
- **DEV-438 drift**: Phase B T000 rechecks main/DEV-438 before implementation (brief Task ordering).

## Ordering

Matches brief Task ordering: selected Q7 composition → startup boundary → request guard → launcher outcomes → shipped paths → gates/smoke/three-axis review/invariant receipt. User stories US1→US3 are behavioural P1; US4 is verification package P2.

## ADR status (durable)

Proposed ADR 0036 remains `proposed` until user Gate 1 spec-PR merge and explicit owner/Keel Accepted lifecycle update. Quill does not Accept the ADR in Phase A.
