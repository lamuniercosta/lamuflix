# OpenSubtitles REST API: observed contract

## 1. Purpose

Reference for the test emulators. Records behavior observed from the live service, not vendor documentation.

- Observed 2026-11-19 and 2026-11-20. Revision of record: API v1 as served then.
- API base: `https://api.opensubtitles.com/api/v1`. Download links use `www.opensubtitles.com` (section 6).
- Sources: no-key probe record, keyed probe record (single key, capped count), no-key canonicalization probes of 2026-11-20.
- Not recorded: keys, download tokens, uploader identities, comment text, request IDs, IPs.

## 2. Request canonicalization (301)

Runs before authentication, so a non-canonical request gets a 301 even with no key.

| Input | Redirects to |
|---|---|
| `query=13+Going+On+30` | `query=13+going+on+30` |
| `query=AMELIE` | `query=amelie` |
| `query=13%20going` | `query=13+going` (space becomes `+`) |
| `languages=PT-BR`, `Languages=pt-br` | `languages=pt-br` |
| `type=Movie` | `type=movie` |
| `moviehash=ABCDEF0123456789` | lowercase hex |
| `moviehash=...&languages=pt-br` | `languages=pt-br&moviehash=...` (languages first) |
| `query=...&languages=pt-br` | `languages=pt-br&query=...` (languages first) |

**Not redirected** (accepted as canonical; 403 without a key):

- Percent-encoded non-ASCII values, any case: `query=am%C3%A9lie`, `am%c3%a9lie`, `Am%C3%A9lie`, `AM%C3%89LIE`.
- `query=star+wars%3A+episode`, `%3a`, literal `:`, `tom+%26+jerry`, `x%2By`.
- `languages=pt-br&query=amelie&type=movie`.
- Comma lists `languages=en,pt-br` and `pt-br,en`.
- Trailing slash `/subtitles/?...`.

**301 shape:** `Location` is path-relative (`/api/v1/subtitles?...`); `X-OS-Rule: canonical`; `X-Canonical: 1`; `Content-Type: text/html` with a short "301 Moved Permanently" page. No `RateLimit-*` headers.

## 3. Auth and headers

| Case | Status | Body |
|---|---|---|
| No `Api-Key`, GET `/subtitles` or POST `/download` or POST `/subtitles` | 403 | JSON, `message` "You cannot consume this service", plus one request-trace field |
| No `User-Agent` (HttpClient default) | 403 | `{"error":"User agent required"}` |
| Valid key, POST `/subtitles` | 404 | `{"status":404,"error":"Not Found"}` |

- Key header: `Api-Key: <key>`. Probes sent `User-Agent: LamuFlix v1.0`.
- `Accept: application/json` made no difference to the 403 result. It does matter for an authorized download (section 5).
- Responses carry `Access-Control-Allow-Origin: *`.

## 4. Search: `GET /api/v1/subtitles`

**Parameters observed:** `languages` (lowercase, `pt-br`; comma list works with a key), `query` (lowercase, `+` for spaces), `moviehash` (16 lowercase hex, no-match case only), `type=movie` (canonicalization only).

**Top-level keys:** `total_pages`, `total_count`, `per_page` (50), `page` (1), `data`. Each `data[]` item has `id` (string), `type` (`"subtitle"`), `attributes`.

**Empty result** (hash no-match, unknown language, no hits):

```json
{"total_pages":0,"total_count":0,"per_page":50,"page":1,"data":[]}
```

Unknown language (`languages=zz`) returns 200 empty, not an error.

**`attributes` fields LamuFlix uses:**

| Field | Type | Status |
|---|---|---|
| `language` | string; returned as `pt-BR` | Observed |
| `download_count` | integer | Observed |
| `release` | string | Observed |
| `moviehash_match` | boolean, hash search only | Not observed (no hash hit) |
| `files[].file_id` | integer | Observed |
| `files[].file_name` | string | Observed |
| `feature_details.feature_type` | string | `"Episode"` observed; `"Movie"` not observed |

Other fields are present (values not recorded). `comments` and `uploader` hold user-written content; emulators should return empty or placeholder values.

**Title search:** unfiltered `query=13 going on 30` returned Episode results from the series "Go On" (`total_count` 3262). Title search mixes in Episode results. A `type=movie` filtered body was not observed.

## 5. Download: `POST /api/v1/download`

Request: `Content-Type: application/json`, `Accept: application/json`, body `{"file_id": <integer>}` from `files[].file_id`. Needs `Api-Key` (403 without).

**`Accept` is required** (observed 2026-11-20, back-to-back probes with the same valid key, User-Agent and body):

| `Accept` header | Status | Body |
|---|---|---|
| absent | 503 | `text/html` error page titled "Error 503 - OpenSubtitles.com"; `Server: cloudflare`, `Via: 1.1 kong/3.9.1` |
| `application/json` | 200 | The JSON body below |

- The 503 repeated on every request without the header over several minutes, so it is not an outage. Chunked and `Content-Length` bodies behaved the same.
- Search (`GET /subtitles`) returned 200 with no `Accept` header.
- Whether a missing key without `Accept` gives 403 or 503 was not observed. Emulators check the key first.
- `GET /api/v1/infos/user` with only `Api-Key` returns 401; it needs a login Bearer token, so a 401 there does not mean the key is bad.

Response 200:

| Field | Type | Notes |
|---|---|---|
| `link` | string | Download URL on the www host (section 6) |
| `file_name` | string | Ends in `.srt` |
| `requests` | integer | Counted for this call (1) |
| `remaining` | integer | Daily quota left (99 after one call) |
| `message` | string | Human-readable quota sentence |
| `reset_time` | string | Relative, e.g. "17 hours and 9 minutes" |
| `reset_time_utc` | string | ISO 8601 with ms and `Z` |
| `uk`, `uid`, `ts` | string, integer, integer | Undocumented extras; values not recorded |

- Daily quota is 100, reported in the body only, not in headers.
- Headers: `Cache-Control: must-revalidate, max-age=0, s-maxage=600, private`, `Pragma: no-cache`, `Expires: 0`.

## 6. Download link

- Host `www.opensubtitles.com`, path `/download/<token>/subfile/<name>`.
- `GET` with no `Api-Key` (User-Agent sent): 200, no redirect, no `Location`.
- `Content-Type: text/plain` (no charset). `Content-Disposition: attachment` (no filename parameter). No `Content-Length`.
- Body is SRT text.

## 7. Rate-limit headers

Present on 200 and 404 responses from the API host:

| Header | Observed |
|---|---|
| `RateLimit-Limit` | 5 |
| `RateLimit-Remaining` | 4 (after one request, paced at 1 s) |
| `RateLimit-Reset` | 1 |
| `X-RateLimit-Limit-Second` | 5 |
| `X-RateLimit-Remaining-Second` | 4 |

- These limits are **per second**. The daily limit is in the download body (section 5).
- 301 and 403 responses carry no rate-limit headers.
- Search responses are cached at Cloudflare (`max-age=86400`); a repeated GET can return `cf-cache-status: HIT`, so cached headers and timings do not reflect the origin.

## 8. Unverified

- **Invalid-key response.** Not observed. The probe labelled "bad key shape" returned a cached valid response (`cf-cache-status: HIT`).
- **429.** Not observed.
- **Download quota exhausted** (over 100 per day). Not observed.
- **`moviehash_match` on a hit**, **`type=movie` filtered body**, **pagination beyond page 1**. Not observed.
