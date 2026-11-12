# Feature Specification: Movie details and recommendation endpoint latency

**Feature Branch**: `feature/483-spec`
**Created**: 2026-11-12
**Status**: gate1: provisional
**Input**: Ticket DEV-483 (size M bug, parent DEV-491); grill-closed `brief.md` (Q1–Q6 closed 2026-11-12) including Plan Challenge Round 1 Adjudication ruling; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; `PLAN_ADJUDICATION.md` fix list 1–10; Maestri notes `DEV-483`, `recon-DEV-483` (authoritative ticket at lines 34–49; routes/handlers at 11–16; tests at 19–22; §2.3 at 25–29); `specs/PRODUCT.md`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Status; Q1–Q6; Round 1 adjudication; `PRODUCT.md` §5). Ordinary owner Gate 1 on the spec PR remains pending after Bernstein coordination / Rigger preparation-opening of that PR and verified user merge. Patron approval is not owner approval. Only Keel declares PLAN FROZEN after independent reread. Quill does not invent PLAN FROZEN or a structural checkbox.

## Clarifications

### Session 2026-11-12

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen delivery scope (three endpoint paths + proven causal collaborators), Q2 Compose measurement protocol (including details id 9999 `< 300 ms`, cold first request, 20-request batches `[assumed]`, diagnosed post-startup for-you timeout trigger + `>= 60 s` settled re-test `[assumed]`), diagnosis-first stop (`needs recon` if cause unestablished), candidate production/test file boundary, regression/Testcontainers/Compose-receipt separation, mandatory .NET gates with honest SKIPPED/SKIP/N/A/Could not run reporting, closing bar and 2×2 caps, three-axis challenge coverage, YouTrack cause documentation via Patron→Rigger with verified read-back, and no new glossary/ADR are frozen in brief Q1–Q6. Runtime cause, fix selection, which candidate files are edited, fixture IDs/history availability, and acceptance evidence remain future Phase B evidence work — not inventable here. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## Scope

**In scope** (`brief.md` Frozen scope; Q1–Q6; ticket Problem/Wanted/Done when at `recon-DEV-483` lines 42–48):

1. **Target routes** (existing; no public shape change):
   - `GET /api/movies/{id}` — known-ID success and missing ID `9999` returning 404.
   - `GET /api/movies/{id}/recommendations` — known seed with eligible results.
   - `GET /api/recommendations/for-you?take=10` — applicable history/candidate fixture.
2. **Diagnosis-first delivery**: Reproduce with the Q2 sequential protocol; isolate web-proxy time versus API/handler/persistence/query/cache/external-wait time using existing Activity/log diagnostic facilities only (exact facilities via recon — do not invent names/APIs); compare matched direct-API and web-proxy requests with transport differences recorded (port 3000 remains acceptance); investigate concurrency only when evidence indicates it and observe cancellation/abandoned work without presuming pool exhaustion — no unconditional concurrent acceptance leg. If a diagnosed trigger was absent from the baseline run, rerun the affected complete baseline protocol with it before production edits. Record an evidence-backed causal chain before choosing any production fix. Temporary instrumentation must be accounted for and removed before fix acceptance (measured build identity recorded); retained instrumentation is a production change. If the cause cannot be established, stop with `needs recon` — no speculative optimization. A proven outside-boundary cause is an expected STOP for cited Patron ruling, never permission to edit.
3. **Minimal proven causal fix** only within the Q3 candidate production seams (and only the subset proven causal):
   - `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs`
   - `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs`
   - `src/LamuFlix.Core/Features/Library/GetMovieDetailsQueryHandler.cs`
   - `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`
   - `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs`
   - `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs`
   - `src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs`
   - `src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs`
   - `src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs`
4. **Regression coverage** at the diagnosed fault seam; extend only justified cases in the existing permitted suites (`recon-DEV-483` lines 19–22). Include a deterministic performance-relevant invariant selected from the actual diagnosis (query/round-trip bounds only if causal). Preserve known/missing details, cancellation, recommendation ranking/filtering, and for-you history semantics. If cached profile source is touched, prove preservation of existing cache identity/history boundaries with regression coverage — collect missing semantics through recon; never invent per-user architecture. Persistence/query-translation behaviour uses Testcontainers (never driver mocks). Machine-dependent latency assertions belong only in the Compose acceptance receipt.
5. **Q2 Compose runtime acceptance**: Invoke-WebRequest through web port 3000 on the idle 17-movie Compose stack with no concurrent build/gate/test/competing measurement/packaging load during sample windows; before/after comparison with recorded dataset/seed/history fixture, environment, build/head, resource/container state, and safe config identity. Readiness observation without prewarming the target; pin/reset dataset/history between comparable runs. Route-specific status/payload/fixture oracles from recon: known details identity/content and existing contract; missing id `9999` existing 404 semantics; by-movie eligible results and preserved ranking/filtering; for-you fixture-specific history/candidate expectations, including non-empty where eligible. Cold first request separate; then 20 sequential samples per target with every elapsed time, status, and payload validity; report median, p95, and maximum. Repeat genres/movie-list control batches before and after; record host/control noise and investigate comparability without adding numeric acceptance thresholds. Every measured request must meet its limit (averages cannot hide violations). Reproduce/exercise the diagnosed post-startup condition for the reported for-you 30-second timeout; repeat batches after a settled post-startup/post-load interval of at least 60 seconds. Record contaminated runs and rerun rather than silently ignoring failing samples. Disposable untracked evidence under `git rev-parse --git-path dev-483-evidence` (scrubbed; no secrets/env/raw config dumps). No warmed-cache-only, synthetic, or unavailable-runtime pass. No unconditional concurrent acceptance leg.
6. **Latency targets** (every measured sample):
   - Known-ID details and id `9999` 404: strictly `< 300 ms`.
   - Both recommendation routes (including cold first and settled re-tests): strictly `< 1 s`.
7. **Cause documentation**: Route verified cause and evidence through Patron to Rigger; only Rigger updates YouTrack DEV-483 and supplies verified read-back. Required by Done when alongside gates and Q2 receipt.
8. **Mandatory verification**: build, Roslyn, cyclomatic complexity (including applicable configured refactor-gate obligation), JetBrains inspections, property tests, vulnerable-package/security checks, formatting, full tests, and pre-PR mutation for eligible production changes — using actual supported scripts and harness configuration; mutation eligibility/exemptions and exact PASS/FAIL/SKIPPED (scope-empty)/SKIP/N/A/Could not run come from actual script/config output, never assumed by project name. Never fold non-pass into PASS; never lower thresholds or invent refactor opt-outs; do not open/change preserved `harness.yml` dirt. Gates run sequentially after Q2 sample windows (never concurrent with measurement). After any remediation commit, rerun the full mandatory gate set on final delivery/review HEAD; never label an old-head run as final-head PASS. Annotate stale receipts; refresh invalidated Q2 runtime evidence and cause documentation/read-back.
9. **Contract & behaviour preservation**: Preserve routes, DTO field sets, HTTP status semantics (including 404), cancellation, ranking/filtering, and history behaviour. Preserve the C# DTO → OpenAPI → generated TypeScript contract chain. Shared collaborators belong in scope only when proven causal. Genres and movie list are diagnostic controls only. Preserve Core dependency direction and layering evidence through existing tests.
10. **Loop discipline & ordering**: Closing bar — every verified in-scope correctness, security, contract, or ticket-acceptance violation blocks at every severity including Low until fixed or proven invalid; other accepted non-blocking Low items need explicit recorded disposition. Require all three Sentry/Risk, Ledger/Standards, and Compass/Spec reports pinned to the reviewed head (Size M). At most two plan-challenge rounds and two implementation review/remediation rounds; at most two fix commits per round; one consolidated numbered fix list per round. Unresolved blockers at the cap remain open. Task order for packaging: post-fix Q2 (T007) then mandatory gates (T009) then YouTrack cause routing (T008), then review/delivery — cause documentation alone cannot yield Done when while runtime or gates remain open/failed. Frozen scope ends: anything else is a follow-up issue, not a finding in this round. Out-of-scope Critical/High or broken behaviour → Patron/Rigger follow-up ticket; other observations → noted, no ticket. Otherwise-unassigned implementation/measurement work is Build-lane owned.

**Out of scope**:

- Speculative cache, index, algorithm, or framework changes without a proven causal chain.
- New NuGet/npm dependency, architectural layer, schema/migration/index, public API shape change, `Features:LocalPlay`/secrets/`Process.Start` surface, or frontend/`web` edits (none preapproved).
- Editing tracked files outside the Q3 candidate set (including Compose/proxy configuration or startup registration) without a deliberate cited Patron ruling first.
- Claiming warmed-cache-only latency success; inventing fixture IDs or history beyond recon (collect via Phase B recon).
- New domain glossary concept, ADR, CONTEXT.md expansion, or constitution amendment (Q6).
- Merging PRs, enabling auto-merge, or treating plan freeze / Gate 1 / code-gate green as runtime acceptance or Done when without Q2 receipt + Rigger verified cause documentation.
- Inventing the runtime root cause or which candidate files will be edited before diagnosis evidence exists.

**Precedence**: Ticket Problem/Wanted/Done when (`recon-DEV-483` lines 42–48) is already approved authority for delivery outcomes (`PRODUCT.md` §3). Brief Q1–Q6, Round 1 adjudication ruling, and `CONCLUSIONS.md` govern boundaries. Ordinary owner Gate 1 remains pending. Only Keel declares PLAN FROZEN after independent reread.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md or ADR expansion)*

**Movie details latency target**
Wall-clock HTTP time for `GET /api/movies/{id}` (known-ID success and missing ID `9999` 404) through web port 3000 on the idle 17-movie Compose stack; every measured sample strictly under 300 ms.
_Avoid_: treating genres/movie-list timings as acceptance; averaging away a failing sample

**Recommendation endpoint latency target**
Wall-clock HTTP time for `GET /api/movies/{id}/recommendations` and `GET /api/recommendations/for-you?take=10` through web port 3000; every measured sample (including cold first and settled re-tests) strictly under 1 s.
_Avoid_: warmed-cache-only claims; ignoring the post-startup for-you timeout trigger

**Causal diagnosis**
Evidence-backed chain isolating where time is spent (web-proxy vs API vs handler/persistence/query/cache/external wait) that establishes the root cause before any production fix is selected.
_Avoid_: speculative optimization without reproduction; inventing a cause when evidence is missing (`needs recon`)

**Compose acceptance receipt**
Reviewable before/after timing package stored under `git rev-parse --git-path dev-483-evidence` with scrubbed exact commands, first/batch/re-test data, control batches/noise notes, validity checks against recon oracles, diagnosed trigger, causal evidence, safe config identity, and limitations — separate from unit/integration behavioural tests; no secrets/env/raw config dumps.
_Avoid_: unit wall-clock timers as latency proof; synthetic-only PASS; committing raw machine/config artifacts

Consumed vocabulary (already in use; not redefined): Movie, recommendations, for-you, movie details, Compose stack.

## What the User Will See *(mandatory)*

- **Where**: Movie details surface (existing SPA / API-backed details for a known movie).
- **What the user sees or does**: Opening movie details continues to show the same details content and 404 behaviour for unknown IDs; responses feel responsive rather than multi-second waits. No new UI chrome, fields, or routes.
- **What it needs to show up**: Default Compose stack with the idle 17-movie library; web reachable on port 3000; API healthy. No new frontend feature flags.

- **Where**: Recommendations on a movie and For You (`take=10`) surfaces.
- **What the user sees or does**: Recommendation lists and For You continue to respect existing ranking/filtering and history semantics, without the multi-second / timeout failure observed on the ticket's Compose stack.
- **What it needs to show up**: Same Compose stack with applicable recommendation seed/history/candidate fixtures collected at Phase B recon (do not invent IDs here).

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** the idle 17-movie Compose stack with web on port 3000, **When** the operator requests a known movie details ID, **Then** the response is a successful details payload (same contract as before) without multi-second delay relative to the ticket targets (full sample protocol is the Q2 receipt, not this smoke alone).
- **SMOKE-002**: **Given** the same stack, **When** the operator requests movie id `9999` details, **Then** the response is HTTP 404 and still meets the details latency target under the Q2 protocol.
- **SMOKE-003**: **Given** a known recommendation seed with eligible results and applicable for-you history/candidates, **When** the operator calls by-movie recommendations and `for-you?take=10`, **Then** each returns valid payloads under the 1 s target for every measured sample in the Q2 protocol, including cold first and settled post-startup re-tests after exercising the diagnosed timeout trigger.

Latency sample medians/p95/max and the full 20-request batches are required acceptance evidence in the Compose receipt; missing/failed runtime evidence ⇒ **Could not run**, never PASS.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reproduce and establish causal diagnosis (Priority: P1)

An operator (Build lane) establishes the Q2 sequential baseline on the idle 17-movie Compose stack under measurement isolation (no concurrent build/gate/test/packaging load), reproduces the ticketed slowness (including the post-startup for-you degradation path), isolates web-proxy versus API/handler/persistence/query/cache/external-wait time with existing Activity/log diagnostics (matched direct-API comparison with transport differences recorded; concurrency only if evidenced; cancellation/abandoned work observed), and records an evidence-backed causal chain. If a diagnosed trigger was absent from baseline, the affected baseline protocol is rerun before production edits. If the cause cannot be established, work stops as `needs recon` — no production fix is selected.

**Why this priority**: Ticket Wanted/Done when require finding and documenting the cause; Q3 forbids speculative fixes.

**Independent Test**: Publish a diagnosis receipt with baseline timings, isolation evidence, and a causal chain (or an explicit `needs recon` stop). No production code change is required for this story's independent test.

**Acceptance Scenarios**:

1. **Given** the idle 17-movie Compose stack reachable through web port 3000 with recon-supplied route-specific oracles and no competing load during sample windows, **When** Q2 baseline measurements run for the three targets plus details id `9999`, **Then** first-after-readiness (without prewarming) and 20 sequential request batch samples (with median/p95/max, status, payload validity against oracles) plus before/after genres/movie-list control batches and host/control noise notes are recorded with environment/build/head/resource/safe-config provenance under `git rev-parse --git-path dev-483-evidence`.
2. **Given** those measurements, **When** isolation proceeds, **Then** web-proxy time versus API/handler/persistence/query/cache/external-wait contributions are evidenced using existing Activity/log facilities (matched direct-API comparison with transport differences recorded; concurrency only if evidenced; cancellation/abandoned work observed) — not guessed or invented APIs.
3. **Given** isolation evidence, **When** the causal chain is written, **Then** it names the proven bottleneck seam(s) within the Q3 candidate set, or work stops as `needs recon` without selecting a speculative fix; if the diagnosed trigger was absent from baseline, the affected complete baseline protocol is rerun before production edits.
4. **Given** a proven cause that requires editing outside the Q3 candidate files or any §2.3 protected surface, **When** implementation is considered, **Then** a deliberate cited Patron ruling is obtained before editing — outside-boundary cause is never permission to edit (`blocked: structural` only for ticket contradiction or constitution departure).

---

### User Story 2 - Minimal causal fix with preserved behaviour (Priority: P1)

After diagnosis, implement only the smallest proven causal fix in the minimal subset of Q3 candidate production files, preserving routes, DTO fields, statuses, cancellation, ranking/filtering, and for-you history semantics. Add deterministic regression coverage at the diagnosed fault seam; extend existing suites only as justified. Persistence/query-translation fixes use Testcontainers.

**Why this priority**: Delivers the ticketed responsiveness while protecting contract and recommendation behaviour.

**Independent Test**: Existing and new behavioural tests at the fault seam pass; public contract unchanged; no latency wall-clock assertions in unit timers.

**Acceptance Scenarios**:

1. **Given** an established causal chain (and any required re-baseline with the diagnosed trigger), **When** production edits land, **Then** only the minimal proven causal subset of the nine Q3 candidate production files (and justified existing test suites) is changed; temporary instrumentation is removed or retained only as a gated production change with measured build identity.
2. **Given** the fix, **When** known details, missing details 404, cancellation, recommendation ranking/filtering, for-you history, and the diagnosis-selected deterministic performance-relevant invariant run, **Then** existing semantics are preserved (and if cached profile source was touched, existing cache identity/history boundaries are preserved via recon-sourced semantics — never invented per-user architecture).
3. **Given** a fix that depends on query translation or persistence serialization, **When** regression coverage is added, **Then** it uses a real database via Testcontainers — never driver mocks — and never unit wall-clock latency assertions.
4. **Given** the C# DTO → OpenAPI → generated TypeScript chain, **When** the change is reviewed, **Then** no public route/DTO/status shape change is introduced without a prior ruling (none is preapproved); Core dependency direction/layering remains evidenced through existing tests.

---

### User Story 3 - Compose after-measurement and for-you timeout exercise (Priority: P1)

After the fix, rerun the Q2 protocol: cold first request, 20-request batches, and settled (`>= 60 s`) re-tests after exercising the diagnosed post-startup for-you timeout trigger. Every measured sample must meet its ticket limit. Publish a reviewable before/after receipt. Missing or failing evidence leaves acceptance open (`Could not run` when unavailable).

**Why this priority**: Done when requires timings on the 17-movie Compose stack to meet the targets; code gates alone never close the ticket.

**Independent Test**: Compose receipt shows before/after comparison with all required samples and limits met, or records Could not run / open acceptance without claiming PASS.

**Acceptance Scenarios**:

1. **Given** the fixed build on the same idle 17-movie dataset and recorded conditions, **When** known-ID details and id `9999` 404 are measured, **Then** every sample (including cold first) is strictly `< 300 ms` with valid status/payload.
2. **Given** the same conditions, **When** by-movie recommendations and `for-you?take=10` are measured, **Then** every sample (including cold first) is strictly `< 1 s` with valid payloads.
3. **Given** the diagnosed post-startup for-you timeout trigger, **When** it is reproduced/exercised and the 20-request batch is repeated after a settled interval of at least 60 seconds, **Then** every settled-batch sample still meets `< 1 s`.
4. **Given** unavailable Compose runtime or failed evidence, **When** acceptance is judged, **Then** the result is Could not run / open — never a warmed-cache-only or synthetic PASS.

---

### User Story 4 - Cause documentation, gates, and capped review (Priority: P2)

Route the verified cause through Patron to Rigger for YouTrack documentation and verified read-back; run mandatory .NET delivery gates with honest verdicts; complete three-axis implementation review within caps; finish at awaiting-merge for the user.

**Why this priority**: Completes Done when documentation and pipeline verification without claiming merge clearance.

**Independent Test**: Rigger verified read-back on DEV-483; gate receipt with distinct verdicts; three-axis reports pinned to reviewed head; PR published for user merge.

**Acceptance Scenarios**:

1. **Given** T007 runtime outcome/status and T009 mandatory gate run/status are recorded, **When** documentation is requested, **Then** Patron routes verified diagnosis to Rigger; only Rigger updates YouTrack and returns verified read-back — cause docs alone cannot yield Done when while runtime or gates remain open/failed.
2. **Given** eligible production changes and completed T007 sample windows, **When** Gauge runs the Q4 gate set sequentially (never concurrent with measurement), **Then** exact commands, head, exits, and PASS/FAIL/SKIPPED (scope-empty)/SKIP/N/A/Could not run from actual script/config output are recorded without folding non-pass into PASS or assuming eligibility by project name.
3. **Given** Size M scope, **When** implementation review runs, **Then** Sentry, Ledger, and Compass reports are present at the reviewed head; unresolved above-bar findings at the 2×2 cap remain open for user review; after any remediation commit, full mandatory gates and invalidated Q2/cause evidence are refreshed on final HEAD — never label an old-head run as final-head PASS.
4. **Given** delivery packaging complete on final HEAD, **When** agents finish, **Then** the work stops at awaiting-merge — never merge or auto-merge; agents do not declare PLAN FROZEN or Gate 1.

### Edge Cases

- Cause cannot be established after Q2 reproduction and isolation → stop `needs recon`; do not implement guesses.
- Proven cause requires files/surfaces outside Q3 → stop for cited Patron ruling before edit (never treat outside-boundary cause as edit permission).
- Fixture IDs, oracles, or for-you history/candidates missing from recon → collect via Phase B recon; do not invent.
- Diagnosed trigger absent from baseline → rerun affected complete baseline protocol before production edits.
- Contaminated measurement samples (competing load or invalid runs) → record and rerun; never silently ignore.
- Details 404 on id `9999` is in the latency target (`< 300 ms`), not merely a behavioural check.
- Ticket's historical for-you “later timed out at 30 s” is a post-startup degradation to reproduce via the diagnosed trigger — not a mandated 30-minute idle sleep; not an unconditional concurrent acceptance leg.
- SKIPPED (scope-empty), SKIP, N/A, and Could not run never become PASS; mutation eligibility never assumed by project name.
- Pre-existing unrelated `harness.yml` modification noted at intake must be preserved (not opened or changed by this work without separate authority).
- After remediation commits: annotate stale receipts; refresh gates/Q2/cause evidence on final HEAD before packaging.
- Out-of-scope observations: Critical/High or broken → follow-up ticket via Patron/Rigger; else noted, no ticket.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Delivery MUST cover exactly the three frozen target paths — `GET /api/movies/{id}`, `GET /api/movies/{id}/recommendations`, and `GET /api/recommendations/for-you?take=10` — plus proven causal shared collaborators only (`brief.md` Q1).
- **FR-002**: System MUST preserve existing public routes, DTO field sets, HTTP status semantics (including details 404), cancellation behaviour, recommendation ranking/filtering, and for-you history semantics (`brief.md` Q1/Q3/Q4).
- **FR-003**: Implementation MUST follow diagnosis-first order: Q2 reproduce → isolate → evidence-backed causal chain → minimal fix. Unestablished cause MUST stop as `needs recon` (`brief.md` Q3).
- **FR-004**: Production edits MUST stay within the minimal proven causal subset of the nine Q3 candidate production files unless a cited Patron ruling expands the boundary (`brief.md` Q3).
- **FR-005**: No new dependency, architectural layer, schema/migration/index, public API shape, LocalPlay/secrets/Process.Start change, or frontend work is preapproved (`brief.md` Q3/Q4).
- **FR-006**: Regression tests MUST target the diagnosed fault seam, include a deterministic performance-relevant invariant selected from the actual diagnosis (query/round-trip bounds only if causal), and preserve known/missing details, cancellation, ranking/filtering, and for-you history behaviour; if cached profile source is touched, MUST prove existing cache identity/history boundaries via recon-sourced semantics (never invent per-user architecture); extend existing suites only as justified (`brief.md` Q4; Round 1 fix 2).
- **FR-007**: Behaviour depending on real query translation or persistence serialization MUST use Testcontainers — never driver mocks (`brief.md` Q4; AGENTS.md).
- **FR-008**: Machine-dependent latency assertions MUST live only in the Compose acceptance receipt, not unit wall-clock timers (`brief.md` Q4).
- **FR-009**: Q2 measurement MUST use Invoke-WebRequest through web port 3000 on the idle 17-movie Compose stack with recorded dataset/seed/history, environment, build/head, resource/container state, and safe config identity; MUST forbid concurrent build/gate/test/competing measurement/packaging load during sample windows; MUST observe readiness without prewarming the target; MUST pin/reset dataset/history between comparable runs; MUST use recon-supplied route-specific status/payload/fixture oracles; MUST repeat genres/movie-list control batches before and after and record host/control noise without new numeric acceptance thresholds; MUST store scrubbed receipts under `git rev-parse --git-path dev-483-evidence` with no secrets/env/raw config dumps (`brief.md` Q2; Round 1 fixes 3, 5, 7).
- **FR-010**: For each target and details id `9999`, measurement MUST capture the first request after readiness separately, then 20 sequential requests with every sample, median, p95, max, status, and payload validity; MUST NOT add an unconditional concurrent acceptance leg; contaminated runs MUST be recorded and rerun (`brief.md` Q2; ASSUMPTIONS; Round 1 fixes 1, 5).
- **FR-011**: Known-ID details and id `9999` 404 MUST each be strictly `< 300 ms` on every measured sample; both recommendation endpoints MUST each be strictly `< 1 s` on every measured sample, including cold first and settled re-tests (`brief.md` Q2).
- **FR-012**: Acceptance MUST reproduce/exercise the diagnosed post-startup condition for the for-you 30-second timeout and repeat the 20-request batch after a settled post-startup/post-load interval of at least 60 seconds; if that diagnosed trigger was absent from baseline, the affected complete baseline protocol MUST be rerun before production edits (`brief.md` Q2; ASSUMPTIONS; Round 1 fix 1).
- **FR-013**: No warmed-cache-only, synthetic, or unavailable-runtime PASS is allowed; missing/failed evidence leaves acceptance open / Could not run (`brief.md` Q2).
- **FR-014**: Verified cause and evidence MUST be routed through Patron to Rigger for YouTrack documentation and verified read-back only after post-fix Q2 outcome/status and mandatory gate run/status are recorded; agents other than Rigger MUST NOT write YouTrack for this Done when item; cause documentation alone MUST NOT yield Done when while runtime or gates remain open/failed (`brief.md` Q3/Q5/Q6; Round 1 fix 9).
- **FR-015**: Phase B MUST run the mandatory delivery verification required by brief.md:73-75 / Q4 and constitution.md:750-756 sequentially after Q2 sample windows: `dotnet build` (additional to all analyzer gates; never a substitute for them), `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (including applicable configured refactor-gate obligation — no hardcoded/lowered threshold or invented opt-out), `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and pre-PR `./scripts/run-mutation.ps1` when production C# is eligible per actual script/config output (never assumed by project name). Record exact commands, HEAD, exit codes, and distinct PASS / FAIL / SKIPPED (scope-empty) / SKIP / N/A / Could not run verdicts; never fold non-pass into PASS; do not open/change preserved `harness.yml` dirt. After any remediation commit, the full mandatory gate set MUST be rerun on final delivery/review HEAD; never label an old-head run as final-head PASS (`brief.md` Q4; constitution.md:751; Round 1 fixes 5, 6, 8).
- **FR-016**: Closing bar, three-axis coverage, and 2×2 caps MUST apply as ruled in Q5; anything outside Q1 frozen scope is a follow-up issue, not a finding in this round (`brief.md` Q5).
- **FR-017**: The C# DTO → OpenAPI → generated TypeScript contract chain MUST remain intact with zero `/web` edits preapproved; Core dependency direction and layering MUST remain evidenced through existing tests (`brief.md` Q4; Round 1 fix 4).
- **FR-018**: No new domain glossary concept or ADR is required or authorized for this reversible Size M diagnosis (`brief.md` Q6).
- **FR-019**: Diagnosis MUST use existing Activity/log facilities only (exact available facilities via recon), `TimeProvider` for injected time, and existing comment rules; temporary instrumentation MUST be accounted for and removed before fix acceptance with measured build identity recorded; any retained instrumentation is a production change inside the approved candidate boundary and gated; matched direct-API vs web-proxy comparison MUST record transport differences; concurrency investigation is conditional on evidence only (`brief.md` Q3/Q4; Round 1 fixes 1, 4).
- **FR-020**: Otherwise-unassigned implementation/measurement tasks MUST be attributed to the generic Build lane; only Keel declares PLAN FROZEN after independent reread; Gate 1, implementation, runtime, and merge remain distinct (`brief.md` Round 1 adjudication; Round 1 fix 10).

### Key Entities *(include if feature involves data)*

No new entities. Existing Movie details and recommendation candidate/profile/history data are read through established catalog and recommendation ports (`recon-DEV-483` lines 11–16). Schema/index changes are not preapproved.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On the idle 17-movie Compose stack through web port 3000, every measured known-ID details and id `9999` 404 sample is strictly under 300 ms (cold first, batch, and applicable re-tests).
- **SC-002**: On the same stack and protocol, every measured by-movie recommendations and `for-you?take=10` sample is strictly under 1 s (cold first, batch, and settled post-startup re-tests after exercising the diagnosed timeout trigger).
- **SC-003**: An evidence-backed causal diagnosis is recorded, and Rigger's verified read-back confirms the cause is documented on YouTrack DEV-483.
- **SC-004**: Existing behavioural semantics for details success/404, cancellation, recommendation ranking/filtering, and for-you history remain intact under automated tests at the fault seam (Testcontainers where persistence translation matters).
- **SC-005**: Mandatory delivery gates are recorded with exact commands/head/exits and honest PASS/FAIL/SKIPPED/SKIP/N/A/Could not run labels from actual script/config output; non-pass states are never reported as PASS; after remediation, final-head gate runs replace stale receipts.
- **SC-006**: Delivery finishes at awaiting user merge with three-axis review evidence at the final reviewed head; agents never merge and do not declare PLAN FROZEN or Gate 1.

## Assumptions

- Sequential measurement batch size of 20 requests per target after the cold first-request measurement (`ASSUMPTIONS.md`; Q2 `[assumed]`).
- Settled post-startup/post-batch interval of `>= 60 s` before repeating verification batches, in addition to exercising the diagnosed timeout trigger (`ASSUMPTIONS.md`; Q2 `[assumed]`).
- Historical ticket timings (details 2.2–3.3 s; 404 2.1 s; by-movie 7.2–8.4 s; for-you 0.75 s then 30 s timeout) are ticket observations from 2026-11-09, not newly reproduced facts until Phase B Q2 baseline runs (`brief.md` Authority).
- Fixture IDs and for-you history/candidate availability beyond recon will be collected in Phase B recon — not assumed here (`brief.md` Q2).
- Baseline Roslyn/complexity at intake were SKIPPED (scope-empty), not PASS; intake noted a pre-existing `harness.yml` modification that must be preserved (`brief.md`; `recon-DEV-483`).
