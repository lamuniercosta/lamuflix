# DEV-338 grill conclusions

Source: task note DEV-338 and recon-DEV-338, including authoritative ticket section 10. Worktree feature-338-spec; branch feature/338-spec; base HEAD 06ec67774a5a957369780bf7d431f6d4ca3eb900. Patron is the decision owner; Keel records each full exchange. Append only.

---

## Q1 — Zero effective weight

Keel asked: Reconcile zero effective weight with the ticket unconditional identity AC. Existing seven SimilarityWeights allow zero (recon section 4), while ticket requires Score(a,a)=1 for any featured a. Recommend return 1 when both inputs have identical nonempty scoring features even when all applicable weights are zero; otherwise zero when denominator is zero, with a caller-safe identity Reason for the positive fallback. This preserves identity but adds an identity fallback outside the written weighted-mean formula; if you judge that changes ticket scope/design, output blocked: structural with exact owner question rather than silently qualify identity or reject valid zero weights. What is your cited ruling?

Patron answered (full ruling):
[from Patron] Ruling (Q1): Accept Keel's recommendation — when sum(w_i) == 0 over terms with data, return Score = 1.0 with a caller-safe identity Reason if a and b have identical non-empty scoring features, and Score = 0.0 with empty Reasons otherwise.

- **Boundary definition for `0 / 0`, not a ticket change**: `sum(w_i * s_i) / sum(w_i)` (`recon-DEV-338` §10.2 item 2) is indeterminate (`0.0 / 0.0 = NaN`) when all applicable weights are zero (`SimilarityWeights` allows `0.0` via `[Range(0.0, double.MaxValue)]`, `recon-DEV-338` §4, `src/LamuFlix.Core/Options/SimilarityWeights.cs:7-28`), so sound engineering practice requires an explicit zero-denominator boundary rule to keep the output in `[0, 1]`.
- **Preserves unconditional identity and symmetry ACs**: Resolving the zero-denominator boundary to `1.0` when `a` and `b` share identical non-empty scoring features (where every active term has `s_i == 1`) and `0.0` otherwise satisfies `Score(a, b)` symmetry, `[0, 1]` clamping, and `Score(a, a) == 1` when `a` has at least one feature (`recon-DEV-338` §10.2 item 2, §10.3 AC 2).
- **Preserves non-empty caller-safe Reason invariant**: Emitting a caller-safe identity `Reason` on the positive zero-weight identity fallback (`[assumed]` taste copy `Identical feature profile`) and empty `Reasons` when returning `0.0` satisfies the AC that `Reasons` are non-empty whenever `Score > 0` and contain no internal identifiers (`recon-DEV-338` §10.3 AC 3, `specs/PRODUCT.md` §4).

---

## Q2 — Overlap and term eligibility

Keel asked: Define overlap math and missingness. Recommend Director/Writers/Cast use set Jaccard intersection/union with existing MovieFeatures case-insensitive semantics; term has data only when both corresponding sets nonempty, whereas nonempty disjoint sets contribute zero and retain their denominator weight. Scalars need both values. Genres delegate similarity and unknown-genre handling to the DEV-337 GenreWeights contract rather than a second formula. Empty on either side excludes term; wholly unscoreable pair returns zero/empty except Q1. Accept this eligibility/overlap rule, or give exact alternative with cited basis?

Patron answered (full ruling):
[from Patron] Ruling (Q2): Accept Keel's recommendation for overlap math and missingness/eligibility rules across all seven scoring terms.

- **Term eligibility (`terms that have data`) and wholly unscoreable pairs**: A set term (`Directors`, `Writers`, `Cast`, `Genres`) has data only when both `a` and `b` have non-empty sets, and a scalar term (`ReleaseYear`, `Runtime`, `ImdbRating`) has data only when both `a` and `b` have non-null values; missing/empty data on either side excludes that term from both numerator and denominator `sum(w_i)`, and when no term has data across the pair (outside the Q1 non-empty identity fallback) `Score` returns `0.0` with empty `Reasons` (`recon-DEV-338` §10.2 items 1–2, §10.3 AC 2–3, `specs/DEV-336/spec.md` FR-001).
- **Unweighted set overlap (`Directors`, `Writers`, `Cast`)**: When both sets are non-empty, compute unweighted Jaccard `|A intersect B| / |A union B|` using `MovieFeatures`'s `StringComparer.OrdinalIgnoreCase` set semantics (`specs/DEV-336/spec.md` FR-001, FR-003); non-empty disjoint sets have data and contribute `s_i = 0.0` while retaining their `w_i` in `sum(w_i)` (`recon-DEV-338` §10.2 item 2).
- **Genre overlap delegation (`Genres`)**: When both `a.Genres` and `b.Genres` are non-empty, delegate IDF-weighted Jaccard similarity (`sum(w in A intersect B) / sum(w in A union B)`) and unknown/empty-library default weight (`1.0`) handling to the `GenreWeights` contract rather than duplicating genre-similarity math in `SimilarityScorer` (`recon-DEV-338` §10.2 item 1, `specs/DEV-337/spec.md` FR-004–FR-005).

---

## Q3 — Decay and stable normalization

Keel asked: Choose decay constants and numerical behavior. Recommend fixed named year k=0.1 per year and runtime k=0.01 per minute (e-folding scales 10 years and 100 minutes); no new options/configuration or dependency. Use absolute differences in double and Math.Exp(-k*distance), clamp normalized output; stabilize weighted mean by dividing eligible weights by their maximum before accumulation to avoid finite-double overflow. A zero weight never contributes a normal Reason. Accept constants and stable computation, or specify your alternative? Basis: recon section 10.2 formula and existing seven-weight configuration.

Patron answered (full ruling):
[from Patron] Ruling (Q3): Accept Keel's recommendation — use `const double` decay rates `0.1` per year and `0.01` per minute (with no new options or dependencies), `double` absolute differences with max-weight scaling and `[0.0, 1.0]` clamping, and suppress normal per-term `Reason` emission whenever `w_i == 0` or `s_i == 0`.

- **Fixed `const` decay rates (`[assumed]` tuning constants, no new options/deps)**: Declare `const double` year decay `0.1` per year (10-year e-folding scale) and runtime decay `0.01` per minute (100-minute e-folding scale) directly in the scorer (`AGENTS.md` `const` rule for mutated projects) without modifying `SimilarityWeights` or `RecommendationOptions` (`recon-DEV-338` §4, §10.2 item 1, `specs/PRODUCT.md` §2 & §5 items 1, 6).
- **Double arithmetic, max-weight overflow stabilization, and `[0, 1]` clamping**: Compute `Math.Exp(-k * Math.Abs((double)a - (double)b))` for year/runtime proximity and `1.0 - Math.Abs((double)ratingA.Value - (double)ratingB.Value) / 10.0` for rating proximity (`recon-DEV-338` §4, §10.2 item 1); normalize eligible positive weights by `w_i / maxW` before summing `sum((w_i / maxW) * s_i) / sum(w_i / maxW)` so valid large weights up to `double.MaxValue` (`SimilarityWeights.cs:7-28`) cannot overflow to `Infinity`/`NaN`, and clamp the final score to `[0.0, 1.0]` (`recon-DEV-338` §10.2 item 2, §10.3 AC 2).
- **Positive weighted contribution required for normal `Reason`s**: Because each term's contribution to the weighted numerator is `w_i * s_i`, a term contributes more than zero if and only if it has data, `w_i > 0`, and `s_i > 0`; a zero weight (`w_i == 0`) contributes zero and never emits a normal per-term `Reason` (`recon-DEV-338` §10.2 item 3, §10.3 AC 3).


---

## Q4 — Result and Reason contract

Keel asked: Lock Reasons contract, determinism, and caller safety. Recommend public sealed record Reason(string Text) and public sealed record SimilarityResult(double Score, ImmutableList<Reason> Reasons), beside static SimilarityScorer in existing Recommendations feature. One Reason per eligible positive-weight positive-similarity term, fixed order Director/Writers/Cast/Genres/Year/Runtime/Rating. Shared names only, sorted OrdinalIgnoreCase then Ordinal for stable representative spelling from the two inputs; case-equivalent variants use the ordinal-min representative across both sides so swapped inputs emit identical reasons. Labels: Same director: <names>; Shared writers: <names>; Features <names>; Shares <genres>; Similar release years; Similar runtime; Similar rating. Use human-facing feature strings only, no movie IDs, GUIDs, internal keys, paths or score diagnostics; replace control characters with spaces, trim, and use generic term copy if display label empties. Text is plain text, no HTML/Markdown generation; rendering callers escape it. Q1 copy remains Identical feature profile. No UI or HTTP DTO work. Accept this contract/copy, or give precise alternative?

Patron answered (full ruling):
[from Patron] Ruling (Q4): Accept Keel's recommendation for `Reason` and `SimilarityResult` record contracts, deterministic symmetric ordering/spelling, control-character sanitization with generic fallback copy, and `[assumed]` label templates.

- **Type shapes and placement**: Define `public sealed record Reason(string Text)` and `public sealed record SimilarityResult(double Score, ImmutableList<Reason> Reasons)` alongside `public static class SimilarityScorer` in `LamuFlix.Core.Features.Recommendations`, with no UI or HTTP DTO changes (`recon-DEV-338` §2 `DEV-338:5`, §7 item 4, §10.2 item 1; `.specify/memory/constitution.md:133-135,294,426-429`).
- **Deterministic symmetric ordering and caller-safe sanitization**: Emit one `Reason` per eligible term with `w_i > 0` and `s_i > 0` in fixed `SimilarityWeights` order (`Director`, `Writers`, `Cast`, `Genres`, `YearProximity`, `RuntimeProximity`, `RatingBoost`, `recon-DEV-338` §4); for shared set members, pick the `StringComparer.Ordinal`-minimum representative across both inputs per case-insensitive match and sort by `StringComparer.OrdinalIgnoreCase` then `StringComparer.Ordinal`, replacing control characters with spaces and trimming (falling back to generic term copy if all matched display tokens are null/empty after sanitization, since `MovieFeatures` preserves null/blank elements per `specs/DEV-336/spec.md` FR-003) with plain text only and zero internal identifiers (`recon-DEV-338` §10.2 item 3, §10.3 AC 2–3).
- **Label copy (`[assumed]` in `ASSUMPTIONS.md`)**: Accept the human-facing label templates (`Same director: <names>`, `Shared writers: <names>`, `Features <names>`, `Shares <genres>`, `Similar release years`, `Similar runtime`, `Similar rating`, generic fallback copy when sanitized tokens are empty, and Q1 `Identical feature profile`) as `[assumed]` taste decisions (`specs/PRODUCT.md` §4, `recon-DEV-338` §10.2 item 3).


---

## Q5 — Input validation and exact identity

Keel asked: Recommend ArgumentNullException for a/b/weights/genreWeights; ArgumentOutOfRangeException naming weights for any of seven negative or nonfinite values, while valid finite values including zero/MaxValue use Q1-Q3. Identity profile compares only all seven scoring feature values, sets via OrdinalIgnoreCase SetEquals and scalar values exactly; no record-reference equality, movie IDs or other fields. Both truly empty profiles return zero. When every eligible positive term equals one, return exactly 1.0 (not approximate); otherwise use stabilized mean. Do not introduce new VO validation or alter existing options. Accept these pure-function boundary rules?

Patron answered (full ruling):
[from Patron] Ruling (Q5): Accept Keel's recommendation for argument guards, seven-scoring-feature identity comparison, empty-profile handling, and exact `1.0` return when all eligible positive-weight terms equal `1.0`.

- **Null and weight range guards (no edits to existing options/VOs)**: Throw `ArgumentNullException` (`nameof`) for null `a`, `b`, `weights`, or `genreWeights`, and `ArgumentOutOfRangeException` (`nameof(weights)`) if any of the seven `SimilarityWeights` values is `< 0.0` or `!double.IsFinite` (`NaN`/`±Infinity`), while handling all finite weights in `[0.0, double.MaxValue]` via Q1–Q3 without modifying `SimilarityWeights`, `RecommendationOptions`, or domain value objects (`recon-DEV-338` §4, §10.2 item 1, §10.3 AC 2; `.specify/memory/constitution.md:453`; `specs/PRODUCT.md` §5 item 6).
- **Seven-scoring-feature identity and empty-profile rule**: Define scoring-feature identity strictly over the seven scored fields (`Directors`, `Writers`, `Cast`, `Genres` via null-safe `StringComparer.OrdinalIgnoreCase` set equality, plus `ReleaseYear`, `Runtime`, `ImdbRating` exact scalar equality, ignoring non-scored `MetaScore`/`FullPlot`, object reference identity, and movie IDs); when all seven scoring features are empty/null on both inputs, the pair has no scoring features and returns `Score = 0.0` with empty `Reasons` (`recon-DEV-338` §10.2 items 1–2, §10.3 AC 2–3; `specs/DEV-336/spec.md` FR-001, FR-005).
- **Exact `1.0` identity guarantee**: When at least one eligible positive-weight term exists and every eligible positive-weight term has `s_i == 1.0`, return `1.0` directly rather than risking IEEE-754 rounding to `0.9999999999999999`, guaranteeing exact `Score(a, a) == 1` across all valid weight combinations (`recon-DEV-338` §10.2 item 2, §10.3 AC 2).


---

## Q6 — File set, delivery order, and predecessors

Keel asked: Recommend new Core files Features/Recommendations/{SimilarityScorer,SimilarityResult,Reason}.cs only; new tests LamuFlix.UnitTests/Features/Recommendations/{SimilarityScorerTests,SimilarityScorerPropertyTests}.cs with optional focused Reason tests in that folder if needed. No dependency/project/layer/schema/HTTP/OpenAPI/web/playback/config changes; no rewrite of unnamed tracked code. DEV-336 MovieFeatures and DEV-337 GenreWeights must be implemented and merged before this build compiles: Phase A can finish now, Phase B must recon actual contracts first; no stubs, duplication, or implementing predecessors in DEV-338. Task order: dependency pickup check, result/Reason contracts and overlap scoring, proximity/normalization and boundary handling, deterministic Reasons, focused unit/property coverage, whole-diff refactor then Gauge gates. No service/interface abstraction, DI or caching. Accept this file set, sequence, and dependency constraint?

Patron answered (full ruling):
[from Patron] Ruling (Q6): Accept Keel's recommendation for the exact file set, task sequence, no-abstraction/no-DI rule, and predecessor dependency boundary on DEV-336 and DEV-337.

- **Approved file set and §2.3 care items (1–6)**: Restrict production files to `src/LamuFlix.Core/Features/Recommendations/{SimilarityScorer,SimilarityResult,Reason}.cs` and test files to `tests/LamuFlix.UnitTests/Features/Recommendations/{SimilarityScorerTests,SimilarityScorerPropertyTests}.cs` (plus optional `ReasonTests.cs` in the same folder if needed), with no new dependency, project/layer, schema/migration, HTTP/OpenAPI/web surface, `Features:LocalPlay`/secrets/`Process.Start` touch, or edits to unnamed tracked files (`recon-DEV-338` §3, §7 items 1–6; `.specify/memory/constitution.md:133-135,294,308-312`; `specs/PRODUCT.md` §5 items 1–6).
- **Predecessor dependency boundary (`DEV-336` & `DEV-337`) and Phase B gate**: Because `MovieFeatures` (`DEV-336`) and `GenreWeights` (`DEV-337`) have raw count `0` in `src/` at `06ec677` (`recon-DEV-338` §3, §8, §10.4), Phase A completes the specification/plan/tasks now while Phase B remains gated on Epic 7 (`specs/PRODUCT.md:17`) plus merged `DEV-336` and `DEV-337` implementations and must recon their landed signatures at pickup without introducing stubs, duplicate types, or predecessor code in `DEV-338`.
- **Implementation sequence and zero speculative abstractions**: Follow the 6-step sequence (dependency pickup check -> `SimilarityResult`/`Reason` contracts and overlap scoring -> proximity/normalization and boundary handling -> deterministic `Reasons` -> unit/FsCheck property tests -> whole-diff refactor and Gauge gates) with a pure `static` `SimilarityScorer` and no `ISimilarityScorer` interface, service wrapper, DI registration, or caching (`recon-DEV-338` §10.2–10.3; `specs/PRODUCT.md` §2 line 18).


---

## Q7 — Tests and gate expectations

Keel asked: Recommend xUnit v3/Shouldly examples and FsCheck properties in existing unit project, no IO/infrastructure mocks/Testcontainers. Cover each of seven isolated terms with known Jaccard/exp/rating values; genre contract examples including unknown defaults; missing-side exclusion versus disjoint zero inclusion; mixed weights and zero weights; empty and equal profiles; scalar boundaries; null/NaN/infinity/negative guards and MaxValue overflow resistance; reason eligibility/order/swap equality/null-blank-control tokens/plain-text adversarial strings/no injected IDs. Properties: score finite and bounded, score symmetric within explicit 1e-12 floating tolerance, exact identity, deterministic full Reasons under swap, nonempty Reasons when score positive, unchanged inputs. Tag properties for run-property-tests. M build requires all Phase 3 step 5 gates including mutation on changed Core C#, no opt-out/no threshold weakening; Scope-empty SKIPPED and mutation N/A retain exact script verdict. Phase A runs no implementation gates. Accept?

Patron answered (full ruling):
[from Patron] Ruling (Q7): Accept Keel's recommendation for unit and FsCheck property verification coverage, property-test tagging, and Phase A/Phase B gate execution rules.

- **Focused unit test coverage (`SimilarityScorerTests.cs`, xUnit v3 + Shouldly, no IO/Testcontainers)**: Verify each of the 7 isolated terms (`Directors`, `Writers`, `Cast`, `Genres` including `1.0` unknown default, `ReleaseYear`, `Runtime`, `ImdbRating`), missing-side exclusion vs non-empty disjoint zero inclusion, mixed and all-zero weights (`Q1`), empty vs identical 7-field scoring profiles (`Q5`), scalar boundaries, `null`/`NaN`/`±Infinity`/negative guards and `double.MaxValue` overflow stabilization (`Q3`, `Q5`), and `Reason` eligibility, fixed order, swap equality, null/blank/control-character sanitization with generic fallback, adversarial plain text, and zero internal identifiers (`Q4`) (`recon-DEV-338` §6, §10.2–10.3; `.specify/memory/constitution.md:308,314-325`).
- **FsCheck property tests (`SimilarityScorerPropertyTests.cs`, tagged for `run-property-tests.ps1`)**: Prove across generated inputs that `Score` is finite and within `[0.0, 1.0]`, symmetric within `1e-12`, exactly `1.0` for `Score(a, a)` whenever `a` has at least one scoring feature, produces identical `Reasons` under `(a, b)` / `(b, a)` swap, emits non-empty `Reasons` iff `Score > 0.0`, and never mutates inputs (`recon-DEV-338` §6, §10.3 ACs 1–3; `.specify/memory/constitution.md:309`).
- **Gate discipline (`size:M`, Phase A vs Phase B)**: Phase A executes no build/test/analyzer/mutation gates; Phase B runs the full configured verification suite including `./scripts/run-property-tests.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (refactor threshold `6`), and `./scripts/run-mutation.ps1` on changed `LamuFlix.Core` C# with zero opt-outs, zero threshold changes, and exact native verdict reporting (`SKIPPED (scope-empty)` / `N/A` never folded into `PASS`, `AGENTS.md` Verification commands, `.specify/memory/constitution.md:312,402-413`).


---

## Q8 — Loop terms and shared understanding

Keel asked: Recommend closing bar Critical/High block, Medium/Low recorded nonblocking unless they demonstrate failed explicit acceptance criteria (then classify by actual impact). Frozen scope: only the ticket pure 7-term scorer, immutable result/Reason records, caller-safe deterministic reasons, approved input/math boundaries and tests in Q1-Q7; exclude orchestration, candidates, persistence, caching, endpoints, web chips, configuration changes, predecessor implementation and tuning studies; anything else is a follow-up issue, not a finding in this round. Cap plan challenge at two rounds and build review/remediation at two rounds; unresolved above-bar findings after cap are blocked, no third round or threshold relaxation. No structural owner checkbox identified in your Q1-Q7 rulings. No ADR/new glossary needed: existing Genre similarity vocabulary remains canonical, Similarity score means normalized pairwise likeness and Reason means a plain-text explanation of a positive weighted term (plus Q1 identity fallback), kept in brief rather than modifying root glossary outside this spec-only PR. Do you confirm these terms and that shared understanding is reached so I may close brief.md for Quill?

Patron answered (full ruling):
[from Patron] Ruling (Q8): Confirm and ratify — shared understanding is reached at Q8/12; close the grill and write `specs/DEV-338/brief.md` for Quill with zero owner structural checkboxes.

- **Standing loop discipline (cited from chain precedent `specs/DEV-337/CONCLUSIONS.md:81-85`)**: Critical and High findings in frozen scope block, and Medium/Low are non-blocking unless they demonstrate a failed explicit acceptance criterion (classified by actual impact); all three challenger axes (`Sentry`, `Ledger`, `Compass`) apply for `size:M`, capped at 2 rounds for plan challenge and 2 rounds (max 2 fix commits/round) for build review/remediation, with unresolved blockers at cap escalated rather than adding a 3rd round or relaxing any threshold.
- **Frozen scope and exclusions (`recon-DEV-338` §7, §10.2–10.3, Q1–Q7)**: Scope is frozen strictly to the pure 7-term `SimilarityScorer`, `SimilarityResult`, and `Reason` records, deterministic caller-safe `Reasons`, Q1–Q5 boundary/numerical semantics, and the Q6–Q7 unit and FsCheck property tests; orchestration, candidate sourcing, persistence, caching, HTTP endpoints, web chips, config edits, predecessor (`DEV-336`/`DEV-337`) implementations, and tuning studies are excluded (any out-of-scope concern is handled under the charter follow-up policy, not as a round finding), with zero owner structural checkboxes.
- **No ADR and no `CONTEXT.md` edit (`CONTEXT.md:93-97,118-128`, `.specify/memory/constitution.md:294,499-503`)**: Existing root `CONTEXT.md` entries (`Recommendation`, `Reason`, `Genre weight`, `GenreWeights`, `Genre similarity`) already cover the ubiquitous language, and keeping the `Similarity score` / `Reason` scoping notes in `specs/DEV-338/brief.md` avoids touching root `CONTEXT.md` or adding an ADR when no architectural choice or port changes.


---

## Q9 — One-way Reason invariant and fallback copy

Keel asked: Your Q7 property ruling says Reasons nonempty iff Score > 0, but ticket AC is one-way and Q3 scaled arithmetic can underflow a tiny positive contribution to zero when weights span epsilon to MaxValue. Recommend retain exact ticket implication Score > 0 => Reasons nonempty, and retain Q4 emission for eligible w_i>0 and s_i>0 independently of normalized underflow. Do not require empty Reasons for a zero rounded final score if a term had positive raw contribution; still empty for Q1 nonidentical denominator-zero and no contributing terms. Reason eligibility is tested directly. This avoids adding an unintended stronger AC. Also fix generic fallback copies explicitly to Shared director, Shared writers, Shared cast, Shared genres when no nonblank sanitized shared tokens remain; partial empty tokens omitted. Confirm this clarification supersedes Q7 iff wording and shared understanding remains reached?

Patron answered (full ruling):
[from Patron] Ruling (Q9): Confirm and ratify — supersede Q7's `iff` wording with the ticket's one-way implication (`Score > 0.0 => Reasons` non-empty) plus Q3/Q4 per-term emission (`w_i > 0 && s_i > 0`), pin the `[assumed]` generic fallback copy (`Shared director`, `Shared writers`, `Shared cast`, `Shared genres` with partial empty sanitized tokens omitted), and close the grill at 9/12 for `brief.md`.

- **One-way AC implication (`Score > 0.0 => Reasons` non-empty) and per-term eligibility**: Retain the exact ticket AC (`recon-DEV-338` §10.3 AC 3: `Reasons are non-empty whenever the score is greater than zero`) and Q3/Q4 per-term rule (`recon-DEV-338` §10.2 item 3: emit a `Reason` for each eligible term with `w_i > 0` and `s_i > 0` regardless of whether `w_i / maxW` underflows the normalized `double` score to `0.0` at extreme `[double.Epsilon, double.MaxValue]` weight spreads), while returning empty `Reasons` whenever no term has `w_i > 0 && s_i > 0` outside the Q1 zero-denominator identity fallback.
- **Explicit generic fallback copy (`[assumed]` in `ASSUMPTIONS.md`)**: When an eligible positive-overlap set term (`w_i > 0 && s_i > 0`) has some empty sanitized tokens alongside non-blank ones, omit the empty tokens; when no non-blank sanitized shared tokens remain, emit `Shared director`, `Shared writers`, `Shared cast`, or `Shared genres` respectively (`specs/PRODUCT.md` §4, `recon-DEV-338` §10.2 item 3).

