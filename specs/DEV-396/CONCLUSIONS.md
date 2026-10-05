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
