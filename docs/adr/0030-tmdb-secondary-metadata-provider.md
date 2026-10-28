# TMDb as a secondary metadata provider beside OMDb

Status: Accepted. Constitution v1.6.0 (MINOR) permits the TMDb secondary adapter, the Core `TmdbOptions` record, and the provider tag-value constants (DEV-331; owner approved on PR #272).

Ticket: DEV-331. Decisions: `specs/DEV-331/CONCLUSIONS.md`, Q1–Q9 and PC1–PC7. Amendment record: `specs/DEV-331/constitution-amendment-v1.6.0.md`.

## Context

The metadata provider seam is a Core port with an Infrastructure adapter (ADR-0001). Constitution v1.5.0 named OMDb as the only metadata provider behind `IMetadataProvider` (Technology Stack Constraints) and allowed vendor names only on the Infrastructure adapter class and on vendor-mandated configuration keys (Principle VIII).

DEV-331 adds TMDb as a second metadata source. OMDb stays the active production provider, and TMDb is disabled by default. DEV-423 consumes the secondary provider through a chained lookup; this ADR does not decide that chain.

The owner approved the departure on delivery PR #272: the structural checkbox was ticked at 2026-10-27T21:51:09Z, with the comment "Accepted." at 21:51:20Z. PR #272 merged at 2026-10-28T02:56:16Z, before the amendment text landed. Constitution v1.6.0 and this ADR are the remedy under Governance, Compliance.

No accepted ADR records OMDb as the sole metadata provider, so no ADR is amended or superseded.

## Decision

1. **Second adapter.** `TmdbMetadataProvider : IMetadataProvider` lives in `LamuFlix.Infrastructure/Adapters`, beside the unchanged `OmdbMetadataProvider`. OMDb remains the unkeyed production `IMetadataProvider`. `Tmdb:Enabled` defaults to `false`, so runtime behaviour does not change until TMDb is enabled.

2. **Options.** `TmdbOptions` in `LamuFlix.Core.Options` (section `Tmdb`) has three settings: `Enabled` (default `false`), `BaseUrl` (default `https://api.themoviedb.org/3/`, required, a URL, HTTPS or loopback HTTP only), and `ApiKey` (default empty, not required at startup). Authentication is the v3 `api_key` query parameter. When `Enabled` is true and `ApiKey` is blank, `FindAsync` returns `Failed(ProviderUnavailable)` without a network call. The match threshold is read from `OmdbOptions.MatchThreshold` (Q3a); there is no TMDb-specific threshold.

3. **Registration.** TMDb is a concrete typed client, `AddHttpClient<TmdbMetadataProvider>`, with trailing-slash normalisation of `BaseUrl` at this registration only. Its `AddStandardResilienceHandler` is configured from the shared `MetadataProviderResilienceOptions` and is independent of the OMDb client's pipeline.

4. **Health.** An isolated `SecondaryMetadataProviderHealthState` backs an untagged `metadata-provider-tmdb` check, `SecondaryMetadataProviderHealthCheck`. It reports Unhealthy on a blank key or a recent 401, Healthy when disabled or with no failure, and Degraded on other lookup failures. It makes no network probe and is outside the readiness set.

5. **Error mapping.**
   - HTTP 401 maps to `Failed(InvalidResponse)` and sets `wasUnauthorized` on the TMDb health state.
   - HTTP 404 from search maps to `Failed(InvalidResponse)`; 404 from details and find maps to `NotFound`.
   - A 200 body with `status_code = 21` is a miss and maps to `NotFound`. Any other `success = false` 200 body maps to `Failed(InvalidResponse)` (Q5; the adapter does not yet do this, see DEV-436).
   - Caller-cancelled `OperationCanceledException` is rethrown and not recorded.

6. **Telemetry.** The adapter reuses `TelemetryConstants.ActivitySourceName` and the `Metadata.Lookup` span and labels metrics with `provider = "tmdb"` (`TelemetryConstants.TmdbProviderName`). No activity source or meter name is added. The tag-value constants `OmdbProviderName` and `TmdbProviderName` stay in Core; constitution v1.6.0 names them in the Principle VIII exception.

7. **Testing.** Lookup and mapping tests run through the real typed `HttpClient` against WireMock.Net in `LamuFlix.IntegrationTests`, with recorded, sanitised JSON fixtures. No mocked HTTP transport, stub `HttpMessageHandler`, or `InternalsVisibleTo` (Principle IX).

8. **Poster URLs.** A poster is built only from a single leading-slash relative `poster_path` with no scheme, host, backslash, control characters, or dot segments, as `https://image.tmdb.org/t/p/w500{poster_path}`. Otherwise `PosterUrl` is absent. `/configuration` is not called.

9. **Redaction.** The API key is redacted in logs, exceptions, and telemetry.

## Consequences

Positive:

- TMDb can be enabled as a secondary source without changing the active OMDb provider or the OMDb health and telemetry behaviour.
- Secondary-provider health is observable on its own and does not affect readiness.

Negative:

- The constitution stack row and Principle VIII now carry vendor names for a second provider.
- TMDb matching depends on the `Omdb` section for its threshold.
- `AllowedBaseUrlAttribute` and the health state and check are duplicated between the OMDb and TMDb paths, accepted to avoid editing `OmdbOptions.cs` and the OMDb probe.

Follow-ups:

- DEV-436: map non-miss `success = false` 200 bodies to `Failed(InvalidResponse)`.
- DEV-423 owns the fallback rule for the chained provider (`specs/DEV-423/spec.md:13`).
