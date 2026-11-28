# Implementation Plan: Subtitles BSPlayer provider

**Branch**: `feature/555-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-555-spec`) | **Date**: 2026-11-28 | **Spec**: `specs/DEV-555/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q5 and plan-challenge D1–D15); `CONCLUSIONS.md`; `ASSUMPTIONS.md` (Q3); Maestri notes `DEV-555`, `recon-DEV-555`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite principles only).

**Status**: Draft — Quill drafting only. Quill does not declare PLAN FROZEN or Gate 1.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `contracts/bsplayer-soap-adapter.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` — grill Q1–Q5 freeze inventable unknowns; no schema change; no new public API shape (adapter contract under `contracts/`).

## Summary

Deliver a movies-only, keyless BSPlayer subtitle provider behind the DEV-551 `ISubtitleProvider` seam: named-instance `SubtitleProviderOptions` bound from `Subtitles:BsPlayer` with BSPlayer-only `AllowPlainHttp: true`, D1 one-deadline configured-order server selection with recorded-evidence session/hash/wait proofs, scrubbed SOAP contract-suite coverage under the DEV-549 fake layout, config-state-only health, setup documentation stating plain HTTP, and a home-cluster live search/download receipt (or explicit down-service note that keeps DEV-555 open).

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Core`, `LamuFlix.Infrastructure`) per `specs/PRODUCT.md` §1. No web delivery files in frozen scope (`ui:none`).

**Primary Dependencies**: Existing `ISubtitleProvider` / DEV-551 provider contract and shared suite; existing typed HttpClient / host-refusal / SRT validation patterns (`recon-DEV-555:12–20`); WireMock.NET already in IntegrationTests (wire shape only — D8); in-box `Microsoft.Extensions.TimeProvider.Testing` FakeTimeProvider if already referenced by test projects (confirm at pickup; any new test package needs a Patron ruling — Q3/Q4). Timing tests run the real adapter over a stub `HttpMessageHandler`; the test owns `FakeTimeProvider.Advance`; no wall-clock assertions (D8). Deadline is `new CancellationTokenSource(timeout, timeProvider)`; waits are `Task.Delay(delay, timeProvider, token)`; `HttpClient.Timeout` stays infinite (recon line 14). No new NuGet/npm dependency.

**Storage**: No database or migration change. Scrubbed recorded SOAP fixtures under DEV-549 layout (`tests/fakes/` preferred). Known file for hash proof is generated in the test only; no media or binary committed (D10).

**Testing**: IntegrationTests shared DEV-551 contract suite + BSPlayer recorded SOAP fake (WireMock for wire shape); stub `HttpMessageHandler` + FakeTimeProvider for D1/D2/D4/all-down timing; focused unit/options/docs/health tests with Theory + MemberData for D15; `SubtitleSetupDocumentationTests`. CI uses recorded fakes. Home-cluster live receipt is separate from CI.

**Target Platform**: Local API / Compose; home-cluster live receipt for delivery PR only.

**Project Type**: Ports-and-adapters .NET API; Infrastructure adapter + Core options; no new project or layer.

**Performance Goals**: Provider latency bounded by configured `Subtitles:BsPlayer` `RequestTimeout`; no invented SLA beyond that bound.

**Constraints**: Charter §2.3 care-list items deliberately ruled with no triggered item (Q4). No LocalPlay / secrets hardcoding / `Process.Start`. Public API shape unchanged. Size M requires Risk (Sentry), Standards (Ledger), Spec (Compass). Closing bar: Critical/High/Medium and accepted in-scope Low block until fixed and re-verified; two review rounds (Q5). DEV-551 is a hard dependency (cite contract, not grill-HEAD landed code — Q1). No `static readonly` / no mutable static state in new or changed production code (D14).

**Scale/Scope**: Size M; one provider adapter, options/validation, recorded SOAP proofs, docs/health, live receipt. Parent DEV-490.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. After T003 evidence, Keel reruns this check against the evidence note before T004 (wait, host shape, or hash may change the plan — Ledger L5 / D Recheck).*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox (`PRODUCT.md` §5): Patron Q1–Q5; no ticket contradiction; no constitution departure (Q1, Q4). Ordinary owner Gate 1 remains the Spec Kit PR merge path. Cite constitution **principles**, not line numbers.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests | **PASS** — reuse existing `ISubtitleProvider` / DEV-551 seam; no new Core port or ADR (Principle I; ADR 0038; Q4). ArchitectureTests remain required green. |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — no new command/query; existing search/download handlers remain the acquisition path (`recon-DEV-555:19`). |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no new public error taxonomy or status codes; provider-local failures are named DEV-551 outcomes (Q2). |
| VI (Observability) | Safe diagnostics; health for new external dependency | **PASS** — config-state-only BSPlayer health registration with no live probe (match existing `SubtitleProviderHealthCheck` pattern, `recon-DEV-555:17`); scrub session tokens from logs/telemetry/fixtures (Q2, Q4). Reuse existing and DEV-551 spans, metrics and attribute keys with no new attribute keys; server host and attempt number in logs only; logs carry movie id, attempt number and failure category (D9). Health registration name and telemetry names are functional per D12 (DEV-551 per-provider mechanism). If the DEV-551 provider identifier requires a new vendor-named `TelemetryConstants` entry, that is a Principle VIII departure: STOP and route it as `blocked: structural` (PRODUCT §5 b). Do not add the entry. STOP for cited Patron ruling before out-of-envelope paths. |
| VII (Configuration / LocalPlay / Process.Start) | Options; secrets; LocalPlay gated | **PASS** — `Subtitles:BsPlayer` is subtitle acquisition settings under the existing Principle VII exception (Q1). No new options record: bind a named instance of the existing `SubtitleProviderOptions` from `Subtitles:BsPlayer` at the existing subtitle-provider registration site cited at T001, without `ValidateOnStart` (D11). Reuse `RequestTimeout`, `DownloadAllowedHosts`, `MaxDownloadBytes`, DEV-551 `Enabled` and `AllowPlainHttp`; add only `Servers` and `MaxResponseBytes`. OpenSubtitles validation stays byte-identical. Download cap follows OpenSubtitles precedence against global `Subtitles:MaxSubtitleDownloadBytes`. If DEV-551's landed shape makes a named instance impossible, STOP for a ruling. Provider-local validation per D15. `AllowPlainHttp: true` only under `Subtitles:BsPlayer`; OpenSubtitles stays HTTPS-only. No guessed numeric defaults. Home settings are untracked; T004 edits no home file (D13). No LocalPlay / `Process.Start`. Core type naming follows VIII (functionality, not vendor); vendor token on Infrastructure adapter and ticket-named config key. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — spec-local vocabulary only; no CONTEXT.md expansion claimed. Identifiers per D12: adapter `BsPlayerSubtitleProvider`; section key `"Subtitles:BsPlayer"` as const on that adapter (doubles as options name); options record existing `SubtitleProviderOptions`; validator extends `SubtitleProviderOptionsValidator`; typed client `AddHttpClient<BsPlayerSubtitleProvider>`; health/telemetry names functional via DEV-551 mechanism; outcome text “BSPlayer is not answering”; tests `BsPlayerSubtitleProvider*Tests`. If a vendor-named `TelemetryConstants` entry is required, STOP and route as `blocked: structural` (PRODUCT §5 b) — do not add it. **STOP**: if implementation introduces a new domain term in code, VIII requires a `CONTEXT.md` entry in the same PR, which is outside the Q4 envelope and needs a Patron ruling first. |
| IX (Test Pyramid) | Meaningful layers; WireMock; WebApplicationFactory | **PASS** — recorded SOAP fake + DEV-551 shared suite in IntegrationTests (WireMock for wire shape); stub `HttpMessageHandler` + FakeTimeProvider for timing proofs (D8); Theory + MemberData for D15 validator rules; docs/health/options unit coverage; live receipt separate from CI. |

### Stack / API / workflow checks

| Check | Plan response |
|---|---|
| Public API / OpenAPI chain | **PASS** — no new routes/DTO/status codes (Q4); no OpenAPI regeneration required for this ticket; check drift via existing CI gate only. |
| Static-analysis gates | Every changed `.cs` task clears Roslyn, cyclomatic complexity, and InspectCode; full Size M backend matrix per brief Task order step 3 / Q5. Keep `SKIPPED`, `N/A`, and `Could not run` distinct from PASS. |
| Web gates | Expect `SKIPPED (scope-empty)` when delivery diff has no web files. |
| Mutation | Run for production C# scope at harness threshold; record actual PASSED / FAILED / SKIPPED / N/A. On share/span failure, correct deficient tests while retaining configured thresholds. No `static readonly` / mutable static in new or changed production code (D14). |

### Complexity Tracking

No constitution violations requiring justification. No new project/package/layer/port. Plain-HTTP exception is ticket-mandated and Patron-bounded (Q1–Q2). D11 named-instance binding stays inside the Q1(a) closed list.

## Approach

- **Spec Kit drafting → Gate 1**: Spec Kit artifacts drafted; Keel analyze / plan challenge follow the pipeline; Spec PR opened under ordinary Gate 1; Build starts after Gate 1 merge receipt and pickup clearance. Quill does not claim freeze or Gate 1.
- **Pickup** (`brief.md` Task order step 1; Q2/Q4): Fresh read-only recon against pickup HEAD for authorized paths — DEV-551 seam/shared suite/`AllowPlainHttp` contract; DEV-551 per-provider options shape, binding site, and OpenSubtitles download-cap precedence (D11); DEV-551 cancellation semantics (D4); DEV-549 fake layout; existing OpenSubtitles adapter/options/health/registration patterns (`recon-DEV-555:12–20`); confirm FakeTimeProvider already referenced by test projects; cite existing OpenSubtitles golden-value hash test or note absence (D6). Record and scrub a BSPlayer exchange **before** implementation relies on session sequencing, required delay, hash algorithm, or download-host shape. Treat all four as evidence to establish; apply D7 second-capture branch when the first capture is inconclusive. Use HolyArrs notes for orientation only; copy no AGPL source. If a recording requires a new destination or redirect, STOP for explicit ruling. Refresh allowed file list; STOP for cited Patron ruling before any outside-envelope or §2.3 protected edit. STOP `needs recon:` if a required path/fact is missing.
- **Task order** (`brief.md` Task order; D Recheck):
  1. Establish DEV-551 + DEV-549 prerequisites; capture/scrub recorded SOAP evidence for session/wait/hash/download-host (D7 second-capture branch as needed).
  2. **Keel Constitution Check recheck** against the T003 evidence note (wait, host shape, or hash may change the plan) before options/adapter work.
  3. Implement named-instance `SubtitleProviderOptions` binding + D15 validation (`Subtitles:BsPlayer`, default off; T004 edits no home file), bounded SOAP adapter (D1–D6, D14), registration/health wiring; exercise DEV-551 contract suite on the recorded fake; assert known-file hash (test-generated only); stub-handler + FakeTimeProvider failover/all-down/cleanup/cancellation (D8); D3/D5 refusal and SRT bounds; preserve OpenSubtitles HTTPS-only and existing read paths.
  4. Document plain HTTP + settings (full example block on tracked setup-doc surface); run `SubtitleSetupDocumentationTests`; run applicable Size M gates with actual statuses; OpenAPI drift via existing CI gate only.
  5. Record home-cluster live search+download on the PR (T017 lists operator keys for untracked home settings) or down-service note keeping ticket open.
- **Do not**: TV; HTTPS unless service offers it; DNS-rebinding pinning; new package/project/layer/port/schema/public API; LocalPlay/`Process.Start`; commit secrets/session tokens/media; copy AGPL; invent wire behaviour without recordings; edit `MovieHash.cs` without hash-difference evidence (and then only via a separately named routine); edit home settings in T004; lower gate thresholds; claim Gate 1 / plan freeze / delivery PASS from this draft.

### Patron-approved identifiers (from brief / CONCLUSIONS / D12; not draft taste)

| Item | Value | Basis |
|---|---|---|
| Adapter class | `BsPlayerSubtitleProvider` | D12; VIII adapter-class rule |
| Config section / options name | `"Subtitles:BsPlayer"` const on the adapter class (doubles as options name) | Wanted 1; Q1; D12 vendor-mandated-key rule |
| Options record | Existing `SubtitleProviderOptions` (named instance; no new record) | D11; D12 |
| Validator | Extend existing `SubtitleProviderOptionsValidator` (functional name) | D12; D15 |
| Typed client | `AddHttpClient<BsPlayerSubtitleProvider>`, keyed by adapter type | D12 |
| Enablement | `Enabled` (default off in appsettings; operator sets in untracked home) | Wanted 1; Open point; Q1/Q5; D13 |
| Plain HTTP opt-in | `AllowPlainHttp: true` (BSPlayer only) | Wanted 1; Q1–Q2 |
| Added fields | `Servers` (`string[]` absolute URLs), `MaxResponseBytes` (`long`) | D11 |
| Reused fields | `RequestTimeout`, `DownloadAllowedHosts`, `MaxDownloadBytes`, `Enabled`, `AllowPlainHttp` | D11 |
| Ignored on BSPlayer | `ApiKey`, `BaseUrl`, `UserAgent`, `DownloadLinkHost` | D11 |
| Allowed hosts | Union of `Servers` and `DownloadAllowedHosts`; empty `DownloadAllowedHosts` valid | D3; D15 |
| Download cap | OpenSubtitles precedence vs global `Subtitles:MaxSubtitleDownloadBytes` (cited at T001) | D11 |
| All-down reason | `failed` + “BSPlayer is not answering” | Wanted 4; Q3; D12 |
| Server try order | Configured list order | Q3; `ASSUMPTIONS.md` `[assumed]` |
| Health / telemetry names | Functional, via DEV-551 per-provider mechanism; no new attribute keys | D9; D12 |
| Vendor `TelemetryConstants` | If required by DEV-551 mechanism → `blocked: structural` (PRODUCT §5 b); do not add | D12; Principles VI/VIII |
| Tests / fixtures | `BsPlayerSubtitleProvider*Tests`; DEV-549 fixture folder | D12 |
| OpenSubtitles section | `Subtitles:OpenSubtitles` unchanged, HTTPS-only; validation byte-identical | Q1; D11 |

## Verification Approach

- **Recorded evidence first**: Scrubbed SOAP exchange establishes session open/close, wait necessity, known-file hash match, download-host shape before reliance (Q2; FR-005); D7 second-capture branch when first capture is inconclusive.
- **Contract suite**: DEV-551 shared suite × BSPlayer recorded fake under DEV-549 layout (SC-001). WireMock covers wire shape only (D8).
- **Hash proof**: Outgoing hash for a test-generated known file matches recording, or D7 hash-acceptance proof (SC-002; D7; D10).
- **Deadline / failover**: Timing tests run the real adapter over a stub `HttpMessageHandler`; the test owns `FakeTimeProvider.Advance`; no wall-clock assertions (D8). Cover D1 ≥3-server hanging/refusing/answering path; zero-budget close success; concurrent two-search token isolation (D2); all-down within timeout with sibling results; caller cancellation during probe → cancellation not `failed` (D4) (SC-003; FR-004).
- **Config honesty / safety**: Disabled/invalid → zero calls while host and existing-track / languages routes remain available (D15 Theory + MemberData); D3 destination-table refusals; D5 over-bound abort and internal-entity refusal; SRT validation; session scrub and log scrub (movie id present, token absent) (SC-004; D9).
- **Docs / health**: `SubtitleSetupDocumentationTests`; full example block on tracked setup-doc surface when confirmed; config-state-only health with no live probe (SC-005; D13).
- **Gates / closing bar** (`brief.md` Q5): Exact-head Roslyn; cyclomatic complexity (incl. refactor threshold); InspectCode; property tests; vulnerable packages; format; full `dotnet test` (incl. ArchitectureTests); mutation when applicable; web `SKIPPED (scope-empty)` if no web files; OpenAPI drift via existing CI gate. Risk/Sentry, Standards/Ledger, Spec/Compass with explicit dispositions; two rounds; Critical/High/Medium and accepted in-scope Low block. Out-of-scope Critical/High or broken behaviour → YouTrack follow-up via Rigger; other out-of-scope noted. Live receipt or down-service note on PR (SC-006).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-555/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file (mirrored to private IMPL_PLAN)
├── tasks.md             # Mirrored to private TASKS
├── contracts/
│   └── bsplayer-soap-adapter.md
├── DRAFTING_RECEIPT.md
└── checklists/
    └── requirements.md
```

Private mirrors (byte-identical when present):
`C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-555\plan.md` and `...\tasks.md`.

### Source Code (authorized responsibility envelope — Q4 / Where it lands)

```text
src/LamuFlix.Infrastructure/Adapters/
├── BsPlayerSubtitleProvider.cs          # new — SOAP adapter (vendor name OK here; section-key const)
└── (minimum registration/health touch on existing subtitle provider extensions)

src/LamuFlix.Core/Options/
├── SubtitleProviderOptions              # existing — named instance bound from Subtitles:BsPlayer (+ Servers, MaxResponseBytes)
└── SubtitleProviderOptionsValidator     # existing — extend per D15; OpenSubtitles path byte-identical

src/LamuFlix.Core/Features/Subtitles/MovieHash.cs   # ONLY if T003 proves hash differs — separately named routine (D6)

tests/LamuFlix.IntegrationTests/
├── (DEV-551 shared suite invocation for BSPlayer)
└── BsPlayerSubtitleProviderTests.cs / recorded SOAP fake wiring / stub HttpMessageHandler timing

tests/fakes/bsplayer/   # preferred DEV-549 layout — T001-cited pickup fact (not verified in recon-DEV-555)

tests/LamuFlix.UnitTests/
└── SubtitleSetupDocumentationTests.cs   # T001-cited pickup fact (not verified in recon-DEV-555); extend for BsPlayer + plain HTTP

README / docker/api.settings.example.json   # T001-cited pickup facts for setup-doc surfaces; T011 adds full Subtitles:BsPlayer example block when confirmed (D13)
appsettings / registration / subtitle setup docs   # necessary defaults and docs only; T004 edits no home file; T017 lists operator keys for untracked home settings (D13)
```

Exact DEV-551 registration/health/options-binding filenames, DEV-549 fixture directory, `SubtitleSetupDocumentationTests` / README / `docker/api.settings.example.json` doc surfaces are **T001-cited pickup facts** (the docs/fake/README/`docker/api.settings.example.json` paths are not verified in `recon-DEV-555`). Quill-named adapter file above is within ticket-owned `Adapters/`; rename only if pickup seams require cited alignment. Identifiers follow D12. Health-check names / telemetry identifiers stay functional (reuse DEV-551 provider-identifier mechanism); vendor-named `TelemetryConstants` entry → `blocked: structural`.

**Structure Decision**: Extend existing Infrastructure adapters + Core subtitle options (named `SubtitleProviderOptions` instance); reuse `ISubtitleProvider` / DEV-551 seam; no new project/layer/port.

## Phase 0 / Phase 1 design outputs

- **research.md**: omitted — Q1–Q5 resolve inventable unknowns; wire details are recorded-evidence prerequisites.
- **data-model.md**: omitted — no schema entities.
- **contracts/bsplayer-soap-adapter.md**: written — external SOAP/plain-HTTP obligations + evidence prerequisites + fixture placement + D1–D15 obligations.
- **quickstart.md**: omitted — verification lives in brief Task order and tasks.
