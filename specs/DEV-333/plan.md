# Implementation Plan: DEV-333 Recursive library import with bounded pipeline and quota-day deferral

**Branch**: `feature/333-spec` | **Date**: 2026-10-29 | **Spec**: `specs/DEV-333/spec.md`

**Input**: Feature specification from `specs/DEV-333/spec.md`, frozen grill brief `specs/DEV-333/brief.md` (Q1–Q8), Patron rulings in `specs/DEV-333/CONCLUSIONS.md`, facts from notes `DEV-333`, `recon-DEV-333`, `recon-DEV-333-2`, product authority `specs/PRODUCT.md` sections 3 and 5. Recon baseline `f698aeceb6b6af50a397d076b83ed178566641b0`.

**Status**: Draft planning artifact only (Phase A). Grill closed; Gate 1 not claimed. No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. **Phase B STOP** until pickup verifies landed amended DEV-426 (quota-specific outcome) and main drift — do not treat unmerged `fee5c7a8` as landed (brief Authority; Q7 order).

## Phase B entry preflight (executable before Code Phase 1)

| Check | Owner / evidence | Blocks code if missing |
|---|---|---|
| Spec PR merged and Gate 1 established | Rigger / YouTrack Ready-to-build handoff | Yes |
| Phase B pickup verifies landed DEV-426 with amended map-to-`QuotaExhausted` (not RateLimited), ADR-0031/constitution alignment approved, and recon drift vs authorized file set | Wisp → recon refresh; Keel clearance | Yes |
| Keel drift check or amend against brief/spec before code | Keel handoff in DEV-333 | Yes |

## Summary

Deliver lazy recursive Candidate Movie Folder discovery behind `ILibraryFolderDiscovery`, a one-shot `ImportLibraryCommand` with bounded MaxConcurrency pipeline and per-folder `IMovieFolderImporter` scopes reusing `ImportMovieFolderCommandHandler`/outbox, plus enrichment-side quota-day deferral (`EnrichmentDeferredUntil`, `QuotaDeferred`, durable `enrichment.quota-deferred` path) so Scope 3/AC3 pause/resume holds without scanner-side gate acquisition. No public API, scheduler, skip-existing/resume, or DEV-426 implementation in this PR.

## Domain vocabulary

| Term | Definition source | Notes |
|------|-------------------|-------|
| Candidate Movie Folder | brief Q4 | Exact glossary text lands in `CONTEXT.md` with the spec (Phase A); not deferred to delivery |
| QuotaExhausted / quota_exhausted | brief Q7 clarification | DEV-426 owns category, constitution amendment, glossary, and chain-fallback eligibility; T001 verifies landing — DEV-333 does not redefine or implement |
| QuotaDeferred | brief Plan-challenge round 1 §7 | Exact delivery glossary entry in `CONTEXT.md` (settlement outcome: Pending, same attempt, re-dispatch at EnrichmentDeferredUntil; not failure/retry) |
| EnrichmentDeferredUntil / enrichment_deferred_until | brief Q2 / Q7 / Plan-challenge round 1 §7 | Exact delivery glossary entry; nullable movies column; next-UTC-midnight from injected TimeProvider |
| quota-deferred (routing) / enrichment.quota-deferred | brief Q7 / Plan-challenge round 1 §7 | Exact delivery glossary entry; durable queue carrying same-attempt EnrichmentRequested until deadline, then dead-letters into requested |

## Technical Context

**Language/Version**: C# / .NET 10 backend. No web change.

**Primary Dependencies**: None added. Reuse System.Threading.Channels (BCL), System.IO.Abstractions, EF Core, Npgsql, RabbitMQ.Client, xUnit/Shouldly/NSubstitute/Testcontainers/MockFileSystem/FakeTimeProvider already in the solution (brief Q3, Q5, Q7; recon-DEV-333 §5, §8).

**Storage**: PostgreSQL existing `movies` table gains nullable `enrichment_deferred_until` timestamptz (brief Q2). One EF-tool-generated migration; never hand-edit generated files. Latest migration at recon baseline includes `20261026035137_AddEnrichmentOutbox` (recon inventory); timestamp for the new migration is tooling-supplied at implementation.

**Testing**: xUnit v3; Shouldly; NSubstitute; MockFileSystem; FakeTimeProvider; AutoFixture/Faker.Net and Theory/MemberData for repeated input cases (constitution IX; brief Plan-challenge round 1 nondecision conventions); invariant-specific builders where needed; real PostgreSQL and RabbitMQ Testcontainers. Never mock EF driver translation. No Gherkin/Reqnroll. New domain unit tests live only in `tests/LamuFlix.UnitTests` (no legacy `LamuFlix.Test` fallback). Property tests in existing UnitTests property-test locations (brief Q7). Named proofs use real SUT member names (`HandleAsync`, `TryClaimForEnrichmentAsync`, `FindStrandedMovieIdsAsync`, `Decide`, composed fixture names only where no single member is SUT). All gate/test execution belongs to Phase B / Build workspace.

**Target Platform**: Existing `LamuFlix.Api` host. No public HTTP/OpenAPI shape change. `POST /movies/import` remains the only import surface (recon-DEV-333 §3).

**Project Type**: Existing ports-and-adapters solution. New ports in `LamuFlix.Core.Ports`; command/handler/result in `LamuFlix.Core.Features.Import`; adapters under Infrastructure FileSystem and Import; enrichment settlement in Core; RabbitMQ routing/topology in Infrastructure; tests in existing unit/integration/architecture projects (brief Planned file responsibilities).

**Performance Goals**: Bounded residency = MaxConcurrency queued + MaxConcurrency in-flight + one producer-held path; traversal memory depth-only (brief Q5). AC4 dry run: 500 movies.

**Constraints**: TreatWarningsAsErrors. Ticket scope authoritative. No LocalPlay/secrets/`Process.Start`. No public HTTP/OpenAPI/web. No new dependency/project/layer. Never hand-edit EF-generated migration/Designer/snapshot. Mutation never uses `--since`. Thresholds never lowered. Scope-empty/opt-out/N/A/Could not run never PASS. Phase B STOP on unverified DEV-426 landing/drift. Walk/import never call TryAcquireAsync.

**Scale/Scope**: Frozen scope sentence (brief Q8): Deliver lazy recursive candidate discovery, one configured bounded import pipeline, unchanged single-folder import/outbox reuse with per-folder scopes, an internal explicitly invoked one-shot command/result, quota-specific durable Pending-until-next-UTC-day settlement with the approved deferral column/claim/sweeper/broker changes, and the exact acceptance proofs, focused ADR, glossary term and port alignment; anything else is a follow-up issue, not a finding in this round.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Every gate cites a numbered Core Principle (I–IX). Unnumbered constraints listed separately.*

- **Principle I** (ports and adapters; `.specify/memory/constitution.md`): Two new Core ports (`ILibraryFolderDiscovery`, `IMovieFolderImporter`) require a focused ADR and ArchitectureTests whitelist + Principle I list update (brief Q3; recon-DEV-333 §2). Keel authors that ADR in Phase A before analyze clearance / plan challenge at `docs/adr/0032-recursive-import-ports-and-scope-seam.md` (brief Post-draft process decision + ADR evidence closure; `recon-DEV-333-2-2` §§1–2). Status Proposed — no Gate 1 or constitution approval claimed. Phase B T002/T004 name an approval-evidence-gated Proposed→Accepted transition only after applicable approval evidence (Ledger M1; brief Plan-challenge round 1 §1); drafting alone never Accepts the ADR. Core stays BCL-only; DI scope seam lives in Infrastructure. Channels are BCL. No new project/layer. Port-list amendment follows Governance Sync Impact Report, version bump, Last Amended date, and `.specify/templates/*` review against the actual Phase B constitution baseline (recon baseline Version 1.6.0; DEV-426 may land a further amendment first) without claiming approval by drafting.
- **Principle II** (explicit handlers): Sealed command/handler; `AddHandler` registration in `HandlerRegistration.cs` (brief Q3; recon-DEV-333 §3).
- **Principle IV** (enrichment state machine; atomic claim; sweeper) — compliant clarification, not a departure; no new DEV-333 owner checkbox (Patron Plan-challenge round 1 §1; CONCLUSIONS clause-by-clause IV/Reliability): Delivery T004 updates Principle IV inventories to name (a) `Movie.DeferEnrichmentUntil(DateTimeOffset)` — sets `EnrichmentDeferredUntil`, no status change, Pending-only; no public clearer (clear only inside atomic claim); (b) claim bullet's deferral predicate and atomic clear of `EnrichmentDeferredUntil` in the same `ExecuteUpdateAsync` as LastAttemptAt refresh while remaining a single atomic `UPDATE … WHERE Status = Pending`; (c) sweeper bullet's future-deferral exclusion / post-expiry eligibility, with attempt-1 recovery retained only on the lost-message backstop path. `QuotaExhausted` closed-category expansion remains DEV-426's constitution amendment and owner checkbox — T001 verifies landing; DEV-333 does not duplicate or close it. Named domain mutator legal/illegal unit proofs and claim/sweeper proofs are required (PR gates / tasks).
- **Principle V** (validated inputs) — by construction, not decorator tolerance (Patron Plan-challenge round 1 §2; `recon-DEV-333-2-2` §2): Register **no** `IValidator<ImportLibraryCommand>` and create **no** empty FluentValidation stub/file. `AddHandler` always composes `ValidationDecorator`; the sealed payload-free command has no bindable input; RootPath/`MaxConcurrency` arrive pre-validated on `LibraryOptions` BindAndValidate / ValidateOnStart (Q5 `[Range(1, int.MaxValue)]`). Precedent: payload-free `SweepStrandedMoviesCommand` / `SweepMissingMovieFeaturesCommand` (and payload-free queries) run with no validator. A future payload field on this command requires a real validator in that change. No public HTTP surface, so no new ProblemDetails mapping (brief Q3; constitution V).
- **Principle VI** (observability): Delayed quota-deferred publish MUST propagate W3C `traceparent`/`tracestate` through the existing publisher propagator into message headers; consumer extract + `Enrichment.Process` / ActivityLink-on-redelivery remain. Span names, metric names, and attribute keys that this surface already uses MUST come from existing `TelemetryConstants` members (`EnrichmentEnqueue`, `EnrichmentProcess`, `MovieId`, `MessagingDeliveryCount`, `EnrichmentOutcome` / `OutcomeTagName`, `EnrichmentOutcomeMetricName`, `ImportCountMetricName`) — no adapter-local telemetry key strings, no speculative new metric/span/tag-value additions, and **no unassigned `TelemetryConstants.cs` write target** (reference existing members only). Settled Q7 routing/outcome names (`quota-deferred`, `ProcessEnrichmentOutcome.QuotaDeferred`, topology constants) are domain/routing vocabulary, not a plan-time TelemetryConstants invent-or-recon obligation. If implementation later finds an actually necessary new telemetry name, require then-current evidence at that time; do not add speculative instrumentation here.
- **Principle VII** (configuration / TimeProvider): `LibraryOptions.MaxConcurrency` validated (binder rejects fractional/nonnumeric configuration strings; Range/startup validation rejects zero/negative integers — distinct failure paths, same fails-startup result). All deadline writes, claim/stranded comparisons, and TTL duration computation use injected application `TimeProvider.GetUtcNow()` UTC samples — never a database-clock substitute (Sentry M4; brief Plan-challenge round 1 §4). Next UTC midnight and lease maths from the same injected TimeProvider; no banned DateTime APIs (brief Q2, Q5; recon-DEV-333-2 §2).
- **Principle VIII** (ubiquitous language): Exact Candidate Movie Folder text lands in `CONTEXT.md` with the spec (Phase A). Delivery lands the three exact Plan-challenge round 1 §7 glossary entries (`QuotaDeferred`, `EnrichmentDeferredUntil`, `quota-deferred` routing) verbatim; DEV-426 owns `QuotaExhausted` glossary/eligibility — verify at T001, never redefine (brief Q4, Q7, Plan-challenge round 1 §7).
- **Principle IX** (test pyramid): Unit + MockFileSystem + FakeTimeProvider in `LamuFlix.UnitTests` only for new domain tests; AutoFixture/Faker.Net and Theory/MemberData for repeated input cases; integration with real Postgres/RabbitMQ Testcontainers; property invariant for candidate-set identity; no EF InMemory; no legacy `LamuFlix.Test` fallback (brief Q7; constitution IX; Plan-challenge round 1 nondecision conventions).

### Unnumbered constitution authority checks

- **Technology Stack Constraints**: Existing .NET 10 / EF / RabbitMQ / System.IO.Abstractions stack; no new package.
- **Coding Conventions — Comments**: AAA headers / empty-block / scoped suppressions only.
- **Coding Conventions — no new plain enums**: Preserve existing string-action `EnrichmentRouting.Disposition` record; narrow extension only — optional `DateTimeOffset? DeferredUntil = null`, `QuotaDeferred` action constant, and one `Decide` arm returning quota-deferred routing key, null `NextAttempt`, and deadline; consumer publishes with delivered original `Attempt` (Patron Plan-challenge round 1 §8; `recon-DEV-333-2-2` §3). No enum or routing rewrite.
- **Enrichment Reliability Rules** (constitution :532–539 alignment — compliant clarification with IV, not departure; brief Plan-challenge round 1 §1): Delivery T004 adds one quota-deferral settlement paragraph: claim-before-provider unchanged; `QuotaDeferred` is settlement, not retry (same attempt, no `EnrichmentRetryPolicy`, no `MarkFailed`); re-dispatch is the durable quota-deferred TTL queue dead-lettering into requested (distinct from failure-retry 30s path); sweeper is post-expiry backstop only. No second pipeline forked.
- **Async rules**: CancellationToken through every async path; awaited WriteAsync/ReadAsync; no sync-over-async (recon-DEV-333-2 §2).
- **Pull Request Quality Gates**: Configured scripts and harness thresholds only (brief Q7–Q8). Domain mutator transition proofs and claim atomicity remain mandatory under Principle IV / PR gates.
- **Governance** (constitution amendment procedure): Against the actual Phase B constitution baseline, record Sync Impact Report, version semantics (MINOR for material Principle I / IV inventory expansion unless baseline already records them), Last Amended date, and `.specify/templates/*` review with evidence of required edits or a no-change conclusion; drafting does not claim owner approval or Gate 1. No new DEV-333 owner checkbox; DEV-426 Gate 1 checkbox remains separate and unclosed by this plan.
- **Known debt**: RateLimited retries remain delayed; quota path must not become a tight loop (recon-DEV-333-2 §2; brief Q1).

Post-design re-check: numbered-principle gates still hold, including IV/V/VI/Reliability obligations above as compliant clarification (not departure). Plan adds ruled ports/adapters/options/deferral/settlement/topology and named proofs with no public HTTP contract, no new dependency/project/layer, no new owner checkbox, and no harness edit. Side design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) are not produced by Quill — the frozen brief settles contracts, files, ordering, and proofs. No `design.md` (no ui tag). Gate 1, plan freeze, implementation authorization, and delivery gates remain unclaimed.

## Scope and prerequisite checks

- Ticket Scope items 1–5 and AC1–AC4 authoritative (`specs/PRODUCT.md` §3; recon-DEV-333-2 §1).
- Schema care list (§2.3 #3): nullable `enrichment_deferred_until` on existing movies table explicitly approved (brief Q2).
- Public API shape (§2.3 #4): unchanged.
- Architecture care list (§2.3 #2): new internal ports within existing projects; focused ADR; no owner checkbox (brief Q3).
- File scope (§2.3 #6): forced enrichment settlement/routing/repository predicate edits citing AC3 (brief Q1); discovery/scanner extension extraction citing Scope 1.
- **Phase B STOP**: amended DEV-426 must land first (brief Authority; Q7). If owner rejects DEV-426 amendment, return to Patron for `blocked: structural`.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-333/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md
├── tasks.md
└── DRAFTING_RECEIPT.md
```

`research.md`, `data-model.md`, `quickstart.md`, `contracts/`, and `checklists/` are not produced. Focused ADR for both new ports and the scope seam is authored by Keel in Phase A at `docs/adr/0032-recursive-import-ports-and-scope-seam.md` (brief ADR evidence closure; `recon-DEV-333-2-2` §§1–2; Status Proposed). Quill verifies/references that exact path from tasks; Quill does not author ADR bodies. T002/T004 gate Proposed→Accepted on applicable approval evidence only. Exact Candidate Movie Folder glossary text lands in root `CONTEXT.md` with this spec (Phase A). Delivery lands the three exact Plan-challenge round 1 §7 glossary entries in the same `CONTEXT.md`. Constitution Principle I + IV inventories, Reliability quota-settlement paragraph, ArchitectureTests whitelist, and Governance Sync Impact Report / version / Last Amended / `.specify/templates/*` review ship with delivery against the actual Phase B baseline and this ADR. Rebase/pickup rechecks ADR numbering and dependency drift.

### Source Code (repository root) — drafting targets from brief

```text
# Options
src/LamuFlix.Core/Options/LibraryOptions.cs
tests/LamuFlix.UnitTests/Options/LibraryOptionsTests.cs
tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs

# Ports + Import command
src/LamuFlix.Core/Ports/ILibraryFolderDiscovery.cs
src/LamuFlix.Core/Ports/IMovieFolderImporter.cs
src/LamuFlix.Core/Features/Import/ImportLibraryCommand.cs
src/LamuFlix.Core/Features/Import/ImportLibraryCommandHandler.cs
src/LamuFlix.Core/Features/Import/ImportLibraryResult.cs

# Discovery / shared extensions
src/LamuFlix.Infrastructure/FileSystem/FileSystemLibraryFolderDiscovery.cs
src/LamuFlix.Infrastructure/FileSystem/SupportedVideoExtensions.cs
src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs

# Scope seam + registration
src/LamuFlix.Infrastructure/Import/ScopedMovieFolderImporter.cs
src/LamuFlix.Api/HandlerRegistration.cs

# Deferral domain / mapping / migration / predicates
src/LamuFlix.Core/Domain/Movie.cs
src/LamuFlix.Infrastructure/Persistence/Records/MovieRecord.cs
src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/<EF timestamp>_AddEnrichmentDeferredUntil.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/<same>_AddEnrichmentDeferredUntil.Designer.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs

# Settlement / routing / topology (forced edits citing AC3)
src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentOutcome.cs
src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs
src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs
src/LamuFlix.Infrastructure/RabbitMq/EnrichmentRouting.cs
src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs

# Architecture / glossary / constitution (reference TelemetryConstants members only — not a write target)
tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs
CONTEXT.md
.specify/memory/constitution.md
docs/adr/0032-recursive-import-ports-and-scope-seam.md

# Tests (named proofs in tasks.md; UnitTests only for new domain; existing property-test locations)
tests/LamuFlix.UnitTests/FileSystem/...
tests/LamuFlix.UnitTests/Features/Import/...
tests/LamuFlix.UnitTests/Features/Enrichment/...
tests/LamuFlix.UnitTests/Domain/MovieEnrichmentDeferredUntilTests.cs
tests/LamuFlix.IntegrationTests/...
```

**Structure Decision**: Match brief Planned file responsibilities and recon-confirmed paths (`MovieConfiguration.cs`, `MovieRecord.cs`, `EfMovieRepository.cs` claim/stranded at :677/:697, `DirectoryMediaLibraryScanner.cs`, `HandlerRegistration.cs`). EF timestamp tooling-supplied. Do not invent unlisted filenames beyond the migration name pattern above. ADR path is the Keel-authored Phase A file above. Pure internal `IsContained` helper lives **inside** `FileSystemLibraryFolderDiscovery.cs` (no new file/port/layer). No `TelemetryConstants.cs` write target; no legacy `LamuFlix.Test` domain fallback; no empty `ImportLibraryCommand` validator file.

## Design (from settled brief)

1. **Quota join (Q1)**: Import completes and stages while exhausted; pause/resume is enrichment progress via QuotaDeferred; gate TryAcquireAsync unchanged; HTTP 429 stays RateLimited; DEV-426 retains chain implementation — non-final quota + next Found completes with zero deferral; only chain-final quota defers (T016 real-chain proof; T001 verifies landed eligibility).
2. **Durability (Q2)**: Persist EnrichmentDeferredUntil via `DeferEnrichmentUntil`; always publish same-attempt delayed message on enrichment.quota-deferred before ack (zero-clamped whole-ms TTL); claim-before-lookup and no-redeferral on early stray; claim/sweeper read application TimeProvider deadlines; sweeper post-expiry backstop only.
3. **Entry (Q3)**: ImportLibraryCommand in Core Features/Import; discovery + scoped importer ports; explicit test invocation; no validator / no empty stub.
4. **Candidates (Q4 / Plan-challenge round 1 §§3,5–6)**: Candidate Movie Folder rules; SupportedVideoExtensions shared holder; explicit stack of lazy enumerators (no call-stack recursion, no depth cap, no materialized child/file lists); host-selected lexical comparison (`RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` → OrdinalIgnoreCase; otherwise Ordinal); pure internal `IsContained(root, candidate, comparison)` inside `FileSystemLibraryFolderDiscovery.cs`; linked children never followed (Mock attribute/no-descent proof); linked configured root resolve-final-target only (Mock does not traverse linked contents — real-filesystem content proof only if that behavior is claimed).
5. **Pipeline (Q5)**: MaxConcurrency = capacity = consumer count; Wait; linked CTS shutdown; consumer-fault/caller-cancellation while writer blocked is explicit — no fictitious simultaneous producer-enumeration+WriteAsync fault.
6. **Result (Q6)**: ImportLibraryResult(ImportedCount); fail-fast; honest non-resuming rerun.
7. **Proofs/order (Q7)**: Named Method_Condition_Expected matrix with real SUT member names; ticket Scope item 5 cited on composed fake-gate proofs; DEV-426 first; gates from harness.yml.
8. **Loop (Q8)**: Critical/High/Medium block; Low dispositions; three axes; two+two rounds; frozen scope sentence; plan-challenge round cap 2 — this correction is round 1; do not mark freeze.

### Plan-detail mechanics (must not alter settled semantics)

- **Quota-deferred TTL (always publish; max-zero whole ms)**: Declare durable quorum `enrichment.quota-deferred` dead-lettering into `requested` (style of existing retry queue). At publish, compute `remaining = EnrichmentDeferredUntil − injected TimeProvider.GetUtcNow()` and set broker expiration to **`max(0, (int)remaining.TotalMilliseconds)`** (whole milliseconds). Publish in all cases — including at/after deadline with expiration 0 on the same durable path, one publish, original attempt. Do **not** skip publish, recompute a new deferral/day rollover, or reuse the 30s retry queue. Preserve save→publish→ack. Named FakeTimeProvider at/equal/after-deadline expiration-zero one-publish same-attempt proof is the **T019 composed consumer settlement/publish fixture** in `QuotaDeferredBrokerTests` (not `EnrichmentRouting.Decide` / settlement handler alone — those do not publish or compute TTL); existing near-midnight real-broker TTL proof remains separate durable-expiry evidence (fake clock does not advance the broker). Exact RabbitMQ argument/property wiring follows existing `RabbitMqTopology` patterns at implementation.
- **Claim-before-lookup / no-redeferral**: Consumer/settlement path claims before provider lookup. Early stray / pre-deadline redelivery: unclaimed no-op ack, zero provider calls, persisted deadline unchanged, zero additional deferral publish (Compass L-2; brief Q2 rules 3–4).
- **Claim clear**: Successful eligible `TryClaimForEnrichmentAsync` clears `enrichment_deferred_until` in the same `ExecuteUpdateAsync` as LastAttemptAt refresh (brief Q2).
- **Application TimeProvider deadline contract**: All deadline writes, claim/stranded comparisons, and TTL duration computation use injected application UTC samples (`TimeProvider.GetUtcNow()`); no database `now()` substitute (Sentry M4).
- **Trace + telemetry constants (Principle VI)**: Delayed quota-deferred publish injects `traceparent`/`tracestate` via the existing default text-map propagator (same path as ordinary enrichment publish). Consumer extract, `TelemetryConstants.EnrichmentProcess`, and ActivityLink-on-redelivery stay. Existing TelemetryConstants keys listed under Principle VI cover this surface — **reference only, not a write target**. Settled Q7 `quota-deferred` / `QuotaDeferred` names are routing/outcome vocabulary. No speculative new metric/span/tag-value constants in this plan; any actually necessary new telemetry name at implementation requires then-current evidence. Structured logging may follow existing `EnrichmentConsumer.LogOutcome` style without adapter-local telemetry key strings.
- **Movie mutator (Principle IV)**: Settlement calls `Movie.DeferEnrichmentUntil(DateTimeOffset)` without `MarkFailed`; sets `EnrichmentDeferredUntil`; legal only from Pending (no status change); illegal transitions throw `InvalidTransitionException`; Rehydrate/map include the new property; clearing only inside atomic claim. Named legal/illegal unit proofs are mandatory (tasks).
- **Payload-free command (brief Q3 / Principle V / Plan-challenge round 1 §2)**: `ImportLibraryCommand` is a sealed payload-free record registered only via `AddHandler` (ValidationDecorator always in pipeline; empty validator set legal). **No** `IValidator<ImportLibraryCommand>` registration and **no** empty stub file — Principle V satisfied by construction (no inputs; options startup-validated; payload-free sweep-command precedent). `LibraryOptions` carries MaxConcurrency validation (binder fractional/nonnumeric distinct from integer Range rejection).
- **Routing Disposition (Plan-challenge round 1 §8)**: Narrow extension of existing `Disposition(string Action, string RoutingKey, int? NextAttempt)` with optional `DateTimeOffset? DeferredUntil = null`; add `QuotaDeferred` action constant and one `Decide` arm → quota-deferred routing key, null `NextAttempt`, deadline; consumer uses delivered original `Attempt`. Existing failure arms unchanged; no enum/rewrite.
- **Discovery traversal (Plan-challenge round 1 §§3,5–6)**: Explicit stack of lazy enumerators (push/advance/pop+dispose); dispose stack on cancellation/fault; deterministic 1,000-level exact-candidate-set/completion proof (T007/T012). Host-selected comparison + internal `IsContained` both-mode proofs independent of runner host. Linked-child: CreateSymbolicLink / ReparsePoint / LinkTarget — no yield/no descent; never claim Mock traverses links. Linked-root: resolve final target and bind containment/canonical identity to resolved root only; content under a real linked root, if asserted, is real-filesystem integration evidence.

## Complexity Tracking

> No Constitution Check violations requiring justification / departure checkbox. Two new ports are ticket-mandated and ADR-gated (Principle I), not a departure. Principle IV mutator/claim/sweeper inventory updates and the Enrichment Reliability quota-settlement paragraph are Patron-ruled **compliant clarification** (Plan-challenge round 1 §1 / CONCLUSIONS), delivered via T004 against the actual Phase B baseline with Governance Sync Impact Report, version/date/template obligations — **no new owner checkbox**. Payload-free command has no validator by construction (Principle V). ADR Proposed→Accepted is approval-evidence-gated (T002/T004), never by drafting alone.
