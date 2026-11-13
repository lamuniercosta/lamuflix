# DEV-500 — Subtitle timing and caption style brief

Status: **GRILL CLOSED — 10/12 questions**, confirmed by Patron at Q10. Ready for Quill drafting. This is not `PLAN FROZEN`, Gate 1 approval, implementation authorization, dependency readiness, runtime acceptance, review closure or merge clearance.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-500-spec`. Branch: `feature/500-spec`. Recon head: `7243314f99f57dbb88a9f5abd2ea43eb0c5f939d`. Preserve the pre-existing `harness.yml` modification. Keel owns this brief, append-only `CONCLUSIONS.md`, and `ASSUMPTIONS.md`; Quill drafts subsequent artifacts without inventing decisions.

## Authority

`recon-DEV-500:21-92` restores the authoritative ticket and owner rulings; `:1-19` records dependency/integration evidence. Earlier Q1/Q2 citations refer to contemporary snapshots before the note was restored. `CONCLUSIONS.md` preserves every full question/answer, basis, rationale and implications. `ASSUMPTIONS.md` distinguishes deliberate `[assumed]` choices from ticket facts. Standing charter §2.3 and task-pipeline Phase 2 govern authority and drafting. Facts come from recon; missing facts require `needs recon: <question>` rather than independent searches or measurements.

Only ticket-changing departures and necessary constitution departures go to owner checkboxes. Patron cannot close an owner checkbox. No departure was ratified in this grill.

## Q1 answer — Frozen scope and loop discipline

Patron ratified the complete ticket scope and owner rulings 1–4, preserving existing subtitle selection, with no new dependency, public API, database schema or LocalPlay changes. Basis: ticket scope/owner rulings, now recon `:28-80`.

Deliver all of the following:

- Populate the CC Options tab to match `ref-options-timing.png`, `ref-options-style-1.png`, and `ref-options-style-2.png`.
- Subtitle delay minus/value/plus controls: 0.1 seconds per click, 0.5 seconds with Shift; help line exactly “Negative shows subtitles earlier, positive later.”; Reset timing; G earlier and H later while playing. Shift loaded cue times instantly without reload.
- Live Subtitle preview at the top of Caption style. Controls: Font, Text size (%), Text color, Text opacity, Background color, Background opacity (75% default), Edge style (None, Drop shadow, Raised, Depressed, Outline). Reset to default restores the settled profile, subject to reference verification.
- Own overlay inside media-chrome; native track loaded but hidden. Preview reuses the same overlay. Every control changes preview and live captions, including fullscreen.
- Browser localStorage only, per device: delay per movie+track, style global. Restore saved values on reload; a never-saved pair starts at zero. Storage failure never disrupts playback.
- Real Chromium and Firefox receipts using copies of actual `E:\Filmes` movie folders, including `<movie folder>\Subs\<video stem>-<code>.srt`. Synthetic fixtures alone do not satisfy acceptance.
- Keep media-chrome (ticket names existing version 4.19.4); no new player library, server persistence, schema changes or subtitle-layout changes.

### Closing bar and caps

Grill cap 12; closed at 10. All verified in-scope Critical/High/Medium/Low correctness, acceptance and documented-standard findings close before review closure. Out-of-scope enhancements become follow-ups. Review/remediation cap is two rounds; report remaining blockers without claiming closure. Standing pipeline also caps fixes at two commits per round; a third requires reassessment. Size M always requires Sentry, Ledger and Compass axis reports; missing reports block adjudication.

anything else is a follow-up issue, not a finding in this round.

## Q2 answer — Dependency and pickup prerequisite

Patron ratified reuse of the **delivered DEV-503 CC shell**, without duplication or cherry-picking unmerged commits. Before implementation: DEV-503 must be merged to main, the worktree rebased/updated, and recon refreshed for exact Options-tab integration points. Basis: recon `:7-14,59-61,82-85`.

Pinned recon found `c7a9222e384121c2ec4b55232be11b8e1e4d7850` on `feature/DEV-503`, absent from `origin/main` at `7243314f`. This is historical evidence, not a current delivery claim. Existing integration references are `BrowserPlayer.tsx:73-75,100,156,199-207` and `SubtitleSelector.tsx:26-38,60-79`; current selector provides selection, not Options. If refreshed recon names a different shell file, amend this brief explicitly before dependent tasks.

## Q3 answer — Provisional style profile

Patron ratified this profile. Only unspecified choices are `[assumed]`; 75% background opacity and five edge choices are ticket facts. Basis: recon `:38-57,73-78,87-91`, §2.3.

| Setting | Choices / range | Default | Provenance |
| --- | --- | --- | --- |
| Font | System sans-serif, Serif, Monospace; local CSS stacks | System sans-serif | [assumed] |
| Text size | 50–200%, step 10 percentage points | 100% | [assumed] |
| Text color | Native color input | `#FFFFFF` | [assumed] |
| Text opacity | 0–100%, step 5 percentage points | 100% | [assumed] |
| Background color | Native color input | `#000000` | [assumed] |
| Background opacity | 0–100%, step 5 percentage points | 75% | Range/step [assumed]; default mandated |
| Edge style | None, Drop shadow, Raised, Depressed, Outline | None | Choices mandated; default [assumed] |

Reset to default restores this profile. Scoped recon found no screenshot/default transcription. **Screenshot fidelity remains unverified and unwaived.** These values permit drafting, not a reference-equivalence claim. Inspect actual references when available; discrepancies require an explicit Patron ruling, or owner checkbox if ticket-changing.

## Q4 answer — Timing behavior

Patron ratified integer-millisecond storage/math, 100 ms ordinary and 500 ms Shift changes, negative earlier and positive later, with no arbitrary product clamp. G/H follow the same steps only while playing inside existing player shortcut scope; ignore editable controls, dialogs and Ctrl/Alt/Meta modifiers. Without a usable selected track, disable timing controls and make hotkeys no-op.

Reset timing clears only active movie+track delay; global style reset never resets timing. Off hides live captions while retaining saved values. Unsaved pairs start at zero; saved pairs restore their own value without cross-movie/track leakage. Basis: recon `:12-14,31-36,48-56,65-69`, §2.3. Unspecified interaction details are `[assumed]`.

## Q5 answer — Rendering and shifted cue approach

Patron ratified these choices as satisfying instant cue shifting without reload:

1. Preserve immutable loaded-cue baseline times; derive effective intervals as baseline + current delay. Never cumulatively shift already-shifted values; do not refetch/reload on timing/style changes.
2. Keep native track loaded/hidden; custom overlay renders all cues active at the actual media clock using effective shifted intervals. Prevent duplicate native/custom captions.
3. Negative effective starts remain meaningful: show the portion overlapping time zero; omit cues already ended. Preserve originals rather than depending on negative assignments to browser cue setters.
4. Resync immediately on delay/track/seek/pause/play/rate/cue-readiness changes. Playback-only frame loop updates state only on active-cue changes. Clean listeners, frames and stale-track work on switch/unmount.
5. One `CaptionOverlay` component/style calculation for live and preview inside media-chrome fullscreen subtree. Preserve multiple cues and line breaks; pointer-transparent; safe text rendering without unrestricted HTML injection or new dependencies.

Basis: recon `:31-46,53-57,65`, §2.3. Implementation details are `[assumed]`; actual hidden-track loading, clocks and fullscreen still need browser evidence.

## Q6 answer — Persistence and failure behavior

Patron ratified these details (basis recon `:48-56,65-71`, §2.3):

- Global key `lamuflix:subtitle-style:v1`; per-pair key `lamuflix:subtitle-delay:v1:<encoded movie+track tuple>`. Encode the whole pair unambiguously.
- Validate font/edge enum identifiers, permitted numeric ranges/steps, hex colors, and safe-integer millisecond delay; never accept arbitrary CSS. Missing/malformed/unsupported style defaults as a complete profile; invalid delay defaults to zero.
- Catch storage access/read/write/remove failures independently of playback. Blocked storage initializes defaults; later changes remain in memory for this mount. Failed writes retain the current adjustment without claiming persistence.
- Independent resets update memory and attempt removal of only their own key. A failed removal cannot promise reset survives reload.
- Prevent stale effects from saving old-pair values under new-pair identity. No server sync, cross-tab live-sync requirement, subtitle-file or cue-content writes.

Key/validation/degradation details are `[assumed]`; local-only persistence and playback safety are ticket requirements.

## Q7 answer — Presentation, accessibility and Palette handoff

Patron ratified reuse of DEV-503 tab/focus/dismissal behavior. Retain ticket labels and exact help text. All buttons/selects/ranges/color inputs must be labeled, keyboard operable and visibly focused. Display percentages and signed seconds to one decimal; zero is `0.0 s`. Options stays editable with captions Off, while timing remains disabled without a usable track.

Preview sample `[assumed]`: “The quick brown fox jumps over the lazy dog.” Text/background opacity operate independently; background boxes fit caption lines rather than tinting video. Local font stacks `[assumed]`: `system-ui, sans-serif`; `Georgia, 'Times New Roman', serif`; `ui-monospace, monospace`.

CSS-only edge mappings `[assumed]`: None no shadow; Drop shadow offset dark shadow; Raised/Depressed opposing light/dark bevel directions; Outline four-direction dark outline. Captions remain non-interactive, pointer-transparent, non-aria-live, and never capture focus.

Palette owns `design.md`: responsive layout, scale/spacing and exact shadow geometry within Q3/Q7 bounds. Additional taste decisions need explicit Patron rulings before plan challenge. Quill must not invent them. Design and any resulting brief amendments precede plan challenge. Screenshot fidelity is unwaived. Basis: recon `:28-46,52-57,73-78`, §2.3.

## Q8 answer — Files and task ordering

Patron ratified the following bounded file plan under `web/src/features/playback/`. New names are planned responsibilities, not discovered existing files.

| File(s) | Responsibility |
| --- | --- |
| `BrowserPlayer.tsx` | Own tab selection and delivered panel mount; mount overlay; integrate preferences, media and shortcuts |
| Delivered `SubtitlePanel.tsx` | Own tab semantics and Options tab body; integrate props-fed Options content in the existing shell |
| `SubtitleSelector.tsx` | Preserve Subtitles-tab track selection/Off/loading/error/retry behavior |
| `useSubtitlePlayback.ts`, `subtitleTrackModes.ts` | Only needed loaded/hidden selected-track integration |
| `subtitleCopy.ts` | Ticket labels/help and settled preview copy |
| New `subtitlePreferences.ts` | Defaults, validation and storage codec/helpers |
| New `subtitleTiming.ts` | Pure baseline/shifted-interval/active-cue rules |
| New `useSubtitleOptions.ts` | Pair/global preference lifecycle |
| New `useSubtitleCues.ts` | Media synchronization and cleanup |
| New `CaptionOverlay.tsx` | Shared safe renderer |
| New `SubtitleOptions.tsx` | Timing and style controls |
| New `captionOverlay.css`, `subtitleOptions.css` | Styles within Palette design |
| Corresponding new Vitest/RTL tests | Pure rules, preference/cue hooks, overlay and Options |
| Existing `BrowserPlayer.test.tsx`, `subtitleSelector.test.tsx`, `subtitleTrackModes.test.ts` | Integration and regression |

No generic service/state layer, unrelated rewrite/deletion, backend, API, DTO, OpenAPI, generated TypeScript, package manifest, harness or subtitle-layout changes. Preserve existing generated contract usage. If refreshed dependency recon changes shell integration ownership, explicitly amend this brief first.

Ratified task order:

1. Phase A drafting from this brief; Palette design and extra taste rulings; read-only analysis and plan challenge; spec PR/user Gate 1. No implementation here.
2. Build pickup: Gate 1 satisfied, DEV-503 merged, worktree updated/rebased, recon refreshed, and brief/tasks amended for confirmed drift.
3. Pure preferences/timing rules plus tests.
4. Shared overlay/media lifecycle plus tests.
5. Options/shortcut shell integration plus tests.
6. Focused regression and applicable web/harness gates.
7. Real-library Chromium/Firefox receipts and outstanding reference verification; preserve honest status if evidence cannot be obtained.

Basis: recon `:12-14,28-71,82-85`, §2.3, Q8. No new architectural layer.

## Q9 answer — Tests and gate expectations

Existing Vitest/React Testing Library tooling, no new dependency. Add deterministic timing-invariant tests without a property-test library. Synthetic unit fixtures supplement real-browser acceptance.

| Area | Required behavioral evidence |
| --- | --- |
| Timing | Normal/Shift steps, sign convention, reset isolation |
| Shortcuts | Playing-only, focus/editable/dialog/modifier guards, unusable track |
| Identity/persistence | Reload, saved-pair restore, new-pair zero, global style, stale-switch races |
| Storage | Invalid JSON/profile/values, denied access/read/write/remove/quota; defaults, session adjustments, uninterrupted playback |
| Cues | Immutable baseline, no cumulative drift, negative overlap, seek/pause/play/rate/late readiness, multiple cues, cleanup |
| Styles | Every control in both shared renderings, independent opacity, five edge styles, line breaks, safe text, reset |
| Integration | Loaded/hidden native track, Off, preserved selection/retry, no timing/style reload/refetch, fullscreen and no duplicate captions |

Real Chromium AND Firefox receipts record browser/version, reviewed commit, fixture-copy path proving actual `E:\Filmes` folder copies with `Subs`, actions, observations and screenshots. Exercise hotkeys, reload/pair isolation, independent resets, every style including fullscreen, blocked storage and uninterrupted playback. Synthetic same-folder fixtures alone fail the owner-required coverage. Inspect actual references before claiming screenshot/default fidelity; missing images stay unverified.

This grill/spec drafting phase runs no build/runtime gates. Later Gauge runs applicable task-pipeline/harness checks, including `scripts/run-web-gates.ps1` when web changes, recording scope, command, exit and verdict. Existing baseline receipt reports scope-empty skips; it does not verify future implementation.

- Exit 0 means PASS only for the checked scope.
- Scope-empty exit 2: non-blocking `SKIPPED (scope-empty)`, never PASS. Harness opt-out: `SKIP`.
- Mutation `N/A` requires the script configured-exclusion verdict; no production C# normally yields scope-empty, not a seat-invented pass.
- Exit 1 and `Could not run` remain unresolved/blocking. No threshold lowering or plain-build substitute for analyzer gates.
- No new C# or .NET property tests planned. Any .NET property-gate opt-out must be explicitly recorded by Conductor as `propertyTests: opt-out` with frontend-only/no .NET domain-invariants rationale. Do not silently count exit 2 as PASS. Vitest still tests deterministic timing invariants.
- Size M requires three review axes and two rounds maximum. Acceptance, review closure, Gate 1 and merge remain distinct. User retains merge authority; no seat merges or enables auto-merge.

Basis: recon `:31-71,87-91`, AGENTS.md and task-pipeline Phases 2–4, Q9.

## Q10 answer — Closure, vocabulary and handoff

Patron confirmed shared understanding and closed at 10/12 on the complete Q1–Q9 basis. No new glossary entry or ADR is needed. Canonical terms: subtitle delay (signed ms); caption style (global device-local profile); movie+track identity; immutable loaded cue baseline; shifted effective interval; shared overlay; preview.

Quill drafts `spec.md`, `plan.md`, `tasks.md` from this brief, recon and authoritative product guidance. A gap is `needs decision: <question>`, never a guessed decision. Keel then runs read-only `/speckit-analyze` and compares plan/tasks to this brief, sending one numbered fix list per round. Await Palette design and extra taste rulings before plan challenge. Record any later decision amendment here before dependent artifacts change.

Outstanding stage prerequisites: dependency pickup refresh; Palette design; actual reference inspection; implementation/applicable gates; both real-browser receipts. Grill closure does not declare them completed. Any later reference discrepancy receives an explicit cited Patron ruling; a ticket-changing resolution or necessary constitution departure requires an owner checkbox under §2.3.

## Phase 2 step 6 — Plan challenge amendment (2026-11-12)

Status: **FIXES REQUIRED — not PLAN FROZEN**. All three axis notes reviewed; full finding dispositions and owner-specific numbered fix lists are in `PLAN_ADJUDICATION.md`. This amendment precedes Quill/Palette repairs and supplements Q4–Q9. It does not reopen or extend the 10/12-question grill. Patron ratified the complete seven-item package; full question and answer are appended to `CONCLUSIONS.md`.

Evidence limitation: the current `recon-DEV-500:7-16` establishes frontend identity consumption but reports backend derivation unavailable. The previous 92-line ticket/reference/dependency note was replaced; earlier citations in Q1–Q10 and the draft artifacts refer to that historical snapshot, preserved in the recorded grill exchanges, not to the current 16-line note. Restoration is unavailable per current recon line 14. Do not present those historical line numbers as currently resolving. Conductor instructed the chain to proceed with unavailable evidence recorded; no ticket, dependency-delivery, reference-fidelity or backend-stability fact is newly inferred.

Patron-ratified decisions (basis Q3–Q8; `PRODUCT.md` §§2–5; current recon `:7-16`; historical ticket basis retained in Q1–Q10):

1. **Identity [assumed]:** use the normalized numeric movie ID already consumed by playback and exact opaque API `track.id`; delay-key suffix is `JSON.stringify([numericMovieId, trackId])`. Never use track position or source/blob URL, case-fold or normalize Unicode. Test delimiter/Unicode/case/empty/long track IDs, pair separation and round-trip, plus equivalent numeric movie representations. Backend cross-request/rescan stability is unavailable evidence, not a guarantee. Test reorder/reload with unchanged IDs and retain required real-library reload persistence acceptance. Do not add a fallback fingerprint or change API/schema.
2. **Cues/rendering:** selected native track mode is explicitly `hidden`. Copy loaded cue data into an immutable snapshot; effective interval is `[baselineStart + delay, baselineEnd + delay)`, start inclusive and end exclusive. At time zero, a cue ending at zero is absent and a cue starting at zero is active. Render all text, including markup-looking strings, literally without HTML interpretation. Keep the original no-refetch, shared-overlay and real-browser obligations.
3. **Shortcuts [assumed]:** G/H ignore focused `input` (including range/color), `textarea`, `select`, contentEditable targets and dialog contexts in the existing player scope, including descendants. Retain playing-only, usable-track, Ctrl/Alt/Meta guards and ordinary/Shift increments.
4. **Persistence:** immediate in-memory adjustments plus immediate best-effort saves. Mandatory debouncing is rejected without measured jank; any later optimization requires evidence and must preserve reload, pair isolation and independent resets without lost or resurrected writes. No cross-tab live sync; no storage-event listener is required.
5. **Presentation [assumed]:** provisional panel colors `#141417`, `#2A2A30`, `#EDEDEF`, `#A7A7B3`; focus `#7DD3FC`; minimum 44px interactive targets; neutral checkerboard preview backdrop; no CSS transitions. Palette must provide numeric shadow/edge geometry, spacing and caption scale/clamp rules in `design.md`. Keep separate visible/programmatic labels Text color, Text opacity, Background color, Background opacity. Screenshot/default fidelity remains unverified and unwaived.
6. **Serialization [assumed]:** font tokens `system-sans`, `serif`, `monospace`; edge tokens `none`, `drop-shadow`, `raised`, `depressed`, `outline`. These are internal browser preferences, not new API fields.
7. **File/state boundary:** add only `web/src/features/playback/subtitleTestDoubles.ts` as a co-located test-only media/TextTrack/rAF helper to Q8. Instantiate `useSubtitleOptions` once in `BrowserPlayer.tsx`; pass state/actions to props-only `SubtitleOptions` and `CaptionOverlay` (no storage/media access in renderers). Keep shortcut guard tests within approved hook/integration files; no additional shortcut module or abstraction is required.

Repair mechanics within these decisions: name `subtitleTiming.ts` as the implementation owner of shifted intervals/active-cue rules; name `subtitleTiming.test.ts` explicitly. Split Options work into preference lifecycle/storage, presentational controls, shell/shortcut integration and loaded/hidden track seams with red/green tasks and no same-file parallel writes. Use deterministic fake media/cue readiness and fake timers/stubbed rAF. Guard stale async work by generation/identity; retain abort and object-URL cleanup with the existing resource owner rather than duplicate ownership. Test out-of-order completion, rapid switches, StrictMode remount, original-baseline independence, long exact delay sequences, half-open boundaries, large delays, paused scheduling, unchanged-active-cue no updates, storage accessor throws and specific unsafe-text vectors. Recompute shifted intervals only when baseline/delay change; compare ordered cue identities so unchanged membership/order does not trigger state churn. Test cleanup of each resource actually acquired, not hypothetical observers. Browser receipts own real cue loading, fullscreen, responsive scale and target/motion/contrast observations; DOM assertions do not prove native suppression. If acceptance fixes change code, rerun affected tests and applicable gates and refresh receipts at the resulting head.

Palette design is delivered; its corrections remain pending. Quill must update stale prerequisite language and distinguish pending design repairs from absent design. Gates still precede browser receipts. Keep task IDs with explicit execution order, or renumber all references consistently; do not run by numeric ID alone. No new owner checkbox is triggered by Patron's rulings; ordinary Gate 1 remains pending.


## Phase B T001 — Landed-main integration amendment and reanalysis (2026-11-13)

Authority: Conductor explicitly requested this bounded reanalysis and brief/tasks amendment at `F:/Dev/LamuFlix.worktrees/DEV-500`, branch `feature/DEV-500`, HEAD `f88ab2336b6733684041082ad10c5d87d28e4e23`. Local `main` and `origin/main` resolve to that same SHA. Source facts come exclusively from refreshed `recon-DEV-500:4-27`; dependency ancestry is recorded by Rigger in task note `DEV-500:21-27`. This section supersedes historical shell ownership, current-recon line descriptions, and pending-design language above and in the drafting-era plan/spec; it does not rewrite historical rulings.

### Demonstrated integration drift (Q2/Q8 amendment)

- `SubtitlePanel.tsx:16-32,95-107,138-147` owns tab semantics and the two tabpanel bodies. Integrate props-fed `SubtitleOptions` in its existing Options body, replacing the placeholder content. This delivered shell file is explicitly within Q8; no parallel shell or cherry-pick.
- `BrowserPlayer.tsx:202-207,412-435` owns tab selection, the CC trigger and panel mount. Keep the sole `useSubtitleOptions` instance here and pass state/actions through the delivered panel to Options and to the shared overlay. Preserve existing tab/focus/dismissal behavior.
- `SubtitleSelector.tsx` owns Subtitles-tab track selection, Off, loading/error/retry. It is not the Options body owner; preserve its behavior and focused regressions. No unrelated rewrite is authorized.
- T018/T019/T022 exercise these ownership boundaries through player integration and selector regression coverage. Pure preferences/timing, cue lifecycle, presentational controls, hidden-track work and their serial ordering remain as ratified.

Basis: recon `:11-18`, Q2/Q8 explicit drift-amendment permission, and the Conductor ask. This is reuse of the delivered dependency within the existing scope, with no new dependency, architecture, API, schema, LocalPlay or constitution departure.

### Read-only analysis result, followed by separately authorized edits

The SpecKit prerequisite command (`check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks`, with `PYTHONUTF8=1`) could not run successfully: it rejects `feature/DEV-500` as nonnumeric. No analyze hooks are registered. The requested manual bounded cross-artifact comparison used spec FR-001–FR-019 / SC-001–SC-006, plan, tasks, this brief, the landed constitution, design status, prior independent reread and refreshed recon. The analysis itself made no writes; the Conductor separately authorized the brief/tasks amendments.

| ID | Severity | Location | Finding / disposition |
| --- | --- | --- | --- |
| I1 | Medium | Q8; plan Source Code; T001/T018/T019/T022 | Historical selector/CC-shell wording misattributes Options ownership. Corrected in Q8 and tasks; this amendment supplies the explicit delivered-panel boundary for the historical plan/spec wording. |
| I2 | High, blocking clearance | T000; CONCLUSIONS Gate 1 provisional confirmation; recon :25-27 | No verified DEV-500 spec-PR merge receipt is present in the supplied authoritative evidence. Provisional confirmation and landed spec files do not establish owner Gate 1. T000 remains unchecked. |
| I3 | Low | plan status/design references; tasks header/dependencies | Drafting-era pending-freeze/pending-design text is stale: KEEL_REPAIR_REREAD.md final correction declares PLAN FROZEN and recon :21 confirms delivered corrected design. Tasks status refreshed; plan remains historical and is qualified by this amendment. |

Coverage remains 25/25 FR/SC items mapped to 28 tasks T000–T027 (planning coverage only), with no newly uncovered requirement, new duplication or constitution conflict identified within this bounded check. Coverage families: FR-001–005 / SC-001 → T002–005,T016–019,T025; FR-006–007,011 / SC-002 → T003–004,T006–009,T011,T016–017,T025; FR-008–009 → T010–013,T018–021,T025; FR-010,012–013 / SC-003–004 → T003–004,T011,T014–015,T018–019,T025; FR-014–017,019 → T001–023,T025 as scoped; FR-018 / SC-005 → T024–025; SC-006 → T023. T000–001 and T026–027 are mapped lifecycle/governance work, not orphan implementation tasks.

### Clearance and remaining evidence

PLAN FROZEN is established by the final 2026-11-12 correction in `KEEL_REPAIR_REREAD.md:60-69`. DEV-503 shell presence and landed-main pickup are supported by refreshed recon and Rigger ancestry receipt. Palette design is delivered with corrections applied (recon :21). **Gate 1 / T000 is not verified; T001 remains incomplete and T002+ blocked.** Gauge baseline is not supplied by this analysis and is not claimed as PASS. No production edits, tests, gates or runtime acceptance were performed.

References were not found tracked; real-layout external fixture configuration was not found in recon scope (:22-23). Keep T024–T025 open; do not invent paths or claim visual/runtime fidelity. Backend identity stability remains unproven. Required next evidence: verified DEV-500 spec-PR URL/number, merged SHA and owner Gate 1 receipt for T000; Gauge pre-edit baseline for T001. This documentation-only reanalysis does not authorize Build past the existing gate.


### T000/T001 clearance update — verified direct owner landing (2026-11-13)

The Conductor supplied and explicitly accepted the verified direct owner landing of the spec package on origin/main: Lamunier Costa, `c3b28aae2379bacd7c5365a83001f55153e8d491`, subject `DEV-500: spec`; no separate PR. Refreshed recon :29-37 records commit provenance; the Conductor instruction supplies the Gate 1 disposition. The receipt is appended to `CONCLUSIONS.md`. This supersedes I2 and the initial blocked clearance above: T000/T001 are checked complete as instructed; T002 is next. Gauge baseline evidence remains with Gauge/Conductor before production edits and is not claimed as run or PASS here. Missing references/fixtures, runtime acceptance and backend identity stability remain as qualified above. This records an owner action already taken, with no agent merge or auto-merge.
