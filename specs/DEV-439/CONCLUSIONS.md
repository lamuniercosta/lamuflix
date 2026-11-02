# DEV-439 Grill Conclusions

Worktree: `F:/Dev/LamuFlix.worktrees/feature-439-spec`; branch: `feature/439-spec`; HEAD: `c2d78146b359bd17ab36ecaa9f33342272d47ebf`.

---

## Q1/12 — Delivery boundary and Phase A Gate 1 owner-approval treatment

**Verdict:** Confirmed — freeze the six ticket Scope & Technical Design items and six Acceptance Criteria verbatim as the DEV-439 delivery boundary, with the ticket-mandated Principle IV constitution list amendment remaining an explicit unresolved owner approval at Phase A Gate 1 and no other structural checkbox indicated.

- **Frozen delivery boundary**: Deliver verbatim DEV-439 Scope & Technical Design items 1–6 and all six Acceptance Criteria, keeping quota deferral (`EnrichmentDeferredUntil`, delayed re-dispatch queue, claim/sweeper changes for DEV-333) and gate/`omdb_daily_usage`/`DailyLimit` changes out of scope (`recon-DEV-439:64-78,85-88`, `specs/PRODUCT.md:23-24`, `lamuflix-team-charter:28`).
- **Owner-gate treatment & cost**: Scope item 6 and AC 6 mandate amending the Principle IV closed `EnrichmentFailureCategory` list (`.specify/memory/constitution.md:309-314`) with explicit owner approval at Phase A Gate 1 before merge; Patron's ruling cannot close that owner gate, and if the owner rejects the amendment at Gate 1, `DEV-333` Scope 3 remains `blocked: structural` (`recon-DEV-439:69,78,82-83,101`, `specs/PRODUCT.md:36-39`, `lamuflix-team-charter:25-28,38`).
- **No other structural checkbox**: All files and contract surfaces are ticket-named and touch no dependency, project/layer, database schema, public API shape, `Features:LocalPlay`/secrets/`Process.Start`, or unnamed tracked file (`recon-DEV-439:90-103`, `specs/PRODUCT.md:41-48`, `lamuflix-team-charter:18-24`).

### Q1 complete exchange receipt (Keel)
**Question:** DEV-439 grill Q1/12: I have read DEV-439 and recon-DEV-439. Recommend freeze the six ticket scope items and six acceptance criteria verbatim as the delivery boundary, with the ticket-mandated constitution list amendment remaining an explicit unresolved owner approval at Phase A Gate 1; your ruling cannot close that owner gate. No other structural checkbox is currently indicated. Basis: recon-DEV-439 lines 64-88 and 101. Cost: DEV-333 remains blocked if owner rejects. Confirm this boundary and owner-gate treatment, with cited basis. Please answer this one question only.
**Answer (Patron):** DEV-439 Q1/12 confirmed: freeze the six Scope & Technical Design items and six Acceptance Criteria verbatim as the delivery boundary (out-of-scope deferral/gate items excluded), with the Principle IV constitution list amendment (.specify/memory/constitution.md:309-314) remaining an explicit unresolved owner approval at Phase A Gate 1 that Patron cannot close (cost accepted: DEV-333 Scope 3 stays blocked if owner rejects) and no other structural checkbox indicated. Basis: recon-DEV-439:64-103, .specify/memory/constitution.md:309-314, specs/PRODUCT.md:23-24,36-48, lamuflix-team-charter:18-28,38; recorded at specs/DEV-439/CONCLUSIONS.md.
**Rationale and implications:** Preserve the authoritative ticket and its explicit owner gate; no deferral implementation or implied approval.

---

## Q2/12 — Category semantics and vocabulary
**Question (Keel):** Recommend QuotaExhausted.IsRetryable=true, code quota_exhausted, and a description meaning the local OMDb daily request quota admission was denied; distinguish provider HTTP 429 as RateLimited and quota persistence failure as ProviderUnavailable. Chain fallback includes QuotaExhausted wherever RateLimited is eligible; do not turn other failures into fallback. Basis: recon-DEV-439:18-24,64-67,73-76. Cost: retries can exhaust until DEV-333 changes settlement. Confirm this category semantics and vocabulary ruling with cited basis.
**Answer (Patron):** DEV-439 Q2/12 confirmed: QuotaExhausted uses const code quota_exhausted, IsRetryable=true, and a SafeDescription stating local OMDb daily request quota admission was denied; OmdbMetadataProvider maps MetadataQuotaExhaustedException to QuotaExhausted while provider HTTP 429 stays RateLimited and MetadataQuotaPersistenceException stays ProviderUnavailable; ChainedMetadataProvider fallback eligibility includes QuotaExhausted wherever RateLimited is eligible without expanding fallback to other categories, and retries may exhaust to Failed until DEV-333 changes settlement (cost accepted; Keel owns CONCLUSIONS.md and brief.md writes from Q2 onward). Basis: recon-DEV-439:18-24,64-67,73-76, src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs:10-52, .specify/memory/constitution.md:309-314, specs/PRODUCT.md:23-24.
**Rationale and implications:** Preserve retry compatibility while distinguishing local quota admission denial from provider throttling. The glossary term describes admission denial, not next-day deferral. Use a const literal and avoid mutable static initialization in mutated code.

---

## Q3/12 — Minimal approach and edit boundary
**Question (Keel):** Recommend minimal existing-surface implementation: extend category recognition/description/retryability and persisted-value converter, change only the quota-exhaustion exception mapping, add chain eligibility, and prove existing settlement emits quota_exhausted headers with the same attempt/delay/terminal behavior as RateLimited. Modify settlement logic only if required to preserve that parity; do not repair the separately observed FeaturesConsumer InvalidResponse hardcoding or other unrelated behavior in this ticket. Existing test suites and ticket glossary/constitution are supporting edits; no new abstraction, dependency, migration, public API or web contract. Basis recon-DEV-439:13-37,64-88. Cost: unrelated divergences remain follow-ups. Confirm this approach and edit boundary, with cited basis.
**Answer (Patron):** DEV-439 Q3/12 confirmed: approve the minimal existing-surface implementation and edit boundary (extend EnrichmentFailureCategory and EnrichmentFailureCategoryConverter.Known, change only OmdbMetadataProvider MetadataQuotaExhaustedException mapping to QuotaExhausted, add QuotaExhausted to ChainedMetadataProvider.IsEligible, prove existing settlement emits quota_exhausted headers with RateLimited retry/delay/terminal parity and touch settlement code only if AC-forced for that parity, update existing test suites plus CONTEXT.md and .specify/memory/constitution.md:309-314, with zero new abstractions/dependencies/migrations/public API/web changes and FeaturesConsumer InvalidResponse hardcoding excluded as out of scope; noted, no ticket). Basis: recon-DEV-439:13-37,64-103, EnrichmentFailureCategory.cs:10-52, EnrichmentFailureCategoryConverter.cs:14-21, ChainedMetadataProvider.cs:57-59, specs/PRODUCT.md:18,23-24,41-48, lamuflix-team-charter:18-28,38.
**Rationale and implications:** Reuse existing settlement behavior; the new category is the contract change. FeaturesConsumer divergence is noted only; no follow-up ticket is authorized by this ruling.

---

## Q4/12 — Test strategy, task ordering, gate expectations
**Question (Keel):** Recommend tests covering (1) category code, description, recognition, retryability and existing values; (2) rejected real handler admission yields QuotaExhausted and zero transport calls, independent HTTP 429 remains RateLimited, persistence-failure mapping stays ProviderUnavailable; (3) quota fallback to TMDb success and unchanged noneligible failures; (4) RateLimited/QuotaExhausted settlement parity for retry attempts, delay, headers and terminal Failed at MaxAttempts; (5) quota_exhausted converter and actual database round-trip using existing Testcontainers patterns, no mocked driver. Order category/persistence tests and implementation, provider mapping, chain, settlement parity, then full gates; glossary and constitution amendment drafted in Phase A, owner Gate 1 before Phase B. Code gates run in implementation, not claimed by this documentation phase; pre-PR mutation eligible projects use harness rules without threshold changes. Cost: real-infrastructure validation and full gates take time. Basis recon-DEV-439:13-37,64-88 plus repository test and gate rules. Confirm test strategy, ordering and gate expectations.
**Answer (Patron):** DEV-439 Q4/12 confirmed: approve test strategy (1)-(5) covering all five code ACs plus existing invariants (including real MetadataQuotaHandler zero-transport rejection, HTTP 429 RateLimited and persistence-failure ProviderUnavailable preservation, TMDb chain fallback, RateLimited/QuotaExhausted settlement/header/retry/terminal parity, and converter + real Testcontainers DB round-trip with no mocked driver), the task ordering (category/persistence -> provider mapping -> chain -> settlement parity -> full gates), and Phase A/B gate boundaries (CONTEXT.md and constitution amendment drafted in Phase A with owner Gate 1 approval before Phase B; all build/analyzer/test/mutation gates via scripts/run-mutation.ps1 without since run in Phase B with zero threshold reductions; cost accepted). Basis: recon-DEV-439:13-37,64-88, AGENTS.md Non-negotiables & Verification commands, specs/PRODUCT.md:12,23-24, lamuflix-team-charter:9-12,73-83.
**Rationale and implications:** Validate observable behavior and real persistence, preserve neighboring classifications, and keep documentation approval separate from executable gate evidence.

---

## Q5/12 — Loop discipline
**Question (Keel):** Recommend loop discipline: closing bar is zero retained Critical/High/Medium findings for code-review and ship-review, every Low explicitly dispositioned, all three review axes Sentry/Ledger/Compass required on this M ticket, and required runnable gates evidenced separately; any blocking failure or missing axis stays blocked. Freeze Q1-Q4 delivery and supporting tests/docs; anything else is a follow-up issue, not a finding in this round. Round cap two review/fix rounds per loop, then stop and report blockers rather than inventing more rounds or waiving evidence; owner Gate 1 remains independent. Cost: unresolved blockers after cap prevent clearance. Basis ticket boundary recon-DEV-439:64-88 and assigned role review rules. Confirm closing bar, frozen scope wording and round cap.
**Answer (Patron):** DEV-439 Q5/12 confirmed: approve the standing review loop discipline — closing bar of zero retained Critical/High/Medium findings for code-review and ship-review with every Low explicitly dispositioned, all three challenger axes (Sentry, Ledger, Compass) required for this size:M ticket, and all required runnable gates evidenced separately (any blocking failure or missing axis stays blocked); freeze Q1-Q4 delivery and supporting tests/docs with the wording that anything else is a follow-up issue, not a finding in this round; cap at 2 review/fix rounds per loop before hard-stopping and reporting unresolved blockers with zero evidence waivers (cost accepted); and keep owner Gate 1 independent. Basis: DEV-439:6, recon-DEV-439:64-88,101, AGENTS.md Verification commands, specs/PRODUCT.md:12,23-24,36-39, lamuflix-team-charter:18-38,50-52,85-89.
**Rationale and implications:** Bounded review has an explicit quality bar; missing evidence, exhausted rounds, and owner approval cannot be silently waived.

---

## Q6/12 — Editorial wording and shared-understanding closure
**Question (Keel):** Recommend exact SafeDescription wording Local OMDb daily request quota admission was denied. and glossary meaning QuotaExhausted is an enrichment failure caused by denial of admission under the local OMDb daily request quota; it is distinct from provider HTTP 429 and does not itself imply deferral. Record this editorial wording as assumed in ASSUMPTIONS.md with your cited ruling. Close the grill at 6 questions with Q1-Q5 frozen, no new ADR or new domain model, and Quill drafting spec/plan/tasks plus the ticket glossary and constitution amendment from the brief; Keel then analyzes read-only before plan challenge. Owner constitution approval remains open, and no implementation gate or merge approval is claimed. Confirm shared understanding and authorize closure on these terms, or identify the remaining decision.
**Answer (Patron):** DEV-439 Q6/12 confirmed and grill closure authorized (6/12 questions used, Q1-Q5 frozen): approve exact SafeDescription wording Local OMDb daily request quota admission was denied. and CONTEXT.md glossary wording QuotaExhausted is an enrichment failure caused by denial of admission under the local OMDb daily request quota; it is distinct from provider HTTP 429 and does not itself imply deferral., logged tagged [assumed] in specs/DEV-439/ASSUMPTIONS.md; close the grill with no new ADR or domain model, authorize Quill to draft spec/plan/tasks plus the CONTEXT.md glossary and .specify/memory/constitution.md:309-314 amendment from brief.md followed by Keel read-only speckit-analyze before plan challenge, with owner Gate 1 constitution approval remaining open and no implementation gate or merge approval claimed. Basis: recon-DEV-439:18-24,60,64-88,101, EnrichmentFailureCategory.cs:34-42, .specify/memory/constitution.md:309-314, specs/PRODUCT.md:23-30,36-39, lamuflix-team-charter:17-28,38.
**Rationale and implications:** The exact editorial copy is captured as assumed; no new architecture decision is required. The grill is closed, Quill can draft, and owner approval remains unresolved.

---

## Plan challenge P1 — Settlement action parity and supporting-file authorization

**Question (Keel):** DEV-439 plan challenge decision P1: recon-DEV-439-2:9-23,53-56,71 now proves QuotaExhausted would get Retry rather than RateLimited RetryDelayed, although both actions currently use the same TTL queue. Recommend settlement parity include the Action value as well as transport/attempt/header/terminal behavior, so explicitly plan the minimal EnrichmentRetryPolicy.Decide condition extension at line 19 to include QuotaExhausted. Test parity red before that edit. Supersede Q3 conditional-edit wording only for this proven site; other settlement production edits remain AC-forced. Scope 4 and AC4 already require exact RateLimited parity (recon-DEV-439 ticket scope 4/AC4); changing this unnamed file is a deliberate Patron-approved supporting edit under charter 2.3 item 6, not a ticket contradiction or user escalation. Ledger H1 remains a plan repair, but its immediate-vs-TTL transport claim is false and must be corrected. Confirm or revise this one decision with cited basis. Keel will record complete exchange in brief/CONCLUSIONS; please do not edit files. Report back using maestri ask Keel.

**Answer (Patron):** DEV-439 P1 confirmed: include EnrichmentFailureAction.RetryDelayed parity in QuotaExhausted settlement parity and explicitly plan the minimal EnrichmentRetryPolicy.Decide condition extension at src/LamuFlix.Core/Features/Enrichment/EnrichmentRetryPolicy.cs:19-21 to cover QuotaExhausted alongside RateLimited (with red parity test first), superseding Q3 conditional-edit wording solely for that proven site while other settlement production edits remain AC-forced; approve editing unnamed tracked file EnrichmentRetryPolicy.cs as an AC-forced supporting edit under charter 2.3 item 6; and retain Ledger H1 as a plan repair while correcting its false immediate-vs-TTL transport claim (Retry and RetryDelayed share the TTL retry queue today). Basis: recon-DEV-439:67,76 (Scope 4/AC4), recon-DEV-439-2:9-23,53-56,71, EnrichmentRetryPolicy.cs:19-21, EnrichmentRouting.cs:27-34, findings-DEV-439-Ledger:7-10, lamuflix-team-charter:18-24.

**Effect:** Q3 conditional settlement-edit wording is superseded only for EnrichmentRetryPolicy.Decide. Explicitly plan this minimal edit and red-first action-parity test. No routing, deferral, sweeper, gate or classifier edit is authorized by P1. Other settlement edits remain AC-forced. No ticket contradiction or new owner checkbox is introduced; ticket-mandated owner constitution Gate 1 remains unresolved.


---

## Plan challenge P2 — Complete adjudication and repair authorization

**Question (Keel):**

[from Keel] DEV-439 plan challenge P2: P1 complete Q&A appended and read back in brief.md and CONCLUSIONS.md. Please rule on this complete 21-finding disposition proposal and repair list using recon-DEV-439 and recon-DEV-439-2. This is adjudication round 1, not a fresh grill or repair round. Do not edit files; Keel records your full answer. Reply with individual exceptions or confirm every ID, cited basis, owner/departure treatment, and NEEDS REPAIRS verdict. Report back via maestri ask Keel.

Sentry:
S-M1: downgrade Medium to Low documentation repair: quota exceptions are caught before classifier (recon-2:42-48). Cite that proof, keep classifier unchanged, name its existing survivor suite in regression inventory. Do not add quota classifier behavior.
S-M2: retain Medium, remove discretionary settlement suite selection. Mandatorily cover Decide RetryDelayed action parity, both ProcessEnrichment and RecordEnrichmentFailure settlement call paths, routing/delay, headers, max-attempt terminal state. T000 fresh recon rechecks identity-map drift before coding; T010 inspection unconditional. recon-2:9-23,60-63.
S-M3: retain Medium, T006 must name composed real MetadataQuotaHandler -> OmdbMetadataProvider proof in tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs, denied StubGate via scoped factory, RecordingHandler inner transport counter zero and returned QuotaExhausted. Handler-only/ThrowingHandler tests do not substitute; keep isolated mapping and neighbor checks. recon-2:65-67.
S-L1: accepted residual until DEV-333; no repair or deferral work, ticket scope4.
S-L2: out of scope, noted only; no follow-up ticket authorized, per Q3 (contrary to report suggestion).
S-L3: reject speculative extra metric test requirement: recon lists suite, no demonstrated tag allow-list failure; full suite includes metrics. No production observability work or new assertions mandated. recon-2:13 shows category.Code metric tag.
S-L4: retain Low risk documentation: converter throws on unknown code (recon-2:10). Record old-reader/rollback risk for persisted quota_exhausted without inventing rolling deployment support, migration, tolerant-reader code or deployment mechanism. No new scope or owner checkbox.

Ledger:
L-H1: retain as Medium plan repair under P1 (observable transport already equal; missing Action parity makes explicit minimal edit required). Correct immediate-vs-TTL false claim; red T009 test, T010 minimal Decide edit, T011 mandatory green reproof. recon-2:9,17,53-56,71.
L-M1: downgrade Medium to Low citation/documentation: no production quota path to features/embedding allow-lists (recon-2:42-48). Deliberately leave them unchanged; pickup verifies drift.
L-M2: downgrade Medium to Low precision: remove if-present and ambiguous relative paths; use exact fully qualified production/test paths now established by recon-2:13-14,60-67.
L-M3: reject as blocker: Phase B legitimately includes pickup, implementation and verification, and explicit dependency graph already states T021 before T000 before T001 etc. Stable IDs need not be renumbered; optional wording may say Phase B pickup/implementation/verification consistently. No new Phase P.
L-L1: accept Low: gate suites regression-run only, not edited for zero-transport; stub gate suffices at composed handler seam. DB round-trip still real Testcontainers, no mocked serialization.
L-L2: accept Low receipt-only clarification for T021: Keel records verified OWNER answer and merge receipt, not a Patron approval or new ruling. Existing Q2 delegation authorizes Keel recording; no role violation. Owner checkbox remains unresolved.
L-L3: reject subjective header/seat-name cleanup: current status/ownership truthful and governed by brief, no concrete failure or mandatory standard; no repair or new artifact required.
L-L4: accept Low constitution-check clarification: provider catches quota per explicit ticket scope2, classifier handles escaped exceptions; existing boundary remains unchanged. Recommend no new constitution departure, no classifier edits, no broad single-classifier rewrite. Please explicitly rule on this tension using recon-2:42-48 and ticket scope2; a necessary constitutional departure would require owner escalation, not assumed clearance.

Compass:
C-H1: High concern resolved by recon-2:28,34-35 locating OMDb StatusFailure 294-317 and existing primary/fallback 429 tests. No ticket contradiction. Low citation repair: anchor preserved behavior/tests and T000 drift recheck, no wait until Phase B to establish current fact.
C-M1: downgrade Medium to Low and dedupe with S-M1/L-L4: cite classifier reachability and existing survivor regression; no classifier edit.
C-L1: accept Low dependency correction: T003 after BOTH T001 and T002.
C-L2: accept Low, superseded by P1 and S-M2: mandatory T010 inspection + known Decide edit; T011 reproof mandatory, not optional MVP.
C-L3: evidence-resolved: TTL mechanism is established recon-2:53-56; add citation and pickup recheck, retain existing constitution TTL wording. No routing edits.
C-L4: accept Low seam precision; reject mandatory Testcontainers for composed quota denial. Real handler+stub gate+recording transport is enough (recon-2:67); real Testcontainers remains mandatory for persistence round-trip.

Consolidated numbered repair list for Quill:
R1 synchronize spec/plan/tasks with P1 explicit Decide edit, full Action parity and red-test -> edit -> mandatory green reproof; no immediate-vs-TTL claim.
R2 mandatory settlement coverage/inventory for both handlers/policy/routing/headers/terminal behavior, unconditional inspection and fresh recon drift check.
R3 anchor HTTP429, classifier quota-unreachability, features/embedding exclusion and TTL mechanism to recon-2; name existing classifier regression suite without new quota classification.
R4 mandate T006 composed unit seam and exact location; preserve isolated exhaustion/fallback, 429 and persistence-failure checks; remove and/or substitutions and EF requirement.
R5 fully qualify evidenced paths, remove if-present; gate suites regression only, actual DB round-trip stays Testcontainers.
R6 add unknown persisted-code old-reader/rollback risk, no new deployment feature.
R7 make T021 verified owner-answer+merge receipt only; keep unchecked Gate1 constitution question.
R8 fix T003 dependency and MVP/dependency text for mandatory settlement inspection/edit/reproof.

Verdict proposed NEEDS REPAIRS, not CLEAN/PLAN FROZEN. Preserve Q5 two-round cap and all axes; no gate pass or implementation clearance. Only owner checkbox remains ticket-mandated constitution amendment; P1 supporting-file permission is Patron-owned, no ticket contradiction; provider mapping is explicit ticket scope with existing classifier boundary, no new constitution departure proposed.

**Answer (Patron, complete written ruling):**

DEV-439 P2 confirmed: approve all 21 finding dispositions (Sentry S-M1..S-L4, Ledger L-H1..L-L4, Compass C-H1..C-L4), the consolidated repair list R1-R8 for Quill, the constitutional/owner-gate treatment, and the round-1 adjudication verdict NEEDS REPAIRS (not CLEAN/PLAN FROZEN, preserving Q5 two-round cap and all three axes, with zero implementation clearance or gate pass claimed).
- Finding dispositions & R1-R8 repairs: Confirm every proposed severity, downgrade, retention, and rejection across S-M1..S-L4, L-H1..L-L4, and C-H1..C-L4, and authorize Quill to execute R1-R8 against spec.md, plan.md, and tasks.md (explicit EnrichmentRetryPolicy.Decide line 19 edit with red T009 -> T010 edit -> mandatory T011 green reproof and no immediate-vs-TTL false claim; mandatory settlement coverage across Decide, ProcessEnrichmentCommandHandler, RecordEnrichmentFailureCommandHandler, routing, headers, and terminal Failed with unconditional T010 inspection and T000 drift recheck; composed real MetadataQuotaHandler + denied StubGate + RecordingHandler zero-transport unit proof in OmdbMetadataProviderUnitTests.cs while keeping isolated/fallback/429/persistence-failure checks and Testcontainers strictly for DB round-trip; exact fully qualified paths with gate suites regression-only; old-reader/rollback risk noted for unknown persisted quota_exhausted code in EnrichmentFailureCategoryConverter without rolling-deploy or tolerant-reader scope; S-L2 FeaturesConsumer divergence noted, no ticket per Q3; T021 scoped to verified owner answer + merge receipt only; T003 after both T001 and T002) (recon-DEV-439:64-88; recon-DEV-439-2:9-67,71; findings-DEV-439-Sentry; findings-DEV-439-Ledger; findings-DEV-439-Compass).
- Explicit ruling on L-L4 / S-M1 / C-M1 (Principle IV single-classifier boundary): No new constitution departure is introduced or required, and EnrichmentFailureClassifier remains untouched. Ticket Scope 2 explicitly mandates mapping MetadataQuotaExhaustedException inside OmdbMetadataProvider.FetchAsync (where it is already caught at OmdbMetadataProvider.cs:211-214 and returned as Fetch.Rejected before ever escaping to ProcessEnrichmentCommandHandler.LookupAsync's catch-all classifier call at ProcessEnrichmentCommandHandler.cs:73), while EnrichmentFailureClassifier continues to own root-cause classification for escaped exceptions; preserving this pre-existing boundary fulfills explicit ticket text without rewriting the classifier or triggering a §2.3b constitutional departure escalation (recon-DEV-439:18,65,74; recon-DEV-439-2:28,42-48; .specify/memory/constitution.md:332-333; lamuflix-team-charter:18-28).
- Owner checkbox & charter §2.3 boundary: The ticket-mandated Principle IV closed-list amendment (.specify/memory/constitution.md:309-314 / v1.9.0) remains the sole unresolved owner Gate 1 checkbox; the P1 supporting edit to unnamed tracked file EnrichmentRetryPolicy.cs:19-21 is Patron-approved as AC-forced under charter §2.3 item 6 with no ticket contradiction (§2.3a) or new constitution departure (§2.3b) (recon-DEV-439:67,69,76,78,101; recon-DEV-439-2:9,71; lamuflix-team-charter:18-28,38).


## Complete disposition register (21 of 21)

IDs retain the original axis and finding number. "Recon-2" means canvas note `recon-DEV-439-2`; every production path:line below is recon evidence, not an independent Keel search.

| ID | Patron-approved disposition | Basis and repair |
|---|---|---|
| S-M1 | Downgraded to Low; documentation repair | Quota exceptions are caught before classifier. Cite recon-2:42-48; existing classifier survivor suite in regression inventory, no quota classifier behavior. R3. |
| S-M2 | Retained Medium | Mandatory policy/action, both handler settlement paths, routing/delay, headers and terminal parity; unconditional inspection, fresh pickup identity-map recon. Recon-2:9-23,60-63. R1-R2. |
| S-M3 | Retained Medium | Mandate composed real handler-to-provider rejection and zero inner transport calls in the provider unit suite; handler-only tests cannot substitute. Recon-2:65-67. R4. |
| S-L1 | Accepted residual; no repair | Same-day attempts may exhaust until DEV-333; intentional ticket Scope 4, recon-DEV-439:67,87. No deferral here. |
| S-L2 | Out of scope; noted only | FeaturesConsumer divergence remains untouched; Q3 explicitly authorized no ticket. Do not file a follow-up from this finding. |
| S-L3 | Rejected speculative test requirement | No demonstrated metric tag allow-list failure; recon-2:13 emits category.Code, existing full suite includes metrics. No new metric assertions or production change mandated. |
| S-L4 | Retained Low risk documentation | Converter rejects unknown codes, recon-2:10. Record old-reader/rollback risk without inventing rolling-deploy support, migration or tolerant reader. R6. |
| L-H1 | Retained as Medium plan repair | P1 requires RetryDelayed Action parity and known Decide edit; immediate-vs-TTL claim rejected because both actions share TTL transport. Recon-2:9,17,53-56,71. R1. |
| L-M1 | Downgraded to Low documentation | No quota production path reaches features/embedding allow-lists; deliberately unchanged. Recon-2:42-48; pickup rechecks drift. R3. |
| L-M2 | Downgraded to Low precision repair | Replace if-present and ambiguous relative paths with evidenced full paths. Recon-2:13-14,60-67. R5. |
| L-M3 | Rejected as blocker | Phase B includes pickup, implementation and verification; explicit graph already establishes execution order. Keep stable IDs; no renumber or new Phase P required. Consistent optional labels allowed. |
| L-L1 | Retained Low | Gate suites regression-run only, no zero-transport edits; stub gate suffices. Real Testcontainers remains required for DB round-trip. Recon-2:66-67; Q4. R4-R5. |
| L-L2 | Retained Low receipt clarification | T021 records verified owner answer and merge receipt only. Q2 authorized Keel recording; no role violation or Patron substitution for owner approval. R7. |
| L-L3 | Rejected subjective cleanup | Truthful status/seat ownership creates no concrete failure or documented-rule violation; no new artifact or header cleanup required. |
| L-L4 | Retained Low clarification; no new constitution departure | Ticket Scope 2 mandates existing provider quota catch, classifier owns escaped exceptions. No classifier edit or broad constitution rewrite. Recon-DEV-439:65; recon-2:42-48. R3. |
| C-H1 | High concern resolved by recon; Low citation repair | OMDb StatusFailure:294-317 maps 429, primary and fallback tests exist. Recon-2:28,34-35. Cite now and pickup-recheck; no present ticket contradiction. R3. |
| C-M1 | Downgraded to Low; deduplicated repair | Same classifier reachability/documentation issue as S-M1/L-L4; existing survivor regression, no classifier edit. Recon-2:42-48,62. R3. |
| C-L1 | Retained Low | T003 depends on both T001 and T002 for converter round-trip. R8. |
| C-L2 | Retained Low; P1 supersedes conditional wording | Mandatory T010 inspection and known Decide edit; T011 mandatory green reproof, including MVP/dependency text. R1-R2,R8. |
| C-L3 | Evidence-resolved; citation repair | TTL mechanism established, both actions share queue. Recon-2:53-56. Cite and pickup-recheck, retain constitution TTL wording, no routing edit. R3. |
| C-L4 | Partially retained Low seam repair | Name real-handler/stub-gate/recording-transport unit composition. Reject mandatory Testcontainers for denial proof; DB round-trip still real. Recon-2:67. R4. |

## Numbered repair list for Quill (round 1)

1. **R1 — Explicit settlement action parity.** Synchronize spec Scope/US4/FR-005/SC-004/assumptions, plan approach/structure/risks/order, and T009-T011 with P1. Plan the minimal `src/LamuFlix.Core/Features/Enrichment/EnrichmentRetryPolicy.cs:19-21` condition extension for QuotaExhausted beside RateLimited. T009 must first fail on missing RetryDelayed action parity; T010 performs the minimal edit; T011 must re-prove green. Both existing actions already use TTL transport; remove any immediate-vs-TTL claim. Other settlement production edits remain AC-forced.
2. **R2 — Mandatory settlement proof and pickup inventory.** Remove discretionary and/or suite selection for the policy, `ProcessEnrichmentCommandHandler`, `RecordEnrichmentFailureCommandHandler`, routing/delay, category header and MaxAttempts terminal state. Name existing tests from recon-2:60-63, including `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs` for TTL/header behavior. Inspection is unconditional, and T000 obtains fresh identity-map/contract drift recon. This authorizes supporting tests, not new routing/deferral policy or gratuitous production edits.
3. **R3 — Evidence anchors and classifier boundary.** Cite recon-2:28,34-35 for preserved provider 429 behavior and primary/fallback tests; :42-48 for caught quota exceptions, unchanged classifier and unreachable features/embedding allow-lists; :53-56 for existing TTL mechanism. Include `tests/LamuFlix.UnitTests/Features/Enrichment/EnrichmentFailureClassifierSurvivorTests.cs` in the existing regression inventory (FR-006/T017). Recheck at pickup. No new quota classifier knowledge, features allow-list edit or constitutional single-classifier rewrite.
4. **R4 — Mandatory zero-transport composition.** T006 names `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs`: real MetadataQuotaHandler resolves a denied StubGate through a scoped factory, wraps a RecordingHandler, and is used by the provider HttpClient; assert returned QuotaExhausted and zero inner transport calls. Recon-2:65-67 supplies the existing seams. Keep isolated ThrowingHandler mapping, fallback, 429 and persistence-failure checks; handler-only tests are extras, never substitutes. No EF/Testcontainers required for this unit seam; actual database serialization round-trip remains real Testcontainers.
5. **R5 — Precise paths and regression boundary.** Fully qualify evidenced production and test paths, remove if-present and relative-project ambiguity. Use recon-2:13-14,60-67, including chain unit/composition test paths. Mark `EfMetadataRequestGateTests.cs` / `EfMetadataRequestGateConcurrencyTests.cs` regression-run only, not edits for denial proof. Do not invent missing paths or alter the gate.
6. **R6 — Persisted-code compatibility risk.** Document that old readers lacking quota_exhausted in Known reject it, so rollback/mixed-version reading of newly persisted values carries a compatibility risk (recon-2:10). Do not claim a deployment topology or add tolerant-reader code, migration or rolling-deployment support.
7. **R7 — Owner receipt only.** T021 records the verified owner checkbox answer and spec-PR merge receipt; it does not create a ruling or infer approval. Keep the sole owner constitution-amendment checkbox unchecked and blocking until actual approval/merge. Patron cannot close it.
8. **R8 — Dependencies and MVP.** T003 follows both T001 and T002. Make settlement inspection, the P1 known edit and T011 reproof mandatory in dependencies, diagrams and MVP text. Keep stable task IDs and the explicit T021 -> T000 -> implementation/verification order.

## Verdict and boundaries

**NEEDS REPAIRS — plan challenge adjudication round 1.** All three axes are present; all 21 findings have an explicit Patron-approved disposition. Quill is authorized by P2 to execute R1-R8 against `spec.md`, `plan.md`, and `tasks.md`. Repair completion must be read back before any CLEAN / PLAN FROZEN claim. Preserve the Q5 two-round cap; this ruling consumes no extra review round or waiver.

The sole unresolved owner checkbox remains the ticket-mandated Principle IV closed-list amendment (proposed constitution 1.9.0), at Gate 1. P1 is a deliberate Patron approval of the supporting unnamed retry-policy edit under charter 2.3 item 6. There is no present ticket contradiction and no new constitutional departure. P2 explicitly rules that provider quota mapping preserves the existing escaped-exception classifier boundary. No production edit, implementation clearance, executable gate result, owner approval or merge approval is claimed.

---

## Phase 2 Gate 1 Provisional Ruling

**Verdict:** Confirmed `gate1: provisional` — plan is frozen after R1–R8 repair closure, with 0 new constitution departures, 0 ticket contradictions, and the ticket-mandated Principle IV constitution-amendment owner checkbox remaining open (`specs/DEV-439/brief.md:258-279`, `specs/DEV-439/spec.md:5,8-12`, `recon-DEV-439:64-103`).

- **Plan frozen & challenge adjudicated**: All 21 Sentry/Ledger/Compass findings are adjudicated under P1–P2 and R1–R8 repairs are verified closed in `spec.md`, `plan.md`, and `tasks.md` (`specs/DEV-439/brief.md:258-274`, `specs/DEV-439/CONCLUSIONS.md:57-164`).
- **Zero new constitution departures & zero ticket contradictions**: Provider quota mapping preserves the existing `OmdbMetadataProvider.FetchAsync` catch boundary before `EnrichmentFailureClassifier`, and `EnrichmentRetryPolicy.cs:19-21` is an AC-forced supporting edit under charter §2.3 item 6 (`recon-DEV-439:64-78`, `recon-DEV-439-2:9-23,42-48,71`, `lamuflix-team-charter:18-28`).
- **Owner Gate 1 checkbox open**: The ticket-mandated Principle IV closed `EnrichmentFailureCategory` list amendment (`.specify/memory/constitution.md:309-314`, v1.8.0 → v1.9.0) remains unchecked at `specs/DEV-439/spec.md:10`; Gate 1 stays closed until owner spec-PR merge (`recon-DEV-439:69,78,101`, `specs/PRODUCT.md:36-39`).

---

## T021 — Verified owner Gate 1 answer and spec-PR merge receipt

Recorded by Keel for Phase B pickup in `F:/Dev/LamuFlix.worktrees/DEV-439`, branch `feature/DEV-439`, picked HEAD `993eeebb3068608e232503aaef84422af8885b32`.

**Verified owner answer: approved [x].** The owner checkbox in the body of [spec PR #301](https://github.com/lamuniercosta/lamuflix/pull/301) is checked for the Principle IV `QuotaExhausted` closed-list amendment, constitution 1.8.0 to 1.9.0. Repository owner `lamuniercosta` merged that PR at `2026-11-02T00:50:57Z`; state `MERGED`; merge commit `6e1ce27b4340a2ec85f38fa5965b5f68303877e7`. Source: Conductor live verification recorded in `recon-DEV-439:91-97`, read back by Keel. Landed-main ancestry and picked identity are recorded at `recon-DEV-439:5-9,18-19`.

This records the actual owner answer and merge receipt required by Q1, FR-008, SC-006 and R7; it introduces no Patron ruling or inferred approval. The unchecked checkbox and provisional/open statuses in the Phase A documents and `recon-DEV-439:86` are historical drafting state, superseded for Gate 1 by this verified receipt. **T021 complete; Gate 1 approved.** T000 pickup analysis follows before implementation clearance.


---

## T000 — Phase B pickup and cross-artifact consistency clearance

Performed after recording and reading back T021 above. Picked HEAD and origin/main were confirmed equal at `993eeebb3068608e232503aaef84422af8885b32` on `feature/DEV-439` in `F:/Dev/LamuFlix.worktrees/DEV-439`. Fresh Phase B evidence is the current `recon-DEV-439`, not the historical Phase A recon with the same note name. Its intake-only disclaimer at lines 86-87 is not a clearance; this analysis supplies the required post-T021 clearance using that refreshed evidence, as directed by Conductor.

**Drift disposition:** recon lines 18-26 show zero C# drift from Phase A base `c2d78146b359bd17ab36ecaa9f33342272d47ebf` to picked main, and no web/project/build-contract drift. Constitution and glossary changes are the landed Phase A deliverables. The constitution list citation moved from 309-314 to 330-331 (retry parenthetical 335, version 687); recon lines 47-48 refresh that locator without changing the contract. No implementation task repair is required. The historical unrelated FeaturesConsumerTests diff is absent at this pickup (line 70); preserve unrelated work if encountered later.

**Read-only speckit-analyze result: no unresolved cross-artifact or constitution blocker.** Reviewed frozen brief Q1-Q6/P1-P2, spec requirements/stories/criteria, plan approach/files/tests/gates/order and tasks against the fresh identity and contract map at recon lines 28-71. The standard prerequisite helper could not resolve the tracker-style branch `feature/DEV-439` because it expects a numeric feature prefix; analysis used the explicitly assigned `specs/DEV-439` paths. No before_analyze or after_analyze hook is registered. This helper failure is disclosed, not reported as a passing executable gate.

| Requirement | Task coverage | Picked-head evidence / consistency |
|---|---|---|
| FR-001 | T001, T003 | Category code, exact description, retryability and existing values; recon 32,60. |
| FR-002 | T002-T004 | Known registration, converter and real DB round-trip; recon 33,61,71. |
| FR-003 | T005-T006, T017 | Exhaustion caught in provider before classifier; 429 stays RateLimited, persistence failure stays ProviderUnavailable; gate/handler unchanged; recon 34,37,42,55,62-63,69. |
| FR-004 | T007-T008 | QuotaExhausted added beside existing fallback eligibility only; recon 35,53,64. |
| FR-005 | T009-T011 | Mandatory red RetryDelayed parity, minimal Decide edit, green reproof across both handlers/routing/consumer; shared TTL transport and category header preserved; recon 36,38-41,52,56,65-68. |
| FR-006 | T003-T004, T006, T008-T011, T017 | Mandatory suites and real-handler/denied-gate/recording-transport composition retained; composed provider zero-transport proof remains to implement, never replaced by handler-only tests; recon 58-71. |
| FR-007 | Landed Phase A, T012 | Exact glossary wording present; recon 47. |
| FR-008 | Landed Phase A, T021, T012 | Constitution 1.9.0 list and owner approval receipt verified; recon 48,91-97. |
| FR-009 | T000, T012 | Frozen edit boundary and unchanged public contracts; recon 24,43,70,73-82. |
| FR-010 | T000, T013-T020 | Gates, three review axes, explicit exit dispositions and two-round cap preserved. |

SC-001 maps to T006; SC-002 to T006/T017; SC-003 to T008; SC-004 to T009-T011; SC-005 to T003-T004; SC-006 to T021; SC-007 to T013-T020. Coverage: all 10 functional requirements and 7 success criteria have delivery or verification coverage; all 22 task IDs map to requirements or prerequisite evidence. No retained ambiguity, duplication, uncovered requirement or new constitution conflict was identified. Phase A open/draft status prose is historical and is superseded by this receipt and T021 for present pickup state; frozen delivery decisions remain intact.

**T000 complete; Phase B pickup CLEARED at the pinned HEAD.** T021 precedes T000, and T001-T020 may now proceed in the frozen dependency order. This clears pickup only: implementation, runnable gates, Sentry/Ledger/Compass review, hosted checks and merge-bar evidence remain outstanding. No production code was edited and no executable gate pass is claimed.
