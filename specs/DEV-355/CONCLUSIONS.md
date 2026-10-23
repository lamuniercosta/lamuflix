# DEV-355 — Grill conclusions

Worktree: `F:/Dev/LamuFlix.worktrees/feature-355-spec`; branch `feature/355-spec`; Phase A base `8f9720958f99d62397b306615daf8d49a76d3032`. Ticket provenance: `recon-DEV-355:75-99`. Append-only full exchanges; Patron decides, Keel records.

## Q1 — Delivery boundary and loop discipline

### Keel question

[from Keel] /grill-with-docs DEV-355. Worktree F:/Dev/LamuFlix.worktrees/feature-355-spec, branch feature/355-spec, HEAD 8f9720958f99d62397b306615daf8d49a76d3032. Read DEV-355, recon-DEV-355 (authoritative ticket at lines 75-96), task-pipeline and charter. This is one continuing grill ask, at most 12 one-at-a-time questions. Do not edit files or commit; I will append full Q&A into specs/DEV-355/CONCLUSIONS.md and write the closing brief. Report each answer by ask back to Keel. Facts come from recon-DEV-355; missing facts are needs recon and stop. Q1: Do you approve this delivery boundary and loop discipline? Ticket scope at recon lines 85-96 is already decided: feedback actions on Library cards and Details, same card reused by recommendation panels, optimistic update then recommendation refresh, verbatim API Personal/Structural reason strings, keyboard/accessibility and RTL tests. I recommend frontend-only consumption of existing DEV-352/354 contracts; no new dependency, backend/schema/API/playback/secrets changes, or unrelated rewrites. Review closing bar: Critical/High/Medium block, Low only if it violates frozen acceptance or an applicable gate; two review rounds, two fix commits per round per task-pipeline lines 27-29; grill cap 12. Frozen scope ends: anything else is a follow-up issue, not a finding in this round. Palette owns visual design later and its design is required before plan challenge. Cost: broader UX/backend changes remain follow-ups. Please ACCEPT or AMEND with cited basis; flag only ticket contradiction or constitution departure as structural.

### Patron answer

DEV-355 Q1 ACCEPT: delivery boundary and loop discipline as proposed. Basis: recon-DEV-355 lines 85-96 authoritative ticket scope already decided (Library cards and Details actions, same card reused by recommendation panels, optimistic update then recommendation refresh, verbatim API Personal/Structural reason strings, RTL tests, keyboard and accessible labels); recon lines 41-48 section 2.3 check shows no new dependency, project, schema, API shape, LocalPlay, Process.Start, or secrets change, so frontend-only consumption of existing DEV-352 and DEV-354 contracts needs no escalation; task-pipeline lines 27-29 fix grill cap 12, review cap 2 rounds, 2 fix commits per round. Closing bar Critical/High/Medium block, Low only if it violates frozen acceptance or an applicable gate, frozen scope ends with follow-ups elsewhere, and Palette design required before plan challenge all accepted. No ticket contradiction and no constitution departure, so nothing structural.

### Rationale and implications

The ticket is authoritative. Frontend scope and review budgets are settled; no structural escalation was identified. Palette design precedes plan challenge. No implementation or Gate 1 approval follows from this ruling.

---

## Q2 — State/action contract

### Keel question

[from Keel] DEV-355 grill Q2: What is the state/action contract? I recommend mark-only commands within the ticket: Mark Watched POST /movies/{id}/watched and Not Interested POST /movies/{id}/not-interested; no undo/toggle product feature in this ticket. Existing true flags render an already-marked disabled state rather than dispatch again; preserve the other flag, never infer mutual exclusion. Share pending state for a movie across card/details/recommendation instances and block both actions for that movie while either request is pending, so duplicates and conflicting optimistic writes cannot race. Other movies remain interactive. Generated contract fields initialize state, no invented defaults for absent flags; absent required data is a recon/contract gap. Basis recon lines 14-19, 22, 29-33 and ticket lines 88-96. Cost: no undo affordance and per-movie serialization until settle. ACCEPT or AMEND with cited basis? Q1 is now durably recorded in CONCLUSIONS.md.

### Patron answer

DEV-355 Q2 ACCEPT: state/action contract as proposed. Basis: ticket lines 88-96 name only Mark Watched and Not Interested calling DEV-352 endpoints with immediate update and keyboard/accessible labels, no undo or toggle named, so mark-only POST commands stay within ticket and DELETE endpoints remain unused product surface; recon lines 14-19 show POST routes exist and Movie transitions throw on repeat, so an already-marked disabled state that does not dispatch again prevents double-fire; recon lines 22 and 32 show IsWatched and IsNotInterested as separate generated contract fields, so preserve the other flag and never infer mutual exclusion; shared per-movie pending blocking both actions while either request is pending prevents duplicate and conflicting optimistic writes while other movies stay interactive. Generated fields initialize state with no invented defaults, and absent required data is a recon/contract gap. Accepted cost: no undo affordance in this ticket and per-movie serialization until settle. No ticket contradiction and no constitution departure, so nothing structural.

### Rationale and implications

Only POST mark commands are delivered. Independent flags and shared per-movie pending state prevent repeat transitions; no undo UI is added.

---

## Q3 — Optimistic lifecycle

### Keel question

[from Keel] DEV-355 grill Q3: Do you approve this optimistic lifecycle? Recommend reuse the existing watchlist mutation pattern (recon 33), with a narrowly scoped shared per-movie feedback hook/state and generated API types. Before optimistic updates, protect affected queries against stale in-flight responses; retain pre-operation state and optimistically change only the chosen feedback flag consistently in already-loaded card/details/recommendation representations. Keep existing recommendation membership/reasons until server refresh: no client scoring, string rewriting, or removing Not Interested cards by guessed policy. On POST success, invalidate/refetch affected movie/library data and all recommendation queries depending on the global feedback profile, including inactive queries marked stale; next fetch is authoritative. POST failure restores only this operation without erasing unrelated movie updates, shows accessible retryable error and reconciles stale server state; no automatic POST retries because repeats throw (recon 17). Refresh failure after successful POST keeps the confirmed mark, exposes refresh error/retry, and never rolls back a server success. Per-movie exclusion covers the update/reconciliation lifecycle. Basis ticket 89/94, recon 17/22/32-33, contract chain charter 31. Cost: cache coordination and separate mutation-versus-refresh error handling. ACCEPT or AMEND with cited basis? Do not invent existing cache keys or APIs; implementation recon will identify them.

### Patron answer

DEV-355 Q3 ACCEPT: optimistic lifecycle as proposed. Basis: ticket lines 89 and 94 require immediate UI update then recommendation refresh visible on next fetch, which the optimistic-then-invalidate flow implements without a page reload; recon line 33 identifies the existing watchlist mutation pattern to reuse and line 32 confirms no web mutation to the feedback endpoints exists yet, so a narrowly scoped shared per-movie feedback hook with generated API types follows the contract chain and Q2 shared pending state; recon line 17 shows repeat transitions throw, so no automatic POST retries is correct; recon lines 22 and 90 show reasons are API strings only, so keeping membership and reasons until server refresh with no client scoring, string rewriting, or guessed Not Interested removal stays within ticket. Pre-operation retention with rollback of only this operation on POST failure, accessible retryable error plus reconciliation, and keeping a confirmed mark on refresh failure while exposing refresh error and retry correctly separate mutation failure from refresh failure. Per-movie exclusion across update and reconciliation matches Q2. Accepted cost: cache coordination and separate mutation-versus-refresh error handling, with concrete cache keys and APIs left to implementation recon and not invented here. No ticket contradiction and no constitution departure, so nothing structural.

### Rationale and implications

Rollback applies to mutation failure, not refresh failure after a confirmed success. Server responses own recommendation membership and reason strings. Concrete cache keys remain recon facts to bind before implementation.

---

## Q4 — Reasons and accessibility

### Keel question

[from Keel] DEV-355 grill Q4: Do you approve the reason and accessible interaction contract? Recommend a shared feedback action view on the Library card, Details action strip, and existing recommendation card composition; generated recommendation reasons are optional presentation input on that shared card, rendered verbatim, in API order, one non-interactive chip per returned string. No inferred Personal/Structural categorization because recon 22/23 exposes a flat strings list, no invented reasons for ordinary Library/Details data that lack them, and empty list means no reason chips. Preserve exact text even duplicates (stable distinct React keys); escape as React text, no HTML interpretation or truncation that hides full text from assistive tech. No client templates, rewriting, prefix classification or new API fields. [assumed] Labels: Mark Watched / Watched and Not Interested / Not Interested (marked); include movie title in accessible names on repeated cards. Native keyboard-operable buttons, visible focus, no accidental parent-card navigation when an action is used; pending announced, mutation error alert with explicit retry; reason chips are informational, not focusable controls. If refresh removes the focused recommendation card, preserve a sensible focus target in its panel. Palette decides placement/visual styling later within these constraints. Basis ticket 88-96, recon 22-31/53 and PRODUCT taste rules. Cost: no category badges or synthesized Library reasons. ACCEPT or AMEND with cited basis? Any taste rulings will be mirrored in ASSUMPTIONS.md.

### Patron answer

DEV-355 Q4 ACCEPT: reason and accessible interaction contract as proposed, with the label wording accepted as a taste ruling to log tagged assumed in ASSUMPTIONS.md. Basis: ticket lines 90 and 95 require Personal and Structural reasons rendered as returned with wording staying in the API, and recon lines 22-23 expose only a flat reasons string list mapped from reason text, so verbatim API-order rendering with one non-interactive chip per string, no inferred Personal or Structural categorization, no invented reasons where data lacks them, and empty list meaning no chips is the contract-faithful reading; recon lines 25-26 and 53 confirm existing verbatim chip rendering and no invented text. Exact text preserved including duplicates with stable distinct keys, escaped as React text with no HTML interpretation or truncation hiding full text from assistive tech, and no client templates, rewriting, prefix classification, or new API fields. Ticket line 96 requires keyboard reachability and accessible labels, supporting native keyboard-operable buttons, visible focus, no accidental parent-card navigation, movie title in accessible names on repeated cards, pending announcement, mutation error alert with explicit retry, non-focusable informational chips, and a sensible preserved focus target if refresh removes the focused recommendation card. Palette decides placement and visual styling later within these constraints. Accepted cost: no category badges or synthesized Library reasons. No ticket contradiction and no constitution departure, so nothing structural.

### Rationale and implications

A flat API string list is rendered faithfully; Personal/Structural labels cannot be inferred. The accepted label wording is a taste assumption mirrored separately. Palette retains visual design ownership.

---

## Q5 — File envelope and ordering

### Keel question

[from Keel] DEV-355 grill Q5: Do you approve this bounded implementation file envelope and ordering? Existing files, only as needed for these ACs: web/src/library/MovieCard.tsx, MovieCard.test.tsx, MovieCard.layout.test.tsx, MovieGrid.test.tsx, libraryQueries.ts; web/src/features/movie-details/MovieDetailsView.tsx, MovieDetailsView.test.tsx, movieDetailsApi.ts, movieDetailsQueries.ts, relatedMoviesApi.ts, RelatedMovies.tsx, RelatedMovies.test.tsx; web/src/ui/MovieCarousel.tsx only if required to pass shared card action/reason props. Proposed new leaf files under existing web/src/library: movieFeedback.ts (typed endpoint functions), movieFeedbackStore.ts (shared per-movie mutation state/hook using existing watchlist approach), MovieFeedbackActions.tsx (shared controls), MovieFeedbackActions.css (scoped style if Palette requires), plus corresponding movieFeedback.test.ts, movieFeedbackStore.test.tsx, MovieFeedbackActions.test.tsx. Names are proposed decisions, not claims files already exist. Do not rewrite watchlist code, generated types/OpenAPI/MSW, package/config/backend files, or build the future DEV-418 panel; existing recommendation reuse is integrated now and future consumers reuse the same card. Recon 29-38 names existing surfaces. Ticket 88/91 and charter 24 support edits necessary to ACs; please explicitly approve this care-list file scope. Ordering: Wisp binds existing DTO availability, cache keys and consumer propagation first; if absent required fields, unexpected consumers, or file need outside envelope stop for recon/Patron amendment rather than defaulting or expanding. Then typed mutation/state with RTL proof, shared actions, Library/Details/recommendation integration and reason rendering, accessibility/failure tests, web gates. Palette design before plan challenge; no implementation until merged Gate 1 and Build pickup drift. Cost: narrow feature-local reuse, no generic new layer/library. ACCEPT or AMEND?

### Patron answer

DEV-355 Q5 ACCEPT: bounded implementation file envelope and ordering approved as an explicit care-list ruling. Basis: recon lines 29-38 name the existing Library card, Details, recommendation, query, and RTL test surfaces listed, and ticket lines 88 and 91 require actions on those surfaces plus RTL tests, so edits to the named existing web files only as needed for these ACs are ticket-scoped; proposed new leaf files under the existing web/src/library folder are feature-local leaves, not a new project, top-level folder, architectural layer, or library, so no structural escalation arises. Typed endpoint functions, shared per-movie mutation state and hook, and shared controls implement the Q2 and Q3 contracts without rewriting watchlist code, generated types, OpenAPI, MSW, package, config, or backend files, and without building the future DEV-418 panel. Ordering accepted: Wisp first binds existing DTO availability, cache keys, and consumer propagation, stopping for recon or Patron amendment if required fields are absent, unexpected consumers appear, or a file need falls outside the envelope rather than defaulting or expanding; then typed mutation and state with RTL proof, shared actions, Library, Details, and recommendation integration with reason rendering, accessibility and failure tests, and web gates. Palette design before plan challenge and no implementation until merged Gate 1 and Build pickup drift check are required. Accepted cost: narrow feature-local reuse with no generic new layer or library. Names are approved as proposed decisions, not claims the files already exist. No ticket contradiction and no constitution departure, so nothing structural.

### Rationale and implications

The explicit file envelope is approved for necessary acceptance work only. New leaf names are decisions, not repository facts. Wisp must bind concrete cache/DTO/consumer facts before implementation, and unexpected needs require an amendment.

---

## Q6 — Proof plan and grill closure

### Keel question

[from Keel] DEV-355 grill Q6 (proposed final): Do you approve the proof plan and confirm shared understanding to close the grill into brief.md with Q1-Q6? Recommend RTL/Vitest with existing repo tooling and request interception, asserting: each action on Library/Details/current recommendation card calls the correct POST and updates before a deferred response without reload; existing true state never dispatches; duplicate/keyboard activations and two instances of the same movie dispatch at most once while another movie can proceed; only selected flag changes; failed POST restores that movie without overwriting a concurrent different-movie success; stale in-flight fetch cannot erase pending state; success refreshes global-profile-dependent recommendation queries and next response supplies authoritative state/reasons, including removal; inactive queries are stale for next fetch; refresh failure retains confirmed state with independent retry; no auto POST retries. Reason tests cover Personal/Structural example strings verbatim in API order, duplicate text, empty/absent inputs and literal HTML-looking text; no fabricated strings or categories. Keyboard Enter/Space, accessible names with title, focus/error/pending and removal focus behavior, and parent-card navigation isolation get interaction coverage. Mirror existing tests at recon 26/35-38; do not change generated MSW or add packages. Future Build uses web-implement red-green loop then scripts/run-web-gates.ps1 and configured contract freshness checks; Gauge classifies C#-only gates from actual diff with scope-empty reported SKIPPED (scope-empty), never PASS, no thresholds lowered. Phase A here runs no tests/build/gates; Quill drafts next only after this brief and Palette design precedes challenge. Existing vocabulary Watched, Not Interested and reason chips is reused; no new glossary meaning or ADR warranted for this M frontend consumption task. Ticket 91/94-96, Q1-Q5, PRODUCT contract/quality rules and pipeline stage boundary are the basis. Cost: async integration coverage to prove failures and concurrency. Any remaining decision or structural blocker, or ACCEPT and grill closed?

### Patron answer

DEV-355 Q6 ACCEPT and grill closed: proof plan approved and shared understanding Q1-Q6 confirmed for closing into brief.md. Basis: ticket line 91 requires RTL tests for both actions and chip rendering and lines 94-96 require no-reload update visible on next recommendation fetch plus keyboard reachability and accessible labels, which the listed RTL and Vitest assertions cover action by action including correct POST, pre-response optimistic update, already-marked non-dispatch, at-most-once dispatch under duplicate, keyboard, and multi-instance activation with another movie proceeding, single-flag change, per-movie rollback without overwriting a concurrent different-movie success, stale-fetch protection, authoritative refresh including removal, inactive-query staleness, retained confirmed state with independent retry on refresh failure, and no automatic POST retries per recon line 17. Reason tests for verbatim Personal and Structural example strings in API order, duplicates, empty or absent inputs, and literal HTML-looking text with no fabricated strings or categories match ticket lines 90 and 95 and recon lines 22-26. Keyboard Enter and Space, accessible names with title, focus, error, pending and removal focus behavior, and parent-card navigation isolation cover the Q4 interaction contract. Mirroring existing tests at recon lines 26 and 35-38 with existing repo tooling and request interception, no generated MSW change and no added packages, keeps Q1 and Q5 boundaries. Future Build proof via web-implement red-green loop then run-web-gates and configured contract freshness checks, with Gauge classifying C-sharp-only gates from the actual diff and scope-empty reported as SKIPPED scope-empty never PASS and no thresholds lowered, is accepted; Phase A here runs no tests, build, or gates. Quill drafts next only after this brief and Palette design precedes challenge per Q1 and Q5. Existing vocabulary Watched, Not Interested, and reason chips is reused with no new glossary meaning or ADR warranted for this M frontend consumption task. Accepted cost: async integration coverage to prove failures and concurrency. No remaining Patron decision and no structural blocker.

### Rationale and implications

Shared understanding reached at Q6 of 12. The proof plan, vocabulary reuse and phase boundaries are settled. The brief may now be written; no Spec Kit artifacts, implementation, gates, freeze or Gate 1 are authorized by grill closure alone.








## Phase B T001 host-contract ruling - 2026-10-23

Question from Keel: May RelatedMovieCard remain the recommendation host by sharing MovieFeedbackActions, or must RelatedMovies mount MovieCard to satisfy the frozen shared-card requirement?

[from Patron] DEV-355 T001 host-contract ruling: RelatedMovies must mount the shared MovieCard; RelatedMovieCard may not remain the recommendation host merely by sharing MovieFeedbackActions and verbatim reasons.
- Basis: ticket as recorded in recon-DEV-355:85-96 and accepted at CONCLUSIONS Q1 requires the same card reused by recommendation panels; brief.md:16 and plan.md:11 freeze that wording, and spec.md US2 acceptance is written against the shared card rendering reasons verbatim in API order.
- Contradiction identified: no contradiction inside the ticket itself; design.md:109-113 contradicts the ticket/brief/plan/spec by keeping local RelatedMovieCard inside MovieCarousel as host with only shared MovieFeedbackActions at design.md:113. Treating shared controls as shared-card reuse would silently substitute a lesser deliverable and change what the ticket delivers; keeping RelatedMovieCard would therefore be blocked: structural and is not approved.
- Scope effect: mounting MovieCard in RelatedMovies.tsx, with MovieCarousel.tsx only if needed to pass action/reason props, stays inside brief Q5 envelope at brief.md:56-58; Library already satisfies the shared-card host through LibraryTile mounting MovieCard and LibraryTile need not change; Phase 2 T002-T005 foundation adds no host chrome and is unaffected. Gate 1 frozen scope is binding via user merge of PR 237 per task-pipeline:83 and DEV-355:41, landed content d803a4e proven in recon-DEV-355-2:15-21,39-46.

Recorded in brief.md Phase B pickup and T001 binding before task disposition. No taste ruling or ASSUMPTIONS change.

---

## Phase 4 adjudication at 391f647 — 2026-10-23

### P4-1 — Two Medium store defects block

[from Patron] DEV-355 P4-1 BLOCK: Phase 4 does not pass until both confirmed Medium store defects are fixed — inactive cached errors counted as refresh failures, and failed POST plus failed reconcile emitting `Saved` copy.
- Basis: Q1 closing bar (this file, Q1) — Critical/High/Medium block; Low blocks only on frozen-acceptance or gate violation.
- Basis: Q3/ASSUMPTIONS store contract — refresh failure after a successful POST and mutation failure are distinct states; `Saved` copy is reserved for a confirmed mark, so emitting it after a failed POST misstates server state, and counting inactive cached errors as refresh failures misattributes stale cache state to the current refresh.
- Scope: fix belongs to the store fix round; Patron changes no source or design in this ruling.

### P4-2 — 409 as already-marked success

[from Patron] DEV-355 P4-2 ACCEPT: treating HTTP 409 as already-marked success, with no API change.
- Basis: existing pattern at `web/src/library/watchlistStore.ts:70-71` treats `204 || 409` as success.
- Basis: Q2/Q3 and the generated repeat-transition contract — repeat transitions throw server-side, so 409 confirms the mark already exists rather than a new failure; dispatching again or surfacing an error would contradict the already-marked disabled state.
- No new route, DTO field, or status-code change is introduced; §2.3 API-shape care item is not triggered.

### P4-3 — Details accessible title suffix

[from Patron] DEV-355 P4-3 ACCEPT [assumed]: a movie-title suffix in the Details accessible name is compatible and stands; `design.md:196-197` visible-label-alone wording does not block.
- Basis: brief Q4 requires the title in accessible names on repeated cards and nowhere forbids it on the single Details instance; a title-suffixed name still contains the settled visible label (`ASSUMPTIONS.md` Q4).
- Taste ruling logged tagged `[assumed]` in `ASSUMPTIONS.md`; no source or design edit in this round per the adjudication ask.

### P4-4 — design.md:90-92 reasons-inside-link text stale

[from Patron] DEV-355 P4-4 SUPERSEDED: `design.md:90-92` reasons-inside-link text is stale and does not govern; the T001 shared-MovieCard structure governs.
- Basis: Phase B T001 ruling (this file) retired `RelatedMovieCard` as host and required `RelatedMovies` to mount the shared `MovieCard`; `design.md:109-113` already records that tree with verbatim reasons on `MovieCard` and `MovieFeedbackActions` as sibling footer.
- Basis: `design.md:90-92` rests on the retired `.related-movie-reasons` rendering, so it cannot override the T001 ruling or brief Q4 verbatim-reasons contract.
- No design edit in this round per the adjudication ask; the stale lines are noted here as the controlling disposition.

### P4-5 — Low CSS ownership and optional error-block extraction

[from Patron] DEV-355 P4-5 FOLLOW-UP, noted no ticket: the Low CSS ownership suggestion and the optional error-block extraction are follow-ups and do not block Phase 4.
- Basis: Q1 closing bar — Low blocks only if it violates frozen acceptance or an applicable gate; neither item is shown to do so.
- Recorded as `noted, no ticket` per the follow-up rule; no YouTrack change is handed to Rigger for these items.

### P4-6 — Modified-click guard is not scope expansion

[from Patron] DEV-355 P4-6 ACCEPT: the modified-click guard preserves retired `RelatedMovieCard` navigation behaviour during the mandatory shared-card integration and is not scope expansion.
- Basis: T001 made mounting the shared `MovieCard` in `RelatedMovies` mandatory (this file, Phase B T001; `design.md:111-113`), so preserving the prior card navigation behaviour on modified clicks is integration parity, not a new feature.
- Basis: brief Q4/Q5 envelope — no accidental parent-card navigation on action use, with edits confined to the named shared-card surfaces; the guard adds no route, dependency, layer, or deliverable beyond that envelope.
