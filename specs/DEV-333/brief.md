# DEV-333 — Recursive library import: frozen grill brief

Status: grill closed by Patron, 2026-10-29; ready for Quill drafting only.
Size: M. Worktree: `F:/Dev/LamuFlix.worktrees/feature-333-spec`; branch: `feature/333-spec`.
Recon baseline: `f698aeceb6b6af50a397d076b83ed178566641b0`.

This brief records the eight settled decisions. The full questions, recommendations, replies, cited rationale and verbatim rulings are append-only in [CONCLUSIONS.md](CONCLUSIONS.md). Q1 resumed and Q4/Q7 clarifications belong to their original decisions; the 12-question cap was not reached. Later refinements supersede only the passages they expressly identify. There are no unsettled DEV-333 grill answers or owner checkboxes.

## Authority and phase boundaries

Facts come from canvas notes `DEV-333`, `recon-DEV-333`, and `recon-DEV-333-2`; do not replace missing facts with searches or assumptions. Ticket Scope items 1–5 and AC1–AC4 are transcribed in recon part 2, section 1. Constitution constraints are in its section 2; frozen DEV-426 evidence is in section 3 and downstream retry evidence in the appended follow-up sections 1–4. `specs/PRODUCT.md` sections 3 and 5 govern authority and escalations.

DEV-426 at `fee5c7a8` is unapproved, unlanded specification evidence, not an implemented dependency. Its frozen map-to-RateLimited rule must be amended as Q1/Q7 require. DEV-426 must obtain its own owner Gate 1 approval of ADR-0031/constitution alignment and the amended outcome contract, then land separately before DEV-333 implementation. Do not carry DEV-426 implementation in the DEV-333 PR or silently consume the old contract. If its owner rejects that amendment, return to Patron for `blocked: structural` disposition.

This closure is neither a clean analysis nor a plan challenge verdict, Gate 1, implementation authorization, test receipt, delivery review clearance or merge bar. Quill drafts spec/plan/tasks next. Keel then performs read-only analysis and briefs one numbered fix list per round; size:M plan challenge requires all three axes before adjudication. DEV-333 Gate 1 remains later and separate. No production code or tests were changed or executed by this grill.

## Frozen scope and loop discipline — Q8

Deliver lazy recursive candidate discovery, one configured bounded import pipeline, unchanged single-folder import/outbox reuse with per-folder scopes, an internal explicitly invoked one-shot command/result, quota-specific durable Pending-until-next-UTC-day settlement with the approved deferral column/claim/sweeper/broker changes, and the exact acceptance proofs, focused ADR, glossary term and port alignment below; anything else is a follow-up issue, not a finding in this round.

No new public route/DTO/status, UI, scheduler, startup scan, external dependency, quota counter implementation, skip-existing behavior or partial-run resume is included. `Features:LocalPlay`, secrets and `Process.Start` are outside the approved edits.

Closing bar: Critical, High and Medium findings block `$code-review`/`$ship-review`. Every Low requires an explicit recorded fixed/rejected/carried disposition; follow-up tickets follow Patron/Rigger charter criteria, otherwise record the carried note without inventing a ticket. All Sentry/Risk, Ledger/Standards and Compass/Spec reports are mandatory for size:M plan and delivery adjudication. Missing axis means blocked, never skipped.

Round cap: two rounds for the plan-challenge loop and a separate two rounds for delivery review/remediation. No silent reset, third review or extra immutable review. Remaining blockers at cap mean blocked for a new decision, never automatic clearance. Findings and summary must be posted to the PR; fixes reply and resolve threads. Never merge; the owner merges.

Failed or unrunnable gates and unmet ACs cannot be hidden by finding dispositions. Preserve exit-2 statuses exactly: SKIPPED (scope-empty), SKIP (opt-out), or mutation NOT APPLICABLE/N/A; none means PASS. Their non-blocking applicability follows repository instructions and script results. Could not run is not a pass.

## Decision register and approach

### Q1 — How does bulk progress join quota pause/resume?

Initially only the no-pre-acquisition invariant was settled; downstream retry fate required recon. Follow-up recon proves gate-generated RateLimited and HTTP 429 are indistinguishable downstream and the default retry path terminally fails at about 60 seconds, not UTC midnight. The resumed ruling therefore requires a quota-specific outcome, not that bounded retry path.

The walk, producer, consumers and existing import handler never acquire quota or call providers. Import/outbox staging can finish while quota is exhausted. Pause/resume means downstream enrichment progress: keep the movie Pending, preserve the message attempt, make no further dispatch for that item before next UTC midnight, then resume automatically without user action. Gate-only `TryAcquireAsync(CancellationToken)` remains unchanged: exactly one committed reservation per physical outbound attempt, including retries; no refunds, no consuming scanner probe, no new gate status/wait API. HTTP 429 retains existing RateLimited bounded retries and terminal failure. Quota remains chain-fallback eligible; a successful fallback still completes enrichment, and only a chain-final quota outcome defers it.

DEV-426 owns the distinguishing provider mapping and chain contract. DEV-333 owns downstream settlement and scanner work. Patron ruled this composition delivers existing Scope 3/AC3 without a DEV-333 ticket change or constitution departure; DEV-426 approval remains separate. Forced edits to unnamed enrichment/routing files are approved under PRODUCT care item 6 because AC3 requires them.

### Q2 — What survives until rollover, including crashes?

Persist nullable `Movie.EnrichmentDeferredUntil` (`DateTimeOffset?`), column `enrichment_deferred_until`, on the existing movies table. Q2 explicitly approves this schema addition under PRODUCT care item 3, superseding Q1's initial no-schema expectation. Map and rehydrate it, with a migration; do not falsify LastAttemptAt or add a table. Compute next UTC midnight through injected TimeProvider. Leave Pending, do not record a terminal failure, do not advance the quota-deferred attempt, and do not enter ordinary retry policy.

Save deferral first; publish durable delayed `EnrichmentRequested(MovieId, same Attempt)` second; ack the original third. Use a separate durable quorum re-dispatch path, expiring at the persisted deferral and dead-lettering into requested, never the 30s retry queue. Consumer publish/ack preserve the existing non-cancelled settlement ordering ruled by Patron; cancellation during lookup still propagates. No sleeping consumer, in-memory timer or restart-sensitive wait.

Both existing repository predicates enforce the persisted deadline: `TryClaimForEnrichmentAsync` refuses early and clears deferral atomically on a successful eligible claim; `FindStrandedMovieIdsAsync` excludes future deferrals and includes eligible expired ones. Early stray deliveries become unclaimed no-op acks, not provider calls or new deferrals. The post-expiry sweeper is the lost-message backstop, retaining its existing attempt-1 recovery semantics only in that recovery path; primary delayed messages retain their original attempt. Duplicate claim protection plus publish-before-ack and the sweeper cover crash windows. Non-quota retry defaults remain unchanged. No new IMovieRepository member is required.

### Q3 — Where does the one-shot capability live?

Core `Features/Import`: sealed, payload-free `ImportLibraryCommand`, sealed `ImportLibraryCommandHandler`, and the Q6 result. Dependencies are discovery, folder importer and validated LibraryOptions. Root and concurrency come from options. Explicit invocation resolves the registered command handler in a fresh caller scope and awaits it with the caller token; initial invokers are tests. No automatic host service or endpoint is added.

Core ports: `ILibraryFolderDiscovery`, yielding `IAsyncEnumerable<LibraryPath>` with cancellation, and `IMovieFolderImporter`, exposing `Task<MovieId> ImportAsync(LibraryPath folder, CancellationToken cancellationToken)`. Infrastructure `FileSystem/FileSystemLibraryFolderDiscovery` uses existing System.IO.Abstractions. Infrastructure `Import/ScopedMovieFolderImporter` creates/disposes a fresh async DI scope per folder and resolves the decorated `ICommandHandler<ImportMovieFolderCommand, MovieId>` within it. Never share one DbContext across consumers or put DI/Infrastructure references into Core. `IMediaLibraryScanner.Scan(LibraryPath)` remains single-folder.

A focused ADR covers both new ports and the scope seam; update the constitution Principle I list and ArchitectureTests whitelist through its existing ADR process. Patron ruled no constitution departure or owner checkbox: discovery behind a Core port is already ticket-approved, and the execution seam preserves that boundary. Quill drafts the ADR with recon-provided numbering; do not invent an occupied ADR number.

### Q4 — Which directories qualify, and how are they identified?

A candidate is a directory at/below RootPath, including root, containing a supported video directly in it. Yield/import it once, and continue descending even when its parent qualifies. Empty and container-only nodes are traversed but not yielded. Discovery does not choose the primary video or parse title/year; unchanged scanner semantics do that.

Do not follow or yield linked/reparse-point subdirectories. The configured root may itself be reached through a link and defines the boundary. Use canonical root-relative LibraryPath values for descendants, preserving enumerated case and OS separators, with no leading/trailing separator or dot segments. Root itself yields its canonical absolute path because empty LibraryPath is invalid.

Q4 clarification: full-path root normalization preserves filesystem/volume roots such as `/` and `C:\`; remove trailing separators only beyond the filesystem-root length. Boundary comparisons use Windows OrdinalIgnoreCase or Ordinal on case-sensitive hosts; never case-fold identity or admit a differently cased sibling. A contained path must equal root or have the root's directory-boundary prefix, handling root-only forms correctly. Reject escapes before yield/descent.

Move the existing extension set unchanged to internal Infrastructure/FileSystem `SupportedVideoExtensions`, shared by scanner/discovery with the same OrdinalIgnoreCase membership. No second list or Core filesystem concept. Scanner regression tests prove selection behavior is unchanged. Preserve harness static-initialization/mutation rules when implementing the shared holder.

Quill lands this exact glossary term in CONTEXT.md with the spec (no shared glossary edit was made by this seat):

**Candidate Movie Folder**
A directory at or below the Library root, including the root itself, containing at least one supported video file directly in it. It is imported exactly once as one Movie, and traversal continues into its subdirectories. A directory with no supported video file directly in it is only traversed, never imported. Directories reached through a link below the root are never candidates.
_Avoid_: scan folder, movie directory, container folder (for a candidate)

### Q5 — What bounds work and ensures termination?

`LibraryOptions.MaxConcurrency`: int, default 4, `[Range(1, int.MaxValue)]`, startup-validated; zero/negative/non-integer configuration fails startup. Use it directly, without derived arithmetic, as both consumer count and channel capacity. No separate capacity knob. `Channel.CreateBounded<LibraryPath>` uses Wait, SingleWriter true, SingleReader false, AllowSynchronousContinuations false.

One producer awaits WriteAsync; exactly MaxConcurrency long-lived consumer tasks each await one scoped import at a time. At most MaxConcurrency queued paths, MaxConcurrency active imports/scopes and one producer-held path; no per-item Task.Run or growing task list. Traversal memory is depth-only streaming enumerators, no materialized per-directory file/child lists; stop candidate file inspection after the first supported match.

Normal completion closes writer, drains readers and awaits all tasks before return. Caller cancellation flows through discovery/read/write/import under a disposed linked CTS; await every task and scope disposal. A producer/consumer fault cancels peers, completes writer and awaits all tasks, preventing blocked writers/readers or abandoned scopes. Already committed imports stay committed; unimported candidates are not automatically recovered.

### Q6 — What does success mean, and what happens on faults/reruns?

`ImportLibraryResult` is a sealed record with `long ImportedCount`. Return only after complete drain; count successful scoped imports whose movies and staged enrichment have been saved. This says nothing about enrichment completion. Existing readable empty library succeeds with zero; missing/unreadable root does not.

Missing/unreadable root/subtree, disappearing/invalid candidate, scanner/import/persistence fault and duplicate library path fail fast after Q5 shutdown. No partial-success result, batch transaction or rollback of earlier folder commits. After all tasks settle, caller-token cancellation propagates OperationCanceledException; otherwise propagate the first genuine fault with original identity/stack, not peer cancellation, AggregateException or a generic wrapper. Preserve Q6's fault-versus-caller-cancellation ordering in named race tests.

Exactly once is per completed invocation and canonical candidate. Existing per-folder unique-path behavior stays: a fresh rerun faults on an already committed candidate; it does not resume, skip existing paths or guarantee completion after partial failure. Patron explicitly corrected the original repair-and-rerun suggestion. No cross-run lock or new repository lookup is added.

### Q7 — What proves delivery, and in what order?

Use existing xUnit v3/NSubstitute/Shouldly/MockFileSystem conventions, deterministic asynchronous barriers, FakeTimeProvider and real PostgreSQL/RabbitMQ Testcontainers. No new framework, Gherkin/Reqnroll, sleep-based unit synchronization, mocked EF driver or EF InMemory. Quill maps every AC and ticket Scope 5 test to named Method_Condition_Expected tests and project in tasks.md.

| Obligation | Required proof |
|---|---|
| AC1 / recursive walk | Exact root/nested-parent/nested-child/container/empty candidate set, once each; real bulk persistence and outbox rows; scanner regression parity |
| Boundary safety | Linked subdirectories neither yielded nor descended; case-sensitive sibling rejection; root-only and `/`/`C:\` preservation; extension parity |
| AC2 / bounded pipeline | Instrumented importer reaches but never exceeds configured concurrency; full-buffer producer wait and bounded discovery using barriers; drain/cancel/producer-fault/consumer-fault await all work and dispose scopes |
| AC3 / UTC deferral | Unit settlement proves Pending, deadline, same attempt and no failure; composed real MetadataQuotaHandler + OmdbMetadataProvider, fake IOmdbRequestGate, recording fake transport and FakeTimeProvider proves deny makes zero transport calls and reopen at midnight resumes automatically without scanner acquisition |
| Durable settlement | Real DB migration/null-row compatibility, early claim/sweeper exclusion, post-expiry claim/clear; real broker TTL with near-midnight test clock; restart/crash windows, lost-message sweeper recovery and duplicate-delivery protection |
| Non-quota regression | HTTP429 retains existing bounded RateLimited behavior; quota remains chain-fallback eligible |
| AC4 / 500-movie dry run | Real bulk handler + PostgreSQL imports 500 distinct movies/outbox rows at bounded concurrency, then fake provider returns Found for all; all 500 Enriched, zero RateLimited/Failed/NotFound/duplicates. Import count alone is insufficient |
| Failure/rerun/options | Empty valid root zero; missing/unreadable/disappearing/error and cancellation semantics; real-DB fresh-rerun duplicate fault; default and invalid MaxConcurrency startup validation |
| Property invariant | At least one generated-tree candidate-set identity property in the existing property-test setup |

Fake-clock date mathematics and composed fake-gate tests do not substitute for real broker expiration or real database predicates. Test real TTL with a near-midnight application clock so the wait is seconds, rather than advancing a fake clock and claiming RabbitMQ advanced with it.

Exact cross-lane names (Q7 clarification; Quill may not rename):

- `EnrichmentFailureCategory.QuotaExhausted`, code `quota_exhausted`; DEV-426 maps `MetadataQuotaExhaustedException` to `MetadataLookupResult.Failed(QuotaExhausted)`. Never persist it as LastFailureCategory or send it into EnrichmentRetryPolicy; HTTP429 remains RateLimited.
- `ProcessEnrichmentOutcome.QuotaDeferred(DateTimeOffset DeferredUntil)`, distinct from Failed.
- `Movie.EnrichmentDeferredUntil`, nullable column `enrichment_deferred_until`.
- Routing action `quota-deferred`; `RabbitMqTopology.QuotaDeferredQueue = enrichment.quota-deferred`, `QuotaDeferredRoutingKey = quota-deferred`, expiry dead-letters into requested. Publish the same attempt before ack.

Binding order: DEV-426 amended approval/landing; DEV-333 Gate 1; focused ADR/glossary/constitution-whitelist alignment; options/shared extensions/discovery; scoped importer/bounded handler/result; deferral domain/mapping/migration/claim/sweeper; quota settlement/routing/topology; composed acceptance; gates and reviews. Recheck main and dependency contract drift at Phase B pickup, then run analysis against that actual baseline before claiming implementation readiness.

PR-stage gates: repository Roslyn, complexity, InspectCode, dotnet format verification, full dotnet test, property tests, vulnerable packages and configured pre-PR mutation. Read parameters/thresholds/exclusions from harness.yml/scripts then; do not hardcode thresholds, alter generated files or lower gates. Core mutation and Infrastructure integration evidence are distinct. A possible property opt-out requires its recorded pre-run disposition; never relabel a failed run or a survivor as opt-out. No gate has been run or passed by this brief.

## Planned file responsibilities — drafting targets, not edits made

- Core/Options/LibraryOptions.cs and existing options/startup-validation tests: MaxConcurrency.
- Core/Ports/ILibraryFolderDiscovery.cs and IMovieFolderImporter.cs; Core/Features/Import/ImportLibraryCommand.cs, ImportLibraryCommandHandler.cs, ImportLibraryResult.cs: streaming and bounded orchestration contracts.
- Infrastructure/FileSystem/FileSystemLibraryFolderDiscovery.cs, SupportedVideoExtensions.cs and the narrow extraction in DirectoryMediaLibraryScanner.cs: filesystem discovery/shared selection eligibility. Existing IMediaLibraryScanner and single-folder import behavior remain intact.
- Infrastructure/Import/ScopedMovieFolderImporter.cs; Api/HandlerRegistration.cs: scope ownership and explicit registrations.
- Core/Domain/Movie.cs; existing EF movie mapping/rehydration files plus a new migration; Infrastructure/Persistence/EfMovieRepository.cs: deferral and claim/stranded predicates. Quill uses recon-confirmed paths rather than inventing mapping/migration filenames.
- Core enrichment settlement/outcome and Infrastructure/RabbitMq/EnrichmentConsumer.cs, EnrichmentRouting.cs, RabbitMqTopology.cs: QuotaDeferred and durable dispatch. Existing sweep command behavior is changed only through its repository predicate boundary as Q2 permits.
- UnitTests, IntegrationTests and existing property-test locations: named proofs in the table. No production changes are assigned to Keel.
- Focused ADR for the two new ports, CONTEXT.md glossary, constitution Principle I list, ArchitectureTests whitelist, spec.md/plan.md/tasks.md and their traceability: Quill drafting, with no approval implied by artifact creation.
- DEV-426 only: QuotaExhausted category/provider mapping and chain fallback contract; no DEV-426 implementation in this PR.

TTL publishing/declaration mechanics, test fixture construction and metric/log tags remain plan detail under these fixed contracts. They may not alter durability, attempt retention, UTC boundary, no-early-dispatch or no-failure semantics. If those details expose a new decision, Quill returns `needs decision:` for a recorded brief amendment; missing repository facts go to Conductor as `needs recon:`. No taste assumptions were made, so no ASSUMPTIONS.md entry is required.

## Post-draft process decision — 2026-10-29, Phase 2 step 3.3

ADR authorship correction: Keel authors the focused ADR for both new ports and the per-folder scope seam in Phase A, before analysis clearance / step 6 plan challenge. This supersedes only the Quill-authorship assignments in Q3 and Planned file responsibilities; all Patron architecture rulings remain unchanged. Basis: Conductor step 3.3 expressly authorizes Keel ADR drafting; Quill DRAFTING_RECEIPT records its seat prohibition on ADR writes; Thinker documentation access permits docs/adr/. Quill aligns spec/plan/tasks and records the resolution in its receipt after Keel supplies the path.

ADR numbering remains pending recon: the supplied recon proves DEV-426 reserves 0031 but does not establish 0032 availability or an ADR format source. The draft 0032 placeholder is not an authoritative reservation. Keel requests the available number, exact target slug, and ADR format/template through Conductor; no ADR number is invented and no ADR body is claimed written. This is a documentation-evidence block, not a new scope decision or owner checkbox.

ADR evidence closure: Conductor supplied `recon-DEV-333-2-2`, sections 1–2, confirming 0032 is free on main, this HEAD and fee5c7a8, and the ADR-0031 house format. Keel drafts `docs/adr/0032-recursive-import-ports-and-scope-seam.md` using H1, qualified Status, provenance, Context, Decision and Consequences. This supersedes the pending-numbering paragraph above only; rebase/pickup must recheck numbering and dependency drift. Proposed status claims no Gate 1 or constitution approval. Quill references this exact file and preserves the historical drafting receipt with an appended resolution.

## Plan-challenge round 1 decisions — 2026-10-29

Authority: Patron full eight-item ruling appended verbatim to CONCLUSIONS.md under `DEV-333 Phase 2 step 6 round 1 — Patron plan-detail rulings`; evidence `recon-DEV-333-2-2` follow-up §§1–3 (lines 55–94). These decisions supersede only conflicting plan-detail deferrals above. No Gate 1, code approval or plan freeze is conferred.

1. Constitution IV/Reliability alignment is a compliant clarification, not a departure; no new owner checkbox. Delivery T004 adds `DeferEnrichmentUntil(DateTimeOffset)` (Pending-only, no status change), claim deferral predicate/atomic clear, sweeper future exclusion/expired eligibility, and a quota-deferral settlement paragraph distinguishing same-attempt durable dispatch from failure retries. Preserve Governance Sync Impact Report, version/date/template review against actual baseline. T002/T004 name approval-evidence-gated ADR Proposed→Accepted transition. DEV-426 alone owns QuotaExhausted category alignment and its existing owner checkbox; T001 verifies landing.
2. Payload-free sealed ImportLibraryCommand passes through the mandated validation decorator via AddHandler with no registered validator and no empty validator stub. Its inputs are startup-validated LibraryOptions; precedent is payload-free sweep commands, recon §2. A future payload addition requires a real validator. No new validator file is authorized.
3. Discovery uses an explicit stack of lazy enumerators, disposing/popping them on exhaustion and disposal on cancellation/fault. No call-stack recursion, depth cap, materialized child/file lists or changed candidate set. T007/T012 include deterministic 1,000-level exact-candidate-set/completion proof.
4. Publish always on the durable quota-deferred path, even if overdue. Expiration is whole milliseconds max(0, (DeferredUntil - injected TimeProvider.GetUtcNow()).TotalMilliseconds); at/after deadline publish once with expiration 0 and original attempt. No skipped publish or new deadline/day rollover. Preserve save→publish→ack. Named fake-clock expiration-zero proof plus existing real broker TTL proof. All deadline writes/comparisons use injected application UTC values, not DB now().
5. Host-selected lexical comparison: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) selects OrdinalIgnoreCase; all other supported hosts Ordinal. No volume probing, alias deduplication or identity folding. Put an internal pure IsContained(root,candidate,comparison) helper inside FileSystemLibraryFolderDiscovery.cs; use existing Infrastructure InternalsVisibleTo, no new file/port/layer. T007 tests both comparison modes independent of host plus discovery flow and root forms.
6. MockFileSystem 22.3.0 linked-child proof uses CreateSymbolicLink / ReparsePoint / LinkTarget and proves no yield/no descent, never target resolution below root. Linked-root proof resolves final target and binds containment/canonical identity to resolved root only; Mock does not enumerate linked contents. Production resolves configured root only, never linked children. If linked-root content discovery is asserted, prove that at real-filesystem integration tier rather than pretending Mock traversed it; no new dependency/port. Existing supported root behavior remains required.
7. Add these exact delivery glossary entries in CONTEXT.md (Candidate Movie Folder remains Phase A):

**QuotaDeferred**
The settlement outcome for a movie whose metadata lookup stopped on provider quota exhaustion: it stays Pending, keeps its attempt, and is re-dispatched at `EnrichmentDeferredUntil`. It is not a failure and never enters retry.
_Avoid_: quota failure, quota retry, paused import

**EnrichmentDeferredUntil**
The persisted next-UTC-midnight instant (from the injected TimeProvider) before which a QuotaDeferred movie must not be claimed, swept, or dispatched.
_Avoid_: retry-after, next retry time, pause timestamp

**quota-deferred** (routing)
The routing action and durable queue (`enrichment.quota-deferred`) carrying the same-attempt `EnrichmentRequested` until `EnrichmentDeferredUntil`, when it dead-letters into requested.
_Avoid_: retry queue, delayed retry

DEV-426 owns QuotaExhausted and chain eligibility; verify landed glossary/eligibility at T001, never duplicate its implementation in DEV-333. T016 composes real chain with controlled provider seams to prove successful fallback has zero deferral and chain-final quota defers.

8. Patron confirms PLAN_CHALLENGE rejected/carried dispositions, including Sentry H1 `noted, no ticket`; no Rigger tracker ask. Narrow routing extension: existing sealed record Disposition(string Action,string RoutingKey,int? NextAttempt) gains optional DateTimeOffset? DeferredUntil = null; add QuotaDeferred action constant and one Decide outcome arm returning quota-deferred routing key, null NextAttempt and deadline. Existing failure arms/constructions stay unchanged; consumer uses delivered original Attempt. No enum or routing rewrite.

Nondecision convention fixes remain required: new domain tests in UnitTests only; AutoFixture/Faker.Net and Theory/MemberData for repeated input cases; no unassigned TelemetryConstants.cs write target; actual SUT method names; Scope-item-5 citation; binder fractional/nonnumeric failure distinct from integer Range validation; early stray unchanged deadline/no extra publish; blocked-writer cancellation/consumer-fault proof without concurrent discovery task. Quill owns spec/plan/tasks/receipt corrections only; Keel records decisions and adjudication. Two-round cap unchanged.
