# DEV-569 grill conclusions

## Q1 — Palette design

### Keel
[from Keel] /grill-with-docs DEV-569
Q1 of at most 12: Does DEV-569 need a Palette design.md? I recommend no. Wisp confirmed the only tag is size:M (recon-DEV-569 line 65), and the ticket describes one adjacent control with explicit range, reset and keyboard behavior (lines 42–49). The implication is that Keel records the control interaction and tests in the brief and Quill can draft without a Palette handoff. If you require design.md, please rule on whether a ui: tag change is needed. I will append the complete exchange to specs/DEV-569/CONCLUSIONS.md in the named worktree. Please answer Q1 only and cite the basis.

### Patron
Q1 ruling: No - DEV-569 does not need a Palette design.md and needs no ui: tag change; record the control interaction and tests in brief.md and have Quill draft directly.
Basis: task-pipeline section 0 line 16 and Phase 2 step 4 line 84 scope Palette design.md to ui: tickets; recon-DEV-569 line 65 confirms tags are size:M only (no ui: tag); recon-DEV-569 lines 42-49 (authoritative per specs/PRODUCT.md section 3 lines 23-24) specify one adjacent boost control inside BrowserPlayer with no new routes, layout wireframe, or API data needs.

---

## Q2 — Unity and limiting

### Keel
[from Keel] DEV-569 Q2 of at most 12: How should we reconcile the ticket graph and the unity acceptance? I recommend native playback until the first deliberate interaction, then keep the once-bound source → gain → compressor → destination graph for the element, with unity gain and a transparent compressor at 100% (including reset). Enable limiting only above 100%; verify unity against native and approximately 2× output at 200% on the same calibrated quiet signal below compression onset. This preserves the fixed graph, catches an audible reset regression and allows limiting on loud material. Basis: recon-DEV-569 lines 41, 44–45 and 53–54. Please rule on Q2 only.

### Patron
Q2 ruling: Approved - keep native playback until first deliberate boost interaction, then retain the once-bound source -> GainNode -> DynamicsCompressorNode -> destination graph with unity gain and a transparent compressor at 100% (including reset), engaging limiting only above 100% and verifying 100% unity vs 200% (~2x) on a calibrated quiet signal below compression onset.
Basis: recon-DEV-569 lines 41 and 44-45 require lazy AudioContext creation and a permanent once-per-element four-node chain; recon-DEV-569 lines 53-54 require no audible or measurable processing at 100% and ~2x output at 200%, which parameterizing the compressor as no-op at 100% and limiting only above 100% satisfies without contradicting ticket scope (specs/PRODUCT.md section 3 lines 23-24).

---

## Q3 — Stream eligibility and source lifetime

### Keel
[from Keel] DEV-569 Q3 of at most 12: How should boost eligibility, once-per-element binding and native fallback work together? I recommend establishing that the effective stream is same-origin before attaching a source, and verifying real movie URLs and redirects in Chromium before Build relies on that eligibility. Hide boost for unavailable Web Audio or cross-origin/unverified sources; initialize/resume on an explicit click or keyboard activation, and only bind after a successful resume. Preserve one binding per actual element through re-render, remount and HMR, with no duplicate connections; use a fresh native-playing element if an element already bound to the graph must switch to an ineligible source. Release or reuse graph resources safely without closing a context for a still-live bound element. The cost is explicit source-transition and failed-activation tests. Basis: recon-DEV-569 lines 11–12 and 41, 44–48. Please rule on Q3 only, including whether actual origin verification is needed before drafting or can be an explicit pre-implementation check.

### Patron
Q3 ruling: Approved - gate boost on Web Audio availability plus verified same-origin effective stream (hide when unavailable, cross-origin, or unverified), lazily create/resume AudioContext on explicit click or keyboard activation and bind createMediaElementSource once per element only after resume succeeds (keeping live bound contexts open and remounting a fresh native video element if a bound element switches to an ineligible source); live Chromium stream-origin/redirect verification is NOT required before Phase A drafting and shall be an explicit pre-implementation check in tasks.md (blocking Build if real library streams are not same-origin).
Basis: recon-DEV-569 lines 41 and 44-48 mandate lazy activation, once-per-element guard across re-render/remount/HMR, keyboard support, and hiding boost with untouched playback when Web Audio is unavailable or stream is not same-origin; lamuflix-team-charter lines 10-12 and recon-DEV-569 lines 47 and 56 place live real-movie Chromium verification in the Build workspace rather than blocking Spec drafting.

---

## Q4 — Remembered boost and activation

### Keel
[from Keel] DEV-569 Q4 of at most 12: What should restoring a saved boost after reload mean before browser activation? I recommend treating the valid saved percentage (100 through 300) as the remembered setting immediately, explaining when it needs activation, and leaving actual audio native until the eligible control is explicitly used. Opening/focusing alone must not attach the graph. On activation apply the remembered level unless the gesture sets a new level or resets to 100%. Default malformed, missing, nonfinite or out-of-range storage to 100%; failed reads/writes never stop playback or in-session adjustment. Use one namespaced browser-only localStorage key, no account or backend state. This preserves persistence and the autoplay requirement. Basis: recon-DEV-569 lines 42, 44, 46, 55 and 57. Please rule on Q4 only.

### Patron
Q4 ruling: Approved - restore a valid saved boost (100-300) on reload from one namespaced browser-only localStorage key as the remembered UI setting immediately while keeping audio native until explicit control activation (not focus/open alone), clearly indicating pending activation when >100% [assumed], applying the remembered level on activation unless that gesture sets a new value or resets to 100%, defaulting missing/malformed/nonfinite/out-of-range storage to 100%, and isolating storage read/write errors from playback and in-session changes.
Basis: recon-DEV-569 lines 42, 44, 46, 55, and 57 require 100-300% range with one-click reset, lazy AudioContext creation/resume on control interaction for browser autoplay policy, browser-only localStorage that never breaks playback on failure, reload restoration, and storage-fallback unit tests; specs/PRODUCT.md section 4 lines 28-30 cover the [assumed] pending-activation indicator micro-interaction.

---

## Q5 — Control interaction and keyboard behavior

### Keel
[from Keel] DEV-569 Q5 of at most 12: How should the control behave in the existing player row? I recommend a separately labelled Volume boost range beside the stock volume slider with a visible percentage, a 1 percentage-point keyboard step, and a Reset boost button that sets 100% in one action. Treat label, step and micro-interaction as taste. Use native range semantics with accessible percent value text and visible focus; when the range has focus its arrows adjust boost without also triggering the player shortcut, and all existing hotkeys keep their normal scope elsewhere. Keep the control in the same fullscreen-enabled player subtree and preserve stock mute/volume behavior in both modes. Basis: recon-DEV-569 lines 11, 14–15, 42–43 and 48–49 and specs/PRODUCT.md section 4. Please rule on Q5 only.

### Patron
Q5 ruling: Approved - render a native range control labelled Volume boost (100-300%, 1 percentage-point step) with visible percentage text, accessible percent valuetext, visible focus, and a one-click Reset boost button beside MediaVolumeRange inside the existing fullscreen-enabled player control row, consuming arrow keys when focused without double-firing player hotkeys while preserving Space/K, arrows, M, F elsewhere and stock mute/volume multiplication; treat Volume boost / Reset boost labels, 1% step, and micro-interaction as [assumed] taste.
Basis: recon-DEV-569 lines 11, 14-15, 42-43, and 48-49 require an adjacent 100-300% boost control with displayed value, one-click reset, preserved media-chrome mute/volume, arrow-key adjustment plus intact player hotkeys, and fullscreen/windowed parity; specs/PRODUCT.md section 4 lines 28-30 govern the [assumed] label, step, and micro-interaction choices for specs/DEV-569/ASSUMPTIONS.md.

---

## Q6 — File scope and delivery evidence

### Keel
[from Keel] DEV-569 Q6 of at most 12: Do you approve this delivery envelope and evidence sequence? I recommend targeted edits to BrowserPlayer.tsx, useBrowserPlayer.ts and BrowserPlayer.test.tsx, plus playerCopy.ts and BrowserPlayer.css only for necessary labels/style. Keep the implementation in the existing player seam, with existing web test tooling, no new dependency, API, storage beyond localStorage, or LocalPlay edits. Gate 1 and Phase B recon plus Chromium origin/redirect proof come before Build. Cover gain math, lazy/resume failure, once-per-element reuse including rerender/remount/HMR, storage failure/reload, mute and stock volume, keyboard, reset and ineligible/native fallback in web tests. Use calibrated quiet-signal AnalyserNode evidence at unity and 200% with fixed stock volume, a loud-signal limiting check, and the required real-movie Chromium screenshot at 200% plus analyser level reading and fullscreen/windowed behavior. Use a test/receipt seam without shipping instrumentation and require applicable web gates. Basis: recon-DEV-569 lines 11–22 and 41–57, task-pipeline Phase 3. Please rule on Q6 only.

### Patron
Q6 ruling: Approved - scope targeted edits to BrowserPlayer.tsx, useBrowserPlayer.ts, and BrowserPlayer.test.tsx (plus playerCopy.ts and BrowserPlayer.css only as needed for labels/styling) within the existing playback seam with zero new dependencies, layers, schema, API contracts, storage beyond browser localStorage, or Features:LocalPlay/secrets/Process.Start changes; sequence Gate 1 -> Phase B recon + live Chromium stream-origin/redirect check (via Test URLs portal) -> Build -> web tests, calibrated quiet-signal unity/200% AnalyserNode + loud-signal limiting checks, real-movie 200% Chromium screenshot + analyser reading + fullscreen/windowed check via a non-shipping test/receipt seam, and Phase 3 step 5 gates (run-web-gates.ps1 exit 0; scope-empty C# gates recorded SKIPPED (scope-empty)).
Basis: recon-DEV-569 lines 11-22 and 41-57 establish the five playback files as forced by ticket scope/ACs with no section 2.3 items 1-5 triggered (specs/PRODUCT.md section 5 lines 41-48; lamuflix-team-charter lines 18-24); task-pipeline Phase 3 step 5 lines 102-112 govern web gate execution and scope-empty SKIPPED reporting, and test-portal-policy line 2 governs local Test URLs portal usage for Chromium verification.

---

## Q7 — Review closing bar and grill closure

### Keel
[from Keel] DEV-569 Q7 of at most 12: What review bar closes this Size M grill and delivery loop? I recommend frozen scope as Q1–Q6 and the ticket lines 39–57, with anything else a follow-up issue, not a finding in this round. Require Sentry Risk, Ledger Standards and Compass Spec reports, explicit disposition of each finding, and independent reviewed-head verification for every accepted in-scope finding from Critical through Low. Cap each repair loop at two rounds: report any unresolved blockers to Bernstein at cap and keep the applicable gate closed. Require the web gate, Chromium receipts and ordinary owner Gate 1 at their scheduled stages. Route ticket scope or acceptance changes and necessary constitution departures as section 2.3 owner checkboxes only; no such request identified here. Basis: DEV-569 size:M, task-pipeline and charter review discipline. If approved and you see no remaining decision, please confirm the grill can close for Quill drafting.

### Patron
Q7 ruling: Approved with one charter precision on out-of-scope items - freeze scope to Q1-Q6 and recon-DEV-569 lines 39-57; out-of-scope items are excluded from in-round findings, with YouTrack follow-up tickets filed (via Patron and Rigger after checking open issues) ONLY for Critical/High, broken behaviour, or user-requested items and all others logged in specs/DEV-569/CONCLUSIONS.md as noted, no ticket; require all three axes (Sentry Risk, Ledger Standards, Compass Spec), explicit disposition plus independent reviewed-head verification of accepted Critical-through-Low findings, a 2-round repair cap (unresolved blockers reported to Bernstein at cap with gate closed), scheduled Phase 3/5 gates, Test URLs portal Chromium receipts, ordinary owner Gate 1, and zero section 2.3 owner checkboxes. No remaining decisions exist - the grill is approved to close for brief.md, CONCLUSIONS.md, ASSUMPTIONS.md ([assumed] taste items from Q4-Q5), and Quill drafting.
Basis: DEV-569 size:M tag (recon-DEV-569 line 65), task-pipeline section 2.2 lines 28-30, Phase 2 steps 3-7 lines 80-87, and Phase 4 steps 1-4 lines 118-124, plus lamuflix-team-charter lines 25-28 and 38 (follow-up ticket bar and section 2.3 escalation bar).

---

## Plan challenge — Enforceability clarifications (Sentry F1-F4, Ledger F1-F7)

### Keel
[from Keel] DEV-569 plan challenge ruling requested. Sentry F1-F4 and Ledger F1-F7 expose enforceability gaps inside Q3/Q5/Q6. Recommended bounded clarification package (1)-(4) covering delivery-invariant eligibility + T001 media/range/retry redirect proof, pre-source node preparation + unbound preservation / fresh-element recovery with same-media continuity, Test URLs portal session-only compressor-output measurement with zero production measurement exports, and Q6 envelope retention + existing keyboard/storage conventions + module-reload same-element reinitialization and controllable AudioContext doubles.

### Patron
Verdict: Approved — clarifications (1)-(4) are ratified as bounded enforceability refinements inside Q2-Q6 that preserve DEV-569 Scope/Acceptance and Constitution v2.0.0 with zero §2.3 owner checkboxes (ordinary owner Gate 1 still applies).
- **Eligibility, atomic bind, and remount continuity ((1)-(2))**: Restricting eligibility to recognized direct same-origin HTTP(S) library media loads backed by an existing delivery invariant verified read-only in T001 (including Chromium media and Range/retry redirect chains, stopping Build if unestablished within scope), re-checking per source generation and immediately before bind with fail-closed native fallback, preparing downstream `GainNode -> DynamicsCompressorNode -> destination` nodes before `createMediaElementSource`, preserving the existing element when demonstrably unbound, and recovering via a fresh native element on partial/uncertain ownership or bound-to-ineligible transitions (reusing attach/detach, controls, and subtitle wiring with same-media state carry-over and normal new-media source-change semantics) enforces ticket Scope without altering the mandated 4-node graph (`recon-DEV-569:11-12,41,43-47`; `specs/DEV-569/brief.md` Q2-Q3; `findings-DEV-569-Sentry` F1-F3; `findings-DEV-569-Ledger` F1, F4).
- **Non-shipping measurement, Q6 envelope, and conventions ((3)-(4))**: Observing actual compressor output via `Test URLs` portal test-session-only evaluation with an independent unbound native baseline (proven at Phase B pickup or reported `blocked`) while forbidding production measurement hooks/exports, keeping edits inside the Q6 five-file `web/src/features/playback/` envelope, reusing existing `BrowserPlayer.tsx` keyboard-exclusion and `subtitlePreferences.ts`-style namespaced `localStorage` error-isolation conventions (without editing `subtitlePreferences.ts`), and requiring same-element module-reload reinitialization plus controllable `AudioContext` doubles while leaving internal guard/remount mechanics to Build satisfies all acceptance checks cleanly (`recon-DEV-569:11-18,45-57`; `specs/DEV-569/brief.md` Q4-Q6; `test-portal-policy:2`; `findings-DEV-569-Sentry` F4; `findings-DEV-569-Ledger` F2-F3, F5-F7).
- **Charter §2.3 and Constitution compliance**: No new dependency, project/layer, schema, public API contract, unnamed tracked-file rewrite, or `Features:LocalPlay`/secrets/`Process.Start` surface is introduced, and neither ticket Scope/Acceptance nor Constitution v2.0.0 is departed from, so zero §2.3 owner checkboxes are added (`recon-DEV-569:19-22,39-57`; `.specify/memory/constitution.md` Principles VI-VII lines 680-746 and Web stack row line 825).

