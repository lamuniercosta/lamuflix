# Feature Specification: Harden the local launcher

**Feature Branch**: `feature/458-spec`

**Created**: 2026-11-07

**Status**: Draft — **PLAN NOT FROZEN**; Gate 1 closed. Owner structural decision open under brief/CONCLUSIONS Q9 (`blocked: structural — constitution VII departure or Q2 rescope`). Known payload feasibility resolved by Q9; Constitution Check VII remains **OPEN** awaiting owner answer. No implementation clearance.

**Input**: Ticket DEV-458 (Size M, parent DEV-100, dependency DEV-438; UI Tag none); grill-closed `specs/DEV-458/brief.md` (Q1–Q9); Patron rulings in `CONCLUSIONS.md` (including Q7–Q9); empty-of-decisions `ASSUMPTIONS.md`; Proposed ADR `docs/adr/0036-local-launcher-trust-boundary.md`; Maestri notes `DEV-458`, `recon-DEV-458`, `recon-DEV-458-2` section 7, `recon-DEV-458-2-2` section 8, `recon-DEV-458-2-3` section 9; `specs/PRODUCT.md`. No decisions beyond the brief and its rulings. Quill invents nothing.

## Domain vocabulary *(mandatory)*

Canonical terms remain the existing ones (brief Task ordering; Q5). No glossary or `CONTEXT.md` addition was settled:

- **LocalPlay** — Feature flag `Features:LocalPlay` that registers the enabled media-player launcher; when off, play refuses with existing 403.
- **Primary video** — The one eligible file selected from a movie folder by the existing DEV-438 shared selector (unchanged by this ticket).
- **Configured player** — A `PlaybackOptions.Players[]` entry whose Formats list covers the selected file format (case/dot-insensitive).
- **Playback history** — Existing playback-event recording after a successful Launch return.

_Avoid_: inventing new trust tokens, mTLS, named-pipe channels, glossary terms, or UI copy.

## Scope

**In scope** (brief Frozen scope Q5; Q1–Q4; Q7 selected hosting design; Q8 plan-challenge pins as refined by Q9; ADR 0036; ticket via recon-DEV-458:9):

1. **Request/proxy trust boundary** for `POST /api/movies/{id}/play`: allow only a proven loopback raw transport peer and absence of exactly the five provenance headers (`Forwarded`, `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto`, `X-Real-IP` — no wider list); header presence fails closed regardless of case, empty value, or multiples; missing/null or non-loopback peer fails closed; IPv4-mapped IPv6 peers normalized before loopback checks; reject with existing FeatureDisabledException → existing 403 ProblemDetails **before** catalog lookup, filesystem work, process start, or history, preserving fixed title `Forbidden` and **absent detail** (handler ignores exception messages; no custom detail text; no disabled-feature claim, peer address, provenance header name, exception type name, or exception message on the wire) (Q1; Q4; Q6; Q8 forbidden-content criteria; Q9); do not enable forwarded-header processing or rewrite the route's transport peer; keep existing route, DTOs, and declared statuses. No GlobalExceptionHandler/Core exception edit.
2. **Fail-start LocalPlay bind invariant** via the **selected** dedicated API `IHostedService.StartAsync` registered in `Api/Program.cs` (synchronous validation or throw before server start; no ServiceDefaults change — brief Q7; CONCLUSIONS Q7; Learn hosted-services/startup timing via `recon-DEV-458-2:18-20`): with `Features:LocalPlay=true`, every effective API listener must be proven loopback before any request is served (literal `localhost`, IPv4 127.0.0.0/8, IPv6 `::1`, and Q4 normalized mapped-loopback); reject wildcards, other hostnames (DNS-independent), non-loopback literals, mixed sets, unknown/unprovable forms, Unix sockets, and named pipes; fail startup and exit; LocalPlay disabled adds no bind restriction; no new config key or test bypass; tests supply provable loopback listeners/peers on a real Kestrel host (Q2; Q4; Q7). Design selection is distinct from pending Phase B actual-startup proof. **PLAN NOT FROZEN**; Gate 1 closed. Constitution Check VII remains **OPEN** awaiting the structural owner decision (Q9) — not awaiting further accessor inventing; silent fail-all of every LocalPlay=true startup is rejected.
3. **Launcher validation precedence**: preserve DEV-438 primary-video selection and handler order (catalog → EnsureEnabled → resolve primary → Launch → clock/history). Inside the existing Infrastructure launcher: existence check via existing `IFileSystem` first → missing file = existing **parameterless** `NotFoundException()` → existing 404 ProblemDetails with fixed title `Not Found` and **absent detail** (never selected path/basename/LibraryPath/player paths; no custom message/constructor); then resolve configured player → none = existing `ValidationException`/422 with safe format-level detail only; then existing `IProcessStarter` with preserved `UseShellExecute` and exactly one `ArgumentList` file argument (no `--` separator). Existence precedes unmapped player. Both 404 and 422 yield zero process starts and zero history. Check/start race and genuine start failures remain generic 500, no retry, no history. No throttle/containment expansion (Q3; Q6; Q8; Q9).
4. **Bounded footprint, tests, shipped-path verification, and docs**: changes only in the brief Q4 Planned file responsibilities zones as refined by Q7/Q8/Q9 (API-only hosting composition; ServiceDefaults unchanged); no new dependency, project, top-level folder, abstraction layer, schema/migration, or secret; no UI/glossary; no GlobalExceptionHandler/Core exception edit; generated contracts only through the C# → OpenAPI → TypeScript chain if a gate requires refresh (no expected contract diff); unit/API/real-Kestrel startup tests and actual nginx/Aspire negative-launch runtime smoke with a **separate named** LocalPlay-enabled loopback positive-control host sharing the same build/player/file fixtures; pin new exact-title / detail-absence / no-message / no-type / no-path-basename-player / no-peer / no-header-name leak assertions in approved existing test zones while preserving Q6 order and side effects (Q4; Q6; Q7; Q8; Q9).

**Out of scope** (brief Frozen scope; Q1–Q5; Q7; ADR 0036):

- Absolute arbitrary-proxy guarantee against a stripping loopback proxy (unsupported; would be `blocked: structural`).
- New trust mechanism (token, mTLS, named-pipe, alternate execution channel).
- Enabling `UseForwardedHeaders` / peer rewrite for the play route.
- New config key, test-only bypass, Unix-socket/named-pipe LocalPlay support; new options record for listener config; ad hoc magic-string configuration reads (constitution VII; Q7).
- Code-defined Kestrel endpoints (`Listen` family, `UseSystemd`, endpoint-defining `ConfigureKestrel`) in this delivery — review + source-search invariant, not a runtime enumeration claim (Q7; `recon-DEV-458-2` G2).
- Core port/`IMediaPlayerLauncher` contract change; DEV-438 selector/handler edits; GlobalExceptionHandler edits; docker-compose or `web/` source; schema/migrations; new packages/projects/layers/secrets; ServiceDefaults change for this invariant.
- Special start-failure taxonomy or detailed path/executable exposure.
- Anything outside Q1–Q4 necessary configuration/tests/docs — follow-up issue, not a finding in this round.

**Precedence**: Ticket Scope & Technical Design (recon-DEV-458:9) plus Patron Q1–Q9 in `brief.md` / `CONCLUSIONS.md` are authoritative (`PRODUCT.md` §3/§5). Proposed ADR 0036 records the trust-boundary tradeoff (Keel-authored; Quill does not rewrite). Phase A produces documents only. **One owner escalation is open** under Q9: `blocked: structural — constitution VII departure or Q2 rescope`. The unchecked owner checkbox below must appear on the spec PR; a Patron answer never closes it. Ordinary Gate 1 stays closed until the user answers. Patron cannot grant Gate 1, close the checkbox, or grant merge approval.

**Selected hosting design (not Phase B proof / not freeze)**: brief Q7 and CONCLUSIONS Q7 select a dedicated API `IHostedService.StartAsync` registered in `src/LamuFlix.Api/Program.cs`, with synchronous validation or throw before server start and no ServiceDefaults change, citing `recon-DEV-458-2` section 7 and Learn hosted-services/startup docs. Actual-startup and shipped-path evidence remain mandatory Phase B obligations; **PLAN NOT FROZEN**; Gate 1 closed.

**Checked-in effective-listener precedence pointer (US1 / FR-001; Compass L4)**: Follow brief Q7.2 and CONCLUSIONS Q7 §2 / supplement 6 (governing over earlier shorthand). Summarized: if the Kestrel configuration section defines endpoints and `preferHostingUrls` is false, Kestrel-section `Url` values are the effective set and override URL-family configuration; otherwise use the merged URL family (`ASPNETCORE_URLS` / `--urls` / `urls` / `UseUrls` / `WebApplication.Urls`), with `ASPNETCORE_URLS` overriding `HTTP_PORTS`/`HTTPS_PORTS`; if no endpoint is configured anywhere, the documented net10.0 default `http://localhost:5000` applies and is proven loopback (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0; https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0#kestrel-endpoint-configuration). Empty/unpopulated feature vs proven merged inputs vs true empty-config default vs unprovable:

- **Absent / not-yet-populated `IServerAddressesFeature`**: do **not** infer “no inputs” or accept vacuous empty-address proof; keep reading merged configuration inputs and fail closed when LocalPlay requires a proven set that cannot be established.
- **Independently proven merged configuration inputs**: usable when obtained through the named available VII-compliant surfaces below (I0/I2/I3/I4/I6).
- **True empty-config default**: applies only when no endpoint is configured anywhere → documented `http://localhost:5000`; pin net10.0 tests to that documented default and fail visibly on default drift (Sentry L3). Under Q9, I1 absence is also unprovable, so this default branch is not certifiable until the owner checkbox is answered; silent fail-all is still rejected.
- **Present but unprovable / irreconcilable inputs**: fail closed under LocalPlay; never select the safe-looking surface (brief Q7.2).

**Known accessor inventory (Q9; `recon-DEV-458-2-2` §8 / `recon-DEV-458-2-3` §8)**:

- **I0 AVAILABLE** — `IOptions<FeatureOptions>` (existing `Features:LocalPlay` binding).
- **I1 UNAVAILABLE** — no permitted pre-bind Kestrel-section effective `Url` / section-absence reader on net10.0 under current VII constraints (`recon-DEV-458-2-2` §8.4). No LocalPlay=true configuration is certifiable against Kestrel-section endpoints without a VII departure or Q2 rescope. Reject silent fail-all. No magic-string read, new Kestrel options record, reflection, or code endpoint approved.
- **I2 AVAILABLE** — `IConfiguration[WebHostDefaults.ServerUrlsKey]`.
- **I3 AVAILABLE** — `WebHostDefaults.HttpPortsKey` / `HttpsPortsKey`.
- **I4 AVAILABLE** — `WebHostDefaults.PreferHostingUrlsKey` plus feature `PreferHostingUrls`.
- **I6 AVAILABLE** — container `IServer.Features.Get<IServerAddressesFeature>()`.
- **I7 Phase B** — empty vs unpopulated `IServerAddressesFeature` population / PreferHostingUrls-copy timing remains undocumented; T005 real-host observation and fail-closed reconciliation remain mandatory (not a second owner question).

**Owner checkbox — must be carried on the spec PR, left unchecked** (exact brief Q9 text):

- [ ] blocked: structural — DEV-458 Q2 requires proving every effective listener loopback before serving when Features:LocalPlay=true, including whether the Kestrel configuration section defines endpoints and with what Url values (that section overrides all URL-family configuration). On net10.0 no documented API reads those values, or proves the section empty, before server start without either (a) reading IConfiguration by Kestrel/Endpoints/Url key strings outside options binding, or (b) adding a new options record binding the Kestrel section — both departures from constitution VII (constitution.md:440-443). Do you approve such a VII departure for this validator (and if so, which of (a) or (b)), or should DEV-458 Q2 be rescoped so LocalPlay=true is not required to be provable against Kestrel-section endpoints?

Until the user answers: Constitution Check OPEN, freeze blocked, Gate 1 closed, no validator implementation, no magic-string workaround, no new Kestrel options record, no reflection or code endpoints. A proposed departure is not permission to implement it.

**Gate 1 / Phase B**: Gate 1 stays closed while the owner checkbox is unanswered. After user answer + Gate 1 approval and user-owned spec PR merge, Phase B pickup rechecks main/DEV-438 drift before implementation. **PLAN NOT FROZEN** is not Gate 1 approval. No seat merges. Spec-PR body preparation carries the checkbox; actual PR publication is Conductor-owned — this draft does not claim the PR was posted.

## What the User Will See *(mandatory)*

Nothing visible: DEV-458 is API/host/security hardening with UI Tag none; no SPA, copy, label, ordering, or UI behaviour change is authorized (brief Q4 point 5; Q5; ASSUMPTIONS.md). Operators and API clients observe stricter play refusals (403/404/422) and LocalPlay-enabled hosts that refuse to start on non-loopback binds; Compose keeps LocalPlay off.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Proven-loopback startup refusal (Priority: P1)

With LocalPlay enabled, the API host's dedicated `IHostedService.StartAsync` validator (registered in `Api/Program.cs`) observes both framework configuration inputs and `IServerAddressesFeature` (including `PreferHostingUrls`) through the named available surfaces I0/I2/I3/I4/I6, reconciles them under the checked-in precedence pointer above, and fails startup before any request is served unless every effective listener is proven loopback — **subject to the open Q9 owner checkbox**, because I1 is UNAVAILABLE and no LocalPlay=true configuration can yet be certified against Kestrel-section endpoints. LocalPlay disabled imposes no new bind restriction. Real-Kestrel test hosts explicitly present provable loopback listeners. No magic-string/new-options/reflection/code-endpoint workaround; no silent fail-all; no code-defined Kestrel endpoint may be introduced in this delivery. I7 timing/parity remains Phase B T005 proof.

**Why this priority**: Ticket requires refuse-startup with LocalPlay on non-loopback binds (recon-DEV-458:9; Q2; Q7).

**Independent Test**: Actual real-Kestrel effective-listener startup tests covering Kestrel-over-URL precedence, PreferHostingUrls both ways, HTTP_PORTS/HTTPS_PORTS and URL override, net10.0 empty-config default, absent/unpopulated feature vs proven merged inputs vs true empty default vs unprovable, mapped and mixed forms, LocalPlay on/off, hostname rejection (runtime matrix where feasible; otherwise deterministic grammar-level rejection coverage justified by Q2 DNS-independence — no new DNS seam), registration ahead of externally active hosted services, concurrency and pre-bind/no-request refusal observables, plus observed shipped Aspire injected-listener environment. Configuration-parser-only or TestServer-only tests are insufficient (brief Test strategy; Q7; Q8; `recon-DEV-458-2` G1/G3).

**Acceptance Scenarios**:

1. **Given** `Features:LocalPlay=true` and every effective listener is proven loopback (including normalized mapped-loopback), **When** the host starts, **Then** startup succeeds and requests can be served.
2. **Given** `Features:LocalPlay=true` and any effective listener is a wildcard (`0.0.0.0`, `::`, `*`, `+`), non-loopback literal, non-localhost hostname (even if DNS would resolve locally), mixed safe/unsafe set, unknown/unprovable form, Unix socket, or named pipe, **When** the host starts, **Then** startup fails and exits having served no requests.
3. **Given** `Features:LocalPlay=false`, **When** the host binds non-loopback listeners, **Then** this ticket adds no bind refusal.
4. **Given** overlapping URL, Kestrel, environment, ports shorthand, PreferHostingUrls, and Aspire overrides, **When** validation runs at `StartAsync`, **Then** it reconciles both configuration and address-feature observations under the checked-in precedence pointer and never silently picks the safe-looking surface when inputs disagree; absent/unpopulated address feature alone is not vacuous proof of no inputs.
5. **Given** LocalPlay=true and a non-loopback/unprovable listener with the shipped registration order, **When** the host starts, **Then** the validator throws before server bind, before later externally active hosted services start, and with no concurrent service serving first.

---

### User Story 2 - Play request and proxy trust boundary (Priority: P1)

`POST /api/movies/{id}/play` accepts launch only when the raw transport peer is loopback and none of the five provenance headers is present. Failed peer/header checks return existing 403 ProblemDetails with fixed title `Forbidden` and absent detail (no false LocalPlay-disabled claim, no peer address, no provenance header name, no exception message/type) before catalog, disk, process, or history — preserving the existing mapping; no custom detail and no handler/Core edit (Q9). Shipped paths use Q8-pinned mechanisms: nginx denies play with proxy 403 before API forwarding; Aspire retains disabled web endpoint-proxy support and permits no endpoint proxy to forward play; Compose LocalPlay-disabled default is retained but is not the relied-on exclusion. Actual runtime smoke proves negatives; a **separate named** LocalPlay-enabled loopback positive-control host (same build/player/file fixtures) proves the launcher works without implying excluded topologies can launch. A stripping loopback proxy or unlisted-header proxy exposing play with LocalPlay enabled remains unsupported.

**Why this priority**: Ticket requires verifying a proxy cannot expose launch remotely (recon-DEV-458:9; Q1; Q8).

**Independent Test**: Peer × five-header matrix (IPv4/IPv6/mapped loopback success; null/non-loopback peers; each header with case/empty/multiple variants) asserting no catalog/disk/process/history **and** Q9 body pins (title exactly `Forbidden`, detail absent, no exception message/type, no peer address, no provenance header name); plus runtime smoke on shipped nginx (proxy 403/zero starts) and Aspire paths (web-proxy no-forward/zero starts; header-bearing or non-loopback direct-API → API 403/zero starts) with the named positive-control host (204/exactly one start) (Q1; Q4; Q6; Q8; Q9).

**Acceptance Scenarios**:

1. **Given** a loopback peer and no provenance headers, with LocalPlay enabled and a valid playable movie, **When** play is posted, **Then** the existing success path may proceed (subject to US3 validations) and history may record only after successful Launch.
2. **Given** a null or non-loopback peer, **When** play is posted, **Then** response is 403 ProblemDetails with title exactly `Forbidden`, detail absent, and no disabled-feature claim, peer address, provenance header name, exception message, or exception type name, and no catalog lookup, disk probe, process start, or history write.
3. **Given** a loopback peer and any of `Forwarded`, `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto`, `X-Real-IP` present (any case, empty, or multiple values), **When** play is posted, **Then** response is 403 with the same zero side effects and the same exact-title / detail-absence / no-leak body constraints.
4. **Given** the shipped nginx path, **When** play is posted through nginx, **Then** the proxy returns 403 and zero process starts; **Given** the Aspire web-proxy attempt, **Then** no forwarded launch and zero starts; **Given** a header-bearing or non-loopback direct-API attempt, **Then** API 403 and zero starts; **Given** the separate named LocalPlay-enabled loopback positive-control host with the same build/player/file fixtures, **When** direct loopback/no-provenance play is posted, **Then** response is 204 with exactly one observed process start — without proving excluded topologies can launch.

---

### User Story 3 - Existence-first launch and unmapped-format 422 (Priority: P1)

After DEV-438 selection, the Infrastructure launcher checks the selected file via existing `IFileSystem`, then resolves the configured player, then starts the process. Missing file → parameterless `NotFoundException()` → 404 title `Not Found` with absent detail; unmapped format → 422 with safe format-level detail; existence wins when both apply; neither starts a process or writes history. Mapping stays case/dot-insensitive; one ArgumentList file argument and UseShellExecute behaviour are preserved. Genuine start failures remain generic 500 without history.

**Why this priority**: Ticket requires file-exists-before-launch and 422 when no player supports the format (recon-DEV-458:9; Q3).

**Independent Test**: Launcher unit tests for success, missing file, unmapped format, both-failures (404 wins), case/dot-insensitive mapping, hostile filename as one argument, and process-start failure; API tests replacing the old unmapped-format 500 assertion with safe 422; preserve folder-missing 404, no-video 409, validation 422, success 204, disabled 403, and history regressions (brief Test strategy; Q6).

**Acceptance Scenarios**:

1. **Given** a selected file that exists and a mapped player, **When** Launch runs, **Then** the process starts with exactly one file argument (preserved UseShellExecute; no argument separator added) and history may record after success.
2. **Given** a selected file that does not exist, **When** Launch runs, **Then** existing parameterless `NotFoundException()` → 404 with title exactly `Not Found`, detail absent, and body free of exception message/type, selected path/basename/LibraryPath/player paths, peer address, and provenance header name; zero process starts, zero history.
3. **Given** an existing selected file whose format has no configured player, **When** Launch runs, **Then** `ValidationException` → 422 with safe format-level detail only (never file or player paths), zero process starts, zero history.
4. **Given** a missing file and an unmapped format would both apply, **When** Launch runs, **Then** existence wins (404), player resolution is not required to run for the outcome, and there is still zero process start and zero history.
5. **Given** LocalPlay disabled, **When** play is posted for a known movie, **Then** catalog runs then EnsureEnabled → 403 before scanner/disk, process, or history (Q6).
6. **Given** a file deleted after the existence check or a genuine process-start failure, **When** Launch fails, **Then** response is generic 500, no retry, no history, no special taxonomy.

---

### User Story 4 - Bounded verification package (Priority: P2)

Focused unit, API, startup, composition, and runtime-smoke evidence covered by Q4/Q6/Q7 land with the implementation; ADR/spec docs already authored in Phase A; no GlobalExceptionHandler, Core, selector/handler, docker-compose, ServiceDefaults, or web edits. Generated contract refresh only if a gate requires it, explained in the PR. No-code-Listen invariant is enforced by source-search receipt at the reviewed head plus review.

**Why this priority**: Completes the ticket's verification bar after the three behavioural stories; does not invent product behaviour beyond Q1–Q3/Q7.

**Independent Test**: Harness gates at reviewed head (Roslyn, complexity, InspectCode, full tests, property, package security, format, mutation, architecture, contract drift) with honest PASS/SKIPPED/N/A/Could not run; Size M three-axis review (Sentry, Ledger, Compass); nginx/Aspire smoke tasks executed for real; source-search receipt for forbidden Listen-family symbols (brief Test strategy; Q5; Q7).

**Acceptance Scenarios**:

1. **Given** the authorized file set after implementation, **When** applicable harness gates run, **Then** each gate is reported distinctly and no threshold is lowered.
2. **Given** shipped nginx and Aspire topologies under LocalPlay rules, **When** negative-launch smoke runs with a positive local control, **Then** evidence shows no proxy-triggered launch.
3. **Given** no user-visible string beyond existing ProblemDetails titles, **When** delivery completes, **Then** ASSUMPTIONS.md remains empty of taste decisions and no glossary addition is required.
4. **Given** the reviewed head, **When** the no-code-Listen source-search receipt runs over `src/`, **Then** no forbidden Listen-family/`UseSystemd`/endpoint-defining `ConfigureKestrel` hits are present (or work returns to Patron before proceeding).

### Edge Cases

- IPv4-mapped IPv6 loopback peers and listeners (`::ffff:127.0.0.1`) must normalize before loopback checks (Q4).
- Provenance header presence with empty value or multiples still fails closed (Q1).
- Mixed loopback/non-loopback effective listener sets fail startup under LocalPlay (Q2).
- Hostname that currently resolves to loopback is still rejected (Q2).
- Check-then-start race: post-check deletion / missing executable / start failure → generic 500, no history (Q3).
- Stripping loopback proxy forwarding play with LocalPlay enabled is unsupported; absolute any-proxy claim is out of scope (Q1).
- Present but irreconcilable configuration vs address-feature observations fail closed under LocalPlay (Q7).
- I1 Kestrel-section Url/absence reader is UNAVAILABLE under current VII; owner checkbox open; no silent fail-all, magic-string, new options record, reflection, or code endpoint (Q9). Required code-defined Kestrel endpoint or contradictory startup evidence → return to Patron/recon; I7 timing/parity remains Phase B T005 (Q7; Q9).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: With `Features:LocalPlay=true`, the dedicated API `IHostedService.StartAsync` registered in `Api/Program.cs` MUST fail startup and exit without serving requests unless every effective listener (after reconciling framework configuration inputs and `IServerAddressesFeature` under the checked-in precedence pointer, including Aspire overrides, and distinguishing absent/unpopulated feature vs proven merged inputs vs true empty-config default vs unprovable) is proven loopback (`localhost`, IPv4 127.0.0.0/8, IPv6 `::1`, or Q4-normalized mapped-loopback) (Q2; Q4; Q7; Q8; `recon-DEV-458-2`).
- **FR-002**: With `Features:LocalPlay=true`, the host MUST reject wildcard listeners, non-loopback literals, non-localhost hostnames (DNS-independent), mixed sets, unknown/unprovable forms, Unix sockets, and named pipes at startup (Q2).
- **FR-003**: With `Features:LocalPlay=false`, this feature MUST NOT impose a new bind restriction (Q2).
- **FR-004**: `POST /api/movies/{id}/play` MUST allow the request boundary only when the raw transport peer is loopback after mapped-IPv6 normalization AND none of exactly `Forwarded`, `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto`, `X-Real-IP` is present (presence fails closed for any case/empty/multiple; no wider header list) (Q1; Q4; Q8).
- **FR-005**: Failed request-boundary checks MUST return existing FeatureDisabledException → existing 403 ProblemDetails before catalog lookup, filesystem work, process start, or playback-history recording, with title exactly `Forbidden`, detail absent, and body free of disabled-feature claim, peer address, provenance header name, exception message, and exception type name; no custom detail text and no GlobalExceptionHandler/Core edit (Q1; Q6; Q8 forbidden-content; Q9).
- **FR-006**: The play route MUST NOT enable forwarded-header processing or rewrite the raw transport peer; route, DTOs, and declared statuses remain unchanged (Q1).
- **FR-007**: Shipped nginx MUST deny `POST /api/movies/{id}/play` with proxy 403 before API forwarding; Aspire MUST retain disabled web endpoint-proxy support and MUST NOT forward play through an endpoint proxy; Compose LocalPlay-disabled default is retained but MUST NOT substitute for the pinned exclusions. Runtime smoke MUST prove negatives (nginx proxy 403/zero starts; Aspire web-proxy no-forward/zero starts; header-bearing or non-loopback direct-API → API 403/zero starts) and MUST include a separate named LocalPlay-enabled loopback positive-control host using the same build/player/file fixtures (204/exactly one start) that MUST NOT be presented as proof excluded topologies can launch (Q1; Q4; Q8).
- **FR-008**: After DEV-438 primary-video selection, the existing Infrastructure launcher MUST check selected-file existence via existing `IFileSystem` before player resolution and process start (Q3).
- **FR-009**: Missing selected file MUST throw existing parameterless `NotFoundException()` → existing 404 ProblemDetails with title exactly `Not Found`, detail absent, and body free of exception message/type, selected path/basename/LibraryPath/player paths, peer address, and provenance header name; zero process starts and zero history; no custom message/constructor and no GlobalExceptionHandler/Core edit (Q3; Q6; Q8 forbidden-content; Q9).
- **FR-010**: Unmapped format MUST throw existing `ValidationException` → existing 422 ProblemDetails with safe format-level detail only (never file or player paths), with zero process starts and zero history (Q3; Q6).
- **FR-011**: When both missing file and unmapped format would apply, existence MUST win (404) (Q3).
- **FR-012**: Successful launch MUST preserve existing `UseShellExecute` behaviour, exactly one `ArgumentList` file argument (no universal `--` separator), case/dot-insensitive player mapping, and DEV-438 handler order (catalog → EnsureEnabled → resolve → Launch → history); MUST NOT add throttle/single-flight or containment assertions in this ticket (Q3; Q8).
- **FR-013**: Genuine process-start failures and check/start races MUST remain generic 500 ProblemDetails with no retry, no history, and no special taxonomy (Q3).
- **FR-014**: LocalPlay-disabled play MUST preserve catalog then EnsureEnabled → 403 before scanner/disk/process/history; wire body remains title `Forbidden` with absent detail (handler ignores exception message); Q6 order/side effects preserved; peer/header and disabled cases share the same title/detail-absence pins and are asserted in approved test zones (Q6; Q8; Q9).
- **FR-015**: Implementation MUST stay inside the brief Q4 Planned file responsibilities zones as refined by Q7/Q8/Q9 (API hosting composition only; no ServiceDefaults change; AppHost edit only under Q8 smoke gate); MUST NOT add dependency, project, top-level folder, abstraction layer, schema/migration, secret, UI, glossary, GlobalExceptionHandler/Core exception edits, ad hoc config-key reads, new public config key, new options record, reflection, code endpoints, or silent fail-all validator; validator implementation remains blocked until the Q9 owner checkbox is answered (Q4; Q7; Q8; Q9).
- **FR-016**: Generated OpenAPI/TypeScript MAY refresh only through the established generation chain if a gate requires it; no hand edits; unexpected contract diffs MUST be explained (Q4).
- **FR-017**: Tests MUST cover the brief Test strategy matrices plus Q7/Q8/Q9 obligations (pure parser unit including hostname rejection; launcher unit with parameterless NotFound and exact 404 title/detail-absence/no-leak asserts; API peer/five-header/side-effect with exact 403 title/detail-absence/no-leak asserts; real-Kestrel startup precedence/PreferHostingUrls/ports/net10.0 empty-default/mapped-mixed/on-off/registration-concurrency/pre-bind observables and I7 timing/parity proof; observed Aspire injected listener; nginx/Aspire runtime smoke with named positive control; and no-code-Listen source-search receipt) (Q4; Q6; Q7; Q8; Q9).

### Key Entities

- **Effective listener**: A concrete bind the API host actually listens on after all overrides; must be proven loopback when LocalPlay is enabled.
- **Raw transport peer**: `HttpContext.Connection.RemoteIpAddress` for the play request; not rewritten by forwarded headers for this route.
- **Selected primary video**: Path + format from DEV-438 resolution; subject to existence and player-mapping checks inside Launch.
- **Configured player mapping**: Playback options Formats → executable; unmapped → 422.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With LocalPlay enabled, a host whose effective listeners include any non-proven-loopback form never serves a request (startup fails closed via the selected API `IHostedService.StartAsync`).
- **SC-002**: Every non-loopback peer and every of the five provenance-header presence cases on play returns 403 with title exactly `Forbidden`, detail absent, zero catalog/disk/process/history side effects, and no disabled-claim / peer / header-name / exception-message / exception-type leak in automated tests.
- **SC-003**: Missing selected file returns 404 with title exactly `Not Found` and detail absent (no path/basename/player/message/type leak) and unmapped format returns safe 422; both show zero process starts and zero history; when both apply, 404 wins.
- **SC-004**: Shipped nginx and Aspire negative-launch smokes show zero proxy-triggered process starts under Q8 pinned expectations; the separate named positive-control host shows 204 with exactly one start without proving excluded topologies can launch.
- **SC-005**: Applicable harness gates at the reviewed head report honestly without threshold reductions; Size M three-axis review completes under the brief closing bar; no-code-Listen source-search receipt is attached at that head.

## Assumptions

- No taste assumptions. `ASSUMPTIONS.md` is an empty-of-decisions audit ledger (Patron Q6).
- Existing GlobalExceptionHandler mappings already express approved 403/404/422/500 outcomes with fixed titles `Forbidden`/`Not Found` and no detail; handler ignores exception messages (Q3; Q4; Q9; `recon-DEV-458-2-3` §9).
- System.IO.Abstractions / `IFileSystem` is already referenced and registered; launcher reuses it (Q3; `recon-DEV-458-2:50-59`).
- Compose/docker profile already ships `Features:LocalPlay: false` (recon-DEV-458:33); LocalPlay-enabled verification uses explicitly configured test/dev hosts with proven loopback.
- Hosting mechanism is selected by Q7 (dedicated API `IHostedService.StartAsync` in `Program.cs`; no ServiceDefaults change). Q8 pins nginx/Aspire mechanisms, five-header set, and named positive control remain. Q9 resolves payload feasibility to existing generic bodies and opens the structural owner checkbox for I1. Constitution Check VII stays OPEN awaiting owner; **PLAN NOT FROZEN**; Gate 1 closed. Phase B actual-startup/shipped-path/I7 proof remains mandatory and is not assumed PASS.
