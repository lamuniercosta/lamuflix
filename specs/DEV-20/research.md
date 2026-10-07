# Research: DEV-20 Web Scaffold

**Branch**: `feature/020-spec` | **Date**: 2026-10-07

All facts below are decided in `brief.md` / `CONCLUSIONS.md`; no new decisions taken. Sources cited per finding.

## R1 — Scope boundary (Q1)

- Decision: scaffold-only; minimal readiness screen (`main`/`h1 LamuFlix`/`p Web client is ready.`, Tailwind utilities); no routes, API calls, client generation, OpenAPI snapshots, hand-rolled types, product MSW handlers, browser worker, router/state/form libs, backend/legacy/migration/playback/secrets/`Process.Start` work.
- Basis: DEV-20:20-32 authoritative (PRODUCT.md section 3); speculative-abstraction ban (PRODUCT.md section 2); recon-DEV-20:29 (legacy `src/LamuFlix.Web` distinction), :65 (contract chain), :66 (LocalPlay/secrets).
- Open: none; anything beyond is a follow-up issue, not a finding in this round.

## R2 — Dependencies (Q2)

- Decision: 24 frozen direct packages (react, react-dom + 22 dev, incl. Patron-added `@testing-library/dom`); React 19.x, Tailwind 4.x + `@tailwindcss/vite`; exact stable versions at implementation; committed `package-lock.json`; no `--force`/`--legacy-peer-deps`.
- Basis: ticket-named 10 (DEV-20:22-26) + standard companions; recon-DEV-20:74 (lockfile).
- Open: concrete patch numbers are implementation selections, not rulings.

## R3 — Test/command contract (Q3)

- Decision: `test`/`test:run` = `vitest run`; `build` = `tsc --noEmit && tsc --noEmit -p tsconfig.node.json && vite build`; `lint` = `eslint . --max-warnings 0 && prettier --check .`; root tsconfig strictly covers `src` directly (bare `npx tsc --noEmit` must check app+tests); `vite/client` via `types`; jsdom + shared setup; MSW node lifecycle with `onUnhandledRequest: error`; role-based screen test + synthetic-URL reset test; no browser worker, snapshot-only tests, or coverage thresholds; temporary type-error fail/restore probe removed before commit.
- Basis: DEV-20:28,32; recon-DEV-20:48 (gate runs bare tsc), :73 (gate calls `test:run`), :76 (single-shot avoids watch hangs).
- Open: none.

## R4 — CI and existing-file scope (Q4)

- Decision: Node 24.x pin (`.node-version` + `engines >=24 <25`, npm unpinned); independent frontend CI job (`setup-node@v4`, npm cache, ordered `npm ci`, `lint`, `tsc --noEmit`, `test`, `build` from `web/`); existing `.NET` job and `contents: read` retained; only `ci.yml`, `harness.yml` (web flag), `.gitignore` (3 scoped ignores) change; `web/README.md` new in approved folder; `run-web-gates.ps1`, `.editorconfig`, solution/project files untouched.
- Basis: DEV-20:29 (CI named); recon-DEV-20:37-38 (v4 convention), :48-52 (gate/harness comment), :56 (ignores), :75 (Node 24.19.0 receipt).
- Open: none.

## R5 — Ordering, verification, loop terms (Q5)

- Decision: file set (18 new under `web/`), order toolchain -> scaffold/tests (web-implement red-green) -> integration (CI/flag/ignores/README) -> Gauge verification; per-phase commits `DEV-20 - <phase>`; Gauge records fresh `npm ci`, `lint`, bare tsc, `test`, `test:run`, `build`, `run-web-gates.ps1` exit 0 + type-error probe receipts; backend gates retained (analyzer/mutation scope-empty = SKIPPED scope-empty; property exit 2 = opt-out note); closing bar Critical/High block, Medium/Low nonblocking; 2 review rounds, 2 fix commits per round.
- Basis: task-pipeline Phase 2-3; charter loop terms; CLAUDE.md skipped-gate rule.
- Open: none. No owner checkbox; Gate 1 is the owner-merged spec PR.
