# 0024. Movie features storage port

- Status: Accepted
- Date: 2026-10-22
- Ticket: DEV-413

DEV-413 requires persisted movie embeddings shared by DEV-414 and DEV-348. Introduce the specific Core port `IMovieFeaturesRepository` with an Infrastructure adapter so callers can read and replace stored embeddings without depending on EF Core or pgvector types. This extends the constitution through its existing new-port ADR mechanism; it requires the synchronized amendment below before implementation merges.

Basis: [brief](../../specs/DEV-413/brief.md), Q1-Q4 and Q7; [conclusions](../../specs/DEV-413/CONCLUSIONS.md), cited Patron rulings Q1-Q4 and Q7; task note `DEV-413:21`; evidence note `recon-DEV-413-2:16-41,49`. The constitution requires an ADR for every new port regardless of ticket size (Principle I, PR gates and Documentation Rules; Q7 cites lines 136-140, 375 and 499-501).

## Decision

Place `IMovieFeaturesRepository` and sealed record `MovieFeaturesSnapshot` in `Core/Ports/`. The port exposes `Task<MovieFeaturesSnapshot?> GetByMovieIdAsync(MovieId movieId, CancellationToken ct)` and `Task UpsertAsync(MovieFeaturesSnapshot features, CancellationToken ct)`. The snapshot carries `MovieId MovieId`, `ImmutableArray<float> Vector` and `DateTimeOffset ComputedAt`; it keeps storage contracts independent of `Pgvector.Vector`. Preserve the existing recommendation `MovieFeatures` type and existing ports.

Require 384 finite vector elements using `EmbeddingDimensions.Value`; reject null snapshots, default arrays, wrong dimensions and NaN/infinity before SQL. Permit finite zero vectors. Reads return null when absent. Callers supply computation time; persist its UTC instant at PostgreSQL timestamptz precision without a repository clock. Both methods check and propagate cancellation. Future callers use injected `TimeProvider`; their implementation is outside DEV-413.

Infrastructure owns `MovieFeaturesRecord`, its EF configuration, `Pgvector.Vector` and `EfMovieFeaturesRepository`, registered scoped through the existing persistence registration. Store required `movie_id integer` as a non-generated MovieId-converted PK and cascading FK to movies, required `vector vector(384)` and required `computed_at timestamp with time zone`. Add the context DbSet and one scaffolded additive `AddMovieFeatures` migration, retaining published migrations and the existing vector extension. Create `IX_movie_features_vector` transactionally through raw SQL using HNSW with `vector_cosine_ops` and default parameters; keep the EF model and generated snapshot consistent without duplicate index creation.

Use one parameterized PostgreSQL `INSERT ... ON CONFLICT (movie_id) DO UPDATE` to replace vector and computation time together unconditionally. Concurrent writes retain one row with one complete submitted pair; the last successful database write wins. A missing parent fails through the FK. Timestamp freshness comparison and optimistic concurrency are outside this contract.

## Consequences and publication requirements

The specific port adds a shared storage boundary without introducing dependencies, projects or architectural layers. DEV-413 excludes embedding generation, consumer orchestration, ranking/retrieval, HTTP/web changes and HNSW tuning. Focused real pinned-pgvector Testcontainers proof must cover migration lifecycle, schema/index and model alignment, persisted reads, sequential/concurrent upserts, FK/cascade behavior and cancellation; unit/model/property proof covers vector validation. Query translation and serialization require real database evidence.

This ADR is Accepted and published in the same build PR as the port and constitutional amendment. Phase B pickup recon confirmed that ADR 0024 remains unclaimed on `origin/main`. The constitution is synchronized with this decision in the same change set, and implementation does not merge ahead of that synchronization.

Under Q7 and the constitution amendment procedure, `IMovieFeaturesRepository` is added immediately after `IPlaybackHistory` in Principle I's closed port enumeration, preserving the BCL/M.E.AI abstractions and `Adding a port requires an ADR.` The settled MINOR amendment from 1.3.0 to 1.4.0 updates the top Sync Impact Report, Version and Last Amended using the actual amendment date, and `.specify/templates/*.md` are reviewed with evidence for any required edits or a no-change conclusion. Existing ports and callers require no behavior migration.

`|IMovieFeaturesRepository` is added to `PortWhitelistPattern` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:28-29`, preserving existing names, anchors, assertions and the anti-vacuity floor. It is verified through `Core_ports_must_match_the_constitution_whitelist` and applicable Phase B gates; no harness configuration, gate script or threshold changes. Q7 rules this a compliant constitutional extension with zero owner checkboxes.
