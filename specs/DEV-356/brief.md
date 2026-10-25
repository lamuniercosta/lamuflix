# DEV-356 — Grill brief

Status: grill closed with Patron at Q10 of 12. Frozen decision brief for Quill drafting; plan challenge/freeze and Gate 1 remain pending. No implementation or gate clearance.

## Identity and authoritative inputs

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-356-spec`.
- Branch: `feature/356-spec`.
- Intake HEAD: `7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947`.
- Feature pin: `.specify/feature.json` points to `specs/DEV-356`; preserve it.
- Task note: `DEV-356`; factual evidence: `recon-DEV-356`.
- Decisions and full exchanges: `CONCLUSIONS.md` alongside this brief.

## Loop discipline

### Q1 — Confirmed closing bar

Patron confirmed that all Critical, High, and Medium in-scope findings block review closure; Low findings are recorded and nonblocking. Keel recommended this bar to keep replay correctness and leakage defects blocking, at the cost of fixing every evidenced Medium defect before closure.

Cited basis supplied by Patron: DEV-415 Q10, `specs/DEV-415/CONCLUSIONS.md:96-100`; DEV-416 Q1, task note `DEV-416:64,72`; task-pipeline Phase 4–5, lines 113–134; and task note `DEV-356:18` deterministic HR@10/MRR and temporal leakage acceptance criteria. See `CONCLUSIONS.md` Q1 for the complete exchange.

### Confirmed loop decisions

Q9 freezes the scope and file boundary, confirms two review rounds and at most two remediation fix commits per round, and retains the Q1 closing bar. Q10 confirms shared understanding. See Q9 below for the complete boundary and gate expectations.

## Q2 — Confirmed dependency readiness rule

Use the real DEV-416 final ranking. Phase A may continue, but implementation has a conjunctive Phase B STOP: drift recon must verify the merged DEV-416 implementation AND its exact invocation/input/output contract, AND the plan must be reconciled with that evidence. Do not invent a replacement ranker or promise a callable seam at this base. Patron confirmed no additional DEV-416 contract detail is needed in Phase A to settle later replay decisions.

Basis: task note DEV-356:18,26; recon-DEV-356:13,18-20,39; PRODUCT.md §2–3 lines 18,23–24; task-pipeline Phase 1 step 6 line 65, as cited by Patron. Complete exchange: CONCLUSIONS.md Q2.

## Recon receipt and input limitations

Conductor supplied the completed recon and DEV-356:26 receipt. recon-DEV-356:15 reports that full Scope and Technical Design is not found in intake artifacts; lines 18–20 establish the DEV-416 contract is absent at this base; lines 23–30 cover evaluation and test conventions; lines 32–36 cover governing clauses. The recon assertion that DEV-356 artifacts do not exist is a stale pre-grill observation: Q1 files now exist and are preserved.

Q3–Q10 below resolve approach, files touched, test strategy, gate expectations, task ordering, metric semantics, replay boundaries, output contract, and loop discipline. Concrete predecessor mapping remains conditional on Q2. A fuller ticket text, if obtained, must be reconciled before build; these rulings do not authorize contradiction of it.

## Q3 — Confirmed project and entry command

Create tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj as a dedicated net10.0 xUnit v3 executable project; use minimal existing CPM packages and necessary project references only. No new NuGet dependencies and no blanket copying of UnitTests references. Register in LamuFlix.sln, an acceptance/gate-forced tracked edit. Add one explicit PowerShell entry script under scripts/ that intentionally runs evaluation and writes its summary, rather than relying on incidental dotnet test execution. Patron deliberately approved charter §2.3 items 1, 2, and 6; basis and full exchange are in CONCLUSIONS.md Q3. Q6–Q7 settle the command and artifact contracts.

## Q4 — Confirmed replay semantics

One playback row is one already-qualified target observation: add no completion threshold. Traverse by timestamp then stable event ID, but build all history-dependent inputs only from rows with timestamp strictly less than target time, across all applicable users. Equal-time rows cannot teach one another. Rank and score before the target becomes visible to later observations. Recompute real DEV-416 final ranking for every observation over an immutable declared fixture catalog, with fixed seeded Random and TimeProvider inputs. Never use target/future events, feedback, or future-derived profiles. No live DB/export integration. Basis and full exchange: CONCLUSIONS.md Q4.

## Q5 — Confirmed metric semantics

For N target rows, HR@10 = hitCount/N; MRR = sum(1/oneBasedRank for targets present in the final top 10)/N, absent targets contribute zero. MRR is truncated at 10. All rows count once, including repeated plays, cold start, short/empty recommendation lists and targets excluded by real ranking policy; do not deduplicate or favorably filter observations. Duplicate movie IDs in final ranking output must fail, never be silently repaired. For N=0, observationCount and both metrics are zero. Basis and full exchange: CONCLUSIONS.md Q5.

## Q6 — Confirmed metrics-summary contract

Write deterministic UTF-8 JSON with these exact fields in this order: schemaVersion (1), fixtureId, seed, cutoff (10), observationCount, hitCount, reciprocalRankSum, hitRateAt10, mrr. Numeric results must be finite, invariant-culture JSON numbers and unrounded. cutoff=10 explicitly defines truncated mrr. Do not emit wall-clock timestamps, machine paths, or timing metadata. Default output is an untracked/ignored artifacts/evaluation path; allow caller output selection; never commit generated summaries. No HTTP API or DB schema change. Basis and full exchange: CONCLUSIONS.md Q6.

## Q7 — Confirmed command and fixture boundary

Command: pwsh -NoProfile -File scripts/run-evaluation.ps1 [-FixturePath <path>] [-OutputPath <path>] [-Seed <int>], with -Help. Defaults resolve from repository root regardless of caller cwd: tests/LamuFlix.Evaluation/Fixtures/baseline.json, artifacts/evaluation/metrics-summary.json, seed 356. Exit 0 only after complete validation, replay, and summary publication. Invalid/missing fixture, ranking failure, or write failure returns nonzero without publishing partial success. Empty event arrays are valid per Q5.

Fixtures declare fixtureId, immutable catalog/ranking inputs, and event rows with unique stable event ID, user/movie identities, and UTC timestamp. Reject duplicate event IDs, malformed/nonfinite values, and unresolved movie/catalog references. Preserve every valid row without deduplication. Exact predecessor DTO mapping is deferred only under Q2 Phase B recon/reconciliation. Basis and full exchange: CONCLUSIONS.md Q7.

## Q8 — Confirmed test strategy and task order

1. Phase B DEV-416 merged-implementation/contract recon AND plan reconciliation; STOP until all Q2 conditions hold.
2. Minimal evaluation project, fixture validation, pure metric tests.
3. Strict as-of replay/history tests.
4. Real DEV-416 fixed-fixture integration with independently worked expected HR@10/MRR and byte-identical repeated command output.
5. Command and summary-publication tests.
6. Final applicable gates.

Metric tests: ranks 1/10/11 or absent; repeated plays; short/empty rankings; zero observations; invalid duplicates. Leakage tests compare every earlier observation ranking/score under appended/shuffled future events, target exclusion, equal-time events, cross-user future history, and future-derived profiles. Also require identical summary under permutation of the same full fixture. Aggregate-only equality is insufficient. Add FsCheck [0,1] bounds and ordering/future-prefix invariants in existing UnitTests property-test location; no property-test opt-out. Use Testcontainers if real persistence/translation is exercised; never mock driver behavior. These are planned Phase B proofs, not tests already run. Basis and complete exchange: CONCLUSIONS.md Q8.

## Q9 — Confirmed frozen boundary and loop discipline

Scope: offline chronological replay through real DEV-416 final ranking; HR@10 and MRR truncated at 10; validated deterministic fixtures, command, summary contract, and Q8 proofs. No production ranking rewrite, new port/layer/package, schema/API/UI/LocalPlay/secrets change, live export, or DEV-417 comparison implementation; anything else is a follow-up issue, not a finding in this round.

Planned file set, deliberately authorized under charter item 6:

- specs/DEV-356/**.
- tests/LamuFlix.Evaluation/**, including its project, fixtures, replay/metrics/validation/output code and tests.
- scripts/run-evaluation.ps1.
- LamuFlix.sln for project registration.
- Existing UnitTests recommendation property tests and tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj only for required evaluation reference.
- .gitignore only if a narrow entry is needed to ignore generated artifacts/evaluation output.

Closing bar: Q1, Critical/High/Medium in scope block; Low is recorded and nonblocking. Round cap: two review rounds maximum, at most two remediation fix commits per round. Unresolved above-bar findings after round two go to user review on the PR without claiming closure.

Phase B gate expectations: Gauge runs applicable task-pipeline Phase 3 gates with unchanged harness thresholds: Roslyn, complexity, InspectCode, mutation where eligible, property tests, vulnerable packages, format, dotnet test, and web gates where applicable. No property-test opt-out. Scope-empty exit 2 remains SKIPPED (scope-empty), never PASS; harness opt-outs and mutation N/A retain their script verdicts, never PASS. Failed or unrunnable applicable gates block. Test-only changes do not establish production mutation eligibility. No implementation or gates during this Phase A grill. Basis and full exchange: CONCLUSIONS.md Q9.

## Q10 — Confirmed approach and closure

Patron confirms Q1–Q9 establish shared understanding. Grill closed at 10 questions of the 12-question cap. Quill may draft spec.md, plan.md, and tasks.md from this brief, recon-DEV-356, and PRODUCT, preserving every confirmed decision and Q2 STOP. If a decision is missing, report needs decision rather than inventing one.

Approach: simple code inside the dedicated evaluation project for fixture validation, strict as-of replay, real DEV-416 ranking integration, pure metric computation, and deterministic summary serialization, invoked by the explicit script. No new production abstraction/port, ADR, or glossary rewrite. The baseline fixture must be nonempty and have independently worked expected HR@10/MRR; empty-input coverage alone cannot meet acceptance.

Rulings elaborate the available ticket summary, not an unavailable full Technical Design. Any fuller ticket text must be reconciled before build without contradiction. Patron records zero known ticket contradictions, zero constitution departures, and no owner checkbox. If a later fact exposes a contradiction or necessary constitution departure, apply charter §2.3 rather than silently changing scope.

Phase B STOP remains conjunctive: verify merged DEV-416 implementation AND exact invocation/input/output contract by drift recon AND reconcile the plan before implementation. No callable seam is invented here. Plan challenge/freeze, Gate 1, implementation, and verification remain separate later steps. No tests, gates, production edits, or commits were made by this grill. No taste or over-cap assumptions were accepted, so no ASSUMPTIONS.md entry is required. Basis and full exchange: CONCLUSIONS.md Q10.

## Plan challenge interpretations — 2026-10-24 (Patron ratified; not plan freeze)

All three axis reports were received at intake HEAD `7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947`. Patron ratified the following interpretations of Q1–Q10. Detailed finding-by-finding dispositions and the single Quill correction list are in `PLAN_CHALLENGE_ADJUDICATION.md`; full Patron response is appended to CONCLUSIONS.md. These decisions precede any Quill drafting correction; Keel has not edited spec.md, plan.md, tasks.md, or DRAFTING_RECEIPT.md.

- Q2/Q4: remove the Phase 2 temporary-stub permission. T009/T010 use the real DEV-416 invocation reconciled by TB002; Phase 3 adds the nonempty baseline and independently expected metrics, rather than introducing a replacement ranker before it.
- Q8 proof order: Phase 2 proves in-memory metric/model permutation equality. Phase 4 proves repeated-command and permuted-fixture published-byte equality after serialization/publication exist. The byte-identical-output element attached to Q8 integration is Phase 4 evidence on the same real-ranker baseline. No acceptance proof is dropped.
- Q4/Q6/Q7: record concrete serializer options, UTC parsing, ordinal stable-ID ordering, and seed/TimeProvider mapping during Phase B TB002 reconciliation; do not invent a missing DEV-416 contract now. Pin an evidence-backed intentional evaluation invocation mode for the approved executable in TB002/T015 before code; incidental test execution is not the command publication path.
- Q7: T014/T015 explicitly cover ordinary write-failure integrity (no truncated success) and native child-exit plus terminating/nonterminating script-error propagation. No mandated write mechanism, additional concurrency/crash-recovery delivery, or script syntax is imposed.
- Q2/Q10: TB003 absence/reconciliation receipts cite Wisp recon rather than unsupported self-certification. No automatic STOP-enforcement script, timeout, replacement ranking path, or re-scope branch is added.
- PRODUCT §2 sequencing note is retained. Patron rules that the epic-level Epic 9 deferral is not an express Epic 7 blocking prerequisite for DEV-356; no new Epic 7 preflight blocker or owner checkbox is added (Patron-supplied DEV-415:41–44 precedent; not independently verified here).
- Q3/Q8: after all entry checks, T003 project scaffolding precedes or accompanies T001/T002 so behavioral tests can compile. No shared library, invariant duplication, architecture-test exemption, or expanded file set is authorized by speculative project-reference concerns.
- Q10 / Principle VIII: these are evaluation-internal names, with no new bounded-context domain term. Record this constitutional check. A later contrary fact stops for recon/ruling; it does not silently authorize CONTEXT.md edits.
- Q6/Q9: check generated-output ignore coverage and add the narrowly authorized entry only if needed, before first output generation. Preserve the conditional file permission.

Status separation: prior read-only analyze round 2 was CLEAN for the pre-correction working draft. Current challenge adjudication retains four blocking findings pending Quill corrections and verification. Analyze must be rechecked after correction; no new CLEAN result is claimed. Plan challenge is not closed, plan is not frozen, and Gate 1 is not established. Q2 conjunctive Phase B STOP and full-ticket reconciliation remain intact.

## Phase B reconciliation — 2026-10-25

Patron ratified this package in CONCLUSIONS.md, commit `8fedb011083e26f5921b1207f92220c32fa9113f`. Evidence base is `5bc9a3c84e127333c7de41f71d9e68ede64783f1` in `F:/Dev/LamuFlix.worktrees/DEV-356`, branch `feature/DEV-356`; factual receipts are `recon-DEV-356:67-127` and `recon-DEV-356-2:1-81`. The ruling commit changes only CONCLUSIONS.md. Earlier Phase A statuses above remain historical.

- **Invocation:** adopt evidenced Mode A (`recon-DEV-356-2:59-62,73-79`). The PowerShell script retains Q7 parameters/defaults/help, builds the approved executable and launches it as a native child with an exact dedicated evaluation-entry filter. Transport fixture path, output path, evaluation seed and an explicit evaluation-mode marker through child-scoped environment variables; restore caller environment afterward. Child performs validation/replay/explicit publication. Publication requires the marker and validated parameters; ordinary suite execution never publishes. Zero selected entry tests must fail the command; runner exit zero alone is insufficient. Never use `-ignoreFailures`, raw evaluation parameters as runner flags, or runner `:seed` as evaluation seed. Propagate build/child/script failures. Exact entry/type/carrier names are local implementation choices in authorized files. No custom Main or new package.
- **Serializer options:** constitution-mandated System.Text.Json; validated compact output, indentation disabled, UTF-8 without BOM or trailing newline. Write exactly `schemaVersion`, `fixtureId`, `seed`, `cutoff`, `observationCount`, `hitCount`, `reciprocalRankSum`, `hitRateAt10`, `mrr` in that order; finite invariant-culture JSON numbers with no manual rounding or volatile metadata. Preserve Q7 ordinary write-failure integrity without adding concurrency/crash-recovery scope.
- **UTC/order/seed/time:** require explicit UTC fixture timestamps and compare instants; use ordinal stable event-ID equality and ordering after timestamp. Reset injected Random from transported evaluation seed (default 356) per evaluation. Inject a TimeProvider fixed to target-observation UTC time for each observation. Derive all history-dependent inputs strictly from timestamp < target across applicable users; equal-time rows cannot teach one another; expose target only after scoring. Selector itself takes no clock/random arguments: these belong to evaluation replay/history preparation.
- **Real DEV-416 call mapping:** reference Core and directly invoke `MmrSelector.Select(IReadOnlyList<MmrCandidate>, int, double, SimilarityWeights, GenreWeights)` (`recon-DEV-356:82-99`). Declared catalog IDs/features and finite non-negative as-of relevance map to `MmrCandidate(MovieId, Relevance, MovieFeatures)`. When deriving signed blended relevance, apply the evidenced handler offset `max(0, -minimumScore)` over the full scored pool and preserve original scores for observation evidence. Pass the full eligible candidate pool, `take = 10` and declared lambda/SimilarityWeights/GenreWeights. Consume returned `ImmutableList<MovieId>` order without re-sorting. Follow evidenced validation, including unique IDs and finite/ranged inputs. History-dependent relevance/weights obey Q4; immutable fixture inputs never authorize future-derived profiles. This consumes the real final MMR stage; no replacement ranker, new production seam or handler rewrite.
- **TB003:** Wisp found no fuller DEV-356 Scope & Technical Design in the named roots (`recon-DEV-356:108-113`). This bounded receipt supersedes the broken historical `recon-DEV-356:15` absence citation. No live YouTrack comparison was performed; any later fuller text requires reconciliation before build without contradiction.
- **TB005:** spec FR-001–FR-014, plan and T001–T020 remain aligned with Q1–Q10 and ratified challenge interpretations. Preserve task order, every Q8 proof, file scope, gate verdict distinctions, closing bar and round cap. Supersede old absent-DEV-416 observations with `recon-DEV-356:74-106`. The Phase A feature pin is absent here (`recon-DEV-356:17,64,127`); preserve it if present, but do not create it or impose a new blocker.

Record TB002/TB003/TB005 completion in note DEV-356. Q2 clearance is separate from full Phase B entry: TB001 and TB004 require their own receipts before Anvil starts T003/T001. No implementation or gate pass is claimed.
