# Development instructions

This repository uses **dotnet-agent-harness**. The pipeline, gates, and conventions
below are enforced by scripts, not by good intentions.

Codex, OpenCode, Gemini CLI, Antigravity, and Junie read this file natively or via configuration pointers. Unlike the Cursor and
Claude Code adapters, it **imports nothing** — the three always-on rules are distilled here rather than referenced. The eight
scoped rules load through skill invocation (and, on Cursor/Claude, glob/`paths` matching). `.cursor/rules/*.mdc` is the canonical source for each; if this file
and those ever disagree, those files are the source and this one is stale —
say so rather than picking one silently.

Read [Limitations under Codex](#limitations-under-codex) before your first session. Project hooks must be reviewed before their safety nets run.

## Non-negotiables

- `TimeProvider` for time, injected seeded `Random` for randomness. `DateTime.Now`
  is in `BannedSymbols.txt` and **fails the build** (RS0030 is an error).
- A surviving mutant is a missing test. Fix the test, never the threshold.
- Match the repo's existing architecture, assertion library, and mocking library.
  Introducing a second one is a defect.
- No explanatory comments. In tests, `// arrange` / `// act` / `// assert` are the
  one exception.
- Never mock the data-access driver for behaviour that depends on real query
  translation or serialisation — use Testcontainers.
- Never hardcode a threshold, and never edit a generated file — the next install
  overwrites it.
- Never lower a gate threshold to make a gate pass. That is the one move the
  pipeline exists to prevent.

## Cost-aware delegation

Agent tiers reflect the cost of a silent miss, not apparent difficulty. Use a
`fast` named agent for a cheap errand only when all four conditions hold:

1. You need a compact answer, not source material you will quote, edit, or
   reason over line by line.
2. The material to inspect is much larger than the returned answer.
3. The brief is short and complete without replaying accumulated conversation.
4. The work is one-shot and should not need clarification.

Keep the work inline when you need the contents afterward, the brief transfers
substantial context, trusting the result requires re-reading the source, or one
tool call answers the question. The governing asymmetry is: **delegate
conclusions; keep required content inline.** There is no numeric threshold.

A cheap errand may gather evidence for a verdict you will make, but may not make
semantic verdicts, feed a human gate, or perform unspecified writes. It also may
not feed a grilling session in progress — that follows from the four conditions,
not from an exception to them: a fact put to a live interrogation is material you
reason over to form the next question (1), and its brief cannot be complete
without replaying the conversation so far (3). One carve-out: a single
reconnaissance pass before the first question, where no conversation exists yet
to replay. That pass includes reading deferred format files (`CONTEXT-FORMAT.md`,
`ADR-FORMAT.md`); those are facts, not decisions. A mid-grill lull is not a new
pre-grill pass. `gate-runner`
has one mechanical exception: it may translate a reported command and exit code
into `Pass`, `Failure`, `Skipped`, `Opt-out`, or `Could not run`; it may not dismiss
findings, judge equivalent mutants, or overrule tool evidence.

- **No unspecified writes.** If any part of an edit still needs deciding, keep it
  with the parent.

Delegate a writable errand to `edit-applier` only when the brief gives one exact
transformation, an explicit file set, and an objective check. Those files belong
exclusively to the errand until it returns. Do not edit them concurrently, and
parallel writable errands must have disjoint file sets. Review the diff afterward.
If partial edits already exist, review the current diff and continue from it; do
not roll back automatically.

### One strike

If an errand returns ambiguity, a partial answer, or a result you cannot trust,
finish it inline and do not re-brief the cheap agent.

## Documentation sources

When a question turns on a specific API's surface or semantics, query the MCP
documentation servers rather than answering from memory or a web search:

- **microsoft-learn** — .NET/C#, ASP.NET Core, Azure (Functions, Service Bus, Blob
  Storage, Entra, Key Vault, App Insights), MSBuild, Roslyn `CA*`/`IDE*` rules.
  Microsoft Learn always wins on a Microsoft or Azure topic.
- **context7** — everything else: MongoDB C# driver, Polly, xUnit, FsCheck,
  Reqnroll, NSubstitute, Testcontainers, WireMock.NET, Stryker.NET, k6.

**Rule: never use Context7 for Microsoft-owned technology.** Microsoft Learn is
the authoritative, version-accurate source there. Context7 covers the rest of
the ecosystem.

When a question spans both — say Serilog sinks wired into ASP.NET Core — query
`microsoft-learn` for the framework side and `context7` for the library side,
rather than picking one for the whole question.

`context7` works anonymously — an unauthenticated request returns 200.
`CONTEXT7_API_KEY` only raises rate limits. So never report context7 as unavailable
merely because the key is unset. On a 429, fall back to normal web search for third-party docs.

Cite the source when a doc answer drives a decision. Skip the lookup when
refactoring working code or discussing design in the abstract.

These servers are registered in `.codex/config.toml`, `.cursor/mcp.json`, `opencode.json`,
`.gemini/settings.json`, and `.junie/mcp/mcp.json`.

## Verification commands

```powershell
./scripts/run-roslyn-analyzers.ps1          # CA/IDE/VSTHRD + the security families
./scripts/run-cyclomatic-complexity.ps1     # tightened at the refactor gate
./scripts/run-jetbrains-inspectcode.ps1     # a different engine; catches duplication
./scripts/run-property-tests.ps1
./scripts/run-vulnerable-packages.ps1
dotnet test
dotnet stryker                              # minutes-expensive; pre-PR only
```

The three analyzer gates — `run-roslyn-analyzers.ps1`,
`run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1` — take
`-BaseRef`, `-Files "a.cs","b.cs"`, and `-All`; with no args they analyse the
files changed against the base branch. The rest take their own parameters
(`run-vulnerable-packages.ps1`: `-Severity`, `-IncludeTransitive`;
`run-property-tests.ps1`: `-Project`, `-Category`) and **error out on `-All`**.
Every script accepts `-Help`; ask it rather than assuming a flag.

**Exit 0 = pass, 1 = fail, 2 = SKIPPED or OPT-OUT.**
SKIPPED (scope-empty) is blocking and never green; OPT-OUT (gate disabled in harness.yml) is non-blocking `SKIP`, reported as `SKIP` not PASS.

**A gate that could not run has not passed.** The scripts enforce this themselves:
if the analyzer they depend on is not wired, they exit 1 with remediation rather
than reporting a pass they did not earn. Report an unrunnable gate as `Could not
run`; reserve `SKIPPED`/`SKIP` on exit 2 — `SKIPPED` is blocking and never green; `SKIP` is a non-blocking opt-out. Never fold `SKIPPED`, `SKIP`, or `Could not run` into a green verdict, and
never substitute plain `dotnet build`.

## Vendor rules (Aaronontheweb C# / .NET standards)

The following 8 rules are located at `.cursor/rules/vendor/*.mdc` (and `.claude/rules/vendor/`):

1. **`aaron-csharp-coding-style.mdc`** (`globs: *.cs`):
   Clean, maintainable, idiomatic C# with functional patterns and proper abstractions. Records for immutable data types, expression-bodied members where concise, pattern matching, nullability annotations enabled (`#nullable enable`).
2. **`aaron-csharp-testing.mdc`** (`globs: *Test*.cs, **/*.Tests/**/*.cs`):
   Testing conventions: unit and integration testing structure, naming patterns (`Method_Condition_Expected`), xUnit assertions, test fixtures, avoidance of disk-bound tests.
3. **`aaron-ci-cd-dotnet-build.mdc`** (`globs: *.sln, *.csproj, .github/workflows/*`):
   Continuous integration and build automation for .NET. Deterministic builds, CI workflows, MSBuild property hygiene, test results reporting.
4. **`aaron-ci-cd-code-signing.mdc`** (`globs: *.csproj, .github/workflows/*`):
   Code signing practices, certificate management, assembly signing configuration for build pipelines.
5. **`aaron-dotnet-sdk-dependency-management.mdc`** (`globs: Directory.Packages.props, *.csproj`):
   Central Package Management (CPM) via `Directory.Packages.props`, transitively pinned dependencies, vulnerability audits, and NuGet version hygiene.
6. **`aaron-dotnet-sdk-solution-management.mdc`** (`globs: *.sln, Directory.Build.props, Directory.Build.targets`):
   Solution architecture, clean project layout (`src/` vs `tests/`), shared MSBuild properties in `Directory.Build.props`.
7. **`aaron-dotnet-tools-consuming.mdc`** (`globs: .config/dotnet-tools.json`):
   Standards for declaring and consuming dotnet local tools (`dotnet-tools.json`, `dotnet tool restore`).
8. **`aaron-dotnet-tools-publishing.mdc`** (`globs: *.csproj`):
   Packaging and publishing standards for custom .NET tools.

## Configuration

`harness.yml` at the repo root is the entire configuration surface — thresholds,
base branch, tracker, agent model tiers. It is the **single source of truth**:
`CodeMetricsConfig.txt`, `stryker-config.json`, and the `.editorconfig` gate
severities are all rendered from it by `install.ps1` on every run.

Task intake is **tracker-neutral**: `tracker` is `youtrack`. `$task <id>` dispatches through `scripts/get-task.ps1`. There is no
CLI tracker override. GitHub remains the code host for remotes, commits, and PRs.

## Limitations under Codex

### Lifecycle hooks require trust, and cannot scan file reads
The harness installs `.codex/hooks.json` with four protections:
- `secret-scan.ps1` warns when a submitted prompt contains a credential shape;
- `guard.ps1` blocks destructive shell commands, writes outside the session worktree boundary, and protected file writes;
- `format-on-edit.ps1` formats edited C# files; and
- `gate-nudge.ps1` reminds the agent that analyzer gates remain pending.

Project-local hooks run only after the project is trusted and each hook definition has been reviewed. Open `/hooks` when Codex reports unreviewed hooks.

Codex currently exposes no file-read lifecycle event. Never ask Codex to read a file containing a live credential. `gitleaks` in CI covers the commit-time half.
