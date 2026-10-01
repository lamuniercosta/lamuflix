# DEV-304 - Patron grill rulings

Round 1, Q1-Q12 (grill budget complete). Worktree: F:\Dev\LamuFlix.worktrees\feature-304-spec; branch: feature/304-spec; intake base: f5d5d60. Ticket substance and repository recon are accepted from Keel and recon-DEV-304, especially §2; no direct tracker query or write was made by Patron. Current assigned-role §2.3 and specs/PRODUCT.md §5 govern these rulings. No owner checkbox is introduced. Gate 1 remains closed pending clean analysis, plan adjudication and owner approval; these rulings do not authorise implementation.

## Q1 - Legacy tests

**Keel:** No ignored disk-bound scanner tests exist anywhere (recon §7). Recommendation: clause satisfied vacuously as ticket-text interpretation, not a scope change; Rigger adds a recon-fact comment. Or blocked: structural?

**Patron:** Interpret the replacement clause as satisfied because no legacy scanner tests exist to replace; retain the clause and deliver the entire new MockFileSystem suite, with Rigger recording the recon fact on DEV-304.

- Basis: recon-DEV-304 §2 scope item 2 and §7 (verified absence); specs/PRODUCT.md:23-24/34-39. Nothing named by the ticket is dropped, and unrelated legacy tests are not rewritten or deleted.

---

## Q2 - ScannedMovie contract

**Keel:** Ticket requires year and size, but ScannedMovie has neither (recon §3/§5/§12.2). Recommendation: ScannedMovie(LibraryPath Path, string Title, MediaFormat Format, ReleaseYear? Year, long SizeBytes), update the four construction sites in ImportMovieFolderCommandHandlerTests, keep handler logic unchanged. Alternative: parse and discard.

**Patron:** Approve ScannedMovie(LibraryPath Path, string Title, MediaFormat Format, ReleaseYear? Year, long SizeBytes), with both new constructor arguments required; update all four existing test construction sites and leave handler behaviour unchanged.

- Basis: recon-DEV-304 §2 scope item 1 (parse year and extract size), §3 and §5; src/LamuFlix.Core/Ports/ScannedMovie.cs:7; specs/PRODUCT.md:34/46/48. Returning extracted data fulfils the scanner contract; discarding it does not. The contract edit and compile-forced test edits are in scope under assigned-role §2.3 item 6.

---

## Q3 - Persistence

**Keel:** Recommendation: no schema, migration, MovieRecord or Movie.Create change; Rigger files a follow-up for persisting scan year/size (recon §8/§12.3). Confirm.

**Patron:** Confirm no schema, migration, MovieRecord, Movie.Create or handler persistence change; future persistence of scan year/size is noted, no ticket.

- Basis: recon-DEV-304 §2 names scanner extraction, not persistence; §8 documents the existing storage shape; specs/PRODUCT.md:38/45 and assigned-role follow-up policy. No Critical/High finding, broken existing behaviour or owner-requested follow-up is established here, so do not create speculative work.

---

## Q4 - Dependencies and test home

**Keel:** Recommendation: System.IO.Abstractions in Infrastructure and System.IO.Abstractions.TestingHelpers in tests, pinned centrally to latest stable; MockFileSystem tests in tests/LamuFlix.UnitTests/FileSystem instead of LamuFlix.Test. Approve an Infrastructure project reference if missing (recon §6/§10; constitution:301/325).

**Patron:** Approve both named packages, centrally pinned in Directory.Packages.props to mutually compatible latest stable versions verified when selected, and tests/LamuFlix.UnitTests/FileSystem; retain the existing Infrastructure project reference.

- Basis: .specify/memory/constitution.md:299-301/318/325; specs/PRODUCT.md:12/34/43; tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj:30-33 already references Infrastructure.
- Basis: recon-DEV-304 §2/§7/§10. LamuFlix.Test identifies the nonexistent legacy suite's location, not a mandatory home for its new replacement. This uses an existing project and a subfolder, not a new architectural layer. No current package version is asserted by this ruling.

---

## Q5 - Folder-name parsing

**Keel:** Recommendation: only trailing four-digit (YYYY) or [YYYY]; trim preceding title; no marker means whole trimmed folder name and Year null; out-of-range year means null year and whole name without truncation. Dots/underscores remain literal [assumed]. Blank parsed title falls back to whole folder name.

**Patron:** [assumed] Parse a matching trailing (YYYY) or [YYYY] containing exactly four ASCII digits from the trimmed final folder name; accept the year through ReleaseYear.TryCreate, strip the suffix only for a valid year with a nonblank preceding title, and otherwise preserve the whole trimmed name.

- Basis: recon-DEV-304 §2 scope item 1 (both example forms) and §4; src/LamuFlix.Core/Domain/ReleaseYear.cs:30-43; .specify/memory/constitution.md:326; specs/PRODUCT.md:28-29.
- No marker, malformed/unmatched marker or invalid year returns Year=null and the whole trimmed name. A valid year-only name retains the valid Year but uses the whole trimmed name as Title. Dots and underscores remain literal; multi-word title content is preserved. Capture one injected UTC time per scan; the accepted range comes from ReleaseYear, not duplicated constants.

---

## Q6 - Formats and case

**Keel:** Recommendation: scanner-private static set {mkv, mp4, avi, m4v}; OrdinalIgnoreCase extension matching on every OS; MediaFormat from matched extension, normalised to lowercase; no new Core value object [assumed] (recon §13).

**Patron:** [assumed] Use a scanner-private static allowlist of .mkv, .mp4, .avi and .m4v with OrdinalIgnoreCase matching, and construct the existing MediaFormat from the selected extension; no new Core type.

- Basis: recon-DEV-304 §2 scope item 1 fixes the four supported formats; §4/§13 describe existing MediaFormat normalisation and the missing allowlist; specs/PRODUCT.md:18/28-29. Placement and cross-platform case semantics are assumed; the four-format scope is ticket-decided.

---

## Q7 - Primary selection

**Keel:** Recommendation: choose largest IFileInfo.Length, then ascending filename using StringComparer.Ordinal for equal sizes.

**Patron:** [assumed] Select the largest supported top-level file by byte length; break size ties by its filename in ascending StringComparer.Ordinal order.

- Basis: recon-DEV-304 §2 scope item 1 and §13; specs/PRODUCT.md:28-29. File size supplies a defensible primary-file heuristic; the total filename ordering avoids enumeration-order and culture dependence.

---

## Q8 - Nested folders

**Keel:** Recommendation: inspect only the supplied folder's top level; ignore Featurettes/Extras, and subfolder-only video yields the no-video outcome. Alternative: recursive search.

**Patron:** [assumed] Scan only the supplied folder's top level; nested videos are ignored, including a subfolder-only video producing Q9's no-supported-video exception.

- Basis: recon-DEV-304 §2 scope items 1-2 and §13; src/LamuFlix.Core/Ports/IMediaLibraryScanner.cs:5-8; specs/PRODUCT.md:28-29. The ticket requires testing nesting but does not require recursive traversal of other movie folders or extras.

---

## Q9 - Missing folder and video

**Keel:** Recommendation: preserve the non-nullable port; missing folder throws DirectoryNotFoundException from IFileSystem, and missing supported video throws a dedicated exception or InvalidOperationException. Rule on type/location; alternative is a nullable port (recon §13).

**Patron:** Preserve non-nullable ScannedMovie Scan(LibraryPath); a missing folder throws System.IO.DirectoryNotFoundException through the IFileSystem path, and an existing folder with no supported top-level video throws System.InvalidOperationException; add no custom exception type.

- Basis: src/LamuFlix.Core/Ports/IMediaLibraryScanner.cs:5-8; recon-DEV-304 §2 scope item 2 and §13; specs/PRODUCT.md:18/34/46; .specify/memory/constitution.md:361. These distinguish invalid input location from an existing folder unable to produce the required result without a sentinel, nullable contract or new exception-mapping obligation. Existing unrelated I/O errors propagate.

---

## Q10 - Result mapping

**Keel:** Recommendation: SizeBytes is selected file IFileInfo.Length as long bytes; Format is its extension; Path remains the input LibraryPath folder, not selected file (recon §8).

**Patron:** Confirm SizeBytes is the selected file's long byte length, Format is its normalised extension, and Path is the unchanged input LibraryPath folder.

- Basis: recon-DEV-304 §2 scope item 1, §5 (handler uses scanned.Path) and §8 (LibraryPath uniqueness); src/LamuFlix.Core/Ports/IMediaLibraryScanner.cs:7. File metadata and library folder identity remain distinct.

---

## Q11 - Constructor and composition

**Keel:** Recommendation: sealed DirectoryMediaLibraryScanner with primary ctor (IFileSystem fileSystem, TimeProvider timeProvider), using GetUtcNow for ReleaseYear. Exclude DI wiring and put it in a follow-up; no health check or telemetry name because filesystem is not an external service. Alternative: small AddMediaLibraryScanner extension (recon §4/§9).

**Patron:** Approve the sealed scanner with primary ctor (IFileSystem fileSystem, TimeProvider timeProvider) and one GetUtcNow value per scan; exclude DI extensions, host wiring, health checks and telemetry constants, with future composition noted, no ticket.

- Basis: recon-DEV-304 §2/§4/§9; src/LamuFlix.Core/Domain/ReleaseYear.cs:8/30-43; .specify/memory/constitution.md:325-326/363/367. The scanner is a filesystem adapter, not a new independently operated external service; injected IFileSystem is the only filesystem access boundary.
- Basis: specs/PRODUCT.md:18/38 and assigned-role follow-up policy. No new import composition is delivered by this ticket; DEV-302 Q3 is not binding precedent. No Process.Start, playback execution, LocalPlay change or secret is involved.

---

## Q12 - Acceptance and loop caps

**Keel:** Recommendation: 100% line AND branch coverage on scanner plus its helpers with existing coverage collection; no threshold edits; complexity <=15 then <=6 at refactor; Core-only Stryker unchanged. Spec review two rounds, plan challenge one adjudication pass (recon §11/§13).

**Patron:** Require 100% line and branch coverage on DirectoryMediaLibraryScanner and every helper added for it, retain the existing complexity and Core-only mutation gates unchanged, and cap spec review at two rounds with one plan-challenge adjudication pass.

- Basis: recon-DEV-304 §2 AC and §11/§13; .specify/memory/constitution.md:290/380-391; specs/PRODUCT.md:12. Ticket coverage is scoped acceptance evidence, not a mutation score or permission to change thresholds or generated configuration.
- The recommendation's existing coverage-collection claim is unverified: a bounded search of scripts, tests, project props and CI found no collection wiring, only Core Stryker with coverage-analysis off. Before freeze, Keel must identify a real executable coverage command/tool and its per-class line/branch report scope; any necessary new tooling dependency needs a cited Patron ruling. This is an evidence gap, not an owner checkbox; passing tests alone cannot claim 100% coverage.
- Basis for loop caps: Keel Q12 and assigned-role grill budget. No third spec-analysis round or second plan-challenge pass is authorised. The grill is complete at 12 questions; neither this cap nor the rulings open Gate 1.
