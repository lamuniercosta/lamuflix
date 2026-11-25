---
description: "Task list for DEV-547 first release v0.1.0 (CHANGELOG and running-app version)"
---

# Tasks: First release v0.1.0 (CHANGELOG and running-app version)

**Input**: Design documents from `specs/DEV-547/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q6), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `contracts/version-endpoint.md`; Maestri notes `DEV-547`, `recon-DEV-547`; `specs/PRODUCT.md`.

**Status** — PLAN FROZEN after Phase 2 step 6 on 2026-11-24. Gate 1 / T000, Build, and live release acceptance remain pending.

**Tests**: Required (`brief.md` Test strategy / Q5; FR-001–FR-011; SC-001–SC-006; SMOKE-001–002). TDD optional. Use existing integration and web test homes. No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/release tasks are never `[P]`.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief Approach (including the P2.6-1 generator-only four-file set); STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no GHCR `v0.1.0` image tags; no gate changes (DEV-546); no hand-edits to generated OpenAPI/TS/MSW/Verify snapshot; never lower harness thresholds; no explanatory comments on changed C# beyond AGENTS exemptions; keep Dockerfile/YAML/Compose free of narrative comments by default (brief decision 2). Otherwise-unassigned implementation tasks are owned by the generic Build lane. Named seats own Spec PR, pickup, refactor, gates, review, ship, and release evidence per `task-pipeline`.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: After plan challenge clearance and Patron `gate1: provisional` (if applicable), Bernstein coordinates; Rigger prepares/opens the Spec Kit PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T001+
Phase 1   T001          (Wisp pickup: DEV-546 + Q3 location + path inventory; Keel reanalysis) — blocks production edits
Phase 2   T002–T004 → T005 preflight (focused version HTTP checks, architecture, static gates)
Phase 3   T006 → T005 closure → T007 (OpenAPI/Verify → complete integration suite → types/MSW) — blocks web display
Phase 4   T008 → T010   (US2 CI/Docker/promote/Compose/k8s metadata) — parallel with Phases 2–3 after T001
Phase 5   T011 → T012   (US4 CHANGELOG + README) — parallel after T001
Phase 6   T013 → T015   (US3 web version line + state tests + SMOKE) — after Phase 3 + T001 location
Phase 7   T016 → T018   (Cog refactor + Gauge pre-merge matrix + Size M review) — after delivery stories
Phase 8   T019 → T021   (US5 post-merge tag + readback + Rigger evidence) — after user merge of delivery PR
```

Ordering per brief Task ordering / Q4–Q5. No dependency cycle. Live release expectations remain open until Phase 8 — do not claim them from planning.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [X] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-547 Spec Kit PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-547 in `specs/DEV-547/CONCLUSIONS.md`. Receipt only. T001–T021 MUST NOT proceed without verified merge (`brief.md` Task ordering; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon and Q3 location selection (blocks production edits)

**Purpose**: Confirm DEV-546 prerequisite (**required before all Build work**, not only tag-run gate work), select one display location with Q3, inventory exact paths. Read-only.
**Checkpoint**: Written pickup receipt cites DEV-546 state, Q3 location choice, and authorized file paths; Keel read-only reanalysis clear. Any authorized drift amendment is recorded as a **separate follow-up** and completed before production edits — T001 itself does not edit production files or amend tasks/spec/plan.

- [X] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree confirming DEV-546 landed (required before Build / production edits per `brief.md` Task ordering step 1), recording whether a DEV-560 status page is present for Q3, and selecting exactly one display location (status page if landed, otherwise `web/src/App.tsx` footer). Inventory exact paths for `ApiEndpoints.cs` (**existing mapper signature** for `.MapVersionEndpoints()`), Dockerfiles, `ci.yml` `container-build`/`publish-images`, `promote-home.yml`, home overlay/base env sites, `docker-compose.yml`, and the P2.6-1 generator-only envelope evidenced at `recon-DEV-547:83–90`: `web/src/api/openapi.json`, `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`, `web/src/api/types.ts`, `web/src/mocks/handlers.ts`, plus opt-in `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` / `LAMUFLIX_REGENERATE_OPENAPI=true`, `DriftMatchesCommittedBaseline`, and `npm run generate:api` / `npm run check:api` (npx-free). Record the `WebApplicationFactory` **host-fixture seam** (peer: `HealthEndpointTests`, `recon-DEV-547:14`). Read the concrete **parity comparison** logic in `scripts/Test-DeployParity.ps1` and **mutation applicability/exclusions** on pickup HEAD; record anticipated verdicts and evidence; **STOP before affected Build work** if a protected or outside-envelope change would be needed (cited Patron ruling first — brief decision 2). Also inventory the chosen UI file and the colocated Vitest/RTL test path for T013. **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. If authorized demonstrated drift needs artifact amendment, record a separate follow-up and complete that amendment before T002+ production edits — do not fold silent production edits into T001. **STOP** `needs recon:` on missing seams; **STOP** for cited Patron ruling rather than expanding file/protected surface (`brief.md` Q3–Q4; decisions 1–2, 7; Task ordering step 1). **Strictly read-only reconnaissance and analysis**.

**Checkpoint**: T002+ unlocks only with a passing Wisp receipt (including parity/mutation preflight and P2.6-1 path check), Keel pickup analysis clear, and any authorized drift follow-up amendments already recorded and applied.

---

## Phase 2: User Story 1 - Version endpoint reports release and commit (Priority: P1) 🎯 MVP

**Goal**: Dedicated `GET /api/version` returns `{ release, commit }` with Q2 normalization.
**Independent Test**: Integration coverage green for FR-001–FR-003 / SC-001.
**Order**: T002–T004 and T005 preflight after T001. T006 runs after preflight and before T005 closure (Phase B ruling).

### Tests for User Story 1

- [X] T002 [P] [US1] **Build lane**. After T001: Add `tests/LamuFlix.IntegrationTests/VersionEndpointTests.cs` using the constitution `WebApplicationFactory` integration convention and the host-fixture seam recorded by T001 (peer `HealthEndpointTests`, `recon-DEV-547:14`). Place env-mutating cases in a **dedicated nonparallel xUnit collection**; create a **fresh host per metadata case** after setting both keys; **save both prior values and restore them in `finally`**, including host-start and assertion failures. Cover through HTTP: configured full SHA, absent release (JSON `null`), configured tag, empty, whitespace-only, `"unknown"` local fallback, and P2.6-2 override cases (absent runtime assignment preserves baked value; explicit nonblank operator value; explicit empty/whitespace operator value → `"unknown"`). Use repo Method_Condition_Expected naming and AAA headers (`// arrange` / `// act` / `// assert`) (FR-001–FR-003; SC-001; brief decisions 1, 6; `contracts/version-endpoint.md`; constitution PR **849–850**).

### Implementation for User Story 1

- [X] T003 [P] [US1] **Build lane**. After T001: Add sealed `src/LamuFlix.Api/Endpoints/VersionDto.cs` with `Release` (`string?`) and `Commit` (`string`) matching the frozen contract (`contracts/version-endpoint.md`; FR-001; brief decision 1).
- [X] T004 [US1] **Build lane**. After T002–T003 are authored: Add `src/LamuFlix.Api/Endpoints/VersionEndpoints.cs` implementing `GET /api/version` returning `TypedResults.Ok(...)` (constitution PR **849**). Capture effective process metadata and normalize it **once per host when mapping the route** (`LAMUFLIX_COMMIT` / `LAMUFLIX_RELEASE` as process/deployment metadata — not `IConfiguration["…"]` magic strings, not Options, and not an additional layer; constitution PR **855–857**; brief decision 1). Apply Q2 / P2.6-2 normalization (`"unknown"` / JSON `null`; operator override precedence). No application/domain use case / `AddHandler` required (Principle II N/A). Wire by adding `.MapVersionEndpoints()` to the existing chain in `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs` (signature confirmed at T001). Make T002 pass (FR-001–FR-003). Add the version path, registered endpoint name, and HTTP operation to the three authorized expected inventories in `ApiHostCompositionTests.cs`, `LibraryEndpointsTests.cs`, and `OpenApiContractTests.cs` respectively (Phase B ruling). Preserve all exact-set checks and existing metadata assertions.
- [X] T005 [US1] **Gauge checkpoint**. After T004 and green T002: Run and record **`LamuFlix.ArchitectureTests`** (constitution PR **840** — required green, not optional) and the three static gates on this phase’s changed `.cs` (`scripts/run-roslyn-analyzers.ps1`, `scripts/run-cyclomatic-complexity.ps1`, `scripts/run-jetbrains-inspectcode.ps1`, no `-All`). This preflight also requires `VersionEndpointTests` to pass. After T006, with the regeneration switch cleared, run the **complete integration project** as the isolation check and record the verdict (brief decision 1; Phase B ruling). Backend C# tasks clear only after ArchitectureTests, the three static gates, and the full-integration isolation check pass.

**Checkpoint**: Green T002–T004 and T005 preflight unlock T006 only. T005 closes when the complete integration project passes after T006.

---

## Phase 3: Contract chain regeneration (blocks web display)

**Goal**: Committed OpenAPI baseline, Verify drift snapshot, generated TypeScript client, and generated MSW handlers expose `GET /api/version` — each by its constitution role, in separate steps.
**Independent Test**: OpenAPI/web contract checks show route and schema; generated types consumed by later web tasks; `check:api` green.
**Order**: T005 preflight → T006 → T005 closure → T007. Keep the two generation steps separate (Phase B ruling).

- [X] T006 [US1] **Build lane**. After green T002–T004 and T005 preflight: **OpenAPI baseline + Verify snapshot only (P2.6-1).** Run the opt-in integration test `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` with `LAMUFLIX_REGENERATE_OPENAPI=true` scoped to that invocation so it writes `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together. Then run `DriftMatchesCommittedBaseline` with the switch cleared. Compare the generated baseline and Verify changes with pickup HEAD; **STOP on unrelated drift before T005 closure or T007** and resolve through this reviewed generation flow before continuing (brief decision 5). Review the authorized diff for `GET /api/version` schema/type shape only — nullable `release`, string `commit`. **Never hand-edit** either file. Confirm paths/invocation against T001 / `recon-DEV-547:83–90` (FR-007; constitution PR **967–969**; `contracts/version-endpoint.md`; P2.6-1).
- [X] T007 [US1] **Build lane**. After T006 and T005 closure: **Generated client + MSW only (separate from T006; P2.6-1).** From `web/`, run `npm run generate:api` (`node scripts/generate-api.mjs`, npx-free) producing exactly `web/src/api/types.ts` and `web/src/mocks/handlers.ts` from the committed `web/src/api/openapi.json`. **Do not** hand-edit those generated outputs; **do not** authorize `web/src/test/*` or other hand-maintained fixture/handler edits. Run `npm run check:api` and require green. The four P2.6-1 outputs remain the only files authorized for generator changes (FR-007; Q5; constitution PR **967–969**; `recon-DEV-547:83–90`).

**Checkpoint**: Web types and MSW handlers available from generation only; display work may begin.

---

## Phase 4: User Story 2 - Commit and release metadata reach images and pods (Priority: P1)

**Goal**: Exact source SHA baked into both images; release injected at promote; Compose parity with empty release.
**Independent Test**: CI build-arg checks, tagged/manual render checks, offline `Test-DeployParity.ps1` green.
**Order**: T008–T010 after T001; may run in parallel with Phases 2–3 (different files).

- [X] T008 [P] [US2] **Build lane**. After T001: Update `docker/Dockerfile.api` and `docker/Dockerfile.web` to accept build arg `LAMUFLIX_COMMIT` and persist `ENV LAMUFLIX_COMMIT` in the final images (defaulting only so unconfigured local builds report `unknown` per Q2). Update `.github/workflows/ci.yml` jobs `container-build` and `publish-images` to pass the exact full source SHA as that build arg to both Dockerfiles. DEV-547 configures commit **only** via these build args and final image ENV — no second variable (FR-004; Wanted 3; P2.6-2). Keep delivery config free of narrative comments (brief decision 2).
- [X] T009 [P] [US2] **Build lane**. After T001: Update `.github/workflows/promote-home.yml` and the relevant manifests under `deploy/k8s/base` and `deploy/k8s/overlays/home` so safely serialized `LAMUFLIX_RELEASE` is rendered into api and web pods from the same `RELEASE_TAG` used for `RELEASE.json` (empty on manual promotion), **omitting any runtime `LAMUFLIX_COMMIT` assignment**, and without changing existing digest pinning behaviour (FR-005; Wanted 4; P2.6-2; brief decision 4). Keep delivery config free of narrative comments.
- [X] T010 [US2] **Build lane**. After T008–T009: Update `docker-compose.yml` for the Q2 / P2.6-2 contract (commit via build args only; **omit runtime `LAMUFLIX_COMMIT`**; empty release). **Execute and record actual evidence** for the Phase-4 slice of the Q5 delivery matrix (not expectation-only; brief decisions 2, 6): (1) both CI jobs `container-build` and `publish-images` pass the full exact source SHA as build input to both Dockerfiles and both final images persist that SHA as ENV; (2) release injection into both api and web pods from the safely serialized manifest `RELEASE_TAG` for a tagged case and for a manual/empty case; (3) digest pinning remains unchanged, Compose/promotion omit runtime `LAMUFLIX_COMMIT`, and P2.6-2 cases are evidenced (absent runtime assignment preserves baked value; explicit nonblank operator value; explicit empty/whitespace operator value → `"unknown"`); (4) run applicable parity (and any other applicable Phase-4 gates) and record the **actual** offline `scripts/Test-DeployParity.ps1` verdict against T001’s anticipated verdict. Hosted CI `deploy-parity` and mutation **actual** verdicts are completed/recorded in T017 (FR-004–FR-006; SC-002; Done when 3; Q5).

**Checkpoint**: US2 independently verifiable via recorded build-input / tagged / manual / P2.6-2 / digest-preservation evidence + actual offline parity verdict.

---

## Phase 5: User Story 4 - Owner-facing CHANGELOG and README link (Priority: P2)

**Goal**: Keep a Changelog file + one-line README link.
**Independent Test**: File exists with required sections/topics; README has one new link line.
**Order**: T011–T012 after T001; parallel with Phases 2–4.

- [X] T011 [P] [US4] **Build lane**. After T001: Create root `CHANGELOG.md` in Keep a Changelog format with `Unreleased` and `v0.1.0`. Author the `v0.1.0` entry for the home-cluster owner from the merged source for the frozen topic list (library browse and filters, movie details, watchlist, OMDb and TMDb enrichment, recommendations, in-browser playback, subtitles, LocalPlay, and home deployment). Parallel with other delivery stories is allowed; **before pre-merge gates**, verify the entry against merged behaviour and correct any drift (FR-009; Wanted 2; brief decision 5). Do not invent behaviour that is not in merged code.
- [X] T012 [P] [US4] **Build lane**. After T001: Add exactly one new line in `README.md` linking to `CHANGELOG.md` (FR-009; Done when 1). Parallel with T011; subject to the same pre-gate validation stop if the link drifts.

**Checkpoint**: US4 independently reviewable in the delivery diff after pre-gate merged-behaviour validation.

---

## Phase 6: User Story 3 - App shows one version line (Priority: P1)

**Goal**: Single Q3-selected UI location consumes generated types and renders the version line.
**Independent Test**: Web state tests + SMOKE-001–002.
**Order**: T013–T015 after T007 and T001 location choice.

### Tests for User Story 3

- [ ] T013 [P] [US3] **Build lane**. After T007: At Phase 6 start, **recheck Q3 drift** — keep the pickup-selected location; if DEV-560 landed later, report through the drift path before any placement change; verify uniqueness across both surfaces when present (brief decision 3). Add/extend Vitest/RTL tests in the **authorized conditional test area** from `plan.md` (colocated `*.test.ts(x)` beside the T001-selected UI file under `web/src/`). Drive states through **generated MSW** (`web/src/mocks/handlers.ts`) with a deterministic, explicitly controlled response release for the loading case. Cover: tagged display `v0.1.0 (########)` (SHA shortened only in UI; release opaque); successful null-release short-SHA `dev (########)`; successful null-release unknown `dev (unknown)` (`"unknown"` rendered verbatim); **in-flight omission under hold** (separate); **failed-response omission** after controlled failure (separate); and **one-location across shell and status page** when both exist (FR-008; SC-003; Q3; SMOKE alignment; brief decision 3). Exact test path comes from T001 pickup when not already evidenced.

### Implementation for User Story 3

- [ ] T014 [US3] **Build lane**. After T013 authored and T007 complete: Implement the version line in the single Q3-selected file (DEV-560 status page path recorded by T001, or `web/src/App.tsx` footer). Consume generated `GET /api/version` types only; format `<release|dev> (<first-8-SHA-chars|unknown>)`; **render `"unknown"` verbatim** and shorten a full SHA only in the UI; render release as opaque text; omit while loading or on failure (FR-007–FR-008; `ASSUMPTIONS.md`; brief decision 3). Make T013 pass.
- [ ] T015 [US3] **Build lane**. After T014: **Run** the mandatory Compose smoke scenarios SMOKE-001 (configured short-SHA and unconfigured `dev (unknown)` cases) and SMOKE-002 against the selected location. For SMOKE-002, **hold the browser’s version request open** until the selected UI is observed, then release it or return a controlled failure with bounded cleanup. **Record** the interception/hold method, absence during the hold, and the expected settled result for each scenario (successful response displays the line; separate failed response leaves it absent). **Record** either (a) **pass evidence** for each scenario — only (a) satisfies the Phase 6 **delivery checkpoint** — or (b) an **explicit smoke blocker** naming the failed/blocked scenario and reason. A recorded smoke blocker is **not** a passed delivery checkpoint and does **not** unlock Phase 7 as delivery-ready. Unspecified disposition does **not** satisfy closure (`spec.md` Smoke Test; SC-003; brief decision 3).

**Checkpoint**: US3 independently testable via web state tests. Phase 6 **delivery checkpoint** passes only with SMOKE-001/002 **pass** evidence; a recorded smoke blocker keeps Phase 6 delivery-incomplete and is distinct from a passed checkpoint.

---

## Phase 7: Polish — refactor, pre-merge gates, Size M review

**Purpose**: Cross-cutting quality bar before delivery PR merge.
**Order**: After Phases 2–6 delivery tasks intended for the PR are complete with Phase 6 smoke **pass** evidence (a recorded smoke blocker is not a passed delivery checkpoint and does not satisfy this order gate).

- [ ] T016 **Cog**. After delivery stories for the PR: Run `/refactor` on changed C# using the **configured refactor threshold from `harness.yml`** (do not hardcode the numeric threshold); no behaviour change outside authorized envelope; apply the existing AGENTS comment rule to C# and delivery-configuration edits (brief decision 2).
- [ ] T017 **Gauge**. After T016: Run the brief Q5 pre-merge matrix and **record actual honest evidence/verdicts for every lane** — not expectation-only (brief decision 2). Required delivery-path evidence: (1) both CI jobs `container-build` and `publish-images` × both Dockerfiles receive the full exact source SHA and persist it as final-image ENV; (2) safely serialized release injection into both pods from manifest `RELEASE_TAG` for tagged and manual/empty cases with no runtime `LAMUFLIX_COMMIT`; (3) digest pinning unchanged and P2.6-2 baked-commit preservation evidenced; (4) **actual** offline `scripts/Test-DeployParity.ps1` verdict and **actual** hosted CI `deploy-parity` job verdict (compare to T001 anticipated). Also: `dotnet test` (including ArchitectureTests and the complete integration project isolation check); Roslyn; cyclomatic complexity; InspectCode; property tests per harness; vulnerable packages; `dotnet format --verify-no-changes`; mutation whenever production C# under `src/` changed — record the **actual** script outcome; a changed project with no eligible mutation tests and no exclusion is **FAILED** if the script returns exit 1; `scripts/run-web-gates.ps1` when `/web` changed; `check:api`. Confirm T011 changelog was validated against merged behaviour before these gates. Never fold SKIPPED/N/A/Could not run into PASS; never lower thresholds; preserve exit labels (FR-010; Q5; SC-002).
- [ ] T018 **Sentry / Ledger / Compass → Keel**. After T017: Size M three-axis review (Risk, Standards, Spec) with explicit disposition for every finding. Remediate accepted in-scope Critical/High/Medium/Low within two rounds; each accepted fix MUST be **independently verified on the reviewed head** before that finding is closed. Apply brief Q6 follow-up/disposition rules before closure: route out-of-scope Critical/High, broken behaviour, or user-requested items via Rigger (fold into existing tickets first); record other out-of-scope items as `noted, no ticket` in `CONCLUSIONS.md`. At the round cap, report remaining blockers and the affected head to Bernstein — do not close review with unverified fixes (`brief.md` Q6; SC-006).

**Checkpoint**: Delivery head ready for user merge of the Build PR (not the live tag).

---

## Phase 8: User Story 5 - First `v0.1.0` tag and deployment readback (Priority: P2)

**Goal**: Authorized post-merge release and evidence comment.
**Independent Test**: Tag run, GitHub Release, `RELEASE.json`, port 3100, Rigger comment.
**Order**: Only after user merge of the delivery PR and DEV-546 on `main`.

- [ ] T019 [US5] After both DEV-546 and DEV-547 are on `main`: keep the **exact nightly SHA** requirement. If the initially considered head has no qualifying green nightly, **wait for an eligible `main` commit** that still contains both tickets and has existing green nightly evidence on that exact SHA. The **Conductor identifies the authorized release operator**. Before pushing, that operator checks the recorded nightly SHA, the local proposed tag target, and remote tag state. Then push tag `v0.1.0` on that same SHA only. If the tag is already bound to the wrong SHA or a mismatch is found after pushing, **halt and escalate** before further release action (FR-011; Q4; Wanted 6; brief decision 4). Do not push earlier. Tag acceptance remains post-merge only.
- [ ] T020 [US5] After T019: Read back and **tie to the T019 target SHA** the green tag-triggered `promote-home` run, GitHub Release existence, and `RELEASE.json` on `env/home` with `"release": "v0.1.0"`, that same target commit, and deployed image digests — using the **same safely serialized release value** as the manifests (FR-011; Done when 4–6; SC-005; brief decision 4). Record the SHA linkage across nightly → tag → Release → promotion.
- [ ] T021 [US5] After T020: Verify `http://127.0.0.1:3100` shows `v0.1.0` and the matching short commit for the T019 target SHA. **Distinguish** an unavailable port-forward or readback harness from app behaviour — record a harness blocker separately, but the required live port-3100 receipt still **blocks ticket closure**. **Rigger** posts the tag-run link and version-line screenshot to DEV-547 before closure (FR-011; Done when 7–8; SC-005; brief decision 4).

**Checkpoint**: Ticket Done when 4–8 satisfied for the captured eligible target SHA; no GHCR `v0.1.0` image tagging (FR-012).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0**: Blocks all Build work
- **Phase 1**: Read-only pickup; DEV-546 required before Build; blocks production edits; supplies Q3 location + colocated test path + host-fixture/mapper signature + P2.6-1 path check + parity/mutation preflight; drift amendments are separate follow-ups before T002+
- **Phase 2**: T002 isolation + P2.6-2 override cases; T003 sealed DTO; T004 mapping, once-per-host capture and exact inventories; T005 preflight unlocks T006, then the complete integration suite closes T005
- **Phase 3**: T006 opt-in export/Verify then stop on unrelated drift; after T005 also passes, T007 npm generate:api (types + handlers.ts) + check:api; blocks Phase 6 web wiring
- **Phases 4–5**: After Phase 1; parallel with Phases 2–3; T010 records build-input/tagged/manual/P2.6-2/actual-offline-parity evidence; T011 parallel docs with pre-gate merged-behaviour validation
- **Phase 6**: After Phase 3 (+ Phase 1 location); Q3 drift recheck; T013 generated-MSW deterministic holds; T015 hold-method/absence/settled evidence (delivery checkpoint) or explicit smoke blocker
- **Phase 7**: After PR-scoped delivery stories with Phase 6 smoke **pass** (blocker ≠ passed checkpoint); T016 harness.yml refactor threshold + comment rule; T017 records full Q5 **actual** evidence including hosted deploy-parity and mutation FAILED-if-applicable; T018 verifies fixes on reviewed head
- **Phase 8**: After user merge of delivery PR + DEV-546 on `main` + eligible green nightly on exact SHA; T019 pre-push SHA/tag-state checks with halt/escalate; T020/T021 tie nightly/tag/Release/promote and distinguish harness blockers while requiring port-3100 receipt

### User Story Dependencies

- **US1 (P1)**: After Phase 1 — MVP contract
- **US2 (P1)**: After Phase 1 — independent of US1 files
- **US3 (P1)**: After US1 contract regeneration + Phase 1 location
- **US4 (P2)**: After Phase 1 — independent docs
- **US5 (P2)**: After merged delivery containing US1–US4 as applicable + DEV-546

### Parallel Opportunities

- T002/T003 after T001
- T008/T009 after T001 (parallel with Phase 2)
- T011/T012 after T001 (parallel with Phase 2–4)
- T013 after T007 (parallel with remaining US2/US4 polish if any)

---

## Parallel Example: After pickup (T001)

```text
Task: "VersionEndpointTests.cs (T002)"
Task: "VersionDto.cs (T003)"
Task: "Dockerfile + ci.yml commit bake (T008)"
Task: "promote-home + k8s release env (T009)"
Task: "CHANGELOG.md (T011)"
Task: "README.md one-line link (T012)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Gate 1 and pickup, then the version endpoint, focused tests and T005 preflight.
2. Run T006 and verify the baseline/snapshot changes. Close T005 with the complete integration suite.
3. Continue through T007 to web work. Delivery metadata and docs may proceed in parallel as defined above.

### Incremental Delivery

1. US1 endpoint → contract regen → US3 display
2. US2 metadata path + parity in parallel
3. US4 changelog/README in parallel
4. Pre-merge gates/review → user merges delivery PR
5. US5 tag + readback + Rigger evidence

---

## Notes

- [P] tasks = different files, no incomplete deps
- [Story] label maps task to US1–US5 for traceability
- Quill-named sealed `VersionDto` / `VersionEndpoints` (`.MapVersionEndpoints()`) / `VersionEndpointTests` are within ticket-owned Api/IntegrationTests areas; alternatives may be adjusted only for authorized demonstrated drift at pickup
- P2.6-1 four-file generator-only envelope and P2.6-2 commit precedence are Patron-approved; Gate 1 remains pending; freeze disposition remains Keel’s
- Record the verified Gate 1 merge at T000 and live release acceptance at T019–T021.
