# DEV-439 Assumptions

Worktree: `F:/Dev/LamuFlix.worktrees/feature-439-spec`; branch: `feature/439-spec`.

---

## Q6/12 — Editorial copy for SafeDescription and CONTEXT.md glossary

- `[assumed]` **`EnrichmentFailureCategory.QuotaExhausted.SafeDescription` copy**: Exact caller-safe description string is `"Local OMDb daily request quota admission was denied."` — accepted per Keel's Q6/12 recommendation to match the concise sentence style of `EnrichmentFailureCategory.SafeDescription` while stating local OMDb daily quota admission denial rather than provider HTTP 429 (`recon-DEV-439:18-24,64-65`, `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs:34-42`, `specs/PRODUCT.md:26-30`, `lamuflix-team-charter:17`).
- `[assumed]` **`CONTEXT.md` glossary copy for `QuotaExhausted`**: Exact glossary definition is `"QuotaExhausted is an enrichment failure caused by denial of admission under the local OMDb daily request quota; it is distinct from provider HTTP 429 and does not itself imply deferral."` — accepted per Keel's Q6/12 recommendation to fulfill Scope & Technical Design item 5 while keeping deferral (`DEV-333`) out of scope (`recon-DEV-439:60,64-68,85-87`, `specs/PRODUCT.md:26-30`, `lamuflix-team-charter:17`).
