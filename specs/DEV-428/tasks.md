# Tasks: Consume thumbnail requests and serve cached JPEGs

**Input**: `specs/DEV-428/` spec.md, plan.md, frozen brief.md (Q1–Q7 + Post-grill Analyze Ruling U1 + Phase 2 Step 7 Patron rulings P1–P20), CONCLUSIONS.md, ASSUMPTIONS.md, notes `DEV-428` / `recon-DEV-428` / `recon-DEV-428-2` / `adjudication-DEV-428-plan` / `adjudication-DEV-17-plan`, specs/PRODUCT.md
**Status**: Draft — Phase 2 analyze round 1 repair (C1/C2/U1) plus Phase 2 step 7 plan-challenge repair (Quill 8-item list). Gate 1 not approved; PLAN FROZEN not declared; no production implementation in this phase.
**Dependency**: DEV-17 must be verified landed on main before Phase 1 coding (brief). T000 is a hard stop if missing or drifted without a cited decision.
**Tests**: Required (ticket consumer + endpoint proofs; brief matrix — all mandatory). TDD optional; tests land with each step. Existing xUnit v3 / Shouldly / NSubstitute / Testcontainers only — no new test project or package. Unique per-test temp dirs; required symlink proofs in capable environment (SKIP = missing required evidence, never PASS).
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`.
**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no new explanatory comments; fake generator SOI stubs; never mock EF/driver SQL translation; never hand-edit generated OpenAPI/TS; never edit dirty `harness.yml`; mutation-safe `const`/computed literals with zero mutable static initialization.

---

## Phase dependency / execution order

```text
Phase 0  T000
Phase 1  T001 ∥ T002 → T003
Phase 2  T004 → T005 → T006 → T007
Phase 3  T008 → T009 → T010
Phase 4  T011 → T012 → T013
Phase 5  T014 → T015
Phase 6  T016 → T017…T024
```

---

## Phase 0: Pickup — DEV-17 dependency and drift (blocks all coding)

**Purpose**: Confirm the frozen brief still matches landed DEV-17 and repository seams before any production edit; record exact landed policy/handler/validator/platform facts.
**Checkpoint**: DEV-17 present on main with expected generator contract; seams compared; exact access-policy / miss-handler / validator / replacement-primitive facts recorded; cited decision obtained if drift; or stop blocked.

- [ ] T000 Verify DEV-17 landed on main (`IThumbnailGenerator.GenerateAsync`, `ThumbnailResult`, closed failure tokens, `ThumbnailOptions`, `AddLamuFlixThumbnails`, ADR0033 / constitution as landed). Compare main/contract/options/DI and existing enrichment trigger / RabbitMQ / endpoint / test seams with frozen brief evidence. Record exact landed facts for: `LibraryEndpoints` caller-access policy attributes to mirror (P1); `NotFoundException` / `GlobalExceptionHandler` 404 mechanism vs any single landed helper (P12); DEV-17 `ThumbnailOptionsValidator` and existing `LibraryOptions` startup validation rules and any exact gap — add no invented filesystem write probe or custom root-restriction validator (P10); .NET 10 target-platform atomic replacement primitive for T002 (P5); whether `Program.cs` / `ServiceDefaults/Extensions.cs` need conditional touch (P14). Run read-only `/speckit-analyze` for pickup drift. If contract facts drift, stop for a cited decision before implementation — do not invent a workaround (brief ordering 1; FR-012; P1, P5, P10, P12).

---

## Phase 1: Foundational — message record and ThumbnailCache (blocks consumer/endpoint)

**Purpose**: Core message shape and canonical cache helper with path/SOI/promotion/symlink/sharing proofs.
**Checkpoint**: Unit proofs green for containment, symlink/reparse, SOI, promote, preserve-on-failure, Windows sharing.
**User stories**: US1, US3.

- [ ] T001 [P] [US1] Create `src/LamuFlix.Core/Ports/ThumbnailRequested.cs` as `ThumbnailRequested(MovieId MovieId, string FilePath)` (FR-002; Q1; brief ordering 2)
- [ ] T002 [P] [US3] Create `src/LamuFlix.Infrastructure/Thumbnails/ThumbnailCache.cs` for canonical `{movieId}.jpg` path resolution; containment checks under trusted `CacheDirectory`; rejection of symlink/reparse on temp `<guid:N>.jpg`, canonical path, and ancestor chain; SOI validation (`Length >= 2`, `0xFF 0xD8`) before promotion and on stream open; file reads with `FileShare.ReadWrite | FileShare.Delete` and `FileOptions.Asynchronous`; atomic promotion from validated generator GUID JPEG using the T000-verified .NET 10 target-platform replacement primitive (do not hardcode an unverified OS/BCL move from memory); on promotion failure or planted symlink/reparse at canonical path preserve any prior valid regular JPEG, leave outside targets untouched (rejecting the planted link is sufficient), and clean only own contained temporary output; all SOI/extension/path literals as `const` or computed getters/methods with zero mutable static initialization (FR-007; Q2, Q5; P2, P5, P8, P16)
- [ ] T003 [US3] Add focused unit tests in `tests/LamuFlix.UnitTests/` using unique per-test temp directories under `Path.GetTempPath()` with deterministic `try`/`finally` cleanup and no `IFileSystem`: path containment reject without deleting unrelated files; planted-destination symlink/reparse rejection (outside target untouched; prior regular JPEG preserved); file+ancestor symlink/reparse rejection; SOI valid/invalid before promote and on open; successful atomic promotion via verified primitive; failed promotion preserves prior canonical JPEG; own-temp cleanup only; concurrent read-during-promotion with `FileShare.ReadWrite | FileShare.Delete` on the target platform (do not claim this runtime check already passed). Required symlink proofs MUST execute and pass in a capable environment; environment-skipped symlink tests are `SKIP` / missing required evidence (blocking), never folded into `PASS` (FR-011; Q7 cache trust row; P2, P5, P8, P15)

---

## Phase 2: Topology, composition, and post-enrichment publisher (P1)

**Goal**: Queue/DLQ/SAC declaration; enabled/disabled composition; singleton cache DI; publisher-confirmed trigger after enrichment with intentional non-ack coupling.
**Independent Test**: Topology/composition proofs; eligible publish with confirm-before-ack; publish-failure enrichment non-ack; suppressions; disabled no publish/subscribe; GET available when broker disabled.
**User stories**: US1, US2, US5.

- [ ] T004 [US5] Edit `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs` to declare durable quorum `thumbnail` (with `x-single-active-consumer=true`) and `thumbnail.dead-letter` (no thumbnail retry queue) on existing exchange conventions; routing keys match queue names per Features-style convention; bind both; document SAC bounds ordinary broker dispatch only (rare overlap last-writer-wins) (FR-001; Q4, Q5; P5)
- [ ] T005 [US2] Edit `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` to add Principle VI thumbnail producer/consumer span-name `const` members only, aligning identifiers to the exact landed naming pattern verified at T000 (no ad-hoc literals; no new metric instruments; no mutable static initialization). Create `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqThumbnailQueuePublisher.cs` with publisher-confirmed publish of `ThumbnailRequested` (reuse existing confirm mechanics; no new Core queue interface), producer Activity using those `TelemetryConstants` names tagged with `TelemetryConstants.MovieId`, mandatory `traceparent`/`tracestate` injection via `TraceContextCarrier.Inject` / `Propagators.DefaultTextMapPropagator`, and queue/routing-key literals as `const` (FR-003, FR-010; Q4; C1; Principle VI; P16, P17)
- [ ] T006 [US2] Edit `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqServiceCollectionExtensions.cs` to register concrete singleton `ThumbnailCache`; register `ThumbnailConsumer` when `Thumbnails:Enabled=true` (startup-validated `IOptions<ThumbnailOptions>.Value`; existing broker activation rules allow) else `InactiveThumbnailConsumer`; register publisher as needed; both consumer and endpoint resolve the same `ThumbnailCache` instance plus `IOptions<ThumbnailOptions>` (never raw `IConfiguration`); no bespoke thumbnail health-check infrastructure or custom metrics; no `Features:LocalPlay` coupling; document that toggling `Thumbnails:Enabled` requires Api host restart (FR-004, FR-010; Q6; P11, P14). Host-composition proofs (whether or not `Program.cs` / `ServiceDefaults` touched) cover singleton cache/options/validation registration including broker-disabled HTTP availability (P19)
- [ ] T007 [US1] Edit `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs` to publish via `RabbitMqThumbnailQueuePublisher` after successful enrichment persistence and before enrichment `BasicAckAsync` only when enabled + poster absent + local path present; require confirmation before ack; publication/topology failure must not ack enrichment and intentionally redrives enrichment via its existing retry/DLQ pipeline (no new outbox/alerts) (FR-003; Q4; P4). Add focused proofs in existing unit/integration projects for: eligible confirmed publish with injected `traceparent`/`tracestate`; publish-failure → enrichment non-ack coupling; poster-present and no-local-source suppress; disabled no publication/subscription; composition respects broker gating and GET remains available when broker disabled; singleton cache/`IOptions` wiring (FR-011; Q7 trigger/delivery/composition rows; C1; P4, P14, P19)

---

## Phase 3: User Stories 1, 3, 4 — ThumbnailConsumer (P1)

**Goal**: Active/inactive consumers with authorization, generation, promotion, terminal failure, cancellation, non-throwing extract.
**Independent Test**: Fake-generator + live-broker consumer/DLQ matrix including corrupt-`traceparent` and immediate-handoff proofs.
**User stories**: US1, US3, US4.

- [ ] T008 [P] [US2] Create `src/LamuFlix.Infrastructure/RabbitMq/InactiveThumbnailConsumer.cs` inactive hosted-service variant matching existing inactive consumer pattern (FR-004; Q1, Q6)
- [ ] T009 [US1] Create `src/LamuFlix.Infrastructure/RabbitMq/ThumbnailConsumer.cs`: extract `traceparent`/`tracestate` via non-throwing fallback (corrupt/malformed headers never throw out of the consumer loop or bypass terminal `MalformedMessage` + `BasicRejectAsync(requeue: false)`); start consumer Activity (`TelemetryConstants` span name; `ActivityLink` on redelivery; tag `MovieId` + `MessagingDeliveryCount`; error events carry movie-id + `ErrorType`); load authoritative movie; reject nonexistent/stale/ineligible as `IneligibleRequest` via focused private helper; authorize source under `LibraryOptions.RootPath` via focused private helper (separator-aware; reject traversal/sibling-prefix/symlink-reparse on file and ancestors; canonicalize/revalidate immediately before `GenerateAsync`; residual name-based TOCTOU documented under trusted operator directories); call `IThumbnailGenerator.GenerateAsync` and log exact closed generator `FailureReason` on failure; validate containment via shared singleton `ThumbnailCache`; atomically promote (`CachePromotionFailed` on promotion failure); SOI cache-hit may ack without generation; ack only after promotion or eligible cache hit; malformed payloads log `MalformedMessage`; terminal failures log one sanitized failure using only closed fields `{MovieId}` (omit only for `MalformedMessage`), `{FailureReason}`, `{Redelivered}`, `{ErrorType}` (unexpected exceptions only) with zero raw paths/payload/process output, and reject without requeue to DLQ; host shutdown cancellation requeues without generator-failure log and best-effort cleans only that attempt’s own contained temporary `<guid:N>.jpg` before requeue; prefetch 1 + serial handling under ordinary dispatch; failure-reason tokens and literals as `const`; introduce no new class/interface/service layer (FR-004–FR-008; Q3–Q5; C1; U1; P3, P5, P7, P9, P13, P16)
- [ ] T010 [US4] Add consumer proofs in `tests/LamuFlix.IntegrationTests/` (and unit where appropriate) using fake generator + live RabbitMQ/PostgreSQL as required, unique temp dirs, and capable-environment symlink proofs: ordinary enabled path to canonical JPEG; Principle VI extract/Activity/redelivery `ActivityLink`/error movie-id context; non-throwing extract — focused malformed body plus corrupt-`traceparent` still terminal `MalformedMessage` DLQ; immediate handoff canonicalize/revalidate proof; source trust rejects with `UnauthorizedSourcePath` (file+ancestor symlink/reparse); ineligible/stale with `IneligibleRequest`; all closed `ThumbnailResult.FailureReason` tokens including `BinaryNotFound` single sanitized terminal DLQ without retry; `MalformedMessage` DLQ with `{MovieId}` omitted; `CachePromotionFailed` preserves prior JPEG / planted destination leaves outside untouched; closed `{MovieId}`/`{FailureReason}`/`{Redelivered}`/`{ErrorType}` field set and omission rules; assert absence of raw paths, payload bodies, exception path text and process output; shutdown cancellation requeue with own-contained-temp best-effort cleanup; SOI cache-hit skip; SAC/prefetch/serial behaviour under ordinary broker dispatch (last-writer-wins overlap documented, not claimed globally exclusive across failover). Do not duplicate DEV-17 real-ffmpeg/Dockerfile tests (FR-011; Q7 failure/delivery/concurrency rows; C1; U1; P2, P3, P5, P9, P15)

---

## Phase 4: User Stories 1, 2, 5 — Endpoint and HTTP proofs (P1/P2)

**Goal**: Serve canonical JPEG or uniform sanitized 404; no generation on GET; concurrent Windows-safe reads; single miss mechanism.
**Independent Test**: HTTP 200/404 matrix; disabled-with-JPEG 404; concurrent read during promotion; OpenAPI Produces metadata.
**User stories**: US1, US2, US5.

- [ ] T011 [US1] Create `src/LamuFlix.Api/Endpoints/ThumbnailEndpoints.cs` implementing `GET /api/movies/{id:int}/thumbnail`: inherit landed `LibraryEndpoints` caller-access policy recorded at T000 (no new auth subsystem); read `IOptions<ThumbnailOptions>.Value` (startup snapshot; restart for flag toggle); check enabled before cache I/O; check movie existence; stream only canonical file as `image/jpeg` after serve-time SOI on stream open with async per-request disposal, `FileShare.ReadWrite | FileShare.Delete`, and `X-Content-Type-Options: nosniff` on 200 (no image-decoder dependency); on miss (disabled / nonpositive or unknown numeric id / missing / unreadable / corrupt-SOI) throw existing `NotFoundException` mapped by single `GlobalExceptionHandler` + `AddProblemDetails` (or the single landed helper recorded at T000 if `NotFoundException` differs — never two mechanisms; never bare `Results.NotFound()`; never competing TypedResults.Problem optional path) producing uniform sanitized RFC 7807 `ProblemDetails` (`status: 404`, identical `title`/`type`, zero path or miss-reason detail; only per-request `traceId` and optional `instance` may vary); declare `.Produces(StatusCodes.Status200OK, contentType: "image/jpeg")` and `.ProducesProblem(StatusCodes.Status404NotFound)`; preserve decided 404 status; never generate or access source media; no path leakage; no ETag/Cache-Control/Range (FR-009; Q3; C2; Principle V; P1, P8, P11, P12)
- [ ] T012 [US1] Edit `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs` to map thumbnail endpoints; edit `src/LamuFlix.Api/Program.cs` and/or `src/LamuFlix.ServiceDefaults/Extensions.cs` only if T000 recorded that cache/endpoint/existing-option wiring requires it (FR-010; Q7; P14)
- [ ] T013 [US5] Add endpoint/composition proofs in existing integration test projects/factories (`ApiHostCompositionTests` / `ApiEndToEndFactory` as applicable): GET 200 exact bytes + `image/jpeg` + `nosniff` after serve-time SOI; corrupt-SOI canonical → uniform sanitized 404; assertions for each miss class — nonpositive/unknown/missing/unreadable/corrupt-SOI and disabled-even-when-cached-JPEG — status 404, `application/problem+json`, `traceId` present, identical `title`/`type`, absence of file paths or miss-reason/internal details (without unsupported assumptions about `GlobalExceptionHandler` internals prior to T000); inherited access-policy mirror after T000; GET performs no generation/source access; independent concurrent HTTP reads while promotion proceeds on the target platform with Windows-safe sharing (do not claim this runtime check already passed); host-composition proofs for singleton cache/`IOptions`/validation and broker-disabled HTTP availability whether or not Program/ServiceDefaults touched (FR-011; Q7 endpoint/concurrency rows; C2; P1, P5, P8, P12, P14, P19)

---

## Phase 5: Contract chain (no web UI)

**Goal**: Regenerate OpenAPI and TypeScript through repository tooling.
**Checkpoint**: Drift checks green; no hand-edits.
**User story**: US5.

- [ ] T014 [US5] Regenerate `web/src/api/openapi.json` (and Verify drift snapshot) via established opt-in OpenAPI workflow after the route exists; confirm OpenAPI documents 200 `image/jpeg` and ProblemDetails 404 (FR-010; Q3, Q7; precedent DEV-80/DEV-425; P12)
- [ ] T015 [US5] Regenerate `web/src/api/types.ts` via `npm run generate:api` (and established mocks/typecheck path as required); never hand-edit generated files; no web UI work; confirm API drift checks (FR-010; Q7)

---

## Phase 6: Polish and Phase B verification

**Purpose**: Frozen-scope confirm, full gates, Size M review.
**User story**: US5.

- [ ] T016 Confirm frozen scope only: no new packages/projects/layers/schema/Core interfaces/outbox; no cache eviction/sweeper/tiles/LocalPlay/Process.Start/secrets; no DTO/PosterUrl/ETag/Range; no new auth/startup write probe/circuit breaker/alerts/counters; no dirty `harness.yml` edit; no deletions/whole-file rewrites; no web UI beyond generated contracts; no changed gate thresholds (FR-010; Q1, Q7; P4, P6, P7, P10, P16)
- [ ] T017 Run Roslyn analyzers on explicit changed `.cs` set via `./scripts/run-roslyn-analyzers.ps1` (FR-012)
- [ ] T018 Run `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml on changed files (FR-012)
- [ ] T019 Run `./scripts/run-jetbrains-inspectcode.ps1` on changed files (FR-012)
- [ ] T020 `dotnet format --verify-no-changes` clean (FR-012)
- [ ] T021 Run affected unit/integration suites then full `dotnet test` (FR-012; SC-001–SC-006)
- [ ] T022 Run property/security/vulnerable-package gates per harness; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS; symlink SKIP remains missing required evidence (FR-012; Q7; SC-006; P15)
- [ ] T023 Mutation: check effective harness eligibility/exclusions at pickup; run `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree; never lower thresholds or edit dirty `harness.yml` to get green; confirm mutation-safe `const`/computed literal discipline (FR-012; Q7; SC-006; P16)
- [ ] T024 Size M code review with Sentry, Ledger and Compass; apply Q1 closing bar (Critical/High/Medium + missing required evidence block; Lows explicit disposition) and 2-round remediation cap; missing axis is blocked; then ship-review after rebase per pipeline — no automatic merge (FR-012; Q1, Q7; SC-006)

---

## Dependencies

- T000 before any T001–T024 coding/verification tasks that mutate production or claim gates.
- Phase 1 before Phase 2–4. T001 ∥ T002; T003 after T002.
- Phase 2: T004 before T005–T007; T005 before T007; T006 can follow T004; T007 after T005–T006.
- Phase 3: T008 ∥ early prep; T009 after T002–T006; T010 after T009 (and T004 topology available for live broker).
- Phase 4: T011 after T002 and T000 policy/handler facts; T012 after T011; T013 after T011–T012 (and consumer available for concurrent-read proofs).
- Phase 5 after endpoint exists (T011–T012).
- Phase 6 after implementation and mandatory proof tasks T003/T007/T010/T013/T015.

## Sequencing example

```text
T000  DEV-17 + drift/policy/validator/platform check (stop if blocked)
T001  ThumbnailRequested.cs
T002  ThumbnailCache.cs (symlink-safe; FileShare; verified promote primitive)
T003  ThumbnailCache unit proofs (planted destination; Windows concurrent streaming)
T004  RabbitMqTopology thumbnail + DLQ + SAC (ordinary dispatch)
T005  TelemetryConstants span consts + RabbitMqThumbnailQueuePublisher (Activity + traceparent/tracestate)
T006  RabbitMqServiceCollectionExtensions active/inactive + singleton ThumbnailCache
T007  EnrichmentConsumer publish-before-ack + coupling/propagation/composition proofs
T008  InactiveThumbnailConsumer.cs
T009  ThumbnailConsumer.cs (non-throwing extract + handoff + U1 tokens/fields + shutdown temp cleanup)
T010  Consumer/DLQ/failure/telemetry/U1/corrupt-traceparent/concurrency proofs
T011  ThumbnailEndpoints.cs ({id:int}; nosniff; single NotFoundException miss; Produces)
T012  ApiEndpoints (+ Program/ServiceDefaults only if T000 requires)
T013  Endpoint HTTP ProblemDetails / nosniff / concurrent-read / composition proofs
T014  OpenAPI regenerate
T015  generate:api + drift
T016–T024  scope confirm + gates + Size M review
```

## MVP

MVP delivers ticket AC 1–3 with the brief proof matrix mandatory: T000, T001–T013, T014–T015, and verification T016–T024. Disabled short-circuit, BinaryNotFound terminal DLQ, GET 200 / uniform ProblemDetails 404 with nosniff, Principle VI propagation/Activity/ActivityLink proofs (including corrupt-`traceparent`), planted-destination/handoff/Windows concurrent proofs, and all four U1 consumer tokens with closed log fields are not optional polish.

## Notes

- `[P]` = different files, no incomplete deps.
- Do not edit files outside the brief Q7 authorized set without a cited decision amendment.
- Quill does not author ADRs or edit constitution. Consume DEV-17 ADR0033 after landing.
- Refresh recon at Phase B pickup for main/contract drift (brief).
- Operator runbook (`BinaryNotFound` disable-or-repair/restart/DLQ purge; disk monitoring/manual purge) is documented in spec/plan only — no automated monitoring project (P6, P7).
- TelemetryConstants member names finalized at T005 against T000-landed pattern (P17); SC-006 cited on T021–T024 for optional Compass Low-1 polish (P18).
