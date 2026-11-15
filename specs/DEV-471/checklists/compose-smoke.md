# Compose Smoke Requirements Checklist: Required Compose smoke with unmasked startup and stubbed full flow

**Purpose**: Unit tests for English — validate clarity, completeness, consistency, and measurability of DEV-471 Compose-smoke requirements (not runtime verification of the stack)
**Created**: 2026-11-15
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [tasks.md](../tasks.md)
**Depth**: Standard (size L clarify/checklist)
**Audience**: Spec author / Keel analyze / plan-challenge reviewers
**Focus**: Two-phase coverage · DEV-440 proof · fixture-correlated acceptance · fail-closed CI · LocalPlay/nginx boundary · file-scope STOP rules

## Requirement Completeness

- [x] CHK001 Are both positive phases (plain shipped startup and separately isolated stubbed full flow) explicitly required inside one `compose-smoke` check? [Completeness, Spec §Scope 1–3, FR-002]
- [x] CHK002 Are library listing, `/hubs/` live updates, and DEV-440 regression proof included in frozen scope (not only intake start/import/enrich/thumbnail/play wording)? [Completeness, Spec §Scope; Conclusions Q2]
- [x] CHK003 Are diagnostics (sanitized logs, health/exit status, browser failure artifacts) and always-attempted cleanup specified? [Completeness, Spec §Scope 10, FR-011]
- [x] CHK004 Is authorized operator verification of `main` branch-protection registration for `compose-smoke` specified as distinct from workflow publication? [Completeness, Spec §Scope 14, FR-015]
- [x] CHK005 Are deliberate failure-path classes (startup timeout, unavailable Docker, assertion failure, teardown failure) named in requirements? [Completeness, Spec §FR-014, US5]

## Requirement Clarity

- [x] CHK006 Is "play" quantified as advancing in-page browser playback with valid metadata (not process-launch)? [Clarity, Spec §Domain vocabulary, FR-007]
- [x] CHK007 Is the DEV-440 oracle specific about expected API startup failure after dependencies are ready versus Docker/build/download/setup failure? [Clarity, Spec §Scope 7, FR-008]
- [x] CHK008 Are budgets and retry policy quantified (30m / 120s / 3m / 120m; zero whole-test retries) with ASSUMPTIONS traceability? [Clarity, Spec §FR-012, ASSUMPTIONS.md]
- [x] CHK009 Is "genuine `/hubs/` update" defined to reject handshake-only, reload, and polling substitution? [Clarity, Spec §FR-004–FR-005]
- [x] CHK010 Is thumbnail success defined as nonempty and image-decodable (not HTTP 200 alone)? [Clarity, Spec §FR-004–FR-005]

## Requirement Consistency

- [x] CHK011 Do requirements consistently forbid treating the e2e overlay alone as production-startup / DEV-440 coverage? [Consistency, Spec §Scope 2–3, ADR 0043, FR-003]
- [x] CHK012 Are LocalPlay-false and nginx process-play 403 preserved consistently across scope, stories, and FRs? [Consistency, Spec §Scope 6, FR-007, US3]
- [x] CHK013 Is the stub boundary consistent: external providers only; no application API/hub mocks? [Consistency, Spec §Scope 5, FR-005–FR-006]
- [x] CHK014 Do fail-closed rules consistently exclude renaming this required smoke's operational failures to SKIPPED/green without reclassifying unrelated harness scope-empty exits? [Consistency, Spec §Scope 8, FR-009]

## Acceptance Criteria Quality

- [x] CHK015 Can SC-001–SC-007 be objectively evidenced (job run, health, fixture assertions, revert proof, failure paths, operator verification, gate verdicts)? [Measurability, Spec §Success Criteria]
- [x] CHK016 Are fixture-correlated assertions required to bind listing/enrichment/thumbnail/hubs/play to the imported fixture rather than preexisting data? [Acceptance Criteria, Spec §FR-004–FR-005, US3]
- [x] CHK017 Is controlled-revert proof required to use the identical plain-stack health assertion as the candidate positive path? [Acceptance Criteria, Spec §FR-008, SC-004]

## Scenario & Edge Coverage

- [x] CHK018 Are masking risks (overlay pass / production revert fail) addressed in edge cases? [Coverage, Spec §Edge Cases]
- [x] CHK019 Are insufficient oracles (handshake-only hubs, empty thumbnail body, visible player without advancing time) addressed? [Coverage, Spec §Edge Cases]
- [x] CHK020 Is STOP-for-ruling required when a Compose edit or missing production capability appears mid-build? [Coverage, Spec §Scope 12, FR-013, Edge Cases]
- [x] CHK021 Is Chromium fixture decodability treated as an explicit first recon obligation rather than an assumed fact? [Coverage, Spec §US1, Assumptions]

## Dependencies & Assumptions

- [x] CHK022 Are Q6 authorized edit targets and reuse-unchanged Compose files explicitly bounded? [Dependency, Spec §Scope 12, FR-013]
- [x] CHK023 Is the live `recon-DEV-471` DEV-440 snapshot vs prior ticket/playback facts via CONCLUSIONS documented so line numbers are not silently remapped? [Assumption, Spec §Input/Assumptions, brief Fact source]
- [x] CHK024 Are budget/retry choices tagged to ASSUMPTIONS rather than silently treated as constitution law? [Assumption, Spec §FR-012, ASSUMPTIONS.md]

## Ambiguities & Conflicts

- [x] CHK025 Does the spec avoid claiming live DEV-440 regression has already been exercised? [Ambiguity guard, Spec §Scope 7, Out of scope]
- [x] CHK026 Does the spec avoid inventing PLAN FROZEN / Gate 1 / runtime PASS from Phase A drafting? [Ambiguity guard, Spec §Status/Owner question]
- [x] CHK027 Are there unresolved inventable behaviour gaps that should appear as `needs decision:` in DRAFTING_RECEIPT rather than silent defaults? [Gap] — expect none after Q1–Q7; Phase B recon facts are not inventable decisions

## Notes

- Checklist tests requirements writing quality only. Runtime Compose proof, controlled-revert execution, and branch-protection admin verification remain Phase B / operator evidence.
- Defaults used for this checklist run (interaction skipped per closed grill): Depth Standard; Audience Reviewer; Focus top clusters two-phase coverage + DEV-440 + fail-closed CI.

## Keel reviewer verdicts — 2026-11-15

These marks mean reviewed requirements quality, not runtime/gate/protection PASS. Freeze remains pending the final Quill repair receipt and independent reread. Named evidence is the repaired spec/plan/tasks plus brief/Patron adjudication decisions; no production-code search was used.

| Item | Verdict | Evidence |
|---|---|---|
| CHK001 | Meets requirement | Spec Scope1–3 / FR002; T002/T006/T008 require both positives |
| CHK002 | Meets requirement | Scope4/7 / FR004/008; body recon36–48 and corrected playback provenance |
| CHK003 | Meets requirement | Scope10 / FR011; T002/T008/T009 redact, capture, teardown |
| CHK004 | Meets requirement | Scope14 / FR015 / SC006; T011 blocking operator verification |
| CHK005 | Meets requirement | FR014 / US5; T009 named deliberate failure classes |
| CHK006 | Meets requirement | FR004/007 / US3.4: valid metadata and advancing currentTime; active403 separate |
| CHK007 | Meets requirement | Scope7 / FR008; T003 prerequisite/readiness/causal classifier;139 optional |
| CHK008 | Meets requirement | FR012 / brief group5 / ASSUMPTIONS Q5 approved values and one config |
| CHK009 | Meets requirement | Scope4 / FR004/005 / US3.3 rejects handshake/reload/poll substitutes |
| CHK010 | Meets requirement | Scope4 / FR004/005 rejects empty/non-image thumbnail |
| CHK011 | Meets requirement | Scope2–3 / FR002/003 / ADR0043; separate unmasked startup |
| CHK012 | Meets requirement | Scope6 / FR007 / SC007 / T006 active nginx403, LocalPlay false |
| CHK013 | Meets requirement | Scope5 / FR005/006; real app routes/generated types, external providers only |
| CHK014 | Meets requirement | Scope8 / FR009 / T009: required smoke exit1; unrelated skips preserved |
| CHK015 | Meets requirement | SC001–007 each has an observable oracle, source-bound receipt or authorized verification; no claimed execution |
| CHK016 | Meets requirement | FR004/005 / T006 imported fixture identity, not unrelated data |
| CHK017 | Meets requirement | FR008 / SC004 / T003 same health assertion candidate vs reverse-only4646476f |
| CHK018 | Meets requirement | Edge case overlay masking + mandatory plain phase / regression proof |
| CHK019 | Meets requirement | Edge cases cover handshake, thumbnail body and advancing media |
| CHK020 | Meets requirement | Scope12 / FR013 / T001 STOP if capability/Compose change required |
| CHK021 | Meets requirement | US1 / assumptions / T001 decodability remains explicit unverified pickup fact |
| CHK022 | Meets requirement | Scope12 / amended brief Q6: dedicated workflow, unchanged ci/promote-home/Compose/nginx |
| CHK023 | Meets requirement | Spec Input/Clarifications/Assumptions now distinguish retained1–25/appended27–73; no historical citation remap |
| CHK024 | Meets requirement | FR012 and ASSUMPTIONS Q5 explicitly approved assumed budgets/retry choices |
| CHK025 | Meets requirement | Scope7 / Out of scope / T004/T010 require future live proof; none claimed exercised |
| CHK026 | Meets requirement | Spec Status / Owner question / T000 separate planning, Keel freeze, owner Gate1 and runtime |
| CHK027 | Reviewed: no unresolved inventable design gap | Negative-polarity question answered NO; Patron provenance + groups1–6 settle decisions; T001 pickup facts and T011 operator evidence stay open as execution obligations, not silent assumptions |

CHK027 checked means reviewed and its answer is NO; it does not assert a gap exists. All accepted plan findings still require the final repair-verification receipt in PLAN_ADJUDICATION.md before freeze. ADR0043 remains proposed and owner Gate1 remains closed.

Final reviewer confirmation: Quill round2 amendments independently reread; A1 budget ordering and A2 check-context provenance closed. All27 quality verdicts above remain valid. PLAN FROZEN recorded by Keel in brief.md and PLAN_ADJUDICATION.md; owner Gate1, implementation and runtime/protection acceptance remain pending.
