# Implementation Plan: Mutation gate linkage fix and receipts

**Branch**: `bug/DEV-382-diagnose-stryker-mutant-linkage-infrastructure-api` | **Date**: 2026-09-28 | **Spec**: [spec.md](spec.md)

**Input**: `specs/DEV-382/spec.md`, `brief.md`, `CONCLUSIONS.md`

## Summary

The gate reports 0 kills because xUnit v3 test projects run out of process under VSTest, so the active mutant never reaches the tests (brief "Root cause"). The fix has five parts:

- Pin Stryker 5.0.0.
- Run it on the MTP runner with coverage off.
- List eligible test projects explicitly, with ArchitectureTests excluded.
- Fail on artifact kills and on unattributed kills.
- Add a receipt script that measures a pushed commit in a throwaway checkout and writes compact, committed receipts.

No `.cs` or `.csproj` file changes.

## Technical Context

**Language/Version**: PowerShell 7 (the gate scripts), plus JSON config. Stryker.NET `5.0.0` as a local dotnet tool.

**Primary Dependencies**:

- `dotnet-stryker` 5.0.0;
- the existing `scripts/_gate-common.ps1` (`Get-TestProjects`, `Get-HarnessValue`, `Get-RepoRoot`);
- `System.Reflection.Metadata`, which ships with the .NET runtime that hosts pwsh.

**Storage**:

- Receipts are files under `specs/DEV-382/receipts/`.
- Reports and logs go under the system temp directory.

**Testing**: the repo has no script-test harness. Verification uses:

- `-EvaluateReport` on the real E1/E3/E5 reports;
- synthetic scratch reports for each failure reason;
- `-DryRun` on this branch and on a DEV-296 checkout;
- the receipt runs themselves.

**Target Platform**:

- The gate runs on Windows, Linux and macOS with pwsh 7.
- The receipt sampler needs Windows (`Win32_Process`, `Process.Modules`).

**Project Type**: build and gate tooling.

**Performance Goals**: none new. With coverage off, runtime grows roughly linearly in mutants × suite time. E5 took 17 minutes for 63 mutants with concurrency 1, and the gate uses Stryker's default concurrency.

**Constraints**:

- The threshold is unchanged.
- The DEV-296 worktree is not modified.
- StrykerOutput never lands in the repo.
- Exit codes: 0 pass, 1 fail, 2 skipped.

**Scale/Scope**: 4 tracked files edited, 1 script added, 1 ADR, and CONTEXT terms (already written), plus the receipts.

## Constitution Check

| Principle / rule | Status |
|---|---|
| IX. Test pyramid: mutation runs against the unit tests with break 80 | **Aligned.** Excluding ArchitectureTests from `test-projects` puts mutation back on the unit-test layer, and break stays 80. |
| Documentation rules: an architectural choice needs an ADR | **Met.** `docs/adr/0016-stryker-mtp-runner.md` (Proposed). |
| `CONTEXT.md` terms | **Met.** Mutant linkage, Artifact kill and Mutation receipt were added. |
| No machine paths | **Met.** Receipts replace the checkout path with `<checkout>` and the output root with `<out>`. |
| Static-analysis gates for `.cs` | **N/A.** No `.cs` changes. |
| Comments only where necessary | Script header comments are updated to match the existing script's density. No new commentary beyond what explains non-obvious behaviour (the tripwire, and why the runner is MTP). |

No violations, so Complexity Tracking is empty.

## Design

### D-1. Tool pin and tracked config

- `.config/dotnet-tools.json`: change `dotnet-stryker` to `"5.0.0"`, with `rollForward` false.
- `stryker-config.json`: add `"test-runner": "mtp"` and `"coverage-analysis": "off"`. Nothing else changes.

### D-2. `scripts/run-mutation.ps1`

**Parameters.** Add:

- `-Project <string[]>`: accepts `LamuFlix.Api` or `LamuFlix.Api.csproj`.
- `-OutputRoot <string>`.

Update the help text.

**Header comment.** Replace the 4.16-only wording:

- The `since` filter stays disabled. Worktree resolution was observed broken on 4.16.0 and was not re-verified on 5.0.0.
- The runner is MTP with coverage off, because under VSTest the xUnit v3 test executables run out of process and never see the active mutant (DEV-382, ADR-0016).
- Generated configs list the eligible test projects and exclude ArchitectureTests.

**Wiring check.** If the tracked config lacks `test-runner` or `coverage-analysis`, print `GATE NOT WIRED: ...` and exit 1. Defaulting to VSTest would silently reproduce the 0-kill defect.

**`Get-EligibleTestProjects -RepoRoot -MutatedProjectPath`.**

- Start from `@(Get-TestProjects -RepoRoot $repoRoot)`, called once and cached.
- Keep each project whose file name does not match `*.ArchitectureTests.csproj` and whose `[xml]` has a `//ProjectReference/@Include` that resolves, relative to that csproj's directory, to the mutated csproj's full path. Compare OrdinalIgnoreCase.
- Return the list sorted.

**Project filter.** Apply it after grouping:

- Normalise the names.
- If any name is not among the changed projects, exit 1 and list both sets.
- Restrict `$sortedProjects` to the named projects.

**Config generation.** Add `test-runner`, `coverage-analysis` and `test-projects` (absolute paths) to each generated config. Print `  Test projects: ...` per project. Collect the projects with an empty list; if there are any, exit 1 after printing all of them. This applies in `-DryRun` too.

**Output.**

- `$outputRoot` = `-OutputRoot` or `[IO.Path]::GetTempPath()/LamuFlix-stryker/<UTC yyyyMMdd-HHmmss>`, fully resolved.
- If it is inside `$repoRoot`, exit 1.
- Per project, the directory is `$outputRoot/<ProjShort>`, and the effective config is copied to `<ProjShort>/stryker-config.json` before the run.
- The run is `Push-Location <projectDir>; & dotnet stryker -f <cfg> -p <proj.csproj> -O <projOut>`, and the report path is `<projOut>/reports/mutation-report.json`.
- The StrykerOutput newest-directory search is removed.
- Print `Stryker native exit for <proj>: <n>` and `Output root: <path>`.

**`Evaluate-MutationReport`.**

- Build `id → @{ Name; File }` from `report.testFiles`, where present.
- For each Killed mutant:
  - an empty `killedBy` counts as unattributed;
  - an id that is not in the map counts as unresolved;
  - a killer whose file path contains a directory segment matching `*.ArchitectureTests`, or whose name matches `(^|\.)[^.]*ArchitectureTests\.`, counts as an artifact.
- Add these failure reasons:
  - `Artifact kill: <n> killed mutant(s) attributed to ArchitectureTests test(s) (<distinct names, first 5>); these fail on Stryker's injected types, not on the mutation.`
  - `<n> killed mutant(s) have no killedBy test names resolvable through testFiles; artifact kills cannot be ruled out.`
- Add `ArtifactKills` and `UnattributedKills` to the returned object.
- Reword the zero-kill reason to end `- this indicates a mutant-to-test linkage or measurement failure, not a test-quality verdict.`

`-EvaluateReport` goes through the same function, so it gets the new checks automatically.

### D-3. `scripts/new-mutation-receipt.ps1` (new)

**Parameters:**

- `-Commit` (mandatory);
- `-Project` (mandatory, `string[]`);
- `-ReceiptDir` (mandatory; one subfolder per project short name without the `LamuFlix.` prefix, e.g. `Infrastructure`);
- `-WorkRoot` (default `<temp>/LamuFlix-receipt/<stamp>`);
- `-KeepCheckout`.

**Exit codes:** 0 when the receipts were written (the gate verdict is inside them), 1 on any failure to produce them.

**Steps:**

1. **Preconditions.**
   - Running on Windows.
   - Resolve `$sha` from `git rev-parse --verify <Commit>^{commit}`.
   - `git branch -r --contains $sha` must not be empty; otherwise exit 1 with "not pushed".
   - The tooling SHA is `git rev-parse HEAD`. It is flagged dirty when `git status --porcelain` lists any overlaid file.
2. **Checkout.**
   - Run `git worktree add --detach <WorkRoot>/checkout $sha`.
   - Overlay these files from the current checkout:
     - `.config/dotnet-tools.json`;
     - `stryker-config.json`;
     - `scripts/run-mutation.ps1`;
     - `scripts/_gate-common.ps1`;
     - `scripts/_harness-config.ps1`.
   - Record each file's SHA256.
   - Run `dotnet tool restore` and record its exit.
   - Record the Stryker version from `dotnet tool list --local`.
3. **Sampler.** A thread job polls `Win32_Process` every 300 ms. It picks each process that is:
   - a process whose command line contains the checkout path and matches `\\bin\\[^ ]*\.(dll|exe)`, excluding `MSBuild.dll` and `VBCSCompiler`; or
   - a process whose name is `testhost*`.

   For each process, on first sight plus 1.5 s, it records:
   - pid, parent pid and name, and the command line;
   - its loaded `LamuFlix.*` modules;
   - for modules named after a target project: the SHA256 and the `Stryker`-namespace type count, read through a `FileShare.ReadWrite|Delete` stream with `PEReader`/`MetadataReader`.

   It writes one JSON line per process.
4. **Gate.**
   - Run `pwsh -NoProfile -File <checkout>/scripts/run-mutation.ps1 -Project <...> -OutputRoot <WorkRoot>/gate`, from the checkout.
   - Tee the output to `<WorkRoot>/gate.log`.
   - Record the gate exit.
   - Parse `Stryker native exit for` lines into per-project Stryker exits.
   - Parse the `Merge base` / base-ref lines if they are printed. Otherwise compute `git merge-base $sha origin/main`.
5. **Clean identity.**
   - Stop the sampler.
   - Build each eligible test project with `dotnet build -c Debug --no-incremental`, and record the exit. The eligible projects are read from the effective config's `test-projects`.
   - For every distinct observed target-module path, hash the file again and count its `Stryker` types.
6. **Receipts.** For each project, write `receipt.json`:
   - `ticket`, `generatedUtc`;
   - `measured {commit, remoteBranches, mergeBase}`;
   - `tooling {commit, dirty, overlaid[]}`;
   - `strykerVersion`, `threshold`;
   - `effectiveConfig` (paths relativised) and `testProjects`;
   - `exits {toolRestore, gate, stryker, cleanBuild}`;
   - `mutatedFiles`;
   - `counts`, `score`;
   - `mutants[] {file, line, mutator, status, statusReason, killedBy[]}`;
   - `processes[]` (those that loaded the project's module; otherwise all sampled);
   - `assembly {module, observations[] {path, runSha256, runStrykerTypes, cleanSha256, cleanStrykerTypes}, verdict}`.

   The verdict is one of:
   - `mutated-assembly-loaded`: the run hash differs from the clean hash and `runStrykerTypes > 0`;
   - `not-observed`;
   - `inconclusive` (with the reason).

   Then write `receipt.md`: a header with the SHAs and versions, the command that reproduces it, the exits, the counts, the evidence verdict, the test projects, and a per-mutant table.
7. **finally:** stop and remove the sampler, then `git worktree remove --force` the checkout unless `-KeepCheckout`. Leave `<WorkRoot>` with its logs and reports.

### D-4. Documentation

Update the lines the scout found that describe the gate as 4.16, VSTest, or StrykerOutput-in-repo, where they would now be wrong. Leave historical specs (`specs/DEV-2xx`) untouched.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-382/
├── CONCLUSIONS.md      # grill record (append-only)
├── brief.md            # closing bar, frozen scope, loop terms
├── spec.md
├── plan.md             # this file
├── tasks.md
├── checklists/
│   ├── requirements.md
│   └── gate.md
└── receipts/
    ├── Infrastructure/receipt.{json,md}
    └── Api/receipt.{json,md}
```

### Source Code (repository root)

```text
.config/dotnet-tools.json          # pin 5.0.0
stryker-config.json                # + test-runner, coverage-analysis
scripts/run-mutation.ps1           # D-2
scripts/new-mutation-receipt.ps1   # D-3 (new)
docs/adr/0016-stryker-mtp-runner.md
CONTEXT.md
```

**Structure Decision**: tooling only. There is no new project, and no `.cs` or `.csproj` change.

## Verification plan

| Check | Expected |
|---|---|
| `-EvaluateReport` E1 (ArchitectureTests killers) | exit 1, artifact-kill reason |
| `-EvaluateReport` E3 (4.16 MTP, ids only) | exit 1, unattributed reason |
| `-EvaluateReport` E5 (5.0.0 MTP, named) | exit 0 |
| `-EvaluateReport` E4 (0 killed) | exit 1, linkage wording |
| Synthetic: Killed with empty `killedBy` | exit 1 |
| `-DryRun` on this branch | exit 2 (no src changes; skip happens before config) |
| `-DryRun` in a DEV-296 detached checkout with tooling overlaid | exit 0; Core/Infrastructure/Api configs list only `LamuFlix.Test` |
| `-DryRun -Project Nope` there | exit 1 |
| `-OutputRoot <inside repo>` | exit 1 |
| Receipt run `-Commit <DEV-296 pushed head> -Project LamuFlix.Infrastructure,LamuFlix.Api` | receipts written; every Killed has names; none are ArchitectureTests; assembly verdict recorded |
| `git worktree list` after the receipt run | no leftover checkout |
| `harness.yml` / `stryker-config.json` thresholds | unchanged at 80 and 90/80/80 |
