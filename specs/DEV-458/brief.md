# DEV-458 — Harden the local launcher: grill brief

Status: grill closed with Patron after six questions (maximum twelve). Phase A design input for Quill; plan not frozen, Gate 1 not approved, no implementation or runtime acceptance claimed.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-458-spec`
Branch: `feature/458-spec`
Intake base: `659a2e9b739c092dc8565ee31ed6bc5bcc4b68e3`
Size: M; parent DEV-100; dependency DEV-438; UI tag: none.
ADR: `docs/adr/0036-local-launcher-trust-boundary.md` (Proposed).

## Authority and evidence

- Task note `DEV-458:3-14`: intake identity, size, branch/base and claimed ADR.
- `recon-DEV-458:9`: tracker scope requires checking file existence before launch, 422 for no configured player supporting the format, startup refusal for enabled LocalPlay on non-loopback binds, and verification that a proxy cannot expose launching remotely. This supplied recon is the ticket evidence; this grill makes no independent tracker or code search claim.
- `recon-DEV-458:15-48`: existing launcher, handler ordering, endpoint status declarations, exception mappings and test anchors; `:50-57`: care-list touch analysis; `:59-62`: ADR reservation.
- `specs/PRODUCT.md` sections 3-5 and charter section 2.3 govern scope, cited structural rulings and taste logging. Constitution Principles I/VII and API/contract rules govern existing ports, configuration, LocalPlay and generated contracts.
- `CONCLUSIONS.md` preserves every question and full Patron answer. `recon-DEV-458-2` section 7 supplies hosting documentation and verifies Q3 filesystem registration, with the conditional RabbitMQ-registration nuance. Q7 records the follow-on hosting decision and proof obligations. This brief adds no separate discovery claim.

## Frozen scope and loop discipline (Q5)

Deliver Q1 request/proxy protection, Q2 proven-loopback startup refusal, Q3 existence-first and unmapped-format launch outcomes, and Q4 necessary configuration, tests and documentation; anything else is a follow-up issue, not a finding in this round.

For code-review/ship-review, Critical/High findings block. Any in-scope ticket-acceptance, security-invariant, constitution, API-contract or required-gate failure blocks regardless of severity. Discretionary abstraction/style changes outside scope are follow-ups. Size M requires Sentry, Ledger and Compass; a missing axis blocks adjudication. Two rounds maximum for plan challenge and two for implementation review; unresolved blockers at the cap stop for explicit disposition, never automatic clearance or a silent third round.

One owner escalation is now open under Q9: blocked: structural — constitution VII departure or Q2 rescope. No departure or ticket change is approved. Patron cannot grant Gate 1, close the owner checkbox, or grant merge approval.

## Settled decisions and complete answer index

### Q1 — Request and proxy trust boundary

Patron approved the request boundary and mandatory deployment exclusion under section 2.3(4/5), citing ticket scope, existing 403 declaration and PRODUCT sections 3/5.

For `POST /api/movies/{id}/play`, accept only a proven loopback raw transport peer and absence of every provenance header: `Forwarded`, `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto`, `X-Real-IP`. Header presence suffices regardless of case, empty value or multiple values. A missing/null or non-loopback peer fails closed. Q4 requires IPv4-mapped IPv6 normalization before loopback checks. Do not enable forwarded-header processing or rewrite this route's transport peer.

Reject failed request-boundary checks with existing 403 ProblemDetails before catalog lookup, filesystem work, process start or playback-history recording. Keep existing route, DTOs and declared statuses.

A loopback proxy stripping provenance is indistinguishable from a direct local HTTP client. Shipped paths use the pinned proxy mechanisms in Q8 below; LocalPlay-disabled defaults do not substitute for a pinned exclusion. A stripping loopback proxy exposing that route while LocalPlay is enabled is unsupported; an absolute arbitrary-proxy guarantee is not claimed. Verify the actual shipped nginx and Aspire paths, including direct API and web-proxy behavior, rather than inferring security from configuration.

### Q2 — Fail-start invariant

Patron approved under section 2.3(5): with `Features:LocalPlay=true`, every effective API listener must be proven loopback before any request is served. Accepted forms are literal `localhost`, IPv4 loopback addresses in 127.0.0.0/8, IPv6 `::1`, and Q4's explicitly normalized mapped-loopback addresses.

Reject wildcard listeners (`0.0.0.0`, `::`, `*`, `+`), other hostnames even if DNS currently resolves locally, non-loopback literals, mixed safe/unsafe sets, unknown/unprovable forms, Unix sockets and named pipes. Fail startup and exit; a runtime request guard or warning cannot replace this requirement. LocalPlay disabled adds no bind restriction.

Validate the actual effective listeners after URL, Kestrel, environment and Aspire overrides. No new config key or test bypass is approved. Tests explicitly supply provable loopback listeners/peers.

**Hosting-hook decision:** The pre-freeze recon requirement was answered by `recon-DEV-458-2` section 7 and Patron's step 3.3 ruling recorded below as Q7. Select the dedicated API `IHostedService.StartAsync` mechanism and its explicit guard/proof obligations; do not retain an unknown-hook placeholder. This selects the design only: actual-startup and shipped-path evidence remains mandatory in Phase B, and plan freeze/Gate 1 are not claimed.

### Q3 — Launcher validation and failure precedence

Patron approved under section 2.3(4/5): preserve DEV-438 primary-video selection and the existing handler order (catalog, EnsureEnabled, resolve primary video, Launch, sample clock/history). Inside the existing Infrastructure launcher:

1. Check the selected file through existing `IFileSystem` immediately before the player/start sequence.
2. If absent, throw existing parameterless `NotFoundException`: existing 404 ProblemDetails (`Not Found`, no detail), per Q9.
3. Resolve the configured player. If none supports the format, throw existing `ValidationException`: existing 422 ProblemDetails, safe format-level detail only; never file or player paths.
4. Start using existing `IProcessStarter`, preserved `UseShellExecute` behavior, and exactly one `ArgumentList` file argument. No new shell parsing.

Existence takes precedence over an unmapped player. Both 404 and 422 cause zero process starts and zero history writes. Player mapping remains case/dot-insensitive. The existence check is not atomic with process start: subsequent deletion, unavailable executable or genuine start failures remain generic 500, with no retry and no history write. Do not introduce special start-failure translation or detailed error exposure.

Patron verified System.IO.Abstractions/IFileSystem is already referenced and registered; reuse it in the launcher and existing playback DI factory. A registration-order issue returns to Patron rather than creating another seam.

### Q4 — Footprint and tests

Patron approved section 2.3(1/2/3/6) touches bounded to the existing launcher/DI, API request/startup composition, conditionally ServiceDefaults, necessary shipped nginx/Aspire config and focused tests. No new dependency, project, top-level folder, abstraction layer, schema/migration or secret. No UI or glossary changes.

GlobalExceptionHandler edits are not approved: existing mappings already cover 403/404/422/500. Any demonstrated need returns to Patron first. Edits outside approved zones, including Core/selector/handler, docker-compose or web source, need a separate ruling. Generated contracts may refresh only through the C# DTO → OpenAPI → generated TypeScript chain if a gate requires it; no contract diff is expected and any generated diff must be explained.

### Q5 — Closing terms

Patron confirmed the frozen scope, closing bar, three-axis requirement, two-round caps, Phase A boundary and hosting/runtime evidence requirements stated above. Shared understanding reached; no owner escalation and no taste decision.

### Q6 — Precise side-effect assertions

Patron clarified Q4's broad wording: no catalog/no disk/no process/no history applies to Q1 request-boundary refusals. LocalPlay-disabled preserves catalog lookup then EnsureEnabled, with no subsequent disk/process/history. Q3 validation/start failures permit earlier catalog/selection/disk work; 404/422 never start a process and every failure avoids history. ASSUMPTIONS.md is an empty-of-decisions audit ledger with zero assumed entries, as explicitly requested by Conductor. This clarification controls the wording of Q4's test assertions.

### Q7 — Step 3.3 hosting mechanism and recon-gap disposition

Patron deliberately ruled under Q2's return condition and section 2.3(5), with Q4 footprint authority. Full initiating asks and answer, including the constitution/ordering supplement, are recorded in `CONCLUSIONS.md` Q7. Basis: `recon-DEV-458-2:12-48` (Microsoft Learn hosting and endpoint documentation, gaps G1-G3), constitution I/VI/VII, and Q2/Q4/Q5. No ticket-text or constitution departure is approved.

1. Register a dedicated API `IHostedService` in `src/LamuFlix.Api/Program.cs`; confine helpers to existing API folders. Its `StartAsync` completes synchronously or throws before server startup. No fire-and-forget or `ApplicationStarted` validation; options startup validation is not the primary hook. No ServiceDefaults change. The framework timing basis is the Microsoft Learn [hosted-services documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0#ihostedservice-interface) and [startup sequence](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-10.0), as supplied by Wisp. Actual registration order and hosted-service concurrency must preserve and be tested for the pre-server guarantee; the interface name alone proves nothing.
2. Observe both merged framework configuration inputs and `IServerAddressesFeature`, including `PreferHostingUrls`. Resolve Kestrel-section endpoints over URL-family endpoints unless hosting URLs are preferred; apply URL-over-HTTP_PORTS/HTTPS_PORTS precedence and the documented empty-config `http://localhost:5000` default. Reconcile observations according to precedence; present but irreconcilable inputs fail closed rather than choosing the safe-looking surface. Preserve Q2 loopback grammar and Q4 mapped-address normalization; disabled LocalPlay adds no bind restriction.
3. Constitution VII remains binding: framework inputs must be obtained through existing approved options-binding composition or documented framework constants/APIs satisfying that rule. No ad hoc raw configuration-key reads, new public configuration key, or new options record is pre-approved. If a required input cannot be obtained within these constraints, return to Patron with the exact missing surface; do not waive VII. A necessary constitution departure goes through the existing structural owner-checkbox process and cannot be silently authorized here.
4. No code-defined Kestrel endpoint may be introduced in this delivery: `Listen`, `ListenAnyIP`, `ListenLocalhost`, `ListenUnixSocket`, `ListenNamedPipe`, `ListenHandle`, `UseSystemd`, or endpoint-defining `ConfigureKestrel`. Recon G2 found no public pre-start enumeration API and no such baseline endpoints. Enforce this as an explicit review invariant with a `src/` source-search receipt at the reviewed head, not as a runtime detection claim. Any required such endpoint returns to Patron with fresh recon before proceeding. URL-family, Kestrel-section, ports-shorthand and Aspire/environment bindings remain in scope.
5. Phase B real-startup tests must cover Kestrel-over-URL precedence, both PreferHostingUrls states, HTTP_PORTS/HTTPS_PORTS and URL overrides, empty-config default, mapped addresses, mixed unsafe sets, LocalPlay on/off, and refusal before any request/socket-serving window. Pin validator/server registration order and concurrency. Observe the actual shipped Aspire API listener environment and reconcile it with the validator; run shipped nginx/Aspire smoke with process-start observation and a positive local control. Parser-only tests cannot close G1/G3.
6. If startup tests contradict the derived precedence or cannot reconcile configuration and the address feature, return for recon: which surface at `IHostedService.StartAsync` in the net10.0 shipped API host authoritatively carries the effective listener set for that case? Do not silently weaken acceptance or claim those runtime obligations passed during planning.

Wisp also confirms Q3's existing filesystem seam: unconditional API registration is `Program.cs:36` -> `HandlerRegistration.cs:32`; the playback singleton factory resolves lazily after registration. RabbitMQ registration is conditional and is not the API proof (`recon-DEV-458-2:50-59`). No new seam or registration-order workaround is needed at the pinned baseline.

## Planned file responsibilities (Q4)

| Area | Planned files or bounded location | Purpose |
| --- | --- | --- |
| Launcher | `src/LamuFlix.Infrastructure/Playback/ProcessMediaPlayerLauncher.cs`, `PlaybackServiceCollectionExtensions.cs` | Existing filesystem seam, existence/422 ordering, existing DI factory |
| Request/startup boundary | `src/LamuFlix.Api/Endpoints/PlaybackEndpoints.cs`, `src/LamuFlix.Api/Program.cs`, focused helpers within existing API folders | Early route-specific request guard; pre-serving effective-listener validation |
| Shared defaults | `src/LamuFlix.ServiceDefaults/Extensions.cs` | No change: Q7 selects API-only composition |
| Shipped proxies | `docker/nginx.conf`, `src/LamuFlix.AppHost/Program.cs` | Only necessary Q1 route exclusion/LocalPlay-disabled deployment and Q2 bind enforcement |
| Launcher tests | `tests/LamuFlix.UnitTests/Playback/ProcessMediaPlayerLauncherTests.cs`, `PlaybackServiceCollectionExtensionsTests.cs` | File/player precedence, mapping, single argument, failure/no-start behavior |
| API/startup tests | `tests/LamuFlix.IntegrationTests/PlaybackEndpointTests.cs`, `ApiEndToEndPlaybackTests.cs`, `PlaybackHistoryEndpointTests.cs`, `ApiHostCompositionTests.cs`; focused host/options tests including existing `tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs` | Peer/header matrix, disabled ordering, failures/history, actual startup behavior |
| Documentation | `specs/DEV-458/`, `docs/adr/0036-local-launcher-trust-boundary.md` | Decisions and downstream spec/plan/tasks/verification evidence |

Exact helper/test filenames must be finalized by Quill within the selected API zones; Q7 fixes the hosting hook and excludes ServiceDefaults changes. No new layer and no production/test edits in Phase A.

## Test strategy and gate expectations

- Unit tests use existing libraries and seams. Cover existing file + mapped player success, missing file, unmapped format, both failures (404 wins), case/dot-insensitive format mapping, hostile filename as one argument, and process-start failure. Update the old unmapped-format test from InvalidOperationException to ValidationException and arrange real mock-filesystem files for successful paths.
- API tests cover IPv4/IPv6/mapped loopback success; null/non-loopback peers; every provenance-header presence with case/empty/multiple variants; and side-effect order precisely per Q6. Replace the existing unmapped-format 500 assertion with safe 422. Keep folder-missing 404, no-video 409, validation 422, success 204, disabled 403 and history regressions.
- Startup tests exercise actual effective-listener precedence: URL, Kestrel, environment and Aspire overrides; multiple listeners; wildcard/hostname/unsupported forms; LocalPlay on/off; and refusal before any request is served. Configuration-parser-only tests are insufficient.
- Runtime evidence must exercise running shipped nginx/Aspire paths with process-start observation and demonstrate no remotely/proxy-triggered launch. Include a direct allowed local launch control so a universally broken launcher cannot satisfy negative tests. Never substitute a mocked proxy or configuration reading for the required shipped-path smoke.
- At Phase B, run applicable harness verification at the reviewed head: Roslyn, complexity, InspectCode, full tests, property tests, package security, format, mutation, architecture and contract drift checks. Follow harness.yml thresholds and script-supported parameters; mutation uses no worktree `since`. No generated-file hand edits or gate reductions.
- Preserve PASS, SKIPPED, N/A and Could not run distinctly; an unrunnable required gate or runtime smoke is outstanding, not acceptance. This docs-only grill ran no implementation gates or runtime smoke.

## Task ordering for Quill

1. Incorporate completed Wisp section 7 hosting/filesystem recon and Patron Q7 into the draft; retain its explicit review invariant and Phase B proof/return conditions.
2. Draft spec, plan and tasks from this brief, all six answers and Proposed ADR 0036. Include requirement-to-task/test coverage for all four ticket behaviors and explicit runtime-smoke tasks.
3. Keel performs read-only speckit-analyze and brief-to-plan/tasks consistency check; one numbered repair list per round. Plan challenge respects the two-round cap. Hook selection is settled by Q7; clean analysis and plan challenge remain required before freeze, with actual-startup/shipped-path acceptance still a Phase B obligation.
4. After separate owner Gate 1, Phase B pickup rechecks main/DEV-438 drift before implementation. Implement proven startup boundary and endpoint request guard, then launcher outcomes and focused regression coverage; configure shipped paths within Q4 limits.
5. Run appropriate gates and actual proxy smoke, collect all three M review axes, adjudicate at the reviewed SHA, publish review findings, and hand off for the owner's merge decision. No merge or auto-merge.

Canonical terms remain the existing LocalPlay, primary video, configured player and playback history. No domain glossary addition was settled. This brief and Proposed ADR 0036 are ready to feed speckit-specify; readiness for drafting is not plan freeze or Gate 1.

## Q8 — Phase 2 step 6 round 1: Patron plan-challenge rulings

Status: recorded from Patron supplied rulings on 2026-11-07; NOT FROZEN. These refinements control earlier Q1/Q3 shorthand where it conflicts. The complete supplied ruling, its cited bases, residual risks and recon questions are preserved verbatim in CONCLUSIONS.md Q8. No ticket-text change, constitution departure or taste ruling is approved; Q9 now opens the structural owner decision. Two-round cap remains unchanged.

1. **Pinned shipped paths (Sentry H1/M3, Compass M1, Ledger M3):** Remove FR-007's open mechanism choice. In `docker/nginx.conf`, deny `POST /api/movies/{id}/play` with proxy 403 before API forwarding. Retain the shipped Compose LocalPlay-disabled default, but do not rely on it in place of this exclusion. No docker-compose/README edit. Any explanatory mechanism documentation belongs in `specs/DEV-458/`. For Aspire, retain disabled web endpoint-proxy support and permit no endpoint proxy to forward play. No AppHost edit unless Phase B smoke shows play forwarding through the web proxy; then only minimal retaining/applying `WithEndpointProxySupport(false)` is within the ruling, with anything beyond returned to Patron. Direct API exposure remains subject to Q1 peer/header checks and Q2 startup refusal. This is a selected mechanism and pending smoke obligation, not verified proxy behaviour.
2. **Named positive control:** Use a separate named LocalPlay-enabled loopback host, using the same build, configured player fixture and selected-file fixture as the negative smokes. Direct loopback/no-provenance play must return 204 with exactly one observed process start. This demonstrates a working launcher only; it does not imply the disabled/excluded negative topology can launch. nginx negative smoke expects proxy 403/zero starts. Aspire web-proxy attempt expects no forwarded launch/zero starts; header-bearing or non-loopback direct-API attempt expects API 403/zero starts. T022/T023 remain mandatory per path, actual Aspire listener injection must be observed, and required smoke marked Could not run continues to block clearance under T032.
3. **Five headers retained (Sentry H3):** Exactly `Forwarded`, `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto`, `X-Real-IP`. No wider header list. Non-shipped loopback proxies using only unlisted headers or stripping all provenance with LocalPlay enabled remain unsupported; noted residual, no ticket. A demonstrated shipped-path header fact returns to Patron.
4. **No scope expansion (Sentry M2/M5/L1/L2):** No throttle/single-flight/concurrency cap, new containment assertion, universal `--` separator or UseShellExecute change. Preserve Q3/FR-012 and existing hostile-filename single-argument test scope. Launch flooding and preserved process semantics are noted residuals with no ticket. Dash-leading selected-path/player-terminator facts and DEV-438 containment guarantees require recon only if pursued later; no follow-up ticket is authorized now.
5. **Safe 404 body (Sentry M4, superseded by Q9):** Preserve existing parameterless NotFoundException, fixed 404 title `Not Found`, and absent detail. No custom throw message, new constructor or handler/Core edit. Q9 pins new no-detail/no-leak assertions in the approved test files; Q8 forbidden-content criteria remain.
6. **Truthful existing 403 payload (Ledger M4, superseded by Q9):** Preserve existing FeatureDisabledException mapping, fixed 403 title `Forbidden`, and absent detail. No custom detail text, new exception/mapping or handler/Core edit. Request-boundary and disabled cases retain Q6 ordering; Q9 pins no-detail/no-leak assertions.
7. **Resolved recon / current blocker (Q9):** Wisp sections 8/9 explicitly report I1 Kestrel pre-bind accessor unavailable and existing generic 403/404 construction sufficient. I0/I2/I3/I4/I6 accessors are named below. I7 timing/parity remains Phase B real-host proof. Constitution Check stays OPEN, now awaiting the structural owner decision below rather than unspecified accessor recon. Freeze and Gate 1 remain blocked; no magic-string read, new options record, reflection, code endpoints or silent fail-all validator approved.

Basis: Patron Q8 full ruling in CONCLUSIONS.md; ticket four behaviours supplied by recon-DEV-458; Q1-Q7; PRODUCT sections 3/5 care items 4-5; constitution VII; supplied recon-DEV-458-2. Quill incorporates these into the existing round-1 repair list after Keel records them; accepted documentary repairs require independent reread before closure.
## Q9 — Phase 2 step 6 supplement: owner decision and existing error bodies

Status: **blocked: structural — constitution VII departure**. PLAN NOT FROZEN; Gate 1 closed. Full cited Patron ruling is preserved verbatim in CONCLUSIONS.md Q9. This section controls Q7/Q8 and earlier no-escalation or custom-payload wording where inconsistent. No taste ruling, production edit, constitution waiver or ticket rescope is approved.

### Listener facts and decision boundary

Supplied recon `recon-DEV-458-2-2` section 8.4 and `recon-DEV-458-2-3` section 8 table report I1 Kestrel-section effective Url access **UNAVAILABLE** across eight documented net10.0 surfaces. Neither values nor section absence can be proven under current VII constraints; therefore the default branch is also unprovable, and no LocalPlay=true configuration can be certified. Baseline absence of Kestrel configuration is not runtime proof. Patron identifies no compliant alternative. Refusing every enabled startup is not an approved implementation: it defeats valid local launch and the positive control.

Available named inputs remain: I0 existing `IOptions<FeatureOptions>`; I2 `IConfiguration[WebHostDefaults.ServerUrlsKey]`; I3 `WebHostDefaults.HttpPortsKey` / `HttpsPortsKey`; I4 `WebHostDefaults.PreferHostingUrlsKey` plus feature `PreferHostingUrls`; I6 container `IServer.Features.Get<IServerAddressesFeature>()`. These are the previously approved framework surfaces, not a cure for I1. I7 address-feature population/prefer-hosting copy timing remains undocumented; Phase B T005 real-host observation and fail-closed reconciliation remain mandatory, not a second owner question.

### Owner checkbox — must be carried on the spec PR, left unchecked

- [ ] blocked: structural — DEV-458 Q2 requires proving every effective listener loopback before serving when Features:LocalPlay=true, including whether the Kestrel configuration section defines endpoints and with what Url values (that section overrides all URL-family configuration). On net10.0 no documented API reads those values, or proves the section empty, before server start without either (a) reading IConfiguration by Kestrel/Endpoints/Url key strings outside options binding, or (b) adding a new options record binding the Kestrel section — both departures from constitution VII (constitution.md:440-443). Do you approve such a VII departure for this validator (and if so, which of (a) or (b)), or should DEV-458 Q2 be rescoped so LocalPlay=true is not required to be provable against Kestrel-section endpoints?

A Patron response does not answer this checkbox. Until the user answers: Constitution Check OPEN, freeze blocked, Gate 1 closed, no validator implementation, no magic-string workaround, no new Kestrel options record, no reflection or code endpoints. The owner choice must be recorded and any required governance amendment/scope change reconciled before clearance; a proposed departure is not permission to implement it.

### Existing payloads preserved; new safety assertions

Supplied recon `recon-DEV-458-2-3` section 9 establishes current 403 title `Forbidden` and 404 title `Not Found`, both with no detail; handler ignores exception messages. Existing NotFoundException is parameterless with fixed `Not found.` message; disabled-launcher message `LocalPlay is disabled in this environment.` is not on the wire. Preserve these mappings, bodies and parameterless throw. No GlobalExceptionHandler/Core exception edit, new message constructor or custom detail is approved or needed. Q8 custom-message/detail expectations are superseded; forbidden-content criteria stand.

In the Q4-approved endpoint/e2e/history/launcher tests, require exact 403/404 titles and absent detail, no exception message (including the above fixed messages), no exception type name, filesystem path/basename/player path, peer address or provenance header name. Keep status, zero-side-effect and history ordering matrices. No future detailed-body change without Patron section 2.3(4)/(6) ruling.

### Plan repair / round discipline

Quill incorporates Q9 and the unchecked owner checkbox into spec/plan/tasks/checklist/receipt and spec-PR body preparation, replacing stale no-escalation, unknown-recon and custom-message/detail wording. Keel aligns owned ADR after recording this decision, then independently rereads Quill repairs. This is the remaining blocker repair within existing step-6 round 1; no extra challenger round or implicit cap extension. Other 29-finding dispositions and mandatory Phase B startup/proxy/gate evidence stand. Awaiting owner decision is not PLAN FROZEN or Gate 1 acceptance.
