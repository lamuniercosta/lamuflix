# DEV-336 — Frozen grill and planning brief

## Identity, authority, and status

- Ticket: DEV-336 — Create MovieFeatures value object and map it from persisted movie metadata; parent DEV-288; size:M, no ui tag.
- Worktree: `F:/Dev/LamuFlix.worktrees/feature-336-spec`.
- Branch: `feature/336-spec`.
- Intake/recon HEAD: `06aedfeabc9e26ecc0348c71ca9983a2a8d27a65`, base `origin/main` (intake receipt).
- Facts come exclusively from `recon-DEV-336` in fichario `LamuFlix notes`, with ticket summary in `DEV-336:18`. Missing facts require `needs recon: <question>` to Conductor; do not search beyond that evidence.
- Patron accepted Q1–Q5 and confirmed shared understanding and closure. Full questions, answers, rationale, and cited basis are append-only in `CONCLUSIONS.md`; taste choices are tagged `[assumed]` in `ASSUMPTIONS.md`.
- Grill complete; zero unresolved decisions or owner checkboxes. This is Phase A planning only. No implementation, gate pass, plan freeze, review clearance, Gate 1 approval, or merge is claimed.

## Loop discipline

**Closing bar:** Zero unresolved Critical or High findings. Medium and Low do not block; Patron triages remaining findings under the charter. Out-of-scope Critical/High or broken behaviour goes through Patron and Rigger for a follow-up record; other non-broken observations are `noted, no ticket` in conclusions. Never silently waive or demote a blocker.

**Round cap:** Maximum two review/remediation rounds. Stop and report an unresolved above-bar finding after the cap; do not add a third fix round or relax a gate. Plan drafting/challenge must remain bounded; send Quill one numbered fix list per round.

**Frozen scope:** A sealed Core Recommendations MovieFeatures record, a pure mapper from persisted Movie/MovieMetadata, and focused unit/property tests for the contracts below. No HTTP, infrastructure, persistence, OMDb persistence (DEV-411), recommendation ranking/scoring/embedding work, UI, dependency, new project/layer, database schema, playback execution, secrets, or Process.Start changes. Anything else is a follow-up issue, not a finding in this round.

Basis: Patron Q1 and Q5; `DEV-336:18`; `recon-DEV-336:9,17,44-50`; charter §2.3; task-pipeline Phase 2 and Phase 4.

## Approach and canonical vocabulary

Use namespace `LamuFlix.Core.Features.Recommendations` within existing Core. It is the ticket-named feature namespace, not a new architectural layer. Refer only to shared Domain value objects. No dependency, DI registration, interface, service wrapper, extension method, extra overload, or new factory.

`MovieFeatures` is the immutable feature projection of persisted metadata, not a recommendation result or a new metadata source. `Directors` means every persisted director; the ticket word director does not authorize truncation. `Cast` maps persisted `Actors`. `FullPlot` maps persisted `Synopsis`; DEV-336 does not fetch or improve plot content. No new glossary or ADR rewrite is needed for this size:M task.

### Exact record contract

A sealed record with these nine get-only properties:

| Property | Type | Persisted source |
| --- | --- | --- |
| Genres | ImmutableHashSet<string> | Movie.Metadata.Genres |
| Directors | ImmutableHashSet<string> | Movie.Metadata.Directors, all entries |
| Writers | ImmutableHashSet<string> | Movie.Metadata.Writers |
| Cast | ImmutableHashSet<string> | Movie.Metadata.Actors |
| ReleaseYear | LamuFlix.Core.Domain.ReleaseYear? | Movie.Metadata.ReleaseYear |
| Runtime | LamuFlix.Core.Domain.Runtime? | Movie.Metadata.Runtime |
| ImdbRating | LamuFlix.Core.Domain.ImdbRating? | Movie.Metadata.ImdbRating |
| MetaScore | int? | Movie.Metadata.MetaScore, value-preserving nullable conversion as needed |
| FullPlot | string? | Movie.Metadata.Synopsis |

One public constructor, in the table order, using parameter names `genres`, `directors`, `writers`, `cast`, `releaseYear`, `runtime`, `imdbRating`, `metaScore`, `fullPlot`. The four collection parameters are `IEnumerable<string>?`; the scalar parameter types match the table. Every parameter defaults to null. No mutable or init setters and no second factory. Default construction is valid and yields empty sets and null scalars.

Normalize null collections to non-null `ImmutableHashSet<string>` instances using `StringComparer.OrdinalIgnoreCase`. Materialize non-null inputs with the same comparer, so source-list mutation cannot change the record. Set duplicates collapse case-insensitively; ordering is not part of the contract. Preserve element text without trimming, case rewriting, or blank filtering; do not promise which spelling survives a case-equivalent duplicate. Preserve all scalars verbatim, including null, empty, or whitespace FullPlot. Add no domain validation or correction beyond existing Domain VO validation.

Implement structural `Equals(MovieFeatures?)` and consistent `GetHashCode()` across all nine fields. Four sets use case-insensitive, order-independent equality (`SetEquals`) and hash aggregation based on `StringComparer.OrdinalIgnoreCase`. Scalars use their existing value equality; FullPlot uses ordinal string equality/hashing. Independently constructed equivalent features must compare equal and have equal hash codes. Unequal values may have colliding hashes; tests must not require distinct hashes for unequal objects.

Basis: Patron Q2–Q4; `recon-DEV-336:12-14,20,52-53`; `.specify/memory/constitution.md:426-429`; MovieMetadata construction/equality precedent cited by Patron at `src/LamuFlix.Core/Domain/MovieMetadata.cs:10-131`.

### Pure mapping contract

The only mapping entry point is static `MovieFeaturesMapper.From(Movie movie)`, returning `MovieFeatures`.

- Null movie: throw ArgumentNullException.
- Movie.Metadata is null: four non-null empty OrdinalIgnoreCase sets and five null scalars; no throw.
- Metadata exists: map all nine sources in the table, tolerating partial/empty collections and absent nullable scalars.
- Never substitute Movie.Title, a filename, Movie.ParsedReleaseYear, or another fallback when metadata/scalars are missing.
- Mapping has no side effects and does not mutate Movie, MovieMetadata, or their collections. The output owns immutable snapshots, including Directors and Actors lists.

Basis: Patron Q2–Q4; `DEV-336:18`; `recon-DEV-336:12-14,23-29`.

## Files touched

Phase A decision artifacts (Keel):

- `specs/DEV-336/CONCLUSIONS.md`
- `specs/DEV-336/ASSUMPTIONS.md`
- `specs/DEV-336/brief.md`

Phase A drafts (Quill): `specs/DEV-336/spec.md`, `plan.md`, `tasks.md`, plus supporting Spec Kit artifacts required by its workflow under this feature directory. Draft only from these decisions; do not invent additional delivery scope. A missing decision is `needs decision: <question>` to Keel. Set `PYTHONUTF8=1` before running Spec Kit.

Phase B new production files only:

- `src/LamuFlix.Core/Features/Recommendations/MovieFeatures.cs`
- `src/LamuFlix.Core/Features/Recommendations/MovieFeaturesMapper.cs`

Phase B new test files only:

- `tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesMapperTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesPropertyTests.cs`

No edits to Movie, MovieMetadata, Options, Infrastructure, Api, web, packages, or config. If the approved file set cannot implement the contract, stop for a decision; do not silently widen it. Basis: Patron Q5 and recon file/namespace inventory.

## Test strategy

Use existing xUnit v3, Shouldly, and FsCheck conventions/integration. No new library, database, HTTP fixture, Testcontainers, or mocks are needed for this pure projection. The property-test gate is active, with no opt-out.

1. Full metadata maps all nine fields with distinct representative values; preserve every director and map Actors to Cast, Synopsis to FullPlot.
2. Missing Movie.Metadata produces four empty case-insensitive sets and five null scalars, even when Movie has a title/parsed year. Null movie throws the specific guard exception.
3. Partial metadata, empty collections, and nullable scalars preserve exactly what exists. Cover each collection and scalar, not just one aggregate case.
4. Collection construction and mapping collapse duplicates independent of case/order without trimming or blank filtering. Verify membership comparer even on empty/default sets.
5. Mutating source Directors/Actors lists after mapping, and source lists after direct construction, cannot change output features.
6. Default construction and explicit null collections yield valid empty sets/null scalars.
7. Independent equivalent records compare equal and hash equally; reordered/case-varied sets remain equal. Change each of the nine fields independently to exercise inequality, plus standard null/type equality behavior.
8. FullPlot null, empty, whitespace, and ordinal case differences survive mapping and obey ordinal equality; case folding applies to sets only.
9. Focused FsCheck properties exercise permutation/case/duplicate invariance and equality/hash coherence using existing repo tagging so run-property-tests.ps1 discovers them. Avoid implementation-mirroring assertions and random/unseeded data.

Basis: Patron Q3–Q5; ticket full/absent/partial acceptance coverage; `.specify/memory/constitution.md:308-325`.

## Task ordering and gates

1. Quill drafts spec, plan, and ordered tasks from this brief. Keel runs read-only speckit-analyze and checks plan/tasks against the brief; Quill handles numbered fixes. Conductor then obtains the three-axis plan challenge and Keel adjudicates before plan freeze.
2. Phase A remains specs-only. Rigger opens the spec PR after the required checks/challenge; Gate 1 stays closed until the user merges that spec PR. No agent merges or starts Phase B from grill closure alone.
3. At Phase B pickup, Wisp supplies current-main drift recon for the named file set; Keel re-runs analysis. Gauge owns baseline measurements. Do not assign searches, baseline gates, or unrelated file inventory to Builder.
4. Anvil implements value object, normalization, equality/hashing, and focused tests first; then mapper and its tests, completing properties within those dependent phases. Each implementation phase is committed before handoff; do not start a dependent phase before its prerequisite is green.
5. Cog refactors the complete bounded diff; no new abstraction or scope expansion. Gauge then runs the backend gates required by task-pipeline Phase 3 step 5.
6. Gauge pre-pass -> Sentry Risk/Security, Ledger Standards, Compass Spec -> Keel adjudication. All three axis reports are mandatory for size:M; missing one is blocked: missing axis <name>. Apply the closing bar and two-round cap above.
7. Later ship/rebase checks, PR merge bar, hosted checks and awaiting-merge are separate steps; neither a brief nor a local pass replaces them. User owns every merge.

Expected Phase B gates: Roslyn analyzers; cyclomatic complexity (refactor limit from harness); JetBrains inspectcode on the diff, never -All in this pipeline; production C# mutation; property tests; vulnerable packages; dotnet format --verify-no-changes; full dotnet test. Web gate is `SKIPPED (scope-empty)` for this frozen file set.

Mutation uses `scripts/run-mutation.ps1` with no since filtering, with thresholds/exclusions/kill-share/file-span rules from harness.yml. Do not edit generated configuration, lower thresholds, or waive survivors. Gauge reports score and Timeout separately and applies charter false-pass checks (zero tested/no score/since-ignored/out-of-scope mutation/all-Timeout do not clear it). A failure goes to Cog under the cap.

Exit 0 is PASS only when the gate actually ran successfully. Exit 1 or Could not run blocks. Scope-empty exit 2 is non-blocking `SKIPPED (scope-empty)`, never PASS; explicit harness opt-out is SKIP; script-reported all-excluded mutation is N/A; property-test exit 2 needs the pipeline-recorded opt-out, which this task does not have. No gate is run or claimed by this Phase A brief.

Basis: Patron Q1/Q5; task-pipeline Phases 2–5; lamuflix-team-charter standing mutation rulings; repository gate instructions. No constitution deviation or §2.3 escalation is required.

## Grill answer index for Quill

- Q1 accepted: frozen Core-only scope, zero unresolved Critical/High closing bar, two-round cap, no contradiction.
- Q2 accepted: exact nine fields, OrdinalIgnoreCase immutable sets, all directors, Actors -> Cast, Synopsis -> FullPlot, Domain scalar VOs/int?/string?, verbatim data and no fallback.
- Q3 accepted: sole static From(Movie), null guard, immutable snapshots, structural set/scalar equality and hashing.
- Q4 accepted: one all-optional constructor, get-only properties, normalization, ordinal FullPlot, no extra validation/factory.
- Q5 accepted: two production/three test files, focused xUnit/Shouldly/FsCheck matrix, ordered execution and gates, shared understanding and closure; zero owner checkboxes.

The full five exchanges and all Patron citations remain in CONCLUSIONS.md. These are the complete decisions for drafting; anything missing must return to Keel rather than being assumed by Quill.

## Phase 2 drafting clarifications — Patron Q6-Q7 (2026-10-19)

These decisions supplement Q1-Q5 and resolve both DRAFTING_RECEIPT.md questions; the remaining scope, closing bar, gates, and two-round cap are unchanged.

- Q6 accepted [assumed]: Preserve null elements at runtime in all four source collections; do not filter or reject them. Duplicate nulls collapse to one set member. Public types remain IEnumerable<string>? constructor parameters and ImmutableHashSet<string> properties as Q4. Equality must handle null elements, and set hash aggregation contributes 0 for a null element. Retain all previously ruled text, comparer, and snapshot semantics. Unit tests must exercise null members in each collection through construction and mapping, duplicate null collapse, and equality/hash coherence with null members. This is separate from a null collection, which still becomes empty.
- Q7 accepted [assumed]: Declare public static class MovieFeaturesMapper, with sole public mapping member public static MovieFeatures From(Movie movie). No instance state or construction.

Basis: Patron Q6-Q7 verbatim in CONCLUSIONS.md; Q2-Q4 pass-through/no-validation contract; note DEV-336:18; recon-DEV-336:13; constitution:451-452. No ticket delivery change or owner checkbox arises from these clarifications. Quill must propagate them into spec.md, plan.md, tasks.md and mark the receipt questions resolved before plan challenge.

Constitution applicability: Patron confirms the pure Core mapper complies with Principle I allowed references (constitution:125-127,141); stack row 353 chooses the mapping library when one is used and does not require Mapster for pure domain construction. No competing library, constitution departure, ADR, or owner checkbox. Full cited ruling is appended in CONCLUSIONS.md.

## Phase 2 plan challenge decisions (2026-10-19)

Patron ruling is persisted verbatim in CONCLUSIONS.md. All three axis reports were read. Sentry F1 is rejected on Compass verification item 1, not waived or demoted: the .NET 10 scratch probe preserved null with count=2, Contains(null)=true and SetEquals=true. Q6 remains unchanged; cite findings-DEV-336-Compass:12 in plan.md to close Ledger F1 feasibility.

- Set hashes must use commutative per-set aggregation: unchecked sum or XOR of OrdinalIgnoreCase element hashes, with 0 for null. Do not feed set enumeration directly into an order-sensitive hash combiner. Combining the four completed set aggregates with scalars remains free. FullPlot hash is 0 for null, otherwise StringComparer.Ordinal.GetHashCode(FullPlot).
- T001 must test the observable zero-identity behavior for each set: when scalars and non-null set members match, adding/removing a null member leaves the whole value hash unchanged. Equality still distinguishes absent/present null membership; unequal values may share a hash.
- Public element annotations remain string per Q6. Runtime null members are deliberately tolerated; DEV-288 consumers must handle null before dereferencing. Surviving case spelling and iteration order remain unspecified. These hazards are noted, no ticket; no consumer work enters this scope.
- Test names follow Method_Condition_Expected. Use existing FsCheck.Xunit [Property(MaxTest = 100)] and [Trait("Category", "Property")], with an explicit deterministic FsCheck Replay seed and no System.Random. Keep one serialized MovieFeaturesPropertyTests.cs (T002 before T005); do not add another file.
- T004 covers persisted MetaScore 0, 100 and null as value-preserving int? results. Null movie guard identifies nameof(movie); do not pin exception message text. Use Movie.Path in no-fallback wording. Correct the frozen-scope sentence capitalization.
- Default ImmutableArray handling awaits the requested bounded recon before final disposition; Patron authorizes normalizing source is ImmutableArray<string> { IsDefault: true } to empty within existing Q4 defensive normalization if reachable. No upstream MovieMetadata edit is authorized. The final branch of this decision must be recorded here before Quill drafts it.

Closing bar, frozen five-file implementation scope, two-round cap, gate requirements and task ordering remain unchanged. These are planning precision decisions, not Gate 1 or Phase B authorization.

### Default-array branch resolved from bounded recon

Read recon-DEV-336:55-85: MovieMetadata freezes null inputs to non-default Empty (MovieMetadata.cs:22-23,41-42,48-49), exposes get-only Genres/Writers (:73,75), and no production or test caller supplies any ImmutableArray. Thus the existing mapper paths do not expose default arrays. Select Patron unsupported-input branch: a caller deliberately boxing default(ImmutableArray<string>) as an IEnumerable input is outside this feature contract, both in direct MovieFeatures construction and in the hypothetical upstream MovieMetadata call. Do not promise normalization or a particular exception on that unsupported input, and add no IsDefault branch, upstream edit or test for it. The BCL identity-fast-path outcome of that hypothetical upstream call was not empirically verified; no claim that it always throws or is impossible is made. Ordinary null/empty collections and all Q6 runtime null-member cases remain fully supported. This closes the pending F5 decision without changing ticket delivery.


## Phase 2 plan freeze — 2026-10-19

Keel completed three-axis adjudication and verified Quill plan-challenge refinement round 1 against this brief. All 17 challenge items have individual dispositions in CONCLUSIONS.md; no unresolved Critical/High or open drafting decision remains. Planning is frozen at the SHA256 identities in canvas note plan-freeze-DEV-336. Earlier status statements describe their earlier phase; this section is the current planning status. Gate 1 remains closed until user merge of the spec PR. No implementation or gate pass is claimed.
