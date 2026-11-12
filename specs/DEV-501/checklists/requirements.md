# Specification Quality Checklist: Explicit subtitle search and download

**Purpose**: Validate specification completeness and quality before Gate 1 / planning freeze  
**Created**: 2026-11-12  
**Feature**: [spec.md](../spec.md)  
**Depth**: Standard · **Audience**: Author + PR reviewer · **Focus**: Contract safety, write guard, UI states, prepare removal, D1–D5 + Principle V

## Content Quality

- [x] No unjustified implementation details in stakeholder-facing requirement intent (contracts/statuses cited from Patron Q3/D1–D5/Principle V are in-scope product decisions)
- [x] Focused on user value and ticket acceptance
- [x] Mandatory sections completed
- [x] Vocabulary section present with qualified glossary names consistent with brief/D4

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain for settled Patron rulings
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable and mapped to tasks
- [x] Success criteria stay user/outcome oriented (runtime honesty included)
- [x] All acceptance scenarios are defined for US1–US4
- [x] Edge cases are identified (incl. cancel≠timeout, known-zero quota, publication ceiling)
- [x] Scope is clearly bounded with out-of-scope list
- [x] Dependencies and assumptions identified; recon:113–130 incompleteness kept as gated pickup checks

## Feature Readiness

- [x] Functional requirements map to acceptance scenarios and tasks
- [x] User scenarios cover primary search/download and failure/read-only flows
- [x] Specification defines measurable outcomes and their planned verification; delivered outcomes are not claimed
- [x] Prepare removal and SaveEnabled retention explicitly required (D4)
- [x] Plan-challenge status: PLAN FROZEN per Keel PLAN_ADJUDICATION.md; Gate 1 not approved; runtime OPEN

## User-Visible Outcome

- [x] "What the User Will See" names Subtitles tab changes
- [x] SMOKE-001..003 written in user terms for Compose-visible path
- [x] Runtime OPEN receipts separated from smoke / unit proofs
- [x] Config/data needs listed (SaveEnabled, preferred languages/catalog, provider key, dependencies)

## Requirement Clarity & Consistency (unit tests for English)

- [ ] CHK001 Are GET search and POST download envelopes, nullable quota fields, downloadAvailability, and Cache-Control (success + mapped errors) fully specified? [Completeness, Spec §FR-001–FR-002 / Q3/D1]
- [ ] CHK002 Is the status map complete for 400/422/403/404/409/429/502/504 with D1 reasons, traceId, centralized handler, and no credential leakage? [Completeness, Spec §FR-003 / D1 / Principle V]
- [ ] CHK003 Are candidate binder Singleton (no interface), TTL/cap options, 32-hex seeded-Random tokens, earliest-expiry FIFO eviction, TimeProvider use, and stale rejection before paid download specified without a DB store? [Clarity, Spec §FR-004 / D2]
- [ ] CHK004 Are match percent formula, midpoint rounding, empty-union, deterministic sort keys, and mandatory scorer/ranking property tests explicit? [Clarity, Spec §FR-005 / Q4/Q9 / brief Final drafting]
- [ ] CHK005 Are catalog route/DTO, bilingual labels, canonical normalization, first-preference default, and empty-preference choose-language (no auto-search; no acquisition 409) specified consistently? [Consistency, Spec §FR-006 / catalog ruling]
- [ ] CHK006 Are never-auto-download and no automatic paid retry stated for both API and UI? [Consistency, Spec §FR-007 / Q4–Q6]
- [ ] CHK007 Are Subs path derivation, `.srt`-only writes, `(N)` collision spelling (no space), and Subs creation-on-missing specified? [Completeness, Spec §FR-008 / Q5]
- [ ] CHK008 Are containment refusals required before directory creation, after create/before staging, and before each publication? [Coverage, Spec §FR-009 / Q5/Q7]
- [ ] CHK009 Are streaming 2 MiB bound, staging reuse, publication-attempt default 10_000, discovery exclusion, owned-staging cleanup, and cancel≠timeout specified? [Clarity, Spec §FR-010 / D3]
- [ ] CHK010 Are POST prechecks ordered movie→binding→read-only→config→containment, with read-only GET + disabled downloadAvailability and empty preferences not causing 409? [Consistency, Spec §FR-011 / D1]
- [ ] CHK011 Are local-tracks-above-results, D5 active-check-local-only, distinct search/download 429 copy, quota local-time/unavailable rendering, and uninterrupted video required? [Coverage, Spec §FR-012 / D1/D5]
- [ ] CHK012 Are session-safe activation and changed-language completion (add local track without replacing remote results) specified? [Clarity, Spec §FR-013 / ASSUMPTIONS]
- [ ] CHK013 Is prepare-only removal complete per D4 after current-source verification (recon incomplete inventories) while shared mechanisms/SaveEnabled remain? [Completeness, Spec §FR-014 / D4]
- [ ] CHK014 Is OpenSubtitles-only provider seam with provider-neutral public DTOs explicit, excluding SubDL? [Boundary, Spec §FR-015 / owner ruling 6]
- [ ] CHK015 Is C# → OpenAPI → generated TypeScript mandatory with no hand-rolled web catalog duplicates? [Consistency, Spec §FR-016]
- [ ] CHK016 Is Principle VII 1.15.0→1.16.0 + ADR 0041 acceptance post-Gate-1, ADR 0038 Proposed wording-only relationship, and Principle V preservation without owner structural checkbox specified? [Process, Spec §FR-017 / Q2/D4/Principle V]
- [ ] CHK017 Are unit/property/provider/filesystem/API/UI test families, mandatory applicable property gate, and gate honesty labels required? [Coverage, Spec §FR-018–FR-019 / Q8 / brief Final drafting]
- [ ] CHK018 Are runtime home-cluster/BS Player receipts marked OPEN with Could not run honesty? [Acceptance Criteria, Spec §FR-020 / Q8]
- [ ] CHK019 Are closing bar, two-round cap, Size L three-axis evidence, and frozen-scope follow-up rule recorded? [Process, Spec §FR-021 / Q1]
- [ ] CHK020 Are PRODUCT Care-list prohibitions (new dependency/layer/schema/LocalPlay) explicit? [Boundary, Spec §FR-022]
- [ ] CHK021 Does design.md cover route map, wireframe, component tree, states, data needs, and a11y for the Subtitles tab without new top-level UI routes (including Palette D5 active-indicator repair)? [Completeness, design.md]
- [ ] CHK022 Are smoke checks SMOKE-001..003 aligned to visible Subtitles tab outcomes and distinct from OPEN runtime receipts? [Consistency, Spec §What the User Will See]
- [ ] CHK023 Is provider-observed quota freshness / Retry-After TimeProvider mapping / no invented charge-refund stated? [Completeness, Spec §FR-023 / D1]

## Notes

- Checklist items are requirements-quality questions for Gate 1 review, not implementation verification steps.
- Current plan-freeze/analysis receipt is Keel PLAN_ADJUDICATION.md; earlier 32-task and pre-challenge status claims are superseded.
- Items remain unchecked for human/reviewer attestation at Gate 1.
