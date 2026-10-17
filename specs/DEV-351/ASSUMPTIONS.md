# DEV-351 — Assumptions

No `[assumed]` taste decisions. This ticket introduces no UI, labels, sorting, or empty-state copy. Taste count stays zero (Q8, PRODUCT.md:28–30, recon-DEV-351:24).

C1/OD-1 is an unresolved owner structural checkbox and blocks Gate 1 (Phase 2 step 3.3). Patron recommends `src/LamuFlix.Core/Domain/PlaybackEvent.cs`. That recommendation is not owner approval. OD-1 stays unchecked.

C2 is resolved: `IPlaybackHistory` is pre-whitelisted and needs no ADR. C3 is resolved: incremental `AddPlaybackEvents` is approved. C2 and C3 are not owner checkboxes.

Q1–Q8 entries in CONCLUSIONS.md are rulings, not silent assumptions. The step 3.3 ruling replaces the earlier Q8 statement that no owner structural checkbox was required. Missing future facts require `needs recon: <question>` rather than an assumption.
