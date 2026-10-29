# Implementation Plan: OMDb Daily Request Quota

**Branch**: `feature/426-spec` | **Date**: 2026-10-28 | **Spec**: `specs/DEV-426/spec.md`
**Input**: Frozen `brief.md` (Q1–Q6 + post-grill C1/C2/I1 + PC1/PC2); `CONCLUSIONS.md` (Patron PC1/PC2 at c625ea29); Proposed ADR-0031; proposed `constitution-amendment-v1.7.0.md`; `PLAN_CHALLENGE.md`; Maestri notes `DEV-426`, `recon-DEV-426`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (staged 1.7.0 for Gate 1; Quill does not edit constitution or ADR bodies — Keel owns ADR/brief/CONCLUSIONS)
**Status**: Draft under plan-challenge ROUND 1 remediation for Keel re-adjudication; Gate 1 not approved (owner checkbox open); Phase A documents only; PLAN FROZEN not declared; Phase B is verify-only with no constitution date edit (PC2).

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`, `constitution-amendment-v1.7.0.md` (proposal record; Quill); companion Proposed `docs/adr/0031-omdb-request-gate.md` (Keel-owned). No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief, and the public contract chain is untouched. Precedent: DEV-423 / DEV-303 / DEV-330 amendment path.

## Summary

Deliver a shared persisted OMDb daily request budget (default 1000 per TimeProvider UTC date) via Core `IOmdbRequestGate` + `OmdbQuotaOptions`, table `omdb_daily_usage`, scoped `EfMetadataRequestGate` atomic admission, and OMDb-only `MetadataQuotaHandler` registered after `AddStandardResilienceHandler` so each physical outbound attempt (including retries) consumes one reservation. Exhaustion and gate persistence failure become distinct internal signals (`MetadataQuotaExhaustedException` / `MetadataQuotaPersistenceException`, one file each) mapped in `FetchAsync` to existing `RateLimited` / `ProviderUnavailable`, without changing `MetadataProviderHealthState`. Concurrent, restart, rollover, limit-change, and shipped-factory+WireMock evidence are required. No public API, secrets, LocalPlay, new dependency, cache, startup-log feature, retry-policy redesign, or DEV-333 bulk-scan work.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).
**Primary Dependencies**: Existing pins only — EF Core / Npgsql / Microsoft.Extensions.Http / Http.Resilience / Options validation. **No new packages** (brief §2.3 item 1; recon-DEV-426 section 7).
**Storage**: PostgreSQL table `omdb_daily_usage` via existing `LamuFlixDbContext`; atomic conditional insert-or-increment.
**Testing**: xUnit v3, Shouldly; AutoFixture/Faker fresh data as applicable; Theory/MemberData for options and WireMock matrices; UnitTests for options/mappings/handler lifetime; IntegrationTests with Testcontainers/PostgresFixture and WireMock through shipped `AddMetadataProvider` composition. No live OMDb. TDD optional; required coverage green before verification.
**Target Platform**: .NET server (ASP.NET Core Minimal API host hosting enrichment consumers).
**Project Type**: Core port/options + Infrastructure persistence adapter + HttpClient DelegatingHandler seam into existing OMDb typed client.
**Performance Goals**: one admission DB round trip per physical attempt (including denied attempts — accepted Q2 cost); conservative lost reservations accepted (Q2). No negative-cache feature.
**Constraints**: `TimeProvider` only (no `DateTime.Now`); ambient `SendAsync` token into gate; no explanatory comments; complexity ≤ 15 then refactor gate per `harness.yml`; never hardcode/lower thresholds; no public API/secret/LocalPlay; no hand-edited migration/snapshot; no hot reload; no scoped capture in handler; no sync-over-async; no commits-equal-wire assertion under admission timeout.
**Scale/Scope**: personal library; shared DB-wide daily counter; Size M.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (staged Version 1.7.0 on this branch; prior baseline was 1.6.0). Post-grill Patron C1/C2 + I2 + PC1/PC2 rulings (brief; `CONCLUSIONS.md`) supersede Q6’s no-ADR / no-owner-approval wording and supersede I2 date-reconcile / same-delivery ADR publish wording: ticket-named `IOmdbRequestGate` and `OmdbQuotaOptions` require Proposed ADR-0031 plus one MINOR amendment 1.6.0 → 1.7.0 drafted and staged **in this DEV-426 spec PR**. Quill drafted the amendment proposal record and cites it; Quill does not edit `.specify/memory/constitution.md` or ADR bodies. New Infrastructure support types use PC1 functional names — no wider Principle VIII exception. Architecture whitelist adaptation for the new port remains the Principle I follow-up test edit (recon-DEV-426:33; FR-007). Owner Gate 1 checkbox in `spec.md` stays unchecked until merge approval. After verified approval, Keel records answer + merge receipt in `CONCLUSIONS.md`; T035 verifies with **no constitution edit including dates**; Keel bumps ADR Accepted only in the implementation PR before code merge. No present owner approval. No root glossary/`CONTEXT.md` rewrite (Q6; PC1 Ledger M1).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Ports in Core; adapters in Infrastructure; closed port list / ADR on new ports (`:192-196`); PR quality gate new ports have an ADR (`:433`) | **CONDITIONAL PASS (owner Gate 1)**. Ticket-named `IOmdbRequestGate` in Core/Ports; `EfMetadataRequestGate` in Infrastructure; Proposed ADR-0031; proposed 1.7.0 adds the port to the closed list. Update `ArchitectureTests` `PortWhitelistPattern` as Governance follow-up. Noncompliant implementation unauthorized until owner answers the checkbox. |
| II / V (Failures / API boundary) | Domain failures mapped at the API; Infrastructure control signals must not escape | Q3 internal signals (`MetadataQuotaExhaustedException` / `MetadataQuotaPersistenceException`) are caught inside Infrastructure `FetchAsync` and become existing `RateLimited` / `ProviderUnavailable` categories; they never reach API exception mapping. Unit/mapping tests lock this (T015/T018/T020). (PLAN_CHALLENGE Ledger L2.) |
| VI (Observability) | Distinct outcomes observable; safe structured fields (`:334-335`) | Distinct outcome/category log events for exhaustion vs persistence failure; no URI/key/raw exception serialization; original inner preserved in memory only; existing processing scope supplies movie id where available; no invented gate identifier (Q3; PC2; PLAN_CHALLENGE Sentry M4, Ledger L4). |
| VII (Configuration Isolation) | Validated options; options-record list (`:324-326`); no hardcoded secrets | **CONDITIONAL PASS (owner Gate 1)**. Ticket-named sealed `OmdbQuotaOptions` / `SectionName` `Omdb:Quota` / `[Range(1, int.MaxValue)] DailyLimit`; BindAndValidate + data annotations + ValidateOnStart; proposed 1.7.0 adds `OmdbQuotaOptions` to the VII list. ApiKey remains env/secret-managed outside this record (Q4). |
| VII (Deterministic time) | TimeProvider for dates/rollover | Gate and tests sample UTC date via injected `TimeProvider` / `FixedTimeProvider` only; no `DateTime.Now` (BannedSymbols). Per-acquisition sampling; midnight-crossing retries sample independently (PLAN_CHALLENGE Ledger L1, Sentry L1). |
| VIII (Ubiquitous Language) | Vendor names limited; Core stays functional (`:351-356`); Infrastructure support types functional | **CONDITIONAL PASS (owner Gate 1)**. Ticket names `IOmdbRequestGate` and `OmdbQuotaOptions` preserved; proposed 1.7.0 adds both to VIII vendor exceptions; Infrastructure support types use PC1 functional names (`EfMetadataRequestGate`, `DailyRequestUsageRecord`, `DailyRequestUsageConfiguration`, `MetadataQuotaHandler`, `MetadataQuotaExhaustedException`, `MetadataQuotaPersistenceException`) — **no wider constitution exception**. No new domain glossary entry (Q6; PC1). |
| IX (Test Pyramid) | WireMock for HTTP; real DB for SQL; AutoFixture/Faker; Theory/MemberData; no mocked driver translation; no sync-over-async (`:403-410`) | PostgreSQL Testcontainers for gate atomicity; WireMock + shipped composition for handler ordering (Q5). Theory/MemberData for T004/T023/T024; AutoFixture/Faker fresh data as applicable; T013 async coordinated start + `Task.WhenAll`. Fixture adaptations travel with the step that needs them on enumerated files (I1). |
| Tech Stack | Typed HttpClient + Http.Resilience for metadata providers | `MetadataQuotaHandler` inserted on existing OMDb builder after standard resilience; ambient token into gate; predicates unchanged (Q3; PC2). |
| IX (async testing) / Coding Conventions / PR quality gates | Ambient tokens; no sync-over-async (`:409-410`, `:475`, `:540-542`) | Handler passes `SendAsync` token into `TryAcquireAsync`; cancellation excluded before fault-to-signal mapping; no blocking waits in concurrency tests (PC2; PLAN_CHALLENGE Sentry H1/H2, Ledger L1/M4). |
| Governance | Amendment procedure (`:597-602`); Compliance simultaneous amendment (`:609-612`) | Written proposal `constitution-amendment-v1.7.0.md` **prepared** and Constitution 1.7.0 **staged** into `.specify/memory/constitution.md` in this spec PR with ADR-0031 Proposed for owner review. After Gate 1 approval, Keel records checkbox + merge receipt in `CONCLUSIONS.md`; Phase B **verifies** merged constitution with **no edit including dates** (date-only difference recorded in `CONCLUSIONS.md`); substantive mismatch blocks. Keel bumps ADR-0031 to Accepted only in the **implementation PR** after verified owner approval and before code merge. Precedents 1.4.0/ADR-0024, 1.5.0/ADR-0029, 1.6.0/ADR-0030. (PC2.) |
| Gates | Roslyn, complexity, InspectCode, format, vulnerable packages, mutation | Phase B only; see tasks verification phase. |

### Complexity Tracking

Constitution departure for ticket-named Core port/options is handled by the proposed 1.7.0 amendment + ADR-0031 path (C1/C2), not a waiver and not a silent assumption. Infrastructure functional renaming needs no further exception (PC1). Gate 1 owner checkbox remains open; Quill invents no alternate disposition.

## Approach

- **Gate 1 prerequisites (C1/C2; PC2)**: amendment proposal + ADR-0031 Proposed are prepared and Constitution 1.7.0 is staged in this spec PR for owner review; owner approves via Gate 1 spec-PR merge (unchecked checkbox). After verified approval, Keel records answer + merge receipt in `CONCLUSIONS.md` (T034); T035 verifies merged constitution with no edit including dates; Keel Accepts ADR-0031 only in the implementation PR before code merge. Rejection → `blocked: structural — ticket-named port/types vs constitution`.
- **Core first (Q5 step 1)**: `IOmdbRequestGate` + sealed `OmdbQuotaOptions` (`SectionName`, `[Range(1, int.MaxValue)]`); ServiceDefaults `BindAndValidate` / ValidateOnStart; unit/startup Theory/MemberData tests for 1000/1/0/negative; architecture whitelist includes `IOmdbRequestGate`; options/startup fixture adaptations on T004 enumerated files travel with this step (I1).
- **Persistence (Q5 step 2)**: `DailyRequestUsageRecord` + `DailyRequestUsageConfiguration` (snake_case `omdb_daily_usage`); DbSet; scoped `EfMetadataRequestGate` with one atomic conditional insert-or-increment using TimeProvider UTC date and `IOptions<OmdbQuotaOptions>` snapshot sampled once (zero rows → false; one row → true; SQL errors → faults); EF-generated migration + snapshot; named migration guards on T012 files; composition/startup/health/options/persistence fixture adaptations on T021 files travel with this step (I1). Real PostgreSQL tests only for SQL behaviour.
- **Transport seam (Q5 step 3)**: `MetadataQuotaHandler : DelegatingHandler` + `MetadataQuotaExhaustedException` / `MetadataQuotaPersistenceException` (separate files); register after `AddStandardResilienceHandler` on OMDb client only; ambient token; `IServiceScopeFactory` per EnrichmentConsumer pattern; dispose-before-transport on every path; `FetchAsync` maps signals (health unchanged; discard partials); distinct safe LoggerMessage entries in `OmdbMetadataProvider.Log.cs`; provider-factory adaptations on T020 files travel with this step (I1).
- **Api-host isolation + matrix (Q5 step 4; I1)**: ApiHost/ApiEndToEnd isolation on T022 files with explicit test `DailyLimit`; then mandatory shipped WireMock attempt-count + slow-gate matrix (T023/T024) on probe/resilience files. Do not hold early fixture work until after Phase 3 completes — only Api-host isolation and WireMock matrix wait; their prerequisites must be done first.
- **Verification (Q5 step 5)**: targeted then full suites; analyzer/complexity/InspectCode/format/security/mutation per harness; Size M three-axis review under Q6 bar.

## Project Structure

```text
specs/DEV-426/
  brief.md, CONCLUSIONS.md, PLAN_CHALLENGE.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  constitution-amendment-v1.7.0.md                 (proposed; Quill)
  checklists/requirements.md
docs/adr/
  0031-omdb-request-gate.md                       (Proposed; Keel)

src/LamuFlix.Core/
  Ports/IOmdbRequestGate.cs                         (new; ticket name exact)
  Options/OmdbQuotaOptions.cs                       (new; ticket name exact; sealed record)

src/LamuFlix.ServiceDefaults/
  Extensions.cs                                     (edit: BindAndValidate OmdbQuotaOptions)

src/LamuFlix.Infrastructure/
  Persistence/Records/DailyRequestUsageRecord.cs    (new; PC1)
  Persistence/Configurations/DailyRequestUsageConfiguration.cs  (new; PC1)
  Persistence/EfMetadataRequestGate.cs              (new; PC1)
  Persistence/LamuFlixDbContext.cs                  (edit: DbSet)
  Persistence/PersistenceServiceCollectionExtensions.cs (edit: scoped gate)
  Persistence/Migrations/*                          (generated)
  Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs (regenerated)
  Adapters/MetadataQuotaHandler.cs                  (new; PC1)
  Adapters/MetadataQuotaExhaustedException.cs       (new; PC1; one file)
  Adapters/MetadataQuotaPersistenceException.cs     (new; PC1; one file)
  Adapters/MetadataProviderServiceCollectionExtensions.cs (edit: AddHttpMessageHandler after resilience)
  Adapters/OmdbMetadataProvider.cs                  (edit: FetchAsync mapping)
  Adapters/OmdbMetadataProvider.Log.cs              (edit: distinct safe log events)

tests/LamuFlix.UnitTests/          (enumerated ownership — see tasks)
tests/LamuFlix.IntegrationTests/   (enumerated ownership — see tasks)
tests/LamuFlix.ArchitectureTests/
  ArchitectureTests.cs                              (edit: PortWhitelistPattern += IOmdbRequestGate)
```

**Disjoint recon-backed test/fixture ownership** (PC2; PLAN_CHALLENGE Ledger M3; recon-DEV-426:27,42-52,79):

| Task | Exact files (quota-only config/registration/isolation or named migration guards) |
|---|---|
| T004 | Create `tests/LamuFlix.UnitTests/Options/OmdbQuotaOptionsTests.cs`; edit `tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs` only for `OmdbQuotaOptions` BindAndValidate / ValidateOnStart coverage (recon:27). |
| T012 | Edit `tests/LamuFlix.IntegrationTests/MigrationTests.cs` (`ApplicationTables` += `omdb_daily_usage`); `tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs` named-entity/column metadata for the new table; `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs` migration-script assertions that enumerate application tables (recon:52). |
| T020 | Create `tests/LamuFlix.UnitTests/Adapters/MetadataQuotaHandlerTests.cs` (lifetime/disposal/token); edit `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs` and `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderFallbackUnitTests.cs` for both-signal mapping, health unchanged, partial discard, safe-log leakage (recon:42-43). |
| T021 | Edit only: `tests/LamuFlix.UnitTests/HealthCheckRegistrationTests.cs`, `tests/LamuFlix.UnitTests/Persistence/PersistenceServiceCollectionExtensionsTests.cs`, `tests/LamuFlix.UnitTests/Persistence/TasteProfileCacheRegistrationTests.cs` — quota `DailyLimit` config / scoped-gate registration / row isolation needed so those suites stay green after gate registration (recon:27). |
| T022 | Edit only: `tests/LamuFlix.IntegrationTests/ApiHostFactory.cs`, `ApiEndToEndFactory.cs`, `ApiEndToEndEnrichmentTests.cs`, `ApiEndToEndImportTests.cs`, `ApiEndToEndFeaturesTests.cs` — explicit test quota configuration and per-class/database row isolation (recon:49). |
| T023/T024 | Edit/extend `tests/LamuFlix.IntegrationTests/MetadataProviderProbe.cs` and `MetadataProviderResilienceTests.cs` for shipped WireMock attempt-count / slow-gate / failure matrix (recon:79). |

No open-ended “related/remaining” suite authorisation. Missing path evidence → `needs recon` in DRAFTING_RECEIPT, not invention.

**Layout decisions (brief file scope + Q5–Q6 + C1/C2 + PC1/PC2)**:

- Names introduce no new abstraction beyond Q1–Q6; PC1 functional Infrastructure names; patterns match `EfEnrichmentOutbox` / OutboxMessage record-configuration / EnrichmentConsumer scope factory.
- No research/data-model/contracts artifacts — public contract chain untouched; decisions frozen in brief.
- Quill owns `spec.md` / `plan.md` / `tasks.md` / drafting receipt/checklist / `constitution-amendment-v1.7.0.md`. Quill does not edit `brief.md` / `CONCLUSIONS.md` / `.specify/memory/constitution.md` / ADR bodies / `PLAN_CHALLENGE.md`.

## Test Strategy

| Surface | Coverage |
|---|---|
| Options/startup (T004) | Theory/MemberData: default 1000, 1 accepted; 0/negative fail; AutoFixture/Faker fresh data as applicable; sealed record / SectionName / Range; architecture whitelist |
| PostgreSQL gate (T013–T014) | N of >N contenders via async coordinated start + `Task.WhenAll` (no delay staggering/blocking waits); first-row race; restart exhaustion; UTC midnight; cross-midnight retry per-acquisition date; lower/raise limit; zero/one-row semantics; pre-cancel; DB failure → fault/no admit |
| Unit mapping/lifetime (T020) | Both signals → RateLimited/ProviderUnavailable; health unchanged; partial discard; fallback stop; fresh scope; disposal on true/false/fault/cancellation + dispose-before-transport; no scoped capture; safe-log leakage; cancellation never mapped |
| Shipped + WireMock (T023–T024) | Theory/MemberData matrices: detail=1; search+detail=2; each extra request +1; 429/500 → 1+MaxRetryAttempts; 503-then-200 → 2; mid-retry exhaustion; breaker/backoff zero extra; cancel unwrapped; no refunds; slow-gate bounds commits/invocations by attempt/total budgets — no commits-equal-wire under timeout; no retry-policy change |
| Regressions (T004/T012/T020/T021/T022 enumerated only) | Named suites stay green with isolated quota state |

No Gherkin. Property tests only if harness requires for changed surface — report SKIP/SKIPPED/N/A distinctly.

## Gates (Phase B — after implementation)

1. Explicit changed-C# scope: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness, `./scripts/run-jetbrains-inspectcode.ps1`.
2. Affected unit/integration/architecture suites, then full `dotnet test`.
3. Property / vulnerable-packages / format per harness; distinguish PASS, failure, SKIPPED (scope-empty), SKIP, mutation N/A, Could not run.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` from worktree).
5. Size M review: Sentry, Ledger, Compass; Q6 closing bar; two remediation rounds max.

Phase A (this draft) runs none of the build/test gates and claims no gate result. Baseline receipts in note DEV-426:30-36 are earlier evidence only.

## Risks

- **Scoped capture**: injecting DbContext/gate into the cached handler — mitigated by `IServiceScopeFactory` + dispose-before-transport on every path (Q3; recon 9.2; PLAN_CHALLENGE Sentry M3).
- **Wrong handler position**: registering before resilience would under-count retries — mitigated by after-resilience registration + WireMock count locks (Q3, Q5).
- **Fixture cross-talk**: shared DailyLimit/rows flaking unrelated tests — mitigated by explicit test limits and per-class/database isolation on enumerated files (Q5; Ledger M3).
- **Migration guard drift**: forgetting `ApplicationTables` — mitigated by FR-003 / T012 (recon:52).
- **Pickup drift**: baseline HEAD may age — mandatory refreshed recon before Phase B coding (brief).
- **Conservative overspend denial**: lost reservations after `true` without successful HTTP — accepted (Q2); slow-gate may leave commits without wire equality — accepted, not asserted away (PC2 Sentry H1).
- **Denied-attempt DB cost**: one round trip per denied admission — accepted Q2 cost; no cache feature (PLAN_CHALLENGE Sentry M2).
- **Mismatched host limits**: operational precondition without detection/reconciliation/startup-log — accepted (Q4; PLAN_CHALLENGE Sentry L2).

## Ordering

See `tasks.md` phase dependency table (stable task IDs retained; execution order made explicit). Sequence: Gate 1 record (T034) + verify merged constitution with no edit (T035) → Core/options (T001–T005) → persistence/migration/gate (T006–T014, T021) → handler/signals/FetchAsync (T015–T020) → Api-host isolation (T022) → mandatory WireMock matrix (T023–T024) → verification/review (T025–T033). Same-file edits are sequenced. Tests land with each step; prerequisite fixtures travel with the first step that needs them (I1); step 5 is full verification, not the first test run (Q5). All Q5 evidence is mandatory. Amendment staging is Phase A / this PR; Phase B verifies only and does not re-apply or date-edit the constitution (PC2).
