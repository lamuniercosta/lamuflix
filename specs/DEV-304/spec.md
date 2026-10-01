# Feature Specification: DirectoryMediaLibraryScanner

**Feature Branch**: `feature/304-spec`

**Created**: 2026-10-01

**Status**: gate1: provisional (Gate 1 closed pending user merge; no Phase B authorization)

**Input**: Phase A grill outcome (DEV-304, parent DEV-283, size M, UI false); `brief.md`, `CONCLUSIONS.md` (Q1-Q12, Q12a), `ASSUMPTIONS.md`, recon-DEV-304, `specs/PRODUCT.md`

## User Scenarios & Testing

### User Story 1 - A scan result carries the year and the size it extracted (Priority: P1)

As application code consuming `IMediaLibraryScanner.Scan`, I need the returned result to carry the release year parsed from the folder name and the byte size of the primary file, so nothing the scanner extracts has to be thrown away or re-read from disk.

**Why this priority**: This is the contract half of the ticket's scope item 1. Without it, the parsing and selection stories have nowhere to deliver their results, and the ticket's "parse ReleaseYear / extract file size" scope would be satisfied only by discarding the data.

**Independent Test**: Scan an in-memory folder named `Inception (2010)` containing `Inception (2010).mkv` and assert the **full** `ScannedMovie` — `Path`, `Title`, `Format`, `Year` and `SizeBytes` (brief §5.3 case 1, written as T006); no standalone record-construction test is added, because a record with no logic gains nothing from one. The four pre-existing construction sites in `ImportMovieFolderCommandHandlerTests` (lines 32, 58, 79, 100) compile against the new shape with their assertions otherwise unchanged (T005), and that class stays green.

**Acceptance Scenarios**:

1. **Given** a library folder whose name is `Inception (2010)` containing `Inception (2010).mkv`, **When** it is scanned, **Then** the result's `Title` is `Inception`, its `Year` is the release year 2010, its `Format` is `mkv`, its `SizeBytes` equals the file's length, and its `Path` is the folder that was passed in.
2. **Given** a library folder with no year marker, **When** it is scanned, **Then** `Year` is null and `Title` is the whole trimmed folder name.
3. **Given** an existing consumer of the scan result that reads only `Title`, `Path` and `Format`, **When** the import flow runs, **Then** its behaviour is unchanged.

---

### User Story 2 - A folder name becomes a title and a release year (Priority: P1)

As the library owner, I need my folder naming conventions to be understood — parentheses, brackets, multi-word titles, punctuation — so my movies are catalogued with the title and year I named them with.

**Why this priority**: Folder-name parsing is the first half of the ticket's scope item 1 and is what makes a scan useful at all.

**Independent Test**: Thirteen in-memory folder layouts (brief §5.3 cases 1-13 for this story) scanned through `Scan` with a fixed clock; each asserts the full `ScannedMovie` — `Path`, `Title`, `Format`, `Year` and `SizeBytes` — not only the parsing fields. No physical disk.

**Acceptance Scenarios**:

1. **Given** `Inception (2010)` or `The Matrix [1999]`, **When** it is scanned, **Then** `Title` is the trimmed name without the marker and `Year` is 2010 or 1999 respectively.
2. **Given** a multi-word name with internal spaces and punctuation, such as `The Lord of the Rings - The Fellowship (2001)`, **When** it is scanned, **Then** that whole title is preserved and `Year` is 2001.
3. **Given** no marker, a non-trailing marker (`Inception (2010) Remastered`), mismatched brackets (`Inception (2010]`), or a marker that is not exactly four digits (`Inception (201)`, `Inception (20100)`), **When** it is scanned, **Then** `Year` is null and `Title` is the whole trimmed folder name.
4. **Given** a syntactically valid but out-of-range year, such as `Old (1887)` or `Future (2032)` against a 2026 clock, **When** it is scanned, **Then** `Year` is null and the marker is **not** stripped — `Title` is `Old (1887)` and `Future (2032)` respectively.
5. **Given** the boundary years 1888 and the clock's year plus five, **When** they are scanned, **Then** they are accepted as valid release years.
6. **Given** a year-only name such as `(2010)`, **When** it is scanned, **Then** `Year` is 2010 and `Title` is the whole trimmed name `(2010)`.
7. **Given** a name whose dots or underscores look like a year marker, such as `The.Matrix.1999` or `The_Matrix (1999)`, **When** it is scanned, **Then** dots and underscores are treated as ordinary characters: the first yields null year and the whole name, the second yields `Title` `The_Matrix` and year 1999.
8. **Given** a marker containing non-ASCII digits, such as `(٢٠١٠)`, **When** it is scanned, **Then** it is not a year marker.

---

### User Story 3 - The right file is chosen as the movie's primary video (Priority: P1)

As the library owner, I need the scanner to pick one definite video out of whatever is in the folder, so a scan always resolves to the same file regardless of how the disk enumerates or how my operating system treats file-name case.

**Why this priority**: Primary-file selection is the second half of scope item 1 and is the difference between a usable scan and an arbitrary one.

**Independent Test**: In-memory folders holding combinations of supported and unsupported files (brief §5.3 cases 14-20); each asserts the full `ScannedMovie` — `Path`, `Title`, `Format`, `Year` and `SizeBytes` — not only the selection fields, and the unsupported-only case asserts the exact exception type instead.

**Acceptance Scenarios**:

1. **Given** a folder whose only video is `.mkv`, `.mp4`, `.avi` or `.m4v`, **When** it is scanned, **Then** that file is selected and `Format` is the lower-case extension without the dot.
2. **Given** an extension in upper or mixed case, such as `MOVIE.MKV` or `Movie.Mp4`, **When** it is scanned, **Then** it is still recognised as supported and `Format` is normalised to lower case.
3. **Given** only unsupported files (`.srt`, `.nfo`, `.txt`, `.wmv`), **When** the folder is scanned, **Then** the scan fails rather than inventing a result.
4. **Given** supported and unsupported files where the unsupported one is larger, **When** the folder is scanned, **Then** the supported file is selected.
5. **Given** several supported files, **When** the folder is scanned, **Then** the largest by byte length is selected and `SizeBytes` equals its length.
6. **Given** two supported files of identical size, **When** the folder is scanned, **Then** the one whose name sorts first under ordinal ordering wins — including a pair where ordinal and culture ordering disagree.
7. **Given** a selected file, **When** the result is inspected, **Then** `Path` is the library folder that was scanned, not the file's own path.

---

### User Story 4 - An unscanable folder fails with a specific, actionable reason (Priority: P1)

As application code, I need a scan of an impossible folder to tell me **which** problem occurred, so I can distinguish a mistyped library location from a folder that holds no playable video, without inspecting the disk myself.

**Why this priority**: The port returns a non-nullable result with no error representation, so the failure mode is the only signal available. Deciding it was a ruling (Q9), not an implementation detail.

**Independent Test**: In-memory layouts for a missing folder, an empty folder, a folder whose only video is in a subfolder, and a folder with a top-level video plus a larger nested one (brief §5.3 cases 21-24); each asserts the exact exception type raised.

**Acceptance Scenarios**:

1. **Given** a library path that does not exist, **When** it is scanned, **Then** a directory-not-found error naming that folder is raised.
2. **Given** a folder that exists but is empty, **When** it is scanned, **Then** an invalid-operation error naming that folder is raised.
3. **Given** a folder whose only video sits in a subfolder, **When** it is scanned, **Then** the nested video is ignored and the same invalid-operation error is raised.
4. **Given** a top-level video and a larger video in a subfolder, **When** the folder is scanned, **Then** the top-level video is selected and no recursion occurred.
5. **Given** any other I/O failure while reading the folder, **When** it is scanned, **Then** that failure propagates unchanged rather than being remapped. **Verified by T021 inspection, not by a test** (D9): the scanner contains no `try`, `catch` or `when` clause, so it cannot remap anything, and a test would need an `IFileSystem` double beyond `MockFileSystem`, which this story's in-memory boundary does not call for.

---

### User Story 5 - The whole filesystem boundary is exercised in memory and measured to completion (Priority: P2)

As the team, I need the scanner's entire behaviour proven by a suite that never touches a physical disk, and I need the coverage claim backed by a real report rather than by "the tests passed".

**Why this priority**: This is a ticket acceptance criterion in its own right, and it is the only way to show that later refactors cannot quietly drop a branch.

**Independent Test**: Run the coverage command from the worktree root and read the emitted report: every in-scope class, including compiler-generated helpers attributable to the scanner, must show a full line rate, a full branch rate, no line with zero hits and no branch below 100%. A stale report is not acceptable proof.

**Acceptance Scenarios**:

1. **Given** the scan test suite, **When** it runs, **Then** no physical directory is read or created, no temporary path is used, and the only filesystem access is through the injected abstraction.
2. **Given** the coverage command run against the test project, **When** it completes, **Then** it reports success **and** emits a coverage report in that run's own results directory; a command that reports success without emitting a report is treated as a failure.
3. **Given** the emitted report, **When** the scanner and its helpers are inspected, **Then** each reports a complete line rate and branch rate with no zero-hit line and no partially-covered branch.
4. **Given** the repository before and after the run, **When** the change set is inspected, **Then** the coverage output was written outside the repository and no coverage exclusion hides scanner code.
5. **Given** the existing architecture test suite, **When** it runs, **Then** it passes with no edit, because the port it guards was already whitelisted.

---

### Edge Cases

- **Year marker position**: the marker must be **trailing**. `Inception (2010) Remastered` has a marker-shaped substring but no marker, so it yields the whole name and a null year.
- **Mismatched brackets**: `(2010]` is not a marker; mixing the opening and closing bracket yields the whole name and a null year.
- **Digit count**: `(201)` and `(20100)` are not markers. The marker is exactly four ASCII digits — a Unicode digit run of the same length is also not a marker, which is what proves the pattern is ASCII-only.
- **Range, both ends**: 1887 and the clock's year plus six are rejected; 1888 and the clock's year plus five are accepted. The accepted range comes from the existing release-year value object and is not duplicated in the scanner.
- **Validity before stripping**: a syntactically valid but out-of-range year leaves the marker in place — `Old (1887)` keeps its marker in the title. Stripping first and validating afterwards would produce `Old`, which the rulings forbid.
- **Year-only name**: `(2010)` yields the year 2010 *and* the title `(2010)`; the year is kept because there is simply nothing left to strip.
- **Dots and underscores are literal**: `The.Matrix.1999` is not a year marker. Underscores inside a title survive intact.
- **Case**: extension matching ignores case on every operating system, so the same library scans identically on Windows and Linux.
- **Tie-break determinism**: two equally large videos resolve by ordinal file-name order, not by enumeration order and not by culture-sensitive collation. This is what keeps a scan reproducible across operating systems.
- **Nesting**: a subfolder video never qualifies, even when the top level has no video at all.
- **Surrounding whitespace in a folder name**: trimmed before parsing, if the in-memory filesystem can represent such a name at all; brief §5.3 makes that row conditional and requires the drop to be recorded if it cannot be represented.
- **Other I/O errors**: an unexpected I/O failure mid-scan is not converted into one of the two defined failures; it surfaces as itself.

## Requirements

### Functional Requirements

- **FR-001** (AC1, Q11, US1-US4): `public sealed partial class DirectoryMediaLibraryScanner(IFileSystem fileSystem, TimeProvider timeProvider) : IMediaLibraryScanner` exists at `src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs` in namespace `LamuFlix.Infrastructure.FileSystem` and implements `ScannedMovie Scan(LibraryPath folder)`. The class is `partial` because the folder-name pattern is a source-generated regex. The `IMediaLibraryScanner` port signature and its non-nullable return are unchanged (Q9).
- **FR-002** (AC1, Q11, §6, US5): The scanner reaches the filesystem **only** through the injected `IFileSystem`. It contains no direct `System.IO.File`, `System.IO.Directory` or disk-path call, and never constructs a default `FileSystem`. `IFileSystem` is the single filesystem boundary.
- **FR-003** (Q5, Q11, §6): The scanner samples the injected clock **exactly once** per `Scan` and passes that single value to the year validation. It uses no ambient current-time call (the `DateTime.Now`/`DateTimeOffset.Now` family is a build-breaking banned symbol).
- **FR-004** (Q2, US1): `ScannedMovie` becomes `ScannedMovie(LibraryPath Path, string Title, MediaFormat Format, ReleaseYear? Year, long SizeBytes)` in `src/LamuFlix.Core/Ports/ScannedMovie.cs`, with `Year` and `SizeBytes` as **required** positional arguments appended after `Format`.
- **FR-005** (Q2, US1): The four `ScannedMovie` construction sites in `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs` (lines 32, 58, 79, 100) are updated to the new shape and their assertions are otherwise unchanged. `ImportMovieFolderCommandHandler` behaviour is unchanged: it still reads only `Title`, `Path` and `Format`.
- **FR-006** (Q5, US2): A year marker is a **trailing** `(YYYY)` or `[YYYY]` holding exactly four ASCII digits with matching brackets, at the end of the trimmed final folder name. Matching uses one compiled source-generated regex, anchored at the end and culture-invariant, whose digit class is the ASCII range rather than the Unicode digit shorthand.
- **FR-007** (Q5, US2): A candidate year is accepted **only** through the existing release-year value object's `TryCreate` factory against the single sampled clock value. The scanner duplicates no range constants.
- **FR-008** (Q5, US2): The candidate year is validated **before** any stripping decision, and there are exactly three outcomes. (a) The pattern does not match, or `ReleaseYear.TryCreate` rejects the candidate: `Year` is null and `Title` is the whole trimmed folder name — no marker, malformed marker, mismatched brackets and wrong digit count all land here, and any marker stays in the title. (b) The year is valid and the trimmed text before it is blank — a year-only name: `Year` is the valid year and `Title` is the whole trimmed folder name, marker kept. (c) The year is valid and the text before it is non-blank: the marker is stripped and `Title` is that preceding text. Dots and underscores remain literal and multi-word title content is preserved.
- **FR-009** (Q6, US3): Supported video extensions are `.mkv`, `.mp4`, `.avi` and `.m4v`, held in a scanner-private static allowlist matched case-insensitively under ordinal comparison on every operating system. No new domain value object is introduced. `Format` is built with the existing media-format value object from the selected file's extension, reusing its normalisation.
- **FR-010** (Q7, US3): The primary file is the **largest** supported top-level file by byte length. Equal lengths are broken by file name ascending under ordinal comparison, so selection never depends on enumeration order or culture-sensitive collation.
- **FR-011** (Q8, US3): Enumeration is **top-level only**. The scanner never requests a recursive or all-directories search option, so a video in a subfolder does not qualify.
- **FR-012** (Q9, US4): A library folder whose existence check fails through `IFileSystem` raises `System.IO.DirectoryNotFoundException` naming that folder.
- **FR-013** (Q9, US4): An existing folder with no supported top-level video raises `System.InvalidOperationException` naming that folder. No custom exception type is introduced, and the port stays non-nullable. Any other I/O error propagates unchanged — verified by T021 inspection, which greps the scanner for `try`, `catch` and `when` clauses and expects zero hits (D9), rather than by a new test.
- **FR-014** (Q10, US3): `SizeBytes` is the selected file's reported length as a 64-bit byte count; `Format` is its normalised extension; `Path` is the unchanged library folder that was scanned, not the selected file's own path.
- **FR-015** (Q4, US1-US5): `System.IO.Abstractions` is referenced by `LamuFlix.Infrastructure` and `System.IO.Abstractions.TestingHelpers` by `LamuFlix.UnitTests`, each pinned by a central version entry to the latest stable mutually compatible releases **verified at selection time**. No new project reference is added — the unit test project already references Infrastructure.
- **FR-016** (Q12a, US5): `coverlet.collector` is a **private, test-only** reference in `LamuFlix.UnitTests` (private assets plus the standard include-assets item), with its version pinned centrally to the latest stable release verified against the existing test SDK 17.12.0 and against a real .NET 10 test run. The test platform, runsettings files, CI workflows, report tooling and all gate thresholds are unchanged.
- **FR-017** (Q12, AC2, US2-US4): The in-memory suite covers every case enumerated in brief §5.3 and reaches **100% line and branch coverage** on the scanner and every helper added for it. The proof is a coverage report freshly emitted by the ticket's own measurement command; passing tests alone do not evidence the figure. No coverage exclusion attribute or filter may hide in-scope code.
- **FR-018** (Q1, AC2, US5): The full new suite is delivered in `tests/LamuFlix.UnitTests/FileSystem/`. Nothing in the legacy test project is deleted or rewritten; the ticket's clause about replacing legacy disk-bound tests is satisfied because no such legacy tests exist, and that recon fact is recorded on the ticket by Rigger.
- **FR-019** (Q3, Q11, §2, US5): Nothing outside the frozen scope changes: no persistence shape, entity mapping, migration, aggregate factory or handler behaviour; no dependency-injection registration, service-collection extension, host wiring, health check or telemetry constant; no HTTP endpoint, DTO, API contract document or `/web` change; no new project or architectural layer.
- **FR-020** (AC, §5.4, US5): The Roslyn analyzer gate, the cyclomatic-complexity gate (ceiling 15, refactor ceiling 6), the InspectCode gate, the format check and the full test suite all exit 0. The vulnerable-packages gate exits 0, which is now materially relevant because two new packages enter the graph. The property-test gate exits **2** — no tagged tests, since the parse and selection rules are not FsCheck-shaped domain invariants — and DEV-304's opt-out from it is recorded in its task note and in the PR body per `harness.yml:30-34`; the pipeline accepts that exit for this ticket only, and no threshold moves to clear it. The mutation gate keeps its existing Core-only scope and configuration. Gate thresholds, the harness configuration and the mutation configuration are not edited. A gate that could not run is reported as "Could not run", never as a pass.

### Key Entities

- **DirectoryMediaLibraryScanner**: the single-folder filesystem adapter. Holds the injected filesystem abstraction and clock, samples the clock once per scan, enumerates one level, selects one primary video, parses one title and year, and returns one result. Carries the private extension allowlist.
- **ScannedMovie**: the port record returned by a scan. Carries the scanned library folder path, the parsed title, the media format, an optional release year and a 64-bit size in bytes. This is the only contract change.
- **Supported video extension allowlist**: a scanner-private set of four extensions. Deliberately not a domain value object, because a closed set of values would otherwise become a new project-owned type.
- **ReleaseYear**: the existing domain value object that owns the acceptable year range. The scanner asks it whether a candidate year is valid and never restates the range.
- **LibraryPath**: the existing domain value object identifying the scanned folder; it is both the scanner's input and the result's `Path`.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All 24 enumerated cases in brief §5.3 pass; every parsing and selection case asserts the complete returned result, and every failure case asserts the exact exception type.
- **SC-002**: In the report emitted by the ticket's measurement command, 0 of the in-scope classes report a line rate or branch rate below complete, 0 in-scope lines are reported with zero hits, and 0 in-scope branch lines are reported below 100% of their conditions — checked against the underlying counts, not against rounded rate strings.
- **SC-003**: The measurement command exits 0 **and** emits its own report in that run's results directory; the receipt records the command, its exit code, the report path and the in-scope class rows. Zero stale reports are reused and zero coverage output lands in the repository.
- **SC-004**: The suite performs 0 physical-disk reads or writes, creates 0 temporary directories and uses 0 machine-specific paths; the scanner contains 0 direct references to the concrete filesystem types.
- **SC-005**: The 4 pre-existing construction sites compile with assertions otherwise unchanged; the import handler test class and the architecture test suite are both green with no edit to the architecture tests.
- **SC-006**: Every gate in §5.4 exits 0; 0 files change outside the frozen scope; 0 gate thresholds, harness settings or mutation settings are edited.
- **SC-007**: 0 unexplained surviving mutants on `ScannedMovie`, which carries no logic of its own.

## Assumptions

- **Carried `[assumed]` rulings** (full basis in `CONCLUSIONS.md`, restated in `ASSUMPTIONS.md`): Q5 the trailing four-digit marker, the ASCII-only digit class, validity before stripping, and literal dots and underscores; Q6 the scanner-private four-extension allowlist with case-insensitive ordinal matching; Q7 largest-by-bytes selection with an ordinal name tie-break; Q8 top-level-only enumeration.
- **`[assumed]` Failure message wording**: both defined failures name the offending library folder, and the exact wording is ordinary copy settled at implementation time. `brief.md` fixes the exception types and the fact that the folder is named; it does not fix the string.
- **`[assumed]` Clock stub for tests**: the suite reuses the existing fixed-clock stub in the unit test project. The alternative named in brief §5.3, `FakeTimeProvider`, would require a package this repository does not reference, which is a new dependency and therefore outside the frozen scope. The fixed stub is the other option brief §5.3 offers and needs nothing new.
- **`[assumed]` Fixed test instant**: 2026-06-01 UTC, which makes the acceptable year range 1888-2031 and lets the boundary rows for 1888 and 2031 both be asserted (brief §5.3).
- **`[assumed]` Test file layout**: one scanner test file in the new test folder, split into parsing and selection files only if readability forces it. brief §5.2 leaves this choice to the writer.
- **`[assumed]` Conditional test row**: the folder-name-whitespace case is kept only if the in-memory filesystem can represent such a name; if it cannot, the row is dropped and the drop is recorded in the implementation receipt rather than silently omitted (brief §5.3).
- **Measurement command**: brief §5.4 fixes the command that proves SC-002 and SC-003, including the trap that the same command reports success while emitting nothing when the collector is absent. A receipt that shows success without an emitted report is not a receipt.
- **Gates the brief's §5.4 list does not name**: the property-test gate and the vulnerable-packages gate are still configured for this repository and two new packages are added here, so both run in the close-out. The property-test gate is expected to report that no tagged tests exist, which is recorded as this ticket's opt-out in its task note per the repository's own configuration. Recorded here so the omission is visible at spec review rather than discovered at the gate.
- **Out of scope** (restated for the reader): persistence of the scanned year and size, and the follow-up ticket that would do it; dependency-injection composition and its follow-up; custom exception types; a new domain type for the allowlist; anything in the legacy test project; the coverage report format beyond the one named; CI coverage reporting.
- **Owed by Rigger**: a comment on DEV-304 recording that no legacy disk-bound scanner tests exist, per Q1. No follow-up ticket is filed for persistence (Q3) or composition (Q11); both are noted only.
