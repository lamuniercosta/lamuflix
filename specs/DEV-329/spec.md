# Feature Specification: DEV-329 Stage EnrichmentRequested in transactional outbox with movie write

**Feature Branch**: `feature/329-spec`

**Created**: 2026-10-25

**Status**: gate1: provisional

**Input**: Ticket DEV-329 (size M, parent DEV-100, backend-only, no ui tag); OPEN grill brief `specs/DEV-329/brief.md` (Q1–Q13 including Q6a settled; brief not frozen); facts from notes `DEV-329`, `recon-DEV-329` through `recon-DEV-329-5`, and the recorded Q7 Gauge baseline refresh in task note DEV-329; Patron rulings in `specs/DEV-329/CONCLUSIONS.md`; `[assumed]` receipt `specs/DEV-329/ASSUMPTIONS.md`; design authority `docs/adr/0027-transactional-outbox-enrichment.md` at cited revision `cb6c6a780d77f41b32d69689f237bdce43d2562d` (Q2). No decisions beyond `brief.md`.

## Domain vocabulary *(mandatory — feature-local operational terms)*

Canonical state terminology for this ticket (brief Q12). These disambiguate ADR "unstaged" language without editing root `CONTEXT.md`:

- **staged**: Tracked in-memory by `IEnrichmentOutbox.Stage` before `SaveChangesAsync`.
- **durable**: Committed in Postgres with the movie write.
- **unprocessed**: `ProcessedAt IS NULL`. Direct enqueue never marks a row processed.

Consumed vocabulary (already in use; not redefined): **EnrichmentRequested** (`MovieId`, `Attempt`), **path A** (EfMovieRepository no-metadata save path), **direct enqueue** (existing `IEnrichmentQueue` publish after successful save).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Stage EnrichmentRequested before path-A save, then retain direct enqueue (Priority: P1)

When ImportMovieFolder or RequestEnrichment succeeds on the shared scoped DbContext path A, the handler stages a typed EnrichmentRequested once before SaveChangesAsync, the shared save makes the movie write and outbox row durable together, and direct enqueue still runs once after successful save with the identical MovieId/Attempt. Stage remains in-memory only until that shared save. Direct enqueue does not mark ProcessedAt. No relay runs in this ticket. Other EnrichmentRequested enqueue paths and FeaturesRequested are untouched.

**Why this priority**: Ticket scope DEV-329:27-29 and brief Q3/Q6 require stage-before-save plus retained direct enqueue for the two named handlers only.

**Independent Test**: Existing handler unit suites prove Stage → SaveChangesAsync → EnqueueAsync exactly once with identical request; EnrichmentOutboxPersistenceTests prove no pre-save DB row and atomic path-A commit for both handler flows (brief Q8).

**Acceptance Scenarios**:

1. **Given** a successful ImportMovieFolder path-A flow, **When** the handler runs, **Then** it calls `Stage(EnrichmentRequested)` once before `SaveChangesAsync`, then direct-enqueues the same MovieId/Attempt once after save succeeds; existing import metrics/status semantics remain.
2. **Given** a successful RequestEnrichment path-A flow, **When** the handler runs, **Then** the same Stage → Save → enqueue ordering holds with identical request identity; existing enrichment status/metric semantics remain.
3. **Given** Stage has been called and before the shared SaveChangesAsync is invoked, **When** a fresh DbContext observes Postgres, **Then** no outbox row is durable yet (Stage has no save/commit/broker side effect). Observe only that pre-invocation window; do not assert absence during or after an in-flight save. Both-or-neither durability and uncertain-completion semantics remain as in User Story 2 / brief Q6a.
4. **Given** a successful save that staged EnrichmentRequested, **When** the outbox row is read, **Then** ProcessedAt is null (unprocessed); direct enqueue did not mark or delete the row.
5. **Given** MatchMovieImdbId, RequeueStrandedMovies, or FeaturesRequested enqueue sites, **When** this ticket ships, **Then** those paths are unchanged (no staging extension).

---

### User Story 2 - Atomic durability and failure boundaries (Priority: P2)

Movie write and outbox insert are atomic (both-or-neither). Stage/serialization failure prevents save and enqueue. Save failure or cancellation propagates, skips direct enqueue, and disposes the failed scope without replay. Deterministic pre-commit failures and explicit rollbacks leave zero durable movie/outbox changes for the attempted write; for RequestEnrichment the pre-existing movie remains in its original state with no new outbox row. Uncertain save completion is not treated as proof of rollback. Post-commit direct publish failure propagates while retaining the committed movie and unprocessed outbox. No Stage/handler retry and no new transaction coordinator. Metadata paths B/C remain unsupported for staging (noted, no ticket; pickup guard).

**Why this priority**: Brief Q6/Q6a fix the failure contract that makes same-transaction staging correct without inventing recovery (DEV-420/sweeper owns eventual delivery).

**Independent Test**: Unit negative guards plus EnrichmentOutboxPersistenceTests for controlled insert rollback, ambient path-A rollback, and post-commit broker failure retention (brief Q8).

**Acceptance Scenarios**:

1. **Given** Stage throws (including serialization failure), **When** the handler runs, **Then** SaveChangesAsync and direct enqueue do not run; no durable movie/outbox change from that attempt.
2. **Given** a deterministic pre-commit save failure or explicit ambient rollback on path A, **When** observed from a fresh Testcontainers context, **Then** neither the attempted movie write nor a new outbox row is durable; RequestEnrichment leaves the pre-existing movie unchanged.
3. **Given** any SaveChangesAsync exception or cancellation, **When** the handler exits, **Then** the exception propagates, direct enqueue is skipped, and the failed request scope is disposed without reuse/replay.
4. **Given** movie and outbox already committed, **When** direct enqueue throws, **Then** the error propagates and both the committed movie and unprocessed outbox row remain.
5. **Given** uncertain save completion, **When** interpreting the failure, **Then** the system does not claim the exception proves rollback and does not invent a provider-specific failure taxonomy; eventual recovery remains DEV-420/sweeper.

---

### User Story 3 - Four-column outbox schema, wire-compatible payload, scoped DI, architecture (Priority: P3)

Postgres gains only `outbox_messages` with id (bigint identity PK), payload (bytea NOT NULL), created_at (timestamptz NOT NULL from injected TimeProvider at Stage), processed_at (timestamptz NULL), plus partial pending index `(created_at, id) WHERE processed_at IS NULL`. Stage serializes typed EnrichmentRequested once with adapter-local System.Text.Json Web defaults into lossless UTF-8 payload bytes (no envelope/type/version; no MovieId reshaping). Scoped EfEnrichmentOutbox shares the movie repository DbContext. Architecture whitelist allows only the new IEnrichmentOutbox port. Exactly one EF-tool-generated AddEnrichmentOutbox migration pair and snapshot; never hand-edit generated files.

**Why this priority**: Brief Q4–Q5 and Q7–Q8 lock the delegated schema, wire parity for future DEV-420 PublishRawAsync reuse, and DI/architecture proofs without relay work here.

**Independent Test**: EnrichmentOutboxPersistenceTests for byte parity/round-trip/Attempt/CreatedAt/ProcessedAt/repeated staging; MigrationTests for empty/upgrade/down/types/nullability/index/no other-table delta; PersistenceCompositionTests for scoped shared context; ArchitectureTests for the single new port (brief Q8).

**Acceptance Scenarios**:

1. **Given** empty DB, upgrade, and down migration paths, **When** MigrationTests run, **Then** only the four-column `outbox_messages` table and pending partial index appear as this ticket's schema delta; other tables are unchanged.
2. **Given** a staged EnrichmentRequested, **When** payload bytes are compared to publisher Web-default serialization of the same typed request and round-tripped with consumer defaults, **Then** bytes match, Attempt is preserved, and nested MovieId representation is not asserted beyond that round-trip (converter details outside recon roots).
3. **Given** injected fixed TimeProvider, **When** Stage runs, **Then** CreatedAt equals `TimeProvider.GetUtcNow()` assigned once at Stage; ProcessedAt starts null.
4. **Given** repeated Stage calls for the same logical request, **When** saved, **Then** distinct outbox rows are created (no movie/attempt uniqueness).
5. **Given** one request scope, **When** PersistenceCompositionTests resolve the outbox adapter and repository, **Then** they share one LamuFlixDbContext instance; cross-scope instances differ.
6. **Given** ArchitectureTests PortWhitelistPattern, **When** IEnrichmentOutbox is added, **Then** only that new port is permitted; Core remains EF-free.

### Edge Cases

- Supported save path is EfMovieRepository path A only for the two ticket handlers; paths B/C clear Added rows outside identityMap and are unsupported for staging — noted, no ticket, with pickup guard before any metadata-diverging caller uses Stage (brief Q6).
- Failed scopes are disposed, never reused or retried (brief Q6).
- No publisher/consumer/relay edits; PublishRawAsync remains the future reuse point for stored bytes (recon-DEV-329-5:3; brief Q4).
- HandlerRegistration.cs stays unchanged because AddHandler already registers Scoped concrete/contract handlers (recon-DEV-329-5:23-36; brief Q7).
- EfMovieRepository production code is not edited (brief Q6–Q7).
- Public API shape, web, OpenAPI, and packages stay unchanged (brief Q7, Q10).
- Phase B STOP: pickup verifies landed DEV-419 authority and main drift; do not treat unmerged cited branch object `cb6c6a780d77f41b32d69689f237bdce43d2562d` as landed (brief Q2, Q9).
- Q7 Gauge baseline refresh measured 14 existing files (exit 0 / 0 findings each for Roslyn, complexity, InspectCode); 7 Q7 net-new files recorded absent until created (task note DEV-329:125-171). Baseline is measurement only, not a gate pass (brief Q10).
- No Gherkin/Reqnroll; no new ADR authored by this ticket (brief Q9).
- Round cap [assumed]: maximum two numbered full plan-challenge rounds and two full code-review/ship-review rounds (ASSUMPTIONS.md Q13).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Deliver `IEnrichmentOutbox` in existing `LamuFlix.Core.Ports` with `void Stage(EnrichmentRequested request)`. Stage is synchronous and in-memory only: no CancellationToken, SaveChangesAsync, commit, broker call, or retry inside Stage (brief Q3; DEV-329:28).
- **FR-002**: Implement scoped Infrastructure adapter `EfEnrichmentOutbox` sharing the movie repository `LamuFlixDbContext` per request scope (brief Q3; recon-DEV-329-5:23-43).
- **FR-003**: `ImportMovieFolderCommandHandler` and `RequestEnrichmentCommandHandler` call Stage once before their shared path-A `SaveChangesAsync` and retain direct `IEnrichmentQueue` enqueue after successful save with identical MovieId/Attempt (brief Q3, Q6; DEV-329:27-29).
- **FR-004**: Serialize typed EnrichmentRequested once in Stage using adapter-local `JsonSerializerOptions(JsonSerializerDefaults.Web)` into immutable UTF-8 bytes stored losslessly as `bytea` Payload; no envelope, schema-version, type field, hand-built JSON, or flattened MovieId. Serialization errors propagate before save/enqueue. No publisher/consumer/relay edits (brief Q4).
- **FR-005**: Schema is only `outbox_messages`: `id` bigint generated identity PK; `payload` bytea NOT NULL; `created_at` timestamptz NOT NULL from injected `TimeProvider.GetUtcNow()` at Stage; `processed_at` timestamptz NULL; partial index `(created_at, id) WHERE processed_at IS NULL`. No other columns, FK, uniqueness, claim/retry/error metadata, or other-table changes. Repeated staging creates distinct rows. Direct enqueue neither marks nor deletes rows (brief Q5).
- **FR-006**: Bind staging to path A for the two named handlers only. Stage once, save once, enqueue once after successful save. Apply Q6a atomic both-or-neither invariant and precise failure assertions (brief Q6, Q6a). Do not edit EfMovieRepository production code.
- **FR-007**: Exact Q7 delivery file set only (plus Phase A `specs/DEV-329/` artifacts). Exactly one EF-tool-generated `AddEnrichmentOutbox` migration pair and snapshot; never hand-edit generated files. EF timestamp is tooling-supplied at implementation, not guessed in Phase A. Outside delivery: HandlerRegistration.cs, EfMovieRepository.cs, broker/consumer, other handlers, API shape, web/, Directory.Packages.props. Additional files need a new cited Patron ruling (brief Q7).
- **FR-008**: Test strategy per brief Q8: extend the two existing handler unit suites; real Postgres Testcontainers in new `EnrichmentOutboxPersistenceTests`; extend MigrationTests, PersistenceCompositionTests, ArchitectureTests; adapt existing constructors/fixtures within Q7. Existing libraries only; no driver mocks for real translation/serialization; no speculative failure taxonomy, relay tests, or path B/C support tests.
- **FR-009**: Dependency ordering per brief Q9: (1) Gauge Q7 baseline refresh already recorded; Phase B pickup verifies landed DEV-419 and main drift (STOP if missing); (2) after Gate 1 and Phase B pickup, implement port/record/config/context/migration/adapter/DI with schema/wire/scoping/no-pre-save tests; (3) wire both handlers plus orchestration tests and all approved fixture adaptations as one buildable unit; (4) complete real-DB atomicity/rollback/broker-failure, migration, composition, and architecture proofs before full verification and review. No Gherkin/Reqnroll, new ADR, relay, metadata-retry refactor, or web work.
- **FR-010**: Gate expectations per brief Q10: Gauge refresh is baseline only. Phase A uses read-only speckit-analyze and brief-vs-plan/tasks consistency (Keel); challenge/freeze/Gate 1 remain distinct. Phase B runs configured backend build/format, exact-changed-file Roslyn/complexity/InspectCode, unit/integration/architecture/full `dotnet test`, property tests, vulnerable packages, and pre-PR mutation via worktree-valid invocation without `--since`. Live harness thresholds only; never hardcode, lower, or hand-edit generated gate config. Report generated-file exclusions, executed PASS, SKIPPED (scope-empty), SKIP, N/A, and Could not run separately; build alone is no substitute. No web/OpenAPI gates for unchanged API shape.
- **FR-011**: Loop discipline per brief Q11–Q13: verified in-scope Critical/High/Medium findings block closure; Low/informational recorded with disposition. All three axes (Sentry Risk, Ledger Standards, Compass Spec) required for M, pinned to current HEAD and agreed scope, with concrete file:line/scenario evidence. Missing axis → `blocked: missing axis <name>`. Round cap [assumed]: max two numbered full plan-challenge rounds and two full code-review/ship-review rounds; at cap, report blocked with exact evidence and stop (ASSUMPTIONS.md).
- **FR-012**: Frozen scope wording for eventual freeze (brief Q12; not frozen now): DEV-329 delivers only typed EnrichmentRequested staging for ImportMovieFolder and RequestEnrichment before their shared path-A save; four-column outbox_messages with generated EF migration and pending index; scoped EF adapter and lossless Web-default payload; retained post-save direct enqueue; the Q7 exact files and Q8 tests proving wire compatibility, atomicity, schema, DI and architecture. Exclude relay/retention execution, direct-publish removal, other enqueue paths, metadata-save compatibility, new dependencies/layers, public API/web changes and other tables; metadata B/C is noted, no ticket with the pickup guard. anything else is a follow-up issue, not a finding in this round.
- **FR-013**: Charter care-list: no new NuGet/npm dependency; no new project/top-level folder/architectural layer; schema limited to ticket-named outbox_messages; no public API shape change; no Features:LocalPlay/secrets/Process.Start; Q7 file boundary and forced test/DI/architecture adaptations under §2.3 item 6. Port whitelist extension for ticket-named IEnrichmentOutbox is explicitly approved (brief Q3). No owner structural checkbox. No ticket-text contradiction.

### Key Entities

- **IEnrichmentOutbox**: Core port exposing synchronous `Stage(EnrichmentRequested)`.
- **EnrichmentRequested**: Existing typed broker payload (`MovieId`, `Attempt`) stored as Web-default JSON UTF-8 bytes.
- **OutboxMessageRecord**: Infrastructure record mapped to `outbox_messages` (Id, Payload, CreatedAt, ProcessedAt).
- **EfEnrichmentOutbox**: Scoped adapter that serializes and tracks Added outbox rows on the shared DbContext.
- **Path A**: EfMovieRepository no-metadata save path that persists shared-context Added rows without ChangeTracker.Clear retry rebuild.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Both named handlers stage before save and retain one direct enqueue after successful save with identical request identity.
- **SC-002**: Fresh-context Testcontainers observations show both-or-neither durability for controlled pre-commit failures and explicit rollbacks; RequestEnrichment rollback preserves the pre-existing movie.
- **SC-003**: Stored payload bytes match publisher Web-default serialization and round-trip with consumer defaults preserving Attempt.
- **SC-004**: Migration empty/upgrade/down proves only the four-column table and pending index; ProcessedAt starts null; CreatedAt follows injected TimeProvider; repeated Stage yields distinct rows.
- **SC-005**: Composition proves scoped outbox shares DbContext with the repository; ArchitectureTests allow only the new port.
- **SC-006**: Post-commit broker failure retains committed movie and unprocessed outbox; no relay or ProcessedAt marking occurs in this ticket.

## Assumptions

- Q13 round cap is Patron-approved `[assumed]` (maximum two plan-challenge rounds and two code-review/ship-review rounds) as recorded in `ASSUMPTIONS.md` and brief Q13.
- Nested MovieId JSON converter details remain outside recon roots; wire proofs use typed serialize/deserialize parity rather than asserting converter internals (recon-DEV-329-4:42; brief Q4).
- Cited ADR-0027 revision clears grill authority (Q2) but Phase B pickup must still verify landing on main and drift; absence or mismatch is STOP, not permission to invent substitutes.
- Metadata paths B/C incompatibility remains noted, no ticket (brief Q6); not a newly requested follow-up issue.
- Brief remains OPEN / NOT FROZEN at drafting time; freeze and Gate 1 are separate workflow steps.
