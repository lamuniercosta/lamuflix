# Feature Specification: Shrink the cached taste profile

**Feature Branch**: `feature/510-spec`
**Created**: 2026-11-15
**Status**: gate1: provisional — PLAN FROZEN (Keel, 2026-11-15; see PLAN_ADJUDICATION.md). Owner Gate 1 merge, implementation verification, home measurement/promotion, and runtime acceptance remain open; runtime-pending.
**Input**: Ticket DEV-510 (parent DEV-491; size M); grill-closed `brief.md` (Q1–Q7); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-510`, `recon-DEV-510` (authoritative ticket at lines 51–67; implementation/test mapping at 27–37; HEAD `6edd0d16f0dd51950c56e21f2c866467dc5dff77`); `specs/PRODUCT.md`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Operational runbook / Grill answer index; Q1–Q7; `PRODUCT.md` §5). Ordinary owner Gate 1 on the spec PR remains pending after Bernstein coordination / Rigger preparation-opening of that PR and verified user merge. Patron approval is not owner approval. Only Keel declares PLAN FROZEN after independent analyze/reread. Quill does not invent PLAN FROZEN or a structural checkbox.

## Clarifications

### Session 2026-11-15

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope (ticket work items 1–7 and Done when), embedding-transfer representation boundary, exact title/order/score/reason parity, fivefold 725-source fixture and 0.8 warning/budget headroom `[assumed]`, home measurement and post-merge 24-hour runtime acceptance, authorized targeted edit list, task ordering, test strategy, and gate/loop discipline are frozen in brief Q1–Q7. Actual serialized bytes, effective HybridCache limit enforcement without `IDistributedCache`, and live latency remain unmeasured Phase B evidence — not inventable here. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## Scope

**In scope** (`brief.md` Problem and frozen scope / Task ordering; Q1–Q7; ticket work and Done when at `recon-DEV-510` lines 59–66):

1. **Baseline before representation change** (ticket item 1; parity baseline in item 3): After read-only pickup (T001) and serializer-only setup (T001b) that installs the shared `IHybridCacheSerializer<CachedProfile>` singleton over the unchanged incumbent representation, measure actual home Postgres serialized `CachedProfile` total and per-component split through that live registered singleton / options on the exact envelope including `Generation` and `CreatedAt` (brief.md PA1 amendment Explicit shared serializer; PA1 amendment Serializer setup before baseline at 85feb708). Record original pickup SHA and T001b seam SHA/configuration/serializer provenance. Separately confirm how/where the HybridCache default 1,048,576-byte limit is enforced without a registered `IDistributedCache` (do not assume serializer invocation proves that path). Capture matched cold/uncached and warm-cache `for-you` timings. Record a fixed input/database snapshot and candidate set with pre-change title/order, exact score, and `Because you liked X` reason. Preserve baseline version and safe evidence references. Do not silently defer pre-change baseline until after the change; do not mark unavailable data complete; unavailable home evidence remains pending/Could not run.
2. **Compact cached taste profile** (items 2–3): Remove only per-liked-source plot embeddings (384-float vectors) from cached `LikedSources`. Retain source identities, cast/genre/director maps, and aggregate/channel vectors. No truncation, source dropping, silent data loss, or payload-limit increase as the solution.
3. **Uncached liked-source embeddings** (item 2): Extend existing uncached profile inputs so `GetProfileInputsAsync` returns liked-source plot embeddings keyed by the existing movie identity, populated with one additional batched query shared across all candidate scoring in that request (no per-movie or per-candidate round trips). Profile inputs and candidate embeddings remain uncached. `BestLikedSource` / `PlotChannel` consume these inputs. Preserve existing missing/zero-vector behaviour, tie ordering, non-plot channels, and all scoring math.
4. **Exact parity** (item 3; Done when): For the same recorded source/candidate inputs, post-change recommendation title/order, exact score, and `Because you liked X` reason match the pre-change baseline. No formula or weight changes. No tolerance introduced to conceal a difference.
5. **Payload budget and observability** (items 4–5): Retain the default 1,048,576-byte ceiling. Prove a representative fivefold synthetic fixture (725 liked movies vs ticket baseline 145) with realistic retained maps and 384-float per-source vectors in uncached inputs serializes strictly below 80 percent of the effective ceiling; current home profile must also serialize strictly below that 80 percent budget (`ASSUMPTIONS.md` A1–A3). Emit structured serialized-payload-byte and effective-budget observations on cache construction, plus a warning at or above a configurable warning fraction defaulting to 0.8. Boundary-test below/equal/above warning and ceiling as applicable. Avoid repeated serialization on cache hits; prove warm reuse and actual cache admission. Do not log vectors, profile content, identities, secrets, or connection strings. Preserve existing cache failure, expiry, invalidation, concurrency/coalescing, cancellation, factory-failure, scope-lifetime, and uncached-input regressions.
6. **Gates and post-change evidence** (items 1, 3, 6 and Done when): Repeat payload/component and matched latency measurements after the change. Record all required gate results with exact checked SHA. Ticket-required: Roslyn analyzers, cyclomatic complexity, InspectCode, full tests, `dotnet format --verify-no-changes`, no new warnings. Apply applicable pipeline property, vulnerability/security, mutation, and web gates. Surviving mutants require tests, never a lowered threshold. Honest SKIPPED (scope-empty) / SKIP / N/A / Could not run labels.
7. **Post-merge operator validation** (item 7 and Done when): After user-owned merge, authorized operator promotes home and records a complete ≥24-hour observation (start/end/time zone), deployed SHA, exercised workload/profile count, zero `MaximumPayloadBytesExceeded` entries, and measured warm-cache latency. Remains `runtime-pending` until the receipt exists. No seat merges or implies deployment authorization.
8. **Documentation**: Spec, plan, tasks, and `EVIDENCE.md` runbook/evidence slots under `specs/DEV-510/` (this draft). Closing bar: every verified Critical/High/Medium/Low finding within frozen scope resolved or explicitly dispositioned with cited evidence; max two review rounds and two remediation commits per round; unresolved above-bar findings after the cap go to user PR review (not auto-cleared). On size M, obtain all three Sentry/Ledger/Compass axes at review. Frozen scope ends: anything else is a follow-up issue, not a finding in this round.

**Out of scope**:

- Changing scoring formula or weights.
- Adding a distributed cache.
- Caching profile inputs or candidate embeddings.
- Raising `HybridCacheOptions.MaximumPayloadBytes` (or equivalent) as the solution.
- Truncation, source dropping, or silent data loss to meet size targets.
- New NuGet/npm dependency, project, top-level folder, architectural layer, database schema, public API shape, `Features:LocalPlay`, secrets, or `Process.Start` change.
- Wholesale rewrite or unrelated file edits; generated OpenAPI/TypeScript edits (public contracts do not change).
- New domain term, root glossary/`CONTEXT.md` change, or ADR.
- Inventing measured byte counts, latency speedups, or runtime success from ticket estimates (~60% reduction / ~1.3 KB/source growth are estimates only).
- Claiming Gate 1 clearance, implementation, home promotion, or runtime PASS from the planning freeze.

**Precedence**: Ticket Scope & Technical Design and acceptance criteria (`recon-DEV-510` lines 59–66) are already approved authority (`PRODUCT.md` §3). Brief Q1–Q7 and `CONCLUSIONS.md` govern plan boundaries. Ordinary owner Gate 1 remains pending. Only Keel declares PLAN FROZEN after independent analyze/reread.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md or ADR expansion)*

**Cached taste profile**
The existing HybridCache aggregate for recommendation taste, including the `CachedProfile` bookkeeping envelope (`TasteProfile`, `Generation`, `CreatedAt`). After this ticket it still holds aggregate/channel vectors, source identities, and source cast/genre/director maps, but not per-liked-source plot embeddings.
_Avoid_: treating profile inputs or candidate embeddings as cached; raising the payload ceiling as the fix

**Liked-source plot embedding**
A liked movie's 384-float plot vector formerly retained on each cached `LikedSources` entry so `BestLikedSource` / `PlotChannel` can score and explain “Because you liked X.” Removed from the cache value; supplied per request through uncached profile inputs keyed by the existing movie identity.
_Avoid_: new identity model; per-candidate round trips; changing non-plot channels

**Profile inputs**
Existing per-request recommendation inputs (including candidate embeddings and the transferred liked-source embeddings). Remain uncached.
_Avoid_: caching inputs; storing them inside `CachedProfile`

**Exact parity**
Identical recommendation title/order, exact score, and `Because you liked X` reason for the same recorded source/candidate inputs and unchanged formula/weights.
_Avoid_: approximate scores; title-only comparison; introduced tolerance

**Effective payload ceiling**
The value of IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes, with no ceiling raise; the ticket reports a 1,048,576-byte default. Read the actual configured value for headroom/warning assertions and confirm real limit behaviour without a registered `IDistributedCache`.
_Avoid_: assuming serialization measurement alone proves limit enforcement; raising the ceiling

Consumed vocabulary (already in use; not redefined): TasteProfile, LikedSources, HybridCache, for-you recommendations, `GetProfileInputsAsync`, `BestLikedSource`, `PlotChannel`.

## What the User Will See *(mandatory)*

Nothing visible: this ticket changes only the internal cached taste-profile representation and uncached request inputs. Recommendation title/order, scores, and “Because you liked X” reasons stay identical for the same inputs; no new UI chrome, routes, or DTO fields.

(No Smoke Test subsection — nothing user-visible changes on the Compose SPA surface.)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pre-change baseline evidence (Priority: P1)

Operators and implementers capture measured home payload/component bytes, no-distributed-cache limit behaviour, matched for-you latency, and a fixed scoring snapshot before any representation change.

**Why this priority**: Ticket item 1 and the parity baseline in item 3 must precede the compact representation; unavailable baseline cannot be marked complete or deferred silently.

**Independent Test**: `EVIDENCE.md` baseline slots filled (or explicitly pending/Could not run) with safe references, SHA, serializer/budget notes, component split, limit-behaviour conclusion, latency classification, and fixed-set title/order/score/reason — without requiring the compact representation to be finished.

**Acceptance Scenarios**:

1. **Given** home Postgres with the current liked-movie profile and a live T001b shared singleton `IHybridCacheSerializer<CachedProfile>` seam over the unchanged incumbent representation, **When** the baseline measurement runs with that same registered singleton / options used for admission on the exact `CachedProfile` envelope, **Then** total and per-component serialized bytes (including envelope fields) are recorded with original pickup SHA, seam SHA, serializer/configuration provenance, and safe evidence references.
2. **Given** a reproduction without registered `IDistributedCache`, **When** limit behaviour is exercised, **Then** whether/where the 1,048,576-byte rejection occurs is recorded separately from the serialization measurement.
3. **Given** matched for-you inputs, **When** cold/uncached and warm-cache timings are captured, **Then** cache-state classification, sample count, elapsed observations, and summary statistics are recorded without an invented speedup target.
4. **Given** a fixed recorded input/database snapshot and candidate set, **When** recommendations are produced on the pre-change build, **Then** title/order, exact score, and reason are preserved as the parity baseline.

---

### User Story 2 - Compact profile with embedding transfer and exact parity (Priority: P1) 🎯 MVP

The cached profile no longer stores per-liked-source plot vectors; those vectors arrive once per request via uncached inputs from one batched query; recommendation results match the baseline exactly.

**Why this priority**: Core ticket design and Done-when parity (`recon-DEV-510` lines 59–65).

**Independent Test**: Unit/property and Testcontainers coverage for compact representation, batched liked-source retrieval/query-count bound, and exact fixed-set parity on both recommendation consumers — without requiring the fivefold fixture or home post-merge day to be finished.

**Acceptance Scenarios**:

1. **Given** a taste profile with liked sources, **When** the compact cached value is built, **Then** per-source plot embeddings are absent while identities, cast/genre/director maps, and aggregate/channel vectors remain.
2. **Given** a recommendation request, **When** profile inputs are loaded, **Then** liked-source embeddings are present keyed by existing movie identity from one additional batched query shared across candidate scoring (query count independent of source/candidate count).
3. **Given** the fixed baseline snapshot and candidate set, **When** both recommendation consumers run post-change with identical inputs, **Then** title/order, exact score, and `Because you liked X` reason match the baseline with unchanged formula/weights.
4. **Given** missing/zero vectors, ties, empty/disliked-only profiles, or multiple liked sources, **When** scoring runs, **Then** existing selection and non-plot channel behaviour is preserved.

---

### User Story 3 - Payload budget, warning, and cache regressions (Priority: P2)

Synthetic fivefold and current-home budgets stay under 80 percent of the effective ceiling; structured size observations and configurable warning fire correctly; cache admission/reuse and prior cache regressions remain green.

**Why this priority**: Ticket items 4–5 operationalize headroom and near-limit signaling after the compact representation exists.

**Independent Test**: Fixture serialization under budget; boundary tests for warning fraction; real cache admission/reuse proof; retained freshness/invalidation/cancellation/factory-failure regressions — without requiring the 24-hour home observation.

**Acceptance Scenarios**:

1. **Given** a representative 725-liked-source fixture with realistic retained maps (384-float vectors in uncached inputs only), **When** the compact `CachedProfile` is serialized with production-equivalent options, **Then** bytes are strictly below 80 percent of the effective 1,048,576-byte ceiling without thinning maps or source counts.
2. **Given** cache construction at below, equal-to, and above the configured warning fraction (default 0.8), **When** observations are emitted, **Then** structured payload-byte and effective-budget signals behave as specified and the warning fires at/above the fraction without logging vectors/identities/secrets.
3. **Given** a warm cache entry under the ceiling, **When** the same profile is requested again, **Then** the entry is reused without re-serializing the payload on every hit.
4. **Given** existing cache suites, **When** the compact representation lands, **Then** freshness, invalidation/generation, expiration, concurrency/coalescing, cancellation, factory failure, scope-lifetime, and uncached-input regressions remain covered.

---

### User Story 4 - Post-change evidence and required gates (Priority: P2)

After the change, payload/latency/parity evidence is refreshed and mandatory gates report honest verdicts at the checked SHA.

**Why this priority**: Ticket items 1, 3, 6 and Done-when gate obligations.

**Independent Test**: `EVIDENCE.md` post-change slots and gate receipt at an exact SHA, or explicit Could not run / open items — never folding non-pass into PASS.

**Acceptance Scenarios**:

1. **Given** the compact representation on a recorded SHA, **When** home payload/component and matched latency measurements repeat, **Then** before/after totals, headroom, and latency statistics are published (current home still strictly below the 80 percent budget).
2. **Given** the fixed scoring snapshot, **When** post-change parity is compared, **Then** exact title/order/score/reason match is evidenced or discrepancies are recorded as failures.
3. **Given** production C# changes under `src/`, **When** required gates run, **Then** Roslyn, complexity, InspectCode, full tests, format, and applicable property/security/mutation/web gates report distinct PASS / SKIPPED (scope-empty) / SKIP / N/A / Could not run / FAIL verdicts with no new warnings and no lowered thresholds.

---

### User Story 5 - Post-merge home runtime acceptance (Priority: P3)

After the user merges the delivery PR, an authorized operator promotes home and records a full-day zero-exceedance receipt with warm-cache latency.

**Why this priority**: Ticket item 7 and Done when; cannot close from build-time tests alone.

**Independent Test**: Operator promotion + ≥24-hour observation receipt in `EVIDENCE.md`, or status remains `runtime-pending` — never implied by Gate 1 or green gates.

**Acceptance Scenarios**:

1. **Given** user-owned merge of the delivery PR, **When** an authorized operator promotes home, **Then** a promotion receipt records deployed SHA and authorization without any seat claiming merge/deploy authority.
2. **Given** the promoted home build, **When** observation spans at least 24 hours with recorded start/end/time zone and workload/profile count, **Then** `MaximumPayloadBytesExceeded` occurrence count is zero and warm-cache latency is measured and recorded.

### Edge Cases

- Missing or zero liked-source plot vectors after transfer (preserve incumbent behaviour).
- Ties among liked sources for `BestLikedSource` / reason selection.
- Empty or disliked-only profiles; retained non-plot channels only.
- Cache payload at warning boundary (equal to configured fraction) and at/above effective ceiling.
- Warm-cache hit must not re-run expensive serialization or lose freshness/generation semantics.
- Home baseline or no-distributed-cache reproduction unavailable → record pending/Could not run; do not invent bytes or claim complete.
- Sensitive home snapshot content must not be committed; log only safe references.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST remove only per-liked-source plot embeddings from the cached taste profile while retaining source identities, cast/genre/director maps, and aggregate/channel vectors.
- **FR-002**: System MUST supply liked-source plot embeddings through uncached profile inputs keyed by existing movie identity via one additional batched query per nonempty source set, shared across candidate scoring; empty source sets require no additional embedding query. Use exactly the active cached profile identities and existing key equality, propagate cancellation, and preserve missing-vector semantics for absent rows.
- **FR-003**: System MUST keep profile inputs and candidate embeddings uncached; MUST NOT add a distributed cache or raise the HybridCache payload ceiling as the solution.
- **FR-004**: For a fixed recorded input/database snapshot and candidate set, recommendation title/order, exact score, and `Because you liked X` reason MUST match pre-change results with unchanged formula/weights (exact equality; no concealing tolerance).
- **FR-005**: System MUST preserve existing missing/zero-vector, tie, empty/disliked-only, and non-plot channel scoring behaviour on both recommendation consumers.
- **FR-006**: Serialized compact `CachedProfile` (including `Generation` and `CreatedAt`) for the representative 725-liked-source fixture MUST be strictly below 80 percent of the effective 1,048,576-byte ceiling without thinning maps or source counts.
- **FR-007**: Current home serialized profile MUST also be strictly below the same 80 percent budget after measurement (`ASSUMPTIONS.md` A3).
- **FR-008**: On cache construction, system MUST emit structured serialized-payload-byte and effective-budget observations using the same registered singleton `IHybridCacheSerializer<CachedProfile>` / options later used for HybridCache admission on the exact `CachedProfile(Profile, Generation, CreatedAt)` envelope, and MUST warn at or above a configurable warning fraction defaulting to 0.8, with boundary coverage below/equal/above as applicable. Compilation of serializer registration alone is not runtime proof of observation/admission agreement.
- **FR-009**: System MUST prove actual `GetOrCreateAsync` cache admission/reuse for payloads under the ceiling and MUST NOT repeatedly serialize the payload on every cache hit; T001b requires both compile accessibility and runtime same-singleton exact-envelope/`GetOrCreateAsync` admission proof before baseline, and compact-profile reuse proofs in T007/T008 remain distinct from compilation alone.
- **FR-010**: System MUST NOT log vectors, profile content, identities, secrets, or connection strings in size/observability output.
- **FR-011**: Pre-change baseline (payload/component, no-distributed-cache limit behaviour, matched latency, fixed scoring snapshot) MUST be captured before representation change, or left explicitly pending/Could not run — never silently deferred or fabricated. Missing pre-change parity snapshot or retained-content fivefold feasibility proof blocks representation edits; pending status alone does not authorize proceeding (brief PA1).
- **FR-012**: Post-change measurements and exact parity comparison MUST be recorded against the same safe evidence conventions.
- **FR-013**: After user merge, authorized home promotion and ≥24-hour observation with zero `MaximumPayloadBytesExceeded` and measured warm-cache latency MUST be tracked as `runtime-pending` until the operator receipt exists.
- **FR-014**: Public API routes, DTO field sets, and status codes MUST remain unchanged; no generated OpenAPI/TypeScript edit is required.
- **FR-015**: Required analyzers, tests, format, and applicable pipeline gates MUST pass or report honest non-pass labels; surviving mutants require tests; harness thresholds MUST NOT be lowered.

### Key Entities

- **CachedProfile**: Cache envelope holding `TasteProfile`, `Generation`, and `CreatedAt`.
- **TasteProfile / LikedSources**: Aggregate preferences and liked-source entries; plot embeddings leave the cached liked-source shape.
- **Profile inputs**: Per-request uncached inputs carrying candidate embeddings and liked-source embeddings.
- **Evidence record**: Baseline/post-change/runtime slots in `specs/DEV-510/EVIDENCE.md` (pending until filled).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Pre-change and post-change serialized `CachedProfile` totals and component splits are measured and recorded (or explicitly pending/Could not run) — ticket estimates are not substituted for measurements.
- **SC-002**: Compact fivefold fixture and current home profile each serialize strictly below 80 percent of the effective 1,048,576-byte ceiling.
- **SC-003**: Fixed-set recommendation title/order, exact score, and reason match pre-change baseline exactly.
- **SC-004**: Liked-source embedding retrieval adds one batched query for nonempty active-profile source sets and zero for empty sets (query count independent of source/candidate count).
- **SC-005**: Structured size/budget observations and configurable warning behave correctly at below/equal/above boundaries using the shared singleton serializer/options on the exact envelope; runtime observation/admission agreement, real `GetOrCreateAsync` warm reuse without per-hit re-serialization, and separate no-`IDistributedCache` limit behaviour are proven (compilation alone is not that proof).
- **SC-006**: Required gates report distinct honest verdicts at the checked SHA with no new warnings and no lowered thresholds.
- **SC-007**: Post-merge home observation spanning ≥24 hours records zero `MaximumPayloadBytesExceeded` entries and measured warm-cache latency, or delivery remains `runtime-pending`.

## Assumptions

- Quantified headroom uses a fivefold fixture of 725 liked movies relative to the ticket's 145 baseline, with representative retained maps and 384-float vectors in uncached inputs (`ASSUMPTIONS.md` A1; brief Q4).
- Configurable warning fraction defaults to 0.8 against the effective ceiling (`ASSUMPTIONS.md` A2).
- Current home 145-liked-movie profile must also fall strictly below the 80 percent budget once measured (`ASSUMPTIONS.md` A3).
- ~60 percent size reduction and ~1.3 KB/source growth in the ticket are estimates only, not acceptance evidence.
- Exact repository paths for declarations named without paths in `recon-DEV-510` (for example ProfileSimilarityCalculator / BestLikedSource / PlotChannel / some handler and test file paths) are obtained via recon at pickup rather than invented in this draft.
- No structural escalations arose in the grill; subsequent warning-domain/logging decisions are recorded in brief PA1 and ASSUMPTIONS A4.

## PA1 edge and configuration contracts

Patron ruling recorded in brief.md PA1 / CONCLUSIONS.md PA1, plus the 2026-11-16 PA1 amendment Explicit shared serializer (CONCLUSIONS.md T001 Measurement Seam Design Disposition at fca46e8c) and the superseding PA1 amendment Serializer setup before baseline (CONCLUSIONS.md T001/T002/T008 Serializer Setup Sequencing Ruling at 85feb708), governs this specification. The effective ceiling is read from HybridCacheOptions, with the ticket's 1 MiB retained as the reported default, never a hardcoded runtime measurement. WarningFraction is finite, strictly greater than zero and at most one; default 0.8. Logging only, numeric PayloadBytes/BudgetBytes/Fraction, no content. Internal HybridCache serializer/options inspection and the blanket custom-wrapper prohibition are replaced by deliberately approved explicit shared `IHybridCacheSerializer<CachedProfile>` registration and adapter injection confined to already-authorized `PersistenceServiceCollectionExtensions.cs` and `HybridCachedRecommendationProfileSource.cs` (thin internal serializer and internal envelope accessibility permitted; no new package/project/layer). Sequencing: T001 read-only → T001b serializer-only install/proof over unchanged incumbent → T002 baseline through that live seam → representation → T007/T008 compact-profile options/logging/boundary/reuse proofs that **reuse** the T001b seam and do not reinstall the serializer. Do not invent an exact `AddSerializer` signature — registration/accessibility remain T001b verification requirements. T001b must supply both compiler/type-check evidence and runtime same-singleton exact-envelope measurement plus real `GetOrCreateAsync` admission proof; compilation alone is insufficient; no assumed serializer invocation without distributed cache. Construction-only byte observation and HybridCache admission MUST invoke the same registered singleton serializer with the same options on the actual `CachedProfile(Profile, Generation, CreatedAt)` envelope; no reflection, independently assumed JSON defaults, replacement admission path, or per-hit serialization. Warn before admission; size alone cannot fail coalesced requests, and swallowed non-admission must not appear as a warm hit. Compact-profile runtime proofs of observation/admission agreement, real `GetOrCreateAsync` reuse, and separate no-`IDistributedCache` limit behaviour remain required. Version the compact key namespace so legacy cached shape is ignored and rebuilt while generation/expiry remain unchanged (not in T001b). Task inventory is 17 with 22/22 FR+SC coverage preserved; T001/T001b/T002 held until independent reread.

Partial/empty embedding results with nonempty sources use existing missing-vector behavior. The profile identity set stays fixed across the request even if liked membership changes concurrently; no new cross-read transaction guarantee is promised. Both consumers must read source plot vectors exclusively from inputs. Missing fixed parity snapshot or unproven/infeasible retained-map/envelope fivefold budget is a blocking T002 checkpoint for recon/Patron disposition after successful T001b. 725 and 80% remain frozen; only ticket contradiction or constitution departure escalates to owner. Warm latency records profile-cache-hit plus uncached batch query cost, or zero batch for empty sources.
