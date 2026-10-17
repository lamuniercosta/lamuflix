# DEV-352 Grill conclusions

Authority: Patron decides under charter §2.3. Facts: recon-DEV-352 at HEAD 1cdb13c487c1b257f20cc4d2466d31414190a814. Grill cap: 12 questions. Keel records the complete decision exchanges; no production implementation is authorized by this document.

## Q1 — Domain model

Keel asked: I recommend two independent single-user movie flags IsWatched and IsNotInterested, both initially false, with Watched meaning explicitly marked seen and Not Interested meaning explicit negative preference, allowing both simultaneously. This avoids overloading enrichment state or inventing identity/history. Cost: no timestamps, recency ranking, or mutual exclusion. Confirm domain model and whether PRODUCT/constitution require a different decision; cite your basis.

Patron answered:
[from Patron] Q1/12 ruling: Confirmed — two independent single-user boolean flags `IsWatched` and `IsNotInterested` on `Movie` (both initially `false`, independent and simultaneously allowed, no timestamps/recency ranking/mutual exclusion in DEV-352).
- Basis 1: Ticket `DEV-352:11,14` ("POST/DELETE /api/movies/{id}/watched and /not-interested") defines two independent toggle endpoint pairs, and `specs/PRODUCT.md:17-18` defers Epic 9 (Recommendations / Taste Profile recency) while forbidding speculative abstractions.
- Basis 2: `.specify/memory/constitution.md:294,525` + `recon-DEV-352:11,21` establish single-user scoping (no cross-user identity) and define Feedback as explicit Watched / Not Interested marks, separate from `EnrichmentStatus` (`recon-DEV-352:20`).
- Basis 3: `recon-DEV-352:12-16` (`Movie.IsInWatchlist` / `MovieRecord.IsInWatchlist`) establishes the non-nullable boolean property pattern on `Movie` for single-user movie flags.

Implications: both flags can coexist; neither implies enrichment status, watch history, identity, or recency computation.

---
## Q2 — Storage and migration

Keel asked: I recommend two required boolean columns is_watched and is_not_interested on existing movies, both default false for preexisting rows, with Movie/MovieRecord rehydration and mapping round-trip and a new reversible EF migration plus generated snapshot. No separate table or identity/history infrastructure. This matches recon 12-16 and single-user scope at 21; cost is one schema migration. Confirm storage choice, additive upgrade and Down removing only these new columns, and authorization under charter 2.3 item 3.

Patron answered:
[from Patron] Q2/12 ruling: Confirmed and authorized under §2.3 item 3 — add two required boolean columns `is_watched` and `is_not_interested` (both default `false` for preexisting rows) on the existing `movies` table, with `Movie`/`MovieRecord` rehydration and mapping round-trip and a new reversible EF migration (`Up` additive only, `Down` dropping only these two columns) plus updated model snapshot; no separate table or identity/history infrastructure.
- Basis 1: `recon-DEV-352:13-16,50` establishes the exact persistence precedent (`is_in_watchlist` required boolean on `movies` in `MovieConfiguration.cs:52-55`, `MovieRecord.cs:17`, `Movie.Rehydrate` at `Movie.cs:47-71`, and `20260930153520_Initial.cs:63`).
- Basis 2: `.specify/memory/constitution.md:525` and `recon-DEV-352:21` confirm single-user scope with no user/identity model, and `specs/PRODUCT.md:18` forbids speculative tables/abstractions not required by the ticket.
- Basis 3: `specs/PRODUCT.md:34,45` (§2.3 item 3) authorizes Patron to rule on database schema/migration changes required to store the ticket's `DEV-352:11,14` state.

Implications: preserve existing movie data and flags; no separate feedback table. Generate migration/snapshot through EF tooling rather than hand-editing generated artifacts.

---
## Q3 — Interaction with watchlist

Keel asked: I recommend complete independence from watchlist: all eight combinations of IsWatched/IsNotInterested/IsInWatchlist remain valid. Marking or unmarking either feedback flag never changes the other flag or watchlist; existing watchlist operations likewise preserve feedback. No implicit removal, recommendation filtering, browse filter additions, or UI actions. This avoids inventing side effects absent from ticket DEV-352:11,14 and preserves watchlist precedent recon 13,19. Cost is callers may retain watched or disliked movies on watchlist. Confirm this state-transition boundary with cited basis.

Patron answered:
[from Patron] Q3/12 ruling: Confirmed — complete independence between `IsWatched`, `IsNotInterested`, and `IsInWatchlist` (all eight boolean combinations valid; mutating any one flag never alters the other two; no implicit watchlist removal, recommendation filtering, or UI actions).
- Basis 1: Ticket `DEV-352:11,14` specifies only `POST/DELETE /api/movies/{id}/watched` and `/not-interested`, with no cross-flag side effects or UI/recommendation scope.
- Basis 2: `specs/PRODUCT.md:17-18` defers Epic 9 (Recommendations) and forbids unrequested speculative behavior.
- Basis 3: `.specify/memory/constitution.md:134-135,192-194` and `recon-DEV-352:13` require feature isolation in `Core` (Watchlist and Feedback/WatchHistory do not coupled-mutate each other) and explicit, single-concern transition methods on `Movie`.

Implications: tests must exercise cross-flag preservation in both directions. No browse/recommendation behavior is implied by feedback storage.

---
## Q4 — Endpoint contract

Keel asked: I recommend mirror watchlist: untyped {id} route binding to int; POST marks and DELETE unmarks, bodyless success 204; repeating the already-satisfied transition returns 409 ProblemDetails with no save, rather than idempotent 204. Unknown positive movie 404, nonpositive integer 422 with id error, malformed/overflow int 400; existing exception pipeline, no request DTO. Four distinct operation names MarkWatched, UnmarkWatched, MarkNotInterested, UnmarkNotInterested. Recon 27-36,39 is the precedent; cost is clients must handle repeated-toggle conflict. Confirm the entire four-operation public contract under charter 2.3 item 4 with cited basis.

Patron answered:
[from Patron] Q4/12 ruling: Confirmed and authorized under §2.3 item 4 — mirror `WatchlistEndpoints` across all four operations (`MarkWatched`, `UnmarkWatched`, `MarkNotInterested`, `UnmarkNotInterested`): untyped `{id}` route bound to `int`, bodyless `204 No Content` on success, `409 Conflict` (`ProblemDetails`, no save) via `InvalidTransitionException` on repeated already-satisfied transitions, `404` on unknown positive movie id, `422` (`errors["id"]`) on nonpositive integer via `MovieId.TryCreate`, and `400` on malformed/overflow route `int`, with no request/response DTO bodies.
- Basis 1: `recon-DEV-352:27-36,39` establishes `WatchlistEndpoints.cs` (`POST`/`DELETE /api/movies/{id}/watchlist`) as the direct structural and OpenAPI template (`.Produces(204)` + `.ProducesProblem` for 400/404/409/422, `ParseMovieId` -> 422, `InvalidTransitionException` -> 409).
- Basis 2: `.specify/memory/constitution.md:192-194,226-233,488` mandates intent-revealing transition methods on `Movie` throwing `InvalidTransitionException` (HTTP 409) on illegal transitions, `204` for membership/toggle endpoints, and unified RFC 7807 `ProblemDetails` mapping in the single `IExceptionHandler`.
- Basis 3: `specs/PRODUCT.md:34,46` (§2.3 item 4) authorizes Patron to rule on public API shape (routes from `DEV-352:11,14`, status codes, and operation names).

Implications: transition methods throw InvalidTransitionException on repeats; handlers do not save rejected transitions; OpenAPI declares 204, 400, 404, 409, 422 for all four operations. Existing watchlist contract stays intact.

---
## Q5 — Read DTO and generated contract impact

Keel asked: I recommend add required non-null bool IsWatched and IsNotInterested to both MovieSummary/MovieDetails ports and response DTOs, yielding isWatched/isNotInterested on browse and details. This makes marks observable across reads without adding endpoints or filters. Regenerate OpenAPI then types.ts through existing generator; update exact field-set locks and Verify baseline. Update existing handwritten runtime schema/typed fixtures only as needed for new fields; no UI controls or new client abstraction. Recon 17-19,40,44-45,59 supplies touchpoints; cost is additive wire/consumer updates. Confirm DTO change under 2.3 item 4 and bounded contract-driven source/test edits under item 6, or choose unchanged GET shapes.

Patron answered:
[from Patron] Q5/12 ruling: Confirmed and authorized under §2.3 items 4 and 6 — add required non-null `bool IsWatched` and `bool IsNotInterested` to `MovieSummary`/`MovieDetails` (Core ports) and `MovieSummaryResponse`/`MovieDetailsResponse` (`isWatched`/`isNotInterested` on browse and details), regenerate committed `web/src/api/openapi.json` and `web/src/api/types.ts`, update the Verify OpenAPI snapshot and `ShouldCarryExactly` integration test assertions, and update existing web Zod schemas/typed fixtures only as required by the contract chain (no new browse filters, UI controls, or client abstractions).
- Basis 1: `recon-DEV-352:17-18,40` shows `IsInWatchlist` is projected on both `MovieSummary`/`MovieDetails` (`MovieSummary.cs:7`, `MovieDetails.cs:13`) and `MovieSummaryResponse`/`MovieDetailsResponse` (`LibraryResponses.cs:17,19`), making bodyless `204` mutations observable through existing read endpoints without inventing new GET endpoints.
- Basis 2: `.specify/memory/constitution.md:356-357,381-382,492-493` and `recon-DEV-352:44-45,59` require OpenAPI (`web/src/api/openapi.json`), the Verify baseline (`OpenApiContractTests`), and generated TypeScript (`web/src/api/types.ts`) to stay in lockstep with C# DTOs so CI drift and typecheck gates pass.
- Basis 3: `specs/PRODUCT.md:34,46,48` (§2.3 items 4 & 6) authorizes Patron to rule on DTO field-set additions and forced downstream contract/test updates across tracked files.

Implications: both flags must survive persistence, projection, browse/detail serialization, and generated client typing. No handwritten duplicate wire interfaces. Web changes are contract compatibility only.

---
## Q6 — Implementation boundary and charter disposition

Keel asked: I recommend Core/Features/Feedback with four command-handler pairs named MarkWatched, UnmarkWatched, MarkNotInterested, UnmarkNotInterested, explicit corresponding Movie transition methods, and Api/Endpoints/FeedbackEndpoints registered in ApiEndpoints and HandlerRegistration. Reuse IMovieRepository GetAsync/SaveChangesAsync, Unit, exception pipeline and existing libraries; no new validator layer, repository, project, dependency, table, identity, LocalPlay/secrets/Process.Start changes. Authorize bounded edits to Movie, MovieRecord, MovieConfiguration, rehydration/persistence and port/response projection sites required by Q2/Q5, registrations, endpoint/test/contract files, and migration generated tooling. No unrelated rewrite/deletion, no root glossary rewrite or ADR for this M ticket; canonical feedback definitions stay in feature brief/conclusions. Confirm architecture and charter 2.3 disposition, including whether any ticket contradiction or constitution departure needs an owner checkbox.

Patron answered:
[from Patron] Q6/12 ruling: Confirmed and authorized across §2.3 items 1–6 with zero owner checkboxes (no ticket contradiction or constitution departure) — implement `src/LamuFlix.Core/Features/Feedback` with four sealed command/handler pairs (`MarkWatched`, `UnmarkWatched`, `MarkNotInterested`, `UnmarkNotInterested`), corresponding explicit transition methods on `Movie`, and `src/LamuFlix.Api/Endpoints/FeedbackEndpoints.cs` registered in `ApiEndpoints` and `HandlerRegistration`, reusing `IMovieRepository` (`GetAsync`/`SaveChangesAsync`), `Unit`, and the existing exception pipeline; no new dependencies, projects, ports, ADR, or root `CONTEXT.md` rewrite.
- Basis 1: `.specify/memory/constitution.md:134-140,151-156,192-194,294,485` and `recon-DEV-352:11,24-36,48-49` establish the exact feature-slice (`Core/Features/<Area>`), `AddHandler<…>` registration, `MapXxxEndpoints` grouping, and `IMovieRepository` reuse pattern (`Watchlist` precedent), while `Feedback` ("Explicit Watched / Not Interested marks") is already defined in `CONTEXT.md:99` and no new port or architectural choice is introduced (`.specify/memory/constitution.md:139,299,500`).
- Basis 2: `specs/PRODUCT.md:34,42-48` (§2.3 items 1–6) and `recon-DEV-352:48-53`: items 1, 2, 5 require no new dependency/layer/LocalPlay/secrets; items 3, 4, 6 authorize the bounded edits to `Movie`, `MovieRecord`, `MovieConfiguration`, rehydration/persistence and `MovieSummary`/`MovieDetails` + response projection sites, DI/endpoint registrations, EF migration + snapshot, OpenAPI/Verify/generated TS contract chain, and corresponding unit/integration/web test files.
- Basis 3: `specs/PRODUCT.md:36-40` — everything aligns strictly with ticket `DEV-352:11,14` and `.specify/memory/constitution.md`, so no `blocked: structural` owner checkbox is required.

Implications: bounded existing-layer changes only. No owner checkbox is created by these decisions; Gate 1 still requires user approval of the subsequent spec PR.

---
## Q7 — Test strategy

Keel asked: I recommend domain transition-table tests for each flag including wrong-state action/state exception metadata and all eight flag combinations/cross-flag preservation; handler tests success saves once, missing movie and repeat save zero; HTTP matrix for all four operations covering empty 204, 409 ProblemDetails, 404, 422 id errors and malformed/overflow 400. Real PostgreSQL/Testcontainers tests must exercise persistence/reload, browse/detail projection, existing watchlist preserving feedback, additive upgrade of preexisting rows to false, and migration Down preserving prior schema/data. Use repository substitutes only for handler/HTTP orchestration, never EF translation/serialization. Update existing membership properties to cover feedback sequences, field-set locks, OpenAPI Verify snapshot and web schema/typecheck tests as forced. No new assertion/mocking/test library. Cost is real DB migration/contract coverage. Confirm this test strategy with cited basis.

Patron answered:
[from Patron] Q7/12 ruling: Confirmed — adopt the recommended test strategy across domain/property, unit handler, HTTP endpoint, Testcontainers PostgreSQL persistence/migration (`Up` default `false` + `Down` rollback), OpenAPI Verify snapshot, and forced web contract/typecheck/schema tests, using existing test libraries only.
- Basis 1: `.specify/memory/constitution.md:306-325,350,379-382,392-393` mandates xUnit v3 + NSubstitute + Shouldly + AutoFixture/Faker.Net + FsCheck, unit tests for every legal/illegal `Movie` state transition, `WebApplicationFactory` endpoint integration tests, and real PostgreSQL via Testcontainers (never mocking EF translation/serialization).
- Basis 2: `recon-DEV-352:38-45` establishes the exact test precedents to mirror and extend (`MovieTests.cs:87-112`, `PropertyTests.cs:183-461`, `AddToWatchlistCommandHandlerTests.cs`, `WatchlistEndpointTests.cs`, `ApiEndToEndLibraryTests.cs:137` + `ShouldCarryExactly` locks, `OpenApiContractTests.cs:33`, and `web/src/api/client.typecheck.ts` / `handlers.test.ts` / `movieDetailsApi.ts`).
- Basis 3: `AGENTS.md` Non-negotiables ("Never mock the data-access driver for behaviour that depends on real query translation or serialisation — use Testcontainers" and "Match the repo's existing architecture, assertion library, and mocking library") and `specs/PRODUCT.md:12,48` (§2.3 item 6 for forced test/snapshot updates).

Implications: simulated repositories prove orchestration only. PostgreSQL is required for persistence, query/serialization and migration claims; downgrade checks preserve preexisting data, not the deliberately removed feedback columns.

---
## Q8 — Closure, task order, verification and loop discipline

Keel asked: I recommend task order domain and transition tests -> persistence migration/rehydration -> commands/handlers/registration -> endpoints -> read DTO/projections -> regenerate contracts and forced web compatibility -> integration/migration/property regressions -> applicable verification and review. Gates follow live harness configuration: build/tests, Roslyn, complexity, InspectCode, properties, vulnerable packages, formatting, OpenAPI/web drift and changed-project mutation without since; never lower thresholds or hand-edit generated outputs, and Could not run is not pass. M review requires all Sentry/Ledger/Compass axes. Critical and High block; Medium/Low stay recorded. At most two formal review/remediation rounds, unresolved blockers at cap stay blocked; no third round or scope expansion. Frozen scope is Q1-Q7 four feedback operations, independent flags, additive persistence, observable GET DTO fields and forced contract/test compatibility; anything else is a follow-up issue, not a finding in this round. No taste/UI choices, ASSUMPTIONS records none. Confirm loop terms and shared understanding so I can close grill at 8/12 and write final brief; no Gate 1, plan freeze or gate-pass claim.

Patron answered:
[from Patron] Q8/12 ruling: Confirmed — adopt the recommended task order, verification gate suite, M-tier three-axis review/remediation loop (Sentry/Ledger/Compass, Critical/High blocking, max 2 formal rounds), frozen Q1–Q7 scope, and empty taste assumptions (`ASSUMPTIONS.md` records none); close the grill at 8/12 and write `brief.md`.
- Basis 1: `AGENTS.md` Verification commands + Non-negotiables and `.specify/memory/constitution.md:368-420,492-493` mandate the full static-analysis, test, property, package vulnerability, format, OpenAPI/web drift, and changed-project Stryker mutation gates against `harness.yml` thresholds (never lowered, generated files never hand-edited, `Could not run` never treated as pass).
- Basis 2: `DEV-352:8,16` (`size:M`), `specs/PRODUCT.md:12,34`, and `lamuflix-team-charter` standing review/closure rules establish the 3-axis M-tier review (`Sentry`/`Ledger`/`Compass`), Critical/High blocking bar, 2-round remediation cap, and frozen Q1–Q7 scope (out-of-scope items handled per §2.3 follow-up rules, zero taste assumptions in `ASSUMPTIONS.md`).
- Basis 3: `specs/PRODUCT.md:32-40` — all Q1–Q8 questions are resolved with zero `blocked: structural` owner checkboxes; grill closes at 8/12 so Keel can finalize `specs/DEV-352/brief.md` (no Gate 1 or gate-pass claim yet).

Implications: grill closed at eight questions. Quill may draft from brief.md; this does not approve the resulting spec, open Gate 1, freeze a plan, or pass implementation gates.

---
## Q9 — InvalidTransitionException action/state token pairs

Keel asked: Recommend InvalidTransitionException action/state pairs: MarkWatched/Watched; UnmarkWatched/NotWatched; MarkNotInterested/NotInterested; UnmarkNotInterested/Interested. Action is attempted method; state is the already-current state causing repeat conflict. Interested here means only absence of the negative mark, not positive preference. Alternative NotNotInterested avoids that semantic implication; please select exact tokens deliberately. Basis brief Q1/Q4/Q7 and recon 13,42 watchlist action/state convention.

Patron answered:
[from Patron] Q9/12 ruling: Confirmed — select the exact `InvalidTransitionException(action, currentState)` token pairs `MarkWatched`/`"Watched"`, `UnmarkWatched`/`"NotWatched"`, `MarkNotInterested`/`"NotInterested"`, and `UnmarkNotInterested`/`"Interested"` (where `"Interested"` denotes only the unmarked `IsNotInterested == false` state, not positive preference, avoiding double-negative `"NotNotInterested"`), with zero owner checkboxes.
- Basis 1: `recon-DEV-352:13,42` (`Movie.cs:7-8,106-124` and `WatchlistEndpointTests.cs:66-92`) establishes `const string` state tokens paired with `nameof(<Method>)` (`AddToWatchlist`/`"InWatchlist"`, `RemoveFromWatchlist`/`"NotInWatchlist"`) surfaced via HTTP 409 `ProblemDetails`.
- Basis 2: `specs/DEV-352/brief.md` Q1/Q4/Q7 (`CONCLUSIONS.md` Q1, Q4, Q7) and `.specify/memory/constitution.md:192-194` require explicit transition methods on `Movie` throwing `InvalidTransitionException` with deterministic action/state metadata tested in unit and HTTP suites.
- Basis 3: `specs/PRODUCT.md:34,46` (§2.3 item 4) authorizes Patron to settle public error-contract tokens; choosing `"Interested"` over `"NotNotInterested"` is also logged `[assumed]` in `ASSUMPTIONS.md`.

Implications: `Movie` defines `const string` tokens `"Watched"`, `"NotWatched"`, `"NotInterested"`, and `"Interested"` for repeat-transition guards; unit and 409 endpoint tests assert these exact tokens.

---
## Q10 — Migration test host, isolation, and tripwire updates (T023)

Keel asked: Recommend existing tests/LamuFlix.IntegrationTests/MigrationTests.cs for T023, PostgresFixture.CreateEmptyDatabaseContextAsync per proof, IMigrator target 20261016195556_AddMovieWriters then latest for Up and latest then that target for Down; preserve seeded data and prior schema, assert both new columns required/default false and only those dropped, update existing migration count/name tripwire for the new generated migration. Never downgrade shared fixture database. Basis recon-DEV-352:74-93 and brief Q2/Q7; bounded existing test edit under Q6 item 6. Confirm no owner checkbox.

Patron answered:
[from Patron] Q10/12 ruling: Confirmed and authorized under §2.3 item 6 with zero owner checkboxes — place T023 migration proofs in `tests/LamuFlix.IntegrationTests/MigrationTests.cs` using `PostgresFixture.CreateEmptyDatabaseContextAsync` per test (never downgrading the shared fixture database), `IMigrator.MigrateAsync("20261016195556_AddMovieWriters")` -> `MigrateAsync(null)` for `Up` and `MigrateAsync(null)` -> `MigrateAsync("20261016195556_AddMovieWriters")` for `Down`, asserting `is_watched` and `is_not_interested` are `NOT NULL` defaulting to `false`, prior schema/seeded data survive both directions, only those two columns drop on `Down`, and updating the migration count/name tripwire in `MigrateAsync_EmptyDatabase_AppliesInitialAndHasNoPendingModelChanges`.
- Basis 1: `recon-DEV-352:74-93` establishes `tests/LamuFlix.IntegrationTests/MigrationTests.cs` as the sole migration-proof host, `CreateEmptyDatabaseContextAsync` (`LamuFlixDbContextFactory.cs:35-43`) as the throwaway-DB isolation mechanism protecting the 17-class `PostgresCollection` shared DB, `20261016195556_AddMovieWriters` as the immediate prior migration, and `:57-71` (`migrations.Length.ShouldBe(2)`) as the explicit migration-list tripwire.
- Basis 2: `specs/DEV-352/brief.md` Q2/Q7 (`CONCLUSIONS.md` Q2, Q7) and `.specify/memory/constitution.md:350` mandate real PostgreSQL verification of additive `Up` (`false` defaults on preexisting rows) and reversible `Down` (dropping only the two new columns while preserving prior schema and seeded rows).
- Basis 3: `specs/PRODUCT.md:34,48` (§2.3 item 6) authorizes bounded edits to `tests/LamuFlix.IntegrationTests/MigrationTests.cs`; no ticket contradiction or constitution departure exists, so no owner checkbox is needed.

Implications: T023 extends `MigrationTests.cs` using isolated throwaway databases per proof and updates the applied-migrations tripwire from 2 to 3; shared fixture database is never downgraded.

