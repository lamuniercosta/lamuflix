# Specification Quality Checklist: Multi-provider subtitle acquisition seam

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-11-28
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## User-Visible Outcome

- [x] "What the User Will See" names each visible change by page or component, or says "Nothing visible" with a reason
- [x] Each visible change has a SMOKE-### check in user terms that can run against the Compose stack
- [x] What each visible change needs to show up in a plain `docker compose up` is listed (config, data, services)

## Notes

- User-visible outcome is **Nothing visible** (`ui:none`; additive API for later picker; existing web legacy contract preserved). Smoke subsection correctly omitted per template when nothing is visible to end users.
- Spec cites ticket/brief-named paths, DTO field names, and config keys only where `brief.md` Q1–Q12 already freeze them. Behaviour in user stories and success criteria stays outcome-focused.
- Checklist validation pass 1 (2026-11-28): all items pass. Grill Q1–Q12 closed; no inventable `[NEEDS CLARIFICATION]` markers. Clarify session recorded in `spec.md`. Ready for `/speckit-plan`.
- Analyze fix round 1 (2026-11-28): revalidated after Keel numbered fixes 1–7. Content-quality “no implementation details” remains satisfied for stakeholder behaviour; authorized file/type names appear where the ticket and brief already freeze the landing set (same pattern as DEV-549). Constitution I Core-side lookup vs outside-Core typed-client composition, Constitution VII request-time 409 on search and download, Q5/Q7 test mapping, contract normalization, delivery-gate ordering, and checklist ref repairs are reflected in `spec.md` / `plan.md` / `tasks.md` / `contracts/search-response.md` / `checklists/multi-provider-subtitles.md`.
- Analyze fix round 2 (2026-11-28): revalidated after Keel numbered fixes 1–5. ADR Accept moved to first delivery task after Gate 1 (before-deliverable-merge bar retained); unverified legacy field table removed (unchanged legacy contract + T010 parity); deterministic state/reason/status wire rules; Q7 over-capacity immediate-resolve and later-search eviction proofs; T018/T010 labeling and T026 dual-receipt dependency; CHK010/019/038 reconciled.
- Analyze fix round 3 (2026-11-28): revalidated after Keel numbered fixes 1–4. Disjoint HTTP status precedence (request-level / Q2 409 / one-provider legacy including baseline non-200 / multi-provider 200); exclusive reason predicates (disabled / unsafe URL / missing-invalid config including timeout-gap); missing-ID skip vs no-formable-input edge; plan Verification split search T010 vs download T015; CHK005/013/027/040 and receipt reconciled.
- Analyze fix round 4 (2026-11-28): revalidated after Keel numbered fixes 1–3 plus brief.md:69 settlement ruling. T010/T026 require request-level error branch and simultaneous-fault first-match; usability freeze + Q2-409 `providers` presence vs request-level absence in FR-003/FR-004/contracts/CHK027; Phase 10 waits for all in-scope receipts including T015/T024/T028; stale round-2 draft labels refreshed.
- Analyze fix round 5 (2026-11-28): revalidated after Keel numbered fix item 1. Discriminating Q2 usability-freeze fixture required in T010/T026 and contracts proof list (multi-provider; only config-usable settles timeout/search-429/failure/no-formable-input with off/refused sibling → 200, empty candidates, ordered named outcomes); CHK019/CHK027 reconciled.
- Plan-challenge fix round (2026-11-28): revalidated after Keel’s 6-item Quill fix list (H1/M1/M2, M3/S1, M4, S2/S3, S4, S5). Dual HTTP opt-in + credentialed refusal + response-independent destinations + redacted warnings; ValidateOnStart exception + invalid-timing continuity; search-only `providers` + unchanged download ProblemDetails; Core Enumeration + JSON Name + central handler with T001 Q10 stop; host-lifetime Core-side spacing/observation state; Patron S5 attribution throughout. CHK002/004/007/008/013/016/029/030/031/033/040 reconciled.
- Plan-challenge fix round 2 (2026-11-28): revalidated after Keel’s 3-item fix list (note DEV-551:55–58). Split search (all-enabled) vs download (bound-provider after read-only, even with usable sibling) 409 predicates in FR-002/scope item 11; T014/T015 + plan Verification bound-provider case with zero calls to either provider and legacy status/ProblemDetails-without-`providers` on 409/read-only/stale-invalid; T006/CHK029 four independent typed-client HTTP gates; prerequisite-resolved private TASKS mirror refreshed to byte-identity with canonical tasks.md.
- Technology-agnostic success criteria: SC items describe observable parity, isolation, health, ship-review/rebased-head gate evidence, and setup-doc outcomes without prescribing frameworks beyond the ticket’s named proof surfaces.
- Preserved: Q6 `[assumed]` ordering, DEV-525 Done-before-Build, existing Q12 round caps.
