# Feature Specification: DEV-334 Browser smoke tests for browse, filter, and import

**Feature Branch**: `feature/334-spec`

**Created**: 2026-10-19

**Status**: gate1: provisional

**Input**: Ticket DEV-334 (size L); settled grill brief `specs/DEV-334/brief.md` (grill closed at Q5/12); facts from notes `recon-DEV-334` and `recon-DEV-334-2`; Patron rulings in `specs/DEV-334/CONCLUSIONS.md`. No decisions beyond `brief.md`.

## Clarifications

### Session 2026-10-19

The grill is closed. This session records the accepted rulings and asks nothing further. Each answer is already the body of this spec.

- Q: How does the compose stack avoid real OMDb calls while genre filtering still works? → A: A test-only compose overlay under `web/e2e/` adds a Node built-in HTTP stub that shares `network_mode: service:api`. Generated settings set `Omdb:BaseUrl` to `http://127.0.0.1:<port>` in that network namespace (brief Q1).
- Q: How are the browse, genre, and Pending results made deterministic? → A: Generated per-movie folders from the four committed DEV-325 placeholders, two seeded genre-bearing movies through the existing import API with immediate canned responses, one reserved file whose provider response is held until Pending is visible, three specs run serially on a fresh stack (brief Q2).
- Q: What may the change add and touch? → A: Only `@playwright/test`, exact-pinned after registry evidence, plus the named `web/e2e/` runner files and an additive CI job. Disposable files stay under ignored `web/e2e/.runtime/`. `Features.LocalPlay` stays false (brief Q3).
- Q: What do the three specs and CI prove? → A: Seeded grid, real genre-control inclusion and exclusion plus `genreIds` in the URL, and ImportDialog submission with POST 202 and a visible Pending row. CI retries once, keeps failure screenshots and sanitized logs, and a temporary local failure proves the red path (brief Q4).
- Q: Where does the work stop? → A: Closing bar, frozen Q1–Q4 scope, two review rounds, at most two fix commits per round, property-test opt-out recorded and not treated as pass, and the Phase 2 then Phase B order in the brief (brief Q5).

## User Scenarios & Testing *(mandatory)*

The actor is a developer, or the pull-request CI job, running the browser smoke suite against an isolated compose stack.

### User Story 1 - Seeded browse grid on an isolated stack (Priority: P1)

A developer runs the smoke suite locally. The suite starts a disposable compose stack, seeds two distinguishable genre-bearing movies through the existing import path, and a headless Chromium spec shows those movies on the browse grid. The developer's existing root environment file and API settings file stay as they were. The run does not call the real metadata provider.

**Why this priority**: The ticket's first scenario, and the first proof that the isolated stack, the stub, and seeding work.

**Independent Test**: Run only the browse spec against a fresh isolated stack. The grid shows the two seeded movies and the run exits 0. No other spec is required first.

**Acceptance Scenarios**:

1. **Given** a machine where port 3000 is free, **When** the local smoke command runs, **Then** it generates credentials, settings, and fixtures under the ignored runtime directory, starts the original compose stack plus the test overlay as its own compose project, and waits until the stub and the stack are healthy before seeding.
2. **Given** that ready stack, **When** the two seed imports finish, **Then** each seeded movie is observable and its genres are available before any browser spec starts.
3. **Given** those two seeded movies, **When** the browse spec opens the library, **Then** the browser shows both movies on the grid.
4. **Given** the run ending in success or failure, **When** teardown finishes, **Then** only that invocation's resources and volumes are removed, and the developer's root environment file and `docker/api.settings.json` are unchanged.

---

### User Story 2 - Genre filtering updates the grid and the URL (Priority: P2)

The developer exercises the real genre control. The grid keeps the seeded movie that belongs to the chosen genre, drops the seeded movie that does not, and the page URL carries that genre's `genreIds` value.

**Why this priority**: The ticket's filter scenario. It depends on enrichment having persisted genres, which the stub supplies for the seeded movies.

**Independent Test**: Run only the genre spec against a fresh stack whose two seeded movies have distinguishable persisted genres. Inclusion, exclusion, and the URL can be judged without the browse spec or the import spec.

**Acceptance Scenarios**:

1. **Given** two seeded movies whose persisted genres differ enough that one genre contains one of them and not the other, **When** the spec activates that genre through the real genre control, **Then** the grid includes the matching seeded movie and excludes the other.
2. **Given** that same activation, **When** the browser URL updates, **Then** it carries the corresponding `genreIds` value for the selected genre.
3. **Given** this spec, **When** it runs in the same invocation as the other specs, **Then** its result does not depend on those specs having run first.

---

### User Story 3 - Folder import shows a Pending row (Priority: P3)

The developer submits the real import dialog for a reserved sample folder. The browser shows a Pending row for that import while the stub is still holding the provider response. The import request itself returns 202.

**Why this priority**: The ticket's import scenario. The hold is what keeps Pending observable despite the live enrichment consumer.

**Independent Test**: Run only the import spec against a fresh stack. Submit the reserved folder, observe POST 202 and a visible Pending row, then release the held response during cleanup.

**Acceptance Scenarios**:

1. **Given** a reserved sample folder whose provider response is held, **When** the spec submits it through the real import dialog, **Then** the import POST returns 202 and the browser shows a Pending row for that import before the response is released.
2. **Given** a retry of that spec in CI, **When** the retry starts, **Then** it uses a distinct folder path derived from the same reserved sample file, and the two immediate seed imports are not repeated.
3. **Given** cleanup after success or failure, **When** the invocation tears down, **Then** the held response is released and only this invocation's resources are removed.

### Edge Cases

- Port 3000 is already taken. The run stops before it replaces anything the developer already has open.
- The effective provider base URL is not loopback. The run stops before seeding or browser specs.
- The stub receives a provider request it does not expect. The run fails. The stub does not forward that request and does not fall back to the real provider.
- A dead loopback port or a junk provider key is not a substitute for the stub. Seeded movies must actually receive genres, and the import spec must observe Pending rather than Pending-or-Failed.
- The import spec's wait is bounded and tied to the request or the visible row. It does not use a fixed sleep.
- CI retries the browser specs once. Seeding of the two movies happens once, outside those retries. The failed attempt's screenshot and sanitized logs are kept even when the retry passes.
- A local negative check fails one assertion on purpose, keeps the failure screenshot and sanitized logs, then restores the spec and passes. That check does not add a fourth smoke spec to the tree.
- Credentials, generated settings, and provider keys do not appear in committed files or in uploaded logs.
- The three specs run one at a time. Each spec's assertions stand on a fresh stack without assuming another spec's order.
- The four committed sample placeholders are the only media sources. The specs assert two seeded movies and one held import. They do not require a fourth imported movie.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The web package MUST add only `@playwright/test` as a direct dev dependency, exact-pinned to the highest stable Node-24-compatible release that was published at least 14 days before selection. Selection MUST use registry publication-date and version evidence gathered before installation. This draft does not name that version. The lockfile MUST be updated by the package manager, not by hand. `npm run e2e` MUST be the local and CI entry point.
- **FR-002**: New implementation files MUST be `web/playwright.config.ts`, `web/e2e/run.mjs`, `web/e2e/stub.mjs`, `web/e2e/fixtures.mjs`, `web/e2e/compose.e2e.yml`, `web/e2e/.gitignore`, `web/e2e/tsconfig.json`, and exactly three `web/e2e/*.e2e.ts` smoke specs. Playwright MUST match those specs explicitly so the existing Vitest run does not collect them. Existing-file edits MUST stay within `web/package.json`, `web/package-lock.json`, `web/eslint.config.js` and `web/.prettierignore` (runtime ignores only), and an additive job in `.github/workflows/ci.yml`. The e2e TypeScript config MUST cover Playwright config/specs and its typecheck MUST run through the existing build script. Phase B MUST publish `docs/adr/0023-compose-browser-smoke-isolation.md` from the draft, changing only Status to Accepted and recording a status-only diff proof. Planning documents for this ticket stay under `specs/DEV-334/`.
- **FR-003**: `npm run e2e` MUST generate disposable credentials, settings, and fixtures under ignored `web/e2e/.runtime/`, pass a generated env-file, and compose the original DEV-325 stack plus `web/e2e/compose.e2e.yml` as an isolated project with an explicit settings-mount override. It MUST refuse to start when something else owns `localhost:3000`. It MUST wait for stub readiness and stack health before seeding or browser specs. On every exit path it MUST remove only that invocation's resources and volumes, after failure evidence is preserved. It MUST NOT overwrite the root `.env` or `docker/api.settings.json`.
- **FR-004**: The overlay MUST add an OMDb-compatible stub implemented with Node's built-in HTTP, sharing `network_mode: service:api`. Generated settings MUST set `Omdb:BaseUrl` to `http://127.0.0.1:<port>` in that network namespace and MUST set `Features.LocalPlay` to false. The runner MUST reject a non-loopback effective provider base URL before seeding or tests. The stub MUST NOT forward or fall back to the real provider, MUST fail the run on an unexpected provider request, and MUST redact keys from request evidence and logs. Hold and release MUST stay on the stub. The application MUST NOT gain an endpoint for this control. The stub MUST bind to literal 127.0.0.1 with no published control/provider port; host runner and attempt hooks MUST use bounded docker compose exec -T into the stub service for loopback control and explicit unexpected-request status. The runner MUST fail nonzero on that status, even after successful browser assertions. The effective provider host MUST be literal 127.0.0.1; hostnames and other addresses MUST be rejected.
- **FR-005**: Fixtures MUST be per-movie folders generated from the four committed DEV-325 placeholder files and mounted read-only through the existing `LIBRARY_PATH`. Each imported folder MUST contain a top-level video. The runner MUST seed exactly two distinguishable genre-bearing movies through the existing `POST /api/movies/import`, with immediate canned stub responses, and MUST wait until those movies and their genres are observable before browser assertions. One reserved placeholder MUST supply the held-import folders. The three specs MUST NOT require a fourth imported movie.
- **FR-006**: There MUST be exactly three headless Chromium smoke specs. Browse MUST show the seeded movie grid. Genre filtering MUST use the real genre control and MUST show inclusion of the matching seeded movie, exclusion of the other, and the corresponding `genreIds` URL state. Import MUST submit the real import dialog, observe the real POST returning 202, and show the browser-visible Pending row before the held provider response is released. Setup MAY call the existing import API. Specs MUST NOT intercept application calls in the browser and MUST NOT replace the backend with a browser mock. TypeScript that names an application contract MUST use the generated API types and MUST NOT add a handwritten duplicate.
- **FR-007**: The three specs MUST run serially on a fresh isolated database and queue per invocation, and each spec's assertions MUST hold without depending on spec order. The two seed imports MUST run once per invocation, outside browser-worker retries. The held import MUST use a distinct folder path, derived from the reserved sample file, for the initial attempt and for the retry attempt. Each attempt MUST arm before submitting, release in finally after Pending observation or failure, and await drain before retry rearm. Invocation finally MUST release every outstanding hold.
- **FR-008**: Waits MUST be bounded and synchronized to requests or events. The import assertion MUST be the Pending row, not Pending-or-Failed, and MUST NOT use a fixed sleep. One named wait-budget source in run.mjs MUST be passed to the stub and specs.
- **FR-009**: CI MUST add a blocking pull-request job that uses Node 24 from `web/.node-version`, installs from the lockfile, installs Chromium with its operating-system dependencies, and runs `npm run e2e`. Browser retries MUST be 1 in CI and 0 locally. The job MUST be Chromium only and MUST NOT keep visual baselines. Failure screenshots and sanitized compose and stub logs MUST be retained as CI failure artifacts, including evidence from an attempt that failed and then passed on retry. Those artifacts and the local runtime output MUST stay within the ignored runtime directory except for the CI upload of failure evidence. CI MUST use a pull_request guard, an explicit bounded timeout, minimum permissions under existing policy, existing action major pins and explicit retention-days. An always-run upload MUST include only the sanitized evidence allowlist, including retry-pass failures; generated settings and compose-config dumps MUST NOT be retained. Disposable credentials MUST use Node crypto.randomBytes. Generated database/queue credentials and provider keys MUST be redacted and the upload allowlist MUST be scanned for generated secret values before upload; unsafe upload MUST be blocked.
- **FR-010**: During verification, a controlled local smoke assertion failure MUST show a nonzero exit and retention of the failure screenshot and sanitized logs. The failure MUST then be restored and the suite MUST pass. The tree MUST NOT gain a fourth smoke spec.
- **FR-011**: The change MUST NOT edit production C#, public API or DTO shapes, database schema, OpenAPI, generated clients, the base compose file, or the committed sample library. It MUST NOT add visual regression, k6 load tests, playback execution, a dependency swap, a root glossary change, or unrelated cleanup. The Phase 2 step 6 gate/safety refinements and ruled canonical ADR publication are included; anything beyond that amended scope is a follow-up issue.
- **FR-012**: Verification MUST keep the web unit suite and this smoke suite distinct. Gauge MUST run controlled negative probes for provider-host refusal, busy port, unknown provider request, secret-value artifact scanning, failed-attempt release/retry rearm and scoped teardown on success/failure. These boundary probes are the ruled verification approach instead of helper unit extraction. The web gate MUST pass after a completed e2e run with runtime output outside lint/format scope. dotnet format MUST be reported separately as an auxiliary check. Property tests MUST be recorded as `propertyTests: opt-out — test-infrastructure-only change introduces no domain invariants`. That opt-out is not a pass. Scope-empty analyzer or mutation results MUST be reported as `SKIPPED (scope-empty)`. A harness-disabled gate MUST be reported as `SKIP`. Mutation `NOT APPLICABLE` is only the script's configured-exclusion verdict. An exit 1 or a gate that could not run blocks. No C# mutation pass is claimed for a change with no production C#.

### Key Entities

- **Smoke invocation**: One isolated compose project with a fresh database and queue, generated credentials, settings, and fixtures, and a teardown that removes only that invocation.
- **Provider stub**: The loopback OMDb-compatible stand-in. It answers the two seed imports immediately, holds the reserved import until Pending has been seen, and never calls the real provider.
- **Seeded movie**: One of the two genre-bearing movies created through the existing import API before the browser specs start.
- **Held import**: The reserved sample, copied to a distinct folder per attempt, whose provider response stays withheld until the Pending row is visible.
- **Failure evidence**: Screenshots and sanitized compose and stub logs for a failed attempt, kept before teardown and uploaded by CI on failure.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A local `npm run e2e` against a free port 3000 exits 0, shows both seeded movies on the browse grid, and leaves the developer's root `.env` and `docker/api.settings.json` unchanged.
- **SC-002**: The genre spec, using the real genre control, includes one seeded movie, excludes the other, and puts the matching `genreIds` value in the URL.
- **SC-003**: The import spec observes HTTP 202 from the real import POST and a browser-visible Pending row for the held folder before that provider response is released.
- **SC-004**: The three specs run serially on a fresh stack, pass in either order, and a CI retry reuses the original two seeds while importing a distinct reserved-file folder.
- **SC-005**: The CI job fails the pull request when a smoke spec is red, retries once, and retains the failure screenshot and sanitized logs, including when a later retry passes.
- **SC-006**: The effective provider base URL is loopback, the stub makes no outbound provider call, an unexpected provider request fails the run, and provider keys are absent from committed files and uploaded logs. `Features.LocalPlay` is false in the generated settings.
- **SC-007**: A temporary local assertion failure exits nonzero and keeps its screenshot and sanitized logs; after that failure is restored, the suite exits 0 and the tree still contains three smoke specs.
- **SC-008**: Required applicable gates have a script verdict. Scope-empty skip, harness skip, mutation not-applicable, and the recorded property-test opt-out stay distinct from pass. Exit 1 or a gate that could not run fails the closeout.

## Assumptions

- DEV-321, DEV-322, DEV-325, and DEV-80 are present at the intake pin `06aedfeabc9e26ecc0348c71ca9983a2a8d27a65`. Phase B checks those flows for drift before implementation. This draft does not treat that pin as proof of a later head.
- Browse, genre, import, and Pending keep their existing meanings. No new domain term and no taste assumption is introduced. There is no `ASSUMPTIONS.md` for this ticket.
- The Playwright version is selected in Phase B from registry evidence. This spec does not choose a version, a wait-timeout number, a compose service key, or which of the four placeholders fills which fixture role.
- The configuration-only isolation ADR is drafted under specs/DEV-334/; Phase B publishes the canonical copy under the ruled status-only change.
