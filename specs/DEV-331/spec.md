# Feature Specification: TMDb Metadata Provider Adapter

**Feature Branch**: `feature/331-spec`
**Created**: 2026-10-26
**Status**: gate1: provisional — Gate 1 closed pending owner resolution of the Q7 checkbox; plan-challenge repair round 2 FINAL of max 2
**Input**: Ticket DEV-331 (parent DEV-100, Size M); frozen `brief.md` (Q1–Q8, Q3a, Q9) + PC1–PC7 addenda; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; `recon-DEV-331`; `recon-DEV-331-2`; `specs/PRODUCT.md`

## Owner checkbox — Gate 1 closed

- [ ] **blocked: structural — Approve amending `.specify/memory/constitution.md` (:280–282, :307–310, :357) and adding an ADR so `TmdbMetadataProvider : IMetadataProvider` is permitted beside `OmdbMetadataProvider` (with `OmdbMetadataProvider` remaining the active production `IMetadataProvider` while `Tmdb:Enabled` defaults to false until DEV-423) and the ticket-named `TmdbOptions` record is permitted in `LamuFlix.Core.Options` alongside `OmdbOptions`, while preserving all other vendor-neutral Core, port, health-type, and layering rules?**

Basis: Q7; constitution stack-row and vendor-naming constraints; ticket overview and options requirement (`recon-DEV-331:137–165`). Carry this checkbox verbatim into the spec PR. Patron technical confirmation does not answer it. Proposed amendment and ADR must cover both constraints; neither is a landed contract or permission for Phase B. Coordinate the proposed amendment/ADR through Conductor; Quill does not invent an ADR identifier or edit the constitution in this drafting pass.

## Scope

**In scope** (Q2–Q6, ticket Scope & Technical Design; PC1–PC6 clarifications):

- Sealed `TmdbMetadataProvider` implementing existing `LamuFlix.Core.Ports.IMetadataProvider` in `src/LamuFlix.Infrastructure/Adapters/`, with `TmdbMetadataProvider.Mapping.cs` and `TmdbMetadataProvider.Log.cs`; private nested `Response` / `ResponseMapper` / `Log`.
- Ticket-named `TmdbOptions` (`Enabled`, `BaseUrl`, `ApiKey`) in Core Options, section `Tmdb`, bound and validated at startup (conditional on owner-approved constitution resolution); private nested `AllowedBaseUrlAttribute` inside `TmdbOptions.cs` only (PC6 / Compass F1).
- Concrete typed client `AddHttpClient<TmdbMetadataProvider>` beside unchanged `AddHttpClient<IMetadataProvider, OmdbMetadataProvider>`; BaseAddress trailing-slash normalization at TMDb registration only (PC1); independent `AddStandardResilienceHandler` configured from existing `MetadataProviderResilienceOptions` (`Omdb:Resilience`).
- Vendor-neutral `SecondaryMetadataProviderHealthState` and `SecondaryMetadataProviderHealthCheck` (`IOptions<TmdbOptions>` constructor dependency — PC6 / Compass F2); untagged check id `metadata-provider-tmdb` outside readiness.
- Telemetry: own `ActivitySource` instance reusing `TelemetryConstants.ActivitySourceName` (no new source/meter **name**); reuse enrichment-duration histogram; add `TelemetryConstants.TmdbProviderName = "tmdb"` (PC6 / Compass F6).
- Recorded sanitized TMDb JSON fixtures colocated under `tests/LamuFlix.IntegrationTests/Fixtures/SecondaryMetadata/`; fixture-driven mapping exercised via `FindAsync` over real typed `HttpClient` + WireMock.Net (no stub `HttpMessageHandler`, no `InternalsVisibleTo` — PC4 / Ledger M1 / Compass F5).
- Dedicated `SecondaryMetadataProviderProbe` and `SecondaryMetadataProviderTelemetryTests.cs`; supporting edits only to the exact existing files named in FR-015 / tasks T024–T025, plus the bounded future `LamuFlix.IntegrationTests.csproj` fixture-copy edit (PC7).

**Out of scope** (ticket; Q8 frozen scope; brief exclusions):

- OMDb-to-TMDb fallback/selection (DEV-423); keywords/cast (DEV-358); collections (DEV-424).
- Schema/migrations; public API / OpenAPI / generated TypeScript; web UI.
- New ports, projects, layers or NuGet/npm packages; `Features:LocalPlay` / `Process.Start`.
- Live TMDb credentials or network-based acceptance tests; Gherkin unless separately opted in.
- Changing Core port/domain types, production enrichment caller selection, shared resilience section name, or OMDb health text/state/tags/recovery.
- Rewriting `MetadataProviderProbe` / OMDb probe, extracting a shared probe abstraction, or editing `OmdbOptions.cs` (PC5; PC6).
- Landing the constitution amendment or ADR in this draft (owner checkbox + Conductor coordination only).

**Precedence**: Q3a replaces Q3’s fixed threshold; Q5 replaces Q4’s 401 failure category; Q7 replaces Q1’s owner-checkbox wording. PC1–PC7 addenda take precedence over earlier unit-mapping layout, literal “no second source” wording, and ambiguous fixture-load phrasing for the points they clarify. Ticket Scope & Technical Design is already approved (`specs/PRODUCT.md:23–24`).

## User Scenarios & Testing

### User Story 1 - Core TMDb Lookup and Field Mapping (Priority: P1)

As the platform preparing a second metadata source, I need a TMDb adapter that finds a movie by IMDb id or title and returns the core fields the enrichment model already understands, so later composition (DEV-423) has a working adapter without changing the Core port.

**Why this priority**: ticket item 1 and AC — Found with title/year/runtime/poster; NotFound on unknown title.

**Independent Test**: invoke concrete `TmdbMetadataProvider.FindAsync` against WireMock stubs fed from colocated recorded fixtures in `LamuFlix.IntegrationTests` and assert `MetadataLookupResult` (PC4).

**Acceptance Scenarios**:

1. **Given** a usable details fixture **When** lookup succeeds via search→details **Then** `Found` carries Title, ReleaseYear, positive Runtime (minutes), Synopsis from overview, PosterUrl from `https://image.tmdb.org/t/p/w500{poster_path}` when `poster_path` is a usable exact-one-leading-slash relative path, and a valid details `imdb_id` when present (Q3, Q5; PC2; recon-DEV-331-2:T4–T6).
2. **Given** optional fields absent, null or domain-invalid **Then** those fields are absent and the result remains `Found` when Title is valid (Q3).
3. **Given** `vote_average` present **Then** `ImdbRating`, Rotten Tomatoes, MetaScore, Directors, Actors, Genres and Writers stay null/empty — never map vote average into `ImdbRating` (ticket item 2; Q3).
4. **Given** `ManualImdbId` **When** `GET /3/find/{id}?external_source=imdb_id` returns empty `movie_results` **Then** `NotFound` with no title-search fallback (Q3).
5. **Given** title lookup with no qualifying `TitleMatcher` hit on page 1 **Then** `NotFound` (Q3, Q3a).
6. **Given** a qualifying search candidate **Then** details are fetched with `GET /3/movie/{movie_id}` before mapping (Q3; recon-DEV-331-2:T2–T4).
7. **Given** adversarial `poster_path` values (scheme/host, backslash, control characters, `.`/`..` segments, missing/extra leading slash) **Then** PosterUrl is absent and mapping does not throw (PC2).

### User Story 2 - Correct Classification of Every Provider Outcome (Priority: P1)

As an operator, I need every TMDb outcome mapped to the enrichment failure categories already understood by the consumer, so retryable and terminal failures behave consistently with OMDb.

**Why this priority**: ticket status mappings and Q5 classification table.

**Independent Test**: one WireMock (or fixture) case per Q5 row, plus explicit search-404 and coincident-cancellation cases (PC3).

**Acceptance Scenarios**:

1. **Given** final HTTP 429 **Then** `Failed(RateLimited)` (Q5).
2. **Given** final 5xx, 408, network error, broken circuit, or non-caller timeout/breaker exception **Then** `Failed(ProviderUnavailable)` (Q5; PC3).
3. **Given** HTTP 401 **Then** `Failed(InvalidResponse)` and unauthorized health flag (Q5).
4. **Given** details/find 404 or 200 error `status_code` 21 **Then** `NotFound` (Q5).
5. **Given** `GET /3/search/movie` HTTP 404 **Then** `Failed(InvalidResponse)` — not NotFound (PC3; Sentry F6).
6. **Given** other unexpected non-success, malformed/wrong-shaped JSON, or non-miss `success=false` 200 **Then** `Failed(InvalidResponse)` (Q5).
7. **Given** blank required title, non-positive selected movie id, or invalid selected id **Then** `Failed(InvalidResponse)` (Q3).
8. **Given** caller cancellation before/during send or during retry delay, including when an `OperationCanceledException` is caught while `ct.IsCancellationRequested` is true (coincident cancel with timeout/breaker) **Then** `OperationCanceledException` propagates and no provider-failure health outcome is recorded (Q5; PC3). Leave `OmdbMetadataProvider.cs` untouched.

### User Story 3 - Independent Resilience Beside OMDb (Priority: P1)

As a library sharing the existing resilience configuration, I need the TMDb typed client to retry and break independently of OMDb’s pipeline, so one provider’s outage does not poison the other.

**Why this priority**: ticket registration resilience; Q5 independent handler.

**Independent Test**: WireMock resilience cases on the TMDb client with short `Omdb:Resilience` test timing; assert OMDb breaker state unaffected.

**Acceptance Scenarios**:

1. **Given** transient 5xx then success **Then** Found after retries under the shared options (Q5).
2. **Given** always-429 **Then** `Failed(RateLimited)` after handler exhaustion (Q5).
3. **Given** synthetic `Retry-After` **Then** handler honours it; no claim that TMDb documents the header (Q5; recon-DEV-331-2:T8,T11).
4. **Given** attempt/total timeout or open breaker on the TMDb client **Then** `Failed(ProviderUnavailable)` without new outbound call when broken (Q5).
5. **Given** OMDb and TMDb clients **Then** breaker state is isolated between them (Q5).

### User Story 4 - Options, Auth, Secrets and Disabled Guard (Priority: P1)

As an operator, I need three-field TMDb options, query-key auth, and a disabled/no-key guard that never places a secret on the wire or in diagnostics, while production enrichment keeps resolving OMDb.

**Why this priority**: ticket items 3–4 and AC on Enabled=false and missing-key health.

**Independent Test**: options startup validation; disabled/blank-key FindAsync with zero HTTP; secrecy sweep with default HttpClient factory logging left enabled; unkeyed `IMetadataProvider` remains OMDb under both Enabled values.

**Acceptance Scenarios**:

1. **Given** defaults **Then** `Enabled=false`, `BaseUrl=https://api.themoviedb.org/3/`, `ApiKey` empty; BaseUrl validated Required/Url/https-or-loopback via private nested `AllowedBaseUrlAttribute` in `TmdbOptions` only; ApiKey not `[Required]` at startup (Q4; PC6).
2. **Given** `Enabled=false` **When** `FindAsync` is invoked **Then** `Failed(ProviderUnavailable)` with zero outbound HTTP (Q2).
3. **Given** `Enabled=true` and blank ApiKey **Then** `Failed(ProviderUnavailable)` with zero outbound HTTP (Q4).
4. **Given** enabled lookup **Then** requests use v3 `api_key` query auth (not bearer); key never appears in logs, tags, exceptions or diagnostic URLs (Q4, Q5).
5. **Given** either Enabled value **Then** production `IMetadataProvider` still resolves to `OmdbMetadataProvider` and RabbitMQ registration-presence behaviour is unchanged (Q2).
6. **Given** a slashless loopback `BaseUrl` at registration **Then** outgoing paths still resolve under `/3/` (trailing slash normalized at TMDb `HttpClient` registration only) (PC1).

### User Story 5 - Isolated Passive Health and Existing Telemetry (Priority: P2)

As an operator, I need a separate untagged TMDb health check and provider-tagged spans/metrics so TMDb problems are visible without changing OMDb readiness or health semantics.

**Why this priority**: ticket health AC; constitution observability; Q4/Q6.

**Independent Test**: health registration/tags via existing named test files; passive state transitions including initial/disabled blank-key; ActivityListener / meter assertions with `provider=tmdb` in dedicated telemetry tests.

**Acceptance Scenarios**:

1. **Given** registration **Then** check `metadata-provider-tmdb` exists, tags empty, outside `/health/ready`; OMDb `metadata-provider` text/state/tags unchanged (Q4).
2. **Given** blank ApiKey (initial state or disabled) or last 401 **Then** Unhealthy with safe configuration advice; no HTTP from the check — blank-key rule reads `IOptions<TmdbOptions>` (Q4; PC6 / Compass F2).
3. **Given** disabled with key, no prior failure, Found or NotFound **Then** Healthy (Q4).
4. **Given** other recorded lookup failures **Then** Degraded (Q4).
5. **Given** a lookup **Then** one `Metadata.Lookup` span from an `ActivitySource` named `TelemetryConstants.ActivitySourceName` and enrichment-duration sample tagged `provider=tmdb` via `TelemetryConstants.TmdbProviderName`; Failed carries existing ErrorType semantics; no secret/title/IMDb-id tags; no new ActivitySource/Meter **name** (Q6; PC6 / Compass F6).

### User Story 6 - Composition Wiring Without Fallback (Priority: P2)

As a maintainer, I need `AddMetadataProvider` to register the concrete TMDb client, resilience and health beside OMDb without introducing a selector, so DEV-423 can compose later.

**Why this priority**: Q2 registration contract; ticket item 4.

**Independent Test**: composition tests resolving concrete `TmdbMetadataProvider`, unkeyed OMDb, both health checks, options bind path; T023 owns inspection that `AddMetadataProvider` does not call `AddLamuFlixOptions` (PC6 / Compass F4).

**Acceptance Scenarios**:

1. **Given** `AddMetadataProvider` **Then** concrete `TmdbMetadataProvider` resolves; unkeyed `IMetadataProvider` is still `OmdbMetadataProvider` (Q2).
2. **Given** health registration in either Enabled mode **Then** `metadata-provider-tmdb` is present (Q2, Q4).
3. **Given** ServiceDefaults options path **Then** `TmdbOptions` binds/validates; `AddMetadataProvider` does not call `AddLamuFlixOptions` (Q4; PC6).

### Edge Cases

- First-page-only search can miss obscure titles; accepted cost (Q3).
- Null/empty/unusable `poster_path` (including adversarial scheme/host/backslash/control/dot-segment forms) yields no PosterUrl via non-throwing `TryCreate`; no `/configuration` call (Q5; PC2).
- Runtime `0` or non-positive → absent Runtime via domain factory (Q3; recon-DEV-331-2:T5).
- Manual-ID miss does not fall back to title search; transport/provider failure likewise has no title fallback (Q3, Q5).
- Caller cancellation during retry delay, and coincident cancel with timeout/breaker when `ct.IsCancellationRequested`, is not recorded as provider failure (Q5; PC3).
- Shared `OmdbOptions.MatchThreshold` and `Omdb:Resilience` couple tuning across adapters until a later ticket — accepted residual (Q3a, Q5; Sentry F7).
- Reactive 429 handling only (no proactive rate limiter) — accepted residual (Sentry F8).
- Health snapshot is atomic immutable last-write-wins under concurrent lookups (Sentry F5).
- Missing TMDb key keeps the secondary check Unhealthy even when disabled; readiness unaffected (Q4).
- Operator-supplied slashless `BaseUrl` is corrected only at TMDb registration (PC1).

## Requirements

### Functional Requirements

- **FR-001**: `public sealed partial class TmdbMetadataProvider` in `src/LamuFlix.Infrastructure/Adapters/` implements Core `IMetadataProvider.FindAsync`, spanning `TmdbMetadataProvider.cs`, `TmdbMetadataProvider.Mapping.cs` and `TmdbMetadataProvider.Log.cs`, with private nested `ResponseMapper`, `Response` and `Log` (Q6; ticket item 1).
- **FR-002**: Primary constructor takes `HttpClient`, `IOptions<TmdbOptions>`, `IOptions<OmdbOptions>` (for `MatchThreshold` only — Q3a), `TimeProvider`, `ILogger<TmdbMetadataProvider>` and `SecondaryMetadataProviderHealthState`. `FindAsync` stays a thin orchestrator under the refactor complexity gate (Q3a, Q6).
- **FR-003**: When `TmdbOptions.Enabled` is false, or Enabled is true and ApiKey is blank, `FindAsync` returns `Failed(ProviderUnavailable)` with no outbound HTTP (Q2, Q4).
- **FR-004**: Manual IMDb ID uses `GET /3/find/{external_id}?external_source=imdb_id`, inspects only `movie_results`; empty → `NotFound`, no title fallback. Title path cleans via `FolderNameCleaner`, queries `GET /3/search/movie` (first page) with `query` and optional `primary_release_year`, scores via `TitleMatcher` against `IOptions<OmdbOptions>.Value.MatchThreshold`, orders score descending then TMDb id ascending; no qualifying hit → `NotFound`. Selected positive id fetches `GET /3/movie/{movie_id}` (Q3, Q3a).
- **FR-005**: Map Title (required non-blank), ReleaseYear (leading 4 digits via `ReleaseYear.TryCreate`), Runtime (positive minutes via `Runtime.TryCreate`), Synopsis (non-blank overview), PosterUrl via non-throwing factory: usable `poster_path` is an exact single leading `/` relative path containing no scheme, host, backslash, control characters, or `.`/`..` segments; build `https://image.tmdb.org/t/p/w500{poster_path}` with `Uri.TryCreate` (else absent). Map valid `imdb_id` via `ImdbId.TryCreate`. Leave ImdbRating, RottenTomatoesRating, MetaScore, Directors, Actors, Genres, Writers null/empty. Never map `vote_average` to ImdbRating. No TMDb id on Core (Q3, Q5; PC2).
- **FR-006**: Classify final outcomes per the Q5 table (NotFound / RateLimited / ProviderUnavailable / InvalidResponse). When an `OperationCanceledException` is caught and `ct.IsCancellationRequested` is true (including coincident cancel with timeout/breaker), rethrow unrecorded; non-caller timeout/breaker exceptions map to `Failed(ProviderUnavailable)`. Explicitly: `GET /3/search/movie` HTTP 404 → `Failed(InvalidResponse)`; details/find 404 (and 200 `status_code` 21) → `NotFound`. Catch the same transport/deserialize families used by the existing OMDb adapter pattern without swallowing programming errors; do not edit `OmdbMetadataProvider.cs` (Q5; PC3).
- **FR-007**: Authenticate with v3 `api_key` query parameter from `TmdbOptions.ApiKey`; redact across logs, exceptions and telemetry on lookup, error and retry paths (Q4, Q5).
- **FR-008**: Private nested source-generated `Log` warnings follow existing nested Log conventions; no URI, key, title or raw secret text in log arguments (Q6).
- **FR-009**: Instantiate a private static `ActivitySource` using `TelemetryConstants.ActivitySourceName` (no new ActivitySource or Meter **name**; existing enrichment-duration histogram reused). Open `TelemetryConstants.MetadataLookup`; tag enrichment duration with `TelemetryConstants.ProviderTagName` = `TelemetryConstants.TmdbProviderName` (`"tmdb"`); on Failed set existing ErrorType semantics; no secret/title/IMDb-id tags (Q6; PC6 / Compass F6).
- **FR-010**: `SecondaryMetadataProviderHealthState` (public sealed singleton, internal members, single-reference **atomic immutable** snapshot — failure category + wasUnauthorized — last-write-wins under concurrency; Volatile publish/read) and `SecondaryMetadataProviderHealthCheck` (`internal sealed` `IHealthCheck` with explicit `IOptions<TmdbOptions>` constructor dependency) implement Q4 Healthy/Degraded/Unhealthy rules including initial and disabled blank-key Unhealthy, with no network probe. Preserve OMDb health types and behaviour. Small duplication vs OMDb health types is accepted because of the blank-key/`IOptions` divergence and isolated snapshot (Q4, Q6; PC5; Sentry F5; PC6 / Compass F2).
- **FR-011**: `AddMetadataProvider` keeps `AddHttpClient<IMetadataProvider, OmdbMetadataProvider>` and existing OMDb health registration; adds `AddHttpClient<TmdbMetadataProvider>` with BaseAddress from `TmdbOptions.BaseUrl` **normalized to a trailing slash at TMDb registration only**, independent `AddStandardResilienceHandler` from `MetadataProviderResilienceOptions`, `TryAddSingleton<SecondaryMetadataProviderHealthState>()`, and `AddCheck<SecondaryMetadataProviderHealthCheck>("metadata-provider-tmdb")` untagged. No selector/composite/new port. Does not call `AddLamuFlixOptions` (Q2, Q4, Q5; PC1).
- **FR-012**: `public sealed record TmdbOptions` in `src/LamuFlix.Core/Options/TmdbOptions.cs`, `SectionName = "Tmdb"`, members `Enabled` (default false), `BaseUrl` (default `https://api.themoviedb.org/3/`), `ApiKey` (default empty). BaseUrl: Required, Url, and private nested `AllowedBaseUrlAttribute` (https-or-loopback) declared inside `TmdbOptions.cs` only — no `OmdbOptions.cs` edit and no shared validator file. ApiKey not Required. Bound via ServiceDefaults `BindAndValidate<TmdbOptions>`. Api `appsettings.json` ships non-secret `Tmdb:Enabled`/`Tmdb:BaseUrl` only (Q4; PC6 / Compass F1). Conditional on owner checkbox (Q7).
- **FR-013**: Resilience numbers, predicates, Retry-After support, timeouts and breaker settings match the existing OMDb handler configuration from `MetadataProviderResilienceOptions`; no new resilience options or packages; no manual retry loops (Q5).
- **FR-014**: Tests: options/passive-health unit coverage; recorded sanitized official JSON fixtures colocated under `tests/LamuFlix.IntegrationTests/Fixtures/SecondaryMetadata/` (same project only — no cross-project access). Fixture files are copied to the test output via the bounded `LamuFlix.IntegrationTests.csproj` edit in FR-015; tests read them only as `Path.Combine(AppContext.BaseDirectory, "Fixtures", "SecondaryMetadata", …)`. Fixture-driven mapping via `FindAsync` over real typed `HttpClient` + WireMock.Net covering core/null/malformed/adversarial-poster cases. WireMock matrix covering every Q5 status/result row (including search 404 → InvalidResponse and coincident-cancellation propagation), retry exhaustion, 429, synthetic Retry-After, attempt/total timeout, isolated breaker, cancellation, secrecy with default HttpClient factory logging left enabled, disabled/missing-key zero HTTP, slashless-loopback BaseUrl outgoing `/3/` path, startup URL validation, concrete resolution, unkeyed OMDb under both Enabled values, passive health matrix (initial/disabled blank-key), untagged registration and unchanged readiness/OMDb health. Dedicated `SecondaryMetadataProviderProbe` (no OMDb probe rewrite / shared abstraction) and dedicated `SecondaryMetadataProviderTelemetryTests.cs`. No mocked HTTP transport; no stub `HttpMessageHandler`; no `InternalsVisibleTo`; no live TMDb. Vendor-neutral test/helper type names (Q6; PC4–PC5; PC7).
- **FR-015**: Edit only: `MetadataProviderServiceCollectionExtensions.cs`, ServiceDefaults `Extensions.cs`, `TelemetryConstants.cs`, Api `appsettings.json`, the exact existing test files `tests/LamuFlix.UnitTests/HealthCheckRegistrationTests.cs`, `tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs`, `tests/LamuFlix.IntegrationTests/MetadataProviderCompositionTests.cs`, `tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs`, and — as a future Phase B edit only — `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` solely to add `Fixtures/SecondaryMetadata/**/*.json` with `CopyToOutputDirectory=PreserveNewest` (preserving relative output directories); no package/project-reference/other-fixture/settings changes. No Core port/domain/caller changes; no web contracts; preserve unrelated existing comments in edited files (Q4, Q6; PC6; PC7; Ledger L3).
- **FR-016**: Phase B verification uses explicit changed-C# analyzer/complexity/InspectCode scope, affected then full `dotnet test`, property/security/format gates per harness, mutation via `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from a worktree; BannedSymbols/`DateTime.Now` covered by existing build/analyzers (no separate task). Never hardcode/lower thresholds. Closing bar Critical/High/Medium block; Low needs explicit nonblocking disposition; Sentry/Ledger/Compass mandatory for size M; two review/fix rounds max (Q6, Q8; Ledger L3). TDD remains optional; fixture-first and all required tests exist before verification (Ledger L2).

### Key Entities

- **`TmdbOptions`**: Core options record under section `Tmdb` (owner-checkbox dependent); owns private nested `AllowedBaseUrlAttribute`.
- **`TmdbMetadataProvider`**: Infrastructure adapter implementing Core `IMetadataProvider`.
- **`SecondaryMetadataProviderHealthState` / `SecondaryMetadataProviderHealthCheck`**: isolated passive health for the secondary provider (`IOptions<TmdbOptions>` on the check).
- **`MetadataLookup` / `MetadataLookupResult` / `MovieMetadata` / `EnrichmentFailureCategory`**: existing Core types, unchanged.
- **`TitleMatcher`**, **`FolderNameCleaner`**, **`MetadataProviderResilienceOptions`**, **`OmdbOptions.MatchThreshold`**: reused existing helpers/options.

## Success Criteria

- **SC-001**: Fixture/WireMock cases prove Found core fields, NotFound on unknown title, 429→RateLimited, 503→ProviderUnavailable, and missing-key Unhealthy on `metadata-provider-tmdb` (ticket AC).
- **SC-002**: With `Enabled=false`, production enrichment still uses OMDb and TMDb `FindAsync` performs zero HTTP (ticket AC; Q2).
- **SC-003**: Secrecy sweep with default HttpClient factory logging left enabled: ApiKey sentinel absent from adapter logs, `Microsoft.Extensions.Http` and resilience logger categories (including retries/errors), structured state, exception `ToString()`, LamuFlix and `System.Net.Http` activity tags/names/descriptions, and health-check descriptions (Q4, Q5; PC1–PC3 / Sentry F3).
- **SC-004**: Analyzer, complexity (≤15 then refactor ≤ harness refactor threshold), InspectCode, format and full `dotnet test` pass on the change set; skipped/unrunnable gates are never reported as PASS (Q6, Q8).
- **SC-005**: Mutation gate run per harness from the worktree without `--since`; score meets configured threshold for eligible Core changes or reports N/A/SKIPPED correctly (Q6).
- **SC-006**: Readiness group and OMDb `metadata-provider` behaviour remain unchanged; secondary check stays untagged (Q4).

## Assumptions

- WireMock and recorded fixtures stand in for live TMDb; no live network or real key (Q6; ASSUMPTIONS.md).
- Defensive mapping is required because official schemas give no required/nullability guarantees (recon-DEV-331-2; ASSUMPTIONS.md) — decided, not assumed.
- Retry-After support is handler behaviour; tests may synthesize the header without asserting TMDb sends it (Q5; ASSUMPTIONS.md).
- Exact new test filenames follow existing IntegrationTests/UnitTests organization with vendor-neutral helper/type names; recorded fixtures live under IntegrationTests only (Q6; PC4 / Compass F5).
- Unhealthy description copy parallels the existing OMDb key-configuration advice, adapted for TMDb (Q4 “safe configuration advice”).
- No assumption closes the Q7 owner checkbox; Gate 1 remains closed (ASSUMPTIONS.md).

## Traceability

Ticket Scope items 1–5 and AC (`recon-DEV-331:141–167`) → US1–US6, FR-001–FR-015, SC-001–SC-002. Rating rule → US1.3, FR-005. Q2 registration → US4.5, US6, FR-011. Q3/Q3a lookup/threshold → US1, FR-004. Q4 options/health → US4, US5, FR-010–FR-012. Q5 classification/resilience/poster/redaction → US2, US3, FR-005–FR-007, FR-013. Q6 structure/tests/gates → FR-001, FR-009, FR-014, FR-016. Q7 owner checkbox → top checkbox, FR-012 conditional. Q8 closing bar/frozen scope → FR-016, Scope out-of-scope list. PC1–PC6 → FR-005–FR-006, FR-009–FR-012, FR-014–FR-015, US1.7, US2.5/2.8, US4.6, US5.2/5.5, US6.3, SC-003. PC7 → FR-014–FR-015 fixture-copy csproj scope + `AppContext.BaseDirectory` reads. Constitution VI/VII/VIII/IX and stack row → plan Constitution Check; departure only via owner checkbox.
