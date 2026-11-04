# Feature Specification: Pre-PR mutation under 30 minutes

**Feature Branch**: `feature/451-spec`
**Created**: 2026-11-04
**Status**: gate1: provisional
**Input**: Ticket DEV-451 (parent DEV-100); frozen `brief.md` (Q1–Q6 + plan-challenge round 1 decisions 1–7); `CONCLUSIONS.md`; `FOLLOW-UP-TICKETS.md`; Maestri note `recon-DEV-451`; adjudication-DEV-451; `specs/PRODUCT.md`

## Owner question — Gate 1

None. Brief Q1–Q6 and `CONCLUSIONS.md` record no ticket contradiction and no constitution departure; no structural owner checkbox is carried (`brief.md` Operational / Handoff; `PRODUCT.md` §5). Operational dependencies (dirty `harness.yml` restoration receipt; verified CRLF follow-up ID) remain Bernstein/Rigger-owned and do not become Gate 1 checkboxes. Plan-challenge round 1 Patron ACCEPT 1–7 refines Q1–Q6; it does not approve Gate 1.

## Scope

**In scope** (ticket Scope & Technical Design items 1–7; ACs; brief Q1–Q6 + plan-challenge decisions 1–7):

1. **Measure first (historical replay)**: Bernstein/Rigger-provisioned detached replay at each historical SHA for DEV-444 (`00799914`), DEV-448 (`6fcbd1ff`), and DEV-333 (`b25ecb16`); record immutable comparison base/head, code/test hashes, mutate files, eligible test projects, machine, actual concurrency integer (≤ half cores), actual unchanged timeout from config, and versioned gate/tool/config overlay versions+hashes (recon §3 overlay precedent). Overlay support only; freeze code/tests; baseline/candidate differ only by lever. Capture native gate-produced `mutation-timing.json`, mutation receipts, wall time, per-project mutant counts, eligible test projects, timeout totals, and per-mutant statuses. No invented historical timings. Reuse the same frozen DEV-444 replay case for post-adoption matched runs and acceptance validation (Q2; recon §3; plan-challenge decision 3).
2. **Patron-picked acceptance ticket**: DEV-444 must complete a real local pre-PR mutation run **strictly under 30 minutes** (wall time &lt; 1800 s — ticket AC) with eligible suites intact and mutation integrity checks satisfied; T013 validates the single authoritative T012 DEV-444 sample (no best-of second sample) (Q2; plan-challenge decision 2).
3. **Measurement-gated lever**: keep `test-runner: mtp` / `coverage-analysis: off` as control; evaluate a concrete non-DEV-435 lever only after baselines via a feasible reversible **non-shipping** scratch path; treat per-test coverage strictly as a bounded experiment (ADR 0016 prior failure: 42 min / 45 restarts / survivor→timeout). Explicitly name `mtp`/non-`off` as **GATE NOT WIRED** — never silently bypass in shipping scripts. Retrying historically rejected `perTest`/`all` or unsupported runner/mutator modes needs targeted recon **and** explicit Patron experimental authorization. Fail closed if no lever meets both speed and verdict-integrity bars, or if mechanism facts are missing (`needs recon`). Explicit Patron ratification proving three-script implementability, then when any ADR 0016 Decision / DEV-410 departure exists an in-place measured ADR 0016 amendment/approval checkpoint (T010) before adoption/implementation (T011); **no-amendment only for zero departure** (Q3; plan-challenge decisions 1, 4).
4. **Policy unchanged**: final PR leaves **entire** `gates.mutation` in `harness.yml` unchanged (threshold 80, `maxKillShare` 50, `maxKillFileSpan` 15, `killFileSpanExempt`, all four exclusions including `LamuFlix.Tests.Common`, and every tuning key — no carve-out). Numeric snapshot provenance is harness.yml / stryker-config / ADR 0016 as cited in recon; strict &lt;30m target provenance is the ticket AC. Pre-existing dirty Tests.Common exclusion removal is out of DEV-451 and must be restored to intake HEAD before Build implementation (Q1; AC 3; plan-challenge decisions 4, 7).
5. **Diff-only / WholeProject proof**: extend Contoso fixture in `scripts/Test-RunMutation.ps1` with changed + unchanged production `.cs` siblings in one project; assert exact changed-file-only generated `mutate`, `since.enabled=false`, and full target only with explicit `-WholeProject`, covering dry-run and stubbed real invocation; preserve all existing cases; full suite green. Contoso may proceed after T000 without waiting for T010 and without adopting the candidate; it does **not** bypass T021/T022 (Q5; AC 2; plan-challenge decision 6).
6. **CRLF span gap split**: leave `.gitattributes` and `src/**/*.cs` unchanged; Rigger-verified YouTrack follow-up linked before handoff; new fixture must not claim to fix PR #297 / `MmrSelector.cs` span defect (Q4).
7. **Upstream sync (all three scripts)**: Bernstein arranges Rigger’s authorized external harness session; Build does not escape its worktree or bypass guards. Mirror changed scripts; record confirmed-or-merged `origin/main` status + exact SHAs + parity for **all three** ticket-named scripts including unchanged counterparts before awaiting-merge/delivery (Q5; AC5; plan-challenge decision 5).
8. **PR evidence**: before/after table for the three Core changes in the PR description, linking durable timing/receipt artifacts and timeout totals (ticket AC; plan-challenge decision 2).

**Out of scope** (ticket; brief frozen scope):

- Post-merge whole-Core job (DEV-409).
- Any change to mutation threshold or any other `gates.mutation` value/tuning key.
- Production C# / `web/` implementation changes; line-ending normalization of `src/**/*.cs`.
- Raising concurrency above `floor(ProcessorCount/2)`; enabling Stryker `since` in linked worktrees; omitting eligible test projects (`LamuFlix.UnitTests` + `LamuFlix.Evaluation` for Core) to claim speed.
- Hand-editing generated `stryker-config.json`; implied renderer/install/`harness.yml` edit permission without a cited Patron stop/ruling.
- Shipping-script bypass of GATE NOT WIRED; best-of acceptance reruns that erase a valid target miss.
- New NuGet/npm dependency, project, top-level folder, architectural layer, schema, public API/DTO/OpenAPI/web contract, secrets, `Features:LocalPlay`, or `Process.Start`.
- Drafting an ADR amendment before measured Build evidence; inventing a new ADR number when amending existing ADR 0016 in place (Q3, Q6; plan-challenge decision 4).

**Precedence**: Ticket Scope & Technical Design is already approved authority (`specs/PRODUCT.md` §3). Brief Q1–Q6, plan-challenge decisions 1–7, and `CONCLUSIONS.md` govern boundaries, ordering, loop discipline, and delivery constraints. This Spec workspace drafts documents only and claims no measured timing success, lever adoption, Gate 1, PLAN FROZEN, or implementation clearance.

## What the User Will See *(mandatory)*

Nothing visible: DEV-451 changes developer/mutation-gate tooling and evidence only; the Compose-stack product UI is unchanged.

## User Scenarios & Testing *(mandatory)*

The actor is a developer or pipeline agent running the local pre-PR mutation gate (`scripts/run-mutation.ps1`) on a task branch.

### User Story 1 - Matched baseline measurement (Priority: P1)

As a developer preparing a speedup claim, I need live baseline mutation runs for DEV-444, DEV-448, and DEV-333 on Bernstein/Rigger-provisioned detached historical replay checkouts so wall time, mutant counts, eligible test projects, timeout totals, and per-mutant statuses are auditable before any lever is chosen.

**Why this priority**: ticket Scope item 1 and ACs require measure-first; historical `mutation-timing.json` / Core receipts are absent from the repo (recon §3).

**Independent Test**: three baseline runs on provisioned replay produce durable native gate-produced timing JSON + receipts with the required fields; each pair holds machine, pinned concurrency, timeout, overlay versions, and code/test tree constant.

**Acceptance Scenarios**:

1. **Given** Bernstein/Rigger-provisioned detached replay at DEV-444 (`00799914`), DEV-448 (`6fcbd1ff`), and DEV-333 (`b25ecb16`) with recorded base/head, code/test hashes, mutate files, eligible projects, overlay versions/hashes, pinned concurrency, and config timeout **When** baseline mutation runs execute **Then** each run records wall time, per-project mutant counts, eligible test projects, timeout totals, per-mutant statuses, native `mutation-timing.json`, and a mutation receipt without inventing prior numbers; Build writes only inside assigned roots (Q2; plan-challenge decision 3).
2. **Given** a Core change **When** eligible test projects are listed **Then** both `LamuFlix.UnitTests` and `LamuFlix.Evaluation` appear (recon §4; brief Approach).
3. **Given** missing historical timings or missing supported replay mechanism in the worktree **When** planning proceeds **Then** Build obtains live baselines via the provisioned path, or stops with `needs recon` — never fabricates evidence or invents checkout commands (Q2; Q6; plan-challenge decision 3).

---

### User Story 2 - Measurement-gated lever selection (Priority: P1)

As Patron and Build, I need a concrete non-DEV-435 speedup candidate evaluated against the Q2 baselines via an authorized reversible non-shipping scratch path with matched speed and verdict-integrity evidence, then explicitly ratified (and ADR 0016 amended in place if any Decision/DEV-410 departure) before adoption — or fail closed.

**Why this priority**: ticket Scope items 2–4; DEV-435 levers are exhausted; ADR 0016 bars silent coverage-analysis adoption; shipping GATE NOT WIRED must not be bypassed (Q3; recon §6).

**Independent Test**: scratch candidate vs control comparison artifacts show wall-time change and per-mutant status-matrix integrity; Patron ratification records three-script implementability and ADR departure/no-departure before T010→T011; shipping scripts unchanged until then.

**Acceptance Scenarios**:

1. **Given** Q2 baselines **When** a candidate lever is proposed **Then** it is outside the exhausted DEV-435 set, proven implementable within the three ticket-named scripts, and is not silently adopted on wall time alone; unnamed renderer/install/config/consumer edits STOP for cited Patron ruling (Q3; plan-challenge decisions 1, 4, 7).
2. **Given** `mtp` / `coverage-analysis: off` as control and `mtp`/non-`off` as GATE NOT WIRED **When** per-test coverage or another rejected/unsupported mode is tried **Then** targeted recon plus explicit Patron experimental authorization exist first; the experiment uses versioned gate copies with durable equivalent receipts and actual EvaluateReport integrity; shipping scripts stay unchanged until T010/T011 (Q3; plan-challenge decision 1).
3. **Given** a candidate that converts survivors or kills to timeouts, shows unexplained timeout growth, unmatched mutant populations, omits eligible suites, raises concurrency above half cores, or relaxes any `gates.mutation` key **When** the per-mutant status matrix is inspected **Then** the candidate is rejected and invalid runs archived; fail closed if none succeeds (Q3; plan-challenge decision 2).
4. **Given** a sound candidate **When** adoption/implementation approaches **Then** explicit Patron ratification exists; if any ADR 0016 Decision or DEV-410 departure exists, the in-place measured ADR 0016 amendment/approval checkpoint completes before T011; no-amendment is recorded only for zero departure — none drafted in Phase 2 (Q3, Q6; plan-challenge decision 4).

---

### User Story 3 - Diff-only and explicit WholeProject regression proof (Priority: P1)

As a gate maintainer, I need the Contoso fixture to prove that a default local run mutates only files in the diff (`since.enabled=false`) and that `-WholeProject` remains the only path to full-target mutation, without claiming to fix the CRLF span defect and without mutating frozen replay inputs.

**Why this priority**: ticket Scope item 5 / AC 2; recon §5 coverage gap vs existing cases 6/9/12 (Q5).

**Independent Test**: new Contoso case(s) with changed + unchanged sibling `.cs` files; dry-run and stubbed real invocation; full `scripts/Test-RunMutation.ps1` suite green; after T000 (hence after T021/T022); independent of T010/T011.

**Acceptance Scenarios**:

1. **Given** one project with a changed and an unchanged production `.cs` sibling **When** default (diff-scoped) dry-run / stubbed invocation runs **Then** generated `mutate` lists exactly the changed file and `since.enabled=false` (Q5).
2. **Given** the same tree **When** `-WholeProject` is used **Then** the full target is selected (Q5).
3. **Given** existing classification/scope/share/span/runner/timeout/timing cases **When** the suite runs **Then** all remain green (Q5).
4. **Given** the PR #297 / `MmrSelector.cs` CRLF span gap **When** the new case is described **Then** it does not claim that defect is fixed (Q4).

---

### User Story 4 - Ratified lever implementation and under-30-minute acceptance (Priority: P1)

As a developer on a Core ticket, I need the ratified lever implemented only in the allowed scripts so a real DEV-444 pre-PR run finishes under 30 minutes without corrupting mutation verdicts or changing `gates.mutation`, validating the single T012 acceptance sample.

**Why this priority**: ticket primary AC; depends on US1–US2 evidence and Q1 policy preservation.

**Independent Test**: T013 validates the authoritative T012 DEV-444 artifact with wall time &lt; 1800 s, eligible suites, and integrity checks; three-row before/after candidate table with timeout totals; entire `gates.mutation` diff empty.

**Acceptance Scenarios**:

1. **Given** Patron-ratified lever and completed ADR 0016 checkpoint (in-place amendment landed when any departure exists, or explicit zero-departure no-amendment) **When** implementation lands **Then** edits are limited to `scripts/run-mutation.ps1`, `scripts/_harness-config.ps1`, and/or `scripts/Test-RunMutation.ps1` as required; `stryker-config.json` is never hand-edited; entire `harness.yml` `gates.mutation` is unchanged; no unnamed renderer/install expansion (Q1, Q3, Q5; plan-challenge decision 4).
2. **Given** the implemented lever **When** matched candidate runs execute for the three frozen replay cases **Then** native gate-produced timing JSON/receipts and per-mutant status matrices (stable source/mutator identity) reject inflated passes; timeout totals are archived for the PR table (Q2, Q3; plan-challenge decision 2).
3. **Given** the authoritative T012 DEV-444 sample **When** T013 validates acceptance **Then** wall time is strictly under 1800 seconds with eligible suites intact; no second best-of sample is run to erase a valid miss; replacement requires documented invalidation plus Patron ruling with all attempts archived (Q2; ticket AC; plan-challenge decision 2).
4. **Given** concurrency and integrity rules **When** the lever runs **Then** the same pinned concurrency integer (≤ half cores), unchanged config timeout, same runner, and quiet-machine control are reused; all safeguards remain; DEV-444 recommendation-test span exemptions weaken that signal so kill-share and per-mutant statuses are inspected (Q1, Q3; plan-challenge decision 2).

---

### User Story 5 - Upstream parity, follow-up link, and delivery evidence (Priority: P2)

As reviewers and the harness owner, I need verified `dotnet-agent-harness` origin/main parity for all three ticket-named scripts, a linked CRLF follow-up ticket, dirty-policy cleanup receipt, and a three-row before/after PR table before delivery.

**Why this priority**: ticket Scope item 7 and ACs; operational dependencies in brief Handoff status.

**Independent Test**: exact SHAs + parity receipts on harness `origin/main` for all three scripts (changed and unchanged); FOLLOW-UP-TICKETS / CONCLUSIONS carry verified CRLF ID; PR body table links durable artifacts and timeout totals; `git diff` shows no `gates.mutation` change.

**Acceptance Scenarios**:

1. **Given** Bernstein-arranged Rigger authorized external harness session **When** delivery or awaiting-merge is claimed **Then** all three ticket-named scripts have confirmed-or-merged `origin/main` status with exact SHAs and parity receipts — local copies or a pending PR are insufficient; Build does not bypass guards (Q5; AC5; plan-challenge decision 5).
2. **Given** Q4 split **When** handoff proceeds **Then** a Rigger-verified YouTrack follow-up ID is linked in `specs/DEV-451/` artifacts (Q4; `FOLLOW-UP-TICKETS.md`).
3. **Given** Q1 policy cleanup **When** Build implementation starts **Then** dirty Tests.Common exclusion removal is restored to intake HEAD with an accepted Rigger evidence receipt recorded by Keel in the Build CONCLUSIONS copy (Q1; plan-challenge decisions 6–7).
4. **Given** the three measured cases **When** the PR is opened **Then** the description includes a before/after table linking durable artifacts and timeout totals (ticket AC; plan-challenge decision 2).

### Edge Cases

- No historical Core `mutation-timing.json` in-repo — live Build measurement via provisioned replay is required; missing mechanism → `needs recon` (recon §3; plan-challenge decision 3).
- Core eligible suites include Evaluation — omitting it to shorten wall time is forbidden (recon §4; brief Approach).
- Per-test coverage on MTP previously took 42 minutes with restart/timeout inflation — wall-time-only acceptance is forbidden; GATE NOT WIRED must not be shipped around (ADR 0016; Q3; plan-challenge decision 1).
- Stryker `since` stays disabled in linked worktrees (charter; DEV-435; Q3).
- CRLF Windows checkout can make span offsets miss mutants — tracked separately; not fixed here (Q4; recon §6).
- Script-only ordinary diffs may be mutation scope-empty / SKIPPED for production C# — that disposition must not replace the ticket’s explicit Core measurement runs (brief Test strategy).
- Experimental scratch receipts must carry honest provenance and must not be claimed as native shipping-gate success (plan-challenge decision 1).
- Fail closed if no lever meets both bars — ticket does not close on fixture-green alone (Q3, Q6).
- Anything outside frozen scope is a follow-up issue, not a finding in this round (Q6).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Build MUST capture matched baseline (then candidate) mutation evidence for DEV-444 (`00799914`), DEV-448 (`6fcbd1ff`), and DEV-333 (`b25ecb16`) on Bernstein/Rigger-provisioned detached historical replay with recorded immutable base/head, code/test hashes, mutate files, eligible test projects, machine, pinned concurrency integer, config timeout, and overlay versions/hashes; including wall time, per-project mutant counts, timeout totals, per-mutant statuses, **native gate-produced** `mutation-timing.json`, and mutation receipts; baseline/candidate differ only by lever; invent no historical numbers; missing supported mechanism → `needs recon` before run; reuse frozen DEV-444 case for post-adoption runs (Q2; ticket Scope item 1; plan-challenge decisions 1, 3).
- **FR-002**: Patron-picked DEV-444 MUST complete a real local pre-PR mutation run with wall time strictly under 1800 seconds (ticket AC), eligible Core suites (`LamuFlix.UnitTests` and `LamuFlix.Evaluation`) intact, and mutation integrity checks satisfied; T013 validates the single authoritative T012 sample without a best-of second run; replacement only after documented invalidation plus Patron ruling with all attempts archived (Q2; ticket AC; plan-challenge decision 2).
- **FR-003**: Lever selection MUST be measurement-gated: control remains `mtp` / `coverage-analysis: off`; `mtp`/non-`off` is GATE NOT WIRED and MUST NOT be silently bypassed in shipping scripts; candidates must be outside exhausted DEV-435 levers and proven implementable within the three ticket-named scripts; per-test coverage and other rejected/unsupported modes require targeted recon + explicit Patron experimental authorization before experiment; pre-adoption experiments use reversible versioned gate-copy scratch paths with durable equivalent receipts and actual EvaluateReport integrity, leaving shipping scripts unchanged until T010/T011; adoption requires explicit Patron ratification and, when any ADR 0016 Decision or DEV-410 departure exists, an in-place measured ADR 0016 amendment/approval checkpoint before T011 (**no-amendment only for zero departure**); fail closed if speed or verdict integrity fails or mechanism facts/authorization are missing (Q3; plan-challenge decisions 1, 4).
- **FR-004**: Concurrency MUST stay at or below `floor(ProcessorCount/2)` with the actual integer pinned and reused; entire `gates.mutation` (threshold 80, `maxKillShare` 50, `maxKillFileSpan` 15, `killFileSpanExempt`, all four exclusions including `LamuFlix.Tests.Common`, and every tuning key) and `additional-timeout: 90000` (provenance: harness.yml / stryker-config / ADR 0016 per recon) MUST remain; final PR MUST NOT change `gates.mutation` values; dirty Tests.Common exclusion removal MUST be excluded from this ticket and restored before implementation; T001 records actual runtime-read values and stops on incompatible pickup drift (Q1; ticket Scope items 3–4; AC 3; plan-challenge decisions 2, 4, 7).
- **FR-005**: `-WholeProject` MUST remain an explicit flag; default runs MUST mutate only diff-scoped files with `since.enabled=false` (Q5; ticket Scope item 5).
- **FR-006**: `scripts/Test-RunMutation.ps1` MUST gain Contoso coverage with changed + unchanged sibling `.cs` files asserting exact `mutate` equality to the changed file, `since.enabled=false`, and full-target selection only under `-WholeProject`, for dry-run and stubbed real invocation; existing cases MUST stay green; full suite MUST pass; Contoso MUST NOT mutate frozen replay inputs (Q5; AC 2; plan-challenge decision 7).
- **FR-007**: Implementation code edits MUST be limited to the three ticket-named scripts as needed (`scripts/run-mutation.ps1`, `scripts/_harness-config.ps1`, `scripts/Test-RunMutation.ps1`) plus bounded evidence/docs under `specs/DEV-451/`; unnamed renderer/install/`harness.yml`/consumer edits STOP for cited Patron ruling (no implied permission); a conditional in-place measured ADR 0016 amendment after ratification MUST precede ratified lever adoption/implementation (T011) when any Decision/DEV-410 departure exists (otherwise leave ADR 0016 unchanged only for zero departure); independent Q5 Contoso fixture work (T007–T009) MAY proceed after T000 without adopting the candidate and without waiting for T010, but MUST NOT bypass T021/T022; NEVER hand-edit `stryker-config.json`; do not change `.gitattributes` or `src/**/*.cs` (Q3, Q4, Q5; plan-challenge decisions 4, 6).
- **FR-008**: Bernstein MUST arrange Rigger’s authorized external harness session; Build MUST NOT escape its worktree or bypass guards; changed scripts MUST be mirrored to `dotnet-agent-harness/packs/dotnet/scripts`; confirmed-or-merged `origin/main` status + exact SHAs + parity receipts MUST be recorded for **all three** ticket-named scripts including unchanged counterparts before awaiting-merge/delivery; pending PR/local copy is insufficient (Q5; ticket AC5; plan-challenge decision 5).
- **FR-009**: PR #297 / `MmrSelector.cs` CRLF span gap MUST be tracked via Rigger-verified YouTrack follow-up linked in `specs/DEV-451/` before handoff; the new fixture MUST NOT claim that defect fixed (Q4).
- **FR-010**: PR description MUST include a before/after table for the three Core changes linking durable timing/receipt artifacts and timeout totals (ticket AC; plan-challenge decision 2).
- **FR-011**: Preserve nonzero-tested, actual kills, scope containment, score, timeout, kill-share, and file-span safeguards; compare baseline/candidate via stable per-mutant identity (source location + mutator/replacement, not run-local numeric IDs) and a status matrix including Survived-to-Timeout and Killed-to-Timeout counts; reject positive conversions, unexplained timeout growth, and unmatched mutant populations; on DEV-444 treat recommendation-test span exemptions as a weak signal and inspect kill-share/per-mutant statuses; archive invalid runs with rationale; inspect timeout distribution rather than accepting Stryker’s score alone; reject speedups from missing scope/tests, policy relaxation, or verdict inflation; reuse pinned concurrency, unchanged timeout, same runner, and quiet-machine control (brief Approach / Test strategy; Q3; plan-challenge decision 2).
- **FR-012**: Closing bar: zero unresolved accepted Critical/High/Medium findings; Low findings receive explicit dispositions; all applicable required gates (including `./scripts/run-property-tests.ps1`) and ticket ACs satisfied; report actual script exits only — Scope-empty / opt-out / Could not run never relabeled PASS; **N/A is mutation-only**; under-30-minute acceptance is a real measured native-gate run, not the script fixture or scratch experiment; maximum two review/remediation rounds then halt to Bernstein; cite constitution Pull Request Quality Gates and Static-Analysis Gates (Q6; plan-challenge decision 7).

### Key Entities

- **Baseline / candidate pair**: matched mutation runs on the same frozen historical-replay checkout, code/test tree, machine, pinned concurrency, timeout, runner, and overlay versions, differing only by the control vs candidate lever configuration.
- **Historical replay checkout**: Bernstein/Rigger-provisioned detached worktree/checkout at a frozen historical SHA with overlay-only support files; Build does not invent commands or write outside assigned roots.
- **mutation-timing.json**: runtime gate timing artifact (`generatedUtc`, total `durationSeconds`, concurrency, threshold, per-project duration/mutants/tested/score/passed) written by the native gate; mandatory for T002 and T012/T013; experimental scratch may produce equivalent durable receipts with honest provenance, never claimed as native shipping-gate success.
- **Per-mutant status matrix**: baseline↔candidate comparison keyed by stable source location + mutator/replacement identity, including Survived-to-Timeout / Killed-to-Timeout counts and timeout totals.
- **Eligible test projects (Core)**: `LamuFlix.UnitTests` and `LamuFlix.Evaluation` via direct `ProjectReference` filtering in `Get-EligibleTestProjects`.
- **Diff-scoped mutate list**: generated Stryker config `mutate` array restricted to changed production `.cs` files; `since.enabled=false`.
- **CRLF span follow-up**: separate YouTrack ticket for PR #297 / `MmrSelector.cs` LF/CRLF span-offset defect (ID pending Order repair per `FOLLOW-UP-TICKETS.md`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Three matched Core cases have durable before/after timing and receipt evidence from provisioned historical replay without invented baselines (ticket AC; Q2; plan-challenge decision 3).
- **SC-002**: Authoritative T012 DEV-444 local pre-PR mutation wall time is strictly under 1800 seconds with eligible suites and integrity checks green; T013 validates that sample without best-of reruns (ticket AC; Q2; plan-challenge decision 2).
- **SC-003**: Contoso fixture proves exact diff-only `mutate` selection and explicit `-WholeProject` full-target selection; full `Test-RunMutation.ps1` suite passes (ticket AC; Q5).
- **SC-004**: Final PR diff leaves entire `gates.mutation` in `harness.yml` unchanged (ticket AC 3; Q1).
- **SC-005**: All three ticket-named scripts are verified confirmed-or-merged on `dotnet-agent-harness` `origin/main` with exact SHAs/parity receipts (including unchanged counterparts), or awaiting-merge/delivery is blocked (ticket AC5; Q5; plan-challenge decision 5).
- **SC-006**: CRLF follow-up ID is linked before handoff; `.gitattributes` / `src/**/*.cs` unchanged; fixture does not claim the span defect fixed (Q4).
- **SC-007**: Applicable gates (including property tests) report honest actual exits; closing bar and two-round cap from Q6 are met; no measured success, PLAN FROZEN, or Gate 1 is claimed by Phase 2 documents alone.

## Assumptions

- No taste ruling or new domain term was needed; no `ASSUMPTIONS.md` or `CONTEXT.md` change is required by this grill (brief Files and artifact boundaries).
- No new ADR is drafted in Phase 2; any in-place ADR 0016 amendment waits for Build evidence; when any Decision/DEV-410 departure exists it must land before ratified lever adoption/implementation (T011); no-amendment only for zero departure; Contoso T007–T009 remain independent of that checkpoint but not of T021/T022 (Q3, Q5, Q6; plan-challenge decisions 4, 6).
- Operational dependencies (dirty `harness.yml` restoration receipt; verified CRLF YouTrack ID after Order repair) are Bernstein/Rigger-owned and may still be pending at Spec drafting time; Spec records them as blockers, not invented completions (`FOLLOW-UP-TICKETS.md`; brief Handoff). Verified T021 receipt may be recorded by Keel as CONCLUSIONS owner in the Build copy after evidence arrives.
- Recon baseline HEAD `010e0cb63a52e3689d9f246c4179e9bb30959738` is planning authority; Build pickup refreshes facts as needed without inventing lever choice; T001 stops on incompatible drift.

## Clarifications

All grill categories were resolved by Patron Q1–Q6 before drafting; plan-challenge round 1 Patron ACCEPT 1–7 further refines execution mechanics. `/speckit-clarify` scan: Functional Scope, Domain/Data, Interaction, NFR, Integration/Dependencies, and Out-of-scope — **Clear**. No `[NEEDS CLARIFICATION]` markers remain. No owner Gate 1 checkbox. Concrete lever identity remains intentionally undecided until Build measurement (Q3) — not a drafting gap.

## Traceability

| Source | Encoded as |
|---|---|
| Ticket Scope 1–7 / ACs | Scope; US1–US5; FR-001–FR-012; SC-001–SC-007 |
| Q1 policy / dirty harness | Owner question (none); FR-004; SC-004; US5 |
| Q2 measurement set / DEV-444 | US1; US4; FR-001–FR-002; SC-001–SC-002 |
| Q3 lever contract | US2; FR-003; FR-011; Edge Cases |
| Q4 CRLF split | US3/US5; FR-007; FR-009; SC-006 |
| Q5 fixture + edit boundary + upstream | US3–US5; FR-005–FR-008; SC-003; SC-005 |
| Q6 loop / ordering / grill close | FR-012; SC-007; plan Ordering |
| Plan-challenge decisions 1–7 | US1–US5; FR-001–FR-012; SC-001–SC-007; Edge Cases; Assumptions |
| recon-DEV-451 facts | Scope; Edge Cases; Key Entities; plan Structure |
| PRODUCT.md §3–§5 | Precedence; no owner checkbox |
