# DEV-304 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-01
**Author**: Quill (Writer). Drafted in `F:\Dev\LamuFlix.worktrees\feature-304-spec`, branch `feature/304-spec`, from `c97f43c` (brief). `git rev-parse --show-toplevel` confirmed the worktree before any write; the main checkout was not touched.
**Method**: `/speckit-specify` executed against `.specify/templates/spec-template.md`; `.specify/scripts/powershell/setup-plan.ps1 -Json` and `setup-tasks.ps1 -Json` run for path resolution (plan and tasks drafted by hand from their templates in the same pass, precedent DEV-296 and DEV-301). `$env:PYTHONUTF8='1'` set before every speckit invocation. No `.specify/extensions.yml` exists, so no extension hooks ran in either direction.

**Inputs**: `specs/DEV-304/brief.md`, `CONCLUSIONS.md` (Q1-Q12, Q12a), `ASSUMPTIONS.md`, note `recon-DEV-304`, `specs/PRODUCT.md`, `.specify/memory/constitution.md`, and the source files the brief cites (`ScannedMovie.cs`, `IMediaLibraryScanner.cs`, `ReleaseYear.cs`, `MediaFormat.cs`, `LibraryPath.cs`, `ImportMovieFolderCommandHandler.cs`, `ImportMovieFolderCommandHandlerTests.cs`, `Directory.Packages.props`, both `.csproj`, `ArchitectureTests.cs`, `FixedTimeProvider.cs`, `BannedSymbols.txt`, `Directory.Build.props`, `harness.yml`).

**Artifacts written**

- `specs/DEV-304/spec.md` — 5 user stories (US1 contract-bearing result, US2 folder-name parsing, US3 primary-file selection, US4 failure signalling, US5 in-memory proof and measured coverage), 28 acceptance scenarios, 13 edge cases, FR-001 to FR-020, 5 key entities, SC-001 to SC-007, 6 `[assumed]` taste items.
- `specs/DEV-304/plan.md` — technical context, constitution check (PASS, no departures), project structure of seven files (eight if the test file is split per brief §5.2), six-part design with the verified behaviour table for the folder-name pattern, test strategy, and the gate set including the coverage measurement and its no-op trap.
- `specs/DEV-304/tasks.md` — 26 tasks in 8 phases (Setup, Foundational, US1-US5, Close-out), each with an exact file path, plus dependencies, parallel opportunities and incremental strategy.
- `specs/DEV-304/checklists/requirements.md` — 17 quality items, all resolved, with three items recorded for spec review.

**Decisions made by Quill**: none. Every behavioural requirement traces to a ticket AC, a ruling in `CONCLUSIONS.md`, a section of `brief.md`, or a line of `ASSUMPTIONS.md`. Where the brief offered a choice, the plan records which branch was taken and why it needed no new ruling:

| Choice | Branch taken | Why it needed no ruling |
|---|---|---|
| Helper visibility (§5.1 `internal static` alternative) | `private static` (confirmed by **D3**, which withdrew the alternative) | `LamuFlix.Infrastructure` has no `InternalsVisibleTo` for the unit test project, so the alternative needs a `csproj` edit beyond §5.2. The folder name is the parser's only input, so all 13 cases are reachable through `Scan` (plan §4) |
| Clock stub (§5.3 `FakeTimeProvider` alternative) | existing `tests/LamuFlix.UnitTests/Features/FixedTimeProvider.cs` (confirmed by **D4**) | `FakeTimeProvider` needs `Microsoft.Extensions.Time.Testing`, which no project references — a new dependency, outside the frozen scope. The fixed stub is the other option §5.3 offers |
| Allowlist collection type (§5.1) | `FrozenSet<string>` with `StringComparer.OrdinalIgnoreCase` | §5.1 offers either; behaviour is identical under Q6 |
| Test file layout (§5.2) | one file in `tests/LamuFlix.UnitTests/FileSystem/`, split only if readability forces it | §5.2 leaves this to the writer explicitly |
| `[assumed]` failure message wording | both defined failures name the offending folder | §5.1 fixes the exception types and that the folder is named; the string is copy. Logged in spec.md Assumptions for Patron's ratification rather than added to `CONCLUSIONS.md` or `ASSUMPTIONS.md`, which are Patron's files |

**Routed to Keel for spec review** (capped at 2 rounds per Q12) — recorded in `checklists/requirements.md` Notes and in the relevant spec/plan sections, not absorbed silently. **All three are now resolved by `brief.md` §7 (round 1, commit `5abacd2`); none changed scope:**

1. **`needs decision:` brief §5.5 item 5 — RESOLVED, D1 (confirmed).** Item 5 lists "Scan composition" after items 3 and 4, while §5.3 exercises all 24 cases through `Scan` and §5.1 makes the helpers `private static`. `tasks.md` reads item 5 as the guard clauses and result mapping (which do depend on 3 and 4), with the minimal `Scan` shell landing with the helpers; item 5's dependency direction is preserved. Keel confirmed this reading rather than overruling it.
2. **`needs decision:` gates absent from brief §5.4 — RESOLVED, D2 (confirmed in scope).** `./scripts/run-property-tests.ps1` and `./scripts/run-vulnerable-packages.ps1` are configured in `harness.yml:23-25` and `harness.yml:30-34` and listed in `AGENTS.md`, and two new packages enter the graph here. `tasks.md` carries both (T022, T023), including the `harness.yml:30-34` mechanism for the property gate's expected exit 2. Keel reads §5.4 as including both; neither was struck.
3. **Recorded, not escalated — RESOLVED, D3 (alternative withdrawn).** §5.1's `internal static` alternative is unreachable under the §5.2 frozen scope. Keel withdrew it: the helpers are `private static` and are reached through `Scan`, so no `InternalsVisibleTo` edit outside §5.2 is needed. `plan.md` §1 and §4 record that withdrawal.

**Two further rulings from round 1 that confirm choices already in the drafts (recorded, not scope changes):**

4. **D4 — clock stub confirmed: the existing `tests/LamuFlix.UnitTests/Features/FixedTimeProvider.cs`.** `FakeTimeProvider` would require `Microsoft.Extensions.Time.Testing`, a new package outside frozen scope, so the existing stub is the branch taken. `spec.md` Assumptions, `plan.md` Test Strategy and T006 already say this.
5. **D5 — vocabulary (`constitution.md:277`): no `CONTEXT.md` edit.** "Primary video" names only a private helper and creates no public type, member or API term; `CONTEXT.md:21-22` (_Avoid_ "Scan" for the single-folder case) applies to command naming; the port `IMediaLibraryScanner.Scan` already exists and the ticket names `DirectoryMediaLibraryScanner`, so the ticket text decides. `CONTEXT.md` is therefore not edited.

D6 (seven files, or eight if the test file is split per §5.2) and D7 (one final gate pass over every changed `.cs` file, complexity at 15 and then at `-Threshold 6`) are applied in `plan.md` "Project Structure", plan Gates/Close-out and `tasks.md` T025-T026. Spec review round 1 is complete; round 2 remains under the hard cap set by Q12.

**Design detail verified against the rulings before drafting** (all 13 parsing cases walked through the prescribed pattern, results recorded in plan.md §4): validity must be established **before** the title decision, so a rejected year leaves its marker in the title; the two-alternative shape is what rejects mismatched brackets, so a collapsed character-class form must not be substituted; `[0-9]` rather than `\d` is what enforces ASCII digits. One code-shape risk is flagged as an implementation checkpoint rather than a question: the pattern reuses the group name `year` in both alternatives, and plan.md records the equivalent two-distinct-group form if the runtime rejects it. Behaviour is identical either way.

**Gate 1 status**: closed. Nothing in this receipt opens it. `CONCLUSIONS.md` remains the record of rulings and was not edited; `ASSUMPTIONS.md` was not edited.

**Rigger action still owed** (unchanged by this receipt): a recon-fact comment on DEV-304 recording that no legacy disk-bound scanner tests exist (Q1). No follow-up ticket for persistence (Q3) or composition (Q11); both noted only.