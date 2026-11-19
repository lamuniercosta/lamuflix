# DEV-541 plan challenge adjudication

Phase 2 step 6 · 2026-11-19 · `feature/541-spec`

Reviewed the plan, tasks, brief, and Q1–Q10 against the Sentry (Risk and Security), Ledger (Standards), and Compass (Spec) reports in LamuFlix notes. The reports contain 11, 6, and 4 findings respectively. All 21 have dispositions below. Nineteen are accepted as written and two are accepted with qualification. Shared repairs are recorded against every finding they close.

## Sentry

| Finding | Disposition | Basis and repair |
|---|---|---|
| F1 · Medium | Accepted | Sentry lines 8–15 and Compass lines 29–34 establish concrete callers. Recon line 5 establishes an inventory gap. Q6 authorizes compatibility edits. T001 confirms the known baseline plus pickup changes. T014 names the hook, page and tests and preserves the page's array input. |
| F2 · Medium | Accepted | Q1/Q5 establish that the selected ranking can contain at most the post-cut candidate count. T009 returns an empty page without invoking MMR when offset reaches that count, otherwise bounds the widened one-extra selection horizon to that count. T007 tests the short-circuit and T008 exercises a huge offset over HTTP. T015 records both. |
| F3 · Medium | Accepted | Q4 defines repeated keys and Q6 defines contract propagation. T011 pins OpenAPI query form style with explode enabled and integer items. T012 checks the generated input types and T014 checks actual generated-client serialization of two IDs and T015 verifies that request against a running API with known eligible IDs. |
| F4 · Medium | Accepted | Q6 limits web changes to compatibility. T014 keeps the take-only array-returning hook and existing query key, unwraps `data.items` in its query function, and checks that the page still renders. DEV-517 owns pagination UI. |
| F5 · Low–Medium | Accepted | Sentry lines 32–35 and Compass lines 19–25 cite generator ownership. T012 regenerates handlers. T013 changes the success fixture using the generated page schema. |
| F6 · Low | Accepted | Q4 and the existing ParseTake count check support HTTP 400 for repeated offset. T010 pins the check and T008 covers it. |
| F7 · Low | Accepted | Q4 plus the cited ParseTake and MovieId signatures establish the parse order. T010 uses invariant `NumberStyles.Integer` parsing before positive MovieId validation. T008 covers malformed, empty, overflow and nonpositive values. |
| F8 · Low | Accepted | Constitution VII and Q2/Q4 support validated options. T005 sets a minimum of one for both ranking caps and zero for the raw exclusion limit. T002 verifies those boundaries and existing startup validation. |
| F9 · Low | Accepted | Q5 permits computing the needed prefix. T007 compares the bounded run to the same fixture's full capped ranking, including caps and tie-breaking. T015 records the result. |
| F10 · Low | Accepted | Q4 follows the existing take error pattern. T010 uses static messages for new 400s. T008 verifies status, ProblemDetails with traceId, and absence of a distinctive malformed input value in the response. |
| F11 · Info | Accepted | T011 requires review of the snapshot diff for the expected For-You contract and preservation of the movie-specific contract before accepting the refreshed baseline. |

## Ledger

| Finding | Disposition | Basis and repair |
|---|---|---|
| F1 · Medium | Accepted with qualification | Recon line 5 was misrepresented. Sentry F1 and Compass F3 supply the affirmative inventory that recon lacked, so T001 must confirm that baseline and record deltas at pickup. The proposed blanket no-consumer outcome is removed. |
| F2 · Medium | Accepted | AGENTS.md's static-initialization and mutation rules apply. The plan and T005 require constant defaults and avoid mutation-sensitive static initialization. T002/T003 assign tests to cap and limit boundaries. |
| F3 · Low | Accepted with qualification | Q5 explicitly permits the prefix shortcut. The full capped ranking still defines correctness. T009 bounds the widened prefix and T007 proves equivalence to the full run. This preserves the Q5 decision and resolves the ambiguity. |
| F4 · Low | Accepted | Q4 fixes the request semantics. The plan and T009/T010 assign transport cardinality, parsing, and raw count to the endpoint and normalized query rules to the validator. T010 places the response DTO in its own file in the existing endpoint folder. |
| F5 · Low | Accepted | The plan points to T016/T017 and the canonical pipeline. Tasks retain the readiness, classification, review ownership and independent smoke criteria. |
| F6 · Low | Accepted | Removed the research-phase template instruction and the machine-local mirror reference from the worktree plan. The worktree copy is the spec PR source. |

## Compass

| Finding | Disposition | Basis and repair |
|---|---|---|
| F1 · Medium | Accepted | Compass lines 8–11 establish distinct HTTP 400 and FluentValidation 422 mappings. Q4 requires the endpoint to reject malformed, repeated and negative offset before dispatch. T009 retains the normalized nonnegative invariant. T010 and T008 pin the 400 path and coverage. |
| F2 · Medium | Accepted | Generator evidence at Compass lines 19–21 establishes ownership of `handlers.ts`. T012 lists it as generated output. T013 owns the fixture edit. |
| F3 · Medium–Low | Accepted | Compass lines 29–34 establish the hook, consuming page tests and MSW contract test. T001 and T014 use these paths and record the exact pickup delta under Q6/Q7. |
| F4 · Low | Accepted | The result type is required by Q1/Q6 and Compass line 42 identifies the existing Core seam. T009 names `ForYouRecommendationsPage.cs` in the existing Core recommendations folder and updates the query, handler and existing registration contract together. |

**PLAN FROZEN.** No finding requires a ticket scope change or constitution departure. Gate 1 remains pending the owner spec PR merge. Build, configured verification, implementation review, SMOKE-001 runtime acceptance, and the final PR merge bar remain pending.
