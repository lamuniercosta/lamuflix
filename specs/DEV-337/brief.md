# DEV-337 technical brief

## Identity and status

- Ticket: DEV-337 — Implement GenreWeights calculator using library-wide inverse document frequency (IDF), parent DEV-288, governing size:M, no UI work.
- Worktree: `F:/Dev/LamuFlix.worktrees/feature-337-spec`; branch: `feature/337-spec`.
- Recon base: `a462c0475f3970ccfda1df0e05cc47498c99f131`, the recorded merge-base with origin/main.
- Grill closed with Patron at Q7/12 on 2026-10-20. Full question/recommendation/ruling exchanges are append-only in `CONCLUSIONS.md`.
- This is the decision source for Quill to draft spec.md, plan.md and tasks.md. Grill completion is not plan freeze, Gate 1, implementation authorization or gate success.
- Phase B build intake remains gated on Epic 7 completion under `specs/PRODUCT.md:17`. No claim is made here that Epic 7 is complete.

## Authoritative evidence and ticket scope

Facts come exclusively from shared `DEV-337`, `recon-DEV-337` sections 1–8, and its continuation `recon-DEV-337-2` section 9. Section 9.1 contains the verbatim ticket; sections 9.2–9.5 contain product/constitution/glossary/file/test evidence. Patron ruling citations retain their original note line numbers, which may shift when notes are partitioned; the section and ticket item identify the source.

The ticket requires natural-log IDF `ln((TotalMovies + 1)/(MoviesWithGenre + 1)) + 1`, cached in memory and invalidated on catalog changes; a new existing-catalog method returning per-genre counts via one grouped query; invalidation after movie add/remove or genre-membership changes; and weighted Jaccard genre overlap. Acceptance is higher rare-genre contribution, a safe empty-library result with weight 1, and updated cached values after membership changes.

DEV-411 is already merged and persists genres (recon section 7). The historical zero-count precondition is satisfied: deliver real persisted counts, not placeholder zeros. Patron ruled `noted, no ticket` for that historical text; no YouTrack edit/comment or owner checkbox is required (Q1). Existing GetGenresAsync already has facet counts, but must not replace the ticket-required grouped counts contract (recon sections 3.3–3.4).

## Canonical vocabulary

The following definitions were approved in Q3 and appended to root `CONTEXT.md`:

- **Genre weight:** the library rarity multiplier for a genre; rarer genres carry more weight in genre similarity.
- **GenreWeights:** an immutable table of genre weights for one library catalog snapshot.
- **Genre similarity:** the overlap of two movies genre sets, weighted by library rarity.

Use existing Movie, Library, Recommendation and Taste Profile vocabulary. These weights are neither user preference weights nor `RecommendationOptions.Weights.Genres`. Keep generic cache and mathematical implementation details in feature documents, not the domain glossary. No taste decisions were made; no ASSUMPTIONS.md is needed.

## Settled approach and contracts

### Q1 — sequencing and historical dependency

Proceed with Phase A specification while retaining the Epic 7 prerequisite for Phase B. DEV-411 is a satisfied precondition. No scope change, constitution departure or owner checkbox was identified.

### Q2 — counts contract

Extend `IMovieCatalog` with `Task<GenreCounts> GetGenreCountsAsync(CancellationToken ct)`. Add `sealed record GenreCounts(long TotalMovies, IReadOnlyDictionary<int, long> MoviesWithGenre)` in Core/Ports, alongside existing catalog contracts.

`TotalMovies` includes all library movies, including those with no genres. The map includes all persisted genre IDs, including genres with zero movies, and counts distinct movie membership. No genre rows means an empty map. Zero movies means total zero while any persisted zero-count genre IDs remain in the map (Q3 clarification supersedes ambiguous empty-map wording in Q2).

`EfMovieCatalog` executes **one grouped SQL query/one database command** over existing tables, returning a consistent total and genre-count snapshot. An empty library and movies without genres must not erase or miscount the total. Do not implement separate total and per-genre round trips, derive total from summed genre counts, or alter existing facet HTTP behavior. Query translation and command count require real PostgreSQL proof. No migration/schema change.

### Q3 — immutable math and weighted overlap

`GenreWeights.Calculate(GenreCounts)` builds a sealed immutable weight table. For a genre with count df in library size N, compute `Math.Log((N + 1.0)/(df + 1.0)) + 1.0` using double arithmetic before division/addition to avoid integer division/overflow. Reject negative N, negative df and df above N. Empty-library and unknown-genre weights default to `1.0` through `GetWeight(int)`.

`Similarity(IEnumerable<int>, IEnumerable<int>)` treats inputs as sets, deduplicating genre IDs. Return the sum of weights in the intersection divided by the sum in the union. Empty union and disjoint sets return 0; identical nonempty sets return 1. Scores must be finite, symmetric, invariant under input order/duplicates and within [0,1]. Do not multiply by the configurable overall genre coefficient. Defensively preserve immutability so later input-map mutation cannot mutate a published table.

Rare/common acceptance must compare controlled examples with a common union context: a shared rare genre should contribute more than a shared common genre. Do not claim that every arbitrary movie-pair score rises merely because one genre is rare.

### Q4 — cache ownership and lifecycle

- `Core/Domain/GenreWeights.cs`: immutable table and pure calculations above.
- `Core/Domain/GenreWeightsCache.cs`: concrete singleton cache using BCL synchronization, immutable weights and a generation counter only.
- `Core/Features/Recommendations/GenreWeightsCalculator.cs`: scoped concrete calculator, `Task<GenreWeights> GetAsync(CancellationToken)`, depending on scoped `IMovieCatalog` and the singleton cache.

Rebuild lazily on first read after invalidation. Serialize loads and recheck the generation atomically with publication. If invalidation occurs during a query, discard the old result and retry; it must not become the current cached table. Errors/cancellation never publish incomplete state or strand the synchronization gate. Cache hits reuse the immutable snapshot. Never capture a scoped catalog or DbContext in the singleton. Already-returned tables remain snapshots.

No new cache port/interface, package, architectural layer, TTL, cache option or background refresh. The shared Domain cache avoids forbidden feature-to-feature references and Infrastructure references to feature internals. No ADR is needed: the existing catalog port is extended, not the closed port set.

### Q5 — persistence invalidation and DI

Inject the shared cache into `EfMovieRepository`. Invalidate immediately after successful `SaveChangesAsync` persistence (covering import and genre replacement) and after `TryDeleteAsync` only when `rows == 1`, before returning. Do not invalidate pre-save `AddAsync`, failed/cancelled saves or false deletes. No command-handler edits.

Conservatively invalidate on every successful repository save, including scalar saves, rather than add fragile genre-diff state tracking. This can cause extra reloads; that cost is accepted. A reader beginning after a completed catalog write must rebuild fresh. Scope covers one process and current repository write paths; direct SQL and distributed invalidation are excluded.

Q5 refines Q4 registration: use `TryAddSingleton<GenreWeightsCache>()` in **both** `PersistenceServiceCollectionExtensions.AddPersistence` and `MovieCatalogServiceCollectionExtensions.AddMovieCatalog`; register the scoped calculator with AddMovieCatalog. Both registration orders must yield one shared cache. Each extension can be used alone for its own services.

An optional `GenreWeightsCache? cache = null` constructor parameter with an instance fallback (`cache ?? new GenreWeightsCache()`) is allowed for directly constructed repositories; DI always supplies its shared singleton. Never introduce a mutable static fallback.

## Approved file set

All paths are relative to the assigned worktree. This is the planned Build scope; this Phase A ask writes only conclusions, brief and the approved glossary additions.

| File | Purpose |
| --- | --- |
| `src/LamuFlix.Core/Ports/IMovieCatalog.cs` | Add counts member |
| `src/LamuFlix.Core/Ports/GenreCounts.cs` | Counts snapshot record |
| `src/LamuFlix.Core/Domain/GenreWeights.cs` | Immutable IDF table and weighted Jaccard |
| `src/LamuFlix.Core/Domain/GenreWeightsCache.cs` | Shared generation-aware cache |
| `src/LamuFlix.Core/Features/Recommendations/GenreWeightsCalculator.cs` | Scoped catalog-to-cache load orchestration |
| `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` | Single grouped query |
| `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` | Post-persistence invalidation |
| `src/LamuFlix.Infrastructure/Persistence/MovieCatalogServiceCollectionExtensions.cs` | Cache/calculator registration |
| `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` | Repository-host cache registration |
| `CONTEXT.md` | Three approved domain terms |
| `tests/LamuFlix.UnitTests/Domain/GenreWeightsTests.cs` | Numerical and property proof |
| `tests/LamuFlix.UnitTests/Domain/GenreWeightsCacheTests.cs` | Cache state/concurrency proof |
| `tests/LamuFlix.UnitTests/Features/Recommendations/GenreWeightsCalculatorTests.cs` | Catalog orchestration/reuse/cancellation proof |
| `tests/LamuFlix.IntegrationTests/GenreWeightsIntegrationTests.cs` | Persisted changes and refresh proof |
| `tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs` | Extend grouped counts/empty/no-genre proof |
| `tests/LamuFlix.IntegrationTests/MovieCatalogRegistrationTests.cs` | Lifetimes/extension ordering |
| `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` | Compiler-forced RecordingMovieCatalog member adaptation only |

No deletion or unrelated rewrite is authorized. Repository invalidation edits are forced by ticket Scope item 3 and AC3; the port double adaptation is compiler-forced; glossary edits are constitution-required. Patron deliberately approved dependency/placement choices and this exact scope under charter §2.3 in Q2–Q7.

## Q6 — task ordering and proof requirements

1. **Counts contract and adapter:** define GenreCounts and port member, adapt RecordingMovieCatalog, implement the grouped query. Test no movies/no genres; zero-count persisted genres; movies without genres; shared/multiple memberships counted distinctly; total not multiplied by joins; one SQL command and coherent snapshot. Use real Postgres/Testcontainers and command instrumentation.
2. **Pure math:** implement immutable GenreWeights. Unit/property proof covers formula tolerance, rare/common controlled comparison, invalid counts, defaults, empty/identical/disjoint sets, duplicate/permutation invariance, symmetry/bounds and immutable snapshots.
3. **Cache and DI:** implement shared state/generation and scoped loading. Use TaskCompletionSource-controlled tests rather than sleeps for single-load reuse across scopes, invalidation during load and discarded stale result/retry, atomic invalidation/publication, cancellation/error recovery, and synchronization release. Test each registration extension alone and both orders; repository and calculator share one cache under DI; singleton captures no scoped dependencies.
4. **Post-persistence integration:** wire repository invalidation. Warm the cache, import/save movies, change genre memberships and remove movies; subsequent reads must rebuild using changed counts/weights. Failed/cancelled saves and false deletes retain prior cache. Cover the ordinary successful path as well as races/failures.

Use established xUnit v3, Shouldly, NSubstitute for unit collaborators, FsCheck for properties, and the existing real PostgreSQL Testcontainers fixture for persistence. No driver mocking, EF InMemory, second assertion/mocking library or new test dependency. No Gherkin/Reqnroll opt-in.

Build tasks depend on the preceding contracts; persistence invalidation requires the shared cache and DI registration. No MovieFeatures type exists at recon HEAD and this ticket must not wait for or implement it. The component operates on genre-ID sets; future ranking/candidate wiring is separate.

## Gate expectations and stage boundaries

This Spec workspace runs no build/test/analyzer/mutation gates. Quill drafts from this brief; Keel then runs read-only speckit analysis, followed by all three plan challenge axes, plan freeze, Patron provisional Gate 1 and the user-owned spec PR merge per task-pipeline. No later stage is claimed complete here.

Phase B starts in the Build workspace only after its prerequisites and fresh drift recon. Gauge runs the applicable configured gates there: build/tests including integration and architecture tests; Roslyn, cyclomatic and InspectCode; property tests; vulnerable-package/security check; format verification; and the configured mutation gate. Read thresholds from harness.yml; never lower or hardcode them. Use scripts/run-mutation.ps1 without Stryker since in worktrees; surviving mutants require tests. No separate architect/Gherkin stage.

Report gate native exits accurately: PASS only for an applicable successful run; scope-empty exit 2 as SKIPPED (scope-empty), disabled gate as SKIP, mutation exclusions as N/A, and an unrunnable gate as Could not run. None is a fabricated pass. Full build gate success is not merge authorization.

## Q7 — loop discipline and frozen scope

**Closing bar:** Evidence-backed Critical, High and Medium findings within frozen scope block. Low is recorded nonblocking. All three axes Sentry (Risk), Ledger (Standards) and Compass (Spec) are mandatory for size:M; a missing axis blocks adjudication. Unnecessary abstractions and concerns outside the frozen scope are follow-ups, not fix commits in this round. Apply charter follow-up recording: file only when its criteria justify a ticket; otherwise record `noted, no ticket`.

**Frozen scope:** Real grouped library counts via the existing catalog; immutable IDF GenreWeights and weighted Jaccard; process-wide generation-safe lazy cache; repository post-persistence invalidation; DI and required test-double compatibility; approved tests and glossary. anything else is a follow-up issue, not a finding in this round.

**Round cap:** At most two review rounds and two fix commits per round. Unresolved blockers at the cap go to user review on the PR; no third round or waived gate. Findings and summary must be posted on the PR in the applicable review stage; chat alone is not completion.

**Exclusions:** Recommendation routes, ranking, MMR, candidate-source port, MovieFeatures integration, option tuning, UI/OpenAPI/generated clients, new port/package/project/layer/schema, direct-SQL or distributed invalidation, TTL/background refresh, handler/playback edits and unrelated cleanup. No secrets, Features:LocalPlay or Process.Start changes.

## Handoff to Quill

Q1–Q7 are settled and recorded above and in CONCLUSIONS.md. Use the canonical glossary terms and preserve all constraints, approved file scope, ordering, proof requirements, exclusions and review caps. No new ADR was required. No unresolved owner structural checkbox was identified by Patron; this does not close Gate 1. Missing facts require `needs recon`; a decision gap requires `needs decision` to Keel, not a drafting assumption. Quill owns spec/plan/tasks drafting; Keel has not drafted those artifacts.

## Phase 2 step 4 round 1 - adjudicated clarifications

Patron accepted Sentry M1 and Ledger F3 in CONCLUSIONS.md, section Phase 2 Step 4, recorded by commit 7f9d86227b0381dcb56211355cb9356a7be304b3. These refine Q4-Q6 within the existing frozen scope:

- `GenreWeightsCache` exposes `Task<GenreWeights> GetOrLoadAsync(Func<CancellationToken, Task<GenreWeights>> load, CancellationToken ct)` and `void Invalidate()`.
- Invalidate completes independently of any in-flight database load and never waits on the async load gate. Brief synchronous publication synchronization is permitted; no lock-free algorithm is mandated. Generation increment and publication recheck remain atomic.
- Only the current gate holder invokes its own scoped loader. Gate wait, load and generation-mismatch retries use that caller token. Cancellation releases the gate without cancelling independent waiters. Never retain the loader or scoped catalog/DbContext after the call completes.
- TCS-controlled tests prove invalidation completes before a held load is released, a waiter survives holder cancellation and succeeds, retry cancellation releases the gate, and success/error/cancellation all release synchronization.

Factual correction to Q5: the existing persistence extension is `PersistenceServiceCollectionExtensions.AddLamuFlixPersistence`, not `AddPersistence` (recon-DEV-337-3 section 10.1, Program.cs:27 and PersistenceServiceCollectionExtensions.cs:39). References to AddPersistence above mean that existing method. No production rename, new extension, or registration-behavior change is authorized. Original grill wording in CONCLUSIONS.md remains historical; this correction is append-only.

Quill must also correct T003 ordering to follow T005 in the same phase commit, and explicitly add warm-cache successful scalar-only save/reload proof to T016. Public successful SaveChangesAsync completion is the invalidation boundary for all internal branches; failed/cancelled saves do not reach it. These enforce existing Q5/Q6 rather than expand scope. No production files or tests are written during Phase A. Five Medium blockers remain pending Quill remediation; this addendum does not freeze the plan or establish Gate 1. The two-review-round and two-fix-commit-per-round limits remain unchanged.
