# Tasks: Harden the local launcher

**Input**: `specs/DEV-458/` spec.md, plan.md, grill-closed brief.md (Q1–Q10), CONCLUSIONS.md (including Q7–Q10), ASSUMPTIONS.md, Proposed ADR 0036, notes DEV-458 / recon-DEV-458 / recon-DEV-458-2 section 7 / recon-DEV-458-2-2 section 8 / recon-DEV-458-2-3 section 9, analysis-DEV-458-Keel, specs/PRODUCT.md

**Status**: Draft — Phase 2 Spec Kit drafting after plan-challenge round-1 Q10 Option A reconciliation (same round). **PLAN FROZEN (Keel Q11)**. Gate 1 closed (user approval/merge still pending). Q9 owner checkbox answered **Option A** on draft spec PR #336 (brief/CONCLUSIONS Q10; DEV-458:40). Constitution Check = **owner-approved VII departure** for bounded I1 reader only — **not** unconditional VII PASS; remainder binding. Known 403/404 payload feasibility resolved; Phase B proof remains separately pending; Keel independent reread completed (brief Q11). 33 tasks / 22 requirements retained.

**Prerequisites**: plan.md (required), spec.md (required). No `research.md` / `data-model.md` / `contracts/` (plan records why). ADR 0036 already authored by Keel (Proposed until Gate 1 Accepted lifecycle; Quill does not edit ADR).

**Phase labels**: Phase A = planning docs. Gate 1 = explicit user approval + user-owned spec PR merge. Phase B = `T000` pickup after that merge. Implementation phases below run only after T000. Local phase numbers are for `/implement` dispatch, not canonical task-pipeline phases.

**Tests**: Required (brief Test strategy; ticket acceptance; Q7/Q8 startup matrices). Prefer red-green within each phase; required coverage green before verification. `[P]` means file-safe parallelism, not “tests must pass before implementation”.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites complete (different files, no incomplete deps). Same-file edits are never `[P]`.

**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no new explanatory comments; never hand-edit generated OpenAPI/TS; thresholds only from harness.yml; no GlobalExceptionHandler/Core/selector/handler/docker-compose/ServiceDefaults/web edits without a new Patron ruling; no throttle/containment/`--` separator/UseShellExecute change (Q8).

**Selected design (not Phase B proof)**: Dedicated API `IHostedService` in `src/LamuFlix.Api/LocalPlayListenerValidationHostedService.cs` registered from `Program.cs`; pure parser `LocalPlayListenerAddressParser.cs`; request helper `Endpoints/PlayRequestBoundary.cs`; synchronous validation or throw before server start; no ServiceDefaults change (brief Q7; CONCLUSIONS Q7; `recon-DEV-458-2:12-48`). Q9 history / Q10: I1 is owner-approved bounded `IConfiguration` Kestrel/Endpoints/Url reader (section absence + endpoint Url values; Option A on PR #336); named I0/I2/I3/I4/I6 retained; I7 Phase B T005 timing/parity retained; no new options record / reflection / code endpoint / silent fail-all / broad raw-config permission; VII exception approved ≠ unconditional VII PASS. Payload feasibility resolved to existing `Forbidden`/`Not Found` with absent detail; parameterless `NotFoundException()`; no handler/Core edit. Also return on required code-defined Kestrel endpoint or contradictory startup evidence — no silent exemption. Gate 1 merge + T000 still block production edits.

**Requirement coverage retained**: 17 FR + 5 SC = 22/22 (FR-001–003 → T001–T006; FR-004–006 → T007–T010; FR-007 → T020–T024; FR-008 → T013–T014/T016; FR-009 → T015–T016/T018; FR-010 → T015–T018; FR-011 → T015–T016; FR-012 → T014/T016/T018; FR-013 → T016/T018; FR-014 → T011; FR-015 → T000/T013/T032; FR-016 → T026; FR-017 → T003–T005/T009–T011/T016–T018/T022–T023/**T032**; SC-001 → T001–T006; SC-002 → T007–T010; SC-003 → T014–T018; SC-004 → T022–T023; SC-005 → T027–T031 + invariant receipt in T032).

---

## Phase dependency / execution order

```text
Gate 1   Explicit user approval + user-owned spec PR merge (prerequisite)
Phase B  T000 (pickup/drift) — blocks T001+
Phase 1  T001 → T006     Startup proven-loopback refusal (US1; Q7 composition)
Phase 2  T007 → T012     Play request/proxy trust boundary (US2)
Phase 3  T013 → T019     Launcher existence-first + 422 (US3)
Phase 4  T020 → T024     Shipped nginx/Aspire exclusion + runtime smoke (US2/US4)
Phase 5  T025 → T032     Docs lifecycle + harness gates + three-axis review (US4)
```

---

## Phase B: Pickup — fresh recon and drift reanalysis (blocks T001+)

**Purpose**: After Gate 1 user approval and user-owned spec PR merge (owner checkbox already answered Option A on PR #336), confirm brief/spec/plan/tasks still match landed main and DEV-438 before any production edit. Selected Q7/Q10 design does not invent PLAN FROZEN or Phase B proof PASS; Gate 1 remains closed until that separate user approval/merge.

- [ ] T000 Obtain fresh Phase B pickup recon against picked HEAD / landed main / DEV-438 for the authorized file set (Infrastructure/Playback, Api playback endpoint/`Program.cs`, nginx, AppHost, focused tests). Confirm selected Q7 composition remains API-only (`IHostedService.StartAsync` in `Program.cs`; no ServiceDefaults change) and still matches landed main. Run cross-artifact reanalysis of brief/spec/plan/tasks against landed main. Amend tasks only where authorized by demonstrated drift through the assigned workflow. **STOP** on unresolved drift or newly opened return-to-Patron conditions — do not invent substitutes. No production edits in this task (brief Task ordering; plan Risks; Q7).

---

## Phase 1: User Story 1 — Proven-loopback startup refusal (Priority: P1)

**Goal**: With LocalPlay enabled, every effective listener is proven loopback before any request is served; otherwise fail-start via selected API `IHostedService.StartAsync`.

**Independent Test**: Actual effective-listener startup tests per Q7 matrices (not parser-only).

- [ ] T001 [US1] Implement `src/LamuFlix.Api/LocalPlayListenerValidationHostedService.cs` + pure `src/LamuFlix.Api/LocalPlayListenerAddressParser.cs` (no mutable static initialization); register the hosted service from `src/LamuFlix.Api/Program.cs` ahead of externally active hosted services. `StartAsync` completes synchronously or throws before server start; observe **both** framework configuration inputs and `IServerAddressesFeature` including `PreferHostingUrls` via named surfaces **I0** (`IOptions<FeatureOptions>`), **I1** (Q10 Option A owner-approved bounded `IConfiguration` reader of Kestrel / Endpoints / Url outside options binding — inspect **section presence/absence** and configured endpoint **Url** values), **I2** (`IConfiguration[WebHostDefaults.ServerUrlsKey]`), **I3** (`WebHostDefaults.HttpPortsKey`/`HttpsPortsKey`), **I4** (`WebHostDefaults.PreferHostingUrlsKey` + feature `PreferHostingUrls`), **I6** (`IServer.Features.Get<IServerAddressesFeature>()`). I1 is an owner-approved VII exception, **not** unconditional VII PASS; Option B / Q2 rescope / new options record / reflection / code endpoint / silent fail-all / broad raw-config permission rejected (`recon-DEV-458-2-2` §8.4 historical; CONCLUSIONS Q9/Q10). Retain fail-closed precedence reconciliation (brief Q7.2 / CONCLUSIONS Q7 §2); apply URL-over-HTTP_PORTS/HTTPS_PORTS and **net10.0** empty-config default `http://localhost:5000` when I1 proves no endpoint configured anywhere; distinguish absent/unpopulated address feature vs proven merged inputs vs true empty-config default vs unprovable/irreconcilable (fail closed; never infer no inputs from empty feature alone; never pick the safe-looking surface). Normalize mapped IPv6 before loopback checks. **No ServiceDefaults change.** Production edits still require Gate 1 merge + T000. (FR-001, FR-002, FR-003, FR-015; Q2; Q7; Q9; Q10; `recon-DEV-458-2:12-42`).
- [ ] T002 [US1] Fail startup and exit with LocalPlay=true when any effective listener is wildcard, non-loopback literal, non-localhost hostname, mixed set, unknown/unprovable, Unix socket, or named pipe; impose no new bind restriction when LocalPlay=false; no new config key or test bypass. Introduce no code-defined `Listen`/`ListenAnyIP`/`ListenLocalhost`/`ListenUnixSocket`/`ListenNamedPipe`/`ListenHandle`/`UseSystemd`/endpoint-defining `ConfigureKestrel`; if such an endpoint is required, **STOP** and return to Patron with fresh recon (FR-001, FR-002, FR-003; Q2; Q7 G2).
- [ ] T003 [P] [US1] Add `tests/LamuFlix.UnitTests/Options/LocalPlayListenerAddressParserTests.cs` covering pure accept/reject grammar: mapped-loopback acceptance; wildcards; non-loopback literals; **hostname rejection** (DNS-independent; grammar-level coverage justified by Q2 — no new DNS seam); unknown/unprovable forms. Supplements only — do not close G1/G3 alone. Do not treat existing `OptionsStartupValidationTests.cs` as bind-refusal coverage (`recon-DEV-458:49`) (FR-017; Q2; Q7; Compass L3).
- [ ] T004 [US1] Add `tests/LamuFlix.IntegrationTests/LocalPlayRealKestrelHostFixture.cs` + `tests/LamuFlix.IntegrationTests/LocalPlayListenerStartupTests.cs` proving a **real Kestrel** host (not `ApiHostFactory`/`ApiEndToEndFactory` TestServer — those are insufficient per recon) with ephemeral ports, isolation, and cleanup/dispose. Cover: Kestrel-over-URL precedence; PreferHostingUrls both ways; HTTP_PORTS/HTTPS_PORTS shorthand and URL override; **net10.0** empty-config default (fail visibly on documented-default drift); absent/unpopulated feature vs proven merged inputs vs true empty default vs unprovable; mapped addresses; mixed unsafe sets; LocalPlay on/off; hostname rejection case where fixture-feasible else rely on T003 grammar rationale. Configuration-parser-only / TestServer-only tests are insufficient. On contradictory precedence or irreconcilable surfaces, **STOP** and return for recon naming which surface at `IHostedService.StartAsync` in the net10.0 shipped API host authoritatively carries the effective set (FR-001, FR-017; Q2; Q4; Q7; `recon-DEV-458-2` G1).
- [ ] T005 [US1] In `LocalPlayListenerStartupTests.cs`, assert LocalPlay=true startup refusal observables on real composition: start-sequence recording; validator `StartAsync` throws before server bind (no listening socket / no served request); before later-registered externally active hosted services start; no concurrent hosted service completing a bind or serving first; registration-order regression coverage; **and I7** empty-vs-unpopulated `IServerAddressesFeature` / PreferHostingUrls-copy timing/parity real-host observation with fail-closed reconciliation (never assume documented timing) (FR-001; Q2; Q7 ordering proof; Q9 I7; Compass L2; Ledger M2).
- [ ] T006 [US1] Checkpoint: startup invariant green on provable loopback real-Kestrel fixtures before request-guard work depends on LocalPlay-enabled hosts; no silent exemption from Q7/Q8 return conditions

---

## Phase 2: User Story 2 — Play request and proxy trust boundary (Priority: P1)

**Goal**: Play accepts only loopback raw peer without provenance headers; otherwise 403 before side effects.

**Independent Test**: Peer × header matrix with Q6 zero side effects (no catalog/disk/process/history).

- [ ] T007 [US2] Add `src/LamuFlix.Api/Endpoints/PlayRequestBoundary.cs` and wire it from `src/LamuFlix.Api/Endpoints/PlaybackEndpoints.cs`: require loopback raw peer after mapped-IPv6 normalization AND absence of exactly `Forwarded`, `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto`, `X-Real-IP` (no wider list; presence fails closed for any case/empty/multiple); do not enable UseForwardedHeaders or rewrite the peer (FR-004, FR-005, FR-006; Q1; Q4; Q8.3).
- [ ] T008 [US2] Map failed request-boundary checks to existing FeatureDisabledException → existing 403 ProblemDetails before catalog lookup, filesystem work, process start, or history, preserving title exactly `Forbidden` and absent detail (handler ignores exception messages; no custom detail text; no disabled-feature claim, peer address, provenance header name, exception message, or exception type name on the wire). Keep route/DTOs/declared statuses unchanged; **no** new exception type, mapping, GlobalExceptionHandler edit, or Core exception edit. Payload feasibility resolved by Q9/`recon-DEV-458-2-3` §9 (FR-005, FR-006; Q1; Q6; Q8 forbidden-content; Q9).
- [ ] T009 [US2] Extend **only** `tests/LamuFlix.IntegrationTests/PlaybackEndpointTests.cs` for IPv4/IPv6/mapped loopback success peers and null/non-loopback peer refusals with Q6 zero side-effect assertions **and** Q9 body asserts: title exactly `Forbidden`, detail absent, no disabled claim / peer address / provenance header name / exception message (including `LocalPlay is disabled in this environment.`) / exception type name (FR-004, FR-005, FR-017; Q1; Q6; Q9). Not `[P]` — exclusive owner of this file until T010/T017 complete.
- [ ] T010 [US2] Extend **only** `tests/LamuFlix.IntegrationTests/PlaybackEndpointTests.cs` for every of the five provenance headers (case/empty/multiple) → 403 with Q6 zero side effects **and** the same Q9 exact-title / detail-absence / no-leak body asserts; depends on T009 (same file; serial) (FR-004, FR-005, FR-017; Q1; Q4; Q8.3; Q9).
- [ ] T011 [P] [US2] Preserve LocalPlay-disabled ordering regressions **only** in `tests/LamuFlix.IntegrationTests/ApiEndToEndPlaybackTests.cs` and `tests/LamuFlix.IntegrationTests/PlaybackHistoryEndpointTests.cs`: catalog then EnsureEnabled → 403 before disk/process/history; assert title exactly `Forbidden`, detail absent, and no exception-message/type leak (handler drops `LocalPlay is disabled in this environment.`) while preserving Q6 order/side effects (FR-014, FR-017; Q6; Q9). Disjoint from T009/T010 file ownership; do not edit `PlaybackEndpointTests.cs` here.
- [ ] T012 [US2] Checkpoint: request-boundary matrix green before launcher outcome changes

---

## Phase 3: User Story 3 — Existence-first launch and unmapped-format 422 (Priority: P1)

**Goal**: Selected-file existence then player resolve then Start; 404/422 with zero start/history; DEV-438 order preserved.

**Independent Test**: Launcher unit precedence + API unmapped-format 422 rewrite + history/no-start regressions.

- [ ] T013 [US3] Inject existing `IFileSystem` into `src/LamuFlix.Infrastructure/Playback/ProcessMediaPlayerLauncher.cs` via existing `src/LamuFlix.Infrastructure/Playback/PlaybackServiceCollectionExtensions.cs` factory; if registration order prevents resolve, **STOP** and return to Patron — do not add a parallel seam (FR-008, FR-015; Q3; Q4; `recon-DEV-458-2:50-59`).
- [ ] T014 [US3] In `ProcessMediaPlayerLauncher.Launch`, check selected-file existence first (abstraction Exists), then resolve configured player, then `IProcessStarter.Start` with preserved UseShellExecute and exactly one ArgumentList file argument; case/dot-insensitive mapping unchanged (FR-008, FR-012; Q3).
- [ ] T015 [US3] Throw existing Core **parameterless** `NotFoundException()` when selected file is absent (no custom message/constructor; never selected LibraryPath/full or partial filesystem path/directory/basename/player executable/configured-player path); throw existing Core `ValidationException` with safe format-level detail only when no player supports the format (replace InvalidOperationException path); existence wins when both would apply. Keep existing NotFound→404 mapping (title `Not Found`, no detail; handler ignores messages); **no** handler/GlobalExceptionHandler/Core exception edit (FR-009, FR-010, FR-011; Q3; Q9; `recon-DEV-458-2-3` §9).
- [ ] T016 [P] [US3] Update `tests/LamuFlix.UnitTests/Playback/ProcessMediaPlayerLauncherTests.cs` and `PlaybackServiceCollectionExtensionsTests.cs` for success (MockFileSystem-arranged files), missing file with parameterless NotFound throw, unmapped → ValidationException, both-failures → NotFound wins, mapping, hostile filename as **single ArgumentList entry only** (no `--` separator; no UseShellExecute change), and process-start failure; assert no path/basename/player leak in exception surface used by tests (FR-012, FR-017; Q3; Q9).
- [ ] T017 [US3] Rewrite **only** `tests/LamuFlix.IntegrationTests/PlaybackEndpointTests.cs` unmapped-format case from InternalServerError/500 to safe 422 ProblemDetails without file/player paths; assert zero process start; depends on T010 (same file; serial after Phase 2 endpoint ownership) (FR-010, FR-017; recon-DEV-458:41).
- [ ] T018 [US3] Keep/extend success 204, folder-missing 404, no-video 409, validation 422, history-after-success, and no-history-on-failure coverage in `tests/LamuFlix.IntegrationTests/ApiEndToEndPlaybackTests.cs`, `PlaybackHistoryEndpointTests.cs`, and handler tests as applicable; assert FR-009 404 bodies have title exactly `Not Found`, detail absent, and omit exception message (including `Not found.`), exception type name, path/basename/player values, peer address, and provenance header name while status stays 404 with zero starts/history; depends on T011 (same e2e/history files; serial). Do **not** edit `PlaybackEndpointTests.cs` here — that file stays with T009/T010/T017. Not `[P]` while sharing T011 files (FR-009, FR-012, FR-013, FR-017; Q3; Q6; Q9).
- [ ] T019 [US3] Checkpoint: launcher 404/422 precedence green; no Core port or PlayMovieCommandHandler edits

---

## Phase 4: User Stories 2/4 — Shipped proxy exclusion and runtime smoke (Priority: P1/P2)

**Goal**: Shipped nginx/Aspire cannot elicit a launch; positive local control proves launcher health; Aspire injected-listener environment is observed.

**Independent Test**: Actual running shipped-path smoke with process-start observation (not mocked proxy / not config-read-only).

- [ ] T020 [US2] Update `docker/nginx.conf` to deny `POST /api/movies/{id}/play` with **proxy 403** before `/api/` `proxy_pass`. Retain Compose LocalPlay-disabled default but do not rely on it as the exclusion. No docker-compose/README edit; mechanism text stays in `specs/DEV-458/` (FR-007; Q1; Q4; Q8.1).
- [ ] T021 [US2] Aspire path: retain disabled web `WithEndpointProxySupport(false)` and permit no endpoint proxy to forward play. **No** `src/LamuFlix.AppHost/Program.cs` edit unless Phase B smoke shows play forwarding through the web proxy; then only minimal retaining/applying `WithEndpointProxySupport(false)` is in-ruling — anything beyond returns to Patron. Direct API remains under Q1 peer/header + Q2 startup rules (FR-007; Q1; Q4; Q8.1).
- [ ] T022 [US4] Run actual shipped nginx negative-launch smoke with process-start observation: expect **proxy 403** and zero starts. Run **separate named** LocalPlay-enabled loopback positive-control host using the **same build, configured player fixture, and selected-file fixture**; expect 204 and exactly one observed process start — never present as proof the excluded topology can launch. Record PASS or Could not run (never inferred PASS). Do not replace with startup unit tests (FR-007, FR-017; Q1; Q4; Q8.2; SC-004).
- [ ] T023 [US4] Run actual Aspire-path smoke: web-proxy attempt → no forwarded launch + zero starts; header-bearing or non-loopback direct-API attempt → API 403 + zero starts; reuse the same named positive-control host/fixtures as T022 (204 + one start). **Observe the actual shipped Aspire API listener environment** and reconcile with the Q7 validator (G3). Record PASS or Could not run. On contradictory injection vs precedence, or AppHost edit need beyond Q8, **STOP** and return for recon/Patron (FR-007, FR-017; Q1; Q4; Q7; Q8.1–Q8.2; SC-004; `recon-DEV-458-2` G3).
- [ ] T024 [US4] Checkpoint: smoke evidence attached; positive control not conflated with negative topology; unsupported stripping-loopback-proxy / unlisted-header residuals not asserted as solved; Aspire observation recorded

---

## Phase 5: User Story 4 — Pipeline lifecycle, gates, review, handoff (Priority: P2)

**Goal**: Labelled pipeline tasks with seat ownership for ADR lifecycle, harness gates, Size M review, and handoff; preserve gate/task coverage and all 33 task IDs (T000–T032). Builder cannot self-claim Keel adjudication or owner merge.

**Independent Test**: Gate scripts + three-axis findings published on the PR path the pipeline names.

- [ ] T025 [P] [US4] **[pipeline · ADR lifecycle · seat: Keel]** Confirm Proposed ADR `docs/adr/0036-local-launcher-trust-boundary.md` remains Proposed until Gate 1 acceptance; after Gate 1 merge receipt, perform explicit owner/Keel Accepted lifecycle update only (Quill/Anvil do not rewrite ADR body in Phase A) (brief; plan ADR status).
- [ ] T026 [US4] **[pipeline · contract refresh · seat: Anvil]** If a gate requires contract refresh, regenerate OpenAPI → TypeScript via established tooling only (`web/src/api/openapi.json`, `web/src/api/types.ts`); never hand-edit; explain any unexpected diff in the PR (FR-016; Q4).
- [ ] T027 [US4] **[pipeline · analyzer gates · seat: Anvil during build; Gauge on ship re-run]** Run changed-C# analyzer gates sequentially: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (then refactor threshold from harness.yml), `./scripts/run-jetbrains-inspectcode.ps1` (FR-017; Q5).
- [ ] T028 [US4] **[pipeline · test/format/package gates · seat: Anvil during build; Gauge on ship re-run]** Run full `dotnet test`, property tests, vulnerable-packages, and `dotnet format --verify-no-changes` per harness; preserve SKIPPED/SKIP/N/A/Could not run labels (FR-017; Q5).
- [ ] T029 [US4] **[pipeline · mutation/architecture/contract · seat: Anvil during build; Gauge on ship re-run]** Run mutation via `pwsh -NoProfile -File ./scripts/run-mutation.ps1` for eligible changed production C# (no worktree `--since`); architecture and contract-drift checks by applicability (FR-017; Q5).
- [ ] T030 [US4] **[pipeline · three-axis review · seats: Sentry, Ledger, Compass]** Collect Size M three-axis review at the reviewed SHA under brief closing bar, including verification that Q7/Q8 obligations (VII surfaces, no-code-Listen invariant, real-Kestrel startup matrices, Aspire observation, nginx/Aspire smoke, truthful 403 / path-free 404) were evidenced rather than assumed; missing axis blocks adjudication; max two remediation rounds (Q5; Q7; Q8).
- [ ] T031 [US4] **[pipeline · PR findings handoff · seat: Bernstein coordinates; owner/user merges]** Publish review findings / dispositions on the PR path; hand off for owner merge decision — no seat merge or auto-merge; builder cannot self-claim adjudication or merge (brief Task ordering; PRODUCT §2).
- [ ] T032 [US4] **[pipeline · final clearance checkpoint · seat: Anvil evidence; Bernstein clearance gate]** Final checkpoint: no unauthorized file touches (including no ServiceDefaults change); attach `src/` source-search receipt at reviewed head for Listen-family/`UseSystemd`/endpoint-defining `ConfigureKestrel` plus review (**FR-017** coverage); ASSUMPTIONS.md still empty of taste decisions; Phase B startup/smoke evidence not silently assumed PASS; any VII/code-endpoint/contradictory-startup/Q8 inexpressible-payload return condition left open blocks clearance (FR-015, FR-017; Q7; Q8; SC-005)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Owner checkbox + Gate 1 / T000**: Q9 owner checkbox answered Option A on PR #336 (Q10); Gate 1 still closed pending separate user approval/merge; after Gate 1 merge, T000 blocks all production edits until drift recheck completes.
- **Phase 1 (US1)**: Selected Q7 composition + startup invariant for LocalPlay-enabled hosts.
- **Phase 2 (US2)**: Request guard; can follow US1; independent of launcher internals.
- **Phase 3 (US3)**: Launcher outcomes; independent of proxy config files. `PlaybackEndpointTests.cs` ownership is serial: T009 → T010 → T017. `ApiEndToEndPlaybackTests.cs` / `PlaybackHistoryEndpointTests.cs` ownership is serial: T011 → T018.
- **Phase 4**: Shipped config + runtime smoke after behavioural code exists (positive local control needs working launcher); Aspire observation mandatory.
- **Phase 5**: Gates, review, and invariant receipt after Phases 1–4.

### User Story Dependencies

- **US1**: After T000; uses selected Q7 API `IHostedService.StartAsync` design.
- **US2**: After T000; preferably after US1 for LocalPlay-enabled host fixtures; request matrix can use existing test-host loopback peers.
- **US3**: After T000; no dependency on nginx/Aspire file edits.
- **US4**: After US1–US3 behaviour exists for meaningful smoke/gates.

### Parallel Opportunities

- T003 parallel with composition/tests work only when touching different unit-test files from T004/T005.
- T009 and T011 parallel: T009 owns `PlaybackEndpointTests.cs`; T011 owns `ApiEndToEndPlaybackTests.cs` + `PlaybackHistoryEndpointTests.cs` only.
- T009 → T010 → T017 are strictly serial on `PlaybackEndpointTests.cs` (no `[P]` among them).
- T011 → T018 are strictly serial on e2e/history files (T018 is not `[P]`).
- T016 parallel with T017 after T015 (different files: unit vs `PlaybackEndpointTests.cs`).
- T016 may also proceed while T018 waits on T011, because T016 does not touch e2e/history files.
- T022 and T023 sequential preferred if they contend for the same Compose/Aspire runtime; otherwise separate environments may run in parallel.
- T025 parallel with gate prep only if no file contention.

### Parallel Example: endpoint vs e2e ownership

```text
# Phase 2 — disjoint files, OK in parallel
Task: PlaybackEndpointTests.cs peer matrix (T009)
Task: ApiEndToEndPlaybackTests.cs + PlaybackHistoryEndpointTests.cs disabled ordering (T011)

# Phase 2/3 — same file, NEVER parallel
Task: PlaybackEndpointTests.cs headers (T010) after T009
Task: PlaybackEndpointTests.cs unmapped 422 (T017) after T010

# Phase 3 — unit vs endpoint OK; e2e/history serial after T011
Task: ProcessMediaPlayerLauncherTests.cs + PlaybackServiceCollectionExtensionsTests.cs (T016)
Task: ApiEndToEnd / History success+history regressions (T018) after T011
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete T000 pickup against selected Q7 design.
2. Deliver US1 startup refusal (Q7 composition + real-startup matrices) + US2 request boundary.
3. **STOP and VALIDATE** peer/header and startup matrices; honor return-to-Patron conditions.

### Incremental Delivery

1. Add US3 launcher 404/422 precedence.
2. Add US4 shipped exclusion + runtime smoke + Aspire listener observation.
3. Run gates, three-axis review, and no-code-Listen receipt; owner merges.

### Notes

- Same-file API test edits must follow ownership above; conflicting `[P]` markers removed.
- Do not edit GlobalExceptionHandler, Core ports/handlers/exception types, DEV-438 selector, docker-compose, ServiceDefaults, or `web/` without a new Patron ruling (Q4; Q7; Q9).
- Unrunnable required smoke or gate is outstanding, not PASS (Q5).
- Plan freeze only; no Gate 1 / Phase B PASS / implementation / runtime PASS claim. PLAN FROZEN (Keel Q11); Gate 1 closed pending separate user approval/merge; draft PR #336 already exists per supplied receipt; Keel independent reread completed (brief Q11).
