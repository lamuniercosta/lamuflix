# Research: DEV-345

Phase 0 records decisions already closed by Patron. The ten drafting gaps are closed in `brief.md` (commit `63d9fff`) from `CONCLUSIONS.md` at `9c5e14c`. `recon-DEV-345:74-90` confirms service resolution and metadata. `recon-DEV-345:64-67` supplies test conventions. No new gap is open.

## Package

- **Decision**: `Microsoft.Extensions.AI.Abstractions` version `10.11.0`, pinned in `Directory.Packages.props` and referenced by Core.
- **Rationale**: Ticket Scope 1 names this package and says it is absent today. Patron opening ruling selects this version. Recon lines 20–23 confirm it is not yet in CPM or Core.
- **Alternatives considered**: The full `Microsoft.Extensions.AI` package. Rejected. The OllamaSharp adapter is DEV-412, and Abstractions is enough for the port.

## Port shape

- **Decision**: Use `IEmbeddingGenerator<string, Embedding<float>>` directly. No Core interface and no wrapper.
- **Rationale**: Ticket Scope 1 and constitution lines 138–140 place this abstraction next to `TimeProvider`. A new named port in `Core/Ports` would hit the architecture whitelist and need an ADR (recon lines 35–38).
- **Alternatives considered**: A local `Core.Ports` wrapper. Rejected by the opening ruling and by Q3.

## Dimension check

- **Decision**: `public const int Value = 384` and `public static void Validate(IEmbeddingGenerator<string, Embedding<float>> generator)` in `src/LamuFlix.Core/Ports/EmbeddingDimensions.cs`. Accept only `DefaultModelDimensions == 384`. `InvalidOperationException` for absent metadata, a null dimension, or any other integer. `ArgumentNullException` for a null generator. No generation probe and no Api or Work registration.
- **Rationale**: Ticket AC2 and Scope 3. Q1 supersedes any check that would generate a vector to learn the length. The `const` follows the mutation static-initialization rule cited in the opening ruling.
- **Alternatives considered**: Probe the generated vector length. Rejected by Q1.

## Test generator

- **Decision**: Sealed `FakeEmbeddingGenerator` in `LamuFlix.Tests.Common`. Constructor `FakeEmbeddingGenerator(int? reportedDimensions = EmbeddingDimensions.Value)` builds `new EmbeddingGeneratorMetadata(defaultModelDimensions: reportedDimensions)` with `ProviderName`, `ProviderUri`, and `DefaultModelId` null. Expansion is 48 independent SHA-256 blocks, counters `0u` through `47u`, four big-endian counter bytes, eight big-endian words per digest, `(word / 2147483648.0) - 1.0`, L2 norm in `double`, then `float`. A zero norm returns `[1f, 0f, ...]`. Nonzero means not all zeros. Unit length is `Math.Abs(norm - 1.0) <= 1e-6`. Null batch or element throws `ArgumentNullException`. A non-null `options.Dimensions` other than 384 throws `ArgumentOutOfRangeException`. Cancellation is checked before the batch, before each item, and before a successful return, and never returns a partial batch. `GetService` throws `ArgumentNullException` for a null `serviceType`, returns null for a non-null `serviceKey`, and with a null key matches `typeof(EmbeddingGeneratorMetadata)` exactly before `serviceType.IsInstanceOfType(this)`.
- **Rationale**: Ticket Scope 2 and AC1–AC2, constitution line 346, Q2, and brief Drafting gap decisions 1–8. Recon lines 74–90 confirm decisions 5 and 6 and the exact-type-before-self order. The `IsInstanceOfType(metadata)` alternative was not selected.
- **Alternatives considered**: A model-backed generator in tests. Rejected. Scope 2 exists so tests need no model download. DEV-349 owns the real-embedding check. Metadata lookup by `IsInstanceOfType(metadata)` was the other permitted alternative. Patron and the brief select exact type equality first.

## Architecture allowlist

- **Decision**: Add `Microsoft.Extensions.AI` to `AllowedFeatureDependencies`.
- **Rationale**: Recon lines 38 and 56: a feature type that uses the abstraction fails the current allowlist. The opening ruling makes the edit gate-forced. The port whitelist is unchanged because the interface is not declared in Core.
- **Alternatives considered**: Leave the allowlist and hide the type behind a Core port. That is the wrapper Q3 rejects.

## Exclusions

- **Decision**: No DEV-412 adapter, no DEV-349 real-embedding check, no DEV-413 schema, no HTTP or OpenAPI change, no constitution edit, no ADR, no glossary edit.
- **Rationale**: Ticket Scope 3, PRODUCT sections 2–5, and Q3.
- **Alternatives considered**: None accepted. Zero owner checkboxes remain.

## Drafting gaps (resolved)

All ten are closed. No placeholder remains.

1. Expansion: UTF-8 bytes, counters `0u`–`47u`, four big-endian counter bytes, independent SHA-256, eight big-endian words, `(word / 2147483648.0) - 1.0`, normalize in `double`, cast to `float`, zero-norm fallback `[1f, 0f, ...]`.
2. Nonzero: the vector is not all zeros. Individual zero coordinates are allowed.
3. Unit tolerance: `Math.Abs(norm - 1.0) <= 1e-6`, norm computed in `double` over the returned floats.
4. Generation exceptions: `ArgumentNullException` for a null batch or null element; `ArgumentOutOfRangeException` for a non-null `options.Dimensions` other than 384.
5. `GetService`: `ArgumentNullException` for a null `serviceType`; null for a non-null `serviceKey`; with a null key, exact `typeof(EmbeddingGeneratorMetadata)` first, then `IsInstanceOfType(this)`, else null.
6. Metadata: `FakeEmbeddingGenerator(int? reportedDimensions = EmbeddingDimensions.Value)` and `new EmbeddingGeneratorMetadata(defaultModelDimensions: reportedDimensions)`. Other metadata properties are null. Overrides do not change generated length.
7. Core members: `public const int Value = 384` and `public static void Validate(...)`.
8. Cancellation: `ThrowIfCancellationRequested` before the batch, before each item, and before success. `OperationCanceledException`. No partial batch.
9. Test paths stay at the UnitTests project root. Namespace for both files is `LamuFlix.UnitTests`. Classes are `public sealed`. Shouldly assertions. xUnit comes from global usings. Root placement is the Patron-approved spec choice. Recon did not find existing embedding tests, and it does not require a `Ports` folder that mirrors production.
10. `CLAUDE.md` stays unchanged. `.specify/feature.json` pins `specs/DEV-345`.
