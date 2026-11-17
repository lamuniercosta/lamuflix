# Feature Specification: Read-only recommendation evaluation harness

**Feature Branch**: `feature/545-spec`
**Created**: 2026-11-17
**Status**: gate1: provisional
**Input**: Ticket DEV-545 (parent DEV-490; size L); grill-closed `brief.md` (Q1–Q9 + PC1–PC3); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; `PLAN_ADJUDICATION.md` fix list 1–10; ADR `docs/adr/0045-read-only-recommendation-evaluation.md`; Maestri notes `DEV-545`, `recon-DEV-545`, `recon-DEV-545-2` (full ticket + seed-ID absence evidence; recon HEAD `2c8f0504b3c2550c8fd655eb58457fa9658f3e25`); `specs/PRODUCT.md`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Handoff; Q1–Q9; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Quill does not invent PLAN FROZEN or a structural checkbox. Runtime home access, seed lookup, owner judgments, current-main scoring equivalence, DB invariance evidence, and Rigger baseline publication remain Phase B acceptance work.

## Clarifications

### Session 2026-11-17

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope (Parts A–D, invariant seams, tests, operating instructions, baseline evidence), placement (Q1), seed defaults/lookup timing (Q2), leave-one-out isolation (Q3), cohort/denominator semantics (Q4), breakdown/compare/pair contract (Q5), snapshot/diff envelope (Q6), quality population (Q7), loop discipline (Q8), and eight-step task/gate/baseline plan (Q9) are frozen in `brief.md` / `CONCLUSIONS.md`. Plan-challenge PC1–PC3 refine Q5/Q7/Q9 boundaries (identity-fallback diagnostic, enriched/vector null plot diagnostics, T019a manifest, SHA-256 sampling, JSON bytes, mode markers, test homes, MetricCalculator share, Process.Start test-only). Presentation `[assumed]` items in `ASSUMPTIONS.md` are Patron-ratified. Real seed IDs, credentials, judgments, measurements, and publication are Phase B evidence — not inventable here. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## What the User Will See *(mandatory)*

Nothing visible: this ticket delivers operator tooling and diagnostic seams only. The For you page, Related rail, scoring, weights, filters, reason text/order, and production recommendation behaviour are unchanged. Personal captures and judgments stay under ignored `artifacts/recommendations/` and are never shown in the SPA.

## Scope

**In scope** (`brief.md` Goal and frozen scope / Approved approach; Q1–Q9; ticket Wanted 1–21 and Done when at `recon-DEV-545-2`):

1. **Part A — Snapshot and diff**: PowerShell 7 script `scripts/run-recommendation-snapshot.ps1` with Capture/Diff, default BaseUrl `http://localhost:3100`, five ordered Related seed IDs (titles fixed now; literal home-library integers populated in Phase B), OutputPath under `artifacts/recommendations/`, deterministic JSON envelope, exact API order, explicit For-you take=30 and Related take=10, five always-present diff groups.
2. **Part B — Leave-one-out**: Real-library modes in existing `tests/LamuFlix.Evaluation` with Infrastructure project reference; immutable no-tracking read; per-seed in-memory hidden-seed copies; real `GetForYouRecommendationsQueryHandler` with full eligible take and disclosed endpoint/validator bypass; liked and watched cohorts with HR@1/3/10 and MRR.
3. **Part C — Compare and pair inspect**: Whole-library pair scoring via real `SimilarityScorer`, internal criterion breakdown seam on `SimilarityResult`/`SimilarityScorer` with zero score/reason/API change, plot cosine/rank, pool membership, matched entity names, diff-compatible artifacts.
4. **Part D — Quality report**: Coverage table at default bars, strong-reason rule without synthesizing Themes before it exists, deterministic band and strong-only samples, directed owner-judged pairs under ignored artifacts, skipped unknown/unenriched IDs.
5. **Tests and gates**: Synthetic/scorer invariance first; unit/integration/property coverage for Q3–Q7 contracts via existing xUnit/Testcontainers/WireMock.Net; Docker/HTTP/pwsh tests only under `tests/LamuFlix.Evaluation.IntegrationTests`; Phase B repo gates with honest labels; Size L three-axis review; max two review/fix rounds.
6. **Baseline evidence**: Parameter-free capture (after seed population), leave-one-out, five compares, quality report on current-main production scoring identity via T019a identity manifest; ticket-required before/after row counts + every movie review (bounded evidence, not whole-DB proof); `git status` excludes outputs/judgments/credentials; Rigger attaches baseline artifacts to DEV-545 with provenance (never credentials).

**Out of scope**:

- Changes to scoring, weights, filters, reason text/order, or the For-you query.
- New metric CI gate/threshold; changes to synthetic fixture (`baseline.json`), `scripts/run-evaluation.ps1`, or `.github/workflows/ci.yml` evaluation step.
- Runtime title lookup; charts/dashboards/web UI; committed personal outputs or connection strings.
- New NuGet/npm dependency or package version, new project, top-level folder, architectural layer, database schema, public API/OpenAPI/TypeScript shape, `Features:LocalPlay`, or secrets embedding.
- Production `Process.Start`. Narrow test-only authorization (Patron PC3): adapt existing `tests/LamuFlix.Evaluation.IntegrationTests/TestSupport/EvaluationCommand.cs` solely to launch `scripts/run-recommendation-snapshot.ps1` under IntegrationTests; zero production process execution.
- Extra capture `schemaVersion` / schema-version metadata beyond ticket Wanted 5 envelope fields.
- Inventing home-library seed IDs, measured ranks/scores, or publication success from planning.
- Claiming PLAN FROZEN, Gate 1 merge, implementation PASS, or runtime acceptance from this draft.

**Precedence**: Ticket Scope & Technical Design and acceptance criteria (`recon-DEV-545-2`) are already approved authority (`PRODUCT.md` §3). Brief Q1–Q9, plan-challenge PC1–PC3, and `CONCLUSIONS.md` govern plan boundaries (PC1–PC3 supersede conflicting Q5/Q9 summaries only as explicitly stated). ADR 0045 records the placement/boundary. Only Keel declares PLAN FROZEN after independent reread.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Snapshot**
API-returned For-you and Related lists with preserved order, explicit take values, and reason kind/text — not a re-rank.
_Avoid_: inventing enrichment diagnosis for empty successful Related lists; timestamps/host/elapsed in the capture file

**Compare ranking**
Scores one selected movie against every other enriched library movie independently of pool truncation, profile blend, and MMR.
_Avoid_: treating compare order as production For-you order

**Hidden seed**
Retains candidate identity while watched/review/profile signals are cleared in a fresh in-memory evaluation copy.
_Avoid_: DB writes; warming writes; HTTP leave-one-out

**Strong match**
Supported Collection, Director, or Themes criterion value of 1; before DEV-518 only Collection and Director are available.
_Avoid_: synthesizing Themes before it exists

**Judged pair**
Directed owner judgment from `movieId` to `otherId` with verdict `good`/`bad` and optional note.
_Avoid_: treating reverse direction as the same judgment; committing the judged-pairs file

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deterministic snapshot and diff (Priority: P1)

An operator captures For you and five Related lists from the running home instance into a deterministic JSON artifact, then diffs two compatible captures to see moved up/down, entered, left, and reasons-changed groups.

**Why this priority**: Later tickets need a comparable before/after list artifact; without Capture/Diff there is no Part A baseline.

**Independent Test**: Capture twice against unchanged quiescent data yields byte/hash-identical files; Diff prints all five groups for same-mode operands only; HTTP/schema/duplicate/cross-mode failures do not publish partial success.

**Acceptance Scenarios**:

1. **Given** a running home instance and five validated positive seed IDs, **When** Capture runs parameter-free (after Phase B ID population), **Then** the artifact records mode, seed IDs, explicit takes, For-you (take=30) and five Related lists (take=10) with exact API order and every reason kind/text, using canonical compact UTF-8 JSON (no BOM, no terminal newline, invariant finite numerics, fixed property order) published atomically via `.partial` then rename.
2. **Given** an empty successful Related response, **When** Capture runs, **Then** the list is recorded empty with status and is not treated as an error.
3. **Given** two compatible same-mode Before/After artifacts (capture/capture or compare/compare), **When** Diff runs, **Then** all five groups print (including empty groups) and reason kind/text sequence differences count as reasons changed.
4. **Given** HTTP/JSON/schema/duplicate-ID failure or mixed-mode Before/After operands, **When** Capture or Diff runs, **Then** the run fails without publishing a successful partial artifact.

---

### User Story 2 - Leave-one-out HR and MRR (Priority: P1)

An operator runs leave-one-out over liked and watched cohorts on the real library without mutating the database, obtaining HR@1/3/10 and MRR with honest denominators.

**Why this priority**: Provides the ranking-recovery baseline later tickets report against.

**Independent Test**: Integration coverage proves no hidden-target leakage, fresh per-seed copies, absent/zero-cohort behaviour, no DbContext save/add/update/remove, and real-handler ranking with disclosed bypass; ordinary `dotnet test` without `LAMUFLIX_EVALUATION_MODE` is inert for real-library modes.

**Acceptance Scenarios**:

1. **Given** a read-only library snapshot and `LAMUFLIX_EVALUATION_MODE=leave-one-out`, **When** each liked and watched seed is hidden in a fresh in-memory copy, **Then** the real For-you handler ranks with take equal to eligible candidate count and the output discloses endpoint/validator bypass plus that absolute HR/MRR are harness-relative (full-eligible ranking; only same-protocol deltas are comparable).
2. **Given** overlapping liked and watched membership, **When** metrics are reported, **Then** overlap is retained and disclosed; denominators equal attempted seeds per cohort; absent targets contribute 0; zero seeds yield null/unavailable.
3. **Given** a leave-one-out run with ticket-required before/after evidence, **When** row counts and every movie review value are compared, **Then** they are unchanged, reviewer inspection of the complete loaded path plus Testcontainers no-write proof hold, and the evidence explicitly discloses it does not prove every untested column or transient write.
4. **Given** a load/scoring failure, **When** the run executes, **Then** it fails rather than silently shrinking denominators.

---

### User Story 3 - Whole-library compare and pair inspect (Priority: P1)

An operator scores one movie against every other enriched library movie with per-criterion breakdown, plot rank, pool membership, and optional pair matched-name inspection.

**Why this priority**: Exposes what the candidate pool hides (e.g. About Time / Pirate Radio) and preserves an invariant diagnostic seam for DEV-518+.

**Independent Test**: Existing `SimilarityScorerTests` expected scores stay green; contributions sum to score under PC1 (identity-fallback diagnostic on the identical-profile zero-weight branch; other zero-denominator branches contribute 0; uncollected criteria omitted); reasons/order unchanged; two-argument `SimilarityResult` construction preserved; compare ordering/null plot diagnostics/pool flags covered.

**Acceptance Scenarios**:

1. **Given** movies with `EnrichmentStatus.Enriched`, **When** compare runs, **Then** every other enriched movie is scored and retained (self excluded; never dropped for missing/unusable vectors), ordered score descending then ID ascending, with criterion breakdown rows; usable vector pairs emit production-equivalent raw plot cosine and 1-based plot rank among usable candidates only (cosine descending then ID ascending); unusable pairs emit null cosine and null plotRank; pool flag is `(plotRank is not null && plotRank <= CandidatePoolSize) || sameCollection` with explicit diagnostic disclosure when the selected vector is unusable (ticket pool formula, not production pool parity).
2. **Given** the About Time / Pirate Radio pair on the home library, **When** compare for About Time runs, **Then** Pirate Radio has plot rank above 50 and is marked outside today's pool (runtime acceptance remains open until Phase B evidence).
3. **Given** a second movie ID, **When** pair inspect runs, **Then** only that pair prints with breakdown plus matched director/cast/keyword/genre/collection names from runner-local intersections of loaded feature IDs/names (never parsed from reason text; no new scoring terms).
4. **Given** the breakdown seam, **When** existing scorer tests and breakdown tests run, **Then** expected scores and reason contents/order are unchanged; ordinary weighted contributions use existing terms/denominator; the identical-profile zero-weight branch emits an explicit identity-fallback diagnostic entry (value=1.0, weight=0.0, contribution=1.0, ineligible for strong-match); other zero-denominator branches contribute 0.0; uncollected criteria are omitted; blend constructions keep an empty breakdown; populated pair diagnostics sum to the pair score; existing two-argument construction/deconstruction remains valid.

---

### User Story 4 - Quality coverage, samples, and judged pairs (Priority: P2)

An operator obtains a coverage table, deterministic band samples, and judged-pair shares from whole-library compare ranks without committing personal judgments.

**Why this priority**: With few liked seeds, judged pairs carry primary quality; still depends on compare ranking.

**Independent Test**: Partition sums equal enriched-selected count; deterministic hash samples; skipped IDs excluded from denominators; zero valid denominators yield null.

**Acceptance Scenarios**:

1. **Given** default bars `0.15, 0.20, 0.225, 0.25, 0.30, 0.35, 0.40`, **When** quality coverage prints, **Then** each bar has score-only and score-OR-strong lines whose four buckets partition the enriched-selected population, with median 1st/10th scores, counts, and null when none.
2. **Given** adjacent `[lower,upper)` bands and the strong-only bar (default 0.225), **When** samples are chosen, **Then** up to 12 directed non-self pairs per band (plus strong-only) are stable across processes via SHA-256 of UTF-8 invariant-decimal `selectedId + ":" + otherId` (no whitespace/newline), ordered by full digest bytes lexicographically then selectedId/otherId ascending, with golden synthetic vectors including direction reversal, disclosing undersupply.
3. **Given** an owner-corrected unversioned judged-pairs JSON including the known-miss good pair The Naked Gun → Hot Shots! unless the owner changes that verdict, **When** quality judged-pairs runs, **Then** good/bad shares and per-pair ranks/scores print; unknown/unenriched IDs are visibly skipped and excluded from denominators.
4. **Given** a completed quality run, **When** `git status` is checked, **Then** the judged-pairs file and outputs are untracked.

---

### User Story 5 - Current-main baseline publication (Priority: P1)

After implementation checks, the operator completes seed lookup, runtime runs, DB invariance proof, and Rigger publishes baseline attachments with main-equivalence provenance for downstream tickets.

**Why this priority**: Ticket Done when requires the baseline comment; without it DEV-518/544/543/540/539 cannot report before/after.

**Independent Test**: T019a identity manifest records main/harness/build/config identity and relevant production source comparison (never credentials; a raw hash alone is not equivalence proof); Rigger-verified YouTrack attachments exist with LOO harness-relative metric disclaimer.

**Acceptance Scenarios**:

1. **Given** Phase B home access, **When** the five titles are looked up once, **Then** literal `-SeedIds` defaults and help text are populated and parameter-free Capture works.
2. **Given** harness complete, **When** immediately before baseline runs the operator fills the T019a identity manifest (then-current `origin/main` SHA, harness commit + dirty status, relevant production recommendation Core/Infrastructure source comparison, API running build/config identity, effective nonsecret `RecommendationOptions` including weights, `CandidatePoolSize`, `MmrLambda`, and takes), **Then** any main SHA, relevant content, build, harness, or effective-config mismatch stops for reconciliation/recapture; feature-only diagnostic seam differences require bounded `git diff` plus score/reason invariance test receipts — a file hash alone is not semantic equivalence proof; if identity is later invalidated, recapture is required (`brief.md` steps 1/7–8; PC3 item 1).
3. **Given** identity reconciled and a quiescent data window (no scan/enrichment or user mutation during Capture×2 and DB before/after), **When** paired recommendation-input fingerprint/counts including pending/enrichment state are recorded before Capture A and after Capture B with explicit before==after equality, then snapshot (twice), diff, leave-one-out, five compares, pair inspect, and quality run using the pinned nonsecret environment interface, **Then** outputs exist under ignored artifacts; fingerprint mismatch discards/retries that window before interpreting capture hashes; changed-data windows are discarded/retried before diagnosing script nondeterminism; failed expectations remain open (no score tuning).
4. **Given** a leave-one-out run is about to start, **When** before-state (row counts and every movie review value) is captured and preserved before the run and after-state is captured immediately after the same run, **Then** comparison satisfies the ticket-required behavioral evidence boundary for SC-003 (with explicit disclosure that it does not prove every column or transient write); evidence acquired only after the run is insufficient; code-path inspection and Testcontainers no-write proof remain required.
5. **Given** completed baseline artifacts, **When** Rigger attaches them to DEV-545, **Then** the comment includes snapshot, leave-one-out, five compares, and quality report with provenance separate from deterministic JSON, including the LOO full-take/harness-relative HR/MRR validity disclaimer.

### Edge Cases

- Empty successful Related list vs HTTP/JSON/schema failure.
- Zero liked or watched seeds → metrics null/unavailable, not zero/NaN.
- Absent hidden target in returned ranking → contributes 0 to HR/MRR.
- Identical-profile zero-weight branch → identity-fallback diagnostic contribution 1.0 (not strong-eligible); other zero-denominator / inactive weighted criteria → contribution 0.0; uncollected criteria omitted (PC1).
- Missing/wrong-dimension/nonfinite/zero-norm/unusable selected or candidate vector → null rawPlotCosine and null plotRank; enriched row still scored and retained; never fabricate cosine 0.0 or drop the row (PC2).
- Themes strong-reason criterion not synthesized before DEV-518; identity-fallback never counts as strong.
- Duplicate directed judged pairs or malformed JSON → reject.
- Undersupplied band samples → disclose actual count < 12.
- Mixed-mode Diff operands (capture vs compare) → reject.
- Fixture About Time ID 90 must never substitute for a home-library seed ID.
- Ordinary `dotnet test` without `LAMUFLIX_EVALUATION_MODE` leave-one-out/compare/quality → real-library modes inert.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide Capture and Diff via `scripts/run-recommendation-snapshot.ps1` with BaseUrl default `http://localhost:3100`, ordered five-title seed contract, OutputPath default under `artifacts/recommendations/`, and Modes Capture/Diff (`brief.md` Q2/Q6).
- **FR-002**: Capture MUST call For-you with explicit take=30 and each Related seed with explicit take=10, preserve exact API order and every reason kind/text, omit timestamps/host/elapsed, and emit canonical compact `System.Text.Json` UTF-8 without BOM or terminal newline, fixed property/list order, invariant round-trip finite numerics (reject nonfinite), following the existing `MetricsSummarySerializer` temp `.partial` then atomic rename publication pattern so repeated captures on unchanged quiescent data are byte/hash-identical. No second JSON library. Envelope schema is owned by C# `RecommendationArtifactEnvelope`; script conforms via cross-language golden fixtures; no extra capture schema-version field (`brief.md` Q6; PC3 items 4/7).
- **FR-003**: Capture MUST validate exactly five positive integer seed IDs before HTTP; support explicit SeedIds overrides; perform no runtime title lookup; and after Phase B lookup embed the five home-library integers in defaults/help (never fixture ID 90).
- **FR-004**: Diff MUST accept same-mode operands only (capture/capture or compare/compare), validate compatible mode/target/take, match by movieId within each list, always print moved up, moved down, entered, left, and reasons changed (including kind/text sequence changes), and fail on malformed/duplicate-ID inputs and mixed-mode operands (`brief.md` Q6; PC3 item 4).
- **FR-005**: Real-library leave-one-out, compare, and quality MUST live in `tests/LamuFlix.Evaluation` with an Infrastructure project reference while preserving synthetic publish entry, fixtures, `scripts/run-evaluation.ps1`, and CI evaluation behaviour. Real-library modes MUST opt in only via `LAMUFLIX_EVALUATION_MODE` values `leave-one-out`, `compare`, and `quality`; ordinary `dotnet test` without those markers MUST be inert for real-library execution. Nonsecret operator inputs MUST use the pinned names in `EVIDENCE.md` §8 (`LAMUFLIX_EVALUATION_OUTPUT_PATH`; compare `LAMUFLIX_EVALUATION_SELECTED_ID` required positive int and optional `LAMUFLIX_EVALUATION_OTHER_ID` for pair inspect; quality optional `LAMUFLIX_EVALUATION_JUDGED_PAIRS_PATH`, `LAMUFLIX_EVALUATION_BARS` default `0.15,0.20,0.225,0.25,0.30,0.35,0.40`, `LAMUFLIX_EVALUATION_STRONG_ONLY_BAR` default `0.225`) with documented validation/defaults and complete scoped/restored commands — not deferred to Phase B invention (`brief.md` Q1; PC3 item 5).
- **FR-006**: Leave-one-out MUST use one immutable no-tracking library snapshot, fresh per-seed in-memory copies that clear hidden watched/review/profile and candidate IsWatched/Review while retaining the target, invoke the real For-you handler with take = eligible candidate count, disclose endpoint/validator bypass and that absolute HR/MRR are harness-relative (full-eligible ranking; only same-protocol deltas comparable), and contain no DbContext save/add/update/remove calls (`brief.md` Q3; PC3 item 6).
- **FR-007**: Leave-one-out MUST evaluate liked and watched cohorts with overlap retained/disclosed; HR@k = hits/attempted seeds and MRR = reciprocal-rank sum/attempted seeds over the full returned ranking using one shared `MetricCalculator` rank-accumulation path for @1/@3/@10 (preserve synthetic API/output/zero behaviour; real-library empty observations map to null, not synthetic zero); absent → 0; zero seeds → null/unavailable; load/scoring errors fail the run; no minimum-count suppression; no new metric CI gate; no duplicate rank loop (`brief.md` Q4; PC3 item 8).
- **FR-008**: Internal `SimilarityResult` MUST expose a non-positional initialized empty immutable criterion breakdown property that preserves existing two-argument construction and deconstruction. `SimilarityScorer` MUST populate breakdown from existing collected terms and denominator (weighted contribution = weight * value / denominator when active; contribution 0.0 when inactive or on non-identity zero-denominator branches) and, solely on the existing identical-profile `ZeroWeightResult` branch, emit an explicit identity-fallback diagnostic entry (value=1.0, weight=0.0, contribution=1.0, ineligible for strong-match); omit uncollected criteria; keep blend results empty. No score/weight/filter/reason/API/DTO/OpenAPI change (`brief.md` Q5; PC1; PC3 item 9).
- **FR-009**: Compare and quality MUST define enriched as `EnrichmentStatus.Enriched`, score and retain every other enriched movie even when a persisted `MovieFeatures` vector is missing or unusable (production missing-plot term behaviour; never drop enriched rows or fabricate cosine 0.0). Usable pairs MUST use production-equivalent cosine and 1-based plotRank among usable candidates only (cosine desc then ID asc); unusable pairs MUST emit null rawPlotCosine and null plotRank. Pool flag MUST be `(plotRank is not null && plotRank <= RecommendationOptions.CandidatePoolSize) || sameCollection` with explicit diagnostic disclosure when the selected vector is unusable. Pair-inspect matched names MUST come from runner-local intersections of loaded feature IDs/names, never parsed reason text, and MUST NOT add scoring terms. Artifacts MUST remain Diff-compatible (`brief.md` Q5/Q7; PC2; PC3 item 9).
- **FR-010**: Quality MUST implement coverage bars (default set in brief Q7), strong-reason rule without synthesizing Themes early and without treating identity-fallback as strong, four-bucket partitions, ordinal medians with counts/nulls, deterministic band/strong-only samples (up to 12) via SHA-256 UTF-8 invariant-decimal `selectedId + ":" + otherId` (no whitespace/newline; order by full digest bytes lexicographically then IDs; no `GetHashCode`/`HashCode.Combine`), golden synthetic vectors including direction reversal, and directed judged-pairs shares with visible skips (`brief.md` Q7; PC3 item 3).
- **FR-011**: Real-library credentials MUST come only from a pre-provisioned process environment variable `ConnectionStrings__DefaultConnection` (never CLI argument/history; never shell-assignment examples in operator docs that invite pasting a live secret into history). Operators and tests MUST validate presence only without reading or echoing the value. Credentials MUST never be committed, printed, echoed, logged, embedded in artifacts, evidence, provenance, notes, or ticket publication. Mode and nonsecret environment markers MUST be scoped and restored so copyable examples do not leave real-library mode enabled. Personal outputs and judged-pairs MUST remain unversioned under ignored `artifacts/recommendations/` (`brief.md` Q9; PC3 item 5).
- **FR-012**: Production `src/` changes MUST be diagnostic seams only (breakdown; runner-local hidden-seed adapter preferred; minimal listed handler/profile seam only if necessary), exhaustively listed in the PR, and MUST NOT change scoring behaviour. Test-only `Process.Start` is authorized solely to launch `scripts/run-recommendation-snapshot.ps1` via adapted `EvaluationCommand` under IntegrationTests; zero production process execution (`brief.md` Q9; PC3 item 7).
- **FR-013**: Baseline publication MUST attach snapshot, leave-one-out, five compares, and quality report to DEV-545 with current-main scoring provenance kept separate from deterministic capture JSON; harness may live on the feature branch but baseline MUST represent current main production behaviour before DEV-518/544/543/540/539 changes. T019a MUST record a scoped identity manifest (main SHA, harness commit+dirty, relevant production Core/Infrastructure recommendation source comparison, API build/config identity, effective nonsecret options) using operator commands `git rev-parse`, `git diff`, and `Get-FileHash` — any mismatch stops for reconciliation/recapture; diagnostic seam diffs need bounded diff + score/reason test receipts; a raw hash is not semantic equivalence proof. Refresh and recheck the manifest immediately before T020 and before publication. Leave-one-out DB evidence MUST capture and preserve ticket-required before-state (row counts + every movie review) before the run and compare after-state from the same run, disclosing that this boundary does not prove every column or transient write, while retaining strict no-write contract, complete loaded-path inspection, and Testcontainers no-write proof. T020 MUST enforce a quiescent data window with paired before/after recommendation-input fingerprint/count records (including stable pending/enrichment state) across Capture×2 and the DB proof window, require explicit equality, and discard/retry changed-data attempts before interpreting capture hashes — no new production job/script/metric controls. Publication and LOO output MUST disclose full-eligible/harness-relative HR/MRR validity (`brief.md` Q9 steps 7–8; PC3 items 1–2/6; SC-003).
- **FR-014**: Closing bar MUST require disposition of every verified in-scope finding including Low across Sentry, Ledger, and Compass, max two review/fix rounds, honest gate labels, and frozen scope ending with “anything else is a follow-up issue, not a finding in this round” (`brief.md` Q8).

### Key Entities

- **Capture artifact**: Deterministic JSON envelope (mode, seed IDs, takes, lists/rows with rank/movieId/title/score/reasons).
- **Compare artifact**: Shared list/row envelope plus breakdown/plot/pool fields.
- **Leave-one-out result**: Per-cohort seed counts, HR@1/3/10, MRR, absence disclosures, bypass notice.
- **Quality report**: Coverage lines, band samples, judged-pair shares and per-pair ranks/scores.
- **Judged pair**: Directed `{ movieId, otherId, verdict, note? }` under ignored artifacts.
- **Hidden-seed evaluation copy**: In-memory profile/candidate view for one seed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Two Captures of unchanged quiescent library data produce identical file hashes under the canonical UTF-8 compact JSON contract.
- **SC-002**: Diff of compatible same-mode captures always emits all five named groups; mixed-mode operands are rejected.
- **SC-003**: Leave-one-out ticket-required evidence shows row counts and every movie review value unchanged across a preserved before/after pair from the same run; reviewer confirms absence of context mutation calls and Testcontainers no-write proof. This bounded evidence does not claim proof of every untested column or transient write.
- **SC-004**: Existing `SimilarityScorerTests` expected scores remain green without edits to expected scores; contributions sum to score under PC1 identity-fallback accounting; two-argument construction preserved.
- **SC-005**: About Time compare marks Pirate Radio with plot rank > 50 and outside pool on the home library (Phase B runtime; remains open until evidenced).
- **SC-006**: Every quality coverage line’s four buckets sum to the enriched-selected population count (`EnrichmentStatus.Enriched`).
- **SC-007**: Repeated quality sample selection on unchanged data yields identical directed pairs under the pinned SHA-256 directed encoding, including across processes and direction-reversal golden vectors.
- **SC-008**: After a full baseline run, `git status` lists no output or judged-pairs files; connection strings appear in no committed file, evidence, provenance, notes, or publication.
- **SC-009**: DEV-545 ticket comment carries baseline snapshot, leave-one-out, five compares, and quality report with main-equivalence provenance and LOO harness-relative HR/MRR disclaimer (Rigger-verified).
- **SC-010**: Synthetic evaluation publish path and CI evaluation step remain behaviourally unchanged; ordinary `dotnet test` without real-library mode markers remains inert for those modes.

## Assumptions

- Presentation `[assumed]` items in `specs/DEV-545/ASSUMPTIONS.md` (compare/plot ID tie-break order; `[lower,upper)` band display; null/unavailable empty denominators) are Patron-ratified and binding for drafting.
- Home instance at `http://localhost:3100`, runtime DB read access, and owner-corrected judged pairs are Phase B prerequisites, not Phase A inventable values (`brief.md` Q2/Q9).
- Existing `artifacts/` gitignore entry covers `artifacts/recommendations/` (`.gitignore:66`; recon).
- Quill may name precise new files under ticket-owned evaluation/script areas without inventing a new project or layer (`brief.md` Q9).
- Exact helper/test file paths are pinned in `plan.md` / `tasks.md` per PC3; Phase B pickup recon may refine only for authorized demonstrated drift without changing delivery or relocating Docker/HTTP/pwsh tests out of IntegrationTests.
