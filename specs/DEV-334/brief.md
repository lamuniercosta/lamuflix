# DEV-334 — Browser smoke tests: grill brief

Status: plan frozen after Phase 2 step 6 adjudication on 2026-10-19. Patron confirmed the six refinement rulings with zero owner checkboxes and no constitution departure. Gate 1, implementation authorization, and verification success remain separate.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-334-spec`; branch: `feature/334-spec`. Intake evidence pin: `06aedfeabc9e26ecc0348c71ca9983a2a8d27a65`. Facts come exclusively from the notes `DEV-334`, `recon-DEV-334`, and its continuation `recon-DEV-334-2`. The continuation contains section 9; it is not inline in the first note. Full exchanges and cited Patron rulings are append-only in [CONCLUSIONS.md](CONCLUSIONS.md).

## Ticket contract

The full ticket is reproduced in `recon-DEV-334-2:7-26`:

- Add `@playwright/test` under `web/`, exact-pinned to a release at least two weeks old; CI installs Chromium.
- Exactly three headless Chromium smoke specs against the DEV-325 compose stack and its sample library: seeded browse grid; genre filtering updates grid and URL; folder import shows a Pending row.
- Provide `npm run e2e` and a CI job that runs after stack health, retries once in CI, and retains failure screenshots.
- Local e2e passes, a red smoke spec fails the CI build, and no real OMDb calls occur: provider disabled or stubbed through configuration.
- No visual regression; k6 is DEV-427. Prerequisites are DEV-321, DEV-322, DEV-325, DEV-80. Recon reports their flows present at intake; Phase B must check drift rather than treating this as future-head proof.

## Loop discipline — Patron Q5

Closing bar: zero unresolved Critical, High, or Medium findings; required applicable gates and all three real-stack smoke specs pass. Record Low findings with their disposition. A gate that could not run or exits 1 blocks. No waiver, threshold change, or green claim from missing evidence.

Frozen scope: the Q1–Q4 Playwright tooling, disposable compose runner, loopback provider stub, generated sample-library fixtures, three browser smoke specs, blocking CI e2e job, and ticket planning documents listed below. Exclude production C#, public API/DTO/schema changes, OpenAPI or generated-client changes, edits to base compose or committed sample media, visual regression, k6, playback execution, dependency swaps, root glossary changes, and unrelated cleanup; anything else is a follow-up issue, not a finding in this round.

Round cap: two review rounds maximum; at most two fix commits per round. Do not introduce a third round or silently reopen scope. Follow-up recording follows the charter: issues only where required, otherwise `noted, no ticket` in conclusions.

Property tests: `propertyTests: opt-out — test-infrastructure-only change introduces no domain invariants`. Record this in the task note. It is not PASS and does not disable other pipeline gates.

## All grill answers and resulting plan decisions

### Q1 — Configuration-only provider isolation: ACCEPT

Use a test-only compose overlay under `web/e2e/` to add an OMDb-compatible stub implemented with Node built-in HTTP. The stub shares `network_mode: service:api`; generated API settings point `Omdb:BaseUrl` to `http://127.0.0.1:<port>` in that network namespace. Keep the real API, PostgreSQL, RabbitMQ, consumer, and production-built web application.

Basis: `recon-DEV-334-2:15,22,30-42,55`. The production host has no configuration-only consumer-disable switch; plain HTTP is allowed only on loopback. Genre facets require actual enrichment persistence, so a dead endpoint cannot meet the genre scenario. The namespace-sharing stub meets the ticket without C# changes or TLS bypasses. Cost: an additional test service and its lifecycle. No owner checkbox or constitution departure.

The earlier first-note claims that a placeholder key is inert and imports can simply tolerate Pending/NotFound are superseded by section 9.2: a junk key still calls real OMDb, and dead-loopback failures eventually become Failed. Neither behavior satisfies this plan.

### Q2 — Deterministic fixtures and Pending row: ACCEPT

Generate per-movie folders from the four committed DEV-325 placeholder files; mount the generated directory through existing `LIBRARY_PATH`, read-only. Preserve provenance without changing the committed sample library. Each import folder has a top-level video, matching the nonrecursive scanner.

Seed two distinguishable, genre-bearing movies through existing `POST /api/movies/import` and immediate canned stub responses. Wait on observable API state and genre availability before browser assertions. Reserve a third sample file for UI import fixtures. Hold its provider response until the browser observes Pending, then release during cleanup. Bound waits and synchronize with requests/events; do not use fixed sleeps or weaken the assertion to Pending-or-Failed.

Run the three specs serially on a fresh isolated database/queue per invocation. Q4 refines retry handling: seed once outside browser-worker retries and create distinct held-import folder paths for initial and retry attempts from the reserved sample file. Assertions must work without depending on spec order.

Basis: `recon-DEV-334:36`; `recon-DEV-334-2:15,22,46-57,61,64`. This exercises real imports and genre persistence without SQL seeding, browser interception, or changing the read-only mount. Cost: test-only hold/release control and serial execution. No owner checkbox or constitution departure.

### Q3 — Tooling, file scope and lifecycle: ACCEPT

Add only `@playwright/test` as a direct dev dependency; no additional direct npm or NuGet dependency. Exact-pin the highest stable Node-24-compatible release published at least 14 days before selection. Wisp must gather registry publication-date/version evidence before installation; no concrete version has been selected by this grill. Update the lockfile through npm, never by hand.

`npm run e2e` invokes a Node runner that generates disposable credentials, settings, and fixtures under ignored `web/e2e/.runtime/`. Use a generated env-file, an isolated compose project, the original DEV-325 compose definition plus the overlay, and an explicit settings-mount override. Never overwrite the user's existing root `.env` or `docker/api.settings.json`. Reject a conflicting owner of `localhost:3000`. Keep `Features.LocalPlay=false` in generated settings.

Wait for stub readiness and compose health before seeding or testing. Always clean up only this invocation's resources and volumes, including failure paths. Preserve failure evidence before teardown; credentials and generated settings must not enter committed files or uploaded logs. Runtime output directories, browser screenshots/reports, and sanitized logs stay within ignored `.runtime/`.

Approved existing-file edits:

- `web/package.json`: Playwright dependency and e2e script.
- `web/package-lock.json`: npm-generated dependency resolution.
- `.github/workflows/ci.yml`: additive e2e job.

Approved new implementation files:

- `web/playwright.config.ts`.
- `web/e2e/run.mjs`, `stub.mjs`, `fixtures.mjs`, `compose.e2e.yml`, `.gitignore`.
- Three `web/e2e/*.e2e.ts` smoke specs, with explicit Playwright `testMatch` and no Vitest discovery.

Ticket planning artifacts live under `specs/DEV-334/`. No tracked base `docker-compose.yml`, sample-library, production application, or unrelated configuration edit is authorized. If an unexpected forced edit/dependency becomes necessary, stop for bounded recon and a cited Patron ruling; Quill and builders must not invent it.

Basis: `recon-DEV-334:13,17-18,32-35,42-43,47-52`; `recon-DEV-334-2:14-16,34-40,57,61-64`. Deliberate section 2.3 rulings approve the dependency, layout/config-file scope, and disposable secret/LocalPlay handling. No new project, top-level folder, application layer, schema, or public API. Cost: lifecycle code to protect existing local data. No owner checkbox or constitution departure.

### Q4 — Browser assertions and CI evidence: ACCEPT

The only smoke scenarios are:

1. Browse: the browser renders the seeded movie grid.
2. Genre filter: interact with the real genre control; verify expected seeded movie inclusion and exclusion and the corresponding `genreIds` URL state.
3. Import: submit the real ImportDialog; observe the real POST returning 202 and the browser-visible Pending row before releasing the held provider response.

Do not intercept application API calls in the browser or replace the real backend with MSW. Setup uses existing API interfaces. Consume generated API types when type references are needed; introduce no handwritten application-contract duplicates.

CI adds a blocking PR e2e job using Node 24 from `web/.node-version`, `npm ci`, and `npx playwright install --with-deps chromium`. Run `npm run e2e` with one retry in CI and zero locally. Chromium only; no visual baselines. Preserve failure screenshots and sanitized compose/stub logs as CI failure artifacts, including evidence from an initially failed attempt that passes on retry.

Enforce loopback-only effective provider configuration before running, wait for stub readiness, and reject unexpected provider requests. The stub never forwards or falls back to the real provider. Redact keys from request evidence and logs. Test-only control is confined to the stub; no new application endpoint.

During verification, temporarily cause a controlled local smoke assertion failure to establish nonzero exit propagation and failure screenshot/log retention, then restore it and rerun successfully. Do not commit a fourth smoke spec. This is failure-path proof, not a claim that hosted CI has already passed.

Basis: `recon-DEV-334:13,24-28,34-35,42-43,51`; `recon-DEV-334-2:14-17,20-22,26,35-40,51-57,64`. Cost: stack startup minutes and one negative verification. No owner checkbox or constitution departure.

### Q5 — Shared understanding and closeout: ACCEPT

Patron confirmed grill closure, the closing bar, frozen Q1–Q4 scope, review/remediation caps, property-test opt-out, and the phase ordering below. Zero owner checkboxes remain; no ticket contradiction or constitution departure was approved.

Basis: `task-pipeline` section 2.2 and Phases 2–3; `DEV-334:5,21`; `recon-DEV-334:18-19,47-54`; `recon-DEV-334-2:14-26`.

## Task ordering and handoff

1. Quill drafts spec, plan, tasks, and L-ticket clarification/checklist artifacts from this brief and the cited recon. Gaps are `needs decision`, never inferred additions.
2. Keel runs read-only Spec Kit analysis and checks plan/tasks against this brief. Corrections follow the bounded numbered-list workflow.
3. At Phase 2 step 5, Keel drafts the L-ticket ADR in `docs/adr/` for configuration-only compose browser smoke isolation. Wisp supplies the available ADR number and relevant prior-ADR evidence first; this brief does not invent either. The ADR is a separately scheduled planning artifact, not a production-code change. Any spec-PR packaging constraint is handled at that step.
4. Run all three plan-challenge axes, adjudicate, and freeze. Patron's later provisional Gate 1 permits spec-PR handoff only. The user merges the spec PR before Phase B implementation.
5. Phase B begins with Wisp drift checks against main and Playwright release-age/version evidence. Missing facts return to recon. Implement tooling/configuration first, then the isolated runner/stub/fixtures and seeding readiness, then the three browser specs, then CI integration. Do not build on stale Phase A facts.
6. Gauge runs applicable pipeline gates, real-stack smoke verification and failure-artifact/exit proof. All three review axes remain required on this L ticket. Complete bounded review/remediation, later rebase/current-head gates and hosted checks before user-owned merge.

No new domain vocabulary was introduced. Existing browse, genre, import, and Pending terms retain their established meanings; no root `CONTEXT.md` edit or taste assumption was needed. Conclusions and this brief are ready to feed Spec Kit; the later ADR must remain consistent with these rulings.

## Verification expectations and reporting

Run the web gate for `/web` changes and the required pipeline checks through Gauge. Do not substitute a plain build for a gate. Keep Vitest coverage and Playwright smoke coverage distinct. Verify the default local `npm run e2e` path as well as CI configuration, clean-stack repeatability, retry fixture isolation, generated-file hygiene, scoped cleanup, and failure evidence.

Report exit-2 scope-empty analyzer/mutation results as `SKIPPED (scope-empty)`, never PASS; harness-disabled gates as `SKIP`. Mutation N/A is only the script's configured-exclusion verdict. This plan does not claim C# mutation success for a change with no production C#. Apply the recorded property-test opt-out without folding it into a green result. Exit 1 or an unrunnable applicable gate remains blocking.

No code, tests, gates, commit, tracker mutation, PR publication, or merge was performed as part of this grill. Plan freeze, provisional Gate 1, user Gate 1, implementation, review clearance, and hosted merge readiness remain separate later decisions/evidence.

## Phase 2 step 5 — ADR packaging ruling

Patron accepted reserving 0022 for the missing but tracked-skill-referenced harness ADR and assigning DEV-334 `0023-compose-browser-smoke-isolation`. Draft at `specs/DEV-334/adr-0023-compose-browser-smoke-isolation.md` with Proposed status for Phase A review; publish canonically at `docs/adr/0023-compose-browser-smoke-isolation.md` in the Phase B delivery PR after user Gate 1 spec merge, changing only the status to Accepted. This refines the packaging of task-ordering item 3; Q1–Q4 architectural content remains unchanged.

Basis: append-only [CONCLUSIONS.md](CONCLUSIONS.md), “Phase 2 Step 5 — ADR numbering and draft/publication path”; `recon-DEV-334-2:109-114,118-143,146-147,152`. This avoids the documented number reservation and follows the existing L-ticket draft/publication convention. No owner checkbox, constitution departure, implementation authorization, or gate result is implied.


## Phase 2 step 6 — Frozen plan refinements

Patron ACCEPTED all six refinement items in CONCLUSIONS.md, “Phase 2 Step 6 — Plan challenge adjudication ruling”, based on all three reports: Sentry (1 Medium, 6 Low), Ledger (3 Medium, 5 Low), Compass (1 Medium, 2 Low). These refinements supersede the narrower Q3 file whitelist and singular hold wording above; ticket delivery, exclusions, closing bar and two-round/two-fix-commit cap are unchanged.

1. Gate integration: additionally authorize existing-file edits to `web/eslint.config.js` and `web/.prettierignore` solely to exclude `e2e/.runtime/`. Add `web/e2e/tsconfig.json` covering the Playwright config and all three smoke specs; a dedicated package typecheck command is included in the existing build script so the web gate checks these files. No other tsconfig edit or dependency. Run the web gate after a completed e2e invocation and prove runtime files are outside lint/format scope.
2. Verification: adopt Ledger M2 option b, explicit Gauge negative probes rather than helper extraction or another unit suite. Prove refusal of non-loopback/provider-hostname configuration and occupied port, nonzero exit on unknown provider requests, secret-value artifact scanning, failed-attempt hold release/retry rearm, and scoped teardown on success/failure. The process/container/filesystem boundaries are verified through actual exits, resource ownership and retained artifacts. No C# mutation PASS or general Vitest TDD waiver is implied.
3. Hold lifecycle: host runner and Playwright attempt hooks use bounded `docker compose exec -T` into the stub service to invoke its loopback control. Bind the stub to `127.0.0.1`, publish no stub/control port, and add no application endpoint. Arm the reserved import before each attempt submits; release in attempt `finally` after Pending observation or failure, await drain before retry rearm, and release every remaining hold in invocation `finally`. Control status explicitly reports unexpected provider requests, which fail the runner even if browser assertions pass. A single named wait-budget source lives in `run.mjs` and is passed to the stub and specs.
4. Safety: generate disposable credentials with Node `crypto.randomBytes`; require the literal host `127.0.0.1` in the effective provider URL, rejecting hostnames and other addresses. Redact generated Postgres/RabbitMQ credentials and provider keys; never retain generated settings or compose-config dumps. Upload only sanitized evidence; scan that allowlist for generated secret values before upload and block unsafe upload.
5. ADR: keep Status Proposed and durable canonical-relative brief/conclusions/spec links; remove pipeline narration and ephemeral-note citations from the ADR. Publish `docs/adr/0023-compose-browser-smoke-isolation.md` in Phase B, changing only Status to Accepted, with a status-only diff proof. Report `dotnet format --verify-no-changes` as an auxiliary check with its own exit.
6. Standards: CI is guarded to pull requests, has an explicit bounded timeout, minimum permissions under the existing workflow policy, existing action majors (checkout/setup-node/upload-artifact v7 per Ledger), explicit retention-days, and an always-run upload of the sanitized evidence allowlist including initially failed retry-pass attempts. Implementation has no explanatory comments and uses recon-backed existing web test naming.

Plan freeze covers the amended brief, spec, plan, tasks and ADR. All five Medium and thirteen Low findings have a recorded disposition; none remains unresolved at plan level. Phase B probes and gates are required future work, not passed evidence. Next is Patron provisional Gate 1 and the spec PR; the user must merge it before Phase B.
