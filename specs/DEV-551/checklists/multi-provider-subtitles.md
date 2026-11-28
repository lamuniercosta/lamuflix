# Multi-Provider Subtitles Requirements Checklist: DEV-551

**Purpose**: Unit tests for English — validate clarity, completeness, and consistency of DEV-551 requirements for the multi-provider subtitle acquisition seam before Gate 1.
**Created**: 2026-11-28
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [tasks.md](../tasks.md) · [contracts/search-response.md](../contracts/search-response.md)
**Depth**: Standard · **Audience**: Reviewer (Spec PR / Gate 1) · **Focus**: aggregate search, additive contract, credential isolation, binding, health, compatibility

## Requirement Completeness

- [ ] CHK001 Is aggregate search specified as one call per language with concurrent fan-out, independent timeouts, merge-after-settle, and no provider query parameter? [Completeness, Spec §FR-001, Q1]
- [ ] CHK002 Are startup refusal, off classification (incl. invalid timing), host continuity, Constitution VII ValidateOnStart exception, and the **split 409 predicates** — **search** when no enabled provider is usable (all-enabled check; usable-sibling search available) vs **download** when the **bound** provider is missing/invalid/unusable after read-only precedence **even with a usable sibling** (zero paid calls to either provider) — all specified without host abort? [Completeness, Spec §FR-002, Q2, Constitution VII, Ledger S1, ADR 0052, M3/S1]
- [ ] CHK003 Are additive `providerId` and ordered `providers` outcomes specified while retaining every legacy field via the **unchanged legacy contract** (baseline parity in T010; no unverified field/type/nullability table claimed from recon-DEV-551), with Patron S5 closed `state`/`reason` Enumerations at the Core outcome boundary and JSON Name conversion in DTO scope? [Completeness, Spec §FR-003, contracts/search-response.md, T010, S2/S5]
- [ ] CHK004 Are per-client destination allow-lists (**response-independent**), credential-origin confinement, redirect/downgrade refusal, hop validation before I/O, dual HTTP opt-in (credential-free declaration **and** explicit typed opt-in; credentialed refusal; OpenSubtitles HTTPS-only), and unsafe-URL-scoped startup refusal with redacted warning (provider name + `unsafe_base_url` only) specified? [Completeness, Spec §FR-005, Q4, H1/M1/M2]
- [ ] CHK005 Are capability precedence, missing earlier identifier with later available input (skip, not terminal), no formable input at all, empty-only fallback, stop-on-timeout/quota/failure, search-429 vs known download-quota exhaustion on otherwise valid candidates, and cancellation≠timeout specified and mapped to FR-006 / T008 / T011 / T014? [Completeness, Spec §FR-006, Q5, Edge Cases, tasks.md]
- [ ] CHK006 Are provider-bound Candidate ID rules, over-capacity every-returned-token-resolves-immediately, later-search eviction → stale with zero paid calls, ranked capacity across providers/preferred languages, expiry as its own case, bounded/provider-bound property invariant, and A/B reference isolation specified (T013 before search issuance; T015 proofs for US2 scenarios 4–6 independently of expiry)? [Completeness, Spec §FR-008, Q7, tasks.md]
- [ ] CHK007 Are Core-side ordered provider lookup (host-lifetime instance-owned spacing/observation state), outside-Core typed-client registration/composition, `Subtitles:ProviderOrder`, Patron S5 `Enabled`/`MinimumRequestGap`/timeout validation, and shared health/search/download/Lookup lookup specified? [Completeness, Spec §FR-009, Q8, Constitution I, Ledger S4, plan.md / T004]
- [ ] CHK008 Are per-provider health observations specified as reading Core-side host-lifetime observation state shared with search, with no network calls and readiness non-Unhealthy on provider faults? [Completeness, Spec §FR-010, Q9, Ledger S4, T023/T024]
- [ ] CHK009 Are glossary, setup-doc, OpenAPI/TypeScript generation, OpenSubtitles-only production adapter, and web-test preservation all required? [Completeness, Spec §FR-011–FR-013]
- [ ] CHK010 Is ADR 0052 acceptance **first in the delivery task sequence after separate Gate 1** (before-deliverable-merge bar retained; Spec PR may carry Proposed) and DEV-525 Done-before-Build stated? [Completeness, Spec §FR-014–FR-015, Q11, brief.md:19, T030 before T001]

## Requirement Clarity

- [ ] CHK011 Is legacy quota projection defined as first usable provider in configured order, never summed? [Clarity, Spec §FR-003, Q3]
- [ ] CHK012 Is legacy download availability defined as library policy AND at least one provider permits download? [Clarity, Spec §FR-003, Q3]
- [ ] CHK013 Are `state`/`reason` required to be Patron S5 closed Enumerations (or equivalent) at the Core outcome boundary with JSON `Name` conversion (never upstream text/URLs/credentials), with exclusive reason predicates/precedence (disabled → `off`/`off`; else unsafe URL → `unusable`/`unsafe_base_url`; else missing/invalid credentials/timeout/gap → `off`/`provider_not_configured`; request-time `no_search_input` only when usable with no formable input), no optional empty slot, and T010 wire assertions assigned? [Clarity, Spec §FR-003, contracts/search-response.md, T009/T010, S2/S5]
- [ ] CHK014 Is cross-provider sort order fully keyed (match% → download count → provider order → within-provider ties) with stable merge? [Clarity, Spec §FR-007, Q6, ASSUMPTIONS.md]
- [ ] CHK015 Is “Nothing visible” stated with ui:none and DEV-552 owning the picker? [Clarity, Spec §What the User Will See]
- [ ] CHK016 Are valid ranges for timeout/gap documented without inventing positive safety defaults, with Patron-approved `Enabled`/`MinimumRequestGap` names? [Clarity, Spec §FR-009, plan Patron-approved identifiers, S5]

## Requirement Consistency

- [ ] CHK017 Do Spec SC-001–SC-008 map to brief Test strategy / Gate expectations (including ship-review split and rebased-head verification) without dropping a receipt? [Consistency, Spec §Success Criteria, brief Gate expectations, T031–T032]
- [ ] CHK018 Is OpenSubtitles both sole production adapter and required shared-suite participant in the foundation path (**T018 adapter contract proof**), with existing assertions preserved? [Consistency, Spec §FR-013, US3, tasks Phase 3]
- [ ] CHK019 Does generate:api wait until **T018 adapter-contract receipt** and **T010 endpoint-parity/isolation receipt with complete wire assertions** (incl. request-level branch, simultaneous-fault first-match, Q2-409 `providers` presence, request-level absence, discriminating Q2 usability-freeze fixture) are both proven (T026 after T018 + T010)? [Consistency, Spec §US6, tasks Phase 9, brief Approach]
- [ ] CHK020 Is ADR 0041 left intact with amendment precedence only in 0052? [Consistency, Spec §FR-014, Q10]
- [ ] CHK021 Are picker, track labels, auto-download, TV, scorer edits, new adapters, new packages/layers/schema explicitly out of scope? [Consistency, Spec §Out of scope, Q10]

## Acceptance Criteria Quality

- [ ] CHK022 Can one-provider legacy parity be objectively verified against today’s statuses/quota/availability/ordering plus additive fields (T010; unchanged legacy contract)? [Measurability, Spec §SC-001, T010]
- [ ] CHK023 Can two-provider isolation be objectively verified (concurrent calls; origin-local 503/quota/timeout)? [Measurability, Spec §SC-002]
- [ ] CHK024 Can provider-bound download isolation be objectively verified (B-only call; A/B collision; cross-movie zero paid calls; over-capacity immediate resolve; later-search eviction stale zero paid)? [Measurability, Spec §SC-003, T015]
- [ ] CHK025 Can health zero-network and readiness continuity be objectively verified? [Measurability, Spec §SC-006]
- [ ] CHK026 Can web-green + drift + wire-level/generated-schema coverage + explicit `SubtitleSetupDocumentationTests` be objectively verified? [Measurability, Spec §SC-007, T028]

## Scenario and Edge Coverage

- [ ] CHK027 Are the disjoint status precedence branches covered — request-level existing errors (`providers` absent), multi-usable zero-candidate 200, one usable plus off/refused → 200, all unusable → search 409 (`providers` present with ordered classified rows), one-provider OpenSubtitles legacy including any baseline non-200 settled outcome (assert against unchanged baseline; do not invent status), simultaneous-fault first-match cases, and discriminating Q2 usability-freeze fixture (multi-provider; only config-usable provider settles timeout / search 429 / failure / no formable input with off/refused sibling → 200, empty candidates, ordered named outcomes in each case)? [Coverage, Spec §US1/FR-003/FR-004, Edge Cases, contracts/search-response.md, brief.md:69, T010]
- [ ] CHK028 Are self-eviction prevention, every-returned-token-resolves-immediately, later-search eviction stale with zero paid calls, and truncation-without-new-field covered independently of expiry? [Coverage, Spec §US2 scenarios 4–6, FR-008, Q7, T013/T015]
- [ ] CHK029 Are cross-host redirect, HTTPS downgrade, credential-never-on-download-host, dual HTTP opt-in as **four independent typed-client gates** (permit credential-free + opt-in; refuse missing opt-in; refuse missing credential-free declaration despite opt-in; refuse credential/login configuration despite both opt-ins), response-independent destination refusal before contact, and redacted unsafe-URL warning covered in T005/T006? [Coverage, Spec §US3, FR-005, H1/M1/M2, T006]
- [ ] CHK030 Are ProviderOrder unknown/duplicate warning behaviour, missing-key zero-request, and invalid-timing continuity (host/tracks/usable-sibling 200; zero calls to invalid provider) covered? [Coverage, Spec §US4, FR-002/FR-009, T020/T021, M3]
- [ ] CHK031 Is ProblemDetails extension key exactly `providers` (ruled providers shape), scoped to settled **search** errors — present on Q2 no-usable search 409 and legacy settled search errors, absent on request-level errors that preempt settlement (classified off/unusable rows settle pre-I/O) — with download retaining unchanged ProblemDetails without `providers`? [Coverage, Spec §FR-004, Q3, M4, brief.md:69, contracts/search-response.md, T010/T015]
- [ ] CHK040 Are search and download endpoint regression evidence for the **split 409 predicates** proven — **search** 409 (no enabled provider usable) + `providers` in T010; **download** bound-provider 409 (even with usable sibling; zero calls to either provider) + read-only precedence + legacy status/ProblemDetails-without-`providers` on 409, read-only, and stale/invalid paths in T015 (T014 implements; T026 stays search-only on T018+T010)? [Coverage, Spec §FR-002/FR-004/SC-005, plan Verification, T010/T014/T015, Constitution VII, Ledger S1, ADR 0052, M4]

## Dependencies and Assumptions

- [ ] CHK032 Is Q6 taste ordering traced to `ASSUMPTIONS.md` `[assumed]` rather than silent invention (preserved)? [Assumption, ASSUMPTIONS.md]
- [ ] CHK033 Are Patron-approved S5 state/reason codes (deterministic), ProblemDetails key `providers` (search-only), Core-side lookup with host-lifetime state, Core Enumeration + JSON Name conversion, central ProblemDetails handler, and `Enabled`/`MinimumRequestGap` traced to CONCLUSIONS/ASSUMPTIONS and DRAFTING_RECEIPT / contracts? [Assumption, DRAFTING_RECEIPT, S2–S5]
- [ ] CHK034 Are DEV-525 Done-before-Build (preserved), DEV-552 picker ownership, and DEV-539 null TMDb skip called out as dependencies rather than in-ticket work? [Dependency, Spec §Dependencies, T001]
- [ ] CHK035 Is “no new NuGet/npm / no new project-layer-port / zero schema” disposition explicit with Core-side lookup vs outside-Core typed-client composition? [Dependency, Spec §Out of scope, Constitution I/VII, T004]

## Ambiguities and Conflicts

- [ ] CHK036 Does any requirement still say “robust/intuitive” without a measurable bar? [Ambiguity, Gap]
- [ ] CHK037 Do recon line citations vs current HEAD create a conflicting file set, or only drift within the same named envelope? [Conflict/Assumption, recon-DEV-551 vs plan pickup T001]
- [ ] CHK038 Are closing bar, existing round caps (two plan-challenge / two implementation review; ≤2 fix commits per round), ADR-Accepted-**first-in-delivery-task-sequence-after-Gate-1** (before-deliverable-merge bar retained), ship-review split, rebased-head gate verification, and Phase 10 wait for all in-scope receipts including T015/T024/T028 consistent across spec, plan, and tasks? [Consistency, Spec §FR-014/SC-008, brief.md:19 Q11–Q12 Gate expectations, T030 then T031–T032]
- [ ] CHK039 Is serialization with DEV-550 on `SubtitleProviderServiceCollectionExtensions.cs` acknowledged without expanding scope? [Consistency, plan Approach / T001]

## Notes

- Checklist is requirements quality only — not implementation verification.
- Closed state/reason names and `Enabled`/`MinimumRequestGap` in `contracts/search-response.md` / plan are Patron-approved under Ledger S5; changing the closed set needs a new Patron ruling.
- Plan challenge dispositions applied; owner Gate 1 remains pending; this checklist does not grant freeze or merge.
- Analyze fix round 2: CHK003/010/019/038 reconciled to repaired ADR sequence, unchanged-legacy-contract evidence, deterministic wire contract, T018/T010 labeling, and T026 dual-receipt dependency.
- Analyze fix round 3: CHK005/013/027/040 reconciled to exclusive reason precedence, disjoint status precedence (including legacy non-200 baseline), missing-ID skip vs no-formable-input, and search/download 409 proof split (T010 vs T015).
- Analyze fix round 4: CHK027/031/038 reconciled to request-level wire branch + simultaneous-fault first-match in T010/T026 receipts, settlement/usability-freeze and Q2-409 `providers` presence vs request-level absence (brief.md:69), and Phase 10 wait for T015/T024/T028.
- Analyze fix round 5: CHK019/CHK027 reconciled to discriminating Q2 usability-freeze fixture in T010/T026 and contracts proof list (multi-provider; only config-usable settles timeout/search-429/failure/no-formable-input with off/refused sibling → 200, empty candidates, ordered named outcomes).
- Plan-challenge fix round: CHK002/004/007/008/013/016/029/030/031/033/040 reconciled to H1/M1/M2 dual-opt-in + destination trust + redacted warnings; M3/S1 ValidateOnStart + invalid timing; M4 search-only `providers` + download ProblemDetails; S2/S3 Enumeration/central handler; S4 host-lifetime state; S5 Patron attribution.
- Plan-challenge fix round 2: CHK002/029/040 reconciled to split search (all-enabled) vs download (bound-provider) 409 predicates (Ledger S1 / ADR 0052) and four independent typed-client HTTP gates in T006.
