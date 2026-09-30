# DEV-299: Phase A Brief

This is the Phase A grill outcome for DEV-299 (parent DEV-282, size:L, no `ui:` tag).

Patron ruled on Q1–Q12 in `specs/DEV-299/CONCLUSIONS.md` (commit 757e773) and logged the taste calls in `ASSUMPTIONS.md`. Facts come from two sources:

- the note `recon-DEV-299`;
- the verbatim recon file `recon-DEV-299-verbatim.txt` at the worktree root, cited below as `V:<line>`.

The ticket text is recon round 1 (get-task receipt): Scope items 1–6 and two ACs.

Grill tally:

- **Questions:** 12 asked, against a cap of 12. Patron ruled 10 and returned 2 as `blocked: structural` (Q1, Q6).
- **Post-grill:** one finding (Q13) went to Patron outside the 12-question budget. Patron ruled Q13 (CONCLUSIONS.md #13, e865874): add `LamuFlix.Core.Library` to the allow-list (see *Open items*).
- **ADR:** one, drafted by Keel (Q11).

**Gate 1 stays closed** until the owner answers the three checkboxes below (D3 added by the plan challenge). Q13 is ruled by Patron.

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
- **AC7:** `LamuFlix.ArchitectureTests` passes with one change only: the Q13 ruling adds `LamuFlix.Core.Library` to the feature allow-list. Core gains no package reference.
- **AC8:** the *Gate expectations* all exit 0, and the full suite is green.

## Owner checkboxes (§2.3(a), for the spec PR; Gate 1 closed until answered)

- [ ] **D1 / Q1: stranded requeue contract.** blocked: structural. May DEV-299 change the stranded-work claim and message contract so that the sweeper can enqueue work a worker can then claim?
  - The ticket's literal "claims stranded Pending movies and re-enqueues them" livelocks. The sweeper's `TryClaimForEnrichmentAsync` succeeds, the worker's mandatory claim (constitution 447-448) then returns false, and the message is acked unprocessed (CONCLUSIONS Q1).
  - Candidate answers, for the owner (not decided):
    - (A) drop `RequeueStrandedMoviesCommandHandler` from DEV-299 into a follow-up that owns the claim-handoff contract;
    - (B) the sweeper does not claim; instead a new `IMovieRepository` list-stranded method (lease-aged `Pending`, constitution 187-188) feeds enqueue, and the worker claim remains the only claim;
    - (C) a claim token in `EnrichmentRequested`, which changes a type constitution 445 fixes at `MovieId` + `Attempt` and so needs an amendment.
  - Ticket-text consequence (Compass S2): (A) drops a ticket-named handler, so AC1/AC2 cannot hold as written and the ticket text is amended (§2.3(a)).
  - Contract impact (Compass S3): (B) edits the `IMovieRepository` port and its implementations (an unnamed file, §2.3 #6); (C) edits `EnrichmentRequested` after a constitution amendment. T030a applies it.
- [ ] **D2 / Q6: MovieId source on import.** blocked: structural. May DEV-299 extend the import contract to obtain a unique `MovieId` before `Movie.Create`, and which port or caller allocates it?
  - `MovieId.TryCreate` only validates a positive int. `Movie.Create` requires the id before `AddAsync`, and no port allocates or returns one (CONCLUSIONS Q6; `Movie.cs:41`).
  - Candidate answers (not decided):
    - (A) `IMovieRepository` gains an id-allocation member;
    - (B) the store assigns the id on save and `Movie` gains a pre-persistence construction path, which edits `Movie.cs`;
    - (C) defer `ImportMovieFolderCommandHandler` to a follow-up.
  - Ticket-text consequence (Compass S2): (C) drops a ticket-named handler, so AC1/AC2 cannot hold as written and the ticket text is amended (§2.3(a)).
- [ ] **D3: wiring deferred past this PR (constitution departure, §2.3(b)).** blocked: structural. May DEV-299 merge Core handlers that are not yet registered via `AddHandler<…>`, have no FluentValidation validators, and whose `EnrichmentOptions` is not bound with `ValidateOnStart()`?
  - Patron's Q9 ruling puts registration, validators and options binding in wiring work. The constitution makes each a per-PR item: checklist "sealed handlers registered via `AddHandler<…>`" and "new options records are validated at startup", Principle V ("All commands, queries … MUST be validated through FluentValidation"), and the configuration rule (options records with `ValidateOnStart()`). Deferring them is a departure, so it needs the owner's checkbox, not only Patron's ruling (Compass S1).
  - Candidate answers (not decided):
    - (A) accept the departure: handlers ship unregistered and unreachable; FR-018 names the wiring prerequisites, and a follow-up ticket delivers them before any use;
    - (B) pull registration, validators and options binding into DEV-299, which widens its scope beyond the ticket text (then also §2.3(a)).

The ruled order of the import once D2 is settled:

1. `Scan` → `Movie.Create` → `AddAsync` → `SaveChangesAsync` → `EnqueueAsync(new EnrichmentRequested(id, 1))`.
2. A publish failure propagates, with no catch (the sweeper mitigates it, constitution 454-455).
3. The command carries a `LibraryPath`.

## Open items

- **Q13 (Patron; found after round 1, outside the question budget) - RULED.** Patron ruled Q13 (CONCLUSIONS.md #13, e865874): add `LamuFlix.Core.Library` to the allow-list of `Core_features_must_depend_only_on_ports_domain_or_pipeline`; `MovieQuery` and its Library value types stay in `LamuFlix.Core.Library`. `Ardalis.SmartEnum` is not added: probe it in T021 with the `ReferenceEquals` fallback; a probe still red after the fallback returns to Patron as a separate ruling before implementation. The edit is gate-forced and narrow, not a general widening of feature dependencies. The move-to-Ports candidate below is rejected. Original finding: the arch rule `Core_features_must_depend_only_on_ports_domain_or_pipeline` lets a type under `LamuFlix.Core.Features.*` depend only on:
  - its own namespace;
  - `LamuFlix.Core.Ports`, `.Domain` and `.Pipeline`;
  - `System`, `Microsoft.Extensions.Logging` and `Microsoft.Extensions.Logging.Abstractions`.

  The source is V:1170-1195. Two conflicts follow:
  - (i) `BrowseMoviesQuery` must carry `MovieQuery`, which lives in `LamuFlix.Core.Library` (V:245; kept there by DEV-298 Q3). That is a direct dependency outside the allow-list.
  - (ii) Handlers comparing `EnrichmentStatus` members (Q7's NotFound/Failed guard; the Apply result) may pick up a member-reference dependency on `Ardalis.SmartEnum`, which is also outside the list. Whether NetArchTest flags inherited SmartEnum operators is **unverified**; Quill's plan must include a probe.

  Candidate rulings:
  - add `LamuFlix.Core.Library` (and `Ardalis.SmartEnum` only if the probe stays red after the `ReferenceEquals` fallback, Compass S5) to the allow-list, which edits an unnamed tracked test file (§2.3 #6);
  - move `MovieQuery` into `Core/Ports`, which reverses DEV-298 Q3; the ruling must name every Library type that moves with it (Compass S7).

  My recommendation is the first option: it is an additive allow-list entry, and `MovieQuery` is shared vocabulary, not a feature. **Quill must not plan the Library tasks until Patron rules.**

## Frozen scope

The only files in scope are these. Anything else is out of scope.

**New, `src/LamuFlix.Core/Pipeline/`:**
- `NotFoundException.cs` (Q4): `sealed`, mirroring `ValidationException`.
- `Unit.cs` (Q5, [assumed] name).
- `EnrichmentOptions.cs` (Q2): a plain, non-positional `sealed record` with `[Range(1, int.MaxValue)] public int MaxAttempts { get; init; }`, and no `IOptions` (Compass S4). The lease and sweep-interval members wait for D1.

**New, `src/LamuFlix.Core/Domain/`:**
- `EnrichmentFailureAction.cs`: a SmartEnum with `Retry`, `RetryDelayed` and `DeadLetter`.
- `EnrichmentFailureDecision.cs`: `sealed record EnrichmentFailureDecision(EnrichmentFailureAction Action, int? NextAttempt)`.

Keel plan decision: Patron fixed the names ([assumed], Q2) but not the placement. They go in Domain beside `EnrichmentFailureCategory` because they are enrichment vocabulary, not handler plumbing. Domain is also outside the feature allow-list, so a SmartEnum base there trips nothing.

**New feature handlers (Q3–Q8).** Each file holds the request record and its handler, or they sit as sibling files; Quill picks one convention and applies it everywhere:
- `Features/Library/`: `BrowseMoviesQuery` + handler, `GetMovieDetailsQuery` + handler. Unblocked by the Q13 ruling.
- `Features/Import/`: `ImportMovieFolderCommand` + handler. D2 gates these.
- `Features/Enrichment/`: `ClaimEnrichment`, `ApplyEnrichmentResult`, `RecordEnrichmentFailure`, `RequestEnrichment` and `RequeueStrandedMovies` commands + handlers. D1 gates `RequeueStrandedMovies`.
- `Features/Watchlist/`: `AddToWatchlist` and `RemoveFromWatchlist` commands + handlers.
- `Features/Playback/`: `PlayMovieCommand` + handler.

**Edited:**
- The Api `IExceptionHandler` (`LamuFlix.Api.ExceptionHandling`, `ValidationExceptionHandler.cs`): add a `NotFoundException` → 404 arm, and extend its existing tests (Q4; Patron forced this under constitution checklist 361-362).
- `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`: add `AutoFixture` and `Faker.Net` `PackageReference`s, with no version attribute (Q10).
- `CONTEXT.md`: add the term **Stranded Movie** (Q9). This applies only if D1 keeps the requeue handler in DEV-299.
- `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`: add `LamuFlix.Core.Library` to the feature allow-list (Q13 ruling); nothing else.

**New tests:** `tests/LamuFlix.UnitTests/Features/{Library,Import,Enrichment,Watchlist,Playback}/<Handler>Tests.cs`, the shared helpers `Features/FixedTimeProvider.cs` and `Features/RecordingLogger.cs` (Q10, Q12, Ledger F1/F6), and `tests/LamuFlix.UnitTests/Pipeline/EnrichmentOptionsTests.cs` (Compass S4).

**D1-conditional edit:** `src/LamuFlix.Core/Ports/IMovieRepository.cs` and its implementations under D1(B), or `EnrichmentRequested` under D1(C) (T030a, Compass S3).

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
| `RequestEnrichmentCommandHandler` | `RequestEnrichmentCommand(MovieId Id)` | `MovieId` | `IMovieRepository`, `IEnrichmentQueue` | Null → NotFound. A status other than NotFound/Failed → `InvalidTransitionException(nameof(RequestEnrichment), movie.Status.ToString())` before the domain call. Otherwise `RequestEnrichment()`, save, then enqueue `EnrichmentRequested(id, 1)`. |
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
  - The D1- and D2-gated handlers are planned but their tasks are marked `[BLOCKED: D1|D2]` until answered; Q13 is ruled, so the Library tasks are unblocked. Tasks are not dropped silently.
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
  7. Library (Q13 ruled).
  8. Import [D2].
  9. RequeueStranded + `CONTEXT.md` [D1].
  10. Api 404 mapping + test.
  11. ADR.
  12. Gates.

  Steps 4–6 can run in parallel after steps 1–3.

## Plan decisions, analyze round 1 (Keel, 2026-09-30)

- **Order:** Quill's order is accepted: Api 404 (brief step 10) runs before the blocked Library, Import and Requeue phases. Unblocked work goes first, and nothing depends on the old position.
- **`Unit`:** a `public sealed record Unit` with `public static readonly Unit Value`. Handlers return `Unit.Value`.
- **RecordFailure log assertion:** add no package, so no `Microsoft.Extensions.Logging.Testing` and no `FakeLogger`. Capture with a small hand-written test logger in `tests/LamuFlix.UnitTests/Features/` (the NSubstitute option is struck by the plan challenge, Ledger F1).
- **SmartEnum probe:** the probe runs once every Enrichment handler compiles (Claim, Apply, RecordFailure, RequestEnrichment). `LamuFlix.ArchitectureTests` is also re-run at each phase checkpoint. A failure naming `Ardalis.SmartEnum` that survives the `ReferenceEquals` fallback goes to Patron as a separate ruling (Q13 ruling), and the SmartEnum part of the arch test is not edited.
- **ADR:** `docs/adr/0017-enrichment-decisions-in-core-handlers.md`, status Proposed. If `origin/main` has taken 0017 by rebase, renumber it.

## Plan challenge adjudication (Keel, 2026-09-30)

Inputs: `findings-DEV-299-Ledger` (F1-F6, A1-A2), `findings-DEV-299-Compass` (S1-S10; S1 in round one, S2-S10 after Compass re-posted the note), Sentry (L1, L2, duplicate-import note; verdict implementable). Each ruling below supersedes any earlier line in this brief it contradicts.

| # | Verdict | Ruling |
|---|---|---|
| Ledger F1 | Accept | NSubstitute cannot usefully assert `ILogger<T>.Log`: the logging extensions and source-generated `LoggerMessage` call `Log<TState>` with an internal or private `TState`, so an `Arg.Any<object>()` match never hits. The round-1 "NSubstitute `Received` on `Log`" option is struck. T017 uses a hand-written `RecordingLogger<T> : ILogger<T>` in `tests/LamuFlix.UnitTests/Features/RecordingLogger.cs` that captures level, the structured state pairs and the exception argument. No package added. |
| Ledger F2 | Reject (no change) | The selection rule already exists: constitution PR checklist "New closed sets of values are Enumerations (`Ardalis.SmartEnum` or equivalent); no new project-owned C# `enum`". `EnrichmentFailureAction` as a SmartEnum is Patron's Q2 ruling and complies; `EnrichmentFailureCategory`'s sealed record with static instances is the "or equivalent" form and predates this ticket. ADR 0017 gains one sentence citing the rule. |
| Ledger F3 | Accept in part | `Unit` stays a `sealed record` (Patron Q5 names the shape). Add a `private Unit()` constructor so no caller can mint one. A member-less record has value equality, so every instance equals `Unit.Value` and `with` cannot produce a distinguishable value; assertions use `ShouldBe(Unit.Value)`. |
| Ledger F4 | Accept, spec line only | FR-006 states the invariant: `Retry`/`RetryDelayed` ⇒ `NextAttempt` non-null and equal to `Attempt + 1`; `DeadLetter` ⇒ `NextAttempt` null. The only producer is RecordFailure, whose T017 matrix asserts it on every row. No constructor guard: Q2 fixed the positional shape, and a guard adds an untested-by-design throw path for one internal producer. |
| Ledger F5 | Accept | Spec US4 scenario 10 and the handler contract table read `InvalidTransitionException(nameof(RequestEnrichment), movie.Status.ToString())`, per the `(string action, string state)` constructor and the `Movie.cs:110,118` precedent. |
| Ledger F6 | Accept | T008 names `tests/LamuFlix.UnitTests/Features/FixedTimeProvider.cs`. T001's worktree is `F:\Dev\LamuFlix.worktrees\feature-299` on `feature/299-…` as created by `/task` in Phase B; the pickup drift check confirms the exact path before T001 runs. |
| Ledger A1, A2 | Noted | No action. RecordFailure's four constructor args sit within the three-port cap because only `IMovieRepository` is a port. The `ValidationExceptionHandler` name is a follow-up naming question, not a fix commit. |
| Compass S1 | Accept | Deferring registration, validators and options binding is a constitution departure that Patron judged necessary (Q9), so §2.3(b) makes it an owner checkbox: **D3** above. plan.md's Constitution Check and Complexity Tracking now list it instead of "No constitution violations". |
| Compass S2 | Accept | D1 and D2 each now state that (A) or (C) drops a ticket-named handler, so AC1/AC2 cannot hold and the ticket text changes (§2.3(a)). SC-001 says the same, so Gate 1 cannot pass silently on "all unblocked handlers". |
| Compass S3 | Accept | New T030a [BLOCKED: D1] mirrors T030: (B) the `IMovieRepository` list-stranded member and its implementations, (C) `EnrichmentRequested` after an amendment, (A) nothing. The D1 checkbox names the unnamed-file and constitution impact. |
| Compass S4 | Accept | `EnrichmentOptions` is a non-positional record with `[Range(1, int.MaxValue)]` on the `MaxAttempts` `init` property. Compass showed that an attribute on a positional parameter lands on the constructor parameter only and never validates. New test T005a (`tests/LamuFlix.UnitTests/Pipeline/EnrichmentOptionsTests.cs`) pins it with `Validator.TryValidateObject`; this supersedes Ledger A2's "no action". |
| Compass S5 | Accept | Handlers compare SmartEnum members with `==`. If the T021 probe is red on `Ardalis.SmartEnum`, the fallback is `ReferenceEquals(movie.Status, EnrichmentStatus.X)` (members are singletons; it references only `System.Object` and Domain). Only a probe still red after the fallback reaches Patron, so Q13 stays about `MovieQuery`. T020, T021 and plan.md name the fallback. |
| Compass S6 | Duplicate | Same as Ledger F5; already fixed. |
| Compass S7 | Accept | Under the move-to-Ports option only T027 is replaced; T024-T026 stand. The Q13 text now says Patron's ruling must name every Library type that moves, at least the siblings `MovieQuery` references (`MovieSort`, `Page`, `RuntimeRange`, `YearRange`, `SortDirection`). |
| Compass S8 | Accept, split | US1 scenarios 1-3 are marked review checks: the architecture tests enforce sealed shapes, and T017 exercises the decision invariant and every action member. The one behavioural shape, the `EnrichmentOptions` annotation, gets T005a and scenario 4. |
| Compass S9 | Accept | plan.md now cites checklist 351-352 for `AddHandler` and 365-366 for options validated at startup (verified in `.specify/memory/constitution.md`). |
| Compass S10 | Noted | Seat-file mismatch (Compass's role file reads Sentry). Not a spec matter; Bernstein reconciles the seat file. |
| Sentry L1 | Accept, clarify (D2-gated) | Import handles one folder → one `Movie` → one `SaveChangesAsync`, with no compensation. A throw from Scan, `Movie.Create` or `AddAsync` means no Save and no Enqueue; a Save throw means no Enqueue; Save succeeding and Enqueue throwing leaves a `Pending` row that only the D1 sweeper recovers. If the owner picks D1(A), that row has no recovery until the follow-up, and the PR body must say so. T028 adds the `AddAsync`-throws case. |
| Sentry L2 | Accept, as a wiring rule | `Attempt < 1` is request validation, which the constitution places in the FluentValidation decorator (Principle V), not in the handler. FR-018 now names two validator rules the wiring ticket must deliver: `RecordEnrichmentFailureCommand.Attempt >= 1` and `EnrichmentOptions.MaxAttempts >= 1` (with `ValidateOnStart()`). The handler adds no guard; the edge case line points to FR-018 and D3. |
| Sentry duplicate-import note | No change | Already out of scope (Patron Q6: note it, file no ticket until existing tickets are checked). It stays in the PR body's follow-up list for Patron to route. |

## Round cap

- **Grill:** 12/12 questions used; there is no grill round 2. Q13 goes to Patron as a single post-grill ruling, with Conductor's approval to exceed the budget, or as a Quill `needs decision:`.
- **Quill:** at most 2 fix-list rounds on spec/plan/tasks against this brief before escalation to the Conductor.
