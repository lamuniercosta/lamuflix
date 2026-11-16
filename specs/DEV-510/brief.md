# DEV-510 - Shrink the cached taste profile

## Status and authority

Phase 2 grill closed with Patron after seven questions (cap: twelve). PLAN FROZEN — Keel, 2026-11-15, after Patron PA1 and independent reread; see PLAN_ADJUDICATION.md. Gate 1 remains pending; no implementation, gate execution, home measurement, promotion, or runtime acceptance is claimed.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-510-spec`.
Branch: `feature/510-spec`.
Recon baseline: `6edd0d16f0dd51950c56e21f2c866467dc5dff77`.

Authoritative ticket provenance: `recon-DEV-510` lines 51-67, recording `scripts/get-task.ps1 DEV-510` from this worktree. Implementation/test mapping: recon lines 27-37. Earlier recon statements that ticket text was unavailable are superseded by lines 51-67. Full grill exchanges, cited bases and implications are in [CONCLUSIONS.md](CONCLUSIONS.md); quantified acceptance interpretations are tagged `[assumed]` in [ASSUMPTIONS.md](ASSUMPTIONS.md).

Facts come from `recon-DEV-510`. A missing fact is `needs recon: <question>` to Bernstein, never an inferred implementation fact. This brief records decisions, not new measurements.

## Problem and frozen scope

The ticket reports that the cached taste profile exceeds HybridCache's default 1,048,576-byte payload ceiling, causing logged/swallowed cache write failures and repeated profile construction. Its estimates for 145 liked movies identify per-movie plot vectors and retained maps as major contributors; actual serialized totals have not been measured. `CachedProfile` wraps `TasteProfile`, `Generation` and `CreatedAt`.

Frozen scope is exactly ticket work items 1-7 and Done when criteria (recon lines 64-65), following the proposed design (59-60) and exclusions (66): measure actual home payload/component split and latency, confirm limit behavior without distributed cache, transfer per-source plot embeddings to uncached inputs with one additional batched query, prove exact score/reason parity, add large-profile/query regressions and payload observability, run required gates, and perform post-merge home/full-day acceptance. Anything else is a follow-up issue, not a finding in this round.

Explicit exclusions: scoring formula or weight changes; adding distributed cache; caching profile inputs or candidate embeddings. No payload-limit increase as the solution, truncation, source dropping, or silent data loss. The roughly 60 percent size reduction and roughly 1.3 KB/source growth are ticket estimates, not measured results or substitute acceptance evidence.

## Canonical terms and representation contract

- Cached taste profile: the existing cache aggregate, including the `CachedProfile` bookkeeping envelope. Keep aggregate/channel vectors, source identities, and source cast/genre/director maps.
- Liked-source plot embedding: a liked movie's 384-float plot vector. Remove this per-source vector from cached `LikedSources`; supply it through uncached request inputs keyed by the existing movie identity.
- Profile inputs: existing per-request inputs, including candidate embeddings and the transferred liked-source embeddings; they remain uncached.
- Exact parity: identical recommendation title/order, score and `Because you liked X` reason for the same recorded source/candidate inputs.

`GetProfileInputsAsync` retrieves liked-source embeddings with one additional batched query, shared across all candidate scoring in that request. No per-movie or per-candidate round trips. `BestLikedSource` / `PlotChannel` consume these inputs. Preserve existing missing/zero-vector behavior, tie ordering, non-plot channels and all scoring math. No new identity model, architecture or domain term is needed; no root glossary change or ADR is required.

## Authorized implementation file responsibilities

Targeted edits are authorized by Patron Q3/Q7 as required by ticket work items 2-5. This is a planning list, not a claim that files were changed.

| Existing file or declaration | Planned responsibility |
| --- | --- |
| `src/LamuFlix.Core/Domain/TasteProfile.cs` | Remove only per-source cached plot vectors; preserve all retained channels and source identity. |
| `src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs` | Construct the compact liked-source representation without changing aggregates/scoring data. |
| `src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs` and existing `GetProfileInputsAsync` return-type declaration | Extend internal request inputs with liked-source embeddings keyed by existing identity. |
| `src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs` | Populate transferred embeddings with one batched additional query. |
| Existing `ProfileSimilarityCalculator` declaration and `BestLikedSource` / `PlotChannel` consumers | Obtain per-source plot vectors from request inputs; preserve formula and selection behavior. |
| Existing `GetRecommendationsQueryHandler` and `GetForYouRecommendationsQueryHandler` consumers | Pass/use the extended inputs only where required; prove parity in both paths. |
| `src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs` | Compact cache value, serialized-size observations and cache admission/reuse regression support; preserve freshness semantics. |
| Existing `TasteProfileCacheOptions` and `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` | Single-source configurable warning fraction and effective-ceiling wiring if needed. |
| Existing suites listed below | Meaningful size, retrieval, query-count, scoring and cache regressions. |
| `specs/DEV-510/` | Spec, plan, tasks, runbook and evidence slots drafted by Quill from these decisions. |

Use existing declarations rather than invent guessed paths for declarations whose exact paths are not supplied in recon. Obtain any needed exact location through recon. No new package, project, architectural layer, database schema, public API, LocalPlay, secret-handling or process-launch change is approved. No wholesale rewrite or unrelated file edit. No generated API/TypeScript edit is planned because public contracts do not change.

## Task ordering and acceptance mapping

1. **Baseline before changing representation** (ticket item 1 and parity baseline in item 3): capture actual home Postgres serialized cache total and per-component split; establish how/where the limit is enforced without `IDistributedCache`; capture matched cold/uncached and warm-cache `for-you` timings; record fixed input/database snapshot and candidate set with pre-change title/order, exact score and reason. Preserve the baseline version and safe evidence references.
2. **Request input contract and retrieval regressions** (item 2 and retrieval portion of item 4): extend existing inputs and EF retrieval for one additional batched liked-source query. Add real Postgres retrieval/query-count coverage; keep inputs uncached.
3. **Compact profile and scoring integration** (items 2-3): remove only source plot vectors from cached representation and wire existing similarity/calculator/consumer paths to request embeddings. Keep all other maps/vectors and scoring behavior.
4. **Budget, observability and regressions** (items 4-5): implement the confirmed size/warning contract; prove real cache admission/reuse; add exact parity, property and Testcontainers cases plus existing cache regression coverage.
5. **Pre/post evidence and gates** (items 1,3,6 and Done when): repeat payload/component and matched latency measurements, demonstrate exact fixed-set title/order/score/reason parity, record all gate results with exact checked SHA. Do not substitute estimates for measured data.
6. **Post-merge operator validation** (item 7 and Done when): after user-owned merge, authorized operator promotes home and records a complete 24-hour observation and warm-cache latency. Remains `runtime-pending` until the receipt exists; no seat merges or implies deployment authorization.

Quill must keep every item above in spec/plan/tasks, including post-merge work. Baseline access or evidence failures must be reported; do not silently defer pre-change baseline until after the change or mark unavailable data as complete.

## Payload and observability acceptance

Measure actual serialized `CachedProfile` bytes with production-equivalent serializer/options, including `Generation` and `CreatedAt`. Record before/after total and component split. Confirm separately the effective HybridCache limit behavior without a registered distributed cache; do not assume the serialization measurement alone proves this behavior.

Retain the default 1,048,576-byte ceiling. Patron's confirmed `[assumed]` headroom interpretation requires a representative fivefold fixture of 725 liked movies relative to the ticket's 145 baseline, with realistic retained maps and 384-float per-source vectors supplied in uncached inputs. Both this compact serialized fixture and the current home profile must be strictly below 80 percent of the effective ceiling. Do not manufacture the result by thinning maps or source counts. Keep test/operational thresholds in their appropriate configuration source of truth, not scattered literals; never lower harness thresholds.

Emit structured serialized-payload-byte and effective-budget observations on cache construction, plus a warning when bytes reach or exceed a configurable warning fraction, default 0.8. Test below, equal-to and above the warning boundary and cache payload ceiling as applicable. Avoid repeated serialization on cache hits and prove warm reuse. Do not log vectors, profile content, identities, secrets or connection strings. Existing cache failure, expiry and invalidation behavior must remain covered.

## Test strategy

Use existing assertion/mocking libraries and test structure. Test database translation/serialization with Postgres Testcontainers, never a mocked driver. Do not add a test dependency.

Relevant existing suites from recon: `HybridCachedRecommendationProfileSourceCacheTests`, `HybridCachedRecommendationProfileSourceColdTests`, `HybridCachedRecommendationProfileSourceFreshnessTests`, `EfRecommendationProfileSourceTests`, `TasteProfileCalculatorTests`, `TasteProfileProperties`, `TasteProfileReviewAggregateTests`, and `TasteProfileCacheRegistrationTests`.

Required evidence/tests:

- Exact pre/post parity on a fixed, recorded input/database snapshot and candidate set: title/order, score and explanatory reason, unchanged formula/weights. No tolerance introduced to conceal a difference.
- Deterministic unit/property cases: multiple liked sources, missing/zero vectors, ties, empty/disliked-only profiles, and retained non-plot channels.
- Postgres retrieval of liked-source embeddings keyed correctly, plus query count bounded independently of source/candidate count (one additional batched query).
- Both recommendation consumers with cold/warm paths; retain freshness, invalidation/generation, expiration, concurrency/coalescing, cancellation, factory failure, scope-lifetime and uncached input regressions already described by recon.
- Real serialized envelope size/headroom, threshold boundaries and actual cache admission/reuse. A size-only assertion is not proof that cache hits work.

No sensitive home snapshot is committed. Record reproducible safe inputs/evidence references without secrets or profile-content logs.

## Gate expectations and loop discipline

Ticket-required build verification: Roslyn analyzers, cyclomatic complexity, InspectCode, full tests, and `dotnet format --verify-no-changes`, with no new warnings. Apply applicable pipeline property, vulnerability/security, mutation and web gates as well. Changed production C# requires the mutation gate; surviving mutants require tests, never a lowered threshold. Gate scripts run in the named worktree against the appropriate diff/base and checked SHA. No implementation gates are claimed by this docs-only grill.

Closing bar: every verified Critical, High, Medium and Low finding within frozen scope must be resolved or explicitly dispositioned with cited evidence. No accepted unresolved correctness or acceptance gap closes review. On M, obtain all three Sentry/Ledger/Compass axes at the review stage. Out-of-scope work follows the frozen-scope rule and applicable charter follow-up policy.

Round cap: maximum two review rounds and two remediation commits per round. Remaining above-bar findings after the cap go to user PR review; the cap does not grant clearance. Applicable failed/unrunnable gates block. Scope-empty exit-2 results are nonblocking `SKIPPED (scope-empty)`, never PASS; explicit harness opt-out is `SKIP`, mutation-only configured exclusions are `N/A`, and unavailable checks are `Could not run`. Property-test opt-out requires the pipeline's explicit documented basis; none is assumed here.

## Operational runbook and evidence slots for Quill

Draft a runbook/evidence template under `specs/DEV-510` containing:

- Baseline and post-change SHA, configuration/serializer/effective budget, observed profile/library count, actual serialized total/component bytes and calculated headroom.
- No-distributed-cache reproduction setup, actual observations and conclusion on limit enforcement, with safe log references.
- Fixed scoring snapshot/input and candidate-set provenance, compared versions, exact title/order/score/reason results and discrepancies if any.
- Matched `for-you` inputs/cache state, cold/uncached/warm classification, sample count, timing observations and summary statistics. Publish measurements; no invented speedup target.
- Synthetic fixture shape/count, serialized bytes, budget assertion, actual cache reuse evidence, bounded-query evidence and applicable gate receipts.
- Authorized operator promotion receipt after user merge; observation start/end/time zone spanning at least 24 hours, deployed SHA, exercised workload/profile count, `MaximumPayloadBytesExceeded` occurrence count (must be zero), and measured warm-cache latency.
- Separate status fields for planning/freeze, Gate 1, implementation verification, promotion and runtime acceptance; missing evidence remains pending.

No owner checkbox or constitution departure arose in this grill. Existing ticket scope is authoritative, not an unresolved owner question. Implementation/runtime facts remain unmeasured and cannot be inferred from this decision record.

## Grill answer index

| Question | Confirmed decision | Record |
| --- | --- | --- |
| Q1 | Full ticket scope and exclusions, including home work | CONCLUSIONS.md Q1 |
| Q2 | All-severity explicit dispositions, two rounds/two fix commits, honest gates | CONCLUSIONS.md Q2 |
| Q3 | Internal-only embedding transfer and targeted edit authorization | CONCLUSIONS.md Q3 |
| Q4 | Real envelope size, fivefold fixture, 80 percent budget/warning | CONCLUSIONS.md Q4; ASSUMPTIONS.md A1-A2 |
| Q5 | Exact parity and real-database/query/cache regression coverage | CONCLUSIONS.md Q5 |
| Q6 | Home measurements, current-home budget, structured observations and full-day receipt | CONCLUSIONS.md Q6; ASSUMPTIONS.md A3 |
| Q7 | Complete approach, file responsibility, task ordering and shared understanding | CONCLUSIONS.md Q7 |

No additional decision is implied. Quill raises any gap as `needs decision:`; Keel records a decision change here before any derivative artifact is changed.

## PA1 - Plan challenge decisions (2026-11-15)

Patron explicitly approved all 20 finding recommendations without amendment. Full proposal and cited answer are recorded in CONCLUSIONS.md PA1. This amendment precedes derivative artifact edits and governs conflicting draft wording above. Authority: original recon-DEV-510 ticket section lines 59-66, targeted implementation seams lines 3-7, Q1-Q7, Charter 2.3 and AGENTS.md. The targeted recon note now contains seven lines; original ticket/recon line references refer to the previously read original snapshot, not the replacement note numbering.

- **Baseline stop**: T002 must capture a reproducible pre-change fixed scoring snapshot and actual component split, and verify retained maps/envelope plus representative 725-source projection fit strictly below 80 percent of the effective ceiling before T003+. Missing parity baseline or unavailable/infeasible feasibility proof blocks implementation. Record pending/Could not run and stop for recon or a cited Patron disposition; pending alone does not unlock representation edits. Keep 725 and 0.8 frozen. Do not thin maps, raise the ceiling or silently relax acceptance. Only a proposed ticket contradiction or constitution departure becomes an owner structural checkbox; a failed projection alone goes to Patron.
- **Request join**: Load profile first; use exactly its LikedSources identities, with existing identity equality, as the additional embedding batch key set. Do not independently reload current liked membership. Missing/partial rows use incumbent missing-vector behavior; never invent vectors. Empty identities return an empty map with zero additional embedding queries; nonempty identities use one additional batched query, independent of candidate/source count. Propagate request CancellationToken. Concurrent membership changes do not change the chosen key set; no cross-read transaction/snapshot consistency guarantee is added. Exact parity is for identical fixed inputs. Both consumers pass inputs, and the compact cached source type exposes no plot vector.
- **Byte observation**: Use the same registered HybridCache serializer/options on the same CachedProfile envelope for construction-only byte measurement. The 2026-11-16 PA1 amendment below replaces internal serializer inspection and the blanket custom-wrapper prohibition with an explicitly shared serializer registration inside the already-authorized persistence files; no new measurement layer. Keep real GetOrCreateAsync admission. Resolve effective ceiling from IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes, never a repeated 1048576 literal. Ticket 1 MiB is a reported default, not a measured runtime value. T001 verifies the pinned package API/signature, accessibility and shared serializer resolution; unsupported seam blocks implementation for recon/Patron, never substitute independent JSON defaults. Test observation/admission agreement below/equal/above the actual ceiling using the confirmed no-distributed-cache behavior.
- **Options and diagnostics**: Authorize src/LamuFlix.Infrastructure/Persistence/TasteProfileCacheOptions.cs, tests/LamuFlix.UnitTests/Options/TasteProfileCacheOptionsTests.cs, tests/LamuFlix.UnitTests/Persistence/TasteProfileCacheRegistrationTests.cs and a minimal LoggerMessage partial beside the cache adapter. Extend the existing registration .Validate chain and ValidateOnStart, no new validator type. WarningFraction must be finite and satisfy 0 < fraction <= 1; default 0.8 is const, never static readonly. Match the existing SectionName:nameof(property) validation-message pattern. Test zero, one, negative, greater than one, NaN and both infinities. Choose logging only: source-generated LoggerMessage with numeric PayloadBytes, BudgetBytes, Fraction, no interpolation or content/identity logs; assert log fields are numeric only.
- **Cache behavior**: Version the compact cache key namespace; legacy-shaped keys cannot be reused, while existing generation/expiry/invalidation semantics remain. Test seeded legacy key ignored and compact value rebuilt. Construct, observe/warn, then admit or retain incumbent swallowed non-admission semantics and return profile. Size alone never throws to callers, including coalesced waiters. Over-ceiling cases warn before admission, return successfully and do not become false warm hits.
- **Cost and ordering**: Label warm measurements as profile-cache-hit plus uncached embedding batch (or zero batch for empty sources); record extra query count/cost and returned vector volume without logging content. Split T005 into T005a input-contract/EF retrieval and T005b compact scoring/both-consumer wiring with compiling green boundaries. Parity tests written pre-change stay green; edge-contract tests may start red. TDD is optional; do not manufacture red parity tests. Mutation runs use the documented worktree invocation without --since, preserving maxKillShare and maxKillFileSpan rules.
- **Documentation**: Waive the three implementation-free/technology-agnostic checklist claims explicitly for this ticket. Ledger L4 redundant process wording is noted as harmless follow-up, with no ticket or cleanup. Round cap and closing bar remain Q2. This is a plan disposition, not proof that implementation tests, payload budgets or runtime acceptance have passed.

## PA1 amendment - Explicit shared serializer (Patron, 2026-11-16)

Authority: CONCLUSIONS.md, T001 Measurement Seam Design Disposition, committed by Patron at fca46e8c; full cited ruling supplied at C:/Users/lamun/AppData/Local/Temp/patron-dev-510-t001-disposition.txt. This decision is recorded here before derivative artifact changes. Recon established no supported internal serializer accessor within the authorized roots; external package artifacts remain NOT AVAILABLE, not independently inspected by Keel.

Replace the requirement to inspect HybridCache's internally selected serializer/options with explicit shared IHybridCacheSerializer<CachedProfile> registration and adapter injection. Confine implementation to already-authorized PersistenceServiceCollectionExtensions.cs and HybridCachedRecommendationProfileSource.cs. Patron permits a thin internal serializer implementation there and an internal CachedProfile envelope if needed for registration accessibility, without a new package, project or architectural layer. The exact AddSerializer overload, DI registration and accessibility remain verification requirements, not established API facts or implementation instructions inferred from an example signature.

HybridCache admission and construction-only byte measurement must invoke the same registered singleton serializer with the same options and actual CachedProfile(Profile, Generation, CreatedAt) envelope. No reflection, independently assumed JSON defaults or replacement cache admission path. Keep GetOrCreateAsync admission/reuse, observations before admission, runtime MaximumPayloadBytes, separate no-IDistributedCache behavior proof, exact fixed-input scoring parity, and the frozen 725-source / 0.8 budgets. Registration/type-check evidence alone is not a claim that runtime byte/admission agreement tests have passed.

The subsequent serializer-sequencing ruling below moves compiler and runtime setup verification into T001b, preserving read-only T001 and placing the live measurement seam before T002. Derivative synchronization and independent reread remain mandatory before dispatch. Keel makes no production code edits. T002 baseline and feasibility evidence remains mandatory before T003+; unavailable home evidence does not authorize representation edits. This ruling approves no ticket contradiction or constitution departure and creates no owner escalation.

## PA1 amendment - Serializer setup before baseline (Patron, 2026-11-16)

Authority: Patron serializer-sequencing ruling, committed in CONCLUSIONS.md at 85feb708, full cited ruling at C:/Users/lamun/AppData/Local/Temp/patron-dev-510-sequencing-ruling.txt. Recorded first here before derivative synchronization. This supersedes conflicting setup ordering in the preceding amendment; it does not waive measurement or baseline requirements.

Execution order is T001 -> T001b -> T002 -> T003/T004 -> T005a -> T005b -> T006 -> T007/T008/T009 under their existing dependencies. T001 remains strictly read-only pickup recon/reanalysis/Gauge baseline. New T001b installs and verifies only the shared serializer seam over the unchanged incumbent TasteProfile/LikedSources representation before T002. Split that setup out of T008; it is not deferred until after the baseline or representation change.

T001b file boundary: src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs, src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs, tests/LamuFlix.UnitTests/Persistence/TasteProfileCacheRegistrationTests.cs, and existing HybridCache suites identified by T001 recon. Internal envelope accessibility is permitted if needed. No edits to TasteProfile.cs, TasteProfileCalculator.cs, IRecommendationProfileSource.cs, EfRecommendationProfileSource.cs, scoring math, profile inputs or cache-key namespace in T001b. Retain actual envelope fields and incumbent shape; declaration signatures are verified by compiler/recon, not assumed from prose examples.

T001b must provide both passing compiler/type-check evidence for the supported pinned-package registration and accessibility and a passing runtime test proving real GetOrCreateAsync admission and exact-envelope byte measurement invoke the same registered singleton serializer/options. Compilation alone cannot close T001b; no assumed serializer invocation in the no-IDistributedCache path. Any unavailable or unsupported proof remains blocked for recon/Patron disposition.

T002 then measures the unchanged pre-representation profile through the live T001b seam. EVIDENCE.md records both original pickup SHA and serializer-seam SHA plus serializer/configuration provenance, total/component bytes, separate no-IDistributedCache limit behavior, matched cold/warm timings, exact fixed scoring snapshot and the retained-content 725-source / strict-80-percent feasibility proof. Home access or evidence remaining pending/Could not run never unlocks T003+.

The shared serializer remains installed when later tasks compact the profile. T007/T008 retain compact-profile runtime byte/admission and warning/ceiling boundary tests, real warm reuse, finite WarningFraction validation with const 0.8 and ValidateOnStart, and structured LoggerMessage observations/warnings; they do not reinstall the serializer. Keep construction-only measurement, runtime ceiling resolution, no reflection or independently assumed JSON defaults, no new package/project/layer, and all exact-parity and 725/0.8 invariants.

T001, T001b and T002 remain held until derivative plan.md, tasks.md, spec.md, EVIDENCE.md and PLAN_ADJUDICATION.md are synchronized and independently reread. After that review, T001 status reflects actual read-only evidence; T001b requires its own compilation and runtime receipts before T002 dispatch. Adding T001b changes the task inventory from 16 to 17 while preserving 15 FR plus 7 SC coverage. No owner escalation is approved or inferred.

## PA1/Q4 amendment - Measured two-tier feasibility budget (Patron, 2026-11-16)

Authority: CONCLUSIONS.md T002 Feasibility Checkpoint Disposition and ASSUMPTIONS.md A1, committed by Patron at 4abc7c53f454cb94c6b368735d382b7f4b33a8f9; existing T002 measurements in EVIDENCE.md section 1c at d1809a46. Recorded FIRST in this brief before derivative repairs. This amendment supersedes every earlier active requirement in this brief for a 725-source / strict-80-percent feasibility gate, including Q4, PA1 baseline stop, and the serializer amendments. Those original decisions remain historical provenance, not current acceptance criteria.

Amended A1 uses realistic retained maps, the actual CachedProfile(Profile, Generation, CreatedAt) envelope, and the shared registered serializer/options against the runtime IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes ceiling:

- The 145-liked-source ticket-baseline fixture and the current measured 4-liked-source home profile must each serialize strictly below 0.8 times the effective ceiling. Existing feasibility evidence: 145 sources = 465,340 B (44.4% of the reported 1,048,576 B default); live home = 41,908 B pre-change and approximately 23,090 B compact. The ticket baseline is not the current live-home source count.
- The 300-liked-source realistic fixture (2.07 times the 145-source ticket baseline; 75 times live home) must serialize strictly below the effective ceiling and within [0.8 times ceiling, ceiling), exercising the near-limit warning band. Existing feasibility evidence: 940,943 B (89.7%). Later T007/T009 tests must prove real HybridCache admission/reuse without MaximumPayloadBytesExceeded and pre-ceiling warning signaling at this scale; measured feasibility does not claim those future tests passed.

Preserve full Liked.Cast and per-source cast/genre/director maps; no thinning, truncation, source capping, scoring change, or raised ceiling. Keep 384-float source embeddings in uncached inputs, WarningFraction const default 0.8, finite 0 < fraction <= 1 validation and ValidateOnStart, construction-only shared-singleton envelope measurement, exact fixed-snapshot parity (digest 9b66ab6e…e259a2), and all existing task dependencies and gates. Budgets use effective runtime options, not new hardcoded production thresholds. The 838,860 B figure in the ruling is the displayed integer approximation of 80% of the default; tests must evaluate the strict fraction against the actual ceiling.

The original 725-source projection remains FAILED as historical measured evidence (155–220% of ceiling); it is superseded as the active acceptance gate, never relabeled as passing. No repeated T002 measurement run is required by Patron. This is a deliberate amendment of Patron's assumed headroom operationalization, with no ticket contradiction, constitution departure, or owner structural checkbox. Additional retained-map compaction above 300+ sources is out of frozen scope: noted, no ticket.

Repair synchronization required before dispatch: Quill aligns spec.md (US3, FR-006, SC-002, assumptions and PA1), plan.md, tasks.md (T002 independent test/checkpoint/status, T007 and all active dependency/MVP references), EVIDENCE.md (status board, section 1c, section 5 and PA1 table), PLAN_ADJUDICATION.md (Sentry F2 / Compass F1 and current release wording), and stale current-home wording in ASSUMPTIONS.md A3. Preserve historical findings, original failed projections, baseline provenance and measurement limits. Do not turn compact feasibility measurements into final post-implementation, cache-warning/reuse, gate, or runtime receipts.

T002 feasibility checkpoint may be labeled PASSED under amended A1 on the existing evidence after synchronization, citing Patron 4abc7c53 and measurement d1809a46. T003+ unlocks only upon Keel's independent reread of the repaired artifacts confirming consistent budgets, preserved baseline/parity evidence, and no remaining active 725-source hold. Until that reread, dispatch remains held for documentation synchronization. T007/T009, implementation verification, T010 post-change measurements, user-owned delivery merge, authorized home promotion and the complete 24-hour runtime receipt remain separate and pending. Keel makes no production changes.
