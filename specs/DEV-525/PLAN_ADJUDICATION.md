# DEV-525 Plan Challenge Adjudication

Round 1 · Size M · 2026-11-19 · feature/525-spec

Reviewed all three challenger notes against [brief.md](brief.md), [CONCLUSIONS.md](CONCLUSIONS.md) Q1–Q3, [spec.md](spec.md), the private plan and tasks, and facts in recon-DEV-525. The running-app cause remains open. The two-round cap and closure through Low apply.

## Risk · Sentry

| ID | Severity | Disposition and basis |
|---|---|---|
| R1 | High | **Accept.** Recon 29 identifies the missing fixture. T002 maps the fixture fields required by assertions to the provider contract consumed by the app. Keep the fixture minimal and document unknowns. T004 ties that mapping, the diagnosed trigger and the failing assertion together. Stop if fidelity or same-trigger failure cannot be shown. T007 uses the same fixture and scenario. Q2 and FR-006/007 govern. |
| R2 | High | **Accept within the affected boundary.** T006 checks relevant existing safeguards for changed remote content handling: destination containment for writes, bounded download size, and safe text rendering for captions. Record which boundaries are untouched, including for a search-only fix. Q1 and FR-010 bound the edit. Recon establishes no vulnerability. Standalone hardening is follow-up scope. |
| R3 | Medium | **Accept.** FR-001/008 and recon 19 support a pre-publication check in T009. Remove keys, tokens, connection strings, settings dumps and sensitive hosts from the receipts. Crop or blur screenshot content as needed and check all three artifacts before attaching. |
| R4 | Medium | **Accept conditionally.** Recon 10 names retry and the asynchronous caller. T001 records whether overlap or retry is implicated, ruled out, or undetermined. Include the diagnosed overlapping action in T003 when implicated and carry the disposition into T004/T007. Q2 requires one faithful scenario. |
| R5 | Medium | **Accept within the existing error contract.** T001 records the observed UI and protocol error surface. T008 verifies search/apply and that the observed failure clears. A fixture pass with an app failure reopens diagnosis and blocks delivery. T011 records existing validation conflict evidence under R6. FR-003 and the spec Edge Cases govern. New timeouts, policies and statuses are outside Q1. |
| R6 | Low | **Accept.** T005 identifies existing evidence for missing and invalid acquisition configuration and request-time conflicts. T011 records the named checks and their results and whether the behavior was touched. Route a coverage gap for decision before claiming preservation. No new suite is authorized. |
| R7 | Low | **Accept with topology correction.** T010 is hosted CI per recon 28. T008/T011 isolate local settings, fixtures and shared stack resources, or serialize local runs that share them. Identify each run, revision and timestamp in its evidence. |
| R8 | Low | **Accept.** T011 uses the contract tests from recon 11, compares API/OpenAPI for all three DEV-501 operations, and checks TypeScript using temporary generation or an equivalent generator comparison. Record evidence even for a clean tracked diff. Build confirms the available commands. FR-013 governs committed regeneration. |

## Standards · Ledger

| ID | Severity | Disposition and basis |
|---|---|---|
| S-01 | Medium | **Accept.** T002 extends the existing stub listener without adding a service or port. Verify an existing provider endpoint override via Options and stop for Patron if unavailable. Recon 29 establishes missing harness settings only. Q2 authorizes limited harness edits. |
| S-02 | Medium | **Accept.** T007 runs the T005-extended suites where applicable and requires green results with assertions intact. |
| S-03 | Low | **Accept.** T002 requires a runtime-generated dummy credential if needed. T009 handles evidence redaction under R3. Q2 permits ephemeral test-only configuration. |
| S-04 | Low | **Accept.** The plan carries the AGENTS.md comments rule and its three exemptions into E2E code. |
| S-05 | Low | **Accept.** T005 carries the C# test naming, assertion/mocking library, AAA and disk-bound test conventions from AGENTS.md. |
| S-06 | Low | **Accept.** T011 verifies that normal web typecheck covers the new E2E spec or runs the applicable existing E2E TypeScript project check. Record the result. |
| S-07 | Low | **Retain with rationale.** Keep dev525-subtitle-search.e2e.ts for ticket-specific regression traceability. Recon 27 supplies the DEV-454 precedent. Discovery is independent of the prefix. |
| S-08 | Low | **Retain with rationale.** FR-012/013 and SC-007 trace Q3's approved review, gate and contract obligations into acceptance. Private-note references supply provenance. Public-spec editorial cleanup is deferred. |
| S-09 | Low | **Accept.** Use Drafting and Build in the plan. Keep the local Phase 0–6 dispatch headings distinct from canonical pipeline stages. |
| S-10 | Low | **Accept in part.** Keep the gate, evidence and handoff tasks to make ownership and dependencies executable under Q2/Q3. Use plan Test Strategy for the gate inventory and reference it in T011/T013. |
| S-11 | Informational | **Confirmed aligned.** Q1, FR-010 and recon 10 support the existing slice and generated client. No new abstraction or hand-rolled web contract is proposed. |

## Spec · Compass

| ID | Severity | Disposition and basis |
|---|---|---|
| M1 | Medium | **Accept.** The screenshot, both regression receipts and hosted E2E evidence belong to the stage-10 implementation PR and its head. T009 prepares the bundle before opening if possible. Stage 10 opens after local gates and pre-PR review dispositions. T009 publishes and T010 records hosted evidence after opening. T012 requires publication to close and T013 requires all receipts. This preserves Q2/Q3 without a PR/CI dependency cycle. |
| L1 | Low | **Accept.** T001 records a reproduced configuration- or data-only cause and stops for a decision on the correction and regression proof before the fixture or code-fix sequence proceeds. Q1 authorizes a diagnosis-led fix. |
| L2 | Low | **Accept.** Static analysis covers any changed C#, including tests. Record SKIPPED (scope-empty) when there are no changed C# files. Mutation scope stays production-only. |
| L3 | Low | **Accept with file bounds.** T005 may edit another existing suite only when directly affected and inside the authorized envelope. Recon 11 names contract suites as verification references. Out-of-envelope edits still require a cited Patron ruling, even when an API shape ruling exists. |
| I1 | Informational | **Clarified.** Recon SHAs are historical inspection points. The plan input removes the ambiguous reviewed-revision reference. T001 records its actual running-app baseline. |

## Closure

**PLAN FROZEN.** Round 1 closes with all 24 dispositions recorded and the applicable changes verified in the brief, plan and tasks. Gate 1 remains pending. Q1–Q3 continue to control product scope and Gate 1. The provider seam, diagnosed trigger, fix-file selection, runtime acceptance and hosted evidence remain Build or delivery checkpoints.
