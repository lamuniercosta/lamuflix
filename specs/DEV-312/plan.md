# Implementation Plan: DEV-312 OpenAPI Contract and Scalar Delivery

**Branch**: `feature/312-spec` | **Date**: 2026-10-08 | **Spec**: `specs/DEV-312/spec.md`

**Input**: `specs/DEV-312/spec.md`, frozen `brief.md`, `CONCLUSIONS.md` Q1–Q5, `recon-DEV-312`, `specs/PRODUCT.md`

## Summary

Wire `Microsoft.AspNetCore.OpenApi` (`AddOpenApi` v1, `/api`-restricted via an `/api`-path document transformer with no MVC reference; `ApiHostCompositionTests.cs` unchanged) plus Scalar at `/scalar`, both Development-only, over the nine existing business operations with no endpoint, DTO, status, or handler change; commit the generated `web/src/api/openapi.json` as the sole authoritative baseline; prove it with a fail-closed Verify drift test in `LamuFlix.IntegrationTests` using the derived-snapshot-plus-equality branch (research.md D3); document the explicit local opt-in regeneration in `quickstart.md`; Keel authors `docs/adr/ADR-0007.md` separately. Order: packages and composition → integration test and export path → document and derived snapshot → ADR-0007 and quickstart → controlled drift negative proof → gates.

## Technical Context

**Language/Version**: C# with .NET 10

**Primary Dependencies** (exact plan-stage pins): `Microsoft.AspNetCore.OpenApi` 10.0.0, `Scalar.AspNetCore` 2.0.15, `Verify.XunitV3` 30.3.0 — all new, pinned in `Directory.Packages.props`, version-specific evidence in research.md D3. `Verify.XunitV3` 30.3.0 ships a `net10.0` asset whose dependency floor is the open-ended `xunit.v3.extensibility.core (>= 2.0.2)`, so the repository `4.0.1` family satisfies the range by NuGet resolution; this is package-range evidence, not a publisher tested-matrix claim. `Scalar.AspNetCore` 2.0.15 ships `net8.0` and `net9.0` assets only with `net10.0` computed-compatible, so `net10` support rests on .NET forward TFM roll-forward and is proven by the Phase B restore and test verdict, not by a publisher `net10` support statement. No incompatibility is inferred from a dependency minimum or asset list alone and no existing xUnit/framework dependency is downgraded. `Microsoft.Extensions.ApiDescription.Server` is explicitly rejected.

**Storage**: N/A — no entity, migration, or `DbContext` change (recon:111).

**Testing**: xUnit v3 + Shouldly; `WebApplicationFactory` via existing `ApiHostFactory` (in-memory config, `Features:LocalPlay` false, stubbed health checks; recon:89); Verify snapshot; no Testcontainers (no database or messaging operation runs; recon:89; constitution IX); no EF InMemory; no driver mocks; no new assertion or mocking library.

**Target Platform**: Linux server API; local-play execution path untouched and never invoked by tests.

**Project Type**: Backend API contract slice plus one generated JSON baseline (no web source change; `web/src/api` is a ticket-named subfolder of existing `web/`).

**Performance Goals**: N/A beyond existing gate behavior.

**Constraints**: `TimeProvider` for time; `TreatWarningsAsErrors`; no explanatory comments (AAA headers only in tests); deterministic document output; received files untracked; normal/CI tests never write tracked files.

**Scale/Scope**: Nine business operations (fewer distinct paths; count path/method pairs); file envelope per brief Q4 only.

## Constitution Check

- Technology Stack Constraints (API docs row): `Microsoft.AspNetCore.OpenApi` + Scalar at `/scalar`, document committed at `web/src/api/openapi.json` — this plan implements exactly that row; the deferred `openapi-typescript`/`openapi-fetch` client stays with DEV-320 per owner-ticked OD-1.
- API and Contract Rules: document snapshot-tested with Verify, drift fails the build via the existing CI Test step; no API versioning introduced; success/error shapes unchanged.
- Principle IX: `WebApplicationFactory` + Verify are the listed API tools; Testcontainers correctly absent (no persistence/messaging layer exercised); xUnit v3 only.
- Documentation Rules: Microsoft Learn for Microsoft-owned surface (D1–D2), project-published docs for Scalar/Verify (D2–D3); ADR-0007 (Keel-owned) records the Development exposure branch and runtime-generation choice.
- No new project, top-level folder, layer, schema change, or secret/Process.Start/LocalPlay wiring change — no §2.3 escalation beyond the brief's deliberate rulings.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-312/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── spec.md              # /speckit-specify command output
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

No `data-model.md` (no data change) and no `contracts/` (the contract artifact is the ticket deliverable `web/src/api/openapi.json` itself, generated at implementation).

### Source Code (repository root)

```text
src/LamuFlix.Api/
├── LamuFlix.Api.csproj          # edit: package references
├── Program.cs                   # edit: AddOpenApi, /api-path document transformer, Development-only mappings
└── Endpoints/                   # unchanged (no endpoint edits)
tests/LamuFlix.IntegrationTests/
├── LamuFlix.IntegrationTests.csproj  # edit: Verify integration wiring
├── OpenApiContractTests.cs           # new: route/drift proof + opt-in regeneration entry
└── ApiHostFactory.cs                 # conditional: minimal additive support only (see below)
web/src/api/
└── openapi.json                      # new generated authoritative baseline
docs/adr/
└── ADR-0007.md                       # new, Keel-owned (not this plan's author)
```

**Structure Decision**: Existing projects only; `web/src/api` is the ticket-named folder inside existing `web/` (recon:110).

## Design

### Composition (`Program.cs`)

`AddOpenApi("v1")` with an `/api`-path document transformer registered on `OpenApiOptions` (remove non-business path items; exact path-boundary matching for `/api` business paths; no MVC reference; research.md D1); inside `if (app.Environment.IsDevelopment())`, `MapOpenApi()` plus `MapScalarApiReference()` (research.md D2). First environment branch in `Program.cs`; health mappings untouched; `ApiHostCompositionTests.cs` unchanged.

### Drift test (`OpenApiContractTests.cs`)

Explicit Development host configuration (`WithWebHostBuilder`/`UseEnvironment` per test preferred; brief Q2); proves Scalar response and document wiring, OpenAPI 3.1, exact nine path/method membership with retained DTO/status metadata; separate Production host check proving 404 for both routes only (`ApiHostFactory.cs` minimal additive support only if per-test configuration cannot cover Production absence, changing no config value, LocalPlay false, health stub, or validation flags — brief Q4(d)). Membership asserts actual generated path/method pairs against this expected generated set — GET /api/movies, GET /api/movies/{id}, POST /api/movies/import, POST /api/movies/{id}/enrichment, POST /api/movies/{id}/watchlist, DELETE /api/movies/{id}/watchlist, POST /api/movies/{id}/play, GET /api/genres, GET /api/people — never derived from generated output or the baseline. The operation table records source route templates (e.g. `/api/movies/{id:int}`, where `:int` is a routing match constraint per [Routing route constraints](https://learn.microsoft.com/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-constraints)), while OpenAPI path items name endpoints without constraint syntax (e.g. `/api/products/{id}` per [OpenAPI overview](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0#api-v-api-operation-v-api-endpoint)); hence the expected generated spelling `GET /api/movies/{id}` above. One shared generation mechanism for T007, drift comparison, initial export, and regeneration: HTTP GET of `/openapi/v1.json` from the explicit Development host using the default factory client base URI `http://localhost/` — the documented default of both [TestServer.BaseAddress](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.testhost.testserver.baseaddress?view=aspnetcore-10.0) and [WebApplicationFactoryClientOptions.BaseAddress](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactoryclientoptions.baseaddress?view=aspnetcore-10.0) — passed identically by every caller with no custom `BaseAddress` and no per-test port override, so the incoming request host is `localhost` on every machine and run. The generated `servers` entry derives from that incoming request (scheme/host/PathBase per [OpenAPI server URL behavior](https://learn.microsoft.com/aspnet/core/breaking-changes/11/openapi-server-url-trailing-slash?view=aspnetcore-10.0)); hence it is stable on this fixed-base-URI path and is compared whole with no scrubbing. No `IOpenApiDocumentProvider` branch is used. Complete committed-document equality plus Verify derived snapshot at `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` (research.md D3), deterministic repeated generation, and a controlled contract-mutation failure with no baseline writes or auto-accept. Repository-root anchor (fail closed): starting from the test assembly directory, walk ancestors at most 8 levels for the stable checkout marker `Directory.Packages.props` (an existing root file edited per brief Q4, present before and after initial export); the verified root is the unique ancestor directory containing it. Resolve both the authoritative JSON and the derived snapshot against that same checkout root independently of the process working directory; validate target containment under the root and fail the test on a missing or ambiguous marker instead of reading or writing an unrelated root. The generated baseline is never a checkout-identity marker. Normal drift requires both `web/src/api/openapi.json` and the derived snapshot to already exist — a missing file is a test failure, never a pass. Only the explicit opt-in export/regeneration path may create the approved output directory and files inside the verified root. Single fixed normalization: replace every CRLF with LF in each compared text; nothing else. The same run first asserts normalized full-text equality between all three of the generated document, the committed `web/src/api/openapi.json` content, and the derived verified file content — before Verify runs — then invokes `Verify(target: <normalized document string>, extension: "json")` with only `UseDirectory("Snapshots")` and `UseFileName("OpenApiContractTests.DriftMatchesCommittedBaseline")`, no other settings and no scrubbers. The drift verdict rests on the three-way pre-Verify equality, so it never depends on Verify serialization or defaults; any Verify-added difference can only fail loudly, never hide a field. Order preserved: shared export (T007) before initial baseline (T009). Regeneration entry uses filter `FullyQualifiedName~OpenApiRegeneration` with `LAMUFLIX_REGENERATE_OPENAPI=true`, identical to quickstart.md, writing those same normalized bytes to both baseline files together. Scrub list: none beyond the single fixed line-ending normalization; every other value compared whole.

### Regeneration entry

Test-project opt-in path, skipped unless the documented local switch is set; same Development host and document path as the drift test; replaces `openapi.json` plus the derived snapshot together. Exact command in `quickstart.md`, referenced from ADR-0007.

### Conditional files (each cites its activating force)

| File | Allowed only if |
|---|---|
| Derived snapshot `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` | Verify cannot target the exact path — already established (research.md D3), so this branch is active |
| `.gitignore` received-file exclusion | Existing rules do not cover `*.received.*` (Q3 fail-closed requirement) |
| `.gitattributes` eol line | Needed for the fixed line-ending normalization (Q3 equality requirement) |
| `ApiHostFactory.cs` minimal additive support | Production-absence test cannot use per-test host configuration (Q2 proof requirement) |

## Complexity Tracking

No constitution violation to justify; no entry.

## Gates (Phase B, Gauge; none run in Phase A)

Build, tests, format, three analyzer gates for changed C#, configured property/security/diff gates, applicable web checks; API-only mutation N/A only while the `harness.yml:17-21` exclusion is live-valid, otherwise the real verdict counts. Scope-empty SKIPPED, configured-disabled SKIP, N/A, and Could not run are distinct from PASS. Pre-existing `harness.yml:45` change preserved untouched; no threshold added or lowered.
