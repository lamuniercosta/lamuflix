# DEV-358 technical brief

## Status and identity

**PLAN FROZEN / OWNER DECISION PENDING — 12/12 questions used; plan challenge closed at Round 2/2 (2026-11-01).**

- Worktree: F:/Dev/LamuFlix.worktrees/feature-358-spec
- Branch: feature/358-spec
- Verified HEAD: c2d78146b359bd17ab36ecaa9f33342272d47ebf
- Ticket: DEV-358, size M; specification phase only.
- Scope and approach are PLAN FROZEN after R1-R10 closure verification of all 23 three-axis findings, subject to the explicit Q6 owner decision below. Gate 1 remains closed; no build pickup, test/gate/metric PASS, or merge permission.
- No Q13 or cap reset. Drafting, analysis and plan challenge are complete; the Q6 owner answer is required before Gate 1 opens.

This is the consolidated controlling brief ratified by Q12. It replaces the earlier open/parked brief presentation, not the historical exchanges: CONCLUSIONS.md remains append-only with every complete Q1-Q12 question, answer, rationale and implication, including the earlier resume dispositions. The full Patron answer register is reproduced at the end of this brief. ASSUMPTIONS.md lists all current assumed choices.

## Evidence boundary and precedence

Facts come from named notes recon-DEV-358 and recon-DEV-358-2, at their recorded provenance. The initial 7c7b4079 absence evidence is historical where superseded by c2d78146 recon. Current authoritative ticket text is restored by recon-DEV-358-2:7-19. Historical DEV-331 deferrals do not reduce DEV-358 delivery (Q5).

Verified at c2d78146:
- Existing TmdbMetadataProvider, keyed provider wiring, TmdbOptions.Enabled and existing secret/client/resilience configuration: recon-DEV-358 follow-up section and recon-DEV-358-2:22-36.
- ChainedMetadataProvider has first-success fallback and no field merge: recon-DEV-358-2:62-82.
- DEV-424 maps collection ID/name and persists collections plus nullable movies.collection_id, not a membership join table: recon-DEV-358-2:85-120.
- DEV-356 fixture loader, replay, seed/options, history-derived relevance, MmrSelector cutoff 10, and evaluation publish command: recon-DEV-358-2:38-47,123-149.

External TMDb keyword/credits schemas are **not available in tree** (recon-DEV-358-2:151-168). Conductor explicitly authorized Patron to decide proposed shapes and missing/null handling as [assumed]; Q7 ratifies them. This supersedes the earlier external-recon stop, without turning assumptions into external documentation receipts. No independent external search was performed.

Q1 still requires merged upstream dependencies and their contracts to be revalidated by bounded recon at the eventual build HEAD. A spec directory, landed symbol inventory or status label is not a substitute for that build-entry receipt. No guessed upstream client, fabricated replay data or weakened AC is authorized.

## Ticket delivery preserved

Authoritative source: recon-DEV-358-2:7-19.

1. Enrich MovieFeatures with TMDb keywords, full cast and franchise collection signals when TMDb is enabled.
2. Add keywords as a set to overlap terms; add a same-collection overlap term.
3. Persist keywords and collection values through the DEV-411 relational-association pattern with migration. The literal collection join-table wording is subject only to the Q6 owner checkbox, never silently waived.
4. Compare DEV-356 Hit Rate@10 and MRR with and without signals on the same replay set.
5. With TMDb enabled, persist keywords/collections and include them in MovieFeatures.
6. Hit Rate@10 with signals is no lower than baseline; record both values.
7. Group same-collection franchise movies together in the seed top 10.
8. DEV-331 prerequisite, DEV-356 measurement, DEV-411 persistence pattern and DEV-338 overlap terms remain in the contract chain.

## Frozen scope for review rounds

DEV-358 covers the Q7 assumed keyword/credits ingestion contracts, Q8 enabled same-movie supplemental merge and availability-aware atomic keyword/full-cast persistence with Q6-owner-conditional collection FK reuse, MovieFeatures/SimilarityScorer terms and disabled parity, Q9 indexed seed-franchise candidate union and contiguous top-10 block, Q10 provenance-backed same-DEV-356 paired HR@10/MRR measurement and separate seed-path acceptance, and exactly the Q11 named files/tests/tasks/gates. Scope excludes new dependencies/projects/ports/layers/public API or frontend changes, LocalPlay/secret changes, generic MMR redesign, unrelated rewrites, fabricated replay data, gate/golden weakening and an unapproved collection join-table transition; anything else is a follow-up issue, not a finding in this round.

This review scope is Q12-ratified; it does not establish a frozen plan before Quill drafting, analysis and challenge. Follow-up filing remains subject to the charter; the scope sentence does not authorize indiscriminate issue creation.

## Owner decision — mandatory on eventual spec PR

- [ ] blocked: structural — Should DEV-358 retain the landed DEV-424 collections table and nullable movies.collection_id FK as the sole collection-membership persistence (adding only keyword persistence/join table and migration) instead of the collection join table named in DEV-358 Scope and Technical Design, or transition collection membership to a join table?

Patron Q6 recommends retaining the landed collections table plus movies.collection_id as the single collection-membership source of truth and adding only keyword persistence and the separately approved full-cast provenance join. This is a recommendation pending the user, not FK approval. Basis: role 2.3(a), recon-DEV-358-2:11-12 versus :94-102,104-120,170-172.

**Gate 1 stays closed until the user answers.** A Patron answer never ticks this checkbox. Quill carries it verbatim and unticked into spec.md and the eventual spec PR. The planned path below uses the recommended FK branch conditionally. If the user chooses a collection join-table transition, amend brief/plan and the affected exact file/test/task boundary before build; no automatic alternate design, duplicate membership state or cap extension.

## Local vocabulary and `CONTEXT.md` glossary entries

| Term | Canonical meaning | Avoid |
| --- | --- | --- |
| Keyword | Positive external metadata keyword ID with a trimmed display name; identity and overlap use the ID. | Tag, genre, free-text keyword label without ID |
| Full cast | All valid actor names returned by the extended credits cast payload, without billing cutoff; excludes crew and character/order features. | Top-billed cast, credits with crew, character list |
| Base cast | Existing primary-provider `Actors` names, retained separately for disabled/ablation parity. | Overwritten cast, merged-only cast |
| Franchise collection | Exact positive `belongs_to_collection` ID with display name; never inferred from title or collection-name similarity. | Title-prefix franchise, fuzzy collection match, user playlist |
| Unavailable signal | No authoritative replacement from the provider lookup: preserve the corresponding persisted signal. | Empty signal, cleared signal |
| Known empty | A valid empty payload (or valid no-collection) from the provider: replace/clear that signal. | Unavailable signal, failed lookup |

Per Constitution Principle VIII (`.specify/memory/constitution.md:408`) and `recon-DEV-358-2:174-192` (all six terms confirmed absent from `CONTEXT.md` at `c2d78146`), DEV-358 adds these six canonical entries to `CONTEXT.md` in the same introducing PR (`CONTEXT.md` is included in the Q11 manifest and task package 2). No new ADR is required for this M ticket.

## Ingestion and normalization — Q7

All external shape choices here are [assumed], not verified external schemas:

- After existing movie identity resolution/details, request movie/{resolved_id}/keywords and movie/{resolved_id}/credits through the existing TmdbMetadataProvider HTTP client, base URL, query-key api_key auth, resilience and cancellation path.
- Keyword object: {id, keywords:[{id,name}]}. Credits object: {id, cast:[{id,name,order?,character?}]}. The response movie ID must equal the resolved positive ID.
- Keyword/person IDs must be positive, names nonblank after trimming. Do not truncate cast, apply a billing cutoff, or include crew/character/order as features.
- Deduplicate keywords by positive ID; conflicting labels choose the ordinal-smallest trimmed name. Deduplicate cast with existing trimmed case-insensitive actor-name conventions.
- Present valid [] is authoritative empty. Missing/null arrays, wrong shape, mismatched movie ID or any malformed item makes that entire signal unavailable; do not accept partial cast as complete. Independently valid signals remain usable.
- Preserve the existing positive collection ID/name identity and validation. A valid no-collection is authoritative empty; unavailable collection is not a clear.
- Verify these paths with offline WireMock fixtures. Actual schema incompatibility must surface as unavailability/failure evidence, never a successful signal receipt. No new SDK, port, layer, route, credential or secret logging.

## Supplemental behavior and persistence — Q8

The existing chain needs a deliberate enabled-only supplement path because its current first-success behavior never merges.

1. Preserve the existing primary/fallback outcome matrix when there is no primary Found.
2. After OMDb Found, invoke configured enabled TMDb once using matched IMDb identity where available, otherwise the existing lookup identity policy. Only same-movie signals may merge. Do not repeat the TMDb lookup when fallback already returned TMDb Found.
3. Preserve primary scalar/genre/director/writer/base Actors metadata. Merge only TMDb keywords, full cast and collection signals.
4. Supplemental Failed/NotFound or malformed signal is unavailable: preserve corresponding previous persisted data. Valid empty/no-collection replaces/clears. Caller cancellation and unexpected exceptions still propagate; retain existing rate-limit, health and retry policies. A failed supplemental call is not signal success.
5. Carry per-signal availability separately from known-empty through immutable domain data and the existing metadata/save path. Use functionality-based Core domain records `MovieKeyword` and `MovieSignalSnapshot` (per Constitution Principle VIII, `.specify/memory/constitution.md:402-407`), optional `Keywords`/`FullCast` (`ImmutableHashSet<string>`) snapshots and explicit collection availability, without a new layer or port. Vendor-prefixed `Tmdb*` identifiers are restricted to the Infrastructure adapter (`TmdbMetadataProvider*`) and `TmdbOptions`.
6. Keep base `Actors` and supplemental full cast (`FullCast`) distinct. Deliberate Care-item-3 approval adds `keywords(id,name)`, `movie_keywords` and `movie_tmdb_cast` reusing existing actor identities, never a second actor identity table.
7. Update successful signals atomically in the existing movie save transaction using existing normalized-key/advisory-lock/concurrent-upsert discipline. Repeated saves replace memberships, unavailable/disabled/failed enrichment never erases them, and explicit clear-all metadata clears all signals.
8. Reuse current collection mapping/FK persistence only on the owner-approved Q6 branch; no duplicate collection ingestion or second membership source.
9. Disabled mode makes zero TMDb requests, preserves stored signals, and builds base-cast-only features without keyword/collection terms or grouping.

All candidate/profile/detail rehydration paths must preserve the signal snapshots and availability semantics, without adding public DTO fields.

## Feature model and scoring — Q8

- `MovieFeatures` gets defensive keyword-ID set `Keywords` and nullable positive `CollectionId`. Effective `Cast` is base `Actors` union `FullCast` only while supplemental signals are enabled.
- Extend mapper, defensive snapshots, equality/hash and all rehydration paths consistently; preserve existing no-signal behavior.
- Keyword overlap uses the existing set-overlap and active-term normalization; empty/missing sets follow the existing set rules.
- Collection term is 1 for equal known IDs, 0 for different known IDs, absent when either ID is null.
- Add configurable finite nonnegative SimilarityWeights.Keyword=1.0 and Collection=2.0. Zero removes that term; defaults are [assumed]. Preserve score bounds/symmetry and the existing reason surface. These are signal weights, not harness gate thresholds.

## Seed franchise grouping — Q9

A weight alone does not guarantee grouping. Extend the existing candidate-source contract and implementation, without a new port:

- For enabled signals and a positive seed CollectionId, union index-backed recommendation-eligible local-library members with that exact ID before cutoff, even if omitted by the vector shortlist.
- Preserve existing eligibility/exclusions. Never return the seed, duplicates, unavailable/ineligible items or external-only collection titles; avoid a whole-catalog scan.
- Put at most min(10,requested capacity) same-collection candidates in a contiguous leading block. Rank that block by existing seed SimilarityScorer descending, then stable MovieId. For more than ten choose the highest-scoring ten; for fewer include all.
- Fill remaining slots in unchanged MMR order with selected IDs removed. Keep real item scores/reasons and existing result-size bounds.
- Disabled signals, null seed collection or no eligible matches is an exact output-order no-op.
- Use pure Core SeedCollectionGrouper under the existing Recommendations feature, shared with seed acceptance tests. Do not modify generic MmrSelector or global/profile ranking and do not add a grouping field to the flat API response.

Block placement and within-block ordering/tie-breakers are [assumed] Patron taste rulings.

## Paired evaluation and acceptance — Q10

Use the existing DEV-356 stack. Extend the catalog loader/options to carry optional keyword IDs, TMDb cast and collection identity without losing original base cast/features. Add -TmdbSignals Disabled|Enabled with default Disabled; the script passes child-only LAMUFLIX_EVALUATION_TMDB_SIGNALS to strict EvaluationReplayOptions.Default parsing. Unset means Disabled; explicit options are isolated from environment transport.

Create tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.json by retaining exact original DEV-356 movie IDs, events/timestamps/history, labels, base features and ranking inputs; enrich only signal fields using truthful provenance-backed TMDb associations to those same movies. Record scrubbed source movie IDs/response hashes in baseline.tmdb.sources.json. Do not invent associations or alter the original baseline.json/baseline.expected.json.

Both runs use the SAME pinned enriched fixture, seed 356, clocks, observation order, eligible cohort, cutoff 10, lambda and weights:

```powershell
pwsh -NoProfile -File ./scripts/run-evaluation.ps1 -FixturePath ./tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.json -OutputPath ./artifacts/evaluation/DEV-358/baseline.json -Seed 356 -TmdbSignals Disabled
pwsh -NoProfile -File ./scripts/run-evaluation.ps1 -FixturePath ./tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.json -OutputPath ./artifacts/evaluation/DEV-358/tmdb.json -Seed 356 -TmdbSignals Enabled
```

Disabled removes TMDb keywords/collection and uses base cast; Enabled uses the new features/effective cast. History-derived relevance is unchanged. Ordinary dotnet test does not replace the publish-script receipt.

Acceptance requires:
- Identical eligible observations and identical **nonzero** denominator.
- Disabled reproduces the existing DEV-356 baseline.
- Enabled exercises nonzero provenance-backed signals; two empty/no-op runs are not acceptance.
- Enabled HitCount >= Disabled HitCount at that identical denominator. Do not pass on rounded HR values.
- Record both Hit Rate@10 and MRR, their deltas, denominator/hit counts, fixture/source hashes, seed/mode, lambda/weights and signal coverage in specs/DEV-358/comparison.md. MRR is reported, not an added no-regression gate.

Untruthful/unavailable provenance mapping, empty coverage or HR regression is BLOCKED/FAIL with measurement task incomplete. Do not relabel/cherry-pick history, fabricate associations, weaken goldens/thresholds or substitute synthetic replay data. Synthetic boundary scenarios are separate tests. Replay/CI makes no live TMDb calls; provenance contains no api_key URLs or secrets.

Verify seeded grouping independently through production seed-handler, shared helper and API integration cases. The existing replay has no seed context; do not invent one or claim that profile replay proves seeded grouping. All metric values and provenance availability are future evidence, not results of this grill.

## Planned exact edit set — Q11

These are prospective planned targets, not claims that each new file already exists. Only the EF-generated timestamp is variable. Named tracked-file edits are forced by the ticket ACs and Q7-Q10; no unspecified writes. Any current filename discrepancy discovered by Quill must be recorded in this brief as a bounded path correction before use, never silently expanded.

```text
CONTEXT.md
src/LamuFlix.Core/Domain/{MovieMetadata.cs,MovieKeyword.cs,MovieSignalSnapshot.cs}
src/LamuFlix.Core/Features/Recommendations/{MovieFeatures.cs,MovieFeaturesMapper.cs,SimilarityScorer.cs,GetRecommendationsQueryHandler.cs,SeedCollectionGrouper.cs}
src/LamuFlix.Core/Options/{SimilarityWeights.cs}
src/LamuFlix.Core/Ports/{IRecommendationCandidateSource.cs}
src/LamuFlix.Infrastructure/Adapters/{TmdbMetadataProvider.cs,TmdbMetadataProvider.Mapping.cs,TmdbMetadataProvider.Log.cs,TmdbMetadataProvider.Signals.cs,ChainedMetadataProvider.cs,MetadataProviderServiceCollectionExtensions.cs}
src/LamuFlix.Infrastructure/Persistence/{LamuFlixDbContext.cs,EfMovieRepository.cs,EfMovieCatalog.cs,EfRecommendationCandidateSource.cs,EfRecommendationProfileSource.cs}
src/LamuFlix.Infrastructure/Persistence/Records/{MovieRecord.cs,KeywordRecord.cs}
src/LamuFlix.Infrastructure/Persistence/Configurations/{MovieConfiguration.cs,KeywordConfiguration.cs}
src/LamuFlix.Infrastructure/Persistence/Migrations/{<EF-generated timestamp>_AddTmdbRecommendationSignals.cs,<same timestamp>_AddTmdbRecommendationSignals.Designer.cs,LamuFlixDbContextModelSnapshot.cs}
tests/LamuFlix.UnitTests/Domain/{MovieMetadataSignalTests.cs}
tests/LamuFlix.UnitTests/Adapters/{MetadataProviderChainTests.cs}
tests/LamuFlix.UnitTests/Features/Recommendations/{MovieFeaturesTests.cs,MovieFeaturesPropertyTests.cs,MovieFeaturesArbitraries.cs,MovieFeaturesMapperTests.cs,SimilarityScorerTests.cs,SimilarityScorerPropertyTests.cs,SimilarityScorerInvariantPropertyTests.cs,SeedCollectionGrouperTests.cs,SeedCollectionGrouperPropertyTests.cs,GetRecommendationsQueryHandlerTests.cs,GetRecommendationsQueryHandlerPropertyTests.cs,EvaluationReplayPropertyTests.cs}
tests/LamuFlix.UnitTests/Persistence/{ModelMetadataTests.cs}
tests/LamuFlix.IntegrationTests/{MetadataProviderTmdbSignalTests.cs,MetadataProviderChainCompositionTests.cs,EfMovieRepositoryTmdbSignalTests.cs,EfMovieRepositoryCollectionTests.cs,EfRecommendationCandidateSourceTests.cs,EfRecommendationProfileSourceTests.cs,ApiEndToEndRecommendationTests.cs,MigrationTests.cs,PersistenceCompositionTests.cs}
tests/LamuFlix.Evaluation/Fixtures/{EvaluationFixtureLoader.cs,EvaluationCatalogMovie.cs,baseline.tmdb.json,baseline.tmdb.sources.json}
tests/LamuFlix.Evaluation/Replay/{EvaluationReplayOptions.cs,EvaluationReplay.cs,TmdbSignalReplayTests.cs,ReplayFixtureFactory.cs}
tests/LamuFlix.Evaluation/Entry/{EvaluationEntry.cs}
tests/LamuFlix.Evaluation.IntegrationTests/Entry/{EvaluationCommandTests.cs}
scripts/{run-evaluation.ps1}
specs/DEV-358/{brief.md,CONCLUSIONS.md,ASSUMPTIONS.md,spec.md,plan.md,tasks.md,checklists/requirements.md,DRAFTING_RECEIPT.md,comparison.md}
```

The single AddTmdbRecommendationSignals migration, its Designer and snapshot must be scaffolded through EF tooling; do not hand-edit generated output. Fold research, data-model, quickstart, and internal signal contracts directly into `plan.md` and `spec.md` (following DEV-331/DEV-424 house precedent; standalone `research.md`, `data-model.md`, `quickstart.md`, and `contracts/tmdb-signals.md` files are omitted by deliberate Patron ruling). The Q6 alternate collection join-table branch is not covered until explicitly amended. HTTP DTO/OpenAPI/TypeScript, generic MmrSelector, original baseline/golden, dependencies, harness thresholds, LocalPlay and unrelated files are excluded. WireMock payloads are embedded in the named signal-test files; no unspecified fixture-file family.

## Implementation task order — Q11

Strict sequential package ordering applies across the eight work packages below: later packages MUST NOT start before their predecessor package checkpoint completes; parallelism is permitted only among independent tasks within the same active package.

1. Verify dependencies/contracts at build HEAD through bounded recon and resolve the owner checkbox. Quill can draft before the owner answer, but build entry cannot.
2. Add immutable functionality-named Core domain signal data (`MovieKeyword`, `MovieSignalSnapshot`, `FullCast`), `MovieFeatures`/defaults, the six `CONTEXT.md` glossary entries, and focused defensive/equality/hash/invariant tests.
3. Extend adapter/chain supplementation and offline outcome/identity/malformed/empty/unavailable/cancellation tests (after package 2 completes).
4. Add atomic persistence/EF-scaffolded migration and PostgreSQL roundtrip, replacement, clear, unavailable-preserve, disabled-preserve, concurrency and rollback tests (after package 3 completes).
5. Complete all feature rehydration/scoring and indexed seed-candidate union/grouper; test no-op, deterministic order/ties, capacity, more-than-ten, eligibility and production/API behavior (after package 4 completes).
6. Build truthful paired fixture/provenance and evaluator mode; test disabled baseline, leakage, explicit options/environment isolation, strict CLI values and output receipts (after package 5 completes).
7. Execute both paired publish commands and record the exact Q10 comparison evidence; missing data or regression leaves this task incomplete (after package 6 completes).
8. Run build/test/applicable harness gates and reviews at the Build workspace stage, not in Spec (after package 7 completes).

## Tests and gate expectations — Q11

Use existing xUnit/assertion/mocking conventions, FsCheck for independent invariants, offline WireMock HTTP and real PostgreSQL Testcontainers for translation, serialization, transaction and concurrency behavior. Do not mock the data driver. Use TimeProvider and injected seeded Random; no explanatory comments or threshold changes. Tests may be written alongside production code or red-first per the repo's `$implement` skill (TDD is optional); `SC-005` requires automated test coverage and passing gate evidence across all `FR-014` paths, without a mandatory red-first ("failing-then-passing") execution receipt.

Required coverage includes full/uncapped cast, identity mismatches, every empty/unavailable path, independent partial signal failures, same-movie supplemental merge, base-metadata preservation, no duplicate fallback lookup, disabled parity, cancellation/unexpected exception propagation, atomic rollback, roundtrip/provenance separation, rehydrated feature equality/hash, symmetry/bounds/zero/absent terms, same-name/different-ID collections, union recall/eligibility/dedup, top-10 limits/no-op and unchanged generic MMR/profile behavior.

Future Build-stage checks use harness.yml configuration and the verified diff:
- Harness build and dotnet test.
- scripts/run-roslyn-analyzers.ps1
- scripts/run-cyclomatic-complexity.ps1
- scripts/run-jetbrains-inspectcode.ps1
- scripts/run-property-tests.ps1
- scripts/run-vulnerable-packages.ps1
- Harness-selected format check.
- Pre-PR: pwsh -NoProfile -File ./scripts/run-mutation.ps1, without --since; preserve every configured score/kill-share/file-span threshold and investigate survivors.
- Exact-head reviews and hosted checks under the pipeline.

Exit-2 scope-empty SKIPPED, configured opt-out SKIP and explicit mutation N/A remain distinct from PASS. An unrunnable applicable gate is Could not run and blocks. Do not replace missing gates with a plain build. No web gate or acceptance-framework generation without an applicable frontend diff/explicit opt-in. This Spec grill ran no implementation tests, gate suite, mutation or evaluation.

## Loop discipline and handoff — Q12

Closing bar for plan challenge and build code-review/ship-review: zero unresolved in-scope Critical/High findings, no unfulfilled ticket AC, and no failed/unrunnable applicable mandatory gate, regardless of severity labeling. Medium/Low receive explicit accepted/follow-up/noted dispositions without silent scope expansion.

Require all three axis reports Sentry, Ledger and Compass on this M ticket; missing axis is blocked, never a skip. Disposition every finding each round. Publish/read back reviews and receipts where the pipeline requires.

Hard cap: **2 review/fix rounds for plan challenge**, and separately **2 rounds total across build internal code-review/ship-review**. No third round for publication/encoding/receipt repair. Report any remaining blocker at the cap; do not demote/waive it or claim clearance.

Follow-up filing goes through Rigger, for out-of-scope Critical/High, broken behavior or user requests; check/fold into an existing open ticket first. Otherwise record noted, no ticket in CONCLUSIONS.md. No tracker write by Keel.

Quill may now draft only the named spec artifacts from this brief, preserving the Q6 owner checkbox. Then Keel performs read-only speckit-analyze, checks plan/tasks against this brief and sends one numbered fix list per round; three-axis plan challenge precedes plan freeze. This handoff is not Gate 1, plan freeze, build entry, PR opening or merge.

## Full Patron answer register — Q1-Q12

The following complete answer texts are copied from append-only CONCLUSIONS.md. Their historical park statements remain historical; Q7/Q12 authorize resumed assumed-contract drafting and close the grill. Q6 persistence choice remains unresolved until the user answers.


### Q1

> DEV-358 Q1 ruling: approved — Phase A specifies full DEV-358 scope against upstream contracts (DEV-331, DEV-411, DEV-356), and Phase B build entry remains explicitly blocked until those contracts are merged and verified at build HEAD via bounded recon; no guessed schemas, local TMDb clients, fabricated replay data, or weakened AC may be substituted. Basis: DEV-358:38-49, specs/PRODUCT.md:18,23-24, lamuflix-team-charter:10-12,25-28, recon-DEV-358:19,24,37-42.

### Q2

> DEV-358 Q2 ruling: clarification preserving ticket scope (no owner checkbox required) — DEV-358:42 ("same path as DEV-411 (join tables plus migration)") specifies the relational-association persistence pattern to follow, while DEV-358 itself owns adding its ticket-authorized keyword and franchise-collection join tables and migration; this does not contradict ticket text. Because authoritative DEV-331 adapter and DEV-356 replay/measurement contracts remain NOT FOUND in recon-DEV-358:44-75 and Q1 prohibits guessed contracts or placeholder drafting, park the grill open (no technical freeze or Spec Kit drafting) until bounded authoritative contracts for DEV-331 and DEV-356 are supplied. Basis: DEV-358:41-49, recon-DEV-358:32,44-75, specs/PRODUCT.md:23-24,38,45, lamuflix-team-charter:21,25-28.

### Q3

> DEV-358 Q3 ruling: confirmed — keep the grill PARKED OPEN (NOT FROZEN) with no further technical, contract-independent, or closure questions and no repeated same-root absence recon (`recon-DEV-358:77-85` already exhausts the current worktree at HEAD `7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947`). Exact resume trigger: resume at Q4 only when authoritative DEV-331 adapter and DEV-356 replay/measurement contracts are supplied via a newly authorized evidence boundary (e.g., upstream ticket/spec notes or rebased/updated worktree after upstream specs/merges land). Basis: DEV-358:38-49, recon-DEV-358:44-85, specs/PRODUCT.md:18,23-24, lamuflix-team-charter:25-28,32,92.

### Q4

> [from Patron] DEV-358 Q4 ruling: Ratify reusing landed TmdbMetadataProvider, TmdbOptions.Enabled, existing secret config, and EvaluationReplay.Run with no new SDK/project/layer/port/route; halt concrete Q1/Q3 contract grilling as needs recon until restored authoritative DEV-358 ticket text (Scope and Technical Design + ACs) and exact contract slices are supplied via Conductor.
> - Role 2.3 and 3.1 (Ticket authority): Authoritative DEV-358 Scope and Technical Design and ACs are required to cite ticket lines and judge 2.3(a) contradictions; intake/history notes in DEV-358 cannot substitute for ticket text.
> - Role 2.3 (Care items 1, 2, 4, 5) and recon-DEV-358:86-90,96-100,109-119: Reusing landed TmdbMetadataProvider, keyed IMetadataProvider, TmdbOptions.Enabled, existing secret config, and EvaluationReplay.Run(EvaluationFixture, EvaluationReplayOptions) with no new dependency, layer, port, or public route is ratified.
> - Role 3.1 (Missing evidence routes as needs recon): Symbol inventory recon-DEV-358:80-120 does not establish TMDb signal response identities, full-cast merge/fallback, provider-chain supplemental-data semantics, replay fixture/options/ranking injection contract, or exact comparison command; route these plus restored DEV-358 ticket text as needs recon through Conductor rather than guessing.

### Q5

> [from Patron] DEV-358 Q5 ruling: Ratify that authoritative DEV-358 ticket scope controls over historical DEV-331 deferral notes; DEV-358 retains all collection, keyword, full-cast, persistence, and replay ACs, reusing landed collection mapping/persistence where verified and implementing all missing signals in the existing adapter with no ticket edit, owner checkbox, or new SDK/port/layer/route, while keeping unverified implementation boundaries contingent on bounded recon via Conductor.
> - Role 2.3 and 3.1 (Ticket text is authoritative; old notes are pointers): Restored ticket text in recon-DEV-358-2:7-19 explicitly mandates TMDb keywords, full cast, collection signals, join tables/migration, same-replay HR@10/MRR comparison, no-lower HR@10, and top-10 franchise grouping; prior DEV-331 notes referencing DEV-424 (recon-DEV-358-2:20,26-36) do not override current ticket text or trigger a 2.3(a) owner checkbox.
> - Role 2.3 (Care items 1-4) and recon-DEV-358-2:26-36: Consuming already-landed belongs_to_collection ID/name mapping without duplicating ingestion and implementing missing keyword, credits/full-cast, and collection recommendation signals inside the landed adapter preserves all DEV-358 ACs without introducing a new dependency, layer, port, or public route.
> - Role 3.1 (Proposed/implemented/unknown distinction and needs recon): Because ChainedMetadataProvider body (recon-DEV-358-2:35), ranking signal injection (recon-DEV-358-2:47), and DEV-424 persistence/code state (recon-DEV-358:105-107) remain unverified, exact reuse vs new-code boundaries stay contingent on bounded recon through Conductor rather than assumption.

### Q6

> [from Patron] blocked: structural — Should DEV-358 retain the landed DEV-424 collections table and nullable movies.collection_id FK as the sole collection-membership persistence (adding only keyword persistence/join table and migration) instead of the collection join table named in DEV-358 Scope and Technical Design, or transition collection membership to a join table?
> - Role 2.3(a) (Ticket-design change and Gate 1 consequence) and recon-DEV-358-2:11-12: Because DEV-358 explicitly specifies join tables plus migration for both keywords and collections, omitting a collection join table in favor of movies.collection_id alters explicit Scope and Technical Design rather than clarifying it; this requires an owner checkbox on the spec PR and keeps Gate 1 closed until the user answers.
> - Role 2.3 (Care item 3, schema boundary) and recon-DEV-358-2:94-102,104-120,170-172: Patron recommends retaining landed collections plus movies.collection_id as the single source of truth for collection membership (preserving verified DEV-424 replacement/clear/preserve and concurrency behavior without duplicate state) and adding only keyword tables/join table plus migration, pending the owner checkbox.
> - Role 3.1 (Missing evidence routes as needs recon) and recon-DEV-358-2:151-161: Authoritative external TMDb keyword and credits response schemas remain unverified and must route as needs recon through Conductor rather than being assumed.

### Q7

> [from Patron] DEV-358 Q7 ruling: Ratify the complete TMDb signal-ingestion, identity, deduplication, and missing-value contract in full, logging the external keyword/credits DTO shapes, deduplication tie-breakers, and per-signal fail-closed unavailable semantics tagged [assumed] in specs/DEV-358/ASSUMPTIONS.md while keeping the Q6 owner checkbox unticked.
> - Role 2.3 and 3.1 (Taste/assumed rulings and Conductor instruction DEV-358:82-87; recon-DEV-358-2:7-19,151-168): With external TMDb keyword and credits schemas not available in tree, requesting movie/{resolved_id}/keywords ({id, keywords:[{id,name}]}) and movie/{resolved_id}/credits ({id, cast:[{id,name,order?,character?}]}) with positive IDs, nonblank trimmed names, full-cast inclusion without billing cutoff or crew/character/order feature leakage, keyword deduplication by positive ID (ordinal-smallest trimmed name on conflict), and case-insensitive trimmed cast deduplication is ratified and logged [assumed].
> - Role 2.3 (Sound engineering practice and contract integrity) and recon-DEV-358-2:26-36: Treating present valid [] as authoritative empty while failing closed per signal on missing/null arrays, mismatched movie ID, wrong payload shape, or malformed items (without invalidating unrelated valid signals or silently accepting partial cast data), preserving positive belongs_to_collection ID/name identity, and verifying via offline WireMock fixtures are ratified.
> - Role 2.3 (Care items 1, 2, 4, 5): Reusing the landed TmdbMetadataProvider HTTP client, base URL, api_key, resilience pipeline, and cancellation path introduces no new dependency, port, layer, public route, or credential/secret surface.

### Q8

> [from Patron] DEV-358 Q8 ruling: Ratify the end-to-end ChainedMetadataProvider supplemental merge, atomic persistence (including deliberate Care-item-3 schema approval for keywords, movie_keywords, and movie_tmdb_cast reusing existing actors identities), MovieFeatures rehydration, and similarity-term contract in full, logging default weights SimilarityWeights.Keyword=1.0 and Collection=2.0 as [assumed] while keeping the Q6 owner checkbox open.
> - Role 2.3 and 3.1 (Ticket scope and sound engineering practice; recon-DEV-358-2:7-19,62-82): Invoking enabled TMDb once after OMDb Found (using matched IMDb identity when available, with no duplicate lookup on TMDb fallback Found), preserving OMDb scalar/genre/director/writer/base Actors while merging only same-movie TMDb keyword/full-cast/collection signals, and separating unavailable (preserve prior persisted state) from valid empty/no-collection (replace/clear) and explicit clear-all satisfies DEV-358 signal delivery and honest disabled/ablation parity.
> - Role 2.3 (Care item 3, deliberate schema ruling) and recon-DEV-358-2:11-12,94-120: Patron explicitly approves adding keywords(id,name), movie_keywords, and movie_tmdb_cast (referencing existing actors identities without a second actor table) updated atomically in the existing movie save transaction under normalized-key and advisory-lock discipline, while collection persistence remains gated on the open Q6 owner checkbox.
> - Role 2.3 (Taste/assumed rulings and Care items 1, 2, 4) and recon-DEV-358-2:125-147: Extending MovieFeatures with defensive keyword-ID set Keywords, nullable positive CollectionId, and enabled-only effective Cast (base Actors union TmdbCast) across all rehydration/mapper/equality paths, using existing set-overlap normalization for keywords and 1/0/absent same-collection matching (absent when either CollectionId is null), and setting configurable finite nonnegative defaults SimilarityWeights.Keyword=1.0 and Collection=2.0 (logged [assumed]) are ratified with no new project, port, layer, or public API change.

### Q9

> [from Patron] DEV-358 Q9 ruling: Ratify the deterministic seed-franchise candidate-union and contiguous top-10 grouping contract in full via pure Core helper SeedCollectionGrouper, logging contiguous top-block placement, within-block SimilarityScorer descending order, and stable MovieId tie-breaking tagged [assumed] in specs/DEV-358/ASSUMPTIONS.md while keeping the Q6 owner checkbox unticked.
> - Role 2.3 and 3.1 (Ticket ACs and sound engineering practice; recon-DEV-358-2:18,94-120,144-149): Unioning indexed, recommendation-eligible local-library members sharing the seed positive CollectionId into the existing candidate source before cutoff (excluding the seed, duplicates, unavailable items, and external-only titles) and placing at most min(10, requested capacity) matching items in a contiguous leading block guarantees the top-10 franchise grouping AC even when vector shortlist recall would otherwise omit a franchise member.
> - Role 2.3 (Taste/assumed rulings): Ordering the contiguous same-collection block by existing seed SimilarityScorer score descending then stable MovieId tie-breaker, filling remaining slots in unchanged MMR order with selected IDs removed, and leaving output untouched when TMDb signals are disabled, seed CollectionId is null, or no eligible matches exist are ratified and logged [assumed].
> - Role 2.3 (Care items 2 and 4): Extending the existing candidate-source contract and placing pure helper SeedCollectionGrouper under the existing Core Recommendations feature without modifying generic MmrSelector or global/profile ranking, fabricating scores, or adding public DTO/route fields preserves existing architectural and API boundaries.

### Q10

> [from Patron] DEV-358 Q10 ruling: Ratify the paired evaluation acceptance protocol, provenance-backed baseline.tmdb.json/baseline.tmdb.sources.json fixture contract, -TmdbSignals Disabled|Enabled CLI/options extension, exactHitCount no-regression bar, and separate seed-grouping verification in full, logging the CLI flag name and artifact paths as [assumed] while keeping the Q6 owner checkbox open and claiming no measurement results in advance.
> - Role 2.3 and 3.1 (Ticket ACs and non-negotiable gate integrity; recon-DEV-358-2:7-19,38-47,123-149): Preserving exact DEV-356 movie IDs, observations, timestamps, labels, and seed 356 in baseline.tmdb.json (with scrubbed provenance hashes in baseline.tmdb.sources.json and baseline.expected.json untouched), requiring Disabled to reproduce the DEV-356 baseline and Enabled to exercise nonzero provenance-backed signal coverage with Enabled HitCount >= Disabled HitCount on the identical nonzero denominator, and reporting HR@10 and MRR in artifacts/evaluation/DEV-358/comparison.md satisfies the replay AC without synthetic data or rounded-metric inflation.
> - Role 2.3 (Sound engineering practice and Care items 2, 4, 5): Extending EvaluationReplayOptions (default Disabled) and scripts/run-evaluation.ps1 with -TmdbSignals Disabled|Enabled for the two pinned offline commands, failing closed as BLOCKED/FAIL if provenance mapping is empty or HR@10 regresses, and verifying Q9 seeded top-10 franchise grouping via production seed-handler/SeedCollectionGrouper/API integration tests rather than fabricating a profile-replay seed keeps both ACs honest and offline in CI.
> - Role 2.3 (Scrub list and future evidence discipline): Stripping api_key URLs from baseline.tmdb.sources.json, making zero live TMDb requests during replay/CI, and treating all replay metrics as future implementation evidence (never claimed during spec grilling) are mandatory.

### Q11

> [from Patron] DEV-358 Q11 ruling: Ratify the complete bounded planned edit set, forced edits justified by ticket ACs and Q7-Q10, immutable TmdbKeyword/TmdbSignalSnapshot domain types, child-process LAMUFLIX_EVALUATION_TMDB_SIGNALS transport, 8-step task ordering, and verification gate contract in full, logging the environment-variable transport and embedded WireMock payloads tagged [assumed] in specs/DEV-358/ASSUMPTIONS.md while keeping the Q6 owner checkbox open.
> - Role 2.3 (Care item 6, forced edits and scope boundary) and recon-DEV-358-2:7-19,62-149: Every named tracked file in the planned edit set is forced by the DEV-358 keyword/full-cast/collection ACs and Q7-Q10 rulings; scaffolding the single AddTmdbRecommendationSignals migration family via EF tooling without hand-editing generated files, keeping HTTP DTOs/OpenAPI/TS, MmrSelector, baseline.json, baseline.expected.json, and harness thresholds untouched, and requiring an explicit brief/plan amendment if the user selects the join-table branch on Q6 are ratified.
> - Role 2.3 (Care items 1, 2, 4, 5 and assumed choices): Using immutable domain records TmdbKeyword and TmdbSignalSnapshot with no new port or layer, passing evaluator mode via child-process LAMUFLIX_EVALUATION_TMDB_SIGNALS into strict EvaluationReplayOptions.Default parsing (logged [assumed]), embedding WireMock payloads in the named signal test files (logged [assumed]), and requiring any filename discrepancy found by Quill to be recorded as a bounded path correction in brief.md before use are ratified.
> - Harness Non-negotiables and Verification rules (AGENTS.md): The 8-step task order, xUnit/FsCheck/WireMock/Testcontainers PostgreSQL test plan using TimeProvider and injected seeded Random with no explanatory comments, and full .NET gate verification (including pre-PR pwsh -NoProfile -File ./scripts/run-mutation.ps1 without --since, keeping exit-2 SKIPPED/N/A distinct from PASS, treating Could not run as blocking, and claiming zero gate results during spec grilling) are mandatory.

### Q12

> [from Patron] DEV-358 Q12 ruling: Confirm shared understanding and ratify the complete closing bar, two-round review caps, frozen scope and exclusions, local domain vocabulary, and transition to GRILL CLOSED / BRIEF READY FOR QUILL / OWNER DECISION PENDING (12/12 used) in full, with the Q6 owner checkbox preserved verbatim and unticked and Gate 1 held closed until the user answers.
> - Role 2.3 (Standing closing bar, round cap, and follow-up filing): Zero unresolved in-scope Critical/High findings, zero unfulfilled ticket ACs, and zero failed or unrunnable mandatory gates (with all three Sentry/Ledger/Compass axes required for this M ticket), a hard cap of 2 review/fix rounds for plan challenge and 2 rounds total for build internal review (reporting any remaining blockers at cap without waiver or a 3rd repair round), and filing follow-ups via Rigger only for out-of-scope Critical/High findings, broken behavior, or user requests (folding into existing open tickets first, otherwise noted, no ticket in CONCLUSIONS.md) are ratified.
> - Role 2.3 and 3.1 (Frozen scope and Q6 structural blocker; recon-DEV-358-2:7-19,62-168): DEV-358 scope is frozen to the Q1-Q11 rulings (Q7 [assumed] keyword/credits ingestion per Conductor DEV-358:82-87, Q8 supplemental merge/atomic persistence/similarity terms, Q9 SeedCollectionGrouper top-10 block, Q10 paired evaluation protocol, and Q11 file/test/task/gate manifest), excluding any new dependency, project, port, layer, public DTO/route, frontend, LocalPlay/secret, or generic MMR change, while keeping the Q6 collection FK vs join-table owner checkbox verbatim and unticked on brief.md and the spec PR with Gate 1 closed until user answer.
> - Role 3.1 (Shared-understanding closure and local vocabulary): Settling keyword, full cast, and franchise collection definitions locally in CONCLUSIONS.md and ASSUMPTIONS.md without a global glossary edit or new ADR, preserving append-only CONCLUSIONS.md while consolidating brief.md and ASSUMPTIONS.md with the full Q1-Q12 register and Q11 manifest, and handing off to Quill under status GRILL CLOSED / BRIEF READY FOR QUILL / OWNER DECISION PENDING are ratified.

### Spec Kit Analyze Round 1 Repair Rulings (2026-11-01)

> [from Patron] DEV-358 analyze Round 1 repair rulings: Ratify all five repair decisions (C1 functionality-based Core naming, C2 `CONTEXT.md` glossary additions and manifest task, I1 folding the four auxiliary design docs into `plan.md`/`spec.md`, I2 strict eight-package sequential ordering, and I3 removing the red-first clause from `SC-005`) while keeping the Q6 owner checkbox verbatim and unticked and Gate 1 closed.
> - **C1 (Constitution VIII functionality-based Core naming; `.specify/memory/constitution.md:402-407`, `analyze-DEV-358:14-18`)**: Rename planned Core domain types and members from vendor-prefixed `Tmdb*` to functionality-based identifiers (`MovieKeyword` in `src/LamuFlix.Core/Domain/MovieKeyword.cs`, `MovieSignalSnapshot` in `src/LamuFlix.Core/Domain/MovieSignalSnapshot.cs`, and `ExtendedCast` / `MovieCastEntry` on `MovieMetadata` and `MovieSignalSnapshot`, with unit test file `tests/LamuFlix.UnitTests/Domain/MovieMetadataSignalTests.cs`). Vendor `Tmdb*` names remain strictly in `LamuFlix.Infrastructure.Adapters` (`TmdbMetadataProvider*`) and `TmdbOptions`.
> - **C2 (Constitution VIII `CONTEXT.md` glossary compliance; `.specify/memory/constitution.md:408`, `recon-DEV-358-2:174-192`, `analyze-DEV-358:19-23`)**: Because all six domain terms (`Keyword`, `Full cast`, `Base cast`, `Franchise collection`, `Unavailable signal`, `Known empty`) are confirmed absent from `CONTEXT.md` at `c2d78146`, authorize adding their canonical `term / definition / _Avoid_:` entries to `CONTEXT.md`, add `CONTEXT.md` to the Q11 file manifest, and include the glossary update task in work package 2.
> - **I1/I2/I3 (Deliverable folding, strict package ordering, and `SC-005` test rule; `analyze-DEV-358:24-38`, `.agents/skills/implement/SKILL.md`)**: (I1) Authorize folding `research.md`, `data-model.md`, `quickstart.md`, and `contracts/tmdb-signals.md` into `plan.md` and `spec.md` per DEV-331/DEV-424 house precedent and remove those four standalone paths from the Q11 manifest (including `DRAFTING_RECEIPT.md` instead); (I2) enforce strict sequential ordering across the 8 work packages so later packages depend on prior package checkpoints and parallelism occurs only within a ready package; (I3) remove the "failing-then-passing" red-first clause from `SC-005` while preserving all mandatory automated test coverage and harness gate evidence across `FR-014`.


---

## Plan Challenge Round 1 — Keel adjudication (2026-11-01)

Provenance: F:/Dev/LamuFlix.worktrees/feature-358-spec; branch feature/358-spec; HEAD c2d78146b359bd17ab36ecaa9f33342272d47ebf. All three reports read: findings-DEV-358-Sentry:1-117, findings-DEV-358-Ledger:1-36, findings-DEV-358-Compass:1-41; DEV-358:163-179. Facts remain attributed to those supplied reports and recon, not independent code searches.

**ADJUDICATION COMPLETE / ROUND 1 REPAIRS REQUIRED / OWNER DECISION PENDING.** 23/23 findings dispositioned: 5 High, 10 Medium, 8 Low; 16 accepted repair/clarification findings, 5 accepted residual/taste/limitation findings with documentation or safeguard repairs, 2 noted no-ticket findings. No rejected finding, no tracker write. All five High findings require repair and read-back before closure. Duplicate concerns map to common repairs, not dropped findings.

Patron ratified P1-P12 under Q12 (not Q13, no cap reset), with three confirmed transcription corrections preserved below. Q6 collection FK versus join-table remains the sole owner checkbox, verbatim unticked; Gate 1 closed. No new ticket contradiction or constitution departure is authorized. No PLAN FROZEN, build pickup, gate/metric PASS, implementation or merge is claimed. This is adjudication and a repair instruction, not proof that Quill has repaired the artifacts. Hard two-round plan-challenge cap remains; no third publication/receipt repair round.

### All 23 dispositions

| Finding | Severity | Disposition | Repair | Decision / evidence |
| --- | --- | --- | --- | --- |
| Sentry H1 | High | Accept — repair | R1 | Explicitly supersede DEV-424 clear-on-OMDb-success for unavailable/disabled collection; availability gates CopyMetadata, not nullable ID. Basis: Sentry:21-26; brief Q8; recon-DEV-358-2:119-120. |
| Sentry H2 | High | Accept — repair | R2 | Load every new join before replace; deterministic keyword-ID and full-cast actor locks/upserts; reload-save and concurrent same-keyword proofs. Basis: Sentry:28-33; brief Q8. |
| Sentry M1 | Medium | Accept — repair | R1,R2,R3 | Per-signal save decision table, immutable availability and equality/dirty-tracking proofs prevent empty/unavailable collapse. Basis: Sentry:37-42; Q7-Q8. |
| Sentry M2 | Medium | Accept — repair | R4 | Pin typed supplemental outcome/status/health matrix after OMDb Found; cancellation and unexpected exceptions propagate. Basis: Sentry:44-49; Q8; constitution IV. |
| Sentry M3 | Medium | Accept — repair | R2,R5 | Independently usable keyword/cast/collection signals; mixed valid/empty/unavailable parse and save tests. Basis: Sentry:51-56; Q7. |
| Sentry M4 | Medium | Accepted residual + clarification | R5 | Uncapped full cast retained; representative large-cast offline correctness smoke, no new threshold or SLA. Basis: Sentry:58-63; ticket full cast; Q7. |
| Sentry L1 | Low | Accepted residual + safeguard | R5,R6 | Existing query-key auth retained; avoid raw credential-bearing URIs/log fields and scrub provenance. Basis: Sentry:67-71; Q7; FR-001/FR-010. |
| Sentry L2 | Low | Accepted taste cost | R9 | Leading franchise block can displace other items; intentional Q9 AC cost, separate from profile replay. Basis: Sentry:73-77; Q9-Q10. |
| Sentry L3 | Low | Accepted residual + clarification | R6,R9 | Comparison explicitly excludes seed-grouping proof; T023/SC-003 production/API proof remains mandatory. Basis: Sentry:79-83; Q10. |
| Ledger F1 | High | Accept — repair | R6 | Tracked specs/DEV-358/comparison.md receipt; raw JSON stays ignored artifacts with hashes and reproducible commands. Basis: Ledger:6-8; Q10-Q11. |
| Ledger F2 | High | Accept — repair | R4 | Correct constitution IV row to actual state machine/claim/handler/error rules and supplemental failure behavior. Basis: Ledger:10-12; constitution IV and Enrichment Reliability Rules. |
| Ledger F3 | Medium | Accept — repair | R5 | Move embedded WireMock cases into named IntegrationTests suite; remove UnitTests HTTP suite target. Basis: Ledger:14-16; constitution IX. |
| Ledger F4 | Medium | Accept — repair | R3 | Resolve cast name/type and availability identifiers; no MovieCastEntry or open either-or. Basis: Ledger:18-20; constitution VIII. |
| Ledger F5 | Medium | Accept — repair | R3 | Single collection identity in MovieMetadata; snapshot carries collection availability only. Basis: Ledger:22-24; Q6/Q8. |
| Ledger F6 | Low | Accept — repair | R3,R5 | Specify immutable collections and existing assertion/mock/data-driven test libraries. Basis: Ledger:26-27; Coding Conventions/constitution IX. |
| Ledger F7 | Low | Noted — retain approved names, no ticket | R10 | Retain schema movie_tmdb_cast and source-specific TmdbSignalReplayTests outside Core; record provenance rationale. Basis: Ledger:29-30; Q8 Care-item-3; Q11. |
| Compass F1 | High | Accept — repair | R7 | Add EvaluationEntry.cs bounded manifest correction and pin strict child mode parsing boundary. Basis: Compass:5-7; Q10-Q11. |
| Compass F2 | Medium | Accept — repair | R8 | Nonzero keyword/collection weights transported explicitly, stable across paired modes; Enabled proves both terms exercised. Basis: Compass:9-11; Q8-Q10. |
| Compass F3 | Medium | Accept — repair | R4 | Resolve identity then fetch details, keywords, credits in one logical provider invocation; collection comes from details. Basis: Compass:13-15; recon-DEV-358-2:62-82; Q7-Q8. |
| Compass F4 | Medium | Accepted incremental limitation | R9 | No automatic backfill; previously enriched titles gain signals only through supported enrichment; document limitation. Basis: Compass:17-19; ticket/recon-DEV-358-2:7-19. |
| Compass F5 | Low | Accept — repair (duplicate Ledger F4) | R3 | Choose immutable normalized string set; remove MovieCastEntry alternative. Basis: Compass:21-23; Q8/Q11. |
| Compass F6 | Low | Accept — clarify eligibility | R9 | Otherwise eligible local union members need not have embeddings; preserve eligibility and existing missing-plot behavior. Basis: Compass:25-27; Q9. |
| Compass F7 | Low | Noted — no ticket | R10 | Record README exclusion and narrative currency residual explicitly; no unrelated README rewrite. Basis: Compass:29-31; Q11-Q12. |

### One numbered repair list for Quill — R1-R10

1. **R1** — Save semantics and DEV-424 supersession (P1; Sentry H1/M1). Amend spec US2/FR-005/FR-006, plan data model/internal contract/Risks and T015/T016: for each keyword/full-cast/collection signal independently, Available replaces with valid values, KnownEmpty clears, Unavailable or Disabled performs no signal write; explicit metadata clear-all clears all. Nullable CollectionId alone never clears. Explicitly supersede DEV-424 clear-on-OMDb-success only for unavailable/disabled collection, preserving known-no-collection clears. Amend the existing EfMovieRepositoryCollectionTests clear-on-OMDb proof; add equality/dirty-tracking and mixed-state save proofs.

2. **R2** — Load/lock/atomicity contract (P2; Sentry H2/M1/M3). In plan approach/data model/Risks and T013/T015/T016 require tracked loading of keyword joins, full-cast joins and related identities before replacement, no accidental writes to unavailable associations, deterministic locks for keyword IDs in distinct stable namespace and both base/full-cast actor natural keys using existing upsert discipline. Test reload-then-save/no wipe, concurrent same-keyword insert, mixed independent signal states and rollback of the entire save on failure.

3. **R3** — Domain representation and glossary (P7; Ledger F4/F5/F6; Compass F5; Sentry M1). Across current spec/plan/tasks/assumptions/CONTEXT glossary use FullCast (amending earlier C1 ExtendedCast), ImmutableHashSet<string> normalized cast, ImmutableArray<MovieKeyword> keyword snapshots and ImmutableHashSet<int> feature keywords. Remove MovieCastEntry and every live either-or. Define nested availability enum Unavailable/KnownEmpty/Available in MovieSignalSnapshot.cs; no new file/type family. Snapshot has collection availability only; sole collection identity/validation stays MovieMetadata.CollectionId/CollectionName. Bind T003-T008 and T015/T016 to defensive/equality/dirty-tracking tests. Preserve historical Q1-Q12 answer texts.

4. **R4** — Supplemental sequence/outcome matrix and constitution (P3/P10; Sentry M2; Ledger F2; Compass F3). In US1/FR-001/FR-003, plan approach/internal contract/Constitution IV and T009/T010/T012: one logical TMDb invocation resolves IMDb identity or existing title/year policy, fetches details for collection then keywords/credits; never duplicate a TMDb fallback Found lookup. After primary OMDb Found, supplemental NotFound or typed Failed RateLimited/ProviderUnavailable/InvalidResponse keeps overall Found/MarkEnriched with unavailable affected signals and existing adapter retry/health accounting; no raw exceptions stored/logged. Cancellation/unexpected exceptions propagate. Preserve no-primary-Found matrix. Correct IV to actual enrichment state machine, atomic claim before provider calls, Core handler decisions/thin consumer and cancellation rules. Tests cover every listed outcome and request sequence.

5. **R5** — Test placement, independent parsing and accepted cast bound (P4/P6/P7/P12; Sentry M3/M4/L1; Ledger F3/F6). Remove UnitTests/Adapters/TmdbMetadataProviderSignalTests.cs target; merge embedded offline WireMock cases into IntegrationTests/MetadataProviderTmdbSignalTests.cs in T011/T012, retaining pure chain unit tests without duplicate HTTP suite. Add valid-keywords/malformed-credits and inverse cases, independent collection and mixed known-empty/unavailable cases; representative large-cast correctness smoke without cutoff, cardinality limit, performance gate or invented SLA. Name Shouldly/NSubstitute/AutoFixture and Theory/MemberData conventions. Preserve uncapped full-cast personal-library residual and zero raw api_key URI logging.

6. **R6** — Reviewable acceptance receipt (P5/P12; Ledger F1; Sentry L1/L3). In FR-010/SC-002/US4, plan structure/approach/quickstart, T028 and Checkpoint 7, use tracked specs/DEV-358/comparison.md for comparison; keep baseline.json/tmdb.json under ignored artifacts/evaluation/DEV-358, never force-add. Receipt includes exact commands, raw/fixture/source SHA-256, seed/mode/cohort/denominator/hit counts/HR@10/MRR/deltas/lambda/weights/independent coverage and scrubbed provenance. State profile replay does not prove seed grouping; T023/SC-003 remains mandatory. Preserve Q10 BLOCKED/FAIL lock on Package 8. Receipt is future Phase B output, not created with invented metrics now.

7. **R7** — Evaluation transport boundary (P8 plus confirmed receipt correction; Compass F1). Add tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs to plan structure and T024/T026. Entry reads child-only LAMUFLIX_EVALUATION_TMDB_SIGNALS strictly, unset means Disabled, rejects invalid values and passes parsed value to pure EvaluationReplayOptions.Default. Explicit options are environment-independent. Amend current Q10/Q11 transport wording and T027 isolation/CLI tests; no EvaluationRunner type.

8. **R8** — Evaluation weight and coverage contract (P9; Compass F2). Enriched baseline.tmdb.json explicitly adds ranking.similarityWeights.keyword=1 and collection=2, retaining all original ranking fields and original baseline/golden unchanged; missing added fields in old fixtures default to record defaults. Both paired modes use identical positive weights; Disabled removes signals and reproduces baseline. T024/T025/T027/T028 require independently nonzero exercised keyword and collection terms with nonzero weights; empty/no-op or cast-only coverage does not prove signal measurement.

9. **R9** — Incremental availability and seed eligibility (P11/P12; Compass F4/F6; Sentry L2/L3). Document accepted incremental enrichment, no automatic backfill/sweep and no follow-up ticket. FR-009/plan step 5/T020/T023 explicitly allow otherwise eligible local union members without persisted embeddings, rehydrating base and persisted signal data; use existing missing-plot fallback with no embedding writer/global-profile change and preserve all other eligibility/exclusions. T021 explicitly keeps grouper pure, no fourth injected port. Retain leading-block taste cost, same capacity/ties/no-op rules, production/API proof separate from profile replay.

10. **R10** — Disposition propagation and round receipt (P12; Ledger F7; Compass F7). Retain approved movie_tmdb_cast and TmdbSignalReplayTests names as source/provenance concepts outside provider-neutral Core; record rationale. README/CHANGELOG remain excluded, narrative-currency residual noted, no ticket; no constitution departure/new owner escalation. Update requirements checklist and DRAFTING_RECEIPT with R1-R10 mapping, exact manifest deltas, and Q6 verbatim unticked. Do not claim repair verification, PLAN FROZEN, Gate 1/build pickup or gate/metric PASS. Round 1 repairs followed by read-back; retain hard two-round plan cap.

### Verification and ownership

Keel changes the controlling decision artifacts only. Quill owns derived spec.md/plan.md/tasks.md/checklist/DRAFTING_RECEIPT repairs, including glossary alignment in CONTEXT.md. Preserve existing unrelated edits and the full append-only Q1-Q12 record. Read back every R item before claiming it closed; no unconditional freeze follows this adjudication.

### Controlling decision precedence

Patron P1-P12 and the confirmed correction amend the current Q8/Q10/Q11 design as specified by R1-R10. Full received ruling and correction are preserved in CONCLUSIONS.md. Earlier Q1-Q12 quoted answers remain historical and verbatim. Current body amendments apply prospectively; no new source existence or gate result is asserted.

---

## Plan Challenge Closure Verification — Keel (2026-11-01)

**PLAN FROZEN / OWNER DECISION PENDING.** Provenance: F:/Dev/LamuFlix.worktrees/feature-358-spec; branch feature/358-spec; HEAD c2d78146b359bd17ab36ecaa9f33342272d47ebf. This is plan-document closure only. Gate 1 remains closed pending the user Q6 answer; no implementation pickup, completed ticket AC, production test/gate/metric PASS, commit, tracker write, or merge is claimed.

All 23 source findings are closed at the plan level under their recorded dispositions: Sentry 9, Ledger 7, Compass 7 (5 High, 10 Medium, 8 Low). All five High repairs verified. Accepted risks, taste costs, incremental limitations and noted no-ticket items remain documented; closure does not erase them. The complete 23-row disposition register above remains authoritative.

| Repair | Read-back evidence | Closure |
| --- | --- | --- |
| R1 | spec.md US2/FR-005/FR-006; plan.md:80; tasks.md:64-65 | Independent Available replace / KnownEmpty clear / Unavailable or Disabled no-write, explicit clear-all, nullable-ID guard and DEV-424 supersession; amended collection proof required. |
| R2 | plan.md:50,81; tasks.md T013/T015/T016 | Tracked joins/identities; deterministic keyword namespace and base/full-cast actor locks; reload-save/no-wipe, concurrent insertion, mixed states and atomic rollback proofs. |
| R3 | spec.md FR-004/FR-007; plan.md:70-82; tasks.md T003-T008; ASSUMPTIONS.md:78; CONTEXT.md glossary | FullCast immutable normalized strings, immutable keywords, nested three-state availability, sole MovieMetadata collection identity; no live MovieCastEntry alternative. Historical answers preserved. |
| R4 | spec.md US1/FR-001/FR-003; plan.md:34,49; tasks.md T009/T010/T012 | Identity/details/keywords/credits sequence; collection from details; typed supplemental failure preserves Found/MarkEnriched and health accounting; exceptions/cancellation propagate; actual constitution IV obligations encoded. |
| R5 | plan.md Test Strategy; tasks.md:48 and T012; spec.md FR-014 | Offline WireMock suite in IntegrationTests; independent parse/mixed states, uncapped large-cast correctness smoke, credential-URI safeguards and existing named test libraries. |
| R6 | spec.md US4/FR-010/SC-002; plan.md:56 and Quickstart; tasks.md:110,112 | Tracked comparison.md, ignored raw JSON, complete reproducible hash/metric/coverage receipt; no fabricated metrics; Package 8 locked on BLOCKED/FAIL/incomplete; seed proof separate. |
| R7 | plan.md Approach 6/Structure Decision; tasks.md T024/T026/T027; DRAFTING_RECEIPT.md Round 2 | Existing EvaluationEntry.cs extended; strict child-mode reader, unset Disabled, invalid rejected; pure EvaluationReplayOptions.Default and environment-independent explicit options. |
| R8 | spec.md US4/FR-010/SC-002; tasks.md:98,100,112 | Explicit positive keyword=1/collection=2 fixture weights, old-field defaults, same weights across modes, Disabled baseline reproduction and independently exercised nonzero keyword/collection coverage. |
| R9 | spec.md FR-009/FR-015; plan.md:51 and Risks; tasks.md T017/T019-T023 | Incremental/no-backfill limitation, embedding-less eligible local union, existing missing-plot fallback, pure grouper/no fourth port, leading-block cost and production/API grouping proof. |
| R10 | CONCLUSIONS.md P12 + confirmed correction; checklist:62-71; DRAFTING_RECEIPT.md repair/finding maps | Approved provenance names retained, README residual/no-ticket dispositions explicit, exact manifest deltas propagated, round cap preserved. |

Round 1 repaired R1-R10. Closure read-back identified one residual R7 wording error: existing EvaluationEntry.cs was called new/prospective. One numbered Round 2 correction was sent to Quill; repaired plan/tasks/receipt were independently read back. Round 2/2 closes here; no third repair/publication round is authorized.

Q6 was compared case-sensitively across brief.md, CONCLUSIONS.md, ASSUMPTIONS.md, spec.md and DRAFTING_RECEIPT.md: six copies across five artifacts, all exactly identical and unticked. Carry the same checkbox onto the spec PR. Patron recommendation is not owner approval; a join-table answer requires the recorded boundary amendment before build.

Earlier repair-pending and not-frozen statements are historical drafting receipts, superseded only by this closure for plan status. The Q6 blocker, build-HEAD recon and all Phase B proof/gate obligations remain active.
