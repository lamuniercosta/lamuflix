# Feature Specification: Stream primary movie file over HTTP

**Feature Branch**: `feature/456-spec`
**Created**: 2026-11-07
**Status**: gate1: provisional
**Input**: Ticket DEV-456 (parent DEV-100); grill-closed `brief.md` (Q1–Q11; Q10 supersedes earlier no-ADR / sole-DI-prerequisite wording; Q11 plan-challenge rulings); `CONCLUSIONS.md` (Q1–Q11); `ASSUMPTIONS.md` (none); Maestri notes `DEV-456`, `recon-DEV-456`, `recon-DEV-456-2`, `recon-DEV-456-3`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only). Dependencies: DEV-438, DEV-459. Pickup Q12: DEV-438 is on main; DEV-459 #343 remains an integration/delivery dependency, not an implementation blocker. Revalidate after DEV-459 lands before live acceptance/delivery closure; T001b/c/d still gate T003.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Q9–Q11; `PRODUCT.md` §5). Q10/Q11 are compliant corrections/refinements (no principle waiver; no ticket deliverable change), so no structural owner checkbox. Ordinary owner Gate 1 on the spec PR remains pending. Patron approval is not owner approval. Quill does not invent PLAN FROZEN or a structural checkbox. Original C1 planning contradiction (port without ADR) is repaired via Patron Q10 and Keel-authored `docs/adr/0037-library-file-reader-port.md` (Bernstein assigned **0037**; ADR exists; remains Proposed until governance/extension exists). Remaining mandatory execution — not claimed Principle I implementation PASS — is scoped constitution enumeration/governance adding only `ILibraryFileReader` (Keel-owned T001d), plus ArchitectureTests append-only `|ILibraryFileReader` whitelist **code** update preserving all 16 existing entries (`recon-DEV-456-3` §3) landed atomically with the port.

## Clarifications

### Session 2026-11-07

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Behaviour for the stream route, range contract, MIME, containment, errors, Compose/browser acceptance, file responsibilities, test order, and loop bar are frozen in brief Q1–Q9. Q10 retains `ILibraryFileReader` and supersedes the no-ADR constraint with a scoped compliance path: ADR `docs/adr/0037-library-file-reader-port.md` (Bernstein assigned **0037**; Keel-authored; already exists — verify at pickup, do not reauthor); constitution named-port enumeration extension + required governance record adding only `ILibraryFileReader` (still mandatory; Keel-owned Build deliverable); ArchitectureTests whitelist append-only `|ILibraryFileReader` before `)$` on `ArchitectureTests.cs:31`, preserving all 16 existing entries and order (`recon-DEV-456-3` §3), landed atomically with the port (future Build execution; not implementation PASS). Q11 freezes exact port signatures, `LibraryFileAccessDeniedException` policy path, async shared open flags, route-local nosniff, Principle VI structured movie-id errors, buildable-red sequencing, fail-loud link fixtures, and inherited-only 422. Phase B Wisp identified `src/LamuFlix.Api/HandlerRegistration.cs` beside the scanner as the existing DI registration seam (Patron Q12). Constitution omission of two prior ports is a recon fact, not authorization to broaden this scoped extension. No `[NEEDS CLARIFICATION]` markers.

## Scope

**In scope** (`brief.md` Q1–Q11; ticket Scope & Technical Design):

1. **Read-only stream route**: `GET /api/movies/{id}/stream` delivers the authorized primary video bytes under the current library caller access policy (thumbnail access-parity precedent). No playback-history/catalog writes, launcher calls, `Process.Start`, secrets, or `Features:LocalPlay` dependency. With `Features:LocalPlay=false`, streaming succeeds. Existing local launch behaviour remains governed by its current flag. Do not call the launcher-bearing play handler to reuse resolution.
2. **Existing resolver reuse**: Primary video means the file selected by existing `IMediaLibraryScanner.ResolvePrimaryVideo` (top-level eligibility, largest-file, ordinal name tie-break). No stream-specific selection rule and no selector rewrite.
3. **Library-root containment (two checks)**: (1) `AuthorizeFolder` on the stored movie folder before `ResolvePrimaryVideo`; (2) `OpenRead` authorizes the returned primary file then opens once. Exact Core port: `void AuthorizeFolder(LibraryPath folder)` and `Stream OpenRead(LibraryPath file)` (BCL `Stream` only; no HTTP/framework types in Core). Same canonicalization and link-denial rules for both operations; reject link/reparse components throughout the candidate chain; use the authorized canonical full path only. Canonicalize configured root and candidates to full paths; platform path rules and separator boundaries; reject outside-root and sibling-prefix escapes, symlink/reparse-point chains, and non-regular files. Policy/safety denial (including non-regular paths) uses new Core Ports `LibraryFileAccessDeniedException`; route-local mapping catches **only** that type → 403. Missing/disappeared folder/file uses existing `NotFoundException` → 404. OS `UnauthorizedAccessException` and genuine I/O/permission/read failure remain sanitized 500 (not blanket-translated into policy denial). `LibraryPath` validation and plain string prefixes are insufficient. Relative movie folders are normal after DEV-459; legacy rooted folders may work only if contained — never via a rooted-path bypass. Follow containment precedents locally in the stream path; do not extract or rewrite thumbnail/cache/discovery subsystems. Open the authorized file once via async `FileStream` with `FileShare.Read` and `FileOptions.Asynchronous | FileOptions.SequentialScan`; serve that response from that handle. Focused concurrent-reader byte proof required; no concurrency subsystem. Trusted local library mount is the explicit threat boundary; same-machine TOCTOU race against open is outside this ticket; missing/replaced content at open fails safely without serving unauthorized bytes; linked folders/files fail closed even when legitimate.
4. **Frozen range / media / OpenAPI contract**:
   - No Range → 200, complete byte-exact file, exact file length, `Accept-Ranges: bytes`, plus route-local `X-Content-Type-Options: nosniff`.
   - Satisfiable single bytes range → 206, exact source slice, exact `Content-Range` and `Content-Length`, `Accept-Ranges: bytes`, plus route-local `X-Content-Type-Options: nosniff`.
   - Unsatisfiable valid byte range → 416, `Content-Range: bytes */length`.
   - Machinery: `Results.Stream` / `TypedResults.Stream` (or equivalent stream file-result family) with `enableRangeProcessing: true` on one open readable seekable file stream with known length. No custom Range parser, whole-file buffering, or multipart implementation. Exact frozen headers are acceptance requirements even where automatic framework emission is not documented; set a frozen header where necessary without replacing framework range parsing. Contradiction of a frozen item blocks delivery and returns to Patron.
   - Malformed Range syntax, unsupported units, and multiple ranges retain framework behaviour; characterize at implementation time; no guaranteed status or multipart promise in this spec.
   - Content-Type: injected or per-instance read-only `FileExtensionContentTypeProvider` lookup using `.` plus resolved `MediaFormat.Extension`; fallback `application/octet-stream`. No copied MIME table; no request-time mutation of provider mappings; never a mutable `static readonly` mapping instance; no browser-codec promise for every supported extension.
   - Pass no `entityTag`, `lastModified`, or download filename; no `Content-Disposition` attachment; do not promise conditional/304/If-Range behaviour.
   - Route-local `X-Content-Type-Options: nosniff` on successful binary **200/206 only**; document, test, and record live header evidence; **no** global/config header change. Preserve all frozen MIME/no-validator/range behavior.
   - Explicit C# OpenAPI metadata: 200 and 206 binary Stream (`string`, `binary`), explicit 416, plus frozen error responses. Preserve C# → `web/src/api/openapi.json` → generated TypeScript / mocks chain; regenerate with existing tools; never hand-edit generated artifacts.
5. **Frozen pre-header error map** (ProblemDetails sanitized — no filesystem paths or exception text; framework-owned 416 exempt from ProblemDetails):

   | Condition before response starts | Status |
   | --- | --- |
   | Malformed movie id | 400 |
   | Missing movie, folder or primary file, including disappearance before open (`NotFoundException`) | 404 |
   | Existing folder with no eligible primary video | 409 |
   | Containment/link/non-regular policy denial (`LibraryFileAccessDeniedException` only) | 403 |
   | Unexpected filesystem access denial or I/O fault (OS `UnauthorizedAccessException` / genuine unreadability; not blanket policy catch) | 500 |
   | Actual existing ValidationException, where applicable (inherited global mapping only) | 422 |

   Do not manufacture 422 for the explicitly mapped conditions; 422 is inherited ValidationException mapping only — no synthetic stream-path ValidationException requirement. Tests assert frozen mapped statuses (and thus exclude 422 for those cases) without promising that every stream path cannot raise validation. Distinguish safety denial (403 via `LibraryFileAccessDeniedException` only) from operational unreadability (500). Preserve current global mappings; stream-local shaping catches only the new policy exception for 403 and does not claim `GlobalExceptionHandler` already supplies Principle VI movie-id structuring. Principle VI: structured errors carry the requested movie id without filesystem paths, including route-local 403, other mapped pre-header errors, and post-header failures; add explicit task/tests; no new telemetry names; no framework types in Core. Transfer stream ownership to the file result; dispose on pre-result failure; framework completion/cancellation owns it after transfer. Once headers/body start, cancellation or read failure aborts/cleans up without a second JSON response or a success claim for truncated delivery. No side effects on error paths.
6. **Required live Compose nginx + browser acceptance** through existing `web/e2e/run.mjs` at `http://localhost:3000` (web nginx `/api/` proxy — never direct API), using sample-library import and source MP4 corpus: full byte-exact; prefix/suffix/open-ended 206; beyond-EOF 416; disjoint sequential and backwards seek slices; malformed/unit/multiple characterization without promised statuses; LocalPlay=false success with no history/process/launcher calls; required real-browser seek on a browser-decodable sample MP4 through the stream URL (harness context — no product player UI; no all-format codec guarantee). Record evidence pinned to the delivery SHA. No Docker / unavailable browser codec ⇒ required acceptance Could not run (blocks closure); script exit-2 SKIPPED remains SKIPPED, never PASS. No speculative nginx/Compose tuning without a further Patron ruling after concrete failed proxy evidence.
7. **Bounded approach and files** (Q7; Q10–Q11): new `StreamEndpoints.cs`, narrow `ILibraryFileReader` + `LibraryFileAccessDeniedException` + `LibraryFileReader`, focused endpoint and file-reader tests, `ApiEndpoints.cs` registration, existing API DI composition seam `src/LamuFlix.Api/HandlerRegistration.cs` beside `IMediaLibraryScanner` (Phase B Wisp; Patron Q12), `OpenApiContractTests.cs`, `LibraryEndpointsTests.cs`, `web/e2e/run.mjs` (+ existing smoke harness), regenerated `openapi.json` / `types.ts` / `mocks/handlers.ts`. No new Core handler, general streaming service, repository/mediator layer, project, top-level folder, dependency, schema or migration. No product UI, launcher/playback-handler changes, global exception remapping, selector rewrite, thumbnail/discovery extraction, nginx/Compose config rewrite, file deletion, or unrelated rewrite. No CONTEXT.md glossary rewrite (Q9).
8. **New-port constitution compliance** (Q10–Q11; Principle I `:299-305`; permitted enumeration extension `:65-82`; Governance `:712-734`; `recon-DEV-456-3`): retain approved narrow `ILibraryFileReader`. Original planning contradiction repaired via Q10 + authored ADR **0037**. Before/with port implementation: (a) Keel verify-only pickup of existing Keel-authored `docs/adr/0037-library-file-reader-port.md` (Bernstein assigned **0037**; do not reauthor; ADR remains Proposed until extension/governance exists); (b) distinct Keel-owned constitution named-port enumeration extension adding only `ILibraryFileReader` with its required governance record (appropriate MINOR — **1.11.0** if pickup still **1.10.0**, else refresh at pickup; Sync Impact Report; Last Amended; template review; migration/follow-up) — preserve existing enumerated entries/project-reference rules/ADR requirement; not a principle waiver; do **not** backfill constitution-omitted `IEnrichmentOutbox` / `IRecommendationProfileSource` (§3.4 recon fact ≠ authorization); if exact equivalent already landed, verify evidence instead of duplicating; (c) ArchitectureTests whitelist: append only `|ILibraryFileReader` inside `PortWhitelistPattern` at `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:31` immediately before `)$`, preserving all 16 existing entries and order; port file + exception + whitelist **code** edit land as **one atomic buildable type slice** (evidenced whitelist **plan** confirmed before that slice). These bounded surfaces supplement Q7 file scope as compliance work for the approved port; they do not change ticket deliverables and create no structural owner checkbox. ADR existence does not earn Principle I implementation PASS — constitution/governance + atomic port/whitelist execution remain mandatory. Clear STOP prerequisites; no premature compliance PASS.

**Out of scope**:

- Product player UI / SPA stream consumer.
- LocalPlay flag changes, launcher/`Process.Start`/secrets changes, playback-history writes from stream.
- Custom Range parser, multipart/byteranges promise, conditional/304/If-Range contract.
- Extracting shared containment helpers from thumbnail/cache/discovery.
- Nginx/Compose config rewrite without concrete failed proxy evidence + further Patron ruling.
- New packages, projects, layers, schema/migrations, Core business handler, general streaming service, concurrency subsystem.
- Global/config header changes for nosniff (route-local only).
- CONTEXT.md glossary rewrite (Q9). Broadening the scoped constitution extension to backfill historically omitted ports. Marking Principle I implementation PASS without scoped governance + atomic port/whitelist evidence (ADR 0037 alone is insufficient).
- Atomic race-proof filesystem sandbox against same-machine malicious writers on the trusted mount.
- All-format browser codec guarantees.
- Sentry F4 personal-scale residual throttle work; Ledger L4 ADR-number search (0036 occupied on feature/458-spec per recon-3 §2 — answered; no numbering repair). ADR M1 body repair remains Keel-owned.

**Precedence**: Ticket Scope & Technical Design is already approved authority (`PRODUCT.md` §3). Brief Q1–Q11 and `CONCLUSIONS.md` govern boundaries (Q10 supersedes earlier no-ADR / sole-DI-prerequisite wording only; Q11 freezes plan-challenge refinements). Ordinary owner Gate 1 remains pending. Quill does not invent PLAN FROZEN.

## Domain vocabulary *(mandatory — feature terms; Q9 forbids CONTEXT.md glossary rewrite this ticket)*

**Primary video**
The file selected by the existing `IMediaLibraryScanner.ResolvePrimaryVideo` rule (not a stream-specific selection rule).
_Avoid_: stream file (when meaning a new selection), main file invented by the endpoint

**Movie folder**
The stored library folder for a movie; normally canonical relative after DEV-459.
_Avoid_: absolute host path as identity (post–DEV-459 normal case)

**Library root**
Configured `LibraryOptions.RootPath`.
_Avoid_: process CWD, sample-library path as a second root

**Stream**
The bytes of the authorized primary file over HTTP — not a local-player launch and not a playback-history event.
_Avoid_: play, launch, LocalPlay

**Seeking proof**
Both byte-range request sequences and browser playback after a seek through the stream URL.
_Avoid_: optional smoke (browser seek is required acceptance per Q6)

Consumed vocabulary (already in use; not redefined here): **LibraryPath**, **MediaFormat**, **Movie**, **IMediaLibraryScanner**, **Features:LocalPlay**, **MovieId**.

## What the User Will See *(mandatory)*

Nothing visible in the Compose-stack SPA product UI: DEV-456 is API streaming only (brief Q1/Q6/Q7). No product player, movie-details stream control, or admin surface ships. Clients and harnesses that call `GET /api/movies/{id}/stream` (including through Compose nginx) observe binary full/partial responses; required browser seek proof runs in the e2e harness against the stream URL, not as product UI.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Contained authorize-and-open (Priority: P1)

Before any bytes leave the API, the stream path authorizes the stored movie folder (`AuthorizeFolder`), resolves primary video with the existing rule, authorizes the returned file and opens once (`OpenRead`) under the library root as one readable seekable regular file via async shared `FileStream`. Outside-root, sibling-prefix, symlink/reparse, and non-regular candidates fail closed (`LibraryFileAccessDeniedException`). Relative folders are the normal case; legacy rooted folders work only when contained.

**Why this priority**: Ticket containment AC and Q4/Q8 step 2; foundation for safe streaming (brief Q4, Q7–Q8; Q11).

**Independent Test**: Real-disk file-reader tests for relative/contained legacy rooted paths, outside/sibling-prefix escapes, **actual** links/reparse chains (fail-loud Windows setup if neither permitted symlink nor genuine junction fixture can be created — never false PASS/SKIPPED), missing files (`NotFoundException`), non-regular/policy denial (`LibraryFileAccessDeniedException`), distinct operational unreadability, returned-file auth before open, single authorized open of canonical path, and focused concurrent-reader byte proof — without requiring the HTTP endpoint. Filesystem authorization is never mocked. Build/Linux live link proof remains required; no invented observed capability.

**Acceptance Scenarios**:

1. **Given** a movie folder stored as a canonical relative path under the library root with an eligible primary video, **When** authorize-and-open runs, **Then** it returns one readable seekable stream for that authorized regular file opened once on the canonical authorized full path with `FileShare.Read` and Asynchronous|SequentialScan.
2. **Given** a legacy rooted folder path that canonicalizes inside the library root, **When** authorize-and-open runs, **Then** it succeeds only because the candidate is contained — not because a rooted path bypasses checks.
3. **Given** an outside-root or sibling-prefix candidate, **When** folder or file authorization runs, **Then** it raises `LibraryFileAccessDeniedException` without opening unauthorized bytes.
4. **Given** a symlink or reparse-point chain on the folder or file, **When** authorization runs, **Then** it fails closed even if the target is under the root.
5. **Given** a missing folder/file, **When** open is attempted, **Then** existing `NotFoundException` semantics apply without serving unauthorized bytes.
6. **Given** a non-regular file, **When** open is attempted, **Then** `LibraryFileAccessDeniedException` safety denial applies without serving unauthorized bytes.
7. **Given** concurrent readers of an authorized openable file, **When** focused concurrent-reader proof runs, **Then** shared-read bytes match without adding a concurrency subsystem.

---

### User Story 2 - HTTP stream with frozen range and error contracts (Priority: P1)

An API client GETs `/api/movies/{id}/stream` under the library caller access policy. Full responses and satisfiable single byte ranges return exact bytes and frozen headers including route-local nosniff; unsatisfiable valid ranges return framework-owned 416 with frozen Content-Range. Pre-header failures map to the frozen ProblemDetails statuses (`LibraryFileAccessDeniedException`→403 only). Streaming succeeds with LocalPlay disabled and never writes history or launches a process. Stream ownership transfers to the file result. Structured errors carry the requested movie id without filesystem paths.

**Why this priority**: Ticket route + full/partial/invalid-range ACs; Q1–Q5; Q8 step 3; Q11.

**Independent Test**: Endpoint/host tests with port doubles for frozen 200/206/416 bytes/headers including nosniff on successful binary 200/206; resolved-format MIME provider mapping + `application/octet-stream` fallback with no Content-Disposition attachment and no validators (`entityTag`/`lastModified`); folder authorization before resolver such that a denied folder prevents `ResolvePrimaryVideo` invocation; returned-file authorization before open with a single authorized open; 400/403/404/409/500 sanitization with correct mapped-status assertions (excluding manufactured 422); Principle VI structured movie-id errors for route-local 403 / pre-header mapped errors / post-header failures without paths; access-policy parity; LocalPlay=false and zero history/process side effects; cancellation/disposal; characterization of malformed/unit/multiple Range and If-Range-without-validators without writing those observations into the public contract. Missing route is a valid behavioral red without referencing a nonexistent endpoint class. Existing framework range machinery and real-disk containment proofs remain required. Do not claim existing `GlobalExceptionHandler` already supplies Principle VI movie-id structuring.

**Acceptance Scenarios**:

1. **Given** a known movie with an authorized primary file and no Range header, **When** the stream endpoint is called, **Then** the response is 200 with the complete byte-exact file, exact length, `Accept-Ranges: bytes`, and route-local `X-Content-Type-Options: nosniff`.
2. **Given** a satisfiable single bytes Range, **When** the stream endpoint is called, **Then** the response is 206 with the exact source slice and exact `Content-Range`, `Content-Length`, `Accept-Ranges: bytes`, and route-local `X-Content-Type-Options: nosniff`.
3. **Given** a valid unsatisfiable byte range, **When** the stream endpoint is called, **Then** the response is 416 with `Content-Range: bytes */length` and is not forced into ProblemDetails.
4. **Given** malformed id / missing movie-folder-file / no eligible video / `LibraryFileAccessDeniedException` containment denial / unexpected I/O before headers, **When** the stream endpoint is called, **Then** the response is 400 / 404 / 409 / 403 / 500 respectively with sanitized ProblemDetails (422 not manufactured for those cases).
5. **Given** `Features:LocalPlay=false`, **When** the stream endpoint is called for a streamable movie, **Then** streaming succeeds and no history write, launcher call, or process start occurs.
6. **Given** client cancellation or mid-body read failure after headers started, **When** transfer aborts, **Then** cleanup occurs without a second JSON response or a success claim for truncated delivery; structured failure carries requested movie id without filesystem paths.
7. **Given** a route-local 403 or other mapped pre-header error, **When** the failure is logged/shaped, **Then** structured output includes the requested movie id and omits filesystem paths.

---

### User Story 3 - Generated OpenAPI contract chain (Priority: P2)

C# endpoint metadata documents 200/206 binary Stream and explicit 416 plus frozen error responses. Regeneration updates committed OpenAPI, TypeScript types, and MSW handlers. Operation and route inventories include the new stream route. Generated artifacts are never hand-edited.

**Why this priority**: Constitution contract chain; Q2–Q3 OpenAPI rule; Q8 step 4.

**Independent Test**: `OpenApiContractTests` / `LibraryEndpointsTests` inventories and `npm run check:api` (or equivalent existing drift checks) green after tooling regeneration.

**Acceptance Scenarios**:

1. **Given** the stream endpoint with explicit Produces metadata, **When** the contract chain regenerates, **Then** OpenAPI documents 200/206 as binary Stream and 416 explicitly, and TypeScript/MSW artifacts match without hand edits.
2. **Given** the new route, **When** inventory tests run, **Then** ExpectedOperations and the library endpoint-name list include the stream operation.

---

### User Story 4 - Live Compose nginx and browser seeking (Priority: P1)

On the delivery SHA, live acceptance runs through Compose nginx at `http://localhost:3000` using the sample-library corpus: full, partial, invalid-range, sequential and backwards seeks, LocalPlay=false side-effect assertions, and required browser seek on a decodable sample MP4. Direct API calls cannot substitute. Evidence is recorded against the delivery SHA.

**Why this priority**: Ticket Compose nginx verification; Q6 required browser seek (not optional); Q8 step 5.

**Independent Test**: Extended `web/e2e/run.mjs` / existing smoke harness through nginx; Docker unavailable or undecodable browser ⇒ Could not run / SKIPPED honesty — never PASS substitutes.

**Acceptance Scenarios**:

1. **Given** Compose up with sample-library import, **When** full and range requests hit the stream URL via `localhost:3000`, **Then** statuses, headers, and bytes match the frozen contract against the source files.
2. **Given** sequential disjoint ranges and a backwards seek, **When** those ranges are requested through nginx, **Then** each response body equals the requested source slice.
3. **Given** a browser-decodable sample MP4 stream URL in a real browser harness context, **When** playback seeks, **Then** playback advances after the seek.
4. **Given** LocalPlay disabled in the live stack, **When** streaming succeeds through nginx, **Then** no history/process/launcher side effects are observed via the asserted seams.
5. **Given** Docker unavailable or browser codec unavailable for the required seek, **When** acceptance is attempted, **Then** the result is Could not run or SKIPPED as applicable — never reported as PASS.

### Edge Cases

- Malformed / unsupported-unit / multiple Range headers: characterize framework behaviour only; no promised status or multipart (Q3).
- If-Range without validators: characterize only; no conditional contract (Q3).
- Exact frozen headers may need endpoint assertion/setting when framework emission is undocumented; failed frozen-header tests block delivery (Q3).
- Seekability/known-length prerequisites are supplied by the implementation and characterized in tests, not asserted as undocumented framework facts (Q2–Q3).
- Linked legitimate library folders/files fail closed (Q4). Actual link/reparse fixtures are mandatory; Windows setup fails loudly if neither permitted symlink nor genuine junction can be created (Q11).
- Same-machine malicious concurrent replacement racing authorize vs open is outside scope (Q4).
- 403 containment denial is distinct from FeatureDisabledException 403 and from 500 unreadability; only `LibraryFileAccessDeniedException` maps route-locally to 403 (Q5; Q11).
- Framework-owned 416 body is not forced into ProblemDetails (Q5).
- 422 remains inherited ValidationException mapping only; no synthetic stream-path ValidationException; correct mapped-status assertions cover negatives (Q11).
- No product player UI; browser seek is harness-only (Q6).
- No nginx tuning without concrete failed proxy evidence + further Patron ruling (Q6).
- Exact DI registration file unidentified until Phase B Wisp (Q7–Q9); never inferred.
- Whitelist facts are evidenced in `recon-DEV-456-3` §3 (16-entry `PortWhitelistPattern` at `ArchitectureTests.cs:30-31`; append-only `|ILibraryFileReader`); refresh only if drifted at pickup.
- Q10/Q11 residual execution: ADR 0037 already authored — verify at pickup (remains Proposed until governance/extension); scoped constitution enumeration/governance for `ILibraryFileReader` only still mandatory (Keel T001d); atomic port+whitelist **code** slice still mandatory with the port; Principle I implementation PASS open until those land; Principle VI structured movie-id errors are an explicit Build requirement, not N/A.
- Anything outside frozen Q1–Q8 scope is a follow-up issue, not a finding in this round (Q9). Sentry F4 residual and Ledger L4 require no scope/code task.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose read-only `GET /api/movies/{id}/stream` under the current library caller access policy, with no playback-history/catalog writes, launcher calls, `Process.Start`, secrets, or `Features:LocalPlay` dependency; LocalPlay=false MUST still stream; MUST NOT call the launcher-bearing play handler to reuse resolution (Q1; ticket route).
- **FR-002**: Stream path MUST reuse existing `IMediaLibraryScanner.ResolvePrimaryVideo` selection (top-level eligibility, largest-file, ordinal tie-break) without a stream-specific selector or selector rewrite (Q1; Q4; ticket resolver reuse).
- **FR-003**: System MUST expose Core port `ILibraryFileReader` with exact signatures `void AuthorizeFolder(LibraryPath folder)` and `Stream OpenRead(LibraryPath file)` (BCL only); authorize the stored movie folder before resolve and authorize-then-open the returned primary file once on the authorized canonical full path; apply the same canonicalization and per-component link/reparse rejection throughout the candidate chain; reject outside-root/sibling-prefix escapes, symlink/reparse chains, and non-regular files; open via async `FileStream` with `FileShare.Read` and `FileOptions.Asynchronous | FileOptions.SequentialScan`; throw `LibraryFileAccessDeniedException` only for policy/safety denial (including non-regular); use existing `NotFoundException` for missing/disappeared folder/file; do not blanket-catch OS `UnauthorizedAccessException`/IO unreadability into policy denial; follow containment precedents locally without extracting thumbnail/cache/discovery helpers. Focused regressions MUST prove: denied folder prevents resolver invocation; returned-file authorization runs before open; exactly one authorized open per successful stream; actual link/reparse fixtures with fail-loud Windows setup; focused concurrent-reader byte proof without a concurrency subsystem (Q4; Q11).
- **FR-004**: File result MUST use `Results.Stream` / `TypedResults.Stream` (or equivalent stream file-result family) with `enableRangeProcessing: true` on one open readable seekable known-length stream; no custom Range parser, whole-file buffering, or multipart implementation (Q2–Q3).
- **FR-005**: No-Range responses MUST be 200 with complete byte-exact body, exact length, `Accept-Ranges: bytes`, and route-local `X-Content-Type-Options: nosniff`; satisfiable single bytes ranges MUST be 206 with exact slice and exact `Content-Range`/`Content-Length`/`Accept-Ranges: bytes` plus route-local nosniff; unsatisfiable valid byte ranges MUST be 416 with `Content-Range: bytes */length`. Nosniff is route-local on successful binary 200/206 only — no global/config header edit; preserve frozen MIME/no-validator/range behavior (Q2–Q3; Q11).
- **FR-006**: Content-Type MUST come from injected or per-instance read-only `FileExtensionContentTypeProvider` lookup on `.` + resolved `MediaFormat.Extension` with fallback `application/octet-stream`; no invented MIME table; no request-time provider mutation; never a mutable `static readonly` mapping instance; no `entityTag`/`lastModified`/download filename; no Content-Disposition attachment; no promised conditional/304/If-Range contract. Focused regressions MUST assert resolved-format provider mapping, fallback when unmapped, and absence of attachment/validators (Q3; Q11).
- **FR-007**: Pre-header errors MUST map malformed id→400; missing movie/folder/file (incl. disappearance before open) via existing `NotFoundException`→404; no eligible primary→409; containment/link/non-regular denial via route-local catch of **only** `LibraryFileAccessDeniedException`→403; unexpected FS access/I/O (OS `UnauthorizedAccessException`/genuine unreadability, not blanket policy catch)→500; actual ValidationException→422 only where the existing inherited global mapping applies — no manufactured/synthetic stream-path ValidationException requirement; ProblemDetails sanitized (no paths/exception text); framework 416 exempt from ProblemDetails. Principle VI structured errors MUST carry the requested movie id without filesystem paths for route-local 403, other mapped pre-header errors, and post-header failures; add explicit task/tests; do not claim existing `GlobalExceptionHandler` already supplies this; no new telemetry names; no framework types in Core (Q5; Q11).
- **FR-008**: Open stream ownership MUST transfer to the file result (no endpoint `using` that closes before send); dispose on pre-result failure; framework owns completion/cancellation after transfer; after headers/body start, abort/cleanup MUST NOT emit a second JSON response or claim success for truncated delivery; no side effects on error paths (Q5).
- **FR-009**: C# OpenAPI metadata MUST declare 200/206 binary Stream and explicit 416 plus frozen error responses; regenerate `web/src/api/openapi.json`, `web/src/api/types.ts`, and `web/src/mocks/handlers.ts` via existing tools; never hand-edit generated artifacts; update `OpenApiContractTests` and `LibraryEndpointsTests` inventories (Q3; Q7).
- **FR-010**: Live acceptance MUST extend `web/e2e/run.mjs` through `http://localhost:3000` nginx `/api/` proxy (never direct API) with the Q6 matrix including required browser seek; record evidence on the delivery SHA; Docker/codec unavailable ⇒ Could not run / SKIPPED honesty never PASS; no nginx/Compose tuning without further Patron ruling after concrete failed proxy evidence (Q6).
- **FR-011**: Implementation footprint MUST be: `src/LamuFlix.Api/Endpoints/StreamEndpoints.cs`; `src/LamuFlix.Core/Ports/ILibraryFileReader.cs`; `src/LamuFlix.Core/Ports/LibraryFileAccessDeniedException.cs`; `src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs`; focused endpoint and file-reader tests including U1 MIME/auth-order/nosniff/policy-distinction/concurrent-read/Principle-VI regressions; `ApiEndpoints.cs` registration; existing API DI seam `src/LamuFlix.Api/HandlerRegistration.cs` beside `IMediaLibraryScanner` (Phase B Wisp; Patron Q12); inventory/e2e/generated surfaces named in Q7; plus Q10/Q11 compliance surfaces — existing Keel-authored `docs/adr/0037-library-file-reader-port.md` (Keel verify-only at pickup; do not reauthor; remains Proposed until governance/extension), scoped constitution named-port enumeration extension adding only `ILibraryFileReader` with required governance record (Keel-owned T001d; appropriate MINOR; Sync Impact Report; Last Amended; template review; migration/follow-up; no DEV-467 backfill), and ArchitectureTests `PortWhitelistPattern` append-only `|ILibraryFileReader` before `)$` at `ArchitectureTests.cs:31` preserving all 16 existing entries/order, committed atomically with the port (`recon-DEV-456-3` §3). No new Core handler, general streaming service, concurrency subsystem, project, layer, package, schema/migration, product UI, launcher changes, global exception remapping, global header config, selector rewrite, thumbnail/discovery extraction, nginx rewrite, deletion, constitution backfill of omitted ports, or unrelated rewrite. No glossary rewrite (Q7; Q9; Q10–Q11). Do not claim Principle I implementation PASS until constitution/governance + atomic port/whitelist evidence exist (ADR alone is insufficient).
- **FR-012**: Delivery MUST follow Q8 order as amended by Q10/Q11: Phase B Wisp pickup facts (drift + exact DI file; whitelist **plan** refresh if drifted from recon-DEV-456-3) → Keel pickup analyze / authorized task-drift adjudication → Keel verify-only ADR/governance/whitelist plan (T001c) → Keel-owned constitution edit deliverable (T001d; or exact-equivalent verified evidence) → atomic buildable port/exception/whitelist **type slice** (T003) → behavioral real-disk red tests (T002; actual link fixture; fail-loud Windows setup) → reader implementation (T004) → DI registration (T005) → endpoint tests/impl (incl. T006a/T006b/T006c nosniff/MIME/auth-order/structured-error) → OpenAPI/contract inventories → Compose nginx/browser acceptance → build-workspace gates. Real filesystem authorization never mocked; endpoint tests may double narrow ports; EF (if touched) uses existing Testcontainers patterns only. Behavior-red tests compile only after referenced surfaces exist; no noncompiling-red commits; missing route is a valid behavioral red without unsupported endpoint-type references. Retain PrimaryVideoSelector property tests; containment boundary properties optional. Never lower thresholds; preserve PASS/FAIL/`SKIPPED (scope-empty)`/harness `SKIP`/mutation `N/A`/`Could not run` distinctions. Required live acceptance remains separately mandatory (Q8; Q10–Q11).
- **FR-013**: Closing bar: Critical/High block code-review and ship-review; Medium/Low each need explicit evidence-based disposition; no disposition waives failed gates or frozen acceptance. Frozen scope = Q1–Q8 set; anything else is a follow-up issue. Cap two remediation/review rounds; remaining Critical/High after cap block and report to Bernstein. Size M requires Sentry, Ledger, and Compass reports published on the PR (Q9).

### Key Entities

- **Primary video / `PrimaryVideo`**: existing resolver result `(LibraryPath Path, MediaFormat Format)`.
- **`ILibraryFileReader`**: narrow Core port with `void AuthorizeFolder(LibraryPath folder)` and `Stream OpenRead(LibraryPath file)` (no HTTP/framework result types).
- **`LibraryFileAccessDeniedException`**: Core Ports policy/safety-denial type (including non-regular paths); route-local 403 catches only this type.
- **`LibraryFileReader`**: Infrastructure adapter implementing containment + async shared authorized open (`FileShare.Read`, Asynchronous|SequentialScan).
- **Stream endpoint**: orchestrates movie-details query, folder auth, resolve, file auth/open, MIME lookup, framework stream result, route-local nosniff on 200/206, route-local policy-exception shaping, Principle VI structured movie-id errors.
- **Movie folder / Library root**: stored path vs configured `LibraryOptions.RootPath` containment boundary.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For an imported sample MP4, a no-Range stream response is byte-exact to the source file with matching length, `Accept-Ranges: bytes`, and route-local `X-Content-Type-Options: nosniff` (ticket full-response AC; Q2–Q3; Q6; Q11).
- **SC-002**: Prefix, suffix, and open-ended single ranges return 206 with exact source slices and exact Content-Range/Content-Length/Accept-Ranges plus route-local nosniff; a valid beyond-EOF range returns 416 with `bytes */length` (ticket partial/invalid AC; Q2–Q3; Q6; Q11).
- **SC-003**: Sequential disjoint ranges and a backwards seek each return the requested source slices; a browser-decodable sample MP4 streamed through the harness advances after seek (ticket seeking AC; Q6).
- **SC-004**: Outside-root/link/non-regular denial yields sanitized 403 via `LibraryFileAccessDeniedException`; missing content yields 404 via `NotFoundException`; no eligible video yields 409; operational unreadability yields sanitized 500; LocalPlay=false still streams with zero history/process/launcher side effects (Q1; Q4–Q6; Q11).
- **SC-005**: Generated OpenAPI/TS/MSW drift checks and applicable delivery gates report honest actual exits; Critical/High review findings are resolved or escalated under the two-round cap (Q3; Q8–Q9).

## Assumptions

None. `ASSUMPTIONS.md` records no taste assumptions. The trusted-library-mount threat boundary is an explicit Q4 ruling, not an assumption. Undocumented range edges are characterized at implementation under Q3 without promised statuses. Exact DI-file identification is Phase B Wisp fact-finding, not assumed. ADR path is Bernstein-assigned `docs/adr/0037-library-file-reader-port.md`. Whitelist change is evidenced by `recon-DEV-456-3` §3, not assumed.

## Traceability

| Source | Encoded as |
|---|---|
| Ticket route / resolver / containment / Compose matrix | Scope; US1–US4; FR-001–FR-003; FR-010; SC-001–SC-004 |
| Q1 access / LocalPlay independence | Scope 1; US2; FR-001; SC-004 |
| Q2–Q3 range / MIME / OpenAPI / characterization | Scope 4; US2–US3; FR-004–FR-006; FR-009; Edge Cases |
| Q4 containment / threat boundary | Scope 3; US1; FR-003; Assumptions (not assumed) |
| Q5 errors / lifetime / cancellation | Scope 5; US2; FR-007–FR-008 |
| Q6 live nginx + browser seek | Scope 6; US4; FR-010; SC-001–SC-004 |
| Q7 files / approach / DI-file deferral | Scope 7; FR-011; plan Approach; tasks Phase 1 |
| Q8 order / gates | FR-012; plan Ordering; tasks phases |
| Q9 loop bar / no glossary rewrite / no checkbox | FR-013; Owner question; Domain vocabulary note |
| Q10 retain port + ADR 0037 / constitution / atomic whitelist | Scope 8; FR-011–FR-012; plan Constitution Check / Ordering; tasks Phase 1–2; DRAFTING_RECEIPT |
| Q11 port signature / policy exception / async open / nosniff / Principle VI / sequencing / link fixture / inherited 422 | Scope 3–5, 8; US1–US2; FR-003, FR-005–FR-007, FR-011–FR-012; plan Constitution Check / Approach / Ordering / Test Strategy; tasks T001c–T007 / T006c; DRAFTING_RECEIPT |
| ASSUMPTIONS.md | Assumptions (none) |
| PRODUCT.md §3–§5 | Precedence; no owner checkbox from grill / Q10–Q11 |
| recon-DEV-456 / recon-DEV-456-2 / recon-DEV-456-3 | Paths and framework facts; MIME/range provenance; ADR number arithmetic + exact whitelist pattern/entries/atomic slice; 0036 occupancy (L4) |
