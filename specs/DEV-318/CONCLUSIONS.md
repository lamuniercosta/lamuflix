# DEV-318 - Patron conclusions

Grill (Phase 2 step 1) with Keel. Budget 12 questions.

## Q1/12 - Meaning of "Worker" in the ticket

**Question (Keel):** Recon-DEV-318:41-56,61,78,108 establish that `EnrichmentConsumer` in the API host is the
active Worker and the old `LamuFlix.Worker` is retired. Keel recommends interpreting the ticket's Worker as that
active hosted consumer, reusing the ApiEndToEnd `WebApplicationFactory` plus Postgres/RabbitMQ Testcontainers and
WireMock, and strengthening existing coverage rather than creating or resurrecting a separate worker host. Cost: the
test proves the deployed in-process worker boundary, not process isolation. Does this fulfil the ticket with no
scope change?

**Ruling:** Approved - the ticket's Worker is the active `EnrichmentConsumer` hosted in the API host; reuse the
ApiEndToEnd stack; no new host, project, or dependency. No scope change, not `blocked: structural`.

- Ticket text (DEV-318:10-11) names `WebApplicationFactory` driving import, a Testcontainers RabbitMQ consumed by the
  worker, WireMock OMDb, and GET status; a `WebApplicationFactory` host is in-process by construction, so the ticket
  itself scopes the worker to the hosted consumer. Recon-DEV-318:41,61,78,108 show that consumer and the OMDb
  client are registered in that host and asserted present by `ApiEndToEndFactory.cs:51`; recon:56 records the
  `LamuFlix.Worker` retirement (ADR-0019), a prior decision this ticket neither makes nor reverses.
- Constitution VI (:218-224) requires one trace API -> RabbitMQ -> Worker -> metadata provider via
  `traceparent`/`tracestate` header inject/extract; that crossing is real over the broker wire even in-process.
  Constitution :288 and :304 place the worker end-to-end and single-trace-id scenario in `LamuFlix.IntegrationTests`
  with exactly these tools (Testcontainers, `WebApplicationFactory`, WireMock.Net). PRODUCT.md:10 names the Worker
  by role (background enrichment over RabbitMQ), which the hosted consumer fills.
- Condition (because in-process removes process isolation): the trace assertion MUST start from the HTTP request
  and prove continuity through the wire headers - consumer span parented on the extracted RabbitMQ publish context -
  not a shared TraceId alone (recon-DEV-318:133 citing DEV-314 brief:61; recon:136 confirms no such test exists).
  Stale constitution :108 / PRODUCT.md:10 references to `LamuFlix.Worker` are not this ticket's to edit:
  noted, no ticket.

## Q2/12 - How Pending and Enriched are observed; API-shape boundary

**Question (Keel):** recon-DEV-318:20-25 establishes POST returns 202 with Location, while GET details exposes
Id/Title/Path/Format/Metadata and no Status field; recon:79,84 establishes persisted status polling exists. Keel
recommends asserting Pending and Enriched directly from real Postgres, then asserting GET 200 and enriched metadata
using the unchanged details contract. Pending must be observed under deterministic test-only synchronization before
enrichment completes, not a timing race. This keeps both state assertions and the GET acceptance evidence without
adding a public DTO field, OpenAPI changes or production behaviour. Does this observation method fulfil ticket
DEV-318:11 without changing what it delivers, or is literal GET Status a structural owner checkbox?

**Ruling:** Approved - assert Pending and Enriched from persisted Postgres state, plus GET /api/movies/{id} 200 with the
enriched metadata on the unchanged `MovieDetailsResponse`; no DTO field, OpenAPI or `types.ts` change; not an owner
checkbox.

- API-shape boundary: DEV-318:10-11 is a test ticket. It names the assertion sequence (POST, Status == Pending, worker
  consumption, WireMock OMDb, GET, Status == Enriched, single trace) and names no response field or contract change.
  The current details contract has no Status (recon-DEV-318:24-25), so adding one would be a public API shape not in
  spec.md (role rule section 2.3 item 4). It would also be production scope beyond a test deliverable, and it would
  force a pass along the DTO -> OpenAPI -> generated TypeScript contract chain. Rejected; the details contract stays
  frozen for DEV-318.
- Both named state assertions and the GET are kept, so what the ticket delivers is unchanged. Constitution :299-301
  (real infrastructure, no mocks) and :304 (single-trace-id end-to-end integration test) are met against real
  Postgres via the existing persisted-status seam (recon-DEV-318:79 `WaitForStatusAsync`, :84). The test must also
  assert the movie id parsed from the 202 Location header (recon:84 `ParseImportedId`).
- Pending must be observed deterministically, for example by holding the WireMock OMDb response or by another test-only
  seam already reachable through `StartHostAsync`'s `installStubs`/`configureTestServices` (recon:79). No
  production code, flag or hook may be added only to make Pending observable. Unverified: this rests on the DEV-318:11
  summary of the ticket text. If the verbatim ticket explicitly requires a Status field in the GET response body, the
  ruling flips to `blocked: structural`.

## Q3/12 - Delivery footprint and the Pending gate mechanism

**Question (Keel):** Keel recommends freezing delivery to strengthening `tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs`.
New test-only synchronization/capture helpers would sit beside it in the same integration project, and only if
extraction is needed for clarity. The existing fixture, factory and base are reused unchanged through `StartHostAsync`
`installStubs`/`configureTestServices` (recon:77-80,132). No production code, and no dependency, project, schema, API,
LocalPlay or secret changes. Plan: a cancellation-aware async gate around the already registered production
`ProcessEnrichmentCommand` handler, installed via `configureTestServices`. The real consumer reaches the gate, POST
completes with 202, a fresh Postgres read asserts Pending, then the gate releases and delegates to the original
production handler and the real WireMock-backed provider. The gate is always released in `finally` before host
shutdown. There is no mocked queue, provider or repository and no fixed response delay; the cost is a small amount
of test-only synchronization code. Does Patron deliberately approve the tracked test-file alteration under role rule
section 2.3 item 6 and the bounded scope?

**Ruling:** Approved - the delivery footprint is frozen to `ApiEndToEndImportTests.cs` plus optional test-only helpers
beside it in `LamuFlix.IntegrationTests`. Fixture, factory and base stay unchanged. Nothing else is in scope.

- Section 2.3 item 6 does not bite: the change is additive to the test file that already holds the ticket-shaped
  scenario (recon-DEV-318:84), and the ticket's acceptance criterion forces it (DEV-318:10-11: an end-to-end test
  over exactly this path). Constitution :288 and :304 place that test in `LamuFlix.IntegrationTests`. Condition: the
  existing `ImportMovieFolder_RealScannerThroughOmdb_...` assertions (recon:84-85) MUST NOT be weakened or removed. Add
  the new scenario, or extend the existing one additively.
- The gate mechanism is approved. It wraps the real handler, so infrastructure stays real (constitution :299-301). It
  runs before the handler's claim (recon:48 `TryClaimForEnrichmentAsync` at :25), so Pending is genuinely persisted.
  It avoids timing races (Q2). Conditions: the gate sits outside the production decorator chain and starts no
  Activity of its own, so the HTTP -> Enqueue -> publish -> consumer -> Metadata.Lookup span chain (Q1) is unaltered. Its
  wait carries a bounded timeout, so a broken test fails rather than hangs. It must pass `ValidateScopes`/`ValidateOnBuild`
  (recon:78).
- Missing fact for the plan, not for this ruling (needs recon): the lifetime and descriptor form (implementation type
  vs factory) of the `ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>` registration after
  `AddHandler` composes Validation, Logging and Tracing (recon:116). The wrapper must resolve the fully decorated
  original, not the bare handler.

## Q3 follow-up - missing fact closed

recon-DEV-318:146-182 (supplied by Conductor via Keel) records the facts Q3 asked for. The interface registration is a
Scoped ImplementationFactory (:166). Compose returns Tracing -> Logging -> Validation -> handler (:171). The consumer
resolves it per delivery in an AsyncServiceScope (:173). `configureTestServices` runs after app registration, under
ValidateScopes/ValidateOnBuild (:177-178). Keel's plan is to capture the original descriptor factory and replace only
that interface descriptor with a scoped outer gate that invokes the captured factory with the current scoped provider.
It never re-resolves the replaced interface or builds a second provider, and it keeps per-test gate state with no
scoped service inside. That satisfies the Q3 condition that the wrapper resolve the fully decorated original. Closed;
no new ruling.

## Q4/12 - Trace proof shape and its bounds

**Question (Keel):** Keel recommends one request-correlated trace proof in the enhanced import scenario:
- Install a disposable test ActivityListener before the HTTP import, with AllData sampling and thread-safe capture of
  stopped spans.
- Send a unique W3C traceparent and tracestate on the HTTP request, with no ambient Activity in the test process.
- Require these spans to share the request trace: the actual API server span, the import handler ancestry,
  Enrichment.Enqueue, the RabbitMQ.Client.Publisher publish, the Enrichment.Process consumer, the processing handler,
  and Metadata.Lookup.
- Assert publish.ParentSpanId == enqueue.SpanId and consumer.ParentSpanId == publish.SpanId, plus propagated
  tracestate, Consumer kind, no redelivery link on first delivery, and Metadata.Lookup descending from the processing
  span.
- Filter by request trace and movie id where available, not by global span counts.
- Wait boundedly for stopped spans, so reaching Enriched does not race trace completion.

This relies on real broker parentage, rather than stealing a message with BasicGet or accepting TraceId equality alone.
The cost is extra local capture and ancestry assertions. Is this sufficient positive-path proof, without expanding into
retry, redelivery or OTLP exporter scenarios?

**Ruling:** Approved as the sufficient positive-path proof for DEV-318. Retry, redelivery, DLQ and OTLP exporter
scenarios are out of scope.

- It meets ticket DEV-318:11 (one trace API -> RabbitMQ -> Worker) and the full constitution VI chain at :220
  (API -> RabbitMQ -> Worker -> metadata provider). It uses the header inject/extract mechanism from :222-224, and it
  closes the gap recon-DEV-318:136 found: no existing test starts the trace from an HTTP request. The parent-span
  assertions fulfil the Q1 condition (wire parentage, not a shared TraceId alone; recon:133).
- Not expanding scope: the redelivery link and retry/DLQ trace behaviour are already proven at the consumer level
  (recon:121,125: EnrichmentConsumerTests :606-653 and :656-744). Constitution :304 assigns retry-queue, DLQ and
  single-trace-id their own integration tests, and DEV-318 owns only the last of these. OTLP export is
  ServiceDefaults wiring (constitution :235), not part of this ticket's chain.
- Conditions:
  - The listener is disposed in `finally` and selects only by the request trace id, so other ApiEndToEnd collection
    tests cannot pollute it or be polluted by it.
  - The Q3 gate creates no span inside the asserted chain.
  - Telemetry names come from `TelemetryConstants` (constitution :225-229); no new string literals are allowed outside
    test-only source-name constants that are already defined in production (recon:117,135).
  - The bounded span wait fails with the missing span named.

## Q5/12 - Loop terms: closing bar, frozen scope, review cap

**Question (Keel):** Keel recommends these loop terms.
- **Closing bar:** zero unresolved Critical, High or Medium in-scope findings, plus every applicable gate meeting the
  pipeline. Low findings get an explicit disposition and may remain nonblocking.
- **Frozen scope:** an additive, deterministic Pending-to-Enriched HTTP import integration proof using the existing real
  Postgres/RabbitMQ/WireMock stack. It covers the unchanged details GET plus metadata/provider evidence, the scoped
  decorated-handler test gate, and HTTP-request-correlated trace ancestry through broker publish, the active consumer
  and Metadata.Lookup. Files are limited by Q3, and anything else is a follow-up issue, not a finding in this round.
- **Review cap:** exactly two rounds, at most two fix commits per round, and no third autonomous review/remediation
  round. Above-bar findings still unresolved after the cap stay blocked for user review on the PR and are never waived.
- **Axis reports:** all three axis reports (Sentry, Ledger, Compass) are required for this L ticket.

The cost is that Medium findings can prevent closure, and exhausting the cap stops automatic fixes. Approve these
loop terms?

**Ruling:** Approved as stated.

- Caps: task-pipeline:26-27 sets the review cap at 2 rounds, with third-round findings going to user review with the
  PR, and the remediation cap at 2 fix commits per round. Keel's terms restate these exactly. Waiving an above-bar
  finding is not available: the charter says the user merges every PR (lamuflix-team-charter:30).
- Axes: task-pipeline:114-118 always runs the Sentry/Ledger/Compass fan-out on M and L, and Keel never adjudicates
  with an axis report missing (`blocked: missing axis <name>`). task-pipeline:119-120 limits remediation to 2 or fewer
  rounds and adds the L mid-row checkpoint. Charter :29: a round is finished only when it is posted on the PR.
- The bar and the frozen scope are sound practice for a test-only ticket. The scope restates Q1-Q4 and the Q3 file
  limit, so anything outside it is a follow-up issue (role rule: file one only if it is Critical/High or broken
  behaviour; otherwise `noted, no ticket`). Low findings need a recorded disposition on the PR. Ruled once for the
  DEV-318 loop, not re-ruled per round.

## Q6/12 - Property-test opt-out and validation strategy

**Question (Keel):** Keel recommends no new property-test or mutation target, because the change is integration proof
only, with no domain invariant and no production C# under src.
- **Opt-out:** record `propertyTests: opt-out - no domain invariants change`. Existing property tests still run through
  the pipeline, and a no-tag exit 2 is accepted only after that opt-out appears in the DEV-318 task note.
- **Gauge verifies later:**
  - Roslyn analyzers.
  - Complexity, at configured thresholds, with a refactor bar of 6 or less.
  - InspectCode on the changed diff, never `-All`.
  - The targeted enhanced import scenario, then the full `dotnet test`, including architecture tests.
  - Property tests, vulnerable packages and format verification.
  - Whether the pipeline's mutation and web gates apply.
- **Expected results:** on this frozen scope, mutation and web are expected `SKIPPED (scope-empty)` from the script
  verdicts, never PASS, and no gate is waived by an expectation. Exit 1, `Could not run`, or an unaccepted
  property-test exit 2 blocks. A configured-disabled SKIP and a mutation `NOT APPLICABLE` remain distinct from PASS
  under task-pipeline:97-107.
- **Not allowed:** threshold edits, Stryker `--since`, and unrelated harness fixes or production patches if
  verification exposes unrelated defects.
- **The import scenario** runs with the gate held, then released. It uses bounded waits, cancellation and cleanup,
  with diagnostics for a missing span or a stuck gate. No synthetic unit tests duplicating the helper implementation
  are required.

The cost is that real container test runtime and full gate evidence remain mandatory despite a tests-only diff.
Approve the property-test opt-out and this validation strategy?

**Ruling:** Approved. The property-test opt-out reason is `no domain invariants change`; the Conductor records it in
the task note.

- task-pipeline:102 accepts property-test exit 2 only when the task note carries `propertyTests: opt-out - <reason>`
  for a change with no domain invariants. By Q3, DEV-318 adds no Core or `src/` code, so the opt-out is factual.
  task-pipeline:101 makes mutation `SKIPPED (scope-empty)` when there is no production C# under `src/`, and :106 makes
  web SKIPPED without `/web`. Both are script verdicts, never PASS.
- task-pipeline:97-105 and :107: the full gate set runs, InspectCode runs with no arguments and never `-All` (:100),
  and only exit 1, `Could not run` or an unaccepted property-test exit 2 block. Q5's frozen scope bars threshold
  edits and fixing unrelated defects; such defects become follow-up issues.
- Clarification: the refactor complexity bar (6 or less) is Cog's `/refactor` pass over the build (task-pipeline:95).
  Gauge's complexity gate runs at the configured threshold (:99). Both apply to the new test code. Skipping synthetic
  unit tests of the helper is sound: the real held-then-released scenario exercises it (constitution :299-301).

## Q7/12 - Shared-understanding check and grill closure

**Question (Keel):** Final shared-understanding check. Keel's consolidated plan:
- **Delivery:** extend the existing `ImportMovieFolder_RealScannerThroughOmdb_PersistsEnrichedMovieWithMeasuredProviderEvidence`
  scenario additively, keeping every existing import, details and WireMock assertion and the owned-folder cleanup test.
  The gate and capture helpers default to private nested types in `ApiEndToEndImportTests.cs`. Only if extraction
  improves clarity or the gates require it, exactly `ApiImportEnrichmentGate.cs` and `ApiImportTraceCapture.cs` may sit
  beside it, with no shared fixture, factory or base edits.
- **Gate:** the registration asserts the expected scoped factory form and uses its captured factory in the same
  consumer scope. One per-test gate controller, with no new Activity.
- **Test sequence:** wait for the real consumer to reach the gate and for the 202; parse the Location id and match the
  delivery MovieId; assert a fresh persisted Pending; release; await Enriched; assert the details and one measured real
  OMDb call; then wait for the trace proof.
- **Waits and cleanup:** the existing 30-second integration wait budget and cancellation bound every wait, with async
  continuation signaling, request-correlated stopped spans and named diagnostics. The gate is released unconditionally
  before host shutdown, the listener is disposed, and the existing owned-folder cleanup runs.
- **Trace capture:** the new capture is installed before the host and the import, and uses a unique HTTP traceparent and
  tracestate with no ambient test Activity.
- **Ordering for Quill:** test-local synchronization and capture support first; then the additive scenario with its
  parentage and status assertions; then the targeted real-stack run and full pipeline evidence. Wisp (recon and drift)
  and Gauge (measurements and gates) remain separate owners. All delivery code tasks belong to this one proof, with no
  independently broader phase.
- **Size and later steps:** size stays L, so the later Quill clarify/checklist and the ADR/plan challenge are still
  required. The ADR will record the integration-proof approach without adding a domain term, port or architecture
  layer. No ADR draft now.
- **Status:** Q1-Q6 rulings stand, with no taste decision and no assumed overflow. The Q2 provenance caveat about the
  ticket summary stays explicit, with no current owner checkbox on the supplied evidence. No recon blocker remains after
  the supplement.

Do we now have shared understanding and the complete decision set, authorizing Keel to finalize brief.md and stop
before Quill or later Phase 2 work?

**Ruling:** Shared understanding confirmed and the decision set is complete. The grill closes at 7/12. Keel is
authorized to finalize brief.md and stop before Quill or any later Phase 2 work.

- The consolidated plan restates Q1-Q6 without drift:
  - Q1: Worker is the hosted consumer, and wire parentage is proven.
  - Q2: Pending and Enriched are persisted, and the GET contract is frozen.
  - Q3: the files are limited (the two optional helper files fall within "test-only helpers beside it"), the gate wraps
    the decorated factory and starts no Activity, and its waits are bounded.
  - Q4: the trace proof starts from the request, through Metadata.Lookup.
  - Q5: the bar, frozen scope and cap.
  - Q6: the property-test opt-out and the gates.
  The additions (matching the Location id to the delivery MovieId, and one measured OMDb call, recon-DEV-318:85) only
  tighten those rulings. No ruling is reopened.
- Process: task-pipeline:14-17 keeps L on the clarify/checklist, ADR, plan challenge and spec PR path through Gate 1,
  so stopping before Quill is correct. No `[assumed]` entries were needed (grill cap task-pipeline:25 not reached), so
  ASSUMPTIONS.md stays empty.
- Open item carried to the spec PR: the Q2 caveat. That ruling rests on the DEV-318:11 ticket summary, and it flips to
  `blocked: structural` only if the verbatim ticket requires a Status field in the GET body. No owner checkbox now.
