# DEV-397 brief

Make `scripts/run-mutation.ps1` handle `LamuFlix.Api` diffs instead of failing closed before any Stryker run.

- Size: **M** (ticket *Size and order*; tag `size:M` verified, task note `DEV-397`).
- Base: `origin/main` = `1e6830b` (recon-DEV-397 §0). Spec branch `feature/397-spec`.
- Sources: ticket DEV-397 (`scripts/get-task.ps1 DEV-397`), `recon-DEV-397`, Patron rulings in `specs/DEV-397/CONCLUSIONS.md` (commit `3e2d5a3`), constitution IX (`.specify/memory/constitution.md:282-308`), `AGENTS.md:124-131`, `task-pipeline` note.
- Author: Keel. Writer: Quill drafts `spec.md`, `plan.md`, `tasks.md` from this file. Anything not here is not decided: ask `needs decision: <question>` to Keel.

## 1. Closing bar

This ticket is done when all of the following hold, each with evidence in the PR body:

1. A diff touching `LamuFlix.Api` and `LamuFlix.Infrastructure` runs Stryker on Infrastructure and reports Api as `NOT APPLICABLE` with its configured reason; the exit code follows the table in §4.3. Proven by the **real (non-DryRun)** run `-BaseRef 9f92ad1` (Q6, Q3).
2. A changed project with no eligible test project that is not in the policy exits 1, shown by the `Test-RunMutation.ps1` fixture case before merge (AC2).
3. `-DryRun` prints one classification line for every changed project (AC3).
4. The `:564` failure message and the `:12-17` header comment name both exclusions (`*.ArchitectureTests`, `*.IntegrationTests`) and match the code (AC4).
5. `AGENTS.md` and the script help carry the exit-code table of §4.3, and the `task-pipeline` note carries the replacement text of §6 verbatim (AC5).
6. The DEV-309 Infrastructure validator mutation score is in the PR body, at or above 80, or every survivor is listed with its disposition (AC6, Q8).
7. Gates recorded per §7, with exit codes. AC7 as written is subject to owner checkbox **OD-2** (§2).

Gate 1 stays closed until both owner checkboxes in §2 are answered.

## 2. Owner decisions (spec PR checkboxes, unchecked; Patron cannot close them)

- **OD-1 (Q1) — Api policy, reserved to the owner by ticket Scope 1.**
  `blocked: structural — does the owner choose (a) the configured Api exclusion in harness.yml or (b) Api unit tests in LamuFlix.UnitTests plus a UnitTests -> LamuFlix.Api project reference?`
  The plan proceeds on **(a)**, the ticket's *Recommended* option. Planning on (a) does not answer OD-1.
  If the owner picks **(b)**: Keel amends this brief before Phase B (drop the `harness.yml` Api entry; add the ProjectReference and Api unit tests; the §4 machinery, the exit table, and the fail-closed rule stay, with an empty exclusions map allowed).
- **OD-2 (Q9) — AC7 cannot be met as worded.** This ticket changes no production C# under `src/`, so `run-mutation.ps1` on its own diff exits 2 SKIPPED, which `AGENTS.md:125` calls blocking and never green.
  `blocked: structural — may AC7 be replaced with: "All applicable harness gates pass and are recorded with exit codes. Record the own-diff mutation command, verdict and exit code verbatim; when no production C# under src/ changed, its exit 2 SKIPPED is recorded as scope-empty, never PASS or all-green evidence. Validate the changed mutation runner with the real Api-plus-Infrastructure run against 9f92ad1 and the DEV-309 retrospective run, and record their scores and dispositions."?`
  Until answered, the requirement is unresolved; the plan collects the evidence the replacement names (§7) either way.

## 3. Frozen scope (Q10)

In scope (only these):

| # | File | Change |
|---|---|---|
| F1 | `scripts/run-mutation.ps1` | classify-all, exclusion policy, run eligible, exit table, `:564` message, `:12-17` header, help block with exit table |
| F2 | `scripts/_harness-config.ps1` | one map type for `gates.mutation.exclusions` (edit, not rewrite; Q2) |
| F3 | `harness.yml` | `gates.mutation.exclusions` with the `LamuFlix.Api` entry (pending OD-1) |
| F4 | `AGENTS.md` | mutation exit-code table next to `:124-131` |
| F5 | `scripts/Test-RunMutation.ps1` (new) | dependency-free harness, 5 cases (§5) |
| F6 | `tests/LamuFlix.UnitTests/...` | survivor-killing tests for the two DEV-309 validators only (Q8) |
| F7 | `task-pipeline` maestri note | replacement text from §6, applied by Bernstein in Phase B (not in the PR diff) |
| F8 | `specs/DEV-397/*` | spec artifacts |

Out of scope (follow-up ticket via Rigger only if found wrong; never a finding in this round): `README.md`; `.cursor/rules/architect-gate.mdc` and `.claude/rules/pipeline/architect-gate.mdc`; `.codex/agents/*.toml`; missing `harness.yml.example`; missing `install.ps1`; legacy `tests/LamuFlix.Test`; Worker and Web policy (they stay unlisted and fail closed); `scripts/new-mutation-receipt.ps1` (no edit; its output seams are preserved, §4.5); `stryker-config.json`; the threshold value.

No §2.3 trigger beyond the ones ruled: no new package (no Pester), no new project or folder, no schema, no public API, no `Features:LocalPlay`, no secrets, no `Process.Start` (recon §5). F2 is a file the ticket does not name; Patron authorised the edit under Q2.

## 4. Approach

### 4.1 Configuration (Q2)

`harness.yml`:

```yaml
gates:
  mutation:
    threshold: 80
    exclusions:
      LamuFlix.Api: "host proof lives in IntegrationTests per Constitution IX"
```

- Keys are project names without `.csproj`, the same form `-Project` takes. Dots in a direct child key of `gates.mutation.exclusions` belong to the project name, not another config level.
- `_harness-config.ps1` gains one map value type for that prefix only. Unknown keys elsewhere stay errors (`:166-172` behaviour unchanged). No general YAML parser.
- Validation: every entry is checked before any classification. A reason that is empty or whitespace after trim is a configuration error, exit 1, before any Stryker run.
- Default (no `exclusions` key): empty map.
- Only `LamuFlix.Api` is listed (pending OD-1).

### 4.2 Classification and run (Q4, Q5)

1. Validate config (§4.1). Invalid → exit 1 before execution.
2. For every changed project, compute eligible test projects with the existing `Get-EligibleTestProjects` rule (`:91-115`, both exclusions unchanged), then classify:
   - **listed** in exclusions → `NOT APPLICABLE` with reason. If it also has eligible tests, print a `WARNING` that the exclusion may be stale; still excluded; the warning does not change the exit (Q5).
   - **eligible** → mutated.
   - **unlisted, no eligible tests** → `no eligible test project, not in policy`.
3. Print one line per changed project: its eligible test projects, or its exclusion reason, or the not-in-policy line. This holds under `-DryRun` too (AC3).
4. A valid entry that matches no changed project produces no output.
5. Run Stryker on every eligible project and report each score, even when an unlisted ineligible project exists (Scope 2).
6. Then compute the verdict per §4.3. Under `-DryRun`: no Stryker; return 1 if any unlisted ineligible project exists or config is invalid. A DryRun success is a classification check, never mutation proof (Q3).

### 4.3 Exit-code table (Q3) — goes verbatim into the script help and `AGENTS.md`

| Exit | Verdict | Meaning | Blocking |
|---|---|---|---|
| 0 | `PASSED` | At least one eligible changed project was mutated, every eligible result is at or above `gates.mutation.threshold`, each configured exclusion is printed as NOT APPLICABLE with its reason, and no unlisted ineligible project exists. | — |
| 1 | `FAILED` | A score below threshold, a Stryker failure, invalid configuration, or any changed project with no eligible test project that is not in `gates.mutation.exclusions`. | Yes, on every tier |
| 2 | `SKIPPED` | No production C# under `src/` changed (scope-empty). | Yes; never green |
| 2 | `NOT APPLICABLE` | Every changed project is explicitly listed in `gates.mutation.exclusions`; nothing was mutated. | No; reported as N/A, never PASS |

Threshold stays read from `harness.yml` (currently 80); no reduction, no hardcoded substitute.

### 4.4 Stale text (AC4)

The `:564` message and the `:12-17` header comment name both `*.ArchitectureTests` and `*.IntegrationTests`, and the new message names the policy (`gates.mutation.exclusions`). Wording is Quill's to draft, Keel checks it against the code.

### 4.5 Compatibility (Q10)

Preserve the output seams `scripts/new-mutation-receipt.ps1:337-351` reads: the raw gate exit, the final `Mutation testing:` verdict line, and `Stryker native exit for <project>.csproj: <n>`. Excluded projects produce no report and stay unmeasured, never successful receipts. Runtime compatibility is verified in Phase B, no edit to the helper.

## 5. Test strategy (Q6)

- `scripts/Test-RunMutation.ps1`, plain PowerShell in the style of `scripts/hooks/Test-*.ps1`, no Pester. It creates a temporary checkout, overlays the gate files under test (the pattern of `scripts/new-mutation-receipt.ps1`), writes a temporary `harness.yml`, and drives `run-mutation.ps1 -DryRun`. It cleans up after itself and exits non-zero on any failed case.
- Five cases, each asserting exit code and the classification lines:
  1. Api + Infrastructure changed, Api listed → Infrastructure lists `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`; Api NOT APPLICABLE with reason.
  2. Api only changed, Api listed → exit 2, `NOT APPLICABLE`.
  3. Api + Infrastructure changed, Api unlisted → exit 1, both classification lines printed.
  4. Empty reason → exit 1, configuration error.
  5. Listed project that has eligible tests → WARNING line, still excluded.
- Real-run evidence, recorded separately (dry-run fixtures are not mutation evidence): `pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef 9f92ad1` (no `-DryRun`), verbatim verdict lines and exit code in the PR body.
- DEV-309 retrospective (Q8): Phase B recon establishes and verifies DEV-309's pre-merge base SHA; Gauge runs `-Project LamuFlix.Infrastructure -BaseRef <verified-sha>` and records the project score plus per-validator results for `BrowseMoviesQueryValidator` and `GetMovieDetailsQueryValidator`. Anvil kills survivors in those two validators with `LamuFlix.UnitTests` tests inside the implement box. Every survivor outside those files, or needing an out-of-box change, goes to Rigger as a duplicate-checked follow-up ticket with an explicit disposition. A follow-up never turns a failing applicable gate into PASS.

## 6. `task-pipeline` note replacement text (Q7)

Bernstein applies this in Phase B and reads it back into the PR body. Replace Phase 3 step 5's list by inserting, after the `scripts/run-jetbrains-inspectcode.ps1` bullet (current note line 94):

```
   - `scripts/run-mutation.ps1` (M and L, whenever the diff changes production C# under `src/`). Exit 0 = PASSED. Exit 1 = FAILED and blocks on every tier, including an unlisted project with no eligible test project. Exit 2 `NOT APPLICABLE` (every changed project listed in `harness.yml` `gates.mutation.exclusions`) is non-blocking and reported as N/A, never PASS. Exit 2 `SKIPPED` (no production C# under `src/`) is scope-empty and never green. Non-blocking mutation N/A comes only from the script's exit-2 `NOT APPLICABLE` verdict, never from a seat ruling or a waiver such as "N/A for M".
```

Step 4 (`L only: Anvil runs /architect`) stays unchanged and separate.

## 7. Gate expectations (Q9)

Gauge records every command with its exit code:

- `run-roslyn-analyzers.ps1` and `run-cyclomatic-complexity.ps1`: `-Files` on changed `.cs` (F6 tests); if no `.cs` changed, `-All`. Exit 0.
- `run-jetbrains-inspectcode.ps1`: no arguments, never `-All` (`task-pipeline:94`). Exit 0, or exit 2 scope-empty recorded as such, never claimed a pass.
- `run-property-tests.ps1` exit 0; `run-vulnerable-packages.ps1` exit 0; `dotnet format --verify-no-changes` exit 0; `dotnet test` exit 0; `run-web-gates.ps1` exit 2 (disabled).
- `pwsh -NoProfile -File ./scripts/Test-RunMutation.ps1` exit 0.
- `run-mutation.ps1` on this ticket's own diff: command, verdict, and exit recorded verbatim; expected exit 2 SKIPPED (scope-empty), never claimed PASS. Its acceptance depends on OD-2.
- The two real runs of §5, with scores and survivor dispositions.

## 8. Task ordering constraints

1. Phase B recon first: drift vs `1e6830b` on the F1-F5 file set, and DEV-309's pre-merge base SHA (needed by step 7).
2. F2 parser map type with its validation before F1 reads `gates.mutation.exclusions`; F3 entry with F2.
3. F5 harness cases written before or with the F1 classification change (they define the behaviour of §4.2-4.3).
4. F1 classification, run-eligible, verdict and exit table; then the `:564` message, header, and help in the same file.
5. F4 `AGENTS.md` table after F1, copied from the help text so the two cannot differ.
6. Cog `/refactor` over the build.
7. Gauge real runs (§5): `-BaseRef 9f92ad1`, then the DEV-309 retrospective. F6 survivor tests (Anvil) depend on the retrospective result; re-run the retrospective after them.
8. Bernstein applies the §6 note text, reads it back.
9. Full gates per §7.

## 9. Caps

- Grill: 10 of 12 questions used (Q1-Q10).
- Review: 2 rounds maximum; third-round findings go to the user with the PR.
- Remediation: 2 fix commits per round.
- Stage boxes: implement 90m, refactor 30m, review 30m per axis.

## 10. Grill answers (summary; full rulings in `CONCLUSIONS.md`)

| Q | Ruling |
|---|---|
| Q1 | Plan on (a); (a)/(b) is owner checkbox OD-1, Gate 1 closed until answered. |
| Q2 | `gates.mutation.exclusions` map, project name -> nonblank reason; parser map type for that prefix only; Api only listed; Worker/Web fail closed. |
| Q3 | Exit table §4.3 accepted; DryRun success is never mutation proof; threshold from `harness.yml`. |
| Q4 | Classify all, run every eligible, then exit 1 naming each unlisted ineligible project; DryRun prints every project and returns 1 likewise. |
| Q5 | Listed-but-eligible stays excluded with WARNING; unmatched entries silent; all entries validated first. |
| Q6 | `scripts/Test-RunMutation.ps1`, no Pester, 5 cases; real `-BaseRef 9f92ad1` run recorded separately. |
| Q7 | Mutation joins Phase 3 step 5 for M and L on `src/` C# diffs; Keel records text (§6), Bernstein applies; L `/architect` unchanged. |
| Q8 | Retrospective `-Project LamuFlix.Infrastructure` against the verified pre-merge base; Anvil kills survivors in the two validators; others become follow-ups via Rigger. |
| Q9 | AC7 replacement is owner checkbox OD-2; record own-diff run verbatim; analyzers `-Files`/`-All`, InspectCode never `-All`. |
| Q10 | Frozen scope §3 accepted, contingent on OD-1/OD-2. |
