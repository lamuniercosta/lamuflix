# Tasks: Stream primary movie file over HTTP

**Input**: `specs/DEV-456/` spec.md, plan.md, grill-closed brief.md (Q1–Q11), CONCLUSIONS.md, ASSUMPTIONS.md (none), recon-DEV-456, recon-DEV-456-2, recon-DEV-456-3, note DEV-456, specs/PRODUCT.md
**Status**: Draft — Quill plan-challenge repair round 1 applied from `PLAN_CHALLENGE_FIXES.md` + brief/CONCLUSIONS Q11. Code tasks unchecked. No PLAN FROZEN / Gate 1 / implementation clearance / analyze-clean / runtime PASS claim. Pending Keel independent plan-challenge freeze. Ordinary Gate 1 remains pending. Q10/Q11 decision records are authoritative; do not duplicate process banners.
**Gate 1**: No owner structural checkbox (brief Q9–Q11; PRODUCT.md §5). Ordinary owner Gate 1 / spec merge precedes Build pickup. Bernstein assigned ADR **0037**; Keel-authored `docs/adr/0037-library-file-reader-port.md` already exists and remains Proposed until governance/extension exists. Whitelist **plan** facts from `recon-DEV-456-3` §3; whitelist **code** insertion lands only with T003.
**Tests**: Required (ticket ACs; brief Q6–Q8; Q11). Prefer new-behaviour regressions red before production edits where feasible, only after referenced compile surfaces exist (no noncompiling-red commits). Real filesystem authorization never mocked. Endpoint tests may double narrow ports. Explicit focused regressions for MIME mapping/fallback/no attachment-or-validators, auth-before-resolve/open order, policy-vs-404-vs-500 distinctions, concurrent shared read, route-local nosniff, and Principle VI structured movie-id errors (Q11). No EF driver mocks / EF InMemory. No real Process.Start. Parallel only on disjoint files after prerequisites; same-file work is never parallel.
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete. Same-file edits are never `[P]`.
**Conventions**: `Method_Condition_Expected`; existing xUnit v3 / Shouldly / NSubstitute / Testcontainers / e2e harness only; no new packages/layers/schema/UI; never hand-edit generated OpenAPI/TS/MSW; never lower harness thresholds; use the evidenced API DI registration file `src/LamuFlix.Api/HandlerRegistration.cs` beside the scanner (Q12); CONTEXT.md glossary rewrite forbidden (Q9); port ADR is `docs/adr/0037-library-file-reader-port.md`; no explanatory comments outside allowed exemptions.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs**. Canonical pipeline remains Gate 1 → Build/refactor/gates → review → delivery under Conductor (brief Q8). Path authority: `recon-DEV-456` + `recon-DEV-456-2` + `recon-DEV-456-3`. Exact DI file = Phase B Wisp. Whitelist pattern facts already evidenced in recon-DEV-456-3. Pickup analyze / task-drift adjudication / constitution edit = Keel only. ADR M1 and any ADR-body repair remain Keel-owned (outside Quill scope).

---

## Local phase dependency / execution order

```text
Phase 0  T000 (Keel Gate 1 record) — blocks T001+
Phase 1  T001 (Wisp pickup facts) → T001b (Keel analyze / task-drift)
         → T001c (Keel verify-only ADR/governance/whitelist plan)
         → T001d (Keel-owned constitution edit deliverable) — blocks T003+
Phase 2  T003 (atomic buildable port+whitelist type slice)
         → T002 (behavioral real-disk red tests)
         → T004 (LibraryFileReader impl) → T005 (DI register + green)
Phase 3  T006 → T010   (US2 HTTP stream range/error/MIME/auth-order/nosniff/structured-error contracts)
Phase 4  T011 → T013   (US3 generated OpenAPI contract chain)
Phase 5  T014 → T015   (US4 Compose nginx + browser seek)
Phase 6  T016 → T021   (verification, review, delivery)
```

---

## Phase 0: Gate 1 record (blocks Phase 1)

**Purpose**: Confirm user Gate 1 merge before any Build work.
**Checkpoint**: Verified merge receipt in CONCLUSIONS.md; or stop.

- [X] T000 **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-456 in `specs/DEV-456/CONCLUSIONS.md`. Receipt only — does not create a new ruling. T001–T021 MUST NOT proceed until this receipt exists (brief Q9; FR-013).

---

## Phase 1: Pickup — Wisp facts, Keel analyze, Q10/Q11 compliance (blocks feature implementation)

**Purpose**: Split pickup ownership (I1 + Q11): Wisp supplies facts; Keel owns subsequent analyze, verify-only ADR/governance/whitelist-plan pickup, and the distinct constitution-edit deliverable. Confirm Q10/Q11 documentation/governance prerequisites and evidenced whitelist plan before the new port.
**Checkpoint**: Fresh Wisp recon with exact DI file (+ whitelist plan facts when drifted/absent); Keel analysis complete/disposed; existing ADR **0037** verified present (remains Proposed until extension/governance exists); T001d constitution/governance landed or exact-equivalent evidence verified; exact evidenced whitelist **plan** (recon-DEV-456-3 §3 / refreshed) confirmed before T003. Whitelist **code** insertion does **not** land here — it lands atomically with T003 only. Unresolved drift, missing facts, missing ADR file, or missing constitution/governance stops — no invention. Clear STOP prerequisites; do not mark premature Principle I compliance PASS. Original C1 planning contradiction is repaired via Q10/ADR; Principle I implementation PASS remains unwarranted until constitution/governance + atomic port/whitelist evidence exist.

- [X] T001 After T000: **Wisp** obtains fresh Build pickup recon against picked HEAD / landed main for approved surfaces cited in `recon-DEV-456` / `recon-DEV-456-2` / `recon-DEV-456-3` (scanner/`ResolvePrimaryVideo`, thumbnail read-then-serve precedent, containment precedents, `ApiEndpoints`, OpenAPI/Library inventories, e2e harness, sample-library corpus, GlobalExceptionHandler mappings) **and identifies existing API DI registration file `src/LamuFlix.Api/HandlerRegistration.cs` beside `IMediaLibraryScanner`** (Phase B Wisp; Patron Q12) for `ILibraryFileReader` / `LibraryFileReader`. Confirm DEV-438/DEV-459 dependency state at implementation base (do not infer DEV-459 implementation from grill-base spec-only presence). Pickup receipt: DEV-438 on main through `86f9aeb5`; DEV-459 #343 `22404dfc` awaiting-merge; unchanged scanner API. Patron Q12 permits implementation after T001b/c/d while requiring post-DEV-459 integration verification before live acceptance/delivery closure; brief/CONCLUSIONS Q12 records the dependency and DI dispositions. Refresh whitelist **plan** facts only if `ArchitectureTests.cs:30-31` drifted from `recon-DEV-456-3` §3. **Wisp owns pickup facts only** — do not run analyze, do not amend tasks, do not invent registration location, do not insert whitelist code, or broaden the whitelist beyond append-only `|ILibraryFileReader`. **STOP** on missing DI file or missing facts. No production feature edits in this task (brief Q7–Q8; Q10–Q11; FR-011–FR-012).
- [X] T001b After T001: **Keel** runs pickup cross-artifact reanalysis of brief/spec/plan/tasks against Wisp facts and performs authorized task-drift adjudication only. Amend Quill-owned task text only where authorized by demonstrated drift under Keel ownership. **Feature implementation (T003+) MUST NOT start until this Keel analysis completes clean or with findings disposed.** Unresolved drift stops for a cited decision — no invention (FR-012; Q11).
- [X] T001c After T001b: **Keel-owned verify-only pickup** — confirm Q10/Q11 documentation/governance prerequisites before port implementation: **verify** existing Keel-authored `docs/adr/0037-library-file-reader-port.md` is present at pickup (**do not reauthor**; ADR remains Proposed until governance/extension exists); verify prior governance/whitelist **plan** evidence (append-only `|ILibraryFileReader` before `)$` on `ArchitectureTests.cs:31`, preserve 16 entries/order per `recon-DEV-456-3` §3 / T001 refresh). **Do not** land the constitution edit here — that is T001d. **Do not** treat `recon-DEV-456-3` §3.4 constitution omission of `IEnrichmentOutbox` / `IRecommendationProfileSource` as authorization to backfill those two ports. ArchitectureTests whitelist **code** edit itself lands atomically with T003 only (not here). **STOP** if ADR missing or whitelist plan unconfirmed. Clear STOP prerequisites; do not mark premature Principle I compliance PASS (brief Q10–Q11; FR-011; recon-DEV-456-3).
- [X] T001d After T001c, **before T003**: **Keel-owned constitution-edit deliverable** in the Build PR — edit `.specify/memory/constitution.md` to add only `ILibraryFileReader` to the named-port enumeration; bump appropriate MINOR (**1.11.0** if pickup still shows **1.10.0**, otherwise refresh version at pickup); complete Sync Impact Report, Last Amended, template review, and migration/follow-up record per Governance. Preserve current enumerated entries and rules; **no DEV-467 / omitted-port backfill**. If the exact equivalent extension+governance evidence already landed, verify that evidence instead of duplicating the amendment. ADR 0037 remains Proposed until this extension/governance exists. **T003 MUST NOT land until T001c verify-only + T001d constitution/governance (or exact-equivalent verified evidence) are complete.** Do not claim Principle I implementation PASS merely because Q10/Q11 were ruled or ADR 0037 exists (brief Q11 item 5; FR-011; constitution Governance).

---

## Phase 2: User Story 1 — Contained authorize-and-open (P1)

**Goal**: Narrow `ILibraryFileReader` (`void AuthorizeFolder(LibraryPath folder)` / `Stream OpenRead(LibraryPath file)`) + `LibraryFileAccessDeniedException` + `LibraryFileReader` with real-disk folder-then-file containment, async shared open, and single authorized open.
**Independent Test**: Real-disk matrix for relative/contained legacy rooted/outside/sibling-prefix/actual links/missing/non-regular/policy-vs-404-vs-500 distinctions — without HTTP — plus returned-file authorization before open, single authorized open, and focused concurrent-reader proof.
**Buildable-red rule (Q11)**: T003 lands a compile-ready port + exception + whitelist type slice first (include minimal compile-only adapter surface in that slice if T002 must name `LibraryFileReader`; otherwise do not reference the adapter type until T004). T002 behavioral reds compile against that surface — no noncompiling-red commits.

- [X] T003 [US1] As **one atomic buildable type slice** (DEV-17 precedent; recon-DEV-456-3 §3.3; Q11): add narrow port `src/LamuFlix.Core/Ports/ILibraryFileReader.cs` with exact Q11 signatures `void AuthorizeFolder(LibraryPath folder)` and `Stream OpenRead(LibraryPath file)` (BCL `Stream` only; no HTTP/framework types on the port); add `src/LamuFlix.Core/Ports/LibraryFileAccessDeniedException.cs` for policy/safety denial including non-regular paths; **and** append `|ILibraryFileReader` inside `PortWhitelistPattern` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:31` immediately before `)$`, preserving all **16** existing entries and their order exactly as in `recon-DEV-456-3` §3.2 — no other whitelist line/body/floor changes; do not reorder or remove entries; do not add `IEnrichmentOutbox` / `IRecommendationProfileSource` via this ticket. If subsequent T002 must reference `LibraryFileReader`, include only a minimal compile-only adapter surface in this same slice; otherwise leave adapter implementation to T004. **Blocked on T001b Keel pickup analyze completion, T001c verify-only, and T001d constitution/governance (or exact-equivalent verified evidence).** Whitelist **code** lands in this task only. Do not claim Principle I implementation PASS prematurely (Q7; Q10–Q11; FR-011).
- [X] T002 [US1] After T003 compile-ready surfaces exist: add failing real-disk unit coverage under `tests/LamuFlix.UnitTests/FileSystem/` (exact new test file at Build) for relative contained paths, contained legacy rooted paths, outside-root escapes, sibling-prefix escapes, **actual** symlink/reparse chains (mandatory live fixture), missing folder/file → existing `NotFoundException` semantics, non-regular-file → `LibraryFileAccessDeniedException` safety denial, outside/link policy denial → `LibraryFileAccessDeniedException`, OS `UnauthorizedAccessException`/genuine I/O unreadability remaining distinct (not blanket-translated to policy denial), **returned-file authorization before open**, **single authorized open of the canonical authorized full path**, per-component link/reparse rejection on the candidate chain, and **focused concurrent-reader byte proof** against async shared open — never mock filesystem authorization; preserve real-disk proof machinery (Q4; Q8; Q11; SC-004). **Windows test setup MUST fail loudly** if neither a permitted symlink fixture nor a genuine junction fixture can be created — never false PASS / never SKIPPED-as-PASS. Build/Linux execution remains required for live link proof; do not invent observed capability. Do not mark unavailable link proof as PASS.
- [X] T004 [US1] Implement `src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs`: same canonicalization/link-denial rules for `AuthorizeFolder` and `OpenRead`; reject link/reparse components throughout the candidate chain; use authorized canonical full path only; authorize selected file then open once; open one async readable seekable regular-file `FileStream` with `FileShare.Read` and `FileOptions.Asynchronous | FileOptions.SequentialScan`; throw `LibraryFileAccessDeniedException` only for policy/safety denial (including non-regular); map missing/disappeared folder/file via existing `NotFoundException`; do not blanket-catch OS `UnauthorizedAccessException`/IO unreadability into policy denial; follow containment precedents locally without extracting/rewriting ThumbnailConsumer/ThumbnailCache/FileSystemLibraryFolderDiscovery; no new concurrency subsystem (Q4; Q11; FR-003; FR-011)
- [X] T005 [US1] Register `ILibraryFileReader` → `LibraryFileReader` **only** in existing API composition file `src/LamuFlix.Api/HandlerRegistration.cs` beside `IMediaLibraryScanner` (T001; Patron Q12); make T002 green; retain existing `PrimaryVideoSelectorPropertyTests` untouched (Q7–Q8; FR-012)

---

## Phase 3: User Story 2 — HTTP stream with frozen range and error contracts (P1)

**Goal**: `GET /api/movies/{id}/stream` with frozen 200/206/416 headers, route-local nosniff on successful binary responses, MIME contract, auth-before-resolve/open order, Q11 error map (`LibraryFileAccessDeniedException`→403 only; `NotFoundException`→404; operational unreadability→500), Principle VI structured movie-id errors, LocalPlay independence, stream ownership transfer.
**Independent Test**: Endpoint/host matrix for bytes/headers/nosniff/MIME/auth-order/errors/structured-id logging/access parity/LocalPlay=false/side effects/cancellation; characterization-only edges. Preserve framework range parsing and real-disk containment proofs. Missing route is a valid behavioral red — do not add unsupported endpoint-type references to pre-endpoint host tests.

- [ ] T006 [US2] After Phase 2 authorize-and-open surfaces exist: add failing endpoint/host coverage under `tests/LamuFlix.IntegrationTests/` (exact new test file at Build) for: no-Range 200 byte-exact + length + `Accept-Ranges: bytes` + route-local `X-Content-Type-Options: nosniff`; satisfiable single-range 206 exact slice + Content-Range/Content-Length/Accept-Ranges + nosniff; unsatisfiable valid range 416 + `Content-Range: bytes */length`; malformed id 400; missing movie/folder/file 404; no eligible primary 409; `LibraryFileAccessDeniedException` containment/link/non-regular denial 403 (route-local catch of that type only); unexpected I/O / OS access unreadability 500; sanitized ProblemDetails (no paths/exception text); **correct mapped-status assertions covering the frozen negatives (and thus excluding manufactured 422 for those cases; 422 remains inherited ValidationException mapping only — no synthetic stream-path ValidationException requirement)**; library caller access-policy parity (ThumbnailEndpointTests precedent); LocalPlay=false success with asserted zero history/process/launcher calls; cancellation/disposal ownership; characterization recording for malformed/unsupported-unit/multiple Range and If-Range-without-validators **without** freezing those statuses as contract (Q1–Q5; Q8; Q11; FR-001; FR-005–FR-008; SC-001–SC-004). Pre-endpoint host tests may fail behaviorally on missing route without referencing a nonexistent endpoint class.
- [ ] T006a [US2] Add focused failing regressions (same endpoint/host surface as T006, or adjacent focused tests under `tests/LamuFlix.IntegrationTests/`) for FR-006 MIME: resolved-format `FileExtensionContentTypeProvider` mapping on `.` + `MediaFormat.Extension`; fallback `application/octet-stream` when unmapped; **no** Content-Disposition attachment; **no** `entityTag` / `lastModified` validators passed — no invented MIME table and no request-time provider mutation; preserve all frozen MIME/no-validator/range behavior (Q3; Q11; FR-006)
- [ ] T006b [US2] Add focused failing regressions for FR-003 orchestration order with port doubles: folder authorization runs **before** `ResolvePrimaryVideo`; a **denied folder prevents resolver invocation**; returned-file authorization runs **before** open; successful stream performs **exactly one authorized open** — preserve existing framework range machinery; do not weaken real-disk reader proofs in T002 (Q4; Q11; FR-003)
- [ ] T006c [US2] Add focused failing regressions for Principle VI structured errors: pre-header mapped errors (including route-local 403) and post-header failures carry the requested movie id in structured logs/errors **without filesystem paths**; do **not** claim existing `GlobalExceptionHandler` already supplies this; no new telemetry names; no framework types introduced into Core (Q11 item 4; constitution Principle VI; FR-007)
- [ ] T007 [US2] Implement `src/LamuFlix.Api/Endpoints/StreamEndpoints.cs` orchestrating existing movie-details query → `AuthorizeFolder` → `IMediaLibraryScanner.ResolvePrimaryVideo` → `OpenRead` → MIME lookup via **injected or per-instance** read-only `FileExtensionContentTypeProvider` (configured/read-only; **never** a mutable `static readonly` mapping instance — preserve const/static mutation hygiene) on `.` + `MediaFormat.Extension` (fallback `application/octet-stream`) → `Results.Stream`/`TypedResults.Stream` with `enableRangeProcessing: true` on the open seekable known-length stream; on successful binary **200/206 only**, set route-local `X-Content-Type-Options: nosniff` (document + live header evidence in tests; **no** global/config header edit); pass no entityTag/lastModified/download filename; transfer stream ownership to the result (no endpoint `using` that closes before send); dispose on pre-result failure; route-local catch **only** `LibraryFileAccessDeniedException` → 403; missing/disappeared → existing `NotFoundException` → 404; OS access/IO unreadability remain sanitized 500; emit Principle VI structured requested-movie-id errors without filesystem paths for local 403 / mapped pre-header errors / post-header failures; assert/set frozen headers where framework emission is insufficient without replacing framework range parsing (Q2–Q5; Q7; Q11; FR-001–FR-008)
- [ ] T008 [US2] Register `MapStreamEndpoints` in `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs` alongside existing map methods; do not amend PlaybackEndpoints/launcher/GlobalExceptionHandler mappings beyond what FR-007 already preserves (existing ValidationException→422 mapping remains inherited; do not invent stream-local ValidationException); do not call the play handler for resolution (Q1; Q7; Q11; FR-001; FR-011)
- [ ] T009 [US2] Wire Content-Type / Produces scaffolding needed for binary Stream responses in `StreamEndpoints.cs` (200/206 Stream + explicit 416 + frozen error Produces) so later contract regeneration can carry metadata (Q3; FR-009) — inventory regeneration itself is Phase 4
- [ ] T010 [US2] Make T006 / T006a / T006b / T006c green against real HTTP results with port doubles as needed; confirm MIME mapping/fallback/no attachment-or-validators; confirm nosniff on successful 200/206 only; confirm denied-folder prevents resolver invocation and single authorized open; confirm policy-exception→403 vs NotFound→404 vs operational→500 distinctions; confirm structured movie-id errors without paths; confirm no history/catalog mutation and no Process.Start/launcher on stream paths; confirm mid-body cancel/read failure does not emit a second JSON success/error body (Q1; Q5; Q11; FR-003; FR-006–FR-008; SC-004)

---

## Phase 4: User Story 3 — Generated OpenAPI contract chain (P2)

**Goal**: Tooling-regenerated OpenAPI/TS/MSW + inventory updates for the stream route.
**Independent Test**: Drift/inventory checks green; no hand-edited generated files.

- [ ] T011 [US3] Update `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` ExpectedOperations (and related schema expectations as forced by regeneration) for `GET /api/movies/{id}/stream` with documented 200/206 binary Stream and explicit 416 plus frozen error responses (Q3; Q7; FR-009)
- [ ] T012 [US3] Update `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` endpoint-name inventory to include the stream endpoint (Q7; FR-009)
- [ ] T013 [US3] Regenerate contracts via supported workflow: set `LAMUFLIX_REGENERATE_OPENAPI=true`, run OpenAPI regeneration test (writes `web/src/api/openapi.json` + Verify snapshot together), then from `web/` run `npm run generate:api` producing `web/src/api/types.ts` and `web/src/mocks/handlers.ts`; verify with `npm run check:api`. **Never hand-edit** generated artifacts (Q3; FR-009; SC-005)

---

## Phase 5: User Story 4 — Live Compose nginx and browser seeking (P1)

**Goal**: Required live acceptance matrix through nginx on the delivery SHA, including browser seek.
**Independent Test**: `web/e2e/run.mjs` / smoke harness through `http://localhost:3000`; honest Could not run / SKIPPED.

- [ ] T014 [US4] Extend `web/e2e/run.mjs` (and existing smoke harness as needed) to exercise through `http://localhost:3000` web nginx `/api/` proxy (never direct API) using sample-library import/source MP4s: full byte-exact + length; prefix/suffix/open-ended 206 exact slices + Content-Range/Content-Length/Accept-Ranges; beyond-EOF 416 + `bytes */length`; disjoint sequential + backwards seek slices; malformed/unit/multiple characterization without status promises; LocalPlay=false stream success with no history/process/launcher calls; **required** real-browser seek on a browser-decodable sample MP4 through the stream URL verifying playback advances — no product player UI (Q6; FR-010; SC-001–SC-004)
- [ ] T015 [US4] Run the live acceptance on the delivery SHA; record source identity/length, requested ranges, observed statuses/headers, byte comparisons, and browser seek result pinned to that SHA. If Docker unavailable or browser codec unavailable: report **Could not run** / preserve exit-2 **SKIPPED** — never PASS. Do not tune `docker/nginx.conf` / Compose without a further Patron ruling after concrete failed proxy evidence (Q6; FR-010; FR-012)

---

## Phase 6: Verification, review, delivery (Q8 step 6; Q9)

**Purpose**: Full delivery gates, Q9 closing bar, exact-head workflow under Conductor.
**Independent Test**: Honest gate exits; review bar met; frozen scope only.

- [ ] T016 Confirm frozen scope only: primary-file HTTP stream on ticket route; resolver reuse; folder/file containment + authorized open with Q11 port/exception/open semantics; frozen range/MIME/nosniff/error contracts; generated contract chain; focused regression tests; required Compose nginx + browser seek; Q10/Q11 surfaces limited to `docs/adr/0037-library-file-reader-port.md`, scoped constitution enumeration for `ILibraryFileReader` only, and append-only ArchitectureTests whitelist entry. No product UI, launcher/LocalPlay/secrets changes, schema, packages, general streaming service, nginx rewrite, thumbnail extraction, glossary rewrite, broadened constitution backfill, concurrency subsystem, global header config, or unrelated rewrites. Sentry F4 personal-scale residual and Ledger L4 (0036 on feature/458-spec per recon-3 §2) require no scope/code task (Q9–Q11; FR-011; FR-013)
- [ ] T017 Run standalone `dotnet build`, then full configured `dotnet test` including new file-reader and stream endpoint suites (FR-012; Q8)
- [ ] T018 Run `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml, `./scripts/run-jetbrains-inspectcode.ps1` on changed `.cs`; `dotnet format --verify-no-changes`; `./scripts/run-property-tests.ps1`; `./scripts/run-vulnerable-packages.ps1` (FR-012; Q8)
- [ ] T019 Run configured mutation (`pwsh -NoProfile -File ./scripts/run-mutation.ps1`) and OpenAPI/TS/MSW drift (`npm run check:api` / applicable web gates). Report **actual** exits only: PASS/FAIL/`SKIPPED (scope-empty)`/`SKIP`/mutation `N/A`/`Could not run` — never relabel as PASS. Required live acceptance remains separately mandatory (FR-012; Q6; Q8; SC-005)
- [ ] T020 Size M review under Q9 closing bar (Critical/High block; Medium/Low explicit evidence-based disposition; two rounds total; unresolved Critical/High after cap report to Bernstein); publish Sentry, Ledger, and Compass outcomes on the PR (FR-013; Q9)
- [ ] T021 Exact-head delivery workflow under Conductor (rebase/ship/PR as routed); user merges; no autonomous merge (brief Q9)

---

## Dependencies

- T000 before T001–T021.
- Q12: DEV-459 #343 is an integration/delivery dependency, not an implementation blocker. After it lands, revalidate relative and contained legacy-rooted folders, unchanged resolver reuse, and independent physical authorization before T015/live acceptance and T021/delivery closure; record landed SHA and actual evidence. T001b/c/d remain prerequisites for T003.
- T001 (Wisp facts) before T001b (Keel analyze); T001b before T001c; T001c before T001d; T001d before T003.
- T001c (Keel verify-only ADR/governance/whitelist plan) + T001d (Keel constitution edit or exact-equivalent evidence) before T003 atomic port+whitelist **code** slice.
- Exact DI file from T001 required before T005.
- **Order**: T003 buildable type slice → T002 behavioral red tests → T004 reader implementation → T005 registration/green. T002 does not precede T003; no T002 parallel-with-T001c allowance.
- Phase 2 (T003–T005) before Phase 3 endpoint production that depends on authorize-and-open.
- T006 / T006a / T006b / T006c before T010; T007 before T008/T009/T010; T008 registration before endpoint green.
- Phase 3 behaviour before Phase 4 regeneration (T009 metadata before T013).
- T011/T012 may prepare ExpectedOperations/name-list edits in parallel after route shape exists (disjoint files); T013 after Produces metadata exists.
- Phase 5 after Phase 3–4 stable on delivery SHA; T014 before T015.
- Phase 6 after implementation + live acceptance evidence (or honest Could not run blocking closure).
- Parallel only on disjoint inventory files after prerequisites; never mark same-file work parallel; T002 and T006/T006a/T006b/T006c are not `[P]`.

## Sequencing examples

```text
T000 Gate 1 receipt → T001 Wisp DI-file (+ whitelist refresh if drifted) → T001b Keel analyze
T001c Keel verify-only ADR/governance/whitelist plan → T001d Keel constitution edit (or exact-equivalent verify)
T003 atomic buildable port + LibraryFileAccessDeniedException + whitelist type slice
T002 behavioral real-disk reds (actual link fixture; fail-loud Windows setup; concurrent-reader proof)
T004 LibraryFileReader (async FileShare.Read + Asynchronous|SequentialScan) → T005 DI register + green
T006 / T006a / T006b / T006c red endpoint + MIME + auth-order + structured movie-id errors
T007 StreamEndpoints (route-local nosniff; policy-exception→403 only; injected MIME provider) → T008 ApiEndpoints → T009 Produces → T010 green
T011 OpenApiContractTests → T012 LibraryEndpointsTests → T013 regenerate + check:api
T014 extend web/e2e/run.mjs → T015 live SHA evidence (or Could not run / SKIPPED honesty)
T016–T021 build / gates / Size M review on PR / delivery
```

## Parallel opportunities

- T011 and T012 inventory edits may proceed in parallel after StreamEndpoints shape exists (different files; disjoint).
- Never mark `[P]` across shared files (`StreamEndpoints.cs`, DI file, `ApiEndpoints.cs`, `ArchitectureTests.cs`, reader/port surfaces) or incomplete prerequisites.
- Do **not** parallelize T002 with T001c/T001d. Do **not** mark T002 or T006/T006a/T006b/T006c as `[P]`.

## Implementation strategy

### MVP

Gate 1 → Wisp DI facts → Keel pickup analyze → Keel verify-only ADR/governance/whitelist plan → Keel constitution edit → US1 buildable port/exception/whitelist slice → US1 behavioral real-disk reds → reader impl → US2 stream 200/206/416 + nosniff + MIME/auth-order + Q11 errors + LocalPlay=false. That alone delivers the ticket route with containment and frozen HTTP contract.

### Incremental delivery

1. Governance verify + constitution edit, then contained authorize-and-open (buildable type slice → behavioral reds → real disk impl) with atomic whitelist.
2. Stream endpoint + endpoint/MIME/auth-order/nosniff/structured-error proofs.
3. Generated contracts / inventories.
4. Live nginx + required browser seek.
5. Full gates + Size M review on PR.

### Notes

- Quill Phase A / plan-challenge repair does not execute T000–T021 and does not mark code tasks complete.
- Exact new test filenames under existing test projects are chosen at Build without expanding frozen scope (Q7).
- Exact existing DI file identified by Phase B Wisp and approved in Q12: `src/LamuFlix.Api/HandlerRegistration.cs` beside `IMediaLibraryScanner`; adapter remains in Infrastructure.
- Whitelist change is exactly recon-DEV-456-3 §3.3 append-only `|ILibraryFileReader` before `)$` on `ArchitectureTests.cs:31`; constitution omission of two prior ports is not in-scope backfill.
- No CONTEXT.md glossary task (Q9). No nginx/Compose config task unless a later Patron ruling follows concrete failed proxy evidence (Q6).
- Sentry F4 accepted residual and Ledger L4 (0036 occupied on feature/458-spec) require no scope/code task. Keel separately repairs ADR M1.
- Authoritative decisions: brief/CONCLUSIONS Q10–Q11. Historical analyze-fix receipts remain history only.
