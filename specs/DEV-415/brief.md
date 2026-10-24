# DEV-415 - Frozen grill brief

Status: grill closed by Patron Q10; ready for Conductor to route Quill drafting. Size M; no UI. Gate 1 remains closed. This document authorizes planning artifacts only, not implementation, build gates, review clearance or merge.

## Identity and decision sources

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-415-spec`.
- Branch: `feature/415-spec`; verified resume HEAD: `25d9b75f828c71d25beacd8b3770038b3b420a42`.
- Ticket source: Maestri note `DEV-415`, lines 17-23: HybridCache package/Infrastructure registration; single profile entry/configurable expiry; tag invalidation on playback and feedback; playback then next-read recomputation. AC: repeated reads cache, playback/feedback invalidate, cold cache empty profile.
- Facts: `recon-DEV-415` sections 1-5 and corrected section 7; `recon-DEV-415-2` section 8. Original recon section 6 is superseded; do not use it as evidence.
- Decisions: [CONCLUSIONS.md](CONCLUSIONS.md), Q1-Q10; `specs/PRODUCT.md` sections 1-3,5; `.specify/memory/constitution.md` principles I, VII, IX, Technology Stack and Development Workflow; `lamuflix-team-charter` section 2.3 and M-axis duties.
- Q1 and Q2 are preserved. Q7 expressly refines Q4 by removing caller-supplied genre weights. Q6 settles adapter lifetime. Every ruling below is deliberate, not a missing-fact assumption.

## Frozen scope and loop discipline

Deliver one process-local derived TasteProfile cache with a fixed trusted key/tag, validated configurable expiry, invalidation after successful persisted playback and watched/not-interested set/unset saves, factory-owned scope, generation/freshness/cancellation correctness, and the tests below; anything else is a follow-up issue, not a finding in this round.

Closing bar for later code-review/ship-review: zero unresolved Critical, High or Medium findings within frozen scope; all applicable gates earned; all three Risk, Standards and Spec reports present for size M. A missing axis blocks. Scope-empty or opt-out is skipped, never pass; an unrunnable gate has not passed. Low and out-of-scope findings follow charter follow-up policy. Unnecessary abstraction is a follow-up rather than a forced fix.

Round cap: two numbered fix/challenge rounds per applicable review loop. Remaining blockers at the cap stop the loop and return to Conductor; no silent third round or weakened bar. These terms were confirmed by Patron Q10, not assumed.

## Settled grill answers

| Question | Frozen answer | Basis |
|---|---|---|
| Q1: What is cached and what does cold mean? | One personal-library derived TasteProfile, not candidates or recommendation results. A cold miss computes persisted inputs; empty only if no qualifying history. | Ticket lines 17-23; recon original 19-22,44; PRODUCT section 3. |
| Q2: Stable or prerelease? | Stable .NET 10-compatible Hybrid only. No prerelease or adjacent-pin inference. | Ticket lines 17,20; PRODUCT 1,3; constitution Technology Stack. |
| Q3: Which exact version? | Microsoft.Extensions.Caching.Hybrid 10.11.0, centrally fixed, Infrastructure direct reference only. Restore/build/audit remain pending. | Recon part 2 section 8 lines 10-15; Q2; constitution I. |
| Q4: Where is the cache seam? | Extend existing IRecommendationProfileSource; retain its input method, add one concrete Infrastructure adapter. No new port/project/layer or Core Hybrid reference. Factories own scopes. Signature refined by Q7, lifetime by Q6. | Constitution I; PRODUCT 2; recon original 13-22; part 2 lines 18-27. |
| Q5: When/how invalidate and protect races? | After successful persistence, tag-invalidating from EfPlaybackHistory and EfMovieRepository. Conservative successful-repository-save invalidation permitted. Application generation rejects pre-invalidation fills before return, even if a read joined old shared work. | Ticket lines 17,21-23; recon original 33,40,43; part 2 lines 25-27,36-44. |
| Q6: Expiry and testability? | Infrastructure TasteProfileCacheOptions section TasteProfileCache; Expiration defaults to five minutes and must be positive, startup validated. Both native expirations match it. Application TimeProvider envelope age enforces maximum age; concrete adapter singleton. | Ticket configurable expiry; constitution VII/IX; part 2 lines 29-34,56. |
| Q7: Who supplies calculation inputs? | Cancellation-only profile retrieval; factory obtains all profile inputs and genre weights in its own scope through existing calculators. Snapshot generation and CreatedAt before reads; never relabel an old result as new. | Recon original 13-15,19-22; Q1,Q5,Q6. Supersedes Q4 caller-weight proposal. |
| Q8: Cancellation, failure and tests? | Caller tokens govern waits/invalidation; shared token governs factory reads. Own scope outlives individual waiter cancellation. No cached failure/empty fallback. Real-cache/Postgres tests establish runtime behavior. | Ticket AC; constitution VII/IX; part 2 lines 24-27,49,59,62. |
| Q9: Files, gates and task order? | Bounded files and sequence below; no current build gates, new remote dependency or external health endpoint. Later applicable harness gates must be earned. | Ticket scope/AC; constitution I,VII,IX and workflow; PRODUCT 2,3; Q1-Q8. |
| Q10: Is the grill closed? | Yes. Scope, closing bar and two-round cap frozen. No owner checkbox identified. Conductor routes Quill; Keel does not start Quill. | Q1-Q9; ticket/constitution preserved; charter and grill loop discipline. |

## Approach and internal API

Register `AddHybridCache` inside Infrastructure persistence registration, process-local L1 only, with no distributed provider. Register one singleton `HybridCachedRecommendationProfileSource` instance, expose that same instance through the existing profile port and to Infrastructure invalidation callers. The singleton holds only cache, scope factory, TimeProvider and validated options; it never captures a request DbContext, scoped source or scoped adapter.

Extend existing `IRecommendationProfileSource` with `GetTasteProfileAsync(CancellationToken)` returning TasteProfile through the repository-consistent async return convention. Preserve existing input retrieval. `EfRecommendationProfileSource` remains the uncached concrete and implements derived retrieval through the existing `GenreWeightsCalculator` and `TasteProfileCalculator`. The cached adapter resolves this concrete in its own created/disposed scope, avoiding recursive resolution of the cached port. Each retained input-method EF read also owns a scope. Handler consumes the derived profile through the existing port; any other required genre/candidate work stays uncached.

Use one fixed trusted key and tag. The cached envelope contains the derived profile, operation generation and CreatedAt. Factory snapshots generation and injected time before reads; all profile inputs, including genre weights, come from the factory-owned scope. Recheck generation and age before returning. A mismatched generation or age greater than or equal to Expiration means evict and recompute, with cancellation-aware awaited retries rather than spinning on completed stale values. No versioned-key accumulation or general cache framework.

Bind validated Infrastructure options from `TasteProfileCache`, default Expiration five minutes. Require strictly positive TimeSpan at startup. Configure Hybrid `Expiration` and `LocalCacheExpiration` from that duration. CreatedAt/age uses injected TimeProvider; native L1 may evict earlier. No sliding expiry extension on hits or background refresh. Time-dependent profile calculation is rerun on expiry.

After successful persistence, `EfPlaybackHistory.RecordAsync` and `EfMovieRepository.SaveChangesAsync` invoke internal invalidation on the shared singleton adapter. Cover watched and not-interested set and unset actions. Conservative invalidation after successful repository saves is approved; preserve existing GenreWeightsCache behavior. Failed persistence does not invalidate. Invalidation failure follows existing error handling, without swallowing failure or pretending the committed database write rolled back.

A read beginning after completed invalidation must not return an old-generation profile even if HybridCache shares an older in-flight fill. It may await old work then retry. Reads spanning a concurrent write have no transactional snapshot guarantee. Existing LocalPlay gating, launcher behavior, routes, DTOs and status codes remain outside these edits.

Caller cancellation flows through cache waits/invalidation; the shared factory token flows into owned-scope async reads. Never bind factory lifetime to the first request token. One canceled waiter does not dispose shared scope or cancel work needed by another. Last-waiter cancellation/factory failure must dispose scope, must not cache failure/empty fallback, and must allow later recovery.

## Approved implementation file set

Paths below are relative to this worktree; production edits belong to the later Build phase, not this spec phase.

| File | Purpose |
|---|---|
| Directory.Packages.props | Exact Hybrid 10.11.0 central pin. |
| src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj | Infrastructure-only direct Hybrid reference. |
| src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs | Hybrid/options/uncached source/same-singleton adapter registration. |
| src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs (new) | Existing-port cached adapter, owned scopes, envelope, freshness/generation and internal invalidation. |
| src/LamuFlix.Infrastructure/Persistence/TasteProfileCacheOptions.cs (new) | Validated configurable duration with approved default. |
| src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs | Uncached cancellation-only derived profile calculation. |
| src/LamuFlix.Infrastructure/Persistence/EfPlaybackHistory.cs | Post-successful-persistence invalidation. |
| src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs | Post-successful-save invalidation; existing cache behavior retained. |
| src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs | Extend existing boundary, retain input method. |
| src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs | Consume derived profile through existing port. |

Tests stay in existing `tests/LamuFlix.UnitTests`, `tests/LamuFlix.IntegrationTests` and `tests/LamuFlix.ArchitectureTests`. Existing known targets include recommendation handler tests/property arrangements, calculator/property regressions, EfRecommendationProfileSourceTests and EfPlaybackHistoryTests. Add owned-scope Hybrid integration coverage in the existing integration project. Additional exact fixture changes are allowed only where constructor/contract edits force them; do not invent unverified existing filenames. Unknown facts route to recon.

No new dependency beyond ticket-authorized Hybrid; no public HTTP contract, OpenAPI/generated TypeScript/web changes, schema/migration, playback execution, new port/project/top-level folder, host configuration rewrite or unrelated source rewrite. No clarify/checklist/ADR on this M path. No new domain term or UI taste assumption; existing Taste Profile, Playback Event and Feedback vocabulary stands. No CONTEXT.md or ASSUMPTIONS.md rewrite. Local process cache introduces no remote health dependency; existing telemetry conventions apply where required by gates, not as a new observability feature.

## Test strategy

Use existing xUnit v3, Shouldly, NSubstitute and property-test conventions. Infrastructure behavior uses real HybridCache 10.11.0 and Testcontainers PostgreSQL; never mock driver translation/serialization or introduce EF InMemory. Command interception, counters and barriers may instrument tests, without production test hooks.

Required scenarios:

1. Repeated reads reuse one computed derived profile; candidates remain uncached.
2. Cold miss with no qualifying history yields empty profile; cold miss with history computes it rather than returning empty.
3. Persist playback then next read recomputes; watched/unwatched and not-interested/unset each invalidate and recompute after persistence.
4. Valid duration binds, invalid/nonpositive duration fails startup, five-minute default applies; fake TimeProvider verifies application age just before and at expiry. Advancing it is not evidence that native MemoryCache expired.
5. Same-key callers coalesce; barrier-controlled old fill across invalidation cannot supply old generation to a post-invalidation read. Cover generation and age rejection without stale-value spin.
6. One waiter cancels while another succeeds; all waiters cancel; initiating request scope disposes while shared work continues safely in owned scope. Verify eventual scope disposal and cancellation termination.
7. Factory failure/cancellation does not cache error/empty fallback and later read can recover. Failed persistence does not invalidate; failed invalidation propagates and does not claim database rollback.
8. Pure freshness/generation/handler unit coverage and existing calculator/property regressions; architecture checks keep Hybrid out of Core and avoid new ports/layers.

## Evidence limits and later validation

NuGet metadata establishes a stable 10.11.0 net10.0 asset group with Caching.Abstractions/Memory, Logging.Abstractions and Options dependencies 10.0.13. No restore has been run; CPM resolution, build and runtime compatibility are not yet verified. Source evidence at dotnet/extensions commit `25f07afa51034e37b334210642d7dc5f191c142d` establishes singleton Hybrid registration and TimeProvider-backed creation/tag timestamps, not a MemoryCache expiry-clock guarantee.

Source inspection supports rejection of stored pre-invalidation tag timestamps; it is not a runtime proof of all in-flight behavior. Part 2 explicitly leaves one shared-factory body incompletely read and scoped EF runtime behavior untested. Our own-scope/generation/cancellation rules are application requirements to verify, not invented library guarantees. Unknown native expiry clock does not block this design because application freshness is independently enforced. No multi-server cache consistency claim.

No restore, build, tests, analyzers, mutation, formatter or other build gates run during this grill. Later Build phase must run restore/build and applicable configured harness gates: Roslyn, complexity, InspectCode; unit, integration, architecture and property tests; vulnerability audit; format verification; mutation where eligible. Use harness configuration as the source of thresholds; never lower them. Scope-empty, opt-out, N/A and could-not-run retain their distinct meanings, never become PASS.

## Task order and next step

1. Later pickup: reconcile against current main and frozen decisions; any new fact gap routes recon. Planning closure is not implementation permission.
2. Pin/reference stable package, restore to prove actual CPM resolution, bind/validate options and wire DI; later package audit must pass.
3. Extend existing port and implement uncached all-input profile calculation with existing calculators.
4. Implement singleton cached adapter with owned scopes, operation snapshot, one-key/tag envelope and cancellation-aware generation/freshness checks.
5. Route handler profile reads and wire post-persistence playback/repository invalidation, preserving other behavior.
6. Implement/run required meaningful unit/integration/race/cancellation/regression coverage.
7. Run applicable gates and the frozen review loop; reconcile all three M axes and report unresolved blockers at the cap. User owns merge.

Immediate next step: Conductor routes Quill to draft spec.md, plan.md and tasks.md from this brief. Keel has not started Quill. After Quill drafting, Keel runs read-only speckit-analyze and checks plan/tasks against this brief within the M path. No clarify/checklist or ADR. Gate 1 remains a separate owner approval.
