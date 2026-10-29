# Feature Specification: Local Aspire AppHost

**Feature Branch**: `feature/332-spec`
**Created**: 2026-10-29
**Status**: gate1: provisional
**Input**: Ticket DEV-332 (parent DEV-100, Size L); `brief.md` (Q1–Q10 + PC1–PC4 + recon construction mapping); `CONCLUSIONS.md`; draft ADR `docs/adr/0031-local-aspire-apphost.md` (proposed until plan freeze); notes `recon-DEV-332`, `recon-DEV-332-2`, `recon-DEV-332-3`, `recon-DEV-332-4`, `recon-DEV-332-5`; `specs/PRODUCT.md`; `PLAN_CHALLENGE.md` Round 2

## Scope

**In scope** (brief Q1–Q8; ticket Scope & Technical Design DEV-332:27–31):

- New `src/LamuFlix.AppHost` project under solution folder `src`, pinned Aspire SDK/hosting packages at **13.6.4**, `AspireUseCliBundle=false`, SDK-managed dashboard (no separate dashboard package).
- Local disposable AppHost-owned Postgres (`WithImage` `pgvector/pgvector` tag `0.8.7-pg17` plus separate `WithImageSHA256` using the full compose digest from `docker-compose.yml:3`; `AddDatabase(name: "DefaultConnection", databaseName: "lamuflix")`) and RabbitMQ (`rabbitmq:4-alpine` precedent); Api project resource with explicit `WithHttpEndpoint(name: "http")`; Vite web via core `AddExecutable` PATH-resolved `node` + Vite bin args on fixed localhost **5173** with `--strictPort` and `WithEndpointProxySupport(false)` (PC3).
- Configuration adapters only: `ConnectionStrings__DefaultConnection` via `WithReference` on the database resource, `Database__ApplyMigrationsOnStartup=true` (AppHost-launched Api only), explicit `RabbitMq__HostName`/`RabbitMq__Port`/`RabbitMq__UserName`/`RabbitMq__Password` from broker resource expressions/parameters, local timing defaults, `DOTNET_ENVIRONMENT`/`ASPNETCORE_ENVIRONMENT=Development` (PC4), `VITE_API_BASE_URL` from `api.GetEndpoint("http")` (same endpoint as `/health/ready`; no Api file changes).
- Distinct non-secret AppHost `UserSecretsId` in the new AppHost csproj (PC4; separate from Api); AppHost `Properties/launchSettings.json` Development/F5 profile; AppHost `README.md` prerequisites; minimal additive `harness.yml` mutation exclusion for `LamuFlix.AppHost`.
- Real-host acceptance receipts (primary) and optional meaningful additive proof under existing `tests/LamuFlix.IntegrationTests` only if needed (existing infrastructure; no new test project/packages).

**Out of scope** (ticket; Q1, Q8; brief exclusions):

- Worker resource; Ollama resource; compose or deployment/production changes.
- Edits to existing Api, ServiceDefaults, Infrastructure, Core, `web/`, root README, `global.json`, generated contracts/OpenAPI/TypeScript.
- New JavaScript hosting package; new schema/migrations; public API shape changes; `Features:LocalPlay` / `Process.Start` / Api `UserSecretsId` changes (AppHost may declare its own distinct `UserSecretsId` per PC4).
- Threshold reductions; treating N/A, SKIP, SKIPPED, or Could not run as PASS.
- Glossary additions; Quill does not author ADRs (draft ADR 0031 is Keel-owned; PC1: Accepted at plan freeze before Gate 1).
- `cmd.exe`/shell or concatenated-command launch workarounds (constitution VII ArgumentList; Q3).

**Precedence**: Ticket Scope & Technical Design is authoritative (`specs/PRODUCT.md` §3). Q1–Q10 plus PC1–PC4 and recon construction mapping in `CONCLUSIONS.md` / `brief.md` settle care-list and challenge decisions for this feature. Owner structural checkboxes: **zero** (Q10; confirmed PC1 — no constitution amendment / no §2.3(b) checkbox).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One AppHost command starts the four local resources healthy (Priority: P1)

As a developer, I run the AppHost from the repository worktree and get disposable Postgres, RabbitMQ, Api, and Vite healthy together with the Aspire dashboard, without a worker resource and without changing compose.

**Why this priority**: Ticket acceptance — `dotnet run` AppHost starts four resources healthy; no worker (DEV-332:29).

**Independent Test**: After documented prerequisites, `dotnet run --project src/LamuFlix.AppHost` yields healthy postgres, rabbitmq, api, and web; dashboard available; no worker application resource (SDK/dashboard bookkeeping is not an extra application resource).

**Acceptance Scenarios**:

1. **Given** prerequisites from the AppHost README are met, **When** `dotnet run --project src/LamuFlix.AppHost` runs, **Then** built-in Postgres/database and RabbitMQ health checks report Healthy (Running plus Healthy via `WaitFor`, not `WaitForStart`) before Api starts, Api `/health/ready` via `WithHttpHealthCheck` on the explicit `http` endpoint becomes healthy before web `WaitFor(api)`, and the SDK dashboard is available (Q1, Q6; brief proof items 3, 5; recon-4:25-31). Cold-start/migration and four-resource healthy timing remain Phase B T016 proofs.
2. **Given** a successful AppHost start, **When** application resources are inspected, **Then** no worker resource exists (Q1; DEV-332:28–29).
3. **Given** AppHost local containers, **When** compared to compose, **Then** compose files remain unchanged and AppHost remains a separate local-dev entrypoint (Q1; DEV-332:27, 31).

---

### User Story 2 - Fresh disposable Postgres migrates and Api becomes ready (Priority: P1)

As a developer, a cold AppHost Postgres using the existing pgvector image applies current migrations before the Api serves, so `/health/ready` can pass against a disposable `lamuflix` database.

**Why this priority**: Without migrated pgvector Postgres, Api readiness and the existing vector schema fail (Q4).

**Independent Test**: Fresh disposable pgvector container starts; existing migrations apply before Api serves; Api `/health/ready` eventually succeeds for ready-tagged checks that depend on Postgres (brief proof item 1).

**Acceptance Scenarios**:

1. **Given** a new ephemeral Postgres overridden with `WithImage` for `pgvector/pgvector` tag `0.8.7-pg17` plus separate `WithImageSHA256` using the full compose digest from `docker-compose.yml:3` (`ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d`), and `postgres.AddDatabase(name: "DefaultConnection", databaseName: "lamuflix")`, **When** AppHost `WaitFor`s database availability (and parent Postgres readiness) then starts Api with `Database__ApplyMigrationsOnStartup=true`, **Then** existing migrations apply on that cold start before Api serves (Q4; recon-4:9-13,17-21). Exact digest argument representation and composed/running image identity are verified in Phase B T016 — not an unknown API and not a fabricated pull success.
2. **Given** the AppHost Api process, **When** connection configuration is inspected, **Then** Api `WithReference` on the database resource injects `ConnectionStrings__DefaultConnection` and no new migration or persistent named volume is introduced (Q4; recon-4:17-21). Physical database name remains `lamuflix`; connection alias and physical name are separate parameters — do not rename either.
3. **Given** AppHost containers are recreated, **When** local data is considered, **Then** disposable-data behavior is documented (local data may be discarded) (Q4).

---

### User Story 3 - RabbitMQ reaches the Api through mapped ports and generated secrets (Priority: P1)

As a developer, the AppHost broker uses dynamic localhost ports and generated secret parameters so the existing `RabbitMqOptions` contract becomes ready without hardcoded credentials.

**Why this priority**: Api does not consume a broker connection-string reference alone (Q5; recon-DEV-332-2).

**Independent Test**: RabbitMQ uses its dynamically assigned host port and matching secret parameters; Api broker/consumer ready checks become ready (brief proof item 2).

**Acceptance Scenarios**:

1. **Given** ephemeral `rabbitmq:4-alpine` with a single resource-generated credential set, **When** Api starts with ticket-required `WithReference(rabbitmq)` (DEV-332:28) retained alongside `WaitFor(rabbitmq)` until Running plus Healthy, and injects `RabbitMq__HostName` from the Host expression, integer-compatible `RabbitMq__Port` via `PrimaryEndpoint.Property(EndpointProperty.Port)` as `WithEnvironment` `IExpressionValue` (not URL/`TargetPort`), plus `RabbitMq__UserName`/`RabbitMq__Password` from the same broker `UserNameReference`/`PasswordParameter`, **Then** broker-dependent ready checks can pass and mapped-port equality/success is proven in T016 without emitting credential values (Q5; recon-4:58-63; PC4; recon-5:14-19).
2. **Given** that single generated credential set per service, **When** logs, commits, and AppHost sources are inspected, **Then** credentials are never hardcoded, logged, or committed; AppHost persists generated parameters in its own distinct `UserSecretsId` store and never reads/copies the Api store (Q5; scrub list; PC4).
3. **Given** the broker resource, **When** ports and UI are considered, **Then** only the AMQP endpoint is host-exposed (dynamic/local-only); no management UI is added (Q5).

---

### User Story 4 - Vite on 5173 reaches the Api (CORS and SignalR) (Priority: P1)

As a developer, the browser at `http://localhost:5173` loads the SPA and talks to the AppHost Api through injected `VITE_API_BASE_URL`, matching existing CORS, without editing `web/` files.

**Why this priority**: Ticket acceptance — web reaches Api with CORS match (DEV-332:29); Q3.

**Independent Test**: Browser at `http://localhost:5173` loads the SPA and reaches Api successfully, including CORS and SignalR, via injected `VITE_API_BASE_URL` (brief proof item 4).

**Acceptance Scenarios**:

1. **Given** AppHost web resource and prerequisite `npm install`, **When** Vite starts, **Then** AppHost launches PATH-resolved `node` via `AddExecutable` with absolute `web` working directory from `builder.AppHostDirectory` and argument array `["node_modules/vite/bin/vite.js", "--port", "5173", "--strictPort"]`, with `WithEndpointProxySupport(false)` on fixed localhost 5173 — no `cmd.exe`/shell/`Process.Start`/`npm` shim/`web/` edits (PC3 supersedes Q3 npm launch; recon-4:46-54; constitution VII ArgumentList). Live Windows node/Vite, proxy-disabled fixed-port, occupied-port failure, and browser proofs remain Phase B T016.
2. **Given** AppHost Api `WithHttpEndpoint(name: "http")` and `api.GetEndpoint("http")` (same endpoint used for `WithHttpHealthCheck` `/health/ready`), **When** Vite starts, **Then** `VITE_API_BASE_URL` is injected from that endpoint before Vite starts; no Api file/launchSettings edits (Q3, Q6; Compass H1; recon-4:35-42).
3. **Given** healthy AppHost stack (Api healthy before web), **When** a real browser session uses the SPA, **Then** typed client and SignalR connect through the injected base URL and existing `http://localhost:5173` CORS policy without `web/` edits (Q3, Q6).

---

### User Story 5 - Documented prerequisites, F5 profile, and clear local timing (Priority: P2)

As a developer, the AppHost README lists prerequisites and private Api secret setup, Development/F5 works, and AppHost injects non-secret local timing defaults that satisfy existing validators.

**Why this priority**: Q6; enables repeatable local use after the core stack works.

**Independent Test**: README documents SDK (`global.json`), OCI runtime, Node `>=24 <25`, npm install, and Api user-secret keys; F5 Development profile starts AppHost with dashboard; timing env values match brief (proof item 5 partially).

**Acceptance Scenarios**:

1. **Given** the AppHost README, **When** a developer prepares a machine, **Then** prerequisites and private Api user-secret keys (`Library:RootPath`, `Omdb:ApiKey`) are documented without committed secret values (Q6).
2. **Given** AppHost launch settings, **When** Development/F5 is used, **Then** AppHost starts with the SDK dashboard (Q6).
3. **Given** AppHost-launched Api, **When** environment is inspected, **Then** `Enrichment__ClaimLease=00:00:30`, `Enrichment__SweepInterval=00:00:10`, and `RabbitMq__RetryDelay=00:01:00` (retry strictly greater than claim lease) are injected locally without changing production defaults (Q6).

---

### User Story 6 - Negative paths, boundary preservation, and gate classification (Priority: P2)

As a developer and reviewer, occupied 5173 and missing private prerequisites fail clearly; compose/Api/frontend/contracts stay untouched; AppHost mutation exclusion reports N/A correctly; in-scope acceptance that cannot run blocks closure.

**Why this priority**: Q7–Q9; prevents silent green and scope creep.

**Independent Test**: Occupied 5173 fails rather than moving the SPA; missing prerequisites fail without validation/health bypasses; compose and existing contracts unchanged; harness exclusion present with PC2 reason; gate exits classified correctly (brief proof items 6–7).

**Acceptance Scenarios**:

1. **Given** port 5173 already occupied, **When** AppHost starts web, **Then** startup fails rather than silently binding another port — separate live proof from Windows startup and proxy-disabled proofs (Q3, Q7).
2. **Given** missing private Api prerequisites, **When** AppHost starts Api with explicit `DOTNET_ENVIRONMENT=Development` and `ASPNETCORE_ENVIRONMENT=Development` so the Api entry assembly loads its unchanged private user-secrets store, **Then** failure is clear without bypassing validation, disabling consumers, weakening health checks, copying Api secrets, or claiming automatic child environment (Q6, Q7; PC4; recon-4:35-42). Actual child secret loading remains a Phase B T016 proof.
3. **Given** the implementation allowlist, **When** the change set is reviewed at pickup and Phase 9 entry, **Then** Api/ServiceDefaults/Infrastructure/Core, `web/`, compose, root README, `global.json`, and generated contracts are unchanged (Q8; Compass L2).
4. **Given** `harness.yml`, **When** mutation exclusions are inspected, **Then** `LamuFlix.AppHost` is listed with reason `local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX` (PC2 supersedes earlier Q7 reason text), thresholds and existing exclusions remain, meaningful automated IntegrationTests stay optional, and N/A / SKIPPED / SKIP / Could not run are never reported as PASS (Q7; PC2).

### Edge Cases

- Live Windows PATH-resolved `node` + Vite bin launch after `npm install`, `WithEndpointProxySupport(false)` on fixed 5173, occupied-5173 failure without fallback, and browser connectivity must be proven as separate Phase B T016 receipts; package/metadata verification is not acceptance (PC3; recon-4:46-54; Sentry M2 / Ledger M3). Historical npm-shim round text remains history only.
- Aspire endpoint proxy must not contend with Vite on 5173; `WithEndpointProxySupport(false)` for the fixed localhost endpoint; occupied-port failure is a separate proof (PC3).
- External OMDb/TMDb/Ollama checks remain outside the ready-tag set; no Ollama resource is added (Q1, Q6).
- If runtime proof exposes a necessary wider edit outside the allowlist, or plain-HTTP/runtime startup errors require a security bypass, retain the acceptance blocker and obtain a cited ruling before editing (Q8; recon-4:35-42).
- All-excluded eligible mutation scope is NOT APPLICABLE / N/A, never PASS; unavailable required runtime/gate is Could not run and blocks closure (Q7, Q9).
- Construction mechanisms from recon-4/-5 + PC3/PC4 are settled in this draft. Retained blocking Phase B T016 proofs only: digest argument representation and composed/running image identity; actual child Api secret loading under explicit Development env; Windows node/Vite launch, proxy-disabled fixed-port, occupied-port, and browser; resolved RabbitMQ mapped-port numeric formatting/equality plus Api validation/readiness; clean CPM restore/build and transitive vulnerable-package checks (unrun). No plan-time claim those passed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST add `src/LamuFlix.AppHost/LamuFlix.AppHost.csproj` to `LamuFlix.sln` under the `src` solution folder, targeting `net10.0`, using `<Project Sdk="Aspire.AppHost.Sdk/13.6.4">` and `AspireUseCliBundle=false` (Q2).
- **FR-002**: System MUST pin `Aspire.Hosting.AppHost`, `Aspire.Hosting.PostgreSQL`, and `Aspire.Hosting.RabbitMQ` at **13.6.4** in `Directory.Packages.props` (CPM); project references consume CPM; no separate dashboard package (Q2).
- **FR-003**: AppHost MUST orchestrate application resources `postgres`, `rabbitmq`, `api`, and `web` plus the SDK dashboard, and MUST NOT add a worker or Ollama resource (Q1).
- **FR-004**: AppHost MUST provision ephemeral Postgres via `AddPostgres`, override with `WithImage` for `pgvector/pgvector` tag `0.8.7-pg17` plus separate `WithImageSHA256` using the full compose digest from `docker-compose.yml:3` (never pass whole `image@digest` to `WithImage`), call `postgres.AddDatabase(name: "DefaultConnection", databaseName: "lamuflix")`, use local/dynamic host port mappings only, and introduce no new migration or persistent named volume (Q4; recon-4:9-13,17-21). Digest representation/composed/running identity is Phase B T016 proof, not an unknown API.
- **FR-005**: AppHost MUST inject `Database__ApplyMigrationsOnStartup=true` only for the AppHost-launched Api and MUST `WaitFor` database availability (and parent Postgres readiness) via built-in health checks (Running plus Healthy; not `WaitForStart`) before Api startup and cold-start migration; MUST NOT invent migration retry or Api changes (Q4, Q6; recon-4:25-31).
- **FR-006**: AppHost MUST provision ephemeral RabbitMQ using the `rabbitmq:4-alpine` image precedent, retain ticket-required `WithReference(rabbitmq)` (DEV-332:28) and `WaitFor(rabbitmq)` until Running plus Healthy via built-in checks (not `WaitForStart`), expose only the AMQP endpoint (no management UI), inject `RabbitMq__HostName` from the Host expression and integer-compatible `RabbitMq__Port` via `PrimaryEndpoint.Property(EndpointProperty.Port)` as `WithEnvironment` `IExpressionValue` (not URL/`TargetPort`), and inject `RabbitMq__UserName`/`RabbitMq__Password` from the same broker `UserNameReference`/`PasswordParameter` (Q5; PC4; recon-4:58-63; recon-5:14-19). Mapped-port numeric formatting/equality and Api validation/readiness are Phase B T016 proofs.
- **FR-007**: AppHost MUST use each `AddPostgres`/`AddRabbitMQ` resource's single generated credential set once (no duplicate password parameters), declare a distinct non-secret AppHost `UserSecretsId` for AppHost-owned persistence of generated parameters, and MUST NEVER hardcode, log, commit, or copy Api store secrets (Q5; PC4; recon-5:14-19). Api `UserSecretsId` stays unchanged.
- **FR-008**: AppHost MUST inject local non-secret timing `Enrichment__ClaimLease=00:00:30`, `Enrichment__SweepInterval=00:00:10`, and `RabbitMq__RetryDelay=00:01:00` for the AppHost-launched Api; retry 60s MUST be strictly greater than lease 30s and sweep MUST be positive 10s (Q6). No further sweep-vs-lease invariant is required.
- **FR-009**: AppHost MUST host web via core `AddExecutable` with PATH-resolved `node`, absolute `web` working directory from `builder.AppHostDirectory`, argument array `["node_modules/vite/bin/vite.js", "--port", "5173", "--strictPort"]`, fixed localhost 5173 with `WithEndpointProxySupport(false)`, and `VITE_API_BASE_URL` from `api.GetEndpoint("http")`; MUST keep `npm install` as a documented prerequisite; MUST NOT use `cmd.exe`/shell/`Process.Start`/concatenated-command/`npm` shim workarounds; MUST NOT add `Aspire.Hosting.JavaScript` or edit `web/`/Api files (PC3; recon-4:46-54).
- **FR-010**: AppHost MUST declare Api `WithHttpEndpoint(name: "http")`, reuse `api.GetEndpoint("http")` for both `WithHttpHealthCheck` `/health/ready` and `VITE_API_BASE_URL`, explicitly set `DOTNET_ENVIRONMENT=Development` and `ASPNETCORE_ENVIRONMENT=Development` on Api (PC4), wait for healthy Postgres/database and RabbitMQ via built-in checks before Api, and wait for healthy Api before web with HTTP `/` probe — without Api launchSettings/code edits, automatic-env claims, or copied secrets (Q6; Compass H1; Sentry H2; recon-4:35-42). Plain-HTTP/runtime startup errors block Phase B without security bypass.
- **FR-011**: AppHost MUST provide Development/F5 `Properties/launchSettings.json` and an AppHost `README.md` documenting .NET SDK per `global.json`, OCI runtime, Node `>=24 <25`, npm dependency installation, private Api user-secret setup for `Library:RootPath` and `Omdb:ApiKey`, launch command `dotnet run --project src/LamuFlix.AppHost`, disposable local data, and the compose boundary (Q6, Q8).
- **FR-012**: Implementation MUST stay within the Q8 file allowlist (checked at Phase B pickup and Phase 9 entry); MUST NOT edit existing Api/ServiceDefaults/Infrastructure/Core, `web/`, compose, root README, `global.json`, or generated contracts; AppHost MUST reference Api for Projects metadata only and MUST NOT duplicate ServiceDefaults wiring or add Core/Infrastructure/ServiceDefaults references (Q8; PC1).
- **FR-013**: `harness.yml` MUST add `gates.mutation.exclusions.LamuFlix.AppHost` with reason `local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX` without changing thresholds, Core mutation, or existing exclusions and without editing generated gate files (Q7; PC2).
- **FR-014**: Verification MUST capture sanitized real-host acceptance receipts under `specs/DEV-332` for brief proof items 1–7 (including digest representation and composed/running image identity, cold-start migration before Api serves, AMQP mapped-port equality/success without credential operands, timing inequality + positive sweep, clear missing-prerequisite failure under explicit Api Development env, and separate Windows node/Vite / proxy-disabled / occupied-5173 / browser proofs). Receipts record key presence and boolean success only — never secret values, private paths, raw connection strings, or dashboard env views. MAY add meaningful additive coverage under existing `tests/LamuFlix.IntegrationTests` without new test projects/packages or mocked Aspire/DB/broker behavior; optional automation that starts the host MUST own port 5173 exclusively, run serially, and not contend with a concurrent developer AppHost (Q7; PC2).
- **FR-015**: Phase B MUST run CPM clean restore/build (explicit versionless `Aspire.Hosting.AppHost` PackageReference suppresses SDK implicit add; CPM `PackageVersion` pin at 13.6.4 resolves the version — recon-5:3-10), solution build, required analyzer/complexity/InspectCode on explicit changed source (exclude `obj`/generated Projects metadata), property-test, vulnerable-package (including new transitive closure), full format, full-test, and mutation gates; N/A, SKIPPED, SKIP, and Could not run MUST NOT be reported as PASS; web gates run only if an authorized change produces a web diff. Do not claim restore/build/vulnerability checks already ran (Q7; Ledger M2/L3).
- **FR-016**: Code-review and ship-review MUST apply the Q9 closing bar: Critical/High/Medium block; every finding disposed; all Sentry/Ledger/Compass axes for size L; two-round hard cap; frozen scope = Q1–Q8 allowlist (Q9).
- **FR-017**: ADR `docs/adr/0031-local-aspire-apphost.md` remains `proposed` during drafting and MUST be marked `Accepted` by Keel at plan freeze before Gate 1; Quill does not edit the ADR (PC1; constitution:559–561,609).

### Key Entities

- **AppHost application**: Local-only distributed application definition owning disposable Postgres/RabbitMQ and orchestrating Api + Vite.
- **Postgres resource**: Ephemeral pgvector-backed container (`WithImage`/`WithImageSHA256`), physical database `lamuflix`, connection alias `DefaultConnection`, with local/dynamic host port mappings.
- **RabbitMQ resource**: Ephemeral broker with single resource-generated credential set and dynamically mapped AMQP host port/host expressions consumed as `RabbitMqOptions`.
- **Api resource**: Existing `LamuFlix.Api` project reference with explicit `http` endpoint and Development env injection; consumers remain in-process; no worker host; Api `UserSecretsId` unchanged.
- **Web executable resource**: PATH-resolved `node` launching installed Vite bin on localhost 5173 with injected Api base URL (PC3).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After documented setup, a developer can start postgres, rabbitmq, api, and web healthy with one AppHost command and see the dashboard, with no worker application resource.
- **SC-002**: A fresh AppHost Postgres run applies existing migrations before Api serves, and Api readiness depending on Postgres/broker/consumers can succeed.
- **SC-003**: A real browser session at `http://localhost:5173` loads the SPA and completes Api and SignalR connectivity through the injected base URL without CORS failures.
- **SC-004**: Occupied 5173 and missing private prerequisites each fail clearly in recorded receipts; compose and existing Api/frontend contracts remain byte-unchanged for this feature’s allowlist.
- **SC-005**: Mutation exclusion for AppHost is present with PC2 reason `local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX`; AppHost-only eligible mutation scope reports N/A (never PASS); required acceptance that cannot run blocks closure as Could not run.
- **SC-006**: Review closure records dispositions for every finding across Sentry, Ledger, and Compass within two rounds against the frozen Q1–Q8 scope.

## Assumptions

- No taste assumptions were introduced (brief). Private Api user secrets already exist for local Api work and remain the source of `Library:RootPath` and `Omdb:ApiKey`. PC4: AppHost explicitly sets child Api `DOTNET_ENVIRONMENT`/`ASPNETCORE_ENVIRONMENT` to Development so the Api entry assembly loads its unchanged private store; AppHost never reads/copies that store. Actual loading is a Phase B T016 proof (Sentry H2; recon-4:35-42).
- Compose remains a separate supported local entrypoint; AppHost does not replace it (ticket; Q1).
- PC1: local-only AppHost with one-way Api metadata reference alongside unchanged compose conforms without constitution amendment or owner structural checkbox (Constitution I:178–197; Tech Stack:393–416,588; Governance:597–612). ADR 0031 stays proposed now; Keel marks it Accepted at plan freeze before Gate 1 (constitution:559–561,609). Plan freeze and Gate 1 remain separate later claims.
- PC3/PC4 and recon-4/-5 construction mechanisms supersede earlier conflicting npm-launch / recon-pending API text in this draft. Retained Phase B T016 proofs (digest/composed image, child secret loading, Windows node/Vite/proxy/occupied-port/browser, mapped broker port formatting/equality, clean CPM restore/build) are unexecuted obligations, not inventable defaults (Q10; PLAN_CHALLENGE Round 2).
