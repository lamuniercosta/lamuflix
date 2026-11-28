# Contract: SubDL v2 adapter and shared archive reader (DEV-554)

**Status**: Draft from grill-closed brief Q1–Q7 + Step 3.3 + Step 6 Quill fix list. Not a new public HTTP API.

**Authority**: `brief.md` Q1–Q5 + Step 3.3 + Step 6 refinements; `CONCLUSIONS.md` Quill fix list; ticket Wanted 1–5; `recon-DEV-554:108–110` (docs facts).

## Public API

No new routes, DTO fields, or status codes. Existing subtitle search/download surfaces and DEV-551 provider state remain the public shape. Step 3.3 multi-provider 409 semantics are **implemented** on those existing surfaces: T001 cites the **exact landed paths and lines** for 409, health, and registration; lists which 409 files require edits and which sit outside Q4's registration/composition allowance; and requires cited Patron approval in `CONCLUSIONS.md` before T008 edits any out-of-envelope file. Search 409 only when no provider has effective, valid settings; download 409 when the chosen candidate's provider has missing or invalid settings after read-only library precedence. Provider identity reaches DEV-551 state and telemetry as **runtime data** under existing vendor-neutral Core names (including Core test/support types); a proposed vendor-named Core constant requires a constitution ruling (§2.3(b) for any departure). Normal-composition SRT publication/rejection and quota→state evidence are **separate** from 409 implementation.

## External SubDL transport (adapter obligations)

| Concern | Contract |
| --- | --- |
| Search | `GET /api/v2/subtitles/search` on validated HTTPS `BaseUrl` origin only |
| Download | `GET /api/v2/subtitles/{nId}/download` default ZIP mode |
| Path/query construction | Build download paths from a validated single-segment `nId` (honor the documented shape if T001 verifies one); encode query values structurally; reject path-shifting identifiers |
| Auth | `X-API-Key` only on an eligible initial request to the validated HTTPS provider origin; never in URI |
| Redirects | Disable automatic redirect following; keep search on its validated origin; for ZIP redirects resolve and validate each target as HTTPS and download-allowlisted before requesting, with a finite hop limit and immediate rejection at the first invalid hop |
| Archive fetch | Allowed download hosts only; every redirected or separately hosted ZIP fetch is credential-free |
| HTTP compressed bound (adapter) | Bounded **actual-byte** read of the response body applies **before any full response buffering or ZIP open**; `Content-Length` is only an early-rejection aid (never the sole bound). On compressed-cap rejection: cancel the HTTP request and dispose adapter-owned response/stream. Reader-side bound remains the shared-reader contract / T002–T003; this row is the HTTP adapter boundary |
| Client reuse | Reuse landed typed/named HttpClient patterns where compatible (`AllowAutoRedirect=false` or equivalent manual hop handling); T001 inventories search vs download client configuration. T005 may use the recorded test-only transport option for a controlled over-cap response stream without a reliable length |
| Modes forbidden | `unpack=1`, `format=file`, legacy API, TV kinds |
| Search kinds | IMDb ID; TMDb ID with `type=movie`; title and year as one kind |
| Year | Pickup verifies documented v2 request-year filter or result-year filter; conflicting year never returned; if neither mapping exists, transport stays locked pending recon + Patron ruling (not a green year-mapping pass) |
| Options binding | Vendor-neutral Core types bind `Subtitles:SubDl` via typed section binding; optional provider stays out of startup validation (no `ValidateOnStart`); typed validator applied before outbound use; effective-disabled (`Enabled=false` or blank key) vs keyed-invalid (redacted misconfigured DEV-551 state); production non-HTTPS rejected with zero outbound; T001 records HTTPS WireMock configuration or isolated test-only transport seam that preserves production HTTPS validation |
| Caps | Required positive typed settings on SubDL options; example/home use 8 MiB / 32 MiB / 128 subject to owner approval; reader receives caller-supplied limits only (no reader defaults) |
| Diagnostics | Fixed failure reasons and structured safe fields only; omit raw entry names, URLs, identifiers, and quota values; reuse landed structured logging convention with required movie ID context where available |
| Quota attribution | Phase B pickup MUST record **Q2 attribution evidence** and the **documented reset representation** (cite whether current SubDL docs identify the download-response `X-RateLimit-Limit` / `X-RateLimit-Remaining` / `X-RateLimit-Reset` trio as download usage and how Reset is represented; `recon-DEV-554:110`). Map a coherent trio into DEV-551 download quota **only** when that pickup evidence affirms attribution. Reject malformed, negative, overflowing, or incoherent values to **`Unknown` without throwing**. Use injected `TimeProvider` for any relative reset calculation. Search-response counters stay separate and MUST NOT fill download quota. Missing/ambiguous pickup attribution, or absent/partial/malformed/ambiguous snapshots → **`Unknown` fallback** with **no** inferred reset and **no** additional `/api/v2/me` call (brief Q2) |

## Shared archive reader (internal contract)

| Concern | Contract |
| --- | --- |
| Library | BCL `System.IO.Compression.ZipArchive` only; no new package |
| Placement | Concrete class under `src/LamuFlix.Infrastructure/Adapters/` |
| Limits | Caller-passed required values only; shared reader supplies no numeric defaults/fallbacks; SubDL options carry the three required caps (example/home 8 MiB compressed, 32 MiB expanded aggregate, 128 entries subject to owner approval) |
| Compressed enforce | Bounded **actual-byte** read before ZIP open; a length header may only provide early rejection |
| State / lifetime | Limits and counters per invocation; no mutable static state; propagate cancellation through compressed/expanded reads; dispose stream and ZIP resources according to ownership on all exits including rejection |
| Counting | Every entry counts; actual expanded bytes across all entries including ignored extensions |
| Paths | Entry names are data only; never used as filesystem paths; never extract to disk |
| Reject | Malformed ZIP; any cap breach; no `.srt`; unsafe names (rooted, drive-qualified, `..` with `/` or `\`) — distinct fixed safe reasons (omit raw entry names from diagnostics) |
| Rank | Exact normalized stem match → shared distinct release-name tokens → largest actual size → ordinal entry name → archive order |
| Tests | Standalone coverage + FsCheck in `tests/LamuFlix.UnitTests/ZipSubtitleArchiveReaderTests.cs`; WireMock/health/composition remain integration |
| Handoff | Selected SRT bytes enter existing `DownloadSubtitleCommandHandler` size + `EnsureValidSrt` + publish path; **separate** composition evidence (after 409 wiring) asserts publication of valid SRT and rejection of invalid SRT on that path |

## Fixtures

Sanitized recorded WireMock responses; placement = landed DEV-549 location, else Conductor-confirmed path before write; one fake framework. T001 records the HTTPS WireMock or isolated test-only transport seam used with production HTTPS validation.
