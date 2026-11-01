# Feature Specification: TMDb Keywords, Full Cast, and Collection Signals in MovieFeatures

**Feature Branch**: `feature/358-spec`
**Created**: 2026-11-01
**Status**: `gate1: provisional` — PLAN FROZEN / OWNER DECISION PENDING (12/12; `/speckit-analyze` Round 3 CLEAN; Plan Challenge Round 2/2 closed with 23/23 findings resolved via R1–R10; Q6 collection FK vs join-table owner checkbox unticked; Gate 1 remains closed until the user answers on the spec PR).
**Input**: Ticket DEV-358 (parent DEV-288, Size M); controlling `brief.md` (Q1–Q12 + Plan Challenge Round 1 P1–P12); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-358`, `recon-DEV-358`, `recon-DEV-358-2`, `adjudication-DEV-358-plan`; `specs/PRODUCT.md`

## Owner checkbox — Gate 1 closed

- [ ] blocked: structural — Should DEV-358 retain the landed DEV-424 collections table and nullable movies.collection_id FK as the sole collection-membership persistence (adding only keyword persistence/join table and migration) instead of the collection join table named in DEV-358 Scope and Technical Design, or transition collection membership to a join table?

Patron Q6 recommends retaining the landed collections table plus `movies.collection_id` as the single collection-membership source of truth and adding only keyword persistence and the separately approved full-cast provenance join. This is a recommendation pending the user, not FK approval. Basis: role 2.3(a); recon-DEV-358-2:11-12 versus :94-102,104-120,170-172; brief Owner decision; ASSUMPTIONS.md owner decision section.

**Gate 1 stays closed until the user answers.** A Patron answer never ticks this checkbox. Carry this checkbox verbatim and unticked into the eventual spec PR. The planned path below uses the recommended FK branch conditionally. If the user chooses a collection join-table transition, amend brief/plan and the affected exact file/test/task boundary before build; no automatic alternate design, duplicate membership state, or round-cap extension.

## Scope

**In scope** (authoritative ticket Scope & Technical Design + ACs restored at recon-DEV-358-2:7-19; brief Ticket delivery preserved; Q5–Q11; Plan Challenge Round 1 R1–R10):

1. Enrich `MovieFeatures` with TMDb keywords, full cast, and franchise collection signals when TMDb is enabled (`TmdbOptions.Enabled`).
2. Add keywords as a set on overlap terms (DEV-338 path) and a same-collection overlap term keyed by exact positive collection ID.
3. Persist keywords through the DEV-411 relational-association pattern (`keywords` + `movie_keywords` + migration). Persist TMDb full-cast provenance via `movie_tmdb_cast` reusing existing actor identities (Care-item-3 approval, Q8; retained adapter-source provenance name outside provider-neutral Core, R10). Collection membership persistence follows the Q6 owner answer; planned draft path is the recommended FK reuse of landed DEV-424 `collections` + nullable `movies.collection_id` (no duplicate collection ingestion).
4. Compare DEV-356 Hit Rate@10 and MRR with and without TMDb signals on the same replay set; record both in tracked `specs/DEV-358/comparison.md` (R6).
5. With TMDb enabled, keywords/collections are persisted and included in `MovieFeatures` for movies enriched through supported enrichment paths (incremental; no automatic backfill/sweep, R9).
6. Hit Rate@10 with signals is no lower than baseline on the DEV-356 replay set (exact HitCount bar, Q10).
7. Same-collection franchise movies are grouped together in the top 10 for a seed movie in that collection (Q9). Leading franchise-block displacement of non-franchise items is an accepted taste cost (R9).
8. Exactly the Q11 named files/tests/tasks/gates as amended by Plan Challenge Round 1 bounded corrections (R3/R5/R6/R7); Q7 assumed keyword/credits contracts; Q8 supplemental merge and scoring; Q9 `SeedCollectionGrouper`; Q10 paired evaluation protocol.

**Out of scope** (brief frozen scope exclusions; Q4/Q8/Q11/Q12; R9/R10):

- New NuGet/npm dependency, project, Core port, architectural layer, public HTTP DTO/route/OpenAPI/TypeScript change, or frontend change.
- `Features:LocalPlay`, new secrets, secret logging, or `Process.Start`.
- Generic `MmrSelector` redesign, global/profile ranking redesign, or public grouping field on the flat recommendation response.
- Fabricated replay data; weakening goldens/thresholds; live TMDb calls during replay/CI; inventing a profile-replay seed.
- Automatic backfill/sweep of previously enriched titles; follow-up ticket for incremental enrichment (accepted limitation, R9).
- Unapproved collection join-table transition (requires brief/plan amendment after owner answer).
- Duplicate collection membership source of truth; second actor-identity table; billing/character/order/crew as features.
- Unrelated rewrites; ADR authorship (Quill never writes ADRs); README/CHANGELOG (README narrative-currency residual noted, no ticket, R10); Gherkin unless separately opted in.
- Anything else is a follow-up issue, not a finding in this round (Q12).

**Precedence**: Ticket Scope & Technical Design and ACs are already approved authority (`specs/PRODUCT.md:23–24`; recon-DEV-358-2:7-19). Historical DEV-331 deferrals do not reduce DEV-358 delivery (Q5). Q7–Q12 assumed taste/schema contracts in `ASSUMPTIONS.md` and Plan Challenge Round 1 controlling amendments govern drafting; they are design contracts, not verified external TMDb documentation. Historical Q1–Q12 answer texts remain verbatim. Q6 owner checkbox remains unresolved until the user answers; Gate 1 closed. Q1 build-HEAD dependency revalidation remains a Phase B entry blocker.

## Local vocabulary

| Term | Canonical meaning |
| --- | --- |
| Keyword | Positive TMDb keyword ID with a trimmed display name; identity and overlap use the ID. |
| Full cast | All valid actor names returned by the assumed TMDb credits cast payload, without billing cutoff; excludes crew and character/order features. Domain member name: `FullCast` (`ImmutableHashSet<string>` normalized names). |
| Base cast | Existing primary-provider Actors names, retained separately for disabled/ablation parity. |
| Franchise collection | Exact positive TMDb `belongs_to_collection` ID with display name; never inferred from title or collection-name similarity. Sole identity/validation: `MovieMetadata.CollectionId` / `CollectionName`. |
| Unavailable signal | No authoritative replacement: preserve the corresponding persisted signal (`SignalAvailability.Unavailable`). |
| Known empty | A valid empty payload (or valid no-collection): replace/clear that signal (`SignalAvailability.KnownEmpty`). |
| Available signal | Authoritative non-empty (or known collection) payload: replace with valid values (`SignalAvailability.Available`). |

Settled by Q7–Q12, Analyze Round 1 repair C2, and Plan Challenge Round 1 R3/P7: the six glossary terms above MUST also appear as canonical `CONTEXT.md` glossary entries (`term` / definition / `_Avoid_:`) in the same introducing PR (Constitution Principle VIII; `recon-DEV-358-2:174-192`). Prior C1 identifier `ExtendedCast` is amended to `FullCast`. No `MovieCastEntry` type. No new ADR for this M ticket.

## User Scenarios & Testing

### User Story 1 - Ingest and Supplement TMDb Signals (Priority: P1)

As the enrichment pipeline with TMDb enabled, I need keywords, full cast, and collection signals fetched for the same resolved movie and merged onto an OMDb Found result without overwriting base OMDb metadata, so recommendation features can use truthful supplemental signals even when OMDb already succeeded.

**Why this priority**: ticket signals item and AC “with TMDb enabled… included in MovieFeatures”; chain currently first-success with no merge (recon-DEV-358-2:62-82); Q7–Q8; R4.

**Independent Test**: offline WireMock keyword/credits/details fixtures through existing `TmdbMetadataProvider` HTTP client in `IntegrationTests/MetadataProviderTmdbSignalTests.cs` (R5); pure chain unit tests without a duplicate HTTP suite; chain matrix preserving primary/fallback when no primary Found; after OMDb Found, one same-movie TMDb supplement covering the full request sequence and typed outcome matrix; identity/malformed/empty/unavailable/cancellation/exception cases; disabled mode makes zero TMDb requests; representative large-cast correctness smoke without cutoff/cardinality/performance gate (R5).

**Acceptance Scenarios**:

1. **Given** TMDb enabled and resolved movie identity **When** one logical provider invocation runs **Then** identity is resolved via matched IMDb ID or existing title/year policy, details supply collection, then keywords and credits are fetched; assumed shapes `{id, keywords:[{id,name}]}` and `{id, cast:[{id,name,order?,character?}]}` are parsed with response movie ID equal to the resolved positive ID; never duplicate a TMDb fallback Found lookup (Q7–Q8; R4).
2. **Given** positive keyword/person IDs and nonblank trimmed names **When** normalizing **Then** keywords dedupe by positive ID (ordinal-smallest trimmed name on conflict), cast dedupes with existing trimmed case-insensitive actor conventions into `ImmutableHashSet<string>`, cast is not truncated, and crew/character/order never enter features (Q7; R3).
3. **Given** present valid empty arrays **When** parsing **Then** that signal is KnownEmpty (authoritative clear); missing/null arrays, wrong shape, mismatched movie ID, or any malformed item makes that entire signal Unavailable without invalidating independently valid signals; partial cast is never accepted as complete (Q7; R1/R3).
4. **Given** no primary Found **When** the chain runs **Then** the existing primary/fallback outcome matrix is preserved (Q8; R4).
5. **Given** OMDb Found and TMDb enabled **When** supplementation runs **Then** TMDb is invoked once for the same movie; only keyword/`FullCast`/collection signals merge; OMDb scalar/genre/director/writer/base Actors are preserved; collection comes from details; no duplicate TMDb lookup when fallback already returned TMDb Found (Q8; R4).
6. **Given** primary OMDb Found and supplemental NotFound or typed Failed (`RateLimited` / `ProviderUnavailable` / `InvalidResponse`) **When** merge completes **Then** overall outcome stays Found/`MarkEnriched` with Unavailable affected signals, existing adapter retry/health accounting retained, and no raw exceptions stored or logged as signal success; valid KnownEmpty/no-collection clears that signal; caller cancellation and unexpected exceptions still propagate; a failed supplemental call is not signal success (Q8; R4).
7. **Given** TMDb disabled **When** enrichment runs **Then** zero TMDb requests occur and stored signals are not erased (Q8; R1).

---

### User Story 2 - Persist Signals and Expose MovieFeatures Terms (Priority: P1)

As the recommendation feature bag builder, I need keywords, TMDb cast provenance, and collection identity persisted atomically and rehydrated into defensive `MovieFeatures` terms with configurable keyword/collection weights, so overlap scoring and disabled/ablation parity stay honest.

**Why this priority**: ticket persistence + AC “keywords and collections are persisted and included in MovieFeatures”; Q8 Care-item-3 schema; Q6-conditional collection FK; R1–R3.

**Independent Test**: Testcontainers PostgreSQL roundtrip/replace/clear/unavailable-preserve/disabled-preserve/concurrency/rollback; tracked-load before replace; equality/dirty-tracking and mixed-state save proofs; amended DEV-424 clear-on-OMDb collection proof; mapper/equality/hash/invariant property tests; disabled builds base-cast-only features without keyword/collection terms.

**Acceptance Scenarios**:

1. **Given** Available keyword/`FullCast`/collection signals **When** a successful movie save runs **Then** `keywords(id,name)`, `movie_keywords`, and `movie_tmdb_cast` (reusing existing actor identities, no second actor table) update atomically inside the existing save transaction after tracked loading of keyword joins, full-cast joins, and related identities; deterministic locks cover keyword IDs in a distinct stable namespace and both base/`FullCast` actor natural keys under existing upsert discipline (Q8; R1/R2).
2. **Given** per-signal availability **When** save finishes **Then** Available replaces with valid values; KnownEmpty clears that signal; Unavailable or Disabled performs no signal write for that signal (strict no-op preserving prior data); explicit metadata clear-all clears all signals; nullable `CollectionId` alone never clears; independently mixed signal states persist; entire save rolls back on failure (Q8; R1/R2).
3. **Given** supplemental collection Unavailable or TMDb Disabled after OMDb Found **When** collection persistence is considered **Then** DEV-424 clear-on-OMDb-success is superseded for that unavailable/disabled collection case (prior collection preserved); known-no-collection (KnownEmpty) clears still apply; availability gates CopyMetadata, not nullable ID alone (R1; P1).
4. **Given** owner-approved Q6 FK branch **When** collection signals are Available **Then** existing DEV-424 collection mapping/FK persistence is reused without duplicate collection ingestion or a second membership source; join-table transition is not implemented unless the user selects it and brief/plan are amended (Q6, Q8).
5. **Given** rehydration paths (candidate/profile/detail) **When** features are built **Then** signal snapshots and availability semantics are preserved without adding public DTO fields; `MovieSignalSnapshot` carries collection availability only while collection identity stays on `MovieMetadata` (Q8; R3).
6. **Given** enabled TMDb signals **When** mapping to `MovieFeatures` **Then** defensive keyword-ID set `Keywords` (`ImmutableHashSet<int>`) and nullable positive `CollectionId` are present; effective Cast is base Actors ∪ `FullCast` (Q8; R3).
7. **Given** disabled TMDb signals **When** mapping **Then** features use base cast only and omit keyword/collection terms and grouping (Q8).
8. **Given** keyword/collection weights **When** scoring **Then** keyword overlap uses existing set-overlap/active-term rules; collection term is 1 for equal known IDs, 0 for different known IDs, absent when either ID is null; defaults `SimilarityWeights.Keyword=1.0` and `Collection=2.0` are configurable finite nonnegative (zero removes the term); score bounds/symmetry and existing reason surface are preserved (Q8; ASSUMPTIONS.md).

---

### User Story 3 - Seed Franchise Top-10 Grouping (Priority: P1)

As a user requesting recommendations for a seed movie that belongs to a franchise collection, I need eligible same-collection library members grouped contiguously at the front of the top 10, so franchise adjacency is guaranteed even when the vector shortlist would omit a member.

**Why this priority**: ticket AC “Franchise movies in the same collection are grouped together in the top 10”; weight alone cannot guarantee it (Q9).

**Independent Test**: pure Core `SeedCollectionGrouper` unit/property tests plus production seed-handler and API integration cases covering no-op, deterministic order/ties, capacity, more-than-ten, eligibility/dedup (including embedding-less eligible locals); generic MMR/profile behaviour unchanged. Profile replay is not seed proof (R6/R9).

**Acceptance Scenarios**:

1. **Given** enabled signals and a seed with positive `CollectionId` **When** candidates are assembled **Then** index-backed recommendation-eligible local-library members with that exact ID are unioned into the existing candidate source before cutoff, without whole-catalog scan, excluding seed, duplicates, unavailable/ineligible items, and external-only titles; otherwise eligible local members need not have persisted embeddings and rehydrate base plus persisted signal data using the existing missing-plot fallback with no embedding writer or global-profile change (Q9; R9).
2. **Given** eligible same-collection candidates **When** results are ordered **Then** at most `min(10, requested capacity)` form a contiguous leading block ranked by existing seed `SimilarityScorer` descending then stable `MovieId`; if more than ten, choose the highest-scoring ten; if fewer, include all; leading-block displacement of non-franchise items is accepted taste cost (Q9; R9).
3. **Given** the leading block selected **When** remaining slots fill **Then** they retain unchanged MMR order with selected IDs removed; real item scores/reasons and existing result-size bounds are kept; no public grouping field is added (Q9).
4. **Given** disabled signals, null seed collection, or no eligible matches **When** ranking runs **Then** output order is an exact no-op relative to pre-grouper behaviour (Q9).
5. **Given** seed grouping verification **When** acceptance is judged **Then** production seed-handler, shared helper, and API integration cases prove the AC; profile replay / `specs/DEV-358/comparison.md` explicitly does not prove seed grouping (Q10; R6/R9).

---

### User Story 4 - Paired DEV-356 Evaluation Receipt (Priority: P1)

As an operator measuring TMDb signal impact, I need Disabled and Enabled runs on the same provenance-backed enriched fixture with identical nonzero denominator so Hit Rate@10 does not regress and both HR@10 and MRR are recorded.

**Why this priority**: ticket measurement Scope item and AC “Hit Rate@10 with TMDb signals is no lower than baseline… both numbers recorded”; Q10; R6–R8.

**Independent Test**: fixture/options/CLI isolation tests via `EvaluationEntry` + pure `EvaluationReplayOptions.Default`; Disabled reproduces DEV-356 baseline; Enabled exercises independently nonzero keyword and collection coverage with positive weights; paired publish commands produce tracked comparison receipt; empty coverage or HR regression is BLOCKED/FAIL.

**Acceptance Scenarios**:

1. **Given** `baseline.tmdb.json` **When** constructed **Then** it retains exact original DEV-356 movie IDs, events/timestamps/history, labels, base features, and ranking inputs; only signal fields are enriched from truthful provenance-backed TMDb associations; ranking explicitly adds `similarityWeights.keyword=1` and `collection=2` while retaining all original ranking fields; original `baseline.json` / `baseline.expected.json` are untouched; scrubbed source IDs/response hashes live in `baseline.tmdb.sources.json` without `api_key` URLs; missing added weight fields in old fixtures default to record defaults (Q10; R8).
2. **Given** `-TmdbSignals Disabled|Enabled` (default Disabled) **When** `scripts/run-evaluation.ps1` runs **Then** `tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs` reads child-only `LAMUFLIX_EVALUATION_TMDB_SIGNALS` strictly, unset means Disabled, rejects invalid values, and passes the parsed value to pure `EvaluationReplayOptions.Default`; explicit options stay environment-independent; no `EvaluationRunner` type (Q10–Q11; R7).
3. **Given** the two pinned commands with the SAME fixture, seed 356, clocks, observation order, eligible cohort, cutoff 10, lambda, and identical positive weights **When** both publish **Then** Disabled strips TMDb keywords/collection and uses base cast while still transporting the same positive weights; Enabled uses new features/effective cast and independently exercises nonzero keyword and collection terms with nonzero weights; empty/no-op or cast-only coverage does not prove signal measurement; history-derived relevance is unchanged (Q10; R8).
4. **Given** paired receipts **When** acceptance is judged **Then** eligible observations and nonzero denominator are identical; Disabled reproduces the existing DEV-356 baseline; Enabled exercises independently nonzero provenance-backed keyword and collection coverage; Enabled HitCount ≥ Disabled HitCount at that denominator (no rounded-HR pass); tracked `specs/DEV-358/comparison.md` records exact commands, raw/fixture/source SHA-256, seed/mode/cohort/denominator/hit counts/HR@10/MRR/deltas/lambda/weights/independent coverage and scrubbed provenance; raw `baseline.json`/`tmdb.json` remain under ignored `artifacts/evaluation/DEV-358` and are never force-added; MRR is reported, not an added no-regression gate; receipt states profile replay does not prove seed grouping (Q10; R6).
5. **Given** untruthful/unavailable provenance, empty coverage, or HR regression **When** measurement finishes **Then** result is BLOCKED/FAIL and the measurement task is incomplete (Package 8 remains locked); no fabricated associations, cherry-picked history, weakened goldens, synthetic acceptance replay, or live TMDb in CI (Q10; R6).

### Edge Cases

- Present valid `[]` is KnownEmpty (clear); missing/null/wrong shape/mismatched ID/malformed item is Unavailable (preserve) per signal independently (Q7; R1).
- Partial cast must never count as complete full cast; uncapped personal-library cast retained with large-cast correctness smoke only (Q7; R5).
- Supplemental NotFound/typed Failed after OMDb Found keeps overall Found/`MarkEnriched` with Unavailable affected signals; never claim signal success or erase prior persisted signals (Q8; R4).
- Disabled TMDb: zero requests, preserve stored signals, base-cast-only features, no keyword/collection terms or grouping; unavailable/disabled collection supersedes DEV-424 clear-on-OMDb-success (Q8; R1).
- Nullable `CollectionId` alone never clears; KnownEmpty no-collection clears still apply (R1).
- Collection term absent when either `CollectionId` is null; same name with different IDs does not match (Q8–Q9).
- Seed grouping no-op when disabled, null seed collection, or no eligible local members; embedding-less eligible locals remain union-eligible; never invent external-only titles (Q9; R9).
- Two empty/no-op evaluation runs are not acceptance; independently nonzero exercised keyword and collection coverage required (Q10; R8).
- Q6 join-table branch is not an automatic alternate path; requires amendment before build (Q6, Q11).

## Requirements

### Functional Requirements

- **FR-001**: System MUST, in one logical TMDb provider invocation after existing identity resolution (matched IMDb ID or existing title/year policy), fetch details (collection), then `movie/{resolved_id}/keywords` and `movie/{resolved_id}/credits` through the existing `TmdbMetadataProvider` HTTP client, base URL, query-key `api_key` auth, resilience, and cancellation path, with no new SDK, port, layer, route, credential, or secret logging; never log raw credential-bearing URIs; never duplicate a TMDb fallback Found lookup (Q7; R4/R5).
- **FR-002**: System MUST normalize keywords and cast per Q7 identity/dedup/full-cast/missing-value rules (positive IDs, trimmed nonblank names, ID-keyed keyword dedupe with ordinal-smallest label tie-break, case-insensitive cast dedupe into `ImmutableHashSet<string>`, no billing cutoff, fail-closed per signal on malformed/unavailable shapes) and preserve existing positive collection ID/name validation on `MovieMetadata` (Q7; R3).
- **FR-003**: System MUST preserve the existing primary/fallback chain matrix when there is no primary Found. After OMDb Found with TMDb enabled, System MUST perform same-movie supplemental merge exactly once: overall Found/`MarkEnriched` survives supplemental NotFound or typed Failed (`RateLimited`/`ProviderUnavailable`/`InvalidResponse`) with Unavailable affected signals and existing adapter retry/health accounting; no raw exceptions stored/logged as success; cancellation and unexpected exceptions propagate; merge only keywords/`FullCast`/collection while preserving OMDb scalar/genre/director/writer/base Actors (Q8; R4).
- **FR-004**: System MUST carry per-signal availability (`Unavailable`/`KnownEmpty`/`Available` nested in `MovieSignalSnapshot.cs`) separately through immutable functionality-based Core domain data (`MovieKeyword`, `MovieSignalSnapshot`, `FullCast` as `ImmutableHashSet<string>`, keyword snapshots as `ImmutableArray<MovieKeyword>`) and the existing metadata/save path without a new layer, port, or `MovieCastEntry` type; `MovieSignalSnapshot` stores collection availability only; sole collection identity/validation stays on `MovieMetadata.CollectionId`/`CollectionName`; vendor-prefixed `Tmdb*` identifiers remain restricted to Infrastructure adapters and `TmdbOptions` (Q8, Q11; R3; Constitution VIII).
- **FR-005**: System MUST atomically persist Available keywords via `keywords`/`movie_keywords` and TMDb cast via `movie_tmdb_cast` reusing existing actor identities inside the existing movie save transaction after tracked loading of joins/identities and deterministic locks (keyword IDs in a distinct stable namespace; base and `FullCast` actor natural keys) under existing upsert discipline. Per signal: Available replaces; KnownEmpty clears; Unavailable or Disabled performs no signal write. Explicit clear-all MUST clear all signals. Entire save MUST roll back on failure (Q8; R1/R2).
- **FR-006**: Collection membership persistence MUST follow the Q6 owner answer. On the recommended FK branch, reuse landed DEV-424 collections + `movies.collection_id` without duplicate ingestion. Availability gates collection writes: Available replaces; KnownEmpty (known-no-collection) clears; Unavailable/Disabled is a no-op that explicitly supersedes DEV-424 clear-on-OMDb-success for unavailable/disabled collection while preserving known-no-collection clears; nullable `CollectionId` alone never clears. On a join-table selection, amend brief/plan/file boundary before build (Q6, Q8, Q11; R1).
- **FR-007**: `MovieFeatures` MUST expose defensive keyword-ID set `Keywords` (`ImmutableHashSet<int>`) and nullable positive `CollectionId`; enabled effective Cast MUST be base Actors ∪ `FullCast`; mapper, snapshots, equality/hash/dirty-tracking, and all rehydration paths MUST stay consistent; disabled MUST omit keyword/collection terms and grouping (Q8; R3).
- **FR-008**: `SimilarityScorer` MUST add keyword set-overlap and same-collection 1/0/absent terms with configurable finite nonnegative `SimilarityWeights.Keyword` (default 1.0) and `Collection` (default 2.0); zero removes the term; existing bounds/symmetry/reason surface preserved (Q8).
- **FR-009**: For enabled signals and positive seed `CollectionId`, the existing candidate-source contract/implementation MUST union indexed eligible local same-collection members before cutoff — including otherwise eligible members without persisted embeddings, rehydrating base and persisted signal data and using the existing missing-plot fallback with no embedding writer/global-profile change — and place at most `min(10, capacity)` in a contiguous leading block ordered by seed similarity descending then stable `MovieId`, filling remaining slots in unchanged MMR order; pure Core `SeedCollectionGrouper` under Recommendations (no fourth injected port); no generic `MmrSelector`/profile/API shape change (Q9; R9).
- **FR-010**: Evaluation MUST support `-TmdbSignals Disabled|Enabled` (default Disabled) via `EvaluationEntry` reading child-only `LAMUFLIX_EVALUATION_TMDB_SIGNALS` into pure `EvaluationReplayOptions.Default`, provenance-backed `baseline.tmdb.json` / `baseline.tmdb.sources.json` (explicit keyword=1 and collection=2 weights), and the exact paired publish commands; acceptance MUST enforce identical nonzero denominator, Disabled baseline reproduction, Enabled independently nonzero keyword and collection coverage with positive weights, and Enabled HitCount ≥ Disabled HitCount; record exact commands, hashes, metrics, weights, independent coverage, and scrubbed provenance in tracked `specs/DEV-358/comparison.md`; keep raw JSON under ignored `artifacts/evaluation/DEV-358` (never force-add); receipt MUST state profile replay does not prove seed grouping (Q10; R6–R8).
- **FR-011**: Seed franchise grouping MUST be verified independently through production seed-handler, shared helper, and API integration tests — not via inventing a profile-replay seed or claiming comparison.md as seed proof (Q10; R6/R9).
- **FR-012**: Implementation MUST stay inside the Q11 planned exact edit set as amended by Plan Challenge Round 1 (add `EvaluationEntry.cs`; remove planned `UnitTests/Adapters/TmdbMetadataProviderSignalTests.cs`; tracked comparison.md; EF migration timestamp tooling-assigned only); HTTP DTO/OpenAPI/TypeScript, generic `MmrSelector`, original baseline/golden, dependencies, harness thresholds, LocalPlay, README/CHANGELOG, and unrelated files remain excluded; WireMock payloads embedded in `IntegrationTests/MetadataProviderTmdbSignalTests.cs` (Q11; R5–R7/R10).
- **FR-013**: Phase B build entry MUST revalidate merged upstream DEV-331/DEV-411/DEV-356 contracts at build HEAD via bounded recon and MUST have a resolved Q6 owner checkbox before coding (Q1, Q11).
- **FR-014**: Tests MUST cover full/uncapped cast (including representative large-cast correctness smoke without performance/cardinality gate), identity mismatches, every empty/unavailable path, independent partial signal failures (valid-keywords/malformed-credits and inverse; mixed known-empty/unavailable), same-movie supplemental merge and every listed outcome/request sequence, base-metadata preservation, no duplicate fallback lookup, disabled parity, cancellation/unexpected exception propagation, tracked-load/reload-save/no-wipe, concurrent same-keyword insert, atomic rollback, mixed independent signal states, amended DEV-424 clear-on-OMDb collection proof, equality/dirty-tracking, roundtrip/provenance separation, rehydrated equality/hash, symmetry/bounds/zero/absent terms, same-name/different-ID collections, union recall/eligibility/dedup (embedding-less eligible), top-10 limits/no-op, and unchanged generic MMR/profile behaviour using existing xUnit + Shouldly + NSubstitute + AutoFixture, Theory/MemberData, FsCheck, offline WireMock, and real PostgreSQL Testcontainers — never mock the data driver; use `TimeProvider` and injected seeded `Random`; no explanatory comments or threshold changes (Q11; R2/R5).
- **FR-015**: Previously enriched titles gain new signals only through supported enrichment paths; no automatic backfill/sweep and no follow-up ticket for that limitation (R9).

### Key Entities

- **MovieKeyword**: Positive keyword ID + trimmed display name; immutable Core domain data; keyword snapshots use `ImmutableArray<MovieKeyword>` (R3).
- **MovieSignalSnapshot**: Per-signal snapshot with nested availability enum `Unavailable`/`KnownEmpty`/`Available`; carries keyword/`FullCast` snapshots and collection availability only — not collection identity (R3).
- **FullCast**: Supplemental full-cast actor names as `ImmutableHashSet<string>` normalized names, kept distinct from base Actors; no `MovieCastEntry` (R3; amends C1 `ExtendedCast`).
- **KeywordRecord / movie_keywords**: Relational keyword identity and movie membership (DEV-411 pattern).
- **movie_tmdb_cast**: Provenance join reusing existing actor identities (not a second actor table); approved source/provenance table name retained outside provider-neutral Core (R10).
- **Collection membership**: Landed DEV-424 `collections` + nullable `movies.collection_id` on recommended Q6 branch (owner-conditional); identity/validation on `MovieMetadata.CollectionId`/`CollectionName`.
- **MovieFeatures**: Defensive `Keywords` (`ImmutableHashSet<int>`), nullable `CollectionId`, enabled effective Cast.
- **SeedCollectionGrouper**: Pure Core helper producing contiguous leading same-collection block (no fourth port).
- **baseline.tmdb.json / baseline.tmdb.sources.json**: Provenance-backed enriched evaluation fixture (explicit keyword/collection weights) and scrubbed sources.
- **specs/DEV-358/comparison.md**: Tracked Phase B acceptance receipt (future output; not invented now).
- **EvaluationEntry**: Child-only mode transport into pure `EvaluationReplayOptions.Default` (R7).

## Success Criteria

### Measurable Outcomes

- **SC-001**: With TMDb enabled, keywords and collection identity are persisted and present in `MovieFeatures` for movies enriched through supported enrichment paths (ticket AC; US1–US2; R9).
- **SC-002**: Paired Disabled/Enabled evaluation on the pinned enriched fixture yields identical nonzero denominator, Disabled reproduction of the DEV-356 baseline, Enabled independently nonzero provenance-backed keyword and collection coverage with positive weights, and Enabled HitCount ≥ Disabled HitCount; both HR@10 and MRR plus hashes/commands/coverage recorded in tracked `specs/DEV-358/comparison.md`; raw JSON stays ignored under `artifacts/evaluation/DEV-358` (ticket AC; US4; R6/R8).
- **SC-003**: For a seed movie with positive collection ID and eligible local franchise members (including embedding-less eligible locals), those members occupy a contiguous leading block of at most ten in the recommendation top results under enabled signals, proven by production/API tests — not by profile replay (ticket AC; US3; R6/R9).
- **SC-004**: Disabled TMDb mode produces zero TMDb HTTP requests during enrichment and base-cast-only features without keyword/collection terms or grouping (US1–US3).
- **SC-005**: All FR-014 required coverage paths have automated test coverage and passing applicable harness-gate evidence; tests may be written alongside production code or red-first (TDD optional per `$implement`); no mandatory failing-then-passing execution receipt. Applicable harness gates (analyzers, complexity, InspectCode, property tests, vulnerable packages, format, pre-PR mutation without `--since`) report Pass / SKIPPED(scope-empty) / SKIP / N/A distinctly — never folding Could not run into Pass (Q11–Q12; Analyze Round 1 I3).
- **SC-006**: No new dependency, project, port, layer, public API/frontend surface, LocalPlay/secret change, README/CHANGELOG rewrite, or harness-threshold edit is introduced (Q4, Q11, Q12; R10).

## Assumptions

Copied from `ASSUMPTIONS.md` (controlling [assumed] set + Plan Challenge Round 1 amendments); full basis there and in CONCLUSIONS Q7–Q12 / P1–P12:

- Assumed external keyword/credits DTO shapes and fail-closed unavailable semantics (Q7).
- Default weights Keyword=1.0 and Collection=2.0; enriched fixture transports them explicitly (Q8; R8).
- Contiguous leading block, within-block similarity-desc + MovieId tie-break; leading-block taste cost retained (Q9; R9).
- CLI flag `-TmdbSignals`, fixture/artifact paths, `LAMUFLIX_EVALUATION_TMDB_SIGNALS` via `EvaluationEntry` → `EvaluationReplayOptions.Default`, WireMock payloads in IntegrationTests (Q10–Q11; R5/R7).
- Q6 FK reuse is a Patron recommendation only — never an assumption of user approval.
- Incremental enrichment / no backfill accepted; embedding-less eligible seed union accepted (R9).

## Clarifications

### Session 2026-11-01

- Q1–Q12 grill closed; brief READY FOR QUILL; OWNER DECISION PENDING on Q6.
- External TMDb schemas not available in tree; Conductor authorized Patron [assumed] contracts (DEV-358:82-87; Q7/Q12).
- Collection persistence literal join-table wording vs landed FK is the sole owner checkbox; Gate 1 closed.
- Plan Challenge Round 1 P1–P12 ratified with confirmed corrections; R1–R10 applied to derived artifacts; five High findings pending Keel read-back; hard two-round plan-challenge cap retained.
- No `[NEEDS CLARIFICATION]` markers invented by Quill; unresolved user choice is the Q6 checkbox above.

## Traceability

| Source | Where encoded |
| --- | --- |
| Ticket Scope/ACs (recon-DEV-358-2:7-19) | Scope; US1–US4; SC-001–SC-003 |
| Q6 owner checkbox | Owner checkbox section (verbatim unticked) |
| Q7 ingestion | FR-001–FR-002; US1 |
| Q8 merge/persist/features | FR-003–FR-008; US1–US2 |
| Q9 grouping | FR-009; US3; SC-003 |
| Q10 evaluation | FR-010–FR-011; US4; SC-002 |
| Q11 files/tests/gates | FR-012–FR-014; SC-005–SC-006 |
| Q12 loop discipline | Status; Out of scope; checklist/plan |
| Plan Challenge R1–R10 | US1–US4; FR-001–FR-015; Key Entities; Assumptions |
| ASSUMPTIONS.md | Assumptions; Local vocabulary |
