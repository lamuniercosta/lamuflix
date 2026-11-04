# Tasks: Pre-PR mutation under 30 minutes

**Input**: `specs/DEV-451/` spec.md, plan.md, frozen brief.md (Q1–Q6 + plan-challenge round 1 decisions 1–7), CONCLUSIONS.md, FOLLOW-UP-TICKETS.md, recon-DEV-451, specs/PRODUCT.md, adjudication-DEV-451
**Status**: Draft — Phase 2 Quill drafting + plan-challenge round 1 repair; Gate 1 not approved; PLAN FROZEN not declared; no implementation clearance. Lever identity intentionally undecided until Build measurement (Q3).
**Gate 1**: No owner structural checkbox (brief Handoff; PRODUCT.md §5). Operational dependencies (T021 dirty-policy restore receipt; T022 verified CRLF follow-up ID) are Bernstein/Rigger-owned and block Build T000+ per Q6; Contoso fixture does **not** bypass T022.
**Tests**: Required (ticket ACs; Q5 Contoso; real Q2/Q3/DEV-444 mutation runs with native gate-produced `mutation-timing.json`). Stubbed Contoso success never replaces measured acceptance. Experimental scratch receipts are not native gate success.
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` is optional and intentionally **unused** in this list (same-file Contoso edits and shared frozen replay inputs serialize). Parallelism is described under Parallel opportunities without inventing `[P]` markers. Same-file concurrent writes are never allowed.
**Conventions**: Edit only ticket-named scripts as needed; never hand-edit `stryker-config.json`; never change any `gates.mutation` values (entire block immutable, including tuning keys); never enable `since` in linked worktrees; never raise concurrency above half cores; report gate exits honestly. Task IDs T000–T022 are stable labels; numeric ID order is **not** execution order (T021/T022 intentionally precede T000).

**Document phase labels vs brief adoption**: Document Phase 0 = operational receipts; Phase 4 = ADR checkpoint (T010) then lever implementation (T011); brief “Phase 5 / adoption” maps to post-T010 lever adoption — always T010-before-T011. Independent Q5 Contoso (T007–T009) may proceed after T000 (which already requires T021/T022) without waiting for T010 and without adopting the candidate; it must not mutate frozen replay inputs.

---

## Phase dependency / execution order

```text
Phase 0   T021, T022 (operational receipts — Bernstein/Rigger) — block Build T000+ (Q6; Contoso does not bypass T022)
Phase A   Spec artifacts — Keel analyze / challenge / Gate 1 outside this list
Phase B   T000 (Build pickup) — blocks T001–T020
Phase 1   T001 → T002 → T003   (US1 baselines via provisioned historical replay)
Phase 2   T004 → T005 → T006   (US2 scratch experiment + ratification; shipping scripts untouched)
Phase 3   T007 → T008 → T009   (US3 Contoso fixture; after T000; may overlap Phase 1; never mutate frozen replay inputs)
Phase 4   T010 → T011 → T012 → T013  (US4 ADR checkpoint → implement → matched candidates → validate T012 DEV-444 sample)
Phase 5   T014 → T020           (US5 upstream/PR/policy + gates + review)
```

---

## Phase 0: Operational prerequisites (blocks Build implementation)

**Purpose**: Record verified dirty-policy restoration and CRLF follow-up ID before Build coding (brief Q6 ordering).
**Checkpoint**: Receipts present in `specs/DEV-451/` (CONCLUSIONS.md and/or FOLLOW-UP-TICKETS.md); or stop blocked operational.

- [ ] T021 **Bernstein/Rigger** restore `harness.yml` `gates.mutation.exclusions` `LamuFlix.Tests.Common` entry to intake HEAD `010e0cb63a52e3689d9f246c4179e9bb30959738`. Rigger supplies verified evidence; **Keel** (CONCLUSIONS owner) records the verified cleanup receipt in the Build worktree copy of `specs/DEV-451/CONCLUSIONS.md` after Gate 1 / spec merge when applicable — no concurrent overwrite of CONCLUSIONS by other seats (Q1; FR-004; recon-DEV-451 §1; plan-challenge decision 6–7). Keel/Quill do not edit `harness.yml` for this cleanup. Stop Build implementation until receipt exists.
- [ ] T022 **Rigger** (coordinated by Bernstein) obtain a verified YouTrack follow-up ID for the PR #297 / `MmrSelector.cs` CRLF span gap after Order-sequence repair, and record the linked ID in `specs/DEV-451/FOLLOW-UP-TICKETS.md` (and CONCLUSIONS as needed) (Q4; FR-009). Do not claim Create success while Order dry-run exits 2. Leave `.gitattributes` and `src/**/*.cs` unchanged. Contoso T007–T009 do **not** unblock from T022.

---

## Phase B: Pickup — fresh recon and drift reanalysis (blocks T001–T020)

**Purpose**: Confirm frozen brief/spec/plan/tasks still match landed main and gate scripts before any script edit.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift stops for a cited decision.

- [ ] T000 After T021 and T022 receipts and after owner Gate 1 / spec merge: obtain fresh Build pickup recon against picked HEAD / landed main for `scripts/run-mutation.ps1`, `scripts/_harness-config.ps1`, `scripts/Test-RunMutation.ps1`, `harness.yml` `gates.mutation`, `stryker-config.json`, and ADR 0016. Run cross-artifact reanalysis of brief/spec/plan/tasks. Amend tasks only where authorized by demonstrated drift. **STOP** on unresolved drift, incompatible pickup drift vs recorded policy/tool versions, or missing lever-selection facts for a cited Patron decision — do not invent a lever. No production/script feature edits in this task (brief Task ordering; FR-003).

---

## Phase 1: User Story 1 — Matched baseline measurement (P1)

**Goal**: Live baselines for DEV-444, DEV-448, DEV-333 via operator-provisioned detached historical replay with durable timing/receipts.
**Independent Test**: three baseline artifact sets with required fields; frozen diffs; constant machine/concurrency/timeout/tree/overlay per case; native gate-produced `mutation-timing.json`.

- [ ] T001 [US1] Obtain Bernstein/Rigger-provisioned detached replay at each historical SHA for DEV-444 (`00799914`), DEV-448 (`6fcbd1ff`), and DEV-333 (`b25ecb16`). Record immutable comparison base/head, code/test tree hashes, mutate-file list, eligible test-project list (`LamuFlix.UnitTests` + `LamuFlix.Evaluation` for Core), machine identity, actual concurrency integer (≤ `floor(ProcessorCount/2)`, no `gates.mutation` edit), actual unchanged `additional-timeout` from config, and versioned gate/tool/config **overlay** versions+hashes (recon §3:49 support-file overlay precedent only — freeze code/tests). Record actual runtime-read policy/tool values from harness/config; **STOP** on incompatible pickup drift. Build must not write outside assigned roots or invent checkout/replay commands; if the supported mechanism is missing, emit `needs recon` before any run (FR-001; FR-004; Q2; recon-DEV-451 §3; plan-challenge decision 3, 7)
- [ ] T002 [US1] On each provisioned replay checkout, run live baseline `pwsh -NoProfile -File ./scripts/run-mutation.ps1` under quiet-machine control with the recorded concurrency/timeout/runner; archive **native gate-produced** `mutation-timing.json`, mutation receipts, wall time, per-project mutant counts, eligible test-project lists, timeout totals, and per-mutant statuses under `specs/DEV-451/` evidence paths. Baseline/candidate must differ only by lever; overlay support files only. Invent no historical timings (FR-001; Q2; recon-DEV-451 §4; plan-challenge decision 1–2)
- [ ] T003 [US1] Verify no invented historical timings were used; confirm baselines are durable, comparable, and carry stable per-mutant identity keys before any lever experiment starts (FR-001; FR-011; Q2; Q6)

---

## Phase 2: User Story 2 — Measurement-gated lever selection (P1)

**Goal**: Concrete non-DEV-435 candidate evaluated via authorized reversible scratch path; Patron ratification or fail closed; shipping scripts untouched until T010/T011.
**Independent Test**: matched scratch candidate vs baseline evidence with integrity matrix; recorded Patron ruling before document Phase 4 / brief adoption; implementability within three-script set proven.

- [ ] T004 [US2] Propose one concrete lever outside the exhausted DEV-435 set (changed-file globs + direct-reference filtering already baseline; `since` forbidden; concurrency already ≤ half cores). Keep `mtp` / `coverage-analysis: off` as control. Explicitly treat `mtp`/non-`off` as **GATE NOT WIRED** (recon §6; `scripts/run-mutation.ps1`) — do not silently bypass or relax that shipping guard. Before retrying historically rejected `coverage-analysis: perTest`/`all` or any unsupported runner/mutator mode, obtain targeted recon **and** explicit Patron **experimental** authorization. Prove the candidate is implementable within the three ticket-named scripts only; if unnamed renderer/install/`harness.yml`/consumer/doc edits would be required, **STOP** for a cited Patron ruling (charter §2.3; user escalation only for ticket contradiction or necessary constitution departure) — no implied renderer permission. Evaluate compatibility of recon-listed CLI/output consumers (README/AGENTS/skills/CI references at recon §5:89); required unnamed consumer edits STOP for ruling/follow-up, never silent rewrite. Specify a feasible reversible **non-shipping** scratch experiment path using versioned gate copies (unchanged policy) that produces durable timing/receipts equivalent to gate artifacts plus actual `-EvaluateReport` integrity, with honest provenance — shipping scripts remain unchanged until T010/T011. Scratch guard changes are never shipping permission. If mechanism facts or authorization are missing, fail closed / `needs recon` before T005 (FR-003; FR-007; Q3; recon-DEV-451 §5–§6; plan-challenge decisions 1, 4, 7)
- [ ] T005 [US2] Run matched candidate measurements via the T004 scratch path on the same three frozen replay cases/trees/machine/pinned concurrency integer/timeout/runner under quiet-machine control; archive timing/receipts with honest provenance (not claimed as native shipping-gate success). Build a baseline↔candidate **per-mutant status matrix** keyed by stable identity (source location + mutator/replacement — not run-local numeric IDs) including Survived-to-Timeout and Killed-to-Timeout counts and timeout totals; unmatched mutant populations invalidate the comparison. Reject positive Survived→Timeout or Killed→Timeout conversions, unexplained timeout growth, wall-time-only gains that omit eligible suites, raise concurrency, relax any `gates.mutation` key, or inflate verdicts. On DEV-444, note recommendation-test `killFileSpanExempt` weakens the span signal — inspect kill-share and per-mutant statuses; keep all safeguards unchanged. Archive invalid runs with rationale (FR-003; FR-011; Q3; plan-challenge decisions 2–3)
- [ ] T006 [US2] Obtain explicit Patron ratification for the selected lever recorded in `specs/DEV-451/CONCLUSIONS.md` before document Phase 4 / brief adoption checkpoint. Ratification MUST include: (a) implementability within the three-script edit boundary with **zero** unnamed renderer/install/config surface edits, (b) comparison of the candidate against **every** ADR 0016 Decision item and DEV-410 safeguard — departure requires in-place ADR 0016 amendment authorization; **no-amendment is valid only for zero departure**, never merely lack of authorization, (c) experimental authorization already obtained if a rejected/unsupported mode was tried. **Fail closed** (stop; report blocked to Bernstein) if no lever meets both speed and verdict-integrity bars. Authorization here is not the landed amendment — when departure exists, T010 must complete before T011. Amending existing ADR 0016 needs **no new number**; only a brand-new superseding ADR uses a Conductor-assigned number. Do not draft ADR text until ratification (+ Conductor number only if a new ADR) (FR-003; Q3; Q6; plan-challenge decision 4)

---

## Phase 3: User Story 3 — Diff-only / WholeProject Contoso proof (P1)

**Goal**: Exact file-selection regression in `scripts/Test-RunMutation.ps1` without adopting the lever and without mutating frozen replay inputs.
**Independent Test**: new cases green; full Contoso suite green; CRLF defect not claimed fixed. Independent of T010/T011; still after T000 (hence after T021/T022).

- [ ] T007 [US3] Extend Contoso fixture in `scripts/Test-RunMutation.ps1` with a changed production `.cs` file and an unchanged sibling `.cs` in the same project; do not modify frozen historical replay checkouts or shared baseline inputs (FR-006; Q5; recon-DEV-451 §5; plan-challenge decision 6–7)
- [ ] T008 [US3] Add assertions for default/diff-scoped dry-run and stubbed real invocation: generated `mutate` equals only the changed file; `since.enabled=false`; and explicit `-WholeProject` selects the full target; do not represent the PR #297 / `MmrSelector.cs` CRLF span gap as fixed (FR-005; FR-006; FR-009; Q4; Q5)
- [ ] T009 [US3] Run the complete `scripts/Test-RunMutation.ps1` suite and keep all existing classification/scope/share/span/runner/timeout/timing cases green (FR-006; Q5)

---

## Phase 4: User Story 4 — Ratified lever implementation and &lt;30m acceptance (P1)

**Goal**: Land any required in-place ADR 0016 amendment (or record zero-departure no-amendment) before adopting the lever; implement only the ratified lever; prove DEV-444 under 1800 s using the single authoritative T012 sample.
**Independent Test**: ADR checkpoint complete (landed amendment or explicit zero-departure no-amendment); matched candidate artifacts + DEV-444 wall time &lt; 1800 s from T012 sample; entire `gates.mutation` unchanged.

- [ ] T010 [US4] After T006 ratification and measured evidence: **conditional measured ADR 0016 amendment/approval checkpoint before adoption** (document Phase 4; maps to brief adoption-before-Phase-5 boundary). If T006 recorded any ADR 0016 Decision or DEV-410 departure, draft/land an **in-place** amendment to `docs/adr/0016-stryker-mtp-runner.md` (no new ADR number) under Conductor process and do not start T011 until approved/landed. If and only if zero departure exists, leave ADR 0016 unchanged and record the explicit no-amendment path, then proceed to T011. Never treat “amendment not authorized” as no-amendment when a departure exists (FR-003; Q3; Q6; plan-challenge decision 4)
- [ ] T011 [US4] After T006 ratification **and** after T010 ADR checkpoint (amendment landed, or explicit zero-departure no-amendment): implement the ratified lever only in `scripts/run-mutation.ps1` and, if required within unchanged policy, `scripts/_harness-config.ps1`; preserve eligible-project filtering, half-core concurrency ceiling, entire `gates.mutation` block (threshold/share/span/exemptions/exclusions — no tuning-key carve-out), `additional-timeout`, `since.enabled=false`, and `-WholeProject` explicitness; never hand-edit `stryker-config.json`; never change `harness.yml` `gates.mutation` values; never silently expand to renderer/install/unnamed files (FR-004; FR-005; FR-007; Q1; Q5; plan-challenge decision 4)
- [ ] T012 [US4] Re-run matched candidate measurements for all three frozen replay cases after implementation using **native gate-produced** `mutation-timing.json`; reuse the same pinned concurrency integer, timeout, runner, quiet-machine control, and frozen DEV-444 replay case. Confirm integrity via per-mutant status matrix (stable identity), timeout totals, kill-share/span safeguards; archive before/after artifacts including timeout totals for the PR table; archive invalid runs with rationale (FR-001; FR-010; FR-011; Q2; Q3; plan-challenge decisions 2–3)
- [ ] T013 [US4] **Validate** the authoritative T012 DEV-444 run artifact: wall time strictly under 1800 seconds (ticket AC / strict under-30-minute target), `LamuFlix.UnitTests` and `LamuFlix.Evaluation` intact, mutation integrity satisfied. Do **not** run a second best-of sample to erase a valid target miss; retain slow/failing samples. Replacement only after documented invalidation **plus** Patron ruling; all attempts archived. Fixture-green alone is insufficient; experimental scratch provenance is not native gate success (FR-002; SC-002; Q2; plan-challenge decision 2)

---

## Phase 5: User Story 5 — Upstream parity, evidence, gates, review (P2)

**Purpose**: Delivery dependencies, policy proof, honest gates, Q6 closing bar.
**Independent Test**: harness origin/main SHAs for all three scripts; PR table; empty `gates.mutation` diff; review bar met.

- [ ] T014 [US5] Bernstein arranges Rigger in an **explicitly authorized** external `dotnet-agent-harness` session; Build does not escape its worktree or bypass guards. Mirror every **changed** ticket-named script to `dotnet-agent-harness/packs/dotnet/scripts`. Record confirmed-or-merged `origin/main` status + exact SHAs + parity receipts for **all three** scripts (`run-mutation.ps1`, `_harness-config.ps1`, `Test-RunMutation.ps1`), including unchanged counterparts. Gate awaiting-merge/delivery on this T014 evidence; pending PR or local copy is insufficient (FR-008; SC-005; Q5; ticket AC5; plan-challenge decision 5)
- [ ] T015 [US5] Confirm final PR diff leaves entire `harness.yml` `gates.mutation` unchanged and that Q1 cleanup receipt (T021) remains valid; confirm `.gitattributes` and `src/**/*.cs` untouched; confirm CRLF follow-up ID (T022) is linked and Contoso does not claim the span defect fixed (FR-004; FR-009; SC-004; SC-006; Q1; Q4)
- [ ] T016 [US5] Write the three-row before/after table for DEV-444 / DEV-448 / DEV-333 into the PR description with links to durable timing/receipt artifacts **and timeout totals** from the status matrices (FR-010; FR-011; ticket AC)
- [ ] T017 Confirm frozen scope only: no threshold or other `gates.mutation` changes; no DEV-409 whole-Core job; no production C#/web; no new dependency/project/layer/schema/API/LocalPlay/secrets; no invented lever without T006; no unnamed renderer/install expansion (FR-007; Q6)
- [ ] T018 Run applicable repository gates after script changes (`./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness if `.cs` changed, `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, format, vulnerable-packages, `dotnet test` as applicable). Report **actual script exits only**: PASS/FAIL/SKIPPED (scope-empty)/SKIP (opt-out)/Could not run — never relabel as PASS. **N/A is mutation-only** (when every changed project is listed in `gates.mutation.exclusions`), not a generic property disposition. Script-only scope-empty does not replace T002/T012/T013 native Core measurement runs (FR-012; Q6; constitution Pull Request Quality Gates / Static-Analysis Gates; plan-challenge decision 7)
- [ ] T019 Run full `scripts/Test-RunMutation.ps1` suite green on the final script set (FR-006; Q5)
- [ ] T020 Size review under Q6 closing bar (zero unresolved accepted Critical/High/Medium; Low explicit dispositions; two remediation rounds max; unresolved blockers halt to Bernstein); do not lower thresholds; user merges (FR-012; Q6)

---

## Dependencies

- T021 and T022 before T000 / Build implementation (operational; Q6). Contoso T007–T009 do **not** bypass T022.
- Owner Gate 1 / spec merge before T000 (brief Task ordering) — no owner checkbox content; gate is ordinary spec approval.
- T000 before T001–T020.
- Phase 1 (T001–T003) before Phase 2 (T004–T006).
- T004 before T005 (scratch path + authorization + implementability proof required first).
- T006 before T010–T013 (no lever adoption without ratification).
- T010 before T011: conditional measured ADR 0016 amendment/approval checkpoint precedes adoption/implementation; no-amendment path only for zero ADR 0016 Decision / DEV-410 departure. Never implement a lever that departs ADR 0016 under the old decision.
- Phase 3 (T007–T009) after T000 (hence after T021/T022 / Gate 1); may proceed in parallel with Phase 1 after T000 without waiting for T010; must not claim acceptance; must not mutate frozen replay inputs; no same-file concurrent writes.
- T011 before T012; T012 before T013 validation and T016 evidence completeness. T013 does not re-run a second acceptance sample.
- T014–T020 after implementation and measured acceptance evidence (T013) for delivery claim; awaiting-merge gated on T014 all-three-script parity.

## Sequencing examples

```text
# Phase 0 (ops) — intentional non-numeric ID order
T021 restore harness.yml Tests.Common exclusion + Keel-recorded receipt
T022 link verified CRLF follow-up YouTrack ID

# Phase B
T000 pickup recon + cross-artifact drift check

# Phase 1 baselines (provisioned replay)
T001 provision/freeze replay + overlays → T002 native baselines → T003 verify artifacts

# Phase 2 lever (scratch; shipping scripts untouched)
T004 propose + scratch path + recon/auth/implementability → T005 matched scratch experiment + matrix → T006 Patron ratify or fail closed

# Phase 3 fixture (after T000; not after T010; does not bypass T022)
T007 Contoso siblings → T008 assertions → T009 full suite
# (must not mutate frozen replay inputs used by T001–T005/T012)

# Phase 4 ADR checkpoint → implement + accept
T010 conditional in-place ADR 0016 amendment/approval (or zero-departure no-amendment) → T011 scripts → T012 matched candidates → T013 validate T012 DEV-444 sample

# Phase 5 deliver
T014 Rigger authorized session + all-three-script parity → T015 policy/CRLF proof → T016 PR table → T017–T020 gates/review
```

## Parallel opportunities

- After T000: T007 Contoso work may proceed while T001–T003 baselines run **only if** Contoso edits cannot change frozen replay inputs and no same-file concurrent writes occur (different concerns; do not conflate fixture-green with measurement). Contoso still requires T021/T022 via T000.
- T021 and T022 are parallel operational tracks (different owners/artifacts); verified T021 receipt is recorded by Keel as CONCLUSIONS owner after evidence arrives.
- T014 upstream sync can prepare in parallel with T015–T016 once script contents are stable post-T011, but delivery/awaiting-merge waits for T013 + T014 all-three receipts.

## Implementation strategy

### MVP

Complete Gate 1 → T000 → Phase 1 baselines → Phase 2 ratification decision. If T006 fail-closed, stop — do not implement a speculative lever.

### Incremental delivery

1. Operational receipts (T021/T022) + Gate 1.
2. Baselines on provisioned historical replay (auditable claim foundation).
3. Contoso diff-only proof (AC 2) after T000, even while lever experiments continue — without mutating replay inputs or bypassing T022.
4. T010 ADR checkpoint (in-place amendment or zero-departure no-amendment) → ratified lever scripts + validate T012 DEV-444 &lt;30m + all-three upstream parity + PR table.

### Notes

- `[P]` intentionally unused; optional marker not applied because Contoso same-file edits serialize and shared replay inputs must not be written concurrently.
- Concrete lever mechanism is intentionally absent from task text until T006 — do not invent one in Spec drafting.
- Quill Phase A does not execute T000–T020.
- No PLAN FROZEN / Gate 1 / measured success claimed by this repair.
