# Implementation Plan: Stream primary movie file over HTTP

**Branch**: `feature/456-spec` | **Date**: 2026-11-07 | **Spec**: `specs/DEV-456/spec.md`
**Input**: Grill-closed `brief.md` (Q1–Q11; Q10 supersedes earlier no-ADR / sole-DI-prerequisite wording; Q11 plan-challenge rulings); `CONCLUSIONS.md` (Q1–Q11); `ASSUMPTIONS.md` (none); `spec.md`; Maestri notes `DEV-456`, `recon-DEV-456`, `recon-DEV-456-2`, `recon-DEV-456-3`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only; Quill does not edit constitution or ADRs)
**Status**: Draft — Quill plan-challenge repair round 1 applied from `PLAN_CHALLENGE_FIXES.md` + brief/CONCLUSIONS Q11. Pending Keel independent plan-challenge freeze. Ordinary owner Gate 1 and implementation clearance remain pending. No PLAN FROZEN / analyze-clean / runtime PASS / Principle I implementation PASS claim. Q10/Q11 decision records are authoritative.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md`, `checklists/requirements.md` (Quill). No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief; public route/statuses/headers are encoded in `spec.md` and OpenAPI generation tasks. Q10 port ADR is Bernstein-assigned **0037** at Keel-authored `docs/adr/0037-library-file-reader-port.md` (already exists; remains Proposed until governance/extension; Quill does not write or reauthor the ADR; ADR M1 repair is Keel-owned).

## Summary

Deliver read-only `GET /api/movies/{id}/stream` that reuses the existing primary-video resolver, authorizes movie folder then primary file under the library root via exact Q11 port signatures, opens one seekable regular file with async shared `FileStream` (`FileShare.Read`, Asynchronous|SequentialScan), and serves bytes through ASP.NET Core stream file-result range processing (`enableRangeProcessing: true`) with the frozen 200/206/416 header contract plus route-local nosniff on successful binary responses, sanitized pre-header errors (`LibraryFileAccessDeniedException`→403 only), Principle VI structured movie-id errors, LocalPlay-independent behaviour, regenerated OpenAPI chain, and required Compose nginx plus browser-seek acceptance — without product UI, launcher changes, schema, packages, global header config, concurrency subsystem, or a general streaming service layer.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).
**Primary Dependencies**: Existing pins only (ASP.NET Core Minimal APIs / file results, `Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider`). **No new NuGet/npm packages** (Q2–Q3; Q7; PRODUCT §5 item 1).
**Storage**: Existing Postgres movies catalog read-only via existing movie-details query; **no schema or migration** (Q1; Q7; PRODUCT §5 item 3). Media bytes from configured library filesystem mount.
**Testing**: xUnit v3, Shouldly, NSubstitute, existing host/endpoint fixtures, real-disk file-reader tests (actual link/reparse fixtures; fail-loud Windows setup; concurrent-reader proof), Testcontainers only if EF query/serialization is touched (never driver mocks), FsCheck PrimaryVideoSelector properties retained, Compose e2e via `web/e2e/run.mjs`. No Gherkin. No MockFileSystem substitute for containment authorization proofs. Behavior-red tests only after compile-ready referenced surfaces exist.
**Target Platform**: .NET server (ASP.NET Core Minimal API) behind Compose web nginx; personal library mount trusted local (Compose `:ro`).
**Project Type**: Ports and adapters in existing Core / Infrastructure / Api projects + generated web contract artifacts + e2e harness extension.
**Performance Goals**: Stream from open file handle without whole-file buffering; personal-library scale; focused concurrent-reader proof only — no new concurrency subsystem.
**Constraints**: No new Core handler / general streaming service / project / layer / package / schema / glossary rewrite; no UI; no LocalPlay/launcher/Process.Start/secrets changes; never hand-edit generated OpenAPI/TS/MSW; never lower harness thresholds; exact DI file named only by Phase B Wisp; ADR path is `docs/adr/0037-library-file-reader-port.md`; whitelist change is recon-DEV-456-3 append-only `|ILibraryFileReader` before `)$` on `ArchitectureTests.cs:31` preserving 16 entries/order, atomic with the port; scoped constitution extension adds only `ILibraryFileReader` (do not backfill omitted ports); Q10/Q11 compliance not claimed PASS prematurely; TimeProvider where time is needed; `const` / non-mutable static init hygiene (MIME provider injected or per-instance read-only — never mutable `static readonly` mapping instance); no explanatory comments outside allowed exemptions.
**Scale/Scope**: Size M; four sample MP4s as natural corpus; required live Compose + browser evidence on delivery SHA. Sentry F4 residual and Ledger L4 require no scope/code task.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (worktree current). Brief Q9–Q11: no ticket contradiction; Q10/Q11 are compliant corrections/refinements using the permitted named-port enumeration-extension procedure — no principle waiver and no structural owner checkbox. Quill does not Accept, invent ADR numbers, invent whitelist facts, or author ADRs/constitution edits.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; adding a port requires an ADR; closed enumeration; architecture enforcement | **OPEN — residual Q10/Q11 execution required (not implementation PASS)**: retain Q7/Q11 narrow `ILibraryFileReader` (`AuthorizeFolder` / `OpenRead`) + `LibraryFileAccessDeniedException` in existing Core Ports / Infrastructure FileSystem seams (not a repository/mediator/services layer). Q10 + authored ADR **0037** repair the original no-ADR planning contradiction; ADR remains Proposed until governance/extension exists. Before/with port implementation: Keel verify-only pickup of existing `docs/adr/0037-library-file-reader-port.md` (do not reauthor); distinct Keel-owned constitution named-port enumeration extension adding only `ILibraryFileReader` with required governance record (`:65-82`, `:712-734`; appropriate MINOR — 1.11.0 if pickup still 1.10.0; Sync Impact Report; Last Amended; template review; migration/follow-up) — do not backfill constitution-omitted `IEnrichmentOutbox` / `IRecommendationProfileSource` (`recon-DEV-456-3` §3.4); ArchitectureTests `PortWhitelistPattern` at `ArchitectureTests.cs:30-31` appends only `|ILibraryFileReader` before `)$`, preserving all 16 existing entries and order, landed atomically with the port file (`recon-DEV-456-3` §3.3; evidenced plan before T003, code with T003). Clear STOP prerequisites; ADR existence alone is not Principle I PASS. |
| II (Explicit Handlers / Decorators) | AddHandler registration | **PASS** — no new Core command/handler; endpoint orchestrates existing movie-details query + scanner port + narrow file reader (Q7). |
| III (Typed query model) | MovieQuery | **N/A** — no filter/sort surface. |
| IV (Domain transitions) | Movie mutators | **PASS** — streaming is read-only; no Movie mutation (Q1). |
| V (Validated inputs / errors) | ProblemDetails taxonomy | **PASS** — frozen 400/403/404/409/500 map + inherited ValidationException→422 only where applicable + framework 416; sanitized ProblemDetails; route-local catch of **only** `LibraryFileAccessDeniedException` → 403; no synthetic stream-path ValidationException (Q5; Q11; recon §9). |
| VI (Observability) | Telemetry / health | **OPEN — pending Build requirement (Q11)**: Principle VI structured error logging MUST carry the requested movie id without filesystem paths, including route-local 403, other mapped pre-header errors, and post-header failures. Explicit task T006c + T007/T010 regressions required. Do **not** claim existing `GlobalExceptionHandler` already supplies this. No new telemetry names; no framework types in Core. |
| VII (Configuration / LocalPlay / Process.Start) | LocalPlay gated; no secrets | **PASS** — stream independent of LocalPlay; no Process.Start/secrets; existing launch path untouched (Q1). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS with Q9 bound** — feature vocabulary recorded in spec; Q9 forbids glossary rewrite this ticket, so no CONTEXT.md edit task. |
| IX (Test Pyramid) | Real I/O where behaviour depends on it | **PASS** — real-disk containment/open with actual link fixtures and fail-loud Windows setup; endpoint doubles for ports; Compose nginx + browser for seeking; no FS-auth mocks; focused MIME/auth-order/nosniff/policy-distinction/concurrent-read/Principle-VI regressions (Q6; Q8; Q11). |
| API / contract rules | MapXxxEndpoints; generated OpenAPI/TS/MSW | **PASS** — new `StreamEndpoints` + `ApiEndpoints` registration; route-local nosniff on successful binary 200/206 only (no global/config edit); `LAMUFLIX_REGENERATE_OPENAPI` → `npm run generate:api` → `npm run check:api`; never hand-edit (Q3; Q11; recon §§7–8). |
| Governance / Static-Analysis Gates | Honest exits; never lower thresholds; extension governance record | **OPEN for Q10/Q11 governance record / evidenced whitelist; otherwise Phase B / Build honesty**: report actual exits; SKIPPED/SKIP/N/A/Could not run never PASS; required live acceptance separately mandatory (Q6; Q8–Q11). |

### Complexity Tracking

No Complexity Tracking rows invented by Quill. Principle I residual execution (constitution/governance + atomic port/whitelist) remains **not** marked PASS. ADR **0037** already authored by Keel; Quill does not author, reauthor, or edit ADR/constitution. Principle VI remains an explicit pending Build requirement, not N/A.

## Approach

- **Phase A (this draft / plan-challenge repair)**: Quill drafts and repairs Quill-owned spec/plan/tasks/checklist/receipt from brief/CONCLUSIONS/ASSUMPTIONS and recon facts, including Q10/Q11 propagation. Keel owns read-only `/speckit-analyze`, plan challenge, freeze, ADR M1 repair, and subsequent authorized task-drift adjudication. No production edits, tests, gates, commits, or PR here (Q8–Q9).
- **Phase B pickup (after Gate 1) — split ownership**:
  - **Wisp**: supply pickup facts only — drift/dependency check at the implementation base and **existing API DI registration file `src/LamuFlix.Api/HandlerRegistration.cs` beside `IMediaLibraryScanner`** (Phase B Wisp; Patron Q12); refresh whitelist facts only if `ArchitectureTests.cs:30-31` drifted from `recon-DEV-456-3` §3. Do not infer DEV-459 implementation from grill-base spec-only presence. Do not invent registration location, broaden whitelist, or amend tasks.
  - **Keel**: subsequent pickup `/speckit-analyze` (or equivalent cross-artifact reanalysis) and authorized task-drift adjudication; **T001c verify-only** ADR/governance/whitelist-plan pickup; **T001d** distinct Keel-owned constitution-edit deliverable (or exact-equivalent verified evidence). Feature implementation MUST NOT start until Keel analysis completes clean or with findings disposed and T001c/d STOP prerequisites clear. Unresolved drift stops for a cited decision — no invention.
- **Q10/Q11 prerequisites before/with port implementation**: Keel verify existing ADR 0037 (do not reauthor; remains Proposed until extension/governance); Keel-owned constitution enumeration + governance for `ILibraryFileReader` only; confirmed evidenced whitelist **plan**; ArchitectureTests append-only `|ILibraryFileReader` **code** before `)$` preserving 16 entries/order, atomic with the port/exception type slice (`recon-DEV-456-3` §3).
- **Dependency-ordered implementation (brief Q8 + Q10/Q11)**:
  1. T003 atomic buildable port + `LibraryFileAccessDeniedException` + whitelist type slice (minimal compile-only adapter surface only if T002 must name `LibraryFileReader`).
  2. T002 behavioral real-disk reds (relative/contained legacy rooted, outside/sibling-prefix, actual links/reparse with fail-loud Windows setup, missing→NotFound, non-regular/policy→LibraryFileAccessDeniedException, operational unreadability distinct, returned-file auth before open, single authorized open, concurrent-reader proof).
  3. T004 `LibraryFileReader` implementation (async shared open flags) → T005 DI registration.
  4. `StreamEndpoints` tests + implementation (frozen statuses/bytes/headers; route-local nosniff on 200/206; focused T006a MIME; T006b auth-order; T006c Principle VI structured movie-id errors; error sanitization with policy-exception→403 only; access parity; LocalPlay=false; zero history/process; cancellation/disposal; framework-edge characterization). Preserve framework range parsing and real-disk containment proofs. Missing route is a valid behavioral red.
  5. C# Produces metadata, regenerate contract chain, update OpenAPI/Library endpoint inventories.
  6. Live Compose nginx + required browser-seek acceptance on delivery SHA.
  7. Full build-workspace gates, then pipeline reviews/delivery under Conductor.
- **API orchestration**: existing movie-details query → `AuthorizeFolder` → `ResolvePrimaryVideo` → `OpenRead` → injected/per-instance MIME lookup → `TypedResults.Stream`/`Results.Stream` with `enableRangeProcessing: true` → route-local nosniff on success → route-local policy-exception shaping + Principle VI structured movie-id errors (Q7; Q11; thumbnail read-then-serve precedent).
- **Containment**: mirror ThumbnailConsumer/ThumbnailCache/FileSystemLibraryFolderDiscovery patterns locally inside `LibraryFileReader`; do not extract shared helpers (Q4).
- **Verification / review**: Q9 closing bar; two remediation/review rounds; Size M Sentry/Ledger/Compass on the PR.

## Project Structure

Verified existing paths cite `recon-DEV-456` / `recon-DEV-456-2`. Proposed new paths stay inside existing folders and are marked *(proposed)*. Phase B Wisp identified existing API composition file `src/LamuFlix.Api/HandlerRegistration.cs:32-34`; Patron Q12 approves reader registration beside the scanner, with the adapter remaining in Infrastructure.

```text
specs/DEV-456/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md
  PLAN_CHALLENGE_FIXES.md                     (Keel fix list — Quill applies; does not own)

src/LamuFlix.Core/
  Ports/IMediaLibraryScanner.cs                    (existing — ResolvePrimaryVideo reuse; recon §4)
  Ports/PrimaryVideo.cs                            (existing)
  Ports/ILibraryFileReader.cs                      *(proposed)* AuthorizeFolder + OpenRead
  Ports/LibraryFileAccessDeniedException.cs        *(proposed)* policy/safety denial only
  Domain/LibraryPath.cs, MediaFormat.cs            (existing — unchanged validation rules)
  Options/LibraryOptions.cs                        (existing RootPath)

src/LamuFlix.Infrastructure/
  FileSystem/DirectoryMediaLibraryScanner.cs       (existing — resolver reuse; no selector rewrite)
  FileSystem/LibraryFileReader.cs                  *(proposed)* containment + async shared authorized open
  FileSystem/SupportedVideoExtensions.cs           (existing)

src/LamuFlix.Api/
  HandlerRegistration.cs                          (existing — register reader beside scanner; Q12)
  Endpoints/StreamEndpoints.cs                     *(proposed)* MapStreamEndpoints + orchestration + route-local nosniff/403
  Endpoints/ApiEndpoints.cs                        (existing — register stream map)
  Endpoints/ThumbnailEndpoints.cs                  (existing precedent only — do not rewrite)
  ExceptionHandling/GlobalExceptionHandler.cs      (existing — preserve inherited ValidationException→422; do not claim Principle VI movie-id compliance)

web/src/api/openapi.json                           (generated — tooling only)
web/src/api/types.ts                               (generated — tooling only)
web/src/mocks/handlers.ts                          (generated — tooling only)
web/e2e/run.mjs                                    (existing — extend Q6 matrix)
scripts/run-e2e-smoke.ps1                          (existing harness)

tests/LamuFlix.UnitTests/
  FileSystem/…                                     *(proposed focused LibraryFileReader tests)*
  FileSystem/PrimaryVideoSelectorPropertyTests.cs  (existing — retain)

tests/LamuFlix.IntegrationTests/
  …Stream… / endpoint tests                        *(proposed focused stream endpoint tests incl. MIME + auth-order + nosniff + Principle VI regressions)*
  OpenApiContractTests.cs                          (existing — ExpectedOperations)
  LibraryEndpointsTests.cs                         (existing — endpoint-name inventory)
  ThumbnailEndpointTests.cs                        (existing access-parity precedent)
  PlaybackEndpointTests.cs                         (existing LocalPlay/side-effect patterns)

tests/LamuFlix.ArchitectureTests/
  ArchitectureTests.cs                             (PortWhitelistPattern lines 30-31 — append |ILibraryFileReader before )$; preserve 16 entries/order; atomic with port)

docs/adr/0037-library-file-reader-port.md          *(Keel-authored; exists — Bernstein assigned 0037; verify at pickup; remains Proposed until governance/extension; ADR M1 = Keel)*
.specify/memory/constitution.md                    (scoped named-port enumeration + governance for ILibraryFileReader only — Keel T001d; still mandatory; Quill does not edit)
```

**Layout decisions**:

- No research/data-model/contracts artifacts — decisions frozen in brief.
- Quill owns spec/plan/tasks/receipt/checklist. Quill does not edit brief/CONCLUSIONS/ASSUMPTIONS/ADRs/constitution/production code.
- Exact new test filenames under existing test projects are chosen at Build inside those folders without expanding scope (Q7).
- Exact DI registration file is `src/LamuFlix.Api/HandlerRegistration.cs` beside `IMediaLibraryScanner` (Phase B Wisp; Patron Q12). Whitelist facts are from recon-DEV-456-3 §3.
- ADR 0037 already exists (Keel-authored); constitution/governance edits remain Keel-owned prerequisites; Quill only models them. Constitution omission of two prior ports is not in-scope backfill.

## Generated-contract workflow (recon-DEV-456 §8; recon-DEV-456-2 §7)

1. Opt-in export: set `LAMUFLIX_REGENERATE_OPENAPI=true`, run OpenAPI regeneration test (writes committed `web/src/api/openapi.json` and Verify snapshot together), unset the env var.
2. From `web/`: `npm run generate:api` produces `src/api/types.ts` and `src/mocks/handlers.ts` (never hand-edit).
3. `npm run check:api` verifies regeneration byte-equality.
4. Declare C# `.Produces<Stream>(200, …)`, `.Produces<Stream>(206, …)`, `.Produces(416)` (and frozen error Produces) so regeneration carries binary Stream + explicit 416.

## Test Strategy

| Surface | Coverage |
|---|---|
| File reader (US1) | Real disk: relative contained, contained legacy rooted, outside-root, sibling-prefix, **actual** symlink/reparse (fail-loud Windows if neither permitted symlink nor genuine junction), missing→NotFoundException, non-regular/policy→LibraryFileAccessDeniedException, operational unreadability distinct, returned-file auth before open, single authorized open of canonical path, focused concurrent-reader byte proof; never mock FS authorization |
| Endpoint (US2) | 200/206/416 bytes+headers incl. route-local nosniff on successful 200/206 (T006); focused MIME mapping/fallback/no attachment-or-validators via injected/per-instance provider (T006a); focused folder-auth-before-resolve / denied-folder blocks ResolvePrimaryVideo / file-auth-before-open / single authorized open (T006b); Principle VI structured movie-id errors without paths for route-local 403 / pre-header mapped / post-header (T006c); 400/403/404/409/500 sanitized ProblemDetails with correct mapped-status assertions (no manufactured 422); access-policy parity; LocalPlay=false + zero history/process; cancellation/disposal; characterize malformed/unit/multiple/If-Range-without-validators; missing route valid behavioral red; preserve framework range machinery |
| Architecture | Atomic with T003: append `|ILibraryFileReader` to `PortWhitelistPattern` (`ArchitectureTests.cs:31`) preserving 16 entries/order (`recon-DEV-456-3` §3) |
| Contracts (US3) | Produces metadata; regenerate OpenAPI/TS/MSW; OpenApiContractTests + LibraryEndpointsTests inventories |
| Live acceptance (US4) | `web/e2e/run.mjs` via `localhost:3000` nginx: full/partial/416/seeks/LocalPlay=false/browser seek; SHA-pinned evidence; Could not run / SKIPPED honesty |
| Properties | Retain PrimaryVideoSelector suites; optional containment boundary properties |
| Delivery gates | Roslyn; complexity; InspectCode; mutation on eligible changed C#; property; vulnerable packages; format; `dotnet test`; applicable web gates — honest exits only |

No Gherkin. Phase A / plan-challenge repair runs none of these and claims no gate result. Code tasks remain unchecked.

## Ordering (brief Q8 + Q10/Q11 — must appear in tasks)

1. Phase B Wisp pickup facts: drift/dependency recon + exact DI file (whitelist refresh only if drifted from recon-DEV-456-3).
2. Keel pickup analyze / authorized task-drift adjudication (blocks feature implementation until complete).
3. T001c Keel verify-only ADR/governance/whitelist plan (ADR remains Proposed until extension/governance).
4. T001d Keel-owned constitution edit deliverable (or exact-equivalent verified evidence) — blocks T003.
5. T003 atomic buildable port + `LibraryFileAccessDeniedException` + ArchitectureTests append-only `|ILibraryFileReader` **code** type slice.
6. T002 behavioral real-disk red tests (actual link fixture; fail-loud Windows setup; concurrent-reader proof) — after T003 compile surfaces.
7. T004 `LibraryFileReader` implementation → T005 DI registration/green.
8. Endpoint tests and implementation (T006 + T006a MIME + T006b auth-order + T006c Principle VI + nosniff + Q11 error distinctions).
9. C# response metadata, generated contract chain, inventory tests.
10. Live Compose nginx + required browser-seek acceptance on delivery SHA.
11. Full normal build-workspace gates, then reviews/delivery.

## Risks / Explicit non-promises

- Framework emission of `Accept-Ranges` / exact 416 Content-Range form is not fully Learn-documented; frozen headers are still acceptance requirements (recon-DEV-456-2 §§3–4, §9).
- Malformed/unsupported/multiple ranges and If-Range-without-validators have no promised statuses (Q3).
- Linked legitimate paths fail closed (Q4). Actual link fixtures mandatory; unavailable proof is never PASS (Q11).
- TOCTOU by a local writer on the trusted mount is outside scope (Q4).
- Required live acceptance environment-bound; Could not run blocks closure (Q6).
- Exact DI file identified by Phase B Wisp: `src/LamuFlix.Api/HandlerRegistration.cs` (Patron Q12).
- Original C1 planning contradiction repaired via Q10 + authored ADR 0037; Principle I implementation PASS remains OPEN until scoped constitution/governance + atomic port/whitelist **code** evidence land (`DRAFTING_RECEIPT.md`).
- Constitution enumeration already omits two code-whitelist ports (`recon-DEV-456-3` §3.4); this ticket does not broaden the scoped extension to backfill them.
- 422 remains inherited mapping only; correct mapped-status assertions cover negatives without promising every stream path cannot raise validation (Q11).
- Sentry F4 personal-scale residual and Ledger L4 (0036 on feature/458-spec) require no scope/code task. ADR M1 is Keel-owned.

## Loop discipline (Q9)

- Closing bar: Critical/High block; Medium/Low explicit evidence-based disposition; no waiver of failed gates or frozen acceptance.
- Frozen scope: Q1–Q8 set; anything else is a follow-up issue.
- Round cap: two remediation/review rounds; remaining Critical/High after cap block and report to Bernstein.
- Size M: Sentry, Ledger, Compass reports published on the PR.

## Phase A / Gate note

This plan is Phase A documentation only, now repaired under plan-challenge round 1. Quill does not claim PLAN FROZEN, Gate 1 approval, runtime acceptance, analyze-clean, Principle I implementation PASS, or merge clearance. Next: Keel independent plan-challenge freeze verification.


## Phase B pickup Q12

DEV-438 is on main through `86f9aeb5` at pickup `17d9545c`. DEV-459 #343 at `22404dfc` remains awaiting-merge: an integration/delivery dependency, not an implementation blocker against the unchanged scanner contract. Implement relative and contained legacy-rooted folder support with independent physical authorization; do not import or duplicate DEV-459 schema/key/helper work. Verify after DEV-459 lands before live acceptance/delivery closure. T001b/c/d still gate T003. Cited evidence and Patron ruling: brief/CONCLUSIONS Q12; recon-DEV-456 lines 8-16; task note DEV-456 line 21.
