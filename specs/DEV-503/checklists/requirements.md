# Specification Quality Checklist: Rebuild browser player controls with CC panel and settings menu

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-11-12
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

- Spec cites MediaTimeDisplay / media-chrome / file paths only where the ticket and brief already make those owner/Patron rulings authoritative (`PRODUCT.md` §3; brief Q1/Q3/Q8); stakeholder scenarios remain user-facing. Q8 supersedes conflicting Q4 historical wording.
- Optional co-located panel filenames settled by brief Q7 (`SubtitlePanel.tsx` / `subtitlePanel.test.tsx` only if warranted; extraction not mandatory; no invented line-count threshold). Q8 adds `playerCopy.ts` / `subtitleCopy.ts` to the editable boundary. No open `needs decision` in `DRAFTING_RECEIPT.md` for inventable behaviour. Unverified media-chrome 4.19.4 installed exports remain a blocking T001 prerequisite, not invented facts (`recon-DEV-503` 212–214).
- Checklist revalidated after analyze fix round 1 (U1 Q7 propagation; C1 FR-018 verification coverage) on 2026-11-12.
- Checklist revalidated after plan-challenge round 1 repairs (`PLAN_ADJUDICATION.md` consolidated list 1–9) on 2026-11-12: source authority/recon 186–214; Q8 file boundary and a11y/event/Escape/rate/cleanup precision; Bernstein/Rigger spec-PR producer before T000; PLAN FROZEN withheld pending Keel independent validation. Residual same-round precision: US4 scenario 2 consumption-gated hotkeys; T013 explicit Escape/fullscreenchange/movie-change implementation. No Gate 1 / implementation / runtime / merge claim. Palette independently repairs `design.md`.
