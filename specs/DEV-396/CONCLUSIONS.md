# DEV-396 - Patron rulings

## Q1 - Carrying readiness while Q2 is unanswered

Verdict: **ACCEPT** (Keel recommendation) - keep T037-T041 explicitly `BLOCKED until Q2 is answered`; brief.md carries the DEV-307 Q2 owner checkbox verbatim, deferral clause included, unticked; plan the unblocked observability, registration and `HandlerOutcome` decorator work separately. No readiness route/body code until the owner ticks Q2 or records an explicit deferral.

- Basis: ticket text `DEV-396:3` names "readiness blocked on Q2" - decided scope, so dropping or silently omitting readiness would change the ticket (2.3a). Recon `recon-DEV-396:174,221,301`: Q2 still open per `specs/DEV-307/CONCLUSIONS.md:25-36`.
- Authority under current 2.3: the body shape alone would be a Patron care item (item 4), but Q2 is bundled with the FR-034 OpenAPI deferral, a constitution departure (`constitution.md:359-360`, 2.3b), which only the owner answers; `specs/DEV-307/brief.md:78` makes the two one answer. task-pipeline:67's broader wording is superseded by the role's 2.3. Patron does not tick or split it.
- Retained text (copy exactly from `specs/DEV-307/brief.md:76`, the `Proposed checkbox` line, starting `blocked: structural - DEV-307 Q2: health routes return JSON {status, checks[name,status,durationMs]}` and ending `- a 2.3b departure from constitution.md:359-360 for this ticket only?`), plus the `brief.md:78` rule that ticking without the deferral clause is not an answer. The answer is recorded at the DEV-396 equivalent of DEV-307 T048. Cost accepted: Gate 1 stays owner-blocked; readiness is not counted as delivered.
