---
status: proposed
---

# Use one subtitle provider with a separate sidecar write boundary

DEV-453 requires one verified provider, ordered languages, BS Player-compatible sidecars, writable daily-driver saves and read-only demo serving. After evaluating BS Player first without finding a public supported service contract, Patron selected OpenSubtitles.com REST for personal non-commercial search/download on documented staff-endorsed evidence; live configured-key verification remains a separate acceptance step, and no redistribution license is inferred (CONCLUSIONS.md Q3; recon-DEV-453 lines 13, 17–21).

Keep ILibraryFileReader read-only under ADR 0037. Introduce only ISubtitleProvider and ISubtitleSidecarStore in the existing Core project, with HTTP and contained filesystem adapters in Infrastructure and explicit prepare/serve endpoints in Api. A selected SRT uses the exact video basename; preserve existing files and save through atomic create-without-replace. This separates opt-in writes and quota-consuming network acquisition from existing-track reads, allowing read-only demo serving without credentials or provider calls (Q4–Q8).

The trade-off is a separate explicit preparation operation and one primary selected language sidecar per video. A provider registry, new package/project, database subtitle table, widened general reader/writer abstraction and streaming-triggered downloads would add scope or couple unrelated responsibilities; none is authorized. Subtitle HTTP operations are independent of Features:LocalPlay, while player execution remains gated and unchanged. The proposed decision is ready to inform Quill's spec/plan/tasks; it does not establish Gate 1, runtime acceptance or merge approval (Q7, Q9, Q12).
