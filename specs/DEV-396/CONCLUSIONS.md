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
