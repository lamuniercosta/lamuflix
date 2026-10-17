# DEV-345 — Embedding port and deterministic test generator

Grill closed: 2026-10-17, Keel with Patron. Shared understanding explicitly confirmed in Patron Q3. Worktree: `F:/Dev/LamuFlix.worktrees/feature-345-spec`; branch: `feature/345-spec`. Recon baseline: `b02a1e9f4bcba8ad1932b15c38bf9bdf2b6a98bc`.

This brief is the decision authority for Quill drafting `spec.md`, `plan.md`, and `tasks.md`. It is not Gate 1 approval, build authorization, review clearance, or permission to merge.

## Sources and acceptance contract

- Facts: canvas `recon-DEV-345:1-56` and `DEV-345:1-30`. No additional code reconnaissance was performed by Keel.
- Product authority: `specs/PRODUCT.md`, especially sections 2–5.
- Full ticket excerpts retrieved and cited by Patron are persisted in `CONCLUSIONS.md` Q1. These supplement the abbreviated intake note: Scope 1 names the external Microsoft.Extensions.AI port, the abstractions package, and 384 dimensions; Scope 2 names the deterministic hash-based fake in Tests.Common; Scope 3 excludes DEV-412 and DEV-349.
- AC1: the fake returns the same vector for the same input and different vectors for different inputs. Prove representative distinct inputs; do not promise mathematical hash collision impossibility.
- AC2: every returned vector has length 384, and startup validation rejects a generator reporting another dimension.
- Patron rulings and their cited bases: `CONCLUSIONS.md` opening and Q1–Q3. Taste and edge-case assumptions: `ASSUMPTIONS.md`.

## Every grill answer

### Opening — package, port, and scope

Keel opened `/grill-with-docs DEV-345`. Patron selected `Microsoft.Extensions.AI.Abstractions` version `10.11.0`, centrally managed in `Directory.Packages.props` and referenced by Core. Use the external `IEmbeddingGenerator<string, Embedding<float>>` directly; do not declare a local interface or wrapper. Add the `Microsoft.Extensions.AI` namespace to the feature-dependency allowlist in ArchitectureTests as a gate-forced edit.

The Core contract fixes dimensions at a `const int` value of 384. The fake belongs in existing Tests.Common. Runtime OllamaSharp integration, real-embedding checks, database changes, and HTTP/OpenAPI changes are excluded. No owner checkbox is required.

Basis: ticket Scope 1–3 and AC1–2; constitution lines 138–140 and 346 as cited in recon; recon lines 20–23 and 35–38; PRODUCT sections 2–5.

### Q1 — startup validation without a runtime adapter

Keel requested the exact ticket excerpt and recommended a Core startup-compatible validation seam without production registration or a model probe. Patron accepted: place a dimension constant and validation helper in `src/LamuFlix.Core/Ports/EmbeddingDimensions.cs`. Inspect generator metadata through the external interface; require reported `DefaultModelDimensions == 384`. Reject absent metadata, null reported dimensions, or any different integer with `InvalidOperationException`; reject a null generator with `ArgumentNullException`.

Exercise this seam with fake generators in tests. Do not call generation to discover dimensions, download a model, contact a service, or register a production generator in Api/Work. Production host wiring remains DEV-412. This supersedes the opening ruling's possible generated-vector-length probe: reported metadata alone drives startup validation.

Basis: exact ticket AC2 and exclusions, persisted in `CONCLUSIONS.md` Q1; PRODUCT sections 2–3.

### Q2 — bounded fake behavior

Keel proposed the deterministic algorithm and edge cases; Patron accepted all of the following:

- A sealed `FakeEmbeddingGenerator` implements the external interface in `LamuFlix.Tests.Common`.
- Hash exact UTF-8 input using SHA-256 counter expansion and L2-normalize to unit length. No case/whitespace normalization, `Random`, or `string.GetHashCode()`. Produce finite, nonzero 384-element vectors.
- Allocate independent output buffers per item and call. Preserve batch order and duplicates. Equal inputs return equal values, including across separate fake instances.
- Accept empty batches and empty strings. Reject null batches, null elements, and requested non-null `options.Dimensions` values other than 384. Honor cancellation.
- Always generate exactly 384 elements, even when test-only constructor configuration reports another dimension or null. The override exists solely to test startup rejection.
- Support the external interface's generation and service surface: metadata/self service resolution for a null service key, standard unsupported-service behavior, and no-op disposal. Do not create a configuration framework.
- Test representative distinct strings, including case/whitespace variations, without asserting collision impossibility.

The normalization, raw-input handling, and input/options assumptions are tagged `[assumed]` in `ASSUMPTIONS.md`. Basis: ticket Scope 2 and AC1–2; constitution line 346; deterministic-test rules; PRODUCT sections 2 and 4.

### Q3 — delivery plan and closure

Keel proposed the file set, ordering, verification, and loop terms below. Patron ratified them and explicitly closed the grill. The constitution's explicit external-port exception authorizes this dependency despite its older Core dependency table: no constitution rewrite, new layer, wrapper, or ADR is needed. No new domain vocabulary was introduced, so no glossary change is required.

## Frozen scope and loop discipline

Closing bar for code/ship review: Critical and High findings block closure. Medium and Low findings require explicit adjudication but do not independently block. Applicable failing or unrunnable gates block; opt-outs and scope exclusions retain their separate verdicts. On this size:M task, Keel requires all three Sentry, Ledger, and Compass reports before review adjudication.

Round cap: two review rounds; escalate unresolved blockers to Conductor after the cap. Do not silently run a third round or lower a threshold.

Frozen scope: centrally managed abstractions dependency, direct external embedding port, Core 384-dimension constant and metadata validation helper, deterministic Tests.Common fake, focused unit/property tests as applicable, and the gate-forced architecture allowlist edit; anything else is a follow-up issue, not a finding in this round.

Excluded: runtime adapter/production host wiring (DEV-412), real-embedding check (DEV-349), vector schema/migrations (DEV-413), HTTP routes/DTOs/OpenAPI/generated TypeScript, frontend, LocalPlay, secrets, Process.Start, new projects/layers/wrappers, constitution changes, and unrelated cleanup. No owner escalations remain. The user retains Gate 1 and every merge decision.

## Implementation plan decisions

Approved implementation files:

- `Directory.Packages.props`: pin `Microsoft.Extensions.AI.Abstractions` `10.11.0`.
- `src/LamuFlix.Core/LamuFlix.Core.csproj`: add the central-version package reference.
- `src/LamuFlix.Core/Ports/EmbeddingDimensions.cs`: new constant and validation helper; no custom port declaration.
- `tests/LamuFlix.Tests.Common/FakeEmbeddingGenerator.cs`: new deterministic fake.
- `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`: change only if transitive package flow is insufficient.
- Focused new test files within `tests/LamuFlix.UnitTests/`, following its existing conventions, covering the fake and dimension validation; property tests if required by the applicable gate.
- `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`: only the external namespace allowance required by this contract.

Task ordering after the required planning gates: (1) CPM/Core reference and contract; (2) fake; (3) focused behavioral and architecture proofs; (4) verification and pre-PR mutation. Quill must map each AC and each settled edge case to concrete tasks and tests. Keep mutable code out of static initialization and do not edit generated gate configuration.

## Test strategy and gate expectations

Unit proofs: default/matching metadata accepted; mismatched, missing, unknown, and null-generator validation rejected without invoking generation; vectors have 384 finite elements and unit norm within an appropriate floating-point tolerance; repeated inputs and independent instances agree; representative distinct inputs differ; duplicate batch entries preserve order without sharing mutable buffers; empty/null inputs, dimension options, cancellation, and service/disposal behavior meet Q2.

These tests require no model, network, or database. Architecture tests must permit the approved external abstraction without weakening unrelated dependency rules. Use the repository's existing test/assertion/property libraries.

At implementation verification, run applicable Roslyn, complexity, InspectCode, property-test, vulnerability, formatting, architecture/unit/full-test and diff checks under the configured harness. Pre-PR mutation uses `scripts/run-mutation.ps1` without `since` from the worktree and covers eligible changed production Core code; obey configured score and artifact-kill limits. Constant-only files need not have mutants. Never lower thresholds or substitute a build for a gate.

Preserve `PASS`, `SKIPPED (scope-empty)`, opt-out `SKIP`, mutation `N/A`, and `Could not run` distinctly. No implementation tests or gates were run during this grill; no pass is claimed here. Spec-only PR gates apply to the actual docs diff at that stage.

## Drafting gap decisions — Patron ruling 2026-10-17

Authority: `CONCLUSIONS.md`, section "Phase 2 step 2 — Drafting gaps ruling", and `ASSUMPTIONS.md`, committed by Patron at `9c5e14c`. These ten refinements complete Q1–Q3 without changing frozen delivery scope, loop terms, or owner gates. Patron reports the external API and UnitTests layout verified in that ruling; no additional reconnaissance was performed by Keel.

1. **Exact expansion:** Encode the input with `Encoding.UTF8.GetBytes(input)`. Append exactly four unsigned counter bytes in big-endian order, using counters `0u` through `47u` inclusive. SHA-256 each concatenation independently. Read each digest as eight big-endian unsigned 32-bit words, producing 384 coordinates in counter order and digest-word order. Map each word to `double raw = (word / 2147483648.0) - 1.0`. Sum squares and L2-normalize in `double`, then cast each coordinate to `float`. If the norm is zero, return `[1f, 0f, ...]` instead. No input normalization is added.
2. **Nonzero:** The vector must not be all zeros; individual zero coordinates are allowed.
3. **Unit tolerance:** Compute the Euclidean norm in `double` over the returned `float` coordinates and require `Math.Abs(norm - 1.0) <= 1e-6`.
4. **Generation exceptions:** Throw `ArgumentNullException` for a null batch or any null element; throw `ArgumentOutOfRangeException` for any requested non-null `options.Dimensions` other than 384. Empty batches and empty strings remain accepted.
5. **Service resolution:** Throw `ArgumentNullException` for a null `serviceType`. Return null for a non-null `serviceKey`. With a null key, return metadata for `serviceType == typeof(EmbeddingGeneratorMetadata)`, return this generator when `serviceType.IsInstanceOfType(this)`, and return null otherwise. Exact metadata-type matching is the first alternative explicitly permitted by Patron; use it consistently in the plan and tests. Disposal remains a no-op.
6. **Metadata:** Use `FakeEmbeddingGenerator(int? reportedDimensions = EmbeddingDimensions.Value)` and `new EmbeddingGeneratorMetadata(defaultModelDimensions: reportedDimensions)`. `ProviderName`, `ProviderUri`, and `DefaultModelId` are null; `DefaultModelDimensions` is the supplied override, defaulting to 384. Overrides never alter the generated vector length.
7. **Core members:** `LamuFlix.Core.Ports.EmbeddingDimensions` exposes `public const int Value = 384` and `public static void Validate(IEmbeddingGenerator<string, Embedding<float>> generator)`. Null generator throws `ArgumentNullException`; absent metadata, null reported dimensions, or any reported integer other than 384 throws `InvalidOperationException`. Validation must not generate a probe.
8. **Cancellation:** Call `cancellationToken.ThrowIfCancellationRequested()` at entry before enumerating the batch, before each item, and before returning successfully. Cancellation throws `OperationCanceledException`; a cancelled batch must never return partial results.
9. **Test paths:** Confirm `tests/LamuFlix.UnitTests/EmbeddingDimensionsTests.cs` and `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`. Patron verified existing root-level UnitTests precedents. Follow existing test conventions and libraries.
10. **Spec Kit pointer:** Leave `CLAUDE.md` unchanged because it is outside the frozen file set. The existing `.specify/feature.json` pin identifies `specs/DEV-345`.

Quill must replace all ten open drafting placeholders with these decisions across the planning artifacts and receipt. The assumptions are already tagged in `ASSUMPTIONS.md`. No drafting gap remains open from this exchange; read-only consistency analysis and plan challenge still follow the completed draft.
## Handoff

Patron has persisted and Keel has read back `CONCLUSIONS.md` and `ASSUMPTIONS.md`; the resolved exchanges are recorded before handoff. Quill drafts the three planning artifacts from this brief. Keel then performs read-only speckit analysis and brief-to-plan/tasks comparison; challenger adjudication and plan freeze remain later steps. No production code was written by Keel.
