# DEV-352 — Frozen grill brief

Status: grill closed with Patron at 8/12 questions on 2026-10-17. Quill drafts spec.md, plan.md and tasks.md from this brief. This is neither plan freeze nor Gate 1 approval nor implementation/gate success.

## Identity, evidence and authority

- Worktree: F:/Dev/LamuFlix.worktrees/feature-352-spec
- Branch: feature/352-spec
- Intake HEAD: 1cdb13c487c1b257f20cc4d2466d31414190a814
- Ticket: DEV-352, parent DEV-288, size:M, State Spec. Task note DEV-352:11,14 names POST/DELETE /api/movies/{id}/watched and /not-interested; no further Scope & Technical Design text exists in the supplied intake (recon-DEV-352:7).
- Repository facts come only from recon-DEV-352:1-69. Missing facts require needs recon to Bernstein; do not widen searches or invent measurements.
- Full questions, answers, rationale and cited Patron bases are append-only in CONCLUSIONS.md Q1-Q8. Patron confirmed shared understanding and closure in Q8.
- No production edits, tracker mutation, commit, push, PR publication or merge is performed by this grill.

## Canonical vocabulary and domain decisions (Q1, Q3)

Feedback means explicit Watched / Not Interested marks (existing CONTEXT.md:99, via recon:11). Watched means explicitly marked seen; Not Interested is an explicit negative preference. Watchlist remains independent intent to watch. These are single-user movie states, separate from EnrichmentStatus.

Movie carries independent non-null boolean IsWatched and IsNotInterested, initially false. Both can be true. Together with IsInWatchlist all eight boolean combinations are legal. Mutating one flag preserves the other two in both directions, including existing watchlist transitions. No timestamps, watch history, identity, recency calculation, ranking, recommendation filtering or implicit watchlist removal.

## Storage and migration (Q2)

Add required PostgreSQL boolean columns is_watched and is_not_interested to the existing movies table. Both default false for preexisting rows. Round-trip through Movie, MovieRecord, rehydration, persistence mapping and read projections. No separate feedback table.

Generate a new additive EF migration and updated model snapshot using tooling; do not edit the existing initial migration or hand-edit generated artifacts. Up preserves existing rows, data and flags. Down drops only the two new columns, preserving the prior schema/data; removed feedback values are naturally not retained after downgrade. Test both directions against real PostgreSQL.

## Public HTTP and read contract (Q4, Q5)

| Method and route | Operation name | Transition |
| --- | --- | --- |
| POST /api/movies/{id}/watched | MarkWatched | false to true |
| DELETE /api/movies/{id}/watched | UnmarkWatched | true to false |
| POST /api/movies/{id}/not-interested | MarkNotInterested | false to true |
| DELETE /api/movies/{id}/not-interested | UnmarkNotInterested | true to false |

Use untyped {id} with int binding, matching WatchlistEndpoints. No request DTO and no success response body. Each operation declares:

| Condition | Result |
| --- | --- |
| Legal transition | 204 No Content, empty body, save once |
| Already in requested state | 409 Conflict ProblemDetails, no save |
| Unknown positive movie id | 404 through existing exception pipeline |
| Nonpositive integer id | 422 with errors containing id, via MovieId.TryCreate |
| Malformed or overflowing integer | 400 through existing binding/error behavior |

Explicit Movie transition methods throw InvalidTransitionException on repeats. Preserve the existing centralized exception mapping; do not add a parallel mapping mechanism. Repeated requests are deliberately conflicting rather than idempotent 204 responses.

Add required bool IsWatched and IsNotInterested to both MovieSummary/MovieDetails Core ports and MovieSummaryResponse/MovieDetailsResponse. Existing browse/details responses expose isWatched and isNotInterested. No new GET endpoint or browse filter.

Preserve C# DTO -> web/src/api/openapi.json -> generated web/src/api/types.ts. Regenerate with existing tooling (recon:59, npm run generate:api); never hand-edit generated interfaces. Update Verify baseline, exact field-set assertions and forced web runtime schema/typed fixtures. No user interface controls, client abstraction or handwritten duplicate wire models.

## Implementation approach and authorized file envelope (Q6)

Follow the existing watchlist feature slice, reusing IMovieRepository.GetAsync/SaveChangesAsync, Unit, exception types and existing libraries. Add src/LamuFlix.Core/Features/Feedback with four sealed command/handler pairs named MarkWatched, UnmarkWatched, MarkNotInterested and UnmarkNotInterested (Command/CommandHandler suffixes). Add corresponding explicit methods on Movie and src/LamuFlix.Api/Endpoints/FeedbackEndpoints.cs. Wire MapFeedbackEndpoints in ApiEndpoints and handlers in HandlerRegistration.

Known existing edit sites from recon:

- src/LamuFlix.Core/Domain/Movie.cs: flags, explicit transition methods and rehydration.
- src/LamuFlix.Infrastructure/Persistence/Records/MovieRecord.cs and Configurations/MovieConfiguration.cs: persisted flags and column mapping.
- src/LamuFlix.Infrastructure/Persistence/Migrations/: new generated migration and generated LamuFlixDbContextModelSnapshot.cs changes.
- src/LamuFlix.Core/Ports/MovieSummary.cs and MovieDetails.cs: read fields; existing persistence/read projection sites only as required for these approved fields. Recon supplies responsibility but not every mapping filename; do not invent an exact path or broad refactor.
- src/LamuFlix.Api/Endpoints/ApiEndpoints.cs, LibraryResponses.cs and src/LamuFlix.Api/HandlerRegistration.cs: registration and response fields.
- Tests in existing domain/property, unit feature, integration endpoint/end-to-end/OpenAPI suites (recon:38-44), including field-set locks in library, browse, enrichment and import suites.
- web/src/api/openapi.json, types.ts, client.typecheck.ts; existing schema in web/src/features/movie-details/movieDetailsApi.ts and typed fixtures/mock tests in web/src/mocks only as forced by contract compatibility (recon:45,59).

No unrelated deletion or rewrite, new dependency, project, top-level folder, port, service/repository abstraction, identity or state library. No LocalPlay, Process.Start or secret changes. Existing watchlist behavior is preserved. No ADR or root CONTEXT.md rewrite is required; feature definitions are recorded here and in conclusions, consistent with the existing glossary.

## Charter §2.3 disposition (Q1-Q8)

| Item | Patron ruling |
| --- | --- |
| 1 — Dependencies | Reuse existing dependencies; no additions/swaps |
| 2 — Architecture | Existing feature-slice pattern only; Feedback folder is within Core/Features, no new layer/project |
| 3 — Schema | Q2 explicitly authorizes two movies columns and reversible generated migration |
| 4 — Public API | Ticket fixes four routes; Q4 fixes statuses/operations; Q5 authorizes additive read DTO fields |
| 5 — LocalPlay, execution, secrets | No changes |
| 6 — Other tracked files | Q5-Q6 authorize bounded persistence/projection/registration/contract/test compatibility edits only |

Patron found no ticket contradiction or constitution departure, so zero structural owner checkboxes. This does not replace user approval of the spec PR at Gate 1. Taste assumptions: none (ASSUMPTIONS.md).

## Test strategy (Q7)

1. Domain transition tables cover legal and repeated mark/unmark actions, including action/state exception metadata. Cover all eight flag combinations and preservation of both untouched flags. Extend existing state-sequence/property coverage for feedback and watchlist isolation.
2. Handler tests for each operation: success saves once and changes only its flag; missing movie and illegal transition save zero.
3. HTTP integration matrix for all four operations: empty 204, 409 ProblemDetails, missing movie 404, nonpositive id 422 with id errors, malformed/overflow id 400. Repository substitutes are for orchestration only.
4. Real PostgreSQL/Testcontainers round-trips: both flags through persistence/reload and browse/detail serialization, watchlist preserving feedback and vice versa, preexisting rows default false after Up, Down preserves prior schema/data. Never mock driver translation or serialization.
5. Update exact field-set assertions and OpenAPI Verify snapshot, regenerate TypeScript, and run forced web runtime schema/typecheck/contract regressions with existing tools. Existing assertion/mocking/testing libraries only.

## Task ordering and expected gates (Q8)

Order: domain/transitions and tests -> persistence/migration/rehydration -> command/handler pairs and DI -> endpoints -> read DTO/projections -> generated contracts and forced web compatibility -> real-DB/migration/property regressions -> applicable verification and review. Preserve these dependencies when Quill creates tasks; do not write production code before Gate 1.

Implementation verification uses live harness.yml for applicability and thresholds: build/tests; Roslyn analyzers; cyclomatic complexity; InspectCode; property tests; vulnerable packages; formatting; OpenAPI snapshot/drift and applicable web checks; changed-project mutation via scripts/run-mutation.ps1 without since. No gate has been run by this grill. Never lower thresholds or hand-edit generated outputs. Could not run is not pass. Keep PASS, failure, scope-empty SKIPPED, opt-out SKIP and mutation N/A distinct. Follow existing gate procedures rather than inventing command flags or numeric thresholds.

## Loop discipline and frozen scope (Q8)

Closing bar: Critical and High findings block. Medium and Low remain recorded. This size:M ticket requires all three Sentry/Risk, Ledger/Standards and Compass/Spec reports before adjudication; a missing axis is blocked, never skipped.

Round cap: at most two formal review/remediation rounds. Unresolved blockers at the cap remain blocked; no third round, severity demotion, gate waiver or silent scope expansion.

Frozen scope: the four Q4 operations; independent watched/not-interested flags and unchanged watchlist semantics; additive reversible movies persistence; observable browse/detail DTO fields; generated contract propagation and forced compatibility edits; the Q7 tests and applicable Q8 gates. Exclude UI controls, browse filters, recommendations/ranking, timestamps/history, identity, separate tables, new dependencies/layers, LocalPlay, secrets and unrelated refactors; anything else is a follow-up issue, not a finding in this round.

## Handoff

Quill drafts spec.md, plan.md and tasks.md from this brief and the cited conclusions. Keel subsequently performs read-only consistency analysis and brief alignment; plan challenge/freeze and Gate 1 remain separate future steps. User owns spec-PR merge; no seat merges.

## Drafting decision supplement — Q9–Q10 (2026-10-17)

These deliberate Patron rulings supplement the closed eight-question grill; full exchanges and cited bases are appended as Q9–Q10 in CONCLUSIONS.md. Facts additionally include recon-DEV-352:70-109. This supplement settles both DRAFTING_RECEIPT questions without approving the drafted artifacts or opening Gate 1.

Q9: Repeat-transition InvalidTransitionException action/current-state pairs are MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, and UnmarkNotInterested/Interested. Interested means only IsNotInterested == false, never positive preference. Use method names for action tokens and const string state tokens. Domain and HTTP conflict tests assert the exact pairs. Basis: recon:13,42; Q1/Q4/Q7; constitution:192-194; PRODUCT.md:34,46. The Interested wording choice is recorded [assumed] in ASSUMPTIONS.md; this supersedes the historical statement that no taste assumption exists.

Q10: T023 extends existing tests/LamuFlix.IntegrationTests/MigrationTests.cs using PostgresFixture.CreateEmptyDatabaseContextAsync for a separate throwaway database per proof. Up uses IMigrator from 20261016195556_AddMovieWriters to latest (null); Down uses latest to that prior target. Verify both new columns are required/default false, preexisting rows and prior schema/data survive, and Down drops only the two new columns. Update the existing migration count/name tripwire from two migrations to three, retaining exact old names and asserting the generated new migration. Never downgrade the shared fixture database. Basis: recon:74-93; Q2/Q7; constitution:350; PRODUCT.md:34,48. This bounded existing test edit is authorized under Q6 item 6.

Patron Q9–Q10 recorded no new owner checkbox for these two decisions. Separate constitution-alignment analysis remains pending; these rulings do not waive any constitution requirement. Quill must propagate this supplement into spec.md, plan.md, tasks.md, requirements checklist and DRAFTING_RECEIPT; Keel does not rewrite those drafts during read-only analysis.

## Constitutional-check supplement — Q11 (2026-10-17)

Patron Q11 in CONCLUSIONS.md amends Q2/Q5/Q6 and the historical zero-owner-checkbox statements in this brief. Two necessary constitution departures remain blocked under charter 2.3(b); no Patron ruling resolves these user-owned checkboxes. Gate 1 stays closed until both are resolved by the user on the spec PR and the spec PR is merged.

- [ ] blocked: structural — Approve departing from .specify/memory/constitution.md:219-220 (all commands validated through FluentValidation in the validation decorator) for the four MovieId-only feedback commands by validating route id via MovieId.TryCreate (HTTP 422) at the endpoint boundary without separate IValidator<TCommand> classes, while retaining the existing validation decorator through AddHandler?
- [ ] blocked: structural — Approve departing from .specify/memory/constitution.md:335-342 (single fresh Initial migration without constitution amendment/ADR) by generating a reversible additive EF migration for movies.is_watched and movies.is_not_interested without editing 20260930153520_Initial.cs?

MSW compliant artifact correction: Q11 directs T020 to regenerate web/src/mocks/handlers.ts with the OpenAPI/TypeScript artifacts, never hand-edit it. T021 is limited to handwritten consumer, fixture and test compatibility changes. This corrects the plan language rather than waiving constitution:492-494. MSW recon follow-up is now verified from recon-DEV-352 (replacement 72-line snapshot): lines 13-27 identify handlers.ts as generated; lines 38-68 verify the four handwritten consumer/fixture/test files. The generator reads committed openapi.json and writes types.ts plus handlers.ts (lines 17-20); export OpenAPI from the C# contract before invoking it. Generation staleness tests at lines 29-34 require forced compatibility updates for the added operations. Historical recon line citations above refer to the earlier intake/migration snapshots already read, not this replacement MSW snapshot.

Quill must propagate Q9–Q11, including both unresolved owner checkboxes, through the drafted artifacts and drafting receipt. The read-only analysis does not approve either departure, amend the constitution, freeze the plan or authorize implementation.

## Plan challenge decision supplement — Q12 (2026-10-17)

Patron Q12/12 in CONCLUSIONS.md settles all three axis reports before Keel edits the planning artifacts. This supplements Q2/Q4/Q6/Q7/Q8/Q10; the original grill and earlier analysis history stay intact. Round 1 reports: Sentry 1 High/3 Medium/2 Low; Ledger 1 High/1 Medium/3 Low; Compass 0 High/2 Medium/3 Low. All 16 findings require recorded disposition. Critical/High closing bar and maximum two formal rounds are unchanged.

- Extend the authorized envelope with existing src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs (ToRecord, ToDomain, ApplyState, Baseline, Baseline.From) and EfMovieCatalog.cs (BrowseAsync, GetDetailsAsync, MovieDetailsRecord). Both new flags must participate in insert, reload, baseline capture, per-field delta application, and both read projections. No new repository abstraction, mapper or concurrency infrastructure.
- Concurrency contract: preserve concurrently committed sibling-flag changes through the existing per-field delta seam. Two same-target requests that both load a valid pre-state may both succeed with 204. Repeat 409 is guaranteed when the request observes already-committed target state. This clarifies the load/transition/save design; it does not promise row-level serialisation. Prove stale-load interleaves with separate PostgreSQL contexts in both directions across watched, not-interested and watchlist, and prove the same-target allowed overlap followed by committed-state repeat conflict.
- T022 must round-trip all eight flag combinations through save, fresh reload, browse and details serialization, including both asymmetric feedback pairs. T007–T010 explicitly prove forwarding the supplied CancellationToken to read and save; handlers forward it.
- T004 mirrors the existing required boolean mapping without HasDefaultValue(false). False initialization belongs to the domain; preexisting-row false backfill/default is verified on the tooling-generated migration. Do not hand-edit generated artifacts or assert a model-level default is required.
- T023 seeds a nonempty prior-schema database before Up, asserts positive unchanged row count and original data plus both false flags after Up; keep Q10 isolated databases and Down preservation. Move the migration count/name tripwire update from T023 to T006 so Phase 2 does not commit a known regression.
- Preserve implementation order, but commit endpoint/read/generated-contract work T016–T021 as one green group after forced compatibility and OpenAPI baseline updates. No commits at intermediate red Phase 4/5 boundaries; no other phase commits while existing checks are red. Generated contract updates are still after C# changes.
- T024 uses live configured complexity gates without a literal threshold override; T025 records an explicit OpenAPI snapshot/drift verdict in addition to existing separate web verdicts. No gate lowering or new opt-out.
- Annotate superseded intake/migration recon citations as historical; current MSW citations identify the preserved 72-line MSW snapshot. Facts not present in current roots are not independently reverified; Q10/Q12 preserve the cited decision bases. Further facts require recon via Bernstein.
- Accepted residuals: after a committed request loses its response, retry may yield already-state 409; use existing reads to observe state, without automatic retry/idempotency infrastructure. Down discards feedback values and a later Up resets them false; do not promise zero-downtime deployment or retention across downgrade.
- Ledger L1 feature-list staleness and L3 existing test-project layout are noted, no ticket, per Patron. No constitution rewrite or test move is added. Sentry L1 remains a historical-provenance observation mitigated by the full T016 real-pipeline matrix.

No new taste assumption, dependency, schema beyond the two ticket columns, public route or owner checkbox. Both Q11 structural owner decisions remain unresolved. Planning freeze, once the corrected artifacts are checked, is conditional on those owner decisions for execution; it never opens Gate 1 or authorizes implementation. Full adjudication belongs in plan.md and the DEV-352 note.

Evidence/closure supplement: recon-DEV-352:74-127 now appends repository baseline/delta, projection and concurrency facts; :1-72 remains the MSW snapshot. Earlier intake/migration snapshots are not present and are cited only as historical decision provenance. Plan challenge round 1 is adjudicated in plan.md (all 16 dispositions); conditional plan freeze for spec-PR handoff leaves both Q11 owner checkboxes unresolved, Gate 1 closed and implementation unauthorized. No delivery gates run.

## Phase 6 decision supplement — T021 forced compatibility (2026-10-18)

Patron explicitly authorizes this supplement under charter 2.3 item 6, amending Q5/Q11/FR-010/T021; the full cited ruling is appended in CONCLUSIONS.md. Evidence is recon-DEV-352-2:1-44 at implementation HEAD 6609ae0221274e3dfca4d326444636ea61a01b45 in F:/Dev/LamuFlix.worktrees/DEV-352 on feature/DEV-352. This completes the existing frozen contract-compatibility scope; it adds no delivery behavior or owner checkbox and makes no Gate 1 or gate-pass claim.

Extend T021 by exactly these ten handwritten files:

- web/src/library/libraryQueries.ts (pendingRow only)
- web/src/api/client.test.ts
- web/src/App.test.tsx
- web/src/library/libraryQueries.polling.test.tsx
- web/src/library/LibraryView.retry.test.tsx
- web/src/library/MovieCard.layout.test.tsx
- web/src/library/MovieCard.test.tsx
- web/src/library/MovieGrid.test.tsx
- web/src/library/watchlist.integration.test.tsx
- web/src/library/watchlistListFilter.test.tsx

Only add isWatched: false and isNotInterested: false to pendingRow and existing response literals/factory defaults; retain existing override spreads. These ten are the direct literal failures (recon:17,19-27,39). MovieDetailsView.tsx and web/src/test/handlers.ts remain excluded from direct edits: their errors are transitive through the already-authorized movieDetailsApi.ts schema and mocks/fixtures.ts (recon:18,28,32-33,40-41). A remaining failure requiring either direct edit needs new recon and a new Patron ruling.

No new UI controls, branching logic, abstraction, dependency, or generated hand-edits. The C# to OpenAPI to generated TypeScript/MSW chain remains mandatory. Existing T025 separate typecheck, runtime-schema/contract, web tests/build, generator/generated-handler regression and generated-drift verdicts apply to this expansion. T020 before T021, the green T016-T021 commit group, Critical/High closing bar, all-three-axis requirement and two-round cap remain unchanged. No extra test suite or new task is introduced.
