# Feature Specification: DEV-353 Recency-weighted TasteProfile

**Feature Branch**: `feature/353-spec`

**Created**: 2026-10-22

**Status**: Draft

**Input**: Ticket DEV-353 (size M, parent DEV-288, no ui); settled grill brief `specs/DEV-353/brief.md` (grill closed Q10/12); facts from notes `recon-DEV-353` and `recon-DEV-353-2`; Patron rulings in `specs/DEV-353/CONCLUSIONS.md`; taste log `specs/DEV-353/ASSUMPTIONS.md`; feature glossary `specs/DEV-353/CONTEXT.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## Domain vocabulary *(mandatory — from feature-local CONTEXT.md)*

These four terms are approved in brief Q10 and recorded in `specs/DEV-353/CONTEXT.md`. Published Spec Kit artifacts must use them as written. No root `CONTEXT.md` edit is authorized for this M ticket.

- **TasteProfile**: A personal preference signal inferred from watched movies, with independent genre and plot components. An empty profile carries no personal signal. _Avoid_: GenreWeights, recommendation score.
- **Watch signal**: A dated recorded playback launch, or an undated manual watched mark when that movie has no recorded launch. Repeated launches are separate signals. _Avoid_: completed viewing, playback progress.
- **Genre preference**: The personal preference for a genre inferred from watched movies, combining their recency with the genre's library rarity. It differs from the library-wide rarity value itself. _Avoid_: GenreWeights, genre scoring coefficient.
- **Plot centroid**: The recency-weighted average of available plot embeddings for watched movies. An empty plot component carries no directional plot preference. _Avoid_: plot similarity score, generated embedding.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Compute a recency-weighted TasteProfile (Priority: P1)

A recommendations caller supplies materialized watched-movie rows (genres, plot embeddings, launch times, watched marks) and a library **GenreWeights** table. The calculator returns a **TasteProfile** whose **genre preferences** and **plot centroid** are independent recency-weighted averages of those **watch signals**. Each dated launch decays by `exp(-RecencyDecayLambda * days)`. Recent dated watches weigh more than older ones. An undated manual watched mark contributes only when that movie has no launches. The calculator does no I/O, caching, DI, or route work.

**Why this priority**: This is the ticket's pure Core delivery. Caching and orchestration belong to DEV-415.

**Independent Test**: Unit tests with a fixed injected clock and hand-computed oracles for 0-day vs 30-day watches at the default lambda (`1.0` vs `0.5` weights; `2/3` vs `1/3` mixture), fractional days, offset-equivalent timestamps, GenreWeights rarity values, unnormalized 384-float basis vectors, duplicate same-instant launches, undated mark fallback without double-counting, and independent channel denominators. No database or HTTP.

**Acceptance Scenarios**:

1. **Given** two distinct MovieIds, each with one dated launch at ages 0 and 30 days respectively, each movie carrying a different single genre `gA` and `gB`, with default `RecencyDecayLambda = Math.Log(2.0) / 30.0`, **When** TasteProfile is calculated, **Then** relative scaled weights are `1.0` and `0.5`, and observable genre outputs are `GenrePreferences(gA) = (2.0/3.0)*rA` and `GenrePreferences(gB) = (1.0/3.0)*rB` for the independently established supplied GenreWeights rarity values `rA`/`rB` (different rarity values are not required).
2. **Given** dated launches and an `IsWatched = true` mark on a movie that already has launches, **When** TasteProfile is calculated, **Then** each launch contributes separately and the mark adds no extra undated signal.
3. **Given** a movie with no launches and `IsWatched = true`, **When** TasteProfile is calculated, **Then** exactly one undated watch signal of age zero (weight `1.0`) contributes.
4. **Given** launches on a movie with `IsWatched = false`, **When** TasteProfile is calculated, **Then** the launches still contribute; the false mark does not erase them.
5. **Given** genre-bearing signals and plot-bearing signals that are not identical sets, **When** TasteProfile is calculated, **Then** each channel's denominator uses only its own contributors (missing genres or missing embeddings do not dilute the other channel).
6. **Given** canonical genre names that differ only by case or surrounding whitespace, **When** TasteProfile is calculated, **Then** blanks are discarded, case-insensitive duplicates collapse, and the canonical key is the Ordinal-minimum trimmed name in the group.
7. **Given** a fixed clock and a fixed ordered input, **When** Calculate runs twice, **Then** genre preference entries and plot coordinates are bitwise identical, and `GetUtcNow` was sampled once per call.

---

### User Story 2 - Cold start and empty channels (Priority: P2)

When there is no watch history, or when a channel has no eligible contributors, the calculator returns empty channel values without throwing for valid input. Callers treat an empty **TasteProfile** as no personal signal. A populated genre channel survives missing or cancelling plot data and vice versa.

**Why this priority**: Ticket acceptance requires empty history to return an empty profile without throwing.

**Independent Test**: Unit tests for empty movies, movies with no launches and `IsWatched = false`, genre-only history, plot-only history, all-zero plot centroid, and missing embeddings or genres.

**Acceptance Scenarios**:

1. **Given** an empty or default movies array, **When** TasteProfile is calculated with valid options and GenreWeights, **Then** both channels are empty, `IsEmpty` is true, and no exception is thrown.
2. **Given** movies that have no dated launches and `IsWatched = false` (even with genres or embeddings present), **When** TasteProfile is calculated, **Then** both channels are empty and `IsEmpty` is true.
3. **Given** genre-bearing watch signals but no valid plot embeddings, **When** TasteProfile is calculated, **Then** genre preferences are populated and plot embedding is empty; `IsEmpty` is false.
4. **Given** plot-bearing watch signals but no genre-bearing signals, **When** TasteProfile is calculated, **Then** the plot centroid is populated (unless all-zero) and genre preferences are empty; `IsEmpty` is false.
5. **Given** plot contributors whose recency-weighted mean is exactly `0.0f` in all 384 coordinates, **When** TasteProfile is calculated, **Then** plot embedding is exposed as empty while any genre preferences remain.

---

### User Story 3 - Options and input validation (Priority: P3)

`RecommendationOptions` gains `RecencyDecayLambda` (inverse days, default 30-day half-life). Invalid lambda, null collaborators, corrupted embeddings, null movie rows, and duplicate MovieId rows fail fast with the ruled exception types. Default/empty immutable arrays are valid absences. Caller inputs are never mutated. Unrelated Take/MMR fields are not calculator preconditions.

**Why this priority**: Ticket text places lambda in RecommendationOptions; constitution requires validated options and injected time.

**Independent Test**: Options validation tests plus calculator constructor/Calculate rejection tests for every Q7 boundary. Assert `MmrLambda` remains unchanged by this feature's default.

**Acceptance Scenarios**:

1. **Given** default `RecommendationOptions`, **When** the options object is created, **Then** `RecencyDecayLambda` equals `Math.Log(2.0) / 30.0`, is finite and strictly positive, and `MmrLambda` remains `0.7`.
2. **Given** non-finite or non-positive `RecencyDecayLambda`, **When** options validation or the calculator constructor runs, **Then** the invalid lambda is rejected (`ArgumentOutOfRangeException` from the calculator constructor; options validation rejects via the existing validation path).
3. **Given** null `TimeProvider` or null `RecommendationOptions`, **When** the calculator is constructed, **Then** `ArgumentNullException` is thrown.
4. **Given** null `GenreWeights`, **When** Calculate runs, **Then** `ArgumentNullException` is thrown.
5. **Given** null movie rows, duplicate `MovieId` rows, or a nonempty plot embedding whose length is not 384 or that contains non-finite floats, **When** Calculate runs, **Then** `ArgumentException` is thrown before accumulation, including for rows with no watch signals.
6. **Given** default/empty `Genres`, `PlayedAt`, or `PlotEmbedding` arrays on otherwise valid rows, **When** Calculate runs, **Then** those absences succeed without throwing.

---

### User Story 4 - Numerical stability and property invariants (Priority: P4)

Old-only histories and large finite lambda remain well-defined through per-channel `age - a_min` rescaling. Property tests lock finite outputs, per-channel convex bounds, identical-source idempotence, uniform-replication invariance within floating tolerance, and fixed-clock determinism over valid bounded histories.

**Why this priority**: Acceptance requires determinism; domain invariants require the property-test gate with no opt-out.

**Independent Test**: Unit oracles for future-timestamp clamp, old-only history, huge finite lambda, relative underflow omitting exact-zero genre entries; property suite in `TasteProfileProperties.cs` under existing `tests/LamuFlix.UnitTests` discovery conventions.

**Acceptance Scenarios**:

1. **Given** a future `PlayedAt`, **When** age is computed, **Then** age clamps to `0.0` (no weight above the newest contributor after rescaling).
2. **Given** an all-old history or a huge finite `RecencyDecayLambda`, **When** TasteProfile is calculated, **Then** the channel denominators remain finite and nonzero (newest contributor scaled weight `1.0`); the result is not a false cold start.
3. **Given** valid generated histories that respect unique movie rows and watch-signal rules, **When** property tests run, **Then** outputs are finite, channel values stay within convex bounds of contributors, identical sources are idempotent, uniform replication is invariant within floating tolerance, and fixed-clock repeats match.

### Edge Cases

- Undated `IsWatched = true` with no launches: one age-zero signal; no fabricated timestamp.
- `IsWatched = true` with launches: no extra undated signal.
- `IsWatched = false`: does not erase launches.
- Duplicate same-instant `PlayedAt` values are preserved as separate signals.
- Missing/null movie metadata genres and default/empty embeddings do not dilute the other channel.
- Valid all-zero 384-float embeddings participate in the plot denominator; an all-zero mean becomes empty plot output.
- Exact-zero genre preference entries are omitted from the result dictionary.
- No `IsNotInterested`, negative weights, or candidate filtering in this calculator.
- No DEV-351/352 schema, DEV-413 repository/ADR publication, DEV-415 cache, DI/handler/API/UI/config edits, LocalPlay, secrets, or `Process.Start`.
- DEV-413 repository/snapshot remain unlanded draft contracts; this ticket consumes materialized floats only.
- Phase B begins with read-only drift/scheduling/property-discovery verification; this Phase A specification does not clear inherited owner checkboxes or claim Epic 7 complete.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Add `double RecencyDecayLambda` to existing `RecommendationOptions` (unit: inverse days; default `Math.Log(2.0) / 30.0`; finite and strictly `> 0.0`) validated through the options validation path. Keep `MmrLambda` separate and unchanged. Ticket authority: DEV-353 note:28-30; brief Q1.
- **FR-002**: Define `sealed record TasteProfileMovie(MovieId MovieId, ImmutableArray<string> Genres, ImmutableArray<float> PlotEmbedding, ImmutableArray<DateTimeOffset> PlayedAt, bool IsWatched)` and `sealed record TasteProfile(ImmutableDictionary<string, double> GenrePreferences, ImmutableArray<float> PlotEmbedding)` with computed `bool IsEmpty` when both channels are empty (`GenrePreferences.IsEmpty && PlotEmbedding.IsDefaultOrEmpty`) in `src/LamuFlix.Core/Domain/`. Brief Q3.
- **FR-003**: Define `sealed class TasteProfileCalculator(TimeProvider, RecommendationOptions)` in `src/LamuFlix.Core/Features/Recommendations/` with synchronous `TasteProfile Calculate(ImmutableArray<TasteProfileMovie> movies, GenreWeights genreWeights)`. Sample `TimeProvider.GetUtcNow()` exactly once per call. Perform no I/O, repository reads, caching, DI registration, route or handler integration. Brief Q3, Q6, Q8.
- **FR-004**: Watch-signal inclusion MUST follow brief Q2: each `PlayedAt` entry is a distinct dated signal regardless of `IsWatched`; `IsWatched = true` adds one undated age-zero signal if and only if that movie has no launches; `IsWatched = false` does not erase launches.
- **FR-005**: Age MUST be `Math.Max(0.0, (now - playedAt).TotalDays)` (future timestamps clamp to zero; undated signals age zero). Per channel, compute `a_min = min(age)` over that channel's contributors and `scaledWeight = Math.Exp(-RecencyDecayLambda * (age - a_min))` with the subtraction before the multiply. Brief Q6.
- **FR-006**: Genre preferences MUST follow brief Q4: trim/discard blanks; deduplicate `OrdinalIgnoreCase`; canonical key is Ordinal-minimum trimmed name; `x_m(g) = genreWeights.GetWeight(g)` for genres in the movie set else zero; `GenrePreferences(g) = sum(w_s * x_m(g)) / sum(w_s)` over genre-bearing signals only; result is `ImmutableDictionary` with `OrdinalIgnoreCase` comparer; omit exact-zero entries; no per-movie genre-count division, L1 normalization, or `SimilarityWeights.Genres` multiplier.
- **FR-007**: Plot centroid MUST follow brief Q5: coordinate-wise weighted mean over signals with provided valid 384-float embeddings only; double accumulation then float output; no input/output L2 normalization or similarity scalar; include valid zero vectors; return `ImmutableArray<float>.Empty` when there are no plot contributors or the mean is all `0.0f`.
- **FR-008**: Validation MUST follow brief Q7: default/empty immutable arrays are valid; null `TimeProvider`/`RecommendationOptions`/`GenreWeights` throw `ArgumentNullException`; non-finite or non-positive lambda throws `ArgumentOutOfRangeException` in the calculator constructor; null rows, duplicate `MovieId`, nonempty embedding length other than `EmbeddingDimensions.Value` (384), or non-finite embedding floats throw `ArgumentException` after validating the full collection before accumulation; preserve duplicate `PlayedAt`; never mutate caller inputs; do not couple the calculator to unrelated Take/MMR validity.
- **FR-009**: Scope exclusions MUST follow brief Q8: no `IsNotInterested`, negative-weight formula, candidate filtering, schema/persistence, DEV-413 port/ADR, DEV-415 cache, DI/handler/API/UI/config-file wiring, LocalPlay, secrets, or `Process.Start`. No new NuGet/npm package, project, top-level folder, or architectural layer. No root glossary edit and no ADR.
- **FR-010**: Tests MUST live in existing `tests/LamuFlix.UnitTests` as `Domain/TasteProfileTests.cs`, `Features/Recommendations/TasteProfileCalculatorTests.cs`, `Options/RecommendationOptionsRecencyTests.cs`, and `Features/Recommendations/TasteProfileProperties.cs`. Use xUnit v3, Shouldly, Theory/MemberData, a test-local BCL `TimeProvider` subclass that counts `GetUtcNow` calls, and the repo's existing property-test framework, category trait, and seed conventions — verified at T001 against the live test project and property-test gate; stop for recon or a cited ruling if any convention is missing. Do not assert a specific property package or discovery attribute as already installed. No new package, test project, or DB/I/O fixture. No property-test opt-out. Compare scalar coordinates and dictionary entries, not immutable-array record reference equality. Brief Q9.

### Key Entities

- **TasteProfileMovie**: Materialized per-movie input carrying genres, plot embedding, launch times, and watched mark (see Domain vocabulary / brief Q3).
- **Watch signal**: Dated launch or undated manual mark contribution (see Domain vocabulary / brief Q2).
- **TasteProfile**: Personal preference with independent genre preferences and plot centroid (see Domain vocabulary / brief Q3).
- **Genre preference**: Recency-weighted rarity-aware per-genre preference entry (see Domain vocabulary / brief Q4).
- **Plot centroid**: Recency-weighted average embedding (see Domain vocabulary / brief Q5).
- **TasteProfileCalculator**: Pure synchronous Core calculator over materialized inputs (brief Q3).
- **RecencyDecayLambda**: Inverse-days decay rate on `RecommendationOptions` (brief Q1).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At default lambda, a 0-day watch weighs twice a 30-day watch (`1.0` vs `0.5`); with two distinct MovieIds each carrying a different single genre `gA`/`gB` and one launch at ages 0 and 30 days, observable genre outputs are `GenrePreferences(gA) = (2.0/3.0)*rA` and `GenrePreferences(gB) = (1.0/3.0)*rB` for independently established supplied rarity values `rA`/`rB` (different rarity values are not required).
- **SC-002**: Valid empty watch history returns an empty TasteProfile (`IsEmpty == true`) without throwing.
- **SC-003**: Repeated Calculate with the same fixed clock and ordered input yields bitwise-identical genre entries and plot coordinates, with exactly one `GetUtcNow` read per call.
- **SC-004**: Genre and plot channels remain independent under partial metadata; all-zero plot means become empty plot output without clearing genre preferences.
- **SC-005**: Invalid lambda, null collaborators, duplicate MovieIds, and corrupted embeddings fail with the ruled exception types; caller inputs are unchanged afterward.
- **SC-006**: Property tests for the named invariants pass under the existing property-test gate; no property-test opt-out. Backend gates use thresholds from live `harness.yml` only; scope-empty skips are not recorded as passes. Unchanged `/web` is scope-empty skip.

## Assumptions

- [assumed] Q1 Patron-selected tuning baseline: `RecencyDecayLambda` defaults to `Math.Log(2.0) / 30.0` (30-day half-life). Logged in `specs/DEV-353/ASSUMPTIONS.md`; not a user-facing taste rule.
- Phase B build intake remains subject to Epic 7 sequencing under `specs/PRODUCT.md:17` and a mandatory pre-Phase-B drift check against landed predecessor contracts. Specifying in Phase A does not authorize Phase B or clear inherited owner checkboxes (brief Q8, Q10).
- No owner structural checkbox was identified for DEV-353 (brief Q1–Q10; no `blocked: structural`).
- Exclusions in brief Q8 remain out of scope for this feature.
