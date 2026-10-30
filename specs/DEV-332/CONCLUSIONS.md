# DEV-332 — Grill conclusions

Worktree: F:/Dev/LamuFlix.worktrees/feature-332-spec  
Branch verified: feature/332-spec  
Phase: Phase 2 step 1; question cap 12. This record is append-only.  
Authority: ticket as captured in note DEV-332; facts only from recon-DEV-332; specs/PRODUCT.md.

---

## Q1 — Local orchestration boundary

### Keel question (full exchange)

[from Keel] /grill-with-docs DEV-332. Phase 2 step 1 in F:/Dev/LamuFlix.worktrees/feature-332-spec branch feature/332-spec. I read DEV-332, recon-DEV-332, and specs/PRODUCT.md. Cap 12 questions, one decision at a time; I record full Q and A append-only in specs/DEV-332/CONCLUSIONS.md and close with brief.md. Facts only from recon. Q1: Confirm the scope boundary: implement the ticket-named local AppHost with postgres, rabbitmq, api, web on 5173, dashboard and F5; AppHost provisions its own local containers; no worker, no Ollama resource, no compose changes, no deployment changes. I recommend this because DEV-332:27-31 explicitly names that topology and excludes compose/deployment, and recon:54 shows compose ports cannot serve the host Api. Cost: local Aspire and compose remain separate dev entry points. Please cite the basis for your ruling; ticket changes require blocked structural. Report your answer back using maestri ask Keel.

### Patron answer (complete)

[from Patron] Confirmed: AppHost provisions its own local Postgres and RabbitMQ containers and orchestrates api, web (Vite on 5173), dashboard, and F5 launch profile, with no worker, no Ollama resource, and no compose or deployment changes.
- Ticket DEV-332:27-31 explicitly defines the four resources (postgres, rabbitmq, api, web on 5173), excludes worker, compose changes, and deployment/production, and specs/PRODUCT.md §3 makes the ticket Scope & Technical Design authoritative.
- Per recon-DEV-332:54, docker-compose publishes no host ports for postgres or rabbitmq and DEV-332:31 forbids compose edits, so AppHost must provision its own local containers as a separate local-dev entrypoint.
- Specs/PRODUCT.md §2 strictly defers Epic 9 (Ollama / pgvector) until after Epic 7, matching the omission of Ollama from DEV-332:28.

### Ruling, rationale, and implications

The ticket resource boundary is settled: four application resources plus the Aspire dashboard, with Postgres and RabbitMQ owned by AppHost for local development. Compose remains a separate entrypoint. No worker, Ollama resource, compose edit, or deployment change is authorized. Basis: DEV-332:27-31; recon-DEV-332:54; specs/PRODUCT.md §§2-3. This does not settle the Postgres image required by the existing database model, dependency versions, configuration mapping, test strategy, gate expectations, review closing bar, or round cap. No owner checkbox is closed and no plan is frozen.

---

## Q2 — Project and dependency pins

### Keel question

[from Keel] DEV-332 /grill-with-docs resume Q2/12: recon-DEV-332-2:24-28 reports latest aligned SDK/AppHost/PostgreSQL/RabbitMQ pins 13.6.4, while the Conductor summary and Learn example say 13.6.0. I recommend explicitly pinning all four to 13.6.4, with SDK version in AppHost csproj, hosting versions in Directory.Packages.props, and explicit AspireUseCliBundle=false to avoid ASPIRE010 (recon:14). No separate dashboard package: SDK supplies it. Approve this exact new-dependency set and src/LamuFlix.AppHost project in the solution src folder, based on DEV-332:28 and PRODUCT sections 3/5? Cost: four coordinated version pins and new project build surface. No JS hosting dependency yet; that is Q3. Cite basis and report back to Keel.

### Patron answer

[from Patron] Approved: Add src/LamuFlix.AppHost/LamuFlix.AppHost.csproj to LamuFlix.sln under the src solution folder and pin Aspire.AppHost.Sdk, Aspire.Hosting.AppHost, Aspire.Hosting.PostgreSQL, and Aspire.Hosting.RabbitMQ at 13.6.4 with AspireUseCliBundle=false and no separate dashboard package.
- Per DEV-332:28 and specs/PRODUCT.md §3/§5 (#1 and #2), the ticket explicitly requires the LamuFlix.AppHost project in LamuFlix.sln and pinned Aspire packages in Directory.Packages.props, while recon-DEV-332-2:24-28 verifies 13.6.4 is the latest mutually aligned NuGet version across all four packages.
- Per Microsoft Learn (recon-DEV-332-2:14), declaring <Project Sdk=Aspire.AppHost.Sdk/13.6.4> supplies the Aspire Dashboard and orchestration packages implicitly, and setting <AspireUseCliBundle>false</AspireUseCliBundle> avoids warning ASPIRE010 under the repo TreatWarningsAsErrors=true posture (specs/PRODUCT.md §1).

### Ruling and implications

Approved exactly as answered. Hosting package versions belong in Directory.Packages.props; SDK pin belongs in the project SDK declaration. New-project and dependency care-list decisions are explicit. Dashboard remains SDK-managed. This settles dependency selection, not gate readiness.


---

## Q3 — Vite orchestration

### Keel question

[from Keel] DEV-332 /grill-with-docs Q3/12: Choose Vite hosting. I recommend the verified core AddExecutable route with existing npm run dev -- --port 5173 --strictPort, web working directory resolved from AppHost location, VITE_API_BASE_URL from api HTTP endpoint, localhost-only fixed 5173 with proxying disabled so Aspire does not contend with Vite. Existing CORS, web source and generated contracts remain unchanged. Basis recon-DEV-332-3:13-21 and recon-DEV-332-2:40-47; no fifth dependency needed. Cost: implementation must prove Windows npm shim resolution and no proxy/port conflict with actual startup, including occupied-5173 failure; do not treat metadata as runtime proof. Approve this design over adding Aspire.Hosting.JavaScript 13.6.4? Cite basis and report back to Keel.

### Patron answer

[from Patron] Approved: Use core AddExecutable for the web resource running npm run dev -- --port 5173 --strictPort with working directory resolved to web, VITE_API_BASE_URL injected from the api HTTP endpoint, and fixed localhost port 5173 with endpoint proxying disabled, rather than adding Aspire.Hosting.JavaScript.
- Per specs/PRODUCT.md §5 (#1 and #6) and recon-DEV-332-3:13-18, AddExecutable, WithWorkingDirectory, WithEnvironment, and WithHttpEndpoint are already in the approved Aspire.Hosting 13.6.4 transitive closure, avoiding a fifth NuGet dependency while keeping web/ files and generated contracts untouched.
- Per DEV-332:28-29 and recon-DEV-332-2:40-47, passing --port 5173 --strictPort with VITE_API_BASE_URL set to the api HTTP endpoint satisfies the 5173 port and CORS contract (http://localhost:5173 in Program.cs:20) and fails fast if port 5173 is occupied.
- Per recon-DEV-332-3:19, implementation verification must prove runtime startup on Windows (including npm shim resolution, no Aspire proxy contention on 5173, and strict-port failure when 5173 is occupied).

### Implications

Core-only executable hosting is settled. Windows startup, port conflict and proxy behavior remain runtime acceptance obligations, not assumed facts. No web file rewrite is authorized.


---

## Q4 — Database lifecycle and existing vector schema

### Keel question

[from Keel] DEV-332 Q4/12: Database lifecycle. I recommend AppHost-owned ephemeral local Postgres using the existing compose pgvector image pin (copy the full existing digest during implementation; never edit compose), database lamuflix referenced via the existing DefaultConnection key, Database__ApplyMigrationsOnStartup=true only for AppHost-launched api, and api waits for Postgres readiness before startup migrations. No new migrations, schema or persistence layer; existing vector schema requires pgvector even though new Epic 9 work is deferred. No named persistent volume in this ticket; local data may disappear when containers are recreated, documented clearly. Basis recon-DEV-332-2:53-58 and Q1 scope. Approve this lifecycle and clarify that Q1 deferred new recommendations work, not support for already-landed vector schema? Cost: cold local DB must migrate and dev data is disposable. Cite basis and report back to Keel.

### Patron answer

[from Patron] Approved: Configure the AppHost Postgres resource as an ephemeral local container using the exact pgvector image and digest from docker-compose.yml:3, exposing database lamuflix via the DefaultConnection connection key, with Database__ApplyMigrationsOnStartup=true injected into the AppHost-launched api and WaitFor(postgres) ordering before startup.
- Clarifying Q1: deferring Epic 9 (specs/PRODUCT.md §2) excludes new Ollama/recommendations resources, not the already-landed vector schema and migration (20261018032119_EnableVector.cs) which require the pgvector extension on Postgres (recon-DEV-332-2:53-54).
- Per DEV-332:28-29 and recon-DEV-332-2:55-56, binding the database resource to ConnectionStrings__DefaultConnection, waiting for Postgres readiness, and setting Database__ApplyMigrationsOnStartup=true for the AppHost api run ensures a fresh local container migrates and passes /health/ready without touching existing persistence code or docker-compose.yml.
- Ephemeral container lifecycle (no persistent named volume in DEV-332) matches DEV-332:27 local-dev scope and avoids undocumented volume state.

### Implications

Q1 is clarified by this later ruling; its original exchange is preserved. Existing schema support is required, no new recommendation scope is introduced. Disposable local data and fresh-start migration proof must be documented.

---

## Q5 — Broker endpoint and secrets

### Keel question

[from Keel] DEV-332 Q5/12: Broker and secret boundary. I recommend ephemeral AppHost-owned RabbitMQ using the existing rabbitmq:4-alpine image precedent, generated Aspire secret parameters for Postgres and RabbitMQ credentials, and explicit api RabbitMq__HostName, RabbitMq__Port, RabbitMq__UserName, RabbitMq__Password injection from the reachable host endpoint and matching broker parameters. Also retain the ticket-required broker reference, but do not assume ConnectionStrings__rabbitmq configures the existing Api. Wait for broker readiness. All container ports dynamically mapped and local-only; no fixed 5672, management UI addition, hardcoded credentials or changes to Api secrets ID, playback, Process.Start, or existing options contracts. Basis recon-DEV-332-2:62-63,85-87 and original recon:56. Approve? Cost: endpoint-to-options wiring needs a live host-port proof; generated secrets must never enter logs or commits. Cite basis and report back to Keel.

### Patron answer

[from Patron] Approved: Configure the AppHost RabbitMQ resource as an ephemeral local container using the rabbitmq:4-alpine image precedent, use Aspire secret parameters for Postgres and RabbitMQ credentials (never hardcoded), retain WithReference(rabbitmq) and WaitFor(rabbitmq) on api, and explicitly inject RabbitMq__HostName, RabbitMq__Port, RabbitMq__UserName, and RabbitMq__Password from the dynamically mapped localhost endpoint and broker parameters.
- Per recon-DEV-332-2:62-63 and 85-87, LamuFlix.Api binds RabbitMqOptions (HostName, Port, UserName, Password) rather than ConnectionStrings__rabbitmq, so explicit environment injection from the resolved endpoint and parameters is required alongside DEV-332:28 WithReference/WaitFor wiring.
- Per charter §2.3 scrub/secrets rules, specs/PRODUCT.md §5 (#5), and recon-DEV-332:56, credentials must be Aspire secret parameters (never hardcoded, logged, or committed), all container ports remain dynamically mapped to localhost with no management UI added, and Api UserSecretsId, Features:LocalPlay, Process.Start, and existing options contracts remain untouched.

### Implications

Reachable mapped ports and matching generated credential parameters are required. A reference alone is insufficient for RabbitMqOptions. Secret-handling care-list ruling is explicit and does not authorize playback changes.

---

## Q6 — Prerequisites, launch profile, and health

### Keel question

[from Keel] DEV-332 Q6/12: Developer prerequisites and health. I recommend an AppHost README documenting .NET SDK per global.json, OCI runtime, Node >=24 <25 and npm install before launch; existing Api user secrets supply Library:RootPath and Omdb:ApiKey, never dummy committed values. AppHost supplies non-secret local defaults Enrichment__ClaimLease=00:00:30, Enrichment__SweepInterval=00:00:10 and RabbitMq__RetryDelay=00:01:00, satisfying the strictly-greater lease rule. Preserve existing Api option validation; missing prerequisites must fail clearly rather than disabling consumers/checks. API resource health uses /health/ready; web starts after Api readiness and HTTP GET / proves its dev endpoint. Launch profile is AppHost Development/F5 with SDK dashboard; startup proof requires actual postgres/rabbitmq/api/web healthy and browser API/CORS/SignalR routing proof, not merely running processes. Basis recon-DEV-332-2:65-79. Approve? Cost: developers configure their existing private Api prerequisites before the single AppHost command; external metadata/Ollama health is not part of ready. Cite basis and report back to Keel.

### Patron answer

[from Patron] Approved: Document prerequisites (.NET SDK per global.json, OCI runtime, Node >=24 <25, npm install, and existing Api user secrets for Library:RootPath and Omdb:ApiKey) in an AppHost README, inject non-secret local defaults Enrichment__ClaimLease=00:00:30, Enrichment__SweepInterval=00:00:10, and RabbitMq__RetryDelay=00:01:00, probe api at /health/ready, order web after api readiness with an HTTP GET / readiness check, and provide the AppHost Development F5 launch profile with SDK dashboard.
- Per recon-DEV-332-2:67-71 and charter §2.3 scrub/secrets rules, Library:RootPath and Omdb:ApiKey remain in existing Api user secrets (never dummy committed values), while non-secret timing defaults (RetryDelay 00:01:00 > ClaimLease 00:00:30, SweepInterval 00:00:10 > 0) satisfy ValidateOnStart validators (RabbitMqConsumerOptionsValidator.cs:10-34, EnrichmentOptionsValidator.cs:13-21) without weakening validation.
- Per DEV-332:28-29 and recon-DEV-332-2:75-79, /health/ready verifies postgres, rabbitmq, both consumers, and outbox-relay (excluding external OMDb/TMDb/Ollama checks), and ordering web after api with HTTP GET / on 5173 plus live browser API/CORS/SignalR proof fulfills the four-resources-healthy and F5 launch profile acceptance criteria.

### Implications

Health cannot be replaced by process-running status. Existing Api user secrets are required setup, not committed placeholders. AppHost-only timing values satisfy current validators; production defaults remain unchanged.

---

## Q7 — Real-host proof and mutation policy

### Keel question

[from Keel] DEV-332 Q7/12: Proof and mutation policy. I recommend add only harness.yml gates.mutation.exclusions.LamuFlix.AppHost with reason orchestration host proof lives in IntegrationTests per Constitution IX (line 370 scopes mutation to Core; recon-DEV-332-3:29-31). Keep all thresholds, Core whole-project mutation and existing exclusions unchanged. Validate with real-host acceptance receipts: clean disposable DB migrations, mapped RabbitMQ port works, all four healthy, browser API/CORS/SignalR connection, F5 dashboard, no worker, occupied-5173 failure and missing-config failure. Use existing IntegrationTests infrastructure for any automated host proof; no mocked Aspire/DB/broker, new test project or test package, and no tests merely mirroring wiring. Run solution build, analyzers, complexity, InspectCode, property/security/format/full tests and mutation per pipeline in Phase B. AppHost-only eligible production scope should be N/A if all changed projects are excluded, never PASS; scope-empty SKIPPED and disabled SKIP remain distinct. No web gate required absent web diff. Approve? Cost: requires OCI-enabled live verification; unavailable runtime is Could not run and blocks acceptance. Cite basis and report back to Keel.

### Patron answer

[from Patron] Approved: Add LamuFlix.AppHost to harness.yml gates.mutation.exclusions with reason orchestration host proof lives in IntegrationTests per Constitution IX, preserve all existing thresholds and exclusions, and require real-host acceptance receipts plus existing IntegrationTests infrastructure with no new test project, no new test packages, and no mocked Aspire/DB/broker tests.
- Per Constitution IX (.specify/memory/constitution.md:362, 370), AGENTS.md:125-134, and recon-DEV-332-3:29-31, Stryker mutation is scoped to LamuFlix.Core, and adding LamuFlix.AppHost to gates.mutation.exclusions is forced by the mutation gate so an AppHost-only change exits 2 (reported as NOT APPLICABLE / N/A, never PASS) without lowering any threshold.
- Per DEV-332:29 and AGENTS.md non-negotiables, verification must prove real container startup, migrations, dynamic RabbitMQ port binding, all four resources healthy, browser API/CORS/SignalR connectivity, F5 dashboard, no worker resource, occupied-5173 failure, and missing-prerequisite failure, alongside full Phase B analyzer/test/mutation gates (and no web gate if web/ has no diff).

### Implications

Mutation exclusion is a Constitution IX conformance decision, not a threshold reduction or constitution departure. Gate outcomes retain their actual exit classifications. Runtime acceptance remains blocking when it cannot run.

---

## Q8 — Files, ADR, and execution ordering

### Keel question

[from Keel] DEV-332 Q8/12: File boundary and ordering. I recommend implementation allowlist: new src/LamuFlix.AppHost/{LamuFlix.AppHost.csproj,Program.cs,Properties/launchSettings.json,README.md}; narrow additive edits LamuFlix.sln, Directory.Packages.props, harness.yml only; meaningful host-proof additions under existing tests/LamuFlix.IntegrationTests only if needed, preserving its existing framework and no project/package changes. Planning files specs/DEV-332/{CONCLUSIONS.md,brief.md,spec.md,plan.md,tasks.md} plus new docs/adr/0031-local-aspire-apphost.md. Explicit care-list approval for minimal harness edit and any existing integration-test-file edit; no wholesale rewrites or deletions. No edits to Api/ServiceDefaults/Infrastructure/Core, web, compose, root README, global.json or generated configs/contracts. Order Quill spec/plan/tasks, read-only analysis, challenge/freeze/Gate 1, then Phase B pickup drift check, project/pins, containers/config, api health, Vite/launch/docs, real-host proofs, full gates/review. ADR records separate disposable local orchestration versus unchanged compose and existing-configuration adaptation; no new glossary term needed. Approve? Cost: anything needing a wider edit returns for a cited ruling; this is not permission to conceal an acceptance blocker. Basis ticket scope, Q1-Q7, PRODUCT 5 and recon-DEV-332-3:25. Cite basis and report back to Keel.

### Patron answer

[from Patron] Approved: Adopt the exact file allowlist (src/LamuFlix.AppHost/*, narrow additive edits to LamuFlix.sln, Directory.Packages.props, harness.yml, and tests/LamuFlix.IntegrationTests if needed, specs/DEV-332/{CONCLUSIONS.md,brief.md,spec.md,plan.md,tasks.md}, and docs/adr/0031-local-aspire-apphost.md with no glossary additions) and the Phase A/Phase B execution ordering.
- Per DEV-332:28-31, specs/PRODUCT.md §5 (#6), and Q1-Q7, LamuFlix.sln and Directory.Packages.props are ticket-named, harness.yml is forced by the mutation gate per Constitution IX, and additive host-proof edits in tests/LamuFlix.IntegrationTests are explicitly approved while Api/ServiceDefaults/Infrastructure/Core, web/, compose, root README, global.json, and generated files remain untouched.
- Per recon-DEV-332-3:25, docs/adr/0031-local-aspire-apphost.md is the next sequential ADR (matching the 0026-0030 naming convention) to record disposable local Aspire orchestration alongside unchanged compose and existing Api configuration contracts.

### Implications

File-scope care-list rulings are explicit. Existing integration-test-file edits are additive only; no new test package/project is approved. Quill owns spec/plan/tasks drafting; Keel writes the brief and ADR. No production implementation is authorized during this planning step.

---

## Q9 — Review-loop contract

### Keel question

[from Keel] DEV-332 Q9/12: Approve this review-loop contract as one closing decision: Critical/High/Medium findings block code-review and ship-review closure; Low items receive explicit disposition, Follow-up never becomes an unnecessary abstraction fix. All Sentry/Ledger/Compass axis reports are required for this L ticket and every finding needs disposition; missing evidence or required gates/runtime acceptance not met blocks closure. Freeze to the Q1-Q8 AppHost topology, configuration adapters, new project/pins, launch/docs/ADR, minimal harness exclusion and host proof within the allowlist; anything else is a follow-up issue, not a finding in this round. Review round cap two, with no replacement immutable PR review to repair receipts; unresolved blockers after two rounds stop for Conductor disposition, never auto-waive. Actual in-scope acceptance failures remain findings, even if a fix needs new authorization. Grill cap remains 12, separate from review cap. Recommend this because seat duties require all axes and bounded review, ticket forbids scope expansion. Cost: stop rather than silently expand scope or extend rounds. Cite basis and report back to Keel.

### Patron answer

[from Patron] Approved: Adopt the closing review-loop contract (Critical/High/Medium block closure; Low/Follow-up receive explicit disposition without speculative abstractions; all Sentry/Ledger/Compass axes required for size:L; scope frozen to Q1-Q8 allowlist; two-round review cap with no replacement immutable PR review; grill cap 12).
- Per DEV-332:27-31, specs/PRODUCT.md §2-§3, and Q1-Q8, freezing scope to the approved AppHost topology and file allowlist prevents out-of-scope expansion while keeping any real in-scope acceptance or gate failure blocking.
- Per team charter §2.3 and size:L pipeline rules, requiring all three review axes, explicit finding disposition, and a strict two-round cap ensures bounded, auditable review without silent waivers or duplicate PR review artifacts.

### Implications

All three axes and every finding disposition are mandatory. Two review rounds is a hard cap; missing evidence, in-scope acceptance failures and required gate failures cannot be waived by scope language. Grill count and review-round count are separate.

---

## Q10 — Shared understanding and grill closure

### Keel question

[from Keel] DEV-332 Q10/12 shared-understanding closure: Q1-Q9 now settle the ticket-only topology, four aligned 13.6.4 pins, core executable Vite hosting, ephemeral pgvector with existing migration startup, explicit mapped RabbitMqOptions plus generated secret parameters, private Api prerequisites/non-secret timing values, readiness/F5/browser proof, Constitution IX exclusion and full Phase B gates, exact file boundary/ADR 0031/order, and the two-round/all-three-axis blocking review bar. I recommend close the grill at 10 questions with no unresolved design question or owner structural checkbox; retain Windows npm/proxy startup and other runtime checks as blocking implementation proofs rather than unresolved design assumptions. I will write brief.md with every answer, cited bases, approach/files/test strategy/gates/order/loop terms and draft ADR 0031; Quill alone drafts spec/plan/tasks. This is grill/brief closure only, not plan freeze, Gate 1, implementation or gate approval. Confirm shared understanding and this closure disposition, or identify a specific remaining decision. Cite basis and report back to Keel.

### Patron answer

[from Patron] Confirmed: Shared understanding verified across Q1-Q9; close the DEV-332 grill at 10/12 questions with zero unresolved design questions or owner structural checkboxes, and proceed to write specs/DEV-332/brief.md and draft docs/adr/0031-local-aspire-apphost.md (Basis: DEV-332:27-31, specs/PRODUCT.md §3/§5, recon-DEV-332-2, recon-DEV-332-3, and Q1-Q9 rulings in specs/DEV-332/CONCLUSIONS.md).

### Implications

Grill closed at 10 of 12 permitted questions. Zero unresolved design questions or owner structural checkboxes. Runtime acceptance obligations remain blocking implementation proof. Quill drafts spec/plan/tasks next; plan freeze, Gate 1, Phase B implementation and gate success remain separate later claims.

---

## PC1 — Plan challenge constitution interpretation and ADR timing

### Keel question

[from Keel] DEV-332 plan challenge decision PC1: Ledger H1/M4 challenge plan Constitution PASS. Constitution I:178-196 strict project reference table lacks AppHost; Tech Stack:393-416 mandates compose and names dashboard only; governance:597-612 requires amendment for noncompliance. Q2 ticket-named AppHost does not by itself settle HOW compliance. Rule whether local-only AppHost metadata reference to Api alongside unchanged compose conforms without amendment, citing exact basis, or needs amendment and owner blocked: structural checkbox under section 2.3(b). Also rule ADR0031 acceptance timing under constitution:559-561; currently proposed. No constitution edits authorized by Q8. Please read named artifacts, decide this single constitution question, report back with maestri ask Keel, and do not edit files.

### Patron answer (complete)
[from Patron] Ruled (PC1): Local-only `LamuFlix.AppHost` referencing `LamuFlix.Api` alongside unchanged compose conforms to the Constitution without amendment (no §2.3(b) `blocked: structural` checkbox), and `docs/adr/0031-local-aspire-apphost.md` must transition from `proposed` to `Accepted` at plan freeze before Gate 1 (`gate1: provisional`).
- **Principle I (`.specify/memory/constitution.md:178-197`)**: The reference table (`:181-186`) constrains what the four application assemblies (`Core`, `Infrastructure`, `Api`, `ServiceDefaults`) may reference to protect inward hexagonal boundaries (`:173-178`, `:199-203`) enforced by `LamuFlix.ArchitectureTests` (`:197`). None of those four projects references `LamuFlix.AppHost`; `AppHost` is an outer local-dev launcher (`Aspire.AppHost.Sdk`) whose metadata reference to `LamuFlix.Api` is strictly unidirectional (`AppHost -> Api`) and introduces no runtime dependency, port, or adapter into the application layers.
- **Technology Stack & Governance (`.specify/memory/constitution.md:393-416, 588, 597-612`)**: Section `:393-394` requires a constitution amendment only when changing an established technology choice. Keeping `docker-compose.yml` and `docker-compose.observability.yml` (`:416`) untouched while adding the ticket-mandated local AppHost entrypoint (`DEV-332:27-31`; `specs/PRODUCT.md` §§3, 5) aligns with `:413` (`Aspire dashboard as local target`) and `:588` (host API process with containerized Postgres/RabbitMQ) rather than amending or contradicting any constitutional HOW rule.
- **ADR-0031 Acceptance Timing (`.specify/memory/constitution.md:559-561, 609`)**: Because `:559-561` requires architectural decisions in `docs/adr/` to carry status `Accepted` and `:609` requires every PR to comply, `docs/adr/0031-local-aspire-apphost.md` (already within the Q8 file allowlist) must be updated by Keel from `proposed` to `Accepted` at plan freeze prior to Gate 1 (with `spec.md`/`plan.md`/`tasks.md` aligned in the challenge fix pass), recording the settled architectural choice while keeping live runtime proofs as Phase B gate obligations.


### Implications

No constitution amendment or owner structural checkbox; local AppHost metadata-only reference remains outside application assembly dependencies. Do not duplicate ServiceDefaults or reference Core/Infrastructure/ServiceDefaults. Keel changes ADR0031 to Accepted at plan freeze before Gate 1, not now. Quill aligns references and cites this ruling. This does not freeze the plan or clear runtime proof.

---

## PC2 — Accurate mutation exclusion reason

### Keel question

[from Keel] DEV-332 PC2 single decision: Ledger H2 correctly notes Q7 exact exclusion reason orchestration host proof lives in IntegrationTests per Constitution IX is false if optional T017 skips. Recommend amend reason to local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX, keeping meaningful automated IntegrationTests optional with no new package/project, all existing thresholds and blocking runtime proofs unchanged. Basis Constitution IX:362-388 only mandates integration scenarios named there, Q7 real-host primary and Q8 optional additive tests. Approve exact accurate reason or give a feasible alternative within Q8. No constitution departure proposed. Report full cited ruling to Keel with maestri ask; no edits.

### Patron answer (complete)
[from Patron] Approved (PC2): Amend the `harness.yml` `gates.mutation.exclusions.LamuFlix.AppHost` reason to `local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX`, keeping meaningful automated `IntegrationTests` optional (no new test project/packages) and all existing thresholds and blocking runtime acceptance proofs unchanged.
- **Constitution IX (`.specify/memory/constitution.md:362-388`)**: Line `:370` scopes Stryker mutation testing to `LamuFlix.Core` (`:387-388`), while `:368` and `:384-385` mandate `LamuFlix.IntegrationTests` for persistence, messaging, API, and the five named end-to-end scenarios rather than local Aspire launcher wiring.
- **Accuracy with Q7 & Q8 (`specs/DEV-332/CONCLUSIONS.md:127-155`, `specs/DEV-332/brief.md:72-84`)**: Because Q7 establishes real-host acceptance receipts (items 1-7) as the primary blocking proof and Q8 makes additive `tests/LamuFlix.IntegrationTests` coverage optional (`only if needed`, avoiding tests that merely mirror wiring), the amended reason remains truthful whether or not optional `T017` adds an integration test.
- **Gate & Threshold Integrity (`AGENTS.md:125-134`, `specs/PRODUCT.md` §5)**: Updating the exclusion reason text in `harness.yml` within the Q8 file allowlist preserves all mutation thresholds, Core whole-project mutation, and `NOT APPLICABLE / N/A` exit-2 classification without a constitution departure.


### Implications

This later ruling supersedes only the exact Q7 exclusion reason. Meaningful IntegrationTests remain optional; required real-host acceptance, all thresholds and Core mutation remain unchanged. No new test package/project or constitution departure.

---

## PC3 — Direct Node/Vite executable on Windows

### Keel question

[from Keel] DEV-332 PC3 decision before final round 2: Read recon-DEV-332-4 Q5, -5 reserved summary and current brief Q3. Win32 no-shell cannot directly execute batch shim; actual npm installation/DCP behavior unproven. Ticket DEV-332:27-29 requires Vite 5173, not npm. Recommend amend Q3 to core AddExecutable running real node executable with argument array for installed web/node_modules/vite/bin/vite.js and --port 5173 --strictPort, absolute web directory from AppHostDirectory. npm install stays prerequisite; no web edit/new package/shell/Process.Start/LocalPlay. Use discovered node executable, never hardcoded machine path; launch/port/browser proof remains blocking Phase B. Rule cited design permission or retain npm only with a specific no-shell feasible mechanism. No user escalation needed if ticket/constitution unchanged. Report full cited ruling via maestri ask Keel; do not edit files.

### Patron answer (complete)
[from Patron] Approved (PC3): Amend Q3 so core `AddExecutable` launches the `PATH`-resolved `node` executable with working directory resolved to absolute `web` from `builder.AppHostDirectory` and argument array `["node_modules/vite/bin/vite.js", "--port", "5173", "--strictPort"]`, preserving `npm install` as a documented prerequisite, `VITE_API_BASE_URL` injection, fixed localhost 5173 with `WithEndpointProxySupport(false)`, and all Phase B blocking runtime proofs.
- **Windows No-Shell Execution & Constitution Compliance (`recon-DEV-332-4` Q5, `recon-DEV-332-5:22-24`, `.specify/memory/constitution.md:319, 333, 422`)**: Because Win32 `CreateProcessW` / `UseShellExecute=false` cannot directly execute a `.cmd` batch shim without a shell and the Constitution forbids both `cmd.exe`/shell launching (`:422`) and hardcoded machine paths (`:319`), invoking the real `node` executable from `PATH` with an argument array avoids `npm.cmd` shim ambiguity without introducing a shell, hardcoded path, or `Process.Start`/`Features:LocalPlay` change.
- **Ticket & Existing `web/` Contract Alignment (`DEV-332:27-29`, `web/package.json:10`, `specs/PRODUCT.md` §§3, 5)**: Ticket `DEV-332:27-29` mandates the `web` Vite dev server on port 5173 with `VITE_API_BASE_URL`, and `web/package.json:10` defines `"dev": "vite"`; passing `node_modules/vite/bin/vite.js` with `--port 5173 --strictPort` in the deterministic `AppHostDirectory`-resolved `web` directory runs the exact Vite entrypoint after prerequisite `npm install` (`Q6`) with zero `web/` edits and no fifth NuGet package (`Q2`-`Q3`).
- **Retained Phase B Runtime Proof Obligations (`specs/DEV-332/CONCLUSIONS.md` Q3/Q6/Q7/Q10)**: Actual Windows `node` + Vite launch after `npm install`, `WithEndpointProxySupport(false)` behavior on 5173, strict-port failure when 5173 is occupied, and live browser API/CORS/SignalR connectivity remain mandatory blocking Phase B runtime receipts.


### Implications

Supersedes Q3 npm launch prescription only. npm install remains prerequisite. No ticket or constitution change; no new package or file scope. Real Windows node/Vite/proxy/occupied-port/browser receipts remain blocking Phase B acceptance.

---

## PC4 — Resource credential ownership and child Api environment

### Keel question

[from Keel] DEV-332 PC4 single secrets-handling decision before final round: recon-DEV-332-5:14-19 verifies resource-generated password parameters persist to AppHost own user-secrets only with own UserSecretsId, separate from Api; defaults supply username and resource PasswordParameter. Recommend add nonsecret distinct AppHost UserSecretsId in new AppHost csproj, use each AddPostgres/AddRabbitMQ generated credential set once (no duplicate T006 params), feed Api RabbitMq username/password from that same resource, explicitly set Api DOTNET_ENVIRONMENT and ASPNETCORE_ENVIRONMENT to Development so its unchanged own ID loads private prerequisites. No reads/copies of Api secret store, no live values in files/receipts. Basis Q5/Q6, constitution VII, recon-4:40-42 and -5:14-19. Approve exact ownership/environment approach inside Q8 new csproj/Program allowlist; no dependency or constitution change. Report full cited ruling via maestri ask Keel; no edits.

### Patron answer (complete)
[from Patron] Approved (PC4): Declare a distinct non-secret `<UserSecretsId>` in `src/LamuFlix.AppHost/LamuFlix.AppHost.csproj`, use each `AddPostgres` and `AddRabbitMQ` resource's single generated credential set once (no duplicate `T006` password parameters) while injecting `RabbitMq__UserName` (`UserNameReference`) and `RabbitMq__Password` (`PasswordParameter`) from that same `RabbitMQServerResource`, and explicitly inject `DOTNET_ENVIRONMENT=Development` and `ASPNETCORE_ENVIRONMENT=Development` on the `api` resource so `LamuFlix.Api` loads its own unchanged user-secrets store without AppHost ever reading or copying Api secrets or exposing live secret values in files, logs, or receipts.
- **Separate Secret Store Ownership & Single Credential Set (`recon-DEV-332-5:14-19`, `.specify/memory/constitution.md:319-330`, `specs/PRODUCT.md` §5 #5)**: Because Aspire persists generated resource passwords to the AppHost's own user-secrets store only when the AppHost assembly has a `UserSecretsId`, adding a distinct AppHost `UserSecretsId` in `src/LamuFlix.AppHost/LamuFlix.AppHost.csproj` (within Q8's new-file allowlist) and sourcing `RabbitMq__UserName` and `RabbitMq__Password` directly from `RabbitMQServerResource.UserNameReference` and `PasswordParameter` eliminates duplicate `T006` parameters while keeping `LamuFlix.Api.csproj`'s `UserSecretsId` untouched.
- **Explicit Api Development Environment for Private Prerequisites (`recon-DEV-332-4:39-42`, `specs/DEV-332/CONCLUSIONS.md` Q6)**: Because `LamuFlix.Api` has no `launchSettings.json` and Aspire does not automatically propagate `DOTNET_ENVIRONMENT`/`ASPNETCORE_ENVIRONMENT` to child project resources, explicitly setting both to `Development` on `api` in `src/LamuFlix.AppHost/Program.cs` ensures the Api process loads its own user secrets (`Library:RootPath`, `Omdb:ApiKey`) natively without AppHost reading or copying the Api store.
- **Scrub List & Boundary Preservation (Charter §2.3, `specs/DEV-332/CONCLUSIONS.md` Q5/Q8)**: All changes stay strictly inside `src/LamuFlix.AppHost/{LamuFlix.AppHost.csproj,Program.cs}` with no new dependencies, no constitution departure, no `Features:LocalPlay` or `Process.Start` edits, and strict redaction of generated/private secret values across source, logs, commits, and acceptance receipts.


### Implications

PC4 refines Q5/Q6/T006 without expanding file scope: one resource-owned generated credential set per service, own distinct AppHost UserSecretsId in its new csproj, explicit child Api Development environment. No shared secret ID or cross-store read/copy; Api ID unchanged. No runtime success asserted.


---

## Gate 1 — Provisional approval

[from Patron] Ruled (Gate 1): `specs/DEV-332/spec.md` is set to `gate1: provisional` following clean `/speckit-analyze` reported by Conductor and `PLAN FROZEN` adjudication in `specs/DEV-332/PLAN_CHALLENGE.md`.
- **Plan Challenge & ADR Closure (`specs/DEV-332/PLAN_CHALLENGE.md:3, 168-176`, `docs/adr/0031-local-aspire-apphost.md:1-3`)**: All 25 findings across Sentry, Ledger, and Compass are closed at the planning level within the two-round cap, `PLAN FROZEN` is issued, and `docs/adr/0031-local-aspire-apphost.md` is `Accepted` per PC1.
- **Care-List & Constitutional Rulings (`specs/DEV-332/CONCLUSIONS.md` Q1–Q10, PC1–PC4)**: All §2.3 care-list items (dependencies, new `LamuFlix.AppHost` project, Postgres/RabbitMQ/Vite configuration and executable boundaries, secret store separation, and `harness.yml` mutation exclusion reason) are explicitly ruled with zero owner structural checkboxes (`§2.3(a)/(b)`).
- **Phase B Boundary (`specs/DEV-332/PLAN_CHALLENGE.md:176`)**: `gate1: provisional` authorizes Phase A spec PR progression only; owner merge of the spec PR closes Gate 1, and all live host/runtime proofs and Phase B gates remain mandatory blocking obligations.

---

## Phase B — AppHost README host prerequisite ruling (Ollama `all-minilm` at `localhost:11434`)

[from Patron] Approved: Document a locally reachable Ollama service (`http://localhost:11434`) with the `all-minilm` model as a host prerequisite in `src/LamuFlix.AppHost/README.md`, while keeping the AppHost topology strictly limited to the four ticket-named resources (`postgres`, `rabbitmq`, `api`, `web`) with no Ollama resource added and no Api startup validation weakened.
- **Ticket Topology & Already-Landed Startup Contract (`DEV-332:27-31`, `specs/PRODUCT.md` §§2-3, `specs/DEV-332/CONCLUSIONS.md` Q1/Q4/Q6)**: Just as Q4 clarified that deferring new Epic 9 scope excludes adding an Ollama AppHost resource (`Q1`) while honoring already-landed vector artifacts, `LamuFlix.Api`'s existing `OllamaEmbeddingStartupValidator` (`specs/DEV-332/ACCEPTANCE_RECEIPTS.md:172-180`) runs before the Api binds its HTTP listener and must not be bypassed or disabled (`Q6`, `specs/DEV-332/spec.md` FR-010/US6).
- **In-Allowlist Documentation Accuracy (`specs/DEV-332/CONCLUSIONS.md` Q6/Q8, `specs/DEV-332/spec.md` FR-011/SC-001)**: `src/LamuFlix.AppHost/README.md` is already inside the Q8 new-file allowlist, and documenting `all-minilm` on `http://localhost:11434` alongside the .NET SDK, OCI runtime, Node, and Api user secrets makes the single-command startup contract (`SC-001`) truthful on a developer host without changing ticket scope, `Program.cs`, `docker-compose.yml`, or `LamuFlix.Api`.

