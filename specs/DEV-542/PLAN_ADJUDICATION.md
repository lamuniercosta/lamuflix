# DEV-542 plan challenge adjudication

**Status**: PLAN FROZEN (2026-11-19). All 28 findings are dispositioned and planning repairs were reread against brief, plan, tasks, spec, contract and Patron ruling. Gate 1, Build, and runtime acceptance remain pending.

**Evidence**: `findings-DEV-542-Sentry` (12), `findings-DEV-542-Ledger` (8), and `findings-DEV-542-Compass` (8) in the Maestri LamuFlix notes. These dispositions use `recon-DEV-542:44–97`, the private brief Q1–Q5, the spec and contract, and Patron’s Plan Challenge Ruling in `CONCLUSIONS.md`. Worktree `F:/Dev/LamuFlix.worktrees/feature-542-spec`, branch `feature/542-spec`.

## Dispositions

All 28 findings adjudicated. 23 accepted and 5 accepted in part. The table identifies the repair or cites the boundary for each declined portion.

| Finding | Disposition | Decision and evidence |
|---|---|---|
| Sentry H1 | Accepted | Q2 requires wire omission. Use a property-level conditional ignore and assert absence in parsed JSON (T002/T004). |
| Sentry H2 | Accepted in part | Patron requires verifying every For You seed, including nonblank title and supported signal (T001). Reject skip, invented evidence, and new status or partial-response policy since Q2/Q4 and ticket scope preserve seed guarantees and result membership. Route any observed runtime contract defect to Patron (CONCLUSIONS, Plan Challenge Ruling). |
| Sentry H3 | Accepted | Independent anchors and navigation for distinct IDs, with component and browser activation checks (T007–T010). Use event isolation as needed at the existing navigation boundary. |
| Sentry M1 | Accepted in part | Keep control-character removal, preserve names/order, test suffix arithmetic and use plain-text rendering (T002–T004/T007/T009). The ticket caps names on display. It authorizes neither a new character limit nor deduplication of matchedNames. |
| Sentry M2 | Accepted | T001 records a shared responsive anchor. T007/T011 exercise local desktop rules. T010 verifies both visible and accessible item counts at the two viewport modes. Select the mechanism against the verified shared anchor. |
| Sentry M3 | Accepted in part | Use upstream values and keep added presentation work on selected results, with no new candidate-loop or per-result seed/evidence reads (T001/T004). A new timing or large-library performance gate has no ticket baseline. |
| Sentry M4 | Accepted | T002–T010 forms one uncommitted migration through the web checks. T001/T006 record DEV-542 first and DEV-541 rebase/regeneration after it lands. |
| Sentry M5 | Accepted | Patron-approved shared pure Core DEV-518 formatter or feature-local seam on both paths, with one shared set of output invariants verified across both routes (T001–T004). |
| Sentry M6 | Accepted in part | Verify DEV-518 public criterion identities and map supported DEV-543 signals explicitly (T001/T004). Assert exact criterion sequences with known route fixtures and the closed generated signal values (T002/T006). A fallback criterion or new DEV-542 criterion taxonomy would extend the ruled contract. Existing API audience is retained. |
| Sentry L1 | Accepted in part | Keep the literal ticket source check in T013 and add response-level checks in T002 on both routes. Broader variant searches are outside the written acceptance criterion. |
| Sentry L2 | Accepted | Capture matched before/after evidence with library state, build/ref, cards, width and paths. Re-capture before evidence after pickup restart and resolve mismatches before publication (T001/T013/T016). |
| Sentry L3 | Accepted | Assert parsed property names including absence across casing variants and null values on nonempty results (T002), plus generated absence (T006) and a deterministic browser fixture (T008). |
| Ledger 1 (High) | Accepted | Internal API/schema checkpoint at T006. One atomic migration includes API, regen, both web consumers, fixtures and tests. Whole-web typecheck and check:api run at T010 before the commit. |
| Ledger 2 (High) | Accepted | Reuse a single pure Core DEV-518 formatter or its existing feature-local seam per Patron. T001 records its concrete name, path and test file before edits. T003 adds direct unit and property coverage. |
| Ledger 3 (Medium) | Accepted | Patron explicitly assigned domain vocabulary to DEV-543/DEV-518. DEV-542 uses spec-local presentation terms. Constitution VIII row cites the ruling and leaves root CONTEXT alone. |
| Ledger 4 (Medium) | Accepted | Retire the legacy builder path and unused mapping. Patron authorizes Reason.cs deletion only after zero remaining callers is proven (plan envelope/T004). |
| Ledger 5 (Medium) | Accepted | T002 owns behavior-level coverage on both API routes. T013 retains the ticket’s one-off source check. The existing no-explanatory-comments rule applies. |
| Ledger 6 (Medium) | Accepted | Tasks name existing assertion/mocking libraries by convention, C# test naming and AAA comment rules. T001 identifies the exact formatter and test file that T003 must use. |
| Ledger 7 (Low) | Accepted | Remove unsupported parallel markers from T007/T008 and show T004/T005 in the complete Phase 2 execution order. |
| Ledger 8 (Low) | Accepted | Carry the repository mutation/static initialization conventions into the Build tasks and label dotnet format a supplemental check. |
| Compass M1 | Accepted | The envelope explicitly assigns T001 bounded recon for the existing For You component test and styles plus shared MovieCard styles. T007/T009 consume recorded paths. |
| Compass M2 | Accepted | T007 owns local component assertions and T010 owns the SC-003 viewport clause and both-surface SC-004 viewport verification. The requirement and outcomes are unchanged. |
| Compass L1 | Accepted | An undefined optional seed means no seed line in the renderer, with regression coverage at T007. API successful responses still require a populated seed on every For You item. |
| Compass L2 | Accepted | T007/T009 pin verbatim plain-text evidence with no client-side format or fallback, consistent with Q3’s single server formatter. |
| Compass L3 | Accepted | T002 asserts the actual pending-enrichment Related response is an empty recommendation list at the existing success status. Empty evidence on populated responses is checked separately. |
| Compass L4 | Accepted | T006 checks the actual three-value signal enum in generated OpenAPI/TS. |
| Compass L5 | Accepted | T002 uses known inputs and compares exact expected criterion sequences on each route. |
| Compass L6 | Accepted | T003 asserts the complete Q3 example, including the single space immediately before +2. T007 uses the exact server-formatted fixture. |

## Closure criteria

- Verify the written repairs in the private brief, plan and tasks against each finding and the cited scope. The spec and contract hold the product requirements and Q1–Q5, with T010 explicitly assigned the shared viewport observation.
- The two-round review/remediation cap and all-severity closing bar remain in force. No structural escalation or owner checkbox resulted from this challenge.
- DEV-543/DEV-518 delivery, concrete path anchors, and the DEV-541 handoff require a Phase B receipt. T001 stops on missing required evidence before production edits.

## Verification receipt

- Independent on-disk reread confirmed 12 Sentry, 8 Ledger and 8 Compass dispositions, 17 unique task IDs, and references to all 13 functional requirements and 7 success criteria. T006 and T010 agree on validation timing, and T001 owns outstanding pickup evidence.
- Planning verification only. Gate 1, production changes, Build checks, runtime observations and delivery publication have no receipt in this step.
