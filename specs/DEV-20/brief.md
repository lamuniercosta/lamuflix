# DEV-20 — Frozen planning brief

Date: 2026-10-07. Thinker: Keel. Decision owner: Patron.
Worktree: F:/Dev/LamuFlix.worktrees/feature-020-spec.
Branch: feature/020-spec.
Recon base: 684d03251cce7551e827b5abe20bdd886fb7c7dd.
Ticket: DEV-20, size:M, no ui: tag; parent DEV-286.
Sources: DEV-20 intake lines 18-32; recon-DEV-20; specs/PRODUCT.md; .specify/memory/constitution.md; task-pipeline Phase 2; charter §2.3.
Full questions, recommendations, rulings, reasons and implications are in CONCLUSIONS.md. Taste copy is in ASSUMPTIONS.md.

## Outcome and frozen scope

Scaffold root web/ with React 19, Vite, strict TypeScript, Tailwind 4, ESLint, Prettier, Vitest, React Testing Library, jsdom and MSW, with npm scripts and frontend CI. npm run build and npm test must terminate successfully locally and in CI (DEV-20:18-32).
web/ is distinct from legacy src/LamuFlix.Web. Create a minimal main landmark with heading LamuFlix and paragraph Web client is ready., using Tailwind utilities (Q1 [assumed]).
No product routes, API calls, client generation, OpenAPI snapshots, hand-rolled API types, product MSW handlers, browser worker, router/state/form libraries, backend/legacy changes, migrations, playback execution, secrets or Process.Start.
Contract generation belongs to later API-client work; the C# DTO → OpenAPI → generated TypeScript chain is preserved. No new domain term, glossary edit or ADR.
Frozen scope is the scaffold, approved tooling, synthetic test harness, CI and necessary support edits described here; anything else is a follow-up issue, not a finding in this round.

## Every grill answer

1. Q1 ACCEPT: scaffold-only boundary and readiness copy; no owner checkbox. No new glossary term or ADR.
2. Q2 APPROVE with amendment: add @testing-library/dom to the direct-package allowlist; 24 direct packages, exact stable versions, React 19.x, Tailwind 4.x, committed npm lockfile, no force/legacy-peer-deps.
3. Q3 ACCEPT with amendment: npm test and test:run are single-shot; strict bare npx tsc --noEmit must cover src app and tests. Verify with a temporary failing source type error and restore before commit. Build also typechecks tooling.
4. Q4 ACCEPT: Node 24, independent frontend CI, npm cache and lockfile; only existing ci.yml, harness.yml web enable flag, and scoped .gitignore additions change.
5. Q5 ACCEPT: file set, task ordering, verification receipts and loop terms frozen; shared understanding reached; close brief.md; no owner blocker.

## Dependency and implementation approach

Plain React DOM entrypoint; no added architectural layer. Vite integrates React and Tailwind; Vitest configuration lives in vite.config.ts. Flat ESLint configuration uses TypeScript and React rules, with eslint-config-prettier to avoid conflicts; Prettier remains separate.

Direct runtime dependencies: react, react-dom.
Direct devDependencies: vite, typescript, eslint, prettier, tailwindcss, vitest, @testing-library/react, jsdom, msw, @types/react, @types/react-dom, @types/node, @vitejs/plugin-react, @tailwindcss/vite, @eslint/js, globals, typescript-eslint, eslint-plugin-react-hooks, eslint-plugin-react-refresh, eslint-config-prettier, @testing-library/jest-dom, @testing-library/dom.
These are the 24 approved direct packages (Q2). Transitives are resolved in the lockfile. Another direct dependency requires a deliberate Patron ruling. Select compatible current stable exact versions during implementation; no prereleases. React/react-dom remain 19.x and Tailwind its Vite plugin remain 4.x. npm ci must resolve without --force or --legacy-peer-deps.

Node 24 is in web/.node-version; package engines.node is >=24 <25. npm is not pinned to the unusual local npm version reported in recon. Lockfile is committed.

## Commands and configuration

- test: vitest run; test:run: vitest run; test:watch: vitest.
- build: tsc --noEmit && tsc --noEmit -p tsconfig.node.json && vite build.
- lint: eslint . --max-warnings 0 && prettier --check .
- format: developer-only prettier --write .; dev: Vite development command.
- Root tsconfig.json has strict checking and directly includes src app/tests, not an empty references-only check. tsconfig.node.json covers vite.config.ts tooling. Use vite/client through tsconfig types rather than generating a vite-env declaration.
- Vitest uses jsdom and the shared setup; RTL cleanup and jest-dom matchers are active.
- MSW node server starts beforeAll with onUnhandledRequest: error, resets afterEach and closes afterAll. Only synthetic non-product test URLs; no runtime App fetch or generated browser worker.
- ESLint and Prettier ignore install/build/coverage outputs; Prettier also excludes lockfile. Existing .editorconfig is not changed.

## Planned delivery file set

All new files below are under web/:

| Responsibility | Files |
| --- | --- |
| Package/toolchain | package.json, package-lock.json, .node-version |
| Bundler/TypeScript | index.html, vite.config.ts, tsconfig.json, tsconfig.node.json |
| Formatting/lint | eslint.config.js, .prettierrc.json, .prettierignore |
| App | src/main.tsx, src/App.tsx, src/index.css |
| Tests/harness | src/App.test.tsx, src/test/setup.ts, src/test/server.ts, src/test/msw.test.ts |
| Local instructions | README.md |

Existing files:
- .github/workflows/ci.yml: add independent frontend job; retain existing .NET job and contents: read.
- harness.yml: only gates.web.enabled false → true in the same delivery PR that adds web/package.json.
- .gitignore: append web/node_modules/, web/dist/, web/coverage/ ignores.

No scripts/run-web-gates.ps1, .editorconfig, project/solution, backend, or other workflow edits. Supporting files are justified by Q4 item-6 ruling and recon-DEV-20:48-56. No template logos, counters or unrelated assets.
This Phase A ask creates only CONCLUSIONS.md, ASSUMPTIONS.md and brief.md. Quill drafts spec/plan/tasks; Rigger owns commits. Existing feature pointer and baseline logs are retained.

## Test strategy

Use web-implement red-green tests. First prove screen behavior through accessible roles, then implement minimal React/Tailwind readiness screen. Prove MSW interception of a synthetic URL and handler reset/isolation; unhandled traffic is an error. No live server or product API contract mock. Do not add snapshot-only tests, arbitrary coverage/mutation thresholds, or backend tests for this scaffold.
Run a temporary intentional src type error: bare npx tsc --noEmit must fail. Restore it and record a successful check before commit. Preserve separate failure and restored-pass receipts.
Build output demonstrates Tailwind integration. Local fresh npm ci and noninteractive scripts prove toolchain/lockfile reproducibility.

## Task ordering and checkpoints

1. Toolchain: package/scripts, configs, Node pin and lockfile. Validate strict checking includes app/tests and tooling.
2. Scaffold/testing: web-implement red-green readiness and MSW harness tests; minimal app/CSS and entrypoint.
3. Integration: independent frontend CI job, enable existing web gate, scoped ignores and web README.
4. Gauge verification: run applicable pipeline gates and fresh frontend receipts.

Each code phase commits DEV-20 - <phase title> before the next phase, following task-pipeline. Required content stays in the named worktree. Phase B first uses Wisp drift recon and Keel pickup analysis before build. Quill must turn these decisions into dependent tasks rather than reselect packages or architecture.

## CI and gate expectations

Frontend job uses checkout and actions/setup-node@v4; node-version-file web/.node-version, cache npm, cache-dependency-path web/package-lock.json. Commands run from web/ in order: npm ci, npm run lint, npx tsc --noEmit, npm test, npm run build. Existing .NET job remains intact.

Gauge records fresh npm ci plus lint, bare strict typecheck, npm test, npm run test:run, npm run build and scripts/run-web-gates.ps1, all exit 0. Record the deliberate typecheck fail/restore probe separately. Enabled web gate must actually execute its steps; disabled SKIP is not acceptance proof.
Retain task-pipeline Phase 3 step 5 backend gates: C# analyzer and mutation scope-empty exits are SKIPPED (scope-empty), never PASS; property gate exit 2 requires task-note propertyTests: opt-out — no domain invariants introduced. Vulnerable-package gate, format and dotnet test remain under pipeline rules. No threshold reduction or substitute build-only receipt.
Hosted CI/gitleaks and all three review axes remain required at their later stages; this grill runs no gates and closes no merge bar.

## Loop discipline and owner boundary

Closing bar: Critical and High findings block; Medium and Low findings are recorded, nonblocking within frozen scope. Applicable gate failure, Could not run, or missing required axis blocks. Scope-empty/configured skips and accepted property opt-out follow task-pipeline exactly and are never PASS.
Review round cap: two. Remediation: at most two fix commits per round. Third-round findings go to owner review with the PR, not another automatic loop. Follow-ups receive tickets only when Critical/High, broken behaviour, or user-requested; otherwise record `noted, no ticket`, per charter.
No owner checkbox currently required: scope does not change ticket text or depart from constitution. Patron confirmed shared understanding in Q5.
Grill completion permits Quill drafting only. Gate 1 is the owner-merged spec PR; nobody merges, enables auto-merge, or starts delivery from this brief alone.

## Section 2.3 disposition

| Item | Disposition and cited ruling |
| --- | --- |
| 1 Dependencies | Ticket set already approved, residual direct set explicitly approved in Q2; DEV-20:22-26, PRODUCT.md §3 and standard companion-package rationale |
| 2 Architecture | web/ already ticket-named; plain React entrypoint with no new layer, Q1/Q2; DEV-20:22, PRODUCT.md:11 and §2 |
| 3 Schema | Untriggered; no database/entity/migration work, Q1 |
| 4 API shape | Untriggered; no route/DTO/status/client contract changes; Q1/Q3 and recon-DEV-20:65 |
| 5 Local execution/secrets | Untriggered; no LocalPlay/secrets/Process.Start work; Q1 and recon-DEV-20:66 |
| 6 Existing file scope | Explicit limited Q4 ruling for harness.yml/.gitignore; ci.yml ticket-named; gate/AC supporting edits, recon-DEV-20:48-56 |

No undecided plan question remains after the accepted Q5 closing ruling. Missing new facts go to Conductor as needs recon; scope/dependency decisions return to Patron.

## Plan challenge decisions - Patron, 2026-10-07

All three full axis reports are read. Patron accepted the following in-scope precision under Q1-Q5; no dependency/file/gate/API expansion, ticket change, constitution departure or owner checkbox.

- Sentry F1: shared setup imports @testing-library/jest-dom/vitest; tests and lifecycle modules explicitly import used APIs from vitest; globals false. Root strict TypeScript includes setup/app/tests. Basis: Q2/Q3; https://github.com/testing-library/jest-dom/blob/main/README.md and https://vitest.dev/config/globals.
- Sentry F2: record exact identity equality of the delivered 24 direct dependencies against Q2, not only their count.
- Sentry F4: T015 records an emitted CSS selector for a Tailwind utility actually used by the readiness screen, plus build/test success; DOM classes/build exit alone are insufficient.
- Sentry F6: T023 records local npm version and committed lockfileVersion, and hosted CI npm-version receipt when available; npm remains unpinned under Q4.
- Ledger L1: T026 records per-phase commit identifiers/subjects and ordering as a receipt.
- Sentry F5 and Compass F2: explicitly defer TanStack Query, Zod, openapi-typescript and openapi-fetch to later API-client work under Q1; no constitution departure. Compass F3: cite Q3 and the constitution API and Contract Rules generated-product-handler clause for synthetic MSW interpretation.
- Sentry F3: add non-gating npm audit --audit-level=high receipt; no gate/CI changes. High/Critical advisory in a runtime (non-dev) dependency is adjudicated under Q5 Critical/High closing bar. Dev-only advisories and an unrunnable audit are honestly recorded, nonblocking. Do not call an unsuccessful/unrunnable audit PASS.
- Compass F1: accept wording clarification, reject suggested pointer commit. Phase 1 .specify/feature.json pin stays uncommitted local workflow metadata in the spec worktree and excluded from staging in both PRs. Q4/Q5 and section 2.3 item 6 preserve specs-only spec PR and exactly three supporting delivery edits. Preserve the existing pin; no added task to commit it.

All ten source findings are nonblocking Medium/Low; duplicated stack citation is consolidated into one correction. Follow-ups: noted, no ticket. One numbered Quill document-fix list followed by independent verification. Gate 1 remains owner-controlled; no delivery authorization or merge-bar signature.

## Plan freeze receipt - Keel, 2026-10-07

Phase 2 step 6 complete: all three full challenge reports adjudicated with Patron, one eight-item numbered Quill fix list applied and independently reread. All ten source findings resolved or dispositioned; no open Critical/High or missing axis. Plan and tasks are FROZEN against this brief and the persisted plan-challenge ruling. Original T001-T026, 24 approved packages, 18 web files, three supporting delivery edits and Q1-Q5 boundaries retained. Frozen clarification locations: spec.md:24,75-76,100; plan.md:19,27,36-37,97; tasks.md:20,22,35-36,50,56,70,90,100,102. Gate 1 remains closed pending the owner-merged specs-only PR; this is no implementation authorization or merge-bar signature. Next: Conductor Phase 2 step 7 spec-PR handoff via Rigger, preserving and excluding the local feature pointer and baseline logs from staging.
