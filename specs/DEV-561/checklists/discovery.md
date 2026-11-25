# Discovery Requirements Quality Checklist: Outside-library For You row

**Purpose**: Unit tests for English — validate requirement quality for DEV-561 discovery before Gate 1
**Created**: 2026-11-24
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [tasks.md](../tasks.md)
**Depth**: Standard · **Audience**: Spec PR reviewer · **Focus**: API contract, ranking/identity, web/attribution

## Requirement Completeness

- [ ] CHK001 Are additive link metadata fields (`title`, `release_year`, `poster_path`) and Crowd ID-only compatibility stated together? [Completeness, Spec §FR-002]
- [ ] CHK002 Are enrichment write rules and backfill hydration-of-legacy-title rows both specified? [Completeness, Spec §FR-003]
- [ ] CHK003 Are both TMDb-disabled and blank-ApiKey empty behaviours specified for API and web? [Coverage, Spec §FR-010, FR-012]
- [ ] CHK004 Are PRODUCT Positioning, CONTEXT Recommendation update, glossary term, and README feature-list obligations all present? [Completeness, Spec §FR-014–015]
- [ ] CHK005 Are home-library ≥1-seed and ≥2-seed denominators plus owner top-10 verdicts required as delivery evidence? [Completeness, Spec §FR-017]

## Requirement Clarity

- [ ] CHK006 Is Option A library exclusion (TMDb id, then normalized title + equal known year) unambiguous and free of IMDb-candidate invent? [Clarity, Spec §FR-005]
- [ ] CHK007 Is “normalized title” defined with NFC, trim, collapsed whitespace, ordinal case-insensitive comparison, and explicit punctuation/accent preservation, and is that normalization applied only to the canonical metadata tuple chosen before title/year fallback? [Clarity, Spec §FR-005, FR-008, data-model.md]
- [ ] CHK008 Are Liked/Watched defaults, Watchlist ineligibility, and Liked precedence quantified? [Clarity, Spec §FR-006]
- [ ] CHK009 Is the reason format pinned to the two-seed ticket example with coincident-title behaviour? [Clarity, Spec §FR-009]
- [ ] CHK010 Is “day-verified official TMDb attribution” bounded without inventing exact credit copy in the draft? [Clarity, Spec §FR-014, Assumptions]

## Requirement Consistency

- [ ] CHK011 Do Wanted 3 / Done when 2 amendments from Q3 Option A match FR/US/edge-case exclusion rules? [Consistency, Spec §Scope, FR-005]
- [ ] CHK012 Do API card fields match web display fields and the codegen contract (no hand-rolled duplicates)? [Consistency, Spec §FR-010–011, contracts/not-in-library.md]
- [ ] CHK013 Do hide-on-loading/empty/error rules align across What the User Will See, US4, FR-012, and Assumptions? [Consistency, Spec §FR-012]
- [ ] CHK014 Do main For You parity and Related library-only invariants appear in both requirements and success criteria? [Consistency, Spec §FR-010, SC-005]

## Acceptance Criteria Quality

- [ ] CHK015 Can the two-seed quality bar and 20-card cap be objectively tested from the written requirements? [Measurability, Spec §FR-007, SC-004]
- [ ] CHK016 Can zero request-time provider HTTP be verified from the written API acceptance scenarios? [Measurability, Spec §US3, SC-005]
- [ ] CHK017 Are SMOKE-001–003 stated in user terms mapped to web/E2E obligations without inventing Compose-only smoke beyond the brief? [Acceptance Criteria, Spec §Smoke Test]

## Scenario & Edge Coverage

- [ ] CHK018 Are blank-title links, missing years, duplicate pairs, disliked sources, and watchlist-only sources addressed? [Coverage, Spec §Edge Cases]
- [ ] CHK019 Is retain-prior-links on failed fetch specified for both enrichment and backfill paths? [Coverage, Spec §FR-003, US1]
- [ ] CHK020 Are sparse/empty home-library acceptance runs explicitly valid when accurately reported? [Coverage, Spec §FR-017, US5]
- [ ] CHK021 Are out-of-scope items (Not interested, Wanted lists, Related outside movies, new provider lists, LocalPlay/secrets/Process.Start) explicitly excluded? [Boundary, Spec §Out of scope]

## Dependencies & Assumptions

- [ ] CHK022 Are DEV-539 Done, ADR 0049 Accepted, and Gate 1 merge named as coding prerequisites? [Dependency, Spec §Dependencies]
- [ ] CHK023 Are `[assumed]` taste rulings cited from `ASSUMPTIONS.md` rather than silently re-decided? [Assumption, Spec §Assumptions]
- [ ] CHK024 Is PLAN FROZEN / implementation PASS explicitly not claimed by this draft? [Traceability, Spec §Owner question]

## Notes

- Checklist tests requirements quality, not implementation behaviour.
- Defaults used for checklist intent: Standard depth; Spec PR reviewer audience; discovery/API/web focus clusters from brief Q1–Q12.
