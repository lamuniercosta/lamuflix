---
description: "Task list for DEV-549 shared service fakes and test egress guard"
---

# Tasks: Shared service fakes and test egress guard

**Input**: Design documents from `specs/DEV-549/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q11 and Step 6), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `contracts/shared-fixture-mapping.md`; Maestri notes `DEV-549`, `recon-DEV-549`; `specs/PRODUCT.md`.

**Status**: Draft

**Tests**: Required (`brief.md` Test strategy; FR-001–FR-014; SC-001–SC-007). TDD optional. Use existing IntegrationTests and e2e homes. No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence tasks are never `[P]`. Remove `[P]` wherever steps touch the same inventoried file or depend on unfinished work unless an explicit ownership/serialization rule is stated.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief Files list; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no CI workflow edit; no k8s pin edits; no guard in `src/`; no new NuGet/npm; never lower harness thresholds; no explanatory comments on changed C# beyond AGENTS exemptions. Otherwise-unassigned implementation tasks are owned by the generic Build lane. Named seats own Spec PR, pickup, refactor, gates, review, and ship per `task-pipeline`. Closing bar and caps: `brief.md` Q8 (single source).

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: After plan challenge clearance, Bernstein coordinates; Rigger prepares/opens the Spec Kit PR; only the user merges; Keel records Gate 1 merge receipt. Ordinary Gate 1 remains. No Build tasks (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T001+
Phase 1   T001          (Wisp pickup recon; Keel reanalysis) — blocks production edits
Phase 2   T002–T006     (US1 shared mapping subset + OMDb consolidation) — after T001; T002 after T004 (no T002||T003)
Phase 3   T007–T011     (US2 TMDb/OpenSubtitles/Ollama embed + enabled-provider e2e) — after T004; T007 after T008 (no T007||T008)
Phase 4   T012–T016     (US3 Compose fakes + AppHost switch) — after T010 (T012 included; FR-007 Compose setup on T012/T013)
Phase 5   T017–T021     (US4 egress guard + proofs) — after T001; independent of Phases 2–4; before Live
Phase 6   T022–T024     (US5 Live smokes) — after guard proofs (T020)
Phase 7   T025–T027     (US6 Ollama pin + README) — pin after T001; five-surface (SC-006) after README pin; opt-in independent; README after verified launches
Phase 8   T028–T030     (Polish: gates, receipts, Size L review) — final same-head receipts after any refactor before review
```

Ordering per brief Task ordering / Q11. Enabled-provider e2e (T010) precedes Compose/AppHost fake rollout including settings (T012–T015). Live depends on guard. OMDb consolidation precedes Done-when 5. Guard and pin lanes stay independently runnable after pickup (separate from fixture serialization). No dependency cycle.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-549 Spec Kit PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-549 in `specs/DEV-549/CONCLUSIONS.md`. Receipt only. T001–T030 MUST NOT proceed without verified merge (`brief.md` Task ordering; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon (blocks production edits)

**Purpose**: Inventory exact current OMDb/TMDb/OpenSubtitles/Ollama embedding sites, IntegrationTests HTTP client registrations/constructions, fixture discovery, and launch seams on pickup HEAD. Read-only.
**Checkpoint**: Written pickup receipt cites authorized paths; Keel read-only reanalysis clear. Any authorized drift amendment is a separate follow-up before production edits — T001 itself does not edit production files.

- [ ] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree inventorying (1) every OMDb response/mapping still living in WireMock-consuming tests under `tests/LamuFlix.IntegrationTests/` and in `web/e2e/stub.mjs`, (2) canonical TMDb and OpenSubtitles success bodies to move vs host-local faults/controls to keep, (3) current `/api/embed` canned response site, (4) `web/e2e/compose.e2e.yml` TMDb/OpenSubtitles flags and `omdb-stub` mounts, (5) `docker-compose.yml` service shape for adding `profiles: [fakes]` services without editing Ollama pin lines, (6) `src/LamuFlix.AppHost/Program.cs` injection points for `UseServiceFakes`, (7) Ollama pin sites in `.env.example`, `README.md`, `OllamaFixture.cs`, and verification-only Compose/home overlay digests, (8) **all actual** named and typed `IHttpClientFactory` registrations and every direct adapter client construction site in `tests/LamuFlix.IntegrationTests/` needing the guard helper (incl. relevant portions of `OpenSubtitlesSubtitleProviderTests.cs` and existing test-host files at pickup), (9) confirmation that `tests/fakes` stays data-only under existing test/project/gate discovery (no Evaluation.IntegrationTests or Tests.Common expansion; no new architecture analyzer), (10) ADR 0051 number availability against current main and open ADR work before later publication/merge (ADR remains proposed until Build). Guard/Live implementation files are fixed as `TestEgressGuardHandler.cs`, `GuardedHttpClientFactory.cs`, `TestEgressGuardTests.cs`, `LiveProviderSearchTests.cs`. **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. **STOP** `needs recon:` on missing seams; **STOP** for cited Patron ruling rather than expanding file/protected surface. **Strictly read-only reconnaissance and analysis**.

**Checkpoint**: T002+ unlocks only with a passing Wisp receipt and Keel pickup analysis clear.

---

## Phase 2: User Story 1 - One shared OMDb fixture set (Priority: P1) 🎯 MVP

**Goal**: `tests/fakes/omdb/` is the only OMDb response set; WireMock and `stub.mjs` both load it with credential exclusions.
**Independent Test**: Zero OMDb copies remain in either host; both hosts load the same checked-in files (required same-file evidence); supported mappings serve; unsupported matcher fails clearly; alternate synthetic credential matches in both hosts; scrub/gitleaks redacted clean before staging.
**Order**: After T001.

### Implementation for User Story 1

- [ ] T003 [US1] **Build lane**. After T001: Create `tests/fakes/omdb/` and migrate every current OMDb mapping/body out of WireMock test setup and `web/e2e/stub.mjs` into that folder (scrubbed). Leave zero OMDb copies in either host (FR-001–FR-002; SC-005; Q1, Q7). Before staging mappings and detached body files, run existing gitleaks/secret-scan rules against those paths and complete a manual scrub review for account data; record redacted outcomes (no secret values). Serializes with T002/T004 on `stub.mjs` / WireMock response and consumer edits — write sets are not disjoint, so not `[P]` with T002 or T004.
- [ ] T004 [US1] **Build lane**. After T003: Teach existing `tests/LamuFlix.IntegrationTests/LoopbackWireMock.cs` consumers and `web/e2e/stub.mjs` to load `tests/fakes/<service>/` through thin readers; implement the supported mapping subset in both readers including the explicit credential-only query exclusions from `contracts/shared-fixture-mapping.md` (`apikey` for OMDb); retain functional query matching; fail clearly on unsupported matchers (FR-002–FR-003; Q2; Step 6). Keep control routes host-local in `stub.mjs`. Keep `fake-key` in fake-mode configuration. Record the unsupported-matcher negative proof and the alternate-synthetic-credential match proof in both hosts as required evidence when exercised here.

### Tests for User Story 1

- [ ] T002 [US1] **Build lane**. After T004: Add or extend IntegrationTests coverage under `tests/LamuFlix.IntegrationTests/` proving WireMock serves at least one OMDb scenario from `tests/fakes/omdb/` via the thin shared-folder load, that credential-only `apikey` is excluded from match criteria, and that an alternate synthetic credential still matches (FR-001–FR-003; SC-005; `contracts/shared-fixture-mapping.md`). Does not edit `web/e2e/stub.mjs`. Not `[P]` with T003 — excluding `stub.mjs` still leaves overlapping WireMock response/consumer edits.
- [ ] T005 [US1] **Build lane**. After T002: Make T002 pass; confirm diff shows both hosts load the same checked-in OMDb files and no remaining OMDb response copies in WireMock tests or `stub.mjs` (SC-005 required same-file / zero-copy proof).
- [ ] T006 [US1] **Gauge checkpoint**. After T005: Run the three static gates on this phase’s changed `.cs` (`scripts/run-roslyn-analyzers.ps1`, `scripts/run-cyclomatic-complexity.ps1`, `scripts/run-jetbrains-inspectcode.ps1`, no `-All`) and the focused IntegrationTests proving shared OMDb load. Record verdicts (keep SKIPPED/N/A/Could not run distinct from PASS).

**Checkpoint**: Shared OMDb set + loader contract ready for additional services and e2e.

---

## Phase 3: User Story 2 - TMDb/OpenSubtitles fakes and e2e (Priority: P1)

**Goal**: Shared TMDb/OpenSubtitles success fixtures and canned Ollama embed; e2e green with both providers on **before** Compose/AppHost fake rollout.
**Independent Test**: Green e2e against fakes with TMDb + OpenSubtitles enabled; browse / subtitle search / one fake download exercised; unsupported-matcher negative proof recorded if not already closed in Phase 2; credential exclusions / alternate-synthetic proofs where listed; scrub/gitleaks on new mappings/bodies.
**Order**: After T004 (shared loader). Enabled-provider e2e verification is brief step 2 and MUST complete before Phase 4 rollout tasks T013–T015.

### Implementation for User Story 2

- [ ] T008 [US2] **Build lane**. After T004: Create `tests/fakes/tmdb/`, `tests/fakes/opensubtitles/`, and `tests/fakes/ollama/`; add mappings for browse, subtitle search, and one subtitle download; move only canonical TMDb/OpenSubtitles success bodies; move the existing canned `/api/embed` response from `web/e2e/stub.mjs` into `tests/fakes/ollama/` (FR-001; FR-004; Q7). Before staging, run existing gitleaks rules and manual scrub review for account data on new mappings and detached bodies; record redacted outcomes. Serializes with T007/T009 on `stub.mjs` / WireMock response and consumer edits — write sets are not disjoint, so not `[P]` with T007; T009 waits for T008.
- [ ] T009 [US2] **Build lane**. After T008: Update `web/e2e/stub.mjs` and `web/e2e/compose.e2e.yml` so TMDb and OpenSubtitles are enabled and served from shared fixtures (mount `tests/fakes` as required). Apply contract credential exclusions (`api_key` for TMDb; none for OpenSubtitles/Ollama). Preserve control routes host-local (FR-004; Wanted 2).

### Tests for User Story 2

- [ ] T007 [US2] **Build lane**. After T008: Add/adjust IntegrationTests under `tests/LamuFlix.IntegrationTests/` so canonical TMDb and OpenSubtitles success paths load from `tests/fakes/{tmdb,opensubtitles}/` without rewriting every hand-written fault case; prove TMDb `api_key` exclusion and alternate synthetic credential match in the WireMock host (FR-003–FR-004; FR-014; Q7; Step 6). Does not edit `web/e2e/stub.mjs` or `compose.e2e.yml`. Not `[P]` with T008 — excluding `stub.mjs` still leaves overlapping WireMock response/consumer edits.
- [ ] T010 [US2] **Build lane**. After T009 and T007: Run e2e and require green against the fakes with both providers on (SC-001). Explicitly exercise browse, subtitle search, and one fake subtitle download. Require the clear unsupported-matcher failure proof (complete here if not already recorded in Phase 2). Require Node-host alternate-synthetic credential match for listed providers if not already recorded in Phase 2. This receipt unlocks Compose/AppHost fake rollout including settings (T012–T015).
- [ ] T011 [US2] **Gauge checkpoint**. After T010: Record e2e receipt and adapter-proof disposition from T007; run static gates on any new/changed `.cs` from this phase; keep SKIPPED/N/A/Could not run distinct from PASS.

**Checkpoint**: E2E Done-when 1 evidence available; Phase 4 rollout may proceed.

---

## Phase 4: User Story 3 - Dev and seat fake mode (Priority: P1)

**Goal**: Compose `fakes` profile and AppHost `UseServiceFakes` launches against the shared fixture host, fail-closed via Node preflight, secret-free.
**Independent Test**: Compose + AppHost fake runs browse/search/download with no-outside-host PR log evidence and destinations against named fixture endpoint; Compose preflight alone fails naming the provider/host on missing bind, empty fixture set, or invalid/missing provider endpoint (API held by readiness); AppHost independently fails when fixture resource or endpoint wiring cannot resolve (Q10; T014/T015); normalized default-profile Compose config matches pre-change baseline; normal Compose and AppHost non-fake baselines unchanged; shared infrastructure/state and port constraint documented.
**Order**: T012–T015 after **T010** (enabled-provider e2e proven), per brief steps 2–3 — not after T004 alone / “prefer T008”. FR-007 Compose fake-mode setup is assigned to T012/T013; T015 proves both launch paths.

### Implementation for User Story 3

- [ ] T012 [P] [US3] **Build lane**. After T010: Add checked-in secret-free `docker/api.settings.fakes.json` assigning **FR-007 Compose fake-mode setup**: every configured provider gets a fake endpoint (base URLs + `fake-key` placeholders only; no Compose env for provider URLs/keys; no provider env override or live settings/fallback). The same required read-only JSON is the authority the fixture-service preflight will validate (exact fixture authority and path per provider). **Compose preflight only** (T013) fails closed on missing bind, empty fixture set, or invalid/missing provider endpoint naming the affected provider/host before any live fallback (FR-005; FR-007; Q3; Step 6). Does not edit `docker-compose.yml` or AppHost. No startup validation added under `src/`.
- [ ] T013 [US3] **Build lane**. After T012: Update `docker-compose.yml` to add `profiles: [fakes]` application services with **distinct container names** (`api-fakes` and companions as required) mounting `docker/api.settings.fakes.json` at `/app/appsettings.Production.json` **only under the fakes profile**, depending on existing infrastructure plus the shared Node fixture host (`service-fakes`) using the same Node image and bind-mounting `web/e2e/stub.mjs` and `tests/fakes` as e2e. Implement **fakes-only startup preflight in existing `web/e2e/stub.mjs`**: read the same required read-only fake settings JSON; validate every configured provider’s exact fixture authority and path and nonempty valid required mappings; refuse provider env override or live settings/fallback; expose readiness only after validation. Wire the fake API to **depend on that validated readiness**. Complete **FR-007 Compose fake-mode setup** on this path. Documented launch command names those fake-only services explicitly so only the intended fake app plus its intended infrastructure starts. Do **not** change Ollama pin lines. Document shared Compose infrastructure/state and that fake and live app services share host ports and must not run together. Keep normal `docker compose up` unchanged for non-profile services (FR-005; FR-007; Q10; Step 6). No startup validation under `src/`.
- [ ] T014 [US3] **Build lane**. After T010: Update `src/LamuFlix.AppHost/Program.cs` so Development-default `UseServiceFakes` starts the fixture host via core `AddContainer` (same Node image and mounts as e2e/Compose fake), injects only fake URLs/placeholders from that resource for **every configured provider**, and fails startup naming the affected host when the fixture resource or endpoint wiring cannot be resolved (AppHost independent proof; not the Compose-preflight three negatives). Preserve constitution VII typed-options binding and request-time subtitle acquisition settings behaviour (no change to that ValidateOnStart exception). No new NuGet; no `AddExecutable` for the fixture host; no `Process.Start`; no `Features:LocalPlay`; no `src/` startup validation beyond AppHost wiring (FR-006; Q10).
- [ ] T015 [US3] **Build lane**. After T013–T014: Run one Compose fake launch and one AppHost fake launch; each must browse, search subtitles, and download one against fakes. Capture API logs proving no non-loopback / non-Compose-network outside provider host and that provider destinations resolve against the named fixture endpoint (quote/grep for the PR). Prove **Compose preflight only**: **missing bind**, **empty fixture set**, and **invalid/missing provider endpoint** each stop launch naming the provider/host (fake API held back by readiness). Prove AppHost independently fails startup naming the affected host when the fixture resource or endpoint wiring cannot be resolved (Q10; covers FR-007 Compose preflight on T012/T013 and FR-006 AppHost on T014). Compare **normalized default-profile Compose configuration** to the pre-change baseline (services, dependencies, mounts, ports, provider settings) and verify explicit fake-target closure (SC-002; Ledger L7). Confirm normal `docker compose up` and baseline AppHost non-fake wiring still use live-configured / non-fake paths.
- [ ] T016 [US3] **Gauge checkpoint**. After T015: Run static gates on changed AppHost `.cs`; record both positive fake-launch receipts and fail-closed proofs; keep SKIPPED/N/A/Could not run distinct from PASS.

**Checkpoint**: Done-when 2 evidence available for both launch paths.

---

## Phase 5: User Story 4 - Real-host egress guard (Priority: P1)

**Goal**: Ordinary IntegrationTests cannot reach non-loopback hosts; redirects checked with origin-defined Authorization rules (changed-origin removal includes same-host different effective port and permitted HTTP-to-HTTPS); factory + direct clients covered against T001 inventory.
**Independent Test**: Denied-host zero-send, loopback pass, parameterized redirect proof table (including same-host different effective port and permitted HTTP-to-HTTPS changed-origin Authorization removal), factory named/typed **and** direct-client helper pipeline assertions reconciled to inventory; test-only wiring in named files.
**Order**: After T001; independently runnable with Phases 2–4 (different primary files; no same-file `[P]` conflict with fixture edits). Must complete before Live (Phase 6).

### Tests for User Story 4

- [ ] T017 [P] [US4] **Build lane**. After T001: Add guard proof tests in `tests/LamuFlix.IntegrationTests/TestEgressGuardTests.cs` covering (1) deliberate non-loopback failure naming the blocked host with recording inner handler zero forbidden sends, (2) loopback success, (3) an **explicit parameterized / table redirect proof** bounded to the test-project handler for relative and absolute `Location`, allowed hops, 301/302/303 POST→GET, 307/308 method preservation, same-origin `Authorization` retention (scheme + normalized host + effective port, including explicit default port), allowed changed-origin `Authorization` removal that explicitly covers same-host different effective port and permitted HTTP-to-HTTPS origin change, HTTPS-downgrade refusal with zero forbidden sends, named hop-limit failure, and redirect-to-real-host zero sends, (4) assertion that **actual** registered named **and** typed `IHttpClientFactory` clients from the T001 inventory include the guard, **and** (5) assertion that the direct-client helper path wires the same guard (FR-008–FR-010; SC-003; Q4, Q9; Step 6). Test files only — does not implement the handler in the same edit as T018 unless serialized by one owner.

### Implementation for User Story 4

- [ ] T018 [P] [US4] **Build lane**. After T001: Implement the single egress guard in `tests/LamuFlix.IntegrationTests/TestEgressGuardHandler.cs` and the shared helper in `GuardedHttpClientFactory.cs`: admit only `localhost` and `IPAddress.IsLoopback` literals without DNS; fail before send naming the host; disable auto-redirect; follow redirects through the bounded Q9 follower with method matrix (301/302/303 POST→GET; 307/308 preserve method); resolve relative/absolute `Location` and validate before each send; define origin as scheme + normalized host + effective port; retain `Authorization` on same origin (including explicit default port); remove `Authorization` on allowed changed origin, explicitly covering same-host different effective port and permitted HTTP-to-HTTPS origin change; refuse HTTPS downgrade; enforce `MaxRedirectHops = 5` and fail when exceeded (FR-009; Q4, Q9; Step 6). No code under `src/`.
- [ ] T019 [US4] **Build lane**. After T018: Wire the guard into every named/typed `IHttpClientFactory` client via the test host overrides and into directly constructed adapter clients via `GuardedHttpClientFactory.cs`, reconciling **each** site inventoried at T001 (including relevant portions of `OpenSubtitlesSubtitleProviderTests.cs` and existing test-host files) (FR-008; Q4; Step 6). No Evaluation.IntegrationTests or Tests.Common expansion.
- [ ] T020 [US4] **Build lane**. After T017–T019: Make all guard proofs pass, including factory named/typed and direct-client helper coverage reconciled to the T001 inventory and the full parameterized redirect proof set (SC-003).
- [ ] T021 [US4] **Gauge checkpoint**. After T020: Run static gates on changed `.cs` (`TestEgressGuardHandler.cs`, `GuardedHttpClientFactory.cs`, `TestEgressGuardTests.cs`, and any wiring files) and focused guard tests; record Done-when 3 receipt; keep SKIPPED/N/A/Could not run distinct from PASS.

**Checkpoint**: Guard Ready — Live exception may be added.

---

## Phase 6: User Story 5 - Live search smokes (Priority: P2)

**Goal**: Three search-only Live smokes with independent key skips and fresh disposable guarded clients.
**Independent Test**: Skip-without-keys for each provider; key present → bounded search pass (acceptance) on a fresh disposable client with immutable provider-host allowance; key unavailable → `needs-owner-run` once with keyed evidence open (disposition, distinct from acceptance); synchronized concurrent negative recording-transport proof (ordinary denies Live host; Live denies different external host; both zero forbidden sends; no credentials/network); CI workflow untouched; no key values in tracked files, fixtures, logs, output, or docs.
**Order**: After T020.

### Tests for User Story 5

- [ ] T022 [US5] **Build lane**. After T020: Add `Live`-category search-only smokes in `tests/LamuFlix.IntegrationTests/LiveProviderSearchTests.cs` for OMDb, TMDb, and OpenSubtitles. Each smoke **owns and disposes a fresh guarded client** with an **immutable** allowance for its provider host only — **no shared or ambient switch**. Each skips when its env var is absent (`LAMUFLIX_TEST_OMDB_API_KEY`, `LAMUFLIX_TEST_TMDB_API_KEY`, `LAMUFLIX_TEST_OPENSUBTITLES_API_KEY`), naming the variable in the skip reason. When the key is present, the bounded real search MUST pass (no `needs-owner-run` alternative in this acceptance case). OpenSubtitles uses API key only; no login, download, or quota-spending call. Include a **synchronized concurrent** negative recording-transport proof that (a) an ordinary client denies the Live host while the allowance is active and (b) the Live client denies a different external host, both with zero forbidden sends, without requiring credentials or network (FR-011; SC-004; Q4, Q5, Q8; Step 6). Not `[P]` with T023 (same Live surface).

### Implementation for User Story 5

- [ ] T023 [US5] **Build lane**. After T022: Observe independent skips without keys. With local keys present, run search-only passes for each provider (acceptance). If a key is unavailable on the seat, record `needs-owner-run` once with keyed evidence still open — disposition only, never a substitute pass and kept distinct from acceptance. Confirm `.github/workflows/ci.yml` is not modified, CI does not map `LAMUFLIX_TEST_*`, and no key values appear in tracked files, fixtures, logs, output, or docs (FR-011; SC-004).
- [ ] T024 [US5] **Gauge checkpoint**. After T023: Run static gates on changed `.cs` (`LiveProviderSearchTests.cs` and related); record Live skip receipt and keyed-pass or `needs-owner-run` disposition; keep SKIPPED/N/A/Could not run distinct from PASS.

**Checkpoint**: Done-when 4 evidence recorded honestly.

---

## Phase 7: User Story 6 - Ollama pin and operator docs (Priority: P2)

**Goal**: `0.40.13` alignment on named surfaces; README operable; real OllamaIntegration retained.
**Independent Test**: Five-surface pin agreement after README pin alignment; independent opt-in fixture green; README covers required operator topics including SC-007 walkthrough, shared infrastructure/state, and fixture-endpoint destinations.
**Order**: Pin alignment may start after T001 independently of fixture/guard lanes. Five-surface pin check is tasked **after** README pin alignment. Real opt-in run stays independent. Operator README completion after T015 (verified fake launches) and T023 (Live commands).

### Implementation for User Story 6

- [ ] T025 [P] [US6] **Build lane**. After T001: Align `.env.example` and `tests/LamuFlix.IntegrationTests/OllamaFixture.cs` to Ollama `0.40.13` (tag/digest consistent with existing `docker-compose.yml` and home overlay). Update the README Ollama pin mention in the same pin-alignment pass. Do not edit Compose pin lines, `deploy/k8s/overlays/home`, or `deploy/k8s/base` (FR-012; Q6). After README pin alignment, record the explicit five-surface tag/digest comparison receipt (compose, home overlay, `.env.example`, README, `OllamaFixture.cs`) as the SC-006 five-surface half. Does not edit fixture/guard production paths.
- [ ] T026 [P] [US6] **Build lane**. After T025: Run real opt-in `OllamaIntegration` locally on the updated pin and record the receipt independently of Compose/AppHost/Live lanes (SC-006 opt-in half; Q6).
- [ ] T027 [US6] **Build lane**. After T015, T023, and T026: Complete `README.md` operator docs for fixture locations, recording/scrubbing with a test key (variable names only; note existing gitleaks/scrub before staging), Live and Ollama opt-in commands, both fake launch commands (explicit fake-only Compose targets), shared Compose infrastructure/state and the fake/live host-port constraint, and that outbound provider destinations target the named fixture endpoint; include an operator walkthrough against SC-007 (FR-012–FR-013; SC-006–SC-007; Q6–Q7; Step 6). Pin lines already aligned in T025 — do not regress them.

**Checkpoint**: Done-when 6 + Wanted 7 docs complete.

---

## Phase 8: Polish & cross-cutting (delivery evidence)

**Purpose**: Same-head gates (including applicable web checks alongside e2e), ArchitectureTests, format, mutation disposition, hosted gitleaks, Size L review under closing bar. Final same-head receipts after any refactor edits before review.
**Order**: After delivery stories intended for the PR.

- [ ] T028 **Gauge**. After delivery code complete: Run applicable same-head C# Roslyn, cyclomatic complexity (incl. refactor threshold from `harness.yml`), InspectCode, `dotnet format --verify-no-changes`, full `dotnet test` including ArchitectureTests, **AppHost-only production C# mutation expected N/A** under current `harness.yml` explicit `LamuFlix.AppHost` exclusion — verify the actual harness exit on the delivery head (never fold N/A into PASS), **hosted gitleaks receipt** on the delivery head for scrubbed fixtures (existing `.github/workflows/gitleaks.yml`; no new tooling), **applicable web checks alongside e2e**. Record PASSED / FAILED / SKIPPED (scope-empty) / N/A / Could not run honestly — never as PASS when not earned (`brief.md` Q8; plan Principle II N/A distinct from PASS). Confirm ADR 0051 remains proposed until Build and that T001’s Wisp number-availability receipt is current before publication/merge.
- [ ] T029 **Cog / Build lane**. After T028 green or dispositioned: Apply refactor-gate cleanup only if complexity/duplication findings exist on changed `.cs`; re-run affected gates. After any refactor edits, capture **final same-head receipts** again before review (T030 does not start on stale receipts).
- [ ] T030 **Size L review**. After T028–T029 final same-head receipts: Require Sentry, Ledger, and Compass under the closing bar in `brief.md` Q8 (Critical/High/Medium + acceptance/safety/constitution defects block; every Low dispositioned in `CONCLUSIONS.md` as `fixed` or `noted, no ticket`). Charter filing bar for out-of-scope follow-ups. Attach each required Done-when / acceptance receipt (e2e with providers on + unsupported-matcher, both fake launches with PR log evidence + Compose-preflight-only fail-closed for missing bind/empty fixture set/invalid endpoint + AppHost independent fail-closed when fixture resource or endpoint wiring cannot resolve + default-profile Compose config comparison, guard full parameterized proof set including changed-origin same-host different effective port and permitted HTTP-to-HTTPS plus inventory-reconciled direct-client helper, Live skip + keyed pass on fresh disposable clients or `needs-owner-run` disposition distinct from acceptance + synchronized concurrent allowance proof, OMDb same-file zero-copy + credential exclusions + gitleaks, Ollama five-surface pin + independent opt-in, SC-007 README walkthrough, AppHost mutation N/A verification, hosted gitleaks receipt) to the delivery PR narrative.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work.
- **Phase 1 (T001)**: Blocks production edits.
- **Phase 2 (US1)**: After T001 — MVP shared OMDb + loader; T003 → T004 → T002 → T005 (no T002||T003).
- **Phase 3 (US2)**: After T004 — needs shared loader; T008 → T007/T009 → T010; unlocks Phase 4 rollout (T012–T015).
- **Phase 4 (US3)**: T012–T015 after T010 (enabled-provider e2e proven); FR-007 Compose setup on T012/T013.
- **Phase 5 (US4)**: After T001; independently runnable with 2–4; blocks Phase 6.
- **Phase 6 (US5)**: After T020.
- **Phase 7 (US6)**: Pin (T025) after T001; five-surface SC-006 receipt after README pin alignment inside T025; opt-in (T026) independent after T025; README completion (T027) after T015 + T023 + T026.
- **Phase 8**: After delivery stories; T030 after final same-head receipts from T028–T029.

### User Story Dependencies

- **US1**: No dependency on other stories after foundation/pickup.
- **US2**: Depends on US1 loader (T004); e2e verification precedes US3 rollout.
- **US3**: Settings + Compose/AppHost rollout (T012–T015) after T010.
- **US4**: Independent of fixture stories after T001; required before US5.
- **US5**: Depends on US4.
- **US6**: Pin independent after T001; docs depend on verified US3/US5 commands and T026 opt-in receipt.

### Parallel Opportunities

- Do **not** advertise T002||T003 or T007||T008 — excluding `stub.mjs` from the test tasks still leaves overlapping WireMock response/consumer edits; serialize those pairs.
- T012 after T010 || T014 after T010 (different files: settings JSON vs AppHost); T013 after T012.
- T017 || T018 after T001 (serialize if one owner edits the same new test/handler file) — guard lane stays separate from fixture serialization.
- T025 || fixture/guard work after T001; T026 after T025 may run parallel with unrelated lanes — pin lane stays separate.
- US4 may proceed in parallel with US1–US3 once T001 completes.
- Do **not** mark T012–T014 parallel with unfinished T010.

---

## Parallel Example: Guard + OMDb (after T001)

```text
# After T001 — guard lane || OMDb migrate (disjoint primary write sets):
Task: "T003 migrate OMDb into tests/fakes/omdb/"
Task: "T018 implement TestEgressGuardHandler.cs + GuardedHttpClientFactory.cs"
Task: "T017 add TestEgressGuardTests.cs parameterized redirect + inventory proofs"

# Serial on WireMock response/consumer surface (not T002||T003):
Task: "T004 teach LoopbackWireMock + stub.mjs shared loader with credential exclusions"  # after T003
Task: "T002 IntegrationTests prove WireMock OMDb load + alternate synthetic credential"  # after T004
```

## Parallel Example: Fake-mode after e2e (after T010)

```text
# After T010 (e2e proven) — not after T004 alone:
Task: "T012 add docker/api.settings.fakes.json (FR-007 Compose setup)"
Task: "T014 AppHost UseServiceFakes wiring"  # || T012; different files

# After T012:
Task: "T013 Compose fakes profile + stub.mjs preflight + fake-API readiness (FR-007)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. T000 → T001
2. Phase 2 US1 (shared OMDb + loader + same-file proof; T003 → T004 → T002 → T005)
3. **STOP and VALIDATE**: SC-005 / FR-002–FR-003

### Incremental Delivery

1. US1 loader → US2 enabled-provider e2e → US3 fake launches (T012–T015 after T010)
2. US4 guard (parallel after pickup; separate lane) → US5 Live
3. US6 pin lane (SC-006 five-surface in T025; independent opt-in T026) + README completion
4. Phase 8 gates, final same-head receipts, Size L review

### Notes

- `[P]` tasks = different files, no incomplete deps; never advertise `[P]` when WireMock response/consumer write sets can overlap.
- Delivery commits follow Build lane rules (`DEV-549 - {subject}`).
- Do not claim Gate 1, plan freeze, or delivery PASS from planning alone.
- Preserve recon provenance (Wanted / Done when / Where-it-lands) and the operator-facing Nothing-visible / documented-dev-fake-mode outcome already in `spec.md`.
- T000/T001 and all lane dependencies, gate receipts, FR/SC links, and numbering are preserved.
