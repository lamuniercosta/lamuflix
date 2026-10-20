# Specification Quality Checklist: DEV-341 GET /api/movies/{id}/recommendations with OpenAPI

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-10-20
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details beyond what the brief fixes — route, take/id/Pending policy, DTO field set, file envelope, proofs, generation chain, and Phase B order appear only where `brief.md` Q1–Q9 already fix them
- [x] Focused on user value and business needs (three stories: ranked recommendations with reasons, status contracts, OpenAPI/client freshness)
- [x] Written for stakeholders via Given/When/Then over observable outcomes
- [x] All mandatory sections completed (Domain vocabulary, User Scenarios & Testing, Requirements, Success Criteria, Assumptions)

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain — brief Q1–Q9 settle all grill items
- [x] Requirements are testable and unambiguous (FR-001–FR-014 restate brief Q1–Q9)
- [x] Success criteria are measurable (SC-001–SC-007 are pass/fail outcomes from the brief proof matrix)
- [x] Success criteria invent no latency or volume targets the brief did not set
- [x] All acceptance scenarios are defined (US1–US3 plus edge cases)
- [x] Edge cases are identified (take default/bounds/malformed, precedence, Pending, double-read cost, zero-score reasons, cancellation/errors, Phase B gate, property opt-out, exclusions)
- [x] Scope is clearly bounded (FR-013 Q6–Q7 file set; exclusions match brief Q8 frozen scope)
- [x] Dependencies and assumptions identified (Epic 7 + DEV-340/336–339 for Phase B, ASSUMPTIONS.md naming, Phase B recon stops, no owner checkbox, propertyTests opt-out)

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows (ranked JSON → status contracts → OpenAPI/TS/MSW)
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] Existing glossary terms **Recommendation** and **Reason** from root `CONTEXT.md` are included in published Spec Kit artifacts (`spec.md` Domain vocabulary, `plan.md` Domain vocabulary table, `tasks.md` vocabulary note); no new term added
- [x] No implementation detail leaks beyond the brief's own decisions

## Notes

- Research, data-model, contracts, quickstart, and design.md side files are not part of this draft. `plan.md` records why.
- `propertyTests: opt-out` is preserved from task note DEV-341 and restated in spec/plan/tasks (exit-2 no-tests-tagged only; never PASS via SKIPPED/SKIP/N/A).
- C1-T1 contract synchronized: `RecommendedMovieDto.Reasons` is `ImmutableList<string>`; map ordered `Reason.Text` via `ToImmutableList()`; explicit empty fixtures use `ImmutableList<string>.Empty` (never null/default); outer `Results<Ok<List<RecommendedMovieDto>>, BadRequest, NotFound>`, twelve fields, rank order, and JSON/OpenAPI/generated TypeScript `string[]` preserved (`spec.md` FR-006; `tasks.md` T003; `plan.md` Design item 3 / Complexity Tracking).
- C2-T1: Constitution Check cites numbered principles only. Ticket scope, no schema/migration, and Epic sequencing remain in sibling `Scope and prerequisite checks` in `plan.md` (principle: none applicable; PRODUCT/brief/care-list sources retained) — outside Constitution Check. C2-a/b/c resolved by C2-T1 in `DRAFTING_RECEIPT.md`.
- Round-1 plan-challenge repairs (authorized): F1 Patron no-departure — Constitution V check distinguishes HTTP parse/binding/adapter take-range `BadHttpRequestException` → 400 from Core FluentValidation decorator `ValidationException` → 422 (`brief.md:129-135`; `constitution.md:217-234`); F2 five-phase/Q7 order with expected inventory/drift failures closing at T008/T009/T012/T013; F3 fixed OpenAPI prose `The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.`; F4 configured harness thresholds only (no literal `-Threshold 6`); L1 plan wording `ports and adapters, feature-organised core`; Compass L2 Q9 path is technical/file-envelope (no ASSUMPTIONS taste line); Ledger O2/O3/O4/O5 citation and clarification repairs applied. Compass O2 analysis disposition left unaltered (factually rejected mapping).
- Round-2 M1 documentation sync (authorized): plan Constitution Check Tests/IX and Technical Context name approved xUnit v3 / Shouldly / NSubstitute / WebApplicationFactory / Testcontainers Postgres / AutoFixture / Faker.Net with fresh data, Theory + MemberData for repeated take matrices, and narrowly justified invariant-builder exception (`.specify/memory/constitution.md:318-325,392-393`; `brief.md:425` / `CONCLUSIONS.md:286`); configured-threshold rule preserved; no new-dependency or verified-reference claim. T002/T007 carry matching conventions without removing cases; T001 confirms Phase B accessibility or stops. Design 2 cites round-2 decision appendix `brief.md:410-437` / `CONCLUSIONS.md:271-298`. Compass L1 citation concordance and Compass L2 shared-cancellation record are recorded in that appendix (and DRAFTING_RECEIPT C2-T1 erratum) — both nonblocking; no third challenge or challenge-clearance claim. `spec.md:96` cancellation wording unchanged.
- Phase B recon obligations are already ruled in brief Q6–Q9.
- Owner checkbox: none (C1 amendment + C1-T1 + C2-T1 + Patron F1 + Compass L2 Q9 classification + round-2 M1/L1/L2 documentation; brief Q8). Gate 1 and plan freeze are not claimed.
