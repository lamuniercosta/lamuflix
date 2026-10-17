# Tasks: Embedding port and deterministic test generator

**Input**: Design documents from `/specs/DEV-345/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Tests**: Required. The brief maps every acceptance criterion and every settled edge case to a proof. Patron Q3 sequences those proofs after the package, the dimension helper, and the fake. This list keeps that phase order. The order is a sequencing decision. It is not a technical dependency of the Story 1 metadata substitute proofs.

**Organization**: Tasks are grouped by user story. Story 1 proofs stay in Phase 5, after the fake, because that is the approved sequence. Metadata cases use NSubstitute interface substitutes with configured metadata and zero recorded generation calls. They do not use the fake to fabricate a reported dimension. T007 also keeps a default-fake validation integration case. T019 keeps the overridden-fake rejection and generation integration proof.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Paths match `plan.md`: existing `src/LamuFlix.Core`, `tests/LamuFlix.Tests.Common`, `tests/LamuFlix.UnitTests`, and `tests/LamuFlix.ArchitectureTests`
- Both new test files stay at the UnitTests project root. Namespace is `LamuFlix.UnitTests`. Classes are `public sealed`. Assertions use Shouldly. Do not add a local `using Xunit`; the project global usings already provide it. Root placement is the Patron-approved choice. It does not claim existing embedding tests, and it does not mirror `LamuFlix.Core.Ports` under a `Ports` folder

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Pin the abstractions package the port needs

- [X] T001 Pin `Microsoft.Extensions.AI.Abstractions` version `10.11.0` in `Directory.Packages.props`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core can compile against the external embedding port

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T002 Add the centrally managed `Microsoft.Extensions.AI.Abstractions` package reference to `src/LamuFlix.Core/LamuFlix.Core.csproj`

**Checkpoint**: Core references the abstraction. No generator is registered in Api or Work.

---

## Phase 3: User Story 1 - Fixed embedding length checked before use (Priority: P1) 🎯 MVP

**Goal**: Core holds the 384-length contract and rejects a generator whose report is not 384, without generating a vector (FR-001 through FR-005, SC-003)

**Independent Test**: After Phase 5, an NSubstitute substitute configured with metadata of 384 is accepted, and substitutes configured with a different integer, missing metadata, or a null dimension are rejected, each with zero recorded `GenerateAsync` calls. A null generator is rejected. A default fake is accepted by validation as the integration case. The fake is not required to fabricate the metadata cases.

### Implementation for User Story 1

- [X] T003 [US1] Add `public const int Value = 384` and `public static void Validate(IEmbeddingGenerator<string, Embedding<float>> generator)` in `src/LamuFlix.Core/Ports/EmbeddingDimensions.cs` (namespace `LamuFlix.Core.Ports`). Accept only `DefaultModelDimensions == 384`. Throw `InvalidOperationException` for absent metadata, a null dimension, or any other integer. Throw `ArgumentNullException` for a null generator. Do not call generation
- [X] T004 [P] [US1] Add `Microsoft.Extensions.AI` to `AllowedFeatureDependencies` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` (allowlist cited at lines 301–304). The entry is forward-looking for DEV-412. Check the bounded edit and leave every other dependency rule unchanged. Do not add a feature-folder consumer to exercise the allowance

**Checkpoint**: The helper and the allowlist edit are in place. Metadata proofs use NSubstitute substitutes and do not wait on the fake to fabricate a reported dimension. The approved phase order still runs those proofs after the fake. That order is sequencing, separate from the substitute proofs' technical dependency on T003. The default-fake integration case is the part that needs the fake.

---

## Phase 4: User Story 2 - Deterministic vectors without a model (Priority: P2)

**Goal**: Tests can build deterministic 384-element vectors with no model download (FR-006 through FR-015, SC-001, SC-002, SC-004, SC-005, SC-006)

**Independent Test**: After Phase 6, repeated inputs match, representative distinct inputs differ, and the batch, empty, rejection, cancellation, override, and service cases match `spec.md`

### Implementation for User Story 2

- [ ] T005 [US2] Edit `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj` only if `Microsoft.Extensions.AI.Abstractions` is not already visible through the existing project references
- [ ] T006 [US2] Add `public sealed class FakeEmbeddingGenerator` implementing `IEmbeddingGenerator<string, Embedding<float>>` (`GenerateAsync`, `GetService`, and `Dispose`) in `tests/LamuFlix.Tests.Common/FakeEmbeddingGenerator.cs` (namespace `LamuFlix.Tests.Common`). Public sealed accessibility lets `LamuFlix.UnitTests` consume it across the project boundary. No visibility workaround and no new project. Constructor is `FakeEmbeddingGenerator(int? reportedDimensions = EmbeddingDimensions.Value)`, and metadata is `new EmbeddingGeneratorMetadata(defaultModelDimensions: reportedDimensions)` with `ProviderName`, `ProviderUri`, and `DefaultModelId` null. Encode exact UTF-8, append four big-endian counter bytes for `0u` through `47u`, hash each block with SHA-256, read eight big-endian words, map with `(word / 2147483648.0) - 1.0`, L2-normalize in `double`, and cast to `float`. A zero norm returns `[1f, 0f, ...]`. The vector is not all zeros. Unit length is `Math.Abs(norm - 1.0) <= 1e-6`. No case or whitespace folding, no `Random`, and no `string.GetHashCode()`. Preserve batch order and allocate a fresh buffer per item and per call. The type is stateless: reported-dimension configuration is immutable, buffers are fresh per item and per call, SHA-256 operations are static, and there is no mutable shared state. Do not add a concurrency test, a cache, or a synchronization abstraction. Throw `ArgumentNullException` for a null batch or null element, and `ArgumentOutOfRangeException` for a non-null `options.Dimensions` other than 384. Call `ThrowIfCancellationRequested` before the batch, before each item, and before a successful return, and do not return a partial batch. `GetService` throws `ArgumentNullException` for a null `serviceType`, returns null for a non-null `serviceKey`, and with a null key matches `typeof(EmbeddingGeneratorMetadata)` exactly before `serviceType.IsInstanceOfType(this)`. `Dispose` is a no-op. Its empty block carries one narrowly scoped intentional-empty-block justification comment, under the existing comment exemption. No other explanatory comment is authorized

**Checkpoint**: The fake compiles. It is not the instrument for Story 1 metadata cases. Those use NSubstitute substitutes. The fake serves the default-fake validation integration in T007 and the overridden-fake rejection and generation proof in T019.

---

## Phase 5: User Story 1 proofs (Priority: P1)

**Goal**: Prove startup validation with NSubstitute substitutes for matching, mismatched, absent, and null metadata, each with zero recorded generation calls, plus a default-fake validation integration case (AC2, SC-003). The fake is not required to fabricate those metadata cases.

**Independent Test**: The five cases below pass without a model, network, or database. Matching, mismatched, absent, and null metadata use NSubstitute substitutes and record zero `GenerateAsync` calls. T007 also accepts a default fake. No probe vector is generated.

- [ ] T007 [US1] Add `public sealed class EmbeddingDimensionsTests` in namespace `LamuFlix.UnitTests` in `tests/LamuFlix.UnitTests/EmbeddingDimensionsTests.cs`. Use Shouldly. Use an NSubstitute substitute of `IEmbeddingGenerator<string, Embedding<float>>` with configured metadata whose `DefaultModelDimensions` is 384. `EmbeddingDimensions.Validate` accepts it. Assert zero recorded `GenerateAsync` calls. Also keep a default `FakeEmbeddingGenerator` validation integration case: `Validate` accepts that fake from its reported dimension and the test does not call `GenerateAsync`. The substitute is the metadata instrument. The default fake is the integration case
- [ ] T008 [US1] In `tests/LamuFlix.UnitTests/EmbeddingDimensionsTests.cs`, reject homogeneous non-384 integers with `InvalidOperationException` using `[Theory]` and `[MemberData]` or `[InlineData]`. Configure each row on an NSubstitute substitute of `IEmbeddingGenerator<string, Embedding<float>>`. Assert zero recorded `GenerateAsync` calls. Keep this matrix to the mismatched-integer shape. Do not combine it with absent metadata, a null dimension, or a null generator, and do not duplicate the generation algorithm
- [ ] T009 [US1] In `tests/LamuFlix.UnitTests/EmbeddingDimensionsTests.cs`, reject absent metadata with `InvalidOperationException` on an NSubstitute substitute of `IEmbeddingGenerator<string, Embedding<float>>` whose configured service result supplies no metadata. Assert zero recorded `GenerateAsync` calls. This behavioral shape stays separate from the T008 integer matrix and from the null-dimension case
- [ ] T010 [US1] In `tests/LamuFlix.UnitTests/EmbeddingDimensionsTests.cs`, reject a null `DefaultModelDimensions` with `InvalidOperationException` on an NSubstitute substitute of `IEmbeddingGenerator<string, Embedding<float>>` with that metadata configured. Assert zero recorded `GenerateAsync` calls. This behavioral shape stays separate from the T008 integer matrix and from absent metadata. Do not duplicate the generation algorithm
- [ ] T011 [US1] In `tests/LamuFlix.UnitTests/EmbeddingDimensionsTests.cs`, reject a null generator with `ArgumentNullException`. This shape stays outside the metadata matrices

**Checkpoint**: Story 1 is proved on its own. Story 2 behavioral proofs are not required for this checkpoint.

---

## Phase 6: User Story 2 proofs (Priority: P2)

**Goal**: Prove deterministic generation, length 384, and the settled edge cases (AC1 and AC2)

**Independent Test**: Each case below passes with no model, network, or database

- [ ] T012 [US2] Add `public sealed class FakeEmbeddingGeneratorTests` in namespace `LamuFlix.UnitTests` in `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`. Use Shouldly. Assert the same text yields the same `Embedding<float>.Vector` on one instance and on a second instance
- [ ] T013 [US2] In `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`, assert representative distinct texts differ, including texts that differ only by case and texts that differ only by whitespace, and do not assert that a hash collision is impossible
- [ ] T014 [US2] In `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`, assert each `Embedding<float>.Vector` has 384 finite elements, is not all zeros, and satisfies `Math.Abs(norm - 1.0) <= 1e-6` when the norm is computed in `double` over the returned floats. For the documented input `a`, also assert these fixed expected coordinates, independently derived from the approved expansion and hardcoded in the test: index 0 is `0.00911402702f`, index 1 is `-0.00423177239f`, index 7 is `0.0727573931f`, index 8 is `0.0636756271f`, index 15 is `-0.0686407834f`, index 16 is `-0.0733914003f`, and index 383 is `0.0482403785f`. Compare each selected coordinate to that float with exact `float` equality. Do not reproduce the production algorithm inside the assertion. Index 0 and index 7 are the first and last words of counter `0u`, index 8 and index 15 are the first and last words of counter `1u`, index 16 is the first word of counter `2u`, and index 383 is the last word of counter `47u`, so the proof covers counter order, word order, and the approved mapping
- [ ] T015 [US2] In `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`, assert a batch with duplicates keeps order and that each item and each call receives its own buffer
- [ ] T016 [US2] In `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`, assert an empty batch returns an empty `GeneratedEmbeddings<Embedding<float>>` and an empty string returns one vector
- [ ] T017 [US2] In `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`, cover homogeneous options and argument matrices with separate `[Theory]` cases (`[MemberData]` or `[InlineData]`) per behavioral shape: `ArgumentNullException` for a null batch and a null element; acceptance of null options, a null `Dimensions`, and `Dimensions` of 384; `ArgumentOutOfRangeException` for any other non-null `Dimensions`. Do not combine those shapes in one theory, and do not duplicate the generation algorithm in the tests
- [ ] T018 [US2] In `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`, keep the triple checkpoint contract and prove it with observable cancellation outcomes. Use a test-local instrumented `IEnumerable<string>` and its enumerator, with a test-owned cancellation source and no timing or thread-scheduling dependency. No fake configuration, production hook, wrapper, dependency, or new test file. Instrumentation stays in this file. Cover (a) a token cancelled before the call, asserting `OperationCanceledException` and that enumeration never starts; (b) cancellation during enumeration between items, when a later `MoveNext()` yields an item, asserting `OperationCanceledException` without advancing to another item or returning a partial batch; and (c) cancellation inside the final `MoveNext()` immediately before it returns false, asserting `OperationCanceledException` instead of a successful complete or partial batch. Await each `GenerateAsync` task and assert no generated batch is returned. Do not directly observe calls to `ThrowIfCancellationRequested`. Retain inspection that the implementation calls it before the batch, before each item, and before a successful return. Ordinary successful-path tests do not prove the pre-return checkpoint
- [ ] T019 [US2] In `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`, parameterize the homogeneous reported-dimension override matrix with `[Theory]` and `[MemberData]` or `[InlineData]` for another integer and for null. Each row still generates 384 elements, reports that override on `DefaultModelDimensions`, and leaves `ProviderName`, `ProviderUri`, and `DefaultModelId` null. `EmbeddingDimensions.Validate` rejects that overridden fake from the report alone. This is the overridden-fake rejection and generation integration proof for SC-006. Do not combine it with other behavioral shapes, and do not reproduce the generation algorithm inside the test
- [ ] T020 [US2] In `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`, assert `GetService` throws `ArgumentNullException` for a null `serviceType`, returns null for a non-null `serviceKey`, returns metadata when `serviceType == typeof(EmbeddingGeneratorMetadata)` before any self check, returns this generator when `serviceType.IsInstanceOfType(this)`, returns null otherwise, and `Dispose` does nothing

**Checkpoint**: Story 1 and Story 2 proofs both pass.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Gates named by the brief and the constitution. Do not lower a threshold. Do not edit generated gate configuration.

- [ ] T021 Add FsCheck property tests under `tests/LamuFlix.UnitTests/` only if `scripts/run-property-tests.ps1` requires them, covering the settled validation and fake invariants. Skip the extra tests when that script does not require them
- [ ] T022 Run `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` and confirm the new allowlist entry does not weaken unrelated dependency rules. The entry is forward-looking for DEV-412. Confirm the bounded edit and preservation of unrelated rules. Do not add a feature-folder consumer to exercise the allowance
- [ ] T023 Run the focused unit tests in `tests/LamuFlix.UnitTests/EmbeddingDimensionsTests.cs` and `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`
- [ ] T024 Run `./scripts/run-roslyn-analyzers.ps1` on the changed C# files
- [ ] T025 Run `./scripts/run-cyclomatic-complexity.ps1`, then `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`
- [ ] T026 Run `./scripts/run-jetbrains-inspectcode.ps1` on the changed C# files
- [ ] T027 Run `./scripts/run-property-tests.ps1`
- [ ] T028 Run `./scripts/run-vulnerable-packages.ps1`
- [ ] T029 Run `dotnet format --verify-no-changes`
- [ ] T030 Run `dotnet test`
- [ ] T031 Run `pwsh -NoProfile -File ./scripts/run-mutation.ps1` with no `since` switch, covering eligible changed Core code, and keep `PASS`, `SKIPPED (scope-empty)`, `SKIP`, `N/A`, and `Could not run` distinct
- [ ] T032 Confirm the implementation diff stays inside `Directory.Packages.props`, `src/LamuFlix.Core/LamuFlix.Core.csproj`, `src/LamuFlix.Core/Ports/EmbeddingDimensions.cs`, `tests/LamuFlix.Tests.Common/FakeEmbeddingGenerator.cs`, `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj` only if T005 required it, the two unit-test files, any property-test file T021 required, and `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on T001. Blocks both stories
- **User Story 1 implementation (Phase 3)**: Depends on T002
- **User Story 2 implementation (Phase 4)**: Depends on T002. T006 depends on T003 because its constructor default is `EmbeddingDimensions.Value`. Phase 4 follows Phase 3, per Patron Q3
- **User Story 1 proofs (Phase 5)**: The approved sequence places Phase 5 after T006. Substitute proofs for matching, mismatched, absent, and null metadata technically depend on T003 only. The default-fake integration case inside T007 technically depends on T006. That phase order is Patron Q3 sequencing, not a technical dependency of the substitute proofs
- **User Story 2 proofs (Phase 6)**: Depend on T006. T019 is the overridden-fake rejection and generation integration proof and also depends on T003
- **Polish (Phase 7)**: Depends on Phases 5 and 6

### User Story Dependencies

- **User Story 1 (P1)**: The helper and the allowlist do not need the fake. Metadata acceptance and rejection proofs use NSubstitute substitutes with configured metadata and zero recorded `GenerateAsync` calls. They do not need the fake to fabricate a non-384, missing, or null report. The default-fake validation integration in T007 and the overridden-fake rejection and generation proof in T019 use the fake. Phase 5 still follows Phase 4 as Patron Q3 sequencing
- **User Story 2 (P2)**: Needs the package reference and T003 to compile, because the constructor default is `EmbeddingDimensions.Value`. Its proofs do not need Story 1 tests

### Within Each User Story

- Patron Q3 places proofs after implementation, so test tasks are not written first
- T003 and T004 touch different files and may run together after T002
- T006 depends on T003. T005 before T006 when the package is not already visible
- Tasks T007 through T011 share one file and stay sequential. Tasks T012 through T020 share one file and stay sequential

### Parallel Opportunities

- T003 and T004 after T002
- Phase 6 may start once T006 is done, even if Phase 5 is still open, because the files differ
- Gate tasks T024 through T029 share a build output and stay sequential

---

## Parallel Example: User Story 1

```text
# After T002, different files:
Task: "T003 EmbeddingDimensions helper in src/LamuFlix.Core/Ports/EmbeddingDimensions.cs"
Task: "T004 Allowlist edit in tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T001 and T002
2. Complete T003 and T004
3. Complete T006 in the approved sequence, including the default-fake integration case that T007 keeps (T005 only if required). Substitute metadata proofs do not need the fake to fabricate a reported dimension
4. Complete T007 through T011. T007 includes the NSubstitute substitute and the default fake. T008 through T010 are substitute proofs with zero recorded `GenerateAsync` calls
5. Stop and validate Story 1 before the rest of the Story 2 matrix

### Incremental Delivery

1. Setup and Core reference
2. Story 1 contract and allowlist
3. Story 2 fake
4. Story 1 proofs, then Story 2 proofs. Metadata cases use NSubstitute substitutes. The default-fake and overridden-fake proofs are the ones that use the fake. Phase order stays the approved sequence
5. Gates in Phase 7, with mutation last

### Parallel Team Strategy

After T002, one person can take T003 and another T004. Story 2 proofs can proceed beside Story 1 proofs once the fake exists.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to spec.md User Story 1 or User Story 2
- Drafting gaps 1–10 are applied in T003, T006, T007, T012, T014, and T017 through T020. No open placeholder remains
- Do not add Api or Work registration, a local port wrapper, an ADR, a constitution edit, a follow-up ticket for the older Core dependency table, or a `CONTEXT.md` glossary change
- Keep the dimension literal a `const int` and keep mutable state out of static initialization
- `FakeEmbeddingGenerator` is `public sealed` and stateless: immutable reported-dimension configuration, fresh per-item and per-call buffers, static SHA-256 operations, and no mutable shared state. No concurrency test is required. `Dispose` has one intentional-empty-block justification and no other explanatory comment
- Homogeneous validation, options, and reported-dimension matrices use `[Theory]` with `[MemberData]` or `[InlineData]`. Distinct behavioral shapes stay separate. Tests do not duplicate the generation algorithm
- The allowlist entry is forward-looking for DEV-412. No feature-folder consumer is added. Approved helper and test locations stay. Existing exception contracts stay
- Commit after each task or logical group is outside this drafting pass
