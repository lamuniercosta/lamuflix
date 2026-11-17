# Requirements Quality Checklist: DEV-545 harness contracts

**Purpose**: Unit tests for English — validate clarity/completeness of Parts A–D operator contracts in `spec.md` / `plan.md` / `tasks.md` before Gate 1.
**Created**: 2026-11-17
**Audience**: Quill (author), Keel (analyze), Gate 1 reviewers
**Depth**: Formal pre-Gate-1 requirements gate (not runtime verification)
**Status**: Plan challenge repair round 2 wording applied (PC3 items 5–6 residuals); Keel independent reread pending. Not PLAN FROZEN. Not a Gate 1 claim.

## Scope & boundaries

- [x] CHK001 Are Parts A–D each named with in-scope deliverables and explicit out-of-scope exclusions (scoring/CI/fixture/title-lookup/UI/committed outputs)?
- [x] CHK002 Is “anything else is a follow-up issue, not a finding in this round” present for loop/frozen scope?
- [x] CHK003 Are Charter §2.3 surfaces (deps, project/layer, schema, public API, LocalPlay/secrets, production Process.Start, unnamed file rewrites) explicitly non-goals, with the narrow PC3 test-only Process.Start authorization for `EvaluationCommand` → `run-recommendation-snapshot.ps1` stated?

## Snapshot / Diff (Part A)

- [x] CHK004 Are Capture defaults (BaseUrl, takes 30/10, artifacts path, five ordered titles) specified without inventing home IDs?
- [x] CHK005 Is determinism defined (canonical compact UTF-8 System.Text.Json, no BOM/terminal newline, invariant finite numerics, fixed property/list order, `.partial` atomic publish, byte/hash identity)?
- [x] CHK006 Are empty successful Related lists distinguished from HTTP/JSON/schema failures?
- [x] CHK007 Are all five Diff groups mandatory even when empty, including reasons-changed on kind/text sequence, with same-mode-only operands and mixed-mode rejection?

## Leave-one-out (Part B)

- [x] CHK008 Is hidden-seed isolation defined (immutable no-tracking snapshot, fresh per-seed copy, clear profile+candidate signals, retain target)?
- [x] CHK009 Are direct-handler take=eligible-count, endpoint/validator bypass disclosure, and harness-relative HR/MRR validity disclaimer required?
- [x] CHK010 Are cohort overlap, denominators, absent=0, zero-seed=null, shared MetricCalculator rank path, and fail-on-load/score-error rules unambiguous?
- [x] CHK011 Is DB non-mutation both behavioural (before-state captured and preserved before leave-one-out; after-state captured and compared immediately after the same run; counts+every review as ticket-required bounded evidence with untested-column/transient-write disclosure) and code-level (no save/add/update/remove + Testcontainers no-write)?

## Compare / breakdown (Part C)

- [x] CHK012 Is breakdown derived from existing terms with PC1 identity-fallback diagnostic on the identical-profile zero-weight branch, contribution 0 on other zero-denominator/inactive cases, uncollected criteria omitted, and empty-default non-positional property preserving two-argument construction?
- [x] CHK013 Are score/reason/API non-change and existing SimilarityScorerTests invariance required?
- [x] CHK014 Are compare ordering, PC2 enriched/vector null plot diagnostics, disclosed ticket pool formula, and pair matched-name intersection fields specified?
- [x] CHK015 Is About Time / Pirate Radio plot-rank>50 outside-pool acceptance stated as runtime evidence, not inventable now?

## Quality (Part D)

- [x] CHK016 Are default bars, dual lines per bar, four-bucket partition, and median/count/null rules complete?
- [x] CHK017 Is Themes non-synthesis before DEV-518 explicit, and identity-fallback excluded from strong-match?
- [x] CHK018 Are band `[lower,upper)`, SHA-256 UTF-8 directed `selectedId:otherId` sampling (digest-byte order then IDs; golden vectors incl. direction reversal), strong-only bar, undersupply disclosure, and directed judged-pair schema/denominators complete?
- [x] CHK019 Is known-miss good pair (The Naked Gun → Hot Shots!) retention unless owner changes verdict stated?

## Secrets, baseline, gates

- [x] CHK020 Are real-library credentials environment-only (`ConnectionStrings__DefaultConnection`), never CLI-echoed/printed/embedded, with evidence/provenance scrub and unversioned personal outputs?
- [x] CHK021 Are baseline artifact set, T019a identity manifest (main/harness/build/config/source comparison; recheck before T020 and publication; raw hash not equivalence proof), quiescent-window paired before/after recommendation-input fingerprint/counts (incl. pending/enrichment state) with equality + discard/retry before hash interpretation across Capture×2 and the DB proof window, and Rigger-only publication with LOO validity disclaimer specified?
- [x] CHK022 Are Phase A vs Phase B gate expectations and honest non-PASS labels distinguished?
- [x] CHK023 Is Size L three-axis closing bar with Low included and two-round cap specified?

## Regression test assignment (analyze fix round 1 + plan challenge repair round 1)

- [x] CHK024 Are loader no-tracking, cohort overlap/attempted denominators, and load/scoring failure integrity assigned to concrete pinned files (T009 `RealLibraryReadOnlyLoaderTests.cs`; T012 `LeaveOneOutRunnerTests.cs`)?
- [x] CHK025 Are plot rank/pool/null-vector flags and pair matched-name assertions assigned (T012 `CompareRunnerTests.cs`; not left only on T011 implementation)?
- [x] CHK026 Are Capture exact order, explicit takes, five-ID validation, duplicate/HTTP rejection without partial publication, and same-mode Diff assigned (T015 `RecommendationSnapshotCommandTests.cs` via adapted `EvaluationCommand` + WireMock)?
- [x] CHK027 Are quality every-line partition totals, strong-only/Themes non-synthesis, ordinal medians/counts/nulls, SHA-256 undersupply samples, and directed judged shares/denominator integrity assigned (T016–T018 pinned RealLibrary files)?
- [x] CHK028 Are `LAMUFLIX_EVALUATION_MODE` markers, inertness without marker, pinned nonsecret env names with validation/defaults (`SELECTED_ID`/`OTHER_ID`/`OUTPUT_PATH`/`JUDGED_PAIRS_PATH`/`BARS`/`STRONG_ONLY_BAR`), complete scoped/restored commands for leave-one-out/compare/pair/quality, pre-provisioned presence-only credentials (no connection-string shell-assignment examples), and T010/T011/T016/T018 consistency documented as complete in EVIDENCE §3/§4/§5/§8 (not deferred to Phase B)?
- [x] CHK029 Are exact IntegrationTests vs Evaluation vs UnitTests homes pinned with authorized CPM Testcontainers.PostgreSql/WireMock.Net only, C#-owned envelope (no schemaVersion), and MetricCalculator shared rank path named?
- [x] CHK030 Are PC1 identity-fallback and PC2 enriched/vector null-plot cases covered in T004/T012 acceptance text without blanket zero-denominator contribution-zero wording?

## Notes

- Intent signals: operator harness, read-only, baseline for DEV-518+.
- Checklist generated under `/speckit-checklist` with focus “harness contracts / Gate 1 readiness”; clarifying questions skipped because brief Q1–Q9 already fixed scope, risk, and depth.
- 2026-11-17 analyze fix round 1: CHK011/CHK021 tightened; CHK024–CHK027 added.
- 2026-11-17 plan challenge repair round 1: CHK003/CHK005/CHK007/CHK009–CHK012/CHK014/CHK017–CHK021/CHK024–CHK027 reconciled; CHK028–CHK030 added for PC1–PC3. Items are checked only as English completeness of the repaired drafts. Keel independent reread pending; no PLAN FROZEN / Gate 1 / implementation PASS claim.
- 2026-11-17 plan challenge repair round 2: CHK021 paired before/after fingerprint slots; CHK028 complete pinned nonsecret operator interface (no Phase B deferral; no connection-string shell assignment). Keel final independent reread pending; no PLAN FROZEN / Gate 1 claim.
