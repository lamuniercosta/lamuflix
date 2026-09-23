# Feature Specification: Close DEV-289 leftovers

**Feature Branch**: `feature/360-dev-360-spec`  
**Created**: 2026-09-23  
**Status**: Gate 1 passed (spec PR merged, `a03da44`), then reopened on 2026-09-23 for the owner-option-A amendment of AC4/SC-003 (Gate 1 "T014 block"). Owner decisions D1–D3 were answered 2026-09-23. The re-freeze needs a clean analysis newer than every edit, after Rigger's DEV-360 AC4 read-back  
**Input**: DEV-360 Scope & Technical Design (`task-DEV-360:14-29`), acceptance criteria (`task-DEV-360:31-40`), owner decisions (`task-DEV-360:81-84`), Patron rulings (`task-DEV-360:89-90`)

## User Scenarios & Testing

### User Story 1 — Explicit configuration failures (Priority: P1)

As a developer, I get an explicit missing-variable outcome rather than a fallback local database connection. **Independent proof:** [quickstart.md](quickstart.md) §2 (context, via the pinned design-time `dotnet-ef` after the §1 tooling gate) and §3 (test setup). The three existing `AssistirFilme_*` tests still execute.

### User Story 2 — Observable missing deletes (Priority: P1)

As a repository caller, a delete for an unknown id reports failure. **Independent check:** `Delete(object id)` with an unknown id throws `KeyNotFoundException` identifying the entity type and id ([quickstart.md](quickstart.md) §4). The required test for this path is deferred to DEV-280 by owner decision D2.

### User Story 3 — Existing model and gates agree (Priority: P2)

As a maintainer, CLR nullability reflects the current EF snapshot, warning settings agree, and DEV-289 Standards findings are handled within the ticket budget. **Independent test:** [quickstart.md](quickstart.md) §5 introduces no pending operation beyond the pre-existing `Movie.Status` AddColumn (the six `Id` IdentityColumn `AlterColumn` operations are recorded baseline residuals) and no migration is committed; §6 settings agree; Ledger's dispositions carry follow-up ids.

### Edge Cases

- An already configured context does not need `LAMUFLIX_CONNECTION` and keeps its supplied options (`LamuFlixContext.OnConfiguring` already checks `IsConfigured`).
- The test connection guard runs only on the DB-dependent path; a classwide `[TestInitialize]` inconclusive would skip the three playback tests that need no MySQL server (Patron Q1, `CONCLUSIONS.md`).
- `Movie.Collection` is nullable with its nullable `CollectionId`; required join navigations remain non-nullable (Patron Q2).
- `LamuFlix.Data/Models/Temp.cs` is not mapped: the context has no `DbSet<Temp>` and the snapshot has no `Temp` entity. FR-004 is snapshot-driven, so `Temp.cs` has no nullability target and is not edited. Untouched, its Portuguese `FilmeId` is not "contact" under Principle VIII; DEV-290 is ticket-authorized to delete the file (`task-DEV-360:24`).
- `LamuFlix.Data/Models/MovieEnrichmentMessage.cs` is also unmapped (absent from the snapshot) and outside the DEV-289 file set. It is not edited. Its pre-existing InspectCode warning (`:1` `RedundantNullableDirective`) is tracked by DEV-366 (Patron G1).
- If DEV-280 or DEV-290 lands before Phase B pickup, skip only work already completed there (for example `Player.cs` nullability or the `UnitTest1.cs` half of item 1) and record the overlap comment through Patron and Rigger. Current chain order makes neither skip applicable yet (`task-DEV-360:18,24,38`; `chain:7-11`).

## Requirements

### Functional Requirements

- **FR-001**: Remove the two hardcoded connection-string fallbacks from `LamuFlix.Data/LamuFlixContext.cs` and `LamuFlix.Test/UnitTest1.cs`, leaving no connection string, user id, or password literal in those two projects.
- **FR-002**: Without options or `LAMUFLIX_CONNECTION`, the context throws `InvalidOperationException` naming the variable ([quickstart.md](quickstart.md) §2). Without `LAMUFLIX_TEST_CONNECTION`, a DB-dependent legacy test is inconclusive naming the variable, and the guard fires on first context access before any file-system access ([quickstart.md](quickstart.md) §3). The three `AssistirFilme_*` tests execute. Host user-secrets wiring already exists and is outside this ticket (`task-DEV-360:15-18`; Patron Q1).
- **FR-003**: `GenericRepository.Delete(object id)` throws `KeyNotFoundException` naming entity type and id when `Find(id)` returns null (`task-DEV-360:19`). `KeyNotFoundException` is retained as ticket-mandated transitional behavior; no `NotFoundException` type is added (Patron ruling, owner-accepted, `task-DEV-360:83`). The not-found test is deferred to DEV-280 (owner decision D2).
- **FR-004**: Annotate every property of an entity mapped in the current snapshot whose column is nullable as a nullable CLR type. Keep `= null!` for non-null columns, required reference navigations, and `DbSet<>`; reference navigation nullability follows FK optionality. Unmapped `Temp.cs` and `MovieEnrichmentMessage.cs` are out of scope. Add no migration (`task-DEV-360:20-24,35`; Patron Q2). Consumer code that FR-004 plus `TreatWarningsAsErrors` breaks gets only behavior-preserving nullability fixes in `LamuFlix.Web/Services/FilmesServices.cs` and `LamuFlix.Test/UnitTest1.cs` (both in the DEV-289 set). Those fixes preserve returned values and public shape and use minimal `!` or guard edits; they do not touch the `ProcessStarter` member (Patron H2 ruling, `task-DEV-360:89`).
- **FR-005**: Set `harness.yml` `warningsAsErrors` to true to agree with `Directory.Build.props` (`task-DEV-360:25,36`).
- **FR-006**: In Phase B, Ledger reviews the files named by `specs/DEV-289/tasks.md` as they are on `main`, using Standards brief, smell baseline, and vendor rules. Fix localized annotation/analyzer findings within that file set and without a new type or behavior. Route wider findings to Patron for a follow-up ticket, record ids on the PR, and do not schedule follow-ups into this chain. In-scope Critical/High findings with a concrete failure scenario must be fixed before verification closes (`task-DEV-360:26-28,37`; Patron Q3, Q4).
- **FR-007**: Leave the settable `ProcessStarter` seam unchanged (owner decision D1; `task-DEV-360:29,39`).
- **FR-008**: Run the pipeline gates and report numeric exits; a skipped or unrunnable gate is not a pass (`task-DEV-360:40`; `task-pipeline:65-75`).
- **FR-009**: Add `Microsoft.EntityFrameworkCore.Design` 9.0.x as a design-time-only reference of `LamuFlix.Data` (`PrivateAssets=all`, `Publish=true`, and the `dotnet add` default `IncludeAssets` from Microsoft's documented workaround) and pin `dotnet-ef` 9.0.x in the local tool manifest, both at the `Microsoft.EntityFrameworkCore` patch; the [quickstart.md](quickstart.md) §1 gate passes (owner decision D3).

## Success Criteria

- **SC-001**: [quickstart.md](quickstart.md) §2 records `InvalidOperationException` naming `LAMUFLIX_CONNECTION`; §3 records MSTest Inconclusive naming `LAMUFLIX_TEST_CONNECTION`; §4 confirms the Delete exception and the PR records the DEV-280 test deferral.
- **SC-002**: The three live `AssistirFilme_*` tests execute and pass when `LAMUFLIX_TEST_CONNECTION` is unset.
- **SC-003**: After DEV-360, [quickstart.md](quickstart.md) §5 shows no pending model operation introduced beyond the pre-existing `AddColumn` for `Movie.Status`. The only other operations allowed are the six pre-existing `AlterColumn` operations on `Id` that add `MySql:ValueGenerationStrategy` IdentityColumn to actor, collection, director, genre, movie, and player. They are pre-existing baseline residuals of the EF 2.1.3-era snapshot (T007 baseline, `research.md`), not DEV-360 drift. Any other pending operation fails SC-003. No migration is committed and the model snapshot is not edited. Refreshing the snapshot is deferred to DEV-19's fresh Initial migration (Patron H1 ruling, `task-DEV-360:89`; AC4 as amended by owner option A, 2026-09-23).
- **SC-004**: Both warning settings read true. No in-scope Critical/High Ledger finding stays open, and every other finding is fixed or linked to a follow-up ticket id.
- **SC-005**: No hardcoded database connection string, user id, or password literal remains in Data or Test; gitleaks is green.
- **SC-006**: Every [quickstart.md](quickstart.md) §1 step exits 0 with matching 9.0.x versions, and the Design package is absent from the `LamuFlix.Web` and `LamuFlix.Work` package graphs and build outputs (§1.5–§1.6).

## Gate 1 and authorization envelope

Owner answers of 2026-09-23 (`task-DEV-360:81-84`; `chain:7`):

- [x] **D1 — Owner:** blocked: structural — keep the settable `ProcessStarter` seam over `Process.Start`? **Answered: keep it unchanged.** This covers the `ProcessStarter` member (`FilmesServices.cs:57`) only, not the whole file (`task-DEV-360:29,39`; charter §2.3 item 5).
- [x] **D2 — Owner:** blocked: structural — where does the required `Delete(object id)` not-found test run? **Answered: defer it to DEV-280.** DEV-360 adds no test project, no test package, no MSTest test, and no EF Core InMemory workaround. FR-003 ships without that test; the earlier `LamuFlix.IntegrationTests` option is not taken.
- [x] **D3 — Owner:** blocked: structural — add a package so the EF design-time checks can run (§2.3 item 1)? **Answered: add `Microsoft.EntityFrameworkCore.Design` 9.0.x, design-time only, with `PrivateAssets=all` and `Publish=true`, and pin `dotnet-ef` to 9.0.x.** `Publish=true` is Microsoft's documented EF 9 workaround for tools on .NET SDK ≥ 9.0.200 (EF Core 9 breaking changes, "Microsoft.EntityFrameworkCore.Design not found when using EF tools"); its side effect is copying the Design assembly to `LamuFlix.Data`'s output.
- Patron ruling, owner-accepted: retain `KeyNotFoundException` under the transitional-layout clause; no new `NotFoundException` type (`task-DEV-360:83`). Patron's C1/C2 ruling is the authority for both that exception and the retained direct `LAMUFLIX_CONNECTION` read: they are scoped, ticket-decided transitional behavior in the flat projects that constitution:426-429 describes. No amendment, ADR, or owner choice (`task-DEV-360:89`).
- Patron's M2 ruling: rename-on-contact (constitution:219-221) applies only to identifiers this ticket edits or signatures it changes, never to every identifier in a touched file. Portuguese-vocabulary cleanup is a DEV-290/Epic 1 follow-up.
- Rigger recorded the D2 deferral on DEV-280 and DEV-360, the H1 AC4 correction, and a DEV-360 comment recording D1–D3 and the AC3 deferral (Patron's Compass-F2 ruling: comment-only record suffices), each with verified read-back (`task-DEV-360:90-91,93`).
- [x] **T014 block — Owner:** blocked: structural — the T014 probe still shows six `Id` IdentityColumn `AlterColumn` operations beside `AddColumn Status`, and removing them needs a migration or snapshot edit (§2.3 item 3). **Answered 2026-09-23: option A.** Amend AC4/SC-003 to record them as pre-existing baseline residuals. DEV-360 must introduce no pending operation beyond `AddColumn movie.Status`, and it adds no migration or snapshot edit. DEV-19's fresh Initial migration owns the snapshot refresh. Patron authorized reopening the frozen spec early for this amendment only.
- Patron's Q1 ticket clarification is recorded on DEV-360 and verified by Rigger's read-back (`chain:7`).
- Nothing else is authorized: no other dependency, project, layer, schema or migration, public API, or out-of-ticket file rewrite. Any additional planned work goes to the owner.
