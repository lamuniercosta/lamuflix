# DEV-299: Phase A Brief

This is the Phase A grill outcome for DEV-299 (parent DEV-282, size:L, no `ui:` tag).

Patron ruled on Q1–Q12 in `specs/DEV-299/CONCLUSIONS.md` (commit 757e773) and logged the taste calls in `ASSUMPTIONS.md`. Facts come from two sources:

- the note `recon-DEV-299`;
- the verbatim recon file `recon-DEV-299-verbatim.txt` at the worktree root, cited below as `V:<line>`.

The ticket text is recon round 1 (get-task receipt): Scope items 1–6 and two ACs.

Grill tally:

- **Questions:** 12 asked, against a cap of 12. Patron ruled 10 and returned 2 as `blocked: structural` (Q1, Q6).
- **Open:** one post-grill finding (Q13) is open for Patron. It is outside the 12-question budget, so I have not asked it (see *Open items*).
- **ADR:** one, drafted by Keel (Q11).

**Gate 1 stays closed** until the owner answers the two checkboxes below and Patron rules on Q13.

## Closing bar

The ticket ACs, verbatim:

- **AC1:** "All use cases implemented with zero database or RabbitMQ dependencies."
- **AC2:** "100% unit test pass rate across all feature handlers."

Keel's closing-bar lines follow from the rulings. They do not change what the ticket delivers:

- **AC3 (Q5, Q2, Q3):** every handler is a `sealed` class implementing `ICommandHandler<,>` / `IQueryHandler<,>` over a `sealed record` request, in `src/LamuFlix.Core/Features/<Feature>/` (constitution II:130-131; arch tests V:984-999, V:1031-1048). Results are exactly those in the *Handler contract table*.
- **AC4 (Q4):** `NotFoundException` exists in Core/Pipeline. The single Api `IExceptionHandler` maps it to 404 as `ProblemDetails`, with a test proving the mapping.
- **AC5 (Q2):** `RecordEnrichmentFailureCommandHandler` never publishes. It returns an `EnrichmentFailureDecision`:
  - `Retry` or `RetryDelayed` (RateLimited) while the category is retryable and `Attempt < EnrichmentOptions.MaxAttempts`, leaving the row Pending;
  - otherwise `DeadLetter`, calling `MarkFailed(category, now)` and saving.
- **AC6 (Q10):** handler tests are in `tests/LamuFlix.UnitTests/Features/<Feature>/`. They cover every legal path and every exception path. `LamuFlix.UnitTests.csproj` references AutoFixture and Faker.Net, whose versions are already pinned (`Directory.Packages.props:31-32`).
- **AC7:** `LamuFlix.ArchitectureTests` passes unchanged, unless the Q13 ruling says otherwise. Core gains no package reference.
- **AC8:** the *Gate expectations* all exit 0, and the full suite is green.

## Owner checkboxes (§2.3(a), for the spec PR; Gate 1 closed until answered)

- [ ] **D1 / Q1: stranded requeue contract.** blocked: structural. May DEV-299 change the stranded-work claim and message contract so that the sweeper can enqueue work a worker can then claim?
  - The ticket's literal "claims stranded Pending movies and re-enqueues them" livelocks. The sweeper's `TryClaimForEnrichmentAsync` succeeds, the worker's mandatory claim (constitution 447-448) then returns false, and the message is acked unprocessed (CONCLUSIONS Q1).
  - Candidate answers, for the owner (not decided):
    - (A) drop `RequeueStrandedMoviesCommandHandler` from DEV-299 into a follow-up that owns the claim-handoff contract;
    - (B) the sweeper does not claim; instead a new `IMovieRepository` list-stranded method (lease-aged `Pending`, constitution 187-188) feeds enqueue, and the worker claim remains the only claim;
    - (C) a claim token in `EnrichmentRequested`, which changes a type constitution 445 fixes at `MovieId` + `Attempt` and so needs an amendment.
- [ ] **D2 / Q6: MovieId source on import.** blocked: structural. May DEV-299 extend the import contract to obtain a unique `MovieId` before `Movie.Create`, and which port or caller allocates it?
  - `MovieId.TryCreate` only validates a positive int. `Movie.Create` requires the id before `AddAsync`, and no port allocates or returns one (CONCLUSIONS Q6; `Movie.cs:41`).
  - Candidate answers (not decided):
    - (A) `IMovieRepository` gains an id-allocation member;
    - (B) the store assigns the id on save and `Movie` gains a pre-persistence construction path, which edits `Movie.cs`;
    - (C) defer `ImportMovieFolderCommandHandler` to a follow-up.

The ruled order of the import once D2 is settled:

1. `Scan` → `Movie.Create` → `AddAsync` → `SaveChangesAsync` → `EnqueueAsync(new EnrichmentRequested(id, 1))`.
2. A publish failure propagates, with no catch (the sweeper mitigates it, constitution 454-455).
3. The command carries a `LibraryPath`.

## Open items

- **Q13 (Patron; found after round 1, not asked because the budget is spent):** the arch rule `Core_features_must_depend_only_on_ports_domain_or_pipeline` lets a type under `LamuFlix.Core.Features.*` depend only on:
  - its own namespace;
  - `LamuFlix.Core.Ports`, `.Domain` and `.Pipeline`;
  - `System`, `Microsoft.Extensions.Logging` and `Microsoft.Extensions.Logging.Abstractions`.

  The source is V:1170-1195. Two conflicts follow:
  - (i) `BrowseMoviesQuery` must carry `MovieQuery`, which lives in `LamuFlix.Core.Library` (V:245; kept there by DEV-298 Q3). That is a direct dependency outside the allow-list.
  - (ii) Handlers comparing `EnrichmentStatus` members (Q7's NotFound/Failed guard; the Apply result) may pick up a member-reference dependency on `Ardalis.SmartEnum`, which is also outside the list. Whether NetArchTest flags inherited SmartEnum operators is **unverified**; Quill's plan must include a probe.

  Candidate rulings:
  - add `LamuFlix.Core.Library` (and, if the probe fails, `Ardalis.SmartEnum`) to the allow-list, which edits an unnamed tracked test file (§2.3 #6);
  - move `MovieQuery` into `Core/Ports`, which reverses DEV-298 Q3.

  My recommendation is the first option: it is an additive allow-list entry, and `MovieQuery` is shared vocabulary, not a feature. **Quill must not plan the Library tasks until Patron rules.**

## Frozen scope

The only files in scope are these. Anything else is out of scope.

**New, `src/LamuFlix.Core/Pipeline/`:**
- `NotFoundException.cs` (Q4): `sealed`, mirroring `ValidationException`.
- `Unit.cs` (Q5, [assumed] name).
- `EnrichmentOptions.cs` (Q2): a plain `sealed record` with `MaxAttempts` and data annotations, and no `IOptions`. The lease and sweep-interval members wait for D1.

**New, `src/LamuFlix.Core/Domain/`:**
- `EnrichmentFailureAction.cs`: a SmartEnum with `Retry`, `RetryDelayed` and `DeadLetter`.
- `EnrichmentFailureDecision.cs`: `sealed record EnrichmentFailureDecision(EnrichmentFailureAction Action, int? NextAttempt)`.

Keel plan decision: Patron fixed the names ([assumed], Q2) but not the placement. They go in Domain beside `EnrichmentFailureCategory` because they are enrichment vocabulary, not handler plumbing. Domain is also outside the feature allow-list, so a SmartEnum base there trips nothing.

**New feature handlers (Q3–Q8).** Each file holds the request record and its handler, or they sit as sibling files; Quill picks one convention and applies it everywhere:
- `Features/Library/`: `BrowseMoviesQuery` + handler, `GetMovieDetailsQuery` + handler. Q13 gates these.
- `Features/Import/`: `ImportMovieFolderCommand` + handler. D2 gates these.
- `Features/Enrichment/`: `ClaimEnrichment`, `ApplyEnrichmentResult`, `RecordEnrichmentFailure`, `RequestEnrichment` and `RequeueStrandedMovies` commands + handlers. D1 gates `RequeueStrandedMovies`.
- `Features/Watchlist/`: `AddToWatchlist` and `RemoveFromWatchlist` commands + handlers.
- `Features/Playback/`: `PlayMovieCommand` + handler.

**Edited:**
- The Api `IExceptionHandler` (`LamuFlix.Api.ExceptionHandling`, `ValidationExceptionHandler.cs`): add a `NotFoundException` → 404 arm, and extend its existing tests (Q4; Patron forced this under constitution checklist 361-362).
- `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`: add `AutoFixture` and `Faker.Net` `PackageReference`s, with no version attribute (Q10).
- `CONTEXT.md`: add the term **Stranded Movie** (Q9). This applies only if D1 keeps the requeue handler in DEV-299.
- `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`: only if Q13 rules the allow-list edit.

**New tests:** `tests/LamuFlix.UnitTests/Features/{Library,Import,Enrichment,Watchlist,Playback}/<Handler>Tests.cs`, plus any test `TimeProvider` subclass (Q10, Q12).

**New docs:** `docs/adr/0017-enrichment-decisions-in-core-handlers.md` (Q11, [assumed] title). Keel drafts it; Q1's section stays "pending owner D1".

**Out of scope (Q9 and others):**
- DI/`AddHandler` registration.
- FluentValidation validators (Core cannot reference them, V:975-980).
- Options binding.
- Endpoints and OpenAPI.
- `EnrichmentConsumer`, the sweeper hosted service, and adapters.
- `DisabledMediaPlayerLauncher` / LocalPlay gating (adapter-side, Q8).
- `TelemetryConstants` additions, including `lamuflix.import.count`.
- `IMetadataProvider` invocation and `MetadataLookup` construction (the consumer's job, Q3).
- Duplicate-path import detection. Patron: note it, and file no ticket until existing tickets are checked.
- Legacy Web/Data/Worker/Test code, including `MovieService.PlayMovie`, `UnitTest1` and `EnrichmentFailureClassifier` plus its `LamuFlix.Test` test.
- Any `Movie.cs` or port-signature edit, unless D1/D2 authorizes it.

**The spec must list the wiring prerequisites** as required-before-use: registration, validators, `EnrichmentOptions` binding, the LocalPlay-gated launcher. They are not delivered here (Q9).

## Handler contract table (Q2–Q8, Q12)

| Handler | Request | Result | Ports / deps | Behaviour |
|---|---|---|---|---|
| `BrowseMoviesQueryHandler` | `BrowseMoviesQuery(MovieQuery Query)` | `PagedResult<MovieSummary>` | `IMovieCatalog` | Passes the query straight to `BrowseAsync`. |
| `GetMovieDetailsQueryHandler` | `GetMovieDetailsQuery(MovieId Id)` | `MovieDetails` | `IMovieCatalog` | A null result → `NotFoundException`. |
| `ImportMovieFolderCommandHandler` | `ImportMovieFolderCommand(LibraryPath Folder)` | `MovieId` | `IMediaLibraryScanner`, `IMovieRepository`, `IEnrichmentQueue` | The D2-gated order above. |
| `ClaimEnrichmentCommandHandler` | `ClaimEnrichmentCommand(MovieId Id)` | `bool` | `IMovieRepository` | Returns the `TryClaimForEnrichmentAsync` result unchanged. |
| `ApplyEnrichmentResultCommandHandler` | `ApplyEnrichmentResultCommand(MovieId Id, MetadataLookupResult Result)` | `EnrichmentStatus` | `IMovieRepository`, `TimeProvider` | Null → NotFound. `Found` → `MarkEnriched(m, now)`; `NotFound` → `MarkNotFound(now)`; then save and return the new status. `Failed` → `ArgumentException` before any mutation. |
| `RecordEnrichmentFailureCommandHandler` | `RecordEnrichmentFailureCommand(MovieId Id, int Attempt, EnrichmentFailureCategory Category)` | `EnrichmentFailureDecision` | `IMovieRepository`, `TimeProvider`, `EnrichmentOptions`, `ILogger<>` | Retryable and `Attempt < MaxAttempts` → `Retry`/`RetryDelayed` (`RateLimited`) with `NextAttempt = Attempt + 1`, no mutation. Otherwise load (null → NotFound), `MarkFailed(category, now)`, save, and return `DeadLetter` with `NextAttempt = null`. Every path logs a structured movie id, attempt, category and action, with no exception text. |
| `RequestEnrichmentCommandHandler` | `RequestEnrichmentCommand(MovieId Id)` | `MovieId` | `IMovieRepository`, `IEnrichmentQueue` | Null → NotFound. A status other than NotFound/Failed → `InvalidTransitionException(nameof(RequestEnrichment), status)` before the domain call. Otherwise `RequestEnrichment()`, save, then enqueue `EnrichmentRequested(id, 1)`. |
| `RequeueStrandedMoviesCommandHandler` | D1-gated | `int` (count enqueued) | D1-gated | D1-gated. |
| `AddToWatchlistCommandHandler` / `RemoveFromWatchlistCommandHandler` | `(MovieId Id)` | `Unit` | `IMovieRepository` | Null → NotFound. Domain transition, then save. A double add or remove propagates `InvalidTransitionException`. |
| `PlayMovieCommandHandler` | `PlayMovieCommand(MovieId Id)` | `Unit` | `IMovieCatalog`, `IMediaPlayerLauncher` | `GetDetailsAsync`; null → NotFound; `Launch(Path, Format)`. The handler is ungated; LocalPlay stays adapter-side (Q8, §2.3 #5 ruled). |

Cross-cutting rules for every handler (Q12, constitution checklist 367-368 and coding conventions 438-439):

- Every async port call receives the `CancellationToken`.
- `TimeProvider.GetUtcNow()` is the only time source.
- No handler takes more than three ports; `TimeProvider`, options and the logger do not count as ports.
- There is no logging beyond RecordFailure's decision log.
- There are no comments except the AAA headers.

## Grill answers (summary; CONCLUSIONS.md is authoritative)

1. **Q1:** blocked: structural → D1.
2. **Q2:** decision contract as in AC5; `EnrichmentOptions` in Core/Pipeline; the consumer alone republishes or dead-letters.
3. **Q3:** Apply handles Found/NotFound; `Failed` → `ArgumentException`; Claim returns the bool.
4. **Q4:** `NotFoundException` is added in Core/Pipeline and mapped to 404 in this PR.
5. **Q5:** results as in the table; the shared no-value result is `Unit`.
6. **Q6:** blocked: structural → D2.
7. **Q7:** manual retry only from NotFound/Failed; Attempt restarts at 1.
8. **Q8:** watchlist errors come from the domain; playback goes through the launcher port and is ungated in the handler.
9. **Q9:** scope fence as above; `CONTEXT.md` gains Stranded Movie; the wiring prerequisites are named in the spec.
10. **Q10:** AutoFixture and Faker.Net are referenced now; tests go under `UnitTests/Features/<Feature>/`; Core Stryker break at 80.
11. **Q11:** the ADR's subject is accepted; the Q1 section stays pending.
12. **Q12:** injected time, CT flow, and the structured RecordFailure log.

## Plan decisions (Keel)

- **Approach:**
  - Build the shared Pipeline/Domain types first.
  - Then build the handlers feature by feature. Each is a thin orchestration over ports and domain methods, with no handler-to-handler calls and no cross-feature references.
  - The D1-, D2- and Q13-gated handlers are planned but their tasks are marked `[BLOCKED: D1|D2|Q13]` until answered. Tasks are not dropped silently.
- **Test strategy:**
  - One test class per handler. Doubles are NSubstitute substitutes of the ports.
  - Movies in a given status are built through the real domain transitions, e.g. `Create` → `MarkNotFound`.
  - AutoFixture/Faker.Net supply anonymous ids and titles.
  - Use `Theory` + `MemberData` for the status/category matrices:
    - RecordFailure: 4 categories × below/at `MaxAttempts`;
    - RequestEnrichment: 4 statuses.
  - Assert each port interaction with `Received`/`DidNotReceive`, including the **order** Save-before-Enqueue on Import and RequestEnrichment.
  - Assert no Save and no Enqueue on every exception path.
  - The time source is a small test `TimeProvider` subclass with a fixed `GetUtcNow`.
  - The Api mapping gets a 404 `ProblemDetails` test beside the existing `ValidationExceptionHandler` tests.
- **Gate expectations:**
  - `dotnet build` with zero warnings.
  - `dotnet test` all green (AC2).
  - `LamuFlix.ArchitectureTests` green.
  - `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (≤15, then refactor gate `-Threshold 6`) and `./scripts/run-jetbrains-inspectcode.ps1` all exit 0 on the diff.
  - `dotnet format --verify-no-changes`.
  - Stryker on `LamuFlix.Core` at break threshold 80 (constitution 290).
  - `git diff --stat origin/main...HEAD` shows only frozen-scope files.
- **Task ordering:**
  1. Pipeline types (`NotFoundException`, `Unit`, `EnrichmentOptions`).
  2. Domain decision types.
  3. The UnitTests csproj references.
  4. Watchlist.
  5. Playback.
  6. Enrichment: Claim → Apply → RecordFailure → RequestEnrichment.
  7. Library [Q13].
  8. Import [D2].
  9. RequeueStranded + `CONTEXT.md` [D1].
  10. Api 404 mapping + test.
  11. ADR.
  12. Gates.

  Steps 4–6 can run in parallel after steps 1–3.

## Plan decisions, analyze round 1 (Keel, 2026-09-30)

- **Order:** Quill's order is accepted: Api 404 (brief step 10) runs before the blocked Library, Import and Requeue phases. Unblocked work goes first, and nothing depends on the old position.
- **`Unit`:** a `public sealed record Unit` with `public static readonly Unit Value`. Handlers return `Unit.Value`.
- **RecordFailure log assertion:** add no package, so no `Microsoft.Extensions.Logging.Testing` and no `FakeLogger`. Capture with an NSubstitute `ILogger<T>` (asserting `Received` on `Log`) or with a small hand-written test logger in `tests/LamuFlix.UnitTests/Features/`.
- **SmartEnum probe:** the probe runs once every Enrichment handler compiles (Claim, Apply, RecordFailure, RequestEnrichment). `LamuFlix.ArchitectureTests` is also re-run at each phase checkpoint. A failure naming `Ardalis.SmartEnum` goes to Patron as part of Q13, and the arch test is not edited.
- **ADR:** `docs/adr/0017-enrichment-decisions-in-core-handlers.md`, status Proposed. If `origin/main` has taken 0017 by rebase, renumber it.

## Round cap

- **Grill:** 12/12 questions used; there is no grill round 2. Q13 goes to Patron as a single post-grill ruling, with Conductor's approval to exceed the budget, or as a Quill `needs decision:`.
- **Quill:** at most 2 fix-list rounds on spec/plan/tasks against this brief before escalation to the Conductor.
