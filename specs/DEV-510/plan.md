# Implementation Plan: Shrink the cached taste profile

**Branch**: `feature/DEV-510` (current worktree `F:/Dev/LamuFlix.worktrees/DEV-510`) | **Date**: 2026-11-15 | **Spec**: `specs/DEV-510/spec.md`

**Historical provenance**: Spec-phase worktree/branch `F:/Dev/LamuFlix.worktrees/feature-510-spec` / `feature/510-spec` (Gate 1 / planning freeze era only).

**Input**: Grill-closed `brief.md` (Q1–Q7); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-510`, `recon-DEV-510` (lines 27–37, 51–67; HEAD `6edd0d16f0dd51950c56e21f2c866467dc5dff77`); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: PLAN FROZEN — Keel, 2026-11-15, after Patron PA1 and independent cross-artifact reread; see PLAN_ADJUDICATION.md. Gate 1 / T000 complete (spec PR #415, `22261b35`). T001b serializer seam complete at f6647681; T002 baseline measured at d1809a46; feasibility PASSED under amended A1 (Patron 4abc7c53). Oversize-cache / Finding 1–2 derivative repair (Patron `0004b000`; brief.md final PA1 amendment) is synchronized here pending Keel independent reread; T003+ held for that reread. Implementation verification, delivery merge, T010 post-change, home promotion and runtime acceptance remain open; runtime-pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `EVIDENCE.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — representation/contracts decisions are frozen in the brief; no public API delta.

## Summary

Shrink the HybridCache-stored taste profile below the default 1 MiB payload ceiling by removing per-liked-source plot embeddings from cached `LikedSources`, transferring those embeddings into uncached `GetProfileInputsAsync` via one additional batched query, and proving exact recommendation title/order/score/reason parity. Measure real home payload/component bytes and limit behaviour without `IDistributedCache`, instrument structured size/budget observations with a configurable 0.8 warning fraction (`WarningFraction` const), and prove the amended two-tier A1 budget on realistic retained maps: 145-source ticket fixture and live 4-source home each strictly below 0.8× the effective ceiling; 300-source fixture in `[0.8×ceiling, ceiling)` with warning and real HybridCache admission/reuse without `MaximumPayloadBytesExceeded`. Preserve maps; keep 384-float source embeddings in uncached inputs; resolve the runtime ceiling from `IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes`. Historical 725-source / strict-80% FAILED projections remain provenance only. Run required gates, and track post-merge home promotion plus a full-day zero-`MaximumPayloadBytesExceeded` receipt as `runtime-pending` until the operator evidence exists.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Core`, `LamuFlix.Infrastructure`; recommendation consumers in Core feature handlers) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing stack only — HybridCache registration already used by `HybridCachedRecommendationProfileSource`, EF profile source, and existing recommendation handlers (`recon-DEV-510` lines 27–32). No new NuGet/npm dependency (`brief.md` Q3/Q7).

**Storage**: Existing PostgreSQL/EF recommendation profile reads. No schema or migration change (`brief.md` Q3). Cache value is in-process HybridCache only; no distributed cache addition.

**Testing**: Existing xUnit suites named in `recon-DEV-510` lines 35–37; Postgres Testcontainers for retrieval/query-count and serialization-sensitive behaviour; property coverage via existing `TasteProfileProperties` patterns. No new test library/dependency (`brief.md` Q5).

**Target Platform**: Local API/recommendation path; home Postgres for payload/latency evidence; post-merge home promotion for runtime acceptance (`brief.md` Q6).

**Project Type**: Existing ports-and-adapters .NET API with feature-organised Core and explicit handlers (constitution Principles I–II); internal representation change only — no `/web` or public API edits.

**Performance Goals**: Publish measured cold/uncached vs warm-cache `for-you` latency with matched inputs — no invented speedup SLA (`brief.md` Q6). One additional batched liked-source query per request; query count independent of source/candidate count.

**Constraints**: Charter §2.3 items 1–6 deliberately ruled (Q3). Retain effective ceiling read from HybridCacheOptions (ticket reports a 1,048,576-byte default); no truncation/silent data loss. Exact parity; no formula/weight change. Configurable warning fraction default 0.8 in existing options source of truth — no scattered literals; never lower harness thresholds. No secrets/vector/identity logs. Baseline before representation change. Post-merge runtime remains pending until operator receipt. Honest gate labels. Closing bar and 2×2 caps per Q2. Size M requires three review axes.

**Scale/Scope**: Size M; targeted edits to existing profile/cache/input/scoring consumers listed below; ticket work items 1–7 including home evidence.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox from this draft.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **PASS** — extend existing `IRecommendationProfileSource` / `GetProfileInputsAsync` return shape only; no new port or layer (`brief.md` Q3). |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — existing `GetRecommendationsQueryHandler` / `GetForYouRecommendationsQueryHandler` consumers only; no MediatR; no new handler type required by grill. |
| III (Typed query model) | MovieQuery | **N/A / PASS** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation; profile representation/scoring-input relocation only. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no public API/error-contract change. |
| VI (Observability) | Safe diagnostics | **PASS** — structured payload-byte and effective-budget observations plus warning; no vectors/identities/secrets/connection strings (`brief.md` Q6). |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — wire configurable warning fraction through existing `TasteProfileCacheOptions` / registration if needed; **preserve or extend** that type's existing typed options validation and `ValidateOnStart` for the new setting (constitution VII lines 648–652, 672 — non-subtitle configuration retains startup validation); add invalid-configuration regression coverage; existing .Validate chain and ValidateOnStart are confirmed by targeted recon lines 3-4; extend the chain without a new validator type; no LocalPlay, secrets, or Process.Start; retain default ceiling (`brief.md` Q3/Q4). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new glossary term or ADR; spec-local clarification of existing ticket terms only (`brief.md` Q7). |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit/property for scoring edges; Testcontainers for EF retrieval/query-count and real serialization/cache admission; home/runtime evidence for limit behaviour and full-day acceptance unit tests cannot prove. |
| Tech stack / API docs | OpenAPI chain | **PASS** — no public DTO/route/status change; no OpenAPI/TS generation edit. |
| Governance / Static-Analysis Gates | Honest exits | Phase B: Roslyn, complexity (including applicable refactor-gate obligation), InspectCode, property tests, vulnerable packages, `dotnet format --verify-no-changes`, `dotnet test`, mutation when production C# under `src/` changes, web SKIPPED (scope-empty) if `/web` untouched; never fold non-pass into PASS; never lower thresholds. Closing bar and 2×2 caps per Q2. |

### Complexity Tracking

No constitution violations requiring justification. Targeted edits to existing types are authorized by Patron Q3/Q7; no new architectural layer.

## Approach

- **Phase A (this draft)**: Quill writes `spec.md` / `plan.md` / `tasks.md` / checklist / `EVIDENCE.md` / receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon/PRODUCT only. Keel runs `/speckit-analyze` and independent reread before PLAN FROZEN. Spec PR: Bernstein coordinates; Rigger prepares/opens; user merges; Keel records Gate 1 merge receipt. No Build work before Gate 1.
- **Phase B pickup**: Fresh drift recon (T001, read-only); resolve exact paths for declarations not path-qualified in `recon-DEV-510`; Gauge baseline. Then T001b installs/proves the shared serializer seam over the unchanged incumbent representation before baseline. Amend brief/tasks only for authorized demonstrated drift.
- **Task order (`brief.md` Task ordering / Q7 / PA1 Serializer setup before baseline at 85feb708 / PA1/Q4 amendment at f17205ff)**:
  1. Read-only pickup recon (T001) — complete as read-only evidence.
  2. Serializer-only setup (T001b): register/inject shared `IHybridCacheSerializer<CachedProfile>` in the two authorized persistence files + registration/HybridCache suites; compile accessibility AND runtime same-singleton exact-envelope + real `GetOrCreateAsync` admission proof — **complete at f6647681**.
  3. Capture pre-change home payload/component through the live T001b seam, no-distributed-cache limit behaviour, matched latency, and fixed scoring baseline into `EVIDENCE.md` (T002 at d1809a46); record original pickup SHA and seam SHA/configuration/serializer provenance; amended-A1 two-tier feasibility stop (145 + live 4-source home < 0.8× ceiling; 300 in `[0.8×ceiling, ceiling)`). Existing evidence: 145 = 465,340 B; 300 = 940,943 B; live home = 41,908 B; verified parity digest `8175b85ab052b9429ad25b2816ce0d62ffb34154c18d713c6bc1b05c5c07eb3c` (stale planning digest `9b66ab6e…e259a2` retained as historical unverified provenance; superseded by brief.md / CONCLUSIONS.md T010 digest disposition at b449c17b). Historical 725/strict-80% FAILED projections preserved. After sync + Keel reread, T002 PASSED under amended A1; T003+ unlocks.
  4. Extend existing internal profile-input contract + EF one-query liked-source retrieval with query-count regressions; keep inputs uncached.
  5. Remove only source plot vectors from cached profile; wire calculator/similarity consumers preserving semantics; prove exact parity on both recommendation consumers.
  6. Size budget/configuration (warning fraction with preserved/extended typed options validation + `ValidateOnStart`), structured warning, 145- and 300-source fixtures under amended A1, warning and ceiling boundary coverage as applicable (ceiling using T002 no-distributed-cache evidence), cache admission/reuse on the compact profile while **reusing the T001b seam** (T007/T008 do not reinstall the serializer), property/Testcontainers and existing cache regressions.
  7. Record post-change measurements and run required gates at exact SHA (T010/T011 pending).
  8. User-owned merge → authorized operator home promotion + ≥24h receipt (`runtime-pending` until filled).
- **Do not**: raise ceiling; truncate/thin maps; invent measurements; cache inputs; change scoring weights; edit public API/`/web`; claim runtime PASS from gates alone; invent an exact `AddSerializer` overload; relabel the historical 725 FAILED projection as passing.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-510/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file
├── tasks.md
├── EVIDENCE.md          # Runbook / evidence slots (pending until filled)
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief; internal-only representation; no API contract delta.

### Source Code (authorized targeted boundary — brief Q3/Q7; recon 27–37)

```text
src/LamuFlix.Core/Domain/
└── TasteProfile.cs                              # remove only per-source cached plot vectors; retain maps/identities/aggregates

src/LamuFlix.Core/Features/Recommendations/
└── TasteProfileCalculator.cs                    # construct compact liked-source representation

src/LamuFlix.Core/Ports/
└── IRecommendationProfileSource.cs              # extend GetProfileInputsAsync return-type declaration with liked-source embeddings

src/LamuFlix.Infrastructure/Persistence/
├── EfRecommendationProfileSource.cs             # one additional batched liked-source embedding query
├── HybridCachedRecommendationProfileSource.cs   # T001b: shared IHybridCacheSerializer<CachedProfile> injection over unchanged incumbent; later compact cache value; versioned key; construction-only observation via that seam
├── HybridCachedRecommendationProfileSource.Log.cs # authorized minimal source-generated LoggerMessage partial (T008)
├── TasteProfileCacheOptions.cs                  # finite WarningFraction, const 0.8 default (T008)
└── PersistenceServiceCollectionExtensions.cs    # T001b: explicit shared IHybridCacheSerializer<CachedProfile> registration (thin internal serializer / internal envelope permitted); later wire warning fraction / ceiling options if needed; preserve or extend typed options validation + ValidateOnStart; no new layer/package

Existing declarations (exact paths via recon if not listed above — do not invent):
├── ProfileSimilarityCalculator + BestLikedSource / PlotChannel consumers
├── GetRecommendationsQueryHandler
├── GetForYouRecommendationsQueryHandler
└── TasteProfileCacheOptions (exact authorized path above)

Existing suites (recon 35–37; exact paths via recon at pickup):
├── HybridCachedRecommendationProfileSourceCacheTests
├── HybridCachedRecommendationProfileSourceColdTests
├── HybridCachedRecommendationProfileSourceFreshnessTests
├── EfRecommendationProfileSourceTests
├── TasteProfileCalculatorTests
├── TasteProfileProperties
├── TasteProfileReviewAggregateTests
├── tests/LamuFlix.UnitTests/Options/TasteProfileCacheOptionsTests.cs
└── tests/LamuFlix.UnitTests/Persistence/TasteProfileCacheRegistrationTests.cs
```

**Structure Decision**: Stay inside existing Core domain/ports/features and Infrastructure persistence adapters. No new project, top-level folder, or architectural layer. Public Api/Endpoints and `/web` remain out of bounds. Any tracked file outside this authorized set needs a cited Patron ruling before edit.

## Test Strategy

| Family | Required evidence |
|---|---|
| Exact parity | Fixed recorded snapshot/candidate set; pre/post title/order, exact score, reason; both consumers; no tolerance |
| Unit / property | Multiple liked sources; missing/zero vectors; ties; empty/disliked-only; retained non-plot channels |
| Testcontainers retrieval | Liked-source embeddings keyed correctly; one additional batched query; count independent of source/candidate count |
| Cache regressions | Admission/reuse; freshness/invalidation/generation; expiration; concurrency/coalescing; cancellation; factory failure; scope-lifetime; uncached inputs |
| Size / warning | T001b installs shared singleton `IHybridCacheSerializer<CachedProfile>` / options and proves compile accessibility plus runtime same-singleton exact-envelope measurement and real `GetOrCreateAsync` admission over unchanged incumbent (complete at f6647681). T002 baselines through that seam (d1809a46). T007/T008 reuse the seam for amended-A1 compact-profile budgets: 145-source fixture and live 4-source home each < 0.8× ceiling; 300-source fixture in `[0.8×ceiling, ceiling)` with warning and real admission/reuse without `MaximumPayloadBytesExceeded`; warning-fraction below/equal/above; payload-ceiling below/equal/above as applicable using T002 confirmed no-`IDistributedCache` limit behaviour (preserve failure semantics; record evidenced inapplicability rather than assuming rejection); runtime observation/admission agreement and warm reuse; invalid-configuration regressions under preserved/extended typed validation + `ValidateOnStart` with `WarningFraction` const 0.8; no per-hit re-serialize; do not reinstall serializer. Historical 725/strict-80% FAILED evidence preserved, not active |
| Home / runtime | Baseline + post-change payload/component/latency; no-distributed-cache limit conclusion; post-merge ≥24h zero exceedance + warm latency — or explicit pending/Could not run |
| Gates | Roslyn, complexity, InspectCode, format, full tests, applicable property/security/mutation/web; honest labels; mutants → tests |

## Phase 0 / Phase 1 note

Per Spec Kit plan workflow: unknowns that would normally populate `research.md` are already ruled in brief Q1–Q7. No inventable-behaviour `needs decision` items opened by Quill in this drafting ask. Exact paths for some existing declarations and suite files are **needs recon** pickup facts, not Quill inventions. No data-model or contracts artifacts (internal representation; no public API delta). Planning analyze/reread is complete; PLAN FROZEN is recorded in PLAN_ADJUDICATION.md. T000 Gate 1 merge receipt is recorded in CONCLUSIONS.md. T001b complete at f6647681; T002 baseline measured at d1809a46. Feasibility-budget sync (amended A1 two-tier) awaits Keel independent reread before T003+ dispatch. Outstanding stage prerequisites: independent reread of this sync; T003+ implementation; T007/T009 budget/admission tests; T010/gates; runtime receipt.

## Post-design Constitution Check

Re-checked after documenting the targeted internal structure above: still PASS/N/A as in the Constitution Check table; no new port, handler framework, schema, public API, LocalPlay, secrets, Process.Start, CONTEXT, or ADR obligations introduced by this plan.

## PA1 binding implementation decisions

Patron ruling: brief.md PA1 / CONCLUSIONS.md PA1, plus the 2026-11-16 PA1 amendment Explicit shared serializer (CONCLUSIONS.md T001 Measurement Seam Design Disposition at fca46e8c), the superseding PA1 amendment Serializer setup before baseline (CONCLUSIONS.md T001/T002/T008 Serializer Setup Sequencing Ruling at 85feb708; recorded first in brief.md), the PA1/Q4 amendment Measured two-tier feasibility budget (CONCLUSIONS.md T002 Feasibility Checkpoint Disposition at 4abc7c53; brief.md at f17205ff; `ASSUMPTIONS.md` A1), and the PA1 amendment Oversize cache behavior and reread provenance (Patron CONCLUSIONS.md at `0004b000`; brief.md final PA1 amendment). Dispositions: PLAN_ADJUDICATION.md (Sentry F2 / Compass F1 amended for Patron budget disposition; Ledger M3 / Sentry F4 725 invariants qualified as superseded; Sentry F6 oversize / no-false-warm-hit qualified by `0004b000`). These requirements supersede draft deferrals and remove prior internal-serializer-inspection / blanket-wrapper-prohibition instructions, the T008-install sequencing, and the blanket no-warm-hit assumption; original recon references retain their historical snapshot numbering, while targeted implementation seams are replacement note lines 3-7.

1. T001 remains strictly read-only pickup recon/reanalysis/Gauge baseline (complete as read-only evidence). T001b alone installs explicit shared `IHybridCacheSerializer<CachedProfile>` registration and adapter injection in already-authorized `PersistenceServiceCollectionExtensions.cs` and `HybridCachedRecommendationProfileSource.cs` plus `TasteProfileCacheRegistrationTests.cs` and existing HybridCache suites from T001 (thin internal serializer and internal envelope accessibility permitted; no new package/project/layer), over the **unchanged** incumbent TasteProfile/LikedSources representation — **complete at f6647681** with compile accessibility AND runtime same-singleton exact-envelope + real `GetOrCreateAsync` admission proof. Forbid T001b edits to domain/calculator/port/EF/scoring/inputs/cache-key namespace. No reflection, independently assumed JSON defaults, replacement admission path, or assumed serializer invocation without distributed cache. Read IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes at runtime and in tests; do not repeat the ticket-default byte count as a test oracle.
2. T002 depends on successful T001b and is a hard stop before T003+: measure the unchanged pre-representation profile through the live T001b seam; record original pickup SHA **and** serializer-seam SHA plus serializer/configuration provenance; obtain the pre-change fixed scoring snapshot and measured retained-maps/envelope split plus amended-A1 two-tier feasibility proof (145-source ticket fixture and live 4-source home each strictly below 0.8× effective ceiling; 300-source fixture strictly below ceiling inside `[0.8×ceiling, ceiling)`); separate no-`IDistributedCache` reproduction; matched cold/warm timings. **Actual receipts (d1809a46)**: 145 = 465,340 B (44.4%); 300 = 940,943 B (89.7%); live home = 41,908 B (4.0%); verified parity digest `8175b85ab052b9429ad25b2816ce0d62ffb34154c18d713c6bc1b05c5c07eb3c` (stale planning digest `9b66ab6e08f9a5091fe48712ccebd9c1b7faeffbfc95c11795bbcb1235e259a2` retained as historical unverified provenance; superseded by brief.md / CONCLUSIONS.md T010 digest disposition at b449c17b); historical 725 projections FAILED at 155–220% and remain provenance. After derivative sync + Keel independent reread confirming consistent budgets, T002 is **PASSED under amended A1** (Patron 4abc7c53) and T003+ unlocks. Preserve all maps; do not thin. Only a proposed ticket contradiction or constitution departure escalates to owner.
3. After loading the compact profile, query exactly that profile's LikedSources identity set using existing key equality. Missing/partial results retain missing-vector semantics; concurrent membership changes do not replace the selected key set, and no new transactional snapshot guarantee is claimed. Empty set skips embedding query, returns empty map. Nonempty set uses one batched query, propagates CancellationToken. Both handlers pass extended inputs; no cached source plot-vector member remains. Parity remains exact for the same fixed inputs.
4. Extend existing TasteProfileCacheOptions .Validate chain plus ValidateOnStart in PersistenceServiceCollectionExtensions. WarningFraction is finite and 0 < fraction <= 1; const default 0.8, no static readonly mutable initialization. Match existing SectionName:nameof(property) error-message style. Existing options/registration paths above cover zero, one, negative, above-one, NaN, positive and negative infinity.
5. Logging only: minimal HybridCachedRecommendationProfileSource.Log.cs partial and source-generated LoggerMessage; structured numeric PayloadBytes, BudgetBytes, Fraction only, no interpolation/content. On factory construction: observe/warn before admission using the shared singleton serializer/options on the exact envelope; size alone never throws; return profile to coalesced callers. **Historical PA1 “swallowed non-admission / no false warm hit” assumption is superseded** by Patron `0004b000` / brief.md PA1 amendment Oversize cache behavior: local-cache-enabled / no-`IDistributedCache` oversize warns before admission, logs HybridCache `MaximumPayloadBytesExceeded` at Error, returns successfully, and retains native L1 reuse (`entryWasReused = true`, `SerializeCount = 1`); actual non-admission (`entryWasReused = false`, factory re-invocation) is only the measured `DisableLocalCache` + `IDistributedCache` L2 case. No adapter-level size rejection, L1 eviction, or bypass. Below/equal ceiling reuses without the payload-ceiling Error; `WarningFraction` remains a separate boundary (including the 300-source warning-band fixture). **T008 does not reinstall the serializer** — it reuses the T001b seam. T007/T008 retain compact-profile options/logging/boundary/runtime reuse proofs under amended A1 and that two-configuration contract: 145 and live-home < 0.8; 300 warning-band + admission/reuse without `MaximumPayloadBytesExceeded`; observation/admission agreement below/equal/above the real ceiling; real `GetOrCreateAsync` reuse; separate no-`IDistributedCache` limit behaviour from T002 — compilation alone is not that proof; also assert numeric-only logs and no per-hit serialization.
6. Version compact cache key namespace; legacy entries cannot be consumed. Keep generation/expiry/invalidation semantics. Seed old namespace in regression and prove rebuild under compact namespace without scoring old vectors. Cache-key namespace versioning remains a later (T005b/T009) obligation — not T001b.
7. T005a extends inputs and EF retrieval while retaining a compiling compatible path; T005b removes cached vectors and wires both consumers. Each boundary compiles and relevant tests are green. Pre-change parity stays green; edge-contract tests may begin red; TDD optional. Warm evidence explicitly records profile cache hit plus uncached embedding batch, or zero batch for empty identities, with query count/cost and vector volume.
8. Worktree mutation invocation is pwsh -NoProfile -File ./scripts/run-mutation.ps1 without --since; enforce configured maxKillShare/maxKillFileSpan and timeout safeguards, never lower thresholds. Three technology-agnostic checklist assertions are waived, not PASS. Ledger L4 redundant process prose is harmless follow-up, no cleanup or ticket. Task inventory is 17 (T001b inserted); 22/22 FR+SC coverage preserved. T010/gates/runtime remain pending.
