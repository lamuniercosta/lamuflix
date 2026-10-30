# Implementation Plan: DEV-427 Manual movie-library load baseline

**Branch**: `feature/427-spec` | **Date**: 2026-10-30 | **Spec**: `specs/DEV-427/spec.md`

**Input**: Feature specification from `specs/DEV-427/spec.md`, settled grill brief `specs/DEV-427/brief.md`, facts from note `recon-DEV-427`, Patron rulings in `specs/DEV-427/CONCLUSIONS.md` and `[assumed]` selections in `specs/DEV-427/ASSUMPTIONS.md`.

**Status**: Plan challenge verified and plan frozen on 2026-10-30 per brief Plan freeze receipt. Gate 1 remains pending user merge of the spec PR; no implementation authorization.

## Summary

Add a manual k6 load baseline for GET `/api/movies` against the existing DEV-325 compose stack: an idempotent 500-movie SQL seed without OMDb on a separately named disposable database provisioned via recon Finding 4 B, a deterministic 50-VU / 120s browse+search workload with preflight and tagged measurement, collision-safe JSON under `artifacts/load/`, and a runbook for Docker-only execution. First valid measured p95 is recorded on ticket DEV-427 through Rigger. Tracked delivery is exactly three `scripts/load/` files. No tracked API, schema, CI, compose, or production dependency changes.

## Technical Context

**Language/Version**: k6 JavaScript for `scripts/load/movies.js`; SQL for `scripts/load/seed.sql`; Markdown runbook. No production C# or React change.

**Primary Dependencies**: Existing DEV-325 Docker Compose stack (API, Postgres/pgvector, nginx-published web on host port 3000). External tools at run time only: in-container `psql` via `docker compose exec -T postgres psql`, and a digest-pinned `grafana/k6` image (`image@sha256:…`; tag may be a label only). No NuGet or npm dependency. No host k6/psql install required. Actual digest value is selected during implementation from official documentation and recorded in README and run evidence. Implementation guidance may consult `.agents/skills/k6-load-testing/SKILL.md`; ticket/frozen metrics and scope prevail — no example-driven dependencies, budgets, helper files, or CI.

**Storage**: Existing PostgreSQL `movies` table only. Seed inserts into that table. No NEW/authored migration or schema definition. Existing committed migrations are applied during runtime provisioning of the disposable database (Finding 4 B). Runtime JSON under ignored `artifacts/load/`.

**Testing**: No new C# or React tests, test infrastructure, or CI wiring. Verification remains real compose seed-twice, k6 preflight/response checks, the actual measured 50-VU / 120s baseline, and bounded negative checks. Do not claim acceptance from syntax checks or synthetic output.

**Target Platform**: Local operator machine with Docker, running against compose nginx at `http://localhost:3000`. Manual only; not CI.

**Project Type**: Tooling scripts under existing `scripts/` tree (`scripts/load/`), precedent `scripts/local/`. No new project, top-level folder, or application layer.

**Performance Goals**: Combined measured browse+search p95 <=200ms inclusive; custom request-level error rate <0.01 strict; nonzero browse and search samples; 50 VUs for 120s steady.

**Constraints**: Exactly three tracked implementation files (`scripts/load/seed.sql`, `scripts/load/movies.js`, `scripts/load/README.md`) — additive README already authorized by Q5; Q9 confirms that bound without rewriting Q5 history. Ignored operator-local runtime configuration (`docker/api.settings.json`) may be used for disposable-DB selection; tracked compose/API/schema/CI/manifest/harness files stay unchanged. Preserve unrelated `harness.yml` modification. No OMDb. No secrets in source; credentials only from existing env/private config (never CLI literals, history, or logs); receipts redact sensitive values. No explanatory comments in `movies.js` or `seed.sql`; rationale lives in README prose. No ambient clock or randomness for fixture/request selection; run ID/UTC/HEAD metadata are explicit env-supplied inputs. No `Features:LocalPlay` or `Process.Start`. No DEV-334 runner reuse, no existing-library deletion, no new helpers. Budget miss stays FAILED without widening scope. Explicit `page`/`pageSize` on every request. Omit optional `sort`/`direction`.

**Scale/Scope**: 500 deterministic movies; 10 title groups of 50; pages 1–25 at pageSize 20; Group00–Group09 search probes; one manual baseline recording path via Rigger.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-427: k6 script `scripts/load/movies.js`, seed `scripts/load/seed.sql`, 500-movie library, 50 VUs / 2 minutes, p95 <=200ms, error <1%, JSON under `artifacts/load/`, first p95 on ticket, manual not CI. Nothing is added or dropped. No owner checkbox (brief Q8/Q9).
- k6 is an external CLI/container tool named by the ticket, not a NuGet/npm dependency (PRODUCT.md care list item 1; recon-DEV-427 §2.3; brief Q5). No package manifest change.
- No new project, top-level folder, or architectural layer (constitution I; PRODUCT.md care list item 2). Files stay under `scripts/load/`.
- No handler, route, status, or public DTO change (constitution II; V; API and Contract Rules). GET `/api/movies` is consumed as-is.
- No NEW/authored schema or migration definition (constitution Technology Stack Constraints; PRODUCT.md care list item 3). Seed inserts into existing `movies` columns only. Applying already-committed migrations to a disposable database via existing API startup is runtime provisioning, not a schema change authored by this delivery.
- `Features:LocalPlay` untouched. No secret committed. No `Process.Start` (constitution VII; PRODUCT.md care list item 5; brief Files touched).
- Existing tracked-file edits: none beyond the three new `scripts/load/` files (PRODUCT.md care list item 6; Q5; Q9 clarification of L2). Compose, CI, API, schema, manifests, generated contracts, and harness configuration stay as they are. Unrelated `harness.yml` modification is preserved, not authored by this plan. Ignored operator-local `docker/api.settings.json` may be temporarily retargeted per Finding 4 B and must be restored.
- No new domain term and no `CONTEXT.md` edit; no ADR (constitution VIII; brief Q8).
- **Principle IX (Test Pyramid with Real Infrastructure)**: The IX production-layer table (Unit/Integration/Architecture/Mutation projects and their mandated tools) is outside the scope of these manual tooling files. That is a scope boundary for this ticket's deliverables, NOT an exemption based solely on "manual / not CI." Required verification remains real-infrastructure: compose seed-twice, preflight against the live API, the actual 50-VU / 120s measured baseline, and bounded negatives. No new C#/React test project is added because none is in scope; the tooling still must prove itself on the real stack.
- **Principle VI (Observability Across Processes)**: No instrumentation, telemetry constant, health-check, or `ServiceDefaults` change. This delivery emits k6 summary JSON and operator receipts only; it does not alter production observability.
- **Coding Conventions**: No production C# is authored. For the JS/SQL tooling files, AGENTS.md / constitution comment rules still apply as a delivery constraint: no explanatory comments in `movies.js` or `seed.sql`; rationale belongs in README prose. Deterministic fixture selection uses no ambient clock or randomness.
- **Documentation Rules**: No ADR and no `CONTEXT.md` change. The additive `scripts/load/README.md` is the runbook authorized by Q5; product README modernization content is untouched.
- Gates stay the configured scripts (Static-Analysis Gates; brief Gate expectations). Scope-empty skip, harness skip, Could not run, and FAILED budget miss are reported as themselves and are not passes.
- No constitution departure and no owner checkbox (brief Q9).

Post-design re-check: the design below adds no production type, package, public field, authored schema/migration, glossary term, ADR, or observability change. Principle IX/VI/coding/documentation applicability is recorded above. The same gates hold. No constitution violation is recorded, so Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-427/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
└── ASSUMPTIONS.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief freezes approach, files, and order. There is no new public contract to put under `contracts/`. No ADR.

### Source Code (repository root)

```text
scripts/load/seed.sql      # new: deterministic idempotent movies fixture
scripts/load/movies.js     # new: k6 workload, preflight, thresholds, summary JSON
scripts/load/README.md     # new: isolated setup, credential-safe commands, network mapping, evidence
artifacts/load/            # runtime only; already covered by root artifacts/ ignore
```

Unchanged tracked inputs, used as they stand: `docker-compose.yml` (nginx `3000:8080`, postgres without host port), existing GET `/api/movies` browse/search contract, existing `movies` table and `library_path` unique index, committed EF migrations applied by API startup when `ApplyMigrationsOnStartup=true`, ignored `artifacts/` output root, ignored operator-local `docker/api.settings.json` (Finding 4 B retarget/restore only; never a tracked delivery file).

**Structure Decision**: Load tooling lives under `scripts/load/` beside existing `scripts/local/` helpers so no top-level folder or project is added. Runtime evidence stays untracked under `artifacts/load/`. Tracked implementation output remains exactly the three authorized files (Q5; Q9 L2 clarification).

## Design Overview

1. **Seed SQL.** `scripts/load/seed.sql` inserts `i=1..500` into `movies` with the frozen path/title/format/watchlist/status/attempts values and `ON CONFLICT (library_path) DO NOTHING`. Required non-default columns are supplied; other required columns use existing DB defaults. No join-table rows are required for title-only search (recon fact). No NEW/authored migration or schema definition. No explanatory SQL comments; rationale stays in the README.
2. **Disposable DB provisioning and seed execution (recon Finding 4 B).** Keep the operator's existing compose stack and `lamuflix` library; do not delete existing-library rows and do not reuse the DEV-334 e2e runner/helpers. Create a separately named disposable database inside the existing compose postgres container via credential-safe `docker compose exec -T postgres psql` (credentials only from existing env/private compose config — never CLI literals, shell history, or logs). Point the API at that database only by editing ignored operator-local `docker/api.settings.json` (`Database=` on `ConnectionStrings:DefaultConnection`), keeping `Database.ApplyMigrationsOnStartup=true`. Restart `api` so existing committed migrations apply to the empty disposable database. Before any insert: prove compose project/container/database identity, prove API and SQL target the same database, and assert the disposable `movies` table is empty or already exact-seed-only. Seed transactionally where practical. Execute seed twice; after each run verify exactly 500 total rows and 500 distinct `/dev427/movie-%.mp4` paths. Retain both receipts with redacted credentials. Document that while redirected, compose UI/API serve the disposable fixture. Preserve the original `docker/api.settings.json` privately and restore it plus restart `api` on success or failure. No tracked compose/API/schema edits; ignored runtime configuration is allowed while tracked delivery remains exactly the three `scripts/load/` files.
3. **k6 workload.** `scripts/load/movies.js` drives GET `/api/movies` with explicit paging. VU loop: four browse (pages 1–25, `pageSize=20`) then one search (Group00–Group09 including mixed-case probes, `page=1`, `pageSize=20`), 1s sleep after each request, deterministic counters only — no ambient clock or `Math.random` for fixture/request selection. Omit `sort`/`direction`. No explanatory JS comments; rationale stays in the README. Deterministic page-wrap order and concrete mixed-case probe spellings are bounded implementation selections recorded in README/evidence; FR-005/FR-006 text stays unchanged. Prefer patterns from `.agents/skills/k6-load-testing/SKILL.md` as guidance only; ticket metrics and manual-only scope prevail.
4. **Preflight and checks.** Preflight validates fixture expectations before measurement. Checks cover HTTP 200, JSON `items`/`totalCount`, browse/search counts, unique IDs within a browse page, and title membership for searches. Failures count once per request toward the custom error rate.
5. **Measurement.** Fixed 50 VUs, exactly 120s steady, no ramp in that window. Tag measured traffic so setup/preflight/optional warm-up are excluded. Thresholds: `p(95)<=200` and custom error rate `<0.01` over combined browse+search. Require nonzero browse and search samples. Emit non-blocking per-type diagnostics.
6. **Docker k6.** Select a digest-pinned `grafana/k6` image during implementation (`image@sha256:…` required in README and evidence; any tag is a label only; actual digest value remains an implementation choice). Run with `BASE_URL` override and bind-mounted `artifacts/load/`. Document and verify how the container reaches host/compose nginx at the same entry as `http://localhost:3000`. Do not assume container localhost reaches the host.
7. **Runbook.** `scripts/load/README.md` covers Finding 4 B isolated DB prep (create, ignored settings retarget, ApplyMigrationsOnStartup, api restart, same-target proof, temporary UI/API notice, private preservation and mandatory restore/restart), credential-safe seed commands, pre-insert identity/empty-or-exact-seed guard, transactional seed where practical, repeat-seed assertions, verified network mapping, digest-pinned image usage, env-supplied run ID/UTC/HEAD inputs, baseline run, evidence fields, and bounded negatives — without new helper scripts.
8. **Evidence and ticket write-back.** Collision-safe summary JSON plus operator receipt (seed counts, redacted command/exit, VUs/duration/target, p95, error rate, sample counts, threshold outcomes, explicit env-supplied run ID/UTC/HEAD, digest-pinned image, hardware/environment). Route first valid measured p95 to Rigger for YouTrack recording and verified read-back even when budget fails. Reproducible without new helper files.
9. **Negatives.** Unreachable target → nonzero exit / Could not run. Invalid fixture → nonzero exit. Empty metrics → not a pass.
10. **Gates.** Apply harness gates only at applicable stage/scope. Empty C# scope → `SKIPPED (scope-empty)`. Disabled gate → `SKIP`. Budget miss → `FAILED`. Never substitute `dotnet build` or unrelated suites for load acceptance.

## Loop discipline

- **Closing bar:** Critical, High, and Medium block; every Low has an explicit disposition. For size-M review adjudication, all Sentry, Ledger, and Compass axis reports are required.
- **Round cap:** two review rounds. Exhaustion leaves a blocker for Conductor; it does not permit extra rounds or relaxed thresholds.
- **Frozen scope:** manual k6 GET `/api/movies` browse paging and title search; idempotent 500-movie SQL seed without OMDb on a Finding 4 B disposable database; 50 VUs for two steady minutes; p95 <=200ms and request error rate <1%; JSON under `artifacts/load/`; first valid measured p95 on DEV-427 through Rigger; the three tracked `scripts/load/` files and these planning documents. Exclude DEV-334 Playwright/runner reuse, existing-library deletion, authored migrations/schema definitions, pre-baseline budget tightening, tracked compose/CI/API/schema/manifest/harness edits, production dependency adds, LocalPlay, new helpers, and unrelated cleanup. Anything else is a follow-up issue, not a finding in this round.
- A performance-budget miss remains FAILED. It does not authorize endpoint, index, or architecture changes.

## Verification expectations

Verification is the seed-twice proof, k6 preflight/response checks, the actual 50-VU / 120s measured baseline, bounded negative checks, Rigger ticket p95 read-back, and applicable harness gate receipts. Do not claim acceptance from syntax checks or synthetic output. Report SKIPPED/SKIP/Could not run/FAILED by name. The baseline must actually meet the ticket budget before acceptance can be claimed; seed-twice and ticket p95 receipts are separately required.

## Complexity Tracking

No constitution violation is recorded. This section is intentionally empty.
