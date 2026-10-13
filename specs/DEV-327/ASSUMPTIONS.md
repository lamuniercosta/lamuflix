# DEV-327 — Assumptions

- [assumed] Stryker artifact name `stryker-report-core`; GitHub default retention (no override). Basis: CONCLUSIONS Q9.
- [assumed] New CI checks use the same triggers as ci.yml (push to main, pull_request to main). Basis: CONCLUSIONS Q3/Q7; path filters are Keel's.
- [assumed] Container job builds images without tagging for any registry (local tags only). Basis: CONCLUSIONS Q1.
