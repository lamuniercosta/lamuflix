# Tasks: DEV-334 Browser smoke tests for browse, filter, and import

**Input**: Design documents from `specs/DEV-334/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` (plan records why).

**Tests**: The three Playwright specs are the ticket's delivery. They are authored after the runner can launch a stack (brief task ordering). There is no C# red-green loop and no fourth spec. Vitest stays the web unit suite. Patron accepted Gauge boundary probes instead of helper extraction in the step 6 ruling. No explanatory comments in .mjs or .ts; Wisp supplies existing web test naming at pickup.

**Organization**: Phases follow brief task ordering. Do not start Phase 1 until the spec PR has merged and Gate 1 is established. Phase 1 is Wisp and is read-only. Anvil implements Phases 2–7, one phase per ask, and stops at the end of that phase. Phase 8 is Gauge. A task that only runs gates stays with Gauge.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 seeded browse grid, US2 genre filter, US3 Pending import.
- Exact file paths are in each description.

## Phase 1: Drift and version evidence (Wisp)

**Purpose**: Confirm the intake flows still exist on the implementation head, and record the Playwright pin before anyone installs it. Blocks Phase 2.

**Independent Test**: The recon note names the head, the drift result for every approved path, and one `@playwright/test` version with its registry publication date at least 14 days before that evidence. No package file has changed.

**Checkpoint**: Implementation may install only the recorded version. A missing flow or missing registry evidence stops the phase.

- [ ] T001 Compare the approved paths in `specs/DEV-334/plan.md` (Project Structure) and the browse, genre, and import flows named in `specs/DEV-334/spec.md` with `origin/main` at the Phase B head. Record the head and the drift result in the recon note. Intake pin `06aedfeabc9e26ecc0348c71ca9983a2a8d27a65` is not proof of that head. Stop if a named flow or an approved path's surrounding contract has drifted. Supply existing web test naming evidence before specs are written.
- [ ] T002 Record, in the recon note, the highest stable Node-24-compatible `@playwright/test` release whose registry publication date is at least 14 days before the evidence date, plus that date. Do not edit `web/package.json` in this task. Stop if that evidence is missing.

---

## Phase 2: Playwright tooling

**Purpose**: The package pin, the e2e script, the Playwright config, and the runtime ignore. Depends on Phase 1. Blocks Phase 3.

**Independent Test**: `web/package.json` lists only the T002 version of `@playwright/test` as a new direct dev dependency, `npm run e2e` is present, and `web/playwright.config.ts` matches only the e2e specs.

**Checkpoint**: The lockfile is npm-generated. Vitest's existing script is unchanged; the build script adds e2e typechecking. No second direct dependency was added.

- [ ] T003 Add `@playwright/test` to `web/package.json` as an exact-pinned dev dependency at the version T002 recorded, and add the `e2e` script that runs `web/e2e/run.mjs`. Update `web/package-lock.json` only by running npm. Do not hand-edit the lockfile. Do not add another direct dependency. Add web/e2e/tsconfig.json covering web/playwright.config.ts and the three e2e specs; wire a dedicated typecheck command into the existing build script so run-web-gates checks them.
- [ ] T004 Create `web/playwright.config.ts` so the suite is headless Chromium only, `testMatch` is limited to the three `web/e2e/*.e2e.ts` specs, the specs run serially, retries are 1 when CI is set and 0 otherwise, the base URL is `http://localhost:3000`, and failure screenshots are written under `web/e2e/.runtime/`. Depends on T003.
- [ ] T005 [P] Create `web/e2e/.gitignore` so `web/e2e/.runtime/` is ignored. Narrowly edit `web/eslint.config.js` and `web/.prettierignore` solely to exclude `e2e/.runtime/` from lint/format scope.

---

## Phase 3: Isolated runner, stub, and fixtures

**Purpose**: The disposable stack, the loopback stub, and seeding readiness. Depends on Phase 2. Blocks the three spec phases.

**Independent Test**: `npm run e2e` can bring the overlay stack to stub readiness and health, seed two genre-bearing movies once, and tear down only that invocation, before the browser specs exist. A busy port 3000 and a non-loopback provider URL refuse to start.

**Checkpoint**: Root `.env` and `docker/api.settings.json` are untouched. Generated settings set `Features.LocalPlay` to false and `Omdb:BaseUrl` to loopback. The base `docker-compose.yml` and `docker/sample-library/` are untouched.

- [ ] T006 [P] Add `web/e2e/compose.e2e.yml` as an overlay on `docker-compose.yml`: one stub service sharing `network_mode: service:api`, and a settings-mount override that points at the generated settings file. Do not edit `docker-compose.yml`. The compose service key is chosen in this file. Bind the stub to 127.0.0.1; publish no stub/control port. Host control uses bounded docker compose exec -T into that service.
- [ ] T007 [P] Implement `web/e2e/stub.mjs` with Node's built-in HTTP. Serve immediate canned Found responses for the two seeded titles, and arm the reserved import per attempt before UI submission and hold until Pending observation; release in attempt finally after observation or failure, await drain before retry rearm, and release every outstanding hold in invocation finally. Fail the run on an unexpected provider request. Do not forward or fall back to the real provider. Redact keys from evidence and logs. Provide loopback-only hold/release/drain/status control invoked through bounded docker compose exec -T into the stub service; runner and attempt hooks never reach a container listener through host HTTP. Return explicit unexpected-request status and make the runner fail nonzero even after browser success. Do not add an application endpoint.
- [ ] T008 [P] Implement `web/e2e/fixtures.mjs` to copy `docker/sample-library/Big Buck Bunny (2008).mp4`, `docker/sample-library/Elephants Dream (2006).mp4`, `docker/sample-library/Sintel (2010).mp4`, and `docker/sample-library/Tears of Steel (2012).mp4` into per-movie folders under the generated library. Each folder that will be imported has a top-level video. Designate exactly two folders as the seed folders. Copy the reserved placeholder into a distinct folder path for the initial attempt and another for the retry attempt. Do not add a media file, and do not require a fourth imported movie. Do not edit `docker/sample-library/`.
- [ ] T009 Implement `web/e2e/run.mjs` so `npm run e2e` writes disposable Postgres and RabbitMQ credentials using Node crypto.randomBytes, generated API settings, and fixtures under `web/e2e/.runtime/`. Pass a generated env-file and the `LIBRARY_PATH` mount. Compose the original `docker-compose.yml` plus `web/e2e/compose.e2e.yml` as an isolated project. Refuse a busy `localhost:3000`. Require the literal host 127.0.0.1 in effective `Omdb:BaseUrl`; reject hostnames and all other addresses. Set `Features.LocalPlay` to false. Wait for stub readiness and stack health, seed the two movies once through `POST /api/movies/import`, and wait until both movies and their genres are observable before launching Playwright. On every path, preserve failure evidence, release every outstanding hold in invocation finally, and remove only this invocation's resources and volumes. Do not overwrite the root `.env` or `docker/api.settings.json`. Own one named wait-budget source and pass it to stub/specs. Redact generated database/queue credentials and provider keys; never retain generated settings or compose-config dumps. Scan the sanitized evidence allowlist for generated secret values before upload and block unsafe upload. Depends on T006, T007, and T008.

---

## Phase 4: User Story 1 - Seeded browse grid (Priority: P1)

**Goal**: Chromium shows both seeded movies on the browse grid.

**Independent Test**: Run only this spec through `npm run e2e` against a fresh stack. Both seeded movies are visible and the command exits 0.

### Implementation for User Story 1

- [ ] T010 [P] [US1] Add the browse smoke spec as one `web/e2e/*.e2e.ts` file. Open the library at `http://localhost:3000` and assert both seeded movies are visible on the grid. Do not intercept application calls in the browser. If the spec names an application contract, use the generated types in `web/src/api/types.ts`. Depends on T009.

**Checkpoint**: The browse spec passes on its own. The developer's root `.env` and `docker/api.settings.json` are unchanged.

---

## Phase 5: User Story 2 - Genre filter (Priority: P2)

**Goal**: The real genre control includes one seeded movie, excludes the other, and writes `genreIds` into the URL.

**Independent Test**: Run only this spec on a fresh stack. Judge inclusion, exclusion, and the URL without the browse spec or the import spec.

### Implementation for User Story 2

- [ ] T011 [P] [US2] Add the genre smoke spec as one `web/e2e/*.e2e.ts` file. Activate the real genre control for a persisted genre that contains one seeded movie and not the other. Assert that inclusion, that exclusion, and the matching `genreIds` value in the URL. Take the id from the live genre data for the run. Do not intercept application calls in the browser. Depends on T009.

**Checkpoint**: The genre spec passes on its own and does not assume spec order.

---

## Phase 6: User Story 3 - Pending import (Priority: P3)

**Goal**: The real import dialog shows a Pending row while the stub holds the provider response.

**Independent Test**: Run only this spec on a fresh stack. Observe POST 202 and a visible Pending row, then release the hold during cleanup.

### Implementation for User Story 3

- [ ] T012 [P] [US3] Add the import smoke spec as one `web/e2e/*.e2e.ts` file. Submit the real import dialog for this attempt's reserved folder. Assert the real POST to `/api/movies/import` returns 202 and that a Pending row for that import is visible before `web/e2e/stub.mjs` releases the response. Use the distinct folder path from T008 for the initial attempt and for a retry. Arm via stub exec control before submission; release in attempt finally on Pending observation or failure, and await drain before retry rearm. Bound the wait on the request or the visible row. Do not use a fixed sleep, and do not accept Pending-or-Failed. Do not intercept application calls in the browser. Depends on T009.

**Checkpoint**: The import spec passes on its own. The tree contains exactly three `web/e2e/*.e2e.ts` specs.

---

## Phase 7: CI e2e job

**Purpose**: A blocking pull-request job runs the same `npm run e2e` entry point and keeps failure evidence. Depends on Phases 4–6.

**Independent Test**: The workflow file shows the job's steps, the Node 24 pin, Chromium install, one CI retry via the runner, and failure-artifact upload. A red spec fails the job.

**Checkpoint**: The job is additive. No visual baseline is configured. Uploaded logs are sanitized.

- [ ] T013 Add a blocking pull-request e2e job to `.github/workflows/ci.yml`. Use Node 24 from `web/.node-version`, run `npm ci` in `web/`, run `npx playwright install --with-deps chromium`, and run `npm run e2e`. Upload failure screenshots and sanitized compose and stub logs from `web/e2e/.runtime/`, including evidence from an attempt that failed and then passed on retry. Do not upload credentials or provider keys. Do not add a visual baseline. Use a pull_request guard, explicit bounded timeout, minimum permissions under existing workflow policy, checkout/setup-node/upload-artifact v7 majors, explicit retention-days, and an always-run upload of only the sanitized screenshots/logs allowlist, including retry-pass failure evidence. Do not upload the whole .runtime tree or settings/compose-config dumps; block upload on secret-value scan failure. Do not edit any other workflow.

---

- [ ] T013a Publish `docs/adr/0023-compose-browser-smoke-isolation.md` from the Phase A draft after user Gate 1, changing only Status to Accepted. Anvil records the status-only diff proof in the delivery PR before Gauge verification. No architectural rewrite.

---

## Phase 8: Verification (Gauge)

**Purpose**: Not an Anvil phase. After Phases 2–7 and T013a are committed, Gauge runs the gates, the real-stack suite, and the restored negative proof, and records observable evidence for the verification named in `brief.md` and `plan.md`.

**Checkpoint**: Every mandatory gate has its script verdict. The negative proof does not remain in the tree. Scope-empty skip is not recorded as pass. Clean-stack repeatability, retry fixture isolation, retry-pass failure evidence, generated-file and secret hygiene, and scoped cleanup are recorded from what the runs showed.

- [ ] T014 Run `scripts/run-web-gates.ps1` and record its exit. Run after a completed e2e invocation; verify the new config/specs are typechecked and runtime output is outside ESLint/Prettier scope. Keep the Vitest result distinct from the Playwright smoke result. Do not treat a plain build as this gate.
- [ ] T015 Run and record each script verdict: `scripts/run-roslyn-analyzers.ps1`, `scripts/run-cyclomatic-complexity.ps1`, `scripts/run-jetbrains-inspectcode.ps1` with no arguments, `scripts/run-property-tests.ps1`, `scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and `scripts/run-mutation.ps1`. Report dotnet format --verify-no-changes as an auxiliary check with its own exit, separate from configured gate verdicts. Record `propertyTests: opt-out — test-infrastructure-only change introduces no domain invariants` on the task note before interpreting the property-test exit. That opt-out is not a pass. Report a scope-empty exit 2 as `SKIPPED (scope-empty)`, a harness-disabled gate as `SKIP`, and mutation not-applicable only when the script says so. An exit 1 or a gate that could not run blocks. Do not claim a C# mutation pass.
- [ ] T016 After `npm run e2e` is green, temporarily fail one assertion in a `web/e2e/*.e2e.ts` spec, confirm a nonzero exit and a retained screenshot and sanitized logs under `web/e2e/.runtime/`, restore that spec, and rerun `npm run e2e` to exit 0. Do not commit the broken assertion. Do not add a fourth spec file.
- [ ] T017 Repeat `npm run e2e` on a clean stack. Record that the repeat exits 0 against a fresh isolated database and queue for that invocation, and that it does not reuse the previous invocation's resources or volumes.
- [ ] T018 Record retry fixture isolation. The two seed imports run once per invocation, outside browser-worker retries. The held import uses a distinct folder path, derived from the reserved sample file, for the initial attempt and for the retry attempt. Prove a controlled failed first attempt releases/drains its hold and the retry re-arms independently; invocation cleanup releases all remaining holds. Do not require a fourth imported movie.
- [ ] T019 Record that the initial attempt's screenshots and sanitized compose and stub logs under `web/e2e/.runtime/` are preserved when the retry passes. Those logs must not contain credentials or provider keys.
- [ ] T020 Record generated-file and secret hygiene. Disposable credentials, settings, and fixtures stay under ignored `web/e2e/.runtime/`. Credentials, generated settings, and provider keys are absent from committed files and from logs kept for upload. Record the generated-secret-value scan of the upload allowlist, proving unsafe upload is blocked.
- [ ] T021 On a successful run and on a failed run, record scoped cleanup. Teardown removes only that invocation's resources and volumes. The owner's root `.env` and `docker/api.settings.json` are unchanged on both paths. Run controlled negative probes for nonliteral/non-loopback provider host, occupied port and unknown provider request; record named cause, nonzero exit and scoped cleanup. Do not commit injected failures.

---

## Excluded (never scheduled)

Production C#, public API or DTO changes, database schema, OpenAPI, generated clients, edits to `docker-compose.yml` or `docker/sample-library/`, visual regression, k6, playback execution, `Features:LocalPlay` enabled, `Process.Start`, a second direct dependency, a root `CONTEXT.md` edit, unrelated cleanup, and a fourth smoke spec. The ADR draft remains the Phase A decision record; canonical Phase B publication is explicitly authorized by T013a. A discovered need outside this list stops for a decision. It is not absorbed into a phase.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: Starts after Gate 1. Blocks Phase 2. Read-only.
- **Phase 2**: Depends on T002's recorded version. Blocks Phase 3.
- **Phase 3**: Depends on Phase 2. Blocks Phases 4–6.
- **Phases 4–6**: Depend on T009. The three spec files are independent of each other.
- **Phase 7**: Depends on Phases 4–6 so the job runs the three specs.
- **Phase 8**: Depends on Phases 2–7 and T013a being committed.

### User Story Dependencies

- **User Story 1 (P1)**: After Phase 3. No dependency on US2 or US3.
- **User Story 2 (P2)**: After Phase 3. Independently testable. Does not require the browse spec to have run.
- **User Story 3 (P3)**: After Phase 3. Independently testable. Retry paths come from T008, not from another spec.

### Parallel Opportunities

- T005 can run beside T004.
- T006, T007, and T008 can run together.
- T010, T011, and T012 can run together after T009.

### Parallel Example: Phase 3

```text
Task: "Add web/e2e/compose.e2e.yml"
Task: "Implement web/e2e/stub.mjs"
Task: "Implement web/e2e/fixtures.mjs"
```

### Parallel Example: User Stories

```text
Task: "Add the browse spec under web/e2e/*.e2e.ts"
Task: "Add the genre spec under web/e2e/*.e2e.ts"
Task: "Add the import spec under web/e2e/*.e2e.ts"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (evidence) and Phase 2 (tooling).
2. Complete Phase 3 (runner, stub, fixtures).
3. Complete Phase 4 (browse spec).
4. Stop and run that spec on a fresh stack.

The ticket's acceptance is not met until Phases 5–8 are done as well. US1 is the first checkpoint, not the closeout.

### Incremental Delivery

1. Evidence and tooling unlock the runner.
2. The runner unlocks the three specs, which can be added one at a time.
3. CI follows the three specs.
4. Gauge closes with gates and the restored negative proof.

## Notes

- [P] tasks use different files and do not wait on each other.
- [Story] labels map T010–T012 to US1–US3.
- `propertyTests: opt-out — test-infrastructure-only change introduces no domain invariants`
- The Playwright version, the wait-timeout number, the compose service key, and which of the four placeholders is a seed or the reserved file are chosen inside the constraints of `spec.md` FR-001 and FR-005. They are not named in this plan.
- Anvil commits each of Phases 2–7 as `DEV-334 - {phase title}` and stops. Do not start the next phase in the same ask.
- Spec filenames under `web/e2e/*.e2e.ts` are phase outputs. There are exactly three.
