# DEV-571 grill conclusions

---

## Q1. IMDb eligibility

**Keel**

In the unchanged DEV-561 rank order, how should a candidate with unavailable IMDb evidence be treated? I recommend rejecting when either known IMDb value is below its inclusive floor (6.5 rating, 1,000 votes), preserving baseline eligibility for a confirmed absent IMDb rating/ID unless a known value fails, and treating transient lookup failures or deferred lookups as unresolved with no durable negative cache. For a valid rating with unknown votes, I recommend treating it as unresolved for the vote floor and deferring it while still considering lower-ranked candidates within the approved budget. This follows Wanted 1 and 4 (verbatim recon supplied in the previous update), current null mapping in recon lines 5–8, and preserves ranking for survivors. Please rule on this eligibility policy as one decision, including what to do under transient provider failure.

**Patron**

[from Patron] DEV-571 Q1 ruling: Accepted as one eligibility policy, exactly as recommended.

- Reject only on known evidence: a candidate is rejected when either known IMDb value is below its inclusive floor (rating < 6.5 or votes < 1,000); values at 6.5 / 1,000 pass. Confirmed absent IMDb rating/ID preserves baseline eligibility unless a known value fails, per the current null mapping (recon lines 5-8) and Wanted 1 and 4.
- Unknown is not failure: transient lookup failures, deferred lookups, and a valid rating with unknown votes are unresolved, not rejections, and must not write a durable negative cache. The unknown-votes candidate is deferred for the vote floor and retried on a later lookup, retaining its DEV-561 rank position on resolution.
- Budget proceeds without blocking: while a candidate is unresolved/deferred, lower-ranked candidates may be considered within the approved budget, preserving the unchanged DEV-561 rank order for survivors and for the deferred candidate when its evidence resolves.

Basis: unchanged DEV-561 rank order; inclusive floors as recommended; Wanted 1 and 4 and recon null mapping as cited by Keel. Record verbatim in specs/DEV-571/CONCLUSIONS.md.


---

## Q2. Visible-row scan and quota

**Keel**

What qualifies for a live rating lookup under the visible-row and daily-limit requirements? I recommend walking deterministic DEV-561 order until 20 eligible collection representatives are found, skipping known duplicate collections once a representative is admitted. Look up only candidates that can still occupy an open slot, with a finite shared allowance across requests and users, charging each actual OMDb request, including failures, and no remote calls for cache hits. Once the allowance is exhausted, keep evaluating cached evidence and confirmed missing IMDb information, deferring other misses under Q1. This can leave the row shorter until lookups resolve. Basis: Wanted 1–4 and recon lines 15–18. Please rule on scan/backfill and budget semantics together, including whether the daily cap must account for other OMDb usage and how a per-load bound is expressed.

**Patron**

[from Patron] DEV-571 Q2 ruling: Accepted as recommended, with the daily-cap accounting and per-load bound below. Record verbatim in specs/DEV-571/CONCLUSIONS.md.

- Scan/backfill: Walk deterministic DEV-561 order until 20 eligible collection representatives are found or candidates are exhausted. Once a representative for a collection is admitted, skip known duplicates of that collection with no lookup. Start a live lookup only for a candidate that can still occupy an open slot; no speculative or duplicate-collection lookups. Basis: Wanted 1-4 and recon lines 15-18.
- Shared daily allowance: The daily cap is one finite, shared allowance across all requests, users, and all other OMDb usage in the application, not a per-request, per-user, or per-feature allowance. Every actual OMDb request is charged, including failures, timeouts, and each retry as a separate request; cache hits and confirmed-missing evidence already held charge nothing and make no remote call. Other OMDb consumers draw from the same pool, so this row receives only the remainder. Basis: a cap that other OMDb traffic could bypass would not be a daily cap.
- Per-load bound: Expressed as a count of actual live requests, at most the smaller of the open slots at load start (maximum 20) and the remaining shared daily allowance. Known-evidence rejections during that load do not replenish the per-load live-lookup count; further backfill in that load uses cached evidence or confirmed-missing information only, and the rest defer under Q1. Basis: 20 is the visible-row capacity, so the bound derives from the row structure, not a new tunable.
- Exhaustion: Once either bound is exhausted, keep evaluating cached evidence and confirmed missing IMDb information, defer all other misses under Q1 with no durable negative cache, and accept that the visible row may be shorter than 20 until lookups resolve.

This preserves Q1: unresolved evidence never rejects, and deferred candidates retain their DEV-561 position when evidence resolves.


---

## Q3. Storage, quota configuration, and charter scope

**Keel**

What is the storage and integration design for candidate evidence and the shared OMDb allowance? Under charter §2.3, I recommend ticket-owned durable cache and quota storage in existing Infrastructure persistence, with the existing OMDb adapter enforcing an atomic application-wide reservation at every outbound attempt and an existing injectable time seam for UTC day rollover. Reuse TMDb collection mapping and extend the existing adapter to resolve external_ids only as needed. Key cached candidate evidence by TMDb ID and retain the IMDb ID, positive collection ID if present, parsed rating/votes and outcome; cache successful floor decisions and confirmed absence, but retry unresolved evidence later and coalesce concurrent identical misses. Use explicit freshness for successful and confirmed-negative evidence so every load does not refetch. I recommend no new dependency, project, top-level folder, architectural layer, public route/DTO/status, security setting, LocalPlay, secrets or Process.Start changes. Limit tracked-source edits to the provider, ranking/selection, persistence, registration and tests directly required by Wanted 1–4. Basis: recon lines 5–16, Wanted 1–4, PRODUCT §§2 and 5, Q1–Q2. Please delimit approved files/components and rule on expiry and quota configuration in this design.

**Patron**

[from Patron] DEV-571 Q3 ruling: Approved as recommended, within the file, expiry, and quota limits below. Record in specs/DEV-571/CONCLUSIONS.md.

Approved scope, and no wider:
- Existing Infrastructure persistence only: ticket-owned durable tables/entities/mappings and any migration solely for the candidate-evidence cache and the shared quota counter for this ticket. No schema change beyond those ticket tables.
- Existing OMDb adapter/provider only: enforce an atomic application-wide reservation before every outbound attempt, charge reserved attempts including failures and each retry separately, and make no call on a fresh cache hit. Reuse the existing TMDb collection mapping; extend the existing adapter to resolve external_ids only when needed for a candidate that can still occupy a slot under Q2.
- Ranking/selection component implementing the DEV-561 order, dependency-injection registration, existing configuration binding, and the existing injectable time seam. Tests directly covering provider, ranking/selection, persistence, reservation, and rollover.
- Explicitly not approved: any new dependency, project, top-level folder, architectural layer, public route, DTO, or status code; any frontend or generated contract-chain change; any LocalPlay, secrets, security-setting, or Process.Start change; and any other tracked source or config edit not directly required by Wanted 1-4. Basis: charter section 2.3, PRODUCT sections 2 and 5, recon lines 5-16, Wanted 1-4, Q1-Q2.

Storage and evidence:
- Key candidate evidence by TMDb ID; retain IMDb ID, positive collection ID if present, parsed rating and votes, outcome, and a UTC expiry from the injectable time seam. Store the parsed values, not only a pass flag, so floor decisions are recomputed from evidence.
- Fresh successful evidence, whether it passes or is known below floor, expires after 30 days. Confirmed absence of an IMDb rating/ID expires after 7 days, because an external ID or rating can appear later. Unresolved, transient-failure, deferred, and unknown-votes evidence gets no durable entry and is retried later under Q1. Coalesce concurrent identical misses in-process so one leader performs and charges the lookup and waiters reuse its outcome; the atomic reservation remains the cross-process quota guard.

Quota configuration:
- One application-wide daily allowance held in existing non-secret application configuration, shared by every OMDb consumer, request, and user; it is not a per-feature pool. The UTC day is the UTC calendar date from the existing injectable time seam, the reservation is keyed by that date, and rollover needs no background job. An absent, zero, or invalid allowance fails closed for live lookups: cached and confirmed-missing evaluation continues, all else defers under Q1.


---

## Q4. Rollout order

**Keel**

How should rollout preserve existing OMDb enrichment while Q3 fails closed for absent or invalid quota? I recommend ordering migration and deployment provisioning of an explicit positive non-secret allowance before enabling the adapter guard in any environment that already uses OMDb. Derive the configured value from the actual provider plan and shared use, verify normal existing enrichment and cross-consumer reservation before rollout acceptance, and keep zero/invalid configuration fail-closed as a deliberate operating state. This adds an environment prerequisite and avoids silently disabling existing enrichment on upgrade. No new fallback allowance or bypass. Basis: Wanted 2, Q2–Q3 and the existing enrichment persistence in recon lines 5–8. Please rule whether that is the approved rollout order within ticket scope.

**Patron**

[from Patron] DEV-571 Q4 ruling: Approved rollout order, within ticket scope as a deployment prerequisite, not an extra code scope.

- Order in every environment that already uses OMDb: first apply the Q3 migration, then provision an explicit positive non-secret daily allowance, then enable the adapter guard. Do not enable the guard in such an environment before the allowance is provisioned and read back. Environments with no existing OMDb use may migrate without that prerequisite and remain deliberately fail-closed until provisioned.
- Value: Derive the allowance from the actual OMDb provider plan and measured shared use across all consumers, at or below the plan limit; it stays the single application-wide pool from Q2-Q3. No default, fallback, or bypass allowance is approved in code or configuration.
- Acceptance before rollout is complete: verify normal existing enrichment still succeeds through the guarded adapter, and that two different OMDb consumers draw reservations from the same shared pool with failures charged. Basis: Wanted 2, Q2-Q3, and existing enrichment persistence in recon lines 5-8.
- Zero, absent, or invalid configuration after enablement is a deliberate fail-closed operating state: cached and confirmed-missing evaluation continues, live lookups and enrichment defer, and remediation is correcting the configuration, never a code bypass.

Record in specs/DEV-571/CONCLUSIONS.md; leave worktree writes to Keel.


---

## Q5. Closing bar and verification

**Keel**

What is the closing bar for the DEV-571 plan and later review? I recommend freezing scope at Wanted 1–4 plus the Q1–Q4 mechanics, with anything else handled as a follow-up issue; two review rounds; all in-scope Critical/High/Medium findings closed and Low findings explicitly disposed with evidence under the written closing bar, plus all applicable harness gates and rollout evidence. Sequence regression fixtures for DEV-561 and the provider evidence model first; then the durable cache, shared guard and migration; then rank-ordered scan and collection selection; then integrated and rollout verification. Test exact boundaries and numeric parsing, confirmed absence vs unresolved, cache reuse and both expiry windows, order/selection/backfill/cap and uncollected titles, no speculative calls, per-load and shared quotas, failures and retry charging, parallel reservations, UTC rollover, ordinary enrichment, and discovery response parity. Use unit/property tests for ranking and selection, provider fixtures for mapping/call counts, and real relational persistence tests for uniqueness and concurrency. For the build phase, require applicable Roslyn, complexity, InspectCode, property/ordinary/integration tests, package scan, Stryker, and API contract drift checks where applicable; record individual gate outcomes. Approve these closure terms and say whether Q1–Q5 settle the grill. Basis: Wanted 1–4, Q1–Q4, PRODUCT §1, and the Size M harness and charter.

**Patron**

[from Patron] DEV-571 Q5 ruling: Closing terms approved; Q1-Q5 settle the grill.

- Frozen scope: Wanted 1-4 plus the Q1-Q4 mechanics only. Anything else is a follow-up issue, except dead code this change itself orphans, which is deleted in the same PR. No further grill questions unless spec or analyze surfaces a contradiction with the ticket text or a constitution departure, which returns under charter section 2.3 as blocked: structural.
- Sequence: First sequence regression fixtures for DEV-561 and the provider evidence model; then durable cache, shared guard, and migration; then rank-ordered scan and collection selection; then integrated and rollout verification.
- Review bar: Two review rounds. All in-scope Critical, High, and Medium findings closed; every Low finding explicitly disposed with evidence under this written closing bar. All applicable harness gates and the Q4 rollout evidence are required, with individual gate outcomes recorded; a skipped, opt-out, or could-not-run gate is reported as such and never folded into a pass, and no gate threshold is lowered.
- Test bar: Unit and property tests for ranking and selection; provider fixtures for mapping and call counts; real relational persistence tests for uniqueness and concurrency, never a mocked data-access driver where query translation or serialisation is in play. Cover exact rating and vote boundaries, numeric parsing, confirmed absence versus unresolved, cache reuse, both expiry windows, order, selection, backfill, cap, uncollected titles, no speculative calls, per-load and shared quotas, failure and retry charging, parallel reservations, UTC rollover, ordinary enrichment, and discovery response parity.
- Build-phase gates where applicable: Roslyn analyzers, cyclomatic complexity, JetBrains InspectCode, property and ordinary tests, integration tests, vulnerable-package scan, Stryker mutation, and API contract drift checks, each recorded separately.

Basis: Wanted 1-4, Q1-Q4, PRODUCT section 1, and the Size M harness and charter. The grill is settled; proceed to spec on Q1-Q5. Leave worktree writes to Keel.


---

## Q3/Q4 narrow update after quota recon

[from Patron] DEV-571 Q3/Q4 narrow update ruling: NOT constitution-compliant as stated; approved in part, with one blocked structural departure for the owner.

Verdict: Approve reuse of the one existing gate/table and candidate-evidence-only schema; do NOT approve the options change as constitution-compliant. It requires an owner-approved Principle VII departure, so record it as blocked: structural, below.

- Compliant and approved: reuse MetadataQuotaHandler, IOmdbRequestGate, atomic EfMetadataRequestGate and omdb_daily_usage (recon-DEV-571 lines 23-25); add only the ticket-owned candidate evidence table/migration; no new quota counter, port, dependency, project, layer, route/DTO or LocalPlay/secrets change. Basis: Constitution I closed port list already includes IOmdbRequestGate, so no new port or ADR; prior Q3 approved ticket-owned tables, and this narrows that to one new table. Preserve the landed enrichment quota settlement per Constitution IV.
- Compliant and approved: remove the 1000 default; no default, fallback or bypass allowance. Basis: Constitution VII bars hard-coded fallbacks and guessed numeric defaults, and prior Q4 already ruled no default. Valid positive allowance remains typed, checked, and the single shared pool.
- Blocked departure: absent/zero/invalid normalized to disabled with no usable allowance, host stays up, live OMDb work denied and cached/confirmed-absence evaluation preserved. This contradicts Constitution VII lines 709-713 and 719-733: OmdbQuotaOptions is in the closed startup-validated list, ValidateOnStart applies, and the only request-time-validation exception is subtitle acquisition, expressly limited to that scope. Current startup rejection of zero/negative (recon line 26-27) is the VII-compliant behaviour; changing it to runtime fail-closed is a subtitle-style VII exception for OmdbQuotaOptions only, which Patron judges necessary to meet Q3/Q4 and Wanted 2 without taking the host and cached evaluation down, but Patron cannot approve it alone.
- blocked: structural - May DEV-571 amend Constitution VII to add a narrow OmdbQuotaOptions exception, scoped to the daily allowance only, so absent/zero/invalid allowance does not fail host startup but yields an explicit disabled state with no usable allowance, denies live OMDb work, preserves cached and confirmed-absence evaluation, leaves all other Omdb and other options startup-validated, and requires Q4 provisioning and readback of a positive allowance before deploying the revised guard where OMDb is already in use? Put this checkbox on the spec PR; Gate 1 stays closed until the owner answers. Until then, do not implement the normalization, and do not claim Q3/Q4 fail-closed runtime behaviour as settled.

Record this in specs/DEV-571/CONCLUSIONS.md verbatim; leave worktree writes to Keel.


---

## Phase 2 plan-challenge adjudication (Ledger H1/H2 and the 20 findings)

[from Patron] DEV-571 Phase 2 ruling: Keel's recommendation approved, with three sharpenings (TMDb call bound, test-fixture config, M4 order). All of it stays inside the frozen Q5 scope. No new structural escalation. The only open owner item is the existing Principle VII checkbox.

- **H2 seam (Ledger H2, recon-DEV-571 31-37): approved.** Reuse the existing `IRecommendationCandidateSource` outside-library path. It is already on the closed port whitelist (ArchitectureTests:30-31, 155-175), so this adds no new named port, layer, or ADR (Constitution I; charter §2.3 item 2 not triggered). The ordering, eligibility, collection, and backfill rules are pure Core functions. Scoped `EfRecommendationCandidateSource` orchestrates them through its existing composition (PersistenceServiceCollectionExtensions:76). Evidence persistence, cache, TMDb `external_ids`, the OMDb send, and full-chain coalescing all live in Infrastructure. The handler receives a typed snapshot result. Extending the snapshot type is internal: no route, DTO, or OpenAPI change (FR contract chain, L3).
- **Concurrency (Sentry F4/F5): approved.** Resolution is serial and ordered within a load. There is one in-process leader per TMDb ID covering the full TMDb-to-OMDb miss chain. A waiter is never promoted to leader, and waiter cancellation is independent of the leader. If the leader fails or is abandoned, the remaining waiters defer, and later loads may retry. The coalescing map is instance state, not `static readonly` (AGENTS static-init rule, Ledger checked-OK).
- **Quota (Sentry F1/F3/F10, Q2): approved.** The per-load bound is fixed at the start of the load. Each send reserves independently in the current UTC day (accepted behaviour: a load that crosses midnight may use two dates). A denial or a `MetadataQuotaExhaustedException` defers that candidate, and the scan continues cache-only. It never fails the load. The outbound IMDb ID format is validated before reservation, so a malformed ID costs zero charge and makes zero sends.
- **TMDb bound (Sentry F2), sharpening:** no TMDb TTL and no extra budget. A TMDb `external_ids` call is made only when an OMDb send could follow it, meaning the per-load bound has room and this load has had no denial. TMDb calls per load are therefore at most the per-load OMDb bound. A resolved IMDb ID is kept in the ticket-owned evidence row, so later loads skip TMDb. Fixtures assert TMDb call counts per load (Q5 'no speculative calls').
- **H1 constitution amendment: approved, conditional on owner approval.** The VII checkbox literally asks to amend the constitution, so a yes from the owner forces the edit of `.specify/memory/constitution.md` (§2.3 item 6, forced edit). It is not a Quill or Build edit. The governance lane (`/speckit-constitution`) writes the actual VII OmdbQuotaOptions daily-allowance exception, with a version bump and a sync-impact block (DEV-453/DEV-501 precedent). The amendment lands on the spec PR, and the owner's merge of that PR is the verified receipt. T000 records the receipt, and nothing from T001 on starts before it. 'Recorded amendment' means the constitution commit, not a CONCLUSIONS line. If the owner says no, the work stops: startup validation stays as it is, and the disabled-state items below are dropped.
- **M1/M3 disabled state, under that amendment:** the existing binding normalizes only the raw `Omdb:Quota:DailyLimit` key, before ordinary conversion, into a typed enabled(positive) or disabled state. Absent, zero, negative, and nonnumeric values all become disabled. While disabled, the gate denies both consumers (discovery and enrichment). Enrichment settlement is unchanged (Constitution IV). Every other `Omdb:*` key and every other option stays startup-validated, with a negative test for that. The 1000 default is removed in the same implementation step. No config-file scope expansion: environment provisioning is Q4 rollout readback, not committed `appsettings`/AppHost/Compose edits. Test fixtures that a gate shows relied on the default are forced edits and in scope (cite the failing test).
- **M2: option (a).** T000 stays a hard block. Delete the dead 'until then / otherwise' branches from T004/T005/FR-011/FR-014 prose. No task split before Gate 1 (Q5 frozen scope).
- **M4: accepted.** Move TMDb `external_ids` (T014) and the evidence-write path into Phase 2 with the store, so selection phases are pure Core over a ready source. This matches the Q5 sequence: evidence model, then cache/guard/migration, then selection.
- **Diagnostics (Sentry F6/F11, Constitution VI):** add safe, structured quota-denial and defer diagnostics only: counts, dates, and TMDb IDs, never keys, request URIs, or raw bodies, with one test that asserts this. The Q4 receipt records the discovery and enrichment charged split. No new metrics stack and no fairness partition (Q2 single shared pool).
- **Lows:** F7: T001 confirms that the gate and handler path take a `TimeProvider`, or reports `needs recon`. F8: adversarial numeric fixtures for both fields, choosing null (unresolved) over a numeric guess. F9: a fresh cached collection ID counts as known until it expires, unless fresher evidence is already known without a remote lookup; one test pins this. L1: tidy to one normative gate-vocabulary statement plus references. L2: new test files are in scope. L3: the `MovieMetadata` widening must not change enrichment output or any public DTO, and the T019 drift check must show zero drift. Compass: 0 findings, nothing to dispose.

Basis: recon-DEV-571 23-37; findings-DEV-571-Ledger/Compass/Sentry; Constitution I, IV, VI, VII; Q2-Q5 rulings above; charter §2.3.

### Keel dispositions

The challenge reviewed draft artifacts on `ef6dd3e694aa82d3bddd7effe3e31f96b5289a75`. Patron's ruling is recorded in `147a48be`. The dispositions below accept the scoped correction for each finding. Where a finding proposed multiple remedies, the disposition identifies the approved remedy.

| Finding | Disposition | Rationale and required repair |
|---|---|---|
| Sentry F1 | Accept | Recon line 23 establishes the denial exception. Discovery defers the candidate and continues cache-only, preserving a successful shorter row. Exercise a mid-scan denial and a lower cached survivor. |
| Sentry F2 | Accept | Cap TMDb calls at the same per-load ceiling and count every external-ID call against it, including failures and missing IDs. Attempt one only while an OMDb send could still follow. Keep successful IDs in eligible evidence rows and assert provider call counts. No additional configured budget or transient TTL. |
| Sentry F3 | Accept | Validate the request ID before reservation. Malformed input makes zero sends or charges, while malformed response evidence cannot undo a charge for a request already sent. |
| Sentry F4 | Accept | Coalesce the full miss chain by TMDb ID. Waiters share the outcome, may cancel independently and never take over the flight. Failed or abandoned flights defer waiters and permit later loads to retry. |
| Sentry F5 | Accept | Resolve serially in DEV-561 order within one load. Preserve atomic shared reservation across loads and consumers and the send-boundary retry guard. |
| Sentry F6 | Accept | Add bounded quota-denial/defer diagnostics and show the charged discovery/enrichment split in Q4 proof. The single pool continues to allow either consumer to exhaust it. |
| Sentry F7 | Accept | Confirm the gate and handler time seam explicitly in T001 and report `needs recon` if it cannot be established. |
| Sentry F8 | Accept | Cover adversarial rating and vote strings and treat ambiguous values as unknown. Preserve Q1 outcome precedence and retention windows. |
| Sentry F9 | Accept | Fresh cached membership is known until expiry unless fresher information is already available locally. Verify the case without triggering a remote refresh on a fresh cache hit. |
| Sentry F10 | Accept | Each send reserves against its current UTC date. The per-load ceiling stays fixed and exhaustion stops fresh resolution for the rest of that load. Cover rollover. |
| Sentry F11 | Accept | Assert diagnostics contain only approved structured fields and exclude credentials, request URIs and raw bodies. |
| Ledger H1 | Accept | The governance lane applies the actual narrow VII amendment with version and sync-impact record to the spec PR after owner approval. T000 records the merged amendment commit and verified owner merge. |
| Ledger H2 | Accept | Recon lines 31–37 establish the existing allowed `IRecommendationCandidateSource` and scoped EF implementation. Keep pure policy in Core and perform evidence I/O through the existing Infrastructure source. |
| Ledger M1 | Accept | Under the amended VII rule, normalize only the raw daily allowance key in existing binding to a typed positive allowance or disabled state. The shared gate denies both consumers when disabled, while other option validation and enrichment settlement are preserved. |
| Ledger M2 | Accept | Adopt option (a). T000 blocks all Build tasks. Remove redundant conditional branches downstream of that hard block and retain the global owner prerequisite. |
| Ledger M3 | Accept | Remove the default and add disabled normalization in the same post-Gate-1 step. Identify dependent hosts and fix test fixtures shown by a gate to rely on the default. Use Q4 provisioning for environments and keep committed host config outside this scope. |
| Ledger M4 | Accept | Move the external-ID capability and evidence-write path into Phase 2 after the evidence model, before selection consumes the source. Keep tests ahead of the corresponding implementation. |
| Ledger L1 | Accept | Keep a single normative statement of gate labels in T019 and refer to it elsewhere. Preserve all existing verdict categories. |
| Ledger L2 | Accept | State directly that the existing test projects may receive new feature tests. |
| Ledger L3 | Accept | Keep external-ID lookup discovery-only and prove unchanged enrichment output and public contract. Run contract drift verification under T019. |

Compass reported zero findings. Total **20 accepted, 0 rejected, 0 deferred**. Gate 1 remains closed on the unchecked Principle VII owner question.

### PLAN FROZEN — Phase 2 receipt

Keel independently checked Quill's repaired artifacts and private plan/tasks mirrors after both Phase 2 read-backs. Read-only `speckit-analyze` prerequisites exited 0 with `PYTHONUTF8=1`. All 15 functional requirements and 8 success criteria map to the 21 tasks, and all named dependencies point to earlier tasks in execution order. The plan and task mirrors match byte for byte. The three-axis challenge closes with 20 accepted, 0 rejected, and 0 deferred findings.

The draft Spec Kit PR may be prepared with the exact unchecked owner checkbox in `spec.md`. Principle VII remains **blocked: structural**. Gate 1 and T000 remain closed until the owner answers affirmatively and merges the Spec Kit PR carrying the actual narrow constitution amendment. T000 records the verified merge and amendment commit. An owner rejection stops this draft for re-planning.

### T000 Gate 1 receipt

Verified at pre-Build HEAD `3496308bd128e9215ac6c90d1d23ac0bb17405d0` on `feature/dev-571`.

- The [Spec Kit PR #504](https://github.com/lamuniercosta/lamuflix/pull/504) is merged at `1aa1d5762e5c4fdccf60924b3377ece2f225f5d0`. That commit is an ancestor of the rebased feature HEAD.
- The [governance PR #508](https://github.com/lamuniercosta/lamuflix/pull/508) carries the checked affirmative owner Gate 1 checkbox and is merged at `9e94faa16358ffeefdd0c76a75a79264ee7486bc`. That commit is the worktree's `origin/main` and its merge base with the feature HEAD.
- [Constitution 2.1.0](../../.specify/memory/constitution.md) carries the sync-impact record and the narrow Principle VII exception at that commit. The worktree copy matches the merged text. The exception normalizes only the raw `Omdb:Quota:DailyLimit` key. A disabled allowance keeps the host and cached or confirmed-absence evaluation available, with zero live OMDb sends for discovery and enrichment. Other startup validation and enrichment quota settlement are preserved. Q4 requires provisioning and reading back a positive allowance before deploying the revised guard where OMDb is already in use.

Gate 1 is verified and T000 is complete. T001-T020 are unlocked in the frozen task order.
