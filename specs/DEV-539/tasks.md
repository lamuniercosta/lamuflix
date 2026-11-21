---
description: "Task list for DEV-539 TMDb Crowd similarity criterion"
---

# Tasks: TMDb Crowd similarity criterion

**Input** — Design documents in `specs/DEV-539/`: spec, plan, research, data-model, quickstart, brief, conclusions. Authority includes the DEV-539 ticket, `recon-DEV-539`, Accepted ADR 0048, and `specs/PRODUCT.md`.

**Status** — PLAN FROZEN. See [plan adjudication](PLAN_ADJUDICATION.md). Build tasks depend on the verified owner Gate 1 merge receipt.

Work inside the file boundaries in `plan.md` and `brief.md`. Follow phase order. `[P]` denotes disjoint work with prerequisites complete.

## Phase 0: Gate 1

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares and opens the DEV-539 spec PR (includes Accepted ADR 0048 in review). Only the user merges. **Keel** records the verified owner merge receipt in `specs/DEV-539/CONCLUSIONS.md`. All Build tasks depend on this receipt.

## Phase 1: Read-only pickup

- [ ] T001 After T000, **Wisp** checks picked HEAD against the named domain, adapter, persistence, scoring/carrier, configuration, startup, handler registration and test seams. Inventory the existing metadata, movie persistence and transaction ports for the Core operator handler. Trace the DEV-545 loader/scorer integration and identify exact files if alignment is needed. Record DEV-543, DEV-544 and DEV-545 readiness for T015, T012 and T017 respectively. Stop with `needs recon` for missing required facts. Keel records demonstrated drift and obtains a cited ruling before boundary extensions. Any new port requires a cited Patron ruling and ADR before implementation.

## Phase 2: Schema and domain carry (US1 foundation)

- [ ] T002 [US1] After T001, map nullable, non-unique `movies.tmdb_id` and unique recommendation pairs in `MovieConfiguration.cs`. Cascade source-owned rows on movie deletion, retain incoming catalogue IDs and index `recommended_tmdb_id`.
- [ ] T004 [P] [US1] After T001, extend `src/LamuFlix.Core/Domain/MovieMetadata.cs` and `src/LamuFlix.Core/Domain/Movie.cs` so `MarkEnriched` carries TMDb id and recommendation-fetch outcome through the existing persistence flow without changing `ProcessEnrichmentCommandHandler` behaviour beyond apply-and-save.
- [ ] T003 [US1] After T002 and T004, add the EF migration under `src/LamuFlix.Infrastructure/Persistence/Migrations/` for `movies.tmdb_id` and the pair table; prove it applies against a real Postgres database fixture that already holds movie rows (model changes from T002/T004 required before migration generation or verification).

## Phase 3: TMDb enrichment signals (US1)

- [ ] T005 [US1] After T004, map the TMDb response id into metadata in `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.Mapping.cs`.
- [ ] T006 [US1] After T005, add first-page-only fetch beside keywords and credits in the named TMDb adapter files. Use the configured typed client and existing resilience handler. Validate page shape and the source ID if echoed by the response contract. Distinguish valid empty pages from transport and malformed-page failures. Record safe recommendation outcome categories without changing keyword/credits health behavior. Redact credential query values including `api_key`; exclude full URIs, raw exceptions and payloads at the logging/output boundary.
- [ ] T007 [US1] After T006, implement replace-on-success (including empty page), preserve-on-failure when TMDb id unchanged, and clear-stale-rows when TMDb id changed for normal enrichment.
- [ ] T008 [US1] After T006/T007, use existing `LamuFlix.Tests.Common` WireMock and Testcontainers Postgres patterns through the real typed client. Assert exactly one recommendations request in the healthy fixture advertising additional pages, the requested path, persisted IDs/rows after reload, empty replacement, malformed-page failure and Q6 preserve/clear behavior. Capture relevant typed-client/application logs and prove a synthetic key sentinel is absent on success and failure. Use repository test comment conventions.

## Phase 4: Operator backfill (US2)

- [ ] T009 [US2] After T003/T006, parse dedicated `--backfill-tmdb-recommendations` in `Program.cs`. Reject malformed operator invocations with a nonzero exit. Preflight configured TMDb and the applied migration before dispatch, outbound calls or host start. Define the Core command and five-count result and register the handler via existing `HandlerRegistration.cs`/`AddHandler`. Keep Api startup to argument handling, preflight, dispatch and sanitized output. Use T001-verified existing ports and preserve normal host/configuration argument behavior.
- [ ] T010 [US2] After T009, process one movie at a time with cancellation through lookup, fetch and persistence. Skip missing IMDb IDs and stored TMDb IDs. Make one logical IMDb find per eligible row and one first-page fetch per found match through the existing bounded retry/circuit policy (including 429). Atomically commit ID and pairs per movie. Unmatched and failed rows stay eligible. Report IMDb eligibility and distinct matched, unmatched, skipped and failure counts. Stop on breaker rejection or cancellation with a nonzero exit and clearly marked sanitized partial counts. Retain completed commits; reruns skip those rows. Use injected `TimeProvider` if timing is recorded.
- [ ] T011 [US2] After T010, exercise the existing WireMock/real-Postgres fixtures. Prove startup refusal for malformed operator invocation, missing configuration and absent migration before work or hosting. Assert skip/match/unmatched/failure paths, truthful counts, atomic retry eligibility, idempotent rerun, logical find/fetch budget and bounded retries including a 429 fixture. Prove breaker/cancellation stops with partial counts, intact commits and a safe rerun. Capture sanitized output/logs on failure and success and assert no synthetic API-key sentinel appears.

## Phase 5: Library feature-load resolution (US3)

- [ ] T012 [US3] After T003/T004 and DEV-544 readiness, batch-load pairs at `EfRecommendationCandidateSource` library feature load. Map each TMDb ID to all corresponding library movie IDs. Build an immutable symmetric binary lookup across distinct catalogue identities with no self-links or reciprocal bonus. Pass it through the existing library feature snapshot to pure scoring. If DEV-544 is absent, report a phase-local dependency blocker to Bernstein.
- [ ] T013 [US3] After T012, use real Postgres to prove late-add links without source re-enrichment, library-only candidates, symmetric one-way links, binary reciprocity, no self-links and duplicate source/target identity behavior. Delete a source and prove source-owned pairs cascade while incoming catalogue IDs remain; verify the targets become unresolved when no matching library row remains. Assert pair reads are bounded to at most one added batch query per library feature load with no per-movie queries.

## Phase 6: Crowd scoring and parity (US4)

- [ ] T014 [US4] After T001, extend `SimilarityWeights` and `RecommendationOptions` with Crowd weight default 2, sibling nonnegative range validation and independent activation Boolean default off. Validate finite positive effective weight sums for inactive and active modes and update the fixed-six validation wording. Preserve existing review-channel validation. Add optional `CrowdActiveStrongMatchThreshold` validated in `[0, 1]`, including rejection of non-finite values.
- [ ] T014a [US4] In the introducing delivery PR, apply the C1 glossary changes in root `CONTEXT.md` and align `docs/recommendations.md` on current criterion count, conditional denominator and Crowd reason copy. See the brief's Plan challenge decisions for scope.
- [ ] T015 [US4] After T012/T014 and DEV-543 readiness, integrate binary Crowd after Themes in both consumers, with weight in the configured active denominator for all pairs. Omit Crowd and keep the original bar when inactive or unconfigured. In both existing strong-match paths, select `CrowdActiveStrongMatchThreshold` only when active and set. Register Crowd with existing reason selection, thread the winning seed title through `RecommendationReasonBuilder`, and map explicit Related copy in `SimilarityScorer`. Use `const` static reason literals. Preserve strong-reason eligibility and default tier rules unless Q4 or the landed DEV-543 contract requires a change. If DEV-543 is absent, report a phase-local blocker to Bernstein.
- [ ] T015a [US4] After T001, capture or verify pinned For you and Related DEV-545 score fixtures for disabled TMDb and Enabled plus blank ApiKey from a revision with the pre-Crowd scorer. Record base SHA, harness revision, identical fixture inputs and seed when applicable. Never derive expected parity from the changed scorer. Run independently of T002–T015; T016 waits for this evidence. The home-library harness run is T017.
- [ ] T016 [US4] After T015/T015a, pin criterion order, weight, binary contribution, denominator and exact pre-change scores for disabled TMDb and Enabled plus blank ApiKey. Cover configured Crowd-off and Crowd-on states, invalid effective sums and threshold values, and threshold selection for active/set, active/unset and inactive states. Exercise both actual winning-Crowd reason paths under the existing selection rules, with exact copy and a seed title containing punctuation/braces treated as literal data (SMOKE-001/002).

## Phase 7: DEV-545 evidence and defaults (US5)

- [ ] T019 [US5] After T003/T011, confirm home-library backup and rollback readiness, apply the migration, run the operator and capture actual IMDb eligibility, matched, unmatched, skipped and failure counts before Crowd-enabled measurement. A refused or partial run does not satisfy T019. Publish sanitized actual counts on the delivery PR.
- [ ] T017 [US5] After T015/T016 and T019, and once DEV-545 is ready, confirm candidate measurement uses the production Crowd feature/scoring path. If alignment is required, use the exact path inventory and approved boundary from T001. Run before/after with the same home library and inputs. Attach sanitized leave-one-out top-10 hit rate, MRR, coverage table and judged pairs. Missing DEV-545 blocks only this evidence phase.
- [ ] T018 [US5] After T017, set and record the default activation decision. Enable by default only if hit rate holds. Use coverage and judged pairs to select a value for the already wired active-only threshold when required, or leave it unset and document evidence for the shared threshold. Verify the final scored configuration against the DEV-545 comparison.

## Phase 8: Gates, Size L review, ship, and mergeability

- [ ] T020 After T008/T011/T013/T016 (and T018 when it changed scoring/options), run UnitTests and IntegrationTests for in-scope suites; resolve in-scope failures. Confirm ticket acceptance evidence set is complete: WireMock/path proofs, real-Postgres persistence/resolution/atomic-retry proofs, late-add and one-way/reciprocal/self-link pins, criteria/reason/parity pins including Crowd-win reason selection against T015a snapshots, home-library migration + Q3 operator counts (IMDb eligibility, matched, unmatched, skipped, failures) for the delivery PR, DEV-545 before/after hit rate and MRR, coverage table, judged pairs, and threshold/default (plus any Crowd-active bar) decision on the delivery PR.
- [ ] T021 After T020, run and record Size L gates with actual statuses: Roslyn, complexity (including refactor `-Threshold 6`), InspectCode, mutation (`scripts/run-mutation.ps1` without `--since` from a worktree), property tests, vulnerable packages, format, tests; web `SKIPPED (scope-empty)` when empty. Failure or `Could not run` blocks.
- [ ] T022 After T021, complete Size L three-axis review (Sentry, Ledger, Compass) against the frozen diff per brief closing bar (lines 40–44): two rounds, two remediation commits per round; pinned review reports and written dispositions for every in-scope finding; unresolved Critical/High/Medium and accepted Low block. Escalate unresolved above-bar findings at the cap for owner review. Then follow task-pipeline Phase 5 (lines 127–140) in order: (1) rebase onto latest default (`scripts/rebase-task-branch.ps1 -Push`); (2) rerun Phase 3 gates on that rebased head; (3) run Size L ship review; (4) open the delivery PR with home-library (T019) and DEV-545 (T017/T018) evidence on the PR body; (5) verify clean findings artifact, green hosted checks including `gitleaks`, and live mergeability (`mergeable` MERGEABLE; `mergeStateStatus` not BEHIND or DIRTY); (6) post the merge-bar receipt as a PR comment. Apply brief exit-2 dispositions (`SKIPPED (scope-empty)`, mutation `NOT APPLICABLE`, harness-disabled `SKIP`); exit 1 and `Could not run` never meet the bar. The user owns the merge.

## Dependencies & execution order

- T000 → T001 → schema/domain (T002 ∥ T004 → T003) → enrichment (T005–T008) and backfill (T009–T011; needs T003+T006) → resolution (T012–T013; needs DEV-544) → scoring (T014–T016 + T014a; needs DEV-543) with T015a snapshot acquisition off the implementation path → home-library operator evidence (T019 after T011) → DEV-545 evidence/defaults (T017–T018; needs DEV-545 harness) → gates/review/ship (T020–T022; Phase 5 order rebase → gates → ship review → PR → merge-bar).
- T004 proceeds in parallel with T002 after T001; T003 follows both T002 and T004 (model before migration generation/verification).
- T014 may start after pickup once options files are free; T014a lands in the introducing delivery PR; T015 requires T012, T014, and DEV-543; T015a may run after T001 in parallel with T002–T015 and must complete before T016.
- DEV-543 readiness gates T015 only; DEV-544 readiness gates T012 only; DEV-545 harness readiness gates T017 only (distinct from T015a pre-change snapshot acquisition). A pending measurement harness is not an unconditional prerequisite for schema or enrichment.
- Brief sequencing preserved: Gate 1 → schema/domain → enrichment + operator (migration on home library before operator; T019 follows T011) → feature-load → scoring → DEV-545 tuning/defaults → Size L verification and Phase 5 ship.
- US1 and US2 can overlap after the schema/enrichment client exists; US3 needs persisted pairs + DEV-544; US4 needs resolved links + DEV-543; US5 needs scoring green + home-library counts + DEV-545.

## Parallel opportunities

- After T001: T002 ∥ T004; T014 may draft against options files once unblocked; T015a snapshot acquisition/verification may proceed in parallel with schema/enrichment/scoring.
- After T006/T007: T008 tests ∥ T009 operator wiring (disjoint files).
- After T012/T014: unit pin scaffolding for T016 can start while T015 lands, then assert against the integrated scorer once T015a snapshots are pinned.
- T014a may proceed in parallel with scoring work that uses disjoint files.

## Implementation strategy

1. Gate 1 + pickup (inventory only; dependency readiness recorded for consumers); start T015a snapshot acquisition off the implementation path.
2. Schema + domain + enrichment (US1 MVP data path); T003 after T002+T004.
3. Backfill (US2) through T011; apply migration and capture home-library operator counts including IMDb eligibility (T019) before enabled measurement; put counts on the delivery PR.
4. Feature-load links (US3) when DEV-544 is ready.
5. Scoring/parity + VIII glossary (US4) when DEV-543 is ready; T014a documentation updates in the introducing delivery PR; T016 after T015+T015a.
6. DEV-545 evidence and defaults including conditional Crowd-active bar (US5) when DEV-545 harness is ready (T017); put evidence on the delivery PR.
7. Size L gates and pinned review dispositions; then Phase 5 (task-pipeline 127–140): rebase → Phase 3 gates on rebased head → Size L ship review → open delivery PR → verify findings/gitleaks/mergeability → merge-bar receipt; await owner merge.

## Notes

- `[P]` = disjoint files, prerequisites complete.
- Gate 1 and runtime acceptance require their own receipts.
- T009 pins the operator invocation for delivery evidence and supersedes the drafting quickstart placeholder.
- Use existing test infrastructure and repository comment conventions for all tests.
