# Feature Specification: Clean Architecture Foundation

**Feature Branch**: `feature/291-spec`

**Created**: 2026-09-24

**Status**: Draft

**Input**: DEV-291 — Establish clean architecture layers with strict dependency contracts and architecture tests

## User Scenarios & Testing

### User Story 1 - Architecture Boundaries Are Enforced at Test Time (Priority: P1)

Developers adding new features need confidence that architectural boundaries (Core, Infrastructure, Api, ServiceDefaults) are automatically enforced. Architecture tests must validate dependency constraints and sealed abstractions, preventing accidental violations.

**Why this priority**: Architectural violations (e.g., Core importing EF Core, Infrastructure accessing HTTP handlers) create tight coupling and technical debt. Catching these at test time prevents defects from entering the codebase.

**Independent Test**: Architecture test project compiles, runs, and reports violations when Core imports prohibited dependencies (EF Core, Npgsql, RabbitMQ, Infrastructure layer).

**Acceptance Scenarios**:

1. **Given** the solution builds successfully, **When** a temporary EF Core `<PackageReference>` is added to `LamuFlix.Core.csproj` and a type referencing `Microsoft.EntityFrameworkCore` is introduced into Core, **Then** architecture tests fail with a clear message indicating the violation; after observation the reference and type are reverted
2. **Given** a Core project exists, **When** architecture tests run, **Then** no type in Core may reference EF Core, Npgsql, RabbitMQ, or Infrastructure namespaces
3. **Given** the architecture test project exists, **When** it runs, **Then** all eight architecture checks are validated (four dependency checks + three sealing checks + one port-mediation check)

---

### User Story 2 - Core Layer Exports Only Sealed Abstractions (Priority: P1)

Core features export sealed Command and Query records and sealed Handlers, preventing infrastructure code from implementing business logic and ensuring the domain model is immutable and well-defined.

**Why this priority**: Sealed abstractions enforce that business logic stays in Core and infrastructure concerns stay in Infrastructure. This is foundational to clean architecture; without it, layers blur.

**Independent Test**: Architecture tests verify all Command, Query, and Handler types in Core are sealed. A test confirms that adding a non-sealed variant fails validation.

**Acceptance Scenarios**:

1. **Given** a Command record exists in Core.Features, **When** architecture tests run, **Then** the Command is sealed and cannot be inherited
2. **Given** a Handler exists in Core.Features, **When** architecture tests run, **Then** the Handler is sealed
3. **Given** an Infrastructure class attempts to inherit from Core.Handlers.CommandHandler, **When** code compiles, **Then** it fails with a compiler error (cannot inherit sealed class)

---

### User Story 3 - Cross-Feature Access Routes Through Ports (Priority: P1)

Features within Core must communicate via mediator ports, not direct feature-to-feature coupling. Infrastructure and external layers access Core features only through a defined surface, enforcing modularity and testability.

**Why this priority**: Direct feature coupling makes refactoring difficult and creates hidden dependencies. Port-based access creates explicit contracts and enables feature isolation.

**Independent Test**: Architecture tests validate that no feature namespace directly accesses another feature namespace except through a mediator port interface.

**Acceptance Scenarios**:

1. **Given** feature UserManagement and feature MediaLibrary both exist in Core, **When** MediaLibrary needs user data, **Then** it requests data through a port defined in Core.Ports, not by directly calling UserManagement
2. **Given** architecture tests run, **When** they check cross-feature references, **Then** all feature-to-feature communication is mediated through interfaces
3. **Given** Infrastructure needs to invoke a Core feature, **When** it does so, **Then** it accesses only the published port interface, not internal feature classes

---

### User Story 4 - Solution Compiles and All Tests Pass (Priority: P1)

The LamuFlix.sln solution must compile cleanly without warnings, and all architecture tests must pass, providing a solid foundation for future feature development.

**Why this priority**: A non-compiling or warning-filled solution signals incomplete work and makes it unsafe to add new features.

**Independent Test**: Running `dotnet build` and `dotnet test` on the solution succeeds with exit code 0 and no compiler warnings.

**Acceptance Scenarios**:

1. **Given** the LamuFlix.sln is cloned and all dependencies are restored, **When** `dotnet build` runs, **Then** the solution compiles with exit code 0 and zero warnings
2. **Given** the solution builds successfully, **When** `dotnet test` runs targeting LamuFlix.ArchitectureTests, **Then** all architecture tests pass with exit code 0
3. **Given** the solution is built and tests pass, **When** each of the five named projects is built individually, **Then** each compiles cleanly

---

### Edge Cases

- What happens if someone tries to add a circular dependency (e.g., Api references Infrastructure, Infrastructure references Api)? → Architecture tests must catch and fail
- What happens if Core accidentally references Infrastructure.Data? → Architecture tests must fail with a clear message about prohibited dependency
- What happens if a Command is not sealed? → Architecture tests must report it as a violation

## Requirements

### Functional Requirements

- **FR-001**: Solution MUST contain five .NET 10 projects (Core, Infrastructure, ServiceDefaults, Api, ArchitectureTests) as named in DEV-291 ticket Scope & Technical Design §1
- **FR-002**: Core project MUST depend only on BCL, Microsoft.Extensions.Logging.Abstractions, and System.Collections.Immutable (no database, queue, or HTTP libraries)
- **FR-003**: Infrastructure project MUST reference Core but Core MUST NOT reference Infrastructure
- **FR-004**: Api project MUST reference Core, Infrastructure, and ServiceDefaults
- **FR-005**: ServiceDefaults project MUST contain shared hosting configuration and be referenceable by Api
- **FR-006**: ArchitectureTests project MUST validate that Core does not reference EF Core, Npgsql, RabbitMQ, or Infrastructure namespaces
- **FR-007**: ArchitectureTests project MUST validate that all Command records in Core are sealed
- **FR-008**: ArchitectureTests project MUST validate that all Query records in Core are sealed
- **FR-009**: ArchitectureTests project MUST validate that all Handler classes in Core are sealed
- **FR-010**: ArchitectureTests project MUST validate that cross-feature access in Core is mediated through port interfaces only (no direct feature-to-feature namespace access)
- **FR-011**: ArchitectureTests project MUST use NetArchTest.Rules 1.3.2 as the architecture testing library (Patron decision per brief.md; if NetArchTest.Rules cannot express a rule or fails on net10.0, switch to ArchUnitNET and document the decision in spec.md)
- **FR-012**: Solution MUST contain a LamuFlix.sln file that includes all five named projects
- **FR-013**: Directory.Packages.props MUST contain one PackageVersion entry for NetArchTest.Rules 1.3.2 to enable central package management
- **FR-014**: Architecture tests MUST fail when a temporary EF Core `<PackageReference>` is added to `LamuFlix.Core.csproj` and a type referencing `Microsoft.EntityFrameworkCore` is introduced; the failure must be observed and recorded as exit evidence, after which the reference and type are reverted (EF Core is already centrally versioned in `Directory.Packages.props`; no new CPM line is needed for this step)

### Key Entities

- **Core Project**: Domain models, business logic, and feature mediators; no infrastructure or external concerns
- **Infrastructure Project**: Data access, messaging, and external service integrations; references Core for business logic
- **Api Project**: HTTP endpoints and request/response contracts; references Core for commands/queries and Infrastructure for services
- **ServiceDefaults Project**: Shared hosting, telemetry, and configuration for Api and Worker services
- **ArchitectureTests Project**: Automated validation of architecture constraints and naming conventions

## Success Criteria

### Measurable Outcomes

- **SC-001**: LamuFlix.sln compiles with exit code 0 and zero compiler warnings after `dotnet build`
- **SC-002**: All tests in LamuFlix.ArchitectureTests pass (100% pass rate) when run via `dotnet test`
- **SC-003**: Architecture tests fail immediately (exit code nonzero) when a temporary EF Core `<PackageReference>` is added to `LamuFlix.Core.csproj` along with a type referencing `Microsoft.EntityFrameworkCore`; after the failure is observed and recorded, the reference and type are reverted, proving the constraint is enforced
- **SC-004**: All eight architecture checks (four dependency checks: Core ≠ EF Core/Npgsql/RabbitMQ/Infrastructure; three sealing checks: Commands/Queries/Handlers; one port-mediation check) are validated by automated tests before any feature development proceeds
- **SC-005**: Solution structure matches the five named projects with correct references as defined in DEV-291 ticket §1

## Assumptions

- NetArchTest.Rules 1.3.2 is available on NuGet and compatible with .NET 10 (ticket-authorized choice; fallback to ArchUnitNET if incompatible)
- The solution structure follows vertical slice architecture within feature folders; cross-feature communication is mediated through defined ports
- "Sealed" refers to C# sealed keyword on record and class declarations; architectural tests validate this at the type level
- "Handler" refers to command and query handlers that encapsulate business logic and are sealed to prevent Infrastructure from extending them
- Future features will be added as new feature folders within Core, each exporting Commands, Queries, and Handlers as sealed types
- Core does not perform I/O operations; I/O (database, HTTP, queue operations) is delegated to Infrastructure and Api layers through dependency injection
- The frozen review envelope (five projects, LamuFlix.sln, ArchitectureTests, specs/DEV-291, one Directory.Packages.props line) is the complete scope for this ticket; any other file changes are follow-up tickets
- **[HIGH-1 fixture durability]** Valid and violating fixture types for sealing and cross-feature port checks MUST be permanent plain C# types residing in `tests/LamuFlix.ArchitectureTests/` (e.g., `Fixtures/Valid/` and `Fixtures/Violating/` subdirectories); they are NOT created or deleted during the test run. This ensures sealing and port checks cannot pass vacuously when no matching types exist: the violating fixture triggers failure, and the valid fixture anchors the passing baseline.
- **[HIGH-2 counterexample methodology]** The EF Core dependency counterexample (FR-014, SC-003) is validated by temporarily adding `<PackageReference Include="Microsoft.EntityFrameworkCore" />` to `LamuFlix.Core.csproj` and a type using that namespace, running `dotnet test`, recording the observed suite failure (exit code and failing test name) as exit evidence in the PR description, then reverting both the csproj edit and the type. EF Core is already centrally versioned at `Directory.Packages.props`; no new CPM line is required. `NetArchTest.Rules 1.3.2` remains the only new CPM line added by this ticket.
- **[manual analysis substitution]** The native `/speckit-analyze` command is unavailable in this session; specification quality analysis was performed manually by reviewing all requirements against the acceptance criteria, edge cases, and ticket scope. Findings recorded in `checklists/requirements.md`. No automated speckit output is attached.
