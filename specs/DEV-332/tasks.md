# Tasks: Local Aspire AppHost

**Input**: `specs/DEV-332/` spec.md, plan.md, brief.md (Q1–Q10 + PC1–PC4 + recon construction mapping), CONCLUSIONS.md, draft ADR 0031 (proposed until plan freeze), PLAN_CHALLENGE Round 2, recon-DEV-332 / -2 / -3 / -4 / -5
**Tests**: Real-host acceptance receipts required (FR-014; Q7; PC2). Automated IntegrationTests optional if meaningful and additive. TDD optional. No new test project/packages; no mocked Aspire/DB/broker.
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`.
**Gate 1**: Patron Gate 1 on frozen spec/plan/tasks precedes Phase B; ADR 0031 must be Accepted at plan freeze before Gate 1 (PC1). Phase A drafting does not implement or freeze. Owner structural checkboxes: none (Q10; PC1 — no constitution amendment / no §2.3(b) checkbox).

**Conventions**: No explanatory comments in production code; no hardcoded/logged/committed secrets; use `WithImage`/`WithImageSHA256` (never whole `image@digest` to `WithImage`); do not edit Api/ServiceDefaults/Infrastructure/Core/`web/`/compose/root README/`global.json`/generated contracts; AppHost references Api for Projects metadata only (no ServiceDefaults duplication or Core/Infrastructure/ServiceDefaults refs — PC1); forbid `cmd.exe`/shell/`Process.Start`/`npm` shim launch workarounds (PC3); preserve unrelated content in narrowly edited shared files (`LamuFlix.sln`, `Directory.Packages.props`, `harness.yml`). Construction mechanisms settled; only unexecuted Phase B proofs remain.

---

## Phase B pickup prerequisite (before T001)

**Purpose**: Confirm the frozen drafting set still matches main/contracts/files and the Q8 allowlist before any implementation task.

- [ ] Phase B pickup: check main/contract/file drift, verify Q8 allowlist compliance, and re-run analysis; amend tasks only for justified drift through the assigned workflow (brief.md:91; plan.md Ordering step 2; Compass L2). **All implementation tasks T001–T026 depend on this unchecked prerequisite.** Stable IDs T001–T026 retain numbering; execution follows dependencies (T026 before T018–T025).

---

## Phase 1: Setup — project, pins, harness exclusion

**Purpose**: Create AppHost project surface and approved dependency/exclusion entries before resource wiring.

- [ ] T001 Create `src/LamuFlix.AppHost/LamuFlix.AppHost.csproj` with SDK `Aspire.AppHost.Sdk/13.6.4`, `TargetFramework` `net10.0`, `AspireUseCliBundle=false`, distinct non-secret `<UserSecretsId>` for AppHost-owned generated-credential persistence (PC4; separate from Api `UserSecretsId`), explicit versionless PackageReferences to `Aspire.Hosting.AppHost`, `Aspire.Hosting.PostgreSQL`, and `Aspire.Hosting.RabbitMQ` (versions via CPM only — explicit AppHost reference suppresses SDK implicit add; recon-5:3-10), and a ProjectReference to `src/LamuFlix.Api/LamuFlix.Api.csproj` for generated `Projects` metadata only — do not modify the Api project; do not reference Core/Infrastructure/ServiceDefaults or duplicate ServiceDefaults wiring (FR-001, FR-002, FR-007, FR-012; Q2; PC1; PC4; Ledger L2)
- [ ] T002 Add Aspire hosting package versions **13.6.4** for `Aspire.Hosting.AppHost`, `Aspire.Hosting.PostgreSQL`, and `Aspire.Hosting.RabbitMQ` to `Directory.Packages.props`; require clean CPM restore/build with coordinated approved pins — explicit versionless AppHost PackageReference + CPM `PackageVersion` pin is the supported path (recon-5:3-10); do not remove approved references; do not claim restore/build already ran (FR-002, FR-015; Q2; Ledger M2)
- [ ] T003 Add `src/LamuFlix.AppHost/LamuFlix.AppHost.csproj` to `LamuFlix.sln` under the existing `src` solution folder (FR-001; Q2, Q8)
- [ ] T004 [P] Add `gates.mutation.exclusions.LamuFlix.AppHost` with reason `local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX` to `harness.yml`; preserve threshold, `wholeProject`, `maxKillShare`, `maxKillFileSpan`, Core mutation, and existing Api/Infrastructure exclusions; do not edit generated gate files (FR-013; Q7; PC2)

---

## Phase 2: Foundational — Program skeleton and shared resource parameters

**Purpose**: AppHost entrypoint and credential parameters required by all stories.

**⚠️ CRITICAL**: User-story resource wiring starts after this phase.

- [ ] T005 Create `src/LamuFlix.AppHost/Program.cs` with `DistributedApplication.CreateBuilder(args)` / `builder.Build().Run()` skeleton ready for resource registration (FR-003; Q1)
- [ ] T006 In `src/LamuFlix.AppHost/Program.cs`, design single-source credential wiring: each `AddPostgres`/`AddRabbitMQ` uses its own single resource-generated credential set once (no duplicate password parameters); Api RabbitMQ username/password come from that same broker `UserNameReference`/`PasswordParameter`; never hardcode, log, commit, or read/copy Api store values — AppHost persistence uses the distinct AppHost `UserSecretsId` from T001 (FR-007; Q5; PC4; Ledger L2; recon-5:14-19)

**Checkpoint**: Project builds as an AppHost shell with pins and harness exclusion present.

---

## Phase 3: User Story 2 — Disposable Postgres + migration ordering (Priority: P1)

**Goal**: Ephemeral pgvector Postgres `lamuflix` via `DefaultConnection` with wait-before-Api and AppHost-only startup migrations.
**Independent Test**: Fresh disposable DB starts; existing migrations apply before Api serves (brief proof item 1).

- [ ] T007 [US2] In `src/LamuFlix.AppHost/Program.cs`, add Postgres via `AddPostgres`, override with `WithImage` for `pgvector/pgvector` tag `0.8.7-pg17` plus separate `WithImageSHA256` using full compose digest `ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d` from `docker-compose.yml:3` (never pass whole `image@digest` to `WithImage`; recon-4:9-13), call `postgres.AddDatabase(name: "DefaultConnection", databaseName: "lamuflix")` (recon-4:17-21; do not rename physical DB or connection alias), ephemeral lifecycle (no persistent named volume), single resource-generated credential set, and local/dynamic host port mappings only — digest representation/composed/running identity verified in T016 (FR-004; Q4; Sentry H1/H3)
- [ ] T008 [US2] In `src/LamuFlix.AppHost/Program.cs`, register Api via `AddProject` with `WithHttpEndpoint(name: "http")` (reuse `api.GetEndpoint("http")` later for health and `VITE_API_BASE_URL`; no Api launchSettings/code edits; recon-4:35-42), `WaitFor` database availability and parent Postgres readiness via built-in checks (Running plus Healthy; not `WaitForStart`; recon-4:25-31), `WithReference` the database resource to inject `ConnectionStrings__DefaultConnection`, explicitly set `DOTNET_ENVIRONMENT=Development` and `ASPNETCORE_ENVIRONMENT=Development` (PC4), and set `Database__ApplyMigrationsOnStartup=true` only for this AppHost-launched Api with cold-start migration before Api serves — no migration retry/Api change/copied secrets; actual child secret loading and cold-start timing proven in T016 (FR-005, FR-010; Q4, Q6; Compass H1; Sentry H2/M1; PC4) — depends on T007

---

## Phase 4: User Story 3 — RabbitMQ mapped endpoint + RabbitMqOptions injection (Priority: P1)

**Goal**: Ephemeral broker with dynamic localhost port and explicit option-key injection.
**Independent Test**: Dynamic host port + matching secrets; Api broker/consumer readiness (brief proof item 2).

- [ ] T009 [US3] In `src/LamuFlix.AppHost/Program.cs`, add ephemeral RabbitMQ resource using `rabbitmq:4-alpine` precedent and its single resource-generated credential set (no duplicate params; T006); expose only the single host-mapped AMQP endpoint (dynamic localhost; no management UI) (FR-006, FR-007; Q5; Compass M3; PC4)
- [ ] T010 [US3] In `src/LamuFlix.AppHost/Program.cs`, on the Api resource retain ticket-required `WithReference(rabbitmq)` (DEV-332:28; Q5) alongside `WaitFor(rabbitmq)` until Running plus Healthy via built-in checks (not `WaitForStart`), inject `RabbitMq__HostName` from Host expression, integer-compatible `RabbitMq__Port` via `PrimaryEndpoint.Property(EndpointProperty.Port)` as `WithEnvironment` `IExpressionValue` (not URL/`TargetPort`; recon-4:58-63), and `RabbitMq__UserName`/`RabbitMq__Password` from the same broker `UserNameReference`/`PasswordParameter`; mapped-port numeric formatting/equality and Api validation/readiness proven in T016 without emitting credential values/operands (FR-006; Q5; Sentry M3; Compass M1; PC4) — depends on T008, T009

---

## Phase 5: User Story 1 & 5 — Api readiness, local timing, launch profile (Priority: P1/P2)

**Goal**: Api `/health/ready`, AppHost-only timing defaults, Development/F5 profile; four-resource topology without worker/Ollama.
**Independent Test**: Api ready probe configured; F5 profile present; no worker resource (brief proof items 3, 5).

- [ ] T011 [US1] In `src/LamuFlix.AppHost/Program.cs`, configure Api `WithHttpHealthCheck` `/health/ready` on `api.GetEndpoint("http")` (same endpoint as `VITE_API_BASE_URL`; no Api file changes; recon-4:35-42), require built-in Postgres/database and RabbitMQ health (Running plus Healthy via `WaitFor`, not `WaitForStart`) before Api and healthy Api before web `WaitFor(api)`, and ensure application resources are exactly postgres, rabbitmq, api, and (later) web — no worker, no Ollama; four-resource healthy timing proven in T016 (FR-003, FR-010; Q1, Q6; Compass H1/M2; Sentry M1/M4; recon-4:25-31)
- [ ] T012 [US5] In `src/LamuFlix.AppHost/Program.cs`, inject `Enrichment__ClaimLease=00:00:30`, `Enrichment__SweepInterval=00:00:10`, and `RabbitMq__RetryDelay=00:01:00` into the AppHost-launched Api (retry 60s > lease 30s; positive sweep 10s; no further sweep-vs-lease invariant) (FR-008; Q6; Sentry L1)
- [ ] T013 [P] [US5] Create `src/LamuFlix.AppHost/Properties/launchSettings.json` Development/F5 profile for the AppHost with SDK dashboard startup (FR-011; Q6)

---

## Phase 6: User Story 4 — Vite executable on 5173 (Priority: P1)

**Goal**: Core `AddExecutable` Vite with `VITE_API_BASE_URL`, strict 5173, proxy disabled, wait for Api.
**Independent Test**: Browser SPA reaches Api/CORS/SignalR; occupied 5173 fails (brief proof items 4–6).

- [ ] T014 [US4] In `src/LamuFlix.AppHost/Program.cs`, add web via core `AddExecutable` PATH-resolved `node`, absolute `web` working directory from `builder.AppHostDirectory`, argument array `["node_modules/vite/bin/vite.js", "--port", "5173", "--strictPort"]`, fixed localhost 5173 with `WithEndpointProxySupport(false)`, `VITE_API_BASE_URL` from `api.GetEndpoint("http")`, `WaitFor` healthy Api, and HTTP `/` probe — `npm install` remains prerequisite; forbid `cmd.exe`/shell/`Process.Start`/`npm` shim/`web/` edits (PC3; recon-4:46-54); no JavaScript hosting package; separate live T016 proofs for Windows node/Vite, proxy-disabled fixed-port, occupied-port failure, and browser (FR-009, FR-010; Q3, Q6; Sentry M2; Ledger M3)

---

## Phase 7: User Story 5 & 6 — README boundary docs (Priority: P2)

**Goal**: Prerequisites, private secrets guidance, disposable-data and compose-boundary documentation.
**Independent Test**: README contains required prerequisite and boundary content without secret values (brief proof item 7).

- [ ] T015 [US5] Create `src/LamuFlix.AppHost/README.md` documenting .NET SDK per `global.json`, OCI runtime, Node `>=24 <25`, npm install prerequisite, private Api user secrets for `Library:RootPath` and `Omdb:ApiKey` (never committed values; AppHost sets Api Development env so Api entry assembly loads its unchanged store — do not copy secret values into AppHost; PC4; actual loading proven in T016), launch command `dotnet run --project src/LamuFlix.AppHost`, disposable local data, and AppHost-vs-compose boundary (FR-011; Q6, Q8; Sentry H2)

---

## Phase 8: User Story 1/4/6 — Real-host proof and optional IntegrationTests (Priority: P1/P2)

**Goal**: Capture brief proof receipts 1–7; add meaningful IntegrationTests only if needed.
**Independent Test**: Receipts satisfy FR-014 / SC-001–SC-005; optional tests do not mock Aspire/DB/broker.

- [ ] T016 [US1] Capture sanitized real-host acceptance receipts under `specs/DEV-332` only for: digest argument representation and composed/running image identity vs compose pin; fresh cold-start migrations before Api serves with local/dynamic Postgres host mapping; RabbitMQ mapped AMQP host-port numeric formatting/equality via `PrimaryEndpoint.Property(EndpointProperty.Port)` and secrets readiness without credential operands; Running-plus-Healthy postgres/database/rabbitmq before Api and healthy Api before web + dashboard + no worker; browser API/CORS/SignalR via `api.GetEndpoint("http")` `VITE_API_BASE_URL`; separate live proofs for Windows PATH-resolved `node`+Vite, proxy-disabled fixed 5173, and occupied-5173 failure without fallback; timing inequality (retry 60s > lease 30s) + positive sweep 10s; clear missing-prerequisite failure under explicit Api Development env without validation/health bypass; unchanged compose/Api/frontend contracts. Record key presence and boolean success only — never secret values, private paths, raw connection strings, or dashboard env views (FR-014; Q7; brief proof items 1–7; Sentry H1/H2/L1/L2/M1/M2/M3)
- [ ] T017 [P] [US6] If meaningful automated coverage is still needed after T016, add additive host-proof tests under existing `tests/LamuFlix.IntegrationTests/` using proposed drafting name `AppHostOrchestration*.cs` (not a verified repo convention) and existing infrastructure only — no new test project/packages, no mocked/wiring-only proof, no shell workaround; any host-starting automated proof MUST own port 5173 exclusively, run serially, and not contend with a concurrent developer AppHost; otherwise record feasibility/skip-with-reason on the receipt. Meaningful IntegrationTests remain optional; real-host receipts stay primary/blocking (FR-014; Q7, Q8; PC2; Sentry M4; Ledger M1)

---

## Phase 9: Polish & Phase B verification

**Purpose**: Full gate suite and size-L review after implementation and real-host proof. Re-check Q8 allowlist at Phase 9 entry before gates (Compass L2).

- [ ] T026 CPM clean restore/build then build `LamuFlix.sln` and record exit receipt (FR-015; Q7; brief.md:82; plan.md Gates item 1; Ledger M2) — required before T018–T025; Phase 2 shell-build checkpoint is not this verification
- [ ] T018 Run Roslyn analyzers on explicit changed source `.cs` set via `./scripts/run-roslyn-analyzers.ps1` — exclude `obj`/generated Projects metadata (FR-015; Q7; Ledger L3) — depends on T026
- [ ] T019 Run `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml on changed source files excluding `obj`/generated metadata (FR-015; Q7; Ledger L3) — depends on T026
- [ ] T020 Run `./scripts/run-jetbrains-inspectcode.ps1` on changed source files excluding `obj`/generated metadata (FR-015; Q7; Ledger L3) — depends on T026
- [ ] T021 `dotnet format --verify-no-changes` clean for full format gate (FR-015; Ledger L3) — depends on T026
- [ ] T022 Run affected tests then full `dotnet test` (FR-015; SC-001–SC-004) — depends on T026
- [ ] T023 Run property/security/vulnerable-package gates per harness including new transitive closure; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS; run web gates only if an authorized web diff exists (FR-015; Q7; Ledger M2) — depends on T026
- [ ] T024 Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree; AppHost exclusion (PC2 reason) → N/A when applicable, never PASS; Core mutation retained (FR-013, FR-015; Q7; PC2; SC-005) — depends on T026
- [ ] T025 Size L code review with current-head Sentry, Ledger, and Compass reports; apply Q9 closing bar and two-round hard cap; findings/summary on the PR (FR-016; Q9; SC-006)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase B pickup**: Unchecked prerequisite before any T001–T026 work (stable IDs; allowlist check included); blocks all implementation phases
- **Phase 1 (Setup)**: Depends on Phase B pickup — T001→T002→T003 sequenced for project usability; T004 `[P]` with T001–T003
- **Phase 2 (Foundational)**: Depends on Phase 1 — blocks resource stories
- **Phases 3–7 (Stories)**: Depend on Phase 2; US2 (Postgres/Api base) before US3 RabbitMQ Api injection; US4 web after Api resource exists; README/launch can proceed once project exists (T013 `[P]` after T001)
- **Phase 8 (Proof)**: After Program wiring + README + launch settings
- **Phase 9 (Verification)**: After implementation and T016 (T017 if used); allowlist re-check at entry; T026 CPM restore/build + solution build + exit receipt before T018–T025

### User Story Dependencies

- **US2 (P1)**: After Phase 2 — Postgres + Api migration injection + AppHost-owned HTTP endpoint
- **US3 (P1)**: After US2 Api registration — broker + option injection with retained WithReference
- **US1 (P1)**: Topology/health complete when US2–US4 wired; proof in T016
- **US4 (P1)**: After Api resource exists (T008+)
- **US5 (P2)**: Timing + launch + README; T013 parallel after project exists
- **US6 (P2)**: Negative paths and boundary proven in T016/T017; harness exclusion T004 (PC2 reason)

### Parallel Opportunities

```text
# After T001 exists:
T004 harness.yml AppHost mutation exclusion (PC2 reason)   # [P]
T013 Properties/launchSettings.json                        # [P] once project folder exists

# After T016 receipts:
T017 optional IntegrationTests (proposed AppHostOrchestration* name)   # [P] if still needed
```

### MVP

After Phase B pickup: T001–T015 + T016 (US1–US5 core path: pins, postgres, rabbitmq, api readiness/timing, Vite 5173, README/F5, real-host receipts) proves ticket acceptance before optional T017 and full gate/review tasks T026 then T018–T025.

---

## Notes

- Task IDs T001–T026 are stable for Keel fix rounds; do not renumber. T026 executes before T018–T025 by dependency; blanket pickup ranges include T026 (Compass L3 / Ledger L1).
- Exact generated `Projects.*` Api type name follows the ProjectReference metadata Aspire emits — verify at implementation, do not invent alternate project APIs.
- PC3 supersedes active npm-launch prescriptions: PATH-resolved `node` + Vite bin args; Windows node/Vite, proxy-disabled 5173, occupied-5173, and browser remain separate live T016 proofs; forbid shell/`npm` shim/`Process.Start`. Historical round receipts remain history.
- PC1 recorded: no constitution amendment/owner structural checkbox; ADR Accepted at plan freeze before Gate 1 (Keel). PC2 recorded: exclusion reason exactly `local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX`. PC3/PC4 recorded: direct node/Vite; distinct AppHost UserSecretsId + single-source credentials + explicit Api Development env. Construction mechanisms settled; retained Phase B proofs only as listed in T016 / plan Risks.
- Stop at awaiting-merge after review; owner alone merges. This Round 2 (cap 2) drafting pass does not freeze the plan.
