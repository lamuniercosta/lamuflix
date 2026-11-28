# DEV-549 Grill Conclusions

## Q1 — Shared fixture boundary

**Keel.** Which boundary should the shared fakes have? I recommend sharing checked-in, scrubbed response fixtures per service in `tests/fakes`, with the existing .NET WireMock and Node e2e hosts serving them through transport-specific adapters. That makes fixture data reusable across test hosts while preserving the existing test tooling and avoids a new project, layer, or package. Please rule on this boundary and confirm the conclusions location.

**Patron.** ACCEPT shared checked-in scrubbed recorded responses, one folder per service under `tests/fakes/<service>/`, served by the existing `LoopbackWireMock` and `web/e2e/stub.mjs`. Basis: ticket Wanted 1 and Where it lands row 1 (`tests/fakes/` new, WireMock and stub.mjs both read the same files) - ticket-named, so no §2.3 ruling needed; no new project, package or layer. Constraint: the transport adapters are thin file readers inside the two existing hosts (a WireMock mapping-folder load and a stub.mjs loader), not a new shared abstraction; Done-when requires the OMDb fake to be one file set with no copy left in either host; scrubbing means no real key, token or account data in any fixture (scrub-list rule). Conclusions location confirmed: `specs/DEV-549/CONCLUSIONS.md` on `feature/549-spec`; taste items to `specs/DEV-549/ASSUMPTIONS.md` tagged [assumed].

---

## Q2 — Fixture format

**Keel.** Shall we adopt the tickets default WireMock JSON mapping format as the checked-in response contract? I recommend one scrubbed mapping set per service, using only the request matching and response fields that both LoopbackWireMock and stub.mjs can consume, with matching coverage for each exercised scenario and host-local controls kept in stub.mjs. This keeps one source for response bodies and makes adding a fixture a data change for supported cases. Basis: Wanted 1-2, Done when OMDb, and Open points on format and keeping stub.mjs.

**Patron.** ACCEPT WireMock JSON mappings as the checked-in response contract, one scrubbed mapping set per service under tests/fakes/<service>/. Basis: ticket Open points, file format default WireMock JSON mappings and stub.mjs reads the same files; Open points, keep stub.mjs; Done when, OMDb fake is one set of files with no copy left in either host. Constraints: stub.mjs supports only the mapping subset the fixtures actually use (method, path or urlPath, query params, status, headers, jsonBody or bodyFileName) and fails loudly on an unsupported matcher rather than silently ignoring it; control routes stay host-local in stub.mjs, not in tests/fakes; no new npm or NuGet dependency for parsing.

---

## Q3 — Dev and seat fake mode

**Keel.** How should fake mode be isolated from normal deployment under Principle VII? I recommend selecting it through the ticket-named Compose fakes profile and AppHost switch, enabled by default for documented dev and seat launch paths. In fake mode, point every configured provider URL at a local or Compose-network fake, use only non-secret placeholders as needed, and fail setup when a fake endpoint is absent rather than fall back to a live URL. Keep container API settings in the mounted JSON required by Principle VII. Preserve production deployment defaults and settings transport. This requires explicitly selecting the Compose profile in the dev launch instructions and keeping the secret-free fake wiring separate from production credentials. Please rule under charter 2.3 on architecture and credential handling.

**Patron.** 

[from Patron] DEV-549 Q3 ruling: ACCEPT, with constraints. Not a constitution departure; no escalation.

- Basis, architecture: the Compose `fakes` profile and the AppHost switch are named in the ticket (Wanted 3; Where it lands row 4: `docker-compose.yml`, `src/LamuFlix.AppHost/`), so they are already approved. They add no new project, layer or abstraction. Fake mode is the dev default per Wanted 3 (DEV-548). Compose profiles are opt-in, so "default" means the documented dev and seat launch commands pass `--profile fakes`, and the AppHost switch defaults on for dev only. `deploy/k8s` and production Compose defaults stay unchanged, and no fake mode is reachable from the home overlay.
- Basis, credentials (constitution VII, lines 779-780 and 825-830): Compose fake mode carries provider base URLs and any keys through a mounted settings JSON, not env vars. Use a separate checked-in, secret-free fakes settings file (for example `docker/api.settings.fakes.json`, mounted at `/app/appsettings.Production.json` only under the `fakes` profile). Never edit or reuse the git-ignored `docker/api.settings.json`. The AppHost switch injects only fake URLs and placeholders, never user-secrets values. Placeholders must be obviously non-secret literals (for example `fake-key`) that live only in fake-mode config. Code gets no fallback key or URL default (VII: no hard-coded fallbacks).
- Basis, fail-closed: in fake mode a missing fake endpoint fails setup or startup with the host named. It never falls back to a live URL. The proof is Done-when 2: in fake mode, logs show no outside host.

---

## Q4 — Real-host test guard

**Keel.** What enforcement boundary should the real-host test guard use? I recommend one fail-before-send test-host guard applied to service HTTP transports for ordinary tests, including any directly constructed adapter clients, with a test-scoped Live exception. Allow only loopback destinations and validate every redirect destination before sending. Prove the guard with a deliberate real-host URL and a recording transport that sees zero sends while the failure names the host, plus passing loopback coverage. Basis: Wanted 5 and Done when guard proof. This puts the guard in the test project and requires each relevant client path to enter it. Please rule on that scope and the no-egress proof.

**Patron.** 

[from Patron] DEV-549 Q4 ruling: ACCEPT, with constraints.

- Scope (Wanted 5: a guard in the test host): the guard lives only in `tests/LamuFlix.IntegrationTests/`, with no guard code or test hook in `src/`. It attaches to every IHttpClientFactory client through the test host's service overrides (for example ConfigureHttpClientDefaults in the WebApplicationFactory). Adapter clients that tests construct directly go through the same guard handler via a shared test helper, so there is one guard with two entry points and no second implementation. Node e2e and MSW are outside this guard. Ticket Done-when 2 already covers them (logs show no outside host).
- Behaviour: fail before send. Loopback means an IP literal where IPAddress.IsLoopback is true, or `localhost`, with no DNS resolution. A blocked call fails the test with a message that names the host. The Live exception is test-scoped and keyed to the `Live` category (Wanted 4), never a global switch. Redirects: a DelegatingHandler does not see redirects that the primary handler follows. So in the guarded pipeline, either turn off auto-redirect and have the guard check and follow each Location itself, or put the check at the primary handler. Both are acceptable; pick one in plan.md.
- Proof (Done-when 3): one deliberate non-loopback URL test. It asserts the failure names the host and that a recording inner handler saw zero sends. Add one loopback pass case and one redirect-to-real-host case. Proving every client path enters the guard needs no extra test framework. One test that resolves each registered named or typed client and asserts the guard is in its handler chain is enough.

---

## Q5 — Live tests and credential handling

**Keel.** How should Live opt-in and credentials work? I recommend one bounded, search-only smoke case for each ticket provider with a credentialed search operation (OMDb, TMDb, OpenSubtitles), selected locally via Category=Live. Skip each case independently if its provider LAMUFLIX_TEST_* key is absent, grant the network exception only to the active Live case with that provider key, and never schedule downloads. Keep OllamaIntegration separately opt-in per Open points. For CI require no Live credentials, so the default run may discover and skip Live tests safely. Sanitize all recorded data and never emit credential values. Please rule under charter 2.3 on secret use and whether CI needs any file changes.

**Patron.** 

[from Patron] DEV-549 Q5 ruling: ACCEPT, with constraints. No CI file change.

- Scope (Wanted 4; Done-when 4): one search-only smoke case per provider: OMDb, TMDb, OpenSubtitles. Each case skips on its own when its `LAMUFLIX_TEST_<PROVIDER>_*` key is absent. The skip reason names the variable, never a value. No download, login or quota-spending call. OpenSubtitles search uses only the API key. `OllamaIntegration` stays as it is (Open points default) and is not folded into `Live`.
- Secrets (charter scrub rule; constitution VII lines 779-780): keys come only from process env at test time. They are never written to fixtures, logs, test output, the README or `.env.example`. Docs name the variables only. The guard exception (Q4) applies only to the running `Live` test and only to that provider's host, not to all non-loopback hosts. A recorded response has its key, token and account data scrubbed before check-in (Q1).
- CI: no change to `.github/workflows/ci.yml`. It is not named in Where it lands. The workflow never maps `LAMUFLIX_TEST_*` into env, so CI never sets them (Wanted 4) and `Live` cases skip there, which meets the Done-when 4 skip bar. Adding `Category!=Live` to the filter would edit an unnamed tracked file that no AC forces (§2.3 item 6). Rejected; noted, no ticket.

---

## Q6 — Ollama version alignment

**Keel.** Which Ollama pin should be authoritative for the named acceptance surfaces? I recommend 0.40.13, the version the ticket identifies in Compose and the home overlay digest. Bring .env.example, README.md and OllamaFixture.cs into alignment, check the existing Compose/home overlay pin, and record the base StatefulSet relationship in the README if needed without changing Kubernetes files. Preserve the real opt-in OllamaIntegration proof. Basis: Wanted 6, Done when version alignment, and the Out of scope clause. Please rule on the file boundary under charter 2.3 item 6.

**Patron.** ACCEPT 0.40.13 as the single Ollama pin. Basis: Done-when 6 names compose, home overlay, .env.example, README and OllamaFixture.cs; compose and the home overlay digest already carry 0.40.13 per ticket Problem, so only .env.example:19, README.md:343 and OllamaFixture.cs:22 change, all named in Where it lands row 5, so §2.3 item 6 is not triggered. Boundary: no edit to docker-compose.yml pin lines, deploy/k8s/overlays/home or deploy/k8s/base; the unpinned base StatefulSet gets at most one README line saying the home overlay supplies the pin. OllamaIntegration stays the real opt-in proof (Out of scope); confirm it passes locally on 0.40.13 as the closing evidence for the fixture bump.

---

## Q7 — Fixture coverage and runtime evidence

**Keel.** Which behavior should the initial fixture set and end-to-end evidence cover? I recommend sharing fixtures for OMDb, TMDb, OpenSubtitles and the existing fake Ollama embed route. Cover the provider interactions exercised by browse, subtitle search and one fake subtitle download, plus existing adapter characterization cases that depend on provider responses. Keep test-specific fault injection and stateful controls in their existing test hosts, without duplicating canonical fixture responses. Run e2e with both providers enabled and prove the fake-mode flow along both documented dev launch paths, including logs that show no outside host. Future DEV-554 to DEV-563 adapters supply their own fixtures. Basis: Goal, Wanted 1-3 and Done when 1-2 and 5. Please rule on coverage and evidence.

**Patron.** 

[from Patron] DEV-549 Q7 ruling: ACCEPT, with constraints.

- Services (Goal; Wanted 1-2): create shared fixture folders for OMDb, TMDb and OpenSubtitles. Also create `tests/fakes/ollama/`, but it only moves the existing `stub.mjs` `/api/embed` canned response; nothing new is invented. It is a canned embed for e2e and fake mode only. It never replaces or weakens `OllamaIntegration` (Out of scope). DEV-554 to DEV-563 add their own fixtures (Wanted 2). They are not stubbed ahead here.
- Coverage bar: the scenarios are the provider calls made by browse, subtitle search and one subtitle download. Each scenario gets at least one mapping. OMDb moves fully: every current OMDb response in `stub.mjs` and in the WireMock tests becomes the shared set, with no copy left (Done-when 5). For TMDb and OpenSubtitles adapter tests, move only the canonical success responses to shared files. Faults, error codes, malformed bodies and stateful control routes stay inline in their current host (Q2). Do not rewrite every hand-written handler test, which would be scope creep past Where it lands. If any canonical body is left in two places, that is a finding.
- Evidence (Done-when 1-2): e2e runs green with TMDb and OpenSubtitles on (`compose.e2e.yml:5` flipped) against the fakes. Run fake mode once on each launch path, the Compose `fakes` profile and the AppHost switch. Each run browses, searches subtitles and downloads one. The API logs are captured and show no non-loopback or non-Compose-network outbound host, quoted as the log line or grep result in the PR. The guard proof (Q4) and the Live skip proof (Q5) are separate evidence and do not count here.

---

## Q8 — Closing bar and review limits

**Keel.** What closing bar and round cap should govern review? I recommend that all confirmed in-scope Critical, High, and Medium findings block, together with any confirmed acceptance, safety, or constitution defect at any severity. Remaining Low findings need explicit disposition before closure. Require the three M/L review axes, passing applicable exact-head gates, and separate runtime receipts for fixtures/e2e, both dev launch paths, guard, Live with and without keys, and the real Ollama pin. Use the task-pipeline cap of two review rounds and two fix commits per round. This trades early closure for independently checkable evidence, while strictly routing scope additions to follow-up.

**Patron.** ACCEPT the closing bar and round cap as proposed. Basis: the task-pipeline standing note sets the cap at 2 review rounds and 2 fix commits per round; cite it, do not re-rule it here. The receipts match Done-when 1-6 one for one. Constraints: (1) the Live-with-keys receipt (Done-when 4) needs local LAMUFLIX_TEST_* keys. If no seat has them, report that once as needs-owner-run evidence and do not fake a pass; the without-keys skip receipt comes from any run. (2) A Low finding is closed with one line in CONCLUSIONS.md: fixed, or noted, no ticket. (3) A scope addition found in review becomes a follow-up ticket only under the charter filing bar (Critical, High, or broken behaviour). Anything else is noted, no ticket.

---

## Q9 — Redirect enforcement

**Keel.** Which redirect enforcement design should Quill put in the plan? I recommend disabling automatic redirects in guarded test clients, then following allowed redirects through the same guarded test pipeline with a bounded hop limit, validating resolved relative and absolute targets before each send. This allows the no-egress requirement to be tested using a recording transport without relying on whether the primary handler exposes a suitable hook. It puts the redirect handling cost in test code only. Basis: Q4 and Wanted 5.

**Patron.** ACCEPT. Turn off auto-redirect in guarded test clients only. The guard follows each redirect itself through the same pipeline, resolving the Location target and checking it before every send. Basis: Q4 left the choice to plan.md, and this is one of its two allowed options; Wanted 5. Constraints: (1) test code only, no change to production client redirect settings in src. (2) The guard follows redirects the way the default handler does, so adapters that rely on redirects behave the same under test. That means 301/302/303 POST becomes GET, 307/308 keep the method, and Authorization is dropped on a cross-host hop. (3) The hop limit is a named test constant. The guard fails the test when the limit is passed. The proof test from Q4 for a redirect to a real host must show zero sends to that host.

---

## Q10 — Compose and AppHost launch wiring

**Keel.** What Compose and AppHost launch wiring should Quill plan so default-on fake mode for dev cannot start a live-configured app alongside it? I recommend that the documented Compose dev command explicitly name the fake-profile application entry points in addition to --profile fakes. They depend on the existing infrastructure and the same Node fixture host as e2e, mount docker/api.settings.fakes.json only in fake mode, and do not pull in live-configured application instances. The AppHost dev switch starts that same fixture host as an AppHost-managed resource and supplies its URLs to provider clients. This isolates the launches without changing normal Compose behavior or using Process.Start. Basis: Q3, Wanted 3 and Done when 2. Please confirm the boundary and path.

**Patron.** 

[from Patron] DEV-549 Q10 ruling: ACCEPT the boundary and path, with constraints.

- Compose (Wanted 3; Q3): add fake-mode application services that exist only under `profiles: [fakes]`, with container names distinct from the normal ones. They mount `docker/api.settings.fakes.json` and depend on the existing infrastructure plus the shared Node fixture host. The documented dev command names those services explicitly, for example `docker compose --profile fakes up <fake services>`. That way no service outside the profile starts, and nothing in the live-configured app runs. The normal `docker compose up` stays byte-for-byte unchanged. Plan.md states that the live and fake app services share host ports and so cannot run together. That is intended.
- AppHost (§2.3 item 1): run the fixture host as a container resource with the core `AddContainer` API, using the same Node image and the same `stub.mjs` and `tests/fakes` bind mounts as e2e. Add no new NuGet package. Specifically, not `Aspire.Hosting.NodeJs` or the JavaScript hosting package, and not `AddExecutable` on the host. This keeps one fixture host definition across e2e, Compose and AppHost. The AppHost passes the provider base URLs from that resource's endpoint and placeholders only (Q3). The switch is on by default in the Development launch profile only.
- Fail-closed (Q3; Done-when 2): if the switch is on and the fixture resource or endpoint cannot be resolved, the AppHost fails to start. It never falls back to a live URL. No `Process.Start` and no code touching `Features:LocalPlay` is involved, so the item 5 check is clear.

---

## Q11 — Implementation order and grill closure

**Keel.** Can we close the grill on this implementation and verification order? I recommend fixture contract and OMDb consolidation first, then shared TMDb/OpenSubtitles and Ollama responses, then e2e wiring, then both dev launch paths. Independently implement the test-host guard and its loopback, denied-host and redirect proofs before adding the Live exception and smoke tests. The Ollama pin and opt-in validation can run independently; document the launch and recording commands against the verified setup, then collect all Done-when receipts and the scoped C# analyzer, full test, format, web and mutation dispositions on the delivery head. This keeps acceptance wired to the relevant task and ensures Live depends on the guard. Please confirm shared understanding and whether anything remains for this phase. Facts come from recon-DEV-549 and the exact ticket clauses you supplied.

**Patron.** CONFIRMED. The order is right: Live depends on the guard, OMDb is consolidated first for Done-when 5, and the pin runs on its own. Nothing else is open for this phase, so the grill can close at 11 questions. Two notes for the spec: (1) checklists/requirements.md should mark the user-visible outcome as Nothing visible to end users and give the reason: test and dev infrastructure only, with dev fake mode as the operator-visible result. (2) Commit CONCLUSIONS.md and ASSUMPTIONS.md on feature/549-spec with a DEV-549 - subject before you report the grill outcome.

---

## Step 6 — Plan challenge batch (Sentry F1/F2/F3/F6/F7, Ledger M1/M2)

**Keel.** Recommends (1) immutable client-local Live allowance, a fresh client owned and disposed by each smoke, no ambient or shared switch, barrier-driven concurrent ordinary-denial proof; (2) keep the Q9 bounded test-only follower and method matrix, strip Authorization on any scheme, normalized host or effective port change, deny HTTPS-to-HTTP downgrade, with recording proofs; (3) existing gitleaks plus review of mappings and detached bodies before staging and on the delivery head, no new dependency or CI change; (4) Compose fail-closed via a preflight and readiness dependency in the existing Node fixture service, validating the same read-only fixture JSON api-fakes mounts, exact fixture authority and path, named host/provider, no live override, no production code, with shared-state, missing-bind and empty-mapping negative proofs; (5) exclude credential-only query params from canonical match criteria in both readers, keep fake-key config, prove with a synthetic alternate value.

**Patron.** ACCEPT all five as recommended. No owner escalation.

- (1) Wanted 4 and 5; Q4 (exception is test-scoped, never a global switch) and Q5 (granted only to the active Live case and its provider host). A per-smoke immutable client is the direct form of that; the concurrency proof uses xUnit and existing primitives only.
- (2) Wanted 5; Done when 3; Q9 constraint 2. Stripping Authorization on scheme, host or port change and refusing HTTPS downgrade tightens Q9 toward the default handler's own behavior; it is not a departure. Hop limit stays a named test constant.
- (3) Charter scrub rule; constitution VII (no secrets in tracked files); Q5 rejected the CI workflow edit; §2.3 item 1 cleared because no package is added. Review evidence is recorded on the delivery head per Q8.
- (4) Wanted 3; Done when 2; Q3 and Q10 (fail closed, never fall back to live). The preflight lives in test fixture tooling and the ticket-named `docker-compose.yml`, so §2.3 items 2 and 6 are not triggered. The three negative proofs count as Done when 2 receipts.
- (5) Q2 and Q3; constitution VII. Excluded params are a named, explicit list in both readers, so the Q2 rule to fail loudly on an unsupported matcher still holds. The synthetic alternate value shows that matching does not depend on the key.
