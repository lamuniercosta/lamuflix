# DEV-427 assumptions

## Q2 [assumed] - Fixture choices

Patron selected an isolated disposable compose database, deterministic seed paths/titles, status=1, enrichment_attempts=0, is_in_watchlist=false and format=mp4. No OMDb identifiers or calls. Verify exactly 500 total rows and 500 distinct seed paths after both seed runs. Basis: DEV-427 lines 5-6; recon-DEV-427 lines 24-35, 57-59. Full exchange: CONCLUSIONS.md Q2.

## Q3 [assumed] - Request mix and pacing

Four browse requests then one title search per VU; pages 1-25, pageSize=20; search page=1 and pageSize=20 with deterministic seed-derived terms; sleep 1s after each request. Omit optional sort and direction (Patron-approved alternative). Basis: DEV-427 line 5; recon-DEV-427 lines 12-16, 33. Full exchange: CONCLUSIONS.md Q3.

## Q4 [assumed] - Measurement semantics

Fixed 50 VUs for 120s; readiness/preflight/optional warmup excluded by scenario tag. Combined-request p95 <=200ms and request error rate <0.01; failure includes transport, non-200, invalid JSON/contract/seed result, counted once per request. Require nonzero browse and search samples; per-type diagnostics do not introduce blocking budgets. Basis: DEV-427 lines 5-6; recon-DEV-427 lines 14,16-17. Full exchange: CONCLUSIONS.md Q4.

## Q5 [assumed] - Docker and runbook

Deliver movies.js, seed.sql and new README.md under scripts/load/. Use in-container psql and pinned grafana/k6, BASE_URL override and bind-mounted artifacts/load/. Docker reachability must be verified, not assumed from localhost inside a container. No host install or manifest/config/compose edits. Basis: DEV-427 lines 5-7; recon-DEV-427 lines 25,37-53. Full exchange: CONCLUSIONS.md Q5.

## Q6 [assumed] - Synthetic fixture naming

For i=1..500, NNNN is four-digit i and GG is two-digit (i-1) modulo 10. Paths /dev427/movie-NNNN.mp4 and titles DEV427 GroupGG MovieNNNN. Each Group00..Group09 search (including mixed-case probes) expects totalCount=50 and 20 matching items; browse expects totalCount=500 and 20 unique IDs per page. Basis: recon-DEV-427 lines 13-16,26-34. Full exchange: CONCLUSIONS.md Q6.

## Q7 [assumed] - Evidence protocol

Use collision-safe per-run summary JSON and an operator receipt (counts after each seed, exact command and exit, run/environment identity and first valid p95). Include measured p95, request error rate, browse/search sample counts, configured VUs/duration/target, threshold outcomes, UTC identity, git HEAD and immutable k6 image in run evidence. Verify unreachable target and invalid fixture fail. Rigger records the first valid measured p95 even when budget fails. Basis: DEV-427 lines 5-7; recon-DEV-427 lines 37-42; AGENTS.md gate rules. Full exchange: CONCLUSIONS.md Q7.

## Q9 - Plan challenge bounded implementation selections

[assumed] Patron ratified leaving deterministic page-wrap and concrete mixed-case Group00-Group09 probe spellings to implementation, recorded in scripts/load/README.md and run evidence within FR-005/FR-006. No new scope or test budget. Basis: CONCLUSIONS.md Q9; recon-DEV-427 lines 13-16,26-34. Database provisioning and all other Q9 decisions are recorded in brief.md; this note does not freeze an uncorrected plan.
