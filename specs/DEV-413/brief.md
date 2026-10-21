# DEV-413 - Grill brief

Status: Grill complete. Patron ratified Q1-Q5 and confirmed shared understanding in Q6, with zero decision corrections and zero owner checkboxes. This brief governs Quill drafting; it is not plan freeze or Gate 1.

## Identity and authority

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-413-spec`; branch `feature/413-spec`.
- Intake base: `503f0ed8c0f4d435aabde2d602b1d3893b4a147e` (note DEV-413:11-14).
- Ticket: DEV-413, M, parent DEV-288, no UI. Task note DEV-413:21 is the supplied ticket summary and AC.
- Fact source: `recon-DEV-413:1-98`. Product authority: `specs/PRODUCT.md`; constitution: `.specify/memory/constitution.md`; process authority: `lamuflix-team-charter`.
- Full questions, answers, cited rulings, rationale and implications are append-only in adjacent `CONCLUSIONS.md`. Q1-Q5 and the Q7 closeout below are the operative decision register. No facts are inferred from absent DEV-414/DEV-348 specs.

## Ticket scope already decided

Deliver `MovieFeaturesRecord` with `MovieId` PK/FK to movies, `Vector vector(384)` and `ComputedAt`; additive migration creates HNSW cosine index via raw SQL; repository read-by-movie and upsert for DEV-414 and DEV-348. AC: migration on pinned pgvector image; `pg_indexes` proves HNSW index on `movie_features.vector`; upsert same MovieId twice keeps one row and updates ComputedAt. These are authoritative ticket decisions (DEV-413:21), not owner checkboxes.

## Grill answers and plan decisions

### Q1 - Contract and architecture

ACCEPT: `Core/Ports/IMovieFeaturesRepository` exposes `Task<MovieFeaturesSnapshot?> GetByMovieIdAsync(MovieId movieId, CancellationToken ct)` and `Task UpsertAsync(MovieFeaturesSnapshot features, CancellationToken ct)`. `Core/Ports/MovieFeaturesSnapshot` is a sealed record with `MovieId MovieId`, `ImmutableArray<float> Vector`, `DateTimeOffset ComputedAt`. Enforce 384 finite elements, using `EmbeddingDimensions.Value`. Infrastructure owns `Pgvector.Vector`, `MovieFeaturesRecord`, `MovieFeaturesConfiguration` and `EfMovieFeaturesRepository`; register the port/adapter scoped in the existing persistence registration. Existing recommendation `MovieFeatures` is unchanged. Add the constitution-required new-port ADR. Basis: DEV-413:21; recon:27-41,54-60,78-87; constitution port and dependency boundaries; PRODUCT sections 1-3.

### Q2 - Write and read semantics

ACCEPT: caller supplies computation time; store its UTC instant at PostgreSQL timestamptz precision, with no repository clock. One parameterized PostgreSQL `INSERT ... ON CONFLICT (movie_id) DO UPDATE` unconditionally replaces vector and computed_at together. Concurrent writes yield one row and one complete submitted pair; last successful database write wins. No timestamp freshness comparison or optimistic concurrency policy. Missing reads return null. Reject null snapshot, default vector, wrong dimension and NaN/infinity before SQL. Finite zero vector is permitted. Database FK rejects missing parents. Check/propagate CancellationToken on both methods. Future callers use injected TimeProvider when producing time; implementing them is outside this ticket. Basis: Q1; DEV-413:21; recon:19,26,30,37,47,58,78-80.

### Q3 - Schema and migration

ACCEPT: `movie_features` has required `movie_id integer` PK with MovieIdConverter and ValueGeneratedNever, required `vector vector(384)` and required `computed_at timestamp with time zone`. FK to movies cascades on delete. No other table alteration. Add `DbSet<MovieFeaturesRecord> MovieFeatures` to the context. One EF-scaffolded `AddMovieFeatures` migration with generated designer/snapshot; preserve all published migrations. Create `IX_movie_features_vector` via raw SQL `CREATE INDEX "IX_movie_features_vector" ON movie_features USING hnsw (vector vector_cosine_ops);` using default HNSW parameters, transactionally. Down drops movie_features and its index, retaining the existing vector extension. EF model/snapshot must agree (`HasPendingModelChanges() == false`), with no duplicate index creation. Phase B verifies supported provider/index representation through authoritative docs rather than guessing. Basis: DEV-413:21; recon:30,35-39,45-60,65-70; constitution vector-store row.

### Q4 - Tests and authorized existing-file edits

ACCEPT: use existing pinned pgvector Testcontainers fixtures, without mocking query translation or serialization. Cover clean migration, upgrade from AddManualImdbId, rollback/reapply retaining vector extension and prior movie data; exact column type/dimension/nullability/PK/FK/cascade; pg_indexes proves HNSW/cosine/vector and expected index name; model/snapshot alignment. Repository proof covers absent null, vector and timestamp round-trip, two sequential upserts with one updated row, two-context concurrent upserts with one coherent submitted pair, missing-parent FK, cascade deletion and cancellation. Fresh contexts observe persisted reads; UTC test instants are representable at database precision. Unit/model/FsCheck proof covers default/wrong-sized/nonfinite vectors and valid finite vectors including zero. Preserve coverage when replacing obsolete global no-vector assertions with exact movie_features.vector checks. All test/gate execution belongs to Phase B. Basis: DEV-413:21; recon:45,49,57-74,79-80; constitution testing rules.

### Q5 - Loop discipline

ACCEPT: verified Critical and High findings block. Medium blocks on a demonstrated ticket, constitution or Q1-Q4 contract violation; record disposition otherwise. Low and optional improvements do not block. Unresolved mandatory gates and owner checkboxes prevent closure.

Frozen scope: Q1-Q4 movie_features storage, its specific Core port/snapshot/adapter/DI, additive migration/raw HNSW cosine index, required ADR, focused real-database and validation tests, and exact forced schema/architecture assertion maintenance; anything else is a follow-up issue, not a finding in this round.

Round cap: two per review loop, with at most two fix commits per remediation round (Q5 detailed ruling). After two unresolved rounds, report the bounded blocker; do not add rounds silently or weaken the bar. Follow-up filing is via Rigger under charter: Critical/High, broken behavior or user request; otherwise record `noted, no ticket`. Basis: grill-with-docs Loop Discipline; charter:16-38,85-92; PRODUCT sections 2-3; Q1-Q4.

## Files and boundaries

New implementation files, for Phase B only:

- `src/LamuFlix.Core/Ports/IMovieFeaturesRepository.cs`
- `src/LamuFlix.Core/Ports/MovieFeaturesSnapshot.cs`
- `src/LamuFlix.Infrastructure/Persistence/Records/MovieFeaturesRecord.cs`
- `src/LamuFlix.Infrastructure/Persistence/Configurations/MovieFeaturesConfiguration.cs`
- `src/LamuFlix.Infrastructure/Persistence/EfMovieFeaturesRepository.cs`
- One scaffolded `AddMovieFeatures` migration and generated designer under `src/LamuFlix.Infrastructure/Persistence/Migrations/`.
- Focused `EfMovieFeaturesRepositoryTests` in IntegrationTests, and snapshot validation/model/property tests in existing test projects.
- New-port ADR `docs/adr/0024-movie-features-storage-port.md`, published Accepted in Phase B with collision recon at pickup; Q7 below carries the Phase A amendment proposal.

Approved existing-file changes, narrowly for the decided work:

- `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs`
- `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`
- EF-generated `src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs`
- `tests/LamuFlix.IntegrationTests/MigrationTests.cs`: schema table list, expected migration count/order/latest, migration lifecycle and movie_features schema/index assertions.
- `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs` and `PostgresFixtureTests.cs`: replace obsolete no-vector assertions with precise new-schema checks; preserve fixed historical migration-range assertions.
- `tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs`.
- `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:28-29`: add `|IMovieFeaturesRepository` to `PortWhitelistPattern`; enforced by `Core_ports_must_match_the_constitution_whitelist` (:152-175), within the existing `dotnet test` gate. No harness/gate/threshold change (recon-DEV-413-2:30-41; Q7).

No new dependency/project/top-level architecture layer, extra schema, HTTP/API/OpenAPI/web changes, embedding generation, worker orchestration, ranking/retrieval, benchmarks/tuning, LocalPlay, secret handling or unrelated refactor. No ticket contradiction or constitution departure has been ruled necessary; owner checkboxes: none.

## Task ordering for Quill

1. Draft spec from ticket and Q1-Q5; bind each AC to proof and carry the loop policy verbatim.
2. Use the settled ADR slot and whitelist evidence in recon-DEV-413-2:16-41 and Q7; carry the exact constitution amendment proposal and Phase B publication/whitelist ordering below. These factual drafting prerequisites are closed.
3. Record the new-port ADR before implementation; define Core snapshot/port and validation contract.
4. Define entity, EF mapping, context/DI integration; scaffold the additive migration, complete its raw-SQL index operation, and keep designer/snapshot generation consistent.
5. Implement parameterized atomic repository read/upsert and guards, then focused real-database, unit/model/property proof and the exact forced existing assertion edits.
6. Run appropriate backend verification in Phase B; perform full required review axes at their scheduled stage and enforce the two-round cap.

This ordering introduces no separate abstraction or caller behavior. Quill owns spec.md, plan.md and tasks.md. Keel checks those artifacts read-only against this brief after drafting; plan challenge, freeze and provisional Gate 1 follow separately.

## Gate expectations and handoff

Spec workspace: documentation work only; no production or test writes and no gate runs. Phase B requires applicable build/analyzers/complexity/InspectCode/tests/property/security/format/mutation/architecture checks per harness configuration; never lower thresholds, count a skipped gate as PASS, or use Stryker since from a worktree. Testcontainers supplies actual PostgreSQL/pgvector proof. Web is outside scope. Missing/unrunnable mandatory verification blocks closure. No verification success is claimed here.

Vocabulary: `MovieFeaturesSnapshot` denotes the stored embedding plus computation instant; existing recommendation `MovieFeatures` remains its existing structured recommendation type. These are implementation contract names, not a new business-domain glossary term. No glossary rewrite is proposed. The required new-port ADR is allocated as `docs/adr/0024-movie-features-storage-port.md`; Q7 carries its proposal and publication requirements. Spec generation must preserve existing vocabulary and ADR decisions.

Open evidence needs for this closeout: none. ADR allocation and the exact architecture whitelist are settled by recon-DEV-413-2 and Q7. Phase B must recheck ADR 0024 availability on main during drift recon because other worktrees were outside recon scope (:49). No new measurement or cross-worktree search is authorized here.


## Q6 - Shared understanding and closure

ACCEPT: Patron ratified this brief with zero decision corrections and zero owner checkboxes. Grill complete; full exchange and cited ruling are in CONCLUSIONS.md. Bounded factual recon prerequisites above remain for dependent drafting. No plan freeze, Gate 1 or verification success is claimed.


## Q7 - Settled ADR, whitelist and constitution plan

ACCEPT: Patron confirmed the full cited Q7 ruling preserved append-only in CONCLUSIONS.md. This closes the factual prerequisites carried in Q6; its ratification and Q1-Q5 decisions remain intact. Basis: recon-DEV-413-2:16-41,49; constitution Principle I :136-141, PR gates :372,375, Documentation Rules :499-501 and Governance :533-555; task-pipeline:74,77-81.

- ADR: `docs/adr/0024-movie-features-storage-port.md`, mandatory on this M ticket because the constitution requires an ADR for every new port. The L-ticket drafting step is not an exemption. Quill must carry the proposal and task ordering from this brief; challenger plan review includes the ADR decision alongside plan.md. If a separate Phase A ADR draft is subsequently assigned, use `specs/DEV-413/adr-0024-movie-features-storage-port.md` with Status Proposed. Publish the canonical ADR with Status Accepted in Phase B, after bounded pickup recon confirms 0024 remains available on main. No separate ADR draft is created by this closeout.
- Exact proposed constitution amendment: in Principle I, add `IMovieFeaturesRepository` immediately after `IPlaybackHistory` in the closed named-port enumeration, retaining the BCL/M.E.AI abstractions and the existing sentence `Adding a port requires an ADR.` Retain all other boundaries and enforcement. Rationale: this ticket requires the specific shared storage port already decided in Q1; existing ports and callers need no behavior migration. Impact: the new port/adapter and the matching architecture whitelist addition only.
- Amendment procedure and timing: Phase B must submit this written proposal and ADR in the same build PR as the port, update the constitution port list, top Sync Impact Report, Version and Last Amended, and review `.specify/templates/*.md` for required follow-up before merge. Patron settled a MINOR bump from 1.3.0 to 1.4.0 against the current baseline; if pickup recon finds a changed baseline, stop for version reconciliation rather than overwriting it. Last Amended uses the actual amendment date. Template review is required; no template edit is currently indicated, and its no-change conclusion must be evidenced rather than claimed now. The code compliance plan is the exact whitelist addition with the new port. This closeout does not edit the constitution.
- Exact whitelist plan: add `|IMovieFeaturesRepository` to `PortWhitelistPattern` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:28-29`, preserving every existing name, anchors and assertion. Enforcing test: `Core_ports_must_match_the_constitution_whitelist` (:152-175), run within `dotnet test`. Do not change its anti-vacuity floor, harness.yml, gate scripts or thresholds. This existing-file edit is approved by Patron under section 2.3 #6; no architecture test is edited or run in Phase A.
- Phase B ordering: pickup drift/ADR collision recon; publish the ADR and synchronize the constitution through its amendment procedure; add the port and its matching whitelist entry as one coherent change before architecture verification; complete the previously approved implementation and focused proof; verify constitution/ADR/whitelist consistency and all applicable gates before the build PR may merge. The port implementation cannot merge ahead of the amendment and ADR.
- This uses the constitution's existing extension mechanism and is not a constitution departure. Ticket scope is unchanged; owner checkboxes remain zero (`blocked: structural = none`). The constitution is an additional, specifically authorized Phase B existing-file change; it does not authorize broader edits.

Phase 2 step 1 closeout is complete. Quill drafting remains unstarted by this closeout. Plan freeze, Gate 1, implementation and verification remain later stages; no success at those stages is claimed.