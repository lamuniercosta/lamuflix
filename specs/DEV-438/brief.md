# DEV-438 — Frozen grill brief

## Status and authority

Grill complete: Patron confirmed shared understanding in Q8 after 8 questions (cap 12). Ready for Quill to draft spec.md, plan.md and tasks.md; Keel does not draft those artifacts. No production changes or implementation authorization. Human Gate 1 remains owner-held; plan challenge/freeze is not claimed.

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-438-spec`; branch: `feature/438-spec`.
- Recon baseline: `1617165de14cb4f5a34151b49e7948443d36afb9`.
- Sources: live `scripts/get-task.ps1 DEV-438`; `DEV-438` and `recon-DEV-438` notes in LamuFlix notes; `specs/PRODUCT.md`; `.specify/memory/constitution.md` v1.10.0. The originally supplied `specs/CONSTITUTION.md` was absent; Bernstein supplied the verified path above.
- Full question/recommendation/answer exchanges and cited bases: `CONCLUSIONS.md` Q1-Q8. Taste assumptions: `ASSUMPTIONS.md`.
- Preserve the pre-existing `harness.yml` modification reported by recon. No commit, PR, merge or tracker mutation is authorized by this grill.

## Outcome and frozen scope

An existing imported movie can refresh its stored Format after an in-place file replacement without creating another row or changing its identity, folder path, title, year, metadata, enrichment state or user flags. Import, rescan and playback choose primary video using the same rule. Play resolves the current file and extension even when stored Format is stale.

Frozen scope: one shared top-level token-filtered selector; Format-only per-movie rescan and synchronous snapshot bulk rescan APIs; defined safe failures; gated file-at-play resolution and existing history ordering; necessary narrow existing-port, adapter, wiring, glossary and generated-contract alignment; approved unit, Testcontainers, endpoint and real-disk verification; anything else is a follow-up issue, not a finding in this round.

Excluded: UI/actions/admin screen; duplicate-import 409 fix or import upsert; recursive discovery/bulk import changes (DEV-333); conversion/transcoding/codec/resolution/duration probing; schema/migration/file-fingerprint changes; deleted-title flags/deletion; worker/outbox/queue additions; new Core port/layer/project/package; play-time persistence; broad rewrites/deletions. No ADR or constitution amendment is needed for the chosen existing-port approach. No ticket contradiction or constitution departure was proposed; zero structural owner checkboxes.

## Settled vocabulary and selection (Q2)

- **Primary video**: the one eligible file selected from a movie folder by the shared rule.
- **Rescan**: re-probe an existing movie folder and update only its stored Format through a Movie domain method.
- Eligible files are directly in the folder, never nested, with the existing case-insensitive extension set: `.mkv`, `.mp4`, `.avi`, `.m4v`, `.rmvb`, `.rm`, `.flv`. Keep membership in existing `SupportedVideoExtensions`; do not fork the list.
- Split the extensionless basename into ASCII-alphanumeric tokens; any other character is a separator. Exclude a file if any token equals `sample`, `trailer`, `featurette`, or `extras`, case-insensitively. `movie-sample` and `Movie.Sample` exclude; `sampler` and `extraspecial` do not. This exact separator rule is recorded [assumed].
- Choose greatest byte length, then ordinal-lowest full filename (name including extension, not directory path). Only-excluded, unsupported-only, empty and nested-only folders have no eligible video.
- Align these terms with `CONTEXT.md` in the delivery PR after recon of its existing entries; keep glossary definitions free of implementation detail. The definitions here and conclusions are ready to feed speckit; no new ADR is required.

## Every grill answer

1. **Q1 — Delivery:** API-only, per-movie plus synchronous bulk rescan; missing folders report-only; no UI, duplicate-import change or play-time write. Explicit rescan persists Format.
2. **Q2 — Rule:** Exact selection, exclusion tokens, only-excluded outcome and vocabulary above; one shared rule for import/rescan/play.
3. **Q3 — Seams/local execution:** Extend existing `IMediaLibraryScanner.ResolvePrimaryVideo(LibraryPath)` with a sealed result record carrying file `LibraryPath` and `MediaFormat`; scanner Scan reuses its selector. Add `Movie.RefreshFormat(MediaFormat)`. Extend existing `IMediaPlayerLauncher.EnsureEnabled()`; enabled adapter is no-op, disabled adapter throws `FeatureDisabledException`, and Launch retains its existing gate. Play order is lookup -> guard -> resolve -> launch -> history. No new port or Core Options dependency.
4. **Q4 — Contracts:** The exact routes, fields and safe failure taxonomy below are approved. Format changes compare old/new extension only. Rescan is independent of LocalPlay. Ascending failed IDs are [assumed].
5. **Q5 — Bulk:** Snapshot existing IDs once, process sequentially ascending, load current row and probe current disk per ID, save only changed Format per item. Expected missing-folder/no-video/deleted-snapshot-row failures continue; unexpected failures abort; cancellation propagates. Prior item commits remain. New IDs wait until the next request. No global transaction or queue.
6. **Q6 — Footprint/order:** Existing feature and layer folders only; narrow forced edits to existing ports, launchers, wiring, tests, glossary and generated contracts approved under care-list item 6. Task order below is frozen.
7. **Q7 — Verification:** Approved unit, real Postgres, endpoint and mandatory real-disk import -> swap -> rescan -> play tests, fake process starter, full delivery gates, no Phase A test execution.
8. **Q8 — Closure:** Critical/High/Medium block; Low requires explicit disposition. Two rounds total per challenge/review loop. Scope frozen; unresolved blockers at cap return to Bernstein. Shared understanding confirmed; ready for Quill, ordinary Gate 1 still pending.

## API contract (Q4-Q5)

| Operation | Success | JSON fields |
|---|---|---|
| Bodyless `POST /api/movies/{id}/rescan` | 200 | `movieId`, `changed`, `oldFormat`, `newFormat` |
| Bodyless `POST /api/movies/rescan` | 200 | `changed`, `unchanged`, `failed`, `failedMovieIds` |
| Existing `POST /api/movies/{id}/play` | 204 | No body |

Formats are lowercase extensions without dot. Per-movie `changed` is boolean; bulk counts are integers; IDs follow the existing movie-ID representation. Same-extension file replacement is unchanged even if contents/size differ. No filename/size/mtime is persisted or exposed by these responses. Empty bulk input returns zero counts and an empty ID array. Each snapshot ID contributes exactly once to a successful summary. `failedMovieIds` are ascending numeric IDs.

| Condition | Per-movie rescan / play | Bulk behavior |
|---|---|---|
| Unknown movie | `NotFoundException` -> 404 | Snapshotted ID deleted before load counts failed |
| Missing folder | `NotFoundException` -> 404 | Count failed, continue |
| Existing folder, no eligible video | `InvalidTransitionException` -> 409 | Count failed, continue |
| Non-positive ID | Validation -> 422 | No ID request parameter |
| Malformed route token | 400 binding error | Not applicable |
| Known movie, LocalPlay disabled | Play -> 403 before disk probe; rescan ungated | Rescan ungated |
| Unexpected IO/permission/database/player configuration error | Generic 500 | Abort request; keep prior commits |
| Cancellation | Propagate cancellation | No success summary; do not count as failed |

Translate only the expected scanner/resolver failures at the filesystem adapter boundary, using existing domain exception types. Do not globally remap arbitrary `InvalidOperationException` or swallow unexpected failures. Single global exception handler emits safe RFC 7807 ProblemDetails with traceId, without raw exception text or machine filesystem paths. Existing 400/422 validation and 403/404 ordering stay intact. Missing/no-video failures never mutate a movie or append playback history.

## Approach, persistence and execution (Q3-Q6)

The scanner adapter owns one selector; Scan uses it for import and ResolvePrimaryVideo exposes only resolved file path/format for existing movies. Rescan does not parse a new title/year or trigger enrichment. `Movie.RefreshFormat` changes only Format; per-item EF changed-property persistence must leave all unrelated stored state intact. Unchanged results perform no save. Existing unique LibraryPath index remains, and rescan never inserts.

Bulk uses an ID snapshot from existing IMovieRepository (extend that interface/adapter as needed). Each item loads the current row; changed items commit independently. Expected item failures do not stop later items. There is no whole-library atomicity, file fingerprint or new locking guarantee. Disk can change between probes; no automatic retry/concurrency subsystem is added. Unexpected failures/cancellation permit partial progress and safe rerun.

Play first resolves the catalog identity (unknown -> 404), then calls EnsureEnabled (known disabled -> 403), then resolves current primary file, passes its path and actual extension to Launch, and records history only after successful launch using existing clock/append behavior. Stored Format is not used for player selection and is not repaired by play. Keep configured player mappings, ProcessStartInfo.ArgumentList and existing Process.Start mechanism; no shell concatenation or new execution path. Disabled Launch still throws even if called directly. Rescan does not launch a process.

## File responsibilities and task ordering (Q6)

1. Shared foundation: `Core/Ports/IMediaLibraryScanner.cs`, sealed resolved-video record in existing ports area; `Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs` and internal selector alongside existing SupportedVideoExtensions; `Core/Domain/Movie.cs` Format mutation. Update scanner/import tests for the intentional token/error changes.
2. Per-movie rescan: sealed command, result and validator/handler in `Core/Features/Library`; `Api/Endpoints/RescanEndpoints` and response DTOs in existing endpoint area; register through existing AddHandler/decorators and endpoint composition. Real-Postgres Format-only persistence verification.
3. Bulk rescan: command/result/validator/handler in same Library feature; existing `Core/Ports/IMovieRepository.cs` and `Infrastructure/Persistence/EfMovieRepository.cs` ID-snapshot projection as needed; bulk endpoint and aggregate tests. No new scope/factory port.
4. Playback: `Core/Features/Playback/PlayMovieCommandHandler.cs`, `Core/Ports/IMediaPlayerLauncher.cs`, enabled/disabled Infrastructure/Playback adapters; wiring in `Api/HandlerRegistration.cs` and existing playback composition as needed. Update playback/guard tests with folder-valued source data.
5. Contract and vocabulary: C# DTO/OpenAPI metadata -> tooling-generated `web/src/api/openapi.json` -> tooling-generated `web/src/api/types.ts`; align forced contract snapshots/mocks through their supported generation workflow, never hand-edit generated files. Align CONTEXT.md after recon. No frontend feature/UI work.
6. Run required verification, review against fixed base/head, then exact-head delivery workflow under Conductor. Quill resolves exact existing composition/test filenames from recon rather than guessing or expanding scope.

## Test strategy and gate expectations (Q7)

- Unit: MockFileSystem selector extension/case/size/tie/token-boundary/subfolder/only-excluded cases and shared import behavior; RefreshFormat preserves other fields; per-movie changed/unchanged/unknown/missing/no-video; bulk empty/mixed/rerun/deleted snapshot/unexpected fault/cancellation and no writes when unchanged.
- Playback: lookup -> guard -> resolver -> launch -> clock/history order; stale Format cannot select wrong player; exact resolved file argument/format; zero probe for disabled known movie and zero launch/history on every earlier failure. Keep success, repeated-play and history completion semantics.
- Postgres Testcontainers: avi -> mkv updates exactly one existing row; id/title/year/metadata/enrichment/user flags remain; no duplicate insert; mixed bulk and rerun persistence. Never mock EF query translation or substitute EF InMemory.
- Endpoint/host: both rescan contracts; corrected PlaybackEndpointTests use stored folder paths and resolved file results; 404/403/409/422/400 failures and safe traceId ProblemDetails. Mandatory real-disk import -> swap -> rescan -> play scenario uses a fake process starter; no real process launch in tests.
- Delivery: build, full unit/integration/architecture/property suites, Roslyn, complexity, InspectCode, vulnerable packages, format, configured mutation, OpenAPI/TypeScript drift and applicable web checks. Use harness.yml for policy/thresholds; never lower a threshold. scope-empty SKIPPED, opt-out SKIP and mutation N/A remain distinct non-PASS dispositions. An unrunnable gate is Could not run, never PASS.
- Phase A documents require artifact consistency/read-back, not production test execution. No delivery gate result is claimed by this brief.

## Loop discipline and handoff (Q8)

Closing bar for code-review/ship-review: Critical, High and Medium block closure. Low requires an explicit disposition and may be deferred as a follow-up. Plan challenge and implementation review each have a cap of **two rounds total** (initial plus one remediation/recheck). Unresolved blockers at cap return to Bernstein; no silent waiver or additional round.

Quill drafts spec/plan/tasks strictly from this brief and CONCLUSIONS.md. Keel then performs read-only speckit-analyze and checks plan/tasks against this brief, one numbered fix list per round. Decision changes must first be recorded here with Patron authority; facts come from recon. Owner Gate 1, any required plan challenge/freeze, and implementation remain pending. No autonomous merge.

## Phase 2 adjudication addendum — 2026-11-04

Authority: Patron's numbered Phase 2 rulings (1)–(8) in `CONCLUSIONS.md`, including the cancellation and route corrections in (8); refreshed `recon-DEV-438` and `recon-DEV-438-2`. These refine Q1–Q8 without a ticket contradiction, constitution departure, or owner structural checkbox. One initial challenge plus one repair/recheck remains the two-round cap; freeze awaits verified repairs.

- Unexpected per-candidate IO/permission failures propagate generic 500; never silently skip a supported candidate. Bulk aborts with prior commits retained. Resolve-to-launch failures reported by the launcher likewise remain generic 500 with no history or Format write and no retry/new translation; successful process start does not guarantee the external player opened the media.
- Playback retains `IMovieCatalog`, `IMediaPlayerLauncher`, `IMediaLibraryScanner`, `IPlaybackHistory`, and `TimeProvider`. Patron assessed the dependency-count signal and found no constitution departure; no composite port or wrapper abstraction is introduced.
- Both decorated rescan handlers call one internal sealed `RescanMovieOperation` in the existing Library feature, depending on `IMovieRepository` and `IMediaLibraryScanner`. No handler-calls-handler or cross-feature reference.
- Snapshot scalar ascending IDs through `IMovieRepository` without preloading entities; each unique ID gets one current first `GetAsync` in the fresh request scope. Bounded tracking at personal-library scale is accepted; unexpected save failure aborts immediately. Extend the repository's existing `Baseline`/`From`/`Apply` Format diff and baseline-refresh/rebuild paths, never full-graph Update. Format-only writes must not trigger metadata writes. Real Postgres tests prove concurrent enrichment-owned state survives a rescan save and unchanged rescans issue no UPDATE.
- `Scan` and `ResolvePrimaryVideo` both translate only expected missing-folder/no-eligible outcomes to existing 404/409 domain exceptions. Explicitly document and test import's change from generic 500 to 404/409, including only-excluded folders; narrowly update import endpoint problem metadata and tests. Recursive discovery's missing-root behavior is outside this change (Patron ruling 6, §2.3 items 4 and 6).
- Concrete names: `PrimaryVideo(LibraryPath Path, MediaFormat Format)` in Core/Ports; `RescanMovieResult`, `RescanLibraryResult`, `RescanMovieOperation` in Core/Features/Library; `RescanMovieResponse`, `RescanLibraryResponse` in Api/Endpoints. Glossary relation: Rescan updates an existing Movie's Format; Import creates a Movie. Taste names/wording are logged in `ASSUMPTIONS.md`.
- Request-aborted tokens flow through all async operations, with cooperative checks before synchronous resolve/launch and before save. Cancellation before launch prevents launch/history; before save prevents save. Cancellation in an in-flight save/history append propagates without success or a rollback promise; preserve existing append-cancellation semantics and bulk prior commits. No cancellation failure count. The two rescan routes have different segment counts; ordinary success tests suffice, no routing workaround.
- Low dispositions: accept legacy sample-only catalog failures and synchronous bulk duration/cooperative-filesystem limits; no migration/progress/worker. Add explicit Principles III/VI and coding/API-rule dispositions, scoped expected load/resolve catches with save outside the catch, non-mutable static initialization, deterministic-selector property coverage, AutoFixture/Faker.Net + Theory/MemberData, unique real-disk temp directory/cleanup, and consistent test-first dependencies where feasible. Explicitly state post-snapshot inserts wait, IDs retain existing representation, any unexpected failure aborts, and rescan never parses new title/year or enqueues enrichment.
- Generated-contract workflow follows refreshed recon part 2: opt-in OpenAPI regeneration writes baseline and Verify snapshot together, then `npm run generate:api` produces TypeScript and MSW handlers, verified by `npm run check:api`; forced generated artifacts remain tooling-only. All named existing paths cite refreshed recon; proposed new paths remain inside existing folders. Build pickup recon/STOP remains mandatory.

Quill applies the single consolidated numbered repair list to owned artifacts; Keel verifies cross-artifact consistency and all three axis dispositions before recording PLAN FROZEN. Gate 1 remains owner-held, and Phase A runs no production tests or gates.

## Phase 2 freeze decision — 2026-11-04

Keel independently verified Quill's completed single repair pass against all three axis reports, Patron's corrected rulings, this brief, and the constitution. Read-only speckit-analyze: 15 functional requirements and 6 success criteria covered by T000–T028; no unresolved blocking inconsistency, ambiguity, duplication, constitution conflict, or unmapped task. All 24 findings have explicit dispositions in Maestri note `adjudication-DEV-438`. The initial challenge plus one repair/recheck closes the two-round loop with zero unresolved Critical/High/Medium findings.

**PLAN FROZEN.** Update the current status in spec/plan/tasks/checklist to this decision; preserve Quill's drafting receipt as historical evidence. This freezes planning only. Owner Gate 1/spec merge and Build pickup remain prerequisites to implementation. No production tests, delivery gates, commit, PR, merge, or tracker mutation was performed by Keel in this adjudication.
