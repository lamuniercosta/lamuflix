# DEV-525 — Subtitle search and apply brief

**Worktree:** `F:/Dev/LamuFlix.worktrees/feature-525-spec` · `feature/525-spec` · Size M  
**Decision record:** [CONCLUSIONS.md](CONCLUSIONS.md). The runtime failure and its cause remain to be established during implementation.

## Frozen scope

Reproduce and record the running-app failure, diagnose it, and restore the existing subtitle search and apply flow. Verify that the user can find a subtitle and apply it in the running app. Add a browser regression that demonstrates the old failure and passes with the fix, run it in the existing CI job, and include a screenshot of working search in the PR. Preserve the DEV-501 search, download, and languages routes, DTO fields, and HTTP statuses. Anything else is a follow-up issue, not a finding in this round.

Use the existing vertical slice and generated web client. If evidence shows that the fix requires a change covered by charter §2.3, obtain and record the applicable ruling before extending the plan. The contract chain is C# DTO → OpenAPI → generated TypeScript.

## Approach, files, and task order

1. **Reproduce in the running app.** Record the baseline revision, exact steps, expected and observed results, and relevant sanitized evidence. Locate the failing boundary and identify the smallest production fix before selecting production files. The search trace in `recon-DEV-525` lines 9–12 names `src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs`, `src/LamuFlix.Core/Features/Subtitles/SearchSubtitlesQueryHandler.cs`, `web/src/features/playback/SubtitlesTab.tsx`, and `web/src/features/playback/useSubtitleAcquisition.ts` as investigation points.
2. **Prove the regression before fixing it.** Add a `web/e2e/*.e2e.ts` Chromium Playwright spec. Add the minimum test-only subtitle provider wiring in `web/e2e/run.mjs` and `web/e2e/stub.mjs`, with `web/e2e/fixtures.mjs` and `web/e2e/compose.e2e.yml` available if required. Route the real application API to the deterministic fixture with ephemeral test-only configuration. Exercise search and successful application in the browser through the real search and download endpoints. Preserve the diagnosed trigger and record the expected failure on the pre-fix revision. Existing discovery through `web/playwright.config.ts` and `npm run e2e` includes the new spec in the `e2e` job displayed as `e2e-smoke` in `.github/workflows/ci.yml`. Recon basis is lines 26–31.
3. **Fix the demonstrated cause.** Select the production edit and directly affected existing coverage from the diagnosis. Candidate coverage includes `tests/LamuFlix.IntegrationTests/SubtitleSearchDownloadEndpointTests.cs`, `tests/LamuFlix.UnitTests/Features/Subtitles/SearchSubtitlesQueryHandlerTests.cs`, and `web/src/features/playback/subtitlesTab.test.tsx` (recon lines 9–11). Run the same Playwright scenario against the fix and record the successful search and apply assertions.
4. **Verify the running app and publish evidence.** Re-run the recorded reproduction steps, confirm search and apply, and capture a screenshot of working search for the PR. Record hosted execution of the new spec in `e2e-smoke` and attach the matching pre-fix and fixed regression receipts.

## Closing bar and gate expectations

Plan challenge and code review each require Risk, Standards, and Spec. Close each loop only when every verified in-scope finding, including Low, is fixed or has an explicit written disposition that permits closure. The cap is **two rounds per loop**. Report unresolved blockers at the cap to Bernstein.

For backend production C# changes, record .NET analyzer, cyclomatic complexity, InspectCode, unit and integration, property-test, vulnerable-package, and mutation gate evidence. For web changes, record lint, typecheck, and unit-test results. Verify OpenAPI and generated TypeScript contract drift. Report `SKIPPED (scope-empty)`, `N/A`, and `Could not run` as distinct outcomes.

Delivery also requires the recorded running-app reproduction and successful retest, the regression's fail-before and pass-after receipts, green hosted CI with execution of the new spec in `e2e-smoke`, and the working-search screenshot in the PR.

## Plan challenge decisions

These decisions implement Q1–Q3. See [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md) for the three-axis dispositions.

- Record the observed UI error and available HTTP status or mapping. State whether overlap or retry is implicated. Stop for a decision if the cause is configuration or data only. A fixture pass with a running-app failure reopens diagnosis.
- Extend the existing stub listener. Verify the provider endpoint override via existing Options and stop for Patron if the seam is unavailable. Use a runtime-generated dummy credential if needed. Map assertion-relevant fields to the provider contract consumed by the app and tie the expected failure to the diagnosed trigger. Stop when fixture fidelity or same-trigger failure cannot be established. Include overlapping actions in the regression only when implicated.
- Verify relevant existing safeguards at any changed remote subtitle content boundary and preserve DEV-501 request-time acquisition conflicts using existing coverage. Keep test edits inside the authorized envelope.
- Check the browser spec under the applicable E2E TypeScript configuration. Verify the API/OpenAPI with existing contract tests and an API/operation comparison, then verify generated TypeScript against OpenAPI even for a clean tracked diff.
- Prepare the sanitized screenshot and both regression receipts for the stage-10 implementation PR. Record review dispositions before opening, then publish on that PR and capture hosted E2E for its head. Identify local and hosted runs separately. Isolate or serialize local runs that share stack resources.

## Grill record

### Q1 · Scope and reproduction

**Keel:** Should we freeze scope to restoring the existing subtitle search in the running app, preserving the existing route, DTO and status contract, and put reproduction and diagnosis first? I recommend that scope because DEV-525 note lines 7-13 describe a bug follow-up and recon lines 9-12 and 23-24 locate the existing path but leave the cause open. This bounds delivery and defers choosing specific fix files until the failure is demonstrated. Please cite the ticket, PRODUCT, and charter as applicable, flag any unresolved ticket criteria, and confirm the decision record location specs/DEV-525/CONCLUSIONS.md.

**Patron:** [Approved in Q1](CONCLUSIONS.md). Scope includes search and apply in the running app. Reproduce and record the failure before fixing it. Preserve the DEV-501 route, DTO, and status contract. Select specific fix files after diagnosis. The ticket also requires a fail-before E2E spec, CI execution, and a screenshot of working search in the PR. The decision record location is confirmed. Basis is DEV-525 Wanted 1–3 and Done when 1–3, the task note, recon lines 9–24, PRODUCT §§2–5, and charter §2.3.

### Q2 · End-to-end regression

**Keel:** Should the regression use the existing Chromium Playwright runner and e2e-smoke CI job with a deterministic provider fixture behind the real application API? I recommend adding a web/e2e/*.e2e.ts spec plus the smallest test-only fixture wiring in the existing harness, asserting a returned search result and successful application, and requiring a fail-before/pass-after receipt for the same scenario. This exercises the browser, API, and affected application behavior without relying on live-provider availability in CI. It adds fixture setup because recon-DEV-525 lines 26-31 found none. Require the fixture to preserve the diagnosed failure on the pre-fix revision and capture the ticket screenshot in the running app.

**Patron:** [Approved in Q2](CONCLUSIONS.md). Use existing Chromium Playwright discovery and hosted `e2e-smoke`. Add a matching browser spec and minimal test-only subtitle provider wiring behind the real API. Preserve the failure with that fixture before production edits, then record pass-after evidence and take the PR screenshot in the running app. Basis is DEV-525 Wanted 1–3 and Done when 1–3, recon lines 26–30, PRODUCT §5, and charter §2.3 items 1, 5, and 6.

### Q3 · Review closure and verification

**Keel:** Should the Size M review close only once every verified finding, including Low, is fixed or has an explicit written disposition that permits closure, with Risk, Standards, and Spec reviews required in both plan challenge and code review, and a cap of two rounds per loop? I recommend this closure rule and cap because it forces every in-scope finding to be accounted for while bounding review churn. At the cap, report unresolved blockers to Bernstein. Require gate evidence for the affected backend and web scopes, contract drift checks, the ticket runtime and screenshot receipts, and hosted e2e-smoke execution.

**Patron:** [Approved in Q3](CONCLUSIONS.md). Require all three axes for plan challenge and code review, disposition of every verified in-scope finding through Low, and at most two rounds per loop. Report remaining blockers to Bernstein. Require affected backend and web gates, contract drift verification, running-app evidence, red and green browser regression receipts, green hosted CI including `e2e-smoke`, and the PR screenshot. Basis is task-pipeline §2.2, DEV-525 Wanted 1–3 and Done when 1–3, the task note, PRODUCT §§1–5, charter §2.3(4), the repository verification commands, and recon lines 27–28. Patron confirmed shared understanding and grill closure.
