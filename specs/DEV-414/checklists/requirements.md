# Requirements Quality Checklist: DEV-414 FeaturesRequested consumer and movie_features persistence

**Purpose**: Unit tests for English — validate requirement completeness, clarity, consistency, measurability, and primary/exception/recovery coverage before Keel analyze and Gate 1
**Created**: 2026-10-23
**Feature**: [spec.md](../spec.md)
**Depth**: Standard (L path)
**Audience**: Reviewer (spec PR) / Keel analyze
**Focus**: Feature lifecycle, retry/DLQ, recovery, activation/observability; exclusions and publication limits

## Content Quality

- [x] CHK001 Are implementation names limited to those already fixed in `brief.md` Q6–Q8 (type/file names, port signatures, topology names) rather than Quill-invented stack choices? [Clarity, Spec Input]
- [x] CHK002 Do user stories describe observable outcomes (persist embedding, retry/DLQ, recovery, activation/E2E) a reviewer can test without inventing a ruling? [Completeness, Spec §User Stories]
- [x] CHK003 Are all mandatory Spec Kit sections present (Clarifications, User Scenarios, Requirements, Success Criteria, Assumptions)? [Completeness]

## Requirement Completeness

- [x] CHK004 Are ticket Scope lines 20–24 and AC line 17 each represented by at least one FR or acceptance scenario? [Completeness, Spec §FR-001–FR-017]
- [x] CHK005 Are missing-movie and null/whitespace no-op requirements present alongside the nonblank success path? [Completeness, Spec §FR-005, US1]
- [x] CHK006 Are retry, permanent failure, Attempt&lt;1/malformed, shutdown requeue, and failed-publish-without-ack requirements all documented? [Completeness, Spec §FR-007–FR-009, US2]
- [x] CHK007 Is independent missing-features recovery (anti-join, Attempt 1, no enrichment mutation, dual-pass isolation, post-DLQ reset) fully specified? [Completeness, Spec §FR-010–FR-011, US3]
- [x] CHK008 Are feature activation prerequisites, inactive-host behavior, features-consumer readiness, and trace/log requirements specified? [Completeness, Spec §FR-012–FR-014, US4]
- [x] CHK009 Is additive CONTEXT.md vocabulary (three Q9 terms, retain existing definitions, conflict → needs decision) required in the same Spec PR? [Completeness, Spec §FR-016]
- [x] CHK010 Are negative exclusions (no new dependency/schema/HTTP/generic framework/LocalPlay/DEV-371 rewrite) explicit? [Completeness, Spec §FR-017]

## Requirement Clarity

- [x] CHK011 Is whitespace-as-no-input stated as an explicit handler decision rather than inferred from the unread OMDb IsMissing body? [Clarity, Spec §Edge Cases, brief Q3]
- [x] CHK012 Is the publication durability limit explicit (configured confirms/mandatory/awaited publish; no asserted broker-ack/nack/return or exactly-once guarantee)? [Clarity, Spec §FR-009, brief Q4 continuation]
- [x] CHK013 Is MaxAttempts defined as one transport chain bound (not lifetime), with deliberate fresh Attempt 1 after prior DLQ? [Clarity, Spec §FR-007/FR-011, brief Q5]
- [x] CHK014 Is the feature failure header whitelist enumerated (four codes) and duplicate_imdb_id explicitly forbidden for feature processing? [Clarity, Spec §FR-008]
- [x] CHK015 Is Sweeper glossary “Avoid: a second consumer” clarified as not prohibiting the ticket-required features consumer? [Clarity, Spec §Clarifications Q2]

## Requirement Consistency

- [x] CHK016 Do FR-006 / Q2 (no metadata mutation) align with US1 scenario 4, US3 recovery, and SC-001/SC-005/SC-006? [Consistency]
- [x] CHK017 Do FR-004 last-write-wins redelivery and FR-015/SC-002 one-row uniqueness agree with US1 scenario 3? [Consistency]
- [x] CHK018 Do plan Phase B entry checks and tasks TB000–TB004 agree with brief Q8 step 1 and Q9 PRODUCT sequencing non-waiver? [Consistency, plan.md / tasks.md]
- [x] CHK019 Do tasks Phases 1–5 map to brief Q8 steps 2–6 without dropping live-broker publication/unroutable proofs? [Consistency, tasks.md]
- [x] CHK020 Does frozen scope in spec Edge Cases / FR-017 match brief frozen scope (including ADR deferred, no Gate 1 claim)? [Consistency]

## Acceptance Criteria Quality / Measurability

- [x] CHK021 Can SC-001–SC-007 be objectively passed or failed from the stated proofs without inventing latency/volume targets? [Measurability, Spec §Success Criteria]
- [x] CHK022 Are live-broker tests required to decide retry/DLQ publication, failed publish without ack, and unroutable expectation rather than documentation claims? [Measurability, Spec §FR-015, US2]
- [x] CHK023 Are Q1 gate verdict labels (exit 1 / Could not run / SKIPPED / NOT APPLICABLE / SKIP / property opt-out) preserved without relabeling as PASS? [Measurability, Spec §SC-007, Clarifications Q1]

## Scenario Coverage — Primary

- [x] CHK024 Are primary happy-path requirements present for nonblank plot → one vector → upsert → one row? [Coverage Primary, Spec §US1]
- [x] CHK025 Are primary recovery requirements present for anti-join enqueue Attempt 1 on SweepInterval? [Coverage Primary, Spec §US3]
- [x] CHK026 Are primary activation/E2E requirements present for active composition and shared trace? [Coverage Primary, Spec §US4]

## Scenario Coverage — Exception

- [x] CHK027 Are exception requirements defined for retryable vs permanent classification, exhausted MaxAttempts, and invalid vector/payload/Attempt&lt;1? [Coverage Exception, Spec §US2, FR-008]
- [x] CHK028 Is host-requested cancellation specified as requeue without Attempt increment / classification bypass? [Coverage Exception, Spec §US2 scenario 5, FR-007]
- [x] CHK029 Is failed or cancelled retry/DLQ publication specified as never ack? [Coverage Exception, Spec §US2 scenario 6, FR-009]

## Scenario Coverage — Recovery

- [x] CHK030 Are recovery requirements defined for empty enrichment pass, isolated pass failure, enqueue-failure eligibility, and post-DLQ fresh Attempt 1? [Coverage Recovery, Spec §US3]
- [x] CHK031 Is persistent lifetime suppression explicitly rejected (accepted indefinite SweepInterval re-enqueue cost)? [Coverage Recovery, Spec §Edge Cases, brief Q5]

## Edge Cases, Dependencies & Ambiguities

- [x] CHK032 Are edge cases listed for whitespace decision, publication evidence limits, RabbitMQ.Client 7.2.2 vs stale DEV-371 text, and PRODUCT sequencing? [Coverage Edge, Spec §Edge Cases]
- [x] CHK033 Are dependencies on DEV-412 generator, DEV-413 repository/upsert, and existing features topology named without inventing missing seams as already present? [Dependency, Spec §Assumptions / Input]
- [x] CHK034 Do any [NEEDS CLARIFICATION] markers remain in spec.md? [Ambiguity] — none
- [x] CHK035 Is any glossary conflict between new Feature terms and existing CONTEXT.md definitions unresolved? [Conflict] — none found; existing Sweeper/Transport Retry Attempt/Worker/Publisher Confirm definitions retained verbatim; additive terms only

## Feature Readiness

- [x] CHK036 Do outcome FRs map to US1–US4 and SC-001–SC-007; supporting vocabulary/exclusion FRs map to FR-016/FR-017 and Spec PR CONTEXT edit rather than invented product SCs? [Traceability]
- [x] CHK037 Was L clarify session recorded under Clarifications with settled grill answers and no new Patron questions? [Traceability, Spec §Clarifications]
- [x] CHK038 Is ADR explicitly deferred (not claimed complete) and Gate 1 not claimed by this draft? [Traceability, Spec Input / Assumptions]
- [x] CHK039 Are research.md / data-model.md / contracts/ / quickstart.md / design.md correctly omitted with plan rationale (brief already freezes approach; no ui tag; no public HTTP)? [Traceability, plan.md]

## Notes

- Checklist validates requirements writing quality, not runtime proof that the consumer already exists (recon: features.requested consumer NOT FOUND).
- No `needs decision` and no `needs recon` raised during drafting. Ten Patron answers (Q1–Q9 including Q4 continuation) remain frozen.
- ≥80% of items carry Spec/brief/plan/tasks traceability references.
- Spec Kit specify quality items are folded into Content Quality / Completeness above (L path single requirements checklist, DEV-349 precedent).
