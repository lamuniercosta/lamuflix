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

No new ADR or global glossary change is required by the settled decisions (Q8).

## Frozen scope and loop discipline

Existing same-directory SRT sidecars for the resolved primary video; Q1 suffix recognition and labels; Q2/Q7 UTF-8 and Windows-1252 conversion; Q3 list/content WebVTT contract; Q4 browser selection; Q5 authorization, file safety and dependency preconditions; Q6 bounded edits. Anything else is a follow-up issue, not a finding in this round.

Closing bar for code-review/ship-review: Critical, High and Medium in-scope findings block. Every Low requires an explicit cited disposition; fix when needed to satisfy an approved contract, otherwise explicitly defer or note under charter policy. No silent dismissal or severity waiver. Unnecessary abstractions are Follow-up, never an automatic fix commit. Follow-up filing follows charter policy; otherwise record "noted, no ticket".

Maximum two challenge/review rounds. Remaining blockers after the cap halt to Bernstein/Patron; the cap never converts a blocker to clearance. All three plan-challenge axes are required before plan freeze. Phase A grill completion does not grant later-stage clearance.

## Approach and behavior

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

Current phase: documentation inspection and scoped diff verification only; no production test or analyzer pass is claimed. Later backend changes require applicable harness verification; frontend work follows web-implement and web gates. Backend mutation applicability is determined by the actual implementation diff, not inferred from this docs change. Report PASS, SKIPPED, SKIP, N/A and Could not run distinctly.

Ready for Quill's draft from this brief and full Q&A. No implementation started. No merge authority delegated.
