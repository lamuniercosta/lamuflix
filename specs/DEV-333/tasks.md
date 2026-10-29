# Tasks: DEV-333 Recursive library import with bounded pipeline and quota-day deferral

**Input**: Design documents from `specs/DEV-333/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `DRAFTING_RECEIPT.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why). No `design.md` (no ui tag).

**Tests**: Included. Brief Q7 / Q7 clarification require named `Method_Condition_Expected` proofs for every AC and Scope item 5 obligation. Author tests in the same phase as the surfaces they lock (TDD). Compile-red is permitted while types are introduced inside a phase; the phase is not done until its named targeted proofs are green. Do not start Phase 2 production until Phase 1 pickup clears landed amended DEV-426 and drift, the spec PR has merged, and Gate 1 is established.

**Organization**: Phases follow brief Q7 binding order and `spec.md` user stories US1–US4.

**Domain vocabulary**: Candidate Movie Folder — exact brief Q4 text lands in `CONTEXT.md` with the Phase A spec (not deferred to delivery); mirror in `spec.md`. Delivery glossary (Plan-challenge round 1 §7): exact `QuotaDeferred`, `EnrichmentDeferredUntil`, and `quota-deferred` (routing) entries in `CONTEXT.md`. Cross-lane names verbatim; DEV-426 owns `QuotaExhausted` / chain eligibility (verify at T001 — do not redefine).

**Exact file boundary**: Paths in `plan.md` / brief Planned file responsibilities. Outside delivery: public routes/DTOs, UI, scheduler/BackgroundService for scan, DEV-426 gate/counter implementation, `Directory.Packages.props`, `Features:LocalPlay` / secrets / `Process.Start`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 discovery/import; US2 bounded pipeline; US3 quota deferral; US4 composed dry run.
- Exact file paths are in each description.

## Phase 1: Setup — Phase B pickup STOP (landed amended DEV-426 + drift)

**Purpose**: Confirm landed DEV-426 quota-specific outcome and file-set drift before production work.

- [ ] T001 Wisp Phase B pickup/drift recon against picked HEAD: verify landed DEV-426 exposes `EnrichmentFailureCategory.QuotaExhausted` / provider map from `MetadataQuotaExhaustedException` to `Failed(QuotaExhausted)` (not RateLimited); confirm landed constitution amendment names `QuotaExhausted` in the closed category inventory; confirm landed glossary wording for `QuotaExhausted` without DEV-333 redefinition; confirm chain-fallback eligibility for the quota outcome remains landed (non-final quota may fall through to next provider); ADR-0031/constitution Gate 1 approved; and `IOmdbRequestGate.TryAcquireAsync` available. Reconcile brief Q1/Q7 names against main; confirm plan.md file paths and absence/presence of net-new discovery/import/deferral files; note any main drift that would change import/enrichment/RabbitMQ surfaces. Keel supplies pickup/drift clearance. **STOP** for a cited decision on missing prerequisite landing, owner-rejected DEV-426 amendment (`blocked: structural`), or any contract/file-boundary mismatch. DEV-426 retains chain implementation — DEV-333 does not edit `ChainedMetadataProvider` as a forced ownership rewrite. No production edits in this task.

**Checkpoint**: Prerequisites match the brief; Phase B build intake is allowed.

---

## Phase 2: Foundational — ADR reference, glossary confirm, whitelist, options, shared extensions (Q7 steps c–d)

**Purpose**: Verify/reference Phase A Keel ADR; confirm CONTEXT.md glossary already landed with the spec; Principle I/whitelist + Governance obligations; MaxConcurrency; SupportedVideoExtensions extraction. Blocks story implementation that depends on ports/options.

**⚠️ CRITICAL**: User-story production beyond options/extension extraction waits on this foundation and on Gate 1 / T001. Keel authors the focused ADR in Phase A at `docs/adr/0032-recursive-import-ports-and-scope-seam.md` (Status Proposed; brief ADR evidence closure; `recon-DEV-333-2-2`). Quill does not draft ADR bodies. Rebase/pickup rechecks numbering and dependency drift.

### Documentation / architecture

- [ ] T002 Verify and reference the Phase A Keel-authored focused ADR for `ILibraryFolderDiscovery` and `IMovieFolderImporter` (per-folder scope seam) at exact path `docs/adr/0032-recursive-import-ports-and-scope-seam.md`. Cross-reference Q1/Q2 no scanner acquisition and the scope seam. First authorship is Keel's Phase A obligation; do not defer first drafting to delivery. Status Proposed claims no Gate 1 or constitution approval. Name the Phase B **approval-evidence-gated** Proposed→Accepted status transition (do not mark Accepted by drafting alone; transition only after applicable approval evidence — Ledger M1; brief Plan-challenge round 1 §1). At Phase B pickup, confirm the file still exists after rebase and still covers both ports + scope seam.
- [ ] T003 [P] Confirm exact Candidate Movie Folder glossary term remains in `CONTEXT.md` (brief Q4 text landed with the Phase A spec). If missing at Phase B pickup, restore the exact brief text before story work. At US3 delivery, add the three exact Plan-challenge round 1 §7 glossary entries to `CONTEXT.md` — **QuotaDeferred**, **EnrichmentDeferredUntil**, and **quota-deferred** (routing) — verbatim; do not redefine DEV-426 `QuotaExhausted` (verified at T001).
- [ ] T004 [P] Against the actual Phase B constitution baseline (recon baseline Version 1.6.0; re-read after landed DEV-426), update `.specify/memory/constitution.md` and `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` PortWhitelistPattern as follows — **compliant clarification, no new owner checkbox** (Patron Plan-challenge round 1 §1): (1) Principle I port list for both new ports; (2) Principle IV Movie mutator inventory adds `DeferEnrichmentUntil(DateTimeOffset)` (sets `EnrichmentDeferredUntil`, no status change); claim bullet gains deferral predicate + atomic clear; sweeper bullet gains future-deferral exclusion / post-expiry eligibility; (3) Enrichment Reliability Rules gain one quota-deferral settlement paragraph distinguishing same-attempt durable quota-deferred TTL dispatch from failure retries (claim-before-provider unchanged; `QuotaDeferred` is settlement not retry; sweeper post-expiry backstop only). Include Governance obligations without claiming approval: Sync Impact Report entry, appropriate version bump (MINOR for material Principle I/IV inventory expansion unless baseline already records them), Last Amended date, and `.specify/templates/*` review with evidence of required edits or a no-change conclusion. Reference `docs/adr/0032-recursive-import-ports-and-scope-seam.md` and name the same approval-evidence-gated ADR Proposed→Accepted transition as T002 (constitution alignment lands with that approval evidence, never by drafting alone). Ordinary ADR/constitution alignment; not a threshold change; drafting does not claim Gate 1 or owner approval. DEV-426 alone owns `QuotaExhausted` category constitution alignment and its existing owner checkbox.

### Options and extensions

- [ ] T005 [P] [US2] Add `MaxConcurrency` (default 4, `[Range(1, int.MaxValue)]`) to `src/LamuFlix.Core/Options/LibraryOptions.cs`; extend `tests/LamuFlix.UnitTests/Options/LibraryOptionsTests.cs` and `tests/LamuFlix.UnitTests/Options/OptionsStartupValidationTests.cs` with `LibraryOptions_MaxConcurrency_DefaultIsFour`, `LibraryOptions_MaxConcurrency_ZeroOrNegativeInteger_FailsRangeStartupValidation`, `LibraryOptions_MaxConcurrency_FractionalOrNonNumericConfiguration_FailsBinder` (Method_Condition_Expected). Distinguish binder rejection of fractional/nonnumeric configuration strings from Range/startup-validation rejection of zero/negative integers — same fails-startup result, different failure path; do not present a binder exception as proof that Range handled non-integers (Compass L-3; brief Q5).
- [ ] T006 [P] [US1] Extract `SupportedVideoExtensions` to `src/LamuFlix.Infrastructure/FileSystem/SupportedVideoExtensions.cs`; narrow `src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs` to consume it; keep membership OrdinalIgnoreCase identical. Extend `tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs` proving selection/extension behaviour unchanged (`Scan_SupportedExtensions_UnchangedAfterSharedHolderExtraction` regression suite retained green). Use AutoFixture/Faker.Net and Theory/MemberData for repeated extension-input cases where the matrix is genuinely identical (constitution IX).

**Checkpoint**: Phase A ADR referenced with approval-gated Accepted transition named; Candidate Movie Folder confirmed; IV/Reliability/Governance obligations recorded against Phase B baseline; MaxConcurrency binder-vs-Range proofs distinct; scanner regression green.

---

## Phase 3: User Story 1 — discovery + scoped importer + bounded handler (Q7 steps d–e; US1/US2)

**Goal**: Streaming discovery, scoped per-folder import, ImportLibraryCommand/Result with bounded pipeline; AC1/AC2 shape without quota settlement yet.

**Independent Test**: MockFileSystem candidate set; instrumented concurrency barriers; empty/missing root; real-DB fresh-rerun duplicate fault.

### Tests

- [ ] T007 [P] [US1] Add `tests/LamuFlix.UnitTests/FileSystem/FileSystemLibraryFolderDiscoveryTests.cs` (MockFileSystem 22.3.0; AAA; Theory/MemberData where input matrices match; InternalsVisibleTo for internal helper). Exact-set / boundary matrix preserving original proofs: `DiscoverAsync_RootNestedParentNestedChildContainerEmpty_YieldsExactCandidateSetOnceEach`; `DiscoverAsync_LinkedChildViaReparsePointOrLinkTarget_NeitherYieldedNorDescended` (seed via `CreateSymbolicLink` / `ReparsePoint` / `LinkTarget`; assert no yield and no descent — **never claim Mock traverses linked contents**); `DiscoverAsync_ConfiguredRootReachedThroughLink_ResolvesFinalTargetAndBindsContainmentToResolvedRootOnly` (resolve-only proof: `ResolveLinkTarget(..., returnFinalTarget: true)`; bind containment/canonical identity to resolved root; **no Mock linked-content enumeration assertion**; if production linked-root **content** discovery is claimed, prove it at real-filesystem integration tier separately — required linked-root support preserved); `DiscoverAsync_DescendantCandidate_YieldsCanonicalRootRelativePath`; `DiscoverAsync_RootCandidate_YieldsCanonicalAbsolutePath`; `IsContained_CaseDistinctSibling_RejectedUnderOrdinal_AdmittedSpellingUnderOrdinalIgnoreCase` (pure helper both-comparator proofs independent of runner host); `IsContained_FilesystemRootForms_PreservedForSlashAndDriveRoot`; `DiscoverAsync_HostSelectedComparison_WindowsOrdinalIgnoreCase_ElseOrdinal` (host policy: `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`); `DiscoverAsync_ExtensionParity_MatchesSupportedVideoExtensions`; `DiscoverAsync_Depth1000NestedCandidateChain_YieldsExactCandidateSetOnceEachAndCompletes` (deterministic 1,000-level exact-candidate-set + normal completion — iterative stack proof; no depth cap).
- [ ] T008 [P] [US1] Add property test `tests/LamuFlix.UnitTests/FileSystem/LibraryFolderDiscoveryCandidateSetPropertyTests.cs`: generated-tree candidate-set identity invariant over MockFileSystem (brief Q7 property obligation; AutoFixture/Faker.Net as appropriate).
- [ ] T009 [P] [US2] Add `tests/LamuFlix.UnitTests/Features/Import/ImportLibraryCommandHandlerPipelineTests.cs` using real SUT member `HandleAsync`: `HandleAsync_UnderLoad_ConcurrencyReachesButNeverExceedsMaxConcurrency`; `HandleAsync_FullChannel_ProducerAwaitsWriteAsyncWithoutPrefetchingTree`; `HandleAsync_Drain_AwaitsAllTasksAndDisposesScopes`; `HandleAsync_CallerCancellation_AwaitsAllAndDisposesScopes`; `HandleAsync_ProducerFault_CancelsPeersAwaitsAllDisposesScopes`; `HandleAsync_ConsumerFault_CancelsPeersAwaitsAllDisposesScopes`; `HandleAsync_CallerCancellationWhileWriterBlockedOnFullChannel_AwaitsAllDisposesScopesWithoutAbandonedWriter`; `HandleAsync_ConsumerFaultWhileWriterBlockedOnFullChannel_CompletesWriterAwaitsAllDisposesScopes` (explicit blocked-writer + caller-cancellation / consumer-fault races; **do not** invent a simultaneous discovery-fault-during-WriteAsync test — Q5 one producer awaiting WriteAsync cannot also advance discovery; Compass L-4); `HandleAsync_CallerCancellationRace_PropagatesOperationCanceledExceptionAfterAwaitAllRetainingCommits`; `HandleAsync_FirstGenuineFaultRace_PropagatesOriginalExceptionIdentityAndStackNotPeerCancellationOrAggregate` (concrete named race cases per brief Q6). Deterministic barriers; no sleeps-for-sync; instrumented fake importer/scope factory.
- [ ] T010 [P] [US1] Add `tests/LamuFlix.UnitTests/Features/Import/ImportLibraryCommandHandlerResultTests.cs` (`HandleAsync` SUT): `HandleAsync_EmptyReadableRoot_ReturnsImportedCountZero`; `HandleAsync_MissingOrUnreadableRoot_FailsFast`; `HandleAsync_UnreadableSubtree_FailsFastAfterShutdown`; `HandleAsync_DisappearingCandidate_FailsFastAfterShutdown`; `HandleAsync_InvalidCandidate_FailsFastAfterShutdown`; `HandleAsync_ScannerFault_PropagatesFirstOriginalFaultRetainingEarlierCommits`; `HandleAsync_ImportFault_PropagatesFirstOriginalFaultRetainingEarlierCommits`; `HandleAsync_PersistenceFault_PropagatesFirstOriginalFaultRetainingEarlierCommits`.
- [ ] T011 [P] [US1] Add/extend integration `tests/LamuFlix.IntegrationTests/ImportLibraryCommandHandlerPersistenceTests.cs` (`HandleAsync` SUT): `HandleAsync_NestedCandidates_PersistsDistinctMoviesAndOutboxRowsOnceEach`; `HandleAsync_FreshRerunDuplicateLibraryPath_FaultsOnRealDatabase`.

### Implementation

- [ ] T012 [P] [US1] Add `src/LamuFlix.Core/Ports/ILibraryFolderDiscovery.cs` and `src/LamuFlix.Infrastructure/FileSystem/FileSystemLibraryFolderDiscovery.cs` per brief Q4 and Plan-challenge round 1 §§3,5–6: **explicit stack of lazy enumerators** (push root; advance top; push child enumerator; dispose/pop on exhaustion; dispose stack on cancellation/fault); no call-stack recursion; no depth cap; no materialized per-directory file/child lists; stop file inspection after first supported video; host-selected comparison; **pure internal `IsContained(string root, string candidate, StringComparison comparison)` helper INSIDE this discovery file** (existing Infrastructure InternalsVisibleTo; no new file/port/layer); linked children never followed; configured root may resolve final link target for boundary/canonical identity only — production never follows linked children. Preserve required linked-root support.
- [ ] T013 [P] [US1] Add `src/LamuFlix.Core/Ports/IMovieFolderImporter.cs` and `src/LamuFlix.Infrastructure/Import/ScopedMovieFolderImporter.cs` (`ImportAsync` SUT: CreateAsyncScope per folder; resolve decorated ImportMovieFolder handler; dispose on all exits).
- [ ] T014 [US1] [US2] Add sealed payload-free `ImportLibraryCommand.cs`, `ImportLibraryResult.cs`, `ImportLibraryCommandHandler.cs` under `src/LamuFlix.Core/Features/Import/` with bounded channel lifecycle per brief Q5–Q6. Register **only** via `AddHandler<ImportLibraryCommandHandler, ImportLibraryCommand, ImportLibraryResult>` (ValidationDecorator always composed). **Principle V disposition (Plan-challenge round 1 §2)**: no `IValidator<ImportLibraryCommand>` registered; **no** empty FluentValidation stub/file; inputs are startup-validated `LibraryOptions`; precedent is payload-free sweep commands (`recon-DEV-333-2-2` §2). Future payload field requires a real validator.
- [ ] T015 [US1] Register handler and scoped adapters in `src/LamuFlix.Api/HandlerRegistration.cs` (`AddHandler` + scoped discovery/importer; no validator registration for ImportLibrary). No new public endpoint. Behavioral proofs: DI resolves `ImportLibraryCommand` handler in a fresh caller scope; `ScopedMovieFolderImporter.ImportAsync` drives decorated `ImportMovieFolder` persistence (covered with T011 real-DB nested import/outbox persistence — no absence-of-validator mirror tests).

**Checkpoint**: Discovery/pipeline unit proofs green; persistence/rerun integration green; no public API change.

---

## Phase 4: User Story 3 — deferral schema, settlement, routing (Q7 steps f–g)

**Goal**: EnrichmentDeferredUntil + QuotaDeferred settlement + durable quota-deferred topology; AC3 proofs.

**Independent Test**: Unit settlement (no publish/TTL from Decide); composed fake-gate AC3; real DB predicates; composed consumer FakeTimeProvider at/equal/after-deadline expiration-0 publish fixture; real broker near-midnight TTL; crash windows.

### Tests

- [ ] T016 [P] [US3] Add `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentQuotaDeferredTests.cs` (real SUT members; FakeTimeProvider; UnitTests only — **no** `LamuFlix.Test` fallback): `HandleAsync_QuotaExhausted_LeavesPendingSetsDeferredUntilSameAttemptNoFailure`; `HandleAsync_Http429_RetainsRateLimitedBoundedRetryAndTerminalFailed`. Optional routing-only unit (if present): `Decide_QuotaDeferred_ReturnsQuotaDeferredRoutingKeyNullNextAttemptAndDeadline` — **Disposition fields only**; `EnrichmentRouting.Decide` does not publish or compute TTL, so this test **must not** claim publish/expiration. Always-publish max-zero TTL / expiration-0 publish proof lives in **T019** composed consumer fixture (not here). **Composed real-chain proof** (DEV-426 retains chain implementation; Q7 settlement proof 4; Compass M-1): compose the **real** `ChainedMetadataProvider` with controlled provider seams proving `FetchAsync_NonFinalQuotaThenNextProviderFound_CompletesEnrichmentWithZeroDeferral` and `FetchAsync_ChainFinalQuota_DefersWithQuotaDeferred` — non-final quota + next Found completes with **zero** deferral; only chain-final quota defers. Do not treat a fake final-outcome stub alone as successful-fallback proof. Add constitution IV domain mutator unit proofs **only** in `tests/LamuFlix.UnitTests/Domain/MovieEnrichmentDeferredUntilTests.cs`: `DeferEnrichmentUntil_FromPending_SetsDeadlineLeavesPendingNoFailure`; `DeferEnrichmentUntil_FromNonPending_ThrowsInvalidTransitionLeavingStateUnchanged` (mutator name locked by Plan-challenge round 1 §1). Do not claim existing `MarkFailed_*` / enrichment transition tests cover deferral (`MovieSurvivorTests.MarkFailed_*`, `MovieTests.Enrichment_FromState_MatchesTransitionTable` address other mutators only). Use AutoFixture/Faker.Net and Theory/MemberData for repeated legal/illegal input cases where matrices match.
- [ ] T017 [P] [US3] Add composed `tests/LamuFlix.IntegrationTests/OmdbQuotaDeferralCompositionTests.cs` delivering **ticket Scope item 5** (fake-gate pause/resume) and AC3 outside scanner code (brief resumed Q1; Q7 clarification rule 1; Compass L-1): `Deny_ZeroTransportPendingDeferralSameAttemptNoImportGateCall`; `ReopenAtUtcMidnight_ClaimsRedispatchesAndLooksUpWithoutUserRequest` using real MetadataQuotaHandler + OmdbMetadataProvider mapping, fake `IOmdbRequestGate`, recording fake transport, FakeTimeProvider. Fake-clock date mathematics only — separate from real broker TTL proof in T019.
- [ ] T018 [P] [US3] Extend migration/repository integration tests (new or existing under `tests/LamuFlix.IntegrationTests/`, e.g. `EfMovieRepositoryTests.cs` / dedicated deferral file) with real SUT members: `Migration_AddEnrichmentDeferredUntil_NullRowsRemainSweeperEligible`; `TryClaimForEnrichmentAsync_BeforeDeferral_Refuses`; `TryClaimForEnrichmentAsync_AfterDeferral_ClearsDeadlineAtomicallyWithLastAttemptAt`; `FindStrandedMovieIdsAsync_FutureDeferral_Excluded`; `FindStrandedMovieIdsAsync_ExpiredDeferral_Included`. All deadline comparisons use application `TimeProvider` samples (not DB now()). Real PostgreSQL Testcontainers.
- [ ] T019 [P] [US3] Add `tests/LamuFlix.IntegrationTests/QuotaDeferredBrokerTests.cs` retaining Q2 crash-window obligations and fuller named assertions: `Publish_NearMidnightClock_RealTtlExpiresIntoRequested` (durable real-broker expiry evidence only — do **not** claim FakeTimeProvider advances the broker); `ConsumerSettlementPublish_FakeClockAtEqualOrAfterPersistedDeadline_RecordsExpirationZeroOnePublishOriginalAttemptPublishBeforeAck` (explicit composed **consumer settlement/publish fixture** operation — no public single-member SUT for TTL compute+publish; FakeTimeProvider at/equal/after persisted `EnrichmentDeferredUntil`; drive the actual consumer quota-deferred publish branch; record outgoing expiration **0**, exactly **one** publish with **original Attempt**, and publish-before-ack; always-publish max-zero whole-ms contract — Plan-challenge round 1 §4; **separate** from the near-midnight real-TTL proof above; no new fixture file/production seam); `Crash_BeforePublish_RedeliveryNoopsUntilExpiryThenSweeperOrDelayedPathRecovers`; `Crash_AfterPublishBeforeAck_DuplicateDeliverySettlesOnce`; `ProcessOrConsumerRestart_PersistedDeferralSurvivesWithSameDeadline`; `DelayedMessage_RetainsOriginalAttemptAcrossExpiry`; `EarlyStrayDelivery_BeforeDeferral_NoOpAckZeroProviderCallsUnchangedDeadlineZeroExtraPublish` (claim-before-lookup / no-redeferral; Compass L-2); `SaveBeforePublishBeforeAck_OrderingHoldsAndCancellationDuringLookupPropagates`; `PostExpiryLostMessage_SweeperRecoversWithAttempt1Only`; `DuplicateDelivery_SingleClaimWins`. Real PostgreSQL + RabbitMQ Testcontainers; near-midnight **application** clock so real TTL wait is seconds. Do not substitute FakeTimeProvider advancement for broker expiration. Keep fake-clock composed AC3 proofs in T017; keep fake-clock expiration-zero **consumer publish** edge proof and real-TTL / crash-window / restart / sweeper / ordering proofs here.

### Implementation

- [ ] T020 [US3] Add `Movie.EnrichmentDeferredUntil` and `DeferEnrichmentUntil(DateTimeOffset)` in `src/LamuFlix.Core/Domain/Movie.cs` (Rehydrate/map; Pending-only; no status change; no public clearer); `MovieRecord` + `MovieConfiguration` column `enrichment_deferred_until`; generate EF migration `AddEnrichmentDeferredUntil` (tooling timestamp) updating snapshot. Never hand-edit generated files. Deadline writes use injected application TimeProvider at settlement call sites.
- [ ] T021 [US3] Update `TryClaimForEnrichmentAsync` and `FindStrandedMovieIdsAsync` in `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` per brief Q2 (refuse early using application TimeProvider comparisons; clear on eligible claim; stranded exclude future / include expired). No new IMovieRepository member.
- [ ] T022 [US3] Add `ProcessEnrichmentOutcome.QuotaDeferred(DateTimeOffset DeferredUntil)` in `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentOutcome.cs`; branch settlement in `ProcessEnrichmentCommandHandler.cs` for **chain-final** QuotaExhausted only (Pending, deadline, same attempt, no MarkFailed, no EnrichmentRetryPolicy; claim-before-lookup). Non-final quota remains DEV-426 chain-fallback (proven in T016).
- [ ] T023 [US3] Extend `RabbitMqTopology.cs` with `QuotaDeferredQueue` / `QuotaDeferredRoutingKey` and durable quorum declaration dead-lettering into requested; narrow-extend existing sealed record `EnrichmentRouting.Disposition(string Action, string RoutingKey, int? NextAttempt)` with optional `DateTimeOffset? DeferredUntil = null`, add `QuotaDeferred` action constant, and one `Decide` arm returning quota-deferred routing key, null `NextAttempt`, and deadline (existing failure arms/constructions unchanged; consumer publishes with delivered original `Attempt`; expiration = max(0, whole ms remaining from application TimeProvider) — always publish). Extend `EnrichmentConsumer.cs` save-before-publish-before-ack (CancellationToken.None settlement ordering; cancellation during lookup still propagates; early stray: unchanged deadline + zero extra publish) with existing `traceparent`/`tracestate` propagation on delayed publish (Principle VI). **Reference** existing `TelemetryConstants` keys listed in plan.md only — **not** an unassigned write target; do not add speculative new telemetry names. Do not route quota through the 30s retry queue. No enum/rewrite. Add `tests/LamuFlix.IntegrationTests/QuotaDeferredTelemetryTests.cs`: `DelayedPublish_InjectsTraceparentIntoHeaders` (behavioral delayed trace-header proof only). Land the three exact delivery glossary entries from T003 / Plan-challenge round 1 §7 in `CONTEXT.md` with this settlement delivery.

**Checkpoint**: Unit + composed AC3 + migration/claim/sweeper + real broker TTL proofs green; HTTP429 regression green.

---

## Phase 5: User Story 4 — composed 500-movie dry run (Q7 step h)

**Goal**: AC4 composed acceptance.

**Independent Test**: 500 import + fake-provider enrichment all Enriched.

### Tests

- [ ] T024 [US4] Add `tests/LamuFlix.IntegrationTests/ImportLibraryDryRunTests.cs`: `DryRun_500Candidates_ImportsAndEnrichesAllWithZeroRateLimitedFailedNotFoundOrDuplicates` — real bulk handler + PostgreSQL; bounded concurrency ≤ configured; fake provider returns Found for all; assert 500 movies, 500 outbox rows, all Enriched. Import count alone is insufficient (brief Q7 clarification).

**Checkpoint**: AC4 green.

---

## Phase 6: Polish — preliminary verification, Cog refactor, final gates (Q7 step i)

**Purpose**: Configured backend verification bar per brief Q7–Q8.

- [ ] T025 Run configured backend build/format and full `dotnet test` (unit/integration/architecture). Fix only in-scope failures. Preliminary only — does not satisfy final post-refactor evidence.
- [ ] T026 Cog `/refactor` on the delivery diff only (helpers/early returns/guards; no scope expansion; no threshold changes; no new packages).
- [ ] T027 After T026, on final changed HEAD: build/format and full `dotnet test`; then `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`; `./scripts/run-property-tests.ps1`; `./scripts/run-vulnerable-packages.ps1`; pre-PR `./scripts/run-mutation.ps1` without `--since`. Report generated migration/snapshot exclusions. Record command, head, exit/verdict. Report SKIPPED (scope-empty) / SKIP / N/A / Could not run verbatim — never as PASS. Do not lower `harness.yml` thresholds. No web/OpenAPI gates. Review checkpoint only after this post-refactor verification.

**Checkpoint**: Delivery ready for review loops under brief Q8 (Critical/High/Medium block; Low recorded; three axes required; plan-challenge and delivery review each at most two rounds; at cap report blocked).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No code — blocks all production.
- **Phase 2**: Depends on Phase 1 + Gate 1.
- **Phase 3 (US1/US2)**: Depends on Phase 2.
- **Phase 4 (US3)**: Depends on Phase 3 surfaces that stage enrichment; requires landed DEV-426 types from Phase 1.
- **Phase 5 (US4)**: Depends on Phases 3–4.
- **Phase 6**: Depends on all desired stories complete.

### User Story Dependencies

- **US1**: After Phase 2 — discovery/import.
- **US2**: Same handler as US1 — pipeline tasks co-located in Phase 3.
- **US3**: After US1 import staging exists; independent of UI/API.
- **US4**: After US1–US3.

### Parallel Opportunities

- T003/T004/T005/T006 after T002 references `docs/adr/0032-recursive-import-ports-and-scope-seam.md`.
- T007–T011 tests in parallel before/with T012–T015.
- T016–T019 tests in parallel once T020–T023 types exist (TDD: write failing first).

### Parallel Example: Phase 3 tests

```text
Task: FileSystemLibraryFolderDiscoveryTests candidate/boundary/canonical-identity matrix
Task: LibraryFolderDiscoveryCandidateSetPropertyTests
Task: ImportLibraryCommandHandlerPipelineTests barrier + concrete cancel/fault race matrix
Task: ImportLibraryCommandHandlerResultTests empty/missing/unreadable/invalid/first-fault matrix
Task: ImportLibraryCommandHandlerPersistenceTests nested + rerun
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Phase 1 pickup STOP
2. Phase 2 foundation
3. Phase 3 discovery + bounded import
4. STOP and validate AC1/AC2 proofs

### Incremental Delivery

1. Add US3 deferral → AC3
2. Add US4 dry run → AC4
3. Polish + gates + reviews

### Notes

- [P] = different files, no unfinished dependency
- Quill may not rename Q7 cross-lane names
- Fake-clock date maths do not substitute for real broker TTL or real DB predicates
- anything else is a follow-up issue, not a finding in this round
