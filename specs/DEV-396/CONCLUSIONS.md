# DEV-396 - Patron rulings

## Q1 - Carrying readiness while Q2 is unanswered

Verdict: **ACCEPT** (Keel recommendation) - keep T037-T041 explicitly `BLOCKED until Q2 is answered`; brief.md carries the DEV-307 Q2 owner checkbox verbatim, deferral clause included, unticked; plan the unblocked observability, registration and `HandlerOutcome` decorator work separately. No readiness route/body code until the owner ticks Q2 or records an explicit deferral.

- Basis: ticket text `DEV-396:3` names "readiness blocked on Q2" - decided scope, so dropping or silently omitting readiness would change the ticket (2.3a). Recon `recon-DEV-396:174,221,301`: Q2 still open per `specs/DEV-307/CONCLUSIONS.md:25-36`.
- Authority under current 2.3: the body shape alone would be a Patron care item (item 4), but Q2 is bundled with the FR-034 OpenAPI deferral, a constitution departure (`constitution.md:359-360`, 2.3b), which only the owner answers; `specs/DEV-307/brief.md:78` makes the two one answer. task-pipeline:67's broader wording is superseded by the role's 2.3. Patron does not tick or split it.
- Retained text (copy exactly from `specs/DEV-307/brief.md:76`, the `Proposed checkbox` line, starting `blocked: structural - DEV-307 Q2: health routes return JSON {status, checks[name,status,durationMs]}` and ending `- a 2.3b departure from constitution.md:359-360 for this ticket only?`), plus the `brief.md:78` rule that ticking without the deferral clause is not an answer. The answer is recorded at the DEV-396 equivalent of DEV-307 T048. Cost accepted: Gate 1 stays owner-blocked; readiness is not counted as delivered.

---

### Q1 complete exchange - Keel record

Keel question: How should DEV-396 carry readiness while Q2 remains unanswered? Recommendation: preserve T037-T041 as explicitly blocked, copy the existing owner Q2 checkbox and conditional FR-034 contract-chain deferral unticked, and separately plan unblocked observability, registrations and decorator work. No silent omission or route/body implementation before an owner answer or explicit owner deferral. Cost: Gate 1 stays owner-blocked and readiness is not delivered.

Patron answer: ACCEPT. Keep T037-T041 BLOCKED, copy the Q2 checkbox verbatim from specs/DEV-307/brief.md:76 with its FR-034 DEV-20/DEV-320 deferral clause and the brief.md:78 not-an-answer rule, leave it unticked, and plan the unblocked work separately. Basis: DEV-396:3 says readiness blocked on Q2, and the deferral is a 2.3b owner item. Ruling reported at commit ff89e52.

Implication: retain the conditional work in the specification and record any eventual owner answer in the DEV-396 equivalent of T048; no Patron answer resolves it.

## Q2 - Keeping inherited T010 without claiming ungated proof

Verdict: **ACCEPT** (Keel recommendation) - a permissible proof placement inside the ticket; no owner disposition needed. Keep the exact T010 edit at `tests/LamuFlix.Test/TracingDecoratorTests.cs:65`, add an equivalent unexpected-exception FullName assertion in the new `tests/LamuFlix.UnitTests/Pipeline/TracingDecoratorTests.cs`, and record the standalone legacy check as `Could not run` (project not in `LamuFlix.sln`, NU1010).

- Basis, ticket text: DEV-396 AC asks for "task boxes ticked or owner deferrals in PR body" and "harness gates with exits" (`DEV-396` note:3). T010 is ticked because its stated edit is made; the AC does not require each task's file be gated, and gate exits are reported truthfully. Nothing the ticket delivers is added or dropped, so 2.3a does not trigger.
- Proof claim: behavioral proof is the active UnitTests assertion only; close-out must say the legacy edit is unverified by any build (`recon-DEV-396:186`). Claiming it as gated would be false evidence.
- Out of scope: no Worker/Pomelo repair, `PackageVersion`, or solution registration in DEV-396 - those would be a new dependency and a change to a tracked file the ticket does not name, with no AC or gate forcing them (2.3 items 1 and 6).

---

### Q2 complete exchange - Keel record

Keel question: How do we keep inherited T010 without claiming ungated proof? Recommendation: retain the exact FullName assertion edit in tests/LamuFlix.Test/TracingDecoratorTests.cs and require equivalent unexpected-exception FullName proof in active tests/LamuFlix.UnitTests/Pipeline/TracingDecoratorTests.cs alongside validation/outcome tests. Record the orphan project NU1010 standalone check as Could not run; root dotnet test proves only active projects. Exclude Worker/Pomelo repair, package additions and solution registration. Cost: the legacy file remains independently ungated. Is this permissible proof placement within the ticket, or does its acceptance criterion require owner disposition?

Patron answer: ACCEPT, this placement is allowed within the ticket and needs no owner disposition. The AC asks for ticked boxes or owner deferrals plus gate exits; it does not require each task file to be gated. Keep the T010 edit, add the active UnitTests FullName assertion, and record the legacy standalone check as Could not run (NU1010). No Worker/Pomelo/sln work. Ruling reported at ae00e57.

Implication: never report standalone legacy coverage as passing or substitute its assertion edit for executing the active behavioral test.

## Q3 - Retrospective execution and proof boundary

Verdict: **ACCEPT** (Keel recommendation), with checkout coordination fixed: fresh Gauge pre-pass pinned to `122c502b03b0eaffe18b79b0fd26183466d8f0d0..7a35e7727241c3afd92ac9ba21fa9d13da37cdd5`, then all three axis reports (Sentry/Ledger/Compass), then Keel adjudication. Retrospective findings and receipts stay separate from DEV-396 delivery review. Accepted Medium+ findings are reproduced against the current DEV-396 head before any narrow fix; findings that are already resolved are recorded with evidence. Lower findings become follow-up records.

- Basis, ticket text (`DEV-396` note:3): "retrospective diff 122c502..7a35e77 with Gauge artifact, Sentry/Ledger/Compass axes, Keel adjudication, Medium+ fixes and lower findings as follow-ups; corrective comments on PR #82 and DEV-307". Recon `:196-199,304`: no Gauge artifact exists, so the pass starts cold. The recommendation adds and drops nothing (2.3a not triggered).
- Checkout: run the pre-pass and axes in a disposable detached worktree at the full head SHA `7a35e77...` (for example `F:\Dev\LamuFlix.worktrees\review-308-7a35e77`), never in the main checkout or `feature/396-spec`. Every artifact names its base and head SHAs. A fix applied in DEV-396 cites both the historical finding and a reproduction at the current DEV-396 head. Delete the worktree once the artifacts exist (disposable, no ruling needed).
- No rewriting of PR history and no borrowed artifacts: the one existing `pre-pass-0c9c052` belongs to another run (recon `:199`). Current-head applicability is a fact for the reproduction pass to establish, not one to infer here. Lower findings get YouTrack tickets only if Critical/High or broken behaviour; anything else is a `noted, no ticket` line, per Patron's role rules.

---

### Q3 complete exchange - Keel record

Keel question: What is the retrospective execution/proof boundary? Recommendation: fresh Gauge pre-pass pinned to 122c502b03b0eaffe18b79b0fd26183466d8f0d0..7a35e7727241c3afd92ac9ba21fa9d13da37cdd5, full-SHA checkout/artifact handshake, all three axes before adjudication, separate historical and delivery receipts, current-head reproduction of accepted Medium+ findings before narrow fixes, and evidence for already-resolved findings. Lower findings become follow-up records. No rewriting PR #83 history or borrowed artifacts. Cost: historical analysis and current applicability are separate passes.

Patron answer: ACCEPT. Run the fresh Gauge pre-pass and Sentry/Ledger/Compass axes in a disposable detached worktree at full SHA 7a35e77, with every artifact naming its base and head SHAs. Keel adjudicates, then reproduces accepted Medium+ findings at the DEV-396 head before any narrow fix; resolved findings get recorded with evidence. Lower findings become follow-up records (ticket only if Critical/High or broken, otherwise noted, no ticket). No borrowed artifacts. Ruling reported at c2bd797.

Implication: Conductor coordinates the isolated historical checkout; no active-head report can impersonate the historical head. Rigger alone records any Patron-decided tracker change.

## Q4 - Pre-existing unstaged harness.yml mutation-exclusion deletion

Verdict: **AMEND** - this does not block Phase A drafting; it blocks only the Phase B gate runs. The committed `harness.yml` at the branch base (`5033c9b`, which carries `gates.mutation.exclusions` / `LamuFlix.Api`) is the sole authoritative configuration. DEV-396 never commits the working-copy deletion and never changes thresholds or adds waivers.

- Provenance is already recorded; no ownership hunt is needed. Rigger's Phase 1 intake (`DEV-396` note:3) says worktree setup "copied ignored harness.yml" into the new worktree, and recon `:125-127` measures the result: a 3-line deletion against committed `main:harness.yml`. It is a setup artifact of this worktree, not authored work on this branch.
- Disposition: before any Phase B gate runs, Rigger restores `harness.yml` in this worktree to the committed version (`git restore harness.yml`), and every gate receipt shows `git diff --quiet HEAD -- harness.yml` exiting 0. If the copied content reflects an intended change somewhere else, it lands through its own ticket, not DEV-396. Restoring a working copy to its committed content rewrites nothing that is tracked (2.3 item 6 not triggered).
- Proof: neither the recon-time exit 2 (scope-empty) nor an assumed Api exclusion proves anything about mutation eligibility. Gauge records the actual `run-mutation.ps1` verdict and exit code on the delivery diff, against the committed config (ACCEPT on Keel's point).

---

### Q4 complete exchange - Keel record

Keel question: How should DEV-396 handle the pre-existing unstaged harness.yml mutation-exclusion deletion? Recommendation: preserve it untouched, exclude it from commits, and have Conductor/Rigger identify and isolate prior ownership before Phase B gates. The committed configuration defines policy; no threshold change or waiver, and the scope-empty baseline is not proof of delivery eligibility. Gauge records the actual delivery verdict. Does it block drafting or later gates?

Patron answer: AMEND. This does not block Phase A, only the Phase B gates. The committed harness.yml at 5033c9b, with the Api exclusion, is authoritative. Provenance is already known: Rigger copied harness.yml in during worktree setup (DEV-396 note:3), so no ownership hunt is needed. Before Phase B, Rigger runs git restore harness.yml, and every gate receipt shows git diff --quiet HEAD -- harness.yml exiting 0. Never commit the deletion; no threshold changes or waivers; Gauge records the real run-mutation verdict.

Implication: Keel preserves this evidence and performs no harness edit. Rigger performs the explicit restoration before delivery gates; recon baseline receipts remain historical receipts, never rewritten.

## Q5 - Implementation approach, boundary and order

Verdict: **ACCEPT** (Keel's plan as proposed); no recon fact is missing. Readiness writer, routes and tests are built only once the owner ticks the Q2 checkbox carried under Q1; until then T037-T041 stay `BLOCKED`, are not counted as delivered, and are not reported as deferred.

- Care items cleared from recon: no new dependency, because every OTel, Npgsql, health-check and Mvc.Testing package is already pinned and referenced (`recon-DEV-396:141-146,219`). No schema change: `AddDbContextCheck` reads an existing context (`:220`). No new project or layer: `HealthCheckTags` sits beside `TelemetryConstants` in the existing `Core/Pipeline` (`:148,164`). No API shape beyond `specs/DEV-307/spec.md` (`:221`). No CPM or project edits.
- ACs held as written: T033A's same-commit redaction proof travels with the HttpClient instrumentation (ticket "same-commit OMDb span-key redaction"). Composition tests are never labelled as export delivery. Liveness/WAF from DEV-308 gets evidence reconciliation, and each box is ticked only on cited evidence. T020A stays open until its owner-answer text is found, never inferred from installed pins.
- Order (retrospective first, then OTel + redaction, health membership, decorator, conditional readiness, evidence/gates/delivery review) is sequencing taste, logged `[assumed]` in ASSUMPTIONS.md.

---

### Q5 complete exchange - Keel record

Keel proposed plan: additive completion of AddServiceDefaults for ASP.NET Core/HttpClient/Npgsql tracing, exact RabbitMQ publisher/subscriber sources, ASP.NET Core/HttpClient metrics with the existing LamuFlix identity, and OTLP logging alongside existing Serilog. Keep argument-free exporters, environment override, existing clock/options/idempotence/pins and no invented resource name. Same commit as HttpClient instrumentation includes T033A: real OMDb lookup with sentinel-key, actual outbound span and Debug-and-above HttpClient-category logs, sentinel absent from tags and rendered/structured/exception log content. Limit MetadataProviderProbe changes to category propagation and capture level. Add HealthCheckTags.Ready in Core/Pipeline, postgres check in Infrastructure, constant for RabbitMQ, metadata check registered but untagged. Decorator preserves ValidationException Unset plus outcome and unexpected Error plus FullName and rethrow. Retain T010 and active Q2 proof. Tests cover provider/exporter composition, instrumentation and removal sensitivity, default/shared/environment endpoint with nonparallel collection and finally restore, real health registrations and decorator outcomes. Never label composition delivery. Reconcile delivered liveness/WAF with evidence rather than reimplementing or blanket ticking. Conditional readiness waits on the Q1 ruling retaining owner Q2. Proposed order: historical retrospective; OTel with same-commit redaction; health membership; decorator; conditional readiness; evidence/gates and delivery review. No CPM/project edits; missing inherited owner-answer text is not inferred from installed pins. Question: accept this boundary/order or require recon for a prerequisite?

Patron answer: ACCEPT the plan boundary and order as proposed; no recon fact is missing. Readiness gets built only once the owner ticks Q2; until then T037-T041 stay BLOCKED and are not counted as delivered. No dependency, schema, layer or API-shape trigger (recon:141-146,148,164,219-221). T020A stays open until its owner-answer text is found. The phase order is logged [assumed] in ASSUMPTIONS.md.

Implication: existing installed packages are reused without asserting an unverified historical owner answer. T020A remains open; it is not a new project-reference change here.

## Q6 - Loop and close-out terms, grill closure

Verdict: **CONFIRM** Keel's terms as stated: Medium+ closing bar for both retrospective and delivery; at most 2 review/remediation rounds of at most 2 fix commits each; no axis waived; scope frozen to Q1-Q5; per-task-ID final evidence (delivered with receipts / BLOCKED / owner-deferred, with T020A and Q2 never ticked by inference); gate exits classified per current task-pipeline. Grill closed. The Q2 owner checkbox stays open and Gate 1 stays closed. The YouTrack comment on DEV-307 goes through Rigger only; no merge authorization.

---

### Q6 complete exchange - Keel record

Keel question: Confirm shared understanding and loop/close-out terms? Recommendation: Medium+ closes both retrospective and delivery, maximum two review/remediation rounds and two fix commits per round, with every Sentry/Ledger/Compass report required. Frozen scope is omitted DEV-307 observability/redaction/health membership/HandlerOutcome, conditional blocked readiness, existing-task evidence reconciliation, fixed DEV-308 retrospective and still-live Medium+ corrections, required gates and corrective records; anything else is a follow-up issue, not a finding in this round. Lower findings follow Q3, no unnecessary abstraction fix. Final evidence distinguishes each task as delivered with receipts, BLOCKED or explicitly owner-deferred; T020A and Q2 never ticked by inference. Retain composition and standalone NU1010 proof limits. Gates follow current task-pipeline: scope-empty SKIPPED is nonblocking but never PASS; mutation NOT APPLICABLE requires actual script verdict; disabled SKIP remains SKIP; property exit 2 requires recorded opt-out. Exit 1/Could not run is never green. Publish retrospective findings/adjudication and corrective PR summary, corrective PR #82/DEV-307 records through proper seats, and Keel merge-bar comment. No merge authorization. Next Quill L specify/plan/tasks with clarify/checklist; Keel read-only analyze, L ADR and three-axis plan challenge. Close grill with owner Q2 open and Gate 1 closed?

Patron answer: CONFIRM loop and close-out terms as stated. Grill closed; the Q2 owner checkbox stays open and Gate 1 stays closed; the DEV-307 YouTrack comment goes through Rigger only.

Implication: drafting is authorized; implementation, owner deferral, Gate 1 approval and merge authorization are not supplied by this grill.

## Q7 - L ADR rulings (number, reserved range, stale records, status, CONTEXT.md)

Asked by Conductor after Wisp recon `recon-DEV-396:340-451`. Rulings only; no ADR drafted, OD-1 and T020A untouched, Gate 1 closed. None needs a constitution departure or an owner answer, so no new checkbox.

### ADR-R1 - Number and filename: `docs/adr/0018-<slug>.md`, title `# 0018. <title>`

- Number: ADR-FORMAT.md:27 (highest + 1) gives 0018 on this tree (`recon-DEV-396:378,385`); 0018 is outside the 0001-0012 range the constitution reserves (`constitution.md:85-86,477-478`), as with the DEV-290 precedent (`specs/DEV-290/CONCLUSIONS.md:29-30`).
- Convention: the bare-slug form of ADR-FORMAT.md:3 and the five newest records (0013-0017, `recon-DEV-396:367-371`). Only ADR-0001..0010 use `ADR-NNNN.md`, and every number they hold sits inside the reserved range, so the bare-slug form is the convention for numbers past it.
- No renaming of existing records in DEV-396. The ticket does not name them and no AC or gate forces it (2.3 item 6); `specs/DEV-295/brief.md:241-242` left the mixed convention alone on purpose. Cite older records by their filed names (ADR-0004, ADR-0006, 0015). Noted, no ticket.

### ADR-R2 - Absent architecture plan and unfiled 0007/0008/0011/0012: do not reference, fill or reuse them

- The reservation is attested only by `constitution.md:85-86,477-479` and the DEV-290 ruling (`recon-DEV-396:384,446`). DEV-396 treats it as binding and does not try to verify it any further.
- The 0018 record does not create, describe, cite or depend on 0007, 0008, 0011, 0012 or the plan, and nobody writes their contents from the constitution's one-line mentions (`constitution.md:473,500,503`). Inventing them would mean recording decisions without their evidence.
- The unfiled reserved records and the missing README ADR links (`constitution.md:483`, `recon-DEV-396:394`) are documentation gaps, not broken behaviour and not Critical/High: noted, no ticket.

### ADR-R3 - 0018 amends; it supersedes nothing, and the earlier files are not edited

- The earlier decisions still stand: the ActivitySource placement in 0015, the propagator and spans in ADR-0004, and the category/raw-error split in ADR-0006. The only stale parts are their not-yet-wired follow-up sentences (`0015:53-57`, `ADR-0004:76,86-88`, `ADR-0006:15,24`; `recon-DEV-396:400-403,448`). ADR-FORMAT.md:21 reserves `superseded by` for a decision that is replaced. These decisions are being completed, so supersede is wrong.
- 0018 carries a short `Relationship to earlier records` section. It cites each of those lines and says which part of the 0018 decision completes it. Constitution:479 (`add or supersede an ADR`) is met by adding 0018.
- No edits to `0015`, `ADR-0004` or `ADR-0006`: they are tracked files the ticket does not name, and no AC or gate forces a change (2.3 item 6). ADRs are point-in-time records.
- Readiness guard: 0018 must not say `/health/ready` exists or that `ADR-0004:88` is resolved while Q2/OD-1 is unticked. It records health membership and tags as delivered and readiness routes as BLOCKED on the owner answer (CONCLUSIONS Q1, Q5).

### ADR-R4 - Status line: `Proposed (Gate 1 closed; OD-1 open; becomes Accepted in the DEV-396 delivery PR)`; the delivery PR changes it to `Accepted`

- Follows the 0003, 0014-0017 precedent of naming a flip condition (`recon-DEV-396:361,368-371`). Until Gate 1 opens and the plan is frozen, the decision is not accepted.
- `constitution.md:477` requires a merged record to read `Accepted`. Records 0014-0017 still read Proposed after their PRs merged (`recon-DEV-396:392`), and this record will not repeat that. Keel has Quill add one close-out line to tasks.md: change 0018 to `Accepted` (with date) in the delivery PR before review closes, and re-word any BLOCKED readiness sentence to match the owner's Q2 answer. This line falls under the L ADR step (task-pipeline:74) and adds nothing to what the ticket delivers, so 2.3a does not trigger. The stale `Proposed` text on 0014-0017 is noted, no ticket.

### ADR-R5 - CONTEXT.md: no term needed

- `CONTEXT.md` holds domain ubiquitous language (`constitution.md:480-481`). Words like span, exporter, instrumentation, health tag and `HandlerOutcome` are implementation vocabulary, already named in code and in 0015 and ADR-0004. `traceparent` is already defined (`CONTEXT.md:62-64`, `recon-DEV-396:415`). brief.md:103 and plan.md:136 record no CONTEXT.md change.
- If Keel's draft brings in a new domain term, Keel stops and asks Patron before adding it. Keel does not add it without asking.

Next: Keel drafts 0018 under ADR-R1..R5, and Quill adds the ADR-R4 close-out task line. Conductor then dispatches the Sentry/Ledger/Compass plan+ADR challenge, and Keel adjudicates and freezes. OD-1 unticked, T020A open, Gate 1 closed.

## Q8 - Post-adjudication decisions P1-P4 (S1, S2/S3/C1, S5/S6, S8)

Asked by Conductor after Keel adjudication `DEV-396:254-342` and Wisp recon `recon-DEV-396:452-710`, pinned HEAD b0713f8. Rulings only: no brief/spec/plan/tasks/ADR edit, OD-1 unticked, T020A open, Gate 1 closed, nothing executed. Facts recon marked UNKNOWN stay unverified here; each ruling names the receipt that closes it. No ruling changes ticket acceptance or departs from the constitution, so no owner checkbox (see Escalation check).

### P1 - OMDb log secret: default URI redaction, no opt-out, is the protection; T018 fails closed

Verdict: **SUFFICIENT, subject to T018 proving it on the pins.** No production logging change, no `SuppressDefaultLogging`, no `System.Net.Http.HttpClient.*` level filter, no `AddExtendedHttpClientLogging`.
- Basis: the IHttpClientFactory URI redaction (query replaced by `*`, user-info/fragment removed) is default since .NET 9 and disabled only by `System.Net.Http.DisableUriRedaction` / `DOTNET_SYSTEM_NET_HTTP_DISABLEURIREDACTION`, both present in the installed 10.0.12 assembly and absent from the repo (`recon-DEV-396:505-507`); headers log only at Trace (`:503`). Suppression or filtering empties both client categories and breaks SC-003/spec:44 (`:535-536`); the Extended-logging pair is a new dependency, 2.3 item 1, rejected under FR-017 (`:541`).
- `MetadataProviderServiceCollectionExtensions.cs` keeps health membership as its only edit (tasks:72); DEV-396:260's undecided logging remedy is decided as none.
- Unverified: the 10.0.12-tag call site (`:509`) and the `url.full` value on Instrumentation.Http 1.19.0 (`:513`). T018 is the verification. If it shows the sentinel, that is a Medium+ finding that comes back to Patron; it is not fixed by an opt-out, a suppression or a weaker assertion.

Exact T018 proof (`MetadataProviderProbe.cs` + `MetadataProviderTelemetryTests.cs`, same commit as T016):
1. Probe capture, three changes and no others: keep `categoryName` on every record (`:165`); `IsEnabled` at `Debug` and above (`:184`); `BeginScope` records the scope state and attaches the active scope payloads to each record (`:181-182`). Scope capture is required because the `HTTP {HttpMethod} {Uri}` scope carries the URI and an absence check that can't see it is vacuous (`:524`). This replaces the "category and level only" limit at plan:92 / tasks:60 / brief:43.
2. Positive client capture, before any absence check: at least 1 record whose category starts with `System.Net.Http.HttpClient.` and ends with `.LogicalHandler` or `.ClientHandler`. Read the category from the capture, never hardcode the typed-client name (`:490,495`). At least 1 of those records has structured key `Uri` whose value contains the stub server's host and path. That value contains neither `apikey` nor the sentinel, which is a positive redaction check. Do not assert the exact `?*` text.
3. Positive span: at least 1 stopped `ActivityKind.Client` activity from the HttpClient instrumentation for the stub host, plus the `LamuFlix` `Metadata.Lookup` activity.
4. Absence, over every captured activity and every captured record including resilience-handler records (`:548`): the sentinel and its `Uri.EscapeDataString` form must not appear in any tag key or value (`TagObjects` stringified), event, or status description. In each log record it must not appear in the category, rendered message, any structured state key or value, any scope payload, or the exception text.
5. No forced failure-path case: SC-003's positive bar is the span and the category entry, and exception text is covered wherever it is captured. The `sentinel-key` constant (`MetadataProviderProbe.cs:25`) is a test sentinel, not a credential literal.

### P2 - Production-composition proof: real hosts, stub OTLP receiver, recorded-activity observation, executed removal receipts

Verdict: **production composition only, no test-local recomposition, no new seam/API/csproj/InternalsVisibleTo.** Removal sensitivity is proved by per-row isolation plus executed, uncommitted removal receipts.
- Exporter presence (SC-001, 3 of 3): no public API lists a provider's exporters (`recon-DEV-396:573-575`), so the proof is behavioural. In IntegrationTests, build the real host with `OTEL_EXPORTER_OTLP_ENDPOINT` set to an already-referenced WireMock stub and `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf`. Emit one span, one metric and one `ILogger` record (through Serilog `writeToProviders`, S6), then call `ForceFlush` on each provider with an explicit timeout and assert it returned true. Assert the stub received a request on `/v1/traces`, `/v1/metrics` and `/v1/logs`. That single test proves that each provider carries an OTLP exporter, that the environment wins, and that a positive log reaches the composed provider (`:561,636-637`). The test name says `exporter is composed`. It is never called delivery and never asserts the stub's payload content (spec:110, ADR:29).
- Default endpoint and shared options (T020, UnitTests): resolve `OtlpExporterOptions` from the container that the production `AddServiceDefaults()` built (`ServiceDefaultsTests.cs:143-151` precedent), and assert the default endpoint plus the environment override. Whether that resolves is UNKNOWN (`:625`). If it does not resolve, Cog stops and reports to Patron. It may not drop the default-endpoint assertion or construct a fresh `OtlpExporterOptions`.
- Six sources reach a recording processor (SC-002, C1): IntegrationTests, on one production `WebApplicationFactory<Program>` host (`ApiHostFactory.cs:14`). The test-side `ActivityListener` has `ShouldListenTo` for the source and `Sample` returning `None`, so it never causes recording itself. A source passes only when a stopped activity from it has `Recorded == true`. Each source needs a real trigger: ASP.NET Core = an in-process request; HttpClient = an outbound request to a WireMock stub; Npgsql = a real query against `PostgresFixture`; `LamuFlix` = a real lookup emission site (`OmdbMetadataProvider.cs:47`); RabbitMQ publisher and subscriber = real `BasicPublishAsync`/`BasicGetAsync` against `RabbitMqFixture` (`RabbitMqProbe.cs:100,134`). That is emission, not just subscription (`:615-618`). This resolves S3: the UnitTests T019 theory rows move to IntegrationTests, and UnitTests keeps provider resolution and options. Read the exact source names (HttpClient: `:597`, ASP.NET Core) from the first execution and pin them as test constants.
- Removal sensitivity (spec:149, three instrumentations only, C1): (a) each row asserts only its own source, so removing one registration can fail only its own row; (b) each row runs a negative control first: before the production host is built, the same trigger yields no recorded activity from that source, so no other listener props up the check; (c) Anvil performs three local, uncommitted removals of `AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation` and `AddNpgsql` in `Extensions.cs` and records, for each removal, that exactly its own row fails and the other rows pass. Restoring the file afterwards needs `git diff --quiet -- src/LamuFlix.ServiceDefaults/Extensions.cs` exit 0, and the receipts go into the T019 evidence. A host that never calls `AddServiceDefaults` is only the negative control. It is not the removal proof (`:587-590`).
- Placement and boundary: add one new IntegrationTests test file plus one new collection definition. The collection has `DisableParallelization = true` and holds `ICollectionFixture<PostgresFixture>` and `ICollectionFixture<RabbitMqFixture>`. Quill adds both, with exact paths, to the plan File Boundary. These are test-only additions, not 2.3 items, and no csproj change is needed (`:563,611`). Npgsql needing `AddNpgsql()` is UNKNOWN (`:624`); the removal receipt settles it.

### P2a - `Sample = None` is a passive observer; the production OTel listener does the recording

Verdict: **P2 stands as intended. The test listener is an observer beside the production-composed `TracerProvider` listener and never causes creation or recording; the six recordings must come from the production listener. Wording is tightened, not changed.**
- Basis: the runtime folds `Sample` results by maximum, so `None` is not a veto, and an Activity is created only if some listener returns more than `None` (`recon-DEV-396:732-738`). The pinned `core-1.19.1` provider defaults to `ParentBased(AlwaysOn)` and returns `AllDataAndRecorded` unless `Sdk.SuppressInstrumentation` is set; no `OTEL_*` sampler is set in the repo (`:751-756`). With the production host present, the observer sees the provider-created activity. With it absent or with the registration removed, the aggregate is `None`: no activity and no callback, so the row fails. That is the removal sensitivity P2 needs (`:745,749`).
- Exact wording for T019, which Keel records in the brief and Quill applies: the observer `Sample` returns `None`, and `SampleUsingParentId` also returns `None` or is left unset. A row passes only when the observer's `ActivityStopped` sees an activity from that source with `IsAllDataRequested == true` and `Recorded == true`. Both are required because an unsampled fallback activity can exist with `Recorded == false` (`:764-769`). The negative control passes when there is no stopped activity from that source, or only ones failing that predicate. The test listener must never return `PropagationData`, `AllData` or `AllDataAndRecorded`. That would make it create the activities itself, which makes the proof vacuous.
- Unverified: this is a source-level reading, not a measurement (`:758,787`). The first T019 execution is the check. If any source shows zero qualifying activities on the production host, Cog/Anvil stops and reports to Patron. They do not raise the observer's sampling, add a sampler or `OTEL_TRACES_SAMPLER`, or drop the row. No ticket change and no constitution departure, so nothing escalates.

### P3 - No-collector policy and deterministic test isolation

Verdict: **production stays argument-free and silent. Tests flush, dispose and isolate deterministically, with a measured bound.**
- Production: when no collector is reachable, the exporters fail to EventSource only (`OpenTelemetry-Exporter-OpenTelemetryProtocol`, `OpenTelemetrySdkEventSource`), not `ILogger`/Serilog. The app keeps running and telemetry is dropped (`recon-DEV-396:643-647`). Quill records in the ADR Consequences that no collector means dropped telemetry and that an operator can opt in with `OTEL_DIAGNOSTICS.json` for diagnosis. No diagnostics file is committed or created by tests (`:645,662`). No exporter timeout, protocol, endpoint or processor arguments (plan:88, brief:41). Shutdown spends up to the documented 10 s per-exporter budget (`:634,655-656`). This is accepted and documented, but it is unmeasured.
- Tests: assertions on exporter output follow an explicit `ForceFlush(timeout)` that returns true. Never sleep or wait out a batch delay (`:653-654`). Every host and provider a telemetry test builds is disposed in `finally`. Environment mutation lives only in the non-parallel collections and is restored in `finally` (plan:23, tasks:62). Recorded-activity and negative-control checks run only in the non-parallel collection, so no other composed provider is alive (`:664`).
- Existing capture tests (`MetadataProviderLookupTests.cs:525-556`, `EnrichmentConsumerTests.cs:276-281`): if T016's process-wide provider changes what they observe, the fix is narrower correlation in the test (source and operation name, or trace id). Global telemetry is never disabled, and a test is never skipped.
- Bound: Anvil records the IntegrationTests suite duration before T016 and after T016, and records the slowest `ApiHostFactory` disposal. Disposal over 12 s, or suite growth over 25%, is a Medium finding that comes back to Patron. It is never silenced by test-host exporter overrides on Cog's own initiative. Unverified until measured: framework-level gRPC logs and the disposal cost (`:668-669`).

### P4 - Review cap accounting and exhaustion

Verdict: **two independent budgets of the same size, one for the DEV-308 historical retrospective (T012) and one for the delivery review (T042), each at most 2 review/remediation rounds of at most 2 fix commits per round. Neither budget can lend to the other.**
- Basis: the brief applies the closing bar to the retrospective and the delivery review separately, and gives each the same cap (brief:9; spec FR-013:130; CONCLUSIONS Q6:95). T012 says `2 rounds in total` for the retrospective and T042 says `within the 2-round, 2-fix-commit cap` for delivery (tasks:45,106). The current plan/ADR challenge-and-fix step is a spec-phase step and is not counted.
- A round is one three-axis review (Sentry, Ledger, Compass, all mandatory) of a pinned HEAD, plus its adjudication, plus remediation. A fix commit is a commit that changes files to close an accepted finding of that round, and a commit that addresses several findings counts once. These do not count: record-only commits (CONCLUSIONS/ASSUMPTIONS/receipts/notes), a rebase on main, and pre-review Gauge gate fixes from Phase 3. The closure check after a round's fixes is part of that round. It is scoped to the fixed finding IDs and done by the originating axis, and it is not a fresh review.
- Exhaustion is terminal: Medium+ still open after round 2's closure check, or a third fix commit needed within a round. Then stop: no further commits, no third round, no extra immutable review, no severity downgrade, no waiver, no merge-bar sign-off. Report once to Bernstein `blocked: review cap exhausted - DEV-396 <historical|delivery> <finding ids>`. The task stays not delivered. Only the owner can extend the cap; Patron cannot.

### Escalation check (for Conductor routing)

- None of P1-P4 contradicts DEV-396 ticket acceptance or departs from the constitution, so no 2.3 escalation and no new checkbox. OD-1 stays the only owner question.
- One fact is unverified and is for Conductor to check: whether the DEV-307/DEV-396 ticket text names `ServiceDefaultsTests.cs`/UnitTests as the home of the instrumentation theory (inherited T030). If it does, the P2 move to IntegrationTests contradicts a ticket line and goes to the owner as `blocked: structural`.

Next: Keel records P1-P4 in the brief, and Quill applies fix list items 1, 2, 3 and 5 (`DEV-396:329-333`) to match.

## Q9 - Inherited T020A receipt, N2 first-half approval and separate Kestrel owner decision - 2026-10-05

Verdict: **ACCEPT the historical approval receipt; AMEND the evidence states; REJECT OD-1 as Kestrel coverage.** This corrects Q5's missing-owner-answer condition and Q8's statement that OD-1 was the only owner question; the earlier exchanges remain historical records.

- **Approval:** the 2026-10-03 Patron ruling approves N2 first half under charter 2.3(6) only: the Api ProjectReference and versionless Microsoft.AspNetCore.Mvc.Testing PackageReference in tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj. Cited basis: recon-DEV-396-T020A-owner-answer:7-17 preserves the exact ruling and chronology, with inherited brief.md:103, D1:119 and plan.md:284; DEV-396:418-423 records its reconciliation and adjudication.
- **Prerequisites and delivery:** approval does not resolve inherited DEV-307 Q1, prove implementation or approve Kestrel. Inherited DEV-307/tasks.md:58 defines T020A with Q1 and Patron-approval prerequisites; T020A remains unticked pending Q1 evidence and its delivery receipts (DEV-396:423). The earlier no-answer record predates the ruling; the PR #79 N2 tick also predates it and is not its source (DEV-396:419; recon-DEV-396-T020A-owner-answer:27-29). No inference from installed references or package pins.
- **OD-1 boundary:** OD-1 covers the retained health body and FR-034 OpenAPI/TypeScript deferral only. It cannot answer inherited N2 second half, the default WebApplicationFactory versus test-composed real Kestrel host constitution departure (DEV-396:425; inherited DEV-307/brief.md:7,74,119; charter 2.3b).
- **Separate user checkbox, OD-2:** blocked: structural - Inherited DEV-307 N2 second half: approve a test-composed real Kestrel host in place of default WebApplicationFactory for the inherited host proof, as the constitution departure reserved in DEV-307/brief.md:7,74,119 under charter 2.3b, or retain default WebApplicationFactory? This is recorded as a distinct unchecked checkbox in brief.md, separate from OD-1.
- **Patron recommendation:** retain default WebApplicationFactory and perform no Kestrel work without explicit user approval (DEV-396:425). N2 first-half approval and Patron answers never close the user checkbox; the question authorizes no host change or added delivery scope.

Disposition: decision records only, as directed in DEV-396:426. spec.md, plan.md and tasks.md remain untouched pending Quill's bounded amendment of the evidence states identified in DEV-396:424. No freeze, checkbox completion, Gate 1/PR-state change, gates, tests or implementation proof.
