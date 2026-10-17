# DEV-351 — Closed grill brief

Status: Phase 2 step 6 Plan Challenge adjudicated across all three axes. Six accepted Medium corrections await Quill correction/read-only verification. C1/OD-1 remains Critical, blocked: structural and unchecked; C2/C3 resolved. Patron changes Q4 event id to integer. Original grill closed at 8/12 questions. Gate 1 remains closed; no plan freeze, implementation authorization or gate result.

## Identity and authority

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-351-spec`.
- Branch: `feature/351-spec`.
- Intake HEAD: `1cdb13c487c1b257f20cc4d2466d31414190a814`.
- Ticket: DEV-351, parent DEV-288, size:M. Authoritative description: note DEV-351:15–28 and recon-DEV-351:94–111. This supplemental ticket text supersedes the earlier summary-only observations at recon:10,51; the recon suggestions about hypothetical API additions do not add scope.
- Facts are supplied by recon-DEV-351; no independent repository searches or measurements were used. Patron ruled; Keel recorded. Full questions, answers, rationale and implications are in append-only CONCLUSIONS.md Q1–Q8.
- Patron also cited PRODUCT.md:18–24,21–48 and §2.3 for scope/authority, and PRODUCT.md:28–30 for taste assumptions. These citations are Patron-supplied decision bases, not independently researched facts by Keel.

## Authoritative ticket

Overview: Implement `WatchHistory` domain model and record `PlaybackEvent` upon movie playback.

Scope & Technical Design:

1. `src/LamuFlix.Core/Features/WatchHistory/` with `PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt)`.
2. Update `PlayMovieCommandHandler` to record event via `IPlaybackHistory`.
3. Create migration adding `playback_events` table `(Id, MovieId, PlayedAt)`.

Acceptance criterion: Launching movie playback automatically persists playback event timestamp.

## Canonical vocabulary

| Term | Settled meaning |
| --- | --- |
| WatchHistory | The history of successful playback launches for movies; no separate aggregate or extra table. |
| PlaybackEvent | One observation of a successful launcher return, with its MovieId and PlayedAt. It does not mean viewing began, progressed, or completed. |
| PlayedAt | The UTC clock instant sampled immediately after the launcher returns successfully. |
| Repeated playback | Separate successful play requests create distinct events, including when timestamps match. |

These terms feed the specification. Per Q7, vocabulary stays here; no root CONTEXT.md change or new ADR is required for this M ticket.

## Settled decisions and approach

### Q1 — Domain boundary

Retain exactly the ticket's two-field `PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt)` record. Its location is unresolved under C1: the ticket names `Core/Features/WatchHistory/`, but the constitution prohibits cross-feature references. Patron recommends `Core/Domain/PlaybackEvent.cs`, subject to an explicit owner decision; neither location change nor constitution/test amendment is authorized yet. WatchHistory names this feature; do not add a WatchHistory aggregate or table. Events are append-only during the owning movie's lifetime. No user identity, progress, completion, resume state, read endpoints, UI or watchlist changes. Repeated successful requests create separate events. Basis: recon:14,18–24,103–111; CONCLUSIONS Q1.

### Q2 — Handler ordering and observable success

`PlayMovieCommandHandler` resolves the movie using its existing catalog, calls the existing launcher, samples injected `TimeProvider.GetUtcNow()`, then awaits history persistence before returning Unit. A 204 means history persistence completed. Missing movie, disabled LocalPlay and failed launch must not append. Pass cancellation to persistence; propagate append failure/cancellation without swallowing it, relaunching, or adding an automatic retry.

The process launch and database write are not atomic. Failure/cancellation after launch can leave a player running without history; a subsequent request is a new launch. No cross-request exactly-once or deduplication promise. Patron explicitly accepted this as satisfying the existing AC without changing delivery. Basis: recon:19,21,34,111; CONCLUSIONS Q2.

### Q3 — Port and persistence seam

Define `IPlaybackHistory` in `src/LamuFlix.Core/Ports/` with `Task RecordAsync(PlaybackEvent playbackEvent, CancellationToken cancellationToken = default)`. Successful completion means durable append, not EF tracking alone. A scoped `EfPlaybackHistory` in existing Infrastructure/Persistence appends a persistence record and awaits `LamuFlixDbContext.SaveChangesAsync`, registered through `AddLamuFlixPersistence`. Inject the port and TimeProvider into the handler.

The persistence identity belongs only to PlaybackEventRecord; retain the exact two-field domain shape. No read/update/delete methods or loaded event collection. Leave Movie, IMovieRepository and EfMovieRepository unchanged. This ticket-required port and adapter stay within existing layers; no new dependency or architectural layer. Basis: recon:20,28–34,48–49,58–59,105–108; CONCLUSIONS Q3.

### Q4 — Schema, lifecycle and migration

Add `PlaybackEventRecord`, `PlaybackEventConfiguration` and `DbSet<PlaybackEventRecord> PlaybackEvents` to the existing context.

| Column | Mapping and constraint |
| --- | --- |
| `id` | `integer`, generated identity, primary key (step 6 Patron amendment). |
| `movie_id` | Required, existing MovieIdConverter, nonunique index, FK to `movies(id)` with ON DELETE CASCADE. |
| `played_at` | Required DateTimeOffset as PostgreSQL `timestamp with time zone`. |

Use only `playback_events`; do not change the shape of movies or other existing tables. No unique constraint on movie/timestamp. Deleting the movie removes its history, so history is not a retained audit of removed movies.

Generate `AddPlaybackEvents` migration, designer and model snapshot using the existing EF tooling; do not hand-edit generated files. Down drops only playback_events. Update migration expectations from two migrations/nine tables to three/ten for this intake baseline. Preserve existing data through upgrade and rollback. Follow existing configuration/environment handling for design-time connectivity; never hardcode a connection string or print secrets. Basis: recon:28–33,41,50,60,107–108; CONCLUSIONS Q4.

### Q5 — Public API and charter §2.3

| Item | Patron ruling |
| --- | --- |
| §2.3.1 packages | No NuGet/npm dependency additions. Existing stack suffices. |
| §2.3.2 architecture | Same-layer feature, port and persistence additions only; no project, top-level folder or new layer. |
| §2.3.3 schema | Only the ticket's playback_events table, with Q4 child lifecycle and constraints; no existing table redesign. |
| §2.3.4 public API | No new route, DTO field set or status code. Preserve `POST /api/movies/{id}/play`; 204 follows durable persistence. Reuse existing error handling. |
| §2.3.5 LocalPlay/execution/secrets | Approve recording after launcher success on the existing gated path. LocalPlay off means no event. No changes to flag semantics, launchers, Process.Start or secrets. |
| §2.3.6 files | Ticket-named handler plus necessary persistence/DI wiring and direct forced tests; no unrelated deletion/rewrite. Generated artifacts only through tooling. |

No OpenAPI/TypeScript baseline regeneration or edits are planned because no contract shape changes. Delivery verifies zero OpenAPI drift; do not handcraft TS DTOs. Existing documented responses are recon:18's 204/400/403/404/422; Patron's reference to existing 500 handling is a reuse ruling, not recon proof or a new documented contract.

The original no-checkbox ruling in Q5/Q8 is superseded by the Phase 2 step 3.3 C1 ruling below. One owner structural checkbox is required; C2 and C3 require none. Basis: CONCLUSIONS Phase 2 step 3.3 and Patron-supplied citations below.

## Q6 — Test strategy

Use existing xUnit, Shouldly, NSubstitute and fixed/manual TimeProvider patterns. Do not mock EF/database-driver behavior. Unit tests may substitute IPlaybackHistory as an application port.

- Handler tests: resolve/launch/clock/append order; exact MovieId, clock timestamp and token; await history completion before success; no event on missing/disabled/failed launch; append failure/cancellation propagation; no retry/relaunch; repeated successful calls.
- Real PostgreSQL Testcontainers tests: actual EfPlaybackHistory persists a row that a fresh DbContext can read; repeated plays at the same clock instant yield distinct IDs; UTC timestamp roundtrip at database-supported precision; FK rejection, cascade deletion and DI composition.
- Endpoint acceptance: seeded real database, actual adapter and substituted IProcessStarter; 204 must imply one persisted event. Disabled LocalPlay must remain 403 with no process and no row. Preserve existing playback failure regressions. Port-only tests do not satisfy this database acceptance proof.
- Migration tests: upgrade from the prior two-migration schema with movie data preserved, exact table/columns/index/FK requirements, downgrade to the prior schema without losing movie data, then reapply. Update the existing migration count/table expectations and direct model metadata tests.
- OpenAPI contract drift tests retain existing baselines.

Routing-only hosts that bypass DB seeding must explicitly satisfy the new port via a test registration or real database, and satisfy clock injection. Do not silently replace real persistence in the acceptance test. Basis: recon:34,38–44,48,52–53; CONCLUSIONS Q6–Q7.

## Q7 — Files touched and task ordering

Phase 2 step 1 touches only this brief, CONCLUSIONS.md and ASSUMPTIONS.md. The following is the approved future implementation envelope for Quill's plan, not changes made now.

Production additions:

- `PlaybackEvent.cs`: location blocked by C1. Ticket location is `src/LamuFlix.Core/Features/WatchHistory/PlaybackEvent.cs`; Patron recommends `src/LamuFlix.Core/Domain/PlaybackEvent.cs` pending owner approval. Do not treat either resolution as approved.
- `src/LamuFlix.Core/Ports/IPlaybackHistory.cs`.
- `src/LamuFlix.Infrastructure/Persistence/EfPlaybackHistory.cs`.
- `src/LamuFlix.Infrastructure/Persistence/Records/PlaybackEventRecord.cs`.
- `src/LamuFlix.Infrastructure/Persistence/Configurations/PlaybackEventConfiguration.cs`.
- EF-generated `AddPlaybackEvents` migration and designer in `src/LamuFlix.Infrastructure/Persistence/Migrations/` (timestamp prefix selected by tooling).

Production modifications:

- `src/LamuFlix.Core/Features/Playback/PlayMovieCommandHandler.cs`.
- `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs`.
- `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`.
- EF-generated `src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs`.

Existing test modifications, limited to new dependency/schema consequences:

- `tests/LamuFlix.UnitTests/Features/Playback/PlayMovieCommandHandlerTests.cs`.
- `tests/LamuFlix.IntegrationTests/PlaybackEndpointTests.cs`.
- `tests/LamuFlix.IntegrationTests/ApiEndToEndPlaybackTests.cs`.
- `tests/LamuFlix.IntegrationTests/MigrationTests.cs`.
- `tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs`.
- `tests/LamuFlix.UnitTests/Persistence/PersistenceCompositionTests.cs`.
- New focused history unit and PostgreSQL integration tests within the existing test projects. These are proposed additions; no existing history tests were found by recon:40. Quill should name the focused files in tasks.md without expanding responsibilities.

Task order:

1. Domain record/port and handler test coverage.
2. Persistence record/configuration/context/adapter/DI and DB tests.
3. Generate migration and add migration regressions.
4. Wire handler clock/append flow and prove endpoint end-to-end behavior.
5. Verify unchanged API contract and run delivery gates.

Within this order, persistence tests that require the new schema execute after the generated migration is available; writing test cases earlier is not a requirement to run against a nonexistent schema.

Quill owns spec.md, plan.md and tasks.md. No new ADR or root glossary file. Keel later checks drafts against this brief; no artifact drafting or plan challenge performed in this ask.

## Gate expectations

No test, build, analyzer, format or mutation gate runs in this grill step. At delivery, use the live harness.yml and applicable skill/scripts for build, Roslyn analyzers, complexity, InspectCode, tests, property tests, security, formatting and mutation. Run the unchanged API drift check. Do not invent or lower thresholds. A gate that could not run has not passed. Report PASS, failure, scope-empty SKIPPED, opt-out SKIP and mutation N/A separately as the actual commands warrant. Any later missing fact goes to recon rather than an assumption. Basis: Q7, AGENTS.md verification/configuration rules.

## Q8 — Loop discipline

Closing bar: zero unresolved Critical, High or Medium findings in frozen scope. Low findings must be recorded and dispositioned. Applicable failing gates remain blockers.

Round cap: at most two review/remediation rounds; no third fix round or waived required gate. At the cap, unresolved blockers remain blocked.

Frozen scope: the exact ticket PlaybackEvent record, IPlaybackHistory port, handler recording on successful gated launch, one playback_events table and generated migration, necessary persistence/DI wiring and direct unit/PostgreSQL/migration/endpoint/contract regression proof specified by Q1–Q7; anything else is a follow-up issue, not a finding in this round.

Excluded: history reads, public history endpoints, progress/completion/resume tracking, user identity, UI/web work, watchlist changes, Movie aggregate or repository expansion, extra dependencies/projects/layers, existing table redesign, player execution/flag changes and unrelated cleanup. Discovery requiring a ticket change or necessary constitution departure uses the charter owner-checkbox process; C1 now requires an explicit owner checkbox as recorded below.

Grill closed at Q8 with shared understanding. ASSUMPTIONS.md has no `[assumed]` taste entries. Quill drafts next, followed by analysis and plan challenge/adjudication under the normal pipeline. Gate 1 stays closed until its normal prerequisites and approval; this scope agreement is not plan freeze, implementation authorization or merge approval.

## Phase 2 step 3.3 — Patron rulings after analysis round 1

These rulings supersede the original Q1/Q5/Q8 no-conflict/no-checkbox conclusions only where stated. The original exchanges remain append-only in CONCLUSIONS.md. Source: Patron message and `C:/Users/lamun/AppData/Local/Temp/patron-dev-351-c1-c3-rulings.txt`; citations below are Patron-supplied, not new Keel recon.

### C1 — blocked: structural

- [ ] Owner decision required before Gate 1: Should `PlaybackEvent(MovieId MovieId, DateTimeOffset PlayedAt)` live in shared `Core/Domain/` (or `Core/Ports/`) to satisfy constitution.md:133–135 and ArchitectureTests.cs:87–95, or should ArchitectureTests and Principle I be amended to permit Features/Playback (and Core/Ports) to reference Features/WatchHistory as literal ticket DEV-351:21 specifies?

Basis: DEV-351:21–22 places the record in Features/WatchHistory and requires PlayMovieCommandHandler in Features/Playback to consume it via IPlaybackHistory in Core/Ports. This conflicts with constitution.md:133–135 (feature folders MUST NOT reference each other; shared concepts belong in Domain/, Ports/, or Pipeline/) and ArchitectureTests.cs:87–95,291–309. Moving the record contradicts the ticket path; allowing the dependency departs from Principle I. Both routes therefore require the owner process under charter §2.3(a)/(b).

Patron recommends `src/LamuFlix.Core/Domain/PlaybackEvent.cs`, alongside shared Movie/MovieId types; Patron cites constitution.md:294 for Playback Event as a core domain term. This recommendation is not an owner answer. Carry the unchecked decision onto the spec PR; do not silently move the type, amend the constitution or ArchitectureTests, or claim Gate 1 open. WatchHistory feature logic remains in Features/WatchHistory if any is needed within frozen scope. No type-forwarding/alias workaround is authorized by this brief.

### C2 — resolved: existing whitelisted port

`IPlaybackHistory` in Core/Ports is already pre-approved by constitution.md:136–140 and ArchitectureTests.cs:28–29 PortWhitelistPattern. The ADR requirement at constitution.md:139–140,375 applies to expanding the closed list, not implementing this listed port required by DEV-351:22. No new ADR, constitution departure, or owner checkbox is required. Q3 remains otherwise unchanged.

### C3 — resolved: incremental migration

Generate the incremental third migration `AddPlaybackEvents` as already settled in Q4. DEV-351:23–24 explicitly requires the playback_events migration. Patron interprets constitution.md:342 single fresh Initial migration as the initial PostgreSQL replacement of legacy MySQL migrations, not a ban on subsequent migrations; established incremental AddMovieWriters and MigrationTests expectations are evidenced at recon-DEV-351:41,42,50. No owner escalation or constitution change is required.

### Draft correction handoff and gate state

Quill owns spec.md, plan.md, tasks.md and associated drafting artifacts. One numbered correction list for analysis round 1:

1. C1: propagate the unresolved owner checkbox consistently through the draft artifacts and spec PR material; keep the Domain location explicitly a recommendation awaiting owner decision, and remove claims that there is no owner checkbox or that the constitutional conflict is resolved.
2. C2: record the port as pre-whitelisted and the ADR concern resolved; do not introduce an ADR requirement.
3. C3: record incremental AddPlaybackEvents as approved and the Initial-only concern resolved; retain generated migration and regression requirements.
4. I1 High: tasks.md:126–133 must put spec analysis, plan challenge/adjudication and owner Gate 1 before implementation/delivery work. The implementation order in Q7 applies only after those prerequisites.
5. I2 Medium: remove T002 parallel marking at tasks.md:24,107 because T002 depends on T001; make that dependency sequential consistently.

C1 remains Critical and blocked pending owner decision. I1 and I2 remain open until Quill correction and read-only verification. This receipt does not consume a new analysis round, freeze the plan, authorize implementation, or open Gate 1. The original two-round cap and zero Critical/High/Medium closing bar remain in force.

## Phase 2 step 6 — Plan Challenge adjudication

All three axis reports were read in full: findings-DEV-351-Sentry:1-69, findings-DEV-351-Ledger:1-41, findings-DEV-351-Compass:1-36. Patron ruled on all 16 findings in DEV-351:161-220; the full ruling exchange is appended in CONCLUSIONS.md. This section supersedes Q4 only as stated and records the correction handoff; the draft is not frozen.

Q4 amendment (Ledger F1): playback_events.id is now integer generated identity / int Id, replacing bigint. The event primary key is independent of movie_id, so matching the FK target type is not the rationale. Patron chose the supplied repository integer identity convention because the ticket leaves Id width unspecified (DEV-351:24; findings-DEV-351-Ledger:12; Patron ruling DEV-351:189-191, charter §2.3.3). All other Q4 constraints and lifecycle decisions remain.

Other settled clarifications: use required MovieId and PlayedAt members and the equivalent timestamptz configuration literal; exact port-unit timestamps and deterministic DB-compatible fixed clock values; explicit sibling movie/event survival, endpoint append-failure verification, and real production-DI acceptance composition. Keep the Q2 non-atomic failure boundary, shared scoped context and existing feature vocabulary. No new abstraction, root glossary, exception mapper, API shape, cleanup, retry or deduplication scope is authorized.

### One numbered Quill correction list

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

### Adjudication state

Adjudication complete for all 16 findings from the three required axis reports. This is a decision receipt, not closure of draft corrections. Six accepted Medium findings remain pending Quill correction and read-only verification: Sentry M2/M3/M4, Ledger F1, Compass F1/F2. Sentry M1 is rejected as already settled; all nine Low findings are dispositioned above, with clarification work included in the single correction list. Existing C1/OD-1 remains Critical and unchecked; no plan freeze, Gate 1 opening, implementation authorization, delivery gate result, commit or merge approval.

The prior DEV-351:104,109,119 attribution of FR-013/FR-021/FR-022 to a T021 diff review and the resulting 33/33 (100%) planned-coverage claim are retracted pending the explicit task correction and read-only verification. The requirements exist; complete planned verification coverage has not yet been earned. The old Sentry summary at DEV-351:138-139 misidentifies L2/L3: the full source report defines L2 as delete-before-append race and L3 as shared-context tracked changes. This report uses those full source findings. Compass statement that OD-1 is carried on the spec PR is treated only as draft checkbox coverage; no actual PR publication was verified.

Work stayed within the four named notes and six artifacts for substantive evidence. Patron rulings were read from DEV-351:161-220; the outside-root Temp file was not read. Reviewer-only convention/citation claims are attributed as such rather than represented as independently verified recon. No production research, production writes or delivery gates ran. The original two-round cap and zero unresolved Critical/High/Medium closing bar remain unchanged; no third fix round is authorized. Next: Conductor routes the single numbered list to Quill (ruling-record correction in ASSUMPTIONS may be recorded by Quill/Rigger), then Keel verifies the corrected artifacts read-only. Owner resolves OD-1 through the spec PR before Gate 1.
