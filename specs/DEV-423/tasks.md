# Tasks: Chain Metadata Providers

**Input**: `specs/DEV-423/` spec.md, plan.md, frozen brief.md (Q1–Q7 + PC1–PC6), CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-423, recon-DEV-423-2
**Status**: PLAN FROZEN for Gate 1 review by Keel (2026-10-26); Gate 1 not approved; Phase B blocked on DEV-331 T000/T001. See PLAN_CHALLENGE_ADJUDICATION.md final receipt.
**Tests**: Required (ticket item 5; Q5). TDD optional; unit tests may be drafted before T003 but compile only with the chain implementation; required coverage green before verification (PC6).
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. T002 has no `[P]` (PC6 / Compass F5).
**Phase B gate**: T000 (upstream landing) and T001 (pickup drift) **before any implementation task (T002–T021)**. Phase A drafting does not implement.
**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no new explanatory comments; preserve unrelated existing comments in edited files; no `DateTime.Now`; Theory+MemberData for the result matrix with categories derived from `EnrichmentFailureCategory`; vendor-neutral test type names; short resilience timing via `MetadataProviderResilienceOptions` / `Omdb:Resilience` only; no live vendors.

---

## Phase 0: Upstream readiness (blocks all implementation)

- [ ] T000 Confirm DEV-331 owner Q7 constitution resolution, amendment/ADR, and TMDb adapter implementation have landed on main; do not invent ADR ids or edit constitution in this ticket (Q1; FR-009)
- [ ] T001 After T000, run mandatory pickup drift check against main and read-only `/speckit-analyze`; confirm which OMDb/TMDb registration, BaseUrl-normalization, resilience, and secondary-health lines DEV-331 actually landed before T005; amend decisions/tasks only if drift requires a cited ruling — do not silently clear upstream departure (Q1, Q5; FR-009; PC4)

---

## Phase 1: Foundational — unit matrix and chain type (blocks composition stories)

**Purpose**: Prove fallback algorithm with fakes before DI wiring.
**Checkpoint**: US1 + US5 unit coverage green against `ChainedMetadataProvider`.

- [ ] T002 [US1] Create `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs` (namespace `LamuFlix.UnitTests.Adapters`, class `MetadataProviderChainTests`) with Theory/MemberData covering the full primary/secondary matrix with categories derived from `EnrichmentFailureCategory` (current 7×7; expands on category additions), secondary call suppression after primary Found or terminal Failed, exhaustion first-eligible-failure precedence, and both-NotFound → NotFound; include explicit unit guards for null → `ArgumentNullException`, empty → `ArgumentException`, and caller-list mutation after construction cannot change order. May be drafted before T003; compiles only with T003 (FR-001, FR-002, FR-006; Q3, Q5; PC2, PC3, PC6)
- [ ] T003 [US1] Create `src/LamuFlix.Infrastructure/Adapters/ChainedMetadataProvider.cs` implementing `IMetadataProvider` with `IReadOnlyList<IMetadataProvider>` constructor that rejects null with `ArgumentNullException` and empty with `ArgumentException`, then snapshots via `ToArray()` (no raw-capture primary constructor incompatible with those guards; no mutable static initialisation); eligibility is exact record equality to `RateLimited`/`ProviderUnavailable` with all other/future categories terminal; FR-002 fallback and FR-003 pass-through (no try/catch, no exception fallback) so the Phase 1 unit suite is green (FR-001–FR-003; Q2, Q3; PC3)
- [ ] T004 [US5] Extend `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs` for identical `MetadataLookup` (including `ManualImdbId`) and `CancellationToken` forwarding, caller-cancellation propagation, unexpected-exception propagation, one-provider equivalence, primary ProviderUnavailable + secondary NotFound → eligible primary failure, and confirm null/empty/snapshot mutation constructor rejection coverage remains present (FR-001, FR-003, FR-006; Q3 — same file as T002)

---

## Phase 2: User Story 2 & 3 — DI keys, sole unkeyed chain, disabled composition (P1)

**Goal**: Keyed omdb/tmdb + sole unkeyed chain; Enabled=false → OMDb-only list.
**Independent Test**: composition resolution + disabled zero secondary HTTP; existing enrichment regressions.

- [ ] T005 [US2] Edit `src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs` to replace only OMDb’s port-typed `AddHttpClient<IMetadataProvider, OmdbMetadataProvider>` with concrete `AddHttpClient<OmdbMetadataProvider>`; retain landed DEV-331 `AddHttpClient<TmdbMetadataProvider>` (BaseUrl trailing-slash normalization, independent resilience, secondary health) verbatim — do not re-author or duplicate; preserve private shared `Configure`, `MetadataProviderResilienceOptions` (`"Omdb:Resilience"`), and unrelated comments; add keyed transient `IMetadataProvider` forwards using `TelemetryConstants.OmdbProviderName` / `TmdbProviderName`; sole unkeyed transient factory builds `ChainedMetadataProvider` with ordered list `[omdb]` or `[omdb, tmdb]` from `TmdbOptions.Enabled`, resolving each entry exclusively via `GetRequiredKeyedService<IMetadataProvider>` (no duplicate-entry guard); never capture typed clients in a singleton (FR-004, FR-005, FR-008; Q2; PC4)
- [ ] T006 [US3] In the same extensions file (sequenced after T005), ensure `TmdbOptions.Enabled=false` omits TMDb from the chain list entirely (no resolve/invoke) while concrete TMDb registration and secondary health check remain available (FR-004; Q2; ticket item 4)
- [ ] T007 [US2] In `tests/LamuFlix.IntegrationTests/MetadataProviderCompositionTests.cs`, edit only type or behaviour assertions that pin unkeyed `IMetadataProvider` to bare `OmdbMetadataProvider` so they resolve `ChainedMetadataProvider`; preserve all unrelated health/telemetry/resilience assertions; broader drift requires a cited ruling first (FR-008; Q2, Q5; PC5)
- [ ] T008 [US2] Edit `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderCompositionTests.cs` **only if** landed DEV-331 pins unkeyed port resolution to bare `OmdbMetadataProvider` via type or behaviour assertions; restrict changes to those assertions and preserve unrelated health/telemetry/resilience assertions — otherwise skip and note N/A in the PR; broader drift requires a cited ruling (FR-008; Q5; PC5)

---

## Phase 3: User Story 2–4 — Integration composition, fallback E2E, observability (P1/P2)

**Goal**: Real typed-client proof of keys, disabled mode, fallback, telemetry, health isolation, breakers, RabbitMQ/readiness.
**Independent Test**: `MetadataProviderChainCompositionTests` + unchanged enrichment suite under Enabled=false.

- [ ] T009 [US2] Create `tests/LamuFlix.IntegrationTests/MetadataProviderChainCompositionTests.cs` asserting keyed `"omdb"`/`"tmdb"` resolution under both `TmdbOptions.Enabled` modes (direct keyed resolution is not disabled-chain construction), sole unkeyed `ChainedMetadataProvider`, preserved RabbitMQ `HasRegistration<IMetadataProvider>` / activation behaviour, and `/health/ready` exclusion for both metadata checks (FR-007; Q2, Q4; PC6)
- [ ] T010 [US3] Extend `MetadataProviderChainCompositionTests.cs` separately for disabled-chain construction with `TmdbOptions.Enabled=false`: zero TMDb resolve/invoke/HTTP; one-provider OMDb result equivalence; run existing enrichment regressions unchanged (FR-007; Q5; SC-003; PC6 — same file, not `[P]`)
- [ ] T011 [US1] Extend composition tests with WireMock end-to-end: OMDb NotFound → TMDb Found; OMDb Found → no TMDb call; OMDb RateLimited → TMDb called; OMDb InvalidResponse → no TMDb call (FR-007; SC-001–SC-002 — same file)
- [ ] T012 [US4] Extend composition tests for per-provider telemetry (enabled fallback → two `Metadata.Lookup` spans and two enrichment-duration samples tagged `omdb` then `tmdb`; disabled → existing one-provider telemetry tests untouched), health isolation split as (a) eligible primary failure then secondary Found preserving the primary degraded snapshot and (b) primary 401 → InvalidResponse with zero secondary calls and primary Unhealthy advice visible (fallback rules unchanged), and independent circuit breakers (FR-005, FR-007; Q4; SC-006 — same file)

---

## Phase 4: Polish & Phase B verification

- [ ] T013 Confirm no Core/options/config/secret/API/schema/`TelemetryConstants`/health-type/README/C4/`CONTEXT.md` edits landed outside the Q5 bounded set; confirm fallback is documented in `specs/DEV-423/` only (FR-008; Q5)
- [ ] T014 Run Roslyn analyzers on explicit changed `.cs` set via `./scripts/run-roslyn-analyzers.ps1` (FR-010; Q5)
- [ ] T015 Run `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml on changed files (FR-010)
- [ ] T016 Run `./scripts/run-jetbrains-inspectcode.ps1` on changed files (FR-010)
- [ ] T017 `dotnet format --verify-no-changes` clean (FR-010)
- [ ] T018 Run affected chain/composition/enrichment tests then full `dotnet test` (FR-010; SC-007)
- [ ] T019 Run property/security/vulnerable-package gates per harness; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS (FR-010; Q6)
- [ ] T020 Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree (FR-010; Q5)
- [ ] T021 Size M code review with Sentry, Ledger and Compass reports; apply Q6 closing bar (Critical/High/Medium block; Low non-blocking unless higher severity) and 2-round remediation cap; missing axis is blocked (FR-010; Q6)

---

## Dependencies

- **T000 → T001 before any T002–T021 implementation/verification** (Phase B gating preserved).
- T002 may be drafted before T003 but compiles only with T003 (no `[P]` marker; PC6). T004 after T002/T003 (same unit file). Required unit coverage must be green before verification.
- T005 → T006 same extensions file sequenced; T007 after T005; T008 after T005 (conditional).
- T009 after T005/T006 (keyed both Enabled modes); T010–T012 extend T009 file sequenced (T010 is the separate disabled-chain zero-secondary proof).
- T013 after implementation; T014–T021 after T013 and implementation complete.

## Sequencing examples

```text
# After T000+T001:
T002 MetadataProviderChainTests matrix   # optional draft before T003; compiles with T003
T003 ChainedMetadataProvider.cs          # Phase 1 unit suite green before verification
T004 same unit file pass-through cases

# After T005/T006:
T007 MetadataProviderCompositionTests bare-OMDb type/behaviour assertions only
T008 SecondaryMetadataProviderCompositionTests (conditional; same assertion narrowness)
T009 MetadataProviderChainCompositionTests keyed both Enabled modes
T010 disabled-chain zero secondary resolve/invoke/HTTP (separate from T009)
```

## MVP

T000–T007 + T009–T011 prove ticket AC (fallback triggers, Found short circuit, disabled enrichment unchanged) before full observability/breaker matrix (T012) and gates (T014–T021).

## Notes

- `[P]` = different files, no incomplete deps. T002 is deliberately unmarked (PC6 / Compass F5).
- Do not edit files the ticket/brief do not name without an explicit decision amendment (Q5; PC5).
- Quill never writes ADRs; constitution amendment remains DEV-331/Conductor.
- Non-blocking dispositions retained without new work: dual-budget latency (L2), RateLimited fan-out noted/no ticket (L3), ManualImdbId unit+real-client sufficiency (L6), no duplicate-entry guard (L4), distinct unit/integration names (S-04).
