# Specification Quality Checklist: Subtitle download-host fail-closed guard

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-11-23
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] Ticket-pinned technical contracts from the ticket/brief (`DownloadLinkHost`, allowlist membership, 409/502 outcomes, process-local refused-host latch, home-cluster hosts, live script, implement-skill trigger) are stated accurately; narrative sections stay outcome-focused where the ticket does not pin a contract
- [x] Focused on user value and business needs (stop undownloadable candidates and wasted quota)
- [x] Written for non-technical stakeholders where behaviour is described; technical contract lines cite ticket/brief authority rather than inventing stack choices
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria name measurable outcomes; brief/ticket verification means (unit/integration homes, `Test-NewHomeCluster.ps1`, live `-Download` receipt, Size M gates) may be cited as evidence paths without becoming the outcome itself
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] Spec does not invent stack or API shape beyond ticket-pinned technical contracts and brief rulings

## User-Visible Outcome

- [x] "What the User Will See" states nothing visible as a new UI surface, with reason (no SPA/toast work; existing 409/502 contracts)
- [x] No SMOKE-### Compose/browser e2e invent — ticket excludes UI toast work; verification is unit/integration/script/live-check per brief
- [x] Operator needs for corrected home-cluster settings and post-merge redeploy are listed in scope / US3

## Notes

- Spec Kit default SMOKE → `web/e2e` / Compose UI smoke mapping is **not** used. This ticket has no web file changes and explicitly keeps on-screen toasts out of scope.
- Grill Q1–Q8 closed; interactive `/speckit-clarify` not required for inventable behaviour.
- Gate 1 / PLAN FROZEN not claimed by this checklist.
