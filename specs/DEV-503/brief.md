# DEV-503 — Browser player controls grill brief

## Keel freeze decision — 2026-11-12

**PLAN FROZEN.** Phase 2 plan-challenge round 1 is closed after all three axis reports, Patron Q8, owner-authored repairs and independent Keel rereads. 27/27 findings dispositioned (19 accepted, 7 partially accepted, 1 rejected); all accepted portions repaired; zero unresolved above-bar planning findings. Detailed rationale and requirement/task evidence: `PLAN_ADJUDICATION.md`.

This is the current planning-status decision and supersedes drafting-era PLAN FROZEN withheld/pending-validation statements below and in drafting receipts. Q8 supersedes conflicting historical Q1–Q7 wording; all complete exchanges are retained. Keel may propagate this status-only decision to spec.md, plan.md, tasks.md and design.md after recording it here; no behavior, ownership or scope change follows from that propagation.

Read-only consistency/coverage validation: 19 FRs, 6 SCs, 20 unique unchecked tasks; all covered. Spec Kit prerequisite discovery succeeded for specs/DEV-503 with required tasks; no before/after-analyze hooks registered. Validation uses the supplied recon authority and constitution excerpts; no production-source reconnaissance or runtime testing was performed. Q8 file-boundary additions are approved and taste is logged; no owner structural checkbox, ticket contradiction or constitution departure.

**Gate 1 remains pending**: Bernstein coordinates/Rigger prepares-opens spec PR; only the user merges; T000 verifies that receipt. T001 still blocks production edits on fresh drift recon and documented 4.19.4 menu exports/semantics verification. Reference/fixture availability and Chromium runtime acceptance remain unverified, required later. No implementation, runtime, code-review or merge clearance is claimed. Round cap remains two challenge rounds/two fix commits per round; this repair work made no fix commits.

## Phase 2 challenge decision request Q8 (2026-11-12)

Status: APPROVED by Patron Q8 on 2026-11-12. The nine points below supersede conflicting Q1–Q7 interaction/design/file-boundary wording; the original exchanges remain historical. All three challenge reports are present: Sentry (Risk, 9), Ledger (Standards, 11), Compass (Spec, 7). Facts are exclusively recon-DEV-503, including its source update at lines 186–214. Keel has not inspected implementation sources. PLAN FROZEN remains withheld.

Keel asks Patron to rule on this single coordinated correction to the existing interaction/design/file-boundary contract:

1. Preserve ticket scope, Q2 closing bar and two-round/two-fix-commit caps. Clarify Q4: page-delivered Escape closes the surface before **application** fullscreen-exit handling. Do not promise control over browser-owned Escape. Add fullscreenchange-to-exited fallback closing stale surfaces and restoring focus to a connected trigger, else the connected player host. Verify actual native behavior in Chromium; no native Escape behavior is claimed established by recon. On movie change, restore focus only when it was inside the removed surface; on unmount, clean listeners without focusing detached nodes or stealing destination focus.
2. Keep incumbent MediaController noHotkeys and BrowserPlayer hotkey ownership (recon 199–201). Use a non-modal labelled region for CC, not role=dialog. Retain editable exclusions; avoid blanket panel exclusion. Controls consume their activation/navigation keys before the player bubble handler, which respects defaultPrevented; test single action and each preserved Space/K/left-right 10-second seek/M/F key on tabs, rows, close button and elsewhere. Do not mandate capture-phase playback handling.
3. Choose track radiogroup/radio/aria-checked, Off plus served tracks; arrows move/select and Space/Enter activate; one radio tab stop. Tabs use tablist/tab/tabpanel, roving tabindex, Left/Right and Home/End, automatic tab activation; selected tab enters Tab order. Keep media-chrome native speed semantics verified at pickup rather than mixing radio/listbox patterns or wrapping native speed items.
4. Local overlay coordination in BrowserPlayer; one authoritative open-surface state, no new state library. Outside pointerdown uses composedPath, with active surface plus its trigger as inside; another trigger switches surfaces once; control-bar/video outside that boundary dismisses. In fullscreen, outside means reachable content inside the fullscreen subtree. Pointercancel alone does not dismiss. Clean up any document/fullscreen subscriptions in one coordinated effect; do not force document capture for control activation. Observe media-chrome native open/close state through documented events/attributes confirmed at pickup; no invented API or mirrored unsynchronized state.
5. Add only playerCopy.ts and subtitleCopy.ts to the Q3 editable boundary (recon 208–210); new copy belongs there. Retain hook/data/helper/generated files read-only. Preserve existing stale-result/owned-URL cleanup (recon 203–206); add rapid track/Off/movie-change/unmount regressions through the new list, with no mandatory AbortController/hook rewrite. Remove dead select CSS/tests while preserving behavior tests. Cleanup is remove-if-present at the recon-established owner, preserving the in-bar play control; do not assume the duplicate belongs to MovieDetailsView (recon 195).
6. Ratify existing Palette labels Subtitles, Settings, Close subtitles panel and Playback speed; x-suffixed rates with 1x (Normal), Options copy as Q5; no wireframe annotations rendered as copy. Tag these taste details [assumed]. At pickup verify supported label customization before implementation; stop for a decision if unsupported rather than replace media-chrome. Use existing CSS tokens, not design hex/RGBA literals. Retain bottom-right anchoring; at narrow widths wrap the strip, give seek its own row, constrain overlay width/height to the player and scroll content, keeping every control including volume reachable. No new breakpoint/size threshold is invented here. Ratify 320px operability, visible focus and reduced-motion extension as accessibility verification of delivered controls, not extra product features. Chromium proves layout/focus appearance/motion; RTL proves roles, state and keyboard behavior, not computed CSS.
7. Keep optional Q7 extraction optional; do not invent a line-count threshold or force extraction against the existing ruling. Preserve status/polite and alert semantics as established in recon 194–197; never call role=alert polite. Keep speed indication sourced from actual media rate, including ratechange and source/movie/metadata transitions; no persistence and no forced 1x reset contrary to live state.
8. Exact settings-menu exports, import paths, native semantics, rates/labels/open-state/anchor/fullscreen behavior remain explicitly **unverified** (recon 212–214). Require Wisp official-doc/4.19.4 export verification in T001 before production edits, record the chosen composition and synchronization mechanism, and STOP for recon/decision on unsupported seams. Do not canonize Ledger spelling/import guesses. Correct design data/helper/action assertions to the established recon facts; remove unverified shipped-on-main lineage. State that reference asset availability is unverified, not measured absent by Keel.
9. Make actual fullscreen control composition explicit in T003; keyboard track-to-Off native-fullscreen pass explicit in T016; add Bernstein/Rigger spec-PR preparation/opening producer before T000 receipt (only user merges). No Gate 1/implementation/runtime/merge claim follows from plan freeze.

Basis: ticket recon 34–69, 77–90; PRODUCT authority/taste/care-list recon 96–123; recon source facts 186–214; Q1–Q7. This refines existing delivery and accessibility without changing ticket scope or departing from the constitution. Patron must identify either escalation if that assessment is wrong. Keel will append the complete ruling before dispatching one numbered repair list to Quill and a design-only repair request to Palette.

## Status and authority

Grill complete: Patron confirmed shared understanding at Q6 on 2026-11-12. Six questions used; cap twelve. This freezes the grill scope and decisions for Quill. It does not claim PLAN FROZEN, a completed plan challenge, Gate 1, implementation, runtime acceptance, or merge clearance.

- Worktree: F:/Dev/LamuFlix.worktrees/feature-503-spec
- Branch: feature/503-spec
- Recon base: d64da227efbd75a78529e830edbdf51871cc4bf2, recorded in recon-DEV-503.
- Ticket: DEV-503, Rebuild browser player controls with CC panel and settings menu; size M; parent DEV-100.
- Facts: recon-DEV-503. Lines 19-26 supersede its initial unsuccessful source-discovery section. Lines 28-90 contain authoritative ticket text; lines 92-184 contain PRODUCT/constitution findings.
- Governance: specs/PRODUCT.md authority and care-list rules, task-pipeline Phase 2 and loop budgets, lamuflix-team-charter.
- Complete question/answer exchanges are reproduced below and retained append-only in CONCLUSIONS.md. Taste assumptions are recorded in ASSUMPTIONS.md.
- No structural owner question was raised in the six rulings. Any later ticket contradiction or necessary constitution departure requires an explicit owner checkbox and keeps Gate 1 closed.

## Loop discipline

Closing bar (Q2): Every verified in-scope Critical, High, Medium or Low correctness, explicit-acceptance, accessibility, contract or documented-standard violation blocks closure until fixed or demonstrated invalid. Preference changes and unrelated improvements do not expand remediation scope.

Round cap: At most two challenge/review rounds and two fix commits per round. An unresolved above-bar finding at the cap stays open for user review on the PR; it is not waived or automatically cleared. Review publication and later merge-bar evidence remain governed by the pipeline.

Frozen scope: The ticket control rebuild, combined time display, persistent CC panel shell with local-track selection, media-chrome gear speed menu, duplicate-control cleanup, keyboard/fullscreen/dismissal behavior and required Chromium visual receipts described below; anything else is a follow-up issue, not a finding in this round.

For out-of-scope recording, Critical/High or broken behavior follows Patron/Rigger routing, folding into an existing issue when appropriate; lesser observations are recorded as noted, no ticket. No new out-of-scope observation was established during this grill.

## Required behavior and frozen boundary

1. Use MediaTimeDisplay with showDuration for current / duration, such as 0:12 / 1:17:05. Remove separate MediaDurationDisplay.
2. Control bar: play/pause, seek, combined time, mute and volume, CC, gear, fullscreen.
3. CC is always visible, including zero tracks or unavailable subtitles. Open a bottom-right panel over the video with Subtitles and Options tabs and a close button.
4. Subtitles lists Off plus served DEV-454 local tracks, with a checkmark and accessible selected state. It must not be a select. Selecting a track shows it; Off hides all tracks, including through keyboard-only operation.
5. Options is a reachable panel shell, with honest non-actionable copy. Timing/style behavior belongs to its later ticket.
6. Gear uses media-chrome settings-menu functionality, with speed values 0.5, 0.75, 1, 1.25, 1.5, 1.75, 2 and current speed selection exposed. Leave extension room for future audio/quality functionality.
7. Remove the subtitle select and duplicate Play button under the player. Account for the DEV-493 overlap during Phase B pickup recon.
8. CC and gear function in windowed and fullscreen modes, dismiss on Escape/outside click, and remain keyboard operable. Preserve Space/K, arrows, M and F player hotkeys outside control interactions.
9. Required Chromium receipts compare delivered controls/panel shell against ticket references using real movie-folder fixture copies with the real library layout.

Ticket basis: recon-DEV-503 lines 34-69. Scope ruling: Q1. All series owner rulings at lines 77-90 remain binding:
- Retain media-chrome 4.19.4; no new player library.
- Subtitle delay is browser-only per device, keyed by movie id plus track; style is browser-only global. Storage failure must never break playback. This ticket introduces neither setting nor persistence.
- Subtitle layout remains <movie folder>\Subs\<video stem>-<code>.srt; no layout rewrite or backend implementation is added here.
- Browser acceptance uses copies of real E:\Filmes movie folders retaining that layout, not solely synthetic same-folder fixtures. Configure fixture paths externally, never hardcode machine paths in source.

Search acquisition, actual timing/style controls, backend layout changes, and functional audio/quality settings are deferred series work under Q1; do not fabricate controls, search results or working deferred actions. Reference comparison must state which delivered shell areas are compared and which depicted future contents are outside this ticket. This distinction is not permission to weaken delivered-shell fidelity or real-layout acceptance.

## Approach and explicit file ownership boundary

Q3 authorizes the tracked file edits needed by ticket behavior under care-list item 6:
- web/src/features/playback/BrowserPlayer.tsx
- web/src/features/playback/BrowserPlayer.css
- web/src/features/playback/SubtitleSelector.tsx
- web/src/features/playback/BrowserPlayer.test.tsx
- web/src/features/playback/subtitleSelector.test.tsx
- web/src/features/movie-details/MovieDetailsView.tsx
- web/src/features/movie-details/MovieDetailsView.subtitles.test.tsx

The paths under playback and the caller/test inventory are identified by recon lines 23-25. MovieDetailsView edits are restricted to duplicate-Play cleanup and necessary integration. Optionally extract one small co-located panel component/test under playback if accessibility or complexity warrants it; decide its exact name in planning rather than inventing an existing path.

Keep the media-chrome controller and existing playback/subtitle hook and served-track contracts. Replace selector presentation, compose the CC shell and media-chrome gear menu, and keep overlays inside the fullscreen player subtree. No new dependency, top-level folder, project, architectural layer, state library, schema, public API, server persistence, LocalPlay execution, secrets or Process.Start changes. Contract generation is not required for this UI-only scope; any newly necessary contract change requires an explicit ruling and the mandated C# DTO to OpenAPI to generated-TypeScript chain.

Unidentified edits outside this boundary need a cited ruling unless forced by the ticket acceptance criteria or a gate. Do not widen refactoring. Missing implementation facts go to the Conductor as needs recon. Consult official media-chrome documentation for exact APIs during detailed planning/implementation; this brief does not assert undocumented component prop names.

## Interaction and failure contract

Q4:
- Only one top-level CC panel or gear menu opens at a time. A trigger toggles its own surface; opening the other closes the first.
- Opening enters the active surface with focus. Escape closes it and returns focus to its trigger before player fullscreen-exit handling. Outside pointer dismissal preserves focus on the clicked destination.
- Named buttons, visible focus, programmatic tab/tabpanel and selected-track states, keyboard activation/navigation and appropriate list/radio semantics.
- A control consuming Space/K, arrows, M or F must not also trigger playback/seek/mute/fullscreen actions. Preserve player hotkeys elsewhere.
- Reset open surfaces on movie change/unmount; keep controls functional across fullscreen transitions.

Q5:
- Subtitles is active on a fresh open. Off is available and selected whenever no local track is active.
- Retain served-track labels/order, with visual checkmark and programmatic selection.
- Preserve existing zero-track/loading/unavailable/content-load-failure/retry semantics and playback. No silent track fallback or invented API/error contract.
- Keep CC visible across those states.
- Options copy: Subtitle timing and style options will appear here.
- Speed reflects current playback state; add no saved preference.

The micro-interaction, order and copy choices marked assumed by Patron are recorded in ASSUMPTIONS.md. Palette supplies concrete visual design against the ticket references; it must respect these behavior decisions.

## Test strategy and evidence

Extend meaningful tests in the approved existing Vitest/React Testing Library suites using web-implement TDD during Phase B:
- Combined time/control composition and removal of duplicate duration/select/Play presentation.
- Persistent CC, tab switching and close; Off/served-track selection with visible and accessible selected state.
- Zero/loading/unavailable/content-failure/retry coverage without playback regressions; movie change/unmount and stale-subtitle lifecycle behavior remain covered.
- Every allowed speed choice and current selected state; no off-step options.
- Mutual exclusion, trigger toggle, Escape/outside click and focus handling.
- Keyboard-only tab/track/Off/speed operation and player hotkey isolation versus preserved normal shortcuts.
- Movie-details integration and regression of existing playback/seek/error behavior.

Unit/component assertions cannot prove native fullscreen, real media behavior or screenshot fidelity. Keep explicit Chromium acceptance tasks:
1. Obtain externally configured real movie-folder copies and ticket reference assets; retain Subs/<video stem>-<code>.srt layout.
2. Play a real movie and capture readable current / duration.
3. Exercise CC open, both tabs, close button, Escape and outside click in windowed and fullscreen modes.
4. Select an actual local track and then Off using only the keyboard; verify rendered/hidden captions.
5. Change speed through gear and verify actual playback state; cover keyboard access and current-state indication.
6. Check focus, surface mutual exclusion and preserved Space/K, arrows, M and F shortcuts.
7. Capture screenshots and compare the delivered UI to ref-subtitles-tab.webp and current-player-time-bug.webp, using the other Options references only for applicable shell context, without claiming deferred content was delivered.

Receipts identify fixture provenance/layout (without secrets), reproducible steps, browser/version, observed outcomes, screenshot locations and comparison results. Missing fixtures, reference images or runnable runtime means Could not run with an open acceptance task. Do not replace required real-layout receipts with synthetic-only fixtures or component tests.

Reference filenames from ticket: ref-subtitles-tab.webp, ref-options-timing.png, ref-options-style-1.png, ref-options-style-2.png, current-player-time-bug.webp. Availability was not established by recon; obtaining them is a planning/acceptance prerequisite, not a claim that they exist locally.

## Gate expectations and ordering constraints

Phase A:
1. Quill derives spec.md, plan.md and tasks.md from this brief and recon; gaps are needs decision to Keel, never a guessed requirement.
2. Keel runs speckit-analyze read-only and checks plan/tasks against this brief; send one numbered repair list per round and independently reread repairs.
3. Palette delivers design.md against the reference assets before plan challenge. Quill must incorporate its settled UI requirements rather than guess visual details.
4. Conduct capped plan challenge and record dispositions before claiming PLAN FROZEN.
5. Open the spec PR with provisional Gate 1; user merges. No implementation before Gate 1. This ask does not create or merge the spec PR.

Phase B, in the Build workspace:
1. Pickup recon checks origin/main drift, named file inventory and DEV-493 cleanup overlap; re-run artifact analysis and reconcile actual drift with decisions first.
2. Use web-implement TDD. Start combined-time/cleanup and CC shell foundations; follow with track-list/Off and gear speed behavior; then finish focus/fullscreen/keyboard coordination and integration regressions.
3. Gauge runs scripts/run-web-gates.ps1 for implementation web changes and applicable configured gates. Web gate exit 0 is required when web is in scope. No plain build substitution.
4. Preserve exact outcomes: scope-empty exit 2 is SKIPPED (scope-empty), never PASS; harness-disabled SKIP and mutation N/A only when actually reported by their scripts. Exit 1 and Could not run block the applicable gate. Do not claim C# gates passed for a web-only diff.
5. Run required Chromium acceptance and publish its receipts as separate runtime evidence. Missing runtime evidence remains open and blocking acceptance even if unit gates are green.
6. Apply capped review/remediation and later exact-head merge-bar procedures in the Build workspace; only the user merges.

The Phase 1 task note records baseline web-gates exit 2 SKIPPED (scope-empty), not a PASS or runtime receipt. No tests, production code or gates were run by Keel during this grill. No new domain term, ADR or constitution amendment was decided for this M ticket.

## Complete grill transcript

The following reproduces every question and complete Patron answer. Recommendations, implications and cited bases form part of the decisions above.

# DEV-503 Grill conclusions

Worktree: F:/Dev/LamuFlix.worktrees/feature-503-spec; branch: feature/503-spec.
Authority: recon-DEV-503, authoritative ticket excerpts and PRODUCT/constitution excerpts; task-pipeline and lamuflix-team-charter. Decisions are Patron rulings. Keel records the complete exchanges.

---

## Q1 — Frozen scope

Keel: Do we freeze DEV-503 to the exact ticket control rebuild, combined time display, persistent CC button with Subtitles and Options panel shell, Off plus served local-track checkmark list, media-chrome gear speed menu 0.5 to 2 in 0.25 steps, duplicate Play/select removal, fullscreen/dismissal/keyboard/hotkey behavior and Chromium reference receipts, leaving search acquisition, timing/style implementation, audio/quality functionality and file-layout implementation to their series tickets? I recommend yes: ticket recon lines 34-69 explicitly assigns the shell here and Options content later; series owner rulings at 77-90 remain binding, including real-library-layout fixtures. Reference matching covers the UI delivered here, without fabricating future functionality. Cost: future panel content requires later integration. Any actual contradiction with the ticket must be an owner checkbox rather than a scope assumption. Please rule on this scope boundary.

Patron (complete answer):
Ruling: Yes — freeze DEV-503 strictly to the ticket's control bar rebuild, combined `<MediaTimeDisplay showDuration />`, persistent CC button with Subtitles/Options panel shell (Off + served local tracks checkmark list), media-chrome gear speed menu (0.5x–2x in 0.25 steps), duplicate Play/`<select>` removal, fullscreen/Escape/outside-click/keyboard/hotkey behavior, and Chromium visual reference receipts against real-library-layout fixtures.

- **Cited basis (`recon-DEV-503` lines 34–69, `specs/PRODUCT.md` §3)**: Ticket Scope & Acceptance explicitly scope DEV-503 to building the CC panel shell, local-track checkmark list, `<MediaTimeDisplay showDuration />`, media-chrome gear speed menu (`0.5–2x` in `0.25` steps), and `<select>`/duplicate Play button removal, while explicitly deferring the Options tab content ("filled by the timing/style ticket") and audio/quality ("leave room for audio track and quality later").
- **Series owner rulings (`recon-DEV-503` lines 77–90)**: Owner rulings 1–4 remain binding across the series without narrowing: keep `media-chrome` 4.19.4 (no new player library), run Chromium real-browser acceptance against real library layout fixtures (`<movie folder>\Subs\<video stem>-<code>.srt` / `E:\Filmes` folder copies), and match `ref-subtitles-tab.webp` for the delivered shell and controls without fabricating deferred search/timing/style functionality.
- **Implications**: Spec and tasks for DEV-503 must cover only the controls, shell, local-track selection, speed menu, cleanup, keyboard/fullscreen behavior, and real-layout Chromium visual receipts specified in lines 34–69 and 77–90; any contradiction with ticket text must be escalated as `blocked: structural` for an owner checkbox rather than assumed.

---

## Q2 — Closing bar and caps

Keel: Q2: What closing bar and review cap apply? I recommend every verified in-scope Critical, High, Medium and Low correctness, explicit-AC, accessibility, contract or documented-standard violation blocks closure until fixed or demonstrated invalid; taste preferences and unrelated improvements are follow-ups, never fix scope. Use at most two challenge/review rounds and two fix commits per round; unresolved above-bar findings at cap remain open for user review on the PR, never auto-cleared. Frozen scope ends: anything else is a follow-up issue, not a finding in this round. Basis: task-pipeline budgets, ticket acceptance and charter separation of rulings from owner authority. Cost: even a small actual AC defect blocks closure; capped rounds may end with an explicitly unresolved PR. Please rule.

Patron (complete answer):
Ruling: Yes — adopt the recommended closing bar and review/remediation caps verbatim.

- **Cited basis (`task-pipeline` lines 28–29, 138–142; `lamuflix-team-charter` lines 16–28, 38)**: Standing pipeline budgets mandate at most 2 challenge/review rounds and at most 2 fix commits per round, with third-round findings escalated to user review on the PR (`task-pipeline` §2.2 lines 28–29) and a clean findings artifact with zero above-bar findings required for the merge bar (`task-pipeline` §8 line 140).
- **Closing bar & scope boundary (`recon-DEV-503` lines 63–69; `lamuflix-team-charter` lines 38, 89)**: Every verified in-scope Critical, High, Medium, or Low correctness, explicit-AC, accessibility, contract, or documented-standard violation blocks closure until remediated or proven invalid; out-of-scope findings or taste improvements never expand fix scope (Critical/High or broken behaviour become follow-up tickets via Rigger; anything else is `noted, no ticket` in `CONCLUSIONS.md`).
- **Implications**: Plan challenge and code review rounds are strictly capped at 2 rounds and 2 fix commits per round; any unresolved above-bar finding remaining at the cap stays open on the PR for the user rather than being auto-cleared or silently waived.

---

## Q3 — Approach and file boundary

Keel: Q3: Approve this implementation approach and explicit file boundary? Keep media-chrome 4.19.4 and its existing controller, use its settings-menu implementation for gear/speed, retain existing playback/subtitle hooks and served-track contracts, replace the SubtitleSelector select presentation with the CC shell/list, and keep panel/menu DOM inside the fullscreen player subtree. No new dependency, project/layer, schema/API, persistence, LocalPlay execution or process change. Primary implementation files: web/src/features/playback/BrowserPlayer.tsx, BrowserPlayer.css, SubtitleSelector.tsx and their existing BrowserPlayer.test.tsx / subtitleSelector.test.tsx; web/src/features/movie-details/MovieDetailsView.tsx and its existing MovieDetailsView.subtitles.test.tsx only for duplicate-Play cleanup and integration. Permit a small co-located panel component/test under playback if separation improves accessibility, without introducing an abstraction layer. I recommend this minimal boundary; cost is retaining the current hook contracts and limiting refactoring. Please explicitly approve the care-list file edits under ticket control-rebuild/removal AC and recon lines 23-26, since the ticket names behaviors rather than paths. Unidentified necessary edits require a cited decision rather than a guessed path; media-chrome API details go to official docs during implementation.

Patron (complete answer):
Ruling: Yes — approve the implementation approach and explicit §2.3 item 6 file boundary (`web/src/features/playback/BrowserPlayer.tsx`, `BrowserPlayer.css`, `SubtitleSelector.tsx`, `BrowserPlayer.test.tsx`, `subtitleSelector.test.tsx`, `web/src/features/movie-details/MovieDetailsView.tsx`, `MovieDetailsView.subtitles.test.tsx`, plus at most a small co-located panel component/test under `web/src/features/playback/` if needed for accessibility/complexity gates).

- **Cited basis (`recon-DEV-503` lines 23–26, 34–69, 78; `lamuflix-team-charter` §2.3 items 1–6)**: Owner ruling 1 (`recon-DEV-503` line 78) mandates keeping `media-chrome` 4.19.4 with no new dependency, and ticket Scope/Acceptance (`recon-DEV-503` lines 34–69) force edits to the tracked playback and movie-details files identified in `recon-DEV-503` lines 23–25 (`BrowserPlayer.tsx`, `BrowserPlayer.css`, `SubtitleSelector.tsx`, `MovieDetailsView.tsx`, and their existing test suites) to rebuild the controls, replace the `<select>` with the CC panel shell/checkmark list inside the fullscreen player subtree, add the media-chrome gear speed menu, and remove the duplicate Play button.
- **Zero other §2.3 touches (`recon-DEV-503` lines 26, 78–79; `specs/PRODUCT.md` §2, §5)**: Existing playback/subtitle hooks (`useBrowserPlayer`, `useSubtitlePlayback`) and served-track API contracts remain unchanged, with no new npm/NuGet dependency, no new architectural layer/state library, no database/API change, and no touch to `Features:LocalPlay`, secrets, or `Process.Start`.
- **Implications**: Implementation is confined to the approved playback and movie-details file set (plus optional co-located panel component/test in `web/src/features/playback/` without new abstraction layers); any additional tracked file edit not forced by these ACs or a gate requires an explicit cited ruling before editing.

---

## Q4 — Interaction and accessibility

Keel: Q4: Approve the panel/menu interaction contract? I recommend only one top-level CC panel or gear menu open at a time; toggling its trigger closes it, opening the other closes the first. Opening moves focus into the active surface; Escape closes the active surface and returns focus to its trigger before any player fullscreen-exit action, while outside pointer dismissal preserves the clicked destination focus. Provide named buttons, visible focus, accessible selected tab/track states and keyboard activation/navigation using tablist conventions for Subtitles/Options and appropriate list/radio semantics for tracks. Suppress player Space/K, arrows, M and F shortcuts while a panel/menu control is consuming that key, so selection/navigation never seeks or toggles playback; preserve shortcuts elsewhere. Close stale surfaces on movie change/unmount, and keep controls functional through fullscreen transitions. Basis: ticket recon 44,57-67 and sound accessibility practice. Cost: explicit focus/event coordination rather than click-only overlays. Palette will specify visual details; this contract does not preempt its design.

Patron (complete answer):
Ruling: Yes — approve the panel and gear-menu interaction, focus, accessibility, and keyboard-coordination contract (`[assumed]` for UI micro-interaction details beyond explicit ticket text).

- **Cited basis (`recon-DEV-503` lines 44, 57–67; `specs/PRODUCT.md` §3, §4; `task-pipeline` line 86)**: Ticket Scope & Acceptance explicitly require the CC panel and gear menu to work in both windowed and fullscreen modes, close on `Escape` and outside click, be reachable and operable by keyboard alone (including track selection and `Off`), and preserve existing player hotkeys (`Space`/`K`, arrows, `M`, `F`), while `task-pipeline` Phase 2 step 4 mandates accessible state and keyboard contracts alongside Palette's `design.md`.
- **Accessibility & shortcut isolation (`recon-DEV-503` lines 25, 58–67)**: Mutual exclusion between the CC panel and gear menu, focus entry on open, focus restoration to the trigger on `Escape` (without unintended fullscreen exit) versus preserving target focus on outside pointer click, ARIA `tablist`/`tab`/`tabpanel` and track selection semantics, closing open surfaces on movie change/unmount, and suppressing player hotkeys (`Space`/`K`, arrows, `M`, `F`) while focus is inside an active panel/menu control prevent keyboard conflicts so keyboard-only track and speed selection never accidentally seeks or toggles playback.
- **Implications**: `spec.md`, `design.md` (Palette), and component tests in `BrowserPlayer.test.tsx` / `subtitleSelector.test.tsx` must enforce mutual exclusion, `Escape`/outside-click dismissal, ARIA tab/selection semantics, fullscreen retention, movie-change reset, and hotkey suppression when panel/menu controls consume key events; log the UI micro-interaction defaults tagged `[assumed]` in `specs/DEV-503/ASSUMPTIONS.md`.

---

## Q5 — Shell and failure states

Keel: Q5: Approve the shell and failure-state presentation? I recommend Subtitles defaults active on each fresh panel open; Off is always available and selected when no local track is active; served tracks retain their existing labels/order and show a checkmark plus programmatic selection state. Zero-track, loading, unavailable, content-load-failure and retry states preserve existing hook semantics and playback; do not invent new API/error contracts or fallback to another track silently. Keep CC visible in all those states. Options is a reachable, honestly empty shell with concise non-actionable copy such as Subtitle timing and style options will appear here; no working timing/style, search acquisition, audio/quality controls or new persistence in this ticket. Maintain the extension space required by the reference and future tickets without displaying fake search results or controls that claim unsupported actions. Gear speed choices are exactly 0.5, 0.75, 1, 1.25, 1.5, 1.75 and 2, with current selection exposed; use current playback state rather than adding saved preferences. Basis: recon 24-25,44-53,63-90 and PRODUCT taste rules. Cost: explicit empty shell copy is less visually complete than later series screenshots. Please rule and tag taste details assumed.

Patron (complete answer):
Ruling: Yes — approve the shell, failure-state, Options empty-state, and gear speed menu presentation contract (`[assumed]` for default active tab on open, existing track order retention, and concise Options empty-shell copy).

- **Cited basis (`recon-DEV-503` lines 24–25, 44–53, 65–68; `specs/PRODUCT.md` §3, §4)**: Ticket Scope & Acceptance require a persistent CC button (even with zero local tracks), a Subtitles tab showing `Off` plus served local tracks from DEV-454 with a checkmark on the active option, an Options tab shell ("filled by the timing/style ticket"), and a media-chrome gear speed menu with exact steps `0.5, 0.75, 1, 1.25, 1.5, 1.75, 2` (`0.5–2x` in `0.25` steps) reflecting current playback state without new persistence.
- **Failure-state preservation & honest shell (`recon-DEV-503` lines 24–25, 45–53; `specs/PRODUCT.md` §4)**: Zero-track, loading, unavailable, content-load-failure, and retry states preserve existing `useSubtitlePlayback` / `SubtitleSelector` semantics without inventing new API/error contracts or silent track fallback, while the Options tab renders an honest, non-actionable empty shell (e.g., concise copy `"Subtitle timing and style options will appear here."`) and preserves layout room for future search/timing/style/audio/quality tickets without fake controls or fake search results.
- **Implications**: `spec.md`, `design.md`, and tests must verify persistent CC button visibility across all track/error states, `Subtitles` as the default tab on fresh open (`[assumed]`), `Off` + served track checkmark and programmatic selection state, honest non-actionable Options tab copy (`[assumed]`), and exact `0.5–2x` (`0.25` step) speed selection; log the taste items tagged `[assumed]` in `specs/DEV-503/ASSUMPTIONS.md`.

---

## Q6 — Verification, ordering and grill closure

Keel: Q6: Approve this test/gate and task-order plan as the final grill decision and confirm shared understanding? Spec work first: Quill drafts spec/plan/tasks solely from brief and recon; Keel runs read-only speckit-analyze, Palette supplies design.md against the named visual references before plan challenge, at most two challenge rounds, then spec PR Gate 1 before any implementation. Later build: use web-implement Vitest/RTL TDD in the approved existing suites; add meaningful coverage for combined time/control cleanup, CC/tab/selection/Off and empty/failure/retry states, all speed choices/current state, mutually exclusive surfaces, focus/Escape/outside click, keyboard/hotkey isolation, lifecycle regression and movie-details integration. Implement combined time/cleanup and shell foundations before track/gear behavior, then coordinate accessibility/fullscreen and regressions. Gauge runs applicable web gates; do not replace them with a plain build or fabricate C# passes: scope-empty exit 2 is SKIPPED (scope-empty), never PASS; exit 1 or Could not run blocks. Chromium acceptance is separate, required evidence: real movie playback with copied E:/Filmes folder fixtures retaining Subs/<video stem>-<code>.srt (fixture root supplied via environment/test configuration, no hardcoded machine paths), current/duration with separator, keyboard-only track-to-Off and speed changes, tab/close/Escape/outside click, existing hotkeys, windowed and fullscreen. Capture reproducible fixture identity, steps, browser/version, screenshots and comparison against ticket references, distinguishing delivered-shell comparison from deferred reference content. A missing fixture/reference/runtime is Could not run with an open acceptance task, never a pass or synthetic substitute. Include DEV-493 overlap check during pickup recon to avoid duplicate deletion. Cost: unit gates alone cannot close browser acceptance; no runtime claim is made in Phase A. No new test dependency or bespoke test framework. Please rule; if approved I will write brief.md and ASSUMPTIONS.md, preserving every complete exchange.

Patron (complete answer):
Ruling: Yes — approve the test, gate, Chromium real-browser acceptance, and task-ordering plan as the final grill decision for DEV-503; shared understanding confirmed.

- **Cited basis (`task-pipeline` lines 65, 67–110; `recon-DEV-503` lines 56, 65–69, 75)**: Phase A completes `brief.md`, Spec Kit artifacts (`spec.md`, `plan.md`, `tasks.md`), `/speckit-analyze`, Palette `design.md` against the ticket visual references (`ref-subtitles-tab.webp`, `current-player-time-bug.webp`), plan challenge (<= 2 rounds), and Gate 1 before Phase B; Phase B intake recon checks `origin/main` drift (including the DEV-493 duplicate-Play overlap at `recon-DEV-503` line 56), and `web-implement` executes Vitest/RTL TDD in the existing test suites ordered from time-display/cleanup and CC shell foundations to track/gear behavior and fullscreen/keyboard/hotkey coordination.
- **Gate & Chromium acceptance rules (`task-pipeline` lines 99–108; `recon-DEV-503` lines 65–69, 80–88, 129–130)**: Gauge runs `scripts/run-web-gates.ps1` (must exit 0) and records scope-empty `.cs` gates verbatim as `SKIPPED (scope-empty)` (never `PASS`), while separate Chromium real-browser acceptance runs against real library layout fixtures (`<movie folder>\Subs\<video stem>-<code>.srt` folder copies configured externally with zero hardcoded machine paths in source per Constitution Principle VII) and compares screenshots against the ticket's visual references for the delivered shell; any missing fixture, reference, or runtime is `Could not run` (blocking acceptance, never substituted by unit tests or synthetic same-folder fixtures).
- **Implications**: The grill for DEV-503 is complete after Q6; Keel may now write `specs/DEV-503/brief.md`, `specs/DEV-503/CONCLUSIONS.md`, and `specs/DEV-503/ASSUMPTIONS.md` preserving all six exchanges and `[assumed]` tags.



---

## Phase 2 drafting clarification — 2026-11-12

Q7 settles the planning filename reserved by Q3. Extraction remains optional, with no scope expansion, new layer, dependency, hook or contract change. The original six-question grill remains closed; this clarification does not claim PLAN FROZEN or Gate 1. The filename deferral in the original Approach section is now resolved by Q7 below. Functionality-based English naming is governed by Constitution Principle VIII; the complete Patron answer retains its original citations verbatim.

## Q7 — Optional co-located CC panel filename

Keel: DEV-503 needs decision: Quill asks for the optional co-located CC panel filename authorized in brief Q3. Recommend web/src/features/playback/SubtitlePanel.tsx and subtitlePanel.test.tsx if accessibility or complexity warrants extraction; otherwise keep the panel in the approved existing files. Basis: brief Q3 and recon-DEV-503 lines 23-25 authorize one co-located pair while retaining the existing SubtitleSelector presentation and hooks; functionality-based English naming, no scope or dependency change. Please rule and cite basis; I will append the answer to brief.md and CONCLUSIONS.md before sending one analyze fix list to Quill.

Patron (complete answer):
Ruling: Yes — approve `web/src/features/playback/SubtitlePanel.tsx` and `web/src/features/playback/subtitlePanel.test.tsx` as the exact optional co-located CC panel component/test filenames if accessibility or complexity warrants extraction; otherwise keep the CC panel within the approved existing files (`[assumed]` for component/test naming).

- **Cited basis (`specs/DEV-503/CONCLUSIONS.md` Q3; `specs/DEV-503/brief.md` lines 48–58; `recon-DEV-503` lines 23–25)**: Q3 authorizes at most one small co-located panel component/test pair under `web/src/features/playback/` alongside `BrowserPlayer.tsx` and `SubtitleSelector.tsx` without introducing a new folder or architectural layer.
- **Naming & convention alignment (`recon-DEV-503` lines 23–25, 111; Constitution Principle IV)**: `SubtitlePanel.tsx` and `subtitlePanel.test.tsx` match the existing co-located playback naming pattern (`SubtitleSelector.tsx` / `subtitleSelector.test.tsx`) and functionality-based English naming rules.
- **Implications**: `plan.md` and `tasks.md` may reference `web/src/features/playback/SubtitlePanel.tsx` and `web/src/features/playback/subtitlePanel.test.tsx` solely as the optional extraction target inside the frozen Q3 file boundary, with zero scope, hook, contract, or dependency change.





## Q8 — Complete Patron answer

Ruling: Yes — approve all nine points of the Q8 coordinated correction (`1`–`9`), ratify adding `web/src/features/playback/playerCopy.ts` and `web/src/features/playback/subtitleCopy.ts` to the Q3 editable file boundary, ratify the `[assumed]` taste and accessibility verification details, require blocking T001 pickup verification of `media-chrome` 4.19.4 settings-menu exports/semantics, and confirm zero ticket contradiction or constitution departure.

- **Interaction, keyboard/focus, and accessibility alignment (Points 1–4, 6–7; `recon-DEV-503` lines 44–68, 194–202, 210; `specs/PRODUCT.md` §3–§4)**: Preserves Q1–Q2 scope/bar/caps while aligning Q4–Q7 with verified source facts: page-delivered `Escape` closes the open surface before application fullscreen-exit handling (`BrowserPlayer.tsx:106-145`) with a `fullscreenchange`-to-exited fallback that closes stale surfaces and restores focus to a connected trigger or player host (without claiming control over browser-owned `Escape`), movie-change restores focus only if inside the removed surface and unmount cleans listeners without focusing detached nodes; retains `MediaController noHotkeys` and `BrowserPlayer` bubble hotkey ownership (`BrowserPlayer.tsx:35-58,100-159`), using a non-modal labelled region for CC (never `role="dialog"`, which `:35-38` excludes) where controls consume their own activation/navigation keys via `defaultPrevented` while preserving `Space`/`K`, left/right 10s seek, `M`, and `F`; standardizes track `radiogroup`/`radio`/`aria-checked` (single tab stop, arrow move/select, `Space`/`Enter` activate) and `tablist`/`tab`/`tabpanel` (roving `tabindex`, `Left`/`Right`/`Home`/`End`, automatic activation), `composedPath` outside `pointerdown` (active surface + trigger as inside, trigger-to-trigger switch, fullscreen subtree boundary, ignoring `pointercancel` alone, single coordinated effect cleanup), existing CSS tokens/reduced-motion (`index.css:3-45`, `BrowserPlayer.css:88-92`), 320px/narrow-width wrapping without invented breakpoints, optional Q7 extraction without a hardcoded line threshold, distinct polite `role="status"` vs `role="alert"` (`BrowserPlayer.tsx:189-231`), and live media `ratechange`/transition speed indication without persistence (`[assumed]` for Palette labels `Subtitles`, `Settings`, `Close subtitles panel`, `Playback speed`, `x`-suffixed rates with `1x (Normal)`, Options Q5 copy, and ARIA/responsive micro-interactions).
- **Explicit §2.3 item 6 file-boundary addition, hook preservation, and duplicate-Play owner (Point 5; `recon-DEV-503` lines 56, 123, 190–197, 203–210; `lamuflix-team-charter` §2.3 item 6)**: Explicitly adds only `web/src/features/playback/playerCopy.ts` and `web/src/features/playback/subtitleCopy.ts` to the Q3 editable file boundary because existing player and subtitle copy constants live there (`recon-DEV-503` line 209), while keeping `useSubtitlePlayback.ts`, `subtitleContent.ts`, `subtitleTrackModes.ts`, `useBrowserPlayer.ts`, and generated `web/src/api/types.ts` strictly read-only, preserving existing `listGeneration`/`attemptRef` stale-result and object-URL cleanup (`recon-DEV-503` lines 203–206) without a mandatory `AbortController` rewrite, removing dead `<select>` CSS/tests while keeping behavior regressions, and removing the duplicate Play control where recon proved it lives (`BrowserPlayer.tsx:233-242` per `recon-DEV-503` line 195, remove-if-present at pickup while preserving the in-bar play button).
- **Blocking pickup verification of `media-chrome` 4.19.4, task/spec repairs, and zero escalation (Points 8–9; `recon-DEV-503` lines 51–53, 58, 66–69, 75, 78, 212–214)**: Because `web/node_modules/media-chrome` is absent in the spec worktree (`recon-DEV-503` line 214), exact `media-chrome` 4.19.4 settings-menu exports, import paths, attributes/events, and label customization remain explicitly unverified and must be verified by Wisp in T001 before production edits (stopping for recon/decision if any seam is unsupported rather than guessing imports or replacing `media-chrome`); correcting design/task assertions to match `recon-DEV-503` lines 190–214, making fullscreen control composition (T003), native-fullscreen keyboard track-to-Off verification (T016), and Bernstein/Rigger spec-PR opening before T000 explicit, and withholding Gate 1/runtime/merge claims neither contradicts ticket text (`recon-DEV-503` lines 34–90) nor departs from the constitution (`recon-DEV-503` lines 125–184).

