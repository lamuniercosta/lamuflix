# Implementation Plan: Play existing sidecar subtitles in the browser

**Branch**: `feature/454-spec` | **Date**: 2026-11-09 | **Spec**: `specs/DEV-454/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q8 + Phase 2 step 6 + Ledger S4 final Patron disposition); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-454`, `recon-DEV-454` (supplement:7–22), `adjudication-DEV-454-Keel`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite / propose only; Quill does not edit constitution). Dependencies: DEV-456, DEV-455.

**Status**: Draft after plan-challenge repair round 2 (final allowed). **blocked: structural** (Ledger S4 / charter 2.3(b)). PLAN FROZEN withheld. Gate 1 closed pending **explicit owner approval** of exact amendment text (or consistently incorporated owner-amended text) plus verified merge — reject/unanswered/non-approval disposition does not unblock. Ready for Keel independent reread with structural blocker — not clean/frozen. No implementation clearance.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — behaviour and public contract are ruled in the brief; generation tasks regenerate committed OpenAPI/TS. Quill does not invent DEV-455/456 types absent at recon base.

## Summary

Recognize existing same-directory BS Player–style SRT sidecars for the resolved primary video via an authorized extension of existing `ILibraryFileReader` enumeration, decode UTF-8 and Windows-1252 safely within configured `LibraryOptions` bounds, serve on-request WebVTT through two additive authorized thin Api routes (dispatching sealed subtitle queries/handlers with movie-scoped SHA-256/base64url tokens) and let viewers choose Off or a listed track inside the dependency-verified DEV-455 browser viewer — without touching local launch, StreamEndpoints, schema, or adding packages. LibraryFileAccessDeniedException → 403 remains constitutionally blocked pending owner-approved v1.13.0 II/V amendment.

## Owner structural checkbox (unticked — Gate 1 closed)

Copied verbatim from brief Ledger S4 final Patron disposition:

- [ ] Approve the DEV-454 narrow constitution v1.13.0 amendment to Principles II and V: recognize existing LibraryFileAccessDeniedException as an expected failure and map it to sanitized 403 Forbidden in the single GlobalExceptionHandler, preserving all other exception mappings and the requirement that endpoints do not map exceptions independently.

### Proposed constitution amendment v1.13.0 (artifacts only — not applied)

**Principle II — exact expected-failure sentence addition:**

> Expected domain failures also include `LibraryFileAccessDeniedException` for library file policy and containment denial.

**Principle V — exact table-row addition:**

| Exception | Status |
|---|---|
| `LibraryFileAccessDeniedException` | 403 |

Reject route-local subtitle mapping. Authorize the bounded `GlobalExceptionHandler` arm only after **explicit owner approval** of the exact amendment text (or consistently incorporated owner-amended text) **plus** verified merge. T000 records whether the checkbox was approved (not merely answered) plus verified merge; reject/unanswered/non-approval keeps pickup blocked even if a disposition and merge receipt both exist — do not count any answer as approval. T001 pickup verifies approved constitutional text before implementing the new mapping.

## Technical Context

**Language/Version**: C# 14 / .NET 10 backend; TypeScript + React 19 + Vite in `web/` (`TreatWarningsAsErrors=true`, nullable enabled, CPM).

**Primary Dependencies**: Existing pins only. Extend delivered DEV-456 `ILibraryFileReader` / `LibraryFileReader` (AuthorizeFolder/OpenRead today per `recon-DEV-454` supplement:7–11 — enumeration is an authorized extension, not a present fact) and DEV-455 browser viewer seam verified at Phase B. **No new NuGet/npm dependency or swap** (Q6). Windows-1252 / codepage provider registration only if T001 proves existing dependencies/runtime support and need — never silent package add; never mutable static init in mutated code.

**Storage**: N/A for new schema/persistence. Sidecars remain filesystem files beside the primary video. Opaque track tokens are unkeyed SHA-256(movieId NUL exact-filename) base64url identifiers for an unchanged set without durable rename identity. No disk writes of converted WebVTT. No secret/HMAC token derivation.

**Testing**: xUnit v3 / existing API integration harness alongside `StreamEndpointTests.cs`; real filesystem fixtures through the extended dependency safe reader (no mocked driver behaviour that depends on query translation or serialization); adversarial replacement/open tests; bound exactly-at/over + invalid-config tests; cue-boundary fixtures. Vitest/RTL at the verified viewer seam including hostile basename, served ordering, generation guard, list-error Retry. Real-browser Compose acceptance for visible cues/switching/Off/list-or-content failure. Prefer failing tests before production edits where feasible.

**Target Platform**: Docker Compose local stack (Api + web nginx); desktop browsers for TextTrack / viewer acceptance.

**Project Type**: Ports-and-adapters API with feature-organised Core, explicit handlers + decorators, and SPA feature folders (constitution I–II — not VSA). Subtitle list/content are constitution II use cases: thin Api HTTP transport endpoints dispatch sealed-record subtitle queries (carrying `MovieId`) to sealed `IQueryHandler` implementations registered via `AddHandler` and the established Tracing → Logging → Validation decorator chain in existing feature organisation (analyze-round-1 C1; brief Approach; Compass F1). Content handler owns reauthorization/enumeration/token lookup/cancellation/disposal/decoding; thin route owns dispatch/metadata/headers only. Core returns sealed immutable results; Api maps to DTO — no Core→Api reference; no escaped live streams. BCL encoding/crypto only within existing Core allowlisted references. Reuse/extend delivered DEV-456 `ILibraryFileReader` after T001 — no new Core port, project, architectural layer, or package. Additive endpoint registration via existing `ApiEndpoints.cs` and handler registration via existing `HandlerRegistration.cs` (serialize T003/T007). UI integrates at `web/src/features/movie-details` and/or cohesive DEV-455 viewer module — exact files from Phase B recon *(proposed until verified)*. Existing stream endpoint evidence is not a Principle II exemption.

**Performance Goals**: Personal-library interactive playback; on-request conversion for selected track content within configured bounds; listing remains available when one track's content fails.

**Constraints**: Charter §2.3 items 1–6 deliberately ruled (Q3/Q5/Q6). Contract chain C# → OpenAPI → generated TS; never hand-edit generated artifacts. Do not change movie detail DTO. Do not edit `StreamEndpoints.cs` or local playback / LocalPlay / Process.Start / secrets surfaces. Never lower harness thresholds. No explanatory comments outside allowed exemptions. Preserve 403/404/422/500 contract. Constitution II/V amendment pending owner checkbox — do not mark compliance PASS for that mapping.

**Scale/Scope**: Size M; additive subtitle API + viewer selector + bounded existing-port/options/ADR0037/handler edits; dependencies DEV-456 and DEV-455.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (worktree current). Ledger S4 is **blocked: structural** under charter 2.3(b). Quill proposes v1.13.0 II/V text in artifacts only; does not edit constitution or close the owner checkbox.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; new port needs ADR; Core reference allowlist | **PASS** — extend existing enumerated `ILibraryFileReader` with same-directory regular-file enumeration in Core port + Infrastructure adapter using existing filesystem abstraction; no new Core port, layer, project, or package (Q5–Q6; Phase 2 step 6; Ledger S1). Bounded accuracy update to existing ADR 0037 only — no new ADR number. BCL encoding/crypto stay within existing Core allowlist (Ledger S6). |
| II (Explicit Handlers / Decorators) | Explicit command/query handlers + AddHandler | **PASS for handlers; BLOCKED for expected-failure list** — list and content are sealed-record subtitle queries with sealed `IQueryHandler`s in existing feature organisation, registered via `AddHandler` + Tracing → Logging → Validation decorators; Api `SubtitleEndpoints` are thin transport only; T007 owns reauth/enumeration/token/cancellation/disposal/decode; T008 transport only (C1; Compass F1). Expected-failure list currently omits `LibraryFileAccessDeniedException`; proposed v1.13.0 sentence addition is recorded above and awaits owner checkbox — **not PASS** for that amendment. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter/sort change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation; no history writes. |
| V (Validated inputs / errors) | Stable public statuses; ProblemDetails including `traceId` | **BLOCKED for LibraryFileAccessDeniedException → 403; PASS otherwise with normative `traceId`** — frozen 403/404/422/500 contracts preserved (Q3); malformed/BOM/over-bound → existing `ValidationException` 422; missing movie/media/track → existing `NotFoundException` 404 (`recon-DEV-454` supplement:14–16). Current GlobalExceptionHandler has no `LibraryFileAccessDeniedException` arm (falls through to 500). Proposed V table row + single-handler arm require **explicit owner approval** of exact amendment text (or consistently incorporated owner-amended text) plus verified merge before implementation; reject/unanswered/non-approval does not unblock. Reject route-local mapping. Endpoints MUST NOT leak `ex.Message`. **Every error response MUST include `traceId`** (constitution V). Recon that the handler does not explicitly set `traceId` (`recon-DEV-454` supplement:14–16) qualifies implementation facts only and does not remove the requirement; assert via the configured ProblemDetails writer; if T001 pickup proves writer configuration missing, add a bounded necessary writer-configuration task — never claim already proven. Error responses include `private, no-store` (and nosniff where applicable), including explicit generic sanitized 500 cases on both subtitle routes. |
| VI (Observability) | Safe diagnostics | **PASS** — queries carry `MovieId`; processing/error diagnostics carry movie id and sanitized categories via `TelemetryConstants`; reuse decorator tracing/logging; no new custom span/metric names; sanitized errors omit paths and cue contents (Ledger S3; Sentry F8). |
| VII (Configuration / LocalPlay / Process.Start) | LocalPlay gated; Process.Start careful; Options | **PASS** — no LocalPlay, Process.Start, playback launch, or secrets changes (Q5–Q6). Four positive Range-validated subtitle bounds added to existing `LibraryOptions` with existing ValidateOnStart wiring — no new options record (Phase 2 step 6; Sentry F1; constitution VII closed options-record list unchanged). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — append only Sidecar subtitle, Subtitle track, Default marker, and WebVTT to existing `CONTEXT.md` (term, definition, `_Avoid_:`) in the introducing implementation PR; no global rewrite; no new ADR (analyze-round-1 C2; brief vocabulary/Q6; constitution VIII). |
| IX (Test Pyramid) | Meaningful layers | **PASS** — real filesystem + extended safe-reader integration proofs including adversarial open; viewer-seam unit/UI tests (text-only, ordering, generation guard, list-error); required real-browser runtime receipts; no serialization/driver mocks for file behaviour (Q8). |
| Tech stack / API docs | OpenAPI chain | **PASS** — regenerate OpenAPI/TS via existing scripts; never hand-edit (Q3). |
| Governance / Static-Analysis Gates | Honest exits | Phase B / Build: report PASS/failure/SKIPPED/SKIP/N/A/Could not run honestly; mutation N/A only for configured exclusions, scope-empty is SKIPPED, changed non-excluded project without eligible tests is FAILED; web gates when `/web` changes; backend gates for actual C# diff; Size M three-axis review; two remediation rounds (Q8). Gate 1 remains closed while Ledger S4 checkbox is open. |

### Complexity Tracking

No unjustified silent constitution violation in the draft. Ledger S4 is an **explicit blocked: structural** owner decision carrying proposed v1.13.0 II/V amendment text and an unticked checkbox — not a Quill workaround. Complexity Tracking otherwise empty. Two additive routes + sealed subtitle queries/handlers + DTO, opaque tokens, encoding policy, viewer selector, existing-port enumeration extension, LibraryOptions bounds, ADR 0037 accuracy update, and bounded CONTEXT.md four-term append are Patron-cited rulings, not silent assumptions.

## Approach

- **Phase A (this draft)**: Quill writes/repairs spec/plan/tasks/checklist/receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon facts and adjudication. Keel runs independent analysis. PLAN FROZEN withheld while Ledger S4 remains unresolved. No Build code/gates here; do not invent missing dependency types or a Windows-1252 package; do not edit constitution.
- **Gate 1 / constitution amendment**: Spec PR carries the unticked owner checkbox and proposed II/V text. Owner must explicitly **approve** the exact amendment text (or supply owner-amended text incorporated consistently). T000 records **approval** (not any disposition) **and** verified merge. Reject, unanswered, or non-approval disposition keeps Gate 1 / pickup blocked even if a disposition and merge receipt both exist — do not count any answer as approval.
- **Phase B pickup (only after explicit owner approval of exact amendment text or consistently incorporated owner-amended text + verified merge + DEV-455/456 delivered)**: Wisp fresh recon against landed main for safe-reader APIs (including authorized enumeration surface), browser viewer subtitle integration seam, `ApiEndpoints`/`HandlerRegistration` registration, `LibraryOptions` binding paths, encoding capability (Windows-1252 via existing BCL/deps — conditional; no shared-framework claim), platform capability for promised safe-open/fail-closed behaviour, configured ProblemDetails writer `traceId` emission on errors (bounded writer-configuration task only if pickup proves missing — never claim already proven; recon non-explicit handler set is not a waiver), approved constitutional text for LibraryFileAccessDeniedException→403, OpenAPI tooling, and drift. Cite `recon-DEV-454` supplement:14–22 for verified GlobalExceptionHandler/AddProblemDetails/AddHandler/Api HandlerRegistration paths and reverify at pickup — do not claim nonexistent present 403 arm. Keel reanalysis on drift. Gauge baseline. **STOP** `needs recon` on absent/drifted seams or missing platform capability; **STOP** for recon + cited Patron ruling if encoding cannot be met without a new package (Q5–Q6; Q8; Ledger S5; Sentry F3).
- **Dependency-ordered implementation (brief Q8 + Phase 2 step 6)**:
  1. Pickup/dependency/Gate 1/constitution-text recon (DEV-455/456 seams + encoding capability + safe-open platform proof + approved II/V text).
  2. Extend `ILibraryFileReader`/`LibraryFileReader` enumeration + strengthen open-time checks; add LibraryOptions bounds; ADR 0037 accuracy update; deterministic discovery/decoder/converter; sealed subtitle queries/handlers (`AddHandler`, serialize shared registration); DTO and thin list/content routes; GlobalExceptionHandler 403 arm only if amendment approved; real filesystem + adversarial tests; append the four CONTEXT.md terms when introduced.
  3. Regenerate and check OpenAPI + TypeScript types.
  4. Integrate the selector at the verified viewer seam (text-only, ordering, generation guard, list-error Retry, URL cleanup).
  5. Complete integration tests and real-browser acceptance.
  6. Run applicable backend verification and web gates with exact status reporting.
- **Refactor / ship**: Per task-pipeline after Build; web gates + applicable C# gates; Size M three-axis review under Q8 bar.

## Project Structure

Surfaces grounded in `brief.md` Q6 bounded edits, Phase 2 step 6, and `recon-DEV-454` supplement:7–22. Proposed paths stay inside existing folders and are marked *(proposed)*. Exact helper filenames and viewer module paths come from Phase B pickup — do not invent an architectural layer.

```text
specs/DEV-454/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md

src/LamuFlix.Core/
  Ports/ILibraryFileReader.cs                 (extend: same-directory regular-file enumeration — authorized)
  Options/LibraryOptions.cs                   (add four Range-validated subtitle bounds — authorized)
  Features/<existing-or-T001-verified feature folder>/   *(proposed)*
    sealed subtitle list/content query records (carry MovieId) + sealed IQueryHandler implementations
    sealed immutable track/list/content result records
    (optional discovery/decode/WebVTT/token helpers colocated — BCL only; no new layer; no Core→Api)

src/LamuFlix.Infrastructure/
  FileSystem/LibraryFileReader.cs             (enumeration + open-time containment/regular/reparse; adapter owns safe open)
  Pipeline/ServiceCollectionExtensions.cs     (existing AddHandler — cite recon supplement:18–22; reverify at pickup)

src/LamuFlix.Api/
  Endpoints/ApiEndpoints.cs                   (register MapSubtitleEndpoints — Q6)
  Endpoints/StreamEndpoints.cs                (untouched absent new Patron ruling)
  Endpoints/PlaybackEndpoints.cs              (untouched)
  Endpoints/SubtitleEndpoints.cs              *(proposed)* thin list + content HTTP transport only
  Endpoints/SubtitleTrackDto.cs               *(proposed)* public DTO beside endpoint DTOs
  HandlerRegistration.cs                      (AddHandler for subtitle query handlers — serialize T003/T007)
  ExceptionHandling/GlobalExceptionHandler.cs (single IExceptionHandler; LibraryFileAccessDeniedException→403 arm ONLY after owner-approved constitution amendment)

docs/adr/0037-library-file-reader-port.md     (bounded accuracy update naming enumeration; no new ADR)

CONTEXT.md                                    (append only the four analyze-round-1 terms — C2; no global rewrite)

tests/LamuFlix.IntegrationTests/
  StreamEndpointTests.cs                      (existing; do not rewrite wholesale)
  SubtitleEndpointTests.cs                    *(proposed)* alongside stream suite (Q6)
  (+ existing LibraryFileReader / LibraryOptions tests at T001-verified paths)

web/
  src/api/openapi.json / types.ts             (generated only)
  src/features/movie-details/…                and/or cohesive DEV-455 viewer module *(verified at Phase B)*
  (viewer-seam subtitle selector + tests)     *(proposed at verified seam)*
  e2e/                                        (existing harness; runtime evidence support)
```

**Layout decisions**:

- No research/data-model/contracts folders — decisions frozen in brief Q1–Q8 / Phase 2 step 6.
- Quill owns spec/plan/tasks/receipt/checklist. Quill does not edit brief/CONCLUSIONS/ASSUMPTIONS/constitution/production code.
- Stream bytes remain DEV-456; browser player shell remains DEV-455; this ticket adds subtitle list/content queries/handlers + thin routes + selector + authorized existing-port/options/ADR0037 updates only.
- Keep local playback files and `StreamEndpoints.cs` untouched; stream recon evidence is not a constitution II exemption.
- Exact Core feature-folder and helper filenames remain T001 recon — Quill does not invent delivery paths.
- Existing reader has no enumeration today (`recon-DEV-454` supplement:7–11); do not call this reuse-only fiction.

## Test Strategy

| Family | Required evidence |
|---|---|
| Discovery / labels | All Q1 filename cases, uppercase, `-pob`, unrelated files, no recursion, unknown suffixes, multiple defaults, deterministic duplicate labels (Q8) |
| Decode / WebVTT | UTF-8 accents ± BOM, Windows-1252 accented fixtures, newline variants, empty/malformed, UTF-16/32 rejection, valid overlap + invalid intervals, cue-boundary fixtures (arrows / NOTE/STYLE/REGION-looking words preserved when valid; reject only unrepresentable structure), exact timing + literal markup (Q8; Sentry F5) |
| Bounds | Exactly-at and first-over for each LibraryOptions subtitle limit; invalid configuration rejected at startup/validation; over-bound → sanitized 422 (Sentry F1) |
| Real FS / reader | Discovery/reads through extended dependency safe reader; adversarial replacement/open fail-closed; no mocked driver translation/serialization substitutes (Q8; Sentry F3) |
| API / contract | `[]` listing, DTO + generated contract, frozen 403/404/422/500 as RFC 7807 `ProblemDetails` via the single `IExceptionHandler`, **every error MUST include `traceId`** (assert through configured ProblemDetails writer; recon non-explicit handler set is not a requirement waiver; bounded writer-config task only if pickup proves missing — never claim already proven), `private, no-store` on success **and** error including **explicit generic sanitized 500** cases on **both** subtitle routes (403/404/422 alone does not prove all-errors headers), nosniff where applicable, Range→full 200, sanitized errors without path/cue leak (Q8; Sentry F8; constitution V) |
| Tokens / isolation | Opaque non-ordinal distinct tokens via movieId NUL filename SHA-256 base64url; stability across repeated unchanged-set listings and list/content round trip; fresh allowlist before open; collision fail-closed; tampered/other-movie/vanished 404; no secret or token-to-path decode; one-bad-track isolation from listing **and** successful other-track content (brief:65,124; Sentry F2; Ledger S2) |
| Security | Token/movie binding, unauthorized access, traversal, symlink escape, non-regular files, stale/vanished track, adversarial open (Q8; Sentry F3) |
| Diagnostics | MovieId on queries; sanitized categories via TelemetryConstants; no new spans/metrics; assertions (Ledger S3) |
| Viewer | Off initially, one showing track, switching/Off, movie-change reset, empty hide vs list-error Retry, text-only hostile basename, served ordering, generation-guard stale success/error suppression, owned-URL cleanup, failure + retry without interrupted video (Q8; Sentry F4/F6/F7; Compass F3) |
| Runtime browser | Visible cues, switching, Off, list/content failure while video continues — unit tests do not replace (Q8) |
| Gates | Applicable backend verify + `scripts/run-web-gates.ps1` when `/web` changes; honest non-pass labels |

## Gates (after Gate 1 and implementation — Gauge owns configured Phase 3 gates)

Gates run **sequentially** (shared worktree outputs; no parallel gate scripts). Gate 1 remains closed while the Ledger S4 owner checkbox is unticked / unanswered.

1. Web: `scripts/run-web-gates.ps1` when `/web` changed.
2. Changed-C# scope: Roslyn, cyclomatic (+ refactor threshold from `harness.yml`), InspectCode.
3. Affected suites then full `dotnet test` when C# changed; Vitest via web gates.
4. Property / vulnerable-packages / format per harness applicability; honest SKIPPED/SKIP/N/A/Could not run.
5. Mutation exits stay honest for the actual diff: **SKIPPED** when no eligible production C# under `src/` changed (scope-empty); **N/A** only when every changed project is an explicit configured `gates.mutation.exclusions` entry; **FAILED** when a changed non-excluded project has no eligible test project — never manufacture PASS from missing eligibility.
6. Size M three-axis review (Sentry/Ledger/Compass) under Q8 closing bar; max two remediation rounds; unresolved blockers stop for Patron/Conductor ruling.
7. Rebased gates before awaiting-merge; user merges.

Phase A runs none of these and claims no gate result. Backend mutation applicability is determined by the actual implementation diff, not inferred from this docs change. PLAN FROZEN is withheld while Ledger S4 remains unresolved.

## Risks

- **Constitutional 403 mapping blocked**: Ledger S4 owner checkbox open — mitigated by carrying exact proposed II/V text + unticked checkbox; no route-local workaround; T000/T001 gate the mapping on **explicit approval** of exact text (or consistently incorporated owner-amended text) plus verified merge only; reject/unanswered/non-approval remains blocked.
- **`traceId` on errors**: Constitution V requires `traceId` on every error; recon shows handler does not explicitly set it — mitigated by normative FR-008/assertions through ProblemDetails writer and conditional bounded writer-config task only if pickup proves missing.
- **Dependency not landed**: DEV-455/456 are dependencies, not verified at grill recon base — mitigated by Q5/Q8 Phase A/B split; pickup recon; stop on drift.
- **Encoding support**: Windows-1252 unverified — mitigated by Phase B conditional capability check; stop + Patron ruling before any package; no mutable static init (Q6; Ledger S5).
- **Enumeration / TOCTOU**: Existing reader has no enumeration and does not prove race-safe open — mitigated by authorized extension, T001 platform capability STOP, adapter-owned safe open, adversarial real-FS tests (Ledger S1; Sentry F3).
- **Path/token confusion**: Mitigated by reauthorization, fresh allowlist token match, no client paths / no token-to-path decode, sanitized errors (Q5; Sentry F2).
- **Auto-enable temptation**: Mitigated by explicit Off-initial policy even with default markers (Q4).
- **Stale UI races / list-error confusion**: Mitigated by generation guard, URL cleanup, distinct list-error Retry vs empty hide (Sentry F6/F7).
- **Editing stream/local play**: Mitigated by Q6 untouched-file rule.
- **Runtime evidence gaps**: Mitigated by Could not run ≠ PASS (Q8).

## Ordering

Matches brief Q8: recon → discovery/decoder/sealed queries+handlers/DTO/thin routes (with port/options/ADR0037 updates) → CONTEXT.md four-term append → OpenAPI/types → viewer selector → integration + browser acceptance → gates. US1/US2 before US3; US4 last. Shared `HandlerRegistration.cs` edits serialize across T003/T007. **T025 execution note**: historical ID `T025` runs in local Phase 4 after US1/US2 introduce vocabulary terms and before OpenAPI regeneration (T010); numbering is preserved rather than renumbered (Ledger S6).

## ADR / glossary status

No new ADR and no global CONTEXT.md rewrite (Q8). Bounded accuracy update to existing `docs/adr/0037-library-file-reader-port.md` is authorized so its Decision names the added same-directory regular-file enumeration signature alongside AuthorizeFolder/OpenRead (Phase 2 step 6; Ledger S1). Analyze-round-1 C2: append only Sidecar subtitle, Subtitle track, Default marker, and WebVTT to existing `CONTEXT.md` (term, definition, `_Avoid_:`) in the introducing implementation PR (constitution VIII).
