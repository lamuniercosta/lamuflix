# DEV-426 — Closing grill brief

Status: grill closed by Patron at Q6; 6 of the maximum 12 questions used. This is Phase A planning authority for Quill to draft spec.md, plan.md and tasks.md. It is not Gate 1 approval, implementation completion, review clearance, or a merge bar.

## Authority and identity

- Worktree: F:/Dev/LamuFlix.worktrees/feature-426-spec; branch: feature/426-spec.
- Recon baseline: f698aeceb6b6af50a397d076b83ed178566641b0, as recorded in recon-DEV-426:4,70; not a new HEAD measurement.
- Ticket authority: note DEV-426:11, quoted by recon-DEV-426:5: persisted daily OMDb request budget (1,000/day free tier), prerequisite for DEV-333; table omdb_daily_usage, IOmdbRequestGate, OmdbQuotaOptions, provider wiring, concurrent/restart/rollover tests.
- Product authority: specs/PRODUCT.md sections 3 and 5. Patron rules; Keel records. Full questions, answers, cited rationale and implications are preserved verbatim in CONCLUSIONS.md, Q1-Q6. Facts come exclusively from recon-DEV-426, including sections 9.1-9.2. No production searches or new measurements were used for the grill.
- Patron settled C1/C2 through a proposed constitution amendment, with owner approval pending at Gate 1; see the post-grill correction below. No taste ruling was made, so no ASSUMPTIONS.md entry is required. No new domain term is required; root context/glossary files remain outside scope. ADR-0031 is required, superseding Q6 on this point.

## Settled terminology

A daily budget is the shared database-wide allowance for one UTC calendar date. A reservation is a committed increment authorising one outbound transport attempt. A logical lookup or FetchAsync call may contain multiple transport attempts through automatic retries. Exhaustion means admission returned false; persistence failure means the gate could not establish admission. These are local descriptions of the feature, not new domain glossary entries.

## Every grill answer and plan decision

### Q1 — Boundary

One shared persisted budget per configured database, across callers, scopes, processes and hosts, including future bulk-scan callers. Default 1000 per UTC calendar date; DailyLimit must be positive. No bypass flag, API-key storage or key-derived partition. Separate keys sharing the database conservatively share the same budget. TimeProvider determines UTC date. Q3 refines the original FetchAsync counting proposal: the unit is each transport attempt, including retries.

### Q2 — Atomic contract and cancellation

Core port IOmdbRequestGate.TryAcquireAsync(CancellationToken) returns Task<bool>. True means exactly one reservation committed; false means exhausted without an increment. Use one parameterised PostgreSQL conditional insert-or-increment, not a read-then-write or an in-memory/process lock. Sample UTC date and configured limit once per acquisition. Contenders from independent contexts arbitrate through the database primary key and atomic predicate.

Reserve before transport and never refund after HTTP failure, timeout, cancellation or crash. A pre-cancelled token throws OperationCanceledException with no increment or dispatch. Cancellation in flight propagates; a statement may already have committed, but without a returned true the caller cannot dispatch or reuse that reservation. Retain prior-date rows; no cleanup job. Accepted cost: one admission database round trip per attempt and conservative lost reservations. Q4 refines the invariant for changed limits: no increment exceeds the limit in effect for that acquisition; historical counts need not be below a newly lowered limit.

### Q3 — Retry-aware seam and failures

Add an OMDb-only DelegatingHandler through AddHttpMessageHandler AFTER AddStandardResilienceHandler on the existing typed-client chain. The evidence for ordering is recon-DEV-426:74-88. This places admission inside retry and attempt timeout. Do not count once in FetchAsync or alter retry predicates/configuration. Each original attempt and retry needs a fresh committed reservation. Rejection above the handler by an open breaker, rate limiter or total timeout/backoff cancellation spends no additional reservation.

Construct the handler only with IServiceScopeFactory; create and dispose an async scope per SendAsync, resolve scoped IOmdbRequestGate, and dispose the scope before calling transport and on every exit. Do not capture a DbContext or scoped gate in the cached handler or keep request state on its instance.

False admission becomes one sealed internal Infrastructure signal, caught separately by FetchAsync and mapped to existing RateLimited. Gate persistence failure fails closed through a distinct internal signal with the original exception preserved, mapped to ProviderUnavailable. Neither signal is HttpRequestException, TimeoutRejectedException or a synthetic retryable HTTP response, so neither triggers retry or breaker predicates. Signals carry no API key or URI. Distinct log events distinguish the outcomes. Both stop further OMDb search/detail fallback variants while preserving the chained provider fallback. Caller cancellation propagates unwrapped rather than becoming either signal/result. No refund after admission.

### Q4 — Exact options and table

OmdbQuotaOptions is a Core record: section Omdb:Quota, single int DailyLimit default 1000, startup validation rejects zero/negative. Bind through ServiceDefaults like existing options. Q6 narrows configuration to a startup IOptions snapshot: changes take effect on restart; no IOptionsMonitor/hot reload. OmdbOptions and TMDB match settings are unchanged.

omdb_daily_usage has usage_date PostgreSQL date primary key and request_count integer NOT NULL CHECK (request_count >= 0), following existing record/configuration snake_case conventions. Enforce the configured limit only through the atomic SQL predicate; never hardcode 1000 in a database limit CHECK. Generate migration and regenerate snapshot using EF tooling, never hand-edit generated output. Add the table to MigrationTests.ApplicationTables and cover model/migration metadata.

Lowering DailyLimit below already-spent usage denies further admissions that date without reset, clamp or rewriting history. Raising it resumes against the carried count. Instances sharing a database must run consistent limits; there is no distributed configuration or reconciliation protocol. Mismatched limits are an operator error and can admit up to a participating instance limit. Prior days remain persisted.

### Q5 — Ordered implementation and tests

Tests land with each step; the last step is full verification, not the first test run.

1. Add Core port/options and ServiceDefaults binding/validation, with unit/startup tests for default 1000, 1, zero and negative; preserve architecture boundaries.
2. Add EF record/configuration/DbSet, atomic gate and scoped persistence registration; generate migration/snapshot and adapt migration metadata guards. Test SQL behavior against real PostgreSQL/Testcontainers.
3. Add the OMDb handler, internal signals, registration, FetchAsync mapping and distinct logging. Test mappings, fallback termination, fresh scopes, disposal before transport and absence of scoped capture.
4. Adapt affected provider factories, composition/startup/health fixtures and WireMock factories. Set explicit test DailyLimit and isolate quota rows per test class/database so unrelated tests cannot consume each other budgets. Fixture updates needed for a step travel with it rather than leaving intermediate tests broken.
5. Run targeted regressions, full suites and applicable implementation/PR gates.

Required PostgreSQL evidence: exactly N admissions from more than N synchronised contenders with independent contexts/scopes; empty-table first-row race creates one row with exact count; exhaustion survives gate/context recreation on the same database; UTC midnight charges distinct dates while retaining the old row; lowered/raised limits preserve history; pre-cancellation has no increment; persistence failure admits nothing. Do not mock EF/driver behavior dependent on SQL translation.

Required shipped-factory plus WireMock evidence: success and search/detail calls each consume appropriately; always-429 and always-500 consume 1+MaxRetryAttempts; 503-then-200 consumes 2; exhaustion during a retry sequence prevents the next transport request, stops OMDb variants, maps RateLimited and preserves chain fallback; open breaker and cancelled retry backoff spend no extra reservation; database failure yields ProviderUnavailable with zero dispatch; caller cancellation stays OperationCanceledException; acquired failures/cancellation never refund. Use the actual production composition, not only a manually assembled handler pipeline. Recon-DEV-426:79 identifies MetadataProviderProbe.BuildServices calling shipped AddMetadataProvider; plural spelling in the verbatim Patron Q5 narrative denotes that same existing composition, not a new method or rename.

Keep provider/fallback/resilience/chain/options/startup/health-registration/migration regressions green. Unit tests can isolate mappings and handler lifetimes; they cannot replace PostgreSQL concurrency or shipped-chain HTTP evidence.

### Q6 — Loop terms and scope

Closing bar for code-review/ship-review: no unresolved Critical, High or Medium findings, all applicable required gates supported by valid current evidence, and every Low finding explicitly fixed, accepted with rationale or routed as follow-up. An M-tier round requires Sentry, Ledger and Compass; a missing axis blocks. Planning/drafting is not review clearance.

Round cap: two full review/remediation rounds. After round two, unresolved blockers stop as blocked and are routed for Patron disposition, with follow-up recording by Rigger or owner escalation only for ticket contradiction/constitution departure. Do not lower thresholds, skip tests, reclassify findings or add rounds to manufacture clearance. No merge is authorised; the user owns merging.

Frozen scope: quota port/options, one counter table and generated migration, EF admission adapter/record/configuration/DbSet, OMDb handler/two internal signals and provider mapping/logging, scoped persistence/typed-client/options registrations, directly affected tests/factories/fixtures/migration metadata and test-only quota configuration; anything else is a follow-up issue, not a finding in this round.

Patron authorises narrow integration edits in recon-named existing files. No whole-file rewrite or deletion. No new dependency, project, top-level folder or architectural layer; no schema beyond omdb_daily_usage; no public API, DTO, OpenAPI, UI or generated TypeScript change; no LocalPlay, Process.Start or secret handling; no per-key budgets, cleanup job, retry-policy redesign, hot reload, distributed configuration or DEV-333 bulk-scan implementation.

## File scope for Quill planning

These paths identify planned changes from recon, not claims that new artifacts already exist. Quill may name the new adapter/handler/record/configuration/signal files within the authorised existing folders; names introduce no new abstraction beyond Q1-Q6.

- New src/LamuFlix.Core/Ports/IOmdbRequestGate.cs and src/LamuFlix.Core/Options/OmdbQuotaOptions.cs.
- New gate adapter, row record and row configuration under existing src/LamuFlix.Infrastructure/Persistence/ (Records and Configurations patterns), and OMDb quota handler/internal signal types under existing Infrastructure adapter organization.
- Edit src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs and PersistenceServiceCollectionExtensions.cs; generated migration under Persistence/Migrations/ and regenerated LamuFlixDbContextModelSnapshot.cs.
- Edit src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs, OmdbMetadataProvider.cs (FetchAsync boundary) and OmdbMetadataProvider.Log.cs; edit src/LamuFlix.ServiceDefaults/Extensions.cs for option registration.
- New gate/options/handler tests in existing tests/LamuFlix.UnitTests and tests/LamuFlix.IntegrationTests structures. Adapt the directly affected recon-identified OmdbMetadataProviderUnitTests, OmdbMetadataProviderFallbackUnitTests, MetadataProviderResilienceTests/probe, chain/health/startup/options/persistence composition fixtures, ApiHostFactory, ApiEndToEndFactory and relevant end-to-end suites. Update MigrationTests and relevant model metadata/script tests. Keep unrelated test behavior intact.
- Planning documents belong in specs/DEV-426. Keel owns CONCLUSIONS.md and this brief; Quill owns spec.md, plan.md and tasks.md drafting.

## Gate expectations and handoff

Implementation/PR phase expects Roslyn analyzers, cyclomatic complexity, JetBrains InspectCode, unit/integration/architecture/property/full-suite tests, vulnerable-package security checks, format and pre-PR mutation per harness configuration. A surviving mutant requires a missing test to be addressed; thresholds stay unchanged. Follow current script help/configuration rather than inventing flags. Exit 0 is pass, exit 1 fail; exit 2 remains the applicable SKIPPED (scope-empty), SKIP (opt-out) or mutation N/A result, never PASS. An unrunnable gate is Could not run. Apply gate evidence to the actual reviewed head.

This grill ran no implementation gates. Baseline receipts in note DEV-426:30-36 are earlier recorded evidence only. Quill can now draft spec/plan/tasks from this brief and CONCLUSIONS.md. Keel subsequently performs read-only consistency analysis and routes a numbered fix list; challenge/freeze, Gate 1, implementation, review and merge-bar decisions remain separate later phases. At pickup, obtain refreshed recon for main/contract drift rather than assuming this baseline remains current. No new glossary is required. ADR-0031 and the proposed constitution amendment accompany the spec PR as required below.

## Post-grill correction — C1/C2 constitution amendment and I1 sequencing

Authority: Patron C1/C2 ruling recorded append-only in CONCLUSIONS.md, committed as 35269ab0. This correction supersedes Q6 only where it said no ADR or owner approval was needed; the original Q6 answer remains preserved in CONCLUSIONS.md. All quota behavior, frozen implementation scope, test evidence, closing bar and two-round cap remain unchanged.

C1: Constitution Principle I (closed port list and ADR requirement, lines 192-197) and PR quality gate (line 433) require ADR-0031 for IOmdbRequestGate and an explicit port-list amendment. Keel drafts docs/adr/0031-omdb-request-gate.md with Proposed status pending owner approval. Preserve the architecture whitelist follow-up.

C2: Constitution Principles VII (options-record list, lines 324-326) and VIII (vendor-name exceptions, lines 348-356) require explicit exceptions for the ticket-named Core types. Keep IOmdbRequestGate and OmdbQuotaOptions exactly as named by the ticket. One proposed MINOR amendment, 1.6.0 to 1.7.0, adds IOmdbRequestGate to Principle I, OmdbQuotaOptions to Principle VII, and both types to the Principle VIII exceptions. Include its Sync Impact Report and template review under Governance (lines 597-602, 609-612); change no unrelated substantive constitution text. Precedents: 1.4.0/ADR-0024, 1.5.0/ADR-0029 and 1.6.0/ADR-0030.

The spec PR must include ADR-0031 and the amendment proposal, and carry this unchecked owner decision:

- [ ] Owner approves ADR-0031 and constitution amendment 1.7.0 for the ticket-named IOmdbRequestGate and OmdbQuotaOptions; approval occurs through the Gate 1 spec-PR merge.

This is an amendment path, not a waiver or present owner approval. Gate 1 stays closed until the owner approves; if the owner rejects, report blocked: structural — ticket-named port/types vs constitution. Planning analysis/challenge may continue against the explicit proposal without claiming that constitution 1.7.0 is already accepted or authorising implementation.

Quill updates spec.md, plan.md and tasks.md to cite Principles I/VII/VIII, ADR-0031 and the 1.7.0 proposal, with the owner checkbox and corresponding prerequisites. Quill may draft specs/DEV-426/constitution-amendment-v1.7.0.md as the proposal record. Keel owns this brief and ADR; constitution edits remain with the Conductor-assigned operator. This narrowly extends planning documentation scope; it adds no production feature.

I1 clarification applies the existing Q5 ruling without changing scope: fixture adaptations needed for Core/options, persistence or handler registration travel with the step that needs them. Do not force all T021/T022 adaptations to wait until Phase 3 is complete. Keep remaining fixture/matrix work in its later phase, require its prerequisites before T023/T024, and present all Q5 evidence as mandatory in the MVP description.

## Plan challenge supplement — PC1/PC2

Authority: Patron PC1/PC2 ruling in `CONCLUSIONS.md`, committed as c625ea29. This supplement supersedes earlier naming and Phase B date-reconciliation wording only where stated below; the owner checkbox, ticket scope and two-round cap remain unchanged.

- New Infrastructure support types use functional names, one file per type: `EfMetadataRequestGate`, `DailyRequestUsageRecord`, `DailyRequestUsageConfiguration`, `MetadataQuotaHandler`, `MetadataQuotaExhaustedException`, `MetadataQuotaPersistenceException`. Ticket Core names `IOmdbRequestGate`, `OmdbQuotaOptions` and table `omdb_daily_usage` remain exact. No wider constitution exception. Quota/admission/reservation remain local implementation vocabulary; no new domain glossary term.
- Neither local admission signal changes `MetadataProviderHealthState`; existing HTTP-path health behavior stays intact. Later admission interruption discards accumulated partial candidates and returns the mapped failure, stops further OMDb variants and preserves chained-provider fallback. Explicit tests lock both outcomes.
- Keel records the owner checkbox answer and spec-PR merge receipt in `CONCLUSIONS.md` for T034. T035 verifies the merged constitution without editing it, including its dates. Record staged-vs-merge dates in `CONCLUSIONS.md`; a date-only difference is recorded, not fixed. Substantive version/I/VII/VIII/Sync Impact Report mismatch blocks. Keel changes ADR-0031 status only in the implementation PR after verified owner approval and before code merge. No write to main. This supersedes I2 permission to reconcile dates.
- Pass the ambient `SendAsync` cancellation token into admission. Dispatch requires a returned true. Preserve Q2's in-flight committed-reservation caveat and unchanged resilience policy. Slow-admission composition evidence bounds commits by admission invocations and configured attempt/total budgets; commits need not equal wire count when admission times out. Cancellation is never converted into a persistence signal.
- Log distinct structured outcome/category events with no URI, key or raw exception serialization; preserve the original persistence exception only as an in-memory inner exception. Existing processing scope supplies movie id where available; invent no identifier at the gate.
- Challenge hardenings within Q2–Q5: zero affected/returned admission rows mean false, one committed row means true; bounded async scope must finish disposal before transport on every path; synchronized contenders use async coordination, never delay staggering or sync-over-async. Test matrices use Theory/MemberData and fresh AutoFixture/Faker data as applicable. Exact success cases are one reservation/request for a direct detail fetch and two for one successful search followed by one detail fetch, with each additional variant/request charged separately. A retry across UTC midnight samples its own date per acquisition.
- Quill must enumerate disjoint fixture file sets for T004/T012/T020/T021/T022 from recon, restrict changes to quota configuration/registration/state isolation and named migration guards, and state dependency order explicitly. No open-ended fixture sweep authorizes extra files. One DB query per denied attempt and consistent host limits remain accepted Q2/Q4 costs; no cache, startup-logging feature or reconciliation mechanism is added.
