# DEV-454 assumptions and taste rulings

These are deliberate Patron taste decisions, not unverified implementation facts. Full exchanges and cited rationale are in CONCLUSIONS.md.

| Ruling | Decision | Basis |
| --- | --- | --- |
| [assumed] Language labels | en: English; pob: Portuguese (Brazil); exact default marker: Default; bare SRT: Subtitles; unknown suffix: filename basename only | Patron Q1/Q4/Q7; recon:26,50; charter 2.3 |
| [assumed] Duplicate labels | Append deterministic (2), (3) counters after sorting | Patron Q4/Q7 |
| [assumed] Ordering | Default-marker first; language/label ordinal case-insensitive; server-side filename ordinal tie-break | Patron Q4/Q7; recon:26 |
| [assumed] Initial choice | Off, including when default-marker sidecars exist; marker is a sort hint | Patron Q4/Q7 |
| [assumed] Selection lifetime | Viewer session only; reset on movie change; no persisted or browser-language selection | Patron Q4 |
| [assumed] Empty state | Hide selector when the track list is empty | Patron Q4 |
| [assumed] Error copy and retry | Subtitles unavailable; switch Off and allow retry by reselecting a track while video continues | Patron Q4 |
| [assumed] List-failure copy and retry | Subtitles unavailable with explicit Retry while video continues and subtitles stay Off (never empty-list hide on list error) | Patron Phase 2 Step 6 (Sentry F7) |
| [assumed] Invalid-content presentation | Generic sanitized 422 error; no filesystem paths or cue contents | Patron Q2/Q3 |

## Explicit uncertainties, not assumed facts

- DEV-455/DEV-456 are ticket dependencies, not proven delivered APIs or proven current browser functionality. Q5 corrects any stronger reading of Q4.
- Existing runtime/BCL Windows-1252 capability is unverified in recon. Q6 approves no new package; inability to meet scope within existing dependencies requires recon and a cited Patron ruling.
- Recon's inspected-path results do not establish global absence of existing subtitle code.
- No separate Technical Design or Acceptance Criteria section exists in the complete task-script output inspected by recon:39-55.
- These uncertainties do not permit silent implementation substitutions, owner approval assumptions, or scope expansion.
