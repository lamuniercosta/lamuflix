# Feature Specification: DEV-355 Watched and Not Interested actions with reason chips

**Feature Branch**: `feature/355-spec`

**Created**: 2026-10-22

**Status**: Draft

**Input**: Ticket DEV-355 (size M, parent DEV-288, ui tag present); settled grill brief `specs/DEV-355/brief.md` (grill closed Q1–Q6); facts from note `recon-DEV-355`; Patron rulings in `specs/DEV-355/CONCLUSIONS.md`; taste receipt `specs/DEV-355/ASSUMPTIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## Domain vocabulary *(mandatory — published from root CONTEXT.md)*

These terms are already present in root `CONTEXT.md`. No new glossary meaning, root CONTEXT change, or ADR is warranted for this M frontend consumption task (brief Q4/Q6). Published Spec Kit artifacts must use them as written:

- **Feedback**: Explicit Watched / Not Interested marks.
- **Reason**: A caller-safe explanation attached to a recommendation. _Avoid_: internal identifiers, scorer debug strings rewritten by this ticket.
- **Taste Profile**: A recency-weighted aggregate of the user's watched movies.

Personal and Structural describe API-provided reason strings in ticket language; the exposed contract is a flat string list, not a category discriminator (brief Q4; `recon-DEV-355:22-23`).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Mark Watched and Not Interested with immediate update and recommendation refresh (Priority: P1)

A user on the Library grid or Movie Details marks a movie Watched or Not Interested. The chosen mark updates immediately on already-loaded representations without a page reload, preserves the other independent flag, and after a successful POST the app refreshes recommendation queries that depend on the global feedback profile so the change is visible on the next recommendation fetch. Already-marked actions show a disabled marked state and do not dispatch again. Shared per-movie exclusion is store-owned: it blocks both actions for that movie across card, details, and recommendation instances while either request is in flight and through update/reconciliation after a settled POST (deferred success or failure reconciliation), survives initiating-control unmount/remount, and keeps remounted same-movie instances pending/blocked until settlement; other movies remain interactive.

**Why this priority**: Ticket Scope and acceptance require Mark Watched and Not Interested on Library cards and Movie Details, calling DEV-352 endpoints, with immediate UI update and recommendation refresh visible on next fetch (`recon-DEV-355:88-94`).

**Independent Test**: Vitest/RTL with existing tooling and request interception prove both POST actions on Library, Details, and current recommendation card; deferred network completion shows the selected flag before response without reload; correct movie/route; preserved other flag; already-marked non-dispatch; success refreshes global-profile-dependent recommendation queries including inactive-query staleness for next fetch (brief Q6; `recon-DEV-355:26,35-38`).

**Acceptance Scenarios**:

1. **Given** a Library grid movie card that is not marked Watched, **When** the user activates Mark Watched, **Then** the card shows Watched immediately without a page reload and POST `/movies/{id}/watched` is sent once (`recon-DEV-355:14,88-94`).
2. **Given** a Movie Details view that is not marked Not Interested, **When** the user activates Not Interested, **Then** the details surface shows Not Interested (marked) immediately without a page reload and POST `/movies/{id}/not-interested` is sent once (`recon-DEV-355:14,88-94`).
3. **Given** a successful POST for either action, **When** recommendation queries dependent on the global feedback profile settle, **Then** server results own final membership, flags, and reasons, and the change is visible on the next recommendation fetch without client scoring or guessed removal (brief Q3; `recon-DEV-355:89,94`).
4. **Given** a movie already marked Watched (or Not Interested), **When** the user views the corresponding action, **Then** it shows the disabled marked state and does not dispatch (`recon-DEV-355:17`; brief Q2).
5. **Given** IsWatched and IsNotInterested as independent contract fields, **When** one action succeeds, **Then** only the chosen flag changes and the other is preserved (`recon-DEV-355:22`; brief Q2).
6. **Given** either action pending for a movie, or a settled POST whose success or failure reconciliation is still deferred, **When** the user tries either action on any card/details/recommendation instance of that movie — including after the initiating control unmounts and a same-movie instance remounts — **Then** both actions stay blocked for that movie across those instances while other movies remain interactive (brief Q2, Q3).

---

### User Story 2 - Verbatim reason chips on the shared recommendation card (Priority: P2)

A user viewing current recommendation consumers that reuse the shared movie card sees every API reason string rendered verbatim and in API order as non-interactive informational chips. Empty or absent reasons produce no chips. Ordinary Library/Details data without reasons gets no fabricated reasons. Duplicate strings still render with distinct stable keys. HTML-looking literal text is escaped as React text and remains fully available to assistive technology.

**Why this priority**: Ticket Scope and acceptance require Personal and Structural reasons rendered as returned so wording stays in the API (`recon-DEV-355:90,95`).

**Independent Test**: Vitest/RTL prove Personal/Structural example strings exactly, in API order, including duplicates and HTML-looking literals; empty/absent input creates no chips; no synthesized strings or category badges (brief Q6; `recon-DEV-355:22-26`).

**Acceptance Scenarios**:

1. **Given** a recommendation item with Reasons including for example "Because you watched Heat", **When** the shared card renders, **Then** each returned string appears verbatim in API order as a non-interactive chip (`recon-DEV-355:22-26,95`).
2. **Given** empty or absent reasons, **When** the card renders, **Then** no chips appear and no fabricated explanation is shown (`recon-DEV-355:26`; brief Q4).
3. **Given** duplicate reason strings, **When** the card renders, **Then** each occurrence renders with a distinct stable React key (brief Q4).
4. **Given** a reason string that looks like HTML, **When** the card renders, **Then** it is escaped as React text, not interpreted as HTML, and full wording remains available to assistive technology (brief Q4).
5. **Given** ordinary Library or Details data without reasons, **When** those surfaces render, **Then** no reason chips are fabricated (brief Q4).

---

### User Story 3 - Accessible controls, failures, and concurrency safety (Priority: P3)

A user can reach both feedback actions by keyboard (Enter/Space on native buttons), with accessible names that include the movie title on repeated cards, visible focus, announced pending state, and an accessible mutation error with explicit retry observable from any same-movie instance after remount. Feedback activation must not accidentally trigger parent-card navigation. On POST failure, only this operation rolls back without erasing other movies' concurrent updates; authoritative reconciliation uses only T001-bound keys scoped to this movie; there is no automatic POST retry. Explicit mutation retry shares the same per-movie exclusion (at most one in-flight POST; no POST while reconciliation is active). A failed refresh after successful POST keeps the confirmed mark and exposes a separate refresh-only error/retry that never issues a POST. Stale in-flight responses cannot erase optimistic state. If refresh removes the focused recommendation card, a sensible panel focus target is preserved.

**Why this priority**: Ticket acceptance requires keyboard reachability and accessible labels (`recon-DEV-355:96`); brief Q2–Q3 settle pending, failure, and concurrency contracts that keep the mark-only POST surface safe.

**Independent Test**: Vitest/RTL prove at-most-once dispatch under duplicate, keyboard, and simultaneous same-movie instances; another movie can proceed; unmount mid-pending then remount still blocked with deferred settlement/reconciliation once and observable error/retry; failed POST restores only its own operation and reconciles via T001-bound keys without erasing other-movie concurrent updates; explicit mutation retry under per-movie exclusion (double-activate once; no POST during reconciliation); stale in-flight fetch cannot erase optimistic state; no automatic POST retry; refresh failure keeps confirmed marks with separate refresh-only retry; Enter/Space, accessible names with title, focus, pending, error/retry, navigation isolation, and sensible focus on card removal (brief Q6).

**Acceptance Scenarios**:

1. **Given** focusable Mark Watched / Not Interested controls, **When** the user activates with Enter or Space, **Then** the same mark behavior as pointer activation occurs with visible focus and accessible names that include the movie title on repeated cards (ASSUMPTIONS.md Q4; `recon-DEV-355:96`).
2. **Given** a pending feedback mutation, **When** assistive technology observes the UI, **Then** pending state is announced (brief Q4).
3. **Given** a failed POST, **When** the UI settles, **Then** only this operation rolls back, an accessible error with explicit mutation retry is shown (readable from any same-movie instance), server state is reconciled for this movie via T001-bound keys without erasing other movies' concurrent updates, and no automatic POST retry occurs (`recon-DEV-355:17`; brief Q3).
4. **Given** a successful POST and a subsequent failed recommendation refresh, **When** the UI settles, **Then** the confirmed mark remains and a separate refresh-only error/retry is available without rolling back the mark and without issuing another POST (brief Q3).
5. **Given** two instances of the same movie and a second movie, **When** the user activates feedback repeatedly or concurrently, or double-activates explicit mutation retry, **Then** the same movie dispatches at most one POST while pending or retrying, issues no POST while reconciliation is active, and the other movie can proceed (brief Q2, Q6).
6. **Given** a pending or reconciling feedback operation whose initiating control unmounts, **When** a same-movie instance remounts before settlement, **Then** the remount renders pending/blocked state from the store, observes mutation error/retry if the POST failed, and deferred settlement/reconciliation completes once (brief Q2, Q3).
7. **Given** a feedback control on a recommendation card, **When** the user activates it, **Then** parent-card navigation does not fire; if refresh removes the focused recommendation card, a sensible panel focus target remains (brief Q4).

### Edge Cases

- DELETE `/movies/{id}/watched` and DELETE `/movies/{id}/not-interested` exist in the contract but are not exposed as an undo product feature in this ticket (brief Q2; `recon-DEV-355:14`).
- Repeat transitions throw server-side; the UI must not double-fire (`recon-DEV-355:17`).
- Keep recommendation membership and existing reasons until authoritative refresh; do not client-score, rewrite reasons, or guess removal policy (brief Q3).
- Concrete existing cache keys, DTO field availability across Library/Details/recommendation representations, and consumer propagation are Phase B Wisp bind facts — not invented in this spec (brief Q3, Q5).
- Palette owns placement and visual styling within these contracts; `design.md` is required before plan challenge (brief Q1, Q4).
- Do not add undo/toggle UI, build the future DEV-418 panel, change dependencies, introduce a generic layer/state library, change backend/schema/API contracts, touch playback/LocalPlay/Process.Start/secrets, or rewrite unrelated files (brief Q1).
- Approved file envelope is exhaustive; stop for recon and Patron amendment if a necessary file falls outside it (brief Q5).
- Scope-empty exit 2 is non-blocking `SKIPPED (scope-empty)`, never PASS; Exit 1 and Could not run remain blocking; no gate threshold may be lowered (brief Q6).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Deliver Mark Watched and Not Interested on Library grid movie cards (`web/src/library/MovieCard.tsx`) and Movie Details (`web/src/features/movie-details/MovieDetailsView.tsx`), calling existing POST `/movies/{id}/watched` and POST `/movies/{id}/not-interested` (NoContent) via generated API types (`recon-DEV-355:14,18-19,88-89`; brief Q2, Q5).
- **FR-002**: Existing recommendation consumers integrate now by reusing the shared card; do not build the future DEV-418 panel; future consumers reuse the same card (`recon-DEV-355:88`; brief Q1). Current consumer surface named in recon includes `RelatedMovies.tsx` / `MovieCarousel.tsx` (`recon-DEV-355:31`).
- **FR-003**: Add typed endpoint functions in `web/src/library/movieFeedback.ts` using generated contract types; do not hand-edit OpenAPI, generated TypeScript, or generated MSW (`recon-DEV-355:18-19,32`; brief Q5).
- **FR-004**: Add shared per-movie mutation state/hook in `web/src/library/movieFeedbackStore.ts` reusing the existing watchlist mutation approach (`recon-DEV-355:33`) in a narrow feature-local implementation — not a new state-management library or architectural layer (brief Q3, Q5). Pending/reconciliation and mutation error/retry state is store-owned: it survives initiating-control unmount/remount; remounted same-movie instances render pending/blocked state and can observe error/retry.
- **FR-005**: Add shared controls in `web/src/library/MovieFeedbackActions.tsx` (and `MovieFeedbackActions.css` only if Palette requires it) with labels `Mark Watched` / `Watched` and `Not Interested` / `Not Interested (marked)` (ASSUMPTIONS.md Q4; brief Q4–Q5).
- **FR-006**: Initialize flags from actual contract fields after Wisp binds DTO availability across Library/Details/recommendation representations; absent required fields are a recon/contract gap, never fabricated false defaults (brief Q2, Q5).
- **FR-007**: Treat IsWatched and IsNotInterested independently; already-marked actions show disabled marked state and do not dispatch; share per-movie exclusion across instances; while either action is pending or update/reconciliation after a settled POST is still in progress, block both for that movie; explicit mutation retry shares that same exclusion (at most one in-flight POST; no POST while reconciliation is active); no automatic POST retries (`recon-DEV-355:17,22`; brief Q2, Q3).
- **FR-008**: Optimistic lifecycle: protect against stale in-flight responses; retain pre-operation state; update only the selected flag across already-loaded representations; keep recommendation membership and reasons until authoritative refresh; after successful POST invalidate/refetch affected movie/library data and all recommendation queries dependent on the global feedback profile; inactive queries become stale for their next fetch; server results own final membership, flags, and reasons (brief Q3). Concrete cache keys are Wisp-bound before implementation, not invented here (brief Q3).
- **FR-009**: On POST failure, roll back only this operation without erasing other movies' concurrent updates, expose an accessible error with explicit mutation retry, and reconcile server state for this movie using only T001-bound keys. A failed refresh after successful POST preserves the confirmed mark and exposes separate refresh-only error/retry that never issues a POST (brief Q3).
- **FR-010**: Pass generated recommendation reasons as optional presentation input to the shared card; render every returned string verbatim and in API order; empty/absent reasons produce no chips; chips are non-interactive informational text; escape as React text; no prefix classification, category badges, templates, client reason generation, or API additions (`recon-DEV-355:22-26,90,95`; brief Q4).
- **FR-011**: Native keyboard-operable buttons, visible focus, accessible names including movie title on repeated cards, announced pending state, mutation error alert with explicit retry readable from any same-movie instance, no accidental parent-card navigation, and a sensible preserved focus target when refresh removes the focused recommendation card (brief Q4; `recon-DEV-355:96`).
- **FR-012**: Approved existing-file edits only as necessary for acceptance: `MovieCard.tsx`, `MovieCard.test.tsx`, `MovieCard.layout.test.tsx`, `MovieGrid.test.tsx`, `libraryQueries.ts`; `MovieDetailsView.tsx`, `MovieDetailsView.test.tsx`, `movieDetailsApi.ts`, `movieDetailsQueries.ts`, `relatedMoviesApi.ts`, `RelatedMovies.tsx`, `RelatedMovies.test.tsx`; `MovieCarousel.tsx` only if shared card action/reason props require it. Approved new leaves under `web/src/library/`: `movieFeedback.ts`, `movieFeedbackStore.ts`, `MovieFeedbackActions.tsx`, optional `MovieFeedbackActions.css`, `movieFeedback.test.ts`, `movieFeedbackStore.test.tsx`, `MovieFeedbackActions.test.tsx` (brief Q5).
- **FR-013**: Vitest/RTL proofs per brief Q6 matrix (both actions on Library/Details/current recommendation card; optimistic pre-response update; already-marked non-dispatch; at-most-once and multi-instance pending including unmount/remount survival with deferred settlement/reconciliation once; per-movie rollback with T001-bound failure reconciliation preserving other-movie ops; mutation retry under per-movie exclusion; stale-fetch protection; no automatic POST retry; recommendation refresh including removal and inactive-query staleness; refresh-failure retention with refresh-only retry; verbatim reasons; keyboard/a11y). Mirror existing tests cited in `recon-DEV-355:26,35-38`. No new packages or generated MSW edits (brief Q6).
- **FR-014**: Future Build runs `scripts/run-web-gates.ps1` and configured contract freshness checks; Gauge determines applicability of C#-only gates from the actual diff. Scope-empty exit 2 is non-blocking `SKIPPED (scope-empty)`, never PASS. Exit 1 and Could not run remain blocking; no thresholds lowered (brief Q6).
- **FR-015**: Loop discipline: Critical, High, and Medium block; Low blocks only when it violates frozen acceptance or an applicable gate. Maximum two review rounds and two fix commits per round. Frozen scope is the ticket acceptance, the six grill rulings, and necessary changes within the approved file envelope; anything else is a follow-up issue, not a finding in this round (brief Q1).

### Key Entities

- **Feedback**: Explicit Watched / Not Interested marks consumed and mutated via existing DEV-352 endpoints.
- **Reason**: Caller-safe explanation exposed as API reason strings on recommendation representations.
- **MovieFeedbackActions**: Shared presentational/control surface for mark actions and marked state.
- **movieFeedbackStore**: Feature-local shared per-movie pending/optimistic mutation state (watchlist-pattern analog).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On Library and Movie Details, Mark Watched and Not Interested update the chosen mark without a page reload.
- **SC-002**: After a successful mark, the next recommendation fetch reflects server-owned membership, flags, and reasons.
- **SC-003**: Recommendation reason chips show API strings exactly as returned (for example "Because you watched Heat"), with no fabricated Library/Details reasons.
- **SC-004**: Both actions are reachable by keyboard and expose accessible names; repeated cards include the movie title in those names.
- **SC-005**: Already-marked actions do not dispatch again; pending state serializes both actions for one movie without blocking other movies.
- **SC-006**: POST failure rolls back only that operation with mutation retry and per-movie T001-bound reconciliation that preserves other movies' concurrent updates; refresh failure after success keeps the confirmed mark with separate refresh-only retry.
- **SC-007**: Phase B web/contract gates run with honest exit/verdict recording; SKIPPED/SKIP/N/A never recorded as PASS.

## Assumptions

- Label wording in `specs/DEV-355/ASSUMPTIONS.md` (Q4) is a Patron taste ruling tagged `[assumed]`; full basis is in `CONCLUSIONS.md`.
- Palette `design.md` must exist before plan challenge; Palette owns placement and visual styling without changing the interaction and accessibility contracts (brief Q1, Q4).
- Concrete cache keys, cross-representation DTO field availability, and consumer propagation remain Wisp bind obligations before Build implementation (brief Q3, Q5). Specifying in Phase A does not invent those facts.
- No owner structural checkbox was identified (brief Q1–Q6). Existing vocabulary is reused; no ADR or root glossary edit.
- Exclusions in brief Q1 frozen scope remain out of scope for this feature.
- DELETE feedback endpoints remain unused product surface for this ticket (brief Q2).
- Phase A drafting runs no tests, build, analyzer, formatter, or gate commands (brief Q6).
