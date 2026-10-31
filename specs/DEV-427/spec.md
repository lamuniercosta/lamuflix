# Feature Specification: DEV-427 Manual movie-library load baseline

**Feature Branch**: `feature/427-spec`

**Created**: 2026-10-30

**Status**: `gate1: provisional` — Plan challenge verified and plan frozen on 2026-10-30 per brief Plan freeze receipt. Gate 1 remains pending user merge of the spec PR; no implementation authorization.

**Input**: Ticket DEV-427 (size M); settled grill brief `specs/DEV-427/brief.md` (grill closed at Q8/12; plan-challenge decisions in Q9); facts from note `recon-DEV-427`; Patron rulings in `specs/DEV-427/CONCLUSIONS.md` and `[assumed]` selections in `specs/DEV-427/ASSUMPTIONS.md`. No decisions beyond `brief.md`.

## Clarifications

### Session 2026-10-30

The grill is closed. Plan-challenge Q9 dispositions are recorded in `brief.md` / `CONCLUSIONS.md`. Each answer below is already the body of this spec. Status corrections remain pending Keel verification; this draft does not declare freeze or Gate 1.

- Q: What loop discipline and scope bound this delivery? → A: Critical/High/Medium block; every Low has an explicit disposition; two review rounds; frozen scope is manual k6 GET `/api/movies` browse paging and title search, idempotent 500-movie SQL seed without OMDb, 50 VUs for two steady minutes, p95 <=200ms and request error rate <1%, JSON under `artifacts/load/`, first valid measured p95 recorded on DEV-427 through Rigger. DEV-334 Playwright and pre-baseline budget tightening are excluded (brief Q1/Q8/Q9).
- Q: How is the acceptance library prepared? → A: Separately named disposable database in the existing compose postgres (recon Finding 4 B); create via credential-safe in-container psql; select via ignored operator-local `docker/api.settings.json` with `ApplyMigrationsOnStartup=true`; restart `api` so existing committed migrations apply; prove same API/SQL target and empty-or-exact-seed-only guard before insert; seed transactionally where practical; exactly 500 deterministic movies via `ON CONFLICT (library_path) DO NOTHING`; seed twice; verify 500 total rows and 500 distinct seed paths after each run without deleting existing-library rows; restore original settings and restart `api` even on failure; no OMDb; no DEV-334 runner reuse (brief Q2/Q6/Q9).
- Q: What request mix and paging rules apply? → A: Each VU issues four browse then one search (80/20), sleeps 1s after each request, browses pages 1–25 with `pageSize=20`, searches Group00–Group09 with mixed-case probes at `page=1&pageSize=20`, and omits optional `sort` and `direction` (superseded by Phase 3 Scope Ruling in `CONCLUSIONS.md`, 2026-10-31: explicit `sort=Title&direction=Ascending`). Deterministic page-wrap order and concrete mixed-case probe spellings are bounded implementation selections recorded in README/evidence; FR-005/FR-006 stay unchanged (brief Q3 amended / Q8/Q9).
- Q: How is performance measured and failed? → A: Fixed 50-VU, 120s steady scenario; readiness/setup/teardown/preflight/optional warm-up excluded by measurement tag; combined browse+search p95 <=200ms inclusive and custom request-level error rate <0.01 strict; nonzero browse and search samples required; one failure count per request for transport, non-200, invalid JSON/contract, or incorrect seed result (brief Q4).
- Q: Which files and runtime path are allowed? → A: Exactly three tracked implementation files — `scripts/load/seed.sql`, `scripts/load/movies.js`, and additive `scripts/load/README.md` (Q5; Q9 L2 clarification). Docker in-container `psql` and digest-pinned `grafana/k6` (`image@sha256:…`; tag label only) with `BASE_URL` override to compose nginx `http://localhost:3000`; bind-mount `artifacts/load/`. Ignored operator-local `docker/api.settings.json` may be retargeted/restored for disposable-DB selection. No tracked compose/API/schema/CI/manifest edits; no NEW/authored migration or schema definition; existing migrations may be applied at runtime during provisioning (brief Q5/Q9).
- Q: What evidence and negatives are required? → A: Collision-safe summary JSON, operator receipt with seed counts, redacted command/exit, explicit env-supplied run ID/UTC/HEAD, digest-pinned image, and first valid measured p95 routed via Rigger; unreachable target and invalid fixture must exit nonzero; empty metrics must not pass; harness skips and budget miss reported without renaming to PASS (brief Q7/Q9).

## User Scenarios & Testing *(mandatory)*

The actor is an operator running the manual load baseline against an isolated DEV-325 compose stack. There is no CI enrollment for this delivery.

### User Story 1 - Idempotent 500-movie seed on an isolated database (Priority: P1)

An operator prepares a separately named disposable database in the existing compose postgres (recon Finding 4 B), applies existing committed migrations by retargeting ignored operator-local `docker/api.settings.json` with `ApplyMigrationsOnStartup=true` and restarting `api`, then applies `scripts/load/seed.sql` twice. After each run the disposable library contains exactly 500 movies and exactly 500 distinct deterministic seed paths. No existing-library rows are deleted. Original settings are restored and `api` restarted even on failure. No OMDb identifiers or calls are involved.

**Why this priority**: The ticket's acceptance dataset. Browse and search checks are meaningless on a contaminated or differently sized library.

**Independent Test**: Against the Finding 4 B disposable database with existing migrations applied, run the seed twice and verify both 500-row and 500-distinct-path receipts after each execution. No k6 run is required.

**Acceptance Scenarios**:

1. **Given** a separately named disposable compose database whose existing `movies` schema was provisioned by applying already-committed migrations at API startup (Finding 4 B), **When** `scripts/load/seed.sql` inserts movies `i=1..500` with `library_path=/dev427/movie-NNNN.mp4`, `title=DEV427 GroupGG MovieNNNN`, `format=mp4`, `is_in_watchlist=false`, `status=1`, `enrichment_attempts=0` using `ON CONFLICT (library_path) DO NOTHING`, **Then** the table has exactly 500 total movies and 500 distinct deterministic seed paths.
2. **Given** that seeded database, **When** the same seed script runs a second time without deleting rows, **Then** the counts remain exactly 500 total rows and 500 distinct seed paths (no duplicates).
3. **Given** the seeded titles, **When** groups are inspected, **Then** each of Group00 through Group09 contains exactly 50 movies.
4. **Given** this delivery, **When** seeding completes, **Then** no NEW/authored migration or schema definition, OMDb call, or tracked compose/API/schema edit was required; existing committed migrations may have been applied during runtime provisioning; ignored operator-local configuration may have been retargeted and restored; tracked implementation output remains exactly the three `scripts/load/` files.

---

### User Story 2 - Deterministic browse and search workload with preflight (Priority: P1)

An operator runs the k6 script against compose nginx. Preflight confirms the exact 500-movie fixture and response expectations. Each virtual user then issues the frozen 80/20 browse/search cycle with explicit paging and seed-derived search terms.

**Why this priority**: The ticket's workload definition and the guard that prevents measuring an empty or wrong fixture.

**Independent Test**: With the US1 fixture in place, run preflight alone (or a short tagged readiness path). Preflight fails on wrong counts or contract mismatch and passes only when browse and search expectations match the fixture. Steady measurement is not required for this story's independent test.

**Acceptance Scenarios**:

1. **Given** the US1 fixture reachable through compose nginx at `http://localhost:3000`, **When** preflight runs, **Then** it verifies browse pages expect `totalCount=500` with 20 unique item IDs per page and search expects `totalCount=50` with 20 matching-title items on page 1 for Group00–Group09 (including mixed-case probes).
2. **Given** a measurement iteration, **When** a VU issues requests, **Then** it performs four browse requests (pages cycling 1–25, `pageSize=20`) then one search (`page=1`, `pageSize=20`, deterministic group term), sleeping 1s after each request, with no random data source and with optional `sort`/`direction` omitted.
3. **Given** any browse or search response under check, **When** status, JSON shape, counts, or seed membership/title matching are wrong, **Then** that request is counted as one failure for the custom error rate.
4. **Given** absent `page`/`pageSize`, **When** the existing API would return empty items, **Then** the script never relies on that path: every issued request sends explicit paging.

---

### User Story 3 - Measured baseline under ticket budgets with retained evidence (Priority: P1)

An operator runs the fixed 50-VU, 120-second steady measurement scenario. Acceptance metrics exclude setup, teardown, preflight, and optional warm-up via the measurement tag. The run retains collision-safe summary JSON and an operator receipt including the first valid measured p95.

**Why this priority**: The ticket's performance budget and the evidence the ticket must record.

**Independent Test**: After US1 and US2 preflight succeed, run the 50-VU / 120s tagged measurement once. Judge p95, custom error rate, nonzero browse and search samples, threshold outcomes, and retained artifacts without CI.

**Acceptance Scenarios**:

1. **Given** a ready fixture and verified nginx reachability, **When** the measurement scenario runs for exactly 120s at 50 VUs with no ramp in that window, **Then** acceptance metrics cover only tagged measured browse+search traffic.
2. **Given** that measured population, **When** thresholds are evaluated, **Then** p95 is <=200ms inclusive, custom request-level error rate is <0.01 strict, and both browse and search sample counts are nonzero.
3. **Given** a completed measured run (pass or budget miss), **When** artifacts are written, **Then** collision-safe summary JSON under `artifacts/load/` and the operator receipt include measured p95, custom error rate, browse/search sample counts, configured VUs/duration/target, threshold outcomes, explicit env-supplied run ID/UTC identity/git HEAD, digest-pinned k6 image (`image@sha256:…`), seed-count receipts, redacted command, process exit, and environment/hardware notes.
4. **Given** a first valid measured p95, **When** ticket recording is requested, **Then** the receipt is routed to Rigger for YouTrack write-back and verified read-back; Keel and Builder do not write YouTrack.

---

### User Story 4 - Bounded negative paths and gate reporting (Priority: P2)

An operator proves that unreachable targets and invalid fixtures fail closed, that empty metric sets cannot pass acceptance, and that harness gate exits are reported by their real names.

**Why this priority**: Protects against false green from syntax checks, synthetic output, or mislabeled skips.

**Independent Test**: Force an unreachable `BASE_URL` and an invalid fixture separately; confirm nonzero exits. Confirm empty metrics are not treated as a passing acceptance result. Record applicable gate exits without renaming SKIPPED/SKIP/Could not run/FAILED to PASS.

**Acceptance Scenarios**:

1. **Given** an unreachable target, **When** the load entry runs, **Then** the process exits nonzero and acceptance remains open (`Could not run` when the stack is missing/unreachable).
2. **Given** an invalid fixture relative to preflight expectations, **When** preflight or the run evaluates that fixture, **Then** the process exits nonzero and does not claim a passing baseline.
3. **Given** empty measured metrics or a missing browse/search traffic type, **When** acceptance is judged, **Then** the result is not a pass.
4. **Given** applicable harness gates on this no-production-C# change, **When** exits are recorded, **Then** empty C# mutation scope is `SKIPPED (scope-empty)`, an explicit disabled gate is `SKIP`, a budget miss is `FAILED` with evidence retained, and none of those are reported as PASS.

### Edge Cases

- A mixed or contaminated library that is not exactly 500 deterministic seed movies does not qualify as the acceptance baseline.
- Missing explicit `page`/`pageSize` yields empty items from the existing API; the script must always send them.
- Numeric `sort=0` is invalid on the existing endpoint; the script omits `sort` and `direction` (superseded by Phase 3 Scope Ruling in `CONCLUSIONS.md`, 2026-10-31: explicit `sort=Title&direction=Ascending`).
- Container `localhost` is not assumed to reach host-published nginx; Docker host/network mapping must be documented and verified.
- Credentials for Postgres come only from existing environment/private runtime configuration; they are never CLI literals, never written into shell history or logs, never committed, and receipts redact them.
- Before the first seed insert, compose project/container/database identity and empty-or-exact-seed-only precondition must hold; API and SQL must target the same disposable database.
- While `docker/api.settings.json` is retargeted, compose UI/API serve the disposable fixture; original settings must be restored and `api` restarted even after failure.
- A real measured error rate below 1% is within budget; a budget miss does not authorize endpoint, index, architecture, CI, or budget changes.
- Syntax-only checks or synthetic JSON are not acceptance evidence.
- Unrelated `harness.yml` modification in the worktree is preserved and is not part of this delivery.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Delivery MUST add exactly three tracked implementation files: `scripts/load/seed.sql`, `scripts/load/movies.js`, and additive `scripts/load/README.md` (Q5; Q9 L2 clarification — README remains the authorized third file without rewriting Q5 history). Planning records remain under `specs/DEV-427/`. The change MUST NOT edit tracked compose, CI, API, schema, manifests, generated contracts, or harness configuration. Ignored operator-local `docker/api.settings.json` MAY be temporarily retargeted and MUST be restored per Finding 4 B. The unrelated existing `harness.yml` modification MUST be preserved.
- **FR-002**: `scripts/load/seed.sql` MUST insert exactly 500 deterministic movies into the existing `movies` table for `i=1..500` where `NNNN` is four-digit `i` and `GG` is two-digit `(i-1) mod 10`, with `library_path=/dev427/movie-NNNN.mp4`, `title=DEV427 GroupGG MovieNNNN`, `format=mp4`, `is_in_watchlist=false`, `status=1` (Enriched), `enrichment_attempts=0`, using `ON CONFLICT (library_path) DO NOTHING`. It MUST supply required non-default columns and rely on existing database defaults for other required columns. It MUST NOT call OMDb or insert OMDb identifiers. It MUST NOT author a NEW migration or schema definition. Applying already-committed migrations during disposable-DB runtime provisioning is allowed and is not a schema change authored by this delivery. `movies.js` and `seed.sql` MUST NOT carry explanatory comments; rationale belongs in README prose.
- **FR-003**: Acceptance seeding MUST use a separately named disposable database in the existing compose postgres (recon Finding 4 B): create via credential-safe in-container psql; select via ignored `docker/api.settings.json` with `ApplyMigrationsOnStartup=true`; restart `api` so existing committed migrations apply; prove compose project/container/database identity, same API/SQL target, and empty-or-exact-seed-only precondition before insert; seed transactionally where practical; document temporary UI/API redirection; preserve original settings privately and restore plus restart `api` even on failure. After each of two seed executions without deleting existing-library rows, verification MUST show exactly 500 total movies and 500 distinct deterministic seed paths. Each title group Group00–Group09 MUST contain exactly 50 movies. A contaminated/mixed library MUST NOT be treated as the acceptance baseline. DEV-334 runner reuse and existing-library deletion are forbidden.
- **FR-004**: `scripts/load/movies.js` MUST implement the manual k6 workload against GET `/api/movies` through compose nginx, support `BASE_URL` override, and default the host-facing target to `http://localhost:3000`. Execution MUST use a digest-pinned `grafana/k6` image (`image@sha256:…` required in README and run evidence; any tag is a label only). The actual digest value is an implementation selection from official documentation. Host k6/psql installs and package-manifest changes MUST NOT be required. `.agents/skills/k6-load-testing/SKILL.md` MAY guide style; ticket/frozen metrics and manual-only scope prevail — no example-driven dependencies, budgets, helpers, or CI.
- **FR-005**: Each VU MUST repeat four browse iterations then one search iteration, issue one request per iteration, and sleep 1s after each request, using deterministic counters and seed-derived terms with no ambient clock or random data source for fixture/request selection. Browse MUST cycle pages 1–25 with explicit `pageSize=20`. Search MUST rotate Group00–Group09 including mixed-case probes with explicit `page=1` and `pageSize=20`. Optional `sort` and `direction` MUST be omitted (superseded by Phase 3 Scope Ruling in `CONCLUSIONS.md`, 2026-10-31: explicit `sort=Title&direction=Ascending`). Deterministic page-wrap order and concrete mixed-case probe spellings are bounded implementation selections recorded in README/evidence; this FR text is otherwise unchanged.
- **FR-006**: Preflight MUST verify the exact fixture and response expectations before load. Measured and preflight checks MUST cover HTTP status, JSON/contract shape (`items`/`totalCount`), counts, and expected seed membership/title matching. Browse expectations: `totalCount=500`, 20 valid items with unique IDs within the page. Search expectations: `totalCount=50`, 20 valid matching-title items on page 1. Cross-page ordering or uniqueness MUST NOT be claimed. No empty-result search variant or extra filters are included. Concrete probe spellings remain bounded implementation selections under this FR (unchanged expectations).
- **FR-007**: Measurement MUST be a fixed 50-VU scenario for exactly 120 seconds of steady load with no ramp in that window. Readiness, setup, teardown, preflight, and optional warm-up MUST be excluded from acceptance metrics using the measurement scenario tag. Acceptance MUST use the combined measured browse+search population with p95 <=200ms inclusive, custom request-level error rate <0.01 strict, and nonzero measured browse and search samples. A request MUST count as failed once for transport failure, non-200 response, invalid JSON/contract, or incorrect expected seed result. Per-type latency/error/sample diagnostics MUST be emitted without adding per-type blocking performance budgets.
- **FR-008**: Seeding MUST run via `docker compose exec -T postgres psql` with credentials taken only from existing environment/private runtime configuration — never CLI literals, shell history, or logs. Operator receipts MUST redact sensitive values. The k6 container MUST reach the same nginx entry the host uses at `http://localhost:3000` through a documented and verified Docker host/network mapping. `artifacts/load/` MUST be bind-mounted. Runtime output MUST remain ignored/untracked. Credentials MUST NOT be committed or printed.
- **FR-009**: Each run MUST write collision-safe per-run summary JSON under `artifacts/load/`. Run evidence MUST include measured p95, custom error rate, browse/search sample counts, configured VUs/duration/target, threshold outcomes, explicit env-supplied run ID/UTC identity/git HEAD (no ambient clock for fixture selection), and the digest-pinned k6 image (`image@sha256:…`; tag label only). The operator receipt MUST also record both seed-count results, redacted command, process exit, environment/hardware, and first valid measured p95 (including when a valid baseline misses budget). The runbook MUST make those metadata inputs/receipts reproducible without new helper files. First valid measured p95 recording on ticket DEV-427 MUST be routed through Rigger with verified read-back.
- **FR-010**: Unreachable target and invalid fixture paths MUST exit nonzero. Empty metric sets or missing browse/search traffic types MUST NOT yield a passing acceptance result. Syntax checks or synthetic output MUST NOT be claimed as acceptance.
- **FR-011**: The change MUST NOT modify backend or frontend behavior, add a new architectural layer, add a production NuGet/npm dependency, touch `Features:LocalPlay`, touch secrets in source, or use `Process.Start`. DEV-334 Playwright work, DEV-334 runner reuse, existing-library deletion, authored migrations/schema definitions, and pre-baseline budget tightening are out of scope. A performance-budget miss remains FAILED and MUST NOT authorize endpoint, index, architecture, CI enrollment, threshold reduction, or budget changes.
- **FR-012**: Applicable harness gates MUST be reported from their script exits. Exit 0 is PASS only with its actual receipt. Empty production-C# mutation scope is `SKIPPED (scope-empty)`, never PASS. An explicit disabled gate is `SKIP`, never PASS. Missing/unreachable runtime is `Could not run` with acceptance remaining open. Measured budget miss is `FAILED` with evidence retained. No gate or live load run from the grill is treated as already passed.

### Key Entities

- **Seed fixture**: The deterministic 500-movie acceptance library identified by `/dev427/movie-NNNN.mp4` paths and `DEV427 GroupGG MovieNNNN` titles across ten groups of fifty.
- **Browse request**: One GET `/api/movies` call with explicit `page` and `pageSize=20` and no optional sort/direction.
- **Search request**: One GET `/api/movies` call with a seed-derived group text term, `page=1`, `pageSize=20`, and no optional sort/direction.
- **Measured run**: The tagged 50-VU / 120s steady population of browse+search requests used for p95 and custom error-rate acceptance.
- **Operator receipt**: The retained seed counts, commands/exits, run identity, threshold outcomes, and first valid measured p95 routed to Rigger.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Seed executed twice on the Finding 4 B disposable compose database yields exactly 500 total movies and 500 distinct deterministic seed paths after each run, with ten groups of 50 titles and no OMDb involvement.
- **SC-002**: Preflight against the seeded library confirms browse `totalCount=500` with 20 unique IDs per requested page and search `totalCount=50` with 20 matching titles for each Group00–Group09 probe.
- **SC-003**: A 50-VU, 120-second tagged steady run produces nonzero browse and search samples, p95 <=200ms, and custom request-level error rate <0.01, or else reports FAILED with retained evidence without widening scope.
- **SC-004**: Collision-safe summary JSON under `artifacts/load/` and the operator receipt capture the required metrics, env-supplied identities, seed proofs, redacted command/exit, and digest-pinned k6 image (`image@sha256:…`); the first valid measured p95 is recorded on DEV-427 through Rigger with verified read-back.
- **SC-005**: Unreachable target and invalid fixture runs exit nonzero; empty metrics do not pass; applicable gate exits keep SKIPPED/SKIP/Could not run/FAILED distinct from PASS.
- **SC-006**: Tracked implementation output contains only the three authorized `scripts/load/` files; tracked compose, CI, API, schema, manifests, generated contracts, LocalPlay, and harness configuration are unchanged by this work (ignored operator-local settings may be retargeted/restored per Finding 4 B).

## Assumptions

- Authority and selections are those already recorded in `brief.md`, `CONCLUSIONS.md` Q1–Q9, and `ASSUMPTIONS.md` Q2–Q7/Q9. This draft adds no new taste or structural assumption beyond applying Q9 corrections.
- Exact `grafana/k6` `image@sha256:…` digest value and the verified Docker host/network mapping are implementation selections recorded in README and run evidence; a version tag is only a label, never the immutable identity. They are not open planning decisions (brief Q5/Q9; FR-004/FR-009).
- Deterministic browse page-wrap order and concrete mixed-case Group00–Group09 probe spellings are bounded implementation selections recorded in README/evidence; FR-005/FR-006 expectations stay unchanged (brief Q9; ASSUMPTIONS.md Q9).
- Existing movie-library vocabulary is retained. Group labels and seed paths are fixture identifiers, not new domain concepts. No `CONTEXT.md` change and no ADR (brief Q8).
- DEV-325 compose is present and is the runtime dependency for seeding and nginx reachability. This delivery does not alter tracked compose/API/schema files; Finding 4 B uses ignored operator-local settings plus existing migration application at API startup.
- No owner checkbox is opened: no ticket contradiction or constitution departure was proposed (brief Status and authority; Q9).
