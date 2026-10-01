# DEV-304 — Brief (Keel)

Ticket: DEV-304 — Implement `DirectoryMediaLibraryScanner` with System.IO.Abstractions and MockFileSystem tests. Size M, UI false, parent DEV-283.
Worktree: `F:\Dev\LamuFlix.worktrees\feature-304-spec` · branch `feature/304-spec` · base `f5d5d60` (grill rulings `db7f80e`, coverage ruling `6e1ef72`).
Sources: ticket text (`recon-DEV-304` §2), `recon-DEV-304`, Patron rulings Q1–Q12 and Q12a in `CONCLUSIONS.md`, taste rulings in `ASSUMPTIONS.md`.

If a decision is not in this file, it is not decided. Quill drafts `spec.md`, `plan.md`, `tasks.md` from this brief only.

---

## 1. Closing bar

DEV-304 is done when all of the following hold:

1. `DirectoryMediaLibraryScanner` in `src/LamuFlix.Infrastructure/FileSystem/` implements `IMediaLibraryScanner.Scan(LibraryPath)` and touches the filesystem **only** through an injected `IFileSystem` (ticket AC: no physical disk).
2. `ScannedMovie` carries `ReleaseYear? Year` and `long SizeBytes` (Q2), and the scanner fills them per Q5, Q7 and Q10.
3. Folder-name parsing, extension allowlist, primary-file selection, top-level-only enumeration and the two failure exceptions behave exactly as Q5–Q9 state.
4. The MockFileSystem suite in `tests/LamuFlix.UnitTests/FileSystem/` covers every case in §5.3 and reaches **100% line AND branch coverage** on the scanner and every helper added for it, measured by the command in §5.4 (Q12).
5. The four existing `ScannedMovie` construction sites in `ImportMovieFolderCommandHandlerTests` compile against the new shape, and their assertions are otherwise unchanged (Q2).
6. All gates in §5.4 pass. Full `dotnet test` is green and `dotnet format --verify-no-changes` is clean.

## 2. Frozen scope

**In scope**
- `DirectoryMediaLibraryScanner` (new) in the new subfolder `src/LamuFlix.Infrastructure/FileSystem/`. The ticket names this folder, so no §2.3 ruling is needed (recon §12.4).
- Extend `ScannedMovie` with `ReleaseYear? Year, long SizeBytes`. Both are required positional arguments, appended after `Format` (Q2).
- Update the 4 construction sites in `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs` (lines 32, 58, 79, 100; recon §5). This is compile-forced, so it is in scope under §2.3 item 6 (Q2).
- New package `System.IO.Abstractions` for `LamuFlix.Infrastructure` and `System.IO.Abstractions.TestingHelpers` for `LamuFlix.UnitTests`, both pinned centrally in `Directory.Packages.props` (Q4). Basis: constitution.md:301/325.
- New test-only package `coverlet.collector` (private asset) in `LamuFlix.UnitTests`, version pinned centrally after compatibility verification (Q12a).
- The MockFileSystem test suite (Q4, Q12).

**Out of scope (do not touch)**
- Persistence of any kind: no `MovieRecord`, `MovieConfiguration`, migration, `Movie.Create` change, and no change to `ImportMovieFolderCommandHandler` behaviour (Q3). Future persistence of year/size is noted only; no follow-up ticket is filed (Q3).
- DI registration, a service-collection extension, host wiring (`Program.cs`), health checks, telemetry constants (Q11). Future composition is noted only, no ticket.
- Any change to the `IMediaLibraryScanner` port signature or its nullability (Q9).
- Custom exception types (Q9), and new Core value objects such as an extension allowlist type (Q6).
- `LamuFlix.Test` and its legacy files. Nothing there is deleted or rewritten (Q1).
- Gate thresholds, `harness.yml`, `stryker-config.json` (Q12). Test platform (`IsTestingPlatformApplication` stays false, VSTest kept), runsettings files, CI workflows, reportgenerator or any other coverage tool, and coverage exclusion attributes (Q12a).
- `Features:LocalPlay`, secrets, `Process.Start`. None are touched (recon §12.6–7). The scanner only enumerates and stats files.
- HTTP API, DTOs, OpenAPI, `/web` (recon §12.8).
- New projects or layers. `LamuFlix.UnitTests` already references Infrastructure (`LamuFlix.UnitTests.csproj:30-33`, Q4), so no reference edit is needed.

**Ticket-text interpretation (recorded, not escalated):** "Fully replaces the legacy ignored disk-bound tests in LamuFlix.Test." No such tests exist (recon §7). The clause is satisfied because there is nothing to replace. The full new suite is still delivered, and Rigger records the recon fact as a comment on DEV-304 (Q1).

## 3. Round cap

- Grill: 1 round, 12/12 questions used. Closed.
- Spec review (Keel ↔ Quill): **2 rounds, hard cap** (Q12). Anything still open after round 2 goes to the Conductor as `blocked:`.
- Plan challenge: one adjudication pass (Q12).

## 4. Grill answers (Patron, Q1–Q12; full text and citations in `CONCLUSIONS.md`)

| # | Ruling |
|---|---|
| Q1 | The legacy-replacement clause is satisfied because no legacy scanner tests exist (recon §7). The clause stays, the full new suite is delivered, and Rigger records the recon fact on DEV-304. Nothing is dropped and no unrelated test is touched. |
| Q2 | `ScannedMovie(LibraryPath Path, string Title, MediaFormat Format, ReleaseYear? Year, long SizeBytes)`. Both new arguments are required. Update the 4 test construction sites and keep handler behaviour unchanged. |
| Q3 | No schema, migration, `MovieRecord`, `Movie.Create` or handler persistence change. Future persistence is noted, no ticket. |
| Q4 | Approve `System.IO.Abstractions` + `System.IO.Abstractions.TestingHelpers`, centrally pinned to mutually compatible latest stable versions **verified at selection time** (no version is asserted by the ruling). Tests go in `tests/LamuFlix.UnitTests/FileSystem/`, reusing the existing Infrastructure reference. |
| Q5 | [assumed] Trim the final folder name. A year marker is a **trailing** `(YYYY)` or `[YYYY]` with exactly four ASCII digits and matching brackets. The year is accepted only through `ReleaseYear.TryCreate`, using one injected UTC "now" per scan. The suffix is stripped only when the year is valid AND the text before it is non-blank (trimmed). A valid year-only name (`(2010)`) keeps `Year` but uses the whole trimmed name as `Title`. A missing, malformed or mismatched marker, or an invalid year, gives `Year = null` and the whole trimmed name. Dots and underscores stay literal. Range constants are not duplicated. |
| Q6 | [assumed] A scanner-private static allowlist `.mkv .mp4 .avi .m4v`, matched `OrdinalIgnoreCase` on every OS. `Format` = `new MediaFormat(selected extension)` (existing normalisation). No new Core type. |
| Q7 | [assumed] Primary file = the largest supported **top-level** file by byte length. Ties go to the file name ascending, `StringComparer.Ordinal`. |
| Q8 | [assumed] Top-level only. Videos in subfolders are ignored, and a folder whose only video is in a subfolder raises the Q9 no-video exception. |
| Q9 | The port stays non-nullable. A missing folder throws `System.IO.DirectoryNotFoundException` via the `IFileSystem` path. An existing folder with no supported top-level video throws `System.InvalidOperationException`. No custom exception type. Other I/O errors propagate unchanged. |
| Q10 | `SizeBytes` = the selected file's `IFileInfo.Length`. `Format` = its normalised extension. `Path` = the unchanged input `LibraryPath` (the folder, not the file). |
| Q11 | `public sealed class DirectoryMediaLibraryScanner(IFileSystem fileSystem, TimeProvider timeProvider)`, calling `GetUtcNow()` once per `Scan`. No DI extension, host wiring, health check or telemetry constant. No `Process.Start`, LocalPlay or secrets. |
| Q12a | Approve `coverlet.collector` as a private, test-only dependency of `LamuFlix.UnitTests`: a `PackageReference` in that csproj plus a `PackageVersion` in `Directory.Packages.props`, pinned to the latest stable verified against Microsoft.NET.Test.Sdk 17.12.0 and the real .NET 10 run (vendor rule `aaron-csharp-testing.mdc:43` names the collector, not its version). VSTest stays. No runsettings, CI, reportgenerator or threshold change. Proof = a fresh Cobertura report (§5.4). |
| Q12 | 100% line AND branch coverage on the scanner and every helper added for it. Existing complexity and Core-only mutation gates stay unchanged. Spec review is capped at 2 rounds, plan challenge at 1 pass. **Freeze condition:** a real coverage command with a per-class line/branch report must be identified (§5.4). Passing tests alone do not prove 100%. |

## 5. Plan decisions

### 5.1 Approach
- **Class shape:** `public sealed class DirectoryMediaLibraryScanner(IFileSystem fileSystem, TimeProvider timeProvider) : IMediaLibraryScanner` in namespace `LamuFlix.Infrastructure.FileSystem`.
- **`Scan(LibraryPath folder)` flow** (one straight path, with guard clauses):
  1. `var directory = fileSystem.DirectoryInfo.New(folder.Value)`. If `!directory.Exists`, throw `DirectoryNotFoundException` naming the folder (Q9). Raising it explicitly keeps the failure deterministic on MockFileSystem rather than relying on an enumeration side effect. It is still "via the IFileSystem path", since existence comes from `IFileSystem`.
  2. `SelectPrimaryVideo(directory)`: call `directory.EnumerateFiles()` (top-level only, **never** `SearchOption.AllDirectories`; Q8), filter to the allowlist on `Extension` with `OrdinalIgnoreCase` (Q6), then order by `Length` descending and `Name` with `StringComparer.Ordinal` ascending (Q7), and take the first or null. Null throws `InvalidOperationException` naming the folder (Q9).
  3. `ParseFolderName(directory.Name, timeProvider.GetUtcNow())` returns `(string Title, ReleaseYear? Year)` per Q5. It is a pure `private static` helper (or `internal static` on a small helper class if Quill needs direct tests; any such helper falls under the 100% coverage scope).
  4. `return new ScannedMovie(folder, title, new MediaFormat(primary.Extension), year, primary.Length)` (Q10).
- **Folder-name regex:** one compiled `[GeneratedRegex]`, anchored at the end, matching `^(?<title>.*?)\s*(?:\((?<year>\d{4})\)|\[(?<year>\d{4})\])$` with `RegexOptions.CultureInvariant`. Use ASCII digits only: `[0-9]{4}`, not `\d` (Q5 says "ASCII digits"). Quill must write `[0-9]`. Mismatched brackets (`(2010]`) do not match.
- **Allowlist:** `private static readonly FrozenSet<string>` (or `HashSet`) of `".mkv", ".mp4", ".avi", ".m4v"` with `StringComparer.OrdinalIgnoreCase`.
- **Time:** capture `timeProvider.GetUtcNow()` exactly once per `Scan` (Q5/Q11). `DateTime.Now` and `DateTimeOffset.Now` are banned (RS0030).
- **ScannedMovie edit:** append `ReleaseYear? Year, long SizeBytes` to the positional record. Keep the existing ReSharper disable comment only if it is still needed; this ticket is the "future consumer" it names (recon §3), so Quill should remove it when InspectCode no longer flags the properties. Decide at the gate, without adding new suppressions.

### 5.2 Files touched
New:
- `src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs`
- `tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs` (may split into `…ParsingTests` and `…SelectionTests` if the file grows past readability; the split is Quill's choice)

Edited:
- `src/LamuFlix.Core/Ports/ScannedMovie.cs` (Q2)
- `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs`: only the 4 constructor calls (Q2)
- `Directory.Packages.props`: two `PackageVersion` entries (Q4)
- `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`: `PackageReference System.IO.Abstractions` (Q4)
- `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`: `PackageReference System.IO.Abstractions.TestingHelpers` (Q4) and `PackageReference coverlet.collector` with `PrivateAssets=all` and the standard `IncludeAssets` (Q12a)
- `Directory.Packages.props`: also the `coverlet.collector` `PackageVersion` in the Test block (Q12a)

Nothing is deleted.

### 5.3 Test strategy (MockFileSystem only; xUnit; `FakeTimeProvider` or a fixed `TimeProvider` stub)
Every test builds a `MockFileSystem` and a fixed "now" (for example 2026-06-01Z, so the valid range is 1888..2031). No physical disk and no temp paths. Each case asserts the full `ScannedMovie` (Path, Title, Format, Year, SizeBytes) or the exact exception type.

Parsing (Q5), required cases, with `[Theory]` rows where natural:
1. `Inception (2010)` → Title `Inception`, Year 2010.
2. `The Matrix [1999]` → Title `The Matrix`, Year 1999 (multi-word + brackets).
3. Multi-word with inner spaces/punctuation preserved: `The Lord of the Rings - The Fellowship (2001)`.
4. No marker: `Inception` → whole name, Year null.
5. Surrounding whitespace is trimmed: `  Inception (2010)  ` (only if MockFileSystem can represent it; otherwise drop with a note).
6. Marker not trailing: `Inception (2010) Remastered` → whole name, null.
7. Mismatched brackets: `Inception (2010]` → whole name, null.
8. Not 4 digits: `Inception (201)`, `Inception (20100)` → whole name, null.
9. Out of range, too low: `Old (1887)` → whole name `Old (1887)`, null.
10. Out of range, too high: `Future (2032)` with now = 2026 → whole name, null. Boundary rows 1888 and now+5 (2031) are valid.
11. Year-only: `(2010)` → Title `(2010)`, Year 2010.
12. Dots/underscores stay literal: `The.Matrix.1999` → whole name, null; `The_Matrix (1999)` → Title `The_Matrix`.
13. Non-ASCII digits (for example Arabic-Indic `(٢٠١٠)`) → no match (proves `[0-9]`).

Selection / format / size (Q6, Q7, Q10):
14. Each allowlisted extension (`.mkv .mp4 .avi .m4v`) is selected, and Format is the lowercase name without the dot.
15. Upper/mixed case (`MOVIE.MKV`, `Movie.Mp4`) is selected; Format is normalised to lowercase.
16. Unsupported files only (`.srt .nfo .txt .wmv`) → `InvalidOperationException`.
17. Mixed supported + unsupported → the supported one is chosen even when the unsupported one is larger.
18. Several supported → the largest by bytes is chosen; `SizeBytes` equals its length.
19. Size tie → the Ordinal-ascending name wins (include a case where Ordinal and culture order differ, for example `a.mkv` vs `B.mkv`: Ordinal picks `B.mkv`).
20. `Path` equals the input `LibraryPath` (the folder), not the file.

Missing / nested (Q8, Q9):
21. Folder does not exist → `DirectoryNotFoundException`.
22. Empty existing folder → `InvalidOperationException`.
23. Video only in a subfolder (`Featurettes/x.mkv`) → `InvalidOperationException`.
24. Top-level video + a larger video in a subfolder → the top-level one is chosen (proves no recursion).

Existing tests: `ImportMovieFolderCommandHandlerTests` stays green with only the constructor-arg edit. `ArchitectureTests` must stay green with no edit (port already whitelisted, recon §10).

### 5.4 Gate expectations
- Roslyn analyzers, cyclomatic complexity ≤ 15, and JetBrains InspectCode on every changed `.cs` file must exit 0. Refactor gate: complexity ≤ 6. Pass `-Files` as real array args, not one comma-joined string (recon §11 gotcha).
- No suppressions without a cited ruling. `dotnet format --verify-no-changes` must be clean.
- Full `dotnet test` must be green.
- Mutation: Stryker stays Core-scoped with no config change (Q12). `ScannedMovie` is Core but carries no logic.
- **Coverage (Q12 + Q12a; tooling settled by recon-DEV-304 §14):**
  - Command, from the worktree root: `dotnet test tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj --collect 'XPlat Code Coverage' --results-directory <fresh temp dir outside the repo>`.
  - **No-op trap (recon §14.3 Run A):** without the collector, this command exits 0 and emits nothing. A receipt is valid only if tests pass AND that same invocation emitted `coverage.cobertura.xml` under its GUID results subfolder. Locate it there, never reuse a stale report, and never write coverage output into the worktree.
  - Scope: every Cobertura `<class>` for `LamuFlix.Infrastructure.FileSystem.DirectoryMediaLibraryScanner`, any helper added for it, and every compiler-generated nested class, lambda closure or `[GeneratedRegex]` helper attributable to it. The scanner must report a nonempty set of executable lines.
  - Pass condition: `line-rate="1"` and `branch-rate="1"` on each in-scope class, AND every `<line>` has `hits > 0` and every branch line's `condition-coverage` is `100% (n/n)`. Rounded rate strings alone are not proof. No `[ExcludeFromCodeCoverage]` or filter may hide scanner/helper code.
  - The receipt (command, exit code, report path, the in-scope `<class>` rows) goes into the implementation PR as evidence. CI is unchanged.

### 5.5 Task ordering constraints
1. Packages: `Directory.Packages.props` + csproj references for `System.IO.Abstractions`, `System.IO.Abstractions.TestingHelpers` and `coverlet.collector`, with versions verified at selection (Q4, Q12a). Must build before anything else. Smoke-check that the coverage command now emits a Cobertura file.
2. `ScannedMovie` extension + the 4 handler-test constructor edits, in one task so the solution never stays red (Q2).
3. Folder-name parsing helper + parsing tests (§5.3 cases 1–13). Independent of 4.
4. Primary-file selection + allowlist + size/format tests (§5.3 cases 14–20). Independent of 3.
5. `Scan` composition + missing/nested tests (§5.3 cases 21–24). Depends on 2, 3, 4.
6. Coverage run per §5.4, proving 100% line+branch on the scanner (and helpers), then gates (analyzers, complexity 15 then 6, InspectCode, format, full suite). Last.

## 6. Constraints carried from rules
- Commit messages and the PR title follow `DEV-304 - {subject}`.
- Contract chain untouched: no DTO, OpenAPI or TS changes.
- Work only in this worktree. The main checkout stays clean.
- The scanner must never use `System.IO.File`, `Directory`, `Path`-based disk calls, or a `new FileSystem()` default. `IFileSystem` is the only boundary (ticket AC, Q11).

## 7. Spec-review decisions (Keel, round 1)

Answers to Quill's `needs decision:` items in `DRAFTING_RECEIPT.md`, plus the open points `/speckit-analyze` raised. None of them changes scope.

- **D1 — §5.5 item 5 reading: confirmed.** Items 3 and 4 land with the minimal `Scan` shell, because the helpers are `private static` and §5.3 reaches every case through `Scan`. Item 5 means the guard clauses (Q9) plus the final result mapping. The dependency direction is unchanged.
- **D2 — gates missing from §5.4: confirmed in scope.** `run-vulnerable-packages.ps1` runs because two new packages enter the graph (`harness.yml:23-25`). `run-property-tests.ps1` is expected to exit 2. This ticket's opt-out is recorded in the task note and the PR body (`harness.yml:30-34`). §5.4 is read as including both.
- **D3 — helper visibility: `private static`, reached through `Scan`.** The `internal static` alternative in §5.1 is withdrawn. It would need an `InternalsVisibleTo` edit outside §5.2.
- **D4 — clock stub: the existing `tests/LamuFlix.UnitTests/Features/FixedTimeProvider.cs`.** `FakeTimeProvider` would bring in a new package (`Microsoft.Extensions.Time.Testing`), which is outside §2.
- **D5 — vocabulary (`constitution.md:277`): no `CONTEXT.md` edit.** "Primary video" names only a private helper and creates no public type, member or API term. `CONTEXT.md:21-22` (Import, _Avoid_ "Scan" for the single-folder case) applies to command naming. The port `IMediaLibraryScanner.Scan` already exists, and the ticket names `DirectoryMediaLibraryScanner`, so the ticket text decides.
- **D6 — file count:** `git diff --stat` covers the seven files in §5.2, or eight if the test file is split as §5.2 allows.
- **D7 — final gate pass (§5.5 item 6):** after coverage, run analyzers, complexity at 15 and then at `-Threshold 6`, and InspectCode once over **every** changed `.cs` file (scanner, test file(s), `ScannedMovie.cs`, `ImportMovieFolderCommandHandlerTests.cs`). Then run format and the full suite.

## 8. Plan-challenge adjudication (Keel, single pass per Q12)

Inputs: `findings-DEV-304-Sentry` (Risk), `findings-DEV-304-Ledger` (Standards), `findings-DEV-304-Compass` (Spec). All three axes are present. Each finding was checked against `plan.md`, `tasks.md` and `spec.md` at the cited line. None of them changes scope, adds a dependency or needs a §2.3 escalation. **The plan freezes once Quill has applied A1–A11.**

### Accepted (required action)

| # | Finding(s) | Verified at | Ruling / action |
|---|---|---|---|
| A1 | Sentry H1 | `plan.md:120` vs table `:135`, FR-008, Q5 | Real contradiction: the prose sends "blank preceding text" to `(whole name, null)`, but Q5, FR-008 and the table keep the year. Rewrite `:120` as three outcomes: (a) no match or `TryCreate` rejects → `(whole trimmed name, null)`; (b) valid year with blank preceding text → `(whole trimmed name, year)`; (c) valid year with non-blank preceding text → `(preceding text, year)`. |
| A2 | Sentry H2 + Compass F2 | `plan.md:89`, `tasks.md:65` (T010), FR-006 | The trim is a Q5 ruling, but no step says where it happens. **Decision D8:** `ParseFolderName` calls `Trim()` on `directory.Name` first, and both the regex match and the "whole name" fallback use the trimmed value. The pattern keeps `$` exactly as §5.1 gives it. Trimming first removes any trailing `\n`, so a `$` vs `\z` difference cannot be observed. State this in plan Design §2 step 5 and §4, and in T010. |
| A3 | Compass F3 | `tasks.md:65` (T010) | T010 must name branch (b) from A1, the valid year-only name: `(2010)` → Title `(2010)`, Year 2010 (case 11). |
| A4 | Compass F1 + Sentry M2 | `spec.md:84`, `:119`, `:137`; `tasks.md:92-93` | US4 scenario 5 and the last clause of FR-013 have no proof. **Decision D9:** "Other I/O errors propagate unchanged" is a structural property (the scanner never catches anything), so it is proven by inspection, not by a new test. A test would need an `IFileSystem` double beyond `MockFileSystem`, which the constitution's MockFileSystem-only boundary and §5.3 do not call for. Add to T021: grep the scanner for `try`/`catch`/`when (` and confirm there are none. In spec US4, mark scenario 5 as "verified by T021 inspection". §5.3 stays at 24 cases. |
| A5 | Ledger LOW-1 | `plan.md:180`; T006/T009/T012/T015 | Verified: every test in `ImportMovieFolderCommandHandlerTests.cs` (lines 29/37/40 …) has `// arrange` / `// act` / `// assert`. Require those headers, and no other comments, in every new test (plan Test Strategy and T006). |
| A6 | Ledger LOW-2 | `plan.md:180`; T006/T009/T012/T015 | Verified the house form `HandleAsync_HappyPath_…` (`:27`). Require `Scan_<Condition>_<Expected>` names, for example `Scan_ParenthesisedYear_ReturnsTitleAndYear`. |
| A7 | Ledger LOW-3 (covers Sentry's "mock setup lifetime" LOW) | `tasks.md:50` (T006) | Each test case builds a fresh `MockFileSystem`. No static or shared instance. The immutable `FixedTimeProvider` may be shared. |
| A8 | Ledger LOW-4 | `tasks.md:38` (T005) | The four sites (32/58/79/100) all read `new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "Imported", new MediaFormat("mkv"))`. Pin the edit to appending `, null, 1024L` at each one. |
| A9 | Compass L1, L2 | `plan.md:163`, `:23` | Fix the wrong task id: `:163` should say T005, not T002. Fix the count at `:23`: "two private static helper methods, one private static allowlist field, plus the source-generated regex partial method". |
| A10 | Compass L3 | `spec.md:144` (FR-020) | Add the vulnerable-packages gate (exit 0) and the property-test gate (expected exit 2, opt-out recorded per `harness.yml:30-34`) to FR-020, matching D2. |
| A11 | Compass L4, L5 | `spec.md:102`, `:19`; `tasks.md:120`, `:137` | L4: T026 also confirms `ArchitectureTests.cs` is absent from the diff, and T025's full suite run shows it green. L5: no direct-construction test, because a record with no logic gains nothing from one. Reword US1's Independent Test so it relies on the full-`ScannedMovie` assertions of §5.3 case 1 (T006) plus the T005 compile, not on a standalone construction test. |

**Applied by Quill and verified by Keel.** A1–A11 landed in spec.md, plan.md and tasks.md. Quill made two edits outside the listed locations, and both are accepted. First, plan Test Strategy now says each test case builds a fresh `MockFileSystem`, which is the A7 rule. Second, FR-008 had the same bug as A1: its "Otherwise" list included "a blank title before the marker → Year null", which contradicts Q5. FR-008 is now the same three outcomes (a)/(b)/(c), so spec, plan and Q5 agree. **Plan frozen.**

### Rejected (rationale)

| # | Finding | Rationale |
|---|---|---|
| R1 | Sentry M1 (root path gives an empty title) | Below the evidence threshold. No concrete input is given, and `DirectoryInfo.Name` for a root is the root itself (`C:\`, `/`), not empty. No ruling makes a filesystem root a movie folder, so Q5's "whole trimmed name" already decides the output. No action. |
| R2 | Sentry L1–L6 (unspecified) | No `file:line` and no failure scenario, so there is nothing to verify. The two named themes are handled elsewhere: mock lifetime under A7, and tie-break precedence is CLEAR (Ledger verified `plan.md:107`, `'B'` < `'a'` ordinal). |
| R3 | Compass L6 (phase-to-US mapping drift) | No line or mismatch is named. I checked the mapping: Phase 3–7 → US1–US5 at `tasks.md:44/58/72/86/100`. No drift. |
| R4 | Compass L7 (Rigger recon-fact comment has no owner) | This is not a `tasks.md` item, since the implementer never writes to YouTrack. It is owned by §9 below and goes to the Conductor to dispatch Rigger. |

## 9. Traceability
- Ticket scope item 1 → Q2, Q5, Q6, Q7, Q10, §5.1. Scope item 2 → Q1, Q8, Q9, §5.3. AC "runs against IFileSystem" → Q11, §6. AC "100% coverage" → Q12, Q12a, §5.4.
- Rigger action owed: recon-fact comment on DEV-304 for Q1 (no follow-up tickets; Q3/Q11 say note only).
