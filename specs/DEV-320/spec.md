# Feature Specification: DEV-320 Typed API Client and MSW Handlers

**Feature Branch**: `feature/320-spec`

**Created**: 2026-10-11

**Status**: Draft

**Input**: Closed grill brief `specs/DEV-320/brief.md` (Q1-Q12 ACCEPT/amended), shared note `recon-DEV-320`, task note `DEV-320`, `specs/PRODUCT.md`. No new decisions taken here; gaps are recorded in `DRAFTING_RECEIPT.md`.

Ticket: DEV-320 — Generate typed API client from committed `web/src/api/openapi.json` and configure MSW mock handlers. Scope and Technical Design: generated client via `openapi-typescript` + `openapi-fetch` in `web/src/api/client.ts` with fully typed parameters, headers, responses; MSW handlers in `web/src/mocks/handlers.ts` using generated types, supporting frontend development and testing without backend services running. Acceptance Criteria: (AC28) generated client provides full TypeScript autocompletion for all endpoints; (AC29) MSW accurately mocks backend API in Vitest tests.

## User Scenarios & Testing

### User Story 1 - Typed client calls against the committed contract (Priority: P1)

A frontend developer imports the generated client and gets full TypeScript autocompletion for all nine operations defined in committed `web/src/api/openapi.json`, with correct path/query/body serialization, success payloads, bodyless 202/204 handling, raw `Location` header access, and generated `ProblemDetails` errors.

**Why this priority**: This is AC28, the core contract-chain deliverable (PRODUCT.md:12; constitution 470-472). Nothing else is usable without it.

**Independent Test**: Run `web/src/api/client.test.ts` (runtime: all nine operations, serialization, success/error payloads, raw Location, bodyless 202/204, explicit and default origins) and `tsc --noEmit` over `web/src/api/client.typecheck.ts` (one correctly typed call per operation plus `@ts-expect-error` negatives). Both pass with phase-1 local typed overrides; global generated handlers arrive in US2.

**Acceptance Scenarios**:

1. **Given** the committed OpenAPI document, **When** a developer calls any of the nine operations through the client, **Then** request/response types and autocompletion match the generated contract exactly.
2. **Given** a 202 import/enrichment response, **When** the caller reads the raw response headers, **Then** the `Location` value is available (opaque fixture string under an existing route; undeclared in OpenAPI, noted as a backend contract gap, no ticket).
3. **Given** a declared error status, **When** the backend (or mock) returns it, **Then** the client surfaces the generated `ProblemDetails` shape with `application/problem+json`.

---

### User Story 2 - Deterministic generated MSW mocks in Vitest (Priority: P2)

Vitest tests exercise the frontend against generated MSW handlers covering every operation's success default and every declared error status, with immutable generated-type fixtures, strict unhandled-`/api` rejection, and per-test isolation via `resetHandlers`.

**Why this priority**: This is AC29. It depends on US1's generated types and extends the same drift check to handlers.

**Independent Test**: Run `web/src/mocks/handlers.test.ts` (every operation success plus every declared error status/content-type, nullable fixture variant, override/reset isolation; T012 additionally owns the Node strict unhandled-`/api` rejection proof under the T011 sole lifecycle, proving failure without real-network reach), `web/src/mocks/generate-api.test.ts` (deterministic LF output, exact operation coverage, stale-temp rejection, plus T013 input-source regression), `npm run check:api` (drift fails the build), and the full Vitest suite with `msw.test.ts` passing unedited.

**Acceptance Scenarios**:

1. **Given** the global test server, **When** a test calls any operation without an override, **Then** it receives the deterministic success fixture for that operation.
2. **Given** a test installs a `server.use` error override, **When** the call runs and the test ends, **Then** only a declared status is mockable and `resetHandlers` restores the default with fixtures unmutated.
3. **Given** an unhandled `/api` request, **When** no handler matches, **Then** the request fails instead of reaching the real network.

---

### User Story 3 - Backend-free browser development (Priority: P3)

A developer starts the Vite dev server with the backend stopped and `VITE_ENABLE_MSW=true`, and the browser worker answers typed client calls with the same generated handlers and fixtures.

**Why this priority**: Ticket item 26 (frontend development without backend services running). Supplementary to the gated Vitest proof; the manual smoke is recorded once in PR verification notes and is not a gate.

**Independent Test**: `web/src/mocks/policy.test.ts` (Q7 policy cases) and `web/src/mocks/startup.test.ts` (opt-in startup before render, off-by-default, production off, visible startup failure) pass; one manual smoke with the backend stopped is recorded in the PR notes.

**Acceptance Scenarios**:

1. **Given** `DEV` and `VITE_ENABLE_MSW=true`, **When** the app boots, **Then** the worker starts (awaited before `createRoot`) with the same handlers, fixtures, wildcard origins, and strict `/api` behavior.
2. **Given** production or the flag unset, **When** the app boots, **Then** no worker registers (the copied worker asset ships inert).
3. **Given** a worker startup failure, **When** it occurs, **Then** it throws visibly and never falls back silently.

---

### Edge Cases

- Browse returns a static page and ignores query params (no query engine, no business-rule duplication).
- Details default is one complete movie; a null-metadata / null-field variant is available as a scenario.
- `202`/`204` responses carry no body; `202` sets an opaque `Location` fixture under an existing route, never a new route.
- `GET /api/genres` declares no error responses, so no error scenario exists for it; every other operation mocks only its declared statuses.
- `/apiculture` and asset paths bypass the worker policy; `/api` and `/api/x.json` are rejected as unhandled API traffic.
- `GetMovieDetails` id is single-type integer; all other id path params and `totalCount` are `number|string` unions preserved exactly as generated.
- Phase 1 client runtime proof uses explicit local typed `server.use` overrides under the existing lifecycle; the lifecycle moves exactly once in phase 2 with no duplicate hooks.

## Requirements

### Functional Requirements

- **FR-001**: System MUST treat committed `web/src/api/openapi.json` as the sole client-generation input (Q1); generation scripts accept no caller-supplied source, URL, or glob — output-temp selection only (Decision A correction ownership; brief:139; CONCLUSIONS:242).
- **FR-002**: System MUST generate `web/src/api/types.ts` via `openapi-typescript` and MUST NOT hand-write or hand-edit it (Q1, Q8).
- **FR-003**: System MUST provide `web/src/api/client.ts` as a thin `openapi-fetch` runtime over generated types, exporting a factory taking an explicit `baseUrl` plus a default client using optional `VITE_API_BASE_URL` or the browser origin; tests use `https://test.local` (Q3).
- **FR-004**: System MUST generate `web/src/mocks/handlers.ts` from committed OpenAPI with one handler per operation, typed only from generated types, deterministic, marked generated, never hand-edited (Q2, Q8).
- **FR-005**: System MUST provide authored `web/src/mocks/fixtures.ts` holding generated-type data and scenario selection only, never handwritten contract types or handler registration (Q2).
- **FR-006**: System MUST keep fixtures immutable with one success default per operation for all nine operations; browse is a static page; details ships complete plus null-metadata/null-field variant; genres/people return arrays (Q4).
- **FR-007**: System MUST provide ready `server.use` scenarios for every declared non-2xx status of every operation (browse 422; details 404; import 400/422; enrichment 400/404/409/422; watchlist add/remove 400/404/409/422; play 400/403/404/422; people 422; genres none), served as `application/problem+json` `ProblemDetails`, and MUST NOT mock any undeclared status (Q4).
- **FR-008**: System MUST keep `server.ts` exporting the server with the existing `test.local/harness` handler plus generated `/api` handlers, and MUST register `listen`/`resetHandlers`/`close` plus RTL cleanup exactly once in the sole configured `setup.ts`; `msw.test.ts` passes unedited (import-path-only adjustment permitted) (Q5).
- **FR-009**: System MUST fail unhandled `/api` requests: Node `server.listen` keeps `onUnhandledFrame: 'error'`; the browser worker uses `web/src/mocks/policy.ts` (pathname equals `/api` or starts with `/api/` calls `defaults.error()` first; other HTTP and `ws` frames bypass) (Q7). The Node strict unhandled-`/api` rejection proof is owned by T012 in existing `web/src/mocks/handlers.test.ts`, using the exported server under the T011 sole `setup.ts` lifecycle to prove failure without real-network reach (Decision B; brief:137; CONCLUSIONS:260-262).
- **FR-010**: System MUST start the browser worker only when `import.meta.env.DEV` and `VITE_ENABLE_MSW === 'true'`, via dynamic `browser.ts` import awaited before `createRoot`; failure throws visibly; production never registers (Q6).
- **FR-011**: System MUST generate `web/public/mockServiceWorker.js` with the installed MSW CLI (`npx msw init ./public --save` from `web/`), never hand-edit it, and accept the documented `package.json` `msw.workerDirectory` metadata (Q7).
- **FR-012**: System MUST wire generation as `web/scripts/generate-api.mjs` (`generate:api` writes `types.ts` and `handlers.ts`) and `web/scripts/check-api.mjs` (`check:api` generates to temp, byte-compares LF output, never overwrites committed files); `lint` runs `check:api` before eslint/prettier; `ci.yml`, `run-web-gates.ps1`, and `harness.yml` stay untouched (Q8).
- **FR-013**: System MUST exclude generated `types.ts`/`handlers.ts` and the official worker asset from eslint/prettier through ignore entries (added per phase with the files that need them) while `tsc --noEmit` still type-checks the generated files (Q8, Q10).
- **FR-014**: System MUST keep the frozen Q10 file set with no other file changes (see Plan); any compiler need beyond the conditional `generate-api.d.ts`, or any new file/edit decision, returns to Patron (Q10).
- **FR-015**: System MUST run the existing web gate per phase (lint, tsc, test:run, build); phase 1 activates `check:api` drift for `types.ts` immediately and phase 2 extends the same check to `handlers.ts`; no checkpoint ships an unchecked generated file (Q10, Q11).

### Key Entities

- **Generated contract (`types.ts`)**: The single typed mirror of the nine OpenAPI operations; input only, never authored.
- **Typed runtime client (`client.ts`)**: Explicit-`baseUrl` factory plus default client; origin-agnostic handlers match by path on any origin.
- **Fixtures (`fixtures.ts`)**: Authored immutable data and scenario selection typed against generated schemas.
- **Generated handlers (`handlers.ts`)**: One deterministic handler per operation consuming generated types and fixtures.
- **Strict policy (`policy.ts`)**: Worker-only `/api`-first unhandled-frame callback; Node stays globally strict.

## Success Criteria

### Measurable Outcomes

- **SC-001**: `client.test.ts` plus `tsc --noEmit` over `client.typecheck.ts` prove typed calls for all nine operations with positive and negative type coverage (AC28).
- **SC-002**: `handlers.test.ts` proves one success per operation and every declared error status/content-type with override/reset isolation (AC29).
- **SC-003**: `check:api` plus `generate-api.test.ts` prove deterministic LF generation, exact operation coverage, and drift failure without touching committed files.
- **SC-004**: Full Vitest suite passes with `msw.test.ts` unedited; per-phase web gate (lint, tsc, test:run, build) is green at every checkpoint.
- **SC-005**: T012 in `handlers.test.ts` proves Node strict unhandled-`/api` rejection without real-network reach; `policy.test.ts` and `startup.test.ts` prove worker `/api` rejection with asset passthrough and opt-in browser startup; one manual backend-stopped smoke is recorded in PR notes (supplementary, not a gate).

## Assumptions

- Q12 records no unresolved taste decisions, no new glossary terms, no ADR requirement, and no ASSUMPTIONS entry (brief Q12; CONCLUSIONS Q12).
- Exact pins are `openapi-typescript@6.7.6` (devDependency) and `openapi-fetch@0.9.8` (runtime dependency), conditionally cleared for T001 under the Patron ruling as amended by Decision A (shared DEV-320:39-41, addendum :40 (39a); CONCLUSIONS:220-233, 253-258; brief:114-127, 129-135): only nodes in those pins' dependency closure may be added/changed with no existing-node version change; no `npm audit fix`, overrides, `--force`, or `--legacy-peer-deps`; the 29-package count is scratch-expected, not a real-lockfile invariant; T001 owns the actual added count, package paths, and pins-closure reconciliation from the real lockfile diff; T021 records fresh observed `npm audit` and `npm audit --omit=dev` commands, outputs, and exits with no pre-stated pass. Known High is keyed by name@version regardless of install path: `@fastify/busboy@2.1.1`, `braces@3.0.3`, `fast-glob@3.3.3`, `micromatch@4.0.8`, `openapi-typescript@6.7.6`, `undici@5.29.0`. Any other High, any existing-node version change (including `undici@8.11.2`), any non-zero omit-dev result, or any pins-closure deviation returns to Patron with evidence. Older-major status and qualification/evidence limits hold; no broader advisory clearance is inferred.
