# Implementation Plan: Reconcile Enrichment Attempt Counting

**Branch**: `feature/390-spec` | **Date**: 2026-10-06 | **Spec**: specs/DEV-390/spec.md

**Input**: `spec.md`, `brief.md`, `CONCLUSIONS.md` (Q1-Q6), `ASSUMPTIONS.md`, recon-DEV-390

## Summary

Remove only the `EnrichmentAttempts++` line from `Movie.BeginPendingAttempt` so the persisted counter equals cumulative successful atomic claims, keep repository claim SQL and D2/D8 persistence behavior untouched, narrow the `CONTEXT.md` glossary to the accepted contract, and prove exact N/N+1/N+2 counts with tests-first PostgreSQL seam cases plus updated domain/property/handler tests.

## Technical Context

**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: none new. EF Core, Npgsql, xUnit v3, Shouldly, FsCheck, and Testcontainers fixtures already present.
**Storage**: PostgreSQL via existing `PostgresFixture`; no schema change, no migration, no backfill.
**Testing**: xUnit v3 + Shouldly; FsCheck property tests for the domain transition invariant; Testcontainers for the real claim/domain seam; no EF/query-translation mocks.
**Target Platform**: existing backend services (no platform change)
**Project Type**: backend bug fix in the existing feature-organised ports-and-adapters core (`LamuFlix.Core`, `LamuFlix.Infrastructure`)
**Performance Goals**: N/A (counting correction; no throughput target)
**Constraints**: Core stays persistence-independent; no new project, folder, layer, or abstraction; frozen file set per FR-007; `TimeProvider` for lease advancement, no sleeps.
**Scale/Scope**: M ticket, ~3h estimate; one-line production change plus glossary and test updates.

## Constitution Check

- Principle I (ports and adapters, feature-organised core): PASS. No new port, adapter, project, folder, or layer; the production edit stays inside the `Movie` domain transition; repository claim SQL, D8 claim sync, and D2 baseline/delta behavior stay unchanged; no feature-folder cross-reference is introduced.
- Principle II (explicit handlers, no MediatR): PASS. No handler registration, dispatch, decorator-order, or exception-mapping change; `EnrichmentRequested` and `EnrichmentFailureDecision` shapes stay unchanged.
- Principle IV (enrichment state machine): PASS under the Q1/Q2 ruled contract change. Atomic claim semantics stay untouched; `EnrichmentStatus` values, failure categories, retry-category policy, and sweeper behavior stay unchanged; only the outcome-side `EnrichmentAttempts++` in `BeginPendingAttempt` is removed.
- Principle VII (configuration isolation, deterministic time): PASS. No secret, path, options-record, `Features:LocalPlay`, or `Process.Start` change; tests advance the lease through the injected `TimeProvider` with no sleeps and no `DateTime.Now`-family calls.
- Principle VIII (ubiquitous language): PASS. No new domain term; the `CONTEXT.md` edit narrows the existing `EnrichmentAttempts` entry and adds the separate transport retry Attempt term using the [assumed] Q6 vocabulary; no Portuguese identifiers.
- Principle IX (test pyramid, real infrastructure): PASS. xUnit v3 with Shouldly assertions; FsCheck transition-table invariant for domain actions; real PostgreSQL/Testcontainers seam proof in `EfMovieRepositoryTests.cs` using its existing fixtures; no EF/query-translation mocks and no new test framework or mocking library.
- Workflow gates (PR quality gates and static-analysis gates): Roslyn analyzers, cyclomatic complexity, InspectCode on the changed diff (no `-All`), property tests (required, no opt-out), vulnerable packages, `dotnet format --verify-no-changes`, `dotnet test`, and `scripts/run-mutation.ps1` for changed production Core C# (expected applicable; verdict comes from the script only; never `since` from a worktree). Thresholds and configuration come from `harness.yml` unchanged. Gate outcomes follow brief.md:140-142.
- PRODUCT care-list authorization (separate from the constitutional validation above): Patron Q4 ruling authorizes the frozen file set under PRODUCT.md section 5 item 6: `Movie.cs` is ticket-named; `CONTEXT.md` is forced by DEV-390:42; the listed test edits are forced by DEV-390:45-46. No care-list item 1-5 trigger: no dependency, project/layer, schema (recon:155), public API shape, `LocalPlay`, secret, or `Process.Start` change.
- No ADR, no design.md (M non-ui ticket, no ui: tag).
- No constitution departure; Gate 1 user merge of the spec PR is the owner gate.
- Status: PASS, no departures.

## Project Structure

```text
src/LamuFlix.Core/Domain/Movie.cs                                          (edit: remove outcome-side increment only)
CONTEXT.md                                                                  (edit: narrow EnrichmentAttempts glossary + wire Attempt term)
specs/DEV-390/
├── spec.md               # /speckit-specify output
├── plan.md               # This file (/speckit-plan output)
└── tasks.md              # /speckit-tasks output
tests/LamuFlix.Test/Domain/MovieTests.cs                                   (edit: counter assertions)
tests/LamuFlix.Test/Domain/PropertyTests.cs                               (edit: transition-table invariant)
tests/LamuFlix.UnitTests/Features/Enrichment/ApplyEnrichmentResultCommandHandlerTests.cs      (edit)
tests/LamuFlix.UnitTests/Features/Enrichment/RecordEnrichmentFailureCommandHandlerTests.cs     (edit)
tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs           (edit)
tests/LamuFlix.UnitTests/Features/Enrichment/RequestEnrichmentCommandHandlerTests.cs           (edit)
tests/LamuFlix.UnitTests/Features/Enrichment/RequeueStrandedMoviesCommandHandlerTests.cs      (edit)
tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs                  (edit: new real-seam cases, existing fixtures)
tests/LamuFlix.IntegrationTests/EfMovieRepositoryClaimConcurrencyTests.cs  (edit)
tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs                 (edit)
```

**Structure Decision**: Existing layout is kept; no new project, folder, or test project. New seam cases stay in `EfMovieRepositoryTests.cs` using its existing fixtures.

## Design

1. **Domain fix (FR-003)**: delete only the `EnrichmentAttempts++;` line in `BeginPendingAttempt` (`Movie.cs:129-134`). `RequirePending`, `LastAttemptAt = now`, and every `Mark*` status/metadata/`EnrichedAt`/failure-category effect stay byte-identical.
2. **Persistence untouched (FR-004)**: claim SQL, D8 claim sync (stamp `LastAttemptAt` from in-process `now`, no-tracking re-read of `EnrichmentAttempts`, set as current and original values), and D2 baseline/delta `ApplyAttempts` stay as they are. Post-fix the aggregate-minus-baseline delta on the outcome path is zero, so saves preserve the claimed N+1. Adapter simplification is noted, no ticket (Q4 amendment).
3. **Wire boundary untouched (FR-005, FR-006)**: no edits to `EnrichmentRequested`, `EnrichmentRetryPolicy`, handlers, or `LeaseAwareMovieRepository`. Manual retry keeps NotFound/Failed-only acceptance, history preservation, save-before-enqueue order, and wire Attempt 1.
4. **Glossary (FR-007, User Story 3)**: rewrite the `EnrichmentAttempts` entry to cumulative successful claims (starts 0, +1 per successful claim, +0 for refused claims and all domain actions, monotonic, never reset, preserved across manual retries, historical pre-DEV-390 overcount caveat) and add the separate transport retry Attempt entry. No other `CONTEXT.md` edits.
5. **Known limitation (accepted, no ticket)**: ADR-0017 sweep behavior (requeue restarts at Attempt 1, `MaxAttempts` not applied across sweeps) is existing and documented; unchanged.

## Test Strategy

- **Order**: tests first. Write/update the exact-value contract tests (changed-contract cases fail before the fix; regression guards may pass before the fix), show the claim-plus-outcome double-count defect failing, then apply the domain fix and glossary, then full verification.
- **PostgreSQL seam** (`EfMovieRepositoryTests.cs`, Testcontainers, existing fixtures, N > 0, FR-004 mapped to both sequences): successful claim N+1; claim plus each outcome (success/not-found/terminal failure) stays N+1 via two sequences — (a) fresh load: successful claim with no aggregate preloaded, then load aggregate and baseline at N+1, apply each outcome, save and reload N+1; (b) preloaded before claim: aggregate loaded at N before claim, D8 sync during the successful claim, outcome delta zero, persisted reload N+1 per outcome; watchlist save preserves claimed count; refused/double/concurrent claims add zero beyond winning claims (retain concurrency proof); Retry/RetryDelayed without `Mark*` leaves N+1, next successful claim N+2, final outcome N+2; manual retry saves then enqueues wire 1 with count N, next claim N+1, final outcome N+1; enqueue/requeue alone unchanged. Lease via injected `TimeProvider`, no sleeps.
- **Domain** (`MovieTests.cs`): every legal `Mark*` keeps count N with existing timestamp/state effects; `RequestEnrichment` and rejected transitions keep history; drop the `+1` snapshot expectations.
- **Property** (`PropertyTests.cs`): FsCheck transition-table invariant becomes count-unchanged for domain actions; all other expectations kept.
- **Handler unit**: assert counts where deterministic under the new contract (Apply/RecordFailure/Process/Request/Requeue files); keep manual-retry rejection and save-before-enqueue ordering.
- **Consumer** (`EnrichmentConsumerTests.cs`): secondary to the real seam; tighten `>= 2` assertions to exact counts where deterministic.
- **Round trips kept**: rehydration and persistence-model arbitrary stored-value assertions stay intact.

## Gates

Thresholds and configuration come from `harness.yml` unchanged. Phase A runs no gates. Expected Phase B checks: Roslyn analyzers, cyclomatic complexity (tightened at refactor), InspectCode on the changed diff (no `-All`), property tests (required, no opt-out), vulnerable packages, `dotnet format --verify-no-changes`, `dotnet test`, and `scripts/run-mutation.ps1` for changed production Core C# (expected applicable; verdict comes from the script only; never `since` from a worktree). Web checks have no scope (`/web` unchanged). Exit 0 records PASS only for the check actually completed; exit 1 (FAILED) or Could not run blocks; gate-disabled opt-out is SKIP never PASS; mutation NOT APPLICABLE only on the script's exit-2 verdict, never a waiver; scope-empty exit 2 is non-blocking SKIPPED (scope-empty), never PASS, per the standing owner ruling; unaccepted property-test exit 2 blocks.

## Complexity Tracking

No constitution violations; nothing to justify.
