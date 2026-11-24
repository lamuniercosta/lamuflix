# DEV-547 — Assumptions

- `[assumed]` (Q2): Use `LAMUFLIX_COMMIT` and `LAMUFLIX_RELEASE` as the build-arg / container environment variable names across Dockerfiles, CI, Kubernetes manifests, and Compose (`recon-DEV-547:56-57,64`).
- `[assumed]` (Q2): Fall back to `"unknown"` for `commit` when `LAMUFLIX_COMMIT` is unset, empty, or whitespace in an unconfigured local source build so no fabricated SHA is reported (`specs/PRODUCT.md` §4; `recon-DEV-547:58,63`).
- `[assumed]` (Q3): Format the single running-app version line as `<release|dev> (<first-8-SHA-chars|unknown>)` — e.g. `v0.1.0 (8a2deb9b)` or `dev (8a2deb9b)` (`specs/PRODUCT.md` §4; `recon-DEV-547:58,68`).
- `[assumed]` (Q3): Keep the version line absent (`null` render, no placeholder text) while `GET /api/version` is loading or if the request fails so the app shell stays usable and never displays invented release data (`specs/PRODUCT.md` §4; `recon-DEV-547:15,58`).
