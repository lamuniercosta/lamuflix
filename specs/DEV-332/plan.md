# Implementation Plan: Local Aspire AppHost

**Branch**: `feature/332-spec` | **Date**: 2026-10-29 | **Spec**: `specs/DEV-332/spec.md`
**Input**: `brief.md` (Q1–Q10 + PC1–PC4 + recon construction mapping); `CONCLUSIONS.md`; draft ADR `docs/adr/0031-local-aspire-apphost.md` (proposed until plan freeze); notes `recon-DEV-332`, `recon-DEV-332-2`, `recon-DEV-332-3`, `recon-DEV-332-4`, `recon-DEV-332-5`; `specs/PRODUCT.md`; `.specify/memory/constitution.md`; `PLAN_CHALLENGE.md` Round 2

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief/CONCLUSIONS, and the public contract chain is untouched (no DTO/route/OpenAPI/TypeScript change). Precedent: DEV-303 / DEV-331. Status: Round 2 (cap 2) drafting refinements applied; **not PLAN FROZEN**.

## Summary

Deliver a local-only .NET Aspire AppHost that provisions disposable Postgres (`WithImage` `pgvector/pgvector`/`0.8.7-pg17` plus separate `WithImageSHA256` full compose digest; `AddDatabase(name: "DefaultConnection", databaseName: "lamuflix")`) and RabbitMQ, references existing `LamuFlix.Api` for Projects metadata only, declares Api `WithHttpEndpoint(name: "http")` shared by `/health/ready` and `VITE_API_BASE_URL`, explicitly sets Api Development env (PC4), runs Vite via PATH-resolved `node` + Vite bin argument array on fixed localhost 5173 with `WithEndpointProxySupport(false)` (PC3), exposes the SDK dashboard and Development/F5 profile, declares a distinct AppHost `UserSecretsId` for single resource-generated credentials, documents prerequisites in an AppHost README, and adds `harness.yml` mutation exclusion for `LamuFlix.AppHost` with PC2 reason `local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX`. Adapt orchestration to existing Api contracts without editing Api/web/compose or adding a worker/Ollama/JavaScript hosting package. Prove with sanitized real-host acceptance receipts (primary) and optional meaningful additive IntegrationTests; run full Phase B gates with correct N/A/SKIP/Could not run classification. Construction mechanisms are settled; only unexecuted digest/port/Windows/CPM/runtime behavior remains blocking Phase B proof.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`), `TreatWarningsAsErrors=true`, nullable enabled, CPM (`Directory.Packages.props`).
**Primary Dependencies (new, Q2)**: `Aspire.AppHost.Sdk/13.6.4` (project SDK); CPM pins `Aspire.Hosting.AppHost`, `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.RabbitMQ` all **13.6.4**; `AspireUseCliBundle=false`. No separate dashboard package; no `Aspire.Hosting.JavaScript`.
**Storage**: Ephemeral AppHost-owned Postgres via `WithImage("pgvector/pgvector", "0.8.7-pg17")` plus separate `WithImageSHA256` using full compose digest `ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d` from `docker-compose.yml:3` (never pass whole `image@digest` to `WithImage`; digest representation/composed/running identity verified in T016). Physical DB `lamuflix` via `AddDatabase(name: "DefaultConnection", databaseName: "lamuflix")`. No new schema/migrations; no persistent named volume.
**Testing**: Real-host acceptance primary; existing `LamuFlix.IntegrationTests` infrastructure only if meaningful additive proof is needed; no new test project/packages; no mocked Aspire/DB/broker. xUnit v3 + existing helpers.
**Target Platform**: Windows local developer host with OCI runtime and Node `>=24 <25` (documented); AppHost launches containers + host processes.
**Project Type**: Aspire AppHost orchestration project under `src/`; references existing Api project for generated `Projects` metadata.
**Performance Goals**: Local-dev startup; containers must become healthy; Api `/health/ready` and web HTTP `/` probes as ruled.
**Constraints**: Scrub list (no hardcoded/logged/committed secrets); do not bypass validation or weaken health; do not touch `Features:LocalPlay` / `Process.Start` / Api `UserSecretsId`; file allowlist Q8; complexity/analyzer gates on changed C#; mutation exclusion only for AppHost, thresholds unchanged.
**Scale/Scope**: Size L personal-library local-dev entrypoint alongside unchanged compose (DEV-325/326 boundary).

## Constitution Check

Gates checked against `.specify/memory/constitution.md`. **No owner structural checkbox** (Q10; confirmed **PC1** — no constitution amendment and no §2.3(b) checkbox). Patron PC1 rationale: Constitution I:178–197 constrains the four application assemblies; AppHost is an outer local-dev launcher with unidirectional `AppHost → Api` Projects metadata only (no runtime dependency into application layers); Tech Stack:393–416,588 retains compose and permits this ticket-named additive local entrypoint; Governance:597–612 / Documentation:559–561 require ADR Accepted at plan freeze before Gate 1. AppHost mutation exclusion uses PC2 reason text; Core mutation and all thresholds retained (Constitution IX:362–388; Q7; PC2).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Core ports; adapters in Infrastructure | No new ports/adapters; AppHost injects env into existing Api; one-way Api metadata reference only; no ServiceDefaults duplication or Core/Infrastructure/ServiceDefaults references (PC1). PASS |
| VII (Configuration Isolation) | Validated options; no hardcoded secrets; ArgumentList | Preserve ValidateOnStart; inject non-secret timing + single resource-generated credentials; distinct AppHost `UserSecretsId` (PC4); private keys stay in Api user secrets via explicit Development env; PATH-resolved `node` ArgumentList (PC3); forbid `cmd.exe`/shell/`Process.Start` (Q3, Q5–Q6; Ledger M3). PASS |
| VIII (Ubiquitous Language) | No unexplained new domain terms | No glossary addition (Q8). PASS |
| IX (Test Pyramid) | Real infrastructure; mutation on Core | Real-host acceptance receipts primary; meaningful IntegrationTests optional; AppHost excluded with PC2 reason; Core mutation retained (Q7; PC2). PASS |
| Tech Stack | Established choices | Aspire AppHost is ticket-named new project + new NuGet set with explicit Patron care-list rulings (Q2); compose unchanged; local AppHost additive (PC1). Dashboard remains local Aspire target. PASS |
| Docs / ADR | Accepted ADRs before Gate 1 | ADR 0031 remains `proposed` now; Keel marks `Accepted` at plan freeze before Gate 1 (PC1; constitution:559–561,609). |
| Gates | Analyzers, complexity, InspectCode, format, vulnerable packages, mutation | Phase B after Gate 1; CPM clean restore/build; changed-source scope excludes obj/generated; transitive vulnerable-package coverage; correct exit classification (Q7). |

### Complexity Tracking

No constitution violations requiring Complexity Tracking (PC1). Care-list items 1–2 (new packages/project), 5 (secrets handling), and 6 (harness.yml + optional IntegrationTests edits) are explicitly ruled in CONCLUSIONS Q2/Q5/Q7/Q8/PC1–PC4.

## Approach

- **Project/pins (Q2; Ledger M2; PC4)**: New AppHost csproj with SDK `Aspire.AppHost.Sdk/13.6.4`, `net10.0`, `AspireUseCliBundle=false`, distinct non-secret `<UserSecretsId>` for AppHost-owned generated-credential persistence; explicit versionless PackageReferences to AppHost/PostgreSQL/RabbitMQ (CPM `PackageVersion` pins at 13.6.4 — explicit AppHost reference suppresses SDK implicit add; recon-5:3-10); ProjectReference to `LamuFlix.Api` for `Projects.*` metadata only — do not modify Api csproj, do not reference Core/Infrastructure/ServiceDefaults, do not duplicate ServiceDefaults wiring (PC1). Add project to `LamuFlix.sln` under `src`. Preserve approved refs/pins; require clean CPM restore/build plus transitive vulnerable checks in Phase B (do not claim they already ran).
- **Postgres (Q4; Sentry H1/H3/M1)**: `AddPostgres` + `postgres.AddDatabase(name: "DefaultConnection", databaseName: "lamuflix")`; override with `WithImage` for `pgvector/pgvector` tag `0.8.7-pg17` plus separate `WithImageSHA256` using full compose digest `ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d` (never whole `image@digest` to `WithImage`; recon-4:9-13,17-21); single resource-generated credential set; local/dynamic host port mappings only; no named volume; Api `WithReference(database)` injects `ConnectionStrings__DefaultConnection`; `WaitFor` database availability / parent Postgres readiness via built-in checks (Running plus Healthy; not `WaitForStart`); document disposable data. Digest representation/composed/running identity = T016.
- **Api wiring (Q4–Q6; Compass H1; Sentry H2; PC4)**: `AddProject<Projects.LamuFlix_Api>("api")` (exact generated type name as produced by the ProjectReference); `WithHttpEndpoint(name: "http")`; reuse `api.GetEndpoint("http")` for `WithHttpHealthCheck` `/health/ready` and `VITE_API_BASE_URL`; explicitly set `DOTNET_ENVIRONMENT=Development` and `ASPNETCORE_ENVIRONMENT=Development` so Api entry assembly loads its unchanged private store — no Api launchSettings/code edits, automatic-env claim, or copied secrets (recon-4:35-42). `WithReference`/`WaitFor` until postgres/database and rabbitmq are Running plus Healthy; inject `Database__ApplyMigrationsOnStartup=true`, RabbitMQ option keys from broker resource, and local timing defaults. Plain-HTTP/runtime startup errors block Phase B without security bypass.
- **RabbitMQ (Q5; Sentry M3; Compass M1/M3; PC4)**: Ephemeral `rabbitmq:4-alpine` precedent; retain ticket-required `WithReference(rabbitmq)` (DEV-332:28); expose only AMQP (no management UI); inject `RabbitMq__HostName` via Host expression and integer-compatible `RabbitMq__Port` via `PrimaryEndpoint.Property(EndpointProperty.Port)` as `WithEnvironment` `IExpressionValue` (not URL/`TargetPort`; recon-4:58-63); inject `RabbitMq__UserName`/`RabbitMq__Password` from same resource `UserNameReference`/`PasswordParameter` (no duplicate T006 params). Mapped-port numeric formatting/equality + Api validation/readiness = T016 without credential operands.
- **Web (PC3; Sentry M2; Ledger M3)**: Core `AddExecutable` PATH-resolved `node`, absolute `web` from `builder.AppHostDirectory`, argument array `["node_modules/vite/bin/vite.js", "--port", "5173", "--strictPort"]`; `npm install` prerequisite; `VITE_API_BASE_URL` from `api.GetEndpoint("http")`; fixed localhost 5173 with `WithEndpointProxySupport(false)`; `WaitFor(api)` healthy and HTTP `/` probe. Forbid `cmd.exe`/shell/`Process.Start`/`npm` shim/`web/` edits (recon-4:46-54). Separate live T016 proofs: Windows node/Vite, proxy-disabled fixed-port, occupied-port failure, browser. Historical npm-launch round text remains history.
- **Credentials/docs (Q6, Q8; PC4; Ledger L2)**: `Properties/launchSettings.json` Development/F5; AppHost README with prerequisites and compose/AppHost boundary; launch command `dotnet run --project src/LamuFlix.AppHost`. Single resource-generated credential set per service; AppHost store separate from Api store. Existing private Api user secrets supply `Library:RootPath` and `Omdb:ApiKey` under explicit Development env — never weaken validation or copy Api secrets into AppHost. Actual child secret loading = T016.
- **Harness (Q7; PC2)**: Additive `harness.yml` exclusion entry with reason `local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX` only; never edit generated analyzer/mutation config files; thresholds/Core mutation unchanged.
- **Proof (Q7; PC2; Sentry L1/L2/M4; Ledger M1)**: Sanitized real-host receipts under `specs/DEV-332` for brief items 1–7 (digest representation and composed/running image identity; cold-start migration before Api serves; timing inequality retry>lease and positive sweep; missing-prerequisite failure without bypass under explicit Development env; presence/boolean results only — never secrets/private paths/raw connection strings/dashboard env views). Optional IntegrationTests remain optional with feasibility/skip-with-reason; no new package/project; no mocked/wiring-only proof; any host-starting automated proof requires exclusive serial ownership of port 5173 and no concurrent developer AppHost. `AppHostOrchestration*` is a proposed drafting file name, not a verified repo convention.

## Project Structure

```text
specs/DEV-332/
  brief.md, CONCLUSIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md

docs/adr/0031-local-aspire-apphost.md            (repo docs/adr; Keel ownership; proposed now;
                                                 Accepted at plan freeze before Gate 1 per PC1;
                                                 Quill does not edit)

src/LamuFlix.AppHost/                            (new)
  LamuFlix.AppHost.csproj
  Program.cs
  Properties/launchSettings.json
  README.md

LamuFlix.sln                                     (narrow additive: AppHost under src)
Directory.Packages.props                         (narrow additive: three Aspire hosting pins)
harness.yml                                      (narrow additive: AppHost mutation exclusion, PC2 reason)

tests/LamuFlix.IntegrationTests/                 (optional meaningful additive host-proof only;
                                                 proposed drafting name if added:
                                                 AppHostOrchestration*.cs — not a verified repo
                                                 convention; no project/package changes)
```

**Layout decisions (brief Q8; drafting-only where unnamed)**:

- All new production files live under `src/LamuFlix.AppHost/` as ticket-named.
- If IntegrationTests proof is added, proposed additive name `AppHostOrchestration*` beside existing tests is drafting-only, not verified convention; do not rewrite unrelated fixtures or change the IntegrationTests csproj packages/refs unless a later cited ruling says so.
- No `research.md` / `data-model.md` / `contracts/` — brief already freezes decisions; public API unchanged.

## Test Strategy

Real-host acceptance is primary (brief; PC2). Capture sanitized receipts under `specs/DEV-332` for:

1. Fresh disposable pgvector DB (digest representation + composed/running image identity vs compose pin) + existing cold-start migrations before Api serves.
2. RabbitMQ reachable mapped AMQP host port via `PrimaryEndpoint.Property(EndpointProperty.Port)` (integer-compatible) + single resource credential set; broker/consumer readiness; port equality/success without credential operands.
3. `dotnet run --project src/LamuFlix.AppHost` → Running-plus-Healthy postgres/database/rabbitmq before Api, healthy Api before web; dashboard; no worker.
4. Browser `http://localhost:5173` SPA + Api + CORS + SignalR via `api.GetEndpoint("http")` `VITE_API_BASE_URL`.
5. F5 Development profile; separate live Windows PATH-resolved `node`+Vite, absolute `AppHostDirectory` web dir, and proxy-disabled 5173 proofs (no shell/`npm` shim).
6. Occupied 5173 fails hard (separate proof); missing private prerequisites fail clearly under explicit Api Development env without validation/health bypasses; timing inequality + positive sweep recorded.
7. Compose + existing Api/frontend/contracts unchanged; disposable-data and compose-boundary docs present; allowlist compliance.

Automated: optional meaningful additive cases under existing IntegrationTests with feasibility/skip-with-reason; never mock Aspire/DB/broker; never assert wiring for its own sake; host-starting proofs need exclusive serial port-5173 ownership and no concurrent developer AppHost. No Gherkin unless separately opted in. Web gates only if an authorized web diff appears (none planned).

## Gates (Phase B — after Gate 1)

1. Allowlist compliance check at Phase 9 entry; CPM clean restore/build; solution build; explicit changed-source Roslyn / cyclomatic / InspectCode (exclude `obj`/generated Projects metadata); full format gate retained.
2. Affected tests, then full `dotnet test`.
3. Property / vulnerable-packages (include new transitive closure) / format per harness; classify PASS vs failure vs `SKIPPED (scope-empty)` vs SKIP vs mutation N/A vs Could not run.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` from worktree). AppHost exclusion (PC2 reason) → N/A when applicable, never PASS; Core mutation retained.
5. Size L review: Sentry, Ledger, Compass; Q9 closing bar; two-round hard cap.

Phase A (this draft) runs none of the build/test gates. Grill baseline gate counts on unrelated files are measurements, not current-head approval (brief).

## Risks

- **Windows node/Vite / proxy / occupied port**: construction is PC3 + recon-4:46-54; live launch/proxy/strict-port/browser must still be proven in T016 without shell workarounds; failure blocks acceptance, not waived by package metadata (PC3).
- **Unexecuted Phase B proofs only**: digest representation/composed/running image identity; actual child Api secret loading; resolved RabbitMQ mapped-port numeric formatting/equality; clean CPM restore/build + transitive vulnerable checks — construction mechanisms settled; do not claim these passed (PLAN_CHALLENGE Round 2).
- **Dual local entrypoints**: AppHost vs compose can confuse developers — mitigated by AppHost README boundary docs (Q1, Q8).
- **Disposable data**: recreating containers discards local DB — document clearly (Q4).
- **Private secrets still required**: AppHost does not invent `Library:RootPath` / `Omdb:ApiKey`; explicit Development env enables Api store load; missing secrets must fail clearly; actual loading is T016 (Q6; PC4; Sentry H2).
- **Wider fix temptation / plain-HTTP errors**: in-scope defect needing files outside the allowlist, or any security-bypass temptation, remains a blocker pending cited ruling (Q8–Q9; recon-4:35-42).
- **Gate misclassification**: reporting N/A/SKIP/Could not run as PASS is a process defect (Q7).

## Ordering

1. Spec/plan/tasks (Phase A Round 2 cap 2 applied) → Keel independent read-back / plan freeze (Keel marks ADR 0031 Accepted) → Patron Gate 1. **Not frozen by Quill.**
2. Phase B pickup: main/contract/file drift + allowlist check + re-analysis before implementation (tasks T001–T026 depend on pickup).
3. AppHost project (incl. distinct UserSecretsId) + solution + CPM pins + harness exclusion (PC2 reason).
4. Containers (WithImage/SHA256 + AddDatabase), single-source credentials, Api WithHttpEndpoint/Development env/readiness (healthy-before-dependent ordering).
5. Vite PATH-resolved `node` executable (PC3), launch profile, AppHost README.
6. Real-host proofs (+ optional meaningful IntegrationTests) → full gates → bounded review/remediation → stop at awaiting-merge (owner merges).
