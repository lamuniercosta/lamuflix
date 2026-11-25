# DEV-569 — Grill brief

**Add volume boost above 100% to the web player** · Size M · Parent DEV-100

**Status** — PLAN FROZEN after the three-axis plan challenge and independent Spec Kit analysis. Ready for the ordinary Gate 1 process. See the freeze receipt in CONCLUSIONS.md.

**Planning base** — `feature/569-spec` at `6fb9c3799ac954dbb3f8f43262806de9956e22f1` in `F:/Dev/LamuFlix.worktrees/feature-569-spec`.

**Authority** — DEV-569 Scope and Acceptance in `recon-DEV-569` lines 39–57, `specs/PRODUCT.md` §§3–5, and the [complete Patron exchange](CONCLUSIONS.md). The [taste assumptions](ASSUMPTIONS.md) capture Q4 and Q5.

## Frozen scope

- Add one boost control beside the stock Media Chrome volume slider in the existing browser player, with 100–300%, a visible current percentage, and a one-action reset to 100%. Keep stock volume and mute working, preserve player hotkeys, and support windowed and fullscreen playback.
- For eligible streams, lazily attach a Web Audio graph to the video element on explicit activation. Use a media element source, a gain node, a dynamics compressor, and the destination. Preserve native playback before activation and for ineligible streams. Bind once per element across re-render, remount, and hot reload.
- Remember the setting on the device using browser localStorage. Verify the same-origin stream in Chromium and collect the ticket’s real-movie screenshot and analyser reading. Scope is limited to the playback files below and non-shipping test evidence. Anything else is a follow-up issue, not a finding in this round.

## Grill answers

1. **Palette** — No `ui:` tag change or `design.md`. The ticket has only `size:M` and specifies one control in the existing player row. Quill uses this brief. Basis is recon lines 42–49 and 65 and task-pipeline Phase 2 step 4.
2. **Unity and limiting** — Keep native audio until deliberate boost interaction. Once bound, retain the mandated source → gain → compressor → destination chain. At 100%, including reset, use gain 1 and a transparent compressor. Engage limiting only above 100%. Verify native versus active unity, then measure about 2× amplitude at 200% on a calibrated quiet signal below compression onset. Basis is recon lines 41, 44–45 and 53–54.
3. **Eligibility and lifecycle** — Hide boost when Web Audio is unavailable or the effective stream is cross-origin or unverified. Start or resume the context on explicit click or keyboard activation. Bind only after resume succeeds. Retain one graph per actual element and keep a live bound context open. If a bound element switches to an ineligible source, use a fresh native video element. Verify real stream origin and redirects before implementation in Phase B. A cross-origin result blocks Build. Basis is recon lines 41, 44–48 and 56.
4. **Remembered setting** — Use one namespaced localStorage key. Restore a valid 100–300% setting immediately in the UI and indicate when a remembered value above 100% awaits activation [assumed]. Audio stays native until the user activates the control. Focus or opening alone does not activate it. Apply the remembered value on activation unless that gesture adjusts or resets it. Default missing, malformed, nonfinite, or out-of-range data to 100%. Failed reads or writes leave playback and in-session adjustment usable. Basis is recon lines 42, 44, 46, 55 and 57 and PRODUCT §4.
5. **Interaction** — Use a native range labelled “Volume boost” and a “Reset boost” button [assumed]. Use a one percentage-point step [assumed], a visible percentage, accessible percent value text, and visible focus. Arrow keys on the range adjust boost without also firing a player shortcut [assumed]. Space/K, arrows, M, and F retain their usual scope elsewhere. Keep the controls inside the existing fullscreen player and multiply boost on top of stock volume and mute. Basis is recon lines 11, 14–15, 42–43 and 48–49 and PRODUCT §4.
6. **Delivery** — Use the existing player seam and approved file scope below. Use the Test URLs portal for Chromium preflight and the real-library receipt, with a non-shipping test or receipt seam for measurement. Require the web gate to exit 0. Record scope-empty C# gates as `SKIPPED (scope-empty)` when applicable. Basis is recon lines 11–22 and 41–57, task-pipeline Phase 3 step 5, and test-portal-policy line 2.
7. **Close and review** — Patron approved closure for Quill drafting. Require the three Size M review axes, explicit dispositions, independent reviewed-head verification, and at most two repair rounds. Apply the follow-up rule below. Ordinary owner Gate 1 applies with zero structural owner checkboxes. Basis is recon line 65, task-pipeline §2.2 and Phases 2 and 4, and lamuflix-team-charter lines 25–28 and 38.

## Approach and files

| File under `web/src/features/playback/` | Scope |
| --- | --- |
| `BrowserPlayer.tsx` | Extend the existing control row, retain Media Chrome, and connect eligibility, activation, reset, and source transitions to the player. |
| `useBrowserPlayer.ts` | Own the element-bound graph and gain, context activation and failure handling, and browser-local remembered state in the existing player seam. Keep one binding through re-render, remount, and hot reload. |
| `BrowserPlayer.test.tsx` | Exercise graph lifecycle, storage, and player interactions with the existing web test tooling. |
| `playerCopy.ts` | Add necessary control labels or pending-activation text. |
| `BrowserPlayer.css` | Add only styling needed for placement or focus. |

Use the stock media element’s volume and mute as inputs to the existing playback path. Boost scales that output by percentage divided by 100. Keep source initialization guarded per actual element, including repeated activation, with no duplicate connections. When resume fails before binding, leave native playback usable. Preserve stock playback and LocalPlay gating. The approved envelope covers targeted edits to the five listed files, with no dependency, layer, API, schema, secrets, or process-launch changes. Ask for a cited Patron ruling before any out-of-envelope edit.

## Test strategy and acceptance evidence

| Evidence | Required checks |
| --- | --- |
| Web tests for gain and activation | 100%, 200%, and 300% map to 1, 2, and 3. Reset restores unity. No source is bound before activation or after failed resume. Repeated activation, re-render, remount, and hot reload preserve a single source and graph per element. A bound element switching to ineligible media uses native playback via a fresh element. |
| Web tests for persistence and interaction | Restore a valid setting on reload with pending activation above 100%, apply it on explicit activation, and cover invalid values plus throwing storage reads and writes. Cover missing Web Audio and cross-origin or unverified sources, range accessibility and keyboard isolation, stock mute and volume, and reset. |
| Chromium analyser measurement | Compare native baseline to active/reset 100% at fixed stock volume. Verify 100% sounds and measures the same as native. With the same calibrated quiet signal below compression onset, record gain value and about 2× analyser amplitude at 200%. Demonstrate limiting with loud material above unity and confirm mute silences boosted audio. Use a non-shipping measurement seam. |
| Chromium real-library receipt | Via the Test URLs portal, play a real library movie, boost to 200%, and capture the visible control and an analyser level reading. Check fullscreen and windowed operation. Preserve the evidence for review. |

## Task order and gates

1. Quill drafts from this brief, the cited recon, and PRODUCT. Keel runs read-only cross-artifact analysis before plan challenge. Ordinary owner Gate 1 must be recorded before Build.
2. Phase B recon checks pickup drift and the approved file/test seams. In the Build workspace, use the Test URLs portal to verify a real library movie’s effective stream origin, including redirects, in Chromium. Record the evidence before implementation. Stop if a real library stream resolves cross-origin or eligibility cannot be verified. Keep native fallback for ineligible streams in the implementation and tests.
3. Establish the test cases, implement the existing player seam, then verify graph, persistence, and UI behavior. Collect the calibrated signal and real-library Chromium evidence before delivery clearance.
4. Run the applicable Phase 3 step 5 and Phase 5 gates. Require `scripts/run-web-gates.ps1` exit 0 on the implementation diff. Preserve actual verdict labels, including `SKIPPED (scope-empty)` for C# gates when the scope check returns that result. The Phase A web baseline is `SKIPPED (scope-empty)` and the Phase A C# baseline was reported as `N/A` in the DEV-569 note.

## Review closing bar and round cap

Require Sentry Risk, Ledger Standards, and Compass Spec reports. Record an explicit disposition for every finding. Independently verify every accepted in-scope Critical, High, Medium, or Low fix on the reviewed head before closing it. Cap the repair loop at two rounds. At the cap, report unresolved blockers and the reviewed head to Bernstein and keep the applicable gate closed.

Patron routes out-of-scope Critical/High, broken behaviour, and user-requested items to Rigger after checking open YouTrack issues. Log other out-of-scope items in CONCLUSIONS.md as “noted, no ticket.” Under §2.3, only a ticket scope or acceptance change or a necessary constitution departure goes to the owner as a structural checkbox.

## Plan challenge decisions

Patron ratified the following clarifications in CONCLUSIONS.md after review by Sentry, Ledger, and Compass. See the individual finding dispositions there. Ticket Scope and Acceptance and the Q6 file envelope apply in full. Zero §2.3 owner checkboxes.

1. **Eligibility (Q3).** Only recognized direct same-origin HTTP(S) library-media loads covered by an existing delivery invariant can qualify. T001 identifies the route and configuration that establish the invariant and confirms the actual Chromium request chain for the real movie and Range/retry loads. If the invariant or browser evidence cannot be established within scope, Build stops. Verify Web Audio availability and that the active element and source generation match the recognized delivery path on source changes and immediately before binding after resume. A same-origin src or currentSrc alone is insufficient. Hide boost and preserve native playback for unsupported, indirect, unknown or unverified paths, including a redirect outside the verified origin.
2. **Attachment and recovery (Q3).** Prepare and connect the downstream gain, compressor and destination first. Keep the current native element if failure or lost eligibility occurs while demonstrably unbound. If source creation or connection leaves ownership partial or uncertain, or a bound element becomes ineligible, recover on a fresh native element without binding the replacement on that attempt. Reuse existing attach/detach, media controls and subtitle wiring. For the same media, retain seek position when seekable after metadata, pause/play intent subject to autoplay policy, stock volume/mute, playback rate and caption selection/delay as applicable. A genuinely new media item follows existing source-change semantics. Prove both failure stages and same-media continuity before Phase 2 passes.
3. **Chromium measurements (Q2/Q6).** Use temporary Test URLs portal page evaluation in the test session to observe the actual compressor output through an analyser branch installed before explicit activation. Capture a separate native reference without binding the element. T001 proves the portal instrumentation and independent baseline method on controlled test media or reports a blocker. T012/T013 collect actual calibrated and real-library evidence. No production measurement hooks, exports or globals.
4. **Existing conventions (Q4–Q6).** Use the existing player keyboard exclusion for the focused native range together with its native navigation and Q5 shortcut behaviour elsewhere. Follow subtitlePreferences.ts for namespaced localStorage and error isolation without changing that file. Tests use controllable AudioContext doubles and verify reinitialization of the same actual element after a module-reload simulation. Leave exact helper visibility, guard storage and remount mechanism to Build inside the approved files.
