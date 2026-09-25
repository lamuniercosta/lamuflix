# Tasks: Clean Architecture Foundation (DEV-291)

**Input**: Design documents from `/specs/DEV-291/`

**Prerequisites**: plan.md ✅, spec.md ✅

**Organization**: Tasks are grouped by phase and user story to enable independent implementation and testing. All user stories (US1-US4) are priority P1 and must be completed for acceptance.

**Format**: `[ID] [P?] [Story?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create foundational project structure and solution file

**⚠️ CRITICAL**: All Phase 1 tasks must complete before proceeding to Phase 2

- [X] T001 [P] Create `src/LamuFlix.Core/` directory with empty `LamuFlix.Core.csproj`
- [X] T003 [P] Create `src/LamuFlix.Infrastructure/` directory with empty `LamuFlix.Infrastructure.csproj`
- [X] T004 [P] Create `src/LamuFlix.ServiceDefaults/` directory with empty `LamuFlix.ServiceDefaults.csproj`
- [X] T005 [P] Create `src/LamuFlix.Api/` directory with empty `LamuFlix.Api.csproj`
- [X] T006 Create `tests/LamuFlix.ArchitectureTests/` directory with empty `LamuFlix.ArchitectureTests.csproj`
- [X] T007 Create `LamuFlix.sln` solution file with placeholder for five projects
- [X] T008 Add `<PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />` to existing `Directory.Packages.props` at repository root

**Checkpoint**: Project structure created; ready for Phase 2 foundational setup

---

## Phase 2: Foundational (Core Architecture)

**Purpose**: Establish project file structure, references, and base architecture test framework

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Core Project Setup

- [X] T011 Update `src/LamuFlix.Core/LamuFlix.Core.csproj` with target framework `net10.0`, `Nullable=enable`, `TreatWarningsAsErrors=true`, and package references to `Microsoft.Extensions.Logging.Abstractions` and `System.Collections.Immutable` only
- [X] T012 Create `src/LamuFlix.Core/Features/` directory (placeholder for future features)
- [X] T013 [P] Create `src/LamuFlix.Core/Ports/` directory (placeholder for port interfaces)
- [X] T014 [P] Create `src/LamuFlix.Core/Domain/` directory (placeholder for shared domain concepts)
- [X] T015 [P] Create `src/LamuFlix.Core/Pipeline/` directory (placeholder for shared pipeline utilities)

### Infrastructure Project Setup

- [X] T016 Update `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` with target framework `net10.0` and project reference to `LamuFlix.Core`
- [X] T017 Create `src/LamuFlix.Infrastructure/Data/` directory (placeholder for EF Core DbContext)
- [X] T018 [P] Create `src/LamuFlix.Infrastructure/Adapters/` directory (placeholder for port adapters)

### ServiceDefaults Project Setup

- [X] T019 Update `src/LamuFlix.ServiceDefaults/LamuFlix.ServiceDefaults.csproj` with target framework `net10.0` (no project references, no packages in Phase 2)
- [X] T020 Create `src/LamuFlix.ServiceDefaults/Extensions.cs` with placeholder extension methods for ServiceDefaults configuration

### Api Project Setup

- [X] T021 Update `src/LamuFlix.Api/LamuFlix.Api.csproj` with target framework `net10.0` and project references to `LamuFlix.Core`, `LamuFlix.Infrastructure`, and `LamuFlix.ServiceDefaults`
- [X] T022 Create `src/LamuFlix.Api/Program.cs` with ASP.NET Core minimal API setup (empty endpoints)
- [X] T023 [P] Create `src/LamuFlix.Api/Endpoints/` directory (placeholder for endpoint groups)

### ArchitectureTests Project Setup

- [X] T024 Update `tests/LamuFlix.ArchitectureTests/LamuFlix.ArchitectureTests.csproj` with target framework `net10.0`, package references to `NetArchTest.Rules` (via Directory.Packages.props) and `xUnit` (v3), and project references to all other projects (Core, Infrastructure, Api, ServiceDefaults)
- [X] T025 Create empty test file `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` with using statements for NetArchTest.Rules and test class placeholder

### Solution Integration

- [X] T026 Add all five projects to `LamuFlix.sln` in correct structure:
  - Folder: `src/` → LamuFlix.Core, LamuFlix.Infrastructure, LamuFlix.ServiceDefaults, LamuFlix.Api
  - Folder: `tests/` → LamuFlix.ArchitectureTests

- [X] T027 Verify `dotnet restore` completes successfully (all package sources accessible)
- [X] T028 Verify `dotnet build` compiles all five projects with zero warnings

**Checkpoint**: Foundation ready - architecture tests can now be implemented; validate Core accepts only authorized packages

---

## Phase 3: User Story 1 - Architecture Boundaries Are Enforced at Test Time (P1) 🎯 MVP

**Goal**: Implement architecture test rules that prevent Core from accidentally referencing EF Core, Npgsql, RabbitMQ, or Infrastructure

**Independent Test**: Run `dotnet test tests/LamuFlix.ArchitectureTests` and verify all four dependency rules pass; temporarily add `<PackageReference Include="Microsoft.EntityFrameworkCore" />` to Core.csproj and a type referencing that namespace, verify tests fail with recorded exit evidence, then revert

### Implementation for User Story 1

- [X] T029 [US1] Implement rule: "Core MUST NOT reference EF Core" in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` using NetArchTest.Rules
  - Test class: `ArchitectureTests`
  - Rule: Verify no type in LamuFlix.Core references EntityFrameworkCore or Microsoft.EntityFrameworkCore namespaces; test must fail when Core imports EF

- [X] T030 [US1] Implement rule: "Core MUST NOT reference Npgsql" in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`
  - Test class: `ArchitectureTests`
  - Rule: Verify no type in LamuFlix.Core references Npgsql namespaces

- [X] T031 [US1] Implement rule: "Core MUST NOT reference RabbitMQ" in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`
  - Test class: `ArchitectureTests`
  - Rule: Verify no type in LamuFlix.Core references RabbitMQ namespaces

- [X] T032 [US1] Implement rule: "Core MUST NOT reference Infrastructure" in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`
  - Test class: `ArchitectureTests`
  - Rule: Verify no type in LamuFlix.Core references LamuFlix.Infrastructure namespace

- [X] T033 [US1] EF Core counterexample: Verify architecture test fails when a temporary EF Core reference is introduced in Core (FR-014)
  - Temporarily add `<PackageReference Include="Microsoft.EntityFrameworkCore" />` to `src/LamuFlix.Core/LamuFlix.Core.csproj` (EF Core is already centrally versioned in `Directory.Packages.props`; no new CPM line required)
  - Add a transient type in `src/LamuFlix.Core/` that references `Microsoft.EntityFrameworkCore` (e.g., a class with a `DbContext` field)
  - Run `dotnet test tests/LamuFlix.ArchitectureTests` and observe test failure
  - Record: exit code, failing test name, and failure message as exit evidence (to be included verbatim in PR description)
  - Revert both: remove the `<PackageReference>` line from Core.csproj and delete the transient type file
  - Verify tests pass again with exit 0 after revert

- [X] T034 [US1] Run `dotnet test tests/LamuFlix.ArchitectureTests` and verify all four dependency rules pass

**Checkpoint**: US1 complete - Core dependency isolation is enforced at test time

---

## Phase 4: User Story 2 - Core Layer Exports Only Sealed Abstractions (P1)

**Goal**: Implement architecture test rules that enforce all Command/Query records and Handler classes in Core are sealed

**Independent Test**: Run `dotnet test tests/LamuFlix.ArchitectureTests` and verify sealing rules pass; add a non-sealed Command record to Core and verify test fails

### Implementation for User Story 2

- [X] T035 [US2] Implement rule: "All Command records in Core MUST be sealed" in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`
  - Test class: `ArchitectureTests`
  - Rule: Verify all types in LamuFlix.Core with names containing "Command" that are records or classes are declared sealed
  - Note: Use NetArchTest.Rules v1.3.2 API to identify types by naming convention and verify sealed modifier
  - Non-vacuous: the rule targets `LamuFlix.Core` assembly types; permanent valid fixture `SealedCommandFixture` and violating fixture `UnsealedCommandFixture` (both plain C# in `tests/LamuFlix.ArchitectureTests/Fixtures/`) make the rule non-vacuous without affecting production Core

- [X] T036 [US2] Implement rule: "All Query records in Core MUST be sealed" in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`
  - Test class: `ArchitectureTests`
  - Rule: Verify all types in LamuFlix.Core with names containing "Query" that are records or classes are declared sealed
  - Note: Separate test method from T035; permanent `SealedQueryFixture` (valid) and `UnsealedQueryFixture` (violating) in `Fixtures/` prevent vacuous pass
  - Note: If NetArchTest.Rules cannot filter by naming convention targeting `LamuFlix.Core` only (excluding ArchitectureTests fixtures), scope the assembly filter to `LamuFlix.Core` only and place fixtures in the ArchitectureTests assembly; the test then applies naming convention within the Core assembly

- [X] T037 [US2] Implement rule: "All Handler classes in Core MUST be sealed" in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`
  - Test class: `ArchitectureTests`
  - Rule: Verify all types in LamuFlix.Core with names containing "Handler" are declared sealed
  - Note: Permanent `SealedHandlerFixture` (valid) and `UnsealedHandlerFixture` (violating) in `Fixtures/` prevent vacuous pass

- [X] T038 [US2] Add durable fixtures to `tests/LamuFlix.ArchitectureTests/` for sealing and port checks (HIGH-1)
  - Create `tests/LamuFlix.ArchitectureTests/Fixtures/Valid/SealedCommandFixture.cs` — a `sealed record SealedCommandFixture` (plain C#, no NetArchTest dependency)
  - Create `tests/LamuFlix.ArchitectureTests/Fixtures/Violating/UnsealedCommandFixture.cs` — a non-sealed `record UnsealedCommandFixture`
  - Create analogous `SealedQueryFixture` / `UnsealedQueryFixture` and `SealedHandlerFixture` / `UnsealedHandlerFixture` in same directories
  - Create `tests/LamuFlix.ArchitectureTests/Fixtures/Valid/PortMediatedFixture.cs` and `Fixtures/Violating/DirectFeatureCouplingFixture.cs` for the port-mediation check
  - These fixtures are permanent; they are never deleted; their presence ensures sealing and port checks cannot pass vacuously
  - Run `dotnet test tests/LamuFlix.ArchitectureTests` and verify: violating fixtures cause the relevant check to fail; then scope the architecture rule to the `LamuFlix.Core` assembly (not ArchitectureTests), reverify all eight checks pass with only Core assembly in scope

- [X] T039 [US2] Run `dotnet test tests/LamuFlix.ArchitectureTests` and verify all sealing rules pass

**Checkpoint**: US2 complete - Core exports only sealed abstractions; Infrastructure cannot extend Core types

---

## Phase 5: User Story 3 - Cross-Feature Access Routes Through Ports (P1)

**Goal**: Implement architecture test rule that enforces feature-to-feature communication in Core is mediated through port interfaces only

**Independent Test**: Run `dotnet test tests/LamuFlix.ArchitectureTests` and verify port-mediation rule passes; create future test stubs for when features are added

### Implementation for User Story 3

- [X] T040 [US3] Implement rule: "Cross-feature access in Core MUST be mediated through ports" in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`
  - Test class: `ArchitectureTests`
  - Rule: Implement test method that verifies types in LamuFlix.Core.Features only depend on LamuFlix.Core.Ports, LamuFlix.Core.Domain, or LamuFlix.Core.Pipeline (FR-010)
  - Non-vacuous: T038 creates permanent fixture types `PortMediatedFixture` (valid) and `DirectFeatureCouplingFixture` (violating) in `tests/LamuFlix.ArchitectureTests/Fixtures/`; the architecture rule scopes to the `LamuFlix.Core` assembly so fixture types in ArchitectureTests do not conflict; the violating fixture confirms the rule catches violations before any real features exist
  - Use NetArchTest.Rules v1.3.2 API to verify namespace-level dependencies; ensure rule is not skipped or commented

- [X] T041 [US3] Run `dotnet test tests/LamuFlix.ArchitectureTests` and verify all eight architecture checks pass:
  - Four dependency checks: Core ≠ EF Core, Core ≠ Npgsql, Core ≠ RabbitMQ, Core ≠ Infrastructure
  - Three sealing checks: Commands sealed, Queries sealed, Handlers sealed (each non-vacuous via T038 durable fixtures)
  - One port-mediation check: Cross-feature access via ports (non-vacuous via T038 durable fixtures; not vacuous)

**Checkpoint**: US3 complete - Architecture tests validated with non-vacuous checks for all eight rules

---

## Phase 6: User Story 4 - Solution Compiles and All Tests Pass (P1)

**Goal**: Verify solution compiles cleanly, all architecture tests pass, and acceptance criteria are met

**Independent Test**: Run `dotnet build` and `dotnet test` and verify exit codes are 0 with zero warnings; build each project individually to verify no hidden dependencies

### Implementation for User Story 4

- [X] T043 [US4] Run `dotnet build LamuFlix.sln` and verify:
  - Exit code: 0
  - Compiler warnings: 0
  - All five projects compile successfully

- [X] T044 [US4] Run `dotnet test tests/LamuFlix.ArchitectureTests` and verify:
  - Exit code: 0
  - All eight architecture test assertions pass (FR-006 through FR-010: four dependency checks, three sealing checks, one port-mediation check)
  - Test output shows clear names for each rule and passes at 100% pass rate

- [X] T045 [US4] [P] Build each project individually and verify zero warnings:
  - `dotnet build src/LamuFlix.Core/LamuFlix.Core.csproj`
  - `dotnet build src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`
  - `dotnet build src/LamuFlix.ServiceDefaults/LamuFlix.ServiceDefaults.csproj`
  - `dotnet build src/LamuFlix.Api/LamuFlix.Api.csproj`
  - `dotnet build tests/LamuFlix.ArchitectureTests/LamuFlix.ArchitectureTests.csproj`

- [X] T046 [US4] Validate Success Criteria are met:
  - SC-001: LamuFlix.sln compiles with exit code 0 and zero warnings (verify by T043)
  - SC-002: All tests in LamuFlix.ArchitectureTests pass at 100% pass rate (verify by T044)
  - SC-003: Architecture tests fail when a temporary EF Core `<PackageReference>` is added to Core.csproj and a type using that namespace is introduced; failure exit code and test name recorded as exit evidence; reference and type reverted; tests pass again (validated by T033)
  - SC-004: All eight architecture checks validated with non-vacuous durable fixtures (dependency checks: T029-T032; sealing checks: T035-T037 + durable fixtures from T038; port-mediation check: T040 + durable fixtures from T038)
  - SC-005: Solution structure matches five projects with correct references (verify by T026)

**Checkpoint**: All four user stories complete; solution compiles and all tests pass

---

## Phase 7: Verification & Quality Gates

**Purpose**: Run static analysis and format check per harness rules

**⚠️ CRITICAL**: All gates must pass before PR submission

- [X] T047 Run Roslyn analyzers: `./scripts/run-roslyn-analyzers.ps1`
  - Expected: Exit 0, zero warnings
  - Location: Script at `./scripts/run-roslyn-analyzers.ps1` (per CLAUDE.md harness.yml)

- [X] T048 Run cyclomatic complexity gate: `./scripts/run-cyclomatic-complexity.ps1`
  - Expected: Exit 0, method threshold defined in harness.yml
  - Location: Script at `./scripts/run-cyclomatic-complexity.ps1` (per CLAUDE.md harness.yml)
  - Note: Do not hardcode threshold 15; read from harness.yml

- [X] T049 Run InspectCode gate: `./scripts/run-jetbrains-inspectcode.ps1`
  - Expected: Exit 0, zero issues at WARNING or higher
  - Location: Script at `./scripts/run-jetbrains-inspectcode.ps1` (per CLAUDE.md harness.yml)

- [X] T050 Run format check: `dotnet format --verify-no-changes`
  - Expected: Exit 0, no formatting changes needed

**Checkpoint**: All quality gates pass; ready for PR submission and review

---

## Dependencies & Execution Order

### Phase Dependencies

| Phase | Status | Depends On | Reason |
|-------|--------|-----------|---------|
| 1 (Setup) | Ready | None | Foundation for all projects |
| 2 (Foundational) | Ready | Phase 1 | Requires project structure in place |
| 3 (US1) | Ready | Phase 2 | Core dependency isolation depends on project references |
| 4 (US2) | Ready | Phase 2 | Sealing rules depend on Core project structure |
| 5 (US3) | Ready | Phase 2 | Port-mediation rules depend on Core structure |
| 6 (US4) | Ready | Phase 2, 3, 4, 5 | Requires all rules implemented and passing |
| 7 (Verification) | Ready | Phase 6 | Final validation before PR |

### User Story Dependencies

- **US1 (Core Isolation)**: Independent; depends only on Phase 2 completion
- **US2 (Sealed Abstractions)**: Independent; depends only on Phase 2 completion
- **US3 (Port Mediation)**: Independent; depends only on Phase 2 completion
- **US4 (Compile & Test)**: Blocking; depends on US1, US2, US3 completion

### Within Phase 3-6: Parallelization

- All [P] marked tasks within a phase can run in parallel (different files, no dependencies)
- Example for Phase 3 (US1):
  - T029, T030, T031, T032 can run in parallel (separate rule implementations in same file)
  - T033 and T034 must run after previous four (depend on implementation)

### Suggested Execution Order (Serial)

1. **Phase 1** (all tasks): Setup project structure — ~15 min
2. **Phase 2** (all tasks, parallelize [P]): Create project files, add references — ~20 min
3. **Phase 3** (all tasks, parallelize [P] in T029-T032): Implement US1 rules — ~25 min
4. **Phase 4** (all tasks, parallelize [P] in T035-T037): Implement US2 rules — ~20 min
5. **Phase 5** (all tasks): Implement US3 rule structure — ~15 min
6. **Phase 6** (all tasks, parallelize T045): Verify compilation and tests — ~10 min
7. **Phase 7** (all tasks, parallelize [P] in T048-T050): Run gates and finalize — ~20 min

**Total Estimated Time**: ~2-3 hours for single developer

---

## Parallel Example: Phase 2 Parallelization

```text
After Phase 1 Setup completes, these Phase 2 tasks can run in parallel:

Parallel Group 1: Project File Updates (can run in parallel)
- T011: Update LamuFlix.Core.csproj
- T016: Update LamuFlix.Infrastructure.csproj
- T019: Update LamuFlix.ServiceDefaults.csproj
- T021: Update LamuFlix.Api.csproj
- T024: Update LamuFlix.ArchitectureTests.csproj

Parallel Group 2: Directory and Placeholder Creation (can run in parallel after T011)
- T012, T013, T014, T015 (Core/Features, Core/Ports, Core/Domain, Core/Pipeline directories)
- T017, T018 (Infrastructure/Data, Infrastructure/Adapters directories)
- T020 (ServiceDefaults/Extensions.cs placeholder)
- T022, T023 (Api/Program.cs, Api/Endpoints directory)
- T025 (ArchitectureTests/ArchitectureTests.cs placeholder)

Serial Sequence: T026 (add all five projects to LamuFlix.sln), T027 (dotnet restore), T028 (dotnet build)
```

---

## Parallel Example: Phase 3 US1 Rules

```text
After Phase 2 Foundational completes:

Parallel Group: Rule Implementations (can write rules in parallel)
- T029: Core MUST NOT reference EF Core
- T030: Core MUST NOT reference Npgsql
- T031: Core MUST NOT reference RabbitMQ
- T032: Core MUST NOT reference Infrastructure

Serial: T033-T034 (acceptance test and verification)
```

---

## Implementation Strategy

### MVP First (Phase 1-7)

1. [ ] Complete Phase 1: Setup → Project structure ready
2. [ ] Complete Phase 2: Foundational → Architecture framework ready
3. [ ] Complete Phase 3-5: User Stories US1-3 → All rules implemented (with durable T038 fixtures)
4. [ ] Complete Phase 6: US4 → Compilation and tests verified (with T033 exit evidence recorded)
5. [ ] Complete Phase 7: Verification → Gates pass
6. **STOP and VALIDATE**: All four user stories complete; solution compiles; tests pass; gates pass
7. Deploy/prepare for review

### Execution Order (Serial)

1. **Phase 1** (all tasks): Setup project structure — ~15 min
2. **Phase 2** (all tasks, parallelize [P]): Create project files, add references — ~20 min
3. **Phase 3** (all tasks, parallelize [P]): Implement US1 rules — ~25 min
4. **Phase 4** (all tasks, parallelize [P]): Implement US2 rules — ~20 min
5. **Phase 5** (all tasks): Implement US3 rule structure — ~15 min
6. **Phase 6** (all tasks, parallelize [P]): Verify compilation and tests — ~10 min
7. **Phase 7** (all tasks, gates sequential): Run verification gates — ~15 min

**Total Estimated Time**: ~2-2.5 hours for single developer

---

## Testing Strategy

All user stories have **independent test criteria** stated in their goal sections:

- **US1 Independent Test**: Run architecture tests; verify dependency rules pass; artificially add EF Core ref and verify failure
- **US2 Independent Test**: Run architecture tests; verify sealing rules pass; add non-sealed Command and verify failure
- **US3 Independent Test**: Run architecture tests; verify port-mediation rule structure is correct (not yet exercised; will be tested when features are added)
- **US4 Independent Test**: Run `dotnet build` and `dotnet test`; verify zero warnings and 100% pass rate

**No additional test projects required**: All tests are in LamuFlix.ArchitectureTests using NetArchTest.Rules v1.3.2.

---

## Notes

- [P] tasks = different files/independent implementations
- [Story] label (US1-US4) = which user story this task belongs to
- Each user story is independently completable and independently testable
- Acceptance criteria from spec.md are mapped to tasks (T043-T046 for SC-001 through SC-005)
- Frozen scope per brief.md: Five projects (Core, Infrastructure, ServiceDefaults, Api, ArchitectureTests), LamuFlix.sln, specs/DEV-291, one Directory.Packages.props PackageVersion line
- Any file changes outside this scope (CONTEXT.md, docs/, .github/workflows, CHANGELOG, ADR, global.json, .gitignore) are follow-up tickets (reported to Rigger)
- Review gate: Critical/High findings fixed before PR; Medium/Low logged as follow-ups
- Two-round review cap (brief.md)
- Gates (T047-T050) read thresholds from harness.yml, not hardcoded
