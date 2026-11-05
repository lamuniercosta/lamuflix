# Implementation Plan: Embedded Trailer on Movie Details (pt-BR captions preferred)

**Branch**: `feature/463-spec` | **Date**: 2026-11-05 | **Spec**: `specs/DEV-463/spec.md`

**Input**: Frozen `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-463`, `recon-DEV-463`, `recon-DEV-463-2` (incl. §9); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only; no constitution or ADR edits — none authorized).

**Status**: PLAN FROZEN — 2026-11-05. Analyze clean after round 1 repair (DEV-463 receipt); three-axis plan challenge round 1 adjudicated and approved updates applied. No implementation clearance. Patron provisional Gate 1 and owner spec-PR merge remain pending; T000 blocks pickup.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief; public DTO field is encoded in spec + OpenAPI generation tasks (precedent DEV-438 / DEV-428). `ASSUMPTIONS.md` already exists from grill; Quill does not rewrite it. No `design.md` (ticket has no ui tag; taste already in ASSUMPTIONS).

## Summary

Add an optional Trailer key to enriched movie metadata from TMDb `/movie/{id}/videos` (pt-BR then original-language ranking, YouTube Trailer eligibility, deterministic official/key ties), persist it as a nullable column, expose `trailerKey` on the existing movie-details metadata response through the OpenAPI → generated TS/MSW chain, and render a lazy `youtube-nocookie` iframe Trailer section on Movie Details (hide when absent/invalid; always request pt-BR captions; document CSP/internet; degrade cleanly when blocked).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM); TypeScript/React 19 in `/web` via `web-implement`.

**Primary Dependencies**: Existing pins only. **No new packages** (brief Q5/Q8; PRODUCT §5 item 1). Plain `<iframe>` — no YouTube SDK.

**Storage**: Nullable `trailer_key` column on existing `movies` table via EF configuration + tool-generated migration/model snapshot (Q1). No other schema scope.

**Testing**: xUnit v3, Shouldly, NSubstitute, WireMock.Net, Testcontainers Postgres, Verify; Vitest + React Testing Library + MSW. No mocked DB driver where translation/serialization matters. Runtime browser acceptance separate from MSW (Q7).

**Target Platform**: .NET server (ASP.NET Core Minimal API + Work enrichment) + Vite/React SPA; Docker Compose local stack.

**Project Type**: Domain/metadata + Infrastructure adapter/chain/persistence + existing details DTO + web movie-details feature slice.

**Performance Goals**: At most the documented videos language requests after TMDb identity resolve; no caption probing or thumbnail fan-out; iframe `loading="lazy"` (Q3, Q5).

**Constraints**: No explanatory comments outside allowed exemptions; do not delete or rewrite unrelated comments. Build/refactor cyclomatic-complexity thresholds from `harness.yml` only; never lower thresholds. No new port/project/layer/dependency; no LocalPlay/secret/Process.Start changes; no global CSP middleware. Mutation hygiene: selection literals are `const`; no new static initialisers that bake mutant state. Tests use deterministic `TimeProvider`/`FakeTimeProvider` and seeded `Random` when needed; no `DateTime.Now`, `Task.Delay`, or wall-clock waits. Private helper extraction stays inside Q8 allowlisted production files — unmet complexity gate needs a new Patron ruling, not silent allowlist expansion. Never hand-edit generated OpenAPI/TS/MSW/migration snapshot.

**Scale/Scope**: personal library; Size M; no ui tag; first third-party embed.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (current 1.10.0 in worktree). No constitution amendment and no ADR (Size M; Q8). No owner Gate 1 checkbox.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | No new ports/layers | PASS — extend existing `IMetadataProvider` / `TmdbMetadataProvider` / `ChainedMetadataProvider`; no new port (Q2, Q8). |
| II (Explicit Handlers and Decorators) / V (Validated Inputs, Consistent Error Responses) | Public API stability | PASS — no new route/status; nullable `trailerKey` on existing metadata response only (Q1). |
| IV (Enrichment state machine) | Existing statuses/transitions | PASS — videos failure is optional enrichment → null Trailer key; does not invent new enrichment statuses (Q2). |
| VI (Observability) | Safe structured logging | PASS — existing `TmdbOptions` credentials; never log API keys (Q2; Principle VII). |
| VII (Configuration / time) | Validated options; TimeProvider | PASS — reuse existing TMDb enablement/credentials; no new options record; cancellation propagates (Q2). |
| VIII (Ubiquitous Language) | Glossary | PASS — append **Trailer key** to `CONTEXT.md` (Q1, Q8). |
| IX (Test Pyramid) | WireMock for HTTP; real DB where translation matters | PASS — WireMock for TMDb videos; Testcontainers for `trailer_key`; Vitest/RTL/MSW for web (Q7). |
| Tech Stack / API docs | OpenAPI at `web/src/api/openapi.json`; drift fails CI | PASS — regenerate baseline + `npm run generate:api` (Q7). |
| Governance / Static-Analysis Gates | Harness thresholds; no lowered gates | PASS — Phase B only; see Gates. Web via web-implement. |

### Complexity Tracking

No constitution violations requiring justification. No amendment path.

## Approach

- **Phase A (frozen planning package)**: Quill drafts spec/plan/tasks + checklist + receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon facts only; Keel runs read-only `/speckit-analyze` and plan challenge; **Patron provisional Gate 1 → Rigger opens the spec PR → user merges Gate 1 → Keel T000 merge receipt**. No Build code/gates here (Q7).
- **Phase B pickup (after Gate 1)**: Wisp fresh recon drift check against main for the Q8 file/contract set, including unspecified entity/projection/copy-site filenames; Keel blocking reanalysis if drift changes contract/plan; Gauge baseline before code edits (Q7–Q8).
- **Dependency-ordered implementation (Q7)**:
  1. Domain Trailer key + metadata copy paths + nullable EF storage/migration/readback.
  2. TMDb videos retrieval, deterministic selection, chain merge, optional failure handling.
  3. Existing details DTO mapping + committed OpenAPI baseline (Verify).
  4. Regenerate TypeScript and MSW via `npm run generate:api`.
  5. Integrate Trailer UI + typed fixtures/components in `web/src/features/movie-details/`.
  6. Verify complete behavior, gather runtime evidence, include CSP/internet note in the PR.
- **Tests before behavior where feasible**: Anvil lands new-behavior selection/UI regressions red where feasible; green locks for existing enrichment failure/cancellation/OMDb-primary short-circuit stay green (Q7).
- **Refactor**: Cog runs `/refactor` once over completed C# implementation (task-pipeline Phase 3 step 3). Web uses web-implement lint/typecheck/Vitest — never apply C# analyzer scripts to TypeScript.
- **Verification**: Gauge harness + web-implement + OpenAPI drift; runtime acceptance; Size M three-axis review (Sentry/Ledger/Compass) under Q6 bar; Phase 5 rebased gates before awaiting-merge. User merges; never auto-merge.

## Project Structure

```text
specs/DEV-463/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md

src/LamuFlix.Core/Domain/
  MovieMetadata.cs                         (edit: nullable TrailerKey + copy paths as recon’d)

src/LamuFlix.Infrastructure/Adapters/
  TmdbMetadataProvider.cs                  (edit: videos fetch + selection)
  TmdbMetadataProvider.Mapping.cs          (edit: video DTOs / original_language as needed)
  ChainedMetadataProvider.cs               (edit: supplement TrailerKey without scalar replace)

src/LamuFlix.Infrastructure/Persistence/
  EfMovieRepository.cs                     (edit: write/read/clear TrailerKey)
  Configurations/MovieConfiguration.cs     (edit: trailer_key column)
  Migrations/*                             (tool-generated nullable column + snapshot)

src/LamuFlix.Api/Endpoints/
  LibraryResponses.cs                      (edit: MovieMetadataResponse.trailerKey)

web/src/api/
  openapi.json                             (regenerated baseline)
  types.ts                                 (generated)

web/src/mocks/
  handlers.ts                              (generated)
  fixtures.ts                              (edit: trailerKey fixtures)

web/src/features/movie-details/
  MovieDetailsView.tsx / .css              (edit: Trailer section placement)
  Trailer*.tsx / tests                     (optional small component in this folder)

CONTEXT.md                                 (Phase B T003: Trailer key glossary; excluded from spec PR)
```

**Layout decisions**:

- No research/data-model/contracts artifacts — decisions frozen in brief; DTO field encoded in spec + generation tasks.
- Quill owns spec/plan/tasks/receipt/checklist. Quill does not edit `brief.md` / `CONCLUSIONS.md` / `ASSUMPTIONS.md` / ADRs / constitution / production code.
- Unspecified entity/projection/copy-site filenames are Phase B recon obligations (Q8) — do not invent shapes in Phase A.

## Test Strategy

| Surface | Coverage |
|---|---|
| Selection / enrichment | pt-BR vs original order; nonofficial pt-BR vs official original; official within tier; deterministic ties; input-order independence; duplicate keys; unknown original language; empty results; wrong site/type; blank/malformed/URL-shaped keys; partial/all videos failures; cancellation (Q7) |
| Composition | OMDb-primary supplementary Trailer key without scalar replacement; TMDb-found path; disabled TMDb; no eligible trailer; metadata copy and clear (Q7) |
| Persistence / API | Real-DB key roundtrip; null existing rows; re-enrichment replace/clear; details JSON null/present; OpenAPI Verify drift (Q7) |
| Web / MSW | Present, absent, invalid key; generated-contract fixture compatibility; section hidden without key; fixed host/path, title, lazy, caption params, no autoplay, note, 16:9, placement before Related Movies (Q7) |
| Runtime | Working embed + blocked-network usability in provisioned browser; onLoad alone insufficient; unavailable external access → `Could not run` (Q5, Q7) |

No Gherkin (acceptance-tests opt-in; not requested). Property tests: add FsCheck selector permutation-invariance and dedupe-idempotence using existing infrastructure; no property-test opt-out for these invariants. Exact suite/project is confirmed by T001.

## Gates (after Gate 1 and implementation — Gauge owns configured Phase 3 gates)

Thresholds for build and refactor cyclomatic complexity come only from `harness.yml`.

1. Explicit changed-C# scope: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml, `./scripts/run-jetbrains-inspectcode.ps1` (never `-All`).
2. Affected unit/integration suites, then full `dotnet test`.
3. Property / vulnerable-packages / `dotnet format --verify-no-changes` per harness; distinguish PASS, failure, SKIPPED (scope-empty), SKIP, mutation N/A, Could not run.
4. No `/architect` invocation (task-pipeline Phase 3 step 4). Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` from worktree) for changed production C#.
5. Web-implement gates + `npm run check:api` / OpenAPI drift for generated artifacts.
6. Runtime SMOKE-001–003 / working-embed + blocked-network evidence; Size M review (Sentry, Ledger, Compass) under Q6 closing bar; max two analysis/fix and two challenge/review/fix rounds per loop.
7. Fresh rebased gates + hosted checks before awaiting-merge; user merges.

Phase A runs none of these and claims no gate result.

## Risks

- **TMDb `language` list incompleteness**: documented parameter has undocumented filter semantics; mitigated by Q3 dual-request union + client-side ranking over returned candidates only (recon-DEV-463-2 §9).
- **Caption availability not guaranteed**: ticket and Q3 accept uploader/player control; always request params; honest note in UI (Q5).
- **First third-party embed / CSP**: deployments with CSP need `frame-src` documentation; no middleware invented here (Q5).
- **Offline/LAN**: embed will not play without internet; local note + isolated layout keep details usable (Q5).
- **Pickup drift / unspecified copy sites**: mandatory Phase B recon before coding (Q7–Q8).
- **Complexity extraction pressure**: helpers stay inside allowlisted files; unmet gate needs Patron ruling.

## Ordering

See `tasks.md`. Numbered local phases below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` dispatch — **not** canonical `task-pipeline` phases. Canonical Build/review/delivery remain Phase 3 / Phase 4 / Phase 5. Seat map: Wisp/Keel/Gauge Pickup (local Phase 1); Anvil code phases 2–5; Cog local Phase 6; Gauge local Phase 7; local Phases 8–9 ↔ canonical Phase 4; local Phase 10 ↔ canonical Phase 5.

```text
Phase A (Spec)     Quill draft → Keel analyze/challenge → Patron provisional Gate 1 → user merges Gate 1
Phase 0            T000 Keel Gate 1 merge receipt (blocks Build)
Phase 1            T001 Wisp drift recon → Keel reanalysis → Gauge baseline (blocks implementation)
Phase 2            T002 → T005 Anvil domain + EF Trailer key (US1 foundation)
Phase 3            T006 → T009 Anvil TMDb selection + chain merge (US1)
Phase 4            T010 → T013 Anvil details DTO + details JSON null/present + OpenAPI + generate:api (US2)
Phase 5            T014 → T016 Anvil Trailer UI + MSW component tests (US3)
Phase 6            T017 Cog /refactor once (required stage; edits may be conditional)
Phase 7            T018 → T023 Gauge configured C#/web gates + mutation + API drift
Phase 8            T024 Runtime SMOKE / embed + blocked-network (US4) + PR CSP note
Phase 9            T025 Sentry/Ledger/Compass; Keel adjudication; Cog remediation (cap 2)
Phase 10           T026 → T027 rebased gates + awaiting-merge — user merges
```

## Challenge round 1 refinements (Patron-approved)

Trim original_language and treat pt/pt-BR variants as fetch-equivalent case-insensitively; language/site/type comparisons are case-insensitive, keys ordinal. MovieMetadata owns the single C# ASCII/length validator; pure selection stays in the existing Mapping partial and reuses it; HTTP fetch stays in the provider. Web revalidation is deliberate. No new abstraction/dependency is introduced.

Test matrix additionally locks videos-only 429/quota/timeout isolation (no retry/DeadLetter, preserve independently successful candidates), unchanged details-path retries, failed supplement with OMDb scalars/null key, shared-token cancellation before request two, FsCheck permutation/dedupe properties, case variants and hostile titles. Details snapshots update through supported Verify workflow. T001 confirms exact test projects and generation workflow before implementation; targeted new files in existing test projects are allowed.

Anvil lands CONTEXT.md in T003; Gauge checks its presence at runtime. Rigger excludes the pending glossary edit and transient _quill-report-bernstein.txt from the spec PR, preserves unrelated changes, and deliberately handles the intake pin after confirming its exact path/manifest. No Keel deletion or rollback is authorized. CONCLUSIONS.md records every finding disposition and the distinction between decisions and unverified inventory labels in the Patron ruling.
