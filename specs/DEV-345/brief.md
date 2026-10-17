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

## Phase 2 step 6 — Three-axis plan challenge adjudication

Keel adjudicated all three required reports on 2026-10-17: `findings-DEV-345-Sentry:8-24`, `findings-DEV-345-Ledger:13-51`, and `findings-DEV-345-Compass:9-31`. Authority: Patron ruling at `0b72072`, `CONCLUSIONS.md:62-75`, read back by Keel. Evidence was limited to the named DEV-345 note, the three reports, and `specs/DEV-345/`, then the Conductor-authorized bounded answer `recon-DEV-345-2:7-22` to close M2; no production-code reconnaissance or new measurements were performed.

The reports contain 0 Critical, 0 High, 3 Medium, and 8 Low findings. All 11 have an explicit disposition below. Seven are accepted with the bounded adjustments stated; four proposed changes are rejected, including S6 recorded as noted with no ticket. These are plan decisions, not implementation verification results.

| Axis / ID | Severity | Ruling | Evidence and disposition |
|---|---|---|---|
| Sentry R-1 | Low | Accept | Record the statelessness rationale rather than add a concurrency test: immutable reported-dimension configuration, fresh per-item/per-call buffers, static SHA-256 operations, and no mutable shared state. This follows Q2 and `plan.md:137-140`; no cache or synchronization abstraction is added. |
| Sentry R-2 | Low | Reject | `spec.md` FR-012/FR-013 and drafting decisions 4/8 do not promise exception precedence for an already-cancelled call with a null batch. Do not introduce a null-before-cancellation requirement. Preserve isolated exception tests and all three cancellation checkpoints. |
| Ledger S1 | Medium | Reject | Drafting decision 9 explicitly approves both root-level test paths. `plan.md:94` and `tasks.md:20` use the matching project-root namespace. The supplied evidence does not establish a mandatory production-namespace mirror that overrides this ruling. No test relocation. |
| Ledger S2 | Low | Reject | Q1 and drafting decision 7 explicitly place the constant/validation helper beside the approved external port in `Core/Ports`. This helper declares no additional port or layer. No relocation to Domain. |
| Ledger S3 | Low | Accept | Make homogeneous case matrices parameterized with `[Theory]` and `[MemberData]`/`[InlineData]`, following the standard cited in the Ledger report and Patron ruling. Apply to suitable T008-T010, T017, and T019 cases; keep distinct behavioral shapes separate. |
| Ledger S4 | Low | Accept | State `public sealed class FakeEmbeddingGenerator` in plan and T006 so the existing UnitTests project can consume it across project boundaries. No visibility workaround or new project. |
| Ledger S5 | Low | Accept | Require one narrowly scoped intentional-empty-block justification inside no-op `Dispose`, under the existing comment exemption. Disposal behavior remains unchanged. |
| Ledger S6 | Low | Reject follow-up ticket; noted | The older Core dependency table is already reconciled for this ticket by Q3 and `plan.md:39,98`. Patron rules noted, no ticket. Do not edit the constitution or ask Rigger to file this Low documentation observation. |
| Compass M1 | Medium | Accept | `tasks.md:78-81` lacks an explicit instrument for asserting zero generation calls. Use existing NSubstitute interface substitutes for matching, mismatched, absent, and null metadata, and explicitly verify no `GenerateAsync` call. Keep the sealed fake unchanged. Retain fake integration in T007/T019 for SC-006. |
| Compass M2 | Medium | Accept proof-design gap; reject contract weakening | T018 must assert observable cancellation failures and no returned partial batch, rather than directly observing calls to `ThrowIfCancellationRequested`. Preserve entry, per-item, and pre-return checkpoints from drafting decision 8 and FR-013. Use a test-local instrumented `IEnumerable<string>` as confirmed by `recon-DEV-345-2:7-22`, with the three deterministic cancellation cases in fix item 2 below. Ordinary successful-path tests alone do not prove the pre-return cancellation checkpoint. |
| Compass L1 | Low | Accept | Annotate `plan.md:117` and T004/T022: the allowlist entry is forward-looking for DEV-412. DEV-345 checks the bounded edit and preservation of unrelated rules; it must not invent feature-folder consumers to exercise the allowance. |

### Required drafting adjustments — one numbered fix list

These decisions supersede conflicting proof-design wording above while preserving the frozen delivery scope, file paths, phase order, and two-round cap. Quill owns plan/tasks drafting; Keel has not edited those artifacts in this adjudication.

1. **M1:** Update the plan verification section and T007-T010 to specify NSubstitute interface substitutes, configured metadata, and zero recorded generation calls. Keep a default-fake validation integration case in T007 and the overridden-fake rejection/generation integration proof in T019. Correct the task introduction, Phase 3 checkpoint, Phase 5 goal, dependency explanations, and MVP narrative wherever they say the fake is necessary to fabricate metadata cases. Preserve the approved phase order; distinguish that sequencing decision from a technical dependency of the substitute proofs.
2. **M2:** Keep the triple checkpoint contract unchanged. T018 uses a test-local instrumented `IEnumerable<string>` and its enumerator, with a test-owned cancellation source and no timing or thread-scheduling dependency. Cover (a) a token cancelled before the call, asserting `OperationCanceledException` and that enumeration never starts; (b) cancellation during enumeration between items, when a later `MoveNext()` yields an item, asserting `OperationCanceledException` without advancing to another item or returning a partial batch; and (c) cancellation inside the final `MoveNext()` immediately before it returns false, asserting `OperationCanceledException` instead of a successful complete or partial batch. Await each `GenerateAsync` task and assert no generated batch is returned. These are observable cancellation outcomes, not direct observations of calls to `ThrowIfCancellationRequested`; retain inspection of the required checkpoint placement. No fake configuration, production hook, wrapper, dependency, or new test file is added: keep instrumentation local to `tests/LamuFlix.UnitTests/FakeEmbeddingGeneratorTests.cs`.
3. **S3:** Add explicit parameterized-test guidance for homogeneous validation, options, and reported-dimension matrices, without combining different behavioral shapes or duplicating the generation algorithm in tests.
4. **S4/S5:** State public sealed accessibility and the single allowed empty-block justification for no-op disposal in the plan and T006. No other explanatory comments are authorized.
5. **R-1/L1:** Record the statelessness rationale and the forward-looking allowlist purpose in the relevant plan/task sections. Add no concurrency test requirement and no feature consumer. Retain the approved helper/test locations and existing exception contracts. S6 needs neither a constitution edit nor a follow-up ticket.

### Outcome and bounded recon closure

Adjudication is complete for all 11 findings, including finalized M2 proof wording; no adjudication question or recon gap remains. Drafting remediation and plan freeze are not complete. No Critical/High finding blocks under the closing bar, but accepted drafting changes must be applied and checked before claiming the adjusted plan is frozen. Gate 1 remains closed; no build, gate, review-clearance, or merge claim is made.

The former bounded signature question is closed by the Conductor-authorized note `recon-DEV-345-2:7-22`. Its package-source evidence at tag `v10.11.0` gives the verbatim declaration:

```csharp
Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
    IEnumerable<TInput> values,
    EmbeddingGenerationOptions? options = null,
    CancellationToken cancellationToken = default);
```

For this fake, `TInput` is `string`, so the parameter is `IEnumerable<string> values`; there is no use-site `in` modifier. This confirms the test-local instrumented-sequence approach already conditionally approved by Patron at `CONCLUSIONS.md:74`; it changes no scope or contract. Keel read only the newly authorized note and this brief during closure, without opening package sources or other recon notes. No owner structural checkbox is introduced. Quill can now apply the complete five-item fix list above before Keel checks the resulting plan/tasks for freeze.

## Phase 2 step 6 — Plan freeze receipt (2026-10-17)

Status: FROZEN. Keel checked Quill's adjusted `spec.md`, `plan.md`, and `tasks.md` against all five required drafting adjustments above. All five are incorporated consistently; no further fix round or decision change is required. This receipt supersedes the earlier pending-remediation and pending-freeze status, while retaining the adjudication history.

| Fix | Verified evidence | Freeze disposition |
|---|---|---|
| 1 — M1 | `plan.md:152,164-175`; `tasks.md:7-9,53,74-82,135-141,175-184`; `spec.md:19,23-27,78,103,106` | NSubstitute metadata cases each assert zero recorded generation calls. Default-fake validation stays in T007 and overridden-fake rejection/generation stays in T019. Technical dependencies are distinguished from approved phase sequencing. |
| 2 — M2 | `plan.md:144,156`; `tasks.md:100` (T018); `spec.md:50,67,87,105` | Triple checkpoint contract preserved. Test-local sequence/enumerator instrumentation produces deterministic entry, between-item, and final-MoveNext cancellation outcomes. Each task is awaited, cancellation throws, and no complete or partial batch returns. Placement inspection is retained; no production hook or new file. |
| 3 — S3 | `plan.md:154`; `tasks.md:79-82,99,101,201` | Homogeneous integer/options/override matrices use theories and data rows. Absent metadata, null dimension, and null generator remain distinct behavioral shapes. No algorithm duplication. |
| 4 — S4/S5 | `plan.md:136,146,158`; `tasks.md:66,200`; `spec.md:89` | Fake is public sealed across project boundaries. No-op Dispose has one intentional-empty-block justification; no other explanatory comment is authorized. Disposal contract stays unchanged. |
| 5 — R-1/L1 | `plan.md:29,117,141,160-162`; `tasks.md:51,66,113,198-202`; `spec.md:79,90,115` | Statelessness rationale and forward-looking DEV-412 allowlist purpose recorded. No concurrency-test requirement, feature consumer, constitution edit, or S6 follow-up ticket. Approved paths and exception contracts retained. |

The spec retains behavior and scope; the plan and tasks carry the implementation/proof details. All 16 functional requirements and six success criteria remain represented by the 32-task sequence. No delivery scope, dependency choice, public contract, file boundary, review cap, or owner ruling changed in this check. No owner structural checkbox is introduced.

Evidence boundary for this freeze check: `F:/Dev/LamuFlix.worktrees/feature-345-spec/specs/DEV-345/` and canvas note `DEV-345`. Prior challenger and recon conclusions are used only as already persisted in this brief; no challenger/recon note, production file, package source, or external project document was opened for this check. This was a bounded artifact consistency check, not a fresh full skill/script analysis or implementation verification.

Gate 1 remains closed pending the user's spec approval/merge. This plan freeze authorizes no implementation, production gate PASS, review clearance, merge-bar signoff, or merge. No implementation tests were run. The planning diff whitespace check is recorded separately from implementation gates.
