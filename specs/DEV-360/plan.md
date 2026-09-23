# Implementation Plan: Close DEV-289 leftovers

**Branch:** `feature/360-dev-360-spec` | **Date:** 2026-09-23 | **Spec:** [spec.md](spec.md)

## Summary

Phase B removes the two database connection fallbacks, makes unknown-id Delete throw, aligns model nullability with the snapshot, and fixes warnings-as-errors drift. Ledger reviews the DEV-289 file set within Patron's budget. Owner decisions D1–D3 (spec.md) are answered: `ProcessStarter` is unchanged, the Delete test is deferred to DEV-280, and the EF design-time tooling is added and pinned so the ticket's EF checks can run. This Phase A artifact authorizes no production edit.

## Technical Context

**Language/Version:** C# / .NET 10; EF Core 9.0.0 with Pomelo 9.0.0 (existing); PowerShell harness configuration.  
**Primary Dependencies:** Existing EF Core and Pomelo/MySQL. Added under owner decision D3 only: `Microsoft.EntityFrameworkCore.Design` 9.0.x (design-time, `PrivateAssets=all`, `Publish=true`) in `LamuFlix.Data`, and the `dotnet-ef` 9.0.x local tool. Both use the `Microsoft.EntityFrameworkCore` patch (currently 9.0.0).  
**Storage:** Existing MySQL model and current snapshot; no migration.  
**Testing:** [quickstart.md](quickstart.md) is the canonical procedure. §1 is the tooling gate, §2–§3 are the missing-variable observations, §5 is the pending-model check, plus the three existing playback tests, gitleaks, and pipeline gates. No new test, test project, or test package (D2).  
**Project Type:** Existing flat solution; DEV-290's layout move is later in the chain. No project is added.  
**Constraints:** No credential literal, new API, or schema. No committed migration (Patron H1). No edit to the `ProcessStarter` member (D1).

## Constitution Check

| Principle | Application | Status |
|---|---|---|
| II — Explicit Handlers and Decorators (domain failures, :96-99) | Authority: Patron's C1/C2 ruling (`task-DEV-360:89`), accepted by the owner, not a permitting reading of constitution:426-429 (:427 still requires II–IX for new code). The ruling scopes this as ticket-decided transitional behavior in the flat projects that :426-429 describes. The ticket requires `GenericRepository.Delete` in the flat transitional `LamuFlix.Data` project to throw `KeyNotFoundException` (`task-DEV-360:19,34`). The Principle II failure types and their single `LamuFlix.Api` mapping (:96-99) belong to the Epic 1 layout, which does not exist yet; no `NotFoundException` type is added (a new type is §2.3). Scope: this one throw site, not extended. Retired when DEV-290/Epic 1 introduces the Core failure types. Owner accepted the ruling (`task-DEV-360:83`). | Pass — by Patron C1/C2 ruling |
| VII — Configuration Isolation | Removing both hardcoded fallbacks satisfies :198-199 and retires two instances of the hard-coded-secrets debt (:430-431). The context keeps its pre-existing direct `LAMUFLIX_CONNECTION` read in the transitional `LamuFlix.Data` project, because the ticket names that variable in the required exception (`task-DEV-360:15-16`). Authority: Patron's C1/C2 ruling (`task-DEV-360:89`), not a :426-429 exemption. The ruling scopes it as ticket-decided transitional behavior in the flat project. Options-record binding (:201-204) arrives with the Epic 1 host layout. The same applies to the test's pre-existing direct `LAMUFLIX_TEST_CONNECTION` read (`UnitTest1.cs:37`), which the ticket names (`task-DEV-360:15-17`). No new direct read is added. The Known Technical Debt list (:430-441) does not name this read, and this plan does not claim it does. | Pass — by Patron C1/C2 ruling |
| VIII — Ubiquitous Language in English | Per Patron's M2 ruling (`task-DEV-360:89`), rename-on-contact (:219-221) applies only to identifiers this ticket edits or signatures it changes, never to every identifier in a touched file. T009 edits only the DB setup/guard path. The `[Ignore]` lift touches the English `TestMethod1` and is reverted. T013a's consumer fixes use `!`/guards without renaming or changing signatures, so untouched Portuguese identifiers in `FilmesServices.cs`/`UnitTest1.cs` stay as they are. Unmapped `Temp.cs` and `MovieEnrichmentMessage.cs` are not touched. If any edit must change a Portuguese identifier or signature, rename it on contact, or escalate to Patron if that goes beyond the ticket. The file-wide Portuguese-vocabulary cleanup is a DEV-290/Epic 1 follow-up. | Pass in plan |
| V — Validated Inputs, Consistent Error Responses (:156-168) | FR-003 adds a throw path. No in-repo caller reaches `Delete(object id)`, so no endpoint exposes it; the legacy `FilmesController` `BadRequest(ex.Message)` sites (:79,127,141,155) are unchanged. T012 re-confirms at pickup. | Pass — not reachable |
| IX — Test Pyramid with Real Infrastructure | No test is added. The required Delete persistence test is deferred to DEV-280 (D2), which avoids a new MSTest test (:247, :429) and EF Core InMemory (:254-256). The three existing InMemory playback tests are legacy evidence the ticket requires and are not extended. T009's `Assert.Inconclusive` guard is setup code inside the existing MSTest project, not a new MSTest test (:429). | Pass (deferral owner-decided) |
| I, III, IV, VI | Not applicable: no port, filter, state-machine, or telemetry change. | N/A |
| Technology stack / Forbidden list | `Microsoft.EntityFrameworkCore.Design` is EF Core tooling, not a forbidden item (:294-297). It does not add Pomelo/MySQL use; it only enables the ticket's design-time checks against the existing provider. It is authorized under §2.3 item 1 by owner decision D3. `PrivateAssets=all` keeps it out of the hosts' dependency graphs (quickstart §1.5). | Pass; owner-authorized |
| Static-Analysis Gates | Match the warning settings and run the actual gates with pwsh 7 and numeric exits. The vulnerable-package gate covers the new package. Patron's Phase A baseline over the 16-file gate-baseline set on `main` (listed in `research.md`): Roslyn 0; complexity 0 at 15 and at 6; InspectCode 1, a single pre-existing `RedundantNullableDirective` in the unmapped `MovieEnrichmentMessage.cs:1`, outside the file impact boundary and tracked by DEV-366. T005 re-measures, and T018 applies the G1 ladder; CA1502 is never suppressed. Refactor gate (Patron G2; constitution:338): T018 runs `run-cyclomatic-complexity.ps1 -Threshold 6` over the whole changed `.cs` files, alongside the threshold-15 run, and records both numeric exits. A DEV-360-created method over 6 is fixed by extracting helpers, early returns, or guard clauses. A pre-existing one is recorded as a PR baseline exception with its numeric exit (DEV-361 precedent, `chain:8`), with no refactor, suppression, or threshold change. The baseline has none for the 16-file set. T017 baselines any other DEV-289-set file before editing it. | Pass in plan |
| Known Technical Debt — transitional layout (:426-429) | The ticket's edits stay in the flat `LamuFlix.Data`, `LamuFlix.Test`, and `LamuFlix.Web` projects under this clause. The II and VII rows above rest on Patron's C1/C2 ruling, and neither behavior is extended. No new reflection query, `DateTime.Now`, Portuguese identifier, or MSTest test. | Pass — scoped, not extended |
| Charter §2.3 | D1–D3 answered by the owner. The T014 block was answered by the owner with option A (2026-09-23): AC4/SC-003 record the six `Id` IdentityColumn `AlterColumn` operations as baseline residuals, and DEV-19 owns the snapshot refresh. Patron rulings C1/C2, H1, H2, and M2 need no owner choice. No migration, API change, other dependency, or unrelated file write. | Answered |

## Phase 0: Research

The live worktree confirms both fallbacks, the silent Delete, model/snapshot drift, `harness.yml` false versus `Directory.Build.props` true, the unmapped `Temp`, existing host user-secrets wiring, and the observed EF tooling failures (U1 NETSDK1004; U2 missing Design package). [research.md](research.md) records the evidence and Patron rulings. At pickup, rerun the drift check against `main` and apply the ticket's overlap conditions.

## Phase 1: Design

No new data model or contract: align CLR annotations to the existing snapshot with no schema or public API change. The pre-existing unmigrated `Movie.Status` column stays pending (Patron H1). [quickstart.md](quickstart.md) defines verification. Existing paths stay in their flat projects. `specs/DEV-289/tasks.md` defines Ledger's review file set.

## Phase A: analysis, challenge, and Gate 1

1. Run `/speckit-analyze` read-only and persist `analyze.md` with its SHA256 (Critical 0, High 0). Sentry, Ledger, and Compass then challenge the plan, and Keel adjudicates. After adjudication edits, rerun and persist the analysis; freeze only on a clean report newer than every edit. There are at most two rounds, and an unresolved Round 2 stops to Patron. Findings, the summary, and fix replies go on the spec PR and threads are resolved (T001–T001b).
2. Owner answers D1–D3 are recorded, and Rigger has verified the D2 deferral comments on DEV-280 and DEV-360 (T004). Rigger opens this ticket's spec PR; the user merges it at Gate 1.

## Phase B: implementation sequence (tasks.md B1–B6)

1. **B1 Pickup:** reconcile against `main`, especially DEV-280/290 overlap.
2. **B2 Tooling:** add the D3 package and tool pin; pass quickstart §1.
3. **B3 US1:** make both missing-connection outcomes explicit; record quickstart §2–§3.
4. **B4 US2:** make unknown-id Delete throw; record quickstart §4 and the DEV-280 deferral.
5. **B5 US3:** align mapped-model nullability; apply behavior-preserving consumer nullability fixes (T013a); confirm quickstart §5 shows no pending operation beyond the `Status` AddColumn, apart from the six `Id` IdentityColumn baseline residuals (owner option A); set `harness.yml`; Ledger reviews under Q3; in-scope Critical/High findings close before B6 (Q4).
6. **B6 Verification:** run pipeline gates with numeric exits; check the diff boundary; hand off to review.

## File impact boundary

`LamuFlix.Data/LamuFlixContext.cs`, `LamuFlix.Test/UnitTest1.cs`, `LamuFlix.Data/Repositories/GenericRepository.cs`, and mapped-entity model files under `LamuFlix.Data/Models` (not the unmapped `Temp.cs` or `MovieEnrichmentMessage.cs`). `LamuFlix.Web/Services/FilmesServices.cs` gets T013a's behavior-preserving nullability fixes plus any T017 Q3-local fixes (FR-006). Each is recorded by `file:line` in the PR "Verification evidence", and its `ProcessStarter` member (:57) stays unchanged (D1). Also `harness.yml`, and under D3: `Directory.Packages.props` (one `PackageVersion`), `LamuFlix.Data/LamuFlix.Data.csproj` (one `PackageReference`), and `.config/dotnet-tools.json` (one `dotnet-ef` entry). Localized Standards edits stay within the `specs/DEV-289/tasks.md` file set. No committed edit to `LamuFlix.Data/Migrations` (the §5 probe is discarded), and no test project or package. Phase B may also change `specs/DEV-360/research.md` and `specs/DEV-360/tasks.md`, but only for evidence records: T005 additions and checkbox state. One further allowance: the owner-option-A amendment (spec.md Gate 1 "T014 block"): commit `0341cf9` and its consistency follow-up commit on this branch, whose subject names option A. It may change only `specs/DEV-360/spec.md`, `plan.md`, `quickstart.md`, `research.md`, `tasks.md`, `checklists/requirements.md`, and the re-persisted `analyze.md`. It adds no migration, snapshot edit, or production file. A newly discovered out-of-set file need is a §2.3 question, not implied authorization.

## Complexity Tracking

No new architecture, project, or layer. The II and VII rows rest on Patron's C1/C2 ruling, which scopes ticket-decided transitional behavior in the flat projects (constitution:426-429 context).

Citations of the form `task-DEV-360:…`, `chain:…`, `task-pipeline:…`, and charter §2.3 refer to the team's shared notes and charter outside the repo. Rigger quotes the relevant lines at pickup if a reviewer needs them.
