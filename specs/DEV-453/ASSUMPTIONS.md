# DEV-453 — Recorded policy assumptions

These are explicit Patron-approved choices, not unverified technical facts or user approvals. Full exchanges and bases are in CONCLUSIONS.md; brief.md is the authoritative planning handoff.

- [assumed] Q4: Within the first matching preferred language, rank moviehash match first, highest download_count, then lowest subtitle file_id. This is deterministic selection policy, not a claim about measured provider ranking.
- [assumed] Q4: One exact-basename SRT is the selected language sidecar; preserve an existing sidecar, do not overwrite/re-download, and do not choose outside configured languages. Exact-basename compatibility itself comes from recon, not taste.
- [assumed] Q5: Preparation is explicit per movie and separate from streaming. No automatic background downloads or web UI workflow in this ticket. Patron found no ticket contradiction.
- [assumed] Q6/Q10: Outcome tokens are saved, alreadyPresent and noMatch, with nullable selectedLanguage. Existing-sidecar success precedes acquisition-setting checks after authorization. Error status mapping is the deliberate public contract in brief.md.
- [assumed] Q6/Q10: Subtitles option names, default SaveEnabled=false and empty language preferences; explicit validated acquisition settings and positive configured resource bounds. No preferred language, numeric safety default or user-facing copy is invented.
- [assumed] Q7: Two narrow ports in existing projects, no provider registry or SDK, no schema changes, subtitle HTTP operations independent of gated player execution. These are cited architectural rulings rather than silent scope additions.
- [assumed] Q11: Two review/remediation rounds, blocking High/Medium/security/contract/ticket failures and explicit disposition for every Low. The cap never grants clearance.

No assumption asserts current live provider success, unlimited quota, a download URL lifetime, redistribution rights, language-suffix autodetection, dependency delivery or runtime acceptance. Phase B verification tasks remain as recorded in Q9. No unresolved structural checkbox was identified by Patron at Q12.
