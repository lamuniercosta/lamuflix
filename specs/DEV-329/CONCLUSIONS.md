
---

## Q1 — DEV-419 design authority prerequisite (2026-10-25)

Status: blocked pending scoped recon; grill open; brief not frozen.

Identity: `F:/Dev/LamuFlix.worktrees/feature-329-spec`, branch `feature/329-spec`, HEAD `bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3`.

### Keel question (full exchange)

[from Keel] DEV-329 grill Q1 — prerequisite authority. Worktree F:/Dev/LamuFlix.worktrees/feature-329-spec, branch feature/329-spec, HEAD bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3. Task note DEV-329:27-29 explicitly fixes staging design by DEV-419 ADR; recon-DEV-329:24,83 reports that ADR absent in scoped roots. New IEnrichmentOutbox and outbox schema artifacts are also absent, as expected for new work. Recommendation: treat DEV-419 design content as required recon before choosing schema, Stage signature/serialization, transaction/retry semantics, or closing the grill; retain all explicit ticket scope without substitutes. Cost: grill and brief freeze wait for the authoritative contract. Do you rule that the missing DEV-419 ADR blocks design decisions and grill closure pending scoped recon? Please cite basis. We will append the full exchange to specs/DEV-329/CONCLUSIONS.md. Reply using maestri ask "Keel" with your ruling.

### Patron answer (verbatim)

[from Patron] DEV-329 Q1 ruling: Approved — missing DEV-419 outbox ADR blocks design decisions (outbox_messages schema, IEnrichmentOutbox.Stage signature/serialization, and transaction/retry semantics) and grill closure pending scoped recon (`needs recon` via Conductor).
- Ticket authority (`specs/PRODUCT.md:23-24`, `lamuflix-team-charter` §2.3, `DEV-329:27-29`): the ticket fixes staging design by outbox ADR DEV-419 and names `DEV-419` as a dependency; retain all explicit ticket scope (`outbox_messages` schema + EF migration, `IEnrichmentOutbox.Stage`, staging before `SaveChangesAsync` in `ImportMovieFolder` and `RequestEnrichment`, direct enqueue retained, no relay) without speculative substitutes (`specs/PRODUCT.md:18`).
- Missing evidence (`recon-DEV-329:24,83`, `task-pipeline` §2.2 Recon belongs to Wisp): `DEV-419` ADR is absent at HEAD `bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3` (`docs/adr/ADR-0005.md:24,31` only notes an outbox needs its own ADR); route `needs recon` through Conductor to supply the authoritative DEV-419 contract before freezing `brief.md` or closing the grill.

### Recorded rationale and implications

The ticket names DEV-419 as design authority; absence of that authority is an evidence gap, not permission to invent a contract or amend ticket scope. Request scoped recon through Bernstein and stop the grill here. This is a recon prerequisite, not an owner structural checkbox.

No outbox schema, Stage contract, serialization, or transaction/retry design is settled. No implementation, spec/plan/tasks drafting, plan freeze, or Gate 1 clearance follows from Q1. The new Core.Ports interface flagged by Wisp still needs an explicit cited Patron ruling after the prerequisite is supplied. Closing bar, round cap, exact file set, test strategy, gate expectations, and task-order constraints remain to be grilled. Gauge measured 14 existing ticket files; that baseline is a measurement only and must be rerun by Gauge if the decided file set changes (task note DEV-329:60-99).

Next unanswered question: needs recon: supply the authoritative DEV-419 outbox ADR contract, with bounded source references, covering outbox_messages schema, IEnrichmentOutbox.Stage signature/serialization, and transaction/retry semantics for the two ticket-named handlers.


---

## Q2 — Authority prerequisite after settled Q1 (2026-10-25)

Status: Q1 authority prerequisite cleared for continued grill; brief freeze and implementation remain closed.

Identity: `F:/Dev/LamuFlix.worktrees/feature-329-spec`, branch `feature/329-spec`, HEAD `bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3`.

### Keel question (full exchange)

[from Keel] DEV-329 grill Q2 — authority prerequisite after settled Q1. Resume in F:/Dev/LamuFlix.worktrees/feature-329-spec, feature/329-spec, HEAD bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3. Q1 remains recorded in specs/DEV-329/CONCLUSIONS.md; brief.md does not yet exist. recon-DEV-329-3:14-18,20-26,51 verifies the DEV-419 comment-cited adopted docs/adr/0027-transactional-outbox-enrichment.md at cb6c6a780d77f41b32d69689f237bdce43d2562d, blob a000f752bae0d20425f6a52c0673b5906dca5de8, available without fetch at the cited remote branch tip; Accepted header, Ticket DEV-329. This is a correction revision; initial adoption commit and main integration were not established, and earlier not-found results still hold for their checked roots. ADR :15 fixes same Postgres/same transaction, ProcessedAt after settled broker confirm and no sent mark on uncertain outcomes; :17,21-22 fixes at-least-once, one Api relay and retention. Per recon-DEV-329-3:28-47, table columns/indexes are unspecified and delegated to DEV-329/420 at ADR :23; Stage signature/serialization and handler/DbContext/retry boundary are absent. Recommendation: this supplies the Q1 missing-authority prerequisite for design discussion; keep its fixed outcomes authoritative, and explicitly decide the delegated implementation contracts here without asserting main integration or an invented ADR contract. Ticket DEV-329:27-29 still fixes outbox_messages, IEnrichmentOutbox.Stage, two handlers stage before save, direct enqueue retained, no relay, unit/integration tests. Cost: each remaining implementation decision needs its own cited ruling and later pickup must verify prerequisite landing. Do you rule that this evidence clears Q1 for continued grill while leaving freeze and implementation closed? Please cite basis and reply with maestri ask "Keel".

### Patron answer (verbatim)

[from Patron] DEV-329 Q2 ruling: Approved — `recon-DEV-329-3` clears the Q1 authority prerequisite for continued grill while leaving `brief.md` freeze and implementation closed until the delegated implementation contracts are explicitly ruled.
- Adopted ADR authority (`recon-DEV-329-3:14-18,20-26,51`, `docs/adr/0027-transactional-outbox-enrichment.md:15,17,21-23` at `cb6c6a780d77f41b32d69689f237bdce43d2562d` / blob `a000f752bae0d20425f6a52c0673b5906dca5de8`): supplies the `DEV-419` outbox design authority fixing same-Postgres / same-transaction staging, `ProcessedAt` after settled broker confirmation (no sent mark on uncertain outcomes), at-least-once delivery, one Api relay, and retention, without claiming `main` integration (`lamuflix-team-charter` §2.3).
- Delegated implementation contracts (`docs/adr/0027-transactional-outbox-enrichment.md:23`, `recon-DEV-329-3:28-47`, `DEV-329:27-29`, `lamuflix-team-charter` §2.3 items 2-3): `outbox_messages` columns/indexes, `IEnrichmentOutbox.Stage` signature/serialization, and handler/`DbContext`/retry boundaries are explicitly delegated to `DEV-329`/`DEV-420` and must each receive an explicit cited ruling in this grill before freezing `brief.md`.
- Ticket scope and prerequisite boundary (`DEV-329:27-29`, `specs/PRODUCT.md:18,23-24`): retain `outbox_messages` schema + EF migration, `IEnrichmentOutbox.Stage`, staging before `SaveChangesAsync` in `ImportMovieFolder` and `RequestEnrichment`, direct enqueue retained, no relay, and unit/integration tests; later implementation pickup must verify prerequisite landing.

---

## Q3 — Typed Stage port and unit-of-work boundary (2026-10-25)

Status: approved; grill open; brief not frozen.

### Keel question (full exchange)

[from Keel] DEV-329 grill Q3 — port and Stage boundary. Q2 is read back in CONCLUSIONS.md and captured in the new OPEN, NOT FROZEN brief.md. Ticket DEV-329:28 explicitly names IEnrichmentOutbox.Stage; recon-DEV-329:31,35-36,68-75 supplies existing EnrichmentRequested(MovieId,int Attempt), Core.Ports whitelist and EF-free Core boundary. ADR at cb6c6a78 :15 mandates atomicity but defines no interface (recon-DEV-329-3:34-42). Recommendation: approve the ticket-named port in existing LamuFlix.Core.Ports, with void Stage(EnrichmentRequested request), an Infrastructure persistence adapter, and scoped DI sharing the movie repository DbContext. Stage only prepares/adds an outbox row to the current unit of work; no DB save, transaction commit, broker call, or retry in Stage; no cancellation parameter for this synchronous in-memory operation. Handlers call Stage before their existing SaveChangesAsync and retain enqueue after successful save. No generic outbox abstraction, new dependency/project/layer, or support for FeaturesRequested. Serialization/schema and save/retry correctness will be separate questions. Cost: a new adapter/DI binding and the explicit port whitelist extension, plus tests proving Stage has no persistence side effect until the shared save. Do you approve this precise port contract and placement, citing the ticket and existing architecture? Please reply with maestri ask "Keel" and leave CONCLUSIONS.md recording to Keel to avoid concurrent writes.

### Patron answer (verbatim)

[from Patron] DEV-329 Q3 ruling: Approved — define `IEnrichmentOutbox` in `LamuFlix.Core.Ports` with `void Stage(EnrichmentRequested request)` and an `Infrastructure` persistence adapter registered as scoped, sharing the movie repository `DbContext`.
- Ticket authority and port boundary (`DEV-329:28`, `recon-DEV-329:31,35-36,68-75`, `lamuflix-team-charter` §2.3 item 2): `DEV-329:28` explicitly names `IEnrichmentOutbox.Stage`; placing `IEnrichmentOutbox` in `LamuFlix.Core.Ports` over existing `EnrichmentRequested(MovieId, int Attempt)` extends the Core.Ports whitelist for the ticket-named port while preserving the EF-free Core boundary with no new project, layer, or generic outbox abstraction (`specs/PRODUCT.md:18`).
- Unit-of-work and synchronous staging contract (`docs/adr/0027-transactional-outbox-enrichment.md:15` at `cb6c6a780d77f41b32d69689f237bdce43d2562d`, `DEV-329:27-29`, `recon-DEV-329-3:34-42`): `Stage` is a synchronous in-memory unit-of-work operation (no `CancellationToken`, no `SaveChangesAsync`, no transaction commit, no broker call, no retry in `Stage`); `ImportMovieFolder` and `RequestEnrichment` call `Stage` before their shared `SaveChangesAsync`, retain direct enqueue after save, and do not add `FeaturesRequested` support.

### Recorded rationale and implications

The ticket-named port extends the existing architecture. Stage has a synchronous, typed, EF-free Core signature and a scoped Infrastructure implementation sharing the repository DbContext. It tracks work without persisting or publishing it; the existing save owns persistence and direct enqueue follows success. No generic outbox abstraction, new layer, or FeaturesRequested staging is authorized. The whitelist change is an explicit ticket-port extension, not a threshold reduction. Schema, serialization, and failure/retry correctness remain separate unresolved decisions.

### Next unanswered question / recon prerequisite

Q4 — payload/schema and save/retry correctness. needs recon: provide exact existing EnrichmentRequested broker serialization/deserialization contract (serializer settings, payload shape and reusable infrastructure conventions), plus the full EfMovieRepository save/retry/ChangeTracker.Clear flow showing whether a staged Added outbox row survives each retry path, and the concrete shared-DbContext DI lifetimes for both ticket-named handler paths. Bound this to the named worktree at the recorded HEAD; supply file:line slices and existing tests. Current recon-DEV-329:31-36,59-69,91-94 identifies the surfaces but does not give these contracts or retry reconstruction details. Do not infer them from the relay ADR, which explicitly omits serialization and handler/DbContext/retry details (recon-DEV-329-3:34-47).

Grill stops pending that evidence. No spec/plan/tasks drafting, freeze, gates, or implementation follows.
---

## Q4 — Stored payload contract (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q4 — stored payload contract. Evidence: recon-DEV-329-4:16-43 and recon-DEV-329-5:3 cite RabbitMqEnrichmentQueuePublisher.cs:17,155-156 serializing the typed EnrichmentRequested with JsonSerializerDefaults.Web to UTF-8 bytes, EnrichmentConsumer.cs:36,335-346 reading that same type/options, and existing PublishRawAsync at publisher:75-85. MovieId converter details were outside recon roots; do not assert its nested representation. Recommendation: Stage serializes the existing typed request with System.Text.Json Web defaults into immutable-at-staging UTF-8 bytes, stored losslessly as bytea Payload; serialize once at Stage, never hand-build JSON or flatten MovieId, and add no body envelope/schema-version/type fields. Future DEV-420 can use existing raw-publish with the existing requested topology; no publisher/consumer or relay edits here. Adapter-local options match current defaults; avoid a shared serializer refactor outside this ticket. Stage serialization errors propagate before save, so no movie/outbox commit or direct enqueue. Unit compatibility tests must compare stored bytes with serialization of the same typed request using current publisher defaults, round-trip with consumer defaults, and prove input Attempt is preserved. Cost: binary payload is less convenient for SQL inspection, but preserves the exact existing body and avoids JSONB normalization or a new wire contract. Approve this payload/serialization decision with cited basis? Reply via maestri ask "Keel"; Keel alone records files. No freeze, draft specs or gates.

### Patron answer (verbatim)

[from Patron] DEV-329 Q4 ruling: Approved — `Stage` serializes the typed `EnrichmentRequested` once at staging using adapter-local `System.Text.Json` `JsonSerializerDefaults.Web` into immutable UTF-8 bytes stored losslessly as `bytea` `Payload`, with no body envelope, schema-version, type field, hand-built JSON, or flattened `MovieId`.
- Existing broker wire compatibility (`recon-DEV-329-4:16-43`, `recon-DEV-329-5:3`, `RabbitMqEnrichmentQueuePublisher.cs:17,75-85,155-156`, `EnrichmentConsumer.cs:36,335-346`, `docs/adr/0027-transactional-outbox-enrichment.md:15,23` at `cb6c6a780d77f41b32d69689f237bdce43d2562d`): storing the exact `JsonSerializerDefaults.Web` UTF-8 bytes as `bytea` preserves byte-for-byte wire parity with `RabbitMqEnrichmentQueuePublisher` and `EnrichmentConsumer` without JSONB normalization or asserting unverified nested `MovieId` converter details, enabling future `DEV-420` relay publishing via existing `PublishRawAsync` without editing publisher, consumer, or relay in `DEV-329`.
- Scope discipline and failure semantics (`DEV-329:27-29`, `specs/PRODUCT.md:18`, `lamuflix-team-charter` §2.3 items 3,6): adapter-local `JsonSerializerOptions(JsonSerializerDefaults.Web)` avoids rewriting unnamed serializer/publisher files; any serialization exception in `Stage` propagates before `SaveChangesAsync` so neither movie/outbox commit nor direct enqueue occurs; unit tests must verify byte parity with publisher defaults, round-trip deserialization with consumer defaults, and preservation of `Attempt`.

### Rationale and implications

Q4 approved: serialize the existing typed EnrichmentRequested once in Stage using adapter-local System.Text.Json Web defaults; store lossless UTF-8 Payload as bytea, no envelope/type/version field or MovieId reshaping. No publisher/consumer/relay edits. Serialization errors precede save and enqueue. Verify byte parity, consumer-default round trip, and Attempt preservation. Basis: recon-DEV-329-4:16-43; recon-DEV-329-5:3; publisher:17,75-85,155-156; consumer:36,335-346; ADR-0027:15,23; DEV-329:27-29; charter section 2.3 items 3,6. Full exchange: CONCLUSIONS.md Q4.

Grill remains open; next Q5 schema. No freeze, spec drafting or gates.


---

## Q5 — Minimal outbox schema (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q5 — minimal table schema. Q4 payload ruling is appended/read back. Ticket DEV-329:28 names outbox_messages schema and EF migration; ADR-0027 at cb6c6a78:15,22-23 fixes ProcessedAt and retained processed rows but delegates columns/indexes (recon-DEV-329-3:28-32). Recommendation: only table outbox_messages, with Id -> id bigint generated identity primary key, Payload -> payload bytea NOT NULL (Q4 bytes), CreatedAt -> created_at timestamp with time zone NOT NULL assigned once at Stage using injected TimeProvider.GetUtcNow(), and ProcessedAt -> processed_at timestamp with time zone NULL initially. One pending-row index (created_at,id) WHERE processed_at IS NULL for future stable oldest-first scans; no ordering/delivery guarantee inferred. No movie FK or duplicated MovieId/Attempt columns: typed payload carries them and retained messages must not cascade away with movie changes. No uniqueness by movie/attempt, sent boolean, retry counters, error/claim fields, message-type/version envelope, broker config, retention worker or relay. Direct enqueue success never marks ProcessedAt in this transition: only future confirmed relay processing may mark it, and rows are not deleted here. Cost: DEV-420 may need a later schema migration if it deliberately chooses additional operational fields; this table serves only EnrichmentRequested. Test empty DB/upgrade/down migration, column nullability/types, pending index, staged timestamp and null ProcessedAt, distinct messages for repeated staging, and no other-table changes. Approve this complete delegated schema with cited basis? Reply via maestri ask "Keel"; Keel records files.

### Patron answer (verbatim)

[from Patron] DEV-329 Q5 ruling: Approved — `outbox_messages` schema and EF migration define only `id` (`bigint` generated identity PK), `payload` (`bytea NOT NULL`), `created_at` (`timestamp with time zone NOT NULL` from injected `TimeProvider.GetUtcNow()` at `Stage`), and `processed_at` (`timestamp with time zone NULL`), with one partial pending-row index on `(created_at, id) WHERE processed_at IS NULL` and no other table changes.
- Ticket and ADR schema authority (`DEV-329:28`, `docs/adr/0027-transactional-outbox-enrichment.md:15,21-23` at `cb6c6a780d77f41b32d69689f237bdce43d2562d`, `recon-DEV-329-3:28-32`, `lamuflix-team-charter` §2.3 item 3): satisfies the delegated `outbox_messages` contract with `ProcessedAt` nullable initially and retained processed rows, while `TimeProvider` obeys `AGENTS.md` non-negotiables (`DateTime.Now` banned) and the partial index `(created_at, id) WHERE processed_at IS NULL` supports future relay polling without implying ordering or delivery guarantees.
- Minimalism and transition semantics (`DEV-329:27-29`, `specs/PRODUCT.md:18`, `docs/adr/0027-transactional-outbox-enrichment.md:15,17,22`): omit movie FK, duplicated `MovieId`/`Attempt` columns, uniqueness constraints on movie/attempt, sent booleans, retry/claim/error columns, and envelope/type fields so retained outbox rows never cascade with movie changes and repeated enrichment requests stage distinct rows; direct enqueue in `DEV-329` leaves `processed_at` `NULL` and never deletes rows, and tests must verify empty-DB/upgrade/down migrations, column types/nullability, partial index, `TimeProvider` timestamp, null `ProcessedAt`, repeated staging, and zero changes to other tables.

### Rationale and implications

Q5 approved: only outbox_messages with id bigint generated identity PK; payload bytea NOT NULL; created_at timestamptz NOT NULL assigned once by injected TimeProvider.GetUtcNow() at Stage; processed_at timestamptz NULL. Partial pending index (created_at,id) WHERE processed_at IS NULL. No other columns, FK, movie/attempt uniqueness, table changes, claim/retry/error metadata, relay or retention worker. Repeated staging creates distinct rows. Direct enqueue neither marks nor deletes rows. DEV-420 owns confirmed processing and retention behavior. Verify migration empty/upgrade/down, types/nullability/index, fixed-clock timestamp, initially-null ProcessedAt and repeated staging. Basis: DEV-329:27-29; ADR-0027:15,17,21-23; recon-DEV-329-3:28-32; charter section 2.3 item 3; AGENTS.md time rule; specs/PRODUCT.md:18.

Grill open; next Q6 save boundary and failure behavior.


---

## Q6 — Save boundary and failure policy (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q6 — supported save boundary and failures. recon-DEV-329-4:47-66 proves both named handlers select path A (metadata unchanged): shared db.SaveChangesAsync persists movie and Added outbox together, no repository retry/Clear. Paths B/C detach unrelated Added rows at RebuildTrackingAsync:297 even on successful metadata save; same-scope metadata divergence therefore cannot be called supported outbox behavior. recon-DEV-329-5:23-43 closes scoped concrete/contract/repository/DbContext lifetimes. Recommendation: DEV-329 uses existing path A only for its two named handler invocations, with no additional same-scope metadata mutation or handler replay. Stage once before save, save once, enqueue once only after save succeeds. Stage/serialization failure -> no save or enqueue; save failure/cancellation -> no direct enqueue and no partial committed movie/outbox (fresh-context DB assertions); uncommitted Added rows can remain tracked until failed request scope disposal, do not reuse/retry that failed scope. Direct publish failure after commit still propagates, with committed movie plus unprocessed outbox retained. Stage and handlers add no retry or new transaction coordinator. Prove shared-context no pre-save DB row, atomic commit, insert-side failure rollback, ambient rollback for path A, and post-commit publish failure retaining both rows using Testcontainers. Do not edit EfMovieRepository metadata retry logic here; record its B/C incompatibility as a follow-up and pickup guard: future staging users or handler metadata divergence require a new explicit design/test decision. Cost: port support is limited to ticket paths, not a promise of outbox compatibility across repository metadata paths; retained row alone has no relay until DEV-420. Approve this bounded failure/retry contract and follow-up disposition, or rule a ticket-required correction? Cite basis and reply via maestri ask "Keel"; Keel records.

### Patron answer (verbatim)

[from Patron] DEV-329 Q6 ruling: Approved — `DEV-329` binds outbox staging strictly to existing `EfMovieRepository` path A for the two named handlers (`ImportMovieFolder` and `RequestEnrichment`), with no edit to `EfMovieRepository` metadata retry/rebuild logic and with paths B/C recorded in `CONCLUSIONS.md` (`noted, no ticket`) as a pickup guard for any future metadata-diverging caller.
- Path A atomicity and failure contract (`recon-DEV-329-4:47-66`, `recon-DEV-329-5:23-43`, `docs/adr/0027-transactional-outbox-enrichment.md:15` at `cb6c6a780d77f41b32d69689f237bdce43d2562d`, `DEV-329:27-29`): both ticket-named handlers execute path A (metadata unchanged, single shared `db.SaveChangesAsync`), staging once before save, saving once, and direct-enqueuing once only after save succeeds; `Stage`/serialization failure prevents save and enqueue, save failure/cancellation prevents direct enqueue and commits neither movie nor outbox (verified via fresh `DbContext` on Testcontainers; failed request scopes are disposed, never reused/retried), and post-commit direct publish failure propagates while retaining both committed movie and unprocessed outbox rows.
- Scope boundary and follow-up disposition (`DEV-329:27-29`, `specs/PRODUCT.md:18`, `lamuflix-team-charter` §2.3 item 6): `EfMovieRepository` paths B/C (`RebuildTrackingAsync:297` clearing tracked entities) are not exercised by the ticket's two handlers, so rewriting `EfMovieRepository` is out of scope (`noted, no ticket` in `CONCLUSIONS.md`; any future caller combining outbox staging with same-scope metadata divergence requires an explicit design/test ruling).

### Rationale and implications

Q6 approved: use shared scoped DbContext path A for the two ticket handlers only. Stage once, save once, direct enqueue after successful save. Stage error prevents save/enqueue; save failure/cancellation prevents enqueue; verify no partial commit from fresh Testcontainers context. Failed scopes are disposed rather than reused/retried. Post-commit direct publish error propagates with committed movie and unprocessed outbox retained. No Stage/handler retry or new transaction coordinator, and no EfMovieRepository production edits. Metadata paths B/C clear Added rows outside identityMap and are explicitly unsupported for staging; pickup guard requires a future explicit design/test decision before any metadata-diverging caller uses Stage. Patron disposition for that caveat: noted, no ticket (not a follow-up issue request). Basis: recon-DEV-329-4:47-66; recon-DEV-329-5:23-43; ADR-0027:15; DEV-329:27-29; charter section 2.3 item 6; specs/PRODUCT.md:18.

Open clarification Q6a: distinguish known rollback from uncertain save completion; do not assume every thrown save proves no durable commit.


---

## Q6a — Atomicity and uncertain save completion (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q6a — tighten the save-failure guarantee before tests. Q6 is recorded verbatim, including noted/no-ticket B/C guard. Our current wording says any save failure/cancellation commits neither row. That is stronger than the ADR same-transaction invariant and should not silently promise knowledge of a durable outcome when save completion is uncertain. Recommendation: the invariant is both movie write and outbox commit together or neither; no partial commit. On any save exception/cancellation, propagate, do not directly enqueue or replay the failed scope. Assert zero durable changes for deliberately injected pre-commit failures and explicit rollbacks; if completion is uncertain, do not claim the exception proves rollback or automatically retry. A subsequent fresh-context observation may establish both-or-neither, and eventual recovery stays with DEV-420/sweeper under the existing at-least-once model. This clarifies failure observations, not a new retry/recovery feature; no new provider-specific failure taxonomy is asserted from recon. Basis: ADR-0027:15,17 at cb6c6a78 (recon-DEV-329-3:41,46); path A and failure evidence recon-DEV-329-4:49,60,66; Q6. Cost: tests distinguish known rollback from unknown completion instead of treating all exceptions as evidence of rollback. Approve this correction to Q6 guarantee, leaving its other decisions intact? Reply via maestri ask "Keel"; Keel records.

### Patron answer (verbatim)

[from Patron] DEV-329 Q6a ruling: Approved — refine the Q6 save-failure guarantee to the atomic both-or-neither invariant (never partial commit), distinguishing known pre-commit/explicit rollback from uncertain completion while keeping all other Q6 rulings intact.
- Atomic invariant vs. uncertain completion (`docs/adr/0027-transactional-outbox-enrichment.md:15,17` at `cb6c6a780d77f41b32d69689f237bdce43d2562d`, `recon-DEV-329-3:41,46`, `recon-DEV-329-4:49,60,66`): on any `SaveChangesAsync` exception or cancellation, propagate immediately, skip direct enqueue, and dispose the failed request scope without replay; movie and outbox rows commit together or neither commits (a fresh `DbContext` observation always finds both or neither), and eventual delivery when a commit succeeded before an uncertain failure remains with `DEV-420` / sweeper under at-least-once semantics.
- Test assertion precision (`DEV-329:28-29`, `AGENTS.md` Testcontainers rule): integration tests assert zero durable movie/outbox changes specifically for deterministic pre-commit failures and explicit transaction rollbacks, without claiming an arbitrary exception proves rollback or introducing a provider-specific failure taxonomy.

### Rationale and implications

Q6a approved clarification supersedes only the overstrong zero-rows-on-any-exception wording in Q6. The movie write and outbox insert are atomic (both-or-neither); uncertain completion is not proof of rollback. Any save exception/cancellation propagates, skips direct enqueue, and disposes the failed scope without replay. Tests assert zero durable changes for deterministic pre-commit failures and explicit rollbacks; for RequestEnrichment this means the pre-existing movie remains in its original state and no new outbox row, not deletion of the movie. Future recovery remains DEV-420/sweeper. No provider-specific failure taxonomy is introduced. Basis: ADR-0027:15,17; recon-DEV-329-3:41,46; recon-DEV-329-4:49,60,66; DEV-329:28-29; AGENTS.md Testcontainers rule.


---

## Q7 — Exact proposed delivery files (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q7 — exact delivery file set and allowed adaptations. Q6a is recorded and read back. Recommendation: approve only the following production/test files for the later plan, plus Phase A specs/DEV-329 decision/spec artifacts. src/LamuFlix.Core/Ports/IEnrichmentOutbox.cs; src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs; src/LamuFlix.Core/Features/Enrichment/RequestEnrichmentCommandHandler.cs; src/LamuFlix.Infrastructure/Persistence/EfEnrichmentOutbox.cs; src/LamuFlix.Infrastructure/Persistence/Records/OutboxMessageRecord.cs; src/LamuFlix.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs; src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs; src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs; src/LamuFlix.Infrastructure/Persistence/Migrations/<EF timestamp>_AddEnrichmentOutbox.cs; src/LamuFlix.Infrastructure/Persistence/Migrations/<same EF timestamp>_AddEnrichmentOutbox.Designer.cs; src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs; tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs; tests/LamuFlix.UnitTests/Features/Enrichment/RequestEnrichmentCommandHandlerTests.cs; tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs; tests/LamuFlix.IntegrationTests/ImportMovieFolderCommandHandlerPersistenceTests.cs; tests/LamuFlix.IntegrationTests/EnrichmentEndpointTests.cs; tests/LamuFlix.IntegrationTests/ImportEndpointTests.cs; tests/LamuFlix.IntegrationTests/MigrationTests.cs; tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs; tests/LamuFlix.IntegrationTests/EnrichmentOutboxPersistenceTests.cs; tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs. EF timestamp is tool-generated later: exactly one migration named AddEnrichmentOutbox with its matching generated Designer and snapshot; generate through EF tooling, never hand-edit generated files. Endpoint/repository/persistence tests need constructor or fixture adaptations for the new port (recon-DEV-329:52-69; recon-DEV-329-4:81-104). New EnrichmentOutboxPersistenceTests owns real-Postgres adapter/serialization/atomicity cases; extend existing two unit suites for handler orchestration. Keep HandlerRegistration.cs unchanged because AddHandler resolves scoped constructor dependencies (recon-DEV-329-5:23-36), and keep EfMovieRepository production code, broker/consumer, other enqueue handlers, API shape, web, packages and other tables unchanged. Composition tests prove outbox scoped registration and same-context commit. Explicitly approve these supporting test/DI edits under charter 2.3 item 6; no deletion or unrelated rewrite. Gauge baseline scope must be rerun via Conductor because this differs from its initial 14-existing-file list (DEV-329:60-99); net-new files are recorded absent until created. Cost: migration paths are exact by EF migration identity rather than guessing a future timestamp, and any additional file needs a new cited ruling/recon before changing the agreed plan. Approve this file set and generation boundary with cited basis? Reply via maestri ask "Keel"; Keel records; no freeze or drafting.

### Patron answer (verbatim)

[from Patron] DEV-329 Q7 ruling: Approved — freeze the exact 21-file production/test delivery set (plus Phase A `specs/DEV-329/` artifacts), EF migration tooling boundary, and Gauge baseline rerun requirement.
- Production and migration file set (`DEV-329:27-29`, `recon-DEV-329:31-51`, `recon-DEV-329-5:23-43`, `AGENTS.md` generated-files rule): approve `src/LamuFlix.Core/Ports/IEnrichmentOutbox.cs`, `src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs`, `src/LamuFlix.Core/Features/Enrichment/RequestEnrichmentCommandHandler.cs`, `src/LamuFlix.Infrastructure/Persistence/EfEnrichmentOutbox.cs`, `src/LamuFlix.Infrastructure/Persistence/Records/OutboxMessageRecord.cs`, `src/LamuFlix.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs`, `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs`, `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`, and exactly one EF tool-generated migration pair `<EF timestamp>_AddEnrichmentOutbox.cs` / `<same EF timestamp>_AddEnrichmentOutbox.Designer.cs` plus `LamuFlixDbContextModelSnapshot.cs` (never hand-edited).
- Forced test/DI/architecture adaptations (`lamuflix-team-charter` §2.3 item 6, `recon-DEV-329:52-75`, `recon-DEV-329-4:81-104`): approve `ImportMovieFolderCommandHandlerTests.cs`, `RequestEnrichmentCommandHandlerTests.cs`, `EfMovieRepositoryTests.cs`, `ImportMovieFolderCommandHandlerPersistenceTests.cs`, `EnrichmentEndpointTests.cs`, `ImportEndpointTests.cs`, `MigrationTests.cs`, `PersistenceCompositionTests.cs`, new `EnrichmentOutboxPersistenceTests.cs`, and `ArchitectureTests.cs` as forced by the ticket's new port, schema/DI wiring, and architecture gate; keep `HandlerRegistration.cs`, `EfMovieRepository.cs`, broker/consumer, other handlers, API contracts, `web/`, and `Directory.Packages.props` untouched.
- Gauge baseline rerun (`DEV-329:60-99`, `task-pipeline` §2.2): because this agreed file set differs from Gauge's initial 14-file baseline, route a Gauge baseline rerun via Conductor on the existing tracked files in this set (recording net-new files as absent until created), and require a new cited Patron ruling before adding any other file.

### Rationale, implications and active phase restriction

Q7 approves the exact delivery file set listed below and supporting DI/test/architecture adaptations (charter section 2.3 item 6). Exactly one EF-tool-generated AddEnrichmentOutbox migration pair and snapshot; never hand-edit generated files. HandlerRegistration, EfMovieRepository production code, broker/consumer, other handlers, API shape, web and package files stay outside delivery. Gauge must rerun baseline via Conductor on the existing files in this changed set, recording new files absent until created. Additional files need a new cited Patron ruling. Basis: DEV-329:27-29,60-99; recon-DEV-329:31-75; recon-DEV-329-4:81-104; recon-DEV-329-5:23-43; AGENTS.md generated-files rule. Patron used the word freeze in the answer, but the active Conductor instruction forbids freeze on this ask: this is an approved proposed file boundary only; no brief/plan freeze has occurred.


---

## Q8 — Test strategy (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q8 — test strategy. Q7 approved file set is recorded as proposed only: active Conductor instruction forbids freeze, so no freeze occurred despite that word in your Q7 answer. Recommendation: unit tests in the two existing handler suites verify Stage(request) -> SaveChangesAsync -> direct enqueue exactly once on success with identical MovieId/Attempt, existing guards/no scan/add failure staging, Stage throw => no save/enqueue, save throw/cancel => no enqueue, post-save queue throw => propagate. Keep existing metric/status semantics. Use real Postgres Testcontainers in EnrichmentOutboxPersistenceTests for adapter/typed wire bytes and Web-default roundtrip, preserving Attempt, fixed TimeProvider timestamp, initially null ProcessedAt, multiple Stage calls giving distinct messages, no pre-save persistence, same-context atomic save for both handler flows, controlled outbox-insert failure rolling back movie write, explicit path-A ambient rollback, and successful commit followed by broker failure retaining movie/outbox. RequestEnrichment rollback must preserve the pre-existing movie state, not assert zero total movies. MigrationTests cover empty/upgrade/down, only four column types/nullability and partial pending index, no other-table delta. PersistenceCompositionTests cover scoped adapter and same-DbContext persistence plus scope isolation; ArchitectureTests permit only the ticket port. Existing host/repository tests adapt constructor/fixtures without mocking driver translation/serialization. Use existing assertion/mocking/test libraries, no new packages. Do not invent arbitrary-exception rollback proof, automatic retry, relay behavior tests, or path B/C support tests. Cost: real DB failure injection and migration checks are slower, but establish the atomicity unit mocks cannot prove. Approve this exact test strategy with cited basis? Reply via maestri ask "Keel"; Keel records; no gates run.

### Patron answer (verbatim)

[from Patron] DEV-329 Q8 ruling: Approved — adopt the proposed unit, Testcontainers integration, migration, composition, and architecture test strategy using existing repo test libraries with no new packages (and keeping `brief.md` unfrozen).
- Handler unit orchestration (`DEV-329:28-29`, `recon-DEV-329:47-51`, `AGENTS.md` non-negotiables): `ImportMovieFolderCommandHandlerTests` and `RequestEnrichmentCommandHandlerTests` verify `Stage(request)` -> `SaveChangesAsync` -> direct enqueue once with identical `MovieId`/`Attempt`, no staging on existing guard/scan/add failures, `Stage` exception preventing save/enqueue, save exception/cancellation preventing direct enqueue, post-save queue exception propagation, and unchanged status/metric semantics using existing xUnit/mocking libraries.
- Real Postgres Testcontainers, migration, DI, and architecture proof (`DEV-329:28-29`, `AGENTS.md` Testcontainers rule, `docs/adr/0027-transactional-outbox-enrichment.md:15` at `cb6c6a780d77f41b32d69689f237bdce43d2562d`, Q4–Q7): `EnrichmentOutboxPersistenceTests` uses real Postgres Testcontainers to prove Web-default byte parity/round-trip, `Attempt` preservation, injected `TimeProvider` timestamp, null `ProcessedAt`, distinct rows across repeated `Stage` calls, no pre-save DB persistence, atomic path-A commit for both handler flows, controlled outbox-insert failure and explicit ambient rollback (with `RequestEnrichment` rollback preserving pre-existing movie state rather than asserting zero movies), and post-commit broker failure retaining movie + outbox; `MigrationTests`, `PersistenceCompositionTests`, and `ArchitectureTests` verify schema/index/down migration, scoped same-`DbContext` isolation, and the single `IEnrichmentOutbox` port extension without inventing arbitrary-exception rollback proofs, relay tests, or path B/C support tests.

### Rationale and implications

Q8 approved test strategy: extend existing handler unit suites for Stage -> Save -> enqueue exactly once with identical request, negative guards and scan/add/Stage/save/cancellation failures, post-save broker errors, existing metrics/status. Real Postgres Testcontainers in EnrichmentOutboxPersistenceTests proves Web-default payload parity/roundtrip, Attempt, injected-clock CreatedAt, null ProcessedAt, distinct repeated staging, no pre-save persistence, both handler atomic commits, controlled insert rollback and ambient rollback (request movie unchanged), and retained rows after broker error. MigrationTests proves empty/upgrade/down schema/types/nullability/pending index and no other-table delta; PersistenceCompositionTests proves scoped outbox shared context and isolation; ArchitectureTests allows only the new port. Existing fixtures/constructors adapt within Q7. Existing libraries only, no driver mocks for real translation/serialization, no speculative failure taxonomy, relay tests or B/C support tests. Basis: DEV-329:28-29; recon-DEV-329:47-69; recon-DEV-329-4:81-104; ADR-0027:15; Q4-Q7; AGENTS.md testing rules.


---

## Q9 — Brief-level dependency ordering (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q9 — dependency ordering for the later plan. Recommendation: (1) before downstream drafting/freeze, Conductor obtains Gauge baseline on Q7 changed existing-file scope, while new files remain marked absent; later Phase B pickup verifies landed DEV-419 authority and main drift, without treating the cited branch object as landed. (2) After authorized Phase A drafting/challenge/Gate 1 and Phase B pickup, implement typed port plus persistence record/configuration/DbContext and EF-generated migration/DI; pair these with schema, wire, scoped-context and no-pre-save adapter tests. (3) Wire both handlers and their unit ordering/failure tests, adapting every approved constructor/endpoint/repository fixture so the solution remains buildable. (4) Complete cross-context real-DB atomicity/rollback/broker-failure, migration, composition and architecture proof; then run later configured verification and review. Changes that must compile together stay one coherent implementation unit rather than leaving a half-wired port in a claimed completed task. No required Reqnroll/Gherkin artifact or new ADR for this M ticket; no relay, metadata retry refactor or web work. These are brief-level dependencies only: Quill still authors spec/plan/tasks when separately authorized, Keel does not draft them. Cost: baseline refresh and prerequisite/drift checks precede delivery rather than carrying stale evidence. Approve this task ordering with cited basis? Reply via maestri ask "Keel"; no freeze/drafts/gates now.

### Patron answer (verbatim)

[from Patron] DEV-329 Q9 ruling: Approved — adopt the four-stage brief-level dependency ordering while keeping `brief.md` unfrozen, leaving spec/plan/tasks drafting to Quill when separately authorized, and running no gates now.
- Pre-freeze and Phase B pickup gates (`DEV-329:27-29,60-99`, `task-pipeline` §2.2, Q2, Q7): (1) Conductor obtains Gauge's updated baseline on the Q7 existing-file scope (with net-new files marked absent) before freeze/downstream drafting, and Phase B pickup verifies landed `DEV-419` authority and `main` drift rather than treating the unmerged branch object `cb6c6a780d77f41b32d69689f237bdce43d2562d` as landed.
- Coherent compilation and proof stages (`DEV-329:27-29`, `AGENTS.md` verification and Testcontainers rules, Q3–Q8): (2) after Phase A drafting/challenge/Gate 1 and Phase B pickup, deliver `IEnrichmentOutbox`, `OutboxMessageRecord`, `OutboxMessageConfiguration`, `LamuFlixDbContext`, EF tool-generated `AddEnrichmentOutbox` migration, `EfEnrichmentOutbox`, and scoped DI paired with schema, wire, scoped-context, and no-pre-save adapter tests; (3) wire both handlers plus their unit tests and all approved constructor/endpoint/repository fixture adaptations as one buildable unit; (4) complete cross-context Testcontainers atomicity/rollback/broker-failure, migration, composition, and architecture proofs before full verification and review, with no Gherkin/Reqnroll artifacts, new ADR, relay, metadata retry refactor, or web work.

### Rationale and implications

Q9 approved dependencies: (1) Conductor obtains refreshed Gauge baseline on Q7 existing-file scope before drafting/freeze; record new files absent. Later Phase B pickup verifies landed DEV-419 and main drift, not just the available cited branch object. (2) After separately authorized Phase A drafting/challenge/Gate 1 and Phase B pickup, implement typed port, outbox record/config/context, EF-generated migration, adapter and scoped DI with schema/wire/scoping/no-pre-save tests. (3) Wire both handlers, orchestration tests and all constructor/endpoint/repository fixture adaptations as a coherent buildable unit. (4) Complete real-DB atomicity/rollback/broker-failure, migration/composition/architecture proof before full verification and review. Brief-level ordering only; Quill authors spec/plan/tasks separately. No Gherkin/Reqnroll, new ADR, relay, metadata-retry refactor or web work. Basis: DEV-329:27-29,60-99; task-pipeline section 2.2 as cited by Patron; Q2-Q8; AGENTS.md verification/Testcontainers rules.


---

## Q10 — Phase-specific gate expectations (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q10 — gate expectations by phase. Recommendation: Q7/Q9 Gauge rerun is baseline evidence only, not a gate pass, and Keel runs no gates on this ask. In Phase A after separate drafting authorization, Keel performs read-only speckit-analyze and brief-vs-plan/tasks checks; plan challenge/freeze/Gate 1 remain separate approvals, and a docs-only diff gets no fabricated C# implementation verification. In later Phase B, run configured backend build/format, Roslyn, complexity, InspectCode, appropriate unit/integration/architecture tests, full dotnet test, property tests and vulnerable packages per harness; pre-PR mutation uses scripts/run-mutation.ps1 with current harness thresholds/kill-share/file-span/exclusions and valid worktree invocation (no --since). Do not guess threshold numbers or introduce a second package/test stack. Analyzer scope must match exact changed C# paths including new files; generated migrations/snapshot are tooling output, any analyzer exclusions reported explicitly. Exit 0 passes only the gate actually run; exit 2 scope-empty => SKIPPED (scope-empty), opt-out => SKIP, mutation exclusions-only => N/A; unavailable gate => Could not run, never PASS. No threshold reduction, generated-config edits or dotnet-build substitute. Backend-only/no API-shape delta means no OpenAPI regeneration or web gate here unless a new cited ruling changes scope. Evidence is same-head and cannot clear missing three-axis review. Cost: real-DB and mutation checks remain later delivery work; this grill records expectations without asserting completion. Approve these gate expectations with cited basis? Reply via maestri ask "Keel"; no gates/freeze now.

### Patron answer (verbatim)

[from Patron] DEV-329 Q10 ruling: Approved — adopt the phase-separated gate expectations while running no gates now and keeping `brief.md` unfrozen.
- Phase A vs. Phase B separation (`task-pipeline` §2.2, `AGENTS.md` verification commands, Q7, Q9): Gauge's Q7/Q9 rerun is baseline evidence only (never a gate pass), and Phase A uses read-only `speckit-analyze` plus `brief.md`-vs-`plan.md`/`tasks.md` consistency checks after separate drafting authorization without fabricating C# gate verdicts on a docs-only diff; plan challenge, `brief.md` freeze, and `gate1: provisional` remain distinct steps.
- Phase B backend gate and reporting rules (`AGENTS.md` non-negotiables & verification commands, `lamuflix-team-charter` §2.3): Phase B runs configured backend build/format, `run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1` (scoped to exact changed C# paths including new files, with EF tool-generated migration/snapshot exclusions reported explicitly), unit/integration/architecture tests, `dotnet test`, `run-property-tests.ps1`, `run-vulnerable-packages.ps1`, and pre-PR `run-mutation.ps1` (worktree-valid invocation without `--since`, enforcing `harness.yml` thresholds, `maxKillShare`, `maxKillFileSpan`, and exclusions without hardcoding or lowering thresholds); report exit 0 as `PASSED` only for executed gates, exit 2 as `SKIPPED (scope-empty)` / `SKIP` / `N/A`, and unrunnable gates as `Could not run` (never `PASS` or plain `dotnet build`), with no OpenAPI/web gates for this backend-only scope and same-HEAD evidence never substituting for three-axis review.

### Rationale and implications

Q10 approved gate expectations: Gauge refresh is baseline only. Separately authorized Phase A drafting is followed by read-only speckit-analyze and brief-vs-plan/tasks consistency review; challenge, brief/plan freeze and Gate 1 remain distinct. Later Phase B uses configured backend build/format, exact-changed-file Roslyn/complexity/InspectCode, unit/integration/architecture/full dotnet test, property tests, vulnerable packages and pre-PR mutation through a worktree-valid invocation without --since. Use live harness thresholds/kill-share/file-span/exclusions; do not hardcode, lower or hand-edit generated gate config. Report generated-file exclusions, actual executed pass, exit-2 scope-empty SKIPPED (scope-empty), opt-out SKIP, mutation exclusions-only N/A and unavailable gate Could not run separately; build alone is no substitute. No web/OpenAPI gates or regeneration for current unchanged API shape. All evidence is same-head and cannot replace three-axis review. Basis: AGENTS.md non-negotiables/verification; task-pipeline section 2.2 and charter section 2.3 cited by Patron; Q7,Q9. No gates executed now.


---

## Q11 — Review closing bar (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q11 — review closing bar, a decision required by grill-with-docs Loop Discipline. Recommendation: for the later code-review/ship-review cycle, Critical, High and Medium verified in-scope findings block closure; Low and informational findings are recorded with disposition but do not force a fix round. Review evidence must meet concrete file:line/scenario threshold, be pinned to current head and the agreed scope, and include all three Sentry Risk, Ledger Standards and Compass Spec reports for this M ticket; a missing axis is blocked: missing axis <name>, never a skip. Gates alone cannot supersede a retained finding or supply review clearance; review closure and merge bar remain distinct, and only user merges. Plan challenge follows the same severity bar for concrete brief/spec inconsistency without treating unrequested abstractions as blocking defects. Cost: real Medium defects must be fixed and rechecked within the later round cap; unrelated improvements stay outside the round. Do you choose this closing bar? Cite basis; reply via maestri ask "Keel". No review or gate is being run now.

### Patron answer (verbatim)

[from Patron] DEV-329 Q11 ruling: Approved — cite the standing review closing bar: verified in-scope Critical, High, and Medium findings block closure; Low and informational findings are recorded with disposition without forcing a fix round.
- Standing closing bar and three-axis review (`task-pipeline` §2.2, `lamuflix-team-charter` §2.3, `grill-with-docs` Loop Discipline): for this `size:M` ticket, review requires all three axes (Sentry Risk, Ledger Standards, Compass Spec) pinned to current HEAD and the agreed file scope with concrete `file:line`/scenario evidence; any missing axis reports `blocked: missing axis <name>` (never skipped), gates never supersede a retained finding or substitute for review clearance, and user merge remains separate from review closure.
- Plan challenge and scope discipline (`specs/PRODUCT.md:18`, `lamuflix-team-charter` §2.3): plan challenge applies the same Critical/High/Medium bar to concrete `brief.md`/`spec.md`/`plan.md`/`tasks.md` inconsistencies while rejecting unrequested abstractions or out-of-scope improvements.

### Rationale and implications

Q11 chosen closing bar: verified in-scope Critical/High/Medium findings block review closure; Low/informational findings are recorded with disposition without forcing fix rounds. All Sentry Risk, Ledger Standards and Compass Spec axes are required for M, pinned to current HEAD and agreed scope, with concrete file:line/scenario evidence. Missing axis => blocked: missing axis <name>, never skip. Gates do not supersede retained findings or review clearance; merge bar and user merge remain separate. Plan challenge uses the same severity bar for concrete brief/spec/plan/tasks inconsistencies; unrequested abstractions and unrelated improvements are not blocking findings. Basis: standing task-pipeline section 2.2, charter section 2.3, grill-with-docs Loop Discipline and specs/PRODUCT.md:18, as cited by Patron.


---

## Q12 — Scope wording and state terminology (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q12 — scope wording for the later frozen brief. Active instruction still forbids freeze now; this approves wording only. Recommendation: use this exact bounded scope: DEV-329 delivers only typed EnrichmentRequested staging for ImportMovieFolder and RequestEnrichment before their shared path-A save; four-column outbox_messages with generated EF migration and pending index; scoped EF adapter and lossless Web-default payload; retained post-save direct enqueue; the Q7 exact files and Q8 tests proving wire compatibility, atomicity, schema, DI and architecture. Exclude relay/retention execution, direct-publish removal, other enqueue paths, metadata-save compatibility, new dependencies/layers, public API/web changes and other tables; metadata B/C is noted, no ticket with the pickup guard. anything else is a follow-up issue, not a finding in this round. Canonical wording in artifacts: staged means tracked by Stage before save, durable means committed with movie write, and unprocessed means ProcessedAt is null; direct enqueue never makes an outbox row processed. This avoids carrying the ADR ambiguous unstaged wording into the Stage API contract (recon-DEV-329-3:37). No new business glossary term or root CONTEXT edit is needed for these implementation states. Cost: new file/caller/schema requirements need a new cited decision or later issue rather than expanding this round. Do you approve the scope wording and state distinction based on DEV-329:27-29, ADR:15,17,22-23 and Q3-Q8? Reply via maestri ask "Keel"; no freeze/draft now.

### Patron answer (verbatim)

[from Patron] DEV-329 Q12 ruling: Approved — adopt the proposed bounded scope wording and canonical state terminology (`staged`, `durable`, `unprocessed`) for the later `brief.md`, while keeping `brief.md` unfrozen now.
- Bounded scope wording and standing exclusion clause (`DEV-329:27-29`, `specs/PRODUCT.md:18`, `lamuflix-team-charter` §2.3, Q3–Q8): `DEV-329` delivers only typed `EnrichmentRequested` staging for `ImportMovieFolder` and `RequestEnrichment` before their shared path-A `SaveChangesAsync`, the four-column `outbox_messages` table with EF tool-generated migration and pending index, the scoped EF adapter and lossless Web-default `bytea` payload, retained post-save direct enqueue, the Q7 exact file set, and the Q8 tests; relay/retention execution, direct-publish removal, other enqueue paths, `EfMovieRepository` metadata paths B/C (`noted, no ticket` with pickup guard), new dependencies/layers, public API/web changes, and other tables are excluded, and `anything else is a follow-up issue, not a finding in this round`.
- Canonical state terminology (`docs/adr/0027-transactional-outbox-enrichment.md:15,17,22-23` at `cb6c6a780d77f41b32d69689f237bdce43d2562d`, `recon-DEV-329-3:37`): in `DEV-329` artifacts, `staged` means tracked in-memory by `Stage` before `SaveChangesAsync`, `durable` means committed in Postgres with the movie write, and `unprocessed` means `ProcessedAt IS NULL` (direct enqueue never marks a row processed), avoiding the ADR's ambiguous `unstaged` phrasing without editing root `CONTEXT.md`.

### Rationale and implications

Q12 approves the exact bounded scope below for eventual freeze only; active no-freeze instruction remains in force. Technical artifact terms: staged = in-memory tracking by Stage before SaveChangesAsync; durable = committed in Postgres with the movie write; unprocessed = ProcessedAt IS NULL. Direct enqueue never marks processed. This disambiguates ADR unstaged language without root CONTEXT.md edits. Metadata B/C remains noted, no ticket with pickup guard, not a newly requested issue. Basis: DEV-329:27-29; specs/PRODUCT.md:18; charter section 2.3; Q3-Q8; ADR-0027:15,17,22-23; recon-DEV-329-3:37.


---

## Q13 — Review round cap [assumed] (2026-10-25)

### Keel question (full exchange)

[from Keel] DEV-329 grill Q13 — round cap, last open Loop Discipline decision. Recommendation: maximum two numbered full rounds for each later plan-challenge cycle and two for the later code-review/ship-review cycle. Round 1 gathers the complete required reports and one numbered fix list; after fixes, Round 2 verifies the current-head results and closure. Changed heads, receipt/encoding/publication repairs, extra findings or a renamed review do not silently reset either cap; do not publish an additional immutable review as a repair. If a blocking finding or missing axis remains at the cap, report blocked with exact remaining evidence to Conductor and stop that cycle without pretending clearance or starting Round 3. Low/info disposition does not trigger a new fix round. Grill Q numbers are decision exchanges and do not consume review rounds. Cost: unresolved blockers at two rounds require explicit subsequent direction rather than an unattended loop. Approve this cap and exhaustion behavior, citing standing Loop Discipline/task-pipeline rules? Reply via maestri ask "Keel"; no freeze or review now.

### Patron answer (verbatim)

[from Patron] DEV-329 Q13 ruling: Approved `[assumed]` — accept Keel's recommendation: maximum 2 numbered full rounds for the plan-challenge cycle and 2 for the code-review/ship-review cycle, reporting `blocked` to Conductor if any blocking finding or missing axis remains at the cap (`grill-with-docs` Loop Discipline, `task-pipeline` §2.2, `lamuflix-team-charter` §3.1 13th-question `[assumed]` rule; `brief.md` remains unfrozen).

### Rationale and implications

Q13 approved [assumed]: maximum two numbered full plan-challenge rounds and two full code-review/ship-review rounds. The answer accepts the full recommendation: Round 1 complete reports/fix list, Round 2 current-head verification; no silent reset for changed HEAD, renamed review, receipt/encoding/publication repair or new findings, and no extra immutable review to repair publication. If any blocking finding or missing axis remains at the cap, report blocked with exact evidence to Conductor and stop that cycle; do not claim clearance or start Round 3. Low/info disposition does not add a fix round; grill decision Q numbers do not consume review rounds. Basis: grill-with-docs Loop Discipline; task-pipeline section 2.2; charter section 3.1 13th-question assumed rule, as cited by Patron. Also recorded in ASSUMPTIONS.md. Brief remains unfrozen.


---

## Plan Challenge Round 1 Adjudication Ruling (2026-10-25)

Identity: `F:/Dev/LamuFlix.worktrees/feature-329-spec`, branch `feature/329-spec`, HEAD `bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3`.

### Keel request (summary)

Adjudicate `DEV-329` (`size:M`) plan challenge Round 1 across all three axes (Sentry Risk, Ledger Standards, Compass Spec) at `bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3` with a 2-item above-bar Quill fix list and recorded dispositions for all Low/Informational findings.

### Patron ruling (verbatim)

[from Patron] DEV-329 Plan Challenge Round 1 adjudication ruling: Approved — accept the exact two-item above-bar Quill fix list (Ledger F1 Medium; Sentry finding 1 Medium / Ledger F3 Low) and record all remaining Low/Info findings as non-blocking without forced edits, scope expansion, or owner checkboxes.
- Above-bar Round 1 fix list (`specs/DEV-329/CONCLUSIONS.md` Q8, Q9, Q10, Q11; `specs/DEV-329/tasks.md:65,95-97`): (1) **Ledger F1 (Medium)** — update `T012` in `specs/DEV-329/tasks.md:65` to explicitly require `RequestEnrichmentCommandHandlerTests` to assert no `Stage`, `SaveChangesAsync`, or `EnqueueAsync` on both `RequestEnrichment` negative paths (missing movie and guard/state rejection) within the approved Q7 unit test file, with no `spec.md` expansion; (2) **Sentry 1 (Medium) / Ledger F3 (Low duplicate)** — keep task IDs/order (`T017`–`T019`) intact and update `T019` (plus any contradictory Phase 5 prose) to require running configured backend build/format and full `dotnet test` (unit/integration/architecture) first after `T018` Cog `/refactor` on the final changed HEAD before the listed Gauge analyzer/property/vulnerability/mutation gates and review checkpoint.
- Non-blocking Low/Info dispositions (`specs/DEV-329/CONCLUSIONS.md` Q7–Q12, `AGENTS.md` Stryker & Testcontainers rules, `lamuflix-team-charter` §2.3): record **Ledger F2** (existing `T001` STOP intent suffices), **Ledger F4** (honor `AGENTS.md` static-init rule in Phase B without pre-deciding serializer options lifetime now), **Ledger F5** (`T004`/`T010` prove `TimeProvider` resolution within Q7 DI scope without new recon), **Ledger F6** (real-DB rollback proof required without prescribing a specific EF interceptor API; `T002`/`T016` coverage overlap is benign), **Ledger F7 & Compass L2** (bounded-root constitution line citations remain unverified without expanding roots; Gauge baseline refresh is file-set freshness measurement only, not a gate pass), **Compass L1** (retain Q7 boundary; route any future out-of-scope fixture need to a new cited ruling), **Compass L3** (`TreatWarningsAsErrors` remains descriptive, not a new gate claim), and **Compass I1** (task-note Gauge refresh governs current state without rewriting historical `brief.md` baseline prose).
- No escalation (`lamuflix-team-charter` §2.3): no ticket contradiction or constitution departure exists; no owner structural checkbox is opened, and `brief.md` freeze and `gate1: provisional` remain closed pending Round 2 verification of Quill's two edits.

---

## Keel separate Phase A freeze receipt (2026-10-25)

Conductor authorized the separate freeze after Round 2 closure, task note DEV-329:191. Keel freezes the settled brief and current spec/plan/tasks; the authoritative current status and SHA-256 input receipt are appended in brief.md under Keel Phase A freeze. Earlier no-freeze wording and the full Patron exchanges remain historical and unchanged. This is execution of the existing Q7–Q13 decisions, not a new Patron ruling or a change to scope.

Both Medium blockers are closed on current artifacts: T012 negative-path no-Stage/save/enqueue assertions and post-T018 final changed-HEAD build/format/full tests before T019 gates/review. All Q11 Low/Info dispositions stand. Q13 [assumed] is preserved unchanged (plan challenge Round 2 of 2 complete; no extra round). Phase B STOP still requires landed DEV-419 and pickup drift clearance. No owner structural checkbox, Gate 1, implementation or merge-bar clearance; no tests/gates, commit, PR or main refresh.

Identity: F:/Dev/LamuFlix.worktrees/feature-329-spec; branch feature/329-spec; HEAD bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3.


---

## Gate 1 Provisional Ruling (2026-10-25)

Identity: `F:/Dev/LamuFlix.worktrees/feature-329-spec`, branch `feature/329-spec`, HEAD `bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3`.

### Patron ruling (verbatim)

[from Patron] DEV-329 Gate 1 provisional ruling: `gate1: provisional` — approved on the frozen Phase A artifacts (`specs/DEV-329/spec.md` `**Status**:` updated to `gate1: provisional`); Phase B STOP preserved for verified landed `DEV-419` authority on `main` and pickup drift clearance.
- Frozen Phase A scope and Round 2 closure (`specs/DEV-329/brief.md:129-141`, `specs/DEV-329/PLAN_CHALLENGE_ADJUDICATION.md:55-62`, `DEV-329:179,191,193`, `specs/DEV-329/CONCLUSIONS.md` Q1–Q13/Q6a, `specs/DEV-329/ASSUMPTIONS.md` Q13): `speckit-analyze` Round 2 is CLEAN, all three plan-challenge axes (Sentry Risk, Ledger Standards, Compass Spec) were adjudicated, both accepted Medium blockers (`tasks.md` `T012` negative-path assertions and `T019` post-`T018` final changed-HEAD build/format/full `dotnet test` sequence) are verified closed in Round 2 of 2, all Low/Info dispositions stand, and `brief.md`/`spec.md`/`plan.md`/`tasks.md` are frozen within the exact 21-file Q7 boundary and Q12 scope wording with zero owner structural checkboxes.
- Preserved Phase B STOP and merge boundary (`specs/DEV-329/brief.md:141`, `specs/DEV-329/plan.md:9-19`, `specs/DEV-329/tasks.md:25`, `lamuflix-team-charter` §2.3 & §3.1): `gate1: provisional` clears Phase A for spec PR publication only; full Gate 1 requires user merge of the spec PR, and Phase B implementation remains strictly gated on `T001` verifying landed `DEV-419` (`docs/adr/0027-transactional-outbox-enrichment.md`) on `main` plus contract/file/handler/save/DI drift clearance at pickup (the unmerged cited branch object `cb6c6a780d77f41b32d69689f237bdce43d2562d` is not landing proof).
