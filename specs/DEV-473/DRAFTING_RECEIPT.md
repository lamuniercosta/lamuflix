# DEV-473 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-14
**Seat**: Quill
**Worktree**: task worktree `feature-473-spec` · branch `feature/473-spec` · HEAD `e06587031dae1d974a41ee98e7b23aa291107129`
**Method**: Phase 2 step 3.2 — `/speckit-specify`, `/speckit-plan`, `/speckit-tasks` from closed grill in one ask. `$env:PYTHONUTF8=1` set for the drafting shell. `.specify/feature.json` already pointed at `specs/DEV-473` and was left unchanged. `before_specify` grill hook treated as already satisfied by closed grill (`brief.md` Q1–Q9). `setup-plan.ps1 -Json` ran successfully; `setup-tasks.ps1 -Json` re-runnable after `spec.md` exists.

**Inputs**: grill-closed `specs/DEV-473/brief.md` (Q1–Q9 + D1–D4 + D3 resolved + P1–P7 + P3 precision), `CONCLUSIONS.md`, Maestri note `recon-DEV-473` lines 1–52 (ticket/caller/guard/generator; exact scrub literals private at lines 19 and 30–31), historical D3 companion evidence preserved in brief D3 exchange, Maestri note `DEV-473`, `specs/PRODUCT.md`, Keel `PLAN_ADJUDICATION.md` consolidated repair list round 1 of 2. No `ASSUMPTIONS.md` (brief: none required). Constitution cited only for plan gates; Quill did not edit constitution, ADRs, or production/test code.

**Artifacts written (Quill-owned)**:

- `specs/DEV-473/spec.md`
- `specs/DEV-473/plan.md`
- `specs/DEV-473/tasks.md`
- `specs/DEV-473/checklists/requirements.md`
- this receipt

**Not written**: `research.md`, `data-model.md`, `contracts/`, `quickstart.md` — inventable behaviour and contracts are frozen in brief Q1–Q9 / D1–D4 / P1–P7; no public API/schema design artifacts required.

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `PLAN_ADJUDICATION.md`, production/test code, `harness.yml`, generated OpenAPI/TS, ADRs, constitution, main checkout, Maestri standing notes.

**Clarify**: Session 2026-11-14 recorded in `spec.md`. No `[NEEDS CLARIFICATION]` markers for inventable behaviour. Interactive clarify not required — Q1–Q9, D1–D4, and P1–P7 freeze inventable boundaries.

**Owner checkbox**: none (`brief.md` Status; Q1–Q9; P2 no structural escalation; no ticket contradiction or constitution departure). Ordinary owner Gate 1 remains open and distinct from PLAN FROZEN.

**Decisions invented by Quill**: none. Behaviour and boundaries taken only from brief/CONCLUSIONS/recon/PRODUCT/P1–P7. No `needs decision:` lines for this repair round.

## Analysis repair round 1 (2026-11-14)

Applied Keel findings A1 / U1 / I1 / C1 / I2 after Patron D1–D4 and D3-resolved rulings in `brief.md`. No new fix list or round claimed. No PLAN FROZEN or Gate 1 claim.

### D1–D4 dispositions (A1 + U1)

| ID | Disposition | Settled value |
|----|-------------|----------------|
| D1 | Resolved (Patron) | Explicit init/migration command `.specify/scripts/powershell/initialize-artifact-storage.ps1` with explicit migration mode; extends shared `.specify/scripts/powershell/common.ps1`; no new layer/dependency |
| D2 | Resolved (Patron) | Optional portable repository slug is environment-only `LAMUFLIX_ARTIFACT_REPOSITORY_ID`, default `LamuFlix`; no new `harness.yml` key or second configuration surface |
| D3 | Resolved after targeted recon (Patron) | Standalone companion `.specify/scripts/powershell/Test-ArtifactStorage.ps1`; repo-root invocation `pwsh -NoProfile -File .specify/scripts/powershell/Test-ArtifactStorage.ps1`; strict errors; unique GUID temp fixtures; child-process real scripts with controlled environment; confinement and deterministic cleanup; no new dependency/project/helper layer; do not copy or dot-source Home domain stubs/helpers. Provenance: historical D3 companion evidence remains brief D3 complete exchange (original targeted lines 1–6 scope); current `recon-DEV-473` lines 1–52 cover ticket/caller/guard/generator |
| D4 | Resolved (Patron) | No closed Gate 1 caller filename list beyond recon evidence; T001 inventory and T018 cutover record the bounded discovered caller set and prove none retain public private-artifact writes; unrelated behaviour returns to Patron |

### Other finding dispositions

| Finding | Disposition |
|---------|-------------|
| I1 | Removed plan.md later-ask README restriction; ticket-forced machine-path scrub of retained tracked content (including `README.md`) authorized as path/content cleanup preserving behaviour (brief Q5) |
| C1 | Extended foundational task ownership in `common.ps1` for FR-004/FR-005 (canonical layout, SHA isolation/provenance, single-writer coordination); cutover tasks must wire callers; T013 asserts the behaviour |
| I2 | T002 is read-only location planning only; private-root writes depend on T003–T006 initialized validated storage; Q7 ordinary-I/O fail-closed retained |

## Consolidated plan repair round 1 of 2 (2026-11-14)

Applied Keel `PLAN_ADJUDICATION.md` ten-item Consolidated repair list. Binding decisions P1–P7 + P3 precision from `brief.md` / `CONCLUSIONS.md`. Current `recon-DEV-473` lines 1–52 used for ticket/caller/guard/generator facts. No implementation. No PLAN FROZEN / Gate 1 claim. No `needs decision:` outstanding.

### Repair mapping — all 21 findings

| Finding | Disposition in repair | Changed artifacts |
|---------|----------------------|-------------------|
| **Sentry H1** | Encoded P1: verified DEV-473 Phase A private bootstrap seed after foundation (T003–T006) and before private-required activation (T014–T018); no dual-read/public-write fallback; bulk removals remain stage 4 / US3 (T011b/T011c; FR-019; Approach step 3) | `spec.md`, `plan.md`, `tasks.md` |
| **Sentry H2** | Added post-scrub behaviour proof for harness `worktreeRoot` and in-scope Home defaults via disposable fixtures / T024b; no Home helper copy into companion; stop for recon if new behaviour fact needed | `spec.md` (US4/SC-008), `plan.md`, `tasks.md` (T024b) |
| **Sentry H3** | T014–T018 completion + inventoried caller proof gate US2 activation/delivery; partial/parallel work cannot count as US2 completion (US2 scenario 7; FR-012; Phase 4 checkpoint) | `spec.md`, `plan.md`, `tasks.md` |
| **Sentry M1** | T019 seeds allowlist files and asserts exclusion from removal candidates + preservation after migration; FR-001/FR-022 | `spec.md` (US3 sc.6), `tasks.md` (T019) |
| **Sentry M2** | P4 one shared ownership contract in T004; T013 asserts competing **real caller processes**; fail closed; no automatic steal; explicit release | `spec.md` (FR-005), `plan.md`, `tasks.md` (T004/T013) |
| **Sentry M3** | P3 precision: maintained `.agents`/`.claude` updated in parity; T018 residual public-write proof across complete adapter set; no unavailable-generator requirement | `spec.md` (FR-014), `plan.md`, `tasks.md` (T016/T018/T024c) |
| **Sentry M4** | Named blocking final checks: companion (T032), exact ticket/slash (T033b), absent-private (T033c) each with command/exit/tree/HEAD; companion cannot substitute | `spec.md` (FR-017/US5 sc.6), `plan.md`, `tasks.md` |
| **Sentry L1** | P5 raw-byte SHA-256, no text/newline normalization (FR-009; T019/T021) | `spec.md`, `plan.md`, `tasks.md` |
| **Sentry L2** | P6 absent/empty/whitespace → default `LamuFlix`; nonempty invalid reject (FR-004; T006/T007) | `spec.md`, `plan.md`, `tasks.md` |
| **Ledger H1** | P2 narrow `guard.ps1` Deny-OutsideBoundary extension; allow/deny regressions T007b/T007c/T013b; no shell bypass | `spec.md` (FR-020/US2 sc.6), `plan.md`, `tasks.md` |
| **Ledger M1** | Explicitly named `setup-plan.ps1` / `setup-tasks.ps1`, authoring skills, `Get-FeaturePathsEnv`, selection precedence from recon 49–52 in T001/T015b/T018 | `spec.md` (scope 6/FR-012), `plan.md`, `tasks.md` |
| **Ledger M2** | Explicit blocking companion at final verification T032 with exit/tree/HEAD; D3 path/rationale preserved | `spec.md`, `plan.md`, `tasks.md` (T032) |
| **Ledger L1** | Removed unavailable host-generator/regeneration-command claims; P3 precision + T026 worktreeRoot-only; stop if generated change needed | `spec.md`, `plan.md`, `tasks.md` (T016/T026/T027) |
| **Ledger L2** | D2 env exception ratified; one shared resolver; receipt tooling consumes common resolver (T017); no harness/gate duplicate | `spec.md` (FR-006), `plan.md`, `tasks.md` |
| **Ledger L3** | Enumerated operational records including brief/plan/tasks/checklists/DRAFTING_RECEIPT/PLAN_ADJUDICATION (T023); no-explanatory-comments stated; draft status not frozen | `spec.md` (scope 2), `plan.md`, `tasks.md` (T023/T035) |
| **Compass H1** | Explicit FR-010 regression: private originals remain byte-identical after sanitization (US3 sc.5; T019; SC-007) | `spec.md`, `tasks.md` (T019) |
| **Compass M1** | Re-pinned provenance to recon lines 1–52; exact scrub literals referenced only via private lines 19 and 30–31; no public machine-root literal reintroduction | `spec.md`, `plan.md`, `tasks.md`, this receipt |
| **Compass M2** | P3 precision: maintained host adapters parity edits; no generated-file waiver; residual adapter proof T024c | `spec.md`, `plan.md`, `tasks.md` |
| **Compass L1** | T002 pins logical location-plan annotation to existing Phase A artifact `specs/DEV-473/DRAFTING_RECEIPT.md` (section below); no private write before init | `tasks.md` (T002), this receipt |
| **Compass L2** | Positive historical allowlist end-state check (FR-022; T020b/T022) | `spec.md`, `tasks.md` |
| **Compass L3** | P7 sanitize existing public conclusions only; never synthesize historical conclusions (FR-021; T020b/T022) | `spec.md`, `plan.md`, `tasks.md` |

### Ten consolidated repair items → coverage

| # | Item | Coverage |
|---|------|----------|
| 1 | Re-pin provenance and regeneration feasibility | Input/Clarifications/Assumptions updated; P3 maintained adapters; T026 worktreeRoot-only; historical D3 evidence preserved; scrub literals private-only |
| 2 | Bootstrap and coherent activation | Phase 2b T011b/T011c; FR-019; US2 activation gate on T014–T018; five-stage Q8 distinction bootstrap vs bulk |
| 3 | Guarded private writes | FR-020; T007b/T007c/T013b; allow/deny; no shell bypass |
| 4 | Confirmed setup and selection consumers | T001/T015b/FR-006; setup-plan/tasks; Get-FeaturePathsEnv precedence; T017 common resolver |
| 5 | Shared writer and edge contracts | T004/T006/T013 P4–P6; SHA-256; real caller processes |
| 6 | Migration safety acceptance | T019 FR-010 + allowlist; T020b/T022/T023 P7 + historical end-state + operational enumeration |
| 7 | Post-scrub behaviour proof | T024b US4 harness worktreeRoot + Home defaults |
| 8 | Host-adapter residual proof | T016/T018/T024c parity + residual; no generator requirement |
| 9 | Named blocking verification | T032/T033b/T033c; companion serialization chain extended |
| 10 | Draft hygiene and T002 output | T002 → DRAFTING_RECEIPT; no-explanatory-comments; status Draft only |

### Exact changed artifacts this round

- `specs/DEV-473/spec.md`
- `specs/DEV-473/plan.md`
- `specs/DEV-473/tasks.md`
- `specs/DEV-473/DRAFTING_RECEIPT.md` (this file)
- `specs/DEV-473/checklists/requirements.md`

### T002 location-plan annotation target (Compass L1)

**Logical private migration inventory path (annotation only — no directories or private writes until T003–T006):** `<private-root>/LamuFlix/DEV-473/` under resolved `LAMUFLIX_ARTIFACT_ROOT` or LocalApplicationData/`LamuFlix/artifacts`. Authoring/check commands use `$env:PYTHONUTF8=1`. Actual initialized private inventory writes remain T021 (bulk) / T011b (bootstrap), not T002.

## Phase 1 Setup — implementation record (T001–T002)

Read-only inventory and annotation performed by Anvil in worktree `F:/Dev/LamuFlix.worktrees/DEV-473`, branch `feature/DEV-473`, HEAD `22261b356b8aec7f06b00380d42f4dbf2b755d28`. No file outside this receipt was modified; no private-root directory was created or written.

### T002 — location-plan annotation (read-only)

- **Intended private migration inventory path (logical):** `<private-root>/LamuFlix/DEV-473/`, i.e. `<private-root>/<repository-id>/DEV-###/` with the default repository id `LamuFlix` (D2/P6). `$env:LAMUFLIX_ARTIFACT_REPOSITORY_ID` is not set in this shell, so the default applies.
- **Root resolution intent:** absolute `LAMUFLIX_ARTIFACT_ROOT` override when present, otherwise LocalApplicationData/`LamuFlix/artifacts` (spec Scope item 4). No machine-local literal is recorded here.
- **`$env:PYTHONUTF8=1`** confirmed set for subsequent authoring/check commands in this worktree.
- **Not done, by design:** no directory creation, no write, no read under the private root. Ordinary I/O stays fail-closed until T003–T006 initialize and validate storage (Q7). Actual private inventory writes remain T011b (bootstrap) and T021 (bulk).

### T001 — bounded discovered caller set (D4)

Evidence gathered by reading tracked sources in this worktree. Paths are repository-relative; line numbers are as of the HEAD above.

**Shared resolver (the one place all cutover writers must route through)**

| Surface | Evidence |
|---|---|
| `Get-FeaturePathsEnv` | `.specify/scripts/powershell/common.ps1:284` (precedence at `:290-319`: explicit `SPECIFY_FEATURE_DIRECTORY` → `.specify/feature.json` `feature_directory` → numeric branch prefix) |
| `Test-FeatureJsonMatchesFeatureDir` | `common.ps1:169` |
| `Find-FeatureDirByPrefix` | `common.ps1:236` |
| `Get-FeatureDirFromBranchPrefixOrExit` | `common.ps1:271` |
| `Test-FeatureBranch` | `common.ps1:140` |

No public/private path split, private-root resolution, confinement, ownership/lock, or SHA-isolation helper exists today. `Get-FeaturePathsEnv` returns only public `specs/`-derived paths (`common.ps1:320-333`). Confirms D1–D4 and P1–P7 are open work, not existing behaviour.

**Confirmed consumers of `Get-FeaturePathsEnv`**

| Consumer | Evidence | Current write behaviour |
|---|---|---|
| `check-prerequisites.ps1` | `:57` dot-source, `:60` call, `:62` branch check | read-only stage detection |
| `setup-plan.ps1` | `:21` dot-source, `:24` call, `:27-28` branch check, `:34` `New-Item -Directory`, `:42` `WriteAllText`, `:46` `New-Item` | writes `plan.md` into the public feature dir |
| `setup-tasks.ps1` | `:17` dot-source, `:20` call, `:23-24` branch check | reads `plan.md`/`spec.md`, no artifact write |

**Producers**

| Producer | Evidence | Note |
|---|---|---|
| `create-new-feature.ps1` | `:177` `specsDir`, `:179` `New-Item -Directory`, `:290-291` `featureDir`/`specFile`, `:348` `New-Item -Directory`, `:350-358` spec write, `:363` sets `$env:SPECIFY_FEATURE` | writes public `specs/<branch>/spec.md`; numbering from local dirs, local branches, and `ls-remote` (`:53-160`) |
| `.agents/skills/speckit-specify/SKILL.md` / `.claude` twin | instruction block: "Specs live under the default `specs/` directory…", "Create the directory and spec file", "Persist the resolved path to `.specify/feature.json`" | the agent-facing producer that instructs the public write and the `feature.json` pin |
| `.agents/skills/speckit-plan/SKILL.md` / `.claude` twin | `:56` runs `setup-plan.ps1 -Json` | authoring skill for the setup-plan producer |
| `.agents/skills/speckit-tasks/SKILL.md` / `.claude` twin | `:56` runs `setup-tasks.ps1 -Json` | authoring skill for the setup-tasks producer |

No tracked script invokes `create-new-feature.ps1`: repository-wide grep finds it only in its own help text (`:20-40`) and in `.specify/integrations/speckit.manifest.json:8` (an upstream hash manifest, not a caller). Its only real driver is the `speckit-specify` skill instruction above. Inventory recorded this instead of inventing a script caller.

**Adapters and other consumers referencing in-scope surfaces**

- `check-prerequisites.ps1` referenced by 11 adapters, mirrored in both trees: `code-review`, `gherkin`, `implement`, `pipeline`, `remediate`, `ship-review`, `speckit-analyze`, `speckit-checklist`, `speckit-clarify`, `speckit-implement`, `speckit-taskstoissues` (`.agents/skills/<name>/SKILL.md` and `.claude/skills/<name>/SKILL.md`).
- `setup-plan.ps1` referenced by `speckit-plan`; `setup-tasks.ps1` by `speckit-tasks`.
- Adapters naming `specs/<feature>/…` paths with operational content: `task` (`:47`, `:94`, `brief.md`), `grill-with-docs` (`:9-14`, `CONCLUSIONS.md`), `handoff` (`:12`), `web-implement` (`:10`, `:15`, `:37`), `test-engineer` (`:25`), `scaffold` (`:54`), `architect` (`:21`), `gherkin` (`:21-22`), `code-review` (`:40`, `:168-171`).
- Adapter trees are 90 files each with no file-set difference; 31 files differ in content, and the sampled difference (`speckit-plan`: `argument-hint`, `user-invocable`, `disable-model-invocation` front matter, and `CLAUDE.md` vs `AGENTS.md` context-file wording) is pre-existing host-specific drift, not ticket-caused. Recorded for T018/T024c drift proof; not corrected here.

**Receipt and docs consumers**

| Surface | Evidence |
|---|---|
| `scripts/new-mutation-receipt.ps1` | `:23` mandatory `-ReceiptDir`, `:17` usage example `specs/task/receipts`, `:323` repo-relative join — public receipts today; does not consume the common resolver |
| `scripts/load/README.md` | `:4`, `:230` reference `specs/DEV-427/` |
| `scripts/new-task-branch.ps1` | `:202-245` consumes `task.worktreeRoot` with sibling `<repo>.worktrees` default — the T026 scrub's behaviour-preservation target |

**Guard**

- `scripts/hooks/guard.ps1`: `Deny-ProtectedPath` `:85`, `Get-WorktreeBoundary` `:133`, `Deny-OutsideBoundary` `:161`, applied at `:474-475` (Edit/Write/MultiEdit) and `:483-484` (apply_patch). No private-root allowance exists; the P2 extension is new work. Existing regressions live in `scripts/hooks/Test-Guard.ps1` (185 lines) and are separate from the D3 companion.

**Generated / installer-rendered outputs (must stay untouched, P3 precision)**

`CodeMetricsConfig.txt`, `stryker-config.json`, `.editorconfig` gate severities — unchanged and out of scope. No host-skill generator and no runnable `install.ps1` exists in this worktree, matching `recon-DEV-473` lines 38–40.

**Additional observed facts (recorded, not acted on)**

- `.specify/feature.json` is git-ignored (`.gitignore:508`) and absent in this worktree, so selection falls through to the numeric branch prefix — and `check-prerequisites.ps1 -Json -PathsOnly` exits 1 here with `Not on a feature branch. Current branch: feature/DEV-473`. That is the FR-006 portability gap, not a Phase 1 defect.
- `.specify/integrations/speckit.manifest.json` records SHA-256 for the five in-scope scripts and five templates; all ten hashes drift from the working tree at this HEAD. Upstream runtime state, not a ticket deliverable.
- `.specify/extensions.yml` registers only `before_specify`, `after_tasks`, `after_implement` hooks; none touches private storage.

### Settled inventables confirmed

D1 (explicit `.specify/scripts/powershell/initialize-artifact-storage.ps1` with migration mode), D2 (environment-only `LAMUFLIX_ARTIFACT_REPOSITORY_ID`, default `LamuFlix`, no harness key), D3 (standalone `.specify/scripts/powershell/Test-ArtifactStorage.ps1`, repo-root `pwsh -NoProfile -File` invocation; historical D3 companion evidence remains the brief D3 exchange), D4 (no closed Gate 1 caller list; the set above is the bounded discovery), and P1–P7 with P3 precision all stand as written. No alternate names, projects, generators, or dependencies were introduced, and no machine-root literal was written into this artifact.

## Notes

- Phase A docs remain at Conductor-required `specs/DEV-473/` paths; implementation must bootstrap then migrate this ticket’s operational records under the same policy (brief Handoff; FR-019; P1).
- Independent Keel read-only reread follows this repair (round 1 of 2); Quill does not invent PLAN FROZEN or Gate 1.
- No production C# or web behaviour change planned; applicable scope-empty gates stay non-PASS reports.
- Machine-root literals remain only in private recon evidence (lines 19 and 30–31).
