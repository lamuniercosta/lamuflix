# Contract: BSPlayer SOAP adapter (DEV-555)

**Status**: Draft from grill-closed brief Q1–Q5 and plan-challenge decisions D1–D15. Not a new public HTTP API.

**Authority**: `brief.md` Q1–Q5 and § Plan-challenge decisions (r1) D1–D15; `CONCLUSIONS.md`; ticket Wanted 1–5 / Done when; `recon-DEV-555:52–108`.

## Public API

No new routes, DTO fields, or status codes. Existing subtitle search/download surfaces and the DEV-551 provider-outcome contract remain the public shape. BSPlayer failures surface as named per-provider outcomes (including `failed` with “BSPlayer is not answering”), not new HTTP statuses (Q2–Q3).

## External BSPlayer transport (adapter obligations)

| Concern | Contract |
| --- | --- |
| Media | Movies only; TV out of scope |
| Search kind | File hash + size; include IMDb id when the movie has one |
| Transport | Keyless SOAP; plain HTTP only under `Subtitles:BsPlayer:AllowPlainHttp: true` |
| HTTPS | In scope only if the service offers it (ticket Out of scope) |
| Servers | Configured `Servers` list; try in **configured list order**; skip non-responders while budget remains (`ASSUMPTIONS.md`) |
| Deadline | One configured `RequestTimeout` bounds probing, session open, search, any required protocol wait, and close. At the start of each session-open attempt, compute `share = (remaining − W) / untried`. `W` is the 8 s wait if T003 proves it is required and 0 otherwise. If `remaining ≤ W`, stop trying and report `failed`. An attempt is the session open only. Once a session opens there is no failover: the wait (if any), the search and the close run on that server within the overall remaining deadline. A search failure or timeout after open produces `failed` with “BSPlayer is not answering”. Close gets no reserved budget (D1). |
| Session | Token is a local in each search call, never a field; the adapter is stateless apart from injected services (D2). Open → search on first successful session → best-effort close awaited within remaining budget; close exceptions are logged scrubbed and swallowed; close never fails a search that already returned results; skip close if no budget remains (D1). |
| Destination predicate | One shared refusal gate, which extends the existing host-refusal service rather than reimplementing it, is applied to every SOAP request and every download URL before contact (D3). A URL is allowed only if all of these hold: it is absolute; its scheme is `https`, or `http` when `AllowPlainHttp` is true; it has no userinfo; its host is not an IP literal; its host, normalized with `Uri.IdnHost`, compared case-insensitively and with one trailing dot stripped, exactly equals the host of a `Servers` entry or a `DownloadAllowedHosts` entry (no suffix or wildcard match); and its effective port equals that entry's effective port. Relative and protocol-relative URLs are refused. Any 3xx counts as a failed attempt and is never followed. Allowed hosts are always the union of `Servers` and `DownloadAllowedHosts`; an empty `DownloadAllowedHosts` is valid (D15). DNS-rebinding pinning is out of scope (Q2; recon line 66; H3(e) dismissed). |
| 8 s wait | Add only if recorded evidence proves it is required; charge to the same deadline; DEV-552 owns picker-first UX |
| All-down | Provider state `failed` with reason text “BSPlayer is not answering”; sibling providers unaffected |
| Redirects | Disabled; any 3xx is a failed attempt and is never followed (D3) |
| XML | Build requests with `XElement`/`XmlWriter`, never string concatenation. Read each response through a counting stream capped at `MaxResponseBytes`, and abort the attempt as soon as the cap is exceeded, before parsing (check `Content-Length` first when present). Never truncate and parse. Parse with `XmlReader`: `DtdProcessing.Prohibit`, `XmlResolver = null`, `MaxCharactersFromEntities = 0`, `MaxCharactersInDocument` derived from the configured bound. Malformed XML or a bad encoding fails the attempt (D5). |
| Download | Configured `MaxDownloadBytes` with download-cap precedence against the global `Subtitles:MaxSubtitleDownloadBytes` (cited at T001) and never exceeds that global cap; existing SRT validation before save; trust no further than payload checks (D11) |
| Secrets | Keyless; scrub anonymous session tokens from logs, telemetry, and fixtures |
| Time | Deadline is `new CancellationTokenSource(timeout, timeProvider)`; waits are `Task.Delay(delay, timeProvider, token)`. `HttpClient.Timeout` stays infinite, per the existing registration (recon line 14). Timing tests (D1, D2, D4, all-servers-down) run the real adapter over a stub `HttpMessageHandler`; a hanging server is a task that never completes until its token is cancelled; the test owns `FakeTimeProvider.Advance`; no wall-clock assertions. WireMock recorded fake covers the wire shape only: contract suite, hash, XML and refusal behaviour (D8). |
| Cancellation | Create the provider deadline source with `TimeProvider`, and create each attempt source linked to it and to the caller token. On `OperationCanceledException`: if the caller token fired, propagate cancellation as the DEV-551 contract specifies (cited at T001) and never map it to `failed`. If the provider deadline fired, report `failed` with “BSPlayer is not answering”. If only the attempt fired, move to the next server (D4). |
| Hash | Assert outgoing hash for a deterministic test-generated known file against recording; recompute from regenerable input in tests; no media or binary file committed (D10). If T003 proves a hash difference, add a separately named, functionally named routine in `MovieHash.cs`; the BSPlayer adapter calls it; the existing routine stays byte-identical; existing OpenSubtitles hash tests stay unchanged and green; add an OpenSubtitles golden-value test only if none exists (cited at T001) (D6). |
| New destination / redirect in recording | Stop for explicit ruling before relying on it |
| Capture failure | If no BSPlayer server answers during evidence capture: STOP blocked with attempt evidence; never synthesize fixtures or wire behaviour from HolyArrs notes |
| Inconclusive capture | If the T002 capture for the generated file returns no candidates or no download link, the operator makes a second capture: a search for a home-library movie that BSPlayer knows, using the hash and size LamuFlix computes on the home cluster. Commit only the scrubbed SOAP bodies and that hash and size, never media bytes. Download-host shape and hash acceptance (a non-empty match for the LamuFlix-computed hash) come from that capture. If it also yields no download link, STOP blocked for a ruling. No allowlist is assumed (D7; Q2). |

## Options binding

| Concern | Contract |
| --- | --- |
| Section | `Subtitles:BsPlayer` (ticket-named; const on the adapter class; doubles as the options name — D12) |
| Shape | No new options record. Bind a named instance of the existing `SubtitleProviderOptions`, as DEV-551 lands it, from `Subtitles:BsPlayer` at the existing subtitle-provider registration site cited at T001, without `ValidateOnStart` (D11). |
| Fields | Reuse `RequestTimeout`, `DownloadAllowedHosts`, `MaxDownloadBytes`, and DEV-551's `Enabled` and `AllowPlainHttp`. Add only `Servers` (`string[]` of absolute URLs) and `MaxResponseBytes` (`long`); both stay inert for the OpenSubtitles instance. The BSPlayer adapter ignores `ApiKey`, `BaseUrl`, `UserAgent` and `DownloadLinkHost`. |
| Defaults | `Enabled=false` in repository appsettings; home settings are untracked (Principle VII); T004 edits no home file (D13). All other listed fields from configuration with no guessed code defaults. |
| OpenSubtitles | Validation of the `Subtitles:OpenSubtitles` instance stays byte-identical; existing validator tests stay unchanged (D11). |
| Download cap | Follows the precedence the OpenSubtitles adapter already applies against the global `Subtitles:MaxSubtitleDownloadBytes` (cited at T001) and never exceeds that global cap (D11). |
| STOP | If DEV-551's landed shape makes a named instance impossible, STOP for a ruling (D11). |
| Core naming | Principle VIII — functionality, not vendor; vendor on Infrastructure adapter + config key |
| Validation | Provider-local under Principle VII acquisition exception; no host abort; DEV-551 search 409 only when no enabled provider is usable; read-only download precedence preserved; existing-track and languages routes remain available |
| Validator rules (D15) | When `Enabled=false`, nothing is validated, no calls are made, and health reports disabled. When `Enabled=true`, the provider is unusable (zero calls, host keeps running) if `Servers` is empty, or any entry is not absolute, has a scheme other than http or https, uses `http` while `AllowPlainHttp=false`, has an IP-literal host or carries userinfo, or `RequestTimeout ≤ 0`, `MaxResponseBytes ≤ 0` or `MaxDownloadBytes ≤ 0`, or any `DownloadAllowedHosts` entry is blank or contains `*`. An empty `DownloadAllowedHosts` is valid: downloads are then limited to `Servers` hosts by D3. `https` entries are valid when `AllowPlainHttp=true`. Tests use Theory + MemberData (Principle IX). |

## Recorded evidence prerequisites (before reliance)

Establish each from a scrubbed recorded exchange (Wanted 5; Done when; Q2). Evidence note location: `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-555\receipts\` (PRIVATE_TICKET_DIR/receipts/). After the evidence note, Keel reruns the Constitution Check against it before T004 (wait, host shape, or hash may change the plan — Ledger L5).

1. Session open / search / close sequence
2. Whether an 8-second wait is protocol-required
3. Known-file hash match (outgoing LamuFlix hash vs recording) for a regenerable deterministic test-generated file — not media; no binary committed (D10)
4. Download-link host shape (allowed hosts are always the union of `Servers` and `DownloadAllowedHosts`; empty `DownloadAllowedHosts` is valid — D3/D15)
5. Inconclusive first capture → second capture branch (D7): home-library movie BSPlayer knows; commit only scrubbed SOAP bodies plus that hash and size; hash acceptance = a non-empty match for the LamuFlix-computed hash; if also inconclusive, STOP blocked for a ruling

HolyArrs notes (`bsplayer.go` / `sites.go`) are orientation only — AGPL; copy nothing. Capture failure → STOP blocked with attempt evidence; never synthesize from HolyArrs.

## Fixtures

Sanitized recorded SOAP responses under the DEV-549 fake layout. Preferred path `tests/fakes/bsplayer/` is a **T001-cited pickup fact** (not verified in recon-DEV-555). One fake framework. Session tokens redacted before store. The known file for the hash proof is generated in the test from deterministic bytes; no media or binary file is committed (D10). Committed fixtures are only scrubbed SOAP bodies and mappings (plus hash and size when D7 second capture applies). DEV-551 shared contract suite runs against this fake through the real typed client path. WireMock covers the wire shape only; timing tests use a stub `HttpMessageHandler` (D8).

## Health / telemetry

Config-state-only registration; no live BSPlayer probe; no session tokens in health data.

**Identifiers (D12)**: Adapter `BsPlayerSubtitleProvider` (VIII adapter-class rule). Section key `"Subtitles:BsPlayer"` is a const on that adapter class and doubles as the options name (vendor-mandated-key rule). Options record: existing `SubtitleProviderOptions`. Validator: extend existing `SubtitleProviderOptionsValidator` (functional name). Typed client: `AddHttpClient<BsPlayerSubtitleProvider>`, keyed by the adapter type. Health registration name and telemetry names: functional, following the DEV-551 per-provider mechanism. Outcome reason text: “BSPlayer is not answering” (ticket Wanted 4). Tests: `BsPlayerSubtitleProvider*Tests` and the DEV-549 fixture folder. If the DEV-551 provider identifier requires a new vendor-named `TelemetryConstants` entry, that is a VIII departure: STOP and route it as `blocked: structural` (PRODUCT §5 b). Do not add the entry.

**Telemetry and logs (D9)**: Reuse the existing and DEV-551 provider spans, metrics and attribute keys (cited at T001); add no new attribute keys. Server host and attempt number may appear in structured logs, never as telemetry tags. Logs carry the movie id, the attempt number and the failure category (Principle VI), and never the session token. Test: captured logs contain no token and do contain the movie id.
