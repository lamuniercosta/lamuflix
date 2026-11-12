# Implementation Plan: Movie details and recommendation endpoint latency

**Branch**: `feature/483-spec` | **Date**: 2026-11-12 | **Spec**: `specs/DEV-483/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q6) including Plan Challenge Round 1 Adjudication ruling; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; `PLAN_ADJUDICATION.md` fix list 1–10; Maestri notes `DEV-483`, `recon-DEV-483` (lines 11–22, 25–29, 34–49); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Challenge-repair round 1 — Quill applying `PLAN_ADJUDICATION.md` fix list 1–10 after Patron-approved dispositions in `brief.md` / `CONCLUSIONS.md`. PLAN FROZEN withheld pending Keel independent reread. Gate 1, implementation, runtime acceptance, and merge remain distinct and open. Quill does not declare freeze or Gate 1.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — behaviour/contracts are existing and frozen; root cause and fix selection are Phase B evidence work, not inventable design artifacts.

## Summary

Diagnose and remediate multi-second latency on movie details and both recommendation endpoints on the idle 17-movie Compose stack. Reproduce via the Q2 Invoke-WebRequest protocol through web port 3000, establish an evidence-backed causal chain across the Q3 candidate seams, apply only the minimal proven causal fix, preserve public contracts and recommendation semantics, prove behavioural regressions with Testcontainers where persistence matters, meet `< 300 ms` (details known + 404 id 9999) and `< 1 s` (both recommendation routes, including cold and settled post-startup re-tests) on every measured sample, document the cause on YouTrack via Patron→Rigger verified read-back, and finish at awaiting user merge.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Api`, `LamuFlix.Core`, `LamuFlix.Infrastructure`) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing stack only — ASP.NET Core minimal endpoints, EF Core persistence adapters already used by `EfMovieCatalog`, `EfRecommendationCandidateSource`, `EfRecommendationProfileSource`, and `HybridCachedRecommendationProfileSource` (`recon-DEV-483` lines 11–16). No new NuGet/npm dependency or framework swap is preapproved (`brief.md` Q3).

**Storage**: Existing PostgreSQL/EF movie catalog and recommendation candidate/profile/history reads. No schema, migration, or index change is preapproved (`brief.md` Q3).

**Testing**: xUnit unit/integration suites named in recon lines 19–22; Testcontainers for persistence/query-translation behaviour; Compose Q2 receipt for latency (no unit wall-clock timers; no new benchmark platform) (`brief.md` Q4).

**Target Platform**: Local Docker Compose stack; measurements through web container port 3000 (`brief.md` Q2; ticket at `recon-DEV-483` lines 42–48).

**Project Type**: Existing ports-and-adapters .NET API with feature-organised core and explicit handlers/decorators (constitution.md:440-472; not VSA); bug fix within named candidate files; no frontend/`web` edits preapproved.

**Performance Goals**: Every measured sample — details known-ID and id `9999` 404 strictly `< 300 ms`; by-movie recommendations and `for-you?take=10` strictly `< 1 s`, including cold first and settled (`>= 60 s`) post-startup re-tests after exercising the diagnosed for-you timeout trigger (`brief.md` Q2).

**Constraints**: Charter §2.3 items 1–6 deliberately bounded (Q3). Diagnosis stop point (`needs recon`); outside-boundary cause is STOP for cited Patron ruling, never edit permission. Preserve routes/DTO/status/ranking/history/cache-identity-history-when-touched and the OpenAPI→TypeScript chain. Preserve unrelated `harness.yml` dirt from intake. Never lower harness thresholds or invent refactor/mutation opt-outs. Honest SKIPPED/SKIP/N/A/Could not run from actual script/config output. Measurement isolation during Q2 windows; sequential T007→T009→T008. Evidence under `git rev-parse --git-path dev-483-evidence` only (scrubbed). User owns merges; only Keel declares PLAN FROZEN.

**Scale/Scope**: Size M bug; three HTTP paths + proven causal collaborators; candidate file set is investigation boundary, not a mandate to edit all nine production files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox from this draft.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **PASS** — no new port; stay on existing `IMovieCatalog` / recommendation candidate & profile ports (`recon-DEV-483` 11–16). |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — existing `GetMovieDetails` / `GetRecommendations` / `GetForYouRecommendations` handlers; no MediatR; no new handler registration required unless diagnosis proves otherwise (would need Patron ruling if outside candidate files). |
| III (Typed query model) | MovieQuery | **N/A / PASS** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation; read-path latency only. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — preserve existing 404 and error semantics; no new public error contract. |
| VI (Observability) | Safe diagnostics | **PASS** — existing Activity/log facilities only (exact available facilities via recon; no invented names/APIs); `TimeProvider` for injected time; temporary instrumentation accounted for and removed before fix acceptance (measured build identity recorded) or retained only as gated production change inside approved candidate boundary; no secrets/env/raw config dumps in receipts under `git rev-parse --git-path dev-483-evidence`. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay, secrets, Process.Start, or options-record changes preapproved. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new glossary term or ADR (Q6). |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit/integration at fault seam; Testcontainers for persistence translation; Compose receipt for latency that unit timers cannot prove. |
| Tech stack / API docs | OpenAPI chain | **PASS** — no public API shape change; contract chain untouched; no `/web` edits. |
| Governance / Static-Analysis Gates | Honest exits | Phase B: `dotnet build` (additional to analyzer gates), Q4 scripts including applicable configured complexity/refactor-gate obligation (no hardcoded/lowered threshold or invented opt-out), `dotnet format --verify-no-changes`, `dotnet test`, pre-PR mutation when eligible per actual script/config output (never assumed by project name); do not open/change preserved `harness.yml` dirt; exact commands/HEAD/exits; distinct verdicts; never rename SKIPPED/SKIP/N/A/Could not run to PASS. Gates sequential after Q2 sample windows; after any remediation commit, full gate set on final HEAD. Closing bar and 2×2 caps per Q5. |

### Complexity Tracking

No constitution violations requiring justification. Diagnosis may later require a Patron ruling to expand file/protected surface; that is a future stop, not a present violation.

## Approach

- **Phase A (this repair)**: Quill repairs `spec.md` / `plan.md` / `tasks.md` / checklist / receipt from brief Round 1 adjudication and `PLAN_ADJUDICATION.md` fix list 1–10 only. Keel performs independent reread before any PLAN FROZEN declaration. Gate 1, implementation, runtime, and merge remain distinct. No Build work before Gate 1. Otherwise-unassigned implementation/measurement work is Build-lane owned.
- **Phase B pickup**: Fresh `origin/main` drift recon/analysis; collect missing fixture IDs/history facts and route-specific status/payload/fixture oracles via recon (do not invent); collect exact available Activity/log facilities and applicable gate/mutation configuration facts via recon.
- **Diagnosis (blocks fix)**: Establish Q2 sequential baseline under measurement isolation (no concurrent build/gate/test/packaging load); readiness without prewarming; pin/reset dataset/history; control batches + host/control noise without new thresholds; isolate proxy vs API vs handler/persistence/query/cache/external wait with existing Activity/log facilities; matched direct-API vs port-3000 comparison with transport differences recorded; concurrency only if evidenced; observe cancellation/abandoned work; re-baseline if diagnosed trigger was absent; document causal chain. Temporary instrumentation removed before fix acceptance (or retained as gated production change). Stop `needs recon` if unestablished. Outside-boundary proven cause → cited Patron ruling STOP, never edit permission. Evidence under `git rev-parse --git-path dev-483-evidence` (scrubbed).
- **Minimal fix + regressions**: Edit only proven causal subset of Q3 candidates; diagnosis-selected deterministic performance-relevant invariant; cache identity/history regressions if cached profile source touched (semantics from recon); Testcontainers for persistence translation; no unit wall-clock assertions; preserve semantics, contract chain, and Core dependency direction/layering via existing tests; `TimeProvider` for injected time.
- **Verify (ordered)**: Q2 after-measurements (T007) including diagnosed for-you timeout trigger and `>= 60 s` settled batches under isolation → mandatory .NET gates (T009) sequential after sample windows → keep missing evidence visibly open. Contaminated runs recorded and rerun.
- **Document (after T007+T009 status)**: Patron routes verified cause to Rigger for YouTrack + verified read-back; cause docs alone cannot yield Done when while runtime/gates remain open.
- **Review / ship prep**: Three-axis implementation review within caps; after any remediation commit, full mandatory gates + refreshed invalidated Q2/cause evidence on final HEAD; annotate stale receipts; never label old-head as final-head PASS; PR publication; awaiting-merge for the user — never merge; Quill does not declare PLAN FROZEN.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-483/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file
├── tasks.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief; no API/schema design delta; cause unknown until Phase B.

### Source Code (investigation / edit candidates — Q3 / recon lines 11–22)

```text
src/LamuFlix.Api/Endpoints/
├── LibraryEndpoints.cs                 # GET /movies/{id:int} → GetMovieDetails
└── RecommendationEndpoints.cs          # GET /movies/{id}/recommendations; GET /recommendations/for-you

src/LamuFlix.Core/Features/Library/
└── GetMovieDetailsQueryHandler.cs

src/LamuFlix.Core/Features/Recommendations/
├── GetRecommendationsQueryHandler.cs
└── GetForYouRecommendationsQueryHandler.cs

src/LamuFlix.Infrastructure/Persistence/
├── EfMovieCatalog.cs
├── EfRecommendationCandidateSource.cs
├── EfRecommendationProfileSource.cs
└── HybridCachedRecommendationProfileSource.cs

tests/LamuFlix.UnitTests/Features/Library/
└── GetMovieDetailsQueryHandlerTests.cs

tests/LamuFlix.UnitTests/Features/Recommendations/
├── GetRecommendationsQueryHandlerTests.cs
└── GetForYouRecommendationsQueryHandlerTests.cs

tests/LamuFlix.IntegrationTests/
├── LibraryEndpointsTests.cs
├── RecommendationEndpointTests.cs
└── ApiEndToEndRecommendationTests.cs
```

These are candidates, not a requirement to edit all of them. Diagnostic controls only: genres and movie list endpoints. Any tracked file outside this set (including Compose/proxy/startup registration) needs a cited Patron ruling before edit.

**Structure Decision**: Remain inside existing Api/Core/Infrastructure feature folders and named test suites. No new project, top-level folder, or architectural layer.

## Test Strategy

| Family | Required evidence |
|---|---|
| Unit — details / recommendations handlers | Preserve success, cancellation, and handler-level semantics; diagnosis-selected deterministic performance-relevant invariant; extend only for diagnosed fault; no unit wall-clock latency asserts |
| Integration — LibraryEndpoints / Recommendation endpoints / E2E recommendations | Preserve HTTP 200/404, ranking/filtering, for-you history; cache identity/history regressions if cached profile source touched (semantics from recon); Testcontainers for persistence translation; Core dependency direction/layering via existing tests |
| Causal regression | Deterministic tests at the actual diagnosed fault seam (chosen after diagnosis); query/round-trip bounds only if causal |
| Compose Q2 receipt | Before/after wall-clock via Invoke-WebRequest port 3000 under measurement isolation; readiness without prewarming; pin/reset; recon oracles; cold first + 20 sequential samples + control batches/noise notes + settled re-test; every sample vs `< 300 ms` / `< 1 s`; for-you timeout trigger exercise; contaminated runs rerun; scrubbed evidence under `git rev-parse --git-path dev-483-evidence`; no unconditional concurrent acceptance leg |
| Gates | Sequential after T007 sample windows: `dotnet build` (additional to all analyzer gates; never a substitute), `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (applicable configured refactor-gate obligation; no hardcoded/lowered threshold or invented opt-out), `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and pre-PR `./scripts/run-mutation.ps1` when eligible per actual script/config output (never assumed by project name); do not open/change preserved `harness.yml` dirt; record exact commands, HEAD, exit codes, and distinct PASS / FAIL / SKIPPED (scope-empty) / SKIP / N/A / Could not run verdicts; after remediation commits, full gate set on final HEAD (`brief.md` Q4; constitution.md:750-756; FR-015; Round 1 fixes 5, 6, 8) |

Unit/integration assertions cannot prove Compose wall-clock targets — the Q2 receipt remains required (`brief.md` Q2/Q4). Packaging order: T007 → T009 → T008 → T010/T011.

## Phase 0 / Phase 1 note

Per Spec Kit plan workflow: unknowns that would normally populate `research.md` are already ruled in brief Q1–Q6 (scope, measurement protocol, file boundary, test/gate strategy, caps, task order). The runtime root cause is intentionally **not** invented here; it is Phase B evidence with an explicit `needs recon` stop. No data-model or contracts artifacts — no public API or schema change is preapproved.

## Post-design Constitution Check

Re-checked after documenting the diagnosis-first, candidate-bounded structure above: still PASS/N/A as in the Constitution Check table; no new port, handler framework, schema, API, LocalPlay, secrets, Process.Start, CONTEXT, or ADR obligations introduced by this plan.
