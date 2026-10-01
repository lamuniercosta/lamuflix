# Implementation Plan: DirectoryMediaLibraryScanner

**Branch**: `feature/304-spec` | **Date**: 2026-10-01 | **Spec**: [spec.md](spec.md)

**Input**: `spec.md`, `brief.md`, `CONCLUSIONS.md` (Q1-Q12, Q12a), `ASSUMPTIONS.md`, recon-DEV-304, `.specify/memory/constitution.md`, `specs/PRODUCT.md`

## Summary

Add one sealed filesystem adapter, `DirectoryMediaLibraryScanner`, in `src/LamuFlix.Infrastructure/FileSystem/`, that reads one library folder through an injected `IFileSystem` and returns title, release year, media format and byte size. Extend the existing `ScannedMovie` port record with `ReleaseYear? Year` and `long SizeBytes`. Add a `MockFileSystem` suite in `tests/LamuFlix.UnitTests/FileSystem/` and prove 100% line and branch coverage on the scanner and its helpers with a freshly emitted Cobertura report.

Every decision below is already fixed by `brief.md`, `CONCLUSIONS.md` or `ASSUMPTIONS.md`. Where the brief offers a choice, this plan records the branch taken and why. Nothing new is decided here.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`net10.0`), `Nullable` enable, `TreatWarningsAsErrors` true
**Primary Dependencies**: new `System.IO.Abstractions` in `LamuFlix.Infrastructure`; new `System.IO.Abstractions.TestingHelpers` and new private `coverlet.collector` in `LamuFlix.UnitTests` (Q4, Q12a). Versions are latest stable, pinned centrally, verified mutually compatible at selection time. No version is asserted by any ruling.
**Storage**: none. The scanner reads a library folder; nothing is persisted (Q3).
**Testing**: xUnit v3 + Shouldly + `[Theory]`/`[MemberData]`, in-memory `MockFileSystem`, the existing `FixedTimeProvider` clock stub. No physical disk, no temp paths.
**Target Platform**: any OS the abstraction runs on; extension matching and tie-breaking are specified to be identical on every OS (Q6, Q7).
**Project Type**: adapter inside an existing library project; no new project (brief §2, out of scope).
**Performance Goals**: none stated by the ticket; the folder is enumerated once, top-level only.
**Constraints**: `IFileSystem` is the only filesystem boundary; `SearchOption.AllDirectories` never used; no ambient current-time call; one clock read per scan; no comment, suppression or new project-owned type beyond what the rulings name.
**Scale/Scope**: one class, two private static helper methods, one private static allowlist field, plus the source-generated regex partial method, one new test folder, 24 enumerated test cases, two build-file edits, one Core record edit, four call-site edits.

## Constitution Check

*Gate: must pass before research; re-checked after design.*

- **Dependencies** (§2.3 item 1): `System.IO.Abstractions` + `System.IO.Abstractions.TestingHelpers` approved in Q4 on the basis of `constitution.md:301` and `constitution.md:325` (Technology Stack row "File system | System.IO.Abstractions | MockFileSystem in unit tests"). `coverlet.collector` approved in Q12a on the basis of `recon-DEV-304` §14.1-§14.6 and `.cursor/rules/vendor/aaron-csharp-testing.mdc:43`, restricted to a `PackageReference` in the unit test project plus one central `PackageVersion`. No dotnet tool, no runsettings, no CI change, no threshold change. **PASS.**
- **Architecture** (§2.3 item 2): no new project, no top-level folder, no layer. `src/LamuFlix.Infrastructure/FileSystem/` is a subfolder of an existing project **and is named by the ticket**, so it is already decided (recon §12.4). Adapters live in Infrastructure, which `constitution.md:353` and the existing `Core_must_not_reference_infrastructure` rule both require. **PASS.**
- **Database schema** (§2.3 item 3): none. `MovieRecord`, `MovieConfiguration`, the model snapshot, the single `Initial` migration and `Movie.Create` are untouched (Q3, brief §2 out of scope). **PASS.**
- **API shape** (§2.3 item 4): none. No HTTP route, DTO field set, status code, OpenAPI document or `/web` change (recon §12.8). The contract chain C# DTO -> OpenAPI -> TypeScript is untouched. **PASS.**
- **Security & local execution** (§2.3 item 5): `Features:LocalPlay` untouched, no secret or connection string, no `Process.Start` (recon §12.6-12.7). The scanner enumerates and stats files; it launches nothing. **PASS.**
- **File scope** (§2.3 item 6): `src/LamuFlix.Core/Ports/ScannedMovie.cs` and the four call sites in `ImportMovieFolderCommandHandlerTests` are named by the brief's frozen scope and ruled in by Q2. Nothing is deleted; `LamuFlix.Test` is untouched (Q1). **PASS.**
- **Test pyramid** (`constitution.md:282-303`): the scanner's filesystem boundary is exercised with `MockFileSystem`, exactly as `constitution.md:301` and the PR checklist line `constitution.md:371` require. No data-access driver is mocked; no persistence behaviour is under test. **PASS.**
- **Static analysis** (`constitution.md:378-394`): no method may exceed complexity 15, and 6 at the refactor gate; failures are fixed by extracting helpers and early returns, never by suppression. **PASS.**
- **Comment and suppression rules** (`AGENTS.md`): no explanatory comment is added. The only pre-existing comment in scope is the `NotAccessedPositionalProperty.Global` disable pair on `ScannedMovie`, whose handling is an explicit task.
- **ADR**: none. No new port, no new architectural layer (Q11; ADRs are Keel's, not Quill's).
- **Owner checkboxes**: none. No ticket change, no constitution departure.

**Status: PASS, no departures, no escalations.**

## Project Structure

```text
src/LamuFlix.Core/Ports/ScannedMovie.cs                              (edit: append Year, SizeBytes)
src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs   (new)
tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs (new; split permitted, brief 5.2)
tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs (edit: 4 ctor calls only)
Directory.Packages.props                                             (edit: 3 PackageVersion entries)
src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj           (edit: 1 PackageReference)
tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj                   (edit: 2 PackageReferences)
```

Seven files, or eight if the test file is split per brief §5.2. Nothing else. `tests/LamuFlix.UnitTests` already references `LamuFlix.Infrastructure` (`LamuFlix.UnitTests.csproj:30-33`), so no project reference is added (Q4). No `.csproj`, props, migration, host, DI or legacy-test file outside that list is touched.

## Design

### 1. Class shape

```csharp
namespace LamuFlix.Infrastructure.FileSystem;

public sealed partial class DirectoryMediaLibraryScanner(
    IFileSystem fileSystem,
    TimeProvider timeProvider)
    : IMediaLibraryScanner
```

The class is `partial` because the folder-name pattern is a `[GeneratedRegex]` source-generated partial method, which the C# compiler requires its containing type to declare. This is a mechanical consequence of the pattern the brief fixes; it does not widen the class's surface. House precedent for the source-generated regex is `src/LamuFlix.Core/Domain/ImdbId.cs:36`.

Members beyond the primary constructor and `Scan` are the three private static members defined in Design §3 and §4, plus the source-generated partial method:

| Member | Kind | Defined in |
|---|---|---|
| `SupportedExtensions` | `private static readonly` allowlist (four extensions, `OrdinalIgnoreCase`) | Design §3 |
| `SelectPrimaryVideo` | `private static IFileInfo?` | Design §3 |
| `ParseFolderName` | `private static (string Title, ReleaseYear? Year)` | Design §4 |
| `FolderNamePattern` | `private static partial Regex`, `[GeneratedRegex]` | Design §4 |

Nothing public is added, and no helper is `internal` or visible to the test project (D3).

### 2. `Scan` flow (one straight path, guard clauses first)

1. `var directory = fileSystem.DirectoryInfo.New(folder.Value);`
2. `if (!directory.Exists) throw new DirectoryNotFoundException(<message naming folder.Value>);` — the existence check is read through `IFileSystem`, so the failure is still "via the `IFileSystem` path" (Q9). Raising it explicitly rather than letting enumeration fail keeps the failure deterministic on an in-memory filesystem, where `EnumerateFiles` on a missing directory does not necessarily throw.
3. `var primary = SelectPrimaryVideo(directory);` (see 3).
4. `var now = timeProvider.GetUtcNow();` — sampled **once**, here, and only here (FR-003).
5. `var (title, year) = ParseFolderName(directory.Name, now);` (see 4). `ParseFolderName` trims `directory.Name` as its first statement, so the regex match and the whole-name fallback both read the trimmed value (D8).
6. `return new ScannedMovie(folder, title, new MediaFormat(primary.Extension), year, primary.Length);` (Q10).

Complexity: two guard clauses plus three straight-line steps, well under the refactor ceiling of 6.

### 3. `SelectPrimaryVideo`

```csharp
private static IFileInfo? SelectPrimaryVideo(IDirectoryInfo directory) =>
    directory.EnumerateFiles()
        .Where(file => SupportedExtensions.Contains(file.Extension))
        .OrderByDescending(file => file.Length)
        .ThenBy(file => file.Name, StringComparer.Ordinal)
        .FirstOrDefault();
```

- `EnumerateFiles()` with no search option is the top level only. `SearchOption.AllDirectories` is never passed (Q8, FR-011).
- The allowlist holds `".mkv"`, `".mp4"`, `".avi"`, `".m4v"` and is constructed with `StringComparer.OrdinalIgnoreCase`, so matching is case-insensitive identically on every OS (Q6, FR-009). A `FrozenSet<string>` is the natural fit and is available on .NET 10; a `HashSet<string>` with the same comparer satisfies the ruling equally. The choice is mechanical, not structural.
- Ordering is size descending, then file name ascending under `StringComparer.Ordinal`. This is what makes selection independent of enumeration order and of culture-sensitive collation (Q7, FR-010). The ordinal tie-break is observable: for two equal-length files named `a.mkv` and `B.mkv`, ordinal order puts `B` first (`'B'` = 0x42 < `'a'` = 0x61), so `B.mkv` wins even though a culture-aware comparison would order `a` first.
- `null` result means no supported top-level video; the caller raises `InvalidOperationException` naming the folder (Q9, FR-013).

### 4. `ParseFolderName`

```csharp
[GeneratedRegex(@"^(?<title>.*?)\s*(?:\((?<year>[0-9]{4})\)|\[(?<year>[0-9]{4})\])$",
    RegexOptions.CultureInvariant)]
private static partial Regex FolderNamePattern();
```

`[0-9]`, not `\d` — brief §5.1 says ASCII digits explicitly, and `\d` is Unicode-aware by default (FR-006).

**Where the trim happens (D8):** `ParseFolderName` calls `Trim()` on `directory.Name` as its first statement, and one trimmed local is then used for **both** the regex match and the whole-name fallback — never the untrimmed name, and never a second trim further down. The pattern keeps `$` exactly as brief §5.1 gives it; trimming first removes any trailing newline a folder name could carry, so a `$` versus `\z` difference cannot be observed and needs no case of its own.

**Ordering rule that must not be inverted (FR-008):** validity is established *before* the title decision, and there are exactly three outcomes:

| Outcome | Condition | Result |
|---|---|---|
| (a) | the pattern does not match, or `ReleaseYear.TryCreate` rejects the candidate | `(whole trimmed folder name, null)` — any marker stays in the title |
| (b) | the year is valid and the text before the marker is blank | `(whole trimmed folder name, year)` — the marker stays in the title and the year is kept |
| (c) | the year is valid and the text before the marker is non-blank | `(preceding text, year)` — the marker is stripped |

`Old (1887)` takes (a) and yields title `Old (1887)` with a null year, not `Old`. `(2010)` takes (b) and yields title `(2010)` with year 2010.

Verified against the enumerated cases:

| Input (trimmed) | Pattern matches? | Result |
|---|---|---|
| `Inception (2010)` | yes, title `Inception` | `Inception` / 2010 |
| `The Matrix [1999]` | yes | `The Matrix` / 1999 |
| `The Lord of the Rings - The Fellowship (2001)` | yes | full title / 2001 |
| `Inception` | no | `Inception` / null |
| `Inception (2010) Remastered` | no (`$` fails) | whole name / null |
| `Inception (2010]` | no (alternative 1 needs `)`, alternative 2 needs `[`) | whole name / null |
| `Inception (201)` / `Inception (20100)` | no | whole name / null |
| `Old (1887)` / `Future (2032)` | yes, but `TryCreate` rejects | whole name incl. marker / null |
| `Old (1888)` / `Future (2031)` at now = 2026 | yes, accepted | `Old` / 1888, `Future` / 2031 |
| `(2010)` | yes, title group empty | `(2010)` / 2010 (year-only, marker kept) |
| `The.Matrix.1999` | no | whole name / null |
| `The_Matrix (1999)` | yes | `The_Matrix` / 1999 |
| `(٢٠١٠)` | no (non-ASCII digits) | whole name / null |

The two-alternative shape (parenthesised **or** bracketed) is what rejects mismatched brackets. A collapsed character-class form such as `[(\[]…[)\]]` would wrongly accept `(2010]`, so it must not be substituted.

*Implementation checkpoint:* the pattern reuses the group name `year` in both alternatives, which .NET permits but which the implementer should confirm on the first run. If the runtime rejects it, the equivalent form with two distinct group names (`yearParen`, `yearBracket`), reading whichever matched, satisfies FR-006 and FR-008 identically. This is a code shape, not a behaviour change, so it needs no ruling.

The helper is `private static` for a concrete reason: `LamuFlix.Infrastructure` declares **no** `InternalsVisibleTo` for the unit test project (only `LamuFlix.Web` and `LamuFlix.Worker` do), so an `internal` helper would not be directly reachable from `LamuFlix.UnitTests`. Brief §5.1 offers an `internal static` alternative "if Quill needs direct tests", but satisfying it would require editing `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` beyond the single package reference the frozen scope allows (§5.2, Q4). It is not needed: the folder name is the parser's only input, so every one of the 13 parsing cases is reachable through `Scan` with a one-file in-memory folder. Recorded as a limitation of the brief, surfaced at spec review, not silently taken.

### 5. Result mapping (Q10, FR-014)

`new MediaFormat(primary.Extension)` reuses the existing value object's normalisation: it trims, strips one leading dot and lowercases, so `.MKV` yields `mkv`. `primary.Length` is the 64-bit byte count. `folder` — the unchanged input `LibraryPath`, not `primary.FullName` and not `directory.FullName`.

### 6. `ScannedMovie` edit (Q2, FR-004)

```csharp
public sealed record ScannedMovie(
    LibraryPath Path,
    string Title,
    MediaFormat Format,
    ReleaseYear? Year,
    long SizeBytes);
```

Both new arguments are required and appended after `Format`. `ImportMovieFolderCommandHandler` reads only `Title`, `Path` and `Format` (`ImportMovieFolderCommandHandler.cs:19`) and is not edited; its behaviour is unchanged (FR-005).

The file carries a `// ReSharper disable NotAccessedPositionalProperty.Global` pair whose comment names "future media library scanner and filesystem adapters". This ticket is that consumer — but only of `Title`, `Path` and `Format`. `Year` and `SizeBytes` are read by nothing yet, because persisting them is out of scope (Q3), so InspectCode is expected to keep flagging them. Task T005 therefore runs InspectCode on the file after the edit and **keeps the existing pair** if any property is still flagged; it removes the pair only on a clean InspectCode result, and adds no new suppression either way. Deleting a disable that is still needed is a build failure; keeping an unneeded one is an InspectCode finding that the gate will name.

### 7. Package edits (Q4, Q12a, FR-015, FR-016)

`Directory.Packages.props` gains three `PackageVersion` entries: `System.IO.Abstractions` in the production block (lines 7-26), and `System.IO.Abstractions.TestingHelpers` plus `coverlet.collector` in the test block (lines 28-39). Under central package management a versionless `PackageReference` fails restore, so both halves are required (recon §14.1).

- `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`: `<PackageReference Include="System.IO.Abstractions" />`.
- `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`: `<PackageReference Include="System.IO.Abstractions.TestingHelpers" />` and `<PackageReference Include="coverlet.collector"><PrivateAssets>all</PrivateAssets><IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets></PackageReference>`.

The three versions are chosen at implementation time and recorded in the receipt, not asserted here (Q4, Q12a). `IsTestingPlatformApplication` stays `false` and the visual-studio runner stays (recon §14.2).

## Test Strategy

Every test builds a **fresh** `MockFileSystem` — never a static, shared or reused instance, because shared mutable in-memory state is what makes a filesystem mock leak between cases — constructs the scanner with that filesystem and a `FixedTimeProvider` at **2026-06-01 UTC** (making the acceptable range 1888-2031), scans one folder, and asserts either the complete result — `Path`, `Title`, `Format`, `Year`, `SizeBytes` — or the exact exception type. Only the immutable `FixedTimeProvider` may be shared. No temp directory, no machine path, no real file (AC "no physical disk requirements", FR-002, SC-004).

- **Clock stub**: the existing `tests/LamuFlix.UnitTests/Features/FixedTimeProvider.cs` (namespace `LamuFlix.UnitTests.Features`), reused by `using`. brief §5.3 offers `FakeTimeProvider` **or** a fixed stub; `FakeTimeProvider` lives in `Microsoft.Extensions.Time.Testing`, which no project in this repository references, so taking it would be a new dependency (§2.3 item 1) outside the frozen scope. The existing stub is the branch taken and needs nothing new (spec Assumptions).
- **Layout**: `tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs`, split into parsing and selection files only if readability forces it — an explicit writer's choice in brief §5.2.
- **Assertion style**: Shouldly; `[Theory]` with `[MemberData]` over repeated `[Fact]`s wherever the cases are one row each (`constitution.md:293`, `:302`).
- **Test names**: `Scan_<Condition>_<Expected>`, the house form's counterpart — `ImportMovieFolderCommandHandlerTests.cs:27` uses `HandleAsync_<Scenario>_<Expected>`. For example `Scan_ParenthesisedYear_ReturnsTitleAndYear`.
- **Comments**: every new test carries exactly the three AAA headers — `// arrange`, `// act`, `// assert`, one per phase — and **no other comment of any kind**. AAA test headers are the one exemption `AGENTS.md` permits, and every existing test in `ImportMovieFolderCommandHandlerTests.cs` already follows it (lines 29/37/40 and onward).
- **Case coverage**: all 24 cases enumerated in brief §5.3, grouped exactly as that section groups them (parsing 1-13, selection/format/size 14-20, missing/nested 21-24). The whitespace-trimmed folder name row is conditional on the in-memory filesystem being able to represent such a name; if it cannot, the row is dropped and the drop is recorded in the implementation receipt.
- **Pre-existing tests**: `ImportMovieFolderCommandHandlerTests` stays green with only its four constructor calls edited. `ArchitectureTests` stays green with **no** edit — `IMediaLibraryScanner` is already in the port whitelist (`ArchitectureTests.cs:28-29`, recon §10).
- **No new property test**: `ScannedMovie` is a record with no logic, and the parse/selection rules are not FsCheck-shaped domain invariants. The property-test gate is therefore expected to report no tagged tests; see Gates.

## Gates

Run after every `.cs` edit, per `constitution.md:375` and `:380-388`:

- `./scripts/run-roslyn-analyzers.ps1` — zero warnings.
- `./scripts/run-cyclomatic-complexity.ps1` — no method over 15. The refactor ceiling is a **separate, explicit run** in the close-out pass: the same script with the same `-Files` array plus `-Threshold 6` (see Close-out gates).
- `./scripts/run-jetbrains-inspectcode.ps1` — zero issues at WARNING or higher.
- `dotnet format --verify-no-changes` — clean.

**`-Files` gotcha** (recon §11): a single comma-joined string silently collapses to one checked file. Pass real array args, for example:

```powershell
pwsh -Command "& './scripts/run-roslyn-analyzers.ps1' -Files 'src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs','tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs','src/LamuFlix.Core/Ports/ScannedMovie.cs'"
```

A gate that cannot run is reported **"Could not run"**, never PASS or SKIP-green (`AGENTS.md`).

### Coverage (Q12, Q12a, FR-017) — the ticket's own acceptance evidence

From the worktree root:

```powershell
dotnet test tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj --collect 'XPlat Code Coverage' --results-directory <fresh temp dir outside the repo>
```

**The no-op trap is the first thing to check** (recon §14.3 Run A): before the collector is referenced, this exact command exits **0**, reports all tests green, and emits nothing at all. A green exit code here is not evidence. A valid receipt requires **both** a successful run **and** a `coverage.cobertura.xml` emitted by that same invocation, located under that run's GUID-scoped results subfolder. A stale report is never acceptable, and no coverage output is ever written into the worktree.

Scope: every report class for `LamuFlix.Infrastructure.FileSystem.DirectoryMediaLibraryScanner`, plus any helper added for it and every compiler-generated nested class, closure or source-generated regex helper attributable to it — the class is `partial`, so the regex helper is emitted into it and must be in scope.

Pass condition, per class: `line-rate="1"` **and** `branch-rate="1"`, and underneath those strings every `<line>` has `hits > 0` and every branch line's `condition-coverage` reads `100% (n/n)`. Rounded rate strings alone are not proof. The scanner must report a non-empty set of executable lines — a class reporting zero executable lines has not demonstrated coverage, and that is recorded as such rather than passed. No `[ExcludeFromCodeCoverage]` attribute and no report filter may hide in-scope code.

The receipt — command, exit code, report path and the in-scope class rows — is pasted into the implementation pull request. CI is unchanged (Q12a).

### Close-out gates

The first block is one pass over **every** changed `.cs` file, run after the coverage read (brief §5.5 item 6, D7). It runs the three analyzer gates once, with `-Files` as a real array covering the scanner, the test file(s), `src/LamuFlix.Core/Ports/ScannedMovie.cs` and `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs`. The complexity gate runs twice: once at the ceiling of 15, then again with `-Threshold 6` for the refactor ceiling.

```powershell
$changed = @(
  'src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs',
  'tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs',
  'src/LamuFlix.Core/Ports/ScannedMovie.cs',
  'tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs'
)
& ./scripts/run-roslyn-analyzers.ps1 -Files $changed          # exit 0
& ./scripts/run-cyclomatic-complexity.ps1 -Files $changed    # exit 0 at 15
& ./scripts/run-cyclomatic-complexity.ps1 -Files $changed -Threshold 6   # exit 0 at 6
& ./scripts/run-jetbrains-inspectcode.ps1 -Files $changed    # zero issues
```

The call operator, not `pwsh -File`: a `-File` launch hands `$changed` over as separate native string arguments, so `-Files` would bind only the first path and the remaining paths would fall through — the same collapse as recon §11's comma-joined string. If the §5.2 test-file split is taken, the second entry becomes the two split files and the array holds five paths.

Then, in order:

- Full `dotnet test` — green.
- `dotnet format --verify-no-changes` — clean.
- `./scripts/run-vulnerable-packages.ps1` — two new packages are in the graph, so this gate is now materially relevant to this ticket. Not named in brief §5.4; carried from `AGENTS.md` and `harness.yml:23-25` (`fail: true`, `includeTransitive: true`). Surfaced at spec review and confirmed in scope by D2.
- `./scripts/run-property-tests.ps1` — expected to exit **2** (no tagged tests). Per `AGENTS.md` a scope-empty skip is blocking and never green; per `harness.yml:30-34` a ticket with no domain invariants records its opt-out in its task note and the pipeline then accepts that exit **for this ticket only**. That note is a task. Not named in brief §5.4; surfaced at spec review and confirmed in scope by D2.
- `pwsh -NoProfile -File ./scripts/run-mutation.ps1` — Stryker keeps its Core-only scope with no configuration change (Q12). `ScannedMovie` is in Core but carries no logic, so it is not expected to produce survivors. No threshold moves.
- `git diff --stat origin/main...HEAD` — only the seven files listed under Project Structure, or eight if the test file is split per brief §5.2.

## Coverage of the Brief's Closing Bar

| Closing-bar item | Where satisfied |
|---|---|
| 1. Scanner implements `IMediaLibraryScanner.Scan(LibraryPath)`, filesystem only through injected `IFileSystem` | Design 1-3, FR-001, FR-002 |
| 2. `ScannedMovie` carries `ReleaseYear? Year` and `long SizeBytes`, filled per Q5/Q7/Q10 | Design 5-6, FR-004, FR-008, FR-010, FR-014 |
| 3. Parsing, allowlist, selection, top-level-only, both exceptions behave per Q5-Q9 | Design 3-5, FR-006 to FR-013 |
| 4. Suite covers §5.3 and reaches 100% line **and** branch coverage, measured per §5.4 | Test Strategy, Gates -> Coverage, FR-017, SC-001 to SC-003 |
| 5. Four existing construction sites compile; assertions otherwise unchanged | Design 6, FR-005, SC-005 |
| 6. All §5.4 gates pass; full `dotnet test` green; format clean | Gates, FR-020, SC-006 |