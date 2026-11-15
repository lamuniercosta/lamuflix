# Implementation Plan: Move spec evidence and receipts out of the public repo and scrub local paths

**Branch**: `feature/473-spec` | **Date**: 2026-11-14 | **Spec**: `specs/DEV-473/spec.md`

**Input**: Feature specification from `specs/DEV-473/spec.md`; grill-closed `brief.md` (Q1–Q9 + D1–D4 + D3 resolved + P1–P7 + P3 precision); `CONCLUSIONS.md`; Maestri note `recon-DEV-473` lines 1–52 (ticket/caller/guard/generator); historical D3 companion evidence preserved in brief D3 exchange; Maestri note `DEV-473`; `specs/PRODUCT.md`. No `ASSUMPTIONS.md`. Exact machine-root literals remain only in private recon lines 19 and 30–31.

**Status**: Draft plan under consolidated plan-repair round 1 of 2. No PLAN FROZEN, Gate 1, implementation, runtime, or merge clearance claimed. Two-round repair cap and all three axes (Sentry, Ledger, Compass) remain in force.

## Summary

Move non-public pipeline/operational records out of the public Git tree into a shared private artifact root, keep only the public allowlist under `specs/DEV-###/`, cut over all in-scope producers/consumers to one resolver contract, migrate existing tracked operational records with verified copy-before-remove, and scrub retained tracked machine paths so the ticket grep (plus slash variants from private recon evidence) is clean. Approach follows brief Q8 order with P1 precision: inventory and contract → shared resolution plus explicit initialization/migration with temporary-directory regressions → **verified bootstrap seed of DEV-473 Phase A records** → coherent caller/skill/adapter cutover (activation gated on complete T014–T018 proof) → verified bulk archive then staged removals and retained-file scrub → clean-checkout / Phase A-to-B smoke and named blocking final verification. No production C# or web behaviour change; no new dependency or architectural layer.

## Technical Context

**Language/Version**: PowerShell 7 for Spec Kit / harness scripts; Markdown skills and docs; no production C# or web change planned

**Primary Dependencies**: Existing repository tooling only — `.specify/scripts/powershell/*` (including `common.ps1` `Get-FeaturePathsEnv`, `create-new-feature.ps1`, `check-prerequisites.ps1`, `setup-plan.ps1`, `setup-tasks.ps1`), harness scripts under `scripts/`, maintained tracked host adapters `.agents/skills/*/SKILL.md` and `.claude/skills/*/SKILL.md` updated in parity (P3 precision; not generated output), and narrow extension of `scripts/hooks/guard.ps1`. No host-skill generator or runnable `install.ps1` is present in this worktree (`recon-DEV-473` lines 38–40); do not invent one. Actual generated/`install.ps1`-rendered outputs remain untouched. No new NuGet/npm dependency (`brief.md` Q7–Q8; `PRODUCT.md` §5 item 1)

**Storage**: Private filesystem artifact root (`LAMUFLIX_ARTIFACT_ROOT` or LocalApplicationData/`LamuFlix/artifacts`); public Git tree retains allowlist only. No database schema change

**Testing**: Standalone PowerShell 7 companion `.specify/scripts/powershell/Test-ArtifactStorage.ps1` (D3; historical companion convention evidence in brief D3 exchange). Repo-root invocation `pwsh -NoProfile -File .specify/scripts/powershell/Test-ArtifactStorage.ps1`. Strict errors; unique GUID temp fixtures; child-process real scripts with controlled environment; confinement and deterministic cleanup; smoke in disposable fixtures only. All companion edits serialized. No new test dependency/project/helper layer; do not copy or dot-source Home domain stubs/helpers. Guard allow/deny regressions and post-scrub behaviour checks use the same companion/disposable-fixture approach.

**Target Platform**: Developer/operator workstations and CI checkouts using the existing harness/Spec Kit PowerShell entrypoints

**Project Type**: Harness / Spec Kit workflow migration (scripts, skills, docs, tracked operational artifacts) — not an application feature slice

**Performance Goals**: None beyond correct deterministic path resolution, migration verification, and gate honesty

**Constraints**:
- Charter §2.3: no new dependency, project/layer, schema, public API, LocalPlay/`Process.Start`, or unrelated file rewrites; ticket acceptance forces in-scope tracked-file migration/scrub under item 6
- Constitution Principle VII: no secrets or machine-specific paths in tracked source (`.specify/memory/constitution.md:645-647`); public docs must not reintroduce machine-root literals that live only in private recon evidence
- No history rewrite; no legacy public write fallback; no tracked private-storage symlinks; no dual-read/public-write fallback (P1)
- No shell bypass of write guard; no broad blanket allow (P2)
- No generated-file hand edits; no speculative installer/generator command (P3 precision)
- Settled inventables (D1–D4, P1–P7): `.specify/scripts/powershell/initialize-artifact-storage.ps1` with explicit migration mode; environment-only `LAMUFLIX_ARTIFACT_REPOSITORY_ID` (default `LamuFlix`, absent/empty/whitespace → default, nonempty invalid → reject; no harness key); `.specify/scripts/powershell/Test-ArtifactStorage.ps1` as above; T001/T018 record bounded discovered callers; P1 bootstrap before activation; P2 narrow guard; P3 maintained adapter parity; P4 shared ownership; P5 raw-byte SHA-256; P7 sanitize-existing-only conclusions
- Existing no-explanatory-comments convention for implementation artifacts
- `$env:PYTHONUTF8=1` for authoring/check commands (`brief.md`)

**Scale/Scope**: Shared resolver + guard extension + in-scope producer/consumer cutover + verified bootstrap + bulk migration of currently tracked operational records + retained-path scrub + regression/smoke proof for ticket done criteria (size:M)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [VII] Configuration Isolation (constitution.md:643-647): private root via env/default discovery; scrub removes machine-specific paths from retained tracked content; no secrets committed; public docs use logical identifiers only; machine-root literals stay in private recon evidence only.
- [LocalPlay / Process.Start] (constitution.md:676-678): scrub is path/content cleanup only; LocalPlay registration and process-launch behaviour are not altered (`brief.md` Q5).
- [Static-Analysis / Quality gates]: No production `.cs` or web behaviour change planned. Applicable gates run at the correct stage; scope-empty exit 2 reported as `SKIPPED (scope-empty)`, never PASS; failures and Could not run block; do not substitute plain `dotnet build` for a required gate (`brief.md` Q8).
- [Architecture / dependencies] (`PRODUCT.md` §5 items 1–2): no new package, project, top-level folder, or architectural layer; shared resolution extends existing PowerShell common/feature scripts; P2 guard change is a narrow confined policy extension without structural escalation.
- [File scope] (`PRODUCT.md` §5 item 6): ticket acceptance criterion authorizes rewriting/migrating unnamed tracked operational files and scrubbing retained machine paths forced by the grep gate; unrelated rewrites remain forbidden.
- [Documentation]: No new ADR (brief Handoff; ADRs remain Keel-owned). Preserve existing glossary/ADR docs. Ticket-forced machine-path scrub of retained tracked content including `README.md` is authorized under Q5 / charter §2.3 item 6 as path/content cleanup preserving intended behaviour; not a product README rewrite beyond that criterion. No explanatory comments beyond standing exemptions.

**Post-design re-check**: Still satisfied — design stays inside existing script/skill/doc/hook surfaces; private storage is external filesystem, not a new repo layer; generated outputs remain untouched.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-473/
├── brief.md                 # Grill-closed (Keel) — Phase A path; migrates later
├── CONCLUSIONS.md           # Public allowlist (sanitized)
├── PLAN_ADJUDICATION.md     # Keel-owned adjudication (operational → private later)
├── spec.md                  # Public allowlist
├── plan.md                  # This file (Phase A path; operational → private later)
├── tasks.md                 # /speckit-tasks (Phase A path; operational → private later)
├── checklists/requirements.md
└── DRAFTING_RECEIPT.md      # Quill-owned; also T002 location-plan annotation target
```

Phase 0/1 Spec Kit extras (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) are not produced: inventable behaviour and contracts are frozen in brief Q1–Q9 / P1–P7; no public API/schema entities.

### Source Code (repository root)

```text
.specify/
├── feature.json                                      # portable public feature selection (no machine root)
├── memory/constitution.md                            # retained-path scrub only if machine paths present
└── scripts/powershell/
    ├── common.ps1                                    # shared public/private resolution + Get-FeaturePathsEnv + FR-004/005/P4 helpers
    ├── create-new-feature.ps1                        # new-ticket artifact creation/numbering
    ├── check-prerequisites.ps1                       # public/private prerequisites + stage detection
    ├── setup-plan.ps1                                # confirmed Get-FeaturePathsEnv consumer (recon 49-52)
    ├── setup-tasks.ps1                               # confirmed Get-FeaturePathsEnv consumer (recon 49-52)
    ├── initialize-artifact-storage.ps1               # explicit init + migration mode (D1) + bootstrap seed mode (P1)
    └── Test-ArtifactStorage.ps1                      # standalone companion regressions (D3)

.agents/skills/**/SKILL.md                            # maintained editable host adapters — update in parity (P3)
.claude/skills/**/SKILL.md                            # parallel maintained adapters — update in parity (P3)
# NOT generated output; no in-repo host generator (recon 38-40). Actual generated files remain off-limits.

scripts/
├── hooks/guard.ps1                                   # narrow Deny-OutsideBoundary extension for validated private ticket paths (P2)
├── new-mutation-receipt.ps1                          # external receipt path; consumes common resolver
├── home/*.ps1                                        # defaults scrub via runtime discovery/external config + behaviour proof
└── load/README.md                                    # operational references

harness.yml                                           # worktreeRoot scrub only (T026) — no threshold-output edits; no new repository-id key
README.md                                             # ticket-forced machine-path scrub only, preserving behaviour (Q5)
docs/reviews/                                         # inventory/classify/archive operational evidence under allowlist rules
specs/DEV-*/                                          # migrate non-allowlist operational artifacts; keep public allowlist
```

**Structure Decision**: Extend existing Spec Kit PowerShell, harness skill/script, and guard surfaces; private archives live outside the repository under the approved root. No new project or top-level architectural folder. Init/migration command, env-only repository slug, companion test path, open caller inventory, bootstrap, guard, ownership, hash, and conclusions scope are settled by D1–D4 and P1–P7 (`DRAFTING_RECEIPT.md`).

## Complexity Tracking

> No constitution violations requiring justification. Private storage is external filesystem configuration, not a new architectural layer. P2 is a narrow confined guard-policy extension with allow/deny regressions, not a structural escalation.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Approach (ordered)

1. **Inventory (T001)**: Confirm in-scope producers/consumers including `setup-plan.ps1`, `setup-tasks.ps1`, their authoring skills, shared `Get-FeaturePathsEnv`, and feature-selection precedence from `recon-DEV-473` lines 49–52; record the bounded discovered caller set (D4). Preserve current recon sections for ticket/caller/guard/generator facts and historical D3 companion evidence scope. Script-test convention is settled (D3) — do not invent alternate projects/paths. Reference exact scrub literals only via private recon lines 19 and 30–31.
2. **Shared resolver + explicit init/migration + contracts**: Implement public/private path APIs, confinement, FR-004/FR-005/P4 ownership helpers, and P6 repository-id handling in `common.ps1`; add `.specify/scripts/powershell/initialize-artifact-storage.ps1` with explicit migration mode (D1); wire env-only `LAMUFLIX_ARTIFACT_REPOSITORY_ID` (D2/P6); implement P2 narrow guard extension with allow/deny regressions; temporary-directory regressions in `.specify/scripts/powershell/Test-ArtifactStorage.ps1` for default/override, validation/confinement, missing/unwritable root, initialization, public-only reads, ownership across real callers, and hash contract.
3. **Bootstrap seed (P1)**: After foundation (T003–T006 / guard primitives) and **before** private-required consumer activation, copy DEV-473’s own Phase A operational records into verified private storage (raw-byte SHA-256). No dual-read; no public-write fallback. Bulk historical archive/removals remain stage 4.
4. **Coherent cutover**: Update create/setup/prerequisite/setup-plan/setup-tasks scripts, maintained `.agents` and `.claude` adapters in parity, mutation receipt tooling, load docs, and inventoried callers together; prove portable feature selection, numeric branch behaviour, single-writer coordination across real caller processes, SHA provenance, residual no-public-write across the complete inventoried adapter set. T014–T018 completion and inventoried caller proof gate activation/delivery; partial work is not US2 completion. Remove claims that an unavailable host generator must run.
5. **Verified bulk archive then scrub**: Private inventory → copy without clobber → raw-byte SHA-256/count verify → stage removals (allowlist excluded); FR-010 byte-identical private originals; P7 sanitize-existing-only; historical allowlist end-state; scrub retained tracked machine paths; T026 changes `harness.yml` worktreeRoot only; update maintained adapters; never hand-edit generated output; post-scrub behaviour proof for worktreeRoot and Home defaults.
6. **Smoke + named blocking final verification**: Clean-checkout new ticket; Phase A-to-B pickup across worktrees; archive survival after worktree deletion; exact ticket grep + slash variants from private recon evidence; absent-private pickup; private migration/gate receipts with readback; explicit blocking companion execution with exit/tree/HEAD; honest harness gate reporting. Companion success cannot discharge ticket/slash or absent-private checks.

## Test Strategy

| Area | Proof |
|------|--------|
| Resolver defaults/override | Temporary directories; LocalApplicationData default path shape; absolute override accepted; relative/in-repo rejected |
| Repository-id (P6) | Absent/empty/whitespace → `LamuFlix`; nonempty invalid rejected |
| Confinement | Traversal, invalid IDs, symlink/junction into checkout, destination escape rejected |
| Missing/unwritable root | Ordinary I/O fails with actionable init guidance; explicit init may create root |
| Public-only reads | Spec/conclusions readable without private root |
| Guard (P2) | Allow normalized validated private ticket paths; deny outside set / protected / unrelated; no shell bypass |
| Ownership (P4) | Competing real caller processes fail closed; no automatic steal; explicit release |
| Cutover writers | New-ticket, setup-plan/tasks, receipt writers land under private root; no legacy public fallback |
| Host adapters (P3) | `.agents` and `.claude` updated in parity; residual public-write proof; no generator invocation required |
| Bootstrap (P1) | DEV-473 Phase A seed verified before consumer activation |
| Migration (P5/FR-010/FR-001/P7) | Inventory, non-clobber, SHA-256/count verify, identical idempotent rerun, differing blocks removal, private originals byte-identical, allowlist excluded from removal, no synthesized historical conclusions, historical allowlist end-state |
| Scrub + behaviour (H2) | Exact ticket/slash checks clean; harness worktreeRoot and Home defaults preserve behaviour via disposable fixtures |
| Pickup / lifecycle | Phase A-to-B across worktrees; absent private inputs fail; archives survive worktree deletion |
| Final blocking checks (M4/M2) | Companion + ticket/slash + absent-private each recorded with command/exit/tree/HEAD |
| Gates | Applicable scripts only; `SKIPPED (scope-empty)` never PASS; Failure/Could not run block |

## Loop terms (from brief Q9)

- **Closing bar**: Zero unresolved substantiated Critical/High/Medium; every Low has explicit cited disposition.
- **Frozen scope**: As `brief.md` / spec Scope; anything else is a follow-up issue.
- **Round cap**: Two review rounds; at most two fix commits per round; two plan-repair rounds max; cap exhaustion never clears an above-bar finding.
- **Reviewers at applicable stage**: Sentry, Ledger, and Compass (size:M).
- **Freeze authority**: Keel independent reread after repairs; Quill does not declare PLAN FROZEN or Gate 1.
