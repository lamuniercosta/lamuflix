# DEV-549 — Shared service fakes and test egress guard

Phase 2, steps 1–3.1. Size L. Branch `feature/549-spec` at `6bd8be6712c6239065b22609db1599b4f91d3955`. Patron closed the grill at Q11. The decision record is [CONCLUSIONS.md](CONCLUSIONS.md). Authority: DEV-549 *Wanted*, *Done when*, *Out of scope*, *Where it lands*, and *Open points*; `recon-DEV-549`; `specs/PRODUCT.md` §3–5; constitution VII and IX; and the `task-pipeline` standing note.

## Frozen scope

- Share checked-in, scrubbed WireMock JSON response mappings per service across the existing test and development hosts. Consolidate OMDb completely, add TMDb and OpenSubtitles to e2e, and move the existing canned Ollama embedding response.
- Run e2e with TMDb and OpenSubtitles enabled. Provide secret-free fake mode by default for documented development and seat launch paths via Compose and AppHost.
- Enforce loopback-only HTTP egress in ordinary integration and provider adapter tests, and add independently key-gated `Live` searches for OMDb, TMDb and OpenSubtitles.
- Align the named surfaces to Ollama `0.40.13`, retain the real opt-in `OllamaIntegration` proof, and document fake locations, launch commands and response recording with a local test key.
- Preserve MSW for web unit tests, production Compose behavior and the Kubernetes deployment. DEV-554 through DEV-563 add their own provider fixtures. The documented operator-visible result is dev fake mode. Quill records “Nothing visible to end users” in `checklists/requirements.md` because the change is test and development infrastructure.

Anything else is a follow-up issue, not a finding in this round. File a follow-up only under the charter filing bar of Critical, High, or broken behavior. Record the remainder as `noted, no ticket`.

## Grill answers and plan

| Q | Patron's ruling and plan consequence |
|---|---|
| 1 | Use one scrubbed recorded response set per service in `tests/fakes/<service>/`. The existing `LoopbackWireMock` and `web/e2e/stub.mjs` load it through thin readers. The entire OMDb fixture set is shared, with zero copies left in either host. Existing hosts and tooling suffice. |
| 2 | Use WireMock JSON mappings. The Node loader supports the fixture subset: method, path or `urlPath`, query params, status, headers, `jsonBody` or `bodyFileName`. Fail clearly on an unsupported matcher. Keep control routes in the Node host and use existing parsers. |
| 3 | Use the named `fakes` profile and AppHost switch. Fake URLs and obvious fake credential literals live only in fake-mode settings. Mount separate checked-in settings JSON for fake Compose, and use only the fake URLs and placeholders in AppHost. Keep the git-ignored live settings file out of fake mode. In fake mode, missing endpoint wiring fails setup or startup and identifies the affected host. No live fallback. |
| 4 | Put one test-only guard in `LamuFlix.IntegrationTests`. Wire it into every `IHttpClientFactory` client's test host configuration and use the same guard in the helper for directly constructed adapter clients. Admit only `localhost` and literal IPs accepted by `IPAddress.IsLoopback`, without DNS. A denied call fails before send and names the host. `Live` exception scope is the active test and its credentialed provider. Node e2e and MSW use their existing mechanisms. |
| 5 | Add one bounded `Live` search smoke test for each of OMDb, TMDb and OpenSubtitles. Each skips independently when its provider's required `LAMUFLIX_TEST_*` env key is missing. Name only the variable in the skip reason. OpenSubtitles search uses only its API key. Exclude login and download steps. CI receives no keys and keeps its existing workflow, so discovered `Live` cases skip. Keep `OllamaIntegration` separate. Keys stay in process env and out of tracked files, fixture payloads, test output and logs. |
| 6 | Align `.env.example`, `README.md` and `OllamaFixture.cs` to `0.40.13`. Verify the existing Compose tag and home overlay digest against the same pin; keep the pin definitions in those files and the Kubernetes base as they are. The README may explain that the home overlay supplies the pin to the base. Run the real opt-in fixture on `0.40.13`. |
| 7 | Add OMDb, TMDb and OpenSubtitles mappings for provider interactions used by browse, subtitle search and one fake subtitle download. Move all existing OMDb responses into shared files. Move canonical TMDb and OpenSubtitles success bodies; keep faults, malformed responses and controls inline where they already live. Avoid a wholesale handler-test rewrite. Move only the current canned `/api/embed` response to `tests/fakes/ollama/`. Capture e2e and both fake-mode launch receipts independently. |
| 8 | Apply the closing bar below. If local test keys are unavailable, record the keyed run once as `needs-owner-run` and leave its evidence open. Record each Low disposition in `CONCLUSIONS.md` as `fixed` or `noted, no ticket`, subject to the blocking severity and conformance rules below. Apply the charter filing bar to out-of-scope follow-ups. |
| 9 | Disable automatic redirects only in guarded test clients. Resolve each Location, check before each send and follow allowed redirects through the guarded test pipeline. Preserve default redirect behavior, including POST-to-GET for 301/302/303, method preservation for 307/308, and stripping Authorization on a cross-host hop. Apply a named test hop limit and fail when exceeded. |
| 10 | Add distinct fake application services under the `fakes` profile. Explicitly target those services in the dev launch command. They depend on existing infrastructure and the shared fixture host, without starting the live-configured app. Live and fake variants share host ports, so run one at a time. Mount `docker/api.settings.fakes.json` only for the fake app. AppHost uses core `AddContainer`, the same Node image and the same bind-mounted `stub.mjs` and `tests/fakes` as e2e. Default its switch on only in Development. Feed provider endpoints from the fixture resource and fail startup when wiring cannot be resolved. |
| 11 | Deliver in the order below and map each acceptance receipt to a task. The checklist records the operator-facing nature of dev fake mode. The Conductor's no-commit instruction governs this handoff. |

### Files for the implementation plan

- `tests/fakes/{omdb,tmdb,opensubtitles,ollama}/` for scrubbed mappings and their body files.
- `tests/LamuFlix.IntegrationTests/` for the existing `LoopbackWireMock.cs`, canonical provider adapter test setup including the relevant portions of `OpenSubtitlesSubtitleProviderTests.cs`, test host and direct-client guard wiring, guard and `Live` proofs, and `OllamaFixture.cs`.
- `web/e2e/stub.mjs` and `web/e2e/compose.e2e.yml` for shared response loading, preserved local controls and TMDb/OpenSubtitles e2e wiring; `docker-compose.yml`, new `docker/api.settings.fakes.json` and `src/LamuFlix.AppHost/` for the two fake-mode launches.
- `.env.example` and `README.md` for alignment. Document fixture paths, recording and scrubbing with a test key, the `Live` and Ollama opt-in commands and both fake launch commands. Describe the fake/live host-port constraint.

Charter §2.3 dispositions: Q1–Q3 and Q10 reuse existing projects, packages and hosts. New fixture folders and AppHost/Compose controls are ticket-named, and Patron approved the separate fake settings JSON under constitution VII. Q3–Q5 and Q10 govern credential isolation. Q5 explicitly rejected the unnamed CI workflow edit. Q6 limits Ollama changes to ticket-named files. Scope introduces no database migrations, public route/DTO/status changes, production test hooks, `Process.Start`, or `Features:LocalPlay` changes. No owner structural escalation was raised.

### Task ordering

1. Define the shared mapping subset and migrate OMDb into one set consumed by both hosts.
2. Add TMDb and OpenSubtitles mappings and move the canned Ollama response. Then enable and verify the two providers in e2e.
3. Reuse the proven fixture host in Compose fake mode and AppHost. Verify normal launches retain their configuration and both fake launches fail closed.
4. In parallel with the fixture work, wire and prove the test-only guard. Add the `Live` exception and searches only after guard coverage is in place.
5. Align the Ollama pin and collect the opt-in receipt independently of the other tasks.
6. Write the README against the verified setup. Collect the acceptance and gate receipts after implementation and verification on the delivery head.

## Test strategy and closing evidence

| Ticket criterion | Required proof |
|---|---|
| E2E with TMDb and OpenSubtitles enabled | Green e2e with both providers routed to the shared fixtures. Exercise the shared loader and confirm that an unsupported matcher produces a clear failure. |
| Fake-mode developer flow | One run each using Compose and AppHost. Browse, search subtitles and download one against the fakes. Capture API logs showing only loopback or approved Compose-network fake destinations for provider calls. Quote the relevant logs or grep result in the delivery PR. Check that missing fixture wiring fails setup or startup. |
| Real-host guard | Ordinary deliberate external URL fails naming the host, with zero sends in a recording transport. Prove loopback success and a redirect to a real host producing zero sends to that host. Assert the factory's registered named and typed clients enter the guard; wire the directly constructed adapter clients through it as well. |
| Live key opt-in | Observe each provider's independent skip without keys. Record successful local search-only runs with each provider's test key, or record `needs-owner-run` with the keyed evidence still open. CI must never receive those keys. |
| One OMDb set | Both hosts load the same checked-in files and the diff shows zero remaining copied OMDb responses in either host. |
| Ollama version | Confirm `0.40.13` tag or corresponding digest across all five acceptance surfaces. Run the real `OllamaIntegration` opt-in locally on the updated pin. |

## Review closing bar and caps

- Block on confirmed in-scope Critical, High or Medium findings and any confirmed acceptance, safety or constitution defect regardless of severity. Every remaining Low gets a recorded disposition. Require Sentry, Ledger and Compass for size L.
- Require same-head applicable C# Roslyn, Cyclomatic Complexity and InspectCode gates, the full .NET suite including architecture tests, formatting, relevant web checks and e2e, and applicable harness checks such as mutation. Keep `SKIPPED`, `N/A`, and `Could not run` distinct from PASS. The task-note baseline on four C# files is intake evidence only.
- Review cap: two rounds and two fix commits per round as set by the task-pipeline note. Third-round findings go to user review through the PR. Grill cap: 12 questions; closed at 11. Any later post-cap recommendation is tagged `[assumed]`, and any charter §2.3 care-list decision still needs a cited Patron ruling.
- This brief ends steps 1–3.1. Spec Kit drafting and analysis, the numbered ADR in the later size-L phase, plan challenge, Gate 1 and delivery remain separate.

## Step 6 plan-challenge decisions

[Patron’s ruling and Keel’s adjudication](CONCLUSIONS.md) refine Q1–Q5 and Q9–Q10.

- Each Live smoke owns and disposes a fresh guarded client with an immutable allowance for its provider host. Use synchronized overlap and a recording transport to prove that an ordinary client cannot inherit the allowance and the Live client cannot send to a different external host.
- Keep the bounded Q9 redirect follower and method rules. Remove Authorization when scheme, normalized host or effective port changes. Refuse HTTPS downgrades. Prove same-origin retention, allowed origin-change removal and zero sends on denied hops.
- List credential-only query parameters explicitly and exclude them from canonical matching by both readers. Retain functional query matching and prove it using an alternate synthetic credential in each host. Review scrubbed mappings and detached bodies, run the existing gitleaks rules against them before staging, and require the hosted gitleaks receipt on the delivery head. Record redacted outcomes.
- The Compose fixture service validates the same required read-only fake settings JSON mounted by the fake API. Require the expected fixture-service authority and path for every provider, exclusive provider settings from that JSON, and valid nonempty required mapping sets before readiness. The fake API depends on readiness. Prove missing mount, empty mappings and invalid provider wiring stop launch and identify the intended host or provider. Document shared infrastructure and host ports. Compare normalized default-profile Compose configuration and verify outbound provider destinations against the named fixture endpoint.
- Keep the guard in LamuFlix.IntegrationTests. Plan TestEgressGuardHandler.cs, GuardedHttpClientFactory.cs, TestEgressGuardTests.cs and LiveProviderSearchTests.cs in that project. Pickup inventories actual registrations and direct-client sites and verifies fixture files remain data-only under project and gate discovery. Expect mutation N/A for AppHost-only production C# under the explicit harness exclusion and verify the disposition on the delivery head.
