# Tasks: TMDb Metadata Provider Adapter

**Input**: `specs/DEV-331/` spec.md, plan.md, frozen brief.md (Q1–Q8, Q3a, Q9) + PC1–PC7 addenda, CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-331, recon-DEV-331-2
**Tests**: Required (Q6). Recorded JSON + WireMock matrix is the deliverable proof. TDD optional; fixture-first and all required tests before verification (Ledger L2).
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`.
**Gate 1**: Owner checkbox in `spec.md` must be resolved and constitution amendment/ADR landed (T026) **before any Phase B implementation task (T001–T025)** executes. Phase A drafting does not implement.
**Repair**: Plan-challenge repair round 2 FINAL of max 2. Task IDs T001–T034 preserved.

**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no new explanatory comments; preserve unrelated existing comments in edited files; no `DateTime.Now` (BannedSymbols via existing build/analyzers); Theory+MemberData for repeated cases; vendor-neutral test/helper type names (`SecondaryMetadataProvider*`; PC5 / Principle VIII / `recon-DEV-331:45`); short resilience timing via `Omdb:Resilience` options only; no live TMDb; no mocked HTTP transport; no stub `HttpMessageHandler`; no `InternalsVisibleTo`.

---

## Phase 0: Owner landing (blocks all implementation)

- [ ] T026 Confirm constitution amendment + ADR covering Q7 adapter row and Core TmdbOptions are landed as owner-approved before any T001–T025 work; do not invent ADR ids in feature code tasks (Q7; brief; PC6 — precedes implementation, not only gates)

## Phase 1: Foundational options, telemetry constant, fixtures (blocks stories)

- [ ] T001 Create recorded sanitized official TMDb JSON fixtures under `tests/LamuFlix.IntegrationTests/Fixtures/SecondaryMetadata/` covering Found details, empty search/find movie_results, malformed/wrong-shaped bodies, adversarial poster_path shapes (scheme/host/backslash/control/dot segments), and status/error payload shapes; as a future Phase B edit only, update `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` solely to include `Fixtures/SecondaryMetadata/**/*.json` with `CopyToOutputDirectory=PreserveNewest` (preserving relative output directories) — no package/project-reference/other-fixture/settings changes; fixture reads use `Path.Combine(AppContext.BaseDirectory, "Fixtures", "SecondaryMetadata", …)` only — no cross-project access (FR-014, FR-015; Q6; PC2; PC4 / Compass F5; PC7; recon-DEV-331-2)
- [ ] T002 Create `src/LamuFlix.Core/Options/TmdbOptions.cs` with `SectionName = "Tmdb"`, `Enabled` default false, `BaseUrl` default `https://api.themoviedb.org/3/`, `ApiKey` default empty; BaseUrl Required+Url+ private nested `AllowedBaseUrlAttribute` https-or-loopback declared inside this file only (no `OmdbOptions.cs` edit, no shared validator file); ApiKey not Required (FR-012; Q4; PC6 / Compass F1; **requires owner checkbox / constitution resolution**)
- [ ] T003 [P] Add `public const string TmdbProviderName = "tmdb";` to `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` (FR-009; Q6) — no dependency on T001
- [ ] T004 Add `BindAndValidate<TmdbOptions>(services, TmdbOptions.SectionName)` to `src/LamuFlix.ServiceDefaults/Extensions.cs` inside `AddLamuFlixOptions`; do not make `AddMetadataProvider` call `AddLamuFlixOptions` (FR-012; Q4; PC6 / Compass F4)
- [ ] T005 Create `tests/LamuFlix.UnitTests/Options/SecondaryMetadataProviderOptionsTests.cs` for defaults and BaseUrl validation edges (valid https, loopback http, reject cleartext non-loopback); assert ApiKey absence is accepted by DataAnnotations (FR-012, FR-014; Q4; PC5 — after T002)
- [ ] T006 Add non-secret `"Tmdb": { "Enabled": false, "BaseUrl": "https://api.themoviedb.org/3/" }` to `src/LamuFlix.Api/appsettings.json` — no ApiKey (FR-012; Q4)

## Phase 2: Isolated health types

- [ ] T007 Create `src/LamuFlix.Infrastructure/Adapters/SecondaryMetadataProviderHealthState.cs`: public sealed singleton-friendly type, internal members, single-reference atomic immutable snapshot (failure category + wasUnauthorized) with last-write-wins Volatile publish/read, no network, no timestamp (FR-010; Q4, Q6; Sentry F5)
- [ ] T008 Create `src/LamuFlix.Infrastructure/Adapters/SecondaryMetadataProviderHealthCheck.cs`: internal sealed IHealthCheck with explicit `IOptions<TmdbOptions>` constructor dependency implementing Q4 Healthy/Degraded/Unhealthy rules including blank ApiKey and last-401 Unhealthy with safe configuration advice; no HTTP probe (FR-010; Q4; PC6 / Compass F2)
- [ ] T009 [US5] Create `tests/LamuFlix.UnitTests/Adapters/SecondaryMetadataProviderHealthCheckTests.cs` covering initial blank-key Unhealthy, disabled blank-key Unhealthy, unauthorized/degraded/recovery mappings (supply `IOptions<TmdbOptions>` in arrange) (FR-014; Q4; PC6 / Compass F2 — after T007→T008)

## Phase 3: User Story 1 — Adapter mapping and lookup flow (P1)

**Goal**: Core-field Found/NotFound via find or search→details.
**Independent Test**: IntegrationTests mapping via FindAsync + WireMock (T013) after probe/registration; lookup Found/NotFound (T018).

- [ ] T010 [US1] Create `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.Mapping.cs` with private nested Response/ResponseMapper mapping Title, ReleaseYear, Runtime minutes, Synopsis, PosterUrl (w500 recipe only for usable exact-one-leading-slash relative `poster_path` with no scheme/host/backslash/control/`.`/`..` segments, via non-throwing `Uri.TryCreate` else absent), ImdbId; leave ratings/credits empty; never map vote_average; one helper per field for complexity (FR-005; Q3, Q5; PC2)
- [ ] T011 [US1] Create `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.cs` primary constructor (`HttpClient`, `IOptions<TmdbOptions>`, `IOptions<OmdbOptions>`, `TimeProvider`, `ILogger<TmdbMetadataProvider>`, `SecondaryMetadataProviderHealthState`) and FindAsync orchestration: disabled/blank-key short-circuit; manual find vs title search; TitleMatcher vs MatchThreshold; details fetch; private static `ActivitySource` using `TelemetryConstants.ActivitySourceName` (no new source/meter name); implement FR-009 instrumentation (open `TelemetryConstants.MetadataLookup` span, tag enrichment-duration histogram with `ProviderTagName`=`TmdbProviderName`, set Failed `ErrorType` from category); helpers keep complexity under refactor gate (FR-001–FR-004, FR-009; Q2, Q3, Q3a, Q6; PC6 / Compass F6 — requires T002, T003, T007)
- [ ] T012 [US1] Create `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.Log.cs` private nested source-generated Log warnings without secrets/titles/URIs (FR-008; Q6)

## Phase 4: User Story 2 & 3 — Classification and resilience wiring (P1)

**Goal**: Q5 outcome table + independent standard resilience handler.
**Independent Test**: WireMock status matrix and resilience cases (T019–T021).

- [ ] T014 [US2] Complete classification helpers in `TmdbMetadataProvider.cs` per Q5 table including 401→InvalidResponse+wasUnauthorized, 429→RateLimited, 5xx/non-caller timeout/breaker→ProviderUnavailable, details/find 404/status_code 21→NotFound, search/movie HTTP 404→InvalidResponse, and when catching `OperationCanceledException` if `ct.IsCancellationRequested` rethrow unrecorded (coincident cancel included) else map non-caller timeout/breaker to ProviderUnavailable; do not edit OMDb (FR-006; Q5; PC3)
- [ ] T015 [US3] Edit `src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs` to add `AddHttpClient<TmdbMetadataProvider>` with BaseAddress from TmdbOptions **normalized to a trailing slash at TMDb registration only**, independent `AddStandardResilienceHandler` from MetadataProviderResilienceOptions (same predicates/backoff/Retry-After/timeouts/breaker as OMDb configurator), keep unkeyed OMDb binding intact; preserve unrelated existing comments; no new explanatory comments (FR-011, FR-013; Q2, Q5; PC1; Ledger L3)
- [ ] T016 [US6] In the same extensions file, `TryAddSingleton<SecondaryMetadataProviderHealthState>()` and `AddCheck<SecondaryMetadataProviderHealthCheck>("metadata-provider-tmdb")` with empty tags; preserve existing `metadata-provider` registration and unrelated comments (FR-011; Q4; US6 ownership)

## Phase 5: User Story 1/4–6 — Integration probe, mapping, WireMock matrix, composition (P1/P2)

**Goal**: Real-client proof (including mapping), secrecy, OMDb regression, health/telemetry composition.
**Independent Test**: SecondaryMetadataProvider* integration classes + exact existing health/composition/options files.

- [ ] T017 Create `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderProbe.cs` dedicated WireMock + ServiceCollection helper (AddLamuFlixOptions + AddMetadataProvider, sentinel key, loopback BaseUrl, short Omdb:Resilience timings, MinimumThroughput left at default except breaker case); do not rewrite `MetadataProviderProbe` or extract a shared probe abstraction (FR-014; Q6; PC5 / Ledger M2)
- [ ] T013 [US1] Create `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderMappingTests.cs` exercising private mapper behaviour only via `FindAsync` over the real typed HttpClient + WireMock, loading T001 fixtures via `Path.Combine(AppContext.BaseDirectory, "Fixtures", "SecondaryMetadata", …)`; cover core fields, null/empty/absent optionals, malformed bodies, zero/invalid runtime/id, vote_average exclusion, and adversarial poster_path cases (FR-014; Q3, Q6; PC2; PC4 / Ledger M1 / Compass F5; PC7 — after T001 + T010–T012 + T015/T016 + T017)
- [ ] T018 [US1] Create `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderLookupTests.cs` for Found core fields, NotFound, manual-id precedence/no title fallback, year query, threshold/tie-break, request `api_key` query shape, and slashless-loopback BaseUrl asserting outgoing paths under `/3/` (FR-014; Q3, Q3a, Q4; PC1)
- [ ] T019 [US2] Extend lookup tests with every Q5 status/result row including 401, 429, 503, details/find 404/status_code 21→NotFound, search/movie 404→InvalidResponse, malformed JSON, other 4xx, and coincident cancellation when `ct.IsCancellationRequested` propagates unrecorded (FR-014; Q5; PC3)
- [ ] T020 [US4] Add disabled and blank-key zero-HTTP cases plus secrecy sweep with default HttpClient factory logging left enabled: sentinel absent from adapter logs, `Microsoft.Extensions.Http` and resilience logger categories (retries/errors), structured state, exception ToString, LamuFlix and System.Net.Http Activity tags/names/descriptions, and health-check descriptions (FR-003, FR-007, FR-014; Q2, Q4, Q5; SC-003; Sentry F3)
- [ ] T021 [US3] Create `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderResilienceTests.cs` for retry exhaustion, 429, synthetic Retry-After, attempt/total timeout, isolated breaker (own ServiceCollection MinimumThroughput=2), cancellation during request/delay including coincident cancel propagation; real-client test proving opening the TMDb breaker leaves OMDb callable and its breaker unaffected (FR-014; Q5, Q6; US3.5; PC3)
- [ ] T022 [US5] Create `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderTelemetryTests.cs` (dedicated file): Metadata.Lookup + enrichment duration tagged provider=tmdb; Failed ErrorType semantics; no secret/title/imdb tags; ActivitySource name is `TelemetryConstants.ActivitySourceName` (FR-009, FR-014; Q6; PC5 / Ledger M2; PC6 / Compass F6)
- [ ] T023 [US6] Create `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderCompositionTests.cs`: concrete TmdbMetadataProvider resolves; unkeyed IMetadataProvider is OmdbMetadataProvider for Enabled true and false; metadata-provider-tmdb registered untagged; secondary health Healthy/Degraded/Unhealthy/recovery; OMDb health unchanged; **owns inspection** that `AddMetadataProvider` does not invoke `AddLamuFlixOptions` (options registered externally / via probe’s separate `AddLamuFlixOptions` call) (FR-011, FR-014; Q2, Q4; SC-002, SC-006; PC6 / Compass F4)
- [ ] T024 [US5] Edit `tests/LamuFlix.UnitTests/HealthCheckRegistrationTests.cs` and `tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs` to assert secondary check present, tags empty, readiness set unchanged (FR-015; Q4; PC6 / Compass F3)
- [ ] T025 [US4] Edit `tests/LamuFlix.IntegrationTests/MetadataProviderCompositionTests.cs` and `tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs` to cover Tmdb bind path and OMDb-unchanged under both Enabled values — no other unnamed path scope (FR-015; Q4, Q6; PC6 / Compass F3)

## Phase 6: Polish & Phase B verification (after Gate 1 + T026 + implementation)

- [ ] T027 Run Roslyn analyzers on explicit changed `.cs` set via `./scripts/run-roslyn-analyzers.ps1` (FR-016; Q6)
- [ ] T028 Run `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml on changed files (FR-016; Q6)
- [ ] T029 Run `./scripts/run-jetbrains-inspectcode.ps1` on changed files (FR-016; Q6)
- [ ] T030 `dotnet format --verify-no-changes` clean (FR-016)
- [ ] T031 Run affected tests then full `dotnet test` (FR-016; SC-004)
- [ ] T032 Run property/security/vulnerable-package gates per harness; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS (FR-016; Q6, Q8). BannedSymbols covered by existing build/analyzers — no separate task (Ledger L3)
- [ ] T033 Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree (FR-016; Q6; SC-005)
- [ ] T034 Size M code review with current-head Sentry, Ledger and Compass reports; apply Q8 closing bar and 2-round cap (FR-016; Q8)

## Dependencies

- **T026 before any T001–T025** (owner landing precedes implementation).
- T001 fixtures in IntegrationTests; T002/T004/T006 gated on owner checkbox for Core TmdbOptions/constitution.
- T003 `[P]` with T002 (no T001 dependency). T005 after T002 (not `[P]`).
- T007 → T008 → T009 (T009 not `[P]`); T007 before adapter constructor (T011).
- T011 requires T002, T003, T007. T010–T012 adapter partials sequenced as needed.
- T014 after T011; T015→T016 same file sequenced (T016 is US6).
- T017 after T015/T016.
- **T013 after T001 + T010–T012 + T015/T016 + T017** (fixtures + adapter + registration + probe).
- T018–T021 after T017; T019/T020 may follow T018 in same file sequenced; T022 after T017 and T011; T023 after T017; T024/T025 after registration exists (T015/T016).
- T027–T034 after T026 and implementation complete; verification last.

## Parallel examples

```text
# After T026 and T002 exist:
T003 TelemetryConstants.TmdbProviderName   # [P] — no fixture dependency
T005 SecondaryMetadataProviderOptionsTests # after T002 only

# After health state+check (T007→T008):
T009 SecondaryMetadataProviderHealthCheckTests

# After T001 + adapter (T010–T012) + registration (T015/T016) + probe (T017):
T013 SecondaryMetadataProviderMappingTests (IntegrationTests FindAsync + WireMock)
T018 SecondaryMetadataProviderLookupTests
T022 SecondaryMetadataProviderTelemetryTests
```

## MVP

US1 + disabled/blank-key guard + unkeyed OMDb preserved (T026, T001–T012, T015–T018, T013, T020, T023) proves ticket AC Found/NotFound and Enabled=false unchanged OMDb before full resilience/health matrix.
