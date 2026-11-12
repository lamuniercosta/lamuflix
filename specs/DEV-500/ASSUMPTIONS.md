# DEV-500 — Assumptions

## Q3/12 — Provisional style defaults and control values

Patron ratified this profile against `recon-DEV-500:38-57,73-78,87-91` and §2.3. Complete exchange: `CONCLUSIONS.md`, Q3. The screenshots are unavailable; these choices are not a verified transcription. Screenshot fidelity remains outstanding visual verification; discrepancies require an explicit ruling and do not silently change ticket acceptance.

| Setting | Choices / range | Default | Provenance |
| --- | --- | --- | --- |
| Font | System sans-serif, Serif, Monospace; local CSS stacks only | System sans-serif | [assumed] |
| Text size | 50–200%, step 10 percentage points | 100% | [assumed] |
| Text color | Native color input | `#FFFFFF` | [assumed] |
| Text opacity | 0–100%, step 5 percentage points | 100% | [assumed] |
| Background color | Native color input | `#000000` | [assumed] |
| Background opacity | 0–100%, step 5 percentage points | 75% | Range/step [assumed]; default is ticket-mandated |
| Edge style | None, Drop shadow, Raised, Depressed, Outline | None | Choices are ticket-mandated; default [assumed] |

Reset restores this profile. Exact presentation details not settled by this ruling remain for Palette design and explicit Patron adjudication; do not invent screenshot evidence.

## Q4/12 — Timing interaction details

[assumed] Patron ratifies G/H using 100 ms steps and Shift+G/H using 500 ms steps; editable controls, dialogs and Ctrl/Alt/Meta combinations are ignored within the existing player shortcut scope. Disable timing controls and make G/H no-op without a usable selected track. Off hides live captions and retains saved values. Reset timing affects only the active pair; Reset to default affects only global style. No arbitrary product delay clamp. Basis: `recon-DEV-500:12-14,31-36,48-56,65-69`, §2.3; full Q4 exchange in `CONCLUSIONS.md`. The ticket already mandates click increments, sign convention and pair/global persistence; those are facts, not assumptions.

## Q5/12 — Overlay implementation choices

[assumed] Immutable cue baselines, derived effective intervals, playback-only frame scheduling, immediate event resynchronization and cleanup are the ratified implementation approach. Render partial negative-start overlap at time zero, preserve line breaks and overlapping cues, and keep overlays pointer-transparent. Safe text rendering introduces no unrestricted HTML injection. Basis: `recon-DEV-500:31-46,53-57,65`, §2.3; full Q5 exchange in `CONCLUSIONS.md`. Shared overlay, hidden loaded track and instant cue shifting without reload are ticket requirements, not assumptions.

## Q6/12 — Storage details

[assumed] Use versioned keys `lamuflix:subtitle-style:v1` and `lamuflix:subtitle-delay:v1:<encoded movie+track tuple>`. Validate the entire style profile and safe-integer delay; invalid style defaults as a whole, invalid delay to zero. Catch access/read/write/remove failures; initialize defaults with blocked storage, retain subsequent session adjustments after failures, and keep resets independent. No cross-tab live synchronization is required. Basis: `recon-DEV-500:48-56,65-71`, §2.3; complete Q6 exchange in `CONCLUSIONS.md`. Browser-only persistence and playback unaffected by storage failure are ticket requirements.

## Q7/12 — Presentation choices

[assumed] Signed delay display uses one decimal and `0.0 s` at zero. Options remains editable with captions Off. Preview sample is “The quick brown fox jumps over the lazy dog.” Font stacks are `system-ui, sans-serif`; `Georgia, 'Times New Roman', serif`; `ui-monospace, monospace`. Background fits caption lines and its opacity is independent from text. CSS edges map to no shadow, offset dark shadow, opposing light/dark raised/depressed bevels, and a four-direction dark outline. Palette determines responsive layout, scale, spacing and exact shadow geometry within these bounds in `design.md`; extra taste choices require Patron ruling before plan challenge. Basis: `recon-DEV-500:28-46,52-57,73-78`, §2.3; full Q7 exchange in `CONCLUSIONS.md`. Ticket labels/help, controls, shared preview and screenshot acceptance remain authoritative requirements.

## Phase 2 step 6 — Patron-ratified amendments

Full question/answer: `CONCLUSIONS.md`, plan challenge section. Binding details: `brief.md`, Phase 2 step 6 amendment. Basis: Q3–Q8, `PRODUCT.md` §§2–5 and current `recon-DEV-500:7-16`. Earlier recon citations above are historical; current note replaced that snapshot and reports restoration unavailable.

- [assumed] Canonical numeric movie ID and exact opaque API track ID form a JSON array suffix (`JSON.stringify([numericMovieId, trackId])`). No positional/URL identity, case folding or Unicode normalization. Backend identity stability is unavailable and unproven; this assumption never waives real reload persistence acceptance.
- [assumed] Half-open active-cue intervals (inclusive start, exclusive end), immutable loaded-cue snapshots and plain literal text including markup-looking strings clarify Q5. Native `hidden` mode is the explicit implementation target for the ticket's loaded/hidden requirement.
- [assumed] Ignore G/H from any input, textarea, select, contentEditable target or dialog context, retaining the Q4 playing/track/modifier guards. Immediate best-effort persistence follows Q6; no mandatory debounce or cross-tab synchronization.
- [assumed] Provisional panel palette `#141417`, `#2A2A30`, `#EDEDEF`, `#A7A7B3`; focus `#7DD3FC`; minimum 44px interactive targets; neutral checkerboard preview backdrop; no CSS transitions. Palette specifies numeric geometry/scale within Q7. Four color/opacity controls retain their individual ticket labels. Reference fidelity remains unverified and unwaived.
- [assumed] Internal font tokens `system-sans` / `serif` / `monospace`; edge tokens `none` / `drop-shadow` / `raised` / `depressed` / `outline`.

Patron also approved the Q8 test-only `subtitleTestDoubles.ts` boundary addition and single `useSubtitleOptions` instance with props-only renderers; these are recorded implementation decisions, not new layers or dependencies.
