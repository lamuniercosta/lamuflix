# DEV-353 Decision Brief

Status: Phase A grill CLOSED after Q10/12. Patron accepted Q1-Q10 and confirmed shared understanding. Ready for Quill drafting only. This is not plan freeze, provisional Gate 1, merged Gate 1, implementation approval, or evidence of passed gates.

## Identity and authority

- Worktree: F:/Dev/LamuFlix.worktrees/feature-353-spec
- Branch: feature/353-spec
- Evidence HEAD: da034bb1129a8f47d4d42f3beb31b780e8dc288f
- Ticket: DEV-353, Compute recency-weighted TasteProfile from watch history and embeddings; parent DEV-288; size M; no UI.
- Ticket Scope & Technical Design and acceptance criteria: task note DEV-353:27-38, Rigger tracker receipt. Its explicit design is already authoritative under specs/PRODUCT.md:23-24.
- Source facts: recon-DEV-353 and recon-DEV-353-2. Use the verified supplement and its cited worktree sources where earlier recon conflicts. No independent implementation measurements were performed by Keel.
- Product constraints: specs/PRODUCT.md:9,12,17-18,23-24,28-30,36-48, carried in recon-DEV-353 Product constraints section.
- Constitution constraints: .specify/memory/constitution.md:110-141 (boundaries/ports), 268-280 (options/time), 306-327 (test stack), 428 (immutable collections), as carried in recon.
- Full questions and answers: append-only CONCLUSIONS.md. Full cited Patron rulings Q1-Q10 are also reproduced below, so the brief contains every grill answer. Q1/Q2 were preserved verbatim.
- Source citations in historical exchanges retain their original note line numbers; note additions shifted some later line numbers. recon-DEV-353-2:9-58 supersedes the earlier GenreWeights absence/genre-ID statements.

## Ticket delivery and acceptance

Implement a pure Core computation of a recency-weighted average of watched movies' genre weights and plot embeddings. Each dated launch uses exp(-lambda * days); lambda belongs in RecommendationOptions. Clock injection makes fixed-clock/fixed-input calculation deterministic. No history returns an empty profile without throwing for valid input; callers treat it as no personal signal. History/marks originate from DEV-351/352, embeddings are supplied from DEV-413 by the eventual caller, and caching is split to DEV-415.

Acceptance: recent dated watches outweigh older ones with exponential decay; valid empty history returns an empty profile; fixed clock and input yield deterministic output. The Q2 undated-manual-mark rule below is explicitly approved by Patron, not an inferred timestamp.

## Verified predecessor contracts and drift boundary

- GenreWeights EXISTS at this HEAD: Core/Domain/GenreWeights.cs:9,17,61; Calculate(GenreCounts), GetWeight(string), case-insensitive keys, unknown/blank fallback 1.0. Source: recon-DEV-353-2:30-44. Earlier recon claiming absence or integer genre IDs is stale.
- Movie.Metadata can be null; MovieMetadata.Genres is ImmutableArray<string>, not IDs. Source: recon-DEV-353-2:21-28.
- DEV-413 repository and MovieFeaturesSnapshot are draft ADR contracts, absent from src at this HEAD. Draft GetByMovieIdAsync(MovieId,CancellationToken) returns Task<MovieFeaturesSnapshot?>; snapshot carries MovieId, ImmutableArray<float> Vector, DateTimeOffset ComputedAt. The draft requires 384 finite elements, allows zero vectors and null reads when missing; normalization is not established. Source: recon-DEV-353-2:9-19. Do not implement or publish these contracts in DEV-353.
- DEV-351 defines launch events with MovieId/PlayedAt, preserves same-instant multiplicity and has a write-only IPlaybackHistory contract. DEV-352 marks are undated booleans on movies, not a movie_feedback table. Source: recon-DEV-353 verified playback/feedback sections. No read port or feedback timestamp is added here.
- Before Phase B, verify current main/landed predecessor contracts, PRODUCT.md:17 Epic 7 sequencing, live harness and existing property-test discovery. Report missing facts as needs recon; substantive drift needs a cited ruling before changing the decisions. No inherited owner checkbox is closed by this grill.

## Settled computation and contracts

### Q1 — Recency option

Add double RecencyDecayLambda to existing RecommendationOptions. Unit: inverse days. Default: Math.Log(2.0)/30.0 (30-day half-life). Finite and strictly positive, validated through the options validation path and calculator constructor. Keep MmrLambda separate. The default tuning baseline is logged in ASSUMPTIONS.md at Patron request.

### Q2 — Watch-signal inclusion

Each PlayedAt entry contributes separately, including repeated instants, regardless of IsWatched. If and only if a movie has no launch entries and IsWatched is true, contribute one undated signal of weight 1.0/age zero. It has no fabricated timestamp. IsWatched true adds nothing when launch history already exists. IsWatched false does not erase recorded launches.

### Q3 — Types and invocation

Existing src/LamuFlix.Core/Domain/ gains sealed records:

- TasteProfileMovie(MovieId MovieId, ImmutableArray<string> Genres, ImmutableArray<float> PlotEmbedding, ImmutableArray<DateTimeOffset> PlayedAt, bool IsWatched).
- TasteProfile(ImmutableDictionary<string,double> GenrePreferences, ImmutableArray<float> PlotEmbedding), with computed IsEmpty when both channels are empty.

Existing src/LamuFlix.Core/Features/Recommendations/ gains sealed TasteProfileCalculator(TimeProvider, RecommendationOptions), exposing synchronous TasteProfile Calculate(ImmutableArray<TasteProfileMovie> movies, GenreWeights genreWeights).

The caller materializes values. Calculator performs no I/O, repository reads, caching, DI registration, route or handler integration. No feature-folder cross-reference is needed for PlaybackEvent. The eventual caller sources embeddings through landed DEV-413; this ticket does not fabricate the unlanded repository/snapshot. No ticket or constitution departure was ruled.

### Q4 — Genre preferences

Trim genre names, discard blanks and deduplicate OrdinalIgnoreCase per movie. Canonical key for a case-insensitive group is the Ordinal-minimum trimmed name across supplied names. For each genre-bearing signal s of movie m, x_m(g)=genreWeights.GetWeight(g) when g is in its genre set, otherwise zero. GenrePreferences(g)=sum(w_s*x_m(g))/sum(w_s), with denominator over genre-bearing signals only. Missing genres do not dilute this channel. Other-genre signals contribute implicit zeros for g. No per-movie genre-count division, L1 normalization or SimilarityWeights.Genres multiplier. Result uses an OrdinalIgnoreCase immutable dictionary; omit exact-zero entries. No contributing signals means empty dictionary.

### Q5 — Plot centroid and emptiness

For each coordinate j, mean[j]=sum(w_s*embedding_s[j])/sum(w_s) over signals with provided valid 384-float embeddings only. Missing vectors do not dilute this channel. Use double accumulation and float output; no input/output normalization or similarity scalar. Valid zero vectors participate in the denominator. Empty plot contributors, or a mean whose 384 float coordinates are all zero, return ImmutableArray<float>.Empty. A populated genre channel survives missing/cancelling plot data and vice versa. No launch and no true watched mark produces empty channels even when metadata is present.

### Q6 — Time, stability and determinism

Sample TimeProvider.GetUtcNow() exactly once per Calculate call. Compute fractional age=Math.Max(0.0,(now-playedAt).TotalDays) over DateTimeOffset instants, so future timestamps clamp to zero; undated manual signals have age zero.

For each channel independently, find minimum contributor age a_min and evaluate scaledWeight=Math.Exp(-RecencyDecayLambda*(age-a_min)), subtracting before multiplying. This common-factor cancellation preserves the weighted mean while preventing an all-old history from producing a zero denominator. No cutoff, lambda cap or false cold start. Relative underflow to zero is allowed at machine precision. Use supplied movies and PlayedAt index order; no permutation sorting or promise of bitwise equality across permutations. Repeated calculation with the same clock and ordered values is bitwise deterministic. Canonical genre casing follows Q4/Q6.

### Q7 — Validation

Default/empty immutable arrays mean empty/absent and are valid. Constructor null TimeProvider or RecommendationOptions, and Calculate null GenreWeights, throw ArgumentNullException. Nonfinite or nonpositive lambda throws ArgumentOutOfRangeException in the constructor, in addition to options validation. Do not require unrelated Take/MMR fields to be valid for this calculation.

Validate the complete materialized movie collection before accumulation. Null rows, duplicate MovieId rows, provided nonempty vectors with length other than EmbeddingDimensions.Value, or nonfinite floats throw ArgumentException on movies. These failures also apply to unwatched rows. Do not add MovieId checks beyond the existing value-object invariant. Preserve duplicate launch timestamps; do not mutate caller inputs. Empty-history no-throw covers valid inputs, not corrupted vectors.

### Q8 — Positive history scope

No IsNotInterested field, negative-weight formula or candidate exclusion in this calculator. Watched history remains positive history independently of later negative marks. No schema/history-reader/port/repository/ADR/cache/API/UI/DI/handler/config edits, LocalPlay or process execution. Pre-Phase-B drift and scheduling checks remain mandatory.

## Q9 — Test strategy

Use the existing tests/LamuFlix.UnitTests project, xUnit v3, Shouldly and Theory/MemberData under existing test conventions. No new package, test project, DB or I/O fixture. A test-local BCL TimeProvider subclass counts clock reads. Use existing fixture/data tooling where appropriate. Property-test discovery conventions must be verified mechanically during Phase B; no new library is asserted as installed and no property-test opt-out is authorized.

Independent oracles include 0/30-day watches giving weights 1/0.5 and mixture 2/3 vs 1/3, fractional days, offset-equivalent timestamps, rarity values from supplied GenreCounts, unnormalized 384-coordinate basis vectors, repeated launch timestamps, manual fallback without double-counting, separate channel denominators, missing/zero/opposing vectors, every validation boundary, default/empty input, genre canonicalization, repeated-call determinism, exactly one clock read, unchanged MmrLambda and input immutability. Cover old-only histories, huge finite lambda and relative underflow without copying the implementation as the oracle.

Properties: finite outputs, per-channel convex bounds, identical-source idempotence, uniform-replication invariance within floating tolerance and fixed-clock determinism over valid deterministic bounded generated histories. Property fixtures must respect unique movie rows and the Q2 fallback rules; replication concerns watch-signal multiplicity rather than duplicating MovieId rows. Compare coordinates and dictionary entries, not record equality over immutable-array references. No DB/API round-trip test is needed for this pure computation.

## Q10 — Loop discipline

Closing bar: Critical, High and Medium findings block. Low findings are recorded and nonblocking. Missing required axis, failed/unrunnable required gate and actual unresolved ticket/constitution departure independently block. Unnecessary abstraction is follow-up, never a fix commit.

Frozen scope: pure materialized-input TasteProfile computation with Q1-Q9 exact contracts/semantics, RecommendationOptions.RecencyDecayLambda and its validation, and the prescribed unit/property tests; anything else is a follow-up issue, not a finding in this round.

Round cap: two for plan correction/challenge and two for formal review/remediation at their respective stages. Remaining above-bar findings after the cap stop and report to Bernstein. No third round or automatic waiver.

## Exact file envelope

Future Phase B production/test edits, subject to fresh drift recon:

1. src/LamuFlix.Core/Domain/TasteProfileMovie.cs
2. src/LamuFlix.Core/Domain/TasteProfile.cs
3. src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs
4. src/LamuFlix.Core/Options/RecommendationOptions.cs — ticket-forced addition/validation only.
5. tests/LamuFlix.UnitTests/Domain/TasteProfileTests.cs
6. tests/LamuFlix.UnitTests/Features/Recommendations/TasteProfileCalculatorTests.cs
7. tests/LamuFlix.UnitTests/Options/RecommendationOptionsRecencyTests.cs
8. tests/LamuFlix.UnitTests/Features/Recommendations/TasteProfileProperties.cs

Decision documents authorized for Keel: specs/DEV-353/CONCLUSIONS.md (append-only), ASSUMPTIONS.md, brief.md and feature-local CONTEXT.md. Quill drafts spec.md, plan.md and tasks.md from them and both recon notes. No global glossary edit or L-sized ADR is authorized/needed for this M ticket. No unnamed existing-file rewrite or new dependency/project/architectural layer is approved.

## Task ordering and gate expectations

1. Phase B read-only pickup: verify current main/predecessor/options/genre/embedding contracts, Epic 7 scheduling, exact harness settings and existing property-test discovery. Preserve independent owner/ADR statuses. Stop on missing facts or unruled substantive drift.
2. Define immutable domain contracts and recency option validation, with corresponding tests.
3. Implement watch-signal construction, channel formulas and calculator using independent unit oracles; preserve synchronous materialized-input boundary.
4. Complete validation, numeric stability, determinism, immutability and property coverage. No opt-out for these domain invariants.
5. Refactor, then run applicable live pipeline gates and all required review axes at their proper stages.

Expected applicable gates: Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable packages, format verification, dotnet test and changed-production Core mutation. Read thresholds/configuration from live harness.yml; never lower them. Web has scope-empty classification if unchanged. Exit 0 earns the tool's passing verdict; exit 1 and Could not run block. Preserve exit-2 SKIPPED (scope-empty), harness-disabled SKIP, mutation N/A and any accepted opt-out distinctions; never label them PASS. Property opt-out is not accepted here. No gate has been run or passed by this grill.

## Drafting handoff

Grill closed by Patron Q10; no unresolved DEV-353 ticket/constitution departure was ruled. Canonical local glossary in CONTEXT.md and every cited ruling below are ready to feed Quill's specification. Quill drafts spec/plan/tasks; Keel later performs read-only speckit-analyze and brief conformance check. Plan challenge/freeze, provisional Gate 1, user spec-PR merge and Phase B approval remain separate future actions. The Conductor routes that handoff; this document does not instruct another seat to start work by itself.

## Full cited Patron answers Q1-Q10

The following are verbatim copies of the full ruling files received during the grill. They supplement, rather than replace, the full questions and answers in CONCLUSIONS.md.

> **Amendment (Phase 4 MED-1 ruling, `specs/DEV-353/CONCLUSIONS.md:172`):** every `tests/LamuFlix.Test` path quoted in the Q9/Q10 rulings below is superseded and reads as `tests/LamuFlix.UnitTests`. The quoted text is preserved verbatim as history; the four DEV-353 test files live in `tests/LamuFlix.UnitTests` with unchanged file names. No move back to `tests/LamuFlix.Test`, no new project, and no ticket or constitution departure.

---

### Patron Q1

[from Patron] RULING (DEV-353 Q1/12): ACCEPT — Add `double RecencyDecayLambda` to `RecommendationOptions` (unit: inverse days, default: `Math.Log(2.0) / 30.0` for a 30-day half-life, validation range: finite and strictly `> 0.0`); keep `MmrLambda` (`0.7`) separate and unchanged. No ticket or constitution departure (`blocked: structural` does not apply).

- **Ticket Authority & Forced Edit Scope**: The ticket's *Scope & Technical Design* (`DEV-353` note:28-30) explicitly mandates `Each watch gets weight e^(-lambda * days), with lambda in RecommendationOptions`, which is authoritative (`specs/PRODUCT.md:23-24`). Because `src/LamuFlix.Core/Options/RecommendationOptions.cs:11-49` (`recon-DEV-353:164-171`) currently contains only `Take`, `CandidatePoolSize`, `MmrLambda`, and `Weights`, adding `RecencyDecayLambda` to `RecommendationOptions.cs` is a forced edit required by the ticket's design and acceptance criteria (§2.3 care item 6; `specs/PRODUCT.md:48`), avoiding any conflation with MMR diversity (`MmrLambda`).
- **Name, Unit, Default & Validation Implications**: `RecencyDecayLambda` in inverse days ($\text{days}^{-1}$) matches the exponent `-lambda * days` (`DEV-353` note:30). Enforcing finite and strictly positive (`> 0.0`) via options validation (`constitution.md:268-272`, `recon-DEV-353:125`) guarantees recent watches weigh strictly more than older watches (`DEV-353` note:35), and defaulting to `Math.Log(2.0) / 30.0` (30-day half-life) sets a sensible tuning baseline (`[assumed]` default value for `specs/DEV-353/ASSUMPTIONS.md`).
- **No Ticket or Constitution Departure**: Adding the missing property implements the existing ticket text (`DEV-353` note:30) and complies with Constitution Principle VII (`constitution.md:268-280`, `recon-DEV-353:125`), so neither escalation trigger in `specs/PRODUCT.md:36-39` applies.


---

### Patron Q2

[from Patron] RULING (DEV-353 Q2/12): ACCEPT — Each `PlaybackEvent` contributes a distinct dated watch signal weighted by `Math.Exp(-RecencyDecayLambda * days)` from its `PlayedAt` regardless of whether `IsWatched` is `false`; an `IsWatched = true` movie contributes one undated positive watch signal with weight `1.0` (`days = 0`) if and only if that movie has no `PlaybackEvent` entries, with no fabricated timestamp, no schema change, and no new read port in this pure Core ticket. No ticket or constitution departure (`blocked: structural` does not apply).

- **Reconciling `DEV-353` Inputs with `DEV-351` & `DEV-352` Contracts**: `DEV-353` note:33 requires watched-movie inputs from both `playback_events` (`DEV-351`) and `movie_feedback` (`DEV-352`), while `DEV-352` FR-001/004/005 (`recon-DEV-353:150-162`) intentionally stores `IsWatched` as an undated boolean on `movies` with no `movie_feedback` table or timestamp. Evaluating an `IsWatched = true` movie with no `PlaybackEvent` at `days = 0` (`Math.Exp(-lambda * 0) = 1.0`, once per movie) honors both `DEV-353` note:30,33 and `DEV-352` FR-004/005 without introducing out-of-scope schema changes (`specs/PRODUCT.md:45`, §2.3 care item 3) or unapproved ports (`constitution.md:136-141`, `recon-DEV-353:123`).
- **Dated `PlaybackEvent` Multiplicity & `IsWatched = false` Semantics**: Per `DEV-351` FR-002/003 (`recon-DEV-353:150-162`), recorded `PlaybackEvent` launches (`MovieId`, `PlayedAt`) are distinct historical watch events that each decay by `Math.Exp(-RecencyDecayLambda * days)` (`DEV-353` note:30,35). When `PlaybackEvent` records exist for a movie, `IsWatched = true` does not add an extra undated signal, and `IsWatched = false` means only that no manual watched mark is asserted—it does not erase historical `PlaybackEvent` contributions.
- **No Ticket or Constitution Departure**: Every dated watch decays strictly by `e^(-lambda * days)` so recent watches weigh more than older watches (`DEV-353` note:30,35), undated manual `IsWatched = true` marks are included without fabricating timestamps (`DEV-353` note:33; `DEV-352` FR-004/005), and the computation remains a pure Core function (`DEV-353` note:38; `constitution.md:110-141`), so neither owner-escalation trigger in `specs/PRODUCT.md:36-39` applies.


---

### Patron Q3

[from Patron] RULING (DEV-353 Q3/12): ACCEPT — Define `sealed record TasteProfileMovie(MovieId MovieId, ImmutableArray<string> Genres, ImmutableArray<float> PlotEmbedding, ImmutableArray<DateTimeOffset> PlayedAt, bool IsWatched)` and `sealed record TasteProfile(ImmutableDictionary<string, double> GenrePreferences, ImmutableArray<float> PlotEmbedding)` (with computed `bool IsEmpty` when both channels are empty) in `src/LamuFlix.Core/Domain/`, and `sealed class TasteProfileCalculator(TimeProvider, RecommendationOptions)` with synchronous `Calculate(ImmutableArray<TasteProfileMovie> movies, GenreWeights genreWeights)` in `src/LamuFlix.Core/Features/Recommendations/`. Materialized inputs satisfy the ticket with no owner checkbox (`blocked: structural` does not apply).

- **Pure Core Boundary & Materialized Inputs Meet the Ticket (`DEV-353` note:28-38)**: The ticket explicitly scopes `DEV-353` as a "pure Core function" that is "deterministic for a fixed clock and input" with caching/orchestration split to `DEV-415` (`DEV-353` note:31,37,38). Because a pure Core function performs no async I/O and `DEV-413` (`IMovieFeaturesRepository` / `MovieFeaturesSnapshot`) and `DEV-351` (`PlaybackEvent`) are not yet landed in `src/` (`recon-DEV-353-2:11-19`), accepting materialized `ImmutableArray<TasteProfileMovie>` and `GenreWeights` fulfills `DEV-353` note:28-38 while leaving async repository projection through `DEV-413`/`DEV-351`/`DEV-352` to the caller (`DEV-415`), without fabricating unlanded contracts or changing ticket delivery (`specs/PRODUCT.md:23-24, 36-38`).
- **Domain Placement & Immutable Collection Compliance (§2.3 item 2; `constitution.md:110-141, 428`)**: Placing `TasteProfileMovie` and `TasteProfile` in existing `src/LamuFlix.Core/Domain/` and `TasteProfileCalculator` in existing `src/LamuFlix.Core/Features/Recommendations/` introduces no new project, top-level folder, port, or speculative abstraction (`specs/PRODUCT.md:18, 44`; `constitution.md:110-141`). Using `ImmutableArray<T>` and `ImmutableDictionary<string, double>` honors `constitution.md:428` (`recon-DEV-353:128`), and treating default/empty `PlotEmbedding` as absent while validating provided vectors as `EmbeddingDimensions.Value` (`384`, `recon-DEV-353:145`) finite floats aligns with `Core/Ports/EmbeddingDimensions.cs:7`.
- **Synchronous Clock Sampling & Stryker Static-Init Safety (`constitution.md:279-280`; `AGENTS.md`)**: `TasteProfileCalculator` takes injected `TimeProvider` and `RecommendationOptions` (`constitution.md:268-280`), samples `TimeProvider.GetUtcNow()` once per synchronous `Calculate` invocation, and computes `TasteProfile.IsEmpty` as a getter property (`GenrePreferences.IsEmpty && PlotEmbedding.IsDefaultOrEmpty`) so no mutable static state is introduced for Stryker (`AGENTS.md`).


---

### Patron Q4

[from Patron] RULING (DEV-353 Q4/12): ACCEPT — For each eligible watch signal $s$ (weight $w_s$) of a movie $m$ with non-empty canonical genre set $G_m$ (trimmed nonblank strings deduplicated by `StringComparer.OrdinalIgnoreCase`, deterministic representative casing via `OrdinalIgnoreCase` then `Ordinal`), define $x_m(g) = \text{genreWeights.GetWeight}(g)$ for $g \in G_m$ and $0$ otherwise, and compute $\text{GenrePreferences}[g] = \left(\sum_{s \in S_{\text{genre}}} w_s \cdot x_{m(s)}(g)\right) / \left(\sum_{s \in S_{\text{genre}}} w_s\right)$ over genre-bearing watch signals $S_{\text{genre}}$ (`G_m` non-empty), returning an `ImmutableDictionary<string, double>` keyed with `StringComparer.OrdinalIgnoreCase` (empty when $S_{\text{genre}} = \emptyset$), with no per-movie $|G_m|$ division, no L1 output normalization, and no `SimilarityWeights.Genres` multiplier. No ticket or constitution departure (`blocked: structural` does not apply).

- **Exact Recency-Weighted Average & Channel-Specific Denominator (`DEV-353` note:29, 32, 35-36)**: Defining $x_m(g) = \text{genreWeights.GetWeight}(g)$ for $g \in G_m$ and $0$ for $g \notin G_m$ and dividing by $\sum_{s \in S_{\text{genre}}} w_s$ implements the literal recency-weighted average of watched movies' genre weights (`DEV-353` note:29). Omitting movies with no nonblank genres ($G_m = \emptyset$) from $S_{\text{genre}}$ prevents missing metadata (`Movie.Metadata` is nullable; `recon-DEV-353-2:23-38`) from artificially diluting genre preferences toward zero, while returning an empty `OrdinalIgnoreCase` `ImmutableDictionary<string, double>` when $S_{\text{genre}} = \emptyset$ satisfies cold-start/empty-channel behavior (`DEV-353` note:32, 36; `constitution.md:428`).
- **Alignment with `DEV-337` `GenreWeights` & `SimilarityWeights.Genres` Separation (`recon-DEV-353-2:23-38`; `recon-DEV-353:131-143`)**: Trimming whitespace, ignoring blank strings, and deduplicating per-movie genres with `StringComparer.OrdinalIgnoreCase` matches `GenreWeights`'s `OrdinalIgnoreCase` lookup and `1.0` unknown fallback (`recon-DEV-353-2:23-38`). Keeping raw `GenreWeights.GetWeight(g)` values without dividing by $|G_m|$, L1-normalizing, or multiplying by `SimilarityWeights.Genres` preserves the separation between per-genre library rarity (`DEV-337` FR-004/005) and downstream scoring coefficients (`recon-DEV-353:133-143`).
- **Deterministic Canonical Casing & Floating-Point Accumulation (`DEV-353` note:37)**: Selecting representative genre keys deterministically (`OrdinalIgnoreCase` then `Ordinal`) and accumulating in a stable order guarantees bit-for-bit deterministic output for a fixed clock and input (`DEV-353` note:37), with no ticket or constitution departure (`specs/PRODUCT.md:36-39`).


---

### Patron Q5

[from Patron] RULING (DEV-353 Q5/12): ACCEPT — Compute the plot channel as the coordinate-wise recency-weighted arithmetic mean $v[j] = \left(\sum_{s \in S_{\text{plot}}} w_s \cdot \text{embedding}_{m(s)}[j]\right) / \left(\sum_{s \in S_{\text{plot}}} w_s\right)$ for $j \in [0, 384)$ using `double` accumulation cast to `float` (`ImmutableArray<float>`), over eligible watch signals $S_{\text{plot}}$ with a supplied 384-element finite `PlotEmbedding`, with no input/output L2 normalization and no similarity-weight scalar; include valid 384-element zero vectors in $S_{\text{plot}}$, return `ImmutableArray<float>.Empty` when $S_{\text{plot}} = \emptyset$ or when all 384 mean coordinates equal `0.0f`, and set `TasteProfile.IsEmpty = true` only when both `GenrePreferences` and `PlotEmbedding` are empty (including when movies exist in the input list with no dated launches and `IsWatched == false`). No ticket or constitution departure (`blocked: structural` does not apply).

- **Coordinate-Wise Recency-Weighted Average & `DEV-413` Alignment (`DEV-353` note:29, 33; `recon-DEV-353-2:12-18`)**: Accumulating $v[j] = \left(\sum_{s \in S_{\text{plot}}} w_s \cdot \text{embedding}_{m(s)}[j]\right) / \left(\sum_{s \in S_{\text{plot}}} w_s\right)$ in `double` before casting to `float` implements the literal recency-weighted average of plot embeddings (`DEV-353` note:29) across all `EmbeddingDimensions.Value = 384` coordinates (`src/LamuFlix.Core/Ports/EmbeddingDimensions.cs:7`, `recon-DEV-353:145`) while minimizing rounding drift (`DEV-353` note:37). Honoring `DEV-413` ADR:13-15 (`recon-DEV-353-2:12-18`), no normalization requirement or similarity scalar is imposed, and valid 384-element zero vectors participate in the weighted mean.
- **Missing Embeddings, Zero Centroids & Channel Independence (`DEV-353` note:29, 32)**: Omitting movies with default/empty `PlotEmbedding` from $S_{\text{plot}}$ prevents un-embedded movies from diluting the plot centroid while keeping them eligible for `GenrePreferences` (and vice versa). If $S_{\text{plot}} = \emptyset$ or the resulting 384-float centroid is all `0.0f` (which has undefined cosine direction and carries no personal plot signal; `DEV-353` note:32, `recon-DEV-353:189`), exposing `ImmutableArray<float>.Empty` cleanly signals an empty plot channel without discarding any populated `GenrePreferences`.
- **Cold-Start & No-Watch-Signal Semantics (`DEV-353` note:32, 36; `constitution.md:428`)**: When `movies` is empty or every supplied movie has `PlayedAt.IsDefaultOrEmpty` and `IsWatched == false`, no watch history exists (`S_{\text{genre}} = \emptyset` and `S_{\text{plot}} = \emptyset`), so `Calculate` returns empty `GenrePreferences` and `ImmutableArray<float>.Empty` (`IsEmpty == true`) without throwing (`DEV-353` note:32, 36), with no ticket or constitution departure (`specs/PRODUCT.md:36-39`).


---

### Patron Q6

[from Patron] RULING (DEV-353 Q6/12): ACCEPT — Sample `now = timeProvider.GetUtcNow()` once per `Calculate` call, compute `age = Math.Max(0.0, (now - playedAt).TotalDays)` across UTC instants (clamping future timestamps to `0.0`, with undated `IsWatched` fallback signals also at `age = 0.0`), stabilize each channel's weighted average independently via `a_min = min(age)` and `scaledWeight = Math.Exp(-RecencyDecayLambda * (age - a_min))` (subtracting `a_min` before multiplying by `RecencyDecayLambda`), omit exact-zero genre entries (`<= 0.0`) from `GenrePreferences`, accumulate in supplied `movies` and `PlayedAt` index order without permutation sorting, and choose each `OrdinalIgnoreCase` genre group's canonical key as the `StringComparer.Ordinal` minimum among trimmed names in that group. No ticket or constitution departure (`blocked: structural` does not apply).

- **Clock Sampling, Fractional `TotalDays` & Future-Timestamp Clamp (`DEV-353` note:30-31, 35, 37; `constitution.md:279-280`)**: Sampling `timeProvider.GetUtcNow()` once per call and computing fractional `(now - playedAt).TotalDays` on `DateTimeOffset` UTC instants fulfills injected-clock determinism (`DEV-353` note:31, 37; `constitution.md:279-280`). Clamping negative elapsed durations (`playedAt > now`) to `age = 0.0` prevents clock-skew timestamps from producing unbounded weights `> 1.0` while preserving strict exponential decay for older watches (`DEV-353` note:30, 35).
- **Algebraically Exact Shift-Before-Multiply Stabilization (`DEV-353` note:29-30, 32, 36)**: In each channel independently, factoring out $\exp(-\lambda a_{\min})$ via $\text{scaledWeight} = \exp(-\lambda \cdot (\text{age} - a_{\min}))$ with $(\text{age} - a_{\min})$ evaluated before multiplying by $\lambda$ is algebraically identical to $\left(\sum w_s x_s\right) / \left(\sum w_s\right)$ (`DEV-353` note:29-30), guarantees the newest contributor has weight `1.0` so the denominator is at least `1.0` (never `0.0`/`NaN` from underflow on old history or large finite `RecencyDecayLambda`), avoids inventing an age cutoff or false cold-start (`DEV-353` note:32, 36), and omits `0.0` genre entries if relative weights underflow at `double` precision.
- **Fixed-Input Determinism & Canonical Genre Key (`DEV-353` note:37; `recon-DEV-353-2:23-38`)**: Iterating `movies` and `PlayedAt` in supplied `ImmutableArray` order (preserving duplicate launch timestamps per `DEV-351` FR-003 and Q2) and selecting the `StringComparer.Ordinal` minimum trimmed name within each `StringComparer.OrdinalIgnoreCase` genre group guarantees bitwise-deterministic results for a fixed clock and input (`DEV-353` note:37) with no ticket or constitution departure (`specs/PRODUCT.md:36-39`).


---

### Patron Q7

[from Patron] RULING (DEV-353 Q7/12): ACCEPT — Treat default/empty `movies`, `Genres`, `PlayedAt`, and `PlotEmbedding` `ImmutableArray` values as valid empty/absent inputs that succeed without throwing; reject `null` `TimeProvider` or `RecommendationOptions` in `TasteProfileCalculator` constructor and `null` `GenreWeights` in `Calculate` with `ArgumentNullException`; reject non-finite or `<= 0.0` `RecencyDecayLambda` in `TasteProfileCalculator` constructor with `ArgumentOutOfRangeException` (and validate `RecencyDecayLambda` in `RecommendationOptions` without coupling `TasteProfileCalculator` to unrelated option fields); and validate all supplied `movies` rows before accumulation—rejecting `null` elements, duplicate `MovieId` rows, non-empty `PlotEmbedding` with `Length != EmbeddingDimensions.Value` (`384`), or non-finite (`NaN`/`Infinity`) embedding floats with `ArgumentException`—while preserving duplicate `PlayedAt` entries, adding no extra `MovieId` checks, and never mutating caller inputs. No ticket or constitution departure (`blocked: structural` does not apply).

- **Cold-Start & Default `ImmutableArray` Resilience (`DEV-353` note:32, 36; `constitution.md:428`)**: Accepting `default` (`IsDefaultOrEmpty`) or empty `movies`, `Genres`, `PlayedAt`, and `PlotEmbedding` arrays without throwing ensures uninitialized `ImmutableArray<T>` struct values on valid cold-start or partial-metadata rows cleanly yield empty channels (`DEV-353` note:32, 36), while preserving duplicate `PlayedAt` timestamps within a single `TasteProfileMovie` (`DEV-351` FR-003; Q2).
- **Constructor & Options Preconditions (`constitution.md:268-280`; `recon-DEV-353:164-171`)**: Guarding `TimeProvider` and `RecommendationOptions` with `ArgumentNullException` and enforcing finite `RecencyDecayLambda > 0.0` with `ArgumentOutOfRangeException` in `TasteProfileCalculator` (alongside `RecommendationOptions` validation) satisfies Constitution Principle VII (`constitution.md:268-280`) and Q1 without coupling `TasteProfileCalculator` to unrelated `Take`, `CandidatePoolSize`, or `MmrLambda` fields.
- **Upfront Materialized-Row Validation & Immutability (`recon-DEV-353:145`; `recon-DEV-353-2:12-18`)**: Rejecting `null` `GenreWeights` (`ArgumentNullException`) and validating every supplied `TasteProfileMovie` row upfront with `ArgumentException` for `null` elements, duplicate `MovieId` rows (Preventing split launch/watch-mark contradictions for the same movie), `PlotEmbedding.Length != EmbeddingDimensions.Value` (`384`, `Core/Ports/EmbeddingDimensions.cs:7`), or non-finite floats (`DEV-413` ADR:13-15) enforces contract integrity before accumulation without mutating caller state or departing from the ticket/constitution (`specs/PRODUCT.md:36-39`).


---

### Patron Q8

[from Patron] RULING (DEV-353 Q8/12): ACCEPT — Keep `IsNotInterested`, negative-weight transformation, and candidate filtering out of `DEV-353` (watched history via `PlayedAt` and `IsWatched` per Q2 is the sole `DEV-352` watch-signal input); confine `DEV-353` deliverables strictly to `TasteProfileMovie`, `TasteProfile`, `TasteProfileCalculator`, `RecommendationOptions.RecencyDecayLambda` (plus validation), and tests, with no `DEV-351`/`DEV-352` schema/persistence, no `DEV-413` port/repository/ADR, no `DEV-415` cache, no DI/handler/API/UI/config-file wiring, and a mandatory pre-Phase-B drift check against landed predecessors without clearing inherited owner checkboxes. No ticket or constitution departure (`blocked: structural` does not apply).

- **Positive Watch-History Scope vs `IsNotInterested` (`DEV-353` note:28-33; `recon-DEV-353:159-164`)**: `DEV-353` note:29-33 defines the computation exclusively as the recency-weighted average of *watched movies'* genre weights and plot embeddings (`Each watch gets weight e^(-lambda * days)`), where `DEV-352` supplies the `IsWatched` watched-movie mark (`recon-DEV-353:159-164`). Excluding `IsNotInterested`, negative weights, and candidate filtering from `TasteProfileMovie`/`TasteProfileCalculator` preserves Q2's independent watched-mark semantics and avoids inventing speculative negative-taste formulas (`specs/PRODUCT.md:18, 23-24`).
- **Strict Deliverable & Care-List Boundaries (`DEV-353` note:38; `specs/PRODUCT.md:43-48`; `constitution.md:110-141`)**: Delivering only the pure Core types (`TasteProfileMovie`, `TasteProfile`), `TasteProfileCalculator`, `RecommendationOptions.RecencyDecayLambda` validation, and unit/property tests—with no schema migration, history reader, `DEV-413` port/ADR publication (`recon-DEV-353-2:11-19`), `DEV-415` cache (`DEV-353` note:38), DI/handler/API/UI integration, config-file edits, or `Features:LocalPlay`/`Process.Start` changes—strictly respects `specs/PRODUCT.md:43-48` (§2.3 items 1-6) and `constitution.md:110-141`.
- **Pre-Phase-B Drift Check & Owner-Checkbox Isolation (`specs/PRODUCT.md:17, 36-39`)**: Requiring a pre-Phase-B contract drift check against landed `DEV-337`/`DEV-413`/`RecommendationOptions` contracts and respecting `specs/PRODUCT.md:17` sequencing ensures clean integration, while keeping inherited `DEV-352` owner checkboxes and `DEV-413` draft ADRs untouched (`specs/PRODUCT.md:36-39`), with no ticket or constitution departure in `DEV-353`.


---

### Patron Q9

[from Patron] RULING (DEV-353 Q9/12): ACCEPT — Place all unit and property tests in existing `tests/LamuFlix.Test` (`Domain/TasteProfileTests.cs`, `Features/Recommendations/TasteProfileCalculatorTests.cs`, `Options/RecommendationOptionsRecencyTests.cs`, and `Features/Recommendations/TasteProfileProperties.cs`) with no new NuGet packages, test projects, or DB/I/O fixtures; use a test-local BCL `TimeProvider` double (counting `GetUtcNow()` calls), Shouldly assertions over scalar coordinates and dictionary entries, `[Theory]`/`[MemberData]` hand-computed oracles covering all Q1–Q8 behaviors and error boundaries, and property tests asserting finite outputs, convex bounds, identical-source idempotence, uniform-replication invariance, and determinism under the repo's existing property-test gate conventions. No ticket or constitution departure (`blocked: structural` does not apply).

- **Existing Test Project & Zero-Dependency Envelope (`specs/PRODUCT.md:43-44`; `recon-DEV-353:174-182`; `constitution.md:306-327`)**: Adding the four test files under existing `tests/LamuFlix.Test` (`recon-DEV-353:174-182`) using xUnit v3, Shouldly, `[Theory]`/`[MemberData]`, and a test-local `TimeProvider` subclass that tracks `GetUtcNow()` call count avoids introducing any new NuGet package (such as `FakeTimeProvider`) or test project (`specs/PRODUCT.md:43-44`, §2.3 care items 1–2). Because `TasteProfileCalculator` is a pure in-memory Core calculation with no database query translation or serialization (`DEV-353` note:38), no Testcontainers or I/O fixtures apply (`AGENTS.md`).
- **Independent Oracles & Mutation-Gate Coverage (`DEV-353` note:35-37; `specs/PRODUCT.md:12`; `constitution.md:311`)**: Asserting exact hand-computed oracles (e.g., 0-day vs 30-day watches at default `Math.Log(2.0) / 30.0` giving `1.0` vs `0.5` weights and `2/3` vs `1/3` mixture ratios, fractional days, UTC offset equivalence, `GenreWeights` rarity values, unnormalized 384-float basis vectors, duplicate `PlayedAt` retention vs undated `IsWatched` fallback, channel-independent denominators, zero/cancelling vectors, `age - a_min` numerical stability on old history and large finite `RecencyDecayLambda`, all Q1/Q7 exceptions, single `GetUtcNow()` read, and element-wise `ImmutableArray`/`ImmutableDictionary` comparisons) directly verifies all acceptance criteria (`DEV-353` note:35-37) and guards the `>= 80%` Stryker mutation threshold (`specs/PRODUCT.md:12`; `constitution.md:311`).
- **Property-Test Invariants (`AGENTS.md`; `scripts/run-property-tests.ps1`)**: Because `TasteProfileCalculator` has clear mathematical invariants—finite outputs, per-channel convex bounds, identical-source idempotence, uniform-replication invariance within floating tolerance, and fixed-clock determinism—`TasteProfileProperties.cs` satisfies the `run-property-tests.ps1` gate using the repo's existing property-test conventions verified at Phase B without adding dependencies or departing from the ticket/constitution (`specs/PRODUCT.md:36-39`).


---

### Patron Q10

[from Patron] RULING (DEV-353 Q10/12 — Grill Close): ACCEPT — Ratify Q1–Q9 unchanged, confirm shared understanding across all six loop/scope/envelope/gate terms, and authorize Keel to record Q10 in `specs/DEV-353/CONCLUSIONS.md`, write `specs/DEV-353/brief.md` and feature-local `specs/DEV-353/CONTEXT.md` (`TasteProfile`, `watch signal`, `genre preference`, `plot centroid`), and close the Phase A grill for Quill drafting. No ticket or constitution departure (`blocked: structural` does not apply).

- **Closing Bar, Round Cap & Frozen Scope (`specs/PRODUCT.md:12, 18, 23-24, 36-39`; `AGENTS.md`)**: `Critical`/`High`/`Medium` findings block while `Low` findings are recorded non-blocking, and any missing required axis, failed or `Could not run` gate, or actual ticket/constitution departure blocks regardless of severity; unnecessary abstractions are deferred to follow-up (`specs/PRODUCT.md:18`). Plan challenge/correction and formal review/remediation are each capped at 2 rounds (stopping to report to Bernstein if above-bar items remain), and scope is frozen to Q1–Q9 contracts, `RecommendationOptions.RecencyDecayLambda` validation, and the Q9 test suite.
- **Strict File Envelope & Phase A Artifact Handoff (`specs/PRODUCT.md:43-48`; `constitution.md:110-141, 428`)**: Phase B production and test edits are restricted to `src/LamuFlix.Core/Domain/TasteProfileMovie.cs`, `src/LamuFlix.Core/Domain/TasteProfile.cs`, `src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs`, existing `src/LamuFlix.Core/Options/RecommendationOptions.cs` (`DEV-353` note:30; §2.3 item 6 forced edit), and the four `tests/LamuFlix.Test/` files (`Domain/TasteProfileTests.cs`, `Features/Recommendations/TasteProfileCalculatorTests.cs`, `Options/RecommendationOptionsRecencyTests.cs`, `Features/Recommendations/TasteProfileProperties.cs`). Keel is authorized to close the grill by writing `specs/DEV-353/CONCLUSIONS.md`, `specs/DEV-353/ASSUMPTIONS.md`, `specs/DEV-353/brief.md`, and `specs/DEV-353/CONTEXT.md` for Quill to draft `spec.md`, `plan.md`, and `tasks.md`.
- **Phase B Task Order & Live Gate Classification (`specs/PRODUCT.md:12, 17`; `AGENTS.md`; `constitution.md:306-327`)**: Phase B begins with read-only drift/scheduling/property-discovery verification (`specs/PRODUCT.md:17`), implements contracts, options, calculator, and unit/property tests in order, and enforces live `harness.yml` gates (Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable packages, format, `dotnet test`, and Core Stryker mutation) while strictly preserving exit-0 `PASSED`, exit-1 `FAILED`/`Could not run`, and exit-2 `SKIPPED (scope-empty)`/`SKIP`/`N/A` semantics (`AGENTS.md`).

