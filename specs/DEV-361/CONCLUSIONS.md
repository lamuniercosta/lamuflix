# DEV-361 grill conclusions

## Q1 — InspectCode file scope

**Question.** Scope item 4 says only the files flagged by the restored inspections may change, but that file set is unknown until an inspection runs. Should the spec infer a file set or freeze the measured paths?

**Patron recommendation.** Run a read-only Phase A `run-jetbrains-inspectcode.ps1 -Files <candidate set> -MinSeverity SUGGESTION` probe, then freeze the verbatim target-inspection paths as AC2's file set. If the probe cannot run, require the implementer to freeze paths before editing. Sources: `task-DEV-361:15-29`; charter §2.3 item 6.

**Keel answer and Patron ruling.** Confirmed. The probe examined the eight files under `LamuFlix.Web/Models` plus `LamuFlix.Web/TagHelpers/Extensions.cs` and `LamuFlix.Test/UnitTest1.cs`. It exited 1 at SUGGESTION with 162 total issues in the candidate set. The five target IDs produced ten diagnostic hits at nine lines only in `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs` (3), `LamuFlix.Web/TagHelpers/Extensions.cs` (2), and `LamuFlix.Test/UnitTest1.cs` (5 across four `if` sites; line 112 has two IDs). This absence claim covers the ten candidates, not the whole solution. `NullableWarningSuppressionIsUsed` also fired but remains outside the five-item restoration until the owner answers B1. Probe log: `%TEMP%/dev-361-inspectcode-probe-bound.log`; relevant output is summarized in `research.md`.

**Rationale and implication.** AC2 uses those three exact file paths. The probe's failure is evidence at a deliberately lower threshold, not a passing gate. A later solution-wide SUGGESTION probe found one extra target-ID WARNING at `Views/Filmes/Details.cshtml:60` and other out-of-scope WARNING+ hits; `research.md` records them for Patron follow-up. The Phase B three-file warning-level gate must pass after the five rules are restored and local findings addressed.

---

## Q2 — KeyValuePair equality in TagHelpers

**Question.** `QueryStringValues.Remove(qs)` in `TagHelpers/Extensions.cs:43,56` uses default `KeyValuePair` struct equality. Should the fix preserve the existing match guard and compare fields, or also change the latent null-value behavior?

**Patron recommendation; Keel answer; Patron ruling.** Confirmed: compare key and value fields under the current guard, preserving reachable behavior. A `RemoveAll` predicate using ordinal-ignore-case key comparison and ordinal value comparison is suitable. Do not drop the guard or turn it into key-only removal. Record the latent null-value edge as a recon fact; Patron decides a follow-up only if a caller can reach it. Sources: `task-DEV-361:19`; `LamuFlix.Web/TagHelpers/Extensions.cs:25,41-56,65`.

**Rationale and implication.** Current callers normalize or supply non-null values. A broader pagination behavior change is outside this ticket's inspection restoration.

---

## Q3 — Always-true branches in ignored tests

**Question.** The condition-family inspection flags four branches in `UnitTest1.TestMethod1` and `TestMethod2`. Should they be fixed now, suppressed pending DEV-280, or left for DEV-280 to delete?

**Patron recommendation; Keel answer; Patron ruling.** Confirmed: simplify the branches in place now, quoting both test names. `movies` is non-nullable in those methods, making the null tests dead; retain the remaining membership condition. DEV-280 is later in the chain, so leaving these lines would fail DEV-361 AC2. No suppression and no DEV-280 comment is required for this code-branch choice. Sources: `task-DEV-361:20,23,27`; `LamuFlix.Test/UnitTest1.cs:46-112`; `chain:7-11`.

**Rationale and implication.** This keeps the warning-level gate satisfiable in the planned chain order and changes no live test behavior.

---

## Q4 — Real model defect versus binder false positive

**Question.** How do we distinguish real unused setters/unchanged collections from properties populated by MVC binding or JSON deserialization, and what local suppression form meets AC3?

**Patron recommendation; Keel answer; Patron ruling.** Confirmed: suppress only after checking that no in-product assignment exists and the writer is external binding or serialization. For the three measured `FilmesFilterViewModel` properties, `[FromQuery]` MVC binding is the expected writer (`FilmesController.cs:25`); JSON deserialization is not established for these hits. Use an in-code `// ReSharper disable <InspectionId>` / `// ReSharper restore <InspectionId>` bracket at the affected type/member with a one-line reason naming the writer. Do not use file-scoped `.editorconfig` or `[SuppressMessage]`. Fix genuine unused accessors/collections minimally, without deleting binder-visible properties. Any behavior change or package need goes to Patron for a follow-up; AC2 must still be green. Sources: `task-DEV-361:15-18,22,27-29`; `FilmesViewModel.cs:14,16,20` probe results; charter §2.3.

**Rationale and implication.** The warning rule remains enabled globally while a precisely documented external writer can be exempted at its member. The exact suppression wording is a taste assumption in `ASSUMPTIONS.md`.

---

## Q5 — Review loop terms

**Question.** What blocks review closure, what scope is frozen, and how many rounds may run?

**Patron recommendation; Keel answer; Patron ruling.** Confirmed: Critical or High findings with concrete failure scenarios block; frozen scope is the five ticket-authorized inspection restorations, their three measured flagged files, and ticket acceptance criteria, ending "anything else is a follow-up issue, not a finding in this round." Cap: two review rounds, with at most two fix commits per round. Sources: `task-DEV-361:14-30`; `task-pipeline:23-29`; charter §2.3.

**Rationale and implication.** The owner B1 reversal remains open and cannot be closed by Patron. Wider findings are routed to Patron for follow-up without extending this ticket.

---

## C1 — Principle VIII language migration (Patron DEV-361 analyze ruling)

**Patron ruling.** Preserve existing Portuguese identifiers during DEV-361's bounded suppression and minimal fixes, introduce no new Portuguese identifiers, and record a separate Epic-1 language migration issue for `FilmesFilterViewModel` and the `AssistirFilme_*` tests in `UnitTest1.cs:479,498,541`. This reconciles Principle VIII (`constitution.md:217-232`) with transitional debt (`constitution.md:421-429`) for this ticket. Rigger records that follow-up under T003; it is not scheduled into the chain and is not an owner checkbox.
