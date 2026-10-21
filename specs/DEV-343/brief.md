# DEV-343 — RelatedMovies grill brief

Status: grill closed by Patron Q7; seven questions, seven recorded rulings, zero open questions and zero owner escalations. This is Phase A input to Quill, not plan freeze, Gate 1 or Phase B authorization.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-343-spec`. Branch: `feature/343-spec`. Base: `8edc1b8f4832006bc812f8ebd2da3caf0e9c3ea9`. Size M, ui ticket, parent DEV-288. Facts come exclusively from `recon-DEV-343`; authoritative ticket text is at lines 99–120, constitution excerpts at 122–153 and canonical vocabulary at 154–177. PRODUCT.md governs product scope and sequencing. Full questions, answers, rationale, implications and citations are append-only in `CONCLUSIONS.md`; taste choices are in `ASSUMPTIONS.md`.

## Goal and authoritative scope

Display RelatedMovies on Movie Details with clear API Reason chips. Deliver an exported reusable horizontal, keyboard-accessible MovieCarousel for DEV-418 reuse, card navigation to movie details, an MSW recommendation endpoint mock and Vitest/RTL empty/loading/populated tests. Empty success shows a message without a carousel; loading shows a placeholder. These are already approved by the ticket (`recon-DEV-343:106–120`).

## Every grill answer and its implications

### Q1 — Dependencies, generated contract and vocabulary

Patron: settled as recommended. Plan now; Phase B waits for merged DEV-323 and DEV-341 and verifies the landed C# DTO → OpenAPI → generated TypeScript/MSW contract. DEV-343 does not implement the backend endpoint, hand-write DTOs or invent an absent contract. At the base, the route, generated types and handler are absent (`recon-DEV-343:29,50–58,87`). Preserve PRODUCT Epic 7 sequencing. Recommendation means a suggested library movie from the details page; Reason means a caller-safe explanation attached to a recommendation (`recon-DEV-343:160–161`). No glossary amendment.

### Q2 — Component seam, files and deliberate care-list ruling

Patron: approved as recommended. Export presentational `web/src/ui/MovieCarousel.tsx`, with CSS and focused tests, accepting an accessible label and rendered card children. It owns horizontal layout and scrolling only. It has no fetching, endpoint DTO dependency or recommendation logic. RelatedMovies and its query live in `web/src/features/movie-details/`. Callers compose cards, including future non-recommendation use; DEV-418 data requirements are not invented here.

Add RelatedMovies to the existing MovieDetailsView. Existing React, TanStack Query, openapi-fetch, MSW, Vitest and RTL suffice. No package, project, architectural layer, database change, backend route/status/DTO change, playback execution, secrets, Process.Start, unrelated deletion or rewrite. Necessary additive host/mock/test edits are forced by ticket acceptance. Patron deliberately approved the architecture and file-scope boundary under §2.3, citing `recon-DEV-343:19,87,54–60,84–89,106–120` and PRODUCT. Preserve all existing details functionality.

### Q3 — Keyboard accessibility and scroll mechanics

Patron: approved as recommended; specific micro-interactions are `[assumed]` taste choices. Use a labelled region, semantic list and focusable scroll viewport. ArrowLeft/ArrowRight scroll only when the viewport itself has focus. Native card links stay in ordinary Tab order and retain Enter and modified activation. Provide Previous/Next buttons, disabled at their respective boundaries and both disabled when content fits. No wraparound, autoplay, roving focus or descendant-key interception. Scrolling does not move movie focus. Support visible focus, native touch/overflow and reduced motion. Palette owns spacing, layout and responsive visuals without changing this interaction contract. Basis: ticket keyboard/reuse requirement, `recon-DEV-343:41–45` and existing anchor precedents.

### Q4 — Query ownership, states and copy

Patron: approved as recommended. Add `relatedMoviesApi.ts` using the existing typed openapi-fetch client and TanStack Query, keyed by movieId and enabled only for a valid loaded Movie Details view. Consume the landed DEV-341 route with default parameters; do not invent limits/options/statuses. Isolate movie identities so previous recommendations never appear for a newly selected movie. Preserve API order.

Initial absent-data request: accessible loading placeholder. Successful empty array: message, no carousel. Populated: ordered cards. Error: local notice and Retry through query refetch, never a false empty result or whole-page failure. A recommendations 404 does not prove the host movie is missing. Same-movie data may remain during background refetch; a failed refetch gets a local failure notice. `[assumed]` copy: heading **Related movies**; loading **Loading related movies…**; empty **No related movies yet.**; error **Could not load related movies.**; action **Retry**. Basis: `recon-DEV-343:62–65,71–78,106,113,118–120,128,141–144`.

### Q5 — Card content, Reasons, navigation and placement

Patron: approved as recommended. Show Title, PosterUrl with neutral missing/broken-image fallback, optional ReleaseYear and every API Reasons string verbatim as inert text chips under the poster. Preserve returned order; no truncation, sorting, synthetic explanation, HTML interpretation or chip filter action. Empty Reasons means no chips. Do not expose Score or add playback/feedback controls. Use decorative poster alt when the title labels the same link.

Each card has a single descriptive native `/movies/{id}` anchor. Ordinary primary activation uses existing SPA navigation and validated libraryReturnUrl handling; modified clicks retain browser behavior. `[assumed]` placement is after MoreWithPanel and before FileSection, retaining both. Full Reasons may wrap; Palette accommodates this. Basis: `recon-DEV-343:20,26–29,35–39,109–110,118,160–161`.

### Q6 — Plan, files, tests and gate expectations

Patron: approved as recommended. Follow the file set, task ordering, verification matrix and gate reporting below. Palette design precedes plan challenge. Phase A runs no build/test/analyzer gates. No generated file is edited by hand; no gate threshold is lowered. Conditional property-test opt-out is recorded below. Basis: ticket `recon-DEV-343:106–120`, constitution `128–129,141–144`, existing mock/test surfaces `51–67`, PRODUCT and task-pipeline Phase 3 step 5.

### Q7 — Loop discipline and shared understanding

Patron: approved; shared understanding confirmed, grill closed. Critical/High block; all Medium/Low require explicit evidence-backed disposition. Accepted corrections stay open until verified. Plan challenge and remediation each cap at two rounds. No open question or owner checkbox remains. No ADR or root glossary edit is justified for this M UI slice, which preserves existing vocabulary and adds no hard-to-reverse architecture. Canonical glossary and existing architectural decisions are ready for Quill to consume. This confirmation closes only the grill.

## Planned implementation file set

- New `web/src/ui/MovieCarousel.tsx`, `MovieCarousel.css`, `MovieCarousel.test.tsx`.
- New `web/src/features/movie-details/RelatedMovies.tsx`, `RelatedMovies.css`, `RelatedMovies.test.tsx`, `relatedMoviesApi.ts`.
- Additive `web/src/features/movie-details/MovieDetailsView.tsx` and `MovieDetailsView.test.tsx` integration/regression coverage.
- Additive typed `web/src/mocks/fixtures.ts`, handler success/status smoke tests and `web/src/api/client.typecheck.ts` only as needed to prove ticket and contract requirements; reuse existing/upstream smoke coverage rather than duplicating it.
- Consume landed generated `web/src/mocks/handlers.ts`. If upstream drift requires regeneration, use C# export → `web/src/api/openapi.json` → `npm run generate:api` → generated types/handlers. Never manually edit generated outputs or duplicate a handler. Backend API implementation belongs to DEV-341.
- Planning artifacts stay under `specs/DEV-343/`; Quill owns spec/plan/tasks, Palette owns design. No production writes are authorized by this brief in Phase A.

## Task ordering

1. Phase B: Wisp verifies merged prerequisites, landed route/DTO/status/default contract and generated outputs, and supplies drift recon; Gauge supplies applicable baseline; Keel supplies pickup/drift clearance. Stop on missing evidence or contract mismatch rather than inventing a bridge.
2. Anvil builds the reusable carousel with focused Vitest/RTL tests and reuse proof.
3. Anvil builds typed RelatedMovies query, cards, Reason chips and states with MSW tests.
4. Anvil integrates additively into MovieDetailsView and proves navigation and existing-section regressions.
5. After implementation, Gauge runs applicable contract/web/full pipeline gates. Follow the remaining build-workspace pipeline stages and approved design.

During Phase A, Conductor next assigns Quill specify/plan/tasks from this brief and recon; Keel then analyzes read-only against the brief. Palette design.md must exist before plan challenge. Plan challenge, freeze, provisional Gate 1 and owner-merged Gate 1 remain distinct later steps.

## Test strategy and evidence

- Vitest/RTL with existing test server, typed fixtures, generated handler and per-test MSW overrides. Smoke the recommendation route and generated field compatibility without creating a synthetic contract.
- Empty success message with no carousel; delayed loading placeholder; populated ordered cards; failure/refetch retry including recommendations 404; background-refetch failure and identity isolation including out-of-order movie requests.
- All Reason strings displayed as text and in order, empty Reasons without fabricated chips; null/missing and broken poster fallback; optional year; card details navigation and validated library return URL; native modified activation; existing details-section regressions.
- Carousel overflow, both boundaries, no-overflow state, Previous/Next buttons, viewport-only Arrow keys, normal link Tab/Enter behavior and a reuse mount with non-recommendation children. Stub layout narrowly for deterministic RTL tests.
- Built-UI browser verification of actual overflow, focus, keyboard behavior, responsive layout and reduced motion. Browser evidence complements layout stubs; no new end-to-end framework or dependency is required.
- Phase B `npm run check:api`, `scripts/run-web-gates.ps1`, and all applicable task-pipeline Phase 3 step 5 gates. Gauge records command, head and actual exit/verdict. No Phase A gate result is claimed.
- `propertyTests: opt-out — no domain invariants in this presentation/contract-only change` applies if no tagged property tests apply; record it in DEV-343 before accepting that exit. Never use the opt-out to bypass failing tests.
- Scope-empty exit 2 is non-blocking `SKIPPED (scope-empty)`, never PASS. Preserve other pipeline-defined opt-out/N/A distinctions. Exit 1 and Could not run remain blocking; no thresholds or required checks are waived.

## Loop discipline

Closing bar: zero unresolved Critical/High for plan clearance and code/ship review. Every Medium/Low receives an evidence-backed disposition; any acceptance-criterion or gate violation is corrected/reclassified, not waived. Accepted corrections require independent verification before closure.

Round caps: plan challenge at most two rounds; later remediation at most two rounds. After the cap, an unresolved blocker is reported blocked to Conductor. Do not silently approve or begin unbounded further rounds.

Frozen scope: RelatedMovies on Movie Details consuming landed DEV-341; exported presentational horizontal keyboard-accessible MovieCarousel reusable by DEV-418; ordered verbatim Reason chips, safe cards and details navigation; required states with local error isolation; generated-contract MSW fixtures/proofs and specified tests/accessibility/browser/gates, with additive existing-folder integration only; anything else is a follow-up issue, not a finding in this round.

Regressions in existing sections caused by this diff are in scope. Follow-up recording follows charter thresholds and existing-ticket reuse; unrelated low-value ideas are noted without forcing a ticket. The frozen scope does not authorize dropping ticket acceptance criteria.

## Handoff

Keel owns this brief and complete decision record. Quill drafts from these settled decisions and reports any gap as `needs decision:`; Quill must not fill gaps. Palette designs within these decisions. Conductor may initiate those assignments only after this completed grill handoff. Implementation waits for normal Gate 1 and the prerequisite/pickup checks above.
