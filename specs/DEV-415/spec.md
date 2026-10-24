# Feature Specification: DEV-415 Cache TasteProfile in HybridCache

**Feature Branch**: `feature/415-spec`

**Created**: 2026-10-24

**Status**: Draft — gate1: provisional

**Input**: Ticket DEV-415 (size M, parent DEV-288, no ui tag); settled grill brief `specs/DEV-415/brief.md` (grill closed by Patron Q10; Q1–Q10); facts from notes `recon-DEV-415` (sections 1–5 and corrected section 7) and `recon-DEV-415-2` (section 8; original recon section 6 superseded); Patron rulings in `specs/DEV-415/CONCLUSIONS.md`; product authority in `specs/PRODUCT.md` sections 1–3 and 5. No decisions beyond the brief and those rulings. No `design.md` (no ui tag). No clarify/checklist or ADR on this M path.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Repeated reads reuse one derived TasteProfile (Priority: P1)

When recommendations need the personal-library derived TasteProfile, the system returns the same computed profile on repeated reads within the configured freshness window without recomputing, while recommendation candidate filtering and other uncached genre/candidate work remain current and uncached.

**Why this priority**: Ticket AC requires cache on repeated reads; Q1 caches only the derived profile, never candidates or recommendation results.

**Independent Test**: With real HybridCache 10.11.0 and Testcontainers PostgreSQL, request the derived profile twice without intervening invalidating writes and assert one computation; assert candidates/genre work outside the cached entry stay uncached.

**Acceptance Scenarios**:

1. **Given** a personal library with qualifying viewing history and a cold miss already filled, **When** the derived TasteProfile is read again before invalidation or expiry, **Then** the same computed derived profile is returned without a second computation.
2. **Given** repeated derived-profile reads, **When** recommendation candidates or other genre/candidate work is evaluated, **Then** that work is not served from the TasteProfile cache entry.
3. **Given** the recommendations handler, **When** it needs the derived profile, **Then** it consumes that profile through the existing `IRecommendationProfileSource` port rather than recomputing it locally from caller-supplied genre weights.

---

### User Story 2 - Cold miss computes; empty only without qualifying history (Priority: P1)

A cold cache is a miss that computes the derived TasteProfile from persisted inputs. The result is empty only when there is no qualifying history—not merely because the cache is cold.

**Why this priority**: Ticket AC requires cold-cache empty-profile semantics; Q1 rejects returning empty solely because the cache is cold.

**Independent Test**: Cold miss with no qualifying history yields empty profile; cold miss with qualifying history computes a non-empty derived profile rather than returning empty.

**Acceptance Scenarios**:

1. **Given** a cold cache and no qualifying persisted history, **When** the derived TasteProfile is read, **Then** an empty profile is returned after computation from persisted inputs.
2. **Given** a cold cache and qualifying persisted history, **When** the derived TasteProfile is read, **Then** the profile is computed from persisted inputs and is not empty merely because the cache was cold.
3. **Given** any cold miss, **When** computation runs, **Then** all profile inputs including genre weights are obtained inside the factory-owned scope through existing calculators; the retrieval API accepts cancellation only (Q7).

---

### User Story 3 - Invalidate after persisted playback and feedback (Priority: P1)

After a PlaybackEvent or watched/not-interested set or unset is successfully persisted, the next derived-profile read recomputes. Failed persistence does not invalidate. Invalidation failure propagates through existing error handling without claiming the database write rolled back.

**Why this priority**: Ticket AC requires invalidation on playback and feedback; Q5 freezes post-successful-persistence hooks and generation-guarded next-read recomputation.

**Independent Test**: Persist playback then assert next read recomputes; prove watched/unwatched and not-interested/unset each invalidate and recompute after successful persistence; prove failed persistence does not invalidate; prove failed invalidation propagates.

**Acceptance Scenarios**:

1. **Given** a cached derived profile, **When** `EfPlaybackHistory.RecordAsync` successfully persists a PlaybackEvent, **Then** the cache is tag-invalidated and the next derived-profile read recomputes.
2. **Given** a cached derived profile, **When** `EfMovieRepository.SaveChangesAsync` successfully persists a watched or not-interested set or unset, **Then** the cache is tag-invalidated and the next derived-profile read recomputes.
3. **Given** persistence fails, **When** the failure completes, **Then** the TasteProfile cache is not invalidated.
4. **Given** persistence succeeds but invalidation fails, **When** the failure surfaces, **Then** it propagates through existing error handling and does not claim the committed database write rolled back.
5. **Given** a read that begins after completed invalidation, **When** HybridCache may still share an older in-flight fill, **Then** that read must not return an old-generation profile; it may await the old work then retry with cancellation until current (Q5).

---

### User Story 4 - Freshness, coalescing, cancellation, and failure recovery (Priority: P1)

Configurable expiry, application age envelope, same-key coalescing, owned-scope lifetime, and failure handling keep cached values correct under concurrent waits and cancellation without caching failures or empty fallbacks.

**Why this priority**: Ticket requires configurable expiry; Q6–Q8 freeze options, generation/age envelope, cancellation, and mandatory real-cache proofs.

**Independent Test**: Valid/invalid options and five-minute default; fake TimeProvider age boundary; same-key coalescing; barrier-controlled old fill across invalidation; one/all waiter cancellation plus request-scope disposal with owned-scope safety; factory failure/cancellation without cached error/empty fallback and later recovery.

**Acceptance Scenarios**:

1. **Given** `TasteProfileCache` options, **When** a positive Expiration binds (default five minutes), **Then** both Hybrid `Expiration` and `LocalCacheExpiration` match that duration; nonpositive duration fails startup.
2. **Given** a cached envelope with CreatedAt from injected TimeProvider, **When** age is greater than or equal to Expiration, **Then** the value is treated as a miss, evicted, and recomputed; advancing the fake TimeProvider proves application age, not native MemoryCache expiry.
3. **Given** multiple same-key callers, **When** they request the derived profile concurrently, **Then** they coalesce on one factory computation.
4. **Given** an in-flight fill started before invalidation, **When** a post-invalidation read joins that work, **Then** generation (and age) rejection prevents returning the old generation; retries are cancellation-aware and do not spin on completed stale values.
5. **Given** one waiter cancels while another continues, **When** shared factory work is still needed, **Then** the shared owned scope is not disposed and work is not cancelled for the remaining waiter; last-waiter cancellation or factory failure disposes the scope, does not cache failure or empty fallback, and allows a later read to recover.
6. **Given** the initiating request scope disposes while shared work continues, **When** the factory runs in its owned scope, **Then** scoped EF/calculation dependencies remain safe until the owned scope is disposed.

### Edge Cases

- Empty derived profile is allowed only when computation finds no qualifying history; cold miss alone never yields empty without that computation (Q1).
- Q7 supersedes Q4 caller-supplied genre weights: derived-profile retrieval takes `CancellationToken` only; factory snapshots generation and CreatedAt before reads and never relabels an old result as new.
- Conservative invalidation after successful repository saves is approved; existing `GenreWeightsCache` behavior is preserved (Q5).
- No sliding expiry extension on hits and no background refresh (Q6).
- Native L1 may evict earlier than the application age envelope; application freshness is independently enforced; no claim that MemoryCache uses TimeProvider (Q6; recon-DEV-415-2 section 8).
- Caller tokens govern cache waits/invalidation; the shared factory token governs owned-scope async reads; never bind factory lifetime to the first request token (Q8).
- No multi-server cache consistency claim; process-local L1 only, no distributed provider (Q4, Q8).
- Existing LocalPlay gating, launcher behavior, routes, DTOs, status codes, public HTTP/OpenAPI/web, schema/migration, and playback execution remain outside these edits (Q5, Q9).
- No new port, project, top-level folder, architectural layer, ADR, clarify/checklist, domain term, UI taste assumption, CONTEXT.md rewrite, or ASSUMPTIONS.md rewrite (Q9–Q10).
- Restore/CPM resolution, build, and runtime compatibility of Hybrid 10.11.0 remain Build-phase obligations; NuGet metadata and source evidence are not a passing restore verdict (Q3; recon-DEV-415-2 section 8).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST add central package pin `Microsoft.Extensions.Caching.Hybrid` exactly `10.11.0` in `Directory.Packages.props` and a direct unversioned package reference only from `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`. No Core Hybrid reference. No prerelease. Later restore/build/audit MUST prove CPM resolution (ticket Scope item 1; Q2–Q3; recon-DEV-415-2 section 8 lines 10–15).
- **FR-002**: Infrastructure MUST call `AddHybridCache` inside existing persistence registration (`PersistenceServiceCollectionExtensions` / `AddLamuFlixPersistence`), process-local L1 only, with no distributed provider (Q4; recon-DEV-415-2 section 8 lines 18–21).
- **FR-003**: Infrastructure MUST bind validated `TasteProfileCacheOptions` from configuration section `TasteProfileCache` with `Expiration` default five minutes and strictly positive `TimeSpan` validated at startup. Both Hybrid `Expiration` and `LocalCacheExpiration` MUST match that duration. No sliding expiry or background refresh. No Core options dependency (ticket Scope item 2; Q6).
- **FR-004**: Core MUST extend existing `IRecommendationProfileSource` with cancellation-only derived-profile retrieval `GetTasteProfileAsync(CancellationToken)` returning `TasteProfile` through the repository-consistent async return convention, while retaining existing `GetProfileInputsAsync`. No new port (Q4, Q7, Q9).
- **FR-005**: `EfRecommendationProfileSource` MUST remain the uncached concrete and MUST implement derived retrieval through existing `GenreWeightsCalculator` and `TasteProfileCalculator` inside its own work; the cached adapter MUST resolve this concrete in a factory-created/disposed scope and MUST NOT recursively resolve the cached port registration (Q4, Q7, Q9).
- **FR-006**: Infrastructure MUST add singleton `HybridCachedRecommendationProfileSource` that implements the existing profile port and exposes internal invalidation to Infrastructure callers. The singleton MUST hold only HybridCache, scope factory, TimeProvider, and validated options; it MUST NEVER capture a request `DbContext`, scoped source, or scoped adapter. Register the same singleton instance for the port and for invalidation callers (Q4, Q6, Q9).
- **FR-007**: The cache MUST use one fixed trusted key and one fixed trusted tag. The cached envelope MUST contain the derived `TasteProfile`, operation generation, and `CreatedAt`. Factory MUST snapshot generation and injected time before reads; recheck generation and age before return; mismatched generation or age greater than or equal to Expiration MUST evict and recompute with cancellation-aware awaited retries rather than spinning on completed stale values. No versioned-key accumulation or general cache framework (Q5–Q7).
- **FR-008**: All profile inputs, including genre weights, MUST be obtained inside the factory-owned scope through existing calculators. Each retained input-method EF read on the adapter MUST also own a scope (Q6–Q7).
- **FR-009**: After successful persistence only, `EfPlaybackHistory.RecordAsync` and `EfMovieRepository.SaveChangesAsync` MUST invoke internal invalidation on the shared singleton adapter for playback events and watched/not-interested set and unset. Preserve existing `GenreWeightsCache` behavior. Failed persistence MUST NOT invalidate. Invalidation failure MUST propagate without silent stale success or fictitious rollback (ticket Scope item 3; Q5, Q9).
- **FR-010**: `GetRecommendationsQueryHandler` MUST consume the derived profile through the existing port. Any other required genre/candidate work MUST stay uncached (Q4, Q7, Q9).
- **FR-011**: Caller cancellation MUST flow through cache waits and invalidation; the shared factory token MUST flow into owned-scope async reads. One canceled waiter MUST NOT dispose shared scope or cancel work needed by another. Last-waiter cancellation or factory failure MUST dispose scope, MUST NOT cache failure or empty fallback, and MUST allow later recovery (Q8).
- **FR-012**: Proof MUST use existing xUnit v3, Shouldly, NSubstitute, and property-test conventions. Infrastructure behavior MUST use real HybridCache 10.11.0 and Testcontainers PostgreSQL; never mock driver translation/serialization or introduce EF InMemory. Command interception, counters, and barriers MAY instrument tests without production test hooks. Required scenarios are those listed in brief Test strategy items 1–8 (ticket tests; Q8).
- **FR-013**: Architecture tests MUST keep Hybrid out of Core and avoid new ports/layers (Q8–Q9).
- **FR-014**: The system MUST NOT add a new dependency beyond ticket-authorized Hybrid; MUST NOT change public HTTP/OpenAPI/generated TypeScript/web; MUST NOT add schema/migration; MUST NOT touch playback execution, LocalPlay, secrets, or `Process.Start`; MUST NOT add a new port/project/top-level folder/ADR/clarify/checklist; MUST NOT rewrite CONTEXT.md or ASSUMPTIONS.md; MUST NOT invent a new domain term or UI taste assumption. Process-local cache introduces no remote health dependency; existing telemetry conventions apply only where required by gates (Q9–Q10).
- **FR-015**: Later Build MUST earn applicable configured harness gates (Roslyn, complexity, InspectCode; unit/integration/architecture/property tests; vulnerability audit; format; mutation where eligible) without lowering thresholds. Scope-empty, opt-out, N/A, and could-not-run retain distinct meanings and never become PASS (Q9–Q10).

### Key Entities

- **Derived TasteProfile**: Personal-library taste profile computed from persisted inputs and genre weights; the sole cached value for this feature.
- **TasteProfile cache envelope**: Cached payload holding derived profile, operation generation, and CreatedAt for freshness and generation checks.
- **TasteProfileCache options**: Infrastructure configuration section binding Expiration (default five minutes, strictly positive).
- **Playback Event**: Persisted playback history write that invalidates the profile cache after successful save.
- **Feedback (watched / not-interested)**: Persisted set/unset movie flags that invalidate the profile cache after successful repository save.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Repeated derived-profile reads within freshness and without invalidating writes reuse one computed profile; candidates remain uncached.
- **SC-002**: Cold miss with no qualifying history yields empty profile; cold miss with qualifying history computes rather than returning empty because the cache was cold.
- **SC-003**: After successful playback persistence, the next derived-profile read recomputes; after each successful watched/not-interested set or unset persistence, the next read recomputes.
- **SC-004**: Valid Expiration binds with five-minute default; nonpositive Expiration fails startup; application age at or beyond Expiration forces recompute under fake TimeProvider without claiming native MemoryCache clock behavior.
- **SC-005**: Same-key callers coalesce; a post-invalidation read never returns an old-generation profile even if it joined older in-flight work; cancellation and owned-scope rules hold; factory failure is not cached and later reads recover.
- **SC-006**: Failed persistence does not invalidate; failed invalidation propagates without claiming database rollback.
- **SC-007**: Applicable configured gates run without lowered thresholds; skipped/opt-out/N/A/could-not-run verdicts are never recorded as PASS.

## Assumptions

- No `[assumed]` taste items were recorded for this ticket; there is no `ASSUMPTIONS.md`, and none is created here (brief Q10).
- No owner structural checkbox is open. Anything not decided in `brief.md` / `CONCLUSIONS.md` is a `needs decision:` item, not an assumption invented here.
- Exact string literals for the single trusted cache key and tag are implementation constants inside the approved adapter, constrained by FR-007’s one-key/one-tag rule; they are not a separate product decision.
- Facts for existing symbols and Hybrid behavior come from `recon-DEV-415` and `recon-DEV-415-2`; proposed new types/files are approved design decisions in the brief, not claims those seams already exist.
- Q7 expressly supersedes the Q4 caller-weight signature; Q6 fixes singleton adapter lifetime.
