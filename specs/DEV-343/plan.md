# Implementation Plan: DEV-343 RelatedMovies on Movie Details with reason chips and MSW mocks

**Branch**: `feature/343-spec` | **Date**: 2026-10-20 | **Spec**: `specs/DEV-343/spec.md`

**Input**: Feature specification from `specs/DEV-343/spec.md`, settled grill brief `specs/DEV-343/brief.md`, facts from note `recon-DEV-343`, Patron rulings in `specs/DEV-343/CONCLUSIONS.md`, taste receipt `specs/DEV-343/ASSUMPTIONS.md`, `specs/PRODUCT.md`.

**Status**: Draft planning artifact only (Phase A). Not frozen; Gate 1 not claimed. No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. Phase B remains gated on merged DEV-323 and DEV-341, landed generated contract, Wisp drift recon, Gauge baseline, and Keel pickup clearance (brief Q1, Q6).

## Summary

Deliver RelatedMovies on Movie Details consuming the landed DEV-341 recommendations contract: typed query with empty/loading/populated and local retryable error states; ordered verbatim Reason chips on safe cards with details navigation; and an exported presentational horizontal keyboard-accessible MovieCarousel for DEV-418 reuse. Prove with generated-contract MSW fixtures, Vitest/RTL, and built-UI browser verification. Additive existing-folder integration only. Nothing else ships.

## Domain vocabulary

| Term | Definition source | Notes |
|------|-------------------|-------|
| Recommendation | root `CONTEXT.md` | Suggested library movie from the details page |
| Reason | root `CONTEXT.md` | Caller-safe explanation; wire form is API Reasons strings |
| MovieCarousel | brief Q2–Q3 | Presentational horizontal scroll; not a glossary term |
| RelatedMovies | brief Q2, Q4 | Movie Details section owning query, states, cards |

## Technical Context

**Language/Version**: TypeScript / React 19 SPA under `web/`; no C# production changes in this ticket.

**Primary Dependencies**: None added. Consume existing React, TanStack Query, openapi-fetch, MSW, Vitest, and RTL already referenced in `web/package.json` (`recon-DEV-343:84`). Exact pins remain Phase B recon facts.

**Storage**: N/A. No database, schema, or migration change (`recon-DEV-343:86`).

**Testing**: Vitest + RTL + MSW with existing test server (`web/src/test/server.ts`), typed fixtures, generated handler and per-test MSW overrides; handler success/status smoke and `client.typecheck.ts` only as needed; built-UI browser verification for overflow/focus/keyboard/responsive/reduced-motion. No new end-to-end framework or dependency. No Gherkin/Reqnroll. **propertyTests: opt-out** per brief Q6–Q7 (presentation/contract-only change; no domain invariants). Gate bar per brief Q6 Gauge expectations and `AGENTS.md` — do not invent unsupported analyzer specifics.

**Target Platform**: Existing Vite/React SPA (`web/`). ui ticket (product UI).

**Project Type**: Existing web feature folder under `web/src/features/movie-details/` plus shared presentational UI under `web/src/ui/` (`recon-DEV-343:19,87`).

**Performance Goals**: No numeric latency target in the brief.

**Constraints**: No new NuGet/npm dependency. No new project, top-level folder, or architectural layer. No backend route/status/DTO change. No schema/migration. No `Features:LocalPlay`, secrets, or `Process.Start`. No hand-written DTOs or hand-edited generated files. Thresholds stay in `harness.yml`. Preserve `propertyTests: opt-out` disposition. Follow established conventions and mandatory gate scripts per brief Q6 / `AGENTS.md`.

**Scale/Scope**: Exactly the brief Planned implementation file set (seven new web files plus named additive edits). Frozen scope sentence (brief Q7): RelatedMovies on Movie Details consuming landed DEV-341; exported presentational horizontal keyboard-accessible MovieCarousel reusable by DEV-418; ordered verbatim Reason chips, safe cards and details navigation; required states with local error isolation; generated-contract MSW fixtures/proofs and specified tests/accessibility/browser/gates, with additive existing-folder integration only; anything else is a follow-up issue, not a finding in this round.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Every gate evaluated in this Constitution Check cites its supporting numbered principle; separate scope and prerequisite checks follow below with their own authority citations.*

- Frontend stack: Vite + React + TypeScript strict, TanStack Query, openapi-typescript + openapi-fetch; Vitest + RTL + MSW (Technology Stack Constraints, `.specify/memory/constitution.md:357`; brief Q2; `recon-DEV-343:128`).
- API and Contract Rules: OpenAPI committed at `web/src/api/openapi.json`; TypeScript client and MSW handlers generated from that document, never hand-written (`.specify/memory/constitution.md:492-494`; brief Q1, Q6; charter contract chain).
- Ubiquitous Language: **Recommendation** and **Reason** already in root `CONTEXT.md`; no new glossary term; no ADR edit (Principle VIII, `.specify/memory/constitution.md:286,290-294,502-503`; brief Q1, Q7; `recon-DEV-343:160-161`).
- No new port, MediatR, generic repository, or UnitOfWork (PRODUCT.md; constitution I/II). Scope stays inside existing `web/src` feature and UI folders (`recon-DEV-343:85`).
- No secret, `Features:LocalPlay`, or `Process.Start` (constitution VII; care list item 5; `recon-DEV-343:88`). RelatedMovies/carousel/MSW scope does not reference play execution.
- Test Pyramid / web tests: Vitest + RTL + MSW only; no second assertion/mocking stack; generated contract types consumed, not duplicated (constitution IX; brief Q6).
- Comments: no explanatory comments beyond AAA / empty-block / scoped suppressions (`.specify/memory/constitution.md:394-396`).
- Gates from configured scripts using harness thresholds only; never lower thresholds or invent a threshold value.

Post-design re-check: the numbered-principle Constitution Check gates above still hold. This plan adds no package, project, new port name, CONTEXT.md edit, ADR, or file outside the approved brief file set. Scope, schema/migration, and epic-sequencing constraints remain in force under **Scope and prerequisite checks** below; they are not waived by this re-check and are not Constitution Check gates. Side design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `design.md`) are not produced by Quill — the brief already freezes contracts, files, ordering, and proofs; Palette owns `design.md` before plan challenge.

## Scope and prerequisite checks

Binding product, care-list, and Phase B prerequisite constraints. **Numbered principle: none applicable** for each item; do not invent I/II/IX citations. Constraints remain in force.

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-343 Scope & Technical Design and Acceptance Criteria (`recon-DEV-343:105-120`; brief Goal). Nothing is added or dropped. No owner checkbox (brief Q7).
- No schema or migration (care list item 3; brief Q2).
- Public API shape is consumed, not changed: landed DEV-341 `GET /api/movies/{id}/recommendations` and RecommendedMovieDto twelve-field wire form (`recon-DEV-343:29,87`; brief Q1). DEV-343 does not implement the backend endpoint.
- Epic / sequencing: preserve PRODUCT Epic 7 sequencing; Phase B waits for merged DEV-323 and DEV-341 and verifies the landed C# DTO → OpenAPI → generated TypeScript/MSW contract (brief Q1; `specs/PRODUCT.md` §2). Full Phase B stop condition: Gate 1, merged prerequisites, Wisp drift recon, Gauge baseline, Keel pickup clearance; stop on missing evidence or contract mismatch (brief Q1, Q6). This is a prerequisite statement, not evidence that dependencies have landed. At base HEAD the route, generated types, and handler are absent (`recon-DEV-343:50-58`).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-343/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
└── design.md                 # Palette-owned; required before plan challenge
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. `AGENTS.md` is not updated: outside the feature directory and outside the named file set.

### Source Code (repository root)

```text
web/src/ui/MovieCarousel.tsx                         # new — exported presentational carousel
web/src/ui/MovieCarousel.css                         # new
web/src/ui/MovieCarousel.test.tsx                    # new — focused RTL + reuse proof

web/src/features/movie-details/RelatedMovies.tsx     # new — section, cards, Reason chips, states
web/src/features/movie-details/RelatedMovies.css     # new
web/src/features/movie-details/RelatedMovies.test.tsx # new
web/src/features/movie-details/relatedMoviesApi.ts   # new — typed query

web/src/features/movie-details/MovieDetailsView.tsx  # additive host integration
web/src/features/movie-details/MovieDetailsView.test.tsx  # additive integration/regression

web/src/mocks/fixtures.ts                            # additive typed fixtures as needed
web/src/mocks/handlers.ts                            # consume landed generated; regenerate only via chain
web/src/api/client.typecheck.ts                      # additive typed exercise as needed
web/src/api/openapi.json / web/src/api/types.ts      # consume landed; regenerate only via chain if drift
```

**Structure Decision**: Match existing Movie Details feature folder and shared UI module locations (`recon-DEV-343:19,87`). Navigation helpers and MorePosterCard patterns already in `MovieDetailsView.tsx` are reuse precedents (`recon-DEV-343:35-36`), not new layers. No new project or top-level folder.

## Design (from settled brief)

1. **Dependencies and contract (Q1)**: Plan now; Phase B waits for merged DEV-323 and DEV-341 and verifies the landed contract. Do not implement the backend endpoint, hand-write DTOs, or invent an absent contract. Preserve PRODUCT Epic 7 sequencing and existing **Recommendation** / **Reason** vocabulary.
2. **Component seam (Q2)**: Export presentational `MovieCarousel` under `web/src/ui/`; RelatedMovies and its query live under `web/src/features/movie-details/`. Callers compose cards. Existing React/TanStack Query/openapi-fetch/MSW/Vitest/RTL suffice. Additive host/mock/test edits only.
3. **Keyboard and scroll (Q3)**: Labelled region, semantic list, focusable viewport; ArrowLeft/ArrowRight only on viewport self-focus; Previous/Next disabled at boundaries and when content fits; native Tab/Enter and modified links; no wrap, autoplay, or roving focus; visible focus and reduced-motion support. Palette owns visuals without changing this contract.
4. **Query and states (Q4)**: `relatedMoviesApi.ts` keyed by movieId; enabled only for valid loaded details; default landed parameters; preserve API order; identity isolation; loading / empty / populated / local Retry error; recommendations 404 is not host-movie-missing.
5. **Cards, Reasons, navigation, placement (Q5)**: Title, PosterUrl fallback, optional ReleaseYear, verbatim ordered Reason chips; single `/movies/{id}` anchor with existing SPA + libraryReturnUrl behavior; place after MoreWithPanel and before FileSection.
6. **Files, tests, gates (Q6)**: Brief file set and verification matrix; consume generated handlers; regenerate only through the contract chain; Phase B `check:api`, `run-web-gates.ps1`, and applicable Phase 3 step 5 gates; property opt-out as recorded.
7. **Loop discipline (Q7)**: Critical/High block; Medium/Low disposition required; plan challenge and remediation each cap at two rounds; frozen scope sentence above.

## Complexity Tracking

> No constitution violations requiring justification. Presentational carousel + feature-local RelatedMovies stay inside existing web folders; contract consumption follows the mandatory OpenAPI → generated TS/MSW chain; no new architectural layer.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phase boundaries

- **Phase A (this ask)**: Spec Kit docs under `specs/DEV-343` only (`spec.md`, `plan.md`, `tasks.md`). No freeze, publish, gates, or implementation. Palette `design.md` must exist before plan challenge.
- **Phase B**: After Gate 1, merged DEV-323 and DEV-341, landed generated contract, Wisp drift recon, Gauge baseline, and Keel pickup clearance with stop-on-divergence. Then Anvil implements per `tasks.md`; Cog refactor; Gauge gates.
