# Implementation Plan: Outside-library IMDb quality floors

**Branch**: `feature/571-spec` | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)

**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-571-spec` @ `147a48becee09777f9d6c423108c29772f383d16` (Phase 2 ruling; challenge base `ef6dd3e694aa82d3bddd7effe3e31f96b5289a75`)

**Input**: Grill-closed `brief.md` (Q1–Q5 + Q3/Q4 update + Phase 2 plan-challenge decisions); `CONCLUSIONS.md` (Phase 2 ruling); Maestri notes `DEV-571`, `recon-DEV-571`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only — Quill does not edit it; ordinary Build excludes constitution edits).

**Status**: gate1: provisional — **blocked: structural** (owner Principle VII checkbox on the Spec Kit PR; Gate 1 / T000 closed until affirmative owner approval, verified merged constitution amendment via governance lane, and verified Spec Kit PR merge). An owner rejection is a stop for re-planning/reanalysis and never unlocks T001+. The draft Spec Kit PR may be prepared/opened with the unchecked owner checkbox. Quill does not invent PLAN FROZEN. Executed gate / rollout evidence is Phase B only.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `research.md`, `data-model.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `contracts/` — public discovery route/DTO/statuses and generated frontend contracts remain the DEV-561 baseline (`brief.md` Q3). No `quickstart.md` beyond this plan’s verification section.

## Summary

Apply inclusive IMDb floors (6.5 rating / 1,000 votes) with decisive known failures, durable candidate evidence, reuse of the landed application-wide OMDb daily reservation, and one-title-per-known-TMDb-collection selection (visible cap on admitted representatives with backfill beyond a former top-20 cut-off) to the existing read-only **Not in your library yet** row via the existing `IRecommendationCandidateSource` / `EfRecommendationCandidateSource` outside-library path. Preserve DEV-561 ranking, seed/exclusion rules, public contract, enrichment output, and library-grid behaviour. Roll out candidate-evidence migration → positive allowance provision → revised-guard deployment in environments that already use OMDb. Under Gate 1 / T000, implement the narrow runtime disabled behaviour for absent/zero/invalid allowance including malformed nonnumeric input.

## Technical Context

**Language/Version**: .NET 10 C# (`LamuFlix.Core`, `LamuFlix.Infrastructure`, `LamuFlix.Api`); existing React/Vite For You consumers unchanged (`specs/PRODUCT.md` §1).

**Primary Dependencies**: Existing stack only — EF Core / PostgreSQL, existing OMDb and TMDb adapters; Core `OmdbQuotaOptions` / `IOmdbRequestGate`; ServiceDefaults options binding/ValidateOnStart; Infrastructure `MetadataQuotaHandler` / `EfMetadataRequestGate` / `omdb_daily_usage`; existing injectable `TimeProvider` seam (recon lines 23–27). **No new NuGet/npm dependency** (`brief.md` Q3 / Q3–Q4 update; `PRODUCT.md` §5 item 1).

**Storage**: **One** new ticket-owned store in existing Infrastructure persistence — candidate IMDb evidence (keyed by TMDb ID) plus EF migration. **Reuse** landed `omdb_daily_usage` / gate for the shared OMDb daily quota (`brief.md` Q3–Q4 update; recon lines 23–28). No new quota counter table. No schema beyond the candidate-evidence ticket table.

**Testing**: xUnit unit/property tests; provider fixtures; real relational/Testcontainers persistence tests (never mock the data-access driver for query translation/serialization); architecture tests and format verification carried forward; existing API/web regression for contract and observable row/grid behaviour (`brief.md` Q5).

**Target Platform**: Home library API + For You web already shipping DEV-561 discovery.

**Project Type**: Existing feature-organised Core with ports-and-adapters seams (constitution Principle I) — .NET API + Vite/React web. No new project or top-level folder. Not VSA and not four-layer Clean Architecture.

**Performance Goals**: Visible row still capped at 20 **admitted representatives**; candidate pool retained beyond former cut-off for backfill; per-load live OMDb attempts ≤ min(open slots at load start, remaining shared allowance) at the individual-send boundary; no speculative or known-duplicate-collection lookups; cache/duplicate skips make zero outbound calls to both providers (`brief.md` Q2).

**Constraints**: Charter §2.3 items 1–6 ruled in Q3 / Q3–Q4 update / Phase 2. Public API/contract unchanged. No default/fallback/bypass allowance; under Gate 1 / T000 remove the 1000 default in the same step as disabled normalization. Preserve landed enrichment quota settlement. Runtime absent/zero/invalid (including malformed nonnumeric) host-up disabled behaviour is governed by Gate 1 / T000. No new port or layer; Core stays Infrastructure-free. Ordinary Build excludes constitution edits. Size M closing bar: Critical/High/Medium closed; Low disposed with evidence; two review rounds; honest gate labels including `FAIL` per T019 (`brief.md` Q5).

**Scale/Scope**: Size M; parent DEV-490; extends DEV-561 discovery only.

## Constitution Check

*GATE: Planning alignment checked before Phase 0 research. Re-check after Phase 1 design. Executed gate evidence is Phase B only — not claimed by this draft.*

Gates checked against `.specify/memory/constitution.md` (worktree current). **Owner structural checkbox present** — see `spec.md` Owner question — Gate 1. The draft Spec Kit PR may be prepared/opened with the unchecked owner checkbox. Affirmative owner approval + governance-lane `/speckit-constitution` amendment (version + sync-impact on the same Spec Kit PR) + verified merged amendment commit + verified Spec Kit PR merge unlock T000/T001+; a CONCLUSIONS receipt must reference the amendment commit and cannot substitute; owner rejection stops for re-planning/reanalysis and never unlocks this draft. This draft does **not** claim Constitution VII PASS and does **not** edit the constitution (ordinary Build excludes constitution edits).

| Principle / rule | Gate | Plan disposition (planning alignment only) |
|---|---|---|
| I (Ports and Adapters with a Feature-Organised Core) | Feature folders in Core; closed named-port list; Core isolation; ArchitectureTests | **PASS (planning alignment)** — extend existing OMDb/TMDb adapters and Infrastructure persistence; Core Recommendations ranking/selection stays in the existing feature slice as pure rules; **reuse** constitution-named `IOmdbRequestGate` and existing `IRecommendationCandidateSource` (already on the closed port list — no new port, layer, or ADR; recon 31–37). Scoped `EfRecommendationCandidateSource` orchestrates ordered I/O, cache, and coalescing and returns `OutsideLibraryRecommendationSnapshot` to `GetOutsideLibraryRecommendationsQueryHandler`. Carry forward architecture tests / `PortWhitelistPattern` restrictions and format verification. No sibling-feature references; shared concepts stay in Domain/Ports/Pipeline; Core stays Infrastructure-free. |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — reuse existing `GetOutsideLibraryRecommendationsQueryHandler`; no MediatR; no new architectural layer. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie lifecycle change; candidate-evidence store is separate from enrichment movie records; **preserve landed enrichment quota settlement** (recon / Q3–Q4 update). |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — public discovery empty/success contract unchanged; no new public error shape. Carry forward closed-set / cancellation conventions on async paths. |
| VI (Observability) | Safe diagnostics | **PASS** — carry safe bounded structured quota denial/defer diagnostics (counts, dates, TMDb IDs) on the existing path; do not log keys, credentials, full request URIs, or raw bodies; assert exclusion in tests. |
| VII (Configuration / LocalPlay / Process.Start) | Options; ValidateOnStart; deterministic time | **NOT PASS — blocked: structural** — reuse `OmdbQuotaOptions`; under Gate 1 / T000 remove the 1000 default in the same step as disabled normalization (VII-aligned: no guessed numeric default). **Current baseline at challenge-base HEAD** (recon lines 26–28): missing quota key defaults to 1,000; zero and negative values fail startup; nonnumeric-specific handling/test was not found in scoped recon. Under the merged owner amendment, bind the raw `Omdb:Quota:DailyLimit` key before typed conversion to positive enabled or explicit disabled/no-allowance; shared gate denies discovery and enrichment while disabled; other options stay startup-validated. Do **not** claim PASS before Gate 1 / T000. No LocalPlay, secrets, or `Process.Start`. Committed appsettings/AppHost/Compose edits outside ruling; Q4 provisioning for environments. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — spec-local vocabulary only; approved vocabulary / vendor-name exceptions for `IOmdbRequestGate` and `OmdbQuotaOptions` carried forward; no Quill ADR; no CONTEXT.md expansion required by brief. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit/property, provider fixtures, real relational persistence, architecture tests, enrichment + discovery API/row regression (`brief.md` Q5). |
| Tech stack / API docs | OpenAPI → types.ts | **PASS** — no public route/DTO/status change; contract drift checks must stay green / unchanged baseline (`brief.md` Q3). |
| Governance / Static-Analysis Gates | Honest exits | **PASS (planning alignment)** — Phase B records applicable gate outcomes with distinct honest labels per the single normative gate-label list in T019; never fold non-pass into PASS; never lower thresholds. Orphan deletion precedes gate runs. |

### Complexity Tracking

| Violation | Why needed | Simpler alternative rejected because |
|---|---|---|
| Principle VII runtime host-up disabled behaviour for absent/zero/invalid `OmdbQuotaOptions` daily allowance | Q3/Q4 want cached/confirmed-absence evaluation to continue without taking the host down when allowance is unusable | Keeping only current startup rejection fails Q3/Q4 operating goals for Wanted 2 without an owner-approved narrow VII exception |

Schema limited to the one candidate-evidence ticket table (`PRODUCT.md` §5 item 3; Q3–Q4 update). Public API unchanged (item 4). Dead-code deletion limited to orphans of this change (Q5), completed before gates.

## Approach

1. **Phase A (this draft)**: Spec Kit `spec.md` / `plan.md` / `tasks.md` from brief Q1–Q5 + Q3/Q4 update + Phase 2 ruling. Keel owns analyze and any later PLAN FROZEN. Spec PR may be prepared/opened carrying the unchecked owner Principle VII checkbox; after affirmative owner answer, governance lane lands the narrow constitution amendment on that PR; Gate 1 stays closed until affirmative answer + verified merged amendment commit + verified Spec Kit PR merge. Owner rejection stops for re-planning/reanalysis and never unlocks Build. Spec PR / Gate 1 follow freeze — not claimed here.
2. **Foundational evidence model and DEV-561 regression**: Establish provider evidence outcomes (complete pass, known floor failure including with unknown/absent counterpart, confirmed absence, unresolved only after those are evaluated — distinguishing explicit provider-confirmed absence from malformed/transient per recon lines 5–8; adversarial numeric fixtures) **before** migration; pin DEV-561 ranking/weight/cap/exclusion regressions (T002 may already be green) (`brief.md` Task order 1).
3. **Durable cache + external_ids + evidence-write + shared guard**: Implement ticket-owned **candidate-evidence only** persistence, EF mapping/migration, finish cache store read/write and both expiry windows against real relational persistence; add discovery-only TMDb on-demand `external_ids` (fixtures before impl) and the evidence-write path **with the store** before selection consumes the source; then in one post-Gate-1 step remove `OmdbQuotaOptions` 1000 default and implement disabled normalization via raw-key binding; reuse Core `OmdbQuotaOptions` / `IOmdbRequestGate`, ServiceDefaults binding, and Infrastructure `MetadataQuotaHandler` / `EfMetadataRequestGate` / `omdb_daily_usage`; in-process coalesce for identical misses on a shared Infrastructure instance (`brief.md` Task order 2; Phase 2 M4; Q3–Q4 update).
4. **Ordered scan / collection selection / budget backfill**: Through `EfRecommendationCandidateSource`, walk DEV-561 order serially retaining candidates beyond former cut-off; admit ≤20 collection representatives; skip known represented collections before lookup; honour fresh cached membership until expiry unless fresher local membership is known; recheck membership before admission when collection becomes known mid-resolution; resolve evidence only for open-slot candidates under the fixed per-load ceiling; enforce per-load and shared bounds at the individual-send boundary; validate IMDb ID before reserve/send; after exhaustion or send-boundary denial use cache/confirmed absence only with zero outbound calls to both providers on cache/duplicate skips; call Core pure ordering/eligibility/collection rules and return `OutsideLibraryRecommendationSnapshot` to the handler (`brief.md` Task order 3; Q2; Phase 2 H2). Early floor/collection checkpoints stay fixture-testable only — they do not imply production live wiring.
5. **Integrated verification + Q4 rollout receipt**: Prove enrichment through guarded adapter; two consumers share pool with failures charged and attempts attributable to each; migration → provision/read-back → revised-guard deploy in OMDb environments; run applicable gates with honest labels per T019; Size M review under Q5 bar — two rounds (`brief.md` Task order 4; Q4–Q5).
6. **Dead code**: Delete only code this change directly orphans, in the same delivery PR, **before** gate runs so the checked diff is the delivery diff.

### Authorized project files

| Area | Files and scope |
|---|---|
| OMDb adapter / quota | `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs`, `OmdbMetadataProvider.Mapping.cs`, `MetadataQuotaHandler.cs`, `MetadataProviderServiceCollectionExtensions.cs` — reuse handler reservation path; charge failures/timeouts/retries; validate IMDb request ID before reserve/send on discovery path; no call on fresh cache hit; preserve enrichment mapping behaviour and enrichment quota settlement (recon lines 5–8, 23–28) |
| Quota gate / store / options | Core `OmdbQuotaOptions.cs` + `IOmdbRequestGate`; ServiceDefaults options binding/ValidateOnStart (raw `Omdb:Quota:DailyLimit` before typed conversion under Gate 1 / T000); Infrastructure `EfMetadataRequestGate.cs`, `DailyRequestUsage*` / `omdb_daily_usage` — reuse landed seams (recon 23–27); remove 1000 default in same step as disabled normalization; no new quota table/port; no committed appsettings/AppHost/Compose scope |
| TMDb adapter | `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.cs`, `TmdbMetadataProvider.Mapping.cs` — on-demand `external_ids` (discovery-only; preserve enrichment output/public contract); reuse existing collection mapping (recon lines 10–13) |
| Ranking / selection / candidate source | Core pure rules beside `OutsideLibraryRanking.cs`; existing port `IRecommendationCandidateSource` / `OutsideLibraryRecommendationSnapshot`; scoped `EfRecommendationCandidateSource` (`PersistenceServiceCollectionExtensions.cs:76`) orchestrates ordered I/O, cache, coalescing; `GetOutsideLibraryRecommendationsQueryHandler` consumes the snapshot (recon 31–37). Keep other source methods/consumers intact; no new port or layer; Core Infrastructure-free |
| Persistence | Existing Infrastructure persistence area — **candidate-evidence** entity/mapping/migration only; DI registration; injectable `TimeProvider` seam for quota date and expiry. `EfMovieRepository.cs` enrichment rating copy remains reference behaviour, not this cache (recon lines 5–6) |
| Tests | Extend `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs`, `tests/LamuFlix.UnitTests/Features/Recommendations/OutsideLibraryRankingPropertyTests.cs`, `GetOutsideLibraryRecommendationsQueryHandlerTests.cs`, `tests/LamuFlix.UnitTests/Options/OmdbQuotaOptionsTests.cs`, `OptionsStartupValidationTests.cs`, `tests/LamuFlix.IntegrationTests/EfMetadataRequestGateTests.cs`; architecture/format verification carried forward; **adjacent new tests for this feature are allowed in these existing test projects**; add provider fixtures and real relational integration coverage (`brief.md` Files and components / Q3–Q4 update / Phase 2 L2) |
| Explicitly not approved | New dependency/project/folder/layer; new quota counter/port; public route/DTO/status; frontend/generated contracts; LocalPlay/secrets/`Process.Start`; constitution edits in ordinary Build; committed appsettings/AppHost/Compose as code scope; unrelated tracked edits (`brief.md` Q3 / Q3–Q4 update / Phase 2) |

Exact new file names inside those authorized areas are Build choices within the Q3 envelope.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-571/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md              # This file (worktree / Spec PR copy)
├── research.md
├── data-model.md
├── tasks.md
├── DRAFTING_RECEIPT.md
└── checklists/
    └── requirements.md
```

Private Spec Kit mirror: `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-571\plan.md` (same content as canonical `specs/DEV-571/plan.md`).

### Source Code (authorized boundary)

See Authorized project files above. Stay inside existing Core Recommendations, Infrastructure adapters/persistence/quota seams, DI/options, and existing test projects. No new top-level folder or architectural layer.

**Structure Decision**: Extend existing feature-organised Core / ports-and-adapters layout per constitution Principle I and Q3–Q4 update.

## Phase 0 research

See [research.md](research.md). All inventable unknowns for Gate 1 drafting are resolved from brief/CONCLUSIONS except the owner Principle VII checkbox (recorded on the Spec Kit PR; Gate 1 closed until affirmative approval + verified merged constitution amendment + verified Spec Kit PR merge; rejection stops for re-planning). Recon supplies path:line facts for reuse of the landed quota seam (recon 23–28), the candidate-source seam (recon 31–37), and the challenge-base HEAD baseline (missing key → 1000; zero/negative fail startup; no nonnumeric-specific handling/test found).

## Phase 1 design

See [data-model.md](data-model.md). No `contracts/` folder — public discovery contract unchanged from DEV-561 baseline.

## Verification (Phase B; not claimed by this draft)

- Unit/property: floors at and immediately below 6.5 / 1,000; mixed-evidence decisive known failures (including known floor failure with unknown votes stored 30 days); adversarial rating/vote fixtures (blank, N/A, separator/spacing, overflow, negatives, zero; ambiguous → unknown); valid rating with unknown votes defers with no durable row after known-failure/confirmed-absence evaluation; absence vs malformed/transient vs unresolved; like/watch weights; order/tie-break; backfill beyond former cut-off; collection representative + membership recheck + fresh-cache membership without remote refresh; uncollected titles; cap 20 admitted representatives; DEV-561 regressions (T002 may be green).
- Provider fixtures: parsing; on-demand external IDs with call-count bound including failed/empty outcomes; cache hit/fresh/expired; serial ordered resolution; mid-scan denial with lower cached survivor; malformed input ID zero-charge; midnight-spanning ceiling; one leader for concurrent same-key misses (waiters send/charge nothing; cancellation/retry); no speculative calls; zero outbound to both providers on cache/duplicate skips; per-load and shared exhaustion at individual-send boundary; chargeable failures/retries; ordinary enrichment; safe quota-denial diagnostics exclusion assertion.
- Real relational: candidate key uniqueness; durable reuse across distinct scopes/context instances; 30-day and 7-day expiry; atomic parallel reservations independent of load cap across separate loads/consumers; shared accounting across consumers with charged attempts attributable to each; UTC rollover; no-default, configured positive, and disabled allowance paths under Gate 1 / T000.
- Contract/row parity: unchanged discovery route/DTO/statuses/generated types **and** observable row/grid behaviour; enrichment output preserved; library-grid independence on loading/empty/error; SMOKE-001–003 including next eligible survivor with cached/confirmed-absent fill when live OMDb is disabled under FR-011 after Gate 1 / T000.
- Q4 receipt: migration → provision/read-back → revised-guard deploy (or no usable allowance without OMDb use); enrichment + two-consumer pool proof.
- Gates: orphan deletion first; then the T019 normative list — Roslyn, complexity, InspectCode, property/ordinary/integration tests, architecture tests, format verification, vulnerable packages, Stryker, API contract drift — each labeled distinctly including `FAIL`.
