# Implementation Plan: DEV-397 Mutation Runner Api Diff Support

**Branch**: `feature/397-spec` | **Date**: 2026-10-04 | **Spec**: [spec.md](spec.md)

**Input**: `specs/DEV-397/brief.md`, `CONCLUSIONS.md`, `recon-DEV-397`, and `specs/PRODUCT.md`.

## Summary

Extend `scripts/run-mutation.ps1` to classify all changed projects, honor a validated configured exclusion map, run Stryker for every eligible project despite unlisted ineligible neighbors, and return the defined exit outcomes. Add the dependency-free fixture harness and align the help, `AGENTS.md`, and pipeline note. Verify the real Api-plus-Infrastructure run and DEV-309 Infrastructure retrospective without altering the receipt helper. OD-1 and OD-2 remain unchecked owner decisions; planning on Api exclusion does not resolve OD-1, and AC7 remains unresolved pending OD-2.

## Technical Context

**Language/Version**: PowerShell 7 (`pwsh -NoProfile`); no C# production-code change is specified.

**Primary Dependencies**: Existing repository PowerShell scripts, `harness.yml`, .NET/Stryker toolchain. No new dependency.

**Storage**: Configuration in `harness.yml`; no database or schema.

**Testing**: New dependency-free `scripts/Test-RunMutation.ps1`, real Stryker run against `9f92ad1`, DEV-309 retrospective, and applicable harness gates per brief §7.

**Target Platform**: Existing LamuFlix repository on supported PowerShell/.NET development environment.

**Project Type**: Repository gate tooling and its documentation.

**Performance Goals**: No new performance target. DryRun performs classification without starting Stryker.

**Constraints**: Preserve the two existing test-project exclusions, threshold from configuration, output seams consumed by `new-mutation-receipt.ps1`, exact frozen file scope, and the exit-code semantics in [spec.md](spec.md). No explanatory code comments beyond repository allowances. No unrelated scope expansion.

**Scale/Scope**: Five implementation file responsibilities F1-F5, targeted UnitTests F6 as required by retrospective survivors, external task-pipeline note update F7, and DEV-397 spec artifacts F8. The exact authorized paths are listed in `brief.md` §3.

## Constitution Check

*Initial and post-design check:*

- No new dependency, project, top-level folder, architectural layer, schema, public API, LocalPlay behavior, secret handling, or `Process.Start` work is specified.
- Test additions, if needed for the named validators, belong in existing `LamuFlix.UnitTests` and follow the existing test framework and conventions (Constitution IX).
- Both `*.ArchitectureTests` and `*.IntegrationTests` remain excluded from mutation eligibility (Constitution IX and brief §4.2).
- `_harness-config.ps1` is an existing file not named by the ticket; its narrowly scoped map support is explicitly authorized by Patron Q2 and brief §3/F2.
- OD-1 and OD-2 are owner checkboxes and stay open. Gate 1 remains closed until answered.

No constitution violation or new structural decision is introduced by this plan.

## Design

### Configuration and classification

Add the `gates.mutation.exclusions` map value type only for that prefix in `_harness-config.ps1`, preserving the existing allow-list behavior elsewhere. Widen key parsing to permit dotted keys only as direct children of the exact, anchored `gates.mutation.exclusions` prefix; nested children, a scalar value on `exclusions`, and near-miss paths remain hard errors. Add one prefix schema entry, an empty-map default in `Get-HarnessDefaults`, and one accessor that lists map entries; `Get-HarnessValue` remains scalar. Validate every entry before classification and the scope-empty check. Exclusion keys match project names ordinal-ignore-case, and a `.csproj` suffix in a key is a configuration error (exit 1). The planned harness entry is `LamuFlix.Api` with the reason from the brief, contingent on OD-1.

For each changed project, calculate eligible test projects with the existing direct-reference rule and both existing test-family exclusions. A configured exclusion takes precedence and reports NOT APPLICABLE; warn if that project also has eligible tests. Otherwise, report eligible tests or `no eligible test project, not in policy`. Do not let an unlisted ineligible project prevent other eligible projects from running. Report eligible scores, then calculate the overall verdict. DryRun emits all classifications and never starts Stryker.

### Outcomes and compatibility

Apply the real-run exit table from the spec. Keep scope-empty SKIPPED blocking; configured all-excluded NOT APPLICABLE is exit 2 and non-blocking, never PASS. Continue using the configured mutation threshold. Preserve `new-mutation-receipt.ps1` inputs: raw gate exit, final verdict line, and per-project native Stryker exit line. Exclusions stay unmeasured and do not produce reports.

### Validation evidence

The new plain PowerShell harness overlays gate files, `.config/dotnet-tools.json`, `stryker-config.json`, the solution with its test projects, and git history reaching the merge-base into a temporary fixture through `HARNESS_REPO_ROOT`; each of its five cases asserts exit code and verdict/classification text. Separately run the real non-DryRun Api-plus-Infrastructure case against `9f92ad1`. Establish the DEV-309 pre-merge base SHA during Phase B recon, run Infrastructure retrospectively, add tests only for survivors in the two named validators, and rerun. Out-of-scope survivors are follow-up dispositions, not gate waivers.

DryRun exit branches are: 1 for invalid configuration or an unlisted ineligible project; 2 `SKIPPED` for scope-empty; 2 `NOT APPLICABLE` when all changed projects are listed; otherwise 0 for successful classification only. Add classification lines to the existing banner and generated-config output, which remains. If `-Project` names a listed project, report NOT APPLICABLE and exit 2 without running Stryker; the receipt helper then fails that receipt because no report exists, as intended, and is not edited.

For script help and `AGENTS.md`, the exit table is verbatim when its header and row text are identical after trimming leading whitespace from each line; keep the table inside the script's comment-help block. Document that reasons must not contain ` #` (an unvalidated parser limitation). Reconcile the global `AGENTS.md` exit-2 wording so mutation exit 2 `NOT APPLICABLE` is non-blocking while `SKIPPED` remains blocking. Record the full gate command set from brief.md §7.

### Repository Structure

```text
scripts/run-mutation.ps1
scripts/_harness-config.ps1
scripts/Test-RunMutation.ps1
harness.yml
AGENTS.md
tests/LamuFlix.UnitTests/  # only tests for the two named DEV-309 validators, if survivors require them
specs/DEV-397/{brief.md,CONCLUSIONS.md,spec.md,plan.md,tasks.md}
```

The `task-pipeline` Maestri note is updated by Bernstein, outside the repository diff. No other paths are authorized by this plan.

## Complexity Tracking

No constitution violations to justify.
