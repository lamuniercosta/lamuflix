## Specification Analysis Report: DEV-361 (re-baseline on `3cd5802`, 2026-09-23)

**Prerequisites:** run from `F:\Dev\LamuFlix.worktrees\feature-361-spec` on `feature/361-spec`. HEAD was `df3cd45` plus the uncommitted re-baseline amend, rebased on `origin/main` `3cd5802` (DEV-360 merged). The merge base is `3cd5802`.

- With `PYTHONUTF8=1`, `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` exited **0**.
- Result: `FEATURE_DIR = specs/DEV-361`, `AVAILABLE_DOCS = [research.md, quickstart.md, tasks.md]`.
- `.specify/extensions.yml` is absent, so no hooks ran.

This report supersedes the Round 2 re-freeze report, which was measured at base `5a38215`.

**Inputs:**

- The T001 drift re-baseline: `research.md`, "Re-baseline after DEV-360". It matches the Conductor's raw T001 measurements on `3cd5802` exactly.
- Keel's drift adjudication: updated baselines, not a re-plan (`CONCLUSIONS.md`, "T001 drift").
- Patron's item-5 ruling: §2.3 item 5 does NOT fire for a comment-only member bracket at `FilmesServices.cs:74,75` in `AssistirFilme`. No executable line changes, and there is no checkbox. It is folded next to F1.
- Rulings still in force: F1, P1, P2, P3, C1, Q2–Q4, owner B1 [x], and owner T020 (A).

**Re-baseline deltas verified in this pass:**

- The B1 group totals reconcile to 112 hits in 18 files: D 18/5, T 34/3, M 40/6, W 20/4.
- `FilmesServices.cs` has 12 hits: 1 pre-existing (`:304`) plus 11 added by DEV-360.
- 19 files are edited: the 18 B1 files plus `TagHelpers/Extensions.cs`. The six zero-hit Data models stay in `$files` and are not edited.
- The five-target hits are 10. `UnitTest1.cs` moved +8, to `:69,80,109,120`.
- Cross-file citations were checked at `3cd5802`, and all are unchanged:
  - `FilmesController.cs:97,103,108`;
  - `HomeController.cs:40`;
  - `FilmesServices.cs:151,350` (`QueryableResult`);
  - `FilmesServices.cs:166` (`DynamicQuery`) and `:171,364` (`DynamicSort`);
  - `AssistirFilme` at `:66-77`, with the LocalPlay check at `:68` and `ProcessStarter` at `:76`.

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| I1 | Inconsistency | MEDIUM (fixed in this amend) | spec.md FR-008; SC-005; tasks.md T017 | FR-008 limited the suppression fallback to hits whose fix would change "binding or deserialization behavior". That excluded the re-baseline's own bracket cases: DEV-360's EF-query `!` reads, where a fix changes the query value or predicate, and `AssistirFilme`, where Patron forbids executable changes. SC-005 already said "reachable behavior". | FR-008 now makes the fallback apply to any hit with no fix that meets every FR-008 condition, and lists binding, deserialization, query behavior, and the Patron item-5 constraint as typical cases. Scope is unchanged. |
| I2 | Inconsistency | LOW (fixed) | plan.md Phase 1 group T | The reason column still listed "fixture initialized in `[TestInitialize]`". DEV-360 removed that fixture and its `:30` hit. | Removed. The FR-008 reason list never carried it. |
| I3 | Inconsistency | LOW (fixed) | plan.md P1 "Known gap" | Cited the `[Ignore]`d `UnitTest1.cs:387` `DynamicQuery` copy, which is at `:395` at `3cd5802`. | Updated to `:395` (`:387` at `5a38215`). |
| I4 | Inconsistency | LOW (fixed) | plan.md P1; tasks.md T019 | "`DynamicQuery`'s callers (`FilmesServices.cs:166,171,364`)": `:171,364` call `DynamicSort`. This wording predates the re-baseline. | Now reads `DynamicQuery` at `:166`, `DynamicSort` at `:171,364`. Both public signatures stay unchanged. |
| L1 | Terminology | LOW (left as is) | plan.md P2 | Patron's size ruling says "mechanical suppression". Fix-first adds judgment per hit, but that does not change size:M. | Patron-owned text. Carried over from the Round 2 report. |
| U1 | Underspecification | LOW | FR-010; T019 | `DynamicQuery` is still untested. Mitigations: the T001 read-only feasibility check, the desk-check reconciling to 12 (the file is unchanged at `3cd5802`), and the zebra-fallback evidence rule. | Not blocking. The hard stop stands. Carried over. |

**Coverage summary**

| Requirement | Tasks |
|---|---|
| FR-001 | T004, T011 |
| FR-002 | T001, T010, T011 (including the six zero-hit files absent from the diff) |
| FR-003 | T005 |
| FR-004 | T006 |
| FR-005 | T007–T009 |
| FR-006 | T011 |
| FR-007 | T004, T014–T017 |
| FR-008 (fix-first) | T014–T017 (Phase 4 header), T019, T011 (bracket parity; `AssistirFilme` comment-only check) |
| FR-009 | T015, T010 |
| FR-010 | T019 (owner choice A, T020), T001 (feasibility), T012 |
| SC-001 | T004, T011 |
| SC-002 | T010 |
| SC-003 | T011 |
| SC-004 | T005–T009 |
| SC-005 | T014–T017, T011 |
| SC-006 | T012 |

**Constitution alignment:** no conflicts.

- The fallback brackets are targeted ReSharper suppressions with a brief justification (`constitution.md:345-346`).
- The FR-008 reason "nullable-column read; `!` preserves the pre-DEV-360 contract" states a justification. It does not claim the value is non-null.
- `Features:LocalPlay` gating (`constitution.md:208`) is untouched, because the `AssistirFilme` change is comment-only and checked by T011.
- T019 keeps the mandated helper-extraction remedy (`constitution.md:344-345`).
- Principle IX: no MSTest tests are added.

**Unmapped tasks:** T001, T003, T013 (superseded), T018 (done), and T020 (owner gate, answered (A)) are process tasks.

**Metrics:**

- Requirements: 16 (10 FR, 6 SC).
- Tasks: 20, of which 19 are live.
- Coverage: 100%.
- Ambiguities: 0.
- Duplications: 0.
- Critical: 0. High: 0.

**Next actions:**

- No Critical or High findings. The re-baseline adds, drops, or reorders no work, so there is no owner checkbox.
- Phase B re-copies `specs/DEV-361/` from this commit into `F:\Dev\LamuFlix.worktrees\DEV-361` and hash-matches every file before T001 closes.
- Then T001 records the `3cd5802` baselines. They need no re-run if `origin/main` is still `3cd5802`.
