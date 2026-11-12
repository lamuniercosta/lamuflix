# DEV-483 — Movie details and recommendation endpoint latency

Status: PLAN FROZEN — 2026-11-12, Keel Phase 2 step 6.2. Round 1 adjudication and independent repair reread complete; reviewer-author attestations settle pre-adjudication draft provenance. Gate 1 approval, implementation completion, runtime acceptance and merge clearance remain separate and open.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-483-spec`
Branch: `feature/483-spec`
Recon reference head: `73f3f74ac2049be1ca4c685472a2b4df42c237f0`
Size/type: M bug, parent DEV-491 (note DEV-483).

## Authority and evidence limits

Facts come exclusively from `recon-DEV-483` and intake note `DEV-483`; the full authoritative ticket is reproduced at recon lines 34–49. Decisions are Patron rulings Q1–Q6, recorded verbatim below and in `CONCLUSIONS.md`. Citations to product/charter/pipeline within the transcript are Patron's cited basis, not additional reconnaissance by Keel.

Ticket delivery: find the cause, document it in the YouTrack ticket, and demonstrate details under 300 ms and both recommendation endpoints under 1 s on a 17-movie Compose stack. Ticket observations used Invoke-WebRequest through web port 3000 with idle containers: details 2.2–3.3 s (404 id 9999: 2.1 s), by-movie recommendations 7.2–8.4 s, for-you initially 0.75 s and later a 30-second timeout. These are historical ticket measurements, not newly reproduced results. The cause is not yet established.

Recon recorded a pre-existing `harness.yml` modification, not opened or changed. Preserve unrelated changes. Baseline Roslyn and complexity were `SKIPPED (scope-empty)`, not PASS. Recon did not run tests.

## Frozen scope and loop discipline

Approved targets:

- `GET /api/movies/{id}` including known-ID success and missing ID 9999 returning 404.
- `GET /api/movies/{id}/recommendations` with a known seed and eligible results.
- `GET /api/recommendations/for-you?take=10` with the applicable history/candidate fixture.

Delivery includes diagnosis, the minimum proven causal fix, behavioral regressions, reproducible before/after timings and verified ticket cause documentation. Preserve routes, DTO field sets, HTTP status semantics, cancellation, ranking/filtering and history behavior. Shared collaborators belong in scope only when proven causal. Genres and movie list are diagnostic controls only. Patron Q1 distinguishes follow-up handling: out-of-scope Critical/High or broken behavior goes through Patron/Rigger for a follow-up ticket; other observations are recorded as noted, no ticket. Frozen scope ends: anything else is a follow-up issue, not a finding in this round.

Closing bar: every verified in-scope correctness, security, contract or ticket-acceptance violation blocks, at every severity including Low, until fixed or proven invalid. Other accepted non-blocking Low items require an explicit recorded disposition. Require all three Sentry/Risk, Ledger/Standards and Compass/Spec reports pinned to the reviewed head for this Size M task; a missing axis blocks adjudication. Review rounds must be published on the PR and fixes linked to findings.

Round cap: at most two plan-challenge rounds and two implementation review/remediation rounds, at most two fix commits per round, one consolidated numbered fix list per round. Unresolved blockers at the cap remain open and stop for a ruling; the cap does not waive them. Grill cap was 12 questions; closed at 6.

Completion requirements remain separate: mandatory gates, the Q2 Compose receipt meeting all targets, and Rigger's verified read-back of cause documentation on DEV-483. Failed or Could not run mandatory gates stay open. SKIPPED, SKIP and N/A never become PASS. User owns all merges; never merge or enable auto-merge.

## Approach and permitted file boundary

Diagnose before choosing or implementing a production fix. Reproduce Q2, isolate web-proxy time versus API time, then handler/persistence/query/cache/external-wait time using existing diagnostic facilities. Record an evidence-backed causal chain. If the cause cannot be established, stop with `needs recon`; do not select a speculative optimization.

Patron Q3 permits only the minimal proven causal subset of these existing production candidates:

- `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs`
- `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs`
- `src/LamuFlix.Core/Features/Library/GetMovieDetailsQueryHandler.cs`
- `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`
- `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs`
- `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs`
- `src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs`
- `src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs`
- `src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs`

Existing permitted test suites from recon lines 19–22:

- `tests/LamuFlix.UnitTests/Features/Library/GetMovieDetailsQueryHandlerTests.cs`
- `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs`
- `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs`
- `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs`

These are candidates, not a requirement to edit all of them. No new dependency, layer, schema/migration/index, public API shape, LocalPlay/secrets/Process.Start surface, frontend work, or speculative cache/algorithm/framework change is preapproved. A proven cause needing any other tracked file (including Compose/proxy configuration or startup registration) or protected surface needs a deliberate cited Patron ruling before editing. Only a ticket contradiction or necessary constitution departure becomes a structural owner checkbox; a Patron ruling cannot close one. Preserve the C# DTO → OpenAPI → generated TypeScript contract chain.

## Runtime measurement protocol — Patron Q2

Use Invoke-WebRequest through web port 3000 on the same idle 17-movie Compose dataset for before/after comparisons. Record dataset/seed/history fixture, environment, build/head, resource/container state and configuration so the observations can be reproduced. Do not assume fixture IDs or history availability beyond recon; collect missing facts through recon.

For each target and the details 404 case, capture the first request after service readiness separately, then 20 sequential requests with every individual elapsed time, HTTP status and payload validity; report median, p95 and maximum as well. Known-ID details and id 9999 returning 404 must each be strictly below 300 ms. Both recommendation routes must be strictly below 1 s, including cold first requests and re-tests. Every measured request must satisfy its limit; averages cannot hide violations.

Reproduce/exercise the diagnosed post-startup condition causing the reported for-you 30-second timeout. Repeat the 20-request batches after a settled post-startup/post-load interval of at least 60 seconds. The ticket's 30-second timeout does not establish a 30-minute idle trigger. Batch size 20 and settled interval >=60 seconds are explicit Patron-approved protocol assumptions recorded in `ASSUMPTIONS.md`.

No warmed-cache-only, synthetic or unavailable-runtime pass is allowed. Missing/failed evidence leaves runtime acceptance open (`Could not run` when unavailable). Keep a reviewable receipt with exact commands, first/batch/re-test data, validity checks, before/after comparison, diagnosed trigger, causal evidence and limitations. Route the verified cause and evidence through Patron to Rigger; only Rigger updates YouTrack and supplies verified read-back.

## Test strategy and gate expectations

Add deterministic regression tests at the actual diagnosed fault seam and preserve existing known/missing details, cancellation, recommendation ranking/filtering and for-you history semantics. Run the existing suites above; extend only cases justified by the diagnosis. Any behavior depending on real query translation or persistence serialization uses a real database through Testcontainers, never driver mocks. Machine-dependent latency assertions belong in the Compose acceptance receipt, not unit timers. No new benchmark platform is planned.

Required delivery verification includes build, Roslyn, cyclomatic complexity, JetBrains inspections, property tests, vulnerable-package/security checks, formatting, full tests, and pre-PR mutation for eligible production changes. Use repository gate configuration and supported script arguments; do not lower thresholds or edit generated configuration. Q4 explicitly names `run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1`, `run-property-tests.ps1`, `run-vulnerable-packages.ps1`, `dotnet test` and pre-PR `run-mutation.ps1`. Record exact commands, head, exits and distinct PASS/FAIL/SKIPPED (scope-empty)/SKIP/N/A/Could not run verdicts. No gates were run by this grill.

## Task ordering and handoff

1. Quill drafts spec.md, plan.md and tasks.md from this brief, the complete Q&A and authorized recon, preserving the diagnosis stop point and separate evidence requirements.
2. Keel performs read-only speckit-analyze and brief alignment; one consolidated numbered repair list per round. Decisions change in brief first, not silently in generated planning artifacts.
3. Sentry, Ledger and Compass challenge the plan within the cap; adjudicate and freeze. Patron may issue provisional Gate 1; owner merge of the spec PR is the distinct Gate 1 boundary.
4. Phase B begins with origin/main pickup drift recon/analysis and explicit task amendments if required.
5. Establish the Q2 baseline, reproduce the issue and document the causal diagnosis. Stop for needs recon if unestablished; obtain Patron rulings before any expanded file/protected change.
6. Implement the smallest causal fix with Q4 regression coverage, preserving semantics.
7. Run mandatory delivery gates and Q2 after measurements, including the delayed-timeout trigger and settled batches. Keep missing evidence visibly open.
8. Patron routes the verified diagnosis to Rigger for YouTrack documentation and verified read-back; this is required by Done when.
9. Complete three-axis implementation review, adjudication, remediation within caps, exact-head verification and PR publication/read-back. Finish at awaiting-merge for the user; do not merge.

No new domain glossary concept or ADR is needed for this reversible Size M bug diagnosis (Q6). Existing canonical vocabulary and ADRs continue to govern Quill's drafting. No open decisions or structural escalations remain from this grill. Runtime cause, fix selection, regression specifics and acceptance evidence are intentionally future evidence work, not established facts.

## Complete grill exchanges and Patron rulings

The following is a verbatim copy of the append-only conclusions at grill closure, preserving all six questions, answers, cited rationale and implications.

# DEV-483 Grill conclusions

Worktree: `F:/Dev/LamuFlix.worktrees/feature-483-spec`; branch: `feature/483-spec`.
Authority: `recon-DEV-483`, authoritative ticket record (`DEV-483`), `specs/PRODUCT.md`, constitution, `task-pipeline`, and `lamuflix-team-charter`.

---

## Q1 — Frozen delivery scope

Keel: DEV-483 grill Q1/12: Authoritative ticket is in recon-DEV-483 lines 34-49: diagnose cause, document it in the ticket, details <300ms and both recommendation endpoints <1s on a 17-movie Compose stack, measured through web port 3000. I recommend freezing delivery to these three existing endpoint paths and their causal shared collaborators, preserving response semantics and recommendation behavior; comparative endpoints are diagnostic controls only. Anything else is a follow-up issue, not a finding in this round. This prevents speculative optimization while allowing a common infrastructure cause. Do you approve this scope? Cite your basis and flag any structural escalation. Please answer this decision only; we will cover measurement and loop terms next.

Patron (complete answer):
Ruling: Yes — approve freezing DEV-483 delivery scope to the three existing target endpoint paths (`GET /api/movies/{id}`, `GET /api/movies/{id}/recommendations`, `GET /api/recommendations/for-you`) and their causal shared collaborators, preserving existing response semantics and recommendation behavior, with comparative endpoints (`GET /api/genres`, movie list) treated strictly as diagnostic controls and zero §2.3(a)/(b) structural escalation.

- **Cited basis (`recon-DEV-483` lines 11–16, 41–48; `specs/PRODUCT.md` §3, §5)**: The authoritative ticket (`Problem`, `Wanted`, `Done when` at `recon-DEV-483` lines 42–48) scopes delivery strictly to diagnosing and documenting the cause of slowness and achieving `< 300 ms` for movie details (`LibraryEndpoints.cs:54-55` -> `GetMovieDetailsQueryHandler` -> `EfMovieCatalog`) and `< 1 s` for both recommendation endpoints (`RecommendationEndpoints.cs:28-36` -> `GetRecommendationsQueryHandler` / `GetForYouRecommendationsQueryHandler` -> `EfRecommendationCandidateSource` / `EfRecommendationProfileSource` / `HybridCachedRecommendationProfileSource`) on the 17-movie Compose stack measured through web port 3000, while citing `GET /api/genres` and the movie list solely as comparison baselines.
- **Contract & behavior preservation (`recon-DEV-483` lines 25–29; `specs/PRODUCT.md` §2, §5)**: Confining work to the three target endpoint paths and their proven causal shared collaborators while preserving public routes, DTO schemas, HTTP status codes (including 404 handling), and recommendation ranking/filtering semantics prevents speculative optimization (`specs/PRODUCT.md` §2) and preserves the API contract chain.
- **Out-of-scope boundary & escalation check (`lamuflix-team-charter` §2.3)**: Any non-causal observation outside these three endpoint paths is out of scope for this round (Critical/High or broken behaviour becomes a follow-up ticket via Rigger; anything else is `noted, no ticket` in `CONCLUSIONS.md`), and zero ticket contradiction or constitution departure is present.

---

## Q2 — Measurement acceptance protocol

Keel: DEV-483 grill Q2/12: Approve this measurement acceptance protocol? Recommend before/after wall-clock HTTP measurements with Invoke-WebRequest through web port 3000 on the same idle 17-movie Compose dataset and recorded resource/build/configuration conditions. Record first request after service readiness separately, then 20 sequential requests per target with every sample, median, p95, max, status and payload validity; repeat the 20-request batch after a documented 30-minute idle interval to exercise the reported delayed timeout. Require every measured successful target request below its ticket limit, including the first and post-idle requests; use known details ID and recommendation seeds with eligible results plus for-you take=10. Measure missing details id 9999 as a separate diagnostic and preserve 404, without extending the success timing target to errors. Failing or unavailable runtime evidence leaves acceptance open; no warmed-cache-only claim. Cost: conservative repeatable acceptance takes at least 30 minutes, with thresholds unchanged. Basis: ticket Problem/Wanted/Done when, recon lines 42-48. Amend if necessary, with cited rationale; please append full question and answer to CONCLUSIONS.md before replying.

Patron (complete answer):
Ruling: Approve with two ticket-grounded amendments — (1) require `GET /api/movies/9999` (`404`) to also meet `< 300 ms`, and (2) replace the hardcoded 30-minute idle wait with reproducing/exercising the diagnosed post-startup trigger for the `for-you?take=10` 30-second request timeout plus a settled post-startup/post-batch re-test (`>= 60 s` `[assumed]`).

- **Ticket-faithful measurement & `404` threshold (`recon-DEV-483` lines 42–48; `specs/PRODUCT.md` §3)**: Use `Invoke-WebRequest` through the web container on port 3000 against the idle 17-movie Compose stack with recorded container/resource/build/config state, capturing the first (cold) request after service readiness separately followed by 20 sequential requests per target (`[assumed]` batch size) with all individual samples, median, p95, max, HTTP status, and payload validity; because `recon-DEV-483` line 42 explicitly cites `(2.1 s for a 404 on id 9999)` under `GET /api/movies/{id}`, both known-ID (`200`) and missing-ID `9999` (`404`) movie details requests must be `< 300 ms`, and both recommendation endpoints (known seed with eligible results and `GET /api/recommendations/for-you?take=10`) must be `< 1 s` on every measured request (including first/cold).
- **Post-startup timeout reproduction vs. 30-minute sleep (`recon-DEV-483` line 42)**: The ticket statement `took 0.75 s soon after startup and later timed out at 30 s` records a 30-second HTTP timeout occurring after initial startup, not a 30-minute idle timer; therefore, acceptance must reproduce/exercise the diagnosed post-startup condition that caused the 30 s timeout and repeat the 20-request batch after a settled post-startup/post-load interval (`>= 60 s` `[assumed]`) rather than mandating an arbitrary 30-minute wall-clock sleep.
- **No warmed-cache-only or synthetic pass (`task-pipeline` Phase 2/Phase 4; `lamuflix-team-charter` §2.3)**: Cold first-request, sequential 20-request batch, and settled post-startup re-test must all meet the ticket limits; missing or failing runtime evidence is `Could not run` / open acceptance, never a pass.

---

## Q3 — Diagnosis-first implementation plan and §2.3 file/care boundary

Keel: DEV-483 grill Q3/12: Approve a diagnosis-first implementation plan with an explicit stop point? Recommend first reproduce with the Q2 protocol, isolate web-proxy versus API time and handler/persistence/query/cache/external-wait time using existing diagnostic facilities, then document the evidenced causal chain before choosing the smallest fix. Named candidate production seams from recon lines 11-16 are LibraryEndpoints.cs, RecommendationEndpoints.cs, GetMovieDetailsQueryHandler.cs, EfMovieCatalog.cs, GetRecommendationsQueryHandler.cs, GetForYouRecommendationsQueryHandler.cs, EfRecommendationCandidateSource.cs, EfRecommendationProfileSource.cs and HybridCachedRecommendationProfileSource.cs. These are investigation candidates, not an instruction to edit all of them. No new dependencies, layers, schema or public contracts are preapproved; no speculative cache, index, algorithm or framework switch. If a causal fix needs another file or any protected change, get a deliberate cited Patron ruling before editing, and use the two structural escalations only as defined in section 2.3. If diagnosis cannot establish the cause, stop as needs recon rather than implement guesses. Cost: implementation remains conditional until evidence exists. Please append full Q&A with rationale and implications before replying.

Patron (complete answer):
Ruling: Yes — approve the diagnosis-first implementation plan, the explicit evidence stop point (`needs recon` if the root cause is not reproduced/established), and the candidate §2.3 item 6 file boundary (`LibraryEndpoints.cs`, `RecommendationEndpoints.cs`, `GetMovieDetailsQueryHandler.cs`, `EfMovieCatalog.cs`, `GetRecommendationsQueryHandler.cs`, `GetForYouRecommendationsQueryHandler.cs`, `EfRecommendationCandidateSource.cs`, `EfRecommendationProfileSource.cs`, `HybridCachedRecommendationProfileSource.cs`, and their existing unit/integration test suites in `recon-DEV-483` lines 19–22).

- **Cited basis (`recon-DEV-483` lines 11–22, 42–48; `specs/PRODUCT.md` §2, §3, §5)**: The ticket (`Wanted` and `Done when` at `recon-DEV-483` lines 42–47) explicitly requires finding and documenting the cause of slowness before claiming completion; reproducing via the Q2 protocol and isolating web-container proxy time (port 3000) vs. API/handler/EF/cache/external-wait time ensures only the proven bottleneck in the candidate seams (`recon-DEV-483` lines 11–16) is modified rather than applying speculative optimizations (`specs/PRODUCT.md` §2).
- **§2.3 care-list boundary (`recon-DEV-483` lines 25–29; `lamuflix-team-charter` §2.3 items 1–6)**: Only the minimal subset of the nine candidate production files (`recon-DEV-483` lines 11–16) and their existing test suites (`recon-DEV-483` lines 19–22) proven causal by diagnosis may be edited; no new NuGet/npm dependency, architectural layer, database schema/migration/index, public API contract change, or `Features:LocalPlay`/secrets/`Process.Start` change is preapproved.
- **Implications (`lamuflix-team-charter` §2.3, §3.1)**: Phase B must complete and record the root-cause diagnosis before applying production code edits (and route the verified cause to Rigger through Patron to document in the YouTrack ticket per `Done when`); if diagnosis cannot establish the cause, stop as `needs recon`, and if the proven cause requires editing any tracked file outside the candidate set or touching any §2.3 item 1–6 surface (such as Compose/proxy config, DB indexes/migrations, or startup registration), request a deliberate cited Patron ruling before editing (`blocked: structural` applies only to a ticket contradiction or constitution departure).

---

## Q4 — Regression testing and verification gate strategy

Keel: DEV-483 grill Q4/12: Approve this regression and verification strategy? Recommend causal regression tests at the actual fault seam and existing details/recommendation handler and HTTP tests from recon lines 18-23 to preserve success, 404, cancellation, ranking/filtering and for-you history behavior. Query translation or persistence behavior uses real database/Testcontainers, never driver mocks; add only the cases justified by the diagnosis. Keep machine-dependent latency assertions in the Compose acceptance receipt rather than flaky unit timers. Run the repository-required build/analyzer/complexity/inspection/property/security/format/test gates and pre-PR mutation for eligible production changes; preserve PASS, SKIPPED, N/A and Could not run separately. Capture exact commands, head, statuses and limitations. No frontend work is preapproved; route/DTO/type contract chain stays intact. Cost: meaningful targeted tests plus mandatory delivery gates, not a new benchmark platform. Please append full Q&A with cited basis, rationale and implications before replying.

Patron (complete answer):
Ruling: Yes — approve the regression testing, Testcontainers persistence rule, Compose latency-receipt separation, and mandatory harness gate verification strategy.

- **Cited basis (`recon-DEV-483` lines 18–23, 27–29; `AGENTS.md` Non-negotiables; `specs/PRODUCT.md` §1, §5)**: Existing unit and integration suites (`GetMovieDetailsQueryHandlerTests`, `LibraryEndpointsTests`, `GetRecommendationsQueryHandlerTests`, `GetForYouRecommendationsQueryHandlerTests`, `RecommendationEndpointTests`, `ApiEndToEndRecommendationTests`) must pass and be augmented only with targeted causal regression tests at the diagnosed fault seam; any query-translation or persistence fix must be verified against a real database via Testcontainers rather than mocked drivers (`AGENTS.md` Non-negotiables).
- **Deterministic tests vs. Compose latency receipts (`recon-DEV-483` lines 42–48; `aaron-csharp-testing.mdc`)**: Automated xUnit/integration tests verify the deterministic behavioral and structural invariants of the fix (preserving 200/404, cancellation, ranking/filtering, and `for-you` history semantics without flaky wall-clock timer assertions), while the `< 300 ms` and `< 1 s` latency thresholds are verified and recorded by the Q2 Compose acceptance protocol through web port 3000.
- **Implications (`AGENTS.md` Verification commands; `lamuflix-team-charter` §2.3)**: Phase B verification must run the full .NET gate suite (`run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1`, `run-property-tests.ps1`, `run-vulnerable-packages.ps1`, `dotnet test`, and pre-PR `run-mutation.ps1`), recording exact commands, `HEAD`, exit codes, and distinct verdicts (`PASS`, `SKIPPED (scope-empty)`, `SKIP`, `N/A`, `Could not run`) without folding non-pass states into `PASS`; zero `/web` edits are preapproved and the `C# DTO -> OpenAPI -> TypeScript` contract chain stays untouched.

---

## Q5 — Closing bar, challenger coverage, and review/remediation caps

Keel: DEV-483 grill Q5/12: Approve the loop terms? Recommend at most two plan-challenge rounds and at most two implementation review/remediation rounds, with one consolidated numbered fix list per round; unresolved blocking findings after the cap stop for a ruling, never silently expand scope or declare success. For this Size M task require all three Sentry/Risk, Ledger/Standards and Compass/Spec reports at the reviewed head. Closing bar: no unresolved in-scope correctness, security, contract or ticket acceptance violation at any severity; other accepted Low findings require explicit recorded disposition, and out-of-scope observations follow Q1. Mandatory failed or Could not run gates remain open; runtime target evidence and verified cause documentation in the ticket remain separate completion requirements even if code gates pass. Freeze scope exactly as Q1, ending: anything else is a follow-up issue, not a finding in this round. Cost: Low contract violations can block, and caps stop loops without waiving findings. Please append full Q&A with cited basis, rationale and implications before replying.

Patron (complete answer):
Ruling: Yes — ratify the standing closing bar, three-challenger coverage (`Sentry`, `Ledger`, `Compass`), two-round / two-fix-commit caps, and separate completion requirements (`Done when` ticket documentation + Q2 Compose runtime receipt + code gates).

- **Cited basis (`task-pipeline` §2.2, §8; `lamuflix-team-charter` §2.3; note `DEV-483` line 4)**: Standing pipeline rules cap plan-challenge and code-review/remediation loops at 2 rounds (and at most 2 fix commits per round) with one consolidated numbered fix list per round, and require all three challengers (`Sentry` Risk, `Ledger` Standards, `Compass` Spec) at the reviewed `HEAD` for `size:M` tasks (`DEV-483` line 4).
- **Closing bar & frozen scope (`recon-DEV-483` lines 42–48; `CONCLUSIONS.md` Q1)**: Every verified in-scope correctness, security, contract, or ticket-acceptance violation at any severity (Critical through Low) blocks closure until fixed or proven invalid, while non-blocking accepted Low items require explicit recorded disposition and any observation outside the Q1 frozen scope is not a finding in this round ("anything else is a follow-up issue, not a finding in this round").
- **Implications (`recon-DEV-483` line 42 `Done when`; `AGENTS.md` Verification commands)**: Unresolved blocking findings at the 2-round cap stop for a Patron ruling / remain open on the PR for user review rather than auto-clearing; failed or `Could not run` mandatory gates remain open, and passing code gates alone never closes DEV-483 without both the Q2 17-movie Compose runtime receipt and Rigger's `(verified)` confirmation that the diagnosed cause is documented on the YouTrack ticket.

---

## Q6 — Task ordering and grill closure

Keel: DEV-483 grill Q6/12 (closure): Confirm shared understanding and this task order so I can close brief.md: Quill drafts spec/plan/tasks from the recorded Q1-Q5 rulings; Keel runs read-only analysis and brief alignment; three-axis plan challenge and freeze precede owner Gate 1. Phase B starts with pickup drift check, then Q2 baseline/reproduction and causal diagnosis; request a cited Patron ruling for any expanded file/protected surface, implement the minimal causal fix with Q4 regressions, run required gates and Q2 after-measurements including delayed-timeout trigger, then send the evidence-backed root cause to Rigger via Patron for ticket documentation and verified read-back. Three-axis implementation review, adjudication and exact-head verification/publication precede a user-owned merge; never merge or treat plan freeze as Gate 1 or runtime acceptance. Brief will preserve every full Q&A and distinguish evidence still to collect from decisions. No new glossary concept or ADR is proposed for this reversible Size M diagnosis. Please confirm closure or identify one remaining decision, append full Q&A with rationale and implications before replying.

Patron (complete answer):
Ruling: Yes — confirm shared understanding, approve the end-to-end Phase A and Phase B task ordering, confirm no new glossary entry or ADR is required for this reversible `size:M` bug diagnosis, and close the DEV-483 grill at Q6/12.

- **Cited basis (`task-pipeline` Phase 2–Phase 8; `lamuflix-team-charter` §2.3, §3.1; `recon-DEV-483` lines 42–48)**: Phase A proceeds strictly through `brief.md`, Quill drafting `spec.md`/`plan.md`/`tasks.md` from Q1–Q6 and `recon-DEV-483`, Keel read-only `/speckit-analyze` and brief alignment, three-axis plan challenge (`Sentry`, `Ledger`, `Compass`, <= 2 rounds), plan freeze, and Patron `gate1: provisional` before the user merges the spec PR (Gate 1).
- **Phase B execution & YouTrack cause documentation (`CONCLUSIONS.md` Q2–Q5; `lamuflix-team-charter` §3.1)**: Phase B begins with pickup `origin/main` drift recon, Q2 baseline reproduction and root-cause isolation across the Q3 candidate seams (stopping for `needs recon` if unestablished or requesting a cited Patron ruling before touching any file/surface outside Q3), minimal causal implementation with Q4 deterministic/Testcontainers regressions, full .NET verification gates plus Q2 post-fix Compose timing receipts (including `GET /api/movies/9999` 404 `< 300 ms` and the post-startup `for-you?take=10` 30s-timeout trigger + `>= 60 s` settled batch `< 1 s`), and routing the verified root cause through Patron to Rigger for YouTrack ticket documentation with `(verified)` read-back.
- **Implications (`specs/PRODUCT.md` §2; `lamuflix-team-charter` §2.3)**: The DEV-483 grill is closed after Q6/12 with zero open questions and zero §2.3(a)/(b) structural escalations; Keel may now write `specs/DEV-483/brief.md` preserving all complete Q1–Q6 exchanges and `specs/DEV-483/ASSUMPTIONS.md` tags, and report grill completion to Bernstein.







---

## Plan Challenge Round 1 Adjudication (21 items across Sentry, Ledger, Compass)

Keel: DEV-483 plan adjudication round 1: consolidated disposition proposal covering 21 challenger items (`Sentry` H1–H2, M1–M6, L1; `Ledger` M1–M3, L1–L4; `Compass` M1–M2, L1–L3) with zero boundary expansion, dependency, or acceptance target change.

Patron (complete ruling):
Ruling: Approve Keel's consolidated Round 1 plan-challenge disposition proposal across all 21 items (`Sentry` H1 rejected as unconditional acceptance leg and folded into T003 conditional concurrency diagnosis/re-baseline; `Sentry` H2 accepted conditionally on preserving existing cache identity/history boundaries and regression tests if cache is touched; `Sentry` M1–M6, L1, `Ledger` M1–M3, L1–L4, and `Compass` M1–M2, L1–L3 accepted as proposed), with zero §2.3 boundary expansion or structural escalation.

- **Risk / Sentry dispositions (`CONCLUSIONS.md` Q1–Q4; `recon-DEV-483` lines 11–22, 42–48)**: Rejecting `Sentry H1` as a mandatory concurrent acceptance leg while requiring `T003` to investigate concurrency if evidenced and re-baseline with any newly diagnosed trigger before code edits honors the Q2 sequential measurement protocol and Q3 diagnosis-first stop without inventing unverified triggers; conditionally accepting `Sentry H2` (preserve existing `HybridCachedRecommendationProfileSource` / history boundaries + regression if touched, without inventing multi-user semantics) and accepting `Sentry M1–M6, L1` (deterministic invariant tied to actual diagnosis rather than forced query-count cuts, cancellation/abandoned-work observation, route-specific Wisp fixture oracles, matched direct-API vs port-3000 comparison using existing diagnostics, re-baselining if trigger absent, zero build/test load during sampling windows, non-warming readiness checks, pinned/reset fixtures, and diagnostic control-batch interpretation without new thresholds) stay strictly within Q1–Q4.
- **Standards / Ledger dispositions (`CONCLUSIONS.md` Q3–Q4; `AGENTS.md` Non-negotiables & Verification commands; `lamuflix-team-charter` §2.3)**: Accepting `Ledger M1–M3, L1–L4` enforces Q2 sample integrity (excluding duplicate/overlapping samples), `AGENTS.md` non-negotiables (`TimeProvider` for injected time, existing `Activity`/logging facilities only, zero unapproved API/instrumentation expansion), Q3/Q4 boundary discipline (preserving the stop point and keeping dependency-direction/layering verification inside existing test suites), script-driven `run-mutation.ps1` and complexity/refactor gate verdicts from `harness.yml` without assumed exemptions or lowered thresholds, scrubbed untracked runtime evidence under `git rev-parse --git-path dev-483-evidence` (disposable per §2.3 and free of secrets/config dumps), and metadata refresh at plan freeze.
- **Spec / Compass dispositions (`CONCLUSIONS.md` Q5–Q6; `task-pipeline` Phase 2–Phase 4)**: Accepting `Compass M1–M2, L1–L3` enforces exact-HEAD final gate and runtime/cause receipt freshness after any remediation commit (Q5), strict task ordering where `T008` (YouTrack cause routing via Patron -> Rigger and packaging) requires `T007` post-fix Q2 runtime status and mandatory gates first (Q6), deduplicated metadata and repeated diagnostic controls (`L1–L2`), and generic Build-lane role attribution without hardcoding seat names (`L3`).

Plan repair requirements and per-finding dispositions: see PLAN_ADJUDICATION.md, Consolidated Quill fix list, items 1-10. These are the implementation details of the approved ruling and are part of this settled brief. PLAN FROZEN remains withheld pending independent repair reread and clarification of exact challenge snapshot provenance.


## Consolidated Quill fix list — round 1

Apply consistently to spec.md, plan.md, tasks.md, checklists/requirements.md and DRAFTING_RECEIPT.md as relevant. Do not edit brief.md, CONCLUSIONS.md or this adjudication; Keel owns those decisions. Do not invent facts or expand production/test boundaries. One list for this round; two-round/two-fix-commit caps remain.

1. T003: investigate concurrency when evidence indicates it; observe cancellation/abandoned work; compare matched direct-API and web-proxy requests with transport differences recorded. If diagnosed trigger was absent from T002, rerun the affected complete baseline protocol with it before T004/production edits. No unconditional concurrent acceptance leg; retain Q2 sequential acceptance.
2. T004–T006: add a deterministic performance-relevant invariant selected from actual diagnosis (query/round-trip bounds only if causal). If cached profile source is touched, prove preservation of existing cache identity/history boundaries with regression coverage; collect missing semantics through recon, never invent per-user architecture. Keep Testcontainers rule and no unit wall-clock assertions.
3. T001/T002/T007: recon supplies route-specific status/payload/fixture oracles: known details identity/content and existing contract; missing id 9999 existing 404 semantics; by-movie eligible results and preserved ranking/filtering; for-you fixture-specific history/candidate expectations, including non-empty where eligible. Define readiness observation without prewarming target; pin/reset dataset/history between comparable runs. Repeat genres/movie-list control batches before and after; record host/control noise and investigate comparability without adding numeric acceptance thresholds.
4. T003/plan: existing Activity/log facilities only, exact available facilities via recon; TimeProvider for injected time, existing comment rules, retained instrumentation justified inside approved candidate boundary and gated. Temporary instrumentation must be accounted for and removed before fix acceptance, with measured build identity recorded; any retained code is a production change. A proven outside-boundary cause is an expected STOP for cited Patron ruling, never permission to edit. Preserve Core dependency direction and layering evidence through existing tests; obtain suite facts via recon if needed.
5. Forbid build/gate/test or competing measurement/packaging load during T002/T007 sample windows. T009 and T007 must run sequentially. Remove resource-safe concurrency permission and conflicting example; controls receive the same isolation. Record contaminated runs and rerun for valid comparison rather than silently ignoring failing samples.
6. T009/plan: use actual supported scripts and configured applicable complexity/refactor obligations; no new hardcoded threshold, lower threshold or invented refactor opt-out. Mutation eligibility/exemptions and exact SKIPPED (scope-empty), SKIP, N/A, FAIL/PASS/Could not run come from actual script/config output, never assumed by project name. Do not open/change preserved harness.yml dirt. Missing configuration facts go through recon.
7. Name disposable untracked receipt directory resolved by git rev-parse --git-path dev-483-evidence. Store scrubbed commands/raw samples/provenance/summaries there, no secrets, connection strings, tokens, env dumps or raw config dumps. Record safe config identity only. This is operational evidence, not a new source folder. Publish a scrubbed reviewable receipt through team handoff; do not commit raw machine/config artifacts accidentally.
8. T009–T011: after any T010 remediation commit, rerun full mandatory gate set on final delivery/review HEAD before packaging. Explicitly annotate stale receipts; refresh affected Q2 runtime evidence and cause documentation/read-back when the change invalidates them. Never label an old-head run as final-head PASS.
9. Make T008 require T007 outcome/status recorded and mandatory gate run/status recorded first, per brief Q6. Reorder local phases/dependency map to T007 then T009 then T008, followed by T010/T011; no dependency cycle. Failed/unavailable runtime/gates remain open and cannot yield Done when, even if cause documentation is recorded. Remove early After T003/when available routing.
10. State generic Build-lane ownership for otherwise unassigned tasks. Refresh all stale drafting/analyze metadata to current challenge-repair state; only Keel declares PLAN FROZEN after independent reread. Gate 1, implementation, runtime and merge remain distinct. Update receipt/checklist with repair traceability and no new decision claims.



## Round 1 independent repair reread — 2026-11-12

Keel independently reread Quill repairs in spec.md, plan.md, tasks.md, checklists/requirements.md and DRAFTING_RECEIPT.md against the ten-item adjudication list. All ten content repairs are CLOSED; detailed evidence is recorded in PLAN_ADJUDICATION.md. This closes the repair reread, not the separate review-provenance condition.

PLAN FROZEN remains WITHHELD: recon-DEV-483:51–63 cannot confirm Compass or Ledger exact reviewed snapshots. Needs recon: reviewer-author attestation of exact reviewed commit/artifact working-tree state, or the permitted second three-axis challenge round against an explicitly pinned snapshot. No snapshot equivalence is inferred from commit order. Gate 1, implementation, runtime acceptance and merge remain open; no further scope or protocol decision is introduced by this status receipt.


## PLAN FROZEN — Phase 2 step 6.2, 2026-11-12

Keel declares PLAN FROZEN. This receipt supersedes the earlier pending-provenance status entries; those entries remain historical evidence.

Basis: all three axis reports were read and all 21 findings explicitly adjudicated under the cited Patron ruling. Quill applied the single ten-item Round 1 repair list, and Keel independently reread the repaired artifacts and closed all ten repairs. Compass reviewer-author attestation (findings-DEV-483-Compass:45–56) identifies the reviewed uncommitted plan/spec/tasks/brief drafts over d7a8b6247f35100d8ba33fdb1221e1d92b985076, before Round 1 repairs. Ledger reviewer-author attestation (findings-DEV-483-Ledger:49–56) likewise identifies its findings as applying to the pre-adjudication drafts over that base. Sentry recorded that same reviewed base in its original report.

Provenance disposition: CLOSED for Round 1 plan adjudication. The original findings quote the pre-repair defects that Keel checked and whose repairs Keel independently verified. The attestations establish the reviewed draft stage and base for those findings; they do not reconstruct byte-identical historical untracked files or claim the reviewers reviewed the repaired current tree. Ledger explicitly cannot independently prove its original checkout HEAD, and neither author has review-time content hashes. Those limits are retained and are not recast as exact-tree proof. The missing provenance was the identity/stage of the original draft review, not a requirement that all three reviewers re-review every repair. Author attestations plus corroborated finding-to-repair evidence satisfy that condition here. No second three-axis plan challenge is required; no second round is claimed.

Frozen scope, diagnosis-first stop, protocol, candidate boundaries, mandatory gates, closing bar and two-round/two-fix-commit caps remain as recorded above. No new scope or protected-surface authorization is introduced. Next is spec-PR / provisional Gate 1 routing under Bernstein and Patron; only the user merges. T000 verified owner-merge receipt remains required before T001+. This declaration is not Gate 1 approval, implementation delivery, runtime acceptance, cause-documentation acceptance or merge clearance. No production gates were run by this plan adjudication.
