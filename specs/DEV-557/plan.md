# Implementation Plan: OMDb and TMDb metadata-provider contract suite

**Branch**: `feature/557-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-557-spec`) | **Date**: 2026-11-28 | **Spec**: `specs/DEV-557/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q7); `CONCLUSIONS.md`; Maestri notes `DEV-557`, `recon-DEV-557`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft — Quill drafting only. Gate 1, Phase B pickup, Build, and runtime receipts remain pending. Quill does not declare PLAN FROZEN. Phase B is blocked until DEV-551 implementation merges to main (Q1).

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` — grill Q1–Q7 freeze inventable unknowns; no schema or public API change. No `ASSUMPTIONS.md` — brief records no taste assumptions. Conductor ask skipped clarify and checklist commands (no `checklists/`).

## Summary

Add one abstract xUnit metadata-provider contract suite that OMDb and TMDb each run through shipped DI against WireMock, consuming the DEV-551 shared harness (no rival). Remove twin lookup/resilience/telemetry cases into the suite while keeping single-adapter and residual assertions. Prove ordinary 401 as the recon-recorded OMDb key-problem outcome; record paired Stryker and Q5 line-count receipts; clear Size M gates under the closing bar.

## Technical Context

**Language/Version**: C# / .NET 10 (`TmdbMetadataProvider.cs` only under Q3 carve-out; `OmdbMetadataProvider.cs` read-only Q3 reference; `tests/LamuFlix.IntegrationTests/`) per `specs/PRODUCT.md` §1. UI none.

**Primary Dependencies**: Existing WireMock.Net and xUnit v3 in IntegrationTests; existing typed `HttpClient` / resilience / quota composition via `MetadataProviderServiceCollectionExtensions`. Consume DEV-551 harness after it merges — no new NuGet/npm (`brief.md` Q6; `PRODUCT.md` §5 item 1).

**Storage**: No database or migration change.

**Testing**: Integration tests only — WireMock with real DI pipeline; injected time where the harness provides it (`brief.md` Test strategy). Twin inventory before any removal. Preserve single-adapter suites listed in the brief. Match IntegrationTests assertion conventions (Shouldly). Never mock the data-access driver for query-translation behaviour (N/A for HTTP provider path).

**Target Platform**: Local IntegrationTests host; no Compose/UI change.

**Project Type**: Existing ports-and-adapters .NET solution; work lands in IntegrationTests plus optional Q1 harness extraction and optional Q3 TMDb-only adapter alignment (`OmdbMetadataProvider.cs` is read-only Q3 reference; expected Q3 outcome is "none, no production edit" per recon OMDb `:452-455` and TMDb `:537-540`).

**Performance Goals**: No invented SLA; contract cases exercise existing timeout/retry/breaker budgets.

**Constraints**: Charter §2.3 care-list items ruled none triggered (Q6). No public API change. No LocalPlay/secrets/`Process.Start`. Phase B blocked on DEV-551 merge (Q1). Spec/plan must not freeze harness type names before main has them. Honest gate labels; never lower thresholds; never merge. Size M requires Risk (Sentry), Standards (Ledger), Spec (Compass). Closing bar: unresolved Critical/High or Done-when/gate failure blocks; two review/remediation rounds (`brief.md` Closing bar).

**Scale/Scope**: Size M; shared contract suite + twin removal + Stryker/line-count receipts. 12 recon files baseline 5,072 lines at `e1649d53` (`brief.md` Q5).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox (`PRODUCT.md` §5): Patron Q1–Q7; no ticket contradiction; no constitution departure.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests | **PASS** — no new port/layer; suite targets existing `IMetadataProvider` adapters through shipped DI (`brief.md` Q6). |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS (N/A use-case path)** — test-only consolidation; no new application command/query. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no public API error-shape change; adapter failure categories unchanged except optional Q3 TMDb-only alignment (`OmdbMetadataProvider.cs` read-only; expected outcome "none"). |
| VI (Observability) | Safe diagnostics | **PASS** — contract requires one span and one duration per call with no key/title/IMDb id tags (*Wanted* 1 telemetry). |
| VII (Configuration / LocalPlay / Process.Start) | Options; secrets isolation; LocalPlay gated | **PASS** — fake WireMock keys only (Q6); no LocalPlay/secrets hardcoding/`Process.Start`; production options untouched except optional Q3 carve-out on `TmdbMetadataProvider.cs` only (`OmdbMetadataProvider.cs` is the Q3 reference and stays read-only; expected outcome "none"). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — spec-local vocabulary only; no CONTEXT.md expansion claimed. |
| IX (Test Pyramid) | Meaningful layers; WireMock; property tests | **PASS** — IntegrationTests WireMock through real typed clients; integration-only per brief; no new UnitTests layer required by brief. |

### Stack / API / workflow checks

| Check | Plan response |
|---|---|
| Tech stack / API docs | **PASS** — no public route/DTO/OpenAPI/TypeScript change (`PRODUCT.md` §5 item 4). |
| Static-analysis gates | Every changed `.cs` task completes only after Roslyn, cyclomatic complexity, and InspectCode pass on those files; full delivery matrix per `brief.md` Gate expectations. Keep `SKIPPED`, `N/A`, and `Could not run` distinct from PASS. Q7: fix InspectCode `:98,:102` if `SecondaryMetadataProviderTelemetryTests.cs` is in the diff. |
| Web gates | Scope-empty on the approved file envelope — record as `SKIPPED (scope-empty)`, never PASS. |
| Mutation | **Q5 receipts** (not a gate PASS): direct Stryker runs with explicit `--mutate` on `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs` and `TmdbMetadataProvider.cs`, identical config including recorded `additional-timeout`, before on Phase B base and after on head; no per-file drop (Q5). Separately, `scripts/run-mutation.ps1` verdict is reported as the script prints it (PASSED / FAILED / SKIPPED / N/A) with kill-share and file-span lines. Honour static-initialization discipline on any touched production C#. Note risk: a consolidated suite may have a few tests killing most mutants across both adapters. |

### Complexity Tracking

No constitution violations requiring justification. No new architectural layer or dependency.

## Approach

- **Phase A — Spec Kit drafting**: Quill drafts `spec.md` / `plan.md` / `tasks.md` from the closed brief. Keel analyzes / plan-challenges. Ordinary Gate 1 Spec Kit PR follows the pipeline. Quill does not claim freeze or Gate 1.
- **Phase B pickup (blocked on DEV-551 implementation merge to main)**: Fresh Build worktree after DEV-551 lands. Re-run `/speckit-analyze` against main; re-verify harness type names on main. **Keel** makes the harness-name repin as one commit on the Phase B Build branch, message `DEV-557 - Repin harness names to DEV-551 main`, changing only names and paths (no scope, FR, or SC edits), and records that commit in `CONCLUSIONS.md` (P9). Everything else in T001 is read-only reconnaissance and analysis. T001 also records a harness capability receipt: delay injection (yes/no and how), unreachable-server mechanism, fresh DI host per case or a proven reset, and time control (or real clock with existing budgets cited). A capability the harness lacks is built inside DEV-557's own contract or fixture files; adding it to a DEV-551 file goes beyond the Q1 extraction and needs a cited Patron ruling (STOP) (P4). If the merged harness is subtitle-bound, extract provider-agnostic WireMock lifetime / DI host build / telemetry capture into a shared base both suites use — structural only, no assertion edits; without touching DEV-551 subtitle assertions (*Wanted* 4; §2.3 item 6 carve-out; P3). Stop `needs recon:` if a required path/fact is missing. Stop for cited Patron ruling before any outside-envelope or §2.3 protected edit.
- **Task order (`brief.md` Task ordering constraints)**:
  1. Gate 1, then Phase B only after DEV-551 implementation is on main; drift check repins harness names.
  2. Stryker "before" receipt and line-count baseline on the Phase B base, before any edit.
  3. Confirm 401 outcomes from `spec.md` (recon citations); take a **draft twin inventory** (read-only test-name lists across lookup/resilience/telemetry, without assertions) before writing the contract class; implement contract base + OMDb + TMDb fixtures covering *Wanted* 1 **minimum** plus every twin in that draft (known extra: "transient server error then success"); all those cases green before any twin removal. A twin with no matching case gets a contract case — never a residual. `OmdbMetadataProvider.cs` stays read-only; production touch only on `TmdbMetadataProvider.cs` under Q3 if ordinary-401 key-problem assertion still requires TMDb alignment after suite asserts OMDb behaviour — expected outcome "none".
  4. Final twin inventory (removed-test → contract-case). If it finds another unmatched twin, add the case and re-green both fixtures before any removal; then remove twins family by family with tests green after each family.
  5. Q7 InspectCode fix if `SecondaryMetadataProviderTelemetryTests.cs` changed.
  6. Stryker "after" plus line-count receipts; full gates on the exact head; Size M three-axis review (two-round cap).
- **Do not**: invent harness type names in this draft; run the suite against `ChainedMetadataProvider`; add a metadata provider; change chain selection; add NuGet/npm/projects/layers/schema/public API; hardcode secrets; touch LocalPlay/`Process.Start`; lower gate thresholds; claim Gate 1 / plan freeze / delivery PASS from this draft; edit outside the authorized envelope without a cited Patron ruling.

### Authorized file envelope

| Path / role | When |
|---|---|
| New abstract contract class under `tests/LamuFlix.IntegrationTests/` | After Phase B name pin |
| New OMDb and TMDb derived fixture classes under `tests/LamuFlix.IntegrationTests/` | After Phase B name pin |
| DEV-551 harness base file(s) | Only under Q1 subtitle-bound extraction clause; structural only (no assertion edits). T001 lists every such file with HEAD blob hash and line count. Touching any file beyond those harness base file(s) — including a derived subtitle suite's base class or constructor — needs a cited Patron ruling (STOP) (P3) |
| `MetadataProviderLookupTests.cs`, `MetadataProviderResilienceTests.cs`, `MetadataProviderTelemetryTests.cs` | Twin removal |
| `SecondaryMetadataProviderLookupTests.cs`, `SecondaryMetadataProviderResilienceTests.cs`, `SecondaryMetadataProviderTelemetryTests.cs` | Twin removal; Q7 if telemetry file touched |
| `MetadataProviderTmdbSignalTests.cs`, `SecondaryMetadataProviderTrailerTests.cs`, `SecondaryMetadataProviderMappingTests.cs`, `MetadataProviderCompositionTests.cs`, `SecondaryMetadataProviderCompositionTests.cs`, `MetadataProviderChainCompositionTests.cs` | **Read-only** (single-adapter stay). If T001 or T008 finds a twin in one, Build stops for a cited Patron ruling before editing it (P2; Q4/Q6) |
| `OmdbMetadataProvider.cs` | **Read-only** (Q3 reference). Expected Q3 outcome: "none, no production edit" (recon OMDb `:452-455`) (P1) |
| `TmdbMetadataProvider.cs` | Only under Q3 401 alignment carve-out; expected outcome "none" (recon TMDb `:537-540`) (P1) |

Exact new contract/fixture file names are **not** frozen here — Phase B drift check pins them against the merged DEV-551 harness (`brief.md` Plan decisions / Q1).

### Quill-named roles (names deferred to Phase B)

Role labels may say "OMDb fixture" / "TMDb fixture". **Phase-B-pinned C# type names for the contract base and both fixtures MUST be functional** (existing family style: `MetadataProvider*` / `SecondaryMetadataProvider*`), **not** vendor-prefixed `Omdb*` / `Tmdb*`. Vendor names are allowed only on Infrastructure adapter classes (constitution VIII).

| Role | Intent |
|---|---|
| Metadata provider contract base | Abstract generic suite over `IMetadataProvider` with hooks for DI registration/options, WireMock stubs per provider wire format, and expected ordinary-401 outcome (advice text via fixture hook); functional type name at Phase B pin |
| OMDb fixture (role label) | Thin derived class registering OMDb via shipped DI; functional type name at Phase B pin (not `Omdb*`) |
| TMDb fixture (role label) | Thin derived class registering TMDb via shipped DI; functional type name at Phase B pin (not `Tmdb*`) |
| Shared harness base (conditional) | Provider-agnostic WireMock/DI/telemetry capture extracted from DEV-551 only if subtitle-bound |

## Verification Approach

- **Contract suite**: All Wanted cases plus inventory extras (including "transient server error then success") green for OMDb and TMDb through real typed client + resilience + quota handler against WireMock; telemetry span/duration/redaction; ordinary 401 = Failed `InvalidResponse` + one request + `wasUnauthorized`, advice via fixture hook (`brief.md` Test strategy / Contract case set; FR-001–FR-003). Each contract case runs on a fresh DI host and fresh WireMock state, or on a reset that T004 cites as proven; breaker and quota state must not carry over between cases; timing budgets come from the shipped options, with no invented SLA (P5).
- **Twin inventory**: Draft inventory before the contract class; final removed-test → contract-case table committed at `specs/DEV-557/twin-inventory.md` before removal; unmatched twin → add case and re-green fixtures first (safety net); `pr-body-DEV-557` copies that table and the Q3 401 difference statement (or "none"); family-by-family removal with green tests after each (`brief.md` Test strategy; FR-004; P11).
- **Residuals / stay**: Single-adapter suites remain and are read-only unless a cited Patron ruling opens them; adapter-specific residuals remain only where the contract cannot express them (Q4; P2).
- **Stryker / lines**: **Q5 receipts** from a direct Stryker run with explicit `--mutate` on both adapter files and identical config (including recorded `additional-timeout`); before on Phase B base (T002 measurement is the line-count comparator; 5,072 at `e1649d53` is historical context only), after on head; labelled receipts, not a gate PASS; no per-file drop; Q5 line-count drop with DEV-551-merged harness lines excluded and separated by diff stat (Q5; P8; P10). Exact-head `scripts/run-mutation.ps1` verdict reported separately with kill-share and file-span lines (P8).
- **Gates**: Exact-head Roslyn; cyclomatic complexity (incl. refactor threshold 6); InspectCode (Q7 if telemetry file touched); `dotnet test`; format; web scope-empty SKIPPED; honest SKIPPED/N/A/Could not run (`brief.md` Gate expectations).
- **Review / closing bar**: Size M Sentry, Ledger, Compass; missing axis blocks; two rounds max; unresolved Critical/High or Done-when/gate failure blocks; residual blockers to Bernstein at the cap (`brief.md` Closing bar).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-557/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md              # This file (also mirrored to private IMPL_PLAN)
├── tasks.md             # Also mirrored to private TASKS
└── DRAFTING_RECEIPT.md
```

### Source Code (authorized responsibility envelope)

```text
tests/LamuFlix.IntegrationTests/
├── <Phase-B-pinned contract base>.cs
├── <Phase-B-pinned OMDb fixture>.cs
├── <Phase-B-pinned TMDb fixture>.cs
├── MetadataProviderLookupTests.cs              # twin removal
├── MetadataProviderResilienceTests.cs          # twin removal
├── MetadataProviderTelemetryTests.cs           # twin removal
├── SecondaryMetadataProviderLookupTests.cs     # twin removal
├── SecondaryMetadataProviderResilienceTests.cs # twin removal
├── SecondaryMetadataProviderTelemetryTests.cs  # twin removal; Q7 if touched
├── MetadataProviderTmdbSignalTests.cs          # read-only stay; twin found → STOP for Patron ruling
├── SecondaryMetadataProviderTrailerTests.cs    # read-only stay; twin found → STOP for Patron ruling
├── SecondaryMetadataProviderMappingTests.cs    # read-only stay; twin found → STOP for Patron ruling
├── MetadataProviderCompositionTests.cs         # read-only stay; twin found → STOP for Patron ruling
├── SecondaryMetadataProviderCompositionTests.cs # read-only stay; twin found → STOP for Patron ruling
└── MetadataProviderChainCompositionTests.cs    # read-only stay; twin found → STOP for Patron ruling

src/LamuFlix.Infrastructure/Adapters/
├── MetadataProviderServiceCollectionExtensions.cs  # composition reference :43-59; read-only reference; no edit
├── OmdbMetadataProvider.cs                         # read-only Q3 reference; expected outcome "none"
└── TmdbMetadataProvider.cs                         # Q3 carve-out only; expected outcome "none"

# DEV-551 harness base path(s) — touched only under Q1 subtitle-bound extraction (structural only)
```

**Structure Decision**: Lands entirely in existing `tests/LamuFlix.IntegrationTests/` plus conditional Q1 harness extraction (harness base file(s) only) and conditional Q3 alignment on `TmdbMetadataProvider.cs` only (`OmdbMetadataProvider.cs` remains read-only). No new project or layer.
