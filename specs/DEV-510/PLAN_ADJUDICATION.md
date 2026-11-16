# DEV-510 plan adjudication

Date: 2026-11-15. Seat: Keel. Worktree: F:/Dev/LamuFlix.worktrees/feature-510-spec; branch feature/510-spec. Recon/review baseline: 6edd0d16f0dd51950c56e21f2c866467dc5dff77. The spec directory is uncommitted planning work over that baseline; this is not an implementation-head review.

## Authority and closing bar

All three axis notes were read: findings-DEV-510-Ledger (7), findings-DEV-510-Sentry (8), findings-DEV-510-Compass (5). Total 20: 0 Critical, 3 High, 9 Medium, 8 Low. Each is dispositioned below, including overlapping findings. No missing axis or silent severity waiver.

Facts: original recon-DEV-510 implementation/tests lines 27-37 and authoritative ticket lines 51-67, read before the note replacement; current recon-DEV-510 targeted implementation seams lines 1-7. Targeted seams confirm options/test paths, existing validation chain, and effective-ceiling option access. Historical PA1 (2026-11-15) required same registered serializer/options on the same envelope without independently assumed JSON defaults. PA1 amendment 2026-11-16 Explicit shared serializer (CONCLUSIONS.md T001 Measurement Seam Design Disposition at fca46e8c; brief.md) replaces internal HybridCache serializer/options inspection and the blanket custom-wrapper prohibition with deliberate explicit shared `IHybridCacheSerializer<CachedProfile>` registration/injection inside already-authorized `PersistenceServiceCollectionExtensions.cs` and `HybridCachedRecommendationProfileSource.cs`. Superseding PA1 amendment Serializer setup before baseline (CONCLUSIONS.md T001/T002/T008 Serializer Setup Sequencing Ruling at 85feb708; brief.md) inserts T001b for that install/runtime proof before T002 and removes serializer installation from T008. Exact `AddSerializer` / DI registration signature and shared-envelope accessibility remain T001b verification requirements (compile + runtime) inside the authorized worktree, not invented API claims.

Patron approved the original 20-finding disposition proposal without amendment on 2026-11-15. The full cited answer is persisted in CONCLUSIONS.md PA1, with control characters normalized against the original proposal; precise approved decisions were recorded first in brief.md PA1. Subsequent 2026-11-16 Patron dispositions amended the measurement seam (explicit shared serializer) and then the sequencing (T001b before baseline); see Ledger M3 / Sentry F4 amendments below. Basis: ticket work/design/acceptance, Q1-Q7, Charter 2.3 and AGENTS.md. Original scope and Q2 two-round/two-fix-commit cap remain. No ticket contradiction or constitution departure was approved; ordinary owner Gate 1 merge receipt is separately recorded in CONCLUSIONS.md (historical freeze text below retains its Gate 1 pending wording from 2026-11-15). Round-1 derivative repair for T001b sequencing is synchronized pending Keel independent reread; prior no-outstanding claim is withdrawn until that reread.

## Dispositions

| ID | Severity | Disposition | Basis and concrete closure in planning artifacts |
| --- | --- | --- | --- |
| Ledger M1 | Medium | Accepted; amended | Targeted recon 3-4; brief PA1 / plan authorized boundary include exact TasteProfileCacheOptions.cs and both existing options/registration test paths; T007/T008 name them. |
| Ledger M2 | Medium | Accepted; amended | Patron PA1 and targeted recon 3-4: extend existing .Validate chain plus ValidateOnStart, no validator type; finite 0 < WarningFraction <= 1, const 0.8; existing message pattern. Plan PA1, T007/T008, spec PA1 and ASSUMPTIONS A4 cover 0/1/negative/>1/NaN/infinities. |
| Ledger M3 | Medium | Accepted; amended | Historical (2026-11-15) closure: Targeted recon 5-7 / Patron PA1 — same registered serializer/options on same envelope, real GetOrCreateAsync admission, runtime MaximumPayloadBytes option; Plan PA1, T001/T007/T008 and EVIDENCE PA1; unsupported pinned API blocks implementation; no independently assumed JSON defaults. PA1 amendment Explicit shared serializer (fca46e8c): deliberate explicit shared `IHybridCacheSerializer<CachedProfile>` registration/injection in the two authorized persistence files; thin internal serializer and internal envelope accessibility permitted; no new package/project/layer; do not invent an exact `AddSerializer` signature. Sequencing amendment (85feb708 / brief.md Serializer setup before baseline): **T001b alone** installs that seam over the unchanged incumbent representation (plus registration test and existing HybridCache suites); forbids domain/calculator/port/EF/scoring/inputs/cache-namespace edits there; requires both compile/accessibility evidence AND runtime same-singleton exact-envelope measurement plus real GetOrCreateAsync admission proof (compilation alone insufficient; no assumed serializer invocation without distributed cache). T001 stays read-only. T002 depends on successful T001b and records pickup SHA + seam SHA/provenance. **T008 does not reinstall the serializer** — T007/T008 retain compact-profile options/logging/boundary/runtime reuse proofs on the T001b seam. T001/T001b/T002 held until derivative sync + independent reread. Frozen 725 / 0.8 and exact parity gates unchanged. Inventory 16→17; 22/22 preserved. |
| Ledger L1 | Low | Accepted; amended | Ticket item 5 / Patron PA1 choose logging only, source-generated LoggerMessage partial; numeric PayloadBytes/BudgetBytes/Fraction, no interpolation/content. Authorized partial in brief/plan, T007/T008 and evidence require numeric-only assertion. |
| Ledger L2 | Low | Accepted; amended | AGENTS mutation rules / Patron PA1: const default, no static readonly; T011 documented worktree invocation without --since, configured kill-share/file-span/timeout safeguards preserved. |
| Ledger L3 | Low | Accepted; amended | Patron PA1 / Q7: T005a input-contract/EF retrieval then T005b compact scoring and both consumers; compiling compatible green boundaries. Tasks Tests/T004 clarify parity stays green, TDD optional; dependencies updated. |
| Ledger L4 | Low | Follow-up; no change | Challenger explicitly identifies harmless process repetition. Patron PA1 notes it as harmless follow-up, no ticket or cleanup. Retain stage distinctions for truthful reporting; no correctness/acceptance gap remains. |
| Sentry F1 | High | Accepted; amended | Original recon 59-60 / Q3/Q5 / Patron PA1: active profile identity set is sole batch key set; existing equality, missing rows retain missing-vector semantics, empty sources skip query. Brief/plan/spec PA1, T003/T004/T005a test partial/empty maps and concurrent membership case. No new transactional consistency guarantee. |
| Sentry F2 | High | Accepted; amended | Original recon 58-65 / Q4 / Patron PA1: T002 hard stop requires measured retained-map/envelope split and realistic 725-source feasibility projection below 80%; unavailable/infeasible proof blocks T003+. Preserve maps/targets/ceiling; Patron disposition required, owner only for ticket contradiction or constitution departure. Brief/plan/spec/evidence PA1 and task checkpoint. |
| Sentry F3 | High | Accepted; amended | Original recon 60/66 / Q6: warm profile hit still incurs uncached nonempty embedding batch. Brief/plan/spec/evidence PA1 and T010 label cache state, query count/cost and vector volume; T003/T005a short-circuit empty source sets. No new input cache or invented speedup SLA. |
| Sentry F4 | Medium | Accepted; amended | Historical (2026-11-15) closure: same as Ledger M3, separately tracked — T007 proves observation/admission agreement below/equal/above effective ceiling with T002 no-distributed-cache semantics; no serializer/options drift accepted. PA1 Explicit shared serializer (fca46e8c) plus sequencing ruling (85feb708): same deliberate shared-singleton disposition as Ledger M3, but install/runtime-agreement move to **T001b** before baseline; T002 measures through that seam with separate no-IDistributedCache proof; T007/T008 reuse the seam for compact-profile singleton identity, construction-only exact-envelope measurement, observation/admission agreement, and real GetOrCreateAsync reuse without reinstalling the serializer; registration/type-check alone is not that proof. |
| Sentry F5 | Medium | Accepted; amended | Patron PA1 chooses compact cache-key namespace versioning rather than relying on deployment/expiry assumptions. Plan/spec/brief PA1, T005b/T009 require legacy-key ignore/rebuild regression; generation/expiry remain intact. No claim that old in-process state actually survives deployment is needed. |
| Sentry F6 | Medium | Accepted; amended | Original recon 58 cache rejection behavior / Patron PA1: construct, observe/warn before admission, retain swallowed non-admission and return profile to callers/coalesced waiters. T007/T008/T009 prove over-ceiling warning, no size-only throw, no false warm hit; same in plan/spec PA1. |
| Sentry F7 | Medium | Accepted; amended | Original recon 31 / Q5: both consumers receive inputs and cached source shape exposes no plot vectors. T004/T005b/T006 explicitly require both paths and exact fixed-input parity; no silent fallback to cached source vectors. |
| Sentry F8 | Low | Accepted; amended | Original recon 35-36 / Patron PA1: propagate request CancellationToken into new batch. T003/T005a/T006 explicitly test cancellation at that seam. |
| Compass F1 | Medium | Accepted with escalation correction; amended | Same feasibility closure as Sentry F2. Ticket ~1.3 KB/source estimate is a risk signal, not measured impossibility. Patron PA1 keeps 725/0.8 frozen until a cited decision; failed projection goes to Patron, not automatically owner structural escalation. Charter 2.3 reserves owner checkbox for ticket contradiction/constitution departure. |
| Compass F2 | Medium | Accepted; amended | Ticket exact parity / Q5 / Patron PA1: missing pre-change fixed snapshot blocks representation changes; pending alone does not unlock T003+. T002 checkpoint, FR-011 and brief/plan/evidence PA1 explicitly distinguish honest status from permission to proceed. |
| Compass F3 | Low | Accepted; amended | Same versioned-key and legacy-rebuild closure as Sentry F5; separate low finding preserved. T005b/T009, spec edge contract and evidence receipt. |
| Compass F4 | Low | Accepted; amended | Patron PA1: three checklist implementation-free/technology-agnostic claims now unchecked and explicitly WAIVED with ticket-specific reason; waiver is not PASS. |
| Compass F5 | Low | Accepted; amended | Patron PA1: pre-change parity tests stay green across change; new edge-contract tests may be red until wiring; no artificial red state. Tasks Tests/T004/Notes and split T005 boundaries carry this. |

## Verification and freeze

**PLAN FROZEN — Keel, 2026-11-15.** Independent reread of amended brief, conclusions, assumptions, spec, plan, tasks, evidence and checklist is complete. Planning dispositions specify future implementation checks; they do not assert those tests or budgets have passed. Gate 1, implementation verification, home measurement/promotion and runtime acceptance remain open; runtime-pending remains explicit. No production code, dependency, schema, public API or generated contract file is changed.


### Final read-only cross-artifact check

SpecKit prerequisites succeeded: check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks resolved this worktree specs/DEV-510 and tasks.md (exit 0). Extensions contain no before_analyze/after_analyze hook. Analysis itself was read-only; authorized amendments were performed separately under the Conductor ask and Patron PA1, then independently reread.

| Requirement | Tasks |
| --- | --- |
| FR-001 | T005b |
| FR-002 | T003, T005a |
| FR-003 | T005a, T009 |
| FR-004 | T004, T006, T010 |
| FR-005 | T004, T006 |
| FR-006 | T007 |
| FR-007 | T010 |
| FR-008 | T001b, T007, T008 |
| FR-009 | T001b, T007, T009 |
| FR-010 | T007, T008 |
| FR-011 | T002 |
| FR-012 | T010 |
| FR-013 | T014 |
| FR-014 | T005b |
| FR-015 | T011, T012 |
| SC-001 | T002, T010 |
| SC-002 | T007, T010 |
| SC-003 | T004, T010 |
| SC-004 | T003, T005a |
| SC-005 | T001b, T007, T009 |
| SC-006 | T011, T012 |
| SC-007 | T014 |

Coverage: 22/22 (15 FR + 7 SC), 100 percent, across **17 tasks** after splitting T005 and inserting T001b. T000/T001/T001b/T013 are mapped process prerequisites/delivery/serializer-seam obligations; no unmapped work. **Prior no-outstanding claim withdrawn** until Keel independent reread of this round-1 sequencing repair. Numeric-only diagnostics, fixed request key set, empty/missing-map behavior, cancellation, versioned-key compatibility and no false admission remain explicit future test obligations, not inferred passes. Round-1 repair sync (plan.md, tasks.md T001/T001b/T002/T007/T008, spec.md, EVIDENCE.md, this Ledger M3 / Sentry F4 amendment) preserves 22/22 mapping, frozen exact-parity / 725 / 0.8 gates, home pending/Could not run status, and prior gate/runtime-pending verdicts; T000 remains complete; T001/T001b/T002 held until independent reread, after which T001b still needs compile+runtime receipts before T002.

Mechanical verification after this repair: 20 unique disposition rows, **17** unique ordered task IDs (including T001b), no stale unsplit T005 task references, three explicit unchecked checklist waivers, valid local Markdown links and no NUL characters — subject to Keel independent reread. No production gates were run for these documentation edits, and none is reported PASS. Exact implementation API verification, T001b runtime agreement, pre-change parity and feasibility checkpoints remain mandatory before Build proceeds. Freeze closes planning only; historical Gate 1 pending wording from 2026-11-15 is preserved here while Gate 1 merge receipt is recorded separately in CONCLUSIONS.md; runtime remains runtime-pending. Review closing bar and two-round/two-fix-commit cap unchanged.
