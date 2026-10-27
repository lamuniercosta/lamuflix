# DEV-331 Plan Challenge Adjudication

Status: FROZEN FOR GATE 1 REVIEW — all 21 findings adjudicated and two repair rounds verified. Gate 1 remains CLOSED on Q7; no Phase B authorization.

## Evidence boundary

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-331-spec`; branch `feature/331-spec`; intake HEAD `52e443666b5e767edc93befc20eb55de2e77787b`.
- All three mandatory axis reports read: `findings-DEV-331-Sentry` (8), `findings-DEV-331-Ledger` (7), `findings-DEV-331-Compass` (6): **21 findings**.
- Local and remote facts limited to `recon-DEV-331` and `recon-DEV-331-2`; challenger citations are claims being adjudicated, not permission for independent recon.
- Decision baseline: brief Q1–Q9/Q3a; Q3a, Q5 and Q7 precedence preserved. Constitution IX requires real WireMock transport; VIII and DEV-303 naming precedent support neutral helpers/tests. No production files inspected or changed for this adjudication.
- Q8 governs: Critical/High/Medium block; Low needs explicit nonblocking disposition; two rounds maximum. Sentry calling its Medium findings Phase B constraints does not waive this bar.

## Finding-by-finding disposition

Report-local identifiers are qualified by axis to avoid collisions. Accepted repairs were closed by the round-1 and final round-2 read-back receipts below.

| Finding | Report severity | Adjudication | Required closure / basis |
|---|---|---|---|
| Sentry F1 | Medium | Accept; blocking repair | Normalize only TMDb BaseAddress trailing slash; test slashless configured base preserves `/3/`. Q4 URL contract; registration file already approved. |
| Sentry F2 | Medium | Accept; blocking repair | Define nonthrowing usable poster path, reject malformed/host/scheme/backslash/control/dot segments; adversarial fixture tests. Q3/Q5 optional domain values; recon-2 T2/T6/T9. |
| Sentry F3 | Medium | Accept; blocking repair | Name default client-factory and resilience logger capture, including retries/errors, structured state, exceptions, activities and health descriptions. Q5 redaction/SC-003. |
| Sentry F4 | Low | Accept clarification; nonblocking after explicit ruling | Distinguish caller OperationCanceledException from timeout/breaker exception; preserve Q5 and no OMDb edits. Patron PC3 accepted; repaired and verified. |
| Sentry F5 | Low | Accept residual; nonblocking | Completion-order last-write-wins passive state is intentional; single immutable snapshot atomic publication/read, no split-field updates. Already T007; Q4/Q6; recon section 3. |
| Sentry F6 | Low | Accept clarification; nonblocking | Explicit search 404 InvalidResponse; only details/find 404 is NotFound. Q5; recon-2 T2/T8. |
| Sentry F7 | Info | Accept residual; nonblocking | Shared threshold and resilience tuning coupling already Q3a/Q5. No new options or decoupling work; DEV-423 context only. |
| Sentry F8 | Info | Accept residual; nonblocking | Reactive handler exhaustion semantics only; no proactive throttle or consumer retry added. Q5; recon-2 T8; out-of-scope expansion rejected. |
| Ledger M1 | Medium | Accept contradiction; blocking repair | Keep no mocked HTTP transport. Move fixture mapping via FindAsync into existing IntegrationTests with real typed client/WireMock, no visibility seam; update every unit-mapping claim. Constitution IX and Q6 dominate stub-handler precedent. Patron PC4-PC5 accepted; repaired and verified. |
| Ledger M2 | Medium | Accept telemetry split; reject required abstraction/rewrite | Own neutral telemetry test file; dedicated bounded probe. No general provider framework or rewrite of OMDb probe. Small isolated setup duplication is acceptable; unnecessary extraction is follow-up only under section 2.3/Q8. |
| Ledger M3 | Medium | Reject renaming premise; rationale repair | Q4/Q6 explicitly require neutral isolated health types. Q7 preserves neutral health naming. Separate blank-key dependency and state isolation justify small duplication; no shared extraction. Patron PC4-PC5 accepted; repaired and verified. |
| Ledger M4 | Medium | Reject vendor-name change; rationale repair | Neutral tests/helpers explicitly frozen in brief and recon section 3 DEV-303 precedent; VIII applies beyond Core. Options test path already explicit in T005. Preserve neutral names and record Patron confirmation; no widened vendor exception. |
| Ledger L1 | Low | Accept; nonblocking repair | Explicit T002/T003/T007 prerequisites for T011; clear parallel semantics and US5/US6 ownership; exact existing paths for T024/T025 from recon sections 6/9. |
| Ledger L2 | Low | Decline mandatory TDD; nonblocking | Q6 ordering stands; TDD optional. Fixture-first T001 retained; all mapping/classification tests mandatory before verification. No behavioural gap or additional task needed. |
| Ledger L3 | Low | Accept constraint; nonblocking repair | Preserve unrelated pre-existing comments; add no explanatory comments; review touched diff. BannedSymbols remains build/analyzer enforcement, no redundant gate task. |
| Compass F1 | Medium | Accept; blocking repair | Keep https-or-loopback validator private nested within new TmdbOptions. No shared validator file or OmdbOptions edit; stays in approved scope. Q4/Q6. |
| Compass F2 | Medium | Accept; blocking repair | Check explicitly takes IOptions<TmdbOptions> alongside isolated state; initial and disabled blank-key Unhealthy tested. Ticket missing-key AC; Q4. |
| Compass F3 | Low | Accept; nonblocking repair | Repair `[P]` and dependency prose consistently, merged with Ledger L1. |
| Compass F4 | Low | Accept inspection ownership; nonblocking | T023 owns inspection that AddMetadataProvider does not invoke AddLamuFlixOptions; Q4/recon section 5. No invented runtime negative-call test required. |
| Compass F5 | Low | Accept; nonblocking repair | Colocate fixtures with moved integration mapping tests; same-project consumers, explicit fixture loading/build-copy strategy. No UnitTests reference or mirrored drift. |
| Compass F6 | Low | Accept wording repair; nonblocking | Reuse existing ActivitySource/Meter names, allow own same-name ActivitySource instance. Existing histogram reused; no second metric identity. Q6/recon section 3. |

Counts: 9 Medium, 10 Low, 2 Info; 0 Critical/High. All 21 have a final disposition; required repairs are verified closed. Medium naming/duplication recommendations are adjudicated against frozen authority rather than automatically treated as implementation mandates.

## Phase boundaries

This document covers Phase A plan challenge only. No tests, analyzers or mutation gates run. A later planning freeze will not close the Q7 owner checkbox, authorize Phase B, establish code review clearance, or establish a merge bar. Constitution amendment/ADR remain Conductor-coordinated and owner-dependent.

## Repair verification — round 1 read-back

Keel read the full repaired plan/tasks/receipt, functional requirements and checklist after Quill returned. Original findings are resolved or explicitly nonblocking under PC1-PC6. Additional consistency item PC7: fixture-copy project file was unnamed; Patron explicitly approved only `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` for `Fixtures/SecondaryMetadata/**/*.json`, `CopyToOutputDirectory=PreserveNewest`, preserving relative directories. Round 2 is limited to naming this file and concrete loading strategy, removing `or equivalent`. No third round permitted.

Verified closure references (task IDs retained across repairs):

| Findings | Repaired evidence |
|---|---|
| Sentry F1 | FR-011; T015 normalization; T018 outgoing slashless-base path proof |
| Sentry F2 | FR-005; T001 hostile fixtures; T010 nonthrowing mapper; T013 real-client tests |
| Sentry F3 | SC-003; T020 factory/resilience capture enabled and all diagnostic surfaces |
| Sentry F4/F6 | FR-006; US2; T014/T019/T021 explicit classification/cancellation cases |
| Sentry F5 | FR-010/T007 immutable Volatile snapshot; accepted last-write-wins residual |
| Sentry F7/F8 | plan Risks/Approach; spec Edge Cases; accepted configuration/reactive-policy residuals |
| Ledger M1 | FR-014; plan Test Strategy; T013 via FindAsync+WireMock in IntegrationTests |
| Ledger M2 | plan dedicated bounded probe; T017/T022 dedicated telemetry file; no shared framework |
| Ledger M3/M4 | PC5; plan Constitution/Health/Layout; neutral names preserved with isolation rationale |
| Ledger L1 / Compass F3 | T011 explicit prerequisites; T005/T009 markers removed; Dependencies; exact T024/T025 paths; T026 first |
| Ledger L2 | plan/tasks optional TDD and fixture-first policy; all required tests before gates |
| Ledger L3 | plan Constraints; tasks Conventions/T015/T016/T032 preserve unrelated comments, no new explanatory comments; existing BannedSymbols enforcement |
| Compass F1/F2 | FR-010/FR-012; T002 private validator; T008 options dependency; T009 initial/disabled blank-key cases |
| Compass F4 | T023 inspection ownership explicitly stated |
| Compass F5 | FR-014; T001/T013 same-project fixtures; PC7 concrete copy-file closure in final round |
| Compass F6 | FR-009; T011/T022 same-name ActivitySource and existing histogram |

## Read-only cross-artifact analysis

Speckit prerequisites ran once: resolved `specs/DEV-331`, tasks present. No before/after-analyze hooks are configured. Analysis is read-only; subsequent authorized recording and Quill repairs are separate actions. No new conflicting requirement, unmapped task, duplication defect or constitution departure found. Existing Q7 constitution departure remains owner-dependent and blocks Gate 1; this result does not declare unrestricted constitution compliance.

| Requirement | Tasks |
|---|---|
| FR-001 | T010–T012 |
| FR-002 | T011 |
| FR-003 | T011, T020 |
| FR-004 | T011, T018 |
| FR-005 | T001, T010, T013 |
| FR-006 | T014, T019, T021 |
| FR-007 | T011, T018, T020 |
| FR-008 | T012, T020 |
| FR-009 | T003, T011, T022 |
| FR-010 | T007–T009, T023 |
| FR-011 | T015–T016, T023 |
| FR-012 | T002, T004–T006, T025, T026 |
| FR-013 | T015, T021 |
| FR-014 | T001, T005, T009, T013, T017–T025 |
| FR-015 | T001, T003–T004, T006, T015–T016, T024–T025 |
| FR-016 | T027–T034 |
| SC-001 | T009, T013, T018–T019, T023 |
| SC-002 | T020, T023, T025 |
| SC-003 | T020, T022 |
| SC-004 | T027–T032 |
| SC-005 | T033 |
| SC-006 | T023–T024 |

Inventory: 16 FR + 6 SC, 34 task IDs; 22/22 keys mapped (100%); no unmapped tasks. T026 separately enforces the owner-dependent prerequisite. No claim that these planned tests or gates have executed.


## Final round-2 verification and freeze receipt

PC7 verified at spec.md:170-171, plan.md:91/107, tasks.md:19/56, checklist and drafting receipt round-2 sections. Exact existing IntegrationTests project file, fixture glob, PreserveNewest, preserved relative paths and AppContext.BaseDirectory loading are explicit. No or-equivalent choice remains in normative spec/plan/tasks.

- **21/21 dispositions complete**: 9 Medium, 10 Low, 2 Info. No unresolved technical plan blocker. Rejected naming/generalization proposals are settled under PC5, not silently skipped.
- **2/2 repair rounds consumed**: first consolidated original-findings repair, second bounded PC7 consistency repair. No automatic third round.
- **Analysis clean within the ruled plan**: 22/22 requirement keys mapped to exactly 34 unique tasks T001-T034; zero unmapped tasks or new conflicts. Q7 remains the single open owner-dependent constitution departure.
- **Integrity**: original brief prefix 30085 bytes SHA-256 `6c2a84d4dc472dd177aba2771914627dfe2b7abd6721023557c45261f125acf0`; original conclusions prefix 27180 bytes SHA-256 `06f71c9fa65a47a130b57b873b139831605a168d1834e43fc992865641a856e7`; both verified preserved. Q7 checkbox verified verbatim in spec.
- **Identity**: branch feature/331-spec, worktree F:/Dev/LamuFlix.worktrees/feature-331-spec. Patron recorded CONCLUSIONS in HEAD c6ede338179b8ea827417d594235c89bbe0d4e86 during adjudication. Other draft bytes remain working artifacts; these hashes pin the reviewed bytes, not a claim they are committed at HEAD.
- **Phase scope**: only feature Markdown changed; no production writes or build/test/analyzer/mutation runs by Keel. Baseline-to-HEAD diff is the Patron conclusions commit; working delta is conclusions append plus untracked feature artifacts. No independent repository/API recon performed.

**FROZEN FOR GATE 1 REVIEW. Gate 1 remains CLOSED on the unchanged Q7 checkbox and constitution amendment/ADR landing.** Planning freeze does not clear Phase B pickup, implementation, code review, merge bar or merge. Conductor owns next spec-PR/amendment/ADR coordination. User owns Gate 1 decision and merge.

### Reviewed artifact byte manifest (SHA-256)

| Artifact under specs/DEV-331 | SHA-256 |
|---|---|
| `spec.md` | `2ee44f634fb007457fad9e1ad0c4a1705954b659759f8f1b35d672f22c3e9e3b` |
| `plan.md` | `2e3c66bf37778c5bd6ec58b79a623d1c789f7a20969329c37e787721fb2f77b5` |
| `tasks.md` | `ea7be76628bd3708ddf6c4d6268c851d7e590d4954a571cb43b71095dc76e166` |
| `checklists/requirements.md` | `42db115637d91cd6f08429835a512396f6ad874d470775172460108da7e5c9b4` |
| `DRAFTING_RECEIPT.md` | `4ce2aee09dc988ae10782005ee4993100e43d2b99dc12de39c18b93cb3714220` |
