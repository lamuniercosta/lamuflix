# DEV-483 Assumptions

- `[assumed]` (Q2): Sequential measurement batch size of 20 requests per target endpoint after the cold first-request measurement, recording every sample plus median, p95, max, HTTP status, and payload validity.
- `[assumed]` (Q2): Settled post-startup/post-batch interval of `>= 60 s` (alongside directly exercising the diagnosed post-startup degradation trigger) before repeating the 20-request verification batch for `GET /api/recommendations/for-you?take=10`.

## DEV-483 Q2 measurement protocol — 2026-11-12

- [assumed] Capture 20 sequential requests per route/case in each batch. Patron explicitly approved this operational sample size in Q2; the ticket itself supplies the thresholds, not the batch size.
- [assumed] Use a settled post-startup/post-load interval of at least 60 seconds before repeating batches, in addition to reproducing/exercising the diagnosed timeout trigger. Patron explicitly approved this protocol parameter in Q2; it is not a claim about the unknown cause.

Basis: CONCLUSIONS.md Q2, confirmed in Q6; recon-DEV-483 lines 42–48. No taste ruling was needed. These assumptions do not waive any ticket target.
