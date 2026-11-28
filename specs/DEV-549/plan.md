# Implementation Plan: Shared service fakes and test egress guard

**Branch**: `feature/549-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-549-spec`) | **Date**: 2026-11-28 | **Spec**: `specs/DEV-549/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q11 and Step 6); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-549`, `recon-DEV-549`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `checklists/fakes-and-guard.md`, `contracts/shared-fixture-mapping.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` — grill Q1–Q11 and Step 6 freeze inventable unknowns; no schema change; fixture contract under `contracts/`.

## Summary

Deliver one scrubbed WireMock JSON fixture tree under `tests/fakes/` shared by WireMock and `stub.mjs` (full OMDb consolidation; TMDb/OpenSubtitles success bodies; canned Ollama embed move; credential-only query exclusions; existing gitleaks scrub gate); run e2e with TMDb and OpenSubtitles on against those fakes; add secret-free Compose `fakes` profile + AppHost Development switch fail-closed launches with Node fixture preflight and readiness; enforce a test-only loopback egress guard with bounded Q9 redirect follower, origin-defined Authorization rules, and fresh disposable `Live` search clients; align named Ollama surfaces to `0.40.13` while keeping real `OllamaIntegration`; document locations, recording/scrubbing, and launch commands; collect Done-when receipts under the size-L closing bar in `brief.md` Q8.

## Technical Context

**Language/Version**: C# / .NET 10 test and AppHost projects + Node e2e stub (`web/e2e/stub.mjs`) per `specs/PRODUCT.md` §1 and existing e2e tooling.

**Primary Dependencies**: Existing WireMock.Net (`LoopbackWireMock`), xUnit v3, `WebApplicationFactory` / integration test host, Node e2e stub, Compose, Aspire AppHost core `AddContainer`, existing gitleaks workflow. No new NuGet/npm dependency (`brief.md` Charter §2.3 dispositions; Q1–Q3, Q5, Q10; Step 6).

**Storage**: Checked-in fixture files under `tests/fakes/`; checked-in secret-free `docker/api.settings.fakes.json` mounted only under Compose `fakes` and read by fixture preflight. No database or migration change. `tests/fakes` remains data-only under existing test/project/gate discovery.

**Testing**: Existing `tests/LamuFlix.IntegrationTests` (guard, Live, adapter wiring, OllamaFixture pin) with named files `TestEgressGuardHandler.cs`, `GuardedHttpClientFactory.cs`, `TestEgressGuardTests.cs`, `LiveProviderSearchTests.cs`; e2e against Compose fakes; real opt-in `OllamaIntegration`. Never mock the data-access driver for query-translation behaviour (N/A for HTTP fixture/guard paths). Recording transport used for zero-send guard proofs and synchronized concurrent Live-allowance confinement proof. Evaluation.IntegrationTests and Tests.Common are outside implementation scope; no new architecture analyzer.

**Target Platform**: Local integration test host; e2e Compose; documented Compose fake-profile and AppHost Development launches. Production Compose defaults and `deploy/k8s` unchanged.

**Project Type**: Test/development infrastructure over existing ports-and-adapters API + SPA e2e harness; no public API shape change.

**Performance Goals**: No invented latency SLA; fixtures serve local/loopback traffic only.

**Constraints**: Charter §2.3 care-list items deliberately ruled (Q1–Q11; Step 6). No CI workflow edit (Q5). No LocalPlay/secrets hardcoding/`Process.Start`. Guard code test-project only. Honest gate labels; never lower thresholds; never merge. Size L requires Risk (Sentry), Standards (Ledger), Spec (Compass). Closing bar, review caps, and grill cap: see `brief.md` Q8 (single source; not restated here).

**Scale/Scope**: Size L; shared fixtures + e2e wiring + dual fake launches + guard/Live + Ollama pin + README. Parent DEV-491.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox (`PRODUCT.md` §5): ticket-named landing set + Patron Q1–Q11 and Step 6; no ticket contradiction. Ordinary owner Gate 1 remains the Spec Kit PR merge path.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests | **PASS** — no Core/port change; fixture/guard/AppHost wiring stay in test, e2e, Compose, and AppHost surfaces named by the ticket. ArchitectureTests remain required green with other tests. |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A** — no application use-case change. (N/A is distinct from PASS.) |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no new public error taxonomy; fake-mode and loader failures are setup/startup/test failures with named hosts. |
| VI (Observability) | Safe diagnostics | **PASS** — never log credential values; Live skip reasons name variables only; fake-mode log proof quotes non-secret host lines for the PR. |
| VII (Configuration / LocalPlay / Process.Start) | Options; secrets isolation; LocalPlay gated | **PASS** — Compose fake mode mounts checked-in `docker/api.settings.fakes.json` at `/app/appsettings.Production.json` only under `profiles: [fakes]` (provider URLs and key placeholders through that file, not Compose env; never edit/reuse git-ignored live settings). Fixture preflight in existing `stub.mjs` validates the same JSON (exact fixture authority/path, nonempty required mappings) before readiness; fake API depends on readiness; no `src/` startup validation. **Compose preflight only** owns the three negatives — missing bind, empty fixture set, and invalid/missing provider endpoint — naming the provider/host. AppHost injects fake URLs + obvious placeholders only from the fixture resource, wires every configured provider to a fake endpoint, and **independently** fails startup naming the affected host when the fixture resource or endpoint wiring cannot be resolved (Q10; T014/T015); no hard-coded live key/URL fallbacks. Fail-closed fake-mode setup/wiring preserves typed-options binding and constitution VII request-time subtitle acquisition settings behaviour (this ticket does not alter the subtitle ValidateOnStart exception). Live keys process-env only. No LocalPlay or `Process.Start`. Fixture host uses core `AddContainer`, not `AddExecutable` / NodeJs hosting packages (Q10). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — spec-local vocabulary only; no CONTEXT.md expansion claimed. |
| IX (Test Pyramid) | Meaningful layers; WireMock; WebApplicationFactory | **PASS** — WireMock.Net retained for adapters; shared fixtures are data; guard/Live proofs live under IntegrationTests; real `OllamaIntegration` retained; e2e proves assembled fake wiring. |

### Stack / workflow checks

| Check | Plan response |
|---|---|
| Tech stack / API docs | **PASS** — no public route/DTO/OpenAPI change; no generate:api work. |
| Static-analysis gates | Every changed `.cs` task completes only after Roslyn, cyclomatic complexity, and InspectCode pass on those files; ArchitectureTests green; full delivery matrix per `brief.md` Q8. Keep `SKIPPED`, `N/A`, and `Could not run` distinct from PASS. |
| Web / e2e gates | E2E green with TMDb + OpenSubtitles on against fakes; applicable same-head web checks alongside e2e; web unit MSW untouched. |
| Mutation | Planned production C# is AppHost-only. `harness.yml` `gates.mutation.exclusions` explicitly lists `LamuFlix.AppHost`, so the expected disposition is **N/A** (never PASS). T028 verifies the actual harness exit on the delivery head. Keep SKIPPED / N/A / Could not run distinct from PASS. |

### Complexity Tracking

No constitution violations requiring justification. Thin readers inside existing hosts; one guard with two entry points; no new project/package/layer.

## Approach

- **Spec Kit drafting → Gate 1**: Spec Kit artifacts drafted; analyze / plan challenge applied; Spec PR opened under ordinary Gate 1; Build starts after Gate 1 merge receipt and pickup clearance. Ordinary Gate 1 and frozen scope boundaries are preserved; no structural owner checkbox.
- **Pickup**: Fresh **read-only** recon against pickup HEAD for authorized paths in Where it lands / brief Files list (current OMDb embedding sites in WireMock tests and `stub.mjs`, TMDb/OpenSubtitles success bodies to move, `compose.e2e.yml` provider flags, AppHost `Program.cs`, Compose services, Ollama pin sites). Inventory **all actual** named/typed `IHttpClientFactory` registrations and direct adapter client constructions in IntegrationTests; confirm `tests/fakes` stays data-only under existing test/project/gate discovery. Existing test-host files resolved at pickup where required. Guard/Live file names are fixed below. Stop `needs recon:` if a required path/fact is missing. Stop for cited Patron ruling before any outside-envelope or §2.3 protected edit. Before ADR publication/merge, Wisp rechecks ADR 0051 number availability against current main and open ADR work; ADR stays `proposed` until Build.
- **Task order (`brief.md` Task ordering / Q11)**:
  1. Define shared mapping subset + thin readers with credential-only exclusions; migrate OMDb into `tests/fakes/omdb/` consumed by both hosts (Done-when 5 / SC-005 same-file proof + alternate-synthetic credential proof); scrub/gitleaks on mappings and detached bodies before staging.
  2. Add TMDb/OpenSubtitles mappings; move canned `/api/embed` to `tests/fakes/ollama/`; **enable and verify** both providers in e2e (including unsupported-matcher negative proof) **before** Compose/AppHost fake rollout.
  3. Compose `fakes` profile (distinct container names + `docker/api.settings.fakes.json` mount at `/app/appsettings.Production.json` under the profile only + shared Node fixture host with fakes-only preflight in `stub.mjs` and fake-API readiness dependency; explicit fake-only launch targets; port-sharing and shared infrastructure/state documented) and AppHost switch (`AddContainer` fixture resource, Development default-on, every provider wired, fail-closed naming the host when fixture resource or endpoint wiring cannot resolve). Prove **Compose preflight only** stops launch on missing bind, empty fixture set, and invalid/missing provider endpoint; prove AppHost independently fails when fixture resource or endpoint wiring cannot resolve (Q10; T014/T015); compare normalized default-profile Compose config to pre-change baseline; verify provider destinations against named fixture endpoint; prove normal Compose and AppHost non-fake launches retain baseline wiring; capture positive browse/search/download flows and PR log evidence.
  4. Guard lane independently runnable after pickup (factory named/typed + direct-client helper reconciled to T001 inventory; bounded Q9 redirect follower with method matrix, origin definition, Authorization retention/removal including same-host different effective port and permitted HTTP-to-HTTPS changed-origin cases, HTTPS-downgrade refusal, named hop limit, parameterized proof table). Add `Live` fresh disposable clients + three search smokes only after guard coverage; key-present requires bounded pass; `needs-owner-run` only when key unavailable; synchronized concurrent negative recording proof.
  5. Align Ollama pin on `.env.example`, README pin text, and `OllamaFixture.cs`; run five-surface pin check **after** README pin alignment; keep real `OllamaIntegration` opt-in as an independent run on `0.40.13`.
  6. Finish README against verified setup; collect Done-when + gate receipts on the delivery head after any refactor edits (web checks + e2e; hosted gitleaks receipt; honest N/A/SKIPPED/Could not run including AppHost mutation N/A). Live-with-keys may be `needs-owner-run` once when keys are unavailable.
- **Named identifiers** (within frozen Wanted/Q patterns; see `DRAFTING_RECEIPT.md`): Live env vars `LAMUFLIX_TEST_OMDB_API_KEY`, `LAMUFLIX_TEST_TMDB_API_KEY`, `LAMUFLIX_TEST_OPENSUBTITLES_API_KEY`; AppHost switch `UseServiceFakes`; Compose fake app service id `api-fakes` (and companion services as required by existing Compose shape); fixture container resource name `service-fakes`; redirect hop-limit constant `MaxRedirectHops = 5`; fake placeholder credential literal `fake-key`; IntegrationTests files `TestEgressGuardHandler.cs`, `GuardedHttpClientFactory.cs`, `TestEgressGuardTests.cs`, `LiveProviderSearchTests.cs`.
- **Do not**: edit CI workflow; edit k8s pin definitions or Compose Ollama pin lines; put guard in `src/`; add NuGet/npm packages or secret-scan tooling; expand Evaluation.IntegrationTests / Tests.Common; add a new architecture analyzer; use `Aspire.Hosting.NodeJs` / JS hosting package / `AddExecutable` for the fixture host; fall back to live URLs; write secrets into fixtures/README/logs/output/docs; replace MSW or `OllamaIntegration`; stub DEV-554–DEV-563 ahead; lower gate thresholds; claim Gate 1 / plan freeze / delivery PASS from this draft.

## Verification Approach

- **Shared fixtures (Q7 / Step 6)**: Both hosts load the same checked-in `tests/fakes` files; OMDb same-file / zero-copy proof is required evidence (SC-005); credential-only exclusions per contract applied by both readers; alternate synthetic credential matches in both hosts; Node and WireMock unsupported-matcher negative proofs are required evidence (FR-003); TMDb/OpenSubtitles success mappings cover browse, subtitle search, and one fake download; redacted gitleaks + scrub review before staging; hosted gitleaks receipt on delivery head.
- **E2E (before fake-mode rollout)**: TMDb + OpenSubtitles enabled; green against fakes with browse / subtitle search / one download exercised; unsupported-matcher failure recorded. Enabled-provider e2e verification precedes Compose/AppHost fake rollout (`brief.md` steps 2–3).
- **Fake mode (Q3/Q10 / Step 6)**: Distinct Compose container names; settings mounted at `/app/appsettings.Production.json` under the profile only; provider URL/key placeholders through that file (not Compose env); fixture preflight in `stub.mjs` validates the same JSON (exact authority/path, nonempty required mappings) before readiness; fake API depends on readiness; no `src/` startup validation; no provider env override or live fallback; explicit fake-only targets with intended infrastructure; shared infrastructure/state and port-sharing constraint documented. Every configured provider wired to a fake endpoint. Positive Compose and AppHost browse/search/download receipts; API log quote/grep for the PR showing no outside host and destinations against named fixture endpoint; **Compose preflight only** fails closed naming the provider/host on missing bind, empty fixture set, and invalid/missing provider endpoint (API held by readiness); AppHost **independently** fails startup naming the affected host when the fixture resource or endpoint wiring cannot be resolved (Q10; T014/T015); normalized default-profile Compose config compared to pre-change baseline; normal Compose and AppHost non-fake baselines verified. Fail-closed setup/wiring preserves constitution VII typed-options and request-time subtitle settings behaviour.
- **Guard (Q4/Q9 / Step 6)**: Recording-transport zero-send on real host (blocked host named); loopback pass; parameterized redirect proof table for relative/absolute `Location`, allowed hops, 301/302/303 POST→GET, 307/308 method preservation, same-origin `Authorization` retention (including explicit default port), allowed changed-origin removal that explicitly covers same-host different effective port and permitted HTTP-to-HTTPS origin change, HTTPS-downgrade refusal with zero forbidden sends, named hop-limit failure, and redirect-to-real zero sends; factory named/typed clients **and** direct-client helper path proofs reconciled to T001 inventory; test-only wiring in the named IntegrationTests files.
- **Live (Q4/Q5/Q8 / Step 6)**: Independent skips without keys; key present → bounded real search pass per provider on a fresh disposable guarded client with immutable provider-host allowance (acceptance); key unavailable → `needs-owner-run` once with keyed evidence open (disposition, distinct from acceptance). No shared/ambient switch. Synchronized concurrent negative recording-transport proof: ordinary client denies Live host while allowance active; Live client denies a different external host; both zero forbidden sends (no credentials/network for that proof). No login/download/quota-spending calls; no key values in tracked files, fixtures, logs, output, or docs; CI workflow untouched and receives no keys.
- **Ollama (Q6)**: Five-surface `0.40.13` tag/digest comparison after README pin alignment; independent real opt-in fixture green locally.
- **Gates / closing bar (`brief.md` Q8)**: Same-head Roslyn; complexity (incl. refactor threshold from `harness.yml`); InspectCode; format; full `dotnet test` incl. ArchitectureTests; AppHost-only mutation expected **N/A** under explicit harness exclusion (verify actual exit); hosted gitleaks receipt; **applicable web checks alongside e2e**. Distinguish N/A / SKIPPED / Could not run from PASS (including Principle II **N/A**). After any refactor edits, require final same-head receipts before Size L review. Carry each required acceptance receipt; every Low dispositioned; charter filing bar. Ticket recon provenance remains Wanted / Done when / Where-it-lands inventory only; operator-facing outcome remains Nothing visible / documented dev fake mode.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-549/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file (mirrored to private IMPL_PLAN)
├── tasks.md
├── contracts/
│   └── shared-fixture-mapping.md
├── DRAFTING_RECEIPT.md
└── checklists/
    ├── requirements.md
    └── fakes-and-guard.md
```

### Source Code (authorized responsibility envelope — brief Files / ticket Where it lands / recon)

```text
tests/fakes/                                  # data-only under existing discovery
├── omdb/                                     # full shared OMDb mappings + bodies
├── tmdb/                                     # canonical TMDb success mappings
├── opensubtitles/                            # canonical OpenSubtitles success mappings
└── ollama/                                   # canned /api/embed only

tests/LamuFlix.IntegrationTests/
├── LoopbackWireMock.cs                       # thin shared-folder load (existing host)
├── OpenSubtitlesSubtitleProviderTests.cs     # canonical success → shared files; direct-client guard helper sites
├── OllamaFixture.cs                          # pin → 0.40.13
├── TestEgressGuardHandler.cs                 # bounded Q9 redirect follower + loopback deny
├── GuardedHttpClientFactory.cs               # shared helper for factory + direct clients
├── TestEgressGuardTests.cs                   # parameterized redirect + inventory pipeline proofs
└── LiveProviderSearchTests.cs                # Live search smokes + concurrent allowance proof

web/e2e/
├── stub.mjs              # shared loader; credential exclusions; fakes-only preflight; keep controls; remove OMDb copies
└── compose.e2e.yml       # TMDb + OpenSubtitles on; mount tests/fakes as needed

docker-compose.yml        # fakes profile + distinct fake app services/container names + fixture host readiness (do not change Ollama pin lines)
docker/api.settings.fakes.json   # new checked-in secret-free fake settings (mount at /app/appsettings.Production.json under fakes only; read by preflight)

src/LamuFlix.AppHost/
└── Program.cs            # UseServiceFakes switch; AddContainer fixture; fake URL/placeholder injection

.env.example              # OLLAMA_IMAGE → 0.40.13 (+ digest matching Compose/home)
README.md                 # pin table + fakes/Live/recording/launch docs
docs/adr/0051-shared-service-fakes-and-test-egress.md   # Keel-owned; proposed until Build; Wisp rechecks number before publication/merge
```

**Structure Decision**: Extend existing test, e2e, Compose, and AppHost surfaces with ticket-named fixture folders and controls. No new project or package. No Evaluation.IntegrationTests / Tests.Common expansion.

## Named identifiers

| Item | Name | Basis |
|---|---|---|
| Live OMDb key | `LAMUFLIX_TEST_OMDB_API_KEY` | Wanted 4 / Q5 pattern `LAMUFLIX_TEST_<PROVIDER>_*` |
| Live TMDb key | `LAMUFLIX_TEST_TMDB_API_KEY` | same |
| Live OpenSubtitles key | `LAMUFLIX_TEST_OPENSUBTITLES_API_KEY` | same; search uses API key only |
| AppHost switch | `UseServiceFakes` | Wanted 3 / Q10 “AppHost switch”; default on in Development only |
| Compose fake API service | `api-fakes` | Q10 distinct fake-profile application services |
| Fixture resource | `service-fakes` | shared Node fixture host across e2e/Compose/AppHost |
| Fake placeholder | `fake-key` | Patron Q3 example |
| Redirect hop limit | `MaxRedirectHops = 5` | Q9 named test constant |
| Settings file | `docker/api.settings.fakes.json` | Patron Q3 example path; mount `/app/appsettings.Production.json` under `fakes` only |
| Guard handler | `TestEgressGuardHandler.cs` | Step 6 named IntegrationTests file |
| Guarded client helper | `GuardedHttpClientFactory.cs` | Step 6 named IntegrationTests file |
| Guard proofs | `TestEgressGuardTests.cs` | Step 6 named IntegrationTests file |
| Live smokes | `LiveProviderSearchTests.cs` | Step 6 named IntegrationTests file |

Existing test-host caller and hosting paths are resolved at pickup recon. Credential-only query exclusions live in `contracts/shared-fixture-mapping.md` (`apikey` for OMDb; `api_key` for TMDb; none for OpenSubtitles/Ollama).
