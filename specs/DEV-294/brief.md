# DEV-294 Phase A brief

**Status.** The grill finished at 12/12 questions. The scope Quill drafts from is frozen. Owner checkboxes answered YES 2026-09-26 (CONCLUSIONS.md:165-168). Gate 1 opens when the user merges the spec PR. `CONCLUSIONS.md` keeps every question, ruling, citation, and the Conductor steer. The DEV-294 task note ("Phase A grill" section) mirrors the rulings.

## Source and worktree

- **Ticket.** DEV-294, "Implement Movie aggregate root, domain state machine, and sealed record value objects". In Progress, size L, parent DEV-282 (task note lines 3-10). No code dependency on open PRs #30 or #31.
- **Worktree.** `F:\Dev\LamuFlix.worktrees\feature-DEV-294-implement-movie-aggregate-root-domain-state-machine-and-seal`.
  - Branch: `feature/DEV-294-implement-movie-aggregate-root-domain-state-machine-and-seal`.
  - Base: `origin/main`. Recon HEAD is `1c3d580e8ab8f117f0b8fa8f9caa27808e567d49`.
  - Feature pin: `specs/DEV-294`.
- **Ticket *Scope & Technical Design* (recon lines 23-33).** Everything below is decided by the ticket text:
  - `class Movie` with private setters and state-transition methods.
  - `InvalidTransitionException` (HTTP 409).
  - Methods: `MarkEnriched(MovieMetadata, DateTimeOffset now)`, `MarkNotFound(now)`, `MarkFailed(EnrichmentFailureCategory, now)`, `RequestEnrichment()`, `AddToWatchlist()`, `RemoveFromWatchlist()`.
  - Tracking fields: `Status`, `EnrichedAt`, `EnrichmentAttempts`, `LastFailureCategory`, `LastAttemptAt`.
  - `EnrichmentStatus { Pending=0, Enriched=1, NotFound=2, Failed=3 }`.
  - Sealed-record value objects: `MovieId`, `ImdbId`, `ImdbRating`, `Runtime`, `ReleaseYear`, `LibraryPath`, `MediaFormat`.
  - Unit tests with **100% branch coverage on the state machine**.
  - Target directory: `src/LamuFlix.Core/Domain/`.

## Frozen committed scope

Every file lives in `src/LamuFlix.Core/Domain/`, one type per file, flat, in namespace `LamuFlix.Core.Domain` (Q5). `.gitkeep` stays.

1. **`Movie` (Q5).** A `public sealed class` with private setters.
   - Properties: `Id` (MovieId), `Title` (non-blank string), `Path` (LibraryPath), `Format` (MediaFormat), `IsInWatchlist`, and `Metadata` (MovieMetadata?), plus the five ticket tracking fields.
   - The only creation path is `static Movie.Create(MovieId, string title, LibraryPath, MediaFormat)`. It returns `Status = Pending`, `EnrichmentAttempts = 0`, not in the watchlist, with every nullable null.
   - There is no rehydration factory.
2. **Transition table (Q6).**

   | Action | Legal from | Effect | From any other state |
   |---|---|---|---|
   | `MarkEnriched(metadata, now)` | Pending | → Enriched. `EnrichmentAttempts++`, `LastAttemptAt = now`, `EnrichedAt = now`, `Metadata = metadata`, `LastFailureCategory = null` | throws |
   | `MarkNotFound(now)` | Pending | → NotFound. `EnrichmentAttempts++`, `LastAttemptAt = now` | throws |
   | `MarkFailed(category, now)` | Pending | → Failed. `EnrichmentAttempts++`, `LastAttemptAt = now`, `LastFailureCategory = category` | throws |
   | `RequestEnrichment()` | Enriched, NotFound, Failed | → Pending. Attempts (monotonic, never reset), `EnrichedAt`, `Metadata`, and `LastFailureCategory` are all kept | throws from Pending |

   A call that throws `InvalidTransitionException` mutates nothing.
3. **Watchlist (Q7).** The watchlist is independent of `Status`.
   - `AddToWatchlist()` throws when the movie is already in the watchlist.
   - `RemoveFromWatchlist()` throws when it is absent.
   - This deliberately differs from the idempotent `MovieService` methods, which are not wired here. Treat the difference as recorded, not as a defect.
4. **`InvalidTransitionException` (Q8).** Declared as `public sealed class InvalidTransitionException : InvalidOperationException`.
   - It carries the attempted action name and the current state as a string, and has the standard message constructors.
   - ~~The XML doc records the HTTP 409 mapping.~~ **Superseded by D1:** no XML doc; ADR 0014 and follow-up #2 record the 409 mapping. No endpoint or middleware is added.
5. **`EnrichmentStatus` (ticket).** `Pending=0, Enriched=1, NotFound=2, Failed=3`. It is independent of `LamuFlix.Data.Models.MovieEnrichmentStatus` (Q3).
6. **`EnrichmentFailureCategory` (Q8).** `Unknown=0, ProviderUnavailable=1, RateLimited=2, InvalidResponse=3`.
7. **`MovieMetadata` (Q4).** A new `sealed record` in Core with `Title` (non-blank), `Synopsis?`, `ReleaseYear?`, `Runtime?`, `ImdbRating?`, `ImdbId?`. It is a separate type from Worker's `MovieMetadata`, which stays untouched.
8. **Value objects (Q9).** Each is a `sealed record` with an explicit validating constructor and a get-only `Value` property (no `init`, so `with` cannot bypass validation). Invalid input throws the `ArgumentException` family. Each also gets a `TryCreate` factory (D2), and the `with` guarantee is evidenced by reflection (D4).

   | Type | Rule |
   |---|---|
   | `MovieId` | > 0 |
   | `ImdbId` | Non-null and matches `^tt\d{7,8}$` via `[GeneratedRegex]` |
   | `ImdbRating` | 0.0-10.0 inclusive, at most one decimal place, no rounding |
   | `Runtime` | `Minutes > 0` |
   | `LibraryPath` | Non-blank, rejects any literal `..` substring |
   | `MediaFormat` | Non-blank; `Extension` is trimmed, its leading `.` is stripped, and it is lowercased. Order fixed by D3 |
   | `ReleaseYear` | `ReleaseYear(int value, DateTimeOffset now)` accepts `1888 <= value <= now.Year + 5`, with equality on `Value` only. Patron ruled this consistent with the ticket, since `DateTime.Now`/`UtcNow` are banned (recon line 118). |
9. **Test project reference (Q1).** Add exactly one `<ProjectReference>` to `src/LamuFlix.Core/LamuFlix.Core.csproj` in `tests/LamuFlix.Test/LamuFlix.Test.csproj`. This is an edit, not a rewrite. Tests go in `tests/LamuFlix.Test/Domain/`.

**Out of scope (Q2, Q3).**
- Do not edit `src/LamuFlix.Data/Models/Movie.cs`, `MovieEnrichmentStatus.cs`, `LamuFlixContext.cs`, `src/LamuFlix.Worker/Services/EnrichmentJobProcessor.cs`, `src/LamuFlix.Worker/Models/MovieMetadata.cs`, `src/LamuFlix.Web/Services/MovieService.cs`, or any API project file.
- No migration, no schema change, no new project, no new NuGet package except FsCheck (D9).

**This boundary is frozen: anything else is a follow-up issue, not a finding in this round.**

## Owner-only checkboxes (answered YES 2026-09-26)

- [x] **Q10(b), §2.3 #1: add FsCheck.** Should FsCheck be added as a new NuGet dependency in `Directory.Packages.props`, so that property tests cover DEV-294's state machine and value objects?
  - `.cursor/rules/refactor-gate.mdc` and `.agents/skills/refactor/SKILL.md:22` require property tests for pure/domain logic.
  - `harness.yml:31-35` permits the per-ticket opt-out only for "a ticket with no domain invariants", and this ticket has them.
  - **YES** authorizes FsCheck and the property tests. Patron then amends DEV-294 so they become frozen Phase B scope.
  - **NO** means the team re-decides how the refactor property-tests gate is satisfied.
  - `run-property-tests` must exit 0; exit 2 SKIPPED is a failure (D10).
- [x] **§2.3 #6: edit CONTEXT.md.** The ticket does not name the existing file. Patron's Q12 ruling would:
  - extend **Enrichment Status** (`CONTEXT.md:27-29`) and **Watchlist** (`CONTEXT.md:38`);
  - add **Movie Aggregate** and **Enrichment Attempt**.

  **YES** authorizes those glossary edits in this delivery. **NO** means no glossary change. Nobody edits `CONTEXT.md` before the owner answers (Conductor steer).

  **Consequence of NO (D5):** constitution VIII says "A new domain term MUST be added to CONTEXT.md in the same PR", and the PR gate repeats it. A NO answer therefore leaves the delivery PR in conflict with constitution VIII. The box stays unticked; this line only informs the owner's answer.

**Not a checkbox: Q1.** The single ProjectReference is not §2.3 #2, and a new test project is **not** authorized. The architecture plan is not in the repo. If its §3 names a Core test project, the owner can raise that at Gate 1.

## Approach and files for Quill's plan

- **New types.** Pure, dependency-free C# 14 on .NET 10 in `LamuFlix.Core`, using only the BCL (the `[GeneratedRegex]` source generator ships with the BCL).
- **Time.** Always passed in as `DateTimeOffset now`. Never read a clock (BannedSymbols.txt).
- **Complexity.** Keep each transition method small: a guard clause that throws, then assignments. That keeps it under the implement threshold of 15 and the refactor threshold of 6.
- **Architecture tests.** They must stay green: Core references no EF Core, Npgsql, RabbitMQ, or Infrastructure (`ArchitectureTests.cs:24-48`).
- **Names.** `Core.Domain.Movie` and `Data.Models.Movie` share a simple name. Test files that need both use qualified names or aliases, and no production file changes.
- **Planned files** (the exact names are Quill's):
  - Domain: `Movie.cs`, `EnrichmentStatus.cs`, `EnrichmentFailureCategory.cs`, `InvalidTransitionException.cs`, `MovieMetadata.cs`, `MovieId.cs`, `ImdbId.cs`, `ImdbRating.cs`, `Runtime.cs`, `ReleaseYear.cs`, `LibraryPath.cs`, `MediaFormat.cs`.
  - The `LamuFlix.Test.csproj` reference line.
  - Test files under `tests/LamuFlix.Test/Domain/`.
- **ADR.** `docs/adr/0014-core-movie-aggregate-separate-from-ef-entity.md` is required for a size-L ticket. Keel drafts it in a separate Phase A ADR step after this brief, and it is **not** part of this grill write. It records:
  - the Core aggregate and value objects as the domain model beside the EF entity;
  - persistence and wiring deferred;
  - time passed in explicitly;
  - invariants enforced in the value-object constructors.

## Test strategy and gate expectations

- **Frameworks (Q10a).** xUnit v3 and Shouldly from the existing CPM (`Directory.Packages.props:18,21`; Shouldly is **4.3.0**, which corrects recon §5). NSubstitute is not needed.
- **State-machine tests.** An exhaustive Theory matrix: 4 states × 4 enrichment actions, plus 2 watchlist states × 2 actions. Each case asserts either the legal result, including every tracking field in the Q6 table, or the throw with the aggregate unchanged.
- **Value-object tests.** Boundary tests on both sides of every rule in Q9, including the `with`-bypass guarantee, `MediaFormat` normalization, and `ReleaseYear` bounds relative to the supplied `now`.
- **Coverage AC.** The "100% branch coverage on state machine" criterion is evidenced by the exhaustive matrix plus Stryker (`gates.mutation.threshold` 80, never lowered).
  - No coverage collector is referenced (`tests/LamuFlix.Test/LamuFlix.Test.csproj:11-16`), so no branch report is attached.
  - Adding a coverage package is §2.3 #1 and is not authorized.
- **Property tests (D9, D10).** Required: the owner answered YES to Q10(b).
  - FsCheck property tests for the value-object invariants and the transition table, each tagged `[Trait("Category", "Property")]`.
  - `run-property-tests.ps1` runs as Gate 9, after `dotnet test`, and must exit 0; exit 2 SKIPPED is a failure.
- **Phase B gates, under pwsh 7.** Each must exit 0 on the `main...HEAD` diff:
  - `run-roslyn-analyzers.ps1`
  - `run-cyclomatic-complexity.ps1` (15) and the refactor pass at `-Threshold 6`
  - `run-jetbrains-inspectcode.ps1`
  - `dotnet format --verify-no-changes`
  - `run-vulnerable-packages.ps1`
  - `dotnet build`
  - `dotnet test`, including ArchitectureTests
  - `dotnet stryker`, before the PR (invocation and evidence fixed by D7)

  How to read the exit codes:
  - Exit 0 = Pass and exit 1 = Fail.
  - Exit 2 = SKIPPED, which never counts as a pass.
  - A gate that could not run is reported as `Could not run`.
  - A plain build cannot substitute for the analyzers.

  There are no `/web` changes and no web gates.

## Closing bar and round cap (Q11)

- **Blocking.** Every verified in-scope **Critical or High** finding is fixed and re-verified before the PR.
- **Follow-ups.** Medium and Low findings, and anything outside the frozen scope, become follow-ups with their source and severity intact (DEV-291 precedent, `specs/DEV-291/brief.md:19`). Rigger records them on Patron's decision.
- **Round cap.** Two formal review rounds, with at most two fix commits per round. A third round escalates to the user and does not publish.
- **Ship review.** `/ship-review` runs because the ticket is size L. On M/L tickets, adjudication needs all three axis reports (Sentry, Ledger, Compass).
- **Checkboxes.** The owner checkboxes consume no round.

## Follow-ups (Patron decides; Rigger records; filing is not a plan change)

1. Persistence, rehydration, and adoption of the aggregate: EF mapping or migration, and wiring `EnrichmentJobProcessor` and `MovieService`. Parent DEV-282, estimate 5, `size:L` (Q2).
2. Map `InvalidTransitionException` to HTTP 409 at the API boundary (Q8). This follow-up also carries the constitution PR-gate line "new exception types are mapped in the single IExceptionHandler" (D6).
3. ~~If Q10(b) is answered NO: how the property-tests gate is satisfied.~~ **Deleted by D11:** the owner answered YES.

## Task ordering and next action

**Phase A, in this worktree.**
1. This brief, `CONCLUSIONS.md`, and the task note. This step is done.
2. Keel drafts ADR 0014, in a separate step.
3. Quill drafts `spec.md`, `plan.md`, and `tasks.md` from this brief.
4. Keel runs read-only `/speckit-analyze` and sends a numbered fix list.
5. Plan challenge and adjudication, then freeze.
6. The spec PR carries the two owner checkboxes, answered YES. Gate 1 opens when the user merges the spec PR.

Phase A changes specs and docs only. Next action: hand this brief to the Conductor.

**Phase B, only after Gate 1 and the user's merge of the spec PR.**
1. Verify the worktree and branch, and use the `DEV-294 - {subject}` commit format.
2. Add value objects and enums, then `InvalidTransitionException`, then `MovieMetadata`, then `Movie`, then the csproj reference and the tests.
3. Run build and `dotnet test` immediately.
4. Run the gates sequentially, then Stryker.
5. Open the delivery PR. The main checkout stays clean.

## Phase A analyze decisions (Keel, 2026-09-26)

Source: Keel's read-only `/speckit-analyze` (DEV-294 note:35-160) and recon-DEV-294 §10 (lines 183-234). These decisions override the earlier lines they name. They do not change frozen scope, the owner checkboxes, or task order. Quill applies them through the fix list (items 1-16).

- **D1 (finding 1). No XML doc on `InvalidTransitionException`.** The constitution's Coding Conventions forbid XML comments outside the exempt uses. ADR 0014 and follow-up #2 record the HTTP 409 mapping. Supersedes Frozen scope item 4, second bullet.
- **D2 (finding 2). `TryCreate` on every value object.** The constitution's Coding Conventions require TryParse-style factories. Each of the 7 value objects gets `public static bool TryCreate(<ctor args>, [NotNullWhen(true)] out T? result)` alongside its throwing constructor, with the same rules. For `ReleaseYear` the signature is `TryCreate(int value, DateTimeOffset now, out ReleaseYear? result)`. Tests cover a valid and an invalid `TryCreate` for each type.
- **D3 (finding 12). `MediaFormat` normalization order.** Trim, then strip exactly one leading `.`, then lowercase, then reject blank. So `"."` and `" . "` are invalid, and `"..mkv"` becomes `".mkv"`.
- **D4 (finding 5). `with`-bypass evidence.** `with { Value = x }` does not compile against a get-only property, so no runtime test can exercise it. The acceptance item is evidenced by a reflection test: every public property of every value object has `SetMethod == null` (no setter, no `init`).
- **D5 (finding 8). CONTEXT.md NO consequence.** Recorded next to the checkbox above. A NO answer conflicts with constitution VIII. The box stays unticked.
- **D6 (finding 9, recon §10a). IExceptionHandler gate: recorded deviation, no fix in scope.** No `IExceptionHandler` implementation exists under `src/` at 1c3d580 (recon-DEV-294:190-194). The constitution PR-gate line "new exception types are mapped in the single IExceptionHandler" therefore cannot be met without creating the handler. Creating one would add a new API-boundary component and edit unnamed API files, which is outside frozen scope and the Q8 ruling. So:
  - plan.md's Constitution Check lists this gate line as **not met in DEV-294**, cites recon §10a and Q8, and names follow-up #2 as the carrier;
  - finding 9 drops from HIGH to a recorded deviation. There is no new task and no owner checkbox, and the Gate 1 reviewer sees the deviation in the Constitution Check.
- **D7 (finding 14, recon §10b). Stryker invocation.** `stryker-config.json` (break 80, `since.target: main`) and `harness.yml` (`gates.mutation.threshold: 80`) name no project. `dotnet stryker` (dotnet-stryker 4.16.0) runs from the repo root and discovers projects solution-wide, mutating only files changed against `main` (recon-DEV-294:198-222). So:
  - plan.md and tasks.md use the repo invocation as-is: `dotnet stryker` from the repo root with the unchanged `stryker-config.json`. Do not add CLI project flags and do not edit the config (it is an unnamed file, §2.3 #6);
  - the Phase B evidence is the Stryker report, whose mutated-file list must contain only `src/LamuFlix.Core/Domain/*.cs`, with a score of at least 80;
  - **stop condition:** if the report mutates files outside `src/LamuFlix.Core/Domain/`, does not cover the Core domain files, or Stryker cannot run solution-wide, stop and report to Keel. Do not add flags or edit config to work around it.
- **D8 (Sentry S4, Patron R1).** ImdbRating is valid iff 0.0m <= v <= 10.0m and v == decimal.Round(v, 1). The check is on the numeric value, not the decimal scale. So 5.50m and 10.00m are valid, and 5.55m and 10.05m are invalid. The supplied value is kept as is: no normalizing, no rounding. The tests add the four named examples, and the FsCheck property uses the same predicate. Supersedes the reading of brief:56.
- **D9 (Compass C4 / Sentry S1, Patron R2).** Add only the base `FsCheck` package: one PackageVersion in Directory.Packages.props and one PackageReference in tests/LamuFlix.Test/LamuFlix.Test.csproj. Pin a stable version compatible with net10.0, chosen and recorded in the commit at Phase B. FsCheck.Xunit.v3 is not authorized. Properties are plain xUnit v3 `[Fact]` methods in tests/LamuFlix.Test/Domain/PropertyTests.cs. Each is tagged `[Trait("Category", "Property")]` and runs FsCheck so that a failure propagates to xUnit. They cover every value-object invariant and the transition table.
- **D10 (S1). Gate sequence becomes 10 gates.** Insert as Gate 9, after `dotnet test`: `./scripts/run-property-tests.ps1 -Project tests/LamuFlix.Test/LamuFlix.Test.csproj`. It must exit 0, and exit 2 SKIPPED is a failure. Stryker becomes Gate 10. Supersedes brief:114 and brief:76.
- **D11 (H2/C1). CONTEXT.md is Phase B Commit 6**, after the tests and before the gates. It extends Enrichment Status (CONTEXT.md:27-29) and Watchlist (CONTEXT.md:38), and adds Movie Aggregate and Enrichment Attempt, per CONCLUSIONS:168. Commit 5b is unconditional. brief:65 "no new NuGet package" now reads "no new NuGet package except FsCheck (D9)". Follow-up #3 (brief:145) is deleted.
- **D12 (C5).** ReleaseYear has an explicit constructor and is not a positional record. `now` is a constructor and TryCreate parameter only, never a property or field. The only public property is `Value`.
