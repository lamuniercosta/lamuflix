# DEV-426 grill conclusions

Append-only decision record. Grill cap: 12 questions. Source facts: recon-DEV-426; ticket scope: note DEV-426:11; product authority: specs/PRODUCT.md. Decision maker: Patron. Recorder: Keel.

---

## Q1 — Budget boundary

**Keel question:** What is the budget boundary? Recommend one shared persisted budget per configured database for all OMDb attempts, default 1000 per UTC calendar date, with positive configurable daily limit and no key storage or bypass flag. All callers including eventual bulk scans share it; UTC TimeProvider date determines rollover. Cost: separate API keys sharing a database share the conservative budget. Confirm or rule otherwise without changing ticket scope.

**Patron answer, rationale, basis, and implications (verbatim):**

[from Patron] DEV-426 Q1 ruling: CONFIRMED — one shared persisted budget per configured database for all OMDb attempts.

Verdict:
- Boundary: a single persisted daily counter in the ticket-named `omdb_daily_usage` table, scoped to the configured database, shared by every OMDb attempt from every caller and process using that database, including eventual DEV-333 bulk scans. No per-caller, per-process, per-instance, or per-key partition.
- Limit: default 1000 attempts per UTC calendar date; `OmdbQuotaOptions` daily limit must be positive and validated at startup (zero/negative rejected). No key storage, no key-derived partitioning, and no bypass flag.
- Counting point and rollover: every outbound OMDb attempt counts (each search page and each detail/by-id fetch is a separate attempt through the single `FetchAsync` funnel); rollover is by UTC date from the injected `TimeProvider`, never local time or `DateTime.Now`.
- Accepted cost: separate API keys sharing one database share the conservative budget. This is accepted and does not change ticket scope; a per-key budget would require key material or a key-derived identity in persisted state and a second partitioning the ticket does not name.

Cited basis:
- Ticket DEV-426 (note DEV-426:11; recon-DEV-426:5): persisted daily OMDb request budget (1,000/day free tier), prerequisite for DEV-333, deliverables counter table `omdb_daily_usage`, `IOmdbRequestGate`, `OmdbQuotaOptions`, wiring in `OmdbMetadataProvider`, concurrent/restart/rollover tests. Persisted in the configured database is what makes the budget survive restart and be shared under concurrency, which those named tests presuppose.
- recon-DEV-426:11-12,19: `FetchAsync` at OmdbMetadataProvider.cs:204 is the single HTTP funnel for all details/search/by-id calls, fallback search pages and detail fetches are separate OMDb requests, `TimeProvider` is already injected, and concurrency arrives from RabbitMQ consumers — so counting at that funnel with a persisted counter is the only boundary that covers all callers.
- specs/PRODUCT.md section 3: a ticket's Scope & Technical Design is authoritative and Patron cites it and moves on; section 5: this structural item is Patron's to rule with cited basis, not an owner checkbox. recon-DEV-426 section 7 items 3 and 6: `omdb_daily_usage` is the ticket's own table (no schema escalation) and `OmdbQuotaOptions` holds only numeric/day settings with the ApiKey already env/secret-managed (no new secret handling).

Scope note: nothing in this ruling adds a route, DTO, table beyond `omdb_daily_usage`, dependency, or bypass; Keel records it append-only in specs/DEV-426/CONCLUSIONS.md.


---

## Q2 — Atomic admission and cancellation

**Keel question:** What is the atomic admission contract? Recommend Core IOmdbRequestGate.TryAcquireAsync(CancellationToken) returning Task<bool>, true only after a committed atomic conditional insert-or-increment in PostgreSQL for TimeProvider UTC date; date primary key and nonnegative integer count in omdb_daily_usage. All contenders including distinct contexts/processes compete on the DB constraint; never read-then-write or process lock. Reserve before dispatch, never refund after timeout, cancellation after acquisition, HTTP failure or crash; pre-cancelled acquisition propagates cancellation without dispatch. UTC date is sampled once per acquisition; old rows retained, no cleanup job. Cost: conservative lost reservations, one database round trip per attempt, retained one row/day. Confirm or refine with cited basis.

**Patron answer, rationale, basis, and implications (verbatim):**

[from Patron] DEV-426 Q2 ruling: CONFIRMED with one refinement on cancellation — Core `IOmdbRequestGate.TryAcquireAsync(CancellationToken)` returning `Task<bool>` is the atomic admission contract.

Verdict:
- Contract: `TryAcquireAsync` returns `true` only after exactly one reservation has been committed by a single atomic conditional insert-or-increment statement in PostgreSQL for the sampled UTC date. It returns `false` only when the daily limit has been reached and no increment was committed. Quota exhaustion is `false`, not an exception.
- Atomicity: `omdb_daily_usage` has the UTC calendar date as primary key and a nonnegative integer count, constrained so a committed count never exceeds the configured positive limit. All contenders — distinct DbContexts, processes, and hosts sharing the configured database — compete only on that database constraint, e.g. an insert-or-increment whose increment is conditional on count < limit in the same statement. Never read-then-write, never a process-local lock, semaphore, or in-memory counter as the authority.
- Sampling: the gate samples the injected `TimeProvider` UTC date exactly once per acquisition and uses that one date for the whole statement, so an acquisition straddling UTC midnight is charged wholly to the sampled date and never split or double-counted. The configured limit is likewise read once per acquisition and applied inside the atomic predicate.
- Reserve-before-dispatch, no refunds: the caller dispatches to OMDb only after `true`. Once `true` is returned the reservation is spent and is never refunded — not on downstream timeout, HTTP failure, cancellation after acquisition, or process crash. This conservative loss is accepted.
- Cancellation refinement: a token already cancelled before acquisition propagates `OperationCanceledException` (via the token) with no database increment and no dispatch. Cancellation while the atomic statement is in flight also propagates cancellation; in that case no `true` has been returned, so the caller must not dispatch, and the reservation exists only if the statement in fact committed before cancellation took effect — the caller cannot observe or reuse it either way. Only a returned `true` authorises exactly one dispatch.
- Retention: prior dates' rows are retained; there is no cleanup job, pruning, or retention window in this ticket. Cost accepted: one database round trip per OMDb attempt, conservative lost reservations, and one small row per UTC day.

Cited basis:
- Ticket DEV-426 (note DEV-426:11): persisted daily budget with counter table `omdb_daily_usage`, `IOmdbRequestGate`, and concurrent/restart/rollover tests — persistence plus DB-level atomicity is the only reading under which the named concurrency and restart tests are meaningful across processes.
- recon-DEV-426:19,51-52: concurrency arrives from parallel RabbitMQ consumers, and the repo's concurrency exemplar is PostgreSQL-backed (`EfMovieRepositoryClaimConcurrencyTests`); a process-local lock cannot arbitrate that traffic. recon-DEV-426:11-12: `FetchAsync` is the single OMDb funnel and `TimeProvider` is already injected, with `DateTime.Now` banned.
- specs/PRODUCT.md section 3 (ticket Scope & Technical Design is authoritative) and section 5 (Patron rules on structural items with cited basis, recorded in CONCLUSIONS.md); recon-DEV-426 section 7 item 3: `omdb_daily_usage` is the ticket's own table, so its key/constraint shape is in scope and no further schema is authorised — a cleanup job or second table would exceed it.

Scope note: no result DTO, exception-for-exhaustion, refund/reaper, or cleanup mechanism is added; Keel records this append-only in specs/DEV-426/CONCLUSIONS.md.


---

## Q3 — Physical attempts, handler lifetime, failure mapping

**Keel question:** recon-DEV-426 sections 9.1-9.2 now prove FetchAsync counts logical calls only; automatic retries run below it. Recommend an OMDb-only DelegatingHandler registered AFTER AddStandardResilienceHandler, opening a fresh async DI scope per SendAsync through IServiceScopeFactory, resolving scoped IOmdbRequestGate and disposing scope before transport. Each physical attempt needs its own committed reservation; breaker/backoff/rate-limiter rejections above it spend none. On false return a distinguishable internal local-quota signal that is NOT HttpRequestException, TimeoutRejectedException or a retryable HTTP status; provider maps it to existing RateLimited, ending OMDb fallback immediately while preserving normal chain fallback. On gate persistence failure fail closed with distinct non-retried internal signal mapped to ProviderUnavailable; propagate caller cancellation. Cost: small internal signal types plus handler, no new public API/dependency/layer; FetchAsync remains mapping boundary. Confirm approach and failure semantics with cited basis.

**Patron answer, rationale, basis, and implications (verbatim):**

[from Patron] DEV-426 Q3 ruling: CONFIRMED — per-physical-attempt admission sits in an OMDb-only DelegatingHandler inside the resilience pipeline, with the two-signal failure semantics below.

Verdict:
- Placement: register the quota `DelegatingHandler` via `AddHttpMessageHandler` AFTER `AddStandardResilienceHandler()` on the OMDb typed-client registration only (not TMDB, not Ollama). That puts it inside the retry/attempt-timeout strategies, so its `SendAsync` runs exactly once per physical outbound attempt: 1 + MaxRetryAttempts on retryable failure, 1 on success or non-retryable response, 0 when the rate limiter, total timeout (including during backoff), or open circuit breaker rejects first — those rejections spend no reservation.
- Admission: each `SendAsync` first resolves a scoped `IOmdbRequestGate` from a fresh async scope (`IServiceScopeFactory.CreateAsyncScope()`, the EnrichmentConsumer pattern), calls `TryAcquireAsync`, and disposes the scope before any transport call and on every exit path. The handler is constructed only with the singleton `IServiceScopeFactory`, captures no scoped `DbContext`/gate instance, and holds no per-request mutable state. Only a `true` (committed reservation per Q2) proceeds to `base.SendAsync`; each retry attempt must win its own new reservation. This refines Q1/Q2: a logical `FetchAsync` call is not the unit of account; every physical dispatch is.
- Quota exhausted (gate returns false): the handler throws one distinguishable internal, non-public signal exception (sealed, Infrastructure-internal). It must NOT be `HttpRequestException` or `TimeoutRejectedException`, must NOT be a synthesised HTTP response/status (so retry's 408/429/5xx handling and the circuit-breaker predicate never engage and no retry occurs), and must carry no API key or request URI. `FetchAsync` remains the mapping boundary: it catches this signal separately and returns the existing `RateLimited` failure, which ends the OMDb fallback loop immediately (no further search variants are attempted) while the chained provider's normal fallback to the next provider proceeds exactly as for an OMDb 429.
- Gate persistence failure (the gate itself faults, e.g. database unreachable): fail closed — no transport call is made. The handler wraps it in a second, distinct internal non-retried signal (original exception preserved as inner exception) that is likewise neither `HttpRequestException` nor `TimeoutRejectedException` nor a fake status. `FetchAsync` maps it to the existing `ProviderUnavailable` failure, again ending OMDb fallback immediately and preserving normal chain fallback. Exhaustion and persistence failure remain distinguishable end-to-end (different signal, different mapped category, different log event).
- Cancellation: caller cancellation propagates unwrapped as `OperationCanceledException` from gate or transport and is never converted into either signal, never retried by the handler, and never mapped to `RateLimited`/`ProviderUnavailable`. If cancellation lands after a reservation committed but before/during transport, the reservation is spent (Q2 no-refund rule).
- Tests must lock the seam, not just the intent: persisted row counts of 1+MaxRetryAttempts on always-429 and always-500, 2 on 503-then-200, unchanged count when the breaker is open, and false-gate producing zero transport requests with a `RateLimited` result and no further fallback variants.

Cited basis:
- recon-DEV-426 9.1 (lines 76-81): handler chain position equals registration order; strategies run rate limiter -> total timeout -> retry -> circuit breaker -> attempt timeout; one logical request is 1 + at most 3 sequential retries (no hedging), proven by WireMock counts in MetadataProviderResilienceTests (always-429/always-500 = 1+MaxRetryAttempts, 503-then-200 = 2, open breaker = 0 new requests). recon 9.2 (line 85-86): `FetchAsync` sees only logical calls, so counting there would miss retries; the after-resilience `AddHttpMessageHandler` position is the seam that sees each physical attempt.
- recon-DEV-426 9.2 (lines 87-88): the handler pipeline is cached across handler lifetime while `IOmdbRequestGate`'s EF backing is scoped, so scoped injection into the handler is a capture defect; the repo's established remedy is singleton `IServiceScopeFactory` + per-unit-of-work async scope (EnrichmentConsumer.cs:24,235; FeaturesConsumer.cs:25,237). No `IDbContextFactory` registration exists today, so scope-per-attempt is the pattern that needs no new registration machinery.
- Ticket DEV-426 (note DEV-426:11) names `IOmdbRequestGate` wiring in `OmdbMetadataProvider` and the persisted budget; retry attempts are OMDb requests against the same 1,000/day free-tier budget, so exempting them would silently overspend the named budget by up to 4x. specs/PRODUCT.md sections 3 and 5: ticket scope is authoritative and Patron rules on the structural seam; recon section 7 confirms no new dependency, layer, public API, or secret handling is introduced — both signals are internal types in the existing Infrastructure layer and the DTO -> OpenAPI -> TypeScript chain is untouched.

Scope note: no new public API, DTO, NuGet package, layer, or retry-policy change; the resilience configuration and its ShouldHandle predicates are not modified. Keel records this append-only in specs/DEV-426/CONCLUSIONS.md.


---

## Q4 — Configuration, schema, and changed limits

**Keel question:** Settle exact configuration/schema and limit changes. Recommend OmdbQuotaOptions section Omdb:Quota with DailyLimit int default 1000, startup positive validation, no enable flag/date setting/key; bind in ServiceDefaults like existing options. Table omdb_daily_usage uses usage_date PostgreSQL date PK and request_count integer NOT NULL CHECK >=0, mapped record/config plus generated EF migration/snapshot. The dynamic configured limit belongs in atomic SQL predicate, NOT a schema CHECK hardcoded to 1000. Lowering the limit on restart below an already-spent count denies further admissions that date without resetting or clamping history; count can therefore exceed a newly lowered limit, refining Q2 wording. Consistent configuration across instances is operational precondition; no distributed config protocol. Preserve OmdbOptions and TMDB match settings. Cost: conservative denial after lowering; no automatic reconciliation of mismatched host settings. Confirm or refine with cited basis.

**Patron answer, rationale, basis, and implications (verbatim):**

[from Patron] DEV-426 Q4 ruling: CONFIRMED with the Q2 invariant restated for limit changes — configuration and schema settle exactly as recommended below.

Configuration:
- New `OmdbQuotaOptions` record, section `Omdb:Quota`, with a single setting `DailyLimit` (int, default 1000). Bound and validated in ServiceDefaults (`AddLamuFlixOptions`/`BindAndValidate`) exactly like the existing options records; startup validation rejects a non-positive `DailyLimit` and the host fails to start rather than running ungated or unlimited.
- No enable/disable flag (a flag would be the bypass Q1 forbids), no configurable date/timezone setting (rollover is always the TimeProvider UTC date per Q1/Q2), and no API key or key-derived setting (keys stay env/secret-managed; the quota options record holds only the numeric limit).
- `OmdbOptions` is not modified and TMDB's use of it (match threshold) is untouched: quota lives in its own ticket-named record so the OMDb/TMDB coupling recon flagged does not grow.

Schema:
- Table `omdb_daily_usage` with `usage_date` PostgreSQL `date` as primary key and `request_count` `integer NOT NULL CHECK (request_count >= 0)`. Modelled as an EF record + configuration in the existing Records/Configurations pattern (snake_case names), delivered by a generated EF migration and regenerated model snapshot (never hand-edited), and added to the hardcoded `ApplicationTables` list in MigrationTests.
- There is deliberately NO schema CHECK tying `request_count` to the limit (certainly not a hardcoded 1000). The limit is dynamic configuration, enforced only as a parameter of the atomic admission statement: one acquisition reads the current `DailyLimit` once and the insert-or-increment commits only while `request_count < DailyLimit` in that same statement.

Limit changes (this restates Q2):
- Q2's invariant is refined to: no admission commits an increment that exceeds the DailyLimit in effect for that acquisition. Historical counts are never reset, clamped, or rewritten by a configuration change.
- Lowering the limit on restart below the count already spent for the current UTC date denies all further admissions that date (the predicate `request_count < DailyLimit` is simply false); the stored count may therefore exceed a newly lowered limit as a historical fact, which the schema must and does permit.
- Raising the limit resumes admissions for that date up to the new limit once a process is running with the new value; spent count is carried, not restarted.
- A changed limit takes effect per process when that process starts/reloads its configuration — there is no distributed configuration protocol, push, or cross-instance reconciliation in this ticket. Running all instances that share a database with the same `DailyLimit` is an operational precondition, recorded as such in the spec; with mismatched values the shared atomic predicate still prevents lost updates, but the effective ceiling is whichever running instance's limit admits, and that inconsistency is an operator error, not something the gate detects or repairs.
- Accepted cost: conservative denial for the rest of the UTC date after a lowering, and no automatic reconciliation of mismatched host settings.

Cited basis:
- Ticket DEV-426 (note DEV-426:11) names `OmdbQuotaOptions` and the `omdb_daily_usage` counter table with the 1,000/day free-tier budget, and Q1/Q2 (specs/DEV-426/CONCLUSIONS.md) fixed a positive configurable limit, no bypass or key storage, UTC TimeProvider rollover, and atomic conditional admission — a schema-level limit CHECK would hardcode what those rulings make dynamic and would break the lowering case.
- recon-DEV-426:23-27: `OmdbOptions` (OmdbOptions.cs:8-22) is validated via `BindAndValidate` in ServiceDefaults Extensions.cs:80-85, where a new options record slots into the existing list; recon:20 — TMDB injects `IOptions<OmdbOptions>` for its match threshold, so quota settings must not be folded into `OmdbOptions`.
- recon-DEV-426:36-38,52: new records/configurations are auto-discovered by `ApplyConfigurationsFromAssembly` in the OutboxMessage record/configuration shape, migrations are EF-generated with a regenerated snapshot, and `MigrationTests.ApplicationTables` (MigrationTests.cs:24-37) must gain `omdb_daily_usage` for the migration guards to pass.
- specs/PRODUCT.md sections 3 and 5: ticket Scope & Technical Design is authoritative and Patron rules on structural items with cited basis; recon section 7 item 3 — `omdb_daily_usage` is the ticket's own table, so this column/key shape is in scope and no second table or column elsewhere is authorised.

Scope note: no appsettings ApiKey, no OmdbOptions/TMDB change, no schema CHECK on the limit, no distributed-config or reconciliation mechanism. Keel records this append-only in specs/DEV-426/CONCLUSIONS.md, including the express refinement of Q2's count-vs-limit wording.


---

## Q5 — Implementation order and test evidence

**Keel question:** Approve the implementation/test sequence for the brief? Recommend (1) Core port/options and startup validation, (2) EF record/configuration/DbSet/gate plus scoped registration and generated migration/snapshot, (3) OMDb-only handler/signals registration and FetchAsync mapping/log events, (4) adapt provider factories/composition fixtures and migration metadata guards, (5) end-to-end verification. Real PostgreSQL tests must prove exact N admissions for >N concurrent contenders using independent scopes, persisted exhaustion across recreated gate/context, first-row race, UTC midnight boundary, retained prior dates, lowered/raised limits, pre-cancellation and DB failure with zero dispatch. Shipped HttpClient factory plus WireMock must prove one count per success/search/detail and each 429/500 retry, 503-then-200=2; near-limit retry exhaustion stops retries/OMDb variants, chain fallback remains; open breaker and backoff cancellation spend no extra reservation; caller cancellation remains unwrapped and no refunds after an acquired failure. Unit tests cover option boundaries, mappings, scope disposal/capture safety; no mocked EF query translation. Existing provider, resilience, chain, startup and migration regressions remain required. Cost: integration-heavy tests because concurrency and handler ordering cannot be established by mocks. Confirm or refine with cited basis.

**Patron answer, rationale, basis, and implications (verbatim):**

[from Patron] DEV-426 Q5 ruling: CONFIRMED with two refinements — tests land with each step (step 5 is full verification, not the first test run), and composition fixtures must isolate quota state so existing suites stay deterministic.

Approved sequence for the brief:
1. Core: `IOmdbRequestGate` port in Core/Ports and `OmdbQuotaOptions` (`Omdb:Quota`, `DailyLimit` default 1000) bound/validated in ServiceDefaults. Unit tests with this step: option boundaries (0 and negative rejected at startup validation, 1 and default 1000 accepted), and the port/options compile against Core alone (architecture tests: no EF/DI references leak into Core).
2. Persistence: EF record/configuration/DbSet for `omdb_daily_usage` (usage_date date PK, request_count integer NOT NULL CHECK >= 0 per Q4), scoped `IOmdbRequestGate` registration alongside the other EF-backed scoped ports, atomic conditional insert-or-increment gate implementation, and the EF-generated migration + regenerated snapshot (never hand-edited) with `omdb_daily_usage` added to MigrationTests.ApplicationTables. Tests with this step are real-PostgreSQL (Testcontainers/PostgresFixture) only — no mocked DbContext/EF translation for any behaviour that depends on the atomic SQL.
3. Transport seam: OMDb-only quota `DelegatingHandler` registered after `AddStandardResilienceHandler()`, the two internal signal exceptions, `FetchAsync` mapping to the existing `RateLimited`/`ProviderUnavailable` failures with distinct log events. Unit tests with this step: FetchAsync signal mappings and fallback-loop termination, handler scope behaviour (fresh scope per SendAsync, scope disposed before transport runs, no scoped instance captured/reused across attempts), handler built only via `IServiceScopeFactory`.
4. Fixture adaptation: provider test factories, composition/startup fixtures, WireMock end-to-end factories, and migration metadata guards are updated so the shipped pipeline runs with the gate present — fixtures configure an explicit test `DailyLimit` and isolate/reset the usage row per test class/database so pre-existing counts can never make an unrelated regression test pass or fail. Existing provider, resilience, chain, startup, options, health-registration, and migration regressions all stay in force and must remain green.
5. End-to-end verification: the full targeted suites below, then the repo's normal gates on the changed files (Roslyn analysers, cyclomatic complexity, InspectCode, `dotnet test`); a gate that could not run is reported as such, never as a pass.

Required proof (integration-heavy by necessity; mocks cannot establish concurrency or handler ordering):
- Real PostgreSQL gate tests: exactly N admissions when more than N contenders race through independent scopes/contexts (synchronised start), first-row race on an empty table creates one row with the exact count, exhaustion persists across a recreated gate/context on the same database, UTC midnight boundary charges two dates separately with the prior date's row retained and never pruned, lowered limit denies without resetting/clamping and raised limit resumes against the carried count, pre-cancelled token throws OperationCanceledException with no increment, and a database failure admits nothing.
- Shipped-factory + WireMock tests (built through the real `AddMetadataProviders` composition, never a hand-built handler pipeline): one persisted count per successful request and per search/detail logical call, 1+MaxRetryAttempts counts on always-429 and always-500, 2 on 503-then-200; near-limit exhaustion mid-retry-sequence produces no further transport request, stops further OMDb fallback variants, returns RateLimited, and the chained provider still falls through to the next provider; open circuit breaker and cancellation during retry backoff each spend no additional reservation; caller cancellation surfaces unwrapped as OperationCanceledException rather than a mapped failure; and an acquired reservation is never refunded when the ensuing attempt fails or is cancelled.

Cited basis:
- Q1-Q4 rulings (specs/DEV-426/CONCLUSIONS.md): shared persisted per-database budget, atomic DB admission, per-physical-attempt counting inside the resilience handler, Omdb:Quota/DailyLimit and the omdb_daily_usage shape — this sequence is exactly those decisions in dependency order (Core port before its EF implementation, model before generated migration, gate before handler, handler before fixture adaptation).
- recon-DEV-426:30-38: ports live in Core/Ports with EF implementations in Infrastructure, EF ports register scoped (PersistenceServiceCollectionExtensions.cs:62-70), records/configurations are auto-discovered, and migrations/snapshots are EF-generated; recon:52 — MigrationTests.ApplicationTables must gain the new table; recon 9.2:87-88 — scope-per-attempt via IServiceScopeFactory is the no-capture pattern the handler must follow.
- recon-DEV-426:41-52,79: the existing unit/integration/provider/resilience suites and the shipped-composition WireMock probe (MetadataProviderProbe.BuildServices through AddMetadataProviders, counting physical wire requests) are the regression base and the only honest way to prove handler ordering; repo non-negotiable (AGENTS.md): never mock the data-access driver for behaviour depending on real query translation — use Testcontainers/real PostgreSQL.

Scope note: this approves the brief's implementation/test sequence only; it adds no step, artefact, dependency, or test category beyond Q1-Q4 and the ticket's named concurrent/restart/rollover tests. Keel records this append-only in specs/DEV-426/CONCLUSIONS.md.


---

## Q6 — Frozen scope, loop terms, and shared understanding

**Keel question:** Settle loop terms, frozen file scope and care-list authorization. Recommend closing bar: no unresolved Critical/High/Medium findings and all applicable required gates have valid evidence; Low findings get explicit dispositions. Cap two review/remediation rounds, then stop blocked and route residual work, never weaken gates. M review needs Sentry/Ledger/Compass; no review clearance from planning. Frozen scope: ticket quota port/options, one table/migration, EF admission adapter/record/config/DbSet, OMDb handler/two signals and provider mapping/logging, scoped persistence/typed-client/options registrations, directly affected tests/factories/migration metadata and test-only explicit quota configuration. Authorize narrowly scoped edits in recon-named existing files needed for this integration; no whole-file rewrite or deletion. No new dependency/project/top-level folder/layer, extra schema, public API/UI, LocalPlay/Process.Start/secrets, cleanup, per-key budgets, retry-policy redesign or DEV-333 bulk-scan implementation; anything else is a follow-up issue, not a finding in this round. Expect analyzer/complexity/InspectCode, unit/integration/architecture/property/full-suite/security/format and pre-PR mutation gates per harness; preserve exit-2 scope-empty/opt-out/N/A accurately. Phase A only produces docs; no gates claimed run. No new domain term or ADR needed for this M ticket; quota terminology is defined in brief without rewriting root glossary. Configuration uses startup IOptions snapshot, no hot-reload feature. Confirm these closing decisions and shared understanding, or identify one remaining issue.

**Patron answer, rationale, basis, and implications (verbatim):**

[from Patron] DEV-426 Q6 ruling: CONFIRMED in full — closing bar, round cap, frozen scope, and care-list authorisation settle as below; no remaining issue.

Closing bar and review loop:
- The work is done when there are no unresolved Critical, High, or Medium findings and every applicable required gate has valid, current evidence. Each Low finding carries an explicit recorded disposition (fixed, accepted with rationale, or routed as follow-up) in the closing brief/CONCLUSIONS — no silent Lows.
- At most two review/remediation rounds. Each round is one full M-tier review (Sentry, Ledger, and Compass all reporting; planning or drafting output is not review clearance and clears nothing) plus remediation of its findings. If anything above Low remains after round two, work stops as blocked and the residual is routed for Patron disposition (follow-up ticket via Rigger or an owner escalation only if it meets the section 2.3 tests) — it is never cleared by weakening a gate, lowering a threshold, skipping a test, or reclassifying a finding.
- Expected gates for the implementation/PR phase: Roslyn analysers, cyclomatic complexity, JetBrains InspectCode, unit/integration/architecture/property tests, the full suite, security (vulnerable packages), format, and the pre-PR mutation gate, per the harness pipeline. Exit codes are reported exactly as the harness defines them: 0 pass, 1 fail, 2 reported as SKIPPED (scope-empty), SKIP (opt-out), or mutation NOT APPLICABLE — never folded into PASS; a gate that could not run is reported 'Could not run'. This Phase A (spec) phase produces documents only and claims no implementation gate evidence.

Frozen scope (authorised, narrowly scoped edits in the recon-named existing files only; no whole-file rewrite or deletion of any tracked file):
- New: Core port `IOmdbRequestGate` and `OmdbQuotaOptions`; EF record, configuration, gate adapter, and handler plus its two internal signals for `omdb_daily_usage`.
- Edited in place: DbSet addition on the DbContext, scoped gate registration in persistence DI, OMDb typed-client handler registration, provider `FetchAsync` mapping and its log events, `OmdbQuotaOptions` binding/validation in ServiceDefaults, the generated EF migration/snapshot, directly affected tests/factories/fixtures, migration metadata guards, and test-only explicit quota configuration.
- Configuration note refining Q4: the gate consumes a startup `IOptions` snapshot — there is no hot-reload (`IOptionsMonitor`) feature; a `DailyLimit` change takes effect on process restart, which is exactly the restart semantics Q4 ruled for raised/lowered limits.
- Care-list (charter section 2.3) authorisation: this ruling is the deliberate Patron authorisation for the frozen scope above. Confirmed not triggered: no new NuGet/npm dependency, no new project/top-level folder/layer, no schema beyond the ticket's `omdb_daily_usage`, no public API/UI/OpenAPI change, nothing touching Features:LocalPlay, Process.Start, or secrets, no per-key budget, no cleanup job, no retry-policy redesign, and no DEV-333 bulk-scan implementation. Anything outside the frozen scope is a follow-up issue, not a finding to absorb into this round.
- Documentation: no new domain term and no ADR for this M ticket; quota terminology is defined locally in the brief and root glossary/context files are not rewritten.

Cited basis:
- Ticket DEV-426 (note DEV-426:11) and recon-DEV-426 section 7 (six care-list items checked: no new dependency, ticket-named port/options in existing layers, schema limited to the named table, no public API, no LocalPlay/Process.Start overlap, secrets already env-managed) — the frozen scope is precisely the recon-named file set those items were checked against.
- specs/PRODUCT.md section 5: Patron decides structural/care-list items with a cited ruling recorded in CONCLUSIONS.md; only a ticket-text contradiction or a constitution departure escalates to the user, and neither arises here. Harness non-negotiables (repo AGENTS.md): never lower a gate threshold to make a gate pass, a surviving mutant is a missing test (fix the test, never the threshold), and exit-2 gate outcomes are SKIPPED/SKIP/NOT APPLICABLE, never PASS.
- Q1-Q5 rulings (specs/DEV-426/CONCLUSIONS.md): every artefact in the frozen scope traces to a prior ruling; the IOptions-snapshot note above is the only refinement and it narrows, not widens, Q4.

Shared understanding: Keel closes the grill on these terms, appends this ruling to specs/DEV-426/CONCLUSIONS.md, and writes the closing brief.md inside the frozen scope. No remaining open issue from Patron.

---

## C1/C2 — Constitution prerequisites from speckit-analyze (post-grill correction)

[from Patron] DEV-426 C1/C2 ruling: AMEND, do not waive and do not halt as blocked: structural. Draft ADR-0031 plus one constitution MINOR amendment 1.6.0 -> 1.7.0 in the DEV-426 spec PR; owner approval is the Gate 1 spec-PR merge (add it as a Gate 1 checkbox). If owner rejects the amendment, this converts to blocked: structural — ticket-named port/types vs constitution. This supersedes Q6's no ADR needed wording above; Q6 history is not rewritten.

- C1: constitution I (`.specify/memory/constitution.md:192-196`) closed port list, Adding a port requires an ADR; Governance (`:597-602`, `:609-612`) amendment procedure and simultaneous-amendment compliance; precedents 1.4.0/ADR-0024 and 1.5.0/ADR-0029. ADR-0031 records `IOmdbRequestGate`; amendment adds it to the I closed list; `PortWhitelistPattern` follow-up already in plan.
- C2: constitution VIII (`:351-356`) vendor-name exceptions and VII (`:324-326`) options-record list; ticket (note DEV-426:11) explicitly names `IOmdbRequestGate` and `OmdbQuotaOptions`, so names are ticket-decided and not renamed; 1.6.0/`TmdbOptions` precedent (`:7-16`). Same amendment adds `OmdbQuotaOptions` to VII and VIII, and `IOmdbRequestGate` to VIII.
- Keel/Quill update `spec.md`/`plan.md` Constitution Check to cite I/VII/VIII plus ADR-0031 and the 1.7.0 amendment; no other constitution text changes.

---

## I2 — Constitution 1.7.0 staging (speckit-analyze residual, post-remediation)

[from Patron] DEV-426 I2 ruling: I2 is valid and retained; YES, stage the proposed 1.7.0 text into `.specify/memory/constitution.md` on `feature/426-spec` for the spec PR — owner approval remains the Gate 1 spec-PR merge, with the owner checkbox unchecked until then, and Phase B verifies the merged approved artifact instead of applying it.

- Basis: C1/C2 ruling above (`CONCLUSIONS.md:178`) places the amendment in the DEV-426 spec PR with approval via Gate 1 merge; Governance (`.specify/memory/constitution.md:597-602`) requires the version bump, Sync Impact Report, and template review before merging, and Compliance (`:609-612`) requires a simultaneously submitted and approved amendment — deferring the actual `constitution.md` edit to Phase B (current `constitution-amendment-v1.7.0.md:8,108`; `tasks.md` T034/T035) splits approval from application and contradicts that ruling.
- Staging scope (Conductor-assigned operator, Rigger per Keel recommendation; `brief.md:102` reserves constitution edits to that operator): apply only the exact I/VII/VIII, Version 1.7.0, Sync Impact Report, and Last Amended text already specified in `constitution-amendment-v1.7.0.md`; no unrelated constitution text, no production code, ADR-0031 stays Proposed, and the Gate 1 checkbox in `spec.md` stays unchecked — staging on the branch claims no present owner approval.
- Consequential doc fix: Keel/Quill rewrite T034/T035 and every Phase B apply-only reference as verify-only (confirm the merged constitution is 1.7.0 with the staged I/VII/VIII text and reconcile the apply date if the merge date differs; any other mismatch or a non-1.6.0 baseline stops as `blocked: structural — ticket-named port/types vs constitution`); `constitution-amendment-v1.7.0.md` remains the proposal record.

