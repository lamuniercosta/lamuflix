# Implementation Plan: Restore subtitle search and apply

**Branch**: `feature/525-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-525-spec`) | **Date**: 2026-11-19 | **Spec**: `specs/DEV-525/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q3 and plan challenge decisions), `CONCLUSIONS.md`, `PLAN_ADJUDICATION.md`, task note `DEV-525`, and recon note `recon-DEV-525` (lines 9–12, 14–21, 26–31). Recon revisions record historical inspections.

**Status**: PLAN FROZEN after round 1 adjudication and plan/task read-back. Gate 1, Build and runtime acceptance remain pending.

**Artifacts**: Public allowlist `specs/DEV-525/spec.md` (+ existing `brief.md`, `CONCLUSIONS.md`). Private operational records under `PRIVATE_TICKET_DIR` (`plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`). No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` — grill decisions freeze the unknowns Spec Kit would research; no schema change; public contract is preserved, not extended.

## Summary

Reproduce the running-app subtitle search/apply failure, add a Chromium Playwright regression with minimal test-only provider fixture wiring behind the real API that fails before the fix and passes after, apply the smallest production fix selected by diagnosis while preserving the DEV-501 route/DTO/status contract, then verify in the running app with a PR screenshot and green hosted `e2e-smoke` execution of the new spec.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Api`, `LamuFlix.Core`) + React 19 / TypeScript / Vite (`web/`) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing stack only — subtitle endpoints/handlers, generated OpenAPI→TypeScript client, Playwright E2E harness (`web/playwright.config.ts`, `web/e2e/run.mjs`). No new NuGet/npm dependency (`brief.md` Frozen scope; CONCLUSIONS Q2).

**Storage**: No schema change. Existing movie/subtitle persistence and sidecar publication paths remain as already implemented by DEV-501; this ticket does not authorize migrations.

**Testing**: Existing unit/integration/UI suites named in `recon-DEV-525` lines 9–11 as candidate coverage; new `web/e2e/*.e2e.ts` Chromium Playwright regression via `npm run e2e`; hosted job `e2e-smoke`. No new test framework/dependency.

**Target Platform**: Local Compose running app for reproduction/screenshot; existing E2E Compose/harness for deterministic browser proof; GitHub Actions `e2e-smoke`.

**Project Type**: Existing ports-and-adapters .NET API with feature-organised Core; SPA consumes generated types only.

**Performance Goals**: No invented SLA. Restore correct search/apply behavior; regression must be deterministic in CI without live providers.

**Constraints**: Preserve the DEV-501 public contract and generated client. Select production fixes after diagnosis. Stop for cited Patron ruling on charter §2.3 changes or edits outside the authorized envelope. No hardcoded secrets or weaker gate thresholds. Apply the AGENTS.md comments rule to E2E code too. Only AAA headers, intentional empty-block justifications, and narrowly scoped warning suppressions are exempt. Use the Q3 closure bar and two-round caps.

**Scale/Scope**: Size M; bounded to restore existing subtitle search/apply plus E2E/CI/screenshot evidence listed in frozen scope.

## Constitution Check

*Check during Drafting and again before Build.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox arose from adjudication.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; closed port list | **PASS** — reuse existing Subtitles feature and `ISubtitleProvider` / sidecar seams; do not add ports. If diagnosis cannot fix within existing seams, stop for ruling — do not invent a port. |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — existing `SearchSubtitlesQuery` / download handlers and endpoint mapping only; no MediatR; no new handler framework. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Enrichment state machine) | Status values | **N/A** — no enrichment status change. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — preserve existing DEV-501 statuses and error mapping; no new public error taxonomy unless §2.3(4) ruling. |
| VI (Observability) | Safe diagnostics | **PASS** — sanitized reproduction evidence only; no secrets in logs/receipts/PR. |
| VII (Configuration / LocalPlay / Process.Start) | Options; acquisition validation; LocalPlay gated | **PASS** — no LocalPlay/`Process.Start` change; no secret hardcoding; E2E uses ephemeral test-only provider configuration; preserve Principle VII search/download request-time acquisition validation behavior from DEV-501. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new glossary term or ADR; restores existing subtitle search/apply vocabulary. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — reproduction + unit/integration/UI as affected + Playwright E2E through real API with deterministic fixture (brief Q2; recon 26–31). |
| Tech stack / API docs | OpenAPI chain | **PASS** — verify OpenAPI and generated TypeScript match the API and preserve the DEV-501 shape even when generated files have no diff; regenerate OpenAPI then TypeScript only after an authorized contract change; web consumes generated types only. |
| Governance / Static-Analysis Gates | Honest exits | Use Test Strategy for gate scope and evidence. Report non-PASS outcomes with their exact labels. |

### Complexity Tracking

No constitution violations requiring justification. AC-forced E2E harness file edits are authorized by Patron Q2. Production file set remains diagnosis-selected within existing slice.

## Approach

- **Drafting**: Spec, plan and tasks derive from the brief. Complete read-only cross-artifact analysis and three-axis plan challenge before Gate 1. The owner merges the Gate 1 spec PR before Build starts.
- **Build pickup (after Gate 1)**: Follow the brief and plan challenge decisions in order.
  1. Reproduce and diagnose in the running app. Record the baseline, steps, observed UI and available API error mapping, and whether overlap/retry is implicated. Stop for a decision on a configuration- or data-only cause.
  2. Wire the deterministic fixture on the existing stub listener through a confirmed provider Options override. Use a runtime-generated dummy credential if required. Record the fixture-to-provider field mapping and reproduce the diagnosed trigger through the real API. Include overlapping actions if implicated. Stop if the override is unavailable or the fixture cannot establish a faithful fail-before.
  3. Update directly affected, authorized suites and implement the smallest fix. Check the existing safeguards at any changed remote subtitle boundary. Pass the same E2E scenario and fixture with both assertions intact.
  4. Retest in the running app and reopen diagnosis if it still fails. Prepare the sanitized screenshot and both regression receipts. After applicable local gates and pre-PR code review dispositions, open the stage-10 implementation PR, publish the evidence and review there, and record hosted e2e-smoke execution on its head. Delivery requires all receipts.
- **Limits**: Evidence governs the cause and edit set. Keep the existing CI workflow and dependencies, preserve the generated contract chain, and obtain rulings before protected changes. Report Gate 1 and runtime acceptance with their own evidence.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-525/                          # worktree records
├── brief.md                            # grill-closed (existing)
├── CONCLUSIONS.md                      # Patron Q1–Q3
├── PLAN_ADJUDICATION.md                # three-axis dispositions
└── spec.md                             # this specify output

PRIVATE_TICKET_DIR (DEV-525)/           # operational records
├── plan.md                             # this file
├── tasks.md
├── checklists/requirements.md
└── DRAFTING_RECEIPT.md
```

No `research.md` / `data-model.md` / `contracts/` / `quickstart.md` — decisions frozen in brief Q1–Q3; no schema/API extension; verification runbook lives in brief/tasks.

### Source Code (investigation / authorized edit envelope)

```text
# Investigation points (recon-DEV-525 lines 9–12) — candidates, not asserted fix set
src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs
src/LamuFlix.Core/Features/Subtitles/SearchSubtitlesQueryHandler.cs
web/src/features/playback/SubtitlesTab.tsx
web/src/features/playback/useSubtitleAcquisition.ts

# Generated contract consumption (preserve; regenerate only if C# DTO must change under ruling)
web/src/api/openapi.json
web/src/api/types.ts

# E2E regression + fixture (brief Approach step 2; recon 26–31)
web/e2e/*.e2e.ts                         # new regression file (mechanical name in tasks)
web/e2e/run.mjs
web/e2e/stub.mjs
web/e2e/fixtures.mjs                     # only if required
web/e2e/compose.e2e.yml                  # only if required
web/playwright.config.ts                 # discovery reference; not expected to need edit

# Candidate existing coverage (recon lines 9–11) — update only if diagnosis says affected
tests/LamuFlix.IntegrationTests/SubtitleSearchDownloadEndpointTests.cs
tests/LamuFlix.UnitTests/Features/Subtitles/SearchSubtitlesQueryHandlerTests.cs
web/src/features/playback/subtitlesTab.test.tsx

# CI discovery reference (do not edit per Q2)
.github/workflows/ci.yml
```

**Structure Decision**: Stay inside the existing Subtitles vertical slice, generated client, and E2E harness files named by brief/recon. Exact production patches remain conditional on reproduction evidence. Any tracked file outside this envelope, or any §2.3-protected change, needs a cited Patron ruling before edit.

## Test Strategy

| Family | Required evidence |
|---|---|
| Running-app reproduction and retest | Baseline revision, steps, expected vs observed UI/error mapping and overlap/retry disposition. Retest the same steps and confirm the failure clears. Reopen diagnosis if the fixture passes but the app fails. Identify each run by revision, timestamp and run ID. Isolate or serialize local runs with shared resources. |
| Fail-before E2E | Verify the assertion-relevant provider field mapping and establish the same trigger on the baseline. Identify the failed assertion in the receipt. If search fails first, apply may not run. Both assertions stay in the scenario. |
| Pass-after E2E | Same unchanged scenario with both assertions passes after the fix |
| Affected unit/integration/UI | Edit only directly affected authorized suites. Record existing missing/invalid acquisition settings checks and conflict results. Verify relevant safeguards if remote subtitle download, write or rendering behavior changes. |
| Contract drift | Run the existing OpenApiContractTests and compare the current API with checked-in OpenAPI for DEV-501 search, download and languages, including route, DTO and statuses. Generate TypeScript from checked-in OpenAPI to temporary output and compare to committed types, or use an equivalent generator check. Record commands and results even with no diff. Commit regenerated OpenAPI then TS only for an authorized contract change. |
| Backend / C# gates | Record LamuFlix.ArchitectureTests and `dotnet format --verify-no-changes`. When any C# changes, run Roslyn, cyclomatic complexity (including applicable `-Threshold 6` refactor obligation), and InspectCode on every changed C# file including tests. Record `SKIPPED (scope-empty)` for empty C# scope. Record unit/integration, property and vulnerable-package gates as applicable. Run mutation via `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` when production C# under `src/` is eligible. Keep `SKIPPED (scope-empty)`, `N/A` and `Could not run` distinct from PASS. |
| Web gates | When `web/` changes, record lint, typecheck and unit tests. Confirm typecheck includes the new E2E file or run the applicable existing E2E TypeScript project check separately. |
| Hosted CI | Green implementation PR head run including `e2e-smoke` executing the new spec. |
| PR screenshot + receipts | Verify all three sanitized artifacts before attaching the working-search screenshot and both regression receipts to the stage-10 implementation PR. |
| Review | Risk, Standards and Spec for the pre-Gate 1 plan challenge and for code review. Fix or explicitly dispose every verified in-scope finding through Low within two rounds per loop. Record pre-PR code review dispositions, then publish and verify them on the implementation PR. |

## Drafting artifacts

Spec Kit research and data-model artifacts are omitted. Patron Q1–Q3 settle scope, the E2E approach and closure rules. Build establishes the runtime cause and fix files.
