# DEV-424 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-28
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-424-spec` · branch `feature/424-spec`
**Method**: `/speckit-specify` → `/speckit-clarify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-checklist` in one ask. `/speckit-analyze` not run by Quill (Keel owns read-only analyze per after_tasks / role). `.specify/feature.json` already pointed at `specs/DEV-424` and was left unchanged. `before_specify` grill-with-docs already satisfied by frozen `brief.md`.

**Inputs**: frozen `specs/DEV-424/brief.md` (Q1–Q8 + C1 + Phase 2 step 6 amendment @ `7a335ac3` + F7 amendment @ `10ffc9c1`), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `CONTEXT.md`, Maestri notes `DEV-424`, `recon-DEV-424`, `recon-DEV-424-2`, `findings-DEV-424-Sentry`, `findings-DEV-424-Ledger`, `findings-DEV-424-Compass`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates only; no edits).

**Artifacts written / repaired** (Quill-owned only):

- `specs/DEV-424/spec.md`
- `specs/DEV-424/plan.md`
- `specs/DEV-424/tasks.md`
- `specs/DEV-424/checklists/requirements.md`
- this receipt

**Preserved (not edited by Quill this round)**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `CONTEXT.md`, `design.md`, `PLAN_CHALLENGE.md`.

**Owner checkbox**: none. No ticket contradiction. Analyze C1 resolved within constitution (`CONCLUSIONS.md` C1). Step 6 challenge rulings and Sentry F7 accepted with no constitution departure (§2.3(b) not triggered).

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ticket/ASSUMPTIONS and Patron step 6 / F7 rulings.

### needs decision:

- *(none — Sentry F7 closed by Patron ruling @ `10ffc9c1`; historical open residual superseded below.)*

### Analyze fix round 1 (Keel numbered list + Patron C1 disposition)

| Item | Disposition |
|---|---|
| **C1 CRITICAL** (400 vs Principle V 422) | **Resolved** by `CONCLUSIONS.md` C1 (commit `2754a161`). Spec/plan/tasks/checklist assert **422** ProblemDetails. |
| **I1 MEDIUM** (web TDD order) | **Applied**. Phase 6: T021 failing tests → T022 mocks → T023–T025 production. |
| **C2 MEDIUM** (architecture wording / Constitution Check) | **Applied**. ports and adapters with feature-organised Core; Principles I, II, V. |

### Plan-challenge correction round 1 (Keel numbered list + Patron step 6 @ `7a335ac3`)

| Item | Disposition |
|---|---|
| **1. Sentry F1 / Ledger F7** | **Applied**. Natural int PK `ValueGeneratedNever`; required whole-save rollback/reset/re-read/retry (implement-and-prove, not existing proven); T011 forced two-movie concurrent same-new-id oracle → one row + both memberships + no surfaced conflict. Spec/plan/tasks/FR-002/FR-003/SC-001. |
| **2. Sentry F2** | **Applied**. Explicit fallback-clear + failure-preserves tests; accepted residual until next successful TMDb; no release-note task. US1 scenarios 4–5; FR-003; SC-002; T010/T011. |
| **3. Sentry F3 / Ledger F2** | **Applied**. T017: raw `string? CollectionId`, `CollectionIdKey`, `ReadNumber`, repeated-key detection; empty/`abc`/`0`/`-3`/repeated → 422; `MovieQueryValidator` null-or->0; codec rejection; preserve ReSharper pair; `LibraryEndpoints` complexity watchlist. |
| **4. Ledger F3** | **Applied**. Backend tests alongside; green before T020; web red-first unchanged; named `MovieQuerySurvivorTests` / `MovieQueryStringSurvivorTests` / `EfMovieRepositorySurvivorTests`. |
| **5. Ledger F6** | **Applied**. `CollectionFacet`/`CollectionDto` `(int Id, string Name, int MovieCount)`; no empty `GetCollectionsQuery` validator; unchanged `AddHandler` chain; reject long count. |
| **6. Sentry F4 / F6** | **Applied**. Plain-text hostile-name + overlong-layout web oracles (T021/T022/T025); migration natural PK + `CollectionId` index assertions (T007/T009). |
| **7. Sentry F7** | **Needs decision / needs recon** *(historical — superseded by round-2 closure below)*. Do not invent inherited malformed-provider fail scope; flagged pending Wisp addendum. |
| **8. Ledger F4 / Compass informational** | **Applied**. Status text updated: `design.md` exists; challenge corrections pending; Gate 1 / freeze unclaimed. No freeze claim. |
| **Patron UI (step 6 ruling 3)** | **Applied** to plan/tasks/spec test contract: fieldset/legend + `label.lamu-chip` sr-only checkbox single toggle; no skeleton/`aria-busy`; legend-only empty; toolbar-only unknown fallback retained loading/error/empty. |
| **Sentry F5** | **Out of scope** — orphan telemetry/cleanup noted no fix. |
| **Compass Q3 historical paragraph** | **No edit** — bottom-section supersede annotation judged sufficient. |
| **design.md** | **Not edited** — Palette owns design corrections. |

### Plan-challenge correction round 2 (Keel FINAL bounded fix list + Patron F7 @ `10ffc9c1` + recon-DEV-424-2)

| Item | Disposition |
|---|---|
| **1. Sentry F7 deliberate `belongs_to_collection` rule** | **Applied — closes round-1 item 7**. Present object nonpositive id or blank name → whole TMDb details response Failed InvalidResponse (not Found with field silently dropped); missing/null → valid absent; final failed/unavailable enrichment preserves prior metadata/membership; successful non-TMDb fallback still clears Q5; not a wholesale optional-field fail claim. Spec Scope / US1 scenarios 6–7 / Edge Cases / FR-001 / Clarifications; plan Approach 1 + Test Strategy + Risks; tasks T003/T004 oracles; checklist; this receipt. |
| **2. recon-DEV-424-2 retry evidence + T010/T011 obligations** | **Applied**. Plan Approach 2 + Risks cite recon-DEV-424-2 lines 22–33: existing retry does not classify collection PK conflicts; ambient metadata writes have no retry loop; collection conflict recovery required implement-and-prove across reachable save paths with whole-unit rollback/reset/re-read; no partial catch/continue; preserve caller transaction ownership; no invented extra infrastructure or partial retry inside an aborted ambient transaction. T010/T011 keep those concrete obligations. AddHandler empty-validator pass-through confirmed by recon-DEV-424-2 lines 39–45 (plan Constitution Check Principle II; checklist). All other round-1 rulings stand. |

**Clarify**: coverage Clear including closed F7; no open drafting `needs decision`; no `[NEEDS CLARIFICATION]` markers inventing fail scope.

**Status**: Plan-challenge correction round 2 FINAL applied — ready for Keel read-back of changed passages. No new scope, design edit, implementation, gates, commit, or freeze. Hard two-round cap honored; remaining blockers (if any) reported rather than starting another round. Gate 1 not opened.

**Next**: Keel reviews correction round 2 → challenge disposition/freeze → Gate 1.

## Phase 2 step 6 final freeze receipt — 2026-10-28

Keel verified both correction rounds against all three original axis reports, Patron step 6 rulings (7a335ac3) and final F7 ruling (10ffc9c1), recon-DEV-424-2, and Palette design corrections. All 17 findings plus the informational status item have explicit dispositions in PLAN_CHALLENGE.md. No retained Critical, High, or Medium blocker; no open decision or missing axis. Accepted fallback-clear residual and retained orphan rows remain deliberate Q2/Q5 behavior; no scope expansion or owner checkbox.

Verdict: PLAN FROZEN for Gate 1 review. The hard two-round cap was honored. Gate 1 remains closed; no implementation, test/gate rerun, spec PR approval, or merge is authorized by this freeze. Phase B must recheck dependency landing and main/contract drift. PLAN_CHALLENGE.md records the frozen artifact SHA256 values and working-tree identity; this freeze includes saved uncommitted/untracked artifacts, not only HEAD.
