# DEV-552 search stream and additive metadata contract

Authority: `brief.md` Plan challenge dispositions items 1–9; `CONCLUSIONS.md` Q1–Q8; Proposed ADR 0053 (updated); DEV-551 consumer contract for retained aggregate JSON and pre-stream error semantics. This file names the **additive** wire shape for Spec Kit and OpenAPI generation. It does not invent routes or remove legacy fields. Patron Q8 (`downloadAvailability` on `providerSettled`) approved at `e9175fcb`.

## Request (unchanged path; opt-in representation)

- `GET /api/movies/{id}/subtitles/search?language={code}`
- No provider query parameter (Q1; matches DEV-551)
- One requested language per call
- Default representation: existing JSON aggregate (semantics and status codes unchanged)
- Opt-in progressive representation: `Accept: text/event-stream` using built-in ASP.NET Core 10 `TypedResults.ServerSentEvents` (Q1; [ASP.NET Core 10 SSE](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0#minimal-apis))
- Negotiated GET MUST preserve existing search `Cache-Control: private, no-store` handling and declare `Vary: Accept` so caches cannot mix JSON and SSE representations (disposition item 7)

## Pre-stream JSON errors (unchanged timing; retained DEV-551 semantics)

Request-level domain errors, validation failures, and no-usable-provider **409** remain JSON **before any stream opens**, with DEV-551 error semantics retained (Q4; disposition item 1). Once a stream has opened, provider outcomes settle as stream events rather than fabricating missing providers on incomplete close.

### Provider-set snapshot (request-scoped; disposition item 1)

For a validated request, capture configured provider identities and their **initial eligibility** before starting the shared fan-out:

1. **Zero usable providers** → existing JSON **409** before SSE headers or any stream bytes (all-unusable never opens a stream of N unusable events).
2. **Opened stream** → emit exactly one outcome per provider in that snapshot, including initially off/unusable providers.
3. **Terminal count** → `settledProviderCount` equals the snapshot size.
4. Changes to the configured/eligibility set after the snapshot belong to the **next** request; late joiners are excluded from the current stream.

## Additive languages field

On the existing languages response (`GET /api/subtitles/languages`):

| Field | Shape | Notes |
| --- | --- | --- |
| `preferredLanguageCodes` | `string[]` (ordered) | Canonical supported codes from `Subtitles:PreferredLanguages` that are present in the supported catalog. Empty array is valid. Backend proves this ordered catalog field; the **picker** owns empty-list fallback to one automatic search of `defaultLanguage` when non-null, else none; **Add a language** stays (Q2). |

Legacy `defaultLanguage` and `languages[]` retained unchanged.

## Additive candidate field (JSON and stream)

| Field | Shape | Notes |
| --- | --- | --- |
| `candidates[].hashMatch` | `boolean` (non-null) | `true` only on **explicit affirmative provider evidence** carried as a nullable explicit-evidence value on the existing candidate result through `ISubtitleProvider` (for example `ExplicitHashMatch`). Public `hashMatch` is `true` only when that evidence is affirmatively true on the hash path. Missing or false evidence — including providers with no defined affirmative signal — and title fallback produce `false`. Keep `MatchesMovieHash` and its scoring fallback intact. Never inferred from match percent (Q2; disposition item 5; recon-DEV-552:4–7). |

DEV-551 additive `providerId` and `providers[]` (when landed) remain required consumer inputs for the picker; this ticket does not remove them.

## SSE event payloads (named C# DTOs → OpenAPI → generated TypeScript)

Quill drafting names authorized by `brief.md` Approach item 3 / Q1. OpenAPI emission must be verified; register schemas explicitly if the generator omits stream item schemas. Consume only via generated `web/src/api/types.ts` (never hand-rolled).

Server-side SSE uses a **single** `SseItem<T>` item type that can carry either concrete DTO. Explicit OpenAPI/`Produces` metadata must declare **both** `200` content types (`application/json` and `text/event-stream`) on the negotiated GET (brief disposition item 7).

### Exact wire discriminators (drafted names; Q1 freezes opt-in SSE + named DTOs)

| SSE `event` wire name | Payload DTO | When |
| --- | --- | --- |
| `providerSettled` | `SubtitleProviderSettledEventDto` | Once per provider in the request snapshot as that provider settles |
| `searchCompleted` | `SubtitleSearchStreamCompletedDto` | Exactly once after every snapshot provider has settled |

These Quill-drafted discriminators are the interoperability contract used by endpoint emission, contract tests, and the web consumer. They are not themselves Patron-named Q1/Q2/Q4 tokens; Q1 requires named C# DTOs in OpenAPI and opt-in `Accept: text/event-stream`.

### Provider-settled event — `SubtitleProviderSettledEventDto` (`event: providerSettled`)

Emitted once per snapshot provider as that provider settles (including immediate off/unusable) (Q1, Q4; disposition item 1).

| Field | Shape | Notes |
| --- | --- | --- |
| `provider` | object | One DEV-551-shaped provider outcome row: `providerId`, closed `state`, closed `reason`, nullable `quota` in existing quota shape |
| `candidates` | array | Candidates contributed by that settled provider for this language; each includes legacy candidate fields plus `providerId` (DEV-551) and `hashMatch` (this ticket) |
| `downloadAvailability` | `SubtitleDownloadAvailabilityDto` (required, non-null) | Reuses the existing DTO (`Enabled`, `Reason`). One availability snapshot is taken when the successful stream opens and is repeated **unchanged** on every `providerSettled` event of that stream. No new DTO, no new event type, no second search. Default JSON search response and download route remain authoritative and unchanged (Q8; disposition item 9 / repair item 7; recon-DEV-552:11–14). |

### Terminal completion event — `SubtitleSearchStreamCompletedDto` (`event: searchCompleted`)

Exactly one terminal event after all snapshot providers have settled (Q4; disposition item 1).

| Field | Shape | Notes |
| --- | --- | --- |
| `settledProviderCount` | integer | Equals the request-scoped snapshot size. Client treats a close **without** this event as incomplete for that language. |

### Stream lifecycle rules

1. Snapshot configured provider identities and initial eligibility before fan-out (disposition item 1).
2. Emit one `providerSettled` event per snapshot provider (off/unusable included).
3. Emit terminal `searchCompleted` with `settledProviderCount` equal to snapshot size.
4. Close the stream.
5. Client disconnect / request abort cancels outstanding work: carry `HttpContext.RequestAborted` through shared fan-out, provider calls, and outbound HTTP while keeping existing deadlines; stop post-abort title fallback and further upstream calls (Q4; disposition item 2).
6. No automatic retry of provider calls after interruption (Q4).
7. Built-in SSE writer delivers per event; supported response-path buffering must not delay provider progress. Preserve search no-store and `Vary: Accept` (brief disposition item 7).
8. On successful stream open, snapshot `downloadAvailability` once and repeat it unchanged on every `providerSettled` event (Q8).

### Client transport (web; brief disposition item 7)

Picker search uses `fetch` with the streaming response body, an in-repo parser (inside `useSubtitleAcquisition.ts` or an existing helper), and `AbortController`. No `EventSource`. No reconnect. Required `Accept: text/event-stream` on success path. Consume generated `downloadAvailability` from the first `providerSettled` and apply it to the first visible candidates, including partial rows after interruption. Read-only search shows candidates and the read-only reason, disables candidate and batch download actions, and issues zero download POSTs (Q8).

## Download route (unchanged)

- Existing single-candidate `POST .../subtitles/download` body and responses unchanged (Q1, Q5, Q6)
- Batching is client-side sequential calls only

## Call / I/O topology (documented cost; disposition item 3)

Documented call and I/O cost is **concurrent preferred languages × usable providers**, with the existing conditional hash-to-title fallback adding another provider call where that path applies (recon-DEV-552:4–7). Retain the existing hash reader and one concurrent request per preferred language. Cross-request hash sharing, throttling, and any changed search concurrency are follow-up scope. This topology does not claim measured performance.

## Hash evidence verification note (plan fact; Q2; disposition item 5)

Worktree verification at draft time (`recon-DEV-552` current lines 4–7 at `066ffed9`; `OpenSubtitlesSubtitleProvider` / `SearchSubtitlesQueryHandler`):

- Handler reads the file hash once per language search and supplies it only when nonblank; if hash search returns candidates it returns immediately; otherwise, when the target title is nonblank, it performs title search with `MovieHash: null` (`SearchSubtitlesQueryHandler.cs` per recon:4)
- Adapter emits the `moviehash` parameter for nonblank hashes; title search uses `MovieHash: null` when hash search yields no candidates (recon:5)
- Explicit `moviehash_match` is OpenSubtitles’ evidence field (nullable on the wire). Port scoring keeps `MatchesMovieHash` as `attributes.MovieHashMatch ?? hashSearch` (recon:6)
- Name a **nullable explicit-evidence** value on the existing candidate result carried through `ISubtitleProvider` (for example `ExplicitHashMatch`), separate from scoring. Public `hashMatch` requires affirmative evidence on the hash path. Missing/false evidence (including providers with no defined affirmative signal) and title fallback produce `false`. Generic default is false.
- Focused mapping covers the generic result path and the OpenSubtitles adapter cases without inventing another adapter or a live provider proof (recon:4–7 as code-path evidence)
- No new hashing work in this ticket

## Wire-level and generated-schema proof

1. **Stream control / snapshot** — two providers; observe first `providerSettled` event and candidates before releasing the slower provider; assert terminal `searchCompleted` / `settledProviderCount` equals snapshot size; all-unusable → JSON 409 (no stream); mixed snapshot (usable + initially unusable); in-flight configured-set change excluded from current stream; interrupted close without terminal; disconnect cancellation with gated provider proving abort cancels outstanding work and stops post-abort fallback (T007; dispositions 1–2, 7)
2. **Framing / buffering** — exact wire discriminators; flushed first event; supported response-path buffering does not delay provider progress; disconnect (T007 framing; T005 delivery) (disposition item 7)
3. **Pre-stream JSON** — request-level domain errors, validation errors, and no-usable 409 never open a stream (DEV-551 semantics retained)
4. **Legacy JSON + dual 200** — non-SSE Accept retains prior semantics/status codes; additive `hashMatch` present; single-candidate download unmodified; generated/OpenAPI asserts both `application/json` and `text/event-stream` 200 content types and both named payload schemas; JSON compatibility apart from additive `hashMatch`; required reused `SubtitleDownloadAvailabilityDto` / `downloadAvailability` on settled schema (T006; brief disposition items 7 and 9)
5. **Languages (backend)** — catalog ordered `preferredLanguageCodes` field
6. **Hash evidence** — generic mapping false default; OpenSubtitles explicit affirmative → `hashMatch` true; explicit false → false; absent on hash search → false (scoring may still use hashSearch fallback); title-fallback → false; no new adapter (T004/T007; recon:4–7)
7. **Generated schema (backend, before web consumption)** — after regenerate, assert both named schemas (`SubtitleProviderSettledEventDto`, `SubtitleSearchStreamCompletedDto`), additive field types (`preferredLanguageCodes`, `hashMatch`), and required `downloadAvailability` reusing `SubtitleDownloadAvailabilityDto` in `openapi.json` / generated `types.ts`
8. **downloadAvailability invariant** — T007 asserts the identical availability snapshot on every `providerSettled` event in both read-only and enabled modes (Q8)
9. **Picker consumer (after wiring)** — `fetch` + in-repo parser + `AbortController` (no EventSource/reconnect); sends required `Accept: text/event-stream`; handles pre-stream JSON errors; consumes `providerSettled` / `searchCompleted` on success; applies `downloadAvailability` from the first event to visible/partial rows; type-checks against both generated payload types; owns empty-preference → `defaultLanguage` fallback branches; T011 covers split chunks, ignored unknown events, close without terminal, and first-event read-only rows visible with disabled download and zero POST (brief disposition items 7 and 9)
