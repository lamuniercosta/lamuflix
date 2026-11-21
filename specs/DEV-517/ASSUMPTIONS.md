# DEV-517 assumptions

- [assumed] Patron approved the continuous mounted For You page as the visit boundary. Leaving For You or reloading starts a new visit. See Q1 in CONCLUSIONS.md.

- [assumed] Patron approved concise visible status when the visit reaches the replacement limit. Load more stays available under the committed exclusions. See Q3 in CONCLUSIONS.md.

- [assumed] Q4 supersedes Q3's limit status. Patron approved a retryable replacement error when the server rejects the attempt. Load more stays usable under the committed state.

- [assumed] Patron approved locking Load more and replacement interactions while one operation is pending, and a concise non-error status for an exhausted replacement. See Q5 in CONCLUSIONS.md.

- [assumed] Patron approved card control placement and label wording as taste decisions. The feature supplies the per-card action. See Q6 in CONCLUSIONS.md.

- [assumed] Patron approved keeping the committed ordered visit grid, committed exclusions, and scan cursor/buffer intact across incidental query invalidation or background refetch once the initial page commits, while starting fresh on remount; scanning consecutive DEV-541 slices during Load more until up to `take` unseen items are collected or `hasMore=false` and hiding Load more on true exhaustion (including an empty terminal append); and keeping Q4 manual retry uncapped/unlatched and Q5 replacement scan to true exhaustion (`hasMore=false`) with visit cancellation and no arbitrary fetch or time cap. See Plan challenge refinement in CONCLUSIONS.md.
