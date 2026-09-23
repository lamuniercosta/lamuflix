## Rules
See the `aaron-*.md` files in `.claude/rules/vendor/` for C#/.NET coding style, testing conventions, CI/CD, and SDK/dependency management guidelines.

## Verification commands
- Test loop: `dotnet test --filter "FullyQualifiedName~<TestClass>" --nologo -v q`
- Full suite: `dotnet test`
- Format check: `dotnet format --verify-no-changes`

## Static-analysis gates (required)

Run after creating or editing ANY `.cs` file, and before marking a task complete.
The three are NOT interchangeable - each catches a different class of problem, and
`dotnet build` alone surfaces none of them reliably.

- Roslyn analyzers (CA/IDE at warning+): `./scripts/run-roslyn-analyzers.ps1`
- Cyclomatic complexity (<= 15): `./scripts/run-cyclomatic-complexity.ps1`
- Rider/ReSharper inspections: `./scripts/run-jetbrains-inspectcode.ps1`

No args analyses changed `.cs` files vs the default branch; `-Files "a.cs","b.cs"` for an
explicit set; `-All` for the whole solution. Exit 0 = pass, 1 = fail.

A gate whose analyzer is not actually enabled exits 1 with remediation rather than
reporting a pass it did not earn. Never treat a skipped gate as a passed gate.

Refactor gate (after implementation works, deliberately stricter):
`./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`

Fix complexity failures by extracting private helpers, early returns, and guard clauses -
not by suppressing. Requires PowerShell 7 (`pwsh`).

## Documentation lookup (MCP routing)

Two documentation MCP servers are configured in `.mcp.json`. They are NOT
interchangeable - choose by who owns the technology.

- **`microsoft-learn`** - anything Microsoft: .NET, C#, ASP.NET Core, EF Core,
  Azure, MSBuild, NuGet, Roslyn analyzers, Windows APIs.
  Tools: `microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search`.

- **`context7`** - third-party libraries ONLY: Selenium, Serilog, Polly, xUnit,
  MassTransit, Newtonsoft, Reqnroll, FluentValidation, and similar.
  Tools: `resolve-library-id` first, then `query-docs`.

**Rule: never use Context7 for Microsoft-owned technology.** Microsoft Learn is
the authoritative, version-accurate source there. Context7 covers the rest of
the ecosystem.

When a question spans both - say Serilog sinks wired into ASP.NET Core - query
`microsoft-learn` for the framework side and `context7` for the library side,
rather than picking one for the whole question.

`context7` works anonymously - verified, an unauthenticated request returns 200.
`CONTEXT7_API_KEY` only raises rate limits (free tier is about 500 requests per
month). So never report context7 as unavailable merely because the key is unset.
On a 429, fall back to normal web search for third-party docs and say so, rather
than substituting Microsoft Learn for a non-Microsoft library.

## Always-on rules

Imported from `.cursor/rules/` deliberately. These three exist in exactly one place:
Cursor requires them at that path to load them, and an `@import` resolves any
relative path, so copying them into `.claude/rules/` would create two files that
drift. One file, one home, both platforms.

Five C# gate rules auto-load when editing `.cs` files. Three procedure rules
(`github-workflow`, `readme-maintenance`, `architect-gate`) load through their
pipeline skills.

If you are working Claude-only, do not delete `.cursor/` — every import below
resolves into it.

@.cursor/rules/agent-pipeline.mdc
@.cursor/rules/delegation.mdc
@.cursor/rules/documentation-sources.mdc

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
<!-- SPECKIT END -->
