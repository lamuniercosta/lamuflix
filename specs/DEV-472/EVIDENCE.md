# DEV-472 — Evidence ledger

Runtime acceptance is **not** claimed here. Layers 1 (static assertions) and 2 (`scripts/Test-RunMutation.ps1`) are recorded below; hosted runtime receipts are T008–T011 and remain **pending**.

- Worktree: `F:/Dev/LamuFlix.worktrees/DEV-472`
- Branch: `feature/DEV-472`
- HEAD at validation: `180e51ddb96af07713bf738740a3aad8a0128c22` (T004/T005, US2)
- Comparison base: `origin/main` = `b98a4b0a6287673e56015145862be43c2e3dd3d0`
- Implementation file under assertion: `.github/workflows/ci.yml`

## Layer 1 — Mechanical static assertions (T006)

Command (run from the worktree root; PyYAML 6.0.3 on Python 3.14.0, `PYTHONIOENCODING=utf-8`; the assertion script lives outside the worktree at `%TEMP%\dev-472-t006-assert.py` and is **not** added to the repository — T006 forbids a new dependency, framework or script file):

```powershell
python "$env:TEMP\dev-472-t006-assert.py"
```

Exit `0`; `RESULT ALL-PASS`; 32 assertions PASS, 0 FAIL.

### YAML structure and four-event truth table

Each job `if:` is evaluated exactly as GitHub reads it (`==`, `&&`, `||`, `github.event_name`, `github.ref`), against a `push` to `refs/heads/main`.

| Job | `if:` condition | pull_request | push | schedule | workflow_dispatch | Expected | Observed |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `mutation-pr` | `github.event_name == 'pull_request'` | **true** | false | false | false | matches | PASS |
| `stryker-core` (16 shards) | `github.event_name == 'schedule' \|\| github.event_name == 'workflow_dispatch'` | false | false | **true** | **true** | matches | PASS |
| `stryker-core-gate` | `github.event_name == 'schedule' \|\| github.event_name == 'workflow_dispatch'` | false | false | **true** | **true** | matches | PASS |
| `publish-images` | `github.event_name == 'push' && github.ref == 'refs/heads/main'` | false | **true** | false | false | matches | PASS |
| `property-tests` | *(none)* | true | true | true | true | matches | PASS |
| `build` | *(none)* | true | true | true | true | matches | PASS |

Note: `publish-images` true on `push`/`main` is the preserved push+main publish guard (FR-008). PR, schedule and dispatch are negative for publication — the scheduled run cannot publish an image.

### Structural, identity, and preserved-policy assertions

| Assertion | Command / evidence | Expected | Observed | Verdict |
| --- | --- | --- | --- | --- |
| Workflow parses as a YAML mapping | `yaml.safe_load` of `ci.yml` | mapping | top-level keys `name`, `on`, `permissions`, `jobs` | PASS |
| Four triggers present, no extra | parsed `on:` keys | `pull_request, push, schedule, workflow_dispatch` | same | PASS |
| Schedule cron exact | parsed `on.schedule` | `[{'cron': '17 3 * * *'}]` | same | PASS |
| `push` limited to `main` | parsed `on.push.branches` | `['main']` | `['main']` | PASS |
| `pull_request` limited to `main` | parsed `on.pull_request.branches` | `['main']` | `['main']` | PASS |
| Stable check identity | job id / display name | `mutation-pr` / `PR mutation` | `mutation-pr` / `PR mutation` | PASS |
| PR job least privilege | `jobs.mutation-pr.permissions` | `{contents: read}` | `{contents: read}` | PASS |
| No `concurrency:` anywhere | workflow-level key and per-job keys | absent | absent (workflow False, jobs `[]`) | PASS |
| No `cancel-in-progress` text | substring count in `ci.yml` | 0 | 0 | PASS |
| Single `property-tests` job | job key match | exactly one | `['property-tests']` | PASS |
| Two distinct property project calls | step `run:` bodies | UnitTests + IntegrationTests once each | both present, one each | PASS |
| 16-shard matrix | `jobs.stryker-core.strategy.matrix.shard` | `[1..16]` | 16 entries | PASS |
| Gate depends on shards | `jobs.stryker-core-gate.needs` | `stryker-core` | `stryker-core` | PASS |

### Immutable history / base / head preparation (`mutation-pr`)

| Assertion | Expected | Observed | Verdict |
| --- | --- | --- | --- |
| Checkout of immutable payload head | `ref: ${{ github.event.pull_request.head.sha }}` | same | PASS |
| Full history for diffing | `fetch-depth: 0` | `0` | PASS |
| Explicit immutable base fetch | `git fetch --no-tags origin $env:PR_MUTATION_BASE_SHA` | present, in the one preparation step | PASS |
| Both commits verified | `git cat-file -e` for base and head | both present | PASS |
| Event SHAs supplied via env | `PR_MUTATION_BASE_SHA` / `PR_MUTATION_HEAD_SHA` from `github.event.pull_request.*.sha` | both wired | PASS |
| Preparation fails closed | `$ErrorActionPreference = 'Stop'` + 4 distinct `throw` paths (blank SHA, fetch failure, base commit missing, head commit missing) | all four present | PASS |

### Runner invocation and exit-translation contract

| Assertion | Expected | Observed | Verdict |
| --- | --- | --- | --- |
| Explicit `-BaseRef`, never `--since` | `pwsh -NoProfile -File $runner -BaseRef $env:PR_MUTATION_BASE_SHA`; zero `--since` in file | present; `--since` occurrences 0 | PASS |
| No blanket `continue-on-error` | none in `mutation-pr` steps | none | PASS |
| Child `pwsh`, exit captured immediately | `$mutationExit = $LASTEXITCODE` before any branch | present, captured before branching | PASS |
| Exit 0 preserves actual PASS | `if ($mutationExit -eq 0) { exit 0 }` | present | PASS |
| Exit 1 / unexpected fails | `throw "the mutation runner exited $mutationExit"` | present | PASS |
| Missing runner fails | `Test-Path` guard + `the mutation runner is missing at` | present | PASS |
| Full log preserved | `*> $logPath` then echoed via `$output` | present | PASS |
| Accepted exit 2 classification verified against the runner, never guessed | wrapper patterns vs `scripts/run-mutation.ps1` text | `Mutation testing: SKIPPED - no production C# files changed \(scope-empty\)` matches `run-mutation.ps1:1026,1085`; `Mutation testing: NOT APPLICABLE` matches `run-mutation.ps1:1202` | PASS |
| Unclassified exit 2 fails closed | `exited 2 without reporting an accepted scope-empty or NOT APPLICABLE verdict` | present | PASS |
| Exit 2 labelled, wrapper 0, never PASS | `'SKIPPED (scope-empty)'` / `'N/A'` written to `GITHUB_STEP_SUMMARY` with `never a mutation PASS` | present | PASS |

### Full-run reuse and policy preservation

| Assertion | Expected | Observed | Verdict |
| --- | --- | --- | --- |
| Full lane reuses shard script | `scripts/ci-stryker-shard.sh run <shard> 16` | present in `stryker-core` | PASS |
| Merge + evaluator reused | `ci-stryker-shard.sh merge` and `run-mutation.ps1 -EvaluateReport` | both present in `stryker-core-gate` | PASS |
| Generated config not hand-edited in workflow | 0 occurrences of `stryker-config.json` | 0 | PASS |

### Residual exit-2 hosted-label risk (static/synthetic only)

Accepted exit 2 is labelled from the runner's own stdout. Static proof is that the wrapper's two accepted patterns match the only two exit-2 messages `scripts/run-mutation.ps1` can emit (`run-mutation.ps1:1026`, `:1085`, `:1202`); synthetic proof is Layer 2 below, which exercises the same exit-2 paths. **No hosted run has yet been observed producing a `SKIPPED (scope-empty)` or `N/A` PR summary line**, so a hosted label proof is absent. Risk remains recorded as static/synthetic-only under the unchanged Q5 hosted receipt contract, and is tracked as a pending ledger row below.

## Layer 2 — Synthetic runner-contract regression (T007)

```powershell
pwsh -NoProfile -File ./scripts/Test-RunMutation.ps1
```

- Exit `0`, duration 78.2 s
- Tail: `All 64 case invocations passed.`
- Scope: synthetic only. The fixture builds throwaway git repositories and a stub `dotnet`; no real Stryker run on repository code occurs. It is **not** nightly proof and **not** a substitute for hosted acceptance.

## Layer 3a — Applicable harness gates (T007)

Run from the worktree at HEAD `180e51dd` against `origin/main` (`b98a4b0a`). The diff is `.github/workflows/ci.yml` + `specs/DEV-472/tasks.md`, so no production C# and no `/web` change exists.

| Gate | Command | Exit | Verdict (verbatim) |
| --- | --- | --- | --- |
| Roslyn analyzers | `pwsh -NoProfile -File ./scripts/run-roslyn-analyzers.ps1 -BaseRef origin/main` | 2 | `SKIPPED (scope-empty)` — `Roslyn analyzers: SKIPPED - no .cs files changed against origin/main.` |
| Cyclomatic complexity | `pwsh -NoProfile -File ./scripts/run-cyclomatic-complexity.ps1 -BaseRef origin/main` | 2 | `SKIPPED (scope-empty)` — `Cyclomatic complexity: SKIPPED - no .cs files changed against origin/main.` |
| JetBrains InspectCode | `pwsh -NoProfile -File ./scripts/run-jetbrains-inspectcode.ps1` | 2 | `SKIPPED (scope-empty)` — `InspectCode: SKIPPED - no .cs files changed against origin/main.` |
| Web gates | `pwsh -NoProfile -File ./scripts/run-web-gates.ps1` | 2 | `SKIPPED (scope-empty)` — `Web gates: SKIPPED - no web/ files changed` |
| Mutation applicability | `pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef origin/main` | 2 | `SKIPPED (scope-empty)` — `Mutation testing: SKIPPED - no production C# files changed (scope-empty).` (never PASS; nothing mutated) |
| Vulnerable packages | `pwsh -NoProfile -File ./scripts/run-vulnerable-packages.ps1` | 0 | `PASS` — `PASS: no vulnerable packages.` |
| Format | `dotnet format --verify-no-changes` | 0 | `PASS` |

No gate is folded into PASS: the five exit-2 results are recorded as `SKIPPED (scope-empty)` because nothing was in scope, and no production scope was fabricated to green a skipped lane. No exit 1 and no `Could not run` occurred.

## Layer 3b — Hosted runtime receipts (T008–T011, PENDING)

Nothing in this section is evidenced. Each row stays **pending** until a real hosted run supplies the receipt. Fixture green, workflow-only scope-empty PR success, and a green hosted check are not mutation PASS and not nightly proof.

| # | Required receipt | Status |
| --- | --- | --- |
| T009a | Non-delivery PR with changed production C# where a mutation regression fails `PR mutation` (PR / run / base / head SHAs) | **pending** |
| T009b | External required-check registration for `PR mutation` / `mutation-pr`, live `gh api` read-back, plus red-check-blocks-merge proof (user-owned; no repository-settings write from this plan) | **pending** |
| T010 | Main-push run showing no Stryker job ran (SC-002) | **pending** |
| T011 | Green scheduled run on the delivered `main` revision completing all 16 shards and passing `stryker-core-gate` merged-report evaluation, with run URL and SHA (SC-003). `workflow_dispatch` is preflight only and cannot substitute | **pending** |
| — | Hosted exit-2 label proof (`SKIPPED (scope-empty)` / `N/A` summary line on a real PR) | **pending** — residual risk recorded as static/synthetic-only above |

Static assertion and synthetic fixture success do not replace any row in this table. Required-check registration may remain pending without inventing a structural checkbox (FR-015).
