# DEV-544 evidence runbook

Status: Phase 2 (T002) before snapshot captured at capture base `b680e2be`. Implementation gates, the T021 after snapshot, judged-pair/coverage evidence and warm-timing measurements remain pending. Do not invent measurements, first placements, or gate results here. Never credentials.

## 0. Provenance

| Field | Value |
|---|---|
| Worktree | `F:/Dev/LamuFlix.worktrees/DEV-544` |
| Branch | `feature/DEV-544` |
| Capture base HEAD (`git rev-parse HEAD`) | `b680e2be3d1d4724ecba773e859d22b8001942e0` (spec/tasks-only commit; tree equals `origin/main`) |
| `origin/main` at capture | `2f1647b260963f645882eb2711cf425e143fa6ff` — `DEV-518 - Remove explicit MMR caps for Related recommendations` (merged DEV-518 six-criterion scorer) |
| Snapshot tool | `scripts/run-recommendation-snapshot.ps1`, SHA-256 `ae07c64c1137ea2357cb9ff1fd54011d577034f83eef5670183c5de30a10b7e8` (unchanged from the DEV-518/DEV-545 receipt) |
| Capture command | `pwsh -NoProfile -File scripts/run-recommendation-snapshot.ps1 -Mode Capture -OutputPath artifacts/recommendations/DEV-544-baseline.json` (parameter-free seed defaults `575,518,515,519,831`) |
| Capture wall clock (UTC) | `2026-11-20T17:55Z` (not carried in the artifact, which records no timestamp by design) |
| Artifact versioning | unversioned — `artifacts/` is gitignored (`.gitignore:66`); raw output is retained locally and never committed |

## 1. T002 before snapshot receipt

| Field | Value |
|---|---|
| Artifact path | `artifacts/recommendations/DEV-544-baseline.json` |
| Size | 39233 bytes |
| SHA-256 | `ff5841af6ba67def4a780e28059e6814cf8089b6c8dc94f47edaf38cf2078b20` |
| Mode / targets / takes | `capture`; targets `[575, 518, 515, 519, 831]`; For you `take=30`; each Related `take=10` |
| List statuses and row counts | `for-you` ok 30 rows; `related 575` ok 10; `related 518` ok 10; `related 515` ok 10; `related 519` ok 10; `related 831` ok 10 (80 rows total) |
| Repeat-capture determinism | second parameter-free Capture to a temporary operator path produced the identical SHA-256 `ff5841af6ba67def4a780e28059e6814cf8089b6c8dc94f47edaf38cf2078b20` — unchanged recommendation inputs across the capture window |
| Exit code | 0 (published atomically; no partial file left behind) |

Staleness check for the first production edit (T002/T003): the file above MUST exist, hash to `ff5841af6ba67def4a780e28059e6814cf8089b6c8dc94f47edaf38cf2078b20`, still carry targets `[575, 518, 515, 519, 831]` and remain reproducible. If any of that is missing or stale, stop before editing and report to Bernstein.

## 2. Fixture identity — 2026-11-16 home-library fixture

| Field | Observed value |
|---|---|
| Fixture | 2026-11-16 home-library fixture (the home instance library, not the synthetic evaluation fixture) |
| Home cluster | `lamuflix-home`, Argo application `lamuflix-home`: sync `Synced`, health `Healthy` (`scripts/home/Get-HomeStatus.ps1`, exit 0) |
| API origin | `http://localhost:3100` |
| Library size (`GET /api/movies` `totalCount`) | `449` movies, 449 fetched across 5 pages of 100 |
| Enrichment state | `Enriched` 449 of 449 |
| State observed | 56 watched, 39 in watchlist; every movie has a review value |
| Seed 575 | A Fistful of Dollars (1967), Enriched |
| Seed 518 | About Time (2013), Enriched |
| Seed 515 | A Few Good Men (1992), Enriched |
| Seed 519 | Aladdin (1992), Enriched |
| Seed 831 | The Wind Rises (2014), Enriched |

The five seed IDs match the DEV-545 one-time lookup (`specs/DEV-545/EVIDENCE.md` §1) and the DEV-518 receipt; fixture ID 90 (evaluation About Time) is not used.

## 3. Source revision of the captured runtime

The artifact records runtime behavior, so the source revision that produced it is the deployed home build, not the capture worktree HEAD. Both are recorded for attribution.

| Field | Value |
|---|---|
| Home promote commit (`origin/env/home` tip, Argo sync revision) | `cd8de7d4971ac61d2a7b22958413defda49fb25c` — `home: promote 8a2deb9b` (2026-11-18) |
| Promoted source commit (`RELEASE.json.commit`) | `8a2deb9b7a971b50108721aa8465620e551f1cfa` (2026-11-18; ancestor of `origin/main`, predates `2f1647b2`) |
| API image digest | `sha256:9f2cffbfd3394dd35849bde9f93bc64d8e01b5f6679fac4bbae68672e4f07924` |
| Web image digest | `sha256:df0ee2b2ec9adf87a9c940a60244d5ec64b91a807349c22fe63ed3524e16d70a` |
| Promoted at (UTC) | `2026-11-18T19:56:10Z` |

Disclosure — the captured runtime predates merged DEV-518. The promoted source `8a2deb9b` sits before `2f1647b2` (DEV-518), and the captured artifact carries the legacy reason vocabulary (`Shared keywords`, `Shared writers`, `Similar runtime`, `Similar rating`, `Features <names>`) with no `Same collection`, `Same director` or `Shared themes` terms. Consequence for T021: the after capture will be produced by a home build that includes DEV-518, so a raw before/after diff mixes the DEV-518 promotion with the DEV-544 change. Before collecting the after evidence, Bernstein/Patron must rule whether a home promote to a DEV-518-inclusive revision and a re-capture of this receipt is required, or whether the mixed-attribution diff is acceptable with the DEV-518 receipt cited alongside. T002 is recorded here as the true pre-change runtime state; no source-revision claim is made that the deployed build matched the implementation baseline.

## 4. Observed baseline first placements (pre-DEV-518 runtime)

Recorded for orientation only; these are the before values the T021 after run must be compared against, not acceptance claims.

| List | Rank 1 | Score |
|---|---|---|
| For you | Good Will Hunting (636) | 0.7167802912849489 |
| Related 575 (A Fistful of Dollars) | The Good, the Bad and the Ugly | 0.8353450423662858 |
| Related 518 (About Time) | Notting Hill | 0.5893684649680211 |
| Related 515 (A Few Good Men) | A Perfect World | 0.6811758567789 |
| Related 519 (Aladdin) | Who Framed Roger Rabbit | 0.7760572031340747 |
| Related 831 (The Wind Rises) | Porco Rosso | 0.9248099887137365 |

Pirate Radio is not first for About Time and Hercules is not first for Aladdin in this pre-change capture, which is the gap SC-001 expects the DEV-544 change to close.

## 5. Pending evidence

| Item | Status |
|---|---|
| T003–T020 implementation, tests and gates | pending |
| T021 after snapshot, before/after diff, coverage table, score-band samples, judged pairs | pending |
| T021 SC-001 first-placement checks under frozen defaults (Pirate Radio for About Time; Hercules for Aladdin) | pending |
| T021 For you leave-one-out unchanged statement, Relation warm <500 ms at 450 movies, tier counts, raw vector payload | pending |

## 6. Sanitization

No credentials, connection strings, tokens, host names beyond the documented `localhost:3100` home origin, or user-identifying paths appear in this receipt or in the artifact. The artifact contains only mode, target IDs, takes, ranks, movie IDs, titles, scores and reason kind/text pairs.
