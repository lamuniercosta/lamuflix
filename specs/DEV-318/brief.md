# DEV-318 - Completed grill brief

## Status and authority

Phase A, Phase 2 step 1 completed. Same Patron grill resumed at Q4 after the bounded recon supplement; Q1-Q3 preserved. Patron confirmed shared understanding at Q7: the decision set is complete, grill closed at 7/12. No overflow or taste assumptions, no current recon blocker or owner checkbox. This is the decision input for later drafting, not plan-challenge clearance or Gate 1 approval. No Quill or later Phase 2 work started.

Worktree: F:/Dev/LamuFlix.worktrees/feature-318-spec. Branch: feature/318-spec. Recon production base: a62fbe869df4d1fb2a738417b06660bf247e96d9. Verified HEAD before finalizing: 3055753ee3d6e8db4c4bb4f75a69ffcb17eb7b6a. Patron reported committing Q1 as 344e01b and Q2-Q7 as 3055753, CONCLUSIONS.md only and not pushed; the latter occurred despite the no-new-commit instruction. Keel made no commit or push and preserved both commits. Preserve the pre-existing .specify/feature.json modification. This brief remains uncommitted.

Authority: DEV-318 task note:10-11; recon-DEV-318:18-136,146-182; specs/PRODUCT.md; constitution I,VI-VIII,IX and verification rules; task-pipeline:14-17,25-27,66-82,95-107,114-120; lamuflix-team-charter section 2.3. Full exchanges, cited rationale and Patron rulings are append-only in CONCLUSIONS.md. Every grill answer is recorded below; Q1-Q3 are retained from the paused brief.

## Confirmed grill decisions

### Q1 - Worker and host

Approved, no scope change. Worker means the active EnrichmentConsumer hosted in the API. Reuse ApiEndToEnd WebApplicationFactory, real Postgres and RabbitMQ Testcontainers, and WireMock OMDb. Do not resurrect the retired worker project or introduce a host, project or dependency. This proves the actual broker crossing and hosted worker, not separate-process isolation.

Trace evidence must begin at the HTTP request and show that the consumer span is parented on the extracted RabbitMQ publish context. A shared TraceId by itself is insufficient. Basis: DEV-318:10-11; recon:41,56,61,78,108,133,136; constitution VI and IX; PRODUCT section 1. Stale host naming outside the ticket is noted, no ticket.

### Q2 - Status and GET evidence

Approved, no owner checkbox on the provided ticket summary. Assert Pending and Enriched from persisted Postgres state. Assert GET /api/movies/{id} returns 200 and enriched metadata with the existing MovieDetailsResponse contract. Parse the movie id from the 202 Location header. No public Status field, OpenAPI or generated TypeScript change.

Observe Pending deterministically using an existing test seam, with no production hook or timing assumption. Basis: DEV-318:10-11; recon:20-25,79,84; constitution IX. Patron explicitly qualified this ruling: the available task note is a ticket summary; if verbatim ticket text requires a Status field in the GET body, that becomes blocked: structural. Do not silently substitute a different acceptance requirement.

### Q3 - Bounded file scope and synchronization

Approved footprint: tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs, plus optional new test-only synchronization/capture helper files beside it in the same integration project where extraction improves clarity. Existing fixture, factory and base stay unchanged. Existing import/scanner/metadata/provider assertions must not be weakened or removed. Use the installStubs/configureTestServices seam already supplied by StartHostAsync.

Approved approach: a cancellation-aware asynchronous gate wraps the fully decorated production ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>, outside its decorator chain and before TryClaimForEnrichmentAsync. The real consumer reaches that gate; POST completes with 202; read a fresh Postgres context and assert Pending; release the gate; execute the original production handler against real WireMock-backed OMDb; await Enriched and assert GET metadata. The gate starts no Activity, uses a bounded timeout, passes ValidateScopes/ValidateOnBuild, and is released in finally before host shutdown. Do not mock the queue, repository or metadata provider or substitute a fixed response delay.

No production code, package/project, schema/migration, public API, LocalPlay or secret changes are authorized. The existing test alteration is forced by the ticket acceptance scope. Basis: DEV-318:10-11; recon:48,77-80,84-85,116,132; constitution IX; PRODUCT section 5 item 6; Patron Q3.

## Q3 evidence resolution and technical registration plan

The former recon blocker is closed by recon-DEV-318:146-182. The interface descriptor is Scoped with an ImplementationFactory (:166); Compose resolves the scoped concrete handler and returns Tracing -> Logging -> Validation -> handler (:171). The consumer resolves it inside one AsyncServiceScope per delivery (:173). The existing configureTestServices callback runs after app registration under ValidateOnBuild/ValidateScopes (:177-178).

Capture the original ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome> descriptor and its factory during test registration. Assert its expected scoped factory form. Replace only that interface descriptor with a scoped outer gate whose factory invokes the captured production factory with the same scoped IServiceProvider. Never recursively resolve the replaced interface, construct the bare handler, copy the decorator composition, create a second provider, or retain scoped services in per-test gate state. The gate adds no Activity. Patron accepted this resolution without reopening Q3 (CONCLUSIONS.md Q3 follow-up).

## Q4 - Complete request-correlated trace proof

Approved as the sufficient positive-path proof (Patron Q4). Install disposable ActivityListener capture before host startup/import, sample AllData and record stopped spans safely across threads. Send a unique W3C traceparent and tracestate on the HTTP request with no test-process ambient Activity. Correlate captured spans by that request TraceId only, never global span counts; delivery MovieId is matched separately to the Location id.

Require the actual API server span, import handler ancestry, Enrichment.Enqueue, RabbitMQ.Client.Publisher publish span, Enrichment.Process consumer, processing handler and Metadata.Lookup to share the request trace. Prove the import/enqueue ancestry reaches the API server span, publish.ParentSpanId equals enqueue.SpanId, consumer.ParentSpanId equals publish.SpanId, and Metadata.Lookup descends from the processing span under the consumer. Assert propagated tracestate, consumer ActivityKind.Consumer and no redelivery link on this first delivery. A common TraceId alone does not pass. Do not steal the production delivery with BasicGet to inspect it.

Use TelemetryConstants for production telemetry names and existing production source-name constants. Wait boundedly for stopped spans after Enriched so persistence completion cannot race capture. Fail with the missing span named; dispose listener in finally. Retry, broker redelivery, DLQ and OTLP exporter scenarios are outside this positive-path proof. Basis: DEV-318:11; recon:114-136; constitution VI; Q1 and Q4.

## Q5 - Loop discipline

Closing bar: zero unresolved Critical, High or Medium in-scope findings, plus every applicable gate meeting the pipeline. Low findings receive an explicit recorded disposition and may remain nonblocking. All three axis reports from Sentry, Ledger and Compass are mandatory for this L ticket; missing one is blocked: missing axis <name>. Review completion requires the PR receipt under the charter, not a chat-only result.

Frozen scope: additive deterministic Pending-to-Enriched HTTP import integration proof using the existing real Postgres/RabbitMQ/WireMock stack; unchanged details GET and metadata/provider evidence; scoped decorated-handler test gate; HTTP-request-correlated trace ancestry through broker publish, active consumer and Metadata.Lookup; delivery file set limited below; anything else is a follow-up issue, not a finding in this round.

Round cap: exactly two review rounds, at most two fix commits per round. No third autonomous review/remediation round. Above-bar findings remaining after the cap stay blocked for user review on the PR; they are never waived. Out-of-scope issue recording follows the charter: Critical/High, broken behaviour or user-requested findings may warrant a follow-up ticket; other observations are noted, no ticket. Patron decides and Rigger records tracker changes. Basis: task-pipeline:26-27,114-120; charter:29-31; Patron Q5.

## Q6 - Test strategy and gate expectations

Property-test decision: propertyTests: opt-out - no domain invariants change. The Conductor must record that exact line in the DEV-318 task note before a no-tag property-test exit 2 is accepted. Existing property tests still run; this is not a blanket skip. No new property-test or mutation target is introduced for integration-only proof. The held-then-released real scenario exercises the synchronization; do not add synthetic unit tests that merely duplicate helper implementation.

Gauge owns later targeted execution of the enhanced import scenario, full dotnet test including architecture, and the following pipeline evidence:

| Verification | Required expectation |
|---|---|
| scripts/run-roslyn-analyzers.ps1 | PASS on changed C#; no warning waiver |
| scripts/run-cyclomatic-complexity.ps1 | Configured harness threshold; Cog refactor separately meets <=6 |
| scripts/run-jetbrains-inspectcode.ps1 | Changed diff, no arguments, never -All |
| scripts/run-property-tests.ps1 | PASS or accepted no-tag exit 2 with recorded task-note opt-out |
| scripts/run-vulnerable-packages.ps1 | PASS |
| dotnet format --verify-no-changes | PASS |
| dotnet test | PASS, including integration and architecture coverage |
| scripts/run-mutation.ps1 | Expected SKIPPED (scope-empty) for no src production C#; report the actual script verdict |
| scripts/run-web-gates.ps1 | Expected SKIPPED (scope-empty) for no web change; report actual verdict |

Expectations are not receipts or waivers. Scope-empty exit-2 SKIPPED is nonblocking and never PASS. Configured-disabled SKIP, mutation NOT APPLICABLE and accepted property-test opt-out remain distinct from PASS. Exit 1, Could not run, or an unaccepted property-test exit 2 blocks. Never change thresholds or generated harness settings to pass; never use Stryker --since from a worktree; no unrelated harness or production repair enters the frozen scope. Applicable hosted checks, including gitleaks, remain later merge-bar requirements. Basis: task-pipeline:95,97-107; charter:66-76; Patron Q6. No test or gate was run during this grill.

## Q7 - File map, ordering and closure

Patron confirmed this complete plan without drift from Q1-Q6 and authorized brief finalization. Extend ImportMovieFolder_RealScannerThroughOmdb_PersistsEnrichedMovieWithMeasuredProviderEvidence additively. Keep every existing import, scanner, details, metadata and measured WireMock assertion, plus the existing owned-folder cleanup tests.

Delivery file map, relative to the named worktree:

| File | Decision |
|---|---|
| tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs | Extend the existing scenario; default private nested synchronization/capture helpers |
| tests/LamuFlix.IntegrationTests/ApiImportEnrichmentGate.cs | Optional extraction only if clarity or gates require it; no new layer |
| tests/LamuFlix.IntegrationTests/ApiImportTraceCapture.cs | Optional extraction under the same condition |

ApiEndToEndFixture, ApiEndToEndFactory and ApiEndToEndTestBase remain unchanged. Production files, packages, projects, schema/migrations, public contracts, OpenAPI/generated TypeScript, LocalPlay and secrets are outside delivery scope. Use the existing configured sentinel and container-generated settings; never copy real credentials. No new domain term or glossary edit is needed. Documentation decisions live in specs/DEV-318/; the L-path ADR is drafted at its later step, not now.

Task ordering for later Quill drafts:

1. Any pickup drift/recon belongs to Wisp; baseline gate measurements belong to Gauge. Resolve missing evidence before production of code tasks.
2. Add test-local gate and capture support in the bounded file set, preserving the scoped original factory and production decorator chain. Keep gate state per test with asynchronous continuations, cancellation and no scoped-service capture.
3. Extend the existing import scenario. Use the existing owned real-scanner folder, production API-host consumer, real Postgres/RabbitMQ containers and WireMock-backed production provider. Install capture before host/import; send unique trace headers without ambient Activity.
4. Await POST 202 and real consumer arrival at the gate using bounded waits. Parse Location, match that id to delivery MovieId, and query a fresh Postgres context to assert Pending before releasing enrichment.
5. Release the gate and await persisted Enriched using the existing status seam. Assert GET 200, unchanged response geometry and all metadata fields, plus exactly one measured OMDb request with the existing method/path/query evidence.
6. Await stopped trace spans and assert the full Q4 ancestry/propagation proof. Use the existing 30-second integration wait budget with cancellation; diagnose a stuck gate or missing span explicitly. These waits observe test completion, not production retry policy. Keep time through TimeProvider and existing test-clock seams.
7. Always release the gate in finally before host disposal, dispose capture, and retain existing owned-folder cleanup. No leaked listener or blocking gate may survive the test.
8. Exercise the enhanced scenario against the real stack, then run the complete pipeline gate set through its designated owners. Do not claim checks passed before receipts exist. Keep all code tasks within this one proof; do not introduce an independently broader implementation phase.

This ask ends here. Size L remains authoritative: later Quill Spec Kit drafting includes clarify/checklist, then analyze, L ADR and plan challenge, and user-owned merged spec Gate 1 before build. The later ADR records this proof approach without adding a port, project or architectural layer. No Quill dispatch, Spec Kit artifacts, ADR draft, tests, gates, source edit, commit or push by Keel occurred in this ask.

## Provenance caveat and handoff

Carry Q2 provenance to the later spec PR: the supplied DEV-318 note contains a ticket summary. On that evidence, persisted status plus unchanged GET metadata meets the ticket and needs no owner checkbox. If verbatim ticket text explicitly requires a Status field in the GET response, the ruling becomes blocked: structural; do not silently broaden the API or drop that requirement. This caveat is not an outstanding recon request on the supplied decision set.

All seven answers are approved and persisted in CONCLUSIONS.md; no [assumed] decisions and no ASSUMPTIONS.md creation is required. The resolved exchanges are persisted and safe to summarize. Next authorized owner/action is for the Conductor to schedule later Phase 2 work from this brief and record the property-test opt-out; Keel has stopped at the requested boundary.

## Bounded decision addendum - ADR-0020 lifecycle (2026-10-09)

Authority: authorized Conductor ask; accepted Compass F1 ruling in DEV-318:148-157, especially :150-156; findings-DEV-318-Compass:7; task-pipeline:14-16,43,76-82,140; task-chain:39. This records the accepted documentation lifecycle only. It is not full plan-challenge adjudication, plan freeze, Gate 1 clearance or build authorization; other findings remain for separately authorized adjudication. Earlier status and handoff wording describes the completed grill ask, not current lifecycle clearance.

Two commit vehicles: keep the Gate 1 spec PR strictly specs/**. Preserve the complete corrected ADR text at specs/DEV-318/adr-0020-request-correlated-import-enrichment-proof.md in that PR. Publish identical approved text at canonical docs/adr/0020-request-correlated-import-enrichment-proof.md in the DEV-318 Phase B delivery PR. The specs copy is the durable review/preservation artifact, not a replacement for the canonical ADR. No docs/adr commit or cherry-pick enters the spec PR; no separate ADR PR or standing cleanup-process extension is authorized.

Lifecycle wording: ADR status is "Proposed - reviewed in Phase A; decision approval depends on Gate 1 spec merge; canonical docs/adr publication occurs in the DEV-318 delivery PR." Gate 1 approves the recorded approach through the preserved specs copy; canonical ADR publication occurs with delivery. Retain the disclaimer that the ADR itself grants neither plan freeze nor Gate 1/build authorization. The final corrected Phase A ADR draft and preserved specs copy must contain identical full text, and Phase B must publish that approved text unchanged.

Drafting boundary: CHK011 governs Quill Spec Kit drafting under specs/DEV-318/ only, including the durable ADR copy within that drafting/commit boundary. It is not a global prohibition on Phase A artifacts: distinguish the later Keel L-path ADR draft mandated at docs/adr/ by task-pipeline:80. Quill must name both specs/DEV-318/adr-0020-request-correlated-import-enrichment-proof.md (Gate 1 review/preservation copy) and docs/adr/0020-request-correlated-import-enrichment-proof.md (Phase B canonical publication) in plan.md documentation structure. These are documentation lifecycle instructions; the bounded C# delivery file map remains unchanged.

Preservation before cleanup: Rigger must commit the complete corrected specs copy, include it in the specs-only Gate 1 PR, and verify/read back its merged full contents before any spec worktree removal. Persist and read back the preservation receipt in DEV-318. The original docs/adr draft must never be the sole surviving copy when the spec checkout is removed. Reconstruction from a summary or reliance only on a deleted worktree is insufficient. A failed preservation receipt is a process blocker, not permission to widen the spec PR. The standing spec cleanup still removes only its worktree/branch, retains task notes and queues Phase B.

Phase B ordering: in the named delivery worktree, Rigger must restore the exact approved text from the verified merged specs copy to canonical docs/adr/0020-request-correlated-import-enrichment-proof.md, verify full-text identity, commit the canonical file for the delivery PR, and persist/read back the restore/commit/content receipt in DEV-318 before delivery handoff. Do not claim canonical publication complete from the Phase A draft or Gate 1 merge alone.

Frozen decisions preserved: Q1-Q7, the positive-path proof, bounded C# file set, unchanged fixture/factory/base, Q2 provenance caveat, propertyTests opt-out, all gate expectations and caveats, closing bar, two-round cap and two-fix-commit-per-round cap remain intact. The canonical ADR is already required L-path documentation, not an added product deliverable, production layer or C# scope extension. No dependency, public API, schema, LocalPlay, secret or constitution change is decided; no owner checkbox is required for this lifecycle ruling. This addendum authorizes no edit outside brief.md in the present ask and supplies the decision basis for later bounded owner dispatches.

## Bounded decision addendum - accepted Sentry clarifications (2026-10-09)

Authority: authorized Conductor ask under DEV-318:201; accepted Sentry F1/F2/F4/F5 rulings at DEV-318:166-184. These clarify the existing positive-path proof only; no plan freeze, Gate 1 clearance or build authorization is granted.

1. Cleanup ownership (Sentry F1): the scenario-level try/finally owns cleanup on both success and failure exits. Its finally releases the gate before host disposal and guarantees capture/listener disposal on either exit. Explicitly verify release-before-host-disposal and capture/listener disposal on both exits. Retain existing owned-folder cleanup; introduce no new disposal abstraction or separate business failure-path scenario.
2. Ambient Activity precondition (Sentry F2): after request preparation, immediately before sending the POST, assert Activity.Current is null. Retain the chosen traceparent/tracestate and existing trace-chain proof without propagation suppression or an additional Activity.
3. Original descriptor guard (Sentry F4): require exactly one descriptor matching ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>; assert that descriptor has scoped lifetime and a non-null ImplementationFactory. Capture that exact factory and replace only that descriptor, retaining the same scoped IServiceProvider and original production decorator chain. No production registration change, second service provider or wrapper-chain change is authorized.
4. Measurement boundary (Sentry F5): measurement depends on the existing fixture warm-up followed by WireMock Reset. Assert exactly one measured OMDb GET filtered by the scenario sentinel apikey and selected t/type values; preserve the existing method/path/query, parameter and secret-scrub assertions. Retry-path behavior is outside this proof, but a second matching request still fails the positive-path count assertion. Do not add another Reset, change the fixture, suppress duplicate matching requests, weaken the count or alter resilience configuration.

Preserved decisions: Q1-Q7, product/code scope, the existing clarity-or-gates helper-extraction condition and named file map, fixture/factory/base boundaries, provenance caveats, gate expectations, closing bar, two-round cap and two-fix-commit-per-round cap remain unchanged. This ask authorizes only this brief.md append; artifact remediation belongs to later bounded owner dispatches.
