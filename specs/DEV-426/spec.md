# Feature Specification: OMDb Daily Request Quota

**Feature Branch**: `feature/426-spec`
**Created**: 2026-10-28
**Status**: gate1: provisional — basis: clean `/speckit-analyze` (C1/C2/I2 residuals ruled and remediated, `CONCLUSIONS.md:176-192`) and PLAN FROZEN (`PLAN_CHALLENGE.md:3,110-122`; 24/24 findings disposed, round 2 of 2). Gate 1 remains closed until owner approves ADR-0031 and constitution amendment 1.7.0 via the spec-PR merge (checkbox below unchecked); ADR-0031 remains Proposed; no implementation clearance.
**Input**: Ticket DEV-426 (parent DEV-100, Size M; prerequisite for DEV-333); frozen `brief.md` (Q1–Q6 + post-grill C1/C2/I1 + PC1/PC2 plan-challenge supplement); `CONCLUSIONS.md` (incl. Patron PC1/PC2 at c625ea29); Proposed ADR-0031; proposed `constitution-amendment-v1.7.0.md`; `PLAN_CHALLENGE.md`; Maestri notes `DEV-426`, `recon-DEV-426`; `specs/PRODUCT.md`

## Owner question — Gate 1 stays closed

- [ ] Owner approves ADR-0031 and constitution amendment 1.7.0 for the ticket-named IOmdbRequestGate and OmdbQuotaOptions; approval occurs through the Gate 1 spec-PR merge.

Carried from `brief.md` post-grill C1/C2 correction and PC1/PC2 supplement (Patron rulings in `CONCLUSIONS.md`). This is the amendment path required by Principles I / VII / VIII and Governance — not a waiver and not present owner approval. Gate 1 stays closed until the owner approves. Owner rejection becomes `blocked: structural — ticket-named port/types vs constitution`. Proposal record at `constitution-amendment-v1.7.0.md`; constitution.md already staged 1.7.0 for review. After verified owner approval and merge, Keel records the checkbox answer plus verified spec-PR merge receipt in `CONCLUSIONS.md` (T034). T035 verifies the merged constitution without editing it (including dates): a date-only staged-vs-merge difference is recorded in `CONCLUSIONS.md`, not fixed; a substantive version/I/VII/VIII/Sync Impact Report mismatch blocks. Keel changes ADR-0031 status to Accepted only in the implementation PR after verified owner approval and before code merge — no write to main, no same-PR status bump inferred from planning. No noncompliant implementation is authorized in advance. (PC2; PLAN_CHALLENGE Compass M3/L2, Ledger M2.)

## Scope

**In scope** (ticket Scope & Technical Design; brief Q1–Q6 + post-grill C1/C2/I1 + PC1/PC2):

- Persisted daily OMDb request budget shared per configured database (default 1000 per UTC calendar date), prerequisite for DEV-333 bulk scans.
- Core port `IOmdbRequestGate.TryAcquireAsync(CancellationToken)` returning `Task<bool>`; Core options `OmdbQuotaOptions` (`Omdb:Quota`, `DailyLimit` default 1000, positive at startup) — ticket names preserved via proposed constitution 1.7.0 + ADR-0031.
- Proposed ADR-0031 and proposed MINOR constitution amendment 1.6.0 → 1.7.0 (Principle I closed port list += `IOmdbRequestGate`; Principle VII options list += `OmdbQuotaOptions`; Principle VIII vendor exceptions += `IOmdbRequestGate` and `OmdbQuotaOptions`); architecture whitelist follow-up; owner Gate 1 checkbox above.
- Counter table `omdb_daily_usage` (`usage_date` date PK, `request_count` integer NOT NULL CHECK >= 0) with EF `DailyRequestUsageRecord` / `DailyRequestUsageConfiguration` / DbSet, generated migration/snapshot, and MigrationTests.ApplicationTables update. (PC1 functional support names; ticket table name exact.)
- Atomic PostgreSQL conditional insert-or-increment admission; reserve before transport; no refunds; prior-date rows retained; no cleanup job. Zero affected/returned admission rows → `false`; one committed admission row → `true`; SQL errors → persistence faults. (PC2; PLAN_CHALLENGE Sentry M1.)
- OMDb-only `MetadataQuotaHandler` (`DelegatingHandler`) registered via `AddHttpMessageHandler` **after** `AddStandardResilienceHandler` on the OMDb typed-client chain; one reservation per physical outbound attempt; ambient `SendAsync` cancellation token passed into the gate; `IServiceScopeFactory` + fresh async scope per `SendAsync` disposed before transport (no scoped capture); no dispatch and no bookkeeping increment without a returned `true`. (PC1/PC2; PLAN_CHALLENGE Sentry H1.)
- Two sealed Infrastructure-internal signals, one file each — `MetadataQuotaExhaustedException` and `MetadataQuotaPersistenceException` — mapped in `FetchAsync`: exhaustion → existing `RateLimited`; gate persistence failure → existing `ProviderUnavailable`; distinct safe structured outcome/category logs; OMDb search/detail fallback variants stop; chained provider fallback preserved; neither signal changes `MetadataProviderHealthState`; later interruption discards accumulated partial candidates and returns the mapped failure; caller cancellation propagates unwrapped and is never converted to either signal. (PC1/PC2; PLAN_CHALLENGE Compass M1/M2, Sentry H2.)
- Concurrent / restart / UTC rollover / limit-change / shipped-factory+WireMock evidence per Q5 (all mandatory); fixture adaptations needed for a step travel with that step on **enumerated recon-backed disjoint file sets**; remaining Api-host isolation and WireMock matrix after their prerequisites (I1; PC2; PLAN_CHALLENGE Ledger M3).

**Out of scope** (ticket; brief Q1, Q4, Q6; PC2):

- Per-key / per-caller / per-process budgets; bypass flag; API-key storage or key-derived partition; cleanup/prune job; refund/reaper.
- Hot reload (`IOptionsMonitor`); distributed configuration or cross-instance reconciliation protocol; startup-log feature for mismatched host limits; negative-cache abstraction.
- Retry-policy redesign or changes to resilience `ShouldHandle` predicates / `Omdb:Resilience` defaults; hedging; counting once in `FetchAsync` as the unit of account; asserting commits equal wire requests under admission timeout.
- Public API, DTO, OpenAPI, UI, or generated TypeScript; `Features:LocalPlay`, `Process.Start`, or new secret handling; `OmdbOptions` / TMDB match-threshold changes.
- New NuGet/npm dependency, project, top-level folder, or architectural layer; schema beyond `omdb_daily_usage`; whole-file rewrite or deletion; DEV-333 bulk-scan implementation.
- New domain glossary term or root `CONTEXT.md` rewrite (Q6; PC1 Ledger M1 closed); README/C4; Gherkin unless separately opted in. Wider constitution exception for Infrastructure support types (PC1 Ledger H1 closed — functional names instead). (ADR-0031 and constitution 1.7.0 remain in scope as the C1/C2 amendment path — not a silent port addition.)

**Precedence**: Ticket Scope & Technical Design is already approved authority. Brief Q1–Q6, post-grill C1/C2/I1 correction, PC1/PC2 plan-challenge supplement, and `CONCLUSIONS.md` govern boundary, atomic contract, seam, schema/options, tests, frozen scope, loop discipline, naming, and Phase B verify path. Phase A produces documents only and claims no implementation gate evidence.

## User Scenarios & Testing

### User Story 1 - Shared Persisted Daily Budget (Priority: P1)

As the enrichment pipeline (and eventual DEV-333 bulk scans), I need every OMDb transport attempt to compete for one shared persisted daily budget on the configured database so the free-tier 1,000/day limit is respected across callers, scopes, processes, and hosts.

**Why this priority**: ticket deliverable; Q1.

**Independent Test**: real PostgreSQL gate tests — N exact admissions from >N synchronised contenders with independent contexts/scopes (async coordinated start + `Task.WhenAll`; no delay staggering or blocking barrier waits); exhaustion survives gate/context recreation; empty-table first-row race creates one row with exact count.

**Acceptance Scenarios**:

1. **Given** a configured database and positive `DailyLimit` **When** callers acquire reservations for the TimeProvider UTC date **Then** committed increments never exceed the limit sampled for that acquisition (Q1, Q2, Q4).
2. **Given** more than N contenders racing with independent scopes when `DailyLimit` is N **When** they finish **Then** exactly N admissions succeed and the rest observe exhaustion (`false`) without increment (Q2, Q5).
3. **Given** exhaustion on a date **When** a new gate/DbContext is created against the same database **Then** further acquisitions that date still return `false` (restart/persistence) (Q2, Q5).
4. **Given** separate API keys that share one database **When** they acquire **Then** they share the same counter (accepted cost; no per-key partition) (Q1).

### User Story 2 - Atomic Admission Without Refunds (Priority: P1)

As a maintainer, I need `TryAcquireAsync` to commit exactly one reservation via a single parameterised conditional insert-or-increment (never read-then-write or process lock), sample UTC date and limit once, and never refund after HTTP failure, timeout, cancellation after `true`, or crash.

**Why this priority**: Q2; load-bearing concurrency contract.

**Independent Test**: PostgreSQL tests for atomic SQL behaviour (zero affected/returned rows → `false`; one committed admission row → `true`; SQL errors → faults), first-row race, lowered-positive-limit denial, pre-cancellation with zero increment; no mocked EF/driver translation.

**Acceptance Scenarios**:

1. **Given** an acquisition **When** it returns `true` **Then** exactly one reservation was committed (one committed admission row) and the caller may dispatch once (Q2; PC2 Sentry M1).
2. **Given** the daily limit already reached **When** acquisition runs **Then** it returns `false` with no increment (zero affected/returned admission rows; exhaustion is not an exception) (Q2; PC2 Sentry M1).
3. **Given** a token already cancelled before acquisition **When** `TryAcquireAsync` is called **Then** `OperationCanceledException` propagates with no increment and no dispatch (Q2).
4. **Given** cancellation while the statement is in flight **When** no `true` was returned **Then** the caller must not dispatch or reuse any reservation that may have committed; the Q2 in-flight committed-reservation / no-refund caveat stands (Q2; PC2 Sentry H1).
5. **Given** a returned `true` **When** the ensuing HTTP attempt fails, times out, is cancelled, or the process crashes **Then** the reservation is never refunded (Q2, Q3).
6. **Given** prior UTC dates with rows **When** the current day advances **Then** prior rows remain; there is no cleanup job (Q2, Q4).
7. **Given** SQL/driver faults during admission **When** the gate runs **Then** the outcome is a persistence fault (not silent `false`/`true`) (PC2 Sentry M1).

### User Story 3 - Per-Physical-Attempt Seam and Failure Mapping (Priority: P1)

As the OMDb adapter, I need admission inside the resilience pipeline so each physical outbound attempt (original and automatic retries) spends a reservation, while breaker/rate-limiter/total-timeout rejections above the handler spend none, and exhaustion/persistence failures map to existing failure categories without triggering retries.

**Why this priority**: Q3; recon 9.1–9.2; ticket wiring.

**Independent Test**: shipped `AddMetadataProvider` composition + WireMock (MetadataProviderProbe.BuildServices pattern) for attempt counts and slow-gate budgets; unit tests for FetchAsync mappings, both-signal health-state invariance, partial-candidate discard, fallback termination, scope disposal on every exit, and no scoped capture.

**Acceptance Scenarios**:

1. **Given** the OMDb typed-client registration **When** `MetadataQuotaHandler` is registered via `AddHttpMessageHandler` after `AddStandardResilienceHandler` **Then** `SendAsync` runs once per physical attempt and passes the ambient `SendAsync` cancellation token into `TryAcquireAsync` (Q3; PC2; recon-DEV-426:74-88).
2. **Given** always-429 or always-500 with `MaxRetryAttempts` **When** a lookup runs **Then** persisted admissions equal `1 + MaxRetryAttempts` (Q3, Q5).
3. **Given** 503-then-200 **When** a lookup runs **Then** persisted admissions equal 2 (Q3, Q5).
4. **Given** a successful direct detail fetch (no search) **When** the lookup completes **Then** exactly **1** reservation/request is committed; **Given** one successful search followed by one detail fetch **Then** exactly **2**; each additional physical variant/request adds one more (Q1/Q5; PLAN_CHALLENGE Compass L1).
5. **Given** open circuit breaker or cancelled retry backoff before a further attempt **When** the pipeline rejects above the handler **Then** no additional reservation is spent (Q3, Q5).
6. **Given** gate returns `false` **When** the handler runs **Then** no transport call is made and no bookkeeping increment occurs without `true`; FetchAsync maps `MetadataQuotaExhaustedException` to `RateLimited`; OMDb fallback variants stop; chained provider fallback still proceeds; `MetadataProviderHealthState` is unchanged (Q3; PC2 Compass M1).
7. **Given** gate persistence failure **When** the handler runs **Then** no transport call is made; FetchAsync maps `MetadataQuotaPersistenceException` to `ProviderUnavailable` with original exception preserved as in-memory inner only; OMDb variants stop; chain fallback preserved; `MetadataProviderHealthState` is unchanged (Q3; PC2 Compass M1).
8. **Given** either local signal interrupts a later search/detail variant after earlier variants already returned candidates **When** FetchAsync handles it **Then** accumulated partial candidates are discarded, the mapped `RateLimited`/`ProviderUnavailable` failure is returned, further OMDb variants stop, and chained-provider fallback is preserved (PC2 Compass M2).
9. **Given** either signal **Then** it is not `HttpRequestException`, `TimeoutRejectedException`, or a synthetic retryable HTTP response/status, carries no API key or URI, does not engage retry or breaker predicates, and never escapes Infrastructure into API exception mapping (Q3; PLAN_CHALLENGE Ledger L2).
10. **Given** caller cancellation (including `OperationCanceledException` / `TaskCanceledException`) **Then** it propagates unwrapped, is excluded before fault-to-signal mapping, and is never mapped to either signal/`RateLimited`/`ProviderUnavailable` (Q3; PC2; PLAN_CHALLENGE Sentry H2).
11. **Given** each `SendAsync` **Then** a fresh async scope resolves scoped `IOmdbRequestGate`, the bounded async scope ends and is disposed before `base.SendAsync` and on every exit (`true`, `false`, persistence fault, cancellation), and the handler captures only singleton `IServiceScopeFactory` (Q3; PLAN_CHALLENGE Sentry M3).
12. **Given** a slow-admission composition under existing attempt/total budgets **When** admission times out or cancels **Then** committed admissions and gate invocations are bounded by configured attempt/total budgets and by admission invocations — not asserted equal to wire count; resilience policy is unchanged; pre-cancelled or not-yet-committed cancelled/timeout admission adds no increment; in-flight committed reservation remains unrefundable (PC2 Sentry H1).
13. **Given** distinct structured outcome/category logs for the two signals **Then** logs contain no request URI, API key, or raw exception serialization (including unsafe scope fields); original persistence exception remains in memory as inner only; existing enrichment processing scope supplies movie id where available; no gate-invented identifier is introduced; tests assert no leakage (PC2; PLAN_CHALLENGE Sentry M4, Ledger L4).

### User Story 4 - Options, Schema, and Limit Changes (Priority: P1)

As an operator, I need `Omdb:Quota:DailyLimit` validated at startup (default 1000; zero/negative rejected), a snake_case `omdb_daily_usage` table without a hardcoded-1000 CHECK, and restart-only configuration so lowering the limit denies further admissions that date without rewriting history.

**Why this priority**: Q4; Q6 IOptions snapshot refinement.

**Independent Test**: options/startup unit tests (1000, 1, 0, negative) via Theory/MemberData with sealed-record/`SectionName`/`[Range(1, int.MaxValue)]` shape; migration metadata guards; PostgreSQL lowered/raised limit and UTC midnight / cross-midnight retry cases.

**Acceptance Scenarios**:

1. **Given** host startup **When** `DailyLimit` is missing **Then** default 1000 applies; when 0 or negative, startup validation fails the host (Q4, Q5).
2. **Given** the migration **Then** `omdb_daily_usage` has `usage_date` date PK and `request_count` integer NOT NULL CHECK (`request_count >= 0`) only — no schema CHECK tying count to 1000 or to `DailyLimit` (Q4).
3. **Given** a process running with a lowered `DailyLimit` below already-spent count for today **When** acquisition runs **Then** further admissions that date are denied without reset/clamp/rewrite; stored count may exceed the new limit as history (Q4).
4. **Given** a raised `DailyLimit` after restart **When** acquisition runs **Then** admissions resume against the carried count up to the new limit (Q4, Q6).
5. **Given** `OmdbQuotaOptions` **Then** it is a sealed record with `SectionName` constant `Omdb:Quota`, `DailyLimit` annotated `[Range(1, int.MaxValue)]`, bound via existing ServiceDefaults `BindAndValidate` / data annotations / `ValidateOnStart`; `OmdbOptions` and TMDB match settings are unchanged; quota holds only the numeric limit (no enable flag, timezone setting, or key) (Q4; PLAN_CHALLENGE Ledger L3).
6. **Given** mismatched `DailyLimit` across instances sharing a database **Then** that is an accepted operational precondition / operator error; the gate does not detect, reconcile, or add a startup-log feature for it (Q4; PLAN_CHALLENGE Sentry L2 closed).

### User Story 5 - Ordered Delivery and Verification Evidence (Priority: P2)

As implementers, I need the Q5 sequence (Core → persistence → handler/mapping → remaining fixture isolation → full verification) with tests landing with each step, prerequisite fixture adaptations traveling with the first step that needs them on enumerated disjoint recon-backed files (I1), all Q5 evidence mandatory, and Size M review under the Q6 closing bar — after Gate 1 owner approval of ADR-0031 + constitution 1.7.0.

**Why this priority**: Q5, Q6, I1; enables Gate 1 planning and Phase B execution.

**Independent Test**: task checklist and gate scripts per harness; three-axis review when implementation lands.

**Acceptance Scenarios**:

1. **Given** implementation order **Then** Core port/options + ServiceDefaults binding land with validation tests and options/startup fixture adaptations they need before EF gate; migration/snapshot are EF-generated (never hand-edited) with persistence/composition fixture adaptations traveling with scoped gate registration; handler and FetchAsync mapping land with the provider-factory adaptations they need; Api-host isolation precedes the shipped WireMock matrix (Q5, I1).
2. **Given** test fixtures **Then** explicit test `DailyLimit` and per-class/database quota-row isolation on enumerated files prevent unrelated suites from consuming each other’s budgets; no open-ended “related/remaining” suite sweep authorises extra files (Q5, I1; PC2; PLAN_CHALLENGE Ledger M3).
3. **Given** verification **Then** every Q5 PostgreSQL and shipped-factory+WireMock evidence item is mandatory (not polish); Roslyn, complexity, InspectCode, unit/integration/architecture/property/full suite, vulnerable packages, format, and pre-PR mutation run per harness; exit 2 SKIPPED/SKIP/N/A and Could not run are never reported as PASS; surviving mutants require missing tests, never lowered thresholds (Q5, Q6, I1).
4. **Given** code-review/ship-review **Then** no unresolved Critical/High/Medium; Lows have explicit dispositions; at most two remediation rounds; Size M requires Sentry, Ledger, and Compass (Q6).

### Edge Cases

- Acquisition straddling UTC midnight charges wholly to the single sampled date for that acquisition (Q2).
- A retry across UTC midnight samples its own date independently per acquisition (Q2/Q3; PLAN_CHALLENGE Sentry L1).
- Empty-table first-row race under concurrency creates exactly one row with the correct count (Q5).
- Exhaustion or persistence failure mid-retry / later-variant sequence: no further transport request; partial candidates discarded; OMDb variants stop; mapped failure; chain fallback preserved; health state unchanged (Q3, Q5; PC2).
- Database failure at the gate: zero dispatch; `ProviderUnavailable` (Q3, Q5).
- Exact positive counts: direct detail = 1; one search plus detail = 2; each extra physical request adds one (Q5; PLAN_CHALLENGE Compass L1).
- Historical count may exceed a newly lowered `DailyLimit` — schema permits it; predicate denies further increments (Q4).
- Conservative lost reservations after `true` without successful HTTP are accepted; one DB round trip per denied attempt is an accepted Q2 cost — no cache feature (Q2; PLAN_CHALLENGE Sentry M2 closed).
- Mismatched host limits remain an operational precondition without detection/reconciliation/startup-log feature (Q4; PLAN_CHALLENGE Sentry L2 closed).
- Anything outside frozen scope is a follow-up issue, not a finding in this round (Q6).

## Requirements

### Functional Requirements

- **FR-001**: Add Core port `IOmdbRequestGate` in `src/LamuFlix.Core/Ports/IOmdbRequestGate.cs` with `Task<bool> TryAcquireAsync(CancellationToken cancellationToken)`. `true` means exactly one reservation committed; `false` means exhausted with no increment. Core must not reference EF, Infrastructure, or DI containers (Q2, Q5; recon-DEV-426:30-33). Authorised only via Proposed ADR-0031 and proposed constitution 1.7.0 Principle I closed-list addition (C1); ticket name preserved.
- **FR-002**: Add Core sealed record `OmdbQuotaOptions` in `src/LamuFlix.Core/Options/OmdbQuotaOptions.cs`, `public const string SectionName = "Omdb:Quota"`, single `int DailyLimit` default 1000 with `[Range(1, int.MaxValue)]`. Bind and validate via existing ServiceDefaults `AddLamuFlixOptions` / `BindAndValidate` / data annotations / `ValidateOnStart` like existing options. Startup validation rejects non-positive `DailyLimit` (host fails to start). Consume as startup `IOptions` snapshot — no `IOptionsMonitor`/hot reload; changes take effect on process restart (Q4, Q6). Do not modify `OmdbOptions` or TMDB match settings. Authorised only via proposed constitution 1.7.0 Principle VII options-list and Principle VIII vendor-exception additions (C2); ticket name preserved; ADR-0031. (PLAN_CHALLENGE Ledger L3.)
- **FR-003**: Persist usage in table `omdb_daily_usage` with `usage_date` (PostgreSQL `date`) primary key and `request_count` (`integer NOT NULL CHECK (request_count >= 0)`). Model as `DailyRequestUsageRecord` + `DailyRequestUsageConfiguration` under existing Persistence Records/Configurations patterns (snake_case). Add DbSet on `LamuFlixDbContext`. Enforce the configured limit only inside the atomic SQL predicate parameterised with the sampled `DailyLimit` — never a schema CHECK hardcoded to 1000 or to the limit. Generate migration and regenerate `LamuFlixDbContextModelSnapshot` with EF tooling; never hand-edit generated output. Add `omdb_daily_usage` to `MigrationTests.ApplicationTables` and cover model/migration metadata as named in plan/tasks (Q4, Q5; recon-DEV-426:36-38,52; PC1).
- **FR-004**: Implement scoped Infrastructure adapter `EfMetadataRequestGate` registering beside other EF-backed scoped ports in `PersistenceServiceCollectionExtensions`. Use one parameterised PostgreSQL conditional insert-or-increment for the TimeProvider UTC date sampled once per acquisition (limit read once). Interpret **zero** affected/returned admission rows as `false`, **one** committed admission row as `true`; SQL errors remain persistence faults. Never read-then-write or process-local lock as authority. Retain prior-date rows; no cleanup job. Pre-cancelled token → `OperationCanceledException` with no increment. Persistence faults fail closed (no partial silent success). Exclude converting `OperationCanceledException` / `TaskCanceledException` into the persistence signal (Q2, Q4, Q5; PC1/PC2; PLAN_CHALLENGE Sentry M1/H2).
- **FR-005**: Add OMDb-only `MetadataQuotaHandler` (`DelegatingHandler`) under Infrastructure Adapters, registered with `AddHttpMessageHandler` **after** `AddStandardResilienceHandler()` on the OMDb typed-client chain only (not TMDB, not Ollama) in `MetadataProviderServiceCollectionExtensions`. Construct the handler only with singleton `IServiceScopeFactory`; each `SendAsync` opens `CreateAsyncScope()`, resolves scoped `IOmdbRequestGate`, calls `TryAcquireAsync` with the ambient `SendAsync` cancellation token, disposes the bounded async scope before `base.SendAsync` and on every exit; capture no scoped DbContext/gate; hold no per-request mutable state on the handler instance. No dispatch and no bookkeeping increment without a returned `true`. Do not count once in `FetchAsync` as the unit of account; do not alter retry predicates/configuration (Q3; PC1/PC2; recon-DEV-426:74-88; PLAN_CHALLENGE Sentry H1/M3).
- **FR-006**: On `false` admission throw sealed Infrastructure-internal `MetadataQuotaExhaustedException` (own file; not `HttpRequestException` / `TimeoutRejectedException` / synthetic retryable HTTP status; no API key or URI). On gate persistence failure throw distinct sealed internal `MetadataQuotaPersistenceException` (own file) wrapping the original exception in memory only. Before fault-to-signal mapping, exclude `OperationCanceledException` / `TaskCanceledException` so cancellation never becomes either signal. In `OmdbMetadataProvider.FetchAsync`, catch each separately: exhaustion → existing `RateLimited` failure; persistence → existing `ProviderUnavailable` failure; emit distinct structured outcome/category log events in `OmdbMetadataProvider.Log.cs` with **no** URI, API key, or raw exception serialization (including unsafe scope fields); original inner preserved in memory only; existing processing scope supplies movie id where available; invent no gate identifier. Both signals leave `MetadataProviderHealthState` unchanged (neither observes upstream health; existing HTTP-path health behavior unchanged). Both stop further OMDb search/detail fallback variants while preserving chained-provider fallback. When either signal interrupts a later variant after earlier candidates accumulated, discard partial accumulation and return the mapped failure. Propagate caller cancellation unwrapped. Never refund after admission. Signals are Infrastructure-internal only and must never reach API exception mapping (Q3; PC1/PC2; PLAN_CHALLENGE Compass M1/M2, Sentry H2/M4, Ledger L2/L4).
- **FR-007**: Tests land with each implementation step (Q5, I1). Required evidence (all mandatory — none deferred as polish):
  - Options/startup: default 1000 and 1 accepted; 0 and negative rejected (Theory/MemberData; AutoFixture/Faker fresh data as applicable; explicit domain boundary inputs); architecture boundaries preserved (including `ArchitectureTests` port whitelist gain for `IOmdbRequestGate`).
  - Real PostgreSQL (Testcontainers/PostgresFixture): exact N of >N contenders with async coordinated start + `Task.WhenAll` (no delay staggering/blocking waits); first-row race; exhaustion across recreated gate/context; UTC midnight distinct dates with prior row retained; retry across midnight samples date per acquisition; lowered/raised limits preserve history; pre-cancellation no increment; zero-row → false / one-row → true / SQL error → fault; DB failure admits nothing — never mock EF/driver behaviour that depends on SQL translation.
  - Unit: FetchAsync signal mappings and fallback-loop termination for **both** signals; health-state unchanged for both; partial-candidate discard for both later-interruption signals; handler fresh scope per attempt; disposal asserted on true/false/fault/cancellation and dispose-before-transport; no scoped capture; safe-log leakage assertions (no URI/key/raw exception/unsafe scope fields).
  - Shipped-factory + WireMock via existing composition (`MetadataProviderProbe.BuildServices` → shipped `AddMetadataProvider`; brief Q5 spelling note): direct detail success = 1; one search + one detail = 2; each extra physical request adds one; always-429/always-500 → `1+MaxRetryAttempts`; 503-then-200 → 2; exhaustion mid-retry → no further transport, OMDb variants stop, `RateLimited`, chain fallback preserved; open breaker and cancelled backoff spend no extra reservation; DB failure → `ProviderUnavailable` with zero dispatch; caller cancellation stays `OperationCanceledException`; no refunds after acquired failure/cancellation; slow-gate composed test bounds admission commits/invocations by configured attempt/total budgets with no retry-policy change and no commits-equal-wire assertion under timeout.
  - Fixture adaptations travel only on enumerated recon-backed disjoint ownership in tasks T004/T012/T020/T021/T022 (quota-only config/registration/isolation plus named migration guards). No open-ended related/remaining suite files. Suites use explicit test `DailyLimit` and isolate quota rows per test class/database. Keep provider/fallback/resilience/chain/options/startup/health-registration/migration regressions green on those enumerated files (Q5, I1; PC2; PLAN_CHALLENGE Ledger M3).
- **FR-008**: Edit only the recon/brief-named create and edit set (see plan Project Structure), plus post–Gate 1 **verify-only** confirmation that the merged approved `.specify/memory/constitution.md` matches Version 1.7.0 and the Gate-1-approved **staged** I/VII/VIII clauses / Sync Impact Report already in that constitution file (**no constitution edit including dates**). T035 does **not** require literal equality to the illustrative Sync Impact Report summary in `constitution-amendment-v1.7.0.md`. Keel records checkbox answer + verified spec-PR merge receipt in `CONCLUSIONS.md`; ADR-0031 Accepted delivery follows CONCLUSIONS PC1/PC2 (Keel bumps status only in the implementation PR after verified owner approval and before code merge). Narrow integration edits in existing files; no whole-file rewrite or deletion. No public API/DTO/OpenAPI/TypeScript; no LocalPlay/Process.Start/secrets; no new dependency/project/top-level folder/layer; no schema beyond `omdb_daily_usage`; no DEV-333 implementation (Q6, C1/C2; PC2; PLAN_CHALLENGE Compass M3/L2, Ledger M2).
- **FR-009**: Phase B verification and review per harness: Roslyn analyzers, cyclomatic complexity (then refactor threshold), JetBrains InspectCode, unit/integration/architecture/property/full suite, vulnerable packages, format, pre-PR mutation (`pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from a worktree). Exit 0 = Pass; exit 1 = Failure; exit 2 = SKIPPED (scope-empty) / SKIP (opt-out) / mutation NOT APPLICABLE — never PASS; Could not run is never Pass. Closing bar: no unresolved Critical/High/Medium; Lows explicitly fixed, accepted with rationale, or follow-up. Round cap two; Size M requires Sentry, Ledger, Compass. Do not lower thresholds, skip tests, or reclassify findings to manufacture clearance. No merge authorised by this spec (Q5, Q6).
- **FR-010**: After Gate 1 owner approval, Keel records the owner checkbox answer plus verified spec-PR merge receipt in `CONCLUSIONS.md` (T034). Then **verify only** (T035) that the merged approved Constitution is Version **1.7.0** with the Gate-1-approved **staged** Principle I / VII / VIII clauses and **that file’s** Sync Impact Report — **with no constitution edit at all, including no date edit**. Comparison target is the actual staged constitution artifact, **not** literal equality to the illustrative Sync Impact Report summary in `constitution-amendment-v1.7.0.md`. Record staged-vs-merge dates in `CONCLUSIONS.md`; a date-only difference is recorded, not fixed. Any substantive mismatch (version, Principle I/VII/VIII text, Sync Impact Report vs the staged constitution) or non-matching baseline → stop `blocked: structural — ticket-named port/types vs constitution`. ADR-0031 Accepted delivery is governed by CONCLUSIONS PC1/PC2: Keel changes status only in the **implementation PR**, after verified owner approval and before code merge; no write to `main`. Do not re-apply or re-stage the amendment in Phase B. Until Gate 1 is answered, noncompliant implementation is unauthorized. Owner rejection → `blocked: structural — ticket-named port/types vs constitution` (C1, C2; Governance; PC2 superseding I2 date-reconcile wording).

### Key Entities

- **Daily budget**: shared database-wide allowance for one UTC calendar date (TimeProvider).
- **Reservation**: committed increment authorising one outbound transport attempt.
- **`omdb_daily_usage`**: persisted counter row (`usage_date`, `request_count`) — ticket table name exact.
- **`IOmdbRequestGate` / `OmdbQuotaOptions`**: Core admission port and numeric daily limit options — ticket names exact.
- **`EfMetadataRequestGate` / `DailyRequestUsageRecord` / `DailyRequestUsageConfiguration` / `MetadataQuotaHandler` / `MetadataQuotaExhaustedException` / `MetadataQuotaPersistenceException`**: functional Infrastructure support types (PC1); one file per type; Infrastructure-only seam and failure discrimination; not part of the public API contract chain.
- Quota / admission / reservation remain local implementation vocabulary, not new Movies domain glossary terms (PC1; Q6).

## Success Criteria

- **SC-001**: Concurrent independent contenders cannot drive committed admissions above `DailyLimit` for a UTC date (ticket concurrent tests; Q1–Q2).
- **SC-002**: Exhaustion persists across process/gate recreation on the same database (ticket restart tests; Q2).
- **SC-003**: UTC date rollover charges a new date while retaining the prior row; retries across midnight sample per acquisition (ticket rollover tests; Q2, Q4).
- **SC-004**: Physical retries consume one reservation each; breaker/backoff/rate-limiter rejections above the handler do not; exact success counts are detail=1 and search+detail=2 (Q3, Q5).
- **SC-005**: Exhaustion maps to `RateLimited` and persistence failure to `ProviderUnavailable`, stopping OMDb variants while preserving chain fallback; both leave health state unchanged; later interruption discards partials; cancellation stays `OperationCanceledException` (Q3; PC2).
- **SC-006**: Startup rejects non-positive `DailyLimit`; default 1000; lowering denies without rewriting history (Q4).
- **SC-007**: Applicable analyzer/complexity/InspectCode/format/test/mutation gates pass or report SKIPPED/SKIP/N/A/Could not run distinctly — never as PASS (Q5, Q6).

## Assumptions

- No taste rulings; no `ASSUMPTIONS.md` entry required (brief).
- Recon baseline HEAD `f698aeceb6b6af50a397d076b83ed178566641b0` is planning authority only; Phase B pickup must refresh recon for main/contract drift (brief).
- WireMock and Testcontainers stand in for live OMDb and real PostgreSQL concurrency; no live OMDb network in tests (Q5).
- Consistent `DailyLimit` across instances sharing a database is an operational precondition, not a runtime protocol; no startup-log feature added (Q4; PLAN_CHALLENGE Sentry L2).
- One DB round trip per denied attempt is an accepted Q2 cost; no cache feature (PLAN_CHALLENGE Sentry M2).

## Clarifications

All grill categories were resolved by Patron Q1–Q6 before drafting; post-grill C1/C2/I1 correction supersedes Q6 only where it said no ADR or owner approval was needed; PC1/PC2 (c625ea29) supersedes earlier Infrastructure naming and I2 Phase B date-reconcile / same-delivery ADR publish wording. `/speckit-clarify` scan: Functional Scope, Domain/Data, Interaction, NFR (concurrency/resilience/security), Integration/Dependencies, and Out-of-scope — **Clear**. No `[NEEDS CLARIFICATION]` markers remain. One owner Gate 1 checkbox remains open (ADR-0031 + constitution 1.7.0); there is no present owner approval. Amendment staging remains Phase A / this spec PR; Phase B is verify-only with no constitution date edit.

## Traceability

| Source | Encoded as |
|---|---|
| Ticket counter/`IOmdbRequestGate`/`OmdbQuotaOptions`/wiring/tests | Scope; US1–US4; FR-001–FR-007; SC-001–SC-006 |
| Q1 shared budget / UTC / no bypass | US1; FR-001–FR-004; SC-001 |
| Q2 atomic contract / no refund / retention | US2; FR-004; Edge Cases |
| Q3 handler after resilience / two signals / mapping | US3; FR-005–FR-006; SC-004–SC-005 |
| Q4 options/schema/limit changes | US4; FR-002–FR-003; SC-006 |
| Q5 sequence and evidence | US5; FR-007; FR-009 |
| Q6 frozen scope / loop / IOptions snapshot (ADR path superseded by C1/C2) | Scope; FR-002, FR-008–FR-010; Owner question |
| C1/C2 ADR-0031 + constitution 1.7.0 (I/VII/VIII) | Owner question; Scope; FR-001–FR-002, FR-008, FR-010; plan Constitution Check |
| I1 fixture sequencing with step | US5; FR-007; plan Ordering; tasks Dependencies/MVP |
| PC1 functional support names / no wider VIII exception | Scope; FR-003–FR-006; Key Entities; plan Structure |
| PC2 health/partials/token/logs/T034–T035 verify path | US3; FR-004–FR-006, FR-008, FR-010; Edge Cases |
| PLAN_CHALLENGE accepted closures | DRAFTING_RECEIPT remediation map; FR/US/task updates above |
| recon-DEV-426 funnel, DI tension, ApplicationTables, 9.1–9.2 seam | FR-003–FR-007; plan Approach / Structure |
