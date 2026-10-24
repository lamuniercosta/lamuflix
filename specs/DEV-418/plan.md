# Implementation Plan: DEV-418 For You recommendation rail above the Library browse grid

**Branch**: `feature/418-spec` | **Date**: 2026-10-24 | **Spec**: `specs/DEV-418/spec.md`

**Input**: Feature specification from `specs/DEV-418/spec.md`, frozen grill brief `specs/DEV-418/brief.md`, facts from note `recon-DEV-418`, Patron rulings in `specs/DEV-418/CONCLUSIONS.md`, taste receipt `specs/DEV-418/ASSUMPTIONS.md`, `specs/PRODUCT.md`.

**Status**: Draft planning artifact only (Phase A). Not frozen; Gate 1 not claimed. No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. Phase B remains STOP until DEV-357 has landed and the C# DTO → OpenAPI → generated TypeScript chain is available and checked for drift at the picked HEAD (brief Q1, Q7).

## Summary

Deliver a frontend-only For You rail above the Library Results section: ForYouRail on unchanged MovieCarousel and MovieCard; eligibility via existing filter/watchlist calculations; independent forYouApi consumer of landed DEV-357 GET /api/recommendations/for-you with take=10, preserved order, and typed reasons; quiet pending/error and 200 [] hide; reused Library navigation and feedback controls; bounded LibraryView/fixture edits and generated-only contract/handlers; Vitest/RTL + typed MSW proofs and Palette design conformance. Nothing else ships.

## Domain vocabulary

| Term | Definition source | Notes |
|------|-------------------|-------|
| For You | brief Q5 | This Library recommendation rail; not a CONTEXT.md glossary entry |
| Personal reason | brief Q5 | Returned caller-safe reason with kind `personal` |
| Cold start | brief Q3, Q5; DEV-357 `spec.md:82` | No-positive-history condition; UI must not claim [] proves it |
| Library browse grid | brief Q5 | Existing Results region |
| RecommendedMovieDto | recon-DEV-418:127; DEV-357 | Generated consumer DTO; verify at pickup |

CONTEXT.md is not edited. Proposed ADR-0021 under specs/DEV-321 remains untouched.

## Technical Context

**Language/Version**: TypeScript / React 19 SPA under `web/`; no C# production changes in this ticket.

**Primary Dependencies**: None added. Consume existing React, TanStack Query, openapi-fetch, MSW, Vitest, and RTL already referenced in `web/package.json`. Exact pins remain Phase B recon facts. MovieCarousel from DEV-343 is reused unchanged (`recon-DEV-418:125`).

**Storage**: N/A. No database, schema, or migration change (brief Q6, Q8).

**Testing**: Vitest + RTL + MSW with existing test server, typed fixtures, generated handler and per-test MSW overrides; feature-local `ForYouRail.test.tsx`, `forYouApi.test.ts`, `LibraryView.forYou.test.tsx`, `mocks/forYou.test.ts`. No new end-to-end framework or dependency. No Gherkin/Reqnroll. **propertyTests: opt-out — frontend presentation/query integration has no new .NET domain invariants** (brief Q7). Gate bar per brief Q7 Gauge expectations and `AGENTS.md` — do not invent unsupported analyzer specifics.

**Target Platform**: Existing Vite/React SPA (`web/`). ui ticket (product UI).

**Project Type**: New feature-local folder under `web/src/features/for-you/` plus bounded additive edits to LibraryView and mocks (`recon-DEV-418`; brief Q6).

**Performance Goals**: No numeric latency target in the brief.

**Constraints**: No new NuGet/npm dependency. No new project, top-level folder, or architectural layer/state library. No backend route/status/DTO change (consume DEV-357 by reference). No schema/migration. No `Features:LocalPlay`, secrets, or `Process.Start`. No hand-written DTOs or hand-edited generated files. No bespoke for-you invalidation in feedback/enrichment stores. Thresholds stay in `harness.yml`. Preserve `propertyTests: opt-out` disposition. Follow established conventions and mandatory gate scripts per brief Q7 / `AGENTS.md`.

**Scale/Scope**: Exactly the brief Planned new files and bounded existing changes (brief Q6). Frozen scope sentence (brief Q8): Frontend ForYouRail and forYouApi on unchanged MovieCarousel and MovieCard above the Library Results section; existing filter/watchlist suppression; generated DEV-357 consumer with take=10, returned order and typed reasons; quiet asynchronous states; reused navigation and existing controls; bounded LibraryView/fixture edits and generated-only contract/handlers; Q7 behavior tests and Palette design conformance; anything else is a follow-up issue, not a finding in this round.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Every gate in this Constitution Check cites a numbered Core Principle (I–IX). Constraints that live only under unnumbered constitution sections are listed in **Unnumbered constitution authority checks** below, not here. Scope and Phase B prerequisites remain under **Scope and prerequisite checks**.*

- **Principle I** (ports and adapters, feature-organised Core; `.specify/memory/constitution.md:127-159`): No new port, generic repository, or UnitOfWork. Scope stays inside existing `web/src` feature and library folders (brief Q6; PRODUCT.md).
- **Principle II** (explicit handlers and decorators; `.specify/memory/constitution.md:161-178`): No MediatR. This frontend-only slice adds no handler/decorator registration surface (brief Q4, Q6).
- **Principle VII** (configuration isolation and deterministic time; `.specify/memory/constitution.md:275-294`): No secret, `Features:LocalPlay`, or `Process.Start` (care list item 5; brief Q6). Unchanged MovieCard reuse preserves execution behavior.
- **Principle VIII** (ubiquitous language; `.specify/memory/constitution.md:296-311`): Feature-local operational terms only (For You, Personal reason, Cold start, Library browse grid); no new glossary entry; CONTEXT.md untouched; Proposed ADR-0021 untouched; no new ADR (brief Q5–Q6; `recon-DEV-418:99-107`).
- **Principle IX** (test pyramid; `.specify/memory/constitution.md:316-343`): Vitest + RTL + MSW only; no second assertion/mocking stack; generated contract types consumed, not duplicated (brief Q7).

### Unnumbered constitution authority checks

Binding constraints from unnumbered constitution sections. **Numbered principle: none** for these items; do not invent I–IX mappings.

- **Technology Stack Constraints** (`.specify/memory/constitution.md:369`): Frontend stack is Vite + React + TypeScript strict, TanStack Query, Zod, `openapi-typescript` + `openapi-fetch`; Vitest + React Testing Library + MSW (brief Q4, Q6).
- **API and Contract Rules** (`.specify/memory/constitution.md:504-506`): OpenAPI committed at `web/src/api/openapi.json`; TypeScript client and MSW handlers generated from that document, never hand-written (brief Q1–Q2, Q6; charter contract chain). Consumed contract recorded by citation to `specs/DEV-357/spec.md:79-87`, not redefined.
- **Coding Conventions — Comments** (`.specify/memory/constitution.md:452-462`): No explanatory comments beyond AAA test headers, empty-block justifications, and scoped warning suppressions.
- **Pull Request Quality Gates** (`.specify/memory/constitution.md:406-408`): PR checklist requires the same comment exemptions (AAA / empty-block / scoped suppressions). Gate runs use configured scripts and harness thresholds only; never lower thresholds or invent a threshold value (brief Q7 / `AGENTS.md`).

Post-design re-check: the numbered-principle Constitution Check gates above still hold. This plan adds no package, project, new port name, CONTEXT.md edit, ADR, or file outside the approved brief file set. Unnumbered constitution authority checks and scope/schema/migration/epic-sequencing constraints remain in force under their sections below; they are not waived by this re-check and are not Constitution Check gates. Side design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) are not produced by Quill — the brief already freezes contracts, files, ordering, and proofs; Palette owns `design.md` before plan challenge.

## Scope and prerequisite checks

Binding product, care-list, and Phase B prerequisite constraints. **Numbered principle: none applicable** for each item; do not invent I/II/IX citations. Constraints remain in force.

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). Full ticket Scope & Technical Design and separate acceptance criteria remain NOT FOUND; Phase A drafts against task-note scope DEV-418:26-28 as Patron ruled (brief Q2). Nothing is added or dropped beyond the frozen brief. No owner checkbox (brief Q6).
- No schema or migration (care list item 3; brief Q8).
- Public API shape is consumed, not changed: predecessor GET /api/recommendations/for-you and RecommendedMovieDto / typed reasons per `specs/DEV-357/spec.md:79-87` (brief Q2). DEV-418 does not implement the backend endpoint.
- Epic / sequencing: preserve PRODUCT Epic 7 sequencing; Phase B STOP until DEV-357 lands with verified generated contract chain (brief Q1; `specs/PRODUCT.md` §2). Full Phase B stop condition: Gate 1, landed DEV-357 route/DTO/reasons, generated OpenAPI/types/MSW, MovieCard assignability, pickup drift check; stop on missing evidence or contract mismatch (brief Q1, Q7). This is a prerequisite statement, not evidence that DEV-357 has landed. At base HEAD the for-you operation, client path, query key, handler, fixture, and invalidation are absent (`recon-DEV-418:146,158,167,175,177-179`).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-418/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── DRAFTING_RECEIPT.md
└── design.md                 # Palette-owned; required before plan challenge
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. `AGENTS.md` is not updated: outside the feature directory and outside the named file set.

### Source Code (repository root)

```text
web/src/features/for-you/ForYouRail.tsx              # new — rail section, quiet states, carousel composition
web/src/features/for-you/ForYouRail.css              # new — required by design.md; three rules only
web/src/features/for-you/forYouApi.ts                # new — typed query key ['forYou', 10], take=10
web/src/features/for-you/ForYouRail.test.tsx         # new — includes focus-continuity proofs
web/src/features/for-you/forYouApi.test.ts           # new

web/src/library/LibraryView.tsx                      # additive mount/eligibility/callback/client wiring
web/src/library/LibraryView.forYou.test.tsx          # new — eligibility/navigation/cached-state regressions

web/src/mocks/fixtures.ts                            # additive generated-type fixtures + success aggregate
web/src/mocks/handlers.ts                            # consume landed generated; regenerate only via chain
web/src/mocks/forYou.test.ts                         # new — generated MSW consumer smoke

web/src/ui/MovieCarousel.tsx                         # reuse unchanged
web/src/library/MovieCard.tsx                        # reuse unchanged
web/src/api/client.ts                                # reuse unchanged
web/src/api/openapi.json / web/src/api/types.ts      # consume landed; regenerate only via chain if drift
```

**Structure Decision**: Match brief Q6 feature-local `web/src/features/for-you/` plus bounded Library/mocks edits. RelatedMovies (`recon-DEV-418:124-125`) is the composition precedent; LibraryView eligibility calculations (`recon-DEV-418:130-138`) are reused inline. No new project or top-level folder.

## Design (from settled brief)

1. **Ownership and dependencies (Q1)**: Frontend-only consumer of DEV-357. Phase A specifies against evidenced predecessor design. Phase B STOP until landed contract chain is verified. No substitute endpoint or hand-written DTOs.
2. **Consumer contract (Q2)**: Cite `specs/DEV-357/spec.md:79-87`. Verify landed RecommendedMovieDto (`recon-DEV-418:127`) and for-you operation at pickup. Preserve Proposed ADR-0021; no router; existing browse URL ownership.
3. **Placement and visibility (Q3)**: Above Results; eligibility `!activeFilters && !watchlistActive`; take=10; hide on 200 []; preserve order and conditional Personal/structural reasons.
4. **Query and async (Q4)**: Key `['forYou', 10]`; quiet pending/error; retain nonempty cache only while eligible; no bespoke invalidation.
5. **Presentation and navigation (Q5)** plus Palette `design.md` refinements: Heading For You; unchanged MovieCarousel/MovieCard; existing openMovie and feedback clients. `ForYouRail.css` is required with exactly three rules — `.for-you-rail`, `.for-you-heading` (shelf-stamp values), `.for-you-card` (rail card geometry) — per `design.md:194-211`; chip styles remain the shared `.related-movie-reasons` treatment from unchanged MovieCard. Focus continuity is rail-owned inside `ForYouRail.tsx` per `design.md:284-311` / `301-311`: (a) nonempty refresh that removes the focused card moves focus to the For You heading (`h2`, `tabIndex={-1}`); (b) whole-shelf removal on successful `[]` refresh or eligibility unmount, when focus is inside the shelf, moves focus to stable Library heading `h2.library-title` (scoped lookup, temporary `tabindex="-1"`, missing-landmark no-op); (c) focus outside the shelf is preserved. No new file beyond the brief set.
6. **Files and charter (Q6)**: Brief file set; deliberate care-list rulings 1/2/4/6 recorded; no owner checkbox.
7. **Tasks, tests, gates (Q7)**: Brief task ordering and Vitest/RTL + MSW matrix, including focus-continuity proofs on authorized `ForYouRail.test.tsx` / `LibraryView.forYou.test.tsx`; Gauge Phase 3 bar; propertyTests opt-out line preserved.
8. **Loop discipline (Q8)**: Critical/High/Medium block; two review rounds max; two fix commits per round; frozen scope sentence above.

## Complexity Tracking

> No constitution violations requiring justification. Feature-local for-you files reuse established web patterns; contract consumption follows the mandatory OpenAPI → generated TS/MSW chain; no new architectural layer.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phase boundaries

- **Phase A (this ask)**: Spec Kit docs under `specs/DEV-418` only (`spec.md`, `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md`). No freeze, publish, gates, or implementation. Palette `design.md` must exist before plan challenge. Keel owns read-only speckit-analyze and brief alignment next.
- **Phase B**: After Gate 1 and verified landed DEV-357 contract (route, DTO/reasons, OpenAPI/types/MSW, MovieCard assignability) with stop-on-divergence. Then Build implements per `tasks.md`; Cog refactor; Gauge gates.
