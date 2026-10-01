# Tasks: DirectoryMediaLibraryScanner

**Input**: `specs/DEV-304/` — spec.md, plan.md, brief.md (task ordering per brief §5.5), CONCLUSIONS.md (Q1-Q12, Q12a), ASSUMPTIONS.md, recon-DEV-304

**Tests**: Required. The ticket's acceptance criteria are a MockFileSystem suite and a measured coverage figure, so every behaviour task writes its test first and watches it fail.

**Gate step after every `.cs` task** (plan §Gates, constitution.md:375/:380-388): Roslyn analyzers, cyclomatic complexity, InspectCode, then `dotnet format --verify-no-changes`. This is the incremental per-edit pass; the single close-out pass over every changed `.cs` file, which also runs complexity at `-Threshold 6`, is T025. Pass `-Files` as real array args, not one comma-joined string (recon §11):

```powershell
pwsh -Command "& './scripts/run-roslyn-analyzers.ps1' -Files 'src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs','tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs'"
```

**Note on the brief's ordering (§5.5).** Item 5 lists "Scan composition" after the parsing (item 3) and selection (item 4) tasks, while §5.3 exercises all 24 cases through `Scan` and §5.1 makes the helpers `private static`. This plan reads item 5's "Scan composition" as the guard clauses and result mapping (which genuinely depend on items 3 and 4), with the minimal `Scan` shell needed to reach the private helpers landing with items 3 and 4. Item 5's dependency direction is preserved. Flagged for Keel at spec review rather than taken silently.

---

## Phase 1: Setup — packages (T001-T004)

**Purpose**: `IFileSystem` and the in-memory filesystem must exist before any scanner code compiles, and the coverage collector must exist before the ticket's own acceptance evidence can be produced (brief §5.5 item 1).

**Critical**: this phase must build before anything else. Versions are latest stable, mutually compatible, verified at selection time — no ruling asserts a version (Q4, Q12a). Record the three chosen versions in the implementation receipt.

- [ ] T001 Add three `PackageVersion` entries to `Directory.Packages.props`: `System.IO.Abstractions` in the production block (lines 7-26), `System.IO.Abstractions.TestingHelpers` and `coverlet.collector` in the test block (lines 28-39). Under central package management a versionless `PackageReference` fails restore, so both halves are required (recon §14.1)
- [ ] T002 [P] Add `<PackageReference Include="System.IO.Abstractions" />` to `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`. No `ProjectReference` is added anywhere: `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj:30-33` already references Infrastructure (Q4)
- [ ] T003 [P] Add `<PackageReference Include="System.IO.Abstractions.TestingHelpers" />` and `<PackageReference Include="coverlet.collector"><PrivateAssets>all</PrivateAssets><IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets></PackageReference>` to `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`. Leave `IsTestingPlatformApplication` false and the visual-studio runner in place (Q12a, recon §14.2)
- [ ] T004 Build the solution, then run the plan's coverage command into a fresh temp directory outside the repo and **confirm that same invocation emits `coverage.cobertura.xml`**. Without the collector this command exits 0 and emits nothing at all (recon §14.3 Run A). If it exits 0 with no file, T001-T003 have not delivered the collector: stop and return to Keel rather than continuing

**Checkpoint**: the scanner's filesystem boundary exists, the in-memory filesystem exists, and coverage measurement is real.

---

## Phase 2: Foundational — the contract change (T005)

**Purpose**: extend the one Core port record the scanner delivers into, atomically with its call sites.

**Critical**: brief §5.5 item 2 requires these in **one** task so the solution never sits red between them.

- [ ] T005 [US1] Append required positional arguments `ReleaseYear? Year, long SizeBytes` to the record in `src/LamuFlix.Core/Ports/ScannedMovie.cs` (after `Format`), **and in the same task** update the four construction sites at lines 32, 58, 79 and 100 of `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs` — at **each** of the four, append exactly `, null, 1024L` to the existing `new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "Imported", new MediaFormat("mkv"))` call, so the call reads `new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "Imported", new MediaFormat("mkv"), null, 1024L)`; change no existing argument and no assertion. Then run InspectCode on the Core file: keep the existing `// ReSharper disable NotAccessedPositionalProperty.Global` pair if `Year` or `SizeBytes` is still flagged (expected — the handler reads only `Title`, `Path`, `Format`), remove the pair only on a clean result, and add no new suppression either way (§5.1). Full `dotnet test` green. `ImportMovieFolderCommandHandler.cs` is **not** edited.

**Checkpoint**: the five-field result exists and every existing consumer compiles.

---

## Phase 3: User Story 1 - A scan returns the complete result (Priority: P1) MVP

**Goal**: scanning a library folder produces a `ScannedMovie` with title, year, format, size and path — the ticket's headline outcome.

**Independent Test**: scan `Inception (2010)` containing `Inception (2010).mkv` through an in-memory filesystem with a fixed clock and assert all five fields; scan a markerless folder and assert `Year` is null and `Title` is the whole name; `ImportMovieFolderCommandHandlerTests` stays green.

- [ ] T006 [P] [US1] Create `tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs` with the shared arrangement: a `MockFileSystem`, a `FixedTimeProvider` at 2026-06-01 UTC (making the acceptable range 1888-2031), and the scanner under construction. **Each test case builds its own fresh `MockFileSystem`** — no static, shared or reused instance anywhere in the file; only the immutable `FixedTimeProvider` may be shared. Name every test `Scan_<Condition>_<Expected>`, e.g. `Scan_ParenthesisedYear_ReturnsTitleAndYear`. Give every test the three AAA headers `// arrange`, `// act`, `// assert` — one per phase — and **no other comment of any kind**. First two cases: §5.3 case 1 and case 4. Reuse the existing `tests/LamuFlix.UnitTests/Features/FixedTimeProvider.cs`; do **not** add `FakeTimeProvider` (it needs a package this repository does not reference). Run them — the red must be the missing type, not a passing test
- [ ] T007 [US1] Create `src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs`: `public sealed partial class DirectoryMediaLibraryScanner(IFileSystem fileSystem, TimeProvider timeProvider) : IMediaLibraryScanner` in namespace `LamuFlix.Infrastructure.FileSystem`. `Scan` reads existence through `fileSystem.DirectoryInfo.New(folder.Value)`, samples `timeProvider.GetUtcNow()` exactly once, parses the folder name, and returns `new ScannedMovie(folder, title, new MediaFormat(primary.Extension), year, primary.Length)`. Only an injected `IFileSystem`; no `System.IO.File`/`Directory`/disk-path call, no `new FileSystem()`. Selection may take the single top-level supported file for now — **do not merge this phase alone**, T013 replaces the pick
- [ ] T008 [US1] Gates on both changed `.cs` files, then the filtered test run green

**Checkpoint**: one folder scans end to end and returns all five fields.

---

## Phase 4: User Story 2 - Folder name becomes title and year (Priority: P1)

**Goal**: the naming conventions in brief §5.3 cases 1-13 parse exactly as ruled.

**Independent Test**: the 13 parsing cases through `Scan`, each asserting the full `ScannedMovie` — `Path`, `Title`, `Format`, `Year` and `SizeBytes` — not only `Title` and `Year` (brief §5.3).

- [ ] T009 [P] [US2] Add the remaining parsing cases to `tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs` per brief §5.3 cases 1-13: parentheses and brackets, multi-word with inner punctuation, no marker, non-trailing marker, mismatched brackets, wrong digit count, out-of-range low and high, both valid boundaries (1888 and 2031), year-only `(2010)`, literal dots and underscores, non-ASCII digits. Name each test `Scan_<Condition>_<Expected>` and give it the three AAA headers and no other comment (form fixed by T006). Every case asserts the **full** `ScannedMovie` — `Path`, `Title`, `Format`, `Year` and `SizeBytes` — and no case asserts `Title` and `Year` alone; each row therefore also fixes the chosen file's extension and length. `[Theory]` with `[MemberData]` where the cases are one row each
- [ ] T010 [US2] Complete `ParseFolderName` and its source-generated regex in `src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs`: **trim `directory.Name` first** (D8) — `Trim()` is the helper's first statement, and one trimmed local feeds both the regex match and the whole-name fallback; the pattern keeps the `$` anchor exactly as brief §5.1 gives it, because trimming first removes any trailing newline so `$` vs `\z` is not observable. Otherwise: end-anchored, `RegexOptions.CultureInvariant`, ASCII `[0-9]{4}` and never `\d`, two alternatives so mismatched brackets cannot match. **Validity before stripping, in three outcomes**: (a) no match or `ReleaseYear.TryCreate` rejects → `(whole trimmed name, null)`, marker kept in the title (`Old (1887)` stays `Old (1887)`); (b) valid year with blank preceding text → `(whole trimmed name, year)` — the year-only branch, `(2010)` → Title `(2010)`, Year 2010 (brief §5.3 case 11); (c) valid year with non-blank preceding text → `(preceding text, year)`, marker stripped. The year goes through `ReleaseYear.TryCreate` only; no range constant is duplicated. Do not collapse the two alternatives into a character-class form — it would accept `(2010]`. Checkpoint: the pattern reuses the group name `year` in both alternatives; if the runtime rejects that, split into two named groups and read whichever matched — identical behaviour, no ruling needed (plan §4)
- [ ] T011 [US2] Gates, then the filtered test run green

**Checkpoint**: every naming convention in §5.3 parses as ruled.

---

## Phase 5: User Story 3 - The right file is the primary video (Priority: P1)

**Goal**: selection is deterministic and independent of enumeration order, file-name case and OS (brief §5.3 cases 14-20).

**Independent Test**: the 7 selection cases through `Scan`, each asserting the full `ScannedMovie` — `Path`, `Title`, `Format`, `Year` and `SizeBytes` — not only `Format`, `SizeBytes` and `Path` (brief §5.3).

- [ ] T012 [P] [US3] Add the selection cases to `tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs` per brief §5.3 cases 14-20: each of `.mkv .mp4 .avi .m4v` selected with a lower-case dot-less `Format`; `MOVIE.MKV` and `Movie.Mp4` still selected and normalised; unsupported-only (`.srt .nfo .txt .wmv`) rejected; mixed supported/unsupported picks the supported one even when smaller; several supported pick the largest and `SizeBytes` equals its length; an `a.mkv` vs `B.mkv` size tie resolves to `B.mkv` under ordinal order; `Path` is the scanned folder and not the file. Name each test `Scan_<Condition>_<Expected>` and give it the three AAA headers and no other comment (form fixed by T006). Every case asserts the **full** `ScannedMovie` — `Path`, `Title`, `Format`, `Year` and `SizeBytes` — and no case asserts only the selection fields; the unsupported-only case asserts the exact exception type instead of a partial result (brief §5.3)
- [ ] T013 [US3] In `src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs` add the private `SupportedExtensions` allowlist (`".mkv", ".mp4", ".avi", ".m4v"`, `StringComparer.OrdinalIgnoreCase`, so matching is case-insensitive on every OS) and finish `SelectPrimaryVideo`: `EnumerateFiles()` with **no** search option, filter on `Extension`, order by `Length` descending then `Name` ascending under `StringComparer.Ordinal`, take the first or `null`. `SearchOption.AllDirectories` is never passed. No new Core type for the allowlist (Q6)
- [ ] T014 [US3] Gates, then the filtered test run green

**Checkpoint**: selection is total, deterministic and case-independent.

---

## Phase 6: User Story 4 - An unscanable folder says why (Priority: P1)

**Goal**: the two defined failures are distinguishable, and nesting never qualifies (brief §5.3 cases 21-24).

**Independent Test**: the 4 failure and nesting cases, each asserting the exact exception type.

- [ ] T015 [P] [US4] Add the failure and nesting cases to `tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs` per brief §5.3 cases 21-24: missing folder; empty existing folder; video only in a subfolder; a top-level video alongside a larger nested one. Name each test `Scan_<Condition>_<Expected>` and give it the three AAA headers and no other comment (form fixed by T006). This stays at 24 cases: the "other I/O errors propagate unchanged" clause of US4 scenario 5 / FR-013 is proven by the T021 inspection, not by a 25th test (D9)
- [ ] T016 [US4] In `src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs` raise `System.IO.DirectoryNotFoundException` naming `folder.Value` when `directory.Exists` is false, and `System.InvalidOperationException` naming `folder.Value` when `SelectPrimaryVideo` returns `null`. No custom exception type, no catch-all, no wrapping — any other I/O error propagates unchanged, and the port stays non-nullable (Q9)
- [ ] T017 [US4] Gates, then the filtered test run green

**Checkpoint**: both failure modes are distinct, specific and nesting is proven non-recursive.

---

## Phase 7: User Story 5 - In-memory proof and measured coverage (Priority: P2)

**Goal**: prove the boundary is disk-free and prove the coverage figure with a real report (Q12, Q12a, AC2).

**Independent Test**: the coverage command emits its own report, and every in-scope class in it is complete.

- [ ] T018 [US5] Resolve the conditional §5.3 case-5 row (folder name with surrounding whitespace). Run it. If `MockFileSystem` cannot represent such a name, drop the row and record the drop and its evidence in the implementation receipt — do not silently omit it
- [ ] T019 [US5] Run the plan's coverage command into a **fresh** temp directory outside the repo and record the command, the exit code and the emitted report path. A run that exits 0 and emits nothing is a failure (recon §14.3 Run A), not a pass
- [ ] T020 [US5] Read the emitted Cobertura and verify every in-scope class: `line-rate="1"`, `branch-rate="1"`, every `<line>` `hits > 0`, and every branch line's `condition-coverage` reading `100% (n/n)`. Confirm the scanner reports a non-empty executable-line set, and that the compiler-generated source-generated-regex helper is inside the scope (the class is `partial`, so it is emitted into it). Close any gap with a test — never with `[ExcludeFromCodeCoverage]` or a report filter. Hand the receipt rows to Quill for the PR body
- [ ] T021 [US5] Confirm the disk-free boundary: grep the finished scanner for `System.IO.File`, `System.IO.Directory`, `Path.`, `FileSystem(` and `new FileSystem` and verify zero direct filesystem-type references; confirm no test in the file creates or reads a real directory and no temp path appears. Then **prove the propagation clause of FR-013 and US4 scenario 5 by inspection** (D9): grep the same scanner for `try`, `catch` and `when` clauses and expect **zero hits**. The scanner catches nothing, so no I/O failure can be remapped — that is the whole proof, and it needs no test double beyond `MockFileSystem`.

  ```powershell
  rg -n '\btry\b|\bcatch\b|\bwhen\s*\(' src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs
  ```

  Any hit is a defect in the scanner or a false positive to be read and dismissed in writing — not a reason to add a test.

**Checkpoint**: the ticket's own acceptance evidence exists and is quotable in the PR.

---

## Phase 8: Close-out

- [ ] T022 Run `./scripts/run-vulnerable-packages.ps1`. Two new packages are now in the graph, so this gate is materially relevant to this ticket (`harness.yml:23-25`: `fail: true`, `includeTransitive: true`). Report exit 0 or "Could not run" — never fold a skip into green
- [ ] T023 Run `./scripts/run-property-tests.ps1`. Expected exit **2**, no tagged tests: `ScannedMovie` is a record with no logic and the parse/selection rules are not FsCheck-shaped domain invariants. Per `harness.yml:30-34`, record this ticket's opt-out in its task note and in the PR body; the pipeline then accepts that exit for DEV-304 only. Never move a threshold to clear it
- [ ] T024 Run `pwsh -NoProfile -File ./scripts/run-mutation.ps1`. Stryker keeps its Core-only scope with no configuration change (Q12). `ScannedMovie` is Core but carries no logic, so no survivors are expected; list any that appear rather than dismissing them
- [ ] T025 [US5] Final gate pass (brief §5.5 item 6, D7), run **after T019-T020**. One pass, `-Files` passed as real array args over **every** changed `.cs` file — the scanner, the test file(s), `src/LamuFlix.Core/Ports/ScannedMovie.cs` and `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs` — in this order: (1) `./scripts/run-roslyn-analyzers.ps1` exit 0; (2) `./scripts/run-cyclomatic-complexity.ps1` exit 0 at the ceiling of 15; (3) `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` exit 0 at the refactor ceiling; (4) `./scripts/run-jetbrains-inspectcode.ps1` with zero issues at WARNING or higher. Then full `dotnet test` green and `dotnet format --verify-no-changes` clean. A gate that cannot run is recorded as "Could not run", never as green.

  ```powershell
  $changed = @(
    'src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs',
    'tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs',
    'src/LamuFlix.Core/Ports/ScannedMovie.cs',
    'tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs'
  )
  & ./scripts/run-roslyn-analyzers.ps1 -Files $changed
  & ./scripts/run-cyclomatic-complexity.ps1 -Files $changed
  & ./scripts/run-cyclomatic-complexity.ps1 -Files $changed -Threshold 6
  & ./scripts/run-jetbrains-inspectcode.ps1 -Files $changed
  ```

  Run these in-process with the call operator, not `pwsh -File`: a `-File` launch hands `$changed` over as separate native string arguments, so `-Files` would bind only the first path and the rest would fall through — the same collapse as recon §11. If brief §5.2's permitted split happened, the second test path becomes the two split files and the array holds five entries.

- [ ] T026 `git diff --stat origin/main...HEAD` shows only the seven files in plan "Project Structure" — or eight if the test file is split per brief §5.2 — and nothing else. Confirm no DI extension, no `Program.cs`, no `TelemetryConstants`, no health check, no migration or model snapshot, no `LamuFlix.Test` edit, and no `harness.yml` or `stryker-config.json` edit. Confirm `ArchitectureTests.cs` is **absent from the diff** — the port it guards was already whitelisted, so it needs no edit — and that T025's full suite run shows that class green

---

## Dependencies

- **Phase 1** blocks everything: no scanner code compiles without `System.IO.Abstractions`.
- **Phase 2** blocks Phases 3-7: the scanner returns the five-field result T005 introduces.
- **Phase 3** blocks Phases 4-6: it creates the `Scan` shell the later tests reach through.
- **Phase 4** (parsing) and **Phase 5** (selection) are independent of each other, as brief §5.5 items 3 and 4 state.
- **Phase 6** depends on Phases 3, 4 and 5 (brief §5.5 item 5).
- **Phase 7** depends on all behaviour phases; **Phase 8** is last.

## Parallel Opportunities

- T002 and T003 touch different `.csproj` files — safe in parallel. T001 is **not** parallel with them: all three version entries land in `Directory.Packages.props`.
- T006, T009, T012 and T015 each only append cases to one test file and are sequential with respect to each other; each is independent of its own implementation task, so the test may be written while the previous phase's implementation settles.
- T020 (coverage read) and T021 (boundary grep) are independent and may run together.

## Implementation Strategy

**MVP first**: Phases 1-3. After T008, one folder scans end to end and returns all five fields from memory, with no persistence and no host wiring.

**Incremental delivery**: T005 lands the contract change alone, safely and green. T007 makes the folder scan at all. T010 pins the naming conventions. T013 pins the selection rule. T016 pins the two failure modes. T019-T021 turn "the tests pass" into a measured coverage claim. T022-T026 close the ticket.

**Do not merge Phase 3 alone**: its selection is provisional and T013 replaces it.

**Stop at any checkpoint** to validate that story independently before continuing.

## Notes

- `[P]` marks tasks touching disjoint files with no unmet dependency.
- Commit after each task or logical group, message form `DEV-304 - {subject}`.
- No task introduces a dependency, project, schema change, DI registration, exception type or domain type beyond the frozen scope. If any of those turns out to be necessary, it is a ruling, not an implementation decision: return it to Keel.