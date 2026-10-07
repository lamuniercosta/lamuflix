# Implementation Plan: DEV-20 Web Scaffold

**Branch**: `feature/020-spec` | **Date**: 2026-10-07 | **Spec**: `specs/DEV-20/spec.md`

**Input**: `specs/DEV-20/spec.md`, frozen `brief.md`, `CONCLUSIONS.md` (Q1-Q5 rulings), `specs/PRODUCT.md`.

## Summary

Scaffold root `web/` with React 19 + Vite + strict TypeScript + Tailwind 4 + ESLint/Prettier + Vitest/RTL/jsdom/MSW harness, a minimal `LamuFlix` readiness screen, an independent frontend CI job, and the three approved existing-file edits. Plain React DOM entrypoint, no new layer; Vitest config in `vite.config.ts`; 24 frozen direct packages at exact stable versions with committed lockfile.

## Technical Context

**Language/Version**: TypeScript strict, Node 24 (`web/.node-version`; `engines.node >=24 <25`).

**Primary Dependencies**: react, react-dom (19.x); dev: vite, typescript, eslint, prettier, tailwindcss (4.x + @tailwindcss/vite), vitest, @testing-library/react, @testing-library/dom, @testing-library/jest-dom, jsdom, msw, @types/react, @types/react-dom, @types/node, @vitejs/plugin-react, @eslint/js, globals, typescript-eslint, eslint-plugin-react-hooks, eslint-plugin-react-refresh, eslint-config-prettier. 24 direct packages frozen (CONCLUSIONS Q2); exact stable patches selected at implementation; no prereleases; `npm ci` without `--force`/`--legacy-peer-deps`.

**Storage**: N/A (no database, entity, or migration work).

**Testing**: Vitest (`vitest run` for `test`/`test:run`; bare `vitest` for `test:watch`), jsdom, `globals: false` with explicit `vitest` imports for all used test/lifecycle APIs, shared setup (`src/test/setup.ts` imports `@testing-library/jest-dom/vitest`, retains explicit RTL cleanup), MSW node server (`onUnhandledRequest: error`, reset/close lifecycle), synthetic test-only URLs. Non-gating `npm audit --audit-level=high` receipt recorded at verification (no CI/gate change; runtime High/Critical adjudicated under Q5 bar, dev-only/unrunnable recorded honestly nonblocking, never PASS).

**Target Platform**: Local dev + GitHub Actions (`actions/setup-node@v4`, npm cache, `node-version-file web/.node-version`).

**Project Type**: Web scaffold (frontend-only toolchain ticket, no backend).

**Performance Goals**: N/A beyond fast single-shot test/build termination locally and in CI.

**Constraints**: Root `tsconfig.json` strictly checks `src` app/tests directly including setup/app/tests (no vacuous references-only check); `tsconfig.node.json` covers `vite.config.ts`; `vite/client` via `types`; Vitest `globals: false` with explicit `vitest` imports; shared setup imports `@testing-library/jest-dom/vitest` with explicit RTL cleanup; ESLint `--max-warnings 0` + `prettier --check`; Prettier/ESLint ignore install/build/coverage outputs; `.editorconfig` untouched.

**Scale/Scope**: 18 new files under `web/` + 3 existing-file edits (`ci.yml`, `harness.yml`, `.gitignore`); M size, no UI tag.

## Constitution Check

- Principle VII (Configuration Isolation and Deterministic Time): N/A — scaffold introduces no clock, randomness, options records, or secrets. No `TimeProvider` consumer exists to wire; no `DateTime.Now`-family call is added.
- Principle I (Ports and Adapters) + Principle II (Explicit Handlers, no MediatR): N/A beyond scope exclusion — no Core ports, handlers, decorators, or dispatch mechanisms introduced; plain React DOM entrypoint adds no architectural layer (PRODUCT.md section 2; CONCLUSIONS Q1/Q2).
- Principle III (Typed Query Model) + Principle IV (Enrichment State Machine) + Principle VI (Observability): N/A — no query model, enrichment transitions, or telemetry surface in this scaffold.
- Principle V (Validated Inputs, Consistent Errors) + API and Contract Rules: no endpoints, DTOs, status codes, or error shapes introduced. Contract chain preserved: no OpenAPI snapshots, no generated types, no hand-rolled API types; generation deferred to later API-client work (CONCLUSIONS Q1). Deferred under Q1 with no constitution departure: TanStack Query, Zod, openapi-typescript, openapi-fetch stay out of this scaffold.
- MSW synthetic test URLs (e.g. `https://test.local/*`) are test-harness interception proofs under contracts/msw-harness.md, not generated product handlers: they are hand-maintained test doubles for a synthetic URL with no product contract, while the constitution's API and Contract Rules generated-product-handler clause governs product MSW handlers generated from the committed OpenAPI document (CONCLUSIONS Q3). No constitution exception is granted or needed.
- Principle VIII (Ubiquitous Language): no new domain term introduced, so `CONTEXT.md` needs no change; identifiers stay English (CONCLUSIONS Q1).
- Principle IX (Test Pyramid) + Technology Stack Constraints (Web app row: Vite + React + TypeScript strict; Vitest + RTL + MSW): ticket-named Vitest + RTL + MSW only, no second assertion/mocking stack; exact-stack versions frozen by CONCLUSIONS Q2. Backend-only stack rows (.NET, Postgres, RabbitMQ, OMDb) and backend test tooling (xUnit, Testcontainers) are N/A — no backend code is touched.
- Pull Request Quality Gates + Static-Analysis Gates: `dotnet format`, C# analyzer, and complexity/InspectCode gates are backend-only and N/A to `web/` sources; gate honesty preserved per brief — scope-empty/configured skips are never PASS, no threshold reduction, no generated-file edits, vulnerable-package/`dotnet format`/`dotnet test` discipline stays under pipeline rules (brief CI and gate expectations; CONCLUSIONS Q5).

*Re-check after Phase 1 design: scope, allowlist, and existing-file set unchanged from Q1-Q5.*

## Project Structure

### Documentation (this feature)

```text
specs/DEV-20/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (no product API; harness contract only)
│   └── msw-harness.md
└── tasks.md             # Phase 2 output (created by /speckit-tasks)
```

### Source Code (repository root)

```text
web/
├── package.json
├── package-lock.json
├── .node-version
├── index.html
├── vite.config.ts
├── tsconfig.json
├── tsconfig.node.json
├── eslint.config.js
├── .prettierrc.json
├── .prettierignore
├── README.md
└── src/
    ├── main.tsx
    ├── App.tsx
    ├── index.css
    ├── App.test.tsx
    └── test/
        ├── setup.ts
        ├── server.ts
        └── msw.test.ts

.github/workflows/ci.yml   # + independent frontend job
harness.yml                 # gates.web.enabled false -> true
.gitignore                  # + web/node_modules/, web/dist/, web/coverage/
```

**Structure Decision**: Ticket-named root `web/`, distinct from legacy `src/LamuFlix.Web`; no new project, folder, or layer beyond it (Q1/Q2; PRODUCT.md sections 2-3).

## Phase B Pickup and Verification (frozen; brief Task ordering / CI and gate expectations / Loop discipline)

- Prerequisite: Phase B starts with Wisp drift recon on the named worktree, then Keel pickup analysis, before any build task. No packages or architecture are reselected.
- Gauge verification (tasks T009/T023): deliberate type-error FAIL then restored PASS receipts for bare `npx tsc --noEmit` from `web/`; fresh `npm ci`, `npm run lint`, bare typecheck, `npm test`, `npm run test:run`, `npm run build`, `scripts/run-web-gates.ps1` — all exit 0. Backend gates retained: C# analyzer and mutation scope-empty are SKIPPED (scope-empty), property exit 2 is opt-out note `propertyTests: opt-out - no domain invariants introduced`; vulnerable-package gate, `dotnet format`, and `dotnet test` remain under pipeline rules. Enabled web gate must execute, not SKIP.
- Loop discipline: Critical/High block; Medium/Low nonblocking within frozen scope; applicable gate failure, Could not run, or missing axis blocks. Two review rounds, at most two fix commits per round; third-round findings go to owner review with the PR.
- Owner boundary: Gate 1 is the owner-merged spec PR (`specs/**` only); nobody merges, enables auto-merge, or starts delivery from the brief alone. The Phase 1 `.specify/feature.json` pin remains uncommitted local workflow metadata, excluded from staging in BOTH PRs. Hosted CI/gitleaks and the three review axes stay required at their later stages.

## Complexity Tracking

No constitution violations to justify. No new project/layer, no repository/mediator wrapper, no migration, no API shape change.
