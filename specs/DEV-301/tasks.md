# Tasks: EfMovieRepository

**Input**: `specs/DEV-301/` spec.md, plan.md, brief.md (task ordering per brief "Task ordering"; decisions D1-D16; plan frozen per brief "Plan freeze")

**Tests**: Required (TDD; write failing test first per task).

Gate step after every `.cs` task: Roslyn analyzers, complexity, InspectCode, `dotnet format --verify-no-changes`.

## Phase 1: Core and skeleton (T001-T003 and T004-T006 may run in parallel)

- [ ] T001 [P] [US5] Write `tests/LamuFlix.UnitTests/Domain/MovieRehydrateTests.cs`: round-trip every field using distinct non-default values (`enrichedAt` and `lastAttemptAt` differ; both nullable enums non-null; Ledger F2); reject a blank title (D1: this is the only invalid-state case, with no new invariants); legal and illegal transitions after rehydrate (AC9)
- [ ] T002 [US5] Add `Movie.Rehydrate(...)` with the D1 signature to `src/LamuFlix.Core/Domain/Movie.cs` (the only edit there; no `Mark*` calls, no replay)
- [ ] T003 [US5] Run gates on Core changes
- [ ] T004 [P] [US4] Write failing test: the constructor throws `ArgumentOutOfRangeException` for a `ClaimLease` of zero and for a negative one (AC5), in `tests/LamuFlix.UnitTests/Persistence/EfMovieRepositoryConstructorTests.cs`. Build the context with `UseNpgsql` and a dummy connection string that is never opened, so no Docker is needed (D16)
- [ ] T005 [US4] Create `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` skeleton: constructor, lease guard, store lease (members stubbed)
- [ ] T006 [US4] Gates on skeleton

## Phase 2: Identity, add, get, save (sequential)

Each test seeds its own movies; no reliance on test order. The test class creates and migrates one database with `LamuFlixDbContextFactory.CreateContext(fixture)`; any second context uses the sibling pattern (`PersistenceRoundTripTests.cs:169-174`; D14).

- [ ] T007 [US2] Tests: `NextIdentityAsync` + `AddAsync` insert with the explicit id. Run this test first in the phase; it proves `pg_get_serial_sequence` resolves (Sentry R5). The out-of-range guard is not integration-tested (D5). `AddAsync` of an id already tracked throws `InvalidOperationException` (D11)
- [ ] T008 [US2] Implement `NextIdentityAsync` (the exact `SqlQuery<long>` call from D13) and `AddAsync` (identity map, baseline, duplicate-id guard; CT forwarded, D12)
- [ ] T009 [US2] Tests: a second `GetAsync` returns the same instance; all 11 aggregate properties are restored, including the six `Metadata` fields (D9); an absent id returns `null` (D11)
- [ ] T010 [US2] Implement `GetAsync` with private mapping (`ToDomain` calls `Rehydrate` with named arguments; D9 column mapping; no `Include`; CT forwarded)
- [ ] T011 [US2] Tests: five-member round trip; a saved `Mark*` transition; record-only preservation. Seed rotten tomatoes, meta score, poster URL, one actor, one director and one genre. Load, save after `MarkEnriched` and again after `AddToWatchlist`, reload, and assert all of that data is intact (D9, Compass F4)
- [ ] T012 [US2] Implement `SaveChangesAsync`: baseline diff over fields the aggregate models only, attempts delta, baseline refresh, exceptions propagate unchanged (D11). Review checkpoint: deltas are computed only from the private `Baseline`, never from EF `OriginalValues` (Ledger F8)
- [ ] T013 [US2] Gates

## Phase 3: Claim (sequential)

Each test seeds its own movies; no reliance on test order.

- [ ] T014A [US1] Add `FixedTimeProvider` at the `tests/LamuFlix.IntegrationTests/` root, mirroring `tests/LamuFlix.UnitTests/Features/FixedTimeProvider.cs` (the duplicate is accepted, Ledger F3; D7)
- [ ] T014 [US1] Tests (AC8): absent id; terminal statuses (Enriched, NotFound, Failed); unexpired lease; expired lease with a persisted increment and `last_attempt_at == now`; strict boundary; a sequential double claim at one instant returns `true` then `false`, with no second increment (Compass F7)
- [ ] T015 [US1] Implement `TryClaimForEnrichmentAsync` (single `ExecuteUpdateAsync`, `now` sampled once, CT forwarded)
- [ ] T016 [US3] Tests (D3), both asserted: (a) load (attempts = a) -> claim -> `AddToWatchlist` -> save -> reload: `last_attempt_at` = claim `now`, attempts = a+1, watchlist change persisted; (b) load -> claim -> `Mark*(now2)` -> save -> reload: attempts = a+2, `last_attempt_at` = `now2`
- [ ] T017 [US3] Implement claim sync: stamp `LastAttemptAt` from the in-process `now`; re-read only `EnrichmentAttempts` with a no-tracking projection, CT forwarded; set both as current and original values on the tracked record (D8). Do NOT advance the private `Baseline` claim fields: the aggregate cannot be updated in memory, so advancing it would zero out the `Mark*` delta (D2)
- [ ] T018 [US3] Gates

## Phase 4: Concurrency proof

- [ ] T019 [US1] Add the SQL-capture `DbCommandInterceptor` at the `tests/LamuFlix.IntegrationTests/` root, only if absent. Build the claim contexts in the test file with `new DbContextOptionsBuilder<LamuFlixDbContext>().UseNpgsql(siblingConnectionString).AddInterceptors(capture).Options`. Do not edit `LamuFlixDbContextFactory` (Compass F9)
- [ ] T020 [US1] Write `EfMovieRepositoryClaimConcurrencyTests.cs` (project root). Create and migrate **one** database via `CreateContext(fixture)`, and attach both claim contexts to it by the sibling connection string (D14). Run `ClaimIterations = 50` **sequentially**. Each iteration seeds a fresh Pending movie, builds two fresh interceptor contexts and repositories with a shared fixed time and a positive lease, and releases both claims with its own `TaskCompletionSource` gate and `Task.WhenAll`. Both contexts are disposed with `await using` before the next iteration (D10). Each iteration must yield exactly one `true` and one `false`, with attempts +1 (AC6)
- [ ] T021 [US1] Assert exactly one predicate `UPDATE` per invocation; no `LOCK TABLE`, no `SELECT ... FOR UPDATE` (AC7)
- [ ] T022 Per D6, confirm reliability (AC1): 3 consecutive filtered runs of the concurrency class, plus a pass inside the full-suite run; record all 4 results in the receipt

## Phase 5: Close-out

- [ ] T023 Refactor pass: `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`. Extract private helpers in the same folder only if the gate forces it; no new abstraction. Add the FsCheck property test `tests/LamuFlix.UnitTests/Domain/MovieRehydratePropertyTests.cs`: for any valid field set, `Rehydrate` returns an aggregate whose 11 properties equal the inputs (D15)
- [ ] T024 `./scripts/run-property-tests.ps1` and `./scripts/run-vulnerable-packages.ps1` (D15); full suite `dotnet test` (Docker required); `dotnet format --verify-no-changes`
- [ ] T025 Mutation gate for Core and Infrastructure. If it cannot run, report "Could not run" with the script output and return to Keel; never lower thresholds or mock the DB. Infrastructure is expected to come back as "Could not run" (Ledger F7). D5 guard survivors are listed and are not treated as failures
- [ ] T026 Verify `git diff --stat origin/main...HEAD` shows only frozen-scope files (brief, including the D16 amendment) and no `.csproj`/props/migration/DI/Tests.Common edits (AC10)

## Dependencies

Phase 1 tracks are independent; Phase 2 needs T002 and T005 (T010's `ToDomain` calls `Rehydrate`); Phase 3 needs Phase 2 and T014A; Phase 4 needs Phase 3; Phase 5 last.
