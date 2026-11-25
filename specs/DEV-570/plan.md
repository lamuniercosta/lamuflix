# Implementation Plan: Movie details About section

**Branch**: `feature/570-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-570-spec`) | **Date**: 2026-11-25 | **Spec**: `specs/DEV-570/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q5); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-570`, `recon-DEV-570`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: See the plan-challenge freeze receipt in `CONCLUSIONS.md`. Zero structural owner checkboxes. Ordinary Gate 1, implementation and Chromium acceptance are pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` — grill Q1–Q5 freeze inventable unknowns; API field set is ticket-specified on the existing route (documented in Scope/FR, not a new contracts package).

## Summary

Project stored writers, genres, and collection name through the existing movie details response; show them with directors and cast in an About section on the movie details page (below actions, above trailer); preserve hero cast and More with selection; verify with API/contract tests, web unit/component tests, applicable gates, and windowed plus phone-width Chromium screenshots of the same real library movie via Test URLs.

## Technical Context

**Language/Version**: .NET 10 (C#) API + TypeScript / React 19 / Vite (`web/`) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing library details endpoint and `MovieMetadata` storage; existing movie-details feature, CastSection selection path, and More with panel; existing OpenAPI → `openapi.json` → `npm run generate:api` → `types.ts` chain; existing Vitest/RTL and xUnit integration tests. No new NuGet/npm dependency (`brief.md` Q4; `PRODUCT.md` §5 item 1).

**Storage**: Existing persisted movie metadata only. No schema or migration change (`recon-DEV-570:34`; ticket Scope).

**Testing**: Extend `LibraryEndpointsTests` endpoint JSON assertions and `OpenApiContractTests` schema assertions (present `writers`/`genres` arrays; present nullable `collectionName`); regenerate OpenAPI/Verify/`types.ts` via the sanctioned path confirmed at T001; extend `movieDetailsApi` Zod alignment and `MovieDetailsView` (and adjacent About) semantic tests (including null-metadata); Chromium receipts via Test URLs — same-movie screenshot pair plus SMOKE-001–002 on enriched/mapped-cast movie and SMOKE-003 on a separate null-metadata movie. No new test framework.

**Target Platform**: Compose/local web app movie details page; Chromium for acceptance screenshots (windowed and phone width).

**Project Type**: Existing API DTO projection + SPA feature under `web/src/features/movie-details/`.

**Performance Goals**: Static metadata display; no invented latency SLA.

**Constraints**: Charter §2.3 care-list items deliberately ruled out by Q4 (no dependency/layer/schema; public API limited to the three ticket fields on the existing route; no LocalPlay/secrets/`Process.Start`; no out-of-envelope file edits). Ordinary owner Gate 1; zero structural owner checkboxes. Size M requires Risk (Sentry), Standards (Ledger), Spec (Compass). Closing bar includes Critical–Low in-scope; max two review rounds and two fix commits per round (`brief.md` Q5). Honest gate labels; never lower thresholds.

**Scale/Scope**: Size M; parent DEV-490; file envelope in brief Q4.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox (`PRODUCT.md` §5; brief Q4–Q5).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests | **PASS** — no new port or Core change; project already-stored `MovieMetadata` fields through the existing API mapper only (`recon-DEV-570:16–19`). |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A** — no new application handler; existing `GetMovieDetails` path unchanged aside from response projection (`recon-DEV-570:18–19`). |
| III (Typed query model) | MovieQuery | **N/A** — no browse/filter change. |
| IV (Domain transitions) | Movie mutators | **N/A** — no Movie mutation (display-only). |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no new error surface; web Zod parser aligns to generated contract before UI wiring (`brief.md` Q2). |
| VI (Observability) | Safe diagnostics | **PASS** — no secrets; no new telemetry requirement for this display slice. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no Options/IConfiguration work; **do not touch** LocalPlay execution path on the details page (`Features:LocalPlay` remains untouched). No secrets/`Process.Start`. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — spec-local vocabulary only; no CONTEXT.md expansion claimed. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — API integration + OpenAPI contract tests; web unit/component tests; Chromium receipts via Test URLs for acceptance evidence. |

### Stack / workflow checks

| Check | Plan response |
|---|---|
| Tech stack / API docs (OpenAPI chain) | **PASS** — ticket fields flow C# DTO → OpenAPI export → `web/src/api/openapi.json` → `npm run generate:api` → `types.ts`; never hand-edit generated types (`PRODUCT.md` §1; brief Q2/Q4). |
| Static-analysis gates (C#) | Applicable Phase 3 step 5 / Phase 5 C# gates scoped to **changed `.cs` files**: Roslyn, cyclomatic complexity, InspectCode. When that scope is empty, record `SKIPPED (scope-empty)` honestly — never as PASS, and never infer a skip without running the gate. |
| Web gates | Required: `scripts/run-web-gates.ps1` exit 0 on the implementation diff (Phase 3 step 5 / Phase 5 applicable web gate). |
| Other brief gates | Property tests, vulnerable packages, format and `dotnet test`. See Verification Approach for disposition and task-note rules. |
| Mutation | Applicable Phase 3 step 5 / Phase 5 mutation gate scoped to **production C# under `src/`**. When none changed, expect script exit 2 `SKIPPED (scope-empty)` — report that label only, never as PASS. When every changed project is listed in `gates.mutation.exclusions`, the script text is `NOT APPLICABLE`; report the recorded verdict as **N/A** (never PASS). Do not conflate `SKIPPED (scope-empty)`, harness `SKIP`, exclusions **N/A**, or Constitution **N/A**. |

### Complexity Tracking

No constitution violations requiring justification. No new architectural layer or dependency.

## Approach

- **Phase A**: Quill drafted the Spec Kit artifacts after the grill. Keel records the plan-challenge freeze in `CONCLUSIONS.md`. Patron then sets provisional Gate 1. Bernstein coordinates and Rigger opens the Spec Kit PR. Owner merge and Keel’s verified T000 receipt unlock pickup.
- **Phase B pickup**: Confirm authorized file/test seams against pickup HEAD (read-only; FR-008 / T001). Inventory all typed consumers of the two generated response types, including typed test/mock fixtures. Verify generator ownership and the exact command in Contract regeneration from the pickup checkout. T001 records evidence read-only. STOP for cited Patron ruling before any out-of-envelope edit.
- **Local Build dispatch (`tasks.md` Phase N headings; Anvil one phase per ask; `brief.md` Verification steps 1–3; Q1–Q5; `web-implement` / implement test-first)**:
  1. Gate 1, then Phase B pickup recon of the authorized envelope (T001).
  2. **API and contract**: Author failing-first endpoint assertions in `LibraryEndpointsTests.cs` and schema assertions in `OpenApiContractTests.cs` for present `writers`/`genres` arrays and present nullable `collectionName` **before** projection; extend `MovieMetadataResponse` projection in `LibraryResponses.cs`; regenerate OpenAPI baseline, Verify snapshot, and `types.ts` via the sanctioned path confirmed at T001; pass endpoint and OpenAPI tests before web.
  3. **Web** (matches T006 → T007 → T008): Author failing-first About/view **semantic** tests first (rows/order, singular/plural Director/Directors and Writer/Writers, empty cases including null-metadata, mapped/unmapped cast, hero regression, trailer placement — not phone-width wrapping); then align `movieDetailsApi.ts` Zod schema to generated `components` for the new fields; then implement About in `MovieDetailsView.tsx` inside the FR-008 envelope (T008; one focused colocated `AboutSection`, scoped styling if needed, and minimal `CastSection.tsx` sharing only if it avoids real duplication). Place About after actions, browser player and launch feedback, before trailer.
  4. **Integrated verification (Build)**: Via Test URLs — windowed and phone-width screenshots of About on the **same** enriched real library movie (same-movie rule applies only to that pair); record route and viewport dimensions; record identifiable SMOKE-001–002 on a movie with required data and mapped cast, and SMOKE-003 on a **separate** null-metadata movie, each with identifiable route/result. Retain no-metadata component coverage from Phase 3. Phone-width wrapping (FR-006) is verified on the screenshot pair. Local Build ends here — do **not** assert hosted merge readiness.
- **Downstream after Build (Conductor-owned; not Anvil local phases; `brief.md` Verification step 4 / Closing bar)**: Cog `/refactor` → Gauge Phase 3 step 5 (Roslyn, cyclomatic complexity, InspectCode, scoped mutation for production C#, property tests under the standing rule, vulnerable packages, format, `dotnet test`, `scripts/run-web-gates.ps1`) → Gauge pre-pass and Sentry/Ledger/Compass review; **Keel** adjudicates over all three axis reports and owns the findings artifact (every verified in-scope finding through Low closes within two rounds / two fix commits per round or is reported blocked) → Phase 5 rebase and gate rerun. Apply the gate rules in Verification Approach. Do not bundle Gauge or review into an Anvil phase.
- **Do not**: add dependencies or layers; change schema; invent API fields beyond ticket Q2 shape; touch LocalPlay execution; hand-edit generated contract files; expand past the Q4 envelope; claim Gate 1 or runtime PASS without their receipts; lower gate thresholds.

## Contract regeneration

T001 verifies the invocation, generator ownership and typed consumers from the pickup checkout read-only. T004 runs the export after the T003 DTO projection, from the repository root. The opt-in test writes the OpenAPI baseline and its derived Verify snapshot together.

```powershell
try {
    $env:LAMUFLIX_REGENERATE_OPENAPI = 'true'
    dotnet test tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj --filter OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether
    if ($LASTEXITCODE -ne 0) { throw 'OpenAPI regeneration failed.' }
} finally {
    Remove-Item Env:LAMUFLIX_REGENERATE_OPENAPI -ErrorAction SilentlyContinue
}
```

From `web/`, T004 then runs `npm run generate:api` and T005 requires `npm run check:api` to pass alongside the endpoint, schema and drift tests. Apply the FR-008 fixture exception only for demonstrated required-field additions. Full web gates remain in the downstream pipeline.

## Verification Approach

- **API / contract**: Author `LibraryEndpointsTests` endpoint JSON assertions (populated values/order, empty writers/genres, explicit null collectionName, preserved null metadata) and `OpenApiContractTests` schema assertions (present writers/genres arrays; present nullable collectionName) **before** projection; regenerate via T001-confirmed sanctioned path; pass endpoint and OpenAPI tests before web.
- **Web**: Vitest/RTL semantic coverage for FR-004–FR-007 / FR-009 / SC-001–SC-002 — full rows, missing writers only, optional collection, all-rows-empty, null-metadata notice, Director/Directors and Writer/Writers, heading-labelled `dl`, comma-separated stored order, blank-member omission and displayed counts, mapped About → More with, unmapped plain text, hero cast regression, trailer remains below About. Author failing semantic tests before Zod alignment and view wiring (T006 → T007 → T008). Cover malformed and missing new keys through the existing details error path. At T009, run parser, view and About tests plus the existing BS.Player and launch regression suites. Existing grey/white styling and phone-width wrapping belong to Chromium (FR-006 → FR-010).
- **Integrated Chromium (Build)**: Test URLs portal; same enriched movie for the windowed + phone-width screenshot pair only; identifiable SMOKE-001–002 on enriched/mapped-cast movie and SMOKE-003 on a separate null-metadata movie, each with route/result; retain no-metadata component coverage; blockers distinct from passed checkpoints.
- **Gates (downstream; not Anvil)**: After Build and Cog `/refactor`, Gauge runs applicable Phase 3 step 5 then Phase 5 rebased gates: Roslyn, cyclomatic complexity, InspectCode, scoped mutation for production C#, property tests under the standing rule, vulnerable packages, format, `dotnet test`, `scripts/run-web-gates.ps1` exit 0. Record `SKIPPED (scope-empty)`, harness `SKIP`, and exclusions **N/A** honestly; never infer a skip without running the gate; never fold those labels or `Could not run` into PASS. Exit 1 and `Could not run` block. Patron ruled the DTO projection and About presentation eligible for a property-test opt-out because they add no domain invariant. Property-test exit 2 blocks unless Bernstein records `propertyTests: opt-out — DTO projection and About presentation add no domain invariant` in the DEV-570 task note.
- **Review (downstream; not Anvil)**: Gauge pre-pass then Sentry/Ledger/Compass; **Keel** adjudicates all three axis reports and owns the findings artifact; every verified in-scope finding through Low closes within the two-round / two-fix-commit-per-round cap or is reported blocked; follow-up routing per brief Q5. Do not assert hosted merge readiness from Build.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-570/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file (also mirrored to private IMPL_PLAN)
├── tasks.md
├── DRAFTING_RECEIPT.md
└── checklists/
    └── requirements.md
```

No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` — grill decisions freeze Spec Kit research unknowns; ticket API fields live in FR/Scope rather than a separate contracts package.

### Source Code (authorized envelope — brief Q4; ticket Scope)

```text
src/LamuFlix.Api/Endpoints/
└── LibraryResponses.cs                 # MovieMetadataResponse projection (+ writers, genres, collectionName)

tests/LamuFlix.IntegrationTests/
├── LibraryEndpointsTests.cs            # populated / missing / null-metadata JSON assertions
├── OpenApiContractTests.cs             # schema assertions (present writers/genres arrays; present nullable collectionName) + drift vs baseline
└── Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json  # REGENERATE via T001-confirmed path

web/src/api/
├── openapi.json                        # REGENERATE from app-generated OpenAPI export
├── types.ts                            # REGENERATE via npm run generate:api
└── client.test.ts                      # additive fixture fields only with demonstrated need

web/src/features/movie-details/
├── movieDetailsApi.ts                  # Zod align to generated MovieMetadataResponse fields
├── MovieDetailsView.tsx                # About placement + wiring; preserve hero cast / NoMetadata / trailer order
├── MovieDetailsView.test.tsx           # (+ adjacent About and parser tests as needed)
├── MovieDetailsView.css                # scoped About styles if needed, using existing tokens
├── CastSection.tsx                     # minimal share only if real reuse is needed
└── AboutSection.tsx                    # focused colocated About component
```

**Structure Decision**: Extend the existing library response mapper and movie-details feature only. Generated contract artifacts refresh through sanctioned tooling. Keep About rendering in the colocated component. A separate row builder is optional. Existing typed test/mock fixtures, including `web/src/mocks/fixtures.ts`, may receive only additive required fields with documented T001 inventory or compiler evidence. `web/src/mocks/handlers.ts` remains generator-owned and any hand edit needs a fresh Patron ruling. STOP for cited Patron ruling before any out-of-envelope edit.

## Phase 0 / Phase 1 Spec Kit outputs

- **research.md**: omitted — Q1–Q5 resolve inventable unknowns.
- **data-model.md**: omitted — no schema entities; stored fields already exist.
- **contracts/**: omitted — field set is ticket-specified on the existing route and enforced by OpenAPI drift + generated types.
- **quickstart.md**: omitted — verification lives in brief Verification and tasks.
