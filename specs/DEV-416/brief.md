# DEV-416 — Closed decision brief

Status: Patron-ratified grill closed at Q12 of 12. This is the decision input for Quill, not a spec, plan, tasks list, plan-challenge clearance or Gate 1 approval.

Worktree: F:/Dev/LamuFlix.worktrees/feature-416-spec
Branch: feature/416-spec
Phase A base HEAD: 7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947
Size: M; no ui tag (task note DEV-416:5-7).
Decision transcript: CONCLUSIONS.md, Q1-Q12. Technical facts: shared note recon-DEV-416 only. Ticket authority: task note DEV-416:30-41.

## Closing bar and frozen scope — Q1

Critical, High and Medium findings block closure. Low findings are recorded without blocking. Maximum two review rounds and two fix commits per round; third-round findings go to user review with the PR, and a third fix commit means the finding or plan was wrong (task-pipeline:28-29).

Frozen scope: deterministic MMR diversification of the existing blended recommendation ranking using RecommendationOptions.MmrLambda (default 0.7), the DEV-338 SimilarityScorer, movie ID tie-break, min(Take, candidate count), FsCheck properties and the sequel-chain fixture tests; anything else is a follow-up issue, not a finding in this round.

Follow-up recording follows lamuflix-team-charter:38, including eligibility and existing-ticket checks. Basis: DEV-416:30-41; PRODUCT.md:23-24; Q1.

## Approach and scoring contract — Q2

Run MMR over the entire eligible scored pool before Take, replacing only the current ranking/truncation stage. Relevance is the original blended Result.Score. At each iteration maximize:

lambda * relevance - (1 - lambda) * maximum similarity to already-selected candidates

Redundancy is zero before the first selection. Pairwise similarity is SimilarityScorer.Score(candidateFeatures, selectedFeatures, options.Weights, genreWeights).Score, with MovieFeaturesMapper.From supplying candidate features. Reuse DEV-338 missing-feature and zero-weight behavior. There is no embedding term in pairwise redundancy, hard franchise quota or forced outsider insertion. Exact equal objectives tie by ascending Movie.Id.Value, without epsilon or rounding.

Return selection order, mapping IDs to original Movie, blended Score and caller-safe Reasons. The marginal objective controls ordering only and never replaces Score or Reasons. RecommendedMovie and OpenAPI field shapes stay unchanged.

Basis: DEV-416:33-35; recon-DEV-416:15,22,73-101,125-145; Q2.

## Selector contract and input domain — Q3

Use a pure static MmrSelector and a sealed record MmrCandidate(MovieId MovieId, double Relevance, MovieFeatures Features) in the existing Core Features/Recommendations folder. Select accepts the complete candidate list, Take, lambda, SimilarityWeights and GenreWeights and returns ordered MovieIds. The handler maps IDs back to original scored items. No interface, DI service, repository, architectural layer or dependency is added.

Validate the full input before empty-result shortcuts. Reject null list/items/features/weight inputs, duplicate MovieIds, negative Take, non-finite lambda or lambda outside inclusive [0,1], and negative or non-finite relevance with argument exceptions. Relevance has no upper bound. Reuse DEV-338 weight validation rather than adding a different policy. Valid empty pools and Take=0 return empty; oversized Take returns the entire pool, with count always min(Take, count). Duplicate IDs are rejected rather than silently deduplicated.

Basis: DEV-416:33-36,40; recon-DEV-416:73-81,92,100,108-112,137-150; Patron cites PRODUCT.md:18,43-44 and constitution.md:438-441,463-464; Q3.

## Invocation-local selection state — Q4

Project each scored candidate once per handler call using MovieFeaturesMapper.From and retain its features for that invocation. Keep one numeric maximum redundancy per unselected candidate, initially zero. After selecting an item, if further selections remain, score each remaining candidate against that newly selected item and update its running maximum. Use the maximum over all prior selections, never only the latest similarity. Discard pairwise Reasons and preserve original handler payloads.

For n candidates and k=min(Take,n), pairwise calls are at most sum over j=1..k-1 of (n-j), empty for k=0 or 1. Work is O(n*k), excluding scorer internals; auxiliary state is O(n). This is an algorithmic bound, not measured performance. Recompute per request: no full pairwise matrix, cross-request/static mutable cache, TTL, invalidation policy, dependency or persistence.

Basis: DEV-416:33-35; recon-DEV-416:73-84,100-101,139-145; AGENTS.md static-initialization mutation rule; PRODUCT.md:18,43; Q4.

## Sequel-chain acceptance fixture — Q5

Use a deterministic in-memory selector fixture with 11 unique candidates, Take=10 and lambda=0.7. Six sequels have relevances 0.90, 0.89, 0.88, 0.87, 0.86 and 0.85 and share a nonempty genre set. Five outsiders have relevance 0.80 each and distinct, mutually disjoint nonempty genre sets, also disjoint from the chain. Use genre-only SimilarityWeights, other weights zero, and valid uniform GenreWeights. Real DEV-338 similarity is 1 within the chain and 0 otherwise. Franchise membership is a test-only ID grouping, with no production metadata field.

Assert the relevance baseline starts with five sequels. Expected MMR order: 0.90 sequel; all five outsiders in ascending MovieId order; 0.89, 0.88, 0.87 and 0.86 sequels. At selection two, outsiders score 0.56 versus 0.323 for the next sequel. Assert ten unique results, at least one outsider in the top ten, and first five not all sequels. The baseline and stronger first-five assertion discriminate diversification, since top-ten outsider presence alone also passes relevance ranking.

Use the real scorer, not a mock. This synthetic fixture asserts neither empirical franchise detection nor a universal diversity guarantee. Basis: DEV-416:30,33-41; recon-DEV-416:63,78-90; Q2-Q5.

## Remaining test strategy — Q6

Focused selector tests cover every Q3 invalid category before shortcuts; valid empty/zero/oversized Take; relevance above 1; lambda endpoints; exact-ID ties across permutations; real DEV-338 missing-feature/zero-weight behavior; and a fixture with at least three selections where an earlier selected item supplies the decisive maximum. Lambda=1 orders by relevance descending then ID. Lambda=0 begins with smallest ID and subsequently minimizes maximum redundancy with ID ties.

Bounded FsCheck valid inputs use unique IDs, finite nonnegative relevance, valid features/weights/lambda and nonnegative Take. Properties cover count=min(Take,count), unique input-subset IDs, repeated-call determinism, permutation invariance including all-equal objectives, and equivalence to an independent test-only full-rescan reference using the real scorer. The reference recomputes the maximum over all selected items and does not copy production incremental state.

Handler tests use a pool larger than Take with a diversifying item beyond the relevance-only cutoff, prove configured MmrLambda sensitivity, and preserve original Movie/Score/Reasons. Update existing ordering expectations only where they conflict with ratified MMR. Reuse existing unit/property conventions; no dependency, database fixture or scorer mock. No benchmark, instrumentation seam or exact scorer-call-count assertion is required.

Basis: DEV-416:33-41; recon-DEV-416:32-39,73-101; constitution.md:320-321 and PRODUCT.md:43 as cited by Patron; Q6.

## Files touched in future implementation — Q7

Production, under src/LamuFlix.Core/Features/Recommendations/:

- Add MmrCandidate.cs.
- Add MmrSelector.cs.
- Narrowly modify GetRecommendationsQueryHandler.cs.

Tests, under tests/LamuFlix.UnitTests/Features/Recommendations/:

- Add MmrSelectorTests.cs.
- Add MmrSelectorPropertyTests.cs.
- Add MmrSelectorSequelChainTests.cs.
- Modify GetRecommendationsQueryHandlerTests.cs only as required for the ratified coverage/order.
- Modify GetRecommendationsQueryHandlerPropertyTests.cs only as required for the ratified coverage/order.

Keep fixture builders and full-rescan reference test-local. Existing RecommendationOptions, MovieFeaturesMapper, SimilarityScorer and BlendScorer remain unchanged. No DTO, endpoint, OpenAPI/generated TypeScript, web, schema/persistence, package/project/harness, playback, unrelated-file or CONTEXT.md edits; no deletions or whole-file rewrites. Phase A decision records and Quill planning artifacts are separate from this eight-file implementation inventory.

Basis: DEV-416:33-41; recon-DEV-416:9-22,32-39,55-57,97-103,155; charter:24; PRODUCT.md:41-48; constitution.md:311; Q7.

## Future gate expectations — Q8

Gauge runs the Phase B gates after implementation/refactor at the exact implementation head and fixed diff base, and reruns gates on the rebased head:

- Roslyn analyzers.
- Cyclomatic complexity: <=15 build, <=6 refactor.
- JetBrains InspectCode in changed scope, without -All.
- Mutation via run-mutation.ps1 with since disabled.
- Property tests.
- Vulnerable packages.
- dotnet format --verify-no-changes.
- dotnet test.
- Web gates, with actual scope-empty result expected because web is untouched.

Thresholds remain harness.yml/script-owned; never lower them or edit generated configuration. Mutation and property tests must execute and exit 0 for this change: no property opt-out or silent waiver. Surviving mutants route to missing-test remediation. Gauge owns supported arguments, baseline evidence and exact-head receipts.

Exit 1 or Could not run blocks. Scope-empty exit 2 is non-blocking SKIPPED (scope-empty), never PASS. Harness SKIP and script-issued mutation N/A stay separately labeled and never count as executed mutation proof. Do not pre-label an unrun web gate.

Basis: DEV-416:36-41; task-pipeline:98-108,128,133; AGENTS.md; charter:73-83; Q8. No gates have run during this grill.

## Task-ordering constraints — Q9

1. Complete remaining Phase A drafting, read-only analysis, challenge and freeze, then owner Gate 1 merge. Perform Phase B pickup drift check before implementation.
2. Establish Q3 candidate/selector contract and Q3/Q6 validation/boundary tests in Q7 files; implement validation and valid empty/zero/oversized-Take behavior.
3. Implement deterministic Q2 MMR and Q4 incremental maxima alongside exact ties, lambda endpoints, missing-feature/zero-weight and earlier-selected-maximum tests.
4. Complete bounded FsCheck/reference equivalence and the Q5 sequel fixture. Both selector proof obligations must pass before handler wiring. Their construction may proceed independently once the selector contract is stable.
5. Integrate over the full scored pool before Take with configured lambda and original-payload mapping. Add handler coverage and necessary ordering expectation updates. Selector-only evidence does not complete integration.
6. Refactor within Q7 files to complexity <=6 and rerun affected tests; hand exact head/base to Gauge for Q8 gates. Remediate missing tests for surviving mutants and repeat required verification after fixes and on the rebased head.

Tests may be developed alongside implementation; no separate mandatory red-first policy is introduced by this ruling. Basis: DEV-416:33-41; recon-DEV-416:15,97-101,139-145; task-pipeline:65,78-108,127-128; Q2-Q9.

## Ownership and call isolation — Q10

Treat the caller candidate list, candidate items, features and both weight inputs as read-only: do not reorder, remove or mutate them. Selection bookkeeping and output sequence are invocation-owned. A retained earlier result stays unchanged after later calls with different valid lambda or Take.

Callers keep inputs stable during each call. No locking, concurrent-mutation guarantee, defensive deep-copy policy, collection dependency or extra interface. Add focused checks in MmrSelectorTests.cs for unchanged input order/content and feature/weight values, plus interleaved calls over stable inputs and retained prior output against expected ordering or the full-rescan reference. No files or gates are added.

Basis: DEV-416:33-41; recon-DEV-416:78-81,97-103; constitution.md:438-441; PRODUCT.md:18,43; Q2-Q4,Q6-Q10.

## Cross-Take consistency and mapped output order — Q11

For an identical valid pool, stable features/weights and lambda, with 0 <= a <= b, Select(pool,a) equals the first min(a,pool.Count) IDs of Select(pool,b). Take only stops iterations: no pool pre-truncation, Take-based normalization or post-sorting. This does not assert identical acquisition pools across separate requests.

The handler retains selector order even when original blended Scores are not descending. Add a bounded FsCheck prefix property including zero/oversized Takes in MmrSelectorPropertyTests.cs and an explicit non-monotonic-score order assertion in the Q6 handler diversification test. Reuse approved fixtures/files; no added API, file, dependency or gate.

Basis: DEV-416:33-41; recon-DEV-416:15,97-103,139-145; Q2,Q4,Q6-Q11.

## Final closure and handoff — Q12

Patron approves freezing Q1-Q11 as the complete brief with no new algorithm, test, file or scope obligations. The grill is closed at Q12: no Q13 and no assumed decisions. Patron rules zero ticket contradictions and zero constitution departures; section 2.3 items are adjudicated within existing boundaries. No structural escalation or owner checkbox is triggered by the grill.

Basis: DEV-416:30-41; recon-DEV-416:73-103; task-pipeline:16,27-29,43-48,69-83; Patron cites PRODUCT.md:36-48, charter:16-28 and constitution.md:127-153,438-475.

Quill next drafts spec.md, plan.md and tasks.md from this brief, recon and PRODUCT. A gap returns as needs decision rather than an invented decision. Keel then runs read-only speckit-analyze and checks plan/tasks against this brief. Analysis, plan challenge, plan freeze, gate1: provisional, spec PR, owner Gate 1 merge and Phase B pickup/implementation remain distinct subsequent steps. No production edits, tests, gates, commits, tracker mutation or implementation authorization are claimed by this brief. Q1-Q12 in CONCLUSIONS.md remain the full exchange and rationale record.


---

## Phase 2 plan challenge amendment A1 — Patron-ratified clarification

Reviewed base: feature/416-spec at 7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947. All three axis reports were available: findings-DEV-416-Sentry, findings-DEV-416-Ledger and findings-DEV-416-Compass. This is challenge adjudication after the closed Q1–Q12 grill, not Q13, plan freeze or Gate 1.

Patron ruled on Compass L4 using recon-DEV-416-2:9-31: SimilarityScorer.ValidateWeights is private static, and the existing TB002 does not name its accessibility. Q3 reuse means the identical validation policy, implemented privately in MmrSelector.cs before empty shortcuts. Check all seven SimilarityWeights properties (Director, Writers, Cast, Genres, YearProximity, RuntimeProximity, RatingBoost); reject each value when < 0.0 or !double.IsFinite with ArgumentOutOfRangeException. No scorer visibility edit, reflection or dummy Score call solely for validation. SimilarityScorer remains unchanged within Q7. This settles implementation of the existing policy without expanding ticket delivery, dependencies or the eight-file inventory.

Carry this clarification into plan.md and tasks.md and, where needed, spec.md FR-004. Schedule combined invalid-weight + valid-empty-pool / zero-Take cases in MmrSelectorTests.cs to observe validation before shortcuts. TB002 must explicitly recheck DEV-338 weight policy and helper accessibility at Phase B pickup; the present fact is settled now, not deferred to unspecified drift recon. Basis: recon-DEV-416-2:9-31; Q3, Q6–Q7; Patron challenge ruling.

Patron also confirmed Sentry L2: preserve Q3 argument-exception rejection of duplicate MovieIds and negative Take at the handler seam. Add no handler catch/fallback, silent deduplication or HTTP/status contract. Movie primary-key evidence remains unverified (recon-DEV-416:108-118); no production duplicate occurrence or endpoint outcome is inferred. Take only stops selection iterations, with no pool pre-truncation or Take-based normalization (Q11). Basis: Q3, Q7, Q11; recon-DEV-416:108-118; Patron challenge ruling.

Sentry M1–M3 are accepted Medium plan-clarity findings: explicitly retain cumulative max(old,new) over every prior selection; exact-equality ties by ascending MovieId.Value without epsilon/rounding; and invocation-owned state with no static mutable cache or pairwise matrix. They remain unresolved until Quill applies the numbered fix list and Keel verifies it. Ledger has no findings. All seven Low findings are recorded without blocking under Q1. No ticket contradiction, constitution departure, owner checkbox, production edit, gate execution, freeze or Gate 1 claim is introduced by this clarification.
---

## Phase A Keel package freeze — 2026-10-24

Disposition: FROZEN for the separate spec PR / owner Gate 1 process. This entry supersedes earlier pending-challenge, pending-remediation and pending-freeze statements as historical records, without changing Q1-Q12 or Patron amendment A1.

Identity: F:/Dev/LamuFlix.worktrees/feature-416-spec; branch feature/416-spec; base HEAD 7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947. The package is untracked working-file content at this base, not content committed at HEAD.

Exact frozen artifact set (paths relative to the worktree):
- specs/DEV-416/brief.md — Q1-Q12 decision brief, A1 and this freeze.
- specs/DEV-416/CONCLUSIONS.md — full Patron exchanges, challenge adjudication and this freeze.
- specs/DEV-416/spec.md — requirements and acceptance scenarios including FR-004/A1.
- specs/DEV-416/plan.md — bounded design and eight-file implementation inventory.
- specs/DEV-416/tasks.md — TB001-TB004 and T001-T019 with dependency order and future verification.

DRAFTING_RECEIPT.md is historical drafting provenance, outside the frozen contract set. No design.md, ADR or ASSUMPTIONS.md is required by this M-path, non-ui package.

Closure basis: task note DEV-416:73-74 records Quill propagation and Keel CLEAN read-only analysis after fixes, with 19/19 requirements mapped (13 FR and six buildable SC), zero new Critical/High/Medium findings, no new ambiguities or constitution issues. Current plan Design Overview items 1-2 and tasks T008 carry cumulative max(previous maximum,new pair similarity) across all prior picks, exact MovieId.Value ties without epsilon/rounding, and invocation-owned O(n) state without static mutable/cross-request state or pairwise matrix. Thus Sentry M1-M3 are CLOSED. A1 is propagated in spec FR-004, plan item 1, T001/T004 and TB002: identical private seven-weight validation before shortcuts, combined invalid-weight/empty-or-zero tests and explicit policy/accessibility drift recon. Compass L4 propagation is complete. Ledger has no findings. All three challenge axes are accounted for in adjudication-DEV-416-Keel; Q1 closing bar is met for Phase A documents.

Remaining limitations: all seven prior Low findings stay recorded and non-blocking, including objective-arithmetic documentation/test traceability and advisory production-type/stub wording. The synthetic sequel fixture is not a universal diversity or empirical franchise-detection guarantee; O(n*k)/O(n) are algorithmic bounds, not measured performance. Movie primary-key configuration remains unverified per recon-DEV-416:108-118; no duplicate occurrence or HTTP outcome is inferred. Facts remain bounded to recon-DEV-416 and recon-DEV-416-2. No additional recon is needed for this document freeze.

No gate1: provisional status is set by this freeze. The pre-existing spec.md Draft — gate1: provisional header is drafting text, not an operative gate receipt; this later explicit disposition governs this freeze. Provisional handling, YouTrack mutation, commit, spec PR and owner Gate 1 merge remain separate. No production changes, tests, gates or implementation authorization are claimed. Phase B still requires TB001-TB004, including fresh drift recon/check and Gauge baseline evidence before T001. Q1 caps and the exact eight-file future implementation boundary remain unchanged.

Freeze byte identity: SHA256 and byte-length manifest for the final five artifacts is recorded in shared note adjudication-DEV-416-Keel after append/read-back. Any later contract edit requires explicit adjudication and a refreshed manifest; a later commit is a separate receipt.