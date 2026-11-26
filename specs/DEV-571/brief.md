# DEV-571 Phase 2 brief

**Evidence base:** `feature/571-spec` at `ef6dd3e694aa82d3bddd7effe3e31f96b5289a75`, task note `DEV-571`, the verbatim Wanted section retained from the prior `recon-DEV-571` revision, and the current recon path and line evidence. Full Patron exchanges are in [CONCLUSIONS.md](CONCLUSIONS.md).

## Frozen scope

Apply the IMDb quality floors, quota-bounded evidence acquisition and persistence, and one title per TMDb collection to the existing read-only **Not in your library yet** discovery row. Preserve DEV-561 ranking (liked seed weight 2, watched seed weight 1, score descending then TMDb ID ascending), contribution eligibility, minimum seed and library exclusion rules, visible cap of 20, and existing row and library-grid behavior. The scope includes the cache, shared OMDb quota and rollout requirements below. Delete dead code directly orphaned by this change in the same PR. Anything else is a follow-up issue, not a finding in this round. An actual ticket contradiction or a constitution departure returns to the charter §2.3 structural process.

## Patron decisions and implementation plan

1. **Q1 — IMDb eligibility.** A known rating below 6.5 or a known vote count below 1,000 rejects a candidate. Both floors are inclusive. A confirmed absent IMDb rating or ID preserves baseline eligibility unless a known value fails. A valid rating with unknown votes, a transient provider failure, or a lookup that cannot run yet leaves an unresolved candidate deferred. Evaluate lower-ranked candidates within the bounds and restore the deferred candidate to its original sort position when evidence resolves. An unresolved outcome creates no durable negative entry.
2. **Q2 — Visible-row lookup and collection selection.** Walk the original sort order until 20 eligible representatives are admitted or candidates run out. The first admitted title represents its collection. Skip later candidates in a known represented collection before a lookup, and keep titles without a collection individually eligible. Resolve IMDb evidence only for a candidate that can occupy a slot. The live OMDb attempt limit per load is the smaller of open slots at load start (at most 20) and remaining shared daily allowance. Rejections do not replenish this limit. Charge every outbound attempt, including failure, timeout and each retry. Continue using cached evidence and confirmed absence after either limit is exhausted. Defer unresolved misses and allow a shorter row.
3. **Q3 — Durable evidence and shared quota.** Add ticket-owned persistence for candidate evidence and the quota counter in existing Infrastructure, with the associated entity/mapping and migration. Key evidence by TMDb ID and retain IMDb ID, positive collection ID where present, parsed IMDb rating/votes, outcome, and UTC expiry. Recompute eligibility from the evidence. Complete evidence, including known floor failures, stays fresh for 30 days. Confirmed absent rating/ID stays fresh for 7 days. Unresolved outcomes get no durable entry. Coalesce identical concurrent misses in-process. Extend the existing TMDb adapter with on-demand `external_ids` and reuse its collection mapping. Reserve atomically by UTC calendar date before every outbound attempt in the existing OMDb adapter, covering every consumer, request and user across processes. Use the existing injectable time seam and non-secret configuration binding. Absent, zero or invalid quota defers live work. Cache hits make no remote call.
4. **Q4 — Rollout.** In existing OMDb environments, apply the migration, provision and read back a positive allowance at or below the actual provider plan limit based on shared use, then enable the guard. Environments with no current OMDb use can migrate and remain fail-closed until configured. Prove ordinary enrichment through the guarded adapter and demonstrate two consumers drawing from the same pool with failures charged. Correct invalid configuration through provisioning.

### Files and components

- Provider evidence and request accounting — `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs` and `OmdbMetadataProvider.Mapping.cs` in the same directory. TMDb identifier lookup and collection mapping belong in the existing `TmdbMetadataProvider.cs` and `TmdbMetadataProvider.Mapping.cs` adapter family. Recon identifies the current null-rating mapping and collection fields.
- Ranking and selection — existing Core Recommendations slice, anchored at `src/LamuFlix.Core/Features/Recommendations/OutsideLibraryRanking.cs`. Preserve the baseline sort and filter/selection invariants.
- Persistence and composition — existing Infrastructure persistence area for the two ticket-owned stores and migration, existing DI registration and non-secret configuration binding. The existing movie enrichment persistence in `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` is a recon reference for current behavior.
- Tests — extend coverage around `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs` and `tests/LamuFlix.UnitTests/Features/Recommendations/OutsideLibraryRankingPropertyTests.cs`. Add provider fixtures and real relational integration coverage in the existing test layout.
- Charter §2.3 scope is recorded in Q3. Approved production changes stay within the listed components. Existing public route, DTO, statuses and frontend/generated contracts remain the baseline. Keep dependencies, project structure, security settings, secrets, LocalPlay and process execution outside the change.

## Q5. Closing bar and round cap

Patron closed the grill after Q5. The later review cap is **two rounds**. Close all in-scope Critical, High and Medium findings, and record an evidence-backed disposition for every Low finding. Require all applicable harness gates and the Q4 rollout evidence, recording the outcome of each gate separately. Preserve `SKIPPED (scope-empty)`, `SKIP`, `N/A` and `Could not run` as distinct outcomes. Keep current gate thresholds.

### Task order

1. Establish DEV-561 regression fixtures and define the provider evidence model, including the distinctions between confirmed absence, known floor failure and unresolved evidence.
2. Implement the durable candidate cache, the shared OMDb guard and the migration before wiring the discovery path to live lookups.
3. Implement the ordered candidate scan, budget-aware backfill and collection selection.
4. Run integrated verification and capture the Q4 rollout receipt in the build phase. Verify the migration and quota prerequisites before guard enablement in applicable environments.

### Test and gate expectations

- Unit and property tests cover 6.5 and 1,000 exactly and immediately below, missing and unresolved evidence, like/watch weighting, order and tie-breaks, backfill, collection representative selection, independent uncollected titles, and the cap. Verify unchanged discovery contract and existing library-grid behavior.
- Provider fixtures cover parsing, on-demand external-ID lookup, cache hits, fresh and expired outcome handling, one leader for concurrent same-key misses, absence of speculative calls, per-load exhaustion and shared quota exhaustion, chargeable failures and retries, and existing enrichment.
- Real relational persistence tests cover candidate key uniqueness, durable reuse, both expiry windows, atomic parallel reservations, shared accounting across consumers, and UTC rollover. Exercise real query translation and serialization.
- In the build phase, run applicable Roslyn analyzers, cyclomatic complexity, JetBrains InspectCode, property and ordinary tests, integration tests, vulnerable-package scan, Stryker and API contract drift checks. The Phase 2 brief records the expected checks only.

## Ticket acceptance source

The prior recon update quoted DEV-571's Wanted section as follows.

> 1. Hide candidates below IMDb 6.5, or with fewer than 1,000 IMDb votes. Get each candidate's IMDb ID from TMDb `external_ids`, then get its score from OMDb. Store the result so it is not fetched again on every load.
> 2. Fetch ratings only for candidates that could reach the visible row, to stay within OMDb's daily limit.
> 3. Show at most one title per TMDb collection.
> 4. Keep today's ranking (liked 2 / watched 1) for every candidate that passes. Candidates with no IMDb rating are treated the same as before.



## Q3/Q4 update after quota recon

This ruling supersedes the original Q3 references to adding a quota counter and to two ticket-owned stores. Reuse the existing OMDb typed-client MetadataQuotaHandler, IOmdbRequestGate, EfMetadataRequestGate and omdb_daily_usage. The candidate evidence cache is the only new table and migration scope. Use the existing OmdbQuotaOptions binding and remove its default allowance of 1,000. Preserve the existing enrichment quota settlement. The approved files and components include the existing handler/client registration, quota gate/store, quota options and binding, and direct tests as needed for these changes. Basis: recon-DEV-571 lines 23–28, Wanted 1–4, Constitution I and IV, and the Patron update in CONCLUSIONS.md.

**blocked: structural** — May DEV-571 amend Constitution VII to add a narrow OmdbQuotaOptions exception, scoped to the daily allowance only, so absent/zero/invalid allowance does not fail host startup but yields an explicit disabled state with no usable allowance, denies live OMDb work, preserves cached and confirmed-absence evaluation, leaves all other Omdb and other options startup-validated, and requires Q4 provisioning and readback of a positive allowance before deploying the revised guard where OMDb is already in use?

The runtime disabled behavior in the original Q3 and Q4 is conditional on the owner's answer. Put this checkbox on the spec PR and keep Gate 1 closed until the owner answers. Startup validation remains in force until the exception is approved and recorded. Do not implement or claim the runtime disabled behavior before that approval. Q4 still requires migration and positive allowance provision/readback before deploying the revised guard where OMDb is in use, plus proof of ordinary enrichment and two-consumer charging.


## Phase 2 plan-challenge decisions

Patron's ruling in [CONCLUSIONS.md](CONCLUSIONS.md#phase-2-plan-challenge-adjudication-ledger-h1h2-and-the-20-findings) refines the implementation plan within Wanted 1–4 and Q1–Q5. Recon lines 23–37 establish the quota and discovery seams.

- Reuse the scoped `EfRecommendationCandidateSource` implementation of the existing `IRecommendationCandidateSource` outside-library method. Infrastructure orchestrates the evidence, cache and provider calls and returns the typed snapshot result. Core owns pure ranking, floor, collection and backfill rules. The existing handler and registration may change for this internal contract. Preserve the other candidate-source methods, enrichment output and public contract.
- Establish evidence fixtures and outcome rules before migration. Build the cache, guard, evidence-write path and on-demand TMDb external-ID capability in the foundation before selection consumes them. Resolve candidates serially in DEV-561 order. Direct new fixtures in existing test projects and test fixture changes proven necessary by a gate are in scope.
- Fix the per-load ceiling at load start. A TMDb external-ID call requires room for an OMDb send, no prior denial on the load and room under that same numeric ceiling for TMDb calls. Count every TMDb external-ID call, including failed and empty lookups. Validate the outbound IMDb ID before reserving. Each OMDb attempt reserves atomically for its current UTC date. A send-boundary denial defers that title and switches the remainder to cache/confirmed-absence evaluation. UTC rollover leaves the local ceiling fixed.
- Coalesce the full miss chain by TMDb ID in Infrastructure instance state shared across participating requests. Waiters send and charge nothing and can cancel independently. A failed or abandoned leader defers remaining waiters and allows a later load to retry after cleanup. Use fresh cached collection membership until expiry unless fresher evidence is already known locally.
- Carry the exact owner checkbox onto the spec PR. After an affirmative answer, the governance lane puts the actual narrow Principle VII amendment, version bump and sync-impact record on that PR. T000 records the amendment commit and verified owner merge. Owner rejection stops this draft for re-planning. T001 and later tasks require T000.
- In one post-Gate-1 implementation step remove the 1,000 default and normalize only `Omdb:Quota:DailyLimit` within existing binding to a typed positive allowance or explicit disabled state. Disabled denies live OMDb work for discovery and enrichment. Preserve enrichment settlement, validation of all other options and the Q4 provisioning order. Use deployment provisioning for environments that rely on the default.
- Extend verification to cover mid-load denial, both providers' call counts, malformed IDs and numeric evidence, coalescing and cancellation, stale collection membership, rollover, unrelated option startup validation, unchanged enrichment projection and safe quota-denial diagnostics. T001 checks the quota time seam. The Q4 receipt records charged discovery and enrichment attempts separately.

