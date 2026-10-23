# DEV-357 - Decision brief

Status: grill CLOSED by Patron Q9 for Quill drafting. Nine questions used of the twelve-question grill limit. Q1-Q3 are preserved without reopening; Q4-Q9 complete the decisions. No further Phase A recon or owner structural checkbox is identified. Plan challenge/freeze, analysis clearance, Gate 1 and implementation readiness remain separate and unclaimed.

## Identity, authority and evidence

- Worktree: F:/Dev/LamuFlix.worktrees/feature-357-spec
- Branch: feature/357-spec
- Phase A base: ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28
- Authority: DEV-357 tracker Scope & Technical Design and acceptance text reproduced in recon-DEV-357:108-117; worktree specs/PRODUCT.md; lamuflix-team-charter; task-pipeline Phase 2.
- Facts: recon-DEV-357 and recon-DEV-357-2 only. Later pinned corrections in recon-DEV-357-2 supersede earlier negative playback/vector and generator-search claims. The tracker excerpts at lines 104-139 supersede the earlier summary-only and missing-tracker-text limitations. They establish DEV-415/416 behavior, not landed APIs. Earlier dated recon statements and the historical Q&A remain evidence of the exchange, not current unresolved decisions.
- Patron decides; Keel records. Q4 and Q5 are explicit Patron design rulings on unspecified semantics, not reconstructed ticket text or evidence of runtime interfaces.
- Complete exchanges are below and in append-only CONCLUSIONS.md. No taste assumption, new ADR or glossary artifact was required (Q9).

## Authoritative ticket delivery

Scope (recon-DEV-357:108-112):

1. GET /api/recommendations/for-you?take=10, with the same take validation as DEV-341.
2. Take TasteProfile (DEV-353, cached in DEV-415), score the library with blend (DEV-354), apply MMR (DEV-416), and return RecommendedMovieDto items with Personal reasons.
3. No watch history returns 200 with an empty list.
4. Update web/src/api/openapi.json and the drift snapshot, as in DEV-341.

Acceptance (recon-DEV-357:114-117):

- With watch history, the endpoint returns ranked recommendations with Personal reasons.
- With no watch history, it returns 200 and an empty list.
- The OpenAPI document has no drift from the committed openapi.json.

The For You frontend rail belongs to DEV-418 (DEV-357 task note overview), outside this delivery.

## Frozen scope and loop discipline (Q3, unchanged)

This delivery covers GET /api/recommendations/for-you, DEV-341 take behavior, the named TasteProfile/cache/blend/MMR integrations, predecessor RecommendedMovieDto with Personal reasons, no-history 200 empty response, required OpenAPI/snapshot/generated TypeScript contract updates and verifying tests; anything else is a follow-up issue, not a finding in this round.

The closing bar is no unresolved Critical or High in-scope findings, all ticket acceptance requirements and applicable gates satisfied, all required axes present, and no unresolved owner escalation or missing contract needed to judge conformance. Medium/Low cannot waive an acceptance requirement or gate; otherwise they do not independently block.

Round cap: two remediation rounds total per review loop, preserved across re-asks. At the cap report unresolved blockers without a third round. Required M-ticket axes are Sentry, Ledger and Compass. Grill closure does not meet this later review bar.

## Decided approach

### HTTP and DTO (Q2)

Use GET /api/recommendations/for-you. take defaults to constant 10 and the effective value is range-validated in 1..CandidatePoolSize; invalid input returns parser 400. Omission itself is allowed, but omitted take returns 400 when startup-valid options.Take <= CandidatePoolSize < 10. This is inherited DEV-341 behavior (recon-DEV-357-2:65-71); keep parser 400 ProblemDetails/traceId distinct from direct-query validation 422. Return a 200 array of predecessor RecommendedMovieDto. Consume DEV-354 typed reasons with lowercase structural/personal kinds and kind/text fields; no parallel DTO, new kind or invented template. Use the existing route parser and ToDto. Preserve final MMR order.

### Profile and history (Q1, Q5)

Consume DEV-353 through DEV-415. HasHistory is at least one PlaybackEvent OR at least one IsWatched=true movie; Not Interested alone is not positive history. No history returns 200 [] before scoring or MMR. TasteProfile.IsEmpty does not establish no history.

DEV-415 owns the single-user tagged profile cache, configurable expiry, and playback/feedback invalidation. DEV-357 adds no cache, TTL, key policy or package. Landed acquisition/cache support must expose or support the history predicate coherently with the underlying history used for profile calculation. Exact signatures and snapshot consistency remain pickup prerequisites, not assumed facts.

### Candidate scoring (Q4)

Candidates are all eligible library movies, excluding Not Interested before scoring. There is no selected-movie candidate source, seed, pre-truncation or refill. CandidatePoolSize bounds take validation; it does not authorize truncating this candidate universe.

Invoke the literal DEV-354 blend Score = a*S + b*P - c*W - d*N with S=0 because no movie is selected. Use predecessor profile signal and watched penalty; retain configured weights with no renormalization or final clamp. Keep DEV-354 per-movie behavior unchanged. Full-library scoring cost is accepted. This candidate/selected-signal policy is Patron Q4, not ticket wording (recon:119-120).

### MMR, score and reasons (Q6)

Pass blended relevance to DEV-416 MMR before Take. Consume its RecommendationOptions.MmrLambda (default 0.7), DEV-338 similarity, iterative relevance/diversity selection, deterministic movie-ID ties, uniqueness and min(Take, eligible candidate count) output (recon:129-135). Do not create another MMR algorithm or invent its landed signatures.

Preserve blend scores and predecessor reason objects in MMR selection order. Do not resort by score or replace the score with the MMR objective. No candidates returns 200 []. Real history with no positive comparable profile channel, or BlendProfileWeight=0, keeps DEV-354 conditional Personal emission and Structural fallback. Do not fabricate a Personal match or classify feature-poor history as absent. Acceptance proof uses a positive-match history fixture with profile weighting enabled. Patron Q6 found no ticket contradiction or constitution departure.

## Planned files and implementation boundary (Q7)

These are decisions for later implementation, not proof that new files or landed dependency APIs exist.

| Planned action | Exact path |
| --- | --- |
| New query | src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQuery.cs |
| New validator (Q7 amendment, DEV-357:225) | src/LamuFlix.Infrastructure/Recommendations/GetForYouRecommendationsQueryValidator.cs |
| New handler | src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs |
| Matching unit tests | tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryTests.cs; GetForYouRecommendationsQueryValidatorTests.cs; GetForYouRecommendationsQueryHandlerTests.cs |
| Add thin route, reuse parser/ToDto | src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs |
| Required registration | src/LamuFlix.Api/HandlerRegistration.cs |
| Route-equality update and additive for-you ManifestContracts row only (DEV-357:254-255) | tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs |
| Endpoint integration coverage | tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs |
| End-to-end coverage | tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs |
| Contract verification/export | tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs |
| Derived drift snapshot | tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json |
| Generated contract | web/src/api/openapi.json |
| Generated TypeScript | web/src/api/types.ts |
| Generated MSW output via existing generate:api only (DEV-357:254-255) | web/src/mocks/handlers.ts |

Patron explicitly authorized the listed existing endpoint, registration and test edits forced by route/OpenAPI acceptance. The handler orchestrates landed profile/cache/history, all-library acquisition, blend and MMR; results are immutable and cancellation propagates. No new port, project, layer, package, migration, schema, DTO shape or cache is authorized. No predecessor algorithm alteration. Infrastructure source/DI files outside this list are not silently authorized.

### Q7 validator amendment - authorized 2026-10-22

Authority: DEV-357:225 records Patron authorization of the proposed Q7 validator amendment within scope, with no owner escalation. The proposal is recorded at DEV-357:209: replace only the validator path with `src/LamuFlix.Infrastructure/Recommendations/GetForYouRecommendationsQueryValidator.cs`; retain the Core query and handler and all three unit-test paths. No new package or Core dependency is authorized. Basis: recon-DEV-357-2:8-13 records the Core FluentValidation prohibition and existing Infrastructure validator placement. This amendment supersedes the original validator placement in Q7; the historical Q&A below remains verbatim. The external temporary ruling file was not read.

The validator receipt authorizes only that path amendment. The two separate additional permissions below come from DEV-357:254-255. No plan freeze, Gate 1, or implementation readiness is granted.

### Q7 host-composition and generated-output amendments - authorized 2026-10-22

Authority: DEV-357:254-255 records two separate Patron rulings, each within scope with no owner escalation:

1. `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs`: authorize the route-equality update and additive for-you `ManifestContracts` row only. Basis: recon-DEV-357-2:82-88 proves the exact route-equality assertion; :90-93 establishes per-row decorator-resolution coverage and no explicit manifest completeness/count assertion. Do not claim a forced manifest-completeness failure or an unsupported route count.
2. `web/src/mocks/handlers.ts`: authorize this generated output only through existing `generate:api`, alongside `web/src/api/types.ts`, never hand-edited. Basis: recon-DEV-357-2:95-106,112-115; `web/package.json:17` invokes `web/scripts/generate-api.mjs`. This permission does not assert that every generator run changes output bytes or authorize a replacement generator.

The external temporary ruling file was not read. These permissions do not authorize UI consumer/fixture edits or unrelated changes.

### Accepted plan-challenge clarifications

Register scoped concrete `GetForYouRecommendationsQueryValidator` and scoped `IValidator<GetForYouRecommendationsQuery>` forwarding to the same concrete instance, alongside `AddHandler` in the already authorized `HandlerRegistration.cs`. Preserve Tracing -> Logging -> Validation -> handler and prove direct-query validation through composed registration (recon-DEV-357-2:16-20).

At read-only Phase B pickup, verify landed typed-reason wire/types, existing RelatedMovies consumers/tests and mock fixtures are coherent. Otherwise STOP for bounded ownership recon and a cited Patron decision before out-of-ledger changes. No assumed predecessor ownership, DEV-418 reassignment, UI authorization or web-gate waiver follows (recon-DEV-357-2:41-44).

## Test strategy (Q8)

Use existing repository testing/assertion conventions, actual predecessor algorithms and seeded data. Use real PostgreSQL/Testcontainers for data-dependent persistence, vectors and cache-invalidation route tests. Never mock driver query translation or serialization, and never substitute fake vectors for runtime acquisition proof.

- take default, including omitted default exceeding a startup-valid pool below 10; lower/upper boundaries, repeated and noninteger inputs; parser 400 ProblemDetails/traceId distinct from direct query validation 422 through composed registration.
- Playback-only and watched-only history; no-history and Not Interested-only cold start; real history with empty features; no candidates.
- Positive genre/plot Personal explanations; disabled profile-weight fallback; typed lowercase reason wire fields.
- All-library candidates beyond CandidatePoolSize; Not Interested exclusion before scoring; S=0 literal blend, watched penalty and negative finite score preservation.
- MMR selection order changes without mutating scores/reasons; uniqueness, min-size, deterministic ordering/ties, and a diverse sequel fixture. FsCheck properties remain required; no property-test opt-out.
- Repeated cache reads and playback/feedback invalidation consume DEV-415 behavior rather than rebuilding its cache.
- Cancellation propagation, existing per-movie route regression, no OpenAPI drift and generated-contract consistency.

## Contract generation and gate expectations (Q8)

Reuse OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether with LAMUFLIX_REGENERATE_OPENAPI=true, followed by existing web generate:api, which produces web/src/api/types.ts and web/src/mocks/handlers.ts (recon-DEV-357-2:95-106,112-115). Preserve C# DTO -> OpenAPI -> generated TypeScript. Never hand-edit generated outputs or introduce a new exporter. Wisp supplies exact commands from landed configuration at pickup; Quill must not invent flags or signatures.

At the Build stage Gauge runs task-pipeline Phase 3 step 5 at harness thresholds: Roslyn, complexity, InspectCode, mutation, property tests, vulnerable packages, format verification, dotnet test and applicable web gates. No threshold changes. Scope-empty exit 2 is nonblocking SKIPPED (scope-empty), never PASS; mutation explicit exclusions are N/A; harness-disabled gates are SKIP; exit 1 or Could not run blocks. No tests or gates run in this Spec-workspace grill.

## Task ordering and pickup stop (Q1, Q7-Q9)

1. First tasks.md phase is read-only: Wisp verifies merged spec, Epic 7 prerequisite, landed DEV-353/354/415/416 and real vector/acquisition dependencies, drift against the Phase A base, exact signatures, coherent history predicate support, and all-library summary/feedback/384-vector acquisition. Gauge baseline measurements are separate. STOP before any code if any prerequisite is absent.
2. After readiness: Anvil implements Core query/handler orchestration, the Infrastructure validator and unit/FsCheck tests as one phase; commit before reporting.
3. Anvil implements thin API route/registration and real integration coverage as the next phase; commit before reporting.
4. Anvil completes opt-in OpenAPI/drift-snapshot export and generated TypeScript contract updates as the next phase; commit before reporting.
5. Cog refactor, Gauge applicable gates, all three review axes, then remediation within Q3 cap, at the Build workspace stages.

At this Phase A base the named DEV-353/354/415/416 runtime contracts remain unlanded. Later recon-DEV-357-2:50-60 corrects the earlier blanket playback/vector absence claims: playback events/write persistence and vector record/column/snapshot/repository exist. That evidence does not establish a history-read contract, complete all-library acquisition, the exact IMovieFeaturesRepository interface signature or runtime readiness (:56,60,62). Tracker excerpts establish intended behavior, not implementation readiness. If existing landed ports cannot supply all-library/history/vector inputs, STOP for bounded dependency recon and a cited Patron amendment before adding a port/adapter. Never waive dependencies, invent a dependency signature or write a production substitute. A ticket contradiction or constitution departure still requires the owner checkbox route under section 2.3.

## Quill handoff and remaining decisions (Q9)

No unresolved Phase A product or scope decision remains. No owner structural checkbox is identified. No further recon is needed for Phase A now. Runtime support and exact command/signature facts remain mandatory Phase B pickup checks, not settled facts.

Quill drafts spec.md, plan.md and tasks.md from this brief, recon-DEV-357 and specs/PRODUCT.md. Preserve the first read-only STOP phase and bounded file set; do not infer missing APIs or generation commands. A decision gap is needs decision to Keel. Quill does not make new rulings. Subsequent read-only speckit analysis, plan challenge/freeze and Gate 1 remain outstanding. No implementation, gate execution, commit, PR or merge is part of this grill closure.

## Historical exchange record

The complete Q1-Q9 exchanges below are preserved verbatim. The earlier Resume requirement after Q3 is a historical intermediate state resolved by the tracker excerpts and Q4-Q9; it does not override the current decisions above.
## Complete grill Q&A
## Q1 - Dependency sequencing and pickup

### Keel question (complete)

[from Keel] /grill-with-docs DEV-357. Resume Phase 2 step 1 in F:/Dev/LamuFlix.worktrees/feature-357-spec, branch feature/357-spec, HEAD ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28. Read DEV-357, recon-DEV-357, lamuflix-team-charter, task-pipeline Phase 2, and this worktree specs/PRODUCT.md. Keel will append the complete exchange to specs/DEV-357/CONCLUSIONS.md and mirror every answer in brief.md; do not edit these files concurrently. Q1: What dependency and pickup rule should DEV-357 freeze? I recommend Phase A specify the ticket-mandated TasteProfile DEV-353, cached DEV-415, blend DEV-354, and MMR DEV-416 integration, but Phase B must wait for their landed contracts plus PRODUCT section 2 Epic 7 prerequisite, then perform fresh bounded drift recon before implementation. No substitute algorithms or stubs in production, no waiver of named dependencies. Basis: DEV-357 note lines 14 and 19; recon-DEV-357 lines 20-32; PRODUCT sections 2-3; charter lines 25-28. Cost: the spec may advance while implementation remains dependency-gated. Please give a cited ruling on Q1 only and report back with maestri ask "Keel" when done. Facts come from recon-DEV-357. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer (complete)

[from Patron] DEV-357 Q1 ruling: APPROVED as recommended. Freeze dependency and pickup rule: Phase A specifies ticket-mandated TasteProfile DEV-353, cached DEV-415, blend DEV-354, and MMR DEV-416 integration; Phase B implementation waits for their landed contracts plus PRODUCT section 2 Epic 7 prerequisite, then fresh bounded drift recon before implementation; no substitute algorithms or stubs in production, no waiver of named dependencies. Basis: DEV-357 note line 14 names TasteProfile DEV-353 cached DEV-415, blend DEV-354, MMR DEV-416 as ticket logic, and line 19 confirms none landed; recon-DEV-357 lines 20-26 and 32 confirm TasteProfile, MMR, blend, and DEV-415 cache absent in src; specs/PRODUCT.md line 17 defers Epic 9 Recommendations until after Epic 7 SPA completes and lines 23-24 make ticket Scope and Technical Design authoritative. Cost accepted: spec may advance while implementation remains dependency-gated. Record in CONCLUSIONS.md and brief.md per your ownership; Q1 only.

### Rationale and implications

The ticket names all four integrations. Deferring implementation preserves that delivery contract without speculative production substitutes. Phase A may continue; Phase B requires the landed predecessor contracts, Epic 7 completion, and fresh bounded drift recon. This is a dependency ruling, not plan freeze or Gate 1 clearance.


---

## Q2 - HTTP and predecessor reason contract

### Keel question (complete)

[from Keel] DEV-357 /grill-with-docs Q2 only, continuing accepted Q1 without reopening it. Worktree F:/Dev/LamuFlix.worktrees/feature-357-spec, branch feature/357-spec, HEAD ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28. Read updated recon-DEV-357, DEV-357, and existing brief.md/CONCLUSIONS.md. What HTTP and reason contract should DEV-357 consume? I recommend the ticket-named GET /api/recommendations/for-you; take default 10, single integer 1..CandidatePoolSize and 400 on invalid input as DEV-341; 200 array using the predecessor RecommendedMovieDto and DEV-354 typed reasons {kind,text} with lowercase structural/personal kinds, preserving the final dependency-defined ranking. No new DTO variant, reason kind, exporter, or reason template. Keep the ticket-summary no-watch-history 200 empty requirement distinct from TasteProfile.IsEmpty: do not equate them or invent the history predicate before its acquisition/cache contract is evidenced. Basis: recon lines 6,10-13,29,55-60,69,101-102; DEV-357 note line 14. This deliberately consumes the accepted DEV-354 wire evolution rather than the older current-source string reasons. Cost: HTTP work waits for that predecessor contract and the dependency-defined history/ranking semantics. Please rule with citations on Q2 only; Keel records full exchange, so do not edit the decision files concurrently. Report back with maestri ask "Keel". Facts come from recon-DEV-357. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer (complete)

[from Patron] DEV-357 Q2 ruling: APPROVED as recommended. Freeze HTTP and reason contract: consume ticket-named GET /api/recommendations/for-you; take default 10, single integer 1..CandidatePoolSize, 400 on invalid input as DEV-341; 200 array using predecessor RecommendedMovieDto and DEV-354 typed reasons kind/text with lowercase structural and personal kinds, preserving final dependency-defined ranking; no new DTO variant, reason kind, exporter, or reason template. Keep ticket no-watch-history 200 empty requirement distinct from TasteProfile.IsEmpty; do not equate them or invent history predicate before acquisition and cache contract is evidenced. Basis: recon line 6 and DEV-357 note line 14 name route, take as DEV-341, RecommendedMovieDto with Personal reasons, and 200 empty without history; recon lines 10 and 29 evidence DEV-341 take default 10, range 1..CandidatePoolSize, and 400; recon lines 69 and 101-102 record DEV-354 accepted typed lowercase kind/text reasons to consume not redefine; recon lines 55-60 define TasteProfile and IsEmpty without a history predicate. Cost accepted: HTTP work waits for predecessor contract and dependency-defined history and ranking semantics. Q2 only, Q1 unchanged.

### Rationale and implications

Consume the predecessor DTO and reason evolution without creating a parallel wire shape. Preserve final dependency-defined order and distinguish history absence from an empty calculated profile. Acquisition/cache history semantics remain unresolved; HTTP implementation waits for their evidence. Q1 remains unchanged.


---

## Q3 - Review closing bar, frozen scope and round cap

### Keel question (complete)

[from Keel] DEV-357 /grill-with-docs Q3 only: settle review loop terms without closing the incomplete grill. Q1 and Q2 unchanged. Worktree F:/Dev/LamuFlix.worktrees/feature-357-spec at ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28. I recommend a closing bar of no unresolved Critical or High in-scope findings, all ticket acceptance requirements and applicable gates satisfied, all required axes present, and no unresolved owner escalation or missing contract needed to judge conformance. Medium/Low cannot waive an acceptance requirement or gate; otherwise they do not independently block. Round cap two remediation rounds total per review loop, preserving the pipeline cap across re-asks; at cap report unresolved blockers without a third round. Frozen scope for this task: implement the ticket-summary GET /api/recommendations/for-you, DEV-341 take behavior, named TasteProfile/cache/blend/MMR integrations, predecessor RecommendedMovieDto with Personal reasons, no-history 200 empty response, and required OpenAPI/snapshot/generated TypeScript contract updates and verifying tests; exact acceptance wording and integration details remain pending evidence; anything else is a follow-up issue, not a finding in this round. This scope statement cannot authorize a ticket change or treat missing predecessor contracts as settled. Basis: DEV-357 note line 14; recon lines 6,16-17,29-32,47-51,69,76,80,98-102; task-pipeline Phase 2 step 3.1, Phase 4 steps 2-4, Phase 3 step 5 scope-empty rules; charter section 2.3. Cost: real acceptance/gate failures still block at any severity, while unrelated cleanup does not expand this delivery. Approve or amend Q3 with cited basis. Do not edit decision files; Keel records the full exchange. Report back with maestri ask "Keel". Facts come from recon-DEV-357. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer (complete)

[from Patron] DEV-357 Q3 ruling: APPROVED as recommended. Freeze review loop terms without closing grill: closing bar is no unresolved Critical or High in-scope findings, all ticket acceptance requirements and applicable gates satisfied, all required axes present, and no unresolved owner escalation or missing contract needed to judge conformance; Medium and Low cannot waive an acceptance requirement or gate, otherwise they do not independently block. Round cap is two remediation rounds total per review loop, preserving pipeline cap across re-asks; at cap report unresolved blockers without a third round. Frozen scope is ticket-summary GET /api/recommendations/for-you, DEV-341 take behaviour, named TasteProfile, cache, blend and MMR integrations, predecessor RecommendedMovieDto with Personal reasons, no-history 200 empty response, and required OpenAPI, snapshot, generated TypeScript updates and verifying tests; exact acceptance wording and integration details remain pending evidence; anything else is a follow-up issue, not a finding in this round. This scope cannot authorize a ticket change or treat missing predecessor contracts as settled. Basis: DEV-357 note line 14 and recon line 6 name the endpoint, integrations, DTO, empty response and OpenAPI work; recon lines 47-51 and 98-102 record summary-only scope and missing DEV-415 and DEV-416 contracts; recon lines 29-32 record DEV-341 and drift contract; task-pipeline section 2.2 line 28 caps review at two rounds and Phase 2 step 3.1 requires brief to carry closing bar, frozen scope and round cap. Cost accepted: real acceptance or gate failures block at any severity, unrelated cleanup does not expand delivery. Q3 only; Q1 and Q2 unchanged; grill not closed.

### Rationale and implications

Review is bounded to this delivery and capped at two remediation rounds. Critical/High findings and any acceptance, applicable gate, required-axis, owner-escalation or necessary-contract gap block closure. Other Medium/Low findings do not independently block. This ruling settles review terms only; it neither closes the grill nor fills missing contracts.



## Resume requirement after Q3

Bounded recon-DEV-357 lines 47-51, 71, 76, 80 and 98-102 establish summary-only DEV-357 requirements, unavailable vector acquisition, and absent DEV-415/416 artifacts. Repeating the same worktree search will not establish those contracts. Request authorized source evidence through Conductor: exact DEV-357 Scope & Technical Design and acceptance text, and authoritative DEV-415/416 ticket or spec contracts, including history/caching semantics, MMR inputs and final ordering. If those sources are also unavailable, record that result and route the remaining design decisions to Patron without claiming any reconstructed text is ticket authority.

Next integration decision must distinguish the selected-movie blend component S from this route, which has no selected-movie route parameter; candidate source and treatment of S are not decided. Do not choose S=0, infer a seed movie, drop a weight, pick a cache TTL/key, invent history semantics, or select an MMR similarity/tie-break formula. File set, tests, gates and task ordering remain to settle after the authoritative inputs are available. Q1-Q3 remain unchanged.

---

## Q4 - For-you candidate and blend-input policy

### Keel question (complete)

[from Keel] DEV-357 /grill-with-docs Q4 only. Q1-Q3 unchanged. Worktree F:/Dev/LamuFlix.worktrees/feature-357-spec, feature/357-spec at ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28. What for-you candidate and blend-input policy should fill the explicitly undecided route semantics? I recommend interpreting ticket score the library as all eligible library movies, excluding Not Interested before scoring, without a selected-movie candidate source, seed, pre-truncation or refill. Invoke the literal DEV-354 blend with selected signal S=0 because this route has no selection, profile signal P and watched penalty W from the mandated predecessors, retain configured weights without renormalization or final clamp, and pass blended relevance to DEV-416 MMR before Take. This is a proposed Patron design ruling, not recovered ticket text or an assertion that a supporting acquisition API exists. Basis: recon lines 65-69 give DEV-354 formula, exclusion and profile channel; lines 109-117 require scoring library, blend then MMR and Personal reasons; lines 119-120 expressly leave source/selected signal undecided; lines 131-135 define MMR. Cost: full eligible-library scoring can cost more than a capped selected-movie pool; runtime acquisition remains a Phase B prerequisite, never a guessed adapter. Keep DEV-354 per-movie behavior unchanged. If you judge this changes ticket delivery or needs a constitution departure, return the exact blocked: structural question; otherwise deliberate cited ruling is yours under charter section 2.3. Keel records full Q&A; do not edit brief/conclusions concurrently. Report back with maestri ask "Keel". Facts come from recon-DEV-357. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer (complete)

[from Patron] DEV-357 Q4 ruling: APPROVED as recommended, as a Patron design ruling and not ticket text. Freeze for-you candidate and blend-input policy: candidates are all eligible library movies, excluding Not Interested before scoring, with no selected-movie candidate source, seed, pre-truncation, or refill; invoke literal DEV-354 blend with selected signal S equal to 0 because this route has no selection, profile signal P and watched penalty W from mandated predecessors, configured weights retained without renormalization or final clamp; pass blended relevance to DEV-416 MMR before Take; keep DEV-354 per-movie behaviour unchanged. Basis: DEV-357 tracker Scope recon lines 109-110 says score the library with DEV-354 blend then apply DEV-416 MMR; recon line 120 expressly leaves candidate source and selected signal undecided for Patron ruling, so this does not contradict ticket text; recon lines 65-66 record literal blend formula and Not Interested exclusion before scoring; recon lines 131-133 record MMR iterative selection on relevance with lambda from RecommendationOptions, ID ties, and output min of Take and candidate count. Cost accepted: full eligible-library scoring may cost more than a capped selected-movie pool, and runtime acquisition remains a Phase B prerequisite, never a guessed adapter. Q4 only; Q1 to Q3 unchanged.

### Rationale and implications

This is an explicit Patron design ruling on previously unspecified semantics. Score all eligible library candidates with S=0 using the unchanged literal blend, then apply MMR and Take. No selected-movie seed or pool truncation. Supporting runtime acquisition remains a prerequisite, not a proven existing API.


---

## Q5 - History predicate and cache boundary

### Keel question (complete)

[from Keel] DEV-357 /grill-with-docs Q5 only: what precise cold-start history predicate and cache-consumption boundary should this endpoint use? Q1-Q4 unchanged. I recommend HasHistory means at least one recorded PlaybackEvent OR at least one IsWatched=true movie; Not Interested alone is not positive watch history. This follows the DEV-353 positive signals, but is a proposed design ruling, not a claimed DEV-415 API. No history returns 200 [] before scoring/MMR; do not treat TasteProfile.IsEmpty as proof of no history. DEV-357 consumes DEV-415 single-user tagged, configurable-expiry cache and its playback/feedback invalidation; it does not add another cache, TTL, key policy or package. Require landed acquisition/cache contracts to expose or support this history predicate from the same underlying history used to calculate the profile; exact signature and snapshot consistency support are prerequisites to verify at pickup, never guessed here. If that support is absent, STOP for bounded dependency recon and Patron decision, not an ad hoc endpoint cache or source. Basis: recon lines 55-60 define profile separately from played/watched signals and assign loading to DEV-415; lines 111,115-116 require no-history empty; lines 123-127 specify single entry, expiry, invalidation and empty cold-cache profile but not a HasHistory API. Cost: history evidence may require predecessor integration work; IsEmpty shortcut would confuse feature-poor history with no history. Rule with citations; if ticket contradiction or constitution departure is necessary, return exact blocked: structural question. Worktree F:/Dev/LamuFlix.worktrees/feature-357-spec, HEAD ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28. Keel records full exchange; do not edit the files. Report back with maestri ask "Keel". Facts come from recon-DEV-357. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer (complete)

[from Patron] DEV-357 Q5 ruling: APPROVED as recommended, as a Patron design ruling and not a claimed DEV-415 API. Freeze cold-start predicate and cache boundary: HasHistory means at least one recorded PlaybackEvent OR at least one IsWatched true movie; Not Interested alone is not positive watch history. No history returns 200 empty list before scoring or MMR; do not treat TasteProfile.IsEmpty as proof of no history. DEV-357 consumes DEV-415 single-user tagged configurable-expiry cache and its playback and feedback invalidation; it adds no other cache, TTL, key policy, or package. Landed acquisition and cache contracts must expose or support this predicate from the same underlying history used to calculate the profile; exact signature and snapshot consistency are pickup prerequisites, never guessed here. If support is absent, STOP for bounded dependency recon and Patron decision, not an ad hoc endpoint cache or source. Basis: recon lines 55-58 define DEV-353 PlaybackEvent and IsWatched positive signals separately from computed IsEmpty; recon lines 111 and 115-116 require 200 empty with no watch history; recon lines 123-127 specify DEV-415 single entry, configurable expiry, playback and feedback invalidation, and empty cold-cache profile without a HasHistory API. Cost accepted: history evidence may require predecessor integration work; the IsEmpty shortcut would confuse feature-poor history with no history. No ticket contradiction or constitution departure found. Q5 only; Q1 to Q4 unchanged.

### Rationale and implications

Positive history is playback or watched feedback, distinct from profile feature emptiness. Consume predecessor cache behavior and require the underlying history predicate to be supported coherently at pickup. Missing runtime support stops implementation; it does not authorize another cache or invented dependency signature.


---

## Q6 - MMR output and degenerate profile behavior

### Keel question (complete)

[from Keel] DEV-357 /grill-with-docs Q6 only: freeze output and degenerate-case behavior after MMR. Recommend preserve DEV-354 blended Score and reason objects on each result, output in DEV-416 selection order, with no final score resort, score replacement by the MMR objective, fabricated Personal reason, or new reason template. Use DEV-416 min(Take, eligible candidate count), uniqueness and deterministic ID ties as its contract; no candidate means 200 []. For real history with no positive comparable profile channel (or BlendProfileWeight=0), retain DEV-354 conditional Personal reasons and Structural fallback instead of inventing a match or treating this as no history. Verify the ticket history-plus-Personal acceptance using a positive-match fixture with profile weighting enabled. Basis: recon lines 65-69 define blend, positive-comparable-channel Personal emission and fallback; 109-117 mandate blend then MMR and Personal results with history; 131-135 define MMR ordering/size/determinism. This is an explicit edge-case interpretation; if you judge retaining the predecessor fallback contradicts the ticket history acceptance, return blocked: structural with the owner question instead of silently narrowing AC. Cost: MMR order need not be descending displayed blend score, and feature-poor history does not guarantee an honest Personal explanation. Q1-Q5 unchanged. Worktree F:/Dev/LamuFlix.worktrees/feature-357-spec at ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28. Keel records full exchange; report back with maestri ask "Keel". Facts come from recon-DEV-357. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer (complete)

[from Patron] DEV-357 Q6 ruling: APPROVED as recommended. Freeze output and degenerate behaviour after MMR: preserve DEV-354 blended Score and reason objects on each result; output in DEV-416 selection order with no final score resort, no score replacement by MMR objective, no fabricated Personal reason, and no new reason template. Use DEV-416 min of Take and eligible candidate count, uniqueness, and deterministic ID ties; no candidate means 200 empty list. For real history with no positive comparable profile channel or BlendProfileWeight equal to 0, retain DEV-354 conditional Personal reasons and Structural fallback instead of inventing a match or treating it as no history. Verify ticket history-plus-Personal acceptance on a positive-match fixture with profile weighting enabled. Basis: recon lines 65-69 define literal blend, Personal emission for positive comparable channels, and Structural fallback; recon lines 109-117 mandate blend then MMR and Personal results with history; recon lines 131-135 define MMR selection order, size, and determinism. Not blocked structural: this consumes the DEV-354 reason contract already frozen in Q2 and scopes the acceptance proof to the positive-match case it can honestly satisfy, rather than narrowing the ticket or fabricating reasons. Cost accepted: MMR order need not be descending displayed blend score, and feature-poor history does not guarantee an honest Personal explanation. Q6 only; Q1 to Q5 unchanged.

### Rationale and implications

MMR selects and orders without changing blended scores or explanation truth. Acceptance proof uses positive profile matches; empty features or disabled profile weighting use the existing fallback. Patron explicitly found no ticket contradiction or constitution departure.


---

## Q7 - Bounded implementation file set and readiness

### Keel question (complete)

[from Keel] DEV-357 /grill-with-docs Q7 only: approve implementation boundaries and planned file set, explicitly conditional on pickup readiness. Recommend new GetForYouRecommendationsQuery.cs, GetForYouRecommendationsQueryValidator.cs and GetForYouRecommendationsQueryHandler.cs under src/LamuFlix.Core/Features/Recommendations/; matching unit-test files under tests/LamuFlix.UnitTests/Features/Recommendations/. Thin new route in src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs reuses its take parser and ToDto, with required registration in src/LamuFlix.Api/HandlerRegistration.cs. Extend tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs and ApiEndToEndRecommendationTests.cs for the route, and OpenApiContractTests.cs plus its existing snapshot for contract verification/export. Regenerate web/src/api/openapi.json and derived types.ts through the existing opt-in writer then generator, never hand-edit generated outputs. These are proposed new file names and bounded edits, not claims about landed predecessor signatures. No new port, project, layer, package, migration, schema, DTO shape or cache; do not alter predecessor algorithms. Core handler orchestrates landed cache/history, full-library acquisition, blend and MMR, returns immutable results and honors cancellation. At pickup Wisp must identify exact landed predecessor signatures and real all-library summary/feedback/384-vector plus history support; if the existing predecessor ports cannot provide these, STOP and request a cited Patron amendment before adding a port or adapter. Infrastructure source/DI files outside this list are not silently authorized. Basis: recon 9-18 existing route/Core/tests, 55 and 70 existing predecessor design, 71 unavailable vectors, 101-102 reasons/export writer, 109-112 ticket, Q1-Q6. Cost: new orchestration remains narrow but cannot become executable until dependencies satisfy these needs. Do you approve this bounded plan, including deliberate section 2.3 authorization for edits to named existing endpoint/registration/tests forced by route AC? Worktree F:/Dev/LamuFlix.worktrees/feature-357-spec at ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28. Keel records full exchange; report back with maestri ask "Keel". Facts come from recon-DEV-357. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer (complete)

[from Patron] DEV-357 Q7 ruling: APPROVED as recommended, conditional on pickup readiness. Freeze bounded file set: new GetForYouRecommendationsQuery.cs, GetForYouRecommendationsQueryValidator.cs and GetForYouRecommendationsQueryHandler.cs under src/LamuFlix.Core/Features/Recommendations/ with matching unit tests under tests/LamuFlix.UnitTests/Features/Recommendations/; thin new route in src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs reusing its take parser and ToDto, with required registration in src/LamuFlix.Api/HandlerRegistration.cs; extend tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs, ApiEndToEndRecommendationTests.cs, OpenApiContractTests.cs and its existing snapshot; regenerate web/src/api/openapi.json and derived types.ts through existing opt-in writer then generator, never hand-edit generated outputs. No new port, project, layer, package, migration, schema, DTO shape or cache; do not alter predecessor algorithms. Core handler orchestrates landed cache and history, full-library acquisition, blend and MMR, returns immutable results and honours cancellation. At pickup Wisp must identify exact landed predecessor signatures and real all-library summary, feedback, 384-vector plus history support; if existing predecessor ports cannot provide these, STOP and request cited Patron amendment before adding a port or adapter; infrastructure source or DI files outside this list are not silently authorized. Section 2.3 authorization granted for edits to the named existing endpoint, registration and test files because the route and OpenAPI acceptance in recon lines 109-117 and DEV-357 note line 14 force those edits. Basis: recon lines 9-18 locate existing route, Core, tests, OpenAPI and snapshot touchpoints; lines 101-102 record typed reasons and existing opt-in export writer; line 71 records unavailable 384-vectors requiring pickup stop. Cost accepted: orchestration stays narrow but cannot become executable until dependencies satisfy these needs. Q7 only; Q1 to Q6 unchanged.

### Rationale and implications

New query orchestration belongs in existing feature modules. Route, registration, tests and generated contracts have explicit authorization; unrelated infrastructure changes and new ports do not. Phase B starts with exact landed-signature and real acquisition proof, otherwise stops for recon and a cited amendment.


---

## Q8 - Verification strategy and task ordering

### Keel question (complete)

[from Keel] DEV-357 /grill-with-docs Q8 only: approve test strategy, gate expectations and task ordering for Q1-Q7. Recommend Phase B first has Wisp read-only merged-spec/Epic7/dependency drift and exact signature/acquisition checks, Gauge baseline separately; STOP before code if any prerequisite missing. Then Anvil implements Core orchestration/validator plus unit and FsCheck properties, API/registration plus real integration tests, and opt-in OpenAPI/snapshot export plus TypeScript generation, each as an ordered tasks.md phase with commit. Then Cog refactor and Gauge gates per task-pipeline, followed by all three mandatory review axes and capped remediation. Tests: take default/bounds/repeated/noninteger inputs and direct query validation; playback-only, watched-only, Not Interested-only and no-history guards; history with empty features; no candidates; positive genre/plot Personal explanations and disabled profile-weight fallback; full-library candidates beyond CandidatePoolSize, pre-score Not Interested exclusion, S=0 unnormalized blend, watched penalty, negative finite score preservation; MMR changes order without changing scores/reasons, uniqueness, min-size, determinism/ties and a diverse sequel fixture. Use actual predecessor algorithms with seeded data; real PostgreSQL/Testcontainers for persistence/vector/cache-invalidation route tests, never mocked data-driver translation/serialization. Cache repeat-read/playback/feedback recompute coverage consumes DEV-415, not a reimplementation. Test cancellation propagation. Regress existing per-movie route and verify typed wire output plus no OpenAPI drift. Use existing opt-in OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether with LAMUFLIX_REGENERATE_OPENAPI=true, then repository TypeScript generator; Wisp supplies exact commands at pickup rather than inventing flags. Gates are task-pipeline Phase 3 step 5 with harness thresholds: Roslyn, complexity, InspectCode, mutation, properties, vulnerabilities, format, dotnet test and applicable web gates. No property opt-out because ordering/size/exclusion invariants exist. Scope-empty exit2 is nonblocking SKIPPED (scope-empty), never PASS; mutation explicit exclusions are N/A; disabled harness gate is SKIP; exit1 or Could not run blocks. No gates/tests run in this Spec-workspace phase. No frontend rail: DEV-418 owns UI. Basis: recon 9-18,65-71,101-102,109-117,123-135, Q1-Q7; task-pipeline Phase 3-4 and charter boundary. Cost: real dependency integration is required; no simulation can clear vector readiness. Worktree F:/Dev/LamuFlix.worktrees/feature-357-spec at ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28. Rule Q8 only with citations, or identify any proposed test that exceeds scope. Keel records full exchange; report back with maestri ask "Keel". Facts come from recon-DEV-357. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer (complete)

[from Patron] DEV-357 Q8 ruling: APPROVED as recommended; no proposed test exceeds scope. Freeze Phase B ordering: Wisp read-only merged-spec, Epic 7 and dependency drift plus exact signature and acquisition checks first, Gauge baseline separately, STOP before code if any prerequisite missing; then Anvil ordered phases for Core orchestration and validator with unit and FsCheck properties, API and registration with real integration tests, and opt-in OpenAPI and snapshot export plus TypeScript generation, each phase committed; then Cog refactor, Gauge gates, all three mandatory review axes, and capped remediation per Q3. Test matrix approved as proposed: take and validator cases; playback-only, watched-only, Not Interested-only and no-history guards; history with empty features; no candidates; positive genre and plot Personal explanations and disabled profile-weight fallback; full-library beyond CandidatePoolSize, pre-score Not Interested exclusion, S equal 0 unnormalized blend, watched penalty and negative finite score preservation; MMR order without score or reason change, uniqueness, min-size, determinism and ties, diverse sequel fixture; cancellation; per-movie regression; typed wire output and no OpenAPI drift. Use actual predecessor algorithms with seeded data and real PostgreSQL Testcontainers for persistence, vector and cache-invalidation route tests, never mocked driver translation or serialization; cache coverage consumes DEV-415, not a reimplementation. No property opt-out. Gates are task-pipeline Phase 3 step 5 at harness thresholds; scope-empty exit 2 is nonblocking SKIPPED scope-empty never PASS, mutation exclusions are N/A, disabled gate is SKIP, exit 1 or Could not run blocks. No gates or tests run in this Spec workspace; no frontend rail, DEV-418 owns UI. Basis: recon lines 9-18 locate route, Core, tests and OpenAPI touchpoints; lines 65-71 and 131-135 define blend, Personal fallback, unavailable vectors and MMR invariants; lines 109-117 and 123-127 define ticket acceptance and DEV-415 cache behaviour; lines 101-102 record existing opt-in export writer. Exact generation commands are supplied by Wisp at pickup, not invented here. Q8 only; Q1 to Q7 unchanged.

### Rationale and implications

Read-only readiness checks precede all code. Tests prove route composition using real predecessor behavior and real persistence for data-dependent behavior; properties remain required. Run gates only in the Build workspace at the authorized pipeline stage, then reconcile all three axes with the unchanged two-round cap.


---

## Q9 - Shared understanding and grill closure

### Keel question (complete)

[from Keel] DEV-357 /grill-with-docs Q9 closing confirmation only. Q1-Q8 full exchanges are persisted and read back in brief.md and append-only CONCLUSIONS.md. Do we have shared understanding to close the grill for Quill drafting from these decisions, with no owner structural checkbox identified and no further recon required for Phase A right now? Recommendation: yes. Q4 deliberately supplies candidate/selected-signal policy absent from tracker; Q5 supplies history predicate without claiming a DEV-415 API; Q6 settles honest reasons and MMR output; Q7 fixes bounded file set with no new port or adapter; Q8 fixes tests, gates and task order; Q1-Q3 remain unchanged. Preserve explicit Phase B STOP until Epic7, landed DEV-353/354/415/416 and their real vector/acquisition dependencies, exact signatures, coherent history support and all-library source are verified by fresh Wisp recon; if support is absent, seek a cited Patron amendment, never guess interfaces, waive dependencies or write a substitute. Quill must make that the first read-only prerequisite phase and must not invent signatures or generation commands. Remaining unknown runtime facts are pickup prerequisites, not settled implementation facts. No new ADR/glossary artifact is proposed: M ticket, established terms, no new domain concept or architecture layer; no taste assumption was made. This closes the grill only, not speckit analysis, plan challenge/freeze, Gate1 or implementation readiness. Basis: recon 55-80,101-102,108-139; Q1-Q8; PRODUCT sections 2-5 and task-pipeline Phase2 writing/decision split. Cost: Phase A can proceed but Phase B may stop for missing predecessors. Worktree F:/Dev/LamuFlix.worktrees/feature-357-spec at ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28. Please confirm or identify the remaining decision; Keel will update final brief and report Conductor. Report back with maestri ask "Keel". Facts come from recon-DEV-357. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer (complete)

[from Patron] DEV-357 Q9 ruling: CONFIRMED, shared understanding to close the grill for Quill drafting from Q1 to Q8, with no owner structural checkbox identified and no further recon required for Phase A now. Q1 to Q3 unchanged; Q4 supplies candidate and selected-signal policy absent from tracker; Q5 supplies history predicate without claiming a DEV-415 API; Q6 settles honest reasons and MMR output; Q7 fixes bounded file set with no new port or adapter; Q8 fixes tests, gates and task order. Preserve explicit Phase B STOP until Epic 7, landed DEV-353, DEV-354, DEV-415 and DEV-416 and their real vector and acquisition dependencies, exact signatures, coherent history support and all-library source are verified by fresh Wisp recon; if absent, seek cited Patron amendment, never guess interfaces, waive dependencies or write a substitute. Quill must make that the first read-only prerequisite phase and must not invent signatures or generation commands; remaining runtime unknowns are pickup prerequisites, not settled facts. No new ADR or glossary artifact; no taste assumption made. This closes the grill only, not speckit analysis, plan challenge or freeze, Gate 1, or implementation readiness. Basis: recon lines 108-120 record authoritative ticket Scope and the expressly undecided candidate rule ruled in Q4; lines 122-139 record DEV-415 and DEV-416 excerpts and explicit unavailable fields; lines 55-80 and 101-102 record predecessor contracts and export writer. Cost accepted: Phase A proceeds but Phase B may stop for missing predecessors. Q9 only.

### Rationale and implications

Grill closed for Quill drafting; no additional Phase A recon or owner checkbox is identified. Runtime facts remain unverified and are mandatory Phase B pickup checks. This closure grants no analysis clearance, plan freeze, Gate 1, implementation readiness or permission to add a new port/adapter.


