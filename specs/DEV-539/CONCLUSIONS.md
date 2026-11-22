# DEV-539 — Patron Rulings (CONCLUSIONS)

## Q1 — ADR 0048 amending ADR-0011 for external catalogue item-to-item recommendations
- **Verdict**: Approved. Write ADR 0048 (`docs/adr/0048-*.md`) with status `Accepted` amending `docs/adr/ADR-0011.md`, and record grill Q&A in `specs/DEV-539/CONCLUSIONS.md`.
- **Basis**:
  - `specs/DEV-539/ticket-text.txt` line 28 (Wanted 1) and line 40 (AC 1): requires an `Accepted` ADR in `docs/adr/` amending `docs/adr/ADR-0011.md` to permit external catalogue item-to-item similarity (TMDb `/movie/{id}/recommendations`) while preserving library-only candidates (`docs/adr/ADR-0011.md` lines 13–15) and rejecting local collaborative filtering.
  - `specs/DEV-539/ticket-text.txt` lines 31–32 (Wanted 3–4): authorizes one first-page recommendation signal fetch (`movie/{id}/recommendations`) per enriched movie when TMDb is configured, resolving links strictly among library movies.
  - `DEV-539` task note line 20 (Rigger ADR check): `0047` is the highest existing ADR across branches; `0048` is the next free ADR number.
# DEV-539 grill conclusions

---

## Q1

[from Keel] /grill-with-docs DEV-539 — Q1. Can you accept the architectural decision for ADR 0048 as an amendment to ADR-0011? I recommend using TMDb catalogue recommendation lists as an external item-to-item signal for For you and Related, keeping candidates library-only and preserving the prohibition on collaborative filtering from local peer histories. The cost is one first-page signal fetch per source movie and reliance on an external catalogue when configured. This decision precedes feature implementation. Basis: ticket Wanted 1, 3–4 and recon-DEV-539 lines 12, 26. I will append the Q&A to F:/Dev/LamuFlix.worktrees/feature-539-spec/specs/DEV-539/CONCLUSIONS.md alongside the brief. Please confirm the ruling and record location.

[from Patron] DEV-539 Q1 ruling: Approved — record ADR 0048 (status Accepted) in docs/adr/ amending docs/adr/ADR-0011.md, and append the grill Q&A to F:/Dev/LamuFlix.worktrees/feature-539-spec/specs/DEV-539/CONCLUSIONS.md.
- Ticket Wanted 1 & AC 1 (specs/DEV-539/ticket-text.txt:28,40) mandate an Accepted ADR amending ADR-0011 (docs/adr/ADR-0011.md:13-15) for external catalogue item-to-item similarity while keeping candidates strictly library-only and local collaborative filtering prohibited.
- Ticket Wanted 3-4 (specs/DEV-539/ticket-text.txt:31-32) authorizes one first-page movie/{id}/recommendations fetch per movie when TMDb is configured, resolving links only across library movies.
- Rigger ADR check (DEV-539 note:20) confirmed 0047 is highest existing; 0048 is the next free ADR number.

## Q2 — Crowd link storage and symmetric binary resolution model
- **Verdict**: Approved. Freeze the Crowd link model at the ticket defaults: store unique pairs of `(source library movie ID, recommended TMDb ID)` including out-of-library targets, and resolve at library feature load into a symmetric binary link (`1` or `0`, no self-links, no reciprocal bonus, no remote lookups during scoring).
- **Basis**:
  - `specs/DEV-539/ticket-text.txt` lines 79, 82 (Open points) and lines 41, 44 (AC 2, AC 5): specifies a pair table of movie ID and recommended TMDb ID retaining out-of-library recommended IDs so movies added later link without re-enriching existing movies.
  - `specs/DEV-539/ticket-text.txt` lines 32–33 (Wanted 4–5) and line 81 (Open points): a link exists between two distinct library movies when either lists the other (`1` when linked, `0` otherwise; one-way counts the same as two-way).
  - `specs/DEV-539/ticket-text.txt` lines 32, 71 (`EfRecommendationCandidateSource.cs`): links resolve against the library during the existing library feature load (`recon-DEV-539` lines 21–22).

---

## Q2

[from Keel] DEV-539 Q2. Can we freeze the Crowd link model at the ticket defaults? I recommend a unique pair of source library movie ID and recommended TMDb ID, retaining out-of-library targets for future joins. At feature load, treat either direction as one binary link between distinct library movies, with no extra credit for reciprocation and no self-link. Preserve the source-owned rows until refreshed, and resolve from the existing library feature load, without remote lookups while scoring. This costs link-table storage for targets that may never enter the library. Basis: ticket Wanted 2–4 and Open points, recon lines 21–22 and 41.

[from Patron] DEV-539 Q2 ruling: Approved — freeze the Crowd link model at the ticket defaults.
- Store unique pairs of (source library movie ID, recommended TMDb ID) in the link table, retaining out-of-library target TMDb IDs so later library additions link without re-enrichment (specs/DEV-539/ticket-text.txt:41,44,79,82).
- Resolve links in memory during the existing library feature load in EfRecommendationCandidateSource without remote calls during scoring (specs/DEV-539/ticket-text.txt:32,71; recon-DEV-539:21-22).
- Treat link presence between two distinct library movies as symmetric and binary (1 when either lists the other, 0 otherwise; one-way equals two-way; no self-links) (specs/DEV-539/ticket-text.txt:32-33,81).

## Q3 — One-off backfill scope and atomic persistence of TMDb ID + recommendation links
- **Verdict**: Approved (in scope, no owner escalation needed). The explicit manual one-off backfill command checks TMDb configuration (`Enabled` true and nonblank `ApiKey`), reports IMDb eligibility, runs one IMDb find (`find/{imdb_id}`) plus one first-page `movie/{id}/recommendations` fetch per matched movie through the shared resilient TMDb `HttpClient`, persists `tmdb_id` and its recommended TMDb IDs atomically per movie, skips movies already holding both or lacking an IMDb ID, leaves unmatched/failed movies without `tmdb_id` so a retry remains eligible, and outputs matched, unmatched, and skipped counts (plus failures) without exposing the API key.
- **Basis**:
  - `specs/DEV-539/ticket-text.txt` lines 30–31 (Wanted 2–3): Wanted 2 specifies the IMDb `find` backfill for the ~449 home-library movies (`TmdbMetadataProvider.cs:462-464`) when `Enabled` is true and `ApiKey` is nonblank, and Wanted 3 states "The first run costs about 450 calls for the home library, then one call per new movie" for `movie/{id}/recommendations` — explicitly accounting for the home-library recommendation backfill and necessary for Wanted 7 / AC 10 (DEV-545 before/after leave-one-out hit rate on the home library).
  - `specs/DEV-539/ticket-text.txt` lines 43, 83 (AC 4, Open points): backfill is a manual one-off command safe to run twice, reporting counts of matched, unmatched, and skipped movies without exposing the owner's TMDb key.
  - Persisting `tmdb_id` and its first-page recommendation links together ensures idempotency on retry (`tmdb_id` is only committed once the movie's recommendation list is also stored).

---

## Q3

[from Keel] DEV-539 Q3. What should the one-off backfill do for existing movies? I recommend an explicit post-deploy command that counts IMDb eligibility, reuses one logical IMDb find per eligible movie without a TMDb ID, fetches the first recommendation page for a match through the same client, and persists the ID and link set together. Without both, the home-library run would leave existing recommendation lists empty or make a partial success ineligible for retry. Skip stored IDs and missing IMDb IDs, leave unmatched rows eligible for a later run, and report matched, unmatched, skipped and failures without key exposure. Keep the command manual and use the same Enabled plus nonblank key predicate. Cost: one additional recommendations request per matched legacy row. If filling legacy recommendations would change ticket scope, rule it as a structural owner question. Basis: ticket Wanted 2–3, backfill AC and recon lines 18–19, 24, 38, 41.

[from Patron] DEV-539 Q3 ruling: Approved (in scope; not a structural escalation).
- The ticket explicitly anticipates fetching first-page recommendations for the existing home library: Wanted 3 (specs/DEV-539/ticket-text.txt:31) states "The first run costs about 450 calls for the home library, then one call per new movie", which is also required to evaluate the DEV-545 before/after leave-one-out hit rate on the 449-movie home library (Wanted 7, AC 10).
- Implement as an explicit manual one-off command gated by Enabled && !string.IsNullOrWhiteSpace(ApiKey) (Wanted 2, Open point line 83) using the existing resilient TMDb HttpClient with no second rate limiter (Wanted 3).
- For each movie with an IMDb ID and no tmdb_id, call find by IMDb ID once and, on match, fetch the first page of movie/{id}/recommendations, persisting tmdb_id and the link rows together so partial failures remain retry-eligible. Skip movies that already have tmdb_id (or lack IMDb ID), leave unmatched movies with null tmdb_id, and report IMDb-eligible, matched, unmatched, and skipped counts (plus any failures) without exposing ApiKey (Wanted 2, AC 4).

## Q4 — Crowd criterion position, weight, denominator behavior, and reason copy
- **Verdict**: Approved. Place binary `Crowd` (`1` when linked, `0` otherwise) after `Themes` in the criteria table for both `For you` and `Related` with default weight `2` in `RecommendationOptions.Weights`, include it in the criteria table and score denominator only when TMDb is configured (`Enabled` is true and `ApiKey` is nonblank) and Crowd is active, and use the ticket's reason strings (`"Often liked together with {seed title}"` on `For you`, `"Often liked together"` on `Related`).
- **Basis**:
  - `specs/DEV-539/ticket-text.txt` lines 33, 72, 80 (Wanted 5, Where it lands, Open points): sets `Crowd` position after `Themes`, value `1` when linked else `0`, default weight `2` in `RecommendationOptions.Weights`, and exact reason strings `"Often liked together with {seed title}"` (For you) and `"Often liked together"` (Related).
  - `specs/DEV-539/ticket-text.txt` lines 33–34, 45–47 (Wanted 5–6, AC 6–8): denominator grows by Crowd's weight only when TMDb is configured (`Enabled` true and `ApiKey` nonblank); when TMDb is not configured, Crowd is absent from the criteria table and `For you` / `Related` scores match pre-change DEV-545 snapshot scores.
  - `specs/DEV-539/ticket-text.txt` lines 33, 35, 49, 84 (Wanted 5, 7, AC 10, Open points): re-run the DEV-545 coverage table and judged pairs with Crowd on to re-tune the strong-match bar and verify leave-one-out top-10 hit rate does not drop.

---

## Q4

[from Keel] DEV-539 Q4. How should Crowd affect scoring and explanations? I recommend adding binary Crowd after Themes in both For you and Related, weight 2, applying the denominator weight to all pairs only when Crowd is active. Use the ticket reason copy for linked pairs and existing reason selection, with no criterion or denominator change when TMDb is disabled or its key is blank. The cost is lower configured-state scores for unlinked pairs and a separate coverage check before choosing the active threshold. Basis: ticket Wanted 5–7, score parity and criteria table ACs, recon lines 23–24 and 41.

[from Patron] DEV-539 Q4 ruling: Approved — freeze Crowd scoring, position, denominator, and reason copy per ticket defaults.
- Place binary Crowd (1 when linked, 0 otherwise) immediately after Themes in the criteria order for both For you and Related, with default weight 2 in RecommendationOptions.Weights (specs/DEV-539/ticket-text.txt:33,72,80).
- Emit reason text "Often liked together with {seed title}" on For you and "Often liked together" on Related for linked pairs using existing reason selection (specs/DEV-539/ticket-text.txt:33).
- Include Crowd in the criteria table and add its weight to the score denominator only when TMDb is configured (Enabled && !string.IsNullOrWhiteSpace(ApiKey)) and Crowd is active; when unconfigured, omit Crowd completely so For you and Related scores match the pre-change DEV-545 snapshot (specs/DEV-539/ticket-text.txt:33-34,45-47).

---

## Q5

[from Keel] DEV-539 Q5. How should DEV-545 evidence determine rollout and the strong-match bar? I recommend a Crowd-specific activation control under RecommendationOptions, with the checked-in default off until same-library before/after evidence shows enabled leave-one-out top-10 hit rate at least equal to baseline. Report MRR, coverage and judged pairs, and keep the legacy threshold for inactive states. Share that threshold only if enabled coverage and judged pairs support it, otherwise configure a Crowd-active threshold, using one threshold at runtime. Leave the default off if hit rate falls and attach both results. The extra option is needed to keep TMDb enrichment available regardless of rollout. Basis: ticket Wanted 5–7, final AC and Open points. Please send the full cited ruling as before. I will append the complete exchange to CONCLUSIONS.md, so you can leave that append to me.

[from Patron] DEV-539 Q5 ruling: Approved — govern Crowd rollout and strong-match bar tuning via DEV-545 evidence and RecommendationOptions.
- Add a Crowd activation control under RecommendationOptions (or gate via Crowd weight / flag independently of Tmdb:Enabled so TMDb enrichment stays active even if Crowd scoring is off), shipping enabled by default only when same-library DEV-545 leave-one-out top-10 hit rate is >= baseline, and attaching before/after hit rate and MRR to the PR (specs/DEV-539/ticket-text.txt:12,35,49).
- Preserve the existing strong-match bar whenever Crowd is inactive (TMDb unconfigured or Crowd disabled) to guarantee exact pre-change score parity (specs/DEV-539/ticket-text.txt:34,45-46).
- Re-tune the strong-match bar from the DEV-545 coverage table and judged pairs with Crowd active: keep a single shared bar value only if the coverage table and judged pairs show it holds across both states; otherwise apply the Crowd-active bar when Crowd is active (specs/DEV-539/ticket-text.txt:33,84).

---

## Q6

[from Keel] DEV-539 Q6. How should a failed recommendations fetch behave during normal enrichment? I recommend carrying the mapped TMDb ID when the usual enrichment succeeds, distinguishing a successfully empty first page from fetch failure. Replace recommendation rows on successful fetch including an empty list, and preserve prior rows on failure for the same source TMDb ID. On a changed source ID, clear rows associated with the previous identity. The backfill remains atomic as ruled in Q3. This keeps Crowd optional and avoids losing links on transient failures, at the cost of an explicit fetch outcome in the metadata path. Basis: ticket Wanted 2–4 and Out of scope, recon lines 15–19 and 24. Please leave the CONCLUSIONS.md append to me.

[from Patron] DEV-539 Q6 ruling: Approved — treat movie/{id}/recommendations during normal enrichment as a non-fatal signal fetch that distinguishes an empty response from a fetch failure.
- Carry the mapped tmdb_id whenever primary TMDb enrichment succeeds, matching how keywords and credits signals are fetched in TmdbMetadataProvider.Signals.cs without failing overall enrichment (specs/DEV-539/ticket-text.txt:29,31,66,68; recon-DEV-539:17,19).
- On a successful recommendations fetch (including an empty first page), replace the movie's stored recommendation link rows with the returned set; on a recommendations fetch failure, preserve existing link rows if the movie's tmdb_id is unchanged, or clear stale rows if tmdb_id changed (specs/DEV-539/ticket-text.txt:31-32,57).
- Keep the one-off backfill atomic per movie as ruled in Q3 so failed backfill fetches remain eligible for retry (specs/DEV-539/ticket-text.txt:30-31,43).

---

## Q7

[from Keel] DEV-539 Q7. What should be the backfill command boundary? I recommend an explicit one-off operator mode on the existing API executable, dispatched after the existing configuration and DI setup and before the normal web host starts. The operator applies the migration first and invokes the mode against the existing library using existing configuration. Reuse the existing services and typed client. This adds an opt-in application command entry point but avoids a new project, public route, hosted job, or new dependency. Basis: ticket Open point line 83, Wanted 2–3, recon lines 24 and 37–41. Please leave the CONCLUSIONS.md append to me.

[from Patron] DEV-539 Q7 ruling: Approved — run the one-off backfill as an explicit CLI operator mode on the existing LamuFlix.Api executable after configuration/DI setup and before starting the web host.
- Satisfies the ticket default of a manual one-off command run once after deploy and safe to run again (specs/DEV-539/ticket-text.txt:30,43,83) while reusing existing configuration and the shared resilient TMDb typed HttpClient registered in MetadataProviderServiceCollectionExtensions (specs/DEV-539/ticket-text.txt:30-31,74; recon-DEV-539:24).
- Complies with specs/PRODUCT.md §5 care items (1, 2, 4, 5): introduces no new NuGet dependency, no new project or layer, no new public HTTP route/DTO, and no Process.Start or secret exposure (recon-DEV-539:34-38).

---

## Q8

[from Keel] DEV-539 Q8. Can we close the grill with this scope and review bar? I recommend freezing ADR 0048, persistence, TMDb enrichment, the one-off backfill, library feature resolution, both Crowd scoring consumers, and DEV-545 rollout evidence. Respect the ticket exclusions and preserve existing public API, frontend and other enrichment behavior. Require Accepted ADR and merged spec Gate 1 before coding, DEV-543 and the DEV-544 load seam before scoring integration, and DEV-545 before tuning and rollout. Require written dispositions for every in-scope finding, with unresolved Critical, High or Medium blocking and Low blocking when accepted for this round, plus the normal Size L gates and all ticket acceptance evidence. Cap review at two rounds and remediation at two fix commits per round. The cost is that unresolved above-bar work at the cap needs the PR owner to decide next steps. Anything else is a follow-up issue, not a finding in this round. Basis: task-pipeline lines 25–29, 80–87, 102–112, 116–140, ticket Wanted, ACs, Out of scope and Needs. Please confirm the closing terms and leave the CONCLUSIONS.md append to me.

[from Patron] DEV-539 Q8 ruling: Approved — close the DEV-539 grill with the frozen scope, dependencies, and standard Size L review/closing bar.
- Scope and exclusions are frozen per Q1-Q7 and specs/DEV-539/ticket-text.txt (Wanted 1-7, ACs 1-10, Out of scope lines 51-57, Needs lines 86-90): ADR 0048 (Accepted before implementation), migration/persistence, TMDb enrichment & manual CLI backfill, library-only feature load resolution (DEV-544 seam), For you (DEV-543) & Related Crowd scoring, and DEV-545 hit-rate/MRR rollout evidence, with no public API/DTO or frontend changes.
- Standard pipeline closing bar applies per task-pipeline (lines 25-29, 80-87, 102-112, 116-140): Gate 1 spec PR merged before coding, all Size L verification gates and ticket ACs satisfied, written dispositions for all in-scope findings (unresolved Critical/High/Medium and accepted Low blocking), and 2 review rounds / 2 remediation commits per round cap.

---

## C1 glossary file scope

[from Patron] DEV-539 Round 1 C1 ruling (§2.3 item 6 — File Scope): Approved — updating root CONTEXT.md in the introducing delivery PR via T014a is in scope as a constitution-mandated edit.
- Constitution §VIII (.specify/memory/constitution.md:687,702) requires that domain terms be defined in CONTEXT.md and states: "A new domain term MUST be added to CONTEXT.md in the same PR that introduces it."
- Under charter §2.3 item 6, editing an unnamed tracked file when a gate or constitution requirement forces the edit is in scope ("a forced edit is in scope: cite the AC or gate and move on").
- Scope of T014a in CONTEXT.md is strictly bounded to adding Crowd, Crowd link, recommended TMDb ID, Crowd criterion, and Crowd activation, and aligning Ordered criterion and Contribution with active Crowd after Themes per brief Q2/Q4/Q5 and specs/DEV-539/ticket-text.txt:26,32-34,80.


---

## Plan challenge adjudication (Ledger H1/H2/M1/M2/M4/L1, Compass F1, Sentry M2)

[from Patron] DEV-539 plan challenge ruling: Approved — authorize the named Core scoring/carrier/handler/doc boundary updates, credential-safe TMDb diagnostics, existing-selection reason rules with optional CrowdActiveStrongMatchThreshold, duplicate-TMDb-ID link resolution with cascade delete, and the --backfill-tmdb-recommendations CLI switch.
- **File boundaries, Core backfill handler, ports, and docs (§2.3 items 2 & 6; Constitution I, II, VIII)**: Authorize the narrowly named Core scoring, reason-builder, and pure in-memory feature-carrier edits (`SimilarityScorer.cs`, `RecommendationReasonBuilder.cs`, `RecommendationScoring.cs`, `MovieFeatures` / `MovieFeaturesSnapshot`, and DEV-545 evaluation feature-load alignment if needed) from Ledger H1 and Compass F1 as forced by Ticket Wanted 4–6 and ACs 5–8 (`specs/DEV-539/ticket-text.txt:32-34,44-47`). Authorize a Core backfill command, handler, and 5-count result record registered via `HandlerRegistration.cs` (`AddHandler`) with `LamuFlix.Api/Program.cs` strictly parsing `--backfill-tmdb-recommendations` (rejecting malformed operator `argv`, preflighting migration/config, running sequentially and cancellably through the shared TMDb client with no new limiter) under Constitution II and Q3/Q7 (`CONCLUSIONS.md:40-56,100-106`). Reuse existing ports where sufficient (`T001` inventories them; any new port requires a cited Patron ruling and an ADR under Constitution I). Authorize surgical updates to `docs/recommendations.md` (limited to current criterion count, active/inactive denominator, and Crowd reason copy) and `docs/adr/0048-tmdb-crowd-similarity.md` (Consequences, rejected options, and targeted clarification of historical `ADR-0046`) under Constitution VIII and C1 (`CONCLUSIONS.md:120-125`).
- **Scoring, reason eligibility, threshold option, and duplicate TMDb IDs (§2.3 item 3; Q2, Q4, Q5)**: Make no change to strong-reason eligibility or default-tier selection unless already required by Q4/DEV-543; `Crowd` participates in existing table-order reason selection immediately after `Themes`, and tests exercise a fixture where `Crowd` wins (`CONCLUSIONS.md:58-75`; `specs/DEV-539/ticket-text.txt:33`). Authorize optional `CrowdActiveStrongMatchThreshold` on existing `RecommendationOptions` (validated in `[0, 1]`), used only when Crowd is active and falling back to the existing strong-match bar when inactive or unset (`CONCLUSIONS.md:78-86`; `specs/DEV-539/ticket-text.txt:33,84`). Keep `movies.tmdb_id` non-unique, resolve links across all distinct library rows matching a recommended TMDb ID without linking distinct rows merely because their `tmdb_id` values are equal (and no self-links), cascade-delete source-owned pair rows on source movie deletion (with an index on `recommended_tmdb_id`), and retain out-of-library recommended TMDb IDs (`CONCLUSIONS.md:22-38`; `specs/DEV-539/ticket-text.txt:32,41,44,79,81-82`).
- **Credential-safe TMDb diagnostics (§2.3 item 5 & Scrub list; Constitution VI)**: Authorize credential-safe TMDb diagnostics for Sentry M2 that redact query values including `api_key`, omit full request URIs, raw exceptions, payloads, and secrets from logs/output/evidence, and add a sentinel test on the existing typed client without changing auth or configuration wiring (`specs/DEV-539/ticket-text.txt:30,43,73-74`).


---

## Gate 1 ruling (Phase 2 step 7)

[from Patron] DEV-539 Gate 1 ruling: `gate1: provisional` — 0 structural owner checkboxes confirmed.
- `PLAN_ADJUDICATION.md` (`PLAN FROZEN`, 25/25 findings adjudicated across Sentry, Ledger, and Compass), `brief.md`, `spec.md`, `plan.md`, `tasks.md`, and Accepted `docs/adr/0048-tmdb-crowd-similarity.md` align with `specs/DEV-539/ticket-text.txt` and `CONCLUSIONS.md` Q1–Q8, C1, and the plan challenge ruling.
- `specs/DEV-539/checklists/requirements.md` lines 32–37 have all `User-Visible Outcome` items ticked (`SMOKE-001`–`SMOKE-003`).
- Structural owner checkboxes: `0` (no ticket contradiction or constitution departure under charter §2.3; ordinary owner Gate 1 spec PR merge remains pending per `T000`).

---

## Gate 1 spec PR merge receipt

- The owner merged [spec PR #460](https://github.com/lamuniercosta/lamuflix/pull/460) into `main` on 2026-11-21 at 17:15:51 UTC. GitHub records merge commit `04e31d45d93aa5a87f0a3d5202b60f5ae434b077`.
- Phase A base `1bbc8a637b97ec2efdf0eaa6f2be6da8abbbc089` is an ancestor of the merge commit, which is included in the DEV-539 worktree history.
- This receipt completes T000 and satisfies the Gate 1 prerequisite for T001 read-only pickup.

---

## Phase 2 and Phase 3 T007 persistence boundary

**Verdict. In scope under charter §2.3 item 6.** Anvil's Phase 2 receipt in the `DEV-539` Maestri note (lines 43–48) flags three paths outside the explicit inventory in [plan.md](plan.md#L53). The [ticket's AC 2](ticket-text.txt#L41) requires the nullable `movies.tmdb_id` column and the recommendation pair table. The [next enrichment criterion](ticket-text.txt#L42) requires persisting both the TMDb ID and the recommended IDs, as specified in [Wanted 2–3](ticket-text.txt#L29). The charter (`lamuflix-team-charter`, line 24) puts edits forced by an acceptance criterion in scope. The T001 pickup entry in `recon-DEV-539` (line 33) identifies `EfMovieRepository` as the existing movie persistence seam.

- **Phase 2 (T002/T004/T003).** Approve the three changes at `159ad381` under `src/LamuFlix.Infrastructure/Persistence/`. `Records/MovieRecord.cs` carries the nullable TMDb ID and the source's recommendation collection. `Records/MovieRecommendationRecord.cs` represents the source movie ID and the recommended catalogue ID, including targets outside the library. `EfMovieRepository.cs` carries the nullable ID through insert, baseline comparison, tracked update, and rehydration. These changes support the EF mapping, migration and domain carry in [tasks.md](tasks.md#L21).
- **Phase 3 T007.** Authorize edits to these paths only as required to persist source-owned pair updates through the existing EF save flow. Under [Q6](#q6) and [FR-005](spec.md#L195), a successful first page replaces the source's rows, including clearing them for a valid empty page. A failed fetch preserves rows when the source TMDb ID is unchanged and clears stale source rows when the ID changes. Persist the ID and affected rows together in the existing save flow. The enrichment handler retains its apply-and-save role under [FR-003](spec.md#L193). [T008](tasks.md#L32) requires integration evidence for the requested path, first page and persisted outcomes.

---

## Phase 6 scoring, evaluation, and glossary boundary

**Verdict. In scope under charter §2.3 item 6.** Reviewed `df855da3...d3044658` on `feature/DEV-539` against Anvil's Phase 6 receipt in the `DEV-539` task note (lines 77–83), [T014a–T016](tasks.md#L45-L51), and the existing [plan challenge ruling](#plan-challenge-adjudication-ledger-h1h2m1m2m4l1-compass-f1-sentry-m2). The four boundary items have the following dispositions.

- **T015 reason path. Accept the landed DEV-543 seam.** T001 recon (`recon-DEV-539`, lines 32 and 35) records the removal of `RecommendationReasonBuilder` and the DEV-543 scoring contract. Read the builder reference in T015 as the required winning-seed title flow through the current evidence path. The For you handler passes the winning seed title to `SimilarityScorer.BuildEvidence`. Related uses the scorer's explicit Crowd reason. This is the same scope approved in [Q4](#q4) and applies the copy in [ticket Wanted 5](ticket-text.txt#L33), including literal handling of the seed title. Keep the existing reason selection, strong-reason eligibility, and default tiers subject to Q4 and the landed contract.
- **T015 scorer and caller wiring. Accept the dependency updates.** `NearestSeedScorer.cs` carries the resolved Crowd scoring state and movie identities into prepared pair scoring for For you. `tests/LamuFlix.Evaluation/RealLibrary/HiddenSeedEvaluationCopy.cs` supplies the added TMDb options dependency when it constructs the production For you handler. The scorer edit is required by [Wanted 5](ticket-text.txt#L33) and the evaluation caller update preserves the existing handler path required by [Wanted 7](ticket-text.txt#L35). These edits implement the approved shared scoring and DEV-545 integration boundaries.
- **T015a/T016 parity and T017 measurement. Accept the empty-link snapshot for Phase 6 compatibility.** The evaluation copy supplies `CrowdLinks.Empty` at `d3044658`. This supports the disabled and unconfigured parity cases against the pinned pre-Crowd fixtures. With Crowd active, an empty link set can add the weight to the denominator while contributing zero for every pair. Keep [T017](tasks.md#L56) open. After T019 and confirmation of DEV-545 readiness, load persisted links through the production-equivalent feature resolution and exercise production scoring with Crowd active. Verify that the candidate run sees resolved links. Compare the same home library and evaluation inputs and attach sanitized top-10 hit rate, MRR, coverage, and judged pairs. Apply the default and threshold decision in T018 after that evidence.
- **T014a glossary and scoring reference. Accept the documentation updates.** Root `CONTEXT.md` defines the five terms approved in [C1](#c1-glossary-file-scope) and updates Ordered criterion and Contribution. `docs/recommendations.md` aligns the criterion count and ordering, conditional denominator, and Crowd reason copy, including the corresponding configuration and test references. These edits belong in the introducing delivery PR under Constitution §VIII, C1, and the plan challenge ruling.

The boundary review closes the four items from Anvil's Phase 6 receipt with no Phase 6 rework or structural owner checkbox. T017 and T018 retain their existing prerequisites and evidence requirements.

---

## Phase 7 operator and DEV-545 evidence (T017–T019)

Sanitized operator evidence recorded by Anvil for the delivery PR body. Raw artifacts stay unversioned under the ignored `artifacts/recommendations/` directory. No credential, connection string or API key value appears in this section, its commands or the artifacts.

### T019 home-library backup, migration and operator counts

| Check | Result |
|---|---|
| Backup before any change | `pg_dump -Fc` of the home `lamuflix` database to `F:/Dev/lamuflix-home/backups/dev539-pre-migration-20261122-1757.dump` (1399218 bytes). Rollback readiness confirmed with `pg_restore -l` on that dump: exit 0, 18 TABLE DATA entries including `public.movies` and `public.movie_features`. `scripts/home/Restore-HomeCatalog.ps1 -DumpPath <dump>` is the documented rollback path. |
| Migration | `dotnet ef database update` through `LamuFlixDesignTimeDbContextFactory` applied `20261122171551_AddCrowdRecommendations` against the home database. `movie_recommendations` exists and `__EFMigrationsHistory` records the migration. |
| Operator run | `dotnet run --project src/LamuFlix.Api -- --backfill-tmdb-recommendations` with configured TMDb, exit 0, 451 s, complete (not stopped or partial). Sanitized counts: `imdb-eligible=447 matched=446 unmatched=1 skipped=0 failures=0`. |
| Idempotent rerun | Same invocation immediately afterwards, exit 0: `imdb-eligible=447 matched=0 unmatched=1 skipped=446 failures=0`. The one unmatched row stays eligible; every stored row is skipped. |
| Persisted result after the runs | 446 of 447 movies carry a `tmdb_id`; 8920 `movie_recommendations` rows owned by 446 distinct sources. |

### T017 candidate measurement on the production Crowd path

The DEV-545 harness previously supplied `CrowdLinks.Empty`. It now carries the link lookup resolved by the production `EfRecommendationCandidateSource.GetLibraryFeaturesAsync` into `RealLibrarySnapshot.CrowdLinks`, and `HiddenSeedEvaluationCopy` passes that lookup to the production For you handler. `CompareRunner` and `QualityReportRunner` now pass `CrowdScoring.Resolve(...)` with the selected and candidate movie identities to the production `SimilarityScorer`. Nonsecret harness markers select the state: `LAMUFLIX_EVALUATION_CROWD_ENABLED`, optional `LAMUFLIX_EVALUATION_CROWD_ACTIVE_THRESHOLD`, and `LAMUFLIX_EVALUATION_TMDB_CONFIGURED` (placeholder key; the real key is never read by the harness).

Both runs used the same home library, harness revision and inputs (447 enriched movies, leave-one-out take 52 eligible candidates per seed, cohorts liked 103 / watched 396, overlap 103). "Before" is Crowd inactive with TMDb configured, which FR-012 defines as the pre-change scoring parity state; "after" is Crowd active with the shared strong-match bar (no active-only threshold set).

| Cohort | Before HR@1 | Before HR@3 | Before HR@10 | Before MRR | After HR@1 | After HR@3 | After HR@10 | After MRR |
|---|---|---|---|---|---|---|---|---|
| liked (103 seeds) | 0.0971 | 0.2621 | 0.5534 | 0.2297 | 0.0874 | 0.2913 | 0.5728 | 0.2092 |
| watched (396 seeds) | 0.1162 | 0.2424 | 0.5379 | 0.2304 | 0.1111 | 0.2854 | 0.5278 | 0.2187 |

Deltas: liked HR@10 +0.0194 with MRR -0.0205; watched HR@10 -0.0101 with MRR -0.0116. Absolute values are harness-relative full-eligible measurements; only the same-protocol deltas are comparable, per the DEV-545 disclosure.

Coverage table (enriched selected 447, score-only partitions, zero / one-to-three / four-to-ten / above-ten):

| Bar | Before | After |
|---|---|---|
| 0.225 | 21 / 105 / 199 / 122 | 28 / 142 / 204 / 73 |
| 0.25 | 34 / 173 / 172 / 68 | 63 / 167 / 176 / 41 |
| 0.30 | 97 / 208 / 117 / 25 | 127 / 180 / 127 / 13 |
| 0.35 | 173 / 179 / 89 / 6 | 197 / 172 / 75 / 3 |

Artifacts (unversioned, SHA-256): `dev539-before-leave-one-out.json` `a5fa8972…aeb3` (119607 bytes), `dev539-after-leave-one-out.json` `b3c20033…f155` (113577 bytes), `dev539-before-quality.json` `52531ecb…1c23d` (8233 bytes), `dev539-after-quality.json` `d923ecd4…9f3e` (8264 bytes).

**Judged pairs gap.** The owner-private, unversioned `artifacts/recommendations/judged-pairs.json` is not present in any accessible worktree, and the DEV-545 publication deliberately excluded it. Both quality reports therefore record `validCount` 0 and null judged-pair shares with zero skipped entries. The judged-pairs attachment remains outstanding for the delivery PR; no judgment was invented or reconstructed.

### T018 activation default and threshold decision

- **Checked-in default stays off.** The watched cohort (396 of 499 leave-one-out seeds) top-10 hit rate falls from 0.5379 to 0.5278 and MRR falls in both cohorts, so the enabled configuration does not hold the baseline hit rate. `RecommendationOptions.CrowdEnabled` keeps its `false` default and no test expectation changes.
- **No Crowd-active strong-match bar is selected.** Because Crowd stays inactive by default and the judged-pairs input is unavailable, `CrowdActiveStrongMatchThreshold` remains unset in the checked-in configuration. The shared `MinimumSimilarity` bar (0.225) continues to govern inactive scoring, matching the pre-change behaviour in the before run.
