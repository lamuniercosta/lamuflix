---
description: "Task list for DEV-559 README overhaul and operations docs move"
---

# Tasks: README overhaul and operations docs move

**Input**: Design documents from `specs/DEV-559/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q10, Q9 amended), `CONCLUSIONS.md`, `ASSUMPTIONS.md`; Maestri notes `DEV-559`, `recon-DEV-559`; `specs/PRODUCT.md`.

**Status**: Draft (Spec Kit Phase 2 step 3.2). Gate 1 / T000 and Build remain pending. Quill does not claim freeze.

**Tests**: Required regression is existing `SubtitleSetupDocumentationTests` (FR-013; Q9 amended). No new test projects or tools. Other checks are heading inventory, command table, link resolution, vocabulary, gates, and PR-body evidence (`brief.md` Test strategy / Closing bar).

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief Files touched; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no new dependency/tooling; no LocalPlay/secrets/Compose `-v`/mutation execution; never lower harness thresholds; no `src/` or `web/src/` changes. Otherwise-unassigned implementation tasks are owned by the generic Build lane. Named seats own Spec PR, pickup, gates, review, and ship per `task-pipeline`.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the Spec Kit PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T001+
Phase 1   T001          (Wisp pickup: analyze vs main, DEV-572, vocab path, before inventory; Keel reanalysis) — blocks production edits
Phase 2   T002–T004     (US2 operations move + vocabulary same-commit; US4 subtitle test + README Subtitles removal)
Phase 3   T005–T006     (US3 modernization.md + c4-container + diagrams)
Phase 4   T007–T008     (US1/US6 currency pass + on-the-day commands)
Phase 5   T009–T011     (US1 README top; US5 credits/screenshot; US5 PRODUCT.md)
Phase 6   T012–T014     (US6 after inventory, links, vocabulary, gates, tests, PR evidence, Size M review)
```

Ordering per `brief.md` Task ordering. No dependency cycle.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-559 Spec Kit PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-559 in `specs/DEV-559/CONCLUSIONS.md`. Receipt only. T001–T014 MUST NOT proceed without verified merge (`brief.md` Task ordering; no structural owner checkbox).

---

## Phase 1: Pickup recon (blocks production edits)

**Purpose**: Align artifacts to pickup `main`, record DEV-572 state, confirm vocabulary entries, capture before heading inventory. Read-only.
**Checkpoint**: Written pickup receipt; Keel read-only reanalysis clear. Any authorized drift amendment is a separate follow-up before production edits.

- [ ] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree: re-run `/speckit-analyze` against `main`; record whether DEV-572 is merged (Q7 transcoding wording); confirm `vocabulary-check.json` path and exceptions at lines 16, 21, 63, 68, 132; take the "before" heading inventory from README at the pickup base. **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. STOP `needs recon:` on missing facts; STOP for cited Patron ruling rather than expanding file/protected surface (`brief.md` Task ordering step 1). **Strictly read-only**.

**Checkpoint**: T002+ unlocks only with a passing Wisp receipt and Keel pickup analysis clear (plus any authorized drift follow-up amendments already applied).

---

## Phase 2: User Story 2 + User Story 4 - Operations move and subtitle test (Priority: P1)

**Goal**: Nine operations files exist; vocabulary `file:` re-points land with the move; subtitle test follows `subtitles.md` in the same green-commit discipline.
**Independent Test**: Heading inventory; README operations links resolve; `npm run check:vocabulary`; `dotnet test --filter "FullyQualifiedName~SubtitleSetupDocumentationTests"`.

### Implementation

- [ ] T002 [US2] **Build lane**. After T001: Create the nine files under `docs/operations/` from README text at HEAD using `ASSUMPTIONS.md` names (`docker-compose.md`, `windows-daily-driver.md`, `home-deployment.md`, `subtitles.md`, `ollama-embeddings.md`, `library-relative-upgrade.md`, `verification.md`, `observability.md`, `local-dotnet-tools.md`). H1 = old H2 verbatim; subheads up one level; no index; Compose keeps only quick start in `README.md` with the rest in `docker-compose.md`; move Architecture-decisions prose beyond links to `docs/architecture/README.md` if present; re-point relative links/images (`observability.md` → `../observability/aspire-dashboard-traces.jpg`); do not use main-checkout untracked `docs/operations/` (FR-004–FR-006; Q1).
- [ ] T003 [US2] **Build lane**. In the **same commit** as T002's text move: update `vocabulary-check.json` so each exception at lines 16, 21, 63, 68, and 132 whose phrase moved gets `file:` re-pointed to the destination doc only; keep phrase and reason; add no new exceptions and do not widen any phrase (FR-014; Q10).
- [ ] T004 [US4] **Build lane**. In one commit with removal of the Subtitles section from `README.md`: in `tests/LamuFlix.UnitTests/SubtitleSetupDocumentationTests.cs`, replace the README path with `Path.Combine(FindRepositoryRoot(), "docs", "operations", "subtitles.md")`; rename only README-named identifiers/messages (`ReadmeFileName` → path segments/constant, `ReadReadme` → `ReadSubtitlesDoc`, `readme` locals, `Readme_` → `SubtitlesDoc_`, failure messages naming `docs/operations/subtitles.md`); add no heading locator; leave assertion count/meaning, both documentation arrays, `FindRepositoryRoot`, and both `ApiSettingsExample_*` tests unchanged (FR-013; Q9 amended). Keep the test green at every commit.

**Checkpoint**: Operations files and vocabulary move are committed together; subtitle test green against `subtitles.md`.

---

## Phase 3: User Story 3 - Modernization and C4 currency (Priority: P1)

**Goal**: Story and diagrams live under `docs/architecture/` and stale migration/OMDb-only claims are fixed.
**Independent Test**: `docs/architecture/modernization.md` exists; README links to it; both diagrams include TMDb; `c4-container.md:15` count-free.

- [ ] T005 [P] [US3] **Build lane**. After T004: Move the 2018–2026 modernization story and its inline C4 from `README.md` into new `docs/architecture/modernization.md`; leave a README link to that file (FR-008; Q2).
- [ ] T006 [US3] **Build lane**. After T005: Update `docs/architecture/c4-container.md` (count-free migration wording at the former `:15` claim) and both C4 diagrams so TMDb appears beside OMDb; apply the same TMDb addition in the modernization diagram (FR-007; Q2).

**Checkpoint**: Story/C4 relocated; named stale diagram/migration claims corrected.

---

## Phase 4: Currency pass and command verification (Priority: P1 / P2)

**Goal**: Every retained command/port/key/path/service name matches HEAD; Q6 command table drafted from on-the-day runs.
**Independent Test**: Diff of corrected commands; command table rows for README and `docs/operations/*`.

- [ ] T007 [US1] **Build lane**. After T006: Run the currency pass across `README.md` and `docs/operations/*` (and architecture docs touched): fix wrong commands, ports, setting keys, file paths, and service names against pickup HEAD; ensure README opening sentence/enrichment line says "OMDb and TMDb" (FR-007; Wanted 3; Q6).
- [ ] T008 [US6] **Build lane**. After T007: Run read-only/idempotent on-the-day commands (build, test, gates, format, web lint/typecheck/test/build, Compose up and down without `-v`, version probes, observability endpoints). Mark not run with Q6 reasons: home-cluster/GitOps apply, Windows installs/services, library-relative upgrades against a real library, secrets, LocalPlay execution, mutation ("runtime cost; covered by CI"). If a doc shows Compose `-v`, keep the command and mark that flag not run. Record the command table for the PR body (FR-011).

**Checkpoint**: Docs match HEAD; command table complete.

---

## Phase 5: User Story 1 + User Story 5 - README top, credits, screenshot, PRODUCT.md (Priority: P1 / P2)

**Goal**: README top matches Wanted 1 order; credits and screenshot Done-when evidence; PRODUCT.md tracked.
**Independent Test**: Ordered README read; terms evidence; image size; `git` tracks `PRODUCT.md`.

- [ ] T009 [US1] **Build lane**. After T008: Rewrite the `README.md` top block in brief order: name + CI badge only + one sentence; screenshot with [assumed] caption; quick start ≤3 commands (`git clone`, `cd`, Compose up as verified; no tag checkout); CHANGELOG link naming `v0.1.0`; "What it does today" from CHANGELOG `v0.1.0`; "What it does not do" with Q7 DEV-572 wording from T001; operations links; "Data sources" credits (terms checked on the day per T010). Below top: link to `docs/architecture/modernization.md`, one-line `docs/adr/` link, remaining short sections. PRODUCT.md copy rule (FR-001–FR-003, FR-010; Q5/Q7; `ASSUMPTIONS.md`).
- [ ] T010 [P] [US5] **Build lane**. With T009: Check OMDb and TMDb terms pages on the day; quote verbatim credit wording in Data sources; record URLs, dates, and wording for the PR body; add an official logo under 1 MB in `docs/images/` only if terms require one in documentation; if terms require in-app attribution, stop for Patron→Rigger follow-up, else noted no ticket (FR-010; Q5).
- [ ] T011 [P] [US5] **Build lane**. After T001 (may parallel T009–T010 once Compose/stand-in library is available): Seed a stand-in library via the existing import API (ADR 0023; no new seeding script); replace `docs/images/library.png` in place as PNG under 1 MB of the library grid; apply [assumed] caption (FR-009; Q3). Copy `F:\Dev\LamuFlix\PRODUCT.md` verbatim to worktree root `PRODUCT.md`; read in full and scrub; if private data found, stop and report to Bernstein; do not merge with `specs/PRODUCT.md`; do not modify the main checkout; track the file (FR-012; Q8).

**Checkpoint**: README top complete; credits/screenshot/PRODUCT.md Done-when evidence ready.

---

## Phase 6: User Story 6 - Closing evidence, gates, and review (Priority: P2)

**Goal**: After inventory, links, vocabulary, tests, gates, and Size M three-axis review meet the closing bar.
**Independent Test**: PR-body nine Done-when evidence lines; green tests/gates/vocabulary; three axis reports; round cap 1+1.

- [ ] T012 [US6] **Build lane**. After T009–T011: Take the "after" heading inventory; verify every relative link and image in `README.md`, `docs/operations/*`, `docs/architecture/modernization.md`, and `docs/architecture/README.md` resolves (hand or existing script; no new tooling); run `npm run check:vocabulary` in `web/`; run `dotnet test` including `SubtitleSetupDocumentationTests`; run Roslyn, complexity ≤15, and InspectCode on the changed `.cs`; run `dotnet format --verify-no-changes`; record mutation N/A; record DEV-558 verified-vacuous (FR-005, FR-011, FR-014–FR-016; Closing bar).
- [ ] T013 [US6] **Build lane**. After T012: Complete PR-body evidence for all nine Done-when lines (heading inventories; command table; terms URLs/dates/wording; screenshot size; corrected lines; PRODUCT.md tracked commit; DEV-558 verified-vacuous) (`brief.md` Closing bar item 1).
- [ ] T014 [US6] **Size M review**. After T013: Produce Risk (Sentry), Standards (Ledger), and Spec (Compass) axis reports; remediate only within round cap 1 review + 1 remediation; residual blockers to Bernstein; no unresolved Critical/High; do not claim closure past the cap (`brief.md` Closing bar; Q10).

**Checkpoint**: Delivery ready for ship path after review clearance; Gate 1 already done at T000.

---

## Dependencies and execution order

- T000 blocks all Build work.
- T001 blocks T002+.
- T002 + T003 same commit; T004 follows or pairs with Subtitles removal after `subtitles.md` exists.
- T005 → T006 → T007 → T008 → T009; T010 with T009; T011 parallel after T001 when stand-in library available.
- T012 → T013 → T014.

## Parallel opportunities

- T005 can start once T004 is green (different files from remaining README ops cleanup if already committed).
- T010 and T011 may run in parallel with each other once T001 is done and T009's Data sources slot is being filled.
- Axis authors (T014) may draft in parallel once T013 evidence exists, then consolidate.

## Implementation strategy

1. Gate 1 (T000), then read-only pickup (T001).
2. MVP slice: operations move + vocabulary + subtitle test green (T002–T004).
3. Architecture/currency (T005–T008), then README top and closing artifacts (T009–T013).
4. Size M review under round cap (T014).

## Notes

- DEV-558 is verified-vacuous — no implementation task (Q4 / FR-015).
- Missing `v0.1.0` git tag: noted, no ticket (Q7).
- Quill mirrors canonical `specs/DEV-559/plan.md` and `tasks.md` to the private Spec Kit ticket directory so IMPL_PLAN/TASKS resolve.
