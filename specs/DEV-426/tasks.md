# Tasks: OMDb Daily Request Quota

**Input**: `specs/DEV-426/` spec.md, plan.md, frozen brief.md (Q1–Q6 + post-grill C1/C2/I1 + PC1/PC2), CONCLUSIONS.md (c625ea29), Proposed ADR-0031, proposed constitution-amendment-v1.7.0.md, PLAN_CHALLENGE.md, recon-DEV-426, note DEV-426, specs/PRODUCT.md
**Status**: Draft under plan-challenge ROUND 1 remediation; Gate 1 not approved (owner checkbox open for ADR-0031 + constitution 1.7.0); Phase A drafting does not implement; PLAN FROZEN not declared.
**Gate 1**: Owner checkbox in `spec.md` must be resolved before any production implementation task (T001–T033) executes. `constitution-amendment-v1.7.0.md` and ADR-0031 Proposed are prepared in this Phase A / spec PR; Constitution 1.7.0 I/VII/VIII text is staged into `.specify/memory/constitution.md` for owner review. After verified owner approval: Keel records checkbox answer + verified spec-PR merge receipt in `CONCLUSIONS.md` (T034); T035 verifies merged constitution with **no edit including dates**; Keel bumps ADR-0031 to Accepted only in the **implementation PR** after that verified approval and before code merge. Owner rejection → `blocked: structural — ticket-named port/types vs constitution`. No present owner approval. (PC2.)
**Tests**: Required (ticket concurrent/restart/rollover; Q5 — all evidence mandatory). TDD optional; tests land with each step; prerequisite fixture adaptations travel with the first step that needs them on **enumerated recon-backed disjoint files** (I1; PC2 Ledger M3); required coverage green before verification.
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Stable task IDs retained; phase/dependency order is authoritative over numeric ID order (PLAN_CHALLENGE Ledger L5).
**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no new explanatory comments; `TimeProvider` / `FixedTimeProvider` only; no `DateTime.Now`; no live OMDb; no mocked EF/driver SQL translation; no hand-edited migration/snapshot; AutoFixture/Faker fresh data as applicable; Theory/MemberData for T004/T023/T024 matrices (explicit domain boundary inputs allowed); T013 uses async coordinated start + `Task.WhenAll` — no `Task.Delay` staggering or blocking barrier waits (constitution IX; PLAN_CHALLENGE Ledger M4, Sentry L3).

---

## Phase dependency / execution order (stable IDs)

```text
Phase 0  T034 → T035
Phase 1  T001 ∥ T002 → T003 → T004; T001 → T005
Phase 2  T006 ∥ T007 → T008 → T009 → T010 → T011 → T012 → T021 → T013 → T014
Phase 3  T015 → T016 → T017; T015 → T018 → T019; then T020
Phase 4  T022 → T023 → T024
Phase 5  T025 → T026…T033
```

T021 is Phase 2 (after T010–T012) and does not wait for Phase 3. T013–T014 follow T021 when those fixtures are required for PostgreSQL suites. Numeric IDs need not match execution order.

---

## Phase 0: Gate 1 — ADR-0031 + constitution 1.7.0 (blocks all implementation)

**Purpose**: Record owner Gate 1 answer and **verify** the already-staged C1/C2 amendment path before any noncompliant Core port/options land (Principles I / VII / VIII; Governance; PC2). Preparation of `constitution-amendment-v1.7.0.md` and ADR-0031 Proposed is complete in this Phase A / spec PR; Constitution 1.7.0 is staged into `.specify/memory/constitution.md` for owner review — Phase 0/Phase B is verify-only, not an apply, re-staging, or date-edit task.
**Checkpoint**: Owner approved; Keel recorded checkbox + merge receipt in `CONCLUSIONS.md`; merged constitution verified Version 1.7.0 with staged I/VII/VIII (no constitution edit); ADR-0031 Accepted deferred to implementation PR by Keel; or stop blocked structural.

- [X] T034 **Keel** records the owner Gate 1 answer for the ADR-0031 + constitution amendment 1.7.0 checkbox from `spec.md` (verbatim brief wording) **plus the verified spec-PR merge receipt** in `specs/DEV-426/CONCLUSIONS.md`; stop if unchecked or rejected (`blocked: structural — ticket-named port/types vs constitution`). No approval may be inferred from planning artifacts. T035 verify and T001–T033 MUST NOT be marked done until this recorded answer and merge receipt exist (C1, C2; FR-010; PC2; PLAN_CHALLENGE Compass L2). Does not gate staging of the amendment (already staged on this branch).
- [X] T035 **Verify only** (Keel; do not apply, re-stage, or edit `.specify/memory/constitution.md` **including dates**): after T034 Gate 1 evidence, confirm the merged approved `.specify/memory/constitution.md` is Version **1.7.0** with the Gate-1-approved **staged** Principle I port list including `IOmdbRequestGate`, Principle VII options list including `OmdbQuotaOptions`, Principle VIII vendor exceptions including `IOmdbRequestGate` and `OmdbQuotaOptions`, and **that staged file’s** Sync Impact Report — compare against the actual staged constitution artifact, **not** literal equality to the illustrative Sync Impact Report summary in `constitution-amendment-v1.7.0.md`; record staged-vs-merge dates in `CONCLUSIONS.md` — a **date-only** difference is recorded, not fixed; any **substantive** mismatch (version, Principle I/VII/VIII text, Sync Impact Report vs staged constitution) or a baseline other than the staged 1.7.0 artifact → stop `blocked: structural — ticket-named port/types vs constitution`; review `.specify/templates/*.md` and record no-edit or follow-up. **ADR-0031 status bump to Accepted is not performed here**: CONCLUSIONS PC1/PC2 governs ADR delivery — Keel performs that status-only change in the **implementation PR** after verified owner approval and before code merge; no write to `main` (C1, C2; Governance; FR-010; PC2 superseding I2; PLAN_CHALLENGE Compass M3, Ledger M2).

---

## Phase 1: Foundational — Core port and options (blocks persistence and seam)

**Purpose**: Ticket-named Core surface and startup validation before any EF or HTTP wiring.
**Checkpoint**: US4 options boundaries green; architecture whitelist accepts `IOmdbRequestGate`.
**I1**: Options/startup fixture adaptations required by binding/validation travel with this phase on T004 files only — do not defer them to Phase 4.

- [X] T001 [P] [US4] Create `src/LamuFlix.Core/Ports/IOmdbRequestGate.cs` with `Task<bool> TryAcquireAsync(CancellationToken cancellationToken)` (FR-001; Q2; ADR-0031; constitution 1.7.0 Principle I)
- [X] T002 [P] [US4] Create `src/LamuFlix.Core/Options/OmdbQuotaOptions.cs` as `public sealed record` with `public const string SectionName = "Omdb:Quota"` and `DailyLimit` int default 1000 annotated `[Range(1, int.MaxValue)]` (FR-002; Q4; constitution 1.7.0 Principles VII/VIII; PLAN_CHALLENGE Ledger L3; recon:23 sealed-record/SectionName precedent)
- [X] T003 [US4] Edit `src/LamuFlix.ServiceDefaults/Extensions.cs` to `BindAndValidate<OmdbQuotaOptions>` in `AddLamuFlixOptions` like existing options (data annotations + `ValidateOnStart`); startup validation rejects non-positive `DailyLimit`; consume as startup `IOptions` snapshot only — no `IOptionsMonitor` (FR-002; Q4, Q6; PLAN_CHALLENGE Ledger L3)
- [X] T004 [US4] Add Theory/MemberData unit/startup tests covering default 1000 and 1 accepted, 0 and negative rejected (explicit domain boundary inputs; AutoFixture/Faker fresh data as applicable) in **exact files only**: create `tests/LamuFlix.UnitTests/Options/OmdbQuotaOptionsTests.cs`; edit `tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs` for `OmdbQuotaOptions` binding/ValidateOnStart only (FR-007; Q5; I1; recon-DEV-426:27; PLAN_CHALLENGE Ledger M3/M4). No other suite files.
- [X] T005 [US4] Edit `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` `PortWhitelistPattern` to include `IOmdbRequestGate` so Core port whitelist stays green (FR-007; recon-DEV-426:33; constitution 1.7.0 Principle I follow-up; ADR-0031)

---

## Phase 2: User Stories 1–2 & 4 — Persistence, atomic gate, migration (P1)

**Goal**: `omdb_daily_usage` + scoped `EfMetadataRequestGate` with real PostgreSQL proof.
**Independent Test**: Testcontainers/PostgresFixture concurrency, restart, rollover, limit-change, cancel, DB-failure, zero/one-row semantics.
**I1**: Composition/startup/health/options/persistence fixture adaptations required by scoped gate registration and the new table travel with this phase on T021 files only — do not force them to wait for Phase 3 completion.

- [X] T006 [P] [US1] Create `src/LamuFlix.Infrastructure/Persistence/Records/DailyRequestUsageRecord.cs` for `usage_date` / `request_count` (FR-003; Q4; PC1)
- [X] T007 [P] [US1] Create `src/LamuFlix.Infrastructure/Persistence/Configurations/DailyRequestUsageConfiguration.cs` mapping table `omdb_daily_usage` snake_case columns, date PK, `request_count` integer NOT NULL CHECK >= 0, **no** limit CHECK hardcoded to 1000 (FR-003; Q4; PC1)
- [X] T008 [US1] Edit `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs` to add the DbSet for `DailyRequestUsageRecord` (FR-003; PC1)
- [X] T009 [US2] Create `src/LamuFlix.Infrastructure/Persistence/EfMetadataRequestGate.cs` implementing `IOmdbRequestGate` with one parameterised PostgreSQL conditional insert-or-increment; sample TimeProvider UTC date and `DailyLimit` once per acquisition; **zero** affected/returned admission rows → `false`; **one** committed admission row → `true`; SQL errors → persistence faults (not silent bool); pre-cancelled token → `OperationCanceledException` with no increment (do not convert OCE/TaskCanceledException into a persistence signal); no refunds; retain prior-date rows (FR-004; Q2, Q4; PC1; PLAN_CHALLENGE Sentry M1/H2)
- [X] T010 [US2] Edit `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` to `AddScoped<IOmdbRequestGate, EfMetadataRequestGate>` beside other EF-backed scoped ports (FR-004; recon-DEV-426:62-70; PC1)
- [X] T011 [US1] Generate EF migration under `src/LamuFlix.Infrastructure/Persistence/Migrations/` and regenerate `LamuFlixDbContextModelSnapshot.cs` via EF tooling — never hand-edit generated output (FR-003; Q4)
- [X] T012 [US1] Update **exact** migration metadata guards only: `tests/LamuFlix.IntegrationTests/MigrationTests.cs` add `omdb_daily_usage` to `ApplicationTables`; `tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs` named-entity/column metadata for the new table; `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs` migration-script assertions that enumerate application tables (FR-003; recon-DEV-426:52; PLAN_CHALLENGE Ledger M3). No other suite files.
- [X] T021 [US5] Adapt **exact** composition/startup/health/options/persistence fixtures required by scoped `IOmdbRequestGate`/`EfMetadataRequestGate` registration and `omdb_daily_usage` so those suites stay green — quota-only `DailyLimit` config / registration / row isolation: `tests/LamuFlix.UnitTests/HealthCheckRegistrationTests.cs`, `tests/LamuFlix.UnitTests/Persistence/PersistenceServiceCollectionExtensionsTests.cs`, `tests/LamuFlix.UnitTests/Persistence/TasteProfileCacheRegistrationTests.cs` (FR-007; Q5; I1; recon-DEV-426:27; PLAN_CHALLENGE Ledger M3). Provider unit factories wait for T020; Api-host files wait for T022; WireMock probe/resilience wait for T023/T024. No open-ended remaining suite sweep.
- [X] T013 [US1] Add real-PostgreSQL integration tests proving exactly N admissions from >N synchronised contenders with independent contexts/scopes using **async coordinated start + `Task.WhenAll`** (no `Task.Delay` staggering, no blocking barrier waits); empty-table first-row race creates one row with exact count; exhaustion survives gate/context recreation on the same database (FR-007; Q5; SC-001–SC-002; mandatory; PLAN_CHALLENGE Sentry L3)
- [X] T014 [US2] Extend PostgreSQL gate tests for UTC midnight charging distinct dates while retaining the prior row; **retry across midnight samples its own date per acquisition**; lowered positive limit denies without reset/clamp (retain first-row and lowered-limit cases); raised limit resumes against carried count; zero-row → false / one-row → true / SQL error → fault; pre-cancelled token throws with no increment; database failure admits nothing (FR-007; Q5; SC-003, SC-006; mandatory; PLAN_CHALLENGE Sentry L1/M1)

---

## Phase 3: User Story 3 — Handler, signals, FetchAsync mapping (P1)

**Goal**: Per-physical-attempt admission inside resilience; two-signal mapping; health unchanged; partial discard; no scoped capture.
**Independent Test**: unit mapping/lifetime; shipped WireMock attempt counts after Phase 4 prerequisites.
**I1**: Provider-factory adaptations required by handler registration travel with this phase on T020 files only.

- [X] T015 [P] [US3] Create **separate files** `src/LamuFlix.Infrastructure/Adapters/MetadataQuotaExhaustedException.cs` and `src/LamuFlix.Infrastructure/Adapters/MetadataQuotaPersistenceException.cs` — two sealed Infrastructure-internal signal types (exhaustion vs persistence-failure wrapping original exception in memory only); neither is `HttpRequestException`/`TimeoutRejectedException`/synthetic retryable HTTP status; carry no API key or URI; never reach API exception mapping (FR-006; Q3; PC1; PLAN_CHALLENGE Ledger H1/L2/L5)
- [X] T016 [US3] Create `src/LamuFlix.Infrastructure/Adapters/MetadataQuotaHandler.cs` (`DelegatingHandler`) constructed only with `IServiceScopeFactory`; each `SendAsync` creates a bounded async scope, resolves scoped `IOmdbRequestGate`, calls `TryAcquireAsync` with the **ambient `SendAsync` cancellation token**, disposes the scope **before** `base.SendAsync` and on every exit (`true` / `false` / persistence fault / cancellation); `false` → exhaustion signal with no dispatch and no bookkeeping increment without `true`; gate fault → persistence signal after excluding OCE/TaskCanceledException; propagate caller cancellation unwrapped; no per-request mutable state on the handler; no scoped capture (FR-005; Q3; PC1/PC2; PLAN_CHALLENGE Sentry H1/H2/M3)
- [X] T017 [US3] Edit `src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs` to register `MetadataQuotaHandler` via `AddHttpMessageHandler` **after** `AddStandardResilienceHandler()` on the OMDb typed-client chain only (not TMDB/Ollama); do not alter resilience predicates/configuration (FR-005; Q3; recon-DEV-426:74-88; PC1)
- [X] T018 [US3] Edit `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs` `FetchAsync` to catch the two signals separately and map exhaustion → existing `RateLimited`, persistence → existing `ProviderUnavailable`; both leave `MetadataProviderHealthState` unchanged; when either signal interrupts a later variant after earlier candidates accumulated, **discard partial accumulation** and return the mapped failure; stop further OMDb search/detail fallback variants while preserving chained provider fallback; never convert caller cancellation into either mapped failure; signals must not escape to API exception mapping (FR-006; Q3; PC2 Compass M1/M2; PLAN_CHALLENGE Ledger L2)
- [X] T019 [US3] Edit `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.Log.cs` to add distinct structured outcome/category log events for exhaustion vs persistence-failure; **no** request URI, API key, or raw exception serialization (including unsafe scope fields); original persistence exception remains in-memory inner only; use existing processing scope movie id where available; invent no gate identifier (FR-006; Q3; PC2; PLAN_CHALLENGE Sentry M4, Ledger L4)
- [X] T020 [US3] Add/extend unit tests in **exact files only**: create `tests/LamuFlix.UnitTests/Adapters/MetadataQuotaHandlerTests.cs` (fresh scope per `SendAsync`, disposal asserted on true/false/fault/cancellation, dispose-before-transport, ambient token, no scoped capture); edit `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs` and `OmdbMetadataProviderFallbackUnitTests.cs` for both-signal FetchAsync mappings, health-state unchanged for both, partial-candidate discard for both later-interruption signals, fallback-loop termination, safe-log leakage assertions, cancellation never mapped (FR-007; Q5; I1; recon-DEV-426:42-43; PC2; PLAN_CHALLENGE Ledger M3, Sentry M3/M4). No other “related factory” files.

---

## Phase 4: User Stories 3 & 5 — Api-host isolation and shipped-composition evidence (P1/P2)

**Goal**: Enumerated Api factories stay deterministic; WireMock proves physical-attempt accounting through shipped composition.
**Independent Test**: end-to-end factories green; attempt-count + slow-gate matrix.
**I1**: Early fixture adaptations already traveled with Phases 1–3; this phase holds only T022 Api-host isolation plus mandatory WireMock evidence. T023/T024 require their fixture prerequisites.

- [ ] T022 [US5] Adapt **exact** Api-host files only: `tests/LamuFlix.IntegrationTests/ApiHostFactory.cs`, `ApiEndToEndFactory.cs`, `ApiEndToEndEnrichmentTests.cs`, `ApiEndToEndImportTests.cs`, `ApiEndToEndFeaturesTests.cs` — explicit test quota configuration and per-class/database row isolation (FR-007; Q5; I1; recon-DEV-426:49; PLAN_CHALLENGE Ledger M3). No open-ended leftover sweep beyond these five paths.
- [ ] T023 [US3] Add/extend shipped-factory + WireMock Theory/MemberData tests via existing `MetadataProviderProbe.BuildServices` → shipped `AddMetadataProvider` composition in **exact files**: `tests/LamuFlix.IntegrationTests/MetadataProviderProbe.cs`, `MetadataProviderResilienceTests.cs` (brief Q5 spelling note — not a rename): direct detail success commits **1**; one successful search + one detail commits **2**; each extra physical request adds one; always-429 and always-500 consume `1+MaxRetryAttempts`; 503-then-200 consumes 2 (FR-007; Q5; SC-004; mandatory; PLAN_CHALLENGE Compass L1, Ledger M4; requires T017 + T020/T021/T022)
- [ ] T024 [US3] Extend shipped WireMock evidence in the same probe/resilience files: exhaustion during a retry sequence prevents the next transport request, stops OMDb variants, maps `RateLimited`, preserves chain fallback (both-signal interruption after partial candidates discards accumulation); open breaker and cancelled retry backoff spend no extra reservation; database failure yields `ProviderUnavailable` with zero dispatch; caller cancellation stays `OperationCanceledException` / never mapped to signals; acquired failures/cancellation never refund; **slow-gate shipped-composition** case bounds admission commits/invocations by configured attempt/total budgets with **no retry-policy change** and **no commits-equal-wire assertion under timeout**; retain in-flight committed/no-refund caveat (FR-007; Q5; SC-005; mandatory; PC2 Sentry H1; requires T017 + T020/T021/T022)

---

## Phase 5: Polish & Phase B verification

- [ ] T025 Confirm frozen scope only: no public API/DTO/OpenAPI/TypeScript, no LocalPlay/Process.Start/secrets, no new NuGet/project/top-level folder/layer, no schema beyond `omdb_daily_usage`, no hand-edited snapshot, no DEV-333 bulk-scan, no whole-file rewrite/deletion, no cache/startup-log feature, no wider constitution exception; constitution remains the Gate 1–approved 1.7.0 staged I/VII/VIII artifact (verify-only; no re-apply; no date edit); ADR Accepted only via Keel in implementation PR (FR-008; Q6; C1/C2; PC2)
- [ ] T026 Run Roslyn analyzers on explicit changed `.cs` set via `./scripts/run-roslyn-analyzers.ps1` (FR-009; Q5)
- [ ] T027 Run `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml on changed files (FR-009)
- [ ] T028 Run `./scripts/run-jetbrains-inspectcode.ps1` on changed files (FR-009)
- [ ] T029 `dotnet format --verify-no-changes` clean (FR-009)
- [ ] T030 Run affected unit/integration/architecture suites then full `dotnet test` (FR-009; SC-007)
- [ ] T031 Run property/security/vulnerable-package gates per harness; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS (FR-009; Q6)
- [ ] T032 Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree (FR-009; Q5)
- [ ] T033 Size M code review with Sentry, Ledger and Compass; apply Q6 closing bar (Critical/High/Medium block; Lows explicit disposition) and 2-round remediation cap; missing axis is blocked; do not lower thresholds (FR-009; Q6)

---

## Dependencies

- Amendment-patch + ADR-0031 Proposed preparation and constitution 1.7.0 staging: already done in Phase A / this spec PR — no task dependency; not gated on T034/owner merge; Phase B does not re-apply or date-edit.
- T034 before T035 verify; T034–T035 before any T001–T033 (Gate 1 / verify path only). ADR Accepted status bump is Keel’s implementation-PR step after T034 evidence, not a Phase 0 constitution mutation.
- T001/T002 can proceed in parallel after T035; T003 after T002; T004 after T003 (T004 files only); T005 after T001.
- Phase 2 after Phase 1. T006/T007 parallel; T008 after T006; T009 after T006–T008 and T003; T010 after T009; T011 after model ready; T012 after T011 (T012 files only); T021 after T010–T012 (T021 files only — does **not** wait for Phase 3); T013–T014 after T009–T012 (and after T021 when those fixtures are needed for the PostgreSQL suites).
- Phase 3 after Phase 2 gate registration. T015 parallel with early Phase 3 prep; T016 after T015; T017 after T016; T018–T019 after T015; T020 after T016–T019 (T020 files only).
- Phase 4: T022 after Phase 3 registration/mapping and after early fixture work (T004/T021/T020) as needed for Api hosts (T022 files only). T023–T024 after T017 and after fixture prerequisites T020/T021/T022 — do not start the WireMock matrix without those prerequisites.
- T025–T033 after implementation and mandatory Q5 evidence (T013–T014, T023–T024) complete.

## Sequencing examples

```text
# Phase 0 (patch/ADR Proposed prepared; constitution 1.7.0 staged in this Phase A / spec PR)
T034 Keel records Gate 1 checkbox answer + verified spec-PR merge receipt in CONCLUSIONS.md
T035 verify merged approved constitution is 1.7.0 with staged I/VII/VIII (NO constitution edit including dates;
     date-only difference recorded in CONCLUSIONS.md; substantive mismatch blocks)
     — ADR Accepted status bump: Keel in implementation PR after verified owner approval, before code merge

# Phase 1
T001 IOmdbRequestGate.cs
T002 OmdbQuotaOptions.cs (sealed record, SectionName, Range)
T003 ServiceDefaults BindAndValidate
T004 OmdbQuotaOptionsTests + OptionsStartupValidationTests only
T005 ArchitectureTests PortWhitelistPattern

# Phase 2
T006-T008 DailyRequestUsageRecord/Configuration/DbSet
T009-T010 EfMetadataRequestGate + scoped registration
T011-T012 migration + IntegrationTests/MigrationTests + UnitTests/Persistence/ModelMetadataTests + IntegrationTests/PersistenceCompositionTests only
T021 UnitTests/HealthCheckRegistrationTests + UnitTests/Persistence/PersistenceServiceCollectionExtensionsTests + TasteProfileCacheRegistrationTests only
T013-T014 PostgreSQL evidence (async WhenAll; zero/one-row; midnight retry)

# Phase 3
T015 MetadataQuotaExhaustedException + MetadataQuotaPersistenceException (separate files)
T016 MetadataQuotaHandler (ambient token; dispose-before-transport every path)
T017 AddHttpMessageHandler after resilience
T018-T019 FetchAsync mapping (health unchanged; discard partials) + safe log events
T020 MetadataQuotaHandlerTests + OmdbMetadataProviderUnitTests + OmdbMetadataProviderFallbackUnitTests only

# Phase 4
T022 ApiHostFactory / ApiEndToEndFactory / three ApiEndToEnd* suites only
T023-T024 MetadataProviderProbe + MetadataProviderResilienceTests (exact counts; slow-gate budgets)
```

## MVP

MVP delivers the ticket budget with **all Q5 evidence mandatory**: T034–T035 (Gate 1 record + verify-only constitution; ADR Accepted in implementation PR), T001–T020, T021 (persistence-step fixtures on enumerated files), T013–T014 (PostgreSQL concurrency/restart/rollover/limit/cancel/failure/zero-one-row), T022 (Api-host isolation on enumerated files), and T023–T024 (full shipped WireMock matrix including mid-retry/breaker/cancel/refund/slow-gate). Fixture adaptations travel with the first step that needs them on enumerated disjoint files (I1; PC2); T022/T023/T024 stay later as prerequisites chain — not optional polish. Gates T025–T033 follow.

## Notes

- `[P]` = different files, no incomplete deps. Stable IDs; phase table governs order (Ledger L5).
- Do not edit files outside the brief/recon frozen enumerated set without a cited decision amendment (Q6; Ledger M3). Missing paths → needs recon, not invention.
- `constitution-amendment-v1.7.0.md` and ADR-0031 Proposed are prepared in this Phase A / spec PR; Constitution 1.7.0 is staged into `.specify/memory/constitution.md` for owner review. Quill does not author ADR decision content or edit `.specify/memory/constitution.md`. T035 verifies the merged approved 1.7.0 artifact with **no constitution edit including dates** (PC2). C1/C2 supersede the earlier Q6 no-ADR wording; PC2 supersedes I2 date-reconcile / same-delivery ADR publish wording.
- Carry accepted denied-attempt DB cost and mismatched-host operational precondition; no cache or startup-log feature (PLAN_CHALLENGE Sentry M2/L2).
- Refresh recon at Phase B pickup for main/contract drift (brief).
