# Tasks: OMDb Wrong-Match Validation (Memento, O Brother)

**Input**: `specs/DEV-447/` spec.md, plan.md, frozen brief.md (Q1–Q8), CONCLUSIONS.md, recon-DEV-447, note DEV-447, specs/PRODUCT.md

**Status**: PLAN FROZEN — Keel completed three-axis adjudication and read-only cross-artifact analysis on 2026-11-03 (brief Plan adjudication closure; note adjudication-DEV-447). Ready for Patron provisional Gate 1 and Rigger spec PR; user Gate 1 merge remains pending. No implementation clearance or production/test edits in this Spec workspace.

**Gate 1**: No owner checkbox (Q8). Handoff chain: **Patron provisional Gate 1 → Rigger opens the spec PR → user merges Gate 1 → Keel T000** records the verified merge receipt in `CONCLUSIONS.md`. Phase B pickup and implementation MUST NOT start before that receipt (Q7; Compass F3).

**Tests**: Required (ticket AC1; Q5). Prefer regression failures before behavior edits **where feasible** (Q7): new-behavior cases red; existing ManualImdbId / failure / one-request / InvalidResponse preservation locks stay green (Compass F2). TDD optional; required coverage green before verification. No live OMDb for AC1.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`.

**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no explanatory code comments; do not delete or rewrite existing comments/docstrings unrelated to the change (no unrelated comment cleanup; no blanket comment-rule exemption). Identify ticket-derived synthetic stub provenance in test names/planning prose; no new packages/layers/API/web/schema; do not edit TitleMatcher.cs or MatchThreshold/options; do not stage unrelated harness.yml change with this task. Mutation hygiene: guard literals are `const`, no new static initialisers. Tests use `FakeTimeProvider` / deterministic `TimeProvider` and seeded `Random` when needed — no `DateTime.Now`, `Task.Delay`, or wall-clock waits (Ledger L1/L2/L3).

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` dispatch (task-pipeline Phase 3 step 2). They are **not** canonical `task-pipeline` phases. Canonical pipeline remains Phase 3 Build/refactor/gates, Phase 4 review, Phase 5 delivery (brief Q7; task-pipeline Phase 3 steps 2–6). Seat map: Wisp/Keel/Gauge retain Pickup (local Phase 1); Anvil code phases 2–4 only; Cog local Phase 5; Gauge local Phase 6; local Phases 8–9 map to canonical review/delivery Phase 4/5.

---

## Local phase dependency / execution order

```text
Phase 0  T000 (Keel Gate 1 record) — blocks T001+
Phase 1  T001 (Wisp drift recon; Keel reanalysis; Gauge baseline) — blocks T002+
Phase 2  T002 → T003 → T004   (Anvil; sequential — shared unit/WireMock files)
Phase 3  T005 → T006 → T007   (Anvil)
Phase 4  T008                 (Anvil; only if documented)
Phase 5  T009                 (Cog /refactor once — required stage; edits may be conditional)
Phase 6  T010 → T016          (Gauge — canonical Phase 3 steps 5–6)
Phase 7  T017                 (Live AC2)
Phase 8  T018                 (canonical Phase 4 — Sentry/Ledger/Compass; Keel adjudication; Cog remediation)
Phase 9  T019 → T020          (canonical Phase 5 merge bar)
```

---

## Phase 0: Gate 1 record (blocks Phase 1)

**Purpose**: Confirm user Gate 1 merge before any Build work.
**Checkpoint**: Verified merge receipt in CONCLUSIONS.md; or stop.
**Handoff note**: Patron provisional → **Rigger** opens the spec PR → user merges Gate 1 → **Keel** T000 (brief Q7; Compass F3).

- [X] T000 **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-447 in `specs/DEV-447/CONCLUSIONS.md`. Receipt only — does not create a new ruling. T001–T020 MUST NOT proceed until this receipt exists (Q7; FR-014).

---

## Phase 1: Pickup — fresh recon, drift reanalysis, Gauge baseline (blocks implementation)

**Purpose**: Confirm frozen brief/spec/plan/tasks still match landed main and the approved file/contract set; obtain Gauge baseline before edits.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift stops for a cited decision — no invention. Baseline captured.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main for the approved production/test allowlist and contracts (do not treat Phase A recon baseline as current-state clearance). Verify Mapping Response/SearchHit DTO visibility stays adapter-local with **actual** recon evidence — no guessed `internal` modifier and no public contract-chain change (Ledger L7). When OMDb credentials are available at pickup, record nonsecret live Type/title/year/rating/director/runtime for incident wrong IDs `tt24735934` and `tt24456948` (or equivalent `t=` probes) into recon; if unavailable, record each field as unknown — never fabricate; **never read or print the API key**. Observed contract mismatch **STOP**s for reanalysis without changing AC1/AC2 (plan-challenge clarification 2; Sentry F6). **Keel** runs cross-artifact reanalysis of brief/spec/plan/tasks against landed main. Amend tasks only where authorized by demonstrated drift through the assigned workflow. **STOP** on unresolved drift or missing contract facts for a cited decision — do not invent substitutes. **Gauge** records pre-edit baseline. No production edits in this task (brief Q7; FR-010).

---

## Phase 2: Regression and request-bound proofs (Anvil; Priority: P1 — User Stories 1–3)

**Goal**: Land deterministic proofs for automatic eligibility, bounds, and ticket regressions before production behavior changes. **New-behavior** cases (type/threshold/suspicious rejection, rejected-ID exclusion, detail-revalidation → NotFound, AC1 selections) are red where feasible. **Existing** ManualImdbId exemption, transport/status/malformed-JSON/cancellation/rate-limit/quota preservation, valid-direct one-request, and MissingTitle/`Failed(InvalidResponse)` locks are green regression locks that must stay green — not absolute-red for every named cell (Compass F2; Q7).
**Independent Test**: Targeted unit/WireMock runs show red on new-behavior cases against current unchecked direct acceptance; green locks remain green.
**Order**: T002 → T003 → T004 strictly (shared unit and WireMock files; no `[P]`).

- [X] T002 [US3] Add deterministic ticket-derived synthetic regression stubs and **failing (red)** AC1 tests covering both incident title→IMDb selections in `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderFallbackUnitTests.cs` (and `OmdbMetadataProviderUnitTests.cs` if the direct-path case belongs there) **and** WireMock lookup coverage in `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs`, proving automatic `Memento[2000]` → `tt0209144` (wrong `tt24735934` must not win) and `O Brother, Where Art Thou[2000]` → `tt0190590` (episode `tt24456948` must not win), including punctuation/year use; name provenance as ticket-derived synthetic, not captured incident payloads and not claiming live-incident causality. Both titles remain required in unit **and** WireMock per Q5 (FR-011–FR-012; Q5; SC-001; brief:24,62–65; Ledger L4)
- [X] T003 [US1] After T002: Add coverage split explicitly — **unit** decision matrix in `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderFallbackUnitTests.cs` and `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs` using the **existing unit HTTP-stub pattern** (no second HTTP-stub approach); **WireMock** cells for the same automatic Type/score/Q4/one-request surfaces that need end-to-end HTTP proof land in `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs` (Ledger M2). Cover automatic Type filtering (movie vs episode/series/absent/unknown), score at/below MatchThreshold (mutation boundary at exact threshold), year veto and unknown-year preservation, Q4 suspicious conjunction (each single usable rating/director/runtime vs all three absent/N/A/invalid). **New-behavior** type/threshold/suspicious rejection cases are red where feasible. **Green locks**: ManualImdbId exemption; valid direct hit exactly one request — repair the primary `Detail(...)` helper / equivalent direct fixtures with `Type=movie` plus at least one usable Q4 field while preserving `Requests.Count==1` (Sentry F5); FR-004a both-path classification preservation — require green preservation tests on **both** automatic direct `t=` and automatic fetched-detail `i=` paths that a `Response=True` body with unusable/missing Title retains `Failed(InvalidResponse)` plus `MissingTitle` logging and does **not** invoke TMDb; keep existing direct invalid-response assertions intact (Sentry F4; Compass F1) (FR-001–FR-005, FR-004a, FR-009, FR-011; Q5)
- [X] T004 [US2] [US3] After T003: Add unit/WireMock and composition coverage in `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderFallbackUnitTests.cs`, `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs`, and `tests/LamuFlix.IntegrationTests/MetadataProviderChainCompositionTests.cs` for rejected-direct-ID exclusion, movie-only search candidates, at most one selected-detail fetch, detail revalidation fail → NotFound, no OMDb restart/fan-out (**new-behavior red where feasible**). Keep **two separate chain cases** without collapsing them when repairing Type: (a) valid automatic OMDb Found → zero TMDb requests (primary short-circuit preserved — do not invert); (b) rejected mapped detail → NotFound with TMDb invoked (Sentry F3). **Reuse T002 AC1 tests** for both incident selections in unit and `MetadataProviderLookupTests.cs` — do not add redundant AC1 copies (Ledger L4). **Green locks**: transport/status/malformed-JSON/cancellation/rate-limit/quota outcomes plus API-key non-disclosure regressions remain green (FR-006–FR-009, FR-011–FR-012; Q3, Q5; SC-001, SC-003–SC-004; brief:24,62–65)

---

## Phase 3: Production guard and green AC1 (Anvil; Priority: P1 — User Stories 1–3)

**Goal**: Implement private/local automatic eligibility in the allowlisted production files and turn AC1/matrix proofs green.
**Independent Test**: T002–T004 suites green; TitleMatcherTests still green unchanged.

- [X] T005 [US1] Bind Type on OMDb Response/SearchHit mapping as needed in `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.Mapping.cs` so automatic paths can require explicit Type=movie case-insensitively; keep the mapping DTO adapter-local (visibility as verified at T001 — no public contract change) (FR-003, FR-010; Q2, Q5; Ledger L7)
- [X] T006 [US1] [US2] Implement private/local automatic eligibility in `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs` at **automatic-only entry points** (automatic direct `t=` path and automatic fetched-detail path after selection) — **do not** put unconditional automatic guards inside shared `Classify` / `FoundOutcome` / `FetchByIdAsync` so ManualImdbId sparse-metadata authority remains intact (Sentry F2). Ordering: Classify/Map before eligibility on both automatic direct and fetched-detail paths; unusable title retains `Failed(InvalidResponse)` + `MissingTitle` (no fallback/TMDb); only mapped Found rejected by the guard routes fallback/NotFound (FR-004a; Sentry F4; Compass F1). Recleaned(stored) scoring with existing TitleMatcher/MatchThreshold; Type=movie; non-suspicious under FR-005; rejected mapped direct → existing finite FallbackQueries with rejected-IMDb-ID exclusion; movie-candidate filter + existing best-score/year selection; at most one detail fetch; same guard on fetched detail against Recleaned(stored); rejected mapped detail → NotFound; preserve ManualImdbId bypass and failure/quota/cancellation semantics. Literals are `const`; no new static initialisers. Private helper extraction stays strictly inside `OmdbMetadataProvider.cs` and `OmdbMetadataProvider.Mapping.cs` — no new type/interface/sibling partial/layer; unmet complexity gate needs a new Patron ruling (Ledger M1; plan-challenge clarification 3). Do not edit TitleMatcher.cs, OmdbOptions, TMDb, or ChainedMetadataProvider production code (FR-001–FR-010; Q1–Q5)
- [X] T007 [US1] [US2] [US3] Re-run T002–T004 suites green after T005–T006; run unchanged `tests/LamuFlix.UnitTests/Adapters/TitleMatcherTests.cs` as regression; preserve mapper-edge assertions in the primary four allowlist files by using ManualImdbId exemption **or** eligible automatic fixtures (Type=movie + usable Q4) so identical mapper asserts remain tested rather than flipping to NotFound (Sentry F7; plan-challenge clarification 4); fix only allowlisted production/test files unless a forced fixture case is documented under T008 (FR-011–FR-012; SC-001–SC-004)

---

## Phase 4: Forced fixture compatibility (Anvil; only as needed)

**Goal**: Repair other existing OMDb automatic-success fixtures that omit Type=movie or usable Q4 metadata, only when forced by Q2/Q4/AC/gates; keep valid-primary short-circuit vs rejected-detail fallthrough cases distinct.
**Checkpoint**: Each edit records exact file and AC/gate reason; assertions not weakened.

- [ ] T008 Document and apply forced payload compatibility edits: in the **primary four** allowlist files, mapper-edge cases may use ManualImdbId or eligible automatic fixtures to preserve identical mapper assertions (Sentry F7); when repairing Type in chain fixtures, keep separate (a) valid OMDb Found → zero TMDb requests and (b) rejected mapped detail NotFound → TMDb invoked — do not invert primary short-circuit assertions (Sentry F3). **Outside** the primary four files, edits remain strictly documented payload-only compatibility repairs with zero assertion weakening. Record exact file and AC/gate reason before each edit; skip entirely if no forced cases appear after T007 (Q5; FR-011; plan-challenge clarification 4)

---

## Phase 5: Refactor (Cog — required stage; canonical Phase 3 step 3)

**Goal**: Cog runs `/refactor` once over the completed implementation; meet refactor complexity gate without changing behavior.
**Independent Test**: Prior AC1 suites remain green.
**Note**: Code edits may be conditional (no-op when complexity/InspectCode already green); the `/refactor` stage itself is mandatory. Any extraction stays inside the two production allowlist files (Ledger M1).

- [ ] T009 After implementation green: **Cog** runs `/refactor` once over the completed implementation (task-pipeline Phase 3 step 3) on allowlisted production files. Code edits apply only if cyclomatic complexity / InspectCode requires them; the refactor stage itself is not optional. Preserve behavior; no speculative abstractions; private helpers only inside the two Q5 production files — unmet gate prompts a new Patron ruling rather than a third file (Q7; FR-010; FR-014; Ledger M1)

---

## Phase 6: Configured gates (Gauge — canonical Phase 3 steps 5–6)

**Purpose**: Harness verification against the task diff; thresholds only from harness.yml.

- [ ] T010 **Gauge** confirms frozen scope only: production edits limited to `OmdbMetadataProvider.cs` and `OmdbMetadataProvider.Mapping.cs`; no TitleMatcher/MatchThreshold/TMDb/chain-production/API/OpenAPI/TS/web/schema/dependency/LocalPlay/secrets/Process.Start/recommendation changes; unrelated harness.yml modification preserved and not staged with this task (FR-010; Q5, Q7; SC-006; Ledger L5)
- [ ] T011 **Gauge** runs Roslyn analyzers on explicit changed `.cs` set via `./scripts/run-roslyn-analyzers.ps1` (FR-014; Q7)
- [ ] T012 **Gauge** runs `./scripts/run-cyclomatic-complexity.ps1` then the refactor threshold from harness.yml on changed files (FR-014; brief:96)
- [ ] T013 **Gauge** runs `./scripts/run-jetbrains-inspectcode.ps1` on changed files (never `-All`) (FR-014; Q7)
- [ ] T014 **Gauge** runs `dotnet format --verify-no-changes` clean (authority: brief Q7 and task-pipeline Phase 3 step 5; FR-014; Ledger L5 / Compass F3)
- [ ] T015 **Gauge** runs affected unit/integration suites (including TitleMatcherTests regression) then full `dotnet test` (FR-011, FR-014; SC-001)
- [ ] T016 **Gauge** runs property/vulnerable-packages gates per harness and web applicability; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS; property exit 2 only with recorded task-note opt-out reason; mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree for changed production C# (FR-014; Q5, Q7; SC-007)

---

## Phase 7: Live AC2 (Priority: P2 — User Story 4)

**Goal**: Mandatory running-app remediation proof via existing manual path.
**Independent Test**: Q6 receipt with environment/HEAD/timestamp and before/after evidence; or explicit pending/blocked.

- [ ] T017 [US4] **Gauge** collects runtime AC2 evidence after code gates, with **Conductor** environment coordination: follow test-portal-policy and existing Match Manually / `PUT /api/movies/{id}/imdb` to correct `/api/movies/7` → `tt0209144` and `/api/movies/29` → `tt0190590`; await completed enrichment; verify correct posters, ratings, and directors; retain environment, exact HEAD, timestamp, and before/after evidence. Do not add endpoints, broaden Enriched retry, or use ad-hoc SQL. Do not claim this proves automatic selection (AC1 already does). If app/credentials/incident data unavailable, record AC2 pending/blocked and route missing facts through recon — never silent waiver or fixture-only substitute (FR-013; Q6; SC-005; Ledger L5)

---

## Phase 8: Review (canonical Phase 4 — Sentry / Ledger / Compass; Keel adjudication; Cog remediation)

- [ ] T018 Size M code review with Sentry, Ledger, and Compass against Q8 closing bar (retained Critical/High/Medium block; Low explicit disposition unless cited mandatory rule/gate); **Keel** adjudicates; **Cog** remediates findings. Mandatory gates and AC1/AC2 block independently; max two review rounds and two fix commits per round; unresolved above-bar at cap blocks clearance and is surfaced on the PR — never silently waived or given a third immutable round; missing axis blocks; keep all review rounds visible on the PR (FR-014; Q7–Q8; SC-007)

---

## Phase 9: Delivery / merge bar (canonical Phase 5)

- [ ] T019 **Rigger** performs fresh rebase onto latest default branch; **Gauge** re-runs applicable gates at current HEAD; **Conductor** coordinates merge-bar confirmation that watched hosted checks meet the standing merge bar (Q7; Ledger L5 / Compass F3)
- [ ] T020 **Conductor** hands off awaiting-merge: DEV-447 (#n); only the user merges (Q7; Ledger L5)

---

## Dependencies

- T000 before T001; T001 before any T002–T020.
- Regression proofs must run T002 → T003 → T004 sequentially (shared unit/WireMock files; no parallel `[P]`). New-behavior cases red where feasible; green locks stay green (Compass F2).
- Production green (T005–T006) after T002–T004; T007 after T005–T006.
- T005 before T006 (Type binding available to guard).
- T008 only after T007 reveals forced fixture failures.
- T009 after T007 (and T008 if used): Cog `/refactor` once is required even when no code edits result.
- T010–T016 after implementation + refactor stage evidence.
- T017 after T010–T016 code gates (Q6–Q7).
- T018 after T017 attempt (AC2 pending/blocked still allows review to proceed but clearance stays blocked until AC2 resolved or explicitly dispositioned per Q6 non-waiver).
- T019–T020 after review clearance path.

## Sequencing examples

```text
# Phase 0 / 1 (Patron provisional → Rigger spec PR → user merge → Keel / Wisp / Keel / Gauge)
T000 Keel Gate 1 merge receipt in CONCLUSIONS.md
T001 Wisp fresh recon (DTO visibility + optional nonsecret incident field observation) + Keel drift reanalysis + Gauge baseline; STOP on unresolved drift/mismatch

# Phase 2 proofs (Anvil; sequential) — new-behavior red where feasible; green locks stay green
T002 AC1 Memento / O Brother synthetic regressions in unit files + MetadataProviderLookupTests (red)
T003 unit HTTP-stub matrix + MetadataProviderLookupTests WireMock cells; Detail helper Type+Q4; green Manual/one-request locks; green InvalidResponse+MissingTitle+no-TMDb on both automatic direct and fetched-detail
T004 rejected-ID / detail bounds; separate valid-Found short-circuit vs rejected-detail→TMDb; reuse T002 AC1 (no copies); green failure locks

# Phase 3 production green (Anvil)
T005 OmdbMetadataProvider.Mapping.cs Type bind (adapter-local)
T006 OmdbMetadataProvider.cs automatic-only guard entry points + Classify/Map-before-eligibility + fallback ID exclusion + detail revalidation
T007 green AC1/matrix + TitleMatcherTests regression; preserve mapper edges via ManualImdbId or eligible fixtures

# Phase 4–9
T008 Phase 4 forced fixtures if documented (Anvil; primary mapper/short-circuit rules; outside primary = payload-only)
T009 Phase 5 Cog /refactor once (required stage; edits conditional; two-file extraction only)
T010–T016 Phase 6 Gauge configured gates + mutation (canonical Phase 3 steps 5–6; T014 format per Q7 / Phase 3 step 5)
T017 Phase 7 Gauge live AC2 + Conductor environment coordination under test-portal-policy
T018 Phase 8 review → canonical Phase 4; Keel adjudication; Cog remediation (cap 2)
T019 Phase 9 Rigger rebase + Gauge gates + Conductor merge-bar coordination
T020 Conductor awaiting-merge handoff → user merges
```

## MVP

MVP for Gate 1 is Phase A documents only. Build MVP after Gate 1 is T000–T007 (local Phases 0–3: red proofs + production guard + green AC1). Full ticket delivery additionally requires T009–T017 (local Phases 5–7: required refactor stage + gates + live AC2) and T018–T020 (local Phases 8–9 / canonical Phase 4–5).

## Notes

- `[P]` only where different files and no incomplete deps; same-file edits stay sequential. T002–T004 share unit/WireMock files and are never `[P]`.
- Local `## Phase N:` IDs are for Anvil `/implement Phase N` dispatch (code phases 2–4) and seat routing; they are not task-pipeline phase numbers. Numbered local Phase headings and the canonical disclaimer are retained (Ledger L6 declined — task-pipeline Phase 3 step 2 dispatch depends on `/implement Phase N`).
- Do not edit files outside the brief/recon frozen allowlist without a cited decision amendment (Q5, Q8). Missing facts → `needs recon` / `needs decision`, not invention.
- Surviving mutant = missing test; never lower thresholds (Q5, Q7).
- Quill does not run `/speckit-analyze` (Keel owns read-only analyze / plan challenge). PLAN FROZEN not declared by this repair.
