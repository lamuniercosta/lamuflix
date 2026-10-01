# DEV-306 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-01
**Author**: Quill (Writer). Drafted in `F:\Dev\LamuFlix.worktrees\feature-306-spec`, branch `feature/306-spec`, from `4dd5e73` (brief). `git rev-parse --show-toplevel` and `git branch --show-current` confirmed the worktree before any write; the main checkout was not touched. The only pre-existing working-tree change, ` M .specify/feature.json` (the `specs/DEV-306` pin, set in Phase 1), was preserved and not staged. `specs/DEV-306/brief.md` was modified in the worktree by Keel during this drafting pass, to add §Drafting decisions D1; that edit is Keel's and is not staged or altered here.
**Method**: `/speckit-specify` executed against `.specify/templates/spec-template.md`; `setup-plan.ps1 -Json` and `setup-tasks.ps1 -Json` run for path resolution (plan and tasks drafted by hand from their templates in the same pass, precedent DEV-303 and DEV-304). `$env:PYTHONUTF8='1'` set before every invocation. `.specify/extensions.yml` does not exist, so no extension hooks ran in either direction. `AVAILABLE_DOCS` came back empty and none were created, for the reason stated in `plan.md`'s **Note on the Spec Kit phases**.

**Inputs**: `specs/DEV-306/brief.md` (223 lines), `CONCLUSIONS.md` (Q1-Q10), `ASSUMPTIONS.md`, note `recon-DEV-306` (88 lines, baseline `3b2e998`), `specs/PRODUCT.md`, `.specify/memory/constitution.md`, `harness.yml`, and every source file the brief cites — re-read at this HEAD, not taken from recon: both fixtures, `LamuFlixDbContextFactory.cs`, `ContainerFixture.cs`, `AssemblyInfo.cs`, `MovieCatalogCollection.cs`, `MovieCatalogSeed.cs`, `MigrationTests.cs`, `RabbitMqTopologyTests.cs`, `RabbitMqProbe.cs`, all fourteen consumer class declarations, `RabbitMqTopology.cs`, `RabbitMqConnectionOwner.cs`, `RabbitMqOptions.cs`, `EnrichmentOptions.cs`, both `.csproj` files, `Directory.Packages.props`, and the solution's project list.

## Artifacts written

- `specs/DEV-306/spec.md` — 7 user stories (US1 collection-scoped sharing, US2 a ready database, US3 a genuinely empty reset, US4 a broker whose topology exists, US5 broker isolation, US6 failure-safe lifetime and verified teardown, US7 evidence and boundaries), 43 acceptance scenarios, 15 edge cases, FR-001 to FR-030, 6 key entities, SC-001 to SC-010.
- `specs/DEV-306/plan.md` — technical context, constitution check (PASS, no departures), project structure of nineteen changed files with three deletions, Design §1-§7 plus the ruled §8 and the branch table §9, test strategy with the two-green-steps rule and the four evidence captures, and the gate set including the two deliberately-not-green gate lines.
- `specs/DEV-306/tasks.md` — 43 tasks in 8 phases (Setup, Foundational, US2, US3, US4, US5, US6, US7), each with an exact file path, plus dependencies, parallel opportunities and incremental strategy. The brief's task ordering is followed verbatim.
- `specs/DEV-306/checklists/requirements.md` — 17 quality items, all resolved, with six items recorded for spec review.
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
| `tasks.md` T002, T004, T005, T011, T038, Dependencies, Parallel Opportunities, Notes | T002 records the ruling instead of blocking; T004/T005 are `[P]` again and state the attribute is absent and must not be added back; T011 gains the stop condition at the first run that can trigger it; T038 records the timing as concurrent |

Nothing else in the draft was waiting on the answer.

## Everything else was a branch the brief or the rulings already delegated

| Choice | Branch taken | Why it needed no ruling |
|---|---|---|
| Connection-cancellation shape (`brief.md` Design §2 offers two) | additive `CreateConnectionAsync(CancellationToken)` overload; existing no-token member preserved | `RabbitMqProbe.cs:201` is the no-token member's only caller, so preserving it leaves that file unedited; `RabbitMqConnectionOwner.cs:37` already uses the token-accepting overload. Q9 satisfied either way (`plan.md` §9) |
| Collection name strings (rulings fix the **type** names only) | one `internal static` holder in the test assembly for the two `[Collection(...)]` strings | A name that must agree across fourteen files has one source. No new project, layer or abstraction; the ruled names are unchanged |
| Truncate cached vs rebuilt per reset | built once at startup, cached | The brief's Design §1 step 3 says to cache it; rebuilding re-walks the model per test for nothing |
| Container teardown call shape | the async disposal overload, called with **no** token | Q9 requires cleanup after cancellation; the existing code already calls it that way at `PostgresFixture.cs:27` |
| Identity restart | `RESTART IDENTITY` on the one truncate | US3 scenario 3 requires it; it is the only server feature that gives it and it costs nothing over a plain truncate |
| The stale fixture comment at `RabbitMqFixture.cs:12` | delete, do not rewrite | Q3 makes the sentence false and `AGENTS.md` forbids explanatory comments, so there is nothing to rewrite it into (`plan.md` §7) |
| Empty-database creation shape | no `TEMPLATE` clause, matching the existing private helper | R5 is satisfied by the clause's absence, and `LamuFlixDbContextFactory.cs:44` already has the right shape |

## Recorded, not escalated — six items from `checklists/requirements.md`

1. **Sharing narrows from assembly-wide to per-collection.** Agreed (Q3 and the ticket's own words), and every current consumer is placed in one of the two collections so none loses access. Recorded because it is the change most likely to surprise a reader of the diff who assumes the wiring is a pure refactor.
2. **`MigrationTests` is a collection member that deliberately does not reset.** Q2 gives it the empty-database path because it proves the opposite property. Recorded so the omission reads as a ruling, not a miss.
3. **`LamuFlixDbContextFactory.CreateContext(PostgresFixture)` becomes unreferenced and must be left alone.** It is called only from `PostgresFixture.CreateContext()` (`LamuFlixDbContextFactory.cs:10-14`, `PostgresFixture.cs:31`), and the brief removes the caller. AC8 freezes the helper's public static members. It is public in a non-packable, non-test project, so the uncalled-private-code analyzer does not reach it; an inspection finding there is a finding about AC8, not a defect to delete around.
4. **`Options.Create` resolves transitively — verified, not assumed.** `Microsoft.Extensions.Options` is a `PackageReference` in `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`, which `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj:20` references, so the broker fixture's new startup path needs no package and no `.csproj` edit. Recorded because the *obvious* fix for a compile failure here would be a `PackageReference` edit, which AC8 and §2.3 item 1 both forbid; the correct response is a stop.
5. **`TestContext.Current` inside a collection fixture's `InitializeAsync`.** Expected to be available; the fallback Q9's own wording permits ("where the API supports it") is a live token for initialisation, with cleanup untied from the token either way. Behaviour is identical under both readings.
6. **Two gate lines that are deliberately not green-and-passing.** The property gate is expected to exit 2 (scope-empty) and DEV-306's opt-out is recorded per `harness.yml:30-35`; the mutation gate is not applicable because no `src/` code changes; the vulnerable-packages gate is out of scope because no dependency is added or removed. Carried as T042 so none is discovered at the gate.

## Verification performed before drafting

Every line citation in `spec.md` and `plan.md` was checked against this worktree, not copied from recon. Four citations were corrected as a result: `PersistenceRoundTripTests`' class declaration is at line 15 (not 17); the two `EfMovieCatalog*Tests` `GetConnectionString` read sites and the six context-based ones were confirmed individually; `MovieCatalogSeed.ResetAsync` confirmed at 24-31; `AssemblyInfo.cs` confirmed to hold nothing but two usings and the two fixture attributes, so T006's delete-if-empty condition is satisfied at the baseline. The solution's project list was confirmed — eight projects, `tests/LamuFlix.Test` not among them (Q5's premise holds), and `tests/LamuFlix.ArchitectureTests` carries no rule that this change could trip.

**Gate 1 status**: closed. Nothing in this receipt or in the drafts opens it. No owner checkbox is proposed, no ticket change is requested, and no constitution departure is claimed.

**Next**: spec review round 1 of 2 (per the brief's Round cap), in progress. The one `needs decision:` is ruled (D1) and nothing in the draft is blocked.
