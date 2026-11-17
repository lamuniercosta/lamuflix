# DEV-522 — Grill closing brief

## Status and provenance

Grill closed with Patron confirmation after 7 of at most 12 questions. PLAN FROZEN after the plan challenge adjudication below. Gate 1 approval, implementation authorization, runtime acceptance, and merge clearance remain separate and open. No production code or tests were changed in this Spec phase.

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-522-spec`.
- Branch: `feature/522-spec`.
- Recon base: `2c8f0504b3c2550c8fd655eb58457fa9658f3e25`.
- Facts: `recon-DEV-522`, especially lines 24, 28–32 and 41–75; earlier tool-limited statements at lines 35–39 are superseded for code locations by lines 41–72.
- Ticket source: successful `scripts/get-task.ps1 DEV-522`, reproduced in recon line 24. Ticket output has no line numbers. It requests duplicate prevention by existing IMDb ID or path, visible duplicate enrichment failure, and cause evidence in the ticket.
- Governing evidence: `specs/PRODUCT.md:23-24,32-48`; `.specify/memory/constitution.md:491-511,561-594`, as captured in recon lines 28–31; charter §2.3 and task-pipeline Phase 2 / loop caps.
- Full Patron exchanges are included below and independently preserved append-only in `CONCLUSIONS.md`.
- The public brief location is explicitly requested by Conductor and the assigned role for this task; that instruction takes precedence over the skill default private brief location.
- Set `$env:PYTHONUTF8='1'` before Spec Kit commands and in fresh shell processes that need it. It was set for this seat's shell actions; it is not an assertion about other seats' environments.

## Frozen scope for this review loop

Import skip/reuse for existing library path/key or known IMDb identity; race-safe one-row/no-second-enrichment-enqueue behavior; truthful late-IMDb `Failed` settlement with caller-safe reason visible through the additive library response contract and existing UI; preserved retries and unique constraints; root-cause evidence recorded by Rigger; and the tests/gates below; anything else is a follow-up issue, not a finding in this round.

The loop boundary and plan are frozen by PLAN_ADJUDICATION.md; Gate 1 remains open.

No automatic historical merge, cleanup or deletion; no external metadata calls added at import; no new dependency, project, port, architectural layer, schema migration, index relaxation, or LocalPlay change. Do not broaden to another worktree or infer the historical incident cause from static code.

## Closing bar and round cap

- Every verified in-scope Critical, High, Medium and Low finding must be fixed or explicitly dispositioned with cited evidence. No undispositioned finding clears review.
- Critical/High cannot be waived away. Accepted unresolved behavior blocks delivery. A genuine out-of-scope concern follows charter follow-up policy without expanding this round.
- Size M requires all three axes: Sentry, Ledger and Compass. Missing axis is blocked, not skipped.
- At most two review rounds and two fix commits per round. Third-round remainder goes to user review on the PR without claiming closure. The cap is not acceptance.
- Scope-empty `SKIPPED (scope-empty)` is non-blocking and never PASS. Harness-disabled `SKIP` and mutation `N/A` retain their meanings. Failure and `Could not run` block; no gate threshold is lowered.

## Approach and behavior

1. Reuse existing import orchestration and persistence seams. Identify an existing movie using library path/key and IMDb ID when already available to the operation. Skip and reuse; never silently merge/delete or add an external metadata lookup to discover an ID at import.
2. Preserve database uniqueness as the concurrency backstop. Repeated or concurrent imports of the same known identity leave one persisted movie, return a deterministic skipped/already-present result through existing import semantics, and do not queue a second enrichment. Preserve the existing failed-enrichment retry path; skipping a duplicate does not remove intended retry behavior.
3. A distinct path whose IMDb identity is discovered only during enrichment can settle to `Failed` with category `duplicate_imdb_id`; it must not become a silent unenriched gap or a falsely successful result. This boundary is explicitly approved as consistent with the ticket by Patron Q2.
4. Existing summary/detail JSON responses gain nullable string fields `failureCategory` and `failureMessage`. For `Failed`, project the existing category code and its canonical caller-safe message when available. For all non-Failed states both fields are null. Never persist or expose raw exception text. Preserve status values and routes.
5. Generate OpenAPI from C# DTOs and TypeScript from OpenAPI before web consumption. No handwritten generated-type duplicates or manual generated-file edits.
6. Show the canonical failure message alongside `Failed` in the existing MovieCard and failed-enrichment section of BulkImportDialog. When details are null, retain the existing `Failed` label without inventing a cause; Q7 taste ruling is also in `ASSUMPTIONS.md`.

## Mandatory investigation gate and root-cause acceptance

The historical scan/rebuild/reimport trigger is **unproven**. Recon lines 74–75 contain no observed incident logs/database records. Static code references are not causal proof.

Before selecting or implementing a fix, reproduce repeat-import and late-IMDb paths against the real database, capture sanitized logs and row/constraint outcomes, and establish the defect mechanism. Distinguish that demonstration from the historical trigger. The later Patron plan-adjudication ruling supersedes the Q4 mechanism-only acceptance: investigate historical logs/database rows to classify the actual scan/rebuild/reimport trigger as well as reproducing the mechanism. If historical evidence is unavailable, stop with needs recon; mechanism-only evidence cannot close Done-when 3. Any future proposal to accept mechanism-only requires an owner checkbox on the PR. Patron rules on the evidence text; Rigger alone records it in YouTrack and supplies the verified receipt.

Investigation and the Rigger receipt precede implementation work. If no failing mechanism can be reproduced, or required evidence is unavailable, stop with `needs recon: <specific question>`; no invented cause or waiver, and root-cause delivery acceptance stays open. If existing ports cannot support race-safe reuse, stop with needs recon/decision; a new port cannot be added silently. Ticket-delivery changes or necessary constitution departures remain §2.3 owner escalations, never closed by a Patron answer.

## Affected-file responsibilities

These are permitted responsibilities and test locations, not an instruction to patch every file. Q4 evidence selects the exact necessary edits; Quill must label conditional tasks accordingly.

| Responsibility | Approved existing paths |
|---|---|
| Import lookup/reuse and conflict handling | `src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs`; `src/LamuFlix.Core/Features/Import/ImportLibraryFoldersCommandHandler.cs`; `src/LamuFlix.Infrastructure/Import/ScopedMovieFolderImporter.cs`; `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` |
| Preserve uniqueness, no migration/index relaxation | `src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs` is evidence of existing constraints, not a planned edit |
| Import transport, conditional on proven need | `src/LamuFlix.Api/Endpoints/ImportEndpoints.cs`; `src/LamuFlix.Api/Endpoints/BulkImportEndpoints.cs`; preserve existing routes/status codes and contract shape; any additional public import-contract change needs a separate ruling |
| Late-failure settlement, conditional on proven need | `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs`; `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs` |
| Additive failure projection | `src/LamuFlix.Api/Endpoints/LibraryResponses.cs`; `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs` |
| Generate contracts | `web/src/api/openapi.json`; `web/src/api/types.ts` |
| Existing failure UI | `web/src/library/MovieCard.tsx`; `web/src/library/BulkImportDialog.tsx` |
| Unit regressions | `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs`; `tests/LamuFlix.UnitTests/Features/Import/ImportLibraryFoldersCommandHandlerTests.cs`; `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs` |
| Real persistence / API regressions | `tests/LamuFlix.IntegrationTests/ImportMovieFolderCommandHandlerPersistenceTests.cs`; `tests/LamuFlix.IntegrationTests/ImportLibraryCommandHandlerPersistenceTests.cs`; `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs`; `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` |
| UI regressions | `web/src/library/BulkImportDialog.test.tsx`; existing `web/src/library/MovieCard.test.tsx` (extend as needed; recon web-standards update line 33) |

Q3 and Q5 are deliberate cited Patron rulings for the additive public API and AC-forced existing-file edits. No unrelated file rewrite or expansion is approved.

## Test strategy and acceptance mapping

| Scenario | Required evidence |
|---|---|
| Root cause | Real-DB mechanism reproduction, sanitized logs and row/constraint outcomes, historical-cause qualification, Rigger verified ticket receipt |
| Single and bulk import reruns of same path/key | One persisted movie, deterministic skipped/already-present result, no duplicate enrichment enqueue |
| Known IMDb identity across paths, where available to existing operation | Reuse existing movie, one row and no second enqueue |
| Concurrent path / known IMDb conflicts | Real-DB uniqueness and conflict translation leave one row and no second enqueue; orchestration unit tests alone do not satisfy this |
| Late IMDb collision on a distinct path | Truthful settled Failed status, duplicate_imdb_id category, existing canonical safe message; no automatic deletion/merge |
| Existing retry path | Failed import/enrichment retry semantics preserved independently of duplicate skipping |
| API JSON | Actual serialized summary and detail fields for Failed with available reason; nullable fallback when unavailable; both fields null for Pending/Enriched/NotFound; no raw exception leakage |
| UI | Failed reason rendered in existing views, null fallback retains Failed label, no raw exception text |
| Identity invariants | Property tests for identity/idempotency invariants using repo conventions and existing semantics |

Use existing assertion/mocking libraries, Testcontainers for real query/constraint/serialization behavior, and unit tests for orchestration. Do not mock the driver to claim persistence proof. Preserve TimeProvider and seeded randomness requirements. Do not invent new path normalization rules.

## Task ordering and gate expectations

1. Quill drafts spec, plan and tasks from this brief and recon. Gaps return as `needs decision:`; facts missing from recon return to Conductor as `needs recon:`. Quill does not fill gaps by assumption. Mark the investigation as an explicit blocking task before production work.
2. Complete read-only Spec Kit analysis against this brief, any required Palette design before plan challenge for a ui-tagged ticket, plan challenge/adjudication, and the separate Gate 1 process. This grill does not decide the ticket ui tag or waive Palette requirements.
3. In the Build workspace after Gate 1 and pickup, run the Q4 investigation and obtain the Rigger receipt first. Stop on a failed evidence gate.
4. Implement and verify import idempotency/concurrency and preserved retry behavior; prove late-IMDb settlement (change it only if necessary).
5. Implement C# DTO projection and real API JSON tests; regenerate OpenAPI and TypeScript; implement UI and tests against generated types.
6. Run configured backend verification: Roslyn analyzers, cyclomatic complexity, JetBrains InspectCode, property tests, vulnerable packages, format verification, full dotnet tests, and mutation for eligible changed production C# via `scripts/run-mutation.ps1` without Stryker since. Run web gates for changed web files. Thresholds/applicability come from harness.yml, not this brief.
7. Preserve exact gate verdicts. Mutation scope-empty SKIPPED or configured N/A is not PASS; a failed gate or Could not run blocks. No blanket property-test opt-out is approved; Q6 requires identity/idempotency properties. Complete the three-axis review and written loop bar before later delivery clearance.

No build, gate, runtime or mutation result is claimed by this Phase A document. No ADR is commissioned for this M-ticket approach, which proposes no new port or constitutional departure.

## Post-grill drafting clarification — UI test mapping

Quill asked whether the Spec Kit default mapping of `SMOKE-###` to `web/e2e/*.e2e.ts` should replace or supplement the Q5/Q6 component-test locations in T010/T012.

Patron approved retaining `web/src/library/BulkImportDialog.test.tsx` and the companion `web/src/library/MovieCard.test.tsx` where needed, without adding browser e2e files. Cover the Failed reason, caller-safe message, and null fallback in both views. Keep real database and serialized API regressions separate. Describe T010/T012 as Vitest + React Testing Library component regressions, never as executed browser e2e or browser smoke tests; update draft acceptance mappings and labels accordingly.

Basis: Q5/Q6 and the full post-grill ruling in `CONCLUSIONS.md` (Patron receipt commit `01c6d482`); Constitution Principle IX (`constitution.md:707-734`) and Technology Stack Constraints (`constitution.md:760`) require Vitest + React Testing Library + MSW and impose no browser-e2e mandate. Template e2e paths yield to this explicit task ruling. No constitutional waiver or ticket-delivery change is approved or required. This clarification does not claim plan freeze, Gate 1, or runtime verification.

## Complete grill exchanges

The following is the full append-only Q1–Q7 record copied at grill closure. Decisions above summarize these exchanges; this transcript retains every question, recommendation, answer, rationale and implication.

---

## Q1 — Ticket scope and evidence boundary

Keel question and recommendation:

> I recommend preserving the ticket scope as prevention by existing IMDb ID or file path, one movie after twice importing, a visible duplicate_imdb_id enrichment failure reason, and evidence-backed root cause recorded in the ticket. No automatic historical data merge or cleanup is assumed. Current recon identifies no implementation paths, tests, or proven cause, so concrete approach and file decisions must await expanded recon. Do you confirm this scope interpretation and the evidence boundary?

Cited basis: recon-DEV-522 lines 6, 10–18; DEV-522 task note line 12; charter §2.3.

Patron answer:

> Confirmed: preserve DEV-522 scope (duplicate prevention by existing IMDb ID or file path, twice-imported movie yields one movie, visible duplicate_imdb_id enrichment failure reason, evidence-backed root cause recorded in ticket, no automatic historical merge/cleanup assumed) and evidence boundary (concrete approach, files, tests, and proven cause await expanded recon; basis: recon-DEV-522 lines 6, 10-18; DEV-522 task note line 12; charter 2.3).

Decision and rationale: Preserve the ticket deliverables without inventing an implementation or claiming an unproven cause. Historical merge or cleanup is not assumed to be part of delivery.

Implications: The grill remains open after question 1 of at most 12. Approach, affected files, tests, gate expectations, ordering, closing bar, and review round cap are not yet settled. The closing brief must await the remaining decisions; this record does not freeze the plan or clear Gate 1. Expanded recon must identify the current import and enrichment paths, duplicate identity behavior, visible failure reporting, relevant tests and governing product/constitution constraints. Root-cause claims require evidence; absent evidence remains explicitly unknown.

---

## Q2 — Import identity and late discovery

Keel: I recommend skip-and-reuse existing movie, never automatic merge: use existing library path/key identity at import, and IMDb identity whenever already known to that operation; preserve DB unique constraints as concurrency backstop. Repeated and concurrent imports of the same known identity must leave one movie and a deterministic skipped/already-present result, with no second enrichment queued. If identity is discovered only during enrichment, retain truthful Failed plus duplicate_imdb_id rather than claiming a successful import or auto-deleting/merging existing records. Do you approve this identity policy and its boundary, or does it conflict with the ticket requirement that twice importing leaves one movie? Cost: explicit late-discovery failure remains for a different path with no known IMDb identity; historical cleanup stays outside scope. Basis: recon lines 24, 49, 53–59, 70–71; charter §2.3.

Patron: Approved: skip-and-reuse existing movie at import by existing library path/key or known IMDb ID (never automatic merge/delete), preserving DB unique constraints as concurrency backstop, yielding one movie with deterministic skipped/already-present result and no second enrichment queued on repeated/concurrent import; retain truthful Failed plus duplicate_imdb_id when IMDb collision is discovered only during enrichment for a distinct path without known IMDb identity (does not conflict with ticket Done-when; basis: recon-DEV-522 lines 24, 30-31, 49, 53-59, 70-71; charter 2.3).

Rationale and implications: Known identities are idempotent and race-safe; enrichment-time discovery is a failed state, not a silent gap. No retrospective merge or deletion is authorized. Existing failed-enrichment retry behavior must remain separate from duplicate import scheduling.

---

## Q3 — Caller-safe failure contract and existing UI

Keel: Approve an additive public failure contract on existing library summary/detail responses: expose a nullable caller-safe failure category code and message for Failed enrichments, including duplicate_imdb_id and the existing canonical safe message; clear/omit stale failure details for other states, preserve existing status enum and routes. Exact DTO member names will be failureCategory and failureMessage in JSON, nullable strings, with code from the existing category and safe message from its canonical mapping; no raw exception text. Generate OpenAPI then TS from C# DTO and show reason alongside Failed in MovieCard and failed-enrichment section of BulkImportDialog, using existing UI rather than a new screen. Recommendation closes proven visibility gap (recon 59, 62–66); cost is additive API fields and generated client refresh. Deliberate charter §2.3 API ruling requested, also approve narrowly necessary source edits in these existing files as AC-forced; no new dependency/project/port/schema or LocalPlay change proposed. Basis: recon 24, 28–32, 58–66.

Patron: Approved (charter 2.3 items 4 and 6): expose additive nullable JSON strings failureCategory and failureMessage on existing library summary/detail DTOs for Failed enrichments (using existing EnrichmentFailureCategory code such as duplicate_imdb_id and its canonical caller-safe message, never raw exception text; cleared/null for non-Failed states; preserving existing status enum and routes), regenerate OpenAPI and TypeScript contracts, and approve AC-forced edits to existing C# DTO/endpoint files, MovieCard.tsx, and BulkImportDialog.tsx (plus tests) with no new dependency/project/port/schema or LocalPlay change (basis: recon-DEV-522 lines 24, 28-32, 58-66; charter 2.3).

Rationale and implications: Persisted status already exists, but the library DTO lacks the failure reason. An additive projection and generated contract update close that gap without inventing a new state, route, or screen. Both JSON fields are null for non-Failed states.

---

## Q4 — Root-cause evidence gate

Keel: Recon 74–75 contains static code evidence only and no incident logs/database records. I recommend a mandatory pre-implementation investigation task: reproduce repeat-import and late-IMDb paths against a real DB, capture sanitized logs plus row/constraint outcomes, distinguish demonstrated mechanism from the unproven historical incident, and have Rigger record the cited causal evidence in the ticket under your ruling. A bounded reproduction proving the defect mechanism satisfies cause evidence; never label it proof of the historical scan/rebuild/reimport trigger. If reproduction finds no failing mechanism or requires unavailable incident data, implementation pickup stops with needs recon and the delivery root-cause AC remains open; no invented cause or waiver. Cost: actual fix selection is gated on evidence rather than a guessed root cause. Do you approve this evidence and stop bar as satisfying ticket scope without changing its delivery? Basis: recon 24, 53–59, 70–75 and Q1.

Patron: Approved: require a mandatory pre-implementation investigation task reproducing repeat-import and late-IMDb paths against a real DB, capturing sanitized logs and row/constraint outcomes, distinguishing demonstrated defect mechanism from unproven historical incident trigger, and recording the cited causal evidence in YouTrack via Rigger; bounded reproduction proving the defect mechanism satisfies the root-cause AC without altering ticket delivery, whereas inability to reproduce or missing incident data stops with needs recon (no invented cause or waiver; basis: recon-DEV-522 lines 24, 53-59, 70-75; Q1 ruling; charter 2.3).

Rationale and implications: Historical causation remains unknown. The plan may describe an investigation gate, but no fix proceeds on a guessed cause and delivery cannot close without the evidence receipt. No YouTrack write is delegated to Keel or Quill.

---

## Q5 — Approach and affected-file envelope

Keel: Approve bounded implementation approach/file responsibilities after Q4 passes: coordinate identity lookup/reuse and duplicate-conflict translation in the existing ImportMovieFolderCommandHandler, ImportLibraryFoldersCommandHandler, ScopedMovieFolderImporter and EfMovieRepository; preserve MovieConfiguration unique indexes, existing retry semantics and enrichment state machine. Touch ImportEndpoints/BulkImportEndpoints only if needed to carry existing import results without inventing status codes/routes; any unestablished public import-contract change returns for a separate ruling. ProcessEnrichmentCommandHandler and EnrichmentFailureCategory change only if the reproduction proves existing late-failure settlement needs repair. LibraryResponses/LibraryEndpoints project Q3 fields, then regenerate web/src/api/openapi.json and types.ts, then MovieCard/BulkImportDialog render existing canonical message. Tests stay in recon-named suites plus a companion MovieCard.test.tsx if needed. No migration/index relaxation, external metadata call added to import, new dependency/project/port/layer, LocalPlay work, or unrelated rewrite. If existing ports cannot support race-safe reuse, stop needs recon/decision rather than adding a port silently. Cost: a deliberately gated file envelope; Quill must mark conditional edits and cite recon rather than pretend exact patches are settled. Basis: recon 46–72 and constitution closed-port rule at recon 29. Approve this file/approach envelope as AC-forced under charter §2.3?

Patron: Approved (charter 2.3 AC-forced envelope): coordinate identity lookup/reuse and duplicate-conflict translation in ImportMovieFolderCommandHandler, ImportLibraryFoldersCommandHandler, ScopedMovieFolderImporter, and EfMovieRepository while preserving MovieConfiguration unique indexes, retry semantics, and enrichment state machine; keep ImportEndpoints/BulkImportEndpoints and ProcessEnrichmentCommandHandler/EnrichmentFailureCategory edits strictly conditional on proven need (any new public import-contract shape requires a separate ruling); project Q3 fields in LibraryResponses/LibraryEndpoints, regenerate web/src/api/openapi.json and types.ts, render the canonical message in MovieCard/BulkImportDialog, and keep tests in recon-named suites plus MovieCard.test.tsx if needed, with no schema migration/index relaxation, external metadata call at import, new dependency/project/port/layer, or LocalPlay change (stop with needs recon/decision if existing ports cannot support race-safe reuse; basis: recon-DEV-522 lines 29, 46-72; charter 2.3).

Rationale and implications: Reuse existing seams and preserve persistence invariants. These are permitted responsibilities, not an assertion that every listed file needs a patch. Any expansion beyond this envelope needs evidence and a ruling; a constitution departure or changed ticket delivery remains an owner escalation.

---

## Q6 — Acceptance coverage, gates, and ordering

Keel: Approve this acceptance/verification plan and ordering: first Q4 real-DB mechanism evidence gate and Rigger ticket receipt; then import idempotency with regressions for single/bulk reruns, known IMDb identity across paths where available, concurrent path/known-IMDb conflicts (one persisted movie and no duplicate enqueue), distinct late-IMDb collision Failed with safe reason, and existing failed-import retry behavior preserved; use Testcontainers for DB constraints/translation, existing unit suites for orchestration, real API JSON assertions for both summary/detail nullable failure fields across Failed and non-Failed states, and UI tests for Failed reason, no raw exception leakage, and null fallback. Generate DTO to OpenAPI to TS before UI. Verify all configured backend gates plus web gates for eventual implementation, mutation on eligible changed production C# with no threshold relaxation, and property tests for identity/idempotency invariants where supported. Scope-empty SKIPPED, harness-disabled SKIP, mutation N/A retain exact labels and are never PASS; failures and Could not run block. Phase A itself is docs-only and cannot claim these runtime gates passed. Cost: DB/concurrency tests are slower but prove actual persistence behavior. Basis: recon 29–31, 53–72, task-pipeline Phase 3, Q2–Q5. Approve this test and dependency-order decision?

Patron: Approved: enforce the dependency order (Q4 real-DB mechanism evidence gate and Rigger YouTrack receipt first; then import idempotency/concurrency, late-IMDb collision Failed settlement, and preserved retry semantics; then C# DTO -> OpenAPI -> TS contract generation before UI), verify with Testcontainers for DB constraint/translation, unit suites for orchestration, real API JSON assertions for nullable failureCategory/failureMessage across Failed and non-Failed states, UI tests for Failed reason/no raw exception leakage/null fallback, property tests for identity/idempotency invariants, and full backend/web/mutation gates with exact exit-2 labels (Phase A remains docs-only; basis: recon-DEV-522 lines 29-31, 53-72; task-pipeline Phase 3; Q2-Q5 rulings; charter 2.3).

Rationale and implications: Real database behavior and persisted API JSON must be tested directly. Generated contracts alone do not prove runtime projection. Runtime verification remains a future Build-workspace obligation, not Phase A evidence.

---

## Q7 — Review loop, frozen scope, and shared understanding

Keel: Approve loop discipline and shared understanding to close the grill. Recommended closing bar: every verified Critical/High/Medium/Low finding in frozen scope must be fixed or explicitly dispositioned with cited evidence under the written bar; no undispositioned finding clears review. Critical/High cannot be waived away; accepted unresolved behavior blocks delivery, and a genuine out-of-scope concern follows charter follow-up policy without expanding this round. Require all 3 review axes for M; max 2 review rounds, max 2 fix commits per round, third-round remainder goes to user PR review without claiming closure. Frozen scope: import skip/reuse for existing path/key or known IMDb identity, race-safe one-row/no-second-enqueue behavior, existing late-IMDb Failed settlement and safe reason visible via Q3 additive DTO/generated contract/existing UI, preserved retries and constraints, root-cause evidence and Rigger ticket recording, plus Q6 tests/gates; anything else is a follow-up issue, not a finding in this round. No historical cleanup, new metadata calls/dependencies/ports/layers/schema/LocalPlay work. Null failure details retain the existing Failed label (taste assumption, no invented reason). Approve these terms and confirm Q1–Q7 provide shared understanding for Keel to write brief.md for Quill, with Q4 investigation still an explicit implementation gate and no claim of plan freeze/Gate1/runtime acceptance? Basis: Q1–Q6, charter §2.3, task-pipeline loop caps.

Patron: Approved: confirm closing bar, 3-axis M review, max 2 review rounds / max 2 fix commits per round, frozen scope, null failure-details fallback to existing Failed label ([assumed] taste), and shared understanding across Q1-Q7 for Keel to write specs/DEV-522/brief.md for Quill (with Q4 investigation remaining an explicit implementation gate and no claim of plan freeze, Gate 1, or runtime acceptance; basis: Q1-Q6 rulings; charter 2.3; task-pipeline loop caps).

Rationale and implications: All severities require explicit disposition, accepted unresolved behavior blocks delivery, and the round cap does not imply clearance. The grill is closed at seven questions; Quill can draft from the brief. Plan challenge, plan freeze, Gate 1, investigation, implementation and runtime acceptance remain separate subsequent states.



## Plan adjudication decisions — 2026-11-17 (authoritative amendments)

These decisions supersede conflicting earlier drafting summaries and preserve the original grill transcript as history. Basis: all 18 findings in the Ledger/Compass/Sentry notes, updated recon (failure projection lines 5–16; web standards lines 22–33), and Patron ruling in CONCLUSIONS.md:97–108 (commit ca667dc7).

- **Q4 correction**: T001 must investigate available incident logs/database rows for the actual scan/rebuild/reimport trigger and separately reproduce the mechanism. Historical evidence unavailable, failure to reproduce, or missing required evidence stops with `needs recon:`. Mechanism-only evidence cannot close Done-when 3. Rigger records Patron-ruled evidence with a verified ticket receipt. No scope reduction or constitution departure is proposed; no current owner waiver is needed. A future mechanism-only acceptance proposal requires an owner checkbox and blocks Gate 1 until answered.
- **Q5 extension**: Approve only `src/LamuFlix.Core/Ports/MovieSummary.cs`, `src/LamuFlix.Core/Ports/MovieDetails.cs`, and `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` in addition to the existing envelope, solely to carry existing nullable LastFailureCategory through existing read models. The category is persisted via EfMovieRepository/MovieConfiguration and converted by EnrichmentFailureCategoryConverter. API mapping uses Code/SafeDescription only for Failed; null otherwise. No new port, schema, layer, or dependency.
- **Investigation outputs**: T001 records the enqueue call sites and ordering relative to durable save/conflict translation, bulk per-folder/request commit and visibility boundaries, and the concrete existing IMDb carrier (including whether ManualImdbId participates and how its uniqueness is translated). Do not invent an IMDb input or metadata lookup. A carrier absent from existing import inputs must be documented; affected conditional scenarios require an explicit evidence-backed applicability disposition, never a silent N/A or dropped ticket requirement. If existing seams cannot deliver required behavior, stop needs recon/decision.
- **Concurrency and phase matrix**: T003 counts actual enqueue calls (at most one) for concurrent winners and losing uniqueness/conflict translation paths. Known-IMDb import collision returns existing skipped/already-present semantics for the existing movie, with no second row or Failed settlement for the second path. Late enrichment collision remains Failed/duplicate_imdb_id with the canonical message and no merge/delete. T004 and T007/T008 must implement/prove their respective sides independently.
- **Retry/bulk/path coverage**: T006/T007 prove duplicate_imdb_id retry does not add a row, loop enqueue indefinitely, or leave Pending: existing non-retryable behavior may no-op or resettle the same Failed category/message. T003 covers two new paths with the same already-known IMDb within one bulk request and verifies remaining items retain existing processing semantics. Use existing path/key equivalence: variants matching the existing key skip; variants still distinct under existing rules follow late-discovery Failed semantics. No new normalization.
- **Standards decisions**: Four spec vocabulary phrases are explanatory labels only, prohibited as new domain/code identifiers; API fields express existing EnrichmentFailureCategory Code/SafeDescription, so no new CONTEXT term is required (Patron ruling). SMOKE component regression mapping is an explicit Patron-approved departure from the browser e2e convention, not a claim of browser execution. T014 checks existing import/library-path e2e assertions for affected Failed text and records compatibility or requests a ruling before out-of-envelope edits.
- **Tests and gates**: Extend existing MovieCard.test.tsx. T013 uses the existing FsCheck unit-test project with a new `tests/LamuFlix.UnitTests/Features/Import/ImportIdentityPropertyTests.cs` if needed: in-memory properties only; database behavior belongs to T003. Remove misleading parallel markers. T014 runs `npm run lint`, `npx tsc --noEmit`, `npm run test:run`, `npm run build` from web (scripts/run-web-gates.ps1:102–128); CI uses npm test. Keep `dotnet format --verify-no-changes` because CI requires it at ci.yml:59–60. No configured OpenAPI drift command was found in the authorized harness/script/CI roots (recon line 24); do not invent one. Verify generated contracts by regeneration and review of the generated diff; generation procedure must be established at pickup before T011 if absent.
- **Evidence retention**: Real-DB reproduction code becomes durable T003/T007 regressions in the named Testcontainers suites, not throwaway scripts. Sanitized logs and row/constraint summaries accompany the ticket receipt and review evidence package; redact credentials, connection strings, and unsafe identifying paths.

**PLAN FROZEN**: All 18 challenge findings are dispositioned in PLAN_ADJUDICATION.md. This freezes the plan only. T000/Gate 1 and every Build task remain unchecked; investigation, gates, runtime acceptance, review closure and merge clearance are not claimed.
