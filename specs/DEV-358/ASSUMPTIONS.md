# DEV-358 assumptions and explicit rulings

**GRILL CLOSED / BRIEF READY FOR QUILL / OWNER DECISION PENDING — 12/12 used.**

Worktree: F:/Dev/LamuFlix.worktrees/feature-358-spec; branch feature/358-spec; verified HEAD c2d78146b359bd17ab36ecaa9f33342272d47ebf. Controlling decisions: complete Q1-Q12 exchanges in CONCLUSIONS.md; full answer register and Q11 file manifest in brief.md. This consolidation supersedes historical open/parked assumptions summaries, without altering append-only conclusions.

## Evidence status

External TMDb keyword/credits schemas are **not available in tree**, per recon-DEV-358-2:151-168. Conductor explicitly authorized Patron to settle them under [assumed] authority (DEV-358:82-87; Q7/Q12). They are proposed design contracts, never verified external documentation. Earlier external-recon stops are superseded by that instruction; no independent external search occurred.

Restored ticket text: recon-DEV-358-2:7-19. Verified first-success/no-merge chain, DEV-424 FK persistence and evaluation construction: recon-DEV-358-2:62-149. Older absence/not-verified claims are historical.

## Assumed external contracts and normalization — Q7

- [assumed] Request movie/{resolved_id}/keywords with {id,keywords:[{id,name}]} and movie/{resolved_id}/credits with {id,cast:[{id,name,order?,character?}]}; response movie ID equals resolved positive ID. Reuse existing HTTP client/base URL/query-key api_key/resilience/cancellation.
- [assumed] Positive keyword/person IDs and nonblank trimmed names required. Include all valid returned cast without billing cutoff; exclude crew/character/order as features.
- [assumed] Keyword identity is positive ID; duplicate-ID label conflicts keep ordinal-smallest trimmed name. Cast names deduplicate with existing trimmed case-insensitive actor conventions.
- [assumed] Present valid [] is authoritative empty. Missing/null arrays, wrong shape, mismatched movie ID or any malformed item makes the entire signal unavailable independently of other valid signals; never silently accept partial cast as complete.
- [assumed] Existing positive belongs_to_collection ID/name identity remains authoritative; no title/name fuzzy inference. Known no-collection clears; unavailable preserves.

Basis: Patron Q7; Conductor instruction; recon-DEV-358-2:7-19,26-36,151-168. Offline WireMock fixtures verify the assumed paths; incompatibility must surface, never be counted as successful enrichment.

## Weight defaults — Q8

- [assumed] SimilarityWeights.Keyword=1.0 and Collection=2.0, both configurable/finite/nonnegative; zero removes the term. They are signal weights, not harness thresholds.

The following are explicit engineering/schema rulings, not assumed implementation facts: enabled same-movie supplementation after OMDb Found, separate base `Actors`/`ExtendedCast` (`MovieCastEntry` or normalized string set) provenance using functionality-based Core domain records `MovieKeyword` and `MovieSignalSnapshot` (per Constitution VIII, `.specify/memory/constitution.md:402-407`), availability-aware atomic updates, `keywords(id,name)`, `movie_keywords` and `movie_tmdb_cast` reusing existing actors, defensive keyword-ID/nullable collection features and existing overlap normalization. See complete Q8 contract and analyze Round 1 repair rulings in `brief.md`/`CONCLUSIONS.md`.

## Grouping taste choices — Q9

- [assumed] Eligible same-collection candidates form a contiguous leading block up to min(10,requested capacity).
- [assumed] Within the block, existing seed similarity descending then stable MovieId; choose highest-scoring ten if more; remainder retains MMR order with selected IDs removed.
- [assumed] Disabled signals, null seed collection or no eligible matches leaves the original output order unchanged.

Indexed eligible-local candidate union before cutoff, no seed/duplicate/ineligible/external item, pure Core SeedCollectionGrouper, real score/reason preservation and no generic MMR/profile/API shape change are explicit Q9 engineering rulings.

## Evaluation names and plumbing — Q10-Q11

- [assumed] -TmdbSignals Disabled|Enabled flag; default Disabled.
- [assumed] Fixtures tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.json and baseline.tmdb.sources.json.
- [assumed] Artifacts artifacts/evaluation/DEV-358/baseline.json, tmdb.json and comparison.md.
- [assumed] Child-only LAMUFLIX_EVALUATION_TMDB_SIGNALS transport; EvaluationReplayOptions.Default strictly parses Disabled|Enabled, unset means Disabled, explicit options remain isolated.
- [assumed] WireMock payloads embedded in the named Q11 signal-test files rather than an unspecified fixture directory.

Identical original replay observations/labels/base data, truthful nonzero exercised signal provenance, unchanged cutoff/clocks/seed/cohort/lambda/weights, Disabled baseline reproduction and Enabled HitCount >= Disabled HitCount at identical nonzero denominator are mandatory Q10 acceptance rules. Report HR@10 and MRR, no extra MRR regression gate. Missing provenance, empty coverage or HR regression is BLOCKED/FAIL, never an assumed pass. No live replay/CI request, invented acceptance replay or secret-bearing provenance. Seed grouping is tested separately through the production path.

## Canonical vocabulary and `CONTEXT.md` glossary entries — Q7-Q12 & Analyze C2

| Term | Meaning | Avoid |
| --- | --- | --- |
| Keyword | Positive external metadata keyword ID with trimmed display name. | Tag, genre, free-text keyword label without ID |
| Full cast | All valid returned extended credits cast actor names without billing cap, excluding crew/character/order features. | Top-billed cast, credits with crew, character list |
| Base cast | Existing primary `Actors` preserved for disabled/ablation parity. | Overwritten cast, merged-only cast |
| Franchise collection | Exact positive `belongs_to_collection` ID, not name similarity. | Title-prefix franchise, fuzzy collection match, user playlist |
| Unavailable signal | Preserve previous persisted value for that signal. | Empty signal, cleared signal |
| Known empty | Authoritative replace/clear for that signal. | Unavailable signal, failed lookup |

Per Constitution Principle VIII (`.specify/memory/constitution.md:408`) and `recon-DEV-358-2:174-192` (all six terms confirmed absent from `CONTEXT.md` at `c2d78146`), DEV-358 adds these six canonical entries to `CONTEXT.md` in work package 2 (`CONTEXT.md` added to Q11 manifest). No new ADR is required.

## Explicit owner decision, never an assumption

- [ ] blocked: structural — Should DEV-358 retain the landed DEV-424 collections table and nullable movies.collection_id FK as the sole collection-membership persistence (adding only keyword persistence/join table and migration) instead of the collection join table named in DEV-358 Scope and Technical Design, or transition collection membership to a join table?

Patron recommends FK reuse but does not approve it on behalf of the user. Gate 1 stays closed; checkbox must remain verbatim and unticked on eventual spec PR. A join-table selection requires brief/plan amendment before build, not silent alternative implementation or duplicate state.

## Loop and phase status — Q12

Closing bar: no unresolved in-scope Critical/High, unfulfilled AC or failed/unrunnable applicable mandatory gate; all Sentry/Ledger/Compass axes required. Medium/Low get explicit disposition. Two plan-challenge review/fix rounds and separately two total build internal-review rounds are hard caps; no extra receipt-repair review. Remaining blockers stay blockers. Follow-up filing follows charter through Rigger; otherwise noted, no ticket.

No new SDK/project/port/layer/public API/frontend/LocalPlay/secret surface is assumed. Exact planned file set/tests/task order/gates are Q11-ratified in brief.md. Filename discrepancies require a bounded recorded correction before use. No tests, build gates or metric results are claimed; no plan freeze, Gate 1 approval or build pickup. No Q13.


---
## Plan Challenge Round 1 controlling amendments (2026-11-01)

Patron P1-P12 with confirmed receipt correction supersede conflicting current assumptions above; historical exchanges remain untouched. See brief.md R1-R10 and CONCLUSIONS.md complete ruling/disposition register. Q6 collection FK versus join-table checkbox remains verbatim unticked; Gate 1 closed; no new owner escalation or plan freeze.

- [assumed] P7: FullCast replaces ExtendedCast, ImmutableHashSet<string> normalized names, ImmutableArray<MovieKeyword> snapshots, ImmutableHashSet<int> feature keyword IDs; no MovieCastEntry alternative. Nested availability enum Unavailable/KnownEmpty/Available in MovieSignalSnapshot.cs; collection identity only in MovieMetadata.CollectionId/CollectionName.
- [assumed] P5: tracked comparison receipt is specs/DEV-358/comparison.md; raw baseline.json/tmdb.json remain in ignored artifacts/evaluation/DEV-358 with SHA-256/reproduction commands in receipt.
- [assumed] P6: embedded WireMock cases only in IntegrationTests/MetadataProviderTmdbSignalTests.cs; planned UnitTests signal HTTP file removed.
- [assumed] P8 corrected: EvaluationEntry.cs strictly reads child mode, unset Disabled, passes parsed mode to pure EvaluationReplayOptions.Default; explicit options independent of environment.
- [assumed] P9: enriched fixture adds keyword=1, collection=2, keeping original fields; both paired modes use same positive weights, old missing fields use record defaults, Enabled independently exercises keyword and collection terms.
- P1-P4/P10: per-signal availability drives atomic preserve/replace/clear; deterministic load/locks; overall OMDb Found survives typed supplemental failure with existing health accounting; same-movie details supplies collection; uncapped cast retains large-cast correctness smoke without performance/cardinality gate.
- P11/P12: incremental/no-backfill and embedding-less eligible seed union behavior accepted; leading franchise-block displacement taste cost retained [assumed]. README narrative residual and retained movie_tmdb_cast/TmdbSignalReplayTests provenance names noted, no ticket. Seed API proof separate from profile replay.
- Quill propagates decisions into derived artifacts/glossary through R1-R10. This addendum does not claim repairs verified.