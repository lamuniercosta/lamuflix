# Implementation Plan: Stop duplicate movie imports

**Branch**: `feature/522-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-522-spec`) | **Date**: 2026-11-17 | **Spec**: `specs/DEV-522/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q7); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-522`, `recon-DEV-522` (lines 24, 28–32, 41–75; HEAD `2c8f0504b3c2550c8fd655eb58457fa9658f3e25`); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: PLAN FROZEN — Keel plan adjudication, 2026-11-17; see PLAN_ADJUDICATION.md and brief.md authoritative amendments. Gate 1, investigation, Build, and runtime acceptance remain open.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `contracts/library-failure-fields.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` — decisions are frozen in brief Q1–Q7; no schema change; additive API fields are documented under `contracts/`.

## Summary

Stop duplicate library movies from being created when import already knows a library path/key or IMDb identity: skip/reuse the existing movie, keep unique indexes as the concurrency backstop, leave one row with no second enrichment enqueue, and preserve retry behavior. When IMDb identity collides only during enrichment on a distinct path, settle truthfully to Failed with `duplicate_imdb_id` and surface the canonical caller-safe reason through additive nullable `failureCategory` / `failureMessage` on existing library summary/detail responses, then generated OpenAPI/TypeScript, then existing MovieCard and BulkImportDialog UI. Block production edits on Q4 real-DB mechanism reproduction plus Rigger YouTrack receipt. No historical merge/cleanup, no new metadata lookup at import, no new dependency/project/port/schema/LocalPlay work.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Api`, `LamuFlix.Core`, `LamuFlix.Infrastructure`) + React 19 / TypeScript / Vite (`web/`) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing stack only — import handlers, EF movie repository, enrichment failure category, library endpoints, generated OpenAPI→TypeScript chain (`recon-DEV-522` lines 46–72). No new NuGet/npm dependency (`brief.md` Frozen scope; Q5).

**Storage**: Existing PostgreSQL/EF movie persistence. Unique indexes on `LibraryPath` / `LibraryPathKey` and filtered unique `ImdbId` / `ManualImdbId` remain; no migration or index relaxation (`recon-DEV-522` lines 53–55; brief Q5).

**Testing**: Existing xUnit unit/integration suites named in `recon-DEV-522` lines 69–72; Postgres Testcontainers for uniqueness/conflict translation and real serialized API JSON (separate from UI); Vitest + React Testing Library **component regressions** only in `web/src/library/BulkImportDialog.test.tsx` and `web/src/library/MovieCard.test.tsx` (T010/T012 — Failed safe reason + null fallback in both views; never browser e2e / never `web/e2e` files; Constitution IX `constitution.md:707-734` and stack `constitution.md:760` impose no browser-e2e mandate); property tests for identity/idempotency invariants per Q6. No new test framework/dependency.

**Target Platform**: Local API + Compose SPA; real-DB investigation evidence before Build fixes (`brief.md` Q4).

**Project Type**: Existing ports-and-adapters .NET API with feature-organised Core and explicit handlers; SPA consumes generated types only.

**Performance Goals**: No invented SLA. Idempotent skip/reuse must avoid duplicate enrichment enqueue; concurrency correctness proven on real DB.

**Constraints**: Charter §2.3 items deliberately ruled where applicable (Q3/Q5). Closed port list — stop rather than add a port (`recon-DEV-522` line 29). Enrichment state machine and caller-safe failure messages preserved (`recon-DEV-522` lines 30–31). Contract chain C# DTO → OpenAPI → TypeScript. No raw exception text in API/UI. Honest gate labels; never lower thresholds. Closing bar and 2×2 caps per Q7. Size M requires three review axes. Conditional file edits only — Q4 evidence selects exact patches (`brief.md` Affected-file responsibilities).

**Scale/Scope**: Size M; bounded import/enrichment/library-response/UI envelope listed below; ticket Done-when including root-cause evidence.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No current owner structural checkbox: ticket delivery is preserved, including actual historical-cause investigation. Mechanism-only acceptance would require a future owner checkbox.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; closed port list | **PASS** — reuse existing import/persistence/enrichment seams; if race-safe reuse cannot be supported on existing ports, stop needs recon/decision — do not add a port (`brief.md` Q5; recon line 29). |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — existing `ImportMovieFolderCommandHandler`, `ImportLibraryFoldersCommandHandler`, `ProcessEnrichmentCommandHandler` only; no MediatR; no new handler framework. |
| III (Typed query model) | MovieQuery | **PASS (design)** — existing summary/details projection models carry nullable persisted failure category via EfMovieCatalog, per Patron ca667dc7; no browse filter or new port. |
| IV (Enrichment state machine) | Status values; caller-safe categories; no raw exception text | **PASS** — preserve Pending/Enriched/NotFound/Failed; retain `duplicate_imdb_id` truthful Failed settlement; never persist/return raw exception text (recon 30–31, 58–59; brief Q2/Q3). |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no new error taxonomy required by grill; preserve existing import/library routes and status codes. |
| VI (Observability) | Safe diagnostics | **PASS** — sanitized investigation logs only; no secrets; raw exceptions stay in logs/traces, not API/UI. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay, secrets, or Process.Start change (`brief.md` Frozen scope). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS (design)** — four spec-local labels are prohibited as domain/code identifiers; failure fields express existing category Code/SafeDescription. Patron ca667dc7 requires no CONTEXT addition. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit orchestration; Testcontainers for constraints/translation; real API JSON (separate); Vitest + RTL component regressions for MovieCard/BulkImportDialog (T010/T012; no browser-e2e mandate at `constitution.md:707-734,760`); property identity/idempotency (brief Q6 + post-grill drafting clarification). |
| Tech stack / API docs | OpenAPI chain | **PASS** — additive library DTO fields per Patron Q3; regenerate OpenAPI then TypeScript; web consumes generated types only. |
| Governance / Static-Analysis Gates | Honest exits | Phase B: Roslyn, complexity (including applicable refactor-gate obligation), InspectCode, property tests, vulnerable packages, `dotnet format --verify-no-changes`, `dotnet test`, mutation via `scripts/run-mutation.ps1` without `--since` when production C# under `src/` is eligible, web gates for changed web files; never fold non-pass into PASS; never lower thresholds. |

### Complexity Tracking

No constitution violations requiring justification. Additive public library failure fields and AC-forced existing-file edits are authorized by Patron Q3/Q5. No new architectural layer.

## Approach

- **Phase A (this draft)**: Quill writes `spec.md` / `plan.md` / `tasks.md` / checklist / contracts note / receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon/PRODUCT only. Keel runs `/speckit-analyze` and independent reread. Plan challenge and Gate 1 are separate subsequent states. No Build work before Gate 1.
- **Phase B pickup (after Gate 1)**: Fresh drift recon as required by pipeline; then **Q4 investigation first** — real-DB reproduction, sanitized evidence, Patron ruling on evidence text, Rigger YouTrack receipt. Stop on failed evidence gate. Exact patches remain conditional on that evidence (`brief.md` Task ordering steps 3–5).
- **Task order (`brief.md` Task ordering / Q6)**:
  1. Spec Kit drafting + Keel analyze + plan challenge + Gate 1 (Phase A).
  2. Q4 investigation + Rigger receipt (blocks production work).
  3. Import identity lookup/reuse and duplicate-conflict translation in authorized import/persistence files; preserve unique indexes and retry semantics; concurrency proven on real DB.
  4. Prove late-IMDb Failed settlement; change enrichment settlement files only if reproduction proves repair is needed.
  5. Project Q3 fields on library responses; real API JSON tests; regenerate OpenAPI and TypeScript.
  6. Render canonical message in MovieCard / BulkImportDialog against generated types; Vitest + RTL component regressions (T010/T012) covering Failed safe reason and null fallback in both views — not browser e2e; no `web/e2e` files.
  7. Property tests for identity/idempotency; full configured backend/web/mutation gates with exact labels.
  8. Three-axis review under written closing bar (2×2 caps).
- **Do not**: historical merge/cleanup; external metadata at import; new dependency/project/port/layer/schema/index relaxation/LocalPlay; invent cause; invent public import-contract shape; claim Gate 1 / runtime acceptance from the plan freeze; lower gate thresholds; hand-edit generated OpenAPI/TS.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-522/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file
├── tasks.md
├── contracts/
│   └── library-failure-fields.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

No `research.md` / `data-model.md` / `quickstart.md` — grill decisions freeze the unknowns Spec Kit would research; no schema/data-model change; operational quickstart is the investigation + test strategy in brief/tasks.

### Source Code (authorized responsibility envelope — brief Q5; recon 46–72)

```text
# Import lookup/reuse and conflict handling
src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs
src/LamuFlix.Core/Features/Import/ImportLibraryFoldersCommandHandler.cs
src/LamuFlix.Infrastructure/Import/ScopedMovieFolderImporter.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs

# Uniqueness evidence (preserve; not a planned edit)
src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs

# Import transport — CONDITIONAL on proven need
src/LamuFlix.Api/Endpoints/ImportEndpoints.cs
src/LamuFlix.Api/Endpoints/BulkImportEndpoints.cs

# Late-failure settlement — CONDITIONAL on proven need
src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs
src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs

# Existing projection data path (Patron ca667dc7; recon projection lines 5–16)
src/LamuFlix.Core/Ports/MovieSummary.cs
src/LamuFlix.Core/Ports/MovieDetails.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs

# Additive failure projection (Q3)
src/LamuFlix.Api/Endpoints/LibraryResponses.cs
src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs

# Generated contracts (after C# DTO change)
web/src/api/openapi.json
web/src/api/types.ts

# Existing failure UI
web/src/library/MovieCard.tsx
web/src/library/BulkImportDialog.tsx

# Tests (recon-named; DB/API separate from UI component regressions)
tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs
tests/LamuFlix.UnitTests/Features/Import/ImportIdentityPropertyTests.cs  # in-memory FsCheck, new file if needed
tests/LamuFlix.UnitTests/Features/Import/ImportLibraryFoldersCommandHandlerTests.cs
tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs
tests/LamuFlix.IntegrationTests/ImportMovieFolderCommandHandlerPersistenceTests.cs
tests/LamuFlix.IntegrationTests/ImportLibraryCommandHandlerPersistenceTests.cs
tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs
tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs   # real serialized API JSON (T009)
web/src/library/BulkImportDialog.test.tsx                  # Vitest+RTL component regressions (T010/T012); not browser e2e
web/src/library/MovieCard.test.tsx                         # Vitest+RTL; existing test file to extend; not browser e2e / no web/e2e
```

**Structure Decision**: Stay inside the Q5 authorized envelope. These are permitted responsibilities, not an assertion every file needs a patch. Any tracked file outside this set, or any new public import-contract shape, needs a cited Patron ruling before edit. Stop rather than add a port.

## Test Strategy

| Family | Required evidence |
|---|---|
| Root cause | Historical log/DB investigation establishing actual trigger plus separate real-DB mechanism reproduction; sanitized evidence and durable regressions; Rigger verified ticket receipt; stop needs recon if historical evidence unavailable |
| Single/bulk path/key reruns | One persisted movie; skipped/already-present; no duplicate enrichment enqueue |
| Known IMDb across paths (when available to operation) | Reuse existing movie; one row; no second enqueue |
| Concurrent path / known IMDb | Real-DB uniqueness + conflict translation; one row; no second enqueue |
| Late IMDb collision | Failed + `duplicate_imdb_id` + canonical safe message; no auto delete/merge |
| Retry path | Failed import/enrichment retry semantics preserved independently of duplicate skipping |
| API JSON (separate from UI) | Actual serialized summary/detail via Testcontainers/`WebApplicationFactory` (T009) for Failed with available reason; nullable fallback; both fields null for Pending/Enriched/NotFound; no raw exception leakage |
| UI component regressions (T010/T012) | Vitest + React Testing Library only — Failed safe reason + null fallback in both MovieCard and BulkImportDialog failed section; no raw exception text; **never** browser e2e / executed browser smoke / `web/e2e/*.e2e.ts` (`brief.md` Post-grill drafting clarification; `constitution.md:707-734,760`) |
| Identity invariants | Property tests for identity/idempotency using repo conventions and existing semantics |
| Gates | Roslyn, complexity, InspectCode, property tests, vulnerable packages, format, full tests, mutation (eligible `src/` C# via `run-mutation.ps1` without `--since`), web gates for changed web files; exact exit-2 labels |

## Phase 0 / Phase 1 note

Per Spec Kit plan workflow: unknowns that would normally populate `research.md` are already ruled in brief Q1–Q7. Additive library failure fields are recorded in `contracts/library-failure-fields.md` from Q3 (not invented). No schema/data-model artifact. Quill opens no inventable-behaviour decisions; any gap is `needs decision:` / `needs recon:` in `DRAFTING_RECEIPT.md`. PLAN FROZEN is declared by PLAN_ADJUDICATION.md; Gate 1 is not claimed.

## Post-design Constitution Check

Re-checked after documenting the authorized envelope above: still PASS/N/A as in the Constitution Check table; no new port, handler framework, schema, LocalPlay, secrets, Process.Start, CONTEXT, or ADR obligations introduced by this plan. Additive library DTO fields remain the Patron-ruled Q3 API shape only.


## Adjudicated execution constraints

The authoritative amendments in brief.md and all dispositions in PLAN_ADJUDICATION.md supersede prior draft-only wording. Constitutional PASS entries describe design compliance, not executed gate results.

- T001 records actual historical trigger evidence, enqueue-versus-durable-save/conflict ordering, bulk commit/visibility granularity, and existing ImdbId/ManualImdbId carrier/applicability. Missing historical evidence stops implementation; mechanism-only does not satisfy Done-when 3.
- T003 counts enqueue calls on winner and losing conflict paths (at most one), proves same-request known-IMDb collisions do not abort remaining bulk items, and asserts skip/reuse returns the existing identity with no second row/Failed settlement.
- Phase translation matrix: import known identity collision -> skipped/already-present, no Failed; enrichment late collision -> Failed + duplicate_imdb_id + canonical safe message, no merge/delete. T004 and T007/T008 independently cover those paths. Retry must not loop, add a row, or regress to Pending; existing no-op or same Failed settlement is acceptable for non-retryable duplicate_imdb_id.
- Existing path/key semantics decide equivalence; variants still distinct follow late-discovery failure, without new normalization.
- Projection: persisted Movie.LastFailureCategory -> existing MovieSummary/MovieDetails via EfMovieCatalog -> LibraryResponses Code/SafeDescription for Failed only -> generated OpenAPI -> generated TypeScript -> existing UI. Patron ca667dc7 authorizes those three additional existing files; no new port/schema/layer.
- T013: in-memory FsCheck in tests/LamuFlix.UnitTests/Features/Import/ImportIdentityPropertyTests.cs (new test file permitted in existing project); T003 owns real DB invariants.
- SMOKE-001/002 component mapping is an explicit Patron-approved departure from browser e2e convention (CONCLUSIONS post-grill ruling, 01c6d482; Constitution IX/stack). T014 checks compatibility of existing import.e2e.ts/library-path.e2e.ts Failed-text assertions without claiming browser execution or authorizing edits outside the envelope.
- T014 web commands from web/: npm run lint; npx tsc --noEmit; npm run test:run; npm run build (recon web standards lines 23–24, run-web-gates.ps1:102–128). Format verification is required by ci.yml:59–60. No configured OpenAPI drift command was found in the authorized roots; record that limitation and verify regeneration output/diff, never claim an invented gate passed.
- T001 reproduction code is retained as named Testcontainers regressions for T003/T007; evidence contains no credentials, connection strings or unsafe identifying paths. The original Q5 envelope is extended solely by the brief amendments.
