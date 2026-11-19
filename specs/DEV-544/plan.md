# Implementation Plan: Whole-library Related strong matches

**Branch** — `feature/544-spec` | **Date** — 2026-11-19 | **Worktree** — `F:/Dev/LamuFlix.worktrees/feature-544-spec`

**Status** — **PLAN FROZEN** after the three-axis challenge. The owner Gate 1 merge, implementation and runtime acceptance remain pending.

**Authority** — DEV-544 ticket (Wanted, Done when, Goal and Where it lands), `DEV-544` and `recon-DEV-544` notes, `brief.md`, `CONCLUSIONS.md` (Q1–Q6 and adjudication ruling), `spec.md`, `specs/PRODUCT.md` and `.specify/memory/constitution.md`. See [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md) for the 30 dispositions.

## Summary

Related loads features for the entire enriched library through the existing candidate port and uses the DEV-518 scorer. Raw pair similarity and enabled reasons decide eligibility. The existing taste blend, watched penalty, MMR and seed collection grouping rank eligible movies within each tier. The caller's `take` is a maximum. `CandidatePoolSize` remains the cap for `take` on both recommendation endpoints.

## Technical context

- **Stack** — .NET 10, the existing Core ports/handlers and EF/PostgreSQL adapter, and Vite/React/TypeScript for Related copy and generated contract outputs.
- **Scale** — Fixed database round trips verified on the live Related request at 20 and 200 movies. Load vectors per request. At 450 movies and 384 four-byte floats per populated vector, the raw vector payload is at most 691,200 bytes before allocations for other data and objects. T021 owns measured runtime evidence. Beyond 450 enriched movies, route a measurement review through Bernstein/Rigger.
- **Boundaries** — DEV-518 owns scoring, normalization, ceiling and reason presentation. DEV-543 owns For you migration. DEV-539 consumes the shared load downstream. DEV-545 owns the snapshot and timing workflow. The current taste-profile cache stays profile-only. The ticket excludes a library-size cache, paging, schema changes and new routes, DTO fields or HTTP statuses.

## Constitution check

| Principle | Plan response |
|---|---|
| I — Ports and adapters | Add a method on the existing port. The EF adapter implements it. The new-port ADR rule does not apply. |
| II — Explicit handlers | Compose Related in its existing handler and recommendation slice. |
| III — Typed query model | N/A. Browse filtering is outside scope. |
| IV — Domain transitions | This is a read and score path. |
| V — Validated inputs and errors | Validate options through the real options pipeline. Preserve empty success and take-cap HTTP behavior. |
| VI — Observability | Record sampled tier counts, effective options and timing in sanitized DEV-545 evidence. |
| VII — Configuration | Use `RecommendationOptions` and existing configuration infrastructure. LocalPlay, secrets and process execution are outside scope. |
| VIII — Ubiquitous language | Related and strong match come from the ticket. Tier A/B are spec-local partitions, per Patron's ruling in `CONCLUSIONS.md`. |
| IX — Test pyramid | Unit and property tests, Testcontainers EF/HTTP regressions, ArchitectureTests, scoped Vitest/RTL and DEV-545 runtime evidence. |
| API contracts and static-analysis gates | Regenerate via the API contract chain. T019 lists the gates and T020 applies brief Q6. |

## Approach

1. **Gate 1 and pickup.** Bernstein coordinates the spec PR, Rigger prepares and opens it, the owner merges, and Keel records the receipt. Wisp verifies merged DEV-518 and dependent seams on the picked base in T001. A missing scorer contract is a dependency blocker to Bernstein. Record other missing required facts as `needs recon`. Keep the pickup read-only and handle any authorized amendments separately. Capture and verify the T002 before-snapshot artifact before production edits.
2. **Options.** Add `MinimumSimilarity` (0.225, finite in [0, 1]) and a string-valued `StrongReasonCriteria` defaulting to Collection, Director and Themes. An empty list is valid. Match canonical tokens using `StringComparer.OrdinalIgnoreCase` with no whitespace trimming. Reject null and unsupported values via options validation. Map canonical tokens to the exact merged DEV-518 breakdown names identified by pickup. No DEV-544 ceiling or Keyword alias.
3. **Shared load.** Add `GetLibraryFeaturesAsync(CancellationToken)` to `IRecommendationCandidateSource`. Its entries represent every enriched movie and carry DEV-518 `MovieFeatures`, summary, watched/review/watchlist state and optional plot vector. Movie metadata and signals feed the mapper. Optional persistence feature rows supply embeddings and may be missing without discarding the movie's other inputs. Keep the existing split-query traversal and enriched filter. `GetCandidatesAsync` is the selected-relative wrapper with the existing selected-vector early return. It delegates to the shared load, excludes the selected movie and pairs each returned entry with the derived cosine. Remove the plot cutoff and same-collection union. FR-001's cosine qualifier applies to the selected-relative paths.
4. **Related scoring.** Related uses the library entries for candidate features and vectors. Resolve the selected vector from the loaded entry when present and fall back to the existing selected-only seam when the selected movie is absent from the enriched load. A missing or unusable selected vector yields an empty result. Exclude the selected movie and disliked candidates. Keep the liked-source fetch for taste scoring and pass no candidate IDs to the profile-input fetch. Use loaded library vectors for candidate scoring and for overlaps in the assembled liked-source inputs. DEV-518 scores missing or unusable candidate vectors as Plot = 0 and evaluates the other criteria. Check request cancellation during scoring.
5. **Eligibility and order.** Classify raw DEV-518 results with a pure method within the existing slice. Tier A clears the inclusive similarity bar. Tier B is below the bar with an enabled, present term at exact normalized value 1.0. A missing term cannot qualify. Retain taste blend and watched penalty for ranking. Run existing MMR and seed grouping within A, which gets up to `take` slots. Run the same stages within B only for remaining slots. Concatenate A then B. Preserve the existing standalone MMR tie and prefix behavior. The grouped rail has no added prefix contract. Preserve DEV-518 reasons and Plot evidence where supplied. If legacy Similar story survives, use actual selected-relative cosine.
6. **Compile and verify.** Add validated options first. Land the record, port, EF and Related switch as one compile boundary with all known substitute changes. Start EF and handler green checkpoints on the compiling slice, keeping the EF load proof separate from live scoring and command-count proof. T007/T010 exercise classification, selected fallback, input reuse and tier ordering. T011 keeps the standalone MMR tests. T019 is the gate checklist.
7. **Descriptions and copy.** Document `CandidatePoolSize` as the `take` cap through the option's validation-message text and both OpenAPI operation descriptions. Keep the name, default 50 and endpoint cap behavior. Compare snapshot descriptions by operation and parameter and regenerate the web contract outputs. Change the Related empty-success copy to “No close match in your library.” Verify empty and non-empty cases with scoped Vitest/RTL.
8. **Runtime evidence.** After automated verification and the three-axis review, run T021 for the DEV-545 after snapshot, quality evidence and warm 450-movie timing. T021 also records diagnostics and other measurement observations available from the existing workflow.

## Authorized project files

| Area | Files and scope |
|---|---|
| Core port | `src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs` |
| Core recommendations | `src/LamuFlix.Core/Features/Recommendations/{RecommendationCandidate,MovieFeaturesMapper,GetRecommendationsQueryHandler}.cs` |
| Core options | `src/LamuFlix.Core/Options/RecommendationOptions.cs` |
| EF | `src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs` |
| API contract | `src/LamuFlix.Api/RecommendationsOperationTransformer.cs`, OpenAPI snapshot in T013, and generated `web/src/api/openapi.json`, `web/src/api/types.ts` and applicable downstream outputs via the generator |
| Related UI | `web/src/features/movie-details/RelatedMovies.tsx` and existing `RelatedMovies.test.tsx` rail smoke as authorized by Patron, ticket Goal and FR-012 |
| Unit and property | `tests/LamuFlix.UnitTests/Features/Recommendations/{GetRecommendationsQueryHandlerTests,GetRecommendationsQueryHandlerPropertyTests,ReviewRankingHandlerTests}.cs`. Options coverage goes in the first file. Review ranking changes are limited to the cited RelatedHarness substitute/constructor. Use existing `MmrSelectorTests.cs` for regression. |
| Integration | `tests/LamuFlix.IntegrationTests/{EfRecommendationCandidateSourceTests,RecommendationEndpointTests}.cs` and the OpenAPI snapshot in T013 |
| Gate evidence | `tests/LamuFlix.ArchitectureTests/` is a verification target. T019 requires a passing receipt. |
