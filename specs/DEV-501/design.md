# DEV-501 — Subtitles tab design (Phase 2)

Authority: `specs/DEV-501/brief.md`, `specs/DEV-501/CONCLUSIONS.md` (Q1–Q9), `specs/DEV-501/ASSUMPTIONS.md`, Maestri `DEV-501` and `recon-DEV-501`.
Scope: player CC Subtitles tab only — language search, candidate download, quota display, match percent and download counts, read-only library state. No new page, dependency, layer, schema, or local-player execution change. Mode: Operate — scanability and restraint outrank expression.

## Design intent

One quiet panel that keeps video playing and local-track switching always usable. The single memorable decision: remote results read as a ranked ledger — right-aligned tabular match percent, then download count — so the ranking the server guarantees (match percent, download count, ordinal release name, candidate ID) is visible at a glance. Everything else stays plain: native controls, sentence-case copy, inline status lines, no cards-within-cards.

## Route map

No new top-level route. The Subtitles tab lives inside the existing player surface that already hosts `SubtitleSelector`:

- Existing movie playback surface (current `BrowserPlayer` route) → CC control → Subtitles tab → this design.
- Language selection and candidate download are tab-local state changes, not navigations. No deep link per language or per candidate.
- Browser back/forward never changes subtitle state. Refresh loses only transient search state; the active local track re-resolves through the existing local-track path.

## Layout wireframe

```
+------------------------------------------------------+
| Subtitles tab                                        |
|                                                      |
|  [Search by language: Portuguese (BR) — Português...]|
|  Quota: 98 downloads left · Resets 14:32             |
|  ─────────────────────────────────────────────────   |
|  LOCAL TRACKS                                        |
|  ( ) Off                                              |
|  (*) Dune (2021)-pt-BR.srt             [active]      |
|  ( ) Dune (2021)-pt-BR(1).srt                        |
|  ─────────────────────────────────────────────────   |
|  RESULTS for Portuguese (BR)                         |
|  Dune.2021.1080p.BluRay...  96%  1,204  [Get]       |
|  Dune.2021.WEB-DL.H264...    82%    860  [Get]       |
|  Dune.2021.HDCAM...          41%     12  [Get]       |
|  ─────────────────────────────────────────────────   |
|  status line (role=status): Ready. / Searching… /    |
|  No subtitles found for this language. / reason      |
+------------------------------------------------------+
```

Notes: local tracks and Off always render above results in every state. Remote rows truncate the release name with the full value on hover and keyboard focus. The active check appears only on the truly active downloaded track in LocalTrackList, never on a candidate. Never match remote candidates to local tracks by release-name or file-name heuristics, and add no candidate-to-track persistence or identity mapping. Quota line sits directly under the language control so the cost of Get is visible before the action.

Narrow widths stack the row as release name on line one, then `96% · 1,204 downloads · [Get]` on line two. Table and tab order do not change.

## Component tree

```
BrowserPlayer (existing, owns video + CC tab shell)
└─ SubtitlesTab (new tab section; extends the area now owned by SubtitleSelector)
   ├─ LanguageSearch (label + native select; "Search by language")
   │  └─ QuotaLine (remainingDownloads + resetAtUtc rendered local; unknown → unavailable copy)
   ├─ LocalTrackList (radio group: Off + local tracks incl. Subs\; always enabled)
   ├─ CandidateList (ranked rows in server order; no client re-sort)
   │  └─ CandidateRow (truncated releaseName, matchPercent, downloadCount, Get button; no active check)
   └─ SubtitleStatus (single aria-live polite line: loading / empty / error / disabled reason)
```

No new state library. Tab-local state only: selected language, in-flight search generation, selected candidateId, download in-flight flag. `SubtitleSelector` keeps Off/local switching; the new tab section adds search/download beside it without replacing its contract.

## States

| State | What shows | Video |
|---|---|---|
| Searching (loading) | Skeleton or "Searching…" in status line; prior results stay visible but dimmed; Get buttons disabled | Keeps playing |
| Empty results | "No subtitles found for this language." Local tracks + Off remain usable | Keeps playing |
| No language configured | Choose-language prompt, no auto-search | Keeps playing |
| Download in flight | Row button becomes "Getting…" (disabled); all other Get buttons disabled; selection locked | Keeps playing |
| Download success (201) | Local list refreshes, new track activates only if same movie/session still current; status confirms; stale-language completion adds the track without replacing current results | Keeps playing |
| Stale/expired candidate (400) | Inline "That result expired. Search again." No paid retry | Keeps playing |
| Validation failure (422) | Actual FluentValidation `ValidationException` from the validation decorator surfaces inline via the status line with reason `invalid_request`; no paid call | Keeps playing |
| Search rate-limited (429) | "Subtitle search is temporarily rate-limited. Try again in a moment." Never marks download quota exhausted | Keeps playing |
| Download quota reached (429) | "Download quota reached. Resets at {localTime}." or "Download quota reached." when reset is unknown; Get buttons disabled with reason | Keeps playing |
| Provider failure/timeout (502/504) | Inline message + manual Retry; no auto-retry. Caller cancellation is never shown as a timeout; only an upstream timeout while the caller request is still active is a timeout | Keeps playing |
| Invalid or oversized subtitle payload (502) | Inline message + manual Retry; rejected before staging with no quota charge/refund claim | Keeps playing |
| Read-only library (409 policy / enabled false) | "Downloads unavailable for this library." + server reason; Get buttons disabled in place, never fire POST; search still works with valid provider configuration | Keeps playing |
| Unknown quota | Quota line reads unavailable; never 0, never an inferred reset | Keeps playing |
| Content error | Existing "Subtitles unavailable" path retained beside the tab | Keeps playing |

Partial data rule: render whatever the provider supplied and say unavailable for the rest. An elapsed reset timestamp alone never implies replenishment.

## Data needs (endpoint + fields)

Web code consumes generated TypeScript from `web/src/api/openapi.json` → `web/src/api/types.ts`, including the language catalog DTOs. No hand-rolled duplicates.

- `GET /api/subtitles/languages` (200) → `{ defaultLanguage, languages: [{ code, englishName, nativeName }] }`. Configured preferences first in configured order, deduplicated, then remaining supported entries by ordinal `englishName` then `code`; `defaultLanguage` is the first preference or null. `Cache-Control: private, no-store`; no provider call; no provider key. Non-null default starts the free search; null shows the choose-language prompt with no search. Options render `englishName — nativeName`.
- `GET /api/movies/{id}/subtitles/search?language=` (200) → `candidates[{ candidateId, language, releaseName, matchPercent, downloadCount }]`, `quota{ remainingDownloads, resetAtUtc }` (both nullable when unknown), `downloadAvailability{ enabled, reason }`. Free call. `Cache-Control: private, no-store`.
- `POST /api/movies/{id}/subtitles/download` (201) ← `{ candidateId }` only → `{ track, quota }`. Single explicit call per pick; disabled while one is in flight; never auto-retried. Refused client-side when `downloadAvailability.enabled` is false.
- Existing local-track list/content endpoints feed `LocalTrackList` and Off switching unchanged.
- Error shape: RFC 7807 ProblemDetails with `extensions["reason"]` and optional `extensions["quota"] = { remainingDownloads, resetAtUtc }` when known; never credentials, paths, or raw provider text. Reasons: `invalid_request`, `stale_candidate`, `access_denied`, `movie_not_found`, `read_only_library`, `provider_not_configured`, `search_rate_limited`, `download_quota_exhausted`, `provider_unavailable`, `provider_timeout`. `GET` search with missing/invalid provider configuration returns 409 `provider_not_configured`; read-only with valid configuration returns 200 with `downloadAvailability: { enabled: false, reason: "read_only_library" }`. `POST` checks read-only policy (409 `read_only_library`) before provider configuration (409 `provider_not_configured`), before any paid call. Caller cancellation is never mapped or displayed as 504 `provider_timeout`. Invalid/non-SRT or oversized payloads (2 MiB streaming cap) return 502 `provider_unavailable` before staging with no quota charge/refund claim. An exhausted collision scan returns 409 `read_only_library` with owned-staging cleanup and no new copy promise. Actual FluentValidation `ValidationException` from the validation decorator on search/download returns 422 `invalid_request`; malformed HTTP/route/query/JSON binding and domain/token prechecks outside that exception (including stale/unknown/evicted/cross-movie bindings) stay 400. The parameterless catalog is unaffected unless a validator applies. Error mapping stays in the single centralized `IExceptionHandler` using `AddProblemDetails()`, every error carries `traceId`, and endpoints never globally remap `ValidationException` or return raw exception text. Status map: 400 binding/stale/unknown/cross-movie prechecks, 403 containment, 404 missing movie, 409 config/disabled/read-only/collision-exhausted POST, 422 decorator validation, 429 search vs download, 502 provider/invalid/oversized, 504 timeout.

Interaction contracts: language change fires a free search, discards stale in-flight responses, preserves playback and the active track. Candidate order is server order, rendered as-is. Quota reset UTC renders in local time.

## Accessibility

- Native `<select>` for language with a real `<label>` ("Search by language"); options show "English Name — Native Name" (e.g. Portuguese (BR) — Português (BR)).
- Local tracks as a real radio group; candidates as a list of rows each with a real `<button>` for Get. Disabled Get buttons keep their reason in visible text and `aria-disabled`/`disabled` plus `aria-describedby` pointing at the status line.
- Truncated release names expose the full value on hover and on keyboard focus (`title` + focusable element); match percent and download count are plain text, never color-only.
- One `role="status"` live region for the status line; download completion and errors announce there without moving focus. Focus stays where the viewer put it; no focus theft on search refresh.
- Full keyboard path: Tab reaches language select → quota (static text, skipped) → Off/local radios → each Get button → Retry. Enter/Space activates. Escape closes the CC panel per existing behavior and never cancels video.
- Visible focus on every control; forced-colors and high-contrast modes keep percent/count legible as text.
- Reduced motion: no entrance animation in this tab; downloading state is a text change ("Getting…"), not a spinner that requires motion.
- Copy: sentence case throughout. "Search by language", "No subtitles found for this language", "Downloads unavailable for this library", "Subtitle search is temporarily rate-limited. Try again in a moment.", "Download quota reached. Resets at {localTime}." / "Download quota reached."

## Explicit non-goals

No SubDL provider, no second provider UI affordance, no client-side ranking or re-sort, no conversion or non-SRT handling in the tab, no delay/style controls here, no new routes or persisted preferences. Prepare references stay gone per brief.
