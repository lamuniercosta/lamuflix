# Implementation Plan: Shrink the cached taste profile

**Branch**: `feature/510-spec` | **Date**: 2026-11-15 | **Spec**: `specs/DEV-510/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q7); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-510`, `recon-DEV-510` (lines 27–37, 51–67; HEAD `6edd0d16f0dd51950c56e21f2c866467dc5dff77`); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: PLAN FROZEN — Keel, 2026-11-15, after Patron PA1 and independent cross-artifact reread; see PLAN_ADJUDICATION.md. Gate 1, implementation verification, home measurement/promotion and runtime acceptance remain open; runtime-pending.

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
- **Phase B pickup**: Fresh drift recon; resolve exact paths for declarations not path-qualified in `recon-DEV-510`; confirm serializer/options used by HybridCache path; Gauge baseline. Amend brief/tasks only for authorized demonstrated drift.
- **Task order (`brief.md` Task ordering / Q7)**:
  1. Capture pre-change home payload/component, no-distributed-cache limit behaviour, matched latency, and fixed scoring baseline into `EVIDENCE.md`.
  2. Extend existing internal profile-input contract + EF one-query liked-source retrieval with query-count regressions; keep inputs uncached.
  3. Remove only source plot vectors from cached profile; wire calculator/similarity consumers preserving semantics; prove exact parity on both recommendation consumers.
  4. Size budget/configuration (warning fraction with preserved/extended typed options validation + `ValidateOnStart`), structured warning, fivefold fixture, warning and ceiling boundary coverage as applicable (ceiling using T002 no-distributed-cache evidence), cache admission/reuse, property/Testcontainers and existing cache regressions.
  5. Record post-change measurements and run required gates at exact SHA.
  6. User-owned merge → authorized operator home promotion + ≥24h receipt (`runtime-pending` until filled).
- **Do not**: raise ceiling; truncate; invent measurements; cache inputs; change scoring weights; edit public API/`/web`; claim runtime PASS from gates alone.

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
├── HybridCachedRecommendationProfileSource.cs   # compact cache value; versioned key; construction-only observation
├── HybridCachedRecommendationProfileSource.Log.cs # authorized minimal source-generated LoggerMessage partial
├── TasteProfileCacheOptions.cs                  # finite WarningFraction, const 0.8 default
└── PersistenceServiceCollectionExtensions.cs    # wire warning fraction / ceiling options if needed; preserve or extend typed options validation + ValidateOnStart

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
| Size / warning | Production-equivalent serialization of envelope; fivefold fixture < 80% ceiling; home < 80%; warning-fraction below/equal/above; payload-ceiling below/equal/above as applicable using T002 confirmed no-`IDistributedCache` limit behaviour (preserve failure semantics; record evidenced inapplicability rather than assuming rejection); invalid-configuration regressions for the new warning option under preserved/extended typed validation + `ValidateOnStart`; no per-hit re-serialize |
| Home / runtime | Baseline + post-change payload/component/latency; no-distributed-cache limit conclusion; post-merge ≥24h zero exceedance + warm latency — or explicit pending/Could not run |
| Gates | Roslyn, complexity, InspectCode, format, full tests, applicable property/security/mutation/web; honest labels; mutants → tests |

## Phase 0 / Phase 1 note

Per Spec Kit plan workflow: unknowns that would normally populate `research.md` are already ruled in brief Q1–Q7. No inventable-behaviour `needs decision` items opened by Quill in this drafting ask. Exact paths for some existing declarations and suite files are **needs recon** pickup facts, not Quill inventions. No data-model or contracts artifacts (internal representation; no public API delta). Planning analyze/reread is complete; PLAN FROZEN is recorded in PLAN_ADJUDICATION.md. Outstanding stage prerequisites: Gate 1; implementation; measured evidence; runtime receipt.

## Post-design Constitution Check

Re-checked after documenting the targeted internal structure above: still PASS/N/A as in the Constitution Check table; no new port, handler framework, schema, public API, LocalPlay, secrets, Process.Start, CONTEXT, or ADR obligations introduced by this plan.

## PA1 binding implementation decisions

Patron ruling: brief.md PA1 / CONCLUSIONS.md PA1; dispositions: PLAN_ADJUDICATION.md. These requirements supersede draft deferrals; original recon references retain their historical snapshot numbering, while targeted implementation seams are replacement note lines 3-7.

1. T001 verifies supported serializer resolution/API against the pinned HybridCache package. Measure the same CachedProfile with the same registered HybridCache serializer/options used for admission; do not use independent JSON defaults or create a wrapper. Keep GetOrCreateAsync for real admission. Read IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes at runtime and in tests; do not repeat the ticket-default byte count as a test oracle. Unsupported serializer API is a blocking recon/Patron issue before implementation, not permission to invent a seam.
2. T002 is a hard stop before T003+: obtain the pre-change fixed scoring snapshot and measured retained-maps/envelope split plus realistic fivefold projection strictly below 80% effective ceiling. Missing proof blocks edits. Record unavailable evidence honestly, request recon or Patron ruling, retain 725/0.8 and all maps. Only a proposed ticket contradiction or constitution departure escalates to owner; no automatic structural escalation for a failed projection.
3. After loading the compact profile, query exactly that profile's LikedSources identity set using existing key equality. Missing/partial results retain missing-vector semantics; concurrent membership changes do not replace the selected key set, and no new transactional snapshot guarantee is claimed. Empty set skips embedding query, returns empty map. Nonempty set uses one batched query, propagates CancellationToken. Both handlers pass extended inputs; no cached source plot-vector member remains. Parity remains exact for the same fixed inputs.
4. Extend existing TasteProfileCacheOptions .Validate chain plus ValidateOnStart in PersistenceServiceCollectionExtensions. WarningFraction is finite and 0 < fraction <= 1; const default 0.8, no static readonly mutable initialization. Match existing SectionName:nameof(property) error-message style. Existing options/registration paths above cover zero, one, negative, above-one, NaN, positive and negative infinity.
5. Logging only: minimal HybridCachedRecommendationProfileSource.Log.cs partial and source-generated LoggerMessage; structured numeric PayloadBytes, BudgetBytes, Fraction only, no interpolation/content. On factory construction: observe/warn before admission; size alone never throws. Preserve incumbent swallowed rejection and return profile to coalesced callers; over-ceiling cannot masquerade as warm admission. Test below/equal/above real ceiling and warning boundary, observation/admission agreement, numeric-only logs and no per-hit serialization.
6. Version compact cache key namespace; legacy entries cannot be consumed. Keep generation/expiry/invalidation semantics. Seed old namespace in regression and prove rebuild under compact namespace without scoring old vectors.
7. T005a extends inputs and EF retrieval while retaining a compiling compatible path; T005b removes cached vectors and wires both consumers. Each boundary compiles and relevant tests are green. Pre-change parity stays green; edge-contract tests may begin red; TDD optional. Warm evidence explicitly records profile cache hit plus uncached embedding batch, or zero batch for empty identities, with query count/cost and vector volume.
8. Worktree mutation invocation is pwsh -NoProfile -File ./scripts/run-mutation.ps1 without --since; enforce configured maxKillShare/maxKillFileSpan and timeout safeguards, never lower thresholds. Three technology-agnostic checklist assertions are waived, not PASS. Ledger L4 redundant process prose is harmless follow-up, no cleanup or ticket.
