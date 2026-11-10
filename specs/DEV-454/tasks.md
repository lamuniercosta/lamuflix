# Tasks: Play existing sidecar subtitles in the browser

**Input**: `specs/DEV-454/` spec.md, plan.md, grill-closed brief.md (Q1–Q8 + Phase 2 step 6 + Ledger S4 final Patron disposition), CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-454 (supplement:7–22), adjudication-DEV-454-Keel, note DEV-454, specs/PRODUCT.md

**Status**: Phase 0–3 cleared. T000 Gate 1 merge + explicit owner approval recorded (`CONCLUSIONS.md` Phase B T000). T001 cleared with Patron-authorized deferral of executed T001(d) safe-open and T001(g) `traceId` response proofs to Phase 2/3 (`CONCLUSIONS.md` Phase B T001 deferral at `dc0550d7`). Constitution v1.13.0 applied (`ba713407`). Phase 2 (T002–T005) cleared at `77d41dab`. Phase 3 (T006–T009) cleared: content query/handler, WebVTT conversion, decoders, four `LibraryOptions` bounds, post-open `SafeFileHandle` re-verification, thin content route, and the real-filesystem content matrix (including the deferred T001(d)/(g) proofs) are green. Phase 4 (T025) cleared: the four-term `CONTEXT.md` vocabulary append is applied additively with no ADR and no global rewrite. Phase 5 (T010–T011) cleared: the committed `web/src/api/openapi.json` + Verify snapshot were regenerated together via the opt-in LAMUFLIX_REGENERATE_OPENAPI test, `npm run generate:api` regenerated `web/src/api/types.ts` and `web/src/mocks/handlers.ts` for both subtitle operations (no hand edits, movie detail DTO unchanged), and the OpenAPI/inventory tests now expect the two new operations and `SubtitleTrackDto`. PLAN FROZEN / Gate 1 merge history remains as recorded; no claim that later phases, delivery, or merge are complete.

**Gate 1**: Owner structural checkbox (brief Ledger S4 final Patron disposition; charter 2.3(b)) — **explicitly approved** and recorded in T000:

- [x] Approve the DEV-454 narrow constitution v1.13.0 amendment to Principles II and V: recognize existing LibraryFileAccessDeniedException as an expected failure and map it to sanitized 403 Forbidden in the single GlobalExceptionHandler, preserving all other exception mappings and the requirement that endpoints do not map exceptions independently.

Handoff (historical): **Patron provisional Gate 1 → Rigger opens the spec PR carrying the checkbox + proposed II/V text → owner explicitly APPROVES the exact amendment text (or supplies owner-amended text incorporated consistently) → user merges Gate 1 → Keel T000** records verified merge **and** that the checkbox was **approved** (not merely answered) in `specs/DEV-454/CONCLUSIONS.md`. Reject, unanswered, or any non-approval disposition keeps pickup blocked even if a disposition and merge receipt both exist — do not count any answer as approval. A merge without explicit approval is not blanket checkbox approval. **Receipt**: Spec PR #357 `MERGED` at `c0bf581310fdcd0b690f17166e264b89d61bbd9b`; owner checkbox `[x]` recorded in `CONCLUSIONS.md` Phase B T000.

**Proposed constitution amendment v1.13.0 (artifacts only — not applied by Quill)**:
- Principle II sentence addition: `Expected domain failures also include LibraryFileAccessDeniedException for library file policy and containment denial.`
- Principle V table row: `LibraryFileAccessDeniedException` → 403.
- Reject route-local mapping; single GlobalExceptionHandler arm only after **explicit approval** of exact text (or consistently incorporated owner-amended text) plus verified merge.

**Tests**: Required (brief Q8; FR-014). Prefer new-behaviour regressions red before production edits where feasible. Never hand-edit generated OpenAPI/TS. Never invent a Windows-1252 package or substitute viewer/reader. Never lower harness thresholds. Encoding registration remains T001-conditional. Do not blanket-reject NOTE/STYLE/REGION words inside valid cues.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared build outputs (gates) are never `[P]`. Shared `HandlerRegistration.cs` edits serialize across T003/T007.

**Conventions**: Web tests Vitest/RTL; backend `Method_Condition_Expected`; only `// arrange` / `// act` / `// assert` in C# tests; no explanatory code comments; TimeProvider when time is needed; literals used in static contexts are `const`.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` / `web-implement` dispatch. They are **not** canonical `task-pipeline` phases.

---

## Local phase dependency / execution order

```text
Phase 0   T000 (Keel Gate 1 + checkbox disposition record) — blocks T001+; CLEARED
Phase 1   T001 (Wisp drift recon; Keel reanalysis; Gauge baseline; deps + encoding + constitution text; (d)/(g) deferred) — blocks T002+; CLEARED
Phase 2   T002 → T005   (Anvil; US1 list query/handler + port enumeration + DTO/thin route)
Phase 3   T006 → T009   (Anvil; US2 content query/handler + WebVTT + bounds + tokens; T007 owns work; T008 transport)
Phase 4   T025          (Anvil; CONTEXT.md four-term append — C2; after terms introduced; historical ID kept)
Phase 5   T010 → T011   (Anvil/web; OpenAPI/TS regeneration — cross-cutting after US1/US2 routes)
Phase 6   T012 → T015   (web-implement; US3 viewer selector)
Phase 7   T016 → T017   (Anvil/web; US4 real-browser evidence)
Phase 8   T018          (Cog /refactor once)
Phase 9   T019 → T022   (Gauge gates — sequential)
Phase 10  T023          (canonical Phase 4 Size M review)
Phase 11  T024          (canonical Phase 5 delivery)
```

**T025 execution note (Ledger S6)**: Historical task id `T025` is intentionally not renumbered. It executes in local Phase 4 after US1/US2 introduce the four vocabulary terms (after T009) and before OpenAPI regeneration (before T010), so CONTEXT.md reflects terms already present in shipped code paths.

---

## Phase 0: Gate 1 + constitution checkbox approval record (blocks Phase 1)

**Purpose**: Confirm user Gate 1 merge **and** **explicit owner approval** of the exact Ledger S4 constitution amendment text (or consistently incorporated owner-amended text) before any Build work.
**Checkpoint**: Verified merge receipt **and** recorded **approval** of exact amendment text (or consistently incorporated owner-amended text) in CONCLUSIONS.md; or stop. Gate 1 / pickup remains blocked while checkbox is unanswered, rejected, or answered without approval — even if a disposition record and merge receipt both exist. Do not count any answer as approval.

- [x] T000 **Keel** records in `specs/DEV-454/CONCLUSIONS.md`: (1) the verified Gate 1 spec-PR merge receipt for DEV-454, and (2) whether the owner **explicitly approved** the exact constitution v1.13.0 II/V amendment text (or owner-amended text incorporated consistently across artifacts). Receipt only. T001–T025 MUST NOT proceed unless **both** verified merge **and** explicit **approval** are recorded. Reject, unanswered, or any non-approval disposition keeps the item blocked even when (1) and a disposition note both exist — do not treat any answer, or merge alone, as approval (brief Ledger S4; FR-020; charter 2.3(b)). **Cleared**: Spec PR #357 merge `c0bf581310fdcd0b690f17166e264b89d61bbd9b` + explicit owner approval recorded in `CONCLUSIONS.md` Phase B T000.

---

## Phase 1: Pickup — dependency delivery, encoding capability, constitution text, fresh recon (blocks implementation)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main only after T000 recorded **explicit approval** + verified merge; confirm DEV-455 viewer seam and DEV-456 safe-reader/stream surfaces are delivered; confirm Windows-1252 achievable with existing dependencies/BCL when needed; confirm approved constitutional text; confirm registration and drift only. **Patron deferral (`CONCLUSIONS.md` Phase B T001 deferral at `dc0550d7`)**: executed T001(d) safe-open handle/race verification and T001(g) configured ProblemDetails `traceId` response verification are **not** T001 pickup gates — deferred to Phase 2/3 (`T002`/`T005` and `T006`/`T007`/`T009`).
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift/decision/encoding/constitution gap stops — no invention. Baseline captured. Do not start T001 if T000 lacks explicit approval. Cleared T001 does **not** claim that deferred (d)/(g) proofs already passed.

- [x] T001 After T000 (only when T000 recorded **explicit owner approval** of exact amendment text or consistently incorporated owner-amended text **plus** verified merge): **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main for recon-bounded surfaces (`src/LamuFlix.Core/Ports/ILibraryFileReader.cs` AuthorizeFolder/OpenRead and authorized enumeration extension target, `src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs` open-time containment/regular/reparse behaviour, primary-video resolution, `src/LamuFlix.Core/Options/LibraryOptions.cs` + existing options binding/ValidateOnStart wiring, `docs/adr/0037-library-file-reader-port.md`, `ApiEndpoints.cs`, `HandlerRegistration.cs` AddHandler patterns citing `recon-DEV-454` supplement:18–22 and reverify, existing Core `Features/` organisation for colocating sealed subtitle queries/handlers (`src/LamuFlix.Core/Features/Subtitles` placement recorded), `StreamEndpoints.cs` untouched baseline, `GlobalExceptionHandler` / `AddProblemDetails` / configured ProblemDetails writer seam citing supplement:14–16 — note constitution V still requires `traceId` on every error (executed emission proof deferred per (g) below), DEV-455 browser viewer module path under `web/src/features/movie-details` and/or cohesive playback folder, generated `web/src/api/openapi.json` / `types.ts`, existing encoding/code-page registration if any, integration/reader/options test patterns beside `tests/LamuFlix.IntegrationTests/StreamEndpointTests.cs` and existing reader tests, `CONTEXT.md` append location, and verify the approved constitution II/V text is present before any GlobalExceptionHandler 403-arm implementation). Explicit authorized future-delivery prerequisites (not invented contract facts): (a) record verified safe-reader open/authorize APIs and the authorized enumeration extension surface; (b) record verified viewer seam for one subtitle selector; (c) prove Windows-1252 decode / codepage provider need+availability via existing dependencies/BCL without a new package — registration only if proven needed/available, idempotent, no mutable static initialization; do not assert shared-framework availability without proof (**Patron**: net10.0 shared-framework BCL `CodePagesEncodingProvider.Instance`; no NuGet `PackageReference`); (d) **DEFERRED** from read-only T001 pickup to `T007` (bounded `LibraryFileReader.cs` post-open `SafeFileHandle` re-verification) and `T006`/`T009` (executed real-FS adversarial fail-closed containment/replacement tests) — do **not** gate T001 clearance on executed safe-open platform proof (`CONCLUSIONS.md` Phase B T001 deferral; Sentry F3); (e) record exact feature-folder paths for sealed subtitle query/handler placement (`src/LamuFlix.Core/Features/Subtitles`); (f) verify approved constitutional text before mapping implementation (v1.13.0 applied at `ba713407`); (g) **DEFERRED** from read-only T001 pickup to `T002`/`T005` and `T006`/`T009` `SubtitleEndpointTests` positive runtime `traceId` emission proof on 403/404/422/500 — bounded `AddProblemDetails` customization in `Program.cs` authorized during `T004`/`T005` only if `T002` shows `traceId` absent; never claim `traceId` already proven from recon (`CONCLUSIONS.md` Phase B T001 deferral; constitution V). Confirm DEV-455 and DEV-456 are delivered before consumption tasks. **Keel** runs cross-artifact reanalysis of brief/spec/plan/tasks against landed main. Amend tasks only where authorized by demonstrated drift. **STOP** `needs recon` on missing/drifted seams (not on deferred (d)/(g) executed proofs); **STOP** for recon + cited Patron package ruling if Windows-1252 cannot be implemented within existing dependencies/BCL — never silent NuGet/npm add; never invent substitute viewer/unsafe reader; never implement route-local 403 mapping. **Gauge** records pre-edit baseline. No production edits in this task (brief Q5–Q6, Q8; FR-012–FR-013, FR-020; C1; Ledger S1/S4/S5; Sentry F3; Compass F2; constitution V; Patron T001 deferral `dc0550d7`).

---

## Phase 2: List eligible sidecar subtitle tracks (Anvil; Priority: P1 — User Story 1)

**Goal**: Deterministic same-directory discovery via authorized `ILibraryFileReader` enumeration extension + sealed list query/handler + thin list route returning opaque-token `SubtitleTrackDto` array (or `[]`) with frozen labels/sort/security/ProblemDetails.
**Independent Test**: Real-filesystem list tests through the extended dependency safe reader cover the Q1 matrix, token stability on repeated unchanged-set listings, 403/404/`[]`/no-store, `traceId` on every error via configured ProblemDetails writer (**deferred T001(g) proof** — positive runtime emission asserted here and in T005), and **explicit generic sanitized 500** private/no-store+`traceId` assertions — without requiring content conversion UI.
**Order**: T002 → T003 → T004 → T005 (acyclic). Shared `HandlerRegistration.cs` serial with T007.

- [ ] T002 [US1] After T001: Add failing integration coverage under `tests/LamuFlix.IntegrationTests/` (new `SubtitleEndpointTests.cs` alongside `StreamEndpointTests.cs`, or focused sibling named by T001) for `GET /api/movies/{id}/subtitles` proving real-filesystem stem matching for bare/`.en`/`-en`/`.pob`/`-pob`/`.default`/`-default`/unknown suffixes, uppercase recognition, unrelated-file exclusion, no recursive matching, multiple defaults with `isDefault` only on exact default, deterministic sort + duplicate `(2)`/`(3)` labels, `[]` when none, 403 unauthorized (only via single GlobalExceptionHandler after approved amendment — never route-local), 404 unknown movie/missing primary as RFC 7807 `ProblemDetails` via the single `IExceptionHandler` including **`traceId`** (asserted through the configured ProblemDetails writer; **deferred T001(g) proof**; never claim already proven from recon; if absent, authorize bounded `AddProblemDetails` customization in T004/T005), no path/cue leak; frozen statuses, `Cache-Control: private, no-store` on success and error, **explicit generic sanitized 500** `ProblemDetails` case asserting `private, no-store`, `traceId`, nosniff where applicable, and no path/cue leak (403/404 alone do not prove all-errors headers), opaque non-ordinal distinct tokens derived per FR-017, **and token stability across repeated listings of an unchanged movie-sidecar set** — without mocking safe-reader/filesystem translation (FR-001–FR-004, FR-008, FR-014, FR-017; brief:65; Q1, Q3, Q5, Q7–Q8; constitution V; Ledger S1/S2; Patron T001 deferral)
- [ ] T003 [US1] After T001: Extend existing `src/LamuFlix.Core/Ports/ILibraryFileReader.cs` and `src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs` with same-directory regular-file enumeration using existing Infrastructure filesystem abstraction only (no new port/layer/project/package); apply bounded accuracy update to `docs/adr/0037-library-file-reader-port.md`; add *(proposed)* sealed-record list subtitle query carrying `MovieId` + sealed `IQueryHandler` returning sealed immutable track/list results in the T001-verified existing Core feature organisation, plus discovery/token helpers colocated there (BCL SHA-256/base64url only; no new Core references; no Core→Api); derive stem from resolved primary video; apply Q1/Q7 recognition/label/sort/disambiguation; enumerate only eligible same-directory regular files via the extended reader; register with `AddHandler` + established Tracing → Logging → Validation decorators in `src/LamuFlix.Api/HandlerRegistration.cs` (serialize with T007 — do not race same-file edits); include corresponding existing reader unit/integration test updates at T001-verified paths — exact filenames from T001 recon (FR-001–FR-003, FR-012, FR-017, FR-019; Q1, Q5–Q6; Phase 2 step 6; Ledger S1/S6; analyze-round-1 C1; constitution II)
- [ ] T004 [US1] After T002–T003: Implement `SubtitleTrackDto` and thin `GET /api/movies/{id:int}/subtitles` transport in `src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs` *(proposed)* that dispatches the sealed list query handler and maps sealed Core results to DTO (no escaped live streams); OpenAPI Produces metadata; reauthorize via handler path; register via `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs`; map failures only through existing `GlobalExceptionHandler` `ProblemDetails` including constitution-required `traceId` via configured writer (do not add a parallel mapper, route-local 403, or leak `ex.Message`); add `LibraryFileAccessDeniedException` → 403 arm in `src/LamuFlix.Api/ExceptionHandling/GlobalExceptionHandler.cs` **only if** T000 recorded **explicit approval** of exact amendment text (or consistently incorporated owner-amended text) and T001 verified that approved text — otherwise leave mapping unresolved and do not invent FeatureDisabledException misuse; apply `private, no-store` on success and error; do not edit `StreamEndpoints.cs`, `PlaybackEndpoints.cs`, LocalPlay, or Process.Start surfaces; do not change movie detail DTO (FR-004, FR-008, FR-012, FR-020; Q3, Q5–Q6; C1; constitution II/V; Ledger S4; Compass F2)
- [ ] T005 [US1] After T004: Make T002 green against T004 using real filesystem fixtures through the extended dependency safe reader (FR-001–FR-004, FR-014)

---

## Phase 3: Serve converted WebVTT content (Anvil; Priority: P1 — User Story 2)

**Goal**: Content query/handler owns reauthorization, fresh enumeration, token matching, cancellation, disposal, decode, and conversion within LibraryOptions bounds; thin content route owns dispatch/metadata/headers only.
**Independent Test**: Real-filesystem content tests cover encoding/WebVTT/bounds/token/security/adversarial matrices (including **deferred T001(d)** open-time fail-closed / adversarial replacement proofs) plus `traceId` on every error (**deferred T001(g)**) and **explicit generic sanitized 500** private/no-store+`traceId` assertions — without requiring the browser selector.
**Order**: T006 → T009. **Prerequisite**: T004 list/token issuance available for round-trip ids. Shared `HandlerRegistration.cs` serial with T003.

- [X] T006 [US2] After T005: Add failing integration coverage in `tests/LamuFlix.IntegrationTests/SubtitleEndpointTests.cs` (or T001-named sibling) plus options/reader tests at T001-verified paths for `GET /api/movies/{id}/subtitles/{trackId}/content` proving UTF-8 accents ± BOM, Windows-1252 accented fixtures (only if T001 proved capability), newline normalization, empty/malformed/UTF-16/32 → sanitized 422 `ProblemDetails` including **`traceId`** (asserted through configured ProblemDetails writer; **deferred T001(g) proof**; never claim already proven from recon), over-bound → 422 with `traceId`, exactly-at each LibraryOptions bound succeeds / first-over fails, invalid LibraryOptions configuration rejected, cue-boundary fixtures with arrows and NOTE/STYLE/REGION-looking text preserving valid literal words/overlap (no blanket NOTE/STYLE/REGION rejection) while rejecting actually unrepresentable structures as sanitized 422 with `traceId`, valid overlap allowed, negative/reversed intervals → 422 with `traceId`, exact WebVTT `WEBVTT`/timing/literal `&</>` escaping, list/content token round trip, tampered/vanished/unknown/other-movie token → 404 `ProblemDetails` including `traceId`, hash collision fail-closed, unauthorized → 403 (single handler only after approved amendment) including `traceId`, Range → full 200 without Accept-Ranges/206, nosniff + `private, no-store` on success and error + `text/vtt; charset=utf-8`, **explicit generic sanitized 500** `ProblemDetails` case on this content route asserting `private, no-store`, `traceId`, nosniff where applicable, and no path/cue leak (403/404/422 alone do not prove all-errors headers), traversal/symlink/non-regular fail-closed, adversarial check/open replacement with no unauthorized bytes (**deferred T001(d) proof** — executed real-FS fail-closed containment/replacement evidence lives here and in T009), one-bad-track isolation from listing, **and successful content for another eligible track while one track remains bad** — real FS + safe reader only; include diagnostic assertions for MovieId + sanitized categories via TelemetryConstants without new spans/metrics (FR-005–FR-008, FR-014, FR-017–FR-019; brief:124; Q2–Q3, Q5, Q7–Q8; Phase 2 step 6; Sentry F1/F2/F3/F5/F8; Ledger S2/S3; constitution V; Patron T001 deferral)
- [X] T007 [US2] After T005: Add four Range-validated positive bounds on existing `src/LamuFlix.Core/Options/LibraryOptions.cs` (`MaxSubtitleInputBytes=8388608`, `MaxSubtitleCues=100000`, `MaxSubtitleLineCharacters=16384`, `MaxSubtitleOutputBytes=33554432`) with existing options validation/startup wiring at T001-verified paths (no new options record; no hardcoded thresholds; ServiceDefaults `AddLamuFlixOptions` — do not duplicate in `Program.cs`); add *(proposed)* sealed-record content subtitle query carrying `MovieId` + sealed `IQueryHandler` in the T001-verified existing Core feature organisation (`src/LamuFlix.Core/Features/Subtitles`) that **owns** reauthorization, fresh eligible enumeration, token recompute/allowlist match, collision fail-closed, cancellation, stream lifetime/disposal, bounded read, strict-UTF-8-exception-fallback then conditional Windows-1252 decode, and WebVTT conversion helpers colocated there (no new package/project/layer/port; BCL only; no Core→Api; no escaped live streams); return sealed immutable document result; strengthen adapter open-time checks in `LibraryFileReader.cs` with bounded post-open `SafeFileHandle` / `File.GetAttributes(SafeFileHandle)` re-verification (**deferred T001(d) implementation** — prove fail-closed behaviour via T006/T009 real-FS adversarial tests); register with `AddHandler` + established decorators in `HandlerRegistration.cs` (serialize with T003); conditional codepage registration only if T001 proved need+availability, idempotent, no mutable static init (FR-006–FR-007, FR-012–FR-013, FR-017–FR-019; Q2, Q6–Q7; Phase 2 step 6; Compass F1; Ledger S5/S6; C1; constitution II/VII; Patron T001 deferral)
- [X] T008 [US2] After T006–T007: Implement thin content route on `src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs` as **pure transport** — HTTP dispatch, metadata, and headers only — dispatching the sealed content query handler; do **not** re-implement reauthorization/enumeration/token/decode in the route; apply frozen headers (`private, no-store` on success and error; nosniff where applicable), no disk writes, and error mapping only through existing `GlobalExceptionHandler` `ProblemDetails` (403 arm only if amendment approved); keep Stream/local-playback files untouched (FR-005, FR-008, FR-012, FR-019; Q3, Q5–Q6; Compass F1; C1; constitution II/V)
- [X] T009 [US2] After T008: Make T006 green against T008 (FR-005–FR-008, FR-014, FR-017–FR-019)

---

## Phase 4: CONTEXT.md vocabulary append (analyze-round-1 C2)

**Purpose**: Satisfy constitution VIII with the bounded four-term append authorized by Patron; no global rewrite.
**Checkpoint**: Only the four terms present as additive entries; no ADR.
**Execution note**: Historical id T025 runs here (after T009, before T010) without renumbering (Ledger S6).

- [X] T025 After T009: Append only `Sidecar subtitle`, `Subtitle track`, `Default marker`, and `WebVTT` entries to existing `CONTEXT.md` using term, definition, and `_Avoid_:` — no global glossary rewrite; no new ADR; no other CONTEXT edits (analyze-round-1 C2; brief vocabulary/Q6; constitution VIII; FR-016). **Cleared**: four additive entries appended after the existing `Existing track` entry; no other CONTEXT.md edits.

---

## Phase 5: Generated OpenAPI / TypeScript contract (cross-cutting after routes)

**Goal**: Regenerate committed contract artifacts from C# metadata; web consumes generated types only.
**Independent Test**: Existing OpenAPI inventory / `npm run check:api` (or repo equivalent) green; no hand edits.

- [X] T010 After T025: Regenerate contract chain with existing tools (`LAMUFLIX_REGENERATE_OPENAPI` / `npm run generate:api` / `npm run check:api` as repo scripts require) updating `web/src/api/openapi.json` and `web/src/api/types.ts` (+ MSW handlers if scripts require) for both subtitle operations — never hand-edit generated artifacts; do not change movie detail DTO (FR-009; Q3)
- [X] T011 After T010: Update any OpenAPI/inventory tests that list expected operations/routes so the new subtitle operations are included using existing test patterns (FR-009; Q3)

---

## Phase 6: Browser subtitle selector (web-implement; Priority: P1 — User Story 3)

**Goal**: One Off + tracks selector at the verified DEV-455 viewer seam; session-local selection; text-only labels; served ordering; generation guard; list-error distinct from empty; failure handling without stopping video.
**Independent Test**: Viewer-seam Vitest/RTL covers Off/single-track/switch/reset/empty vs list-error/hostile basename/ordering/generation guard/URL cleanup/failure without claiming codec proof.
**Order**: T012 → T015. **Prerequisite**: T010 generated types available.

- [X] T012 [P] [US3] After T010: Add failing Vitest/RTL coverage at the T001-verified viewer seam under `web/src/features/movie-details/` and/or cohesive DEV-455 playback module for: selector hidden on successful `[]`; Off + labels when non-empty with **served ordering** asserted (default-first, labels, duplicate counters); initial Off despite default marker; text-only label rendering including hostile basename (no raw HTML); only selected track `showing`; Off disables all; movie-change reset; no `localStorage`/language auto-select; current movie/selection generation guard suppressing stale list/content success **and** errors; Off/movie-change/unmount invalidate pending work and revoke owned object URLs where created; content-load failure keeps video playing, switches Off, shows `Subtitles unavailable`, retry via reselect; list-load failure keeps video playing, keeps Off, shows `Subtitles unavailable` with explicit Retry, never masquerades as empty success; content URL built from movie id + opaque token using generated types only (FR-010–FR-011, FR-014; Q4, Q7–Q8; Sentry F4/F6/F7; Compass F3; ASSUMPTIONS). **Cleared**: `web/src/features/playback/subtitleSelector.test.tsx` (18 tests) and `web/src/features/movie-details/MovieDetailsView.subtitles.test.tsx` (5 tests) cover every listed case.
- [X] T013 [US3] After T012: Implement the subtitle selector integration at the verified seam (same directories as T012) consuming generated list/content types; hide when successful empty; wire TextTrack modes per FR-010; text-only labels; session-local state + generation guard only (FR-010–FR-012; Q4, Q6). **Cleared**: `SubtitleSelector.tsx`, `subtitleCopy.ts`, `subtitleContent.ts`, `subtitleTrackModes.ts`, `useSubtitlePlayback.ts`; wired into `BrowserPlayer.tsx` at the DEV-455 seam using `SubtitleTrackDto` from generated `types.ts`.
- [X] T014 [US3] After T013: Wire content-load and list-load failure handling to keep video playing, force/keep Off, show `Subtitles unavailable` (with explicit Retry for list failure), allow content retry by reselecting, suppress stale success/error, and clean up owned URLs on Off/movie-change/unmount (FR-011; Q4; Sentry F6/F7; ASSUMPTIONS). **Cleared**: content failure returns to Off with reselect retry; list failure keeps Off with explicit Retry and never hides as empty; movie/list-attempt and selection-attempt generations suppress stale success and failure; `releaseOwnedUrl` on Off, movie change and unmount.
- [X] T015 [US3] After T014: Make T012 green against T013–T014 without requiring LocalPlay-on (FR-010–FR-011). **Cleared**: `scripts/run-web-gates.ps1` PASS (lint, tsc, vitest 741 passed, build); no `Features:LocalPlay` dependency.

---

## Phase 7: Real-browser subtitle acceptance (Priority: P2 — User Story 4)

**Goal**: Record Compose/real-browser evidence for cues/switching/Off/list-or-content failure or Could not run.
**Independent Test**: Runtime evidence file/note with required fields; or Could not run.
**Order**: T016 → T017.

- [ ] T016 [US4] After T015: Provision or identify a playable Compose movie with eligible same-directory SRT sidecars suitable for browser verification (git-ignored/outside-tree media policy as used by sibling playback tickets; never commit unlicensed media; never dirty unrelated tracked placeholders) (FR-014; Q8)
- [ ] T017 [US4] After T016: Execute real-browser Compose verification for SMOKE-001–004 family (visible cues, track switching, Off, content failure and list failure while video continues) and record browser/version, sample identity, Compose URL, and exact status against the delivery SHA; if browser/sample/seam unavailable report **Could not run** (never PASS); unit/jsdom success does not substitute (FR-014; SC-006; Q8)

---

## Phase 8: Refactor (Cog)

- [ ] T018 After T017: **Cog** runs `/refactor` once over completed implementation for this ticket (C# complexity/duplication; web cleanup in-scope of web-implement); fix without expanding scope, editing untouched Stream/local-play files, or lowering thresholds (brief Q8; FR-015).

---

## Phase 9: Verification gates (Gauge) — sequential

- [ ] T019 **Gauge** runs `scripts/run-web-gates.ps1` for DEV-454 `/web` changes (FR-014; plan Gates). Sequential with T020–T022 — not parallel.
- [ ] T020 **Gauge** runs applicable changed-C# Roslyn, cyclomatic (build then refactor threshold from `harness.yml`), and InspectCode scripts for `.cs` files changed; report honest SKIPPED/SKIP/N/A/Could not run — never PASS substitutes (FR-014). After T019.
- [ ] T021 **Gauge** runs affected suites / full `dotnet test` for C# changes; report web test results via web gates; honest non-pass labels (FR-014). After T020.
- [ ] T022 **Gauge** runs property / vulnerable-packages / `dotnet format --verify-no-changes` / mutation per harness applicability for this diff; mutation **SKIPPED** when no eligible production C# under `src/` changed (scope-empty); **N/A** only when every changed project is an explicit configured `gates.mutation.exclusions` entry; **FAILED** when a changed non-excluded project lacks eligible tests — never manufacture PASS (FR-014; Q8; plan Gates). After T021; sequential.

---

## Phase 10: Size M review (canonical Phase 4)

- [ ] T023 **Sentry / Ledger / Compass** three-axis review under brief Q8 closing bar (Critical/High/Medium in-scope block; every Low explicit cited disposition; frozen Q1–Q7 scope; two review/fix rounds then stop for fresh Patron/Conductor ruling). Unresolved blockers return to Bernstein — do not silently extend or clear (Q8; FR-015).

---

## Phase 11: Delivery (canonical Phase 5)

- [ ] T024 Rebase onto latest default branch; re-run required gates on rebased HEAD; open/update PR with merge-bar proof; confirm T025 four-term CONTEXT.md append is present (no global rewrite); confirm ADR 0037 accuracy update present if enumeration shipped (no new ADR); confirm constitution amendment text matches approved disposition before claiming 403 arm compliance; no ADR accept task (Q8; C2; Ledger S1/S4); user merges (PRODUCT §2)

---

## Dependencies & Execution Order

- Phase 0 blocks all Build; requires **explicit owner approval** of exact amendment text (or consistently incorporated owner-amended text) **plus** verified merge — reject/unanswered/non-approval remains blocked even if a disposition and merge receipt both exist. **Cleared** (T000).
- Phase 1 blocks production edits; requires DEV-455/456 delivered seams + encoding capability proof (conditional) + approved constitution text verification when mapping is in scope. **Patron deferral (`dc0550d7`)**: executed safe-open platform proof and ProblemDetails `traceId` writer response verification are **not** Phase 1 clearance gates — deferred to `T007`/`T006`/`T009` (safe-open) and `T002`/`T005`/`T006`/`T009` (`traceId`; bounded writer-config in `T004`/`T005` only if `T002` shows `traceId` absent). **Cleared** (T001).
- Phase 2 (US1) before Phase 3 content token round-trips that depend on list issuance; T003/T007 serialize on `HandlerRegistration.cs`.
- Phase 3 (US2) before Phase 4 CONTEXT.md four-term append (T025) and Phase 5 regeneration that documents both routes.
- Phase 5 before Phase 6 web consumption of generated types.
- Phase 6 (US3) before Phase 7 runtime evidence.
- Phase 8–11 after functional suites exist; gates sequential; review before delivery.

### Parallel opportunities

- T003 is **not** marked parallel with T007 because both edit `HandlerRegistration.cs` (and possibly `LibraryFileReader.cs`); serialize shared registration.
- T007 may proceed after T005 once list tokens exist; keep T006 after T005.
- T012 may start after T010 while unrelated docs are untouched; do not parallelise with same-file viewer edits in T013–T014.
- Gate tasks T019–T022 are never parallel.

### Parallel example: User Story 1

```text
Task: "Failing SubtitleEndpointTests list matrix in tests/LamuFlix.IntegrationTests/SubtitleEndpointTests.cs"
Task: "Extend ILibraryFileReader enumeration + sealed list query/handler in verified Core feature organisation from T001"
```

(Those two may proceed after T001 on different files; T003 still serializes registration with later T007.)

---

## Implementation Strategy

### MVP First (User Stories 1–2)

1. Complete Phase 0–1 (Gate 1 + explicit approval + verified merge + pickup STOP checks)
2. Complete Phase 2–4 (list + content API + CONTEXT.md four-term append)
3. Complete Phase 5 (generated contract)
4. **STOP and VALIDATE**: API matrices green via real FS tests

### Incremental Delivery

1. Add Phase 6 viewer selector → Vitest green
2. Add Phase 7 real-browser receipt or Could not run
3. Refactor → gates → review → delivery

### Notes

- `[P]` tasks = different files, no incomplete deps
- `[Story]` maps to US1–US4 from spec.md
- Proposed paths remain *(proposed)* until T001 verifies seams
- Avoid: vague tasks, stream-exemption claims, editing `StreamEndpoints.cs`, silent packages, fake dependency types, global CONTEXT rewrite, route-local 403, claiming PLAN FROZEN / Gate 1 while Ledger S4 lacks explicit approval, treating reject/unanswered disposition as approval, blanket NOTE/STYLE/REGION rejection, asserting encoding already proven without T001 proof, asserting `traceId` or safe-open already proven from recon/T001 pickup (deferred proofs belong to T002/T005/T006/T007/T009), relying on 403/404/422 alone for all-errors headers
