# DEV-312 — Phase 2 grill conclusions

Worktree: F:/Dev/LamuFlix.worktrees/feature-312-spec. Branch: feature/312-spec. Base and HEAD: 01caa4d1b3e4e4ba7674abfd91bb6af2f3be21bc. Date: 2026-10-08. Append-only decision record. Patron decides; Keel records. Original recon-DEV-312 and chain-4 read successfully. No prior grill questions existed (DEV-312:68-72). Pre-existing harness.yml:45 and .specify/feature.json modifications are preserved.

## Q1 — Frozen delivery scope

**Keel question and recommendation:** What exact frozen scope and completion bar should govern DEV-312? Recommend the five DEV-312:16 deliverables: Microsoft.AspNetCore.OpenApi configuration, Scalar /scalar, committed generated web/src/api/openapi.json, Verify drift test in IntegrationTests, accepted ADR-0007; include existing nine business and two health routes using current DTO metadata, and necessary CI drift enforcement; no TypeScript generation/client deliverable or endpoint behavior changes. Cost: client generation remains subsequent contract-chain work. Cite any constitution obligation requiring an owner checkbox rather than silently extending the ticket. Also decide whether ticket built-in wording warrants a Rigger correction without changing delivery.

**Patron initial answer:** Accept the five-deliverable scope with one amendment: health routes out, owned by DEV-20/DEV-320 per owner-ticked OD-1 at specs/DEV-396/spec.md:15. No blocker, no owner checkbox, no YouTrack change. Full ruling was offered in a temporary file outside the authorized read roots.

**Keel follow-up (Q1 completion, not another question):** Requested the full ruling inline because the temporary artifact was outside the authorized read roots; requested exact frozen scope, rationale, implications, citations and why built-in wording needs no correction. No temporary artifact was read.

**Patron full ruling (inline):**

VERDICT: accept Keel five-deliverable scope with one amendment, health routes OUT. No blocker, no owner checkbox, no YouTrack change.

BASIS: (1) Ticket DEV-312:16 names five deliverables: Microsoft.AspNetCore.OpenApi configuration, Scalar at /scalar, committed web/src/api/openapi.json, Verify drift test in LamuFlix.IntegrationTests, ADR-0007; acceptance: Scalar at /scalar, snapshot test blocks silent drift, ADR-0007 accepted. Ticket-named items are decided. Packages Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore and Verify for xUnit v3 are approved by ticket text plus constitution.md:334 and :288. Microsoft.Extensions.ApiDescription.Server (build-time generation) is NOT approved; nothing requires it. (2) Amendment: owner-ticked OD-1 at specs/DEV-396/spec.md:15 assigns both health routes in the committed openapi.json, with their TS, to DEV-20 then DEV-320; the owner word is final, so DEV-312 must not take them. The document covers the nine /api business routes at recon-DEV-312:73-82, described by current DTO and TypedResults metadata. No OpenAPI metadata added to ServiceDefaults health mappings. (3) CI drift: constitution.md:337 and :470-471 (regenerated in CI, drift fails the build) are met by the Verify drift test running in the existing ci.yml Test step at :30-31. It composes the host via ApiHostFactory, generates the document and fails on any difference. No new CI job or step. The committed web/src/api/openapi.json is the single drift baseline; no second snapshot copy that could diverge. (4) No constitution departure, hence no owner checkbox: constitution.md:471-472 requires the TS client and MSW handlers to be generated, never hand-written. DEV-312 writes no TypeScript, and OD-1 names DEV-320 as client owner, so deferring client generation is sequencing, not a departure.

FROZEN SCOPE OUT: TS generation; openapi-typescript and openapi-fetch; web/src/api/types.ts; MSW handlers; any web/package.json script; health-route documentation; any route, status code, DTO field set or handler behaviour change (task-pipeline section 2.3 item 4); build-time document generation.

COMPLETION BAR: (a) AddOpenApi and MapOpenApi wired in LamuFlix.Api; (b) Scalar UI answers at /scalar; (c) web/src/api/openapi.json committed, generated from the host, covering exactly the nine business routes; (d) the IntegrationTests drift test passes on the committed file and fails on any change to the generated document; (e) docs/adr/ADR-0007.md with Status Accepted in the house shape of ADR-0006 (recon-DEV-312:66); (f) all required gates green. ApiHostCompositionTests.cs:160-174 stays green unchanged; /openapi/v1.json and /scalar fall outside its /api selection.

BUILT-IN WORDING: no Rigger correction. It reads as first-party Microsoft as opposed to Swashbuckle, which is accurate. That the package ships via NuGet (recon-DEV-312:118, Microsoft Learn aspnetcore-openapi, Package installation) is recorded in the spec, and the wording does not change what the ticket delivers, so it fails the correction test. Noted, no ticket change.

IMPLICATIONS: three new PackageVersion rows in Directory.Packages.props; edits to Program.cs, LamuFlix.Api.csproj and LamuFlix.IntegrationTests.csproj; new web/src/api/ folder, drift test file and ADR-0007. Still open for a later question: environment gating of /scalar and /openapi (recon-DEV-312:121 versus Program.cs having no environment branch, recon-DEV-312:71). You persist this exchange in specs/DEV-312/CONCLUSIONS.md; I will not edit concurrently.

**Keel evidence check:** The cited specs/DEV-396/spec.md owner checkbox is checked in this worktree. No owner checkbox is inferred from Patron approval alone. Technical documentation facts are inherited from original recon, including https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0.

---

## Q2 — Documentation exposure and document contract

**Keel question and recommendation:** What environment exposure and precise document contract do you rule for the new documentation routes? Recommend v1 document, OpenAPI 3.1 per .NET 10 default (recon:119-120), GET /openapi/v1.json and Scalar /scalar only in Development, document selection restricted to existing /api business routes, existing service health mappings untouched. Cost: production has no interactive docs; tests explicitly use Development and prove docs absence in Production without exercising media execution. Ticket /scalar does not specify environment; Microsoft Learn sample is Development-only (recon:121). Deliberately rule under section 2.3 API-shape and security care items, cite whether this refines rather than drops ticket delivery, and choose another exposure if it would otherwise require an owner checkbox.

**Patron full answer:**

VERDICT: ACCEPT Keel recommendation as stated. Document name v1, OpenAPI 3.1 (.NET 10 default, no OpenApiVersion override). GET /openapi/v1.json and Scalar /scalar are mapped only when the environment is Development. The document holds exactly the nine existing /api business routes. ServiceDefaults health mappings stay untouched. No owner checkbox, no blocker.

BASIS: (1) Section 2.3 item 4, API shape: /scalar is named by ticket DEV-312:16 and so already decided. /openapi/v1.json is the framework default route of MapOpenApi (recon-DEV-312:119), which the ticket's OpenAPI configuration deliverable requires, so it is ruled in as the only added route. No business route, status code or DTO field set changes (Q1). If a framework default would put any non-/api endpoint into the document, restrict selection to the /api prefix through OpenApiOptions; the mechanism is the plan's choice. (2) Section 2.3 item 5, security: Development-only follows the Microsoft Learn sample (recon-DEV-312:121) and keeps the full endpoint map, including the LocalPlay-gated play route, off production hosts. It touches no secret, no Process.Start and no Features:LocalPlay wiring. Production tests must keep LocalPlay false (ApiHostFactory.cs:26) and must not call the play route. (3) Refines, does not drop, delivery: DEV-312:16 states no environment, and its acceptance (Scalar at /scalar) is met in Development, where the drift test and local use run. The committed web/src/api/openapi.json is the contract for consumers in every environment, so production loses nothing the ticket delivers. No ticket text changes and no constitution clause requires production docs (constitution.md:334, :470), so no owner checkbox is needed and no alternative exposure is required.

IMPLICATIONS: Program.cs gains its first environment branch (recon-DEV-312:71), which ADR-0007 records. The drift test and /scalar test run the host in Development (the WebApplicationFactory default). One test proves /openapi/v1.json and /scalar return 404 in Production. The generated document must be deterministic, so the drift test is stable across machines. Persist in CONCLUSIONS.md; I will not edit concurrently.

**Documentation basis:** Original recon-DEV-312:119-121 cites https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0 and https://learn.microsoft.com/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0. These are inherited recon facts, not fresh API measurements by Keel.

---

## Q3 — Baseline authority and regeneration

**Keel question and recommendation:** What baseline and intentional regeneration workflow should the Verify drift test require? Recommend committed web/src/api/openapi.json is the sole authoritative baseline; generation compares the complete document without scrubbing contract fields, fails without autoaccept or tracked-file writes, and explicit human-reviewed regeneration replaces that file using the same host/document path. Cost: Verify integration must support the exact baseline path; standard verified/received naming might otherwise create a second snapshot. Original recon:53-59 establishes Verify absent, but contains no Verify xUnit v3 API or baseline naming evidence. Do you retain sole-baseline requirement pending bounded recon, or deliberately permit a derived Verify snapshot with enforced equality to committed JSON as a supporting artifact? A semantic test API mechanism cannot be invented. Rule the requirement with basis; if facts are needed, identify exact needs recon question for Conductor and I will stop as seat discipline requires.

**Patron full answer:**

VERDICT: rule the REQUIREMENT, not the mechanism; no recon needed to rule. web/src/api/openapi.json stays the sole AUTHORITATIVE baseline. A derived Verify snapshot is PERMITTED as a supporting artifact only under the equality condition below. No blocker, no checkbox.

REQUIREMENTS: (1) Authority: the committed web/src/api/openapi.json is the contract (ticket DEV-312:16; constitution.md:334, :470). Any other baseline file is derived, never edited by hand, and never consulted by consumers. (2) Equality: if Verify naming cannot target that exact path, the drift test must assert in the same run that the generated document equals the committed openapi.json, byte-for-byte after one fixed normalisation such as line endings. A derived *.verified.* file must therefore always equal it, so the two can never diverge silently (this is what Q1 meant by no divergent second copy). If Verify can target the exact path, use no derived file. (3) Completeness: compare the whole document. Scrubbing is allowed only for values that vary by machine or run and are not contract, such as a server URL, each one named in plan.md. Paths, operations, parameters, schemas, status codes and the info title or version are never scrubbed. (4) Fail-closed: no AutoVerify or auto-accept in any committed configuration. In CI a mismatch fails the test. The test writes no tracked file; received files stay untracked through .gitignore. (5) Regeneration: one explicit, documented, human-reviewed step that generates through the same Development host and document path as the test. It replaces openapi.json, and any derived snapshot, in the same commit. The reviewer sees the contract change as a diff of openapi.json. The step is recorded in ADR-0007 or quickstart.

BASIS: ticket acceptance (snapshot test blocks silent drift) is met by (2) and (4); constitution.md:470-471 (snapshot-tested with Verify, drift fails the build) is met by Verify plus the existing CI Test step from Q1. Permitting the derived file refines the mechanism without changing delivery, so section 2.3a is not triggered.

FOR PLAN, NOT RECON: the plan stage settles which branch of (2) applies by reading the Verify xUnit v3 naming and UseDirectory/UseFileName docs through context7, and cites them there. Neither branch needs a new ruling. Persist in CONCLUSIONS.md; I will not edit concurrently.

**Effect on Q1:** Q3 clarifies the ban on a divergent second copy. A derived, equality-enforced supporting Verify snapshot is permitted; a second authoritative baseline is prohibited. Mechanism selection requires cited documentation, never an invented API.

---

## Q4 — File set, regeneration entry and ordering

**Keel question and recommendation:** Approve the bounded implementation file set and task order? Recommend edit Directory.Packages.props, src/LamuFlix.Api/LamuFlix.Api.csproj, src/LamuFlix.Api/Program.cs, tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj; create tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs, web/src/api/openapi.json, docs/adr/ADR-0007.md; conditional derived *.verified.json and .gitignore received-file exclusion only if Q3 documentation branch requires. Keep ApiHostFactory.cs and ApiHostCompositionTests.cs unchanged if per-test host configuration suffices; explicitly permit minimal ApiHostFactory support only if needed for Production host coverage or reusable generation, no dependency/config/LocalPlay changes. No CI edits, no build-time package, no generator project, no generic abstraction, no schema changes. Explicit regeneration may be a small test-project helper exposed by a documented opt-in command, separate from normal tests; do you prefer instead a single new scripts/local/Generate-OpenApi.ps1 using a test-host export path? Recommend helper and local script only if needed for reproducible configuration-free generation; otherwise documented same-host regeneration in quickstart suffices. Dependencies use compatible pinned versions with xUnit v3 integration, selected with cited docs during plan. Order: packages/composition, integration test and export path, document/optional derived snapshot, ADR/quickstart, drift negative proof, gates. Cost: precise package API/version and smallest exporter are documented plan choices, not extra delivery. Rule all conditional file edits under section 2.3(6) and no architecture-layer item, with cited basis.

**Patron full answer:**

VERDICT: APPROVE Keel file set and task order, with regeneration settled: NO new script. No blocker, no checkbox.

FILE SET (approved): edit Directory.Packages.props, src/LamuFlix.Api/LamuFlix.Api.csproj, src/LamuFlix.Api/Program.cs and tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj. Create tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs, web/src/api/openapi.json and docs/adr/ADR-0007.md. All follow from ticket deliverables DEV-312:16 plus rulings Q1-Q3, so they are in scope without a 2.3(6) ruling.

CONDITIONAL EDITS under 2.3(6), each allowed only when the named force applies, and the plan cites that force: (a) a derived *.verified.json, only on the Q3(2) branch where Verify cannot target the exact path; (b) .gitignore received-file exclusion, only if the existing .gitignore does not already cover *.received.*, forced by Q3(4); (c) a .gitattributes line pinning eol for openapi.json and any derived snapshot, only if needed for the Q3(2) fixed normalisation; (d) ApiHostFactory.cs, minimal additive support only if the Production-absence test (Q2) cannot be built per test with WithWebHostBuilder and UseEnvironment. It may not change existing configuration values, Features:LocalPlay false at :26, the health stub or validation flags. ApiHostCompositionTests.cs stays unchanged (Q1 completion bar). Anything outside this list is out of scope and comes back to Patron.

REGENERATION: no scripts/local/Generate-OpenApi.ps1. Use the test-project opt-in: an explicitly invoked path in the IntegrationTests project, such as an opt-in-gated test or helper that is skipped unless a documented local switch is set. It generates through the same Development host and document path and writes openapi.json, plus any derived snapshot. The documented command lives in specs/DEV-312/quickstart.md and is referenced from ADR-0007. CI never sets the switch, and the normal drift test never writes (Q3 items 4 and 5). Basis: smallest change that satisfies Q3(5); a script adds a tracked file and a second entry path with no added proof.

ARCHITECTURE (2.3 item 2): none triggered. No new project, top-level folder, generator project, layer or generic abstraction. web/src/api is a ticket-named folder inside existing web/ (recon-DEV-312:110). No CI edit (Q1). No build-time package (Q1). No schema change (recon-DEV-312:111).

DEPENDENCIES (2.3 item 1): already approved in Q1. Exact versions are plan choices: pinned in Directory.Packages.props, compatible with net10 and xUnit v3, with cited docs (Microsoft Learn for Microsoft.AspNetCore.OpenApi; context7 for Scalar.AspNetCore and Verify).

ORDER (approved): packages and composition; integration test and export path; document and optional derived snapshot; ADR-0007 and quickstart; drift negative proof (a mutated document fails the test); gates. Persist in CONCLUSIONS.md; I will not edit concurrently.

---

## Q5 — Proof, gates and loop terms; grill closure

**Keel question and recommendation:** Approve test strategy, gate expectations and loop terms? Recommend tests using ApiHostFactory and real OpenAPI generation without containers because no database/messaging operation runs (recon:89); prove Development Scalar response/document wiring, OpenAPI 3.1 and exact nine path/method operations including retained DTO/status metadata, Production 404 for both documentation routes, complete committed-document equality plus Verify, deterministic repeated generation, and controlled contract-mutation failure without baseline writes or autoaccept. Existing ApiHostCompositionTests and all existing tests remain green, LocalPlay false and no play execution. Phase A runs no production gates; Phase B Gauge must report build/tests/format, three analyzer gates for changed C#, property/security/diff gates as configured; API-only mutation is N/A only if exclusion remains live-valid (recon:101), otherwise actual gate verdict; web gate applies by live harness and JSON-only scope, disabled is SKIP not PASS; do not touch the pre-existing harness false change. No new thresholds, no lowering. Closing bar: Critical/High/Medium in frozen scope block, Low may be noted for owner review, unmet acceptance or failed/unrunnable applicable gate always blocks; unnecessary abstraction is Follow-up, no fix commit. Frozen scope Q1-Q4, anything else is a follow-up issue, not a finding in this round. Review maximum 2 rounds, remediation maximum 2 fix commits per round, grill maximum 12 (currently 5). Follow-up tickets only per charter:31, otherwise noted no ticket; no unrelated issue filing. Do you approve and confirm shared understanding reached so I can close brief.md, or identify the remaining decision? Cite basis.

**Patron full answer:**

VERDICT: APPROVE test strategy, gate expectations, closing bar and loop caps as stated. Shared understanding reached: close brief.md. No remaining decision, no blocker, no owner checkbox; grill closes at 5 of 12.

BASIS: (1) No containers: no database or messaging operation runs in document generation or the doc-route checks, and ApiHostFactory needs no container (recon-DEV-312:89). Constitution IX (constitution.md:288) lists Testcontainers for the persistence and messaging layers, which this work does not exercise; WebApplicationFactory and Verify are the listed API tools. (2) The proof list maps one-to-one onto Q1-Q4: Development wiring, OpenAPI 3.1 and exactly nine operations (Q1, Q2); Production 404 on both routes (Q2); full-document equality, Verify, determinism, and a mutation that fails with no write or auto-accept (Q3); ApiHostCompositionTests unchanged and green, LocalPlay false, play route never called (Q1, Q2, Q4). (3) Gates: the harness decides applicability; nothing is waived. Mutation is N/A only while the harness.yml:17-21 LamuFlix.Api exclusion is live-valid at Gauge time; otherwise its real verdict counts. A disabled web gate reports SKIP, never PASS. The pre-existing uncommitted harness.yml:45 change stays untouched. No threshold is added or lowered (CLAUDE.md static-analysis gates). (4) The closing bar and caps are a standing chain ruling, cited once and not re-ruled per ticket: Critical, High and Medium in frozen scope block; Low is noted; an unmet acceptance criterion or a failed or unrunnable applicable gate always blocks; unnecessary abstraction is a follow-up, not a fix commit; at most 2 review rounds with at most 2 fix commits per round. Out-of-scope findings follow charter:31 (follow-up ticket only if Critical or High, broken behaviour or owner-requested), otherwise one CONCLUSIONS line: noted, no ticket.

Persist and close brief.md; I will not edit concurrently.

**Closure:** Five questions used of twelve. All five exchanges persisted before brief closure. No taste decisions, cap fallback assumptions, ticket corrections, YouTrack changes or new owner checkboxes. Patron-approved scope is ready for Quill drafting, not implementation or a frozen plan. Gate 1 awaits the subsequent spec PR and owner merge.

## Phase 2 plan-challenge adjudication — 2026-10-08

Identity: feature/312-spec at 01caa4d1b3e4e4ba7674abfd91bb6af2f3be21bc in F:/Dev/LamuFlix.worktrees/feature-312-spec. All three full axis reports read: Compass (3), Ledger (0), Sentry (6). chain-4:8 read after note access was restored; its missing-access blocker is now resolved. Evidence below is from the named artifacts and reports, not new repository searches, measurements, or gate execution.

### Patron ruling and effect on settled decisions

Patron rules: the frozen file envelope does not permit editing ApiHostCompositionTests.cs. Preserve that file unchanged and green. Replace ShouldInclude with an /api-path document transformer in Program.cs, carrying no MVC assembly reference. Basis: DEV-312:110, Conductor instruction, brief.md:50,66,79 and FR-014. This supersedes research D1 and FR-005 mechanism wording only; the exact nine business operations, health exclusion, Development-only documentation, existing DTO/status metadata, and Q4 file envelope remain settled. Sentry F2 is moot because the rejected ApiDescription.RelativePath predicate will not be implemented. No ticket delivery change, constitution departure, owner checkbox, production edit, or test waiver follows.

### Every finding disposition

| Axis / ID | Reported severity | Disposition | Evidence and rationale |
|---|---|---|---|
| Compass F1 | Medium | Accept, merged with Sentry F3; blocking plan gap | plan.md:81 and research.md:41 name only CRLF-to-LF normalization; T007 does not specify its document generation entry point. brief.md:38 permits naming non-contract machine-varying values but does not require removing servers. The risk report establishes different HTTP/provider server behavior; specify one stable shared generation path rather than assert that every TestServer request necessarily has a random port. |
| Compass F2 | Medium | Accept; blocking | brief.md:22 and CONCLUSIONS Q1 require the package-installation reality with recon:118 citation in spec.md. FR-001..015 omit that statement. |
| Compass F3 | Low | Accept; noted for owner review, non-blocking | spec.md:57 US3 AC3 proves nothing when no play call occurs. FR-014 already owns LocalPlay false and no execution. Remove this redundant acceptance scenario without dropping the normative safety discipline. No follow-up ticket. |
| Ledger | None | Read; zero findings | Full findings-DEV-312-Ledger:1-20 read. Its general compatibility/API checks do not override the more specific Sentry evidence or the Patron ruling. |
| Sentry F1 | High | Accept; blocking; Patron resolved approach | plan.md:77, spec.md:90, research.md:9 and tasks.md:25 still prescribe ShouldInclude. recon-DEV-312:95 independently records the no-MVC assembly-reference test. Apply the Patron transformer ruling without changing that test. |
| Sentry F2 | Medium | Moot | Superseded by the Patron ruling. Do not repair or retain the RelativePath predicate; remove its stale mechanism references as part of F1. |
| Sentry F3 | Medium | Accept, merged with Compass F1; blocking plan gap | plan.md:81 and T007/T009/T015 do not select HTTP versus provider generation or stabilize request-dependent server values. Evidence: findings-DEV-312-Sentry:13-16. Normal drift and deliberate export must share the same explicitly stable input and bytes. |
| Sentry F4 | Medium | Accept; blocking plan gap | plan.md:81 and T007/T009/T010/T015 specify repository paths with no anchor or resolution rule. A process-relative path is insufficient. Anchor both authoritative and derived files to the named checkout and fail closed on missing or ambiguous markers. |
| Sentry F5 | Medium | Accept compatibility-evidence gap; do not accept a proven runtime break | research.md:25 supports Verify.XunitV3 30.3.0 as the integration line but does not substantiate compatibility with the reported repository xunit.v3.extensibility.core 4.0.1. A NuGet dependency version of 2.0.2 alone does not establish an upper bound or prove TypeLoadException/MissingMethodException. Require cited compatibility/range evidence before freezing the exact pin; no framework downgrade, speculative test failure claim, or gate execution in this phase. |
| Sentry F6 | Low | Accept inaccurate evidence wording; patch/TFM observation noted, non-blocking | research.md:25 claims .NET 10 support for Scalar 2.0.15 without version-specific support evidence. A net9 asset on net10 is not itself proof of incompatibility; OpenAPI 10.0.0 below a reported 10.0.12 family is not alone a defect. Correct source provenance and version-specific compatibility claims; no mandatory upgrade inferred from asset absence. No follow-up ticket. |

### One bounded numbered Quill fix list

Quill owns edits only to spec.md, research.md, plan.md, tasks.md, and quickstart.md if command/path clarification requires it. Keel records decisions in brief.md/CONCLUSIONS.md. No implementation, dependency installation, tests, gates, commits, or new files in this fix round. Preserve the existing snapshot path, no-autoaccept/normal-test-no-write rules, exact operation set, conditional file guards and unrelated changes.

1. **Sentry F1 / moot F2:** Replace ShouldInclude and ApiDescription selection throughout spec FR-005, research D1 (including its rejected-transformer alternative), plan summary/structure/composition, and T003 with the Patron-approved /api-path document transformer in Program.cs. Cite the documented transformer API through Microsoft Learn; state exact path-boundary matching for /api business paths and removal of non-business paths, with no MVC reference and unchanged ApiHostCompositionTests.cs. Preserve existing business metadata and health mappings.
2. **Compass F1 + Sentry F3:** Specify and cite one shared generation mechanism for T007, drift, initial export and regeneration. Prefer an HTTP GET of /openapi/v1.json from the explicit Development host with a fixed localhost client base URI used identically by all callers; substantiate stable servers behavior for that path. If the documented mechanism instead needs normalization of a non-contract server URL, name the exact field/value and transformation under Q3/FR-010 and update all normalization/equality descriptions consistently. Do not silently remove servers or any contract fields. Do not leave HTTP-versus-provider selection to the builder.
3. **Compass F2:** Add the required concise spec statement: Microsoft.AspNetCore.OpenApi is an installable first-party NuGet package, not an in-box reference, citing recon-DEV-312:118. Preserve the ticket wording and delivery.
4. **Sentry F4:** Document the exact repository-root anchor/resolution rule inside the approved OpenApiContractTests.cs file. Resolve the authoritative JSON and exact derived snapshot to that same checkout independently of process working directory; bound any ancestor walk, validate the checkout markers and target containment, and fail instead of reading/writing an unrelated root. Apply it to T007/T009/T010/T015 and quickstart explanations where needed. No helper project, script or outside-file edit.
5. **Sentry F5 + F6 evidence:** Correct research D3 and matching plan/T001/T004 compatibility claims with version-specific primary documentation/package-range evidence for the reported xUnit v3 4.0.1 family and selected Verify pin, and for Scalar on net10. Use the required documentation lanes; honestly record failed Context7 lookup and any primary-source fallback instead of claiming that lane was used. Retain exact pins only when supported; if changing a pin within the already-approved package families is necessary, state the exact replacement and cited compatibility basis consistently. Do not downgrade existing xUnit/framework dependencies or infer incompatibility from a lower dependency minimum/net9 asset alone. OpenAPI patch alignment is noted, not a mandatory upgrade without evidence.
6. **Compass F3 (Low, non-blocking):** Remove US3 AC3 as redundant and unfalsifiable, retaining FR-014 unchanged. This is acceptance-expression cleanup, not a safety scope reduction.

### Freeze ruling

**NOT FROZEN — bounded Quill artifact fixes required.** Critical/High/Medium in-scope plan gaps block under brief.md:91. This is plan-challenge adjudication, separate from the two completed earlier analysis fix rounds recorded at DEV-312:99-103; no cap extension is inferred. Return this single fix list to Conductor for Quill routing. Stop before read-only analyze/freeze; independently reread the corrected artifacts on the next authorized ask. Gate 1 remains closed pending the later spec PR and user merge. No gates, tests, implementation edits, commits, PRs, tracker writes or merges performed.

## Independent post-fix analysis and freeze assessment — 2026-10-08

Identity verified: F:/Dev/LamuFlix.worktrees/feature-312-spec, feature/312-spec, HEAD 01caa4d1b3e4e4ba7674abfd91bb6af2f3be21bc. Read DEV-312, original recon-DEV-312, all three full Compass/Ledger/Sentry reports, brief, conclusions, spec, research, plan, tasks, quickstart and ADR-0007. chain-4: **not found in these roots**; no substitute note or outside temporary ruling artifact was read. Maestri debug confirmed healthy transport. The current authorized Conductor ask supplies this assessment assignment; missing chain access is not treated as proof of any chain transition.

Read-only speckit-analyze prerequisite command ran once with -Json -RequireTasks -IncludeTasks and resolved specs/DEV-312 with research.md, quickstart.md and tasks.md. No extensions.yml was present. Constitution IX, stack constraints, API/Contract and Documentation Rules were read within the named worktree. Analysis itself changed no artifact; this separately authorized append records its results. No new repository reconnaissance, dependency lookup, gate or test was performed. Technical evidence remains the provided recon and challenger reports; this assessment does not independently certify package/runtime behavior.

### Six-fix independent verification

| Fix | Outcome | Artifact evidence |
|---|---|---|
| 1 — transformer / no MVC / moot RelativePath | Complete at artifact level | spec.md:89; research.md:7-13; plan.md:9,77; tasks.md:25. ShouldInclude survives only as a rejected alternative. Transformer documentation is cited; exact path boundary, metadata preservation, unchanged health mappings and composition tests remain mandated. Runtime verification belongs to Phase B. |
| 2 — one stable shared generation/server path | Partial; evidence gap remains | plan.md:81 and tasks.md:40,53,68 select HTTP GET with fixed localhost consistently; research.md:43 asserts stable servers but supplies no citation for request-derived server behavior and does not state the complete fixed base URI. The mechanism no longer mixes HTTP/provider paths, and servers remain compared whole. Complete the prior evidence mandate, without inventing a runtime failure. |
| 3 — NuGet installation reality | Complete | spec.md:99 explicitly records first-party NuGet, not in-box, citing recon-DEV-312:118; ticket wording unchanged. |
| 4 — checkout anchor / cwd independence | Incomplete; initial-export dependency contradiction | plan.md:81 requires both LamuFlix.sln and web/src/api/openapi.json as existing checkout markers. tasks.md:40,53 make this anchor a prerequisite of T009, which first creates openapi.json. recon-DEV-312:44-49 establishes the file and directory are absent initially. Bounded walk, containment and missing/ambiguous failure rules are present but cannot bootstrap the deliverable as written. |
| 5 — version compatibility / honest provenance | Incomplete; blocking evidence gap retained | research.md:25 and plan.md:15 now correctly avoid claiming a proven break and honestly record failed keyed Context7 lookup. However unversioned latest-stable Gallery links, a reported dependency floor and deferral to implementation restore do not provide the required version-specific dependency-range/TFM support evidence for Verify 30.3.0 against xUnit 4.0.1 and Scalar 2.0.15 on net10. No speculative upgrade or downgrade is ordered. |
| 6 — tautological US3 AC3 | Complete | spec.md:53-56 contains only the two observable Production 404 scenarios; FR-014 at :98 retains unchanged/green composition tests, LocalPlay false and no play invocation. |

### Specification Analysis Report

| ID | Category | Severity | Locations | Summary | Recommendation |
|---|---|---|---|---|---|
| I1 | Inconsistency / task dependency | Medium | plan.md:81; tasks.md:40,53,68; quickstart.md:9-13; recon-DEV-312:44-49 | Initial baseline creation requires the baseline to exist as a root marker. | Separate stable checkout identity from required input-baseline existence; permit explicitly opted-in initial creation in the verified checkout and fail normal drift on missing baselines. |
| E1 | Underspecification / evidence | Medium | research.md:25; plan.md:15; tasks.md:23,26; CONCLUSIONS.md:121,132 | Required compatibility evidence was postponed to implementation instead of completed before freeze. | Cite exact version package ranges/assets and support rationale; distinguish package-resolution evidence from later runtime/gate proof. |
| E2 | Underspecification / evidence | Medium | research.md:43; plan.md:81; tasks.md:40; CONCLUSIONS.md:129 | Stable server behavior is asserted without its required source or complete fixed URI. | State one complete fixed URI and cite the generation/server behavior that makes it stable, consistently for every caller. |

### Coverage summary

| Requirement | Task IDs | Notes |
|---|---|---|
| FR-001 | T002,T003,T005 | Services/document route |
| FR-002 | T003,T005 | Scalar wiring |
| FR-003 | T003,T006,T008 | Development/Production proof |
| FR-004 | T003,T005 | Exact nine operations; health exclusion |
| FR-005 | T003,T005 | Transformer/no MVC |
| FR-006 | T003,T005 | OpenAPI 3.1 |
| FR-007 | T007,T009,T015 | Covered; I1 blocks initial creation |
| FR-008 | T010,T017,T018 | Full equality and negative proof |
| FR-009 | T004,T010 | Equality-enforced Verify snapshot |
| FR-010 | T007,T010,T011 | Whole comparison; E2 evidence open |
| FR-011 | T007,T015,T016 | Opt-in and identical outputs |
| FR-012 | T001,T002,T003 | Runtime-only package/file envelope |
| FR-013 | T001-T018 | Explicit bounded file envelope / exclusions |
| FR-014 | T003,T005,T006,T008,T018 | Existing tests and LocalPlay discipline |
| FR-015 | T001,T002,T004 | Covered; E1 evidence open |
| SC-001 | T003,T005 | Scalar/document/metadata |
| SC-002 | T009,T010,T017 | Positive/negative drift |
| SC-003 | T006,T008 | Production absence |
| SC-004 | T007,T011 | Determinism; E2 evidence open |
| SC-005 | T014,T018 | Accepted ADR/full suite |

Metrics: 20 requirements (15 FR + 5 SC); 18 tasks; task coverage 100 percent, which does not mean all tasks are executable or evidence is complete. Three Medium findings; zero Critical/High, duplication, unmapped requirements or unmapped tasks. Two underspecified evidence obligations and one dependency inconsistency. No new constitution scope conflict identified; the required documentation-lane/version evidence remains incomplete under E1/E2. No performance/security adjective without a testable outcome was found.

Brief-to-plan/tasks check: five deliverables, exact operation membership, Development-only exposure, no-MVC ruling, health deferral, no consumer/TS/CI/script additions, approved file envelope and conditional guards, complete three-way equality, CRLF-to-LF only, no autoaccept/normal writes, command/filter/env identity, ADR Accepted and Phase B gate semantics remain aligned. Order is nominally correct, except the bootstrap dependency I1. ADR-0007:15-27 remains consistent with settled authority and scope. No decision change or new owner checkbox is inferred; brief.md is left unchanged.

### One bounded numbered follow-up fix list

Quill edits only research.md, plan.md, tasks.md and quickstart.md where necessary for consistency. No production files, new helpers/scripts, dependency installation, tests, gates, commits or tracker writes. Preserve every completed fix and the existing scope and unrelated modifications.

1. **I1 / prior fix 4:** Replace the newly generated baseline as a checkout-identity marker with stable existing checkout markers supported by recon. Retain the assembly-directory start, bounded ancestor search, unique root, same-root output resolution and containment checks. Distinguish normal drift (both baselines must already exist; missing means failure) from explicit initial export/regeneration (may create the approved output directory/files inside the verified root). Carry that distinction through T007/T009/T010/T015 and quickstart. If choosing existing markers needs facts not supplied by recon, return needs recon through Conductor; do not search or invent them.
2. **E1 / prior fix 5:** Supply version-specific primary package range/asset evidence for Verify.XunitV3 30.3.0 with repository xunit.v3.extensibility.core 4.0.1 and Scalar.AspNetCore 2.0.15 on net10. Use the mandated documentation lanes, recording exact failures and fallback provenance honestly. Replace the unsupported latest-stable labels with exact-version citations and a bounded support conclusion. A minimum alone neither proves incompatibility nor closes the compatibility obligation. Runtime restore/tests remain later verification, not substitutes for plan evidence. Keep pins only when supported; any necessary replacement within approved families must be exact and consistently cited. No speculative mandatory patch update or framework downgrade.
3. **E2 / prior fix 2:** Name the full shared fixed localhost base URI (including scheme and port behavior), and cite primary framework generation/server behavior supporting the stable HTTP request path; the provided Sentry F3 identifies the underlying behavior but the corrected research currently cites none of it. Apply the same URI and path to drift, initial export and regeneration. Keep servers compared whole and the single line-ending normalization; no new scrub rule or mechanism is authorized.

### Freeze ruling

**NOT FROZEN.** Three of six fixes are not claimed complete: fixes 2 and 4 are partial/incomplete, fix 5 remains incomplete; fixes 1, 3 and 6 are complete at artifact level. Three Medium gaps block under brief.md:91. This is the independent reread after the authorized single plan-challenge fix list, not a new challenger round or a cap extension; Conductor owns routing within the standing caps. Stop before freeze and return the one bounded list. Gate 1 remains closed pending later spec PR and user merge. chain-4 remains not found in these roots, independently reported as an access limit. Only this append to CONCLUSIONS.md was authored by this assessment; existing harness.yml and .specify/feature.json changes were preserved. No gates, tests, implementation edits, commits, tracker writes, PRs or merges.

## Latest bounded-correction verification and plan freeze — 2026-10-08

Identity: F:/Dev/LamuFlix.worktrees/feature-312-spec, branch feature/312-spec, HEAD/base 01caa4d1b3e4e4ba7674abfd91bb6af2f3be21bc verified before this append. Read the connected DEV-312 and original recon-DEV-312 notes, all three complete Compass/Ledger/Sentry reports, and brief.md, CONCLUSIONS.md, spec.md, research.md, plan.md, tasks.md and quickstart.md. The prior analysis and its numbered correction list at CONCLUSIONS.md:139-203 supply the comparison baseline; these draft artifacts are untracked, so an ordinary tracked git diff does not expose Quill edits. No earlier file revision or external source was inferred from that empty diff.

Read-only speckit-analyze prerequisites ran once and resolved this feature with tasks present. No .specify/extensions.yml was present. Constitution was read in this worktree. Analysis changed no draft artifact; this separately authorized append records the assessment. No outside-root documentation lookup or new repository reconnaissance was performed. Exact-version/source statements below are assessed as cited planning evidence in Quill research and the supplied reports, not independently fetched package metadata or a runtime certification.

### All three remaining Medium items

| Prior ID | Disposition | Independent artifact evidence and limits |
|---|---|---|
| I1 — bootstrap/root marker | Closed | plan.md:81 replaces generated openapi.json with existing Directory.Packages.props (existence supported by recon-DEV-312:35). Assembly-directory start, at most eight ancestors, unique marker, same-root resolution, containment and missing/ambiguous failure remain explicit. Normal drift requires both baseline files; explicit export/regeneration may create them. tasks.md:40,53,54,68 and quickstart.md:9-16 carry the distinction. T007 can now precede T009 without requiring T009 output. |
| E1 — exact-version compatibility evidence | Closed at plan-evidence level | research.md:25 now cites exact Verify.XunitV3/30.3.0 and Scalar.AspNetCore/2.0.15 Gallery pages and states the open-ended xunit.v3.extensibility.core >= 2.0.2 dependency, reported net10 asset for Verify, and included net8/net9 plus computed net10 compatibility for Scalar. plan.md:15 and T001/T004 retain the same pins. The open-ended range admits the reported repository 4.0.1 family; the asset/compatibility record supplies the previously missing TFM rationale. This is a bounded package-resolution/targeting conclusion, not proof of binary API compatibility or publisher testing. Actual restore/tests stay mandatory Phase B checks. No dependency downgrade or speculative upgrade follows. Failed keyed Context7 resolve and NuGet fallback are stated honestly; anonymous Context7 is not claimed unavailable. |
| E2 — fixed URI and request-server evidence | Closed | research.md:43 and plan.md:81 name the complete http://localhost/ URI, no custom BaseAddress or port override, and one HTTP GET /openapi/v1.json path for drift, initial export and regeneration. They cite TestServer/WebApplicationFactory client defaults and framework request-derived server behavior; this agrees with Sentry F3:13-16. tasks.md:40,53,68 and quickstart.md:25-28 use the same host/URI/path. All server data remains compared whole; only CRLF-to-LF normalization is permitted. The linked .NET 11 trailing-slash change is used for its description of prior request-derived behavior, not to import .NET 11 output formatting into the .NET 10 plan. |

Prior completed fixes remain complete: Patron-approved transformer/no MVC and unchanged composition tests (spec.md:89, research.md:9-13, plan.md:77, T003); NuGet installation reality with recon citation (spec.md:99); removed tautological US3 AC3 while preserving FR-014 (spec.md:53-56,98). No finding is dismissed as a proven runtime success.

### Specification Analysis Report

No remaining actionable Critical, High or Medium findings in the authorized artifact set. No duplication, unresolved placeholder, uncovered requirement, unmapped task, ordering contradiction, new constitution conflict or decision change found. Earlier I1/E1/E2 are closed as above; all nine original challenger findings retain their recorded dispositions, including moot Sentry F2 and noted non-blocking Sentry F6. Ledger full zero-findings report was read and does not substitute for either other axis.

| Requirement | Task coverage |
|---|---|
| FR-001 | T002,T003,T005 |
| FR-002 | T003,T005 |
| FR-003 | T003,T006,T008 |
| FR-004 | T003,T005 |
| FR-005 | T003,T005 |
| FR-006 | T003,T005 |
| FR-007 | T007,T009,T015 |
| FR-008 | T010,T017,T018 |
| FR-009 | T004,T010 |
| FR-010 | T007,T010,T011 |
| FR-011 | T007,T015,T016 |
| FR-012 | T001,T002,T003 |
| FR-013 | T001-T018 bounded file envelope |
| FR-014 | T003,T005,T006,T008,T018 |
| FR-015 | T001,T002,T004 |
| SC-001 | T003,T005 |
| SC-002 | T009,T010,T017 |
| SC-003 | T006,T008 |
| SC-004 | T007,T011 |
| SC-005 | T014,T018 |

Metrics: 20 requirements (15 FR, 5 SC), 18 tasks, 100 percent task coverage; zero remaining Critical/High/Medium, ambiguity, duplication, constitution-alignment issues and unmapped tasks. Coverage is planning traceability, not implementation/test evidence.

Brief-to-plan/tasks consistency: all five deliverables, exact nine generated operation pairs, health exclusion, Development-only documentation and separate Production 404 checks, no-MVC ruling, approved file envelope/conditional guards, whole-document three-way pre-Verify equality, exact snapshot path, fixed normalization, no autoaccept/normal tracked-file writes, identical filter and opt-in variable, runtime-only generation, ADR ownership and Accepted requirement, and Phase B gate semantics remain aligned. Package/composition precedes host/export; export precedes initial baseline; equality precedes negative proof and gates. No new TS/client/MSW/CI/script, schema, layer, owner checkbox or ticket change is introduced. brief.md needs no decision amendment.

### Freeze ruling and next action

**FROZEN — plan-stage closing bar satisfied.** This is verification of the latest authorized bounded corrections, not an additional challenger round or a cap extension. Prior NOT FROZEN assessments remain append-only historical rulings and are superseded by this assessment of the current artifacts. Conductor may route the frozen Phase A package to the spec-PR step. Gate 1 remains CLOSED until the spec PR is user-merged; this ruling authorizes no implementation and certifies no Phase B gate.

Only this append to CONCLUSIONS.md was authored. Existing harness.yml and .specify/feature.json modifications and all Quill drafts were preserved. No gates, tests, implementation edits, dependency installation, commits, tracker writes, PR publication or merge performed. No bounded remediation is needed for these three items.

## Patron Gate 1 provisional ruling — 2026-10-08

**gate1: provisional.** spec.md:7 Status set to `gate1: provisional`; no other spec edit.
- Basis: read-only /speckit-analyze clean and plan challenge adjudicated, FROZEN at CONCLUSIONS.md:254 (20 requirements, 18 tasks, 100 percent, zero Critical/High/Medium); all nine challenger findings disposed.
- Verified on feature/312-spec at HEAD 01caa4d1b3e4e4ba7674abfd91bb6af2f3be21bc; pre-existing harness.yml and .specify/feature.json changes untouched.
- Gate 1 stays CLOSED until the user merges the spec PR; no owner checkbox open; no YouTrack change. Next: Rigger commits the Phase A package (specs/DEV-312/*, docs/adr/ADR-0007.md) as `DEV-312 - ...` and opens the spec PR; never merges.
