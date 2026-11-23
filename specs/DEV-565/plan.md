# Implementation Plan: Unify provider API keys into API settings

**Branch**: `feature/565-spec` | **Date**: 2026-11-23 | **Spec**: `specs/DEV-565/spec.md`

**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-565-spec`

**Input**: Grill-closed `brief.md` (Q1–Q8) plus analyze-time Q9; `CONCLUSIONS.md` Q1–Q9; Maestri notes `DEV-565`, `recon-DEV-565` (HEAD `15c1e43af7ba6c3d5f37efbabd4a313164588932`); `specs/DEV-565/ticket-text.txt`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only in this drafting round — no amendment here).

**Status** — PLAN FROZEN following [plan adjudication](PLAN_ADJUDICATION.md). The Q9 owner checkbox remains open. T000 verifies owner approval and the spec PR merge before Build.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — decisions frozen in the brief; no public API delta; no schema change.

## Summary

Unify provider API keys (`Omdb:ApiKey`, `Tmdb:Enabled`/`Tmdb:ApiKey`, `Subtitles:OpenSubtitles:ApiKey`) into the mounted API settings JSON for Compose and the home cluster. Remove TMDb env / dedicated Secret channels, complete the OpenSubtitles example contract, guard deploy parity against restored env wiring, update focused tests and README, and close acceptance only when required automated checks are green and both separate live Compose and home enrichment receipts are present (failed/`Could not run` required checks remain unresolved; missing live evidence is pending per deployment, never an alternate acceptance outcome).

## Technical Context

**Language/Version**: Existing stack — Docker Compose YAML, Kubernetes manifests, PowerShell 7 home provisioning scripts, C# xUnit configuration tests, Markdown README. No production C# behaviour change planned (`specs/PRODUCT.md` §1; `brief.md` Q7).

**Primary Dependencies**: Existing tooling only. No new NuGet/npm dependency or version swap (`PRODUCT.md` §5 item 1; brief out of scope).

**Storage**: No database/schema change. Runtime provider config via mounted JSON / `api-settings` Secret. Home operational ingestion via external `secretSourcePath` only.

**Testing**: Rewrite `tests/LamuFlix.UnitTests/TmdbComposeTests.cs`; update stubbed `scripts/home/Test-NewHomeCluster.ps1` (focused checks stay at that step); run the full `dotnet test` suite as prescribed by the Closing bar; run property tests explicitly when tagged invariants apply (otherwise Bernstein records property-tests `SKIP`); run `scripts/Test-DeployParityFixtures.ps1`, `scripts/Test-DeployParity.ps1`, `scripts/run-compose-smoke.ps1`, `npm run e2e` from `web/`, and gitleaks to green. Hosted-check evidence is carried into gate tasks. Both separate live Compose and home enrichment receipts are closure criteria (`brief.md` Q6/Q8).

**Target Platform**: Local Compose stack and home Kubernetes cluster (`lamuflix` namespace for documented Secret cleanup).

**Project Type**: Deployment configuration / provisioning / docs change on existing LamuFlix repo. No `/web` product code change planned; e2e runner is review-only unless Patron-ratified regression fix.

**Performance Goals**: N/A — configuration channel consolidation; no invented latency SLA.

**Constraints**: Charter §2.3 items 5–6 deliberately ruled (Q1, Q7); §2.3(b) constitution departure ruled in Q9. Never print/commit secrets. Provisioning must never delete `tmdb-credentials`. File edits confined to Q1/Q5/Q7 allowlist plus `.specify/memory/constitution.md` solely for the owner-approved Q9 Principle VII amendment / Governance bump. Closing bar: Size M three axes, max two review rounds, Critical–Low disposition (`brief.md` Q8); Gate 1 closed until Q9 owner checkbox approval (`brief.md` Q9).

**Scale/Scope**: Size M. Supersedes DEV-445 Q2 for Compose and home-cluster TMDb configuration only; local `dotnet run` user-secrets / gitignored Development settings channel unchanged. Q9 adds constitution-only scope; Q1–Q8 runtime and acceptance unchanged.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current, **unamended in this drafting round**). Open owner structural checkbox per Q9 (supersedes Q8 zero-checkbox status).

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **PASS** — no new ports/adapters; provider adapters out of scope. |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A / PASS** — no handler work. |
| III (Typed query model) | MovieQuery | **N/A / PASS** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **N/A / PASS** — no domain mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — option validation unchanged (out of scope). |
| VI (Observability) | Safe diagnostics | **PASS** — cutover/docs never echo secret values; live evidence is credential-redacted (Q3/Q6). |
| VII (Configuration / LocalPlay / Process.Start) | Options; secrets; LocalPlay gated | **CURRENT CONFLICT — not PASS** — Patron Q1 (§2.3 item 5) requires mounted settings JSON for Compose/home provider keys while retaining user secrets / gitignored Development settings for `dotnet run` and non-API Compose values in `.env`. Constitution VII at `.specify/memory/constitution.md:673–674` still states containers receive local values as environment variables. Q9 records a §2.3(b) departure with an **open owner-approval checkbox** and a post-approval amendment path (amend lines 673–674; Governance proposal, MAJOR version bump, Sync Impact Report, `Last Amended`, migration plan = DEV-565 cutover/README sequence, review `.specify/templates/*` for follow-up). **No new ADR and no ADR supersession** (Q9). This drafting round does **not** edit the constitution and does **not** mark VII PASS. VII PASS only after owner-approved amendment lands and compliance is re-checked. No LocalPlay or `Process.Start`. Example placeholders only; no committed secrets. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new glossary/CONTEXT term; no ADR (none required per Q9). Spec-local vocabulary only. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — static Compose/unit config tests, stubbed home provisioning tests, deploy-parity fixtures, smoke/e2e gates, live enrichment receipts. |
| Tech stack / API docs | OpenAPI chain | **PASS** — no public DTO/route/status change; no OpenAPI/TS edit. |
| Governance / Static-Analysis Gates | Honest exits; amendment procedure | **Governance proposal (not executed here)**: after owner checkbox approval, follow constitution Governance (`.specify/memory/constitution.md:949–960`) — written proposal/rationale/impact, no ADR (Q9 Documentation Rules disposition), migration plan via the ticket cutover sequence, apply MAJOR version bump + Sync Impact Report + `Last Amended`, review `.specify/templates/*` before merging the amendment. Phase B gates: scoped Roslyn/complexity/InspectCode, package audit, format, full .NET suite; mutation `SKIPPED (scope-empty)` when no production `src/` C#; web gates `SKIPPED (scope-empty)` when `web/` untouched; property-tests `SKIP` via Bernstein when no tagged invariants; never fold non-pass into PASS (Q8). Constitution compliance required before the implementation merge (Q9). |

### Complexity Tracking

| Violation / open clash | Why needed | Simpler alternative rejected because |
|---|---|---|
| Constitution VII `.specify/memory/constitution.md:673–674` (containers receive local values as environment variables) vs Patron Q1 mounted JSON for provider keys in Compose/home | Ticket *Scope & Technical Design* and Patron Q1 require the settings-file channel; Q9 judges a narrowly scoped Principle VII amendment necessary under §2.3(b) | Reverting to container env for provider keys would contradict the ratified ticket/Q1 design; silent VII PASS without owner-approved amendment would violate constitution Governance |

VII remains a **current conflict** until the owner approves the Q9 checkbox and the scoped amendment (plus Governance version bump / Sync Impact Report) lands. This plan does not amend the constitution.

## Approach

- **Phase A** — Plan challenge complete. The spec PR carries Q9. See the brief and T000 for owner approval and merge verification.
- **Phase B pickup**: Wisp fresh drift recon is strictly read-only. Constitution amendment and any plan/tasks drift amendment are **separate authorized steps** after that recon — never part of read-only analyze.
- **Task order (`brief.md` Task order; Q7/Q9)**:
  0. **Gate 1 / T000** — verify owner approval of the Q9 checkbox and the spec PR merge before any Build work.
  0b. **Post-approval constitution amendment** — scoped edit to `.specify/memory/constitution.md:673–674` plus Governance version bump, Sync Impact Report, `Last Amended`, migration note, and template review; complete before runtime cutover; required for constitution compliance before the implementation merge. No new ADR.
  1. **Focused regression checks first** — rewrite `TmdbComposeTests.cs` and update `Test-NewHomeCluster.ps1` in parallel; both authored before any cutover task; use failing assertions to drive the cutover (focused checks remain at this step and the post-cutover re-run).
  2. **Atomic runtime and provisioning cutover** — update example JSON (template) first, then Compose / `.env.example` / API deployment / secrets example / `New-HomeCluster.ps1` / `_home-common.ps1`. Home provisioning consumes the example template, so template-dependent completion follows the template update; preserve non-provider settings; re-run focused checks.
  3. **Parity and secret scanning** — update `Test-DeployParity.ps1` and fixtures (including Quill-named negative case `provider-key-env`); remove obsolete gitleaks allowlist once the parsing needle is gone. Complete before README.
  4. **Document and verify** — README table/migration (after parity and gitleaks per frozen brief); run automated checks to green; capture both live receipts or pending-per-deployment labels; carry hosted-check evidence into gate tasks; full .NET suite plus explicit property-test run or Bernstein `SKIP`; Size M review under Q8 bar.
- **Do not**: edit the constitution in this drafting round; edit adapters/validation; edit `appsettings.json` or `web/e2e/run.mjs` without Patron ratification; auto-delete `tmdb-credentials`; invent secrets; claim runtime PASS or VII PASS from planning; treat Patron as owner approval.

## Plan challenge changes

- T001 checks both Compose smoke settings channels. T001b applies the Q9 amendment with a MAJOR bump, verifies the ADR disposition, and records the dependent template and docs review.
- T002–T006 pin the empty generated TMDb key representation and keep known-red test commits local until the focused rerun passes.
- T008–T009 use inert inputs, assert the parity rejection in both deployment modes, and scan the final test and fixture changes after removing the obsolete allowlist.
- T010–T012 document the home override and rollback sequence, verify the actual E2E settings mount and missing-file behavior in isolation, and record both live receipts. Implementation merge clearance requires both receipts.

See the brief and adjudication for the rationale and tasks for execution details.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-565/
├── brief.md
├── CONCLUSIONS.md
├── ticket-text.txt
├── spec.md
├── plan.md              # This file
├── tasks.md
├── PLAN_ADJUDICATION.md
├── DRAFTING_RECEIPT.md
└── checklists/
    └── requirements.md
```

No `research.md` / `data-model.md` / `contracts/` / `quickstart.md` — grill closed every inventable decision; no public API or schema artifact.

### Source Code (authorized boundary — brief Q1/Q5/Q7 + Q9 constitution-only)

```text
# Post-approval constitution amendment only (Q9; not in this drafting round)
.specify/memory/constitution.md   # lines 673–674 + Governance version bump / Sync Impact Report / Last Amended; review .specify/templates/*

# Focused tests (step 1)
tests/LamuFlix.UnitTests/TmdbComposeTests.cs
scripts/home/Test-NewHomeCluster.ps1

# Runtime / provisioning cutover (step 2)
docker/api.settings.example.json
docker-compose.yml
.env.example
deploy/k8s/base/api-deployment.yaml
deploy/home/secrets.example.json
scripts/home/New-HomeCluster.ps1
scripts/home/_home-common.ps1

# Parity + gitleaks (step 3)
scripts/Test-DeployParity.ps1
scripts/fixtures/deploy-parity/compose.yml
scripts/fixtures/deploy-parity/compose-extra-service/compose.yml
scripts/fixtures/deploy-parity/pass/rendered.yaml
scripts/fixtures/deploy-parity/missing-service/rendered.yaml
scripts/fixtures/deploy-parity/compose-extra-service/rendered.yaml
scripts/fixtures/deploy-parity/digest-mismatch/rendered.yaml
scripts/fixtures/deploy-parity/provider-key-env/   # NEW negative case (Quill-named)
.gitleaks.toml

# Docs (step 4)
README.md

# Read-only verification inputs (do not edit unless Patron-ratified ticket-forced regression)
src/LamuFlix.Api/appsettings.json
web/e2e/run.mjs
scripts/run-compose-smoke.ps1
```

**Structure Decision**: Edit existing deployment, provisioning, test, parity, gitleaks, and README files, plus the Q9-conditional constitution amendment only. No new project, layer, package, or ADR.

## Phase 0 / Phase 1 note

Research and data-model/contract generation are **skipped as separate files**: every NEEDS CLARIFICATION that would have driven them was closed in grill Q1–Q8. Public OpenAPI contracts are unchanged.

## Gate expectations

- **Phase A** — Plan challenge complete. See the adjudication and the Q9 owner checkbox in the brief.
- **Gate 1**: open Q9 owner checkbox on the spec PR; T000 verifies owner approval and merge before Build. Patron is not owner approval.
- **Phase B / pre-PR**: post-approval scoped constitution amendment (Governance bump / Sync Impact Report) before runtime cutover; focused tests at the regression step; full `dotnet test` suite; explicit property-test run when applicable (else Bernstein `SKIP`); deploy-parity fixtures + check, compose-smoke, e2e-smoke, gitleaks all green; hosted-check evidence carried into gate tasks; scoped analyzer gates as applicable; honest `SKIPPED (scope-empty)` / `SKIP`. Failed or `Could not run` required checks leave gates unresolved. Both separate live Compose and home enrichment receipts required for delivery closure; missing proof is pending per deployment, never an alternate acceptance outcome (`brief.md` Q6/Q8). Constitution compliance required before the implementation merge.
- **Review**: Size M Risk/Standards/Spec; Q8 two-round closing bar including Low; never merge by seats.
- **Constitution VII**: **current conflict** with explicit Q9 owner-approval/amendment path; **not PASS** on current (unamended) evidence. No new or superseded ADR per Q9.
