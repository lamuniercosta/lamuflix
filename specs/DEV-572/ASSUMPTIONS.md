# DEV-572 assumptions

- [assumed] Q2 chooses completed, non-fragmented MP4 (video/mp4) as the output container, subject to the representative browser compatibility proof and the stop condition recorded in CONCLUSIONS.md. Patron approved this container choice.
- [assumed] Q4 scratch defaults proposed by Quill in `plan.md` Assumed scratch defaults (four only): MaxConcurrentConversions = 2 per API instance; IdleExpiry = 60 minutes after last reader release; ScratchSizeLimit = 20 GiB (LRU among completed assets with zero readers); default scratch directory `cache/stream-conversion` under content root unless an absolute path is configured. Q6 sets the 120 s cold-preparation proof bar only; no conversion-child process timeout is settled here.

- [assumed] DEV-572 R1: in-flight zero-waiter reattachment grace is 1 s, an internal constant (Keel recommendation; ruling in CONCLUSIONS.md Step 6).
