# DEV-325 — Brief (Phase 2 step 3.1, grill close)

Ticket: DEV-325 — Create docker-compose.yml with health checks, dependency ordering, and sample library
Size: M | ui: (none) | Parent: DEV-287
Worktree: F:\Dev\LamuFlix.worktrees\feature-325-spec | Branch: feature/325-spec
Grill base HEAD: d668cbd01dccba2e4f1eaacca26e5a5452d6cd73; grill rulings committed 33be0ab
Sources: YouTrack DEV-325 via scripts/get-task.ps1; specs/PRODUCT.md; lamuflix-team-charter; notes DEV-325, recon-DEV-325 (sections 0–11); specs/DEV-325/CONCLUSIONS.md (C1–C14); specs/DEV-325/ASSUMPTIONS.md (A1–A3); specs/DEV-21/brief.md and plan.md; task-pipeline.

## Closing bar

Current decision receipt (2026-10-13): the owner approved C1-A, no `worker` service and the enrichment consumer stays in api (DEV-325:32,36; CONCLUSIONS.md C1). C1 owner block is resolved. Gate 1 and plan freeze remain incomplete and require their separate pipeline steps. Alternative B is historical, not an active prerequisite.

Patron C15 (CONCLUSIONS.md, commit 7926171): fixed non-secret database name `lamuflix`. The postgres service sets `POSTGRES_DB=lamuflix`; api `ConnectionStrings__DefaultConnection` uses `Database=lamuflix` with host `postgres`. Credential interpolation stays per C8, required with no defaults. This adds no `.env` variable, schema change or API shape change. Basis: C8 and recon-DEV-325:43. Quill must carry this decision into plan.md and T009.

Keel C3 test decision (2026-10-13, recon-DEV-325 section 12.1–12.5): add `tests/LamuFlix.IntegrationTests/DatabaseStartupTests.cs` alongside the default/binding unit test `tests/LamuFlix.UnitTests/DatabaseOptionsTests.cs`. This selects the targeted startup test already permitted by the test strategy; production scope stays `src/LamuFlix.Api/Program.cs` plus the single `src/LamuFlix.Core/Options/DatabaseOptions.cs`. No production seam, package, project-reference, fixture or existing test-file edit is approved.

Use the existing Postgres collection and fixture to create a separate empty database per case, obtain its connection string from the returned context, and inject it into the existing `ApiHostFactory` additional settings. Start the real Api through `CreateClient()` and complete a request to `/health/live` before observing the database through a separate context. Use existing harness controls to remove the sweeper and disable the enrichment consumer, so background application writes do not contaminate the migration observation (recon:121–124,129–136). This harness stubs health checks; its HTTP success proves startup completed, not real dependency readiness.

Cover the option absent, explicitly false, and explicitly true. Query `information_schema.tables` for public BASE TABLEs before and after startup: absent/false must leave the database empty, including no `__EFMigrationsHistory`; true must create the existing migration history and application tables using the migration expectations evidenced in recon:136. The true case is a positive control for the same harness and observation method; live fresh-volume compose acceptance remains mandatory for real readiness. Do not use a pre-migrated end-to-end fixture, duplicate the boolean gate, mock the driver, or depend on unverified `GetAppliedMigrationsAsync` behaviour when history is absent (recon:124,134–136,154).

- C1 owner block is resolved by owner answer (A), recorded at DEV-325:32,36. Gate 1 and plan freeze remain incomplete.
- This brief and Quill's spec/plan/tasks proceed on owner-approved (A): no worker service; consumer stays in api.
- C2–C14 remain ruled, C15 fixes the database name, and taste calls remain A1–A3 in ASSUMPTIONS.md. The C3 targeted startup test decision is recorded above.
- This brief freezes scope, grill answers, and plan decisions for Quill. If it is not in this brief, it is not decided; a gap is `needs decision: <question>` to Keel, never a guess by Quill.
- Quill next drafts spec.md, plan.md, and tasks.md in one ask from this brief, recon-DEV-325, and specs/PRODUCT.md (Phase 2 step 3.2). Keel then runs /speckit-analyze read-only against this brief and sends one numbered fix list per round (step 3.3).
- Spec Kit is not started by this brief.

## C16/C17 plan-challenge decision receipt (2026-10-13)

Authority: Patron ruling summary in DEV-325:168; bounded adjudication DEV-325:160-167; recon-DEV-325-2:20-55; findings-DEV-325-Compass. Applied at supplied HEAD `7926171a700c0206296243f33196d4a32adc2385` on `feature/325-spec`. The referenced external temporary wording is **not found in these roots** and is not an input. CONCLUSIONS.md currently records C1-C15; C16/C17 are recorded here from the authorized ruling receipt, not attributed to unseen wording in CONCLUSIONS.md.

- **C16 — Postgres credentials outside the connection string.** The api maps `PGUSER: ${POSTGRES_USER:?message}` and `PGPASSWORD: ${POSTGRES_PASSWORD:?message}`; its `ConnectionStrings__DefaultConnection` is the non-secret literal `Host=postgres;Database=lamuflix`. No credential composition or encoding is used. The postgres service still maps the same supplied `POSTGRES_USER` and `POSTGRES_PASSWORD` through required interpolation with no defaults and sets `POSTGRES_DB=lamuflix`. `PGUSER` and `PGPASSWORD` are container environment keys, not new supplied `.env` variables. This supersedes the earlier C15 receipt and plan-decision wording only where they put credentials into the api connection string; C8 secret handling and C15 host/database remain unchanged. Basis: DEV-325:168 (Patron ruling), C8/C15, and recon-DEV-325-2:20-36 (unresolved prose composition and verbatim runtime consumption). P-F1 is accepted as that planning contract gap only; no injection, leak, delimiter failure or round-trip success has been demonstrated. Phase B must verify the decided credential delivery through the real Api/Postgres connection without printing credentials or resolved secret-bearing configuration; that is future evidence, not a current success claim. No production transform, credential restriction, dependency, extra source file or supplied variable is authorized.
- **C17 — Verify the api probe before T008.** After the C2 prerequisite (DEV-21 Phase B merged and its Dockerfiles available), require an image-tool inventory and an exact exec-form probe invocation using only tools in the actual DEV-21 api image, with the interpreter explicit if one is used. Retain `GET /health/ready`, and require positive and negative proof of the selected probe before T008 uses it. If no viable existing-tool probe is available, stop and route to Patron; do not add tooling, edit Dockerfiles or change the endpoint/scope. Basis: DEV-325:168 (Patron ruling), C2/C7, and recon-DEV-325-2:41-55 (tool availability and concrete invocation not established). This makes the earlier conditional bash fallback contingent on that evidence. P-F2 is accepted as missing feasibility/invocation planning only; bash absence, wrong-shell execution and probe failure are not established. Keep Wisp read-only prerequisite/evidence routing and Anvil compose-authoring routing. Inventory and positive/negative proof are future requirements, not work performed in this Phase A receipt.
- Patron reports no owner escalation (DEV-325:168). This receipt neither freezes the plan nor advances Gate 1. The existing scope, frozen file set, C1-A, review cap of two rounds, remediation cap of two fix commits per round and all other limits remain in force; it grants no additional round.

### One consolidated correction list for Quill

1. **C16 / accepted Sentry P-F1:** synchronize spec.md FR-007/FR-008 and credential/evidence descriptions, plan.md environment table/design/evidence, and tasks.md T009 and related acceptance wording with C16 above. Remove the superseded instruction to compose credentials into `ConnectionStrings__DefaultConnection` and its secret-bearing classification. Specify the exact separate api `PGUSER`/`PGPASSWORD` mappings and literal `Host=postgres;Database=lamuflix`; retain the same required supplied credentials, postgres mappings, fixed database, no defaults and no secret output. Encode future real connection verification without claiming it is already proven. No encoding, input restriction, new supplied variable, dependency, production transform or file is approved.
2. **C17 / accepted Sentry P-F2:** synchronize spec.md FR-005, plan.md api healthcheck/evidence and tasks.md with the C2-dependent image-tool inventory, exact exec-form invocation and positive/negative proof required before T008. Preserve GET /health/ready, in-image tools only, no Dockerfile edits, Wisp evidence/Anvil writing routing, and stop-to-Patron if no viable probe exists. Do not select an unverified binary or invent a concrete command now; those facts are not found in these roots.
3. **Accepted Compass F1:** replace plan.md:58 `pg_isready -U $POSTGRES_USER` with plain `pg_isready`, matching ticket-fixed command, spec.md FR-002 and tasks.md T007. The acceptance is the frozen-command mismatch only; do not present the alleged credential exposure as an established runtime defect or substitute the alternative escaped-variable command.
4. **Accepted Compass F2:** correct plan.md:94 and tasks.md T001 to require explicitly passing existing `disableEnrichmentConsumer: true` to `ApiHostFactory`. Sweeper removal is existing default behavior; consumer disabling is opt-in. Preserve the fresh empty database per absent/false/true case, existing additional settings, real startup completion, separate catalog observation, positive control and the distinction between stubbed startup health and live compose readiness. No fixture, existing test-file, production seam or reference change is approved.
5. **Retain rejected Sentry P-F3 and all limits:** retain manual replacement of every `.env.example` placeholder before a successful-start run and the missing-variable tests with absence from both `.env` and inherited environment. Required interpolation is not claimed to recognize placeholders. Do not add automatic placeholder recognition/rejection; P-F3 is rejected as a mandatory defect, not tested away. Retain all other frozen scope and round limits, no worker, no extra dependency/schema/API/file changes, no freeze or Gate 1 advancement. This is one consolidated correction list within the existing cap, not authority to start another round.

## Round caps

- Grill cap: 12 questions (task-pipeline section 2.2); beyond that, Keel recommendation tagged [assumed] in ASSUMPTIONS.md. Grill closed within cap; the 3 logged assumptions are in ASSUMPTIONS.md, plus the C1 owner checkbox above.
- Review cap: 2 rounds maximum; third-round findings go to user review with the PR.
- Remediation cap: 2 fix commits per round; a third means the finding or the plan was wrong.
- Blocked limit: 3 blocked reports parks the task.

## Frozen scope

In scope (implementation, Phase B, on owner-approved A):

- `docker-compose.yml` at repository root — new file: services postgres, rabbitmq, api, web (no worker, per C1-A), healthchecks, `service_healthy` dependency ordering, `Features__LocalPlay=false`, sample-library mount, web publish `3000:8080`, api not published (C14).
- `.env.example` at repository root — new file, placeholder values only (C8).
- `docker/sample-library/` — new folder with 3–5 small placeholder files with video extensions, `Title (Year).ext` names, total under 100 KB (C4, A2).
- Api migrate-on-startup wiring for C3: `Database:ApplyMigrationsOnStartup` validated option, default `false`, applying existing EF Core migrations unchanged at Api startup only when `true`; compose sets it `true`. Exact files are named in plan.md and are limited to the Api startup path plus at most one options type; any wider change is `needs decision` to Keel.
- `README.md` — one added section `Run with Docker Compose` (C11, A3). No other README change.

In scope (this Phase A): specs/DEV-325/brief.md (this file), CONCLUSIONS.md, ASSUMPTIONS.md, then Quill-drafted spec.md, plan.md, tasks.md under specs/DEV-325/.

Out of scope (adding any of these is a ticket change unless noted):

- A `worker` compose service, a fake worker running the Api image a second time, or a new worker host project (C1; (B) is the owner's alternative, delivered by its own ticket if chosen).
- `docker-compose.observability.yml` and any OTLP collector service (C10, C12).
- Editing any DEV-21 file: `docker/Dockerfile.api`, `docker/Dockerfile.worker`, `docker/Dockerfile.web`, the DEV-21 nginx conf, root `.dockerignore`; adding HEALTHCHECK to any Dockerfile (C2).
- Editing `web/vite.config.ts`, the nginx conf, or any web source to achieve port 3000 or the proxy (C5).
- Any NuGet/npm dependency change, database schema or migration change (C3 applies existing migrations only), public API route/DTO/status-code change, or contract-chain change.
- Committing a real `.env`, any secret, token, password, or connection string with a real value, or any compose default for a secret (C8, scrub list).
- Setting `Features:LocalPlay` to anything but `false`, or anything enabling `Process.Start` playback.
- Committing real media files for the sample library (C4). If the scanner needs real media content, Keel reports `needs recon: <question>` and stops that line.
- Deleting or rewriting any other tracked file the ticket does not name, except the C3 and C11 forced edits cited there.

Recorded side effect, not in scope: DEV-21 `Dockerfile.worker` publishes the retired `src/LamuFlix.Worker` and will fail DEV-21 SC-001 (C1). The recon fact goes to DEV-21 as a YouTrack comment through Rigger (Patron decides, Rigger records); DEV-325 does not fix DEV-21.

## Grill answers (every ruling, cited)

Decided by ticket text, not re-grilled (CONCLUSIONS.md preamble):

- Services postgres (PostgreSQL 17, `pg_isready`) and rabbitmq (RabbitMQ 4, `rabbitmq-diagnostics -q ping`); api depends on both with `condition: service_healthy`; web depends on api; `Features:LocalPlay=false` in container environment; sample library mount; AC1 `docker compose up -d` starts all services healthy; AC2 web UI at http://localhost:3000 with functioning API proxy. Basis: ticket Scope items 1–2 and acceptance criteria (recon section 1); PRODUCT.md section 3.

Patron rulings (CONCLUSIONS.md):

- C1. Worker: owner approved (A), no worker service; consumer stays in api (DEV-325:32,36; CONCLUSIONS.md C1). C1 owner block resolved; Gate 1 and plan freeze remain incomplete. Fake worker rejected.
- C2. Images: api and web use compose `build:` from DEV-21 `docker/Dockerfile.api` / `docker/Dockerfile.web`, context repository root, no registry. Phase B is sequenced after DEV-21 Phase B merges (Dockerfiles must exist on disk). DEV-325 edits no DEV-21 file.
- C3. Schema bootstrap: Api applies existing EF Core migrations at startup only when `Database:ApplyMigrationsOnStartup=true` (validated option, default `false`); compose sets `true`. Forced by AC2/Scope item 2 under charter section 2.3 item 6; not a schema change.
- C4. Sample library: committed placeholders under `docker/sample-library/` (no new top-level folder), mounted `${LIBRARY_PATH:-./docker/sample-library}:/media/library:ro`, `Library__RootPath=/media/library`.
- C5. Port 3000: compose publish `3000:8080` on web only; nginx stays on 8080; `web/vite.config.ts` untouched.
- C6. Proxy proof: `curl -fsS http://localhost:3000/` returns 200 with the SPA index and `curl -fsS http://localhost:3000/api/genres` returns 200 with a JSON array; raw output recorded. The SPA makes no API calls, so the HTTP proof stands in for UI interaction.
- C7. Healthchecks: api probes `GET /health/ready` using only tooling already in the DEV-21 aspnet image (bash `/dev/tcp` probe expecting `200` if no curl/wget; no Dockerfile edit); web uses busybox `wget -q --spider http://127.0.0.1:8080/`; worker none under (A).
- C8. Secrets: committed `.env.example` (placeholders only: `OMDB_API_KEY`, postgres user/password, rabbitmq user/password) plus user-supplied `.env`; compose reads every secret as `${VAR:?message}` with no default. Compose also maps the required non-secret options so ValidateOnStart passes: `RabbitMq__HostName=rabbitmq`, `Enrichment__SweepInterval`, `Enrichment__ClaimLease`, `RabbitMq__RetryDelay` greater than ClaimLease (`Omdb__BaseUrl` comes from appsettings).
- C9. Web `depends_on` api: `condition: service_healthy`.
- C10. `docker-compose.observability.yml`: out (noted, no ticket).
- C11. README: one `Run with Docker Compose` section is in scope (constitution-forced): copy `.env.example` to `.env`, `docker compose up -d`, open localhost:3000, `LIBRARY_PATH` override.
- C12. OTLP: no endpoint env set, no collector; export failures non-fatal. Phase B evidence must show AC1 healthy with no collector; if exporter failures block health, report `needs decision`.
- C13. Image tags: `postgres:17-alpine`, `rabbitmq:4-alpine`, major-pinned, never `latest`.
- C14. Api port: not published to the host; only web 3000 is published, proxy reaches `api:8080` on the compose network.

Assumptions (ASSUMPTIONS.md, taste, [assumed]):

- A1. Image variants `postgres:17-alpine`, `rabbitmq:4-alpine` (no management UI) (= C13).
- A2. Sample placeholders: `Title (Year).ext`, 3–5 files, total under 100 KB (= C4).
- A3. README section title `Run with Docker Compose` (= C11).

## Plan decisions

Approach:

- One root `docker-compose.yml` with four services on owner-approved (A): postgres, rabbitmq, api, web. Postgres healthcheck `pg_isready`, rabbitmq healthcheck `rabbitmq-diagnostics -q ping` (ticket-fixed commands). Api `depends_on` postgres and rabbitmq `service_healthy`; web `depends_on` api `service_healthy` (C9).
- api and web build from the DEV-21 Dockerfiles with repository-root context (C2). No `image:` registry reference, no Dockerfile edit, no HEALTHCHECK added anywhere by this ticket.
- Healthchecks per C7: api `/health/ready` probe with in-image tooling only, web busybox wget spider. Postgres/rabbitmq keep the ticket commands. AC1 healthy means every service in the file reports healthy.
- Environment: `Features__LocalPlay=false` explicitly on api (ticket Scope item 2; also the code default). All secrets via `${VAR:?message}` from `.env`; `.env.example` carries placeholders only (C8). Non-secret required options mapped per C8 so the Api passes ValidateOnStart.
- `Database__ApplyMigrationsOnStartup=true` is set only in the compose api environment (C3). The code default stays `false`, so the host-run daily-driver path (constitution :506-507) is unchanged.
- Sample library mounted read-only at `/media/library` with `Library__RootPath=/media/library` and the `LIBRARY_PATH` host override (C4). Placeholders are committed dummy files, not real media.
- Web publishes `3000:8080` only; api publishes no host port (C5, C14). No OTLP endpoint is set and no collector service is added (C12).
- README gains exactly one section per C11/A3.

Files touched:

- Spec phase (Phase A): specs/DEV-325/spec.md, specs/DEV-325/plan.md, specs/DEV-325/tasks.md (Quill, from this brief); this brief, CONCLUSIONS.md, and ASSUMPTIONS.md are the Keel/Patron-written artifacts. No ADR (M ticket; ADR is L-only, task-pipeline Phase 2 step 5). No design.md (no ui: tag).
- Build phase (Phase B, for plan/tasks to enumerate, after DEV-21 Phase B merges per C2): root `docker-compose.yml`, root `.env.example`, `docker/sample-library/` placeholder files, `README.md` (one section), the C3 Api startup/options wiring (exact files named above, limited as in Frozen scope), and the two C3 test files named in the decision receipt above. Nothing else. Any further file is `needs decision` to Keel before it is added.

Test strategy:

- No existing test references docker-compose (recon section 9). Acceptance is by live compose evidence in Phase B, recorded with raw command output: `docker compose up -d` reaches all-healthy (AC1), then the two C6 curl proofs (SPA index 200, `/api/genres` 200 JSON array) (AC2). Evidence must also show AC1 healthy with no OTLP collector present (C12).
- C3 is evidenced by the api reaching healthy against a fresh postgres volume with `Database:ApplyMigrationsOnStartup=true`, and by the default-`false` path remaining off outside compose (existing test suite plus a targeted test for the new option/startup gate if plan.md adds one; no schema is created or altered beyond applying existing migrations).
- Structural checks in the same evidence pass: no `worker` service (owner-approved A), no secret default in compose, no committed `.env`, placeholder files only under `docker/sample-library/`, image tags exactly per C13 with no `latest`, api has no host port mapping.
- If placeholder files fail any scanner/media check in Phase B, that line stops with `needs recon: <question>` (C4); real media is never committed as a workaround.
- Owner answer C1-A governs the compose acceptance above (DEV-325:32,36).

Gate expectations (Phase B, run by Gauge per task-pipeline Phase 3 step 5):

- DEV-325 Phase B starts only after DEV-21 Phase B merges (C2). The compose build evidence depends on `docker/Dockerfile.api` and `docker/Dockerfile.web` existing on disk.
- This ticket changes production C# only in the C3 startup/options wiring, so the C# gates have real scope on those files and must exit 0; the mutation gate applies to the changed production projects per the pipeline and is reported by its own exit verdict (PASSED / FAILED / NOT APPLICABLE / SKIPPED (scope-empty)), never folded into PASS.
- Gates with no scope in the diff (for example web gates if no web source changes) report exit 2 `SKIPPED (scope-empty)` verbatim: non-blocking, never PASS, never proof the code was checked. Exit 1 or `Could not run` blocks; a property-test exit 2 blocks unless the task note records the pipeline opt-out line.
- Never lower a threshold, never edit a generated file, and never substitute plain `dotnet build` for a gate script. The live compose evidence (AC1/AC2/C6) is this ticket's primary proof and is required in addition to the gate set, not instead of a gate that has real scope.

Task-ordering constraints (for Quill to encode in tasks.md):

1. Phase B does not start until DEV-21 Phase B has merged and `docker/Dockerfile.api` / `docker/Dockerfile.web` exist on disk (C2). The C1 owner checkbox is answered A; Gate 1 remains incomplete pending its separate ruling.
2. The C3 option/startup wiring lands before any compose acceptance run, because the api cannot serve a fresh database without it; its default remains `false` outside compose.
3. `.env.example` is written before the compose file is exercised; acceptance uses a local `.env` copied from it, which is never committed.
4. Sample-library placeholders are committed before the acceptance run; if they fail a scanner check, stop with `needs recon` instead of substituting real media (C4).
5. The README section is written last, describing only the commands and override the frozen compose file actually supports (C11).
6. No task may add a worker service, an observability compose file, a collector, a dependency, a schema change, a Dockerfile edit, or a tracked-file rewrite beyond the frozen set. Such a task is a brief violation for Keel to strike at /speckit-analyze.

## Next owner and action

- Next owner: Quill — Phase 2 step 3.2, /speckit-specify, /speckit-plan, /speckit-tasks in one ask from this brief, recon-DEV-325, and specs/PRODUCT.md, with $env:PYTHONUTF8 set to 1 first.
- Then Keel: /speckit-analyze read-only against this brief (step 3.3), checking in particular the C1 owner-approved-A provenance, the C3 file limit, the C8 secret handling, and the frozen file set.
- Gate 1, plan challenge, and the spec PR follow the pipeline after a clean analyze; the C1 owner checkbox is answered A; Gate 1 remains incomplete pending its separate ruling. Nobody merges; the user merges every PR.

## Keel plan-freeze receipt (2026-10-13)

**Decision: PLAN FROZEN for the current on-disk DEV-325 drafts.** Worktree `F:/Dev/LamuFlix.worktrees/feature-325-spec`, branch `feature/325-spec`, verified HEAD `7926171a700c0206296243f33196d4a32adc2385`. This receipt supersedes earlier pending-freeze and next-Quill statements in this brief; it retains the frozen scope, decisions, file set, caps and task ordering.

**Report availability closed: not available in connected roots.** Wisp final note-availability receipt at DEV-325:168 records that `DEV-325 C17 retry report` is neither visible nor readable and its exact read returned `No connection to note`. The standalone source report was not read, recovered or sought elsewhere. The accessible DEV-325:168 correction summary and prior post-correction analyze receipt are the inputs used here.

**Sufficiency ruling.** The accessible task-note summary plus the current-draft analysis are sufficient for the normal Keel plan-freeze step under task-pipeline Phase 2 steps 3.3 and 6 and its writing/deciding separation. All three challenge axes have recorded receipts (DEV-325:134-141); bounded dispositions are recorded at DEV-325:149-167. Ledger is clean; Compass F1/F2 and Sentry P-F1/P-F2 are accepted within the recorded limits and their corrections are incorporated; P-F3 is rejected as a mandatory defect. The missing standalone retry report adds no unresolved planning decision. Neither the pipeline nor charter requires independently proven edit provenance for this semantic freeze; its absence remains a receipt limitation, not a substituted success claim.

**Current-draft alignment.** Read spec.md, plan.md and tasks.md against this brief and DEV-325:168. Plain `pg_isready`, explicit `disableEnrichmentConsumer: true`, C16 separate `PGUSER`/`PGPASSWORD` mappings and literal `Host=postgres;Database=lamuflix`, manual placeholder replacement and C17 Wisp-to-Anvil handoff are retained. I1 is resolved: Wisp supplies the C2-dependent image-tool inventory, exact exec-form `GET /health/ready` invocation with interpreter if used, and positive/negative proof before Anvil selects/adds the probe in T008. The Phase 0 checkpoint has no new task ID or reordered tasks; Wisp evidence, Anvil writing and Gauge gate ownership remain intact. No tool or command is presumed available; no viable probe means stop to Patron within the existing-tool and no-Dockerfile-edit boundary.

The prior post-correction analyze receipt records CLEAN, zero findings, prerequisites exit 0, 17/17 requirements covered across 18 ordered tasks T000-T017, and no unmapped tasks, ambiguity, duplication or demonstrated constitution conflict. Current draft reading supports that planning alignment; no new analyze command, prerequisite run, test or gate was performed in this ask. C2, real credential delivery, startup behavior and probe execution remain Phase B prerequisites/evidence obligations, not current execution successes.

**Provenance limitation retained.** Quill reports only plan.md and tasks.md edited; both are untracked and the reported scoped git diff was empty. No before/after source report or independent diff verification is available here; this freeze does not attribute edits, prove unchanged contents during the blocked ask or establish edit provenance. Verified dirty paths before this brief-only edit: modified brief.md; untracked pr-body-DEV-325.md, plan.md, spec.md and tasks.md. All prior state is preserved.

**Boundary and next seat.** This ask edits only this brief to record the decision. Gate 1 remains incomplete; no `gate1: provisional`, implementation authority, gate/test success, commit, PR readiness, tracker action or merge approval follows. Bernstein next routes the separate Patron Gate 1 ruling under task-pipeline Phase 2 step 7; no additional correction round is granted.

## Patron Gate 1 ruling (2026-10-13, Phase 2 step 7)

**Verdict: `gate1: provisional`.** HEAD `7926171a700c0206296243f33196d4a32adc2385`, branch `feature/325-spec`.

- Both preconditions in the Patron charter (`gate1: provisional` once `/speckit-analyze` is clean and the plan challenge is adjudicated) are recorded: Keel's post-correction analyze reported CLEAN, I1 resolved, 17/17 coverage, T000-T017 order kept (Keel plan-freeze receipt above). The plan challenge was adjudicated as C16/C17 plus Compass F1/F2 accepted and P-F3 rejected (DEV-325:160-168).
- Provenance is not a blocker. spec.md, plan.md and tasks.md are untracked, so no before-and-after diff exists to verify. The charter does not require edit provenance for Gate 1. The record of these files' content will be the spec-PR commit that first tracks them, and the reviewer checks `git diff main...feature/325-spec`. The missing Quill retry report adds no open decision (Keel sufficiency ruling above).
- There is no owner escalation. C1-A is answered (DEV-325:32,36), and no change to the ticket text or departure from the constitution is open. `pr-body-DEV-325.md` is a scratch file and can be deleted.

Limits: this ask edits only this brief. It does not edit the spec.md `**Status**:` line, commit anything, change PR #135, touch YouTrack, run gates or tests, or authorize implementation. Bernstein routes the spec.md status line and the commit. Gate 1 passes only when the user merges the spec PR. Phase B still waits for DEV-21 Phase B to merge (C2). C16 credential delivery and C17 probe proof are still owed as Phase B evidence.

## Owner ruling: pre-commit secret-scan correction (2026-10-13)

The pre-commit `connection string password` rule (`scripts/hooks/secret-scan.ps1`) blocked the spec-pack commit on every password mapping written with `=` in brief.md, plan.md, spec.md and tasks.md. Owner decision, applied directly by Fixer at the owner's instruction:

- Every Compose environment mapping in these artifacts is written in map form, `NAME: ${VAR:?message}`. Required interpolation (C8) is unchanged; the map form does not match the rule.
- Phase B: the compose file uses the same map form, and every `.env.example` placeholder value is shorter than 8 characters (T005). No scanner exclusion.
- This replaces the pending recon/Patron step on this question. No other scope, decision or limit changes.
