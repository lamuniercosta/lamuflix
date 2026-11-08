# DEV-459 — Acceptance matrix lock (T023)

Status: **LOCKED** with observed evidence. Operations evidence is `README.md` "Operating a library-relative upgrade" (T022) plus the AppHost README configuration note; the visible outcome is an **actually executed** SMOKE-001 Compose run with recorded status.

Run conditions: worktree `F:/Dev/LamuFlix.worktrees/DEV-459`, branch `feature/DEV-459`, HEAD `8be55a65` plus the T022/T023 working-tree change. M2 successor-scaffold proof executed on fix HEAD `c6c51289` (Phase 4 remediation commit 1). .NET SDK `10.0.403`, Node `v24.22.1`, Docker `29.9.2`, PostgreSQL 17 (Testcontainers `pgvector/pgvector:0.8.7-pg17`). Each row cites the automated test that proves it; no row is satisfied by planned criteria alone.

## 1. Cross-platform identity

| Evidence | Result |
|---|---|
| `LibraryPathCanonicalTests.TryCanonicalizeRelative_SegmentShapes_ProducesCanonicalIdentity`, `..._MixedSeparators_UsesForwardSlashes`, `..._SegmentCasing_IsPreserved`, `..._IsIdempotent` — separators, nesting, casing, `.` root, idempotent relative normalization | PASS |
| `LibraryPathCanonicalTests.TryCanonicalizeUnderRoot_WindowsFixtures_RunsOnAnyHost`, `..._PosixFixtures_RunsOnAnyHost`, `..._UncFixtures_RunsOnAnyHost` — both foreign-OS fixture sets run on one host | PASS |
| `LibraryPathCanonicalTests.TryCanonicalizeUnderRoot_OutsideRootOrEscaping_IsRejected`, `..._ConflictingPathSyntax_IsRejected`, `TryCanonicalizeRelative_InvalidShapes_AreRejected`, `..._AbsoluteInput_IsRejected`, `Constructor_DriveRelativeOrMalformed_ThrowsWithReason` | PASS |
| `LibraryPathCanonicalTests.Constructor_RootIdentity_StoresDot`, `..._RedundantSeparators_StoresCanonicalIdentity` | PASS |
| `LibraryPathUpperInvariantEquivalenceTests.DivergentPairings_AcrossProbeAlphabet_AreExactlyTheRuledPairings` + `UpperKey_ForRuledLongSPairings_...` + `UpperKey_ForRuledGarayPairings_...` — A12 shipping-runtime equivalence outside exactly the ruled divergences (24 where the runtime keeps Garay pairs distinct, 2 where it collapses them) | PASS |
| `LibraryOptionsValidatorTests.Validate_WellFormedAbsoluteAlias_Succeeds`, `..._MalformedOrNonAbsoluteAlias_FailsWithIndexedKey`, `..._OverlappingAliases_FailsWithConflictDiagnostic`, `..._CaseOnlyWindowsAliasOverlap_FailsAsConflict`, `AddLamuFlixOptions_RegistersLibraryValidatorAndKeepsDataAnnotations` | PASS |
| `ArchitectureTests.Core_must_not_reference_...` (EF Core, Npgsql, RabbitMQ, Infrastructure, HttpClient, Process) plus `Core_must_not_depend_on_process_or_process_start_info` — Core stays pure-string, no `System.IO.Path` seam | PASS (16/16) |

## 2. Safe preflight

| Evidence | Result |
|---|---|
| `MovieLibraryPathGateTests.Run_OfflineMedia_MigratesWithoutFilesOnDisk` — offline media migrates with no filesystem presence | PASS |
| `..._Run_UnmappedRoot_FailsClosedWithActionableMovieId`, `..._Run_OverlappingRoots_MapsAmbiguouslyAndFailsClosed`, `..._Run_MalformedRelativeRow_FailsClosed` | PASS |
| `..._Run_CaseOnlyCollision_FailsClosedWithBothMovieIdsAndNoIndexOrWrites`, `..._Run_LongSCollision_BlocksOnWholeStringUpperInvariantKey`, `..._Run_SingleLongSPath_KeysToTheSameUpperInvariantValueAsPlainS`, `..._Run_GarayCasePairs_RemainDistinctUnderOrdinalUpperKeys` — collisions fail before the unique index exists, with IDs (Garay pair collides or stays distinct per shipping-runtime casing) | PASS |
| `..._Run_DefaultDockerAlias_MapsLegacyRowsWhenTheActiveRootIsTheContainerPath`, `..._Run_AutomaticDockerAliasConflictingWithTheActiveRoot_FailsClosed`, `..._Run_NonAbsoluteActiveRoot_FailsClosedBeforeAnyDatabaseWork`, `..._Run_MissingAdditiveColumn_FailsClosedAsSchemaNotReady` | PASS |
| `..._Run_PreservesIdsAndPreExistingBusinessColumns` — IDs and every non-path column unchanged | PASS |

## 3. Atomicity and startup

| Evidence | Result |
|---|---|
| `MovieLibraryPathGateTests.Migrate_AdditiveOnly_LeavesSchemaNotReady`, `..._Migrate_AppliedMigrationList_ContainsOnlyTheAdditiveLibraryPathKeyMigration` | PASS |
| `..._Run_ActivationFailureAfterRealWrites_RollsTheWholeRewriteBack` — rollback after actual writes, not a mid-preflight abort | PASS |
| `..._Run_InterruptedThenRerunAfterRepair_CommitsCleanly` | PASS |
| `..._Run_WhileAnotherSessionHoldsTheTable_FailsClosedOnTheAccessExclusiveLock`, `..._Run_TwoConcurrentGates_SerializeAndBothSucceed` | PASS |
| `..._Run_SecondRunOnActivatedConsistentTable_WritesNothing` — canonical no-op rerun | PASS |
| `..._Run_WithMigrationsDisabled_VerifiesTheActivatedSchemaWithoutWriting`, `..._Run_WithMigrationsDisabledOnSchemaNotReady_FailsClosedWithoutMigrating`, `..._Run_WithMigrationsDisabledAndKeyDrift_FailsClosed` | PASS |
| `ApiStartupLibraryPathBarrierTests.Startup_WithMigrationsEnabled_ActivatesTheLibraryPathKeyBeforeServingHttp`, `..._WithMigrationsEnabledOnCollidingRows_FailsClosedAndServesNoHttp`, `..._WithMigrationsDisabledOnUnactivatedSchema_FailsClosedAndServesNoHttp`, `..._WithMigrationsDisabledOnActivatedSchema_ServesHttp` — Migrate → gate → Run; no HTTP or consumer work before success | PASS |
| `MigrationTests.MigrateAsync_EmptyDatabase_AppliesMigrationsEnablesVectorAndHasNoPendingModelChanges` — `has-pending-model-changes` clean | PASS |
| Unrelated-successor scaffold proof (`dotnet ef migrations add SuccessorProbe`, `20261108001840`, scratch removed after): `Up`/`Down` empty, zero `migrationBuilder.` operations, no `AlterColumn`/`CreateIndex`; `has-pending-model-changes` exit 0 before and after | PASS |
| F8 immediate-successor `migrations remove` recovery, executed: `remove --force` reverted the in-repo snapshot to interim; redid Phase B external `migrations add LibraryPathKeyActivation --output-dir` outside the worktree — `AlterColumn(library_path_key)` + `CreateIndex(IX_movies_library_path_key)` live only in the deleted external scratch; in-repo snapshot restored byte-identical, no hand-edits, `has-pending-model-changes` exit 0 | PASS |

## 4. Persistence invariants

| Evidence | Result |
|---|---|
| `MovieLibraryPathKeyUniquenessTests.Column_KeyColumn_CarriesBinaryCollationAndNotNullAfterActivation`, `MigrationTests.ActivateLibraryPathKey_AdditiveMigrateThenGate_RetainsExactIndexAndActivatesColumnCollationC` — exact index retained, key index with effective **column** collation `C` | PASS |
| `MovieLibraryPathKeyUniquenessTests.ExactIndex_ExactDuplicatePath_RejectsWith23505OnLibraryPathIndex`, `..._KeyIndex_CaseOnlyDuplicate_RejectsWith23505OnLibraryPathKeyIndex`, `..._KeyIndex_SameExactPathWithDifferentCasing_RejectsOnTheKeyIndex` | PASS |
| `..._KeyIndex_LongSAndPlainS_RejectsWith23505OnLibraryPathKeyIndex`, `..._KeyIndex_GarayCasePair_AcceptsBothRowsUnderDistinctOrdinalKeys` — ruled comparison semantics enforced by the database (accepts both rows, or rejects the second with 23505, per shipping-runtime casing) | PASS |
| `..._KeyIndex_ConcurrentCaseOnlyImports_ProduceExactlyOneWinnerAndOne23505` | PASS |
| `MovieLibraryPathKeyUniquenessTests.SaveChangesChokePoint_EveryTrackedWriter_DerivesWholeStringUpperInvariantKey`, `..._SynchronousSaveChanges_AlsoDerivesTheKey`, `..._PathMutatedOnTrackedRow_RefreshesTheKey`, `..._UnmodifiedRows_LeaveTheStoredKeyAlone` | PASS |
| `ArchitectureTests.Library_path_gate_must_be_a_sealed_concrete_infrastructure_type_with_no_port_or_interface` | PASS |

## 5. Import and rescan

| Evidence | Result |
|---|---|
| `ImportMovieFolderCommandHandlerTests.HandleAsync_RelativeFolder_CanonicalizesBeforeScanningAndPersisting`, `..._AbsolutePathUnderActiveRoot_CanonicalizesToRelativeIdentity`, `..._AbsoluteActiveRootItself_CanonicalizesToRootIdentity` | PASS |
| `..._HandleAsync_AbsolutePathOutsideActiveRoot_RejectsWithoutScanningOrPersisting`, `..._LegacyAliasPath_RejectsBecauseAliasesNeverAuthorizeNewImports`, `ImportEndpointTests.ImportMovie_OutsideRootPath_ReturnsUnprocessableEntityWithoutScanning`, `..._LegacyAliasPath_ReturnsUnprocessableEntityBecauseAliasesNeverAuthorizeImports` | PASS |
| `ImportEndpointTests.ImportMovie_DuplicateFromEitherConstraint_KeepsTheExistingOutcomeWithoutANewConflictMapping` — both `23505` sources route through the existing duplicate outcome with no new 409 mapping | PASS |
| `ImportMovieFolderCommandHandlerPersistenceTests.HandleAsync_CaseOnlyDuplicate_RejectsOnTheKeyIndexAndPreservesTheWinner`, `..._HandleAsync_DuplicateLibraryPath_RealDatabaseRejectsSecondImport_RecordsOneImport`, `ImportLibraryCommandHandlerPersistenceTests.*` (bulk persistence) — sequential duplicate outcomes preserve the winner ID | PASS |
| `DirectoryMediaLibraryScannerTests.Scan_...` and `ResolvePrimaryVideo_...` relative/root-identity/CWD-divergent cases; `FileSystemLibraryFolderDiscoveryTests.DiscoverAsync_DescendantCandidate_YieldsCanonicalRootRelativePath`, `..._RootCandidate_YieldsCanonicalRootIdentity`, `..._ProcessCurrentDirectoryDiffersFromRoot_StillDiscoversUnderActiveRoot`, `..._NonAbsoluteConfiguredRoot_FailsClosedInsteadOfUsingProcessCurrentDirectory` | PASS |
| `RescanEndpointTests.RescanMovie_*` plus `RescanMovieCommandHandlerTests` — rescan preserves stored identity and resolves the active root | PASS |
| Automated `MovieDetailsResponse.Path` relative-value assertion (distinct from SMOKE-001): `LibraryEndpointsTests.GetMovieDetails_PopulatedMetadata_MapsEveryFrozenScalarMember` asserts `path` is `Heat/Heat.mkv` (relative, not rooted) on the real endpoint; `ImportMovieFolderCommandHandlerPersistenceTests.HandleAsync_AbsolutePathUnderRoot_PersistsRelativeIdentityAndDerivedKey` and `..._DuplicateLibraryPath_...` assert the stored relative value and derived key | PASS |

## 6. Queue transition

| Evidence | Result |
|---|---|
| `ThumbnailConsumerTests.Consumer_ALegacyWindowsAbsolutePayload_CanonicalizesThroughTheActiveRootAndGenerates`, `..._ALegacyAbsolutePayloadUnderAConfiguredAlias_CanonicalizesAndGenerates` — old Windows/Docker absolute deliveries and configured aliases after backfill | PASS |
| `..._ARelativePayloadResolvesUnderTheConfiguredRootNotTheProcessWorkingDirectory` — new relative deliveries, CWD-independent | PASS |
| `..._APayloadNamingAnotherMoviesPath_DeadLettersAsIneligibleAndNeverAuthorizesOnId`, `..._ALongSPathPayload_CollidesWithThePlainSKeyAndGenerates`, `..._AGarayCasePairPathPayload_DoesNotMatchTheStoredKeyAndDeadLettersAsIneligible` — whole-string `ToUpperInvariant` ordinal identity, never ID-only (generates when the runtime collapses the pair) | PASS |
| `..._AnAbsolutePayloadUnderAnUnknownRoot_DeadLettersAsUnauthorizedWithoutGeneration`, `..._ASourceThatEscapesTheLibraryRoot_DeadLettersAsUnauthorized`, `..._ASourceFileThatIsASymlink_DeadLettersAsUnauthorized`, `..._AnAncestorDirectoryThatIsASymlink_DeadLettersAsUnauthorized`, `..._ARequestForAMovieThatNoLongerExists_DeadLettersAsIneligible` — terminal outcomes, no source access, no retry loop | PASS |
| `EnrichmentConsumerTests.Consumer_AnEligibleEnrichmentWithThumbnailsEnabled_PublishesBeforeAck` — publishes `ThumbnailRequested` carrying the movie's canonical `Path.Value` with the message schema unchanged | PASS |

## 7. Shared runtime resolution

| Evidence | Result |
|---|---|
| `DirectoryMediaLibraryScannerTests.Scan_ProcessCurrentDirectoryDiffersFromRoot_StillResolvesAgainstRoot`, `ResolvePrimaryVideo_ProcessCurrentDirectoryDiffersFromRoot_StillResolvesAgainstRoot`, `FileSystemLibraryFolderDiscoveryTests.DiscoverAsync_ProcessCurrentDirectoryDiffersFromRoot_StillDiscoversUnderActiveRoot` | PASS |
| `ApiEndToEndPlaybackTests.PlayMovie_ImportedThenSwappedAndRescanned_LaunchesTheResolvedFileAndRecordsHistory`, `PlaybackEndpointTests.PlayMovie_RelativeStoredPath_PassesTheStoredIdentityToTheScannerWithoutRewritingIt`, `..._PlayMovie_StaleStoredFormat_SelectsPlayerFromResolvedExtension` — playback resolves relative stored identity under the active root through the existing scanner seam | PASS |
| `ApiEndToEndPlaybackTests.PlayMovie_LocalPlayDisabled_ReturnsForbiddenProblemWithoutProcessStart`, `ArchitectureTests.Core_must_not_depend_on_process_or_process_start_info` — `Features:LocalPlay` boundary and launcher untouched | PASS |

## 8. Operations

| Evidence | Result |
|---|---|
| `README.md` §"Operating a library-relative upgrade" (T022) — shared `Library:RootPath` / `Library:LegacyRootPaths`, default Docker alias conflicts, hard stop of all old instances, matching application version/runtime/globalization/ICU and roots/aliases, pre-upgrade backup, no mixed-version writers, backup-restore downgrade, preflight diagnostics table, alias retirement after queue drain, F8 immediate-successor `migrations remove` recovery, named backstops (post-activation `NOT NULL`, startup drift fail-closed) and the explicit statement that no new pre-activation interlock exists. No real connection values. | PASS (doc review against brief A6/A9 and spec FR-018) |
| `src/LamuFlix.AppHost/README.md` — `Library:LegacyRootPaths` user-secrets guidance and a pointer to the runbook | PASS |
| `docker/api.settings.example.json` — `Library:RootPath` plus `Library:LegacyRootPaths` present in the plain (non-e2e) stack configuration | PASS |
| `LibraryOptionsValidatorTests` — startup validation fails closed on malformed or overlapping aliases before any gate write | PASS |

## Visible outcome — SMOKE-001

**Status: PASS (observed).**

Executed with `pwsh -NoProfile -File scripts/run-e2e-smoke.ps1` against the plain Compose stack (base `docker-compose.yml` plus the e2e stub overlay; library mounted at `/media/library`, `Library:RootPath` = `/media/library`, empty `Library:LegacyRootPaths`), exit code **0**.

Observed runtime evidence, `web/e2e/library-path.e2e.ts` test title `SMOKE-001 shows and copies the library-relative folder instead of an absolute host path`:

- The seeded movie imported as `seeded/Big Buck Bunny (2008)` is served by `GET /api/movies/{id}` with `path` exactly `seeded/Big Buck Bunny (2008)` — relative, not the host absolute path and not `/media/library/...`.
- The movie details "File" line renders that same relative value; the `Copy path` button reports `Copied` and the clipboard content equals it.
- Full suite result: `7 passed (1.8m)`, including all pre-existing smoke specs (`browse`, `genre`, `import`, `for-you-review`, `manual-match` SMOKE-001/SMOKE-002). Log: `artifacts/e2e-smoke/run-e2e-smoke.log`.

## Gate snapshot at lock time (build-side only; Gauge owns the ticket gates in Phase 8)

| Check | Result |
|---|---|
| `dotnet test LamuFlix.sln` | Unit 2907 passed; Integration 1494 passed, 2 skipped (0 failed); Architecture 16 passed; Evaluation 81; Evaluation.Integration 21 |
| `npm run typecheck:e2e` | exit 0 |
| `npx eslint e2e` / `npx prettier --check e2e` | exit 0 |
| `scripts/run-e2e-smoke.ps1` | exit 0 (PASS) |
| Web implementation / contract drift gates | Not applicable — this ticket plans no frontend implementation and no contract change; not claimed as PASS |
