# DEV-503 — Taste assumptions

These are explicit Patron rulings, not inferred requirements. Complete exchanges and cited bases are in CONCLUSIONS.md and brief.md.

- [assumed] Q4: One top-level surface at a time; triggers toggle; opening enters focus; Escape restores trigger focus before player fullscreen-exit handling; outside click preserves destination focus; reset surfaces on movie change/unmount. Basis: ticket keyboard/dismissal/fullscreen requirements and PRODUCT taste rules. Accessibility and hotkey non-interference remain required.
- [assumed] Q5: Subtitles is the active tab on every fresh panel open. Basis: local-track selection is this ticket's delivered content.
- [assumed] Q5: Preserve existing served-track labels and order. Basis: minimize unrelated behavior change while replacing presentation.
- [assumed] Q5: Options empty-shell copy is Subtitle timing and style options will appear here. It is non-actionable; no fake controls/results. Basis: ticket explicitly defers timing/style content, and PRODUCT requires honest concise copy.
- [assumed] Q7: Optional co-located CC panel extraction uses `web/src/features/playback/SubtitlePanel.tsx` and `web/src/features/playback/subtitlePanel.test.tsx` if accessibility or complexity warrants extraction; otherwise keep in existing approved playback files. Basis: matches `SubtitleSelector.tsx` / `subtitleSelector.test.tsx` co-located naming under Q3 file boundary.

Palette specifies visual details in design.md against the references without changing the frozen behavior. No assumptions after the twelve-question cap were needed; the grill closed after six.
