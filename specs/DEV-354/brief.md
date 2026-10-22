# DEV-354 provisional decision brief

## Status and authority

Phase A, Phase 2 step 1. Grill OPEN; scope NOT frozen; no Gate 1 clearance or gate1: provisional authorization. This is a partial decision record, not a completed handoff to Quill.

Worktree: F:/Dev/LamuFlix.worktrees/feature-354-spec
Branch: feature/354-spec
Phase A base: 53f4205d901a1798f41077bbd67127305dff301c
Evidence: task note DEV-354 and recon-DEV-354 (124-line version read 2026-10-22).

## Every grill answer

### Q1 - Evidence boundary

Patron accepts supported provisional design using the DEV-354:14 intake summary, verified current-code facts (recon-DEV-354:5-19), and predecessor spec contracts (recon-DEV-354:80-104). The summary is not the full authoritative ticket. Scope freeze, grill completion, and gate1: provisional remain blocked until full DEV-354 Scope, Technical Design, and per-criterion acceptance text are reconciled. Do not invent wording or certify conformance. Full exchange: CONCLUSIONS.md Q1.

DEV-353 TasteProfile and DEV-348 RecommendationCandidate / SimilarStoryThreshold are spec-stage contracts, not implemented runtime dependencies at this base (recon-DEV-354:108-112).

## Blocking evidence

Full authoritative DEV-354 Scope, Technical Design, and per-criterion acceptance text are unavailable in the bounded roots (recon-DEV-354:119-122). This is an evidence blocker, not a discovered ticket contradiction or a constitution departure. No owner structural checkbox is inferred.

## Loop discipline and plan decisions

Closing bar, frozen scope, round cap, implementation approach, exact authorized file set, tests, gate expectations, and task ordering remain unsettled unless recorded below following a cited Patron ruling. Quill must not infer missing decisions.

### Q2 - Provisional review loop and scope

Patron accepts Critical/High/Medium as blocking and Low as non-blocking, maximum two review rounds and two fix commits per round (task-pipeline:25-29). A third round is not authorized by this brief.

Provisional scope candidate: blend formula, configurable blend weights, Structural or Personal reason contract, and pre-scoring Not Interested exclusion from DEV-354:14; MMR remains DEV-416; anything else is a follow-up issue, not a finding in this round.

This is not a current scope freeze. Reconcile it against authoritative ticket Scope/TD/AC before closing the grill. No gate waiver. Full exchange: CONCLUSIONS.md Q2; basis recon-DEV-354:62,119-122.

### Q3 - Provisional Not Interested invariant and test strategy

Exclude Movie.IsNotInterested candidates in GetRecommendationsQueryHandler before scoring or computing reasons, independent of adapter behavior. A positive d coefficient is not an alternative to exclusion. Score/order only eligible candidates, preserving score descending, Movie.Id ascending, then Take unless full authoritative scope requires a different ruling (recon-DEV-354:11,57).

Proof obligations: a mixed-candidate fixture and FsCheck over candidate flags; flagged candidates never appear and never reach scoring, including high-similarity and all-excluded cases. Existing test seams are GetRecommendationsQueryHandlerTests.cs and GetRecommendationsQueryHandlerPropertyTests.cs (recon-DEV-354:21-22). Exact instrumentation remains a plan decision; do not introduce an abstraction merely for observation.

No pool refill or query widening is inferred when exclusion leaves fewer results. This ruling approves the invariant and proof obligations, not an exact source edit list, new dependency, schema, or public API shape. Subject to Q1. Full exchange: CONCLUSIONS.md Q3; basis DEV-354:14 and recon-DEV-354:11-17,21-22,57,120.

## Handoff limits and next step

Three questions answered; nine remain within the twelve-question grill budget. Grill paused on evidence, not complete. No drafting, plan challenge, freeze, Gate 1, implementation, or gate execution is cleared by this record.

needs recon: Provide full authoritative DEV-354 Scope, Technical Design, and each acceptance criterion, with source citations, so Patron can reconcile Q1-Q3 and rule on the remaining design without inferring missing delivery requirements.

After reconciliation, settle: blend coefficient names/defaults/validation, raw versus bounded final score, selected/profile similarity definitions and missing-component handling, profile input acquisition and dependency readiness, Structural/Personal reason shape and precedence (including empty/cold-start cases and every-result coverage), public contract compatibility and DTO/OpenAPI/generated TypeScript consequences, exact authorized file set, remaining fixture/property/integration tests, gate expectations, and task ordering. These are open questions, not decisions. DEV-348 and DEV-353 spec presence does not prove runtime availability. No ADR or glossary change has been decided.


## Q4 - Authoritative reconciliation (supersedes the missing-ticket blocker above)

Current verified HEAD: d794a0e0643a012d877e736349e02a2af59d9691; branch feature/354-spec. Intake base remains 53f4205d901a1798f41077bbd67127305dff301c. Earlier source recon applies to that intake base; no source equivalence to current HEAD is claimed.

Authoritative ticket retrieval succeeded (exit 0), recorded verbatim in recon-DEV-354-2:88-117, the Part 2 explicitly linked by recon-DEV-354:1. Earlier unavailable-ticket statements above are historical and superseded by Q4. Patron lifts Q1's missing-ticket evidence blocker and ratifies Q2 and Q3 against the full ticket. The grill and plan freeze remain open.

Reconciled scope: literal unnormalized Score = a*sim(item,selected) + b*sim(item,profile) - c*alreadyWatched - d*notInterested; all terms in [0,1] before weighting. Selected similarity is DEV-338; profile signal uses DEV-353 TasteProfile and DEV-348 plot similarity. MMR is DEV-416. Every result must carry at least one caller-safe Structural or Personal reason; Personal reasons may name only watched movies. Filter Not Interested before scoring; never replace exclusion with a d penalty. Basis: recon-DEV-354-2:100-111. Full exchange: CONCLUSIONS.md Q4.

Review bar and caps remain Q2. No implementation or Gate 1 clearance. Four grill questions used. Remaining technical decisions require further rulings and supported facts; the previous request to retrieve the full ticket is closed.



## Q5 - Coefficient configuration and numeric contract

Patron accepts four proposed scalar RecommendationOptions fields: BlendSelectedWeight (a=1.0), BlendProfileWeight (b=1.0), AlreadyWatchedPenalty (c=0.25), NotInterestedPenalty (d=1.0). Field names and defaults are [assumed] Patron choices, not values specified by the ticket; mirrored append-only in ASSUMPTIONS.md under the standing taste-log requirement.

Validate each coefficient as finite and >=0.0, requiring a>0.0 || b>0.0 (do not add a+b for validation). Keep selected/profile similarities and watched/not-interested indicators in [0,1] before weighting; the latter are 0.0/1.0. NotInterested=0.0 for every handler-eligible candidate. Keep the final blend raw: no post-blend clamp, division, or missing-component renormalization. Reject nonfinite calculated final scores instead of emitting NaN/Infinity; the exact exception and configuration-validation integration must be pinned in the plan after recon. Final score is not guaranteed to be [0,1].

Profile similarity construction, absent-component values, and acquisition remain open. Full exchange: CONCLUSIONS.md Q5. Basis: recon-DEV-354-2:104,106,109; configuration-isolation clause located at recon-DEV-354:73 in the current note layout. Five grill questions used, seven remain before the twelve-question cap.

## Current open evidence and exact next step

The missing-ticket blocker is closed by Q4. A sound profile/reason/API ruling now needs bounded recon at current HEAD d794a0e0643a012d877e736349e02a2af59d9691:

- Explain the change from intake base for DEV-354-relevant source/spec artifacts, with paths and citations; do not carry intake source-absence claims forward as current facts.
- Extract exact DEV-353 genre-preference normalization, missing-component and contributor/provenance contracts; identify available watched-movie title/ID/history and candidate embedding data access paths, or report not found.
- Extract DEV-348 plot-comparison reuse surface and how candidate/profile vectors can reach it, keeping selected-candidate cosine distinct from profile-candidate similarity.
- Extract DEV-338 Structural reason generation, empty/no-signal behavior, exact current Reason and recommendation DTO reason field types/serialization, plus existing OpenAPI generation command evidence. These facts are needed to satisfy every-result reasons, watched-only names, and the C# -> OpenAPI -> generated TypeScript contract without inventing sources or a public shape.

Conductor routes this recon to Wisp, then resumes Keel for Q6 profile similarity and missing-component policy, followed by reason/provenance and API rulings. No new layer, profile repository, runtime source, or fallback reason has been decided. Exact file authorization, tests beyond Q3, gate expectations, task order, and final grill closure remain open. No plan freeze or Gate 1 claim.

Citation correction for Q4: use recon-DEV-354-2:24 or recon-DEV-354:64 for PRODUCT authority; the verbatim ruling uses an obsolete line reference. Ticket citations at Part 2 lines 100-111 remain valid.

## Q6 - Pure profile similarity

Patron accepts weighted Jaccard over the case-insensitive union of trimmed nonempty candidate genres and DEV-353 GenrePreferences: sum(min(candidateWeight, profileWeight))/sum(max(candidateWeight, profileWeight)). Candidate weight is GenreWeights.GetWeight for present genres and zero otherwise. Genre comparability requires a nonempty canonical candidate genre set and positive profile mass. Plot similarity is clamp(max(0, cosine(candidateEmbedding, profile.PlotEmbedding)), 0, 1), using stable scaled arithmetic and two finite nonzero 384-vectors. Reject malformed nonempty vectors explicitly; empty/zero vectors are not comparable. DEV-348 selected-candidate cosine cannot substitute for profile-candidate cosine.

Take the arithmetic mean when both channels are comparable, the single channel when only one is, and zero when neither is. This does not renormalize Q5 a/b coefficients. Formulas and equal channel weights are [assumed] Patron choices, mirrored in ASSUMPTIONS.md. Basis: recon-DEV-354-2:104,109,150-162; DEV-353 spec FR-002/006/007/008. Full exchange: CONCLUSIONS.md Q6.

This settles pure evaluation over materialized inputs only. Runtime acquisition, contributor provenance wiring, reason/API policy, exact file authorization, tests, gates, ordering, and grill closure remain open. Wisp recon at d794a0e confirms predecessor source symbols are absent; spec contracts do not establish runtime readiness. Six questions used; no plan freeze or Gate 1.


## Q7 - Reason and API policy

Patron accepts Core Reason.Kind (Structural or Personal) plus caller-safe Text, and HTTP RecommendedMovieDto.Reasons objects {kind,text} using lowercase string kinds structural/personal. This replaces the existing string-array wire contract under explicit Patron section 2.3 item 4 ruling. Propagate C# DTO -> OpenAPI -> generated TS; DEV-355/357 consume prebuilt strings and kind without constructing explanations in UI/endpoint. Exact consumer/artifact migration and generation commands remain unverified.

Retain existing DEV-338 reasons and DEV-348 Similar story as Structural in deterministic order, then Personal reasons when b>0 for positive comparable profile channels: Matches your <genre> interest for positive overlapping genre, ordered OrdinalIgnoreCase then Ordinal and sanitized for display; Matches your viewing history for positive profile plot match. If no explanation exists, emit one Structural No shared features identified. Plain text, no internal IDs/paths/diagnostics, DEV-338 display sanitation, caller-side escaping. Kind spellings, templates, ordering, and fallback are [assumed] and logged in ASSUMPTIONS.md. Named-movie Personal explanations remain open pending watched-title/provenance evidence; never infer titles from aggregate TasteProfile or candidate cosine. Full exchange: CONCLUSIONS.md Q7; basis recon-DEV-354-2:38,105,110,131,135-138,144-162.

## Current handoff after Q7

Q6-Q7 are persisted product rulings, not runtime/source readiness or file-edit authorization. Seven questions used, five remain. Before Q8 acquisition/file/test/gate/order/closure rulings:

needs recon: Identify exact existing ports/adapters/domain inputs for all watched movies and PlayedAt history, watched title/ID provenance, candidate embeddings and profile construction at the current HEAD (or report not found), plus the complete consumer/test/generated-artifact file set affected by replacing recommendation Reasons strings with {kind,text}, and a supported OpenAPI export and TypeScript generation path (or report not found). Existing Wisp evidence establishes abstract row fields but no acquisition/reuse surface; do not invent a repository, dependency, schema, source, or export command. Conductor assigns any additional recon roots explicitly.

The existing recon reports no explicit OpenAPI generation command in package.json/scripts; not found in those roots. No broader search was performed. No production/generated edits, commits, tests or gates run. Grill remains open; no freeze or Gate 1.

## Q8 - Generic Personal explanations

Patron accepts only Q7 positive-channel templates: Matches your <genre> interest and Matches your viewing history. Do not emit Because you watched <title> or add a separate contributor-provenance type. This is an [assumed] explanation-policy choice, mirrored in ASSUMPTIONS.md. The ticket examples do not require named-movie explanations; AC-2 still forbids naming an unwatched movie. Full exchange: CONCLUSIONS.md Q8; basis recon-DEV-354-2:105,110,155-162; DEV-354:35; DEV-353 spec FR-002/004.

Actual runtime TasteProfile and candidate embedding acquisition remain unresolved delivery prerequisites. Generic text does not solve them. Never use a permanent empty profile to replace unavailable acquisition, or substitute DEV-348 selected-candidate cosine for profile-candidate cosine. Predecessor contracts remain spec-only at verified HEAD d794a0e0643a012d877e736349e02a2af59d9691.

## Bounded consumer and generator reconciliation after Q8

Evidence: DEV-354:35 and recon-DEV-354-2:122-175. This is an affected-surface ledger, not an authorized final edit set. A type may propagate without requiring a textual edit; no edit is authorized merely by its presence below.

Exact source paths identified by recon:
- src/LamuFlix.Core/Options/RecommendationOptions.cs (blend options; recon-DEV-354:8)
- src/LamuFlix.Core/Features/Recommendations/Reason.cs
- src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs
- src/LamuFlix.Core/Features/Recommendations/SimilarityResult.cs
- src/LamuFlix.Core/Features/Recommendations/RecommendedMovie.cs
- src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs
- src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs
- src/LamuFlix.Api/Endpoints/RecommendedMovieDto.cs

Exact test paths from recon-DEV-354:22-30 and DEV-354:35:
- tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerPropertyTests.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerInvariantPropertyTests.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenTests.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenFixture.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs
- tests/LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs
- tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs
- tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs
- tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs
- tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json

Exact web paths from DEV-354:35:
- web/src/api/openapi.json
- web/src/api/types.ts
- web/src/mocks/handlers.ts (generated output; must not be hand-edited)
- web/src/mocks/fixtures.ts
- web/src/features/movie-details/RelatedMovies.tsx
- web/src/features/movie-details/RelatedMovies.test.tsx

Incomplete file identity: DEV-354:35 names ArchitectureTests.cs without its directory and mock recommendation tests without filenames. Any golden data files beyond the two named C# golden files are also not enumerated. These exact paths are not found in the supplied recon; do not invent them or claim a complete file set. Additional blend/profile implementation and acquisition files remain undecided.

OpenAPI runtime registration is evidenced at Program.cs:40-43,66 (recon-DEV-354-2:164-169). An explicit export command writing web/src/api/openapi.json is not found in the bounded recon roots; runtime registration alone is not an export workflow. Do not invent or execute an exporter or hand-edit generated artifacts to bypass this gap.

The downstream generation path is established: from web/, npm run generate:api invokes web/scripts/generate-api.mjs, reads src/api/openapi.json, and generates src/api/types.ts plus src/mocks/handlers.ts. npm run check:api invokes web/scripts/check-api.mjs and compares both generated outputs for freshness. These exact scripts were read in this worktree on resume; no generator or check was executed. This closes the downstream TypeScript-path evidence question only, not upstream DTO-to-OpenAPI export.

## Current handoff after Q8

Eight questions used; four remain. Q8 and its full exchange are persisted. Grill OPEN; no freeze, Gate 1, implementation, source edits, commits, tests, or gates claimed. Only DEV-354 decision documents were appended.

Next: Conductor obtains narrowly scoped recon identifying the exact ArchitectureTests.cs, mock recommendation test, and any external golden data paths, and a supported DTO-to-OpenAPI export procedure or explicit not-found result. Export procedure is currently not found; Patron must then decide a concrete supported workflow if no existing one exists. Runtime watched-history/profile and candidate embedding acquisition are already established absent: Patron must decide a concrete acquisition design and exact file boundary from sufficient recon, rather than requesting another unchanged absence check. No new port, repository, schema, dependency, or delivery deferral is silently approved. After those prerequisites, resume Q9 for acquisition/file authorization, then tests, gate expectations, task ordering and closure within the remaining budget. A ticket-delivery change or constitution departure follows the standing structural escalation rule.

## Q9 - Runtime acquisition, export design, and file authorization

Patron approves a new narrow acquisition port under section 2.3 item 2 and scoped file edits under item 6. Full exchange: CONCLUSIONS.md Q9. Basis: DEV-354:35,40; recon-DEV-354:8-30,82-106; recon-DEV-354-2:104-111,128-170. These are design decisions, not claims that the new types or export operation already exist.

New authorized files (names tagged [assumed]):
- src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs
- src/LamuFlix.Core/Features/Recommendations/RecommendationProfileInputs.cs
- src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs
- src/LamuFlix.Core/Features/Recommendations/BlendScorer.cs
- src/LamuFlix.Core/Features/Recommendations/ProfileSimilarityCalculator.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/BlendScorerTests.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/BlendScorerPropertyTests.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/ProfileSimilarityCalculatorTests.cs
- tests/LamuFlix.UnitTests/Features/Recommendations/ProfileSimilarityCalculatorPropertyTests.cs
- tests/LamuFlix.IntegrationTests/EfRecommendationProfileSourceTests.cs

The cancellation-aware port returns immutable materialized TasteProfileMovie rows plus candidate-ID-to-384-float-vector inputs for the current candidate set. The Infrastructure adapter reads actual persisted watched/launch/genre/vector data; the handler constructs TasteProfile once per request using DEV-353 TasteProfileCalculator and GenreWeights. Preserve DEV-353 PlayedAt/IsWatched semantics. No duplicate calculator, permanent empty-profile substitution, selected-cosine reuse, dependency, schema, project or architectural layer is approved.

The existing exact source/test/web paths in the Q8 ledger above are now authorized only for necessary feature and contract-migration changes. Additional reconciled existing paths authorized by Q9:
- tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs
- web/src/mocks/recommendations.test.ts
- web/src/mocks/handlers.test.ts
- web/src/mocks/generate-api.test.ts

Golden fixtures are the already listed same-folder SimilarityScorerGoldenTests.cs and SimilarityScorerGoldenFixture.cs; no external golden-data directory was found (DEV-354:40). The former Q8 incomplete-path statement is superseded for these identities. Authorization does not require edits to every listed file and permits no unrelated rewrite.

OpenAPI: add an explicit opt-in export operation to tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs, obtaining the registered normalized v1 document from the test-hosted API and writing web/src/api/openapi.json. Ordinary tests remain read-only and keep drift verification against the committed snapshot. Quill must specify and test a discoverable invocation for this NEW operation; no existing DTO-to-OpenAPI command was found and none is invented here. Follow the export from web/ with npm run generate:api, then npm run check:api; never hand-edit types.ts or mocks/handlers.ts. Snapshot changes must represent the authorized DTO contract.

## Current handoff after Q9 (supersedes Q8 next step)

Q9 full exchange, decisions and assumptions are persisted. Nine questions used; three remain. Acquisition/export DESIGN and the listed file boundary are settled subject to narrow persistence/DI recon; actual runtime input readiness, adapter mappings, DI edit path and complete final file authorization remain open. No drafting handoff, freeze, Gate 1, implementation, commits or gate execution is claimed.

needs recon: At HEAD d794a0e0643a012d877e736349e02a2af59d9691, identify exact existing EF entities/tables/mappings and source file paths for watched flags, launch PlayedAt history, genres and stored 384-vector embeddings needed to materialize TasteProfileMovie rows and the current candidate-ID-to-vector snapshot; identify the exact DI composition-root registration file for IRecommendationProfileSource/EfRecommendationProfileSource and calculator/handler wiring. Report unavailable individual fields or mappings explicitly. This requests concrete mapping/registration evidence, not another search for the absent acquisition abstraction. No new schema or dependency may be inferred.

Conductor routes that bounded recon; Keel then resumes Q10 to reconcile mappings/registration and final authorization, followed by remaining test strategy, gate expectations, task ordering and closure decisions within the remaining question budget. If persisted inputs cannot satisfy delivery without a ticket change or constitution departure, Patron must rule under section 2.3 rather than silently defer it.

## Q10 - Concrete persistence reconciliation and wiring boundary

Patron accepts Q10 against DEV-354:44 and Q6-Q9. Full question and ruling: CONCLUSIONS.md Q10. Branch feature/354-spec verified before document edits; HEAD d794a0e0643a012d877e736349e02a2af59d9691 remains recon-reported, not independently measured in this pass. Intake base remains 53f4205d901a1798f41077bbd67127305dff301c.

### Inputs and adapter

Retain Q9 IRecommendationProfileSource, immutable RecommendationProfileInputs and EfRecommendationProfileSource. Read existing movie IDs, IsWatched and genres from MovieRecord.IsWatched -> movies.is_watched (MovieConfiguration.cs:56-58) and MovieRecord.Genres / GenreRecord -> movie_genres (MovieConfiguration.cs:174-179). Materialize movies with IsWatched OR any launch. Add a NEW read query in the approved adapter grouping PlaybackEventRecord.MovieId / PlayedAt from playback_events.played_at timestamptz (PlaybackEventConfiguration.cs:11-31). EfPlaybackHistory.RecordAsync:14-20 establishes only a writer; no existing PlayedAt reader is claimed. Preserve each launch, including launches when IsWatched=false, and add the DEV-353 undated age-zero watched signal only when there are no launches.

The approved new adapter/query does not authorize modifications to MovieRecord, GenreRecord, PlaybackEventRecord, their mappings, EfPlaybackHistory, schema or migrations. These are evidence references, not additional edit-ledger entries.

Both candidate and contributor 384-vector acquisition are UNAVAILABLE in current source per DEV-354:44. MovieRecord.Plot:51 is text; vector extension and EmbeddingDimensions.Value do not establish storage. No vector store, property, column or reader is invented or authorized. DEV-348/353 remain spec-stage dependencies. Keep vector acquisition as a Phase B readiness prerequisite and vector-roundtrip integration verification blocked until a real source lands. Q6 per-movie absent-vector handling applies only after real acquisition exists; it cannot substitute for absent infrastructure. No always-empty runtime profile or selected-candidate cosine substitution. Phase B must stop for dependency recon if inputs remain unlanded; changing delivery requires the standing ticket-change escalation.

### Composition and pure evaluation

Authorize necessary scoped-DbContext-compatible source registration at existing PersistenceServiceCollectionExtensions.cs:32,41-43, handler composition at HandlerRegistration.cs:49, and options wiring at ServiceDefaults/Extensions.cs:90. Exact repo-relative directories of all three files are NOT provided by current recon; these basenames are conditional file authorizations, not a completed exact-path ledger.

Construct/call DEV-353 TasteProfileCalculator, BlendScorer and ProfileSimilarityCalculator directly as pure components. Preserve DEV-353 no-DI calculator contract and keep I/O in the adapter/handler orchestration. Build the profile once per request. No artificial calculator DI wrappers. Quill must pin concrete construction/options-validation details to cited existing conventions.

### Reconciled source/test/generated ledger

All exact Q8 source/test/web paths and Q9 new-file paths remain the authorized ledger, plus Q9's exact ArchitectureTests and mock test identities. No unrelated rewrites and no requirement to edit every listed path. The three composition-root basenames above remain the ONLY unresolved path identities; complete final edit-ledger status is OPEN pending their full paths. No extra entity/configuration/playback-writer/vector-store/migration files are added.

EfRecommendationProfileSourceTests.cs must exercise real PostgreSQL query/translation for watched-only, launch-only, multiple launches, watched-plus-launch without duplicate undated signal, genres and cancellation. Vector roundtrip tests require landed vector acquisition; no mocked driver may stand in for it. Remaining fixture/property/API/web proof strategy, gate expectations and dependency-aware task ordering still need final rulings.

OpenAPI export remains a NEW explicit opt-in normalized-v1 operation in tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs; no existing export command is claimed. Quill must specify a discoverable invocation and its validation. Normal tests remain read-only and verify committed drift baseline. The affected generated artifacts remain the authorized snapshot tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json, exported web/src/api/openapi.json, then generated web/src/api/types.ts and web/src/mocks/handlers.ts. After export, run from web/: npm run generate:api, then npm run check:api. No hand edits to generated artifacts; snapshot changes must reflect the authorized DTO contract.

## Current handoff after Q10 (supersedes Q9 next step)

Ten questions used; two remain. Mapping/query policy, per-input unavailability, pure calculator wiring policy and new opt-in export requirement are settled and persisted. Exact final file ledger remains incomplete only for the three composition-root full paths. No new structural owner question is raised by Q10; no scope reduction, dependency waiver, freeze or Gate 1.

needs recon: Supply the exact repo-relative paths corresponding to Wisp DEV-354:44 PersistenceServiceCollectionExtensions.cs:32,41-43, HandlerRegistration.cs:49 and ServiceDefaults/Extensions.cs:90 at the reported task HEAD. Path identity only; do not repeat searches for absent acquisition/vector infrastructure.

After those identities, mirror them into this ledger under Q10 authorization; resume Patron Q11 for remaining tests/gates/ordering and Q12 for grill closure/readiness decisions within the cap. Quill invocation/construction details must be explicit in its eventual drafting; absent runtime prerequisites remain Phase B blockers. This is not a drafting handoff. Only DEV-354 decision documents changed; no production/generated edits, commits, generators, tests or gates run.




## Q11 - Tests, gate expectations, exact wiring paths and ordering (2026-10-22)

Patron accepts the full Q11 proposal. Full question and answer: CONCLUSIONS.md Q11. Basis: DEV-354:35,40,42,44,46,48; recon-DEV-354:22-30,63,68-78; recon-DEV-354-2:104-111,128-170; Q2-Q10. Branch feature/354-spec and HEAD d794a0e0643a012d877e736349e02a2af59d9691 verified on this resume; intake base remains 53f4205d901a1798f41077bbd67127305dff301c.

### Exact composition roots (supersedes Q10 identity blocker)

Q10 conditional authorizations now resolve to:
- src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
- src/LamuFlix.Api/HandlerRegistration.cs
- src/LamuFlix.ServiceDefaults/Extensions.cs

These complete the Q8-Q10 authorized file ledger for the settled design; only necessary wiring edits are authorized. Scoped-compatible EF source registration; pure calculators use direct construction/calls, without artificial DI wrappers. No entity/configuration/playback-writer/schema/vector-store edits added.

### Behavioral proof matrix

- BlendScorer unit/property fixtures: independent formula a*S+b*P-c*W-d*N with bounded input terms, binary penalties, zero/independent weights, negative and above-one raw scores, invalid coefficients and nonfinite result rejection. Pure formula tests exercise N=1; handler-eligible output has N=0.
- ProfileSimilarityCalculator unit/property fixtures: case-insensitive weighted Jaccard using unnormalized preferences; two/one/no comparable channels; positive/negative/zero cosine and stable finite 384-vector extremes; malformed nonempty vector rejection; finite [0,1] profile output. Never substitute selected-candidate cosine.
- Handler fixtures and FsCheck: mixed/high-similarity/all-excluded Not Interested candidates neither scored nor explained nor returned; prove with existing seams/discriminating inputs, no spy-only abstraction. Preserve Score descending, Id ascending, then Take without refill; construct real profile once per request. Every result has deterministic Structural-then-Personal reasons, caller-safe generic Personal templates for positive comparable channels only when b>0, and Structural fallback when otherwise empty. Migrate Structural/golden expectations without changing DEV-338 selected-score semantics.
- Real PostgreSQL Testcontainers adapter tests: watched-only, launch-only, all launch events, watched-plus-launch without duplicate undated signal, genres and cancellation. Real candidate/contributor vector roundtrip verification remains blocked until actual acquisition lands; no mocked driver or empty-profile substitution.
- Endpoint/E2E/architecture and affected web consumer/mock tests: eligible ordering, raw finite scores, nonempty lowercase {kind,text} reason objects, safe generic strings, dependency boundaries and generated object consumers. UI/endpoint consume explanations without constructing them.

### Discoverable new export invocation

Quill must specify the exact opt-in selector/environment variable, command, working directory, output-path handling and validation for the NEW normalized registered-v1 test-host export in tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs. The draft must explain how Build proves ordinary test runs write nothing, explicit export produces web/src/api/openapi.json with the authorized DTO contract, and the committed normalized snapshot matches. Quill specifies tests; Build implements and executes them in Phase B.

Then, from web/, run npm run generate:api followed by npm run check:api for generated types.ts and mocks/handlers.ts. No hand edits to generated artifacts. No existing export invocation is claimed. Exact construction, options-validation/error and invocation details must be explicit in drafting with cited conventions; unresolved semantic choices return needs decision to Keel/Patron.

### Separate phases and gate expectations

Phase A remains specification-only. After Q12 closure: Quill drafts spec/plan/tasks; Keel performs read-only speckit-analyze and brief conformance; required plan challenge follows; plan freeze and Gate 1 remain separate decisions. No production tests or gates run during this grill.

Phase B requires merged specs, Epic 7 sequencing and fresh bounded dependency/drift recon against main establishing landed compatible DEV-338/348/353 contracts plus real candidate AND contributor 384-vector acquisition. Stop for dependency recon before source implementation when absent; no readiness waiver, silent scope reduction or partial substitute.

Pin supported commands/options and thresholds from authoritative current harness configuration through recon, without guessing flags or numeric thresholds. Required implementation verification includes focused unit/property, real infrastructure, API/architecture and affected web checks; backend build, Roslyn, complexity, inspections, properties, vulnerable packages, formatting, full tests and pre-PR mutation; current web gates apply to touched web scope. Fix surviving mutants with tests, never threshold reduction. PASS, SKIPPED (scope-empty), opt-out SKIP, mutation N/A and Could not run remain distinct. No configured opt-out becomes PASS.

M-tier review requires Sentry, Ledger and Compass reports; Q2 Critical/High/Medium blocking bar and maximum two rounds/two fix commits per round remain. Ship review, hosted checks and merge bar remain separate, and the user owns merging.

### Dependency-aware task order

1. Phase A drafting, read-only analysis/conformance and plan challenge, then separate freeze/Gate 1 decisions.
2. Phase B prerequisite/drift recon against main; stop if required input acquisition or predecessors remain unavailable.
3. Options, typed Core/wire contracts, pure calculators and focused tests.
4. Approved profile-input port/snapshot and real EF adapter, with PostgreSQL tests.
5. Handler prefilter, profile-once construction, blend/reasons/order and minimal source/options composition.
6. Endpoint DTO and integration tests.
7. New opt-in normalized-v1 export and snapshot, then web generate:api/check:api and affected consumer tests.
8. Full backend/web gates, pre-PR mutation, required reviews and hosted checks, then user-owned merge bar.

Focused tests accompany implementation steps. No permanent partial delivery, new schema/dependency, missing-vector replacement or generated hand edits.

## Current handoff after Q11 (supersedes Q10 next step)

Eleven questions used; one remains within the twelve-question cap. Q11 proof strategy, gate expectations, phase ordering and exact composition-root identities are settled and persisted. No remaining composition-root path recon is requested. No source/generated edits, commits, generators, tests or gates run; no plan freeze or Gate 1.

OPEN: Q12 grill closure and complete brief readiness for Quill drafting. Exact next step: Conductor routes Keel Q12 with Patron to ratify shared understanding, final scope/closing bar/caps and the completed Q1-Q11 decision/file/test/gate/order ledger, explicitly carrying Phase B acquisition prerequisites and Quill invocation/construction obligations. Then record the Q12 exchange before declaring a drafting handoff. Q11 itself does not authorize that handoff.



## Q12 - CONTROLLING GRILL CLOSURE AND DRAFTING HANDOFF (2026-10-22)

GRILL CLOSED at 12/12 by explicit Patron Q12 ruling. Shared understanding reached; Q1-Q11 are ratified as the complete decision brief for Quill drafting. Full exchange is appended in CONCLUSIONS.md Q12. Basis: DEV-354:35,40,42,44,46,48,50; recon-DEV-354-2:88-117,128-170; Q1-Q11.

This section controls current status and supersedes historical OPEN, missing-ticket, unresolved composition-path and next-step statements above without rewriting their text. Q4 closed the missing-ticket blocker; Q11 completed the exact composition-root identities. The title and earlier provisional/open states remain historical. Grill closure approves drafting only: plan freeze, Gate 1, gate1: provisional and runtime readiness are NOT granted.

Worktree F:/Dev/LamuFlix.worktrees/feature-354-spec; branch feature/354-spec; evidence HEAD d794a0e0643a012d877e736349e02a2af59d9691; intake base 53f4205d901a1798f41077bbd67127305dff301c. No new source-state measurements claimed in Q12.

### Final agreed scope for drafting

Literal raw finite Score = a*S + b*P - c*W - d*N, Q5 configurable coefficient names/defaults/finite validation, pre-weight terms [0,1], no final clamp/division/outer renormalization. S is DEV-338 selected similarity; P follows Q6 weighted Jaccard on unnormalized genre preferences and positive scaled 384-vector cosine, mean of comparable channels or zero if none. Typed Structural/Personal Core reason kind and lowercase wire {kind,text}, deterministic Structural then Personal order, Q7/Q8 generic positive-channel Personal templates only when b>0, caller-safe text, Structural fallback for every otherwise unexplained result.

Actual materialized watched/launch/genre/vector inputs via Q9 IRecommendationProfileSource, immutable RecommendationProfileInputs and EfRecommendationProfileSource; Q10 NEW PlayedAt read query preserves every launch and watched-only age-zero fallback. Pure calculator direct calls/construction; minimal scoped-compatible composition wiring. Handler excludes Not Interested BEFORE scoring/reasons, preserves Score-desc/Id-asc/Take, no refill. C# DTO -> normalized OpenAPI -> generated TypeScript and mocks, with affected consumers migrated. Authorized edits remain exactly Q8-Q11 ledger, including Q11 full composition-root paths; only necessary changes, no unrelated rewrites.

No new dependencies/projects/layers/schema/vector store or entity/configuration/playback-writer changes, MMR (DEV-416), named-title explanation template, missing-infrastructure substitute or selected-cosine reuse. No delivery reduction or constitution departure approved; neither owner section 2.3 escalation newly raised. Future necessary ticket change or constitution departure follows the standing escalation rule. anything else is a follow-up issue, not a finding in this round.

### Closing bar, caps and proof/order

Critical/High/Medium findings block; Low is non-blocking. Maximum two review rounds and two fix commits per round; no third-round authorization or gate waiver. M-tier adjudication requires Sentry, Ledger and Compass. Q11 behavioral/property/PostgreSQL/API/architecture/web proof matrix, contract validation, honest gate status distinctions and dependency-aware task ordering remain authoritative.

### Mandatory Quill draft details

Quill drafts spec.md, plan.md and tasks.md from this brief. Specify the NEW explicit opt-in normalized registered-v1 test-host export in tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs with a discoverable selector/environment name, exact command, working directory and output-path handling. Specify validation proving normal tests write nothing, explicit export writes web/src/api/openapi.json reflecting the authorized DTO shape, and tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json matches.

Build implements and executes that validation in Phase B. After explicit export, from web/: npm run generate:api then npm run check:api. Never hand-edit generated web/src/api/types.ts or web/src/mocks/handlers.ts. No existing exporter command is claimed.

Quill must pin concrete construction, options validation/error details and supported gate commands/thresholds to cited recon/current harness evidence. Missing facts return needs recon; unresolved semantic choices return needs decision to Keel/Patron, without scope or architectural changes or silent assumptions. These are bounded drafting obligations, not permission to invent infrastructure.

### Phase B STOP and separate approvals

Before source implementation: merged specs, Epic 7 sequencing, fresh bounded main drift/dependency recon establishing compatible landed DEV-338/348/353 contracts AND real candidate plus contributor 384-vector acquisition. Stop if unavailable; no dependency waiver, invented store, always-empty profile or selected-cosine substitution. Per-movie missing-channel handling cannot replace absent infrastructure.

After drafting: Keel read-only speckit-analyze and brief conformance, required plan challenge, then separate plan-freeze and Gate 1 decisions. Implementation/readiness, backend/web verification, mutation, all-axis review, ship review, hosted checks, merge bar and user-owned merge remain separate phases. Only decision documents changed in this exchange; no commits, production/generated edits, generators, tests or gates run.

### Exact next owner step

Conductor routes Quill to draft specs/DEV-354/spec.md, plan.md and tasks.md from this closed brief, preserving the exact Q8-Q11 file ledger, Q11 proof/order, opt-in export invocation/validation obligations and Phase B readiness STOP. This handoff becomes reportable only after Q12 full exchange and this closure are persisted/read back. Keel performs the post-draft read-only analysis/conformance when routed. No remaining grill question; budget complete at 12/12.

## CONTROLLING POST-Q12 EXPORTER CLARIFICATION (2026-10-22)

Authority: Patron exporter YES recorded at DEV-354:59, answering the clarification recorded at DEV-354:58; recon-DEV-354-3:55-65. Full recorded Q&A: CONCLUSIONS.md Post-Q12 exporter clarification; policy mirror: ASSUMPTIONS.md post-Q12.

This controls exporter decisions and supersedes earlier separate NEW-operation/new-selector requirements, LAMUFLIX_OPENAPI_EXPORT=normalized-v1 wording and assertions that no existing exporter exists, including Q9-Q12 export wording and brief.md:316-318. Prior text is preserved as history. Extending the EXISTING operation/helper as the single writer satisfies the Q9-Q12 opt-in normalized registered-v1 requirement; no duplicate writer or dual-switch contract is authorized.

- Retain tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs operation OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether and helper WriteRegeneratedOutput. Retain case-insensitive LAMUFLIX_REGENERATE_OPENAPI=true. Unset or any non-true value skips the writer; ordinary tests write neither artifact and verify drift.
- Reuse RepositoryRoot: walk up to eight ancestors from AppContext.BaseDirectory for exactly one Directory.Packages.props; zero/multiple matches throw InvalidOperationException. Reuse absolute CommittedBaselinePath / DerivedSnapshotPath resolution and IsWithinRoot containment guards.
- Preserve paired writes of normalized registered /openapi/v1.json to web/src/api/openapi.json AND tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json through the one writer.
- Preserve Core Structural/Personal with caller-safe Text and lowercase wire {kind,text}. Migrate the OpenAPI reasons assertion from string items; verify authorized reason shape and normalized host/baseline/snapshot equality.
- Quill specifies the discoverable retained selector/env, exact command/cwd, guarded output paths and validation; Build implements/executes in Phase B. Prove true opt-in writes BOTH and unset/non-true writes NEITHER. Content equality alone is insufficient no-write proof.
- After authorized DTO/assertion changes, opt-in regenerate BOTH normalized artifacts; clear LAMUFLIX_REGENERATE_OPENAPI even on failure; then final ordinary read-only drift/equality/no-write checks. Subsequently from web/: npm run generate:api then npm run check:api, consumer migration and affected consumer tests. Consumer tests depend on generation/freshness and migration. Never hand-edit generated types.ts or mocks/handlers.ts.

This persistence step appends only CONCLUSIONS.md, brief.md and ASSUMPTIONS.md. Patron authorization to correct spec.md:89, plan.md:181-225 and tasks.md:148-157 is carried for subsequent routing; those drafts remain unchanged now. Other construction/error/gate-detail corrections remain open. Q12 scope/bar/caps and Phase B dependency STOP remain controlling. No plan challenge, freeze, Gate 1, code edits, tests, gates or generators.

Verified identity for this persistence step: worktree F:/Dev/LamuFlix.worktrees/feature-354-spec; branch feature/354-spec; HEAD d794a0e0643a012d877e736349e02a2af59d9691; intake base 53f4205d901a1798f41077bbd67127305dff301c.

### Persistence and exact read-back receipt (2026-10-22)

Exact appended bytes were read back and matched the intended text for CONCLUSIONS.md:215-230 (recorded Q&A at :222-228), brief.md:331-347 (controlling decision at :332-347), and ASSUMPTIONS.md:41-44 (policy mirror at :42-44). Every pre-existing byte in those three files was preserved. spec.md, plan.md and tasks.md were verified byte-identical to their pre-edit contents. Verification was document persistence/read-back only; no tests, gates or generators ran.


## CONTROLLING POST-Q12 SCORER-ERROR-POLICY CLARIFICATION (2026-10-22)

Authority: Patron ruling at DEV-354:61, answering the policy proposal recorded at DEV-354:58. Full recorded Q&A: CONCLUSIONS.md Post-Q12 scorer-error-policy clarification. Basis: CONCLUSIONS.md Q5; recon-DEV-354-3:14,16-17. Assumed policy mirror: ASSUMPTIONS.md post-Q12 scorer-error-policy clarification.

This controls the previously unresolved exact blend-error policy in Q5 and subsequent drafting obligations, without rewriting earlier records:

- Negative or nonfinite blend members: ArgumentOutOfRangeException, following the existing invalid-weight precedent in recon-DEV-354-3:14.
- a=b=0: existing IValidatableObject field-level ValidationResult via the established options validation/startup path (BindAndValidate / ValidateDataAnnotations / ValidateOnStart), per recon-DEV-354-3:16-17. Preserve a > 0.0 || b > 0.0; do not add a+b for validation.
- Otherwise-finite inputs producing a nonfinite calculated raw score: InvalidOperationException without clamping. This exception choice is Patron specification policy; recon did not identify an existing calculated-score nonfinite exception precedent.

Q5 still requires each a/b/c/d finite and nonnegative, bounded pre-weight terms, binary watched/not-interested indicators and raw finite Score = a*S + b*P - c*W - d*N. No final clamp, division or outer renormalization; negative and above-one finite scores remain allowed.

Specification policy only, not implementation approval. Quill must carry these exact distinctions into subsequent separately routed spec/plan/tasks corrections; this step edits none of those drafts. Existing exporter clarification, closed Q12 scope/bar/caps, other conformance corrections and Phase B dependency STOP remain controlling. No source edits, tests, gates, generators, plan challenge, freeze or Gate 1.

Verified identity: worktree F:/Dev/LamuFlix.worktrees/feature-354-spec; branch feature/354-spec; evidence HEAD d794a0e0643a012d877e736349e02a2af59d9691; intake base 53f4205d901a1798f41077bbd67127305dff301c.

## CONTROLLING C3 PERSONAL GENRE-REASON MULTIPLICITY (2026-10-22)

Authority: Patron contract ruling recorded in DEV-354:76, following Keel C3 adjudication/question at DEV-354:75 and findings-DEV-354-Compass:12. Recorded Q&A: CONCLUSIONS.md C3 Personal genre-reason multiplicity. Assumed policy mirror: ASSUMPTIONS.md C3.

Emit one Personal genre reason per canonical positive-mass overlapping genre in OrdinalIgnoreCase then Ordinal order. This resolves the prior Q7/Q8 singular/plural ambiguity; do not select only the first such genre. Preserve Q6 canonicalization and Q7/Q8 eligibility (b>0, positive comparable genre channel), caller-safe sanitized Matches your <genre> interest text, existing Structural-first ordering, plot-channel Personal reason and Structural fallback rules. No new canonicalization or display policy is introduced.

Quill must mirror this exact multiplicity in the separately routed spec/plan/tasks correction, including reason fixtures with multiple qualifying genres and exact deterministic ordering. C1/C2 plan/tasks corrections remain distinct from this decision. Prior historical records are preserved. Grill remains closed at 12/12; Phase B NOT READY and T009 STOP remain mandatory. This decision does not freeze the plan, set Gate 1, or authorize implementation.

Evidence identity: F:/Dev/LamuFlix.worktrees/feature-354-spec; feature/354-spec; HEAD d794a0e0643a012d877e736349e02a2af59d9691.
