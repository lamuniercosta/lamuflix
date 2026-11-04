# Implementation Plan: Rescan replaced movie files and resolve primary video at play time

**Branch**: `feature/438-spec` | **Date**: 2026-11-04 | **Spec**: `specs/DEV-438/spec.md`
**Input**: Frozen `brief.md` (Q1–Q8 + Phase 2 adjudication addendum); `CONCLUSIONS.md` (Q1–Q8 + Phase 2 rulings 1–8); `ASSUMPTIONS.md`; `spec.md`; Maestri notes `DEV-438`, `recon-DEV-438` (203 lines), `recon-DEV-438-2` (77 lines); `specs/PRODUCT.md`; `.specify/memory/constitution.md` v1.10.0 (gates cite only; Quill does not edit constitution or ADRs)
**Status**: **PLAN FROZEN** — Keel verified the Phase 2 repair round on 2026-11-04; adjudication: `adjudication-DEV-438` in LamuFlix notes. Owner Gate 1 not approved; no implementation clearance.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md`, `checklists/requirements.md` (Quill). No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief; public routes/DTOs/statuses are encoded in `spec.md` / OpenAPI generation tasks rather than a separate contracts folder. No new ADR (brief Q3/Q8).

## Summary

Let an already-imported movie refresh stored Format after an in-place file replacement without creating another row or changing identity/path/title/year/metadata/enrichment/user flags, and make import/rescan/play share one top-level token-filtered primary-video selector. Deliver API-only per-movie and synchronous snapshot bulk rescan, map safe failures (including import's change from unmapped generic 500 to 404/409 for expected missing/no-eligible outcomes), and resolve the current file at play after `EnsureEnabled` so stale Format cannot pick the wrong player — without play-time persistence, UI, schema, workers, or a new Core port. Spec workspace produces documents only; freeze left to Keel.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).
**Primary Dependencies**: Existing pins only (System.IO.Abstractions, EF Core, FluentValidation, ASP.NET Core Minimal APIs, AutoFixture, Faker.Net, FsCheck). **No new packages** (brief §2.3; Q6).
**Storage**: Existing Postgres `movies` table / unique LibraryPath index — Format column only via existing `Baseline`/`From`/`Apply` diff; **no schema or migration** (Q1/Q5; Phase 2 ruling 5).
**Testing**: xUnit v3, Shouldly, NSubstitute, MockFileSystem, AutoFixture/Faker.Net, Theory/MemberData, Testcontainers Postgres, existing host/endpoint fixtures, fake process starter for LocalPlay. No Gherkin. No EF driver mocking / EF InMemory.
**Target Platform**: .NET server (ASP.NET Core Minimal API) with optional LocalPlay process launch gated by `Features:LocalPlay`.
**Project Type**: Ports and adapters with a feature-organised core and explicit handlers with decorators, in existing Core / Infrastructure / Api projects + generated web contract artifacts.
**Performance Goals**: Synchronous bulk over personal-library scale; sequential per-item processing; no new concurrency subsystem; no progress/worker/deadline guarantee beyond RequestAborted cancellation and cooperative filesystem limits (Q5; Phase 2 F7).
**Constraints**: No new Core port/layer/project/package/ADR/composite wrapper; no UI; no play-time Format write; no shell concatenation; preserve ArgumentList + Process.Start; never lower harness thresholds; never hand-edit generated OpenAPI/TS/MSW; preserve unrelated `harness.yml` modification; TimeProvider for clocks; `const` / non-mutable static initialization (AGENTS Stryker); no explanatory comments outside allowed exemptions.
**Scale/Scope**: Size M; personal library (~265 in-place swaps + report-only missing folders). Bounded tracking at that scale is accepted (Phase 2 ruling 5).

## Constitution Check

Gates checked against `.specify/memory/constitution.md` v1.10.0 on this worktree. Brief + Phase 2 rulings: no new ADR; no constitution departure; no owner structural checkbox (`PRODUCT.md` §5 / brief Q8 / Phase 2 addendum). Quill invents no amendment path.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; adding a port requires an ADR | **PASS** — extend existing `IMediaLibraryScanner`, `IMediaPlayerLauncher`, `IMovieRepository` only; sealed `PrimaryVideo` record is data, not a new interface (Q3; Phase 2 ruling 7). |
| II (Explicit Handlers / Decorators) | `AddHandler` registration | **PASS** — both rescan handlers register through existing AddHandler/decorators (Tracing → Logging → Validation → handler) and call shared `internal sealed RescanMovieOperation`; no handler-calls-handler (Q6; Phase 2 ruling 4). |
| III (Typed query model) | MovieQuery / filter/sort | **N/A** — DEV-438 adds no filter/sort/query-model surface (Phase 2 Compass F5). |
| IV (Domain transitions) | Movie private setters + domain methods | **PASS** — `Movie.RefreshFormat(MediaFormat)` Format-only mutation; no title/year re-parse or enrichment enqueue (Q2–Q3; Phase 2 ruling 8). |
| V (Validated inputs / consistent errors) | Domain exceptions → ProblemDetails | **PASS** — approved 404/409/422/400/403/500 taxonomy; narrow adapter translation shared by Scan/ResolvePrimaryVideo including import 404/409 change; safe ProblemDetails + traceId; no global remapping (Q4; Phase 2 ruling 6). |
| VI (Observability) | TelemetryConstants / health | **N/A for new work** — existing tracing/logging decorators and `/health/live` + `/health/ready` remain unchanged; no new telemetry names or observability dependency (Phase 2 Compass F5). |
| VII (Configuration / LocalPlay / Process.Start) | LocalPlay gated; no Core Options | **PASS** — `EnsureEnabled` on existing launcher port; Launch retains gate; ArgumentList + Process.Start unchanged mechanism; rescan ungated (Q3). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — Primary video / Rescan glossary alignment at delivery after recon, with Rescan vs Import contrast (Q2/Q6; Phase 2 ruling 7; [assumed] naming). |
| IX (Test Pyramid) | Real Postgres where persistence matters | **PASS** — Testcontainers for Format-only persistence + concurrent enrichment proof; MockFileSystem selector; fake starter for process; mandatory real-disk host case with unique temp dir cleanup; AutoFixture/Faker.Net + Theory/MemberData (Q7; Phase 2 L3–L4). |
| Coding conventions | >3 ports signal; sealed; CT rules; no mutable static init | **PASS** — `PlayMovieCommandHandler` retains `IMovieCatalog`, `IMediaPlayerLauncher`, `IMediaLibraryScanner`, `IPlaybackHistory`, and `TimeProvider`; Patron assessed the dependency-count signal and found no constitution departure / no composite port (Phase 2 ruling 3; recon-DEV-438-2 §7). Exclusion tokens as `const`; no mutable static initializer in mutated projects (AGENTS). RequestAborted flows through async ops with cooperative pre-side-effect checks (Phase 2 ruling 8). |
| API / contract rules | per-feature MapXxxEndpoints; generated OpenAPI/TS/MSW | **PASS** — new `MapRescanEndpoints` follows existing per-feature grouping; both features reference only ports/shared types; contract workflow: `LAMUFLIX_REGENERATE_OPENAPI` opt-in writes baseline + Verify snapshot together, `npm run generate:api` → `types.ts` + `src/mocks/handlers.ts`, `npm run check:api` verifies; never hand-edit (recon-DEV-438-2 §6; Phase 2 ruling 8). |
| Governance / PR Quality / Static-Analysis Gates | Honest exits; never lower thresholds | Phase B / Build: standalone build then full tests; report actual script exits; SKIPPED/SKIP/N/A/Could not run never relabeled PASS (Q7–Q8; Phase 2 Compass F5). |

### Complexity Tracking

No unjustified constitution violation. No Complexity Tracking rows. No ADR required for the chosen existing-port approach (brief Q3/Q8; Phase 2 ruling 3).

## Approach

- **Phase A (this draft + repair)**: Quill drafts then applies Keel's consolidated Phase 2 repair list to owned artifacts; Keel owns read-only `/speckit-analyze`, plan challenge, freeze; ordinary owner Gate 1 remains pending. No production edits, tests, gates, commits, or PR here (Q7–Q8; Phase 2 addendum).
- **Shared foundation first**: sealed `PrimaryVideo` on existing ports surface (`PrimaryVideo.cs`); extend `IMediaLibraryScanner.ResolvePrimaryVideo`; internal selector beside `SupportedVideoExtensions` in Infrastructure/FileSystem; Scan and ResolvePrimaryVideo share selector and translate only expected missing/no-eligible (incl. only-excluded) to `NotFoundException`/`InvalidTransitionException` (import thereby moves from unmapped generic 500 → 404/409); unexpected candidate IO propagates 500 with no silent skip; `Movie.RefreshFormat`; update scanner/import/ImportEndpoints tests and OpenAPI metadata narrowly (Q6 step 1; Phase 2 rulings 1, 6).
- **Per-movie rescan**: sealed `RescanMovieCommand` / `RescanMovieResult` / validator / handler in `Core/Features/Library` invoking `internal sealed RescanMovieOperation` (`IMovieRepository` + `IMediaLibraryScanner`); extend `EfMovieRepository` Baseline/From/Apply/refresh/rebuild for Format-only diff; `Api/Endpoints/RescanEndpoints` + `RescanMovieResponse`; HandlerRegistration + `ApiEndpoints` / `MapRescanEndpoints` composition; RequestAborted + cooperative pre-resolve/pre-save checks; real-Postgres Format-only proof including concurrent enrichment survival and no-UPDATE when unchanged (Q6 step 2; Phase 2 rulings 4–5, 8).
- **Bulk rescan**: `RescanLibraryCommand` / `RescanLibraryResult` path in same Library feature; both handlers decorated at entry and share `RescanMovieOperation`; scalar ascending ID snapshot on `IMovieRepository` / `EfMovieRepository` (AsNoTracking; no entity preload; one GetAsync per unique ID); catch only expected load/resolve failures with saves outside the catch; any unexpected abort keeps prior commits; cancellation never counted failed; aggregate endpoint tests (Q6 step 3; Phase 2 rulings 4–5, 8).
- **Playback**: amend `PlayMovieCommandHandler` to lookup → EnsureEnabled → resolve → launch → history while retaining the four ports + TimeProvider; extend launcher port + enabled/disabled adapters; launch-failure remains unexpected 500 with zero history/Format write; cancellation pre-launch / in-flight append semantics; update playback/guard tests with folder-valued source data (Q6 step 4; Phase 2 rulings 2–3, 8).
- **Contracts / glossary**: regenerate via `LAMUFLIX_REGENERATE_OPENAPI` then `npm run generate:api` (includes forced Verify snapshots + `src/mocks/handlers.ts`); `npm run check:api`; CONTEXT.md after recon with Rescan vs Import (Q6 step 5; recon-DEV-438-2 §6).
- **Verification / review**: required unit/integration/endpoint/real-disk suites; standalone build then full delivery gates; review against fixed base/head under Q8 closing bar and two-round cap (Q6 step 6; Q7–Q8).

## Project Structure

Verified existing paths cite refreshed `recon-DEV-438` / `recon-DEV-438-2`. Proposed new paths stay inside existing feature/test folders and are marked *(proposed)*.

```text
specs/DEV-438/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md

src/LamuFlix.Core/
  Ports/IMediaLibraryScanner.cs                    (existing — extend ResolvePrimaryVideo; recon §1)
  Ports/IMediaPlayerLauncher.cs                    (existing — extend EnsureEnabled; recon §1)
  Ports/IMovieRepository.cs                        (existing — scalar ID snapshot as needed; recon §3)
  Ports/IMovieCatalog.cs                           (existing — playback; recon §2)
  Ports/IPlaybackHistory.cs                        (existing — playback; recon §2)
  Ports/PrimaryVideo.cs                            *(proposed)* sealed record Path+Format
  Domain/Movie.cs                                  (existing — RefreshFormat; recon)
  Features/Library/RescanMovieCommand.cs           *(proposed)* + Handler/Validator
  Features/Library/RescanMovieResult.cs            *(proposed)*
  Features/Library/RescanLibraryCommand.cs         *(proposed)* + Handler/Validator
  Features/Library/RescanLibraryResult.cs          *(proposed)*
  Features/Library/RescanMovieOperation.cs         *(proposed)* internal sealed shared op
  Features/Playback/PlayMovieCommandHandler.cs     (existing — amend order + scanner dep; recon §2)

src/LamuFlix.Infrastructure/
  FileSystem/DirectoryMediaLibraryScanner.cs       (existing — selector + narrow 404/409; recon §1)
  FileSystem/SupportedVideoExtensions.cs           (existing — membership unchanged; recon §1)
  Persistence/EfMovieRepository.cs                 (existing — Baseline/From/Apply Format + snapshot; recon §3)
  Playback/ProcessMediaPlayerLauncher.cs           (existing — EnsureEnabled no-op; recon §1)
  Playback/DisabledMediaPlayerLauncher.cs          (existing — EnsureEnabled throws; recon §1)
  Playback/PlaybackServiceCollectionExtensions.cs  (existing; recon §1)

src/LamuFlix.Api/
  Endpoints/RescanEndpoints.cs                     *(proposed)* MapRescanEndpoints + DTOs
  Endpoints/RescanMovieResponse / RescanLibraryResponse  *(proposed)*
  Endpoints/ImportEndpoints.cs                     (existing — narrow forced problem metadata; recon-DEV-438-2 §8)
  Endpoints/ApiEndpoints.cs                        (existing — compose MapRescanEndpoints)
  Endpoints/PlaybackEndpoints.cs                   (existing — problem metadata as needed; play stays 204)
  HandlerRegistration.cs                           (existing — AddHandler for rescans)

web/src/api/openapi.json                           (existing generated baseline — tooling only; recon-DEV-438-2 §6)
web/src/api/types.ts                               (existing generated — tooling only)
web/src/mocks/handlers.ts                          (existing generated MSW — tooling only)
tests/.../Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json  (forced Verify snapshot)

CONTEXT.md                                         (Primary video / Rescan glossary at delivery)

tests/LamuFlix.UnitTests/
  FileSystem/DirectoryMediaLibraryScannerTests.cs  (existing — extend; recon-DEV-438-2 §6)
  Features/Import/ImportMovieFolderCommandHandlerTests.cs  (existing — extend)
  Features/Playback/PlayMovieCommandHandlerTests.cs        (existing — extend; append-cancellation preserved)
  Playback/ProcessMediaPlayerLauncherTests.cs      (existing)
  Playback/DisabledMediaPlayerLauncherTests.cs     (existing)
  Domain/ + Features/Library/                      *(proposed new files inside existing folders)*

tests/LamuFlix.IntegrationTests/
  ImportEndpointTests.cs                           (existing — add 404/409 cases; recon-DEV-438-2 §6)
  PlaybackEndpointTests.cs                         (existing — folder-valued catalog + resolver)
  OpenApiContractTests.cs                          (existing — regeneration opt-in; recon-DEV-438-2 §6)
  ApiEndToEndImportTests.cs / ApiEndToEndPlaybackTests.cs  (existing patterns for real-disk)
  (new rescan endpoint / persistence tests — *(proposed)* inside this project)
```

**Layout decisions**:

- No research/data-model/contracts artifacts — decisions frozen in brief; routes/fields encoded in spec + generation tasks.
- Quill owns spec/plan/tasks/receipt/checklist. Quill does not edit `brief.md` / `CONCLUSIONS.md` / `ASSUMPTIONS.md` / ADRs / constitution / production code.
- Exact new test filenames beyond the recon-named surfaces are chosen at Build inside existing test projects without expanding scope (brief Q6).
- Preserve and exclude unrelated `harness.yml` modification from this task's staging.
- T001 still requires pickup drift recon and STOP on missing facts (Phase 2 ruling 7).

## Generated-contract workflow (recon-DEV-438-2 §6)

1. Opt-in export: set `LAMUFLIX_REGENERATE_OPENAPI=true`, run `dotnet test … --filter FullyQualifiedName~OpenApiRegeneration`, unset the env var — writes committed `web/src/api/openapi.json` and the Verify snapshot together.
2. From `web/`: `npm run generate:api` produces `src/api/types.ts` and `src/mocks/handlers.ts` (AUTO-GENERATED headers; never hand-edit).
3. `npm run check:api` verifies regeneration byte-equality; web lint runs `check:api` first.
4. Forced footprints in this ticket: baseline OpenAPI, Verify snapshot, `types.ts`, and `src/mocks/handlers.ts` — all tooling-only.

## Test Strategy

| Surface | Coverage |
|---|---|
| Selector / import (US1) | MockFileSystem Theory/MemberData + AutoFixture/Faker.Net: extension/case/size/tie/token-boundary/subfolder/only-excluded; explicit unreadable-candidate IO fault (no silent skip); deterministic selection under permuted listing property coverage in approved suites; import 404/409 (incl. only-excluded) replacing prior generic 500 |
| Domain (US1) | `RefreshFormat` preserves non-Format fields; test-first where feasible |
| Per-movie rescan (US2) | Unit changed/unchanged/unknown/missing/no-video/pre-save + in-flight cancellation; Postgres avi→mkv Format-only + concurrent enrichment-owned column survives + unchanged issues no UPDATE; endpoint 200/404/409/422/400 + RequestAborted wiring + safe ProblemDetails |
| Bulk (US3) | Unit empty/mixed/rerun/deleted-snapshot/unexpected fault/cancellation (unexpected IO + cancellation not counted failed); saves outside expected-only catch; no write when unchanged; Postgres mixed + idempotent rerun; endpoint aggregate |
| Playback (US4) | Unit order + stale Format ignored + disabled zero-probe + failure no history + launch-failure 500/no history/no Format + pre-launch cancellation + existing append-cancellation; endpoint folder-valued data + fake starter; mandatory real-disk import→swap→rescan→play with unique temp directory + guaranteed cleanup + fake starter only |
| Contracts (US5) | LAMUFLIX_REGENERATE_OPENAPI + generate:api + check:api; forced snapshots/mocks; CONTEXT.md glossary (Rescan vs Import); ordinary success tests for both rescan routes (no routing workaround) |
| Delivery gates | standalone build; then full unit/integration/architecture/property; Roslyn; complexity; InspectCode; vulnerable packages; format; configured mutation; OpenAPI/TS/MSW drift; applicable web checks — honest exits only |

No Gherkin. Phase A / this repair round runs none of these and claims no gate result. Code tasks remain unchecked.

## Gates (Build — after Gate 1 and implementation)

1. Standalone `dotnet build`, then targeted then full `dotnet test` (unit + integration + architecture as configured), including the mandatory real-disk case.
2. `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (then refactor threshold from harness.yml), `./scripts/run-jetbrains-inspectcode.ps1` on changed `.cs`.
3. `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`.
4. Configured mutation (`pwsh -NoProfile -File ./scripts/run-mutation.ps1` without inventing `--since` from worktrees); OpenAPI/TS/MSW drift (`npm run check:api` / web gates) and applicable web checks.
5. Review under Q8 closing bar (Critical/High/Medium block; Low explicit disposition); two rounds total: initial plus one remediation/recheck; unresolved blockers return to Bernstein.

**Numeric provenance**: all thresholds from `harness.yml` / rendered configs only — never hardcode or lower. scope-empty SKIPPED, opt-out SKIP, mutation N/A, and Could not run remain distinct non-PASS dispositions (Q7).

## Risks

- **Stale Format vs play**: mitigated by resolve-at-play using actual extension without persisting Format (Q1/Q3).
- **Unreadable candidate IO (Sentry F1)**: unexpected generic 500 / bulk abort with prior commits; never silent skip of a supported candidate — covered by explicit selector unit fault case (Phase 2 ruling 1).
- **Resolve-to-launch race (Sentry F2)**: launcher-reported failure remains unexpected 500 with zero history/Format write; no retries/new taxonomy; no promise the launcher always detects a disappeared media argument — unit regression required (Phase 2 ruling 2).
- **Import behavior change (Ledger M4)**: token exclusions now apply on import; missing/no-eligible (incl. only-excluded) move from unmapped generic 500 → 404/409 — intentional; scanner/import/ImportEndpoints tests + OpenAPI metadata must be updated narrowly (Q2/Q6; Phase 2 ruling 6).
- **404 overlap** (unknown id vs missing folder): approved shared status; messages remain caller-safe without filesystem paths (Q4).
- **Partial bulk progress**: approved; rerun is safe; any unexpected abort keeps prior commits (Q5; Phase 2 Compass F2).
- **Legacy sample-only catalog (Sentry F6)**: accepted — now no eligible video ⇒ 409 on rescan/play, failed on bulk; no repair/flag/migration (Phase 2 ruling 8).
- **Synchronous bulk duration (Sentry F7)**: may be slow at personal-library scale; cancellation wiring only; no progress/worker/deadline guarantee; cooperative filesystem limits; Q5 retained (Phase 2 ruling 8).
- **LocalPlay / Process.Start care list**: deliberate §2.3 ruling — resolved file argument only; mechanism unchanged (Q3).
- **Generated contract drift**: tooling-only regeneration per recon-DEV-438-2 §6; never hand-edit (Q6).
- **Unrelated harness.yml dirt**: preserve; exclude from staging (brief Status).
- **Pickup drift**: Build refreshes recon before coding (T001); STOP on missing facts; does not invent decisions (Phase 2 ruling 7).

## Ordering

See `tasks.md`. Sequence (brief Q6):

```text
Phase A   Quill spec/plan/tasks (draft + Phase 2 repair) → Keel analyze / freeze → owner Gate 1 / spec merge
Phase B   Build pickup / drift recheck (T001 STOP on missing facts)
Phase 1   Shared selector + port extensions + Movie.RefreshFormat + scanner/import 404/409 test updates (US1)
Phase 2   Per-movie rescan command/API + Postgres Format-only + concurrent enrichment proof (US2)
Phase 3   Bulk snapshot rescan + repository projection + aggregate tests (US3)
Phase 4   Playback EnsureEnabled + resolve-at-play + launch-failure/cancellation tests (US4)
Phase 5   OpenAPI/TS/MSW generation + CONTEXT.md glossary (US5)
Phase 6   Verification, review, exact-head delivery workflow under Conductor (Q6 step 6; Q7–Q8)
```

Gate 1 / spec merge precede Build implementation. This planning adjudication runs no production tests or delivery gates and creates no commit or PR.

## Plan freeze receipt — Keel, 2026-11-04

**PLAN FROZEN** after independent verification of Quill's single consolidated repair pass against Risk (Sentry), Standards (Ledger), Spec (Compass), refreshed recon, brief/Patron rulings, and constitution v1.10.0. Full dispositions: `adjudication-DEV-438` in LamuFlix notes. Initial challenge plus one repair/recheck completes the two-round loop; no unresolved Critical/High/Medium findings, and all Low/Info findings explicitly disposed.

Read-only cross-artifact analysis: 15 FRs + 6 SCs covered by 29 tasks (T000–T028), 100% requirement coverage; no remaining blocking inconsistency, ambiguity, duplication, unmapped task, or constitution conflict. Verified repair evidence: spec.md FR-001–FR-015 and US1–US5; this plan Constitution Check/Approach/Test Strategy/Risks; tasks.md T002–T021 and dependencies. The complete coverage map and per-axis evidence are in the adjudication note.

This is a planning freeze only. Owner Gate 1/spec merge remains pending, followed by T001 fresh pickup recon/drift reanalysis. No production verification result is claimed. Unrelated `harness.yml` dirt remains excluded from staging.
