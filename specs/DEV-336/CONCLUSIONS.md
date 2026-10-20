# DEV-336 grill conclusions

Worktree: F:/Dev/LamuFlix.worktrees/feature-336-spec
Branch: feature/336-spec
Recon pin: 06aedfeabc9e26ecc0348c71ca9983a2a8d27a65
Decision authority: Patron. Facts: recon-DEV-336. Maximum 12 questions.

---

## Q1 — Scope and loop discipline

**Keel:** /grill-with-docs DEV-336. Worktree F:/Dev/LamuFlix.worktrees/feature-336-spec branch feature/336-spec. Read notes DEV-336 and recon-DEV-336 plus specs/PRODUCT.md, ticket text, constitution and charter as your decision basis. Max 12 questions, one at a time. Do not write the brief or conclusions; I will append the full exchanges. Q1: Confirm frozen scope is only a sealed Core Recommendations MovieFeatures record, pure mapper from persisted Movie/MovieMetadata, and focused unit tests; empty sets and null scalars on missing metadata; no HTTP, infrastructure, persistence, OMDb work (DEV-411), new dependency or layer. Recommend closing bar zero unresolved Critical/High, lower findings recorded as follow-ups, maximum two review/remediation rounds, anything else is a follow-up issue, not a finding in this round. Please rule with cited basis, note any scope or constitution contradiction, and confirm these loop terms. Facts come from recon-DEV-336. If you need one that is not there, reply needs recon: <question> and stop there.

**Patron (verbatim cited ruling):**
[from Patron] DEV-336 Q1 ACCEPT: Frozen scope, loop terms, and closing bar confirmed with zero scope or constitution contradictions.
- Scope frozen to Core (Features/Recommendations) sealed record MovieFeatures, pure mapper from persisted Movie/MovieMetadata (empty sets and null scalars when metadata is missing), and focused unit tests; no HTTP, infrastructure, persistence, OMDb work (DEV-411), new dependency, or layer (note DEV-336:18; recon-DEV-336:9,17,44-50; specs/PRODUCT.md:18,23-24,43-48).
- Constitution alignment verified: Core-only references, Recommendations feature folder isolation (referencing shared Domain/ types Movie and MovieMetadata only), sealed record with System.Collections.Immutable collections, English vendor-free naming, and xUnit v3 + Shouldly unit tests (.specify/memory/constitution.md:127,133-135,284-299,308,426-429).
- Standing loop terms cited from chain (DEV-334:24; AGENTS.md §2.3): closing bar is zero unresolved Critical/High, lower/out-of-scope non-broken findings logged in CONCLUSIONS.md as noted, no ticket, maximum two review/remediation rounds.

**Implications:** No implementation or gate clearance is claimed by this grill. Phase A drafts follow this bounded decision. Lower findings follow Patron triage, not automatic ticket creation. The frozen scope ends: anything else is a follow-up issue, not a finding in this round.

---

## Q2 — Field contract and missing values

**Keel:** Settle the exact MovieFeatures record field types and collection semantics. Recon says persisted Directors is a list, Actors is a list, Genres/Writers immutable arrays; ticket says director and cast, sets when absent. Recommend immutable sets Genres, Directors, Writers, Cast using StringComparer.OrdinalIgnoreCase, preserving all directors (no first-only truncation), mapping Actors to Cast, no trim/case rewriting or blank filtering; nullable ReleaseYear/Runtime/ImdbRating existing Domain value objects, int? MetaScore, string? FullPlot from Synopsis. Missing metadata must not fall back to parsed filename/year or title. Is this the right bounded contract? Please decide exact comparer, scalar types and null/empty text policy with cited basis; naming taste [assumed]. Do not add fields absent from ticket. Reply directly with the full ruling or one exact result file. Facts come from recon-DEV-336. If you need one that is not there, reply needs recon: <question> and stop there.

**Patron (verbatim cited ruling):**
[from Patron] DEV-336 Q2 ACCEPT: Bounded MovieFeatures contract, collection semantics, scalar types, and null/empty policy confirmed (taste choices tagged [assumed]).
- Collections & comparer: Genres, Directors, Writers, and Cast ([assumed] for Directors plural and Cast from Actors, matching SimilarityWeights) are ImmutableHashSet<string> with StringComparer.OrdinalIgnoreCase ([assumed]), preserving all directors without truncation and defaulting to empty OrdinalIgnoreCase sets when metadata is null or empty (note DEV-336:18; recon-DEV-336:13,20,52-53; .specify/memory/constitution.md:429).
- Scalars: ReleaseYear?, Runtime?, and ImdbRating? reuse existing LamuFlix.Core.Domain value objects; MetaScore is int? ([assumed], widened from MovieMetadata.MetaScore) and FullPlot ([assumed]) is string? from MovieMetadata.Synopsis; strictly the nine ticket fields only (note DEV-336:18; recon-DEV-336:9,13-14; .specify/memory/constitution.md:427-429).
- Null/empty & fallback policy: Mapper preserves persisted values verbatim without trim/case rewriting or blank filtering ([assumed]); when Movie.Metadata is null, all four sets are empty and all five scalars are null, never falling back to Movie.Title or Movie.ParsedReleaseYear (note DEV-336:18; recon-DEV-336:12-13).

---

## Q3 — Mapper, ownership, and value equality

**Keel:** Settle pure mapper API, snapshot ownership, and equality. Recommend one static MovieFeaturesMapper.From(Movie movie) in Core Features/Recommendations; no DI/interface/extension method/service or MovieMetadata overload unless justified. Null movie throws ArgumentNullException; null Movie.Metadata returns all-empty feature record. Materialize immutable sets so mutating source Directors/Actors after mapping cannot change features. The ticket calls this a value object: should equality be structural across independently mapped equivalent feature sets (case-insensitive, order-independent set equality plus scalar equality), requiring coherent Equals/GetHashCode, or only standard sealed-record semantics? Recommend structural equality as the useful value-object contract, with tests for reordered/case-varied duplicate inputs and matching hash codes; cost is a small explicit equality implementation. Please make a cited ruling, flag if this would change ticket delivery, and confirm mapper naming [assumed]. Facts come from recon-DEV-336. If you need one that is not there, reply needs recon: <question> and stop there.

**Patron (verbatim cited ruling):**
[from Patron] DEV-336 Q3 ACCEPT: Pure static MovieFeaturesMapper.From(Movie), immutable snapshot ownership, and structural value-object equality confirmed; zero change to ticket delivery (mapper naming tagged [assumed]).
- Mapper API & guard semantics: Single static MovieFeaturesMapper.From(Movie movie) in LamuFlix.Core.Features.Recommendations ([assumed]); no interface, DI registration, service wrapper, extension method, or extra overload (specs/PRODUCT.md:18; .specify/memory/constitution.md:451-452). Throws ArgumentNullException when movie is null; returns all-empty sets and all-null scalars when movie.Metadata is null (note DEV-336:18; recon-DEV-336:12,28).
- Snapshot ownership: Materializes ImmutableHashSet<string> with StringComparer.OrdinalIgnoreCase during mapping (and ensures non-null OrdinalIgnoreCase sets on MovieFeatures construction) so mutating source collections cannot mutate MovieFeatures (recon-DEV-336:13; .specify/memory/constitution.md:426-429).
- Structural VO equality: Implement explicit Equals(MovieFeatures?) and order-independent/case-insensitive GetHashCode() across the four sets (via SetEquals and StringComparer.OrdinalIgnoreCase element hashes) plus scalar equality, matching the MovieMetadata value-object precedent without altering ticket scope (note DEV-336:18; src/LamuFlix.Core/Domain/MovieMetadata.cs:77-131).

---

## Q4 — Construction invariants

**Keel:** Finish the construction invariant behind Q3. Recommend a sealed record with one constructor accepting IEnumerable<string>? genres/directors/writers/cast and nullable ReleaseYear, Runtime, ImdbRating, int? metaScore, string? fullPlot, all optional defaults null; expose get-only properties, normalize null collections to non-null OrdinalIgnoreCase ImmutableHashSet values and defensively materialize non-null inputs. No second factory or mutable init setters; preserve source element text as ruled Q2; no new domain validation or metadata correction. Scalars retain Domain VO types, null/empty/whitespace FullPlot passes through. Equality compares FullPlot ordinally and every scalar plus the four sets, independent of order and case only for sets. This makes default construction a valid empty feature record and prevents with/init comparer bypass. Approve this concrete construction contract and naming as [assumed], or give the minimal correction with cited basis. Facts come from recon-DEV-336. If you need one that is not there, reply needs recon: <question> and stop there.

**Patron (verbatim cited ruling):**
[from Patron] DEV-336 Q4 ACCEPT: Explicit single-constructor MovieFeatures contract, get-only properties, defensive OrdinalIgnoreCase set materialization, and ordinal FullPlot equality confirmed (constructor signature/naming tagged [assumed]).
- Single constructor & get-only properties: MovieFeatures is a sealed record with one constructor taking optional IEnumerable<string>? (genres, directors, writers, cast) and nullable scalars (releaseYear, runtime, imdbRating, metaScore, fullPlot) all defaulting to null, exposing get-only properties with no init setters or second factory ([assumed]), mirroring MovieMetadata (src/LamuFlix.Core/Domain/MovieMetadata.cs:10-76; .specify/memory/constitution.md:426-429).
- Defensive normalization: Null collection inputs normalize to empty ImmutableHashSet<string> with StringComparer.OrdinalIgnoreCase and non-null inputs materialize via ToImmutableHashSet(StringComparer.OrdinalIgnoreCase), preventing with/init comparer bypass while passing source elements and scalar values (including null/empty/whitespace FullPlot) through untouched (note DEV-336:18; recon-DEV-336:13).
- Equality semantics: Equals and GetHashCode compare ReleaseYear, Runtime, ImdbRating, MetaScore, and FullPlot ordinally (StringComparer.Ordinal for FullPlot) and compare Genres, Directors, Writers, and Cast via order-independent StringComparer.OrdinalIgnoreCase set equality and hash aggregation (src/LamuFlix.Core/Domain/MovieMetadata.cs:77-131).

---

## Q5 — Complete plan and grill closure

**Keel:** Confirm shared understanding and the complete bounded plan for Quill. New production files only src/LamuFlix.Core/Features/Recommendations/MovieFeatures.cs and MovieFeaturesMapper.cs. New tests only tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesTests.cs, MovieFeaturesMapperTests.cs, MovieFeaturesPropertyTests.cs (test path naming [assumed]); no edits to Movie/MovieMetadata, Options, infrastructure/API/web, packages/config. Tests use existing xUnit v3/Shouldly and existing FsCheck integration: full nine-field mapping including multiple directors, metadata absent, partial/empty collections and nullable scalars, duplicates/case/order set semantics, null movie guard, independent source-list mutation, default/null collection construction, equality and hashing, each field affects equality, FullPlot null/empty/whitespace and ordinal case pass-through. Add focused properties for permutation/case/duplicate invariance and equality/hash coherence; no property-test opt-out. Order: Anvil value object and its tests, then mapper and tests, Cog refactor, Gauge gates, three axes and Keel adjudication. Gauge applies all backend gates in task-pipeline Phase 3 step 5; mutation on changed production C# using run-mutation.ps1 without since, thresholds from harness.yml, honest SKIPPED/N/A per standing rules; web scope-empty. No gates claimed now. Specs-only Phase A awaits merged spec PR Gate 1 before build, no merge by agents. Closing bar zero unresolved Critical/High, two rounds maximum, remaining findings Patron-triaged. Approve plan/tests/gates and grill closure, identify any unresolved question or required correction, and cite basis. Facts come from recon-DEV-336. If you need one that is not there, reply needs recon: <question> and stop there.

**Patron (verbatim cited ruling):**
[from Patron] DEV-336 Q5 ACCEPT: Shared understanding, bounded file/test/gate plan for Quill, and grill closure confirmed with zero unresolved questions or owner checkboxes (test file paths tagged [assumed]).
- Bounded file & test scope: Production files strictly src/LamuFlix.Core/Features/Recommendations/MovieFeatures.cs and MovieFeaturesMapper.cs; unit and FsCheck property tests strictly tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesTests.cs, MovieFeaturesMapperTests.cs, and MovieFeaturesPropertyTests.cs ([assumed]); zero edits to Movie, MovieMetadata, Options, Infrastructure, Api, web, or packages/config (note DEV-336:18; recon-DEV-336:9,17,41,44-50; .specify/memory/constitution.md:127,133-135,308-309).
- Test matrix & property tests: xUnit v3 + Shouldly + FsCheck covering full nine-field mapping (including multi-director preservation), null Movie.Metadata, partial/empty sets and nullable scalars, duplicate/case/order invariance, null Movie guard, post-mapping source collection mutation isolation, default/null constructor normalization, field-by-field structural equality/hash coherence, and ordinal FullPlot pass-through; propertyTests gate active with no opt-out (note DEV-336:18; .specify/memory/constitution.md:308-325).
- Execution & gate sequence: Phase A specs-only awaits user merge of spec PR (Gate 1, zero owner checkboxes); Phase B runs Anvil (VO + tests -> mapper + tests) -> Cog refactor -> Gauge full backend gates (including run-mutation.ps1 without since per charter standing ruling; web scope-empty) -> Sentry/Ledger/Compass 3-axis challenge -> Keel adjudication under the two-round, zero unresolved Critical/High closing bar (specs/PRODUCT.md:12,19,36-40; lamuflix-team-charter:66-80).

**Closure:** Five of twelve questions used. All five rulings accepted by Patron and persisted before drafting the brief. No unresolved decisions, ticket contradiction, constitution departure, owner checkbox, or recon request. This closes the grill only; plan challenge, Gate 1, implementation, gates, review and merge remain separate. No ADR or glossary file rewrite is required for this size:M mapping task; canonical feature terms and their mappings are fixed in brief.md.

---

## Q6-Q7 — Drafting decision follow-up (2026-10-19)

**Keel:** Quill asks whether null string elements inside source collections are kept, dropped, or rejected, and whether MovieFeaturesMapper is static or sealed. Recommend null pass-through without new validation, null-safe equality/hashing, and a public static mapper class with only From(Movie). These clarify Q2-Q4 without widening the ticket.

**Patron (verbatim cited ruling):**
[from Patron] DEV-336 Q6-Q7 ACCEPT: Null collection element pass-through and static MovieFeaturesMapper class confirmed with zero change to ticket delivery (both taste choices tagged [assumed]).
- Q6 (Null collection elements): Preserve null elements inside Genres, Directors, Writers, and Cast at runtime as supplied, collapsing duplicate nulls via ImmutableHashSet<string>.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase) with no filtering or validation ([assumed]), null-safe equality and hash aggregation (treating null element hash as 0), and keeping public declared non-nullable element types (IEnumerable<string>? parameters, ImmutableHashSet<string> properties) as ruled in Q2/Q4 (specs/DEV-336/CONCLUSIONS.md:29-32,53-56; note DEV-336:18; recon-DEV-336:13; src/LamuFlix.Core/Domain/MovieMetadata.cs:77-131).
- Q7 (Mapper class modifier): Declare MovieFeaturesMapper as public static class MovieFeaturesMapper with sole public static MovieFeatures From(Movie movie) ([assumed]), permitting zero instance state or construction in keeping with Q3 (specs/DEV-336/CONCLUSIONS.md:41-42; specs/PRODUCT.md:18; .specify/memory/constitution.md:451-452).

---

## Phase 2 constitution applicability ruling — Mapster (2026-10-19)

**Keel:** Constitution stack row 353 names Mapster while Q3 prescribes pure static Core construction. Decide whether this is within existing constraints or a constitution departure; do not silently waive a MUST.

**Patron (verbatim cited ruling):**
[from Patron] DEV-336 ANALYSIS CONSTITUTION RULING: Pure static MovieFeaturesMapper is strictly within the existing constitution; zero constitution departure, ADR, or owner checkbox required.
- Core reference boundary mandates pureBCL mapping in Core: Principle I explicitly restricts LamuFlix.Core references to BCL, Microsoft.Extensions.Logging.Abstractions, System.Collections.Immutable, and Ardalis.SmartEnum only, enforced by LamuFlix.ArchitectureTests (.specify/memory/constitution.md:125-127,141; recon-DEV-336:17-19,44-45), so referencing Mapster inside Core is constitutionally forbidden.
- Stack row 353 governs library selection (Mapster over forbidden AutoMapper), not pure domain construction: Technology Stack Constraints row 353 establishes Mapster as the project's object-mapping library choice and forbids AutoMapper (.specify/memory/constitution.md:335-336,353,361); a pure static domain projection in Core neither replaces Mapster nor introduces a competing mapping library.
- Explicit ticket and product alignment: Both the ticket and PRODUCT.md specify a pure mapper from persisted Movie/MovieMetadata to the MovieFeatures value object inside Core Recommendations with zero new package or architectural dependency (note DEV-336:18; specs/PRODUCT.md:18,44; specs/DEV-336/CONCLUSIONS.md:16-17,41-42).

---

## Phase 2 three-axis plan adjudication — Patron ruling (2026-10-19)

**Keel:** Requested a cited disposition for all Sentry F1-F7, Ledger F1-F7, and Compass F1-F3, testing the null-element premise against Compass verification item 1 and requesting bounded recon for default ImmutableArray reachability.

**Patron (verbatim cited ruling):**

[from Patron] DEV-336 PHASE 2 ADJUDICATION RULING: Reject Sentry F1 technical premise on empirical .NET 10 evidence; retain Q6-Q7; apply bounded precision updates across all remaining Sentry, Ledger, and Compass items with zero delivery expansion, constitution departure, or owner checkbox.
- Sentry F1 (High -> Rejected) & Ledger F1 (Medium -> Closed): Reject Sentry F1's unverified claim that ToImmutableHashSet(StringComparer.OrdinalIgnoreCase) throws on null elements; Compass verification item 1 empirically proved on .NET 10 that new[]{"A", null, "a"}.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase) succeeds (count=2, Contains(null)==true, SetEquals==true), because ImmutableHashSet<T> handles null keys before invoking comparer hashing; cite this empirical proof in plan.md to close Ledger F1 feasibility and retain Q6 as ruled (findings-DEV-336-Compass:12; specs/DEV-336/CONCLUSIONS.md:55).
- Set & scalar hashing (Sentry F2, Ledger F5, Compass F1, Sentry F4): Specify commutative per-set element hash aggregation (unchecked sum or XOR of StringComparer.OrdinalIgnoreCase.GetHashCode(element) with 0 for null, matching MovieMetadata.cs:77-131) and null-safe FullPlot hash (0 when FullPlot is null else StringComparer.Ordinal.GetHashCode(FullPlot)); in T001 state the observable null-hash test form (adding/removing a null element leaves GetHashCode() unchanged when scalars and non-null set members match) without pinning sum vs XOR (findings-DEV-336-Sentry:47-58,73-78; findings-DEV-336-Ledger:23-25; findings-DEV-336-Compass:20-22; src/LamuFlix.Core/Domain/MovieMetadata.cs:77-131).
- Default ImmutableArray reachability, test/spec precision & triage (Sentry F3/F5/F6/F7, Ledger F2/F3/F4/F6/F7, Compass F2/F3): MovieMetadata constructor normalizes Genres/Writers via ToImmutableArray() (recon-DEV-336:13; MovieMetadata.cs:45-49) so default ImmutableArray<string> cannot arise from persisted MovieMetadata, and direct callers passing a boxed default ImmutableArray<string> to MovieFeatures are documented as unsupported unless MovieMetadata.cs:45-49 leaves any path unnormalized (if recon shows it reachable via with/init on MovieMetadata, normalize source is ImmutableArray<string> { IsDefault: true } to empty set within existing Q4 defensive normalization scope); record Sentry F3/F6 as noted, no ticket in CONCLUSIONS.md (with a note for DEV-288 consumers on null-element and case-variant iteration); apply one-line precision edits for Sentry F7 (ArgumentNullException(nameof(movie))), Ledger F2 (Method_Condition_Expected), Ledger F3 ([Property(MaxTest = 100)] + [Trait("Category", "Property")], FsCheck Replay seed, zero System.Random), Ledger F4 (retain single MovieFeaturesPropertyTests.cs), Ledger F6 (MetaScore 0, 100, null in T004), Ledger F7 (accepted), and Compass F2/F3 (Movie.Path wording and brief.md capitalization) (findings-DEV-336-Sentry:60-71,80-103; findings-DEV-336-Ledger:14-30; findings-DEV-336-Compass:24-30).

**Sentry F5 evidence resolution:** Bounded follow-up recon-DEV-336:55-85 finds no existing constructor, persistence, OMDb or test path supplying a default ImmutableArray; MovieMetadata.cs:48-49 normalizes null and ordinary inputs and :73,75 prevent init/with bypass. Patron unsupported-input branch selected: deliberately boxed default ImmutableArray inputs are unsupported, with no promised result or exception. Residual BCL fast-path behavior was not probed and is not asserted. No new normalization logic, test, or upstream edit is scheduled. This supersedes the overly broad phrase in the ruling that default arrays cannot arise at all: the hypothetical future boxed-default call is explicitly outside the supported contract.

## Phase 2 complete finding disposition ledger

All three mandatory axes present. Source notes: findings-DEV-336-Sentry:12-103; findings-DEV-336-Ledger:8-30; findings-DEV-336-Compass:20-30. Patron cited ruling above governs; no severity was silently lowered. Draft refinements are verified before freeze.

| Axis/item | Original severity | Adjudication |
| --- | --- | --- |
| Sentry F1 | High | Rejected: Compass .NET 10 empirical construction/Contains/SetEquals evidence disproves claimed deterministic throw. Q6 retained. |
| Sentry F2 | Medium | Accepted precision: commutative per-set sum or XOR, null zero identity; no order-sensitive enumeration hash. |
| Sentry F3 | Medium | Noted, no ticket: runtime null members despite string annotations deliberately retained by Q6; DEV-288 consumers must guard null before dereference. No consumer edit in scope. |
| Sentry F4 | Low | Accepted precision: null FullPlot contributes 0; non-null uses ordinal hash. |
| Sentry F5 | Low | Closed with documented unsupported boxed-default input boundary and recon-DEV-336:55-85; no current caller/default-array bypass, no new normalization requirement. Residual hypothetical BCL behavior unverified and unclaimed. |
| Sentry F6 | Note | Noted, no ticket: set iteration order and surviving case spelling are unspecified; DEV-288 must not depend on them. |
| Sentry F7 | Note | Accepted: guard identifies nameof(movie); no fixed message assertion. |
| Ledger F1 | Medium | Feasibility closed by Compass empirical proof in plan; annotation concern disposed with Sentry F3. Q6 not dropped. |
| Ledger F2 | Low | Accepted: Method_Condition_Expected test names. |
| Ledger F3 | Low | Accepted: explicit FsCheck.Xunit Property(MaxTest=100), Category=Property trait and deterministic Replay seed, no System.Random. |
| Ledger F4 | Low | Noted, no ticket: retain single property file and existing sequential T002 then T005 ownership; no sixth file. |
| Ledger F5 | Low | Accepted with Sentry F2; sealed record and structural set equality follow ticket/Q3 and existing MovieMetadata precedent. |
| Ledger F6 | Low | Accepted: T004 score 0, 100 and null prove nullable widening. |
| Ledger F7 | Info | Accepted existing ticket-decided Recommendations placement and static mapper; no new abstraction or checkbox. |
| Compass F1 | Medium | Accepted observable T001 assertion: same scalars/non-null members, add/remove null in each set, same whole-value hash; equality still distinguishes membership. No combiner mirroring. |
| Compass F2 | Low | Accepted wording: Movie.Path in no-fallback examples. |
| Compass F3 | Low | Accepted capitalization correction in brief frozen-scope sentence. |

Total: 17 unique items (7 Sentry, 7 Ledger, 3 Compass). Only High rejected on contrary evidence, never waived. Two non-broken consumer hazards and existing serialized-file design are noted, no ticket, under Patron triage. No ticket contradiction, constitution departure, owner checkbox, expanded implementation scope, new dependency, or gate waiver. Plan challenge refinement is one round; prior drafting-analysis rounds are recorded separately. No implementation or runtime/gate success follows from these dispositions.

## Final read-only consistency analysis and freeze — 2026-10-19

Prerequisite check returned FEATURE_DIR specs/DEV-336 and AVAILABLE_DOCS tasks.md; no extension hooks present. Analysis itself remained read-only; decision artifacts were updated separately under the explicit plan-adjudication/freeze ask. Quill completed one numbered plan-challenge refinement round and Keel independently read the amended contracts, tasks, receipt and checklist.

| Requirement | Covered by tasks |
| --- | --- |
| FR-001 | T001, T003 |
| FR-002 | T001, T003 |
| FR-003 | T001, T002, T003, T004, T005, T006 |
| FR-004 | T001, T003, T004, T006 |
| FR-005 | T001, T002, T003, T004, T005 |
| FR-006 | T004, T005, T006 |
| FR-007 | T004, T006 |
| FR-008 | T001, T002, T004, T005, T008 |
| FR-009 | T003, T006, T007, T008 scope constraints |
| SC-001 | T004, T006 |
| SC-002 | T004, T006 |
| SC-003 | T004, T006 |
| SC-004 | T001, T002, T003, T004, T005, T006 |
| SC-005 | T001, T002, T003, T004, T005 |
| SC-006 | T008 |

Analysis verdict: CLEAN. Fifteen FR/SC requirements, eight distinct tasks, 100% coverage, zero unmapped tasks, zero unresolved ambiguity/duplication/constitution findings. T007 is mapped to maintainability/gate compliance across FR-008/FR-009 and SC-006. All nine fields, construction, null/empty/partial mapping, null members, immutability, no fallback and equality/hash cases remain covered. US1 precedes US2, T002 precedes T005 on the same file, and Cog precedes Gauge. Existing five-file implementation boundary and all gate semantics remain intact. No new package, layer, upstream type edit or architecture change.

Sentry F1 is rejected by contrary empirical evidence, Ledger F1 closed, accepted refinements implemented in drafts, and noted items retain Patron dispositions. No unresolved Critical/High, owner checkbox, constitution departure or drafting question remains. Planning frozen; exact working-file identities are in canvas note plan-freeze-DEV-336. Gate 1 remains closed until the user merges the spec PR. No production code, runtime test, backend gate, tracker mutation, PR publication or merge was performed by Keel. Spec-PR packaging belongs to Rigger; no commit is claimed here.

---

## Phase 2 Step 7 — Gate 1 provisional confirmation (2026-10-19)

**Patron (verbatim cited ruling):**
[from Patron] DEV-336 GATE 1 PROVISIONAL: Confirmed `**Status**: gate1: provisional` in `specs/DEV-336/spec.md:7` with zero owner checkboxes and zero constitution departures.
- Consistency & plan challenge clearance: `/speckit-analyze` is CLEAN (15 FR/SC requirements, 8 tasks, 100% coverage, 0 unresolved findings) and all 17 three-axis findings across Sentry, Ledger, and Compass are adjudicated and frozen (note `DEV-336:41-61`; note `plan-freeze-DEV-336:6-15`; `specs/DEV-336/CONCLUSIONS.md:110-160`).
- Zero escalations: Zero ticket-text contradictions and zero constitution departures; pure static Core mapping complies with Principle I and Stack row 353 (`specs/DEV-336/CONCLUSIONS.md:15-18,90-93`; `.specify/memory/constitution.md:125-127,353`).
- Gate 1 boundary: Phase A spec artifacts are ready for Rigger spec-PR packaging; Gate 1 remains closed until the user merges the spec PR.

---

## Phase 3 Step 6 — Gate-forced test fixture scope disposition (2026-10-20)

**Conductor / Cog:** In `F:/Dev/LamuFlix.worktrees/DEV-336` (`feature/DEV-336`), `dotnet test` fails on `RabbitMqTopologyTests.EnsureDeclaredAsync_BindsEveryRoutingKeyToItsOwnQueue` during `DisposeAsync` (`ResetTopologyAsync` -> `EnsureTopologyAsync`, broker-side `cluster_not_formed`). Does Patron authorize modifying tracked infrastructure files (`RabbitMqFixture.cs` or `RabbitMqTopology.cs`) to remediate the `DisposeAsync` reset flakiness under DEV-336, or does DEV-336 scope strictly prohibit infrastructure edits?

**Patron (verbatim cited ruling):**
[from Patron] DEV-336 SCOPE DISPOSITION: Authorize a minimal gate-forced remediation in test support (`tests/LamuFlix.Tests.Common/RabbitMqFixture.cs` only); strictly prohibit any edit to production infrastructure (`src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs`).
- Gate-forced test-fixture edit in scope (§2.3 item 6): `dotnet test` exit 0 is a non-waivable blocking gate with rerun loops forbidden (`task-pipeline:106,108-110,133`; `DEV-336:180-198`), and the `cluster_not_formed` failure originates in `RabbitMqFixture.ResetTopologyAsync` / `EnsureTopologyAsync` during test teardown (`DEV-336:185-187`), so a minimal remediation confined to `tests/LamuFlix.Tests.Common/RabbitMqFixture.cs` is a gate-forced edit authorized by Patron under §2.3 item 6 (`lamuflix-team-charter:24`; `specs/DEV-336/tasks.md:65,71`).
- Production `src/LamuFlix.Infrastructure` strictly prohibited: DEV-336's ticket text and acceptance criteria forbid production HTTP/infrastructure work and require Core-only production types (`DEV-336:18`; `specs/DEV-336/CONCLUSIONS.md:16,66`), and modifying `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs` would violate the Core-only production boundary and pull `LamuFlix.Infrastructure` into `run-mutation.ps1` scope (`task-pipeline:102`).
- Execution path for Cog and Gauge: Cog applies the minimal teardown/declaration resilience fix in `tests/LamuFlix.Tests.Common/RabbitMqFixture.cs` (leaving `src/` restricted to `MovieFeatures.cs` and `MovieFeaturesMapper.cs`), commits on `feature/DEV-336`, and hands back to Gauge to re-run Phase 3 step 5 gates on the new HEAD (`task-pipeline:109`; `DEV-336:190,198`).


