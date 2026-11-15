# Feature Specification: Move spec evidence and receipts out of the public repo and scrub local paths

**Feature Branch**: `feature/473-spec`
**Created**: 2026-11-14
**Status**: gate1: provisional
**Input**: Ticket DEV-473 (size:M, parent DEV-492); grill-closed `brief.md` (Q1–Q9 + D1–D4 + D3 resolved + P1–P7 + P3 precision); `CONCLUSIONS.md`; Maestri note `recon-DEV-473` lines 1–52 (ticket/caller/guard/generator facts; exact scrub literals remain private at lines 19 and 30–31); historical D3 companion-test evidence preserved in brief D3 complete exchange (original targeted lines 1–6 scope); Maestri note `DEV-473`; `specs/PRODUCT.md`. No `ASSUMPTIONS.md`. Intake HEAD `e06587031dae1d974a41ee98e7b23aa291107129`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Status; Q1–Q9; P2 no structural escalation; `PRODUCT.md` §5). Ordinary owner Gate 1 on the later spec PR remains separate from this draft. Quill does not invent PLAN FROZEN or a structural checkbox.

## Clarifications

### Session 2026-11-14

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for inventable behaviour? → A: Public/private allowlist, private-root resolution and failure policy, ticket identity/concurrency/pickup, migration safety, retained-file scrubbing, producer/consumer cutover, initialization/confinement/lifecycle, implementation order, test/gate expectations, and loop terms are frozen in brief Q1–Q9. Drafting decisions D1–D4 (including D3 resolved after targeted recon) settle the explicit init/migration command `.specify/scripts/powershell/initialize-artifact-storage.ps1` (migration mode), environment-only slug `LAMUFLIX_ARTIFACT_REPOSITORY_ID` (default `LamuFlix`, no harness key), standalone companion `.specify/scripts/powershell/Test-ArtifactStorage.ps1` invoked `pwsh -NoProfile -File .specify/scripts/powershell/Test-ArtifactStorage.ps1`, and no closed Gate 1 caller filename list (T001/T018 record the bounded discovered set). Plan adjudication P1–P7 plus P3 precision settle bootstrap seed before private-required activation, narrow private-ticket guard extension with allow/deny regressions, maintained `.agents`/`.claude` SKILL.md parity edits (no absent-host-generator requirement; no generated-file waiver), one shared writer ownership contract, raw-byte SHA-256, absent/empty/whitespace repository-id defaulting, and sanitize-existing-only historical conclusions. Current `recon-DEV-473` lines 1–52 restore ticket Wanted/Done-when, scrub-literal private evidence, generator absence, guard denial, and confirmed setup/selection consumers. Historical D3 companion evidence remains the brief D3 exchange. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## Scope

**In scope** (`brief.md` Frozen scope; Q1–Q9; P1–P7; ticket Wanted/Done-when at `recon-DEV-473` lines 8–25):

1. **Public per-ticket allowlist**: `specs/DEV-###/spec.md`, sanitized `CONCLUSIONS.md`, and `ASSUMPTIONS.md` when applicable. Preserve product, glossary, and ADR documentation. Historical migration sanitizes existing public `CONCLUSIONS.md` only and MUST NOT synthesize new historical conclusions (P7).
2. **Private operational records**: briefs, plans, tasks, checklists, evidence, recon, findings, receipts, raw exchanges, drafting receipts, plan adjudications, and other operational/workflow records — including historical records and operational records outside `specs/` when the migration inventory identifies them. This ticket’s own Phase A set enumerates at least `brief.md`, `plan.md`, `tasks.md`, checklists, `DRAFTING_RECEIPT.md`, and `PLAN_ADJUDICATION.md`.
3. **Sanitization vs archive**: Public conclusions keep complete decision substance and cited rationale with portable source identifiers; private archives keep original unsanitized bytes (FR-010). Do not copy a machine-local private storage path into tracked documentation. Exact machine-root grep pattern and slash-variant spellings live only in private `recon-DEV-473` lines 19 and 30–31; retained public artifacts reference that private evidence without reintroducing the literal.
4. **Private root**: Optional absolute override `LAMUFLIX_ARTIFACT_ROOT`; otherwise platform LocalApplicationData plus `LamuFlix/artifacts`. Canonical ticket directory `<private-root>/<repository-id>/DEV-###/` (default repository-id `LamuFlix`). Optional portable slug override is environment-only `LAMUFLIX_ARTIFACT_REPOSITORY_ID` (D2). Absent, empty, or whitespace-only values default to `LamuFlix`; nonempty invalid slugs are rejected (P6).
5. **Identity and concurrency**: Canonical brief/plan/tasks survive branch/worktree transitions. Mutable run output and receipts are isolated by worktree or full SHA; all gate/review evidence carries SHA provenance. One shared per-ticket exclusive ownership contract across all real cutover writers: fail closed on competing writers, no automatic lock stealing, explicit release; concrete lock primitive may be chosen in `common.ps1` under that identical contract (P4).
6. **Shared resolution**: Distinct public and private paths through one shared resolver in `.specify/scripts/powershell/common.ps1` (`Get-FeaturePathsEnv` and related helpers). Confirmed consumers include `check-prerequisites.ps1`, `setup-plan.ps1`, and `setup-tasks.ps1` with feature-selection precedence: explicit `SPECIFY_FEATURE_DIRECTORY`, then `.specify/feature.json` `feature_directory`, otherwise numeric branch-prefix resolution (`recon-DEV-473` lines 49–52). Public `.specify/feature.json` stays portable. Public-spec inspection works without private storage; workflow stages that require private inputs fail when those inputs are absent. Receipt tooling and harness/gate helpers MUST consume the one common resolver rather than duplicate it (D2 ratification; Ledger L2).
7. **Confinement**: Reject relative roots, effective roots inside any repository/worktree, traversal, invalid IDs, symlink/junction escapes, and destination escapes. No tracked private-storage symlinks and no public legacy-write fallback.
8. **Guarded private writes**: Narrow confined extension to `scripts/hooks/guard.ps1` (`Deny-OutsideBoundary`) permitting Edit/Write/apply-patch only to normalized, validated private artifact paths under the approved external root/`<repository-id>/DEV-###/` layout, while preserving protected-path checks and all unrelated boundary denials, with allow/deny regressions. No shell-mediated bypass and no broad blanket allow (P2; `recon-DEV-473` lines 44–45).
9. **Initialization, bootstrap, and migration**: Explicit initialization/migration may create the root. Ordinary reads/writes fail with actionable setup guidance on missing/unwritable root. **P1 bootstrap**: after foundational resolver/init (T003–T006) and before private-required consumer activation (T014–T018), seed DEV-473’s own current Phase A operational records into verified private storage; no dual-read and no public-write fallback. **Bulk historical archive/removals remain stage 4** (US3 / Q8 stage 4). Migration is an explicit operator action (inventory → copy without clobber → verify raw-byte SHA-256 hashes and counts → only then stage Git removals). Already-identical destinations succeed on rerun; differing files, absent destinations, or failed verification block removal. History rewrite is out of scope. Keep private originals intact when publishing sanitized conclusions (FR-010).
10. **Archive lifecycle**: Operator owns backup and cross-machine transfer. Archive retention is independent of branch/worktree cleanup. Missing evidence is never reconstructed as an approved historical receipt.
11. **Producer/consumer cutover**: Shared resolver plus all in-scope producers/consumers named by `recon-DEV-473` (create/setup/prerequisite/setup-plan/setup-tasks, named skills and parallel `.claude` adapters, mutation receipt tooling, load docs) and callers found within ticket scope during implementation inventory, cut over together. T014–T018 completion and all inventoried caller proof gate activation/delivery; partial commits or parallel work MUST NOT count as US2 completion (P1/H3). No closed Gate 1 filename list; T001 inventory and T018 cutover record the bounded discovered caller set and prove none retain public private-artifact writes (D4).
12. **Host adapters and scrub**: Tracked `.agents/skills/*/SKILL.md` and `.claude/skills/*/SKILL.md` are maintained editable repository sources updated in parity — not generated output (P3 precision; `recon-DEV-473` lines 38–40). Actual generated or `install.ps1`-rendered outputs (`CodeMetricsConfig.txt`, `stryker-config.json`, `.editorconfig` gate severities) remain untouched; if implementation requires a generated-output change whose authoritative source or supported installer is absent, work stops rather than hand-editing. T026 changes `harness.yml` `worktreeRoot` only; no threshold-output edits and no guessed installer invocation. All retained tracked files outside `.gitignore` satisfy the exact ticket grep and case-insensitive forward/backslash variant checks recorded in private recon evidence. Prose uses repository-relative paths or logical identifiers. Executable/config defaults use runtime discovery or external configuration while preserving intended behaviour, with post-scrub behaviour proof for harness `worktreeRoot` and in-scope Home defaults. Path/content cleanup only — not a constitution policy change and not authorization to alter LocalPlay/process behaviour.
13. **Verification**: Five-stage order and regression matrix from brief Q8 / Task order, with explicit distinction between bootstrap copy verification and later bulk migration/removal. Standalone PowerShell 7 companion `.specify/scripts/powershell/Test-ArtifactStorage.ps1` (repo-root `pwsh -NoProfile -File .specify/scripts/powershell/Test-ArtifactStorage.ps1`) covers temporary-directory regressions with strict errors, unique GUID temp fixtures, child-process real scripts under controlled environment, confinement and deterministic cleanup; no new test dependency/project/helper layer and no Home domain stubs/helpers (D3). Every companion edit is serialized. Final verification MUST explicitly run the companion plus exact ticket/slash checks and absent-private pickup smoke with commands, exits, and tree/HEAD recorded; companion success cannot substitute for those distinct acceptance checks. Smoke uses disposable fixtures, never live operator archives. Applicable harness gates at the correct stage; failures and Could not run block. Scope-empty results remain `SKIPPED (scope-empty)`, never PASS; harness-disabled opt-outs remain SKIP; mutation exclusions remain N/A as reported by their scripts. Existing no-explanatory-comments convention applies to implementation artifacts.
14. **This ticket’s own Phase A records**: Authored at currently required `specs/DEV-473/` paths; implementation must migrate its operational records under the same policy as other tickets. Do not claim the external storage cutover already exists.

**Out of scope** (`brief.md` Frozen scope; Q9):

- Git history rewrite (unless owner later asks).
- Application features, public API/schema changes, production C# or web behaviour changes.
- Hosted archive service or new NuGet/npm dependency.
- New project, top-level folder, or architectural layer.
- Constitution policy change; LocalPlay / `Process.Start` behaviour change.
- Reconstructing missing private evidence as approved historical receipts.
- Synthesizing new historical `CONCLUSIONS.md` where none exist (P7).
- Hand-editing generated/`install.ps1`-rendered outputs; inventing a host-skill generator or speculative installer command (P3 precision).
- Shell bypass of the write guard (P2).
- Anything else: follow-up issue, not a finding in this round.

**Precedence**: Ticket Wanted/Done-when is already approved authority (`PRODUCT.md` §3; `recon-DEV-473` lines 8–25). Brief Q1–Q9, D1–D4, P1–P7 / P3 precision, and `CONCLUSIONS.md` govern inventable boundaries. Ordinary owner Gate 1 remains pending. Quill does not invent PLAN FROZEN.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md or ADR expansion)*

**Public allowlist**
Per-ticket tracked artifacts limited to `spec.md`, sanitized `CONCLUSIONS.md`, and `ASSUMPTIONS.md` when applicable, plus preserved product/glossary/ADR docs.
_Avoid_: treating briefs, plans, tasks, checklists, evidence, receipts, drafting receipts, or adjudications as public

**Private artifact root**
Resolved absolute storage for operational records: `LAMUFLIX_ARTIFACT_ROOT` when set and valid, otherwise platform LocalApplicationData plus `LamuFlix/artifacts`.
_Avoid_: a path inside the Git checkout or any task worktree; relative roots

**Canonical ticket directory**
`<private-root>/<repository-id>/DEV-###/` holding durable operational records for one ticket (default repository-id `LamuFlix`; optional env-only override `LAMUFLIX_ARTIFACT_REPOSITORY_ID`; absent/empty/whitespace → default; nonempty invalid → reject).
_Avoid_: machine checkout path as identity; silent shared mutable run folders; harness.yml repository-id key

**Verified migration**
Explicit operator action that inventories, copies without clobbering, verifies raw-byte SHA-256 hashes and counts (no text/newline normalization), then stages public Git removals only after verification succeeds.
_Avoid_: automatic side effect of prerequisite/read scripts; unverified deletion; dual-read/public-write fallback

**Bootstrap seed**
Verified private copy of the in-flight ticket’s own Phase A operational records after foundation and before private-required consumer activation; distinct from stage-4 bulk historical archive/removal.
_Avoid_: treating partial cutover as activation; dual-read exceptions

Consumed vocabulary (already in use; not redefined): feature directory, `.specify/feature.json`, numeric branch detection, harness gates, SHA provenance, worktree.

## What the User Will See *(mandatory)*

Nothing visible: this ticket changes pipeline/tooling artifact locations, scripts, skills, and tracked documentation paths. The running Compose streaming app UI is unchanged.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Shared public/private resolution and private-root policy (Priority: P1)

An operator or pipeline script resolves distinct public and private artifact paths for a ticket, using the approved private-root rules, confinement, repository-id defaults, shared ownership helpers, and failure behaviour, without writing private records into the public tree.

**Why this priority**: Every later cutover, migration, and pickup depends on one shared resolution contract (brief Q2, Q3, Q6, Q7; P4–P6).

**Independent Test**: Against a temporary private root (default and override), assert public vs private path distinction, rejection of relative/in-repo/in-worktree/traversal/symlink escapes and invalid IDs, absent/empty/whitespace repository-id defaulting and nonempty invalid rejection, missing/unwritable root failures with actionable guidance, public-only reads without private storage, and no legacy public write fallback.

**Acceptance Scenarios**:

1. **Given** no `LAMUFLIX_ARTIFACT_ROOT`, **When** resolution runs, **Then** the private root is platform LocalApplicationData plus `LamuFlix/artifacts` and the canonical ticket directory is `<private-root>/LamuFlix/DEV-###/`.
2. **Given** a valid absolute `LAMUFLIX_ARTIFACT_ROOT` outside every checkout/worktree, **When** resolution runs, **Then** that override is used and relative or in-repo/in-worktree roots are rejected.
3. **Given** a missing or unwritable private root during ordinary read/write, **When** a workflow operation that needs private storage runs, **Then** it fails with actionable initialization guidance and does not fall back to tracked `specs/`.
4. **Given** only public allowlist files present, **When** a read-only public-spec inspection runs, **Then** it succeeds without requiring private storage.
5. **Given** a workflow stage that requires private inputs, **When** those inputs are absent, **Then** the stage fails clearly and does not recreate the private inputs publicly.
6. **Given** traversal segments, invalid ticket/repository IDs, symlink/junction targets into a checkout, or destination escape, **When** resolution or write is attempted, **Then** the operation is rejected.
7. **Given** absent, empty, or whitespace-only `LAMUFLIX_ARTIFACT_REPOSITORY_ID`, **When** resolution runs, **Then** repository-id is `LamuFlix`; nonempty invalid slugs are rejected (P6).

---

### User Story 2 - Coherent producer/consumer cutover for new tickets (Priority: P1)

After verified bootstrap of this ticket’s Phase A private seed and complete cutover of all inventoried callers, creating and authoring a new ticket writes operational records only under the private root, keeps public allowlist files in `specs/DEV-###/`, preserves portable `.specify/feature.json` and numeric branch detection, coordinates same-ticket writers without silent overwrite, and routes sanctioned private writes through the narrow guard extension.

**Why this priority**: Ticket done criterion requires new tickets to write evidence/receipts at the external location (`recon-DEV-473` lines 14–20; brief Q6; P1/P2).

**Independent Test**: From a clean checkout with initialized private storage and verified DEV-473 bootstrap seed, run new-ticket creation and in-scope authoring/receipt/setup writers; assert private writes stay external, public allowlist is respected, feature.json stays portable, concurrent same-ticket write without coordination fails closed, guard allow/deny regressions pass, and US2 is not marked complete until T014–T018 plus inventoried caller proof are done.

**Acceptance Scenarios**:

1. **Given** initialized private storage, verified DEV-473 Phase A private seed, and a clean checkout, **When** a new ticket is created through the cut-over create/setup path, **Then** operational records land under `<private-root>/LamuFlix/DEV-###/` and public `specs/DEV-###/` holds only allowlisted files.
2. **Given** cut-over skills/scripts/receipt/setup tooling (including `setup-plan.ps1` / `setup-tasks.ps1` and parallel `.agents`/`.claude` adapters), **When** they write briefs, plans, tasks, checklists, evidence, or receipts, **Then** writes use private paths and never use a public legacy-write fallback or tracked symlink to private storage.
3. **Given** public `.specify/feature.json` and a numeric feature branch, **When** feature selection runs through the shared `Get-FeaturePathsEnv` precedence, **Then** selection remains portable (no machine-specific root stored in Git) and numeric branch detection still works.
4. **Given** two real cutover writers targeting the same ticket’s canonical durable files, **When** coordination is absent, **Then** the shared ownership protocol fails closed without automatic lock stealing rather than silently overwriting (P4).
5. **Given** mutable run output or gate/review evidence, **When** it is written, **Then** it is isolated by worktree or full SHA and carries SHA provenance.
6. **Given** an Edit/Write/apply-patch targeting a normalized validated private ticket path under the approved external root, **When** the guard evaluates, **Then** the write is allowed; paths outside that confined set, protected paths, and unrelated boundary cases remain denied; shell bypass is not a sanctioned route (P2).
7. **Given** incomplete T014–T018 or missing inventoried caller proof, **When** delivery or US2 completion is considered, **Then** activation is blocked — partial/parallel work is not an accepted cutover (H3).

---

### User Story 3 - Verified migration of existing operational records (Priority: P1)

An operator runs the explicit migration action to inventory and archive currently tracked non-public operational records (including historical tickets and operational evidence elsewhere identified during inventory), verifies copies with raw-byte SHA-256, then stages public removals only after verification, while preserving private originals and the public allowlist.

**Why this priority**: Ticket requires moving existing non-public records out of the public tree without silent data loss (`recon-DEV-473` lines 14–21; brief Q4; FR-010).

**Independent Test**: Seed tracked non-public sources and private destinations (empty, identical, and differing) plus allowlisted public files; run migration; assert inventory, non-clobber copy, SHA-256/count verification, blocking conflicts, idempotent identical reruns, byte-identical private originals when sanitizing public conclusions (FR-010), allowlisted paths excluded from removal staging and preserved after migration, historical per-ticket directories containing only the public allowlist end-state, no synthesized historical conclusions (P7), and no Git history rewrite.

**Acceptance Scenarios**:

1. **Given** tracked non-public operational records in scope, **When** the operator runs migration, **Then** a private source/destination inventory is produced and copies run without clobbering existing differing destinations.
2. **Given** completed copies, **When** verification runs, **Then** raw-byte SHA-256 hashes and counts must match before any public Git removal is staged (P5).
3. **Given** a destination that already matches the source byte-for-byte, **When** migration is rerun, **Then** that file succeeds idempotently.
4. **Given** a differing destination, absent required destination, or failed verification, **When** migration attempts removal, **Then** removal is blocked and sources remain.
5. **Given** sanitized public conclusions being published, **When** migration/sanitization runs, **Then** private unsanitized originals remain byte-identical in private storage (FR-010 regression).
6. **Given** public allowlist files (`spec.md`, sanitized `CONCLUSIONS.md`, applicable `ASSUMPTIONS.md`), **When** removal candidates are classified, **Then** allowlisted paths are excluded from Git-removal staging and remain after migration (FR-001).
7. **Given** ordinary prerequisite or stage-detection scripts, **When** they run, **Then** they do not perform migration as a side effect.
8. **Given** a historical ticket without an existing public `CONCLUSIONS.md`, **When** migration runs, **Then** no new historical conclusions file is synthesized (P7).
9. **Given** completed historical migration for a ticket directory, **When** the public tree is inspected, **Then** that per-ticket directory contains only the applicable public allowlist (plus preserved product/glossary/ADR docs outside per-ticket dirs).

---

### User Story 4 - Scrub retained tracked machine paths (Priority: P1)

After migration removals are staged for non-public records, all retained tracked files outside `.gitignore` contain no disallowed machine-root paths under the exact ticket grep and case-insensitive forward/backslash variant checks, and scrubbed executable/config defaults preserve intended behaviour.

**Why this priority**: Second ticket done criterion (`recon-DEV-473` lines 18–19; brief Q5).

**Independent Test**: After scrub of retained tracked content (including surfaces named in recon), run the exact ticket grep and slash-variant checks from private recon evidence; assert zero matches outside `.gitignore`; prove preserved harness `worktreeRoot` and in-scope Home default behaviour with disposable fixtures; assert maintained host adapters carry no residual operational-public-write instructions and no adapter drift; assert generated outputs remain untouched.

**Acceptance Scenarios**:

1. **Given** retained tracked files outside `.gitignore`, **When** the exact ticket machine-root grep from private recon evidence runs with the ticket’s exclusion of `.gitignore`, **Then** there are no matches.
2. **Given** the same retained set, **When** case-insensitive forward-slash and backslash variants from private recon evidence are checked, **Then** there are no matches.
3. **Given** prose that formerly embedded absolute machine paths, **When** scrubbed, **Then** it uses repository-relative paths or logical identifiers only.
4. **Given** executable/config defaults formerly embedding machine paths (including `harness.yml` worktreeRoot and `scripts/home/*.ps1`), **When** scrubbed, **Then** they use runtime discovery or external configuration while preserving intended behaviour, proven by post-scrub behaviour regressions (US4).
5. **Given** maintained `.agents`/`.claude` host SKILL.md adapters, **When** cutover/scrub completes, **Then** they are updated in parity as tracked sources and the complete inventoried adapter set has no operational-public-write instructions and no adapter drift (P3 precision).
6. **Given** actual generated or `install.ps1`-rendered outputs, **When** scrubbing is considered, **Then** those files remain untouched; if a required generated change is discovered without supported source/tooling, work stops rather than hand-editing (P3 precision).
7. **Given** scrub edits, **When** reviewed against charter §2.3, **Then** they do not change constitution policy and do not alter LocalPlay/`Process.Start` behaviour; any necessary non-migration behaviour change routes back to Patron.

---

### User Story 5 - Clean-checkout and Phase A-to-B pickup smoke (Priority: P2)

An operator proves end-to-end: new-ticket external writes from a clean checkout; Phase A-to-B pickup across worktrees using canonical private durable records; public-only readability; absent-private failure; archives surviving worktree deletion; and private migration/gate receipts with readback — with named blocking final verification invocations.

**Why this priority**: Closes the Q8 verification stage after cutover, migration, and scrub (brief Task order step 5; Sentry M4; Ledger M2).

**Independent Test**: Exercise clean-checkout new-ticket flow and Phase A-to-B pickup across two worktrees; delete a worktree and confirm private archives remain; assert public allowlist, absent-private block, and recorded private verification/gate receipts with SHA readback; at final verification explicitly run the D3 companion, exact ticket/slash checks, and absent-private pickup smoke with commands, exits, and tree/HEAD recorded as distinct blocking checks.

**Acceptance Scenarios**:

1. **Given** a clean checkout and initialized private storage, **When** a new ticket is authored through cut-over tooling, **Then** private writes stay external and public specs remain readable without private storage for public-only inspection.
2. **Given** Phase A durable private records for a ticket, **When** Phase B picks up on another worktree/branch, **Then** canonical brief/plan/tasks are available from the canonical ticket directory without relying on the old worktree path.
3. **Given** a workflow stage that requires private inputs, **When** private storage or those inputs are absent, **Then** pickup/build workflow fails and does not reconstruct approved historical receipts — recorded as a named blocking check with command, exit, and tree/HEAD.
4. **Given** private archives for a ticket, **When** a task worktree is deleted, **Then** the private archives remain.
5. **Given** completed migration and gates, **When** receipts are recorded, **Then** private migration verification and gate receipts include SHA provenance and readback.
6. **Given** final verification, **When** the D3 companion runs from repo root, **Then** `pwsh -NoProfile -File .specify/scripts/powershell/Test-ArtifactStorage.ps1` exit and tree/HEAD are recorded as a blocking gate; companion success does not discharge the distinct ticket/slash or absent-private checks.

## Edge Cases

- Relative `LAMUFLIX_ARTIFACT_ROOT`, root inside repo/worktree, `..` traversal, invalid ticket/repository IDs, symlink/junction into a checkout, or destination escape → reject.
- Absent/empty/whitespace `LAMUFLIX_ARTIFACT_REPOSITORY_ID` → default `LamuFlix`; nonempty invalid slug → reject (P6).
- Missing or unwritable private root on ordinary workflow I/O → fail with actionable initialization guidance; explicit init/migration may create the root.
- Differing private destination during migration → block; identical destination → idempotent success.
- Public-only inspection without private root → allowed; private-required stage without private inputs → fail closed, no public recreation.
- Simultaneous same-ticket real writers without coordination → fail closed under shared ownership; never silent overwrite; no automatic lock stealing (P4).
- Guarded write outside normalized validated private ticket paths, or to protected paths → deny; shell bypass is not sanctioned (P2).
- Actual generated files needing path cleanup without supported source/tooling → stop; never hand-edit generated output (P3 precision).
- Necessary behaviour change beyond path/content migration → stop and route to Patron under charter §2.3; do not silently alter LocalPlay/process behaviour.
- Missing private evidence after transfer failure → block pickup; do not invent an approved historical receipt.
- Historical ticket lacking public conclusions → do not synthesize (P7).
- Scope-empty applicable gates → report `SKIPPED (scope-empty)`, never PASS; Could not run and failures block.
- Partial T014–T018 cutover → not US2 completion (H3).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST limit public per-ticket tracked artifacts to `spec.md`, sanitized `CONCLUSIONS.md`, and `ASSUMPTIONS.md` when applicable, while preserving product/glossary/ADR documentation; allowlisted paths MUST be excluded from Git-removal staging and preserved after migration (Q1).
- **FR-002**: System MUST store briefs, plans, tasks, checklists, evidence, recon, findings, receipts, raw exchanges, drafting receipts, plan adjudications, and other operational/workflow records outside the public Git tree under the private artifact root (Q1, Q2).
- **FR-003**: System MUST resolve the private root via shared PowerShell logic using optional absolute `LAMUFLIX_ARTIFACT_ROOT` or default LocalApplicationData/`LamuFlix/artifacts`, rejecting relative and in-repo/in-worktree roots (Q2).
- **FR-004**: System MUST place canonical durable ticket records at `<private-root>/<repository-id>/DEV-###/` with default repository-id `LamuFlix` (optional override `LAMUFLIX_ARTIFACT_REPOSITORY_ID`), isolate mutable run output/receipts by worktree or full SHA, and attach SHA provenance to gate/review evidence (Q3, D2). Absent, empty, or whitespace-only `LAMUFLIX_ARTIFACT_REPOSITORY_ID` MUST default to `LamuFlix`; nonempty invalid slugs MUST be rejected (P6).
- **FR-005**: System MUST enforce one shared per-ticket exclusive ownership contract across all real cutover writers: fail closed on competing writers, no automatic lock stealing, explicit release; MUST NOT silently overwrite concurrent writers (Q3, P4).
- **FR-006**: Shared resolution MUST expose distinct public and private paths through one common resolver (`Get-FeaturePathsEnv` and related helpers in `common.ps1`), keep `.specify/feature.json` portable, preserve numeric branch detection and the confirmed selection precedence (`recon-DEV-473` lines 49–52), allow public-spec inspection without private storage, and fail workflow stages that require missing private inputs without public recreation or legacy write fallback (Q6, Q7). Receipt/harness helpers MUST NOT duplicate the resolver.
- **FR-007**: System MUST reject traversal, invalid IDs, symlink/junction escapes into any checkout, and destination escapes; MUST NOT create tracked symlinks to private storage (Q7).
- **FR-008**: Explicit initialization/migration via `.specify/scripts/powershell/initialize-artifact-storage.ps1` (explicit migration mode) MAY create a missing private root; ordinary reads/writes MUST fail fast with actionable initialization guidance when the root is missing or unwritable (Q2, Q7, D1).
- **FR-009**: Migration MUST be an explicit operator action that inventories, copies without clobbering, verifies raw-byte SHA-256 hashes and counts (no text/newline normalization), then stages Git removals only after verification; identical destinations succeed on rerun; conflicts and failed verification block removal; history rewrite is out of scope (Q4, D1, P5).
- **FR-010**: When publishing sanitized public conclusions, system MUST keep private unsanitized originals byte-identical in private storage (Q4).
- **FR-011**: Private archives MUST survive branch/worktree deletion; operator owns backup/cross-machine transfer; missing evidence MUST NOT be reconstructed as an approved historical receipt (Q7).
- **FR-012**: All in-scope producers/consumers (including `create-new-feature.ps1`, `check-prerequisites.ps1`, `setup-plan.ps1`, `setup-tasks.ps1`, named `.agents`/`.claude` adapters, mutation receipt tooling, load docs, plus callers found in ticket-scope inventory recorded by T001/T018) MUST cut over together to the shared resolver contract; T014–T018 completion and inventoried caller proof MUST gate activation/delivery; no closed Gate 1 filename list is required (Q6, D4, H3).
- **FR-013**: Retained tracked files outside `.gitignore` MUST pass the exact ticket machine-root grep and case-insensitive forward/backslash variant checks recorded in private `recon-DEV-473` lines 19 and 30–31 (Q5). Public drafting MUST reference that private evidence without reintroducing the machine-root literal.
- **FR-014**: Scrubbing MUST replace prose paths with repository-relative or logical identifiers and replace executable/config defaults with runtime discovery or external configuration while preserving intended behaviour, including post-scrub proof for harness `worktreeRoot` and in-scope Home defaults; maintained `.agents`/`.claude` host SKILL.md adapters are updated in parity as tracked sources; actual generated/`install.ps1`-rendered outputs MUST NOT be hand-edited; T026 MUST change `harness.yml` `worktreeRoot` only and stop if a generated-output change is required without supported tooling; ticket-forced machine-path scrub of retained tracked content (including `README.md`) is authorized as path/content cleanup, not a product README rewrite (Q5, P3 precision).
- **FR-015**: Scrubbing MUST NOT change constitution policy or LocalPlay/`Process.Start` behaviour; any necessary non-migration behaviour change MUST stop for Patron under §2.3 (Q5).
- **FR-016**: Implementation MUST follow the five-stage order and regression matrix in brief Q8 / Task order, including verified bootstrap seed before private-required activation and bulk removals remaining stage 4, using standalone companion `.specify/scripts/powershell/Test-ArtifactStorage.ps1` (D3) with no new test dependency/project/helper layer; all companion edits MUST be serialized (Q8, P1).
- **FR-017**: Applicable harness gates MUST run at the correct stage; failures and Could not run block; scope-empty exit 2 is reported as `SKIPPED (scope-empty)`, never PASS; configured SKIP and mutation N/A remain as scripts report (Q8, Q9). Final verification MUST explicitly record companion, exact ticket/slash, and absent-private pickup invocations with commands, exits, and tree/HEAD; companion success MUST NOT substitute for the other two (Sentry M4; Ledger M2).
- **FR-018**: Review closure MUST meet the Q9 closing bar, frozen scope, and two-round / two-fix-commit round cap; size:M requires Sentry, Ledger, and Compass at the applicable review stage (Q9).
- **FR-019**: DEV-473’s own operational Phase A records authored under `specs/DEV-473/` MUST receive a verified private bootstrap seed after foundation and before private-required consumer activation, then complete bulk migration under the same public/private policy during stage 4; the draft MUST NOT claim external cutover already exists (brief Handoff; P1).
- **FR-020**: System MUST apply a narrow confined extension to `scripts/hooks/guard.ps1` permitting Edit/Write/apply-patch only to normalized validated private artifact paths under the approved external root, preserving protected-path and unrelated boundary denials, with allow/deny regressions; MUST NOT bypass the guard via shell or broaden allowance beyond that set (P2).
- **FR-021**: Historical migration MUST sanitize only existing public `CONCLUSIONS.md` files and MUST NOT synthesize new historical conclusions for tickets that lack one (P7).
- **FR-022**: After historical migration, each migrated per-ticket public directory MUST contain only the applicable public allowlist (FR-001 end-state).

### Key Entities

- **Public allowlist artifact**: Tracked per-ticket `spec.md` / sanitized `CONCLUSIONS.md` / optional `ASSUMPTIONS.md`.
- **Private operational record**: Non-public workflow artifact stored under the canonical ticket directory or worktree/SHA-isolated run area.
- **Private artifact root**: Resolved absolute storage root with override/default rules and confinement.
- **Migration inventory entry**: Private source→destination mapping with raw-byte SHA-256/count verification state.
- **Feature selection record**: Portable public `.specify/feature.json` entry without machine-specific roots.
- **Bootstrap seed receipt**: Verified private copy proof for the in-flight ticket’s Phase A operational set before consumer activation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After cutover, a new ticket created from a clean checkout writes operational records only under the private artifact root and keeps public `specs/DEV-###/` within the allowlist.
- **SC-002**: After verified migration and scrub, the exact ticket machine-root grep and case-insensitive slash variants (private recon evidence) return no matches outside `.gitignore`.
- **SC-003**: 100% of inventoried in-scope non-public operational records are either verified-copied to private storage before public removal or explicitly blocked with intact sources when verification fails.
- **SC-004**: Public-only spec inspection succeeds without private storage; every tested private-required stage fails closed when private inputs are absent (no public recreation).
- **SC-005**: Phase A durable private records remain available for Phase B pickup after worktree/branch change, and private archives remain after worktree deletion.
- **SC-006**: Applicable gates for the change report honest PASS / `SKIPPED (scope-empty)` / SKIP / N/A / Failure / Could not run verdicts with no invented eligibility and no substitution of plain build for a required gate.
- **SC-007**: FR-010 regression: private unsanitized originals remain byte-identical after public sanitization publication.
- **SC-008**: Post-scrub harness `worktreeRoot` and in-scope Home defaults preserve intended behaviour under disposable-fixture outcome checks.
- **SC-009**: Final verification records distinct blocking companion, ticket/slash, and absent-private pickup results with commands, exits, and tree/HEAD.

## Assumptions

- Grill Q1–Q9, drafting decisions D1–D4 (D3 resolved), and plan adjudication P1–P7 plus P3 precision fully settle inventable behaviour for this draft; no taste assumptions and no `ASSUMPTIONS.md` file required (`brief.md` Handoff; D1–D4; P1–P7).
- Settled inventables: init/migration command `.specify/scripts/powershell/initialize-artifact-storage.ps1` with explicit migration mode (D1); environment-only `LAMUFLIX_ARTIFACT_REPOSITORY_ID` default `LamuFlix` with absent/empty/whitespace → default and nonempty invalid reject (D2, P6); companion `.specify/scripts/powershell/Test-ArtifactStorage.ps1` and repo-root invocation above (D3); T001/T018 record bounded discovered callers with no closed Gate 1 filename list (D4); P1 bootstrap before activation; P2 narrow guard extension; P3 maintained host-adapter parity with no generated-file waiver; P4 shared ownership; P5 raw-byte SHA-256; P7 sanitize-existing-only conclusions. Provenance: current `recon-DEV-473` lines 1–52 for ticket/caller/guard/generator; historical D3 companion evidence in brief D3 exchange; exact scrub literals only in private recon lines 19 and 30–31.
- No production C# or web behaviour change is planned; scope-empty C#/web gates remain non-PASS scope-empty reports when applicable (`brief.md` Task order; Q8).
- Existing glossary/ADR documentation is preserved; no new domain ADR is required for this size:M workflow migration (`brief.md` Handoff).
- Existing no-explanatory-comments convention applies; draft status remains Draft until Keel independently freezes after repair readback.
