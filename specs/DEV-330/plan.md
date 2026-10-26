# Implementation Plan: DEV-330 Enrichment status SignalR push

**Branch**: `feature/330-spec` | **Date**: 2026-10-26 | **Spec**: [spec.md](spec.md)

**Input**: `spec.md`, closed grill `brief.md` (Q1–Q9 + PC1/PC2), `CONCLUSIONS.md` (incl. PC1/PC2), Proposed ADR-0029 (Keel PC1 update), accepted ADR-0022, proposed `constitution-amendment-v1.5.0.md`, notes `recon-DEV-330` / `recon-DEV-330-2`, `specs/PRODUCT.md`, `.specify/memory/constitution.md`.

**Note on Spec Kit phases.** Phase 0 (`research.md`) and Phase 1 (`data-model.md`, `contracts/enrichment-hub.md`, `quickstart.md`) are written. No new SQL entity; `data-model.md` records the logical transition and wire DTO only. The hub contract lives under `contracts/` because the public SignalR event and OpenAPI components schema are ticket deliverables.

## Summary

Add Core port `IEnrichmentStatusNotifier`, detect committed enrichment-status transitions in `EfMovieRepository`, and broadcast `EnrichmentStatusChanged` from Api `EnrichmentHub` via `SignalREnrichmentStatusNotifier`. Expose `EnrichmentStatusChangedDto` through OpenAPI components (no REST route), regenerate the contract chain, keep existing DevSpa CORS, add test-only SignalR.Client 10.0.14, and apply owner-gated Constitution v1.5.0 + whitelist + Accepted ADR-0029 in Phase B after Gate 1.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`net10.0`), nullable enable, `TreatWarningsAsErrors`, CPM (`Directory.Packages.props`).

**Primary Dependencies**: ASP.NET Core SignalR **shared framework** (no server NuGet — ADR-0022); test-only `Microsoft.AspNetCore.SignalR.Client` **10.0.14** (Q6; recheck Microsoft.* train at Build pickup); existing OpenAPI / Verify / web generate-api toolchain. No npm package in this ticket.

**Storage**: Existing Postgres movie rows via `EfMovieRepository`; **no schema/migration**.

**Testing**: xUnit v3 + Shouldly + NSubstitute (adapter unit); `WebApplicationFactory` + official HubConnection; Testcontainers for persistence transition proofs; existing OpenAPI/CORS tests extended. No new assertion/mocking library; no driver mocks for query/transaction semantics.

**Target Platform**: Local Api host (single instance); Vite origin `http://localhost:5173`.

**Project Type**: Existing `LamuFlix.Core`, `LamuFlix.Infrastructure`, `LamuFlix.Api`, unit/integration/architecture test projects, generated `web/src/api/*`. No new project/layer.

**Performance Goals**: Sequential awaited ephemeral one-shot post-commit broadcasts only; no application-level timeout or fan-out bound; unbounded post-commit latency accepted for single-instance scope (Q3; PC1).

**Constraints**: Owner Gate 1 open until constitution/ADR approval; frozen file set (brief Planned files; Api root paths retained — no `Hubs/` folder taste change; no speculative transition-tracker helper unless measured T016/T031 complexity forces a bounded file-list ruling — Ledger M2 Follow-up noted, no ticket); no LocalPlay/secrets/`Process.Start`; no explanatory comments beyond AAA/suppressions; no threshold lowering; worktree mutation without `--since`.

**Scale/Scope**: Size L. Frozen delivery scope per brief Q9.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Cite principle numbers.*

- **I. Ports and Adapters** — **CONDITIONAL PASS (owner Gate 1)**. Ticket requires new Core port `IEnrichmentStatusNotifier`. Closed list + ADR requirement (`constitution.md:148-152`) force Proposed ADR-0029 and proposed MINOR v1.5.0 (`constitution-amendment-v1.5.0.md`). Whitelist update in `ArchitectureTests.cs` `PortWhitelistPattern`. Noncompliant implementation unauthorized until user answers the checkbox (`spec.md` Owner question).
- **II. Explicit Handlers** — **PASS**. No MediatR; no handler-scattered notifies; unused handlers stay unregistered (Q5).
- **V. Validated Inputs / Errors** — **PASS**. No new REST endpoint; existing exception mapping untouched.
- **VI. Observability** — **PASS**. Adapter logs attempt start and failure with movie id and committed status; repository per-movie isolation also logs; not durable crash-window detection; no duplicate Serilog/OTel host wiring.
- **VII. Configuration / Time** — **PASS**. No new secrets; `TimeProvider` untouched; CORS origin remains existing const.
- **VIII. Ubiquitous Language** — **PASS**. No new glossary terms (Q9); no `CONTEXT.md` edit.
- **IX. Test Pyramid** — **PASS**. Real HubConnection + Testcontainers for persistence; no EF driver mocks for transition semantics.
- **API and Contract Rules** — **CONDITIONAL**. Push channel accepted by ADR-0022; constitution polling wording updated only via v1.5.0 amendment. Contract chain via components schema + regeneration (Q4).
- **Technology Stack / Packages** — **PASS**. Server SignalR from shared framework; one test-only CPM pin (Q6).
- **Static-Analysis / PR gates** — **Planned Phase B verification, pending** — applicable gates run honestly in Phase B; not claimed PASS in Phase A.
- **§2.3 care list** — Dependencies (Q6 approved); no new layer; no schema; public hub contract frozen in brief/spec (Q4); no LocalPlay/secrets; file adaptations named in brief.

**Re-check after design**: Same conditional Gate 1; research/contracts introduce no additional constitution departure.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-330/
├── brief.md
├── CONCLUSIONS.md
├── constitution-amendment-v1.5.0.md
├── spec.md
├── plan.md                 # this file
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/enrichment-hub.md
├── tasks.md
├── checklists/requirements.md
├── checklists/requirements-quality.md
└── DRAFTING_RECEIPT.md
docs/adr/
├── 0022-signalr-enrichment-status-push.md   # Accepted (Keel)
└── 0029-enrichment-status-post-commit-notifier.md  # Proposed (Keel)
```

### Source Code (repository root) — Phase B design paths

```text
src/LamuFlix.Core/Ports/IEnrichmentStatusNotifier.cs          # new
src/LamuFlix.Api/EnrichmentHub.cs                             # new
src/LamuFlix.Api/EnrichmentStatusChangedDto.cs                # new
src/LamuFlix.Api/SignalREnrichmentStatusNotifier.cs           # new
src/LamuFlix.Api/EnrichmentHubSchemaTransformer.cs            # new
src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs  # edit: detect + post-commit notify
src/LamuFlix.Api/Program.cs                                   # edit: AddSignalR, MapHub, DI, JSON protocol
Directory.Packages.props                                      # edit: SignalR.Client 10.0.14
tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj  # PackageReference
tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs         # PortWhitelistPattern token
.specify/memory/constitution.md                               # v1.5.0 after Gate 1
tests/LamuFlix.UnitTests/Enrichment/SignalREnrichmentStatusNotifierTests.cs          # new
tests/LamuFlix.IntegrationTests/EnrichmentHubTests.cs                                 # new
tests/LamuFlix.IntegrationTests/EnrichmentStatusNotificationPersistenceTests.cs       # new
tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs       # edit: schema expectations
tests/LamuFlix.IntegrationTests/ApiCorsTests.cs               # edit: hub negotiate preflight
web/src/api/openapi.json + types.ts + Verify snapshot         # regenerated, never hand-edited
```

Plus strictly necessary repository composition/constructor fixture adaptations only.

**Structure Decision**: Existing solution layout; no new project/top-level folder/layer (Q5).

## Design

### 1. Core port

`IEnrichmentStatusNotifier.NotifyAsync(MovieId, EnrichmentStatus, CancellationToken)` in Core/Ports. Core references neither SignalR nor Api DTOs (Q5; ADR-0029).

### 2. Repository post-commit boundary

At `EfMovieRepository`, capture persisted-before and final committed-after enrichment status per movie in the save; retain assigned insert ids; on failure clear candidates; after **enclosing** transaction commit, sequentially await `NotifyAsync` once per transitioned movie with `CancellationToken.None`, catching/logging each movie's notifier exception and continuing remaining movies (Q2, Q5; PC1). Require `IEnrichmentStatusNotifier` on the constructor; fixtures supply explicit doubles; no optional/no-op production fallback. Do not treat an intermediate `SaveChangesAsync` inside an uncommitted transaction as the notification boundary. Do not add a speculative internal transition-tracker collaborator up front (Ledger M2 Follow-up); T016/T031 enforce measured complexity.

### 3. Api hub + adapter

Empty (or minimal) `EnrichmentHub` mapped at `/hubs/enrichment` under the flat Api root (no `Hubs/` folder). `SignalREnrichmentStatusNotifier` builds `EnrichmentStatusChangedDto`, sends `EnrichmentStatusChanged` to `Clients.All`, configures SignalR JSON converters independently of HTTP, logs attempt start/failure with movie id and committed status, isolates send exceptions, and does not skip send solely for post-commit caller cancellation (Q3–Q5; PC1). No application-level send timeout or fan-out bound. Register real notifier in Api DI; named live-host resolution proof required.

### 4. OpenAPI components schema

New `EnrichmentHubSchemaTransformer` (`IOpenApiDocumentTransformer`) inserts the unreferenced hub DTO into `components.schemas`. This is distinct from existing `EnrichmentStatusSchemaTransformer` (`IOpenApiSchemaTransformer`), which only runs for types already discovered from HTTP endpoints (recon-DEV-330-2:30-33; PC2 / Ledger L1). Preserve required fields, four-name string enum, and exact `ExpectedOperations`; regenerate per `quickstart.md` (Q4).

### 5. CORS

Reuse `DevSpa`; prove hub negotiate OPTIONS retaining origin/method/header/no-credentials, exposed `Location` on the appropriate actual CORS response (not a preflight-only header claim), rejected origin, and live delivery (Q7; PC2).

### 6. Constitution / ADR / whitelist

After Gate 1: apply `constitution-amendment-v1.5.0.md`, accept ADR-0029 in-tree as Accepted if still Proposed, add `|IEnrichmentStatusNotifier` to whitelist preserving other tokens (Q1).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Constitution closed port list departure until v1.5.0 | Ticket requires `IEnrichmentStatusNotifier` | Silent port without amendment violates Principle I / compliance rule; escalated as owner Gate 1 (Q1) |

## Phase B entry preflight (not Phase A work)

1. User answered Gate 1 checkbox (constitution v1.5.0 + ADR-0029).
2. Pickup drift check vs `origin/main` for prerequisites DEV-421/310/314/320 and contract/DI drift.
3. Confirm SignalR.Client 10.0.14 still aligns with Microsoft.AspNetCore.* train; stop for Patron if drift.
4. Confirm constitution baseline still 1.4.0 (or reconcile version bump).
5. Confirm ADR 0029 path free of collision on main before Accepted publication.

## Gates (Phase B)

Run applicable analyzer, complexity (implementation then refactor ceiling), InspectCode, architecture, unit/integration, property, vulnerable-package, format, mutation (no `--since` in worktree), and web/generated-contract gates per `harness.yml`. Report SKIPPED/SKIP/N/A/Could not run distinctly. Closing bar: Critical/High/Medium verified in-scope findings block; Low non-blocking; two-round challenge/review cap (Q9).
