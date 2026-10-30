# DEV-332 — Local Aspire AppHost brief

Status: grill closed by Patron at Q10/12; decisions settled for Quill drafting. This is not plan freeze, Gate 1 approval, implementation, or gate success.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-332-spec`  
Branch: `feature/332-spec`  
Recon baseline: `f698aeceb6b6af50a397d076b83ed178566641b0` (recorded recon identity, not a fresh HEAD measurement).  
Size: L, per note DEV-332:5.  
Decision authority: append-only [CONCLUSIONS.md](CONCLUSIONS.md), Q1-Q10. Full question and answer exchanges, cited bases, rationale and implications are preserved there.

## Evidence and authority

- Note DEV-332:27-31: local AppHost, Postgres, RabbitMQ, Api, Vite on 5173, dashboard, F5, pinned packages and solution inclusion; no worker, production/deployment or compose changes.
- Notes recon-DEV-332, recon-DEV-332-2 and recon-DEV-332-3 supply all repository and API facts. Part 3 explicitly corrects part 2's failed search for Aspire.Hosting.JavaScript; the package exists, but Q3 deliberately does not select it.
- [PRODUCT.md](../PRODUCT.md), especially sections 2, 3 and 5, supplies sequencing, ticket authority and care-list rules.
- Constitution IX, `.specify/memory/constitution.md:362,370,387-388`, as quoted in recon-DEV-332-3:29-31, scopes mutation to Core and supports the AppHost exclusion.
- Draft ADR: [0031-local-aspire-apphost.md](../../docs/adr/0031-local-aspire-apphost.md). No new domain vocabulary or glossary edit is required (Q8).

## Every grill answer

| Question | Patron ruling and cited basis |
|---|---|
| Q1 — Topology | AppHost owns separate local Postgres and RabbitMQ containers and orchestrates `postgres`, `rabbitmq`, `api`, `web`, plus SDK dashboard and F5. No worker, Ollama resource, compose or deployment changes. DEV-332:27-31; original recon:54; PRODUCT sections 2-3. Q4 clarifies that existing vector schema support remains required. |
| Q2 — Project and pins | New `src/LamuFlix.AppHost/LamuFlix.AppHost.csproj` under solution `src`. SDK, AppHost, PostgreSQL and RabbitMQ packages all pin **13.6.4**; `AspireUseCliBundle=false`; no separate dashboard package. DEV-332:28; PRODUCT sections 3/5; recon part 2:14,24-28. This resolves the earlier 13.6.0 summary/example discrepancy. |
| Q3 — Web hosting | Core `AddExecutable` runs existing npm dev script with explicit 5173 and strict-port failure, resolves the web working directory, injects `VITE_API_BASE_URL` from the Api HTTP endpoint, and disables endpoint proxying on fixed localhost 5173. No JavaScript hosting package or web file edits. Windows npm resolution and port behavior require runtime proof. Recon part 3:13-21; part 2:40-47; DEV-332:28-29. |
| Q4 — Database | Ephemeral Postgres uses the exact existing compose pgvector image and full digest, database `lamuflix`, existing `DefaultConnection` key, AppHost-only startup migration switch and wait-for-Postgres ordering. No new migration or persistent named volume. Existing vector schema is supported without adding Epic 9 work. Recon part 2:53-58; DEV-332:27-29. |
| Q5 — Broker and secrets | Ephemeral `rabbitmq:4-alpine`, generated Aspire secret credential parameters, dynamic localhost host ports, broker reference/wait and explicit `RabbitMq__HostName/Port/UserName/Password` injection. No management UI, hardcoded/logged/committed credentials, or changes to Api secrets ID, playback or options contracts. Recon part 2:62-63,85-87; original recon:56; PRODUCT section 5. |
| Q6 — Setup and health | AppHost README documents prerequisites and existing private Api settings. Inject claim lease 30s, sweep 10s and retry 60s locally. Api `/health/ready`; web waits for Api readiness and probes `/`; Development F5 profile and SDK dashboard. Real browser API/CORS/SignalR proof is required. Recon part 2:65-79; DEV-332:28-29. |
| Q7 — Proof and gates | Add only AppHost mutation exclusion citing Constitution IX; retain all thresholds and existing exclusions. Real-host acceptance and existing IntegrationTests infrastructure; no new test project/packages or mocked host/DB/broker. Run full Phase B gates; N/A and skip outcomes never become PASS. Constitution IX:362,370; AGENTS mutation table; recon part 3:29-31. |
| Q8 — Files and ordering | Exact allowlist below; minimal harness and additive integration-proof edits explicitly approved. ADR 0031, no glossary addition. Quill drafting then analysis/challenge/freeze/Gate 1; Phase B pickup drift check before implementation. DEV-332:28-31; PRODUCT section 5; recon part 3:25; Q1-Q7. |
| Q9 — Loop contract | Critical/High/Medium block closure; every finding needs disposition; all Sentry/Ledger/Compass reports mandatory for L; two-round review cap, no replacement immutable review or silent waiver. Frozen scope below; real in-scope acceptance and gate failures remain blocking. Ticket scope, PRODUCT sections 2-3 and size:L pipeline rules. |
| Q10 — Closure | Patron confirmed shared understanding at 10/12, zero unresolved design questions and zero owner structural checkboxes; authorized this brief and draft ADR. Quill owns spec/plan/tasks. Q1-Q9 and cited ticket/product/recon bases. |

## Approach and configuration

Use `<Project Sdk="Aspire.AppHost.Sdk/13.6.4">`, target `net10.0`, and explicitly set `AspireUseCliBundle` to false. Put the three hosting package versions in `Directory.Packages.props`; project package references consume CPM. The SDK owns dashboard/orchestration dependencies. Reference the existing Api project for generated `Projects` resource metadata; do not modify the Api project or introduce a worker, service layer or new test project.

Provision disposable Postgres and RabbitMQ containers independently of compose. Copy the full current `docker-compose.yml:3` image/digest into AppHost configuration during implementation; recon abbreviates the digest, so no abbreviated or invented hash belongs in code. Keep all container host mappings local and dynamic. No persistent named volume is selected; document that recreating containers may discard local data.

| AppHost mapping | Required behavior |
|---|---|
| Postgres database | `lamuflix`, using existing pgvector image; reference/inject `ConnectionStrings__DefaultConnection`. |
| Api migrations | Inject `Database__ApplyMigrationsOnStartup=true` for the AppHost-launched Api only; wait for Postgres readiness before Api startup. Reuse existing migrations. |
| Broker | Retain `WithReference(rabbitmq)` and `WaitFor(rabbitmq)`; map reachable endpoint host and dynamic host port to `RabbitMq__HostName` and `RabbitMq__Port`. |
| Broker credentials | Inject `RabbitMq__UserName` and `RabbitMq__Password` from the same generated secret parameters used by the broker. A connection-string reference alone does not configure the current Api. |
| Local timing | `Enrichment__ClaimLease=00:00:30`, `Enrichment__SweepInterval=00:00:10`, `RabbitMq__RetryDelay=00:01:00`; retry remains strictly greater than claim lease. |
| Web | Core executable runs `npm run dev -- --port 5173 --strictPort` from resolved `web` directory, fixed localhost 5173 without Aspire endpoint proxy contention. |
| Browser routing | Inject `VITE_API_BASE_URL` from Api HTTP endpoint before Vite starts; current typed client and SignalR consume it. Existing `http://localhost:5173` CORS policy remains unchanged. |
| Readiness | Api probes `/health/ready`; web waits for Api readiness and probes HTTP `/`. Containers must actually be healthy, not merely started. |

Preserve existing validation and consumer behavior. The README must document SDK per `global.json`, OCI runtime, Node `>=24 <25`, npm dependency installation, and private Api user-secret setup for `Library:RootPath` and `Omdb:ApiKey`. Do not read, copy, log or commit live secret values. Missing configuration must fail clearly; never bypass validation, disable consumers or weaken health checks to obtain a green dashboard. External OMDb/TMDb/Ollama checks are outside the existing ready-tag set; no Ollama resource is added. Keep Api `UserSecretsId`, `Features:LocalPlay`, `Process.Start` and public API/contracts unchanged.

Add AppHost Development/F5 launch settings and SDK dashboard startup. Verify rather than assume Windows npm shim resolution, working-directory resolution from the documented command, and no proxy contention on 5173. The local README launch command is `dotnet run --project src/LamuFlix.AppHost` from the repository worktree, after prerequisite setup.

## Files touched

Planning ownership:

- Keel: `specs/DEV-332/CONCLUSIONS.md`, `specs/DEV-332/brief.md`, `docs/adr/0031-local-aspire-apphost.md`.
- Quill: `specs/DEV-332/spec.md`, `plan.md`, `tasks.md`, drafted from this brief. Keel reviews these read-only and routes one numbered fix list per round.

Phase B implementation allowlist:

- New `src/LamuFlix.AppHost/LamuFlix.AppHost.csproj`, `Program.cs`, `Properties/launchSettings.json`, `README.md`.
- Narrow additive edits to `LamuFlix.sln`, `Directory.Packages.props`, `harness.yml`.
- Meaningful additive host-proof edits under existing `tests/LamuFlix.IntegrationTests` only if needed, using its existing infrastructure/framework; no project/package changes or whole-file rewrite/deletion.

No edits to existing Api, ServiceDefaults, Infrastructure or Core code; `web/`; compose files; root README; `global.json`; generated configuration or contracts. If runtime proof exposes a necessary wider change, retain the acceptance blocker and obtain a cited ruling before editing. The allowlist cannot waive an in-scope defect. New dependency, new layer/project, schema, public API, execution/security and unnamed-file changes require the applicable explicit care-list ruling; only ticket contradiction or necessary constitution departure escalates to the owner.

## Test strategy and gate expectations

Real-host acceptance is primary proof. Use existing IntegrationTests infrastructure for meaningful automation without new dependencies or mocked Aspire/database/broker behavior; do not add assertions that merely mirror the wiring. Capture bounded receipts for:

1. Fresh disposable pgvector database starts and existing migrations apply before Api serves.
2. RabbitMQ uses its dynamically assigned host port and matching secret parameters; Api broker/consumer checks become ready.
3. `dotnet run --project src/LamuFlix.AppHost` starts exactly postgres, rabbitmq, api and web healthy; dashboard is available and no worker resource exists. SDK/dashboard bookkeeping is not an extra application resource.
4. Browser at `http://localhost:5173` loads the SPA and reaches Api successfully, including CORS and SignalR connection, through injected `VITE_API_BASE_URL`.
5. F5 Development profile works; Windows npm shim and working-directory resolution work; no Aspire/Vite port collision occurs.
6. Occupied 5173 fails rather than silently moving the SPA; missing private prerequisites fail clearly without validation/health bypasses.
7. Compose files and existing Api/frontend/contracts remain unchanged; local disposable-data and compose boundary documentation is present.

Phase B runs solution build and required analyzer, complexity, InspectCode, property-test, vulnerable-package, format, full-test and mutation gates per harness pipeline. No gates were run during this grill. Original recon baseline receipts are measurements, not current-head approval. Run web gates only if a subsequently authorized change actually produces a web diff.

Add `LamuFlix.AppHost` to `harness.yml` mutation exclusions with reason `orchestration host proof lives in IntegrationTests per Constitution IX`; preserve Core whole-project mutation, all thresholds and existing exclusions. Never edit generated files to implement the exclusion. All-excluded eligible scope is `NOT APPLICABLE / N/A`, never PASS; scope-empty exit 2 is `SKIPPED (scope-empty)`, opt-out is `SKIP`, and an unavailable required runtime/gate is `Could not run`, never success. Required acceptance that could not run blocks closure.

## Task ordering and handoff

1. Quill drafts spec, plan and tasks from this brief and ADR, preserving every Q1-Q10 ruling and evidence boundary.
2. Keel runs read-only speckit analysis and brief consistency review; Quill receives numbered fixes. Patron decides any new decision, recorded here first through append-only conclusions and corresponding brief update.
3. Plan challenge requires all Sentry/Ledger/Compass reports and explicit disposition of every finding. Plan freeze and Patron Gate 1 follow independently; this brief does not claim either.
4. Phase B pickup checks main/contract/file drift and reruns analysis before implementation; amend tasks for justified drift through the assigned workflow.
5. Add AppHost project/solution/CPM pins and minimal harness exclusion; configure containers, parameters, Api references/environment/readiness; then Vite executable, launch profile and local README.
6. Prove fresh-host and negative paths, add meaningful existing-infrastructure integration proof as needed, run full gates, then bounded review and remediation. Findings and summary go on the PR; fixes follow the review-thread workflow.
7. Preserve exact-head evidence and stop at `awaiting-merge`; the owner alone merges.

## Loop discipline

Closing bar for code-review/ship-review: zero unresolved Critical, High or Medium findings; explicit disposition for every Low/Follow-up item; all Sentry/Ledger/Compass axes present; required gates and runtime acceptance evidenced. Unnecessary abstraction goes to Follow-up, not a speculative fix commit. Missing axis reports or acceptance evidence block closure.

Frozen scope: the Q1-Q8 local AppHost topology, configuration adapters, approved project/packages, launch/docs/ADR, minimal harness exclusion and real-host proof within the file allowlist; anything else is a follow-up issue, not a finding in this round.

An actual in-scope acceptance/gate defect remains a finding even when its fix needs a wider authorization. Review round cap: **two**, hard limit. No replacement immutable PR review to repair a receipt; unresolved blockers after round two stop for Conductor disposition, never automatic waiver. Grill cap: **12**, closed at **10**. The review cap and grill cap are separate.

## Open items and phase boundary

Unresolved design questions: **zero**. Owner structural checkboxes: **zero** (Patron Q10). Implementation-time Windows command/endpoint behavior and live acceptance remain unproven obligations, not asserted successes. No taste assumptions were introduced. ADR is a draft; spec/plan/tasks are ready for Quill drafting. No implementation, local gate clearance, plan freeze, Gate 1 approval, PR approval or merge is claimed.

## Plan challenge decision PC1 (2026-10-29)

Patron ruled no constitution amendment or owner structural checkbox is needed: Constitution I:178-197 constrains application assemblies; AppHost is an outer local-dev launcher with one-way Api metadata reference. Constitution Tech Stack:393-416,588 retains compose and permits this additive ticket-named local entrypoint. No ServiceDefaults duplication or direct Core/Infrastructure/ServiceDefaults reference. Full exchange and cited rationale: CONCLUSIONS PC1.

Keel must change ADR0031 from proposed to Accepted at plan freeze before Gate 1 (constitution:559-561,609). Quill aligns spec/plan/tasks to this timing. ADR remains proposed until freeze. Plan challenge findings and runtime proof remain separate blockers; this decision is not PLAN FROZEN or Gate 1.

## Plan challenge decision PC2 (2026-10-29)

Patron approved replacing Q7 exact AppHost mutation exclusion reason with `local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX`. Meaningful automated IntegrationTests remain optional; no new packages/projects, no mocked infrastructure, no threshold changes and no waiver of blocking real-host acceptance. Full cited exchange: CONCLUSIONS PC2. This later ruling supersedes the earlier Q7 reason wherever repeated; Quill aligns spec/plan/tasks before closure.

## Plan challenge decision PC3 and recon construction mapping (2026-10-29)

Patron PC3 supersedes earlier Q3 npm launch text: use core AddExecutable with PATH-resolved `node`, absolute web working directory computed from `builder.AppHostDirectory` and argument array `["node_modules/vite/bin/vite.js", "--port", "5173", "--strictPort"]`. Keep npm install prerequisite, existing VITE_API_BASE_URL, fixed localhost 5173 and WithEndpointProxySupport(false). No shell/cmd.exe, machine-specific source path, Process.Start, LocalPlay or web/package edit. Live Windows launch/proxy/strict-port/browser proof remains mandatory Phase B. Full cited exchange in CONCLUSIONS PC3.

Recon-backed construction refinements (no live host proof claimed):

- recon-DEV-332-4:9-13: override stock postgres using `WithImage` for pgvector/pgvector with tag 0.8.7-pg17 and `WithImageSHA256` for the compose digest; digest is a separate field, never pass image@digest as the image name. The exact digest argument representation and composed/running identity must be validated at Phase B; do not invent a successful pull.
- recon-DEV-332-4:17-21: `postgres.AddDatabase(name: "DefaultConnection", databaseName: "lamuflix")`; Api references that database resource for ConnectionStrings__DefaultConnection and waits for its availability. Parent Postgres and database health are built in; no Api rename/schema edit.
- recon-DEV-332-4:25-31: WaitFor dependencies with attached health checks requires Running plus Healthy; built-in Postgres/database and RabbitMQ checks establish service readiness. Api uses `/health/ready`; web WaitFor(api) uses that check, never WaitForStart as readiness substitution.
- recon-DEV-332-4:35-42: explicitly declare AppHost Api `WithHttpEndpoint(name: "http")`, retrieve `api.GetEndpoint("http")` for VITE_API_BASE_URL and use that same endpoint for WithHttpHealthCheck `/health/ready`. Api launchSettings stays absent/untouched. Plain-HTTP/runtime startup must be proven without weakening security; any necessary wider change or departure returns for ruling.
- recon-DEV-332-4:58-63: inject RabbitMq__Port using the primary endpoint `Property(EndpointProperty.Port)` through WithEnvironment IExpressionValue; host separately via Host expression. Never URL or TargetPort for the Api integer host port. Actual resolved numeric value/mapped-port equality and readiness remain Phase B proof.
- recon-DEV-332-5:3-10: retain explicit versionless Aspire.Hosting.AppHost PackageReference and CPM 13.6.4 pin; SDK skips implicit add when explicit exists and resolves CPM versions. No speculative reference deletion. Actual clean restore/build and transitive vulnerability gate remain required.

## Plan challenge decision PC4 (2026-10-29)

Patron approved a distinct nonsecret AppHost UserSecretsId in the new AppHost csproj for its own generated resource-credential persistence. Use each AddPostgres/AddRabbitMQ generated credential set once, not additional duplicate T006 parameters; Api RabbitMq__UserName/Password come from the broker resource UserNameReference/PasswordParameter. Default username configuration is supplied by integration, not a new hardcoded credential. Explicitly set both child Api DOTNET_ENVIRONMENT and ASPNETCORE_ENVIRONMENT to Development; the Api entry assembly loads its own unchanged user-secrets ID. Stores stay separate: AppHost never reads/copies Api store, Api never needs AppHost store location. No secret values/logs/commits or machine paths. Basis constitution VII; recon-DEV-332-4:40-42 and -5:14-19; full cited Patron exchange CONCLUSIONS PC4.

These appended PC1-PC4 and recon construction refinements are current and supersede earlier conflicting brief text only as stated. No open plan-time API/ownership decision remains after their incorporation; actual runtime checks remain unproven blocking Phase B obligations. Plan freeze still requires Quill final round and independent read-back.

## Plan freeze receipt (2026-10-29)

PLAN FROZEN: Keel independently read back Quill round 2 spec/plan/tasks and DRAFTING_RECEIPT against the final eight-item list, recon parts 4–5 and Patron PC1–PC4. All 25 axis findings have final dispositions; no accepted plan finding remains unresolved. The complete per-finding closure record and current status are in PLAN_CHALLENGE.md. Two correction rounds used; no third round or waiver.

Current frozen scope is Q1–Q10 as refined by PC1–PC4 and the recon construction mapping above. Earlier conflicting npm-launch, duplicate-parameter, exclusion-reason or pending-recon passages are preserved historical decisions, superseded only as explicitly recorded. Quill draft-time status labels describe the authoring phase; the current freeze receipt in PLAN_CHALLENGE.md governs plan status.

ADR0031 is now Accepted per PC1 before Gate 1. No owner structural checkbox or unresolved design decision. This is plan freeze only: Patron Gate 1 is still required; Phase B pickup drift/re-analysis, implementation, real-host proofs, all local/hosted gates and reviews remain unrun/unapproved. No implementation authorization or gate PASS is inferred.

## Phase B rulings (2026-10-30)

- **Ollama host prerequisite (`CONCLUSIONS.md` Phase B)**: `src/LamuFlix.AppHost/README.md` documents a locally reachable Ollama service (`http://localhost:11434`) with model `all-minilm` as a host prerequisite for `LamuFlix.Api`'s existing `OllamaEmbeddingStartupValidator`, keeping the AppHost topology strictly limited to the four ticket-named resources (`postgres`, `rabbitmq`, `api`, `web`) with no Ollama resource added and no startup validation weakened.
- **Compass C1 — F5 launch profile proof (`CONCLUSIONS.md` Phase B)**: Proof item 5 (`F5 Development profile works`) and `spec.md` US1/US5/FR-011/FR-014 are satisfied by the committed `Properties/launchSettings.json` profile being loaded by `dotnet run --project src/LamuFlix.AppHost` and starting the SDK dashboard (`https://localhost:17000`) plus all four healthy resources (`ACCEPTANCE_RECEIPTS.md` sections `A`, `A3`, `A5`, `B3` row 1 `PASS`). Interactive IDE `F5` keystroke and OS desktop `launchBrowser` window automation are outside ticket/spec/brief scope; recording `Could not run (headless)` on that sub-row in `B3` is an honest boundary note, not an unrun in-scope acceptance blocker under Q7/Q9 (no waiver, no scope change, no `blocked: structural`).
