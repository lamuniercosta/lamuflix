# Implementation Plan: EfMovieRepository

**Branch**: `feature/301-spec` | **Date**: 2026-09-30 | **Spec**: specs/DEV-301/spec.md

**Input**: `spec.md`, `brief.md`, `CONCLUSIONS.md` (Q1-Q12), `ASSUMPTIONS.md`, recon-DEV-301

## Summary

Add a sealed `EfMovieRepository` implementing all five `IMovieRepository` members over `LamuFlixDbContext`, with an atomic single-statement `ExecuteUpdateAsync` claim, plus `Movie.Rehydrate` in Core. Proven against real Postgres.

## Technical Context

**Language/Version**: C# 14 / .NET 10
**Dependencies**: none new (Q12). EF Core, Npgsql, xUnit v3, Shouldly and Testcontainers fixture already present.
**Storage**: PostgreSQL via existing `PostgresFixture`; no schema change.
**Testing**: xUnit v3 + Shouldly; no mocks of database behaviour.
**Constraints**: Core stays persistence-independent; no new project, folder, layer or abstraction.

## Constitution Check

- New dependency: none. New project/folder/layer: none (flat in `Infrastructure/Persistence/`).
- Schema change: none (Q4 calls the existing sequence).
- API shape: none. `Features:LocalPlay`, secrets, `Process.Start`: untouched.
- File scope: only the frozen scope in `brief.md`. The `Movie.cs` edit (`Rehydrate` only) is ruled in Q2 (§2.3 item 6).
- No ADR (Q12). No owner checkboxes.
- Status: PASS, no departures.

## Project Structure

```text
src/LamuFlix.Core/Domain/Movie.cs                                          (edit: add Rehydrate only)
src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs               (new)
src/LamuFlix.Infrastructure/Persistence/<private mapping helpers>          (only if a complexity gate forces it)
tests/LamuFlix.UnitTests/Domain/MovieRehydrateTests.cs                     (new)
tests/LamuFlix.UnitTests/Domain/MovieRehydratePropertyTests.cs             (new, D15; may share the file above)
tests/LamuFlix.UnitTests/Persistence/EfMovieRepositoryConstructorTests.cs  (new, D16: AC5 guard, no Docker)
tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs                  (new)
tests/LamuFlix.IntegrationTests/EfMovieRepositoryClaimConcurrencyTests.cs  (new)
tests/LamuFlix.IntegrationTests/  SQL-capture DbCommandInterceptor, FixedTimeProvider (project root, flat; only if absent)
```

`LamuFlix.Tests.Common` (including `LamuFlixDbContextFactory`) is not edited.

## Design

1. **Constructor**: `EfMovieRepository(LamuFlixDbContext db, TimeProvider timeProvider, IOptions<EnrichmentOptions> options)`. It reads `options.Value.ClaimLease` once, throws `ArgumentOutOfRangeException` if it is `<= TimeSpan.Zero`, and stores it.
2. **Mapping (D1, D9)**: `Movie.Rehydrate` is `public static Movie Rehydrate(MovieId id, string title, LibraryPath path, MediaFormat format, bool isInWatchlist, MovieMetadata? metadata, EnrichmentStatus status, DateTimeOffset? enrichedAt, int enrichmentAttempts, EnrichmentFailureCategory? lastFailureCategory, DateTimeOffset? lastAttemptAt)`: the 11 properties of `Movie.cs:19-39`, in declaration order. Its only direct check is `Create`'s blank-title `ArgumentException`. Invalid value-object state is rejected earlier, when the value objects are constructed. There are no new invariants and no parameter object. The private static `ToDomain(MovieRecord)` calls it with **named arguments** (Ledger F2). The metadata columns are `Title`↔`MetadataTitle`, `Synopsis`↔`Plot`, `Runtime`↔`RuntimeMinutes`, `ReleaseYear`, `ImdbRating` and `ImdbId`, and `Metadata` is `null` exactly when `MetadataTitle` is `null`. `Apply(Movie, MovieRecord, Baseline)` writes **only fields the aggregate models**. Record-only data (`RottenTomatoesRating`, `MetaScore`, `PosterUrl`, and the `Actors`/`Directors`/`Genres` skip navigations) is never read into the aggregate and never written from it. `GetAsync` loads the record tracked, with no `Include`, and returns `null` for an absent id without adding anything to the identity map (D11). `MovieRecord` shape is unchanged.
3. **Baseline/diff (Q3)**: `GetAsync` and `AddAsync` snapshot the persisted aggregate state as a private `Baseline` record. `SaveChangesAsync` writes only fields that differ from the baseline. `EnrichmentAttempts` is written as a delta (`record.EnrichmentAttempts += aggregate - baseline`), so a database-side claim increment survives a later `Mark*` save. The baseline is refreshed after the save. Deltas are computed only from the private `Baseline`, never from EF `OriginalValues` (Ledger F8; review checkpoint on T012). `AddAsync` of an id already in the identity map throws `InvalidOperationException` (D11).
4. **Claim sync (Q3, D2, D8)**: when the claim returns `true` and the id is in the identity map, `LastAttemptAt` is stamped from the in-process `now` as both the current and original value on the tracked record. `EnrichmentAttempts` is re-read with a no-tracking projection and set as the current and original value. The private `Baseline` claim fields are not advanced, because the aggregate cannot be updated in memory and advancing the baseline would zero out the `Mark*` delta (D2). Other pending changes are left untouched.
5. **Claim (Q5)**: a single `ExecuteUpdateAsync`. `Status == EnrichmentStatus.Pending` goes through `EnrichmentStatusConverter`, and the rendered predicate `status = 0` is recorded here. `now` is sampled once, `leaseExpiry = now - ClaimLease`, and the method returns `rows == 1`. No raw SQL.
6. **Identity (Q4, D13)**: `db.Database.SqlQuery<long>($"SELECT nextval(pg_get_serial_sequence('movies', 'id')) AS \"Value\"").SingleAsync(ct)`, an interpolated string with no holes. Then `MovieId.TryCreate`, or `InvalidOperationException` if that fails (the guard is not integration-tested, D5). If a banned-symbol rule or analyzer rejects this call, report to Keel; do not switch to a `Raw` API.
7. **Cancellation (D12)**: every database call forwards the CT: the `GetAsync` query, the identity query, `SaveChangesAsync`, the claim, and the claim-sync re-read. Cancellation surfaces as `OperationCanceledException`.
8. **Errors (D11)**: EF/Npgsql exceptions, including `DbUpdateException`, propagate unchanged. There is no catching, wrapping or retrying.

### Known limitations (accepted)

- **No concurrency token (Sentry R2).** Concurrent `SaveChangesAsync` calls from different scopes are last-writer-wins, for the fields each aggregate changed. Adding a token is a model/schema change outside the frozen scope (AC10). Proposed follow-up F4, for Patron to rule on.
- **Claim-sync re-read race (Sentry R1, D8).** Another scope's save that commits between the claim `UPDATE` and the `EnrichmentAttempts` re-read is picked up by the re-read. This is the same no-token limitation.

## Test Strategy

- **Integration** (`PostgresFixture`; the repository is constructed directly, Q11): AC6-AC8. Each test seeds its own movies and does not depend on test order. **Database shape (D14):** each test class creates and migrates one database with `LamuFlixDbContextFactory.CreateContext(fixture)` and then `MigrateAsync`. Any second context attaches to **the same database** through the sibling pattern (`PersistenceRoundTripTests.cs:169-174`). The container's default connection string is never used.
- **Concurrency (D10, D14, Ledger F5):** `ClaimIterations = 50`, run **sequentially**. Each iteration seeds a fresh Pending movie, then builds two claim contexts in the test file with `new DbContextOptionsBuilder<LamuFlixDbContext>().UseNpgsql(siblingConnectionString).AddInterceptors(capture).Options`, and two repositories sharing one fixed `FixedTimeProvider` and a positive lease. A per-iteration `TaskCompletionSource` start gate releases both claims, which are awaited with `Task.WhenAll`. Both contexts are disposed (`await using`) before the next iteration, so at most two claim connections are open at once. The SQL-capture interceptor goes on the claim contexts only.
- **Record-only preservation (D9, Compass F4):** seed a fully populated record (rotten tomatoes, meta score, poster URL, one actor, one director, one genre). Load it, save after `MarkEnriched` and again after `AddToWatchlist`, reload, and assert all of that data is intact.
- **Unit** (`LamuFlix.UnitTests`): the `Rehydrate` round trip uses distinct, non-default values for every field (Ledger F2). The blank title is rejected. After rehydrating, legal transitions succeed and illegal ones throw. The AC5 constructor guard test is in `UnitTests/Persistence`, with a dummy Npgsql connection string that is never opened (D16).
- **Property (D15)**: FsCheck. For any valid field set, `Rehydrate` returns an aggregate whose 11 properties equal the inputs.
- Fixed UTC instant at microsecond precision, with a small `FixedTimeProvider` subclass. The IntegrationTests copy mirrors `UnitTests/Features/FixedTimeProvider.cs` (Ledger F3; the duplicate is accepted).

## Gates

After each `.cs` edit: Roslyn analyzers, cyclomatic complexity <= 15, InspectCode, and `dotnet format --verify-no-changes`. Refactor stage: complexity <= 6. Private helpers are extracted only if that gate forces it, plus the D15 property test. Close-out: `./scripts/run-property-tests.ps1` and `./scripts/run-vulnerable-packages.ps1` (D15), then the full `dotnet test` (needs Docker). Mutation covers Core and Infrastructure per `scripts/run-mutation.ps1`. Infrastructure is expected to come back as "Could not run" (Ledger F7, Q12), reported with the script output and returned to Keel. D5 guard survivors are listed and are not treated as failures.
