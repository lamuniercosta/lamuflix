# Feature Specification: Subtitle provider and BS Player-compatible sidecar

**Feature Branch**: `feature/453-spec`
**Created**: 2026-11-09
**Status**: Draft — Plan Challenge round 1 planning repairs against brief Authoritative Plan Challenge adjudication addendum (2026-11-10). **blocked: structural (C4/VII)**. Gate 1 stays closed until the owner checkbox below is answered. No analyze-clean, PLAN FROZEN, implementation clearance, runtime acceptance, or merge claim. Affected SubtitlesOptions / OpenSubtitlesOptions ValidateOnStart implementation remains blocked until owner approval and the corresponding scoped constitution/governance amendment lifecycle. Principle I port work remains independent.
**Input**: Ticket DEV-453 (parent DEV-100); grill-closed `brief.md` (Q1–Q12) + Authoritative analyze adjudication addendum + Authoritative Plan Challenge adjudication addendum (2026-11-10); `CONCLUSIONS.md` corrected Patron analyze rulings; `ASSUMPTIONS.md`; Keel-authored `docs/adr/0038-subtitle-provider-and-sidecar-boundary.md` (Proposed); Maestri notes `DEV-453`, `recon-DEV-453`, `adjudication-DEV-453-Keel`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only). Dependencies: DEV-457, DEV-454. Grill commit: `cd575e55`.

## Owner question — Gate 1

Gate 1 stays closed until the user answers this unresolved structural checkbox. Do not check it, do not amend the constitution on assumed approval, and do not implement the affected ValidateOnStart departure path until owner approval plus the normal scoped constitution/governance amendment. Spec-PR body handoff MUST carry this same unchecked question. Q10 / no guessed numeric defaults remain in force. Principle I remains independent. Implementation C# type names for the `Subtitles:OpenSubtitles` config section are functionality-named (`SubtitlesOptions` / nested `SubtitleProviderOptions`, plus functional telemetry identifiers) to satisfy Principle VIII; the checkbox wording below remains verbatim and does not authorize a vendor-named options class. If/when approved, the scoped Principle VII amendment must also append the new subtitle options record(s) to Principle VII's closed options-record list.

- [ ] Approve a narrowly scoped Principle VII departure for SubtitlesOptions / OpenSubtitlesOptions so missing or invalid subtitle acquisition settings, with no guessed numeric defaults, do not fail host startup via ValidateOnStart(), and are instead evaluated at POST /movies/{id}/subtitles/prepare to return 409 Conflict, preserving read-only startup and existing-track serving?

Basis: brief Authoritative analyze adjudication addendum item 3; brief Authoritative Plan Challenge adjudication addendum items 3–4; CONCLUSIONS.md corrected Patron item 3; constitution Principle VII:500-503/625-626; Q8/Q10; charter §2.3(b).

## Clarifications

### Session 2026-11-09

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: None after Q1–Q12 for provider selection, language/sidecar policy, prepare/serve trigger and deployment modes, public routes/statuses/options, architecture/LocalPlay boundary, acquisition safety/lookup, task/test/gate ordering, settings/error refinement, loop discipline, and grill closure. Phase B pickup verifies dependency contracts, DI/mapping entrypoint filenames, hash reference vectors, provider headers/error schema, and download-host allowlist before affected implementation — those are brief Q9 verification tasks, not open product decisions. No `[NEEDS CLARIFICATION]` markers.

## Scope

**In scope** (`brief.md` Q3–Q10 frozen deliverables; ticket Scope via DEV-453 / `PRODUCT.md` §3):

1. **One verified provider for personal non-commercial use**: OpenSubtitles.com REST is the sole shipped provider after BS Player hash-service evaluation found no public supported API contract (`recon-DEV-453` line 13; Q3). Planning evidence is documentation/staff endorsement, not a live successful download or redistribution license. Live configured-key verification remains separate runtime acceptance (Q3, Q9).
2. **Ordered language preferences and selected subtitle**: Server-configured ordered provider-language codes; selected subtitle is a compatible SRT in the first preferred language with an eligible result. Never fall back outside preferences. Deterministic tie-break within that language: moviehash match first, then highest `download_count`, then lowest subtitle `file_id` (Q4; ASSUMPTIONS).
3. **BS Player-compatible exact-basename sidecar**: One primary sidecar per video as exact video basename + `.srt` (`Film.mkv` → `Film.srt`). No language-suffix naming. Preserve an existing exact-basename sidecar without overwrite or another download (Q4; recon line 13).
4. **Explicit preparation, separate serving**: `POST /movies/{id}/subtitles/prepare` performs per-movie acquisition/save using server preferences only (no client path, provider URL, or language override). Ordinary streaming never initiates provider traffic. `GET /movies/{id}/subtitles` serves an authorized existing exact-basename SRT as `application/x-subrip` with no provider traffic, and with `X-Content-Type-Options: nosniff` plus `Content-Disposition: attachment` using a safe server-derived basename (Q5–Q6; Plan Challenge item 2). Routes capture the raw `{id}` segment (or equivalent reachable binding/fallback) and explicitly parse it so malformed non-integer ids yield **400** while well-formed absent movies yield **404**; valid integer endpoint semantics are preserved. Brief-approved relative route shapes are authoritative; exact existing DI/endpoint-mapping entrypoint and any MapGroup prefix are Phase B Wisp T001 facts (`recon-DEV-453` does not retain `ApiEndpoints.cs` or `/api` grouping) — not Phase A verified claims.
5. **Writable daily-driver vs read-only demo**: `Subtitles:SaveEnabled` defaults false. Read-only demo serves existing tracks without provider calls or writes. Writable daily-driver enables acquisition via explicit config and a writable library mount, documented without committing machine-specific paths or credentials (Q5, Q10).
6. **Approved prepare response and errors**: 200 body `{ outcome: saved|alreadyPresent|noMatch, selectedLanguage: string? }` — `selectedLanguage` is **always** the matched configured preferred-language code on `saved`, and **always null** on `alreadyPresent`/`noMatch`; the DTO field remains nullable across the three-outcome union. Existing-sidecar check runs after movie/video/library authorization and before acquisition-configuration or write checks, so `alreadyPresent` returns 200 even when saving is disabled, the mount is read-only, or Api-Key is absent. Transport malformed/unparseable route ID is explicitly **400** ProblemDetails + `traceId` via raw-segment capture/parse (or equivalent reachable binding), not left to `{id:int}` route-constraint dead branches. Validly bound command/query FluentValidation `ValidationException` remains **422** via the single ProblemDetails + `traceId` mapper (never remapped to 400); 422 coverage follows real reachable validators only — do not invent unreachable GET validation scenarios. Do **not** claim all validation is 400. Other prepare failures: 403 library authorization/containment denial, 404 movie/video absent, 409 acquisition disabled/unconfigured/unwritable or local write-permission failure (including invalid local acquisition `BaseUrl`), 429 upstream quota (propagate safe Retry-After when it parses as nonnegative delta-seconds or HTTP-date; omit absent/malformed; no invented cap), 502 provider/content/unsafe-URL/redirect failure, 504 **provider** timeout only (caller abort propagates as cancellation and MUST NOT map to 504; no rollback promise after a committed atomic save). GET: 404 missing movie/video/sidecar; 403 unauthorized library access. Application-validation 422 MUST appear in OpenAPI metadata and tests alongside the approved statuses. Errors disclose no filesystem paths or secrets (Q6, Q10; brief addendum C1/V; Plan Challenge items 2, 6).
7. **Narrow ports and adapters**: Existing Core / Infrastructure / Api projects only. Two narrow Core ports `ISubtitleProvider` and `ISubtitleSidecarStore`; keep `ILibraryFileReader` read-only (ADR 0037) and use it for authorized moviehash head/tail reads. `ISubtitleProvider` accepts computed moviehash/size/title/preferences as data only — never a local path or stream. Infrastructure OpenSubtitles HTTP adapter and contained filesystem sidecar adapter; Api `SubtitleEndpoints.cs` with DTO/options/composition. Functionality-named options/telemetry (`SubtitlesOptions` / `SubtitleProviderOptions`); vendor nesting retained only as config path `Subtitles:OpenSubtitles:*`. No new NuGet/npm packages, schema, project, top-level folder, or architectural layer; no provider registry/SDK (Q7; ADR 0038; Plan Challenge items 4–5).
8. **Contract chain**: C# DTO → `web/src/api/openapi.json` → generated `web/src/api/types.ts`; never hand-written duplicate TS types. Backend-only scope; no web UI workflow in this ticket (Q5–Q6).
9. **Safety**: Authorize resolved primary video folder and regular file before hashing/reading/writing; server derives exact sidecar path; reject traversal, reparse/symlink escapes, and nonregular files; same-directory temp + atomic create-without-replace; concurrent-create collision → `alreadyPresent` after safe recheck; before every provider request validate HTTPS scheme and membership in the verified API/download host allowlist, disable automatic redirects (`AllowAutoRedirect = false`), and reject **all** `3xx` as **502** (no invented hop cap; rejected destinations MUST NOT be contacted); no caller-controlled URLs; configured size/time bounds; reject non-SRT/invalid/oversized downloaded content under the minimal SRT criteria in FR-012; no automatic download retry; never persist temporary download URLs; never log keys, paths, or subtitle content; bound serving resources. Invalid local acquisition-config `BaseUrl` remains prepare-time **409**; upstream unsafe URL/redirect remains **502** (Q8, Q10; Plan Challenge item 1).
10. **Lookup**: moviehash-first for eligible videos (16-hex size + first/last 65,536 bytes for files ≥ 131,072 bytes) computed through the authorized read-only `ILibraryFileReader` seam; title fallback uses the authorized movie's primary title only when hash is ineligible or yields no match within preferred languages; null/empty/whitespace title skips the title request and yields `noMatch` after permitted fallback, and never overrides a successful hash match. Authoritative hash reference-vector verification and Movie primary-title property confirmation required at Phase B pickup before implementing those paths (Q8–Q9; Plan Challenge items 5–6).
11. **LocalPlay separation**: Subtitle HTTP preparation/serving is independent of `Features:LocalPlay`. No `Process.Start`, player-argument, or launch-path changes. Actual local-player execution remains gated behind `Features:LocalPlay` (Q7).
12. **Tests, gates, runtime receipts**: Focused unit/integration coverage under existing projects using existing xUnit/FsCheck/WireMock.NET packages only; all applicable harness gates with thresholds from `harness.yml` (never lowered); OpenAPI/TS drift checks for generated contract diff; separate live receipts for configured-key download, writable save, BS Player exact-basename autodetection, and read-only existing-track serving — unrunnable live checks are `Could not run`, never PASS (Q9, Q11).

**Out of scope**:

- Web UI / SPA subtitle workflow (follow-up; Q5).
- BS Player hash-service integration or claims that its endpoint is dead (Q3; recon line 13).
- Language-suffixed sidecar names; multi-language sidecar collections; overwrite/re-download of existing exact-basename SRT (Q4).
- Automatic/background downloads during streaming; caching temporary download URLs; invented local quota reset counters; automatic download retries (Q3, Q5, Q8).
- Redistribution rights or blanket subtitle-content license claims (Q3; recon lines 17–21).
- New packages, projects, layers, schema/migrations, provider registry/SDK, generic repository/mediator/"services" abstraction (Q7).
- Widening `ILibraryFileReader` to writes (ADR 0037 / Q7).
- `Features:LocalPlay` / `Process.Start` / player launch changes (Q7).
- Guessed numeric safety defaults or invented preferred-language defaults (Q8, Q10).
- Root glossary rewrite beyond the five append-only settled terms authorized by brief analyze adjudication item 4 / C4/VIII (preserving all prior CONTEXT.md content).
- Anything outside Q3–Q10 frozen deliverables is a follow-up issue, not a finding in this round (Q11).

**Precedence**: Ticket Scope & Technical Design is already approved authority (`PRODUCT.md` §3). Brief Q1–Q12, Authoritative analyze adjudication addendum, and `CONCLUSIONS.md` govern boundaries. Taste/policy assumptions in `ASSUMPTIONS.md`. Gate 1 stays closed until the owner structural checkbox is answered. Quill invents no decisions.

## Domain vocabulary *(mandatory — same-PR append-only root CONTEXT.md)*

Settled feature terms below are also appended to repository-root `CONTEXT.md` in this same spec PR (brief addendum C4/VIII; Principle VIII). No rewrite of prior glossary content; no deferral to a later delivery PR.

**Ordered language preferences**
Explicit server-configured provider-language codes in selection priority order; no invented default language.
_Avoid_: browser Accept-Language as authority, silent default language

**Selected subtitle**
Compatible SRT in the first configured preferred language that has an eligible result.
_Avoid_: multi-language pack, best overall subtitle across all languages

**Sidecar**
Subtitle file alongside the primary video with exact basename: `Film.mkv` → `Film.srt`.
_Avoid_: language-suffixed names (`Film.eng.srt`) as BS Player autodetection

**Preparation**
Explicit per-movie acquisition/save operation, separate from byte-range video streaming.
_Avoid_: auto-download on stream, background subtitle job

**Existing track**
An authorized exact-basename SRT already on library storage; its language may be unknown.
_Avoid_: treating missing language as noMatch for an already-present file

Consumed vocabulary (already in use; not redefined here): **LibraryPath**, **ILibraryFileReader**, **Features:LocalPlay**, **MovieId**, OpenSubtitles moviehash.

## What the User Will See *(mandatory)*

Nothing visible: this ticket is backend-only (brief Q5). No SPA controls, movie-details subtitle UI, or admin surface ships. Default plain `docker compose up` product UI is unchanged. Callers and tests that invoke the prepare/serve HTTP operations observe the approved JSON/binary contracts; operators configure writable vs read-only modes via documented settings and mounts.

Operator/API outcomes (not default-Compose UI):

- **Where**: New subtitle prepare/serve HTTP operations at the brief-relative routes `POST /movies/{id}/subtitles/prepare` and `GET /movies/{id}/subtitles` (raw `{id}` capture/parse; registration/MapGroup facts deferred to T001); configuration/`README`/Compose docs for read-only demo vs writable daily-driver; regenerated `web/src/api/openapi.json` and `web/src/api/types.ts`.
- **What the operator or API caller sees or does**: Prepare a movie’s subtitle explicitly; serve an existing exact-basename SRT; enable writable acquisition only when SaveEnabled, PreferredLanguages, Api-Key, and related acquisition settings are deliberately configured; keep demo serving working without credentials.
- **What it needs to show up**: Authorized movie with primary video; for acquisition — writable mount + SaveEnabled + non-empty PreferredLanguages + injected Api-Key/BaseUrl/UserAgent/allowlist/bounds; for existing-track serve — exact-basename `.srt` already present.

### Smoke Test

Skipped — nothing visible on the default Compose stack SPA. Runtime provider/save/autodetection/serving proofs are Phase B receipts, not e2e smoke against plain `docker compose up` UI.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Serve existing exact-basename sidecar (Priority: P1)

An authorized caller GETs the movie’s subtitle. When an exact-basename `.srt` already exists beside the primary video, the API returns its bytes as `application/x-subrip` without contacting the provider, without requiring SaveEnabled or an Api-Key, and without writing. Missing movie/video/sidecar yields 404; unauthorized library access yields 403. Serving remains bounded and path/secret-safe.

**Why this priority**: Ticket requires serving existing tracks from the read-only demo library; existing-sidecar precedence is the independent baseline for preparation (Q5–Q6, Q10).

**Independent Test**: Integration/host tests with a real temporary filesystem fixture placing `Film.srt` beside an authorized primary video; assert bytes, content-type, no provider calls, 404/403 maps, and resource bounds — without requiring a live OpenSubtitles key.

**Acceptance Scenarios**:

1. **Given** an authorized movie whose primary video folder contains exact-basename `Film.srt`, **When** `GET /movies/{id}/subtitles` is called, **Then** the response is 200 `application/x-subrip` with the exact sidecar bytes, headers `X-Content-Type-Options: nosniff` and `Content-Disposition: attachment` with a safe server-derived basename (no path disclosure), and no provider traffic.
2. **Given** SaveEnabled false and/or missing Api-Key, **When** GET is called for an existing sidecar, **Then** serving still succeeds with the same anti-sniff/attachment headers.
3. **Given** missing movie, missing primary video, or missing sidecar, **When** GET is called, **Then** the response is 404 without path disclosure.
4. **Given** unauthorized library access or containment denial, **When** GET is called, **Then** the response is 403 without path disclosure.

---

### User Story 2 - Explicit preparation with ordered preferences (Priority: P1)

An authorized caller POSTs prepare for a movie. After authorization, if an exact-basename sidecar already exists, the API returns 200 `alreadyPresent` with null `selectedLanguage` without provider traffic or writes. Otherwise, when acquisition is enabled and configured, the API looks up via moviehash-first (title fallback only when hash is ineligible or yields no preferred-language match; blank title skips the title request and yields `noMatch`), selects deterministically within the first matching preferred language, downloads a compatible SRT from a pre-validated allowlisted HTTPS URL with redirects disabled, and atomically creates the exact-basename sidecar, returning 200 `saved` with `selectedLanguage` always set to the matched preferred-language code. No eligible result returns 200 `noMatch`. Disabled/unconfigured/unwritable acquisition (including invalid local BaseUrl) returns 409. Quota/provider-timeout/provider-content/unsafe-URL/redirect failures map to 429/504/502; caller abort never maps to 504. Malformed id is 400; containment denial is 403; missing movie/video is 404.

**Why this priority**: Ticket core — one verified provider, ordered preferences, BS Player-compatible save in writable daily-driver (Q3–Q8, Q10).

**Independent Test**: Unit/property coverage for hash vectors/boundaries and preference/tie/noMatch selection; WireMock.NET provider adapter tests; real temp-filesystem sidecar store tests for atomic create, no-overwrite, races, containment; API integration covering prepare outcomes and error map — no real credentials in fixtures.

**Acceptance Scenarios**:

1. **Given** an authorized movie with an existing exact-basename sidecar, **When** prepare is called (even with SaveEnabled false or no Api-Key), **Then** the response is 200 `{ outcome: alreadyPresent, selectedLanguage: null }` with no provider traffic and no overwrite.
2. **Given** no sidecar, SaveEnabled true, non-empty PreferredLanguages, and valid acquisition settings, **When** prepare finds an eligible preferred-language SRT, **Then** the response is 200 `{ outcome: saved, selectedLanguage: <matched-preferred-code> }` (always non-null) and `Film.srt` is created atomically beside the video.
3. **Given** no sidecar and no eligible preferred-language result (including blank primary title after permitted hash fallback), **When** prepare completes lookup, **Then** the response is 200 `{ outcome: noMatch, selectedLanguage: null }` with no sidecar written.
4. **Given** no sidecar and acquisition disabled/unconfigured/unwritable (or local write-permission failure / invalid local acquisition BaseUrl), **When** prepare is called, **Then** the response is 409 with an actionable disabled/unconfigured/unwritable signal and no path/secret disclosure.
5. **Given** upstream quota / provider timeout / provider or unsafe-URL/redirect or invalid content failure, **When** prepare is called, **Then** the response is 429 (with safe Retry-After when it parses as nonnegative delta-seconds or HTTP-date; omit absent/malformed) / 504 / 502 respectively; caller abort does not become 504; no automatic download retry; temporary URLs are not cached; rejected destinations are never contacted.
6. **Given** malformed/unparseable transport id / containment denial / missing movie or video, **When** prepare is called, **Then** the response is 400 / 403 / 404 respectively (raw `{id}` capture/parse reaches the non-integer 400 path; well-formed absent id remains 404).
7. **Given** a validly bound prepare/serve command or query that fails FluentValidation on a real reachable validator, **When** the validation decorator throws `ValidationException`, **Then** the single ProblemDetails mapper returns **422** (never remapped to 400) with `traceId`, and OpenAPI/tests document that application-validation contract without inventing unreachable GET validation failures.
8. **Given** multiple compatible SRT candidates in the first matching preferred language, **When** selection runs, **Then** moviehash match wins first, then highest download_count, then lowest subtitle file_id — never a tie rejection and never a language outside preferences.

---

### User Story 3 - Generated OpenAPI contract chain (Priority: P2)

C# DTOs and endpoint metadata document the prepare response shape and approved statuses plus the binary GET subtitle response. Regeneration updates committed `web/src/api/openapi.json` and generated `web/src/api/types.ts`. Generated artifacts are never hand-edited; no duplicate hand-rolled TS types.

**Why this priority**: Charter contract chain even on backend-only tickets (Q6; `PRODUCT.md` §5 item 4).

**Independent Test**: Existing OpenAPI regeneration / inventory / `npm run check:api` (or equivalent drift checks) green after tooling regeneration.

**Acceptance Scenarios**:

1. **Given** the prepare/serve endpoints with explicit metadata, **When** the contract chain regenerates, **Then** OpenAPI and generated TypeScript match the approved DTO/statuses without hand edits.
2. **Given** backend-only scope, **When** contracts regenerate, **Then** no SPA workflow is required for the chain to be complete.

---

### User Story 4 - Setup documentation and runtime receipts (Priority: P1)

Documentation distinguishes read-only demo (no acquisition calls/writes; existing-track serving) from deliberately writable daily-driver setup without committing machine-specific paths or credentials. After implementation, separate live receipts record configured-key provider search/download, successful writable sidecar save, supported BS Player autodetection of exact-basename SRT, and read-only demo serving — or honest `Could not run` with reason.

**Why this priority**: Ticket acceptance spans writable save and read-only serving; automated tests do not substitute for runtime evidence (Q5, Q9–Q10).

**Independent Test**: Doc review against Q5/Q10; scrubbed runtime receipts under `specs/DEV-453/` (conventional `runtime-acceptance.md`) or explicit `Could not run`.

**Acceptance Scenarios**:

1. **Given** the default demo configuration, **When** an operator follows the docs, **Then** existing-track serving works without Api-Key and without acquisition writes.
2. **Given** writable daily-driver intent, **When** an operator follows the docs, **Then** they see how to enable SaveEnabled, supply PreferredLanguages, inject secrets, and use a writable mount — without committed credentials or machine-specific paths.
3. **Given** a configured key and writable setup on the delivery SHA, **When** live verification runs, **Then** receipts record provider download + save + BS Player autodetection, or `Could not run` with reason — never silent PASS.
4. **Given** read-only demo with an existing sidecar, **When** live serving verification runs, **Then** the receipt shows serve-without-acquisition evidence, or `Could not run` with reason.

### Edge Cases

- Existing sidecar present with SaveEnabled false / read-only mount / missing Api-Key → 200 `alreadyPresent` (Q6, Q10).
- Concurrent create collision → recheck and resolve to `alreadyPresent` without overwrite (Q8). Concurrent prepares may consume two downloads before the atomic-create winner; that personal-scale risk is accepted without adding per-movie locking (Plan Challenge item 2.3).
- Ordinary cancellation during download/write → cleanup; no partial sidecar left behind (Q8). Abrupt process death between temp create and atomic move may orphan a temp file; accepted nonblocking risk without a background janitor (Plan Challenge item 2.3).
- Caller abort (`RequestAborted`) propagates as cancellation and never maps to provider 504; genuine provider timeout maps to 504; no rollback promise after a committed atomic save (Plan Challenge item 2.2).
- Hash-ineligible small file (< 131,072 bytes) or hash with no preferred-language match → title fallback using the authorized movie primary title; blank title skips the title request and yields `noMatch` (Q8; Plan Challenge item 6.2).
- Non-SRT / invalid / oversized download, pre-request host/HTTPS failure, or any `3xx` redirect → 502; no write; rejected destination never contacted (Q8, Q10; Plan Challenge item 1).
- Invalid local acquisition BaseUrl → prepare-time 409; upstream unsafe URL/redirect → 502 (Plan Challenge item 1).
- Upstream 429 without inventing local quota counters; propagate Retry-After only when it parses as nonnegative delta-seconds or HTTP-date; omit absent/malformed; no invented cap (Q3, Q8; Plan Challenge item 6.4).
- Missing/invalid acquisition settings must not prevent demo startup or existing-track serving (Q10).
- Ordinary `GET /movies/{id}/stream` must not trigger provider traffic (Q5).
- Existing-track GET serves exact authorized bytes without applying the new downloaded-SRT validity gate; header hardening does not alter bytes (Plan Challenge O3).
- Language-suffixed sidecar names are not treated as the selected BS Player-compatible track (Q4).
- Phase B must verify hash vectors, exact API/download hosts + protocol compatibility (STOP if redirects required), Movie primary-title property, `ILibraryFileReader` head/tail read contract, download raw-vs-wrapper payload shape, feature-folder enumeration/isolation, provider header/error schema, and DI/mapping filenames before implementing affected paths (Q9–Q10; Plan Challenge items 1, 5–6).
- Anything outside Q3–Q10 frozen scope is a follow-up issue, not a finding in this round (Q11).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST treat OpenSubtitles.com REST as the sole subtitle provider for personal non-commercial use, with injected Api-Key and descriptive application/version User-Agent; MUST NOT integrate the BS Player hash service; MUST NOT infer redistribution rights or claim live verification from planning evidence alone (Q3; recon-DEV-453 lines 13, 17–21).
- **FR-002**: System MUST apply server-configured `Subtitles:PreferredLanguages` in order; select the first preferred language with a compatible eligible SRT; MUST NOT fall back outside preferences; within that language MUST break ties by moviehash match first, then highest download_count, then lowest subtitle file_id (Q4; ASSUMPTIONS).
- **FR-003**: System MUST use exact-basename sidecar naming (`Film.mkv` → `Film.srt`); MUST NOT use language-suffix names; MUST preserve an existing exact-basename sidecar without overwrite or re-download (Q4).
- **FR-004**: System MUST expose `POST /movies/{id}/subtitles/prepare` (no client path/provider URL/language override) and `GET /movies/{id}/subtitles` at the brief-approved relative routes, capturing the raw `{id}` segment (or equivalent reachable binding/fallback) and explicitly parsing it so valid integer semantics remain while malformed non-integer ids reach **400** (exact MapGroup/registration entrypoint from Phase B T001 — not asserted here); ordinary streaming MUST NOT initiate provider traffic (Q5–Q6; Plan Challenge item 2.4).
- **FR-005**: Prepare 200 body MUST be `{ outcome: saved|alreadyPresent|noMatch, selectedLanguage: string? }` with `selectedLanguage` **always** set to the matched configured preferred-language code on `saved`, and **always null** on `alreadyPresent`/`noMatch`; the DTO field remains nullable across outcomes (Q6; Plan Challenge item 6.1).
- **FR-006**: After movie/video/library authorization, prepare MUST evaluate existing exact-basename sidecar before acquisition-configuration or write checks so `alreadyPresent` returns even when SaveEnabled is false, the mount is read-only, or Api-Key is absent (Q6, Q10).
- **FR-007**: Transport malformed/unparseable route id MUST map to **400** ProblemDetails + `traceId` via raw-segment capture/parse (or equivalent reachable binding), not `{id:int}` route-constraint-only. Validly bound FluentValidation `ValidationException` MUST map to **422** via the single ProblemDetails + `traceId` mapper and MUST NEVER be remapped to 400; OpenAPI metadata and tests MUST document the application-validation 422 contract for real reachable validators only (no invented unreachable GET validation failures; STOP on unresolved validator/contract conflict rather than dropping approved statuses). Do not claim all validation is 400. Prepare MUST also map containment/authorization denial→403, missing movie/video→404, acquisition disabled/unconfigured/unwritable or local write-permission failure (including invalid local acquisition BaseUrl)→409, upstream quota→429 (propagate Retry-After when it parses as a valid RFC 9110 value: nonnegative delta-seconds or HTTP-date; omit absent/malformed; no invented cap), provider/content/unsafe-URL/any-`3xx`-redirect failure→502, **provider** timeout→504; caller abort/`RequestAborted` MUST propagate as cancellation and MUST NEVER map to 504; after a committed atomic sidecar create there is no rollback promise for a later-aborted client. GET MUST map missing movie/video/sidecar→404 and unauthorized access→403; errors MUST omit filesystem paths and secrets. All declared prepare/serve failure statuses MUST be typed Core exceptions/failure categories translated by the existing single ProblemDetails mapper — no new result-type layer and no endpoint-local status mapping (Q6, Q10; brief addendum C1/V; Principle V:451-466; Plan Challenge items 2, 5.3, 6.4, O1).
- **FR-008**: GET MUST return authorized exact-basename SRT bytes as 200 `application/x-subrip` with no provider traffic; MUST set `X-Content-Type-Options: nosniff` and `Content-Disposition: attachment; filename="<safe-ascii-or-rfc5987-basename>.srt"` derived strictly from the server-resolved safe basename (never exposing directory paths); serving MUST remain resource-bounded and MUST serve existing authorized exact bytes without applying the downloaded-SRT validity gate (Q5–Q6, Q10; Plan Challenge item 2.1 / O3).
- **FR-009**: Configuration MUST provide `Subtitles:SaveEnabled` (default false), `Subtitles:PreferredLanguages` (empty by default), and under vendor-mandated config path `Subtitles:OpenSubtitles` the values `ApiKey` (secret injection only), `BaseUrl`, `UserAgent`, `DownloadAllowedHosts`, `MaxDownloadBytes`, and `RequestTimeout` — all acquisition bounds explicitly configured and positively validated; no guessed numeric safety defaults; missing/invalid acquisition settings (including invalid local BaseUrl) yield prepare-time 409 without blocking demo startup or existing-track serving (Q6, Q10; nesting authorized by brief). C# options/telemetry identifiers MUST be functionality-named (`SubtitlesOptions`, nested `SubtitleProviderOptions`, functional TelemetryConstants) — never a vendor-named options class; Principle VIII permits the vendor key path and Infrastructure adapter class name only. Implementing a Principle VII ValidateOnStart departure for these options remains **blocked** until the owner structural checkbox is approved and the scoped constitution/governance amendment lands; that eventual amendment MUST also append the new subtitle options record(s) to Principle VII's closed options-record list (brief addendum C4/VII; Plan Challenge items 3–4).
- **FR-010**: System MUST introduce only Core ports `ISubtitleProvider` and `ISubtitleSidecarStore`; keep `ILibraryFileReader` read-only and use it for authorized moviehash head/tail reads without widening; `ISubtitleProvider` MUST accept computed moviehash (`string?`), file size (`long`), title (`string?`), and ordered language preferences as data only — never a local filesystem path or stream, and the OpenSubtitles adapter MUST NOT open local paths; implement OpenSubtitles HTTP adapter and contained filesystem sidecar adapter in Infrastructure; implement `SubtitleEndpoints.cs` in Api; reuse existing projects only; zero new packages/schema/projects/layers; no provider registry/SDK (Q7; ADR 0038; Plan Challenge item 5.1).
- **FR-011**: Before hash/read/write, system MUST authorize the resolved primary video folder and regular file; compute moviehash through the authorized existing read-only `ILibraryFileReader` seam (first/last 65,536 bytes) without duplicating filesystem authorization inside the HTTP adapter; derive the exact sidecar path server-side; reject traversal, reparse/symlink escapes, and nonregular files; write via same-directory temporary file with ordinary cancellation cleanup and atomic create-without-replace; map concurrent-create collision to `alreadyPresent` after safe recheck. No per-movie locking or background temp-janitor is added; concurrent pre-create double-download and crash-orphan temp files are accepted personal-scale nonblocking risks (Q8; Plan Challenge items 2.3, 5.1).
- **FR-012**: Before every provider request (configured search `Subtitles:OpenSubtitles:BaseUrl` and temporary `/download` URLs), the system MUST validate HTTPS scheme and membership in the verified API/download host allowlist, disable automatic redirects (`AllowAutoRedirect = false`), and reject **all** HTTP `3xx` as **502** without inventing a hop cap; rejected destinations MUST NOT be contacted; caller-controlled URLs MUST be rejected. Invalid local acquisition-config BaseUrl remains prepare-time **409**; upstream unsafe URL/redirect remains **502**. Configured size/time bounds MUST be enforced. Downloaded content MUST pass minimal SRT validity before write: non-empty, `<= MaxDownloadBytes`, valid UTF-8 text (UTF-8 BOM permitted), MUST NOT start with HTML/XML/JSON/binary archive signatures (`<`, `{`, `[`, `PK`, gzip `1F 8B`), and MUST contain at least one valid SubRip timestamp cue line (`HH:MM:SS,mmm --> HH:MM:SS,mmm` or `.` millisecond separator); otherwise reject as **502** without writing. Automatic download retries MUST NOT occur; temporary URLs MUST NOT be persisted; keys/paths/subtitle content MUST NOT be logged. This downloaded-content gate does **not** rewrite or re-validate existing-track serve bytes (Q8; Plan Challenge items 1, 6.3, O3).
- **FR-013**: Lookup MUST use moviehash first for eligible videos (16-hex size + first/last 65,536 bytes when size ≥ 131,072 bytes) and title fallback only when hash is ineligible or yields no preferred-language match; title fallback MUST use the authorized movie's primary title property confirmed at Phase B pickup; if that title is null/empty/whitespace, skip the provider title request and return `200 { outcome: noMatch, selectedLanguage: null }` — never overriding a successful hash match. Phase B MUST verify authoritative hash reference vectors and the actual Movie primary-title property before implementing those paths (Q8–Q9; Plan Challenge item 6.2).
- **FR-014**: Subtitle HTTP preparation/serving MUST operate independently of `Features:LocalPlay`; this ticket MUST NOT change `Process.Start`, player arguments, or launch paths (Q7).
- **FR-015**: Contract chain MUST regenerate `web/src/api/openapi.json` and `web/src/api/types.ts` from C# DTOs via existing tooling; MUST NEVER hand-edit generated artifacts or invent duplicate TS types (Q6).
- **FR-016**: Tests MUST cover preference order/ties/noMatch; hash vectors/boundaries; existing-sidecar precedence over missing key/disabled writes; read-only no-network/no-write; ordinary writable save; concurrent creates/no overwrite/ordinary cancellation cleanup; containment/reparse/nonregular denial; oversized/invalid downloaded-SRT content (including blocked prefixes and missing timestamp cue); pre-request HTTPS/host rejection and all-`3xx` redirect rejection with proof that rejected destinations are never contacted; provider auth/quota/timeout/error mapping; caller-abort-vs-provider-timeout separation; transport malformed-id **400** vs well-formed absent **404** vs application-validation **422**; GET nosniff/attachment headers and exact served bytes; and the optional-provider health matrix (enabled/disabled/configured/invalid with always-Healthy + `ready`/`disabled_or_unconfigured` metadata, zero provider calls, and exclusion from `/health/live`) — using existing xUnit/FsCheck/WireMock.NET only; no real credentials in fixtures (Q9; brief addendum C1/V, C2/VI; Plan Challenge items 1–3, 7, 10–11).
- **FR-017**: After Gate 1, Phase B pickup MUST recon merged/delivered DEV-457 and DEV-454 contracts, actual DI/endpoint mapping filenames, current provider headers/error schema, exact API/download hosts and protocol compatibility (STOP if redirects are required by live protocol), `ILibraryFileReader` head/tail read/seek contract, Movie primary-title property, download raw-vs-wrapper payload shape, Principle I feature-folder enumeration and ArchitectureTests namespace-isolation rules, and safe download-host allowlist; amend only for demonstrated drift; STOP on unresolved facts — no invention (Q9–Q10; Plan Challenge items 1, 5–6, 9–10).
- **FR-018**: Implementation MUST run all applicable harness build/analyzer/complexity/inspection/test/property/security/format/mutation gates with thresholds from `harness.yml` (never lowered), plus OpenAPI/TS drift and relevant web checks for the generated contract diff; preserve PASS / `SKIPPED (scope-empty)` / `SKIP` / `N/A` / `Could not run` distinctions (Q9).
- **FR-019**: Runtime acceptance MUST separately evidence configured-key provider search/download, writable sidecar save, BS Player exact-basename autodetection, and read-only existing-track serving; unrunnable live checks are `Could not run`, never PASS; automated tests do not substitute (Q9).
- **FR-020**: Review MUST use the Q11 closing bar (High/Medium and any security/contract/ticket failure block; every Low needs explicit cited disposition; Size L requires Sentry, Ledger, and Compass reports published on the PR pinned to the exact reviewed head; frozen scope = Q3–Q10; two review/remediation rounds maximum; unresolved blockers escalate to Bernstein — never automatic clearance).
- **FR-021**: Optional subtitle-provider health registration MUST use ServiceDefaults/Infrastructure-owned wiring, perform zero network/quota calls, and always report Healthy with sanitized local metadata `ready` or `disabled_or_unconfigured` (including enabled-but-invalid/missing acquisition config). It MUST be excluded from `/health/live` (which runs no dependency checks per Principle VI), MUST NOT assert upstream reachability, and MUST NOT change other dependency health results. Telemetry names MUST live in TelemetryConstants and MUST be functionality-named; processing/error logs MUST carry movie id (brief addendum C2/VI; Plan Challenge item 7; Principle VI:478-490/623-624).
- **FR-022**: Repository-root `CONTEXT.md` MUST gain append-only `term / definition / _Avoid_:` entries for Ordered language preferences, Selected subtitle, Sidecar, Preparation, and Existing track in the same PR that introduces those terms, preserving all prior content (brief addendum C4/VIII; Principle VIII:522/537/629).

### Key Entities

- **Ordered language preferences**: Server configuration list of provider language codes in priority order.
- **Selected subtitle**: The one compatible SRT chosen for a video under preferences + tie-break rules.
- **Sidecar**: Exact-basename `.srt` beside the primary video on library storage.
- **Prepare outcome**: `saved` | `alreadyPresent` | `noMatch` with `selectedLanguage` always present on `saved` and null otherwise.
- **Acquisition settings**: SaveEnabled, PreferredLanguages, and `Subtitles:OpenSubtitles` ApiKey/BaseUrl/UserAgent/DownloadAllowedHosts/MaxDownloadBytes/RequestTimeout bound to functionality-named `SubtitlesOptions` / `SubtitleProviderOptions`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An authorized caller can serve an existing exact-basename sidecar as subtitle bytes without provider traffic, Api-Key, or SaveEnabled.
- **SC-002**: With writable acquisition configured, prepare either saves one preferred-language exact-basename SRT with non-null matched `selectedLanguage`, reports `alreadyPresent`, or reports `noMatch` — never overwrites, never selects outside preferences, and never writes partial sidecars on ordinary cancellation.
- **SC-003**: Prepare/serve failures use the approved status map without disclosing paths or secrets; transport malformed ids are explicitly 400; well-formed absent ids are 404; validly bound FluentValidation failures are explicitly 422 (not remapped to 400); Retry-After covers both delta-seconds and HTTP-date when valid; OpenAPI/tests cover reachable contracts only.
- **SC-004**: Read-only demo starts and serves existing tracks without acquisition calls/writes; missing acquisition settings do not block startup; optional-provider health remains Healthy with sanitized local readiness metadata, zero network calls, and exclusion from `/health/live`.
- **SC-005**: Generated OpenAPI and TypeScript artifacts match the C# contract without hand edits, including application-validation 422 metadata.
- **SC-006**: Live receipts (or honest `Could not run`) exist for configured-key download, writable save, BS Player autodetection, and read-only serving on the delivery evidence set.
- **SC-007**: Applicable harness gates and Size L three-axis review report honest statuses under the Q11 bar; thresholds are never lowered.

## Assumptions

- Policy/taste assumptions already logged in `specs/DEV-453/ASSUMPTIONS.md` (tie-break order; exact-basename preserve policy; explicit prepare trigger; outcome tokens/`selectedLanguage`; option names/defaults; two narrow ports; loop bar).
- Provider choice for planning is OpenSubtitles.com REST per Q3; live configured-key success is not assumed.
- Anonymous quota figure (5 downloads/24h/IP) is documented evidence to handle, not a hard-coded local counter to invent (Q3; recon line 18).
- Exact DI/endpoint mapping filenames, download-host allowlist contents, hash reference vectors, Movie primary-title property, `ILibraryFileReader` head/tail contract, download raw-vs-wrapper payload shape, feature-folder enumeration/isolation facts, and current provider header/error schema are Phase B verification inputs (Q9–Q10) — Quill does not invent them.
- Routes are specified in brief-relative form with raw `{id}` capture/parse semantics; exact DI/endpoint-mapping filenames and any MapGroup prefix are Phase B T001 verification inputs (`recon-DEV-453` does not retain them) — not Phase A codebase facts.
- Concurrent pre-create double-download and crash-orphan temp files are accepted personal-scale nonblocking risks; no locking/janitor expansion (Plan Challenge item 2.3).
- ADR 0038 Proposed text remains accepted historical grill-closure context; operative later Plan Challenge rulings reside in the brief addendum (Plan Challenge item 6.5 / Ledger L3).
- No Gherkin/acceptance-test opt-in requested for this ticket.
- Dependencies DEV-457 and DEV-454 must be delivered (not merely planned) before affected Phase B implementation paths that consume their contracts (Q9).
- ADR 0038 remains Proposed until Gate 1 / governance acceptance lifecycle; Quill does not Accept or edit the ADR body.
