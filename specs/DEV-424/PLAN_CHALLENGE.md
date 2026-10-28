# DEV-424 Phase 2 step 6 plan challenge adjudication

Status: PLAN FROZEN — both correction rounds verified on 2026-10-28; Gate 1 closed.

Authority: all three complete Maestri reports findings-DEV-424-Sentry (Risk), findings-DEV-424-Ledger (Standards), findings-DEV-424-Compass (Spec); recon-DEV-424 and recon-DEV-424-2; brief.md Q1-Q8/C1/step 6/F7 amendments; CONCLUSIONS.md Patron decisions at 7a335ac3 and 10ffc9c1. Scope: F:/Dev/LamuFlix.worktrees/feature-424-spec, feature/424-spec, specs/DEV-424 only. No independent source reconnaissance, implementation, tests, or gates by Keel.

Seventeen findings (six Medium, eleven Low) plus one informational status item; every source item is disposed below. Duplicate mechanisms are consolidated without dropping either source finding. Hard cap: two correction rounds, no third round or implicit waiver.

| Axis / ID | Severity | Disposition and evidence |
|---|---|---|
| Sentry F1 | Medium | Accepted, consolidated with Ledger F7. Patron step 6 ruling 1: natural int PK ValueGeneratedNever; whole-save rollback/reset/re-read/retry; forced same-new-id two-movie oracle. plan Approach 2; T005/T007/T009/T010/T011. Existing retry is not claimed sufficient. |
| Sentry F2 | Medium | Ruled residual accepted, explicit fallback-clear/failure-preserve tests. Membership may remain cleared until successful TMDb restores it. Patron ruling 2; spec US1/FR-003/SC-002; T010/T011. No release-note addition. |
| Sentry F3 | Medium | Accepted, consolidated with Ledger F2. Raw string request, key constant, single-value parse/repeated detection; invalid empty/abc/0/-3/repeated 422; nullable-positive validator and rejecting codecs. Patron ruling 4; T015/T017/T019. |
| Sentry F4 | Low | Accepted bounded test guidance. Plain-text hostile name and overlong layout cases; T021/T022/T025, FR-008. No new name-normalization storage contract. |
| Sentry F5 | Low | Noted, no fix. Retained orphans are accepted Q2 scope; no telemetry/janitor task or ticket created here. Future operational need may be considered separately by Patron. |
| Sentry F6 | Low | Accepted migration oracle. Natural PK plus movie CollectionId index; T007/T009. |
| Sentry F7 | Low | Accepted deliberate mapping decision. Patron F7 at 10ffc9c1; recon part 2 lines 11-20 distinguishes optional omission from response rejection. Present malformed object fails whole TMDb response; absent/null valid; final failure preserves; successful fallback clears. Round 2 propagation verified in spec US1 scenarios 6-7/FR-001, plan Approach 1, T003/T004 and drafting receipt closure. |
| Ledger F1 | Medium | Accepted Patron ruling 3. Existing fieldset/legend, label.lamu-chip, sr-only controlled checkbox; select/replace/uncheck clears; no radio/pressed-button vocabulary or new skeleton/aria-busy. design.md intent/states/a11y; FR-008; T021/T025. |
| Ledger F2 | Medium | Accepted, consolidated with Sentry F3. T017 includes raw binding/parser/validator, existing suppression pair; T028 LibraryEndpoints complexity watchlist. |
| Ledger F3 | Medium | Accepted clarification. Backend tests alongside and green before T020; web red-first retained; query/codec/repository survivor suites named. Patron ruling 6; plan Test Strategy/Ordering; tasks header/T011/T019. |
| Ledger F4 | Low | Accepted status refresh. Design exists; final challenge/freeze/Gate 1 remain distinct. spec/plan/tasks/checklist/receipt status text. |
| Ledger F5 | Low | Accepted Patron taste ruling. Legend-only empty state, no helper copy; ASSUMPTIONS.md amended; design states/a11y and FR-008/T025 aligned. |
| Ledger F6 | Low | Partially accepted. No empty query validator; existing AddHandler chain, pass-through confirmed recon part 2 lines 39-45. Long MovieCount assertion rejected by recon genre facet and Compass; Patron ruling 5 fixes both records to int Id/string Name/int MovieCount. T012-T014. |
| Ledger F7 | Low | Accepted duplicate of Sentry F1; same mechanism and oracle, no extra abstraction/raw-SQL requirement. |
| Compass Low 1 | Low | Accepted placement clarification. Unknown valid id is toolbar-only, never fabricated as section option; retained loading/error/empty. design states; Patron ruling 3; T024. |
| Compass Low 2 | Low | Accepted ASSUMPTIONS status refresh; grill closed, challenge pending. |
| Compass Low 3 | Low | No further edit. Historical Q3 400 remains append-only with C1 superseding status to 422; Compass judged bottom amendment sufficient. Active requirements all 422. |
| Compass informational | Info | Accepted with Ledger F4 status refresh. Design existence does not clear Gate 1. |

## Evidence limits and next boundary

Recon part 2 lines 26-37 confirms collection PK conflicts are not currently covered by the existing unique-conflict retry predicate and ambient transactions have no retry loop; these are implementation obligations, not inherited guarantees. Round 2 plan Approach 2 and T010/T011 retain whole-unit transaction safety and require proof across reachable save paths while preserving caller transaction ownership.

No ticket contradiction or constitution departure was judged necessary by Patron. No owner checkbox is opened by these rulings. No Gate 1, implementation, delivery verification, PR approval, or merge is implied. Round 2 saved passages are verified; the final freeze receipt follows.


## Phase 2 step 6 final freeze receipt — 2026-10-28

Keel verified both correction rounds against all three original axis reports, Patron step 6 rulings (7a335ac3) and final F7 ruling (10ffc9c1), recon-DEV-424-2, and Palette design corrections. All 17 findings plus the informational status item have explicit dispositions in PLAN_CHALLENGE.md. No retained Critical, High, or Medium blocker; no open decision or missing axis. Accepted fallback-clear residual and retained orphan rows remain deliberate Q2/Q5 behavior; no scope expansion or owner checkbox.

Verdict: PLAN FROZEN for Gate 1 review. The hard two-round cap was honored. Gate 1 remains closed; no implementation, test/gate rerun, spec PR approval, or merge is authorized by this freeze. Phase B must recheck dependency landing and main/contract drift. PLAN_CHALLENGE.md records the frozen artifact SHA256 values and working-tree identity; this freeze includes saved uncommitted/untracked artifacts, not only HEAD.

## Frozen saved-artifact manifest

Worktree: F:/Dev/LamuFlix.worktrees/feature-424-spec; branch feature/424-spec; HEAD 10ffc9c1c2c575b835220cc19f08838c6eb62a38. SHA256 covers saved bytes including uncommitted artifacts. PLAN_CHALLENGE.md is the receipt and is excluded from its own manifest.

| Artifact | SHA256 |
|---|---|
| brief.md | E5568BD3C4E4691989558C8BC8298A664E7236DEC0CCB5ECC364ED1E07B0597A |
| CONCLUSIONS.md | CCEB957FA7E988B174E0D59A1804199A35BD6E64186922FB9C63A812001171EA |
| ASSUMPTIONS.md | 0662C601410A490A1CC0329AE29794820E23D2A8BB9037A48F899CB287EC6E5B |
| CONTEXT.md | 781EEB5FDD5C6DE7447F07C4AA7CC851D9F0FF60E676C81269E4991895579F22 |
| spec.md | 5AEDE0FB1088FFB4537DC80DC657E8BBB917A51B3116799FAB1476FA35080095 |
| plan.md | 77784615B0083A3DD6A3E61E33B81319E42D9123195DC971CFE88F2C66BDD6B9 |
| tasks.md | E9AAB6AF590BD1FF21C7783AF6AB5A51A365753ACBE7ADDB5CFAAFAEB9017A40 |
| design.md | 357FB6C022BA44A84BB724CBE6EE93A149DFCEF57DB5E51890FAB33FD97D4091 |
| checklists/requirements.md | E804899CDE56CE1B1BCAE6F8C23DF264574DCF5D436C61A28CE3CBA02FB38735 |
| DRAFTING_RECEIPT.md | 651C158D3D6890410240DBDBCAAE87FCE49D899F7F4A5321F5744363054CB85A |
