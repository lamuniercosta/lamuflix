# Feature Specification: DEV-356 Offline Evaluation Harness (Hit Rate@10 and MRR)

**Feature Branch**: `feature/356-spec`

**Created**: 2026-10-24

**Status**: Draft — gate1: provisional

**Input**: Ticket DEV-356 (size M, parent DEV-288, no ui tag); settled grill brief `specs/DEV-356/brief.md` (grill closed by Patron at Q10 of 12; Q1–Q10); facts from note `recon-DEV-356`; Patron rulings in `specs/DEV-356/CONCLUSIONS.md`; product authority in `specs/PRODUCT.md` sections 1–3 and 5. No decisions beyond the brief and those rulings. No `design.md` (no ui tag). No clarify/checklist or ADR on this M path. Plan challenge/freeze and Gate 1 remain separate later steps and are not claimed here.

## Dependency and intake caveats *(mandatory)*

- **Q2 conjunctive Phase B STOP**: Use the real DEV-416 final ranking. Phase A may continue, but implementation MUST NOT begin until Phase B drift recon verifies the merged DEV-416 implementation AND its exact invocation/input/output contract, AND the plan is reconciled with that evidence. Do not invent a replacement ranker or promise a callable seam at this base (brief Q2; CONCLUSIONS.md Q2; recon-DEV-356:18–20,39).
- **Full-ticket reconciliation**: Full Scope & Technical Design was not found in intake artifacts (recon-DEV-356:15). These rulings elaborate the available ticket summary (task note DEV-356:18). Any fuller ticket text obtained later MUST be reconciled before build without contradiction (brief Q10; CONCLUSIONS.md Q10). Patron records zero known ticket contradictions, zero constitution departures, and no owner checkbox.
- **Separate boundaries**: This draft does not clear plan challenge/freeze or Gate 1. Those remain separate later claims (brief Q10).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deterministic offline replay scores Hit Rate@10 and MRR (Priority: P1)

An offline evaluation harness replays historical playback events chronologically against recommendations built only from earlier history, scores each target movie against the final top-10 from real DEV-416 ranking, and produces deterministic Hit Rate@10 and MRR (truncated at 10) for a validated fixture.

**Why this priority**: Ticket AC requires deterministic HR@10 and MRR on fixture replay; this is the core delivery.

**Independent Test**: With a fixed nonempty baseline fixture, fixed seed and TimeProvider inputs, and real DEV-416 final ranking available under the Q2 STOP, run evaluation and match independently worked expected HR@10 and MRR; repeated evaluation of the same inputs yields identical metric numerics. Byte-identical published command output on that baseline is Phase 4 / US3 evidence (Patron-ratified Q8 interpretation; Compass F2).

**Acceptance Scenarios**:

1. **Given** a validated nonempty fixture with immutable catalog/ranking inputs and playback event rows, **When** evaluation replays observations in timestamp then stable event-ID order through real DEV-416 final ranking, **Then** HR@10 and MRR (truncated at 10) match independently derived expected values for that fixture and seed.
2. **Given** the same fixture, seed, and inputs, **When** evaluation runs twice, **Then** metric numerics are identical.
3. **Given** each target observation, **When** history-dependent inputs are built, **Then** only rows with timestamp strictly less than the target timestamp (across all applicable users) are used; equal-time rows cannot teach one another; the target is scored before becoming visible to later observations.
4. **Given** ranking output that contains duplicate movie IDs, **When** validation runs, **Then** evaluation fails rather than silently repairing duplicates.

---

### User Story 2 - Temporal leakage isolation (Priority: P1)

Appending or shuffling future events must not change earlier observations' rankings or scores. Aggregate-only equality is insufficient.

**Why this priority**: Ticket AC requires a leakage test with shuffled future events; Q8 freezes per-observation evidence.

**Independent Test**: Compare every earlier observation ranking and score under appended/shuffled future events, target exclusion, equal-time events, cross-user future history, and future-derived profiles; prove full-fixture permutation yields equal in-memory metrics/model results. Published-byte equality for equivalent original/permuted fixtures is Phase 4 / US3 evidence after serializer/publication exist (Compass F1).

**Acceptance Scenarios**:

1. **Given** a fixture and a set of earlier observations already scored, **When** future events are appended or shuffled, **Then** each earlier observation's ranking and score remain unchanged.
2. **Given** equal-time events, **When** as-of history is built for one of them, **Then** the other equal-time rows are excluded from that observation's history.
3. **Given** cross-user future history or future-derived profiles, **When** an earlier observation is re-evaluated, **Then** those future inputs do not enter its as-of computation.
4. **Given** the same complete fixture permuted in presentation order, **When** evaluation runs, **Then** in-memory metrics and observation-level ranking/score model results are identical (publication not required for this story's proof).

---

### User Story 3 - Single command publishes the metrics summary (Priority: P1)

One explicit PowerShell entry command validates the fixture, runs replay, and writes the deterministic UTF-8 JSON metrics summary for DEV-417—never as an incidental test side effect.

**Why this priority**: Ticket AC requires a single command that writes the metrics summary; Q3/Q6/Q7 freeze command and file contracts.

**Independent Test**: Invoke `scripts/run-evaluation.ps1` with defaults and with explicit paths using the Phase B–recorded intentional evaluation invocation mode; prove exit 0 only after complete publication; prove nonzero exit without truncated/partial success on invalid/missing fixture, ranking failure, ordinary write failure, native child nonzero exit, or terminating/nonterminating script error; prove byte-identical published summaries for repeated identical-fixture runs and for equivalent original/permuted fixtures after serializer/publication exist.

**Acceptance Scenarios**:

1. **Given** repository-root defaults, **When** `pwsh -NoProfile -File scripts/run-evaluation.ps1` runs successfully, **Then** it validates the default fixture, replays, and writes `artifacts/evaluation/metrics-summary.json` with the exact ordered field set from Q6.
2. **Given** caller-selected `-FixturePath`, `-OutputPath`, and `-Seed`, **When** the command succeeds, **Then** those values govern the run and the summary reflects `fixtureId`, `seed`, and metrics for that run.
3. **Given** missing/invalid fixture, ranking failure, ordinary write failure, native child nonzero exit, or terminating/nonterminating script error, **When** the command completes, **Then** exit code is nonzero and no truncated or partial-success summary is presented as a successful publication.
4. **Given** an empty event array fixture, **When** evaluation succeeds, **Then** `observationCount`, `hitRateAt10`, and `mrr` are zero per Q5.
5. **Given** the nonempty Phase 3 real-ranker baseline, seed, and paths, **When** the successful command runs twice on the identical fixture, **Then** published summary bytes are identical.
6. **Given** equivalent original and presentation-permuted fixtures of that same complete baseline, **When** the successful command publishes each, **Then** published summary bytes are identical.

### Edge Cases

- Playback rows are already-qualified target observations; no new playback-completion threshold (Q4).
- Every row counts once: repeated plays, cold start, short/empty recommendation lists, and targets excluded by real ranking policy; no favorable filtering or deduplication (Q5).
- MRR is truncated at 10 via `cutoff=10`; absence in the top 10 contributes zero reciprocal rank (Q5–Q6).
- For N=0, `observationCount` and both metrics are zero (Q5).
- Empty-input coverage alone cannot meet acceptance; the baseline fixture MUST be nonempty with independently worked expected HR@10/MRR (Q10).
- No live DB/export integration; no precomputed future-derived profiles (Q4).
- No HTTP API, DB schema, UI, LocalPlay, secrets, or `Process.Start` change (Q6, Q9).
- No new NuGet dependency, production port/layer, ADR, glossary rewrite, or ASSUMPTIONS.md entry (Q3, Q9–Q10).
- Exact predecessor DTO mapping to DEV-416 remains deferred under the Q2 Phase B STOP; no invented callable seam (Q2, Q7).
- PRODUCT Epic 9 sequencing is not waived by this draft (PRODUCT.md §2; recon-DEV-356:33).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST add dedicated project `tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj` as a net10.0 xUnit v3 executable using only existing CPM packages as needed and necessary project references only—no new NuGet dependencies and no blanket copy of all UnitTests references. Register it in `LamuFlix.sln` (Q3; charter §2.3 items 1, 2, and 6).
- **FR-002**: The system MUST provide `scripts/run-evaluation.ps1` with `-FixturePath`, `-OutputPath`, `-Seed`, and `-Help`. Defaults resolve from repository root regardless of caller cwd: `tests/LamuFlix.Evaluation/Fixtures/baseline.json`, `artifacts/evaluation/metrics-summary.json`, seed `356`. Publication MUST use the evidence-backed intentional evaluation invocation mode recorded in Phase B TB002 reconciliation; incidental ordinary test execution MUST NOT be the publication path. Exit 0 only after complete validation, replay, and summary publication; nonzero without presenting truncated/partial success on invalid/missing fixture, ranking failure, ordinary write failure, native child nonzero exit, or terminating/nonterminating script error (Q3, Q7; Sentry F3/F4; Compass F5).
- **FR-003**: Fixtures MUST declare `fixtureId`, immutable catalog/ranking inputs, and event rows with unique stable event ID, user/movie identities, and UTC timestamp. Validation MUST reject duplicate event IDs, malformed/nonfinite values, and unresolved movie/catalog references, and MUST preserve every valid row without deduplication. Empty event arrays are valid. Exact DEV-416 predecessor DTO mapping MUST follow Phase B recon/reconciliation under Q2—not an invented Phase A seam (Q7).
- **FR-004**: Replay MUST treat each playback row as one already-qualified target observation (no new completion threshold). Traversal order is timestamp then stable event ID. All history-dependent inputs MUST come only from rows with timestamp strictly less than target time across all applicable users. Equal-time rows MUST NOT teach one another. Rank and score MUST complete before the target becomes visible to later observations. Recompute real DEV-416 final ranking per observation over an immutable declared fixture catalog with fixed seeded `Random` and `TimeProvider` inputs. Never use target/future events, feedback, or future-derived profiles. No live DB/export integration (Q4).
- **FR-005**: For N target rows, HR@10 MUST equal hitCount/N; MRR MUST equal sum(1/oneBasedRank for targets present in the final top 10)/N, with absent targets contributing zero. MRR is truncated at 10. All rows count once, including repeated plays, cold start, short/empty lists, and policy-excluded targets—no deduplication or favorable filtering. Duplicate movie IDs in final ranking output MUST fail validation. For N=0, observationCount and both metrics are zero (Q5).
- **FR-006**: The metrics summary MUST be deterministic UTF-8 JSON with these exact fields in this order: `schemaVersion` (1), `fixtureId`, `seed`, `cutoff` (10), `observationCount`, `hitCount`, `reciprocalRankSum`, `hitRateAt10`, `mrr`. Numeric results MUST be finite, invariant-culture JSON numbers and unrounded. Do not emit wall-clock timestamps, machine paths, or timing metadata. Default output is an untracked/ignored `artifacts/evaluation` path; caller may select output path; never commit generated summaries. Before first generated output, ignore coverage MUST be verified and the narrowly authorized `.gitignore` entry added only if needed (Q6, Q9; Sentry F9). No HTTP API or DB schema change (Q6).
- **FR-007**: Implementation MUST use the real DEV-416 final ranking. Before any implementation, Phase B drift recon MUST verify the merged DEV-416 implementation AND exact invocation/input/output contract, AND the plan MUST be reconciled with that evidence—including concrete deterministic serializer options, UTC parsing, ordinal stable-ID ordering, seed/`TimeProvider` mapping, and an evidence-backed intentional evaluation invocation mode. No replacement ranker, invented callable seam, temporary fake ranking path, or stub compile seam (Q2; Sentry F1/F2 clarifications; Compass F5).
- **FR-008**: Proof MUST follow the Q8 strategy and order: Phase B DEV-416 recon/reconciliation STOP first; minimal project scaffold then fixture validation and pure metric tests; strict as-of replay/history tests with in-memory metrics/model equality under full-fixture permutation (no premature publication requirement); real DEV-416 fixed-fixture integration with independently worked expected HR@10/MRR (integration supplies the baseline; published-byte proof is not required at this step); command and summary-publication tests that prove byte-identical repeated successful command runs on that same nonempty baseline AND published-byte equality for equivalent original/permuted fixtures after serializer/publication exist; final applicable gates. Metric cases MUST cover ranks 1/10/11 or absent, repeated plays, short/empty rankings, zero observations, and invalid duplicates. Leakage tests MUST compare every earlier observation ranking/score under appended/shuffled future events, target exclusion, equal-time events, cross-user future history, and future-derived profiles, plus full-fixture permutation invariance at the in-memory metrics/model layer. Aggregate-only equality is insufficient. FsCheck `[0,1]` bounds and ordering/future-prefix invariants MUST live in the existing UnitTests property-test location; no property-test opt-out. Use Testcontainers if real persistence/translation is exercised; never mock driver behavior (Q8; Compass F1/F2).
- **FR-009**: The baseline fixture MUST be nonempty and carry independently worked expected HR@10/MRR; empty-input coverage alone MUST NOT be treated as meeting acceptance (Q10).
- **FR-010**: Scope is offline chronological replay through real DEV-416 final ranking; HR@10 and MRR truncated at 10; validated deterministic fixtures, command, summary contract, and Q8 proofs. The system MUST NOT rewrite production ranking; MUST NOT add a new production port/layer/package; MUST NOT change schema/API/UI/LocalPlay/secrets; MUST NOT implement live export or DEV-417 comparison. Anything else is a follow-up issue, not a finding in this round (Q9).
- **FR-011**: Authorized tracked edits are limited to `specs/DEV-356/**`, `tests/LamuFlix.Evaluation/**`, `scripts/run-evaluation.ps1`, `LamuFlix.sln`, existing UnitTests recommendation property tests and `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj` only for required evaluation reference, and a narrow `.gitignore` entry only if needed to ignore `artifacts/evaluation` output after ignore-coverage verification (Q9; charter §2.3 item 6; Sentry F9).
- **FR-012**: Later Build MUST earn applicable configured harness gates (Roslyn, complexity, InspectCode, mutation where eligible, property tests, vulnerable packages, format, `dotnet test`, and web gates where applicable) without lowering thresholds. No property-test opt-out. Scope-empty exit 2 remains SKIPPED (scope-empty), never PASS; harness opt-outs and mutation N/A retain script verdicts, never PASS. Failed or unrunnable applicable gates block. Test-only changes do not establish production mutation eligibility (Q9).
- **FR-013**: Review closing bar: Critical, High, and Medium in-scope findings block closure; Low findings are recorded and nonblocking. Round cap: two review rounds maximum, at most two remediation fix commits per round. Unresolved above-bar findings after round two go to user review on the PR without claiming closure (Q1, Q9).
- **FR-014**: Any fuller ticket text MUST be reconciled before build without contradicting ticket Scope & Technical Design or acceptance criteria. If a later fact exposes a ticket contradiction or necessary constitution departure, apply charter §2.3 rather than silently changing scope (Q10).

### Key Entities

- **Evaluation fixture**: Declared `fixtureId`, immutable catalog/ranking inputs, and playback event rows used for offline replay.
- **Playback observation**: One already-qualified historical playback row (stable event ID, user/movie identities, UTC timestamp) scored as a target.
- **As-of history**: Event rows with timestamp strictly less than the target timestamp used to build ranking inputs.
- **Final ranking (DEV-416)**: Real predecessor final top-ranked movie list recomputed per observation; exact invocation/input/output contract verified in Phase B.
- **Metrics summary**: Versioned deterministic JSON artifact for DEV-417 with ordered fields from FR-006.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Nonempty baseline fixture replay through real DEV-416 yields independently derived expected HR@10 and MRR; repeated evaluation of the same inputs yields identical metric numerics. Byte-identical published command-output proof on that baseline is Phase 4 / US3 evidence (Patron-ratified Q8 interpretation).
- **SC-002**: Leakage proofs show unchanged earlier observation rankings/scores under future append/shuffle and related Q8 cases; full-fixture permutation preserves in-memory metrics/model equality. Published-byte equality for equivalent original/permuted fixtures is Phase 4 / US3 evidence after serializer/publication exist.
- **SC-003**: `scripts/run-evaluation.ps1` is the single intentional publication path using the Phase B–recorded evaluation invocation mode; exit 0 only after complete summary write; failures (including ordinary write failure, native child nonzero exit, and terminating/nonterminating script errors) are nonzero without truncated/partial success.
- **SC-004**: Published JSON matches FR-006 field order and numeric rules; default output stays untracked under `artifacts/evaluation` after ignore-coverage verification and conditional narrow ignore entry if needed.
- **SC-005**: Applicable configured gates run without lowered thresholds; skipped/opt-out/N/A/could-not-run verdicts are never recorded as PASS.
- **SC-006**: Q2 Phase B STOP conditions are evidenced before implementation; no invented ranker, callable seam, or temporary fake ranking path is introduced at this base.

## Assumptions

- No `[assumed]` taste items were recorded for this ticket; there is no `ASSUMPTIONS.md`, and none is created here (brief Q10).
- No owner structural checkbox is open. Anything not decided in `brief.md` / `CONCLUSIONS.md` is a `needs decision:` item in `DRAFTING_RECEIPT.md`, not an assumption invented here.
- Facts for absent DEV-416 contract, absent evaluation project, and intake limitations come from `recon-DEV-356`; proposed new project/script/fixture paths are Patron-approved design decisions in the brief, not claims those seams already exist at HEAD `7c7b4079`.
- Exact DEV-416 predecessor DTO and invocation mapping is intentionally deferred to Phase B recon/reconciliation (Q2, Q7), not filled by drafting assumption.
