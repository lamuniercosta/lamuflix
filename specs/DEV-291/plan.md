# Implementation Plan: Clean Architecture Foundation

**Branch**: `feature/291-spec` | **Date**: 2026-09-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/DEV-291/spec.md`

**Note**: This plan establishes the foundational project structure, dependency contracts, and architecture tests for LamuFlix per Constitution Principle I (Ports and Adapters with Feature-Organized Core).

## Summary

Establish a clean architecture foundation for LamuFlix by creating five .NET 10 projects (Core, Infrastructure, ServiceDefaults, Api, ArchitectureTests) with strict, unidirectional dependencies enforced by automated architecture tests using NetArchTest.Rules 1.3.2. Core houses domain logic with sealed Command/Query records and sealed Handlers, communicating with Infrastructure through defined ports. Architecture tests validate Core''s isolation from EF Core, Npgsql, RabbitMQ, and Infrastructure; sealed abstractions; and port-mediated cross-feature access.

## Technical Context

**Language/Version**: .NET 10 / C# 14  
**Primary Dependencies**: NetArchTest.Rules 1.3.2 (architecture testing; ticket-authorized per brief); Microsoft.Extensions.Logging.Abstractions, System.Collections.Immutable (Core only)  
**Storage**: PostgreSQL via Npgsql.EntityFrameworkCore.PostgreSQL (Infrastructure only; Core has no database dependencies)  
**Testing**: xUnit v3, NetArchTest.Rules 1.3.2  
**Target Platform**: .NET 10 runtime; ASP.NET Core for Api project; background worker for future enrichment service  
**Project Type**: Vertical-slice architecture with feature folders in Core; backend API + background worker  
**Constraints**: Core MUST NOT reference EF Core, Npgsql, RabbitMQ, or HTTP client libraries; no hard-coded secrets or machine paths  
**Scale/Scope**: Five named projects (Core, Infrastructure, ServiceDefaults, Api, ArchitectureTests); eight architecture checks (four dependency + three sealing + one port-mediation); single LamuFlix.sln file; Central Package Management via Directory.Packages.props

## Constitution Check

**GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.**

### Compliance with Constitution v1.1.0

- ✅ **Principle I (Ports and Adapters)**: Five projects with unidirectional dependencies per CLAUDE.md §1.1.0
  - Core depends only on BCL, Microsoft.Extensions.Logging.Abstractions, System.Collections.Immutable
  - Infrastructure references Core; Api references Core + Infrastructure + ServiceDefaults
  - Feature folders in Core (future); no cross-feature references outside Ports/Domain/Pipeline
  - Architecture tests enforce all rules (FR-006 through FR-010 in spec)

- ✅ **Principle II (Explicit Handlers)**: All use cases to be Commands/Queries (sealed records) with sealed Handlers; no MediatR
  - Sealed Command/Query records required (FR-007, FR-008)
  - Sealed Handlers required (FR-009)
  - Architecture tests validate sealing (test strategies in Phase 1)

- ✅ **Principle III (Typed Query Model)**: Filter model and typed queries (future feature, not in scope for DEV-291); noted in tasks

- ✅ **Principle VI (Observability)**: Health endpoints and TelemetryConstants (ServiceDefaults, future); noted in Phase 1 quickstart

- ✅ **Principle VII (Configuration Isolation)**: No secrets or hard-coded paths in DEV-291 skeletons; validated in Phase 1

- ✅ **Principle VIII (Ubiquitous Language)**: CONTEXT.md required for future features; no Portuguese identifiers in DEV-291

- ✅ **Principle IX (Test Pyramid)**: Architecture tests project added; Unit/Integration test projects noted for future epics

- ✅ **Tech Stack Constraints**: NetArchTest.Rules 1.3.2 (ticket-authorized, Patron decision per brief); Central Package Management; zero compiler warnings

- ✅ **No violations or deviations**: DEV-291 does not violate any constitution rule. No complexity justification needed.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-291/
├── spec.md                  # Feature specification (completed)
├── plan.md                  # This file (implementation plan)
├── tasks.md                 # Phase 2 output (task breakdown; NOT created by /speckit-plan)
└── [research.md, data-model.md, quickstart.md are NOT part of DEV-291 scope; future tickets]
```

### Source Code (repository root)

```text
src/
├── LamuFlix.Core/                   # Domain logic, features, ports (sealed Commands/Queries/Handlers)
├── LamuFlix.Infrastructure/         # Data access, adapters, external services
├── LamuFlix.ServiceDefaults/        # Shared hosting, telemetry, health checks
└── LamuFlix.Api/                    # HTTP endpoints, request/response contracts

tests/
└── LamuFlix.ArchitectureTests/      # Architecture validation; NetArchTest.Rules 1.3.2

Directory.Packages.props              # Central package management; one entry for NetArchTest.Rules 1.3.2

LamuFlix.sln                          # Solution file including all five projects
```

**Structure Decision**: Five-project architecture per Constitution Principle I. Core is feature-organized (future); Infrastructure adapts Core to EF Core, Npgsql, and messaging; Api exposes Core commands/queries via HTTP; ServiceDefaults provides shared configuration; ArchitectureTests enforces contracts.

## Complexity Tracking

No violations or complexity justifications needed. DEV-291 is a foundational architecture ticket authorized in full by the ticket''s Scope & Technical Design §1 (CLAUDE.md, brief.md, CONCLUSIONS.md).

---

## Phase 0: Outline & Research

**Status**: NOT CONDUCTED  
**Reason**: The brief (CONCLUSIONS.md, brief.md) is the authoritative research outcome. Patron has resolved all structural decisions and no research deliverables are part of DEV-291 scope:
- Five named projects and their dependencies (ticket §1, brief.md)
- NetArchTest.Rules 1.3.2 (Patron decision, ticket-authorized per brief)
- Eight architecture checks (four dependency + three sealing + one port-mediation) to validate (ticket §2, brief.md)
- Frozen review envelope (CONCLUSIONS.md)
- research.md, data-model.md, quickstart.md are follow-up tickets

**No NEEDS CLARIFICATION markers**: The specification has zero clarifications. Moving directly to Phase 1 design.

---

## Phase 1: Design & Contracts

### Structural Entities (DEV-291 scope)

DEV-291 establishes five .NET 10 projects with the following structure:

- **Core Project (LamuFlix.Core.csproj)**: Domain logic, features, ports; depends only on BCL, Microsoft.Extensions.Logging.Abstractions, System.Collections.Immutable
- **Infrastructure Project (LamuFlix.Infrastructure.csproj)**: Data access, adapters; references Core
- **ServiceDefaults Project (LamuFlix.ServiceDefaults.csproj)**: Shared hosting configuration; no references
- **Api Project (LamuFlix.Api.csproj)**: HTTP endpoints; references Core, Infrastructure, ServiceDefaults
- **ArchitectureTests Project (LamuFlix.ArchitectureTests.csproj)**: Architecture validation using NetArchTest.Rules 1.3.2 and xUnit v3; references Core, Infrastructure, Api, ServiceDefaults

---

## Phase 1 Deliverables (this plan only)

No data-model.md, quickstart.md, or research.md deliverables are part of DEV-291. Task implementation provides project file content; future tickets will add developer documentation.

---

## Gate Evaluation

**Constitution Check** — No violations. DEV-291 conforms to all principles and constraints. (Pending implementation verification)  
**No NEEDS CLARIFICATION** — Specification quality manually reviewed; no blockers. (Native /speckit-analyze unavailable; manual substitution per spec.md Assumptions)  
**Phase 0 Skipped** — Brief is authoritative; no research needed.  
**Phase 1 Design** — Architectural structure and project dependencies clearly defined; implementation not yet started; Gate 1 remains open until implementation tasks are complete.

---

## Implementation Sequence

1. **Phase 1a (Prepare)**: Create project file skeletons and LamuFlix.sln
   - Add NetArchTest.Rules 1.3.2 to Directory.Packages.props
   - Create five .csproj files with correct dependencies and references
   - Update LamuFlix.sln

2. **Phase 1b (Architecture Tests)**: Implement eight validation checks in ArchitectureTests (four dependency checks + three sealing checks + one port-mediation check)
   - Check 1: Core does not reference EF Core (FR-006)
   - Check 2: Core does not reference Npgsql (FR-006)
   - Check 3: Core does not reference RabbitMQ (FR-006)
   - Check 4: Core does not reference Infrastructure (FR-006)
   - Check 5: All Command records in Core are sealed (FR-007) — non-vacuous: a permanent violating fixture type `UnsealedCommandFixture` in `tests/LamuFlix.ArchitectureTests/Fixtures/Violating/` and a valid fixture `SealedCommandFixture` in `Fixtures/Valid/` ensure the check cannot pass vacuously
   - Check 6: All Query records in Core are sealed (FR-008) — same fixture approach as Check 5
   - Check 7: All Handler classes in Core are sealed (FR-009) — same fixture approach as Check 5
   - Check 8: Cross-feature access in Core is mediated through ports only (FR-010) — permanent violating fixture `DirectFeatureCouplingFixture` and valid fixture `PortMediatedFixture` in `Fixtures/` ensure the check cannot pass vacuously when no real features exist
   - EF Core counterexample (FR-014): temporarily add `<PackageReference Include="Microsoft.EntityFrameworkCore" />` to `LamuFlix.Core.csproj` and a type using that namespace; run `dotnet test`; record observed failure (exit code + failing test name) as exit evidence; revert both csproj and type; EF Core is already centrally versioned in `Directory.Packages.props` — no new CPM line required

3. **Phase 1c (Build & Validate)**: Verify solution builds and architecture tests pass
   - `dotnet build` → exit 0, zero warnings
   - `dotnet test` targeting ArchitectureTests → exit 0, all pass
   - Run applicable analyzer gates (Roslyn, complexity, InspectCode)

4. **Phase 2 (Tasks)**: Detailed task breakdown created by `/speckit-tasks` (NOT by this plan)

---

## Notes

- **NetArchTest.Rules 1.3.2 Decision**: Per brief.md and Patron''s ruling (CONCLUSIONS.md §1), this is a ticket-authorized dependency, not a taste assumption. If NetArchTest.Rules cannot express a ticket rule on net10.0, switch to ArchUnitNET (fallback named in ticket §1) and record the decision in spec.md.

- **Frozen Scope**: Five named projects, LamuFlix.sln, ArchitectureTests, specs/DEV-291, one Directory.Packages.props line. Any other file changes are follow-up tickets (per CONCLUSIONS.md).

- **Review Gate**: Critical/High findings fixed and re-verified before PR; Medium/Low logged as follow-ups. Two-round review cap (CONCLUSIONS.md).

- **Acceptance Criteria**: All eight architecture checks validated by automated tests (non-vacuous via durable fixtures); LamuFlix.sln compiles with zero warnings; temporary EF Core reference in Core causes architecture tests to fail (exit evidence recorded and reverted per FR-014).
