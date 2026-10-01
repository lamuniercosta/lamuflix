# DEV-306 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-01
**Author**: Quill (Writer). Drafted in `F:\Dev\LamuFlix.worktrees\feature-306-spec`, branch `feature/306-spec`, from `4dd5e73` (brief). `git rev-parse --show-toplevel` and `git branch --show-current` confirmed the worktree before any write; the main checkout was not touched. The only pre-existing working-tree change, ` M .specify/feature.json` (the `specs/DEV-306` pin, set in Phase 1), was preserved and not staged. `specs/DEV-306/brief.md` was modified in the worktree by Keel during this drafting pass, to add §Drafting decisions D1; that edit is Keel's and is not staged or altered here.
**Method**: `/speckit-specify` executed against `.specify/templates/spec-template.md`; `setup-plan.ps1 -Json` and `setup-tasks.ps1 -Json` run for path resolution (plan and tasks drafted by hand from their templates in the same pass, precedent DEV-303 and DEV-304). `$env:PYTHONUTF8='1'` set before every invocation. `.specify/extensions.yml` does not exist, so no extension hooks ran in either direction. `AVAILABLE_DOCS` came back empty and none were created, for the reason stated in `plan.md`'s **Note on the Spec Kit phases**.

**Inputs**: `specs/DEV-306/brief.md` (223 lines), `CONCLUSIONS.md` (Q1-Q10), `ASSUMPTIONS.md`, note `recon-DEV-306` (88 lines, baseline `3b2e998`), `specs/PRODUCT.md`, `.specify/memory/constitution.md`, `harness.yml`, and every source file the brief cites — re-read at this HEAD, not taken from recon: both fixtures, `LamuFlixDbContextFactory.cs`, `ContainerFixture.cs`, `AssemblyInfo.cs`, `MovieCatalogCollection.cs`, `MovieCatalogSeed.cs`, `MigrationTests.cs`, `RabbitMqTopologyTests.cs`, `RabbitMqProbe.cs`, all fourteen consumer class declarations, `RabbitMqTopology.cs`, `RabbitMqConnectionOwner.cs`, `RabbitMqOptions.cs`, `EnrichmentOptions.cs`, both `.csproj` files, `Directory.Packages.props`, and the solution's project list.

## Artifacts written

- `specs/DEV-306/spec.md` — 7 user stories (US1 collection-scoped sharing, US2 a ready database, US3 a genuinely empty reset, US4 a broker whose topology exists, US5 broker isolation, US6 failure-safe lifetime and verified teardown, US7 evidence and boundaries), 43 acceptance scenarios, 16 edge cases, FR-001 to FR-030, 6 key entities, SC-001 to SC-010.
- `specs/DEV-306/plan.md` — technical context, constitution check (PASS, no departures), project structure of twenty-four changed files with two deletions (twenty-two still on disk, which is what the gate pass walks), Design §1-§7 plus the ruled §8 and the branch table §9, test strategy with the green-steps rule, the fixed container-identity emission mechanism and the four evidence captures, and the gate set including the two deliberately-not-green gate lines.
- `specs/DEV-306/tasks.md` — 43 tasks in 8 phases (Setup, Foundational, US2, US3, US4, US5, US6, US7), each with an exact file path, plus dependencies, parallel opportunities and incremental strategy. The brief's task ordering is followed verbatim.
- `specs/DEV-306/checklists/requirements.md` — 17 quality items, all resolved, with seven items recorded for spec review.
- `specs/DEV-306/DRAFTING_RECEIPT.md` — this file.

`CONCLUSIONS.md` and `ASSUMPTIONS.md` were **not** edited. They are Patron's files, and nothing in this drafting pass changed a ruling.

## The one question escalated, and its ruling

`needs decision:` **Should `PostgresCollection` and `RabbitMqCollection` carry `DisableParallelization = true`, as the `MovieCatalogCollection` they replace does?**

Escalated because it changes observable run behaviour, it contradicted the only in-repo precedent, and the writer's job was to make the consequence visible rather than absorb it. `MovieCatalogCollection.cs:5` — the only collection definition in the repository, and the file this ticket deletes — sets the attribute, which in xUnit does more than serialise within a collection: it stops that collection running alongside **any** other. Inheriting it would have serialised the two collections against each other and forfeited the overlap AC7 asks to be measured. Q3 requires both clauses, and the attribute supplied only the first.

**RULED: Outcome A, omit it. `brief.md` §Drafting decisions, D1, Keel, spec-review round 1.** Basis as recorded there: Q3 rules both clauses explicitly, and `CONCLUSIONS.md` outranks the deleted file's precedent. The drafts already followed that reading, so no requirement, no phase and no file list changed. Three things were updated to cite D1 and to carry the obligation it adds:

| Where | Change |
|---|---|
| `plan.md` §8 | retitled from *Escalated* to *Ruled*, states the basis, and gains the three consequences: AC7's timing is a concurrent run; cross-collection interference is a stop-and-report, never a fix by adding the attribute or a third collection (feeding R1/R6); and why the attribute was originally set is unrecoverable, so the stop condition covers the risk instead |
| `plan.md` §1, Constitution Check | the attribute note is now a ruling reference rather than an open question; the constitution status line no longer carries an escalation |
| `spec.md` Assumptions | the parallelism assumption now cites D1 by name and names the stop condition |
| `tasks.md` T002, T003, T004, T010, T036, Dependencies, Parallel Opportunities, Notes | T002 records the ruling instead of blocking; T003/T004 are `[P]` again and state the attribute is absent and must not be added back; T010 gains the stop condition at the first run that can trigger it; T036 records the timing as concurrent |

Nothing else in the draft was waiting on the answer.

## Everything else was a branch the brief or the rulings already delegated

| Choice | Branch taken | Why it needed no ruling |
|---|---|---|
| Connection-cancellation shape (`brief.md` Design §2 offers two) | additive `CreateConnectionAsync(CancellationToken)` overload; existing no-token member preserved | `RabbitMqProbe.cs:201` is the no-token member's only caller, so preserving it leaves that file unedited; `RabbitMqConnectionOwner.cs:37` already uses the token-accepting overload. Q9 satisfied either way (`plan.md` §9) |
| Collection name strings (rulings fix the **type** names only) | `nameof(PostgresCollection)` / `nameof(RabbitMqCollection)` on the definitions and every consumer; **no holder file** | The type name becomes the one source for the string xUnit matches on, so definition and consumers cannot drift and a rename is a compile error. A holder would have needed a Frozen-scope ruling for no gain, because `tests/LamuFlix.IntegrationTests/IntegrationCollections.cs` is not on the brief's **In** list (`plan.md` §9) |
| Truncate cached vs rebuilt per reset | built once at startup, cached | The brief's Design §1 step 3 says to cache it; rebuilding re-walks the model per test for nothing |
| Container teardown call shape | the async disposal overload, called with **no** token | Q9 requires cleanup after cancellation; the existing code already calls it that way at `PostgresFixture.cs:27` |
| Identity restart | `RESTART IDENTITY` on the one truncate | US3 scenario 3 requires it; it is the only server feature that gives it and it costs nothing over a plain truncate |
| The stale fixture comment at `RabbitMqFixture.cs:12` | delete, do not rewrite | Q3 makes the sentence false and `AGENTS.md` forbids explanatory comments, so there is nothing to rewrite it into (`plan.md` §7) |
| Empty-database creation shape | no `TEMPLATE` clause, matching the existing private helper | R5 is satisfied by the clause's absence, and `LamuFlixDbContextFactory.cs:44` already has the right shape |

## Recorded, not escalated — seven items from `checklists/requirements.md`

1. **Sharing narrows from assembly-wide to per-collection.** Agreed (Q3 and the ticket's own words), and every current consumer is placed in one of the two collections so none loses access. Recorded because it is the change most likely to surprise a reader of the diff who assumes the wiring is a pure refactor.
2. **`MigrationTests` is a collection member that deliberately does not reset.** Q2 gives it the empty-database path because it proves the opposite property. Recorded so the omission reads as a ruling, not a miss.
3. **`LamuFlixDbContextFactory.CreateContext(PostgresFixture)` becomes unreferenced and must be left alone.** It is called only from `PostgresFixture.CreateContext()` (`LamuFlixDbContextFactory.cs:10-14`, `PostgresFixture.cs:31`), and the brief removes the caller. AC8 freezes the helper's public static members. It is public in a non-packable, non-test project, so the uncalled-private-code analyzer does not reach it; an inspection finding there is a finding about AC8, not a defect to delete around.
4. **`Options.Create` resolves transitively — verified, not assumed.** `Microsoft.Extensions.Options` is a `PackageReference` in `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`, which `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj:20` references, so the broker fixture's new startup path needs no package and no `.csproj` edit. Recorded because the *obvious* fix for a compile failure here would be a `PackageReference` edit, which AC8 and §2.3 item 1 both forbid; the correct response is a stop.
5. **What `TestContext.Current` offers inside a collection fixture — verified, not assumed, and the two halves differ.** `ITestContext.CancellationToken` carries no nullability caveat, so a collection fixture's `InitializeAsync` has a live token and Q9's "where the API supports it" needs no fallback there. `ITestContext.TestOutputHelper` is documented as available only when `ITestContext.Test` is non-null, and a collection fixture is created under the *test case's* context where `Test` is `null`.
6. **The container-identity evidence is emitted from `[Fact]` bodies, and that placement is forced by item 5.** A test class's `IAsyncLifetime.InitializeAsync` runs before `TestOutputHelper.Initialize` is called, so the write throws `There is no currently active test.`; `[CaptureConsole]` funnels into the same helper and inherits the limit; and `SendDiagnosticMessage` needs an `xunit.runner.json` or a `.runsettings` to be surfaced, which is a file outside the Frozen scope. So it is four new `[Fact]`s, two per collection, using `TestContext.Current.TestOutputHelper` and surfaced by a `dotnet test --logger` command-line option — no new file, no `.csproj` change (`plan.md` §Test Strategy evidence 1).
7. **Two gate lines that are deliberately not green-and-passing.** The property gate is expected to exit 2 (scope-empty) and DEV-306's opt-out is recorded per `harness.yml:30-35`; the mutation gate is not applicable because no `src/` code changes; the vulnerable-packages gate is out of scope because no dependency is added or removed. Carried as T041 so none is discovered at the gate.

## Spec review round 1 findings, applied in one pass

Keel's spec review round 1 returned 0 CRITICAL, 3 HIGH, 5 MEDIUM, 4 LOW. All twelve are applied. The three that changed the shape of the draft rather than its wording:

- **HIGH — the collection-name holder was outside the frozen scope.** `IntegrationCollections.cs` was never on the brief's **In** list, so T003 and the holder are gone; `nameof` on both definitions and every consumer gives the same single-source guarantee with no new file. Every task below T003 is renumbered, and `plan.md` §1, §9 and §Project Structure follow.
- **HIGH — no task produced the AC4 and AC5 evidence.** Four tasks now emit the two owned container identities, from `[Fact]` bodies, because the placement the first draft chose provably throws; the mechanism and the reason are named in `plan.md` §Test Strategy, recorded as `checklists/requirements.md` item 7, and carried in `tasks.md` Notes. The Phase-2 green run no longer records any identity (its containers prove nothing about the final run's), and T037/T038 read the identities T036's final run emitted.
- **HIGH — a full green run was required where the suite cannot be green.** T014 moves eight consumers onto one shared migrated database and the reset does not arrive until T020/T022, so T019 runs the gates and the catalog-contract test only, and T024 is the first full-suite green on `postgres:17-alpine`.

The remaining nine: file counts reconciled to one number (24 changed, 2 deleted, 22 gate-listed); FR-019's `RabbitMqTopologyTests` exception stated and its unclear last sentence rewritten; the four Phase-4 tasks retagged `[US3]`; the T019/T029 cross-references to the reset cases repaired; spec Status set to *Draft - Gate 1 closed* per `brief.md`:231; the false "a repeated relation in one TRUNCATE list is a server error" claim dropped from `spec.md`, `plan.md` §3 and two tasks, with de-duplication kept because Q1 rules it; `plan.md` §7's `T0xx` placeholder resolved and its "two RabbitMQ-owning collections" corrected to one; `plan.md` §9's garbled cached-truncate citation reduced to `brief.md` Approach 1 step 3; and every empty disposal changed to the expression-bodied `ValueTask.CompletedTask` form, which leaves no empty block and therefore needs no justification comment.

## Verification performed before drafting

Every line citation in `spec.md` and `plan.md` was checked against this worktree, not copied from recon. Four citations were corrected as a result: `PersistenceRoundTripTests`' class declaration is at line 15 (not 17); the two `EfMovieCatalog*Tests` `GetConnectionString` read sites and the six context-based ones were confirmed individually; `MovieCatalogSeed.ResetAsync` confirmed at 24-31; `AssemblyInfo.cs` confirmed to hold nothing but two usings and the two fixture attributes, so T006's delete-if-empty condition is satisfied at the baseline. The solution's project list was confirmed — eight projects, `tests/LamuFlix.Test` not among them (Q5's premise holds), and `tests/LamuFlix.ArchitectureTests` carries no rule that this change could trip.

**Gate 1 status**: closed. Nothing in this receipt or in the drafts opens it. No owner checkbox is proposed, no ticket change is requested, and no constitution departure is claimed.

**Next**: plan challenge round 1 fixes applied (below); awaiting Keel's freeze check and re-run of `/speckit-analyze`. Nothing in the draft is blocked, and no finding opened Gate 1.

## Plan challenge round 1, fix list F1-F14 applied

`findings-DEV-306-Sentry` (0 C / 3 H / 4 M / 5 L, security PASS), `findings-DEV-306-Ledger` (0 C / 0 H / 4 M / 6 L) and `findings-DEV-306-Compass` (0 C / 1 H / 2 M / 1 L) — 26 findings, adjudicated in `brief.md` §8. All fourteen **Fix** items are applied; the **Accept-risk** and **Reject** verdicts need no artifact change and are not restated here. `brief.md` was not edited.

| Fix | Where it landed |
|---|---|
| **F1** | `spec.md` Assumptions gains **R8**: the reset is the collection's only isolation barrier, a member without it leaks state, and no fixture, definition or gate catches that. Cross-linked from the migration-test edge case, which is now named as the one deliberate exception. Edge-case count 15 → 16. |
| **F2** | `spec.md` FR-009 gains the bounded lock wait; `plan.md` §3 gains `SET LOCAL lock_timeout = '10s'` in the truncate's own transaction, `55P03` surfaced unwrapped, and the note that no test forces the lock; T020 and plan §9 carry it; T024's R3 note now reads "a `55P03` lock-timeout failure" instead of a hang. |
| **F3** | `plan.md` §2 states **no fallback of any kind** — no `?.`, no `?? CancellationToken.None`, no guard — with T024 and T029 as the verify-or-stop runs; T012, T025 and the tasks Notes checkpoint say the same. |
| **F4** | T037 carries the digest-first diagnosis line for the floating tags. |
| **F5** | Ordering note 4 in `tasks.md` states the one-re-run rule and the D1/R4 stop-candidate record; T010's stop condition and T024, T029, T034, T037 all reference it. |
| **F6** | `plan.md` §2 and §4 (the fixture-design sections — §1 is the collections section and has no disposal) plus T012 and T025 null the container field on the normal dispose path. |
| **F7** | `spec.md` US6 *Independent Test* amended to inspection for scenarios 2-3 and running for scenario 4; T035 narrowed to the startup-failure inspection and **T036** added with the four runnable disposal cases, two per fixture-behaviour file; `plan.md` §Test Strategy lists them. Phase 8 renumbered to T037-T043. |
| **F8** | T043 redacts connection strings, usernames, passwords and broker/database URIs before pasting a receipt. |
| **F9** | The identity-only `[Fact]`s are gone. The emission rides the startup case in each fixture-behaviour file plus **one existing test** each in `EfMovieRepositoryTests` and `RabbitMqPublisherTests`, as a single Arrange line with no assertion change (T018, T022, T028, T031). `SHOW server_version` and `ServerProperties["version"]` are written by the two startup cases with **no assertion on either value** (T018, T028; plan Test Strategy evidence 1 and 3). T037's count explanation is now the fixture-behaviour cases including the four disposal cases, and no identity-only cases. |
| **F10** | `plan.md` §3 step 6 and §9 name `ISqlGenerationHelper.DelimitIdentifier(name, schema)` via `context.GetService<ISqlGenerationHelper>()`, and T020 says hand-built quoting fails review. |
| **F11** | T013 requires `OpenAsync(ct)` and `ExecuteNonQueryAsync(ct)` and forbids calling the synchronous `CreateDatabase` helper; `plan.md` §2 and §9 carry the same. |
| **F12** | T020 runs the Roslyn gate on `PostgresFixture.cs` at that task, executes the cached non-interpolated string through `ExecuteSqlRawAsync`, and carries the pre-authorised single-site `#pragma warning disable` / `restore` with a justification citing Q1 and F10 — plus the explicit bar on reworking the quoting. `plan.md`'s constitution check and §9 record the same pre-authorisation and its limit. |
| **F13** | T026 states the token parameter has **no default value** (and `plan.md` §4 repeats it); T042 runs `./scripts/run-vulnerable-packages.ps1` and records its exit code verbatim, with `plan.md` §Gates changed from "out of scope" to "runs, and its exit code is recorded". |
| **F14** | T010 and T037 both check the parallelism precondition — no `xunit.runner.json` under `tests/`, and `rg -n "CollectionBehavior" tests/LamuFlix.IntegrationTests` returns nothing; T040 takes the `ArchitectureTests` green run from T041's unfiltered `dotnet test` rather than starting a second run. |

Two places carry the substance of a fix where the named target could not hold it, and both are recorded rather than silently swapped:

- **F6 names `plan.md` Design §1/§2.** §1 is the collection-definition section and has no disposal path; the broker fixture's disposal is described in §4. The rule was applied to §2 and §4, which are the two sections that actually describe a fixture's lifetime.
- **F7 adds a task**, so every task from the old T036 onward shifts by one: the full run is T037, the teardown check T038, sharing T039, boundaries T040, the final gate pass T041, the gate-record T042, and the receipt hand-off T043. 42 tasks → 43.
