# DEV-355 — Decided Phase A brief

## Identity, authority and status

- Task: Implement Watched and Not Interested actions in UI with dynamic reason chips; parent DEV-288; size:M; ui:.
- Worktree: `F:/Dev/LamuFlix.worktrees/feature-355-spec`.
- Branch: `feature/355-spec`.
- Phase A base: `8f9720958f99d62397b306615daf8d49a76d3032`.
- Authority: complete ticket retrieved read-only by `scripts/get-task.ps1 DEV-355`, exit 0, recorded in `recon-DEV-355:75-99`; authoritative Scope & Technical Design at lines 85-91 and acceptance criteria at 93-96. This later retrieval supersedes the earlier missing-ticket status.
- Governing sources: `specs/PRODUCT.md` §§2-5; `task-pipeline` Phase 2 and budgets; `lamuflix-team-charter` §2.3 and Spec workspace boundary.
- Patron ACCEPTed Q1-Q6 and explicitly confirmed shared understanding and grill closure at Q6. Full questions and answers are append-only in `CONCLUSIONS.md`; every answer is reproduced below.
- No remaining Patron decision or structural blocker was identified. This is grill closure only: no plan freeze, analysis clearance, Gate 1, implementation or runtime proof is claimed.

## Frozen scope and loop discipline — Q1

Deliver frontend consumption of existing DEV-352 feedback endpoints and DEV-354 reason strings: Mark Watched and Not Interested on Library grid movie cards and Movie Details; existing recommendation panels reuse the shared card; immediate state update followed by recommendation refresh; verbatim API Personal/Structural reasons; RTL proof, keyboard access and accessible labels.

Do not add undo/toggle UI, build the future DEV-418 panel, change dependencies, introduce a generic layer/state library, change backend/schema/API contracts, touch playback/LocalPlay/Process.Start/secrets, or rewrite unrelated files. Existing recommendation consumers integrate now; future consumers reuse the same card.

Closing bar for code review/ship review: Critical, High and Medium block. Low blocks only when it violates frozen acceptance or an applicable gate. Maximum two review rounds and two fix commits per round; grill cap 12 (closed at six). Required M-axis reports remain required by the pipeline. At the cap, unresolved blockers remain blockers for the prescribed owner/PR path; no invented extra round or silent waiver.

Frozen scope is the ticket acceptance, these six rulings, and necessary changes within the approved file envelope; anything else is a follow-up issue, not a finding in this round.

## State and endpoint contract — Q2

Use generated API types and existing POST `/movies/{id}/watched` and POST `/movies/{id}/not-interested` endpoints (NoContent). DELETE endpoints are not exposed as an undo product feature. Initialize flags from actual contract fields; absent required fields are a recon/contract gap, never fabricated false defaults.

Already-marked actions show disabled marked state and do not dispatch. Treat IsWatched and IsNotInterested independently: change only the chosen flag and preserve the other. Share pending state by movie across card/details/recommendation instances; while either action is pending, block both actions for that movie across those instances. Other movies remain interactive. No automatic POST retries because repeat transitions throw (`recon-DEV-355:14-19`).

## Optimistic lifecycle — Q3

Reuse the existing watchlist mutation approach (`recon-DEV-355:33`) in a narrow feature-local hook/state implementation. Protect against stale in-flight responses before optimistic changes. Retain pre-operation state and update only the selected flag consistently across already-loaded representations.

Keep recommendation membership and existing reasons until the authoritative refresh. Do not client-score, rewrite reasons, or guess removal policy. After successful POST, invalidate/refetch affected movie/library data and all recommendation queries dependent on the global feedback profile; inactive queries become stale for their next fetch. Server results own final membership, flags and reasons.

On POST failure, roll back only this operation without erasing other movies' concurrent updates, expose an accessible error with explicit retry, and reconcile server state. A failed refresh after successful POST preserves the confirmed mark and exposes separate refresh error/retry; it must not turn a server success into a rollback. Per-movie exclusion covers update/reconciliation.

Concrete existing cache keys, DTO availability and consumer propagation are facts for Wisp to bind before implementation, not assumptions or gaps delegated to Quill to invent.

## Reasons, vocabulary and interactions — Q4/Q6

Existing terms Watched, Not Interested and reason chips are reused; no new glossary meaning, root CONTEXT change, or ADR is warranted for this M frontend consumption task. Personal and Structural describe API-provided reasons; the exposed contract is a flat string list, not a category discriminator.

Pass generated recommendation reasons as optional presentation input to the shared card. Render every returned string verbatim and in API order, including duplicates with distinct stable React keys. Empty or absent reasons produce no chips. Ordinary Library/Details data without reasons gets no fabricated reasons. Chips are non-interactive informational text. Escape as React text; never interpret HTML or hide full wording from assistive technology. No prefix classification, category badges, templates, client reason generation or API additions.

Taste wording (also recorded `[assumed]` in `ASSUMPTIONS.md`): `Mark Watched` / `Watched`; `Not Interested` / `Not Interested (marked)`. Repeated-card accessible names include the movie title. Use native keyboard-operable buttons, visible focus, announced pending state, mutation error alert and explicit retry. Feedback activation must not accidentally trigger parent-card navigation. Preserve a sensible panel focus target when refresh removes the focused recommendation card.

Palette owns placement and visual styling within these contracts. Its `design.md` is required before plan challenge; this brief does not replace design.

## Approved file envelope — Q5

Existing files may change only as necessary for these acceptance criteria:

| Directory | Files |
| --- | --- |
| `web/src/library/` | `MovieCard.tsx`, `MovieCard.test.tsx`, `MovieCard.layout.test.tsx`, `MovieGrid.test.tsx`, `libraryQueries.ts` |
| `web/src/features/movie-details/` | `MovieDetailsView.tsx`, `MovieDetailsView.test.tsx`, `movieDetailsApi.ts`, `movieDetailsQueries.ts`, `relatedMoviesApi.ts`, `RelatedMovies.tsx`, `RelatedMovies.test.tsx` |
| `web/src/ui/` | `MovieCarousel.tsx` only if shared card action/reason props require it |

Approved proposed new leaf files under existing `web/src/library/` (names are design decisions, not claims of pre-existing files):

| File | Responsibility |
| --- | --- |
| `movieFeedback.ts` | Typed endpoint functions using generated contract types |
| `movieFeedbackStore.ts` | Shared per-movie mutation state/hook using existing watchlist approach |
| `MovieFeedbackActions.tsx` | Shared controls |
| `MovieFeedbackActions.css` | Scoped styling only if Palette requires it |
| `movieFeedback.test.ts` | Endpoint behavior proof |
| `movieFeedbackStore.test.tsx` | Mutation/concurrency/lifecycle proof |
| `MovieFeedbackActions.test.tsx` | Shared interaction/accessibility proof |

No watchlist rewrite; use it as the existing pattern. Do not hand-edit generated MSW, OpenAPI or TypeScript outputs; do not change package/config/backend files. The existing DTO → OpenAPI → generated TypeScript chain remains authoritative.

Patron explicitly approved this file scope under the care-list rule based on ticket necessity and `recon-DEV-355:29-38`. If concrete recon reveals absent required fields, unexpected consumers, or a necessary file outside this envelope, stop for recon and a Patron amendment. Do not silently expand the envelope or change ticket delivery.

## Task ordering and prerequisites — Q5/Q6

1. Quill later drafts spec/plan/tasks from this brief and recon; no independent decisions. A decision gap is `needs decision:`. No Spec Kit drafting is part of the present ask.
2. Palette supplies design before plan challenge. Analysis, challenge, provisional Gate 1, spec PR, user merge and Build handoff remain distinct subsequent steps under the pipeline.
3. Before Build implementation, merged Gate 1 and pickup drift check are required. Wisp first binds DTO fields across Library/Details/recommendation representations, existing cache keys and consumer propagation in the approved surface. Report absent required data/unexpected consumers/outside-envelope files rather than inventing contracts.
4. Future Build: typed endpoint functions and shared mutation/state with RTL proof, then shared controls, then Library/Details/current recommendation integration and reason rendering.
5. Complete failure, concurrency and accessibility coverage; run applicable web gates and contract freshness checks. Build follows `web-implement` red-green TDD; refactor/review/gates stay at their assigned pipeline stages.

## Test strategy and gate expectations — Q6

Use existing Vitest/RTL tooling and request interception, mirroring tests cited in `recon-DEV-355:26,35-38`. No new packages or generated MSW edits.

- Exercise both POST actions on Library, Details and current recommendation card. Defer network completion to prove the selected flag changes before response, without a page reload. Verify the correct movie/route, preserved other flag and already-marked non-dispatch.
- Prove at-most-once dispatch under duplicate activation, keyboard activation and simultaneous instances of the same movie; another movie can proceed.
- Prove failed POST restores only its own operation while another movie succeeds; stale in-flight fetch cannot erase the optimistic state; no automatic POST retry occurs.
- Prove success refreshes all global-profile-dependent recommendation queries, inactive queries are stale for next fetch, and authoritative results change state/reasons/membership including removal. Refresh failure keeps confirmed marks and has independent refresh retry.
- Render Personal/Structural example strings exactly, in API order, including duplicate and HTML-looking literal text; empty/absent input creates no chips; no synthesized strings or categories.
- Exercise Enter/Space, accessible names including title, visible focus, pending announcement, error/retry behavior, navigation isolation and sensible focus on card removal.

Future Build runs `scripts/run-web-gates.ps1` and the configured contract freshness checks. Gauge determines applicability of C#-only gates from the actual diff. Scope-empty is `SKIPPED (scope-empty)`, never PASS; no gate threshold may be lowered. A gate that could not run has not passed.

This Phase A grill runs no tests, build, analyzer, formatter or gate commands. No execution results are claimed.

## Complete Patron answers

### Q1 — Delivery boundary and loop discipline

DEV-355 Q1 ACCEPT: delivery boundary and loop discipline as proposed. Basis: recon-DEV-355 lines 85-96 authoritative ticket scope already decided (Library cards and Details actions, same card reused by recommendation panels, optimistic update then recommendation refresh, verbatim API Personal/Structural reason strings, RTL tests, keyboard and accessible labels); recon lines 41-48 section 2.3 check shows no new dependency, project, schema, API shape, LocalPlay, Process.Start, or secrets change, so frontend-only consumption of existing DEV-352 and DEV-354 contracts needs no escalation; task-pipeline lines 27-29 fix grill cap 12, review cap 2 rounds, 2 fix commits per round. Closing bar Critical/High/Medium block, Low only if it violates frozen acceptance or an applicable gate, frozen scope ends with follow-ups elsewhere, and Palette design required before plan challenge all accepted. No ticket contradiction and no constitution departure, so nothing structural.

### Q2 — State/action contract

DEV-355 Q2 ACCEPT: state/action contract as proposed. Basis: ticket lines 88-96 name only Mark Watched and Not Interested calling DEV-352 endpoints with immediate update and keyboard/accessible labels, no undo or toggle named, so mark-only POST commands stay within ticket and DELETE endpoints remain unused product surface; recon lines 14-19 show POST routes exist and Movie transitions throw on repeat, so an already-marked disabled state that does not dispatch again prevents double-fire; recon lines 22 and 32 show IsWatched and IsNotInterested as separate generated contract fields, so preserve the other flag and never infer mutual exclusion; shared per-movie pending blocking both actions while either request is pending prevents duplicate and conflicting optimistic writes while other movies stay interactive. Generated fields initialize state with no invented defaults, and absent required data is a recon/contract gap. Accepted cost: no undo affordance in this ticket and per-movie serialization until settle. No ticket contradiction and no constitution departure, so nothing structural.

### Q3 — Optimistic lifecycle

DEV-355 Q3 ACCEPT: optimistic lifecycle as proposed. Basis: ticket lines 89 and 94 require immediate UI update then recommendation refresh visible on next fetch, which the optimistic-then-invalidate flow implements without a page reload; recon line 33 identifies the existing watchlist mutation pattern to reuse and line 32 confirms no web mutation to the feedback endpoints exists yet, so a narrowly scoped shared per-movie feedback hook with generated API types follows the contract chain and Q2 shared pending state; recon line 17 shows repeat transitions throw, so no automatic POST retries is correct; recon lines 22 and 90 show reasons are API strings only, so keeping membership and reasons until server refresh with no client scoring, string rewriting, or guessed Not Interested removal stays within ticket. Pre-operation retention with rollback of only this operation on POST failure, accessible retryable error plus reconciliation, and keeping a confirmed mark on refresh failure while exposing refresh error and retry correctly separate mutation failure from refresh failure. Per-movie exclusion across update and reconciliation matches Q2. Accepted cost: cache coordination and separate mutation-versus-refresh error handling, with concrete cache keys and APIs left to implementation recon and not invented here. No ticket contradiction and no constitution departure, so nothing structural.

### Q4 — Reasons and accessibility

DEV-355 Q4 ACCEPT: reason and accessible interaction contract as proposed, with the label wording accepted as a taste ruling to log tagged assumed in ASSUMPTIONS.md. Basis: ticket lines 90 and 95 require Personal and Structural reasons rendered as returned with wording staying in the API, and recon lines 22-23 expose only a flat reasons string list mapped from reason text, so verbatim API-order rendering with one non-interactive chip per string, no inferred Personal or Structural categorization, no invented reasons where data lacks them, and empty list meaning no chips is the contract-faithful reading; recon lines 25-26 and 53 confirm existing verbatim chip rendering and no invented text. Exact text preserved including duplicates with stable distinct keys, escaped as React text with no HTML interpretation or truncation hiding full text from assistive tech, and no client templates, rewriting, prefix classification, or new API fields. Ticket line 96 requires keyboard reachability and accessible labels, supporting native keyboard-operable buttons, visible focus, no accidental parent-card navigation, movie title in accessible names on repeated cards, pending announcement, mutation error alert with explicit retry, non-focusable informational chips, and a sensible preserved focus target if refresh removes the focused recommendation card. Palette decides placement and visual styling later within these constraints. Accepted cost: no category badges or synthesized Library reasons. No ticket contradiction and no constitution departure, so nothing structural.

### Q5 — File envelope and ordering

DEV-355 Q5 ACCEPT: bounded implementation file envelope and ordering approved as an explicit care-list ruling. Basis: recon lines 29-38 name the existing Library card, Details, recommendation, query, and RTL test surfaces listed, and ticket lines 88 and 91 require actions on those surfaces plus RTL tests, so edits to the named existing web files only as needed for these ACs are ticket-scoped; proposed new leaf files under the existing web/src/library folder are feature-local leaves, not a new project, top-level folder, architectural layer, or library, so no structural escalation arises. Typed endpoint functions, shared per-movie mutation state and hook, and shared controls implement the Q2 and Q3 contracts without rewriting watchlist code, generated types, OpenAPI, MSW, package, config, or backend files, and without building the future DEV-418 panel. Ordering accepted: Wisp first binds existing DTO availability, cache keys, and consumer propagation, stopping for recon or Patron amendment if required fields are absent, unexpected consumers appear, or a file need falls outside the envelope rather than defaulting or expanding; then typed mutation and state with RTL proof, shared actions, Library, Details, and recommendation integration with reason rendering, accessibility and failure tests, and web gates. Palette design before plan challenge and no implementation until merged Gate 1 and Build pickup drift check are required. Accepted cost: narrow feature-local reuse with no generic new layer or library. Names are approved as proposed decisions, not claims the files already exist. No ticket contradiction and no constitution departure, so nothing structural.

### Q6 — Proof plan and grill closure

DEV-355 Q6 ACCEPT and grill closed: proof plan approved and shared understanding Q1-Q6 confirmed for closing into brief.md. Basis: ticket line 91 requires RTL tests for both actions and chip rendering and lines 94-96 require no-reload update visible on next recommendation fetch plus keyboard reachability and accessible labels, which the listed RTL and Vitest assertions cover action by action including correct POST, pre-response optimistic update, already-marked non-dispatch, at-most-once dispatch under duplicate, keyboard, and multi-instance activation with another movie proceeding, single-flag change, per-movie rollback without overwriting a concurrent different-movie success, stale-fetch protection, authoritative refresh including removal, inactive-query staleness, retained confirmed state with independent retry on refresh failure, and no automatic POST retries per recon line 17. Reason tests for verbatim Personal and Structural example strings in API order, duplicates, empty or absent inputs, and literal HTML-looking text with no fabricated strings or categories match ticket lines 90 and 95 and recon lines 22-26. Keyboard Enter and Space, accessible names with title, focus, error, pending and removal focus behavior, and parent-card navigation isolation cover the Q4 interaction contract. Mirroring existing tests at recon lines 26 and 35-38 with existing repo tooling and request interception, no generated MSW change and no added packages, keeps Q1 and Q5 boundaries. Future Build proof via web-implement red-green loop then run-web-gates and configured contract freshness checks, with Gauge classifying C-sharp-only gates from the actual diff and scope-empty reported as SKIPPED scope-empty never PASS and no thresholds lowered, is accepted; Phase A here runs no tests, build, or gates. Quill drafts next only after this brief and Palette design precedes challenge per Q1 and Q5. Existing vocabulary Watched, Not Interested, and reason chips is reused with no new glossary meaning or ADR warranted for this M frontend consumption task. Accepted cost: async integration coverage to prove failures and concurrency. No remaining Patron decision and no structural blocker.



## Phase B pickup and T001 binding — 2026-10-23

- Build identity: `F:/Dev/LamuFlix.worktrees/DEV-355`, branch `feature/DEV-355`, baseline HEAD `2a6181a38257d44e21bf667be6872022260d5865`. Phase A identity above remains historical. Recon confirms clean working tree and `origin/main...HEAD` counts `0 0` (recon-DEV-355:60,120-123; recon-DEV-355-2:5-7).
- Gate 1 established by user merge of spec PR #237 (DEV-355 note:38-41; task-pipeline Phase 2 step 7). Its content landed in linear history as `d803a4effb791d63fc0ad5f9f7c9464c96f5e6a3`, uniquely patch-identical to PR tip `d0cf6fce58a960e2f280a2fae40ed45097892b4d`; spec/plan/tasks are byte-identical (recon-DEV-355-2:15-21,39-46). No separate merge commit or retrospective provisional approval is claimed.
- T001 fields bound: initialize `isWatched` and `isNotInterested` from generated Library, Details and RecommendedMovie DTO fields; recommendation `reasons` is the generated string array. Library/Details have no reasons. No fabricated flags or reasons (recon-DEV-355:64-77).
- T001 keys/client scopes bound: root/library client owns `['browseMovies', browseParams]`; details client owns `['movieDetails', movieId]` and the sole recommendation family `['relatedMovies', id|null]`, using `relatedMoviesQueryKey(movieId)`. Refresh all recommendation queries in that family after successful feedback, including staling inactive queries. Thread the existing client scopes through the narrow feature-local implementation; do not assume one ambient client or invent a new key/provider/layer. Watchlist precedent supplies shared external state, not existing invalidation (recon-DEV-355:79-98). Preserve the Q3 per-movie rollback, exclusion and retry contracts.
- Patron T001 host ruling: `RelatedMovies` must mount shared `MovieCard`; retaining `RelatedMovieCard` with only shared `MovieFeedbackActions` is not approved. Basis: ticket accepted in Q1, brief:16/44, plan:11 and spec US2 require shared-card reuse. Recon:104-108 establishes current hosts and the conflicting design guidance. Mounting `MovieCard` inside `RelatedMovies.tsx`, with `MovieCarousel.tsx` changed only if needed, stays inside Q5. Library already mounts `MovieCard` through `LibraryTile`; no `LibraryTile` edit is authorized. Align design.md:109-113 with this ruling before host integration; do not reinterpret shared controls as shared-card reuse. This enforces frozen delivery and introduces no ticket change or constitution departure. Patron ruling received 2026-10-23 in `DEV-355-T001-host-ruling.md`.
- Baseline accepted from Gauge in DEV-355:22-33: full web gates, vulnerable-package gate, format verification and backend tests exit 0; diff-scoped web/Roslyn gates remain `SKIPPED (scope-empty)`, non-blocking and never PASS. Full web audit reported three non-blocking vulnerabilities. No new gates run by Keel.
- Disposition: T001 bind complete; pickup clearance granted for Phase 2 T002-T005 only, from the baseline above. Anvil follows web-implement and stops after the two named targeted proof files are green and the phase is committed. This is build intake clearance, not completed implementation, later-phase clearance, review clearance or merge-bar approval. Phase 2 contains no host chrome. The design correction belongs before Phase 4 host integration.