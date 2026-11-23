# DEV-567 Plan Adjudication

**PLAN FROZEN** · 2026-11-23 · Phase 2 step 6

**Review baseline** · `feature/567-spec` at `c7d5fb025b3793680a02fdf5f1785553becf9343`, with the untracked Phase 2 planning artifacts in `specs/DEV-567/`.

**Gate 1** · Ready for ordinary owner ruling. T000 records the verified Spec PR merge before T001 or Build. No structural owner checkbox.

The three reports contain 24 findings. The dispositions below account for 11 accepted, 11 partially accepted, one already covered and one superseded. The challenge closed against the ticket and the cited Patron rulings in [CONCLUSIONS.md](CONCLUSIONS.md). Recon facts come from the `recon-DEV-567` note. The task assignments reflect the updated [brief](brief.md), [plan](plan.md) and [tasks](tasks.md).

## Sentry — Risk (11)

| Finding | Severity | Disposition and evidence |
| --- | --- | --- |
| H1 | High | **Partially accepted.** T005/T008 test concurrent cache failures, deterministic FIFO eviction, per-execution ownership, and timer cleanup. Distinct failures remain independently reportable under Q4, ticket lines 20 and 23–25, and the Q6 cap. Throttling or collapsing distinct failures would change that policy. |
| H2 | High | **Accepted.** T008–T010 require explicit transport provenance assigned at the scoped fetch-rejection boundary, known HTTP failure classification, and safe handling of unknown throws. Ordinary TypeErrors, non-Error values and malformed bodies have regression coverage. Q5 and recon lines 7 and 30 establish the classification boundary and the limits of wrapper evidence. |
| H3 | High | **Accepted.** T010 ignores React Query cancellation and scoped intentional aborts before reporting. T011/T012 check abort during flight and stale failure after newer success, and require current-request/cancellation checks before every raw publication. T005 checks focused eviction. Q4 and recon lines 4–11 anchor the two reporting boundaries. |
| M1 | Medium | **Accepted.** T008/T009/T011 preserve the known HTTP status through HTML, empty and malformed bodies, use fallback fields, and avoid body text or reporting exceptions. The known response supplies HTTP classification under Q1/Q5, including proxy failures in the ticket context. |
| M2 | Medium | **Partially accepted.** T009 uses only approved diagnostics as text, T006 wraps long values and T013 exercises long diagnostics at narrow width. Q1/Q5 and ticket line 20 require supplied reason and trace ID. Recon lines 28–29 identify their sources and the challenge supplies no example of a leaking reason. An allow-list or truncation would change the diagnostic display behavior. |
| M3 | Medium | **Partially accepted.** T008–T012 cover tolerant reporting and preserved caller/inline handling for malformed and unclassified inputs. A new React error boundary and injected host-render-fault case are outside the Q6/Q8 host scope. |
| M4 | Medium | **Accepted.** T001 records exact throwing lines and the response-to-cache diagnostic comparison for the same execution. T010 allows only a metadata/provenance edit at that boundary under Q5. T014 records route, retry and inline invariance. An unidentified required boundary goes to targeted recon. |
| M5 | Medium | **Partially accepted.** T011 proves repeated 429s remain independently reportable and inline quota remains available. With mismatched statuses, the toast follows the response and the panel retains its decoded-body behavior (Q4/Q5, recon lines 5 and 18). Throttling or suppressing 429s would change Q4. |
| L1 | Low | **Accepted.** T001 inventories existing error subscribers in touched paths. T008 proves a single report for one terminal execution alongside local error handling. The global cache and raw calls retain distinct ownership (Q4, recon lines 9–11). |
| L2 | Low | **Partially accepted.** T011 includes traceId-present and traceId-absent cases. T014 records schema and runtime invariance from registration and affected files, with focused .NET evidence as applicable. T013 retains Q7's one HTTP scenario. Vitest/RTL+MSW exercises rejected fetch through the configured caches and raw calls, plus 429 and cancellation. |
| L3 | Low | **Accepted.** T002 records export command, environment/configuration and v1 output. T004 verifies the optional string properties in JSON before TypeScript generation and proves zero drift on the full repeat cycle (Q3, recon lines 25–29). |

## Ledger — Standards (10)

| Finding | Severity | Disposition and evidence |
| --- | --- | --- |
| S-01 | High | **Accepted.** T009 requires a scoped transport discriminator and known HTTP diagnostics, using generated ProblemDetails. T010 retains the Q5 bounded adapter contingency with a concrete trigger. Q5 permits the discriminator without requiring a new general exception hierarchy. |
| S-02 | High | **Accepted.** The QueryClient factory and colocated test in the Q5/Q7 authorized API area exercise the production wiring without importing the entrypoint. T010 preserves retry policy. The existing QueryClient location is pinned in recon line 10. |
| S-03 | Medium | **Partially accepted.** T006/T007 specify an app-scoped queue, subscriber host and fresh state per test. Publish at the reporting boundaries, independent of render/effect replay. Q4 and T008 assign one report owner per execution. A shared reset hook or a second dedup policy would add state management beyond that ownership rule. |
| S-04 | Medium | **Partially accepted.** T009 keeps pure normalization separate from publication within one helper module, uses generated ProblemDetails and preserves the inline-only decoder (Q5, recon line 5). No separate module or property-test mandate is needed for this distinction. |
| S-05 | Medium | **Partially accepted.** T001 gathers the candidate and T002 verifies it against app-generated v1 or routes the bounded recon stop to Wisp through Bernstein. The exact command is Phase B evidence under Q3 and recon lines 25–27. Retain the brief's contract-first order so Gate 1 hands Build one sequence. |
| S-06 | Medium | **Partially accepted.** T003 scopes the transformer to exact ProblemDetails and leaves HttpValidationProblemDetails outside this change. Add focused positive/non-target coverage in the existing test project if mutation-eligible. T014 explicitly accounts for mutation according to the current harness and records any exclusion. Q2/Q3 authorize the schema-only change and Q7 requires applicable gates. No new project or fixed mutation outcome is assumed. |
| S-07 | Low | **Already covered.** Brief approach step 2 and spec Assumptions authorize the existing plan location. Compass worktree check 3 reports `web/src/ui/` exists, contrary to Ledger's inventory. No new top-level folder or §2.3 ruling is required. |
| S-08 | Low | **Accepted.** T006 calls for existing house surface, ink, accent and square-corner tokens in `index.css` and its reduced-motion treatment, under ticket lines 17–19 and Q6. Preserve the brief's no-glass/no-blur requirement and keep the styling in the authorized stylesheet. |
| S-09 | Low | **Partially accepted.** T012 captures the caught value, checks current execution and cancellation, then reports before the existing inline error update. Scope the check to the owned signal and intentional abort outcome where applicable. Recon lines 4–7 and Q4 support the order without a global exception-name rule. |
| S-10 | Low | **Partially accepted.** T014 uses the configured refactor threshold rather than a literal. T013 keeps the one-scenario Q7 ruling and uses explicit assertions for diagnostics, inline behavior, viewport fit, dismissal and focus. |

## Compass — Spec (3)

| Finding | Severity | Disposition and evidence |
| --- | --- | --- |
| 1 | Low | **Superseded by Ledger S-02.** The production factory and its test now live in `web/src/api/`, already authorized in the brief and spec Assumptions. |
| 2 | Low | **Accepted.** The brief now explicitly places actual narrow viewport fit in T013 Playwright and limits Vitest to styling intent and lifecycle, aligned with FR-009 and Q6/Q7. |
| 3 | Low | **Accepted.** T001 confirms the raw Response shape in the installed client. T009 prefers its status when available and uses a valid structured failure status where that is all the cache receives (FR-003, Q5). |

## Cross-artifact closeout

| Coverage | Tasks |
| --- | --- |
| FR-001, FR-002 | T008–T012 cover cache and raw ownership, cancellation, stale suppression, retry policy and inline preservation. |
| FR-003, FR-004 | T008–T012 cover classified HTTP diagnostics, transport provenance, malformed bodies and unknown throws. |
| FR-005, FR-006 | T005–T007 and T013 cover the host, FIFO, timers, live region, focus, theme and actual viewport fit. |
| FR-007, FR-008, FR-009 | T002–T004, T008–T014 cover the contract chain, one shared reporting path, acceptance suites and applicable gates. |
| SC-001–SC-005, SMOKE-001, SMOKE-002 | T005–T013 cover HTTP and network outcomes, lifecycle, preservation of inline UI and the single browser check. |
| SC-006, SC-007 | T002–T004 and T014 cover schema scope, regeneration, runtime invariance and configured verification. |

All 9 functional requirements, 7 success criteria and 2 smoke checks retain task coverage. T000–T015 remain in prerequisite order. Q1–Q8, the single Playwright scenario, the Size M closing bar and the two-round cap govern the frozen plan. The export and any necessary adapter confirmation are bounded Phase B evidence under T001/T002/T010. This receipt records planning disposition. Implementation and runtime evidence belong to Phase B.
