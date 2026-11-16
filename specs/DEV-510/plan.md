# Implementation Plan: Shrink the cached taste profile

**Branch**: `feature/510-spec` | **Date**: 2026-11-15 | **Spec**: `specs/DEV-510/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q7); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-510`, `recon-DEV-510` (lines 27–37, 51–67; HEAD `6edd0d16f0dd51950c56e21f2c866467dc5dff77`); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: PLAN FROZEN — Keel, 2026-11-15, after Patron PA1 and independent cross-artifact reread; see PLAN_ADJUDICATION.md. Gate 1 merge receipt recorded separately in CONCLUSIONS.md (T000 complete). Round-1 serializer-sequencing repair (T001b; 16→17 tasks; 22/22 preserved) is synchronized here pending Keel independent reread; T001/T001b/T002 held. Implementation verification, home measurement/promotion and runtime acceptance remain open; runtime-pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `EVIDENCE.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — representation/contracts decisions are frozen in the brief; no public API delta.

## Summary

Shrink the HybridCache-stored taste profile below the default 1 MiB payload ceiling by removing per-liked-source plot embeddings from cached `LikedSources`, transferring those embeddings into uncached `GetProfileInputsAsync` via one additional batched query, and proving exact recommendation title/order/score/reason parity. Measure real home payload/component bytes and limit behaviour without `IDistributedCache`, instrument structured size/budget observations with a configurable 0.8 warning fraction, prove a 725-source fivefold fixture and current home profile stay under 80 percent of the effective ceiling, run required gates, and track post-merge home promotion plus a full-day zero-`MaximumPayloadBytesExceeded` receipt as `runtime-pending` until the operator evidence exists.

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
- **Task order (`brief.md` Task ordering / Q7 / PA1 Serializer setup before baseline at 85feb708)**:
  1. Read-only pickup recon (T001).
  2. Serializer-only setup (T001b): register/inject shared `IHybridCacheSerializer<CachedProfile>` in the two authorized persistence files + registration/HybridCache suites; compile accessibility AND runtime same-singleton exact-envelope + real `GetOrCreateAsync` admission proof; no domain/calculator/port/EF/scoring/inputs/cache-namespace edits; no assumed serializer invocation without distributed cache.
  3. Capture pre-change home payload/component through the live T001b seam, no-distributed-cache limit behaviour, matched latency, and fixed scoring baseline into `EVIDENCE.md` (T002); record original pickup SHA and seam SHA/configuration/serializer provenance; strict 725/0.8 feasibility stop; unavailable home remains pending/Could not run.
  4. Extend existing internal profile-input contract + EF one-query liked-source retrieval with query-count regressions; keep inputs uncached.
  5. Remove only source plot vectors from cached profile; wire calculator/similarity consumers preserving semantics; prove exact parity on both recommendation consumers.
  6. Size budget/configuration (warning fraction with preserved/extended typed options validation + `ValidateOnStart`), structured warning, fivefold fixture, warning and ceiling boundary coverage as applicable (ceiling using T002 no-distributed-cache evidence), cache admission/reuse on the compact profile while **reusing the T001b seam** (T007/T008 do not reinstall the serializer), property/Testcontainers and existing cache regressions.
  7. Record post-change measurements and run required gates at exact SHA.
  8. User-owned merge → authorized operator home promotion + ≥24h receipt (`runtime-pending` until filled).
- **Do not**: raise ceiling; truncate; invent measurements; cache inputs; change scoring weights; edit public API/`/web`; claim runtime PASS from gates alone; invent an exact `AddSerializer` overload.

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
| Size / warning | T001b installs shared singleton `IHybridCacheSerializer<CachedProfile>` / options and proves compile accessibility plus runtime same-singleton exact-envelope measurement and real `GetOrCreateAsync` admission over unchanged incumbent (compilation alone insufficient; no assumed serializer invocation without distributed cache). T002 baselines through that seam. T007/T008 reuse the seam for compact-profile fivefold < 80% ceiling; home < 80%; warning-fraction below/equal/above; payload-ceiling below/equal/above as applicable using T002 confirmed no-`IDistributedCache` limit behaviour (preserve failure semantics; record evidenced inapplicability rather than assuming rejection); runtime observation/admission agreement and warm reuse; invalid-configuration regressions under preserved/extended typed validation + `ValidateOnStart`; no per-hit re-serialize; do not reinstall serializer |
| Home / runtime | Baseline + post-change payload/component/latency; no-distributed-cache limit conclusion; post-merge ≥24h zero exceedance + warm latency — or explicit pending/Could not run |
| Gates | Roslyn, complexity, InspectCode, format, full tests, applicable property/security/mutation/web; honest labels; mutants → tests |

## Phase 0 / Phase 1 note

Per Spec Kit plan workflow: unknowns that would normally populate `research.md` are already ruled in brief Q1–Q7. No inventable-behaviour `needs decision` items opened by Quill in this drafting ask. Exact paths for some existing declarations and suite files are **needs recon** pickup facts, not Quill inventions. No data-model or contracts artifacts (internal representation; no public API delta). Planning analyze/reread is complete; PLAN FROZEN is recorded in PLAN_ADJUDICATION.md. T000 Gate 1 merge receipt is recorded in CONCLUSIONS.md. Round-1 sequencing repair (T001b; 17-task inventory; 22/22 preserved) awaits Keel independent reread before T001/T001b/T002 dispatch. Outstanding stage prerequisites: independent reread of this repair; T001b compile+runtime receipts; measured baseline; implementation; runtime receipt.

## Post-design Constitution Check

Re-checked after documenting the targeted internal structure above: still PASS/N/A as in the Constitution Check table; no new port, handler framework, schema, public API, LocalPlay, secrets, Process.Start, CONTEXT, or ADR obligations introduced by this plan.

## PA1 binding implementation decisions

Patron ruling: brief.md PA1 / CONCLUSIONS.md PA1, plus the 2026-11-16 PA1 amendment Explicit shared serializer (CONCLUSIONS.md T001 Measurement Seam Design Disposition at fca46e8c) and the superseding PA1 amendment Serializer setup before baseline (CONCLUSIONS.md T001/T002/T008 Serializer Setup Sequencing Ruling at 85feb708; recorded first in brief.md). Dispositions: PLAN_ADJUDICATION.md (Ledger M3 / Sentry F4 amended for T001b sequencing; prior no-outstanding claim withdrawn pending independent reread). These requirements supersede draft deferrals and remove prior internal-serializer-inspection / blanket-wrapper-prohibition instructions and the T008-install sequencing; original recon references retain their historical snapshot numbering, while targeted implementation seams are replacement note lines 3-7.

1. T001 remains strictly read-only pickup recon/reanalysis/Gauge baseline. T001b alone installs explicit shared `IHybridCacheSerializer<CachedProfile>` registration and adapter injection in already-authorized `PersistenceServiceCollectionExtensions.cs` and `HybridCachedRecommendationProfileSource.cs` plus `TasteProfileCacheRegistrationTests.cs` and existing HybridCache suites from T001 (thin internal serializer and internal envelope accessibility permitted; no new package/project/layer), over the **unchanged** incumbent TasteProfile/LikedSources representation. Forbid T001b edits to domain/calculator/port/EF/scoring/inputs/cache-key namespace. Do not invent an exact `AddSerializer` overload — signature/DI details remain verification requirements, not established API facts. T001b requires **both** passing compiler/type-check accessibility evidence **and** a passing runtime test proving HybridCache admission and construction-only exact-envelope byte measurement invoke that same registered singleton serializer/options on the actual `CachedProfile(Profile, Generation, CreatedAt)` envelope with real `GetOrCreateAsync` admission. Compilation alone is insufficient. No reflection, independently assumed JSON defaults, replacement admission path, or assumed serializer invocation without distributed cache. Read IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes at runtime and in tests; do not repeat the ticket-default byte count as a test oracle. Unsupported registration/accessibility or failed runtime agreement blocks for recon/Patron. T001/T001b/T002 stay held until synchronized-artifact independent reread; after that, T001b needs its own compile+runtime receipts before T002 dispatch.
2. T002 depends on successful T001b and is a hard stop before T003+: measure the unchanged pre-representation profile through the live T001b seam; record original pickup SHA **and** serializer-seam SHA plus serializer/configuration provenance; obtain the pre-change fixed scoring snapshot and measured retained-maps/envelope split plus realistic fivefold projection strictly below 80% effective ceiling; separate no-`IDistributedCache` reproduction; matched cold/warm timings. Missing proof blocks edits. Record unavailable home evidence honestly as pending/Could not run, request recon or Patron ruling, retain 725/0.8 and all maps. Only a proposed ticket contradiction or constitution departure escalates to owner; no automatic structural escalation for a failed projection.
3. After loading the compact profile, query exactly that profile's LikedSources identity set using existing key equality. Missing/partial results retain missing-vector semantics; concurrent membership changes do not replace the selected key set, and no new transactional snapshot guarantee is claimed. Empty set skips embedding query, returns empty map. Nonempty set uses one batched query, propagates CancellationToken. Both handlers pass extended inputs; no cached source plot-vector member remains. Parity remains exact for the same fixed inputs.
4. Extend existing TasteProfileCacheOptions .Validate chain plus ValidateOnStart in PersistenceServiceCollectionExtensions. WarningFraction is finite and 0 < fraction <= 1; const default 0.8, no static readonly mutable initialization. Match existing SectionName:nameof(property) error-message style. Existing options/registration paths above cover zero, one, negative, above-one, NaN, positive and negative infinity.
5. Logging only: minimal HybridCachedRecommendationProfileSource.Log.cs partial and source-generated LoggerMessage; structured numeric PayloadBytes, BudgetBytes, Fraction only, no interpolation/content. On factory construction: observe/warn before admission using the shared singleton serializer/options on the exact envelope; size alone never throws. Preserve incumbent swallowed rejection and return profile to coalesced callers; over-ceiling cannot masquerade as warm admission. **T008 does not reinstall the serializer** — it reuses the T001b seam. T007/T008 retain compact-profile options/logging/boundary/runtime reuse proofs: observation/admission agreement below/equal/above the real ceiling, real `GetOrCreateAsync` reuse, and separate no-`IDistributedCache` limit behaviour from T002 — compilation alone is not that proof; also assert numeric-only logs and no per-hit serialization.
6. Version compact cache key namespace; legacy entries cannot be consumed. Keep generation/expiry/invalidation semantics. Seed old namespace in regression and prove rebuild under compact namespace without scoring old vectors. Cache-key namespace versioning remains a later (T005b/T009) obligation — not T001b.
7. T005a extends inputs and EF retrieval while retaining a compiling compatible path; T005b removes cached vectors and wires both consumers. Each boundary compiles and relevant tests are green. Pre-change parity stays green; edge-contract tests may begin red; TDD optional. Warm evidence explicitly records profile cache hit plus uncached embedding batch, or zero batch for empty identities, with query count/cost and vector volume.
8. Worktree mutation invocation is pwsh -NoProfile -File ./scripts/run-mutation.ps1 without --since; enforce configured maxKillShare/maxKillFileSpan and timeout safeguards, never lower thresholds. Three technology-agnostic checklist assertions are waived, not PASS. Ledger L4 redundant process prose is harmless follow-up, no cleanup or ticket. Task inventory is 17 (T001b inserted); 22/22 FR+SC coverage preserved.
