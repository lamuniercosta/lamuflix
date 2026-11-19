# Specification Quality Checklist: Whole-library Related strong matches

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-11-19
**Updated**: 2026-11-19 (analyze round 2 residual compile-boundary fix)
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] Ticket-pinned technical contracts from the ticket/brief (port method on `IRecommendationCandidateSource`, revised candidate/feature-state shape, `MinimumSimilarity` / `StrongReasonCriteria` defaults and domains, `CandidatePoolSize` take-cap role, empty-rail copy, DEV-545/DEV-518 evidence boundaries) are stated accurately; narrative user-value sections stay outcome-focused where the ticket does not pin a contract
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders where behaviour is described; technical contract lines cite ticket/brief authority rather than inventing stack choices
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria name measurable outcomes; brief/ticket Done-when verification means (DEV-545 coverage table / score-band samples / judged pairs / 450-movie warm; EF interceptor; Vitest/RTL) may be cited as evidence paths without becoming the outcome itself
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

- [x] "What the User Will See" names each visible change by page or component, or says "Nothing visible" with a reason
- [x] Each visible change has a SMOKE-### check in user terms executed by the **scoped Vitest + RTL runner** on `web/src/features/movie-details/RelatedMovies.test.tsx` (never Compose UI smoke execution; never browser e2e / `web/e2e`)
- [x] What each visible change needs to show up in a plain `docker compose up` is listed (config, data, services) for operator context; smoke execution itself remains Vitest/RTL as above

## Analyze round 1 alignment (Keel fix list)

- [x] DEV-545 before snapshot is an explicit prerequisite before Related path replacement; after snapshot stays after automated verification
- [x] Goal first-placement (Pirate Radio / Hercules), 450-movie warm timing, quality coverage, judged-pair attachments, DEV-518 Plot evidence, and selected-relative cosine verification if Similar survives are restored
- [x] Explicit selected-vector guard on live Related handler path with regression test; T009 before T015 20-vs-200 command-count
- [x] Below-bar Themes-only admission at value 1 alongside just-below-ceiling exclusion; automated bar-config eligibility assertion
- [x] Contract chain regenerates/verifies `web/src/api/openapi.json` and downstream generated outputs; generated paths allowed in plan
- [x] Complete Vitest/RTL SMOKE-001/002 (empty copy + no carousel; Related heading + API order without padding)
- [x] Passing `LamuFlix.ArchitectureTests` gate and receipt required in plan/T019
- [x] Principle III is **N/A** alone; validated options precede handler wiring; read-only pickup analysis distinguished from authorized task amendment

## Analyze round 2 alignment (Keel fix list)

- [x] T021/SC-001/plan evidence require DEV-545 coverage table, score-band samples, judged pairs; ticket-named odd keyword pairs recorded as bad judgments; stoplist/`ThemesCeiling` via DEV-518; defaults frozen; ticket 450-movie warm benchmark
- [x] T006 EF missing-feature-row load distinct from T015 live Related shared-director-without-embedding in the EF suite after T009
- [x] Unsupported For-you/`GetCandidatesAsync` compile claim removed; Phase 3/T009 compile boundary aligns with migrating the confirmed Related caller to the replacement result shape
- [x] Residual: T006 execution/green requires T009 (or staged buildable migration); phase graph / US1 / parallel checkpoints / plan aligned; T006 kept distinct from T015
- [x] This checklist describes ticket-pinned contracts and scoped Vitest/RTL smoke (no Compose smoke / no browser e2e invent)
- [x] `tasks.md` Input source is `specs/DEV-544/`

## Analyze I1 alignment (Keel Medium finding)

- [x] T006 checkbox moved under a real Phase 4b heading after US2 (Phase 4); no longer under Phase 3
- [x] Staged-migration exception retained on T006
- [x] T006 EF missing-feature-row load kept distinct from T015 live Related proofs
- [x] Worktree and private artifact plan/tasks copies mirrored

## Notes

- Spec Kit default SMOKE → `web/e2e` / Compose UI smoke mapping is **not** used. SMOKE-001/002 map exclusively to Vitest + RTL on brief-named `web/src/features/movie-details/RelatedMovies.test.tsx`, matching constitution IX / stack practice used on DEV-522. Runtime Pirate Radio / Hercules first placements, coverage table, score-band samples, judged pairs, and 450-movie warm timing remain DEV-545 evidence, not browser e2e and not Compose smoke execution.
- SC-001/SC-002/SC-003 cite measurable outcomes; DEV-545 script and interceptor are verification means named in the brief Done when / test strategy, not success-criterion implementation leakage in the stakeholder sense.
- Former ThemesCeiling invent gap closed by brief **Drafting clarification for Q1 and Q5**: draft against DEV-518 contract; map Collection/Director/Themes directly; DEV-518 owns normalization/ceiling; Build pickup requires merged DEV-518 or Bernstein dependency blocker. No open `needs decision:` in `DRAFTING_RECEIPT.md`.
- Analyze round 2: Quill applied Keel numbered fixes 1–5 to `spec.md`, `plan.md`, `tasks.md`, this checklist, and `DRAFTING_RECEIPT.md` (both plan/tasks copies mirrored). Residual compile-boundary fix applied afterward (T006 green requires T009 or staged migration). Analyze I1 applied (T006 under real Phase 4b after US2). Gate 1 / PLAN FROZEN not claimed.
