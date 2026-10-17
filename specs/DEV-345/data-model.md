# Data model: DEV-345

No persisted entity, table, or migration. DEV-413 owns vector storage. The objects below exist only in memory for the port contract and the test generator.

## Dimension contract

| Field | Rule |
|---|---|
| Expected length | `EmbeddingDimensions.Value`, `public const int` 384, in `LamuFlix.Core.Ports` |
| Checker | `public static void Validate(IEmbeddingGenerator<string, Embedding<float>> generator)` |
| Reported length | `EmbeddingGeneratorMetadata.DefaultModelDimensions` |
| Accept | Reported length is 384 |
| Reject | Metadata absent, reported length null, or reported length any other integer: `InvalidOperationException` |
| Reject | Generator is null: `ArgumentNullException` |
| Probe | Validation does not generate a vector |

There is no state machine. The helper is a pure check of the report.

## Embedding vector (test generator)

| Field | Rule |
|---|---|
| Length | Always 384 elements, including when the reported length is overridden |
| Values | Finite. Not all zeros. Individual zero coordinates are allowed |
| Scale | Euclidean norm of the returned floats, computed in `double`, satisfies `Math.Abs(norm - 1.0) <= 1e-6` |
| Read surface | `Embedding<float>.Vector` (`ReadOnlyMemory<float>`) |
| Identity | Equal exact UTF-8 inputs produce equal vectors, including across instances |
| Distinctness | Representative different inputs, including case-only and whitespace-only differences, produce different vectors. Collisions are not claimed to be impossible |
| Buffer | New array per item and per call |
| Derivation | `Encoding.UTF8.GetBytes`. Counters `0u` through `47u`. Four big-endian counter bytes per block (`BinaryPrimitives.WriteUInt32BigEndian`). One `SHA256.HashData` per block. Eight big-endian `uint` words per digest (`BinaryPrimitives.ReadUInt32BigEndian`), in counter order then word order. `raw = (word / 2147483648.0) - 1.0`. L2-normalize in `double`, then cast to `float`. Zero norm returns `[1f, 0f, ...]` |

No case folding and no whitespace folding. No `Random` and no `string.GetHashCode()`.

## Generation request

| Input | Result |
|---|---|
| Empty batch | Empty `GeneratedEmbeddings<Embedding<float>>` |
| Empty string | One 384-element vector |
| Null batch | `ArgumentNullException` |
| Null element | `ArgumentNullException` |
| `options` null, or `Dimensions` null, or `Dimensions` 384 | Accepted |
| `options.Dimensions` non-null and not 384 | `ArgumentOutOfRangeException` |
| Cancelled token | `ThrowIfCancellationRequested` before the batch, before each item, and before a successful return. `OperationCanceledException`. No partial batch |
| Batch with duplicates | Same order as the input; equal inputs stay equal; buffers are distinct |

## Dimension report on the fake

| Setting | Generated length | Reported `DefaultModelDimensions` |
|---|---|---|
| Default | 384 | 384 |
| Constructor override to another integer | 384 | That integer |
| Constructor override to null | 384 | null |

`ProviderName`, `ProviderUri`, and `DefaultModelId` are null. The constructor is `FakeEmbeddingGenerator(int? reportedDimensions = EmbeddingDimensions.Value)` and `new EmbeddingGeneratorMetadata(defaultModelDimensions: reportedDimensions)`.

## Service resolution on the fake

| Request | Service key | Result |
|---|---|---|
| `serviceType` null | any | `ArgumentNullException` |
| Any type | non-null | null |
| `serviceType == typeof(EmbeddingGeneratorMetadata)` | null | The report above. This exact match is checked first |
| `serviceType.IsInstanceOfType(this)` | null | This instance |
| Any other type | null | null |

Do not treat `IsInstanceOfType` on the metadata object as the metadata match. Recon lines 74–90 confirm exact type equality before self resolution.

`Dispose` does nothing. There is no configuration object beyond the reported-dimension override.

## Relationships

The validation helper reads the fake's report in tests. It does not reference the fake from Core. Production code does not register either type in this feature.
