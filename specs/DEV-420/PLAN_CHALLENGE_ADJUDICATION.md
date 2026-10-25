# DEV-420 plan challenge adjudication — round 1

Date: 2026-10-25. Seat: Keel. Phase 2 step 6 only.

**Status: Round 1 re-adjudication CLOSED — CLEAN.** All five accepted Medium findings are corrected and verified against the updated working artifacts and Patron Q7 ruling at CONCLUSIONS.md:233-236. No accepted Critical/High/Medium finding remains open; no Round 2 Quill list is required. Ledger F2 remains rejected and all six Low dispositions remain recorded. The initial dispositions and handoff below are historical; the closure evidence at the end is current. No freeze or Gate 1 clearance.

## Target and evidence

Worktree `F:/Dev/LamuFlix.worktrees/feature-420-spec`; branch `feature/420-spec`; verified HEAD `8063eed0d5cc4b5bb3a8eb0c68b8990acf7a8cd3`. The spec packet and ADR-0028 are untracked working content, not content pinned by that HEAD. See the source hashes below for the actual reviewed bytes.

Read task note `DEV-420`, both named recon notes, all three named findings notes, `brief.md`, `CONCLUSIONS.md`, `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, and ADR-0027/0028. All three axes are present. Ledger and Compass identify the target directly; Sentry target verification is recorded in task note `DEV-420:75`. No independent production searches or measurements were made. Recon's older 23-file/absent-TelemetryConstants documentation observations are historical: current brief C1 and downstream documents supersede those observations, without implying implementation exists.

Authority: brief Q11/round cap at `brief.md:23-29`, Q7/Q8 at `:120-137,182-191`, Q9 at `:145-178`; Patron Q7/Q8/Q9/Q11/Q12 and post-grill C1 in `CONCLUSIONS.md`; charter sections 2.3 and Halt Conditions. Verified in-scope Critical/High/Medium findings block; Low observations require no fix commit. This is the first complete plan-challenge set, not another grill question and not a new code-review round. Two plan review/fix rounds maximum; unresolved blockers after round 2 go to Conductor. No third review to repair a receipt.

## Complete finding dispositions

| Finding | Submitted | Disposition | Evidence and reason |
|---|---|---|---|
| Sentry logging boundary | Medium | ACCEPTED; OPEN downstream correction | The original spec.md:108 and ADR-0028:27 allowed unspecified exception content. Patron resolved the policy at CONCLUSIONS.md:233-236: row id when row-scoped, exception type name and stable internal error category only; exception objects, raw Message/ToString/stack/inner-exception text, payload-derived values and sensitive configuration prohibited. Keel recorded brief/ADR alignment; Quill spec/plan/tasks/checklist propagation and proof ownership remain open. No observed runtime leak is asserted. |
| Ledger F1 stale ADR status | Medium | ACCEPTED; OPEN correction | `spec.md:9,139,155,188`, `plan.md:7,35,65,102,169`, and checklist CHK023/CHK043 still describe deferred ADR work, while ADR-0028:3,13 is an existing Draft. Reconcile present packet status, preserving historical descriptions of the earlier Quill ask where explicitly labeled historical. `tasks.md:219` is an ownership restriction on future Build asks, not proof that the ADR is missing; keep that restriction while updating references. No new decision required. |
| Ledger F2 refactor boundary | Medium | NOT ACCEPTED as blocker; speculative, no severity demotion | `tasks.md:146` already permits helpers/guards; `brief.md:147` and `plan.md:55,86` already require a cited ruling before another file. A 24-file boundary does not itself forbid private methods or imply a measured complexity failure. No code, metric, or impossible requirement is supplied. Do not pre-authorize another collaborator, file, or architectural abstraction. Actual later evidence follows the existing ruling path. |
| Ledger F3 worker test seam | Low | RECORDED; no required correction | `brief.md:184,188,190` and `tasks.md:46,66,86` already put query/serialization/fault-persistence proofs on real DB support and forbid mocked drivers. No specific existing reusable harness is established by this finding. Do not invent one, a new relay port, or a persistence substitute. Concrete missing support at pickup requires recon; no semantic seam decision is made here. |
| Ledger F4 ADR durability/duplication | Low | RECORDED; no required correction | ADR-0028:13,21 already links durable repository brief, conclusions and plan, alongside note evidence; no evidence says the spec directory will be deleted. Self-containment/less repetition is an editorial preference, not a correctness blocker. Retain Draft and scope limitations; no ADR rewrite required. |
| Ledger F5 proof cohesion/boilerplate | Low | RECORDED; no required correction | `brief.md:161` explicitly assigns state/health/metric proofs to `OutboxRelayHealthCheckTests.cs`; `tasks.md:105` names metrics there. A separate test file is unnecessary and outside Q9. Repetition is maintainability advice. M2 below supplies the missing concrete recovery proofs without new files. |
| Compass M1 non-overlap proof | Medium | ACCEPTED; OPEN correction | `brief.md:190` explicitly requires no-overlapping-passes proof; FR-001 at `spec.md:147` requires the behavior. Neither T002 nor T008 names its executable proof. Assign one deterministic in-flight-pass/no-second-pass test in the already authorized worker test file. |
| Compass M2 recovery/empty sample | Medium | ACCEPTED; OPEN correction | `brief.md:126` and ADR-0028:25 require respective fault clearing and count/age zero after a successful empty sample. US4:105-106 and T014 omit those positive recovery assertions. Preserve precedence and independent fault state: cleanup success cannot clear a pass fault, nor pass success a cleanup fault. |
| Compass M3 cutover atomicity | Medium | ACCEPTED; OPEN correction | `brief.md:187` explicitly requires staging/rollback atomicity proof; US5 and T021/T022 name successful commits but not a real-DB both-or-neither failure proof after cutover. Extend existing authorized tests, not DEV-329 persistence production files. Known pre-commit failure/explicit rollback leaves neither durable change; an uncertain save completion must not be described as guaranteed rollback (`recon-DEV-420:171-176`; brief handler-cutover contract). |
| Compass L1 marker-save host token | Low | RECORDED; no required correction | Already explicit in `spec.md:131,150`, `brief.md:112`, ADR-0028:21. T009 can repeat it editorially, without a decision or scope change. |
| Compass L2 ADR cross-reference | Low | RECORDED; covered by accepted Ledger F1 | Add current Draft ADR references while correcting stale status. This does not accept ADR-0028 or grant later phase clearance. |
| Compass L3 Gherkin citation | Low | RECORDED; no required correction | Q10 governs task ordering, not a specific Gherkin exclusion; the exclusion's attribution is unsupported. It does not remove any Q8 proof. Do not infer a new acceptance-test framework obligation or broaden scope. An editorial attribution correction may remove the misleading Q10 association. |

Totals: 12 submitted findings (6 Medium, 6 Low); 5 accepted Medium gaps remain open; 1 submitted Medium rejected on evidence, not demoted or deferred to obtain clearance; all 6 Low observations recorded. No Critical/High finding. No new abstraction or out-of-scope ticket is authorized. No tracker write or follow-up filing is performed.

## Patron decision — resolved on resumption

The original stop asked Patron to settle Q7's safe failure-log fields. Patron's complete question, ruling and cited basis are recorded in `CONCLUSIONS.md:225-236`; approval is at `:233`. Allowed fields are outbox row id when row-scoped, exception type name, and stable internal error category only. Prohibited are exception objects and raw Message/ToString()/stack trace/inner-exception text, payload-derived values and sensitive connection/configuration details. There is no sanitized-exception exception or new dependency.

Keel recorded this exact ruling in `brief.md` (Q7 logging paragraph and appended Plan-challenge Q7 amendment) before the narrow matching ADR-0028:27 edit, as Patron directed at `CONCLUSIONS.md:236`. ADR-0028 remains Draft. CONCLUSIONS was read and preserved; no new decision was made. The decision blocker is resolved; Sentry's accepted finding remains open until downstream correction and verification. No second challenge round is claimed by this resumption.

## Consolidated Quill fix list — final Round 1 handoff

1. **Sentry — accepted Medium, OPEN correction; Patron decision resolved:** propagate `CONCLUSIONS.md:233-236` and the updated brief Q7 allowlist into spec US4/FR-006, plan telemetry guidance, task implementation/proof ownership and checklist. Allow only row id when row-scoped, exception type name and stable internal error category; prohibit logger exception-object arguments, raw Message/ToString()/stack/inner-exception text, payload-derived values and sensitive configuration. Assign the proof to existing `OutboxRelayHostedServiceTests.cs`: sentinel values in exception/inner-exception content must not reach captured logs or structured state, no exception object may be passed, and rowless failures do not invent ids. Preserve existing no-sensitive/high-cardinality metric-label rules. Keel has aligned ADR-0028; Quill does not edit it. No redaction dependency or sanitized-exception escape hatch.
2. **Ledger F1 accepted Medium / Compass L2 recorded Low:** reconcile current Draft ADR-0028 references in spec/plan/tasks/checklist and revalidate CHK023/CHK043. Preserve earlier-stage history as history, future Build ownership restrictions, Draft status and separate acceptance/freeze/Gate 1 steps.
3. **Compass M1 accepted Medium:** name the deterministic no-overlap proof in T002 or T008, mapped to FR-001, in `OutboxRelayHostedServiceTests.cs`; an in-flight pass must prevent a second pass despite elapsed due time. No wall-clock timing or new test file.
4. **Compass M2 accepted Medium:** add US4 acceptance scenarios/FR-006 traceability and T014 assertions for successful empty count/age zero, pass/sample failure recovery, and cleanup-fault recovery independently; revalidate affected readiness/recovery checklist items. Preserve gauge withholding until a valid sample.
5. **Compass M3 accepted Medium:** add US5 known-failure/rollback atomicity acceptance and assign real-Postgres both-or-neither cutover proof in T021 using `ImportMovieFolderCommandHandlerPersistenceTests.cs` and authorized handler cases in `OutboxRelayTests.cs` as needed for both handlers. Preserve Stage/save exception and cancellation propagation and the uncertain-save distinction; no new transaction coordinator or prerequisite edits. Revalidate cutover coverage checklist items.

Resumption write receipt: Keel updated only brief.md, this adjudication artifact, and the narrow ADR-0028 logging paragraph explicitly directed by Patron. spec.md, plan.md, tasks.md, checklists/requirements.md and CONCLUSIONS.md remain unchanged on this resumption. All 12 dispositions and the two-round cap are preserved; five accepted Medium findings await Quill correction and verification. No new decisions, freeze, Gate 1, production changes, tests, gates, PR publication or merge. Conductor may now assign all five corrections to Quill as one Round 1 fix list.

## Initial Round 1 reviewed working-content SHA256 (before Patron ruling propagation)

```text
brief.md 093D03C484006E689428FCB75EAB95B09AD236CF90C259F4D33496782830EB57
CONCLUSIONS.md BFB4838745DE8705C09C1C2D6B715541EDE770CFED898B07A0F005D30C172322
spec.md FCE5C02E6F3F001A441A99C150234CDFE52BD60573A9DD4DB7288789E7C4104C
plan.md 0D9141FB3B770A08CEDFB6E1BB795FE42B38B5BF4FD734C839EE5039A6EE46F5
tasks.md B13C9C396291CBB2E52FB821E9DE1F1B38149B347832B471451FF971D1C85053
checklists/requirements.md F26C2C3ABA540B73A765EA660AA7F211E4639288DC4394F40E8EAF27D7CFAD58
ADR-0027 C0DA92F268163642A159BFD5AF35EB800E092C617D5735302EA0F4E76C5191B8
ADR-0028 35C28E32159B2E3E4169475BE36EAC451448858BDDFF4967F94EA644365C5E10
```

## Resumption working-content receipt

Patron ruling was already present before this resumption. Source hashes above remain the original challenge baseline; they do not describe the amended brief, conclusions or ADR. Current hashes after Keel propagation:

- specs/DEV-420/brief.md SHA256 323BB3DC1EE37F594583E907D4B2C3C968683776519AE5CF04031516CE3B6444
- specs/DEV-420/CONCLUSIONS.md SHA256 6DF718332B84DA2F4B0BD79C6269449D69F7A29652FDBDA7E0E87DE7BDA6F132
- docs/adr/0028-confirmed-enrichment-outbox-relay.md SHA256 F6D101F811599AF339B004CCBCFA95105387F35784B056DFF82FD81ABE0A4B4F


## Round 1 re-adjudication closure after Quill corrections — 2026-10-25

**Verdict: CLEAN.** Keel verified the same worktree, branch and expected HEAD above, read task note DEV-420 through the Quill correction receipt at line 80, recon-DEV-420, all three findings notes, this adjudication, brief, conclusions, spec, plan, tasks, requirements checklist and Draft ADR-0028. Repository facts come from the supplied recon and findings; no independent implementation searches, tests or gates were run. This verifies the one consolidated Round 1 fix list, not a second full challenge round.

### Accepted Medium findings — closure evidence

| Accepted finding | Current artifact evidence | Closure |
|---|---|---|
| Sentry failure-log boundary | Patron authority CONCLUSIONS.md:233-236; brief.md:137,217-225; ADR-0028:27; spec.md:20,111,156,160,185; plan.md:45,63,153-154; tasks.md:67 (T008); requirements.md:24,64,93,97. | CLOSED. Only row id when row-scoped, exception type name and stable internal category are allowed. Exception-object arguments, raw Message/ToString/stack/inner-exception text, payload-derived values and sensitive configuration are prohibited. T008 owns exception/inner-exception payload/secret sentinel capture and no-exception-object proof in the existing worker tests; rowless failures must not invent ids. No sanitized-exception exception or redaction dependency is introduced. |
| Ledger F1 stale ADR status; related Compass L2 remains Low | spec.md:9,143,159,192,194; plan.md:5,7,35,65,102,148,169-170; tasks.md:3,147,219; requirements.md:43,84,97; ADR-0028:3,13,46. | CLOSED. Current references consistently identify existing Draft ADR-0028; earlier Quill drafting is explicitly historical. CHK023/CHK043 are revalidated against present Draft status. Future Build ADR ownership restrictions remain intact; no ADR acceptance, plan freeze or Gate 1 is claimed. |
| Compass M1 no-overlap proof | brief.md:190; spec.md:57,151,160; plan.md:45,149,154; tasks.md:45 (T002),72 (T010); requirements.md:65,97. | CLOSED. FR-001/US1 now state that an in-flight pass prevents a second pass despite elapsed due time. T002 owns deterministic TimeProvider proof in the authorized OutboxRelayHostedServiceTests file; no wall-clock sleeps or new test file. |
| Compass M2 empty sample and fault recovery | brief.md:124-126; ADR-0028:25; spec.md:106-109,156,160,185; plan.md:63,153-154; tasks.md:105 (T014),113 (T018); requirements.md:24,35,57,69,97. | CLOSED. Successful empty samples report count/age zero. Later successful passes clear pass/sample faults; later successful cleanup clears cleanup faults independently, with explicit prohibition on cross-clearing. Failed/invalid sample gauges remain withheld until a valid sample; health precedence remains unchanged. T014 owns these assertions and T018 drives them green. |
| Compass M3 cutover atomicity proof | recon-DEV-420:171-176; brief.md:78,187; spec.md:117,127,157,160,186; plan.md:22,45,148,154; tasks.md:128-130,134-135 (T020-T024); requirements.md:25,56,71,97. | CLOSED. US5 now requires real-Postgres known-precommit-failure/explicit-rollback both-or-neither proof for either cutover handler. T021 assigns Import persistence proof in ImportMovieFolderCommandHandlerPersistenceTests; T022 assigns authorized OutboxRelayTests cases covering both ImportMovieFolder and RequestEnrichment as needed. Stage/serialization/save exceptions and cancellation still propagate; uncertain save completion is not proof of rollback. No new coordinator or DEV-329 persistence production edit. |

### Preserved dispositions and round accounting

- Ledger F2 remains NOT ACCEPTED as a blocker: speculative refactor failure without measured evidence; no demotion, extra collaborator/file authorization or scope expansion.
- All six Low observations remain recorded without required fixes: Ledger F3 (worker seam), F4 (ADR durability/duplication), F5 (proof cohesion/boilerplate), Compass L1 (marker-save token), L2 (ADR cross-reference), L3 (Gherkin attribution). L2 is covered by F1 correction. Quill also removed the unsupported Q10 attribution in spec.md:143,161; this does not change L3 severity or create another obligation.
- Totals remain 12 submitted findings: 6 Medium and 6 Low; 5 accepted Medium CLOSED, 1 submitted Medium rejected, 6 Low recorded. Remaining accepted Critical/High/Medium findings: 0.
- Round 1 closes within the two-round cap. A Round 2 Quill list is NOT required. No new decision, plan freeze, ADR acceptance, Gate 1, implementation clearance, tests/gates, publication or merge is claimed.

### Re-adjudication write and working-content receipt

Only PLAN_CHALLENGE_ADJUDICATION.md is updated by this ask. The historical initial review and Patron-resumption receipts above are preserved. brief.md, CONCLUSIONS.md, spec.md, plan.md, tasks.md, checklists/requirements.md and ADR-0028 were hashed before and after this write and must remain byte-identical. These artifacts remain untracked working content at the verified HEAD; CLEAN describes documentation correction closure, not HEAD-pinned implementation or runtime proof.

Reviewed working-content SHA256:
- specs/DEV-420/brief.md 323BB3DC1EE37F594583E907D4B2C3C968683776519AE5CF04031516CE3B6444
- specs/DEV-420/CONCLUSIONS.md 6DF718332B84DA2F4B0BD79C6269449D69F7A29652FDBDA7E0E87DE7BDA6F132
- specs/DEV-420/spec.md CE1EE68CA078C83D2A4DA89FA71352480714D83A5D06D662B8A50B7D414B3DE6
- specs/DEV-420/plan.md EEEB7F39DBD5D4519231B9299FAE7DC70ABADB6F65B9FDD464EE2CFBD11ED8AF
- specs/DEV-420/tasks.md 50E54056454504503DD72548598820738C73370D4305C9A7333985B3394EC852
- specs/DEV-420/checklists/requirements.md 9FAF1CCB099C0AF37147847DF768D686346F5FE1366D0422E85C021BDDBFCDF6
- docs/adr/0028-confirmed-enrichment-outbox-relay.md F6D101F811599AF339B004CCBCFA95105387F35784B056DFF82FD81ABE0A4B4F
