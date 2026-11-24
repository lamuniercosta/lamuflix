# Implementation Plan: First release v0.1.0 (CHANGELOG and running-app version)

**Branch**: `feature/547-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-547-spec`) | **Date**: 2026-11-24 | **Spec**: `specs/DEV-547/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q6); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-547`, `recon-DEV-547`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status** — PLAN FROZEN after Phase 2 step 6 on 2026-11-24. See the dispositions in `CONCLUSIONS.md`. Gate 1, Build, and live release acceptance remain pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `contracts/version-endpoint.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` — grill Q1–Q6 freeze inventable unknowns; no schema change; API shape under `contracts/`.

## Summary

Deliver the first home-cluster release record and running-app version signal for `v0.1.0`: owner-facing Keep a Changelog entry plus README link; bake exact source commit into API and web images; carry release tag through home promotion (empty on manual/Compose); expose `GET /api/version` as `{ release, commit }` through the OpenAPI → TypeScript chain; show one version line in the Q3-selected UI location; verify pre-merge gates/parity; then execute and evidence the authorized post-merge tag after DEV-546 and a green nightly on the exact `main` commit.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Api`) + React 19 / TypeScript / Vite (`web/`) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing Minimal API endpoint mapping (`ApiEndpoints`), Dockerfiles, GitHub Actions CI/`promote-home`, Compose, k8s home overlay, OpenAPI → generated TypeScript chain. No new NuGet/npm dependency (`brief.md` Frozen scope; `PRODUCT.md` §5 item 1).

**Storage**: No database or migration change. Metadata is process environment / image ENV (`LAMUFLIX_COMMIT`, `LAMUFLIX_RELEASE`) and existing `RELEASE.json` on `env/home`.

**Testing**: Existing xUnit integration host conventions under `tests/LamuFlix.IntegrationTests`; Vitest/RTL web state tests; OpenAPI drift / `generate:api` / `check:api`; offline `scripts/Test-DeployParity.ps1`; configured C# and web gates. No new test framework/dependency. Never mock the data-access driver for query-translation behaviour (N/A for this ticket’s version path).

**Target Platform**: Local API + Compose SPA; home cluster promotion path; port 3100 readback after tagged promote.

**Project Type**: Existing ports-and-adapters .NET API with feature-organised endpoints; SPA consumes generated types only; CI/CD and Compose/k8s delivery metadata.

**Performance Goals**: Version read is a small metadata response; no invented latency SLA.

**Constraints**: Charter §2.3 care-list items deliberately ruled (Q1–Q6). Public API shape frozen by Patron under ticket Wanted 5 (dedicated route). DEV-546 required first; DEV-560 optional for display location only. No LocalPlay/secrets/`Process.Start`. Honest gate labels; never lower thresholds; never merge. Size M requires Risk (Sentry), Standards (Ledger), Spec (Compass). Closing bar includes Critical–Low in-scope; max two review/fix rounds (`brief.md` Q6).

**Scale/Scope**: Size M; changelog/README + version API + web one-line display + CI/image/Compose/k8s metadata + post-merge first-tag evidence. Parent DEV-492.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox arose from the plan challenge (`PRODUCT.md` §5 item 4 satisfied by Patron Q1 citing ticket Wanted 5 / Done when 2). PR Quality Gate lines carried explicitly: **840** (`LamuFlix.ArchitectureTests` / project references), **849–850** (`TypedResults` + `WebApplicationFactory` integration coverage + committed OpenAPI drift green), **855–857** (no secrets / machine paths / `IConfiguration` magic strings; validated options at startup with Principle VII exception only), **967–969** (committed `web/src/api/openapi.json` Verify snapshot-tested and CI-regenerated; TypeScript client and MSW handlers generated from that document, never hand-written).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests (PR **840**) | **PASS** — version metadata is API/delivery concern; no new Core port or layer; `LamuFlix.ArchitectureTests` remains required green with other tests (`brief.md` Approach; constitution PR line 840). |
| II (Explicit Handlers) | Handlers + AddHandler for use cases | **PASS (N/A use-case path)** — `GET /api/version` is an **API-only deployment-metadata route** with **no application/domain use case**, so Principle II does not require a new sealed command/query + `AddHandler<…>` registration. Implementation is a Minimal API mapper under the existing `/api` group returning `TypedResults.Ok(...)` (PR **849**); no MediatR. Q1 surface unchanged. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — successful metadata read returns 200 via `TypedResults.Ok`; no new error taxonomy required for unset metadata (normalized into payload per Q2). |
| VI (Observability) | Safe diagnostics | **PASS** — commit/release are non-secret build metadata; do not log or embed credentials. |
| VII (Configuration / LocalPlay / Process.Start) | Options; no IConfiguration magic strings (PR **855–857**); LocalPlay gated | **PASS** — `LAMUFLIX_COMMIT` / `LAMUFLIX_RELEASE` are **process/deployment metadata** (image ENV / pod env / Compose), not application Options configuration. Capture effective process metadata and normalize it **once per host when mapping the route**. The selected process read needs **neither Options nor an additional layer** (brief decision 1; constitution PR **855–857**): **not** via `IConfiguration["…"]` magic strings and **not** via a startup-validated Options record that would reject unset/blank and invalidate the Q2 approved `"unknown"` / JSON `null` behaviour. No LocalPlay, secrets hardcoding, or Process.Start. Q2 / P2.6-2 contract unchanged. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — spec-local vocabulary only; no CONTEXT.md expansion claimed. |
| IX (Test Pyramid) | Meaningful layers; WebApplicationFactory (PR **849–850**) | **PASS** — endpoint coverage uses the constitution `WebApplicationFactory` integration convention under `tests/LamuFlix.IntegrationTests`, mirroring the recon-established peer `HealthEndpointTests` (`recon-DEV-547:14`). T002 uses a fresh host per metadata case in a dedicated nonparallel xUnit collection with prior-value save/restore in `finally`; Gauge runs the complete integration project as the isolation check. T001 records the host-fixture seam and existing mapper signature. Web state tests use generated MSW with deterministic holds; offline + hosted deploy-parity verdicts; post-merge live readback separate from pre-merge matrix. |

### Stack / API / workflow checks (outside Constitution Check principle rows)

| Check | Plan response |
|---|---|
| Tech stack / API docs (OpenAPI chain; PR **967–969**; Patron P2.6-1; `recon-DEV-547:83–90`) | **PASS** — exact generated-only envelope and npx-free flow: (1) T006 runs opt-in `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` with `LAMUFLIX_REGENERATE_OPENAPI=true` scoped to that invocation, writing `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together, then `DriftMatchesCommittedBaseline` with the switch cleared; stop before T007 on unrelated drift vs pickup HEAD; (2) T007 runs `npm run generate:api` from `web/` (`node scripts/generate-api.mjs`) producing exactly `web/src/api/types.ts` and `web/src/mocks/handlers.ts`, then `npm run check:api`. These four outputs are authorized for generator changes only; no `web/src/test/*`; never hand-edit. T001 checks evidenced paths/invocation for pickup drift. |
| Static-analysis gates | Every changed `.cs` task completes only after Roslyn, cyclomatic complexity, and InspectCode pass on those files; ArchitectureTests green; full pre-merge matrix per brief Q5. Apply the existing AGENTS no-explanatory-comment rule to changed C# and keep delivery configuration (Dockerfile/YAML/Compose) free of narrative comments by default (brief decision 2). |
| Web gates | When `/web` changes: lint (incl. `check:api`), test, build via configured web gates; honest SKIPPED if a lane is out of scope. |
| Mutation | T001 reads mutation applicability/exclusions on pickup HEAD and records anticipated verdicts. T010/T017 run applicable gates and record **actual** outcomes. A changed project with no eligible mutation tests and no exclusion is **FAILED** if the script returns exit 1. Report PASSED / FAILED / SKIPPED (scope-empty) / N/A honestly — never as PASS when not earned. Any harness exclusion edit needs a cited Patron ruling first (brief decision 2). |

### Complexity Tracking

No constitution violations requiring justification. No new architectural layer. Process-metadata capture-once-per-host (brief decision 1) preserves Q1–Q2 without Options or an extra layer. P2.6-1 / P2.6-2 are Patron-approved.

## Approach

- **Phase A** — Quill drafted and remediated the Spec Kit artifacts. Keel completed the independent reread and recorded the freeze. Bernstein coordinates the spec PR, Rigger prepares and opens it, and Keel records the user’s merge receipt for Gate 1. Build starts after that receipt and pickup clearance.
- **Phase B pickup**: Confirm DEV-546 landed (**required before any Build / production edits**, not only before tag-run gate work). Apply Q3 display-location rule once (DEV-560 status page if present, else `App.tsx` footer). Fresh **read-only** drift recon against pickup HEAD for authorized paths, including P2.6-1 generated envelope paths (`recon-DEV-547:83–90`), host-fixture seam, existing mapper signature, concrete parity comparison, and mutation applicability/exclusions with anticipated verdicts. Stop before affected Build work if a protected or outside-envelope change would be needed — cited Patron ruling first. Any authorized drift amendment is a **separately recorded follow-up** before production edits begin — pickup itself does not edit production files. Stop `needs recon:` if a required path/fact is missing.
- **Task order (`brief.md` Task ordering / Q4–Q5; Plan challenge decisions 1–7)**:
  1. Gate 1, then DEV-546 prerequisite + Q3 location selection + parity/mutation preflight at pickup (blocks all Build work).
  2. Sealed `VersionDto` + `GET /api/version` via `.MapVersionEndpoints()` (`TypedResults.Ok`) with once-per-host metadata capture/normalize + isolated nonparallel `WebApplicationFactory` cases (incl. P2.6-2 override cases) under `src/LamuFlix.Api` / `tests/LamuFlix.IntegrationTests`; ArchitectureTests + three static gates on changed `.cs`; Gauge isolation check runs the complete integration project.
  3. Separate contract steps (P2.6-1): T006 opt-in OpenAPI export/Verify pair → stop on unrelated drift → T007 `npm run generate:api` (types + `web/src/mocks/handlers.ts`) → `check:api` — before web client wiring.
  4. In parallel with (2)–(3) as staffing allows: CI/Dockerfile commit bake (build args + final ENV only); `promote-home` + k8s omit runtime `LAMUFLIX_COMMIT` and inject safely serialized `LAMUFLIX_RELEASE`; Compose commit via build args only / empty release; executable build-input / tagged / manual / P2.6-2 evidence; `CHANGELOG.md` + one-line README link in parallel with pre-gate merged-behaviour validation.
  5. At Phase 6 start recheck Q3 drift (keep pickup-selected location; report later DEV-560 landing through drift path). Web version line + generated-MSW state tests with deterministic controlled release for loading; Compose SMOKE-001/002 with request held open until UI observed, then controlled release or failure and bounded cleanup; record hold method, absence during hold, settled result; `"unknown"` verbatim / SHA shortened only in UI; uniqueness across both surfaces when present.
  6. Pre-merge matrix: full Q5 delivery evidence with **actual** gate verdicts (parity, mutation including possible FAILED, refactor threshold from `harness.yml`), C# gates, web gates, Size M three-axis review under closing bar (max two rounds; reviewed-head fix verification before closure).
  7. User merges delivery PR. Wait for an eligible `main` commit containing both tickets with green nightly evidence on that exact SHA; Conductor identifies the authorized release operator; pre-push checks of nightly SHA / local tag target / remote tag state; halt and escalate on wrong-SHA bind or post-push mismatch; safely serialized release for manifests/`RELEASE.json`; distinguish port-forward/readback harness blockers from app behaviour while still requiring live port-3100 receipt; Rigger posts evidence comment.
- **Do not**: tag GHCR `v0.1.0`; change the gate; add release tooling/auto-bumps; invent changelog prose beyond the frozen topic list before Phase B authorship from merged code; lower gate thresholds; touch LocalPlay/secrets/`Process.Start`; hand-edit generated envelope files; add Compose/promotion runtime `LAMUFLIX_COMMIT`.

## Verification Approach

- API integration (constitution `WebApplicationFactory` convention; peer `HealthEndpointTests` per `recon-DEV-547:14`; host-fixture seam + mapper signature via T001): fresh host per metadata case in a dedicated nonparallel collection; save/restore both prior env values in `finally`; HTTP 200 via `TypedResults.Ok`; configured full commit; explicit JSON `null` absent release; configured tag; empty/whitespace normalization; `"unknown"` fallback; P2.6-2 override cases (absent / nonblank / empty-whitespace operator runtime); Method_Condition_Expected + AAA; Gauge runs the complete integration project as isolation check; ArchitectureTests green (`brief.md` Q5; decisions 1, 6; constitution 840, 849–850).
- Contract (P2.6-1; `recon-DEV-547:83–90`): T006 opt-in export/Verify writes the two named baseline/snapshot files together; `DriftMatchesCommittedBaseline` with switch cleared; stop before T007 on unrelated drift vs pickup HEAD; T007 `npm run generate:api` writes exactly `web/src/api/types.ts` and `web/src/mocks/handlers.ts`; `npm run check:api` green. Never hand-edit the four authorized outputs.
- Web: tagged display; successful null-release short-SHA `dev`; successful verbatim `dev (unknown)`; deterministic MSW-controlled loading hold and separate controlled failure; one-location across shell and status page when both exist; Phase 6 Q3 drift recheck; SMOKE-001–002 on Compose with hold-method / absence / settled evidence — pass evidence is the Phase 6 delivery checkpoint; an explicit smoke blocker is recorded but is not a passed delivery checkpoint.
- Delivery path (executable evidence, not expectation-only): both CI jobs × both Dockerfiles bake full exact source SHA into final image ENV; Compose and promotion omit runtime `LAMUFLIX_COMMIT`; safely serialized release injection for tagged and manual/empty cases; digest pinning unchanged; P2.6-2 evidence in Phase 4; offline `Test-DeployParity.ps1` and hosted CI `deploy-parity` **actual** verdicts recorded (T001 anticipates; T010/T017 record actuals).
- Gates: Roslyn; complexity using the configured refactor threshold from `harness.yml` (do not hardcode); InspectCode; property tests per harness; vulnerable packages; format; `dotnet test` (incl. ArchitectureTests and full integration project isolation check); mutation with actual outcomes including possible FAILED; web lint/test/build; honest SKIPPED/N/A; comment rule on C# and delivery config edits.
- Review closure: accepted in-scope fixes independently verified on the reviewed head; brief Q6 follow-up/disposition rules applied before closure.
- Post-merge: Q4 + decision 4; eligible exact SHA; pre-push SHA/tag-state checks; halt/escalate on mismatch; harness vs app distinction at T021; evidence comment before closure.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-547/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file (also mirrored to private IMPL_PLAN)
├── tasks.md
├── contracts/
│   └── version-endpoint.md
├── DRAFTING_RECEIPT.md
└── checklists/
    └── requirements.md
```

No `research.md` / `data-model.md` / `quickstart.md` — grill decisions freeze Spec Kit research unknowns; no schema change.

### Source Code (authorized responsibility envelope — brief Approach; ticket Where it lands; recon)

```text
CHANGELOG.md                                          # new
README.md                                             # one-line link only

src/LamuFlix.Api/Endpoints/
├── ApiEndpoints.cs                                   # add .MapVersionEndpoints() to existing chain
├── VersionEndpoints.cs                               # GET /api/version; capture/normalize once per host (Quill-named)
└── VersionDto.cs                                     # sealed { Release, Commit } response DTO (Quill-named)

tests/LamuFlix.IntegrationTests/
├── VersionEndpointTests.cs                           # nonparallel collection; fresh host per case; finally restore
└── Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json  # generator-only (P2.6-1)

docker/
├── Dockerfile.api                                    # build-arg → final ENV LAMUFLIX_COMMIT only
└── Dockerfile.web                                    # build-arg → final ENV LAMUFLIX_COMMIT only

.github/workflows/
├── ci.yml                                            # container-build + publish-images build-args
└── promote-home.yml                                  # inject LAMUFLIX_RELEASE; omit runtime LAMUFLIX_COMMIT

deploy/k8s/base/                                      # release env for api/web; omit runtime LAMUFLIX_COMMIT
deploy/k8s/overlays/home/                             # render release beside digests; omit runtime LAMUFLIX_COMMIT
docker-compose.yml                                    # commit via build.args only; release empty; omit runtime LAMUFLIX_COMMIT

web/src/api/openapi.json                              # generator-only via opt-in OpenAPI export (P2.6-1; recon 83–90)
web/src/api/types.ts                                  # generator-only via npm run generate:api (P2.6-1)
web/src/mocks/handlers.ts                             # generator-only via npm run generate:api (P2.6-1; exact MSW output)
web/src/App.tsx                                       # footer only if Q3 selects it
web/src/                                 # DEV-560 status page only if landed at pickup (exact path = T001 pickup)
web/src/                                 # version-line Vitest/RTL tests colocated with the Q3-selected UI file (exact test path = T001 pickup; authorized conditional area below)
```

**Authorized generator-only envelope (P2.6-1; never hand-edit; no `web/src/test/*`)**: `web/src/api/openapi.json`; `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`; `web/src/api/types.ts`; `web/src/mocks/handlers.ts`.

**Authorized conditional web test area (T013)**: After T001 records the Q3 location, place or extend Vitest/RTL coverage in a colocated `*.test.ts(x)` beside that selected UI file under `web/src/` (footer → beside `App.tsx`; status page → beside the landed DEV-560 status module). Drive states through the **generated** MSW handlers with deterministic, explicitly controlled response release for the loading case. Cross-surface uniqueness may assert against the other candidate surface when it exists. Exact UI/test filenames are a **T001 pickup recon** item when not yet evidenced — do not invent a status-page path here. At Phase 6 start recheck Q3 drift; keep the pickup-selected location; report later DEV-560 landing through the drift path before any placement change.

**Structure Decision**: Extend the existing API endpoint folder and delivery manifests; no new project or top-level layer. Quill-named sealed `VersionDto` / `VersionEndpoints` (wired with `.MapVersionEndpoints()`) / `VersionEndpointTests` sit inside ticket-owned areas (`brief.md` Approach; recon Where it lands; brief decision 1). Exact DEV-560 status file path and colocated test path are confirmed at Phase B pickup if that location is selected. OpenAPI export/Verify and npm generate/check paths are Patron-evidenced (P2.6-1; `recon-DEV-547:83–90`). T001 still checks those evidenced paths and the host-fixture/mapper signature for pickup drift; do not authorize hand-maintained generated edits or Compose/promotion runtime `LAMUFLIX_COMMIT`.

## Phase 0 / Phase 1 Spec Kit outputs

- **research.md**: omitted — Q1–Q6 resolve inventable unknowns.
- **data-model.md**: omitted — no schema entities.
- **contracts/**: `version-endpoint.md` records the frozen HTTP contract.
- **quickstart.md**: omitted — verification lives in brief Q5 and tasks.
