# Tasks: Browse and retrieve movies through the Api

## Six ordering notes. None is taken silently.

1. **The owner decisions are settled.** Q1, Q2, Q8 and Q11 are approved in merged spec PR #81 and recorded as decided in `spec.md`. Do not start Phase B until the normal Gate 1 approval is recorded and the DEV-308 implementation is merged on the build base (`PR #81`; `brief.md` §§2-3, 8; `CONCLUSIONS.md` Q1, Q2, Q8, Q11).
2. **The branch-local mapper is inherited.** DEV-308 creates `LibraryEndpoints.cs` empty. DEV-309 fills it and adds no `Program.cs` route call (`recon-DEV-309.md:13-18`; DEV-308 spec FR-027).
3. **Keep one exception mapper.** Endpoint code throws or bubbles the existing exceptions. `UseStatusCodePages()` fills bodyless routing responses; it does not add an exception handler (`CONCLUSIONS.md` Q2/Q9; recon R5).
4. **Parse first, validate through the decorator.** Codec syntax errors become field-keyed `ValidationException`; representable values proceed through the real `BrowseMoviesQueryValidator` wrapper and the existing handler decorator. Do not add endpoint-side domain validation or server defaults (`CONCLUSIONS.md` Q1/Q3/Q4/Q9).
5. **Q8 is an approved public contract choice.** PR #81 approves scalar response DTOs; preserve every frozen scalar field and null rule, without converters or nested objects (`PR #81`; `CONCLUSIONS.md` Q8).
6. **Q11 is an approved OpenAPI deferral.** PR #81 approves DEV-20 ownership of the committed OpenAPI document, Verify snapshot and drift check for these routes. Endpoint metadata is not that document; do not create web/OpenAPI work here (`PR #81`; `CONCLUSIONS.md` Q11; recon R4).

## Phase 1: Setup — the pickup receipt and owner gate (T001-T005)

- [x] T001 Confirm the working tree is `F:/Dev/LamuFlix.worktrees/feature-309-spec`, branch `feature/309-spec`, and the DEV-308 implementation commit is present on the build base before implementation. Record the exact base/head and `git status`; stop Phase B if the prerequisite is absent.
- [x] T002 Read the live answers to Q1, Q2, Q8 and Q11 from merged spec PR #81 and reconcile `spec.md`, `plan.md` and `tasks.md` to those owner-approved decisions (`PR #81`).
- [x] T003 Obtain the normal Gate 1 approval after all four owner decisions are recorded. Record the approval evidence in the task note. PR #81 supplies the owner answers but does not by itself supply normal Gate 1 approval or DEV-308 code (`PR #81`).
- [x] T004 At pickup, confirm the inherited route mapper, `HandlerRegistration.cs`, `WebApplicationFactory<Program>`, handler registrations, `AddMovieCatalog`, problem-details service and test fixture are present on the implementation base. Record any mismatch and stop if the DEV-308 prerequisite is incomplete.
- [x] T005 Record the inherited route surface and test host recipe needed for the real host, including configuration supplied before production registration, `Features:LocalPlay=false`, the catalog fake seam, and the exact frozen file envelope. No secret or real connection string is copied into the note.

## Phase 2: User Story 1 - Browse movies through codec-faithful binding (Priority: P1) 🎯 MVP

- [x] T006 Create `src/LamuFlix.Api/Endpoints/BrowseMoviesRequest.cs` as the internal flat `[AsParameters]` request record with all 14 canonical keys. Use `string?` for scalar keys and `string[]?` for repeated keys; bind explicit query names. No typed query primitives, nested ranges, Core type edits or defaults.
- [x] T007 Implement focused request-to-`MovieQuery` mapping helpers. Read raw query presence/multiplicity; reject repeated scalar keys and partial ranges; distinguish absent values from present empty bounds and empty text from absent text; preserve repeated-array order; parse invariantly and match Enumeration names exactly. Convert supplied malformed values into field-keyed `ValidationException` failures.
- [x] T008 Add sealed `BrowseMoviesQueryValidator` under `src/LamuFlix.Infrastructure/Library/` delegating its nested query to unchanged `MovieQueryValidator`. Add sealed `GetMovieDetailsQueryValidator` there with the Id-not-null rule. Add both concrete scoped registrations and corresponding scoped `IValidator<TQuery>` factory registrations in `HandlerRegistration.cs` before their `AddHandler` rows. Keep them unconditional and do not add duplicate handlers or catalog rows.
- [x] T009 Fill the browse route in existing `LibraryEndpoints.cs`. Map to `BrowseMoviesQuery`, dispatch through the decorated handler, map the success result to the approved response contract, and add `WithName`, `WithSummary`, success metadata and `ProducesProblem(422)`. Do not short-circuit representable values around the validator.
- [x] T010 Add focused unit coverage for browse validator delegation and details Id-null/valid behavior. Add host integration coverage for validator resolution and verify the catalog is not called on invalid browse input.
- [x] T011 Add the full-generator FsCheck Format-to-dispatched-query property using the existing accepted `MovieQuery` generator without narrowing its domain. Add codec cases for empty and partial bounds, repeated scalar key, `text=` versus absent text, array ordering, exact/wrong-case names, and malformed integer input.
- [x] T012 Run the applicable changed-file analysis/format checks and focused tests at this increment. Record commands, exit codes and failures. Fix shape/test defects without suppression or threshold changes; a gate that cannot run blocks.

**Checkpoint**: all generator-accepted queries round-trip to equal dispatched queries; invalid syntax is field-keyed and invalid representable queries are rejected by the decorated validator; invalid browse never reaches the catalog.

## Phase 3: User Story 2 - Retrieve movie details and share error handling (Priority: P1)

- [x] T013 Add the `{id:int}` details route. Parse `MovieId` with `TryCreate`; throw `NotFoundException` for non-positive ids; dispatch the existing `GetMovieDetailsQuery`; allow the handler's unknown-id `NotFoundException` to bubble. Add `WithName`, `WithSummary`, success metadata and `ProducesProblem(404)`. Do not catch or construct endpoint NotFound results.
- [x] T014 Add `UseStatusCodePages()` immediately after `UseExceptionHandler()` in `Program.cs`, before CORS and endpoint execution. Preserve existing problem-details service registrations and the single exception mapper. Only if an exercised response lacks a non-empty trace id, add the minimum shared `AddProblemDetails` customization required and cite the failed acceptance.
- [x] T015 Extend host integration tests for known details dispatch/mapping, unknown positive id, zero id, non-integer route id and unknown route. For each error assert expected status, `application/problem+json`, and non-empty `traceId`; ensure field errors are present for validation cases.
- [x] T016 Verify browse and details endpoint metadata from `EndpointDataSource`: names, summaries, success response types and their matching ProblemDetails 422/404 metadata. Assert the emitted 422 body is ProblemDetails with an `errors` extension, not an assumed `HttpValidationProblemDetails` document.
- [x] T017 Run applicable changed-file analysis/format checks and focused integration tests. Record exit codes and fix failures before moving to the next story.

**Checkpoint**: handler exceptions use the one exception mapper; routing misses receive a body through status-code pages; all tested errors carry the required content type and trace id.

## Phase 4: User Story 3 - Freeze the scalar success contract (Priority: P1)

- [x] T018 Built in commit `1b419a2` ("DEV-309 - Freeze the scalar success contract"), verified at HEAD `dd725d7`: `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` holds exactly the four approved `internal sealed record` types — `BrowseMoviesResponse` (:9), `MovieSummaryResponse` (:17), `MovieDetailsResponse` (:19), `MovieMetadataResponse` (:30). Value objects map to scalars, `metadata.Runtime?.Minutes` at :42, absent metadata and absent nullable members stay present as JSON null, and the frozen camelCase shapes are preserved. The file is 47 lines at HEAD after `f3e7477` applied accepted finding A4.
- [x] T019 Built in commit `1b419a2`. `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs`: browse success shape :349, populated metadata with all seven members :375, absent metadata kept as JSON null :416, sparse metadata nullable members kept as JSON null :436. Comparison is property-order independent (`ShouldCarryExactly`/`ShouldCarryJsonNull` at :598-608 use `ignoreOrder: true`), the normalized format extension is asserted as `mkv` (:398, :431) and runtime minutes as `131` (:408). All four pass in the T024 run (39/39).
- [x] T020 Checked by added-line scan of `src` and `tests` over `git diff origin/main...HEAD`: no `JsonConverter`, `JsonSerializerOptions` or `ConfigureHttpJsonOptions` call, no `Contracts` folder, no generic response abstraction, no new package. The only `Enrichment` hits are the canonical `statuses` codec key (`LibraryEndpoints.cs` `TryParseStatus`), its test assertion and the catalog fake — no enrichment field was added to a response. New directories are only the two planned ones (`src/LamuFlix.Infrastructure/Library`, `tests/LamuFlix.UnitTests/Library`). No divergence to escalate.
- [x] T021 Analysis and test gates at HEAD `dd725d7` all exit 0: changed-file Roslyn, cyclomatic complexity at the implementation ceiling 15 and at the refactor ceiling 6, InspectCode, and the full suite (621 tests). Recorded exception, not a green claim: the `dotnet format --verify-no-changes` gate FAILS on `LibraryResponses.cs(46,2)` (missing final newline, review finding A7). See T025 — that failure blocks the full gate sweep and is routed to Cog.

**Checkpoint**: both success JSON contracts match the approved frozen scalar shape and null behavior; route metadata describes those exact response DTOs.

## Phase 5: User Story 4 - Handoff the OpenAPI inputs to DEV-20 (Priority: P2)

- [x] T022 Built in commit `a5889db` ("DEV-309 - Handoff the OpenAPI inputs to DEV-20"). Verified in that commit's diff: the receipt named `GET /api/movies` and `GET /api/movies/{id}`, the `BrowseMoviesResponse` and `MovieDetailsResponse` DTOs in `LibraryResponses.cs`, the `WithName`/`WithSummary`/`Produces`/`ProducesProblem` metadata as inputs, the Q11 deferral of document generation, `web/src/api/openapi.json`, its Verify snapshot and the drift check to DEV-20, and DEV-320 retaining generated-client ownership. Receipt location note: `f3e7477` deleted that in-code copy as accepted finding A1 (AGENTS.md:21 permits only three comment exemptions), so at HEAD `dd725d7` the receipt lives in commit `a5889db`, `CONCLUSIONS.md` Q11 and `spec.md:20`, not in the source file. That was an accepted ruling, not a lost handoff; nothing further is owed in code.
- [x] T023 Verified on `git diff --name-only origin/main...HEAD` (14 files): no OpenAPI package, no `WithOpenApi`/document-generation call, no `web/` folder, no `openapi.json`, no Verify snapshot, no drift check, no TypeScript file. An added-line scan for `openapi` returns only spec/task prose describing the deferral. The sole `.csproj` delta is the A3 forced reference pair (see T026). DEV-20 is named document owner; DEV-320's generated-client ownership is not claimed as done anywhere in the diff.

**Checkpoint**: Q11's approval in merged spec PR #81 is recorded, ownership boundaries are explicit, and no deferred artifact is claimed or produced in DEV-309 (`PR #81`).

## Phase 6: User Story 5 - Evidence, scope and close-out (Priority: P2)

- [x] T024 Complete error matrix run at HEAD `dd725d7` against the real `WebApplicationFactory<Program>` host with the recording `IMovieCatalog` fake. Command: `dotnet test tests/LamuFlix.IntegrationTests --filter "FullyQualifiedName~LibraryEndpointsTests"` → **Passed, 39 of 39, 0 failed, exit 0**. Matrix rows and their tests: parameter-less browse with its four failures → `LibraryEndpointsTests.cs:245`, exactly `Query.Direction`, `Query.Page.Number`, `Query.Page.Size`, `Query.Sort`; invalid Enumeration → `:195` (`statuses=pending`) fails field-keyed on `statuses`; `pageSize=101` → `:264` rejects `Query.Page.Size`; inverted range → `:283` `Query.Year` and `:284` `Query.Runtime`; unknown positive id → `:457`; zero id → `:474`-`:476` (`0` and `-1`); non-integer id → `:493`; unknown route → `:510`. Every row runs through `ReadProblemAsync` (:574) or `ReadNotFoundAsync` (:585), which assert status, media type `application/problem+json` and a non-empty `traceId`; every browse failure asserts field-keyed `errors` and `catalog.Queries.ShouldBeEmpty()`, so invalid browse never calls the catalog. Verified limit on one row: the tested invalid-`Enumeration` case is a wrong-case name; a wholly unknown name takes the identical path — `EnrichmentStatus.TryFromName(raw, false)` at `LibraryEndpoints.cs:278`-`:279` returning false into the same `state.Fail(key, NamesMessage(...))` — so no separate bogus-name case exists.
- [ ] T025 **BLOCKED — two gates fail; this task is not ticked.** Every command was run from the repo root `F:\Dev\LamuFlix.worktrees\DEV-309` at HEAD `dd725d7`, clean tree, in harness order: (1) `pwsh -NoProfile -File ./scripts/run-roslyn-analyzers.ps1` → checked 10 changed `.cs` files against `origin/main`, "passed (no CA/IDE warnings)", **exit 0**; (2) `pwsh -NoProfile -File ./scripts/run-cyclomatic-complexity.ps1` → "no methods exceed threshold of 15", **exit 0**; (3) `pwsh -NoProfile -File ./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` (refactor ceiling, per the script's own `-Help`) → "no methods exceed threshold of 6", **exit 0**; (4) `pwsh -NoProfile -File ./scripts/run-jetbrains-inspectcode.ps1` → "InspectCode: passed (no WARNING+ issues)", report at `artifacts/inspectcode/inspect-report.sarif.json`, **exit 0**; (5) `dotnet format --verify-no-changes` → **FAIL, exit 2**, the single diagnostic `src/LamuFlix.Api/Endpoints/LibraryResponses.cs(46,2): error WHITESPACE ... Substituir 1 caracteres por '\r\n'` (missing final newline — review finding A7, never applied); (6) `dotnet test` → ArchitectureTests 13/13, UnitTests 384/384, IntegrationTests 224/224, 621 passed, 0 failed, **exit 0**; (7) `pwsh -NoProfile -File ./scripts/run-property-tests.ps1` → "Property tests: passed (16 test(s) executed across 3 project(s))", **exit 0**; (8) `pwsh -NoProfile -File ./scripts/run-vulnerable-packages.ps1` → "PASS: no vulnerable packages", **exit 0**; (9) `pwsh -NoProfile -File ./scripts/run-mutation.ps1` → **FAIL, exit 1**, printed "Mutation testing: FAILED - no eligible test project (a test project that directly references it, excluding *.ArchitectureTests) for: LamuFlix.Api.csproj." Cause is the script's own scope rule at `scripts/run-mutation.ps1:102`, which excludes `*ArchitectureTests` and `*IntegrationTests`, and the only project referencing `LamuFlix.Api.csproj` is `LamuFlix.IntegrationTests`; so this is a harness/script limitation for any branch touching `src/LamuFlix.Api`, not a test-coverage defect, and no mutation score exists for this ticket. Not worked around: `-Project LamuFlix.Infrastructure` was deliberately NOT run, because it would exclude `LibraryEndpoints.cs`, the principal changed file. Both failures are routed to Cog as fixes; neither is described as green.
- [x] T026 Audited `git diff origin/main...HEAD` at `dd725d7`: 14 files, +1356/-50. Approved set: `specs/DEV-309/{CONCLUSIONS,spec,tasks}.md`, `src/LamuFlix.Api/Endpoints/{BrowseMoviesRequest,LibraryEndpoints,LibraryResponses}.cs`, `src/LamuFlix.Api/{HandlerRegistration,Program}.cs`, `src/LamuFlix.Infrastructure/Library/{BrowseMoviesQueryValidator,GetMovieDetailsQueryValidator}.cs`, `tests/LamuFlix.IntegrationTests/{ApiHostCompositionTests,LibraryEndpointsTests}.cs`, `tests/LamuFlix.UnitTests/Library/LibraryQueryValidatorTests.cs`, `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj`. Confirmed absent: no `src/LamuFlix.Core` file at all (no `MovieQuery`/`MovieQueryValidator` rewrite), no duplicate registration (`HandlerRegistration.cs` adds two concrete validators plus two `IValidator<T>` factory rows and no second `AddHandler` or `IMovieCatalog` row; `Program.cs` adds only `UseStatusCodePages()`), no project/package/threshold/schema/migration change (`harness.yml`, `CodeMetricsConfig.txt`, `stryker-config.json`, `.editorconfig`, `Directory.Packages.props` and every `.csproj` except the forced test reference are untouched; no `Persistence`/`Migrations` path in the diff), no secret and no machine path (`Features:LocalPlay` untouched, and an added-line scan for `password|secret|token=|api[_-]?key|ConnectionString|Host=|C:\Users|F:\Dev` returns nothing), no OpenAPI or web artifact. **Forced extra edit, named as A3:** `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` gains `<PackageReference Include="FsCheck" />` and `<ProjectReference Include="..\LamuFlix.UnitTests\LamuFlix.UnitTests.csproj" />`, forced by `plan.md:68,105` requiring the full existing `MovieQuery` generator, which lives at `tests/LamuFlix.UnitTests/Library/MovieQueryFixture.cs:8`; FsCheck is already centrally pinned (`Directory.Packages.props:44`), so §2.3 item 1 does not apply. Patron ruling: `specs/DEV-309/CONCLUSIONS.md`, "Phase 4 step 4 — accepted finding A3"; origin: `findings-DEV-309-adjudication` A3. The only other minimum acceptance-forced edit is `ApiHostCompositionTests.cs:131`, whose DEV-308 assertion that no `/api` route exists had to become the two-route assertion once these routes shipped.
- [x] T027 Verified at `dd725d7`: `spec.md:13`-`:22` records Q1 (flat all-string `[AsParameters]` request), Q2 (422/404 from the single `IExceptionHandler`), Q8 (scalar Api response DTOs with the frozen FR-009 contract) and Q11 (OpenAPI deferral to DEV-20) as **decided and approved in merged spec PR #81**, each cited to `CONCLUSIONS.md` and its recon note, reconciled by commit `dd725d7`. Approved contract evidence is preserved: `spec.md` FR-009 (frozen scalar field/type contract) and FR-011 (deferred document, no artifact produced here), SC-006 (diff stays inside the envelope), and `tasks.md` ordering notes 5 and 6 restating the Q8 scalar contract and the Q11 deferral. No fifth checkbox was invented for the forced status-code-pages/traceId and validator work (`CONCLUSIONS.md` Q9, Q10).
- [x] T028 Implementation head `dd725d7` ("DEV-309 - Reconcile spec and tasks to merged spec PR #81 (A2) and rewrite close-out tasks") on `feature/DEV-309`, tree clean before this evidence edit, base `origin/main`. Applicable gate outcomes and exit codes are recorded verbatim in T025: seven commands exit 0 (Roslyn, complexity at 15, complexity at 6, InspectCode, `dotnet test`, property tests, vulnerable packages) and two fail (`dotnet format --verify-no-changes` exit 2 on `LibraryResponses.cs(46,2)`; `run-mutation.ps1` exit 1, no eligible test project for `LamuFlix.Api.csproj`). Blocking findings, both routed to Cog: (B1) the missing final newline is accepted finding A7 from the last review round, left unapplied by `f3e7477`; (B2) the mutation gate cannot select a test project for `LamuFlix.Api` because `run-mutation.ps1:102` excludes `*IntegrationTests` — it is a harness limitation, and no mutation score or surviving-mutant verdict exists for DEV-309. Verification limits: gates ran with their default changed-file scope against `origin/main`, never `-All`; no threshold was altered and no gate was narrowed to obtain a pass; no DEV-376 acceptance is claimed; nothing was pushed and no PR was opened or edited from this task.

**Checkpoint**: the complete error and success contracts are proved, applicable gates and scope are reported honestly, the owner decisions and remaining prerequisites are recorded, and no DEV-376 acceptance is claimed.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: starts only after this spec is approved, the four owner decisions from merged spec PR #81 are recorded, Gate 1 is approved, and DEV-308 implementation is merged. T001/T004 can end the work if those prerequisites are absent (`PR #81`).
- **Phase 2**: depends on Phase 1. The request mapper and validators establish the browse binding/validation seam.
- **Phase 3**: depends on Phase 2's handler/test host and shares `Program.cs` only for status-code pages; endpoint and error flow are proved together.
- **Phase 4**: depends on the Q8 approval recorded in merged spec PR #81 and the approved scalar contract; success mapping and JSON assertions proceed together (`PR #81`).
- **Phase 5**: depends on the Q11 approval recorded in merged spec PR #81 and DEV-20 ownership receipt; it changes no API behavior (`PR #81`).
- **Phase 6**: depends on all implementation phases; it is verification and handoff only.

### User Story Dependencies

- **US1 (P1)**: first implementation increment after setup; delivers browse parsing, mapping and validator evidence.
- **US2 (P1)**: builds the details route and common error-body behavior on the same real host.
- **US3 (P1)**: requires Q8 approval before DTO records and their contract tests are authored.
- **US4 (P2)**: requires Q11 owner approval and DEV-20 handoff receipt.
- **US5 (P2)**: requires all previous stories and applicable gates.

### Within Each User Story

- The real host and DEV-308 composition start before request assertions; do not replace decorated handlers.
- Validator interface and concrete registrations must exist before dispatch tests run.
- Codec parse failures are field keyed; representable invalid values must traverse the decorator.
- Error-path tests assert the response body, content type and trace id as well as status.
- Response JSON asserts explicit nulls and scalar value-object mappings.
- A red run is recorded; do not rerun repeatedly until green or silently replace a red result.

### Parallel Opportunities

No implementation tasks are marked parallel. Request mapping, handler registration, endpoint code and host tests share interfaces and incrementally depend on one another. Validator unit tests can be developed separately only after the types and rules are fixed, but the plan keeps their tasks sequential to preserve a single reviewable integration point.

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1's prerequisite and approval checks.
2. Complete Phase 2, including validator registrations, the full-generator property and codec edges.
3. Stop and validate: the real host starts, valid queries reach the recording catalog unchanged, and invalid queries do not reach it.

### Incremental Delivery

1. Phase 1 + Phase 2 → codec-faithful browse binding and decorated validation.
2. + Phase 3 → details retrieval and shared error-body behavior.
3. + Phase 4 → frozen scalar success contract.
4. + Phase 5 → DEV-20 OpenAPI handoff under the approved deferral.
5. + Phase 6 → full gates, frozen-diff proof and PR handoff.

### Parallel Team Strategy

The implementation path is sequential because the request/validator registrations and endpoint/host tests are connected increments, and `Program.cs` is touched only for the shared bodyless-error middleware. Keep the mapper and error behavior in one coordinated sequence; avoid parallel edits to the same files.

## Notes

- `[P]` is unused; no task is independent enough to mark parallel in this plan.
- Commit after each task or logical group using the `DEV-309 - {subject}` message form.
- `CONCLUSIONS.md` and `ASSUMPTIONS.md` are Patron-owned; implementation tasks never edit them.
- No phase, test or gate is authorized by the draft itself. Gate 1 and the DEV-308 implementation prerequisite remain controlling.
