# DEV-471 — Assumed Decisions (ASSUMPTIONS)

- `[assumed]` **Q5 — CI job & stage timeout budgets and retry policy**: Reuse `web/e2e/run.mjs:17-36` operational budgets (`30m` per Compose stack startup, `120s` per async stage assertion, `3m` teardown per Compose project) under a `120m` total CI job timeout with zero automatic whole-test retries (`specs/DEV-471/CONCLUSIONS.md` Q5).
