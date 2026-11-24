# DEV-561 — Patron Rulings (CONCLUSIONS)

## Q1 — ADR 0049 boundary amending ADR-0011 decision 2
- **Verdict**: Approved — author ADR 0049 (`docs/adr/0049-*.md`, status `Accepted`) as a narrow amendment to `docs/adr/ADR-0011.md` decision 2 permitting one separate, labelled, read-only `Not in your library yet` discovery row on `For You`, while keeping main `For You` and `Related` recommendations strictly library-only with unchanged eligibility.
- **Basis**:
  - **Ticket scope & boundary** (`DEV-561` Goal; Wanted 1, 6, 7, 8; Done when 1, 5, 7; Out of scope 1, 3, 4; Needs 2): mandates an `Accepted` ADR amending `ADR-0011` decision 2 before code merges so one separate read-only `For You` row shows out-of-library movies with library-seed reasons and no playback/request/purchase controls, while library `For You` and `Related` stay library-only.
  - **Preserved ADR-0011 & ADR 0048 invariants** (`docs/adr/ADR-0011.md:13-15,19`; `docs/adr/0048-tmdb-crowd-similarity.md:11,17-19`): candidates come exclusively from locally persisted first-page TMDb recommendation rows already fetched for library movies (no discover/trending/similar lists, zero request-time provider HTTP calls, no viewing/taste data leaving the machine, explainable reasons required, and peer-history collaborative filtering still rejected).
  - **ADR numbering** (`DEV-561` Wanted 1; `recon-DEV-561:34-36`): `0048` (`docs/adr/0048-tmdb-crowd-similarity.md`) is the highest ADR on `origin/main` and open PR branches, so `0049` is the next free ADR number.
