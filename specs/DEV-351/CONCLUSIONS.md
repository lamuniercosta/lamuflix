# DEV-351 — Grill conclusions

Append-only decision exchanges. Authority: Patron. Facts: recon-DEV-351, supplemented authoritative ticket at lines 94–111 supersedes the earlier summary-only observations. Worktree: F:/Dev/LamuFlix.worktrees/feature-351-spec; branch feature/351-spec; intake HEAD 1cdb13c487c1b257f20cc4d2466d31414190a814. Question cap: 12. Keel records; Quill drafts later. No implementation or Gate 1 authorization.

## Opening

Keel: [from Keel] /grill-with-docs DEV-351. Open Phase 2 step 1 grill, maximum 12 questions, one at a time. Worktree F:/Dev/LamuFlix.worktrees/feature-351-spec branch feature/351-spec. Authoritative ticket is now recon-DEV-351:94-111 and DEV-351:15-28; earlier summary-only observations are superseded. Please read those connected notes. Keel will ask numbered decisions, append full exchanges and cited rulings to specs/DEV-351/CONCLUSIONS.md, then close brief.md and ASSUMPTIONS.md. Do not edit files or tracker. Acknowledge readiness; decisions follow. Reply directly, or ask Keel back if this ask returns before you finish.

Patron: [from Patron] Ready for the DEV-351 grill on F:/Dev/LamuFlix.worktrees/feature-351-spec (feature/351-spec). Read PRODUCT.md, recon-DEV-351 (:1-116, authoritative ticket text at :94-111), and DEV-351 (:15-28). Standing by for Decision 1.

---

## Q1/12 — Domain scope

Keel: [from Keel] DEV-351 Q1/12 — Freeze domain scope. Recommend WatchHistory is the feature vocabulary for an append-only history of successful launcher returns, represented by exactly the ticket record PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt) under Core/Features/WatchHistory; no separate WatchHistory aggregate/table, progress/completion/resume state, user identity, read endpoints, UI, or watchlist changes. Each successful play request creates one event; repeated requests remain distinct. Basis recon-DEV-351:14,18-24,103-111. This meets the explicit record, port, handler and one-table design without inventing delivery scope; cost is no history browsing or actual-player completion guarantee. Confirm ACCEPT/CHANGE with cited basis and any precise amendments. Keel will persist the full exchange before Q2.

Patron: [from Patron] ACCEPT: WatchHistory is the feature slice vocabulary for an append-only history of successful playback launches represented by PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt) under src/LamuFlix.Core/Features/WatchHistory/, with no extra aggregates, progress/resume state, user identity, read endpoints, UI, or watchlist changes.

- Authoritative ticket scope specifies src/LamuFlix.Core/Features/WatchHistory/ with PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt), PlayMovieCommandHandler recording via IPlaybackHistory, and the playback_events table (DEV-351:19-28, recon-DEV-351:103-111).
- Authority and non-goal rules prohibit speculative abstractions or unrequested scope beyond the ticket's Scope & Technical Design (PRODUCT.md:18-24, §2.3).
- When Features:LocalPlay is disabled or launch fails (DisabledMediaPlayerLauncher / 403 or NotFoundException / 404), no PlaybackEvent is recorded (recon-DEV-351:21,52).

Implications: repeated successful requests produce distinct events; launcher success is the observation boundary, not watched/completed content. Read/query capabilities remain outside this ticket.

---

## Q2/12 — Recording order and failure boundary

Keel: [from Keel] DEV-351 Q2/12 — Recording order and failure boundary. Recommend handler resolves movie, invokes existing launcher, then samples TimeProvider.GetUtcNow and awaits durable history append before returning Unit. Disabled/missing/failed launch never appends. If append fails after launch, propagate existing unhandled infrastructure failure; never swallow, relaunch, retry automatically, or claim 204. Cancellation is forwarded to persistence; cancellation after launch can leave a launched player without history. External process and DB cannot share a transaction, so no exactly-once/across-retry claim. Basis ticket AC recon:111 and existing handler/flag/clock recon:19,21,34; cost is visible request failure despite already launched player. ACCEPT/CHANGE with cited basis; explicitly settle whether this boundary meets AC without changing ticket delivery.

Patron: [from Patron] ACCEPT: PlayMovieCommandHandler resolves the movie, launches playback via IMediaPlayerLauncher, then samples TimeProvider.GetUtcNow() and awaits IPlaybackHistory persistence before returning Unit, propagating any persistence failure or cancellation without swallowing, retrying, or returning 204; this boundary satisfies the ticket AC without changing ticket delivery.

- Authoritative ticket scope and AC (DEV-351:20-28, recon-DEV-351:105-111) require recording PlaybackEvent upon movie playback; launching first guarantees disabled (Features:LocalPlay = false, 403), missing (404), or failed launches never record a false playback event (recon-DEV-351:19,21,52).
- TimeProvider injection is mandatory (AGENTS.md Non-negotiables; recon-DEV-351:34), and awaiting persistence before returning Unit ensures a 204 response guarantees the event was durably persisted as required by the AC.
- Because OS process launch (Process.Start) and PostgreSQL cannot share an atomic transaction, launch-then-persist with failure propagation is sound engineering practice and avoids silent data loss or duplicate process launches on DB error (§2.3).

Implications: cancellation or storage failure after launch can leave playback running without an event; a retry is a new launch, with no cross-request deduplication guarantee.

---

## Q3/12 — Persistence seam

Keel: [from Keel] DEV-351 Q3/12 — Persistence seam. Recommend ticket-named IPlaybackHistory in Core/Ports with Task RecordAsync(PlaybackEvent playbackEvent, CancellationToken cancellationToken); one call means append and SaveChangesAsync, not merely tracking. Scoped EfPlaybackHistory uses existing LamuFlixDbContext and is registered in AddLamuFlixPersistence. Inject port and TimeProvider into PlayMovieCommandHandler. Keep Movie aggregate, IMovieRepository and EfMovieRepository unchanged: history is append-only independent storage, no load of event collections or query methods. Record remains exactly the two ticket fields; DB identity stays in persistence record. Basis recon:20,28-34,58-59,105-108. Cost is one narrow ticket-required port/adapter rather than aggregate mapping expansion. ACCEPT/CHANGE with cited basis; confirm same-layer additions need no new architectural layer/package.

Patron: [from Patron] ACCEPT: Define IPlaybackHistory in src/LamuFlix.Core/Ports/ with Task RecordAsync(PlaybackEvent playbackEvent, CancellationToken cancellationToken = default) implemented by scoped EfPlaybackHistory in src/LamuFlix.Infrastructure/Persistence/ (appending and awaiting SaveChangesAsync on LamuFlixDbContext), registered in AddLamuFlixPersistence, and injected alongside TimeProvider into PlayMovieCommandHandler, leaving Movie, IMovieRepository, and EfMovieRepository unchanged; confirmed these same-layer additions introduce no new architectural layer or package.

- Authoritative ticket scope explicitly names IPlaybackHistory, PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt), and PlayMovieCommandHandler (DEV-351:20-25, recon-DEV-351:105-108).
- Keeping Movie and EfMovieRepository unchanged avoids speculative aggregate bloat (recon-DEV-351:59, PRODUCT.md:18), while placing IPlaybackHistory in Core/Ports and EfPlaybackHistory in Infrastructure/Persistence matches the existing port/adapter architecture (recon-DEV-351:20,28-34,48-49, §2.3.1–2.3.2).
- Keeping the persistence primary key (Id) on PlaybackEventRecord preserves the exact two-property domain record specified by the ticket (DEV-351:21).

Implications: the accepted signature includes the optional cancellation-token default; RecordAsync is durable on success and exposes no read/update/delete surface.

---

## Q4/12 — Table lifecycle and migration

Keel: [from Keel] DEV-351 Q4/12 — Table lifecycle and migration. Recommend one playback_events table via PlaybackEventRecord and PlaybackEventConfiguration plus DbSet PlaybackEvents: id bigint generated identity PK, movie_id required existing MovieId converter with FK to movies and ON DELETE CASCADE, played_at required DateTimeOffset mapped timestamp with time zone. Nonunique movie_id index, no uniqueness on (movie_id, played_at), no extra columns/tables or changes to movies shape. Cascade means append-only during movie lifetime, with history removed when movie is deleted; cost is no retained audit of deleted movies. Generate AddPlaybackEvents migration/designer/snapshot through existing EF tooling, do not hand-edit generated files; Down drops only new table. Existing migration count/table expectations update to three/ten. Basis recon:28-33,41,50,105-108. ACCEPT/CHANGE with cited basis, including deliberate schema/deletion-policy ruling under 2.3.3.

Patron: [from Patron] ACCEPT: Map playback_events via PlaybackEventRecord, PlaybackEventConfiguration, and DbSet<PlaybackEventRecord> PlaybackEvents on LamuFlixDbContext with id (bigint generated identity PK), movie_id (required MovieId via MovieIdConverter, non-unique index, FK to movies(id) with ON DELETE CASCADE), and played_at (required DateTimeOffset as timestamp with time zone), generating the AddPlaybackEvents migration via EF Core tooling (Down drops playback_events) and updating MigrationTests to 3 migrations and 10 tables.

- Authoritative ticket scope specifies the playback_events table with (Id, MovieId, PlayedAt) (DEV-351:23-24, recon-DEV-351:107-108), and snake_case columns, MovieIdConverter, DateTimeOffset, and ApplyConfigurationsFromAssembly follow existing persistence conventions (recon-DEV-351:28-33,60).
- Under §2.3.3, playback_events is the ticket's own table with no modifications to movies or other existing tables; ON DELETE CASCADE on the movie_id FK is ruled appropriate because playback events are movie-scoped child records (PlaybackEvent(MovieId, PlayedAt)) with no orphan lifecycle after a movie is removed, and omitting uniqueness on (movie_id, played_at) preserves append-only event semantics.
- Updating MigrationTests.cs (migrations.Length to 3 and expected table list to 10) is an in-scope forced edit required by the migration gate (recon-DEV-351:41,53, §2.3.6), and generated migration/snapshot files must be produced via EF tooling rather than hand-edited (AGENTS.md Non-negotiables).

Implications: event history survives repeated plays but is removed with its owning movie. No WatchHistory table or edits to existing table shapes.

---

## Q5/12 — Public API and LocalPlay charter ruling

Keel: [from Keel] DEV-351 Q5/12 — Public API and LocalPlay charter ruling. Recommend no route/DTO/status additions: existing POST /api/movies/{id}/play keeps documented contract, success 204 only after history persistence, existing failure handling reused. No OpenAPI/types regeneration or baseline edits when schema is unchanged; verify drift at delivery. Explicitly approve handler-side recording on the LocalPlay-gated flow, relying on existing launcher gate so disabled playback writes nothing, with no edits to flag semantics, launchers, Process.Start, secrets or player execution. No new dependency, project, top-level folder, layer or unrelated file deletion/rewrite. Basis recon:18-24,43,48-54,105-111 and Q1-Q4. Cost: no public history access. ACCEPT/CHANGE with cited 2.3.1-6 disposition and whether any owner structural checkbox is required.

Patron: [from Patron] ACCEPT: Keep the public HTTP API contract unchanged (POST /api/movies/{id}/play returning 204 after durable history persistence, reusing existing 404/403/500 handling, with no OpenAPI or web/src/api/types.ts changes) and approve handler-side event recording after IMediaPlayerLauncher.Launch on the Features:LocalPlay-gated path with zero changes to flag semantics, launchers, Process.Start, or secrets; no owner structural checkbox is required.

- §2.3.1–2.3.3 & §2.3.6 (Dependencies, Architecture, Schema, File Scope): Approved with no new NuGet/npm packages (recon-DEV-351:48), same-layer additions only (Core/Features/WatchHistory, Core/Ports, Infrastructure/Persistence; recon-DEV-351:49), schema confined to the ticket's playback_events table (DEV-351:23-24, recon-DEV-351:50), and edits to existing tracked files limited to ticket-named PlayMovieCommandHandler, DI/DbContext wiring, and forced test updates (MigrationTests, unit/integration playback tests; recon-DEV-351:38-42,53).
- §2.3.4 (Public API Shape): Approved with no new routes, DTOs, or status codes (DEV-351:19-28, recon-DEV-351:43,51); OpenApiContractTests continues to verify zero contract drift without baseline regeneration.
- §2.3.5 (Features:LocalPlay, Secrets, Process.Start) & Owner Escalation: Approved because PlayMovieCommandHandler records only after IMediaPlayerLauncher.Launch returns (recon-DEV-351:19,21,52), leaving FeatureOptions.LocalPlay, DisabledMediaPlayerLauncher, ProcessMediaPlayerLauncher, Process.Start, and secrets untouched; because all decisions implement the ticket's Scope & Technical Design (DEV-351:19-28) and comply with the constitution, no owner checkbox (blocked: structural) is required.

Implications: error handling is reused, not redesigned. Recon verifies documented 400/403/404/422 responses at line 18; the Patron reference to 500 handling is a reuse decision, not new independently verified recon evidence.

---

## Q6/12 — Test proof

Keel: [from Keel] DEV-351 Q6/12 — Test proof. Recommend unit tests for launch-before-clock-before-append ordering, exact injected clock timestamp/MovieId/token, awaited durable call, no history on missing/disabled/failed launch, propagated append failure/cancellation without retry, and repeated successful calls. Use existing xUnit/Shouldly/NSubstitute and fixed time; no EF driver mocks. Real PostgreSQL Testcontainers tests must prove 204 -> persisted row from fresh DbContext using actual EfPlaybackHistory and fake process starter, repeat plays at same clock -> distinct IDs, UTC timestamp round trip at database-supported precision, FK rejection and delete cascade, DI composition. Extend migration tests for Up from existing two-migration schema with existing data preserved, table/column/index/FK requirements, Down back to prior schema preserving movie data, and reapply. Keep disabled endpoint 403/no process/no row and existing playback failure regressions; API drift remains unchanged. Basis recon:34,38-44,48 and Q2-Q5. Cost is real DB integration time. ACCEPT/CHANGE with cited basis and any needed narrowing.

Patron (final clean answer; terminal also displayed the same answer once with a trailing typo, immediately corrected): [from Patron] ACCEPT: Prove PlayMovieCommandHandler and PlaybackEvent with xUnit/Shouldly/NSubstitute unit tests using injected TimeProvider and no EF mocks, prove EfPlaybackHistory, endpoint persistence, FK/cascade, and migration Up/Down/reapply against real PostgreSQL Testcontainers (PostgresFixture), and verify zero OpenAPI drift.

- Non-negotiables require TimeProvider for timestamps, existing xUnit/Shouldly/NSubstitute conventions (recon-DEV-351:34,48), and real PostgreSQL Testcontainers rather than mocking the data-access driver (AGENTS.md Non-negotiables; recon-DEV-351:41).
- Verifying 204 endpoint persistence via a fresh LamuFlixDbContext with EfPlaybackHistory wired and IProcessStarter substituted (plus 403/no-process/no-row when Features:LocalPlay is disabled) directly proves the ticket AC (DEV-351:26-28) and preserves existing PlaybackEndpointTests guarantees (recon-DEV-351:38,52).
- Extending MigrationTests and ModelMetadataTests for the 3rd migration (playback_events schema, movie_id index, FK/cascade, Up/Down preserving existing movie rows) is a required forced edit (recon-DEV-351:41-42,53, §2.3.6).

Implications: database translation, serialization, constraints and migration behavior require a real database. Handler orchestration tests may substitute the application port. The complete proposed scenario list is accepted without narrowing.

---

## Q7/12 — Plan and file envelope

Keel: [from Keel] DEV-351 Q7/12 — Plan and file envelope. Recommend Quill tasks ordered: domain record/port + handler tests; persistence record/config/DbSet/adapter/DI + DB tests; generate migration + migration regressions; handler clock/append wiring + endpoint end-to-end tests; API drift/full delivery gates. Production envelope is Core/Features/WatchHistory/PlaybackEvent.cs, Core/Ports/IPlaybackHistory.cs, existing PlayMovieCommandHandler.cs, Infrastructure/Persistence/EfPlaybackHistory.cs, Records/PlaybackEventRecord.cs, Configurations/PlaybackEventConfiguration.cs, LamuFlixDbContext.cs, PersistenceServiceCollectionExtensions.cs and generated migration/designer/snapshot. Test envelope: existing handler/playback endpoint/end-to-end/migration/model-metadata/persistence-composition tests as forced by new dependency/schema, plus new focused history unit/DB tests in existing test projects. No unrelated rewrites/deletions. Keep domain vocabulary in brief glossary and current decisions in CONCLUSIONS; no ADR or root glossary edit needed for this M ticket. Phase A writes only requested three specs/DEV-351 files; Quill owns spec/plan/tasks later. Delivery gates use live harness.yml, no thresholds invented/changed; build/analyzers/complexity/InspectCode/tests/property/security/format/mutation as applicable, API drift unchanged, skips distinct. No gates now. ACCEPT/CHANGE with cited basis and precise additions.

Patron (final clean answer; an earlier duplicated answer had a trailing typo and was immediately corrected): [from Patron] ACCEPT: Approve the 5-phase task ordering, the production and test file envelopes, keeping domain vocabulary in specs/DEV-351/brief.md and rulings in specs/DEV-351/CONCLUSIONS.md without a new ADR or root glossary edit, limiting Phase 2 step 1 writes to specs/DEV-351/{brief.md,ASSUMPTIONS.md,CONCLUSIONS.md}, and running delivery verification against live harness.yml thresholds.

- The production envelope (PlaybackEvent.cs, IPlaybackHistory.cs, PlayMovieCommandHandler.cs, EfPlaybackHistory.cs, PlaybackEventRecord.cs, PlaybackEventConfiguration.cs, LamuFlixDbContext.cs, PersistenceServiceCollectionExtensions.cs, and EF-generated AddPlaybackEvents migration/designer/snapshot) and forced test updates (PlayMovieCommandHandlerTests, PlaybackEndpointTests, ApiEndToEndPlaybackTests, MigrationTests, ModelMetadataTests, PersistenceCompositionTests, plus focused history unit/integration tests) match DEV-351:19-28 and recon-DEV-351:19-43,53 (§2.3.6).
- Note for the forced test envelope: PlaybackEndpointTests (recon-DEV-351:38) substitutes IMovieCatalog and IProcessStarter on the test host, so any host setup that bypasses full DB seeding must also satisfy IPlaybackHistory (via real Postgres or explicit test registration where persistence is not under test).
- harness.yml is the single source of truth for all verification thresholds (AGENTS.md Configuration & Non-negotiables).

Implications: routing-only test hosts may substitute the history application port; the acceptance proof must use actual persistence and a seeded movie. No file-envelope expansion is inferred from host setup requirements.

---

## Q8/12 — Closing bar and shared understanding

Keel: [from Keel] DEV-351 Q8/12 — Closing bar and shared understanding. Recommend review closing bar zero unresolved Critical/High/Medium within frozen scope; Low issues recorded and dispositioned, not silently lost. Two review/remediation rounds maximum, no third fix round or waived gate; unresolved blockers at cap remain blocked. Frozen scope is Q1-Q7: exact ticket PlaybackEvent/IPlaybackHistory/handler recording and one playback_events table with required wiring, generated migration and direct tests; anything else is a follow-up issue, not a finding in this round. No speculative history reads/progress, movie aggregate change, public API/web changes, dependencies/layers, launcher execution edits or unrelated cleanup. All Q1-Q7 are structural implementation choices explicitly ruled, no UI/taste choices require assumed entries; ASSUMPTIONS records none. No owner checkbox, Gate 1 remains closed pending Quill drafts/analysis/challenge/normal approval; grill close is not plan freeze. Confirm ACCEPT/CHANGE with cited basis, agreement all eight decisions reach shared understanding, permission to close requested three artifacts and hand off to Quill through Bernstein. Cost: blockers can halt delivery after two rounds.

Patron (final clean answer; an earlier duplicated answer had a trailing typo and was immediately corrected): [from Patron] ACCEPT: Shared understanding is reached across Q1–Q8; approve the closing bar (zero unresolved Critical/High/Medium within frozen scope, Low dispositioned, 2-round remediation cap), ASSUMPTIONS.md recording no [assumed] taste entries, no owner structural checkbox, and permission to close specs/DEV-351/{brief.md,ASSUMPTIONS.md,CONCLUSIONS.md} and hand off to Quill via Bernstein while Gate 1 remains closed pending spec/plan/tasks, analysis, and challenge.

- Frozen scope and structural rulings Q1–Q7 cover all items in DEV-351:19-28 and recon-DEV-351:46-54 without ticket contradiction or constitution departure (PRODUCT.md:21-48, §2.3).
- Because DEV-351 has no UI, copy, or sort-order taste choices (recon-DEV-351:24, Q1/Q5), specs/DEV-351/ASSUMPTIONS.md properly records zero [assumed] entries (PRODUCT.md:28-30).
- Gate 1 remains closed until Quill completes spec.md, plan.md, and tasks.md, /speckit-analyze is clean, and plan challenge is adjudicated (§2.3, §3.1).

Closing receipt: grill closed after eight questions, all ACCEPT. No unresolved decision or owner checkbox. Scope/loop terms are settled inputs to Quill, not plan-freeze or implementation authorization. No ADR or root glossary change; use the canonical vocabulary in brief.md when drafting the specification. No production changes, commits, tests or gates were authorized in this ask.


---

## Phase 2 step 3.3 — Analysis round 1 C1–C3 rulings

Full received Patron ruling follows verbatim. It supersedes the earlier no-owner-checkbox conclusion for C1; it does not erase the original exchanges.

[from Patron] DEV-351 Phase 2 step 3.3 Rulings on C1, C2, and C3 (keel to record in specs/DEV-351/CONCLUSIONS.md and update brief.md):

1. C1 — blocked: structural — Should PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt) live in shared Core/Domain/ (or Core/Ports/) to satisfy constitution.md:133-135 and ArchitectureTests.cs:87-95, or should ArchitectureTests and Principle I be amended to permit Features/Playback (and Core/Ports) to reference Features/WatchHistory as literal ticket line DEV-351:21 specifies?
- DEV-351:21-22 specifies src/LamuFlix.Core/Features/WatchHistory/ with PlaybackEvent and updating PlayMovieCommandHandler (in Features/Playback) to record via IPlaybackHistory (in Core/Ports), which creates a cross-feature dependency that violates constitution.md:133-135 (Feature folders in Core MUST NOT reference each other; shared concepts live in Domain/, Ports/, or Pipeline/) and fails the build gate ArchitectureTests.Core_features_must_depend_only_on_ports_domain_or_pipeline (ArchitectureTests.cs:87-95, 291-309).
- Under charter §2.3 (a) and (b), either moving PlaybackEvent out of the ticket-named path src/LamuFlix.Core/Features/WatchHistory/ (contradicting ticket Scope & Technical Design) or permitting cross-feature references (departing from constitution Principle I and modifying ArchitectureTests) requires an explicit owner checkbox before Gate 1 can open.
- Patron recommends placing PlaybackEvent in src/LamuFlix.Core/Domain/PlaybackEvent.cs (where shared domain types Movie, MovieId, etc. reside and where CONTEXT.md already defines Playback Event as a core domain term at constitution.md:294) while keeping WatchHistory feature logic/handlers in Features/WatchHistory/, or alternatively keeping a type-forwarding/alias only if the owner prefers amending ArchitectureTests; carry this as an owner structural checkbox on the spec PR so Gate 1 stays closed until the user decides.

2. C2 — RESOLVED (no ADR required; not a constitution departure): Implementing IPlaybackHistory in Core/Ports/ is pre-authorized by the constitution whitelist and requires no new ADR.
- constitution.md:136-140 explicitly enumerates IPlaybackHistory in the closed whitelist of pre-approved Core ports (the only ports are IMovieRepository, IMovieCatalog, IMetadataProvider, IEnrichmentQueue, IMediaLibraryScanner, IMediaPlayerLauncher, IRecommendationCandidateSource, IPlaybackHistory...), and ArchitectureTests.cs:28-29 already whitelists IPlaybackHistory in PortWhitelistPattern.
- The clause Adding a port requires an ADR (constitution.md:139-140, 375) governs expanding that closed port list with a port not already in the constitution, whereas DEV-351:22 implements the already-whitelisted IPlaybackHistory port.

3. C3 — RESOLVED (incremental AddPlaybackEvents migration approved; no owner escalation): Adding the 3rd EF Core migration (AddPlaybackEvents) is mandated by the ticket and established repo practice, not a constitution departure.
- DEV-351:23-24 explicitly mandates Create migration adding playback_events table (Id, MovieId, PlayedAt), which is authoritative ticket text under charter §2.3.
- constitution.md:342 (single fresh Initial migration) described the initial greenfield replacement of legacy MySQL migrations when PostgreSQL was introduced; subsequent schema evolution on main already uses incremental EF migrations (e.g. 2nd migration AddMovieWriters enforced by MigrationTests.cs at recon-DEV-351:41,42,50).

Keel disposition: C1 remains blocked: structural pending the explicit owner checkbox on the spec PR. C2 and C3 resolved as ruled. brief.md updated first with these decisions and one numbered Quill correction list covering C1/C2/C3/I1/I2. Quill owns draft corrections; no production, constitution, ArchitectureTests or draft artifact edits by Keel. Gate 1 remains closed; no new review round or approval claimed.


---

## Phase 2 step 6 — Full Patron rulings and Keel adjudication

Source: DEV-351:162-220; received through the permitted connected note. Supersedes Q4 bigint choice only as ruled. Earlier exchanges retained.

[from Patron] DEV-351 Phase 2 step 6 Plan Challenge Adjudication Rulings (16 findings across Sentry, Ledger, Compass; OD-1 remains unchecked Critical, Gate 1 remains closed):

### Risk Axis (Sentry)
1. Sentry M1 — ACCEPT Keel proposed disposition (Reject as duplicate of Q2 / spec.md:40; no follow-up ticket):
- CONCLUSIONS.md Q2 (lines 36-37), brief.md:47, and spec.md:40 already explicitly record that process launch and DB write are not atomic, failure/cancellation after launch can leave a player running without history, and a subsequent request is a new launch with no cross-request deduplication or process-cleanup scope.
- Under charter §2.3, this accepted non-atomic boundary is neither broken behaviour nor a Critical/High out-of-scope defect: `noted, no ticket`.

2. Sentry M2 — ACCEPT Keel proposed disposition (Bounded failure-response verification through existing error handling):
- Extend T019 (or existing playback failure regression coverage) with one bounded case verifying that when `IPlaybackHistory.RecordAsync` throws after launch, the endpoint does not return 204 and routes through the existing unhandled-exception handling without promising an unverified new OpenAPI status contract or altering documented responses (`recon-DEV-351:18`, `CONCLUSIONS.md` Q2/Q5).

3. Sentry M3 — ACCEPT Keel proposed disposition (Exact equality on port unit tests; aligned fixed clock values for DB round-trip proofs):
- Pure handler unit tests (`T005`, substituting `IPlaybackHistory`) assert exact `DateTimeOffset` equality against `TimeProvider.GetUtcNow()` (`CONCLUSIONS.md` Q2/Q6).
- PostgreSQL integration tests (`T017`, `T019`) use deterministic fixed `TimeProvider` instants aligned to database-supported precision (whole seconds or whole milliseconds/microseconds without sub-microsecond 100ns ticks) so round-trip assertions are deterministic without importing unverified numeric precision claims into the spec (`spec.md:80`, `CONCLUSIONS.md` Q6).

4. Sentry M4 — ACCEPT Keel proposed disposition (Two-movie sibling event survival in T017):
- Update `T017` so the cascade-delete test seeds two movies with playback events, deletes one movie, and asserts both that the deleted movie's events are removed and that the surviving sibling movie's events remain intact, directly proving `SC-005` (`spec.md:35,111`, `CONCLUSIONS.md` Q4/Q6).

5. Sentry L1 — ACCEPT Keel proposed disposition (Document that Down intentionally drops playback history while preserving movie data):
- Add one explicit clause in `FR-016` and `T016` stating that `Down` drops `playback_events` (discarding recorded playback events by design) while preserving all `movies` rows (`CONCLUSIONS.md` Q4/Q6).

6. Sentry L2 — ACCEPT Keel proposed disposition (Delete-before-append race is already covered by Q2/FR-007 failure propagation):
- If a movie is deleted between catalog resolution and `SaveChangesAsync`, the foreign-key failure propagates under `FR-007` after launch; record as an accepted Q2 non-atomic residual with no new concurrency harness or extra test obligation (`CONCLUSIONS.md` Q2, `spec.md:40,58`).

7. Sentry L3 — ACCEPT Keel proposed disposition (Keep shared scoped `LamuFlixDbContext` per Q3; bounded delivery check that play path performs no unrelated tracked writes):
- Retain scoped `EfPlaybackHistory` calling `SaveChangesAsync` on `LamuFlixDbContext` (`CONCLUSIONS.md` Q3, `recon-DEV-351:28,32`), and add a bounded verification note in `T011`/`T019` confirming the play flow (`IMovieCatalog` lookup + launcher + `EfPlaybackHistory.RecordAsync`) introduces no unrelated tracked entity modifications before `SaveChangesAsync`; any missing fact during delivery goes to recon.

### Standards Axis (Ledger)
8. Ledger F1 — CHANGE Q4 `playback_events.id` from `bigint` to `integer` (`int Id` on `PlaybackEventRecord`):
- While an event table's own primary key does not need to match the `movies(id)` foreign-key type (rejecting the FK-matching rationale), `DEV-351:24` (`recon-DEV-351:108`) specifies `playback_events (Id, MovieId, PlayedAt)` without mandating `bigint`, and matching the repository's universal `integer` identity convention (`int Id` with `UseIdentityByDefaultColumn` across `MovieConfiguration`, `ActorConfiguration`, and `AddMovieWriters`, `findings-DEV-351-Ledger:12`) is the cleaner standards-consistent choice under charter §2.3.3.
- Update `brief.md` Q4, `spec.md` FR-014, `plan.md` Design §3, and `tasks.md` T008/T009/T014/T016 from `bigint` to `integer` (`int Id` on `PlaybackEventRecord`, generated identity primary key).

9. Ledger F2 — ACCEPT Keel proposed disposition (Acknowledge four constructor dependencies and replace unverifiable citation with Q3/Q5 reasoning):
- `PlayMovieCommandHandler` takes `IMovieCatalog`, `IMediaPlayerLauncher`, `IPlaybackHistory`, and `TimeProvider` (three application ports plus the injected clock required by `AGENTS.md` Non-negotiables and `recon-DEV-351:34`).
- Remove the unverifiable `constitution.md:460-461` citation from `plan.md:43` and ground the 4-dependency constructor directly in `CONCLUSIONS.md` Q2/Q3/Q5: all four dependencies are required by the ticket and non-negotiables, and introducing a facade/wrapper abstraction for a four-step handler would violate `PRODUCT.md:18` and Q5 (`§2.3.2` no extra layer/abstraction).

10. Ledger F3 — ACCEPT Keel proposed disposition (`WatchHistory` is settled feature-slice vocabulary per Q1/Q7; no `CONTEXT.md` edit; replace unverified glossary citation):
- Per `CONCLUSIONS.md` Q1 and Q7 (`brief.md:30,35,41,136`), `WatchHistory` is the ticket-mandated feature folder/slice name (`DEV-351:17,21`) and `PlaybackEvent` is the already-established domain concept (`constitution.md:134,294`); no root `CONTEXT.md` edit or new ADR is in scope.
- In `plan.md:46`, cite `brief.md:26-35`, `CONCLUSIONS.md` Q1/Q7, and `constitution.md:134,294` rather than any unverified `CONTEXT.md` line number.

11. Ledger F4 — ACCEPT Keel proposed disposition (Pin `required` members on `PlaybackEventRecord` and preserve equivalent PostgreSQL timestamptz mapping):
- In `plan.md` Design §3 and `tasks.md` T008/T009, specify `required MovieId MovieId` and `required DateTimeOffset PlayedAt` on `PlaybackEventRecord` and allow `.HasColumnType("timestamptz")` (PostgreSQL's canonical alias for `timestamp with time zone`) as the reviewer-supplied repo configuration convention while preserving the exact FR-014 schema semantics (`CONCLUSIONS.md` Q4, `findings-DEV-351-Ledger:28-31`).

### Spec Axis (Compass)
12. Compass F1 — ACCEPT Keel proposed disposition (Update `ASSUMPTIONS.md` and `spec.md:116-118` to reflect C1/OD-1):
- Update `specs/DEV-351/ASSUMPTIONS.md` (and `spec.md` Assumptions) to keep "zero `[assumed]` taste entries" while replacing the stale "No unresolved owner structural checkbox" sentence with the Phase 2 step 3.3 ruling that `OD-1 (C1)` is an unresolved owner structural checkbox blocking Gate 1 (`CONCLUSIONS.md:126-145`, `brief.md:82,158-164`).

13. Compass F2 — ACCEPT Keel proposed disposition (Add explicit Q7 file-envelope and unchanged-aggregate diff check to Phase 5 / T021, and retract the 33/33 claim until tasks.md is updated):
- Amend Phase 5 (`T021` or a dedicated task step in Phase 5 and `plan.md` §7) to explicitly verify `git diff` against the Q7 file envelope: confirm `Movie`, `IMovieRepository`, and `EfMovieRepository` are untouched (`FR-013`), no new NuGet/npm package, project, top-level folder, or layer was added (`FR-021`), and all modified/added files stay within the Q7 envelope (`FR-022`). Retract the 33/33 coverage claim in note `DEV-351` until Quill applies the task update and Keel re-verifies read-only.

14. Compass F3 — ACCEPT Keel proposed disposition (Clarify in T019/T017 that `EfPlaybackHistory` is resolved via production `AddLamuFlixPersistence` DI):
- Add one explicit clause in `T019` (and `plan.md` §6) stating that the seeded PostgreSQL acceptance host resolves the real `EfPlaybackHistory` through `AddLamuFlixPersistence` DI registration rather than manual instantiation (`CONCLUSIONS.md` Q3/Q6).

15. Compass F4 — ACCEPT Keel proposed disposition (Make `T003` unit-test file path follow `OD-1` resolution):
- Update `T003` and `plan.md:86,96` so the new `PlaybackEventTests.cs` path mirrors whichever location the owner selects in `OD-1` (`tests/LamuFlix.UnitTests/Domain/PlaybackEventTests.cs` if `Core/Domain/PlaybackEvent.cs` is chosen, or `tests/LamuFlix.UnitTests/Features/WatchHistory/PlaybackEventTests.cs` if `Core/Features/WatchHistory/PlaybackEvent.cs` is chosen).

16. Compass F5 — ACCEPT Keel proposed disposition (Replace unsupported `constitution.md:460-461` and `:488` citations in `plan.md:43,48` with settled Q2/Q3/Q5 sources):
- Replace `constitution.md:460-461` and `constitution.md:488` in `plan.md:43,48` with the already-settled citations `recon-DEV-351:18-19`, `brief.md` Q2/Q3/Q5, and `CONCLUSIONS.md` Q2/Q3/Q5; no outside recon pass is needed.

Gate & Loop Status: `OD-1 (C1)` remains an unchecked Critical owner structural checkbox; Gate 1 remains closed; no plan freeze or implementation authorization.

## Phase 2 step 6: Full Plan Challenge adjudication report (Keel)

- Status: adjudication-complete; corrections pending; blocked: structural — C1/OD-1 owner decision.
- Identity: F:/Dev/LamuFlix.worktrees/feature-351-spec, branch feature/351-spec; intake HEAD 1cdb13c487c1b257f20cc4d2466d31414190a814. Branch verified before edits; six named artifacts inspected. Three exact reports present; no missing axis.
- Authority: full cited Patron rulings above at DEV-351:161-220, now preserved verbatim in CONCLUSIONS.md. Ledger F1 changes the settled event PK to integer; other dispositions accepted as proposed.

| Source / finding | Ruling and basis | Required disposition |
| --- | --- | --- |
| Sentry M1, Medium | Rejected as a new blocker: brief Q2:45-47, CONCLUSIONS Q2 and spec Edge Cases already accept a running player without history and a later request as a new launch. | Retain handler no-relaunch proof. No process cleanup, deduplication or retry scope. |
| Sentry M2, Medium | Accepted as a bounded proof gap, not authority to add a status/mapper: Q2/Q5 require propagation and unchanged API; plan:44 overstates unverified existing 500 mapping. | Add a controlled append-failure endpoint case using existing failure handling; do not promise a response shape absent evidence or substitute away T019 real-persistence success proof. Any missing mapper fact is needs recon, not a new API decision. |
| Sentry M3, Medium | Accepted as deterministic proof clarification under Q6; no numeric database precision fact verified in these roots. | Use fixed UTC test instants aligned to supported DB precision and state the comparison boundary in T017/T019; port-only T005 retains exact equality. Do not invent a tolerance or change PlayedAt sampling. |
| Sentry M4, Medium | Accepted: spec SC-005 and scenario 7 require sibling survival; T017:67 only explicitly removes the owning movie events. | Seed two movies and their events; delete one; assert its events gone and the other movie and its events survive with exact counts. |
| Sentry L1, Low | Accepted clarification: Q4 requires Down to drop playback_events, preserving existing movie data. | State explicitly that downgrade discards playback history and preserves movies. No additional retention/backup scope. |
| Sentry L2, Low | Accepted residual, not a new blocker: delete after resolve/before append is governed by Q2 propagation and Q4 FK rejection. | Record launch may remain running when the later insert fails; no required new interleaving harness, exception-shape guarantee or cleanup. |
| Sentry L3, Low | Accepted bounded review obligation under Q3 scoped context/SaveChanges, not authorization for a new context/layer. | Delivery review checks whether this play scope has unrelated tracked writes that history SaveChanges could commit. Missing facts go to recon; do not claim the catalog is tracking-free from these roots. |
| Ledger F1, Medium | Accepted convention correction; Patron changes Q4 from bigint to integer for the event PK. The event PK is independent of movie_id; a PK/FK type mismatch is not established by bigint on the separate id. | Propagate integer generated identity / int Id through brief, spec FR-014, plan and T008/T009/metadata/migration expectations. movie_id converter and FK stay unchanged. |
| Ledger F2, Low | Accepted rationale correction: acknowledge four dependencies including TimeProvider and the heuristic signal; Q2/Q3 justify the simple sequence. | Remove the unsupported claim that the signal is not crossed; no new wrapper/service/abstraction or constitution departure is authorized. |
| Ledger F3, Low | Dispositioned as existing feature vocabulary under Q1/Q7, not a new domain concept. No CONTEXT.md edit authorized. | Re-cite settled brief vocabulary/Q1/Q7; remove unsupported glossary line attribution rather than asserting an outside-source location. |
| Ledger F4, Low | Accepted implementation convention clarification, preserving required schema semantics from Q4. | Pin required MovieId/PlayedAt record members and timestamptz configuration literal as Patron-approved use of supplied reviewer convention evidence; no new field or dependency. |
| Compass F1, Medium | Accepted: ASSUMPTIONS:3-5 contradicts superseding C1 ruling at brief:158-164 and CONCLUSIONS Phase 2 step 3.3. | Correct the ruling record: no taste assumptions, C1/OD-1 unresolved, C2/C3 resolved. Owner checkbox is not an assumed taste item. |
| Compass F2, Medium | Accepted: tasks T021:91 has gate execution but no explicit diff review; earlier DEV-351:104,109,119 coverage claim is overstated. | Add explicit base...head diff-envelope verification for FR-013/021/022 to plan/T021; confirm unchanged Movie/repositories, no extra dependency/project/layer, and only Q7-authorized edits. Coverage remains pending correction/readback. |
| Compass F3, Low | Accepted clarification of Q6 composition; unit T013 alone is not the real-host persistence composition proof. | T019 resolves actual EfPlaybackHistory through production AddLamuFlixPersistence DI on the seeded host; retain T013 scoped registration check. |
| Compass F4, Low | Accepted conditional path alignment under existing OD-1. | Make T003 test home/namespace follow the owner-approved production type home; neither production location is chosen now. |
| Compass F5, Low | Accepted source correction, not an independent verification of constitution:460-461 or :488. | Re-cite settled Q2/Q3/Q5 and acknowledged heuristic rationale; remove unsupported source assertions. No outside-root lookup performed. |

### Single numbered correction handoff

1. Correct ASSUMPTIONS.md and spec.md Assumptions to retain zero taste assumptions while explicitly carrying unresolved C1/OD-1 and resolved C2/C3. Do not check OD-1 or treat Patron recommendation as owner approval (Compass F1).
2. Propagate Patron replacement of Q4 event id bigint with integer generated identity / int Id throughout spec FR-014, plan and T008/T009; keep movie_id converter/FK and all other Q4 semantics unchanged (Ledger F1). Pin required MovieId/PlayedAt members and equivalent timestamptz literal (Ledger F4).
3. Add bounded endpoint append-failure response verification via existing handling, preserving T019 seeded actual-adapter acceptance. Remove assertions that a shaped 500 mapper is already evidenced; missing behavior facts go to recon, no new mapper/status/DTO/baseline change (Sentry M2).
4. State deterministic database round-trip comparison using fixed UTC instants aligned to supported precision in T017/T019; retain exact event/clock equality at substituted-port T005. Do not import an unverified precision number (Sentry M3).
5. T017 must prove two-movie sibling movie/event survival with exact counts after deleting one owner (Sentry M4 / SC-005).
6. Explicitly include base...head diff-envelope review in plan delivery verification and T021 covering FR-013/021/022; do not claim complete planned coverage until this exists and is read back (Compass F2). Include a bounded verification clause in T011/T019 for unrelated tracked writes in the shared play context; no speculative new context/service (Sentry L3).
7. Clarify Down loses playback history while preserving movies; record delete-between-resolve-and-append as accepted failure residual. Existing retry-is-new-launch wording already disposes M1; do not add cleanup or deduplication scope (Sentry L1/L2/M1).
8. Acknowledge all four handler dependencies and the heuristic without introducing an abstraction. Use settled Q1/Q2/Q3/Q5/Q7 citations for handler rationale, WatchHistory vocabulary and unchanged 204; remove unsupported constitution/glossary assertions. No root glossary edit (Ledger F2/F3, Compass F5).
9. State T019 resolves the real adapter through production AddLamuFlixPersistence on the seeded host, alongside unit composition coverage (Compass F3). Make T003 test path/namespace conditional on OD-1 (Compass F4).
10. Retain unchecked OD-1 consistently, prerequisites before implementation/delivery, Q7 envelope, zero Critical/High/Medium closing bar and the existing two-round cap. Return corrected drafts for read-only verification; no third fix round, plan freeze or delivery authorization is granted.

### Closing state and limits

Adjudication complete for all 16 findings from the three required axis reports. This is a decision receipt, not closure of draft corrections. Six accepted Medium findings remain pending Quill correction and read-only verification: Sentry M2/M3/M4, Ledger F1, Compass F1/F2. Sentry M1 is rejected as already settled; all nine Low findings are dispositioned above, with clarification work included in the single correction list. Existing C1/OD-1 remains Critical and unchecked; no plan freeze, Gate 1 opening, implementation authorization, delivery gate result, commit or merge approval.

The prior DEV-351:104,109,119 attribution of FR-013/FR-021/FR-022 to a T021 diff review and the resulting 33/33 (100%) planned-coverage claim are retracted pending the explicit task correction and read-only verification. The requirements exist; complete planned verification coverage has not yet been earned. The old Sentry summary at DEV-351:138-139 misidentifies L2/L3: the full source report defines L2 as delete-before-append race and L3 as shared-context tracked changes. This report uses those full source findings. Compass statement that OD-1 is carried on the spec PR is treated only as draft checkbox coverage; no actual PR publication was verified.

Work stayed within the four named notes and six artifacts for substantive evidence. Patron rulings were read from DEV-351:161-220; the outside-root Temp file was not read. Reviewer-only convention/citation claims are attributed as such rather than represented as independently verified recon. No production research, production writes or delivery gates ran. The original two-round cap and zero unresolved Critical/High/Medium closing bar remain unchanged; no third fix round is authorized. Next: Conductor routes the single numbered list to Quill (ruling-record correction in ASSUMPTIONS may be recorded by Quill/Rigger), then Keel verifies the corrected artifacts read-only. Owner resolves OD-1 through the spec PR before Gate 1.

Persistence receipt: Keel updated brief.md status and Q4 id type, appended this challenge decision/correction handoff to brief.md, and appended the full Patron ruling plus adjudication/correction list to CONCLUSIONS.md. spec.md, plan.md, tasks.md and ASSUMPTIONS.md remain unchanged for the correction owner. Full artifact and note readback follows; no delivery gates run.

---

## Phase 2 step 7 — Closure ratification and `gate1: provisional`

[from Patron] DEV-351 Phase 2 step 7 ruling: `gate1: provisional` set on `specs/DEV-351/spec.md:7` with `OD-1 (C1)` (`blocked: structural`) carried onto the spec PR body as an unchecked owner decision checkbox (Gate 1 stays closed until owner answers `OD-1` before merging).
- All 10 numbered Phase 2 step 6 corrections and the six accepted Medium findings (Sentry M2/M3/M4, Ledger F1, Compass F1/F2) are ratified closed at draft verification level per Keel's read-only verification receipt (`DEV-351:287-308`, `CONCLUSIONS.md:239-250`).
- `OD-1 (C1)` (`spec.md:13-15`, `CONCLUSIONS.md:132-135`, charter §2.3 (a)/(b)) remains an unchecked owner structural checkbox (`blocked: structural — Should PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt) live in shared Core/Domain/ (or Core/Ports/) to satisfy constitution.md:133-135 and ArchitectureTests.cs:87-95, or should ArchitectureTests and Principle I be amended to permit Features/Playback (and Core/Ports) to reference Features/WatchHistory as literal ticket DEV-351:21 specifies?`) that must be carried onto the spec PR body; Gate 1 stays closed until the user resolves it before merging.

---

## Phase 3 step 5 — Charter §2.3 ruling on mutation gate artifact kill share

[from Patron] APPROVE Option A: Harden `tests/LamuFlix.Tests.Common/PostgresFixture.cs` (and `tests/LamuFlix.Tests.Common/LamuFlixDbContextFactory.cs` if required for the uncached model key) with per-reset dynamic `TRUNCATE` discovery from `information_schema` and an uncached EF model key as an in-scope gate-forced edit under charter §2.3.6; reject Options B, C, and D.

- Charter §2.3.6 & Q7/FR-022 (Gate-Forced Edit in Scope): Modifying tracked shared test infrastructure in `tests/LamuFlix.Tests.Common/` (`PostgresFixture.cs` and, if needed for the uncached model key, `LamuFlixDbContextFactory.cs`) is forced by the `scripts/run-mutation.ps1` `maxKillShare` (50%) gate (`AGENTS.md` Verification commands) to eliminate cross-mutant state poisoning in reused Stryker test-server processes; the Q7 / `FR-022` / `T021` authorized test file envelope is explicitly extended to include `tests/LamuFlix.Tests.Common/` for this forced fix.
- `AGENTS.md` Non-negotiables & DEV-410 Process-Reuse Isolation: `AGENTS.md` prohibits process-cached state in Stryker runs from leaking across mutant sessions (DEV-410 precedent); replacing the cached `context.Model`-derived `_truncateStatement` with per-reset dynamic table discovery from `information_schema` and preventing cached mutated EF models from poisoning subsequent sessions fixes the root cause in test infrastructure without altering production code or gate thresholds.
- Rejection of Options B, C, and D: Option B is rejected because `scripts/run-mutation.ps1`, `harness.yml`, and `stryker-config.json` are protected/generated harness files that must not be edited or weakened (`AGENTS.md` Non-negotiables & Configuration); Option C is rejected because excluding Docker/Testcontainers-backed tests violates the real-database testing non-negotiable and weakens mutation coverage; Option D is rejected because rerunning relies on nondeterministic worker scheduling rather than fixing the cross-mutant state leak.
