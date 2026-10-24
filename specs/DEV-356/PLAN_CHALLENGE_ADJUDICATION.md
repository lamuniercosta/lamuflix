# DEV-356 plan challenge adjudication — round 1

Date: 2026-10-24. Seat: Keel. Decision authority: Patron response ratifying the proposed dispositions, with the Sentry F8 refinement, appended in CONCLUSIONS.md and incorporated into brief.md before drafting corrections.

## Identity and evidence boundary

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-356-spec`; branch: `feature/356-spec`.
- Verified HEAD: `7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947`.
- Reviewed working bytes, not HEAD-pinned artifacts: `specs/DEV-356/` is untracked. Line citations below refer to the pre-correction working draft reviewed at this HEAD.
- Read task note DEV-356, recon-DEV-356, brief.md, CONCLUSIONS.md, spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md, and all three named challenge notes. No independent code searches, measurements, external API assumptions, tests, or gates.
- All axes present: Sentry Risk (9 findings), Ledger Standards (0), Compass Spec (5). Ledger zero findings is one axis report, not clearance of independent axes.
- Factual repository claims use recon-DEV-356. DEV-415:41–44 is a Patron-supplied precedent, not independently verified recon for this task.

## Every reported finding

### Sentry F1 — High — ACCEPTED, blocking correction pending

Evidence: tasks.md:59 permits a temporary compile seam; tasks.md:72 and spec.md:13,91 require the real reconciled DEV-416 surface without invented seams. This is a direct conflict with brief Q2/Q4, regardless of whether a stub could make tests green. Remove the permission; T009/T010 must exercise the real invocation from TB002. Phase 3 adds independently expected nonempty baseline evidence; it must not be the first replacement of a fake ranking path. No new port or production change is authorized.

### Sentry F2 — High — REJECTED as reported; implementation clarification retained

Evidence: spec.md:87–92, tasks.md:24,67,110 already require UTC inputs, stable traversal, fixed seed/time, deterministic JSON and byte equality; Q2 explicitly defers absent predecessor mapping. The report identifies unspecified implementation choices, but supplies no recon-backed failure showing that a deterministic implementation cannot satisfy these constraints. Byte identity is required for repeated equivalent runs; the brief does not promise byte equivalence across arbitrary serializers or runtime environments. Do not claim that every whitespace/ID representation is already pinned. Record concrete serializer options, UTC parsing, ordinal stable-ID ordering, and seed/TimeProvider mapping in Phase B TB002 reconciliation. Patron ratifies this treatment under Q2/Q4/Q6–Q8; no fabricated predecessor seam.

### Sentry F3 — Medium — PARTIALLY ACCEPTED, blocking correction pending

Evidence: spec.md:65,86 and tasks.md:104,110,114 require nonzero failure without publishing partial success, but the tasks do not explicitly test interrupted ordinary output writes. T014/T015 must prove ordinary write-failure integrity, with no truncated output presented as success. Retain Medium for that contract-proof gap. A specific temp/rename design and additional crash-recovery/concurrent-writer delivery are not established requirements; Patron does not add them. Implementation chooses an appropriate mechanism within the authorized files.

### Sentry F4 — Medium — PARTIALLY ACCEPTED, blocking correction pending

Evidence: spec.md:86 and tasks.md:110 require failure propagation but do not explicitly distinguish child nonzero exits and PowerShell terminating/nonterminating errors. Add concrete command-path failure tests and propagation behavior to T014/T015. Retain Medium for the proof gap. No specific preference variable, catch syntax, or implementation template is mandated. Basis: Q3/Q7 and Patron ratification.

### Sentry F5 — Medium — REJECTED, unsupported failure scenario

Evidence: brief Q3/Q8/Q9 deliberately approve the executable project and required UnitTests reference; recon-DEV-356:23–29 describes the conventions but reports no incompatible-reference build result. An executable project reference is not evidence by itself of conflicting entry points. No failed build/reference diagnostic is supplied. Do not add a shared library or duplicate invariants as speculative remediation. Validate the approved shape during Build; if evidence exposes incompatibility, stop for recon and a cited ruling. This adjudication does not assert that compatibility has been experimentally proved.

### Sentry F6 — Medium — REJECTED, unsupported enforcement gap

Evidence: the report cites a production-project reference table, but recon-DEV-356:24,29–30 does not establish that ArchitectureTests enumerate every test project, reject this project, or require a new exemption. The cited table alone does not prove a failure for an evaluation test project. No ArchitectureTests update/exemption or file-boundary expansion is authorized. Build remains subject to actual applicable gates; contrary evidence returns for recon/ruling. Basis: Q3/Q9/Q10 and Patron ratification.

### Sentry F7 — Medium — REJECTED as a new enforcement/timeout requirement; receipt clarification retained

Evidence: plan.md:11–19 and tasks.md:19–29 already block code until named-owner evidence and Q2 reconciliation exist. Q2 deliberately permits Phase A while implementation waits; an indefinite dependency wait is not permission for a replacement path, re-scope branch, or new enforcement script. No automatic STOP mechanism or timeout was promised. TB003 must cite Wisp recon for a fuller-text reconciliation or no-fuller-text receipt, rather than unsupported self-certification. Existing full-ticket and Q2 obligations remain mandatory. Basis: recon-DEV-356:15,18–20,39; Q2/Q9/Q10; Patron ratification.

### Sentry F8 — Medium — REJECTED by cited Patron interpretation

Evidence: recon-DEV-356:33 flags PRODUCT §2 Epic 9 sequencing; spec.md:79 and plan.md:41 already preserve it. Patron rules that PRODUCT §2:17 is an epic-level deferral and does not create an express Epic 7 blocking prerequisite for DEV-356, citing DEV-415:41–44. Retain the sequencing note. Do not infer Epic 7 completion, waive the note, assert a sequence violation, add an unscheduled Epic 7 preflight blocker, or create an owner checkbox. This is an interpretation of the existing authority, not a new factual claim about completion.

### Sentry F9 — Low — ACCEPTED, nonblocking correction pending

Evidence: spec.md:90 and tasks.md:114 require ignored/untracked generated output. The conditional narrow .gitignore edit is valid because coverage may already exist; no recon establishes otherwise. Make the ordering explicit: check coverage and add the narrow authorized entry if needed before first output generation. Do not demand an unconditional duplicate ignore entry. Basis: Q6/Q9 and Patron ratification.

### Ledger — zero findings — RECORDED

Standards axis supplied all required reporting and found no defects. No finding to remediate. Its statement that no fake seam is introduced does not override the explicit Sentry F1 evidence at tasks.md:59. Its sequencing conclusion likewise does not dispose of Compass F1.

### Compass F1 — Medium — ACCEPTED, blocking correction pending

Evidence: spec.md:42,49 requires a published-summary permutation proof in US2; tasks.md:61,68 place it in Phase 2, before Phase 4 publication exists at T015. T014 repeated identical-fixture runs do not by themselves explicitly assign the permuted-fixture published proof. Phase 2 must compare in-memory metrics/model equality under full-fixture permutation; Phase 4 must explicitly compare published bytes for equivalent original/permuted fixtures after serializer/publication exist. This preserves Q8/SC-002 and avoids the earlier I1 dependency class. Update US2/story dependencies and checkpoints accordingly.

### Compass F2 — Low — ACCEPTED, interpretation recorded; drafting alignment pending

Evidence: brief Q8 step 4 and spec.md:92 group byte equality with real-ranker integration; tasks.md:81–96 correctly defer publication to Phase 4. The brief append records Patron interpretation now: integration supplies the baseline and independently expected metrics; byte-identical command-output proof on that baseline is Phase 4 evidence. Align plan/spec wording without deleting the original ruling or claiming freeze. Basis: Q8 and prior I1 correction.

### Compass F3 — Low — ACCEPTED, nonblocking correction pending

Evidence: tasks.md:43–48,161 put compilable test authoring ahead of project scaffolding. Explicitly order T003 scaffolding before or alongside T001/T002, after ALL Phase B entry checks; behavior still follows tests at T004/T005. Preserve task IDs where practical and align dependency/parallel notes. A missing-project compile error is not a behavioral red test. Basis: Q3/Q8 and Patron ratification.

### Compass F4 — Low — ACCEPTED, nonblocking correction pending

Evidence: plan.md:45–61 omits Principle VIII; Q10 rules no glossary rewrite and the spec names evaluation-internal concepts. Patron ratifies the explicit check that these introduce no new bounded-context domain term. Add this explanation to the constitutional check; no CONTEXT.md edit is authorized. A future contrary fact returns for recon/ruling rather than silently overriding the constitution or approved boundary. This adjudication does not claim an independent glossary survey.

### Compass F5 — Low — ACCEPTED, nonblocking correction pending

Evidence: brief Q3 and tasks.md:102,114 require intentional script-driven evaluation but name no invocation mode for the approved executable. Record an evidence-backed explicit evaluation invocation mode during Phase B TB002 reconciliation, before T001, and implement it at T015. Keep executable/reference decisions within Q3/Q8/Q9; no custom entry-point API or library addition is invented now. Actual framework/API details require supplied recon; this ruling does not adopt the report technical claim as independently verified fact.

## Single numbered Quill correction list

Drafting corrections only, authorized files: spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md. Do not edit brief.md or CONCLUSIONS.md; the interpretations have been recorded there first. No production edits, gates, new dependency, expanded layer, or commit requested.

1. Remove Phase 2 stub/seam permission (Sentry F1). Require real TB002-reconciled invocation at T009/T010; Phase 3 owns the independently expected nonempty baseline, not replacement of a fake ranking path. Align phase descriptions and dependencies.
2. Split permutation proof coherently (Compass F1): Phase 2 in-memory metrics/model equality; Phase 4 explicit equivalent original/permuted-fixture published-byte comparison. Retain repeated identical-fixture command proof too. Align spec US2/FR-008, plan proof order, T008/T014/T016 and story/checkpoint mappings. State the Patron-ratified Q8 byte-proof interpretation (Compass F2); no premature publication requirement.
3. In TB002 and plan preflight, require the concrete deterministic serializer/UTC/ordinal-ID/seed/TimeProvider choices and an evidence-backed explicit evaluation invocation mode before code. T015 implements that mode; never publish incidentally through ordinary test execution (Sentry F2 clarification, Compass F5). Do not invent DEV-416 details now. Make TB003 cite Wisp recon for fuller-ticket reconciliation or the no-fuller-text receipt (Sentry F7 clarification).
4. T014/T015 explicitly cover ordinary write-failure integrity and native child nonzero exit plus terminating/nonterminating script-error propagation (Sentry F3/F4). Nonzero failure must not present truncated/partial output as a successful summary. Do not add concurrency/crash-recovery scope or prescribe syntax/mechanism.
5. Explicitly scaffold T003 before or alongside compilable T001/T002 authoring, after ALL entry checks, then implement behavior after tests; update dependencies/parallel notes (Compass F3).
6. Add the Principle VIII check explaining evaluation-internal vocabulary and no new bounded-context domain term under Q10; contrary later facts stop for recon/ruling, without authorizing a glossary edit (Compass F4).
7. Require ignore-coverage verification and the conditional narrow entry before first generated output (Sentry F9). Preserve Q9 conditional permission.
8. Update DRAFTING_RECEIPT.md with this challenge correction round and changed proof ownership. Preserve Q2 conjunctive STOP, full-ticket reconciliation, PRODUCT sequencing note, zero new owner checkbox, closing bar, round cap, and gate verdict distinctions. Add no Epic 7 preflight blocker, shared-library workaround, ArchitectureTests exemption, automatic STOP script, or unapproved file edit.

## Disposition totals and remaining steps

14 reported findings: 7 accepted (Sentry F1/F9 and Compass F1–F5), 2 partially accepted (Sentry F3/F4), 5 rejected (Sentry F2/F5/F6/F7/F8). Rejected findings with explicit drafting clarifications remain listed above; rejection is not a claim that an implementation was tested.

Four retained above-bar findings remain OPEN pending corrections and verification: Sentry F1 High; Sentry F3 Medium; Sentry F4 Medium; Compass F1 Medium. Five accepted Low findings are recorded and nonblocking. No finding is called remediated by issuing instructions.

Next: Bernstein routes this one list to Quill. After Quill receipt, Keel re-runs read-only speckit-analyze and verifies every retained correction against the brief and all-axis evidence; any required further challenge stays within the agreed two-round cap. Further recon is required only if a concrete unresolved fact is needed; do not independently investigate code.

Separate claims:

- Analyze: prior round 2 CLEAN is historical for the pre-correction draft (DEV-356:32–33); no fresh analysis performed here. Corrected bytes must be re-analyzed.
- Plan challenge adjudication: complete for all three supplied reports; correction verification and challenge closure pending.
- Plan freeze: NOT claimed or performed.
- Gate 1: NOT claimed or established; separate spec-PR/owner boundary remains.
- Phase B implementation: NOT authorized; Q2 conjunctive STOP and other entry checks remain mandatory.

Only documentation decision/adjudication artifacts were written. No tests, gates, code changes, commits, tracker mutations, or merges.
