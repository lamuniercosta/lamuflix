# DEV-331 — TMDb metadata provider grill brief

Status: FROZEN FOR QUILL — Patron confirmed all three grill artifacts at Q9 on 2026-10-26. Technical rulings Q1–Q8 plus Q3a and Q9 recorded; owner checkbox open. This is a planning freeze. Gate 1 remains closed; implementation, review clearance and merge are not approved.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-331-spec`.
Branch: `feature/331-spec`. Intake HEAD (recon identity): `52e443666b5e767edc93befc20eb55de2e77787b`.
Size: M. Parent: DEV-100. Dependencies: DEV-298, DEV-300, DEV-303.

## Authority and precedence

- Local facts: `recon-DEV-331`, especially port/domain sections 2–3, resilience/DI/health/options sections 4–7, test inventory section 9, and authoritative ticket at lines 129–175.
- Remote contract facts: `recon-DEV-331-2`, official TMDb API v3 documentation retrieved 2026-10-26. These facts were supplied through recon; Keel did not run independent repository or API reconnaissance.
- Decisions: Patron Q1, Q2, Q3, Q3a, Q4, Q5, Q6, Q7, Q8; complete exchanges and cited basis are append-only in `CONCLUSIONS.md`. Every answer is also reproduced below.
- Q3a replaces Q3's fixed threshold. Q5 replaces Q4's 401 failure category. Q7 replaces Q1's owner-checkbox wording. Historical answers remain preserved, not alternate implementation choices.
- Ticket scope is already approved. Patron found no ticket contradictions. The constitution departure is the only owner escalation.

## Open owner checkbox — Gate 1 closed

- [ ] **blocked: structural — Approve amending `.specify/memory/constitution.md` (:280–282, :307–310, :357) and adding an ADR so `TmdbMetadataProvider : IMetadataProvider` is permitted beside `OmdbMetadataProvider` (with `OmdbMetadataProvider` remaining the active production `IMetadataProvider` while `Tmdb:Enabled` defaults to false until DEV-423) and the ticket-named `TmdbOptions` record is permitted in `LamuFlix.Core.Options` alongside `OmdbOptions`, while preserving all other vendor-neutral Core, port, health-type, and layering rules?**

Basis: Q7; constitution stack-row and vendor-naming constraints; ticket overview and options requirement (`recon-DEV-331:137–165`). Carry this checkbox verbatim into the spec PR. Patron technical confirmation does not answer it. Proposed amendment and ADR must cover both constraints; neither is a landed contract or permission for Phase B. Coordinate the proposed amendment/ADR through Conductor; this grill writes only the three requested feature artifacts. ADR identifier and constitution edits are not silently invented here.

## Loop discipline (Q8)

**Closing bar:** Critical, High and Medium block. Low requires an explicit nonblocking disposition and a follow-up when warranted. Sentry, Ledger and Compass reports are all mandatory for this M ticket; any missing axis blocks. Unnecessary abstractions are follow-ups, never an automatic fix commit.

**Round cap:** Two review/fix rounds per loop. At the cap, route unresolved blockers to Conductor and stop that loop. No automatic third round or repost of an immutable review. A plan freeze, spec analysis, Gate 1, Phase B pickup, review clearance and merge bar are separate claims.

**Frozen scope:** TMDb adapter/core-field mapping, three-field options, concrete typed-client registration beside unchanged OMDb, isolated passive untagged health, shared-config independent resilience, redacted existing telemetry, recorded JSON plus WireMock/regression tests, and the Q7 proposed constitution amendment/ADR; anything else is a follow-up issue, not a finding in this round.

Excluded: OMDb-to-TMDb fallback/selection (DEV-423); keywords/cast (DEV-358); collections (DEV-424); schema changes, public API/OpenAPI/generated types, web UI, new ports/projects/layers/packages, LocalPlay and playback execution. No live credentials or network-based acceptance tests.

## Settled behavior

### Port, registration and activation (Q2)

Keep `IMetadataProvider`, `MetadataLookup`, `MetadataLookupResult`, `MovieMetadata`, the production enrichment caller and the unkeyed OMDb binding unchanged. Register `AddHttpClient<TmdbMetadataProvider>` beside the existing `AddHttpClient<IMetadataProvider, OmdbMetadataProvider>` through `AddMetadataProvider`. The concrete TMDb adapter implements the existing port and is directly resolvable for tests/future composition. No selector, composite or new port.

For either Enabled value, production enrichment still resolves OMDb and RabbitMQ registration-presence behavior remains intact. `Enabled=true` allows explicit TMDb invocation; it does not activate fallback. `Enabled=false` direct invocation returns `Failed(ProviderUnavailable)` without HTTP. Register the separate health check in either mode.

### Search, details and mapping (Q3, corrected by Q3a)

Manual IMDb ID takes precedence: `GET /3/find/{external_id}?external_source=imdb_id`; inspect only `movie_results`. Empty movie results are `NotFound` with no title fallback. Title lookup uses the existing cleaner, `GET /3/search/movie` with `query` and optional `primary_release_year`, first page only. Score with existing `TitleMatcher`; obtain the acceptance threshold from injected `IOptions<OmdbOptions>.Value.MatchThreshold`. No hardcoded threshold and no additional `TmdbOptions` field. Order candidates by score descending then TMDb ID ascending; no qualifying hit is `NotFound`.

After selecting a positive movie ID, call `GET /3/movie/{movie_id}` for details: search/find lack runtime and IMDb ID. Do not add a TMDb ID to Core. Map title, release year, positive runtime in minutes, overview and poster. Map valid details `imdb_id` into the existing optional IMDb ID. Preserve domain validation; absent or domain-invalid optional values become absent. Required blank title, invalid selected ID, malformed/wrong-shaped JSON are `InvalidResponse`. Leave IMDb rating, Rotten Tomatoes rating, MetaScore, Directors, Actors, Genres and Writers null/empty. Never map `vote_average` to `ImdbRating`.

Poster URL uses the documented `https://image.tmdb.org/t/p/w500{poster_path}` recipe for usable leading-slash relative paths; null/empty/unusable paths yield no poster URL. Do not call `/configuration` or download images.

### Options, authentication and health (Q4, Q5)

`TmdbOptions` in Core Options has exactly `Enabled`, `BaseUrl`, `ApiKey`; section `Tmdb`; defaults false, `https://api.themoviedb.org/3/`, empty key. Bind through the existing ServiceDefaults options startup-validation pattern. Validate required URL/base URL with the existing HTTPS-or-loopback policy. Do not mark `ApiKey` required at startup: the host must start so missing-key health can be reported. Enabled adapter plus blank key returns `Failed(ProviderUnavailable)` without HTTP.

Use v3 `api_key` query authentication. This field is an API key, not a bearer read token. Keys come only from user secrets/environment; never commit defaults or put keys in logs, tags, exceptions or diagnostic URLs. Test redaction for all lookup, error and retry paths.

Use vendor-neutral `SecondaryMetadataProviderHealthState` and `SecondaryMetadataProviderHealthCheck` separate from OMDb state/check. Register `metadata-provider-tmdb` untagged, outside readiness. No HTTP in health checks and no authentication probe. Blank key or last 401: Unhealthy with safe configuration advice. With a supplied key, disabled/no previous failure, Found or NotFound: Healthy. Other lookup failures: Degraded. Preserve OMDb health text, state, tags and recovery behavior; caller cancellation does not overwrite a health outcome.

### Resilience and classification (Q5)

TMDb gets an independent standard resilience pipeline, configured from the existing `MetadataProviderResilienceOptions` section `Omdb:Resilience`. Reuse current validated retry predicates, exponential backoff/jitter, Retry-After support, attempt/total timeouts and breaker settings. No manual retry loops, new packages or resilience options. Breaker state must be isolated from OMDb. Retry-After support is a handler behavior, not a documented TMDb guarantee.

| Final outcome | Port result / health side effect |
| --- | --- |
| No qualifying search/find movie result | `NotFound` |
| Details/find 404; 200 error code 21 | `NotFound` |
| 429 after handler policy | `Failed(RateLimited)` |
| 5xx, 408, network error, broken circuit, non-caller timeout | `Failed(ProviderUnavailable)` |
| 401 | `Failed(InvalidResponse)` plus unauthorized health flag |
| Other unexpected non-success code | `Failed(InvalidResponse)` |
| Malformed/wrong-shaped JSON, non-miss `success=false` 200 payload | `Failed(InvalidResponse)` |
| Caller cancellation, including retry delays | Propagate `OperationCanceledException`; do not record provider failure |

No title fallback after manual-ID miss or transport/provider failure.

### Telemetry (Q6)

Reuse `Metadata.Lookup` and the existing enrichment-duration histogram. Add `TelemetryConstants.TmdbProviderName = "tmdb"` and tag provider accordingly; record existing failure ErrorType semantics. No second ActivitySource/Meter, secret values, title or IMDb-ID tags. Keep logging source generated, private nested Log conventions, `TimeProvider` and existing injection patterns.

## Plan decisions and file scope (Q4, Q6)

This is a planned change map, not a report of implementation. Quill drafts `spec.md`, `plan.md` and `tasks.md` from this brief; Keel checks them read-only against these decisions. Exact existing file locations follow recon; where recon gives only a basename, Quill obtains any missing path fact through Conductor rather than inventing it.

New production files:

- `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.cs`, `TmdbMetadataProvider.Mapping.cs`, `TmdbMetadataProvider.Log.cs`; private nested Response/ResponseMapper/Log, following the existing adapter.
- `src/LamuFlix.Core/Options/TmdbOptions.cs` (conditional on owner-approved constitution resolution).
- `src/LamuFlix.Infrastructure/Adapters/SecondaryMetadataProviderHealthState.cs` and `SecondaryMetadataProviderHealthCheck.cs`, vendor-neutral isolated health types.

Supporting existing files approved by Patron:

- `src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs`: concrete typed client, independent resilience, isolated health registration; keep the unkeyed OMDb binding and original health state/check intact.
- `src/LamuFlix.ServiceDefaults/Extensions.cs`: bind/validate the three-field options; do not make `AddMetadataProvider` invoke global options registration.
- Existing `TelemetryConstants.cs` and API `appsettings.json` identified in recon: provider constant and nonsecret disabled/base-URL defaults.
- Existing options-startup, composition, health-registration/endpoint and telemetry tests in recon sections 6 and 9, only as required to assert the added untagged check and unchanged OMDb/readiness behavior. No gratuitous rewrites of existing suites.

Test additions belong to existing unit/integration test projects and their fixture area. Use vendor-neutral test/helper type names, following the existing metadata-provider test organization. Add mapping/options/health unit tests, recorded sanitized JSON fixtures, and real-client WireMock lookup/resilience/composition/telemetry tests. Exact new test filenames may be selected by Quill consistently with those existing conventions; no new project or top-level folder is authorized. No packages are added. No changes to Core port/domain/caller, shared options section names or web contracts are planned.

The proposed constitution amendment/ADR remains a separate owner-checkbox dependency, not permission to edit protected/shared documents during this grill. No ADR number has been reserved. Existing glossary vocabulary is reused; no new domain term or glossary edit was decided.

## Test strategy (Q6)

Use recorded, sanitized official TMDb JSON, never real credentials. Mapping tests cover required core fields, null/empty/absent optional fields, malformed/wrong-shaped bodies, zero/invalid runtime and ID, distinct rating sources and excluded credits. Matching tests cover deterministic ties, threshold configuration, year query, no matches, manual-ID precedence and no title fallback.

WireMock.Net must exercise actual typed HttpClient and standard resilience handlers. Cover every status/result row above; exhausted retry, 429 and synthetic Retry-After, attempt/total timeout, isolated breaker, caller cancellation during requests/delays, and sanitized logs/spans/exceptions. Configure short timing through options as the existing probe does; do not add production timing seams. Test disabled and missing-key zero HTTP, startup URL validation, direct concrete resolution, unkeyed OMDb selection under both Enabled values, passive health initial/missing-key/unauthorized/degraded/recovery behavior, untagged registration and unchanged readiness/OMDb health. Do not mock an HTTP transport or contact TMDb. No Gherkin unless separately opted in.

## Task ordering and gate expectations (Q6, Q8)

1. Phase A: Quill drafts spec, plan, tasks and traces the owner checkbox; coordinate proposed constitution amendment/ADR. Keel runs read-only SpecKit analysis and checks every plan/task against this brief. Plan challenge/adjudication/freeze and spec PR follow as separate phases. No implementation or build/test gates in this grill.
2. Gate 1 stays closed until owner checkbox resolution and required constitution/ADR work are satisfied. Phase B follows owner approval and merge, plus pickup/drift checks; do not treat this brief as Phase B clearance.
3. Phase B task dependency order: fixtures/options/startup validation → mapper/lookup → DI/resilience/health/telemetry → integration and OMDb regression tests → verification/review.
4. Use explicit changed-C# scope for Roslyn, complexity and InspectCode; run affected tests then full `dotnet test`, required property/security/format gates under harness configuration. Mutation is pre-PR, using `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from a worktree. Never hardcode/lower thresholds or edit generated files. Follow harness refactor complexity configuration, not a new literal in production.
5. Distinguish PASS, failure, `SKIPPED (scope-empty)`, opt-out SKIP, mutation N/A and Could not run. A skipped or unrunnable gate is never PASS. Full M review requires current-head Sentry/Ledger/Compass reports. Apply Q8 severity and round limits. No review/merge readiness is claimed here.

## Every Patron answer

The following answer archive is part of this brief. Later corrections take precedence as listed above; full questions and recommendations remain in `CONCLUSIONS.md`.

---

[from Patron] DEV-331 Q1 ruling (recorded in specs/DEV-331/CONCLUSIONS.md):
blocked: structural — Approve amending .specify/memory/constitution.md:357 (Technology Stack Constraints -> Metadata provider) and adding an ADR so TmdbMetadataProvider : IMetadataProvider is permitted beside OmdbMetadataProvider (with OmdbMetadataProvider remaining the active production IMetadataProvider while Tmdb:Enabled defaults to false until DEV-423)?
- Ticket authority (recon-DEV-331:137, 154-158, 165): Delivering TmdbMetadataProvider : IMetadataProvider registered beside OMDb with TmdbOptions.Enabled defaulting to false and existing OMDb flow unchanged is ticket-approved (specs/PRODUCT.md:23-24).
- Constitution constraint (.specify/memory/constitution.md:347-348, 357): Technology Stack Constraints mandates OMDb behind IMetadataProvider and requires a constitution amendment and an ADR to change; no new port is needed (:148-152).
- Escalation (specs/PRODUCT.md:36-39, 2.3b): Carry as an owner checkbox on the spec PR before Gate 1; proceed with Q2.

---

[from Patron] DEV-331 Q2 ruling:
ACCEPT — Keep the unkeyed IMetadataProvider -> OmdbMetadataProvider registration for both Enabled values; register TmdbMetadataProvider as a concrete typed HttpClient (AddHttpClient<TmdbMetadataProvider>) beside it with the metadata-provider-tmdb health check registered in either mode; when Enabled=false, TmdbMetadataProvider.FindAsync makes no outbound HTTP call and returns MetadataLookupResult.Failed(EnrichmentFailureCategory.ProviderUnavailable); introduce no selector, chain, or new port.
- Ticket scope & sequencing (recon-DEV-331:137, 150, 156-157, 174): DEV-331 registers TmdbMetadataProvider beside OMDb with Enabled defaulting to false and existing OMDb flow unchanged, while DEV-423 explicitly owns the OMDb-to-TMDb chained fallback (specs/PRODUCT.md:18, 23-24).
- Existing DI & consumer gate (recon-DEV-331:55, 60, 79): Keeping AddHttpClient<IMetadataProvider, OmdbMetadataProvider> untouched preserves ProcessEnrichmentCommandHandler injection and RabbitMqServiceCollectionExtensions.HasRegistration<IMetadataProvider>, while AddHttpClient<TmdbMetadataProvider> makes the concrete adapter directly resolvable for tests and DEV-423 composition.
- Port & disabled contract (recon-DEV-331:23-27, .specify/memory/constitution.md:148-152): TmdbMetadataProvider implements Core IMetadataProvider without adding a new port; short-circuiting FindAsync to Failed(ProviderUnavailable) when Enabled=false enforces a deterministic no-network guard if invoked directly while disabled.

---

[from Patron] DEV-331 Q3 ruling:
ACCEPT — Freeze the two-step TMDb lookup and core-field mapping contract as recommended: ManualImdbId uses GET /3/find/{external_id}?external_source=imdb_id inspecting movie_results only (empty movie_results returns NotFound with no title fallback); title lookup cleans via FolderNameCleaner, queries GET /3/search/movie (first page only) with query and optional primary_release_year, scores via TitleMatcher with a fixed 0.8 threshold and deterministic tie-break (score descending, TMDb id ascending), returning NotFound on no qualifying hit; the selected movie id (positive integer required, else Failed(InvalidResponse)) fetches GET /3/movie/{movie_id} to map Title (required non-blank, else Failed(InvalidResponse)), ReleaseYear (leading 4 digits of release_date via ReleaseYear.TryCreate), Runtime (positive integer minutes via Runtime.TryCreate), Synopsis (non-blank overview), PosterUrl (from poster_path), and ImdbId (via ImdbId.TryCreate when valid), leaving ImdbRating, RottenTomatoesRating, MetaScore, Directors, Actors, Genres, and Writers null/empty, with no Core port or domain changes.
- Ticket scope & rating rule (recon-DEV-331:140-141, 147, 174-175): Ticket item 1 maps title, year, runtime, overview, and poster path into Found and returns NotFound on no match; item 2 explicitly forbids mapping TMDb vote_average into ImdbRating; credits/keywords/collections belong to DEV-358 and DEV-424.
- Official TMDb v3 contract (recon-DEV-331-2:T2-T5): Search and find responses omit runtime and imdb_id, requiring the follow-up GET /3/movie/{movie_id} call; search/find misses return HTTP 200 with empty arrays rather than 404; OAS fields are optional/nullable so absent or domain-invalid optional fields map to null while missing/non-positive movie id, blank details title, or malformed JSON returns Failed(InvalidResponse).
- Existing adapter & domain reuse (recon-DEV-331:23-28, 36, 40): Reusing FolderNameCleaner, TitleMatcher (0.8 threshold matching OmdbOptions default), and Core value object TryCreate factories keeps Core untouched (.specify/memory/constitution.md:148-152).

---

[from Patron] DEV-331 Q3a ruling:
ACCEPT — Amend Q3 so TmdbMetadataProvider reads the title-matching threshold from injected IOptions<OmdbOptions>.Value.MatchThreshold (validated configuration default 0.8) instead of a hardcoded 0.8 literal, keeping TmdbOptions strictly to the ticket's three fields (Enabled, BaseUrl, ApiKey) and adding no new options record; all other Q3 terms stand.
- Non-negotiable rule (AGENTS.md:27): Never hardcode a threshold in production code.
- Ticket & existing options contract (recon-DEV-331:72, 142, 156): Ticket item 3 fixes TmdbOptions as (Enabled, BaseUrl, ApiKey), while OmdbOptions.MatchThreshold is already bound and validated at startup with default 0.8 and mirrors the shared MetadataProviderResilienceOptions ("Omdb:Resilience") precedent.

---

[from Patron] DEV-331 Q4 ruling:
ACCEPT — Approve TmdbOptions in LamuFlix.Core.Options (SectionName="Tmdb", Enabled=false, BaseUrl="https://api.themoviedb.org/3/", ApiKey=string.Empty), v3 api_key query authentication, startup validation of BaseUrl (Required, Url, AllowedBaseUrl https-or-loopback) without [Required] on ApiKey, Enabled=true blank-key short-circuit in FindAsync returning Failed(ProviderUnavailable), isolated TMDb health state and untagged metadata-provider-tmdb check (Unhealthy on blank ApiKey or last 401 with safe config advice; Healthy when disabled with key or no failure / Found / NotFound; Degraded on other lookup failures; no network probe), and the forced supporting edits to existing options, registration, telemetry, config, and test files.
- Ticket authority & options naming (recon-DEV-331:142-144, 149-150, 156-157, 164-165): The ticket explicitly names TmdbOptions (Enabled, BaseUrl, ApiKey) and metadata-provider-tmdb and requires the health check to report Unhealthy when the key is missing (specs/PRODUCT.md:23-24); omitting [Required] on ApiKey lets the host start with Enabled=false and satisfies the missing-key health check AC directly, matching the OmdbOptions Core-record precedent (recon-DEV-331:72).
- Security, auth & passive health (recon-DEV-331:64-75; recon-DEV-331-2:T1, T8-T9; .specify/memory/constitution.md:266-277): Passing ApiKey as the v3 api_key query parameter (never logged, tagged, or committed; user-secrets/env only), keeping metadata-provider-tmdb untagged outside /health/ready, and isolating TMDb health state from OMDb's MetadataProviderHealthState preserves existing readiness and OMDb health semantics.
- Forced file scope (§2.3 items 5-6, specs/PRODUCT.md:47-48): Editing ServiceDefaults/Extensions.cs (BindAndValidate<TmdbOptions>), MetadataProviderServiceCollectionExtensions.cs, TelemetryConstants.cs (TmdbProviderName = "tmdb"), appsettings.json ("Tmdb": { "Enabled": false, "BaseUrl": "https://api.themoviedb.org/3/" }), and affected composition/health/options tests is directly forced by the ticket's registration, telemetry, and health-check requirements.

---

[from Patron] DEV-331 Q5 ruling:
CHANGE — Approve the recommended HTTP operational, resilience, poster, and redaction contract with one classification alignment to OmdbMetadataProvider: HTTP 401 maps to MetadataLookupResult.Failed(EnrichmentFailureCategory.InvalidResponse) (non-retryable) while setting wasUnauthorized: true on the TMDb health state; all other terms are approved as recommended (independent AddStandardResilienceHandler configured from MetadataProviderResilienceOptions; 429 -> RateLimited; 5xx/408/HttpRequestException/TimeoutRejectedException/BrokenCircuitException/non-caller timeout -> ProviderUnavailable; details/find 404 or 200 status_code=21 miss -> NotFound; other 4xx, malformed/wrong-shaped or success=false non-miss 200 bodies -> InvalidResponse; caller OperationCanceledException rethrown and unrecorded; PosterUrl built as https://image.tmdb.org/t/p/w500{poster_path} for valid leading-slash relative paths and null otherwise without calling /configuration; full api_key redaction across logs, exceptions, and telemetry).
- Existing adapter & non-retryable 401 contract (OmdbMetadataProvider.cs:286-312, recon-DEV-331:27, 137, 140): EnrichmentFailureCategory.ProviderUnavailable has IsRetryable=true whereas InvalidResponse has IsRetryable=false; OmdbMetadataProvider.UnauthorizedFailure returns Failed(InvalidResponse) with wasUnauthorized: true so a bad API key is terminal for the message rather than retried by the consumer while still driving the health check to Unhealthy.
- Resilience & official TMDb error/image contract (MetadataProviderServiceCollectionExtensions.cs:25-73; recon-DEV-331:140, 143, 148; recon-DEV-331-2:T6-T9): Reusing Configure(HttpStandardResilienceOptions, MetadataProviderResilienceOptions) gives TMDb its own isolated pipeline with identical backoff/jitter/timeout/breaker behaviour; TMDb error codes (404, 200 status_code=21, 429, 5xx) and the static https://image.tmdb.org/t/p/w500 + poster_path recipe match the official v3 specification without extra network calls or NuGet packages.

---

[from Patron] DEV-331 Q6 ruling:
ACCEPT — Approve the bounded implementation structure, vendor-neutral SecondaryMetadataProviderHealthState/SecondaryMetadataProviderHealthCheck, telemetry contract (Metadata.Lookup span + EnrichmentDuration histogram tagged provider="tmdb" via TelemetryConstants.TmdbProviderName, no new source/meter or high-cardinality/secret tags), recorded JSON fixture + WireMock.Net test matrix, task ordering, and Phase A / Phase B verification and closing bar.
- Adapter, health & telemetry conventions (recon-DEV-331:36-41, 68, 75, 140-150; .specify/memory/constitution.md:266-276, 397): Using TmdbMetadataProvider.{cs,Mapping.cs,Log.cs} with private nested types and vendor-neutral SecondaryMetadataProviderHealthState/HealthCheck satisfies constitution VIII vendor-naming rules while isolating OMDb and TMDb health states; tagging EnrichmentDuration with TelemetryConstants.TmdbProviderName = "tmdb" and recording ErrorType on Metadata.Lookup failures matches OmdbMetadataProvider:51-64.
- Test tooling & fixture requirements (recon-DEV-331:86-98, 144, 147-150; .specify/memory/constitution.md:302, 362): Combining recorded sanitized official TMDb JSON fixtures with WireMock.Net over the real typed HttpClient and standard resilience pipeline satisfies both the ticket's fixture/status-code test requirements and Principle IX (no mocked HTTP transport, no live network calls).
- Standing closing bar & stage discipline (AGENTS.md Verification commands; specs/PRODUCT.md:12): Phase A stops at spec/plan/tasks, /speckit-analyze, and the spec PR (remaining open on the Q1 owner checkbox); Phase B runs after Gate 1 with all analyzer, complexity (<=6 refactor), InspectCode, unit/integration/property/vulnerability/mutation gates and 3-axis code review on this size:M ticket.

---

[from Patron] DEV-331 Q7 ruling:
blocked: structural — Approve amending .specify/memory/constitution.md (:280-282, :307-310, :357) and adding an ADR so TmdbMetadataProvider : IMetadataProvider is permitted beside OmdbMetadataProvider (with OmdbMetadataProvider remaining the active production IMetadataProvider while Tmdb:Enabled defaults to false until DEV-423) and the ticket-named TmdbOptions record is permitted in LamuFlix.Core.Options alongside OmdbOptions, while preserving all other vendor-neutral Core, port, health-type, and layering rules?
- Consolidated constitution departure (.specify/memory/constitution.md:280-282, 307-310, 347-348, 357): Technology Stack Constraints (:357) names only OMDb behind IMetadataProvider, and Principle VIII (:307-310) restricts vendor names to the Infrastructure adapter class and configuration keys (while Principle VII :281 explicitly lists OmdbOptions); folding both into one owner checkbox ensures the constitution amendment and ADR cover the adapter row and the Core TmdbOptions record together.
- Zero ticket contradictions (recon-DEV-331:137-165, specs/PRODUCT.md:23-24, 38-39): Every requirement in Q1-Q6 matches the authoritative DEV-331 Scope & Technical Design and Acceptance Criteria without adding to, dropping from, or contradicting the ticket text; only the constitution departure (§2.3b) escalates to the owner checkbox.

---

[from Patron] DEV-331 Q8 ruling:
ACCEPT — Approve the closing bar (Critical/High/Medium block; Low requires explicit nonblocking disposition; all three Sentry/Ledger/Compass review axes mandatory for this size:M ticket), the 2-round review/fix cap (escalating any unresolved blocker at the cap to Conductor without an automatic 3rd round or repost), and the frozen scope (TMDb adapter and core-field mapping, three-field TmdbOptions, concrete typed-client registration beside unchanged OMDb, isolated passive untagged metadata-provider-tmdb health check, shared-config independent resilience, redacted existing telemetry with provider="tmdb", recorded JSON + WireMock/regression tests, and the Q7 proposed constitution amendment + ADR, with everything else out of scope).
- Standing loop discipline & scope boundaries (specs/PRODUCT.md:12, 18, 23-24, 36-39; recon-DEV-331:4, 137-175): Phase A concludes with draft spec artifacts, /speckit-analyze, plan challenge, and the spec PR with Gate 1 held closed by the Q7 owner checkbox; Phase B executes only after owner approval and merge.

---

## Q9 — Final shared-understanding confirmation and planning freeze

**Keel — full question and recommendation:**
Please read specs/DEV-331/brief.md, CONCLUSIONS.md and ASSUMPTIONS.md in the authorized worktree. Draft brief contains normalized Q1-Q8/Q3a decisions, exact open Q7 owner checkbox, loop terms, approach/file scope/test strategy/gates/task order, and every complete Patron answer; precedence explicitly corrects threshold and 401. No production writes, implementation gates, or spec/plan/tasks drafting by Keel. Proposed amendment/ADR is a dependency coordinated separately, not silently landed. Recommend confirm this complete brief accurately represents settled understanding and authorize its planning freeze for Quill; Gate1 remains closed and no owner checkbox is answered. If any mismatch remains, identify it before freeze.

**Patron — complete final ruling:**
[from Patron] DEV-331 Q9 ruling:
ACCEPT — Confirm that specs/DEV-331/brief.md, specs/DEV-331/CONCLUSIONS.md, and specs/DEV-331/ASSUMPTIONS.md accurately and completely represent the settled technical understanding (Q1-Q8 and Q3a, with Q3a threshold, Q5 401->InvalidResponse, and Q7 consolidated owner-checkbox precedence); authorize freezing brief.md for Quill while Gate 1 remains closed on the open Q7 owner checkbox.

---

## Plan challenge decision addendum — PC1–PC6 (2026-10-26)

This append-only addendum takes precedence over earlier unit-mapping layout and literal no-second-source wording. It preserves every Q7 owner-checkbox term, Q8 closing bar and two-round cap. Plan repairs are authorized; no implementation is authorized.

Frozen plan decisions: fixture mapping tests and recorded fixtures move together to existing IntegrationTests, exercising FindAsync via real typed client/WireMock; unit tests remain for options/passive health. Dedicated neutral secondary probe and telemetry tests; neutral health/test/helper names remain required. Accept small isolated duplication, no shared abstraction or OMDb probe/options rewrite. TmdbOptions owns its private nested URL validator. Secondary health check explicitly takes IOptions<TmdbOptions>, with initial and disabled blank-key Unhealthy tests. Normalize only TMDb BaseAddress trailing slash; test outgoing slashless-base path. Usable poster is exactly-one-leading-slash relative path without scheme/host/backslash/control/dot segments, nonthrowing factory or absent. Caller OperationCanceledException propagates unrecorded when ct is cancelled; non-caller timeout/breaker exceptions remain ProviderUnavailable. Search 404 is InvalidResponse. Reuse existing source/meter names; own same-name ActivitySource instance is permitted. Secrecy sweep keeps factory/resilience logging enabled and captures their categories, structured state, exceptions, activities and health descriptions. T023 owns inspection of no global options registration call. Dependencies/parallel markers and existing test paths must be explicit; preserve unrelated comments; TDD remains optional.

Complete Patron question and cited ruling follow (also recorded in CONCLUSIONS.md):
## PC1–PC6 — Phase 2 Plan Challenge Adjudication (Sentry / Ledger / Compass)

**Keel — full question and recommendation:**
[from Keel] DEV-331 plan challenge rulings needed. Worktree F:/Dev/LamuFlix.worktrees/feature-331-spec branch feature/331-spec. Read brief.md and findings-DEV-331-Sentry/Ledger/Compass in LamuFlix notes. Recommend PC1: Sentry F1 normalize configured BaseUrl with trailing slash in TMDb registration only; outgoing slashless-base path test. PC2: Sentry F2 usable poster exactly one leading slash, no scheme/host/backslash/controls/dot segments, nonthrowing domain factory else absent. PC3: Sentry F4 preserve Q5 caller OperationCanceledException propagation unrecorded, including coincident cancellation when caught; timeout/breaker exception still ProviderUnavailable per Q5, no OMDb changes. Sentry F6 explicit search 404 InvalidResponse. PC4: Ledger M1 preserve constitution no mocked transport; move fixture-driven private-mapper tests via FindAsync to IntegrationTests with real typed client/WireMock and recorded fixtures colocated there. No stub handler or InternalsVisibleTo. PC5: Ledger M2 do not broaden scope to rewrite existing OMDb probe or add shared abstraction; dedicated neutral probe, no duplicated helpers where reuse is already possible, own SecondaryMetadataProviderTelemetryTests.cs. Ledger M3/M4 retain Q6/Q7 neutral health/test/helper names and isolated state, no vendor naming exception or shared extraction; recon-DEV-331:45 records DEV-303 no vendor-named tests/helpers, not Core-only. Explain isolation/blank-key divergence and accepted small duplication. PC6: Compass F1 keep private nested validator inside new TmdbOptions, no OmdbOptions edit/shared file. Compass F2 explicit IOptions<TmdbOptions> health-check dependency and initial/disabled blank-key tests. Compass F5 integration fixtures colocated with moved mapping tests, no cross-project access. Compass F6 no new ActivitySource/Meter name; same-name instance allowed. Other findings are in-scope task clarifications: default HttpClient factory/resilience logs and health descriptions in secrecy sweep, atomic last-write-wins snapshot, shared tuning/reactive rate limit residuals, dependencies/parallel markers, bounded existing test paths, inspection owner for no AddLamuFlixOptions, preserve unrelated comments, TDD optional. Please deliberately rule with cited basis; no ticket change or new constitution departure recommended. Report back with maestri ask Keel when done. I will append full ruling before Quill repairs.

**Patron — complete ruling:**
[from Patron] DEV-331 Plan Challenge (PC1–PC6) ruling:
ACCEPT — Approve PC1–PC6 and all in-scope task clarifications as recommended: (PC1) normalize `TmdbOptions.BaseUrl` with a trailing slash at TMDb `HttpClient` registration only and assert outgoing path resolution against a slashless loopback base URL; (PC2) define a usable `poster_path` as an exact single leading `/` relative path containing no scheme, host, backslash, control characters, or `.`/`..` segments, constructed via a non-throwing `TryCreate` factory (else absent `PosterUrl`); (PC3) preserve Q5 caller `OperationCanceledException` propagation unrecorded (checking `ct.IsCancellationRequested` when caught so coincident caller cancellation still propagates unrecorded while non-caller timeout/breaker exceptions map to `Failed(ProviderUnavailable)` without editing OMDb) and state explicitly that `GET /3/search/movie` HTTP 404 maps to `Failed(InvalidResponse)`; (PC4) preserve Principle IX (no mocked HTTP transport, no stub `HttpMessageHandler`, no `InternalsVisibleTo`) by executing fixture-driven `FindAsync` mapping tests in `LamuFlix.IntegrationTests` over the real typed `HttpClient` and `WireMock.Net` with recorded sanitized JSON fixtures colocated in `LamuFlix.IntegrationTests`; (PC5) keep a dedicated neutral secondary probe without rewriting the existing OMDb probe or extracting a shared abstraction, place telemetry assertions in a dedicated `SecondaryMetadataProviderTelemetryTests.cs`, and retain Q6/Q7 vendor-neutral health, test, and helper names (`SecondaryMetadataProviderHealthState`, `SecondaryMetadataProviderHealthCheck`, `SecondaryMetadataProvider*Tests`) with isolated state and accepted small duplication; (PC6) keep a private nested `AllowedBaseUrlAttribute` inside `TmdbOptions.cs` (no `OmdbOptions.cs` edit or shared validator file), inject `IOptions<TmdbOptions>` into `SecondaryMetadataProviderHealthCheck` (covering initial/disabled blank-key `Unhealthy` unit tests), colocate recorded fixtures with the integration mapping tests (no cross-project file access), clarify that `TmdbMetadataProvider` instantiates its own `ActivitySource` reusing `TelemetryConstants.ActivitySourceName` (no new source/meter name), and fold the remaining Low/Info items as in-scope task clarifications with no ticket change or new constitution departure.
- **URI, poster, cancellation & error safety (PC1–PC3; `findings-DEV-331-Sentry` F1–F8; `recon-DEV-331-2:T2-T9`; Q5)**: Ensuring a trailing `/` on TMDb `BaseAddress` prevents `Uri` segment truncation on relative paths (`search/movie`, `movie/{id}`, `find/{id}`); strict single-leading-slash relative path validation and non-throwing `Uri.TryCreate` prevent hostile or malformed remote `poster_path` values from throwing or emitting invalid URLs; propagating `OperationCanceledException` whenever `ct.IsCancellationRequested` is true (and classifying `search/movie` 404 as `InvalidResponse`) honors Q5 and US2.4/US2.7 while leaving `OmdbMetadataProvider.cs` untouched, and T020/SC-003 explicitly sweeps default `Microsoft.Extensions.Http` and resilience logger categories, structured state, exceptions, activity tags, and health descriptions.
- **Real-transport testing, vendor-neutral naming & bounded duplication (PC4–PC5; `findings-DEV-331-Ledger` M1–M4, L1–L3; `.specify/memory/constitution.md:302, 307-310, 362`; `recon-DEV-331:45`; Q6–Q8)**: Running recorded-fixture mapping tests through `FindAsync` against `WireMock.Net` in `LamuFlix.IntegrationTests` (colocating fixtures there under Compass F5) upholds Principle IX's ban on mocked HTTP transports without adding `InternalsVisibleTo`; retaining `SecondaryMetadataProvider*` health, probe, and test names follows Principle VIII and the DEV-303 repo precedent banning vendor-named tests/helpers (`recon-DEV-331:45`), while small duplication across health state/check (which diverge on TMDb's `IOptions<TmdbOptions>` blank-key rule and isolated snapshot) and `AllowedBaseUrlAttribute` avoids unapproved edits to `OmdbOptions.cs`/`MetadataProviderProbe.cs` or premature shared abstractions (§2.3 item 6, Q8).
- **Spec/task precision & stage discipline (PC6; `findings-DEV-331-Compass` F1–F6; `AGENTS.md` Non-negotiables)**: Declaring `private sealed class AllowedBaseUrlAttribute` inside `TmdbOptions.cs`, specifying `IOptions<TmdbOptions>` on `SecondaryMetadataProviderHealthCheck`, clarifying "no new `ActivitySource` or `Meter` name" (`TelemetryConstants.ActivitySourceName`), splitting `SecondaryMetadataProviderTelemetryTests.cs`, fixing `[P]`/dependency markers, naming bounded existing test files in T025, recording inspection ownership for `AddMetadataProvider` not calling `AddLamuFlixOptions`, preserving unrelated comments, and keeping TDD optional resolve all Medium and Low plan-challenge items within frozen scope while Gate 1 remains closed on the open Q7 owner checkbox.


---

## PC7 — Explicit fixture-copy supporting file (2026-10-26)

Keel question: PC4 moved recorded JSON mapping tests to existing IntegrationTests. T001/plan chose CopyToOutputDirectory or equivalent without naming its project-file edit, while FR-015 excludes project files. Recommend deliberately approving only tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj to copy Fixtures/SecondaryMetadata/**/*.json with CopyToOutputDirectory=PreserveNewest, preserving relative directories; no package/project/other-fixture changes. Name this exact file in brief supporting scope, FR-015, plan tree and T001; remove the or-equivalent choice. Basis: approved fixture tests, recon-DEV-331 section 9 and ticket Tests, PC4. No ticket contradiction or new constitution departure.
[from Patron] DEV-331 Plan Challenge (PC7) ruling:
ACCEPT — Approve editing `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` to copy `Fixtures/SecondaryMetadata/**/*.json` to the output directory with `PreserveNewest` (preserving relative directory structure), name `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` explicitly in the `brief.md` supporting-file scope, `spec.md` FR-015, `plan.md` Project Structure tree, and `tasks.md` T001, and remove the ambiguous "or equivalent" wording, with no new packages, projects, settings, or other fixture changes.
- **Forced supporting edit (§2.3 item 6; `recon-DEV-331:89-98, 146, 160`; PC4)**: PC4 moved the recorded sanitized TMDb JSON fixtures into `tests/LamuFlix.IntegrationTests/Fixtures/SecondaryMetadata/` so `FindAsync` mapping tests run against `WireMock.Net` over the real typed `HttpClient`; copying those fixture files to the test output directory via `LamuFlix.IntegrationTests.csproj` (`CopyToOutputDirectory="PreserveNewest"`) is directly forced by the ticket's recorded-JSON test requirement and Principle IX.
- **Concrete task & file-scope boundary (`specs/PRODUCT.md:47-48`; Q8)**: Naming `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` explicitly in `brief.md`, FR-015, `plan.md`, and T001 and dropping "or equivalent" eliminates implementation ambiguity while keeping all package references, project references, and unrelated test settings untouched.


---

## Final planning freeze — Gate 1 review (2026-10-26)

Keel verified Quill repair rounds 1 and 2 (final; cap reached) against all three challenger reports and Patron PC1-PC7. All 21 original findings adjudicated: 9 Medium resolved by repairs or rejected recommendations with cited governing rulings; 10 Low and 2 Info explicitly nonblocking. No remaining technical plan blocker. Full receipt and requirement/task coverage are in PLAN_CHALLENGE_ADJUDICATION.md.

Plan/spec/tasks are FROZEN FOR GATE 1 REVIEW with PC1-PC7 precedence. Original Q7 owner checkbox is unchanged and remains OPEN; Gate 1 is CLOSED until owner approval and required constitution amendment/ADR landing. No Phase B implementation, code-review clearance, merge-bar claim or merge authorization follows from this freeze. T026 precedes all implementation. No build/test/mutation gates run in Phase A.

Existing file-scope clarification PC7 is part of the final plan: only tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj may receive the future fixture-copy edit for Fixtures/SecondaryMetadata/**/*.json with PreserveNewest, preserving relative directories and reading from AppContext.BaseDirectory; no unrelated project edits.
