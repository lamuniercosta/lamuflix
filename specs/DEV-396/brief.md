# DEV-396 - Phase A decision brief

Grill closed 2026-10-05 by Patron Q6 after six questions. This file owns the plan decisions; Quill drafts spec, plan and tasks from it. Authority: DEV-396 task note:3, recon-DEV-396:108-307, specs/PRODUCT.md and CONCLUSIONS.md Q1-Q6. Worktree: F:/Dev/LamuFlix.worktrees/feature-396-spec; branch feature/396-spec; recon baseline 5033c9bdd05c9ebaab65efbc604a673a8aee540e. Patron ruling commits advanced the branch during the grill; the recon baseline remains historical.

## Frozen scope and loop discipline

Finish omitted DEV-307 OTel tracing/metrics/log composition, same-commit OMDb redaction proof, PostgreSQL/RabbitMQ readiness membership/tags, HandlerOutcome decorator, conditional readiness blocked on inherited Q2, and evidence-only reconciliation of existing task IDs; run the fixed-range missing DEV-308 retrospective, narrowly correct accepted still-live Medium+ findings, and publish required corrective records, gates and merge-bar evidence; anything else is a follow-up issue, not a finding in this round.

Patron P4: Medium+ findings block closure for retrospective and delivery. Two independent budgets apply: DEV-308 historical retrospective (T012) and delivery review (T042), each at most two review/remediation rounds of at most two fix commits per round; neither lends to the other. A round is one mandatory Sentry/Ledger/Compass review of a pinned HEAD, adjudication and remediation. Its closure check belongs to that round, covers only the fixed finding IDs and is done by the originating axis; it is not a fresh review. A commit closing several accepted findings counts once. Record-only commits (CONCLUSIONS/ASSUMPTIONS/receipts/notes), a rebase on main and pre-review Gauge Phase 3 gate fixes consume neither budget; the current plan/ADR challenge is a spec-phase step and does not count. Medium+ still open after round 2 closure, or a third fix commit needed within a round, is terminal: stop with no further commits, third round, extra immutable review, severity downgrade, waiver or merge-bar sign-off. Report once to Bernstein blocked: review cap exhausted - DEV-396 <historical|delivery> <finding ids>; the task stays not delivered. Only the owner can extend the cap, never Patron. A missing axis is blocked, never waived. Lower findings follow Q3: follow-up records; tickets only under Patron rules for Critical/High or broken behavior, otherwise noted, no ticket. No unnecessary abstraction fix. Authority: CONCLUSIONS.md:184-189 (P4).

## Every grill answer

1. Q1 ACCEPT: carry inherited readiness T037-T041 explicitly BLOCKED and the owner Q2 checkbox verbatim with its full FR-034 deferral clause. Never infer an answer or tick it. Gate 1 stays closed. Unblocked work is planned separately; readiness is not counted delivered or deferred while unanswered.
2. Q2 ACCEPT: retain the exact T010 FullName assertion edit in the legacy test file; add equivalent unexpected-exception FullName proof to the active UnitTests decorator fixture. Standalone legacy check is Could not run (NU1010), not active proof. No Worker/Pomelo repair, package addition or solution registration. Ticket AC does not require each edited task file independently gated.
3. Q3 ACCEPT: fresh historical pre-pass and all axes in a disposable detached checkout at full historical head; adjudicate only after all reports. Reproduce accepted Medium+ findings at delivery head before a narrow fix; record resolved findings with evidence. Keep historical and delivery receipts separate; no borrowed artifacts or rewritten PR history.
4. Q4 AMEND: existing harness deletion does not block drafting. Rigger restores committed harness.yml before Phase B gate runs; each gate receipt includes git diff --quiet HEAD -- harness.yml exit 0. Committed base policy is authoritative. Never commit deletion, change thresholds or invent waivers; Gauge records real mutation verdict.
5. Q5 ACCEPT: completion approach, file boundary, tests and phase order below. Reuse current installed pins without claiming an unverified historical owner answer. The historical 2026-10-03 Patron receipt approves N2 first half only under charter 2.3(6); inherited Q1 and delivery receipts remain unproved, and T020A stays unticked (see the evidence-state correction below). No new dependency/schema/project/layer/API shape. Phase order is [assumed] in ASSUMPTIONS.md.
6. Q6 CONFIRM: loop, evidence, publication and handoff terms; shared understanding reached and grill closed. Owner Q2 remains open; Gate 1 remains closed. Rigger alone writes the DEV-307 tracker comment. No merge authorization.

Full exchanges, rationale and cited Patron rulings are append-only in CONCLUSIONS.md. These are not owner answers.

## Retained owner Q2 - unchecked

- **Proposed checkbox:** `blocked: structural - DEV-307 Q2: health routes return JSON {status, checks[name,status,durationMs]}; /health/ready Unhealthy -> 503 ProblemDetails with traceId and checks extension (no exception text); Degraded -> 200; FR-034 (both health routes in the committed web/src/api/openapi.json, with the generated TS) is deferred to the Epic 6 web scaffold ticket DEV-20, which owns the C# to OpenAPI to TypeScript chain for both routes, with DEV-320 (generate the TypeScript API client from the committed openapi.json and configure the MSW handlers) owning the generated TS client afterwards - a 2.3b departure from constitution.md:359-360 for this ticket only?` (Patron rejected the exclusion — `CONCLUSIONS.md` Q2, `constitution.md:359-360` — so the checkbox asks about the documentation rather than proposing to skip it. The document itself does not exist yet, which is need N1; the deferral clause is how N1 is answered, so the clause and the question are the same answer.)

**Ticking the checkbox above without its deferral clause is not an answer, and this says why rather than assuming it.** Without that clause Q2 can be ticked while FR-034 stays unsatisfiable and the C# to OpenAPI to TypeScript chain for the two health routes has **no owner at all** — which is need N1 recorded as a gap and nothing else. With it, the owner either accepts the deferral, and FR-034 rides DEV-20 with DEV-320 after it, or **declines it, and this ticket is then `blocked: structural` with Gate 1 staying closed**: building the OpenAPI document here needs a new package and a new top-level folder (`PRODUCT.md:43-44`), which is precisely what the deferral exists to avoid, and this brief cannot answer that on the owner's behalf. Either answer is deliverable; an answer that leaves FR-034 orphaned is not.

- [ ] Owner supplies the complete inherited Q2 answer including acceptance or rejection of the deferral clause above. This checkbox is open; no Patron ruling closes it.

Readiness writer, mapping and tests run only after the owner answers Q2; T037-T041 remain BLOCKED meanwhile. Record the exact eventual answer at the DEV-396 equivalent of T048. An explicit owner deferral must be recorded as such; it is not implementation approval or an inferred answer. FR-034 is not delivered by this branch; conditional DEV-20/DEV-320 ownership must never be described as already accepted. No web scaffold, AddOpenApi, OpenAPI package or generated TypeScript change here.

## T020A evidence-state correction and separate inherited N2 owner question - 2026-10-05

The 2026-10-03 Patron ruling approves **N2 first half only**, under charter 2.3(6): the Api ProjectReference and versionless Microsoft.AspNetCore.Mvc.Testing PackageReference in tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj. Basis: recon-DEV-396-T020A-owner-answer:7-17 (exact ruling and chronology, citing inherited brief.md:103, D1:119 and plan.md:284); DEV-396:418-423. This corrects the earlier missing-approval claim. It does not resolve inherited DEV-307 Q1, prove implementation or approve Kestrel. Inherited DEV-307/tasks.md:58 requires Q1 plus Patron approval; T020A remains unticked pending its Q1 and delivery receipts. Installed references, package pins and the earlier PR #79 tick are not those receipts (DEV-396:419,423).

OD-1 is the retained health-body and FR-034 OpenAPI/TypeScript deferral question above only. It does not cover the inherited N2 second half or a real Kestrel test host. Basis: DEV-396:425; inherited DEV-307/brief.md:7,74,119 reserves that separate constitution departure under charter 2.3b. The earlier conclusion that OD-1 was the only owner question is corrected by this record.

- [ ] **OD-2 - distinct user decision:** blocked: structural - Inherited DEV-307 N2 second half: approve a test-composed real Kestrel host in place of default WebApplicationFactory for the inherited host proof, as the constitution departure reserved in DEV-307/brief.md:7,74,119 under charter 2.3b, or retain default WebApplicationFactory?

Patron recommendation: **retain default WebApplicationFactory**. Perform no Kestrel work without explicit user approval. The unchecked OD-2 question is separate from OD-1; neither a Patron answer nor N2 first-half approval closes either user checkbox. Record the exact user disposition before changing the host approach; no Kestrel scope is authorized by this record.

This is a decision-record correction only. spec.md, plan.md and tasks.md still need the bounded amendment identified in DEV-396:424,426. No artifact freeze, checkbox completion, Gate 1 or PR-state change, implementation proof, gate or test receipt is supplied here.

## Four evidence constraints

- recon:174,221,301: Q2 remains blocked:structural; readiness is not runnable.
- recon:184-186,213,292: tests/LamuFlix.Test is absent from LamuFlix.sln; standalone build exits 1 with NU1010 from Worker/Pomelo missing CPM version. Root test baseline excludes it; T010 is ungated independently.
- recon:196-199,304: no DEV-308 pre-pass/axis artifact exists; retrospective starts cold. Historical PR #83 is already merged and has no reviews/comments in this snapshot.
- recon:125-131,226,302: pre-existing unstaged harness.yml deletion removes mutation exclusions, including LamuFlix.Api. Preserve the recon receipt; do not adopt the local deletion as policy or commit it. Rigger restoration before Phase B follows Q4.

## Approach

Add OTel to existing AddServiceDefaults while preserving existing clock, options, Serilog console and idempotent composition. Traces cover ASP.NET Core, HttpClient, Npgsql, application ActivitySource, RabbitMQ.Client.Publisher and RabbitMQ.Client.Subscriber (names centralized in TelemetryConstants). Metrics cover ASP.NET Core, HttpClient and the existing LamuFlix identity; logs reach OTLP alongside the console pipeline. Use argument-free exporter configuration and retain environment endpoint precedence; no invented service.name or extra metric identity.

Patron P1: default URI redaction on the installed pins, with no opt-out, is sufficient subject to T018 proving it; no production logging change, SuppressDefaultLogging, System.Net.Http.HttpClient.* level filter or AddExtendedHttpClientLogging. MetadataProviderServiceCollectionExtensions.cs keeps health membership as its only edit. HttpClient instrumentation (T016) and T018, carrying inherited T033A, land in the same commit. Cog changes MetadataProviderProbe in exactly three ways: retain categoryName on every record, capture Debug and above, and record scope state with active scope payloads attached to each record. Anvil creates MetadataProviderTelemetryTests using the real lookup. Before absence assertions require at least one captured category starting System.Net.Http.HttpClient. and ending .LogicalHandler or .ClientHandler, read from capture without hardcoding the typed-client name; at least one such record has structured Uri containing the stub host and path and neither apikey nor the sentinel (no exact ?* assertion). Also require a stopped ActivityKind.Client HttpClient-instrumentation activity for the stub host plus the LamuFlix Metadata.Lookup activity. Over every captured activity and record, including resilience-handler records, exclude the sentinel and its Uri.EscapeDataString form from tag keys/values (stringified TagObjects), events, status descriptions, log categories, rendered messages, structured keys/values, scope payloads and exception text. No forced failure-path case; sentinel-key is a test sentinel, not a credential. The pinned call site and url.full remain unverified until T018; a disclosed sentinel returns to Patron as Medium+, never an opt-out, suppression or weaker assertion. Authority: CONCLUSIONS.md:146-158 (P1), with the cited recon basis.

HealthCheckTags.Ready lives beside existing Core/Pipeline constants. Infrastructure registers the existing DbContext postgres check, tags postgres/RabbitMQ with the shared constant and leaves metadata-provider registered but untagged. Registration proof reads real registrations, independently of endpoint stubs. No schema/migration or driver mock.

TracingDecorator retains validation Unset with lamuflix.handler.outcome=validation_failed; all other exceptions set Error and error.type FullName, preserving rethrow. Keep inherited Decision B spelling, T010 correction and active behavioral proof. Preserve delivered liveness/WAF; reconcile boxes against receipts rather than reimplementing or blanket marking them. T020A stays unticked pending inherited Q1 evidence and delivery receipts; the found Patron approval covers N2 first half only and authorizes no Kestrel work.

## File boundary

Production completion:
- src/LamuFlix.Core/Pipeline/TelemetryConstants.cs
- src/LamuFlix.ServiceDefaults/Extensions.cs
- src/LamuFlix.Core/Pipeline/HealthCheckTags.cs (new)
- src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
- src/LamuFlix.Infrastructure/RabbitMq/RabbitMqServiceCollectionExtensions.cs
- src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs
- src/LamuFlix.Infrastructure/Pipeline/TracingDecorator.cs

Test completion:
- tests/LamuFlix.UnitTests/ServiceDefaultsTests.cs
- tests/LamuFlix.UnitTests/OtlpEndpointEnvironmentTests.cs (new)
- tests/LamuFlix.IntegrationTests/MetadataProviderProbe.cs (category, Debug-and-above capture and attached active scope payloads only; Patron P1 supersedes the earlier two-change limit)
- tests/LamuFlix.IntegrationTests/MetadataProviderTelemetryTests.cs (new)
- tests/LamuFlix.UnitTests/HealthCheckRegistrationTests.cs (new)
- tests/LamuFlix.UnitTests/Pipeline/TracingDecoratorTests.cs (new)
- tests/LamuFlix.Test/TracingDecoratorTests.cs (T010 assertion only)

Conditional readiness only after owner Q2:
- src/LamuFlix.ServiceDefaults/HealthCheckResponseWriter.cs (new)
- tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs (new)
- tests/LamuFlix.IntegrationTests/HealthCheckStubs.cs (new)
- tests/LamuFlix.IntegrationTests/ApiHostFactory.cs and ApiHostCompositionTests.cs, narrowly for readiness stubs/contract assertions if required

Evidence: specs/DEV-396 artifacts, specs/DEV-307/tasks.md individual receipt/deferral updates, task note, historical and delivery findings artifacts and corrective PR records. No CPM/csproj/sln changes, Worker repair, unrelated consumer MarkError edits, threshold changes, schema, LocalPlay or process execution changes. Accepted retrospective fixes require file:line/current-head proof and a narrowly recorded file addition to the plan before editing; their paths cannot be guessed before reports exist.

## Tests and gate expectations

Use existing xUnit/Shouldly conventions. Prove validation and unexpected exception paths with ActivityListener, including status, attributes and propagation; the active UnitTests FullName assertion must execute. Prove all three telemetry providers carry exporters and share default/environment endpoint options. Isolate process environment mutation in a nonparallel collection and restore in finally. Prove actual instrumentation behavior and that removing each instrumentation registration fails its own check. Provider/exporter composition is never described as collector export delivery. Prove real health registrations, actual OMDb span/log redaction, and conditional readiness route states/body only after Q2.

Patron P3: production exporters remain argument-free and silent when no collector is reachable: telemetry is dropped, the application keeps running, and diagnostics go only to OpenTelemetry-Exporter-OpenTelemetryProtocol and OpenTelemetrySdkEventSource, never ILogger/Serilog. Operators may opt into OTEL_DIAGNOSTICS.json; no diagnostics file is committed or created by tests. No exporter timeout, protocol, endpoint or processor argument is set. The documented shutdown budget is up to 10 s per exporter, accepted but unmeasured. Exporter-output assertions follow explicit ForceFlush(timeout) returning true; never sleep or wait out a batch delay. Every telemetry-test host/provider is disposed in finally. Environment mutation is confined to non-parallel collections and restored in finally; recording and negative-control checks run only there with no other composed provider alive. If T016 affects MetadataProviderLookupTests.cs:525-556 or EnrichmentConsumerTests.cs:276-281 capture, narrow correlation by source/operation or trace id; never globally disable telemetry or skip a test. Anvil records IntegrationTests duration before/after T016 and the slowest ApiHostFactory disposal. Disposal over 12 s or suite growth over 25 percent returns to Patron as a Medium finding, never unilateral test-host exporter overrides. Framework-level gRPC logs and disposal cost remain unverified until measured. Authority: CONCLUSIONS.md:176-182 (P3), with the cited recon basis.

Patron P2a: P2 stands. For T019, ActivityListener Sample=None with SampleUsingParentId unset is a passive observer beside the production-composed OTel listener, which alone creates and records. A row passes only after observed ActivityStopped with IsAllDataRequested and Recorded both true; no qualifying activity is the negative control. The test listener never samples above None: PropagationData, AllData and AllDataAndRecorded are forbidden. This is a source-level conclusion, not execution proof: the first T019 execution must stop and report to Patron if the production host yields zero qualifying activities, without changing the sampler, environment or observer, or dropping a row. Authority: Patron note DEV-396:357-360; recon-DEV-396 Q6:710-789, especially 732-738, 751-758 and 783-787.

Recon baseline: 621 passed (ArchitectureTests 13, UnitTests 384, IntegrationTests 224), 16 property tests, format/vulnerable package checks exit 0; scoped analyzers exit 0 at baseline 15 complexity ceiling. These are historical baselines, never delivery gates. Track counts/deltas honestly; legacy tests contribute zero. Standalone legacy NU1010 is a separate known Could not run limitation, not a substitute for or failure waiver on required active gates.

Gauge runs all required current task-pipeline Phase 3/5 gates: Roslyn, complexity (configured normal/refactor ceilings), InspectCode on delivery diff, mutation, property tests, vulnerability scan, format, dotnet test and applicable web gate. Receipt captures each command, exit and full verdict. Exit 1/Could not run never passes. Scope-empty exit 2 is non-blocking SKIPPED (scope-empty), never PASS; disabled gate is SKIP. Mutation NOT APPLICABLE is permitted only on the actual script exit-2 classification, not by seat waiver. Property exit 2 needs a recorded propertyTests opt-out reason. No threshold lowered; surviving mutants require tests. Q4 clean harness receipt precedes gates. Baseline script verdicts prove no future changed-project eligibility.

## Order and task ownership

1. Phase B pickup drift/evidence recon; Rigger restores harness before gates.
2. Fresh cold retrospective: Conductor coordinates Rigger detached checkout at 7a35e7727241c3afd92ac9ba21fa9d13da37cdd5; Gauge pins base 122c502b03b0eaffe18b79b0fd26183466d8f0d0 and full head in pre-pass. Sentry/Ledger/Compass handshake and report; Keel adjudicates. Current-head reproduction precedes Cog remediation of accepted still-live Medium+ findings. Retain artifacts before retiring the isolated checkout.
3. OTel composition and same-commit redaction/test fixture proof.
4. Health membership/constant and real registration tests.
5. HandlerOutcome decorator, legacy assertion and active UnitTests proof.
6. Conditional readiness only after Q2; no provisional body/default writer.
7. Per-task evidence reconciliation, gates/refactor/architect and full delivery review per pipeline, corrective PR/tracker records and merge-bar publication.

Tests accompany each phase. Shared Extensions.cs/TelemetryConstants edits are sequential. Wisp owns recon, Anvil creates new M/L code, Cog alters/remediates, Gauge runs gates, Keel adjudicates only with every axis, Patron decides tracker recording and Rigger writes it. This brief schedules no new ticket automatically.

## Close-out and exact next action

Every inherited task ID/outcome maps to a concrete test, gate or inspection receipt, BLOCKED status or explicit owner deferral. Do not retroactively tick T020A or Q2 from installed files. Separate historical finding, current reproduction, fix and delivery receipts. Publish retrospective findings/adjudication summary in the corrective PR, corrective comment on PR #82 and DEV-307 through the authorized seats, and Keel merge-bar PR comment. Awaiting user merge is the terminal shipping state.

Next: Conductor asks Quill, in this worktree on feature/396-spec, to run /speckit-specify DEV-396, /speckit-plan and /speckit-tasks with L clarify/checklist, from this brief, CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-396 and specs/PRODUCT.md. Set PYTHONUTF8=1. Preserve Q2 unticked/T020A open and all proof limits. Quill drafts, never decides; any gap is needs decision to Keel. Facts outside recon are needs recon, not new searches.

After draft: Keel read-only /speckit-analyze and brief-versus-plan/tasks check; one numbered Quill fix list per round. L ADR step follows task-pipeline Phase 2:74; obtain Wisp numbering/context facts if needed rather than inventing an ADR number. No new domain terminology arose; no CONTEXT.md change was decided. Then three-axis plan challenge, Keel adjudication/freeze, Patron provisional gate and Rigger spec PR. Q2 stays owner-open and Gate 1 closed until the user answers; a provisional spec PR does not approve implementation.
