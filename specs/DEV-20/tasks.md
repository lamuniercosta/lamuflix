# Tasks: DEV-20 Web Scaffold

**Input**: `specs/DEV-20/spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/msw-harness.md`.

**Tests**: Included — required by spec FR-007 (web-implement red-green). Write tests first; ensure FAIL before implementation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 readiness screen, US2 MSW harness, US3 toolchain/CI

---

## Phase 1: Setup — Toolchain, configs, lockfile

**Purpose**: Package manifest, Node pin, TS/bundler/lint configs, committed lockfile.

- [X] T001 [US3] Create `web/package.json` with frozen 24 direct packages at exact stable versions (React 19.x, Tailwind 4.x), `engines.node >=24 <25`, and scripts `dev`/`build`/`lint`/`format`/`test`/`test:run`/`test:watch` per spec FR-004/FR-008.
- [X] T002 [US3] Create `web/.node-version` containing `24` (spec FR-008).
- [X] T003 [P] [US3] Create `web/tsconfig.json` with `strict: true` directly covering `src` app and tests including setup/app/tests; `vite/client` via `types`, no generated `vite-env.d.ts` (spec FR-005).
- [X] T004 [P] [US3] Create `web/tsconfig.node.json` covering `vite.config.ts` tooling (spec FR-005).
- [X] T005 [P] [US3] Create `web/vite.config.ts` with React plugin, Tailwind Vite integration, Vitest jsdom + shared setup reference and `globals: false` (spec FR-001/FR-006).
- [X] T006 [P] [US3] Create `web/index.html` entrypoint host (no template logos/counters).
- [X] T007 [P] [US3] Create `web/eslint.config.js` (flat TS/React rules + `eslint-config-prettier`), `web/.prettierrc.json`, `web/.prettierignore` (ignore install/build/coverage outputs + lockfile) (brief Commands).
- [X] T008 [US3] Run `npm install` in `web/` to generate committed `web/package-lock.json`; verify `npm ci` resolves without `--force`/`--legacy-peer-deps` (spec SC-003).

**Checkpoint**: Toolchain installs reproducibly. Strict `src` coverage is proven later by the T009 fail/restore probe (Phase 5), not by this checkpoint: a bare check passing here with no `src` sources present proves nothing.

---

## Phase 2: Foundational — Shared test harness shell (no product code)

**Purpose**: Setup/servers every story test depends on. No user-story assertion work starts until complete.

- [X] T010 [P] Create `web/src/test/setup.ts` importing `@testing-library/jest-dom/vitest` with explicit RTL cleanup (spec FR-006).
- [X] T011 Create `web/src/test/server.ts` as a shell/lifecycle module only: MSW node server with `onUnhandledFrame: error`, listen/reset/close lifecycle with explicit `vitest` lifecycle imports, and no handlers (not even defaults); synthetic default + override handlers arrive in T017 (contracts/msw-harness.md describes that final state).

**Checkpoint**: Harness shell ready; US1/US2 tests can now be written against it.

---

## Phase 3: User Story 1 — Readiness screen (Priority: P1) 🎯 MVP

**Goal**: Minimal `LamuFlix` screen proving React + Tailwind.

**Independent Test**: `npm test` passes the role-based screen test with no network; `npm run build` emits Tailwind-compiled output.

### Tests for User Story 1 (write FIRST, ensure FAIL)

- [X] T012 [US1] Write `web/src/App.test.tsx` with explicit `vitest` imports for all used test APIs: render through accessible roles (`main`, `heading`/`LamuFlix`, paragraph `Web client is ready.`); assert Tailwind utility classes present; confirm FAIL with no `App.tsx` (spec FR-007).

### Implementation for User Story 1

- [X] T013 [US1] Create `web/src/App.tsx` (single `main`, `h1 LamuFlix`, `p Web client is ready.`, Tailwind utilities; no fetch/routes/links/buttons) (spec FR-002).
- [X] T014 [US1] Create `web/src/index.css` (Tailwind import) and `web/src/main.tsx` (plain React DOM entrypoint rendering `App`) (spec FR-003).
- [X] T015 [US1] Re-run `npm test` and `npm run build`; record receipts including an emitted CSS selector for a Tailwind utility actually used by the readiness screen (DOM classes/build exit alone do not satisfy this) (spec SC-001 subset).

**Checkpoint**: US1 fully functional and testable independently.

---

## Phase 4: User Story 2 — MSW interception + isolation (Priority: P2)

**Goal**: Synthetic-URL interception and handler reset proof; App stays fetch-free.

**Independent Test**: Two-test override/reset sequence passes; unhandled request errors.

### Tests for User Story 2 (write FIRST, ensure FAIL)

- [X] T016 [US2] Write `web/src/test/msw.test.ts` with explicit `vitest` imports for all used test/lifecycle APIs: fetch synthetic URL returns default handler; per-test override applies once; next test observes reset defaults; suite wires server lifecycle (spec FR-006/FR-007, contracts/msw-harness.md); run the suite and confirm the interception/isolation assertions FAIL while `server.ts` is still the handler-free T011 shell, before T017 adds any handler.

### Implementation for User Story 2

- [X] T017 [US2] Implement synthetic handlers in `web/src/test/server.ts` (default + overridable, test-only URLs; no product handlers, no browser worker) (spec FR-006/FR-011).
- [X] T018 [US2] Re-run `npm test`; record receipt; confirm App test unaffected by MSW (spec SC-001 subset).

**Checkpoint**: US1 and US2 both work independently.

---

## Phase 5: User Story 3 — CI, gate flag, ignores, README (Priority: P3)

**Goal**: Independent frontend CI lane and delivery-scope existing-file edits.

- [X] T019 [US3] Edit `.github/workflows/ci.yml`: add independent frontend job (`setup-node@v4`, `node-version-file web/.node-version`, npm cache on `web/package-lock.json`, ordered `npm ci`, `npm run lint`, `npx tsc --noEmit`, `npm test`, `npm run build` from `web/`); retain .NET job and `contents: read` (spec FR-009).
- [X] T020 [US3] Edit `harness.yml`: `gates.web.enabled` false to true (same delivery PR as `web/package.json`) (spec FR-010).
- [X] T021 [US3] Edit `.gitignore`: append only `web/node_modules/`, `web/dist/`, `web/coverage/` (spec FR-010).
- [X] T022 [P] [US3] Create `web/README.md` (Node 24 pin, `npm ci`, command list) (spec FR-010).
- [X] T009 [US3] Run the deliberate type-error fail/restore probe from `web/` with app and test sources present: introduce a temporary type error in `src/`, run bare `npx tsc --noEmit` and confirm FAIL, restore, re-run and confirm PASS; record both receipts; probe is never committed (spec SC-002; brief Test strategy; CONCLUSIONS Q3).
- [ ] T023 [US3] Run full Gauge verification from clean tree: fresh `npm ci`, `npm run lint`, bare `npx tsc --noEmit`, `npm test`, `npm run test:run`, `npm run build`, `scripts/run-web-gates.ps1` — all exit 0; record receipts including `npm --version` output, the committed `package-lock.json` `lockfileVersion`, and the hosted CI npm-version receipt when available (no CI sequence change, no npm pin); run non-gating `npm audit --audit-level=high` and record the receipt with no CI/gate change — a runtime (non-dev) High/Critical advisory is adjudicated under the Q5 closing bar, dev-only or unrunnable audit output is recorded honestly nonblocking and never PASS; retain backend gates (C# analyzer and mutation scope-empty exits are SKIPPED (scope-empty), never PASS; property gate exit 2 is recorded as opt-out note `propertyTests: opt-out - no domain invariants introduced`); vulnerable-package gate, `dotnet format`, and `dotnet test` remain under pipeline rules; enabled web gate must execute, not SKIP (spec SC-001/SC-004; brief CI and gate expectations).

**Checkpoint**: All user stories independently functional; CI lane proven. T009 receipts prove the bare check is non-vacuous (FAIL with the error, PASS without); a bare-check PASS recorded with no `src` input or with no FAIL counterpart does not satisfy this checkpoint.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Spec-scope hygiene only; frozen scope admits nothing else.

- [ ] T024 [P] Confirm no delivery file outside the frozen set changed: 18 new files under `web/` + existing-file edits to `ci.yml`/`harness.yml`/`.gitignore` only; no `run-web-gates.ps1`, `.editorconfig`, solution/project, backend, or other workflow edits; record exact package-identity equality of the delivered 24 direct ids against the Q2 approved list (count alone does not satisfy this). This check covers delivery production files only: `specs/**` planning artifacts (this spec PR) and the approved `ci.yml` frontend-job addition are in scope by ruling and are not violations (CONCLUSIONS Q4/Q5; brief Planned delivery file set). PR boundary: the Phase 1 `.specify/feature.json` pin remains uncommitted local workflow metadata and is excluded from staging in BOTH PRs; it is not a fourth supporting delivery edit.
- [ ] T025 [P] Confirm scope exclusions hold: no routes, API calls, generated types/snapshots, product MSW handlers, browser worker, hand-rolled API types, migrations, playback/secrets/`Process.Start`.
- [ ] T026 Verify per-phase commits `DEV-20 - <phase title>` exist before next phase, per task-pipeline; record per-phase commit ids/subjects and ordering as a receipt.

---

## Dependencies & Execution Order

- Phase 0 pickup (prerequisite, no task ID): Wisp drift recon on the named worktree, then Keel pickup analysis, before any build task (brief Task ordering and checkpoints).
- Phase 1 -> Phase 2 -> Phases 3/4 (US1 then US2 sequentially; US2 tests need the T011 handler-free harness shell) -> Phase 5 -> Phase 6.
- US3 toolchain tasks (T001-T008) precede everything; T009 probe runs in Phase 5 after T018 (app + test sources exist) and before T023 verification/commit; US3 integration tasks (T019-T023) close delivery.
- Tests T012/T016 written FIRST and FAIL before T013-T014/T017; T016 FAIL is observed against the handler-free T011 shell.
- Parallel: T003-T007, T010, T022, T024-T025 (disjoint files).

## Implementation Strategy

1. Phase 0 pickup: Wisp recon + Keel analysis (prerequisite to build).
2. Phase 1 + Phase 2 (foundation ready: toolchain + handler-free harness shell).
3. US1 (MVP): T012 fail -> T013-T014 -> T015 validate; STOP and validate independently.
4. US2: T016 fail (against T011 shell) -> T017 handlers -> T018 validate.
5. US3 integration (T019-T022), then T009 fail/restore probe, then Gauge verification (T023); polish (T024-T026).

## Loop Discipline and Owner Boundary (frozen; brief Loop discipline and owner boundary)

- Closing bar: Critical and High findings block; Medium and Low are recorded, nonblocking within frozen scope. An applicable gate failure, Could not run, or missing required axis blocks. Scope-empty/configured skips and the accepted property opt-out follow the pipeline exactly and are never PASS.
- Review round cap: two. Remediation: at most two fix commits per round. Third-round findings go to owner review with the PR, not another automatic loop.
- Hosted CI/gitleaks and the three review axes stay required at their later stages; no gates run and no merge bar closes in this spec work.
- Owner boundary: Gate 1 is the owner-merged spec PR (`specs/**` only); nobody merges, enables auto-merge, or starts delivery from the brief alone. Skip honesty (no gate is skipped to look green) is preserved. Follow-ups get tickets only for Critical/High, broken behaviour, or user-requested work; otherwise `noted, no ticket`.
