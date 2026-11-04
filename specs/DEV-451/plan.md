# Implementation Plan: Pre-PR mutation under 30 minutes

**Branch**: `feature/451-spec` | **Date**: 2026-11-04 | **Spec**: `specs/DEV-451/spec.md`
**Input**: Frozen `brief.md` (Q1–Q6 + plan-challenge round 1 decisions 1–7); `CONCLUSIONS.md`; `FOLLOW-UP-TICKETS.md`; `spec.md`; Maestri note `recon-DEV-451`; adjudication-DEV-451; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only; Quill does not edit constitution or ADRs)
**Status**: Draft — Phase 2 Quill drafting + plan-challenge round 1 repair; Gate 1 not approved; PLAN FROZEN not declared; no implementation clearance. Measurement and lever selection remain blocking Build work — no speedup, score, or runtime success is asserted here.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md`, `checklists/requirements.md` (Quill). No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief; no public API. No ADR drafted now (Q3, Q6).

## Summary

Make the local pre-PR mutation gate finish under 30 minutes on Patron-picked Core ticket DEV-444 without relaxing mutation policy or corrupting verdicts. Measure matched baselines first via Bernstein/Rigger-provisioned detached historical replay for DEV-444 / DEV-448 / DEV-333, then evaluate a concrete lever outside the exhausted DEV-435 set under the Q3 experimental contract using a reversible non-shipping scratch path; adopt only after Patron ratification proving three-script implementability and, when any ADR 0016 Decision / DEV-410 departure exists, an in-place measured ADR 0016 amendment/approval checkpoint before T011 (no-amendment only for zero departure). Extend Contoso `Test-RunMutation.ps1` to prove exact diff-only `mutate` selection and explicit `-WholeProject`. Keep entire `gates.mutation` unchanged; split the CRLF span defect to a follow-up; obtain Rigger-authorized external harness parity for **all three** scripts. Spec workspace produces documents only.

## Technical Context

**Language/Version**: PowerShell 7 gate scripts; pinned `dotnet-stryker` 5.0.0 (`.config/dotnet-tools.json`); C# production source unchanged by this ticket.
**Primary Dependencies**: Existing pins only — Stryker.NET via local tool, harness scripts. **No new packages** (brief §2.3; recon §7).
**Storage**: Runtime `mutation-timing.json` and Stryker reports under temp output roots outside the repo; durable copies under `specs/DEV-451/` evidence paths as Build records them. No database.
**Testing**: `scripts/Test-RunMutation.ps1` Contoso fixture (stubbed `dotnet stryker`); real `scripts/run-mutation.ps1` Core runs for Q2/Q3/acceptance with native gate-produced timing JSON; pre-adoption scratch experiments use versioned gate copies with honest provenance; applicable harness gate scripts including `./scripts/run-property-tests.ps1` with honest exit dispositions. No Gherkin.
**Target Platform**: Developer Windows/Linux workstations running linked worktrees; CI `-EvaluateReport` path unchanged; post-merge whole-Core job out of scope (DEV-409).
**Project Type**: Harness / gate-script performance and regression proof; external harness pack sync.
**Performance Goals**: Real DEV-444 local pre-PR mutation wall time strictly &lt; 1800 s (ticket AC) without omitting eligible suites or converting survivors/kills to timeouts.
**Constraints**: Never raise concurrency above half cores; pin and reuse actual concurrency integer; never change any `gates.mutation` value/tuning key; never enable `since` in linked worktrees; never hand-edit generated `stryker-config.json`; never lower thresholds; never imply renderer/install permission; preserve kill-share / file-span / scope / nonzero-tested safeguards; GATE NOT WIRED must not be shipped around; fail closed if no sound lever or missing mechanism facts.
**Scale/Scope**: three Core measurement cases + one acceptance ticket; Size bounded by ticket (script surfaces + evidence).

## Constitution Check

Gates checked against `.specify/memory/constitution.md` on this worktree. Brief: no new ADR now; no constitution departure; no owner structural checkbox (PRODUCT.md §5 / brief Handoff). Quill invents no amendment path.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | No new ports without ADR | **PASS** — no Core ports; script-only. |
| II / V (Failures / API) | Public API / ProblemDetails | **PASS** — no public API or DTO changes. |
| IV (Enrichment) | State machine untouched | **PASS** — enrichment domain out of scope. |
| VI (Observability) | Existing gate timing/receipts | **PASS** — reuse native `mutation-timing.json` + receipts; no new telemetry subsystem. |
| VII (Configuration / secrets / LocalPlay) | No secrets; LocalPlay gated | **PASS** — no secrets / LocalPlay / `Process.Start`; entire `harness.yml` mutation policy unchanged in final PR. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new domain terms; no CONTEXT.md change (brief). |
| IX (Test Pyramid) | Real proofs where required | **PASS** — fixture for config selection; real Core mutation runs for acceptance (fixture/scratch cannot substitute). |
| Governance / ADR | ADR when architectural choice changes | **PASS path** — in-place measured ADR 0016 amendment only after Patron ratification and Build evidence when any Decision/DEV-410 departure exists; checkpoint precedes T011; no-amendment only for zero departure; none drafted in Phase 2 (Q3, Q6; plan-challenge decision 4). |
| Pull Request Quality Gates (constitution Development Workflow) | Honest dispositions; required gates; never lower thresholds | Phase B / Build: report actual script exits (PASS/FAIL/SKIPPED/SKIP/Could not run) distinctly; N/A is mutation-only; Core measurement runs mandatory even if script-only scope-empty on ordinary C# mutation (plan-challenge decision 7). |
| Static-Analysis Gates (constitution) | Roslyn / complexity / InspectCode applicability | Applicable scripts after implementation; scoped to changed C# when present; actual exits only; never relabel Could not run as PASS (plan-challenge decision 7). |

### Complexity Tracking

No unjustified constitution violation. No Complexity Tracking rows.

## Approach

- **Phase A (this draft)**: spec/plan/tasks + receipt + requirements checklist from closed brief + plan-challenge round 1 decisions; grill not re-run; analyze/plan challenge remain Keel’s; no PLAN FROZEN claim.
- **Operational prerequisites (Bernstein/Rigger)**: restore dirty `harness.yml` Tests.Common exclusion to intake HEAD with verified receipt (Keel records in Build CONCLUSIONS copy); obtain verified CRLF follow-up YouTrack ID after Order repair (`FOLLOW-UP-TICKETS.md`). Spec records blockers; does not invent IDs or cleanup completion. T021/T022 before T000; Contoso does not bypass T022 (plan-challenge decision 6).
- **Measure first (Build)**: Bernstein/Rigger provision detached replay at `00799914` / `6fcbd1ff` / `b25ecb16`; record immutable base/head, hashes, mutate files, eligible projects, overlay versions/hashes, pinned concurrency, config timeout; run live native baselines; archive timing JSON + receipts + status fields; list eligible test projects (Core → UnitTests + Evaluation). Missing mechanism → `needs recon` (plan-challenge decision 3).
- **Experiment second (scratch)**: propose concrete non-DEV-435 candidate; prove three-script implementability; obtain recon+experimental authorization before rejected/unsupported modes; specify reversible versioned gate-copy path with durable equivalent receipts + EvaluateReport; matched scratch runs; per-mutant status matrix; GATE NOT WIRED remains in shipping scripts; Patron ratification before document Phase 4 / brief adoption; when any ADR 0016 Decision / DEV-410 departure exists, in-place amendment/approval lands before T011 (no-amendment only for zero departure) (plan-challenge decisions 1, 4).
- **Regression proof**: Contoso changed/unchanged siblings after T000; assert `mutate` + `since.enabled=false` + WholeProject; full `Test-RunMutation.ps1` suite; must not mutate frozen replay inputs.
- **Implement within edit boundary**: three ticket-named scripts as needed overall. Independent Q5 Contoso (T007–T009) may proceed after T000 without adopting the candidate and without waiting for T010 (still after T021/T022). Ratified lever adoption/implementation (T011) in `scripts/run-mutation.ps1` and, if required, `scripts/_harness-config.ps1` only after the T010 ADR checkpoint. Unnamed renderer/install/`harness.yml`/consumer edits STOP for cited Patron ruling — **no implied renderer permission**. Never hand-edit `stryker-config.json`.
- **Upstream + evidence**: Bernstein arranges Rigger authorized external session; mirror changed scripts; confirmed-or-merged origin/main SHA/parity for **all three** scripts including unchanged; PR before/after table with timeout totals; Size/review under Q6 bar; awaiting-merge gated on T014 (plan-challenge decision 5).

## Project Structure

```text
specs/DEV-451/
  brief.md, CONCLUSIONS.md, FOLLOW-UP-TICKETS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md
  # Build later: bounded measurement/receipt evidence (paths chosen at Build; not claimed present)

scripts/run-mutation.ps1              (conditional lever implementation; retain safeguards; shipping GATE NOT WIRED intact until T011)
scripts/_harness-config.ps1           (conditional support only if lever requires; policy unchanged)
scripts/Test-RunMutation.ps1          (Contoso diff-only / WholeProject cases; full suite)

# External repo (not in this worktree):
dotnet-agent-harness/packs/dotnet/scripts/{run-mutation,_harness-config,Test-RunMutation}.ps1
  (Rigger authorized session; all-three confirmed-or-merged origin/main parity)

# Unchanged in DEV-451:
harness.yml gates.mutation            (entire block immutable = intake HEAD; dirty removal excluded)
stryker-config.json                   (generated; never hand-edit; no implied renderer permission)
.gitattributes, src/**/*.cs, web/     (no implementation changes)
docs/adr/0016-stryker-mtp-runner.md   (conditional in-place amendment after ratification when departure exists; lands before T011; no-amendment only for zero departure; does not gate Contoso T007–T009)
```

**Layout decisions**:

- No research/data-model/contracts artifacts — decisions frozen in brief; no public contract.
- Quill owns spec/plan/tasks/receipt/checklist. Quill does not edit `brief.md` / `CONCLUSIONS.md` / ADR bodies / Rigger FOLLOW-UP-TICKETS. Keel owns analyze / plan challenge and records verified operational receipts in Build CONCLUSIONS.
- `packs/` absent here (recon §5:91) — upstream parity is a Build delivery dependency via authorized Rigger session.
- Document Phase 4 = ADR checkpoint T010 → lever T011; brief “Phase 5 / adoption” maps to post-T010 adoption — always T010-before-T011. Task IDs are labels; T021/T022 intentionally precede T000.

## Test Strategy

| Surface | Coverage |
|---|---|
| Diff-only mutate (US3) | Contoso: changed + unchanged sibling `.cs`; dry-run + stubbed real; assert generated `mutate` == changed file only; `since.enabled=false`; `-WholeProject` full target; existing cases stay green — `scripts/Test-RunMutation.ps1`; must not mutate frozen replay inputs |
| Baseline / candidate (US1–US2) | Provisioned historical replay + real `scripts/run-mutation.ps1` (native timing JSON) for baselines; reversible scratch gate copies for pre-adoption candidates; archive receipts + per-mutant status matrices + timeout totals |
| Acceptance (US4) | Validate single T012 DEV-444 native-gate artifact wall time &lt; 1800 s; eligible suites intact; integrity matrix; no best-of second sample |
| Policy (US5) | Final PR: `git diff` shows no `gates.mutation` change; dirty cleanup receipt recorded by Keel |
| Upstream (US5) | Exact SHAs + parity receipts vs `dotnet-agent-harness` origin/main for all three scripts |
| Harness gates | Applicable scripts after implementation including `./scripts/run-property-tests.ps1`; SKIPPED (scope-empty) / SKIP / Could not run never reported as PASS; N/A mutation-only; Core measurement runs remain mandatory |

No Gherkin. Property tests listed explicitly; report actual exits.

## Gates (Build — after Gate 1 and implementation)

1. Full `scripts/Test-RunMutation.ps1` suite green.
2. Applicable `./scripts/run-roslyn-analyzers.ps1`, cyclomatic, InspectCode, `./scripts/run-property-tests.ps1`, format, vulnerable-packages — honest actual dispositions (script-only C# absence may scope-empty some gates).
3. Real Q2/Q3/DEV-444 mutation evidence with native gate-produced timing JSON (never replaced by stub or scratch success).
4. Upstream origin/main parity receipts for all three scripts via authorized Rigger session.
5. Review under Q6 closing bar; two remediation rounds max; unresolved blockers halt to Bernstein.

Phase A runs none of these and claims no gate result.

**Numeric provenance**: `gates.mutation` snapshot from harness.yml (recon); `additional-timeout: 90000` from stryker-config / ADR 0016; strict under-30-minute / 1800 s target from ticket AC. T001 records actual runtime-read values and stops on incompatible drift (plan-challenge decision 7).

## Risks

- **No sound lever / missing mechanism**: fail closed or `needs recon` — ticket blocks rather than relaxing policy, inventing replay commands, or omitting Evaluation (Q3; plan-challenge decisions 1, 3).
- **Verdict inflation**: Survived/Killed→Timeout under aggressive concurrency/coverage — mitigated by per-mutant status matrix, pinned concurrency, quiet-machine control, and half-core ceiling (DEV-435 history; Q3; plan-challenge decision 2).
- **GATE NOT WIRED bypass**: shipping relaxation of mtp/non-off — forbidden; scratch-only until T010/T011 (recon §6; plan-challenge decision 1).
- **Dirty harness.yml**: accidental policy removal in PR — mitigated by Q1 exclusion + AC 3 proof (recon §1).
- **CRLF conflation**: line-ending churn spoiling timing — mitigated by Q4 split; fixture honesty (recon §6).
- **Upstream overwrite / incomplete parity**: local-only script edits or changed-only receipts — mitigated by mandatory all-three origin/main parity via authorized Rigger session (ticket AC5; plan-challenge decision 5).
- **Implied renderer expansion**: candidate needing unnamed install/config — STOP for cited Patron ruling (plan-challenge decision 4).
- **Operational blockers**: Order-sequence prevents CRLF ticket create; cleanup receipt pending — Spec does not invent IDs; Bernstein coordinates (brief Handoff).
- **Pickup drift**: Build refreshes recon before coding; T001 stops on incompatible drift; does not treat Phase A recon as clearance for lever identity.

## Ordering

See `tasks.md`. Sequence:

```text
Phase 0   Operational receipts (dirty harness restore; CRLF follow-up ID) — Bernstein/Rigger; block Build T000+ (Contoso does not bypass T022)
Phase A   Quill spec/plan/tasks (this draft + plan-challenge repair) → Keel analyze / plan challenge → owner Gate 1 / spec merge
Phase B   Build pickup / drift recheck
Phase 1   US1 provisioned historical-replay baselines (three cases)
Phase 2   US2 scratch candidate experiment → Patron ratification (+ ADR amendment authorization if departure)
Phase 3   US3 Contoso diff-only / WholeProject fixture (after T000; must not claim lever success; must not mutate frozen replay inputs)
Phase 4   US4 conditional in-place ADR 0016 amendment/approval (or zero-departure no-amendment) → implement ratified lever (T011) → matched candidates → validate T012 DEV-444 sample
Phase 5   US5 Rigger authorized all-three-script parity + PR table + policy proof + applicable gates (incl. property) + review (Q6 bar)
```

Gate 1 / spec merge precede Build implementation. Spec workspace does not run code/tests or create a Build branch (brief Task ordering). Document Phase 4 is the ADR/T011 checkpoint; brief Phase 5 adoption maps after T010 — always T010-before-T011.
