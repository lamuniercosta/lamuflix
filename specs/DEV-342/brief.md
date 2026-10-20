# DEV-342 — SimilarityScorer property and golden tests

## Status and authority

Phase A grill closed with Patron-confirmed shared understanding at Q8/12. Zero owner structural checkboxes. This brief is the drafting input for Quill; it is not plan freeze, Gate 1 approval, implementation authority, or evidence of passing build gates.

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-342-spec`.
- Branch: `feature/342-spec`.
- Intake base: `8edc1b8f4832006bc812f8ebd2da3caf0e9c3ea9`.
- Read-back conclusions commit: `c0e5dd42e733cda077d389684a3e17198e934aff`.
- Authoritative ticket: YouTrack Description:1–18, transcribed in `recon-DEV-342:83-111`; Scope & Technical Design at :94-104, acceptance criteria at :106-108.
- Contract evidence: `recon-DEV-342-2:7-73`. Facts come from these recon notes; no additional code reconnaissance or measurements were performed for this brief.
- Full exchanges and cited Patron rulings: [CONCLUSIONS.md](CONCLUSIONS.md), Q1–Q8. The answers govern where they amend a recommendation, especially Q7.

## Product outcome and frozen scope

Verify the mathematical correctness of the real `SimilarityScorer` through four FsCheck properties and an independently audited golden ranking fixture in `LamuFlix.UnitTests`.

Acceptance is fixed by Description:17–18: all four scorer properties pass 1000 generated iterations each, and a hand-curated 30-movie library yields the expected ordered top-3 for each of three seeds.

Scope includes the local generators/shrinkers, the four property tests, the typed golden library and assertions, the manual oracle calculation record, and the planning/pickup evidence required by Q1–Q8. Production edits, predecessor-test rewrites, query-handler exclusion tests, new packages/projects/layers, database/API/playback changes, and unrelated cleanup are excluded; anything else is a follow-up issue, not a finding in this round.

The selected-movie exclusion property belongs to DEV-340 (Description:14). Seed removal in golden test setup does not test or depend on the handler.

## Settled grill answers

### Q1 — Prerequisites and workspace boundary

Complete Phase A specification now. Gate all Phase B implementation on Epic 7 completion and merged DEV-337 and DEV-338 production implementations on main, verified by pickup drift recon. No scorer/IDF stubs, duplicate contracts, or split build passes. Spec-package merges do not establish production availability; both symbols are absent at intake (`recon-DEV-342:38-41`). Build execution belongs to the Build workspace, not this Spec workspace.

### Q2 — Iterations, replay and shrinking

Each of the four properties uses `[Property(MaxTest = 1000, Replay = "...")]` and `[Trait("Category", "Property")]`, with a distinct committed replay seed. Preserve shrinking and replay/minimized-counterexample diagnostics. Construct self-similarity inputs with at least one scoring feature and preserve that invariant during shrinking; do not rely on implication discards. Keep featureless inputs in the other three properties. Leave existing unrelated `MaxTest = 100` tests unchanged.

### Q3 — Independent golden oracle

Use exactly 30 fixed movie identities, with three seeds drawn from those 30. For each seed, directly score the other 29 through the real merged scorer with real GenreWeights calculated from the full 30-movie library and explicit SimilarityWeights. Q8 pins the coefficient vector to `Director = 3.0`, `Writers = 2.0`, `Cast = 2.0`, `Genres = 3.0`, `YearProximity = 1.0`, `RuntimeProximity = 0.5`, and `RatingBoost = 0.5`. Construct those values explicitly in fixture data and its calculation record so changes to defaults do not silently alter the oracle.

Commit independently hand-derived ordered top-3 identities for each seed. Record the arithmetic and why the selected three outrank the remaining candidates in `golden-calculations.md`. Require strict score separation across the top three and the membership boundary; ties must not govern membership or order. Do not generate expected rankings from the scorer, create a shadow scorer, or call the DEV-340 handler. Write the fixture and calculations only in Phase B after both prerequisite implementations merge.

### Q4 — Files and dependency preservation

Create these files under `tests/LamuFlix.UnitTests/Features/Recommendations/`:

| File | Responsibility |
| --- | --- |
| `MovieFeaturesArbitraries.cs` | Local feature/pair/weight/IDF generation and valid shrinking |
| `SimilarityScorerInvariantPropertyTests.cs` | The four 1000-case scorer properties |
| `SimilarityScorerGoldenFixture.cs` | Typed in-memory 30-movie data, seeds, explicit weights and expected identities |
| `SimilarityScorerGoldenTests.cs` | Direct scorer execution and ordered top-3 assertions for three seeds |

The fifth Phase B artifact is `specs/DEV-342/golden-calculations.md`. Use no disk I/O, external data loader, new packages, new project/layer, production changes, or `LamuFlix.Tests.Common` changes. Preserve landed DEV-337/DEV-338 tests, including DEV-338's planned `SimilarityScorerTests.cs` and `SimilarityScorerPropertyTests.cs`. Pickup contradictions route for a decision before any dependency edit. Put calculation explanations in documentation, not explanatory C# comments.

### Q5 — Numeric assertions

| Property | Assertion |
| --- | --- |
| Self-similarity | Exactly `Score(a, a) == 1.0` for a nonempty seven-field scoring profile, including valid all-zero weights |
| Symmetry | `Math.Abs(scoreAB - scoreBA) <= 1e-12`, using the same weights and GenreWeights snapshot |
| Boundedness | `double.IsFinite(score) && score >= 0.0 && score <= 1.0`; no outside-bound epsilon |
| Reasons | `score > 0.0` implies nonempty Reasons; do not assert the converse |

The existing zero-denominator fallback returns 1.0 for identical nonempty scoring profiles, otherwise zero for empty/nonidentical profiles. Valid extreme ratios may underflow the score to zero while Reasons remain nonempty. These semantics interpret the four ticket properties; they do not add new property deliverables (`recon-DEV-342-2:35-44,68-71`).

### Q6 — Generator design and pickup facts

Use explicit branches for empty, single-field, partially missing, identical, disjoint and overlapping feature profiles. Use a shared token vocabulary so generated pairs exercise actual overlap; include case variants and preserved null/blank tokens according to the landed rules. The seven scored fields are Directors, Writers, Cast, Genres, ReleaseYear, Runtime and ImdbRating. MetaScore and FullPlot do not establish scoring-feature presence or identity.

Generate only valid finite nonnegative coefficients in `[0.0, double.MaxValue]`. Cover all-zero, single-positive across all seven positions, ordinary mixed and extreme-ratio/MaxValue families. Invalid-argument guard tests remain predecessor coverage. Construct real pure GenreWeights contexts for populated counts and empty/unknown defaults, without a catalog/repository. Preserve valid domains while shrinking and retain branch-coverage diagnostics from the Q6 exchange.

Before encoding generators, pickup recon must supply exact landed VO bounds, constructors, scorer/IDF call signatures, and the string-genre/integer-ID bridge, including null/blank handling. These are explicit deferred facts, not guessed Phase A contracts (`recon-DEV-342-2:58-64`). Do not invent a bridge or alter dependencies to fit generators.

### Q7 — Loop discipline

- Closing bar: verified in-scope Critical and High findings block. Medium and Low are recorded nonblocking with disposition unless they demonstrate a failed explicit acceptance criterion; classify actual impact and do not waive a failed AC.
- All three M-ticket axes are mandatory: Sentry, Ledger and Compass. A missing report blocks adjudication; it is not a skip.
- Plan challenge and Phase 4 review/remediation are each capped at two rounds, with at most two fix commits per remediation round. Remaining blockers at the cap produce a blocked report with exact unresolved findings, not another round or a weakened threshold.
- Apply charter :38 to out-of-scope observations: a follow-up ticket only for Critical/High, broken behavior or a user request, folding into existing tickets where possible; otherwise record `noted, no ticket`. Rigger alone performs tracker writes.

This is Patron's amended Q7 answer, superseding the proposed Medium-blocking bar in the question. No owner structural checkbox was identified.

### Q8 — Gates and documentation

Phase A runs no implementation gates. In Phase B, the following must exit 0 and be recorded as PASS at the verified build head:

- `scripts/run-roslyn-analyzers.ps1`.
- `scripts/run-cyclomatic-complexity.ps1`, with the configured refactor threshold <= 6.
- `scripts/run-jetbrains-inspectcode.ps1`.
- `scripts/run-property-tests.ps1`.
- `scripts/run-vulnerable-packages.ps1`.
- `dotnet format --verify-no-changes`.
- `dotnet test`.

For the test-only diff with zero changed production C# under `src/`, `scripts/run-mutation.ps1` must report its native exit-2 `SKIPPED (scope-empty)`, nonblocking and never PASS. Do not opt out, touch production to force mutation, or change thresholds. Web gates are N/A. A gate that cannot run has not passed; preserve its actual verdict and route the failure.

No ADR, root `CONTEXT.md` edit or `ASSUMPTIONS.md` is required: this feature consumes existing vocabulary and introduces no architectural trade-off or taste assumption. No new domain glossary terms were settled.

## Dependency-ordered implementation plan

1. Verify Q1 prerequisites on main in the Build workspace and perform pickup drift recon. Record VO bounds, call shapes, genre bridging and predecessor-test overlap. Resolve contradictions before writes; re-run cross-artifact analysis and amend drifted tasks through the prescribed process.
2. Add local generators/shrinkers and the four properties under the Q2/Q5/Q6 contracts. Exercise the new properties with reproducible diagnostics.
3. Curate the typed 30-movie fixture, explicit weights and three seeds; independently derive and document the expected rankings before adding golden assertions against real scorer output.
4. Run the focused suite, the property gate and full suite, then complete refactor, Q8 harness gates, exact-head receipts and mandatory three-axis review/adjudication under the Build pipeline and Q7 limits. Route real dependency failures with replay/counterexample rather than rewriting production, predecessor tests or assertions to obtain green results. The user owns merging.

The ordering follows the prerequisites in Q1, constructive test inputs in Q2/Q6, oracle-before-assertion independence in Q3, and Q8 validation requirements. It does not authorize Phase B in the Spec workspace.

## Phase A handoff

Quill drafts `spec.md`, `plan.md` and `tasks.md` from this brief and the full cited rulings. Keel then performs read-only cross-artifact analysis against this brief; plan challenge, plan freeze, provisional Gate 1, the specs-only PR and user-owned merge remain separate required states. No implementation, gates, ADR, glossary rewrite or new taste artifact was produced by closing this grill.
