# Specification Analysis Report: DEV-360

I read spec.md, plan.md, tasks.md, research.md, quickstart.md, CONCLUSIONS.md, ASSUMPTIONS.md, brief.md and checklists/requirements.md, and checked them against the constitution (v1.0.0). I changed no files and ignored the old `analyze.md`. Owner decisions D1–D3 and the Patron rulings you listed were treated as settled, not as findings. I also spot-checked the line numbers the artifacts cite against the worktree:

- **Cited lines all match the code:**
  - `LamuFlixContext.cs:31-41` (the fallback string is on :36).
  - `UnitTest1.cs:37` (the test fallback is on :38).
  - `FilmesServices.cs:57` (`ProcessStarter`).
  - `FilmesController.cs:79,127,141,155` (the `BadRequest(ex.Message)` calls).
  - `Movie.cs:21`, `Directory.Build.props:20`, `harness.yml:22`.
- **The 16-file baseline set is accurate:** there are 12 model files.
- **Nothing calls `Delete(object id)`** anywhere in the repo.
- **The only credential literals in Data or Test** are the two fallbacks this ticket removes.
- **A .NET 8 runtime is installed** (8.0.22), so a pinned `dotnet-ef` 9.0.x tool with `rollForward: false` should be able to run.

## Findings

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| U1 | Underspecification (sequencing risk) | MEDIUM | tasks.md:22 (T007), tasks.md:39 (T014); research.md:5; quickstart.md:34 | SC-003 needs the probe's `Up` to contain exactly one operation, `AddColumn Status`. The current snapshot comes from EF 2.1.3 (`LamuFlixContextModelSnapshot.cs:17`) and has no `HasColumnType` or charset annotations, so Pomelo 9 may produce extra `AlterDatabase` or `AlterColumn` operations that no nullability fix can remove. T007 records this baseline but does not stop on it. Only T014 stops, which is after T008–T013a have already landed. The H1 ruling is not in question; the timing of the stop is. | Have T007 stop and report to Patron straight away if its baseline contains any operation that isn't `AddColumn Status` and isn't a nullability `AlterColumn` that T013 is expected to remove. Keep T014 as the final check. |
| A1 | Ambiguity (gate interpretation) | MEDIUM | tasks.md:46-51 (T018); tasks.md:17 (T005); plan.md:29 | T018 doesn't say which files the Roslyn and InspectCode gates run over. Only the refactor gate is scoped ("whole changed `.cs` files"). If run over the 16-file baseline set, InspectCode exits 1 by design because of `MovieEnrichmentMessage.cs:1`. If run with no arguments (changed files only), it should exit 0 and the DEV-366 citation is irrelevant. So it's unclear whether exit 1 is acceptable at T018. | State that T018 runs every gate over the changed `.cs` files (the default diff against `main`) and expects exit 0. Keep the DEV-366 citation for the T005 baseline only. |
| C1 | Coverage | LOW | quickstart.md:13-14 (§1.5–§1.6); spec.md:52 (SC-006) | The check that the Design package stays out of other projects covers `LamuFlix.Web` and `LamuFlix.Work` but not `LamuFlix.Test`, which also references `LamuFlix.Data`. The test project isn't shipped, so the risk is small. | Optionally add `LamuFlix.Test` to the §1.5 `--include-transitive` check, or say why it is left out. |
| I1 | Inconsistency (citation drift) | LOW | research.md:3 (`UnitTest1.cs:32-41`), tasks.md:27 (`:32-44`), plan.md:23 (`:37`) | Three different line ranges are given for the same test setup. They are all roughly right: `SetUp` spans 32–44 and the literal is on :38. | Use `:32-44` for the method and `:37-38` for the environment-variable read and fallback. |
| I2 | Inconsistency (traceability) | LOW | spec.md:6, spec.md:54-65 | The spec's Input line cites Patron rulings only as `task-DEV-360:89-90`. Its "Gate 1 and authorization envelope" lists C1/C2, H1, H2, M2 and Q1 but not Q4, Round 1, G1 or G2, even though those rulings govern T005, T017 and T018. For example, G1(c) allows targeted suppressions, and the envelope says "Nothing else is authorized". The checklist (requirements.md:13) says those rulings are recorded only in plan.md and tasks.md. | Add one line to the envelope that points to Q4, Round 1, G1 and G2 as recorded in plan.md and tasks.md. |
| D1 | Dependency ordering | LOW | tasks.md:56 | The dependencies section doesn't mention T015 and doesn't say explicitly that T005 comes before T006. Both are implied ("T005 is the Phase B pickup check"; T015 is independent). | Add "T005 precedes T006; T015 is independent and precedes T018." |

**Severity counts:** CRITICAL 0 · HIGH 0 · MEDIUM 2 · LOW 4

## Coverage Summary

| Requirement | Has task? | Task IDs | Notes |
|---|---|---|---|
| FR-001 Remove both fallbacks | Yes | T008, T009, T010 | T010 runs the literal scan |
| FR-002 Explicit missing-variable outcomes | Yes | T008, T009, T010 | quickstart §2–§3 |
| FR-003 Delete throws `KeyNotFoundException` | Yes | T011, T012 | The test is deferred to DEV-280 (D2) |
| FR-004 Snapshot-aligned nullability, no migration | Yes | T013, T013a, T014 | See U1 |
| FR-005 `harness.yml` `warningsAsErrors` | Yes | T015 | |
| FR-006 Ledger Standards review within budget | Yes | T016, T017 | Q3/Q4 closing bar is in T017 |
| FR-007 `ProcessStarter` unchanged | Yes | T013a, T019 | |
| FR-008 Pipeline gates with numeric exits | Yes | T005, T018 | See A1 |
| FR-009 Design package and pinned `dotnet-ef` | Yes | T006, T007 | |
| SC-001 | Yes | T010, T012 | |
| SC-002 | Yes | T009, T010 | |
| SC-003 | Yes | T007, T014 | See U1 |
| SC-004 | Yes | T015, T017 | |
| SC-005 | Yes | T010, T018 | gitleaks runs in T018 |
| SC-006 | Yes | T007 | See C1 |

## Constitution Alignment Issues

None at CRITICAL level. Every constitution citation in plan.md matches its lines. These points rest on your owner decisions or Patron rulings, not on a reading of the constitution:

- **Principle II (`KeyNotFoundException`) and Principle VII (direct environment-variable read):** Patron's C1/C2 ruling.
- **Principle VIII (renaming Portuguese identifiers on contact):** M2 ruling.
- **Principle IX (deferred test, `Assert.Inconclusive` in the MSTest project):** D2 and Q1.
- **Design package vs. the Forbidden list:** D3.

None of these is extended beyond its single site, which is consistent with constitution :423-429. Principle V holds: nothing calls `Delete(object id)`, so no endpoint exposes the new throw path.

## Unmapped Tasks

- **T001–T004:** Phase A process, analysis and records.
- **T005:** Phase B pickup and drift check.

These are workflow tasks, not orphans.

## Metrics

- **Total requirements:** 15 (9 FR + 6 SC)
- **Total tasks:** 22 (T001–T019, plus T001a, T001b and T013a)
- **Coverage:** 100% (15 of 15 requirements have at least one task)
- **Ambiguities:** 1 (A1)
- **Duplications:** 0
- **Critical issues:** 0

## Next Actions

- There are no CRITICAL or HIGH findings, so the analysis bar of Critical 0 and High 0 is met.
- **T001 still needs a persisted `analyze.md` with its SHA256.** This read-only run did not write it, so T001 stays open until someone saves this report.
- **U1 and A1 are cheap to fix before freezing.** Both are small edits to tasks.md (T007's stop condition and T018's gate scope). The LOW findings are optional wording fixes.
- **Any edit restarts the check.** Under T001a, you'd need to rerun `/speckit-analyze` and persist the new report, newer than every edit, before freezing the plan.

Would you like me to suggest concrete remediation edits for the top 2 issues (U1, A1)? I won't apply anything unless you ask.
