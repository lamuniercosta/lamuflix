# DEV-360 grill conclusions

## Q1 — Missing test connection variable

**Question.** `UnitTest1.SetUp` runs before every test, including three live `AssistirFilme_*` tests. Should the ticket's inconclusive result for a missing `LAMUFLIX_TEST_CONNECTION` be applied in `[TestInitialize]`, which would skip those tests, or only where a database connection is consumed?

**Keel answer.** Use the database-dependent path. Preserve the missing-variable inconclusive result while keeping the live playback tests running.

**Patron ruling.** Confirmed. This clarifies Scope item 1 bullet 3 without changing delivery. Patron prepared a ticket comment for Rigger at `C:\Users\lamun\AppData\Local\Temp\opencode\DEV-360-patron-comment-q1.md`; Rigger has since recorded it on DEV-360 with verified read-back (`chain:7`). The implementer may use a connection guard helper or lazy context initialization reached only by the three DB-dependent ignored tests (`TestMethod1`, `TestMethod2`, `TestMethod5`; `TestMethod4` never uses the context), firing on first context access before any file-system access. The context's own missing `LAMUFLIX_CONNECTION` exception remains required. Sources: `task-DEV-360:15-18,31-33`; `LamuFlix.Test/UnitTest1.cs:32-41,46,88,327,344,479,498,541`; charter §2.3 and `task-pipeline:34,43-51`.

**Rationale and implication.** A classwide inconclusive result would suppress two of the only `ProcessStarter` seam tests and could leave a green `dotnet test` exit. The guard must preserve those tests. The owner answered the seam checkbox on 2026-09-23: keep it unchanged (D1).

---

## Q2 — Reference navigation nullability

**Question.** Scope item 3 specifies nullable CLR properties for nullable columns but does not say whether reference navigations mirror their foreign keys or all become nullable because navigations have no columns.

**Keel answer.** Match the relationship: `Movie.Collection` is nullable because `CollectionId` is nullable; the six join navigations remain non-nullable where their foreign keys are required.

**Patron ruling.** Confirmed. No migration. Sources: `task-DEV-360:20-24,35`; `LamuFlix.Data/Models/Movie.cs:18,23`; `LamuFlix.Data/Migrations/LamuFlixContextModelSnapshot.cs:81,118-152,180-224`; charter §2.3 item 3.

**Rationale and implication.** The navigation communicates the relationship's optionality. Column annotations still follow the snapshot, and the pending-model-changes check must remain clean.

---

## Q3 — Standards findings within budget

**Question.** Scope item 5 says to fix Ledger's DEV-289 Standards findings when they are in the named files and within budget. What is the budget boundary?

**Patron recommendation.** Limit this ticket's Standards fixes to localized annotation or analyzer edits within the `specs/DEV-289/tasks.md` file set, with no new type or unrelated behavior. Any finding requiring a package, schema or migration, route/DTO/status change, `Features:LocalPlay`, secrets, `Process.Start`, or an out-of-set file goes to Patron for a follow-up ticket; it is not added to the chain. Run Ledger in Phase B against those files as they are on `main` and record findings rather than predicting them in this spec. Sources: `task-DEV-360:26-28,36-37`; `task-pipeline:23-34`; charter §2.3.

**Keel answer and Patron ruling.** Confirmed clauses (a)–(d). This is the acceptance criterion 6 lens. A follow-up ticket records an out-of-scope finding; scheduling it is an owner plan change.

**Rationale and implication.** The ticket is size:M and its live findings are not known during Phase A. The bounded review can fix small local defects while keeping extra work out of the current delivery.

---

## Q4 — Review loop terms

**Question.** What blocks review closure, what is this round's frozen scope, and how many review rounds are allowed?

**Patron recommendation; Keel answer; Patron ruling.** Confirmed: the closing bar is Critical or High findings with a concrete failure scenario; frozen scope is DEV-360 Scope & Technical Design and its authorized acceptance criteria, ending "anything else is a follow-up issue, not a finding in this round." The cap is two review rounds and two fix commits per round. Sources: `task-pipeline:23-29`; `task-DEV-360:14-40`; charter §2.3.

**Rationale and implication.** Review can close against the ticket's requested behavior. Findings outside the frozen scope are routed through Patron, and reaching the cap is reported to the user instead of opening a third round.
