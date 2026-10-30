# DEV-428 — Phase 2 frozen grill brief

Status: grill closed by Patron at Q7/12 on 2026-10-30; ready for Quill drafting. This is the decision baseline, not a specification, implementation, plan-challenge verdict, or Gate 1 approval.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-428-spec`; branch: `feature/428-spec`. Recon HEAD: `477ac3934f93a09bd0e78e95b7b5ca04bd92aa84` (recon-DEV-428-2:6-7). The existing uncommitted `harness.yml` mutation-exclusion deletion is unrelated and must be preserved.

Decision authority: Patron. Full question/recommendation/answer exchanges are preserved append-only in [CONCLUSIONS.md](CONCLUSIONS.md). Every full Patron answer is reproduced below. [ASSUMPTIONS.md](ASSUMPTIONS.md) records the absence of taste assumptions; no UI or copy decisions are introduced.

## Evidence and precedence

- `recon-DEV-428:75-104`: verbatim ticket description, Scope & Technical Design, acceptance criteria, dependency, exclusions.
- `recon-DEV-428:18-50`: existing consumer, host, repository, endpoint, generated contract, and test patterns.
- `recon-DEV-428-2:13-126`: complete DEV-17 generator contract, including failure tokens, options, output naming, registration, and consumer ownership constraints.
- `recon-DEV-428-2:146-151`: verified substance of the downstream constraint comment, corroborated by `adjudication-DEV-17-plan:99,113`. Verbatim tracker comment is unavailable; it is not represented as retrieved.
- Q1-Q7 below are Patron rulings. Patron supplied additional root/handler file anchors in Q3/Q4/Q7; these are cited ruling inputs, not independent Keel recon. Recheck them at Phase B pickup.
- Q5 supersedes Q4 requested-queue spelling. Effective requested queue is **`thumbnail`**, not `thumbnail.requested`; DLQ is `thumbnail.dead-letter`. Preserve Q4 as historical evidence, not an alternative contract.
- Earlier needs-recon entries in CONCLUSIONS.md are historical; the supplied follow-up recon and Q2-Q7 resolved the grill blockers.

## Ticket delivery and frozen scope

Deliver the DEV-428 Scope & Technical Design items 1-6 and all three acceptance criteria (recon-DEV-428:84-95):

1. Queue `thumbnail`, with its own DLQ on the existing RabbitMQ exchange conventions; `ThumbnailRequested(MovieId MovieId, string FilePath)` in Core/Ports.
2. Api-hosted `ThumbnailConsumer`, calling DEV-17 `IThumbnailGenerator`, opt-in through `Thumbnails:Enabled` default false; inactive variant when disabled.
3. `GET /api/movies/{id}/thumbnail`: cached `image/jpeg` or 404.
4. Publish only after enrichment for a movie without a poster and with a local file.
5. Record a generator failure once per terminal handling attempt. Missing binary is permanent and never enters a retry loop.
6. Fake-generator consumer proof and endpoint 200/404 proof, expanded only by the load-bearing cases settled below.

Acceptance criteria, verbatim:

- With `Thumbnails:Enabled=true`, a movie with no poster gets a cached JPEG, and the endpoint returns it.
- With the flag off, no thumbnail message is consumed and the endpoint returns 404.
- A missing ffmpeg binary logs one permanent failure and no retry loop starts.

DEV-17 owns generator, process runner, process execution, Docker ffmpeg installation, shared ThumbnailOptions and their existing validation/registration. DEV-429 owns tiles. Cache eviction, expiry, orphan sweeping, distributed storage/provisioning, new packages, DB schema/tables, new Core interfaces, new project/layer, root glossary rewrites, and unrelated harness edits are excluded. No playback implementation or `Features:LocalPlay` changes. No new ADR is required for this M ticket; consume DEV-17 ADR0033 after its landing. For this frozen scope, anything else is a follow-up issue, not a finding in this round.

## Loop discipline

- Grill cap: 12 questions; closed after 7, with explicit shared understanding in Q7.
- Code-review/ship-review closing bar: Critical, High and Medium findings block; missing required evidence also blocks. Every Low needs an explicit disposition.
- Review/repair cap: two rounds, followed by hard stop if blockers remain. The cap does not waive a blocker or authorize a third round.
- M-ticket adjudication requires Sentry, Ledger and Compass axis reports. Missing axis is blocked, never skipped.
- No owner structural checkbox was identified by Patron; no constitution departure was approved. Gate 1 remains closed until its separate owner action. If later drafting discovers a ticket contradiction or necessary constitution departure, use the prescribed owner checkbox; this brief does not waive it.
- No production code, tests, hosted checks, mutation result, PR review, merge bar, or merge is claimed by this document.

## Settled approach

Use a concrete `ThumbnailCache` helper in Infrastructure/Thumbnails for canonical cache paths, containment, SOI checks, file reads and atomic promotion. The consumer owns movie eligibility/source authorization and calls the existing Core generator port; no additional port or architectural service layer.

Use the existing DEV-17 signature unchanged: `Task<ThumbnailResult> GenerateAsync(string sourcePath, CancellationToken cancellationToken = default)` and `ThumbnailResult(bool Success, string? CachePath, string? FailureReason)`. Preserve its closed failure tokens and cancellation behavior; do not leak raw exception/path/process output. DEV-17 writes a random GUID JPEG under trusted CacheDirectory; DEV-428 validates containment and atomically promotes to `{movieId}.jpg`. Promotion failure preserves an existing canonical JPEG and cleans only its own contained temporary output. A duplicate can skip generation only for a readable canonical JPEG with length >= 2 and SOI `0xFF 0xD8`; otherwise regenerate.

The consumer loads the authoritative movie, rejects nonexistent/stale/ineligible requests, compares queued FilePath to current movie Path, and authorizes the source beneath operator-configured `LibraryOptions.RootPath` using separator-aware OS comparison and rejection of traversal, sibling-prefix and symlink/reparse escapes. Broker paths are not authorization. Trust cache/tool/root operator configuration, never substitute request-controlled paths.

Declare durable quorum `thumbnail` and `thumbnail.dead-letter`, no thumbnail retry queue. Set `x-single-active-consumer=true`, prefetch 1 and serial handling. Preserve at-least-once delivery: no cross-crash deduplication or exactly-once guarantee. Acknowledge after canonical promotion or eligible validated cache hit. Malformed/invalid message, failed generator result, or promotion failure gets one sanitized failure for that handling attempt and terminal rejection without requeue to DLQ. Host shutdown cancellation requeues without generator-failure logging. Follow existing broker reconnection behavior; no automatic DLQ redrive.

Publish with a concrete RabbitMqThumbnailQueuePublisher from EnrichmentConsumer after successful enrichment persistence, only when thumbnails enabled, poster absent and local source present. Require publisher confirmation before enrichment acknowledgement. No new outbox table/Core queue interface and no Infrastructure dependency in a Core handler. Publisher failure must not acknowledge enrichment; its existing failure/redelivery mechanics apply. Redelivery can duplicate publication.

GET checks enabled before any cache I/O and checks movie existence before serving only the canonical file. Return 200 image/jpeg with asynchronous per-request disposed streaming, or 404 for disabled/unknown/invalid id or missing/unreadable cache. Never generate or access source media on GET. No DTO/PosterUrl changes, ETag/Cache-Control/Range policy, path leakage, or manual edits to generated contracts. Reads are independent of broker state and do not share a generation lock.

Supported default is one Api instance with persistent operator-controlled cache and library directories. Multiple instances require operator-shared backing storage for both; no distributed availability guarantee or storage provisioning. When disabled, no publication, active thumbnail subscription, generator call, source check or cache I/O even if a JPEG already exists. Active subscriptions also respect existing broker activation rules. Use existing validation and logging conventions, no bespoke health infrastructure, counters or orphan sweeper.

U1 logging decision (Patron, post-grill analyze): non-generator terminal outcomes use only `MalformedMessage`, `IneligibleRequest`, `UnauthorizedSourcePath`, or `CachePromotionFailed`; generator failures retain the exact existing closed `ThumbnailResult.FailureReason` tokens. Terminal failure fields are closed to `{MovieId}`, `{FailureReason}`, `{Redelivered}`, and `{ErrorType}` with zero raw paths, payload bodies or process output. The full U1 ruling reproduced below defines token mapping, field omission and exception sanitization. Basis: note `DEV-428:45-48` and `CONCLUSIONS.md`, Post-grill Analyze Ruling U1. This refines Q6 logging and Q7 failure proofs; it does not change the terminal DLQ or cancellation decisions.
## Approved files and editing boundary

New production files (Q7):

- `src/LamuFlix.Core/Ports/ThumbnailRequested.cs`
- `src/LamuFlix.Infrastructure/RabbitMq/ThumbnailConsumer.cs`
- `src/LamuFlix.Infrastructure/RabbitMq/InactiveThumbnailConsumer.cs`
- `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqThumbnailQueuePublisher.cs`
- `src/LamuFlix.Infrastructure/Thumbnails/ThumbnailCache.cs`
- `src/LamuFlix.Api/Endpoints/ThumbnailEndpoints.cs`

Bounded existing-file edits deliberately authorized by Patron under section 2.3.6:

- `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` solely for C1 Principle VI thumbnail producer/consumer span-name `const` members; no ad-hoc telemetry-name literals or new metric instruments. I1 closure records this constitution-required boundary explicitly, consistent with spec FR-003/FR-004/FR-010, plan Principle VI / Project Structure, and task T005 (Conductor instruction, 2026-10-30).
- `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs`
- `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqServiceCollectionExtensions.cs`
- `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs`
- `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs`
- `src/LamuFlix.Api/Program.cs` only if cache/endpoint registration needs it.
- `src/LamuFlix.ServiceDefaults/Extensions.cs` only if existing-option validation/wiring needs it.
- `web/src/api/openapi.json` and `web/src/api/types.ts` only through existing generation commands, preserving C# -> OpenAPI -> generated TypeScript.

Add focused tests in the existing unit/integration test projects and existing factory/composition seams as needed for the proof matrix. No test project/package change is authorized. Quill drafts spec.md, plan.md and tasks.md from this brief; Keel does not draft those artifacts. No production deletions or whole-file rewrites, no editing dirty harness.yml, no unrelated cleanup.

## Test strategy and acceptance evidence

Use existing xUnit v3, Shouldly, NSubstitute and Testcontainers RabbitMQ/PostgreSQL. Fake generator writes a small SOI-valid JPEG fixture; real ffmpeg and Dockerfile proof remains DEV-17-owned. Use real DB when query translation or persistence is involved; never mock the data-access driver.

| Behavior | Required proof |
| --- | --- |
| Ordinary enabled path | Persist enrichment for eligible movie -> publisher-confirmed queue message -> fake generator -> canonical JPEG -> GET 200 with exact bytes and image/jpeg. |
| Default false / flag off | No publication, subscription, generation, source validation or cache I/O; GET 404 even when cached JPEG exists. |
| Trigger predicate | Poster-present and no-local-source movies publish nothing; consumer revalidates current eligibility and queued/current path equality. |
| Endpoint misses | Unknown/invalid id, missing/unreadable cache return 404; GET performs no generation or media-source access. |
| Source trust | Outside-root, traversal, sibling-prefix, symlink/reparse escape and stale broker path rejected before generator call. |
| Cache trust / atomicity | Output path outside cache rejected without deleting unrelated files; failed promotion preserves prior canonical JPEG; cache-hit SOI valid/invalid branches. |
| Failure contract | All closed generator failure tokens; BinaryNotFound gives one sanitized terminal failure and DLQ entry with no retry loop. Prove all four U1 consumer token mappings, the closed MovieId/FailureReason/Redelivered/ErrorType field set and omission rules, and absence of raw paths, payload bodies, exception path text and process output. Malformed messages and storage/promotion failures also terminal. |
| Delivery | Publisher confirmation precedes enrichment acknowledgement; publication failure does not ack; host shutdown cancellation requeues without failure log; eligible valid-cache duplicate skips generation. |
| Concurrency | Broker queue arguments, prefetch and serial processing; second consumer cannot become concurrently active in ordinary broker dispatch; independent asynchronous HTTP reads while generation proceeds. |
| Composition / contract | Inactive variant and existing global broker gating; no Features:LocalPlay coupling; C# OpenAPI/TS regeneration and drift checks. |

Integration proofs inspect live broker requested/DLQ behavior and authoritative persistence using existing fixtures. Do not replace them with fake-broker assertions. Do not repeat DEV-17 real-tool test ownership.

## Task ordering and gates

1. At Phase B pickup verify DEV-17 landed on main, then compare main/contract/ADR/constitution/options/DI and existing trigger/validator/endpoint/test seams with this frozen evidence. Run read-only speckit-analyze after Quill drafting and again for pickup drift. If contract facts drift, obtain a cited decision before implementation.
2. Add ThumbnailRequested and ThumbnailCache with focused path/header/promotion proofs.
3. Add topology, activation and publisher-confirmed post-enrichment trigger proofs.
4. Implement ThumbnailConsumer / inactive composition and terminal failure/cancellation/authorization proofs.
5. Implement endpoint and HTTP/composition/concurrent-read proofs.
6. Regenerate OpenAPI and TypeScript through repository tooling, with no web UI work.
7. Run focused tests, then full repository verification and contract drift checks, followed by required review stages.

Phase A here: saved decision/brief consistency only. Quill drafting, speckit-analyze and plan challenge are later distinct steps. Do not run or claim production gates for these decision documents.

Phase B expectations from Q7: Roslyn analyzers, complexity, InspectCode, property tests, vulnerable-package scan, full dotnet test, mutation according to effective harness policy and generation drift. Never lower thresholds or change exclusions to get green. Mutation eligibility/exclusions and exact script flags must be checked at pickup, not inferred from the unrelated dirty harness edit. Exit 0 alone is PASS; exit 2 is the actual SKIPPED (scope-empty), opt-out SKIP, or mutation N/A; Could not run is not PASS. Preserve actual receipts. Then review/ship-review under the two-round bar and all three M axes; no automatic merge.

## Canonical vocabulary and handoff

Poster: existing external PosterUrl metadata. Thumbnail: local generated JPEG fallback for a posterless movie. Thumbnail request: the movie-id/source-path broker message, not image bytes. Generated output: DEV-17-owned GUID JPEG; canonical cache: DEV-428-owned movie-id JPEG after promotion. These distinctions do not require a new root glossary or rewrite. Consume existing ADR0033 without changing DEV-17 scope. Quill should use these terms consistently in the specification and must not contradict the upstream contract/ADR.

The next step is Quill drafting from this brief, followed by Keel read-only consistency analysis and plan challenge. Gate 1, main dependency landing, implementation, verification, review clearance, PR and merge remain separate states.

## Every Patron grill answer (verbatim full rulings)

The corresponding complete questions and transport exchanges are in CONCLUSIONS.md. These answers are part of the frozen brief, including explicit supersession noted above.

### Q1/12

Verdict: Confirmed — frozen scope and standing review loop approved as recommended.

- **Scope & AC**: Deliver verbatim DEV-428 Scope & Technical Design items 1–6 (`thumbnail` queue + DLQ with `ThumbnailRequested(movieId, filePath)`, `ThumbnailConsumer` in `LamuFlix.Api` calling DEV-17 `IThumbnailGenerator` gated by `Thumbnails:Enabled=false` with inactive fallback, `GET /api/movies/{id}/thumbnail` returning `image/jpeg` or 404, post-enrichment trigger for local movies without a poster, single recorded generator failure with no retry on missing binary, and consumer/endpoint 200+404 tests) and all 3 Acceptance Criteria (`recon-DEV-428:80-96`).
- **Boundaries & exclusions**: Generation and `ThumbnailOptions` (`Enabled`, `FfmpegPath`, `CacheDirectory`) belong to DEV-17 (`recon-DEV-428:98,111-116`); tile rendering (DEV-429), cache eviction (`recon-DEV-428:100-103`), and the pre-existing dirty `harness.yml` (`recon-DEV-428:6`) are out of scope.
- **Standing review & closing bar**: Everything outside frozen scope is a follow-up issue, not a round finding; Critical/High/Medium and missing required evidence block, Low requires explicit disposition, capped at 2 review rounds before hard stop (`lamuflix-team-charter` standing chain rule). Later boundary questions depend on the unverified DEV-17 contract body and DEV-428 comment text (`recon-DEV-428:129-132`).


### Q2/12

Verdict: Confirmed — dependency contract, atomic disk-cache promotion, retention boundary, and concurrency design approved as recommended.

- **DEV-17 dependency & zero new process/package surface (§2.3.1, §2.3.2, §2.3.3, §2.3.5)**: DEV-17 must land on `main` before Phase B; DEV-428 consumes `IThumbnailGenerator.GenerateAsync(string, CancellationToken)`, `ThumbnailResult(bool Success, string? CachePath, string? FailureReason)`, and shared `ThumbnailOptions` as registered by `AddLamuFlixThumbnails`, adding no new packages, process invocations, DB tables, image decoders, or extra Core cache ports (`recon-DEV-428:86,98`, `recon-DEV-428-2:17-54,106-111`).
- **Deterministic canonical cache promotion & retention boundary**: Because `FfmpegThumbnailGenerator` writes `<guid:N>.jpg` under the resolved trusted `CacheDirectory` (`recon-DEV-428-2:39,101`), DEV-428 atomically promotes a validated generator output (`Success && !string.IsNullOrWhiteSpace(CachePath)`) to a deterministic `{movieId}.jpg` canonical path under that same `CacheDirectory` and serves only that canonical path (`recon-DEV-428:82,87`); failed promotion removes only its own temporary `<guid:N>.jpg` file while preserving any existing canonical JPEG, with no expiry, eviction, or background cleanup per the ticket's Out of Scope `Cache eviction` exclusion (`recon-DEV-428:102`, `recon-DEV-428-2:124,149-151`).
- **Upstream concurrency constraint satisfied**: Single-active queue consumption (`prefetchCount: 1` matching `FeaturesConsumer` / `EnrichmentConsumer` at `recon-DEV-428:21-22`) bounds concurrent ffmpeg executions per consumer instance without new locks or rate-limiter dependencies, while `GET /api/movies/{id}/thumbnail` streams the canonical file asynchronously with per-request disposal and no shared lock against generation (`recon-DEV-428-2:125,149-151`).


### Q3/12

Verdict: Confirmed — consumer source-path authorization against `LibraryOptions.RootPath`, cache-path containment, and `GET /api/movies/{id}/thumbnail` HTTP contract (§2.3.4) approved as recommended.

- **Authoritative movie & library-root source authorization**: `ThumbnailConsumer` loads the movie by `movieId` from the repository and terminally rejects nonexistent movies, movies where a poster is now present, absent local source paths, or a broker `filePath` differing from the movie's current authoritative `Path` (`recon-DEV-428:28,33,85,88`). Before invoking `IThumbnailGenerator`, the source file must resolve inside operator-configured `LibraryOptions.RootPath` (`src/LamuFlix.Core/Options/LibraryOptions.cs:7-13`, bound at `src/LamuFlix.ServiceDefaults/Extensions.cs:83`) with separator-aware OS path comparison, rejecting traversal, sibling-prefix escapes, and symlink/reparse-point escapes (`recon-DEV-428-2:126,148-151`).
- **Cache containment before promotion**: `ThumbnailOptions.CacheDirectory` is trusted operator configuration (`recon-DEV-428-2:39`); before atomic promotion to canonical `{movieId}.jpg`, the consumer validates that `ThumbnailResult.CachePath` resolves directly beneath `CacheDirectory`.
- **Public HTTP endpoint contract (§2.3.4)**: `GET /api/movies/{id}/thumbnail` accepts only the movie ID, verifies `Thumbnails:Enabled` and movie existence, and serves only the canonical cached JPEG beneath `CacheDirectory` as `200 OK` (`image/jpeg`) streamed per request with no path leakage, returning `404 Not Found` when disabled, unknown/invalid ID, or cached file missing/unreadable (`recon-DEV-428:87,93-94`). No generation or media source access occurs on `GET`, no DTO or `PosterUrl` fields change, no ETag/Cache-Control/Range headers are added, and the C# route → generated `web/src/api/openapi.json` → generated `web/src/api/types.ts` contract chain is preserved without hand-edits (`recon-DEV-428:35,40,56`).


### Q4/12

Verdict: Confirmed — queue topology, post-enrichment publication, and single-attempt terminal failure/DLQ semantics approved as recommended; no ticket contradiction or owner checkbox required.

- **Topology, contract & disabled gating**: Declare durable quorum queue `thumbnail.requested` and its own `thumbnail.dead-letter` DLQ (with no retry queue) on the existing exchange conventions in `RabbitMqTopology` / `RabbitMqServiceCollectionExtensions`, with `ThumbnailRequested(MovieId MovieId, string FilePath)` in `LamuFlix.Core/Ports` and `ThumbnailConsumer` / `InactiveThumbnailConsumer` in `LamuFlix.Infrastructure/RabbitMq` gated by `Thumbnails:Enabled` (default `false`) so no active thumbnail subscription or publication occurs when disabled (`recon-DEV-428:20-25,85-86,94`).
- **Post-enrichment trigger & publisher confirmation**: Publish `ThumbnailRequested` only after successful enrichment persistence when `Thumbnails:Enabled=true` and the authoritative movie has no poster (`PosterUrl` absent) and a local `Path` (`recon-DEV-428:28,33,88,93`, `ProcessEnrichmentCommandHandler.cs:89-105`), reusing existing `IEnrichmentQueue` / `RabbitMqEnrichmentQueuePublisher` publisher-confirmed publish mechanics (`RabbitMqEnrichmentQueuePublisher.cs:14-42,116-127`, `EnrichmentConsumer.cs:208-209,255`) before acknowledging enrichment delivery, with no new outbox table or new Core port interface (§2.3.2, §2.3.3).
- **Single-attempt failure, DLQ & at-least-once semantics**: Acknowledge a thumbnail delivery only after atomic canonical cache promotion (or an eligible existing canonical cache hit); on malformed message, terminal validation failure, any failed `ThumbnailResult` (including `BinaryNotFound`), or promotion failure, log one sanitized structured failure for that handling attempt and dead-letter (`requeue: false`) to the thumbnail DLQ with no retry queue or automatic redrive (`recon-DEV-428:89,95`, `recon-DEV-428-2:32-33,41-54`). Host shutdown cancellation (`stoppingToken.IsCancellationRequested`) requeues (`requeue: true`) without logging a generator failure (`EnrichmentConsumer.cs:211-217`, `recon-DEV-428-2:36`), and broker reconnects follow existing consumer loop behavior under documented at-least-once delivery.


### Q5/12

Verdict: Confirmed — literal `thumbnail` queue name (superseding Q4 spelling), broker-enforced `x-single-active-consumer=true` with `prefetchCount=1`, and SOI-validated cache-hit skip approved as recommended; no owner checkbox required.

- **Exact queue & DLQ naming (supersedes Q4 requested-queue spelling)**: Use literal queue name `thumbnail` per verbatim ticket Scope item 1 (`recon-DEV-428:85`) and DLQ name `thumbnail.dead-letter` on the existing exchange/routing conventions in `RabbitMqTopology` (`src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs:16-25,107-146`), avoiding any §2.3a ticket-text divergence.
- **Broker-enforced single-active consumption**: Declare `thumbnail` with `x-single-active-consumer = true` in addition to consumer channel `prefetchCount = 1` and serial message handling (`RabbitMqTopology.cs:57-66`, `recon-DEV-428-2:125,149-151`), bounding concurrent ffmpeg executions to one globally across Api replicas while HTTP `GET` readers stream independently.
- **SOI-validated canonical cache-hit skip**: A duplicate `ThumbnailRequested` delivery may acknowledge without invoking `IThumbnailGenerator` only when the canonical `{movieId}.jpg` under `CacheDirectory` is readable and passes the DEV-17 JPEG header check (`Length >= 2` starting with SOI bytes `0xFF, 0xD8` per `recon-DEV-428-2:53,102`); an unreadable or invalid canonical file must be regenerated and atomically replaced rather than silently acknowledged.


### Q6/12

Verdict: Confirmed — local-filesystem operational boundary, `Thumbnails:Enabled=false` total short-circuit, and existing validation/telemetry/no-sweeper boundaries approved as recommended.

- **Local-filesystem deployment boundary**: Supported default is a single `LamuFlix.Api` instance with persistent operator-controlled `ThumbnailOptions.CacheDirectory` and `LibraryOptions.RootPath` (`recon-DEV-428:82`, `recon-DEV-428-2:39,126`); multi-instance deployments require operator-shared backing storage for both directories, with no distributed cache, object store, or DB mapping added in DEV-428 (§2.3.1, §2.3.3).
- **`Thumbnails:Enabled` short-circuit & composition**: `Thumbnails:Enabled=false` registers `InactiveThumbnailConsumer`, skips post-enrichment publication, and short-circuits `GET /api/movies/{id}/thumbnail` to `404 Not Found` before any cache I/O, generator call, or source-path check even if a JPEG exists on disk (`recon-DEV-428:21,86,94`, `recon-DEV-428-2:35`). When `Thumbnails:Enabled=true`, active `ThumbnailConsumer` subscription follows `RabbitMqServiceCollectionExtensions` consumer activation (`src/LamuFlix.Infrastructure/RabbitMq/RabbitMqServiceCollectionExtensions.cs:53-94`), while `GET` serves existing canonical cache independently of broker state (`recon-DEV-428:87,93`); DEV-17 owns `AddLamuFlixThumbnails` independent of `Features:LocalPlay` (`recon-DEV-428-2:106-109`).
- **Validation, sanitized logging & no orphan sweeper**: Enabled startup relies on existing `ThumbnailOptions` and `LibraryOptions` validation (`recon-DEV-428-2:108`, `src/LamuFlix.ServiceDefaults/Extensions.cs:83`); logs emit closed `ThumbnailResult.FailureReason` tokens without raw paths, stdout, or stderr and add no bespoke health-check infrastructure or custom metrics (`recon-DEV-428-2:33,41-54,120`); crash-interrupted temp files require no background sweeper per the ticket's Out of Scope `Cache eviction` exclusion (`recon-DEV-428:102`, `recon-DEV-428-2:124`).


### Q7/12

Verdict: Confirmed — implementation architecture, bounded file set (§2.3.6), test/proof strategy, task order, and Phase A/B gate rules approved as recommended; shared understanding locked for `brief.md`.

- **Architecture & bounded file surface (§2.3.2, §2.3.6)**: Concrete `ThumbnailCache` (`Infrastructure/Thumbnails/ThumbnailCache.cs`), `ThumbnailRequested` (`Core/Ports/ThumbnailRequested.cs`), `ThumbnailConsumer` / `InactiveThumbnailConsumer` / `RabbitMqThumbnailQueuePublisher` (`Infrastructure/RabbitMq/`), and `ThumbnailEndpoints` (`Api/Endpoints/ThumbnailEndpoints.cs`), with post-enrichment publication triggered from `EnrichmentConsumer` before enrichment `BasicAckAsync` so Core adds no new interface or Infrastructure reference (`recon-DEV-428:20-24,85-88`, `EnrichmentConsumer.cs:208-209,253-267`). Bounded edits to `RabbitMqTopology.cs`, `RabbitMqServiceCollectionExtensions.cs`, `EnrichmentConsumer.cs`, `ApiEndpoints.cs` (plus `Program.cs` / `ServiceDefaults/Extensions.cs` only if required for cache/existing-option wiring) and regenerated `web/src/api/openapi.json` + `web/src/api/types.ts` are authorized under §2.3.6 as forced by ticket Scope 1–6 and AC 1–3 (`recon-DEV-428:40,85-95`); no deletions, rewrites, or edits to dirty `harness.yml` (`recon-DEV-428:6`).
- **Proof strategy & test boundaries**: Use existing xUnit v3, Shouldly, NSubstitute, and Testcontainers (RabbitMQ + PostgreSQL) with a fake `IThumbnailGenerator` emitting a tiny valid SOI JPEG stub (`recon-DEV-428:47-50,90`, `recon-DEV-428-2:102,115`), proving end-to-end enabled enrichment → publisher-confirmed `thumbnail` message → canonical cache promotion → `GET 200 image/jpeg`, `404` on missing/unknown/disabled (even with cached JPEG present), poster/no-local-file trigger suppression, stale/unauthorized/traversal/symlink/sibling-prefix rejection, output containment, malformed message DLQ, all closed `ThumbnailResult.FailureReason` tokens including `BinaryNotFound` single sanitized log + terminal DLQ without retry (`recon-DEV-428:89,93-95`, `recon-DEV-428-2:41-54`), failed promotion preserving existing canonical cache, host shutdown requeue, SOI cache-hit validation, `x-single-active-consumer=true` + `prefetch=1`, and concurrent lock-free HTTP reads without duplicating DEV-17 real-ffmpeg/Dockerfile tests.
- **Task sequence & gate discipline**: Execute in order (main DEV-17 dependency/drift check → `ThumbnailRequested` + `ThumbnailCache` & unit proofs → topology/composition + publisher trigger → `ThumbnailConsumer` → `ThumbnailEndpoints` → OpenAPI/TS regeneration → focused & full repo verification gates → `/speckit-analyze` & 3-axis `size:M` review); Phase A checks document consistency only, Phase B runs full verification gates (`run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1`, `run-property-tests.ps1`, `run-vulnerable-packages.ps1`, `dotnet test`, `run-mutation.ps1`, and contract drift), reporting exit-2 strictly as `SKIPPED (scope-empty)` / `SKIP` / `N/A` and never `PASS`.

---

## Post-grill Analyze Ruling U1 — Closed sanitized logging tokens and fields for consumer non-generator outcomes

### Request (Conductor / Keel speckit-analyze U1)

Product and scope ruling for DEV-428 analyze finding U1: For consumer non-generator outcomes (malformed message, stale or ineligible request, source authorization failure, cache promotion failure), what closed sanitized logging tokens or fields should be permitted? Facts come from `recon-DEV-428`, `recon-DEV-428-2`, and note `DEV-428`. Provide ruling for Keel to record into `specs/DEV-428/brief.md`.

### Patron full ruling

Verdict: Approved — consumer non-generator terminal outcomes use a closed set of 4 sanitized `FailureReason` tokens (`MalformedMessage`, `IneligibleRequest`, `UnauthorizedSourcePath`, `CachePromotionFailed`) alongside the 10 closed `ThumbnailResult.FailureReason` tokens (`recon-DEV-428-2:41-54`), with a closed set of structured log fields (`{MovieId}`, `{FailureReason}`, `{Redelivered}`, `{ErrorType}`) and zero raw paths, payload bodies, or process output.

- **Closed consumer non-generator `FailureReason` tokens**:
  1. `MalformedMessage` — payload cannot be deserialized as a valid `ThumbnailRequested` with non-null `MovieId` and non-whitespace `FilePath` (`FeaturesConsumer.cs:198-203,381-393`, `EnrichmentConsumer.cs:196-203,335-347`).
  2. `IneligibleRequest` — authoritative movie does not exist, now has a non-empty `PosterUrl`, has no local `Path`, or its current authoritative `Path` does not equal the queued `FilePath` (`recon-DEV-428:28,33,88`, Q3/Q4).
  3. `UnauthorizedSourcePath` — authoritative source path fails `LibraryOptions.RootPath` containment (outside root, traversal, sibling-prefix, or symlink/reparse escape) before generator invocation (`recon-DEV-428-2:126,148-151`, Q3).
  4. `CachePromotionFailed` — generator returned `Success` with a `CachePath`, but canonical `{movieId}.jpg` promotion failed (`CachePath` outside `ThumbnailOptions.CacheDirectory`, unreadable/invalid SOI, or filesystem I/O error during atomic move/replace) (`recon-DEV-428-2:39,53`, Q2/Q3).
- **Generator outcomes (unchanged)**: When `IThumbnailGenerator.GenerateAsync` returns `Success == false`, log its exact closed `ThumbnailResult.FailureReason` token (`Disabled`, `InvalidSourcePath`, `BinaryNotFound`, `ProcessTimedOut`, `ProbeFailed`, `NoVideoStream`, `InvalidDuration`, `ExtractionFailed`, `InvalidOutput`, `StorageError` per `recon-DEV-428-2:41-54`).
- **Closed structured log fields & sanitization invariant**: Terminal failure logs may emit only `{FailureReason}` (one of the 4 consumer tokens above or 10 generator tokens), `{MovieId}` (`request.MovieId.Value`, omitted only for `MalformedMessage` when deserialization failed), `{Redelivered}` (`delivery.Redelivered`), and `{ErrorType}` (`exception.GetType().Name` only when an unexpected exception is caught, matching `FeaturesConsumer.cs:224-228` without passing the raw `Exception` object when it could carry path strings). Never log raw `FilePath`, `CachePath`, `RootPath`, `CacheDirectory`, message payload bytes/strings, exception messages/stack traces containing paths, stdout, or stderr (`recon-DEV-428-2:33`).

---

## Phase 2 Step 7 — Plan Challenge Adjudication (Sentry F1–F11, Ledger L1–L6, Compass Low-1–Low-3)

### Request (Keel Phase 2 step 7)

Adjudicate product and scope choices for all 20 plan-challenge findings across `findings-DEV-428-Sentry` (F1–F11), `findings-DEV-428-Ledger` (L1–L6), and `findings-DEV-428-Compass` (Low-1–Low-3) against bounded evidence (`recon-DEV-428`, `recon-DEV-428-2`, `DEV-428`, and `specs/DEV-428/{brief,plan,spec,tasks}.md`).

### Patron full ruling

Verdict: Approved — all 20 plan-challenge findings (Sentry F1–F11, Ledger L1–L6, Compass Low-1–Low-3) adjudicated within frozen scope; zero owner structural checkboxes (§2.3a) and zero constitution departures (§2.3b).

1. **Sentry F1 (High — Endpoint caller authorization & uniform 404 anti-oracle)**: No new authentication or authorization subsystem in DEV-428; `GET /api/movies/{id:int}/thumbnail` inherits the existing `LibraryEndpoints` access policy (`recon-DEV-428:40`, `specs/DEV-428/spec.md:139` FR-009), and Because exact caller-auth attributes on `LibraryEndpoints.cs:48,54` are not detailed in bounded recon roots, T000 MUST verify and mirror the landed `LibraryEndpoints` route access policy rather than inventing one (`specs/DEV-428/tasks.md:31` T000). Across all 404 miss classes (flag disabled, unknown/invalid id, missing/unreadable canonical cache), the endpoint MUST emit a uniform sanitized RFC 7807 `ProblemDetails` semantic body (`status: 404`, identical `title`/`type`, zero path or miss-reason detail disclosure) where only per-request `traceId` (and request `instance`, if emitted by the host problem pipeline) may vary — byte-identical `traceId` across distinct requests is rejected as incompatible with per-request Principle VI/V correlation (`specs/DEV-428/plan.md:33`, `findings-DEV-428-Sentry:11-15,74`).
2. **Sentry F2 (High — Symlink-safe canonical promotion)**: Tighten `ThumbnailCache` containment and promotion (`specs/DEV-428/spec.md:137` FR-007, `specs/DEV-428/tasks.md:42-43` T002/T003) so that both the generator temporary `<guid:N>.jpg` and the canonical `{movieId}.jpg` path — plus their ancestor chain under `ThumbnailOptions.CacheDirectory` — reject symlinks and reparse points (`FileAttributes.ReparsePoint` / `LinkTarget != null`). Same-directory atomic replacement under `CacheDirectory` MUST NOT follow a pre-planted symlink or reparse point at `{movieId}.jpg` to overwrite an external target; if `{movieId}.jpg` is a symlink/reparse point or promotion fails, any outside target file MUST remain untouched and any prior valid regular canonical JPEG MUST be preserved (`findings-DEV-428-Sentry:17-20`, `specs/DEV-428/brief.md:51,145`).
3. **Sentry F3 (Medium — Source authorization TOCTOU window)**: `ThumbnailConsumer` MUST canonicalize and revalidate the authoritative movie source path against `LibraryOptions.RootPath` (separator-aware containment plus symlink/reparse rejection on file and ancestors) immediately before invoking `IThumbnailGenerator.GenerateAsync(sourcePath, cancellationToken)` (`specs/DEV-428/spec.md:135-136` FR-005/FR-006, `recon-DEV-428-2:19-23`). Because `IThumbnailGenerator.GenerateAsync` takes a `string sourcePath` by contract (DEV-17 signature unchanged) and the OS provides no cross-process handle handoff to `ffprobe`/`ffmpeg`, document the residual name-based TOCTOU window as accepted under the trusted operator-owned `LibraryOptions.RootPath` and `ThumbnailOptions.CacheDirectory` deployment model (`findings-DEV-428-Sentry:24-27`, `recon-DEV-428-2:39,126`, `specs/DEV-428/brief.md:51-53`).
4. **Sentry F4 (Medium — Publish-before-ack enrichment coupling)**: Retain the settled Q4 design requiring broker publisher confirmation of `ThumbnailRequested` before `EnrichmentConsumer` acknowledges enrichment delivery (`specs/DEV-428/brief.md:57,163`, `specs/DEV-428/spec.md:133` FR-003). Explicitly document in `plan.md`/`spec.md` that when `Thumbnails:Enabled=true`, a thumbnail topology or publish failure prevents enrichment ack and redrives enrichment via its existing retry/DLQ pipeline; add no new outbox table, Core port, or bespoke alerting/metrics infrastructure (`findings-DEV-428-Sentry:29-32`, `specs/DEV-428/brief.md:61`).
5. **Sentry F5 (Medium — SAC failover, Windows file-sharing & atomic promotion)**: Do not claim `x-single-active-consumer=true` prevents brief concurrent consumer activation across broker failover or network partitions; document that rare overlapping generation is benign last-writer-wins on deterministic `{movieId}.jpg` (`findings-DEV-428-Sentry:34-37`, `specs/DEV-428/spec.md:110,131`). Require `ThumbnailCache` read streams to open with `FileShare.ReadWrite | FileShare.Delete` (and `FileOptions.Asynchronous`) so active HTTP `GET` readers on Windows do not block same-directory atomic replacement or vice versa, avoiding any shared generation lock or unbounded retry loop; do not hardcode an unverified OS/BCL move primitive from memory in the spec — T000/T002 MUST verify the landed .NET 10 Windows replacement behavior and T003/T013 MUST prove concurrent `GET` streaming during active promotion on the target platform (`specs/DEV-428/brief.md:47,99,102`).
6. **Sentry F6 (Medium — `BinaryNotFound` DLQ accumulation & operator runbook)**: Add operational runbook prose only inside existing `spec.md`/`plan.md` artifacts (no new infrastructure, counters, or circuit-breaker state): if `BinaryNotFound` occurs, the operator disables `Thumbnails:Enabled=false` (or installs `ffmpeg` at `FfmpegPath`), restarts the Api host, and inspects/purges `thumbnail.dead-letter` using standard RabbitMQ management tooling (`findings-DEV-428-Sentry:39-42`, `specs/DEV-428/brief.md:61,181-182`).
7. **Sentry F7 (Low — Unbounded cache growth & shutdown temp cleanup)**: Document in `spec.md`/`plan.md` that operators monitor disk usage of `ThumbnailOptions.CacheDirectory` and perform manual cache purges while the host is stopped (or with `Thumbnails:Enabled=false`), as cache eviction and background orphan sweepers are excluded by ticket scope (`recon-DEV-428:104`, `specs/DEV-428/brief.md:46,61`). When host shutdown cancellation interrupts consumer promotion after `GenerateAsync` returns a path, best-effort cleanup deletes only that attempt's own contained temporary `<guid:N>.jpg` before requeueing (`findings-DEV-428-Sentry:46-49`, `specs/DEV-428/brief.md:51,55`).
8. **Sentry F8 (Low — Served JPEG SOI integrity & `X-Content-Type-Options: nosniff`)**: Approved under §2.3.4: require `ThumbnailCache` to verify the 2-byte JPEG SOI signature (`Length >= 2` and leading bytes `0xFF, 0xD8`) both before atomic promotion (rejecting non-SOI generator output as `CachePromotionFailed`) and when opening the canonical `{movieId}.jpg` for `GET /api/movies/{id:int}/thumbnail` (treating corrupt/non-SOI canonical files as an unreadable cache miss returning 404 `ProblemDetails`), and set response header `X-Content-Type-Options: nosniff` on `200 OK` `image/jpeg` responses, with no image-decoder dependency (`findings-DEV-428-Sentry:51-54`, `specs/DEV-428/brief.md:51,98`).
9. **Sentry F9 (Low — Infallible trace extraction on malformed messages)**: `ThumbnailConsumer` MUST wrap header trace extraction (`TraceContextCarrier` / `Propagators.DefaultTextMapPropagator`) in a non-throwing fallback so corrupt or malformed `traceparent`/`tracestate` headers never throw out of the consumer loop or bypass the single sanitized `MalformedMessage` log + terminal `BasicRejectAsync(requeue: false)` DLQ contract; add a focused corrupt-`traceparent` malformed-message proof in T010 (`findings-DEV-428-Sentry:56-59`, `specs/DEV-428/spec.md:134,138`).
10. **Sentry F10 (Low — Startup options validation boundary)**: Add no new startup filesystem write probe or custom root-restriction validator beyond landed `ThumbnailOptions` and `LibraryOptions` validation without evidence (`specs/DEV-428/brief.md:115,182`, `recon-DEV-428-2:108`); T000 MUST verify the landed DEV-17 `ThumbnailOptionsValidator` and existing `LibraryOptions` startup validation rules on `main` and record any exact gap, while runtime path/storage failures remain sanitized terminal consumer outcomes (`UnauthorizedSourcePath`, `StorageError`, `CachePromotionFailed`) (`findings-DEV-428-Sentry:61-64`, `specs/DEV-428/tasks.md:31`).
11. **Sentry F11 (Low — Route `:int` constraint & startup `IOptions<ThumbnailOptions>` snapshot)**: Pin the endpoint route template as `GET /api/movies/{id:int}/thumbnail` (matching `GET /movies/{id:int}` at `recon-DEV-428:40`) while preserving the decided 404 `ProblemDetails` contract for non-existent or invalid movie IDs (`id <= 0` or unknown in repository; `specs/DEV-428/spec.md:139` FR-009). Both `ThumbnailConsumer` registration and `ThumbnailEndpoints` MUST read `IOptions<ThumbnailOptions>.Value` (startup-validated snapshot) consistently rather than mixing `IOptionsMonitor` hot-reload with static consumer registration; document that toggling `Thumbnails:Enabled` requires an Api host restart (`findings-DEV-428-Sentry:66-69`, `recon-DEV-428-2:106`).
12. **Ledger L1 (Medium — Single 404 miss mechanism & `.ProducesProblem(404)`)**: Standardize `ThumbnailEndpoints` miss handling on one mechanism — throwing the existing `NotFoundException` mapped by the single `GlobalExceptionHandler` + `AddProblemDetails()` pipeline (verified at T000 against landed `LibraryEndpoints` conventions; if `NotFoundException` differs on `main`, use the single landed endpoint 404 `ProblemDetails` helper uniformly, never two mechanisms) — and declare `.Produces(StatusCodes.Status200OK, contentType: "image/jpeg")` and `.ProducesProblem(StatusCodes.Status404NotFound)` on the route (`findings-DEV-428-Ledger:9-14`, `specs/DEV-428/plan.md:32-33`). T013 HTTP tests MUST assert status 404, `application/problem+json`, `traceId` presence, and absence of file paths or internal details without making unsupported assumptions about `GlobalExceptionHandler` internals prior to T000 (`findings-DEV-428-Sentry:74`).
13. **Ledger L2 (Medium — Placement of source authorization vs cache containment)**: Keep movie eligibility and `LibraryOptions.RootPath` source-path authorization as focused private helper methods inside `ThumbnailConsumer` (keeping per-method cyclomatic complexity within harness thresholds), and keep `CacheDirectory` containment, SOI verification, stream open, and atomic promotion inside `ThumbnailCache`; introduce no new class, interface, or service layer (`findings-DEV-428-Ledger:16-22`, `specs/DEV-428/brief.md:49,66-74`, §2.3.2).
14. **Ledger L3 (Medium — DI registration of `ThumbnailCache` & `IOptions<ThumbnailOptions>` access)**: DEV-428 registers `ThumbnailCache` as a singleton in `RabbitMqServiceCollectionExtensions` (within the existing `AddLamuFlixRabbitMq` composition called by `LamuFlix.Api/Program.cs:31`, `recon-DEV-428:23`), and both `ThumbnailConsumer` and `ThumbnailEndpoints` resolve that same `ThumbnailCache` instance plus `IOptions<ThumbnailOptions>` (never raw `IConfiguration`, satisfying Principle VII); `Program.cs` and `ServiceDefaults/Extensions.cs` remain conditional edits authorized only if T000 shows existing host wiring requires them (`findings-DEV-428-Ledger:24-31`, `specs/DEV-428/brief.md:79-83`).
15. **Ledger L4 (Low — Temp-directory hygiene & non-skippable symlink proof)**: Disk-bound `ThumbnailCache` and consumer authorization tests MUST use per-test unique temporary directories under `Path.GetTempPath()` with deterministic `try`/`finally` cleanup and no `IFileSystem` abstraction (`findings-DEV-428-Ledger:35-39`). Because symlink/reparse escape rejection is a required security proof in the frozen brief matrix (`specs/DEV-428/brief.md:98`, F2/F3) and the closing bar states that missing required evidence blocks (`specs/DEV-428/brief.md:41`) and `SKIP` is never `PASS` (`AGENTS.md`), symlink tests MUST execute and pass in a capable test environment (e.g. Windows Developer Mode / elevated privilege or Linux container/CI); an environment-skipped symlink test MUST be reported as `SKIP` / missing required evidence (blocking closure until run), never folded into `PASS`.
16. **Ledger L5 (Low — `const` literals & zero mutable static initialization)**: All span names in `TelemetryConstants`, JPEG SOI bytes (`0xFF`, `0xD8`), queue/routing-key literals, failure-reason tokens, and file extensions in `ThumbnailCache`, `ThumbnailConsumer`, and `RabbitMqThumbnailQueuePublisher` MUST be declared `const` (or computed in getters/methods) with zero mutable `static readonly` initialization, preventing cross-mutant state contamination under Stryker (`findings-DEV-428-Ledger:41-44`, `AGENTS.md` mutation static-initialization rule).
17. **Ledger L6 (Low — Optional workflow prose cleanup & `TelemetryConstants` member naming)**: Cleaning workflow meta-references from `plan.md` is optional non-blocking polish; `TelemetryConstants` producer and consumer span-name `const` identifiers may be finalized in T005 to match the exact landed naming pattern in `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` verified at T000 (`findings-DEV-428-Ledger:46-49`, `specs/DEV-428/tasks.md:54`).
18. **Compass Low-1 (Low — Explicit `SC-006` task citation)**: Optionally append `SC-006` to the requirement citations on tasks `T021`–`T024` in `tasks.md` during Quill's repair pass for explicit traceability completeness (`findings-DEV-428-Compass:24-25`, `specs/DEV-428/tasks.md:105-108`).
19. **Compass Low-2 (Low — Conditional `Program.cs` / `ServiceDefaults` wiring proof)**: Subsume verification of DI registration and option-validator wiring into `T006`/`T007`/`T013` host-composition and endpoint tests (`ApiHostCompositionTests` / `ApiEndToEndFactory`) whether or not `Program.cs` or `ServiceDefaults/Extensions.cs` requires a touch after T000 (`findings-DEV-428-Compass:27-28`, `specs/DEV-428/tasks.md:55-56,79-80`).
20. **Compass Low-3 (Low — Informational ProblemDetails 404 refinement record)**: Recorded as informational with no action or owner checkbox: returning RFC 7807 `ProblemDetails` with `traceId` on 404 misses (plus `X-Content-Type-Options: nosniff` on 200 hits per F8) refines the HTTP contract per Constitution Principle V while preserving the ticket's verbatim 404 miss status (`findings-DEV-428-Compass:30-31`, `specs/DEV-428/spec.md:14,139`).


## PLAN FROZEN receipt — DEV-428 — 2026-10-30

Keel, Phase 2 step 7 closure verification, challenge round 1 bounded Quill repair pass. **Status: PLAN FROZEN.** This receipt supersedes the earlier repair-required / awaiting-verification status for plan challenge only; earlier drafting headers and rulings remain historical records.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-428-spec`; verified branch: `feature/428-spec`. Decision baseline: brief Q1–Q7, U1, and complete P1–P20 rulings at brief.md:215–244. Read all three axis reports: Sentry F1–F11 (11), Ledger L1–L6 (6), Compass Low-1–Low-3 (3). Total: **20 findings; 20 closed by verified repair or explicit nonblocking disposition; zero unaddressed; zero remaining blocking plan findings.** No additional challenge round or cap extension.

Directly reread Quill-owned spec.md, plan.md, tasks.md and DRAFTING_RECEIPT.md against adjudication-DEV-428-plan:46–53 and the brief. All eight repair items are satisfied. Receipt item numbers 1–8 map the saved repairs; `needs decision: none` is consistent with the approved baseline. No decision change was needed.

| Finding | Final plan disposition | Saved verification anchors |
| --- | --- | --- |
| Sentry F1 High | CLOSED — inherited caller policy and uniform sanitized misses | spec.md:143,145–146; plan.md:33,48,106; tasks.md:31,78,80 |
| Sentry F2 High | CLOSED — temp/canonical/ancestor rejection, outside untouched, preserve prior regular JPEG | spec.md:80–81,141; plan.md:49,102; tasks.md:42–43,68 |
| Sentry F3 Medium | CLOSED — immediate source handoff checks; accepted residual string-path race explicit | spec.md:79,83,139; plan.md:51,126; tasks.md:67–68 |
| Sentry F4 Medium | CLOSED — confirm-before-ack and intentional enrichment redrive coupling | spec.md:126,137; plan.md:11,50,104; tasks.md:56 |
| Sentry F5 Medium | CLOSED — ordinary SAC bounds, last-writer-wins, Windows sharing and future platform proof | spec.md:112–113,135,141,145; plan.md:19,21,127–129; tasks.md:31,42–43,80 |
| Sentry F6 Medium | CLOSED — bounded BinaryNotFound operator runbook | spec.md:95,142; plan.md:54,108; tasks.md:154 |
| Sentry F7 Low | CLOSED — disk/manual purge and own-temp shutdown cleanup | spec.md:98,125,142; plan.md:51,54,105; tasks.md:67–68,154 |
| Sentry F8 Low | CLOSED — promotion/open SOI check, corrupt miss, nosniff | spec.md:141,143; plan.md:49,106; tasks.md:42–43,78,80 |
| Sentry F9 Low | CLOSED — non-throwing trace fallback and malformed/corrupt-header proof | spec.md:97,138,145; plan.md:34,51; tasks.md:67–68 |
| Sentry F10 Low | CLOSED — exact validator rules deferred to pickup, no invented validators | spec.md:146; plan.md:35,48; tasks.md:31 |
| Sentry F11 Low | CLOSED — id:int, numeric miss scope, consistent startup options/restart | spec.md:138,143; plan.md:33,35; tasks.md:55,78 |
| Ledger L1 Medium | CLOSED — one handler mechanism, metadata, actual traceId/sanitization proof | spec.md:143,145; plan.md:32–33; tasks.md:31,78,80 |
| Ledger L2 Medium | CLOSED — private consumer eligibility/source helpers; cache filesystem responsibilities | spec.md:138,141; plan.md:49,51; tasks.md:42,67 |
| Ledger L3 Medium | CLOSED — DEV-428 singleton cache registration and shared IOptions composition | spec.md:144–145; plan.md:35,52,103; tasks.md:55–56,80 |
| Ledger L4 Low | CLOSED — unique temporary dirs/cleanup; required capable-environment symlink proof | spec.md:145–146; plan.md:18,38,110; tasks.md:43,68,106 |
| Ledger L5 Low | CLOSED — const/computed literals, no mutable static initialization | spec.md:144,146; plan.md:22,40; tasks.md:42,54,67,107 |
| Ledger L6 Low | CLOSED BY EXPLICIT DEFERRED/NONBLOCKING DISPOSITION P17 — optional workflow prose retained; names align at T005 | brief.md:241; plan.md:50; tasks.md:54,155 |
| Compass Low-1 Low | CLOSED — optional SC-006 citations applied | tasks.md:105–108,155 |
| Compass Low-2 Low | CLOSED — conditional wiring proof subsumed, including broker-disabled HTTP | spec.md:145; plan.md:52,103; tasks.md:55–56,80 |
| Compass Low-3 Low | CLOSED BY INFORMATIONAL/NO-ACTION DISPOSITION P20 — 404 refinement preserves ticket status | brief.md:244; spec.md:32,143; DRAFTING_RECEIPT.md item 8 |

Cross-artifact consistency: FR-001–FR-012 and SC-001–SC-006 have task coverage (T000–T024); no remaining blocking ambiguity, constitution conflict, missing repair, or unmapped delivery requirement identified in this bounded closure check. The concrete Constitution Check remains populated. Earlier C1/C2/U1 requirements and the authorized file boundary are preserved. Quill receipt mapping is verified, not accepted solely on self-report.

**Scope of freeze:** document/plan closure only. Zero owner structural checkboxes; zero approved constitution departures. Gate 1 remains closed pending its separate owner action. DEV-17 landing, live caller policy/handler/validator details, replacement primitive and required symlink/Windows runtime proofs remain mandatory T000/Phase B work, not current guarantees or passed gates. No production tests/gates, implementation clearance, PR approval, merge-bar clearance, tracker mutation or merge claimed. Existing unrelated harness.yml change preserved. Next: Conductor routes the frozen plan to the separate Gate 1 step.
