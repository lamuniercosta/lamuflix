# DEV-18 — Assumptions

- [assumed] Q6: fixed delayed-retry default is 30 seconds; configurable test value may be approximately 1 second. Basis: ticket T10 requires a TTL delay without prescribing its value; Patron accepts Keel's default.
- [assumed] Q6: Prefetch defaults to 1. Basis: ticket T09 requires options-based prefetch; recon-DEV-18:113 confirms the existing value.
- [assumed] Q7: use routing keys requested, retry and dead-letter. Basis: ticket T08-T11 names exchange/queues but leaves binding-key wording open; Patron accepts Keel's wording.
- [assumed] Q9: the intended cutover targets a personal development broker with no required in-flight-message migration. Basis: Keel's stated premise and PRODUCT.md:8; this is unverified operational context and does not authorize purging broker data.
