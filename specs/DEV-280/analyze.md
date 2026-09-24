# DEV-280 Specification Analysis Report

**Command:** `/speckit-analyze` (pipeline T001-equivalent), read-only over the artifacts; this report file is the only write, plus the two AC3 evidence-command lines Patron corrected (H7).
**Round:** **2 of 2 — final for the loop** (round 1 = `C9978A76A44B0E8DF73508CBFA1507C6F5E31D8F48730B7800625DF4A25C50DB`, superseded)
**Date:** 2026-09-23 · **Run by:** Patron
**Base:** `feature/280-spec` @ `b2659b7` · **FEATURE_DIR:** `specs/DEV-280`
**Artifacts:** spec.md, plan.md, tasks.md (provenance: brief.md, CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-280)
**Verdict:** **FREEZABLE** — Critical 0 / High 0 / Medium 3 / Low 5. `gate1: provisional` stands; **Gate 1 stays CLOSED** because owner checkbox 1-(FR-008) is unticked.

---

## R4 adjudication (referral from recon-DEV-280 item 7 and Keel)

**R4 CLOSED. Pin hardening AUTHORIZED.**

1. **Provenance is adequate.** `recon-DEV-280` item 7 now carries the redo: `dotnet package search --exact-match` verbatim outputs for `xunit.v3 4.0.1`, `xunit.runner.visualstudio 4.0.0`, `NSubstitute 6.2.0`, `Shouldly 4.3.0`, `AutoFixture 4.18.1`, `Faker.Net 2.0.163`, `Microsoft.NET.Test.Sdk 18.10.1`, plus the two doc answers (Test.Sdk floor, `OutputType Exe`). Independently corroborated by Patron against the NuGet flat-container index on 2026-09-23: all seven match, and `4.3.0` / `4.18.1` are the latest **stable** (the only newer entries are `5.0.0-preview.2` and `5.0.0-rc.1`). The item's own REDO header names the superseded stale values (`1.1.0 / 3.0.2 / 5.3.0 / 18.0.1`), which is exactly what H1 was about.
2. **Hardening is authorized:** replace the six `<latest stable, recon R4>` placeholders and every "Candidate pin, pending R4 verification" label with the R4 values — `spec.md` FR-006 (63-68), `plan.md` (71-78 header + rows), `tasks.md` T004 (71-77, 86) and T006 (132-134). No requirement, task count, AC, or scope changes.
3. **`Microsoft.NET.Test.Sdk` stays 17.12.0 — do not bump.** R4(b) answers the floor question in the affirmative ("17.12.0 IS above the xunit.v3 floor on net10 … CONTRADICTS earlier must-bump premise"), and Keel's A-adjudication rule was "Test.Sdk stays 17.12.0 unless R4 shows it below the floor". So `plan.md:92` and `tasks.md:78` lose the bump branch rather than gaining one. The recon item-15 tail still saying "must rise 17.12.0 → 18.0.1" is superseded by the item-7 REDO header that names `18.0.1` stale; that is a provenance-hygiene nit (L10), not a live instruction.
4. **`OutputType Exe` becomes unconditional.** R4(c): required for xUnit v3 (explicit build error if missing since Core Framework v3 1.1.0). Make it a firm requirement in `spec.md` FR-010 (114), `plan.md` step 3 (44), and `tasks.md` T005 (112, 119).
5. **MTP runner stays OUT** (not ticket-named) — Q2 unchanged.

---

## Round-1 findings — disposition

| ID | Sev | Status | Evidence |
|----|-----|--------|----------|
| H1 package-pin provenance | HIGH | **Resolved by ruling** (R4 closed; hardening authorized) | recon item 7 + Patron NuGet re-verification; `plan.md:71` still says "Candidate Pin" → authorized edit |
| H2 Test.Sdk bump | HIGH | **Resolved by ruling** (keep 17.12.0) | recon 15(b); `plan.md:92`, `tasks.md:78` conditional text |
| H3 Option-B baseline `15/1/0` | HIGH | **CLOSED** | `tasks.md:54-57` — baseline 18/4/0 once, post-change pair A 18/0/0 · B 15/0/0 |
| H4 `15` as baseline | HIGH | **CLOSED** | `plan.md:26` (unconditional), `plan.md:159` |
| H5 `docs/adr/0013` at rebase | HIGH | **CLOSED** | `plan.md:21` only `specs/DEV-290/`; `tasks.md:36` moves it to the T002 pickup |
| H6 Option B executable | HIGH | **CLOSED** | `spec.md:92,94-98`; `tasks.md:46-51` STOP RULE |
| M1 InMemory kept vs conditional | MED | **CLOSED** | `spec.md:107`; `tasks.md:141` |
| M2 Exe conditional | MED | **Resolved by ruling** → unconditional | R4(c); see §4 above |
| M3 factory name TBD | MED | **CLOSED** | `spec.md:141`; `plan.md:113-115`; `tasks.md:95-100` (A2 `[assumed]`) |
| M4 exact Location values | MED | **CLOSED** | `tasks.md:219-222` — both literals, by member name, "zero assertion logic edits" |
| M5 Location-data wording | MED | **CLOSED** (nit L11) | `spec.md:159` |
| M6 carried InMemory/IModel debt | MED | **Carried by ruling** (no edit) | Follow-up filed by Patron decision; Rigger not connected → queued in the `DEV-280-patron-pending` note |
| M7 "only where ProcessStarter is mocked" | MED | **CLOSED** | `tasks.md:225` — "UnitTest1 has no NSubstitute substitutes" |
| L1 file-scoped exception | LOW | **CLOSED** | `spec.md:45`, `spec.md:127` pattern-scoped + "Do not grep bare `F:`" |
| L2 AC1/FR-001 duplication | LOW | Accepted, no change | — |
| L3 `bare F\…` typo | LOW | **CLOSED** | `spec.md:127` |
| L4 process tasks unmapped | LOW | Accepted, no change | — |
| L5 duplicate Exe | LOW | **CLOSED** | `tasks.md` T006 no longer re-confirms |
| L6 rebase state | LOW | **CLOSED** | `tasks.md:15` DONE; `plan.md:176` consistent |

---

## Round-2 findings

| ID | Category | Severity | Location(s) | Summary | Disposition |
|----|----------|----------|-------------|---------|-------------|
| **H7** | False-green acceptance evidence | **HIGH → CLOSED by Patron** | spec.md:127; tasks.md:308 | The AC3 evidence command for the backslash form, `git grep "F:\Filmes" -- …`, is **vacuous**: git's BRE reads `\F` as an escaped literal `F`, so the pattern is `F:Filmes` and can never match `F:\Filmes`. Measured on the branch: bare form → exit 1 / **0 matches**; `-F` form → exit 0 / **3 matches** (`UnitTest1.cs:349`, `:534`, `:576`). AC3 could therefore report green with a banned literal still present. | **Fixed by Patron** in both files to `git grep -F "F:\Filmes" …` with the reason inline. Re-persisted after the edit, so this report is newer than every artifact edit. |
| M8 | Inconsistency (ordering refs) | MEDIUM | plan.md:160-165 vs plan.md:42-49 | Milestones cite "Phase II step 5/6/7/8/9/10" while Phase II has **8** steps; Tests.Common is step 2, WorkerTests step 4, EnrichmentTests step 5, UnitTest1 step 6, old packages step 7, verify step 8. | Quill: renumber the milestone rows. |
| M9 | Unresolved placeholder | MEDIUM | plan.md:59; tasks.md:344 | `Awaiting-merge: DEV-280 (#TBD)` asserts a PR state that does not exist yet, with an unresolved `#TBD`. | Quill: drop the line until the PR number exists. |
| M10 | Inconsistency (pin state) | MEDIUM | plan.md:18, 172 vs plan.md:71-78; tasks.md:71 | plan says "Recon-DEV-280 pins filed (exact versions in place)" while the tables and T004 say "candidate pins, pending R4 verification". | Resolved by the authorized hardening edit set (§R4.2). |
| L7 | Terminology drift | LOW | spec.md:81; tasks.md:326, 358 | The D4 → "checkbox 1-(FR-008)" rename is incomplete in three places. | Quill: finish the rename. |
| L8 | False footers | LOW | spec.md:161; plan.md:205; tasks.md:356 | "Line count (body only)" states 159 / 200 / 355 lines; actual 161 / 205 / 358. | Quill: correct or delete the footers. |
| L9 | Convention breach (`[assumed]` log) | LOW | ASSUMPTIONS.md:8 | The A2 row puts `LamuFlixContextFactory` in the **Tag** column and `Quill [assumed]` in **Accepted by**; the tag must be `[assumed]` and the acceptor Patron. | Quill: re-column the row. |
| L10 | Provenance hygiene | LOW | recon-DEV-280:4, 12, 15 tail | Items 4 and 12 still describe the Q4 ban as forward-slash-only, and item 15's tail still says "Test.Sdk must rise", both superseded by N1 and the item-7 REDO. The frozen artifacts are correct; the note is not. | Conductor/Rigger: annotate the note so a future reader does not re-open the ban. |
| L11 | Ambiguity (wording) | LOW | spec.md:159 | "use these exact reference values for assertions" still reads as if the assertions were replaced; they are `Location` **data** values, and the assertions are unchanged because they compare against `movie.Location`. | Quill: tighten the sentence. |

---

## Coverage Summary

| Requirement | Has task? | Task IDs | Notes |
|---|---|---|---|
| FR-001 MSTest → xUnit v3 | Yes | T006, T007, T008, T009, T010 | AC1 |
| FR-002 Moq → NSubstitute | Yes | T007, T008, T009, T010 | AC2 |
| FR-003 Shouldly / `Assert.Collection` | Yes | T007, T008, T009 | AC5 |
| FR-004 delete 4 disk-bound tests; no disk I/O | Yes | T009 (A/B) | AC3 (H7 fixed), AC4 |
| FR-005 `tests/LamuFlix.Tests.Common/` | Yes | T005, T006 | A2 `[assumed]` |
| FR-006 six package versions in CPM | Yes | T004 | R4 closed; hardening authorized |
| FR-007 migrate WorkerTests + EnrichmentTests | Yes | T007, T008 | AC1 |
| FR-008 UnitTest1 non-ignored tests (owner checkbox) | Yes | T003, T009, T013 | OPEN — owner |
| FR-009 remove MSTest/Moq packages | Yes | T010 | M1 closed |
| FR-010 project config for xUnit v3 | Yes | T005, T006 | R4(c): Exe required |

**Coverage:** 10/10 requirements (100%). AC1–AC9 each have a verifying task (T011, T012).

## Unmapped Tasks

T001 (rebase, DONE), T002 (pickup drift check), T003 (owner decision + baseline), T013 (PR). Process/gate tasks; no requirement gap.

## Constitution Alignment Issues

- IX test tooling — aligned: xUnit v3, NSubstitute, Shouldly, AutoFixture/Faker.Net; MSTest/Moq removal enforced by T010; non-ticket analyzers excluded (A2).
- IX InMemory / infrastructure-mock prohibition — **documented deviation** (M6): carried 1:1, never extended, ruled not-a-finding, follow-up filed. Converting needs Testcontainers, a §2.3 item 1 dependency the ticket does not decide.
- VII machine paths — the `F:/Filmes` / `F:\Filmes` literal is banned and rewritten (N1); inert drive-prefixed `Location` data permitted by AC3. No secret or connection string carried; the deleted tests remove the `LAMUFLIX_OMDB_API_KEY` / `LAMUFLIX_TEST_CONNECTION` reads. H7 fixed the one vacuous check.
- §2.3 item 1 — no unnamed package added; Test.Sdk is neither added nor bumped (R4.3).
- §2.3 items 5/6 — owner checkbox 1-(FR-008) remains the single open structural item.

## Metrics

| Metric | Value |
|---|---|
| Total requirements (FR + AC) | 19 (10 FR, 9 AC) |
| Total tasks | 13 |
| Coverage % | 100% |
| Critical | 0 |
| High | 0 (1 raised as H7, closed by Patron this round) |
| Medium | 3 (M8, M9, M10) |
| Low | 5 (L7–L11) |
| Ambiguity | 1 (L11) |
| Duplication | 0 open |

## Evidence commands used

- `git grep "F:\Filmes" -- LamuFlix.Test/` → exit 1, 0 matches (vacuous); `git grep -F "F:\Filmes" -- LamuFlix.Test/` → exit 0, 3 matches (`:349`, `:534`, `:576`); `git grep "F:/Filmes" -- LamuFlix.Test/` → exit 0, 1 match (`:27`).
- `https://api.nuget.org/v3-flatcontainer/{id}/index.json` for all seven R4 packages — every pin exists and matches recon item 7.
- `maestri note read "recon-DEV-280"` — items 7, 15(b), 15(c) carry the R4 redo.
- `check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` → `FEATURE_DIR=…\specs\DEV-280`; `.specify/extensions.yml` absent (no hooks).

## Conditions of the freeze

1. **Authorized, decided edit set** (mechanical; no requirement, task-count, AC, or scope change): the R4 pin hardening, the `OutputType Exe` unconditional change, and dropping the Test.Sdk bump branch — Quill; and closing R4 in `CONCLUSIONS.md:79` plus retiring the stale `:70` line — Keel.
2. **Non-blocking residuals** M8, M9, L7–L11 — fix before the T002 pickup drift check.
3. **M6 follow-up** is filed by Patron decision and queued for Rigger (not connected on this canvas).
4. **Gate 1 remains CLOSED**: owner checkbox 1-(FR-008) is unticked; `gate1: provisional` is already recorded (`brief.md:5`, `spec.md:98`). No new rounds remain.

## Next Actions

1. Keel: close R4 in CONCLUSIONS.md and note this report as the round-2 freeze report.
2. Quill: the authorized edit set + M8/M9/L7–L11.
3. After those edits, no further `/speckit-analyze` round is available; the T002 pickup drift check re-confirms the artifacts against `origin/main`.
