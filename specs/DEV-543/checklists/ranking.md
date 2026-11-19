# Requirements Quality Checklist: Nearest-seed ranking and reasons

**Purpose**: Unit tests for English — validate ranking, eligibility, reasons, Related, load/cache, and evidence requirements quality before Gate 1
**Created**: 2026-11-19
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [brief.md](../brief.md)
**Depth**: Standard
**Audience / timing**: Author + PR reviewer (Gate 1 readiness)
**Focus**: Ranking math clarity, strong-match eligibility, reason contracts, Related profile removal, load/cache bounds, cleanup gates, acceptance evidence

## Requirement Completeness

- [ ] CHK001 Are positive seed kinds, weights, and single-kind precedence fully specified for multi-signal movies? [Completeness, Spec §FR-002, brief Q5]
- [ ] CHK002 Are candidate eligibility boundaries (unwatched, not disliked, watchlist dual role) documented? [Completeness, Spec §US1, brief Scoring item 1]
- [ ] CHK003 Are top-three aggregation, rank multipliers, and fewer-than-three behaviour specified? [Completeness, Spec §FR-001, brief Scoring item 3]
- [ ] CHK004 Is the disliked penalty formula specified as unscaled max similarity × configured weight? [Completeness, Spec §FR-001, brief Q3]
- [ ] CHK005 Are empty-result conditions (no positive seeds; no strong pairs after self-exclusion; no padding) explicit? [Completeness, Spec §US1 scenario 3, brief Q5]
- [ ] CHK006 Is Related behaviour without taste-profile contribution and without profile reasons fully specified? [Completeness, Spec §FR-008, brief Q2]
- [ ] CHK007 Are one-load, no per-movie GetAsync, and no second profile read requirements present? [Completeness, Spec §FR-007, brief Wanted 8]
- [x] CHK008 Are ADR/CONTEXT/reason-table same-**delivery**-PR documentation requirements present without Quill inventing ADR text, with Proposed ADR 0047 preserved for delivery (not Gate 1 alone)? [Completeness, Spec §FR-010; DRAFTING_RECEIPT ADR path closed]

## Requirement Clarity

- [ ] CHK009 Is "strong match" quantified with the shared bar default and strong-reason list, and explicitly pre-weight/pre-recency? [Clarity, Spec §Domain vocabulary / FR-003, brief Q4]
- [ ] CHK010 Is "best seed" selection metric identical to the recency-adjusted weighted similarity used in ranking contributions, restricted to strong pairs? [Clarity, Spec §FR-004, brief Q6]
- [ ] CHK011 Are the three seed reason templates and their position as first `reasons` entry unambiguous? [Clarity, Spec §FR-005, brief Scoring table]
- [ ] CHK012 Is recency floor default 0.25 and the unscaled cases (no playback, disliked, raw strong-match) explicit? [Clarity, Spec §FR-006, brief Q3]
- [ ] CHK013 Is self-exclusion for watchlisted candidates stated so a movie is not compared to itself? [Clarity, Spec §Edge Cases, brief Q5]

## Requirement Consistency

- [ ] CHK014 Do For you strong-match rules and DEV-544 shared options stay one policy for both surfaces? [Consistency, Spec §FR-003, brief Q4]
- [ ] CHK015 Do reason requirements forbid all three superseded templates consistently across stories and SC items? [Consistency, Spec §FR-005 / SC-002]
- [ ] CHK016 Does Related retention of `AlreadyWatchedPenalty` stay consistent with Wanted 11 cleanup (keep that option)? [Consistency, Spec §FR-008 / FR-009, brief Q2]
- [ ] CHK017 Do ownership boundaries (DEV-542/541/518/544/539/540) match Frozen scope and Out of scope? [Consistency, Spec §Scope, brief Frozen scope]

## Acceptance Criteria Quality

- [ ] CHK018 Can top-10 seed diversity and the three named home-library pairs be objectively verified via DEV-545 snapshot? [Measurability, Spec §SC-001]
- [ ] CHK019 Can Related score stability under an unrelated review be objectively tested? [Measurability, Spec §SC-003]
- [ ] CHK020 Can hit-rate non-regression and warm sub-500 ms runtime be measured with named fixtures? [Measurability, Spec §SC-007, brief Acceptance]
- [ ] CHK021 Is OpenAPI snapshot unchanged stated as a hard acceptance check? [Measurability, Spec §SC-008]

## Scenario / Edge Coverage

- [ ] CHK022 Are watchlist-only ranking and no-fallback empty results both covered? [Coverage, Spec §US1 scenarios 3]
- [ ] CHK023 Are director-only strong match admission and weak-aggregate rejection both covered? [Coverage, Spec §US1 scenarios 4 / SC-005]
- [ ] CHK024 Is best-strong-seed naming when that pair is outside the top-three score contributors covered? [Coverage, Spec §US2 / Edge Cases]
- [ ] CHK025 Are cleanup-after-caller-check and incomplete-inventory stop conditions covered? [Coverage, Spec §US5 / Edge Cases]
- [ ] CHK026 Are prerequisite merges (DEV-518/544/510) and DEV-545 baseline capture called out before behaviour change? [Coverage, Spec §Edge Cases, brief Task order]

## Dependencies & Ambiguities

- [x] CHK027 Is Proposed ADR 0047 cited as the resolved nearest-seed path for the **delivery** PR (T017–T019), with historical 0045→DEV-545 collision preserved as provenance only and no open Gate 1 ADR-number dependency / no separate Gate 1 ADR land? [Ambiguity resolved, DRAFTING_RECEIPT, Spec §FR-010]
- [ ] CHK028 Are presentation `[assumed]` ID tie-breaks referenced from ASSUMPTIONS.md rather than re-invented? [Assumption, Spec §Assumptions / FR-004]
- [ ] CHK029 Does the checklist avoid testing implementation behaviour (no "verify handler returns…") and only test requirement quality? [Consistency]
- [ ] CHK030 Is T002 a hard dependency for T003/T004/T005 and subsequent ranking/Related behaviour-changing tasks, with T006 after T005 (not parallel with it)? [Coverage, tasks.md Phase 2–3]
- [ ] CHK031 Are SMOKE-001 and SMOKE-002 covered by a verification-only evidence task recording build identity, fixture, both rail outcomes, and no `/web` edits? [Coverage, Spec §Smoke / SC-010 / EVIDENCE §8]
- [ ] CHK032 Do Constitution Check / T003 / T004 require typed ValidateOnStart options validation (binding verified at pickup T001, not an unverified planning-time helper symbol) and injected TimeProvider for recency, with Principle III marked N/A (not PASS)? [Consistency, plan.md Constitution Check]
- [ ] CHK033 Does T023 closure depend on T021/T022/T024 plus Q7, with Phase 7 ordered T020→T021→T022→T024→T023? [Coverage, tasks.md Phase 7 / Spec §FR-012]

## Notes

- Defaults when interaction skipped: Depth Standard; Audience Reviewer (PR); Focus ranking + reasons + Related + load/evidence.
- Soft-capped near-duplicates; ticket 155/422 figure treated as reference data, not a hard SC threshold (brief Acceptance).
- No PLAN FROZEN / Gate 1 / implementation PASS claims from this checklist.
- Analyze round 1: ADR 0047 resolution, T002 hard gate, Compose smoke task, Principle VII/III alignment.
- Analyze round 2: T003/T004/T005 gated on T002; Phase 7 order + T023 deps; Proposed ADR delivery-diff; recon historical/recovery gap; Principle VII binding verified at pickup; stale ADR checklist wording closed.
