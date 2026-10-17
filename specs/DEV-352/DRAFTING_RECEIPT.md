# DEV-352 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-17
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-352-spec` | **Branch**: `feature/352-spec` | **HEAD at drafting**: `1cdb13c487c1b257f20cc4d2466d31414190a814`

**Method**: `/speckit-specify`, `/speckit-plan`, and `/speckit-tasks` followed from the skills and `.specify/templates/`. `setup-plan.ps1 -Json` and `setup-tasks.ps1 -Json` were run in this worktree with `PYTHONUTF8=1` during the original draft. No `.specify/extensions.yml` hooks were present. Round 1 content comes from `brief.md`, `CONCLUSIONS.md` Q1–Q11, `ASSUMPTIONS.md`, notes `analyze-DEV-352` (lines 44–58) and `DEV-352`, and `specs/PRODUCT.md`. No decision was made by Quill.

**Round**: Phase 2 step 3.3 speckit-analyze fix round 1. Applied analyze-DEV-352 items 1–7 to `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, and this receipt. This round does not freeze the plan, open Gate 1, or authorize implementation. No production code was edited. No delivery gate was run. `CONCLUSIONS.md` stays append-only.

**Owner checkboxes** (unresolved). Q11 supersedes the historical Q6 statement of zero owner checkboxes and zero constitution departures. Gate 1 stays closed until the user resolves both on the spec PR and the spec PR merges. No Patron ruling, drafting fix, or prerequisite success resolves them.

- [ ] blocked: structural — Approve departing from .specify/memory/constitution.md:219-220 (all commands validated through FluentValidation in the validation decorator) for the four MovieId-only feedback commands by validating route id via MovieId.TryCreate (HTTP 422) at the endpoint boundary without separate IValidator<TCommand> classes, while retaining the existing validation decorator through AddHandler?
- [ ] blocked: structural — Approve departing from .specify/memory/constitution.md:335-342 (single fresh Initial migration without constitution amendment/ADR) by generating a reversible additive EF migration for movies.is_watched and movies.is_not_interested without editing 20260930153520_Initial.cs?

**Closed drafting questions** (the former needs-decision lines; the stop instructions on T001, T003, and T023 are removed):

1. Closed by Q9. `InvalidTransitionException` pairs are method-name actions and const state tokens: MarkWatched/Watched, UnmarkWatched/NotWatched, MarkNotInterested/NotInterested, UnmarkNotInterested/Interested. Interested means only `IsNotInterested == false`. That wording is the `[assumed]` entry in `ASSUMPTIONS.md`. Domain tests and HTTP 409 tests assert the exact pairs.

2. Closed by Q10. T023 extends existing `tests/LamuFlix.IntegrationTests/MigrationTests.cs`. Each proof uses `PostgresFixture.CreateEmptyDatabaseContextAsync` for its own database and never downgrades the shared fixture database. Up calls `IMigrator.MigrateAsync("20261016195556_AddMovieWriters")` and then `MigrateAsync(null)`. Down calls `MigrateAsync(null)` and then `MigrateAsync("20261016195556_AddMovieWriters")`. The proof asserts `is_watched` and `is_not_interested` are required and default false, preexisting rows and the prior schema and data survive both directions, and Down removes only those two columns. The migration count and name tripwire in `MigrateAsync_EmptyDatabase_AppliesInitialAndHasNoPendingModelChanges` moves from 2 to 3, keeps the exact previous names, and asserts the generated migration.

**Needs decision**: none for Quill. The two owner checkboxes above are user decisions on the spec PR.

**Round-1 propagation**:

1. The FluentValidation checkbox is in `spec.md`, the plan constitution check, the task prerequisite, T015, and this receipt. Endpoint `MovieId.TryCreate` and the existing validation decorator stay. The omission of separate validators is pending user approval.
2. The additive-migration checkbox is in those same handoff places and on T006. Zero-departure and zero-checkbox assertions in these drafts are superseded by Q11. Historical grill text in `CONCLUSIONS.md` is unchanged.
3. Q9 tokens and the assumed Interested wording are in the spec, plan, domain tasks, and HTTP 409 task.
4. Q10 names the existing migration test, isolation, migrator targets, column rules, preservation rules, and the 2-to-3 tripwire. Obsolete file stops are removed.
5. OpenAPI is exported from the C# contract first. `npm run generate:api` reads the committed document and writes `web/src/api/types.ts` and `web/src/mocks/handlers.ts`. It does not export OpenAPI. Neither generated file is hand-edited. T021 may change `movieDetailsApi.ts`, `client.typecheck.ts`, `web/src/mocks/fixtures.ts`, and `web/src/mocks/handlers.test.ts` only as forced, plus `web/src/mocks/generate-api.test.ts` in that envelope: the ten-operation lock becomes those ten plus the four approved operation identities (14), and the byte and staleness checks stay. Evidence: replacement recon-DEV-352:13-34,38-68.
6. Web typecheck, runtime-schema and contract regression tests, web tests, web build, generated-output drift, and the generator and generated-handler regressions are scheduled as separate verdicts. Lint does not cover that set. No new command flags or thresholds. Execution stays implementation-stage work.
7. T016 requires zero-save assertions for unknown positive (404), nonpositive (422), and malformed and overflowing (400) ids on all four operations, matching SC-003, and keeps the success and conflict matrix.

**Not decisions** (brief constraints, recorded so they are not read as new rulings):

- New fields and columns are appended after the existing watchlist members, in the brief's watched then not-interested order, because the brief does not authorize reordering existing members.
- `MapFeedbackEndpoints` and the four `AddHandler` calls are appended without reordering existing registrations.
- New test and command file names mirror the watchlist files recon names.
- Property cases are authored in T002 (brief Q7 item 1). T025 re-runs the property gate (brief Q8). That is the brief's split, not a second suite.
- Persistence and read-projection filenames that recon does not give stay unnamed. Tasks edit only the existing `IsInWatchlist` copy.
