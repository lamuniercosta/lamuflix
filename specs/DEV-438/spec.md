# Feature Specification: Rescan replaced movie files and resolve primary video at play time

**Feature Branch**: `feature/438-spec`
**Created**: 2026-11-04
**Status**: gate1: provisional
**Input**: Ticket DEV-438 (parent DEV-100); frozen `brief.md` (Q1–Q8 + Phase 2 adjudication addendum); `CONCLUSIONS.md` (Q1–Q8 + Phase 2 rulings 1–8); `ASSUMPTIONS.md`; Maestri notes `DEV-438`, `recon-DEV-438`, `recon-DEV-438-2`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` v1.10.0 (cite only)

## Owner question — Gate 1

None. Brief Q1–Q8 and `CONCLUSIONS.md` (including Phase 2 rulings) record no ticket contradiction and no constitution departure; zero structural owner checkboxes (`brief.md` Outcome / Q8 / Phase 2 addendum; `PRODUCT.md` §5). Ordinary owner Gate 1 on the spec PR remains pending. Patron approval is not owner approval.

## Scope

**In scope** (brief Frozen scope; Q1–Q7; Phase 2 addendum; ticket ACs via Patron rulings):

1. **One shared primary-video selector** used by import, rescan, and play: top-level files only (never nested); existing case-insensitive extension membership in `SupportedVideoExtensions` (`.mkv`, `.mp4`, `.avi`, `.m4v`, `.rmvb`, `.rm`, `.flv` — do not fork the list); exclude a file when any ASCII-alphanumeric token of its extensionless basename equals `sample`, `trailer`, `featurette`, or `extras` case-insensitively (non-ASCII-alphanumeric characters are separators — [assumed] Q2); choose greatest byte length, then ordinal-lowest full filename (name including extension, not directory path). Only-excluded, unsupported-only, empty, and nested-only folders have no eligible video. If the folder exists and is listable, any unexpected per-candidate length/access/IO/permission failure while enumerating or reading candidates propagates as generic `500` on import, per-movie rescan, and play, and aborts bulk while retaining prior commits — never silently skip an unreadable supported candidate to pick a sibling (Phase 2 ruling 1).
2. **Per-movie Format-only rescan**: bodyless `POST /api/movies/{id}/rescan` → 200 JSON `{ movieId, changed, oldFormat, newFormat }`; loads the existing movie, re-probes its folder with the shared rule, updates only Format via `Movie.RefreshFormat(MediaFormat)`; never inserts; never changes identity, folder path, title, year, metadata, enrichment state, or user flags; never re-parses title/year or enqueues enrichment; never launches a process; not gated by `Features:LocalPlay`. Request-aborted `CancellationToken` flows through the endpoint, decorators, handler, repository save; cooperative check before synchronous resolve and before save (Phase 2 ruling 8).
3. **Synchronous snapshot bulk rescan**: bodyless `POST /api/movies/rescan` → 200 JSON `{ changed, unchanged, failed, failedMovieIds }`; snapshot existing scalar ascending `MovieId`s once via `IMovieRepository` (no entity preload), process sequentially ascending with one current first `GetAsync` per unique ID in the fresh request scope, load current row and probe current disk per ID, save only changed Format per item independently via existing `Baseline`/`From`/`Apply` Format diff (never full-graph `Update`); expected missing-folder / no-video / deleted-snapshot-row failures count failed and continue; **any** unexpected failure aborts with prior commits kept; cancellation propagates without a success summary and is never counted failed; IDs inserted after the snapshot wait for a later request; empty library returns zero counts and empty ID array; each snapshot ID contributes exactly once; `failedMovieIds` ascending ([assumed] Q4); IDs use the existing `MovieId` wire representation; no worker/outbox/queue; no global transaction or vacuum (Q5; Phase 2 rulings 5, 8).
4. **Play-time file resolution**: existing `POST /api/movies/{id}/play` stays 204; order is lookup → `EnsureEnabled` → resolve primary file → launch resolved path + actual extension → history only after successful launch; stored Format is not used for player selection and is not repaired by play; known disabled → 403 before disk probe; unknown id → 404; missing folder / no eligible video → mapped failures without history; launcher-reported resolve-to-launch failure remains unexpected generic `500` with zero history and zero Format write, no retries or new taxonomy — successful process start does not guarantee the external player opened a disappeared media argument; keep configured player mappings, `ProcessStartInfo.ArgumentList`, and existing `Process.Start` mechanism; request-aborted token flows with cooperative checks before resolve/launch and before history (Phase 2 rulings 2, 8).
5. **Narrow existing-port / wiring / glossary / generated-contract alignment**: extend existing `IMediaLibraryScanner.ResolvePrimaryVideo(LibraryPath)` returning sealed `PrimaryVideo(LibraryPath Path, MediaFormat Format)` in `src/LamuFlix.Core/Ports/PrimaryVideo.cs`; `Scan` and `ResolvePrimaryVideo` share the same selector and translate only expected missing-folder / no-eligible (including only-excluded) outcomes to existing `NotFoundException`/`InvalidTransitionException` — explicitly changing import's current unmapped generic `500` to `404`/`409` (narrow forced `ImportEndpoints` OpenAPI metadata + import/scanner tests; no global remapping; no duplicate-import fix; recursive discovery missing-root untouched); extend existing `IMediaPlayerLauncher.EnsureEnabled()`; extend `IMovieRepository` / `EfMovieRepository` Baseline/From/Apply/refresh/rebuild for Format and scalar ID snapshot; both decorated rescan handlers invoke one `internal sealed class RescanMovieOperation` in Library (`IMovieRepository` + `IMediaLibraryScanner` only); register via existing `AddHandler` / decorators; `MapRescanEndpoints` + DTOs `RescanMovieResponse` / `RescanLibraryResponse`; C# DTO → `LAMUFLIX_REGENERATE_OPENAPI` opt-in export (baseline + Verify snapshot together) → `npm run generate:api` (`types.ts` + `src/mocks/handlers.ts`) verified by `npm run check:api`; align `CONTEXT.md` after recon — **Rescan** updates an existing Movie's Format whereas **Import** creates a Movie; no frontend feature/UI work (Phase 2 rulings 4–7, 8; brief addendum).
6. **Approved verification**: unit (MockFileSystem / Shouldly / NSubstitute / AutoFixture / Faker.Net / Theory/MemberData + deterministic-selection property coverage in approved suites), real Postgres Testcontainers (no EF mock / InMemory) including concurrent enrichment-owned column survival and no-UPDATE when unchanged, endpoint/host tests (ordinary success for both rescan routes — different segment counts, no routing workaround), mandatory real-disk import → swap → rescan → play with unique temporary directory + guaranteed cleanup and fake process starter only; full delivery gates from `harness.yml` without lowering thresholds (Q7; Phase 2 rulings 5, 8, L3–L4).

**Out of scope** (brief Frozen scope / Excluded; Q1–Q8; Phase 2 Low dispositions):

- UI / actions / admin screen; duplicate-import 409 fix or import upsert; recursive discovery / bulk import changes (DEV-333); conversion / transcoding / codec / resolution / duration probing; schema / migration / file-fingerprint changes; deleted-title flags / deletion; worker / outbox / vacuum / progress / deadline additions; new Core port / layer / project / package / composite wrapper; play-time Format persistence; broad rewrites / deletions; new ADR or constitution amendment; repair/flag/migration for legacy sample-only catalog rows (accepted: now no eligible video ⇒ 409 on rescan/play, failed on bulk).

**Precedence**: Ticket Scope & Technical Design is already approved authority (`specs/PRODUCT.md` §3). Brief Q1–Q8, Phase 2 addendum/freeze decision, and `CONCLUSIONS.md` govern boundaries, contracts, ordering, and loop discipline. Planning freeze is recorded in plan.md; owner Gate 1 and implementation clearance remain pending. Preserve the pre-existing `harness.yml` modification reported by recon; do not stage it with this task.

## Domain vocabulary *(mandatory — glossary terms for CONTEXT.md at delivery)*

Align with root `CONTEXT.md` in the delivery PR after recon of its existing entries; keep glossary definitions free of implementation detail (brief Q2 / Phase 2 addendum; Principle VIII; [assumed] Phase 2 naming). Definitions ready to feed glossary:

**Primary video**
The one eligible file selected from a movie folder by the shared selection rule (top-level supported extensions, token exclusions, largest then ordinal-lowest full filename).
_Avoid_: main file, biggest file, scanned file (when meaning the selection outcome)

**Rescan**
Re-probe an existing movie folder and update only the stored Format of that existing Movie through a Movie domain method, preserving identity, folder path, title, year, metadata, enrichment state, and user flags. Distinct from Import: Rescan updates Format on an existing Movie; Import creates a Movie.
_Avoid_: re-import, refresh metadata, upsert import

Consumed vocabulary (already in use; not redefined here): **LibraryPath**, **MediaFormat**, **Movie**, **IMediaLibraryScanner**, **IMediaPlayerLauncher**, **Features:LocalPlay**, **MovieId**.

## What the User Will See *(mandatory)*

Nothing visible in the Compose-stack SPA: DEV-438 is API-only (brief Q1); no Web UI, movie-details action, or admin bulk trigger ships in this ticket. API clients and LocalPlay callers that already use the existing play route observe corrected file-at-play behavior; Format refresh requires an explicit rescan call.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Shared primary-video selector and Format mutation (Priority: P1)

Import, rescan, and play all choose the same top-level eligible video. The Infrastructure scanner owns one selector reused by `Scan` and by `ResolvePrimaryVideo`. Token exclusions remove sample/trailer/featurette/extras whole-token basenames. Unexpected candidate length/access/IO failures propagate generic `500` with no silent sibling fallback. `Movie.RefreshFormat` changes only Format. Import's previously unmapped missing-folder / no-eligible (including only-excluded) outcomes become adapter `NotFoundException`/`InvalidTransitionException` → `404`/`409`.

**Why this priority**: Ticket Primary-video rule AC and Proposed scope §1; shared rule is the foundation for all later stories (brief Q2–Q3, Q6 step 1; Phase 2 rulings 1, 6).

**Independent Test**: MockFileSystem selector cases for extensions/case/size/tie/token-boundary/subfolder/only-excluded plus explicit unit fault case for unreadable-candidate IO; import-shared-rule regression including 404/409 (only-excluded); `RefreshFormat` preserves other fields; Theory/MemberData + AutoFixture/Faker.Net; deterministic selection under permuted listing property coverage in approved suites.

**Acceptance Scenarios**:

1. **Given** a folder with `movie.mkv` (large), `movie-sample.mkv` (small), `Sample/sample.mkv`, and `trailer.mp4`, **When** the shared selector runs, **Then** it picks `movie.mkv` (subfolders ignored; token-excluded names ineligible).
2. **Given** a folder holding only excluded files (or only unsupported / empty / nested-only), **When** selection runs, **Then** there is no eligible video.
3. **Given** `movie-sample` or `Movie.Sample` basename tokens, **When** tokens are evaluated, **Then** the file is excluded; `sampler` and `extraspecial` are not excluded ([assumed] separator rule).
4. **Given** an existing Movie, **When** `RefreshFormat` is called with a new format, **Then** only Format changes; id, path, title, year, metadata, enrichment state, and user flags remain.
5. **Given** a listable folder where a supported candidate throws unexpected length/access/IO while siblings remain readable, **When** `Scan` or `ResolvePrimaryVideo` runs, **Then** the failure propagates as unexpected generic `500` and no alternate candidate is selected.
6. **Given** import against a missing folder or only-excluded / no-eligible folder, **When** `POST /api/movies/import` runs, **Then** the response is `404` or `409` ProblemDetails respectively (not the prior unmapped generic `500`).

---

### User Story 2 - Per-movie Format-only rescan (Priority: P1)

An API client posts bodyless `POST /api/movies/{id}/rescan` for an already-imported movie whose on-disk primary file was replaced (for example avi → mkv). The handler re-probes the folder, updates Format only when the extension changes, and returns whether Format changed with old/new values. Same-extension replacement is unchanged even if contents/size differ. Missing folder and no-eligible-video failures are mapped; the row is never mutated on those failures; rescan is not gated by LocalPlay. Request-aborted cancellation checked before resolve/save prevents those side effects; in-flight save cancellation propagates with no success or rollback promise. Rescan neither re-parses title/year nor enqueues enrichment.

**Why this priority**: Ticket Replaced-file and Error-responses ACs; Proposed scope §2 (brief Q1, Q4; Phase 2 ruling 8).

**Independent Test**: Unit handler cases for changed/unchanged/unknown/missing/no-video/pre-save cancellation/in-flight save cancellation; Postgres Testcontainers avi→mkv updates exactly one existing row with preserved fields plus concurrent enrichment-owned column write between load/save survives while changed Format persists and unchanged rescan issues no UPDATE; endpoint contract tests for 200/404/409/422/400 and safe ProblemDetails + traceId; `HttpContext.RequestAborted` wired through endpoint → decorators → handler → repository.

**Acceptance Scenarios**:

1. **Given** a movie imported from a folder holding `Heat.avi` later replaced so the folder holds only `Heat.mkv`, **When** per-movie rescan succeeds, **Then** Format is `mkv`, `changed` is true, old/new formats are returned lowercase without dot, and there is still exactly one row for that LibraryPath with id/title/year/metadata/enrichment/user flags unchanged.
2. **Given** a same-extension file replacement, **When** rescan runs, **Then** the operation succeeds with `changed` false and no save.
3. **Given** an unknown id, **When** rescan is called, **Then** `NotFoundException` → 404 and no mutation.
4. **Given** a known movie whose folder is missing, **When** rescan is called, **Then** `NotFoundException` → 404 and no mutation.
5. **Given** a known movie whose folder exists but has no eligible video, **When** rescan is called, **Then** `InvalidTransitionException` → 409 and no mutation.
6. **Given** LocalPlay disabled, **When** rescan is called for a known movie, **Then** rescan proceeds ungated (not 403).
7. **Given** cancellation observed before save, **When** per-movie rescan is aborted, **Then** no Format save occurs and cancellation propagates without a success body.
8. **Given** cancellation during an in-flight save, **When** the token cancels, **Then** cancellation propagates with no success response and no rollback promise.

---

### User Story 3 - Synchronous snapshot bulk rescan (Priority: P1)

An API client posts bodyless `POST /api/movies/rescan`. The handler snapshots existing scalar movie IDs once (ascending, non-tracking), processes them sequentially in ascending order with one fresh-scope first `GetAsync` each, and returns aggregate changed/unchanged/failed counts plus ascending `failedMovieIds`. Expected item failures continue; **any** unexpected failure aborts while keeping prior commits; cancellation propagates without converting to failed; new IDs after snapshot wait until the next request; rerun after a successful pass writes nothing. Bounded personal-library tracking is accepted; unexpected save abort prevents reuse of poisoned tracked state. Synchronous bulk may be slow — no progress/worker/deadline guarantee beyond cancellation wiring and cooperative filesystem limits (Q5 retained).

**Why this priority**: Ticket Bulk rescan AC for the in-place swap cohort; brief Q1, Q5; Phase 2 rulings 5, 8.

**Independent Test**: Unit bulk empty/mixed/rerun/deleted-snapshot/unexpected-fault/cancellation (unexpected IO and cancellation not counted failed) and no writes when unchanged; Postgres mixed bulk and idempotent rerun; endpoint aggregate contract with RequestAborted wiring.

**Acceptance Scenarios**:

1. **Given** a mixed library of changed, unchanged, missing-folder, and no-video movies, **When** bulk rescan completes, **Then** counts and `failedMovieIds` account for every snapshotted ID exactly once, and failed IDs are ascending.
2. **Given** an empty library, **When** bulk rescan runs, **Then** all counts are zero and `failedMovieIds` is empty.
3. **Given** a successful bulk pass with no disk changes, **When** bulk rescan runs again, **Then** nothing is written and unchanged covers the snapshot.
4. **Given** a snapshotted ID deleted before load, **When** that item is processed, **Then** it counts failed and later items continue.
5. **Given** any unexpected failure mid-run (including unreadable-candidate IO), **When** the request aborts, **Then** the response is generic 500, prior per-item commits remain, and rerun is safe.
6. **Given** cancellation during bulk, **When** the token cancels, **Then** cancellation propagates with no success summary and is not counted as failed.
7. **Given** movie IDs inserted after the snapshot was taken, **When** the current bulk run finishes, **Then** those IDs are not processed until a later request.

---

### User Story 4 - Gated play-time primary-file resolution (Priority: P1)

Play for a known movie resolves the folder to the current primary video after the LocalPlay capability guard, launches the player with that file path and actual extension, and records history only after successful launch. Stale stored Format cannot select the wrong player. Unknown id is 404 before the gate; known disabled is 403 before any disk probe; missing/no-video failures leave history untouched. Launcher-reported failure after resolve remains unexpected generic `500` with zero history and zero Format write. Pre-launch cancellation prevents launch and history; in-flight history-append cancellation preserves existing append-cancellation semantics (no success, no rollback promise). Playback retains `IMovieCatalog`, `IMediaPlayerLauncher`, `IMediaLibraryScanner`, `IPlaybackHistory`, and `TimeProvider` (Patron assessed dependency-count signal; no composite port).

**Why this priority**: Ticket Play AC and Error-responses AC; brief Q3–Q4; §2.3 LocalPlay / Process.Start ruling; Phase 2 rulings 2–3, 8.

**Independent Test**: Unit order lookup → guard → resolve → launch → clock/history; stale Format ignored; zero probe for disabled known movie; zero launch/history on every earlier failure; explicit unit regression for thrown launch-failure path (500, no history, no Format write); cancellation pre-launch and append-cancellation regressions; endpoint/host tests with folder-valued catalog data + fake process starter; mandatory real-disk import → swap → rescan → play with unique temp directory cleanup and fake starter (no real Process.Start).

**Acceptance Scenarios**:

1. **Given** a known movie whose LibraryPath is a folder containing an eligible video, **When** play succeeds, **Then** the launcher receives the resolved file path and that file's extension, and history is recorded after launch.
2. **Given** a stale stored Format that differs from the on-disk primary extension, **When** play runs, **Then** player selection uses the resolved extension, not the stored Format, and Format is not persisted by play.
3. **Given** an unknown id, **When** play is called, **Then** 404 occurs with no disk probe, no launch, and no history.
4. **Given** a known movie with LocalPlay disabled, **When** play is called, **Then** 403 occurs before disk probe, with no launch and no history (even if the folder is missing).
5. **Given** a known enabled movie with missing folder or no eligible video, **When** play is called, **Then** the mapped failure is returned with no launch and no history.
6. **Given** disabled Launch called directly, **When** Launch runs, **Then** it still throws `FeatureDisabledException`.
7. **Given** `IMediaPlayerLauncher.Launch` throws after a successful resolve, **When** play runs, **Then** the response is generic `500`, history is not appended, and Format is not written — without promising the launcher always detects a disappeared media argument.
8. **Given** cancellation observed before launch, **When** play is aborted, **Then** neither launch nor history occurs.

---

### User Story 5 - Generated contracts and glossary alignment (Priority: P2)

OpenAPI metadata for the new rescan success/problem responses and narrowly forced import 404/409 problem metadata flows through opt-in `LAMUFLIX_REGENERATE_OPENAPI` (writes committed `web/src/api/openapi.json` baseline and Verify snapshot together), then `npm run generate:api` producing `web/src/api/types.ts` and `web/src/mocks/handlers.ts`, verified by `npm run check:api`. Forced contract snapshots/mocks align via that workflow (never hand-edit generated files). `CONTEXT.md` gains Primary video / Rescan glossary alignment (Rescan vs Import contrast) after recon. No frontend feature or UI work. Both rescan routes retain ordinary success/reachability tests (3 vs 4 segments; no routing workaround).

**Why this priority**: Constitution OpenAPI drift / contract chain and Principle VIII (brief Q6 step 5; Phase 2 ruling 8; recon-DEV-438-2 §6).

**Independent Test**: Generated OpenAPI/TS/MSW drift checks green; glossary terms present without implementation detail; no hand-edited generated artifacts.

**Acceptance Scenarios**:

1. **Given** the new rescan endpoints/DTOs and forced import problem metadata, **When** the contract chain regenerates, **Then** OpenAPI, TypeScript, and MSW handlers reflect the approved routes/fields/statuses without hand edits to generated files.
2. **Given** delivery vocabulary work, **When** `CONTEXT.md` is updated after recon, **Then** Primary video and Rescan definitions align with this spec (including Rescan vs Import) and remain free of implementation detail.

### Edge Cases

- Same-extension replacement → successful rescan, `changed` false, no save (Q4).
- No filename, size, mtime, codec, or file-change detection persisted or exposed (Q4).
- Missing/no-video failures never mutate a movie or append playback history (Q4).
- Narrow filesystem-adapter translation only to existing domain exception types; do not globally remap arbitrary `InvalidOperationException` or swallow unexpected failures (Q4; Phase 2 ruling 6).
- Unreadable supported candidate while siblings exist → unexpected `500`; never silent skip/fallback (Phase 2 ruling 1).
- Resolve-to-launch launcher failure → unexpected `500`; zero history; zero Format write; no retries/new taxonomy (Phase 2 ruling 2).
- Legacy sample-only catalog rows under the unified rule → 409 on rescan/play, failed on bulk; no repair/flag/migration (Phase 2 F6).
- Synchronous bulk may be slow at personal-library scale; cancellation wiring only; no progress/worker/deadline guarantee (Phase 2 F7; Q5).
- Single global exception handler emits safe RFC 7807 ProblemDetails with traceId, without raw exception text or machine filesystem paths (Q4).
- Existing 400/422 validation and 403/404 ordering stay intact (Q4).
- Concurrent bulk rescans may observe different disk states; no locking, fingerprints, or whole-library atomicity (Q5).
- Disk can change between probes; no automatic retry/concurrency subsystem (Q5).
- Unexpected 500 / cancellation permit partial progress and safe rerun (Q5).
- Process argument remains a single resolved file via ArgumentList — no shell concatenation or new execution path (Q3).
- Route collision premise rejected: `POST /api/movies/rescan` (3 segments) vs `POST /api/movies/{id}/rescan` (4 segments); ordinary success tests suffice (Phase 2 Compass F3).
- Preserve pre-existing unrelated `harness.yml` modification; do not stage it with this task (brief Status).
- Anything outside frozen scope is a follow-up issue, not a finding in this round (Q8).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST select primary video with one shared top-level token-filtered rule for import, rescan, and play: supported extensions via existing `SupportedVideoExtensions` only; ASCII-alphanumeric basename tokens; exclude whole tokens `sample`/`trailer`/`featurette`/`extras` case-insensitively; largest byte length then ordinal-lowest full filename; subfolders never participate; only-excluded/unsupported-only/empty/nested-only ⇒ no eligible video. When the folder exists and is listable, unexpected per-candidate length/access/IO/permission failures MUST propagate as generic `500` (import/per-movie/play) and abort bulk with prior commits retained; the selector MUST NEVER silently skip an unreadable supported candidate (Q2; Phase 2 ruling 1; ticket Primary-video rule AC).
- **FR-002**: Infrastructure `DirectoryMediaLibraryScanner` MUST own the selector; `Scan` and `ResolvePrimaryVideo` MUST share it; `IMediaLibraryScanner.ResolvePrimaryVideo(LibraryPath)` MUST return sealed `PrimaryVideo(LibraryPath Path, MediaFormat Format)` from `src/LamuFlix.Core/Ports/PrimaryVideo.cs` (Q3; Q6; Phase 2 ruling 7).
- **FR-003**: `Movie.RefreshFormat(MediaFormat)` MUST change only Format; per-item EF changed-property persistence MUST extend existing `EfMovieRepository` `Baseline`/`From`/`Apply` (and baseline refresh/rebuild paths) for Format, issuing Format-only SQL never full-graph `Update`, leaving unrelated stored state intact and keeping metadata-write detection false on format-only change; unchanged results MUST perform no save / no UPDATE; unique LibraryPath index remains; rescan MUST never insert, MUST NOT re-parse title/year, and MUST NOT enqueue enrichment (Q3; Q5; Phase 2 rulings 5, 8; ticket Replaced-file AC).
- **FR-004**: Bodyless `POST /api/movies/{id}/rescan` MUST return 200 `{ movieId, changed, oldFormat, newFormat }` with lowercase extensions without dot; `changed` compares persisted old/new Format only; rescan MUST NOT be gated by LocalPlay and MUST NOT launch a process; request-aborted `CancellationToken` MUST flow through endpoint, decorators, handler, and repository save with cooperative checks before synchronous resolve and before save; pre-save cancellation MUST prevent save; in-flight save cancellation MUST propagate without success or rollback promise (Q4; Phase 2 ruling 8).
- **FR-005**: Bodyless `POST /api/movies/rescan` MUST return 200 `{ changed, unchanged, failed, failedMovieIds }` with integer counts, ascending numeric `failedMovieIds` ([assumed] Q4), empty-library zero/empty semantics, and exactly-once contribution per snapshot ID using the existing `MovieId` wire representation (Q4–Q5; Phase 2 ruling 8).
- **FR-006**: Bulk MUST snapshot scalar ascending `MovieId`s once via existing `IMovieRepository` (`AsNoTracking` scalar projection; no metadata/entity preload; no new scope/factory port), process sequentially ascending, call `GetAsync(id, ct)` once per unique ID for a current first read in the fresh request scope, resolve current disk file, mutate via `RefreshFormat`, and save independently only when changed; IDs inserted after the snapshot MUST wait for a later request; bounded personal-library tracking is accepted; unexpected save failure MUST abort immediately so poisoned tracked state is not reused; no aggregate-wide transaction, vacuum, or new factory/port (Q5; Phase 2 ruling 5).
- **FR-007**: Expected bulk item failures (missing folder, no eligible video, snapshotted row deleted before load) MUST count failed and continue; **any** unexpected failure MUST abort with generic 500 keeping prior commits; CancellationToken MUST propagate without success summary or conversion to failed (cancellation never counted failed); bulk MUST catch only expected load/resolve domain failures, with saves outside that catch so cancellation/IO/DB errors propagate (Q5; Phase 2 rulings 1, 8).
- **FR-008**: Failure taxonomy MUST be: unknown movie → `NotFoundException`/404; missing folder → `NotFoundException`/404; existing folder no eligible video (including only-excluded) → `InvalidTransitionException`/409 on per-movie rescan, play, **and import** (changing import's prior unmapped generic `500` for those expected outcomes); non-positive id → Validation/422; malformed route token → 400; known movie LocalPlay disabled on play → `FeatureDisabledException`/403 before disk probe; unexpected (including unreadable-candidate IO and launcher-reported launch failure) → generic 500. Translate only expected scanner/resolver failures at the filesystem adapter boundary using existing domain exception types — no global remapping and no duplicate-import fix; recursive discovery missing-root behavior is outside this change; narrowly force `ImportEndpoints` OpenAPI problem metadata and existing import/scanner tests (Q4; Phase 2 ruling 6).
- **FR-009**: Single global `IExceptionHandler` MUST emit RFC 7807 ProblemDetails with traceId and caller-safe messages without raw exception text or machine filesystem paths (Q4).
- **FR-010**: Play MUST order lookup → `IMediaPlayerLauncher.EnsureEnabled()` → resolve primary file → `Launch(resolved path, actual extension)` → history after successful launch using existing clock/append behavior; stored Format MUST NOT drive player selection or be repaired by play; enabled EnsureEnabled is no-op; disabled EnsureEnabled and Launch throw `FeatureDisabledException`; launcher-reported failure MUST remain unexpected generic `500` with zero history and zero Format write, no retries or new exception taxonomy, without promising the launcher always detects a disappeared media argument; keep ArgumentList + Process.Start mechanism; no Core Options dependency; request-aborted token MUST flow through endpoints/decorators/handler/catalog/history with cooperative checks before synchronous resolve/launch; pre-launch cancellation MUST prevent launch and history; in-flight history-append cancellation MUST propagate without success or rollback promise while preserving existing append-cancellation regression semantics (Q3; Phase 2 rulings 2, 8).
- **FR-011**: Implementation footprint MUST stay in existing feature/layer folders: Core `Features/Library` for `RescanMovieCommand` / `RescanMovieResult` / `RescanLibraryCommand` / `RescanLibraryResult` / `internal sealed class RescanMovieOperation` (both decorated handlers call it; `IMovieRepository` + `IMediaLibraryScanner` only; no handler-calls-handler / cross-feature reference / new interface/layer); Core Domain `Movie`; Core ports including `PrimaryVideo.cs`; Infrastructure FileSystem / Persistence / Playback adapters; Api `MapRescanEndpoints` + `RescanMovieResponse` / `RescanLibraryResponse` + `HandlerRegistration` / endpoint composition following existing per-feature grouping with features referencing only ports/shared types. Playback handler retains `IMovieCatalog`, `IMediaPlayerLauncher`, `IMediaLibraryScanner`, `IPlaybackHistory`, and `TimeProvider` (Patron assessed; no missing-abstraction workaround). No new layer, port, dependency, schema, root folder, or ADR (Q6; Phase 2 rulings 3–5, 7).
- **FR-012**: Narrow forced edits to unnamed tracked files are authorized under §2.3 item 6 for existing ports, launcher adapters, DI/endpoint wiring, existing tests (including import/scanner), `ImportEndpoints` problem metadata, `CONTEXT.md`, and tooling-regenerated OpenAPI/Verify/TS/MSW only; broad rewrites/deletions remain forbidden; never hand-edit generated files (Q6; Phase 2 ruling 6).
- **FR-013**: Verification MUST include unit selector/import/RefreshFormat/per-movie/bulk/play-order cases (including unreadable-candidate fault, launch-failure regression, cancellation cases, and unexpected IO/cancellation not counted failed on bulk); Postgres Testcontainers Format-only persistence with concurrent enrichment survival and no-UPDATE when unchanged; endpoint/host tests including corrected `PlaybackEndpointTests` with folder-valued catalog data and ordinary success coverage for both rescan routes; mandatory real-disk import → swap → rescan → play with unique temporary directory, guaranteed cleanup, fake process starter, and no real Process.Start; selector matrix via Theory/MemberData with AutoFixture/Faker.Net plus deterministic selection under permuted listing property coverage in approved existing suites; `const` token literals / non-mutable static initialization per `AGENTS.md` Stryker rules (Q7; Phase 2 L3–L4).
- **FR-014**: Delivery gates MUST use harness.yml policy/thresholds only (standalone build, then full unit/integration/architecture/property suites, Roslyn, complexity, InspectCode, vulnerable packages, format, configured mutation, OpenAPI/TS drift via `npm run check:api` / web gates, applicable web checks). Never lower a threshold. scope-empty SKIPPED, opt-out SKIP, and mutation N/A remain distinct non-PASS dispositions. Unrunnable gate ⇒ Could not run, never PASS. Phase A documents require artifact consistency only — no production test execution claimed by this draft (Q7–Q8; Phase 2 Compass F5).
- **FR-015**: Closing bar: Critical/High/Medium block code-review/ship-review; Low requires explicit disposition. Plan challenge and implementation review each cap at two rounds total; unresolved blockers at cap return to Bernstein. Frozen scope as brief Q8; anything else is a follow-up issue, not a finding in this round. Owner Gate 1 remains pending; PLAN FROZEN is recorded in plan.md (Q8; Phase 2 addendum/freeze decision).

### Key Entities

- **Primary video / `PrimaryVideo`**: single eligible file chosen by the shared selector; sealed ports record `(LibraryPath Path, MediaFormat Format)`.
- **RescanMovieCommand / `RescanMovieResult`**: per-movie Format-only rescan.
- **RescanLibraryCommand / `RescanLibraryResult`**: synchronous snapshot bulk rescan aggregate.
- **`RescanMovieOperation`**: internal sealed shared per-movie operation used by both decorated rescan handlers.
- **`RescanMovieResponse` / `RescanLibraryResponse`**: API DTOs for the two rescan routes.
- **Movie.Format / RefreshFormat**: persisted lowercase extension without dot; Format-only domain mutation.
- **IMediaLibraryScanner / DirectoryMediaLibraryScanner**: Scan + ResolvePrimaryVideo sharing one selector.
- **IMediaPlayerLauncher EnsureEnabled / Launch**: capability guard then resolved-file launch under LocalPlay.
- **IMovieRepository ID snapshot**: ascending existing scalar `MovieId` projection for bulk membership.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After avi→mkv in-place replacement and per-movie rescan, Format is `mkv`, exactly one row remains for the LibraryPath, and id/title/year/metadata/enrichment/user flags are unchanged (ticket Replaced-file AC).
- **SC-002**: Shared selector picks `movie.mkv` over sample/trailer/subfolder decoys; only-excluded folders have no eligible video (ticket Primary-video rule AC).
- **SC-003**: Play launches the resolved file path (not the folder) and selects the player from that file's extension even when stored Format is stale (ticket Play AC).
- **SC-004**: Missing folder, no eligible video, and unknown id each return defined problem responses (not 500) on per-movie rescan, play, and import; bulk embeds expected folder/deleted failures in counts/IDs (ticket Error-responses / Bulk ACs; Phase 2 ruling 6).
- **SC-005**: Bulk over a mixed set reports correct changed/unchanged/failed counts; a second unchanged run writes nothing (ticket Bulk rescan AC).
- **SC-006**: Generated OpenAPI/TS/MSW drift checks and applicable delivery gates report honest actual exits; Critical/High/Medium review findings are resolved or escalated under the two-round cap (Q6–Q8).

## Assumptions

- [assumed] Name tokens are ASCII-alphanumeric runs in the extensionless basename, matched case-insensitively; every other character is a separator; only `sample`, `trailer`, `featurette`, and `extras` are excluded (`ASSUMPTIONS.md`; Q2).
- [assumed] `failedMovieIds` are returned in ascending numeric order for predictable output (`ASSUMPTIONS.md`; Q4).
- [assumed] Concrete identifier names `PrimaryVideo`, `RescanMovieResult`, `RescanLibraryResult`, `RescanMovieResponse`, `RescanLibraryResponse`, and `internal sealed class RescanMovieOperation`, plus Rescan-vs-Import glossary contrast (`ASSUMPTIONS.md`; Phase 2 ruling 7).
- Recon baseline HEAD `1617165de14cb4f5a34151b49e7948443d36afb9` is planning authority; Build pickup refreshes facts as needed without inventing decisions (`recon-DEV-438`, `recon-DEV-438-2`).
- Pre-existing `harness.yml` modification is preserved and excluded from this task's staging (brief Status; recon).

## Clarifications

All grill categories were resolved by Patron Q1–Q8 before drafting; Phase 2 adjudication rulings refine those without owner checkboxes. `/speckit-clarify` scan: Functional Scope, Domain/Data, Interaction, NFR, Integration/Dependencies, and Out-of-scope — **Clear**. No `[NEEDS CLARIFICATION]` markers remain. No owner Gate 1 checkbox. Unresolved drafting gaps, if any, are listed only as `needs decision:` lines in `DRAFTING_RECEIPT.md` without guessing.

## Traceability

| Source | Encoded as |
|---|---|
| Ticket ACs / Proposed scope §§1–6 | Scope; US1–US5; FR-001–FR-015; SC-001–SC-006 |
| Q1 delivery boundary | Scope; What the User Will See; US2–US3 |
| Q2 selection + vocabulary | Domain vocabulary; US1; FR-001; Assumptions |
| Q3 seams / LocalPlay / Process.Start | US4; FR-002–FR-003; FR-010–FR-011 |
| Q4 contracts / failures | US2–US4; FR-004–FR-005; FR-008–FR-009 |
| Q5 bulk model | US3; FR-006–FR-007 |
| Q6 footprint / order / §2.3 item 6 | FR-011–FR-012; plan Ordering; tasks phases |
| Q7 tests / gates | FR-013–FR-014; plan Test Strategy / Gates |
| Q8 closing bar / frozen scope | FR-015; Edge Cases; Owner question (none) |
| Phase 2 rulings 1–8 / brief addendum | Scope; US1–US5; FR-001–FR-015; Edge Cases |
| ASSUMPTIONS.md | Assumptions; FR-001; FR-005; FR-011 |
| PRODUCT.md §3–§5 | Precedence; no owner checkbox |
| recon-DEV-438 / recon-DEV-438-2 / note DEV-438 | Paths in plan/tasks; harness.yml preserve note; contract workflow |
