# DEV-20 — Grill conclusions

Date: 2026-10-07. Keel asks; Patron rules. Sources: DEV-20 intake, recon-DEV-20, PRODUCT.md, constitution, task-pipeline and charter. Maximum 12 questions. These are planning rulings; Gate 1 stays with the owner.

## Q1 — Frozen scope and vocabulary

### Keel question and recommendation

[from Keel] DEV-20 Q1/12 — Freeze scope and vocabulary. I recommend a minimal working scaffold in root web/, distinct from legacy src/LamuFlix.Web, with a small accessible LamuFlix readiness screen that proves React and Tailwind work. No product routes, API calls, OpenAPI snapshots or generated types, generated MSW API handlers, router/state/form libraries, backend edits, database edits, LocalPlay/secrets/process edits, or legacy migration. MSW is configured only as a test harness with a synthetic non-product URL when needed to prove interception. No new domain terms or ADR is needed for this M toolchain ticket. This keeps DEV-20:18-32 intact and avoids pulling deferred client work into scaffold; cost is later tickets must add real screens and contract generation. Please ACCEPT or AMEND with cited basis, confirm scope carries no owner checkbox, and settle assumed minimal screen copy. Facts come from recon-DEV-20. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron ruling

ACCEPT. The scope stays as you froze it, and no owner checkbox is needed.
- Basis: DEV-20:20-32 delivers only the toolchain, testing setup and CI. PRODUCT.md §3 makes the ticket authoritative; §2 bans speculative abstractions, so routes, an API client, a router and state or form libraries stay out. Keeping OpenAPI snapshots and generated types out leaves §2.3 item 4 untriggered and the contract chain intact (recon-DEV-20:65). LocalPlay, secrets and Process.Start stay out, so item 5 is not triggered (recon-DEV-20:66).
- Not a ticket change and no constitution departure: the readiness screen and synthetic MSW URL are the least needed to prove the acceptance criteria. Root web/ and legacy src/LamuFlix.Web must not be confused (recon-DEV-20:29). No new domain term is introduced, so CONTEXT.md needs no change (Principle VIII). No ADR is required for this M scaffold.
- Screen copy [assumed]: one <main> with an <h1> reading LamuFlix and a <p> reading Web client is ready., styled with Tailwind utility classes. No links or buttons or placeholder text (PRODUCT.md §4).

### Implications

Scaffold only; no backend or product API contract changes. No glossary change or ADR. No owner checkbox.

---

## Q2 — Dependencies and implementation approach

### Keel question and recommendation

[from Keel] DEV-20 Q2/12 — Supporting dependencies and implementation approach. I recommend a plain React DOM entrypoint and Vite React TypeScript setup, Tailwind through its Vite integration, flat ESLint TypeScript/React rules, and separate Prettier configuration. Approve an explicit direct dependency allowlist: ticket-named react, vite, typescript, eslint, prettier, tailwindcss, vitest, @testing-library/react, jsdom, msw; supporting react-dom, @types/react, @types/react-dom, @types/node, @vitejs/plugin-react, @tailwindcss/vite, @eslint/js, globals, typescript-eslint, eslint-plugin-react-hooks, eslint-plugin-react-refresh, eslint-config-prettier, and @testing-library/jest-dom. No autoprefixer/postcss direct packages, no user-event unless a later task needs interactions, no prettier ESLint runtime plugin. Use mutually compatible stable releases with exact direct versions and committed lockfile; Anvil resolves concrete versions during implementation without adding another direct package. The cost is a small explicit toolchain, no speculative app libraries. Deliberately rule section 2.3 items 1 and 2 and version-selection policy, cite ticket/constitution/engineering basis, and amend the list if needed. Facts come from recon-DEV-20. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron ruling

APPROVE the allowlist with one addition: @testing-library/dom as a direct devDependency. That makes 24 direct packages. This rules §2.3 items 1 and 2.
- Item 1: ten ticket-named packages are already decided (DEV-20:22-26, PRODUCT.md §3). Supporting packages are standard companions of named tools: runtime, types, Vite plugin, flat-config ESLint rules, Prettier/ESLint conflict switch, or test matchers. None is an app library. Patron adds @testing-library/dom as the peer companion of RTL 16; declaring it keeps the lockfile honest. Any direct package beyond these 24 needs another Patron ruling; Anvil may not add one independently.
- Item 2: no new folder or layer beyond ticket-named web/ (DEV-20:22, PRODUCT.md:11). Plain react-dom entrypoint, no router, state library or services wrapper (PRODUCT.md §2).
- Version policy: exact direct versions from stable lines at implementation time; React and react-dom 19.x (PRODUCT.md:11); Tailwind 4.x with @tailwindcss/vite. Commit web/package-lock.json because npm ci requires it (recon-DEV-20:74). No prereleases; peer resolution without --force or --legacy-peer-deps.

### Implications

The 24-package direct allowlist is frozen. Concrete compatible patch versions are implementation selections, not authority to add packages. React 19 and Tailwind 4 are fixed.

---

## Q3 — Test and command contract

### Keel question and recommendation

[from Keel] DEV-20 Q3/12 — Test and command contract. I recommend npm test and test:run both execute vitest run (single-shot locally and in CI); optional test:watch runs vitest for developers. build executes strict typechecking for app and tooling then vite build, lint runs ESLint with zero warnings and Prettier check, and format is a developer-only Prettier write command. Configure Vitest jsdom and RTL cleanup/jest-dom, plus MSW node server lifecycle beforeAll/listen(onUnhandledRequest error), afterEach/resetHandlers, afterAll/close. Meaningful tests render the readiness screen through accessible roles and prove one fetch to a synthetic test-only URL is intercepted and resets between tests; no browser worker/service-worker file, live server, real API mock, product fetch in App, snapshot-only test or artificial coverage threshold. This proves integration while keeping the screen independent of MSW. Costs one focused screen test and MSW harness test. Please ACCEPT or AMEND this test strategy and script contract citing DEV-20 and recon gate mismatch. Facts come from recon-DEV-20. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron ruling

ACCEPT, with one amendment: npx tsc --noEmit run from web/ must actually typecheck src.
- Script contract: test and test:run run vitest run. Ticket names test and requires it clean in CI (DEV-20:28,32); gate calls test:run (recon-DEV-20:73). Single-shot avoids watch-mode hangs (recon-DEV-20:76). test:watch and format add scripts without removing ticket requirements. lint runs ESLint --max-warnings 0 plus prettier --check.
- Gate integrity: gate runs bare npx tsc --noEmit (recon-DEV-20:48). Patron identifies the template risk of a root tsconfig with files: [] and references only producing a vacuous check. Structure root tsconfig so this command checks app and test sources with strict: true. Prove one deliberate src type error fails that command. build still typechecks app and tooling before vite build.
- Test strategy accepted: no browser worker, public/mockServiceWorker.js, product fetch in App, snapshot-only tests or new coverage threshold. One role-based readiness screen test and one synthetic-URL MSW interception/reset test prove the ticket testing tools run (DEV-20:26); API shape remains untriggered.

### Implications

Root TypeScript configuration must cover app and tests directly. The negative typecheck probe is temporary and must be removed before commit. The existing web gate is not rewritten.

---

## Q4 — CI and supporting file scope

### Keel question and recommendation

[from Keel] DEV-20 Q4/12 — CI and supporting file scope. I recommend Node 24.x in a new independent frontend job in the existing .github/workflows/ci.yml, actions/setup-node@v4 with npm cache keyed by web/package-lock.json and node-version-file web/.node-version containing 24. Run commands from web/: npm ci, npm run lint, npx tsc --noEmit, npm test, npm run build. Retain the .NET job as-is and keep contents:read permissions. Commit package-lock.json; declare engines.node >=24 <25 without pinning npm to the anomalous local npm version in recon. Enable only gates.web.enabled false to true in harness.yml in the same delivery PR. Append scoped web/node_modules/, web/dist/, web/coverage/ ignores to .gitignore. No changes to run-web-gates.ps1, .editorconfig, solution/project files, backend code, or other workflows. Include web/README.md with node pin, npm ci and commands. The cost is one frontend CI job; forced supporting edits fulfill AC and activate the existing gate. Deliberately ACCEPT or AMEND section 2.3 item 6 scope and Node pin with cited basis; confirm no ticket/constitution departure. Facts come from recon-DEV-20. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron ruling

ACCEPT. This rules §2.3 item 6 and the Node pin. No ticket change or constitution departure.
- ci.yml is ticket-named (DEV-20:29). harness.yml changes only gates.web.enabled false to true; its comment mandates enabling with web/package.json (recon-DEV-20:49-52), raising coverage without lowering a gate (recon-DEV-20:67). .gitignore gains only web/node_modules/, web/dist/, web/coverage/ (recon-DEV-20:56). All other named exclusions remain untouched.
- Node 24.x in web/.node-version and engines >=24 <25 matches the local v24.19.0 recon receipt (recon-DEV-20:75). Patron selects this line for CI/local parity. npm is not pinned; anomalous local npm 9.6.7 is not a contract requirement. setup-node@v4 with lockfile-keyed npm cache matches existing @v4 action convention (recon-DEV-20:37-38).
- New independent frontend CI job keeps .NET job and contents: read unchanged, mirrors gate order (recon-DEV-20:48), and satisfies CI AC (DEV-20:32). web/README.md is new inside the approved folder and does not trigger existing-file item 6.

### Implications

Node major 24 is the project pin. npm resolves through the selected Node installation and committed lockfile. Only the three approved existing files are in delivery edit scope.

---

## Q5 — Closing plan and loop discipline

### Keel question and recommendation

[from Keel] DEV-20 Q5/12 — Close and freeze the implementable plan and loop terms. I recommend this file set: web/package.json, package-lock.json, .node-version, index.html, vite.config.ts, tsconfig.json (strict src app/tests), tsconfig.node.json (strict vite.config.ts tooling), eslint.config.js, .prettierrc.json, .prettierignore, README.md, src/main.tsx, src/App.tsx, src/index.css, src/App.test.tsx, src/test/setup.ts, src/test/server.ts, src/test/msw.test.ts; existing edits only ci.yml, harness.yml, .gitignore. Vitest config lives in vite.config.ts. build is tsc --noEmit && tsc --noEmit -p tsconfig.node.json && vite build; use vite/client via tsconfig types instead of a generated vite-env declaration. Ignore dependency/build/coverage/lockfile in Prettier as appropriate. Tasks order: Phase 1 toolchain/package/config/lockfile; Phase 2 web-implement red-green tests and minimal screen; Phase 3 CI, harness flag, ignores and README; Phase 4 Gauge verification. All code phases commit before next phase; existing supporting file changes remain in scaffold delivery, not this spec PR. Gauge must record fresh npm ci, lint, bare tsc, npm test, test:run, build and run-web-gates.ps1 exit 0, plus temporary src type-error fail/restore probe; retain existing backend workflow checks and pipeline Phase 3 step 5 backend gates, with C# analyzer/mutation scope-empty results never called PASS. No extra frontend mutation/coverage threshold, no backend property tests added; record propertyTests: opt-out — no domain invariants introduced if that gate exits 2. Closing bar: Critical/High findings block; Medium/Low are recorded nonblocking within frozen scope; failed applicable gates or Could not run block; scope-empty/configured skips follow pipeline. Review cap two rounds, remediation two fix commits per round. Frozen scope is Q1-Q4 and this plan; anything else is a follow-up issue, not a finding in this round. ACCEPT or AMEND, confirm shared understanding and permission to close brief; identify any owner blocker. Facts come from recon-DEV-20. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron ruling

ACCEPT. The file set, task order, closing bar and caps are frozen as proposed; no owner blocker; close brief.md.
- Consistent with Q1-Q4: strict root tsconfig checks app/tests; tooling config covers vite.config.ts; temporary type-error fail/restore proves Q3. Supporting file edits are the three approved in Q4. Spec PR stays specs/** only; scaffold and supporting edits ship in delivery PR (task-pipeline Phase 2 step 7 and line 140).
- Gate honesty: C# and mutation scope-empty results and accepted property-test opt-out exit 2 are skips, never PASS (CLAUDE.md skipped-gate rule). Enabled run-web-gates.ps1 must exit 0 (recon-DEV-20:48-50).
- Closing terms: Critical/High and failed or could-not-run applicable gates block; Medium/Low are recorded nonblocking. Two review rounds, two fix commits per round. Outside Q1-Q5 is follow-up: file a ticket only for Critical/High, broken behaviour, or user-requested work under charter; otherwise record noted, no ticket. These are standing chain terms, not a per-ticket override.

### Implications

Shared understanding reached after five questions; no open decision or owner checkbox. Keel closes brief.md for Quill. No implementation or gates were run in this grill. Resolved Q1-Q5 exchanges are persisted before being treated as safe to summarize.

## Plan challenge ruling - 2026-10-07

[from Patron] DEV-20 plan challenge ruling: ACCEPT, with one amendment (F3).

1. Sentry F1/F2/F4/F6 + Ledger L1: ACCEPT as in-scope clarifications.
   - They are receipts and test-setup precision inside the Q2 dependency set and Q3 command contract (CONCLUSIONS Q2/Q3); no new dependency, file, gate or API shape.
   - The vitest jest-dom entrypoint with explicit imports and globals false is a configuration choice for packages Q2 already approved, not a swap.

2. Sentry F5 / Compass F2 (duplicate) + Compass F3: ACCEPT as cited deferral / synthetic-handler clarification.
   - Basis: Q1 frozen scope (no product routes or API calls) and Q3 test contract. No ticket: noted, no ticket (not Critical/High, not broken behaviour).

3. Sentry F3: ACCEPT, AMENDED.
   - Non-gating npm audit --audit-level=high receipt; no change to gates or CI (Q4 supporting-file scope, Q5 closing terms).
   - Amendment: a High/Critical advisory in a runtime (non-dev) dependency is adjudicated as a Critical/High finding under the Q5 closing bar, not merely recorded. Dev-only advisories and an unrunnable audit are recorded honestly, nonblocking.

4. Compass F1: ACCEPT the rejection. .specify/feature.json is not committed in either PR.
   - Basis: §2.3 item 6. feature.json is a tracked file the ticket does not name, and no AC or gate forces the edit; Q4/Q5 fix the delivery to the scaffold plus three supporting files and the spec PR to specs/** only.
   - Disposition: leave the Phase 1 pin as an uncommitted local workflow edit in the spec worktree and exclude it from staging. No new facts and no further scope ruling needed. No follow-up ticket: noted, no ticket.

Persist as you proposed; no PR or gate writes from me.

