# DEV-454 grill brief

## Status and provenance

Grill closed with Patron at Q8 of a maximum 12 questions on 2026-11-09. Complete questions, recommendations, answers, rationale and implications are in CONCLUSIONS.md; taste rulings are also in ASSUMPTIONS.md. This is the Phase 2 grill handoff to Quill, not PLAN FROZEN, implementation approval, runtime acceptance, or Gate 1 clearance.

- Worktree: F:/Dev/LamuFlix.worktrees/feature-454-spec.
- Branch: feature/454-spec.
- Recon base: 2ced085e21f2916c035d13de0d281716d823ed2f.
- Sources: shared DEV-454 note and recon-DEV-454, especially lines 12-33 and authoritative ticket supplement lines 39-55. Current code facts below come only from that recon; policy comes from Patron Q1-Q8.
- Authoritative ticket Scope (recon:50): "Recognize BS Player language suffixes such as `-pob`, decode UTF-8 and Windows-1252 `.srt` files, serve WebVTT, and let viewers choose a track."
- Dependencies: DEV-456 and DEV-455 (recon:53). Pipeline: Phase A spec and Gate 1 before implementation.
- Separate Technical Design and Acceptance Criteria were not found in the complete task-script output (recon:51-55). The decisions here elaborate the stated Scope.
- Recon did not establish repository-wide absence of subtitle code (recon:25). Do not turn the inspected-path limitations into an absence claim.
- Patron cites PRODUCT.md sections in its rulings; those are Patron's cited bases, not an independent Keel reconnaissance claim.

## Local vocabulary

- Sidecar subtitle: an existing SRT file in the resolved primary video's directory whose filename matches the policy below.
- Subtitle track: one selectable eligible sidecar, identified through a server-issued opaque token.
- Default marker: an exact default filename suffix; a sorting hint, not permission to auto-enable subtitles.
- WebVTT: the UTF-8 browser subtitle representation produced from eligible SRT content.

No new ADR or global glossary rewrite is required (Q8). Analyze-round-1 Patron alignment requires appending only Sidecar subtitle, Subtitle track, Default marker and WebVTT to existing CONTEXT.md in Phase B when introduced, using term, definition and _Avoid_: entries (constitution VIII; charter 2.3 item 6). This supersedes the earlier no-glossary-change interpretation only; no constitution departure or owner structural checkbox is required.

## Frozen scope and loop discipline

Existing same-directory SRT sidecars for the resolved primary video; Q1 suffix recognition and labels; Q2/Q7 UTF-8 and Windows-1252 conversion; Q3 list/content WebVTT contract; Q4 browser selection; Q5 authorization, file safety and dependency preconditions; Q6 bounded edits. Anything else is a follow-up issue, not a finding in this round.

Closing bar for code-review/ship-review: Critical, High and Medium in-scope findings block. Every Low requires an explicit cited disposition; fix when needed to satisfy an approved contract, otherwise explicitly defer or note under charter policy. No silent dismissal or severity waiver. Unnecessary abstractions are Follow-up, never an automatic fix commit. Follow-up filing follows charter policy; otherwise record "noted, no ticket".

Maximum two challenge/review rounds. Remaining blockers after the cap halt to Bernstein/Patron; the cap never converts a blocker to clearance. All three plan-challenge axes are required before plan freeze. Phase A grill completion does not grant later-stage clearance.

## Approach and behavior

Analyze-round-1 C1 implementation clarification (Patron; constitution II): subtitle Api endpoints are thin HTTP transport dispatching explicit sealed-record subtitle queries and sealed IQueryHandler implementations registered via AddHandler and the established decorators in existing feature organisation. Reuse delivered DEV-456 safe-reader seams only after T001 recon; exact dependency signatures and paths remain unverified until pickup. Existing stream endpoint evidence is not a constitution exemption. Q6 bounded additive source/test authorization covers this; no new port, project, layer or package is approved.

### Recognition (Q1, Q7)

Use case-insensitive exact primary-video-stem matching, in the same directory only:

| Filename pattern/example | language | label | isDefault |
| --- | --- | --- | --- |
| <stem>.srt | und | Subtitles | false |
| <stem>.en.srt or <stem>-en.srt | en | English | false |
| <stem>.pob.srt or <stem>-pob.srt | pt-BR | Portuguese (Brazil) | false |
| <stem>.default.srt or <stem>-default.srt | und | Default | true |
| <stem>.<unknown>.srt or <stem>-<unknown>.srt | und | filename basename | false |

The stem is derived from the resolved primary video, not movie title. No fuzzy matching, recursion, acquisition, external search, extra language inference, or compound language/flag grammar. Unknown suffixes remain selectable. Only exact default sets isDefault. Multiple default tracks remain available and none auto-enables.

Sort default-marker tracks first, then language and label ordinal case-insensitively, then filename ordinal server-side as tie-break. Apply duplicate label counters (2), (3) after sorting. Expose basenames for unknown labels, never directory paths.

### SRT decoding and WebVTT conversion (Q2, Q7)

Reject UTF-16/UTF-32 BOMs with 422. Otherwise use strict UTF-8 first, stripping an optional UTF-8 BOM; fall back to Windows-1252 only when strict UTF-8 decoding fails. Do not statistically guess or report replacement-character decoding as success. Accepted limitation: damaged UTF-8 may decode through the mandated Windows-1252 fallback.

Normalize CRLF/CR to LF. Emit WEBVTT plus a blank line, convert comma milliseconds to dot milliseconds, strip numeric SRT sequence indices, preserve cue order/timing, and escape &, < and > to render cue text literally. No raw HTML injection. Valid overlapping cues are allowed. Structurally invalid, empty/cue-less, unparseable, negative or reversed intervals produce sanitized 422 errors with no paths or cue content. Failure of one track does not block listing or other tracks. No other encoding or subtitle format is included.

### Deliberate public API contract (Q3, Q7; charter 2.3 item 4)

| Request | Successful response |
| --- | --- |
| GET /api/movies/{id:int}/subtitles | 200 JSON array of SubtitleTrackDto; [] for no sidecars |
| GET /api/movies/{id:int}/subtitles/{trackId}/content | 200 converted UTF-8 WebVTT |

SubtitleTrackDto fields: id (opaque string), label (string), language (BCP47 string or und), isDefault (boolean). Tokens are distinct per eligible filename, stable for an unchanged movie-sidecar set and list/content round trip, and never ordinal indexes. No durable identity is promised across renames. Frontend constructs the content URL from movie id and opaque token; no client filename/path input.

Both endpoints use Cache-Control: private, no-store. Content uses Content-Type: text/vtt; charset=utf-8 and X-Content-Type-Options: nosniff. Content Range requests receive full 200 content, no Accept-Ranges and no 206. Conversion is on request, with no disk writes.

- 403: unauthorized library access.
- 404: unknown movie, missing primary media, vanished or unknown track.
- 422: malformed or unsupported-BOM SRT content, on the content endpoint.
- 500: unexpected failure, generic sanitized response.
- Invalid movie route input follows framework routing behavior.

Do not change the movie detail DTO. Contract generation is strictly C# DTO and endpoint metadata -> web/src/api/openapi.json -> generated web/src/api/types.ts. Web consumes generated types without manual duplicates; generated files are regenerated, never hand-edited.

### Browser interaction (Q4, Q7)

Integrate one selector into the dependency-verified browser viewer. Offer Off and server-listed tracks; hide selector for an empty list. Start Off even when a default marker exists. Only the selected track has TextTrack.mode = showing; all others are disabled. Off disables all. Selection is viewer-session-local, resets when movie changes, and does not use localStorage or browser-language detection.

On track-load failure, keep video playing, switch subtitles Off, and display "Subtitles unavailable"; reselecting a track retries. Labels, sorting, initial choice and error wording are recorded as assumed taste rulings.

### Security and dependency preconditions (Q5, Q6)

Both routes reauthorize the movie's library folder before enumeration/open. Resolve content tokens again against current eligible same-directory sidecars for that movie; a token is an identifier, not authority. Reject arbitrary paths/traversal and non-regular files, prevent symlink escape beyond the authorized directory, and use cancellation and stream disposal.

DEV-455 viewer integration and DEV-456 safe-reader integration are planned preconditions, not verified delivery facts. Phase B requires recon receipts for their delivered seams and for existing runtime Windows-1252 capability. If seams are absent/drifted or encoding cannot be implemented with existing dependencies/BCL, stop with needs recon; a package additionally requires a new cited Patron ruling. Do not invent a replacement viewer, unsafe reader or architectural layer.

## Charter 2.3 rulings and files touched (Q3, Q5, Q6)

1. No new NuGet/npm dependency or swap is approved. Unverified encoding support is an explicit pickup check.
2. No new project, top-level folder or architectural layer is approved. Use existing project structure and dependency seams.
3. No schema or persistence changes.
4. The two routes and exact DTO/status/header policy above are deliberately approved.
5. No changes to Features:LocalPlay, local request boundary, playback launch routes, ProcessMediaPlayerLauncher, Process.Start or secrets.
6. Deliberately approved bounded file edits for later implementation:
   - Add subtitle source and tests within existing projects, following verified architecture.
   - Register endpoints in src/LamuFlix.Api/Endpoints/ApiEndpoints.cs.
   - Integrate UI in web/src/features/movie-details and/or the cohesive DEV-455 viewer module, verified by Phase B recon.
   - Add subtitle integration tests alongside tests/LamuFlix.IntegrationTests/StreamEndpointTests.cs and tests at the verified viewer seam.
   - Regenerate web/src/api/openapi.json and web/src/api/types.ts through the contract chain.
   - Append only the four local vocabulary entries to existing CONTEXT.md in the introducing implementation PR (analyze-round-1 Patron alignment; constitution VIII; charter 2.3 item 6). No global rewrite.
   - Keep StreamEndpoints.cs and local playback files untouched absent a new cited Patron ruling. No deletions or wholesale rewrites.

Quill must map additive internal filenames to verified seams without inventing an architectural layer. This Phase A change writes only brief.md, CONCLUSIONS.md and ASSUMPTIONS.md under specs/DEV-454. Quill, not Keel, drafts spec.md, plan.md and tasks.md.

No ticket contradiction or constitution departure is proposed; Patron confirmed no user structural checkbox is required. This does not close any owner checkbox or Gate 1.

## Task ordering and test strategy (Q8)

1. Quill drafts spec/plan/tasks from this brief; Keel runs read-only analysis and checks against this decision record; required challenge and adjudication precede PLAN FROZEN and separate Gate 1.
2. At Phase B pickup, recon verifies delivered DEV-455/DEV-456 seams and encoding capability, then analyzes drift against main before implementation.
3. Implement deterministic discovery, decoder/converter, DTO and routes using the verified safe reader and real filesystem tests.
4. Regenerate and check OpenAPI and TypeScript types.
5. Integrate the selector at the verified viewer seam.
6. Complete integration tests and real-browser acceptance.
7. Run applicable backend verification and web gates with exact status reporting.

Acceptance coverage must include:

- All filename cases above, uppercase recognition, -pob, unrelated files, no recursive matching, unknown suffixes, multiple defaults, deterministic duplicate labels.
- UTF-8 accents with/without BOM, Windows-1252 accented byte fixtures, newline variants, empty/malformed SRT, UTF-16/32 rejection, valid overlap and invalid intervals, exact WebVTT timing and literal markup.
- Real filesystem discovery/reads through the dependency safe reader; no mocked driver behavior that depends on query translation or serialization.
- [] listing, DTO and generated contract, 403/404/422 behavior, no-store, nosniff, full 200 response to Range, sanitized errors.
- Token/movie binding, unauthorized access, traversal, symlink escape, non-regular files, stale/vanished track and one-bad-track isolation.
- Off initially, one showing track, switching/Off, movie-change reset, empty selector state, label ordering, failed load and successful retry without interrupted video.
- Real-browser runtime receipts for visible cues, track switching, Off, and subtitle failure while video continues. Unit tests alone do not prove runtime acceptance.

## Gate expectations and handoff

Analyze round 1 glossary decision (2026-11-09): Patron deliberately authorized the bounded CONTEXT.md additions above to comply with constitution VIII and the PR documentation gate. Full ruling and request are appended to CONCLUSIONS.md. All other Q1-Q8 scope and loop decisions remain in force.

Current phase: documentation inspection and scoped diff verification only; no production test or analyzer pass is claimed. Later backend changes require applicable harness verification; frontend work follows web-implement and web gates. Backend mutation applicability is determined by the actual implementation diff, not inferred from this docs change. Report PASS, SKIPPED, SKIP, N/A and Could not run distinctly.

Ready for Quill's draft from this brief and full Q&A. No implementation started. No merge authority delegated.

## Phase 2 step 6 adjudication decisions (2026-11-09; supersedes conflicting draft language)

All 17 findings from Sentry (8), Ledger (6), Compass (3) are individually disposed in the owned canvas note adjudication-DEV-454-Keel. Patron full seven-group ruling is recorded in CONCLUSIONS.md, Phase 2 Step 6. Round 1 of 2; PLAN FROZEN remains withheld until repairs and final read-only analysis. The following approved decisions replace earlier reuse-only assumptions and must propagate into Quill drafts:

1. Existing ILibraryFileReader has only AuthorizeFolder/OpenRead; no enumeration exists or is promised by DEV-456 (updated recon:7-11). Authorize bounded same-directory regular-file enumeration extension in src/LamuFlix.Core/Ports/ILibraryFileReader.cs and src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs using the existing filesystem abstraction only in Infrastructure, plus corresponding existing reader tests and Api HandlerRegistration. No new port/layer/project/package. Existing port extension does not require a new ADR per Patron; any bounded accuracy update to existing ADR 0037 remains conditional on pickup, never invent another ADR number.
2. Configure conversion defaults on existing LibraryOptions: MaxSubtitleInputBytes = 8388608, MaxSubtitleCues = 100000, MaxSubtitleLineCharacters = 16384, MaxSubtitleOutputBytes = 33554432. Bind with existing options validation/startup wiring, no new options record, magic-string reads or hardcoded enforcement thresholds. Count payload line characters after decoding, output as UTF-8 bytes, and enforce limits during bounded reading/parsing/emission, not only after unbounded allocation. All limits positive and startup-validated; test exactly-at and first-over boundaries and invalid configuration. Exceeding limits yields sanitized ValidationException / 422. No 413 or package added. Corresponding existing options/binding/configuration files and tests are authorized bounded edits at pickup-verified paths.
3. Token derivation is unkeyed SHA-256 of unambiguously framed movie id and exact ordinal filename, encoded unpadded base64url. To eliminate encoding ambiguity, use invariant decimal movie id, NUL separator, then UTF-8 exact filename. No HMAC/secret or reversible path decoding. Recompute fresh movie-scoped eligible-set tokens and lookup before candidate open; collision fails closed without choosing a file. Tampered/unknown/vanished/other-movie ids yield sanitized 404. Stable identity is unchanged-set only.
4. T007 content handler owns reauthorization, fresh eligible enumeration, token matching, cancellation, stream lifetime/disposal, decode and conversion; T008 remains HTTP dispatch/metadata/headers only. Filesystem adapter owns open-time folder/root containment, path-chain link/reparse and opened-handle regular-file checks. Bounded reader strengthening authorized. Do not claim existing check-then-open is race-safe; T001 must verify platform capability for the promised safe-open implementation, STOP needs recon if unavailable, and implementation must include real-FS adversarial replacement tests with no unauthorized bytes returned.
5. Strict UTF-8 uses exception fallback; codepage provider registration only if T001 proves existing dependencies/runtime support and need. Registration idempotent without mutable static initialization in mutated code. No unconditional shared-framework availability claim or silent package.
6. Viewer labels text-only, with hostile-basename coverage. Only current movie/selection generation may apply list/content success OR failure; cancellation alone without stale-result suppression is insufficient. Off, switching, movie change and unmount invalidate pending selection work; revoke owned object URLs where created. Failed list keeps video playing and subtitles Off, shows Subtitles unavailable and explicit Retry, never masquerades as empty success. T012 asserts label ordering as served, rapid switch and movie-change/Off during pending loads, retry and stale failure suppression.
7. Both subtitle routes use private, no-store on success and error (403/404/422/500), with nosniff where applicable; test error paths. Malformed/BOM/over-bound content uses existing ValidationException 422, missing movie/media/track existing NotFoundException 404. The proposed LibraryFileAccessDeniedException global 403 arm requires final constitutional disposition below; do not invent a new exception or repurpose FeatureDisabledException.
8. Preserve literal cue text and valid overlap; add parser-boundary fixtures with arrows and NOTE/STYLE/REGION-looking text. Escape &, <, > without indiscriminate prefixing or rewriting valid payload words. Reject payload constructs that actually cannot be represented as structurally valid literal WebVTT with sanitized 422; a word alone inside a valid cue is not automatically a control block.
9. Queries carry MovieId. Processing/error diagnostics carry movie id and sanitized category, never paths/cue contents. Reuse decorator tracing/logging; no new custom metric/span names. Attribute keys, including any needed category key, must use TelemetryConstants. Include diagnostic assertions. Core list result is a sealed record with immutable collection of sealed track records; content result a sealed immutable document record/string payload, no live stream crossing to Api. Api maps to SubtitleTrackDto; no Core->Api reference. No helper layer or new Core references for BCL encoding/crypto. Serial phases prevent T003/T007 HandlerRegistration conflicts; explicitly explain T025 execution position rather than renumbering historical IDs.

Outstanding constitutional disposition: Patron proposed adding existing LibraryFileAccessDeniedException -> 403 to the single GlobalExceptionHandler. Current constitution II limits expected failure types and V gives a closed status table with fallback 500. Keel requested an explicit narrow II/V alignment amendment or compliant alternative and the charter 2.3(b) owner-checkbox ruling if departure is necessary. The original no-departure/no-checkbox statement in the first Step 6 Patron answer is NOT sufficient closure of this discrepancy. Do not mark this item clean or PLAN FROZEN while unresolved. This is not authority to modify constitution or to close Gate 1.

### Ledger S4 final Patron disposition — owner decision required

Patron final answer supersedes both earlier no-departure alternatives: **blocked: structural** under charter 2.3(b). Propose narrow constitution amendment v1.13.0 to Principles II and V, adding existing LibraryFileAccessDeniedException to the expected failure list and mapping it to 403 Forbidden in the single GlobalExceptionHandler. Reject route-local subtitle mapping; do not repurpose FeatureDisabledException. Deliberately authorize bounded GlobalExceptionHandler change only after owner-approved amendment; no amendment is applied by this Phase A repair and no owner approval is inferred. All other exception mappings, failure hygiene and pipeline rules stay unchanged.

Required spec PR owner checkbox (unticked):
- [ ] Approve the DEV-454 narrow constitution v1.13.0 amendment to Principles II and V: recognize existing LibraryFileAccessDeniedException as an expected failure and map it to sanitized 403 Forbidden in the single GlobalExceptionHandler, preserving all other exception mappings and the requirement that endpoints do not map exceptions independently.

Record the proposed exact II expected-exception sentence addition and V table-row addition in the spec artifacts; Gate 1 stays closed until the owner explicitly answers the checkbox through the spec PR. A spec PR merge without explicit disposition is not blanket checkbox approval. T000 records both verified merge and explicit amendment approval; pickup verifies the approved constitutional text before implementation of the new mapping. Any implementation requires the approved text and appropriate amendment workflow first. PLAN FROZEN is withheld while this constitution blocker is unresolved; Quill can finish all drafting repairs and the remaining owner decision must be carried to Bernstein for the spec PR. No third challenge round is authorized or implied.

Encoding provider availability remains T001-conditional; do not assert that shared framework supplies it. Control-looking NOTE/STYLE/REGION words within otherwise valid cue payload are not automatically rejected. Existing ADR0037 bounded accuracy update is explicitly in the planned edit list; no new ADR.

### Independent repair reread — round 2 (final allowed round)

Restore existing constitution V contract without asserting current handler implementation: every subtitle error MUST include traceId through the configured ProblemDetails pipeline. T002/T006 must assert this plus generic sanitized 500 no-store on both routes, alongside other frozen error cases. Recon absence of explicit handler assignment is not permission to drop traceId.

For avoidance of ambiguity, only explicit owner approval of the exact amendment text (or an owner-amended version consistently incorporated) PLUS verified merge unblocks pickup. An owner rejection or unanswered checkbox keeps the work blocked even if a generic disposition/merge receipt exists. The approved constitutional text must be applied through the authorized amendment workflow before T001 can verify it and before implementation can rely on it. The spec artifacts propose the text only; no amendment approval or application is claimed here.

Final numbered Quill repair list sent for these three reread defects (traceId restoration, approval-only gate, explicit 500 header/error coverage). Round cap is now reached; any residual blocker after reread is reported to Bernstein, never silently waived or sent for an automatic third round.
