# Implementation Plan: DEV-355 Watched and Not Interested actions with reason chips

**Branch**: `feature/355-spec` | **Date**: 2026-10-22 | **Spec**: `specs/DEV-355/spec.md`

**Input**: Feature specification from `specs/DEV-355/spec.md`, settled grill brief `specs/DEV-355/brief.md`, facts from note `recon-DEV-355`, Patron rulings in `specs/DEV-355/CONCLUSIONS.md`, taste receipt `specs/DEV-355/ASSUMPTIONS.md`, `specs/PRODUCT.md`.

**Status**: Draft planning artifact only (Phase A). Not frozen; Gate 1 not claimed. No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. Phase B remains gated on merged Gate 1, Build pickup drift check, and Wisp binding of DTO availability, existing cache keys, and consumer propagation in the approved surface (brief Q5/Q6).

## Summary

Deliver frontend consumption of existing DEV-352 feedback endpoints and DEV-354 reason strings: Mark Watched and Not Interested on Library grid movie cards and Movie Details; existing recommendation panels reuse the shared card; immediate state update followed by recommendation refresh; verbatim API reason strings as non-interactive chips; RTL proof, keyboard access, and accessible labels. Narrow feature-local hook/controls under `web/src/library/` mirroring the existing watchlist mutation approach. Nothing else ships.

## Domain vocabulary

| Term | Definition source | Notes |
|------|-------------------|-------|
| Feedback | root `CONTEXT.md` | Explicit Watched / Not Interested marks |
| Reason | root `CONTEXT.md` | Caller-safe explanation; wire form is API reason strings |
| Taste Profile | root `CONTEXT.md` | Recency-weighted aggregate of watched movies |
| MovieFeedbackActions | brief Q5 | Shared controls; not a glossary term |
| movieFeedbackStore | brief Q3, Q5 | Feature-local per-movie mutation state; watchlist-pattern analog |

## Technical Context

**Language/Version**: TypeScript / React 19 SPA under `web/`; no C# production changes in this ticket (`recon-DEV-355:41-48`).

**Primary Dependencies**: None added. Consume existing React, TanStack Query / watchlist-pattern mutation approach, openapi-fetch generated client, Vitest, and RTL already in the web stack (`recon-DEV-355:33,42`; brief Q1, Q5). Exact pins remain Phase B recon facts.

**Storage**: N/A. No database, schema, or migration change. Feedback marks and recommendation reasons already exist server-side (`recon-DEV-355:14-23,44`).

**Testing**: Vitest + RTL with existing request-interception conventions, mirroring tests cited in `recon-DEV-355:26,35-38`. No new packages. No generated MSW edits. No Gherkin/Reqnroll. Gate bar per brief Q6: `scripts/run-web-gates.ps1`, configured contract freshness checks, and Gauge applicability of C#-only gates from the actual diff — do not invent unsupported analyzer specifics.

**Target Platform**: Existing Vite/React SPA (`web/`). ui ticket (product UI).

**Project Type**: Existing web library and movie-details feature folders (`recon-DEV-355:29-38`; brief Q5).

**Performance Goals**: No numeric latency target in the brief. Immediate UI update before network completion is a functional requirement, not a latency SLA (brief Q3, Q6).

**Constraints**: No new NuGet/npm dependency. No new project, top-level folder, or architectural layer / state library. No backend route/status/DTO change. No schema/migration. No `Features:LocalPlay`, secrets, or `Process.Start`. No hand-written DTO duplicates or hand-edited generated files. No undo/toggle UI. Do not build the future DEV-418 panel. Thresholds stay in `harness.yml`. Follow established conventions and mandatory gate scripts per brief Q6 / `AGENTS.md`.

**Scale/Scope**: Exactly the brief Q5 approved file envelope (named existing edits plus proposed new leaves under `web/src/library/`). Frozen scope sentence (brief Q1): frontend consumption of existing DEV-352 feedback endpoints and DEV-354 reason strings — Mark Watched and Not Interested on Library grid movie cards and Movie Details; existing recommendation panels reuse the shared card; immediate state update followed by recommendation refresh; verbatim API Personal/Structural reasons; RTL proof, keyboard access and accessible labels; anything else is a follow-up issue, not a finding in this round.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Every gate evaluated in this Constitution Check cites its supporting numbered principle; separate scope and prerequisite checks follow below with their own authority citations.*

- Frontend stack: Vite + React + TypeScript strict; Vitest + RTL; consume generated OpenAPI types (Technology Stack Constraints, `.specify/memory/constitution.md`; brief Q2, Q5; `recon-DEV-355:18-19`).
- API and Contract Rules: OpenAPI committed at `web/src/api/openapi.json`; TypeScript client generated from that document, never hand-written; consume existing POST feedback routes and RecommendedMovieDto reason/flag fields (`.specify/memory/constitution.md` API and Contract Rules; charter contract chain; `recon-DEV-355:14-23`; brief Q2, Q4).
- Ubiquitous Language: **Feedback**, **Reason**, and **Taste Profile** already in root `CONTEXT.md`; no new glossary meaning; no ADR edit (Principle VIII; brief Q4/Q6).
- No new port, MediatR, generic repository, UnitOfWork, or state-management library (PRODUCT.md §2, §5; constitution I/II; brief Q1, Q5). Scope stays inside existing `web/src/library/` and `web/src/features/movie-details/` (plus conditional `MovieCarousel.tsx` only if shared card props require it).
- No secret, `Features:LocalPlay`, or `Process.Start` (constitution VII; care list item 5; `recon-DEV-355:46-47`). Feedback UI scope does not reference play execution.
- Test Pyramid / web tests: Vitest + RTL only; no second assertion/mocking stack; generated contract types consumed, not duplicated (constitution IX; brief Q6).
- Comments: no explanatory comments beyond AAA / empty-block / scoped suppressions.
- Gates from configured scripts using harness thresholds only; never lower thresholds or invent a threshold value (brief Q6).

Post-design re-check: the numbered-principle Constitution Check gates above still hold. This plan adds no package, project, new port name, CONTEXT.md edit, ADR, or file outside the approved brief file envelope. Scope and Phase B bind constraints remain in force under **Scope and prerequisite checks** below; they are not waived by this re-check and are not Constitution Check gates. Side design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `design.md`) are not produced by Quill — the brief already freezes contracts, files, ordering, and proofs; Palette owns `design.md` before plan challenge; concrete cache keys/DTO availability/consumer propagation are Wisp bind facts, not Quill inventions (brief Q3, Q5).

## Scope and prerequisite checks

Binding product, care-list, and Phase B prerequisite constraints. **Numbered principle: none applicable** for each item; do not invent I/II/IX citations. Constraints remain in force.

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-355 Scope & Technical Design and Acceptance Criteria (`recon-DEV-355:85-96`; brief Q1). Nothing is added or dropped. No owner checkbox (brief Q1–Q6).
- No schema or migration (care list item 3; `recon-DEV-355:44`; brief Q1).
- Public API shape is consumed, not changed: existing POST `/movies/{id}/watched` and POST `/movies/{id}/not-interested` (NoContent); RecommendedMovieDto already exposes `IsWatched`, `IsNotInterested`, and `Reasons` (`recon-DEV-355:14,22-23,45`; brief Q2, Q4). DELETE endpoints exist but are not undo product surface (brief Q2).
- Phase B prerequisites: merged Gate 1; Build pickup drift check; Wisp first binds DTO fields across Library/Details/recommendation representations, existing cache keys, and consumer propagation in the approved surface; stop for recon and Patron amendment if required fields are absent, unexpected consumers appear, or a file need falls outside the envelope (brief Q5/Q6). This plan does not invent those bind results.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-355/
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
web/src/library/movieFeedback.ts                 # new — typed POST helpers (generated types)
web/src/library/movieFeedbackStore.ts            # new — shared per-movie mutation/pending state
web/src/library/MovieFeedbackActions.tsx         # new — shared controls
web/src/library/MovieFeedbackActions.css         # new only if Palette requires scoped styling
web/src/library/movieFeedback.test.ts            # new — endpoint behavior proof
web/src/library/movieFeedbackStore.test.tsx      # new — mutation/concurrency/lifecycle proof
web/src/library/MovieFeedbackActions.test.tsx    # new — shared interaction/accessibility proof

web/src/library/MovieCard.tsx                    # additive — host shared actions / reasons as needed
web/src/library/MovieCard.test.tsx               # additive RTL
web/src/library/MovieCard.layout.test.tsx        # additive as needed
web/src/library/MovieGrid.test.tsx               # additive as needed
web/src/library/libraryQueries.ts                # additive invalidate/refetch wiring after Wisp bind

web/src/features/movie-details/MovieDetailsView.tsx
web/src/features/movie-details/MovieDetailsView.test.tsx
web/src/features/movie-details/movieDetailsApi.ts
web/src/features/movie-details/movieDetailsQueries.ts
web/src/features/movie-details/relatedMoviesApi.ts
web/src/features/movie-details/RelatedMovies.tsx
web/src/features/movie-details/RelatedMovies.test.tsx

web/src/ui/MovieCarousel.tsx                     # only if shared card action/reason props require it

# Pattern reference only (do not rewrite):
web/src/library/watchlist.tsx
web/src/library/watchlistStore.ts
web/src/library/watchlist.test.tsx
web/src/library/watchlist.integration.test.tsx
```

**Structure Decision**: Match the brief Q5 approved file envelope and existing Library / Movie Details / RelatedMovies surfaces (`recon-DEV-355:29-38`). Watchlist files are the mutation-pattern precedent (`recon-DEV-355:33`), not rewrite targets. No new project or top-level folder.

## Design (from settled brief)

1. **Delivery boundary (Q1)**: Frontend-only consumption of DEV-352 endpoints and DEV-354 reason strings. No undo/toggle UI, no DEV-418 panel, no dependency/layer/schema/API/LocalPlay/secrets changes. Closing bar Critical/High/Medium block; Low only if it violates frozen acceptance or an applicable gate; two review rounds, two fix commits per round.
2. **State/action contract (Q2)**: Mark-only POSTs; DELETE unused as product surface; independent IsWatched / IsNotInterested; already-marked disabled non-dispatch; shared per-movie exclusion is store-owned and blocks both actions while either request is pending and through update/reconciliation after a settled POST, including across initiating-control unmount/remount; initialize from actual contract fields after Wisp bind — never fabricate defaults.
3. **Optimistic lifecycle (Q3)**: Reuse watchlist mutation approach in a narrow feature-local hook; stale in-flight protection; retain pre-operation state; keep membership/reasons until refresh; invalidate/refetch movie/library and global-profile-dependent recommendation queries after success; POST failure rolls back only this operation with accessible mutation retry and per-movie-scoped authoritative reconciliation that uses only T001-bound keys and never erases other movies' concurrent updates; refresh failure keeps confirmed mark with separate refresh-only error/retry that never issues a POST; per-movie exclusion covers update/reconciliation, not only in-flight POST; pending/reconciliation and mutation error/retry state survives initiating-control unmount so remounts render pending/blocked and can observe error/retry with deferred settlement/reconciliation once; explicit mutation retry re-issues that POST once under the same per-movie exclusion (at most one in-flight POST; no POST while reconciliation is active); refresh-only retry stays separate. Concrete cache keys are Wisp-bound, not invented here.
4. **Reasons and accessibility (Q4/Q6)**: Optional reasons input on shared card; verbatim API-order chips; no categories/templates/fabrication; keyboard-native buttons; titles in accessible names; pending announcement; mutation error alert + explicit mutation retry under the same per-movie exclusion; separate user-facing refresh-only error/retry that retains the confirmed mark and retries refresh without another POST; navigation isolation; sensible focus on card removal. Labels from ASSUMPTIONS.md. Palette owns placement/styling.
5. **File envelope and ordering (Q5)**: Approved existing and new leaves only. Ordering: Wisp bind → typed endpoints + shared mutation/state with RTL → shared controls → Library/Details/current recommendation integration and reason rendering → failure/concurrency/a11y coverage → web gates and contract freshness.
6. **Proof and gates (Q6)**: Brief Q6 RTL matrix; `web-implement` red-green TDD in Build; `scripts/run-web-gates.ps1` and contract freshness; Gauge classifies C#-only gates from the actual diff; scope-empty `SKIPPED (scope-empty)` never PASS.

## Complexity Tracking

> No constitution violations requiring justification. Feature-local feedback leaves stay inside existing `web/src/library/`; contract consumption follows the mandatory OpenAPI → generated TS chain; watchlist-pattern reuse is not a new architectural layer.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phase boundaries

- **Phase A (this ask)**: Spec Kit docs under `specs/DEV-355` only (`spec.md`, `plan.md`, `tasks.md`). No freeze, publish, gates, or implementation. Palette `design.md` must exist before plan challenge. Keel analysis and plan challenge are subsequent distinct steps.
- **Phase B**: After Gate 1 merge and Build pickup drift check, Wisp binds DTO/cache/consumer facts in the approved surface (stop on gaps), then Anvil implements per `tasks.md` via `web-implement`; Cog refactor; Gauge gates.
