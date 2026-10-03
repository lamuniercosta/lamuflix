# Tasks: Browse and retrieve movies through the Api

## Six ordering notes. None is taken silently.

1. **The owner gate comes first.** Q1, Q2, Q8 and Q11 are all unchecked in `spec.md`. Do not start Phase B until the owner answers all four, the normal Gate 1 approval is recorded, and the DEV-308 implementation is merged on the build base (`brief.md` §§2-3, 8; `CONCLUSIONS.md` Q1, Q2, Q8, Q11).
2. **The branch-local mapper is inherited.** DEV-308 creates `LibraryEndpoints.cs` empty. DEV-309 fills it and adds no `Program.cs` route call (`recon-DEV-309.md:13-18`; DEV-308 spec FR-027).
3. **Keep one exception mapper.** Endpoint code throws or bubbles the existing exceptions. `UseStatusCodePages()` fills bodyless routing responses; it does not add an exception handler (`CONCLUSIONS.md` Q2/Q9; recon R5).
4. **Parse first, validate through the decorator.** Codec syntax errors become field-keyed `ValidationException`; representable values proceed through the real `BrowseMoviesQueryValidator` wrapper and the existing handler decorator. Do not add endpoint-side domain validation or server defaults (`CONCLUSIONS.md` Q1/Q3/Q4/Q9).
5. **Q8 is a public contract choice.** Do not substitute response DTOs or begin implementation until the owner answers. If approved, preserve every frozen scalar field and null rule; do not quietly choose converters or nested objects instead (`CONCLUSIONS.md` Q8).
6. **Q11 is a separate constitutional departure.** Endpoint metadata is not the committed OpenAPI document. Do not call the deferral approved, create web/OpenAPI work, or mark the checkbox closed before its answer (`CONCLUSIONS.md` Q11; recon R4).

## Phase 1: Setup — the pickup receipt and owner gate (T001-T005)

- [x] T001 Confirm the working tree is `F:/Dev/LamuFlix.worktrees/feature-309-spec`, branch `feature/309-spec`, and the DEV-308 implementation commit is present on the build base before implementation. Record the exact base/head and `git status`; stop Phase B if the prerequisite is absent.
- [x] T002 Read the live answers to Q1, Q2, Q8 and Q11 from the spec PR and reconcile `spec.md`, `plan.md` and `tasks.md` to those answers. Keep the checkboxes open until actual owner replies exist. If an answer rejects a recommendation, stop and reconcile the artifacts before code; do not decide for the owner.
- [x] T003 Obtain the normal Gate 1 approval after all four owner questions are answered. Record the approval evidence in the task note. A spec PR merge by itself does not supply Gate 1 approval or DEV-308 code.
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

- [ ] T013 Add the `{id:int}` details route. Parse `MovieId` with `TryCreate`; throw `NotFoundException` for non-positive ids; dispatch the existing `GetMovieDetailsQuery`; allow the handler's unknown-id `NotFoundException` to bubble. Add `WithName`, `WithSummary`, success metadata and `ProducesProblem(404)`. Do not catch or construct endpoint NotFound results.
- [ ] T014 Add `UseStatusCodePages()` immediately after `UseExceptionHandler()` in `Program.cs`, before CORS and endpoint execution. Preserve existing problem-details service registrations and the single exception mapper. Only if an exercised response lacks a non-empty trace id, add the minimum shared `AddProblemDetails` customization required and cite the failed acceptance.
- [ ] T015 Extend host integration tests for known details dispatch/mapping, unknown positive id, zero id, non-integer route id and unknown route. For each error assert expected status, `application/problem+json`, and non-empty `traceId`; ensure field errors are present for validation cases.
- [ ] T016 Verify browse and details endpoint metadata from `EndpointDataSource`: names, summaries, success response types and their matching ProblemDetails 422/404 metadata. Assert the emitted 422 body is ProblemDetails with an `errors` extension, not an assumed `HttpValidationProblemDetails` document.
- [ ] T017 Run applicable changed-file analysis/format checks and focused integration tests. Record exit codes and fix failures before moving to the next story.

**Checkpoint**: handler exceptions use the one exception mapper; routing misses receive a body through status-code pages; all tested errors carry the required content type and trace id.

## Phase 4: User Story 3 - Freeze the scalar success contract (Priority: P1)

- [ ] T018 Only after Q8 is answered affirmatively, create `LibraryResponses.cs` with the four internal sealed response records exactly as approved. Map value objects to scalars, use `Runtime.Minutes`, keep null metadata and nullable members present as JSON null, and preserve the frozen camelCase browse/details shapes. If Q8 is answered differently, stop and reconcile before writing this file.
- [ ] T019 Extend success integration tests for browse and details JSON with populated metadata, absent metadata and nullable metadata members. Compare fields/types/nulls without depending on JSON property order. Assert normalized format extension and runtime minutes.
- [ ] T020 Check the response mapping contains no converter, JSON-options registration, enrichment field, Contracts folder, generic response abstraction, new package or dependency. Record any required divergence for owner review; do not silently widen scope.
- [ ] T021 Run applicable changed-file analysis/format checks and focused tests. Keep implementation complexity at or below the harness implementation ceiling and extract helpers as needed to satisfy the later refactor ceiling of 6.

**Checkpoint**: both success JSON contracts match the approved frozen scalar shape and null behavior; route metadata describes those exact response DTOs.

## Phase 5: User Story 4 - Handoff the OpenAPI inputs to DEV-20 (Priority: P2)

- [ ] T022 Only after Q11 is answered affirmatively, verify the DEV-20 ownership comment/receipt identifies both routes, their DTO and endpoint metadata inputs, and the pending deferral. Its receipt does not itself close the checkbox; only the owner answer does. If Q11 is rejected or unanswered, stop and reconcile the contract-chain work before implementation proceeds.
- [ ] T023 Confirm the DEV-309 diff adds no OpenAPI package, document generation, `web/` folder, committed `openapi.json`, Verify snapshot, drift check or TypeScript file. Verify that the handoff names DEV-20 as document owner and DEV-320's separate generated-client ownership is not claimed as completed.

**Checkpoint**: Q11's owner answer is recorded, ownership boundaries are explicit, and no deferred artifact is claimed or produced in DEV-309.

## Phase 6: User Story 5 - Evidence, scope and close-out (Priority: P2)

- [ ] T024 Run the complete error matrix: parameter-less browse with its four failures, invalid Enumeration, `pageSize=101`, inverted range, unknown positive id, zero id, non-integer id and unknown route. Every error asserts status, `application/problem+json` and non-empty `traceId`; browse failures assert field-keyed `errors`; invalid browse never calls the catalog.
- [ ] T025 Run all applicable gates in harness order: changed-file Roslyn, cyclomatic complexity at implementation and refactor ceilings, InspectCode, format, full tests, property tests and vulnerable packages; mutation at its proper stage. Use `harness.yml` and each script's `-Help`. Record each result and exit code. Exit 1 or Could not run blocks; exit 2 for empty scope is blocking SKIPPED; only harness-disabled is non-blocking SKIP; surviving mutant routes to a missing test.
- [ ] T026 Inspect `git diff <base>...HEAD` and confirm only the approved implementation/test files plus any minimum acceptance-forced fix changed. Confirm no Core query rewrite, duplicate registration, project/package/threshold/schema/migration change, secret, machine path, local-play change, OpenAPI or web artifact. Name any forced extra edit with the concrete failed AC/gate it fixes.
- [ ] T027 Re-read all four owner checkboxes and compare the spec/plan/tasks against their actual answers. Confirm the PR body carries the gate results, diff boundary and error/contract evidence, and does not claim DEV-376 acceptance. Leave merge to the user.
- [ ] T028 Report the actual implementation head, merge readiness or blocking findings, applicable gate outcomes, and any limits. Do not describe any unrunnable or skipped-blocking gate as green.

**Checkpoint**: the complete error and success contracts are proved, applicable gates and scope are reported honestly, owner answers and prerequisites are recorded, and no DEV-376 acceptance is claimed.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: starts only after this spec is approved, all four owner questions have answers, Gate 1 is approved, and DEV-308 implementation is merged. T001/T004 can end the work if those prerequisites are absent.
- **Phase 2**: depends on Phase 1. The request mapper and validators establish the browse binding/validation seam.
- **Phase 3**: depends on Phase 2's handler/test host and shares `Program.cs` only for status-code pages; endpoint and error flow are proved together.
- **Phase 4**: depends on the Q8 owner answer and the approved scalar contract; success mapping and JSON assertions proceed together.
- **Phase 5**: depends on Q11 answer and DEV-20 ownership receipt; it changes no API behavior.
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
