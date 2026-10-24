# Implementation Plan: DEV-415 Cache TasteProfile in HybridCache

**Branch**: `feature/415-spec` | **Date**: 2026-10-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/DEV-415/spec.md`, settled grill brief `specs/DEV-415/brief.md` (Q1–Q10; grill closed by Patron Q10), facts from notes `recon-DEV-415` (sections 1–5 and corrected section 7; original section 6 superseded) and `recon-DEV-415-2` (section 8), Patron rulings in `specs/DEV-415/CONCLUSIONS.md`, product authority in `specs/PRODUCT.md` sections 1–3 and 5.

**Note**: Planning artifact only (Phase 2 Spec Kit drafting). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1, and until Phase B entry evidence is complete. No `design.md` (no ui tag). No clarify/checklist or ADR on this M path. Gate 1 is not claimed.

## Phase B entry preflight (executable before Code Phase 1)

Schedule these Build-workspace checks before T001. Do not run gates, tests, or analyzers from Spec. Missing evidence blocks code.

| Check | Owner / evidence | Blocks code if missing |
|---|---|---|
| Spec PR merged and Gate 1 established | Rigger / YouTrack `Ready to build` handoff in `DEV-415` | Yes |
| Phase B recon drift vs `origin/main` for the authorized file set; reconcile against frozen decisions; any new fact gap routes recon (brief task order item 1) | Wisp → `recon-DEV-415` refresh | Yes |
| Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef`) | Gauge → recorded in `DEV-415` | Yes |
| Keel drift check or amend against brief/spec before code | Keel handoff in `DEV-415` | Yes |

## Summary

Deliver one process-local derived TasteProfile cache via stable `Microsoft.Extensions.Caching.Hybrid` 10.11.0 registered in Infrastructure: single trusted key/tag, validated configurable expiry (default five minutes), factory-owned scopes, generation and TimeProvider age envelope, and post-successful-persistence invalidation from `EfPlaybackHistory` and `EfMovieRepository` for playback and watched/not-interested set/unset. Extend existing `IRecommendationProfileSource` with cancellation-only `GetTasteProfileAsync`; keep `EfRecommendationProfileSource` uncached; expose one singleton `HybridCachedRecommendationProfileSource` for the port and invalidation. Handler consumes the derived profile through the existing port; candidates stay uncached. Prove behavior with real HybridCache and Testcontainers PostgreSQL per brief test strategy.

## Technical Context

**Language/Version**: C# / .NET 10 backend. No web change.

**Primary Dependencies**: Add `Microsoft.Extensions.Caching.Hybrid` 10.11.0 (CPM + direct Infrastructure reference only). NuGet metadata establishes net10.0 assets and Caching.Abstractions/Memory, Logging.Abstractions, Options 10.0.13 constraints (recon-DEV-415-2 section 8 lines 10–15); restore/build/audit remain Build obligations. No other new NuGet/npm dependency.

**Storage**: No schema or migration. Cache is process-local HybridCache L1 only; no distributed provider. Persisted inputs remain existing playback/feedback/library data.

**Testing**: xUnit v3; Shouldly; NSubstitute; property-test conventions. Real HybridCache 10.11.0 and Testcontainers PostgreSQL for Infrastructure behavior. Never mock driver translation/serialization or use EF InMemory. Command interception, counters, and barriers may instrument tests only. Existing known targets include recommendation handler tests/property arrangements, calculator/property regressions, `EfRecommendationProfileSourceTests`, and `EfPlaybackHistoryTests`; add owned-scope Hybrid integration coverage in the existing integration project. Additional fixture edits only when constructor/contract changes force them. All gate/test execution belongs to Phase B / Build workspace.

**Target Platform**: Existing `LamuFlix.Api` host. No public HTTP surface change. No new external health endpoint for this process-local cache.

**Project Type**: Existing ports-and-adapters solution. Edits stay in Core ports/features and Infrastructure persistence. No new project or layer.

**Performance Goals**: None in the brief. None added. Application age envelope bounds approximation of time-dependent profile values; native L1 may evict earlier.

**Constraints**: TreatWarningsAsErrors. Ticket scope authoritative. No LocalPlay/secrets/`Process.Start`. No public HTTP/OpenAPI/web. No schema/migration. No new port/ADR. Mutation never uses `--since`. Thresholds never lowered. Scope-empty/opt-out/N/A/could-not-run never PASS. PRODUCT Epic 9 sequencing not waived.

**Scale/Scope**: Authorized file set in brief Approved implementation file set. Unanticipated files return to Keel/recon.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). Ticket DEV-415 Scope items 1–4 and AC match brief frozen scope. Owner structural checkboxes: none (Q10).
- Dependency care list (§2.3 #1; PRODUCT §5 #1): ticket lines 17 and 20 authorize Hybrid; Patron Q2–Q3 freeze stable 10.11.0 Infrastructure-only pin (constitution I / Technology Stack).
- No new project/folder/layer (§2.3 #2): one concrete Infrastructure adapter for existing port (Q4; PRODUCT §2 non-goals).
- Database schema (§2.3 #3): no migration; cache is process-local only.
- No public API shape change (§2.3 #4): no routes/DTOs/status codes; no OpenAPI or generated TypeScript (Q9).
- No LocalPlay/secrets/`Process.Start` (§2.3 #5): excluded (Q5, Q9).
- File scope (§2.3 #6): bounded edits to brief-approved files; AC-forced existing-file edits ruled by Patron Q9.
- Principle I: Core stays behind existing `IRecommendationProfileSource`; no Core Hybrid reference; no new port/ADR (Q4, Q7, Q9).
- Principle VII: injected TimeProvider for CreatedAt/age; no secrets in source (Q6).
- Principle IX: real HybridCache + Testcontainers PostgreSQL; never mock Infrastructure drivers; unit coverage for pure freshness/generation/handler paths (Q8).
- Static-analysis gates: configured scripts; no threshold waiver; scope-empty SKIPPED never PASS (Q9–Q10).

Post-design re-check: the design below adds the ruled package/options/adapter/port extension/handler consumption/invalidation hooks with no public HTTP contract, no schema migration, no new port/ADR, and no harness edit. The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-415/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md
├── tasks.md
└── DRAFTING_RECEIPT.md
```

`research.md`, `data-model.md`, `quickstart.md`, `contracts/`, and `checklists/` are not produced. The brief already freezes approach, files, invalidation, freshness, cancellation, and proof obligations. There is no new public HTTP contract. No `design.md` (no ui tag). No clarify/checklist or ADR on this M path (brief Q9–Q10; Conductor drafting ask).

### Source Code (repository root)

```text
# Package / project
Directory.Packages.props
src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj

# New Infrastructure
src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs
src/LamuFlix.Infrastructure/Persistence/TasteProfileCacheOptions.cs

# Extended Infrastructure
src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs
src/LamuFlix.Infrastructure/Persistence/EfPlaybackHistory.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs

# Extended Core
src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs
src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs

# Tests (existing projects; exact fixtures follow Build layout)
tests/LamuFlix.UnitTests/...
tests/LamuFlix.IntegrationTests/...
tests/LamuFlix.ArchitectureTests/...
```

No `/web` edit. No `harness.yml` or gate-script edit. No migration file. No CONTEXT.md rewrite.

**Structure Decision**: Register Hybrid and the singleton cached adapter beside existing persistence DI (`AddLamuFlixPersistence`). Keep uncached `EfRecommendationProfileSource` as the scoped concrete resolved inside owned scopes. Extend the existing Core port; do not introduce a second profile port. Hook invalidation after successful saves in existing Infrastructure persistence types so LocalPlay/launcher/public API stay untouched (Q5).

## Design Overview

1. **Package and DI (Q2–Q4, Q6, Q9).** Pin Hybrid 10.11.0 centrally; Infrastructure-only reference. In `AddLamuFlixPersistence`: `AddHybridCache` (process-local L1); bind/validate `TasteProfileCacheOptions` (`TasteProfileCache`, default Expiration five minutes, strictly positive); register uncached `EfRecommendationProfileSource` for owned-scope resolution; register one singleton `HybridCachedRecommendationProfileSource` as `IRecommendationProfileSource` and as the invalidation target. Singleton dependencies: HybridCache, `IServiceScopeFactory`, TimeProvider, validated options only.

2. **Port and uncached computation (Q4, Q6–Q7).** Extend `IRecommendationProfileSource` with `GetTasteProfileAsync(CancellationToken) -> Task<TasteProfile>` (repository-consistent async convention). Retain `GetProfileInputsAsync`. Uncached concrete computes all inputs including genre weights via existing `GenreWeightsCalculator` and `TasteProfileCalculator`. Cached adapter never recursively resolves the cached port. On the singleton adapter, each retained `GetProfileInputsAsync` call creates and disposes a fresh owned scope, resolves the uncached concrete directly (no recursive port resolve), and propagates the caller cancellation token (FR-008 / brief Q6).

3. **Cached adapter (Q5–Q8).** One fixed trusted key and tag; envelope holds profile, generation, CreatedAt. Each factory envelope keeps a pre-read generation snapshot (and CreatedAt/time). Factory creates/disposes own scope and obtains all inputs in that scope. After every awaited cache/fill result, compare that snapshot with the same singleton adapter's current generation and application age; stale results (generation mismatch or age >= Expiration) use the frozen eviction and cancellation-aware awaited retry path and never relabel old results. Internal `InvalidateAsync` (or equivalent internal API), used only from Infrastructure persistence hooks, MUST advance that same singleton adapter current generation with concurrency-safe increment/read semantics before reporting successful completion.

4. **Invalidation hooks (Q5).** After successful `SaveChangesAsync` in `EfPlaybackHistory.RecordAsync` and `EfMovieRepository.SaveChangesAsync`, invalidate via the shared singleton. Cover watched and not-interested set and unset. Preserve `GenreWeightsCache` invalidation behavior. Persistence failure skips TasteProfile invalidation; invalidation failure propagates.

5. **Handler (Q4, Q7).** `GetRecommendationsQueryHandler` obtains derived profile through the port; other genre/candidate work remains uncached.

6. **Proof order (Q8; brief test strategy).** Build pickup drift/baseline → package restore/options/DI → port + uncached computation → singleton adapter (hit/cold/expiry/generation/cancellation) → handler + persistence invalidation hooks → required unit/integration/race/cancellation/regression/architecture coverage → gates/review loop (closing bar and two-round cap per Q10).

## Loop discipline

- **Closing bar:** zero unresolved Critical, High, or Medium findings within frozen scope; all applicable gates earned; all three Risk, Standards, and Spec reports present for size M. A missing axis blocks. Scope-empty or opt-out is skipped, never pass; an unrunnable gate has not passed. Low and out-of-scope findings follow charter follow-up policy. Unnecessary abstraction is a follow-up rather than a forced fix (Q10).
- **Round cap:** two numbered fix/challenge rounds per applicable review loop. Remaining blockers at the cap stop the loop and return to Conductor; no silent third round or weakened bar (Q10).
- **Frozen scope:** single process-local derived TasteProfile cache; fixed trusted key/tag; validated configurable expiry; invalidation after successful persisted playback and watched/not-interested set/unset; factory-owned scope; generation/freshness/cancellation correctness; and the brief test strategy. Anything else is a follow-up issue, not a finding in this round.
- **Gate verdicts:** preserve scope-empty SKIPPED, opt-out SKIP, mutation N/A, and Could not run distinctions; never PASS.

## Complexity Tracking

> No constitution violations requiring justification. File placements are Patron-ruled under ticket scope / §2.3 item 6 without owner checkboxes. Package is ticket-authorized with Patron version ruling. No ADR.
