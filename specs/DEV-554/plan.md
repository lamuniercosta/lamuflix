# Implementation Plan: SubDL provider and shared archive reader

**Branch**: `feature/554-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-554-spec`) | **Date**: 2026-11-28 | **Spec**: `specs/DEV-554/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q7 + Step 3.3 governance ruling at `508f8e48`); `CONCLUSIONS.md`; Maestri notes `DEV-554`, `recon-DEV-554`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft — Phase 2 step 6 Quill fix list applied; awaiting Keel reread. Gate 1 remains pending the owner checkbox. Quill does not declare freeze. Quill does not claim the VII amendment is already applied or approved.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `contracts/subdl-v2-adapter.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` — grill Q1–Q7 + Step 3.3 freeze inventable unknowns; no schema change; no new public API shape (adapter contract under `contracts/`).

## Summary

Add a movie-only SubDL v2 subtitle provider behind the DEV-551 `ISubtitleProvider` seam, with typed `Subtitles:SubDl` options bound by vendor-neutral Core types, secure `X-API-Key` transport, ZIP download through a shared BCL archive reader that returns SRT bytes into the existing handler validation path, attributed download-quota mapping into DEV-551 state, Step 3.3 multi-provider 409 / provider-state behaviour, sanitized WireMock contract coverage, setup documentation, and a scrubbed home-cluster live receipt on the delivery PR. Spec Kit PR carries the narrow VII amendment and unchecked owner checkbox as Gate 1 prerequisites.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Core`, `LamuFlix.Infrastructure`) per `specs/PRODUCT.md` §1. No web delivery files in frozen scope.

**Primary Dependencies**: Existing `ISubtitleProvider` port and OpenSubtitles adapter pattern (`recon-DEV-554:5,83–86`); BCL `System.IO.Compression.ZipArchive` (standing ruling 3 / Q3); WireMock.NET already in integration tests; existing FsCheck. No new NuGet/npm dependency (`brief.md` Frozen scope; `PRODUCT.md` §5 item 1).

**Storage**: No database or migration change.

**Testing**: Standalone ZIP reader + FsCheck under `tests/LamuFlix.UnitTests/ZipSubtitleArchiveReaderTests.cs`; xUnit integration tests under `tests/LamuFlix.IntegrationTests` (DEV-551 shared suite + WireMock + sentinel logs + health + explicit 409/provider-state cases + **separate** normal-composition handler/quota evidence); focused unit tests including `SubtitleSetupDocumentationTests` and download-handler path. CI uses sanitized WireMock fixtures (Q6).

**Target Platform**: Local API / Compose; home-cluster live receipt for delivery PR only.

**Project Type**: Ports-and-adapters .NET API; Infrastructure adapter + Core options; no new project or layer.

**Performance Goals**: Bounded archive reads under caller-supplied caps; no invented latency SLA.

**Constraints**: Charter §2.3 care-list items ruled in Q1–Q7 and Step 3.3. DEV-551 hard pickup prerequisite; DEV-549 preferred fixture placement. No LocalPlay / secrets hardcoding / `Process.Start`. Public API shape unchanged beyond Step 3.3 multi-provider 409 semantics on existing surfaces. Size M requires Risk (Sentry), Standards (Ledger), Spec (Compass). Closing bar: Critical/High with concrete scenario, or any demonstrated acceptance / credential / archive safety violation (`brief.md` Q7). Max two review rounds, two fix commits per round. VII amendment + owner checkbox are Gate 1 / T000 prerequisites — not presumed done.

**Scale/Scope**: Size M; one provider adapter, one shared reader, options/docs/tests/fixtures, home-cluster receipt. Downstream YIFY (DEV-556) reuses the reader outside this ticket.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). **Open governance (Step 3.3)**: Principle VII closed options-record list (constitution.md:782-807) does not enumerate SubDL / `Subtitles:SubDl`, and the OpenSubtitles exception (:788-801) does not authorize an independently optional SubDL provider. Principle VIII (:852-857) bars vendor-bearing Core options/validator names. Patron ruled: amend VII narrowly behind the unchecked owner checkbox in `brief.md:99`; meet VIII with vendor-neutral Core names (no VIII exception). Quill records the amendment **requirement** on the Spec Kit PR; Quill does **not** claim the amendment is already applied on `origin/main` or already approved.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests | **PASS** — reuse existing `ISubtitleProvider`; no new Core port; concrete adapters in Infrastructure; ArchitectureTests remain required green. |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — no new command/query; existing search/download handlers retain SRT seam (`recon-DEV-554:7,84`). |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no new public error taxonomy; adapter/reader failures stay within existing provider/handler paths; Step 3.3 multi-provider 409 semantics apply on existing search/download surfaces. |
| VI (Observability) | Safe diagnostics; health check per new external dependency; TelemetryConstants for telemetry names (constitution.md:760-770, :944-945) | **PASS with pickup obligations** — Q1/Q6 require credential redaction at URI/error/log/trace boundaries; fixed failure reasons and structured safe fields only (omit raw entry names, URLs, identifiers, quota values); sentinel tests required; misconfigured state redacts the key. SubDL is a new external dependency: **T001** identifies the **exact landed paths and lines** for health-check registration (recon-DEV-554:85 pattern — `SubtitleProviderServiceCollectionExtensions` registers `SubtitleProviderHealthCheck` as `"subtitle-provider"`; `SubtitleProviderHealthRegistrationTests` covers untagged registration and live/ready probes) and records compatible landed host-matching, URI-redaction, structured-logging (including `SubDlSubtitleProvider.Log.cs` layout if that convention is verified at pickup), and existing SRT seams for reuse. **T008** MUST register SubDL through that landed health seam as part of the minimum DEV-551 registration touch and MUST verify effective-disabled and keyed-invalid safety (host starts; zero outbound; probes stay reachable without authentication; no credential leak in health/data). Provider identity reaches DEV-551 state and telemetry as **runtime data** under existing vendor-neutral Core names (including Core test/support types); a proposed vendor-named Core constant (for example a `SubDlProviderName` in `TelemetryConstants`) requires a constitution ruling, with charter §2.3(b) governing any necessary departure. Any new span, metric, or attribute name MUST live in `TelemetryConstants` (ad-hoc telemetry string literals forbidden). Reuse existing subtitle TelemetryConstants when they already cover the acquisition state; **STOP for a cited Patron ruling recorded in `CONCLUSIONS.md` before editing any path outside the Q4 / Step 3.3 approved envelope** (including `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` if a new name is required). |
| VII (Configuration / LocalPlay / Process.Start) | Options; secrets; LocalPlay gated | **OPEN — owner checkbox / amendment required** — typed `Subtitles:SubDl` via vendor-neutral Core record/validator per Step 3.3; reuse the landed typed subtitle-options registration pattern (optional acquisition stays out of startup validation / no `ValidateOnStart`; typed section binding; typed validator applied before outbound use); effective-disabled (`Enabled=false` or blank key) without error/startup failure/outbound calls; keyed-invalid → startup ok, zero outbound, redacted misconfigured DEV-551 state; required positive archive caps (example/home 8 MiB / 32 MiB / 128 subject to owner approval); code supplies no guessed numeric defaults; no secrets in git; no LocalPlay/`Process.Start`. Spec PR amendment MUST (1) add the typed options entry to the closed list (:782-784), (2) **replace** the singular-provider 409 clause (:795-801) with Step 3.3 provider-set search and chosen-provider download rules, (3) update the VII checklist (:946-948), and (4) carry DEV-501 / ADR 0041 impact in the Sync Impact Report with the version bump + unchecked checkbox (`brief.md:99`). Preserve OpenSubtitles binding and validation, `SaveEnabled`, read-only precedence, existing-track serving, languages, and secret isolation while stating that search aggregation changes for blank OpenSubtitles with valid SubDL. Gate 1 closed until owner ticks and merges. T000 verifies evidence before Build. Owner rejection → revert amendment and §2.3(a) re-scope; no workaround. Any separate ADR edit needs a cited Q4 ruling. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS (ruling: no exception)** — Core options/validator use vendor-neutral names (`ArchiveSubtitleProviderOptions` / `ArchiveSubtitleProviderOptionsValidator`); vendor token only on Infrastructure adapter (`SubDlSubtitleProvider`) and `Subtitles:SubDl` config key (constitution.md:852-857; Step 3.3). Provider identity in state/telemetry is runtime data under those vendor-neutral rules, including Core test/support types; a proposed vendor-named Core constant requires a constitution ruling (§2.3(b) for any departure). Spec-local vocabulary only; no CONTEXT.md expansion claimed. |
| IX (Test Pyramid) | Meaningful layers; WebApplicationFactory / integration | **PASS** — Standalone ZIP reader + FsCheck properties in `tests/LamuFlix.UnitTests/ZipSubtitleArchiveReaderTests.cs` (repo naming + property-gate discovery); WireMock/health/composition/409 in IntegrationTests; DEV-551 shared suite; credential sentinel after Phase 3; home-cluster receipt separate from CI. |

### Stack / API / workflow checks

| Check | Plan response |
|---|---|
| Public API / OpenAPI chain | **PASS** — no new routes/DTO/status codes (Q4); Step 3.3 changes multi-provider 409 semantics on existing search/download only; no OpenAPI regeneration required for this ticket. |
| Static-analysis gates | Every changed `.cs` task clears Roslyn, cyclomatic complexity, and InspectCode; full size-M backend matrix per brief Test and acceptance evidence / Q6 on the delivery diff; **named ship checkpoint (T017)**: Gauge finishes the size-M matrix on the rebased delivery head with actual verdicts **before opening the delivery PR** (`task-pipeline` Phase 5 steps 1–2); live receipt (T016) attaches after T017 on that PR. |
| Web gates | Expect `SKIPPED (scope-empty)` when delivery diff has no web files. |
| Mutation | Run for production C# scope at harness threshold; record actual PASSED / FAILED / SKIPPED / N/A — never fold into undeserved PASS. On share/span failure, correct deficient tests while retaining configured thresholds (T014 / T017). |

### Complexity Tracking

No unjustified constitution violation. VII departure is Patron-ruled necessary and escalated via the owner checkbox; amendment is required on the Spec Kit PR and is **not** claimed applied. No new architectural layer. BCL ZipArchive chosen under standing ruling 3. Caps are required caller-supplied settings (Step 3.3 supersedes Q3 only on where values are set).

## Approach

- **Phase A** — Quill drafts Spec Kit artifacts from brief/CONCLUSIONS/recon facts. Keel analyzes and challenges; Challengers review plan; Spec Kit PR carries VII amendment draft (typed options entry; singular 409 clause replaced; checklist update; DEV-501 / ADR 0041 Sync Impact Report; version bump) + unchecked owner checkbox; owner ticks and merges Gate 1; Keel records Gate 1 merge receipt. Quill does not claim amendment applied before that merge.
- **Phase B pickup** (`brief.md` Task order step 1; Q4–Q5; Step 6 refinements): Confirm DEV-551 landed (registry, quota state, shared suite). **Inventory exact landed paths and lines** for 409, health, and registration; T001 lists which 409 files require edits and which are outside Q4's registration/composition allowance — **cited Patron approval in `CONCLUSIONS.md` is required before T008 edits any out-of-envelope 409 file**. **Identify the landed Principle VI health-check registration seam** (constitution.md:767-770, :944-945; recon-DEV-554:85). **Inventory HttpClient / redirect configuration** at pickup (search vs download clients; whether redirects auto-follow). **Record reuse candidates** for host matching, URI redaction, structured logging layout, and existing SRT seams (`recon-DEV-554:83–98`); verify helper names and logging layout at pickup. **Record an HTTPS WireMock configuration or isolated test-only transport seam** that preserves production HTTPS validation. **Record Q2 attribution and reset-representation pickup evidence** (whether download-response `X-RateLimit-*` trio is documented as download usage and how Reset is represented; absent/ambiguous → `Unknown` fallback locked). Trace provider ID as runtime data into DEV-551 state/telemetry under vendor-neutral Core names. Verify current v2 title-and-year mapping against docs + sanitized fixtures; a **verified documented** mapping is required to unlock the normal T003→T002 sequence; a Q5 stop leaves transport locked pending recon and a cited Patron ruling (not a green year-mapping pass). Prefer DEV-549 fixture location; else Conductor confirmation before writing fixtures. Refresh allowed file list against landed seams; **STOP for a cited Patron ruling recorded in `CONCLUSIONS.md`** before any path outside the Q4 / Step 3.3 envelope (including new `TelemetryConstants` names under `Core/Pipeline/` if required). Read-only recon first; no production edits until clearance.
- **Guarded transport** (Q1 + Step 6): Disable automatic redirect following. Keep search on its validated origin. For ZIP redirects, resolve and validate each target as HTTPS and download-allowlisted before requesting, with a finite hop limit and immediate rejection at the first invalid hop. Send the key only on an eligible initial request to the validated provider origin; every redirected or separately hosted ZIP fetch is credential-free. Build request paths from a validated single-segment `nId` (honor documented shape if pickup verifies one) and encode query values structurally. Reuse landed typed HttpClient patterns where compatible (`AllowAutoRedirect=false` or equivalent manual hop handling).
- **Byte bounds / diagnostics / ownership** (Q3 + Step 6): Bounded actual-byte read before ZIP open; length header may only early-reject; per-invocation limits/counters; cancellation through HTTP and compressed/expanded reads; dispose response/stream/ZIP per ownership on all exits; fixed failure reasons and structured safe fields; omit raw entry names, URLs, identifiers, and quota values from diagnostics.
- **Task order** (`brief.md` Task order):
  1. T000 Gate 1 / VII amendment evidence (owner tick, amended VII+version on `origin/main`, DEV-551 seam/state, approved caps; Sync Impact Report covers DEV-501 / ADR 0041) — blocks Build; rejection path reverts amendment and returns for §2.3(a) re-scope.
  2. Pickup / exact 409+health+registration path inventory / client-redirect inventory / Q2 attribution+reset evidence / verified year-mapping / test-only HTTPS transport / fixture-location clearance (blocks production edits; Q5 stop locks transport; missing attribution locks quota mapping to `Unknown`).
  3. Standalone archive reader call shape (T003) then unit tests + FsCheck (T002) then T004 verification (archive provable independently; caller-passed caps; no reader defaults; no mutable static state) + establish sanitized fixture location.
  4. Bind/validate vendor-neutral Core options for `Subtitles:SubDl` (optional provider out of startup validation); implement guarded search, ZIP download, attribution-gated quota mapping with `TimeProvider`; wire into DEV-551 with Principle VI health-check registration and **implement** Step 3.3 409 semantics (T008; Patron-approved out-of-envelope 409 paths only); then **separate** composition evidence (valid/invalid SRT through handler; quota → DEV-551 or `Unknown`) plus shared contract suite (T009); Phase 3 checkpoint before credential sentinel.
  5. Setup docs + sample config (required caps at approved example values) + documentation test (T011 only after Phase 3 checkpoint; sentinel + no credentials on all followed hops); size-M backend gates and review on the delivery diff; **T017 Gauge ship checkpoint on rebased head before opening the delivery PR** (correct tests on mutation share/span failure); **then T016** home-cluster receipt on that delivery PR (`task-pipeline` Phase 5 steps 1–2; `brief.md:33,40`).
- **Do not**: TV; unpack/file modes; legacy API; `/me` calls; new package/layer/schema/public API; LocalPlay/`Process.Start`; commit keys; invent year mapping when docs lack it; invent Q2 attribution/reset representation; treat Q5 stop as T003+ pass; conflate 409 implementation with composition evidence; start T011 before Phase 3 checkpoint; open delivery PR before T017 finishes; skip SubDL health-check registration or invent telemetry name literals / vendor-named Core constants outside `TelemetryConstants` without a constitution ruling; edit out-of-envelope paths without a cited Patron ruling in `CONCLUSIONS.md`; auto-follow redirects; bake reader-side numeric defaults or mutable static cap state; parallel fake framework; claim VII amendment already approved.

## Verification Approach

- **Provider contract**: DEV-551 suite × sanitized WireMock — IMDb, TMDb+movie type, title+year + same-title/different-year, ZIP download with guarded multi-hop / disallowed-hop / hop-exhaustion / invalid-`nId` / title-metacharacter cases, coherent attributed and unknown download quota (Unknown when T001 attribution/reset evidence is absent/ambiguous or headers incomplete/malformed/negative/overflowing/incoherent; no throw), search counters separate, zero `/me`, enablement/blank-key, keyed-invalid/misconfigured state, production non-HTTPS rejection with zero outbound, Step 3.3 search/download 409 cases.
- **Explicit 409 + health registration** (T008; after T001 exact-path inventory + Patron approval for any out-of-envelope 409 file): implement and prove Step 3.3 search/download 409 + effective-disabled/keyed-invalid membership in the effective-valid set; register SubDL into the T001-cited Principle VI health seam and verify disabled/keyed-invalid safety on that registration — separate from composition.
- **Separate composition evidence** (T009; after T008): selected valid SRT reaches `DownloadSubtitleCommandHandler` publication; invalid selected SRT rejected through that same handler path; derived download quota reaches DEV-551 per-provider state only when T001 attribution evidence allows mapping, else `Unknown`.
- **Archive safety** (unit project): compressed/expanded/entry caps via actual-byte bound before ZIP open; over-cap data without reliable length; zip-bomb; too-many-entries; `../x.srt` and peer unsafe forms; no SRT; malformed ZIP; multi-entry ranking; invalid selected SRT via existing checks; distinct fixed safe reasons; independent concurrent calls; cancellation/disposal on rejection; no untrusted marker text in reasons/logs/traces/errors; FsCheck for order-independent choice and limit boundaries. Provable without SubDL HTTP.
- **Credential safety**: sentinel across all log levels, traces, errors; upstream failure; redirected download; no credential on **any followed hop** — **T011 only after Phase 3 checkpoint**.
- **Gates**: task-pipeline Phase 3 step 5 on the delivery diff; **Phase 5 steps 1–2 ship checkpoint (T017 before opening the delivery PR)** — rebase then Gauge re-runs size-M matrix on rebased head with actual verdicts (Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable packages, format, `dotnet test`; mutation actual verdict — correct deficient tests on share/span failure, retain configured thresholds; web `SKIPPED (scope-empty)` if applicable); Risk/Standards/Spec reports; **then T016** live home-cluster receipt on the delivery PR.
- **Runtime**: scrubbed home-cluster search + one download on delivery PR after T017 (movie, languages, candidate count, success).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-554/
├── brief.md                 # Keel (grill close + Step 3.3 ruling)
├── CONCLUSIONS.md           # Patron/Keel
├── spec.md                  # This ticket’s public spec
├── plan.md                  # This file (mirrored to private IMPL_PLAN)
├── tasks.md                 # /speckit-tasks (mirrored to private TASKS)
├── checklists/requirements.md
├── contracts/subdl-v2-adapter.md
└── DRAFTING_RECEIPT.md
```

Private mirrors (byte-identical when present):
`C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-554\plan.md` and `...\tasks.md`.

### Source Code (repository root)

```text
src/LamuFlix.Infrastructure/Adapters/
├── SubDlSubtitleProvider.cs          # new — v2 search/ZIP download adapter (vendor name OK here)
├── SubDlSubtitleProvider.Log.cs      # new — structured LoggerMessage partial if pickup verifies that convention
├── ZipSubtitleArchiveReader.cs       # new — shared BCL ZipArchive reader (caller-passed caps only; per-invocation state)
└── (DEV-551 registration/composition/health touch — exact paths/lines at pickup)

src/LamuFlix.Core/Options/
├── ArchiveSubtitleProviderOptions.cs           # new — vendor-neutral; section Subtitles:SubDl
└── ArchiveSubtitleProviderOptionsValidator.cs  # new — enablement/key/HTTPS/allowlists/required caps; no ValidateOnStart

tests/LamuFlix.UnitTests/
├── ZipSubtitleArchiveReaderTests.cs            # standalone archive + FsCheck (Method_Condition_Expected; property-gate discovery)
└── SubtitleSetupDocumentationTests.cs          # extend for SubDl settings + required caps

tests/LamuFlix.IntegrationTests/
├── (DEV-551 shared suite invocation for SubDL)
├── SubDlSubtitleProviderTests.cs     # WireMock / sentinel / quota / 409 (T008) / separate composition (T009)
└── (composition / health-adjacent tests only if DEV-551 registration requires)

README.md
docker/api.settings.example.json      # required caps at 8 MiB / 32 MiB / 128 subject to owner approval
```

Exact DEV-551 registration/409/health filenames and DEV-549 fixture directory are **pickup facts** (`recon-DEV-554:111` — absent at grill HEAD). T001 cites exact landed paths/lines and lists which 409 files need edits vs which sit outside Q4. Reuse landed host-matching, URI-redaction, logging, SRT, typed-options, and HttpClient patterns where pickup verifies compatibility (`recon-DEV-554:83–98`). Quill-named new files above are within ticket-owned folders; rename only if pickup seams require cited alignment. Core type names above are the Step 3.3 / VIII pick.

**Structure Decision**: Extend existing Infrastructure adapters + Core options; reuse `ISubtitleProvider` and download handler SRT seam; no new project/layer.

## Phase 0 / Phase 1 design outputs

- **research.md**: omitted — Q1–Q7 + Step 3.3 resolve inventable unknowns.
- **data-model.md**: omitted — no schema entities.
- **contracts/subdl-v2-adapter.md**: written — external SubDL transport + archive reader + fixture placement + attribution-gated quota mapping.
- **quickstart.md**: omitted — verification lives in brief Test and acceptance evidence and tasks.
