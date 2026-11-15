---

description: "Task list for DEV-473 private artifact migration and path scrub"
---

# Tasks: Move spec evidence and receipts out of the public repo and scrub local paths

**Input**: Design documents from `specs/DEV-473/`
**Prerequisites**: `plan.md`, `spec.md`, grill-closed `brief.md` (Q1–Q9 + D1–D4 + D3 resolved + P1–P7 + P3 precision), `CONCLUSIONS.md`, `recon-DEV-473` lines 1–52 (ticket/caller/guard/generator); historical D3 companion evidence in brief D3 exchange; exact scrub literals only in private recon lines 19 and 30–31
**Tests**: Required by brief Q8 / FR-016. Exact companion: `.specify/scripts/powershell/Test-ArtifactStorage.ps1`. Invoke from repo root: `pwsh -NoProfile -File .specify/scripts/powershell/Test-ArtifactStorage.ps1`. Strict errors; unique GUID temp fixtures; child-process real scripts with controlled environment; confinement and deterministic cleanup; no new dependency/project/helper layer; do not copy or dot-source Home domain stubs/helpers (D3). **All tasks that edit this one test file run sequentially (no `[P]`), including any added extension tasks.** Smoke uses disposable fixtures only, never live operator archives.
**Organization**: Foundational inventory → shared resolver + guard → bootstrap seed → user stories in priority order → polish/named blocking final verification.
**Status**: Draft under consolidated plan-repair round 1 of 2. Quill does not declare PLAN FROZEN, Gate 1, or runtime acceptance.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks). Do not mark `[P]` on any task that edits `.specify/scripts/powershell/Test-ArtifactStorage.ps1`.
- **[Story]**: US1–US5 map to `spec.md` user stories
- Include exact file paths in descriptions

## Path Conventions

- Shared resolution: `.specify/scripts/powershell/common.ps1` (`Get-FeaturePathsEnv` and related helpers)
- Feature create / prerequisites / setup: `.specify/scripts/powershell/create-new-feature.ps1`, `.specify/scripts/powershell/check-prerequisites.ps1`, `.specify/scripts/powershell/setup-plan.ps1`, `.specify/scripts/powershell/setup-tasks.ps1`
- Explicit init/migration/bootstrap: `.specify/scripts/powershell/initialize-artifact-storage.ps1` (explicit migration mode; bootstrap seed before activation) (D1, P1)
- Companion regressions: `.specify/scripts/powershell/Test-ArtifactStorage.ps1` (D3)
- Public selection: `.specify/feature.json` (precedence: `SPECIFY_FEATURE_DIRECTORY` → feature.json → numeric branch; recon 49–52)
- Maintained host adapters (P3 precision — not generated): `.agents/skills/**/SKILL.md` and `.claude/skills/**/SKILL.md` (named starters: task, grill-with-docs, speckit-specify, speckit-plan, speckit-tasks, pipeline, plus inventoried consumers); update in parity
- Guard: `scripts/hooks/guard.ps1` (`Deny-OutsideBoundary` narrow private-ticket extension) (P2)
- Receipts/docs: `scripts/new-mutation-receipt.ps1`, `scripts/load/README.md` (consume common resolver; no duplicate)
- Scrub surfaces: `.specify/memory/constitution.md`, `README.md`, `harness.yml` (worktreeRoot only for T026), `scripts/home/*.ps1`, retained `specs/DEV-*/` allowlist files, `docs/reviews/` operational evidence
- Private root logical shape: `<private-root>/<repository-id>/DEV-###/` (override `LAMUFLIX_ARTIFACT_ROOT`; repository-id from env-only `LAMUFLIX_ARTIFACT_REPOSITORY_ID`, default `LamuFlix`; absent/empty/whitespace → default; nonempty invalid → reject; no harness key) (D2, P6)
- Exact ticket/slash scrub patterns: private `recon-DEV-473` lines 19 and 30–31 only — do not reintroduce machine-root literals in public artifacts
- `$env:PYTHONUTF8=1` for authoring/check commands

## Conventions

- No new NuGet/npm dependency, project, top-level folder, or architectural layer
- No history rewrite; no legacy public write fallback; no tracked private-storage symlinks; no dual-read/public-write fallback
- Maintained `.agents`/`.claude` SKILL.md adapters are updated in parity; do **not** require an absent host generator; do **not** hand-edit actual generated/`install.ps1`-rendered outputs (`CodeMetricsConfig.txt`, `stryker-config.json`, `.editorconfig`); stop if a required generated change lacks supported tooling (P3 precision)
- Do not alter LocalPlay / `Process.Start` behaviour; route non-migration behaviour changes to Patron under §2.3
- Ticket-forced machine-path scrub of retained tracked content (including `README.md`) is authorized as path/content cleanup preserving behaviour (Q5); not a product README rewrite
- Gate verdicts: PASS / `SKIPPED (scope-empty)` / SKIP / N/A / Failure / Could not run stay distinct; never report scope-empty as PASS
- Existing no-explanatory-comments convention applies to implementation artifacts
- Build-lane ownership: otherwise-unassigned tasks belong to the Build seat after Gate 1; Quill does not implement
- No closed Gate 1 caller filename list; T001 and T018 record the bounded discovered caller set (D4)
- T014–T018 completion + inventoried caller proof gate US2 activation/delivery; partial/parallel work is not US2 completion (H3)
- Bulk historical archive/removals remain stage 4 / US3 after bootstrap (P1)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inventory producers/consumers and annotate private locations before code (read-only until storage is initialized)

- [X] T001 Inventory in-scope producers/consumers using current `recon-DEV-473` lines 1–52: include `create-new-feature.ps1`, `check-prerequisites.ps1`, `setup-plan.ps1`, `setup-tasks.ps1`, their authoring skills (`speckit-plan`, `speckit-tasks`, and related), shared `Get-FeaturePathsEnv` consumers, feature-selection precedence (explicit env → feature.json → numeric branch), maintained `.agents`/`.claude` adapters, `scripts/new-mutation-receipt.ps1`, `scripts/load/README.md`, and `scripts/hooks/guard.ps1`; record the bounded discovered caller set for T018 (D4). Confirm settled inventables D1–D4 and P1–P7. Preserve historical D3 companion evidence scope from brief D3. Do not invent alternate names, projects, generators, or dependencies.
- [X] T002 [P] Read-only location planning only: annotate the intended private migration inventory path under logical `<private-root>/LamuFlix/DEV-473/` and confirm `$env:PYTHONUTF8=1` for subsequent check commands **inside the existing Phase A artifact `specs/DEV-473/DRAFTING_RECEIPT.md`** (location-plan annotation section). Do not create directories or write under the private root until T003–T006 have initialized and validated storage (ordinary I/O remains fail-closed per Q7). Distinct from later initialized private inventory writes in T021.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared public/private resolution, ownership/hash/slug contracts, explicit initialization/migration primitives, and narrow guard extension that all stories use

**⚠️ CRITICAL**: No user-story cutover, migration removals, or scrub staging until this phase is complete. No private-root writes before T003–T006 complete. No private-required consumer activation before bootstrap T011b.

- [ ] T003 Implement shared private-root resolution (default LocalApplicationData/`LamuFlix/artifacts`, absolute `LAMUFLIX_ARTIFACT_ROOT` override, reject relative/in-repo/in-worktree) in `.specify/scripts/powershell/common.ps1`
- [ ] T004 Extend `.specify/scripts/powershell/common.ps1` with distinct public vs private path helpers, portable `.specify/feature.json` selection support via shared `Get-FeaturePathsEnv` (precedence per recon 49–52), numeric branch detection preservation, confinement checks (traversal, invalid IDs, symlink/junction escape, destination escape), and **FR-004/FR-005/P4 enforcement ownership**: canonical `<private-root>/<repository-id>/DEV-###/` layout, worktree/full-SHA isolation helpers for mutable run output/receipts, SHA provenance attachment for gate/review evidence, and **one shared per-ticket exclusive ownership contract** used by all real cutover writers — fail closed on competing writers, no automatic lock stealing, explicit release (concrete lock primitive chosen in `common.ps1` under this identical contract). T013 must assert competing **real caller processes**, not only helper behaviour. Cutover T014–T018 must call these helpers.
- [ ] T005 Add `.specify/scripts/powershell/initialize-artifact-storage.ps1` with explicit migration mode extending shared `common.ps1` logic (D1): create missing root on explicit init only; ordinary I/O remains fail-closed with actionable guidance (Q7); support verified bootstrap-seed mode for the in-flight ticket (P1) distinct from stage-4 bulk migration
- [ ] T006 Wire optional portable repository-id slug via environment-only `LAMUFLIX_ARTIFACT_REPOSITORY_ID` in `.specify/scripts/powershell/common.ps1` (D2, P6): default `LamuFlix`; **absent, empty, or whitespace-only → default `LamuFlix`**; nonempty invalid slugs rejected; no new `harness.yml` key or second configuration surface; never a machine path. Preserve all confinement checks.
- [ ] T007 Create `.specify/scripts/powershell/Test-ArtifactStorage.ps1` covering default/override roots, confinement rejections, missing/unwritable root messaging, explicit initialization, public-only reads without private storage, and P6 repository-id cases (absent/empty/whitespace default; nonempty invalid reject). Use strict errors, unique GUID temp fixtures under `Path.GetTempPath()`, child-process `pwsh -NoProfile -File` against real scripts with controlled environment, confinement assertions, and deterministic cleanup. Invoke: `pwsh -NoProfile -File .specify/scripts/powershell/Test-ArtifactStorage.ps1`. No Home stubs/helpers.
- [ ] T007b Extend `scripts/hooks/guard.ps1` (`Deny-OutsideBoundary`) with the sanctioned narrow private-ticket write allowance (P2): Edit/Write/apply-patch only to normalized, validated paths under the approved external private root/`<repository-id>/DEV-###/` layout; preserve protected-path checks and all unrelated boundary denials; **no shell bypass and no broad blanket allow**.
- [ ] T007c Extend `.specify/scripts/powershell/Test-ArtifactStorage.ps1` with guard allow/deny regressions for T007b (allowed confined private ticket path; denied outside-set / protected / unrelated cases). Depends on T007 and T007b. Sequential companion edit — no `[P]`.

**Checkpoint**: Foundation ready — bootstrap then user story implementation can begin

---

## Phase 2b: Bootstrap seed (P1) — before private-required activation

**Purpose**: Verified private seed of DEV-473’s own Phase A operational records so consumer cutover cannot self-break. Bulk historical archive/removals remain Phase 5 / US3 / Q8 stage 4.

- [ ] T011b After T003–T006 (and guard primitives T007b/T007c), seed DEV-473’s current Phase A operational records (`brief.md`, `plan.md`, `tasks.md`, checklists, `DRAFTING_RECEIPT.md`, `PLAN_ADJUDICATION.md`, and any other inventoried operational records under `specs/DEV-473/`) into verified private storage under logical `<private-root>/LamuFlix/DEV-473/` via `.specify/scripts/powershell/initialize-artifact-storage.ps1` bootstrap mode; verify raw-byte SHA-256 (P5). **No dual-read. No public-write fallback.** Must complete before T014–T018 private-required consumer activation.
- [ ] T011c Extend `.specify/scripts/powershell/Test-ArtifactStorage.ps1` to assert bootstrap seed verification and that private-required activation paths remain blocked until seed proof exists (disposable fixtures). Depends on T007c and T011b. Sequential companion edit — no `[P]`.

**Checkpoint**: In-flight ticket private seed verified; cutover may activate

---

## Phase 3: User Story 1 - Shared public/private resolution and private-root policy (Priority: P1) 🎯 MVP

**Goal**: Operators/scripts resolve public vs private paths under approved policy without polluting the public tree

**Independent Test**: Temporary-root regressions from T007/T007c plus public-only inspection and private-required failure cases in US1 acceptance scenarios

### Tests for User Story 1

> Sequential edits to `.specify/scripts/powershell/Test-ArtifactStorage.ps1` only — no `[P]`

- [ ] T008 [US1] Extend `.specify/scripts/powershell/Test-ArtifactStorage.ps1`: assert public-spec inspection succeeds without private storage and private-required stages fail closed without public recreation (depends on T011c)
- [ ] T009 [US1] Extend `.specify/scripts/powershell/Test-ArtifactStorage.ps1`: assert rejection of relative roots, in-repo/in-worktree roots, traversal, invalid IDs, symlink/junction escapes, and destination escapes (depends on T008)

### Implementation for User Story 1

- [ ] T010 [US1] Expose actionable missing/unwritable-root guidance from `.specify/scripts/powershell/common.ps1` / `.specify/scripts/powershell/initialize-artifact-storage.ps1` without legacy fallback to tracked `specs/`
- [ ] T011 [US1] Ensure `.specify/feature.json` remains portable with no machine-specific private root stored in Git; confirm selection precedence consumers use shared `Get-FeaturePathsEnv` only

**Checkpoint**: US1 independently testable

---

## Phase 4: User Story 2 - Coherent producer/consumer cutover for new tickets (Priority: P1)

**Goal**: New tickets and in-scope writers land operational records only under the private root after bootstrap; activation requires complete caller proof

**Independent Test**: Clean-checkout new-ticket creation and receipt/authoring/setup writes stay external; allowlist respected; single-writer coordination across real callers; guard allow path used by consumer routes; US2 incomplete until T014–T018 done

### Tests for User Story 2

> Sequential edits to `.specify/scripts/powershell/Test-ArtifactStorage.ps1` only — no `[P]`

- [ ] T012 [US2] Extend `.specify/scripts/powershell/Test-ArtifactStorage.ps1`: new-ticket create path writes operational artifacts under `<private-root>/<repository-id>/DEV-###/` and public allowlist only under `specs/DEV-###/` (depends on T009; disposable fixtures only)
- [ ] T013 [US2] Extend `.specify/scripts/powershell/Test-ArtifactStorage.ps1`: competing **real caller processes** against the same ticket fail closed under the shared ownership protocol (no automatic steal; explicit release); mutable outputs isolated by worktree/full SHA with SHA provenance — asserts FR-004/FR-005/P4 built by T004 and wired by T014–T018 (depends on T012)
- [ ] T013b [US2] Extend `.specify/scripts/powershell/Test-ArtifactStorage.ps1`: real consumer-route proof that sanctioned private writes succeed through the narrow guard allowance and denied routes still fail (depends on T013). Sequential companion edit — no `[P]`.

### Implementation for User Story 2

- [ ] T014 [US2] Cut over `.specify/scripts/powershell/create-new-feature.ps1` to shared public/private resolution and T004 FR-004/FR-005/P4 helpers (numbering/creation behaviour preserved without public operational writes; no silent overwrite)
- [ ] T015 [US2] Cut over `.specify/scripts/powershell/check-prerequisites.ps1` so stage detection distinguishes public vs private prerequisites without migration side effects
- [ ] T015b [US2] Cut over `.specify/scripts/powershell/setup-plan.ps1` and `.specify/scripts/powershell/setup-tasks.ps1` (and their authoring skill instructions) to shared `Get-FeaturePathsEnv` / private path contract; preserve feature-selection precedence (recon 49–52)
- [ ] T016 [US2] Update maintained host adapters in parity — `.agents/skills/task/SKILL.md`, `.agents/skills/grill-with-docs/SKILL.md`, `.agents/skills/speckit-specify/SKILL.md`, `.agents/skills/speckit-plan/SKILL.md`, `.agents/skills/speckit-tasks/SKILL.md`, `.agents/skills/pipeline/SKILL.md`, parallel `.claude/skills/**/SKILL.md` copies, plus any other inventoried adapters — to route operational writes/reads coherently via shared resolver and FR-004/FR-005/P4 helpers (P3 precision). **Do not invoke an absent host generator. Do not hand-edit actual generated outputs.**
- [ ] T017 [P] [US2] Update `scripts/new-mutation-receipt.ps1` and `scripts/load/README.md` for external receipts and operational references using logical identifiers only; wire SHA-isolated receipt paths via T004 helpers; **consume the one common resolver** (no harness/gate helper duplicate)
- [ ] T018 [US2] Cut over remaining inventoried in-scope authoring/review consumers from T001 together with T014–T017 (no split-brain writers; no tracked symlinks; no legacy public write fallback). Record the bounded discovered caller set and prove none retain public private-artifact writes across the **complete inventoried adapter set** (`.agents` and `.claude`) with no adapter drift (D4, P3). Route unrelated behaviour changes to Patron. **US2 / activation / delivery is incomplete until T014–T018 and this proof are done** (H3).

**Checkpoint**: US1 + US2 independently testable only after T014–T018 complete; new tickets write externally

---

## Phase 5: User Story 3 - Verified migration of existing operational records (Priority: P1)

**Goal**: Explicit verified bulk archive of currently tracked non-public records before public removals (Q8 stage 4; distinct from bootstrap)

**Independent Test**: Seeded sources/destinations prove inventory, non-clobber, SHA-256/count verify, idempotent identical rerun, blocking conflicts, FR-010 byte-identical private originals, allowlist exclusion from removal, historical allowlist end-state, P7 no synthesis, no prerequisite side-effect migration

### Tests for User Story 3

> Sequential edits to `.specify/scripts/powershell/Test-ArtifactStorage.ps1` only — no `[P]`

- [ ] T019 [US3] Extend `.specify/scripts/powershell/Test-ArtifactStorage.ps1`: copy-without-clobber; **raw-byte SHA-256** (P5, no text/newline normalization) and count verification; identical destination idempotent success; differing/absent destination blocks removal; **FR-010 regression**: after public sanitization publication, private unsanitized originals remain byte-identical; **FR-001**: seed `spec.md`, sanitized `CONCLUSIONS.md`, and applicable `ASSUMPTIONS.md` and assert they are excluded from removal candidates and preserved after migration (depends on T013b; disposable fixtures only)
- [ ] T020 [US3] Extend `.specify/scripts/powershell/Test-ArtifactStorage.ps1`: ordinary prerequisite/stage-detection scripts do not migrate as a side effect (depends on T019)
- [ ] T020b [US3] Extend `.specify/scripts/powershell/Test-ArtifactStorage.ps1`: historical per-ticket public directory end-state contains only the applicable public allowlist; tickets lacking public `CONCLUSIONS.md` do not receive synthesized conclusions (P7, FR-021/FR-022) (depends on T020). Sequential companion edit — no `[P]`.

### Implementation for User Story 3

- [ ] T021 [US3] Implement explicit bulk migration flow in `.specify/scripts/powershell/initialize-artifact-storage.ps1` (migration mode): private source/destination inventory → copy without clobber → verify raw-byte SHA-256 hashes/counts → stage Git removals only after verification. Actual private inventory writes require initialized validated storage from T003–T006. Distinct from bootstrap T011b.
- [ ] T022 [US3] Include historical `specs/DEV-*/` non-allowlist artifacts and `docs/reviews/` operational evidence identified by inventory; keep private unsanitized originals intact when publishing sanitized conclusions (FR-010); **sanitize only existing public `CONCLUSIONS.md`; never synthesize new historical conclusions** (P7); verify historical per-ticket allowlist end-state (FR-022)
- [ ] T023 [US3] Run verified bulk migration for in-scope records (including remaining DEV-473 operational Phase A records under the same policy after bootstrap); enumerate operational set including brief/plan/tasks/checklists/`DRAFTING_RECEIPT.md`/`PLAN_ADJUDICATION.md`; block removals on verification failure; do not rewrite Git history

**Checkpoint**: US3 independently testable; public removals only after verified copies

---

## Phase 6: User Story 4 - Scrub retained tracked machine paths (Priority: P1)

**Goal**: Retained tracked content passes exact ticket grep and slash-variant checks; scrubbed defaults preserve behaviour; adapters have no residual public-write instructions

**Independent Test**: Post-scrub exact ticket/slash checks return nothing outside `.gitignore`; harness worktreeRoot and Home defaults still resolve intended behaviour; inventoried adapters show no operational-public-write residual or drift

### Tests for User Story 4

- [ ] T024 [US4] Regression: exact ticket grep and case-insensitive forward/backslash variant checks from **private `recon-DEV-473` lines 19 and 30–31** are clean outside `.gitignore` after scrub (run from worktree root with `$env:PYTHONUTF8=1`). Do not embed machine-root literals in public task text beyond the private-recon reference.
- [ ] T024b [US4] Post-scrub behaviour proof (Sentry H2 / US4 scenario 4): using disposable fixtures and outcome checks only (no Home helper copy/dot-source into the companion), assert preserved harness `worktreeRoot` resolution behaviour after T026 and preserved in-scope `scripts/home/*.ps1` default behaviour after scrub. If implementation requires a new fact about expected behaviour, stop for recon instead of inventing it. Prefer companion extension when the check fits `.specify/scripts/powershell/Test-ArtifactStorage.ps1` (sequential — no `[P]`); otherwise record disposable manual outcome commands/exits.
- [ ] T024c [US4] Residual host-adapter proof: assert the complete inventoried `.agents`/`.claude` adapter set has no operational-public-write instructions and no adapter drift after T016/T018 (P3 precision / Sentry M3)

### Implementation for User Story 4

- [ ] T025 [P] [US4] Scrub retained prose/machine paths in `.specify/memory/constitution.md` and `README.md` to repository-relative or logical identifiers without constitution policy change (ticket-forced path/content cleanup preserving behaviour; Q5)
- [ ] T026 [P] [US4] Replace machine-path default in `harness.yml` `worktreeRoot` with runtime discovery or external configuration preserving intended behaviour (**worktreeRoot only**; do not edit `CodeMetricsConfig.txt`, `stryker-config.json`, or `.editorconfig`; do not invent/guess `install.ps1` invocation; stop if a generated-output change is required). Scrub `scripts/home/*.ps1` defaults similarly. Do not add a repository-id harness key; D2 remains env-only.
- [ ] T027 [US4] Scrub any remaining retained tracked matches found by T024 inventory; update maintained `.agents`/`.claude` adapters in parity as needed; **never hand-edit generated output**; STOP for Patron if a change would alter LocalPlay/`Process.Start` behaviour; STOP for supported source/tooling if a necessary generated change is encountered

**Checkpoint**: Ticket machine-path done criterion met for retained tracked files; behaviour regressions green

---

## Phase 7: User Story 5 - Clean-checkout and Phase A-to-B pickup smoke (Priority: P2)

**Goal**: End-to-end pickup, archive lifecycle, receipt readback, and named blocking final verification

**Independent Test**: Clean-checkout new ticket; Phase A-to-B across worktrees; delete worktree and confirm archives remain; absent-private fails closed; receipts include SHA readback — all in disposable fixtures; final verification records companion + ticket/slash + absent-private as distinct blocking checks

### Tests for User Story 5

- [ ] T028 [US5] Smoke in disposable fixtures only (never live operator archives): clean-checkout new-ticket external writes + public-only readability without private storage (document commands/exits/HEAD)
- [ ] T029 [US5] Smoke in disposable fixtures only: Phase A-to-B pickup across worktrees using canonical private durable records; **absent private inputs block pickup** without reconstructing approved receipts — record as a **named blocking check** with command, exit, and tree/HEAD (cannot be discharged by companion success alone)
- [ ] T030 [US5] Smoke in disposable fixtures only: private archives survive task worktree deletion; migration verification and gate receipts recorded with SHA provenance/readback under disposable private storage

### Implementation for User Story 5

- [ ] T031 [US5] Document operator init/backup/transfer expectations using logical identifiers only (no machine-local private path copied into tracked docs) in the cut-over skill/script help surfaces touched by T016–T017

**Checkpoint**: All user stories independently demonstrable

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Gate honesty, named blocking final verification, and draft-status hygiene

- [ ] T032 **Named blocking final verification — companion**: from repository root with `$env:PYTHONUTF8=1`, run `pwsh -NoProfile -File .specify/scripts/powershell/Test-ArtifactStorage.ps1`; record command, exit code, and reviewed tree/HEAD. Failure or Could not run blocks. Companion success does **not** discharge T033b/T033c.
- [ ] T033 Run applicable harness gates for this change set; record PASS / `SKIPPED (scope-empty)` / SKIP / N/A / Failure / Could not run distinctly; do not invent eligibility; do not substitute plain `dotnet build` for a required gate
- [ ] T033b **Named blocking final verification — exact ticket/slash scrub**: run the exact ticket machine-root grep and case-insensitive forward/backslash variant checks from private `recon-DEV-473` lines 19 and 30–31 outside `.gitignore`; record commands, exits, and tree/HEAD. Distinct from T032.
- [ ] T033c **Named blocking final verification — absent-private pickup**: rerun/record the absent-private pickup smoke (T029) with command, exit, and tree/HEAD as a distinct blocking check. Distinct from T032.
- [ ] T034 If any `.cs` files were unexpectedly touched, run `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (then `-Threshold 6` at refactor), `./scripts/run-jetbrains-inspectcode.ps1`, and `dotnet format --verify-no-changes`; otherwise record scope-empty honestly
- [ ] T035 Confirm no new ADR/CONTEXT vocabulary expansion was required; preserve existing glossary/ADR docs; state existing no-explanatory-comments convention was followed; **do not claim PLAN FROZEN, Gate 1, runtime PASS, or merge clearance from drafting alone**; keep two-round repair/review cap and all three axes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — starts immediately; T002 remains read-only annotation of `DRAFTING_RECEIPT.md`
- **Foundational (Phase 2)**: Depends on T001 confirmations — BLOCKS bootstrap and all user stories; T003–T006 must complete before any private-root writes
- **Bootstrap (Phase 2b)**: Depends on Foundational — BLOCKS private-required consumer activation (T014–T018)
- **US1 (Phase 3)**: Depends on Foundational (+ bootstrap for private-required cases)
- **US2 (Phase 4)**: Depends on Foundational + Bootstrap; uses US1 resolver contract and T004 helpers; **activation incomplete until T014–T018 done**
- **US3 (Phase 5)**: Depends on Foundational + cutover writers understanding private destinations (US2); bulk removals only here (stage 4)
- **US4 (Phase 6)**: Depends on US3 verified removals/staging for non-public records so scrub targets retained files only
- **US5 (Phase 7)**: Depends on US2–US4 behaviours being present
- **Polish (Phase 8)**: Depends on desired stories complete; includes named blocking final checks

### User Story Dependencies

- **US1 (P1)**: After Foundational — MVP resolver policy
- **US2 (P1)**: After Foundational + Bootstrap — independently testable via new-ticket writes only after T014–T018
- **US3 (P1)**: After private destinations exist (US2 cutover); bulk archive/removal stage
- **US4 (P1)**: After migration staging decisions (US3)
- **US5 (P2)**: After cutover + migration + scrub

### Companion test-file serialization (`.specify/scripts/powershell/Test-ArtifactStorage.ps1`)

T007 → T007c → T011c → T008 → T009 → T012 → T013 → T013b → T019 → T020 → T020b (and T024b if implemented as companion extension) — strictly sequential; no parallel markers on any companion-editing task

### Parallel Opportunities

- T002 (read-only DRAFTING_RECEIPT annotation) parallel with T001 research reads
- T016 may proceed alongside T015b only when file sets are disjoint and bootstrap T011b is complete; prefer sequencing adapters after setup cutover if unsure
- T017 parallel after T014–T015 land resolver calls (different files; not the companion test)
- T025/T026 parallel after T023 (different scrub surfaces)

---

## Parallel Example: User Story 2

```bash
# After Foundational + Bootstrap + create/prereq/setup cutover (T014-T015b):
# T017 may run in parallel with adapter parity work when files are disjoint:
Task: "Update scripts/new-mutation-receipt.ps1 and scripts/load/README.md"

# Companion test extensions remain sequential:
# T012 then T013 then T013b on .specify/scripts/powershell/Test-ArtifactStorage.ps1
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1–2 (inventory + resolver/init/guard + companion foundation)
2. Complete Phase 2b bootstrap seed
3. Complete Phase 3 (US1)
4. **STOP and VALIDATE** with `pwsh -NoProfile -File .specify/scripts/powershell/Test-ArtifactStorage.ps1`

### Incremental Delivery

1. US1 resolver policy
2. Bootstrap seed (P1) → then US2 coherent cutover → new tickets write externally (activation only when T014–T018 complete)
3. US3 verified bulk migration → public tree sheds operational records
4. US4 scrub + behaviour/residual proofs → ticket grep clean
5. US5 smoke (disposable fixtures) + named blocking final verification + gate receipts

### Suggested MVP scope

US1 + foundational resolver/init/guard + bootstrap seed (proves private root policy and in-flight seed before mass cutover/migration)

---

## Notes

- D1–D4 and P1–P7 / P3 precision are settled in `brief.md` / `CONCLUSIONS.md` / `DRAFTING_RECEIPT.md`; tasks use those exact rulings and do not invent alternatives
- Current `recon-DEV-473` lines 1–52 supply ticket/caller/guard/generator facts; historical D3 companion evidence remains brief D3 exchange; exact scrub literals stay private at recon lines 19 and 30–31
- Phase A `plan.md`/`tasks.md`/`brief.md`/`checklists/`/`DRAFTING_RECEIPT.md`/`PLAN_ADJUDICATION.md` under `specs/DEV-473/` must themselves bootstrap then migrate under US3
- Quill does not declare PLAN FROZEN or Gate 1
