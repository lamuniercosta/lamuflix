# Feature Specification: Shared service fakes and test egress guard

**Feature Branch**: `feature/549-spec`

**Created**: 2026-11-28

**Status**: gate1: provisional

**Input**: Ticket DEV-549 (parent DEV-491; size L); grill-closed `brief.md` (Q1–Q11 and Step 6 plan-challenge decisions); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-549`, `recon-DEV-549`; `specs/PRODUCT.md`. Decisions come only from brief and its rulings.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope / Charter §2.3 dispositions; `PRODUCT.md` §5). Ordinary owner Gate 1 remains the Spec Kit PR merge path.

## Clarifications

### Session 2026-11-28

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Shared fixture boundary and WireMock JSON subset (Q1–Q2), Compose `fakes` profile and AppHost switch fail-closed wiring (Q3, Q10), test-host loopback guard with redirect follow-through (Q4, Q9), bounded `Live` search-only opt-in with independent key skips and no CI workflow edit (Q5), Ollama `0.40.13` pin alignment on named surfaces (Q6), fixture coverage and dual launch evidence (Q7), closing bar and review caps (Q8), and task order (Q11) are settled in `brief.md` and `CONCLUSIONS.md`. Step 6 plan-challenge refinements (Live disposable clients, origin-defined Authorization strip, credential query exclusions, Compose fixture preflight, named IntegrationTests files, AppHost mutation N/A) are recorded in `brief.md` Step 6 and `CONCLUSIONS.md` Step 6. `ASSUMPTIONS.md` has no `[assumed]` entries. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap that cannot be closed from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

Nothing visible: this ticket delivers shared test/development service fakes, a test-host egress guard, opt-in `Live` searches, Ollama pin alignment on named surfaces, and operator docs. End-user browse, search, playback, and production Compose / Kubernetes behaviour are unchanged. The operator-visible result is documented **dev fake mode** (Compose `fakes` profile and AppHost switch), not a SPA UI change.

## Scope

**In scope** (`brief.md` Frozen scope / Grill answers and plan / Task ordering; Q1–Q11; Step 6; ticket Wanted 1–7 and Done when at `recon-DEV-549`):

1. **Shared scrubbed fixtures**: One folder per service under `tests/fakes/<service>/` holding WireMock JSON mappings (and body files as needed) for OMDb, TMDb, OpenSubtitles, and the existing canned Ollama `/api/embed` response. Existing `LoopbackWireMock` and `web/e2e/stub.mjs` load the same files through thin readers. OMDb is fully consolidated with zero remaining copies in either host. TMDb and OpenSubtitles share canonical success bodies; faults, malformed responses, and control routes stay host-local where they already live. Credential-only query parameters are listed per provider in `contracts/shared-fixture-mapping.md` and excluded from canonical matching by both readers; functional query parameters remain matched; unsupported matchers fail clearly. Scrubbed mappings and detached bodies are reviewed and scanned with existing gitleaks rules before staging; the delivery head carries the hosted gitleaks receipt. Future adapter tickets DEV-554–DEV-563 add their own fixtures to the same place.
2. **E2E with providers on**: Enable TMDb and OpenSubtitles in e2e / `compose.e2e.yml` and run green against the shared fakes. Preserve stub control routes in the Node host. Unsupported mapping matchers fail clearly.
3. **Dev and seat fake mode**: Compose profile `fakes` with distinct fake-profile application services and container names (sharing host ports with live counterparts, so only one mode runs at a time) that mount checked-in secret-free `docker/api.settings.fakes.json` at `/app/appsettings.Production.json` only under that profile — no fake Compose provider URLs or credentials via env. The existing Node fixture service (`stub.mjs`) performs fakes-only startup preflight: it reads the same required read-only fake settings JSON the fake API mounts, validates every configured provider’s exact fixture authority and path plus nonempty valid required mappings, and refuses provider env override or live settings/fallback. The fake API depends on that validated readiness. No startup validation is added under `src/`. AppHost Development-default switch starts the same Node fixture host via core `AddContainer` with the same Node image and the same `stub.mjs` / `tests/fakes` bind mounts as e2e and Compose fake, and injects only fake URLs and non-secret placeholders from that resource’s endpoints. Every configured provider must be wired to a fake endpoint. Compose preflight alone assigns missing bind, empty fixture set, and invalid/missing provider endpoint negatives (API held by readiness). AppHost independently fails startup naming the affected host when the fixture resource or endpoint wiring cannot be resolved (Q10). Never falls back to a live URL. Documented fake Compose targets start only the intended fake app plus its dependencies. Shared Compose infrastructure/state and host ports are documented. Normalized default-profile Compose configuration is compared to the pre-change baseline. Normal `docker compose up`, production Compose defaults, baseline AppHost non-fake wiring, and `deploy/k8s` stay unchanged. Live and fake app variants share host ports and cannot run together.
4. **Test-host egress guard**: One guard in `tests/LamuFlix.IntegrationTests` only (no `src/` hook), in named files `TestEgressGuardHandler.cs`, `GuardedHttpClientFactory.cs`, `TestEgressGuardTests.cs` (Live smokes in `LiveProviderSearchTests.cs`). Applies to every `IHttpClientFactory` client via test-host overrides and to directly constructed adapter clients via a shared helper. Allows only `localhost` and `IPAddress.IsLoopback` literals without DNS. Fail-before-send naming the host. Auto-redirects disabled on guarded clients; the guard follows redirects through the same pipeline with the bounded Q9 follower and method matrix (301/302/303 POST→GET; 307/308 method preserved), resolving relative and absolute `Location` values and validating them before each send, and enforcing a named hop-limit constant that fails when exceeded. Origin is scheme + normalized host + effective port. Same-origin hops (including explicit default port) retain `Authorization`; allowed changed-origin hops remove `Authorization` (explicitly including same-host different effective port and permitted HTTP-to-HTTPS origin change); HTTPS downgrade is refused with zero forbidden sends; redirect-to-real-host produces zero forbidden sends. `Live` exception is a fresh per-smoke disposable guarded client with an immutable provider-host allowance — no shared or ambient switch. Node e2e and MSW stay outside this guard. Evaluation.IntegrationTests and Tests.Common stay outside implementation scope.
5. **`Live` category**: One search-only smoke per OMDb, TMDb, and OpenSubtitles in `LiveProviderSearchTests.cs`. Each smoke owns and disposes a fresh guarded client with an immutable allowance for its provider host. Each skips independently when its `LAMUFLIX_TEST_*` key is absent, naming the variable (never a value) in the skip reason. When the key is present, a bounded search MUST pass (measurable acceptance). `needs-owner-run` is an evidence disposition only when a key is unavailable on the seat and leaves that keyed requirement open — it is not a substitute pass. Synchronized concurrent negative recording proof: an ordinary client denies the Live host while the allowance is active; the Live client denies a different external host; both observe zero forbidden sends; no keys or network required for that proof. No login, download, or quota-spending call. OpenSubtitles search uses only the API key. Keys stay out of tracked files, fixture payloads, test output, logs, and docs (variable names only). CI receives no keys and keeps its existing workflow (no `.github/workflows/ci.yml` edit). `OllamaIntegration` stays separately opt-in.
6. **Ollama pin alignment**: Align `.env.example`, `README.md`, and `OllamaFixture.cs` to `0.40.13` (matching existing Compose and home-overlay digest). Do not edit Compose pin lines, home overlay, or Kubernetes base; README may note that the home overlay supplies the pin to the unpinned base. Keep and re-run the real `OllamaIntegration` opt-in proof on `0.40.13`.
7. **Docs and evidence**: README documents fixture locations, recording/scrubbing with a local test key, `Live` and Ollama opt-in commands, both fake launch commands, shared Compose infrastructure/state and the fake/live host-port constraint, and outbound provider destinations against the named fixture endpoint. Collect Done-when receipts per `brief.md` Test strategy (including `needs-owner-run` for keyed Live if keys are unavailable). AppHost-only production C# mutation is expected N/A under the current `harness.yml` AppHost exclusion and is verified on the delivery head. ADR 0051 stays proposed until Build; number availability is rechecked via Wisp receipt before publication/merge.

**Out of scope**:

- Replacing the real Ollama path in `OllamaIntegration` with a fake.
- Replacing MSW for web unit tests.
- Editing `.github/workflows/ci.yml`, `deploy/k8s/**` pin definitions, or production Compose defaults.
- New NuGet/npm dependency; new project, top-level folder, or architectural layer beyond ticket-named fixture folders and AppHost/Compose controls; database schema; public API route/DTO/status changes; `Features:LocalPlay`; secrets hardcoding; `Process.Start`.
- New architecture analyzer, Evaluation.IntegrationTests / Tests.Common expansion, or new secret-scan tooling.
- Stubbing DEV-554–DEV-563 providers ahead of those tickets.
- Wholesale rewrite of hand-written handler tests beyond moving canonical success bodies and wiring the shared guard helper.
- Anything else is a follow-up issue, not a finding in this round (charter filing bar before filing).

**Precedence**: Ticket Wanted / Done when and the Where-it-lands inventory credited from `recon-DEV-549` are approved authority (`PRODUCT.md` §3). Out of scope is carried by `brief.md` Frozen scope / Out of scope and the cited Patron rulings in `CONCLUSIONS.md` (Q1–Q11 and Step 6) — recon is not credited for Out of scope clauses it does not contain. Brief Q1–Q11, Step 6, and `CONCLUSIONS.md` govern plan boundaries. Concrete C# type/file names for the guard, helper, and Live tests are the named IntegrationTests files in Scope item 4.

**Dependencies**: None blocking. DEV-548 consumes documented fake-mode defaults. DEV-554–DEV-563 add fixtures after this lands.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Shared fixture**
Scrubbed, checked-in WireMock JSON mapping (and optional body file) under `tests/fakes/<service>/`, loaded by both WireMock and `stub.mjs`.
_Avoid_: duplicating the same response body in either host; putting real keys/tokens/account data in fixtures; putting e2e control routes in `tests/fakes`

**Fake mode**
Documented Compose `--profile fakes` launch and AppHost Development switch that point every configured provider base URL at local/Compose-network fakes with non-secret placeholders only.
_Avoid_: falling back to live URLs; mounting or editing git-ignored live `docker/api.settings.json`; reaching fake mode from the home overlay

**Egress guard**
Test-only fail-before-send check that admits loopback destinations only for ordinary integration/adapter tests.
_Avoid_: placing guard code in `src/`; DNS-based loopback checks; a global network kill-switch

**Live category**
Opt-in xUnit category of search-only provider smokes that each own a fresh disposable guarded client with an immutable provider-host allowance and skip without their process-env test key.
_Avoid_: folding `OllamaIntegration` into `Live`; download/login calls; writing keys into tracked files, fixtures, logs, or README values; shared or ambient allowance switches

**Origin (guard redirect)**
Scheme + normalized host + effective port used to decide `Authorization` retention or removal on a followed hop.
_Avoid_: host-only comparisons; retaining credentials on HTTPS downgrade

Consumed vocabulary (already in use; not redefined): OMDb, TMDb, OpenSubtitles, Ollama, WireMock, `LoopbackWireMock`, `stub.mjs`, Compose profile, AppHost, `OllamaIntegration`, MSW, DEV-548 seat runs.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One shared OMDb fixture set for both hosts (Priority: P1) 🎯 MVP

Operators and developers have a single scrubbed OMDb response set under `tests/fakes/omdb/` that both WireMock adapter tests and the e2e Node stub load, with no remaining copied OMDb responses in either host.

**Why this priority**: Done-when requires one OMDb set; later provider fixtures and fake mode reuse the same loader contract.

**Independent Test**: Diff shows zero remaining OMDb response copies in WireMock tests or `stub.mjs`; both hosts serve from `tests/fakes/omdb/`; credential-only query exclusions apply consistently; an unsupported matcher fails clearly; an alternate synthetic credential still matches in both hosts.

**Acceptance Scenarios**:

1. **Given** OMDb mappings previously embedded or copied in WireMock tests and `stub.mjs`, **When** the shared set is landed, **Then** both hosts load the same checked-in files under `tests/fakes/omdb/` and neither host retains a duplicate OMDb response body or mapping copy.
2. **Given** a mapping that uses only the supported subset (method, path or `urlPath`, functional query params, status, headers, `jsonBody` or `bodyFileName`) with credential-only params excluded per contract, **When** either host loads it, **Then** the recorded response is served.
3. **Given** a mapping that uses an unsupported matcher, **When** either reader loads it, **Then** load/startup fails with a clear error rather than silently ignoring the matcher.
4. **Given** an alternate synthetic OMDb credential value (not `fake-key`, not a live key), **When** both hosts match against the shared OMDb fixture, **Then** both serve the fixture without depending on the credential value.

---

### User Story 2 - TMDb and OpenSubtitles fakes power e2e with providers on (Priority: P1)

E2E runs with TMDb and OpenSubtitles enabled against shared fixtures for browse, subtitle search, and one fake subtitle download, while control routes and fault injection stay host-local.

**Why this priority**: Done-when 1 requires green e2e with both providers on against the fakes; adapter tickets will extend the same folders.

**Independent Test**: Green e2e with both providers routed to shared fixtures; canonical success bodies live under `tests/fakes/{tmdb,opensubtitles}/`; faults/controls remain inline in their current hosts; credential exclusions and alternate-synthetic proofs apply where listed.

**Acceptance Scenarios**:

1. **Given** shared TMDb and OpenSubtitles success mappings covering browse, subtitle search, and one subtitle download, **When** e2e runs with both providers enabled in `compose.e2e.yml`, **Then** the suite passes against the fakes.
2. **Given** existing fault, malformed, or control-route behaviour in adapter tests or `stub.mjs`, **When** fixtures are shared, **Then** those behaviours stay host-local and are not required to move into `tests/fakes`.
3. **Given** the canned Ollama `/api/embed` response currently in `stub.mjs`, **When** fixtures land, **Then** it moves to `tests/fakes/ollama/` for e2e/fake mode only and does not replace `OllamaIntegration`.
4. **Given** scrubbed mappings and detached body files under `tests/fakes/`, **When** they are staged, **Then** existing gitleaks rules and a manual scrub review for account data have redacted clean outcomes, and the delivery head later carries the hosted gitleaks receipt.

---

### User Story 3 - Dev and seat fake mode without live egress (Priority: P1)

A developer or seat can launch the app in fake mode via Compose or AppHost, browse, search subtitles, and download one subtitle while API logs show no outside host, with fail-closed wiring and unchanged normal launches.

**Why this priority**: Done-when 2 and Wanted 3 (DEV-548 default) require secret-free local runs against fakes.

**Independent Test**: One Compose fake-profile run and one AppHost fake-switch run each browse/search/download against fakes with loopback-or-Compose-network-only provider logs (quoted or grepped for the delivery PR); Compose preflight alone fails naming the provider/host on missing bind, empty fixture set, or invalid/missing provider endpoint (API held back by readiness); AppHost independently fails startup naming the affected host when the fixture resource or endpoint wiring cannot be resolved (Q10); normalized default-profile Compose config matches the pre-change baseline for services/dependencies/mounts/ports/provider settings; normal Compose and baseline AppHost non-fake wiring are unchanged; live and fake variants cannot share ports simultaneously.

**Acceptance Scenarios**:

1. **Given** the documented Compose fake launch (`--profile fakes` naming only the intended fake-profile application services and their intended infrastructure plus the shared fixture host), **When** the operator browses, searches subtitles, and downloads one, **Then** behaviour succeeds against fakes and captured API logs show no non-loopback / non-Compose-network outside host for provider calls, with provider destinations logged against the named fixture endpoint (log line or grep quoted in the PR).
2. **Given** the documented AppHost Development fake switch on with every configured provider wired to a fake endpoint, **When** the operator browses, searches subtitles, and downloads one, **Then** behaviour succeeds against fakes with the same no-outside-host log evidence requirement.
3. **Given** Compose fake mode with a missing fixture bind, empty required mapping set, or invalid/missing provider endpoint, **When** fixture preflight runs, **Then** readiness fails naming the affected provider/host, the fake API stays held back, and there is no live provider URL fallback. **Given** AppHost fake mode where the fixture resource or a provider endpoint cannot be resolved, **When** startup runs, **Then** AppHost fails independently naming the affected host with no live fallback (Q10; T014/T015).
4. **Given** normal `docker compose up` without the fakes profile and baseline AppHost with the fake switch off, **When** those stacks start, **Then** live-configured / non-fake application wiring is unchanged relative to this ticket’s baseline intent, and normalized default-profile Compose configuration matches the pre-change baseline (services, dependencies, mounts, ports, provider settings).
5. **Given** fake-mode settings, **When** settings are inspected, **Then** provider base URLs and key placeholders are secret-free literals living only in checked-in `docker/api.settings.fakes.json` (mounted at `/app/appsettings.Production.json` only under the `fakes` profile) or AppHost injection — not via Compose env or provider env override — and git-ignored live settings are not edited or reused; shared Compose infrastructure/state and host ports are documented.

---

### User Story 4 - Ordinary tests cannot call real hosts (Priority: P1)

Ordinary integration and provider adapter tests fail before send when a non-loopback host is targeted, including via redirects, while loopback calls still succeed; factory and direct-client paths both enter the same guard.

**Why this priority**: Done-when 3 and Wanted 5 stop accidental live traffic from the test suite.

**Independent Test**: Deliberate external URL fails naming the host with zero sends on a recording transport; loopback succeeds; parameterized redirect proofs cover relative and absolute `Location`, allowed hops, 301/302/303 POST→GET, 307/308 method preservation, same-origin `Authorization` retention (including explicit default port), allowed changed-origin `Authorization` removal that explicitly covers same-host different effective port and permitted HTTP-to-HTTPS origin change, HTTPS-downgrade refusal with zero forbidden sends, hop-limit failure, and redirect-to-real-host zero sends; registered named/typed factory clients **and** the direct-client helper path assert the same guard against the T001 inventory; wiring stays test-project only.

**Acceptance Scenarios**:

1. **Given** an ordinary (non-`Live`) test that targets a non-loopback host, **When** the request would be sent, **Then** the test fails before send with a message that names the host, and a recording inner handler observes zero forbidden sends.
2. **Given** a loopback destination (`localhost` or an `IPAddress.IsLoopback` literal), **When** an ordinary test sends, **Then** the call proceeds.
3. **Given** guarded redirect follow-through bounded to the test-project handler, **When** parameterized proofs run, **Then** relative and absolute `Location` values are resolved and checked before each send; allowed hops within the named limit succeed when destinations remain loopback; 301/302/303 turn POST into GET; 307/308 preserve the method; same-origin hops (scheme + normalized host + effective port, including explicit default port) retain `Authorization`; allowed changed-origin hops remove `Authorization`, with explicit cases for same-host different effective port and permitted HTTP-to-HTTPS origin change; HTTPS downgrade is refused with zero forbidden sends; exceeding the named hop-limit constant fails the test; and a redirect whose next Location is a real host produces zero sends to that host.
4. **Given** the T001 inventory of named and typed `IHttpClientFactory` clients and directly constructed adapter clients in IntegrationTests, **When** coverage inspects handler chains and the shared helper path, **Then** every inventoried factory pipeline and each inventoried direct site enters the same guard implementation with no guard code under `src/` and no Evaluation/Tests.Common expansion.

---

### User Story 5 - Opt-in Live search smokes (Priority: P2)

Developers can run one search-only Live smoke per OMDb, TMDb, and OpenSubtitles locally with process-env test keys; without keys each case skips independently; CI never receives the keys.

**Why this priority**: Done-when 4 proves real-provider search still works without making live calls the default.

**Independent Test**: Without keys, each Live case skips naming its variable; with a key present, that provider’s bounded search-only smoke MUST pass (measurable acceptance). `needs-owner-run` is an evidence disposition only when a key is unavailable on the seat and leaves keyed evidence open — it is not an acceptance alternative and is kept distinct from acceptance throughout. Each smoke owns and disposes a fresh guarded client with an immutable provider-host allowance (no shared/ambient switch). Synchronized concurrent negative recording proof: ordinary client denies the Live host while allowance is active; Live client denies a different external host; both zero forbidden sends; no keys/network required. CI workflow unchanged and keys unset; no key values in tracked files, fixtures, logs, output, or docs.

**Acceptance Scenarios**:

1. **Given** no `LAMUFLIX_TEST_*` key for a provider, **When** that provider’s Live smoke is discovered, **Then** it skips independently and the skip reason names the variable only.
2. **Given** the provider’s test key in process env, **When** that Live smoke runs, **Then** it owns and disposes a fresh guarded client with an immutable allowance for that provider host only, performs a bounded search only (no login, download, or quota-spending call), and **passes**.
3. **Given** an active Live smoke holding its immutable client-local allowance, **When** a synchronized concurrent recording-transport proof runs an ordinary client against the Live host and the Live client against a different external host without keys or network, **Then** both observe zero forbidden sends and neither inherits the other’s allowance.
4. **Given** CI’s existing workflow, **When** tests run, **Then** no `LAMUFLIX_TEST_*` values are mapped into env and Live cases skip; `.github/workflows/ci.yml` is not edited by this ticket.

---

### User Story 6 - One Ollama pin and operator docs (Priority: P2)

Named acceptance surfaces agree on Ollama `0.40.13`, the real opt-in fixture still proves embeddings, and the README tells operators where fakes live, how to record/scrub responses, and how to launch fake mode and Live/Ollama opt-ins.

**Why this priority**: Done-when 6 and Wanted 6–7 close pin drift and make the shared fake workflow operable.

**Independent Test**: `.env.example`, README, and `OllamaFixture.cs` show `0.40.13` consistent with Compose/home overlay; `OllamaIntegration` passes locally on that pin; README covers fixture paths, recording/scrubbing, Live/Ollama commands, both fake launches, shared infrastructure/state, host-port constraint, and fixture-endpoint destinations.

**Acceptance Scenarios**:

1. **Given** the five Done-when surfaces, **When** pins are compared, **Then** they agree on `0.40.13` (tag or corresponding digest) without editing Compose pin lines, home overlay, or Kubernetes base.
2. **Given** `LAMUFLIX_RUN_OLLAMA_TESTS` opt-in, **When** `OllamaIntegration` runs on the updated fixture pin, **Then** the real embedding proof still passes locally.
3. **Given** the updated README, **When** an operator follows it, **Then** they can find fixture folders, record and scrub a response with a local test key (variables named, values never written), launch both fake-mode paths, understand shared Compose infrastructure/state and ports, and run Live / Ollama opt-ins.

### Edge Cases

- Unsupported WireMock matcher in a shared mapping → either reader fails clearly; no silent ignore.
- Missing fixture bind, empty required mappings, or invalid/missing provider endpoint under Compose fake mode → Compose preflight alone fails naming the provider/host; fake API stays held by readiness; no live fallback. AppHost fake mode where the fixture resource or endpoint wiring cannot resolve → AppHost fails startup independently naming the affected host (Q10); no live fallback.
- Redirect hop count exceeds the named test constant → guarded client fails the test.
- Same-origin redirect (scheme + normalized host + effective port, including explicit default port) → `Authorization` retained; allowed changed-origin hop (including same-host different effective port and permitted HTTP-to-HTTPS) → `Authorization` removed; HTTPS downgrade → refused with zero forbidden sends; redirect-to-real host → zero forbidden sends.
- Live test without its key → independent skip; other providers unaffected.
- Live allowance → fresh disposable client-local immutable provider-host allowance only; no shared/ambient switch; synchronized concurrent ordinary-denial and different-host denial proofs (no key/network for that proof).
- Fake and live Compose app services → share host ports; running both together is unsupported and documented; shared infrastructure/state documented.
- Scrubbed fixture still containing a key/token/account remnant → finding; must be scrubbed and cleared by existing gitleaks before check-in.
- Key unavailable on the seat for keyed Live evidence → record `needs-owner-run` once with keyed evidence left open; do not treat it as acceptance or fabricate a pass.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST keep one scrubbed WireMock JSON mapping set per service under `tests/fakes/<service>/` for OMDb, TMDb, OpenSubtitles, and the canned Ollama embed response moved from `stub.mjs`. Mappings and detached body files MUST pass existing gitleaks rules plus a manual scrub review for account data before staging, with redacted outcomes recorded; the delivery head MUST carry the hosted gitleaks receipt. No new secret-scan tooling.
- **FR-002**: Both `LoopbackWireMock` consumers and `web/e2e/stub.mjs` MUST load shared fixtures through thin readers inside those existing hosts; OMDb MUST have zero remaining copies in either host. Both readers MUST apply the same explicit credential-only query parameter exclusions listed in `contracts/shared-fixture-mapping.md` while retaining functional query matching.
- **FR-003**: Both fixture readers MUST support the mapping subset method, path or `urlPath`, query params (with credential-only exclusions per contract), status, headers, and `jsonBody` or `bodyFileName`, MUST fail clearly on unsupported matchers, and MUST prove that an alternate synthetic credential still matches the shared fixture in both hosts for each provider that lists an exclusion. Fake-mode configuration continues to use `fake-key` placeholders.
- **FR-004**: E2E MUST run with TMDb and OpenSubtitles enabled against the shared fakes, covering browse, subtitle search, and one fake subtitle download scenarios used by those flows.
- **FR-005**: Compose MUST provide a `fakes` profile with distinct fake-profile application service and container names that mount checked-in `docker/api.settings.fakes.json` at `/app/appsettings.Production.json` only under that profile (provider base URLs and key placeholders via that file, not Compose env), depend on existing infrastructure plus the shared Node fixture host (same Node image and `stub.mjs` / `tests/fakes` bind mounts as e2e), and are named explicitly as fake-only targets in the documented dev launch command; live and fake app variants share host ports and MUST NOT run together. Shared Compose infrastructure/state and ports MUST be documented. The fake API MUST depend on fixture-service readiness.
- **FR-006**: AppHost MUST expose a Development-default fake switch that starts the fixture host via core `AddContainer` (same Node image and `stub.mjs` / `tests/fakes` mounts as e2e and Compose fake), injects only fake URLs and non-secret placeholders from that resource’s endpoints for every configured provider, and fails startup naming the affected host when wiring cannot be resolved. AppHost proof remains independent of the Compose readiness dependency.
- **FR-007**: Fake mode MUST wire every configured provider to a fake endpoint, never fall back to live provider URLs, and never reuse/edit git-ignored live settings; placeholders MUST be obviously non-secret literals living only in fake-mode config. Compose fail-closed MUST be implemented as fakes-only startup preflight inside the existing Node fixture service (`web/e2e/stub.mjs`): read the same required read-only fake settings JSON mounted into the fake API; validate every configured provider’s exact fixture authority and path and nonempty valid required mappings; refuse provider env override or live settings/fallback; hold the fake API behind readiness. **Compose preflight only** MUST assign the three negatives — missing bind, empty fixture set, and invalid/missing provider endpoint — and stop launch naming the provider/host. AppHost fail-closed remains the independent Q10 proof that startup fails naming the affected host when the fixture resource or endpoint wiring cannot be resolved (FR-006; T014/T015). No startup validation is added under `src/`. Fake-mode setup/wiring fail-closed checks MUST preserve constitution VII typed-options binding and the existing request-time subtitle acquisition settings behaviour (this ticket does not change that subtitle ValidateOnStart exception). Normalized default-profile Compose configuration MUST be compared to the pre-change baseline (services, dependencies, mounts, ports, provider settings), with explicit fake-target closure and provider destinations verified against the named fixture endpoint.
- **FR-008**: `tests/LamuFlix.IntegrationTests` MUST install one egress guard on all named and typed `IHttpClientFactory` clients and on directly constructed adapter clients via a shared helper, with no guard code in `src/`, in files `TestEgressGuardHandler.cs`, `GuardedHttpClientFactory.cs`, and proofs in `TestEgressGuardTests.cs`. Evaluation.IntegrationTests and Tests.Common remain outside implementation scope.
- **FR-009**: The guard MUST allow only `localhost` and `IPAddress.IsLoopback` literals without DNS, fail before send naming the host, disable auto-redirect on guarded clients, and follow redirects through the bounded Q9 test-only follower resolving relative and absolute `Location` values before each send with the method matrix (301/302/303 POST→GET; 307/308 method preserved), enforcing a named hop-limit constant that fails when exceeded. Origin MUST be defined as scheme + normalized host + effective port. Same-origin hops (including explicit default port) MUST retain `Authorization`; allowed changed-origin hops MUST remove `Authorization`, and the changed-origin rule MUST explicitly cover same-host different effective port and permitted HTTP-to-HTTPS origin change; HTTPS downgrade MUST be refused.
- **FR-010**: The suite MUST include deliberate non-loopback failure (blocked host named; zero forbidden sends on a recording transport), loopback success, an explicit parameterized redirect proof table in `TestEgressGuardTests.cs` covering relative/absolute `Location`, allowed hops, 301/302/303 POST→GET, 307/308 method preservation, same-origin `Authorization` retention (including explicit default port), allowed changed-origin `Authorization` removal that explicitly covers same-host different effective port and permitted HTTP-to-HTTPS origin change, HTTPS-downgrade refusal with zero forbidden sends, named hop-limit failure, redirect-to-real-host zero sends, and factory named/typed plus direct-client helper pipeline proofs reconciled to the T001 inventory.
- **FR-011**: The suite MUST include one `Live` search-only smoke each for OMDb, TMDb, and OpenSubtitles in `LiveProviderSearchTests.cs` that skip independently without their process-env `LAMUFLIX_TEST_*` key (variable named in the skip reason), and when the key is present MUST pass a bounded real search with no login, download, or quota-spending call; each smoke MUST own and dispose a fresh guarded client with an immutable provider-host allowance and MUST NOT use a shared or ambient switch; a synchronized concurrent negative recording-transport proof MUST show that an ordinary client denies the Live host while the allowance is active and that the Live client denies a different external host, both with zero forbidden sends, without requiring credentials or network; keys MUST stay out of tracked files, fixtures, logs, output, and docs; CI MUST receive no keys and `.github/workflows/ci.yml` MUST remain unedited by this ticket. `needs-owner-run` is an evidence disposition only when a key is unavailable and is not an acceptance alternative.
- **FR-012**: `.env.example`, `README.md`, and `OllamaFixture.cs` MUST align to Ollama `0.40.13` consistent with existing Compose and home-overlay pins without editing those Kubernetes/Compose pin definitions.
- **FR-013**: README MUST document fixture locations, recording and scrubbing with a test key (variable names only), Live and Ollama opt-in commands, both fake launch commands, shared Compose infrastructure/state and the fake/live host-port constraint, and that outbound provider destinations target the named fixture endpoint.
- **FR-014**: Control routes, fault injection, and malformed-response cases that already live host-locally MUST remain host-local unless they are OMDb copies required to consolidate.

### Key Entities

- **Service fixture folder**: Directory under `tests/fakes/<service>/` containing scrubbed mappings and optional body files for one outside service.
- **Mapping document**: WireMock JSON request/response record using only the supported subset fields, with credential-only query exclusions applied by both readers.
- **Fake-mode settings document**: Checked-in secret-free JSON mounted only under the Compose `fakes` profile (`docker/api.settings.fakes.json`) and read by the fixture-service preflight.
- **Live smoke**: Category-tagged search-only test bound to one provider’s process-env test key and a fresh disposable guarded client with an immutable provider-host allowance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: E2E completes green with TMDb and OpenSubtitles enabled against the shared fakes.
- **SC-002**: One Compose fake-mode run and one AppHost fake-mode run each complete browse, subtitle search, and one subtitle download with captured API logs showing no outside non-loopback / non-Compose-network provider host and provider destinations against the named fixture endpoint (quoted for the PR); Compose preflight alone fails closed naming the provider/host on missing bind, empty fixture set, or invalid/missing provider endpoint (fake API depends on fixture readiness); AppHost independently fails startup naming the affected host when the fixture resource or endpoint wiring cannot be resolved (Q10; T014/T015); normalized default-profile Compose configuration matches the pre-change baseline; normal Compose and AppHost non-fake baselines remain; shared infrastructure/state and ports are documented.
- **SC-003**: A deliberate ordinary-test call to a real host fails naming that host with zero recorded sends; loopback success, parameterized redirect proof set (relative/absolute Location, method matrix, same-origin Authorization retention including explicit default port, changed-origin removal explicitly covering same-host different effective port and permitted HTTP-to-HTTPS origin change, HTTPS-downgrade refusal with zero forbidden sends, hop-limit failure, redirect-to-real zero sends), and factory named/typed plus direct-client helper pipeline proofs reconciled to the T001 inventory also pass.
- **SC-004**: Each Live smoke skips without its key; with a key present each bounded search-only smoke passes on a fresh disposable guarded client with immutable provider-host allowance. Synchronized concurrent recording proof shows ordinary-client denial of the Live host and Live-client denial of a different external host, both with zero forbidden sends, without keys or network. When a key is unavailable on the seat, keyed evidence may be recorded once as `needs-owner-run` — that disposition is distinct from acceptance, leaves Done-when 4 keyed evidence open, and MUST NOT be filed as PASS. CI never sets the keys and the CI workflow file is unchanged.
- **SC-005**: OMDb responses exist as one shared file set only; WireMock and `stub.mjs` retain no OMDb copies; both readers apply the listed credential exclusions; alternate synthetic credentials still match in both hosts; scrubbed mappings/bodies clear existing gitleaks before staging and the delivery head carries the hosted gitleaks receipt.
- **SC-006**: Ollama `0.40.13` is consistent across the five Done-when surfaces, and the real `OllamaIntegration` opt-in still passes locally on that pin.
- **SC-007**: An operator following the README can locate fixtures, record/scrub a response, and launch fake mode without consulting the ticket text.

## Assumptions

- Grill Q1–Q11 and Step 6 fully freeze inventable behaviour for this draft; `ASSUMPTIONS.md` carries no `[assumed]` taste entries.
- Existing WireMock.Net, Node e2e stub, Compose, AppHost tooling, and existing gitleaks workflow remain the hosts; no new package, project, or secret-scan tooling is required (`brief.md` Charter §2.3 dispositions).
- Exact Live env var strings, fake Compose service names, AppHost switch identifier, guard/Live file names, and redirect hop-limit numeric value are recorded in `plan.md` / `DRAFTING_RECEIPT.md` within frozen Wanted/Q patterns (not Patron taste assumptions).
- Line-number drift in ticket/recon citations versus current HEAD does not change the named file set; implementers verify paths at pickup.
- When local test keys are unavailable, keyed Live evidence may remain open once as `needs-owner-run` (`brief.md` Q8); that disposition is never an acceptance substitute for a key-present run.
- Closing bar, review caps, and grill cap are owned by `brief.md` Q8; this draft does not restate numeric caps.
