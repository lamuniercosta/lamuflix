# DEV-483 Assumptions

- `[assumed]` (Q2): Sequential measurement batch size of 20 requests per target endpoint after the cold first-request measurement, recording every sample plus median, p95, max, HTTP status, and payload validity.
- `[assumed]` (Q2): Settled post-startup/post-batch interval of `>= 60 s` (alongside directly exercising the diagnosed post-startup degradation trigger) before repeating the 20-request verification batch for `GET /api/recommendations/for-you?take=10`.
