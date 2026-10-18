# Implementation Plan: DEV-352 Explicit watched and not-interested marks

**Branch**: `feature/352-spec` | **Date**: 2026-10-17 | **Spec**: `specs/DEV-352/spec.md`

**Input**: Feature specification from `specs/DEV-352/spec.md`, settled grill brief `specs/DEV-352/brief.md`, facts from `recon-DEV-352`, Patron rulings in `specs/DEV-352/CONCLUSIONS.md`.

**Note**: Planning artifact only, corrected under Patron Q12 after all three Phase 2 step 6 axis reports. Freeze status is recorded in the adjudication section below. No implementation gates have run. Build waits until the user resolves both structural owner checkboxes on the spec PR and that spec PR merges, which is what establishes Gate 1 (brief handoff). Q12 is recorded in brief.md before these decision-driven corrections.

## Summary

Add two independent movie marks, watched and not interested, on the existing movie. Expose four bodyless mark and unmark operations that follow the watchlist slice, persist the marks as two required boolean columns with a generated reversible migration, and return them on the existing list and details responses. Export the OpenAPI document from the C# contract, then let `npm run generate:api` write TypeScript and MSW handlers from that committed document, and update only the handwritten web files that contract forces. No on-screen control, browse filter, recommendation, history, or new dependency. The additive migration and the omitted per-command validators remain unresolved owner checkboxes.

## Technical Context

**Language/Version**: C# / .NET 10 backend (`specs/PRODUCT.md` product goals). Web changes are contract compatibility only on the existing Vite / React / TypeScript app.

**Primary Dependencies**: None added. Existing stack only: EF Core with PostgreSQL, the existing exception pipeline, xUnit, NSubstitute, Shouldly, Testcontainers PostgreSQL, Verify, and the existing generator. Export `web/src/api/openapi.json` from the C# contract first. `npm run generate:api` then reads that committed document and writes `web/src/api/types.ts` and `web/src/mocks/handlers.ts` (replacement recon-DEV-352:13-27). That npm script does not export OpenAPI.

**Storage**: PostgreSQL. Two new required boolean columns on the existing `movies` table, `is_watched` and `is_not_interested`, default false for preexisting rows. No feedback table. The existing `is_in_watchlist` column stays.

**Testing**: xUnit, Shouldly, NSubstitute, and the existing property-test library. Domain and property proofs stay in the test files recon names. Handler proofs use a stand-in repository. HTTP proofs use the existing integration host with a stand-in repository for orchestration only. Persistence, serialization, and migration proofs use real PostgreSQL via Testcontainers. No EF InMemory and no mocked data-access driver.

**Target Platform**: The existing API host. No new host.

**Project Type**: Existing ports-and-adapters solution. No new project or layer. The new folder is `src/LamuFlix.Core/Features/Feedback`, which the brief places inside the existing Core features area.

**Performance Goals**: No target is set in the brief. No number is invented.

**Constraints**: TreatWarningsAsErrors. No new NuGet or npm dependency. No new project, port, repository, or service abstraction. No change to `Features:LocalPlay`, secrets, or `Process.Start`. No hand-edited migration, generated TypeScript, or generated MSW handlers. No UI control, browse filter, recommendation, timestamp, or identity. Existing watchlist behavior stays. Q9 fixes the conflict action and state tokens. Q10 names `tests/LamuFlix.IntegrationTests/MigrationTests.cs`. Two structural owner checkboxes stay unresolved in the constitution check below. Gate 1 stays closed.

**Scale/Scope**: Single-user library (constitution known debt, line 525; brief Q1). Four operations and two columns. The file set is the brief's authorized envelope. The migration filename is a tooling output, not a name chosen here. Q12 names EfMovieRepository.cs and EfMovieCatalog.cs as the existing persistence and projection sites; no new mapper is added.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

This check does not pass. Two unresolved structural owner checkboxes remain. The historical grill statement of zero owner checkboxes and zero constitution departures is explicitly superseded by Q11. No Patron ruling, drafting fix, or prerequisite success resolves either checkbox. Gate 1 stays closed until the user resolves both on the spec PR and the spec PR merges. Conditional planning freeze never resolves those owner decisions or authorizes implementation.

- [ ] blocked: structural — Approve departing from .specify/memory/constitution.md:219-220 (all commands validated through FluentValidation in the validation decorator) for the four MovieId-only feedback commands by validating route id via MovieId.TryCreate (HTTP 422) at the endpoint boundary without separate IValidator<TCommand> classes, while retaining the existing validation decorator through AddHandler?
- [ ] blocked: structural — Approve departing from .specify/memory/constitution.md:335-342 (single fresh Initial migration without constitution amendment/ADR) by generating a reversible additive EF migration for movies.is_watched and movies.is_not_interested without editing 20260930153520_Initial.cs?

- Ticket scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-352: four routes from the ticket title, the status and operation rulings in Q4, and the read fields in Q5. Nothing is added or dropped. The historical "no owner checkbox" statement in brief Q6 and Q8 is superseded by the two checkboxes above.
- No new dependency, project, or layer (constitution I, lines 114–147; PRODUCT.md care list 1–2). Feedback reuses `IMovieRepository` and does not add a port (constitution I, lines 136–140). The feature folder does not reference the Watchlist feature folder; shared state stays on `Movie` in Domain (constitution I, lines 133–135). Patron Q6 ruled the Feedback folder is not a new layer. That folder ruling is not one of the two Q11 departures.
- Handlers are sealed command records and sealed handlers registered with `AddHandler` (constitution II, lines 149–157). No MediatR. The existing validation decorator stays wired through `AddHandler`. Endpoint `MovieId.TryCreate` still returns HTTP 422 for a nonpositive id. Separate `IValidator<TCommand>` classes for the four MovieId-only commands are omitted only as the first unresolved checkbox above. That omission is pending user approval. It is not a compliant or approved exception (constitution lines 219–220; Q11).
- New movie transitions are explicit methods that throw `InvalidTransitionException` on an illegal repeat, with unit tests for legal and illegal transitions (constitution IV, lines 190–194; workflow rule lines 379–380). The method list at line 192–194 is the set that exists today. Q4 and Q6 add the four feedback methods and Patron judged that not a departure. Q9 fixes the pairs: method-name actions and const state tokens MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, and UnmarkNotInterested/Interested. Interested means only `IsNotInterested == false`. Domain tests and HTTP 409 tests assert those exact pairs. The Interested wording is the `[assumed]` entry in `ASSUMPTIONS.md`.
- Errors stay on the single `IExceptionHandler` (constitution V, lines 217–233). 409, 404, and 422 reuse that mapping. 400 for a malformed or overflowing id reuses the existing binding behavior (brief Q4). `GlobalExceptionHandler.cs` is not given a second map.
- No browse-filter change (constitution III, lines 168–188). `MovieQuery.InWatchlist` stays. No watched or not-interested query key is added (brief Q3, Q5).
- Public API shape is the four ticket routes plus the two additive read fields Patron authorized under care-list items 4 and 6 (constitution API and Contract Rules, lines 483–495; brief Q4, Q5). Line 488 names 204 for watchlist and play. Q4 extends that empty-204 shape to these four operations and Patron judged that not a departure. Export `web/src/api/openapi.json` from the C# contract first. `npm run generate:api` reads that document and writes `web/src/api/types.ts` and `web/src/mocks/handlers.ts`. It does not export OpenAPI. Neither generated file is hand-edited. The Verify baseline stays in lockstep with the exported document. Q11 records MSW generation as a compliant artifact correction of constitution lines 492–494, not a departure: `handlers.ts` is generated. The original Q11 handwritten envelope is `web/src/features/movie-details/movieDetailsApi.ts`, `web/src/api/client.typecheck.ts`, `web/src/mocks/fixtures.ts`, and `web/src/mocks/handlers.test.ts` (replacement recon-DEV-352:38-68). `web/src/mocks/generate-api.test.ts` is in that same forced-compatibility envelope: replace the ten-operation lock with those ten plus the four approved operation identities (14), and keep the byte and staleness checks (replacement recon-DEV-352:29-34).
- Database and migrations (constitution Technology Stack Constraints, lines 335–342): the stack note says a single fresh Initial migration without a constitution amendment or ADR. Q2 authorizes one new generated additive migration and forbids editing `20260930153520_Initial.cs`. Q6 historically recorded that as not a constitution departure and not an owner checkbox. Q11 explicitly supersedes that statement. The additive migration is the second unresolved checkbox above. It is pending user approval. It is not an approved exception, and this plan does not resolve it.
- No secret, machine path, clock, or `Features:LocalPlay` change (constitution VII, lines 263–277). These marks have no timestamp, so no `TimeProvider` is introduced for them.
- Vocabulary (constitution VIII, lines 284–299): Feedback is already the glossary term for explicit Watched / Not Interested marks (brief Q1/Q6; historical intake recon-DEV-352:11, CONTEXT.md:99; the current recon snapshot does not reverify this citation). No new glossary term, no root `CONTEXT.md` rewrite, and no ADR (brief Q6; constitution Documentation Rules, lines 497–507).
- Tests use the existing pyramid (constitution IX, lines 304–325). Brief Q7 and the historical intake recon place domain and property tests in `tests/LamuFlix.Test/Domain/` and handler tests in `tests/LamuFlix.UnitTests/Features/Watchlist/`. New proofs follow those existing homes. Gates are the configured scripts with no threshold change (Static-Analysis Gates, lines 400–420; PR Quality Gates, lines 368–398; brief Q8). Resolve the applicable refactor gate from live configuration and documented tooling at implementation; do not prescribe a literal threshold override (Q12).

Post-design re-check: the design below adds no port, package, browse filter, identity, timestamp, or separate table. It does not edit the exception map, the initial migration, or the root glossary. Q9 and Q10 close the two former drafting gaps. The two owner checkboxes above stay unresolved. The same gates hold only after the user resolves both checkboxes and the spec PR merges. Freeze status is separate from the unresolved constitution check and appears below.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-352/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes approach, files, and order, and this ask writes the spec, plan, tasks, and requirements checklist. The public contract is the four operations and two read fields already fixed in the brief, so no new contract file is opened. `AGENTS.md` is not edited: this ask does not name it, and the spec-kit marker update would touch a file outside the feature directory.

### Source Code (repository root)

```text
src/LamuFlix.Core/Domain/Movie.cs
src/LamuFlix.Core/Features/Feedback/
    MarkWatchedCommand.cs
    MarkWatchedCommandHandler.cs
    UnmarkWatchedCommand.cs
    UnmarkWatchedCommandHandler.cs
    MarkNotInterestedCommand.cs
    MarkNotInterestedCommandHandler.cs
    UnmarkNotInterestedCommand.cs
    UnmarkNotInterestedCommandHandler.cs
src/LamuFlix.Core/Ports/MovieSummary.cs
src/LamuFlix.Core/Ports/MovieDetails.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs
src/LamuFlix.Infrastructure/Persistence/Records/MovieRecord.cs
src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/
    <generated migration and designer>
    LamuFlixDbContextModelSnapshot.cs
src/LamuFlix.Api/Endpoints/FeedbackEndpoints.cs
src/LamuFlix.Api/Endpoints/ApiEndpoints.cs
src/LamuFlix.Api/Endpoints/LibraryResponses.cs
src/LamuFlix.Api/HandlerRegistration.cs

tests/LamuFlix.Test/Domain/MovieTests.cs
tests/LamuFlix.Test/Domain/PropertyTests.cs
tests/LamuFlix.UnitTests/Features/Feedback/
    <handler tests mirroring the watchlist handler tests>
tests/LamuFlix.IntegrationTests/FeedbackEndpointTests.cs
tests/LamuFlix.IntegrationTests/ApiEndToEndLibraryTests.cs
tests/LamuFlix.IntegrationTests/ApiEndToEndEnrichmentTests.cs
tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs
tests/LamuFlix.IntegrationTests/ApiEndToEndBrowseTests.cs
tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs
tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json
tests/LamuFlix.IntegrationTests/MigrationTests.cs

web/src/api/openapi.json
web/src/api/types.ts
web/src/api/client.typecheck.ts
web/src/features/movie-details/movieDetailsApi.ts
web/src/mocks/handlers.ts
web/src/mocks/fixtures.ts
web/src/mocks/handlers.test.ts
web/src/mocks/generate-api.test.ts
web/src/library/libraryQueries.ts
web/src/api/client.test.ts
web/src/App.test.tsx
web/src/library/libraryQueries.polling.test.tsx
web/src/library/LibraryView.retry.test.tsx
web/src/library/MovieCard.layout.test.tsx
web/src/library/MovieCard.test.tsx
web/src/library/MovieGrid.test.tsx
web/src/library/watchlist.integration.test.tsx
web/src/library/watchlistListFilter.test.tsx
```

**Structure Decision**: Existing projects only. New command and handler file names mirror `AddToWatchlistCommand.cs` and `AddToWatchlistCommandHandler.cs` (brief Q6; historical intake recon-DEV-352:32–33, not the current MSW snapshot) under `Features/Feedback`. `FeedbackEndpointTests.cs` mirrors `WatchlistEndpointTests.cs` (brief Q7; historical intake recon-DEV-352:39, not the current MSW snapshot). Q12 explicitly authorizes EfMovieRepository.cs and EfMovieCatalog.cs, including the baseline/delta update path as well as insert/restore and browse/details projections. Earlier missing-path statements are superseded; no broader refactor is authorized. Q10 places the migration proof in the existing `tests/LamuFlix.IntegrationTests/MigrationTests.cs`. `WatchlistEndpoints.cs` and `GlobalExceptionHandler.cs` stay as they are. `20260930153520_Initial.cs` is not edited. `web/src/api/types.ts` and `web/src/mocks/handlers.ts` are generated outputs. The forced handwritten envelope is the four files Q11 names plus `web/src/mocks/generate-api.test.ts`, extended only by the Phase 6 ruling below.

### Phase 6 T021 envelope amendment (2026-10-18)

Patron Phase 6 ruling (brief.md Phase 6 supplement; CONCLUSIONS.md; recon-DEV-352-2:1-44) additionally authorizes exactly `web/src/library/libraryQueries.ts`, `web/src/api/client.test.ts`, `web/src/App.test.tsx`, `web/src/library/libraryQueries.polling.test.tsx`, `web/src/library/LibraryView.retry.test.tsx`, `web/src/library/MovieCard.layout.test.tsx`, `web/src/library/MovieCard.test.tsx`, `web/src/library/MovieGrid.test.tsx`, `web/src/library/watchlist.integration.test.tsx`, `web/src/library/watchlistListFilter.test.tsx`. In these ten files only, add `isWatched: false` and `isNotInterested: false` to `pendingRow` and existing response literals/factory defaults, retaining existing override spreads. No UI controls, branching logic or new abstractions. `web/src/features/movie-details/MovieDetailsView.tsx` and `web/src/test/handlers.ts` remain excluded from direct edits: fix their transitive errors through the already-authorized `movieDetailsApi.ts` schema and `mocks/fixtures.ts`. Any required direct edit there needs new recon and a new Patron ruling. T025 separate verdicts and the green T016-T021 commit group remain unchanged.

No new owner checkbox or delivery behavior; prior freeze history remains historical.

## Design Overview

1. **Movie.** In `Movie.cs`, add non-null `IsWatched` and `IsNotInterested`, both false for a new movie. Append the two restore parameters after the existing `isInWatchlist` parameter on `Rehydrate`, in that name order, without reordering existing parameters. Add `MarkWatched`, `UnmarkWatched`, `MarkNotInterested`, and `UnmarkNotInterested`. Each legal call flips only its mark. A repeat throws `InvalidTransitionException` and leaves all three flags unchanged. The action token is the method name. The state token is a const string: MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, UnmarkNotInterested/Interested (Q9). Interested means only `IsNotInterested == false`, the `[assumed]` wording in `ASSUMPTIONS.md`. Domain tests and HTTP 409 tests assert those exact pairs. Do not add a timestamp or clear the watchlist.
2. **Storage.** In `MovieRecord.cs` and `MovieConfiguration.cs`, add `is_watched` and `is_not_interested` after `is_in_watchlist`, required booleans mapped like the existing watchlist flag with `.HasColumnType("boolean").IsRequired()`, without `.HasDefaultValue(false)`. Domain flags start false; the generated migration supplies and tests false backfill/default for preexisting rows. Do not edit `20260930153520_Initial.cs`. Generate one migration, its designer, and the updated `LamuFlixDbContextModelSnapshot.cs` with EF tooling. Do not hand-edit those generated files. Upgrade adds the two columns and fills preexisting rows with false. Downgrade drops only those two columns.
3. **Mapping.** In `EfMovieRepository.cs`, extend `ToRecord`, `ToDomain`, `Baseline`, `Baseline.From` and `ApplyState` for both marks, preserving per-field delta application. In `EfMovieCatalog.cs`, extend `BrowseAsync`, `GetDetailsAsync` and `MovieDetailsRecord` so the two new marks round-trip and appear on `MovieSummary`, `MovieDetails`, `MovieSummaryResponse`, and `MovieDetailsResponse`. Append the new properties after `IsInWatchlist` in the name order watched then not interested. Do not add a mapper, a port, or a Mapster registration. Capture both flags in the baseline and apply a record property only when its corresponding domain flag changed since load. Never write a stale sibling value. Q12 authorizes these existing sites.
4. **Commands.** Four sealed records and four sealed handlers in `src/LamuFlix.Core/Features/Feedback`. Each handler loads with `IMovieRepository.GetAsync`, throws `NotFoundException` when the movie is missing, calls the matching movie method, saves once with `SaveChangesAsync`, and returns `Unit.Value`. Forward the supplied CancellationToken to both asynchronous repository calls and test forwarding for every operation. A missing movie or a rejected transition saves zero. Register each handler with `AddHandler` in `HandlerRegistration.cs`, appended after the existing registrations, in the brief's operation order. Concurrent sibling writes must survive stale-load interleaves through the extended delta path. Same-target requests that both loaded a valid pre-state may both return 204; a repeat observing committed target state returns 409. No new locks or concurrency tokens (Q12).
5. **Endpoints.** Add `FeedbackEndpoints.cs` mirroring `WatchlistEndpoints.cs`: untyped `{id}`, `.Produces(204)`, `.ProducesProblem` for 400, 404, 409, and 422, a local `ParseMovieId` that uses `MovieId.TryCreate` and the same id error the watchlist endpoint already raises (brief Q4; historical intake recon-DEV-352:29, not the current MSW snapshot), and `TypedResults.NoContent()` on success. Do not edit `WatchlistEndpoints.cs`. Append `MapFeedbackEndpoints` in `ApiEndpoints.cs` without reordering the existing maps. Do not edit `GlobalExceptionHandler.cs`.
6. **Reads and contract.** Add the two required bools on the Core ports and on `LibraryResponses.cs`. Update exact `ShouldCarryExactly` assertions in the library, browse, enrichment, and import end-to-end tests named in brief Q7 and the historical intake recon. Export committed `web/src/api/openapi.json` from the C# contract with the existing flow first. Then run `npm run generate:api`, which reads that document and writes `web/src/api/types.ts` and `web/src/mocks/handlers.ts`. That command does not export OpenAPI. Update the Verify baseline. The original Q11 handwritten envelope is `web/src/features/movie-details/movieDetailsApi.ts`, `web/src/api/client.typecheck.ts`, `web/src/mocks/fixtures.ts`, and `web/src/mocks/handlers.test.ts`, and only where the new required fields or the four operations force a failure. In `web/src/mocks/generate-api.test.ts`, replace the ten-operation lock with those ten plus the four approved operation identities (14) and keep the byte and staleness checks. Do not hand-edit `types.ts` or `handlers.ts`. Do not add a UI control or a browse filter. Applicable web typecheck, runtime-schema and contract regression tests, web tests, web build, and generated-output drift checks are scheduled in `tasks.md` with separate verdicts, including this generator regression. Lint alone does not schedule that set. Execution is implementation-stage work.
7. **Proof order.** Domain transition tests and property coverage, then persistence and the generated migration, then handler tests and handlers, then the HTTP matrix and endpoints, then read fields and projections, then generated contracts and forced web compatibility, then real-database round-trip and migration proofs, then the applicable gates and the M-tier review. The brief's trailing "property regressions" are the property-test gate in that last step. The property cases themselves are authored with the domain tests (brief Q7 item 1). Preserve task order, but group Phases 4–6 into one green commit after T021; existing OpenAPI baseline and forced compatibility checks may be red only in that uncommitted group. Update the migration tripwire in T006 before the Phase 2 commit. That reconciliation is the brief's own split, not a new test suite.
8. **Real database.** Extend `ApiEndToEndLibraryTests.cs` for all eight `(IsWatched, IsNotInterested, IsInWatchlist)` combinations through save, fresh-context reload, browse and details serialization, explicitly including asymmetric feedback pairs. Prove stale-load interleaves in separate request contexts for each distinct pair of the three flags in both actor/save orders: A loads, B loads and commits a sibling change, A changes its own flag and commits; a fresh read must retain both. Prove allowed same-target valid-pre-state overlap followed by committed-state repeat conflict (Q12). Prove upgrade and downgrade in the existing `tests/LamuFlix.IntegrationTests/MigrationTests.cs` (Q10). Each proof calls `PostgresFixture.CreateEmptyDatabaseContextAsync` for its own database and never downgrades the shared fixture database. Up calls `IMigrator.MigrateAsync("20261016195556_AddMovieWriters")` then seeds a nonempty set of prior-schema movie rows and captures their count/data before `MigrateAsync(null)`; assert a positive unchanged count, preserved data and both flags false after Up. Down calls `MigrateAsync(null)` and then `MigrateAsync("20261016195556_AddMovieWriters")`. Assert both new columns are required and default false, preexisting rows and the prior schema and data survive both directions, and Down removes only those two columns. T006 updates the existing migration count/name tripwire from 2 to 3 immediately with generation, retaining exact previous names and asserting the generated name. T023 retains that check while proving Up/Down. This proof does not resolve the additive-migration owner checkbox.

## Loop discipline

- **Closing bar:** Critical and High findings block. Medium and Low remain recorded. A mandatory gate failure or `Could not run` blocks. Scope-empty `SKIPPED`, opt-out `SKIP`, and mutation `NOT APPLICABLE` stay distinct and are never recorded as PASS (brief Q8).
- **Axes:** This size:M ticket requires Sentry/Risk, Ledger/Standards, and Compass/Spec reports before adjudication. A missing axis is blocked, never skipped.
- **Round cap:** At most two formal review and remediation rounds. Unresolved blockers at the cap remain blocked. No third round, no severity demotion, no gate waiver, and no silent scope expansion.
- **Frozen scope:** The four Q4 operations; independent watched and not-interested flags and unchanged watchlist semantics; additive reversible movies persistence; observable browse and detail fields; generated contract propagation and forced compatibility edits; the Q7 tests and applicable Q8 gates. Excluded: UI controls, browse filters, recommendations and ranking, timestamps and history, identity, separate tables, new dependencies or layers, LocalPlay, secrets, and unrelated refactors. Anything else is a follow-up issue, not a finding in this round.

## Complexity Tracking

Q11 supersedes the historical Q6 statement of zero constitution departures and zero owner checkboxes. Two necessary departures remain unresolved owner checkboxes in the constitution check. No Patron ruling, drafting fix, or prerequisite success resolves them. Gate 1 stays closed. Conditional freeze is recorded below and leaves both owner approvals unresolved.

| Tension | Ruling already made | Quill does not |
| --- | --- | --- |
| Constitution lines 219–220 require FluentValidation for every command inside the validation decorator | The proposed design validates route id with `MovieId.TryCreate` (HTTP 422) and keeps the existing decorator through `AddHandler`, without separate `IValidator<TCommand>` classes. That omission is the first unresolved checkbox. It is pending user approval | Treat the omission as a compliant or approved exception, or drop `MovieId.TryCreate` or the existing decorator |
| Constitution lines 335–342 require a single fresh Initial migration without constitution amendment or ADR | Q2 authorizes one generated additive migration and forbids editing Initial. Q6 historically said this is not a departure and not an owner checkbox. Q11 supersedes that statement. The additive migration is the second unresolved checkbox | Treat the departure as approved, edit `20260930153520_Initial.cs`, or add an ADR or constitution amendment in this ticket |
| Constitution lines 133–134 and 192–194 name today's feature folders and transition methods | Q6 authorizes `Features/Feedback` and the four methods inside the existing Core. That folder ruling is outside the two Q11 checkboxes | Treat that folder as a new layer or add a port |
| Conflict action and state tokens; migration-test filename | Q9 fixes method-name actions and const state tokens MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, UnmarkNotInterested/Interested. Interested means only `IsNotInterested == false` and is `[assumed]` in `ASSUMPTIONS.md`. Q10 places the proof in existing `MigrationTests.cs` with the isolation, `IMigrator` targets, column, preservation, and 2-to-3 tripwire rules in design item 8 | Reopen either closed drafting question |

## Phase 2 step 6 — Plan challenge adjudication and conditional freeze

All three round-1 reports were read before adjudication: findings-DEV-352-Sentry:1-67, findings-DEV-352-Ledger:1-72, findings-DEV-352-Compass:1-33. Original severities are retained below. Patron Q12/12 is appended in CONCLUSIONS.md and summarized in brief.md before these edits. No further grill question or review round was opened.

Evidence refresh: recon-DEV-352:74-127 now appends the repository/projection facts without replacing the MSW snapshot at :1-72. Repository insert/restore and baseline capture are at :80-84, per-property delta/save at :88-94, fresh browse/details projections at :98-101, sibling-write preservation at :103-116 and same-target overlap at :118-127. Feedback behavior is a planned extension, not existing executed proof. DI lifetime and model definitions outside those recon roots were not verified; integration proofs must use explicit separate contexts. Earlier intake/migration citations remain historical, not independently reverified; brief Q1-Q10 and the append-only conclusions preserve their decision bases.

| Finding | Original severity | Adjudication and corrected planning evidence |
| --- | --- | --- |
| Sentry H1 | High | Closed at planning level by Q12: the whole-row-overwrite premise does not match the existing per-field delta mechanism (recon:88-110). Extend that seam for feedback rather than add concurrency infrastructure. Design 3/4/8 and T005/T022 explicitly preserve sibling writes and test stale-load interleaves. Same-target valid-pre-state overlap may return two 204s; observed committed-state repeat is 409. Severity was not demoted; the evidence and correction resolve the issue. |
| Ledger H1 | High | Accepted and corrected. Name EfMovieRepository.cs and EfMovieCatalog.cs in the envelope; T005 explicitly extends ToRecord, ToDomain, Baseline, Baseline.From and ApplyState; T019 extends BrowseAsync, GetDetailsAsync and MovieDetailsRecord. T022 verifies persisted updates as well as inserts/reads. |
| Sentry M1 | Medium | Accepted and corrected by all-eight real-DB save/reload/browse/details matrix in T022, including both asymmetric feedback pairs. |
| Sentry M2 | Medium | Accepted and corrected: T023 seeds a nonempty prior-schema database before Up and asserts positive unchanged row count, original data and both false flags. |
| Sentry M3 | Medium | Accepted residual under Q4/Q12. Commit followed by lost response can make a retry return already-state 409. Existing read endpoints reveal current state; no automatic retry or idempotency machinery is added. Explicit cancellation forwarding is scheduled in T007-T014. |
| Ledger M1 | Medium | Accepted and corrected in design 2/T004. Required boolean model mapping mirrors watchlist without HasDefaultValue(false); generated migration backfill/default is checked in T006/T023. No generated file hand-edit. |
| Compass M1 | Medium | Accepted and corrected. Historical intake/migration citations are explicitly labeled; MSW citations retain their snapshot identity; new persistence recon is append-only at :74-127. Historical source snapshots themselves are not found in current roots; no reconstructed evidence or widened search is claimed. |
| Compass M2 | Medium | Accepted and corrected. Tripwire update moves to T006 before Phase 2 commit. Phases 4-6/T016-T021 form one green commit group after endpoint, read, baseline and generated-contract/compatibility work; no known-red intermediate commits. Task order is unchanged. |
| Sentry L1 | Low | Recorded, mitigated by unchanged complete T016 matrix through the real exception pipeline. Historical pipeline precedent is not independently found in current recon roots. No additional source inspection or separate mapping is authorized. |
| Sentry L2 | Low | Accepted residual under Q2/Q12. Down destroys feedback values; later Up sets them false. No zero-downtime/deploy-order guarantee is introduced. |
| Ledger L1 | Low | Patron Q12: noted, no ticket. Existing feature-list documentation staleness does not require a new constitution departure for the authorized Feedback folder. No constitution rewrite. |
| Ledger L2 | Low | Accepted and corrected. T007-T010 each explicitly prove HandleAsync_ForwardsCancellationToken for GetAsync and SaveChangesAsync; T011-T014 carry the implementation requirement. |
| Ledger L3 | Low | Patron Q12: noted, no ticket. Preserve existing domain/property and handler test homes; no test-project move or constitution rewrite. |
| Compass L1 | Low | Accepted consistency correction despite the documented flag noted by Ledger. Remove literal threshold override from T024; both complexity invocations use live configuration and applicable documented procedure. No threshold is invented or lowered. |
| Compass L2 | Low | Accepted and corrected with Sentry M1: T022 explicitly stores all eight combinations through PostgreSQL and both read projections. |
| Compass L3 | Low | Accepted and corrected. T025 schedules an explicit separate OpenAPI snapshot/drift verdict in addition to dotnet test and separate web verdicts. |

**Freeze receipt (2026-10-17):** Plan challenge round 1 adjudicated, 16/16 findings disposed; zero unresolved Critical/High plan-challenge findings. Corrected spec/plan/tasks/checklist are aligned to brief Q12, with 26 task identities and the existing 14 FR/7 SC retained. Plan is frozen for spec-PR handoff, conditionally on the two Q11 owner decisions before execution. This is not constitution clearance: both structural checkboxes remain unchecked, Gate 1 stays closed, and implementation is not authorized. User must resolve both checkboxes on the spec PR and merge it. No implementation gate PASS, publication, commit, push or merge is claimed by this receipt. Original two-round cap, closing bar and scope exclusions remain unchanged.
