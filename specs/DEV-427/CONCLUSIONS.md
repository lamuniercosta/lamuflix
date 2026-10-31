# DEV-427 grill conclusions

Append-only full decision exchanges. Authority: Patron. Facts: recon-DEV-427. Date: 2026-10-30.

## Q1 - Loop discipline and scope

**Keel question and recommendation:** Q1: Please settle loop discipline. I recommend Critical/High/Medium block code-review and ship-review; Low gets an explicit disposition; cap two review rounds. Freeze scope to manual k6 GET /api/movies browse paging and title search, 500-row idempotent SQL seed without OMDb, 50 VUs for two steady minutes, p95 <=200ms and errors <1%, artifacts/load JSON, first measured p95 recorded by Rigger on ticket; DEV-334 Playwright and pre-baseline tightening excluded; anything else is a follow-up issue, not a finding in this round. Cost: no opportunistic endpoint/performance fixes; a missed budget stays a failed acceptance result until separately authorized. Confirm or amend with cited basis.

**Patron answer:** Confirmed. Standing loop discipline (Critical/High/Medium block, Low dispositioned, 2-round cap) and frozen scope/cost bounds ratified per note DEV-427 lines 5-7 and recon-DEV-427 lines 9-18, 37-53.

**Rationale and implications:** Ticket-defined delivery stays frozen. Critical, High, and Medium block; Low requires disposition. Two review rounds maximum. A failed performance budget remains failed and does not authorize endpoint changes.


---

## Q2 - Seed and isolated baseline

**Keel question and recommendation:** Q2: How do we guarantee the acceptance library is exactly 500 movies while preserving existing data? I recommend a dedicated disposable compose test database, seeded with deterministic DEV-427 library paths and titles, ON CONFLICT (library_path) DO NOTHING, all required columns supplied, status Enriched=1, enrichment_attempts=0, watchlist=false, format mp4, no OMDb IDs or calls. Run seed twice and assert both total count and distinct seed paths remain 500; refuse to call a mixed-data run the acceptance baseline. Never delete existing rows. Cost: operator prepares an isolated database and must confirm exactly 500 before load. Basis: DEV-427 lines 5-6; recon lines 24-35, 41-42, 57-59. Confirm or amend.

**Patron answer:** Confirmed `[assumed]`. Dedicated disposable compose DB seeded with 500 deterministic rows via ON CONFLICT (library_path) DO NOTHING (status=1, enrichment_attempts=0, is_in_watchlist=false, format=mp4, no OMDb), verified at 500 rows after two runs without deleting rows, per DEV-427 lines 5-6 and recon-DEV-427 lines 24-35, 57-59.

**Rationale and implications:** Exactly 500 rows is an acceptance prerequisite; existing data is preserved. Repeat inserts must not duplicate seed paths. No background OMDb enrichment is needed.

---

## Q3 - Request workload

**Keel question and recommendation:** Q3: What reproducible request mix should the 50 VUs exercise? I recommend each VU repeat a deterministic cycle of four browse requests then one title-search request (80/20 by issued requests), pageSize=20, browse cycling pages 1-25, sort=0 (Title), direction=Ascending; searches rotate fixed title substrings whose exact expected matches come from the deterministic seed, with explicit page=1 and pageSize=20. One request per iteration with 1s sleep after each request gives browse pacing while retaining 50 concurrent active VUs; no random data source. Cost: this models paced browsing, not saturation throughput or every filter. Query spelling must use existing contract; do not add or change endpoint semantics. Basis: DEV-427 line 5; recon lines 12-16, 33. Confirm or amend; if direction spelling is not established by recon, choose omitting optional sort/direction rather than inventing a fact.

**Patron answer:** Amended `[assumed]`: accept the 80/20 browse/search cycle (pages 1-25, pageSize=20, 1s sleep, deterministic search substrings with page=1&pageSize=20), but use `sort=Title&direction=Ascending` (or omit both) — `sort=0` is invalid because `LibraryEndpoints.cs:326-330` parses both `sort` and `direction` by SmartEnum name (`MovieSort.TryFromName`, `SortDirection.TryFromName`), not integer value.

**Rationale and implications:** Use the expressly permitted option of omitting both optional sort and direction. The additional parser detail in the answer is not promoted to recon evidence. Four browse iterations then one search iteration per VU, explicit page/pageSize, 1s pacing, deterministic seed-derived search terms. This models browsing rather than saturation.

---

## Q4 - Measurement and failure semantics

**Keel question and recommendation:** Q4: What counts as the two-minute steady measurement? I recommend a fixed 50-VU measurement scenario for exactly 120s, without a ramp inside that window. Any readiness checks or optional warm-up are explicitly outside measured metrics; exclude setup/teardown/preflight traffic via a scenario tag. Require p95 <=200ms (inclusive), errors <1% (strict), using the whole measured browse+search request population; emit separate browse/search diagnostics without inventing extra blocking budgets. Failed request means transport/non-200 response OR invalid JSON/response contract/expected seed result, captured in a custom request-level error rate once per request. Require nonzero measured browse and search samples. Cost: a real 0.5% failure rate remains within the specified budget; no silent tightening to zero errors. Basis: DEV-427 lines 5-6; recon lines 14,16-17. Confirm or amend.

**Patron answer:** Confirmed `[assumed]`. Fixed 50-VU 120s steady measurement scenario (setup/preflight/warmup excluded by tag), blocking thresholds `p(95)<=200` and custom request-level error rate `<0.01` over combined browse+search requests (transport/non-200 or JSON/contract/seed mismatch counted once per request), with non-blocking per-type diagnostics and nonzero browse/search sample checks, per DEV-427 lines 5-6 and recon-DEV-427 lines 14, 16-17.

**Rationale and implications:** Only 120s steady scenario requests contribute to acceptance. Both request types must have samples. Count each failed measured request once. Inclusive latency and strict error boundaries preserve the ticket budget; do not add separate per-type budgets.

---

## Q5 - Execution and file scope

**Keel question and recommendation:** Q5: Which execution path and delivered files should Quill plan? I recommend exactly scripts/load/movies.js, scripts/load/seed.sql, and a new scripts/load/README.md runbook. Use Docker for k6 and in-container psql, no host installation or package manifests; select and record an immutable k6 image reference during implementation using official documentation. Default host-facing target is http://localhost:3000; container k6 must reach the same nginx entry through a documented, verified Docker host/network mapping rather than container localhost. Accept BASE_URL override for that mapping. Runbook covers isolated compose setup, env-supplied credentials without printing them, two seed executions/count receipts, preflight, bind-mounted artifacts/load output, exact run command and exit code. No compose/config/API/schema/CI edits. Cost: manual Docker/network prerequisites; inability to reach the stack is Could not run, never PASS. Basis: recon lines 25,37-51. Confirm or amend.

**Patron answer:** Confirmed `[assumed]`. Plan `scripts/load/movies.js`, `scripts/load/seed.sql`, and `scripts/load/README.md` only; execute via Docker (`docker compose exec -T postgres psql` for seeding and pinned `grafana/k6` image with `BASE_URL` override reaching compose nginx `http://localhost:3000` via Docker host/compose network, bind-mounting `artifacts/load/`), with no compose/API/schema/CI/manifest edits, per DEV-427 lines 5-7 and recon-DEV-427 lines 25, 37-53.

**Rationale and implications:** Three delivery files only, plus Phase A planning/decision artifacts. Use Docker, environment-provided credentials and bind-mounted output. Select the immutable k6 image and verify network mapping during implementation rather than assert missing environment facts now. Existing harness.yml modification belongs to prior work and is preserved.

---

## Q6 - Fixture and response checks

**Keel question and recommendation:** Q6: Shall we freeze a small fixture/check contract so a fast but empty response cannot pass? I recommend rows i=1..500 with library_path /dev427/movie-NNNN.mp4, title DEV427 GroupGG MovieNNNN, zero-padded NNNN=i and GG=(i-1) modulo 10. Ten groups each have exactly 50 titles. Search cycles Group00..Group09 (including mixed-case probes to exercise documented ILIKE); expected totalCount=50 and 20 matching items on page 1. Browse pages 1..25 each expect totalCount=500, 20 valid items with unique IDs within the page; no cross-page stability claim without explicit ordering. Preflight checks exact total library count and these fixture expectations; measured checks validate status, JSON, items/totalCount and matching title membership, counting failures once as Q4. No empty-result search variant or unrelated filters. Cost: synthetic title data is repeatable, not representative production metadata. Basis: recon lines 13-16,26-34. Confirm or amend.

**Patron answer:** Confirmed `[assumed]`. Freeze deterministic 500-row fixture (`library_path` `/dev427/movie-NNNN.mp4`, `title` `DEV427 GroupGG MovieNNNN` across 10 groups of 50), cycling mixed-case `Group00`..`Group09` searches (`totalCount=50`, 20 matching items on page 1) and browse pages 1..25 (`totalCount=500`, 20 unique items per page), validated in preflight and counted once per request in measured error rate, per recon-DEV-427 lines 13-16, 26-34.

**Rationale and implications:** Deterministic i=1..500, NNNN=i padded to four digits and GG=(i-1) modulo 10 padded to two digits. Ten groups of 50 make title-search expectations exact. Every browse response expects 500 total and 20 unique items within its page. No assumption of cross-page ordering. A fast empty payload must fail validation.

---

## Q7 - Verification and evidence

**Keel question and recommendation:** Q7: What evidence and verification closes implementation without expanding this manual tooling ticket? I recommend order: seed/runbook, k6 workload/checks/output, then isolated seed-twice proof and preflight, then actual 50-VU 120s run and bounded negative checks (unreachable target and invalid fixture must exit nonzero; no passing budget on empty metrics). Persist collision-safe per-run summary JSON under artifacts/load with p95, request error rate, counts per request kind, configured VUs/duration/target, threshold outcomes, UTC run identity, git HEAD and pinned k6 image; keep output untracked. Operator receipt records both 500-row counts, command, exit, environment/hardware and first valid measured p95 even if budget fails; route ticket update to Rigger, never write YouTrack ourselves. Use only applicable harness gates; production-C# mutation scope empty is SKIPPED (scope-empty), never PASS; no new C#/React tests or CI wiring. An actual budget miss is FAILED; missing runtime is Could not run and acceptance stays open. Cost: real manual run required before claiming AC; no synthetic PASS. Basis: DEV-427 lines 5-7; recon lines 37-42; seat gate rules. Confirm or amend.

**Patron answer:** Confirmed `[assumed]`. Ratify implementation/verification sequence, untracked collision-safe summary JSON under `artifacts/load/`, bounded negative checks, operator receipt with first valid measured p95 routed via Rigger to YouTrack, and harness gate reporting (`SKIPPED (scope-empty)` on empty C# scope, `Could not run` if stack unreachable, `FAILED` on budget miss), per DEV-427 lines 5-7, recon-DEV-427 lines 37-42, and AGENTS.md verification rules.

**Rationale and implications:** An actual successful manual run and seed-twice receipt are needed for acceptance, not for Phase A drafting. Failure evidence stays failure. Runtime output stays ignored. Only Rigger updates the ticket. No backend/frontend implementation or new test infrastructure follows from this tooling ticket.

---

## Q8 - Shared understanding and closure

**Keel question and recommendation:** Q8 (closure): Do we have shared understanding with Q1-Q7 frozen, including your amendments? Final choices: omit optional sort/direction; exactly the Q6 deterministic 500 rows in isolated compose DB; 80/20 deterministic paced traffic; fixed 50 VUs/120s with inclusive 200ms p95 and strict <1% errors; Docker execution and three delivery files; seed-twice, live baseline, failure-path and JSON/operator evidence; Rigger owns ticket recording; Critical/High/Medium block and two review rounds. I recommend no domain glossary change or ADR: this introduces a test fixture/workload, no new domain concept or hard-to-reverse architecture decision (ticket size M). Quill will draft spec/plan/tasks from brief.md, then Keel analyzes read-only and challenges plan; this grill does not clear Gate 1 or authorize implementation. No ticket contradiction or constitution departure is proposed, so no owner checkbox is opened. Preserve unrelated harness.yml change. Remaining implementation selections (pinned image, verified Docker mapping, run-specific identity) follow Q5/Q7 and do not assert unavailable environment facts. Confirm closure or identify one unresolved decision with cited basis.

**Patron answer:** Confirmed. Shared understanding reached with Q1-Q7 frozen (including omitting optional sort/direction), no glossary/ADR changes, no owner checkboxes, and unrelated harness.yml change preserved, per DEV-427 lines 3-7 and recon-DEV-427 lines 6, 44-53. Grill closed.

**Rationale and implications:** Grill closed after eight questions. Quill drafts from brief.md; later analysis, plan challenge and Gate 1 remain distinct. No glossary or ADR changes, owner escalation or implementation authorization results from this closure.

---

## Q9 - Plan challenge adjudication (2026-10-30)

**Keel question and recommendation:**

Plan challenge ruling needed. All Sentry/Ledger/Compass reports read. Recon 61-111 now establishes provisioning; no further facts needed. Please settle these recommendations with cited basis and report back via maestri ask Keel. (1) Retain Compass F1 Medium at plan:81/T001/T003: choose recon Finding 4 B, a separately named disposable DB in existing compose postgres; change only ignored operator-local docker/api.settings.json Database target, retain ApplyMigrationsOnStartup=true, restart api to apply existing migrations, confirm API/SQL same DB before seed. Preserve settings privately, document temporary UI switch, restore original settings and restart even after failure. No tracked schema/migration edits, no DEV-334 runner reuse, no destructive existing-library cleanup. Clarify file-scope boundaries allow this supported ignored runtime configuration, not tracked API/compose edits. Basis brief Q2/Q5; recon 69-81,93-99. (2) Retain Ledger M1 Medium only as missing constitution applicability evidence: IX table covers existing production test layers, not a blanket exemption because manual/not CI; explicit IX rationale with real compose seed/preflight/baseline/negatives, VI no instrumentation changes, coding/documentation applicability. No departure proposed. (3) Downgrade Ledger M2 to Low documentation reminder: inherited comment policy already binds; add no explanatory JS/SQL comments, rationale in README; no actual offending code exists. (4) Accept Sentry S1/S2 Low as runbook safeguards: credential values only from existing environment/private runtime config, never literal command/history/log/receipt; redacted receipt; pre-insert compose/database identity and empty-or-exact-seed-only guard, transactional seed where practical. (5) Deduplicate Sentry S3 with Ledger L1 Low: require full image@sha256 digest in README/evidence, actual value deferred. (6) Ledger L2 accepted already decided Q5 additive README, exactly three implementation files retained. (7) Ledger L3 reject alleged contradiction: deterministic counters describe workload, not run metadata; prefer explicit operator-supplied run ID/UTC/HEAD via env and no fixture clock/random, document without new helper files. (8) Ledger L4 Low guidance reference only; cite existing k6 skill, no wholesale adoption of example dependencies/thresholds/CI. (9) Compass F2/F3 Low: leave deterministic page-wrap and mixed-case probe spelling as implementation selections recorded in README/evidence, bounded by FR-005 and preflight FR-006. Confirm/amend, and state whether any ticket contradiction or constitution departure needs owner checkbox. I will append full ruling to CONCLUSIONS and brief before Quill edits; plan remains unfrozen.

**Patron answer (verbatim):**

Confirmed: all 9 DEV-427 plan challenge recommendations (Compass F1-F3, Ledger M1-M2 and L1-L4, Sentry S1-S3) are ratified as proposed; no ticket contradiction and no constitution departure exist, so no owner checkbox is opened.

- **Isolation & safeguards (1, 4, 6 — Compass F1 Medium, Sentry S1/S2 Low, Ledger L2 Low)**: Adopt recon Finding 4 B (create a separately named disposable database in the running compose Postgres container via `docker compose exec -T postgres psql`, point the API to it only via gitignored operator-local `docker/api.settings.json` `Database=` with `ApplyMigrationsOnStartup=true`, restart `api` to apply existing migrations, verify API/SQL target identity and empty-or-exact-seed guard before seeding, and restore original `docker/api.settings.json` plus restart `api` even after failure), keeping credentials in env/private runtime config with redacted receipts and retaining the exact 3 tracked delivery files (`scripts/load/movies.js`, `scripts/load/seed.sql`, `scripts/load/README.md`) without tracked compose/API/schema edits or DEV-334 reuse (`CONCLUSIONS.md` Q2/Q5/Q7; `recon-DEV-427` lines 69-81, 93-99; `README.md:55,59,63`; `.gitignore:496`).
- **Constitution, comments & references (2, 3, 5, 8 — Ledger M1/M2, Sentry S3/Ledger L1 Low, Ledger L4 Low)**: Retain M1 only to record explicit Constitution Principle IX/VI/coding/documentation applicability rationale (manual k6 tooling is outside the Principle IX C#/React test-layer table by scope and verified via real compose seed-twice, preflight, 50-VU baseline, and bounded negative checks; no Principle VI instrumentation edits; no constitution departure), downgrade M2 to Low reminder enforcing `AGENTS.md` Non-negotiables (no explanatory JS/SQL comments; rationale in `scripts/load/README.md`), deduplicate S3/L1 requiring full `image@sha256` digest in README/evidence (`CONCLUSIONS.md` Q5/Q7), and cite `.agents/skills/k6-load-testing/SKILL.md` as guidance only without extra dependencies, thresholds, or CI (`CONCLUSIONS.md` Q1/Q4/Q5).
- **Determinism & fixture bounds (7, 9 — Ledger L3, Compass F2/F3 Low)**: Reject Ledger L3 contradiction (deterministic `__VU`/`__ITER` counters govern request workload while run metadata uses explicit operator-supplied env vars for run ID/UTC/HEAD with no fixture clock/random or new helper files per `CONCLUSIONS.md` Q3/Q6/Q7), and leave deterministic browse page-wrap and mixed-case `Group00`..`Group09` search probe spellings `[assumed]` as implementation selections recorded in `scripts/load/README.md`/evidence within FR-005 and FR-006 (`CONCLUSIONS.md` Q6; `recon-DEV-427` lines 13-16, 26-34).

**Rationale and implications:** Two Medium gaps require documentary correction before plan freeze: Compass F1 provisioning and Ledger M1 applicability evidence. Every other finding is dispositioned below and in brief.md. Applying existing migrations to a disposable database is runtime provisioning, not authoring schema or migration changes. No Gate 1 or implementation acceptance is claimed.


---

## Gate 1 Provisional Confirmation (2026-10-30)

- **Ruling**: `gate1: provisional` confirmed and marked on `specs/DEV-427/spec.md:7` (`adjudication-DEV-427-plan:9`, `specs/DEV-427/brief.md`, `CONCLUSIONS.md` Q1-Q9). Ready for Rigger to commit `specs/DEV-427/`, open the spec PR, and set YouTrack state to Gate 1.


---

## Phase 3 Scope Ruling — Explicit `sort=Title&direction=Ascending` on k6 Requests (2026-10-31)

- **Verdict**: Authorize `scripts/load/movies.js` (and `scripts/load/README.md`) to send explicit `sort=Title&direction=Ascending` on all `GET /api/movies` browse and search requests; no owner PR checkbox escalation.
- **Contract basis**: Live `MovieQueryValidator` (`src/LamuFlix.Infrastructure/Library/MovieQueryValidator.cs:13-14`, tested in `tests/LamuFlix.UnitTests/Library/MovieQueryValidatorTests.cs:81-90`) requires `RuleFor(query => query.Sort).NotNull()` and `RuleFor(query => query.Direction).NotNull()`, and `LibraryEndpoints` (`src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs:265-283,326-330`) parses `sort` and `direction` by SmartEnum name (`Title`, `Ascending`), returning HTTP 422 when omitted.
- **Prior ruling & spec consistency**: Patron's original Q3 grill ruling (`specs/DEV-427/CONCLUSIONS.md:30`) already authorized `sort=Title&direction=Ascending` for the 80/20 browse/search cycle; adopting it aligns the k6 workload with the existing consumed endpoint contract as-is (`specs/DEV-427/CONCLUSIONS.md:28,50`; `specs/DEV-427/spec.md:114,121` FR-004/FR-011).
- **No §2.3 escalation**: Sending the required canonical query parameters neither contradicts the DEV-427 ticket text nor departs from the constitution (`note DEV-427` lines 8, 39; Charter §2.3), so no owner checkbox is opened.