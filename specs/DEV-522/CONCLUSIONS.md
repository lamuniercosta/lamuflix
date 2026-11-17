
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

---

## Post-grill Drafting Ruling — SMOKE task mapping vs brief-approved UI component tests

Keel question and recommendation:

> Quill `DRAFTING_RECEIPT.md` raises Spec Kit default `SMOKE-###` to `web/e2e/*.e2e.ts` mapping versus settled brief Q5/Q6 approved test locations `BulkImportDialog.test.tsx` and optional `MovieCard.test.tsx`. Recommendation: retain brief-named component regressions only for T010/T012, covering Failed reason, safe message and null fallback in both views; retain real DB and serialized API tests separately. Do not claim component tests as browser e2e or browser smoke execution.

Patron ruling:

> Approved: retain the brief-approved Vitest + React Testing Library component regressions (`web/src/library/BulkImportDialog.test.tsx` and `web/src/library/MovieCard.test.tsx`) for T010/T012 without adding unapproved `web/e2e/*.e2e.ts` files, keep real DB (Testcontainers) and serialized API (`WebApplicationFactory`) tests separate, and describe T010/T012 honestly as component regressions rather than browser e2e/smoke execution.
> - Constitution Principle IX (`constitution.md:707-734`) and Technology Stack Constraints (`constitution.md:760`) mandate Vitest + React Testing Library + MSW for the web app and contain no browser-e2e or `web/e2e/*.e2e.ts` requirement, so no constitutional e2e mandate is waived.
> - Spec Kit's `.specify/templates/spec-template.md` / `tasks-template.md` `web/e2e/*.e2e.ts` wording is a template default, superseded here by the frozen Q5/Q6 file envelope (`specs/DEV-522/brief.md`, `specs/DEV-522/CONCLUSIONS.md` Q5–Q6, `recon-DEV-522:65-72`).
> - Charter §2.3 item 6 restricts touched files to the approved AC-forced envelope.


---

## Plan Challenge Adjudication Ruling — Q5 projection read-models, Q4 historical trigger gate, and Principle VIII vocabulary

Keel question and recommendation:

> Plan adjudication needs cited decisions on three items: (1) updated `recon-DEV-522` lines 5–16 proves persisted `Movie.LastFailureCategory` and canonical `SafeDescription` exist, while `MovieSummary`/`MovieDetails` and `EfMovieCatalog` omit the category — recommend extending Q5 only to `src/LamuFlix.Core/Ports/MovieSummary.cs`, `src/LamuFlix.Core/Ports/MovieDetails.cs`, and `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` to carry the existing nullable category through existing projections, retaining `Failed`-only `Code`/`SafeDescription` and existing routes/statuses, with no new port/schema/layer; (2) Compass H1 challenges Q4 mechanism-only acceptance against ticket Wanted actual scan/rebuild/reimport cause — recommend preserving full ticket delivery so T001 explicitly investigates historical logs/DB to classify the actual trigger, separately reproduces the mechanism, and stops `needs recon` if historical evidence is unavailable (mechanism-only evidence cannot close Done-when 3; any later proposal to accept mechanism-only requires an owner checkbox); (3) Ledger M4 — recommend marking the four vocabulary phrases as spec-local explanatory labels, not new domain/code terms, since API fields express existing failure category and `SafeDescription` with no new glossary concept.

Patron ruling:

> Approved: extend the Q5 AC-forced file envelope to `MovieSummary.cs`, `MovieDetails.cs`, and `EfMovieCatalog.cs` for nullable failure-category projection, correct Q4 so T001 must investigate historical log/DB evidence for the actual scan/rebuild/reimport trigger (stopping `needs recon` if unavailable; mechanism-only cannot close Done-when #3 without an owner checkbox), and classify the four spec vocabulary phrases as spec-local explanatory labels rather than new Principle VIII domain terms.
> - **Q5 projection envelope (`recon-DEV-522:5-16`, `findings-DEV-522-Compass:19-24`, charter §2.3 items 4 & 6)**: `Movie.LastFailureCategory` (`src/LamuFlix.Core/Domain/Movie.cs:51,72-127`, `EfMovieRepository.cs:121-143,1153-1156,1209-1225`, `MovieConfiguration.cs:88-93`) and `EnrichmentFailureCategory.SafeDescription` (`src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs:10-46`) already exist, so `src/LamuFlix.Core/Ports/MovieSummary.cs`, `src/LamuFlix.Core/Ports/MovieDetails.cs`, and `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` are approved as AC-forced edits solely to carry the existing nullable category into `LibraryResponses.cs` (`Failed`-only `Code` and `SafeDescription`, `null` for non-`Failed` states, preserving existing routes/statuses and adding no new port, schema, or layer).
> - **Q4 root-cause gate (`findings-DEV-522-Compass:9-15`, `recon-DEV-522:24`, charter §2.3(a))**: Preserving full ticket delivery requires T001 both to investigate available historical logs/DB rows to classify the actual duplicate trigger (folder scan vs rebuild vs reimport) and to reproduce the defect mechanism against a real DB; if historical evidence is unavailable, T001 stops with `needs recon`, mechanism-only evidence does not close Done-when #3, and any future proposal to accept mechanism-only requires an owner checkbox on the PR.
> - **Principle VIII vocabulary (`findings-DEV-522-Ledger:29-33`, `recon-DEV-522:6-10`, Constitution Principle VIII)**: `Known import identity`, `Late IMDb discovery`, `Caller-safe failure projection`, and `Defect-mechanism evidence` are spec-local explanatory labels prohibited as code/domain identifiers, and `failureCategory`/`failureMessage` project the existing `EnrichmentFailureCategory` (`Code` and `SafeDescription`) domain concept without requiring a `CONTEXT.md` addition.


---

## Gate 1 Ruling — `gate1: provisional`

Conductor request:

> DEV-522 Phase 2 step 7 Gate 1 ruling: review `specs/DEV-522/PLAN_ADJUDICATION.md` and `brief.md`, mark `specs/DEV-522/spec.md` Status as `gate1: provisional`, record Gate 1 ruling in `specs/DEV-522/CONCLUSIONS.md`, and report receipt.

Patron ruling:

> Approved (`gate1: provisional`): mark `specs/DEV-522/spec.md` `**Status**:` line as `gate1: provisional` after verifying clean Spec Kit analysis, complete plan-challenge adjudication (`PLAN FROZEN`), ticked `User-Visible Outcome` checklist items, and absence of any unresolved charter §2.3(a)/(b) owner checkbox.
> - **Plan challenge & Spec Kit analysis (`specs/DEV-522/PLAN_ADJUDICATION.md:1-45`, `specs/DEV-522/brief.md:189-202`, `DEV-522` note lines 48-63)**: Keel independently verified `/speckit-analyze` clean (23/23 requirements covered across T000–T016, 0 open findings) and adjudicated all 18 plan-challenge findings across Ledger (8), Compass (4), and Sentry (6) to `PLAN FROZEN`.
> - **User-Visible Outcome checklist (`specs/DEV-522/checklists/requirements.md:32-36`, `specs/DEV-522/spec.md:65-81`)**: All three `User-Visible Outcome` items are checked (`[x]`), covering `MovieCard` and `BulkImportDialog` visible failure-reason behavior and `SMOKE-001`/`SMOKE-002` component-regression mappings.
> - **No structural owner blocker (`specs/DEV-522/CONCLUSIONS.md:97-108`, `specs/DEV-522/spec.md:8-10`, charter §2.3)**: Full ticket delivery (including T001 historical trigger investigation plus mechanism reproduction) and constitutional compliance are preserved with no owner waiver or checkbox required; final Gate 1 approval remains the user's merge of the spec PR.
