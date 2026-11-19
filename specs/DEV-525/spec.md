# Feature Specification: Restore subtitle search and apply

**Feature Branch**: `feature/525-spec`

**Created**: 2026-11-19

**Status**: `gate1: provisional`

**Input**: Ticket DEV-525 (bug follow-up to DEV-501; parent DEV-490); grill-closed `brief.md` (Q1–Q3); `CONCLUSIONS.md`; Maestri notes `DEV-525`, `recon-DEV-525` (lines 9–12, 14–21, 26–31); `specs/PRODUCT.md`. Constitution cited only in the plan. Quill invents no decisions beyond the brief.

## Scope

**In scope** (`brief.md` Frozen scope; CONCLUSIONS Q1–Q3):

1. **Reproduce first**: Record baseline revision, exact steps, expected vs observed results, and sanitized evidence for subtitle search/apply failure in the running app before selecting production fix files.
2. **Restore search and apply**: Viewer can find a subtitle and apply it in the running app through the existing Subtitles flow.
3. **Preserve DEV-501 public contract**: Keep `GET /api/movies/{id}/subtitles/search`, `POST /api/movies/{id}/subtitles/download`, `GET /api/subtitles/languages`, existing DTO field sets, and HTTP statuses. Contract chain remains C# DTO → OpenAPI → generated TypeScript. Any proven need to change public API shape requires a charter §2.3(4) ruling before altering the contract.
4. **Browser regression**: Add a Chromium Playwright `web/e2e/*.e2e.ts` spec discovered by existing `web/playwright.config.ts` / `npm run e2e`, with minimal test-only subtitle provider wiring in the existing E2E harness behind the real application API, preserving the diagnosed failure on the pre-fix revision and recording pass-after evidence.
5. **Hosted CI**: The new spec runs in the existing `e2e` job displayed as `e2e-smoke` without adding npm/NuGet dependencies or editing `.github/workflows/ci.yml`.
6. **PR evidence**: Working-search screenshot in the PR; fail-before and pass-after regression receipts; recorded running-app reproduction and successful retest.

**Out of scope**: New NuGet/npm dependency; new project/layer/port; database schema change; `Features:LocalPlay` / `Process.Start` / secret hardcoding; inventing root cause or production fix files before reproduction; live-provider dependence in CI; workflow/job additions; anything outside the frozen scope (follow-up issue, not this round).

**Precedence**: Ticket text; brief Q1–Q3; `CONCLUSIONS.md`; recon facts; `specs/PRODUCT.md`. Quill invents no decisions.

## What the User Will See *(mandatory)*

- **Where**: Movie playback → Subtitles tab (`SubtitlesTab` / subtitle acquisition UI).
- **What the user sees or does**: Can search for remote subtitle candidates and successfully apply/download one so captions become usable, restoring the broken post-DEV-501 path.
- **What it needs to show up**: Default Compose running app with an authorized movie and valid subtitle acquisition configuration for search/download (no new Compose override required for the product path). CI regression uses ephemeral test-only provider fixture wiring, not live secrets.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a movie open in playback with the Subtitles tab available and acquisition configured, **When** the viewer runs subtitle search and applies a returned candidate, **Then** search returns usable results and the applied subtitle is active without inventing a new public contract.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reproduce the running-app failure (Priority: P1)

Operator reproduces subtitle search/apply failure in the running app, records baseline revision, steps, expected vs observed results, and sanitized evidence, and identifies the failing boundary before any production fix is chosen.

**Why this priority**: Brief and CONCLUSIONS Q1 require reproduction-first; fix files remain deferred until the failure is demonstrated.

**Independent Test**: Written reproduction record exists with baseline revision and observed failure before production edits.

**Acceptance Scenarios**:

1. **Given** the pre-fix revision in the running app, **When** the recorded subtitle search/apply steps are executed, **Then** the failure is observed and recorded with expected vs actual results and sanitized evidence.
2. **Given** that reproduction record, **When** diagnosis names the failing boundary, **Then** specific production fix files are selected only from that evidence (investigation points in `recon-DEV-525` lines 9–12 remain candidates, not asserted causes).

---

### User Story 2 - Fail-before browser regression with deterministic fixture (Priority: P1)

A new Playwright Chromium `web/e2e/*.e2e.ts` spec plus minimal test-only subtitle provider wiring in the existing E2E harness exercises real search and download/apply endpoints, preserves the diagnosed failure on the pre-fix revision, and yields a verified fail-before receipt.

**Why this priority**: Ticket Wanted/Done criteria require an e2e spec that fails on old behavior; CONCLUSIONS Q2 freezes the runner, CI job, and fixture approach.

**Independent Test**: On the pre-fix revision with the fixture, the new spec fails for the diagnosed trigger; no live provider secrets required.

**Acceptance Scenarios**:

1. **Given** the existing Playwright discovery (`web/playwright.config.ts`, `npm run e2e`), **When** the new `web/e2e/*.e2e.ts` file is added, **Then** it is included without editing `.github/workflows/ci.yml` or adding dependencies.
2. **Given** minimal test-only provider wiring in `web/e2e/run.mjs` and `web/e2e/stub.mjs` (and `web/e2e/fixtures.mjs` / `web/e2e/compose.e2e.yml` only if required), **When** the browser drives search and apply through the real API, **Then** the fixture is deterministic and uses ephemeral test-only configuration with zero live secrets.
3. **Given** the pre-fix revision and that fixture, **When** the new spec runs, **Then** it fails in a way that preserves the diagnosed trigger and produces a fail-before receipt.

---

### User Story 3 - Fix demonstrated cause and pass-after proof (Priority: P1) 🎯 MVP

Implement the smallest production fix for the demonstrated cause inside the existing vertical slice and generated web client, update only directly affected existing coverage as needed, then record pass-after Playwright evidence asserting returned search results and successful application.

**Why this priority**: Restores the user-visible search/apply path that the ticket exists to repair.

**Independent Test**: Same Playwright scenario passes after the fix; running-app retest of recorded steps succeeds.

**Acceptance Scenarios**:

1. **Given** the demonstrated cause, **When** the production fix is applied, **Then** existing DEV-501 routes, DTO fields, and statuses remain unchanged unless a separate §2.3(4) ruling authorizes a contract change.
2. **Given** the fix and the same E2E fixture/scenario, **When** the Playwright spec runs, **Then** it asserts a returned search result and successful subtitle application (pass-after receipt).
3. **Given** candidate coverage named in recon (`SubtitleSearchDownloadEndpointTests`, `SearchSubtitlesQueryHandlerTests`, `subtitlesTab.test.tsx`), **When** the diagnosis selects affected suites, **Then** only those directly affected existing tests are updated as required by the fix — no invented new framework.

---

### User Story 4 - Running-app verification, screenshot, and hosted CI (Priority: P2)

Re-run recorded reproduction steps successfully, attach a working-search screenshot to the PR, and show green hosted CI including execution of the new spec in `e2e-smoke`.

**Why this priority**: Completes ticket Done-when evidence after the functional restore and local regression proof.

**Independent Test**: Screenshot on PR; hosted CI green with `e2e-smoke` executing the new spec; running-app success record.

**Acceptance Scenarios**:

1. **Given** the fixed build in the running app, **When** recorded search/apply steps are re-run, **Then** search and apply succeed and a working-search screenshot is attached to the PR.
2. **Given** the branch on hosted CI, **When** the existing `e2e-smoke` job runs, **Then** it executes the new spec and the overall required CI result is green.

---

### Edge Cases

- Search or apply fails because acquisition settings are missing/invalid → preserve existing DEV-501 acquisition validation behavior (including request-time conflict responses); do not invent new status codes in this ticket.
- Provider/fixture unavailable or miswired in E2E → treat as harness defect to fix within the approved test-only file set; do not fall back to live provider credentials.
- Diagnosis shows a charter §2.3-covered change is required → stop and obtain/record the ruling before extending the plan (brief Frozen scope).
- Pre-fix revision cannot preserve the diagnosed failure under the fixture → stop; do not claim fail-before evidence.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Delivery MUST reproduce and record the running-app subtitle search/apply failure (baseline revision, steps, expected vs observed, sanitized evidence) before production fix selection.
- **FR-002**: After the fix, users MUST be able to search for subtitles and apply a candidate successfully in the running app.
- **FR-003**: Delivery MUST preserve the DEV-501 public search, download, and languages routes, DTO field sets, and HTTP statuses by default.
- **FR-004**: Any public API shape change required by evidence MUST obtain and record a charter §2.3(4) ruling before implementation.
- **FR-005**: Delivery MUST add a Chromium Playwright `web/e2e/*.e2e.ts` regression discovered by the existing runner and included in hosted `e2e-smoke` without new dependencies or CI workflow edits.
- **FR-006**: Delivery MUST add minimal test-only subtitle provider wiring in the existing E2E harness (`web/e2e/run.mjs`, `web/e2e/stub.mjs`, and `fixtures.mjs` / `compose.e2e.yml` only if required) routing the real API to a deterministic fixture with ephemeral test-only configuration and no live secrets.
- **FR-007**: The regression MUST preserve the diagnosed failure on the pre-fix revision (fail-before receipt) and pass after the fix with assertions for returned search results and successful application (pass-after receipt).
- **FR-008**: Delivery MUST re-verify recorded steps in the running app and include a working-search screenshot in the PR.
- **FR-009**: Delivery MUST record green hosted CI including execution of the new spec in `e2e-smoke`.
- **FR-010**: Production edits MUST stay in the existing vertical slice / generated web client; fix files are selected only after diagnosis.
- **FR-011**: If evidence shows a charter §2.3-covered change is required, work MUST stop for the applicable ruling before extending the plan.
- **FR-012**: Plan challenge and code review MUST require Risk, Standards, and Spec axes; every verified in-scope finding through Low must be fixed or carry an explicit written disposition that permits closure; cap is two rounds per loop; unresolved blockers at the cap report to Bernstein.
- **FR-013**: Affected backend and web scopes MUST record honest gate evidence (including `SKIPPED (scope-empty)`, `N/A`, and `Could not run` as distinct non-PASS outcomes). Delivery MUST verify that the generated OpenAPI and TypeScript contract matches the API and preserves the DEV-501 shape even when generated files have no diff; regenerate OpenAPI then TypeScript only when an authorized contract change requires it.

### Key Entities *(include if feature involves data)*

- **Subtitle search result**: Existing remote candidate returned by the preserved search contract; not redefined here.
- **Applied subtitle track**: Existing local/active track resulting from successful download/apply; not redefined here.
- **E2E provider fixture**: Test-only deterministic stand-in for subtitle provider responses used solely by the browser harness.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Recorded running-app reproduction exists before production fix commits.
- **SC-002**: After the fix, the same recorded steps succeed for search and apply in the running app.
- **SC-003**: Fail-before and pass-after receipts exist for the same Playwright scenario.
- **SC-004**: Hosted CI is green and shows the new spec executed under `e2e-smoke`.
- **SC-005**: PR includes a screenshot of working subtitle search.
- **SC-006**: Public DEV-501 search/download/languages contract shape remains unchanged unless a recorded §2.3(4) ruling says otherwise.
- **SC-007**: Review loops close only when every verified in-scope finding through Low is fixed or has an explicit written disposition that permits closure, within two rounds per loop.

## Assumptions

- Runtime failure cause is unknown at drafting time (`brief.md`); diagnosis during Build selects the smallest production fix.
- Investigation points in `recon-DEV-525` lines 9–12 are starting points, not asserted root-cause files.
- Existing E2E discovery and `e2e-smoke` job remain sufficient; no new runner or workflow is required (CONCLUSIONS Q2).
- No taste copy changes are in scope; no `ASSUMPTIONS.md` taste entries are required for this draft.
