# DEV-503 — Phase 2 plan challenge adjudication

Date: 2026-11-12. Worktree: F:/Dev/LamuFlix.worktrees/feature-503-spec; branch feature/503-spec. Recon source HEAD: d64da227efbd75a78529e830edbdf51871cc4bf2. This reviews planning documents, not implementation or an immutable delivery SHA.

## Evidence and status

All required reports were read: shared notes findings-DEV-503-Sentry (Risk, 9 findings), findings-DEV-503-Ledger (Standards, 11), findings-DEV-503-Compass (Spec, 7). Decision authority: ticket in recon-DEV-503 28–90, PRODUCT/constitution excerpts 92–184, source-fact supplement 186–214, brief Q1–Q8 (Patron approved all nine Q8 points; complete answer appended to brief and CONCLUSIONS; taste logged in ASSUMPTIONS). No implementation-source reconnaissance was performed by Keel. Ledger source observations count only where independently established in recon; undocumented menu API claims do not become facts by repetition.

Status: **PLAN FROZEN**, signed Keel on 2026-11-12 after Q8, document repairs and independent validation. All accepted portions closed; zero unresolved above-bar planning findings. Round 1 of at most 2 plan-challenge rounds; at most 2 fix commits per round; no fix commits made in this repair. Existing analyze drafting repairs are not an extra challenge round. No code-review, Gate 1, runtime or merge clearance is claimed.

## Risk axis — Sentry

| ID | Disposition | Rationale and required closure |
|---|---|---|
| H1 | Accept, bounded | A page-level ordering promise needs a fallback. Q4 orders application Escape handling; recon establishes no browser-owned Escape guarantee. Clarify page-delivered Escape and add fullscreenchange exit cleanup/focus fallback in T012/T013 and native Chromium T016. Do not assert that Chromium invariably swallows Escape or that preventDefault controls browser exit. |
| H2 | Accept | Define active surface plus trigger as inside, composedPath pointer dismissal and fullscreen reachable boundary; another trigger switches once. Pointercancel alone is not outside activation. Cover retargeted paths, windowed/fullscreen, destination focus in T012/T013/T016. No new layer. |
| H3 | Partially accept; reject hook rewrite | Recon 203–206 establishes latest-wins attempts, stale URL revocation, Off release and movie/unmount cleanup already exist. Missing protection and mandatory AbortController claims are rejected. Add rapid A/B/Off/movie-change/unmount regression coverage through new list in T005/T012; retain hooks read-only, no cancellation redesign. |
| H4 | Partially accept; reject prescribed capture-phase playback handler | Recon 199–201 establishes noHotkeys and app ownership. Keep them, make consumption/defaultPrevented ordering explicit, and assert exactly one action plus preserved individual keys; a capture playback handler cannot rely on later controls setting defaultPrevented. |
| M1 | Accept, bounded | Define connected trigger/player fallback on closing/fullscreen exit and focus-inside-surface movie reset; unmount removes listeners and never focuses a detached host or steals destination focus. T012/T013 cover cleanup/repeated mounting. No new route-host edit. |
| M2 | Accept | Design lacks a narrow-layout strategy despite 320px binding. Q8 ratifies wrapped controls, seek row, bounded scrollable overlay, retained volume reachability; T003/T007 implement, T016 proves actual layout. Do not pretend RTL proves CSS width. |
| M3 | Accept | T001 pickup determines owner and DEV-493 overlap; T003 removes only if present and T002 asserts in-bar play remains. Recon 195 locates incumbent duplicate inside BrowserPlayer, so do not force deletion in MovieDetailsView. |
| M4 | Partially accept; reject forced 1x | Actual media rate is authoritative (Q5); ratechange/source/movie/metadata transitions must resynchronize selected indication in T009/T010/T012. Do not assume every source change resets rate or impose a saved/default preference. |
| L1 | Accept | Real motion is Chromium evidence. Remove computed CSS/motion claims from RTL T012; preserve roles/interaction tests, extend incumbent reduced-motion block (recon 210), prove internal/native menu behavior in T016. |

## Standards axis — Ledger

| ID | Disposition | Rationale and required closure |
|---|---|---|
| S1 | Accept | Recon 195–197 proves polite status and separate alert behavior. Correct plan/design/task invariant to preserve each role and live semantics, without calling alert polite or adding new roles to old status output. |
| S2 | Accept | Recon 200 confirms dialog ancestors suppress hotkeys. Q8 selects non-modal labelled region, retains editable exclusions, tests consumed versus unconsumed keys on tab/row/close and elsewhere. Do not blanket suppress all panel key events. |
| S3 | Accept | Mixed listbox/radio wording is ambiguous. Q8 fixes radiogroup/radio/aria-checked and roving keyboard behavior plus tabs; retain verified native media-chrome speed pattern. Update FR-019, design, T005/T007/T012. No guessed native menu role becomes a fact. |
| S4 | Accept, subject to cited Q8 boundary ruling | Recon 209–210 establishes playerCopy/subtitleCopy and existing select CSS. Add only those two copy files to editable boundary, route new copy through them, remove dead select CSS/test selectors without losing behavioral tests. No global index.css edit. |
| S5 | Accept | Name exact T013 files; hooks, subtitleTrackModes, content helpers, generated types stay read-only. Any additionally necessary edit requires cited decision before work. |
| S6 | Partially accept; reject asserted API spellings | Recon 212–214 verifies manifest version, not installed exports/import path/state APIs. T001 must verify official 4.19.4 exports, rates/labels/native semantics/open-state/anchor/fullscreen composition before edits; stop on unsupported seams. Do not hard-bind Ledger speculative exports or custom versus native trigger claims without documentation. |
| S7 | Partially accept; reject unnecessary implementation prescription | Define composedPath boundary, one authoritative local coordination model synchronized to verified native menu state and effect cleanup. No state library. No need to put every activation listener at document level or create one effect per surface. T012/T013 verify behavior/cleanup. |
| S8 | Accept | No explanatory comments applies to test and production code. Conventions cite only AGENTS exemptions. |
| S9 | Reject as preference; noted, no ticket | Optional extraction was explicitly settled by Patron Q3/Q7. A line-count/size threshold has no authoritative basis and would add a hardcoded gate. Keep existing optional pair; normal accessibility/complexity gates still apply. |
| S10 | Accept | Same closure as Sentry L1; visible focus appearance, 320px layout and media-query behavior belong to Chromium, RTL covers semantic focus movement and roles. Extend existing reduced-motion block, do not duplicate it. |
| S11 | Partially accept | Use incumbent tokens (recon 210), obtain references via T015, verify native labels/customization at T001 before enforcing Q8 copy. Hardcoded color prose is not evidence of a harness threshold violation; reject that rationale. Availability and exact labels remain unverified until recon/pickup. |

## Spec axis — Compass

| ID | Disposition | Rationale and required closure |
|---|---|---|
| M1 | Accept rationale gap; existing flag confirmed | Recon 201 establishes noHotkeys and app key set. Explain ownership in design and verify each preserved key (including left/right 10 seconds), consumed keys and single-action behavior in T012. Do not remove incumbent flag. |
| M2 | Partially accept; some facts now established | Recon 191–192 establishes endpoints/DTO fields/content URL helper module; retain generated contracts with updated citations. Recon 195 establishes loading/status/error actions, including optional Open Local action, not invented Open in BS Player copy. Correct design action/helper assertions; omit unsupported shipped-on-main lineage. T001 explicitly checks drift. No contract delta. |
| M3 | Accept authority gap | Q8 must ratify and log taste/a11y specifics before freezing. Preserve ticket shell and existing Q5 copy; distinguish design annotations from rendered copy and browser-proof requirements from RTL assertions. |
| L1 | Accept | T003 composes real MediaFullscreenButton, no fullscreen placeholder. |
| L2 | Accept | T016 expressly performs keyboard track then Off in both windowed and native fullscreen with rendered/hidden caption evidence. |
| L3 | Accept | Declare wireframe placeholder/status/future-space annotations non-rendered; no audio/quality placeholder entries or fake actions. |
| L4 | Accept traceability clarification | Brief already requires opening spec PR. Name Bernstein coordination/Rigger preparation-opening as producer before Keel T000 verified user merge receipt. No new Build task before Gate 1; only user merges. |

## Closure and freeze bar

27/27 dispositions: 19 accepted, 7 partially accepted, 1 rejected. Partial acceptance requires the bounded repair above; rejected subclaims do not waive the accepted gap. Duplicate findings share repairs but retain independent dispositions. No out-of-scope broken behavior/follow-up ticket is established; S9 preference is noted, no ticket.

Closure completed: complete Patron Q8 ruling appended to brief and CONCLUSIONS; taste logged in ASSUMPTIONS; Palette repaired design; Quill applied one numbered repair list to owned artifacts; Keel independently reread and validated consistency/coverage against brief and recon. Residual design diagram/status/reference wording and spec US4 consumption-gating/T013 fallback wording were independently caught, repaired within the same round, and reread. Unverified media-chrome APIs remain a STOP-before-production T001 prerequisite, never a verified fact or a fabricated public contract. Any newly necessary ticket change or constitution departure follows the owner-checkbox rule.

## Independent read-only validation and freeze receipt

Keel validated final spec.md, plan.md, tasks.md and design.md against brief Q1–Q8, CONCLUSIONS, ASSUMPTIONS, all three reports and recon-DEV-503 including 186–214. Spec Kit prerequisite discovery returned FEATURE_DIR specs/DEV-503 and tasks.md successfully (exit 0); there are no registered before_analyze/after_analyze hooks. This validation used recon-supplied source facts and constitution excerpts, without implementation-source searches. Repairs were authorized separately from the read-only analysis.

| Requirement / criterion | Task evidence |
|---|---|
| FR-001–FR-002 | T002–T004 composition and real fullscreen; T003 responsive foundation; T016 real current/duration and layout |
| FR-003–FR-004 | T006–T008 persistent CC and panel; T012–T014 interaction; T016 runtime |
| FR-005 | T005/T007/T008 list semantics, track/Off and stale-result regression; T012 keyboard; T016 captions windowed/fullscreen |
| FR-006 | T006–T008 honest Options shell; T016 shell comparison |
| FR-007 | T001 verified API prerequisites; T009–T011 seven rates/live state; T012 transition regressions; T016 actual rate |
| FR-008 | T001 owner/overlap; T002/T003/T004 remove-if-present and retained in-bar play |
| FR-009–FR-012 | T007 fullscreen subtree; T009–T014 mutual exclusion/event/focus/hotkey/fallback work; T016 native behavior |
| FR-013–FR-014 | T005–T008 failure/stale lifecycle preservation; T012–T014 reset, focus and cleanup |
| FR-015 | T001 inventory/STOP; explicit editable/read-only boundaries and copy modules in T003/T007/T010/T013; no new dependency/API |
| FR-016 | T002/T005/T006/T009/T012 tests and T004/T008/T011/T014 green checkpoints; existing suites retained |
| FR-017 | T015 prerequisites; T016 required real-layout Chromium receipts; missing inputs leave acceptance open |
| FR-018–FR-019 | T003/T007/T010 delivered design; T005/T006/T012 semantic/focus tests; T016 actual layout/focus appearance/motion; Q8 logged authority |
| SC-001 | T002–T004 and T016 real movie time |
| SC-002 | T005–T008/T012–T014 and explicit T016 keyboard track-to-Off in windowed/native fullscreen |
| SC-003 | T009–T011/T012/T016 live speed state |
| SC-004 | T012–T014/T016 each preserved shortcut and consumed-key single action |
| SC-005 | T015/T016 required evidence and honest Could not run; acceptance remains open if unavailable |
| SC-006 | T017 applicable web gates; honest non-PASS scope-empty reporting |

Metrics: 19 FRs; 6 SCs; 20 unique tasks T000–T019, all unchecked; 100% requirement/criterion task coverage. Every task maps to delivery, verification or explicit pipeline prerequisites (T000/T001/T018/T019). No unmapped implementation task, unresolved cross-artifact ambiguity, duplicate requirement or recon-established constitution conflict remains within this analysis scope. S9 is intentionally rejected under Q7, not an unresolved finding. All 26 accepted/partially accepted findings have their accepted portions closed by the repairs above.

**Freeze decision: PLAN FROZEN.** Keel recorded the decision in brief before propagating status-only changes to the four artifacts. Drafting receipt/checklist references to validation pending are historical, not current blockers. No new code-review round or immutable PR findings publication is represented by this plan artifact.

**Next gate: ordinary owner Gate 1 remains pending.** Bernstein coordinates and Rigger prepares/opens the spec PR; only the user merges; Keel T000 verifies the merged receipt. T001 fresh recon and documented media-chrome 4.19.4 API verification remain mandatory before production. Runtime reference/fixture availability is unverified; T015/T016 are required later, not passed. No production code, tests, runtime gates, commit, PR opening or merge was performed by Keel here. Existing harness.yml and scratch-file changes were preserved.

## Consolidated repair list — challenge round 1

Dispatch only after Q8 is ruled and appended to brief. This is one list; repeated findings are not extra rounds. Quill owns spec.md, plan.md, tasks.md, checklists/requirements.md and DRAFTING_RECEIPT.md. Palette owns design.md and must align it to the same Q8; neither writes brief/other owners. Keep statuses awaiting Keel validation until independently checked.

1. **Source authority (Compass M2; Ledger S6/S11)**: Incorporate recon 186–214, correct unsupported lineage/helper/action claims, distinguish manifest pin from unverified installed exports and reference availability. T001 inventories concrete generated routes/fields/helper module, current roles/actions, noHotkeys/guard, copy/CSS and stale-result ownership; STOP on drift. Exact menu export/import/props/state/label semantics must be documented for 4.19.4 at T001 before production, not guessed from challengers. No dependency change.
2. **Boundaries (Ledger S4/S5/S8/S9; Sentry H3)**: Propagate Q8 playerCopy.ts/subtitleCopy.ts approval to all file lists/FR-015/T001/T003/T007/T010/T013 as appropriate. Name T013 files explicitly: BrowserPlayer.tsx, BrowserPlayer.css, SubtitleSelector.tsx, approved copy files, optional Q7 panel pair, and bounded MovieDetailsView integration with existing named tests. Hooks, subtitleTrackModes.ts, subtitleContent.ts and generated types are read-only. Remove explanatory-comments production-only exception. Optional extraction remains Q7, no invented threshold. No mandatory AbortController/hook rewrite.
3. **Accessibility pattern (Ledger S1–S3; Compass M1/M3)**: Non-modal labelled region, consistent radiogroup/radio/aria-checked, Off, arrows/Space/Enter and one tab stop; tablist/tab/tabpanel with Left/Right/Home/End, roving tabindex and automatic activation. Native media-chrome speed pattern is verified, not rewrapped. Preserve status polite versus alert semantics. Update scope/scenarios/FR-019/plan/T005/T007/T012 and design, not just an appendix.
4. **Event coordination (Sentry H2/H4; Ledger S2/S7; Compass M1)**: Keep noHotkeys and app-owned bubbling hotkeys with control-first/defaultPrevented handling; consumed activation/navigation yields one action. T012 explicitly covers each existing shortcut incl left/right 10-second seek, tabs/rows/close/speed and elsewhere. One local open-surface coordinator reconciles verified native menu state. composedPath outside boundary is active surface+trigger; other trigger switches once; reachable fullscreen outside region dismisses; pointercancel alone does not. T013 names effect cleanup. Do not mandate capture-phase playback or guessed native state props.
5. **Escape/lifecycle/subtitles (Sentry H1/H3/M1/M4)**: Everywhere scope/scenarios/FR-011/brief interaction is referenced, distinguish page-delivered Escape-before-app-exit from browser-owned exit; fullscreenchange-exited fallback closes surfaces with connected-trigger/player fallback. Movie change restores only focus lost from its surface; unmount never focuses detached nodes/steals destination focus, and cleans subscriptions. T005/T012 cover rapid track A/B/Off, movie-change-during-fetch, stale resolve and URL release via unchanged hook behavior; T013 preserves contracts. T016 records actual native Escape outcome and closure. No unsupported browser-event guarantee.
6. **Responsive/visual evidence (Sentry M2/L1; Ledger S10/S11; Compass M3/L3)**: Propagate Q8 ratified copy and [assumed] authority. Implement token-based wrapped strip/seek row and bounded scrollable bottom-right overlay retaining volume and every control in T003/T007. Wireframe annotations are never rendered. Extend incumbent reduced-motion block. Remove all RTL computed focus-ring/media-query/320px layout proof claims; T012 asserts roles/state/focus movement/interaction; T016 alone proves CSS appearance, 320px reachability and actual reduced motion including native menu internals. Make test tables/outcomes consistent.
7. **Rate state (Sentry M4; Ledger S6/S11)**: T009/T010/T012 explicitly verify selected rate matches real media on ratechange, source/movie and metadata changes; no forced 1x or persistence. T001 verifies supported label/rate/open-state composition; unsupported customization stops for decision rather than custom menu/library substitution. Keep seven ticket rates and Q8 copy binding.
8. **Cleanup/runtime precision (Sentry M3; Compass L1/L2)**: T002 asserts exactly one in-bar play and no duplicate below; T003 remove-if-present at owner confirmed by T001, with real fullscreen control (no fullscreen placeholders), and dead select CSS/test paths removed while behavior remains tested. Recon currently locates duplicate in BrowserPlayer, not necessarily MovieDetailsView. T016 explicitly keyboard-selects track then Off with visible/hidden captions both windowed and native fullscreen.
9. **Gate provenance/status (Compass L4; all findings)**: Name Bernstein coordination/Rigger preparation-opening as the spec-PR producer before T000 verified user-merge receipt. Do not add Build work before Gate 1. Update drafting/checklist receipt to these final repairs, preserving historical records. Link PLAN_ADJUDICATION.md. Leave PLAN FROZEN withheld until Keel checks all repairs; no Gate 1/implementation/runtime/merge claim and no production edits/commits/PR opening in this repair ask.

