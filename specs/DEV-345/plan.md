# Implementation Plan: Embedding port and deterministic test generator

**Branch**: `feature/345-spec` | **Date**: 2026-10-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/DEV-345/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

DEV-345 adds the constitutional text-embedding port at a fixed length of 384 and a deterministic test generator so unit and integration tests need no model download. Core references centrally managed `Microsoft.Extensions.AI.Abstractions` `10.11.0` and uses `IEmbeddingGenerator<string, Embedding<float>>` directly. `src/LamuFlix.Core/Ports/EmbeddingDimensions.cs` holds a `const int` of 384 and a startup validation helper that reads `EmbeddingGeneratorMetadata.DefaultModelDimensions` and does not generate a probe vector. `tests/LamuFlix.Tests.Common/FakeEmbeddingGenerator.cs` is the public sealed test double. The architecture feature-dependency allowlist gains `Microsoft.Extensions.AI`, a forward-looking entry for DEV-412. This ticket checks that bounded edit and does not add a feature-folder consumer. Runtime OllamaSharp wiring, the real-embedding check, and vector schema stay on later tickets.

## Technical Context

**Language/Version**: .NET 10 / C# (product blueprint section 1; constitution Technology Stack)

**Primary Dependencies**: `Microsoft.Extensions.AI.Abstractions` `10.11.0`, centrally pinned in `Directory.Packages.props` and referenced by `LamuFlix.Core`. Not the full `Microsoft.Extensions.AI` package.

**Storage**: N/A. No entity, configuration, or migration. Vector storage is DEV-413.

**Testing**: Existing unit and architecture projects. xUnit v3, Shouldly, NSubstitute, and FsCheck.Xunit where the property-test gate requires it (constitution Principle IX). No model, network, or database for these proofs.

**Target Platform**: Existing LamuFlix solution. No new host, project, or deploy target.

**Project Type**: Port and test double inside the existing Core and Tests.Common projects.

**Performance Goals**: None specified. No latency or throughput target is in the brief.

**Constraints**: Vector length `EmbeddingDimensions.Value` (384). Validation uses reported metadata only. The dimension literal is `public const int Value` so mutation testing does not bake it into static initialization. The fake is deterministic: exact UTF-8, 48 independent SHA-256 blocks with big-endian counters `0u` through `47u`, L2 unit length within `1e-6`, no `Random`, no `string.GetHashCode()`. The fake is stateless: immutable reported-dimension configuration, fresh per-item and per-call buffers, static SHA-256 operations, and no mutable shared state. No concurrency test and no cache or synchronization abstraction. No production generator registration. No `DateTime.Now`. Mutable values stay out of static initialization. Frozen file set and the two-round review cap are unchanged.

**Scale/Scope**: size:M, estimate 1d. Two new types, one package pin, one allowlist edit, focused unit tests.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Verdict | Basis |
|---|---|---|
| I. Ports and feature-organised Core | Pass | `IEmbeddingGenerator<string, Embedding<float>>` is already a constitutional port (constitution lines 138–140) beside `TimeProvider`. No new local port and no ADR. The Core package table at line 127 does not list this abstraction; lines 138–140 and 346 pre-authorize it. Patron Q3: no constitution rewrite. |
| II. Handlers and decorators | Pass | No new command, query, or handler. |
| III. Filters | Pass | No query model change. |
| IV. Enrichment state machine | Pass | No enrichment change. |
| V. Validated inputs and errors | Pass | `Validate` throws `InvalidOperationException` for absent metadata, a null reported dimension, or any integer other than 384, and `ArgumentNullException` for a null generator. The fake throws `ArgumentNullException` for a null batch, a null element, or a null `serviceType`, and `ArgumentOutOfRangeException` when `options.Dimensions` is non-null and not 384. |
| VI. Observability | Pass | No production generator is registered, so no new health check or telemetry name is in this ticket. The runtime adapter is DEV-412. |
| VII. Configuration and time | Pass | No secret, machine path, options record, or `DateTime.Now`. The fake does not use `Random`. |
| VIII. Ubiquitous language | Pass | Q3: no new domain term and no `CONTEXT.md` change. |
| IX. Test pyramid | Pass | Behavioral proofs live in `LamuFlix.UnitTests`. Architecture proof is the existing allowlist test plus the one required string. No infrastructure mock. Mutation later covers eligible changed Core code at the configured threshold. |
| Embeddings stack row | Pass | Constitution line 346: this interface, OllamaSharp at runtime, `FakeEmbeddingGenerator` in tests. Runtime wiring is excluded here. |
| New port ADR / new project / HTTP contract | Pass | Not applicable. Exclusions are FR-016. |
| Static-analysis and format gates | Planned | Not run in this drafting pass. Tasks name the constitution scripts. A gate that cannot run is not a pass. |

Post-design re-check: Phase 1 adds no persisted model, no HTTP contract, and no production registration. The same verdicts hold. No row in Complexity Tracking is required.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-345/
├── spec.md              # /speckit-specify
├── plan.md              # /speckit-plan (this file)
├── research.md          # Phase 0 — rulings already closed; no new research
├── data-model.md        # Phase 1 — in-memory contracts only
├── quickstart.md        # Phase 1 — verification map
├── checklists/
│   └── requirements.md  # specify quality checklist
├── tasks.md             # /speckit-tasks
├── brief.md             # grill authority (pre-existing)
├── CONCLUSIONS.md       # Patron rulings (pre-existing)
├── ASSUMPTIONS.md       # taste log (pre-existing)
└── DRAFTING_RECEIPT.md  # ten gaps closed from the brief ruling
```

`contracts/` is omitted. This feature exposes no HTTP route, DTO, or client contract. The port is an in-process constitutional abstraction, documented in `data-model.md`.

### Source Code (repository root)

```text
Directory.Packages.props
src/LamuFlix.Core/
├── LamuFlix.Core.csproj
└── Ports/
    └── EmbeddingDimensions.cs
tests/LamuFlix.Tests.Common/
├── LamuFlix.Tests.Common.csproj   # edit only if the abstraction is not already visible
└── FakeEmbeddingGenerator.cs
tests/LamuFlix.UnitTests/
├── EmbeddingDimensionsTests.cs
└── FakeEmbeddingGeneratorTests.cs
tests/LamuFlix.ArchitectureTests/
└── ArchitectureTests.cs           # AllowedFeatureDependencies only
```

**Structure Decision**: Existing Core, Tests.Common, UnitTests, and ArchitectureTests projects. No new project, folder layer, or host project. Patron approved the root paths `tests/LamuFlix.UnitTests/EmbeddingDimensionsTests.cs` and `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`. Both use namespace `LamuFlix.UnitTests`, because a file in that project root uses the project namespace. That is the folder-equals-namespace rule for the root. It is not a claim that embedding tests already exist, and it is not a requirement to mirror `LamuFlix.Core.Ports` under a `Ports` folder. Test classes are `public sealed`. Assertions use Shouldly. xUnit is already in the project global usings.

## Complexity Tracking

No constitution violation requires a justification row. The older Core dependency table and the embeddings port lines are reconciled by the Q3 ruling: use the abstraction, do not edit the constitution, and do not file a follow-up ticket for that observation.

## Note on the Spec Kit phases

Phase 0 does not open a new search. Package, port shape, validation, fake behavior, the ten drafting-gap refinements, and exclusions are closed in `brief.md` (section Drafting gap decisions, commit `63d9fff`) and `CONCLUSIONS.md` (Patron commit `9c5e14c`). `recon-DEV-345:74-90` confirms decisions 5 and 6, including exact metadata-type matching before self resolution. `research.md` records those rulings.

Phase 1 writes `data-model.md` and `quickstart.md` only. There is no `contracts/` directory.

`CLAUDE.md` stays unchanged. Gap 10 places it outside the frozen file set. `.specify/feature.json` already pins `specs/DEV-345`.

## Design binding

Settled names and behaviors below are copied from the brief, including Drafting gap decisions, and from the conclusions those decisions cite. Recon confirms the service and metadata facts. No drafting gap remains open.

### Package and port

- Pin `Microsoft.Extensions.AI.Abstractions` version `10.11.0` in `Directory.Packages.props`.
- Add the central package reference to `src/LamuFlix.Core/LamuFlix.Core.csproj`.
- Callers use `IEmbeddingGenerator<string, Embedding<float>>`. Do not declare a Core interface for it.
- Add `"Microsoft.Extensions.AI"` to `AllowedFeatureDependencies` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` (recon cites the allowlist at lines 301–304). The entry is forward-looking for DEV-412. DEV-345 checks this bounded edit and the preservation of unrelated dependency rules. It does not add a feature-folder consumer to exercise the allowance. Do not loosen other dependency rules.
- Add a direct package reference to `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj` only when the abstraction types are not already visible through the existing project references.

### Dimension helper

File: `src/LamuFlix.Core/Ports/EmbeddingDimensions.cs`. Namespace: `LamuFlix.Core.Ports`.

- `public const int Value = 384`. It is `const` because mutation testing must not capture a mutable static.
- `public static void Validate(IEmbeddingGenerator<string, Embedding<float>> generator)`.
- Read `DefaultModelDimensions` from `EmbeddingGeneratorMetadata` obtained through the generator. Do not call generation.
- Accept only when that value is 384.
- Throw `InvalidOperationException` when metadata is absent, when the reported dimension is null, or when it is any other integer.
- Throw `ArgumentNullException` when the generator is null.
- Do not register the helper, or any generator, in Api or Work.

### Fake generator

File: `tests/LamuFlix.Tests.Common/FakeEmbeddingGenerator.cs`. Namespace: `LamuFlix.Tests.Common`, matching the existing test-double precedent cited in recon.

- `public sealed class FakeEmbeddingGenerator` implementing `IEmbeddingGenerator<string, Embedding<float>>`, including `GenerateAsync`, `GetService`, and `Dispose`. Public sealed accessibility lets the existing `LamuFlix.UnitTests` project consume the type across the project boundary. No visibility workaround and no new project.
- Constructor: `FakeEmbeddingGenerator(int? reportedDimensions = EmbeddingDimensions.Value)`. The override may be another integer or null. Generation still returns 384 elements. Metadata is `new EmbeddingGeneratorMetadata(defaultModelDimensions: reportedDimensions)`, so `ProviderName`, `ProviderUri`, and `DefaultModelId` are null and `DefaultModelDimensions` is the override.
- Encode with `Encoding.UTF8.GetBytes`. For counters `0u` through `47u` inclusive, append exactly four big-endian counter bytes (`BinaryPrimitives.WriteUInt32BigEndian`), hash that concatenation alone (`SHA256.HashData`), and read eight big-endian unsigned 32-bit words (`BinaryPrimitives.ReadUInt32BigEndian`). Map each word with `double raw = (word / 2147483648.0) - 1.0` in counter order and digest-word order. Sum squares and L2-normalize in `double`, then cast each coordinate to `float`. If the norm is zero, return `[1f, 0f, ...]`. No case or whitespace folding. No `Random`. No `string.GetHashCode()`.
- Finite 384-element vectors that are not all zeros. Individual zero coordinates are allowed. The Euclidean norm of the returned floats, computed in `double`, satisfies `Math.Abs(norm - 1.0) <= 1e-6`. Length is `Embedding<float>.Vector`.
- Fresh buffer per item and per call. Preserve batch order and duplicates. Equal inputs match across instances.
- Stateless: reported-dimension configuration is immutable, buffers are fresh per item and per call, SHA-256 operations are static, and there is no mutable shared state. No concurrency test, cache, or synchronization abstraction is added. This follows Q2.
- Empty batch returns an empty `GeneratedEmbeddings<Embedding<float>>`. Empty string is accepted.
- Throw `ArgumentNullException` for a null batch or any null element. Throw `ArgumentOutOfRangeException` when `options.Dimensions` is non-null and not 384. Accept a null options value, a null `Dimensions`, and `Dimensions` of 384.
- Call `cancellationToken.ThrowIfCancellationRequested()` before enumerating the batch, before each item, and before returning successfully. Cancellation throws `OperationCanceledException` and never returns a partial batch.
- `GetService`: throw `ArgumentNullException` when `serviceType` is null. Return null when `serviceKey` is not null. With a null key, return metadata when `serviceType == typeof(EmbeddingGeneratorMetadata)`, then return this generator when `serviceType.IsInstanceOfType(this)`, and return null otherwise. Exact metadata-type equality is checked first. Do not use `IsInstanceOfType` on the metadata object. Recon lines 74–90 confirm this order.
- `Dispose` is a no-op. Its empty block carries one narrowly scoped intentional-empty-block justification comment, under the existing comment exemption. No other explanatory comment is authorized. No configuration type beyond the reported-dimension override.

### Verification

Proofs are unit tests in `LamuFlix.UnitTests` plus the architecture allowlist. They map to spec stories US1 and US2 and to every edge case in `tasks.md`.

Story 1 metadata cases use existing NSubstitute interface substitutes of `IEmbeddingGenerator<string, Embedding<float>>`. Matching 384, a mismatched integer, absent metadata, and a null reported dimension are each configured on the substitute. Each case asserts zero recorded `GenerateAsync` calls. The fake remains the deterministic generator and is not the instrument that fabricates those metadata cases. T007 also keeps one default-fake validation integration case: `Validate` accepts a default `FakeEmbeddingGenerator`. T019 keeps the overridden-fake rejection and generation integration proof for SC-006: a non-384 or null `reportedDimensions` still generates 384 elements, reports that override, and `Validate` rejects that generator from the report alone.

Homogeneous validation, options, and reported-dimension matrices use `[Theory]` with `[MemberData]` or `[InlineData]`. Suitable matrices are the non-384 integers (T008), the options and argument groups (T017), and the reported-dimension overrides (T019). Absent metadata (T009), a null reported dimension (T010), and a null generator (T011) stay separate behavioral shapes. Tests do not combine those shapes and do not duplicate the generation algorithm.

T018 keeps the triple checkpoint contract: `ThrowIfCancellationRequested` at entry before enumerating the batch, before each item, and before a successful return. The proof uses a test-local instrumented `IEnumerable<string>` and its enumerator, with a test-owned cancellation source and no timing or thread-scheduling dependency. Three deterministic cases: (a) a token cancelled before the call asserts `OperationCanceledException` and that enumeration never starts; (b) cancellation during enumeration between items, when a later `MoveNext()` yields an item, asserts `OperationCanceledException` without advancing to another item or returning a partial batch; (c) cancellation inside the final `MoveNext()` immediately before it returns false asserts `OperationCanceledException` instead of a successful complete or partial batch. Await each `GenerateAsync` task and assert no generated batch is returned. These are observable cancellation outcomes, not direct observations of calls to `ThrowIfCancellationRequested`. Retain inspection of the required checkpoint placement. Ordinary successful-path tests do not prove the pre-return checkpoint. No fake configuration, production hook, wrapper, dependency, or new test file is added. Instrumentation stays in `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`.

`FakeEmbeddingGenerator` is `public sealed`. `Dispose` is a no-op with one narrowly scoped intentional-empty-block justification comment. No other explanatory comment is authorized.

The fake is stateless: immutable reported-dimension configuration, fresh per-item and per-call buffers, static SHA-256 operations, and no mutable shared state. No concurrency test is required. No cache or synchronization abstraction is added.

The `Microsoft.Extensions.AI` allowlist entry is forward-looking for DEV-412. DEV-345 checks the bounded edit and that unrelated dependency rules stay in force. It does not add a feature-folder consumer to exercise the allowance. Approved helper and test locations stay. Existing exception contracts stay.

Property tests are added only if `scripts/run-property-tests.ps1` requires them for the changed code. Pre-PR mutation uses `scripts/run-mutation.ps1` without `since`, on eligible changed Core code, at the configured score and artifact-kill limits. Constant-only code need not have mutants. Do not lower a threshold and do not treat a build as a gate. Approved phase order stays. That order is a sequencing decision. Substitute metadata proofs technically depend on the dimension helper, not on the fake. The default-fake case in T007 and the override proof in T019 are the proofs that use the fake.

### Delivery order

Patron Q3 order, which this plan follows instead of a test-first template default. This order is a sequencing decision. It is not a technical dependency of the NSubstitute metadata proofs.

1. Package pin, Core reference, dimension helper, and allowlist edit.
2. Fake generator, and the Tests.Common package reference only if required.
3. Behavioral and architecture proofs.
4. Scoped verification and pre-PR mutation.

Substitute proofs for matching, mismatched, absent, and null metadata technically depend on the dimension helper. They do not need the fake to fabricate a reported dimension. The default-fake validation integration in T007 and the overridden-fake rejection and generation proof in T019 are the proofs that need the fake.

## Out of scope

DEV-412 runtime adapter and host registration, DEV-349 real-embedding check, DEV-413 vector schema, HTTP and OpenAPI and generated TypeScript, frontend, `Features:LocalPlay`, secrets, `Process.Start`, new projects or layers or wrappers, constitution edits, ADRs, glossary edits, and unrelated cleanup.
