# Feature Specification: Build-configuration hardening — central versions, strict compilation, banned ambient time

**Feature Branch:** `claude/ticket-scope-cs-edits-3ed088` (base `origin/main` @ `aa02408`)

**Created:** 2026-09-22

**Status:** Draft (pending human gate 1 — the B1 Scope amendment is open in YouTrack)

**Input:** YouTrack `DEV-289` — "Add global.json, Directory.Packages.props, .editorconfig, and BannedSymbols.txt" (read via `scripts/get-task.ps1`). Type: feature.

**Related:** `specs/DEV-289/brief.md` · `specs/DEV-289/CONCLUSIONS.md` · `.specify/memory/constitution.md` (Principle VII; Technology Stack Constraints; Static-Analysis Gates) · `specs/PRODUCT.md` §5

## Gate 1 — Owner decisions (B1–B5) and authorization envelope

This specification is bounded by five confirmed owner decisions recorded in `CONCLUSIONS.md`.
They are restated here because they govern the ticket, and because **no implementation begins in this spec PR.**

- **B1 — File Scope, option (a); confirmed; YouTrack Scope item 6 pending.** The ticket's acceptance
  criteria cannot be met inside its named file set. Measured on this branch: applying the ticket's
  settings (`-p:TreatWarningsAsErrors=true -p:Nullable=enable`) produces **168 errors across 33 unnamed
  files**, on top of **3 banned-symbol hits in 2 unnamed files** — **35 unnamed source files in total
  (34 `.cs` + 1 Razor view, `LamuFlix.Web/Views/Filmes/Index.cshtml`)**. The owner chose **(a)**: amend
  DEV-289 Scope to name those 35 files, with edits restricted to
  `System.TimeProvider` injection at the three call sites and nullable/analyzer fixes with **no behaviour
  change**. That amendment is a **pending owner action in YouTrack** and is not retroactive; **until it is
  live the 35 files are not authorized.**
- **B2 — `BannedSymbols.txt` retains both sets.** The five ticket-mandated time symbols plus the two
  pre-existing sync-over-async bans (`Task.Wait`, `Task.WaitAll`) = **seven entries**; every time-symbol
  message points at `System.TimeProvider`, never at a itself-banned API.
- **B3 — Closing bar.** `/code-review` and `/ship-review` are blocked by every unresolved substantive
  Critical or High finding. Medium, Low, and Info findings are recorded and either fixed or tracked as
  follow-ups. Process/tooling observations are non-findings unless they establish a concrete product or
  verification risk.
- **B4 — Frozen scope.** Ticket-named configuration files and the four `.csproj` `Version` removals, plus
  the precisely enumerated 35 source files (34 `.cs` + 1 Razor view) **only after** Scope item 6 is live.
  *Anything else is a follow-up issue, not a finding in this round.*
- **B5 — Round cap.** The default maximum is two review rounds; no third review may repair findings or
  review transport.

**Authorization envelope**

| Set | Files | Authorized now? |
|---|---|---|
| Named configuration (Scope items 1–5) | `global.json` (new), `Directory.Packages.props` (new), `Directory.Build.props` (edit), `.editorconfig` (edit), `BannedSymbols.txt` (edit) | **Yes** — ticket-named |
| `Version` removal | `LamuFlix.Web/LamuFlix.Web.csproj`, `LamuFlix.Data/LamuFlix.Data.csproj`, `LamuFlix.Work/LamuFlix.Worker.csproj`, `LamuFlix.Test/LamuFlix.Test.csproj` | **Yes** — ticket-named |
| B1 remediation (Scope item 6) | 35 source files = 34 `.cs` + 1 Razor view: the two banned-symbol files (`LamuFlix.Web/Controllers/FilmesController.cs`, `LamuFlix.Work/Worker.cs`) and the 33 nullable/analyzer files listed in `brief.md` §Appendix (16 Data + 14 Web incl. `Views/Filmes/Index.cshtml` + 3 Test) | **No — blocked until the YouTrack Scope item 6 amendment is live** |

No other existing file may be edited. `brief.md` and `CONCLUSIONS.md` are preserved verbatim.

## User Scenarios & Testing *(mandatory)*

The actor is a **maintainer of the repository**; this ticket is `ui: none`. Each story is an
independently observable outcome of the build configuration.

### User Story 1 - Package and SDK versions come from one place (Priority: P1)

A maintainer bumps a package once, in a single file, and every project picks it up; the SDK patch
level is pinned and reproducible across machines.

**Why this priority**: Central Package Management and a pinned SDK are the foundation the other two
stories build on, and the `Version`-attribute removal is explicitly named in the ticket. It delivers
value on its own with no `.cs` edits.

**Independent Test**: `global.json` pins the exact SDK `10.0.400` with `rollForward: latestPatch`;
`Directory.Packages.props` enables CPM and lists every package once; no `<PackageReference>` anywhere
carries a `Version` attribute; `dotnet build LamuFlix.sln` resolves versions from `Directory.Packages.props`.

**Acceptance Scenarios**:

1. **Given** the new `global.json`, **When** the SDK is selected in a fresh shell, **Then** the exact
   `10.0.400` SDK is used with `rollForward: latestPatch` and `allowPrerelease: false`.
2. **Given** `Directory.Packages.props` with `ManagePackageVersionsCentrally=true`, **When** any project
   is built, **Then** no `<PackageReference>` defines `Version` and every version resolves centrally.
3. **Given** a package version is edited only in `Directory.Packages.props`, **When** the solution is
   rebuilt, **Then** all referencing projects pick up the new version with no `.csproj` edit.

---

### User Story 2 - Ambient time APIs are a compile error (Priority: P1)

A developer who writes `DateTime.Now` (or any of the four companion APIs) is stopped by the compiler,
with a message that names `System.TimeProvider` as the replacement.

**Why this priority**: This is the ticket's headline enforcement goal and the concrete acceptance
criterion ("adding a line using `DateTime.Now` triggers a compilation error"). It is the guardrail that
keeps nondeterministic time out of a codebase being modernised.

**Independent Test**: `BannedSymbols.txt` contains the five time symbols plus the two retained `Task` bans;
a scratch line using `DateTime.Now` fails compilation with a `Microsoft.CodeAnalysis.BannedApiAnalyzers`
diagnostic whose message points at `System.TimeProvider`; the existing `Task.Wait`/`Task.WaitAll` rows are
still present **byte-for-byte verbatim**, including the full IDs
`M:System.Threading.Tasks.Task.Wait` and `M:System.Threading.Tasks.Task.WaitAll(System.Threading.Tasks.Task[])`
(never a literal ellipsis).

**Acceptance Scenarios**:

1. **Given** `BannedSymbols.txt` (seven entries) and the analyzer wired, **When** a new line uses
   `DateTime.Now`, **Then** the build fails with a ban diagnostic naming `System.TimeProvider`.
2. **Given** the same configuration, **When** a new line uses `DateTime.UtcNow`, `DateTime.Today`,
   `DateTimeOffset.Now`, or `DateTimeOffset.UtcNow`, **Then** each also fails the build.
3. **Given** the pre-existing `Task.Wait` / `Task.WaitAll` rows, **When** the file is reviewed, **Then**
   both rows are intact and no time-symbol message recommends a banned API.

---

### User Story 3 - Every project compiles strict — warnings-as-errors and nullable on (Priority: P2)

A maintainer sees a solution that builds with zero warnings, with nullable reference analysis enabled
everywhere.

**Why this priority**: This is the ticket's widest-reaching change and the one that cannot go green
inside the named file set — it is the reason B1 exists. It is P2 because it is only unlockable after
the owner's Scope item 6 amendment authorizes the 35-file remediation; until then it is deliberately
**blocked**.

**Independent Test**: with `TreatWarningsAsErrors=true` and `Nullable=enable` global, a **non-incremental**
build (`dotnet build LamuFlix.sln --no-incremental`, or from a clean checkout) exits 0 with 0 errors and 0
warnings; a warm incremental build is not proof. Reverse-checking the KPI: disabling the settings against
the unchanged tree reproduces the 168 diagnostics measured in B1.

**Acceptance Scenarios**:

1. **Given** `Directory.Build.props` sets `TreatWarningsAsErrors=true`, `Nullable=enable`, and
   `EnforceCodeStyleInBuild=true`, **When** the solution builds, **Then** it exits 0 with 0 warnings.
2. **Given** the nullable analysis is on, **When** each project compiles, **Then** no `CS8618`/`CS8600`
   family diagnostic remains (the 155 nullable diagnostics in B1 are resolved).
3. **Given** the `.editorconfig` formatting/naming rules, **When** the authorized changed set is
   formatted, **Then** `dotnet format LamuFlix.sln --verify-no-changes --include <authorized files>` passes
   for that set (scoped to the solution and the exact authorized files, never un-scoped over the whole repo).

---

### Edge Cases

- **The strict settings break the build before the remediation lands.** `Directory.Build.props` and
  `BannedSymbols.txt` are ticket-named and land as specified, but the 35 unauthorized files then fail.
  The green-build acceptance criterion is therefore **only satisfiable once Scope item 6 is live** — this
  is B1, option (a), and it is why the 35 files are gated rather than optional.
- **A `Now → UtcNow` swap is not an available fix.** The ticket bans `DateTime.UtcNow` and
  `DateTimeOffset.UtcNow` too, in favour of `System.TimeProvider`. The three call sites need a
  `TimeProvider` injected; there is no one-token substitution.
- **The committed `BannedSymbols.txt` contradicts the ticket.** It bans `Now`/`Today` but *recommends*
  `UtcNow` in its messages. It is ticket-named, so rewriting it needs no owner checkbox (B2).
- **A `Version` attribute also lives outside the four `.csproj`s.** `Directory.Build.props` carries
  `<PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0">`. Under CPM a
  `Version` on any `PackageReference` is an `NU1008` error, so that version must move to
  `Directory.Packages.props` as well. `Directory.Build.props` is ticket-named, so this is in scope.
- **BannedApiAnalyzers is a new dependency.** Wiring `Microsoft.CodeAnalysis.BannedApiAnalyzers` adds a
  package; it is explicitly named in the ticket's Scope item 5, so it is pre-approved (not a §5.1 blocker).
- **Warnings-as-errors vs the analyzer gate config.** `harness.yml` currently sets
  `analyzers.warningsAsErrors: false`. The AC requires warnings-as-errors for the *build*; the
  relationship between the MSBuild property and the gate script must be stated, not assumed.
- **Overlap with DEV-290.** `LamuFlix.Data/Models/Temp.cs` and the migration `.Designer.cs` files are
  edited here and moved or deleted by DEV-290. Accepted as churn; DEV-289 lands first (B1 implication 4).
- **Forbidden-but-pre-existing packages.** The solution currently uses Pomelo/MySQL, MSTest, and Moq,
  which the constitution forbids for new work. DEV-289 centralises their versions; it does **not** convert
  them or add new usages (Known Technical Debt).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A new root `global.json` MUST pin the .NET SDK to the exact numeric version `10.0.400` with
  `rollForward: "latestPatch"` and `allowPrerelease: false`. A wildcard such as `10.0.x` MUST NOT be used
  (`sdk.version` requires a full version; `CONCLUSIONS.md` D4(a)).
- **FR-002**: A new root `Directory.Packages.props` MUST enable Central Package Management
  (`ManagePackageVersionsCentrally=true`) and consolidate every package version used across the solution.
- **FR-003**: Every `Version` attribute on a `<PackageReference>` MUST be removed — from the four named
  `.csproj` files **and** from `Directory.Build.props` (the `Microsoft.CodeAnalysis.NetAnalyzers` row) —
  so no CPM `NU1008` error remains.
- **FR-004**: `Directory.Build.props` MUST enable `TreatWarningsAsErrors=true`, `Nullable=enable`, and
  `EnforceCodeStyleInBuild=true`.
- **FR-005**: `.editorconfig` MUST standardise repo-wide formatting and naming rules, including
  file-scoped namespaces.
- **FR-006**: `BannedSymbols.txt` MUST ban exactly the five ticket time symbols
  (`System.DateTime.Now`, `System.DateTime.UtcNow`, `System.DateTime.Today`, `System.DateTimeOffset.Now`,
  `System.DateTimeOffset.UtcNow`) **and** retain the two pre-existing `Task` rows **byte-for-byte verbatim**
  (seven entries total): `M:System.Threading.Tasks.Task.Wait` and
  `M:System.Threading.Tasks.Task.WaitAll(System.Threading.Tasks.Task[])` — full documentation-comment IDs,
  with no literal ellipsis in the ID. Every time-symbol message MUST point at `System.TimeProvider` (B2).
- **FR-007**: `Microsoft.CodeAnalysis.BannedApiAnalyzers`, pinned to the ticket-approved `3.3.4` (Scope
  item 5; `CONCLUSIONS.md` D4(b)), MUST be configured with `BannedSymbols.txt` registered as an
  `AdditionalFiles` input, so a `DateTime.Now` line is a **compilation error**.
- **FR-008**: A non-incremental `dotnet build LamuFlix.sln --no-incremental` MUST succeed with **0 errors
  and 0 warnings** under the new settings.
- **FR-009**: The 35 source files enumerated under B1 (34 `.cs` + the Razor view
  `LamuFlix.Web/Views/Filmes/Index.cshtml`) MUST NOT be edited until the DEV-289 Scope item 6 amendment is
  live in YouTrack; when authorized, edits are restricted to `TimeProvider` injection at the three call
  sites and nullable annotations / the listed analyzer fixes (CS8981, IDE0305, CA1822, CA1502 — by
  extraction only, SYSLIB0014) with no behaviour change.
- **FR-010**: `TimeProvider` injection MUST use an optional constructor parameter defaulting to
  `TimeProvider.System`, so no host `Program.cs`/`Startup.cs` registration edit is required.
- **FR-011**: No file outside the named set and the (post-amendment) authorized 35-file set may be edited
  (B4 frozen scope).
- **FR-012**: No secret, token, connection string, or machine-specific path may be introduced by this
  ticket.

### Key Entities

- **Build configuration file**: a root MSBuild/editor file that governs the solution — `global.json`,
  `Directory.Packages.props`, `Directory.Build.props`, `.editorconfig`, `BannedSymbols.txt`. Identified
  by path; each is either new or an edit to an existing ticket-named file.
- **Package reference**: a `<PackageReference>` item; under CPM it carries no `Version` (the version lives
  in `Directory.Packages.props`). 14 such attributes exist across the four `.csproj` files, plus 1 in
  `Directory.Build.props`.
- **Banned symbol entry**: a `documentation-comment-id;message` row in `BannedSymbols.txt`; the ticket
  adds two time rows, corrects three messages, and retains two `Task` rows.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `dotnet build LamuFlix.sln --no-incremental` (or a clean-checkout build) exits 0 with **0
  errors and 0 warnings**; a warm incremental build is not proof.
- **SC-002**: A newly added `DateTime.Now` line fails the build with a BannedApiAnalyzers error whose
  message names `System.TimeProvider`; the same holds for the four companion time APIs.
- **SC-003**: Every project compiles with `Nullable=enable` and `TreatWarningsAsErrors=true`; the 155
  nullable diagnostics measured in B1 are gone.
- **SC-004**: `Version=` appears on no `<PackageReference>` in the four `.csproj` files or in
  `Directory.Build.props`; versions resolve from `Directory.Packages.props`.
- **SC-005**: `git diff --name-only` touches only the ticket-named files and — only after Scope item 6 is
  live — the 35 enumerated source files (34 `.cs` + 1 Razor view). `brief.md` and `CONCLUSIONS.md` are
  unchanged.
- **SC-006**: `BannedSymbols.txt` contains exactly seven rows: the five time symbols and the two retained
  `Task` rows, the latter preserved byte-for-byte with full IDs
  `M:System.Threading.Tasks.Task.Wait` and `M:System.Threading.Tasks.Task.WaitAll(System.Threading.Tasks.Task[])`;
  no message recommends a banned API.

## Assumptions

- The branch base is `origin/main` @ `aa02408`; no concurrent structural change is in flight except
  DEV-290, which lands after DEV-289 (B1 implication 4).
- The five configuration files are ticket-named (Scope items 1–5) and therefore pre-approved; no §5.1–§5.5
  structural checkbox is required for them. `Microsoft.CodeAnalysis.BannedApiAnalyzers` is named in Scope
  item 5 and is likewise pre-approved; its exact version `3.3.4` is fixed by the owner decision
  `CONCLUSIONS.md` D4(b), not assumed.
- The 35-file B1 remediation (34 `.cs` + 1 Razor view) is **not authorized** until the owner pastes the
  proposed Scope item 6 into DEV-289 in YouTrack; this spec PR performs no implementation, and enforcement
  configuration (T011/T012/T016) must not be merged before that amendment is live.
- Acceptance tests (Gherkin/Reqnroll) are **opted out**: this is a build-tooling ticket whose verifiable
  outcomes are build results and analyzer diagnostics, not runtime behaviour.
- `harness.yml` `analyzers.warningsAsErrors: false` remains the *gate* setting; the AC's
  warnings-as-errors is the MSBuild property in `Directory.Build.props`. The two are reconciled in
  `plan.md` §Constitution Check rather than assumed equal.
- No `web/` (Vite) build is affected; the ticket does not touch the frontend.
