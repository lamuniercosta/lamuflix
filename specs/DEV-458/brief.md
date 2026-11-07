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
- `CONCLUSIONS.md` preserves every question and full Patron answer. Q3 includes Patron-supplied verification of the existing IFileSystem registration; Wisp should carry that evidence into recon for planning. This brief adds no separate discovery claim.

## Frozen scope and loop discipline (Q5)

Deliver Q1 request/proxy protection, Q2 proven-loopback startup refusal, Q3 existence-first and unmapped-format launch outcomes, and Q4 necessary configuration, tests and documentation; anything else is a follow-up issue, not a finding in this round.

For code-review/ship-review, Critical/High findings block. Any in-scope ticket-acceptance, security-invariant, constitution, API-contract or required-gate failure blocks regardless of severity. Discretionary abstraction/style changes outside scope are follow-ups. Size M requires Sentry, Ledger and Compass; a missing axis blocks adjudication. Two rounds maximum for plan challenge and two for implementation review; unresolved blockers at the cap stop for explicit disposition, never automatic clearance or a silent third round.

No ticket change or constitution departure was requested; no owner escalation is open. Patron cannot grant Gate 1 or merge approval.

## Settled decisions and complete answer index

### Q1 — Request and proxy trust boundary

Patron approved the request boundary and mandatory deployment exclusion under section 2.3(4/5), citing ticket scope, existing 403 declaration and PRODUCT sections 3/5.

For `POST /api/movies/{id}/play`, accept only a proven loopback raw transport peer and absence of every provenance header: `Forwarded`, `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto`, `X-Real-IP`. Header presence suffices regardless of case, empty value or multiple values. A missing/null or non-loopback peer fails closed. Q4 requires IPv4-mapped IPv6 normalization before loopback checks. Do not enable forwarded-header processing or rewrite this route's transport peer.

Reject failed request-boundary checks with existing 403 ProblemDetails before catalog lookup, filesystem work, process start or playback-history recording. Keep existing route, DTOs and declared statuses.

A loopback proxy stripping provenance is indistinguishable from a direct local HTTP client. Shipped proxies must therefore exclude the play route from forwarding, or run with LocalPlay disabled. A stripping loopback proxy exposing that route while LocalPlay is enabled is unsupported; an absolute arbitrary-proxy guarantee is not claimed. Verify the actual shipped nginx and Aspire paths, including direct API and web-proxy behavior, rather than inferring security from configuration.

### Q2 — Fail-start invariant

Patron approved under section 2.3(5): with `Features:LocalPlay=true`, every effective API listener must be proven loopback before any request is served. Accepted forms are literal `localhost`, IPv4 loopback addresses in 127.0.0.0/8, IPv6 `::1`, and Q4's explicitly normalized mapped-loopback addresses.

Reject wildcard listeners (`0.0.0.0`, `::`, `*`, `+`), other hostnames even if DNS currently resolves locally, non-loopback literals, mixed safe/unsafe sets, unknown/unprovable forms, Unix sockets and named pipes. Fail startup and exit; a runtime request guard or warning cannot replace this requirement. LocalPlay disabled adds no bind restriction.

Validate the actual effective listeners after URL, Kestrel, environment and Aspire overrides. No new config key or test bypass is approved. Tests explicitly supply provable loopback listeners/peers.

**Pre-freeze recon requirement:** Wisp must name the precise ASP.NET Core hosting hook, cite Microsoft Learn documentation, and prove it observes effective listeners while failing before serving. No hook is selected by this brief. Plan freeze is prohibited until this proof exists; if it cannot satisfy ticket-required bindings, return to Patron.

### Q3 — Launcher validation and failure precedence

Patron approved under section 2.3(4/5): preserve DEV-438 primary-video selection and the existing handler order (catalog, EnsureEnabled, resolve primary video, Launch, sample clock/history). Inside the existing Infrastructure launcher:

1. Check the selected file through existing `IFileSystem` immediately before the player/start sequence.
2. If absent, throw existing `NotFoundException`: existing 404 ProblemDetails.
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

## Planned file responsibilities (Q4)

| Area | Planned files or bounded location | Purpose |
| --- | --- | --- |
| Launcher | `src/LamuFlix.Infrastructure/Playback/ProcessMediaPlayerLauncher.cs`, `PlaybackServiceCollectionExtensions.cs` | Existing filesystem seam, existence/422 ordering, existing DI factory |
| Request/startup boundary | `src/LamuFlix.Api/Endpoints/PlaybackEndpoints.cs`, `src/LamuFlix.Api/Program.cs`, focused helpers within existing API folders | Early route-specific request guard; pre-serving effective-listener validation |
| Conditional startup composition | `src/LamuFlix.ServiceDefaults/Extensions.cs` | Only if hosting-hook recon demonstrates this is the required composition point |
| Shipped proxies | `docker/nginx.conf`, `src/LamuFlix.AppHost/Program.cs` | Only necessary Q1 route exclusion/LocalPlay-disabled deployment and Q2 bind enforcement |
| Launcher tests | `tests/LamuFlix.UnitTests/Playback/ProcessMediaPlayerLauncherTests.cs`, `PlaybackServiceCollectionExtensionsTests.cs` | File/player precedence, mapping, single argument, failure/no-start behavior |
| API/startup tests | `tests/LamuFlix.IntegrationTests/PlaybackEndpointTests.cs`, `ApiEndToEndPlaybackTests.cs`, `PlaybackHistoryEndpointTests.cs`, `ApiHostCompositionTests.cs`; focused host/options tests including existing `tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs` | Peer/header matrix, disabled ordering, failures/history, actual startup behavior |
| Documentation | `specs/DEV-458/`, `docs/adr/0036-local-launcher-trust-boundary.md` | Decisions and downstream spec/plan/tasks/verification evidence |

Exact helper/test filenames and hosting hook must be finalized by Quill from Wisp recon within these existing zones, not by inventing another layer. No production/test files are changed in Phase A.

## Test strategy and gate expectations

- Unit tests use existing libraries and seams. Cover existing file + mapped player success, missing file, unmapped format, both failures (404 wins), case/dot-insensitive format mapping, hostile filename as one argument, and process-start failure. Update the old unmapped-format test from InvalidOperationException to ValidationException and arrange real mock-filesystem files for successful paths.
- API tests cover IPv4/IPv6/mapped loopback success; null/non-loopback peers; every provenance-header presence with case/empty/multiple variants; and side-effect order precisely per Q6. Replace the existing unmapped-format 500 assertion with safe 422. Keep folder-missing 404, no-video 409, validation 422, success 204, disabled 403 and history regressions.
- Startup tests exercise actual effective-listener precedence: URL, Kestrel, environment and Aspire overrides; multiple listeners; wildcard/hostname/unsupported forms; LocalPlay on/off; and refusal before any request is served. Configuration-parser-only tests are insufficient.
- Runtime evidence must exercise running shipped nginx/Aspire paths with process-start observation and demonstrate no remotely/proxy-triggered launch. Include a direct allowed local launch control so a universally broken launcher cannot satisfy negative tests. Never substitute a mocked proxy or configuration reading for the required shipped-path smoke.
- At Phase B, run applicable harness verification at the reviewed head: Roslyn, complexity, InspectCode, full tests, property tests, package security, format, mutation, architecture and contract drift checks. Follow harness.yml thresholds and script-supported parameters; mutation uses no worktree `since`. No generated-file hand edits or gate reductions.
- Preserve PASS, SKIPPED, N/A and Could not run distinctly; an unrunnable required gate or runtime smoke is outstanding, not acceptance. This docs-only grill ran no implementation gates or runtime smoke.

## Task ordering for Quill

1. Schedule Wisp hosting-hook/effective-binding recon and Microsoft Learn proof before freeze; incorporate the actual ticket acceptance text and Patron filesystem-registration evidence in recon.
2. Draft spec, plan and tasks from this brief, all six answers and Proposed ADR 0036. Include requirement-to-task/test coverage for all four ticket behaviors and explicit runtime-smoke tasks.
3. Keel performs read-only speckit-analyze and brief-to-plan/tasks consistency check; one numbered repair list per round. Plan challenge respects the two-round cap. Do not freeze on the outstanding hosting-hook proof.
4. After separate owner Gate 1, Phase B pickup rechecks main/DEV-438 drift before implementation. Implement proven startup boundary and endpoint request guard, then launcher outcomes and focused regression coverage; configure shipped paths within Q4 limits.
5. Run appropriate gates and actual proxy smoke, collect all three M review axes, adjudicate at the reviewed SHA, publish review findings, and hand off for the owner's merge decision. No merge or auto-merge.

Canonical terms remain the existing LocalPlay, primary video, configured player and playback history. No domain glossary addition was settled. This brief and Proposed ADR 0036 are ready to feed speckit-specify; readiness for drafting is not plan freeze or Gate 1.
