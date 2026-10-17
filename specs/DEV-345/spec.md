# Feature Specification: Embedding port and deterministic test generator

**Feature Branch**: `feature/345-spec`

**Created**: 2026-10-17

**Status**: Draft

**Input**: User description: "DEV-345 — Define the embedding port at a fixed length of 384 and a deterministic test generator so tests need no model download. Runtime adapter and real-embedding check stay out of scope."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fixed embedding length checked before use (Priority: P1)

A maintainer can depend on one embedding port for text whose length contract is 384. Before any production embedding work is wired, startup validation accepts a generator only when that generator reports dimension 384. A generator that reports another dimension, reports none, or is absent is rejected. Validation uses the reported dimension only. It does not create a vector to discover the length, does not download a model, and does not contact a service.

**Why this priority**: Later recommendation work and the deferred runtime adapter both depend on one agreed vector length. A generator that reports a different length must fail closed before any vector is trusted.

**Independent Test**: Present generators that differ only in the dimension they report. A report of 384 is accepted. Any other integer, a missing report, an unknown (null) dimension, and a missing generator are rejected. Validation does not request a vector. Those report differences are configured metadata on the generator under test. The shared test generator is the integration generator for a default report of 384, and for a configured non-384 or unknown report that still yields length-384 vectors which validation rejects from the report alone.

**Acceptance Scenarios**:

1. **Given** a generator that reports dimension 384, **When** startup validation runs, **Then** the generator is accepted and no vector is requested.
2. **Given** a generator that reports a dimension other than 384, **When** startup validation runs, **Then** the generator is rejected and no vector is requested.
3. **Given** a generator that supplies no dimension report, **When** startup validation runs, **Then** the generator is rejected and no vector is requested.
4. **Given** a generator whose reported dimension is unknown (null), **When** startup validation runs, **Then** the generator is rejected and no vector is requested.
5. **Given** no generator, **When** startup validation runs, **Then** the call is rejected and no vector is requested.
6. **Given** this check, **When** validation runs, **Then** no model is downloaded and no service is contacted.

---

### User Story 2 - Deterministic vectors without a model (Priority: P2)

A test author can turn text into 384-element vectors without downloading a model or contacting a service. The same text always yields the same vector, including when a second test generator is used. Representative different texts yield different vectors. Each vector is finite, not all zeros, and within one millionth of unit length. Tests do not claim that no two texts can ever collide.

**Why this priority**: Unit and integration tests need stable vectors before a runtime model exists. The length contract from Story 1 is what those vectors must satisfy.

**Independent Test**: Generate vectors for repeated texts, for texts that differ only by case or whitespace, for batches that contain duplicates, and for the empty and missing-input cases. No model, network, or database is required.

**Acceptance Scenarios**:

1. **Given** the same text, **When** two generations run, including on two separate test generators, **Then** the vectors are equal and each has length 384.
2. **Given** representative distinct texts, including texts that differ only by case or by whitespace, **When** vectors are generated, **Then** those vectors differ. The proof does not claim hash collisions are impossible.
3. **Given** any accepted text, **When** a vector is generated, **Then** it has 384 finite elements, is not the all-zero vector (individual zero coordinates are allowed), and its Euclidean norm, computed in double precision over the returned coordinates, satisfies absolute distance from 1 of at most one millionth.
4. **Given** a batch that includes duplicate texts, **When** vectors are generated, **Then** output order matches input order, duplicate inputs produce equal vectors, and each item uses its own buffer. A second call also uses new buffers.
5. **Given** an empty batch, **When** generation runs, **Then** the result contains no vectors.
6. **Given** an empty text, **When** generation runs, **Then** one 384-element vector is returned.
7. **Given** a missing batch or a missing element, **When** generation runs, **Then** the request is rejected as a missing argument. **Given** a requested length that is present and is not 384, **When** generation runs, **Then** the request is rejected as out of range.
8. **Given** a requested length of 384, or no requested length, **When** generation runs, **Then** the request is accepted and every vector has length 384.
9. **Given** a cancelled request, **When** generation runs, **Then** cancellation is checked before the batch is read, before each item, and before a successful return. The call fails as cancelled and returns no partial batch.
10. **Given** a test generator configured to report a dimension other than 384, or to report an unknown dimension, **When** generation runs, **Then** every vector still has length 384, and the reported dimension stays the configured one so Story 1 can reject it.
11. **Given** a service request, **When** the service type is missing, **Then** the request is rejected as a missing argument. **Given** a service key, **When** the key is present, **Then** the result is empty. **Given** no service key, **When** the request names the dimension-report type exactly, **Then** the generator returns that report. **When** the request type is the generator itself, **Then** the generator returns itself. **When** the request names anything else, **Then** the result is empty. Disposal does nothing. No separate configuration facility is added. The dimension-report match is exact type equality, checked before the generator-instance check.

---

### Edge Cases

- Reported dimension is an integer other than 384: startup validation rejects the generator and does not generate a probe vector.
- Dimension report is absent: startup validation rejects the generator.
- Reported dimension is unknown (null): startup validation rejects the generator.
- Generator is absent: startup validation rejects the call.
- Same text on two generator instances: vectors are equal.
- Texts that differ only by case, or only by whitespace: treated as different inputs; representative cases produce different vectors.
- Batch order, including duplicates: preserved; buffers are not shared across items or across calls.
- Empty batch: no vectors. Empty text: one vector.
- Missing batch or missing element: rejected as a missing argument. A requested length other than 384: rejected as out of range. A requested length of 384, or no requested length, is accepted.
- Cancellation: checked before the batch is read, before each item, and before a successful return. A cancelled batch returns nothing.
- Test-only reported dimension of another integer or unknown: generated vectors still have length 384. Provider name, provider address, and model id on that report stay empty.
- Service type missing: rejected as a missing argument. A present service key, or any type that is neither the dimension report nor the generator itself: empty result. Exact dimension-report type match wins over the generator-instance check.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The product MUST expose one text embedding port whose vector length is fixed at 384, named `EmbeddingDimensions.Value`. Callers MUST use `IEmbeddingGenerator<string, Embedding<float>>` directly. The product MUST NOT add a local stand-in port or wrapper around it.
- **FR-002**: `EmbeddingDimensions.Validate` MUST accept a generator only when the dimension it reports is 384.
- **FR-003**: `EmbeddingDimensions.Validate` MUST reject a generator that reports any other integer, that supplies no dimension report, or whose reported dimension is unknown, by throwing `InvalidOperationException`. It MUST reject an absent generator by throwing `ArgumentNullException`.
- **FR-004**: Startup validation MUST decide from the reported dimension alone. It MUST NOT generate a vector, download a model, contact a service, or register a production generator.
- **FR-005**: Feature code MUST be allowed to depend on the external embedding abstraction. The allowance is forward-looking for DEV-412. This feature checks that bounded allowance and MUST NOT add a feature-folder consumer to exercise it. Unrelated dependency rules MUST stay in force.
- **FR-006**: The shared test suite MUST provide one deterministic test generator for that same port, so tests need no model download.
- **FR-007**: The test generator MUST derive each vector from the exact input text. It MUST NOT fold case or whitespace, and it MUST NOT use nondeterministic or process-randomized hashing.
- **FR-008**: Each accepted input MUST produce a 384-element vector that is finite, is not all zeros (individual zero coordinates are allowed), and whose Euclidean norm computed in double precision over the returned coordinates satisfies `Math.Abs(norm - 1.0) <= 1e-6`.
- **FR-009**: Equal inputs MUST produce equal vectors, including across separate test-generator instances. Representative distinct inputs MUST produce different vectors. Tests MUST NOT claim that a collision is mathematically impossible.
- **FR-010**: Batch results MUST preserve input order and duplicate inputs. Each item and each call MUST receive its own output buffer.
- **FR-011**: An empty batch MUST return no vectors. An empty text MUST be accepted and MUST return one vector.
- **FR-012**: A missing batch and a missing element MUST be rejected with `ArgumentNullException`. A requested length that is present and is not 384 MUST be rejected with `ArgumentOutOfRangeException`. A requested length of 384, and a request that names no length, MUST be accepted.
- **FR-013**: The test generator MUST call `ThrowIfCancellationRequested` before reading the batch, before each item, and before returning successfully. Cancellation MUST throw `OperationCanceledException` and MUST NOT return a partial batch.
- **FR-014**: The test generator MUST always emit 384 elements, including when a test-only setting reports another dimension or an unknown dimension. That setting exists only so startup validation can be shown to reject it. The reported dimension MUST remain the configured value.
- **FR-015**: `GetService` MUST throw `ArgumentNullException` when the service type is null. It MUST return null when the service key is not null. With a null key it MUST return metadata when the service type equals `typeof(EmbeddingGeneratorMetadata)`, then return this generator when `serviceType.IsInstanceOfType(this)`, and otherwise return null. That exact metadata-type check is the selected alternative; an instance check against the metadata object is not the rule. Disposal MUST do nothing. The test generator MUST NOT add a configuration facility. Metadata other than the reported dimension (`ProviderName`, `ProviderUri`, `DefaultModelId`) MUST be null.
- **FR-016**: The product MUST NOT add a runtime embedding adapter, a real-model embedding check, a vector schema or migration, an HTTP route, a request or response shape, generated client types, a frontend change, local-play behavior, secrets, or a process launch. It MUST NOT add a project, a layer, a wrapper, a constitution change, an architecture decision record, or a glossary term.

### Key Entities

- **Embedding vector**: The 384-element numeric result for one text. Test vectors are finite, not all zeros, and within `1e-6` of unit length. Production storage of vectors is outside this feature.
- **Dimension report**: The length a generator says it uses. Startup validation reads this report and does not derive the length by generating a vector. The test generator can report 384, another integer, or an unknown dimension without changing the length of vectors it generates.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For every text tried twice, including on two separate test generators, the two vectors are equal.
- **SC-002**: Every returned vector has length 384.
- **SC-003**: Startup validation accepts a report of 384 and rejects every other integer, a missing report, an unknown dimension, and an absent generator. Across those checks, the number of generated probe vectors, model downloads, and service calls is zero.
- **SC-004**: A documented set of representative distinct texts, including pairs that differ only by case and pairs that differ only by whitespace, produces pairwise different vectors. The result does not state that no collision can exist.
- **SC-005**: Every accepted test vector is finite, is not all zeros, and satisfies `Math.Abs(norm - 1.0) <= 1e-6` when the norm is computed in double precision over the returned coordinates. Empty batches return no vectors. Empty text returns one vector. Missing batches and missing elements throw `ArgumentNullException`. Requested lengths other than 384 throw `ArgumentOutOfRangeException`. Cancellation throws `OperationCanceledException` with no partial batch.
- **SC-006**: A test generator configured to report a non-384 or unknown dimension still returns only length-384 vectors, and startup validation rejects that generator from the report alone.

## Assumptions

- `[assumed]` The test generator encodes the exact input with UTF-8, appends four big-endian counter bytes for counters 0 through 47 inclusive, hashes each block independently with SHA-256, reads each digest as eight big-endian 32-bit words in counter order then word order, maps each word with `(word / 2147483648.0) - 1.0`, normalizes in double precision, and casts each coordinate to float. A zero norm returns `[1f, 0f, ...]`. No case or whitespace folding. Logged in `ASSUMPTIONS.md`.
- `[assumed]` Nonzero means the vector is not all zeros. Unit length means `Math.Abs(norm - 1.0) <= 1e-6` on the Euclidean norm of the returned floats, computed in double precision. Logged in `ASSUMPTIONS.md`.
- `[assumed]` `EmbeddingDimensions` exposes `public const int Value = 384` and `public static void Validate(IEmbeddingGenerator<string, Embedding<float>> generator)`. The fake is constructed as `FakeEmbeddingGenerator(int? reportedDimensions = EmbeddingDimensions.Value)` and builds `new EmbeddingGeneratorMetadata(defaultModelDimensions: reportedDimensions)`. Logged in `ASSUMPTIONS.md`.
- Epic 9 (recommendations, the runtime model, and vector storage) stays deferred at product level. This feature is only the port and the test generator.
- No production host registration ships with this feature. Wiring a live generator remains a later ticket.
- The test generator is stateless: its reported dimension is immutable configuration, each item and each call uses a fresh buffer, and it keeps no mutable shared state. This feature adds no concurrency proof.
- `CLAUDE.md` stays unchanged. It is outside the frozen file set. `.specify/feature.json` already pins `specs/DEV-345`.
