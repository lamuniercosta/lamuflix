# Data model: DEV-501 Explicit subtitle search and download

Authority: `brief.md` Contract decisions / D1–D5 / Principle V clarification; `CONCLUSIONS` Q3–Q9 + plan-challenge rulings.

## Provider-neutral domain shapes

- **SubtitleCandidate** (existing port record; extend as needed): provider file identity stays adapter-internal; public surface uses opaque candidateId. Needed public/search fields: language (canonical catalog Code), releaseName, downloadCount, matchesMovieHash (for 100% score). Do not expose OpenSubtitles-specific fields on public DTOs.
- **Quota snapshot**: `remainingDownloads: int?`, `resetAtUtc: DateTimeOffset?` — null when provider values unknown; never invent; never claim charge/refund without provider evidence. Relative durations/Retry-After → `timeProvider.GetUtcNow().Add(duration)`.
- **Download availability**: `enabled: bool`, `reason: string` (safe; e.g. `read_only_library` when disabled).

## Language catalog

- **SupportedSubtitleLanguages**: single Core catalog of `(Code, EnglishName, NativeName)`.
- **Normalization**: trim whitespace; match with `StringComparison.OrdinalIgnoreCase`; emit canonical `Code` in DTOs, bindings, provider requests, filenames.
- **Preferences**: empty PreferredLanguages valid; entries must resolve to supported codes; catalog order = configured preferences (deduped, configured order) then remaining by ordinal English name then code; `defaultLanguage` = first preference or null.
- **HTTP**: `SubtitleLanguagesResponse { string? defaultLanguage; IReadOnlyList<SubtitleLanguageOptionDto> languages }` where option = `{ code, englishName, nativeName }`.

## Candidate binding (process-local)

- **Type**: concrete `SubtitleCandidateBinder` in `LamuFlix.Core.Features.Subtitles` — DI Singleton, thread-safe; **no** interface/port/layer/distributed store; no mutable static initialization.
- **Binding**: `(candidateId, movieId, language, providerCandidate, expiresAtUtc)`.
- **Token**: opaque 32-hex-character / 128-bit under lock from injected seeded `Random` + `TimeProvider`; not claimed cryptographically secure.
- **Options** (`SubtitlesOptions`): `CandidateTokenTtl` default `00:05:00` (> zero); `CandidateStoreMaxEntries` default 1000 (>= 1).
- **Eviction**: on insert, purge `expiresAtUtc <= now`; if still full, evict earliest `expiresAtUtc` with FIFO tie-break; new search issuance succeeds.
- **Rules**: issue IDs only from successful search; resolve on download; expired/evicted/restart-lost/cross-movie → 400 `stale_candidate` before paid call; process-local only (replica miss → treat stale / re-search; no affinity claim).

## Match scoring

- **Type**: concrete `SubtitleMatchScorer` in `LamuFlix.Core.Features.Subtitles`.
- Inputs: video filename stem (server-derived), candidate releaseName, matchesMovieHash flag, downloadCount.
- Output: integer `matchPercent` in `[0, 100]`.
- Formula: hash match → 100; else distinct-token Jaccard × 100 rounded `AwayFromZero`; empty union → 0.
- Sort key: matchPercent ↓, downloadCount ↓, ordinal releaseName, candidateId.
- Property proofs required: bounds, hash override, deterministic ordering, overlap symmetry/empty.

## Filesystem publication

| Stage | Path rule |
|---|---|
| Final | `<movie folder>/Subs/<stem>-<canonicalCode>.srt` then `<stem>-<code>(1).srt`, `(2)`, … (no space) |
| Staging | One private leading-dot `.srt` inside same `Subs\` reused across publication attempts; discovery must exclude |
| Bounds | `MaxSubtitleDownloadBytes` default `2 * 1024 * 1024` while streaming provider body; `MaxSubtitlePublicationAttempts` default 10_000 (typed positive options) |
| Checkpoints | Containment before directory creation; after creation/before staging; before each publication attempt |
| Policy | Create Subs if missing; validate SRT before stage; non-replacing publish; cleanup owned staging on cancel/finally/attempt exhaustion (409 `read_only_library`); never overwrite/delete published library files |

## HTTP DTOs (Api)

- **Languages response (200)**: `SubtitleLanguagesResponse` as above; private no-store; no provider call/key.
- **Search response (200)**: `SubtitleSearchResponse { CandidateDto[] candidates; QuotaDto quota; DownloadAvailabilityDto downloadAvailability }` — `quota` always present; unknown provider values use null **fields** only.
- **CandidateDto**: `{ string candidateId, string language, string releaseName, int matchPercent, int downloadCount }`.
- **QuotaDto**: `{ int? remainingDownloads, DateTimeOffset? resetAtUtc }`.
- **DownloadAvailabilityDto**: `{ bool enabled, string reason }`.
- **Download request**: `{ string candidateId }` only.
- **Download response (201)**: `{ SubtitleTrackDto track, QuotaDto quota }`.
- **Removed**: `SubtitlePrepareResponse` and prepare-only command/result types.

## Error contract (ProblemDetails)

Every non-2xx acquisition/catalog-related error uses RFC 7807 ProblemDetails from centralized `IExceptionHandler` + `AddProblemDetails()` and **must include `traceId`**. Optional `extensions["quota"] = { remainingDownloads, resetAtUtc }` when known. `extensions["reason"]` is one of:

`invalid_request`, `stale_candidate`, `access_denied`, `movie_not_found`, `read_only_library`, `provider_not_configured`, `search_rate_limited`, `download_quota_exhausted`, `provider_unavailable`, `provider_timeout`.

| Status | When |
|---|---|
| **422** | Actual FluentValidation `ValidationException` from validation decorator (`invalid_request`). No global remap. |
| **400** | Malformed HTTP/route/query/JSON binding or domain/token prechecks **outside** ValidationException (e.g. `stale_candidate`, non-VE language domain refusal). |
| **403** | Containment/access (`access_denied`). |
| **404** | Missing movie (`movie_not_found`). |
| **409** | Read-only (`read_only_library`) or missing/invalid provider config (`provider_not_configured`); POST checks read-only before config; publication-attempt exhaustion → `read_only_library`. |
| **429** | GET `search_rate_limited` vs POST `download_quota_exhausted` (known remainingDownloads == 0 before paid call). |
| **502** | Provider failure or invalid/oversized payload before staging (`provider_unavailable`). |
| **504** | Upstream provider timeout while caller request still active (`provider_timeout`). Caller cancellation is not 504. |

Catalog endpoint is parameterless and unaffected unless a validator applies. Never credentials, paths, raw provider text, or `BadRequest(ex.Message)`.

## Configuration (retained / extended / trimmed)

- Retain: `Subtitles:SaveEnabled`, `Subtitles:PreferredLanguages`, `Subtitles:OpenSubtitles:*`, secret isolation, startup non-failure for acquisition settings.
- Add on `SubtitlesOptions`: `CandidateTokenTtl`, `CandidateStoreMaxEntries`, `MaxSubtitleDownloadBytes`, `MaxSubtitlePublicationAttempts` with positive validation; preserve acquisition-startup-safe validation.
- Remove: settings that exist solely for prepare auto-pick / Film.srt (not SaveEnabled).

## State transitions (download)

```text
Search 200 → candidateIds issued (32-hex)
  → UI Get (enabled; last-known quota null or > 0)
      → POST order: movie 404 → binding 400 → read-only 409 → config 409 → containment 403
      → refuse known-zero quota (429) else paid provider download (stream ≤ 2 MiB)
      → validate SRT → stage once → publish with rechecks/(N)/attempt ceiling
      → 201 track (+ quota)
  → on failure: ProblemDetails with reason + traceId; no partial local track
  → on cancel: cleanup owned staging; not provider_timeout
```

Pickup note: recon:116/119/125/128 leaves prepare/shared-file, language-validator, provider-body, and installer/snapshot inventories incomplete; T003/T004/T006/T025/T026 verify before mutation.
