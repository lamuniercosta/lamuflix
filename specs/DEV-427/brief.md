# DEV-427 - Manual movie-library load baseline brief

## Status and authority

Phase 2 grill closed on 2026-10-30 after eight questions (maximum twelve). Patron confirmed shared understanding and froze Q1-Q7 with amendments in Q8. This brief is the decision source for Quill to draft spec.md, plan.md and tasks.md; Keel does not draft those artifacts.

Worktree: F:/Dev/LamuFlix.worktrees/feature-427-spec.
Branch: feature/427-spec.
Recon HEAD: 477ac3934f93a09bd0e78e95b7b5ca04bd92aa84 (recon-DEV-427 lines 3-7).
Existing harness.yml modification is unrelated and must be preserved.
Authority: task note DEV-427 lines 3-7; factual source: recon-DEV-427.
Full append-only question/recommendation/answer/rationale exchanges: CONCLUSIONS.md Q1-Q8.
Patron selections tagged [assumed]: ASSUMPTIONS.md Q2-Q7.

No owner checkbox is opened: no ticket contradiction or constitution departure was proposed. Phase A grill closure is not plan freeze, Gate 1 clearance, implementation authorization or an acceptance result.

## Loop discipline

- Closing bar for code-review and ship-review: Critical, High and Medium block; every Low has an explicit disposition. For size M review adjudication, all Sentry, Ledger and Compass axis reports are required.
- Frozen scope: manual k6 GET /api/movies browse paging and title search; an idempotent 500-movie SQL seed without OMDb; 50 VUs for two steady minutes; p95 <=200ms and request error rate <1%; JSON under artifacts/load/; first valid measured p95 recorded on DEV-427 through Rigger. DEV-334 Playwright and pre-baseline budget tightening are excluded; anything else is a follow-up issue, not a finding in this round.
- Round cap: two review rounds. Exhaustion leaves a blocker for Conductor; it does not permit extra rounds or relaxed thresholds.
- A performance-budget miss remains FAILED. It does not authorize endpoint, index or architecture changes.
- Basis: Patron Q1 and Q8; DEV-427 lines 5-7; recon-DEV-427 lines 9-18,37-53; seat review rules.

## Frozen approach

### Seed and isolated baseline

Use a dedicated disposable compose test database without deleting existing rows. The acceptance dataset must contain exactly 500 total movies, not 500 seed rows added to an arbitrary library. Do not alter schema or migrations.

For i=1..500:
- NNNN = i padded to four digits; GG = (i-1) modulo 10 padded to two digits.
- library_path = /dev427/movie-NNNN.mp4.
- title = DEV427 GroupGG MovieNNNN.
- format = mp4; is_in_watchlist = false; status = 1 (Enriched); enrichment_attempts = 0.
- Supply the required non-default columns; use existing database defaults for other required columns. No OMDb IDs or calls.
- Insert with ON CONFLICT (library_path) DO NOTHING.

Run seed.sql twice. After each execution verify exactly 500 total rows and 500 distinct deterministic seed paths. Ten title groups each contain exactly 50 movies. A contaminated/mixed library does not qualify as the acceptance baseline.

Basis: Patron Q2/Q6; DEV-427 lines 5-6; recon-DEV-427 lines 24-35,57-59.

### Workload and response checks

Each VU repeats four browse iterations followed by one search iteration: an 80/20 issued-request cycle. Issue one request per iteration and sleep 1s after each request. Use deterministic counters and seed-derived terms, with no random data source.

Browse cycles pages 1-25 with explicit pageSize=20. Expect HTTP 200, valid JSON matching the existing items/totalCount response, totalCount=500 and 20 valid items with unique IDs within the page. Do not claim cross-page ordering or uniqueness.

Search rotates Group00 through Group09, including mixed-case probes; always send page=1 and pageSize=20. Expect totalCount=50, 20 valid matching-title items and the existing response shape. Omit optional sort and direction on both request kinds. Do not use the initially proposed numeric sort value.

Preflight verifies the exact fixture and response expectations before load. Measured checks cover HTTP status, JSON/contract, counts, and expected seed membership/title matching. No empty-result search variant or extra filters are included.

Basis: Patron Q3 amended, Q6 and Q8; recon-DEV-427 lines 12-16,33. Patron Q3 mentioned additional parser detail; the selected omission avoids relying on that detail as a new recon fact.

### Measurement and error semantics

Use a fixed 50-VU scenario for exactly 120 seconds of steady measurement, with no ramp in that window. Readiness, setup, teardown, preflight and any optional warm-up must be excluded from acceptance metrics using the measurement scenario tag.

Acceptance uses the combined measured browse+search population:
- p95 <=200ms, inclusive.
- Custom request-level error rate <0.01, strict.
- Require nonzero measured browse and search samples.
- Count a request as failed once if it has a transport failure, non-200 response, invalid JSON/contract or incorrect expected seed result.
- Emit per-type latency/error/sample diagnostics without adding per-type blocking performance budgets.

A real error rate below 1% remains within the specified budget. Empty metric sets or missing traffic types cannot pass. Distinguish invalid preflight, unrunnable environment and a measured budget failure.

Basis: Patron Q4/Q7; DEV-427 lines 5-6; recon-DEV-427 lines 14,16-17.

### Execution and output

Use Docker; no host k6/psql installation or package manifest changes. Select an immutable grafana/k6 image reference during implementation using official documentation and record it in run evidence. Use in-container psql via docker compose exec -T postgres psql with environment-provided database credentials; do not commit or print credentials.

The host-facing target is http://localhost:3000 via compose nginx. The k6 container must reach that same nginx entry through a documented and verified Docker host/network mapping; container localhost is not assumed to reach the host. Support BASE_URL override. Exact image selection and verified mapping are implementation selections, not asserted current environment facts.

Bind-mount artifacts/load/ and create collision-safe per-run summary JSON there. Runtime output remains ignored/untracked. Run evidence must include p95, custom error rate, browse/search sample counts, configured VUs/duration/target, threshold outcomes, UTC run identity, git HEAD and immutable k6 image. The runbook must make those metadata inputs/receipts reproducible without new helper files.

An operator receipt also records the two seed count results, exact command, process exit, environment/hardware and first valid measured p95, including when a valid baseline misses budget. Route the ticket recording request and verified receipt to Rigger. Keel and Builder do not write YouTrack.

Basis: Patron Q5/Q7; recon-DEV-427 lines 25,37-53; DEV-427 lines 5-7.

## Files touched and boundaries

Implementation delivery is exactly:
1. scripts/load/seed.sql - deterministic idempotent existing-table fixture.
2. scripts/load/movies.js - manual k6 workload, preflight/checks, tagged metrics, thresholds and summary output.
3. scripts/load/README.md - isolated setup, credential-safe commands, verified network mapping, repeat-seed assertions, pinned image and baseline/evidence instructions.

Phase A records are specs/DEV-427/brief.md, CONCLUSIONS.md and ASSUMPTIONS.md; Quill subsequently drafts the feature specification, plan and tasks from this brief.

Do not edit compose, CI, API, schema, manifests, generated contracts or harness configuration for this delivery. Preserve unrelated harness.yml modification. No backend/frontend behavior changes, new architectural layer, production dependency or LocalPlay change is authorized.

## Task ordering and test strategy

1. Draft seed and runbook using existing columns and container tooling.
2. Implement the deterministic k6 workload, preflight, response/error checks, tagged measurement and JSON output.
3. Prepare an isolated database; seed twice and retain both 500-row/500-distinct-path receipts.
4. Verify container reachability through nginx and run preflight for the exact fixture.
5. Run the actual 50-VU, 120s steady acceptance workload and retain output, threshold verdicts, command and exit.
6. Exercise bounded negative cases: unreachable target and invalid fixture must produce nonzero exit; empty metrics must not yield a passing acceptance result.
7. Route first valid measured p95 and supporting receipt to Rigger for ticket recording and verified read-back.
8. Complete applicable review/gates under the frozen closing bar and two-round cap. The user owns merging.

No new C# or React tests, test infrastructure or CI wiring. Verification is the seed-twice proof, k6 preflight/response checks, actual measured baseline and bounded negative checks. Do not claim acceptance from syntax checks or synthetic output.

## Gate expectations and failure reporting

Apply harness gates only at their applicable stage/scope. Do not substitute dotnet build or unrelated test suites for load acceptance. No gates or live load runs were executed during this grill.

- Applicable gate exit 0 can be reported as PASS only with its actual receipt.
- Empty production-C# mutation scope: SKIPPED (scope-empty), never PASS.
- An explicit disabled gate is SKIP, never PASS.
- Missing/unreachable runtime: Could not run; acceptance remains open.
- Measured budget miss: FAILED; retain evidence and route the issue through Conductor/Patron without widening scope or tightening/relaxing the ticket budget.
- No threshold reductions, CI enrollment or pre-baseline budget tightening.

The baseline must actually meet the ticket budget before acceptance can be claimed, and seed-twice plus ticket p95 receipts are separately required.

## Vocabulary, ADRs and next phase

Retain existing movie-library vocabulary. Group labels and seed paths are test fixture identifiers, not new domain concepts. Patron Q8 confirmed no CONTEXT.md changes and no ADR: this size-M tooling feature introduces no hard-to-reverse architectural choice.

Quill drafts spec.md, plan.md and tasks.md from this brief. Keel subsequently runs read-only speckit-analyze, checks plan/tasks against this brief and sends one numbered fix list per round. Plan challenge/freeze and Gate 1 remain future work. No structural escalations or unresolved planning decisions remain; runtime validation and exact immutable image selection are implementation work.

## Every grill answer (verbatim)

### Q1

Confirmed. Standing loop discipline (Critical/High/Medium block, Low dispositioned, 2-round cap) and frozen scope/cost bounds ratified per note DEV-427 lines 5-7 and recon-DEV-427 lines 9-18, 37-53.

### Q2

Confirmed `[assumed]`. Dedicated disposable compose DB seeded with 500 deterministic rows via ON CONFLICT (library_path) DO NOTHING (status=1, enrichment_attempts=0, is_in_watchlist=false, format=mp4, no OMDb), verified at 500 rows after two runs without deleting rows, per DEV-427 lines 5-6 and recon-DEV-427 lines 24-35, 57-59.

### Q3

Amended `[assumed]`: accept the 80/20 browse/search cycle (pages 1-25, pageSize=20, 1s sleep, deterministic search substrings with page=1&pageSize=20), but use `sort=Title&direction=Ascending` (or omit both) — `sort=0` is invalid because `LibraryEndpoints.cs:326-330` parses both `sort` and `direction` by SmartEnum name (`MovieSort.TryFromName`, `SortDirection.TryFromName`), not integer value.

### Q4

Confirmed `[assumed]`. Fixed 50-VU 120s steady measurement scenario (setup/preflight/warmup excluded by tag), blocking thresholds `p(95)<=200` and custom request-level error rate `<0.01` over combined browse+search requests (transport/non-200 or JSON/contract/seed mismatch counted once per request), with non-blocking per-type diagnostics and nonzero browse/search sample checks, per DEV-427 lines 5-6 and recon-DEV-427 lines 14, 16-17.

### Q5

Confirmed `[assumed]`. Plan `scripts/load/movies.js`, `scripts/load/seed.sql`, and `scripts/load/README.md` only; execute via Docker (`docker compose exec -T postgres psql` for seeding and pinned `grafana/k6` image with `BASE_URL` override reaching compose nginx `http://localhost:3000` via Docker host/compose network, bind-mounting `artifacts/load/`), with no compose/API/schema/CI/manifest edits, per DEV-427 lines 5-7 and recon-DEV-427 lines 25, 37-53.

### Q6

Confirmed `[assumed]`. Freeze deterministic 500-row fixture (`library_path` `/dev427/movie-NNNN.mp4`, `title` `DEV427 GroupGG MovieNNNN` across 10 groups of 50), cycling mixed-case `Group00`..`Group09` searches (`totalCount=50`, 20 matching items on page 1) and browse pages 1..25 (`totalCount=500`, 20 unique items per page), validated in preflight and counted once per request in measured error rate, per recon-DEV-427 lines 13-16, 26-34.

### Q7

Confirmed `[assumed]`. Ratify implementation/verification sequence, untracked collision-safe summary JSON under `artifacts/load/`, bounded negative checks, operator receipt with first valid measured p95 routed via Rigger to YouTrack, and harness gate reporting (`SKIPPED (scope-empty)` on empty C# scope, `Could not run` if stack unreachable, `FAILED` on budget miss), per DEV-427 lines 5-7, recon-DEV-427 lines 37-42, and AGENTS.md verification rules.

### Q8

Confirmed. Shared understanding reached with Q1-Q7 frozen (including omitting optional sort/direction), no glossary/ADR changes, no owner checkboxes, and unrelated harness.yml change preserved, per DEV-427 lines 3-7 and recon-DEV-427 lines 6, 44-53. Grill closed.


## Plan challenge decisions - Q9 (2026-10-30)

Patron ratified all nine Keel recommendations, with cited basis, in CONCLUSIONS.md Q9. This additive section governs Quill corrections; Q1-Q8 remain preserved. All three axis reports are present. Current verdict: NEEDS FIXES, not frozen. Closing bar and two-round cap remain unchanged; this is the first plan challenge adjudication, not a new reviewer round.

1. **Compass F1 - retain Medium, correction required.** Use recon Finding 4 B: a separately named disposable database in the existing compose postgres container. Create via in-container psql, select it through ignored operator-local docker/api.settings.json, keep Database.ApplyMigrationsOnStartup=true, restart api to apply existing committed migrations, and verify API and SQL target the same database before insert. Document temporary API/UI redirection. Preserve original settings privately and restore/restart even on failure. Never delete existing library rows or reuse the DEV-334 runner. Basis: recon 69-81,93-99; Q2/Q5/Q7. The three-file delivery boundary concerns tracked output: ignored runtime configuration is allowed. No new migration/schema definition is authored. Spec US1 AC4 must express that distinction consistently.
2. **Ledger M1 - retain Medium only for missing applicability evidence, correction required.** Constitution IX production test-layer table is outside the scope of these manual tooling files; manual/not CI alone is not an exemption. Retain real compose seed-twice, preflight, measured 50-VU/120s run and bounded negatives as verification. Explicitly document VI applicability (no instrumentation change) and coding/documentation rules. No departure; no owner checkbox. Basis: constitution VI/IX; Q1/Q5/Q7.
3. **Ledger M2 - downgrade to Low; accept reminder.** No offending implementation comments exist in this planning slice. Add plan/task reminder: no explanatory comments in movies.js or seed.sql; rationale belongs in README prose. Existing policy already applies. Basis: AGENTS Non-negotiables; Q9.
4. **Sentry S1 - accept Low safeguard.** Runbook uses existing environment/private runtime configuration, no credential literals in CLI/history or output; receipts redact sensitive values. Exact safe invocation is an implementation deliverable. Basis: FR-008, recon 25,49,76-81; Q5/Q9.
5. **Sentry S2 - accept Low safeguard.** Before inserting, verify compose project/container/database identity, same API/SQL target, and empty-or-exact-seed-only movies precondition. Seed transactionally where practical. Preserve existing library. Basis: FR-003, Q2/Q9.
6. **Sentry S3 / Ledger L1 - merge duplicate Low findings; accept digest clarification.** Require full grafana/k6 image@sha256 digest in README and run evidence. Tag may be a label, never the immutable identity. Actual digest remains an implementation selection. Basis: FR-004/FR-009, Q5/Q9.
7. **Ledger L2 - accepted existing Q5 decision, no scope defect.** README is the already authorized additive third file; exactly three tracked implementation files remain. Preserve Q5 history and cite Q9 clarification. Basis: Q5, FR-001/SC-006, Q9.
8. **Ledger L3 - reject claimed determinism contradiction.** Deterministic counters govern fixture/request selection, not UTC run identity. Use explicit operator-supplied run ID/UTC/HEAD metadata via environment and document commands without new helper files. No ambient clock or randomness for fixture selection. Basis: Q3/Q6/Q7/Q9, FR-005/FR-009.
9. **Ledger L4 - accept Low guidance reference only.** Cite .agents/skills/k6-load-testing/SKILL.md for implementation guidance; frozen ticket metrics, dependencies and manual-only scope prevail over examples. No additional dependencies, thresholds, helper files or CI. Basis: recon 39, Q1/Q4/Q5/Q9.
10. **Compass F2 - accept Low as deferred bounded selection.** Deterministic browse-page wrap is selected in implementation and recorded in README/evidence; retain pages 1-25 and four browse then one search. No cross-page uniqueness claim. Basis: FR-005/FR-006, Q3/Q9.
11. **Compass F3 - accept Low as deferred bounded selection.** Concrete mixed-case Group00-Group09 probe spellings are selected in implementation and recorded in README/evidence, covered by preflight matching expectations. Basis: recon 13-16, FR-005/FR-006, Q6/Q9.

Patron explicitly records zero ticket contradictions, zero constitution departures and zero owner checkboxes. Quill updates spec/plan/tasks from these decisions; Keel checks corrections before any freeze declaration. No production writes, runtime validation, gate PASS, Gate 1 clearance or merge authorization is implied.

## Plan freeze receipt - 2026-10-30 - Keel

Correction round 1 was independently read back from saved spec.md, plan.md and tasks.md on branch feature/427-spec. This receipt supersedes the preceding pending-correction verdict; prior decisions remain preserved. All three axis reports were adjudicated under Patron Q9.

- Compass F1 Medium CLOSED: plan.md:19,41,43,86,91; spec.md:30,38,41,111-113; tasks.md T001/T003/T010. Existing migrations, ignored runtime settings, same-target and pre-insert guards, temporary UI switch, private settings preservation and restore/restart on failure are explicit.
- Ledger M1 Medium CLOSED: plan.md:45-52 explicitly records IX/VI/coding/documentation applicability and real-stack proofs, without a manual-only exemption or constitution departure.
- Ledger M2 Low disposition VERIFIED: plan.md:29,47,85,87 and tasks T002/T004/Notes enforce the comment boundary.
- Sentry S1/S2 Low dispositions VERIFIED: plan.md:86,91; spec FR-003/FR-008; tasks T001/T003/T010 preserve credential safety, redaction, target identity, seed-only preconditions and transactional seeding where practical.
- Sentry S3/Ledger L1 duplicate Low disposition VERIFIED: plan.md:17,90; spec FR-004/FR-009; tasks T008/T009 require digest identity, actual value deferred.
- Ledger L2 existing-authority disposition VERIFIED: plan.md:29,43,81; spec FR-001/SC-006 preserve Q5 additive README and exactly three tracked files.
- Ledger L3 rejection and metadata clarification VERIFIED: plan.md:29,87,92; spec FR-005/FR-009; tasks T004/T008/T009 distinguish deterministic workload and explicit run metadata.
- Ledger L4 guidance-only disposition VERIFIED: plan.md:17,87; spec FR-004; tasks T004/Notes preserve frozen metrics and scope over examples.
- Compass F2/F3 bounded implementation-selection dispositions VERIFIED: plan.md:87; spec FR-005/FR-006 and Assumptions; tasks T004/T006/Notes retain deterministic wrap and mixed-case probe recording within existing expectations.

Verdict: CLEAN for plan challenge; PLAN FROZEN. All 12 original finding IDs have verified dispositions; no open Critical/High/Medium plan findings. No new review round was opened; the two-round cap remains unchanged. Patron Q9 records zero ticket contradictions, zero constitution departures and zero owner checkboxes. Draft status text in Quill-owned documents is administrative and will be synchronized to this receipt.

Gate 1 remains pending user merge of the spec PR. This is not implementation authorization, a runtime acceptance result, gate PASS, code-review clearance or merge-bar clearance. No production code or runtime configuration was changed during this documentary check.
