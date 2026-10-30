# Implementation Plan: Consume thumbnail requests and serve cached JPEGs

**Branch**: `feature/428-spec` | **Date**: 2026-10-30 | **Spec**: `specs/DEV-428/spec.md`
**Input**: Frozen `brief.md` (Q1–Q7 + Post-grill Analyze Ruling U1 + Phase 2 Step 7 Patron rulings P1–P20); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; `spec.md`; Maestri notes `DEV-428`, `recon-DEV-428`, `recon-DEV-428-2`, `adjudication-DEV-428-plan`, `adjudication-DEV-17-plan`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only; Quill does not edit constitution or ADRs)
**Status**: Draft — Phase 2 analyze round 1 repair (C1/C2/U1) plus Phase 2 step 7 plan-challenge repair (Quill 8-item list / P1–P20). Gate 1 not approved; PLAN FROZEN not declared; awaiting Keel re-challenge verification. This repair does not claim plan freeze or Gate 1.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md` (Quill). No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief; public contract is the single new GET route regenerated through existing tooling. No new ADR (consume DEV-17 ADR0033 after landing).

## Summary

Deliver Api-hosted thumbnail consumption and JPEG serving for posterless local movies. After enrichment, publish `ThumbnailRequested` to durable quorum queue `thumbnail` (own DLQ `thumbnail.dead-letter`, no retry queue, SAC + prefetch 1 under ordinary broker dispatch; rare failover overlap is benign last-writer-wins) with mandatory `traceparent`/`tracestate` injection and a `TelemetryConstants`-named producer Activity. `ThumbnailConsumer` extracts propagation via non-throwing fallback, starts a consumer Activity (`ActivityLink` on redelivery), immediately canonicalizes/revalidates the source under `LibraryOptions.RootPath` (file + ancestor symlink/reparse rejection; residual name-based TOCTOU documented under trusted operator directories), calls DEV-17 `IThumbnailGenerator`, atomically promotes a contained GUID JPEG to canonical `{movieId}.jpg` via singleton `ThumbnailCache` (symlink-safe; SOI before promote), and acknowledges. When enabled, publish/topology failure intentionally prevents enrichment ack and redrives enrichment via existing retry/DLQ (confirm-before-ack retained; no new outbox). Terminal failures use closed U1 consumer tokens plus closed generator tokens and the closed `{MovieId}`/`{FailureReason}`/`{Redelivered}`/`{ErrorType}` field set. `GET /api/movies/{id:int}/thumbnail` inherits landed `LibraryEndpoints` caller policy (T000 verifies), streams the canonical file as `image/jpeg` with serve-time SOI + `X-Content-Type-Options: nosniff` on 200 (`FileShare.ReadWrite | FileShare.Delete`), or returns uniform sanitized 404 RFC 7807 `ProblemDetails` via one `NotFoundException` → `GlobalExceptionHandler` mechanism with `.Produces(200, image/jpeg)` / `.ProducesProblem(404)`. Gated by `Thumbnails:Enabled` default false via startup `IOptions<ThumbnailOptions>.Value` (restart to toggle; inactive consumer). Operator runbook covers `BinaryNotFound` disable-or-repair/restart/DLQ purge and disk monitoring/manual purge; shutdown best-effort cleans own contained temp before requeue. Fake-generator and live-broker proofs cover the brief matrix including C1/C2/U1 and P1–P20. No new packages, schema, Core interfaces, eviction/sweeper/alerts/counters/circuit breaker, tiles, LocalPlay coupling, or harness edits.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).
**Primary Dependencies**: Existing pins only — RabbitMQ.Client, ASP.NET Core Minimal APIs, Options validation, DEV-17 `IThumbnailGenerator` / `ThumbnailOptions` / `AddLamuFlixThumbnails` after DEV-17 lands on main. **No new packages** (brief §2.3.1).
**Storage**: Operator-controlled local filesystem under trusted `ThumbnailOptions.CacheDirectory`; authoritative movie rows in existing PostgreSQL (no new tables). Library roots from existing `LibraryOptions.RootPath`.
**Testing**: xUnit v3, Shouldly, NSubstitute; Testcontainers RabbitMQ + PostgreSQL; fake `IThumbnailGenerator` emitting tiny SOI-valid JPEG stubs. Unique per-test temp dirs with deterministic cleanup; required symlink proofs in capable environment (SKIP = missing required evidence, never PASS). No live ffmpeg/Dockerfile duplication of DEV-17. Real DB when persistence/query translation matters; never mock the data-access driver.
**Target Platform**: .NET server (ASP.NET Core Api host that already hosts enrichment/features consumers). Windows file-sharing and replacement primitives verified at T000/T002; concurrent read-during-promotion proved at T003/T013 — not claimed already passed.
**Project Type**: Core message record + Infrastructure RabbitMQ consumer/publisher/cache helper + Api Minimal API binary endpoint.
**Performance Goals**: one active thumbnail generation under ordinary broker SAC + prefetch 1 (rare overlap last-writer-wins); GET streams with `FileShare.ReadWrite | FileShare.Delete` without sharing a generation lock or unbounded retry. No invented rate-limiter package.
**Constraints**: `TimeProvider` only where time is needed; no explanatory comments beyond allowed exemptions; complexity ≤ 15 then refactor gate per `harness.yml`; never hardcode/lower thresholds; no `Features:LocalPlay` / `Process.Start` / secrets changes; no hand-edited generated OpenAPI/TS; sanitized logs only (closed U1 consumer tokens + closed generator tokens; closed fields `{MovieId}`, `{FailureReason}`, `{Redelivered}`, `{ErrorType}`; zero raw paths); Principle VI mandatory `traceparent`/`tracestate` with non-throwing extract fallback, consumer Activity, redelivery `ActivityLink`, movie-id error context; miss HTTP is uniform sanitized ProblemDetails 404 with `traceId`; mutation-safe `const`/computed literals and zero mutable static initialization; at-least-once delivery documented; intentional publish-failure ↔ enrichment-redelivery coupling when enabled.
**Scale/Scope**: personal library; supported default one Api instance with persistent operator-controlled cache and library directories; Size M.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` on this worktree. Brief: no new ADR for DEV-428; consume DEV-17 ADR0033 after landing; no constitution departure approved; no owner structural checkbox identified (P20 informational ProblemDetails/`nosniff` refinement preserves ticket 404). Phase B pickup must verify DEV-17 (including Principle I `IThumbnailGenerator` / Principle VII `ThumbnailOptions` as landed) before coding. Quill invents no amendment path.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Ports in Core; adapters in Infrastructure; closed port list / ADR on **new** ports | **PASS**. No new Core port interface. Add message record `ThumbnailRequested` in Core/Ports (same pattern as `FeaturesRequested` / `EnrichmentRequested`). Consume existing DEV-17 `IThumbnailGenerator` after it lands. Concrete Infrastructure publisher/consumer/cache — no new architectural service layer. Consumer private helpers for eligibility/source; cache owns containment/SOI/open/promotion (P13). |
| II (Handlers / API failures) | Domain failures mapped at API; expected exceptions | GET hit returns 200 `image/jpeg` + `nosniff`; GET misses throw existing `NotFoundException` mapped by the single `GlobalExceptionHandler` + `AddProblemDetails()` (verified at T000 against `LibraryEndpoints`; if drifted, one landed helper only — never two mechanisms) — no new domain exception types for cache misses; consumer terminal paths use broker reject + sanitized U1 logs, not API exception mapping (P12). |
| V (Validated Inputs / Consistent Error Responses) / API and Contract Rules | Every error response is RFC 7807 `ProblemDetails` from `AddProblemDetails()` + one `IExceptionHandler`, includes `traceId`; C# → OpenAPI → generated TS; no hand-edits | New `GET /api/movies/{id:int}/thumbnail` (§2.3.4 ruled in Q3; P11). Inherit landed `LibraryEndpoints` caller-access policy (T000 verifies; no new auth) (P1). **Miss paths keep decided 404 status** and MUST emit uniform sanitized `application/problem+json` `ProblemDetails` (`status`/`title`/`type` identical across miss classes; only per-request `traceId` and optional `instance` may vary) (C2; P1). Never bare `Results.NotFound()` empty bodies; remove competing optional miss mechanisms. Success remains streamed `image/jpeg` after serve-time SOI with `X-Content-Type-Options: nosniff` (P8). Declare `.Produces(200, "image/jpeg")` and `.ProducesProblem(404)`. Regenerate `web/src/api/openapi.json` then `types.ts` via established opt-in OpenAPI workflow + `npm run generate:api` / drift checks. No DTO/`PosterUrl` changes. TypedResults/ProblemDetails assertions required in endpoint miss proofs (C2; P12). |
| VI (Observability Across Processes) | Full Principle VI: publishers inject `traceparent`/`tracestate` via `Propagators.DefaultTextMapPropagator`; consumers extract and start a consumer Activity; redelivery uses `ActivityLink`; span names/attribute keys live in `TelemetryConstants` (no ad-hoc literals); structured error/processing events carry movie id; existing `/health/live` + `/health/ready` remain — no new external dependency health registration for this ticket; ServiceDefaults owns Serilog/OTel wiring | **PASS with full checks restored (C1) + non-throwing extract (P9).** `RabbitMqThumbnailQueuePublisher` injects mandatory `traceparent`/`tracestate` via existing `TraceContextCarrier.Inject` and starts a producer Activity whose name is a `const` in `TelemetryConstants`, tagged with `TelemetryConstants.MovieId`. `ThumbnailConsumer` extracts headers with non-throwing fallback (corrupt headers never bypass terminal `MalformedMessage` DLQ), starts a consumer Activity, uses `ActivityLink` on redelivery, tags `MovieId` + `MessagingDeliveryCount`, and marks errors with movie-id context + `ErrorType`. Structured terminal logs use closed U1 tokens/fields (no raw paths/stdout/stderr). No bespoke health checks or custom metric instruments (Q6); Activity span-name constants only (identifiers aligned at T005 to landed pattern). Proofs cover inject/extract, Activity, redelivery link, error movie-id context, and corrupt-`traceparent` malformed handling (C1; FR-003, FR-004, FR-011; P9). |
| VII (Configuration) | Validated options; no new secrets | Reuse DEV-17 `ThumbnailOptions` and existing `LibraryOptions` validation/wiring; both consumer and endpoint read `IOptions<ThumbnailOptions>.Value` (startup snapshot; restart to toggle; no `IOptionsMonitor` asymmetry) (P11). DEV-428 registers singleton `ThumbnailCache` in `RabbitMqServiceCollectionExtensions`; resolve via DI with `IOptions` only (never raw `IConfiguration`) (P14). Edit ServiceDefaults/`Program.cs` only if T000 shows existing-option wiring requires it. No new options class. No invented startup filesystem write probe or custom root-restriction validator; T000 records exact landed validator rules and gaps (P10). |
| LocalPlay / Process.Start | Gated playback only | Generator registration remains DEV-17-owned and independent of `Features:LocalPlay`. DEV-428 adds no playback or process-start surface (Q6). |
| VIII (Ubiquitous Language) | Vocabulary consistency | Use brief vocabulary: Poster vs Thumbnail vs Thumbnail request vs Generated output vs Canonical cache. No root glossary rewrite. |
| IX (Test Pyramid) | Real broker/DB where behaviour depends on them; no mocked driver translation | Live RabbitMQ for queue/DLQ/SAC proofs; PostgreSQL when persistence involved; fake generator for consumer/endpoint matrix; unique temp dirs; required symlink proofs in capable environment; no DEV-17 real-tool duplication (P15). |
| Tech stack (RabbitMQ) | Quorum queues; publisher confirms; topology in `RabbitMqTopology` | Extend topology for `thumbnail` + `thumbnail.dead-letter`; publisher confirms before enrichment ack (intentional non-ack coupling on publish failure when enabled); SAC + prefetch 1 under ordinary dispatch (Q4, Q5; P4, P5). |
| Governance / Gates | Analyzers, complexity, InspectCode, format, vulnerable packages, mutation | Phase B only; see tasks verification phase. Preserve unrelated dirty `harness.yml`. Mutation-safe `const`/computed literals; no changed thresholds (P16). |

### Complexity Tracking

No constitution violations requiring justification. No owner checkbox. If Phase B pickup finds DEV-17 missing or contract drift, stop for a cited decision — do not invent a workaround.

## Approach

1. **Pickup drift check (before coding)**: Verify DEV-17 on main (generator port/result/options/DI/ADR0033/constitution/architecture whitelist as landed). Record exact landed facts for `LibraryEndpoints` access policy, `NotFoundException`/global-handler 404 mechanism, `ThumbnailOptionsValidator` / `LibraryOptions` startup rules (and exact gaps — no invented probe/root validator), .NET 10 target-platform replacement primitive, and whether `Program.cs` / `ServiceDefaults` need conditional touch. Compare trigger/validator/endpoint/test seams with frozen evidence. Run read-only `/speckit-analyze` for pickup drift. Cited decision required if facts drift (brief task ordering item 1; P1, P10, P12).
2. **Message + cache**: Add `ThumbnailRequested`; implement `ThumbnailCache` with path containment, symlink/reparse rejection on temp/canonical/ancestors, SOI before promote and on open, `FileShare.ReadWrite | FileShare.Delete` reads, atomic promote using T000-verified platform primitive; unit proofs for path/header/promotion/preserve-on-failure/planted-destination/Windows concurrent streaming (`CachePromotionFailed` surfaces to consumer) (P2, P5, P8).
3. **Topology + publisher trigger**: Declare `thumbnail` / `thumbnail.dead-letter` (SAC on requested; no retry queue); add Principle VI thumbnail span-name `const`s to `TelemetryConstants` (align identifiers to landed pattern at T005); `RabbitMqThumbnailQueuePublisher` with producer Activity + mandatory `TraceContextCarrier.Inject`; post-enrichment publish from `EnrichmentConsumer` with confirmation before ack and documented intentional publish-failure → enrichment non-ack coupling; composition proofs for enabled/disabled publication and header propagation (P4, P17).
4. **Consumer**: `ThumbnailConsumer` / `InactiveThumbnailConsumer`; non-throwing extract + consumer Activity + redelivery `ActivityLink`; eligibility/source private helpers (`IneligibleRequest` / `UnauthorizedSourcePath` with immediate canonicalize/revalidate + ancestor symlink rejection); generator call (exact generator tokens); promotion via shared singleton cache (`CachePromotionFailed`); malformed body + corrupt-`traceparent` (`MalformedMessage`); closed U1 field set; terminal failure/DLQ; shutdown requeue with own-contained-temp best-effort cleanup; SOI cache-hit; live-broker proofs including telemetry and U1 token/field matrix (P3, P7, P9, P13).
5. **Endpoint + DI**: Register singleton `ThumbnailCache` in `RabbitMqServiceCollectionExtensions`; `ThumbnailEndpoints` + `ApiEndpoints` map `GET /api/movies/{id:int}/thumbnail`; inherit access policy; enabled/existence/canonical-only serve with serve-time SOI + `nosniff`; one `NotFoundException` miss path + OpenAPI Produces metadata; concurrent-read proofs; T006/T007/T013 prove options/validation/registration including broker-disabled HTTP availability; optional `Program.cs` / `ServiceDefaults` wiring only if T000 requires (P1, P8, P11, P12, P14, P19).
6. **Contracts**: Regenerate OpenAPI baseline + TypeScript via repository tooling; drift checks; no web UI.
7. **Ops documentation (in-spec only)**: `BinaryNotFound` disable-or-repair/restart/DLQ inspect-purge runbook; disk monitoring and manual purge with host stopped or disabled after restart; no sweeper/eviction/alerts/counters/circuit breaker (P6, P7).
8. **Verify + review**: Focused then full suite; analyzer/complexity/InspectCode/property/vulnerable/format/mutation per harness; Size M three-axis review under Q1/Q7 bar.

## Project Structure

```text
specs/DEV-428/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md

src/LamuFlix.Core/
  Ports/ThumbnailRequested.cs                 (new)
  Pipeline/TelemetryConstants.cs              (edit: Principle VI thumbnail producer/consumer span-name consts only; C1; P17)

src/LamuFlix.Infrastructure/
  RabbitMq/ThumbnailConsumer.cs               (new; Activity extract fallback + ActivityLink + U1 logging + private eligibility/source helpers)
  RabbitMq/InactiveThumbnailConsumer.cs       (new)
  RabbitMq/RabbitMqThumbnailQueuePublisher.cs (new; Activity + TraceContextCarrier.Inject)
  RabbitMq/RabbitMqTopology.cs                (edit: thumbnail + DLQ declare/bind + SAC args)
  RabbitMq/RabbitMqServiceCollectionExtensions.cs (edit: active/inactive registration + singleton ThumbnailCache)
  RabbitMq/EnrichmentConsumer.cs              (edit: post-enrichment publish before ack)
  Thumbnails/ThumbnailCache.cs                (new; beside DEV-17 thumbnails after land; containment/SOI/open/promotion)

src/LamuFlix.Api/
  Endpoints/ThumbnailEndpoints.cs             (new; {id:int}; NotFoundException miss; Produces metadata; nosniff)
  Endpoints/ApiEndpoints.cs                   (edit: map thumbnail endpoints)
  Program.cs                                  (edit only if T000 shows cache/endpoint/options wiring needs it)

src/LamuFlix.ServiceDefaults/
  Extensions.cs                               (edit only if T000 shows existing-option validation/wiring needs it)

web/src/api/
  openapi.json, types.ts                      (regenerated only)

tests/LamuFlix.UnitTests/                     (focused ThumbnailCache / composition / authorization proofs; unique temp dirs; symlink in capable env)
tests/LamuFlix.IntegrationTests/              (live broker consumer/DLQ; endpoint 200/404; Windows concurrent streaming; factories as needed)
```

**Layout decisions (brief Q7 file boundary)**:

- Exact new production paths and bounded edit set are Patron-authorized under §2.3.6; no expansions without a cited decision.
- Test files land in existing unit/integration projects using existing factory/composition seams; no new test project or package.
- Quill owns `spec.md` / `plan.md` / `tasks.md` / `DRAFTING_RECEIPT.md`. Quill does not edit `brief.md` / `CONCLUSIONS.md` / constitution / ADR bodies / production code in this Phase A ask.

## Test Strategy

| Surface | Coverage |
|---|---|
| ThumbnailCache unit | Canonical path; containment reject without collateral deletes; symlink/reparse on temp/canonical/ancestors and planted-destination (outside untouched, prior regular JPEG preserved); SOI valid/invalid before promote and on open; atomic promote via T000-verified primitive; failed promote preserves prior JPEG; own-temp cleanup only; `FileShare.ReadWrite \| FileShare.Delete` open; unique temp dirs + deterministic cleanup |
| Topology / composition | Queue/DLQ declaration; SAC argument (ordinary dispatch); inactive vs active registration; singleton `ThumbnailCache` + `IOptions` resolution by consumer and endpoint; disabled skips publication/subscription; GET available when broker disabled; existing broker gating; no LocalPlay coupling; startup `IOptions.Value` (restart for flag) |
| Publisher / enrichment | Eligible publish after persistence with confirm-before-ack; intentional publish-failure → enrichment non-ack / existing retry-DLQ coupling; `traceparent`/`tracestate` injected; producer Activity + MovieId tag; poster-present / no-local-source suppress; disabled no publish |
| Consumer (fake generator + live broker) | Enabled path → canonical JPEG; non-throwing extract + consumer Activity + redelivery ActivityLink + error movie-id context; immediate handoff canonicalize/revalidate; stale/unauthorized/traversal/symlink/sibling-prefix/ancestor-escape reject as `IneligibleRequest`/`UnauthorizedSourcePath`; all closed generator tokens; all four U1 consumer tokens; closed `{MovieId}`/`{FailureReason}`/`{Redelivered}`/`{ErrorType}` fields and omission rules; zero raw paths/payload/process output; BinaryNotFound single sanitized log + terminal DLQ no retry; malformed body + corrupt-`traceparent` → `MalformedMessage` DLQ; promotion fail → `CachePromotionFailed` preserves prior JPEG; shutdown cancellation requeue with own-contained-temp best-effort cleanup without failure log; SOI cache-hit skip; serial/SAC under ordinary dispatch; last-writer-wins overlap documented |
| Endpoint HTTP | 200 exact bytes + image/jpeg + nosniff after serve-time SOI; uniform sanitized ProblemDetails 404 with `traceId` (no path/miss-reason) for unknown/nonpositive/missing/unreadable/corrupt-SOI and disabled-even-with-JPEG via single miss mechanism; OpenAPI Produces metadata; inherited access policy after T000; no generation/source access; concurrent reads during promotion on target platform (T013); no claim these runtime checks already passed |
| Contract | OpenAPI/TS regeneration + drift checks |
| Ops (documented, not automated) | BinaryNotFound disable-or-repair/restart/DLQ purge runbook; disk monitoring/manual purge; no sweeper/eviction/alerts/counters/circuit breaker |

No Gherkin unless separately opted in. Property tests only if harness requires for changed surface — report SKIP/SKIPPED/N/A distinctly. Symlink environment SKIP is missing required evidence, never PASS.

## Gates (Phase B — after implementation)

1. Explicit changed-C# scope: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness, `./scripts/run-jetbrains-inspectcode.ps1`.
2. Affected unit/integration suites, then full `dotnet test`.
3. Property / vulnerable-packages / format per harness; distinguish PASS, failure, SKIPPED (scope-empty), SKIP, mutation N/A, Could not run.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` from worktree); check eligibility/exclusions at pickup — do not touch dirty `harness.yml`; mutation-safe `const`/computed literals; never lower thresholds.
5. Contract drift checks after regeneration.
6. Size M review: Sentry, Ledger, Compass; Q1 closing bar; two remediation rounds max; missing axis blocks.

Phase A (this draft) runs none of the build/test gates and claims no gate result.

## Risks

- **DEV-17 not on main at pickup**: Phase B blocked until dependency lands; do not re-implement generator (brief).
- **Path escape / symlink**: mitigated by RootPath authorization with immediate handoff canonicalize/revalidate and file+ancestor symlink/reparse rejection before GenerateAsync; residual name-based TOCTOU under trusted operator directories accepted, not claimed eliminated (Q3; P2, P3).
- **Promotion race / partial write / planted destination**: mitigated by symlink-safe atomic promote + preserve-on-failure + outside-target untouched; T000/T002 verify platform primitive (Q2; P2, P5).
- **SAC failover overlap**: ordinary dispatch only; rare overlapping generation is benign last-writer-wins — do not claim global exclusive activation across partitions (P5).
- **Windows GET vs promote contention**: mitigated by `FileShare.ReadWrite | FileShare.Delete`; proved at T003/T013 on target platform — not claimed already passed (P5).
- **Exactly-once expectation**: at-least-once only; duplicates possible after enrichment redelivery — mitigated by SOI cache-hit skip (Q4, Q5). Publish-failure when enabled intentionally couples to enrichment redrive (P4).
- **Multi-instance split brain cache**: documented operator-shared storage requirement; no distributed feature (Q6).
- **Cache growth / BinaryNotFound DLQ**: operator disk monitoring + manual purge; BinaryNotFound disable-or-repair/restart/DLQ runbook; no sweeper/circuit breaker (P6, P7).
- **Harness dirty file**: preserve unrelated `harness.yml` edit; mutation policy checked at pickup (brief).
- **Binary endpoint novelty**: first image/jpeg stream route — keep contract minimal (no caching headers) per Q3; serve-time SOI + nosniff per P8.

## Ordering

See `tasks.md`. Sequence: pickup DEV-17/drift/policy/validator/platform check → `ThumbnailRequested` + `ThumbnailCache` + unit proofs → topology/composition + singleton cache DI + publisher trigger → `ThumbnailConsumer` → `ThumbnailEndpoints` → OpenAPI/TS regeneration → focused & full verification → Size M review. Same-file edits sequenced. Tests land with each step.
