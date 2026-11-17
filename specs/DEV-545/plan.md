# Implementation Plan: Read-only recommendation evaluation harness

**Branch**: `feature/545-spec` | **Date**: 2026-11-17 | **Spec**: `specs/DEV-545/spec.md`

**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-545-spec`

**Input**: Grill-closed `brief.md` (Q1–Q9); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; ADR `docs/adr/0045-read-only-recommendation-evaluation.md`; Maestri notes `DEV-545`, `recon-DEV-545`, `recon-DEV-545-2` (Wanted 1–21, Done when, Where it lands, open points; recon HEAD `2c8f0504b3c2550c8fd655eb58457fa9658f3e25`); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft — Quill Spec Kit plan from closed grill; plan-challenge PC1–PC3 binding; plan challenge repair round 1 applied. Not PLAN FROZEN. Gate 1, implementation, and runtime acceptance remain open. Keel independent reread pending before any freeze disposition.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `checklists/harness.md`, `EVIDENCE.md`, `DRAFTING_RECEIPT.md`, `PLAN_ADJUDICATION.md` (Keel-owned). No `research.md`, `data-model.md`, or `contracts/` — decisions frozen in the brief + PC1–PC3; no public API delta.

## Summary

Deliver a read-only real-library recommendation evaluation harness (Parts A–D): deterministic For-you/Related snapshot+diff script; leave-one-out HR@1/3/10 and MRR over liked/watched cohorts; whole-library compare with invariant criterion breakdown and pair inspect; quality coverage, band samples, and owner-judged pairs. Reuse `tests/LamuFlix.Evaluation` with an Infrastructure project reference; keep Core free of EF/HTTP; preserve synthetic evaluation and CI. Establish and publish the current-main baseline that DEV-518, DEV-544, DEV-543, DEV-540 and DEV-539 report against.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Core`, `LamuFlix.Infrastructure`, `tests/LamuFlix.Evaluation`) and PowerShell 7 for `scripts/run-recommendation-snapshot.ps1` per `specs/PRODUCT.md` §1 and brief Q1/Q6/Q9.

**Primary Dependencies**: Existing stack only — recommendation handlers/scorer, EF profile/candidate sources, evaluation entry/metrics, xUnit/Shouldly, centrally pinned `Testcontainers.PostgreSql` and `WireMock.Net`. No new NuGet/npm package or version change (`brief.md` Q1/Q9; PC3 item 7). Authorized IntegrationTests PackageReference additions only for those two existing CPM pins if absent.

**Storage**: Read-only PostgreSQL via existing Infrastructure persistence (`EfRecommendationProfileSource` no-tracking pattern). No schema/migration change. Real-library connection via pre-provisioned process environment `ConnectionStrings__DefaultConnection` only (presence-only checks; never CLI argument/history; never shell-assignment examples that paste secrets into history). Artifacts under ignored `artifacts/recommendations/`.

**Testing**: Existing xUnit v3 + Shouldly; Postgres Testcontainers and WireMock.Net only under `tests/LamuFlix.Evaluation.IntegrationTests` — never mock EF driver translation; no Pester/new framework (`brief.md` Q9; PC3 item 7). Pure RealLibrary/math tests in Evaluation; scorer/breakdown in UnitTests. Snapshot script behaviour via adapted `EvaluationCommand` pwsh child-process (test-only `Process.Start`).

**Target Platform**: Local home API at `http://localhost:3100` for Capture; evaluation executable modes for leave-one-out/compare/quality; home Postgres read access for Phase B baseline.

**Project Type**: Existing ports-and-adapters .NET API + evaluation console/test project; operator scripts. No `/web` changes.

**Performance Goals**: Full eligible-candidate ranking for leave-one-out (validator/endpoint bypass disclosed); whole-library compare over enriched movies. No invented latency SLA.

**Constraints**: Charter §2.3 items 1–6 deliberately ruled (Q1–Q9). Zero score/weight/filter/reason/API change. Prefer runner-local hidden-seed adapter; list any minimal production seam in the PR. Honest gate labels; never lower thresholds; never merge. Closing bar includes Low; Size L requires Sentry/Ledger/Compass; max two review/fix rounds (Q8). Seed IDs/credentials/judgments/publication are Phase B evidence.

**Scale/Scope**: Size L; Parts A–D + invariant seams + tests + baseline evidence. Downstream blockers: DEV-518, DEV-544, DEV-543, DEV-540, DEV-539.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox from this draft.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **PASS** — reuse existing handlers/scorer/profile ports; runner-local adapters for hidden seed; Core stays free of EF/HTTP (Q1/Q3). |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — invoke existing `GetForYouRecommendationsQueryHandler` directly in evaluation; no MediatR; no new production handler required. |
| III (Typed query model) | MovieQuery | **N/A / PASS** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — read-only; no Movie mutation; leave-one-out uses in-memory copies only. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no public API/error-contract change; script/runner fail-fast on invalid seeds/HTTP/schema. |
| VI (Observability) | Safe diagnostics | **PASS** — operator artifacts only; never print/embed credentials or connection strings (FR-011). |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay or secrets embedding; connection string environment-only for real-library modes; production Process.Start forbidden; Patron PC3 authorizes test-only Process.Start solely to launch `scripts/run-recommendation-snapshot.ps1` via adapted IntegrationTests `EvaluationCommand`. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new glossary/CONTEXT term; ADR 0045 already authored by Keel; spec-local vocabulary only. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit for breakdown/diff/quality math; Testcontainers for read-only loader/hidden-seed; home runtime for baseline publication. |
| Tech stack / API docs | OpenAPI chain | **PASS** — no public DTO/route/status change; no OpenAPI/TS generation edit (Q5). |
| Governance / Static-Analysis Gates | Honest exits | Phase B: build/test, Roslyn, complexity (incl. applicable refactor obligation), InspectCode, property, vulnerable packages, format, mutation when `src/` changes; web SKIPPED if `/web` untouched; never fold non-pass into PASS. |

### Complexity Tracking

No constitution violations requiring justification. Infrastructure project reference on the existing evaluation project is Patron Q1–approved and avoids a new console project/layer.

## Approach

- **Phase A (this draft)**: Quill writes `spec.md` / `plan.md` / `tasks.md` / checklists / `EVIDENCE.md` / receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon/PRODUCT/ADR only. Keel runs `/speckit-analyze` and independent reread before any PLAN FROZEN claim. Spec PR: Bernstein coordinates; Rigger prepares/opens; user merges; Keel records Gate 1 merge receipt. No Build work before Gate 1.
- **Phase B pickup**: Fresh drift recon; Gauge baseline; exact path inventory for any helper files named only by convention here.
- **Task order (`brief.md` steps 1–8 / Q9)**:
  1. Preserve synthetic invocation and existing scorer expected scores; shared Q6 `RecommendationArtifactEnvelope` + canonical serializer following `MetricsSummarySerializer`; extend `MetricCalculator` with one shared rank-accumulation path for @1/@3/@10 (synthetic zero preserved; real-library null wrapper). No synthetic fixture/history/CI edits.
  2. Q5 breakdown seam + tests: non-positional empty-default immutable breakdown property; PC1 identity-fallback diagnostic; score/reason invariance; contribution sum scope = populated pair diagnostics only; blend empty; matched names from runner-local ID/name intersections.
  3. Read-only loader + Q3 hidden-seed adapters (Testcontainers IntegrationTests prove loader no-tracking/no-write; no driver mocks; secrets via pre-provisioned `ConnectionStrings__DefaultConnection` presence-only). Mode markers + inertness + scoped/restored nonsecret env tests on EvaluationEntry.
  4. Leave-one-out + whole-library compare + pair inspect using Q3–Q5/PC2 (enriched retention, nullable plot diagnostics, disclosed ticket pool formula; missing/wrong-dimension/nonfinite/zero-norm/tie cases) with pinned env names `LAMUFLIX_EVALUATION_SELECTED_ID` / `OTHER_ID` / `OUTPUT_PATH`.
  5. Snapshot/diff script Q6 with IntegrationTests WireMock+pwsh coverage: byte-equal captures, exact order/takes, five-ID validation, same-mode Diff only, culture/adversarial finite-number golden vectors, atomic `.partial` publication.
  6. Quality Q7 with SHA-256 directed sample encoding golden vectors (incl. direction reversal), partition/strong/Themes/median/undersupply/judged-share tests, and pinned `JUDGED_PAIRS_PATH` / `BARS` / `STRONG_ONLY_BAR` / `OUTPUT_PATH`.
  7. One-time home seed ID lookup → literal defaults/help. **T019a identity manifest** then **T020 quiescent window** with paired before/after recommendation-input fingerprint/counts (incl. pending/enrichment state) across Capture×2 + DB proof window, plus ticket-required DB before/after counts+reviews (bounded evidence disclosure). Refresh manifest before T020 and before publication. Discard/retry changed-data windows before interpreting capture hashes.
  8. Rigger attaches baseline artifacts + provenance + LOO harness-relative disclaimer to DEV-545. Publication notes do not replace T019a.
- **Do not**: change scores/weights/filters/reasons; add metric CI gate; commit/echo credentials; invent seed IDs or measurements; synthesize Themes; claim runtime PASS or PLAN FROZEN from planning.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-545/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file (worktree / Spec PR copy; brief Q9)
├── tasks.md
├── EVIDENCE.md          # Phase B runbook / evidence slots
├── DRAFTING_RECEIPT.md
└── checklists/
    ├── requirements.md
    └── harness.md

docs/adr/
└── 0045-read-only-recommendation-evaluation.md
```

No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief; operator/internal contracts only; no public API delta.

### Source Code (authorized boundary — brief Q9; PC3 item 7; recon Where it lands)

Exact pinned homes (no alternate locations):

```text
scripts/
└── run-recommendation-snapshot.ps1          # NEW — Capture/Diff (Q2/Q6)

tests/LamuFlix.Evaluation/
├── LamuFlix.Evaluation.csproj               # add ProjectReference to LamuFlix.Infrastructure; preserve synthetic packages/refs
├── Entry/EvaluationEntry.cs                 # extend dispatch for LAMUFLIX_EVALUATION_MODE leave-one-out|compare|quality + pinned nonsecret env inputs; preserve synthetic publish
├── Entry/EvaluationEntryModeTests.cs        # EXTEND — inert without marker; mode opt-in; presence-only credential; scoped/restored markers
├── Metrics/MetricCalculator.cs              # EXTEND — one shared rank-accumulation path for @1/@3/@10; preserve synthetic zero API
├── Metrics/MetricCalculatorTests.cs         # EXTEND — shared path + synthetic zero preservation
├── Output/MetricsSummarySerializer.cs       # PATTERN ONLY — reuse .partial atomic publish + compact UTF-8 writer (no second serializer library)
├── RealLibrary/                             # NEW folder (Quill-named under ticket-owned evaluation area)
│   ├── RealLibrarySnapshotLoader.cs         # immutable no-tracking read
│   ├── HiddenSeedEvaluationCopy.cs          # per-seed in-memory hide adapter (prefer runner-local)
│   ├── LeaveOneOutRunner.cs
│   ├── LeaveOneOutRunnerTests.cs            # pure LOO/cohort/denominator/bypass-disclaimer tests (no Docker)
│   ├── CompareRunner.cs
│   ├── CompareRunnerTests.cs                # pure compare/pair/PC2 vector diagnostics (no Docker)
│   ├── QualityReportRunner.cs
│   ├── QualityReportRunnerTests.cs          # partition/strong/Themes/median/judged shares
│   ├── BandSampleSelector.cs                # SHA-256 directed sample selection
│   ├── BandSampleSelectorTests.cs           # golden vectors incl. direction reversal
│   ├── RealLibraryMetricCalculator.cs       # nullable empty-observation wrapper over MetricCalculator
│   ├── RecommendationArtifactEnvelope.cs    # C#-owned capture/compare envelope (no schemaVersion field)
│   ├── RecommendationArtifactSerializer.cs  # canonical compact UTF-8 + .partial atomic publish
│   ├── RecommendationArtifactEnvelopeTests.cs
│   └── Fixtures/
│       ├── capture-envelope.golden.json     # cross-language golden (C# + script)
│       └── band-sample-hash.golden.json
└── (existing Fixtures/Replay untouched except necessary dispatch hooks)

tests/LamuFlix.Evaluation.IntegrationTests/
├── LamuFlix.Evaluation.IntegrationTests.csproj  # authorize PackageReference to existing CPM Testcontainers.PostgreSql + WireMock.Net if absent; no version change
├── TestSupport/EvaluationCommand.cs         # ADAPT — add snapshot-script launch for scripts/run-recommendation-snapshot.ps1; preserve run-evaluation.ps1 path; test-only Process.Start
├── Entry/RecommendationSnapshotCommandTests.cs  # NEW — WireMock.Net fake HTTP + pwsh Capture/Diff byte/order/take/fail/same-mode tests
└── RealLibrary/RealLibraryReadOnlyLoaderTests.cs # NEW — Testcontainers no-tracking + no-write proof

tests/LamuFlix.UnitTests/Features/Recommendations/
├── SimilarityScorerTests.cs                 # expected scores stay green (no expected-score edits)
└── SimilarityResultBreakdownTests.cs        # NEW — PC1 identity/non-identity zero-denominator, ordinary sums, no strong eligibility, empty blend default, construction/deconstruction

src/LamuFlix.Core/Features/Recommendations/
├── SimilarityResult.cs                      # non-positional initialized empty immutable breakdown property (Q5/PC1/PC3)
├── SimilarityScorer.cs                      # populate breakdown + identity-fallback diagnostic (Q5/PC1)
└── GetForYouRecommendationsQueryHandler.cs  # ONLY if minimal listed hidden-seed seam proven necessary

src/LamuFlix.Infrastructure/Persistence/
├── EfRecommendationProfileSource.cs         # read path reuse; no write behaviour change expected
└── EfRecommendationCandidateSource.cs       # PC2 vector/IsUsable semantics reference; no behaviour change expected

# Explicitly unchanged
scripts/run-evaluation.ps1
tests/LamuFlix.Evaluation/Fixtures/baseline.json
.github/workflows/ci.yml
web/                                         # no edits planned
```

**Structure Decision**: Extend existing evaluation project + one new operator script + minimal Core diagnostic seam. No new project/layer/package. Docker/HTTP/pwsh exclusively under IntegrationTests.

## Phase 0 / Phase 1 note

Research and data-model/contract generation are **skipped as separate files**: every NEEDS CLARIFICATION that would have driven them was closed in grill Q1–Q9. Operator artifact shapes are specified in `spec.md` FR-001–FR-010 and `checklists/harness.md`. Public OpenAPI contracts are unchanged.

## Gate expectations

- **Phase A**: documentation + Keel `speckit-analyze` only; do not claim implementation/mutation results.
- **Phase B / pre-PR**: repo-required build/test, Roslyn, complexity, InspectCode, property, package security, format, and mutation when `src/` seams change (`scripts/run-mutation.ps1` without `--since` from worktrees). Honest SKIPPED/N/A/SKIP/Could not run. No new metric CI gate. Frontend gates only if `/web` actually changes via new ruling.
- **Review**: Size L three axes; Q8 two-round closing bar including Low; never merge.

## EVIDENCE / baseline provenance (Phase B)

Fill `specs/DEV-545/EVIDENCE.md` slots using the documented operator commands (nonsecret inputs only):

- **§0 T019a identity manifest**: `git rev-parse origin/main`, harness `git rev-parse HEAD` + dirty status, `git diff` of relevant production recommendation Core/Infrastructure paths vs main, API build/config identity, effective nonsecret `RecommendationOptions` (weights, CandidatePoolSize, MmrLambda, takes), `Get-FileHash` where useful. Refresh before T020 and before publication. Mismatch → stop/reconcile/recapture. Diagnostic seam diffs need bounded diff + score/reason test receipts. Never credentials.
- **§1 seed IDs**; **§2 Capture hashes** under quiescent window with paired before/after recommendation-input fingerprint/counts (incl. pending/enrichment state) and equality/discard-retry; **§3 LOO** with bypass + harness-relative disclaimer, ticket-required before/after counts+every review (disclose untested columns/transient writes), code-path inspection + Testcontainers receipt, scoped/restored mode commands (no connection-string shell assignment); **§4 compares/pair inspect** with pinned `SELECTED_ID`/`OTHER_ID`/`OUTPUT_PATH` (About Time/Pirate Radio remains open until evidenced); **§5 quality** with pinned `JUDGED_PAIRS_PATH`/`BARS`/`STRONG_ONLY_BAR`/`OUTPUT_PATH`; **§6 scrub** including evidence/provenance credential scrub; **§7 publication** with LOO validity disclaimer; **§8 complete pinned nonsecret operator interface** (not deferred).

Rigger alone publishes YouTrack attachments. Recapture required if identity is invalidated after the pre-run gate.
