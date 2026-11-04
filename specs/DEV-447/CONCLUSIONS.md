# DEV-447 Grill conclusions

## Q1 - Scope and suspicious-match conjunction

### Question
[from Keel] /grill-with-docs DEV-447 Q1/12. Worktree F:/Dev/LamuFlix.worktrees/feature-447-spec branch feature/447-spec. Read recon-DEV-447 especially lines 106-141 and DEV-447. I recommend freezing this as automatic OMDb match validation plus bounded fallback for the two named regressions, preserving existing matcher threshold/year/normalization semantics, manual IMDb authority, APIs and recommendation algorithms. Suspicious means all three rating, director and runtime absent or unusable, not any single missing field. This enforces ticket Fix bullets without discarding ordinary incomplete records; cost is possible extra fallback requests. Do you approve this scope and conjunction definition? Give cited basis. Answer only this decision; Keel records full exchange in specs/DEV-447/CONCLUSIONS.md and brief.md. Please return your ruling in this ask response.

### Patron answer
Approved: freeze DEV-447 scope to automatic OMDb match validation and bounded fallback for the two regressions while preserving existing MatchThreshold/TitleMatcher semantics, manual IMDb authority, public APIs, and recommendation algorithms; define suspicious as the conjunction of rating, director, and runtime all absent or unusable.
- Ticket basis (DEV-447 Fix & Acceptance, recon-DEV-447:108,124-130): Fix bullet 3 specifies a match with no rating, director and runtime (conjunction of all three absent/N/A/unusable) falls through to search results or TMDb, and Acceptance requires Memento[2000] -> tt0209144 and O Brother, Where Art Thou[2000] -> tt0190590.
- Matcher & threshold preservation (recon-DEV-447:24-41,53-56; OmdbMetadataProvider.cs:69-84; TitleMatcher.cs:14-39,120+; OmdbOptions.cs:22; ADR 0030:21): Direct t= lookup currently skips candidate verification (OmdbMetadataProvider.cs:76-84), whereas TitleMatcher already normalizes punctuation and scores years and MatchThreshold (0.8) is shared with TmdbMetadataProvider.
- Scope boundary (§2.3 & recon-DEV-447:69-90): Manual IMDb bypass (OmdbMetadataProvider.cs:69-72), public API contracts, and DEV-288 recommendation algorithms remain untouched; extra OMDb fallback requests on rejected/suspicious direct hits are the accepted trade-off.

### Implications
Scope is settled as ruled; no implementation or Gate 1 approval is implied.

---
## Q2 - Automatic eligibility

### Question
[from Keel] DEV-447 Q2/12. Q1 recorded and read back in specs/DEV-447/CONCLUSIONS.md. Decide the automatic eligibility rule: I recommend require an explicit movie type for direct responses and search candidates; reject episode, series, missing or unknown type. For direct hits use the recleaned lookup and existing TitleMatcher.Score/year parse semantics, require non-null score >= configured MatchThreshold, and require not suspicious under Q1. Search candidates require movie type plus existing score/year selection. Rejected direct hits enter the existing finite search variants instead of returning Found. Manual IMDb lookup remains exempt. This trusts verified movie identity over OMDb t= success and avoids threshold or TMDb changes; cost is fail-closed handling of malformed types and extra bounded requests. Approve or amend, with cited basis. Please send full ruling to Keel or supply its exact file path; do not edit shared artifacts.


### Patron answer
Approved: require explicit Type=movie (case-insensitive) on direct t= responses and search candidates (rejecting episode, series, missing, or unknown types), require direct t= hits to pass TitleMatcher.Score >= MatchThreshold against Recleaned(stored) and not be suspicious under Q1, route rejected direct hits into FallbackAsync, and keep ManualImdbId exempt.
- Direct-hit verification & fallback routing (DEV-447 Fix 1-3, recon-DEV-447:24-25,124-127; OmdbMetadataProvider.cs:69-84,93-123): Direct t= responses currently bypass scoring at OmdbMetadataProvider.cs:82-84; verifying Type=movie, non-null TitleMatcher.Score >= omdbOptions.MatchThreshold against Recleaned(stored), and Q1 non-suspiciousness before accepting Found routes bad direct hits (e.g. Memento tt24735934, O Brother tt24456948) into FallbackAsync.
- Search candidate filtering (DEV-447 Fix 1, recon-DEV-447:27-30,125; OmdbMetadataProvider.cs:158-188; OmdbMetadataProvider.Mapping.cs:150-209): Adding Type binding to Response/SearchHit and requiring explicit movie type before BestCandidate/ScoreCandidate enforces Fix bullet 1 (reject episodes/series and malformed/missing types fail-closed) while preserving shared MatchThreshold (OmdbOptions.cs:22, ADR 0030:21).
- Manual authority & test fixtures (OmdbMetadataProvider.cs:69-72; recon-DEV-447:62-66; §2.3 rule 6): ManualImdbId bypasses automatic eligibility unchanged, and updating existing OMDb unit/integration test payloads that omit Type or Q1 fields is in scope as forced by the DEV-447 acceptance criteria.

---
## Q3 - Fetched-detail validation and request bounds

### Question
[from Keel] DEV-447 Q3/12. Q2 recorded/read back. Decide terminal detail-validation and request bounds: I recommend applying the same Q2 automatic guard to the selected search candidate detail body after its IMDb fetch, against the recleaned original lookup, before Found; a rejected or suspicious fetched detail returns NotFound so the existing chain may try TMDb. Do not restart direct/search or recursively try the same candidate. Keep the existing finite search-variant sequence and one selected candidate detail fetch per logical fallback, no new paging or candidate fan-out. Valid direct hits still cost one request. Preserve existing quota/rate-limit/provider-unavailable/cancellation semantics and gates on every actual request, never turn those failures into NotFound. This closes the unchecked detail-body hole at recon lines 29-30 with bounded quota cost, at the cost of not trying lower-ranked OMDb candidates after a rejected detail. Approve or amend with cited basis.


### Patron answer
Approved with one zero-cost guard: validate the fetched search-candidate detail body with the Q2 rule against Recleaned(stored) (returning NotFound on rejection so the chain can try TMDb), keep one detail fetch per fallback with no paging/fan-out, exclude any imdbID already rejected on the direct t= response from BestCandidate selection, and preserve all failure/quota/cancellation semantics.
- Detail verification & chain fallthrough (DEV-447 Fix 3, recon-DEV-447:29-30,54,127; OmdbMetadataProvider.cs:190-202,253-280; ChainedMetadataProvider.cs:32-47): Checking the FetchMatchAsync detail response with the Q2 guard before FoundOutcome closes the unchecked detail-body gap at OmdbMetadataProvider.cs:190-202 and returns NotFound so ChainedMetadataProvider falls through to TMDb.
- Request bounds & direct-hit ID exclusion (DEV-447 Acceptance 1, recon-DEV-447:27-30,120-129; OmdbMetadataProvider.cs:104-188): Keeping the finite FallbackQueries loop and a single i= fetch bounds quota usage (ADR 0031), while skipping a direct-hit imdbID already rejected in the same FindAsync call prevents BestCandidate from re-selecting tt24735934 ahead of tied tt0209144 on s=Memento&y=2000 at zero extra request cost.
- Failure & quota invariants (OmdbMetadataProvider.cs:204-300; OmdbMetadataProviderFallbackUnitTests.cs:231-301,415-472): Valid direct hits remain a single request, and transport, HTTP status, malformed-JSON, cancellation, rate-limit, and quota failures continue to return their existing failure outcomes rather than NotFound.

---
## Q4 - Suspicious-field semantics

### Question
[from Keel] DEV-447 Q4/12. Q3 recorded/read back including rejected direct-ID exclusion. Decide exact suspicious-field semantics: I recommend evaluating the existing mapped OMDb IMDb rating, director and runtime values, using current mapper validity semantics rather than new numerical confidence thresholds. A field is unusable if absent, blank, N/A or unable to produce its existing valid mapped value; director requires nonblank non-N/A content; runtime requires a valid positive duration under existing parsing; zero or invalid rating follows existing mapper/domain semantics. Suspicious iff all three unusable. Missing poster alone does not reject; missing one or two fields alone does not reject. No external poster probes or new network checks. Existing unknown-year scoring remains unchanged. This bounds heuristics to ticket Fix 3; cost is accepting a movie with only one usable field if other Q2 checks pass. Approve or amend with cited basis. Do not infer new mapper facts: if the proposed exact rating/runtime reuse needs recon, identify the fact rather than inventing it.


### Patron answer
Approved: define a mapped OMDb detail record as suspicious iff all three existing mapped values are unusable (metadata.Rating is null, metadata.Directors.Count == 0, and metadata.Runtime is null), with no new numeric thresholds, no poster network probes, and no change to unknown-year scoring.
- Exact mapper & domain reuse (OmdbMetadataProvider.Mapping.cs:29-44,70-88,99-120,146-148; Runtime.cs:30-42; ImdbRating.cs:20-33): MapRuntime already requires leading digits with Runtime.TryCreate (minutes > 0), MapRating requires decimal.TryParse with ImdbRating.TryCreate (0.0..10.0, <=1 decimal place), and SplitCredits(response.Director) strips whitespace and N/A tokens into metadata.Directors.
- Conjunction & non-rejection rules (DEV-447 Fix 3, recon-DEV-447:108,127): Only the conjunction (Rating is null && Directors.Count == 0 && Runtime is null) marks a record suspicious; missing Poster or missing one or two of the three fields does not reject a candidate that otherwise passes Q2.
- Network & scoring invariance (TitleMatcher.cs:91-107; recon-DEV-447:35,89): No external poster reachability probes or extra network checks are introduced, and TitleMatcher.YearAdjustment unknown-year handling (0.0) remains unchanged.

---
## Q5 - File manifest and tests

### Question
[from Keel] DEV-447 Q5/12. Q4 recorded/read back. Decide implementation/test manifest. Recommend production edits only src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs and OmdbMetadataProvider.Mapping.cs (bind Type where needed, keep guard private/local, reuse TitleMatcher unchanged). Test edits: tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderFallbackUnitTests.cs and OmdbMetadataProviderUnitTests.cs; tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs and MetadataProviderChainCompositionTests.cs. Add deterministic synthetic stubs explicitly labeled as regressions derived from ticket, not captured real payloads: Memento[2000] wrong tt24735934 -> correct tt0209144; O Brother missing question mark wrong episode tt24456948 -> movie tt0190590. Cover movie/episode/series/missing/unknown type, score threshold equality/below, disallowed year and unknown year, each one usable field vs all three missing/N/A/invalid, detail revalidation, rejected-ID exclusion, one valid-direct request, finite fallback/one detail, no eligible -> NotFound/TMDb, manual exemption, quota/failure/cancellation preservation. Existing TitleMatcher tests remain a regression gate. Other existing OMDb fixture payload edits allowed only when forced by Q2/AC or failing gate, with explicit file and reason recorded before edit; no blanket behavior changes. No new deps, project/layer/schema/API/web/options/threshold changes. Approve or amend manifest and coverage, citing basis and care-list file scope.


### Patron answer
Approved: freeze production edits to OmdbMetadataProvider.cs and OmdbMetadataProvider.Mapping.cs (with TitleMatcher.cs unchanged) and primary test edits to OmdbMetadataProviderFallbackUnitTests.cs, OmdbMetadataProviderUnitTests.cs, MetadataProviderLookupTests.cs, and MetadataProviderChainCompositionTests.cs, permitting forced Q2/Q4 payload updates in other existing OMDb test fixtures when documented.
- Production & §2.3 care-list scope (recon-DEV-447:19-46,80-90; §2.3 items 1-6): Confining production changes to src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs and OmdbMetadataProvider.Mapping.cs keeps the eligibility guard private/local, leaves TitleMatcher.cs and OmdbOptions.MatchThreshold untouched (ADR 0030:21), and triggers zero dependency, layer, schema, API/OpenAPI/web, or LocalPlay/secret changes.
- Test manifest & forced fixture updates (DEV-447 Acceptance 1, recon-DEV-447:62-66,94,129; §2.3 item 6): Unit and WireMock tests in the four named test files cover deterministic ticket-derived regressions (Memento[2000] tt24735934 -> tt0209144; O Brother, Where Art Thou[2000] episode tt24456948 -> movie tt0190590) while TitleMatcherTests.cs runs as a regression gate; any other existing OMDb test fixture that omits Type=movie or all three Q4 fields on automatic lookups may be updated as a forced gate/AC edit with file and reason recorded.
- Coverage & mutation completeness (AGENTS.md Non-negotiables; Q1-Q4 rulings): The test matrix covers type filtering (movie/episode/series/missing/unknown), score threshold boundary (== / < MatchThreshold), year tolerance and unknown year, Q4 suspiciousness (each single usable field vs all three absent/N/A/invalid), fetched-detail revalidation, rejected direct-ID exclusion, single-request valid direct hits, finite fallback with one detail fetch, NotFound -> TMDb chain fallthrough, ManualImdbId exemption, and failure/quota/cancellation preservation.

---
## Q6 - Running-app acceptance

### Question
[from Keel] DEV-447 Q6/12. Decide running-app acceptance without changing API scope. Recon lines 69-76 establish wrong matches are Enriched, ordinary retry refuses them, and manual IMDb correction already requeues. I recommend retaining ticket AC2 as mandatory Build-workspace verification after code gates: correct /api/movies/7 via existing manual match to tt0209144 and /api/movies/29 to tt0190590, await completed enrichment, verify right identity, posters, ratings and directors in running app, record environment/head/time and before/after evidence using test-portal-policy. This exercises the supported remediation path; automatic-selection proof remains the deterministic Unit/WireMock tests, since manual correction intentionally bypasses Q2. Do not claim this manual live receipt proves automatic selection, and do not add reset SQL, new endpoint or broaden retry semantics. If runtime/keys unavailable AC2 remains pending, never silently waived. This preserves both acceptance criteria and explicit scope; cost is live correction alone not exercising automatic matcher. Approve or amend with cited basis.


### Patron answer
Approved: retain AC2 as a mandatory Build-phase running-app verification using the existing PUT /api/movies/{id}/imdb re-enrichment flow for /api/movies/7 (tt0209144) and /api/movies/29 (tt0190590) under test-portal-policy, keep AC1 Unit/WireMock tests as the deterministic proof of automatic selection, add no API/retry/SQL reset scope, and never silently waive AC2 if runtime or keys are unavailable.
- Existing product re-enrichment path & §2.3 API stability (DEV-447 Acceptance 2, recon-DEV-447:69,74-76,85,130; EnrichmentEndpoints.cs:15-29; RequestEnrichmentCommandHandler.cs:16-21; MatchMovieImdbIdCommandHandler.cs:12-26): Because wrong matches are already in Enriched state where POST /api/movies/{id}/enrichment rejects retries, using PUT /api/movies/{id}/imdb (or MatchManuallyDialog) re-enriches /api/movies/7 and /api/movies/29 in the running app to verify right posters, ratings, and directors without altering public API transition semantics or running ad-hoc SQL.
- Honest separation of evidence (DEV-447 Acceptance 1-2, recon-DEV-447:128-130; Q1-Q2 rulings): AC1 Unit/WireMock tests prove automatic non-manual selection of tt0209144 and tt0190590, while the AC2 live receipt (recording environment, HEAD, timestamp, and before/after state per test-portal-policy) proves live re-enrichment and rendering without claiming manual IMDb override exercised the automatic matcher.
- Non-waiver gate rule (AGENTS.md Verification commands): If the running app or live provider credentials are unavailable during Build verification, AC2 must be reported explicitly as pending/blocked rather than folded into a silent pass.

---
## Q7 - Gates and task ordering

### Question
[from Keel] DEV-447 Q7/12. Decide gate expectations and task ordering. Recommend Quill draft spec/plan/tasks from Q1-Q6; Keel analyze then plan challenge then spec PR/user Gate 1; no code/gates now. Build pickup first recon drift plus blocking reanalysis if contracts changed, Gauge baseline; implement regression tests and private adapter/mapping guard, preserve/update only forced fixture payloads, add WireMock/chain request proof, refactor, then Gauge full configured Phase 3 gates: Roslyn, complexity, InspectCode scoped diff (never -All), property tests, vulnerabilities, dotnet format --verify-no-changes, dotnet test, mutation for changed production C# without since, web scope-empty. Thresholds only harness.yml, never edit to pass; no changes to existing harness.yml modification. Exit 1/Could not run block; exit 2 scope-empty SKIPPED and mutation N/A stay distinct from PASS; property exit 2 accepted only recorded task-note opt-out with reason. No invented new property invariant: reuse property suite, obtain opt-out receipt if no tagged applicable tests. Then mandatory Q6 live AC2 and three-axis review/remediation, rebased fresh gates and hosted checks before awaiting-merge; user merges. Keep tests before production behavior change where feasible and no next stage without predecessor evidence. Approve or amend with cited basis.


### Patron answer
Approved: adopt the proposed Phase 1 Spec -> Gate 1 -> Phase 2/3 Build, gate, Q6 live AC2, review/remediation, and awaiting-merge sequence under the standing harness and pipeline rules.
- Phase 1 -> Gate 1 ordering (AGENTS.md Duties §3.1/§5; Q1-Q6 rulings): Quill drafts spec.md, plan.md, and tasks.md from Q1-Q6; Keel runs /speckit-analyze and plan challenge before Patron marks gate1: provisional and opens the spec PR for user Gate 1, with no production/test code or Build gates in Phase 1.
- Build & Phase 3 verification gates (AGENTS.md Non-negotiables & Verification commands; Q5-Q6 rulings): After pickup drift check and Gauge baseline, Anvil lands regression tests and the OmdbMetadataProvider/Mapping guard, refactors, and Gauge runs scoped analyzer/complexity/InspectCode checks, run-property-tests.ps1 (reusing existing suite without inventing new property frameworks; exit-2 opt-out only with recorded task-note reason), run-vulnerable-packages.ps1, dotnet format --verify-no-changes, dotnet test, and run-mutation.ps1 (no --since in worktrees), keeping harness.yml untouched and exit-2 SKIPPED/SKIP/N/A distinct from PASS.
- Live AC2, review, and merge handoff (AGENTS.md Hard rules §2.3; Q6 ruling): Mandatory Q6 running-app AC2 verification, three-axis review/remediation, and fresh rebased gates + green hosted checks precede awaiting-merge: DEV-447 (#n), with the user merging all PRs.

---
## Q8 - Closing bar, frozen scope and round cap

### Question
[from Keel] DEV-447 Q8/12 final closing decision. Recommend closing bar: retained Critical/High/Medium concrete in-scope findings block; Low gets explicit disposition and does not block unless a cited mandatory rule/gate makes it blocking. Mandatory gates and both acceptance criteria remain blocking regardless of severity. Maximum two review rounds and two fix commits per round; unresolved above-bar work at cap blocks clearance and is surfaced on PR for user review, never silently waived or a third round. Frozen scope is Q1-Q7 automatic OMDb movie/title/year/completeness validation, bounded fallback and rejected-ID exclusion, named regression/compatibility tests, and existing-path live remediation proof; anything else is a follow-up issue, not a finding in this round. No new vocabulary/domain model, ADR (size M), taste assumption or owner checkbox is proposed; §2.3 constraints remain and new contradictory requirements would block structurally. Approve closure/shared understanding for Keel to write brief.md containing every full exchange, manifest, tests, gates, ordering and these loop terms, ready for Quill drafting only (not plan freeze/Gate 1 clearance)?


### Patron answer
Approved: ratify the standing closing bar, two-round/two-commit review cap, and Q1-Q7 frozen scope; close the DEV-447 grill for Keel to write brief.md and specs/DEV-447/CONCLUSIONS.md ready for Quill drafting only (not Gate 1 clearance).
- Standing closing bar & loop cap (AGENTS.md Hard rules §2.3; task-pipeline): Retained in-scope Critical/High/Medium findings, mandatory gates, and both DEV-447 acceptance criteria block; Low findings receive explicit disposition unless a cited rule/gate makes them blocking; review is capped at 2 rounds and 2 fix commits per round, surfacing any unresolved above-bar item on the PR rather than waiving it or starting round 3.
- Frozen scope & follow-up routing (AGENTS.md Hard rules §2.3; Q1-Q7 rulings): Scope is frozen to Q1-Q7 (automatic OMDb Type=movie/title/year/Q4 completeness validation, bounded fallback with rejected direct-ID exclusion, named regression/compatibility tests, and Q6 existing-path live AC2 proof); out-of-scope Critical/High/broken-behaviour items route to follow-up tickets via Rigger and anything else is noted in CONCLUSIONS.md.
- Artifact & Gate 1 state (DEV-447 note: Size M; AGENTS.md Duties §3.1/§5): No new domain model/ADR (Size M), taste assumption ([assumed]), or §2.3 owner checkbox applies; Keel writes brief.md and CONCLUSIONS.md for Quill drafting, while gate1: provisional awaits clean /speckit-analyze and plan-challenge adjudication.

---

## Plan challenge round 1 - Patron clarifications (2026-11-03)

### Question
[from Keel] DEV-447 plan adjudication decision request. Read findings-DEV-447-Sentry F1/F4/F6/F7, findings-DEV-447-Compass F1, and brief Q2-Q5. Please rule with cited basis and report back via maestri ask Keel. 1. Recommend preserve Failed(InvalidResponse) and MissingTitle logging for Response=True bodies that cannot map, on both automatic direct and fetched-detail paths; classify/map failures before eligibility, guard only mapped Found records. Rejected mapped records retain Q2/Q3 fallback/NotFound behavior. 2. Sentry F1 counterexample is a movie-typed same-title/year wrong ID with usable Runtime: Q4 expressly accepts one usable field, so proposed universal rejection contradicts Q4. Recommend explicitly document residual identity risk; AC1 stays mandatory synthetic selection proof and AC2 mandatory live remediation, neither proves unknown incident payload causality. Require Wisp pickup recon of nonsecret Type/title/year/rating/director/runtime when credentials are available; unavailable means unknown, never fabricated; observed contract mismatch stops for reanalysis without changing AC. No denylist/new heuristic. 3. Ledger M1 complexity concern: private helper extraction stays inside the two Q5 production files, no preauthorized third file/type/layer; blocked gate prompts a new ruling rather than scope expansion. 4. Mapping edge tests in primary allowlist may use ManualImdbId exemption or eligible automatic fixtures to preserve identical mapper assertions; other fixtures remain payload-only repairs, no assertion weakening. These are clarifications of frozen scope, not ticket acceptance waivers. Approve/amend each and cite evidence. Do not edit shared artifacts.

### Patron answer
Approved: ratify all four plan-adjudication clarifications within Q1-Q8 frozen scope (preserve Failed(InvalidResponse)/MissingTitle before eligibility on automatic direct and fetched-detail paths; document residual identity risk under Q4 with mandatory AC1/AC2 and nonsecret Wisp pickup recon without denylists or new heuristics; confine helper extraction to the two Q5 production files; permit ManualImdbId exemption or eligible automatic fixtures for primary-allowlist mapper edge tests while keeping other fixtures payload-only).
- Classification/mapping before eligibility & Ledger M1 file boundary (Compass F1, Sentry F4, Ledger M1; brief Q2-Q3, Q5; OmdbMetadataProvider.cs:196-202,253-274; OmdbMetadataProviderUnitTests.cs:152-163,667; ChainedMetadataProvider.cs:75-92): Classify and Map run before automatic eligibility on both automatic direct t= and fetched-detail i= paths so Response=True bodies with unusable/missing titles still log MissingTitle and return Failed(InvalidResponse) without falling through to fallback/TMDb, whereas mapped Found records rejected by automatic eligibility retain Q2/Q3 fallback/NotFound routing; any private helper extraction to satisfy complexity gates stays strictly inside OmdbMetadataProvider.cs and OmdbMetadataProvider.Mapping.cs, and a blocked gate requires a new Patron ruling rather than a third file/type/layer.
- Residual identity risk under Q4 & Wisp pickup recon (Sentry F1, F6; brief Status & Q1, Q4, Q6, Q7; recon-DEV-447:24-25,94,120-130): Because Q4 defines suspicious strictly as Rating is null && Directors.Count == 0 && Runtime is null, rejecting a movie-typed same-title/same-year record that has a valid Runtime would contradict Q4 and ticket Fix 3; document this residual identity risk explicitly (AC1 remains mandatory synthetic selection proof and AC2 mandatory live remediation, neither claiming unknown incident payload causality, with no ID denylist or extra heuristic), and require Wisp at Build pickup to record nonsecret live Type/title/year/rating/director/runtime for the incident IDs when credentials exist (recording unavailable as unknown, never fabricated, and stopping for reanalysis if an observed contract mismatch appears without altering AC1/AC2).
- Primary-allowlist mapper edge tests vs forced fixture repairs (Sentry F2, F7; brief Q2, Q5; OmdbMetadataProviderUnitTests.cs:90-101,363-401; §2.3 rule 6): In the four Q5 primary-allowlist test files, mapper-focused edge tests may either exercise the exempt ManualImdbId path or add eligible automatic movie/Q4 fixture fields so identical mapper assertions remain tested rather than flipping to NotFound, while any edits outside the primary four test files remain strictly documented payload-only compatibility repairs with zero assertion weakening.

### Keel drafting dispositions
- Sentry F2/F3/F5/F7: make automatic-only guard placement, valid-primary short-circuit versus rejected-detail fallthrough, valid Detail fixture fields/one-request count, and preservation of mapper assertions explicit. Q2/Q5 already require these; no new scope.
- Compass F2: new-behavior tests can be red; existing manual/failure/one-request preservation tests are green locks, per Q7 where feasible.
- Ledger M2: T003 explicitly assigns WireMock coverage to MetadataProviderLookupTests; keep existing unit HTTP stub patterns. Both AC1 titles remain required in unit and WireMock tests (Q5); T004 reuses those cases rather than duplicating them.
- Ledger L1/L3/L7: apply existing mutation static-initialization hygiene, deterministic TimeProvider/seeded Random conventions, and adapter-local mapping DTO scope; no public contract change. Wisp verifies actual DTO visibility at pickup; no guessed access modifier.
- Ledger L2: do not perform unrelated comment cleanup. Existing comment rules still govern new/changed comments; no blanket exemption for prohibited comments.
- Ledger L4: no removal of required unit/WireMock AC1 proof; clarify task reuse only. Ledger L6: retain numbered local phase headings and canonical disclaimer because task-pipeline Phase 3 step 2 dispatch depends on /implement Phase N.
- Ledger L5/Compass F3: name owners and Rigger spec-PR handoff explicitly; dotnet format authority is Q7 and task-pipeline Phase 3 step 5.
- Sentry F8: Low acknowledged out-of-scope residual; no blanket re-enrichment sweep is authorized. Q6 only corrects the two named entries. No follow-up tracker write requested or implied.

These clarifications supersede any ambiguous guard-first prose above; they do not change Q4, AC1/AC2, the two-file production manifest, closing bar or review cap. No ticket contradiction or constitution departure is approved. Gate 1 remains unapproved pending plan repair verification and Patron provisional handoff.

## Plan adjudication closure - PLAN FROZEN (2026-11-03)

Keel completed all-three-axis plan adjudication and independent repair read-back. Authoritative canvas receipt: adjudication-DEV-447. All 20 findings dispositioned; retained High/Medium drafting gaps repaired or settled by cited Patron clarification, every Low explicitly accepted/declined/acknowledged. One consolidated 10-item repair list and one single-item precision repair; both read back. No extra immutable PR review, no cap extension.

Read-only cross-artifact analysis: 15 functional requirements (FR-001 through FR-014 plus FR-004a), 7 success criteria, 21 tasks T000-T020, complete task coverage, no unmapped tasks, no unresolved ambiguity/duplication/constitution conflict/above-bar finding. FR-004a both-path failure/logging/no-TMDb tests explicitly verified in tasks.md T003 and plan.md Test Strategy. Q4 residual same-title/year wrong-identity risk remains accepted by Patron and documented, with mandatory AC1/AC2 unchanged; no universal identity guarantee, denylist, new heuristic or captured-incident claim.

Decision: PLAN FROZEN for Patron provisional Gate 1 and Rigger spec-PR preparation. This freezes planning artifacts only. Gate 1 is NOT approved or merged; no Build implementation clearance, tests, gates or live AC success is claimed. No production/test code was edited in this phase. The unrelated harness.yml change remains preserved and excluded. Rigger owns the authorized subsequent spec-only commit/PR; no commit or PR made by Keel.

Next chain: Patron gate1: provisional -> Rigger spec PR -> user merges Gate 1 -> Keel T000 verified merge receipt -> fresh Wisp pickup recon/Keel reanalysis/Gauge baseline before Build. User alone merges. The 2-round/2-fix-commits-per-round cap and closing bar remain unchanged.

---

## Gate 1 Provisional Ruling (2026-11-04)

### Patron ruling
Approved: `gate1: provisional` granted for DEV-447 (`specs/DEV-447/spec.md` status updated); ready for Rigger spec PR and user Gate 1 merge.
- Read-only cross-artifact analysis & adjudication clean (`adjudication-DEV-447:4,40-70`; `brief.md:244-252`; `spec.md:141-178`; `plan.md:35-53`; `tasks.md:61-130`): All 20 plan-challenge findings across Sentry, Ledger, and Compass are dispositioned with zero unresolved Critical/High/Medium or mandatory-rule Low items, and 15 FRs (FR-001..FR-014 + FR-004a) and 7 SCs map 100% across T000–T020 with no constitution conflict or owner checkbox.
- Checklist & user-visible outcome verified (`specs/DEV-447/checklists/requirements.md:7-37`; `spec.md:40-52`): All checklist items including "User-Visible Outcome" (`SMOKE-001`, `SMOKE-002`) are ticked, and no `[NEEDS CLARIFICATION]` markers remain.
- Scope & handoff boundary (`brief.md:44-59,250-252`; `AGENTS.md` §2.3/§3.1/§5): Production/test allowlists remain frozen to Q1–Q8, the unrelated `harness.yml` modification stays unstaged, and Gate 1 remains open for the user to merge on the spec PR before any Phase B Build work (`T000`).
---

## T000 - Verified Gate 1 spec-PR merge receipt (2026-11-04)

Receipt only; no new ruling. The DEV-447 task note records spec PR #320 merged at `b310c567` with the specification on main (task note `DEV-447`:3-6). Wisp verified pickup HEAD and locally recorded `origin/main` both at `b310c56704ad2a9f3e9ae6fba07b08cbb15561d6`, on branch `feature/DEV-447` in `F:/Dev/LamuFlix.worktrees/DEV-447`, with a clean tree (`recon-DEV-447`:5-11). The landed specification file set is byte-identical to `origin/feature/447-spec` (`recon-DEV-447`:32-34). Recon used the locally recorded ref without fetching; this receipt makes no additional remote-freshness claim (`recon-DEV-447`:41,111).

T000 is complete: the Gate 1 merge receipt required by Q7 / FR-014 is recorded. T001 remains required before T002-T020: fresh pickup recon, Keel cross-artifact reanalysis, and Gauge baseline. This receipt does not complete T001, authorize bypassing its checkpoint, or claim implementation, tests, verification gates, or live acceptance success. Earlier provisional/pending statements above describe the historical pre-merge state.

## T001 - Keel pickup cross-artifact reanalysis (2026-11-04)

### Verdict and evidence boundary

**CLEAN for artifact/contract drift; Keel portion complete. No task amendments required. T001 as a whole remains open.** Read-only analysis of brief.md, spec.md, plan.md and tasks.md against the landed-main facts supplied by Wisp; this appended receipt is the separately authorized record, not a change to the frozen decisions.

Pickup identity: F:/Dev/LamuFlix.worktrees/DEV-447, feature/DEV-447. Recon HEAD and locally recorded origin/main are b310c56704ad2a9f3e9ae6fba07b08cbb15561d6. Recon found no approved production/test or section 2.3 contract-path drift from Phase A base 010e0cb6; the landed spec set is byte-identical to the spec branch. The only other main change was DEV-452 CI timeout work (recon-DEV-447:5-11,28-42,95-116). No independent source searches, live measurements or remote-freshness claims are made here.

The Spec Kit prerequisite helper could not resolve the repository naming convention: feature/DEV-447, and the explicit SPECIFY_FEATURE=DEV-447 override, both fail its numeric-feature-name validation. This is a helper failure, not a successful scripted analysis or gate. The manual semantic analysis used the exact feature directory assigned by the Conductor; all required artifacts exist and were read. extensions.yml has no before_analyze or after_analyze hooks. Constitution validation used the worktree constitution 1.10.0.

### Specification analysis report

No unresolved requirement, ordering, duplication, ambiguity, scope or constitution finding requires a task change. Coverage below denotes planned work, not implemented or passing tests.

| Requirement | Has task? | Task IDs |
|---|---|---|
| FR-001 manual bypass | Yes | T003, T006, T007 |
| FR-002 recleaned lookup/matcher preservation | Yes | T003, T006, T007 |
| FR-003 movie Type | Yes | T003-T006 |
| FR-004 direct eligibility | Yes | T003, T006 |
| FR-004a classify/map before eligibility | Yes | T003, T006 |
| FR-005 exact suspicious conjunction | Yes | T003, T006 |
| FR-006 rejected-ID exclusion | Yes | T004, T006 |
| FR-007 bounded search/detail validation | Yes | T004-T006 |
| FR-008 rejected detail/chain fallthrough | Yes | T004, T006 |
| FR-009 request bounds/failure preservation | Yes | T003, T004, T006 |
| FR-010 file/contract boundaries | Yes | T001, T005, T006, T009, T010 |
| FR-011 required matrix/fixture compatibility | Yes | T002-T004, T007, T008, T015 |
| FR-012 synthetic automatic AC1 | Yes | T002, T004, T007 |
| FR-013 live AC2 | Yes | T017 |
| FR-014 gates/review/delivery | Yes | T000, T001, T009-T016, T018-T020 |
| SC-001 two incident selections | Yes | T002, T004, T007, T015 |
| SC-002 automatic eligibility | Yes | T003, T005-T007 |
| SC-003 exclusion/detail bounds/chain | Yes | T004, T006 |
| SC-004 preservation | Yes | T003, T004, T006, T007 |
| SC-005 running-app acceptance | Yes | T017 |
| SC-006 frozen boundaries | Yes | T001, T010 |
| SC-007 gates and review discipline | Yes | T009-T016, T018-T020 |

Metrics: 15 functional requirements plus 7 buildable success criteria; 21 tasks T000-T020; coverage 22/22 (100%); unmapped tasks 0; unresolved semantic ambiguity 0; duplication findings 0; Critical/High/Medium findings 0; constitution conflicts 0. No implementation or executable-gate result is implied.

### Recon observations and dispositions

- Historical Gate 1 pending/provisional prose in spec.md and the planning headers is superseded for pickup sequencing by the appended T000 verified merge receipt and completed T000 checkbox. Preserve the historical planning record; do not redraft the frozen spec or infer that T001 is complete (task note DEV-447:3-6,19-21; recon-DEV-447:38-41,108,117).
- Response and SearchHit are actually private nested adapter DTOs (recon-DEV-447:60-63,99). T005 already says adapter-local visibility as verified at T001; no guessed internal modifier, public API shape or generated contract change is needed.
- The two-file production boundary includes all complexity extraction. The existing third Log partial is outside it (recon-DEV-447:58); T006/T009 already prohibit scope expansion. No amendment.
- Existing Type-less fixtures and separate primary-success versus rejected-detail chain cases are already covered by T003/T004/T007/T008; the recon candidate inventory is not blanket edit permission (recon-DEV-447:78-86). No assertion weakening or additional production file is authorized.
- The old unrelated harness.yml modification is absent from this pickup tree (recon-DEV-447:37,107). Preserve the exclusion instruction as a historical safeguard; do not recreate the change. No task amendment.
- Live incident fields remain unknown because Wisp did not perform that observation; recon calls it optional, but T001 and the Patron plan-challenge clarification make it conditional on credential availability. This analysis does not infer credentials are unavailable, satisfy that conditional by omission, or waive AC2 (recon-DEV-447:109,118). Needs recon: were OMDb credentials available at pickup; if available, what are the nonsecret Type/title/year/rating/director/runtime observations for tt24735934 and tt24456948; otherwise record unavailability with the fields unknown, without reading or printing the key? Any observed contract mismatch stops for reanalysis.

Next: Conductor obtains the remaining conditional Wisp recon and Gauge pre-edit baseline before closing T001 or dispatching T002+. No new Patron decision, scope change, owner checkbox, commit, production/test edit, test execution or acceptance success is claimed.

## T001 - Conditional recon closure and Gauge baseline clearance (2026-11-04)

Keel read the appended credential-availability receipt in recon-DEV-447:120-142. Wisp records OMDb credentials unavailable in the inspected pickup configuration/environment, no authenticated live probe, and all six fields (Type/title/year/rating/director/runtime) explicitly unknown for each of tt24735934 and tt24456948. No secret was read or printed. This satisfies the unavailable/unknown branch of the frozen T001 conditional observation requirement and closes the prior needs-recon item for this bounded pickup analysis.

Evidence precision: recon-DEV-447:132 explicitly says the user-secrets store is outside the permitted roots and was not inspected. This receipt therefore does not claim that store was checked or that credentials are globally absent. Clearance relies on Wisp reporting the scoped pickup environment unavailable and retaining unknown observations, not an invented negative fact about user secrets. No live contract observation exists; no contract mismatch is established or disproved by unknown fields.

**Final Keel clearance: T001 cross-artifact reanalysis is satisfied; CLEAN for demonstrated artifact/contract drift; no task amendments. Proceed to Gauge pre-edit baseline.** T001 remains unchecked until Gauge records the required baseline and the Conductor confirms the composite checkpoint; no production implementation is cleared before that checkpoint. Mandatory T017 live AC2 remains unchanged and must be reported pending/blocked if its runtime prerequisites remain unavailable. No test/gate PASS, live acceptance success, new scope decision or commit is claimed.

---

## Phase B decision amendment: CI smoke coverage (2026-11-04)

The user approved browser coverage for SMOKE-001 and SMOKE-002 in the existing Playwright `e2e-smoke` job. T021 and T022 exercise Match Manually on isolated movies awaiting correction and verify the IMDb link, loaded poster, rating and director after enrichment. The Q5 test allowlist now includes `web/e2e/manual-match.e2e.ts`, `web/e2e/fixtures.mjs`, `web/e2e/run.mjs`, and `web/e2e/stub.mjs` for this coverage.

Keep the title-based smoke fixtures compatible with the new automatic movie-type guard. The stub returns deterministic by-ID metadata for the new cases and Playwright serves the poster images locally. T017 still supplies the live AC2 receipt and T002–T004 still supply AC1.

---

## Phase 4 scope ruling: forced Solaris arrange exception in `MetadataProviderTmdbSignalTests.cs` (2026-11-04)

### Patron ruling
Approved: ratify the narrow forced arrange exception in `tests/LamuFlix.IntegrationTests/MetadataProviderTmdbSignalTests.cs` (`The Matrix` -> `Solaris` lookup input and `SolarisSearchBody`/`StubSignals()` stub alignment alongside `"Type": "movie"` on `OmdbFoundBody`); do not revert.
- Forced gate & AC compatibility (`AGENTS.md` §2.3 item 6; `spec.md` FR-003, FR-004, FR-011; `tasks.md` T008:84,92): `OmdbFoundBody` already carried `"Title": "Solaris"`, so the FR-004 automatic `TitleMatcher.Score >= MatchThreshold` guard rejected `"The Matrix"` lookups and broke the `dotnet test` gate (T015) until the lookup title and secondary TMDb search stub matched `"Solaris"`.
- Zero assertion weakening (`CONCLUSIONS.md` Q5:66, Plan challenge clarification 4:119; `findings-DEV-447-Compass` F2:20-22; `MetadataProviderTmdbSignalTests.cs:355-440`): A payload-only edit changing `OmdbFoundBody` from `"Solaris"` to `"The Matrix"` would have forced rewriting the existing `Metadata(result).Title.ShouldBe("Solaris")` assertions, whereas the arrange alignment preserves every existing assertion (`Title`, `ImdbId`, `CollectionId`, `CollectionName`, `Signals`, `SecondaryPaths`) byte-identically.
- Chain-case separation preserved (`CONCLUSIONS.md` Q3:39, Plan challenge dispositions:122; `findings-DEV-447-Compass` F2:22,39): Both tests continue to exercise the valid primary OMDb `Found` path distinctly from the rejected-detail TMDb fallthrough case without broadening production scope or test helper abstractions.

