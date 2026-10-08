# DEV-312 — OpenAPI contract and Scalar delivery brief

Date: 2026-10-08. Phase A, Phase 2 grill closed by Patron after Q5 (5 of 12 questions). Size M, no ui tag. This brief owns the settled decisions; full question, recommendation, answer, rationale and implications are append-only in CONCLUSIONS.md Q1–Q5. Quill drafts spec.md, plan.md and tasks.md; Keel does not draft them.

## Identity and sources

- Worktree: F:/Dev/LamuFlix.worktrees/feature-312-spec; branch feature/312-spec; HEAD/base 01caa4d1b3e4e4ba7674abfd91bb6af2f3be21bc.
- Original recon-DEV-312: 143 lines, readable; DEV-312:74-78 records verified 21736-byte integrity. Duplicate recon-DEV-312-2 is ignored and must not be deleted.
- chain-4:8 assigns Phase 2 to Keel; DEV-312:16 is authoritative ticket scope; task-pipeline-2:66-78 governs deciding versus drafting; lamuflix-team-charter-2:7-31 governs authority, care items and escalation.
- specs/PRODUCT.md sections 3–5 and .specify/memory/constitution.md (IX, Technology Stack Constraints, API and Contract Rules) are the product and constitution sources.
- Pre-existing .specify/feature.json pin and harness.yml:45 web enabled true → false remain untouched. Neither is authored by this grill. Do not reset, rewrite or bundle the harness change as DEV-312 implementation.
- Phase B worktree is preserved; this brief authorizes no build there. No recon facts were searched or measured independently; the cited OD-1 owner checkbox was directly checked in the authorized worktree.

## Every grill answer and plan decisions

### Q1 — Scope and authority

Patron accepts the five ticket deliverables: Microsoft.AspNetCore.OpenApi configuration, Scalar at /scalar, committed generated web/src/api/openapi.json, Verify drift test in LamuFlix.IntegrationTests, and docs/adr/ADR-0007.md with Status Accepted. The generated document covers exactly the nine existing business operations listed below, from current DTO and TypedResults metadata. Health routes are excluded: owner-ticked specs/DEV-396/spec.md OD-1 assigns their contract chain to DEV-20, then DEV-320. Existing health mappings receive no metadata edits. No owner checkbox is added by this scope ruling.

Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore and a Verify integration compatible with xUnit v3 are approved by DEV-312:16 and constitution API docs/Test Pyramid choices. Exact compatible versions and actual integration package identifier must be pinned through Central Package Management and supported by cited plan-stage documentation. Microsoft.Extensions.ApiDescription.Server is not approved: generation is runtime/test-host based, not build-time. No npm packages, TypeScript generation, types.ts, client, MSW changes, package.json scripts or business route/status/DTO/handler changes.

CI enforcement uses the existing .github/workflows/ci.yml Test step (recon:102). The drift test generates in the host and fails on differences; no new CI job, step or workflow edit. TypeScript/client sequencing is not a constitution departure because no handwritten client or handlers are introduced. Ticket wording built-in means first-party Microsoft; no YouTrack correction is warranted. Record package-installation reality in the spec, citing recon:118, rather than editing delivery scope.

Basis: DEV-312:16; recon:31,73-82,102,118; constitution API docs, CI and API/Contract Rules; specs/DEV-396/spec.md checked OD-1; CONCLUSIONS Q1 and Q3 clarification.

### Q2 — Exposure and document contract

Patron deliberately approves the added default document route GET /openapi/v1.json under care item 4; /scalar is ticket-decided. Both documentation mappings exist only in Development. Use document name v1 and .NET 10 default OpenAPI 3.1 without a version override. Restrict document inclusion to /api business operations; choose and cite the documented OpenApiOptions mechanism during plan. Production returns 404 for both documentation routes. Production tests retain Features:LocalPlay false and never invoke the play route. No business route/versioning change, secrets, Process.Start or LocalPlay wiring change.

This refines the unspecified environment in the ticket, with no owner checkbox: Development satisfies Scalar acceptance and the committed contract serves consumers in every environment. ADR-0007 records the new Development branch. Use explicit Development test-host configuration, with a separate Production host check.

Basis: DEV-312:16; recon:71,113,119-121; constitution API docs and API/Contract Rules; CONCLUSIONS Q2.

### Q3 — Baseline and deliberate regeneration

Patron rules requirements rather than inventing Verify APIs. web/src/api/openapi.json is the sole authoritative baseline, never hand-edited. Prefer Verify targeting that exact path if documented support permits. Otherwise a derived *.verified.json supporting snapshot is allowed, never a consumer contract, only with full generated-document equality to committed openapi.json asserted in the same run after one fixed normalization. Passing tests must prevent silent divergence between all copies.

Compare the complete document. Scrub only non-contract values varying by machine/run, each explicitly named in plan.md; server URL is an example of a possible value, not advance permission to remove contract data. Never scrub paths, operations, parameters, schemas, status codes or info title/version. No AutoVerify, autoaccept or tracked-file writes in normal tests or CI. Received files remain untracked. Intentional local regeneration uses the same Development host/document path as the drift test and replaces openapi.json plus any derived snapshot together for human review in one commit. The openapi.json diff is the review surface.

Quill must read and cite Verify xUnit v3 naming and UseDirectory/UseFileName documentation through context7 to select the permitted branch, rather than invent an API. Either branch needs no further ruling; an additional scope change does. Microsoft framework documentation uses Microsoft Learn; Scalar and Verify use context7. Missing repo facts still go through the Conductor as needs recon.

Basis: ticket snapshot/drift acceptance; constitution API/Contract Rules; CONCLUSIONS Q3 clarifies Q1 ban on a divergent second snapshot.

### Q4 — File set, approach and task order

Patron approves these existing-file edits forced by the ticket deliverables:

- Directory.Packages.props: compatible pinned OpenAPI, Scalar and Verify integration dependencies.
- src/LamuFlix.Api/LamuFlix.Api.csproj: required package references.
- src/LamuFlix.Api/Program.cs: AddOpenApi, /api-only document selection, Development-only MapOpenApi and Scalar mappings.
- tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj: Verify integration and any necessary baseline/test resource wiring.

Approved new delivery files:

- tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs: document/route/drift proof and bounded test-project opt-in generation entry.
- web/src/api/openapi.json: generated authoritative contract.
- docs/adr/ADR-0007.md: Accepted, house shape described by recon:66 (Context, Decision, Consequences, ticket/date/status), recording dependency/runtime generation choice, Development exposure, drift authority and regeneration reference.

Conditional file changes are deliberate care-item-6 approvals; plan.md must cite the activating force:

- Derived *.verified.json only if the documented Q3 baseline-path branch requires it; Quill names its exact path in plan/tasks.
- .gitignore received-file exclusion only if existing rules do not cover *.received.*; forced by Q3 fail-closed/untracked-receipt requirement.
- .gitattributes line for openapi.json and any derived snapshot only if needed for fixed normalization.
- tests/LamuFlix.IntegrationTests/ApiHostFactory.cs minimal additive support only if Production absence cannot use per-test WithWebHostBuilder/UseEnvironment. Existing config values, LocalPlay false, health stub and validation flags must remain unchanged.

ApiHostCompositionTests.cs remains unchanged. No schema, new project, top-level folder, generic abstraction or architectural layer. web/src/api is a ticket-named subfolder of existing web/. No script, including scripts/local/Generate-OpenApi.ps1. Use a locally and explicitly invoked test-project opt-in path, skipped unless its documented switch is set; CI never sets it. The normal drift test never writes. Document the exact command in specs/DEV-312/quickstart.md and reference it from ADR-0007. Quill's Phase A spec/plan/tasks and supporting quickstart are drafting artifacts, distinct from implementation authorization.

Task ordering: packages and composition → integration test and export path → document and optional derived snapshot → ADR-0007 and quickstart → controlled drift negative proof → gates. Test/host wiring must exist before generating the authoritative baseline. Outside-file needs return to Patron; Quill must not fill scope gaps.

Basis: DEV-312:16; recon:66,89,109-114; Q1–Q3; CONCLUSIONS Q4.

### Q5 — Test strategy, gates and closure

Patron approves WebApplicationFactory/ApiHostFactory and real document generation, with no containers because no database or messaging operation runs (recon:89; constitution IX). Tests prove:

- Development Scalar response and its document wiring; OpenAPI version 3.1; exact path/method membership for the nine business operations and retained DTO/status metadata.
- Production 404 for /openapi/v1.json and /scalar.
- Complete committed-document equality, Verify snapshot, deterministic repeated generation, and a controlled contract mutation causing failure without baseline writes or autoaccept.
- Existing composition and full tests stay green, LocalPlay stays false, and no play execution occurs.

Phase A does not run production implementation gates. Phase B Gauge evaluates live applicability and reports build, tests, format, the three analyzer gates for changed C#, configured property/security/diff gates and any applicable web checks. Nothing is waived. API-only mutation may be N/A only if the harness exclusion remains live-valid (recon:101); otherwise report the actual mutation verdict. Scope-empty SKIPPED, configured-disabled SKIP, N/A and Could not run are separate from PASS. A disabled web gate is SKIP, never PASS. Preserve the pre-existing harness change and never add or lower a threshold to pass.

Patron confirms shared understanding reached: close brief.md at 5 of 12 questions. No remaining decisions, structural blocker, owner checkbox, taste assumption, cap fallback assumption or YouTrack change. No ASSUMPTIONS.md is needed because no taste decision was made. No new Movies domain term is introduced; OpenAPI/tooling terms do not belong in CONTEXT.md.

Basis: CONCLUSIONS Q5; recon:89,95-103; constitution IX and quality/static-analysis gates; task-pipeline-2:23-27; charter:31,78-80. The static-analysis requirement is directly available in the constitution; no outside-scope CLAUDE.md read was needed.

## Frozen scope and loop discipline

Frozen scope is the five ticket deliverables, nine-business-operation contract, Development documentation exposure, complete fail-closed Verify drift protection, explicit test-host regeneration and only the bounded file set above; anything else is a follow-up issue, not a finding in this round.

Closing bar: Critical, High and Medium findings in frozen scope block. Low is noted for owner review. Unmet acceptance and failed or unrunnable applicable gates always block. Unnecessary abstraction is Follow-up, not a fix commit. Follow-up recording obeys charter:31: ticket only for Critical/High, broken behavior or owner-requested items, after checking existing tickets; otherwise CONCLUSIONS records noted, no ticket. Rigger alone writes YouTrack after a Patron decision.

Caps: maximum two review rounds; maximum two fix commits per round; third-round findings go to owner review with the PR. Grill cap twelve, closed at five. For size M, later review adjudication requires all three Sentry/Ledger/Compass axis reports; missing axis blocks. No merge or auto-merge; owner merges. Gate 1 is not passed by this brief.

## Exact business operation set

From recon-DEV-312:73-82, with /api prefix established at :72:

| Method | Path |
|---|---|
| GET | /api/movies |
| GET | /api/movies/{id:int} (source route template) |
| POST | /api/movies/import |
| POST | /api/movies/{id}/enrichment |
| POST | /api/movies/{id}/watchlist |
| DELETE | /api/movies/{id}/watchlist |
| POST | /api/movies/{id}/play |
| GET | /api/genres |
| GET | /api/people |

Nine operations share fewer distinct paths; tests must count path/method pairs, not demand nine unique paths. Documentation routes and health routes are not included as business operations. Plan must cite framework documentation for generated route-constraint normalization; this table records the source route templates from recon rather than asserting a generated path spelling.

## Next action and boundary

Conductor asks Quill to run /speckit-specify, /speckit-plan and /speckit-tasks from this brief, original recon-DEV-312 and specs/PRODUCT.md, setting PYTHONUTF8=1 before Spec Kit. Quill reports exact files and raises needs decision gaps to Keel. Quill drafts the mandated ADR-0007 and regeneration quickstart within this settled scope. Keel then runs read-only /speckit-analyze and brief-to-plan/tasks checks, one numbered fix list per round. Plan challenge/freeze and merged spec PR Gate 1 remain separate subsequent steps. No production implementation, tracker mutation, commit, PR or merge was performed in this grill.

## Plan-challenge decision amendment — 2026-10-08

Patron ruling recorded at DEV-312:110 supersedes the mechanism in Quill research D1 and spec FR-005: implement /api-only document filtering using a document transformer in Program.cs with no MVC assembly reference. ApiHostCompositionTests.cs remains unchanged and green; ShouldInclude and its ApiDescription.RelativePath predicate are rejected. The file envelope, exact nine operations, untouched health mappings, Development-only exposure and metadata preservation remain unchanged. Basis: Q2 permits the documented OpenApiOptions mechanism; Q4 excludes edits to ApiHostCompositionTests.cs; Q5 requires existing tests green. No owner checkbox or ticket/constitution departure.

Plan challenge has all three full reports available. Keel dispositions and one bounded numbered Quill artifact fix list are appended to CONCLUSIONS.md. Plan freeze is withheld until the blocking artifact gaps are corrected and independently analyzed. This amendment records the decision before Quill changes the mechanism; it authorizes no implementation or gates.
