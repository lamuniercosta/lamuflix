# Implementation Plan: DEV-320 Typed API Client and MSW Handlers

**Branch**: `feature/320-spec` | **Date**: 2026-10-11 | **Spec**: `specs/DEV-320/spec.md`

**Input**: Feature specification from `specs/DEV-320/spec.md` (settled brief Q1-Q12, shared note `recon-DEV-320`).

## Summary

Generate a strictly typed `openapi-fetch` client from committed `web/src/api/openapi.json` via `openapi-typescript`, generate deterministic MSW handlers from the same document, and wire lint-integrated byte-equality drift (`check:api`) plus strict mock policy and opt-in browser startup. Phased per Q10: contract tooling/types/client, then fixtures/handlers/lifecycle, then policy/worker/bootstrap, then docs plus full verification.

## Technical Context

**Language/Version**: TypeScript 6.0.3 (web/package.json:41), Node per web/.node-version 24 (engines >=24 <25).

**Primary Dependencies**: `openapi-typescript@6.7.6` (generator devDependency, exact pin) + `openapi-fetch@0.9.8` (runtime client, exact pin); `msw` 3.0.2 already installed. Older major retained under Patron conditional clearance as amended by Decision A (shared DEV-320:39-41, addendum :40 (39a); recon-DEV-320:113-143; CONCLUSIONS:220-233, 253-258; brief:114-127, 129-135): (1) `generate:api` and `check:api` pass the generator only the fixed committed local file path `web/src/api/openapi.json`, never a URL, glob, or caller-supplied source; output-temp selection only. Basis: generator input is the repo-controlled contract (constitution 470-472); advisories need attacker-controlled glob/WebSocket/multipart input (recon-DEV-320:122-126); Q11 blocks only evidence-supported in-scope High (CONCLUSIONS:142-143; brief:102). (2) Five High + one Moderate dev-only advisories on six versioned nodes (`@fastify/busboy@2.1.1`, `braces@3.0.3`, `fast-glob@3.3.3`, `micromatch@4.0.8`, `openapi-typescript@6.7.6`, `undici@5.29.0`, keyed by name@version regardless of hoisted/nested path; npm counts nodes, not distinct advisories — recon:119-120) reachable only through the devDependency generator (recon:119-127, 131); T021 records fresh observed `npm audit` and `npm audit --omit=dev` commands, outputs, and exits against the actual manifest/lockfile, with no pre-stated pass. A High on exactly those six name@version nodes is known, not a new finding; any other High, any existing-node version change (including baseline `undici@8.11.2`), or any non-zero omit-dev result returns to Patron with evidence. Basis: routing paragraph CONCLUSIONS:200-214; Decision A (CONCLUSIONS:255-258). (3) No `npm audit fix`, no overrides, no `--force`, no `--legacy-peer-deps`; a plain install adds or changes only nodes in the two pins' dependency closure, with no existing lockfile node changing version. The 29-package count is scratch-expected (recon:115), not a real-lockfile invariant; T001 owns the actual added count, package paths, and pins-closure reconciliation from the real lockfile diff, and any pins-closure deviation returns to Patron. Basis: overrides prohibited and force conflicts with frozen TS 6.0.3 (recon:135-136; CONCLUSIONS:169-171); package.json/lockfile are in the frozen Q10 file set (CONCLUSIONS:128). Evidence limits: observed scratch install, two byte-identical generations and corrected harness tsc validation over all nine operations qualify this exact pair for this contract/toolchain only (recon:137-139; CONCLUSIONS:202-208); general TS 6 support, runtime fetch/MSW behavior, generated-file lint/format compliance, multi-status narrowing and affected-code unreachability are not established (recon:140-141); scratch six-node and omit-dev-zero observations (recon:119-131) are not real-lockfile facts or future passes. No broader advisory clearance is inferred.

**Storage**: N/A (static immutable fixtures; handlers mutate nothing).

**Testing**: Vitest 5.0.3 + React Testing Library + MSW 3.0.2; `tsc --noEmit` including compile-only `client.typecheck.ts`; `check:api` drift; `generate-api.test.ts` determinism/coverage/stale-temp checks; existing web gate per phase (lint, tsc, test:run, build).

**Target Platform**: Web frontend (`web/`), Vite 8.3.3 + React 19; jsdom for unit/integration tests; one supplementary manual browser smoke (backend stopped) recorded in PR notes, not a gate.

**Project Type**: Web application frontend slice (no backend change; committed OpenAPI is input only).

**Performance Goals**: N/A (no domain invariants; propertyTests opt-out per Q11 — generated contracts, static fixtures, bootstrap wiring).

**Constraints**: Deterministic LF generator output (web/.gitattributes eol=lf); generated files never hand-edited; `check:api` byte-compares temp output without overwriting committed files; `lint` = `check:api && eslint ... && prettier --check .`; generated types/handlers excluded from eslint/prettier but still type-checked by tsc; `msw.test.ts` unedited; `ci.yml`, `run-web-gates.ps1`, `harness.yml` untouched; no guessed ports/proxies/auth/retry/wrappers; no committed `.env*`.

**Scale/Scope**: Size M, no UI. Nine OpenAPI operations; frozen Q10 file set only.

## Constitution Check

GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.

- Contract chain (C# DTO -> OpenAPI -> generated TypeScript): PASS. Committed `openapi.json` is the sole input; `types.ts` generated; web code consumes generated types only; no hand-rolled duplicates (Q1-Q3).
- Generated MSW handlers + drift fails the build (470-472), CI OpenAPI drift (337), PRODUCT.md:12: PASS via `generate:api`/`check:api` wired through `lint`, which CI (`ci.yml:55-56`) and `run-web-gates.ps1` (:104) already run (Q8).
- `TimeProvider`/seeded-`Random`, `DateTime.Now` ban: PASS, not applicable (frontend-only; no C# touched; C# gates report SKIPPED scope-empty, never PASS per Q11).
- No new layer/project/top-level folder: PASS. Generator scripts under `web/scripts/`; supporting files inside `web/`; `web/public/` is inside `web/` (Q2, Q6, Q10).
- Dependencies (§2.3(1)): PASS via ticket text — ticket :22-23 names `openapi-typescript` + `openapi-fetch`; versions pinned at implementation (Q1, Q8).
- LocalPlay (§2.3(5)): PASS — `/api/movies/{id}/play` is mocked only, nothing executes (Q1).
- File scope (§2.3(6)): PASS — all edits are ticket-forced supporting files per Q5-Q8/Q10 rulings; `msw.test.ts`, `tsconfig*`, `vite.config`, `.gitattributes`, backend, committed OpenAPI, harness/workflow, ADR/glossary untouched.

Re-check after design: no violations introduced; conditional `generate-api.d.ts` holds script-module declarations only, never API types; any further compiler need returns to Patron (Q10).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-320/
├── spec.md              # Feature specification
├── plan.md              # This file
├── tasks.md             # Phase 2 output
├── brief.md             # Closed Phase 2 grill brief (Q1-Q12)
├── CONCLUSIONS.md       # Patron rulings (append-only)
└── DRAFTING_RECEIPT.md  # Drafting sources and needs-decision items
```

### Source Code (repository root)

```text
web/
├── package.json
├── package-lock.json
├── scripts/
│   ├── generate-api.mjs
│   └── check-api.mjs
├── src/
│   ├── api/
│   │   ├── types.ts            # generated
│   │   ├── client.ts
│   │   ├── client.test.ts
│   │   └── client.typecheck.ts # compile-only, never runtime-imported
│   ├── mocks/
│   │   ├── handlers.ts         # generated
│   │   ├── fixtures.ts
│   │   ├── browser.ts
│   │   ├── policy.ts
│   │   ├── startup.ts
│   │   ├── handlers.test.ts
│   │   ├── policy.test.ts
│   │   ├── startup.test.ts
│   │   ├── generate-api.test.ts
│   │   └── generate-api.d.ts   # only if compiler requires script declarations
│   ├── test/
│   │   ├── setup.ts            # sole lifecycle owner (edited)
│   │   └── server.ts           # harness handler + generated /api handlers (edited)
│   ├── vite-env.d.ts           # optional VITE_API_BASE_URL / VITE_ENABLE_MSW typing (edited)
│   └── main.tsx                # await startup() only (edited)
├── public/
│   └── mockServiceWorker.js    # official MSW CLI output, never hand-edited
├── eslint.config.js            # ignore entries (edited per phase)
├── .prettierignore             # ignore entries (edited per phase)
└── README.md                   # env flags, generate/check commands, regen order (edited per phase)
```

**Structure Decision**: Existing Vite/React layout under `web/` only; no new project, top-level folder, or architectural layer (Q10 frozen set).

## Complexity Tracking

No constitution violations to justify; nothing to record here.

## Phase Design (Q10 ordering, each checkpoint green)

**Phase 1 — Contract tooling/types/client.** Pinned deps + lockfile; `web/scripts/generate-api.mjs` + `check-api.mjs`; `generate:api`/`check:api` scripts with `lint` running `check:api` first; generated `types.ts` with its ignore entries; `client.ts` (explicit-`baseUrl` factory + default client); `client.test.ts` (phase-1 local typed `server.use` overrides under the existing lifecycle); `client.typecheck.ts`; `vite-env.d.ts` `VITE_API_BASE_URL` typing + one README line. `check:api` drift for `types.ts` active immediately. Ends green on the web gate.

**Phase 2 — Fixtures/generated handlers/global lifecycle.** `fixtures.ts`; generated `handlers.ts` extended into the same `check:api` comparison; `server.ts` gains generated `/api` handlers; `setup.ts` becomes the sole lifecycle owner (T011); `handlers.test.ts` (Q2 coverage + Q4 scenarios, plus T012-owned Node strict unhandled-`/api` rejection proof using the exported server under that lifecycle, proving failure without real-network reach; no new test file, no `msw.test.ts` edit, no duplicated hooks, no test logic in `server.ts`); `generate-api.test.ts` under `src/mocks` (determinism/coverage/stale-temp plus T013 input-source regression). `msw.test.ts` passes unedited. Ends green on the web gate.

**Phase 3 — Policy/browser/startup/official worker.** `policy.ts` (worker-only; worker-policy proof stays separately owned from the T012 Node rejection proof), `browser.ts`, `startup.ts` (exported opt-in function; `main.tsx` only awaits it); `policy.test.ts`, `startup.test.ts`; explicitly created `web/public` + `npx msw init ./public --save` (accepts `msw.workerDirectory`); worker + generated-file ignore entries; `VITE_ENABLE_MSW` typing + README line. Ends green on the web gate.

**Phase 4 — Documentation and integration verification.** Remaining README lines (backend-then-web regen order via `LAMUFLIX_REGENERATE_OPENAPI`, then `npm run generate:api`); full integration verification (check:api + generator tests, tsc incl. typecheck proof, full Vitest incl. unchanged harness tests, lint/build, hosted CI/gitleaks/vulnerability checks); one supplementary manual backend-stopped smoke recorded in PR notes. C# and mutation gates report SKIPPED (scope-empty), never PASS; propertyTests opt-out recorded by Conductor before verification (Q11).

Ownership: single writer per file at a time; Cog alters existing integration/config files, Anvil creates new modules, Conductor sequences handoffs (Q10).
