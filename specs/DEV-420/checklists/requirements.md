# Requirements Quality Checklist: DEV-420 Relay outbox rows to RabbitMQ with publisher confirms and remove the direct enqueue

**Purpose**: Unit tests for English — validate requirement completeness, clarity, consistency, measurability, and primary/exception/recovery coverage before Keel analyze and Gate 1
**Created**: 2026-10-25
**Feature**: [spec.md](../spec.md)
**Depth**: Standard (L path)
**Audience**: Reviewer (spec PR) / Keel analyze
**Focus**: Confirmed relay, failure/backoff, retention, readiness/metrics, two-handler cutover; Q9 boundary and Phase B prerequisites

## Content Quality

- [x] CHK001 Are implementation names limited to those already fixed in `brief.md` Q3–Q9 (type/file names, option names, metric names, health check name) rather than Quill-invented stack choices? [Clarity, Spec Input]
- [x] CHK002 Do user stories describe observable outcomes (confirmed publish, failure abort, retention, readiness, cutover) a reviewer can test without inventing a ruling? [Completeness, Spec §User Stories]
- [x] CHK003 Are all mandatory Spec Kit sections present (Clarifications, Domain vocabulary, User Scenarios, Requirements, Success Criteria, Assumptions)? [Completeness]
- [x] CHK004 Is the specification free of unresolved `[NEEDS CLARIFICATION]` markers? [Completeness]

## Requirement Completeness

- [x] CHK005 Are ticket Scope lines (task note DEV-420:27-30) each represented by at least one FR or acceptance scenario (relay confirms, backoff, cutover, retention, health/backlog, Postgres+RabbitMQ proof, multi-instance exclusion)? [Completeness, Spec §FR-001–FR-014]
- [x] CHK006 Are DEV-329 prerequisite contracts adopted unchanged and Phase B landed-dependency STOP documented? [Completeness, Spec §FR-008, Clarifications]
- [x] CHK007 Are per-row post-confirm markers, no transaction-across-broker-waits, and at-least-once redelivery after failed marker all specified? [Completeness, Spec §FR-002, US1]
- [x] CHK008 Are abort-first failure, poison head-of-line, capped backoff, and clean shutdown requirements all documented? [Completeness, Spec §FR-003, US2]
- [x] CHK009 Are processed-only retention bounds, cutoff equality retention, and cleanup isolation from publication specified? [Completeness, Spec §FR-005, US3]
- [x] CHK010 Are Ready-tagged `outbox-relay`, health precedence, six instruments with names centralized in `TelemetryConstants.cs` and consumed by `OutboxRelayMetrics`, gauge withholding on invalid samples, successful empty-sample count/age zero, independent pass/cleanup fault clearing, and the strict Q7 failure-log allowlist (row id when row-scoped, exception type name, stable category only; no exception objects/raw text/payload-derived values/sensitive config) specified? [Completeness, Spec §FR-006, US4; post-grill C1; plan-challenge Q7]
- [x] CHK011 Is two-handler-only direct-enqueue cutover with Stage/save preservation, known-precommit both-or-neither rollback, uncertain-save distinction, error propagation, and other-caller exclusion fully specified? [Completeness, Spec §FR-007, US5]
- [x] CHK012 Is the exact Q9 24-file boundary enumerated (including `TelemetryConstants.cs` as file 24 solely for the six Q7 constants) and additional-file ruling requirement explicit? [Completeness, Spec §FR-009, SC-007, plan.md; CONCLUSIONS.md:218-221]
- [x] CHK013 Are negative exclusions (no new dependency/schema/HTTP/Core relay port/publisher rewrite/LocalPlay; no further `TelemetryConstants.cs` edits beyond the six constants) explicit? [Completeness, Spec §FR-014, Edge Cases; brief Q9]

## Requirement Clarity

- [x] CHK014 Is feature-local vocabulary (`staged`/`durable`/`unprocessed`/`confirmed`/`processed`/`expired processed`) defined without collapsing ADR informal wording? [Clarity, Spec §Domain vocabulary, brief Q12]
- [x] CHK015 Is ordering stated as per-batch `(CreatedAt, Id)` only — not global FIFO or exactly-once? [Clarity, Spec §Domain vocabulary / US1]
- [x] CHK016 Are options defaults/bounds enumerated with validation rules rather than “reasonable defaults”? [Clarity, Spec §FR-004]
- [x] CHK017 Is marker-save token vs publish-timeout token distinction explicit? [Clarity, Spec §FR-004, Edge Cases]
- [x] CHK018 Is gauge withholding on invalid/failed samples explicit (never stale-as-current or false zero), and are successful empty-sample zeros plus independent pass/cleanup fault-clearing rules explicit? [Clarity, Spec §FR-006, US4; Clarifications]

## Requirement Consistency

- [x] CHK019 Do FR-002/US1 confirmed-marker rules align with ADR-0027 and SC-001/SC-002? [Consistency]
- [x] CHK020 Do FR-003/US2 poison head-of-line rules align with Q4 and SC-003 (no skip/quarantine columns)? [Consistency]
- [x] CHK021 Do plan Phase B entry checks and tasks TB000–TB003 agree with brief Q2/Q10 landed-dependency STOP? [Consistency, plan.md / tasks.md]
- [x] CHK022 Do tasks Phases 1–5 map to brief Q10 steps 2–6 without cutting over handlers before relay proofs, and is the TelemetryConstants constants task scheduled before OutboxRelayMetrics (T015a before T016) with updated parallel guidance? [Consistency, tasks.md; brief Q10 step 5]
- [x] CHK023 Does frozen scope in spec Edge Cases / FR-013 match brief Q11–Q12 including the post-grill C1 24-file Q9 boundary, with Draft ADR-0028 referenced as Draft (not accepted) and no Gate 1 claim — preserving historical Phase 2 step 3 predating-ADR notes and later-phase boundaries? [Consistency, Spec §FR-009/FR-013/SC-007; plan.md Phase boundaries; brief.md:137-176; Draft ADR-0028]

## Acceptance Criteria Quality / Measurability

- [x] CHK024 Can SC-001–SC-007 be objectively passed or failed from the stated proofs without inventing latency/volume targets? [Measurability, Spec §Success Criteria]
- [x] CHK025 Are real Postgres/RabbitMQ and controlled publisher-fault proofs required rather than documentation claims for confirm/failure/retention/cutover? [Measurability, Spec §FR-010, US1–US5]
- [x] CHK026 Are Q10 gate verdict labels (PASS / SKIPPED (scope-empty) / SKIP / N/A / Could not run) preserved without relabeling skips as PASS? [Measurability, Spec §FR-012, SC-007]
- [x] CHK027 Is “real relay tests must exercise the worker” stated so factory isolation cannot count as delivery proof? [Measurability, Spec §Edge Cases, brief Q9]

## Scenario Coverage — Primary

- [x] CHK028 Are primary happy-path requirements present for bounded ordered confirmed publish and per-row ProcessedAt? [Coverage Primary, Spec §US1]
- [x] CHK029 Are primary retention requirements present for strictly-older processed deletion? [Coverage Primary, Spec §US3]
- [x] CHK030 Are primary cutover requirements present for broker-down durable save then relay recovery, plus known-precommit both-or-neither rollback acceptance for both handlers? [Coverage Primary, Spec §US5]
- [x] CHK031 Are primary readiness/composition requirements present for one relay/check and sweeper retention, including empty-sample zeros and independent fault clearing? [Coverage Primary, Spec §US4]

## Scenario Coverage — Exception

- [x] CHK032 Are exception requirements defined for nack/return/timeout/connection/pre-confirm cancellation and mid-batch abort with preserved prefix? [Coverage Exception, Spec §US2]
- [x] CHK033 Is invalid oldest payload specified as head-of-line block with health/backlog surfacing rather than silent skip? [Coverage Exception, Spec §US2 scenario 4]
- [x] CHK034 Is host shutdown specified as clean exit without synthetic failure or retry delay, distinct from PublishTimeout? [Coverage Exception, Spec §US2 scenario 5]
- [x] CHK034a Are failure-log allowlist prohibitions (no exception objects, raw Message/ToString/stack/inner-exception text, payload-derived values, or sensitive config) and sentinel/rowless proof ownership in `OutboxRelayHostedServiceTests` explicit? [Coverage Exception, Spec §US4/FR-006; tasks T008]
- [x] CHK034b Is non-overlapping-pass behavior mapped to FR-001 with deterministic proof ownership in `OutboxRelayHostedServiceTests` (T002)? [Coverage Primary, Spec §US1/FR-001; tasks T002; brief Q8 item 5]

## Scenario Coverage — Recovery

- [x] CHK035 Are recovery requirements defined for fresh-scope retry after failure, confirmed-then-failed marker redelivery, cleanup next-interval retry, and independent pass/cleanup health-fault clearing? [Coverage Recovery, Spec §US1/US2/US3/US4]
- [x] CHK036 Is at-least-once absorption by existing enrichment claims stated without inventing exactly-once? [Coverage Recovery, Spec §US1 scenario 4]
- [x] CHK036a Are real-Postgres cutover both-or-neither rollback proofs assigned to T021/`ImportMovieFolderCommandHandlerPersistenceTests` and authorized T022/`OutboxRelayTests` cases for both handlers, preserving uncertain-save distinction? [Coverage Recovery, Spec §US5; tasks T021–T022; brief Q8 item 2]

## Edge Cases, Dependencies & Ambiguities

- [x] CHK037 Are edge cases listed for multi-instance exclusion, no batch-wide marking, cleanup-after-publish-failure, TimeProvider-only delays, and no CONTEXT/constitution edit? [Coverage Edge, Spec §Edge Cases]
- [x] CHK038 Are dependencies on DEV-329 staged rows and DEV-419 ADR-0027 named with Phase B pickup STOP rather than treating the design packet as landed? [Dependency, Spec §Assumptions / FR-008]
- [x] CHK039 Do any `[NEEDS CLARIFICATION]` markers remain in spec.md? [Ambiguity] — none
- [x] CHK040 Is any owner structural escalation (ticket change or constitution departure) left unresolved? [Conflict] — none; brief Q12 closed with no structural checkbox

## Feature Readiness

- [x] CHK041 Do outcome FRs map to US1–US5 and SC-001–SC-007; supporting boundary/gate/loop FRs map to FR-009–FR-014 rather than invented product SCs; does SC-007 cite the 24-file boundary including centralized constants? [Traceability, Spec §SC-007/FR-006/FR-009]
- [x] CHK042 Was L clarify session recorded under Clarifications with settled grill answers (including post-grill C1 centralized names and 24-file boundary) and no new Patron questions? [Traceability, Spec §Clarifications; brief.md:137-143]
- [x] CHK043 Is Draft ADR-0028 referenced as Draft (present, not accepted or Gate-1-cleared), with historical Quill-did-not-author notes preserved, Build ADR-ownership restrictions retained, and Gate 1 not claimed by this draft? [Traceability, Spec Input / Assumptions / Edge Cases; plan.md Phase boundaries; tasks.md Notes]
- [x] CHK044 Are research.md / data-model.md / contracts/ / quickstart.md / design.md correctly omitted with plan rationale (brief already freezes approach; no ui tag; no public HTTP)? [Traceability, plan.md]
- [x] CHK045 Spec Kit specify quality items (no invented stack beyond brief; stakeholder-testable stories; mandatory sections; no NEEDS CLARIFICATION) are folded into Content Quality / Completeness above (L path single requirements checklist, DEV-414 precedent). [Traceability]
- [x] CHK046 Do plan Principle VI and Design/ordering sections state centralized metric names in `TelemetryConstants.cs`, constants-before-consumer order, and the Patron C1 citation? [Consistency, plan.md Constitution Check / Design / Q9 boundary]
- [x] CHK047 Does tasks.md preserve seven-step Q10 delivery order and final post-refactor gates (T027) while adding the explicit constants prerequisite before T016? [Consistency, tasks.md Phase 4 / Phase 6 / Dependencies]

## Notes

- Checklist validates requirements writing quality, not runtime proof that the relay already exists on this baseline HEAD.
- No `needs decision` and no `needs recon` raised during drafting. Twelve Patron answers (Q1–Q12) remain frozen; post-grill C1 revises Q9 count to 24 solely for six TelemetryConstants members (CONCLUSIONS.md:218-221); plan-challenge Q7 logging allowlist is at CONCLUSIONS.md:233-236 / brief.md:217-223.
- ≥80% of items carry Spec/brief/plan/tasks traceability references.
- Spec Kit specify auto-checklist (`checklists/requirements.md`) is this L-path file; no second duplicate checklist file.
- Revalidated after Quill Phase 2 step 3 correction: CHK010–CHK013, CHK022–CHK023, CHK041–CHK042, plus CHK046–CHK047 for centralized naming / constants-before-consumer / seven-step + post-refactor gate preservation.
- Revalidated after Quill Phase 2 step 6 Round 1 plan-challenge correction: CHK010–CHK011, CHK018, CHK023, CHK030–CHK031, CHK034a–CHK034b, CHK035, CHK036a, CHK043 for logging allowlist, Draft ADR-0028 status, no-overlap proof, empty-sample/fault-clearing recovery, and cutover both-or-neither proofs. Ledger F2 remains rejected; six Low observations recorded without required fixes.
