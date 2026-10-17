# DEV-346 Grill brief

## Status and authority

Phase 2 step 1 grill closed by Patron at Q7. This is the decision handoff to Quill for specification, plan and tasks; it is not plan freeze, Gate 1, implementation, or a gate receipt. No owner checkbox is open under Q1-Q7.

- Task: DEV-346, size:M, parent DEV-288; task note DEV-346:9,15.
- Worktree: F:/Dev/LamuFlix.worktrees/feature-346-spec; branch feature/346-spec.
- Recon base: 7bc803be582161094e3ec2dc00aab24e53647ebc.
- Fact sources: recon-DEV-346 and recon-DEV-346-2 only. Pin again through Wisp at Phase B pickup; do not treat this base as a later-head receipt.
- Full decision exchanges and cited Patron rulings: CONCLUSIONS.md Q1-Q7. Taste assumptions: ASSUMPTIONS.md.
- Ticket text is authoritative. ADR-0012 and constitution technology table already select pgvector; Q2 resolves Epic 9 sequencing as infrastructure groundwork. Q4 supersedes DEV-325 image choice for the Compose postgres image line only, preserving historical specs.

## Outcome and frozen scope

Enable pgvector infrastructure in existing PostgreSQL persistence: compatible centrally pinned packages, identical immutable test/runtime image, provider-owned vector configuration, and a fresh/upgrade-safe extension-only migration. Existing library behavior and database tables remain intact. No recommendation behavior, stored embeddings, movie_features schema, or vector entity/column is delivered.

Frozen scope: Q1-Q6 package/project wiring; three image sites; runtime/design-time/test UseVector options; model extension declaration; additive generated EnableVector migration; forced contract/wiring edits and real-Postgres proof tests. Exclusions: recommendations and Ollama behavior, embeddings storage, movie_features/DEV-413, API/OpenAPI/generated TypeScript/web, Features:LocalPlay, secrets, Process.Start, new dependencies beyond the two approved packages, new projects/layers/abstractions, published migration edits, historical spec rewrites including DEV-325, PRODUCT edits, and unrelated cleanup; anything else is a follow-up issue, not a finding in this round.

## Settled grill answers

1. **Q1 — Migration interpretation.** Preserve published Initial and AddMovieWriters migrations. First migration means first vector-enablement migration before DEV-413. Add extension-only migration. Unchanged suites means existing behavior remains passing; update forced migration-count/name assertions and add real-Postgres extension proof. No structural checkbox.
2. **Q2 — Sequencing.** Ticket-authorized groundwork proceeds under ADR-0012 despite PRODUCT Epic 9 behavior deferral. Deliver no recommendation behavior, stored embeddings or movie_features, and do not rewrite PRODUCT. No constitution departure or owner checkbox.
3. **Q3 — Packages.** Directory.Packages.props pins Pgvector 0.3.2 and Pgvector.EntityFrameworkCore 0.3.0. Keep EF Core 10.0.13 and Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3. Every project directly referencing Pgvector.EntityFrameworkCore must directly reference Npgsql.EntityFrameworkCore.PostgreSQL to avoid resolving 9.0.1/NU1608. Add both vector packages in Infrastructure and Tests.Common; UnitTests/IntegrationTests receive direct references only for APIs they consume, with the direct provider invariant. Do not use unpublished 10.0.0. Recon-2 A/C verifies published net8 package EF10 runtime/design-time compatibility, not just version ranges.
4. **Q4 — Images.** Both fixtures and only the postgres image line in docker-compose.yml use the exact pin below. Existing credentials, volumes, ports, healthcheck and RabbitMQ remain as-is. This is forced dependency integration authorized by the ticket, not owner escalation; historical DEV-325 artifacts remain intact.
5. **Q5 — Wiring.** UseNpgsql(connectionString, o => o.UseVector()) at runtime, design time and connection-bearing tests. The EF plugin configures driver UseVector and provider-owned lifetime. No manual NpgsqlDataSourceBuilder or abstraction. Connectionless model-only callers use UseNpgsql(o => o.UseVector()) and remain connectionless. Model declares HasPostgresExtension("vector"). Apply forced narrow edits at all retained option-build sites in recon-2 C.5.
6. **Q6 — Migration/proof.** Scaffold EnableVector via EF tools, generating migration/designer/snapshot; no hand-edited generated files or published-history edits. Use default non-cascading Down, no custom CASCADE. Prove fresh migration, AddMovieWriters upgrade, bounded Down/reapply, preserved table catalog/data and no pending model drift. Prove tableless parameterized Pgvector.Vector roundtrip through runtime/shared factory paths and design-time SQL/options parity. Use existing test homes and real Testcontainers PostgreSQL.
7. **Q7 — Closure.** Patron ratified Q1-Q6, loop terms and Phase B order below; no new FsCheck tests, optional Gherkin, glossary or ADR. All applicable gates and mutation remain mandatory with existing thresholds. Property-test script still runs on existing suites; report its outcome as-is, without harness changes or a presumed pass.

Exact image pin (all three sites):

`pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d`

Recon-2 B verified PostgreSQL 17.11/vector 0.8.7, manifest digest, Testcontainers 4.15.0 digest handling, Compose parsing and a runtime extension probe. It is a Debian image; no Alpine pgvector variant was found. Preserve existing runtime environment-variable secret handling; do not introduce credentials into artifacts.

## Planned files and edit boundaries

Paths are relative to the named worktree. The following implementation surface is approved; Phase A writes only this ticket planning artifacts.

| File or bounded set | Authorized change |
|---|---|
| Directory.Packages.props | Two approved vector package pins; existing EF/provider versions unchanged |
| src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj | Direct approved package references, retain direct provider reference |
| tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj | Direct approved package references, retain direct provider reference |
| tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj | References required by retained direct UseVector/API calls; provider invariant |
| tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj | References required by retained direct UseVector/Vector proof calls; provider invariant |
| docker-compose.yml | postgres image line only |
| tests/LamuFlix.Tests.Common/ContainerFixture.cs | PostgreSQL image pin only; retain RabbitMQ behavior |
| tests/LamuFlix.Tests.Common/PostgresFixture.cs | PostgreSQL image pin only |
| src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs | Runtime provider-owned UseVector options |
| src/LamuFlix.Infrastructure/Persistence/LamuFlixDesignTimeDbContextFactory.cs | Matching design-time UseVector options; existing environment contract |
| src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs | HasPostgresExtension("vector") model declaration |
| src/LamuFlix.Infrastructure/Persistence/Migrations/ | New tool-generated EnableVector migration/designer and regenerated model snapshot only |
| tests/LamuFlix.Tests.Common/LamuFlixDbContextFactory.cs | OpenContext and CreateAdminContext vector options |
| tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs | Retained direct UseNpgsql option consistency only |
| tests/LamuFlix.IntegrationTests/EfMovieRepositoryClaimConcurrencyTests.cs | Retained direct UseNpgsql option consistency only |
| tests/LamuFlix.IntegrationTests/EfMovieCatalogPagingTests.cs | Retained direct UseNpgsql option consistency only |
| tests/LamuFlix.IntegrationTests/MovieCatalogRegistrationTests.cs | Retained direct UseNpgsql option consistency only |
| tests/LamuFlix.UnitTests/Persistence/MovieModelTests.cs | Connectionless model-only vector options |
| tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs | Connectionless model-only vector options |
| tests/LamuFlix.UnitTests/Persistence/EfMovieRepositorySurvivorTests.cs | Retained direct UseNpgsql option consistency only |
| tests/LamuFlix.UnitTests/Persistence/EfMovieRepositoryConstructorTests.cs | Retained direct UseNpgsql option consistency only |
| tests/LamuFlix.IntegrationTests/MigrationTests.cs | Forced migration contract update; fresh/upgrade/Down-reapply/catalog/model-drift proofs |
| tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs | Runtime vector registration and design-time parity proof |
| tests/LamuFlix.IntegrationTests/PostgresFixtureTests.cs | Shared factory vector registration proof |

No deletion or full-file rewrite is approved. Do not add a new helper layer to consolidate the narrow option edits. Existing legacy LamuFlix.Test behavior remains, except the explicitly named ContainerFixture image. New unexpected callers or necessary changes outside this surface return through recon and a Patron ruling.

## Test strategy and gate expectations

Use the repository assertion/mocking conventions and real PostgreSQL containers for extension/mapping semantics; never mock driver translation/serialization. No persistent vector test table or domain vector mapping is needed for a parameterized vector SQL roundtrip.

- A genuinely empty database migrates to latest and has a vector pg_extension row, the expected ordered migration list, existing public tables and HasPendingModelChanges=false.
- A database at AddMovieWriters upgrades to EnableVector with existing rows/catalog preserved. Exercise default Down back to AddMovieWriters and reapply without introducing CASCADE or altering historical migration semantics. Do not invent an extension-drop guarantee beyond the provider-generated Down behavior.
- Runtime DI and shared test-factory contexts roundtrip a parameterized Pgvector.Vector with no permanent test table, catching missing driver registration. Verify design-time provider/model options and migration SQL parity through tooling.
- Run existing unit, integration and legacy ContainerFixture-dependent suites. Only the authorized US2 fixture image-pin edits, Q1/Q5 forced edits and the approved new proofs alter tests; no unrelated expected-behavior changes.
- Restore/build must remain warning-clean with direct provider references. Confirm the same full image pin in both fixtures and Compose and preserve Compose environment/volume/port/healthcheck/RabbitMQ settings.
- Gauge runs required Roslyn, complexity, InspectCode, mutation when applicable, property-test, vulnerability, format and full test gates; web gate reports its actual scope verdict. Read live harness configuration for thresholds; never change it to pass. Run required later-head gates after rebase per pipeline.
- No new FsCheck test is required: infrastructure configuration introduces no pure domain invariant. Run run-property-tests.ps1 against existing suites and retain its exact outcome; Q7 is not a waiver of gate-result handling. Exit 1/Could not run blocks. Distinguish PASS from scope-empty SKIPPED, harness-disabled SKIP, and mutation N/A per live pipeline; never infer a pass or an opt-out from this brief.

## Task ordering and handoff

1. Quill drafts spec.md, plan.md and tasks.md from this brief and append-only conclusions. Needs-decision questions return to Keel; missing facts return through Conductor to Wisp.
2. Keel runs read-only cross-artifact analysis; challenge/adjudication, plan freeze and Patron provisional Gate 1 follow independently. Rigger handles the ticket-only spec PR. User merge opens Gate 1. No production work or commits are authorized by this grill receipt.
3. Phase B pickup: Wisp pins current base/drift and Gauge measures baseline. Any changed migration baseline or new caller returns through the decision process.
4. Implement approved package/image changes, then consistent runtime/design-time/test options and model declaration. Keep change phases coherent; use existing architecture.
5. Scaffold EnableVector and generated companion files through EF tools. Verify extension-only scope; unexpected schema operations stop for decision/recon rather than being silently included.
6. Add real-Postgres proofs and forced test updates; run the existing suites.
7. Cog refactors the full implementation diff; Gauge runs required gates, then Sentry/Ledger/Compass all review. Remediation and exact-head hosted merge bar remain separate stages; no seat merges.

## Loop discipline

Closing bar: unresolved Critical, High or Medium findings block closure; Low findings are recorded and non-blocking. Required failed or unrunnable gates remain blockers independently of finding severity.

Frozen scope is the Q1-Q6 surface and exclusions above; anything else is a follow-up issue, not a finding in this round.

Round cap: maximum two review rounds and two fix commits per round. Unresolved blockers remain blocked at the cap; no third round, fix commit, threshold reduction or implicit waiver. All three review axes are required for this size:M ticket. A review round is complete only when findings/summary are on the PR per pipeline.

## Documentation readiness

No new domain terms or ADR were needed. Existing glossary vocabulary and accepted ADR-0012 are ready for Quill to use with CONCLUSIONS.md; drafting must not contradict them. EnableVector is recorded as an assumed naming choice. Q1-Q7 are the complete settled grill exchanges; this brief adds no new product or architecture decision.

## Q8 — Plan challenge adjudication and frozen clarifications

[from Patron] Approved: ratify the bounded Phase 2 step 6 plan-challenge corrections for Sentry M1/L1/L2, Ledger L1/L2, and Compass L1/L2 with no scope expansion, new tests, file-boundary changes, constitution deviation, or owner checkbox; append this ruling in `CONCLUSIONS.md` and `brief.md` before updating `plan.md` and `tasks.md`, with Gate 1 remaining separate.

- **Migration privilege precondition & design-time env handling (`findings-DEV-346-Sentry:8-64`, `findings-DEV-346-Ledger:31-44`, `findings-DEV-346-Compass:30-32`, `DEV-346:15`, `CONCLUSIONS.md` Q1/Q4/Q5/Q6/Q7)**: Document in `plan.md` that the migration connection must hold privilege to execute `CREATE EXTENSION IF NOT EXISTS vector` when absent (satisfied by fixture bootstrap superusers and `docker-compose.yml:5` `POSTGRES_USER`, while least-privilege roles require out-of-band pre-provisioning; no role provisioning, privilege automation, managed-host support, or negative test added); require a session-only, never-committed/never-printed `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` in `T026` before scaffolding (`LamuFlixDesignTimeDbContextFactory.cs:14-19`, Charter §2.3 secrets rule); and keep `T030` in `PostgresCollection` with `finally` restoration on the verified premise that no concurrent collection reads/writes that variable or factory, with Phase B recon (`T001`) checking and stopping for a ruling if any new consumer exists (`aaron-csharp-testing.mdc:22`).
- **Package CLI conformance & US2 test-source boundary alignment (`findings-DEV-346-Ledger:13-29`, `findings-DEV-346-Compass:26-28`, `CONCLUSIONS.md` Q3/Q4)**: Specify `dotnet add package` in `T004`–`T008` with the exact Q3 pins (`Pgvector 0.3.2`, `Pgvector.EntityFrameworkCore 0.3.0`) and direct `Npgsql.EntityFrameworkCore.PostgreSQL` (`10.0.3`) reference invariant rather than manual project edits (`.specify/memory/constitution.md` Coding Conventions, `aaron-dotnet-sdk-dependency-management.mdc:32-33`), and include the authorized US2 fixture image-pin edits (`ContainerFixture.cs`, `PostgresFixture.cs`, `T010`–`T011`) in `T032` (`tasks.md:116`) and `brief.md`.
- **Patron authority, artifact order & Gate 1 separation (`specs/PRODUCT.md:36-48`, Charter §2.3, `CONCLUSIONS.md` Q7)**: All five corrections are bounded plan/task clarifications within Patron's authority and the frozen Q1–Q7 scope/file boundary (neither a ticket contradiction nor a constitution departure); record this ruling in `specs/DEV-346/CONCLUSIONS.md` and `specs/DEV-346/brief.md` before `plan.md`/`tasks.md` edits, while Gate 1 (`gate1: provisional` in `spec.md`) remains a separate step after the edits and verification close.


Execution clarifications: T004-T008 use the package CLI sequentially because it may update the shared Directory.Packages.props; remove their parallel markers. T004 establishes the two central pins through the Infrastructure package commands and T005 verifies those resulting references. Preserve all Q3 versions and reference placement. T001 rechecks the process-global environment-reader/writer premise through Wisp; T030 stays in PostgresCollection and restores the prior value in finally. The existing suite test-source boundary includes the authorized US2 image-pin edits in ContainerFixture.cs and PostgresFixture.cs, alongside Q1/Q5 forced edits and T029-T031 proofs.

Adjudication round 1: accept all seven reported findings as five corrections (the three environment-isolation findings are duplicates). The optional least-privilege negative probe is not adopted; Q8 closes M1 through the documented precondition. No scope, test or file-boundary expansion. No production edits, gate result, or Gate 1 approval is implied.

Plan-challenge closure: round 1 corrections verified against all three axis reports; no unresolved findings remain. Plan frozen at Q1-Q8 scope for Patron Gate 1 provisional consideration. Gate 1 approval, spec PR publication and user merge remain separate; no implementation or gate execution authorized by this receipt.
