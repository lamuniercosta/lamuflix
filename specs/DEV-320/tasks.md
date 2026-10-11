# Tasks: DEV-320 Typed API Client and MSW Handlers

**Input**: Design documents from `specs/DEV-320/` (spec.md, plan.md, closed brief.md Q1-Q12, CONCLUSIONS.md, shared note recon-DEV-320).

**Prerequisites**: plan.md (required), spec.md (required). No research.md / data-model.md / contracts/ — the committed `web/src/api/openapi.json` plus `recon-DEV-320` lines 31-78 are the contract evidence.

**Tests**: Included — Q9 proof strategy requires them (client, handlers, policy, startup, generator tests).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths in descriptions. Single writer per file at a time (Q10).

## Phase 1: Contract tooling, types, client (US1) — MVP

**Goal**: Generated `types.ts`, thin typed `client.ts`, `check:api` drift for types active, compile + runtime proof green.

**Independent Test**: `web/src/api/client.test.ts` passes on phase-1 local typed `server.use` overrides; `tsc --noEmit` passes over `client.typecheck.ts`; `npm run check:api` passes for `types.ts`; web gate green.

- [ ] T001 [US1] Pin `openapi-typescript@6.7.6` as generator devDependency and `openapi-fetch@0.9.8` as runtime client in `web/package.json` and update `web/package-lock.json` via plain install (no `npm audit fix`, no overrides, no `--force`, no `--legacy-peer-deps`; only nodes in the two pins' dependency closure may be added/changed, with no existing lockfile node changing version). Per Patron conditional clearance as amended by Decision A (shared DEV-320:39-41, addendum :40 (39a); brief:129-135; CONCLUSIONS:253-258): T001 owns the actual added count, package paths, and pins-closure reconciliation from the real lockfile diff; the 29-package count is scratch-expected (recon:115), not a real-lockfile invariant. (1) `generate:api` and `check:api` pass the generator only the fixed committed file path `web/src/api/openapi.json`, never a URL, glob, or caller-supplied source. Qualified one-pass evidence only: observed scratch install, two byte-identical generations and corrected harness tsc validation over all nine operations (recon-DEV-320:137-139; CONCLUSIONS:200-208); no general TS 6 support, runtime fetch/MSW behavior, generated-file lint/format compliance, multi-status narrowing, or affected-code unreachability claim (recon:140-141); scratch six-node and omit-dev-zero observations are not real-lockfile facts or future passes. Any other High, any existing-node version change (including baseline `undici@8.11.2`), or any pins-closure deviation returns to Patron with evidence. Full ruling and advisory record in plan.md section 15.
- [ ] T002 [US1] Create `web/scripts/generate-api.mjs` (plain Node + installed openapi-typescript; deterministic LF output with do-not-edit header; uses only the fixed committed `web/src/api/openapi.json` with no caller-supplied source, URL, or glob — output-temp selection only; writes `web/src/api/types.ts` in phase 1, extended to `web/src/mocks/handlers.ts` in phase 2)
- [ ] T003 [US1] Create `web/scripts/check-api.mjs` (passes the generator only the fixed committed `web/src/api/openapi.json` with no caller-supplied source, URL, or glob — output-temp selection only; generates to temp, byte-compares LF output, fails drift, never overwrites committed files) and wire `web/package.json` scripts `generate:api` + `check:api`, with `lint` running `check:api` before eslint/prettier
- [ ] T004 [US1] Generate committed `web/src/api/types.ts` via `generate:api`; add its `web/eslint.config.js` + `web/.prettierignore` entries in this phase
- [ ] T005 [US1] Create `web/src/api/client.ts` (explicit-`baseUrl` factory + default client over `VITE_API_BASE_URL`/browser origin; bodyless 202/204, generated ProblemDetails, unions/nullability preserved; Location read from raw response headers)
- [ ] T006 [P] [US1] Create `web/src/api/client.test.ts` (all nine operations, path/query/body serialization, success/error payloads, raw Location, bodyless 202/204, explicit + default origins; uses local typed overrides until phase 2)
- [ ] T007 [P] [US1] Create compile-only `web/src/api/client.typecheck.ts` (one correctly typed call per operation incl. id-union distinctions + `@ts-expect-error` negatives; never runtime-imported; inside tsconfig `include src`)
- [ ] T008 [US1] Type optional `VITE_API_BASE_URL` in `web/src/vite-env.d.ts` and add one `web/README.md` line (no committed `.env*`); run per-phase web gate (lint incl. check:api, tsc, test:run, build) green

**Checkpoint**: US1 independently functional — typed client proven; types drift active; no unchecked generated file landed.

---

## Phase 2: Fixtures, generated handlers, global lifecycle (US2)

**Goal**: Authored fixtures, generated handlers under the same drift check, sole lifecycle owner, full scenario proof.

**Independent Test**: `web/src/mocks/handlers.test.ts` (incl. T012-owned Node strict unhandled-`/api` rejection proof after the T011 sole lifecycle, proving failure without real-network reach), `web/src/mocks/generate-api.test.ts` (incl. T013 input-source regression), full Vitest with `msw.test.ts` unedited, `check:api` covering types + handlers, web gate green.

- [ ] T009 [US2] Create `web/src/mocks/fixtures.ts` (immutable generated-type data/scenario selection only: static browse page, complete + null-metadata details variants, genres/people arrays, opaque 202 Location fixture under existing route)
- [ ] T010 [US2] Extend `web/scripts/generate-api.mjs` + `check:api` to `web/src/mocks/handlers.ts` (one handler per operation, wildcard origin, deterministic generated header) and add its eslint/prettier ignore entries
- [ ] T011 [US2] Edit `web/src/test/server.ts` (keep harness handler, add generated `/api` handlers, export server) and `web/src/test/setup.ts` (sole `listen`/`resetHandlers`/`close` + RTL cleanup, registered once); `msw.test.ts` passes unedited
- [ ] T012 [US2] Create `web/src/mocks/handlers.test.ts` after T011 establishes the sole `setup.ts` lifecycle (every operation success + every declared error status/content-type per Q4 list, nullable fixture, override/reset isolation; T012 owns the Node strict unhandled-`/api` rejection proof using the exported server under that configured lifecycle to prove an unhandled `/api` request fails without reaching the real network; no new test file, no `msw.test.ts` edit, no duplicated lifecycle hooks, no test logic in `server.ts`)
- [ ] T013 [P] [US2] Create `web/src/mocks/generate-api.test.ts` (deterministic LF output, exact nine-operation coverage, stale-temp rejection — all preserved — plus input-source regression coverage proving the scripts use only the fixed committed `web/src/api/openapi.json` with no caller-supplied source, URL, or glob); create `web/src/mocks/generate-api.d.ts` only if the compiler requires script declarations (never API types)
- [ ] T014 [US2] Run per-phase web gate green with handlers drift active

**Checkpoint**: US1 + US2 work — global generated mocks with strict isolation and joint drift proof.

---

## Phase 3: Policy, browser worker, startup (US3)

**Goal**: Strict worker policy, opt-in startup, official worker asset, bootstrap proof.

**Independent Test**: `web/src/mocks/policy.test.ts` (`/api`, `/api/x.json` rejected; `/apiculture`, asset path, `ws` frame bypassed; worker-policy proof separately owned) + T012-owned Node strict unhandled-`/api` rejection proof in `handlers.test.ts` (Phase 2, proving failure without real-network reach); `web/src/mocks/startup.test.ts` (DEV+flag on, defaults off, production off, visible failure); manual backend-stopped smoke recorded once in PR notes (supplementary).

- [ ] T015 [US3] Create `web/src/mocks/policy.ts` (worker-only `/api`-first `onUnhandledFrame` callback: `/api` or `/api/` prefix calls `defaults.error()` before any bypass; other HTTP and `ws` frames bypass; no `isCommonAssetRequest` needed)
- [ ] T016 [US3] Create `web/src/mocks/browser.ts` (setupWorker with generated handlers) and `web/src/mocks/startup.ts` (exported opt-in decision function: DEV && `VITE_ENABLE_MSW === 'true'`); edit `web/src/main.tsx` to await startup before `createRoot` with visible failure
- [ ] T017 [US3] Explicitly create `web/public` and run installed CLI from `web/` (`npx msw init ./public --save`); accept `package.json` `msw.workerDirectory`; never hand-edit `web/public/mockServiceWorker.js`; add worker ignore entries
- [ ] T018 [P] [US3] Create `web/src/mocks/policy.test.ts` (Q7 cases) and `web/src/mocks/startup.test.ts` (Vitest env stubbing + module mocks; startup-before-render, off defaults/production, visible failure)
- [ ] T019 [US3] Type optional `VITE_ENABLE_MSW` in `web/src/vite-env.d.ts`, document alongside `VITE_API_BASE_URL` in `web/README.md`; run per-phase web gate green

**Checkpoint**: US1-US3 work — backend-free browser development behind the opt-in flag with strict API behavior.

---

## Phase 4: Documentation and integration verification

**Purpose**: Final docs + full proof (no new behavior).

- [ ] T020 Document `generate:api`/`check:api` and backend-then-web regeneration order (`LAMUFLIX_REGENERATE_OPENAPI`, then `npm run generate:api`) in `web/README.md`
- [ ] T021 Run final integration verification: `check:api` + generator tests, `tsc --noEmit` incl. `client.typecheck.ts`, full Vitest incl. unchanged harness tests, lint/build, hosted CI + gitleaks/vulnerability checks; record fresh observed `npm audit` and `npm audit --omit=dev` commands, outputs, and exits against the actual manifest/lockfile, with no pre-stated pass, and record supplementary manual smoke result once in PR notes. Per Patron conditional clearance as amended by Decision A (shared DEV-320:39-41, addendum :40 (39a); brief:129-135; CONCLUSIONS:253-258): a High on exactly these six name@version nodes (`@fastify/busboy@2.1.1`, `braces@3.0.3`, `fast-glob@3.3.3`, `micromatch@4.0.8`, `openapi-typescript@6.7.6`, `undici@5.29.0`, regardless of hoisted/nested path) is known, not a new finding; any other High, any existing-node version change (including baseline `undici@8.11.2`), any non-zero omit-dev result, or any pins-closure deviation returns to Patron with evidence. No `npm audit fix`, no overrides, no `--force`, no `--legacy-peer-deps`. Older-major status and qualification/evidence limits hold; no broader advisory clearance is inferred. Full ruling and advisory record in plan.md section 15.
- [ ] T022 Confirm C#-scope gates and mutation report SKIPPED (scope-empty), never PASS; Conductor records propertyTests opt-out (no domain invariants in generated contracts, static fixtures, bootstrap wiring) before verification

---

## Dependencies & Execution Order

- **Phase 1**: No dependencies — starts immediately; activates types drift.
- **Phase 2**: Depends on Phase 1 — extends the same drift check to handlers; carries the Q5 lifecycle move exactly once (T011 establishes the sole lifecycle; T012 proves Node strict rejection after it).
- **Phase 3**: Depends on Phase 2 — same handlers/fixtures, wildcard origins, strict API outcome.
- **Phase 4**: Depends on Phases 1-3 — docs + full verification only.
- Within a phase, [P] tasks touch different files and may run in parallel; otherwise single writer per file at a time. No simultaneous writes to the same file.

## Parallel Example: Phase 2 tests

```bash
# Different files, no dependencies — may run in parallel:
Task: "Create web/src/mocks/handlers.test.ts (US2 scenario proof)"
Task: "Create web/src/mocks/generate-api.test.ts (US2 generator proof)"
```

## Implementation Strategy

1. Complete Phase 1 — MVP: typed client with compile + runtime proof, types drift active.
2. Add Phase 2 — generated handlers + global lifecycle + scenario proof; test independently.
3. Add Phase 3 — policy + worker + startup; test independently.
4. Phase 4 — docs + full verification; stop at each checkpoint and validate before proceeding.
