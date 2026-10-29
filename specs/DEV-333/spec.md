# Feature Specification: DEV-333 Recursive library import with bounded pipeline and quota-day deferral

**Feature Branch**: `feature/333-spec`

**Created**: 2026-10-29

**Status**: gate1: provisional — basis: clean /speckit-analyze (Phase 2 step 7, Keel/Conductor report) and PLAN FROZEN plan challenge adjudicated (specs/DEV-333/PLAN_CHALLENGE.md:3,136-140)

**Input**: Frozen grill brief `specs/DEV-333/brief.md` (Q1–Q8 settled 2026-10-29); Patron rulings in `specs/DEV-333/CONCLUSIONS.md`; facts from notes `DEV-333`, `recon-DEV-333`, `recon-DEV-333-2`; product authority `specs/PRODUCT.md` sections 3 and 5. No decisions beyond the brief. No taste assumptions; no `ASSUMPTIONS.md` entry required (brief).

## Domain vocabulary *(mandatory — glossary terms for CONTEXT.md)*

Exact Candidate Movie Folder glossary text lands in root `CONTEXT.md` with this spec (brief Q4; Principle VIII; Phase A). Not deferred to delivery. Grill seat did not edit the shared glossary; Quill lands it here with the Spec Kit drafts:

**Candidate Movie Folder**
A directory at or below the Library root, including the root itself, containing at least one supported video file directly in it. It is imported exactly once as one Movie, and traversal continues into its subdirectories. A directory with no supported video file directly in it is only traversed, never imported. Directories reached through a link below the root are never candidates.
_Avoid_: scan folder, movie directory, container folder (for a candidate)

Delivery glossary entries (brief Plan-challenge round 1 §7; land in `CONTEXT.md` with US3 settlement delivery — Candidate Movie Folder remains Phase A):

**QuotaDeferred**
The settlement outcome for a movie whose metadata lookup stopped on provider quota exhaustion: it stays Pending, keeps its attempt, and is re-dispatched at `EnrichmentDeferredUntil`. It is not a failure and never enters retry.
_Avoid_: quota failure, quota retry, paused import

**EnrichmentDeferredUntil**
The persisted next-UTC-midnight instant (from the injected TimeProvider) before which a QuotaDeferred movie must not be claimed, swept, or dispatched.
_Avoid_: retry-after, next retry time, pause timestamp

**quota-deferred** (routing)
The routing action and durable queue (`enrichment.quota-deferred`) carrying the same-attempt `EnrichmentRequested` until `EnrichmentDeferredUntil`, when it dead-letters into requested.
_Avoid_: retry queue, delayed retry

Consumed vocabulary (already in use; not redefined): **LibraryPath**, **ImportMovieFolder**, **EnrichmentRequested** (`MovieId`, `Attempt`), **Pending**, **RateLimited**, **IMediaLibraryScanner**.

Cross-lane names (brief Q7 clarification; Quill may not rename):

- `EnrichmentFailureCategory.QuotaExhausted`, code `quota_exhausted` — **DEV-426 owns** provider mapping, constitution category amendment, glossary, and chain-fallback eligibility (T001 verifies landing; DEV-333 does not redefine or implement the chain). Maps `MetadataQuotaExhaustedException` to `MetadataLookupResult.Failed(QuotaExhausted)`; never persisted as `LastFailureCategory`; never enters `EnrichmentRetryPolicy`; HTTP 429 remains `RateLimited`.
- `ProcessEnrichmentOutcome.QuotaDeferred(DateTimeOffset DeferredUntil)`.
- `Movie.EnrichmentDeferredUntil` / column `enrichment_deferred_until`; mutator `DeferEnrichmentUntil(DateTimeOffset)`.
- Routing action `quota-deferred`; `RabbitMqTopology.QuotaDeferredQueue = enrichment.quota-deferred`; `QuotaDeferredRoutingKey = quota-deferred`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Recursive candidate discovery and exactly-once import (Priority: P1)

An explicitly invoked one-shot bulk import walks under `LibraryOptions.RootPath` with an **explicit stack of lazy enumerators** (no call-stack recursion, no depth cap, no materialized child/file lists), yields every Candidate Movie Folder lazily, and imports each through the existing single-folder import/outbox path so nested and root candidates become movies with staged enrichment. Container-only and empty directories are traversed but not imported. Linked/reparse-point subdirectories below the root are neither yielded nor descended (MockFileSystem proves attribute/no-descent only — it does not traverse linked contents). The configured root may be reached through a link: production resolves the root's final target for boundary/canonical identity only and never follows linked children; Mock linked-root proofs are resolve-only. Discovery does not choose the primary video or parse title/year; unchanged scanner semantics do that. Boundary comparison is host-selected (`OrdinalIgnoreCase` on Windows, `Ordinal` otherwise) via a pure internal `IsContained` helper inside the discovery adapter. A successful empty readable library returns zero imports; a missing or unreadable root fails. A fresh rerun of an already-committed candidate faults under existing unique-path behaviour — it does not skip, resume, or complete a remainder.

**Why this priority**: Ticket Scope items 1 and 4 and AC1 (recon-DEV-333-2 §1); brief Q3–Q4, Q6; Plan-challenge round 1 §§3,5–6.

**Independent Test**: MockFileSystem discovery yields the exact canonical candidate set once each (including deterministic 1,000-level nested chain); linked-child no-yield/no-descent; linked-root resolve-only; both-comparator `IsContained` proofs; real PostgreSQL bulk handler persists distinct movies and outbox rows; scanner regression proves primary selection unchanged; fresh-rerun duplicate path faults on real DB (brief Q7).

**Acceptance Scenarios**:

1. **Given** a library root with a root-level video, a nested parent candidate, a nested child candidate, and container/empty directories, **When** `ImportLibraryCommand` runs to completion, **Then** each Candidate Movie Folder is imported exactly once with canonical path identity, and container/empty nodes produce no movie.
2. **Given** a subdirectory that is a link/reparse point below the root, **When** discovery runs, **Then** that directory is neither yielded nor descended.
3. **Given** case-distinct sibling containment and root-only / volume-root forms (`/`, `C:\`), **When** `IsContained` / boundary checks run under both Ordinal and OrdinalIgnoreCase plus host-selected discovery flow, **Then** differently cased siblings are rejected under Ordinal, filesystem roots are preserved, and Windows hosts use OrdinalIgnoreCase while other supported hosts use Ordinal (brief Q4 clarification; Plan-challenge round 1 §5).
4. **Given** an existing readable root with no candidates, **When** the command completes, **Then** `ImportLibraryResult.ImportedCount` is 0.
5. **Given** movies already committed for canonical candidates, **When** a fresh bulk invocation runs, **Then** the invocation fails fast on the duplicate library path after peer shutdown; already committed work stays committed (brief Q6).
6. **Given** a 1,000-level nested candidate chain, **When** discovery runs, **Then** the exact candidate set is yielded once each and enumeration completes (iterative stack; no depth cap).

---

### User Story 2 - Bounded import pipeline (Priority: P2)

The one-shot handler runs one producer and exactly `LibraryOptions.MaxConcurrency` consumers over a bounded `LibraryPath` channel whose capacity equals `MaxConcurrency` (default 4, startup-validated `[Range(1, int.MaxValue)]`). The producer awaits writes; each consumer awaits one scoped folder import at a time. At most `MaxConcurrency` queued paths, `MaxConcurrency` active imports/scopes, and one producer-held path exist. Normal completion drains and awaits all tasks before return. Caller cancellation and producer/consumer faults cancel peers, complete the writer, await every task, and dispose every scope. Already committed imports stay committed; unimported candidates are not automatically recovered.

**Why this priority**: Ticket Scope item 2 and AC2; brief Q5–Q6.

**Independent Test**: Instrumented importer with interlocked in-flight counter and deterministic barriers proves concurrency reaches but never exceeds the configured cap, full-buffer producer wait, and drain/cancel/fault await-all plus scope disposal (brief Q7).

**Acceptance Scenarios**:

1. **Given** configured `MaxConcurrency`, **When** the pipeline runs under load, **Then** observed concurrent imports never exceed that value and do reach it.
2. **Given** all consumers blocked, **When** the producer writes into a full channel, **Then** the producer awaits `WriteAsync` and lazy discovery does not enumerate the whole tree ahead.
3. **Given** normal completion, caller cancellation, a producer fault, or a consumer fault, **When** the handler exits, **Then** every producer/consumer task was awaited, the writer completed, and every adapter-created scope was disposed.
4. **Given** the producer blocked on a full channel, **When** caller cancellation or a consumer fault occurs, **Then** peers cancel, the writer completes, every task is awaited, and no writer/reader/scope is abandoned — without requiring a fictitious simultaneous discovery-fault-during-`WriteAsync` (Q5 one-producer design; Compass L-4).
5. **Given** zero or negative integer `MaxConcurrency`, **When** the host starts, **Then** Range/startup validation fails; **Given** fractional or nonnumeric `MaxConcurrency` configuration strings, **When** the host starts, **Then** binder rejection fails startup (distinct paths; same fails-startup result; Compass L-3); default 4 is used when unset.

---

### User Story 3 - Quota-day deferral settlement (Priority: P3)

When enrichment settles a **chain-final** quota-exhausted outcome (DEV-426 distinguishable `QuotaExhausted`, not HTTP 429 `RateLimited`), DEV-333 keeps the movie `Pending`, sets `EnrichmentDeferredUntil` via `DeferEnrichmentUntil` to the next UTC midnight from injected application `TimeProvider`, does not advance the message attempt, does not record a terminal failure, and **always** publishes durable delayed `EnrichmentRequested(MovieId, same Attempt)` on the quota-deferred path before ack with expiration `max(0, whole ms remaining)`. Claim-before-lookup; early stray: no-op ack, zero providers, unchanged deadline, zero extra publish (no redeferral). Claim and stranded predicates refuse early and clear deferral on eligible claim using application TimeProvider comparisons. After expiry, re-dispatch and sweeper backstop resume without user action. Import/walk/producer/consumers never acquire quota. HTTP 429 retains existing bounded `RateLimited` behaviour. DEV-426 retains chain implementation: non-final quota + next Found completes with zero deferral; only chain-final quota defers. Ticket **Scope item 5** fake-gate pause/resume composition is delivered here (outside scanner code).

**Why this priority**: Ticket Scope items 3 and 5 and AC3; brief Q1 resumed, Q2, Q7 clarification; Plan-challenge round 1 §§1,4,7–8. Forces edits to enrichment settlement/routing files the ticket does not name (PRODUCT care item 6; cite AC3).

**Independent Test**: Unit settlement with FakeTimeProvider (Pending/deadline/same-attempt; Decide asserts Disposition only — no publish); composed consumer FakeTimeProvider at/equal/after-deadline expiration-zero one-publish same-attempt publish-before-ack fixture in `QuotaDeferredBrokerTests` (T019; separate from real broker TTL); composed real MetadataQuotaHandler + OmdbMetadataProvider, fake `IOmdbRequestGate`, recording fake transport (Scope item 5); composed real chain with controlled provider seams; real broker TTL with near-midnight application clock; real DB claim/sweeper predicates (brief Q7).

**Acceptance Scenarios**:

1. **Given** a chain-final quota-exhausted outcome, **When** settlement runs, **Then** status stays Pending, `EnrichmentDeferredUntil` is next UTC midnight, attempt is unchanged, no failure category is persisted, and outcome is `ProcessEnrichmentOutcome.QuotaDeferred`.
2. **Given** gate deny on the composed OMDb path (ticket Scope item 5), **When** lookup runs, **Then** zero transport calls occur, no import-side gate call occurs, and reopen at UTC midnight resumes claim/re-dispatch/lookup automatically.
3. **Given** a deferred movie before its deadline, **When** claim or stranded query runs or an early stray redelivers, **Then** claim refuses and stranded exclusion holds; early stray deliveries are unclaimed no-op acks with zero provider calls, unchanged persisted deadline, and zero additional deferral publish.
4. **Given** post-expiry, **When** delayed message or sweeper fires, **Then** claim clears deferral; primary delayed messages retain original attempt; sweeper attempt-1 recovery applies only on the lost-message backstop path.
5. **Given** provider HTTP 429, **When** settlement runs, **Then** existing `RateLimited` bounded retry and terminal Failed after MaxAttempts remain unchanged.
6. **Given** non-final quota on the real chain with the next provider returning Found, **When** lookup completes, **Then** enrichment completes with zero deferral; **Given** chain-final quota, **Then** settlement defers.
7. **Given** publish at or after the deferral deadline, **When** the durable quota-deferred path publishes, **Then** expiration is 0, exactly one publish occurs, and the original attempt is retained (always-publish max-zero whole-ms TTL).

---

### User Story 4 - Composed dry-run acceptance (Priority: P4)

A 500-candidate fixture imports through the real bulk handler against PostgreSQL at bounded concurrency, then every staged enrichment completes against a fake provider that returns Found for all. All 500 movies end Enriched with zero RateLimited, Failed, NotFound, or duplicates. Import count alone is insufficient.

**Why this priority**: Ticket AC4 and Scope item 5; brief Q7 clarification.

**Independent Test**: Integration composed fixture named in tasks.md (brief Q7).

**Acceptance Scenarios**:

1. **Given** 500 distinct candidates, **When** bulk import then fake-provider enrichment complete, **Then** 500 movies, 500 outbox rows, all Enriched, zero RateLimited/Failed/NotFound/duplicates, and observed concurrency ≤ configured MaxConcurrency.

### Edge Cases

- Missing/unreadable root or subtree, disappearing/invalid candidate, scanner/import/persistence fault: fail fast after Q5 shutdown; propagate first genuine fault (not peer cancellation, AggregateException, or generic wrapper); caller-token cancellation propagates `OperationCanceledException` after tasks settle (brief Q6).
- Qualifying as a candidate never prunes descent (brief Q4).
- Configured root may itself be reached through a link; no-follow applies below it (brief Q4).
- DEV-426 must land with amended quota-specific outcome before DEV-333 implementation; DEV-333 PR carries no DEV-426 implementation (brief Authority; Q7 order).
- No new public route/DTO/status, UI, scheduler, startup scan, external dependency, quota counter, skip-existing, or partial-run resume (brief Q8).
- `Features:LocalPlay`, secrets, and `Process.Start` are outside approved edits (brief Q8).
- Gherkin/Reqnroll not adopted (brief Q7).
- Failed/unrunnable gates and unmet ACs cannot be hidden by finding dispositions; exit-2 statuses preserved exactly (brief Q8).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Deliver Core `Features/Import` sealed payload-free `ImportLibraryCommand`, sealed `ImportLibraryCommandHandler`, and sealed `ImportLibraryResult(long ImportedCount)`. Handler depends on discovery, folder importer, and validated `LibraryOptions`. Register via `AddHandler` only — **no** `IValidator<ImportLibraryCommand>` and **no** empty validator stub (Principle V by construction; payload-free sweep-command precedent; Plan-challenge round 1 §2). Explicit invocation only: resolve registered handler in a fresh caller scope and await with the caller token; initial invokers are tests. No automatic host service or endpoint (brief Q3, Q6).
- **FR-002**: Deliver Core ports `ILibraryFolderDiscovery` (`IAsyncEnumerable<LibraryPath>` with cancellation) and `IMovieFolderImporter` (`Task<MovieId> ImportAsync(LibraryPath, CancellationToken)`). Infrastructure `FileSystemLibraryFolderDiscovery` uses existing System.IO.Abstractions with an **explicit stack of lazy enumerators** and pure internal `IsContained` helper **inside** that file. Infrastructure `ScopedMovieFolderImporter` creates/disposes a fresh async DI scope per folder and resolves decorated `ICommandHandler<ImportMovieFolderCommand, MovieId>` inside it. Never share one DbContext across consumers or put DI/Infrastructure into Core. `IMediaLibraryScanner.Scan` remains single-folder (brief Q3; Plan-challenge round 1 §§3,5).
- **FR-003**: Candidate and traversal rules per brief Q4 / Q4 clarification / Plan-challenge round 1 §§5–6: yield directories at/below RootPath with a supported top-level video; continue descending when a parent qualifies; do not follow linked/reparse-point subdirectories (Mock attribute/no-descent proof; never claim Mock traverses links); configured root may resolve final link target for boundary only (required linked-root support; real-filesystem content proof if content discovery under linked root is claimed); canonical root-relative `LibraryPath` for descendants; root yields canonical absolute path; host-selected containment (`RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` → OrdinalIgnoreCase; else Ordinal); filesystem-root-preserving normalization; deterministic both-comparator helper proofs.
- **FR-004**: Move the existing scanner extension set unchanged to internal Infrastructure/FileSystem `SupportedVideoExtensions`, shared by scanner and discovery with OrdinalIgnoreCase membership. Scanner regression proves selection unchanged. Preserve harness static-initialization/mutation rules (brief Q4).
- **FR-005**: `LibraryOptions.MaxConcurrency`: int, default 4, `[Range(1, int.MaxValue)]`, startup-validated. Binder rejects fractional/nonnumeric configuration strings; Range rejects zero/negative integers (distinct paths; same fails-startup). Use directly as consumer count and channel capacity. `Channel.CreateBounded<LibraryPath>` with Wait, SingleWriter true, SingleReader false, AllowSynchronousContinuations false. One producer, exactly MaxConcurrency long-lived consumers; termination and fault/cancellation contracts per brief Q5–Q6, including blocked-writer + consumer-fault/caller-cancellation without inventing simultaneous producer enumeration during WriteAsync.
- **FR-006**: ImportedCount increments only after scoped import returns (movie + staged enrichment saved). Says nothing about enrichment completion. Fail-fast and rerun semantics per brief Q6.
- **FR-007**: Quota-specific settlement per brief Q1 resumed / Q2 / Q7 names / Plan-challenge round 1 §§1,4,8: persist nullable `Movie.EnrichmentDeferredUntil` via `DeferEnrichmentUntil` (`enrichment_deferred_until`) with mapping, rehydration, and EF migration on existing movies table; all deadline writes/comparisons and TTL duration from injected application `TimeProvider` (not DB now()); save deferral then **always** publish same-attempt delayed message on quota-deferred topology with expiration `max(0, whole ms remaining)` then ack; claim-before-lookup / no-redeferral; early stray unchanged deadline + zero extra publish; claim and stranded predicates enforce deadline and clear on eligible claim; sweeper is post-expiry lost-message backstop only; narrow `Disposition` extension with optional `DeferredUntil`. HTTP 429 path unchanged. DEV-426 retains chain: non-final quota + next Found completes with zero deferral; only chain-final quota defers.
- **FR-008**: Walk, producer, consumers, and `ImportMovieFolderCommandHandler` never call `TryAcquireAsync` or metadata providers. Gate stays `TryAcquireAsync(CancellationToken)` only (DEV-426). No scanner-side pause (brief Q1).
- **FR-009**: Focused ADR for both new ports and the per-folder scope seam is authored by Keel in Phase A before analyze clearance / plan challenge at `docs/adr/0032-recursive-import-ports-and-scope-seam.md` (brief Post-draft process decision and ADR evidence closure 2026-10-29; `recon-DEV-333-2-2` §§1–2). Status Proposed — no Gate 1 or constitution approval claimed. Delivery verifies/references that Phase A ADR, names approval-evidence-gated Proposed→Accepted transition, and updates constitution Principle I list, Principle IV mutator/claim/sweeper inventories, Enrichment Reliability quota-settlement paragraph, plus ArchitectureTests whitelist through the existing ADR/Governance process (Sync Impact Report, version/date/template review against actual Phase B baseline; no new owner checkbox). Candidate Movie Folder lands in `CONTEXT.md` with this spec (Phase A); three Plan-challenge round 1 §7 glossary entries land at delivery (brief Q3–Q4; Post-draft process decision; Plan-challenge round 1 §1).
- **FR-010**: Acceptance proofs and binding order per brief Q7 / Q7 clarification table. Map every AC and ticket Scope item 5 test to named `Method_Condition_Expected` tests (real SUT member names) and project in `tasks.md`. Existing xUnit v3 / NSubstitute / Shouldly / MockFileSystem / FakeTimeProvider / AutoFixture/Faker.Net / Theory/MemberData / Testcontainers only. New domain unit tests in `LamuFlix.UnitTests` only. No new framework, Gherkin, sleep-based unit sync, mocked EF driver, or EF InMemory. At least one property invariant for candidate-set identity. No unassigned `TelemetryConstants.cs` write target.
- **FR-011**: Binding order: DEV-426 amended approval/landing (T001 verifies QuotaExhausted category/constitution/glossary/fallback); DEV-333 Gate 1; Phase A focused ADR already authored then referenced / constitution-whitelist + IV/Reliability alignment / Candidate Movie Folder already with spec; options/shared extensions/discovery; scoped importer/bounded handler/result; deferral domain/mapping/migration/claim/sweeper + delivery glossary trio; quota settlement/routing/topology; composed acceptance; gates and reviews. Separate PRs; no DEV-426 code in DEV-333 PR (brief Q7).
- **FR-012**: PR-stage gates from harness scripts only (Roslyn, complexity, InspectCode, format, full `dotnet test`, property tests, vulnerable packages, pre-PR mutation). Never hardcode/lower thresholds or edit generated files. Preserve exit-2 / Could not run semantics (brief Q7–Q8). No false phase/gate claims in this draft.
- **FR-013**: Closing bar and frozen scope per brief Q8: Critical/High/Medium block code-review/ship-review; every Low needs explicit disposition; all three axes mandatory for size:M; two rounds plan-challenge and two rounds delivery review (this correction is plan-challenge round 1 of cap 2 — do not mark freeze); anything outside frozen scope is a follow-up issue, not a finding in this round. No outstanding DEV-333 owner checkbox.
- **FR-014**: Charter care-list: no new NuGet/npm dependency (Channels and IO.Abstractions already in use); no new project/top-level folder/architectural layer beyond named ports/adapters in existing projects; schema limited to approved nullable column on movies; no public API shape change; no LocalPlay/secrets/`Process.Start`; forced enrichment/routing/repository predicate edits under care item 6 citing AC3 (brief Q1–Q2, Q8).

### Key Entities

- **ImportLibraryCommand / ImportLibraryCommandHandler / ImportLibraryResult**: One-shot bulk entry and result (`ImportedCount`).
- **ILibraryFolderDiscovery / FileSystemLibraryFolderDiscovery**: Streaming candidate discovery.
- **IMovieFolderImporter / ScopedMovieFolderImporter**: Per-folder scope seam over existing import handler.
- **SupportedVideoExtensions**: Shared Infrastructure extension holder.
- **Movie.EnrichmentDeferredUntil**: Nullable UTC deferral deadline.
- **ProcessEnrichmentOutcome.QuotaDeferred**: Settlement outcome distinct from Failed/Completed.
- **Quota-deferred topology**: Durable quorum re-dispatch path into requested on expiry.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Scanning a root with nested movie folders imports each Candidate Movie Folder exactly once (AC1).
- **SC-002**: Observed concurrency never exceeds configured `MaxConcurrency` under the named pipeline proofs (AC2).
- **SC-003**: With the gate exhausted, no OMDb transport call is made; movies stay Pending until next UTC day and resume automatically without user re-request (AC3).
- **SC-004**: A 500-movie fixture completes import plus fake-provider enrichment with all Enriched and zero RateLimited/Failed/NotFound/duplicates (AC4).
- **SC-005**: Fresh readable empty library returns ImportedCount 0; missing/unreadable root does not; fail-fast and cancellation ordering hold under named race tests.
- **SC-006**: HTTP 429 retains bounded RateLimited behaviour; real-chain non-final quota + next Found completes with zero deferral; only chain-final quota defers (DEV-426 retains chain implementation).
- **SC-007**: Architecture whitelist and constitution Principle I list include the two new ports after Phase A ADR `docs/adr/0032-recursive-import-ports-and-scope-seam.md` is referenced and Governance-aligned (approval-gated Accepted); Principle IV / Reliability inventories record deferral mutator/claim/sweeper/quota-settlement paragraph; Candidate Movie Folder appears in `CONTEXT.md` with the spec (Phase A); three Plan-challenge round 1 §7 glossary entries appear at delivery.

## Assumptions

- No taste assumptions were made; no `ASSUMPTIONS.md` entry (brief).
- DEV-426 at `fee5c7a8` remains unapproved unlanded specification evidence until its own amended Gate 1 and landing; DEV-333 implementation is blocked on that landing (brief Authority).
- TTL publishing/declaration mechanics, test fixture construction, and metric/log tags are plan detail under fixed contracts and must not alter durability, attempt retention, UTC boundary, no-early-dispatch, or no-failure semantics (brief Planned file responsibilities).
- Side Spec Kit design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`, checklists) are not produced; the frozen brief already settles contracts, files, ordering, and proofs (Quill path for closed grill; same pattern as recent M drafts).
