# Implementation Plan: Collection Filter TMDb Enablement

**Branch**: `feature/445-spec` | **Date**: 2026-11-04 | **Spec**: `specs/DEV-445/spec.md`

**Input**: Frozen `brief.md` (Q1–Q5) plus Q6 plan-challenge amendment; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-445`, `recon-DEV-445`, `recon-DEV-445-2`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only; no constitution or ADR edits — none authorized).

**Status**: PLAN FROZEN — Keel Phase 2 step 6 adjudication verified 2026-11-04; see brief.md / CONCLUSIONS.md final freeze receipt and adjudication-DEV-445. Gate 1 remains CLOSED pending Patron provisional review, Rigger spec PR, and user merge. No implementation clearance.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief, and the public contract chain is untouched (precedent DEV-426 / DEV-439 / DEV-447). `ASSUMPTIONS.md` already exists from grill (Q3); Quill does not rewrite it. `design.md` is Palette-owned and not authored here.

## Summary

Enable TMDb in Compose via `.env`, forward OMDb IMDb identity into the existing DEV-358 TMDb supplement so collection membership resolves through `/find`, preserve merge/persistence semantics, and hide the Filter Panel Collection group only when the facet is healthy-empty. Prove with static config tests, chain/WireMock/persistence regressions, web-implement TDD, harness/web gates, and live Compose Bond acceptance.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM); TypeScript/React in `/web` via `web-implement`.

**Primary Dependencies**: Existing pins only. **No new packages** (brief §2.3; recon-DEV-445:80).

**Storage**: Existing `collections` table + `MovieRecord.CollectionId` — **no schema/migration** (recon:83; Q5).

**Testing**: xUnit v3, Shouldly; existing WireMock harness for TMDb/OMDb composition; new static `TmdbComposeTests`; Vitest + React Testing Library for FilterPanel via web-implement TDD. No reading `.env` or real secrets in tests. Live Compose Bond acceptance is separate required evidence.

**Target Platform**: .NET server (ASP.NET Core Minimal API) + Vite/React web; Docker Compose local stack.

**Project Type**: Config/wiring + minimal adapter identity handoff + Filter Panel conditional render.

**Performance Goals**: No new request fan-out beyond existing supplement path; IMDb `/find` replaces the mistaken title-search supplement when an id exists (Q1).

**Constraints**: No explanatory comments outside allowed exemptions; do not delete or rewrite unrelated comments. Build/refactor cyclomatic-complexity thresholds from `harness.yml` only; never lower thresholds. No public API/schema/dependency/LocalPlay/Process.Start. Never inspect, print, or commit real TMDb/OMDb keys. Mutation hygiene: literals used in static contexts are `const`; no new static initialisers that bake mutant state. Tests use deterministic `TimeProvider`/`FakeTimeProvider` and seeded `Random` when needed; no `DateTime.Now`, `Task.Delay`, or wall-clock waits. Private helper extraction for complexity stays inside allowlisted production files — unmet complexity gate needs a new Patron ruling, not allowlist expansion.

**Scale/Scope**: personal library; Size M; `ui:` tag; movie-only.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (current 1.10.0 in worktree). No constitution amendment and no ADR (Size M; Q5). No owner Gate 1 checkbox.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | No new ports/layers | PASS — reuse existing `IMetadataProvider` chain/TMDb adapter; minimal forwarding inside `ChainedMetadataProvider`; no new port/project/layer. |
| II (Explicit Handlers and Decorators) / V (Validated Inputs, Consistent Error Responses) | Public API / error-response stability | PASS — no route/DTO/status change; existing `/api/collections` and `collectionId` browse remain. |
| IV (Enrichment state machine) | Existing statuses/transitions | PASS — enrichment flow unchanged; collection arrives via existing supplement merge + persistence. |
| VI (Observability) | Safe structured logging | PASS — never log API keys; no new secret surfaces beyond ignored `.env` channel already ruled (Q2). |
| VII (Configuration / time) | Validated options; TimeProvider | PASS — existing `TmdbOptions` / `IsConfigured`; Compose env binds `Tmdb__*` with Q2 `TMDB_ENABLED:-true` default and empty key placeholder; appsettings default stays disabled. Enabled + blank/whitespace key is zero TMDb HTTP with existing ProviderUnavailable / Found+Unavailable semantics — not disabled-chain equivalence (Q6 / Sentry F1; recon-DEV-445-2:9-26); no registration edits. |
| VIII (Ubiquitous Language) | No invented Core vendor types | PASS — existing glossary terms only (metadata enrichment, signals, collection membership/facet). |
| IX (Test Pyramid) | WireMock for HTTP; real DB where translation matters | PASS — WireMock for `/find` and blank/whitespace-key composed proofs (T006); persistence uses existing real-DB collection tests; static file asserts for Compose/docs mirror `ObservabilityComposeTests` line-based active-line parsing + `Directory.Packages.props` root walk (recon-DEV-445-2:44-49); no YAML package; never read `.env`. |
| Tech Stack | Typed HttpClient + resilience for metadata providers | PASS — existing TMDb client/resilience retained. |
| Governance / Static-Analysis Gates | Harness thresholds; no lowered gates | PASS — Phase B only; see Gates. Web via web-implement. |

### Complexity Tracking

No constitution violations requiring justification. No amendment path.

## Approach

- **Phase A (this draft)**: Quill drafts / repairs spec/plan/tasks + checklist + receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon facts only; Palette `design.md` exists with verified corrections; Keel runs read-only `/speckit-analyze` and plan challenge freeze verification; **Patron provisional Gate 1 → Rigger opens the spec PR → user merges Gate 1 → Keel T000 merge receipt**. No Build code/gates here (Q5). Gate 1 remains closed until Keel freeze verification.
- **Phase B pickup (after Gate 1)**: Wisp fresh recon drift check against main for the approved file/contract set; Keel blocking reanalysis if drift changes contract/plan; Gauge baseline before code edits (Q5 handoff).
- **Execution order (Q4, reconciled Q6 / Ledger S-5/S-10)**:
  1. Backend TDD optional: identity unit proofs (T003) before production identity edit (T005), red where feasible; existing green regressions stay green. T002 may parallel T003; T004 after T002; shared-file work sequential. Update Compose, `.env.example`, and README together (T004) including blank-key opt-out warning.
  2. Prove composed OMDb hit → IMDb `/find` → TMDb details → signals/collection → existing persistence; composed blank/whitespace-key proofs (T006); extend persistence tests only if handoff coverage is missing.
  3. Empty-facet change via mandatory web-implement red→green TDD (T009→T010→T011); preserve error and active-filter behavior; replace obsolete empty-legend assertion with role/name absence + positive controls.
  4. Targeted regressions, then applicable backend harness (changed C# only) and web gates (FilterPanel lint/typecheck/Vitest).
  5. Single operational live Compose sequence in T020 (key-only default, false opt-out, restore enabled Bond) with expected/imported/matched counts; sanitized evidence; demonstrated environmental inability only → `Could not run`.
  6. Review within Q5 closing bar / frozen scope / two-round cap.
- **Refactor**: Cog runs `/refactor` once over the completed implementation (task-pipeline Phase 3 step 3). C# cyclomatic/InspectCode apply only to changed C# (including chain); FilterPanel uses web lint/typecheck/Vitest via web gates — never apply C# tools to TypeScript (Ledger S-1). Code edits may be conditional when already green; the refactor stage itself remains mandatory even when edits are unnecessary.
- **Verification**: Gauge configured Phase 3 gates on task diff; web gates when `/web` changed; then mandatory live Compose Bond acceptance; Size M three-axis review with Keel adjudication and Cog remediation; Phase 5 rebased gates + hosted checks before awaiting-merge.

## Project Structure

```text
specs/DEV-445/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md
  design.md                                 (Palette-owned; required before ui plan challenge — not authored by Quill)

src/LamuFlix.Infrastructure/Adapters/
  ChainedMetadataProvider.cs                (edit: supplement lookup identity forwarding)

docker-compose.yml                          (edit: api Tmdb__Enabled / Tmdb__ApiKey only)
.env.example                                (edit: TMDB_ENABLED / TMDB_API_KEY placeholders)
README.md                                   (edit: Run with Docker Compose TMDb vs OMDb docs)

web/src/features/library/
  FilterPanel.tsx                           (edit: conditional Collection fieldset)

# Explicitly unchanged production (among others):
#   TmdbMetadataProvider*.cs behavior beyond existing /find path consumption
#   EfMovieRepository persistence rules (reuse)
#   Facet/Library endpoints, OpenAPI/TS contracts, schema/migrations
#   docker/api.settings.example.json, appsettings.json Enabled:false default

tests/LamuFlix.UnitTests/
  Adapters/MetadataProviderChainTests.cs
  TmdbComposeTests.cs                       (new)

tests/LamuFlix.IntegrationTests/
  MetadataProviderChainCompositionTests.cs
  SecondaryMetadataProviderLookupTests.cs
  EfMovieRepositoryCollectionTests.cs       (extend only if needed)

web/src/features/library/FilterPanel.test.tsx
web/src/library/LibraryView.toolbar.test.tsx  (only if needed)
```

**Layout decisions**:

- No research/data-model/contracts artifacts — public contract chain untouched; decisions frozen in brief.
- Quill owns spec/plan/tasks/receipt/checklist. Quill does not edit `brief.md` / `CONCLUSIONS.md` / `ASSUMPTIONS.md` / ADRs / constitution / production code / `design.md`.
- Keel owns analyze/plan challenge. Palette owns `design.md`.

## Test Strategy

| Surface | Coverage |
|---|---|
| Chain unit (US2) | Four separate identity branches (Ledger S-4/S-6): manual only; OMDb only; both with distinct valid IDs so manual wins; neither preserves original lookup instance/title/year. Also OMDb metadata retention; TMDb-only merge fields; NotFound → Found/Unavailable; disabled no call; fallback no repeated call. `Method_Condition_Expected` naming; reuse existing `StubMetadataProvider`; no NSubstitute introduced to the chain suite. Existing chain failure matrices remain regression evidence. |
| WireMock composition/lookup (US2) | Exact `/find` IMDb + `external_source=imdb_id` request; details fetch and collection/signals handoff; zero title search on `/find` miss; retain resilience/quota regressions. T006 composed blank AND whitespace key coverage: primary miss → `Failed(ProviderUnavailable)`; primary hit → `Found` with `Unavailable` signals; zero TMDb HTTP; exact categories/signals (Q6 / Sentry F1). Do not claim a new end-to-end enrichment retry test outside the allowlist. |
| Persistence (US2) | Available persists; KnownEmpty clears membership without deleting collection row; Unavailable leaves new membership unset and preserves any existing membership; extend `EfMovieRepositoryCollectionTests` only for missing composed handoff assertion |
| Static config (US1) | Exact Compose interpolation/defaults; `.env.example` entries/empty key; `appsettings` disabled default; static negative lock that tracked `docker/api.settings.example.json` has no `Tmdb` section or `TMDB_API_KEY` material (file unchanged). Mirror `ObservabilityComposeTests` line-based active-line parsing and `Directory.Packages.props` root-marker walk (recon-DEV-445-2:44-49); no YAML package; no shared-helper extraction; never hardcode gate thresholds; never read `.env` or secrets (Q6 / Sentry F3 / Ledger S-2) |
| Web (US3) | Absence query by role/name for Collection plus positive controls that other groups remain visible (Genres, Enrichment status, or Sort); visible populated; visible fetch-error; active `collectionId` retained; chip removal and Clear All; replace obsolete empty-legend assertion (never weaken to pass). Toolbar stale-chip tests must close the panel first per corrected design. Mandatory web-implement red→green TDD (T009→T010→T011). T010: derive const beside `collectionFailed`, no `useState`/`useEffect`; extraction inside `FilterPanel.tsx` only; preserve Q3 render predicate and generated types |
| Runtime (US1 + US4) | Single operational sequence owned by T020 — reference T020 rather than repeating the full sequence here (Ledger S-7). Outcomes retained: key-only default true, explicit false opt-out, restore enabled Bond import → persisted collection → James Bond Collection facet → filter; record expected/imported/matched Bond membership counts plus sanitized environmental quota/rate-limit evidence; incomplete membership never passes; only demonstrated environmental inability may be `Could not run` (acceptance remains open). Never inspect/print keys or resolved secret-bearing Compose config. Mocked pass does not close |
| Normalization citation (Ledger S-9) | Cite recon-DEV-445-2:30-42: OMDb `MapIdentifier` → `ImdbId.TryCreate` rejects blank/whitespace/`N/A` to null; manual IDs validated at `PUT /api/movies/{id}/imdb`. Preserve Q1 typed-null coalescing (`lookup.ManualImdbId ?? found.Metadata.ImdbId`); no string guard or extra normalization file/test scope. S-13 optional process trimming of FR-013/SC-007 is deferred — retain those IDs and traceability |
| Regression | Existing endpoint/catalog collection facet and `collectionId` browse tests remain green without API changes; existing chain failure matrices remain regression evidence |

**Supplement failure theory (Q6 / Sentry F2; recon-DEV-445-2:18,26)**: Supplement `RateLimited` / `QuotaExhausted` / `ProviderUnavailable` collapses to primary OMDb `Found` with `Unavailable` signals (new membership unset; existing membership preserved per Q4). Retain the existing failed-supplement theory without production edits or resilience/pacing scope expansion. Do not reclassify unexplained Bond mismatch as environment.

No Gherkin. Property tests: reuse existing suite; no new framework; exit-2 opt-out only with recorded task-note reason.

## Gates (after Gate 1 and implementation — Gauge owns configured Phase 3 gates)

Thresholds for build and refactor cyclomatic complexity come only from `harness.yml`.

1. Explicit changed-C# scope only (Ledger S-1): `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml, `./scripts/run-jetbrains-inspectcode.ps1` (never `-All`). Do not apply C# cyclomatic/InspectCode to TypeScript/`FilterPanel.tsx`.
2. Affected unit/integration suites, then full `dotnet test`.
3. Property / vulnerable-packages / format per harness; distinguish PASS, failure, SKIPPED (scope-empty), SKIP, mutation N/A, Could not run.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` from worktree) for changed production C#.
5. Web: `scripts/run-web-gates.ps1` / web-implement workflow (lint/typecheck/Vitest) when `/web` changed — never claim PASS on scope-empty SKIPPED; FilterPanel gates live here, not under C# tools.
6. Mandatory live Compose acceptance via T020 (Q4, Q6); Size M review (canonical Phase 4): Sentry, Ledger, Compass with Keel adjudication and Cog remediation; Q5 closing bar (High/Critical block; Medium/Low explicit cited disposition); two rounds / two fix commits per round max.
7. Fresh rebased gates + current-head review evidence + watched hosted checks before awaiting-merge (canonical Phase 5).

Phase A runs none of these and claims no gate result. Verification/gate tasks (T008/T011/T013/T017/T018) produce receipts — no empty commits (Ledger S-11).

## Risks

- **Key-only acceptance depends on Compose default true**: deliberate Q2 ruling; static tests assert interpolation only (US1 static evidence). Runtime proof is the single T020 sequence (Ledger S-7) — reference T020; do not restate the full order here. Retain acceptance outcomes, sanitized evidence, and no-secrets / no-resolved-Compose-config restrictions (brief.md:54; US1 acceptance #5; FR-001, FR-011).
- **Enabled + blank/whitespace key ≠ disabled** (Q6 / Sentry F1; recon-DEV-445-2:9-26): zero TMDb HTTP; OMDb miss → retryable `Failed(ProviderUnavailable)` (default three attempts then terminal `Failed`); OMDb hit → `Found`/`Unavailable` (new membership unset; existing membership preserved). README (T004) must warn `TMDB_ENABLED=false` until a real key is set. T006 proves composed blank and whitespace cases. Keep exact Q2 Compose `:-true` default and empty `.env.example` placeholder; no registration edits.
- **Supplement rate-limit / unavailable collapse** (Q6 / Sentry F2; recon-DEV-445-2:18,26): RateLimited/QuotaExhausted/ProviderUnavailable → primary Found/Unavailable under existing failed-supplement theory. T020 must record expected/imported/matched Bond counts plus sanitized quota/rate-limit evidence; incomplete membership never passes; only demonstrated environmental inability may be `Could not run` — do not reclassify unexplained mismatch as environment or add resilience/pacing scope.
- **`/find` miss without title retry**: may leave Unavailable collection signals on primary Found — accepted Q1; ManualImdbId / correct OMDb id remain the happy path. Typed `ImdbId.TryCreate` already normalizes blank/whitespace/`N/A` to null (recon-DEV-445-2:30-42); no extra string guard (Ledger S-9).
- **Active selection survives hidden facet**: shared links keep a stale filter until chip/Clear All — accepted taste assumption (Q3). Initial-load hide remains accepted existing design (Q6 / Sentry F4).
- **Unavailable live credentials / environmental quota**: Bond acceptance `Could not run` leaves acceptance open and blocks closure — never silent PASS (Q4–Q5, Q6).
- **Pickup drift**: mandatory Phase B recon/reanalysis before coding (Q5).
- **Secret handling**: never print/commit keys; tests never read `.env`; README distinguishes TMDb ignored `.env` from OMDb ignored JSON (Q2, Q4, Q6 / Sentry F3).
- **design.md**: Palette design exists with Compass M1/M2/L1 and Ledger S-8 corrections verified; obsolete awaiting-design status replaced (Ledger S-12).

## Ordering

See `tasks.md`. Numbered local phases below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` dispatch — **not** canonical `task-pipeline` phases. Canonical Build/review/delivery remain Phase 3 / Phase 4 / Phase 5. Seat map: Wisp/Keel/Gauge Pickup (local Phase 1); Anvil code phases 2–4; Cog local Phase 5; Gauge local Phase 6–7; local Phases 8–9 ↔ canonical Phase 4/5.

```text
Phase A (Spec)     Quill draft → Palette design.md → Keel analyze/challenge → Patron provisional Gate 1 → user merges Gate 1
Phase 0            T000 Keel Gate 1 merge receipt (blocks Build)
Phase 1            T001 Wisp drift recon → Keel reanalysis → Gauge baseline (blocks implementation)
Phase 2            T002 → T005 Anvil config + chain identity (US1–US2)
Phase 3            T006 → T008 Anvil composed/persistence proofs (US2)
Phase 4            T009 → T011 Anvil web-implement empty-facet (US3)
Phase 5            T012 Cog /refactor once (required stage; code edits may be conditional)
Phase 6            T013 → T019 Gauge configured backend + web gates
Phase 7            T020 Live Compose Bond acceptance (US4)
Phase 8            T021 Sentry/Ledger/Compass; Keel adjudication; Cog remediation (cap 2)
Phase 9            T022 → T023 rebased gates + hosted checks → awaiting-merge
```
