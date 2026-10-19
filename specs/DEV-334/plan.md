# Implementation Plan: DEV-334 Browser smoke tests for browse, filter, and import

**Branch**: `feature/334-spec` | **Date**: 2026-10-19 | **Spec**: `specs/DEV-334/spec.md`

**Input**: Feature specification from `specs/DEV-334/spec.md`, settled grill brief `specs/DEV-334/brief.md`, facts from `recon-DEV-334` and `recon-DEV-334-2`, Patron rulings in `specs/DEV-334/CONCLUSIONS.md`.

**Status**: Frozen after three-axis Phase 2 step 6 adjudication, 2026-10-19. Patron rulings and dispositions are in brief.md and CONCLUSIONS.md. Build waits for the user to merge the spec PR and establish Gate 1.

## Summary

Add three headless Chromium smoke specs for the seeded browse grid, real genre filtering, and a Pending folder import. Run them through `npm run e2e` against the DEV-325 compose stack plus a test-only overlay whose Node stub answers OMDb on loopback inside the API network namespace. Generate fixtures, credentials, and settings under an ignored runtime directory. CI gains a blocking pull-request job that retries once and keeps failure screenshots and sanitized logs.

## Technical Context

**Language/Version**: Node 24 (`web/.node-version`, engines `>=24 <25`). No production C# change. The stub and runner use Node built-in HTTP and the existing compose stack (.NET 10 API, PostgreSQL, RabbitMQ, production-built web).

**Primary Dependencies**: `@playwright/test` only, as a direct dev dependency. Exact pin is the highest stable Node-24-compatible release published at least 14 days before selection. Wisp records the registry version and publication date before installation. This plan does not name a version. No other direct npm or NuGet dependency. The lockfile is produced by npm.

**Storage**: The isolated compose project's own PostgreSQL and RabbitMQ, fresh per invocation, removed with that invocation's volumes. No schema change. Media comes from generated per-movie folders mounted read-only through `LIBRARY_PATH`. Sources are the four committed placeholders in `docker/sample-library/`: `Big Buck Bunny (2008).mp4`, `Elephants Dream (2006).mp4`, `Sintel (2010).mp4`, `Tears of Steel (2012).mp4`.

**Testing**: Playwright smoke specs under `web/e2e/*.e2e.ts`, matched explicitly so Vitest does not collect them. Vitest, React Testing Library, and MSW stay the web unit suite. Property tests are opted out with the brief's recorded reason. No production C# mutation pass is claimed.

**Target Platform**: Local developer machine and GitHub Actions `ubuntu-latest` pull-request CI. The browser target is `http://localhost:3000`.

**Project Type**: Existing web package plus test-only files under `web/e2e/`. No new project, top-level folder, or application layer.

**Performance Goals**: None. The brief records stack startup as a cost and sets no latency or volume target.

**Constraints**: Headless Chromium only. Serial specs. Retries 1 in CI and 0 locally. Bounded waits synchronized to requests or events. `Features.LocalPlay=false` in generated settings. Loopback-only provider URL. No overwrite of root `.env` or `docker/api.settings.json`. Refuse a busy `localhost:3000`. Secrets stay out of git and out of uploaded logs. Approved edits are only the files in Project Structure.

**Scale/Scope**: Three smoke specs, one runner, one stub, one fixture generator, one compose overlay, one additive CI job. The Playwright version, the wait-timeout number, the compose service key, and which placeholder fills which fixture role are phase outputs inside the constraints in `spec.md`. They are not frozen here.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-334: `@playwright/test`, three headless Chromium specs, `npm run e2e`, a CI job after stack health, retries 1, failure screenshots, and no real OMDb calls. Nothing is added or dropped. No owner checkbox (brief Q1–Q5).
- Dependency ruling is deliberate and cited (constitution Technology Stack Constraints; PRODUCT.md care list item 1; CONCLUSIONS.md Q3). The only new direct dependency is ticket-named `@playwright/test`. Vitest, React Testing Library, and MSW remain the web unit tools (constitution Technology Stack Constraints). xUnit remains the C# test framework (constitution IX). Patron recorded no constitution departure.
- No new project, top-level folder, port, or layer (constitution I; PRODUCT.md care list item 2). Files stay under `web/` and `web/e2e/`. The test-only provider listener is confined to the API network namespace with no additional host-published port. The stub is a test process, not a new adapter in `LamuFlix.Infrastructure`.
- No handler, route, status, or public DTO change (constitution II; V; API and Contract Rules). Setup calls existing `POST /api/movies/import`. Specs observe existing 202 and existing `genreIds`. No OpenAPI or generated TypeScript edit. TypeScript that names a contract uses the generated types.
- No schema change (constitution Technology Stack Constraints; PRODUCT.md care list item 3). Genres appear because the existing enrichment path persists a canned Found response.
- `Features:LocalPlay` stays false in generated settings. No secret is committed. No `Process.Start` (constitution VII; PRODUCT.md care list item 5; brief Q3).
- Existing-file edits are the five the amended ruling names (PRODUCT.md care list item 6; brief Phase 2 step 6). Base `docker-compose.yml` and `docker/sample-library/` stay as they are.
- No new domain term and no `CONTEXT.md` edit (constitution VIII; brief Q5). The isolation ADR is drafted as 0023 under this feature; Phase B publishes it canonically with only its status changed (constitution Documentation Rules).
- Gates stay the configured scripts (Static-Analysis Gates; brief Q5). Scope-empty skip, harness skip, mutation not-applicable, and the property-test opt-out are reported as themselves and are not passes.

Post-design re-check: the design below adds no production type, package beyond `@playwright/test`, public field, schema change, or glossary term. The same gates hold. No constitution violation is recorded, so Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-334/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── adr-0023-compose-browser-smoke-isolation.md
├── DRAFTING_RECEIPT.md
└── checklists/
    ├── requirements.md
    └── browser-smoke.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief freezes approach, files, and order. There is no new public contract to put under `contracts/`.

### Source Code (repository root)

```text
web/package.json                         # edit: @playwright/test and npm run e2e
web/package-lock.json                    # edit: npm-generated lockfile only
web/eslint.config.js                     # edit: runtime ignore only
web/.prettierignore                      # edit: runtime ignore only
web/e2e/tsconfig.json                    # new: Playwright config/spec typechecking
web/playwright.config.ts                 # new
web/e2e/run.mjs                          # new
web/e2e/stub.mjs                         # new
web/e2e/fixtures.mjs                     # new
web/e2e/compose.e2e.yml                  # new
web/e2e/.gitignore                       # new: ignores .runtime/
web/e2e/*.e2e.ts                         # new: exactly three smoke specs
.github/workflows/ci.yml                 # edit: additive blocking pull-request e2e job
```

Unchanged, and used as they stand: `docker-compose.yml`, `docker/sample-library/`, `docker/api.settings.example.json` as the shape the generated settings must satisfy, `web/.node-version`, `web/src/api/types.ts` when a spec names a contract, `web/src/library/LibraryView.tsx`, `web/src/features/library/FilterPanel.tsx`, `web/src/library/ImportDialog.tsx`.

**Structure Decision**: Test files live under `web/` so no top-level folder is added. Runtime output lives under ignored `web/e2e/.runtime/`. The runner passes a generated env-file and a settings-mount override. It does not write the developer's root `.env` or `docker/api.settings.json`.

## Design Overview

1. **Version gate.** Before any install, Wisp records drift of the approved file set against `origin/main` and the registry evidence for `@playwright/test`. Missing evidence stops the build. This plan does not select the version.
2. **Tooling.** `web/package.json` gains the exact-pinned dev dependency and `npm run e2e`, which starts `web/e2e/run.mjs`. `web/playwright.config.ts` runs headless Chromium only, matches only the three `web/e2e/*.e2e.ts` specs, runs those specs serially, sets retries to 1 when CI is set and 0 otherwise, uses `http://localhost:3000`, and writes failure screenshots under `web/e2e/.runtime/`. Add web/e2e/tsconfig.json covering the config/specs and wire its dedicated typecheck into the existing build script. Narrow eslint.config.js and .prettierignore edits exclude e2e/.runtime from lint and formatting.
3. **Overlay.** `web/e2e/compose.e2e.yml` is applied on top of the original `docker-compose.yml`. It adds one service that runs the Node stub and shares `network_mode: service:api`, and it overrides the API settings mount to the generated file. The service key is chosen in that file during implementation. The base compose file is not edited.
4. **Stub.** `web/e2e/stub.mjs` listens on loopback inside the API network namespace. Generated settings point `Omdb:BaseUrl` at `http://127.0.0.1:<port>` on that listener. The two seed titles receive immediate canned Found responses that the existing enrichment path persists with distinguishable genres. The reserved import is armed before each attempt, held until Pending is observed, and released in attempt finally on observation or failure; await drain before retry rearm. Unexpected provider requests fail the run. Nothing is forwarded. Keys are redacted. The host runner and attempt hooks invoke loopback control through bounded docker compose exec -T into the stub service. Bind to literal 127.0.0.1 and publish no stub/control ports. Control status exposes unexpected requests so the runner fails nonzero even after browser success. No application endpoint is added.
5. **Fixtures.** `web/e2e/fixtures.mjs` copies the four committed placeholders into per-movie folders under the generated library. Folder names remain names the existing folder cleaner can turn into a title and year. Exactly two folders are seeded. The reserved placeholder is copied to a distinct folder path for the initial attempt and another for the retry attempt. The specs do not require a fourth imported movie. `LIBRARY_PATH` in the generated env-file mounts that directory read-only at `/media/library`.
6. **Runner.** `web/e2e/run.mjs` creates `web/e2e/.runtime/` for this invocation, writes disposable Postgres and RabbitMQ credentials, a generated settings file (`Features.LocalPlay=false`, loopback `Omdb:BaseUrl`, and the other keys the existing compose settings template requires), and the fixtures. Use Node crypto.randomBytes for credentials. It refuses a busy `localhost:3000` and any effective provider URL whose host is not literal `127.0.0.1`, rejecting all hostnames. It starts an isolated compose project with the original compose file, the overlay, the generated env-file, and the settings-mount override. It waits for stub readiness and stack health, seeds the two movies once through `POST /api/movies/import`, and waits until both movies and their genres are observable. It then runs Playwright. On every path it preserves failure evidence, releases every outstanding hold in invocation finally, and deletes only this invocation's resources and volumes.
7. **Browse spec.** Opens the library in Chromium and asserts both seeded movies are visible on the grid.
8. **Genre spec.** Activates a real genre control whose persisted genre contains one seeded movie and not the other. Asserts that inclusion, that exclusion, and the matching `genreIds` value in the URL. The id comes from the live genre data for that run, not from a constant chosen in this plan.
9. **Import spec.** Submits the real import dialog for the attempt's reserved folder. Asserts the real POST returns 202 and that a Pending row for that import is visible before the stub releases the response. Arm before submission; release in attempt finally on success or failure and await drain before retry rearm.
10. **CI.** `.github/workflows/ci.yml` gains a blocking pull-request job. It uses Node 24 from `web/.node-version`, `npm ci` in `web/`, `npx playwright install --with-deps chromium`, and `npm run e2e`. It uploads failure screenshots and sanitized compose and stub logs, including an attempt that failed and then passed on retry. Use a pull_request guard, explicit bounded timeout, minimum permissions under existing workflow policy, and existing checkout/setup-node/upload-artifact v7 majors. Set retention-days explicitly and use an always-run upload of only sanitized screenshots/logs, including retry-pass failures. Redact generated Postgres/RabbitMQ credentials and provider keys; never retain settings or compose-config dumps. Scan the upload allowlist for every generated secret value and block unsafe upload.
11. **Negative proof.** After the suite is green, verification temporarily breaks one smoke assertion, confirms a nonzero exit and retained screenshot and sanitized logs, restores the spec, and reruns to exit 0. The broken assertion is not committed and a fourth spec is not added.
12. **Gates.** Gauge runs the web gate and the pipeline scripts. `propertyTests: opt-out — test-infrastructure-only change introduces no domain invariants` is recorded on the task note. It is not a pass. Scope-empty C# analyzer and mutation exits are `SKIPPED (scope-empty)`. A harness-disabled gate is `SKIP`. Mutation not-applicable is only the script's own exclusion verdict. Exit 1 or a gate that could not run blocks.

## Loop discipline

- **Closing bar:** zero unresolved Critical, High, or Medium findings; required applicable gates and all three real-stack smoke specs pass. Low findings are recorded with their disposition. A gate that could not run or exits 1 blocks. No waiver, threshold change, or green claim from missing evidence.
- **Round cap:** two review rounds maximum; at most two fix commits per round. Do not introduce a third round or silently reopen scope. Follow-up recording follows the charter: issues only where required, otherwise `noted, no ticket` in conclusions.
- **Frozen scope:** the Q1–Q4 Playwright tooling, disposable compose runner, loopback provider stub, generated sample-library fixtures, three browser smoke specs, blocking CI e2e job, and these planning documents. Exclude production C#, public API/DTO/schema changes, OpenAPI or generated-client changes, edits to base compose or committed sample media, visual regression, k6, playback execution, dependency swaps, root glossary changes, and unrelated cleanup. Anything else is a follow-up issue, not a finding in this round.

## Verification expectations

Run `scripts/run-web-gates.ps1` because `/web` changes, and run the pipeline checks through Gauge. Do not treat a plain build as a gate. Keep Vitest coverage and Playwright smoke coverage distinct. Exercise the default local `npm run e2e` path, the CI job configuration, a clean-stack repeat, retry fixture isolation, generated-file hygiene, scoped cleanup, and the restored negative failure path.

## Complexity Tracking

No constitution violation is recorded. This section is intentionally empty.


## Frozen refinements and verification

The six decisions in brief.md Phase 2 step 6 are normative. One named wait-budget in run.mjs is passed to stub and specs. No explanatory comments; existing web test naming is supplied by Wisp at pickup. Gauge verifies runner safety through controlled negative probes for provider-host refusal, busy port, unknown request, generated-secret scanning, failed-attempt release/retry rearm, and scoped cleanup. This is the deliberate Ledger M2 option b ruling, based on observable process/container/filesystem boundaries, not a claim that helper unit tests ran. Run the web gate after e2e and prove runtime files stay outside lint/format scope.

Phase B also publishes docs/adr/0023-compose-browser-smoke-isolation.md from the draft with only Status changed to Accepted and records the diff proof. dotnet format --verify-no-changes is auxiliary, reported separately from configured gates. Five Medium and thirteen Low findings are disposed in CONCLUSIONS.md; freeze does not establish Gate 1 or verification success.
