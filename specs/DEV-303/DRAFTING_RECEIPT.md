# DEV-303 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-01
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-303-spec` · branch `feature/303-spec`
**Method**: `speckit-specify` → `speckit-plan` → `speckit-tasks` run in one flow with `$env:PYTHONUTF8='1'` set. `setup-plan.ps1 -Json` and `setup-tasks.ps1 -Json` resolved `specs/DEV-303`. Plan and tasks were filled against the repo's templates directly (precedent: DEV-301, DEV-302 receipts).

**Inputs**: `specs/DEV-303/brief.md` (frozen; §5 plan decisions, §5.5 ordering, §5.4 gates, §7 traceability, §9 recon [R-1]–[R-4]), `CONCLUSIONS.md` (Patron Q1–Q12), `ASSUMPTIONS.md`, `grill-questions.md`, `recon-DEV-303`, `specs/PRODUCT.md`, `.specify/memory/constitution.md`, and the current tree (ports, options records, value-object `TryCreate` signatures, the RabbitMq registration and health-check precedent, the existing test layout and ArchitectureTests rules).

**Artifacts written**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, this receipt. `.specify/feature.json` already read `specs/DEV-303` and was left as found.

**Decisions**: none. Every choice traces to a cited ruling in the brief, `CONCLUSIONS.md` or recon. The three judgement calls the brief explicitly delegated to the drafting role are recorded once each in `plan.md` § Project Structure: health state and health check in two files (one-type-per-file), the options-record unit tests in a new per-record file (matching `RabbitMqOptionsTests.cs`), and the WireMock fixture flat in `LamuFlix.IntegrationTests` (matching `RabbitMqProbe.cs`, with no csproj edit outside the ticket).

**Verification performed**: standard-handler defaults, the retry/breaker predicate set and the strategy order cross-checked against Microsoft Learn; constitution gates checked one by one against `.specify/memory/constitution.md`. The library validation ranges were **not** established in the first pass — see round 1 below.

**Open questions**: none. No `needs decision:` item, no `blocked: structural` escalation, no owner checkbox. Gate 1 unchanged (Q12). Property-test opt-out (Q10) is recorded for Bernstein in the task note; the gate reports SKIP, not PASS.

---

## Round 1 (spec review fixes, `brief.md` §8 D1–D7)

**Date**: 2026-10-01 · **Reviewer**: Keel (`[from Keel] DEV-303 analyze round 1 of 2`) · **Role**: Quill

**Applied**: D1–D7 across `spec.md`, `plan.md`, `tasks.md` and `checklists/requirements.md` — the vendor-name rename across all three artifacts (Core record, nested `ResponseMapper`/`Response`/`Log`, health types, the `metadata-provider` check identifier, `AddMetadataProvider`, the renamed test and fixture types), the D2 span-tag rule, the D3 constructor and accessibility fix, the D5 phase reorder, the D4 health-state flag and its new Degraded case, the D6 `IMovieRepository` fake plus the inverse composition case, D7 `Theory`+`MemberData`, and the sweep that leaves no stale vendor-named helper.

**Corrections to this receipt from round 1**: the first pass asserted that `LamuFlix.ArchitectureTests` holds no vendor-naming rule. That claim had no source and is withdrawn; D1 and Patron's `2ef2c04` decide the naming on constitution VIII:270/273-276 directly. The first pass also left the `[Range]` minima as prose. They are now tabulated with exact values in `plan.md` §Approach, read by reflection over `CustomAttributeData` on the assemblies the pinned `Microsoft.Extensions.Http.Resilience` 10.10.0 actually resolves to — `Microsoft.Extensions.Resilience` 10.10.0 → `Polly.Extensions`/`Polly.RateLimiting` 8.4.2 → `Polly.Core` 8.4.2 — because the `RangeAttribute`s sit on the inherited Polly base types that Microsoft Learn's `Http*` reference pages do not render. One value differs from the brief's prose: the library's `FailureRatio` range is `[Range(0, 1)]`, inclusive of `0`, not `(0, 1]`. The plan mirrors the library and says so; the shipped default stays at the ruled `0.1`.

**Task renumbering**: D5 reordered the phases, so the fix list's task numbers map to new numbers by content. Old → new: T010 (fixture) → T020; T011 (mapper) → T012; T012 (mapping tests) → T021; T013 (adapter class) → T011; T016 (span) → T015; T017–T020 (provider tests) → T022–T025; T019 (timeout run) → T026; T021 (registration) → T016; T022 (handler) → T017; T026 (health state) → T010; T027 (health check) → T018; T029 (health tests) → T029; T033 (composition) → T033. T006, T009 and T005–T008 keep their numbers.