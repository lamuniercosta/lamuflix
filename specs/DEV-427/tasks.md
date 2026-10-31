# Tasks: DEV-427 Manual movie-library load baseline

**Input**: Design documents from `specs/DEV-427/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` (plan records why).

**Tests**: No new C# or React tests, test infrastructure, or CI wiring. Verification remains real compose seed-twice, k6 preflight/response checks, actual measured baseline, bounded negatives, Rigger p95 read-back, and applicable harness gate receipts. Do not claim acceptance from syntax checks or synthetic output. Manual/not-CI is a scope boundary for Principle IX's production test-layer table, not a substitute for those real-stack proofs.

**Organization**: Phases follow brief task ordering. Plan challenge verified and plan frozen on 2026-10-30 per brief Plan freeze receipt. Do not start implementation until the spec PR has merged and Gate 1 is established; no implementation authorization from this freeze. Exact `grafana/k6` `image@sha256:…` digest (tag label only), verified Docker network mapping, deterministic page-wrap, and concrete mixed-case probes are implementation selections recorded in README/evidence.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 seed fixture, US2 workload/preflight, US3 measured baseline/evidence, US4 negatives/gates.
- Exact file paths are in each description.

## Phase 1: Setup (scripts/load skeleton)

**Purpose**: Create the authorized directory and runbook shell so later tasks have stable paths. Blocks Phase 2+.

**Independent Test**: `scripts/load/` exists with a README stub that states Docker-only execution, no host k6/psql requirement, Finding 4 B disposable-DB provisioning outline, and that credentials must come only from existing env/private config.

**Checkpoint**: No tracked compose/CI/API/schema/manifest/harness file has changed. Unrelated `harness.yml` modification remains untouched.

- [X] T001 Create `scripts/load/` and add `scripts/load/README.md` outlining recon Finding 4 B prep: separately named disposable DB in existing compose postgres; credential-safe `docker compose exec -T postgres psql` creation (credentials only from existing env/private config — never CLI literals/history/logs); ignored operator-local `docker/api.settings.json` Database retarget with `ApplyMigrationsOnStartup=true`; `api` restart so existing committed migrations apply; same API/SQL target proof and empty-or-exact-seed-only guard before seed; temporary UI/API redirection notice; private original-settings preservation and mandatory restore plus `api` restart even on failure; digest-pinned `grafana/k6` (`image@sha256:…`) usage with `BASE_URL`; bind-mounted `artifacts/load/`; env-supplied run ID/UTC/HEAD inputs; repeat-seed assertions; evidence fields; bounded negatives. Do not add helper scripts, reuse DEV-334 runner, delete existing-library rows, author migrations/schema, or edit tracked compose/CI/API/schema/manifests/harness.

---

## Phase 2: Foundational - Idempotent seed (User Story 1)

**Purpose**: Deterministic 500-movie fixture on the existing `movies` table. Blocks workload and measurement stories.

**Goal**: US1 — isolated DB can be seeded twice to exactly 500 rows / 500 distinct seed paths without OMDb.

**Independent Test**: On the Finding 4 B disposable compose database (existing migrations applied via ignored settings + api restart), run `scripts/load/seed.sql` twice via in-container psql; after each run verify 500 total movies and 500 distinct `/dev427/movie-%.mp4` paths; confirm ten groups of 50 titles; restore original settings and restart api afterward.

### Implementation for User Story 1

- [X] T002 [US1] Implement `scripts/load/seed.sql` for `i=1..500` with `NNNN` four-digit and `GG` two-digit `(i-1) mod 10`, inserting `library_path=/dev427/movie-NNNN.mp4`, `title=DEV427 GroupGG MovieNNNN`, `format=mp4`, `is_in_watchlist=false`, `status=1`, `enrichment_attempts=0`, using `ON CONFLICT (library_path) DO NOTHING`. Supply required non-default columns; rely on existing DB defaults otherwise. No OMDb columns/calls. No NEW/authored migration or schema definition. No explanatory SQL comments (rationale in README). Prefer a transactional seed wrapper where practical. Depends on T001.
- [X] T003 [US1] Update `scripts/load/README.md` with the full Finding 4 B sequence (create disposable DB, ignored settings retarget, ApplyMigrationsOnStartup, api restart, pre-insert project/container/database identity + same API/SQL target + empty-or-exact-seed-only guard, temporary UI/API notice, private preserve/restore/restart even on failure), the exact credential-safe seed command (env/private config only; redacted receipt examples), transactional seed note, the two verification queries/assertions (500 rows, 500 distinct seed paths, 50 per group), and the rule that a contaminated library is not an acceptance baseline. Depends on T002.

**Checkpoint**: Seed-twice receipts can be produced without any k6 run.

---

## Phase 3: User Story 2 - Workload, preflight, and checks (Priority: P1)

**Goal**: Deterministic 80/20 browse/search k6 script with preflight against the exact fixture.

**Independent Test**: With US1 fixture present and nginx reachable, run preflight (or equivalent readiness path) and confirm pass on correct fixture / fail on wrong counts or contract mismatch. Steady 120s measurement is not required yet.

### Implementation for User Story 2

- [X] T004 [US2] Implement `scripts/load/movies.js` VU loop: four browse requests cycling pages 1–25 with explicit `pageSize=20`, then one search rotating Group00–Group09 including mixed-case probes with `page=1` and `pageSize=20`; sleep 1s after each request; deterministic counters only (no ambient clock or Math.random for fixture/request selection); omit `sort` and `direction` (superseded by the Patron Phase 3 scope ruling of 2026-10-31 in `CONCLUSIONS.md`: send explicit `sort=Title&direction=Ascending`); support `BASE_URL` defaulting to `http://localhost:3000`. No explanatory JS comments (rationale in README). Record chosen deterministic page-wrap and concrete mixed-case probe spellings in README/evidence; FR-005/FR-006 unchanged. Follow `.agents/skills/k6-load-testing/SKILL.md` as guidance only — ticket metrics and manual-only scope prevail. Depends on T002.
- [X] T005 [US2] Add preflight and response checks in `scripts/load/movies.js` for HTTP 200, JSON `items`/`totalCount`, browse `totalCount=500` with 20 unique IDs per page, search `totalCount=50` with 20 matching-title items, and one failure count per request for transport/non-200/invalid JSON/contract/seed mismatch. Do not add empty-result search variants or extra filters. Depends on T004.
- [X] T006 [US2] Document in `scripts/load/README.md` how the k6 container reaches compose nginx (verified Docker host/network mapping), `BASE_URL` override, that container localhost must not be assumed to reach the host, and the selected page-wrap / mixed-case probe spellings. Depends on T004.

**Checkpoint**: Preflight validates the US1 fixture through nginx without claiming a measured baseline.

---

## Phase 4: User Story 3 - Measured baseline and evidence (Priority: P1)

**Goal**: Tagged 50-VU / 120s steady run with ticket budgets and retained evidence; p95 routed via Rigger.

**Independent Test**: Run the measurement scenario once after preflight; retain summary JSON and operator receipt; judge thresholds; route first valid measured p95 to Rigger for ticket recording and verified read-back.

### Implementation for User Story 3

- [ ] T007 [US3] Extend `scripts/load/movies.js` with fixed 50-VU / exactly 120s steady measurement (no ramp in that window), measurement-scenario tag excluding setup/preflight/optional warm-up, blocking thresholds `p(95)<=200` and custom request-level error rate `<0.01` over combined browse+search, nonzero browse/search sample checks, and non-blocking per-type diagnostics. Depends on T005.
- [ ] T008 [US3] Write collision-safe per-run summary JSON under bind-mounted `artifacts/load/` from `scripts/load/movies.js`, including measured p95, custom error rate, browse/search sample counts, configured VUs/duration/target, threshold outcomes, explicit env-supplied run ID/UTC identity/git HEAD, and digest-pinned k6 image (`image@sha256:…`) selected during implementation. Depends on T007.
- [ ] T009 [US3] Complete `scripts/load/README.md` baseline/evidence instructions: full `image@sha256:…` digest (tag label only), artifact bind-mount, env-supplied run ID/UTC/HEAD command inputs, operator receipt fields (both seed counts, redacted command, process exit, environment/hardware, first valid measured p95 including budget-miss cases), and that runtime output stays untracked. Depends on T008.
- [ ] T010 [US3] Execute Finding 4 B end-to-end: create separately named disposable DB, retarget ignored `docker/api.settings.json` with ApplyMigrationsOnStartup=true, restart api, prove same API/SQL target and empty-or-exact-seed-only guard, seed twice retaining both redacted receipts (transactional where practical), verify nginx reachability, run preflight, run the actual 50-VU / 120s acceptance workload, retain output/threshold verdicts/command/exit/digest/env metadata, restore original settings and restart api even on failure, and route the first valid measured p95 plus supporting receipt to Rigger for YouTrack recording and verified read-back. Keel and Builder do not write YouTrack. Depends on T003, T009.

**Checkpoint**: A real measured run exists with evidence. Budget miss is FAILED with evidence, not a scope change.

---

## Phase 5: User Story 4 - Negatives and gate reporting (Priority: P2)

**Goal**: Fail-closed negatives and honest harness gate labels.

**Independent Test**: Unreachable target and invalid fixture each exit nonzero; empty metrics do not pass; applicable gate exits are recorded without renaming to PASS.

### Implementation for User Story 4

- [ ] T011 [US4] Exercise bounded negatives using `scripts/load/movies.js` / runbook paths: unreachable target exits nonzero; invalid fixture exits nonzero; empty metrics or missing browse/search traffic do not yield a passing acceptance result. Record outcomes in the operator evidence. Do not commit broken fixtures. Depends on T009.
- [ ] T012 [US4] Run applicable harness gates for this no-production-C# change and record script verdicts. Report empty C# mutation scope as `SKIPPED (scope-empty)`, disabled gates as `SKIP`, unreachable runtime as `Could not run`, and budget miss as `FAILED`. Never report those as PASS. Do not substitute `dotnet build` or unrelated suites for load acceptance. Depends on T010.

**Checkpoint**: False-green paths are closed; gate vocabulary matches AGENTS.md / brief Gate expectations.

---

## Phase 6: Polish & review closeout

**Purpose**: Frozen closing bar under the two-round cap. User owns merging.

- [ ] T013 Complete applicable code-review / ship-review under Critical/High/Medium blocking bar with explicit Low dispositions and size-M Sentry/Ledger/Compass axis reports. Round cap is two. Exhaustion blocks via Conductor; do not add rounds or relax thresholds. Depends on T011, T012.

---

## Excluded (never scheduled)

Tracked compose, CI, API, schema, manifest, generated-contract, or harness configuration edits; NEW/authored migrations or schema definitions; production NuGet/npm dependencies; `Features:LocalPlay`; secrets or credential literals in source/CLI/history/logs; `Process.Start`; DEV-334 Playwright or runner reuse; existing-library deletion; pre-baseline budget tightening; endpoint/index/architecture changes after a budget miss; CI enrollment for this load suite; `CONTEXT.md` / ADR authorship; new helper scripts beyond the three authorized tracked files; explanatory comments in `movies.js`/`seed.sql`; example-driven k6 deps/budgets/CI from skill docs; unrelated cleanup; claiming PASS from SKIPPED/SKIP/Could not run/FAILED or from syntax/synthetic output. A discovered need outside this list stops for a decision. It is not absorbed into a phase.

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start after Gate 1.
- **Foundational / US1 (Phase 2)**: Depends on Setup — BLOCKS US2–US4 measurement paths that need the fixture.
- **US2 (Phase 3)**: Depends on Phase 2 seed existing; preflight can be proven before full measurement.
- **US3 (Phase 4)**: Depends on Phases 2–3.
- **US4 (Phase 5)**: Depends on Phase 4 artifacts/commands existing.
- **Polish (Phase 6)**: Depends on Phases 4–5 evidence and negatives.

### User Story Dependencies

- **User Story 1 (P1)**: After Setup — no dependency on k6.
- **User Story 2 (P1)**: After US1 fixture — independently testable via preflight.
- **User Story 3 (P1)**: After US1+US2 — measured baseline and evidence.
- **User Story 4 (P2)**: After US3 commands exist — negatives and gate reporting.

### Parallel Opportunities

- T001 is sequential setup.
- After T002, README seed docs (T003) can proceed while k6 loop work (T004) starts in parallel on a different file.
- T006 (network mapping docs) can proceed in parallel with T005 once T004 exists.
- T011 negatives should not run in parallel with the acceptance measurement retention step T010 on the same environment without isolation.

### Parallel Example: After seed SQL exists

```text
Task: "Update scripts/load/README.md seed commands and assertions (T003)"
Task: "Implement scripts/load/movies.js VU loop (T004)"
```

## Implementation Strategy

### MVP First (User Stories 1–2)

1. Complete Phase 1 setup
2. Complete Phase 2 seed + seed-twice proof
3. Complete Phase 3 workload/preflight
4. **STOP and VALIDATE**: Preflight passes on the isolated 500-movie fixture

### Incremental Delivery

1. Setup + seed → fixture proof
2. Add workload/preflight → request-contract proof
3. Add measured baseline + Rigger p95 → ticket evidence
4. Add negatives + gate reporting → fail-closed closeout
5. Review under frozen bar → user merges

## Notes

- [P] tasks = different files, no unfinished dependency
- [Story] label maps task to US1–US4 in `spec.md`
- Acceptance reminder: no explanatory comments in `movies.js` or `seed.sql`; rationale lives in README prose only
- Exact `grafana/k6` `image@sha256:…` digest (tag label only), verified network mapping, deterministic page-wrap, and concrete mixed-case probes are recorded at implementation time in evidence/README, not invented in this task list
- Cite `.agents/skills/k6-load-testing/SKILL.md` as guidance only; ticket/frozen metrics and manual-only scope prevail
- Additive README + exactly three tracked implementation files remain (Q5; Q9 L2 clarification)
- Preserve unrelated `harness.yml` modification
- Status corrections pending Keel verification; do not declare freeze/Gate 1 or start implementation from this list
- Commit message format when committing: `DEV-427 - {subject}`
