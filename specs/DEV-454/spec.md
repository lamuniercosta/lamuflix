# Feature Specification: Play existing sidecar subtitles in the browser

**Feature Branch**: `feature/454-spec`
**Created**: 2026-11-09
**Status**: gate1: provisional
**Input**: Ticket DEV-454 (parent DEV-100); grill-closed `brief.md` (Q1–Q8) including Phase 2 step 6 adjudication decisions and Ledger S4 final Patron disposition; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-454`, `recon-DEV-454` (supplement lines 7–22 authoritative for reader/handler/exception facts), `adjudication-DEV-454-Keel`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite / propose only — Quill does not edit constitution). Dependencies: DEV-456, DEV-455. Ticket Description carries Scope / Dependencies / Pipeline only — no separate Technical Design or Acceptance Criteria sections in the task-script output (`recon-DEV-454:51-55`); Patron Q1–Q8 and Phase 2 step 6 elaborate the stated Scope.

## Owner question — Gate 1 (blocked: structural)

Ordinary Gate 1 stays **closed**. Patron ruled **blocked: structural** under charter 2.3(b) for Ledger S4. Quill does **not** modify the constitution. The following proposed narrow amendment text MUST appear on the spec PR; the owner checkbox below remains **unticked** until the owner explicitly **approves** the exact amendment text (or supplies owner-amended text that is incorporated consistently across artifacts). **Only** that explicit approval **plus** verified merge unblocks pickup. Reject, unanswered, or any non-approval disposition keeps the item blocked even if a disposition record and merge receipt both exist — do not count any answer as approval. A merge without explicit approval is not blanket checkbox approval. PLAN FROZEN is withheld while this blocker remains.

### Proposed constitution amendment v1.13.0 (artifacts only — not applied)

**Principle II — exact expected-failure sentence addition** (append after the current expected-domain-failures sentence that names `NotFoundException`, `ValidationException`, `InvalidTransitionException`, and `FeatureDisabledException`):

> Expected domain failures also include `LibraryFileAccessDeniedException` for library file policy and containment denial.

**Principle V — exact table-row addition** (insert into the single `IExceptionHandler` exception-to-status table; preserve every existing row and the Unhandled → 500 fallback):

| Exception | Status |
|---|---|
| `LibraryFileAccessDeniedException` | 403 |

All other exception mappings, failure hygiene, and the rule that endpoints MUST NOT map exceptions independently remain unchanged. Reject route-local subtitle 403 mapping. After **explicit owner approval** of the exact amendment text (or consistently incorporated owner-amended text) **and** verified merge — not merely any disposition — Phase B may add the corresponding arm only in the single existing `GlobalExceptionHandler`; until then the constitutional mapping remains unresolved and Gate 1 stays closed. Reject or unanswered checkbox remains blocked.

### Required owner checkbox (unticked)

- [ ] Approve the DEV-454 narrow constitution v1.13.0 amendment to Principles II and V: recognize existing LibraryFileAccessDeniedException as an expected failure and map it to sanitized 403 Forbidden in the single GlobalExceptionHandler, preserving all other exception mappings and the requirement that endpoints do not map exceptions independently.

## Clarifications

### Session 2026-11-09

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Behaviour for sidecar recognition and labels, UTF-8/Windows-1252 decoding and WebVTT conversion, list/content API contract, browser selection, authorization/file safety, dependency preconditions, edit boundaries, edge cases, closing bar, and task/test order are frozen in brief Q1–Q8. Analyze-round-1 C1/C2 and Phase 2 step 6 Patron rulings add constitution II thin-transport + sealed subtitle queries/handlers, bounded CONTEXT.md four-term append, existing-port enumeration extension, LibraryOptions bounds, token derivation, handler/adapter ownership, viewer concurrency/list-failure UX, and literal cue-boundary policy. Residual open item: Ledger S4 constitution II/V amendment owner checkbox (blocked: structural). No `[NEEDS CLARIFICATION]` markers outside that owner checkbox.

## Scope

**In scope** (`brief.md` frozen scope, Approach, and Phase 2 step 6; Q1–Q8; ticket Scope):

1. **Existing same-directory SRT sidecars**: Recognize sidecar `.srt` files that share the resolved primary video's directory and case-insensitive exact primary-video-stem matching for `<stem>.srt`, `<stem>.<suffix>.srt`, and `<stem>-<suffix>.srt`. Stem comes from the resolved primary video, not the movie title. No fuzzy matching, recursion, acquisition, external search, extra language inference, or compound language/flag grammar.
2. **Frozen recognition table (Q1, Q7)**:

   | Filename pattern/example | language | label | isDefault |
   | --- | --- | --- | --- |
   | `<stem>.srt` | und | Subtitles | false |
   | `<stem>.en.srt` or `<stem>-en.srt` | en | English | false |
   | `<stem>.pob.srt` or `<stem>-pob.srt` | pt-BR | Portuguese (Brazil) | false |
   | `<stem>.default.srt` or `<stem>-default.srt` | und | Default | true |
   | `<stem>.<unknown>.srt` or `<stem>-<unknown>.srt` | und | filename basename | false |

   Only the exact case-insensitive `default` suffix sets `isDefault: true`. Unknown suffixes remain selectable. Multiple default-marker tracks remain available; none auto-enables. Sort default-marker tracks first, then language and label ordinal case-insensitively, then filename ordinal server-side as tie-break. Apply duplicate label counters `(2)`, `(3)` after sorting. Expose basenames for unknown labels, never directory paths.
3. **Authorized existing-port enumeration and safe open (Phase 2 step 6; Ledger S1; Sentry F3)**: Extend existing `ILibraryFileReader` / `LibraryFileReader` with same-directory regular-file enumeration using the existing Infrastructure filesystem abstraction only — no new port, layer, project, or package. Handler owns reauthorization, enumeration, and token matching policy sequence. Adapter owns open-time folder/root containment, path-chain link/reparse rejection, opened-handle regular-file checks, and safe open. Existing check-then-open is **not** proven race-safe (`recon-DEV-454` supplement:8–11; ADR 0037 does not prove TOCTOU immunity). T001 MUST verify platform capability for the promised safe-open/fail-closed implementation and STOP `needs recon` if unavailable. Implementation MUST include real-FS adversarial replacement tests that return no unauthorized bytes. Bounded accuracy update to existing `docs/adr/0037-library-file-reader-port.md` is authorized; no new ADR number.
4. **Configured conversion bounds (Phase 2 step 6; Sentry F1)**: Add four positive `[Range]`-validated properties on existing `LibraryOptions` with defaults `MaxSubtitleInputBytes = 8388608`, `MaxSubtitleCues = 100000`, `MaxSubtitleLineCharacters = 16384`, `MaxSubtitleOutputBytes = 33554432`. Bind with existing options validation/startup wiring — no new options record, magic-string reads, or hardcoded enforcement thresholds. Count payload line characters after decoding; count output as UTF-8 bytes; enforce during bounded reading/parsing/emission, not only after unbounded allocation. Exceeding limits yields sanitized `ValidationException` / 422 (not 413). Tests cover exactly-at, first-over, and invalid configuration.
5. **Opaque token derivation (Phase 2 step 6; Sentry F2; Ledger S2)**: Derive `trackId` as unkeyed SHA-256 over invariant-decimal movie id, NUL separator, then UTF-8 exact ordinal filename, encoded unpadded base64url. No HMAC, secret, or reversible token-to-path decode. Before open, recompute fresh movie-scoped eligible-set tokens and match against that allowlist only. Hash collision fails closed without choosing a file. Tampered / unknown / vanished / other-movie tokens yield sanitized 404. Stable identity is unchanged-set only.
6. **SRT decoding and WebVTT conversion (Q2, Q7; Sentry F5)**: Reject UTF-16/UTF-32 BOMs with 422. Otherwise strict UTF-8 first (optional UTF-8 BOM stripped) using exception fallback; fall back to Windows-1252 only when strict UTF-8 fails. Codepage provider registration only if T001 proves existing dependencies/runtime support and need; registration must be idempotent without mutable static initialization in mutated code — no unconditional shared-framework availability claim (`recon-DEV-454:29`). No statistical guessing; do not report replacement-character decoding as success. Accepted limitation: damaged UTF-8 may decode through the mandated Windows-1252 fallback. Normalize CRLF/CR to LF. Emit `WEBVTT` plus a blank line; convert comma milliseconds to dot milliseconds; strip numeric SRT sequence indices; preserve cue order/timing; escape `&`, `<`, and `>` so cue text renders literally (no raw HTML injection). Valid overlapping cues allowed. Preserve valid literal words including NOTE/STYLE/REGION-looking text inside otherwise valid cues — do **not** blanket-prefix or rewrite those words. Add parser-boundary fixtures with arrows and NOTE/STYLE/REGION-looking text. Reject with sanitized 422 only payload constructs that actually cannot be represented as structurally valid literal WebVTT (for example payload lines containing `-->` or blank-line-separated block injections that would terminate the cue and forge a header/block). Structurally invalid, empty/cue-less, unparseable, negative, or reversed intervals produce sanitized 422 with no paths or cue content. Failure of one track does not block listing or other tracks. No other encoding or subtitle format is included.
7. **Deliberate public API contract (Q3, Q7; charter 2.3 item 4; Sentry F8)**:

   | Request | Successful response |
   | --- | --- |
   | `GET /api/movies/{id:int}/subtitles` | 200 JSON array of `SubtitleTrackDto`; `[]` for no sidecars |
   | `GET /api/movies/{id:int}/subtitles/{trackId}/content` | 200 converted UTF-8 WebVTT |

   `SubtitleTrackDto` fields: `id` (opaque string), `label` (string), `language` (BCP47 string or `und`), `isDefault` (boolean). Tokens are distinct per eligible filename, stable across repeated listings of an unchanged movie-sidecar set and for list/content round trip, never ordinal indexes; no durable identity across renames. Frontend builds the content URL from movie id and opaque token; no client filename/path input. Both endpoints use `Cache-Control: private, no-store` on **success and error** (403/404/422/500). Content uses `Content-Type: text/vtt; charset=utf-8` and `X-Content-Type-Options: nosniff` on success; apply nosniff where applicable on errors. Content Range requests receive full 200 content, no `Accept-Ranges`, no 206. Conversion is on request with no disk writes. Frozen statuses unchanged: 403 unauthorized library access; 404 unknown movie / missing primary media / vanished or unknown track; 422 malformed, unsupported-BOM, or over-bound SRT on the content endpoint; 500 unexpected failure with generic sanitized response. Malformed/BOM/over-bound content uses existing `ValidationException` → 422; missing movie/media/track uses existing `NotFoundException` → 404 (`recon-DEV-454` supplement:14–16). Mapping existing `LibraryFileAccessDeniedException` → 403 is **constitutionally blocked** pending **explicit owner approval** of the exact amendment text (or consistently incorporated owner-amended text) **plus** verified merge — reject/unanswered/non-approval disposition keeps the blocker even if a disposition and merge receipt both exist; until then do not implement a new global arm and do not use route-local mapping or `FeatureDisabledException` misuse. Every error response (403/404/422/500) MUST be RFC 7807 `ProblemDetails` through the single existing `IExceptionHandler` / `AddProblemDetails()` path **and MUST include `traceId`** (constitution V). Recon fact that `GlobalExceptionHandler` does not explicitly set `traceId` (`recon-DEV-454` supplement:14–16) qualifies implementation wiring only — it does **not** remove the normative `traceId` requirement. Assert `traceId` via the configured ProblemDetails writer; if T001 pickup proves writer configuration is missing, add a bounded necessary writer-configuration task — never claim `traceId` already proven. Do not claim an existing 403 arm for `LibraryFileAccessDeniedException`. Invalid movie route input follows framework routing. Do not change the movie detail DTO. Contract chain C# DTO/endpoint metadata → `web/src/api/openapi.json` → generated `web/src/api/types.ts`; never hand-edit generated artifacts; web consumes generated types only.
8. **Handler / transport ownership (Phase 2 step 6; Compass F1; Ledger S6)**: Thin Api endpoints dispatch sealed subtitle queries/handlers (analyze-round-1 C1; constitution II). Content `IQueryHandler` (T007) owns reauthorization, fresh eligible enumeration, token matching, cancellation, stream lifetime/disposal, decode, and conversion. Thin content route (T008) owns HTTP dispatch, metadata, and headers only. Core list result is a sealed record with an immutable collection of sealed track records; content result is a sealed immutable document record/string payload — no live stream crossing Core→Api; no Core→Api project reference. Api maps Core results to `SubtitleTrackDto`. Queries carry `MovieId`. Processing/error diagnostics carry movie id and sanitized failure categories via `TelemetryConstants` keys; reuse decorator tracing/logging; no new custom span/metric names. BCL encoding/crypto only inside existing Core allowlisted references — no helper layer or new Core references. Serialize shared `HandlerRegistration.cs` edits across T003/T007.
9. **Browser interaction (Q4, Q7; Sentry F4/F6/F7; Compass F3)**: Integrate one selector into the dependency-verified DEV-455 browser viewer. Offer Off and server-listed tracks; hide selector for a successful empty list. Start Off even when a default marker exists. Only the selected track has `TextTrack.mode = "showing"`; all others `"disabled"`; Off disables all. Selection is viewer-session-local, resets when the movie changes, and does not use `localStorage` or browser-language detection. Labels render as text-only (`textContent` / JSX text children — never raw HTML / `dangerouslySetInnerHTML`); hostile-basename coverage required. Assert served label ordering (default-first, labels, duplicate counters). Only the current movie/selection generation may apply list or content success **or** failure; cancellation alone without stale-result suppression is insufficient. Off, switching, movie change, and unmount invalidate pending selection work and revoke owned object URLs where created. On track-load failure: keep video playing, switch subtitles Off, display `Subtitles unavailable`; reselecting a track retries. On **list** failure: keep video playing, keep subtitles Off, show `Subtitles unavailable` with explicit Retry — never masquerade as successful empty list. Labels, sorting, initial choice, and error wording are assumed taste rulings.
10. **Security and dependency preconditions (Q5, Q6)**: Both routes reauthorize the movie's library folder before enumeration/open. Content re-resolves opaque tokens against currently eligible same-directory sidecars for that movie; tokens are identifiers, not authority. Reject arbitrary paths/traversal and non-regular files; prevent symlink escape beyond the authorized directory; use cancellation and stream disposal. Discovery derives from the resolved primary video — not movie title, database persistence, or external search. No call to external player, `Process.Start`, `Features:LocalPlay` change, or secrets. DEV-455 viewer integration and DEV-456 safe-reader integration are planned preconditions requiring Phase B recon receipts — not verified delivery facts at grill recon base. If seams are absent/drifted, platform safe-open capability is unavailable, or Windows-1252 cannot be implemented with existing dependencies/BCL, stop with `needs recon` (a package additionally requires a new cited Patron ruling). Do not invent a replacement viewer, unsafe reader, or architectural layer.
11. **Bounded edits (Q6; C1/C2; Phase 2 step 6)**: Additive subtitle source and tests within existing projects following verified architecture, including sealed-record subtitle queries, sealed `IQueryHandler`s, `AddHandler` registration, and thin Api transport (analyze-round-1 C1); extend `src/LamuFlix.Core/Ports/ILibraryFileReader.cs` and `src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs` plus corresponding existing reader tests; add four bounds on `src/LamuFlix.Core/Options/LibraryOptions.cs` plus existing options/binding/configuration files and tests; bounded accuracy update to `docs/adr/0037-library-file-reader-port.md`; register endpoints in `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs` and handlers in `src/LamuFlix.Api/HandlerRegistration.cs`; after owner-approved constitution amendment only, bounded `LibraryFileAccessDeniedException` → 403 arm in `src/LamuFlix.Api/ExceptionHandling/GlobalExceptionHandler.cs`; integrate UI in `web/src/features/movie-details` and/or the cohesive DEV-455 viewer module verified by Phase B recon; add subtitle integration tests alongside `tests/LamuFlix.IntegrationTests/StreamEndpointTests.cs` and tests at the verified viewer seam; regenerate OpenAPI/types through the contract chain; append only Sidecar subtitle, Subtitle track, Default marker, and WebVTT to existing `CONTEXT.md` in the introducing PR (analyze-round-1 C2). Keep `StreamEndpoints.cs` and local playback files untouched absent a new cited Patron ruling. No deletions or wholesale rewrites. No new NuGet/npm dependency or swap; no new port/project/top-level folder/architectural layer; no schema or persistence changes; no new ADR number.
12. **Acceptance coverage and ordering (Q8)**: Pickup/recon including platform safe-open + encoding capability STOP checks → deterministic discovery/decoder/converter + sealed queries/handlers + DTO/thin routes using the extended safe reader and real filesystem + adversarial tests → CONTEXT.md four-term append → regenerate OpenAPI/TS → integrate selector at verified viewer seam → integration tests and real-browser acceptance → applicable backend verification and web gates with exact status reporting. Unit tests alone do not prove runtime acceptance.

**Out of scope**:

- External subtitle acquisition, recursive search, fuzzy matching, compound language/flag grammar, or language mappings beyond Q1.
- Other subtitle formats or encodings beyond UTF-8 and Windows-1252 SRT → WebVTT.
- Changing the movie detail DTO; content ranges / 206 for subtitle content; durable track identity across renames; disk-written converted files.
- Auto-enabling subtitles from a default marker; preference persistence; browser-language auto-selection.
- Touching `Features:LocalPlay`, `PlayRequestBoundary`, `PlaybackEndpoints`, `ProcessMediaPlayerLauncher`, `Process.Start`, or secrets.
- Editing `StreamEndpoints.cs` or local playback files without a new cited Patron ruling.
- New NuGet/npm packages or swaps; new project/top-level folder/architectural layer; schema/persistence changes; new Core port; new ADR number.
- Inventing a standalone player or unsafe reader when DEV-455/DEV-456 seams are missing.
- New ADR or global CONTEXT.md rewrite (Q8). Bounded four-term CONTEXT.md append is in scope (C2), not a global rewrite. Bounded existing ADR 0037 accuracy update is in scope; inventing another ADR is not.
- Claiming DEV-455/DEV-456 already delivered at recon base; silent package addition for encoding; claiming existing reader already provides enumeration or TOCTOU-proof open; claiming shared-framework codepage availability without T001 proof.
- Treating recon stream endpoint surfaces as a constitution II handler exemption (C1).
- Route-local exception-to-status mapping; misusing `FeatureDisabledException` for filesystem containment denial; implementing the global `LibraryFileAccessDeniedException` → 403 arm before explicit owner amendment approval and verified merge.
- Closing owner checkboxes via this draft; claiming PLAN FROZEN or Gate 1 clearance while Ledger S4 remains unresolved.
- Blanket rejection or rewriting of NOTE/STYLE/REGION words inside otherwise valid cue payloads.
- New custom telemetry span/metric names outside `TelemetryConstants`.

**Precedence**: Ticket Scope is already approved authority (`PRODUCT.md` §3). Brief Q1–Q8, Phase 2 step 6, Ledger S4 final Patron disposition, and `CONCLUSIONS.md` govern boundaries. Gate 1 remains closed for the unticked constitution checkbox. Quill does not invent PLAN FROZEN.

## Domain vocabulary *(mandatory — brief Q8 terms; analyze-round-1 C2 appends these four to existing CONTEXT.md in the introducing PR; no global rewrite; no new ADR)*

**Sidecar subtitle**
An existing SRT file in the resolved primary video's directory whose filename matches the frozen recognition policy.
_Avoid_: downloaded subtitle, embedded stream, recursive library search

**Subtitle track**
One selectable eligible sidecar, identified through a server-issued opaque token.
_Avoid_: ordinal index as identity, client-supplied path

**Default marker**
An exact `default` filename suffix; a sorting hint (`isDefault: true`), not permission to auto-enable subtitles.
_Avoid_: automatic On, preferred language

**WebVTT**
The UTF-8 browser subtitle representation produced from eligible SRT content.
_Avoid_: serving raw SRT to the browser as the delivery format

Consumed vocabulary (already in use; not redefined): **Movie**, primary video, `ILibraryFileReader` / DEV-456 safe reader (AuthorizeFolder / OpenRead today; same-directory enumeration extension authorized for this ticket), DEV-455 browser viewer, Features:LocalPlay, `LibraryOptions`, `LibraryFileAccessDeniedException`, `TelemetryConstants`.

## What the User Will See *(mandatory)*

- **Where**: Dependency-verified browser movie viewer (DEV-455 seam under movie details / cohesive playback module — exact surface confirmed at Phase B recon).
- **What the user sees or does**: When eligible sidecar tracks exist, a subtitle selector offers Off plus server-listed track labels as plain text. Playback starts with Off. Choosing a track shows that track's cues; choosing Off clears them. Successful empty sidecar sets hide the selector. On content-load failure or list-load failure the video continues, subtitles return to / remain Off, and the user sees `Subtitles unavailable` with Retry (list failure) or retry-by-reselect (content failure) — list failure never looks like a successful empty list.
- **What it needs to show up**: Default Compose stack with Api + web nginx; DEV-455 browser viewer and DEV-456 stream/safe-reader delivered; at least one movie whose primary video directory contains matching `.srt` sidecars (fixtures or library media — not a claimed tracked corpus in this draft).

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001** *(planned criteria)*: **Given** Compose is up with a playable movie that has at least one eligible same-directory SRT sidecar, **When** the user opens browser playback for that movie, **Then** a subtitle selector is visible with Off plus the server-listed track label(s) in served order as plain text.
- **SMOKE-001** *(observed runtime acceptance)*: Delivery must execute real-browser Compose verification of visible cues / selector presence and record status. Missing browser/sample/seam ⇒ **Could not run** (never PASS).

- **SMOKE-002** *(planned criteria)*: **Given** the selector lists tracks, **When** the user chooses a track then Off, **Then** cues appear for the selected track and clear on Off while video continues.
- **SMOKE-002** *(observed runtime acceptance)*: Same Compose/real-browser bar; Could not run ≠ PASS.

- **SMOKE-003** *(planned criteria)*: **Given** a deliberately unloadable/malformed selected track (or simulated content failure), **When** the track fails to load, **Then** video continues, subtitles return to Off, and `Subtitles unavailable` is shown; reselecting retries.
- **SMOKE-003** *(observed runtime acceptance)*: Recorded on the real-browser path; unit/jsdom success does not substitute.

- **SMOKE-004** *(planned criteria)*: **Given** a simulated subtitle list failure, **When** the list request fails, **Then** video continues, subtitles remain Off, `Subtitles unavailable` with Retry is shown, and the UI does not treat the failure as a successful empty list.
- **SMOKE-004** *(observed runtime acceptance)*: Same Compose/real-browser bar; Could not run ≠ PASS.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - List eligible sidecar subtitle tracks (Priority: P1)

An authorized caller lists subtitle tracks for a movie. A sealed list subtitle query/handler reauthorizes the library folder, resolves the primary video, discovers eligible same-directory SRT sidecars via the extended `ILibraryFileReader` enumeration surface using the frozen recognition table, sorts and disambiguates labels, derives opaque movie-scoped SHA-256/base64url tokens, and returns `SubtitleTrackDto` entries (or `[]`) through a thin Api transport. Unrelated files and recursive matches are excluded. Tokens remain stable across repeated listings of an unchanged set. One bad content decode must not prevent listing or serving other tracks.

**Why this priority**: Ticket recognition + choose-a-track foundation; Q1/Q3/Q5/Q7; Phase 2 step 6.

**Independent Test**: Real-filesystem discovery through the extended dependency safe reader covering the Q1 matrix (including `-pob`, uppercase, unknown suffixes, multiple defaults, unrelated files, no recursion, deterministic duplicate labels), token stability, and 403/404/`[]`/no-store behaviours — without requiring the browser selector.

**Acceptance Scenarios**:

1. **Given** a movie whose primary video directory contains eligible stem-matched SRT sidecars (including `.pob` / `-pob` and uppercase variants), **When** `GET /api/movies/{id}/subtitles` is called by an authorized caller (including repeated listings without set changes), **Then** the response is 200 JSON of `SubtitleTrackDto` entries with opaque ids stable across those listings, frozen labels/languages/`isDefault`, default-marker-first ordering, and duplicate counters after sort.
2. **Given** no eligible sidecars, **When** the list endpoint is called, **Then** the response is 200 `[]` and no selector-driving payload invents tracks.
3. **Given** unrelated same-directory files or nested subdirectory SRT files, **When** listing runs, **Then** those files are excluded.
4. **Given** unauthorized library access or unknown movie / missing primary media, **When** listing is attempted, **Then** the response is 403 or 404 respectively as sanitized RFC 7807 `ProblemDetails` including `traceId`, with `Cache-Control: private, no-store` and no path/cue leak (403 path gated on explicit owner approval of exact amendment text plus verified merge for `LibraryFileAccessDeniedException`).
5. **Given** an unexpected failure on the list route, **When** a generic sanitized 500 is returned, **Then** the response is RFC 7807 `ProblemDetails` including `traceId`, with `Cache-Control: private, no-store`, nosniff where applicable, and no path/cue leak.

---

### User Story 2 - Serve converted WebVTT for a track (Priority: P1)

An authorized caller fetches subtitle content by opaque `trackId`. The sealed content subtitle query/handler (not the thin route) reauthorizes, freshly enumerates eligible sidecars, recomputes tokens, fail-closes on collision, matches the requested token against that allowlist only, opens via the adapter's safe open, decodes UTF-8 then Windows-1252 per policy within configured bounds, converts to WebVTT, and returns a sealed immutable document result that Api maps to UTF-8 text/vtt with nosniff and no-store. Malformed/unsupported-BOM/over-bound inputs return sanitized 422 `ProblemDetails`. Range requests still return full 200 without Accept-Ranges/206. Tokens are not authority; traversal/symlink/non-regular/adversarial-replacement paths fail closed. One bad track must not block successful content for other eligible tracks.

**Why this priority**: Ticket decode/serve WebVTT; Q2/Q3/Q5/Q7; Phase 2 step 6.

**Independent Test**: Real-filesystem content tests for UTF-8 accents with/without BOM, Windows-1252 accented fixtures, newline variants, empty/malformed/UTF-16/32 rejection, bound exactly-at/over, valid overlap and invalid intervals, literal cue-boundary fixtures (arrows / NOTE/STYLE/REGION-looking words preserved when valid), exact WebVTT timing and literal markup escaping, token/movie binding, tampered/other-movie/vanished 404, collision fail-closed, 422 sanitization, Range→full 200, one-bad-track isolation from listing, adversarial open replacement, and successful other-track content while one track remains bad — without requiring UI.

**Acceptance Scenarios**:

1. **Given** an eligible UTF-8 (optional BOM) or Windows-1252 SRT track listed for a movie, **When** content is requested with that opaque id, **Then** the response is 200 `text/vtt; charset=utf-8` with nosniff, no-store, `WEBVTT` grammar per Q7, and cue text escaped literally.
2. **Given** UTF-16/UTF-32 BOM, empty/cue-less, unparseable, negative/reversed-interval, over-bound, or actually unrepresentable cue-structure SRT, **When** content is requested, **Then** the response is sanitized 422 `ProblemDetails` including `traceId`, with `Cache-Control: private, no-store`, and no filesystem paths or cue contents.
3. **Given** a vanished, unknown, tampered, or other-movie token, **When** content is requested, **Then** the response is 404 `ProblemDetails` including `traceId`, with `Cache-Control: private, no-store`, after fresh allowlist re-resolution against current eligible sidecars.
4. **Given** a Range header on the content request, **When** content is returned successfully, **Then** the response is full 200 without `Accept-Ranges` or 206.
5. **Given** traversal, symlink escape, non-regular file, or adversarial file replacement between check and open, **When** open is attempted, **Then** the request fails closed without serving unauthorized bytes.
6. **Given** one bad eligible track and one good eligible track for the same movie, **When** listing and the good track's content are requested, **Then** listing remains available and the good track returns successful WebVTT.
7. **Given** cue text containing arrow-looking or NOTE/STYLE/REGION-looking words inside an otherwise valid cue, **When** conversion runs, **Then** valid literal words and valid overlaps are preserved (no blanket NOTE/STYLE/REGION rejection), and only actually unrepresentable structures yield sanitized 422 `ProblemDetails` including `traceId`.
8. **Given** an unexpected failure on the content route, **When** a generic sanitized 500 is returned, **Then** the response is RFC 7807 `ProblemDetails` including `traceId`, with `Cache-Control: private, no-store`, nosniff where applicable, and no path/cue leak.

---

### User Story 3 - Browser subtitle selector (Priority: P1)

In the dependency-verified browser viewer, the user chooses among Off and server-listed tracks. Selection starts Off; only one track shows at a time; movie change resets; successful empty lists hide the selector; list failure shows `Subtitles unavailable` + Retry without looking empty; content-load failure keeps video playing, returns to Off, shows `Subtitles unavailable`, and allows retry by reselecting. Frontend builds content URLs from movie id + opaque token using generated types only. Labels are text-only; served order is preserved; stale success/error from prior movie/selection generations are suppressed; owned URLs are cleaned up on Off/movie-change/unmount.

**Why this priority**: Ticket “let viewers choose a track”; Q4/Q7; Phase 2 step 6.

**Independent Test**: Viewer-seam Vitest/RTL (or equivalent at the verified module) for Off/initial, single showing track, switching/Off, movie-change reset, empty hide vs list-error Retry, hostile basename text-only, served ordering, generation-guard stale suppression, owned-URL cleanup, failure copy/retry without interrupted video — without claiming codec decoding proof.

**Acceptance Scenarios**:

1. **Given** a non-empty track list in the browser viewer, **When** the page loads, **Then** the selector shows Off plus listed labels in served order as plain text and selection starts Off even if a default-marker track exists.
2. **Given** the user selects a track, **When** content loads successfully, **Then** only that track is `showing` and others are `disabled`.
3. **Given** the user chooses Off or the movie changes, **When** state updates, **Then** all tracks are disabled (Off) and a new movie starts Off again with no `localStorage` persistence; pending work is invalidated and owned object URLs are revoked where created.
4. **Given** a successful empty list, **When** the viewer renders, **Then** the selector is hidden.
5. **Given** track content fails to load, **When** the failure surfaces, **Then** video continues, selection returns to Off, `Subtitles unavailable` is shown, and reselecting retries.
6. **Given** the subtitle list request fails, **When** the failure surfaces, **Then** video continues, subtitles remain Off, `Subtitles unavailable` with explicit Retry is shown, and the UI does not present a successful empty list.
7. **Given** rapid switch / movie change while list or content requests are in flight, **When** stale responses arrive, **Then** only the current movie/selection generation applies success or error.

---

### User Story 4 - Real-browser subtitle acceptance (Priority: P2)

Delivery records real-browser Compose evidence for visible cues, track switching, Off, list/content subtitle failure while video continues — or reports Could not run. Unit tests do not replace this receipt.

**Why this priority**: Brief Q8 runtime acceptance requirement.

**Independent Test**: Documented runtime receipt against the delivery SHA (or Could not run).

**Acceptance Scenarios**:

1. **Given** Compose with a playable movie and eligible sidecars plus delivered DEV-455/456 seams, **When** real-browser verification runs, **Then** visible cues, switching, Off, and failure-with-continued-video are recorded against the delivery SHA.
2. **Given** browser, sample, or dependency seam unavailable, **When** verification is attempted, **Then** the result is Could not run and runtime acceptance stays open (never PASS).

---

### Edge Cases

- Multiple `.default` / `-default` tracks: all selectable; none auto-enables; defaults sort first.
- Unknown suffix: selectable `und` track labelled with basename only (never directory path); hostile basenames render as text only.
- Duplicate labels after mapping: deterministic `(2)`, `(3)` counters after sort.
- Damaged UTF-8 that still fails strict decode: may surface via Windows-1252 fallback (accepted Q2 limitation); UTF-16/32 BOM never falls through that path.
- One malformed track among many: list remains available; other content fetches succeed.
- Stale/tampered/other-movie token: 404 on content after fresh allowlist re-resolution.
- Token hash collision in eligible set: fail closed without choosing a file.
- Over-bound input/cues/line/output: sanitized 422; exactly-at bounds succeed.
- Unauthorized folder: 403 on list and content before enumeration/open — mapping via single GlobalExceptionHandler only after approved constitution amendment.
- Content Range: full 200 only; no partial subtitle bodies.
- Check/open race / adversarial replacement: fail closed; no unauthorized bytes; platform capability proven at T001 or STOP.
- Missing DEV-455 viewer or DEV-456 safe reader at pickup: stop `needs recon` — no substitute player/reader.
- Windows-1252 / codepage provider unavailable within existing dependencies/BCL: stop for recon + cited Patron package ruling — no silent NuGet add; no mutable static init registration shortcut.
- Recon inspected-path limits do not authorize claiming global absence of prior subtitle code; Phase B recon checks drift before edits.
- NOTE/STYLE/REGION-looking words inside valid cues: preserved; not blanket-rejected.
- List request failure vs successful `[]`: distinct UI outcomes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST discover eligible sidecar `.srt` files only in the resolved primary video's directory using case-insensitive exact stem matching for `<stem>.srt`, `<stem>.<suffix>.srt`, and `<stem>-<suffix>.srt`, with stem from the resolved primary video — no fuzzy matching, recursion, acquisition, or external search (Q1; Q5). Discovery MUST use the authorized same-directory regular-file enumeration extension on existing `ILibraryFileReader` / `LibraryFileReader` (Phase 2 step 6; Ledger S1; `recon-DEV-454` supplement:7–11).
- **FR-002**: System MUST apply the frozen Q1/Q7 recognition table for `en`, `pob`→`pt-BR`, exact `default` marker, bare stem, and unknown suffixes (unknown remain selectable `und` with basename labels; only exact default sets `isDefault: true`; multiple defaults never auto-enable) (Q1; Q7).
- **FR-003**: System MUST sort default-marker tracks first, then language/label ordinal case-insensitively, then filename ordinal server-side; MUST apply duplicate label counters `(2)`, `(3)` after sorting; MUST NOT expose directory paths in labels (Q4; Q7; ASSUMPTIONS).
- **FR-004**: System MUST expose `GET /api/movies/{id:int}/subtitles` returning 200 JSON array of `SubtitleTrackDto { id, label, language, isDefault }` or `[]`, with opaque non-ordinal tokens stable across repeated listings of an unchanged movie-sidecar set and for list/content round trip, and `Cache-Control: private, no-store` on success and error (Q3; Q7; brief:65; Sentry F8).
- **FR-005**: System MUST expose `GET /api/movies/{id:int}/subtitles/{trackId}/content` returning 200 UTF-8 WebVTT with `Content-Type: text/vtt; charset=utf-8`, `X-Content-Type-Options: nosniff`, and `Cache-Control: private, no-store` on success and error; MUST re-resolve tokens against a freshly enumerated eligible allowlist before open; MUST NOT accept client filenames/paths or decode tokens to paths; MUST convert on request with no disk writes; MUST answer Range with full 200 and without `Accept-Ranges`/206 (Q3; Q5; Q7; Phase 2 step 6).
- **FR-006**: Decoding MUST reject UTF-16/UTF-32 BOMs with 422; otherwise strict UTF-8 first with exception fallback (optional UTF-8 BOM stripped) then Windows-1252 only on strict UTF-8 failure; codepage registration MUST be conditional on T001 capability proof and MUST be idempotent without mutable static initialization; MUST NOT statistically guess or treat replacement-character decoding as success (Q2; Q7; Ledger S5).
- **FR-007**: Valid conversion MUST emit `WEBVTT` + blank line, normalize newlines to LF, convert comma milliseconds to dots, strip numeric SRT indices, preserve cue order/timing, escape `&`/`<`/`>`, allow valid overlaps, preserve valid literal words including NOTE/STYLE/REGION-looking text inside cues, reject only actually unrepresentable/invalid/empty/cue-less/unparseable/negative/reversed/over-bound structures as sanitized 422 with no paths or cue content, and MUST NOT blanket-prefix or rewrite NOTE/STYLE/REGION words; one bad track MUST NOT block listing or successful content for other tracks (Q2; Q7; Sentry F5; brief:124).
- **FR-008**: List and content handlers MUST reauthorize the movie library folder before enumeration/open; adapter MUST enforce open-time containment/regular-file/reparse checks and fail closed under unproven check/open races after T001 platform capability proof; MUST reject traversal, non-regular files, and symlink escape; MUST use cancellation and stream disposal; MUST map frozen 403/404/422/500 per Q3 as RFC 7807 `ProblemDetails` through the single existing `IExceptionHandler` / `AddProblemDetails()` path (no parallel mapper; no route-local mapping; no `ex.Message` leak); **every error response MUST include `traceId`** (constitution V) asserted through the configured ProblemDetails writer — recon that the handler does not explicitly set `traceId` qualifies implementation facts only and does **not** remove this requirement; if pickup proves writer configuration missing, add a bounded necessary writer-configuration task and never claim already proven; `LibraryFileAccessDeniedException` → 403 global arm is authorized only after **explicit owner approval** of the exact constitution v1.13.0 II/V amendment text (or consistently incorporated owner-amended text) **plus** verified merge — reject/unanswered/non-approval disposition keeps the blocker even when a disposition and merge receipt both exist; MUST NOT invoke launcher/`Process.Start`/LocalPlay/secrets paths (Q3; Q5; constitution V; Ledger S4; Sentry F3/F8; `recon-DEV-454` supplement:8–16).
- **FR-009**: Contract generation MUST flow C# DTO/endpoint metadata → `web/src/api/openapi.json` → generated `web/src/api/types.ts` with no handwritten duplicates and no movie-detail DTO change; generated files MUST be regenerated, never hand-edited (Q3).
- **FR-010**: Browser viewer MUST offer one selector with Off plus server-listed tracks rendered as text-only labels; MUST hide the selector when the list is a successful empty result; MUST start Off even when default markers exist; MUST set only the selected track to `TextTrack.mode = "showing"` and others `"disabled"`; Off MUST disable all; selection MUST be session-local and reset on movie change without `localStorage` or browser-language auto-detect; MUST preserve served label ordering including default-first and duplicate counters (Q4; Q7; Compass F3; Sentry F4; ASSUMPTIONS).
- **FR-011**: On track-load failure the viewer MUST keep video playing, switch to Off, show `Subtitles unavailable`, and allow retry by reselecting a track; on list-load failure the viewer MUST keep video playing, keep Off, show `Subtitles unavailable` with explicit Retry, and MUST NOT present list failure as successful empty; only current movie/selection generation may apply list/content success or failure; Off/movie-change/unmount MUST invalidate pending work and revoke owned object URLs where created (Q4; Sentry F6/F7; ASSUMPTIONS).
- **FR-012**: Implementation MUST stay within existing projects and verified seams: additive sealed-record subtitle queries carrying `MovieId`, sealed `IQueryHandler`s returning sealed immutable Core results mapped to Api DTOs (no Core→Api reference; no escaped live streams), `AddHandler` registration, and thin Api transport (analyze-round-1 C1; Compass F1; Ledger S6; constitution II); existing-port enumeration extension + LibraryOptions bounds + ADR 0037 accuracy update as authorized; `ApiEndpoints.cs` registration; UI at `web/src/features/movie-details` and/or verified DEV-455 viewer module; tests alongside `StreamEndpointTests.cs` and viewer-seam tests; MUST NOT edit `StreamEndpoints.cs` or local playback files without a new cited Patron ruling; MUST NOT add packages, new ports, projects, layers, schema, or new ADR numbers (Q6; C1; Phase 2 step 6).
- **FR-013**: Phase B MUST recon-verify delivered DEV-455/DEV-456 seams, platform safe-open capability for the promised fail-closed open, existing runtime Windows-1252 / codepage capability, configured ProblemDetails writer emission of `traceId` on error responses (bounded writer-configuration task only if pickup proves missing — never claim already proven), and — only after **explicit owner approval** of exact amendment text (or consistently incorporated owner-amended text) **plus** verified merge — that approved constitutional text for LibraryFileAccessDeniedException→403 is present before implementing that mapping; STOP `needs recon` (and cited Patron ruling for any package) rather than invent substitutes or silent dependencies (Q5; Q6; Q8; Ledger S4/S5; Sentry F3; constitution V).
- **FR-014**: Acceptance MUST cover the Q8 matrices (filename/encoding/WebVTT/API/security/UI) including token stability on repeated unchanged-set listings, tampered/other-movie/vanished/collision cases, bound exactly-at/over + invalid config, cue-boundary fixtures, adversarial open tests, served ordering, list-error vs empty, generation-guard stale suppression, successful other-track content under one-bad-track isolation, and **explicit generic sanitized 500** cases on **both** subtitle routes asserting `private, no-store`, `traceId`, nosniff where applicable, and no path/cue leak (403/404/422 coverage alone does not prove all-errors headers), with real filesystem + safe-reader proofs for backend file behaviour; runtime browser acceptance MUST prove visible cues, switching, Off, and load failure with uninterrupted video — unit tests alone MUST NOT close runtime acceptance (Q8; brief:65,124; Phase 2 step 6).
- **FR-015**: Closing bar for code-review/ship-review: Critical, High, and Medium in-scope findings block; every Low requires explicit cited disposition; unnecessary abstractions are Follow-up; maximum two challenge/review rounds then halt to Bernstein/Patron; anything outside frozen Q1–Q7 scope is a follow-up issue, not a finding in this round (Q8). PLAN FROZEN remains withheld and Gate 1 closed while Ledger S4 lacks **explicit owner approval** of exact amendment text (or consistently incorporated owner-amended text) plus verified merge; reject/unanswered/non-approval disposition does not unblock.
- **FR-016**: Introducing implementation PR MUST append only Sidecar subtitle, Subtitle track, Default marker, and WebVTT to existing `CONTEXT.md` (term, definition, `_Avoid_:`); MUST NOT perform a global glossary rewrite or add an ADR for vocabulary (analyze-round-1 C2; brief vocabulary/Q6; constitution VIII).
- **FR-017**: Opaque `trackId` MUST be unkeyed SHA-256 of invariant-decimal movie id + NUL + UTF-8 exact filename, encoded unpadded base64url; MUST NOT use HMAC/secret or reversible path decode; collision MUST fail closed; fresh allowlist match MUST precede open (Phase 2 step 6; Sentry F2; Ledger S2).
- **FR-018**: Conversion MUST enforce configured `LibraryOptions` bounds `MaxSubtitleInputBytes=8388608`, `MaxSubtitleCues=100000`, `MaxSubtitleLineCharacters=16384`, `MaxSubtitleOutputBytes=33554432` via existing Range validation/startup wiring during bounded read/parse/emit; exceeding MUST yield sanitized `ValidationException`/422; tests MUST cover exactly-at, first-over, and invalid configuration (Phase 2 step 6; Sentry F1).
- **FR-019**: Content handler (T007) MUST own reauthorization, fresh enumeration, token lookup, cancellation, disposal, decode, and conversion; thin transport (T008) MUST own dispatch/metadata/headers only; diagnostics MUST carry `MovieId` and sanitized categories via `TelemetryConstants` without new spans/metrics (Compass F1; Ledger S3/S6).
- **FR-020**: Constitution Principle II/V mapping of `LibraryFileAccessDeniedException` → 403 remains **unresolved** / pickup-blocked until **both** (a) **explicit owner approval** of the exact proposed amendment text (or owner-amended text incorporated consistently across artifacts) **and** (b) verified Gate 1 merge are recorded; reject, unanswered, or any non-approval disposition keeps the item blocked even if a disposition record and merge receipt both exist — do not count any answer as approval; until unblocked Gate 1 stays closed, PLAN FROZEN is withheld, and no route-local or pre-approval global arm is authorized (Ledger S4; brief Ledger S4 final Patron disposition).

### Key Entities

- **SubtitleTrack / SubtitleTrackDto**: Selectable eligible sidecar metadata (`id`, `label`, `language`, `isDefault`) exposed by the list endpoint after Api mapping from sealed Core track records.
- **Sidecar subtitle file**: Same-directory SRT whose name matches the frozen stem/suffix policy relative to the resolved primary video.
- **WebVTT document**: On-request UTF-8 conversion result for one track's content endpoint (sealed immutable Core payload).
- **Viewer subtitle selection**: Transient Off/track choice bound to the active browser viewer session for one movie, guarded by current movie/selection generation.
- **LibraryOptions subtitle bounds**: Four Range-validated positive limits on existing `LibraryOptions` governing bounded read/convert.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a movie with eligible sidecars, an authorized viewer can obtain a non-empty track list whose labels match the frozen recognition table (including Portuguese Brazil for `-pob` / `.pob`).
- **SC-002**: Selecting a listed track in the browser viewer displays that track's cues; choosing Off clears cues.
- **SC-003**: Movies with no eligible sidecars show no subtitle selector; list failures show Retry rather than empty success.
- **SC-004**: Malformed, unsupported-BOM, or over-bound subtitle content fails closed with a sanitized error and does not interrupt other tracks' availability or (in the viewer) continuing video after failure handling.
- **SC-005**: Unauthorized library access cannot list or fetch subtitle bytes for a movie folder the caller may not read (403 contract preserved; constitutional mapping path gated on owner amendment).
- **SC-006**: Delivery either records real-browser Compose evidence for SMOKE-001–004 family checks or reports Could not run — never treats missing evidence as PASS.

## Assumptions

- Language labels, duplicate counters, ordering, initial Off, session-local selection, empty-state hide, list-failure Retry, and `Subtitles unavailable` copy are Patron taste rulings logged `[assumed]` in `ASSUMPTIONS.md` (Q1/Q4/Q7; Phase 2 step 6).
- Generic sanitized 422 presentation (no paths or cue contents) is an assumed taste/security presentation ruling (Q2/Q3; ASSUMPTIONS).
- DEV-455 browser viewer and DEV-456 safe reader are ticket dependencies and Phase B preconditions — not proven delivered APIs at grill recon HEAD (Q5; ASSUMPTIONS). Existing reader today has only AuthorizeFolder/OpenRead (`recon-DEV-454` supplement:7–11); enumeration is an authorized extension, not a delivery fact.
- Existing runtime/BCL Windows-1252 capability is unverified in recon; inability within existing dependencies requires recon + cited Patron ruling (Q6; ASSUMPTIONS; Ledger S5).
- Check/open race resistance is unproven for the existing reader; T001 platform capability proof is required (Sentry F3).
- Recon inspected-path results do not establish global absence of existing subtitle code (ASSUMPTIONS).
- No separate Technical Design or Acceptance Criteria sections exist in the complete task-script output; Q1–Q8 and Phase 2 step 6 elaborate Scope (recon:51-55; ASSUMPTIONS).
- Ledger S4 constitution amendment remains owner-blocked; Quill proposes text only and does not apply or close the checkbox.
