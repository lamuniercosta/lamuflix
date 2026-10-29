
---

## Grill exchange Q1/12 — 2026-10-29

Question from Keel to Patron:

> [from Keel] /grill-with-docs DEV-333. Phase 2 step 1, cap 12 questions, one decision at a time. Use notes DEV-333, recon-DEV-333 and recon-DEV-333-2 plus specs/PRODUCT.md as authority. I record full exchanges in specs/DEV-333/CONCLUSIONS.md and close brief.md; you rule, Quill drafts later. Q1/12: Which contract joins bulk-import progress to quota pause/resume? Recon section 3 says ImportMovieFolderCommandHandler only scans, persists and stages EnrichmentRequested in the outbox; no provider result reaches that handler. Recon part 2 section 3 says DEV-426 gate exposes only consuming TryAcquireAsync and its transport handler returns RateLimited on exhaustion. Thus a scanner pre-acquisition would spend an allowance without an HTTP attempt, while merely importing candidates never observes exhaustion. I recommend preserving one reservation per physical attempt and the existing import/outbox boundary, and explicitly settling an observable, non-consuming bulk pause/resume composition rather than adding speculative pre-acquisitions or direct provider calls. Cost: the join must be specified and may need targeted recon or a structural ticket ruling. What precise composition do you rule, with cited basis? If existing facts cannot support a decision, identify the one missing recon fact. DEV-426 is frozen unapproved spec at fee5c7a8, not landed; no implementation approval is implied. Report back with maestri ask Keel using your full ruling.

Patron reply:

> [from Patron] DEV-333 Q1/12 ruling: partially settled, invariant ruled, one missing recon fact named; full ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q1-ruling.md - record it in CONCLUSIONS.md and route the named fact as needs-recon before drafting pause/resume wording.

Full Patron ruling (verbatim):

# DEV-333 Q1/12 — Patron ruling: bulk-import progress to quota pause/resume contract

Verdict: PARTIALLY SETTLED — invariant ruled now; pause/resume composition NOT YET DECIDABLE. One missing recon fact named below. Do not draft a pause/resume mechanism in spec.md or brief.md beyond recording this ruling and the open fact. No implementation approval is implied, and DEV-426 remains frozen unapproved at fee5c7a8.

## Settled now (cited basis)

1. One reservation per physical attempt, and nowhere else. The scanner walk, the bounded-channel producer/consumers, and ImportMovieFolderCommandHandler must not call TryAcquireAsync, must not pre-acquire, and must not call any metadata provider directly.
   - Basis: ticket Scope item 3 (each OMDb lookup goes through IOmdbRequestGate) and item 4 (each folder goes through ImportMovieFolderCommandHandler, outbox staging from DEV-329 applies), verbatim in recon-DEV-333-2 section 1.
   - Basis: recon-DEV-333 section 3 — ImportMovieFolderCommandHandler only scans, persists, and stages EnrichmentRequested in the outbox; no provider result reaches that handler.
   - Basis: recon-DEV-333-2 section 3 — the frozen DEV-426 gate is consumed only by TryAcquireAsync, one reservation authorises exactly one outbound transport attempt (including each automatic retry as its own attempt), no refunds, and its transport handler returns RateLimited on exhaustion. A scanner pre-acquisition would therefore spend allowance with no HTTP attempt, which contradicts that contract.
2. The existing import/outbox boundary is preserved. Bulk import joins quota only asynchronously, downstream of the staged EnrichmentRequested, where the DEV-426 handler is the sole acquirer. Merely importing candidates never observes exhaustion, so no import-side pause signal exists in current facts.
   - Basis: recon-DEV-333 sections 3 and 5; recon-DEV-333-2 section 3 (gate has no pause/wait API and exposes no reopen timestamp; scan pause/resume at next UTC day is DEV-333's own obligation).
   - specs/PRODUCT.md is silent on quota, bulk pause, and RateLimited (only the RabbitMQ enrichment worker is named), so the ticket text above is the governing authority; no PRODUCT.md basis exists for a different join.

## Not settled — the one missing recon fact

What is the complete downstream fate, in time and attempts, of one enrichment item that receives the gate-generated RateLimited: the configured RetryDelayed delay (queue TTL or equivalent), the MaxAttempts value it is bounded by, and whether that item is still pending rather than terminally failed at the next UTC midnight — including whether gate-generated RateLimited is distinguishable downstream from a provider HTTP 429 RateLimited.

Why this single fact is decisive: code already observed in the DEV-333 worktree (EnrichmentRetryPolicy: RateLimited maps to RetryDelayed, bounded by attempt >= maxAttempts; EnrichmentFailureCategory.RateLimited is retryable) shows a bounded delayed-retry path, not a pause-until-next-UTC-day path. If the delayed retry survives until rollover without terminal failure, the ruled composition is: import completes and stages, enrichment progress pauses by non-dispatch/requeue, and resume is re-dispatch at or after the next UTC day via injected TimeProvider (constitution Principle VII, recon-DEV-333-2 section 2; also the known-debt rule that RateLimited retries must be delayed, never tight loops). If it terminally fails before midnight, or gate-generated and provider-generated RateLimited are indistinguishable where the distinction is required, then no composition exists within the frozen DEV-426 contract: a non-consuming exhaustion/wait signal would be a new port/API shape in a frozen, owner-unapproved DEV-426 spec (Gate 1 unchecked, ADR-0031 Proposed), which must not be silently assumed in DEV-333.

## Direction to Keel

Record this ruling verbatim in specs/DEV-333/CONCLUSIONS.md, carry the missing fact as the sole open item for Q1, and route it as needs-recon through the existing Conductor path before Q2 proceeds on any pause/resume wording. Ticket test item 5 (fake gate asserting pause and resume) and AC3 (gate exhausted: no OMDb call, scan resumes once the gate reopens, fake clock) cannot be drafted as DEV-333 scanner tests until that fact lands, because on current facts the fake gate belongs to the DEV-426 transport handler, not to any scanner code.


---

## Grill exchange Q1/12 — resumed after downstream recon

Question from Keel:

> [from Keel] DEV-333 resume Q1/12, not a new question. Recon complete in recon-DEV-333-2 downstream-fate sections 1-4: default MaxAttempts=3 and 30s retry TTL produce terminal Failed and dead-letter at about 60s, no automatic revival; gate exhaustion and HTTP 429 both collapse into RateLimited, TMDb success can mask exhaustion. Your prior invariant and entire Q1 exchange are saved in specs/DEV-333/CONCLUSIONS.md. Please now settle the composition or issue the exact blocked: structural owner question if a ticket change or constitution departure is necessary, citing the basis. My recommendation is to preserve DEV-333 ticket Scope 3 and AC3, the import/outbox boundary, and one reservation per physical attempt; do not silently reuse terminal bounded retry as pause, or assume a new gate wait/status API in unapproved DEV-426. A quota-specific resumable outcome and day-aware settlement would need an explicit ownership/contract ruling, including whether it changes any ticket delivery. Cost: conditional plan work or an owner checkbox, rather than claiming the existing path satisfies pause/resume. Decide the precise permitted mechanism and owned contract changes, or the precise owner question; distinguish an implementation dependency from a user escalation under PRODUCT section 5. Return the full cited ruling via a file and maestri ask Keel. We still need to settle remaining scanner decisions and loop terms before brief closure, within 12 total questions.

Patron reply:

> [from Patron] DEV-333 Q1/12 resumed ruling: settled, enrichment-side quota-specific day-aware composition, no owner question; full cited ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q1-resume-ruling.md - record verbatim in CONCLUSIONS.md and proceed to Q2 within the cap.

Full Patron ruling (verbatim):

# DEV-333 Q1/12 resumed — Patron ruling: quota pause/resume composition SETTLED

Verdict: SETTLED. Adopt the enrichment-side, quota-specific, day-aware settlement below. No DEV-333 owner question is raised. The DEV-426 contract amendment ruled here is an implementation dependency for Patron decision and DEV-426 Gate 1 approval, not a user escalation from DEV-333. Record verbatim in specs/DEV-333/CONCLUSIONS.md.

## The permitted mechanism (precise)

1. Import side unchanged. The walk, bounded channel, and ImportMovieFolderCommandHandler never call TryAcquireAsync, never pre-acquire, never call a provider, and are not paused by quota. Importing and staging EnrichmentRequested may complete while the budget is spent; exactly-once import (ticket AC1) and the MaxConcurrency cap (AC2) are unaffected.
   Basis: prior Q1 ruling, settled and saved; recon-DEV-333 section 3; ticket Scope items 3-4 in recon-DEV-333-2 section 1.
2. The gate stays as frozen. IOmdbRequestGate keeps only TryAcquireAsync(CancellationToken); one reservation per physical HTTP attempt, no refunds, rejected acquisition consumes nothing and makes zero transport calls. No peek, status, wait, or reopen-timestamp member is added to the gate, and the scanner acquires nothing.
   Basis: recon-DEV-333-2 section 3 (fee5c7a8 FR-001, FR-005, FR-006); Keel recommendation adopted on this point.
3. Quota exhaustion must be distinguishable where settlement decides. At the provider boundary, before any collapse into the shared category, a gate rejection surfaces as a quota-specific outcome to ProcessEnrichmentCommandHandler settlement, distinct from provider HTTP 429 RateLimited. Downstream of the provider the two are currently indistinguishable, and that indistinguishability is exactly what this amendment removes.
   Basis: recon-DEV-333-2 downstream-fate section 4 (both collapse into EnrichmentFailureCategory.RateLimited rate_limited; separable only provider-side, pre-mapping, in logs).
4. Day-aware settlement for the quota-specific outcome only. An item settling with the quota outcome: stays EnrichmentStatus.Pending (never MarkFailed, never dead-lettered for quota), does not consume its attempt (the attempt count does not advance toward MaxAttempts for a quota outcome), makes no further dispatch for that item before the next UTC day, and is re-dispatched at or after the next UTC midnight derived from injected TimeProvider. Provider HTTP 429 keeps today's behaviour unchanged: RateLimited, RetryDelayed, terminal Failed after MaxAttempts.
   Basis: ticket Scope item 3 and AC3 (when the budget is spent the scan pauses and resumes at the next UTC day; it does not fail; with the gate exhausted no OMDb call is made and the scan resumes once the gate reopens, fake clock); recon downstream-fate sections 2-3 prove the existing path cannot deliver this (MaxAttempts 3, 30s retry TTL, terminal Failed and dead-letter at about 60s, no automatic revival, StrandedMovieSweeper sees Pending only); constitution Principle VII (TimeProvider for the UTC boundary) and the known-debt rule that RateLimited retries must be delayed, never tight loops, per recon-DEV-333-2 section 2. Reusing the bounded 30s retry as the pause is expressly ruled out.
5. Observable bulk pause/resume is therefore enrichment progress, not import progress. Bulk progress stalls on enrichment settlement while exhausted, no bulk-imported movie fails for quota, and settlement resumes automatically after rollover with no explicit user re-request. The mechanism is global to OMDb quota exhaustion, not bulk-only, because EnrichmentRequested carries no bulk-run identity and none is added; a single import hitting quota exhaustion gets the same does-not-fail settlement, which changes no DEV-333 delivery.
   Basis: recon downstream-fate sections 1-3 (attempt counter rides EnrichmentRequested(MovieId, Attempt); revival today needs explicit re-enqueue, which AC3 forbids as the resume path).
6. Chain behaviour preserved. The quota outcome remains fallback-eligible in ChainedMetadataProvider exactly as RateLimited is today, so a TMDb success still completes the item and masks exhaustion; only a chain-final quota outcome triggers rule 4. AC4 (500-movie dry run against a fake provider, no RateLimited) is unaffected, as the fake provider bypasses transport and gate.
   Basis: recon downstream-fate section 4 (ChainedMetadataProvider.cs fallback eligibility; TMDb disabled by default).

## Owned contract changes (Patron care-list rulings, PRODUCT section 5)

- DEV-426 contract amendment (dependency, Patron-ruled): DEV-426 FR-006's map-quota-exhaustion-to-existing-RateLimited is superseded by rule 3 above — quota exhaustion reaches enrichment settlement as a distinguishable quota-specific outcome. IOmdbRequestGate itself, the omdb_daily_usage table, DailyLimit, and ADR-0031's gate design are unchanged. Classification under PRODUCT section 5: Care item 2 Architecture (an internal Core failure/outcome contract, not a new project, layer, or port) — no new port is created, so no new ADR and no constitution port-list change beyond the 1.7.0 staging DEV-426 already carries. It is not Care item 4 API Shape (no public route, DTO field set, or HTTP status changes), not Care item 3 (no schema beyond DEV-426's own named table), and not an owner ticket change from DEV-333: the DEV-333 ticket already declares Depends on DEV-426 and Scope item 3, and this amendment is the only composition that delivers DEV-333 Scope 3/AC3 as written. DEV-426's own unchecked Gate 1 owner checkbox (approve ADR-0031 and constitution 1.7.0) remains the owner's approval point and must be updated by the DEV-426 lane to name the quota-specific outcome before approval; a Patron answer never closes that checkbox, DEV-333 implementation remains blocked on DEV-426 landing, and if the owner rejects the amended DEV-426 contract at its Gate 1, DEV-333 Scope 3 returns here as blocked: structural.
- DEV-333 file-scope ruling (Care item 6, forced edit): enrichment settlement files the DEV-333 ticket does not name (ProcessEnrichmentCommandHandler settlement, provider outcome mapping, routing/consumer re-dispatch) may be edited under DEV-333/DEV-426 delivery because AC3 forces the edit; cite AC3 in the spec. No DEV-333 schema change, no new dependency, no public API change, nothing touching Features:LocalPlay, secrets, or Process.Start.

## What is NOT changed

DEV-333 ticket Scope and ACs stand verbatim; scan in Scope 3/AC3 is read at the bulk-operation level (import plus its enrichment settlement), which is the only reading compatible with the settled import/outbox boundary. No ticket delivery is added or dropped, and no constitution departure is required (TimeProvider use is constitution-mandated, not a departure), so no blocked: structural question is issued. Remaining scanner decisions and loop terms continue within the 12-question cap.


---

## Grill exchange Q2/12

Question from Keel:

> [from Keel] DEV-333 Q2/12: What concrete mechanism preserves quota-pending items until UTC rollover across retries, cancellation/restart and StrandedMovieSweeper intervention? Q1 resumed ruling is appended verbatim and read back. I recommend a durable day-aware re-dispatch path with injected TimeProvider, retaining the same attempt and Pending state, with an explicit sweeper rule preventing early re-enqueue; no in-memory sleeping consumer for up to 24h and no use of the existing 30s retry budget. Cost: broker/persistence settlement wiring must be explicit, and any additional schema requires your care-list ruling. Please settle the owned mechanism, crash/ack ordering and sweeper interaction on the recon evidence; if existing claim/delay contracts are essential but absent, name one precise recon request instead of assuming them. Also make the DEV-426/DEV-333 ownership split precise: which lane changes provider outcome and which lane owns settlement/routing. Basis: ticket Scope 3 and AC3; recon downstream-fate sections 1-4. Return full cited ruling by file and ask Keel.

Patron reply:

> [from Patron] DEV-333 Q2/12 ruling: settled, database-anchored deferral plus durable broker re-dispatch, sweeper as post-expiry backstop; full cited ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q2-ruling.md - record verbatim in CONCLUSIONS.md and proceed to Q3.

Full ruling (verbatim; Q2 explicitly supersedes Q1 no-schema expectation with its cited care-item-3 approval):

# DEV-333 Q2/12 — Patron ruling: durable quota-pending mechanism SETTLED

Verdict: SETTLED on existing recon; no further recon needed. Adopt a database-anchored deferral plus durable broker re-dispatch, with the StrandedMovieSweeper as post-expiry backstop only. Record verbatim in specs/DEV-333/CONCLUSIONS.md.

## Mechanism (contract level; queue mechanics below this line are plan detail)

1. Deferral is persisted on the movie, never held in memory. On a quota-specific outcome (Q1 resumed ruling rule 3), DEV-333 settlement sets one new nullable deferral timestamp on the movie to the next UTC midnight computed from injected TimeProvider, leaves Status = Pending, writes no failure category as a terminal failure, and does not advance the message attempt toward MaxAttempts. No consumer sleeps, no Task.Delay-until-midnight, no in-memory timer survives or needs to survive a restart.
   Basis: recon downstream-fate sections 2-3 (today the item terminally fails at about 60s: EnrichmentOptions.MaxAttempts = 3 at EnrichmentOptions.cs:12-13, RabbitMqOptions.RetryDelay 30s, MarkFailed plus dead-letter, sweeper sees Pending only); EnrichmentOptions.cs:15-17 and docker/api.settings.example.json:22-23 (SweepInterval 1 minute, ClaimLease 10 seconds defaults).
   Care-list ruling (PRODUCT section 5, Care item 3): one nullable timestamp column on the existing movies table is approved under DEV-333, owned by the DEV-333 lane including EF mapping, Movie rehydration, and migration. It adds no table and touches no DEV-426 schema. Setting LastAttemptAt into the future instead is expressly rejected: that column means last attempt time, drives claim-lease maths at EfMovieRepository.cs:680-687, and lying in it corrupts both.
2. Primary re-dispatch is a durable broker message, published with the same attempt. The consumer publishes EnrichmentRequested(MovieId, same Attempt) to a durable day-aware delayed re-dispatch path whose message expires at the persisted deferral instant and then dead-letters into the requested queue. The existing 30s retry queue and its Retry/RetryDelayed routing keys must not be reused for quota (EnrichmentRouting.cs:27-33 routes both identically to the 30s TTL queue, RabbitMqTopology.cs:68-77). Quota items never enter that budget.
   Basis: Q1 resumed ruling rule 4; recon downstream-fate sections 1-2.
3. The sweeper cannot re-enqueue early, and the claim cannot fire early — both read the deferral. FindStrandedMovieIdsAsync (EfMovieRepository.cs:697-706, today Pending plus stale LastAttemptAt only) must exclude movies whose deferral is in the future and include them once the deferral has passed. TryClaimForEnrichmentAsync (EfMovieRepository.cs:677-695, today Pending plus lease-expired LastAttemptAt) must likewise refuse a claim before the deferral instant, and a successful post-expiry claim clears the deferral in the same update. These two gates make any stray or duplicate message before midnight a claimed-false no-op ack, with no provider call and no new delayed message.
   Basis: SweepStrandedMoviesCommandHandler.cs:20-28 (lease cutoff from ClaimLease via TimeProvider), RequeueStrandedMoviesCommandHandler.cs:16 (today it re-enqueues at attempt 1 — see rule 5 for its narrowed role).
4. Crash and ack ordering, in this exact order. (a) Handler persists the deferral with SaveChangesAsync and returns the quota outcome. (b) Consumer publishes the delayed re-dispatch message. (c) Consumer acks the original delivery. Publish strictly before ack, both on CancellationToken.None as today (EnrichmentConsumer.cs:266-267). Cancellation of the consumer token during lookup keeps today's propagation (ProcessEnrichmentCommandHandler.cs:67-69); a host restart loses nothing because deferral is in PostgreSQL and the re-dispatch message is on a durable quorum path.
   Crash windows, all closed: death before publish leaves the original unacked for redelivery, and rule 3 turns that redelivery into a no-op ack while rule 5 guarantees eventual re-dispatch; death after publish but before ack produces one duplicate delivery, which the atomic claim (ExecuteUpdateAsync, EfMovieRepository.cs:681-687) suppresses — exactly one claim wins, the loser acks and drops; death after ack is fully settled. No window dead-letters or fails a quota item.
   Basis: EnrichmentConsumer.cs:243-268 (today's decide, publish, ack sequence); ProcessEnrichmentCommandHandler.cs:27-30 (unclaimed returns Completed(Claimed: false)).
5. Sweeper after expiry is backstop, not the driver. Once the deferral passes, a quota-pending movie whose delayed message was somehow lost is stranded-eligible (Pending, stale LastAttemptAt) and the existing sweeper re-enqueues it on its normal 1-minute interval. If the delayed message fires first, its claim refreshes LastAttemptAt and clears the deferral, so the sweeper sees no stranded movie. The sweeper's existing attempt-1 re-enqueue (RequeueStrandedMoviesCommandHandler.cs:16) applies only in this lost-message recovery, matching today's stranded semantics for all movies; attempt retention is guaranteed on the primary broker path, and no sweeper change beyond rule 3's exclusion is permitted (it must never clear a deferral early or enqueue before expiry).
6. Non-quota paths are untouched. Provider HTTP 429 keeps RateLimited, RetryDelayed, the 30s queue, and terminal Failed after MaxAttempts (EnrichmentRetryPolicy.cs:19-21, ProcessEnrichmentCommandHandler.cs:145-155). SweepInterval, ClaimLease, and MaxAttempts defaults do not change for quota or for anyone else.

## Ownership split (precise)

- DEV-426 lane owns only the provider outcome: gate and MetadataQuotaHandler unchanged (TryAcquireAsync only), plus the FR-006 amendment from Q1 so OmdbMetadataProvider surfaces a quota-specific outcome before collapse into rate_limited, with chain fallback eligibility preserved. It owns no Movie state, no settlement, no routing, no deferral.
- DEV-333 lane owns everything downstream and everything scanner-side: the quota settlement branch in ProcessEnrichmentCommandHandler, the deferral column and its claim/sweeper gates in EfMovieRepository, the routing disposition and publish-before-ack wiring in EnrichmentConsumer/EnrichmentRouting, the delayed re-dispatch topology, and the unchanged import/outbox scanner path.
- Interface between the lanes is exactly one thing: the quota-specific outcome type crossing IMetadataProvider's result. No new Core port is created (no ADR, no whitelist change); IMovieRepository gains no new member — its two existing enrichment queries change predicate only, which is DEV-333 Care item 6 forced-edit territory already ruled under AC3 in Q1.

AC3 is the forcing basis throughout: gate exhausted means no OMDb transport call, no failure, and automatic resume once the gate reopens under a fake clock — this mechanism is the durable form of that sentence. Q3 may proceed.


---

## Grill exchange Q3/12

Question from Keel:

> [from Keel] DEV-333 Q3/12: What is the one-shot bulk import entry point and architectural placement? Recommend a cancellable explicitly invoked ImportLibraryCommand/handler within Core Features/Import, streaming discovery through a new Core port ILibraryFolderDiscovery returning IAsyncEnumerable<LibraryPath>, Infrastructure filesystem adapter using existing System.IO.Abstractions, and Infrastructure orchestration for per-folder DI scopes using existing import handler. Preserve IMediaLibraryScanner.Scan as single-folder; avoid cross-feature references and Core dependence on DI. No recurring schedule, automatic startup scan, new public route/DTO, or external package. Cost: this delivers an internal one-shot capability until a separately scoped trigger is requested; a new discovery port needs a focused ADR and closed-port whitelist/constitution alignment mandated by Principle I. Please rule precise names/placement, trigger, ownership of scope creation, and whether the ticket-approved discovery port requires any owner checkbox versus ordinary cited ADR/list update. Do not approve any layering that requires Core to reference Infrastructure or DI. Basis: ticket Scope 1,2,4; out-of-scope recurring schedule; recon constitution section 2 and scoped import registration. Q2 saved verbatim and read back. Return full ruling by file and ask Keel.

Patron reply:

> [from Patron] DEV-333 Q3/12 ruling: settled, Core ImportLibrary command and discovery port with an Infrastructure scope-execution seam, explicit invocation only, ADR without owner checkbox; full cited ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q3-ruling.md - record verbatim in CONCLUSIONS.md and proceed to Q4.

Full ruling (verbatim):

# DEV-333 Q3/12 — Patron ruling: one-shot bulk import entry point and placement SETTLED

Verdict: SETTLED as below, with one refinement to Keel's recommendation: per-folder DI scopes require an explicit execution seam, ruled here, because the decorated import handler is scoped and one shared scope cannot serve MaxConcurrency concurrent consumers. No owner checkbox. Record verbatim in specs/DEV-333/CONCLUSIONS.md.

## Names and placement

1. Command and handler live in Core Features/Import: sealed record ImportLibraryCommand, sealed ImportLibraryCommandHandler implementing ICommandHandler<ImportLibraryCommand, TResult>. The command carries no payload: the root comes from validated LibraryOptions.RootPath (constitution Principle VII: validated options, no hardcoded roots or magic-string configuration reads), and LibraryOptions.MaxConcurrency (default 4, ticket Scope item 2) bounds the channel consumers. TResult is deliberately not locked here; it is decided with the outcome/failure question later in this grill — do not default it to Unit in the spec. Handler dependencies are exactly three ports/options (discovery, folder importer, LibraryOptions), at the constitution three-port guideline, with CancellationToken flowing through every async path and no sync-over-async (recon-DEV-333-2 section 2).
   Basis: ticket Scope items 1, 2, and 4 (recon-DEV-333-2 section 1); constitution Principle II (sealed record commands, sealed handlers, explicit registration — AddHandler at HandlerRegistration.cs and Pipeline/ServiceCollectionExtensions.cs:28-35).
2. New Core port ILibraryFolderDiscovery in LamuFlix.Core/Ports, returning IAsyncEnumerable<LibraryPath> with the CancellationToken flowing into enumeration ([EnumeratorCancellation]). Its Infrastructure adapter FileSystemLibraryFolderDiscovery sits beside DirectoryMediaLibraryScanner in LamuFlix.Infrastructure/FileSystem, uses the existing System.IO.Abstractions seam and IOptions<LibraryOptions>, and recursively yields candidate movie folders under RootPath. IMediaLibraryScanner.Scan(LibraryPath) stays exactly as it is — single-folder, synchronously reused inside the existing import handler per candidate (DirectoryMediaLibraryScanner.cs single-directory scope is untouched; recon-DEV-333 sections 2 and 4).
   Basis: ticket Scope item 1 names this port split verbatim; constitution file-system seam (MockFileSystem in tests) per recon-DEV-333-2 section 2.
3. Execution seam for per-folder import: new Core port IMovieFolderImporter in LamuFlix.Core/Ports (Task<MovieId> ImportAsync(LibraryPath folder, CancellationToken cancellationToken)), with Infrastructure adapter ScopedMovieFolderImporter in a new LamuFlix.Infrastructure/Import folder. Core must not reference DI, Infrastructure, or IServiceScopeFactory (constitution Principle I: Core references BCL only), so this adapter — not the Core handler — owns scope creation: one CreateAsyncScope per folder import, resolving ICommandHandler<ImportMovieFolderCommand, MovieId> inside it, awaiting it, and disposing the scope. Each channel consumer therefore gets its own scope and its own EF-backed repository for the folder, which is the StrandedMovieSweeper/OutboxRelayHostedService scope precedent (IServiceScopeFactory plus CreateAsyncScope per pass, recon-DEV-333 section 5) applied per folder.
   Why the seam is mandatory, not optional: AddHandler registers handlers scoped (ServiceCollectionExtensions.cs:28), IMediaLibraryScanner is scoped (HandlerRegistration.cs), and a single shared scope's DbContext cannot be used by 4 concurrent consumers. Ticket Scope item 4 (each folder goes through ImportMovieFolderCommandHandler, so DEV-329 outbox staging applies) is preserved literally inside the adapter; the scanner pipeline itself never touches the import handler type, so there is no cross-feature reference (constitution: Feature folders MUST NOT reference each other; shared concepts live in Ports) and no Core-to-Infrastructure reference in either direction of this design.

## Trigger

Explicit invocation only. A caller resolves ICommandHandler<ImportLibraryCommand, TResult> from a fresh DI scope and awaits it with the caller's CancellationToken; cancelling that token stops discovery, production, and in-flight folder imports through normal token flow. DEV-333 registers everything through the existing HandlerRegistration composition (AddHandler for the new handler, scoped registrations for both ports' adapters) and its first invokers are tests. There is no recurring schedule (ticket Out of Scope), no startup/automatic scan, no BackgroundService, and no public route or DTO: POST /movies/import stays the only import surface, unchanged.
Basis: ticket Scope and Out of Scope (recon-DEV-333-2 section 1); recon-DEV-333 section 3 (existing endpoint surface) and section 5 (hosted-service patterns considered and not adopted here). Cost accepted as Keel states it: DEV-333 delivers an internal one-shot capability until a separately scoped ticket requests a trigger surface.

## New port: ADR yes, owner checkbox no

- A focused ADR is required and sufficient. Constitution Principle I, lines 192-196: ports are a closed list and Adding a port requires an ADR. The ADR covers both new ports named above (discovery and the scope-execution seam), why the seam cannot live in Core, and the rejection of pausing or acquiring anything in the scanner (cross-reference Q1/Q2). The ArchitectureTests port whitelist (ArchitectureTests.cs PortWhitelistPattern, recon-DEV-333 section 2) and the constitution Principle I list are updated in the same PR as ordinary alignment following that ADR, with any new domain term added to CONTEXT.md in the same PR (Principle VIII).
- No owner checkbox and no blocked: structural. The ticket's Scope item 1 explicitly mandates a discovery service behind a port in LamuFlix.Core — ticket text counts as decided, so this is not a Patron assumption and not an escalation. Under specs/PRODUCT.md section 5 this is Care item 2 Architecture (new internal ports within existing projects), which Patron rules with cited basis; it is not Care item 4 (no public route/DTO/status), creates no project, layer, or dependency (System.Threading.Channels and System.IO.Abstractions are already in use; IAsyncEnumerable is BCL), and follows the constitution's own port process rather than departing from it. Contrast DEV-426: there the port itself is not ticket-mandated inside DEV-426's own delivery being approved at its Gate 1, so its owner checkbox stands untouched by this ruling.
- Forbidden expressly: Core referencing IServiceScopeFactory, IServiceProvider, or Infrastructure; a second registration lifetime hack (transient handler over a shared scoped DbContext) instead of the ruled seam; and any new public route/DTO or scheduler smuggled in as the trigger.

Q4 may proceed.


---

## Grill exchange Q4/12

Question from Keel:

> [from Keel] DEV-333 Q4/12: What exactly is a candidate movie folder and the traversal boundary? Recommend every ordinary directory at or below RootPath, including the root itself, with at least one supported top-level video using the existing scanner extension semantics; import one candidate per directory, selecting its primary video via the unchanged scanner; still descend when a parent qualifies so nested movies are found. Stream lazily, never materialize the library, use canonical root-relative LibraryPath values, do not follow directory symlinks/reparse points (cycles and escape), and exclude outside-root paths. Container-only and empty directories are traversal nodes, not imports. Cost: linked media trees are excluded and discovery must inspect top-level files without duplicating primary-file/title parsing. Please rule these boundaries, path identity/casing and supported-extension sharing placement so no feature-to-feature reference or second divergent video-extension list is introduced. If there is a new domain term for CONTEXT.md, specify its glossary definition. Facts: recon scanner section 4 lists supported extensions and largest-file/name tie break; AC1 requires nested folders exactly once. Q3 saved verbatim and read back. Return full ruling by file and ask Keel.

Patron reply:

> [from Patron] DEV-333 Q4/12 ruling: settled, candidate and traversal boundaries with canonical path identity, single shared extension list, and one CONTEXT.md glossary term; full cited ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q4-ruling.md - record verbatim in CONCLUSIONS.md and proceed to Q5.

Full ruling (verbatim):

# DEV-333 Q4/12 — Patron ruling: candidate folder and traversal boundary SETTLED

Verdict: SETTLED. Keel's recommendation is adopted with the identity and sharing rules below. Record verbatim in specs/DEV-333/CONCLUSIONS.md.

## Candidate and traversal

1. Candidate definition. A candidate movie folder is a directory at or below LibraryOptions.RootPath, including the root itself, that contains at least one supported video file directly in it (top level only, never nested). One candidate yields exactly one import; its primary video, title, year, and format continue to come solely from unchanged IMediaLibraryScanner.Scan selection and FolderNameCleaner (DirectoryMediaLibraryScanner.cs:54-59 largest file, ordinal name tie-break; :38-40 cleaned title/year). Discovery may inspect which files exist and their extensions; it must not select the primary file, parse title or year, or otherwise duplicate scanner semantics.
   Basis: ticket AC1 (nested folders imported exactly once) and Scope items 1-2, recon-DEV-333 section 4 (21 scanner tests lock today's semantics), DirectoryMediaLibraryScanner.cs:22-40.
2. Traversal always descends. Qualifying as a candidate never prunes the walk: children of a candidate are still traversed, so a movie directly in the root and a movie three levels down are both found exactly once. Container-only and empty directories are traversal nodes only — descended into, never yielded. Nothing is materialized: discovery streams lazily over IAsyncEnumerable<LibraryPath> (Q3 ruling), enumerating each directory's files and children on demand, so channel backpressure reaches the walk.
   Basis: ticket AC1 nested requirement; Q3 ruling on streaming; Keel recommendation adopted.
3. Links are never followed. A subdirectory carrying the reparse-point/symlink attribute is neither yielded nor descended into, whatever it contains or points at. This is a hard exclusion, not a cycle-detection shortcut: linked media trees are out of scope and accepted as a cost. The configured root itself is canonicalized and accepted as the boundary even if it is reached through a link; the no-follow rule applies below it.
   Basis: Keel recommendation; no ticket text mandates linked trees (specs/PRODUCT.md is silent), so excluding them narrows nothing the ticket delivers.

## Path identity and casing

4. Candidates are yielded as canonical LibraryPath values relative to the canonicalized root: full root path with trailing separator removed is the comparison base, every yield is the root-relative path built from enumerated segments with OS separators, no trailing separator, no leading separator, and no dot segments. The root itself is the single exception: it yields as the canonical absolute root path (a rooted LibraryPath, which scanner resolution passes through unchanged, DirectoryMediaLibraryScanner.cs:45-47), because the empty relative path is not a valid LibraryPath (LibraryPath.cs:32-33 requires non-blank). The scan-side Resolve mechanism (relative combined onto RootPath) is unchanged and becomes the only place relative yields are re-anchored.
5. No case folding, ever. Enumeration-returned casing is preserved byte-for-byte, nothing is lowercased or normalized for comparison, and a yielded path's exact LibraryPath value is its identity for exactly-once purposes. Defence check before any yield: the candidate's canonical full path must equal the root or start with root plus one separator, ordinal-ignore-case matching the host filesystem, and anything escaping is skipped (never yielded, never descended). With links unfollowed and a single depth-first pass, the same directory cannot be reached twice; the spec must carry a test proving root, candidate-parent, and nested-candidate yields are exactly this canonical set, once each.
   Basis: LibraryPath.cs:6-16 (record identity is its string value; blanket dot-dot rejection), DirectoryMediaLibraryScanner.cs:43-52, ticket AC1.

## Supported-extension sharing

6. Exactly one extension list exists after this change. The FrozenSet at DirectoryMediaLibraryScanner.cs:19-20 moves to a single internal static holder in LamuFlix.Infrastructure.FileSystem (name: SupportedVideoExtensions, extension membership tested with the same OrdinalIgnoreCase comparison), consumed by both DirectoryMediaLibraryScanner (semantics provably unchanged: same members, same comparison) and FileSystemLibraryFolderDiscovery. It stays in Infrastructure — it is a filesystem-adapter concern, not a Core domain concept — so no Core port, Domain type, or feature-folder reference is introduced for it, and the existing scanner tests (extension normalization theory rows, recon-DEV-333 section 4) are the regression proof. Editing DirectoryMediaLibraryScanner.cs for this extraction is a Q3-style forced edit under ticket Scope item 1; its Scan behaviour must not change.
   Rationale: a second list in the discovery adapter would silently diverge candidate detection from primary-video selection — the exact failure this question exists to prevent. A candidate is therefore guaranteed importable by the scanner, since the scanner is guaranteed to find a supported top-level video in it.

## Glossary (CONTEXT.md, same PR, Principle VIII)

7. Add exactly this term:

**Candidate Movie Folder**
A directory at or below the Library root, including the root itself, containing at least one supported video file directly in it. It is imported exactly once as one Movie, and traversal continues into its subdirectories. A directory with no supported video file directly in it is only traversed, never imported. Directories reached through a link below the root are never candidates.
_Avoid_: scan folder, movie directory, container folder (for a candidate)

Q5 may proceed.


---

## Grill exchange Q4/12 — path clarification

Question from Keel:

> [from Keel] DEV-333 Q4 clarification before Q5, same decision: path boundary rule 5 says ordinal-ignore-case matching the host filesystem, but that comparison would accept a differently cased sibling on a case-sensitive deployment; rule 4 trims the root separator but must preserve filesystem roots such as C:\ and /. I recommend host-appropriate path comparison (Windows OrdinalIgnoreCase, case-sensitive hosts Ordinal) and root normalization preserving volume/filesystem roots, with no case-folding of yielded identity. Cost: tests must cover root-only and case-distinct sibling containment; no scope change. Please confirm this precise refinement or give your alternative. Record response by file and ask Keel. Q4 original ruling is appended verbatim and read back; glossary definition captured there for Quill rather than editing shared root documentation from this seat.

Patron reply:

> [from Patron] DEV-333 Q4 clarification: confirmed with host-appropriate comparison and filesystem-root-preserving normalization; full ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q4-clarification-ruling.md - record verbatim in CONCLUSIONS.md and proceed to Q5.

Full ruling (verbatim):

# DEV-333 Q4/12 clarification — Patron ruling: CONFIRMED with refinement

Verdict: Keel's refinement is adopted. It supersedes the host-filesystem comparison sentence in Q4 rule 5 and the trailing-separator sentence in Q4 rule 4; everything else in the Q4 ruling stands. No scope change. Record verbatim in specs/DEV-333/CONCLUSIONS.md alongside the original ruling.

1. Host-appropriate containment comparison. The rule 5 defence check (candidate canonical full path equals the root, or starts with root plus one separator) uses the comparison matching the host filesystem: OrdinalIgnoreCase on Windows, Ordinal on case-sensitive hosts. It must never accept a differently-cased sibling prefix on a case-sensitive deployment, and it must never use the comparison to alter a yielded path: no case-folding of identity, enumeration casing preserved byte-for-byte, and the exact LibraryPath value remains the exactly-once identity (LibraryPath.cs:6-16, Q4 rule 5 as amended).
   Basis: Q4 rule 5 was over-broad as written for non-Windows hosts; DirectoryMediaLibraryScanner and LibraryPath preserve supplied casing (DirectoryMediaLibraryScanner.cs:43-52, LibraryPath.cs).
2. Root normalization preserves filesystem roots. Canonicalize RootPath to its full path, then remove trailing directory separators only while the remaining length exceeds the length of its filesystem root (Path.GetPathRoot): C:\ stays C:\, / stays /, and a non-root root loses its trailing separators. The root yield, the containment base, and the root-plus-separator prefix in rule 5 all derive from this normalized root, so root-only and case-distinct sibling containment remain testable on both host families.
   Basis: Q4 rules 4-5 as amended; root itself yields as the canonical absolute root path because the empty relative path is not a valid LibraryPath (LibraryPath.cs:32-33).
3. Required test coverage in the spec: root-only library containment, case-distinct sibling rejection under Ordinal comparison, and volume/filesystem-root preservation (C:\ and / forms). Keel's note on glossary custody is accepted: the Q4 glossary definition stays captured in CONCLUSIONS.md for Quill to land in CONTEXT.md with the spec, and neither seat edits the shared root CONTEXT.md from this exchange.


---

## Grill exchange Q5/12

Question from Keel:

> [from Keel] DEV-333 Q5/12: What bounded-channel and termination contract should the one-shot pipeline guarantee? Recommend validated LibraryOptions.MaxConcurrency default 4, positive integer; bounded channel capacity exactly MaxConcurrency (one tuning option, no separate capacity knob), FullMode Wait, one producer and exactly MaxConcurrency consumers. Each consumer awaits one folder at a time through the scoped importer; producer awaits writes, never spins or drops, and lazy discovery must not prefetch the whole tree. On normal completion writer completes and readers drain; caller cancellation cancels producer/readers/in-flight imports, awaits all tasks and disposes scopes; a producer/consumer fault cancels peers and completes writer so a full buffer cannot deadlock. No per-item Task.Run or unbounded task lists. Cost: no independent queue-size tuning and completion order is unspecified. This decides lifecycle only; Q6 will settle the failure/result meaning. Please approve or refine, including overflow-safe option validation, bounded auxiliary traversal memory and no scope leaks. Basis: ticket Scope 2/AC2 plus constitution async rules. Q4 clarification recorded and read back. Return ruling by file and ask Keel.

Patron reply:

> [from Patron] DEV-333 Q5/12 ruling: settled, single-knob bounded channel with producer/consumer lifecycle and fault-safe termination; full cited ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q5-ruling.md - record verbatim in CONCLUSIONS.md and proceed to Q6.

Full ruling (verbatim):

# DEV-333 Q5/12 — Patron ruling: bounded-channel and termination contract SETTLED

Verdict: SETTLED. Keel's recommendation is adopted, with the memory, validation, and shutdown mechanics stated precisely below. This ruling decides lifecycle only; what an individual folder failure means and what the handler reports are Q6. Record verbatim in specs/DEV-333/CONCLUSIONS.md.

## Pipeline shape (only permitted shape)

1. One producer, exactly MaxConcurrency consumers, channel capacity exactly MaxConcurrency. Channel.CreateBounded<LibraryPath> with BoundedChannelFullMode.Wait, SingleWriter = true, SingleReader = false, AllowSynchronousContinuations = false. There is no separate capacity knob: LibraryOptions.MaxConcurrency (ticket Scope item 2, default 4) is the single tuning option, and capacity equals it. Worst case residence is therefore bounded and fixed: at most MaxConcurrency queued LibraryPath values, at most MaxConcurrency in the consumers' hands, and one in the producer's hand.
   Basis: ticket Scope item 2 and AC2 (concurrency never exceeds MaxConcurrency); constitution Principle I (System.Threading.Channels is BCL, recon-DEV-333-2 section 2).
2. Each consumer is a single long-lived loop created once at pipeline start (exactly MaxConcurrency consumer tasks total — never Task.Run per folder, never a task list that grows with the library). A consumer awaits one folder import through IMovieFolderImporter before reading the next, so at most MaxConcurrency imports — and therefore at most MaxConcurrency adapter-created DI scopes — are ever in flight.
   Basis: Q3 ruling (ScopedMovieFolderImporter owns one scope per folder); recon async rules (recon-DEV-333-2 section 2).
3. The producer awaits every WriteAsync. It never spins, polls, retries a write, drops an item, or writes after completion. Discovery stays lazy end to end: the producer pulls the next candidate only when ready to write it, so neither the walk nor the channel ever holds the whole tree (Q4 rule 2). Traversal auxiliary memory is depth only: one streaming directory enumerator per level of the current path, no materialized directory or file lists at any level, and a directory's top-level file scan stops at the first supported-extension match.
   Basis: ticket Scope items 1-2 (producer waits when the channel is full, read as async backpressure per recon-DEV-333-2 section 2); Q4 ruling.

## Option validation (overflow-safe)

4. LibraryOptions gains MaxConcurrency as an int, default 4, annotated [Range(1, int.MaxValue)], bound and startup-validated exactly as today's [Required] RootPath (LibraryOptions.cs:11-13, BindAndValidate per recon-DEV-333 section 1). Zero, negative, and non-integer configuration fail host startup; DEV-333 code performs no derived arithmetic on the value — no doubling, no capacity-times-anything — using it directly as consumer count and channel capacity, so no computed quantity can overflow.

## Termination (three ways out, one shutdown path)

5. Normal completion: producer finishes discovery, completes the writer exactly once, consumers drain the channel to empty and exit, the handler awaits every consumer task, and only then returns. Completion order of individual folders is unspecified and no caller may rely on it.
6. Caller cancellation: the handler runs the pipeline under a linked CancellationTokenSource created from the caller's token and disposed by the handler. Cancellation stops discovery, unblocks the waiting producer and readers, cancels in-flight imports through the token the importer passes to the scoped import handler, and the handler still awaits all producer and consumer tasks and lets every per-folder scope dispose before returning; the scope adapter disposes its scope on cancelled and faulted imports exactly as on success (await using, Q3 ruling). No task is abandoned and no scope outlives the handler call.
7. Producer or consumer fault: the first fault cancels the linked source so peers stop, completes the writer so no reader or writer can deadlock against a full buffer, and the handler awaits every pipeline task before anything propagates. How faults are counted, classified, or reported is Q6 and is not decided here; what is decided is that no fault can leave the producer blocked on a full channel, a consumer blocked on an empty one, a peer still running, or a scope undisposed.
8. Shutdown never loses queue safety properties: items already imported stay imported (Q1 outbox staging), items never claimed from the channel are simply not imported by this invocation — this one-shot capability is explicitly invoked again by its caller, and no recovery of un-imported candidates is promised by this contract.

Q6 may proceed.


---

## Grill exchange Q6/12

Question from Keel:

> [from Keel] DEV-333 Q6/12: What does bulk command success or failure mean? Recommend sealed record ImportLibraryResult(long ImportedCount), returned only after discovery and every folder import finish and outbox staging is saved; count successful import calls, never imply enrichment has completed. Empty valid root returns zero. Missing/unreadable root, unreadable subtree, disappearing candidate, invalid folder and persistence/import faults fail fast with the original meaningful failure after Q5 peer shutdown, not a misleading partial-success result; already committed folders remain committed. Caller cancellation propagates OperationCanceledException, not success. Cost: one damaged subtree aborts the invocation; the caller can repair and explicitly rerun. Exactly once is one attempt per candidate within one completed invocation, preserving existing single-folder persistence/idempotency behavior rather than inventing cross-run locks or transactions. Please decide result shape, fault precedence versus cancellation and these partial-progress/rerun semantics with ticket basis. Also say whether proposed fail-fast exclusions alter ticket scope; if so rule the existing-scope alternative or exact owner checkbox. Q5 recorded verbatim and read back. Return full ruling by file and ask Keel.

Patron reply:

> [from Patron] DEV-333 Q6/12 ruling: settled, fail-fast ImportLibraryResult with cancellation precedence and honest non-resuming rerun semantics, no owner checkbox; full cited ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q6-ruling.md - record verbatim in CONCLUSIONS.md and proceed to Q7.

Full ruling (verbatim):

# DEV-333 Q6/12 — Patron ruling: bulk success and failure meaning SETTLED

Verdict: SETTLED. Keel's fail-fast recommendation is adopted, with one correction to its rerun wording: an explicit rerun is a fresh invocation, not a resume, and on today's single-folder behaviour it will fault on an already-committed candidate — the spec must say so plainly instead of promising repair-and-rerun completion. No ticket-scope change and no owner checkbox. Record verbatim in specs/DEV-333/CONCLUSIONS.md.

## Result shape and success

1. ImportLibraryResult is a sealed record carrying one long ImportedCount, and it is the only thing a successful invocation returns. (This locks the TResult left open in Q3.) The handler returns it only after discovery has ended, the writer has completed, and every consumer has drained and finished (Q5). A count increments only when a scoped folder import returns, which is only after ImportMovieFolderCommandHandler has saved the movie and its staged EnrichmentRequested (ImportMovieFolderCommandHandler.cs:17-22: scan, create, add, stage, SaveChangesAsync, then return). ImportedCount therefore means movies persisted with enrichment staged — it never means enrichment has run, let alone completed (Q1/Q2: quota settlement happens downstream and may be deferred to the next UTC day).
   Basis: ticket Scope item 4 (import through the existing handler, outbox staging applies); Q3 result-shape deferral.
2. An existing, readable root containing no candidate movie folders is success with ImportedCount zero. A missing root or a root that cannot be traversed is not an empty library and never returns zero (rule 3).

## Failure: fail fast, original failure, committed work stays

3. The following fail the whole invocation, after the Q5 peer shutdown (cancel peers, complete the writer, await every task, dispose every scope): missing or unreadable root; an unreadable subtree during traversal; a candidate that disappears or becomes invalid between discovery and import (DirectoryMediaLibraryScanner.cs:26-36 already throws for missing folder or no supported video); a scan, invalid-folder, or persistence fault from the scoped import; and a duplicate library path under the existing unique index (ImportMovieFolderCommandHandlerPersistenceTests.cs:33-83: second import of one path is rejected by IX_movies_library_path, leaving exactly one movie, one outbox row, one import metric). No partial-success result is returned, no failure is converted into a count or a warning, and one damaged subtree aborts the invocation. Folders committed before the fault remain committed — there is no cross-folder transaction and none is invented; the per-folder save (movie plus its outbox row) is the only atomic unit.
4. Fault versus cancellation precedence. After all pipeline tasks have been awaited: if the caller's own token requested cancellation, OperationCanceledException propagates (a TaskCanceledException is acceptable; a result never is). Otherwise the first genuine fault propagates as its original exception with its original identity and stack — specifically not the linked-token cancellation of its peers, not an AggregateException, and not a generic bulk-import wrapper that would lose the meaningful type the existing API and handler taxonomy rely on. Peer cancellations caused by the fault are shutdown noise and are suppressed only after the pipeline is fully awaited. A disappearing candidate racing caller cancellation still yields cancellation when the caller's token fired; a fault that completed before any cancellation yields the fault.
   Basis: constitution expected-failure taxonomy and async rules (recon-DEV-333-2 section 2: a paused scan is not an exception path, CancellationToken flows, no sync-over-async); Q5 termination ruling.

## Exactly once and rerun semantics (the correction)

5. Exactly once means: within one completed invocation, each canonical candidate (Q4 identity) is yielded once, written once, and passed to the scoped importer once, and the existing per-folder persistence plus the IX_movies_library_path unique index keep exactly one movie and one outbox row per path. It is not a cross-run promise. A rerun is a new explicit invocation under the existing single-folder behaviour: an already-imported candidate faults under rule 3 rather than being skipped, silently absorbed, or counted again. The spec must therefore describe rerun honestly — it restarts; it does not resume a partially completed run, and completing a remainder after a partial failure is not a DEV-333 behaviour. No skip-existing lookup (IMovieRepository has no library-path lookup today, IMovieRepository.cs:9-42), no cross-run lock, and no batch transaction is added by this ticket. Any future skip-or-resume behaviour is a separate decision for a separate ticket, not a silent extension of this one.
   Basis: ticket AC1 speaks of one scan of a root (imports each folder exactly once) and is silent on repeat invocations; existing persistence tests (above) fix the behaviour in roots Patron may not re-read.

## Scope check

6. Fail-fast changes no ticket delivery. Every Scope item and acceptance criterion describes a completable scan (nested exactly-once import, concurrency cap, quota pause/resume, fake-provider dry run), and all remain deliverable verbatim; partial-failure handling is unspecified behaviour settled by Patron ruling, not a dropped or added deliverable. Under specs/PRODUCT.md section 5 the result record is an internal command result, not a public DTO (no route exists, Q3), so no Care item 4 API ruling or owner checkbox arises; the ticket-change and constitution-departure escalations are not triggered.

Q7 may proceed.


---

## Grill exchange Q7/12

Question from Keel:

> [from Keel] DEV-333 Q7/12: Approve the acceptance and implementation-order plan? Recommend deterministic unit tests using existing xUnit/NSubstitute/Shouldly plus MockFileSystem: candidate root/nesting/links/containment/extension parity; option validation; controlled asynchronous barriers proving MaxConcurrency and full-channel backpressure; drain/cancel/fault/scope disposal; zero-root-result and fresh-rerun duplicate fault. Fake TimeProvider and fake gate prove no OMDb transport during exhaustion, Pending/same-attempt until midnight and automatic resume; HTTP429 must retain bounded retries, chain fallback must still work. Use Testcontainers PostgreSQL/RabbitMQ for the actual deferral migration/mapping, atomic claim and sweeper predicates, durable delayed routing, publish-before-ack crash windows and duplicates; no mocked EF driver or EF InMemory. A 500-candidate fixture must actually import all 500, then drive downstream enrichment with a fake provider to completion, assert zero RateLimited, zero duplicate movie/outbox rows, and max concurrency <= configured count. Do not pass AC4 by counting imports alone. Order: amend/land DEV-426 outcome first; focused ADR and glossary/port alignment; discovery/options; scoped importer and bounded handler; deferral schema/domain/settlement; broker/routing/claim/sweeper; composed acceptance; gates/reviews. Gate 1 precedes implementation. Required delivery gates are repo analyzer/complexity/InspectCode, format, property tests, vulnerability scan, full suite and configured mutation pre-PR, thresholds only from harness.yml, skips never pass. No tests/gates are run by this planning ask. Cost: real broker rollover is integration timing; fake-clock contract test stays separate from broker TTL. Please settle concrete evidence obligations and ordering; Gherkin remains opt-in unless you explicitly need it. Q6 saved/read back. Return full ruling by file and ask Keel.

Patron reply:

> [from Patron] DEV-333 Q7/12 ruling: acceptance evidence and binding implementation order approved with refinements, no Gherkin; full cited ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q7-ruling.md - record verbatim in CONCLUSIONS.md and proceed to Q8.

Full ruling (verbatim):

# DEV-333 Q7/12 — Patron ruling: acceptance evidence and implementation order APPROVED with refinements

Verdict: APPROVED as refined below. This is a planning ruling: no tests or gates are run by it, Gate 1 still precedes all implementation, and Gherkin/Reqnroll is not adopted — xUnit evidence is the acceptance record. Record verbatim in specs/DEV-333/CONCLUSIONS.md.

## Evidence obligations (each claim needs the named proof, nothing weaker)

1. Traceability. The spec's test plan must map every ticket AC and ticket Scope item 5 test to named tests (Method_Condition_Expected) with their project (UnitTests vs IntegrationTests). Suggested coverage is not evidence: an AC without a named test against the real seam is unproven.
2. Unit tier (xUnit v3, NSubstitute, Shouldly, MockFileSystem — never EF InMemory, never a mocked EF driver for translation-dependent behaviour): discovery yields the exact candidate set for root-only, nested-candidate-below-candidate, container/empty, root-as-candidate; reparse-point folders are neither yielded nor descended; containment cases from the Q4 clarification (case-distinct sibling rejection under Ordinal, C:\ and / root preservation); extension parity proves discovery and scanner consume the one SupportedVideoExtensions holder; LibraryOptions.MaxConcurrency validation (default 4, zero/negative rejected at startup validation, direct use with no derived arithmetic).
3. Pipeline tier (unit, deterministic barriers, no sleeps-for-sync): an instrumented importer with an interlocked in-flight counter proves observed concurrency never exceeds MaxConcurrency and reaches it under load; a blocked consumer set proves the producer parks on WriteAsync against the full channel and lazy discovery does not enumerate ahead; drain, caller-cancellation, producer-fault, and consumer-fault tests each assert all tasks awaited, writer completed, and every adapter-created scope disposed (counted on the fake scope factory). No per-item Task.Run or unbounded task list is asserted by construction review, not by timing.
4. Settlement tier (unit, FakeTimeProvider, fake quota outcome at the provider seam): quota outcome yields next-UTC-midnight deferral, Status stays Pending, message attempt unchanged, no failure recorded; HTTP 429 still produces RateLimited and terminal Failed after MaxAttempts through the existing policy (EnrichmentRetryPolicy precedent); chained-provider fallback on the quota outcome still completes via the next provider. These are contract tests at Core seams — they do not claim broker behaviour.
5. Integration tier (Testcontainers PostgreSQL and RabbitMQ, real EF, real topology — this is where persistence proof lives per harness.yml mutation exclusions): the deferral migration applies cleanly and existing rows (null deferral) remain sweeper-eligible; claim and stranded predicates refuse before, and admit and clear at, the deferral instant; the delayed re-dispatch message survives and expires into the requested queue using a near-midnight clock so the real TTL expiry is seconds, not 24 hours (the fake-clock unit test proves the date maths, this test proves durable broker expiry — neither substitutes for the other, per Keel's cost note); publish-before-ack crash windows (kill between deferral save and publish, between publish and ack) lose no item and produce no duplicate settlement, with the atomic claim and post-expiry sweeper backstop doing the work; duplicate delivery settles once; fresh-rerun duplicate library path faults on a real database (today's IX_movies_library_path behaviour, already proven at ImportMovieFolderCommandHandlerPersistenceTests.cs:33-83, extended to the bulk handler).
6. Composed AC4, exactly as Keel frames it and not by counting imports: a 500-candidate fixture is imported through the real bulk handler against PostgreSQL (500 movies, 500 outbox rows, zero duplicates, observed concurrency at or under the configured cap), then every staged enrichment is driven to a terminal state against a fake provider, asserting zero RateLimited outcomes. ImportedCount 500 alone proves AC1/AC2 shape, not AC4.

## Order (binding)

7. (a) DEV-426 lands first, including its Gate 1 approval of ADR-0031/constitution 1.7.0 as amended by the Q1 quota-specific-outcome ruling — DEV-333 implements nothing against an unlanded outcome type. (b) DEV-333 Gate 1 (this grill, specs, analyze) completes before any DEV-333 code. (c) Focused DEV-333 ADR plus CONTEXT.md glossary and Principle I/whitelist alignment (Q3/Q4). (d) Options, extension holder extraction, discovery adapter. (e) Scoped importer and bounded handler with its result. (f) Deferral column/domain, claim and sweeper predicates. (g) Quota settlement, routing disposition, delayed topology. (h) Composed acceptance. (i) Delivery gates and reviews. DEV-426 and DEV-333 stay separate PRs in that order; no DEV-333 PR may carry DEV-426 changes.

## Delivery gates (PR stage, reported verbatim, never folded)

8. Exactly the repo scripts: run-roslyn-analyzers.ps1, run-cyclomatic-complexity.ps1, run-jetbrains-inspectcode.ps1, dotnet format verification, dotnet test (full suite), run-property-tests.ps1, run-vulnerable-packages.ps1, and run-mutation.ps1 pre-PR. Thresholds come only from harness.yml (mutation 80 on LamuFlix.Core, complexity 15 implement, analyzers warnings-as-errors): no threshold is lowered, hardcoded elsewhere, or edited in a generated file. Exit 2 is reported as SKIPPED (scope-empty), SKIP (opt-out), or NOT APPLICABLE exactly as the scripts print it and never counted as a pass; an unrunnable gate is Could not run. At least one property test must exercise a DEV-333 invariant (traversal candidate-set identity over generated MockFileSystem trees is the expected one), so the property gate is earned rather than opted out; if that proves impossible, the opt-out is recorded in the task note before the gate runs, never after it fails. Mutation proof is Core-scoped by harness.yml design (Infrastructure and Api exclusions); settlement and handler logic living in Core is therefore mutation-covered, and Infrastructure behaviour is integration-covered — neither tier may borrow the other's evidence.

Q8 may proceed.


---

## Grill exchange Q7/12 — composed acceptance and names clarification

Question from Keel:

> [from Keel] DEV-333 Q7 evidence clarification, same decision: your settlement-unit proof uses fake quota outcome rather than the ticket-mandated fake gate. Recommend an additional composed AC3 test after DEV-426 lands that uses its real OMDb quota-handler/provider mapping, fake IOmdbRequestGate, recording fake transport and FakeTimeProvider: deny -> zero transport, persist Pending deferral with same attempt; advance to UTC midnight and reopen -> successful claim/re-dispatch/lookup without user request or pre-acquisition; no import-side gate call. Separate this from the real-broker TTL test. For AC4 require all 500 fake-provider enrichments succeed, not merely reach any terminal state with zero RateLimited. Cost: one composed test to prevent a fake-outcome unit test from hiding broken transport-to-settlement integration. Please confirm these refinements and provide exact quota-outcome, deferral property/column and routing disposition names for the dependency contract if settled, otherwise state that names are Quill plan detail and preserve semantic obligations. Q7 original saved/read back. Return file ruling and ask Keel.

Patron reply:

> [from Patron] DEV-333 Q7 clarification: confirmed, composed real-gate AC3 added, AC4 tightened to all-success, and the cross-lane contract names are settled in the ruling; full cited ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q7-clarification-ruling.md - record verbatim in CONCLUSIONS.md and proceed to Q8.

Full ruling (verbatim):

# DEV-333 Q7 clarification — Patron ruling: CONFIRMED, names settled

Verdict: Both refinements are adopted and the dependency-contract names below are settled here, not left to Quill, because DEV-426 and DEV-333 lanes must agree on them across separate PRs (Q7 order rule 7). Internal mechanics below these names (TTL wiring, metric tagging) remain plan detail. Record verbatim in specs/DEV-333/CONCLUSIONS.md with the original Q7 ruling.

## Composed AC3 (in addition to, never replaced by, the Q7 unit settlement proof)

1. After DEV-426 lands, one composed test drives the real OMDb quota path end to end at the Core/Infrastructure seam: real MetadataQuotaHandler and OmdbMetadataProvider outcome mapping from DEV-426, a fake IOmdbRequestGate, a recording fake HTTP transport, and FakeTimeProvider. Deny phase: zero recorded transport calls, movie persisted Pending with the deferral instant at the next UTC midnight, message attempt unchanged, and no import-side gate call anywhere (the scanner and ImportMovieFolderCommandHandler never see the gate, Q1). Reopen phase: advance FakeTimeProvider to the UTC midnight, gate now acquires, and the item is claimed, re-dispatched, and looked up successfully with no user re-request and no pre-acquisition. This test is about transport-to-settlement integration and stays separate from the Q7 real-broker TTL test, which alone proves durable queue expiry.
   Basis: ticket Scope item 5 (fake gate pause/resume test) and AC3; Q1 resumed ruling (fake gate belongs to the DEV-426 transport seam, not scanner code); Q2 ruling (settlement and sweeper/claim gates).

## Composed AC4 tightened

2. All 500 fake-provider enrichments must succeed: the fake provider returns Found for every candidate, every movie ends Enriched, and the assertions are zero RateLimited, zero Failed, zero NotFound, 500 movies, 500 outbox rows, zero duplicates. Reaching any terminal state is not success.
   Basis: ticket AC4 (500-movie dry run against a fake provider with no RateLimited result); Q7 rule 6 as tightened here.

## Settled dependency-contract names

3. These exact names are the cross-lane contract; Quill's plan uses them verbatim and may not rename them:
   - Provider outcome category: EnrichmentFailureCategory.QuotaExhausted, code quota_exhausted (existing closed-category style, EnrichmentFailureCategory.cs:11-22). OmdbMetadataProvider maps MetadataQuotaExhaustedException to MetadataLookupResult.Failed(QuotaExhausted), never to RateLimited. The category is an outcome discriminator only: it is never persisted as Movie.LastFailureCategory and never enters EnrichmentRetryPolicy (Q2); provider-429 keeps EnrichmentFailureCategory.RateLimited unchanged.
   - Settlement outcome: ProcessEnrichmentOutcome.QuotaDeferred(DateTimeOffset DeferredUntil), a distinct outcome subtype beside Completed and Failed (ProcessEnrichmentOutcome.cs:5-17) — not a Failed outcome, since no failure decision (retry, retry-delayed, dead-letter; EnrichmentFailureAction.cs:7-9) describes it.
   - Deferral state: Movie.EnrichmentDeferredUntil (DateTimeOffset?), database column enrichment_deferred_until, nullable (Q2 Care item 3 ruling).
   - Routing: EnrichmentRouting disposition action quota-deferred; RabbitMqTopology.QuotaDeferredQueue = enrichment.quota-deferred and RabbitMqTopology.QuotaDeferredRoutingKey = quota-deferred, dead-lettering into the existing requested routing key on expiry (topology style at RabbitMqTopology.cs:17-22). The consumer publishes EnrichmentRequested(MovieId, same Attempt) there and then acks (Q2 publish-before-ack).
   Semantic obligations are unchanged by naming: same attempt, Pending, next-UTC-midnight from TimeProvider, claim and sweeper gates read EnrichmentDeferredUntil. What remains Quill plan detail: publisher TTL mechanics, topology declaration arguments, metric/log tags for the quota outcome, and test fixture construction.

Q8 may proceed.


---

## Grill exchange Q8/12 — closure

Question from Keel:

> [from Keel] DEV-333 Q8/12: Confirm the closing loop terms and shared understanding so I can close brief.md? Recommend Critical/High/Medium findings block code-review/ship-review; Low findings require explicit fix/follow-up/reject disposition and cannot hide a failed gate or unmet AC. All Sentry/Ledger/Compass reports mandatory for size:M adjudication. Two rounds per review loop (plan challenge separately, delivery review/remediation separately), no silent reset or extra immutable review; if blockers remain at cap report blocked for a new decision. Grill cap remains 12; Q1,Q4,Q7 clarifications count within their original decisions, eight distinct decisions so far. Frozen scope: recursive lazy candidate discovery, configured fixed bounded import pipeline, unchanged single-folder import/outbox reuse, per-folder scopes, one-shot internal command/result, quota-specific durable Pending-until-UTC-day settlement with deferral schema/claim/sweeper/broker changes, and the exact acceptance proofs/ADR/glossary/port alignment ruled Q1-Q7; anything else is a follow-up issue, not a finding in this round. No new API, UI, schedule, skip-existing/resume, external dependency, gate counter work or DEV-426 implementation carried in DEV-333 PR. Cost: bounded review can end blocked, never automatic clearance. Please explicitly confirm no outstanding DEV-333 owner question and authorize grill closure for Quill drafting, while preserving DEV-426 amended Gate 1 and landing dependency, DEV-333 later Gate 1, analysis and plan challenge as distinct unpassed phases. Q7 clarification appended/read back. Return full cited ruling by file and ask Keel.

Patron reply:

> [from Patron] DEV-333 Q8/12 ruling: closing loop terms confirmed, no outstanding DEV-333 owner question, grill closure authorized for brief.md and Quill drafting only; full cited ruling at C:\Users\lamun\AppData\Local\Temp\patron-DEV-333-Q8-ruling.md - record verbatim in CONCLUSIONS.md and close the grill.

Full ruling (verbatim):

# DEV-333 Q8/12 — Patron ruling: closing loop terms CONFIRMED, grill closure authorized

Verdict: CONFIRMED. The grill is closed on eight distinct decisions within the 12-question cap, Keel is authorized to close brief.md from CONCLUSIONS.md and hand drafting to Quill, and there is no outstanding DEV-333 owner question. Record verbatim in specs/DEV-333/CONCLUSIONS.md.

## Closing loop terms

1. Findings gate delivery, by severity. Critical, High, and Medium findings block code-review and ship-review clearance. A Low finding never blocks by itself, but it cannot be silently dropped: each Low gets an explicit disposition recorded on the PR — fixed in the round, rejected with its reason, or carried forward; a carried-forward Low becomes a follow-up ticket only when it meets the charter criteria (Critical/High, broken behaviour, or user-requested — via Patron decision and Rigger, charter YouTrack-records rule), otherwise it is one line, noted, no ticket, in CONCLUSIONS.md. No disposition of any severity may hide a failed or unrunnable gate, a SKIPPED/SKIP/Could-not-run gate result, or an unmet acceptance criterion: those are never clearance, whatever the findings say (Q7 rule 8).
   Basis: task-pipeline size:M row (Stage 5: pre-pass plus three axes always; Stage 6 remediation); lamuflix-team-charter (review findings are posted as PR comments, fixes reply and resolve threads; follow-up ticket criteria).
2. All three challenger reports are mandatory for this size:M ticket — Sentry (Risk), Ledger (Standards), Compass (Spec) — at plan challenge and at delivery review as the pipeline stages them. A missing report is a missing phase, not a pass; Keel adjudicates, Patron rules only what is escalated under the charter.
   Basis: task-pipeline Stage 2 and Stage 5 (M: challengers on plan.md; pre-pass + 3 axes in parallel, always).
3. Two rounds per loop, no resets. Plan challenge is one loop (2 rounds maximum) and delivery review/remediation is a separate loop (2 rounds maximum, remediation caps apply inside it); rounds never reset silently, no third or immutable-extra review is invented, and findings beyond a round go to user review on the PR as the pipeline directs. If blockers remain when a loop's cap is reached, the loop reports blocked for a new decision — it does not self-extend and nothing is auto-cleared.
   Basis: task-pipeline Standing Constraints (Review cap: 2 rounds maximum; Remediation cap) and Stage 6 (Cog, 2 rounds).

## Count, scope, and closure authorization

4. Question count: Q1 through Q8 are the eight distinct decisions. The Q1 resumed exchange and the Q4 and Q7 clarifications ride inside their original decisions and consume no additional questions. Four questions of the cap remain unspent and lapse with closure; any genuinely new decision after closure is a new ask, not a resumed grill question, and from a hypothetical 13th question the charter's [assumed] rule would have applied.
   Basis: task-pipeline Grill cap (at most 12; Patron grill into brief.md).
5. Frozen scope is exactly the Q1-Q7 set: recursive lazy candidate discovery with the Q4 candidate/traversal/identity rules; the configured fixed bounded import pipeline (Q5); unchanged single-folder import and outbox reuse with per-folder scopes (Q3); the one-shot internal ImportLibraryCommand/ImportLibraryResult with Q6 success/failure semantics; the quota-specific durable Pending-until-next-UTC-day settlement with deferral column, claim and sweeper predicates, and delayed broker routing under the Q7 settled names (QuotaExhausted, QuotaDeferred, EnrichmentDeferredUntil/enrichment_deferred_until, quota-deferred); and the Q7 acceptance proofs, focused ADR, glossary term, and Principle I/whitelist alignment. Anything outside that set — including any new public API or route, UI, recurring schedule, startup scan, skip-existing or resume-a-partial-run behaviour, new external dependency, gate-counter work, and all DEV-426 implementation — is not a finding to fix inside this ticket: it is a follow-up under rule 1's criteria or it does not ship.
6. No outstanding DEV-333 owner question exists, and none is to be planted in the DEV-333 spec PR as a checkbox. Explicitly preserved as distinct, unpassed phases, none of which this closure touches: (a) DEV-426's own amended Gate 1 owner approval (ADR-0031 and constitution 1.7.0 including the Q1 quota-outcome amendment) and DEV-426 landing before DEV-333 implementation (Q7 order); (b) DEV-333's own later Gate 1, which follows Quill drafting, a clean /speckit-analyze, and adjudicated plan challenge, and is marked gate1: provisional only then; (c) implementation, delivery review, gates, and ship in the Build workspace. Grill closure authorizes exactly one thing: Keel closes brief.md carrying the closing bar, this frozen scope, the round caps, and every Q1-Q8 ruling, and hands it to Quill for drafting. It authorizes no code, no tests, no analyze pass claim, and no gate claim.
   Basis: specs/PRODUCT.md section 5 (only a ticket-text change or a judged constitution departure escalates; neither exists in Q1-Q8), lamuflix-team-charter section 2.3 (same two escalations; a Patron answer never closes an owner checkbox — and here there is no DEV-333 checkbox to close), task-pipeline Phase 2 (Keel closes the grill by writing brief.md; Quill drafts; Keel analyzes; Gate 1 is the merged spec PR).

The grill for DEV-333 is closed.



---

# DEV-333 Phase 2 step 6 round 1 — Patron plan-detail rulings

Verdict: all eight items settled below. No Gate 1, freeze, or code approval is sought or given. Keel records this ruling verbatim in `specs/DEV-333/CONCLUSIONS.md` (append-only) and the settled points in `specs/DEV-333/brief.md` before Quill's correction round. Round cap remains 2; this is round 1.

Basis read: `specs/DEV-333/PLAN_CHALLENGE.md` (28 dispositions), `specs/DEV-333/brief.md` Q1–Q8, `specs/DEV-333/CONCLUSIONS.md` Q1 (initial + resumed), Q2, Q7 (+ clarification), constitution v1.6.0 Principle IV (.specify/memory/constitution.md:246–271), Principle V (:273–276), Enrichment Reliability Rules (:523–539), and canvas note `recon-DEV-333-2-2` follow-up lines 55–94.

## 1. Ledger H1 — constitution IV / Reliability reconciliation: compliant clarification, with named alignment tasks; no departure, no new owner checkbox

Ruling: **compliant clarification, not a constitution departure.** The Q2 mechanism operates inside every operative clause it touches; what is stale is the constitution's inventories, and updating an inventory through the alignment process already ruled for Principle I in Q3 is clarification, not departure. No `blocked: structural` is issued and no DEV-333 owner checkbox is created. This expressly confirms, and does not silently extend, the Q1-resumed/Q2 no-departure verdicts (CONCLUSIONS.md:81, :100–106).

Clause-by-clause basis:

- **Claim (IV :255–257; Reliability :527–528).** `TryClaimForEnrichmentAsync` remains a single atomic `UPDATE … WHERE Status = Pending`; Q2 adds a deferral predicate to that WHERE and clears `EnrichmentDeferredUntil` in the same update (CONCLUSIONS.md:109). Exactly-one-winner and claim-before-provider are unchanged. A predicate refinement of the ruled claim is not a new claim mechanism.
- **Sweeper (IV :264–265; Reliability :534–535).** The sweeper still periodically re-enqueues `Pending` rows with null/stale last attempt. Excluding future deferrals and including expired ones (CONCLUSIONS.md:109, :114) is a temporal eligibility refinement of that same duty; post-expiry lost-message recovery retains attempt-1 semantics only there. No second pipeline is forked (Reliability :538–539 untouched).
- **Retry (IV :262–263; Reliability :532–533).** The category-driven retry rule governs failures. `ProcessEnrichmentOutcome.QuotaDeferred` is by settled definition not a failure outcome (CONCLUSIONS.md:394; brief.md:108): it never enters `EnrichmentRetryPolicy`, never advances the attempt, never `MarkFailed`s. A non-failure settlement outside the retry rule needs no retry exception, and the durable TTL re-dispatch is topology-owned, not an ad-hoc application retry loop.
- **Mutator / Movie state (IV :248–250).** Staying `Pending` is not a status transition, so no listed transition method is contradicted. The one new state write follows IV's own pattern of intent-revealing methods: settlement calls a new `Movie.DeferEnrichmentUntil(DateTimeOffset deferredUntil)` mutator that sets `EnrichmentDeferredUntil` and changes no status. There is no public clearer; clearing happens only inside the atomic claim update. Adding this method is an inventory extension of the same kind as the Principle I port-list addition ruled "no constitution departure or owner checkbox" in Q3 (brief.md:53).
- **`QuotaExhausted` category (IV :258–261).** This expansion of the closed category list is **DEV-426's**, already carried by DEV-426's constitution 1.7.0 amendment and its own owner Gate 1 checkbox (CONCLUSIONS.md:76, :353). DEV-333 neither duplicates that amendment nor closes that checkbox; a Patron answer never closes an owner checkbox.

Exact alignment tasks (Quill adds these to `plan.md` Constitution Tracking / Complexity Tracking at :70, :179 and to T004; nothing is predeclared compliant in plan text before these tasks exist):

1. Constitution text alignment (DEV-333 delivery, same ADR/alignment process as the Principle I list update in Q3): Principle IV gains (a) `DeferEnrichmentUntil(DateTimeOffset)` in the Movie mutator inventory with "sets `EnrichmentDeferredUntil`, no status change"; (b) the claim bullet's deferral predicate and atomic clear; (c) the sweeper bullet's future-deferral exclusion / post-expiry eligibility.
2. Constitution text alignment: Enrichment Reliability Rules gain one quota-deferral paragraph — claim-before-provider unchanged; `QuotaDeferred` is settlement, not retry (same attempt, no `EnrichmentRetryPolicy`, no `MarkFailed`); re-dispatch is the durable quota-deferred TTL queue dead-lettering into requested; sweeper is post-expiry backstop only.
3. ADR-0032 / Ledger M1 linkage: T002/T004 must name the Phase B ADR Proposed→Accepted status transition on applicable approval evidence (PLAN_CHALLENGE.md:32); the constitution alignment in tasks 1–2 lands with that approval evidence, never by drafting alone.
4. Pickup verification (T001 STOP checklist): confirm DEV-426 has landed with `QuotaExhausted` named in its constitution amendment; if DEV-426's owner rejects its amended contract at Gate 1, DEV-333 Scope 3 returns as `blocked: structural` per Q1 resumed (CONCLUSIONS.md:76).

Owner checkbox: **none new.** The only owner checkbox in this chain remains DEV-426's pre-existing Gate 1 checkbox, unchanged and unclosed by this ruling.

## 2. Ledger M5 — payload-free `ImportLibraryCommand` validation: no validator registered; Principle V satisfied by construction, not by decorator tolerance

Ruling: **register no `IValidator<ImportLibraryCommand>`.** `ImportLibraryCommandHandler` is registered only via `AddHandler<ImportLibraryCommandHandler, ImportLibraryCommand, ImportLibraryResult>` in `Api/HandlerRegistration.cs`. No validator file is created, and no empty `AbstractValidator<ImportLibraryCommand>` stub may be added to fake compliance.

Basis (this is a principled reading, not reliance on the decorator tolerating an empty set):

- Principle V (.specify/memory/constitution.md:275–276) obliges validation of commands, queries, and request contracts **through the validation decorator**. Recon proves `AddHandler` always composes `ValidationDecorator` for every handler (`ServiceCollectionExtensions.cs:12–66`; `recon-DEV-333-2-2`:75, :79), so the command does pass through the mandated decorator; with a sealed payload-free record there is no input for a rule to bind to. The obligation attaches to inputs, and this command has none.
- Its actual inputs arrive pre-validated elsewhere: RootPath and `MaxConcurrency` come from `LibraryOptions`, startup-validated with `[Range(1, int.MaxValue)]` under Q5 (brief.md:73). Re-validating an empty payload per invocation would validate nothing.
- Settled repository convention is identical: payload-free `SweepStrandedMoviesCommand` and `SweepMissingMovieFeaturesCommand` (and payload-free queries `GetGenresQuery`/`GetPeopleQuery`) run with no validator (`recon-DEV-333-2-2`:83). A DEV-333-only empty validator would diverge from that convention and add a vacuous artifact.
- Plan consequence: `plan.md:58, :175` and T014 must record this paragraph as the Principle V disposition for the payload-free command (decorator always in pipeline; no inputs; options startup-validated; precedent cited). If a future payload field is ever added to the command, a real validator becomes mandatory in that change.

## 3. Sentry M1 — traversal mechanics: explicit stack of lazy enumerators; no recursion, no depth cap, no materialization

Ruling: adopt Keel's recommendation. `FileSystemLibraryFolderDiscovery` walks with an **explicit stack of lazy enumerators** (push the root enumerator; advance the top; on a qualifying/traversable child directory push its enumerator; dispose and pop on exhaustion), depth-only memory per Q5 (brief.md:75). Call-stack recursion over directory depth is forbidden. No depth cap is added (a cap would change the candidate set ruled in Q4), and no per-directory file/child list is materialized; candidate file inspection still stops at the first supported video (brief.md:75).

Proof required in T007/T012: a deterministic deep-tree test (generated nested candidate chain at a depth that would make recursion a stack risk — state the depth in the test name/data, e.g. 1,000 levels) asserting the exact candidate set, once each, and normal completion. This proves the iterative mechanism by construction and result, not by timing.

## 4. Sentry M2B — non-positive TTL at delayed publish: clamp to zero, publish anyway, same durable path

Ruling: adopt Keel's recommendation exactly. At publish, compute `remaining = EnrichmentDeferredUntil − TimeProvider.GetUtcNow()` from the injected `TimeProvider` (Sentry M4 contract, PLAN_CHALLENGE.md:19) and set the broker expiration to **`max(0, remaining.TotalMilliseconds)`** (whole milliseconds). Publish in all cases: a zero/overdue expiration makes the message immediately eligible for dead-letter dispatch into requested on the durable quota-deferred queue. Do **not** skip the publish, do **not** recompute a new deferral or roll to the next UTC day, and do **not** change the primary path — `Pending` status, same attempt, save-deferral → publish → ack ordering (CONCLUSIONS.md:104–112) are all untouched. Broker expiration remains a relative TTL; no broker wall-clock semantics are invented (PLAN_CHALLENGE.md:17).

Proof: unit test with FakeTimeProvider at/after the deferral instant asserting expiration 0, one publish, same attempt; the existing near-midnight real-broker TTL proof (brief.md:97) remains the durable-expiry evidence.

## 5. Sentry M3 / Compass M3 — supported-host lexical comparator: host-selected comparison, pure internal helper, both-mode deterministic proofs

Ruling: adopt Keel's recommendation. Boundary comparison is purely lexical over canonical root-relative paths: **Windows hosts use `OrdinalIgnoreCase`; all other supported hosts use `Ordinal`**, selected once from the host platform (`RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`). No volume/filesystem capability probing, no case-alias deduplication, no case-folding of identity (Q4 clarification, brief.md:61, stands verbatim).

Mechanism: an `internal` pure containment helper in Infrastructure/FileSystem (alongside the discovery adapter) with signature shaped as `IsContained(string root, string candidate, StringComparison comparison)` — root-equality or root-plus-directory-boundary-prefix, handling filesystem-root forms (`/`, `C:\`) per brief.md:61. Production passes the host-selected comparison; unit tests pass each comparison explicitly, giving deterministic both-mode proofs on any runner host (case-distinct sibling rejected under `Ordinal`, admitted-spelling behaviour under `OrdinalIgnoreCase`, root-only forms preserved). Test access uses the existing `InternalsVisibleTo` grant from `LamuFlix.Infrastructure` to `LamuFlix.UnitTests`/`LamuFlix.IntegrationTests` (`recon-DEV-333-2-2`:71) — no new grant, no Core internals, no new layer. T007 records both the helper proofs and the discovery-flow behaviour.

## 6. Compass M2 — linked-root/linked-child test mechanism on MockFileSystem: root resolution only; never follow linked children

Ruling: settle on the installed double's proven semantics (`recon-DEV-333-2-2`:63–67). MockFileSystem 22.3.0 supports `Directory.CreateSymbolicLink`, `LinkTarget`, and `ReparsePoint`, but enumeration under a link returns empty — therefore:

- **Linked child (unit, MockFileSystem):** seed a link below the root (`CreateSymbolicLink` or `MockFileData.LinkTarget`); assert it is neither yielded as a candidate nor descended, detected via `ReparsePoint`/`LinkTarget`. Never resolve or follow a linked child's target in test or production code — content behind a child link is out of scope by Q4 (brief.md:59), so the double's non-traversal is aligned, not a gap.
- **Linked root (unit, root resolution only):** the proof asserts only that root handling resolves a linked configured root to its final target (`ResolveLinkTarget(..., returnFinalTarget: true)`) and binds containment/canonical `LibraryPath` values to that resolved root. No assertion enumerates target contents through the Mock link, because the double cannot do so; content discovery below a real linked root is covered, if claimed at all, by the real-filesystem integration tier, not by Mock. Production likewise resolves the configured root link only, and never follows any link below it.
- No new test port, production port, package, or dependency is authorized (PLAN_CHALLENGE.md:47). T007 is rewritten to state this mechanism and its limit explicitly instead of naming linked-root content proofs on MockFileSystem.

## 7. Ledger M2 — minimal quota-deferral glossary: DEV-333 records three terms; DEV-426 keeps `QuotaExhausted` and chain eligibility

Ruling: Quill adds exactly these terms to `CONTEXT.md` in the DEV-333 delivery, no others:

> **QuotaDeferred**
> The settlement outcome for a movie whose metadata lookup stopped on provider quota exhaustion: it stays Pending, keeps its attempt, and is re-dispatched at `EnrichmentDeferredUntil`. It is not a failure and never enters retry.
> _Avoid_: quota failure, quota retry, paused import

> **EnrichmentDeferredUntil**
> The persisted next-UTC-midnight instant (from the injected TimeProvider) before which a QuotaDeferred movie must not be claimed, swept, or dispatched.
> _Avoid_: retry-after, next retry time, pause timestamp

> **quota-deferred** (routing)
> The routing action and durable queue (`enrichment.quota-deferred`) carrying the same-attempt `EnrichmentRequested` until `EnrichmentDeferredUntil`, when it dead-letters into requested.
> _Avoid_: retry queue, delayed retry

Ownership boundary: **DEV-426 owns `QuotaExhausted` (`quota_exhausted`) and chain-fallback eligibility vocabulary** (brief.md:35–37, :107); DEV-333 records only `QuotaDeferred`, `EnrichmentDeferredUntil`, and the routing term above, cross-referencing DEV-426's terms without redefining them. At Phase B pickup, verify DEV-426's landed glossary wording for `QuotaExhausted` rather than duplicating it (PLAN_CHALLENGE.md:33).

## 8. Confirmation of rejected/carried dispositions; Sentry H1 follow-up; routing Disposition extension

**Confirmed as Keel adjudicated them** in PLAN_CHALLENGE.md — no re-opening:

- Rejected: Sentry H1 (in-scope blocker rejected, :15), Sentry M2 Part A (:17), Sentry L1 mechanism (:20), Sentry L3 (:22), Sentry L6 (:25), Ledger L1 WireMock substitution (:37), Compass M-1 ownership allegation (:46), Compass L-4 simultaneous-fault premise (:52).
- Carried with their stated costs/limits: Sentry L1 residual duplicate-dispatch risk under the mandatory single-claim proof (:20), Sentry L2 publish/ack cost (:21), Sentry L4 no race-free boundary promise (:23), Sentry L5 T024 duration is not a defect (:24), Ledger L1 convention concern recorded under the ruled fake-transport proof (:37).

**Sentry H1 follow-up: `noted, no ticket`.** The rerun cost is the Q6/Q8-ruled behaviour itself — a fresh rerun faults on the first already-committed candidate, earlier commits are retained, and there is no skip/resume or completion guarantee (brief.md:85; CONCLUSIONS Q6). That is settled product behaviour, not broken behaviour, and it meets none of the charter follow-up criteria as a defect (Critical/High broken behaviour or user request); severity alone does not convert a deliberately ruled limitation into a ticket. Recorded residual for any future owner decision: a rerun after partial failure requires the operator to remove/reset the already-imported library paths before re-invoking; if the owner ever wants skip-existing or resume, that is a new ticket changing Q6, not a DEV-333 finding. No Rigger ask is made.

**Routing Disposition — extend narrowly, no enum, no rewrite.** Recon confirms `EnrichmentRouting.Disposition` is a `public sealed record Disposition(string Action, string RoutingKey, int? NextAttempt)` (`EnrichmentRouting.cs:35`; `recon-DEV-333-2-2`:87–92). Rule:

- Add the settled action constant `QuotaDeferred = "quota-deferred"` beside Ack/Requeue/DeadLetter (:9–14), and add one optional positional member `DateTimeOffset? DeferredUntil = null` to the record — existing constructions and the `Failed`/`From` arms are source-unchanged.
- `Decide(ProcessEnrichmentOutcome)` gains exactly one arm: `QuotaDeferred(DateTimeOffset deferredUntil)` → `new Disposition(QuotaDeferred, RabbitMqTopology.QuotaDeferredRoutingKey, null, deferredUntil)`. `NextAttempt` stays null for this arm; the consumer's quota-deferred publish uses the delivered message's own `Attempt` unchanged (same-attempt rule, CONCLUSIONS.md:107, :396), and computes the §4 expiration from `DeferredUntil`. Every other outcome keeps falling through exactly as today.
- Constitution Coding Conventions' no-new-plain-enum rule (PLAN_CHALLENGE.md:40) is satisfied: the string-action record representation is preserved and extended, not replaced.

## Routing

Keel: record this file verbatim in CONCLUSIONS.md, fold §§1–8 into brief.md/plan.md/tasks.md corrections for Quill round 1, and keep PLAN_CHALLENGE.md's numbered correction list (:62–68) mapped to these rulings. No Gate 1 marking, no freeze, no code. If any item above cannot be recorded without changing ticket text, return `blocked: structural — <question>` to Patron instead of assuming.

