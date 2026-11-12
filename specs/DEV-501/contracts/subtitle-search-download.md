# Contracts: DEV-501 subtitle search and download

Mirrors the OpenAPI shape produced from minimal-API wiring. Committed `web/src/api/openapi.json` and `web/src/api/types.ts` are regenerated, never hand-edited. Catalog and both acquisition responses set `Cache-Control: private, no-store` on success and mapped error responses.

Authority: `brief.md` D1–D5 + Principle V clarification; `CONCLUSIONS.md` Cross-Artifact Alignment.

## GET /api/subtitles/languages

Patron-approved catalog contract: 200 `SubtitleLanguagesResponse(string? DefaultLanguage, IReadOnlyList<SubtitleLanguageOptionDto> Languages)` serializes as `{ "defaultLanguage": null, "languages": [{ "code": "pt-BR", "englishName": "Portuguese (BR)", "nativeName": "Português (BR)" }] }` (illustrative entry, not the complete catalog).

`SupportedSubtitleLanguages` supplies canonical codes and names and validates search codes/configured preferences. Return configured preferences first, deduplicated in configured order, followed by all other supported entries ordered by ordinal English name then code. Default is the first preference or null. Empty preferences are valid; explicit supported-language search remains available with otherwise valid provider configuration. This endpoint makes no provider call, requires no provider key, and sets `Cache-Control: private, no-store`. Parameterless and unaffected by FluentValidation 422 unless a validator applies. Generate web types from the C# DTOs; do not maintain a second web catalog.

## GET /api/movies/{id}/subtitles/search

- Query: `language` (required; trim + OrdinalIgnoreCase against catalog; emit canonical Code)
- `200` → `application/json`:

```json
{
  "candidates": [
    {
      "candidateId": "string",
      "language": "string",
      "releaseName": "string",
      "matchPercent": 0,
      "downloadCount": 0
    }
  ],
  "quota": {
    "remainingDownloads": null,
    "resetAtUtc": null
  },
  "downloadAvailability": {
    "enabled": true,
    "reason": ""
  }
}
```

- Notes: `quota` is always present on 200; `quota.remainingDownloads` / `quota.resetAtUtc` are nullable when unknown; never guessed; never omit or null the whole quota object. Relative provider reset/Retry-After values map via `timeProvider.GetUtcNow().Add(duration)`. Read-only library may still return `200` with candidates and `downloadAvailability.enabled=false` + `reason: "read_only_library"` when provider settings are valid. Missing/invalid provider configuration → `409` `provider_not_configured`. Empty preferred languages alone must not produce acquisition `409`.

### Search errors

All errors: RFC 7807 ProblemDetails via centralized `IExceptionHandler` + `AddProblemDetails()`, include `traceId`, `extensions["reason"]`, optional `extensions["quota"]` when known. Never credentials/paths/raw provider text.

| Status | reason | Notes |
|---|---|---|
| 422 | `invalid_request` | Actual FluentValidation `ValidationException` from decorator |
| 400 | `invalid_request` (or applicable) | Malformed HTTP/route/query/JSON binding or domain language precheck outside ValidationException |
| 403 | `access_denied` | Access/containment |
| 404 | `movie_not_found` | Missing movie |
| 409 | `provider_not_configured` | Missing/invalid acquisition configuration when search cannot run |
| 429 | `search_rate_limited` | Free-search rate limit — **not** proof of download quota exhaustion |
| 502 | `provider_unavailable` | Provider failure |
| 504 | `provider_timeout` | Upstream timeout while caller request remains active |
| (cancel) | — | Caller cancellation is not mapped as 504 or quota consumption |

## POST /api/movies/{id}/subtitles/download

- Body: `{ "candidateId": "string" }` only — no paths, URLs, or raw provider file IDs.
- `201` → `application/json`: `{ "track": { /* existing SubtitleTrackDto */ }, "quota": { "remainingDownloads": null, "resetAtUtc": null } }` — `quota` always present; fields nullable when unknown.

### POST prechecks before paid call (order)

1. Missing movie → 404 `movie_not_found`
2. Invalid/stale/unknown/evicted/restart-lost/cross-movie binding → 400 `stale_candidate`
3. Read-only policy → 409 `read_only_library`
4. Missing/invalid provider configuration → 409 `provider_not_configured`
5. Containment refusal → 403 `access_denied`
6. Known `remainingDownloads == 0` → 429 `download_quota_exhausted` (no elapsed-reset replenishment). Unknown or positive last-known quota may proceed after prechecks.

### Download errors

| Status | reason | Notes |
|---|---|---|
| 422 | `invalid_request` | Actual FluentValidation `ValidationException` from decorator |
| 400 | `invalid_request` / `stale_candidate` | Malformed binding/body **or** domain/token precheck outside ValidationException |
| 403 | `access_denied` | Containment/access (incl. create / pre-stage / pre-publication checkpoints) |
| 404 | `movie_not_found` | Missing movie |
| 409 | `read_only_library` / `provider_not_configured` | Read-only before config; also publication-attempt ceiling exhaustion → `read_only_library` |
| 429 | `download_quota_exhausted` | Paid download quota; include known quota/reset metadata |
| 502 | `provider_unavailable` | Provider failure, invalid/non-SRT, or streaming body over `MaxSubtitleDownloadBytes` (2 MiB default) before staging; no invented charge/refund |
| 504 | `provider_timeout` | Upstream timeout while caller still active |
| (cancel) | — | Propagate cancellation through body read/staging/publication; cleanup owned staging; never 504 |

## Removed

- `POST /api/movies/{id}/subtitles/prepare` — must not be mapped; callers observe `404`.
- Prepare-only OpenAPI operation, DTO, and generated TS symbols removed on final regenerate after sequential shared edits.

## Retained (unchanged contracts unless noted)

- `GET /api/movies/{id}/subtitles` — local track list (`SubtitleTrackDto[]`)
- `GET /api/movies/{id}/subtitles/{trackId}/content` — WebVTT content
- `GET /api/movies/{id}/subtitles/sidecar` — exact-basename sidecar serve (DEV-453); not removed by this ticket
