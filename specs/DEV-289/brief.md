# DEV-289 — Add global.json, Directory.Packages.props, .editorconfig, and BannedSymbols.txt

**Ticket:** DEV-289 (YouTrack) · **Type:** feature · **Status:** spec ready · gate 1 provisional, pending YouTrack Scope item 6 + owner merge

Ticket Scope names five files — `global.json`, `Directory.Packages.props`, `Directory.Build.props`,
`.editorconfig`, `BannedSymbols.txt` — plus the removal of `Version` attributes from `<PackageReference>`
items in the four `.csproj` files. Acceptance criteria: solution builds with CPM on; a new `DateTime.Now`
line is a compile error; all projects build with warnings-as-errors and nullable enabled.

---

## Owner decisions — gate 1 blockers

Gate 1 stays closed until every box below is checked. Each item is a `PRODUCT.md` §5 structural
decision that Patron may not assume.

### [x] B1 — Structural §5.6 (File scope): the ticket's own acceptance criteria cannot be met inside its named file set

**Why this is blocked.** `PRODUCT.md` §3 makes ticket Scope authoritative; §5 item 6 forbids
"deleting or rewriting any existing file that the ticket does not explicitly name." Turning on the
gates the ticket asks for breaks the build in files the ticket does not name. Measured on this
branch (`dotnet build LamuFlix.sln -p:TreatWarningsAsErrors=true -p:Nullable=enable`, plus a grep
for the five banned symbols):

| Cause | Errors | Unnamed files | Where |
|---|---|---|---|
| Banned symbols (`DateTime.Now`, `DateTimeOffset.Now`) | 3 | 2 | `LamuFlix.Web/Controllers/FilmesController.cs:28`, `LamuFlix.Work/Worker.cs:37,57` |
| `Nullable=enable` + `TreatWarningsAsErrors=true` | 168 | 33 | `LamuFlix.Web` 77 · `LamuFlix.Test` 53 · `LamuFlix.Data` 38 (`LamuFlix.Work` already nullable-clean: 0) |

Of the 168, 155 are nullable diagnostics (97 × CS8618 uninitialised non-nullable member, the rest
CS8600/8602/8603/8604/8625 family); the remainder are CS8981 (lowercase type names), IDE0305,
CA1822, CA1502, SYSLIB0014. The full file list is in the appendix.

Note the ticket bans `DateTime.UtcNow` / `DateTimeOffset.UtcNow` as well, in favour of
`System.TimeProvider`. A one-token `Now → UtcNow` swap is therefore **not** an available fix; the
three call sites need a `TimeProvider` injected. (`BannedSymbols.txt` as currently committed by the
harness bans only `Now`/`Today` and *recommends* `UtcNow` in its messages — it is a named file, so
rewriting it to the ticket's list is in scope and needs no checkbox.)

**Owner: choose one.** — *Decided 2026-09-22: (a). See `CONCLUSIONS.md` § B1 for the exchange and the proposed Scope amendment.*

- [x] **(a) Amend Scope to name the affected files — recommended.** Add the 2 + 33 files in the
  appendix to DEV-289 Scope, with edits restricted to:
  - the three banned call sites: inject `TimeProvider` (optional ctor parameter defaulting to
    `TimeProvider.System`, matching `Worker`'s existing optional-dependency pattern, so no host
    `Program.cs`/`Startup.cs` edit is needed) and call `GetUtcNow()`;
  - nullable annotations only (`?`, `required`, `= null!`, `= []`) and the handful of analyzer
    fixes listed above — **no logic changes**.
  This is the only option that meets the AC as written and `PRODUCT.md` §1's "zero compiler
  warnings" goal. Cost: ~170 mechanical edits, some in files DEV-290 will move or delete
  (`Temp.cs`, migration designers) — harmless churn, but churn.
- [ ] **(b) Keep the gate real for new code, defer legacy cleanup.** Amend Scope to name only
  `FilmesController.cs` and `Worker.cs` (banned symbols fixed as in (a)). Set
  `TreatWarningsAsErrors=true` and `Nullable=enable` globally, but add
  `<WarningsNotAsErrors>nullable</WarningsNotAsErrors>` **in the three legacy `.csproj`s only**
  (`LamuFlix.Web`, `LamuFlix.Data`, `LamuFlix.Test`), with a follow-up ticket to remove it.
  `LamuFlix.Work` and every DEV-291 skeleton build fully strict. Honest cost: the AC "all projects
  build with warnings as errors and nullable enabled" is met only literally — those three projects
  still emit 155 nullable *warnings* — and `PRODUCT.md` §1 "zero compiler warnings" is not met until
  the follow-up lands.
- [ ] **(c) Narrow the AC.** Ship the five named files; keep `TreatWarningsAsErrors=false` and
  per-project `<Nullable>` (current state) and `RS0030` at `warning`. Every gate exists but none
  blocks. Not recommended: it delivers configuration, not enforcement.

Not offered: `#pragma warning disable` / `[SuppressMessage]` at the call sites. That edits the same
unnamed files while hiding exactly what the gates exist to catch.

### [x] B2 — `BannedSymbols.txt`: retain existing `Task.Wait` / `Task.WaitAll` bans alongside the five ticket-mandated TimeProvider symbols

**Why this needed a decision.** The ticket mandates five time-provider bans. The committed
`BannedSymbols.txt` (from the DEV-359 harness adoption) also bans `Task.Wait` and `Task.WaitAll`.
Retaining those rows is not required by the ticket, but dropping them silently weakens a live
guardrail. Neither keep nor drop can be assumed under §5 item 1.

**Owner: retain both sets.** — *Decided 2026-09-22. See `CONCLUSIONS.md` § B2 for the full exchange.*

`BannedSymbols.txt` will contain **seven entries** total:

- `P:System.DateTime.Now`
- `P:System.DateTime.UtcNow` *(new — ticket-mandated)*
- `P:System.DateTime.Today`
- `P:System.DateTimeOffset.Now`
- `P:System.DateTimeOffset.UtcNow` *(new — ticket-mandated)*
- `M:System.Threading.Tasks.Task.Wait` *(pre-existing — retained)*
- `M:System.Threading.Tasks.Task.WaitAll` *(pre-existing — retained)*

All five time-symbol messages must point at `System.TimeProvider` (not `UtcNow`). The `Task` rows
are retained verbatim; removing them is an unauthorized change to a guardrail not named in the ticket.

---

## Appendix — unnamed files that fail under the ticket's settings

Banned symbols (2):

- `LamuFlix.Web/Controllers/FilmesController.cs`
- `LamuFlix.Work/Worker.cs`

Nullable / warnings-as-errors (33; count of diagnostics in parentheses):

`LamuFlix.Data` — `Repositories/GenericRepository.cs` (9), `Models/Movie.cs` (7), `Models/Player.cs` (3),
`Models/Temp.cs` (3), `Models/MovieGenre.cs` (2), `Models/MovieDirectors.cs` (2), `Models/MovieActors.cs` (2),
`Models/Genre.cs` (2), `Models/Actor.cs` (1), `Models/Collection.cs` (1), `Models/Director.cs` (1),
`Repositories/UnitOfWork.cs` (1), `Migrations/20180917031551_initial.cs` (1),
`Migrations/20180917031551_initial.Designer.cs` (1), `Migrations/20181017221210_modifications.cs` (1),
`Migrations/20181017221210_modifications.Designer.cs` (1)

`LamuFlix.Web` — `Models/Filmes/FilmesViewModel.cs` (33), `Services/FilmesServices.cs` (12),
`Extensions/ViewModelExtensions.cs` (7), `Extensions/EntityExtensions.cs` (7), `Models/Helper/QueryParams.cs` (3),
`TagHelpers/PaginationTagHelper.cs` (3), `TagHelpers/Extensions.cs` (2), `TagHelpers/AlertsTagHelper.cs` (2),
`Models/AlertModel.cs` (2), `Views/Filmes/Index.cshtml` (2), `Models/Helper/QueryableResult.cs` (1),
`Models/FilterViewModel.cs` (1), `Models/ErrorViewModel.cs` (1), `Models/Helper/DataMapping.cs` (1)

`LamuFlix.Test` — `ApiDataModel.cs` (26), `UnitTest1.cs` (26), `EnrichmentTests.cs` (1)

---

## Loop Discipline

### B3 — Closing bar

Both `/code-review` and `/ship-review` are blocked by every unresolved substantive Critical or High finding. Medium, Low, and Info findings do not block closure, but must be recorded and either fixed or accepted as a tracked follow-up. Process/tooling observations are non-findings unless they establish a concrete product or verification risk; then their assigned severity governs. Critical and High findings have credible correctness, security, data-integrity, contract, or gate-evidence failures; lower severity stays auditable without making the loop unfinishable.

### B4 — Frozen scope

Frozen scope is limited to the ticket-authorized configuration files and four `.csproj` `Version` removals, plus the precisely enumerated 35 files (34 `.cs` + 1 Razor view) under the B1 envelope only after the owner's pending YouTrack Scope item 6 amendment is live. Until then, the 35-file portion is not authorized. All other changes are out of scope: anything else is a follow-up issue, not a finding in this round.

### B5 — Round cap

The default maximum is two review rounds. No third review may repair findings or review transport. Any unresolved finding or transport issue is reported with its source evidence rather than publishing a replacement review.
