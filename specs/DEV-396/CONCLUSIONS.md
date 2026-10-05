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
