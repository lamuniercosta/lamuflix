# DEV-547 — Assumptions

- `[assumed]` (Q2): Use `LAMUFLIX_COMMIT` and `LAMUFLIX_RELEASE` as the build-arg / container environment variable names across Dockerfiles, CI, Kubernetes manifests, and Compose (`recon-DEV-547:56-57,64`).
- `[assumed]` (Q2): Fall back to `"unknown"` for `commit` when `LAMUFLIX_COMMIT` is unset, empty, or whitespace in an unconfigured local source build so no fabricated SHA is reported (`specs/PRODUCT.md` §4; `recon-DEV-547:58,63`).
