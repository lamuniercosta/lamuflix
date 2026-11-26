---
description: "Task list for DEV-571 outside-library IMDb quality floors"
---

# Tasks: Outside-library IMDb quality floors

**Input**: Design documents from `specs/DEV-571/` — `spec.md`, `plan.md`, `research.md`, `data-model.md`, grill-closed `brief.md` (Q1–Q5 + Q3/Q4 update + Phase 2 plan-challenge decisions), `CONCLUSIONS.md`; Maestri notes `DEV-571`, `recon-DEV-571`; `specs/PRODUCT.md`.

**Status**: gate1: provisional — **blocked: structural** (owner Principle VII checkbox on Spec Kit PR). Quill does not invent PLAN FROZEN. The draft Spec Kit PR may be prepared/opened with the unchecked owner checkbox. Gate 1 / T000 stays closed until **affirmative** owner approval, the **verified merged** narrow Principle VII constitution amendment (`.specify/memory/constitution.md` via `/speckit-constitution` with version and sync-impact on the same Spec Kit PR), and verified owner Spec Kit PR merge → Keel receipt that **references the amendment commit** (a CONCLUSIONS line alone cannot substitute). An owner rejection stops this draft for re-planning/reanalysis and never unlocks T001+. Ordinary Build scope excludes constitution edits.

**Tests**: Required (`brief.md` Test and gate expectations; FR-014; SC-001–SC-008). Prefer failing tests before implementation inside each story phase except where noted (T002 may already be green). Use existing xUnit/property, provider fixtures, architecture/format verification, and real relational/Testcontainers layout. Adjacent new tests for this feature are explicitly allowed in the existing test projects. Never mock the data-access driver where query translation or serialization is under test. **T019** is the single normative gate-label list; other tasks reference it.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. Same-file edits are never parallel. Shared gate tasks are never parallel. Do not start a later ID before its named predecessor completes.

**Conventions**: Edit only the authorized boundary in `plan.md` / brief Files and components (as updated by Q3/Q4 reuse of landed quota seams and Phase 2 H2 candidate-source seam); STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` only when a required landed-main fact for Q3 seams remains unidentified after pickup; no new NuGet/npm dependency; no new quota counter/port or layer; no public route/DTO/status or frontend/generated contract change; no LocalPlay/secrets/`Process.Start`; no constitution edit in ordinary Build (governance lane only after affirmative owner answer); no explanatory comments beyond AGENTS exemptions; never lower harness thresholds. Otherwise-unassigned implementation tasks are owned by the generic Build lane.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR / governance provenance**: After clean analyze → plan challenge → PLAN FROZEN, Bernstein coordinates ordinary Spec Kit PR preparation/opening (Rigger) carrying the unchecked owner Principle VII checkbox from `spec.md`. Only after an **affirmative** owner answer does Bernstein coordinate the **governance lane**: Rigger prepares the authorized narrow Principle VII amendment for owner merge through `/speckit-constitution` (actual change in `.specify/memory/constitution.md`, version bump, sync-impact record) on the **same** Spec Kit PR. No constitution change now. Only the user merges. Keel T000 records the verified Spec Kit PR merge **and** the verified merged amendment commit. An owner rejection is a stop for re-planning/reanalysis. No Build work before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   T000                Spec PR + affirmative owner VII + merged constitution amendment + Gate 1 — hard-blocks T001+
Phase 1   T001                Pickup recon (read-only) — blocks T002
Phase 2   T002 → T005         Foundation: DEV-561 + evidence model → store/migration → external_ids (+fixtures) → evidence-write → guard/default/disabled
                              Order inside Phase 2: T002 ∥ T003; then T004; then T014 (fixtures before impl); then T009; then T005
Phase 3   T006 → T008         US1 IMDb floors on selection path (after foundation ready; T007 serial)
Phase 4   T010 → T012         US2 collection representative selection + backfill beyond former cut-off
Phase 5   T013 → T016         US3 quota-bounded live lookup wiring (T013 → T015 → T016; T014 already in Phase 2)
Phase 6   T017 → T020         US4 contract/row parity + orphan delete BEFORE gates + Q4 receipt + review
```

Ordering follows `brief.md` Task order 1–4 and Phase 2 M4: evidence model, then cache/guard/migration with external-ID capability and evidence-write in the foundation, then selection over a ready source. Early floor/collection fixture checkpoints exercise fixture/cached evidence only — they do not imply production live wiring.

---

## Phase 0: Spec PR, governance amendment, and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the Spec Kit PR with the unchecked owner Principle VII checkbox; after affirmative owner answer, run the governance lane to land the narrow constitution amendment on that PR; confirm verified owner merge and amendment commit before Build.
**Checkpoint**: Draft Spec Kit PR may be prepared/opened with the unchecked structural checkbox. Affirmative owner answer. Governance lane completes: `/speckit-constitution` writes the narrow VII `OmdbQuotaOptions` daily-allowance exception, version bump, and sync-impact on the same Spec Kit PR; owner merges. Keel records verified merge **and** the amendment commit SHA in `CONCLUSIONS.md` (receipt references the commit; cannot substitute for it). Owner rejection stops for re-planning — do not unlock T001+. Ordinary Build never edits the constitution.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-571 Spec Kit PR for ordinary owner Gate 1, carrying the unchecked **blocked: structural** Principle VII owner checkbox from `spec.md` Owner question — Gate 1 (exact Patron wording). The draft Spec Kit PR may be prepared/opened with the unchecked owner checkbox. Only the user merges after answering. **Only after an affirmative owner answer**, Bernstein coordinates the **governance lane**: Rigger prepares the authorized narrow Principle VII amendment for owner merge via `/speckit-constitution` — the actual change in `.specify/memory/constitution.md` with version bump and sync-impact record — on the **same** Spec Kit PR. No constitution change before that affirmative answer. After affirmative answer, amendment merge, and Spec Kit PR merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt **and** the verified merged constitution amendment commit SHA for DEV-571 in `specs/DEV-571/CONCLUSIONS.md`. A CONCLUSIONS receipt that lacks the amendment commit cannot unlock Build. T001–T020 MUST NOT proceed without affirmative owner approval, the verified merged constitution amendment, and verified Spec Kit PR merge (`brief.md` Phase 2 H1; Q3–Q4 update; Q5). An owner rejection stops for re-planning/reanalysis and never unlocks this draft. Ordinary Build scope excludes constitution edits.

---

## Phase 1: Pickup recon (read-only; blocks T002)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; inventory OMDb/TMDb/ranking/persistence/quota/candidate-source seams named in recon and plan.
**Checkpoint**: Fresh recon recorded; any authorized drift amendment is a **separate subsequent step**. Strictly read-only.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying: `OmdbMetadataProvider.cs` / `OmdbMetadataProvider.Mapping.cs` outbound attempt sites and null-rating / not-found mapping (recon lines 5–8); landed `MetadataQuotaHandler` / `IOmdbRequestGate` / `EfMetadataRequestGate` / `omdb_daily_usage` / `OmdbQuotaOptions` (recon lines 23–28) including 1000 default and startup validation; `TmdbMetadataProvider.cs` / `TmdbMetadataProvider.Mapping.cs` collection mapping and absence of `external_ids`; Core port `IRecommendationCandidateSource` (`src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs`) including outside-library method `GetOutsideLibraryRecommendationsAsync` → `OutsideLibraryRecommendationSnapshot`, scoped registration `IRecommendationCandidateSource` → `EfRecommendationCandidateSource` at `PersistenceServiceCollectionExtensions.cs:76`, implementation `EfRecommendationCandidateSource.cs`, and Core handler `GetOutsideLibraryRecommendationsQueryHandler` (recon lines 31–37); existing handler tests at `GetOutsideLibraryRecommendationsQueryHandlerTests.cs`; `OutsideLibraryRanking.cs` and discovery DI path; Infrastructure persistence/DbContext/migration patterns for the **candidate-evidence** table only; existing `TimeProvider` injection on the **gate and handler path** for quota UTC date and evidence expiry — **confirm this TimeProvider seam explicitly**, or report `needs recon` if evidence is missing; test neighbors (`OmdbMetadataProviderUnitTests.cs`, `OutsideLibraryRankingPropertyTests.cs`, `OmdbQuotaOptionsTests.cs`, `OptionsStartupValidationTests.cs`, `EfMetadataRequestGateTests.cs`, architecture/format verification layout). Adjacent new test files in those existing test projects remain in scope for later tasks. **Keel** reanalyzes brief/spec/plan/tasks against landed main in a **strictly read-only** pass. **Gauge** records pre-edit gate baseline. **STOP** `needs recon` only when a required Q3 seam remains unidentified after pickup. **STOP** for cited Patron ruling rather than expanding file/protected surface. **Strictly read-only**.

---

## Phase 2: Foundational — regressions, evidence model, store, external_ids, evidence-write, guard (blocks user stories)

**Purpose**: DEV-561 regression fixtures; provider evidence outcomes **before** migration; durable candidate-evidence store + migration; on-demand TMDb `external_ids` (discovery-only) and evidence-write path **with the store**; then remove the 1,000 default in the **same** post-Gate-1 step as disabled normalization and shared-gate deny — before selection consumes the ready source (`brief.md` Task order 1–2; Phase 2 M4; Q3–Q4 update).
**Checkpoint**: Evidence model defined; migration applies; cache read/write and both expiry windows proven against real relational persistence; `external_ids` discovery-only with enrichment output and public contract preserved; evidence-write path ready; reservation charges every outbound OMDb attempt via landed gate; no-default and configured positive / disabled paths tested under the merged amendment; enrichment still callable through the guarded adapter in tests; no discovery selection wiring required yet. Early fixtures do not exercise production live wiring.

### Tests

- [X] T002 **Build lane**. After T001: Add/extend DEV-561 regression coverage in `tests/LamuFlix.UnitTests/Features/Recommendations/OutsideLibraryRankingPropertyTests.cs` (and adjacent Core recommendation tests in the existing layout, including existing `GetOutsideLibraryRecommendationsQueryHandlerTests.cs` as needed) for liked weight 2 / watched weight 1, score-descending then TMDb-ID-ascending order, two-seed/exclusion invariants, and cap 20 (`brief.md` Task order 1; FR-012; SC-006). **T002 pins the current baseline and MAY already be green** — do not force a red state when the baseline already holds.
- [X] T003 [P] **Build lane**. After T001: Add failing provider/unit coverage in `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs` (and adjacent new fixture files in the existing adapters test layout as needed) distinguishing complete pass, known floor failure (including known floor failure with unknown/absent counterpart — stored 30 days), confirmed absence (explicit provider-confirmed per recon lines 5–8), and unresolved outcomes evaluated only after known floor failures and confirmed absence; exact 6.5 / 1,000 boundaries and immediately below; **mixed-evidence** fixtures (known failing rating with unknown/absent votes; known failing votes with unknown/absent rating — known failure decisive and durable); valid at/above-floor rating with unknown votes defers with no durable row; **adversarial rating/vote fixtures** for blank, `N/A`, separator/spacing variants, overflow, negatives, and zero, using **strict documented numeric grammar and semantic bounds** and treating ambiguous values as unknown (no numeric guess); preserve decisive known-failure and explicit provider-confirmed-absence precedence and both TTLs (30d complete / 7d confirmed absence); no durable write for unresolved after that evaluation order (`brief.md` Task order 1; Phase 2 F8; FR-001; FR-002; FR-003; SC-001–SC-002). Evidence model establishment precedes migration work in T004.

### Implementation

- [X] T004 **Build lane**. After T002 and T003: Implement **only** the ticket-owned candidate-evidence entity/mapping plus EF migration in the existing Infrastructure persistence area; key evidence by TMDb ID with fields/expiry from [data-model.md](data-model.md); finish cache store read/write and expiry handling **before** any discovery selection wiring; **reuse** landed `OmdbQuotaOptions` / `IOmdbRequestGate` / `EfMetadataRequestGate` / `omdb_daily_usage` / `MetadataQuotaHandler` — **no new quota counter table or port**; register DI as needed for the evidence store; use injectable time seam for UTC day and expiry; preserve landed enrichment quota settlement (`brief.md` Q3–Q4 update; FR-007; FR-010). **Do not** remove the 1,000 default or implement disabled normalization in this task — those belong to T005 in the same post-Gate-1 implementation step as each other.
- [X] T014 [US3] **Build lane**. After T004: **First** add failing provider fixtures for on-demand TMDb `external_ids` (including multiple failed/empty external-ID outcomes counted against the per-load TMDb ceiling) in the existing adapters test layout; **then** extend `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.cs` and `TmdbMetadataProvider.Mapping.cs` with on-demand `external_ids` for discovery candidates that can occupy a slot under Q2; reuse collection mapping (recon: external_ids absent today). Keep `external_ids` **discovery-only**; preserve enrichment output and the public DTO/contract; add provider regression evidence that enrichment projection is unchanged (`brief.md` Q3; Phase 2 L3; FR-009).
- [X] T009 [US1] **Build lane**. After T014: Persist complete and confirmed-absence outcomes through the ticket-owned evidence store from Infrastructure adapters / `EfRecommendationCandidateSource` orchestration, including known floor failures with unknown/absent counterpart (30-day complete evidence) and resolved IMDb IDs where the existing outcome policy permits a row; apply the unresolved/no-row rule only after known floor failures and confirmed absence are evaluated (exact Patron: "Unresolved, transient-failure, deferred, and unknown-votes evidence gets no durable entry and is retried later under Q1."); apply 30-day / 7-day expiry per ruled outcomes (`brief.md` Q3; Phase 2 M4; FR-007).
- [X] T005 **Build lane**. After T009: In **one** post-Gate-1 implementation step: (a) remove the hard-coded default allowance of 1,000 and (b) under the merged owner Principle VII amendment, bind the **raw** `Omdb:Quota:DailyLimit` key in existing options binding **before** typed conversion into a typed positive enabled allowance or explicit disabled / no-allowance state — covering missing, zero, negative, malformed/overflow, and positive. While disabled, the shared gate **denies outbound OMDb** for both discovery and enrichment; retain existing enrichment quota settlement and all other startup validation (separate invalid-option startup proof for unrelated keys). Inventory **dev, Compose, and test hosts** that rely on the removed default **before** the switch; use Q4 provisioning for environment dependencies; committed `appsettings` / AppHost / Compose edits are outside this ruling. Fix test fixtures a gate proves relied on the default, citing the failing test. No numeric default, fallback, bypass, or credentials. Keep atomic application-wide reservation on the landed `MetadataQuotaHandler` → `IOmdbRequestGate` → `EfMetadataRequestGate` path before every outbound attempt (including failures, timeouts, and each retry); make no call on fresh cache hits once evidence store is consulted; keep the shared reservation atomic and **independent of the per-load live-attempt cap**; preserve existing enrichment mapping behaviour in `OmdbMetadataProvider.Mapping.cs` (`brief.md` Q3–Q4; Phase 2 M1/M3; FR-010–FR-011; recon lines 5–8, 23–28). Add real relational tests in the existing integration test layout for uniqueness, durable reuse across distinct scopes/context instances, both expiry windows, atomic parallel reservations across separate loads/consumers, shared accounting across two consumers with charged attempts attributable to each consumer (no new metric stack or pool split), and UTC rollover (FR-014; SC-004–SC-005). Add targeted tests of the **no-default** path, **configured positive** allowance path, **disabled** path (absent/invalid → no sends, no used quota, cache/confirmed-absence evaluation continues), and **disabled and positive enrichment** coverage (`OmdbQuotaOptionsTests` / `OptionsStartupValidationTests` / gate tests as appropriate). Keep Q4 rollout order.

**Checkpoint**: Foundation ready — evidence model before migration; candidate-evidence cache; discovery-only `external_ids`; evidence-write path; shared guard with default removed and disabled normalization; DEV-561 regressions and outcome distinctions red/green as appropriate before selection wiring.

---

## Phase 3: User Story 1 - Hide low IMDb-quality discovery titles (Priority: P1) 🎯 MVP

**Goal**: Reject any known below-floor IMDb evidence (decisive even when counterpart unknown/absent); preserve confirmed absence; defer unresolved (including valid rating with unknown votes) without durable negatives.
**Independent Test**: Floor boundary, mixed-evidence, numeric parsing, and absence/unresolved tests pass against selection eligibility helpers using fixtures/cached evidence without requiring full quota exhaustion matrix. Early floor checkpoints remain fixture-testable only — they do not exercise production live wiring.
**Order**: T006 → T007 → T008 (after T005; **T007 is serial — no parallel marker**). Evidence-write already landed in Phase 2 T009.

### Tests

- [X] T006 [US1] **Build lane**. After T005: Add failing unit/property tests under `tests/LamuFlix.UnitTests/Features/Recommendations/` for eligibility at/below 6.5 and 1,000, mixed-evidence decisive known failures, valid at/above-floor rating with unknown votes deferral, confirmed absence preservation, unresolved deferral with original sort-position restore, and no durable negative for unresolved (`brief.md` Q1; FR-001; FR-002; FR-003; SC-001–SC-002). Adjacent new test files in this existing test project are allowed.

### Implementation

- [X] T007 [US1] **Build lane**. After T006: Implement pure eligibility evaluation from parsed evidence (recompute floors from stored rating/votes; any known failure decisive; valid rating with unknown votes unresolved) in `src/LamuFlix.Core/Features/Recommendations/` beside `OutsideLibraryRanking.cs` (`brief.md` Q1; FR-001–FR-003). Keep Core Infrastructure-free.
- [X] T008 [US1] **Build lane**. After T007: Wire eligibility into the discovery selection path so Core pure ordering/eligibility rules are invoked from the existing Infrastructure `EfRecommendationCandidateSource` outside-library path, which carries the admitted ordered result via the existing typed `OutsideLibraryRecommendationSnapshot` seam to `GetOutsideLibraryRecommendationsQueryHandler`; keep other `IRecommendationCandidateSource` methods/consumers intact; add no port or layer (`brief.md` Phase 2 H2; Frozen scope; FR-012; recon 31–37). Production live wiring remains dependent on the Phase 2 foundation.

**Checkpoint**: US1 independently testable with fixture evidence; DEV-561 order of survivors preserved.

---

## Phase 4: User Story 2 - One title per TMDb collection (Priority: P1)

**Goal**: First admitted title represents its known collection; skip later known members before lookup; retain candidates beyond former top-20 cut-off for backfill; recheck membership before admission when collection becomes known mid-resolution; uncollected titles stay independent; fresh cached membership counts as known until expiry unless fresher membership is already available without a remote lookup.
**Independent Test**: Selection tests prove representative behaviour, no lookup for known duplicates after admit, backfill from a lower-ranked candidate beyond the former cut-off, membership recheck path, and changed-membership fixtures with no refresh solely to check a fresh cache hit.
**Order**: T010 → T012 (after T008).

### Tests

- [X] T010 [US2] **Build lane**. After T008: Add failing selection tests under `tests/LamuFlix.UnitTests/Features/Recommendations/` for first-admitted collection representative, skip of later known same-collection candidates before lookup (zero outbound to both providers), independent uncollected titles, cap 20 **admitted representatives**, **backfill from a lower-ranked candidate beyond the former cut-off**, **membership recheck before admission** when collection becomes known during evidence resolution, and **selection fixtures for changed membership** proving fresh cached collection membership is used until evidence expiry unless fresher membership is already available without a remote lookup — **no refresh solely to check a fresh cache hit** (`brief.md` Q2; Phase 2 F9; FR-004; SC-003). Early collection checkpoints remain fixture-testable only.

### Implementation

- [X] T011 [US2] **Build lane**. After T010: Implement ordered collection-representative admission as pure Core rules in `src/LamuFlix.Core/Features/Recommendations/` walking DEV-561 order with candidate pool retained beyond former cut-off for backfill (`brief.md` Q2; Wanted 3; FR-004).
- [X] T012 [US2] **Build lane**. After T011: Ensure positive collection IDs flow from reused TMDb collection mapping / evidence into selection skip decisions without speculative lookups; recheck represented-collection membership before admission when collection becomes known mid-resolution; honour fresh cached membership until expiry unless fresher local membership is already known (`src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.Mapping.cs` reuse; evidence store; FR-009).

**Checkpoint**: US1+US2 selection behaviour holds on cached/fixture evidence.

---

## Phase 5: User Story 3 - Quota-bounded evidence with durable reuse (Priority: P1)

**Goal**: Serial ordered per-load resolution; live lookups only for open-slot candidates within the fixed per-load ceiling and shared bounds at the individual-send boundary; IMDb ID validated before reserve/send; cache hits free; coalesce concurrent misses on an Infrastructure instance shared across participating loads; post-exhaustion/denial cache/confirmed-absence backfill only; safe quota-denial diagnostics.
**Independent Test**: Provider fixtures prove call counts (including zero outbound on cache/duplicate skips; TMDb calls counted even when failed/empty; malformed input ID → zero sends/charges), mid-scan denial with lower cached survivor and no escaping quota-denial exception, exhaustion, charging, coalesce/cancellation, retry at boundary, midnight-spanning ceiling, and enrichment through the guard.
**Order**: T013 → T015 → T016 (after T012). TMDb `external_ids` capability already landed in Phase 2 T014.

### Tests

- [X] T013 [US3] **Build lane**. After T012: Add failing provider fixtures covering: cache hit (zero OMDb **and** TMDb calls); fresh/expired handling; **serial ordered** per-load resolution with **no speculative parallel candidates**; per-load ceiling fixed at load start from open slots and remaining shared capacity; **mid-scan denial** at the actual send boundary that defers the candidate without an outbound send, does not fail the load, switches the rest to cache/confirmed-absence only, admits a lower cached survivor, and raises **no escaping quota-denial exception**; retry still at the send boundary with independent atomic reservation for the **current UTC date**; **midnight-spanning** evidence that crossing midnight does not replenish the current load's ceiling or restart a cache-only load; before each TMDb `external_ids` call require room for a following OMDb send, no preceding denial, and room within the same numeric per-load ceiling for TMDb calls — count TMDb calls even when they fail or yield no usable ID and assert the bound with multiple failed/empty external-ID outcomes; **no additional configured TMDb budget or TTL**; keep resolved IDs in durable evidence where outcome policy permits; **validate IMDb request ID** in the discovery lookup flow **before** constructing/sending the OMDb request or reserving shared quota — fixture proving a **malformed input ID** defers with **zero sends and no usage increment**; a malformed ID echoed by OMDb is an unresolved response and the already attempted request remains charged; one in-process leader for concurrent same-key misses with waiters that send/charge nothing; no speculative or known-duplicate-collection lookups (zero outbound to both providers); chargeable failures/timeouts/retries in the existing adapters test layout (`brief.md` Q2–Q3; Phase 2 F1–F5/F10; FR-005–FR-010; SC-004).

### Implementation

- [X] T015 [US3] **Build lane**. After T013: Wire budget-aware backfill through the existing Infrastructure `EfRecommendationCandidateSource` outside-library path (ordered I/O orchestration, cache, and coalescing) calling Core pure ordering/eligibility/collection rules and carrying the admitted ordered result via `OutsideLibraryRecommendationSnapshot` to the handler, so live OMDb attempts respect the fixed per-load ceiling (min of open slots at load start and remaining allowance) at the individual-send boundary, rejections do not replenish the per-load count, a send-boundary denial defers without failing the load and switches the remainder to cache/confirmed-absence only, and post-exhaustion evaluation uses cache/confirmed absence only with no fresh resolution (`brief.md` Q2; Phase 2 H2; FR-004–FR-006). Carry safe bounded structured quota denial/defer diagnostics on the existing path (counts, dates, TMDb IDs) with a focused assertion excluding keys, credentials, full request URIs, and raw bodies (`brief.md` Phase 2 F6/F11; FR-014 diagnostics half).
- [X] T016 [US3] **Build lane**. After T015: Implement in-process coalesce for the full provider/cache-miss chain by TMDb ID on an **Infrastructure instance shared across participating loads** (avoid a per-request map or `static` mutable state): one TMDb-ID leader covers the whole miss operation; waiters share the outcome and never send or charge under that flight; **no promotion within the flight**; waiter cancellation is independent; transient failure or leader cancellation leaves waiters deferred and permits independent later loads to retry after cleanup. Test transient failure, leader cancellation, independent waiter cancellation, and later retry. Verify ordinary enrichment still succeeds through the guarded OMDb adapter with two consumers drawing from the same pool and charged attempts attributable to each consumer (existing two-consumer proof; no new metric stack or pool split); keep shared reservation independent of the load cap (`brief.md` Q3–Q4; Phase 2 F4; FR-008; FR-010; SC-005/SC-007 test halves).

**Checkpoint**: US3 quotas and durable reuse proven; shorter rows allowed under deferral/exhaustion/denial.

---

## Phase 6: User Story 4 - Preserve contracts, rollout evidence, gates, review (Priority: P2)

**Goal**: Unchanged public discovery contract and observable row/grid behaviour; orphan deletion before gates; Q4 rollout receipt; applicable gates with honest labels per T019; Size M two-round review closure.
**Independent Test**: API contract **and** observable row/grid evidence; Q4 evidence recorded; gate matrix with honest labels including `FAIL` as defined in T019.
**Order**: T017 → T018 → T019 → T020 (after T016).

### Tests / verification

- [ ] T017 [US4] **Build lane**. After T016: Add/extend API **and** observable row/grid regression coverage in the existing test layout proving unchanged discovery route/DTO/statuses/generated contract baseline **and** observable For You row / library-grid behaviour on loading/empty/error (reuse existing web regressions as appropriate within the frozen boundary); cover SMOKE-001–003 acceptance intents including next eligible survivor in unchanged order and cached/confirmed-absent fill with live OMDb disabled under FR-011 (`brief.md` Frozen scope; FR-011; FR-012; FR-015; SC-006). Contract drift verification is recorded under T019.
- [ ] T018 [US4] **Build lane**. After T017: Capture Q4 rollout receipt for applicable environments — candidate-evidence migration applied; positive non-secret daily allowance provisioned and read back at or below provider plan from shared use; revised guard deployed only after that read-back (or deliberate no usable allowance when OMDb is unused); ordinary enrichment through guarded adapter; two consumers share pool with failures charged (charged attempts attributable to each consumer); invalid configuration remediated by provisioning only (`brief.md` Q4 / Q3–Q4 update; FR-013; SC-007). Gate outcome vocabulary follows T019.
- [ ] T019 **Build lane**. After T018: **Normative gate-label list for this ticket.** **First** delete dead code directly orphaned by this change so the checked diff is the delivery diff; **then** run applicable harness gates — `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (refactor threshold when applicable), `./scripts/run-jetbrains-inspectcode.ps1`, property/ordinary/integration tests, architecture tests, format verification, vulnerable-package scan, `./scripts/run-mutation.ps1` under harness applicability, API contract drift checks — recording each outcome separately as `PASS` / `FAIL` / `SKIPPED (scope-empty)` / `SKIP` / `N/A` / `Could not run` (`FAIL` recorded separately from other non-pass labels); never fold non-pass into PASS; never lower thresholds; carry forward named-port restrictions, existing enrichment quota handling, approved vocabulary, and closed-set/cancellation conventions (`brief.md` Q5; FR-015; SC-008). Other tasks that mention gates reference this list rather than restating labels.
- [ ] T020 **Build lane**. After T019: Size M three-axis review pinned to the reviewed revision; **two review rounds**; close all in-scope Critical/High/Medium within those rounds; dispose every Low with evidence; route remaining blockers at the cap to Bernstein (`brief.md` Q5). Gate labels follow T019.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Hard block after PLAN FROZEN — affirmative owner Principle VII approval + verified merged constitution amendment commit + verified Spec Kit PR merge; blocks all Build work; owner rejection stops for re-planning/reanalysis and never unlocks T001+
- **Phase 1 (T001)**: Depends on T000 — read-only; blocks T002+; confirms TimeProvider seam or reports `needs recon`
- **Phase 2 (T002, T003, T004, T014, T009, T005)**: Foundational — evidence model before migration; store before external_ids and evidence-write; external_ids fixtures before T014 impl; evidence-write before guard/default/disabled (T005); blocks US1–US4 selection wiring; T003 may start in parallel with T002 after T001
- **Phase 3 (T006–T008)**: US1 — after T005; T007 serial after T006 (no parallel marker)
- **Phase 4 (T010–T012)**: US2 — after T008
- **Phase 5 (T013, T015, T016)**: US3 — after T012; T014 already completed in Phase 2
- **Phase 6 (T017–T020)**: US4 / polish — after T016; orphan deletion before gates inside T019 (normative gate labels)

### User Story Dependencies

- **US1 (P1)**: After foundational cache/guard/evidence-write — MVP quality filter
- **US2 (P1)**: After US1 eligibility exists — collection representatives + backfill
- **US3 (P1)**: After US2 selection — live budget and coalesce on the wired path (`external_ids` already in foundation)
- **US4 (P2)**: After US1–US3 — parity, Q4 receipt, orphan delete, gates, review

### Parallel Opportunities

- After T001: T002 ∥ T003 only
- After T006: T007 proceeds serially (no parallel start with later US1 tasks)
- No other parallel starts; shared files and gate tasks stay sequential

---

## Implementation Strategy

### MVP First (US1 floors)

1. Complete Phase 0–2 (Gate 1 + affirmative owner VII approval/amendment + foundation including external_ids and evidence-write)
2. Complete Phase 3 US1
3. **STOP and VALIDATE**: Floor boundaries, mixed-evidence, and absence/unresolved behaviour
4. Continue US2 → US3 → US4

### Incremental Delivery

1. Foundation (candidate-evidence migration + discovery-only `external_ids` + evidence-write + reuse of landed guard; 1000 default removed with disabled normalization under merged amendment) → enrichment-safe guarded OMDb
2. US1 floors → low-quality titles hidden from cached/fixture evidence
3. US2 collections → one title per known collection with backfill beyond former cut-off
4. US3 budgets → live lookups stay inside OMDb limits at the send boundary with serial resolution and coalesce
5. US4 → API + row/grid parity, Q4 receipt, orphan delete before gates (labels per T019), review

---

## Notes

- `[Story]` maps to US1–US4 in `spec.md`
- Authorized areas are fixed in `plan.md`; Build names new files inside those areas without expanding §2.3 scope; adjacent new tests in existing test projects are in scope
- Keel owns analyze / PLAN FROZEN / Gate 1 receipt; Quill does not claim them from this draft
- T019 is the one normative gate-label list; reference it elsewhere rather than restating
- Private mirror: keep `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-571\tasks.md` byte-identical with canonical `specs/DEV-571/tasks.md` when present
