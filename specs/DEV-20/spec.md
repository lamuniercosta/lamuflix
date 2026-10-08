# Feature Specification: DEV-20 Web Scaffold (web/)

**Feature Branch**: `feature/020-spec`

**Created**: 2026-10-07

**Status**: Draft — gate1: provisional

**Input**: brief.md (frozen 2026-10-07, Keel/Patron Q1-Q5), CONCLUSIONS.md, ASSUMPTIONS.md, specs/PRODUCT.md; recon-DEV-20; ticket DEV-20:18-32.

## User Scenarios & Testing

### User Story 1 - Readiness screen proves React + Tailwind (Priority: P1)

A developer opening the scaffolded web client sees a `LamuFlix` heading and `Web client is ready.` paragraph in a main landmark, styled with Tailwind utilities.

**Why this priority**: Ticket acceptance is a working toolchain plus a minimal screen proving React and Tailwind render (brief Outcome; CONCLUSIONS Q1).

**Independent Test**: Render `src/App.tsx` with React Testing Library via accessible roles (`main`, `heading`); assert copy exactly. Passes with no MSW or network.

**Acceptance Scenarios**:

1. **Given** a fresh `npm ci`, **When** `npm test` runs, **Then** the readiness-screen test passes.
2. **Given** `npm run build`, **When** Vite emits `web/dist/`, **Then** emitted CSS contains a selector for a Tailwind utility actually used by the readiness screen (DOM classes/build exit alone are insufficient).
3. **Given** the App, **When** rendered, **Then** no product routes, API calls, links, buttons, or placeholder text exist (Q1 boundary).

---

### User Story 2 - Synthetic MSW harness proves interception and isolation (Priority: P2)

A developer running tests gets an MSW node server lifecycle (listen with `onUnhandledFrame: error`, reset after each, close after all) that intercepts a fetch to a synthetic test-only URL and proves handler reset/isolation between tests.

**Why this priority**: Proves the ticket-named MSW tooling runs without mocking any product API contract (brief Test strategy; CONCLUSIONS Q3).

**Independent Test**: Fetch a synthetic non-product URL in two tests; first test overrides the handler once, second test observes the default handler again. Both pass; any unhandled request errors.

**Acceptance Scenarios**:

1. **Given** the harness, **When** a test fetches the synthetic URL, **Then** the MSW handler responds and no live server is contacted.
2. **Given** a per-test handler override, **When** the next test runs, **Then** handlers have reset to defaults.
3. **Given** a request to any other URL, **When** unhandled, **Then** MSW raises an error.

---

### User Story 3 - Reproducible toolchain, scripts, and CI (Priority: P3)

A developer or CI agent checks out the branch, runs `npm ci`, and executes lint, typecheck, test, and build with exit 0; an independent frontend CI job does the same on every push.

**Why this priority**: Ticket requires `npm run build` and `npm test` to terminate successfully locally and in CI (DEV-20:18-32; brief CI section).

**Independent Test**: From a clean checkout: `npm ci`, `npm run lint`, `npx tsc --noEmit`, `npm test`, `npm run test:run`, `npm run build` all exit 0. The CI frontend job runs the frozen five-command sequence (`npm ci`, `npm run lint`, `npx tsc --noEmit`, `npm test`, `npm run build`); `npm run test:run` is an additional local/Gauge receipt, not a CI step.

**Acceptance Scenarios**:

1. **Given** Node 24 with `web/.node-version`, **When** CI runs `setup-node@v4` with npm cache keyed by `web/package-lock.json`, **Then** `npm ci` resolves without `--force`/`--legacy-peer-deps`.
2. **Given** an intentional temporary `src` type error, **When** bare `npx tsc --noEmit` runs from `web/`, **Then** it fails; after restore it passes (probe recorded, never committed).
3. **Given** the delivery PR, **When** merged, **Then** `harness.yml` `gates.web.enabled` is `true` and the web gate executes its steps (disabled SKIP is not proof).

### Edge Cases

- `web/` must not be confused with legacy `src/LamuFlix.Web` (recon-DEV-20:29).
- Root `tsconfig.json` with `files: []` references-only is rejected: bare `npx tsc --noEmit` must directly check `src` app and tests with `strict: true`.
- MSW synthetic URLs only; no `public/mockServiceWorker.js` browser worker, no product fetch in `App`, no generated API types or OpenAPI snapshots.
- No prerelease versions; React/react-dom stay 19.x, Tailwind plus `@tailwindcss/vite` stay 4.x; npm version is not pinned.
- Existing `.editorconfig` unchanged; only `ci.yml` (new frontend job), `harness.yml` (web flag), `.gitignore` (three scoped ignores) change among existing files.

## Requirements

### Functional Requirements

- **FR-001**: Scaffold MUST create root `web/` with React 19, Vite, strict TypeScript, Tailwind 4, ESLint, Prettier, Vitest, React Testing Library, jsdom, MSW.
- **FR-002**: `src/App.tsx` MUST render one `<main>` landmark with `<h1>` `LamuFlix` and `<p>` `Web client is ready.`, Tailwind utilities only.
- **FR-003**: `src/main.tsx` MUST be a plain React DOM entrypoint rendering `App`; no router, state, or form library.
- **FR-004**: Scripts MUST define `test`/`test:run` as `vitest run`, `test:watch` as `vitest`, `build` as `tsc --noEmit && tsc --noEmit -p tsconfig.node.json && vite build`, `lint` as `eslint . --max-warnings 0 && prettier --check .`, `format` as Prettier write, `dev` as Vite dev.
- **FR-005**: Root `tsconfig.json` MUST strictly check `src` app and tests directly (`strict: true`), including setup/app/tests; `tsconfig.node.json` MUST cover `vite.config.ts`; `vite/client` via `types`, no generated `vite-env.d.ts`.
- **FR-006**: Vitest MUST use jsdom with `globals: false`, shared setup (`src/test/setup.ts` imports `@testing-library/jest-dom/vitest`, retains explicit RTL cleanup), and explicit `vitest` imports for all used test/lifecycle APIs in app tests, harness, and lifecycle modules; MSW node server (`src/test/server.ts`) with `onUnhandledFrame: error`, reset/close lifecycle.
- **FR-007**: Tests MUST include role-based readiness test (`src/App.test.tsx`) and synthetic-URL interception/reset test (`src/test/msw.test.ts`); no snapshot-only tests, no coverage/mutation thresholds.
- **FR-008**: Toolchain MUST pin Node 24 in `web/.node-version`, `engines.node >=24 <25`; MUST commit `web/package-lock.json` with the 24 frozen direct packages at exact stable versions.
- **FR-009**: Delivery MUST add an independent frontend CI job (checkout, `setup-node@v4`, node-version-file, npm cache, ordered commands from `web/`); existing .NET job and `contents: read` unchanged.
- **FR-010**: Delivery MUST set `harness.yml` `gates.web.enabled` false to true in the same PR that adds `web/package.json`; MUST append only `web/node_modules/`, `web/dist/`, `web/coverage/` to `.gitignore`; MUST add `web/README.md` (Node pin, `npm ci`, commands).
- **FR-011**: Scope exclusions MUST hold: no product routes, API calls, client generation, OpenAPI snapshots, hand-rolled API types, product MSW handlers, browser worker, backend/legacy changes, migrations, playback execution, secrets, or `Process.Start`.

### Key Entities

- **Web scaffold**: the `web/` toolchain, configs, app, and test harness; attributes: package manifest, TS/Vite/ESLint/Prettier configs, entrypoint, screen, tests.
- **Synthetic MSW harness**: test-only interception proof; attributes: node server lifecycle, synthetic URL handlers, reset semantics. No product contract involved.
- **Frontend CI job**: independent verification lane; attributes: Node 24 pin, npm cache, ordered lint/typecheck/test/build steps.

## Success Criteria

- **SC-001**: Fresh `npm ci` plus `lint`, bare `tsc --noEmit`, `test`, `test:run`, `build`, and `scripts/run-web-gates.ps1` each exit 0, with receipts recorded.
- **SC-002**: Temporary `src` type error makes bare `npx tsc --noEmit` fail; restored tree passes; both receipts recorded, probe removed before commit.
- **SC-003**: `npm ci` resolves with no `--force` or `--legacy-peer-deps`; lockfile committed.
- **SC-004**: Enabled web gate executes its steps in Gauge verification; C#/mutation scope-empty results reported as SKIPPED (scope-empty), property gate exit 2 recorded as opt-out with note `propertyTests: opt-out - no domain invariants introduced`; none reported as PASS.

## Assumptions

- Taste copy from ASSUMPTIONS.md Q1 [assumed]: `main`/`h1`/`p` wording above, Tailwind utilities, no links or buttons (PRODUCT.md section 4).
- The 24-package direct allowlist in brief.md is frozen (CONCLUSIONS Q2); concrete exact patch versions are implementation selections, not additions.
- This spec PR changes only `specs/**`; scaffold and the three existing-file edits ship in the delivery PR. The Phase 1 `.specify/feature.json` pin remains uncommitted local workflow metadata, excluded from staging in BOTH PRs.
